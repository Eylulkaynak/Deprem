"""Same-camera reference / rejected / revised geometry review in Blender."""
import bpy,pathlib,math
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'ClientExports/YanYana/ArtReview/ResidentWorkshop'
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=True
sc.world=bpy.data.worlds.new('Reference lighting');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.7,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.4
ink=bpy.data.materials.new('Labels');ink.diffuse_color=(.03,.08,.07,1)
for i,(label,path) in enumerate([
    ('ONAYLI EMRE',ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.blend'),
    ('REDDEDİLEN EREN',ROOT/'.codex_tmp/rejected-wide-residents-20261006/Eren.blend'),
    ('DÜZELTİLEN EREN',ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop/Eren/Eren.blend')]):
    with bpy.data.libraries.load(str(path),link=False) as (src,dst):dst.objects=[n for n in src.objects]
    objs=[o for o in dst.objects if o and o.type in ('MESH','ARMATURE','FONT')]
    for obj in objs:sc.collection.objects.link(obj)
    roots=[o for o in objs if not o.parent]
    for obj in roots:obj.location.x+=(i-1)*.68
    data=bpy.data.curves.new(label,'FONT');data.body=label;data.align_x='CENTER';data.size=.031
    obj=bpy.data.objects.new(label,data);sc.collection.objects.link(obj);obj.location=((i-1)*.68,-.5,.94);obj.rotation_euler=(math.pi/2,0,0);data.materials.append(ink)
for name,pos,power,size in [('Key',(-3,-4,5),650,3),('Fill',(3,-3,3),220,3),('Rim',(1,3,4),250,3)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;obj=bpy.data.objects.new(name,d);sc.collection.objects.link(obj);obj.location=pos;obj.rotation_euler=(Vector((0,0,1.4))-obj.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Identical proportions review');cam=bpy.data.objects.new('Identical proportions review',d);sc.collection.objects.link(cam);sc.camera=cam;d.type='ORTHO';d.ortho_scale=2.05;cam.location=(0,-6,1.36);cam.rotation_euler=(Vector((0,0,1.36))-cam.location).to_track_quat('-Z','Y').to_euler()
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.render.resolution_x=1650;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/'Eren_Proportion_Comparison.png');bpy.ops.render.render(write_still=True)
