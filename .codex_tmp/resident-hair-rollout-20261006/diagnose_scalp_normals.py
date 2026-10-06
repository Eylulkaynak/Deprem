import bpy,pathlib
root=pathlib.Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
sc=bpy.context.scene;sc.cycles.samples=8
hair=[o for o in sc.objects if 'tailored hair silhouette' in o.name]
for ob in hair:
 for mod in list(ob.modifiers):
  if mod.type=='SOLIDIFY':ob.modifiers.remove(mod)
sc.render.filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-no-solidify.png');bpy.ops.render.render(write_still=True)
for ob in hair:ob.data.normals_split_custom_set([(0,0,0)]*len(ob.data.loops))
sc.render.filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-geometric-normals.png');bpy.ops.render.render(write_still=True)
