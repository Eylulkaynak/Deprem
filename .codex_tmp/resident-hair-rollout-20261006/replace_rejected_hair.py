import bpy,pathlib,sys
root=pathlib.Path.cwd();sys.path.insert(0,str(root/'Tools/YanYana'))
from author_resident_identity import remove_inherited_hair
from reference_resident_hair import make_reference_hair
for name,source in [('Kemal','Emre'),('Ece','Ada'),('Gul','Derya')]:
 path=root/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name/(name+'.blend')
 bpy.ops.wm.open_mainfile(filepath=str(path));body=bpy.data.objects[name+'_SculptedBody'];rig=body.parent
 eye,*_=remove_inherited_hair(body,source,name)
 for ob in list(bpy.context.scene.objects):
  if ob.name.startswith(name+' tailored hair silhouette'):bpy.data.objects.remove(ob,do_unlink=True)
 make_reference_hair(body,rig,name,eye)
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
