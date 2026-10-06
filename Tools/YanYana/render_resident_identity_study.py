"""Same-lighting Blender portraits for the three redesigned residents."""
import bpy,pathlib,math,json,bmesh,re
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
WORK=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop'
OUT=ROOT/'ClientExports/YanYana/ArtReview/ResidentWorkshop'
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=20;sc.cycles.use_denoising=True
sc.world=bpy.data.worlds.new('Warm reference lighting');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.7,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.4
ink=bpy.data.materials.new('Study labels');ink.use_nodes=True;ink.diffuse_color=(.035,.08,.07,1);ink.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.035,.08,.07,1)
for i,name in enumerate(('Kemal','Ece','Gul')):
    with bpy.data.libraries.load(str(WORK/name/(name+'.blend')),link=False) as (src,dst):dst.objects=src.objects
    objs=[o for o in dst.objects if o and o.type in ('MESH','ARMATURE','FONT')]
    for obj in objs:sc.collection.objects.link(obj)
    # Source scans use inward winding; the expression/export pipeline repairs
    # this. Apply that same correction in this source-only preview.
    for obj in objs:
        if obj.type!='MESH':continue
        inherited=obj.name.startswith(name+'_SculptedBody') or obj.name.startswith(name+'_ChildClothing')
        if inherited:
            bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
    rig=next(o for o in objs if o.type=='ARMATURE')
    hair=next((o for o in objs if o.name.startswith(name+' tailored hair silhouette')),None)
    body=next(o for o in objs if o.name==name+'_SculptedBody')
    top=max(v.co.z for v in hair.data.vertices) if hair else body['hair_top']
    rig.location=Vector(((i-1)*.69,0,1.69-top))
    data=bpy.data.curves.new(name,'FONT');data.body='Gül' if name=='Gul' else name;data.align_x='CENTER';data.size=.038
    ob=bpy.data.objects.new(name+' label',data);sc.collection.objects.link(ob);ob.location=((i-1)*.69,-.70,.98);ob.rotation_euler=(math.pi/2,0,0);data.materials.append(ink)
for name,pos,power,size in [('Key',(-3,-4,5),650,3),('Fill',(3,-3,3),220,3),('Rim',(1,3,4),250,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size;ob=bpy.data.objects.new(name,data);sc.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((0,0,1.4))-ob.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Identity review');cam=bpy.data.objects.new('Identity review',data);sc.collection.objects.link(cam);sc.camera=cam;data.type='ORTHO';data.ortho_scale=2.10;cam.location=(0,-6,1.31);cam.rotation_euler=(Vector((0,0,1.31))-cam.location).to_track_quat('-Z','Y').to_euler()
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.render.resolution_x=1650;sc.render.resolution_y=850;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/'Three_Resident_Identities.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
bpy.ops.render.render(write_still=True)
