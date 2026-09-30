import bpy
import math
from mathutils import Vector

ROOT = r"C:\Users\Gokturk\Documents\GitHub\Deprem"
FBX_PATH = ROOT + r"\ArtDirection\CharacterModels\MeshyFamily\Baba\BlenderRigged\Baba_Rigged.fbx"
TEXTURE_PATH = ROOT + r"\ArtDirection\CharacterModels\MeshyFamily\Baba\Extracted\Meshy_AI_Cartoon_boy_in_a_blue_0726155643_texture_fbx\Meshy_AI_Cartoon_boy_in_a_blue_0726155643_texture.png"
OUTPUT_PATH = ROOT + r"\.codex_tmp\abdullah_rebuild\base_bright.png"


def point_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX_PATH, use_anim=False)

mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
material = mesh.material_slots[0].material
image = bpy.data.images.load(TEXTURE_PATH, check_existing=False)
for node in material.node_tree.nodes:
    if node.type == "TEX_IMAGE" and node.label.lower() != "normal":
        # The color texture is the only sRGB image in this imported material.
        if node.image and "normal" not in node.image.name.lower() and "roughness" not in node.image.name.lower() and "metallic" not in node.image.name.lower():
            node.image = image

world = bpy.data.worlds.new("Studio World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.72, 0.72, 0.72, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.7

bpy.ops.object.light_add(type="AREA", location=(-110, -180, 170))
key = bpy.context.object
key.name = "Key"
key.data.energy = 1250
key.data.shape = "DISK"
key.data.size = 120
point_at(key, (0, 0, 10))

bpy.ops.object.light_add(type="AREA", location=(140, -90, 70))
fill = bpy.context.object
fill.name = "Fill"
fill.data.energy = 800
fill.data.size = 100
point_at(fill, (0, 0, 0))

bpy.ops.object.light_add(type="AREA", location=(0, 100, 150))
rim = bpy.context.object
rim.name = "Rim"
rim.data.energy = 1000
rim.data.size = 90
point_at(rim, (0, 0, 20))

bpy.ops.object.camera_add(location=(0, -420, 0))
camera = bpy.context.object
camera.name = "Preview Camera"
camera.data.type = "ORTHO"
camera.data.ortho_scale = 220
point_at(camera, (0, 0, 0))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.filepath = OUTPUT_PATH
scene.view_settings.look = "AgX - Medium High Contrast"
bpy.ops.render.render(write_still=True)
print(f"Rendered {OUTPUT_PATH}")
