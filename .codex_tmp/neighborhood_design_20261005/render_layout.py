# Independent scale/layout review; does not import assets or touch Unity.
import bpy,math,pathlib
from mathutils import Vector
stage=pathlib.Path(__file__).parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def color(name,c):
 m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=c;return m
stone=color('WarmLimestone',(.68,.63,.51,1));grass=color('OliveGround',(.43,.48,.31,1));path=color('Paving',(.76,.69,.56,1));teal=color('Canvas',(.13,.36,.33,1))
def p(v):return(-v[0],-v[2],v[1])
def box(name,at,size,mat):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p(at));o=bpy.context.object;o.name=name;o.scale=(size[0],size[2],size[1]);o.data.materials.append(mat);return o
def library(name):
 with bpy.data.libraries.load(str(stage/'Source/Environment'/f'{name}.blend'),link=False) as (source,destination):destination.objects=source.objects
 return [o for o in destination.objects if o and o.type=='MESH']
houses=[library(f'KKTC_House_{i}') for i in range(1,5)]
trees=[library('KKTC_OliveTree'),library('KKTC_CarobTree')]
def model(parts,name,at,yaw=0,scale=1):
 empty=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(empty);empty.location=p(at);empty.rotation_euler.z=math.radians(-yaw);empty.scale=(scale,)*3
 for prototype in parts:
  o=prototype.copy();o.data=prototype.data;bpy.context.collection.objects.link(o);o.parent=empty
 return empty
box('Ground',(0,-.72,-13),(110,.4,108),grass)
for name,at,size in [('Plaza',(0,-.437,-25.1),(26,.04,10.2)),('Main walk',(-4,-.46,-14.6),(4.8,.025,11.2)),('East walk',(6,-.46,-15.9),(3.6,.025,12.8)),('Back promenade',(0,-.47,-37.4),(39,.025,2.6)),('West sidewalk',(-15.25,-.47,-23.4),(2.5,.025,29)),('East sidewalk',(15.25,-.47,-23.4),(2.5,.025,29)),('Park connection',(2.9,-.44,-33),(2.6,.025,5.8))]:box(name,at,size,path)
idx=0
for x in (-18,-9,0,9,18):
 model(houses[idx%4],f'Full House {idx+1}',(x,-.48,-41.8));idx+=1
for side in (-1,1):
 for z in (-12,-22,-32):
  model(houses[idx%4],f'Full House {idx+1}',(side*18.2,-.48,z),90 if side<0 else 270);idx+=1
for i,at in enumerate([(-13.2,-.48,-21.4),(13.1,-.48,-23),(-12.5,-.48,-31.7),(-6.5,-.48,-34.1),(0,-.48,-34.5),(6.6,-.48,-34),(12.7,-.48,-32.3),(-10.4,-.48,-10.1),(11.6,-.48,-11),(-11.8,-.48,-17),(12.7,-.48,-17.3)]):model(trees[i%2],f'Olive/Carob {i+1}',at,i*37)
for x,z in ((0,-23.15),(3,-24.6),(6,-23.15)):
 box('Aid table',(x,.22,z),(1.6,.07,.78),stone)
 for dx in (-1.13,1.13):
  for dz in(-.73,.73):box('Canvas support',(x+dx,.72,z+dz),(.064,2.4,.064),stone)
 verts=[p((x-1.4,1.99,z-.87)),p((x+1.4,1.99,z-.87)),p((x+1.4,1.99,z+.87)),p((x-1.4,1.99,z+.87)),p((x,2.54,z))]
 mesh=bpy.data.meshes.new('Canopy');mesh.from_pydata(verts,[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]);mesh.update();o=bpy.data.objects.new('Aid canopy',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(teal)
 # A neutral 1.8 m silhouette makes the scale review explicit.
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.13,location=p((x,.48,z+1.6)));head=bpy.context.object;head.data.materials.append(stone)
 box('Adult 1.8m reference',(x,.17,z+1.6),(.32,1.3,.22),stone)
bpy.ops.object.camera_add(location=p((-25,20,-8)));camera=bpy.context.object;camera.rotation_euler=(Vector(p((0,1,-28)))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=56
scene=bpy.context.scene;scene.camera=camera
bpy.ops.object.light_add(type='SUN',location=p((-15,20,-10)));sun=bpy.context.object;sun.rotation_euler=(.5,-.55,-.5);sun.data.energy=2;sun.data.angle=.12
scene.world.color=(.65,.69,.65);scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.render.filepath=str(stage/'layout-scale-review.png');bpy.ops.render.render(write_still=True)
