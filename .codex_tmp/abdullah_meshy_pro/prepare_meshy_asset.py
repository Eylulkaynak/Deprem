"""Normalize, extract textures, export, and preview the approved Meshy GLB."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


def arguments() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--glb", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--role", default="AbdullahEkinci")
    parser.add_argument("--height", default=1.8, type=float)
    return parser.parse_args(argv)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def import_and_join(glb_path: Path, role: str) -> bpy.types.Object:
    bpy.ops.import_scene.gltf(filepath=str(glb_path.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported from {glb_path}")

    bpy.ops.object.select_all(action="DESELECT")
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()

    character = bpy.context.view_layer.objects.active
    character.name = role
    character.data.name = f"{role}_Mesh"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return character


def object_bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    maximum = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    return minimum, maximum


def normalize_character(character: bpy.types.Object, target_height: float) -> None:
    minimum, maximum = object_bounds(character)
    height = maximum.z - minimum.z
    if height <= 1.0e-6:
        raise RuntimeError("Imported character has zero height")

    uniform_scale = target_height / height
    character.scale = (uniform_scale, uniform_scale, uniform_scale)
    bpy.context.view_layer.objects.active = character
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    minimum, maximum = object_bounds(character)
    center = (minimum + maximum) * 0.5
    character.location += Vector((-center.x, -center.y, -minimum.z))
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def remove_waist_artifact(character: bpy.types.Object) -> dict[str, object]:
    """Delete the disconnected waist spike and close the shirt opening below it."""
    mesh = character.data
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)

    component_index = [-1] * len(mesh.vertices)
    components: list[list[int]] = []
    for start in range(len(mesh.vertices)):
        if component_index[start] >= 0:
            continue
        index = len(components)
        stack = [start]
        component_index[start] = index
        vertices: list[int] = []
        while stack:
            current = stack.pop()
            vertices.append(current)
            for neighbor in adjacency[current]:
                if component_index[neighbor] < 0:
                    component_index[neighbor] = index
                    stack.append(neighbor)
        components.append(vertices)

    faces_by_component: list[list[int]] = [[] for _ in components]
    for polygon in mesh.polygons:
        faces_by_component[component_index[polygon.vertices[0]]].append(polygon.index)

    all_points = [vertex.co for vertex in mesh.vertices]
    full_min = Vector(tuple(min(point[axis] for point in all_points) for axis in range(3)))
    full_max = Vector(tuple(max(point[axis] for point in all_points) for axis in range(3)))
    full_extent = full_max - full_min

    selected_components: list[int] = []
    selected_faces: list[int] = []
    reports: list[dict[str, object]] = []
    for index, vertices in enumerate(components):
        face_indices = faces_by_component[index]
        if not face_indices or len(face_indices) > 2:
            continue
        points = [mesh.vertices[vertex].co for vertex in vertices]
        minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        extent = maximum - minimum
        is_waist_spike = (
            minimum.x > full_min.x + full_extent.x * 0.55
            and minimum.z > full_min.z + full_extent.z * 0.50
            and maximum.z < full_min.z + full_extent.z * 0.80
            and extent.x > full_extent.x * 0.15
        )
        if is_waist_spike:
            selected_components.append(index)
            selected_faces.extend(face_indices)
            reports.append(
                {
                    "component": index,
                    "faces": len(face_indices),
                    "min": list(minimum),
                    "max": list(maximum),
                }
            )

    patch_center: Vector | None = None
    filled_hole_edges = 0
    filled_faces = 0
    if selected_faces:
        selected_face_set = set(selected_faces)
        selected_vertices = sorted(
            {
                vertex
                for face_index in selected_face_set
                for vertex in mesh.polygons[face_index].vertices
            }
        )
        base_points = [
            mesh.vertices[vertex].co.copy()
            for vertex in selected_vertices
            if mesh.vertices[vertex].co.z < full_min.z + full_extent.z * 0.62
        ]
        if base_points:
            patch_center = sum(base_points, Vector()) / len(base_points)
            patch_center.y = min(point.y for point in base_points) - full_extent.y * 0.012

        patch_material_index = 0

        bm = bmesh.new()
        bm.from_mesh(mesh)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(
            bm,
            geom=[bm.faces[index] for index in sorted(selected_face_set)],
            context="FACES",
        )
        loose_vertices = [vertex for vertex in bm.verts if not vertex.link_faces]
        if loose_vertices:
            bmesh.ops.delete(bm, geom=loose_vertices, context="VERTS")
        if patch_center is not None:
            # The spike masks a narrow opening in a non-manifold shirt surface.
            # Put a textured backing just inside the shirt so the existing rim
            # naturally hides the repair from every exterior camera angle.
            surface_center = Vector((0.39976, -0.1600, 0.5806))
            surface_normal = Vector((0.0, -1.0, 0.0))
            tangent_u = Vector((surface_normal.y, -surface_normal.x, 0.0)).normalized()
            tangent_v = surface_normal.cross(tangent_u).normalized()
            center_vertex = bm.verts.new(surface_center)
            rim_vertices = []
            segments = 16
            for index in range(segments):
                angle = math.tau * index / segments
                point = (
                    surface_center
                    + tangent_u * (math.cos(angle) * 0.036)
                    + tangent_v * (math.sin(angle) * 0.052)
                )
                rim_vertices.append(bm.verts.new(point))

            patch_faces = []
            uv_layer = bm.loops.layers.uv.active
            for index in range(segments):
                face = bm.faces.new(
                    (
                        center_vertex,
                        rim_vertices[index],
                        rim_vertices[(index + 1) % segments],
                    )
                )
                face.normal_update()
                if face.normal.dot(surface_normal) < 0.0:
                    face.normal_flip()
                    face.normal_update()
                face.material_index = patch_material_index
                face.smooth = True
                if uv_layer is not None:
                    for loop in face.loops:
                        loop[uv_layer].uv = (0.0052, 0.9530)
                patch_faces.append(face)
            patch_center = surface_center
            filled_faces = len(patch_faces)
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()

    return {
        "components": selected_components,
        "removed_faces": len(set(selected_faces)),
        "patch_center": list(patch_center) if patch_center is not None else None,
        "filled_hole_edges": filled_hole_edges,
        "filled_faces": filled_faces,
        "details": reports,
    }


def linked_image(socket: bpy.types.NodeSocket | None) -> bpy.types.Image | None:
    if socket is None or not socket.is_linked:
        return None
    seen: set[int] = set()
    stack = [link.from_node for link in socket.links]
    while stack:
        node = stack.pop()
        pointer = node.as_pointer()
        if pointer in seen:
            continue
        seen.add(pointer)
        if node.type == "TEX_IMAGE" and getattr(node, "image", None):
            return node.image
        for input_socket in node.inputs:
            if input_socket.is_linked:
                stack.extend(link.from_node for link in input_socket.links)
    return None


def image_roles(character: bpy.types.Object) -> dict[bpy.types.Image, set[str]]:
    result: dict[bpy.types.Image, set[str]] = {}
    for slot in character.material_slots:
        material = slot.material
        if not material or not material.use_nodes or not material.node_tree:
            continue
        for node in material.node_tree.nodes:
            if node.type != "BSDF_PRINCIPLED":
                continue
            for socket_name, role in (
                ("Base Color", "BaseColor"),
                ("Metallic", "Metallic"),
                ("Roughness", "Roughness"),
                ("Alpha", "Alpha"),
                ("Normal", "Normal"),
            ):
                image = linked_image(node.inputs.get(socket_name))
                if image:
                    result.setdefault(image, set()).add(role)
    for image in bpy.data.images:
        if image.name not in {"Render Result", "Viewer Node"}:
            result.setdefault(image, set()).add("Texture")
    return result


def safe_name(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9_-]+", "_", value).strip("_") or "Texture"


def extract_textures(
    character: bpy.types.Object,
    output_dir: Path,
    role: str,
) -> list[dict[str, object]]:
    output_dir.mkdir(parents=True, exist_ok=True)
    roles_by_image = image_roles(character)
    records: list[dict[str, object]] = []
    used_names: set[str] = set()
    for index, (image, roles) in enumerate(roles_by_image.items()):
        ordered = sorted(roles - {"Texture"}) or ["Texture"]
        joined = "_".join(ordered)
        if "BaseColor" in roles:
            stem = f"{role}_BaseColor"
        elif "Normal" in roles:
            stem = f"{role}_Normal"
        elif {"Metallic", "Roughness"} & roles:
            stem = f"{role}_MetallicRoughness"
        else:
            stem = f"{role}_{safe_name(joined)}_{index}"
        candidate = stem
        suffix = 2
        while candidate in used_names:
            candidate = f"{stem}_{suffix}"
            suffix += 1
        used_names.add(candidate)
        path = output_dir / f"{candidate}.png"

        image.file_format = "PNG"
        image.filepath_raw = str(path.resolve())
        image.save()
        image.filepath = str(path.resolve())
        image.source = "FILE"
        image.reload()
        records.append(
            {
                "name": image.name,
                "roles": ordered,
                "path": str(path.resolve()),
                "width": int(image.size[0]),
                "height": int(image.size[1]),
            }
        )
    return records


def export_assets(character: bpy.types.Object, output_dir: Path, role: str) -> dict[str, str]:
    output_dir.mkdir(parents=True, exist_ok=True)
    blend_path = output_dir / f"{role}_MeshyPro.blend"
    fbx_path = output_dir / f"{role}_MeshyPro_Unrigged.fbx"
    obj_path = output_dir / f"{role}_MeshyPro.obj"

    bpy.ops.object.select_all(action="DESELECT")
    character.select_set(True)
    bpy.context.view_layer.objects.active = character

    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path.resolve()),
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )

    bpy.ops.wm.obj_export(
        filepath=str(obj_path.resolve()),
        export_selected_objects=True,
        export_materials=True,
        path_mode="COPY",
        forward_axis="NEGATIVE_Z",
        up_axis="Y",
    )

    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path.resolve()))
    return {
        "blend": str(blend_path.resolve()),
        "fbx": str(fbx_path.resolve()),
        "obj": str(obj_path.resolve()),
    }


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    return material


def render_previews(character: bpy.types.Object, output_dir: Path, role: str) -> list[str]:
    output_dir.mkdir(parents=True, exist_ok=True)
    minimum, maximum = object_bounds(character)
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum
    radius = max(extent) * 2.8

    bpy.ops.mesh.primitive_plane_add(size=max(extent.x, extent.y) * 4.0, location=(0, 0, 0))
    ground = bpy.context.object
    ground.name = "PreviewGround"
    ground.data.materials.append(make_material("PreviewGroundMaterial", (0.12, 0.15, 0.19, 1.0)))

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(extent.x, extent.z) * 1.22
    bpy.context.scene.camera = camera

    lights: list[bpy.types.Object] = []
    for location, energy, size in (
        ((-2.5, -3.0, 3.2), 1150, 2.0),
        ((2.5, -1.5, 2.4), 850, 1.6),
        ((0.0, 2.0, 3.0), 700, 1.4),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.rotation_euler = ((center - light.location).to_track_quat("-Z", "Y").to_euler())
        lights.append(light)

    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.world.color = (0.035, 0.05, 0.075)

    views = {
        "Front_YMinus": Vector((0.0, -radius, center.z + extent.z * 0.02)),
        "Back_YPlus": Vector((0.0, radius, center.z + extent.z * 0.02)),
        "Left_XMinus": Vector((-radius, 0.0, center.z + extent.z * 0.02)),
        "ThreeQuarter": Vector((-radius * 0.72, -radius * 0.72, center.z + extent.z * 0.05)),
    }
    outputs: list[str] = []
    for label, location in views.items():
        camera.location = location
        camera.rotation_euler = ((center - camera.location).to_track_quat("-Z", "Y").to_euler())
        path = output_dir / f"{role}_{label}.png"
        scene.render.filepath = str(path.resolve())
        bpy.ops.render.render(write_still=True)
        outputs.append(str(path.resolve()))

    bpy.data.objects.remove(ground, do_unlink=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    for light in lights:
        bpy.data.objects.remove(light, do_unlink=True)
    return outputs


def main() -> None:
    args = arguments()
    output_root = args.output.resolve()
    game_ready = output_root / "GameReady"
    textures = game_ready / "Textures"
    previews = output_root / "Preview"

    clear_scene()
    character = import_and_join(args.glb.resolve(), args.role)
    cleanup = remove_waist_artifact(character)
    normalize_character(character, args.height)
    texture_records = extract_textures(character, textures, args.role)
    exports = export_assets(character, game_ready, args.role)
    preview_paths = render_previews(character, previews, args.role)

    minimum, maximum = object_bounds(character)
    triangle_count = sum(len(obj.data.polygons) for obj in [character])
    vertex_count = len(character.data.vertices)
    metadata = {
        "role": args.role,
        "source": str(args.glb.resolve()),
        "height_m": maximum.z - minimum.z,
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "vertices": vertex_count,
        "triangles": triangle_count,
        "materials": [slot.material.name for slot in character.material_slots if slot.material],
        "cleanup": cleanup,
        "textures": texture_records,
        "exports": exports,
        "previews": preview_paths,
    }
    metadata_path = output_root / "MeshyPro_Metadata.json"
    metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=exports["blend"])
    print("[MeshyPro] " + json.dumps(metadata))


if __name__ == "__main__":
    main()
