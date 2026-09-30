"""Inspect the body faces immediately behind the disconnected Meshy waist artifact."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--glb", required=True, type=Path)
    return parser.parse_args(argv)


def main() -> None:
    args = arguments()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(args.glb.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    character = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    mesh = character.data
    target = Vector((0.446696, -0.202596, 0.570286))
    uv_layer = mesh.uv_layers.active.data if mesh.uv_layers.active else None
    records: list[dict[str, object]] = []
    for polygon in mesh.polygons:
        center = polygon.center
        xz_distance = Vector((center.x - target.x, center.z - target.z)).length
        distance = (center - target).length
        if xz_distance > 0.14 or center.y > 0.05:
            continue
        uvs = []
        if uv_layer:
            uvs = [list(uv_layer[loop_index].uv) for loop_index in polygon.loop_indices]
        records.append(
            {
                "face": polygon.index,
                "center": list(center),
                "normal": list(polygon.normal),
                "distance": distance,
                "xz_distance": xz_distance,
                "material": polygon.material_index,
                "vertices": [list(mesh.vertices[index].co) for index in polygon.vertices],
                "uvs": uvs,
            }
        )
    records.sort(key=lambda record: (record["xz_distance"], record["distance"]))
    print("[PatchFaces] " + json.dumps(records[:80]))


if __name__ == "__main__":
    main()
