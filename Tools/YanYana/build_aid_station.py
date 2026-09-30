"""Editable original aid canopy, folding desk and family sign. Dimensions in metres."""
import importlib.util,pathlib,bpy,math
spec=importlib.util.spec_from_file_location('yy',pathlib.Path(__file__).with_name('build_original_art.py'));a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
a.reset()
for x in (-1.0,1.0):
 for y in (-.65,.65):
  a.tube('KatlanirAyak',(x,y,0),(x,y,2.05),.035,'cream')
  a.box('AyakTabani',(x,y,.025),(.18,.18,.05),'tealDark',.025)
# Four fabric roof triangles, with rounded seam rails instead of a solid cube roof.
verts=[(-1.15,-.80,2.02),(1.15,-.80,2.02),(1.15,.80,2.02),(-1.15,.80,2.02),(0,0,2.65)]
mesh=bpy.data.meshes.new('DikisliBez');mesh.from_pydata(verts,[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]);mesh.update()
roof=bpy.data.objects.new('YumusakTente',mesh);bpy.context.collection.objects.link(roof);roof.data.materials.append(a.mat('teal'))
solid=roof.modifiers.new('BezKalinligi','SOLIDIFY');solid.thickness=.025
for i in range(4):a.tube('TenteDikisi',verts[i],verts[4],.017,'cream')
a.box('MasaTablasi',(0,0,.78),(1.65,.78,.07),'wood',.055)
for x in (-.65,.65):
 for y in (-.25,.25):a.tube('MasaAyagi',(x,y,.05),(-x,y,.73),.027,'tealDark')
a.box('MasaOnEtiketi',(0,-.405,.55),(.85,.035,.35),'cream',.045)
a.export('AidStation','Environment')
print('ORIGINAL AID STATION EXPORTED',flush=True)
