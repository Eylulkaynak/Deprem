"""Normalize, extract the embedded texture, export and preview a Meshy responder."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy

HELPER_DIR = Path(__file__).resolve().parents[1] / "abdullah_meshy_pro"
sys.path.insert(0, str(HELPER_DIR))

from prepare_meshy_asset import (
    clear_scene,
    export_assets,
    normalize_character,
    object_bounds,
    render_previews,
)


def arguments() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--fbx", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--role", required=True)
    parser.add_argument("--height", default=1.8, type=float)
    return parser.parse_args(argv)


def import_character(fbx_path: Path, role: str) -> bpy.types.Object:
    bpy.ops.import_scene.fbx(filepath=str(fbx_path.resolve()), use_image_search=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported from {fbx_path}")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    character = bpy.context.view_layer.objects.active
    character.name = role
    character.data.name = f"{role}_Mesh"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return character


def save_base_color(game_ready: Path, role: str) -> Path:
    candidates = [
        image
        for image in bpy.data.images
        if image.name not in {"Render Result", "Viewer Node"}
    ]
    if not candidates:
        raise RuntimeError("The FBX has no embedded base-color image")
    image = next(
        (candidate for candidate in candidates if "base" in candidate.name.lower()),
        candidates[0],
    )
    path = game_ready / f"{role}_BaseColor.png"
    game_ready.mkdir(parents=True, exist_ok=True)
    image.file_format = "PNG"
    image.filepath_raw = str(path.resolve())
    image.save()
    image.filepath = str(path.resolve())
    image.source = "FILE"
    image.reload()
    return path.resolve()


def main() -> None:
    args = arguments()
    output_root = args.output.resolve()
    game_ready = output_root / "GameReady"
    previews = output_root / "Preview"
    clear_scene()
    character = import_character(args.fbx.resolve(), args.role)
    normalize_character(character, args.height)
    texture_path = save_base_color(game_ready, args.role)
    exports = export_assets(character, game_ready, args.role)
    preview_paths = render_previews(character, previews, args.role)
    minimum, maximum = object_bounds(character)
    metadata = {
        "role": args.role,
        "source_fbx": args.fbx.name,
        "height_m": maximum.z - minimum.z,
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "triangles": sum(
            max(1, len(polygon.vertices) - 2)
            for polygon in character.data.polygons
        ),
        "vertices": len(character.data.vertices),
        "base_color": str(texture_path),
        "exports": exports,
        "previews": preview_paths,
    }
    (output_root / "Metadata.json").write_text(
        json.dumps(metadata, indent=2), encoding="utf-8"
    )
    bpy.ops.wm.save_as_mainfile(filepath=exports["blend"])
    print("[ResponderPrepared] " + json.dumps(metadata))


if __name__ == "__main__":
    main()
