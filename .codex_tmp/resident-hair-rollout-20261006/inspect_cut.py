import bpy,pathlib,sys,numpy as np
from mathutils import Vector
sys.path.insert(0,str(pathlib.Path.cwd()/'Tools/YanYana'))
from author_resident_identity import reference
root=pathlib.Path.cwd()
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop/Gul/Gul.blend'))
ob=bpy.data.objects['Gul_SculptedBody'];ps,cs,cx,ez,s=reference('Derya');groups={g.index:g.name for g in ob.vertex_groups}
samples=[]
for poly in ob.data.polygons:
 p=sum((ps[i] for i in poly.vertices),Vector())/len(poly.vertices)
 if 1.0<p.z<1.16 and p.y>.03 and abs(p.x-cx)>.13:
  c=sum((cs[i] for i in poly.vertices),np.zeros(3))/len(poly.vertices)
  h=sum(q.weight for i in poly.vertices for q in ob.data.vertices[i].groups if groups[q.group]=='Head')/len(poly.vertices)
  samples.append((p[:],[round(float(x),3) for x in c],round(h,3)))
print('REMNANTS',len(samples),samples[::max(1,len(samples)//15)],flush=True)
hair=bpy.data.objects['Gul tailored hair silhouette']
print('HAIR_MESH',len(hair.data.vertices),len(hair.data.polygons),'smooth',sum(p.use_smooth for p in hair.data.polygons),'attrs',[a.name for a in hair.data.attributes],flush=True)
