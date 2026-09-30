"""Raycast preview pixels to identify the exact mesh faces behind visual defects."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    maximum = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    return minimum, maximum


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--blend", required=True, type=Path)
    args = parser.parse_args(argv)
    bpy.ops.wm.open_mainfile(filepath=str(args.blend.resolve()))
    character = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    minimum, maximum = bounds(character)
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum
    radius = max(extent) * 2.8

    bpy.ops.object.camera_add(
        location=(0.0, -radius, center.z + extent.z * 0.02)
    )
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(extent.x, extent.z) * 1.22
    camera.rotation_euler = ((center - camera.location).to_track_quat("-Z", "Y").to_euler())
    bpy.context.view_layer.update()

    width = 512
    height = 512
    rotation = camera.matrix_world.to_quaternion()
    direction = rotation @ Vector((0.0, 0.0, -1.0))
    aspect = width / height
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh = character.data
    pixels = [(295, 225), (294, 220), (296, 224), (285, 220), (260, 220)]
    reports = []
    for pixel_x, pixel_y in pixels:
        local_x = ((pixel_x + 0.5) / width - 0.5) * camera.data.ortho_scale * aspect
        local_y = (0.5 - (pixel_y + 0.5) / height) * camera.data.ortho_scale
        origin = camera.location + rotation @ Vector((local_x, local_y, 0.0))
        hit, location, normal, face_index, obj, _matrix = bpy.context.scene.ray_cast(
            depsgraph, origin, direction, distance=radius * 2.0
        )
        record: dict[str, object] = {
            "pixel": [pixel_x, pixel_y],
            "origin": list(origin),
            "direction": list(direction),
            "hit": hit,
        }
        if hit and obj == character and 0 <= face_index < len(mesh.polygons):
            polygon = mesh.polygons[face_index]
            uv_layer = mesh.uv_layers.active.data if mesh.uv_layers.active else None
            record.update(
                {
                    "face": face_index,
                    "location": list(location),
                    "normal": list(normal),
                    "material": polygon.material_index,
                    "vertices": [list(mesh.vertices[i].co) for i in polygon.vertices],
                    "uvs": (
                        [list(uv_layer[i].uv) for i in polygon.loop_indices]
                        if uv_layer
                        else []
                    ),
                }
            )
        reports.append(record)
    print("[RaycastPixels] " + json.dumps(reports))


if __name__ == "__main__":
    main()
