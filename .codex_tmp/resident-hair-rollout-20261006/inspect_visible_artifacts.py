import bpy,pathlib
from mathutils import Vector
root=pathlib.Path.cwd()
bpy.ops.wm.open_mainfile(filepath=str(root/'ClientExports/YanYana/ArtReview/ResidentWorkshop/EightResidents_Expressions.blend'))
sc=bpy.context.scene;deps=bpy.context.evaluated_depsgraph_get();cam=sc.camera
for x,y in [(580,697),(582,706),(577,689),(597,681),(643,682),(186,806),(188,815),(191,821),(1248,395),(1259,401),(1290,411)]:
 origin=Vector(((x/1600-.5)*3.34,-8,.94+(.5-y/1000)*3.34*1000/1600))
 ok,loc,normal,fi,ob,matrix=sc.ray_cast(deps,origin,Vector((0,1,0)))
 if ok:
  evaluated=ob.evaluated_get(deps);poly=evaluated.data.polygons[fi]
  print('PIXEL',x,y,'OBJECT',ob.name,'MATERIAL',evaluated.data.materials[poly.material_index].name,'LOCAL',list(matrix.inverted()@loc),'FACE',fi,flush=True)
