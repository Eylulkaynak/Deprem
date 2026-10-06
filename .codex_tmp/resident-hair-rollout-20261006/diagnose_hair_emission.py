import bpy,pathlib
from mathutils import Vector
root=pathlib.Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
sc=bpy.context.scene;deps=bpy.context.evaluated_depsgraph_get()
for x,y in [(270,235),(780,215),(1390,225)]:
 origin=Vector(((x/1650-.5)*2.1,-6,1.31+(.5-y/850)*2.1*850/1650));ok,hit,normal,idx,ob,mat=sc.ray_cast(deps,origin,Vector((0,1,0)))
 if not ok:continue
 for point in [Vector((-3,-4,5)),Vector((3,-3,3))]:
  ok2,h2,n2,f2,o2,m2=sc.ray_cast(deps,hit+normal*.0002,(point-hit).normalized(),distance=(point-hit).length)
  print('SHADOW',x,y,ob.name,'blocker',o2.name if ok2 else 'none','distance',(h2-hit).length if ok2 else 0,flush=True)
for ob in sc.objects:
 if 'tailored hair silhouette' not in ob.name:continue
 for mat in ob.data.materials:
  nodes=mat.node_tree.nodes;links=mat.node_tree.links;p=nodes['Principled BSDF'];source=p.inputs['Base Color'].links[0].from_socket;e=nodes.new('ShaderNodeEmission');links.new(source,e.inputs[0]);links.new(e.outputs[0],nodes['Material Output'].inputs[0])
sc.cycles.samples=8;sc.render.filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/hair-emission.png');bpy.ops.render.render(write_still=True)
