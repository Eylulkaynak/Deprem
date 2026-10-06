import bpy,pathlib
root=pathlib.Path.cwd()/'ArtDirection/YanYana/Characters/ResidentWorkshop/Kemal'
bpy.context.preferences.filepaths.save_version=0
for suffix in ('','_Expressions'):
 path=root/('Kemal'+suffix+'.blend');bpy.ops.wm.open_mainfile(filepath=str(path))
 ob=bpy.data.objects['Kemal finished crew neck'];p=ob.data.polygons[0]
 if p.normal.y<0:
  for face in ob.data.polygons:face.flip()
  ob.data.update();bpy.ops.wm.save_as_mainfile(filepath=str(path));print('COLLAR_WINDING_FIXED',suffix,flush=True)
