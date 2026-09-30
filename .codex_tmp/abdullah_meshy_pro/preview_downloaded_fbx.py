"""Normalize and render the manually downloaded high-resolution Meshy FBX."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))

from prepare_meshy_asset import (
    clear_scene,
    normalize_character,
    object_bounds,
    render_previews,
)


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--fbx", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args(argv)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)

    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(args.fbx.resolve()), use_image_search=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    character = bpy.context.view_layer.objects.active
    character.name = "AbdullahEkinci_HighRes"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    normalize_character(character, 1.8)
    previews = render_previews(character, output, "AbdullahEkinci_HighRes")
    minimum, maximum = object_bounds(character)
    print(
        "[HighResPreview] "
        + json.dumps(
            {
                "vertices": len(character.data.vertices),
                "triangles": len(character.data.polygons),
                "bounds_min": list(minimum),
                "bounds_max": list(maximum),
                "previews": previews,
            }
        )
    )


if __name__ == "__main__":
    main()
