import bpy,pathlib,sys
root=pathlib.Path.cwd();sys.path.insert(0,str(root/'Tools/YanYana'))
from author_resident_identity import remove_inherited_hair,mesh
from close_resident_head import close_head
for name,source in [('Gul','Derya'),('Kemal','Emre'),('Ece','Ada')]:
 path=root/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name/(name+'.blend')
 bpy.ops.wm.open_mainfile(filepath=str(path))
 body=bpy.data.objects[name+'_SculptedBody'];rig=body.parent
 eye,*_=remove_inherited_hair(body,source,name)
 for obj in list(bpy.context.scene.objects):
  if obj.name.startswith(name+' fitted back of head'):bpy.data.objects.remove(obj,do_unlink=True)
 close_head(body,rig,name,eye,mesh)
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
