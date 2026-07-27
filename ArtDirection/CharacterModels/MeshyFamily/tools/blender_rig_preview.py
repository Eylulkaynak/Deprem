"""Render a quick posed preview of a generated family rig."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--fbx", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args(argv)


def main() -> None:
    options = args()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(options.fbx.resolve()))

    rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    pose = rig.pose.bones
    pose["LeftUpperArm"].rotation_mode = "XYZ"
    pose["LeftUpperArm"].rotation_euler.y = math.radians(-22)
    pose["LeftLowerArm"].rotation_mode = "XYZ"
    pose["LeftLowerArm"].rotation_euler.y = math.radians(-18)
    pose["RightUpperLeg"].rotation_mode = "XYZ"
    pose["RightUpperLeg"].rotation_euler.x = math.radians(18)
    pose["RightLowerLeg"].rotation_mode = "XYZ"
    pose["RightLowerLeg"].rotation_euler.x = math.radians(-12)
    bpy.context.view_layer.update()

    points = [
        obj.matrix_world @ Vector(corner)
        for obj in meshes
        for corner in obj.bound_box
    ]
    minimum = Vector(
        tuple(min(point[index] for point in points) for index in range(3))
    )
    maximum = Vector(
        tuple(max(point[index] for point in points) for index in range(3))
    )
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(extent.x, extent.z) * 1.25
    camera.location = Vector((center.x, minimum.y - max(extent) * 3.0, center.z))
    camera.rotation_euler = (
        (center - camera.location).to_track_quat("-Z", "Y").to_euler()
    )
    bpy.context.scene.camera = camera

    for x in (-1.0, 1.0):
        bpy.ops.object.light_add(
            type="AREA",
            location=(
                center.x + extent.x * 0.8 * x,
                minimum.y - extent.y * 2.0,
                center.z + extent.z * 0.6,
            ),
        )
        light = bpy.context.object
        light.data.energy = 900
        light.data.shape = "DISK"
        light.data.size = max(extent) * 0.8
        light.rotation_euler = (
            (center - light.location).to_track_quat("-Z", "Y").to_euler()
        )

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.035, 0.05, 0.075)
    options.output.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(options.output.resolve())
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
