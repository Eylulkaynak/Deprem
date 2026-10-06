import bpy,pathlib,numpy as np
from mathutils import Vector
root=pathlib.Path.cwd();bpy.ops.wm.open_mainfile(filepath=str(root/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
sc=bpy.context.scene;deps=bpy.context.evaluated_depsgraph_get()
for x,y in [(270,235),(330,233),(288,275),(780,235),(800,244),(1390,225),(1340,220),(880,196),(700,335)]:
 origin=Vector(((x/1650-.5)*2.1,-6,1.31+(.5-y/850)*2.1*850/1650))
 ok,hit,normal,idx,ob,matrix=sc.ray_cast(deps,origin,Vector((0,1,0)))
 if not ok:print('MISS',x,y);continue
 ev=ob.evaluated_get(deps);poly=ev.data.polygons[idx];mat=ev.data.materials[poly.material_index]
 cn=sum((ev.data.corner_normals[i].vector for i in poly.loop_indices),Vector())/len(poly.loop_indices)
 uv=sum((ev.data.uv_layers.active.data[i].uv for i in poly.loop_indices),Vector((0,0)))/len(poly.loop_indices)
 tex=next((n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None)
 col=[]
 if tex:
  pix=np.array(tex.pixels[:]).reshape(tex.size[1],tex.size[0],4);col=pix[int(uv.y*(tex.size[1]-1)),int(uv.x*(tex.size[0]-1)),:3].tolist()
 print('PIX',x,y,'OBJECT',ob.name,'MATERIAL',mat.name,'N',list(normal),'CORNER',list(cn),'P',list(matrix.inverted()@hit),'UV',list(uv),'COL',col,flush=True)
