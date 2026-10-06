import bpy,pathlib
from mathutils import Vector
root=pathlib.Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
sc=bpy.context.scene
for ob in sc.objects:
 if 'tailored hair silhouette' not in ob.name:continue
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 for mod in list(ob.modifiers):
  if mod.type!='ARMATURE':bpy.ops.object.modifier_apply(modifier=mod.name)
 d=ob.data;normals=[n.vector.copy() for n in d.corner_normals];count=0
 for face in d.polygons:
  for li in face.loop_indices:
   if normals[li].dot(face.normal)<0:normals[li]=-normals[li];count+=1
 d.normals_split_custom_set(normals);print(ob.name,'FLIPPED',count,flush=True)
sc.cycles.samples=12;sc.render.filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-corrected-normals.png');bpy.ops.render.render(write_still=True)
