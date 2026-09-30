"""Optimize the clean high-resolution Meshy FBX for the Unity character family."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))

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
    parser.add_argument("--base-color", required=True, type=Path)
    parser.add_argument("--normal", required=True, type=Path)
    parser.add_argument("--metallic", required=True, type=Path)
    parser.add_argument("--roughness", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--role", default="AbdullahEkinci")
    parser.add_argument("--height", default=1.8, type=float)
    parser.add_argument("--target-triangles", default=55000, type=int)
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


def relink_images(args: argparse.Namespace) -> list[dict[str, str]]:
    paths = {
        "base": args.base_color.resolve(),
        "normal": args.normal.resolve(),
        "metallic": args.metallic.resolve(),
        "roughness": args.roughness.resolve(),
    }
    records = []
    for image in bpy.data.images:
        lowered = image.name.lower()
        if "normal" in lowered:
            path = paths["normal"]
        elif "metal" in lowered:
            path = paths["metallic"]
        elif "rough" in lowered:
            path = paths["roughness"]
        elif "base" in lowered or "color" in lowered:
            path = paths["base"]
        else:
            continue
        image.filepath = str(path)
        image.source = "FILE"
        image.reload()
        records.append({"image": image.name, "path": str(path)})
    return records


def triangle_count(character: bpy.types.Object) -> int:
    return sum(max(1, len(polygon.vertices) - 2) for polygon in character.data.polygons)


def optimize(character: bpy.types.Object, target_triangles: int) -> tuple[int, int]:
    before = triangle_count(character)
    if before <= target_triangles:
        return before, before
    modifier = character.modifiers.new("GameReady_Decimate", "DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = max(0.001, min(1.0, target_triangles / before))
    modifier.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = character
    character.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    for polygon in character.data.polygons:
        polygon.use_smooth = True
    character.data.update()
    return before, triangle_count(character)


def main() -> None:
    args = arguments()
    output_root = args.output.resolve()
    game_ready = output_root / "GameReady"
    previews = output_root / "Preview"
    clear_scene()
    character = import_character(args.fbx.resolve(), args.role)
    image_records = relink_images(args)
    normalize_character(character, args.height)
    before, after = optimize(character, args.target_triangles)
    exports = export_assets(character, game_ready, args.role)
    preview_paths = render_previews(character, previews, args.role)
    minimum, maximum = object_bounds(character)
    metadata = {
        "role": args.role,
        "source_fbx": str(args.fbx.resolve()),
        "height_m": maximum.z - minimum.z,
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "source_triangles": before,
        "triangles": after,
        "vertices": len(character.data.vertices),
        "materials": [slot.material.name for slot in character.material_slots if slot.material],
        "images": image_records,
        "exports": exports,
        "previews": preview_paths,
    }
    metadata_path = output_root / "MeshyPro_Metadata.json"
    metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=exports["blend"])
    print("[MeshyProHighRes] " + json.dumps(metadata))


if __name__ == "__main__":
    main()
