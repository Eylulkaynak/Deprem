"""Headless Blender auto-rig exporter for the approved Meshy family."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--role", required=True)
    parser.add_argument("--obj", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args(argv)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_character(obj_path: Path, role: str) -> bpy.types.Object:
    bpy.ops.wm.obj_import(
        filepath=str(obj_path),
        forward_axis="NEGATIVE_Z",
        up_axis="Y",
    )
    meshes = [obj for obj in bpy.context.selected_objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported from {obj_path}")

    bpy.ops.object.select_all(action="DESELECT")
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()

    character = bpy.context.view_layer.objects.active
    character.name = role
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return character


def bounds(character: bpy.types.Object) -> tuple[Vector, Vector]:
    corners = [character.matrix_world @ Vector(corner) for corner in character.bound_box]
    minimum = Vector(
        (
            min(corner.x for corner in corners),
            min(corner.y for corner in corners),
            min(corner.z for corner in corners),
        )
    )
    maximum = Vector(
        (
            max(corner.x for corner in corners),
            max(corner.y for corner in corners),
            max(corner.z for corner in corners),
        )
    )
    return minimum, maximum


def create_rig(
    role: str,
    minimum: Vector,
    maximum: Vector,
) -> bpy.types.Object:
    center_x = (minimum.x + maximum.x) * 0.5
    center_y = (minimum.y + maximum.y) * 0.5
    height = maximum.z - minimum.z
    half_width = (maximum.x - minimum.x) * 0.5

    def z(ratio: float) -> float:
        return minimum.z + height * ratio

    def point(x_ratio: float, z_ratio: float, depth: float = 0.0) -> Vector:
        return Vector(
            (
                center_x + half_width * x_ratio,
                center_y + depth * height,
                z(z_ratio),
            )
        )

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    rig = bpy.context.object
    rig.name = f"{role}_Rig"
    rig.data.name = f"{role}_Armature"
    rig.show_in_front = True
    edit_bones = rig.data.edit_bones
    edit_bones.remove(edit_bones[0])

    bones: dict[str, bpy.types.EditBone] = {}

    def bone(
        name: str,
        head: Vector,
        tail: Vector,
        parent: str | None = None,
        connected: bool = False,
    ) -> bpy.types.EditBone:
        result = edit_bones.new(name)
        result.head = head
        result.tail = tail
        if parent:
            result.parent = bones[parent]
            result.use_connect = connected
        bones[name] = result
        return result

    bone("Hips", point(0, 0.29), point(0, 0.38))
    bone("Spine", point(0, 0.38), point(0, 0.48), "Hips", True)
    bone("Chest", point(0, 0.48), point(0, 0.59), "Spine", True)
    bone("Neck", point(0, 0.59), point(0, 0.70), "Chest", True)
    bone("Head", point(0, 0.70), point(0, 0.93), "Neck", True)

    for side_name, side in (("Left", 1.0), ("Right", -1.0)):
        shoulder = point(0.57 * side, 0.57)
        elbow = point(0.79 * side, 0.45)
        wrist = point(0.96 * side, 0.38)
        hand_end = point(1.03 * side, 0.35)
        hip = point(0.14 * side, 0.31)
        knee = point(0.22 * side, 0.19)
        ankle = point(0.22 * side, 0.045)
        foot_end = Vector((ankle.x, center_y - height * 0.09, z(0.025)))
        toe_end = Vector((ankle.x, center_y - height * 0.14, z(0.025)))

        bone(
            f"{side_name}Shoulder",
            point(0.08 * side, 0.57),
            shoulder,
            "Chest",
        )
        bone(
            f"{side_name}UpperArm",
            shoulder,
            elbow,
            f"{side_name}Shoulder",
            True,
        )
        bone(
            f"{side_name}LowerArm",
            elbow,
            wrist,
            f"{side_name}UpperArm",
            True,
        )
        bone(
            f"{side_name}Hand",
            wrist,
            hand_end,
            f"{side_name}LowerArm",
            True,
        )
        bone(f"{side_name}UpperLeg", hip, knee, "Hips")
        bone(
            f"{side_name}LowerLeg",
            knee,
            ankle,
            f"{side_name}UpperLeg",
            True,
        )
        bone(
            f"{side_name}Foot",
            ankle,
            foot_end,
            f"{side_name}LowerLeg",
            True,
        )
        bone(
            f"{side_name}Toes",
            foot_end,
            toe_end,
            f"{side_name}Foot",
            True,
        )

    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def bind(character: bpy.types.Object, rig: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    character.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    try:
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    except RuntimeError:
        bpy.ops.object.parent_set(type="ARMATURE_ENVELOPE")
    repair_weights(character, rig)


def repair_weights(character: bpy.types.Object, rig: bpy.types.Object) -> None:
    deform_bones = [bone for bone in rig.data.bones if bone.use_deform]
    groups = {
        bone.name: character.vertex_groups.get(bone.name)
        or character.vertex_groups.new(name=bone.name)
        for bone in deform_bones
    }

    def distance_to_bone(point: Vector, bone: bpy.types.Bone) -> float:
        start = bone.head_local
        end = bone.tail_local
        direction = end - start
        length_squared = direction.length_squared
        if length_squared <= 1.0e-12:
            return (point - start).length_squared
        factor = max(0.0, min(1.0, (point - start).dot(direction) / length_squared))
        closest = start + direction * factor
        return (point - closest).length_squared

    for vertex in character.data.vertices:
        if vertex.groups:
            continue
        nearest = min(
            deform_bones,
            key=lambda candidate: distance_to_bone(vertex.co, candidate),
        )
        groups[nearest.name].add([vertex.index], 1.0, "REPLACE")

    bpy.ops.object.select_all(action="DESELECT")
    character.select_set(True)
    bpy.context.view_layer.objects.active = character
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)


def export_fbx(
    character: bpy.types.Object,
    rig: bpy.types.Object,
    output_path: Path,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    character.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )


def main() -> None:
    args = arguments()
    clear_scene()
    character = import_character(args.obj.resolve(), args.role)
    minimum, maximum = bounds(character)
    rig = create_rig(args.role, minimum, maximum)
    bind(character, rig)
    export_fbx(character, rig, args.output.resolve())
    print(f"[DepremRig] {args.role}: {args.output.resolve()}")


if __name__ == "__main__":
    main()
