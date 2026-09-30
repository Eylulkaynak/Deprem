"""Editable travel pose of the original flashlight: upright grip, forward-facing hinged head."""
import importlib.util,pathlib,bpy,math
from mathutils import Matrix,Vector
spec=importlib.util.spec_from_file_location('yy_art',pathlib.Path(__file__).with_name('build_original_art.py'))
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
bpy.ops.wm.open_mainfile(filepath=str(art.SOURCE/'Props/Flashlight.blend'))
bpy.context.preferences.filepaths.save_version=0
pivot=Vector((0,0,.225));turn=Matrix.Translation(pivot)@Matrix.Rotation(math.pi/2,4,'X')@Matrix.Translation(-pivot)
for obj in bpy.context.scene.objects:
    if obj.name.startswith(('LampHead','Lens')):obj.matrix_world=turn@obj.matrix_world
art.sphere('HingedHeadJoint',(0,0,.218),(.043,.043,.037),'tealDark',segments=20,rings=10)
art.curve('FoldOutCarryHandle',[(-.038,.015,.22),(-.065,.015,.34),(.065,.015,.34),(.038,.015,.22)],.021,'teal')
for obj in bpy.context.scene.objects:
    for slot in obj.material_slots:
        if slot.material and slot.material.name.endswith('.001'):
            original=bpy.data.materials.get(slot.material.name[:-4])
            if original:slot.material=original
art.export('FlashlightTravel','Props')
