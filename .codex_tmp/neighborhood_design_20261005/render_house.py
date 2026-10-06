import bpy, pathlib, math
from mathutils import Vector
folder=pathlib.Path(__file__).parent
bpy.ops.wm.open_mainfile(filepath=str(folder/'Source/Environment/KKTC_House_1.blend'))
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object
floor.location.z=-.04
m=bpy.data.materials.new('WarmFloor');m.diffuse_color=(.65,.61,.49,1);floor.data.materials.append(m)
bpy.ops.object.camera_add(location=(12,-15,9.3))
cam=bpy.context.object
cam.rotation_euler=(Vector((0,2.25,2.7))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=13.8
scene=bpy.context.scene;scene.camera=cam
bpy.ops.object.light_add(type='AREA', location=(-5,-8,14))
key=bpy.context.object;key.data.energy=1900;key.data.shape='DISK';key.data.size=9
key.rotation_euler=(Vector((0,2,2))-key.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.light_add(type='SUN',location=(8,5,12))
sun=bpy.context.object;sun.rotation_euler=(.55,-.5,-.6);sun.data.energy=1.4;sun.data.angle=.25
scene.world.color=(.65,.70,.68)
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(folder/'house-3d-preview.png')
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
bpy.ops.render.render(write_still=True)
