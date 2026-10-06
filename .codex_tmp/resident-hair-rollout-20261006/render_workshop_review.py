"""A native Blender face review, using the exact exported resident sculptures."""
import bpy,math,pathlib,sys,argparse,json,re
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
PACK=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop/ReviewPack'
OUT=ROOT/'ClientExports/YanYana/ArtReview/ResidentWorkshop'
NAMES=['Eren','Deniz','Aylin','Zeynep','Gul','Kemal','Ece','Arda']
KEYS=['Mouth_A','Mouth_E','Mouth_O','Blink','Fear','Surprise','Smile']

def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.85
    return m

def label(text,position,size,mat):
    data=bpy.data.curves.new(text,'FONT');data.body=text;data.align_x='CENTER';data.size=size
    obj=bpy.data.objects.new(text,data);bpy.context.collection.objects.link(obj);obj.location=position;obj.rotation_euler=(math.pi/2,0,0);data.materials.append(mat)
    return obj

def main():
    p=argparse.ArgumentParser();p.add_argument('--movie',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'Frames').mkdir(exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;bpy.context.preferences.filepaths.save_version=0
    sc.render.engine='CYCLES';sc.cycles.samples=12;sc.cycles.use_denoising=True
    sc.world=bpy.data.worlds.new('Soft studio');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.66,.73,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.5
    cream=material('Review background',(.70,.76,.71));ink=material('Review type',(.027,.065,.066))
    for location,scale in [((0,1.3,1), (4,.03,3)),((0,0,-.3),(4,.03,2.3))]:
        bpy.ops.mesh.primitive_cube_add(size=1,location=location);board=bpy.context.object;board.scale=scale;board.data.materials.append(cream)
    faces=[]
    for i,name in enumerate(NAMES):
        with bpy.data.libraries.load(str(PACK/name/(name+'.blend')),link=False) as (source,target):target.objects=source.objects
        objs=[o for o in target.objects if o]
        for obj in objs:sc.collection.objects.link(obj)
        rig=next(o for o in objs if o.type=='ARMATURE');face=next(o for o in objs if o.type=='MESH' and o.data.shape_keys and 'Mouth_A' in o.data.shape_keys.key_blocks)
        manifest=json.loads((PACK/name/'character.json').read_text(encoding='utf-8'))
        portable={}
        for entry in manifest['materials']:
            m=material(name+' review '+entry['name'],entry['base_color_linear'][:3]);p=m.node_tree.nodes['Principled BSDF'];p.inputs['Roughness'].default_value=entry['roughness']
            if entry.get('albedo'):
                node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(PACK/name/entry['albedo']),check_existing=False);m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
            portable[re.sub(r'(\.\d{3})+$','',entry['name'])]=m
        for obj in objs:
            if obj.type=='MESH':
                for slot in obj.material_slots:
                    token=re.sub(r'(\.\d{3})+$','',slot.material.name)
                    if token not in portable:raise RuntimeError(name+' unmapped material '+slot.material.name)
                    slot.material=portable[token]
        keys=face.data.shape_keys;keys.animation_data_clear();base=keys.key_blocks[0];blink=keys.key_blocks['Blink'];weights=[(a.co-b.co).length for a,b in zip(base.data,blink.data)]
        eyez=sum(v.co.z*w for v,w in zip(base.data,weights))/sum(weights)
        col=i%4;row=i//4;x=(col-1.5)*.79;rig.location=(x,.5 if row==0 else -.5,(1.36 if row==0 else .41)-eyez)
        label('Gül' if name=='Gul' else name,(x,-1.03,.91 if row==0 else -.045),.059,ink)
        faces.append(face)
    state=label('YÜZ HAREKETLERİ · BLENDER',(0,-1.03,1.92),.043,ink)
    for name,pos,power,size in [('Key',(-3,-4,5),700,4),('Fill',(3,-3,3),250,4),('Rim',(0,3,5),350,3)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;obj=bpy.data.objects.new(name,d);sc.collection.objects.link(obj);obj.location=pos;obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Face review');cam=bpy.data.objects.new('Face review',data);sc.collection.objects.link(cam);sc.camera=cam;data.type='ORTHO';data.ortho_scale=3.34;cam.location=(0,-8,.94);cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.render.resolution_x=1600;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.fps=12;sc.frame_start=1;sc.frame_end=96
    for frame in range(1,97):
        t=(frame-1)/12
        for i,face in enumerate(faces):
            values={k:0 for k in KEYS}
            if .15<t<.55:values['Blink']=max(0,1-abs(t-.34)/.14)
            if 1<t<3.5:
                gate=min(1,(t-1)*5,(3.5-t)*5);phase=t+i*.07
                values['Mouth_A']=max(0,math.sin(phase*math.pi*6.3))*.8*gate
                values['Mouth_E']=max(0,math.sin(phase*math.pi*8.2))*.24*gate
                values['Mouth_O']=max(0,math.sin(phase*math.pi*3.1))*.4*gate
            if 3.6<t<4.7:values['Surprise']=min(1,(t-3.6)*5,(4.7-t)*5)*.8
            if 4.8<t<6.2:values['Fear']=min(1,(t-4.8)*5,(6.2-t)*5)*.78
            if 6.3<t:values['Smile']=min(1,(t-6.3)*4)*.8
            for key,value in values.items():face.data.shape_keys.key_blocks[key].value=value;face.data.shape_keys.key_blocks[key].keyframe_insert('value',frame=frame)
    sc.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'EightResidents_Expressions.blend'))
    sc.render.filepath=str(OUT/'Blender_Faces.png');bpy.ops.render.render(write_still=True)
    if args.movie:
        for frame in range(1,97):
            sc.frame_set(frame);sc.render.filepath=str(OUT/'Frames'/f'{frame:04d}.png');bpy.ops.render.render(write_still=True)
            print('REVIEW_FRAME',frame,flush=True)

if __name__=='__main__':main()
