"""Run the project family auto-rig and export a lean FBX with external textures."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--role", required=True)
    parser.add_argument("--obj", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args(argv)

    tools_dir = (
        Path(__file__).resolve().parents[2]
        / "ArtDirection"
        / "CharacterModels"
        / "MeshyFamily"
        / "tools"
    )
    sys.path.insert(0, str(tools_dir))
    import blender_auto_rig as family_rig

    family_rig.clear_scene()
    character = family_rig.import_character(args.obj.resolve(), args.role)
    minimum, maximum = family_rig.bounds(character)
    rig = family_rig.create_rig(args.role, minimum, maximum)
    family_rig.bind(character, rig)

    args.output.resolve().parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    character.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=str(args.output.resolve()),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    print(f"[DepremRigExternal] {args.role}: {args.output.resolve()}")


if __name__ == "__main__":
    main()
