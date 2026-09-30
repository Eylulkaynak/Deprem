"""Build a clean, downloadable Abdullah Ekinci character from the approved family base.

This is an offline art-build script. It adds no Unity runtime behaviour.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


ROOT = Path(r"C:\Users\Gokturk\Documents\GitHub\Deprem")
BASE_FBX = ROOT / "ArtDirection/CharacterModels/MeshyFamily/Baba/BlenderRigged/Baba_Rigged.fbx"
OUTPUT = ROOT / ".codex_tmp/abdullah_rebuild/output"
BASE_COLOR = OUTPUT / "AbdullahEkinci_BaseColor.png"
RIGGED_FBX = OUTPUT / "AbdullahEkinci_GameReady_Rigged.fbx"
UNRIGGED_FBX = OUTPUT / "AbdullahEkinci_GameReady_Unrigged.fbx"
OBJ_PATH = OUTPUT / "AbdullahEkinci_GameReady.obj"
BLEND_PATH = OUTPUT / "AbdullahEkinci_GameReady.blend"
FRONT_PREVIEW = OUTPUT / "AbdullahEkinci_GameReady_RigPreview.png"
THREE_QUARTER_PREVIEW = OUTPUT / "AbdullahEkinci_GameReady_ThreeQuarter.png"


def clear_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name: str, color: tuple[float, float, float, float], roughness: float = 0.58, metallic: float = 0.0) -> bpy.types.Material:
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    return result


def textured_material(name: str, image_path: Path) -> tuple[bpy.types.Material, bpy.types.Image]:
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    nodes = result.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = 0.56
    shader.inputs["Metallic"].default_value = 0.0
    image = bpy.data.images.load(str(image_path), check_existing=False)
    image.name = "AbdullahEkinci_BaseColor"
    texture = nodes.new("ShaderNodeTexImage")
    texture.name = "AbdullahEkinci_BaseColor"
    texture.image = image
    texture.interpolation = "Linear"
    result.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return result, image


def append_material(mesh: bpy.types.Object, result: bpy.types.Material) -> int:
    mesh.data.materials.append(result)
    return len(mesh.data.materials) - 1


def polygon_sample_rgb(mesh: bpy.types.Object, image: bpy.types.Image) -> np.ndarray:
    width, height = image.size
    pixels = np.empty(width * height * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    pixels = pixels.reshape((height, width, 4))
    uv_data = mesh.data.uv_layers.active.data
    result = np.zeros((len(mesh.data.polygons), 3), dtype=np.float32)
    for polygon in mesh.data.polygons:
        samples = []
        for loop_index in polygon.loop_indices:
            uv = uv_data[loop_index].uv
            x = max(0, min(width - 1, int(uv.x * (width - 1))))
            y = max(0, min(height - 1, int(uv.y * (height - 1))))
            samples.append(pixels[y, x, :3])
        result[polygon.index] = np.median(np.asarray(samples), axis=0)
    return result


def classify_body_materials(
    mesh: bpy.types.Object,
    sampled_rgb: np.ndarray,
    shirt_index: int,
    pants_index: int,
    shoes_index: int,
    hair_index: int,
) -> None:
    for polygon in mesh.data.polygons:
        center = sum((mesh.data.vertices[index].co for index in polygon.vertices), Vector()) / len(polygon.vertices)
        red, green, blue = sampled_rgb[polygon.index]
        dark_blue = blue >= red * 0.90 and blue >= green * 0.90 and max(red, green, blue) < 0.38
        dark_brown = red > blue * 1.18 and red > green * 0.93 and max(red, green, blue) < 0.28

        if center.z > 53 and dark_brown:
            polygon.material_index = hair_index
        elif center.z < -77:
            polygon.material_index = shoes_index
        elif -36 < center.z < 43 and dark_blue:
            polygon.material_index = shirt_index
        elif -82 < center.z < -14 and abs(center.x) < 31:
            polygon.material_index = pants_index


def broaden_adult_torso(mesh: bpy.types.Object) -> None:
    """Subtly reduce the childlike silhouette while preserving the approved rig."""
    for vertex in mesh.data.vertices:
        x, y, z = vertex.co
        if -35.0 <= z <= 34.0 and abs(x) < 34.0:
            height_weight = max(0.0, 1.0 - abs(z + 1.0) / 38.0)
            side_weight = max(0.0, 1.0 - max(0.0, abs(x) - 19.0) / 15.0)
            weight = height_weight * side_weight
            vertex.co.x *= 1.0 + 0.13 * weight
            vertex.co.y *= 1.0 + 0.05 * weight


def smooth_object(obj: bpy.types.Object) -> None:
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True


def skin_to_bone(obj: bpy.types.Object, rig: bpy.types.Object, bone_name: str) -> None:
    obj.parent = rig
    obj.matrix_parent_inverse = rig.matrix_world.inverted()
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new(name="Armature", type="ARMATURE")
    modifier.object = rig


def apply_bevel(obj: bpy.types.Object, width: float, segments: int = 3) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new(name="Soft bevel", type="BEVEL")
    modifier.width = width
    modifier.segments = segments
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def create_beard(rig: bpy.types.Object, beard_material: bpy.types.Material) -> list[bpy.types.Object]:
    # A rounded Bezier jawline follows the cheek depth; it avoids the flat-paper look
    # common in single-view reconstructions while keeping the mouth unobstructed.
    points = [
        (-15.0, -19.3, 58.2),
        (-16.0, -20.2, 53.2),
        (-12.8, -22.2, 46.4),
        (-6.8, -23.7, 41.2),
        (0.0, -24.2, 39.5),
        (6.8, -23.7, 41.2),
        (12.8, -22.2, 46.4),
        (16.0, -20.2, 53.2),
        (15.0, -19.3, 58.2),
    ]
    data = bpy.data.curves.new("Abdullah_BeardCurve", type="CURVE")
    data.dimensions = "3D"
    data.resolution_u = 18
    data.bevel_depth = 3.75
    data.bevel_resolution = 5
    data.resolution_u = 24
    spline = data.splines.new(type="BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for bezier_point, position in zip(spline.bezier_points, points):
        bezier_point.co = position
        bezier_point.handle_left_type = "AUTO"
        bezier_point.handle_right_type = "AUTO"
    beard = bpy.data.objects.new("Abdullah_Beard", data)
    bpy.context.collection.objects.link(beard)
    data.materials.append(beard_material)
    bpy.ops.object.select_all(action="DESELECT")
    beard.select_set(True)
    bpy.context.view_layer.objects.active = beard
    bpy.ops.object.convert(target="MESH")
    # Flatten only the tube cross-section, not the cheek-to-chin depth curve.
    for vertex in beard.data.vertices:
        vertex.co.y = -22.0 + (vertex.co.y + 22.0) * 0.58
    smooth_object(beard)
    skin_to_bone(beard, rig, "Head")

    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=(0.0, -25.25, 46.6))
    chin = bpy.context.object
    chin.name = "Abdullah_Beard_Chin"
    chin.scale = (6.8, 1.15, 4.7)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    chin.data.materials.append(beard_material)
    smooth_object(chin)
    skin_to_bone(chin, rig, "Head")

    moustache_parts = []
    for x, rotation in ((-3.4, -0.18), (3.4, 0.18)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=14, location=(x * 0.78, -25.75, 52.9))
        part = bpy.context.object
        part.name = "Abdullah_Moustache_L" if x < 0 else "Abdullah_Moustache_R"
        part.scale = (3.15, 0.82, 0.95)
        part.rotation_euler.y = rotation
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        part.data.materials.append(beard_material)
        smooth_object(part)
        skin_to_bone(part, rig, "Head")
        moustache_parts.append(part)
    return [beard, chin, *moustache_parts]


def create_triangle_patch(name: str, points: list[tuple[float, float, float]], result_material: bpy.types.Material) -> bpy.types.Object:
    data = bpy.data.meshes.new(name + "Mesh")
    data.from_pydata(points, [], [(0, 1, 2)])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(result_material)
    solidify = obj.modifiers.new(name="Fabric thickness", type="SOLIDIFY")
    solidify.thickness = 0.75
    solidify.offset = 0.0
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    obj.select_set(False)
    apply_bevel(obj, 0.65, 3)
    smooth_object(obj)
    return obj


def create_shirt_details(
    rig: bpy.types.Object,
    collar_material: bpy.types.Material,
    detail_material: bpy.types.Material,
) -> list[bpy.types.Object]:
    left = create_triangle_patch(
        "Abdullah_ShirtCollar_L",
        [(-1.2, -15.8, 34.2), (-11.6, -15.0, 29.5), (-4.0, -15.7, 21.7)],
        collar_material,
    )
    right = create_triangle_patch(
        "Abdullah_ShirtCollar_R",
        [(1.2, -15.8, 34.2), (4.0, -15.7, 21.7), (11.6, -15.0, 29.5)],
        collar_material,
    )
    for collar in (left, right):
        skin_to_bone(collar, rig, "Chest")

    bpy.ops.mesh.primitive_cube_add(location=(0.0, -15.05, -0.5), scale=(0.8, 0.48, 22.0))
    placket = bpy.context.object
    placket.name = "Abdullah_ShirtPlacket"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    placket.data.materials.append(collar_material)
    apply_bevel(placket, 0.45, 3)
    skin_to_bone(placket, rig, "Spine")

    buttons = []
    for index, z in enumerate((18.0, 9.0, 0.0, -9.0, -18.0), start=1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, location=(0.0, -16.05, z))
        button = bpy.context.object
        button.name = f"Abdullah_ShirtButton_{index:02d}"
        button.scale = (0.72, 0.38, 0.72)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        button.data.materials.append(detail_material)
        smooth_object(button)
        skin_to_bone(button, rig, "Spine" if z < 14 else "Chest")
        buttons.append(button)
    return [left, right, placket, *buttons]


def create_watch(
    rig: bpy.types.Object,
    strap_material: bpy.types.Material,
    face_material: bpy.types.Material,
) -> list[bpy.types.Object]:
    forearm_direction = Vector((47.877, 0.103, -22.962)) - Vector((39.362, 0.103, -9.668))
    alignment = Vector((0.0, 0.0, 1.0)).rotation_difference(forearm_direction.normalized())
    center = Vector((44.0, 0.4, -17.0))

    bpy.ops.mesh.primitive_torus_add(major_radius=3.35, minor_radius=0.72, major_segments=28, minor_segments=10, location=center)
    strap = bpy.context.object
    strap.name = "Abdullah_WatchStrap"
    strap.rotation_mode = "QUATERNION"
    strap.rotation_quaternion = alignment
    strap.data.materials.append(strap_material)
    smooth_object(strap)
    skin_to_bone(strap, rig, "LeftLowerArm")

    bpy.ops.mesh.primitive_cube_add(location=(43.8, -3.45, -17.2), scale=(1.75, 0.52, 2.2))
    watch_face = bpy.context.object
    watch_face.name = "Abdullah_WatchFace"
    watch_face.rotation_euler.y = math.radians(-31.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    watch_face.data.materials.append(face_material)
    apply_bevel(watch_face, 0.65, 4)
    skin_to_bone(watch_face, rig, "LeftLowerArm")
    return [strap, watch_face]


def setup_studio() -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    world = bpy.data.worlds.new("Abdullah Preview World")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.72, 0.72, 0.72, 1.0)
    background.inputs["Strength"].default_value = 0.72

    def point_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
        obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

    lights = []
    for name, location, energy, size in (
        ("Key", (-115, -190, 170), 1450, 125),
        ("Fill", (145, -110, 75), 900, 100),
        ("Rim", (0, 125, 155), 1200, 90),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        point_at(light, (0, 0, 2))
        lights.append(light)

    bpy.ops.mesh.primitive_plane_add(size=420, location=(0, 0, -95.25))
    floor = bpy.context.object
    floor.name = "Preview Floor"
    floor.data.materials.append(material("Preview Floor Material", (0.49, 0.49, 0.49, 1.0), roughness=0.86))

    bpy.ops.object.camera_add(location=(0, -420, 4))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 220
    point_at(camera, (0, 0, 0))
    bpy.context.scene.camera = camera
    return camera, [*lights, floor]


def configure_render() -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"


def render_preview(camera: bpy.types.Object, path: Path, location: tuple[float, float, float], target: tuple[float, float, float]) -> None:
    camera.location = location
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def export_rigged(path: Path, rig: bpy.types.Object, meshes: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=str(path),
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


def export_unrigged(path: Path, meshes: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    duplicates = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for source in meshes:
        evaluated = source.evaluated_get(depsgraph)
        data = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=depsgraph)
        duplicate = bpy.data.objects.new(source.name + "_Static", data)
        bpy.context.collection.objects.link(duplicate)
        duplicate.matrix_world = source.matrix_world.copy()
        for slot in source.material_slots:
            if slot.material and slot.material.name not in [material_slot.name for material_slot in duplicate.data.materials]:
                duplicate.data.materials.append(slot.material)
        duplicate.select_set(True)
        duplicates.append(duplicate)
    bpy.context.view_layer.objects.active = duplicates[0]
    if len(duplicates) > 1:
        bpy.ops.object.join()
    static = bpy.context.view_layer.objects.active
    static.name = "AbdullahEkinci_Static"
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )
    bpy.ops.wm.obj_export(
        filepath=str(OBJ_PATH),
        export_selected_objects=True,
        export_materials=True,
        forward_axis="NEGATIVE_Z",
        up_axis="Y",
        path_mode="COPY",
    )
    bpy.data.objects.remove(static, do_unlink=True)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(BASE_FBX), use_anim=False)
    rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    rig.name = "AbdullahEkinci_Rig"
    rig.data.name = "AbdullahEkinci_Armature"
    body.name = "AbdullahEkinci_Body"
    body.data.name = "AbdullahEkinci_BodyMesh"

    # Rebuild materials so every path is portable and every gameplay color is intentional.
    body.data.materials.clear()
    base_material, image = textured_material("Abdullah_Base", BASE_COLOR)
    body.data.materials.append(base_material)
    shirt_material = material("Abdullah_Shirt_LightBlue", (0.36, 0.61, 0.86, 1.0), roughness=0.67)
    pants_material = material("Abdullah_Pants_CharcoalNavy", (0.025, 0.035, 0.055, 1.0), roughness=0.72)
    shoes_material = material("Abdullah_Shoes_Black", (0.008, 0.010, 0.014, 1.0), roughness=0.62)
    hair_material = material("Abdullah_Hair_Dark", (0.025, 0.012, 0.009, 1.0), roughness=0.65)
    collar_material = material("Abdullah_Shirt_Collar", (0.43, 0.69, 0.94, 1.0), roughness=0.69)
    detail_material = material("Abdullah_Shirt_Details", (0.09, 0.23, 0.39, 1.0), roughness=0.58)
    beard_material = material("Abdullah_Beard", (0.020, 0.009, 0.006, 1.0), roughness=0.72)
    watch_material = material("Abdullah_Watch_Black", (0.008, 0.009, 0.012, 1.0), roughness=0.42)
    watch_face_material = material("Abdullah_Watch_Face", (0.035, 0.045, 0.052, 1.0), roughness=0.28, metallic=0.35)

    shirt_index = append_material(body, shirt_material)
    pants_index = append_material(body, pants_material)
    shoes_index = append_material(body, shoes_material)
    hair_index = append_material(body, hair_material)
    sampled_rgb = polygon_sample_rgb(body, image)
    classify_body_materials(body, sampled_rgb, shirt_index, pants_index, shoes_index, hair_index)
    broaden_adult_torso(body)
    smooth_object(body)

    accessories = []
    accessories.extend(create_shirt_details(rig, collar_material, detail_material))
    accessories.extend(create_watch(rig, watch_material, watch_face_material))
    character_meshes = [body, *accessories]

    export_rigged(RIGGED_FBX, rig, character_meshes)
    export_unrigged(UNRIGGED_FBX, character_meshes)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)

    camera, studio_objects = setup_studio()
    configure_render()
    render_preview(camera, FRONT_PREVIEW, (0, -420, 4), (0, 0, 0))
    render_preview(camera, THREE_QUARTER_PREVIEW, (205, -355, 32), (0, 0, 0))
    print(f"Built {RIGGED_FBX}")
    print(f"Rendered {FRONT_PREVIEW}")
    print(f"Rendered {THREE_QUARTER_PREVIEW}")


if __name__ == "__main__":
    main()
