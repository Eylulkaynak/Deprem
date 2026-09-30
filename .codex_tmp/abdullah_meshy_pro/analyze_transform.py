"""Report the source-to-game normalization transform for the Meshy character."""

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
    parser.add_argument("--glb", required=True, type=Path)
    args = parser.parse_args(argv)
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
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    points = [vertex.co for vertex in obj.data.vertices]
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    center = (minimum + maximum) * 0.5
    scale = 1.8 / (maximum.z - minimum.z)
    target_game = Vector((0.1694, -0.185, 1.045))
    target_source = Vector(
        (
            target_game.x / scale + center.x,
            target_game.y / scale + center.y,
            target_game.z / scale + minimum.z,
        )
    )
    print(
        "[Transform] "
        + json.dumps(
            {
                "min": list(minimum),
                "max": list(maximum),
                "center": list(center),
                "scale": scale,
                "target_game": list(target_game),
                "target_source": list(target_source),
            }
        )
    )


if __name__ == "__main__":
    main()
