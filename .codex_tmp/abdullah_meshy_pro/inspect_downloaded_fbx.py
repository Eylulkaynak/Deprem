"""Inspect a manually downloaded Meshy FBX package."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--fbx", required=True, type=Path)
    args = parser.parse_args(argv)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(args.fbx.resolve()), use_image_search=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    minimum = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    maximum = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    report = {
        "objects": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "triangles": sum(
            sum(max(1, len(polygon.vertices) - 2) for polygon in obj.data.polygons)
            for obj in meshes
        ),
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "materials": sorted(
            {
                slot.material.name
                for obj in meshes
                for slot in obj.material_slots
                if slot.material
            }
        ),
        "images": [
            {
                "name": image.name,
                "path": bpy.path.abspath(image.filepath),
                "size": list(image.size),
            }
            for image in bpy.data.images
            if image.name not in {"Render Result", "Viewer Node"}
        ],
    }
    print("[DownloadedFBX] " + json.dumps(report))


if __name__ == "__main__":
    main()
