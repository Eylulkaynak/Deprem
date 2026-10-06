import bpy,pathlib,numpy as np
root=pathlib.Path.cwd()
for name in ('Gul','Kemal','Ece'):
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name/(name+'.blend')))
 for ob in bpy.context.scene.objects:
  if ob.type not in ('MESH','ARMATURE'):continue
  print(name,ob.name,'MATRIX',[[round(x,4) for x in row] for row in ob.matrix_world],flush=True)
  if ob.type=='ARMATURE':
   for bn in ('Head','Neck'):print('BONE',bn,'REST',list(ob.data.bones[bn].head_local),'POSE',list(ob.pose.bones[bn].head),flush=True)
 body=bpy.data.objects[name+'_SculptedBody'];ids=set(i for f in body.data.polygons for i in f.vertices);ps=np.array([body.data.vertices[i].co[:] for i in ids]);ez={'Gul':1.26204,'Kemal':1.371,'Ece':.86286}[name]
 for dz in (-.18,-.12,-.06,0,.06,.12):
  pts=ps[(abs(ps[:,2]-ez-dz)<.008)]
  print('SLICE',name,round(ez+dz,3),'COUNT',len(pts),'BOUNDS',np.round(pts.min(0),3).tolist(),np.round(pts.max(0),3).tolist(),flush=True)
  for dx in (.0,.10,.15,.18):
   near=pts[abs(abs(pts[:,0]-.02)-dx)<.008]
   if len(near):print(' X',dx,'YRANGE',np.round(np.percentile(near[:,1],[0,25,50,75,100]),3).tolist(),flush=True)
