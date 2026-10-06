"""Native Blender actions, portable FBX and a read-back validation report.
This is offline authoring; all output remains outside Unity Assets.
"""
import bpy,pathlib,json,sys,math,argparse,re
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2];WORK=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop';OUT=WORK/'ReviewPack'
KEYS=('Mouth_A','Mouth_E','Mouth_O','Blink','Fear','Surprise','Smile')

def clip_curves():
    speech={k:[] for k in ('Mouth_A','Mouth_E','Mouth_O')}
    for frame in range(1,50):
        t=(frame-1)/24;gate=min(1,(frame-1)/4,(49-frame)/4);amplitude=max(0,math.sin(t*math.pi*6.3))*.67*gate
        mix=(.5+.5*math.sin(t*math.pi*1.4));a=amplitude*(.25+.75*mix);o=amplitude*(1-mix)*.68;e=max(0,math.sin(t*math.pi*8.2))*.25*gate
        for k,v in zip(speech,(a,e,o)):speech[k].append([frame,round(v,5)])
    return {'Talk':dict(frames=49,loop=True,curves=speech),
      'Blink':dict(frames=7,loop=False,curves={'Blink':[[1,0],[3,.35],[4,1],[5,.75],[7,0]]}),
      'Fear':dict(frames=37,loop=False,curves={'Fear':[[1,0],[8,.85],[26,.85],[37,0]]}),
      'Surprise':dict(frames=33,loop=False,curves={'Surprise':[[1,0],[6,.9],[19,.9],[33,0]]}),
      'Smile':dict(frames=37,loop=False,curves={'Smile':[[1,0],[10,.80],[26,.80],[37,0]]})}

def author_action(obj,name,clip):
    keys=obj.data.shape_keys;keys.animation_data_clear()
    for key in keys.key_blocks:key.value=0
    for k,points in clip['curves'].items():
        for frame,value in points:keys.key_blocks[k].value=value;keys.key_blocks[k].keyframe_insert(data_path='value',frame=frame)
    action=keys.animation_data.action;action.name=name;action.use_fake_user=True
    for layer in action.layers:
        for strip in layer.strips:
            if not hasattr(strip,'channelbags'):continue
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    for p in curve.keyframe_points:p.interpolation='LINEAR'
    keys.animation_data_clear()
    for key in keys.key_blocks:key.value=0

def prepare(name):
    folder=OUT/name;folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(WORK/name/(name+'_Expressions.blend')));bpy.context.preferences.filepaths.save_version=0
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');face=bpy.data.objects[name+'_SculptedBody']
    objects=list(bpy.context.scene.objects)
    face.data.calc_loop_triangles()
    source_faces=[tuple(p.vertices) for p in face.data.loop_triangles]
    body_bvh=BVHTree.FromPolygons([v.co for v in face.data.vertices],source_faces)
    # Convert the modeled badge lettering and skin it to the chest so it follows
    # the vest during animation, then bake authoring-only garment modifiers.
    for obj in objects:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        if obj.type=='FONT':
            bpy.ops.object.convert(target='MESH');obj=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
            group=obj.vertex_groups.new(name='Chest');group.add(list(range(len(obj.data.vertices))),1,'REPLACE');mod=obj.modifiers.new('Badge skin','ARMATURE');mod.object=rig
        if obj.type!='MESH':continue
        if name=='Eren' and obj!=face:
            # Fit the vest's motion to the actual sweater weights; a height-only
            # Chest/Spine blend otherwise lets the shoulder cut through the vest.
            groups={g.name:obj.vertex_groups.get(g.name) or obj.vertex_groups.new(name=g.name) for g in face.vertex_groups}
            for vertex in obj.data.vertices:
                loc,normal,poly_index,distance=body_bvh.find_nearest(vertex.co)
                indices=source_faces[poly_index]
                a,b,c=(face.data.vertices[index].co for index in indices)
                u=b-a;v=c-a;p=loc-a
                d00=u.dot(u);d01=u.dot(v);d11=v.dot(v);den=d00*d11-d01*d01
                if abs(den)<1e-16:fractions=(1,0,0)
                else:
                    beta=(d11*p.dot(u)-d01*p.dot(v))/den;gamma=(d00*p.dot(v)-d01*p.dot(u))/den
                    fractions=(max(0,1-beta-gamma),max(0,beta),max(0,gamma))
                weighted={};total=sum(fractions)
                for index,factor in zip(indices,fractions):
                    donor=face.data.vertices[index]
                    for weight in donor.groups:
                        bone=face.vertex_groups[weight.group].name;weighted[bone]=weighted.get(bone,0)+weight.weight*factor
                for group in obj.vertex_groups:group.remove([vertex.index])
                for bone,weight in weighted.items():groups[bone].add([vertex.index],weight/total,'REPLACE')
        if obj!=face:
            # Correct the inherited family-body/accessory winding. New modeled
            # garments and hair already use outward Blender triangle winding.
            for polygon in obj.data.polygons:
                material=obj.data.materials[polygon.material_index]
                if re.match(r'^(Ada|Efe|Emre|Derya|Yusuf)_\d+_\d+',material.name) or 'portable approved palette' in material.name:
                    polygon.flip()
            obj.data.update()
        for mod in list(obj.modifiers):
            if mod.type!='ARMATURE':bpy.ops.object.modifier_apply(modifier=mod.name)
        # Head replacement preserves source indices until facial authoring is
        # complete. Remove those now-unused vertices across all shape keys.
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.delete_loose(use_verts=True,use_edges=True,use_faces=False);bpy.ops.object.mode_set(mode='OBJECT')
    # A fitted skull patch belongs to the facial surface. Transfer the actual
    # exported facial deformations onto it and join it to the same renderer,
    # so raised brows and jaw movement cannot reopen the scalp/cheek boundary.
    closures=[o for o in bpy.context.scene.objects if o.type=='MESH' and (o.name.startswith(name+' fitted back of head') or (o.name.startswith(name+' tailored hair silhouette') and o.data.attributes.get('ForeheadBlend')))]
    if closures:
        face.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in face.data.loop_triangles]
        basis=face.data.shape_keys.key_blocks[0]
        tree=BVHTree.FromPolygons([p.co for p in basis.data],triangles)
        keys=list(face.data.shape_keys.key_blocks)
        for closure in closures:
            mapping=[]
            for vertex in closure.data.vertices:
                hit,normal,index,distance=tree.find_nearest(vertex.co);ids=triangles[index]
                a,b,c=(basis.data[i].co for i in ids);u=b-a;v=c-a;p=hit-a
                uu=u.dot(u);uv=u.dot(v);vv=v.dot(v);den=uu*vv-uv*uv
                if abs(den)<1e-15:fractions=(1.,0.,0.)
                else:
                    beta=(vv*p.dot(u)-uv*p.dot(v))/den;gamma=(uu*p.dot(v)-uv*p.dot(u))/den
                    fractions=(max(0,1-beta-gamma),max(0,beta),max(0,gamma))
                total=sum(fractions);amount=max(0,min(1,(.10-distance)/.08))
                forehead=closure.data.attributes.get('ForeheadBlend')
                if forehead:amount*=forehead.data[vertex.index].value
                mapping.append((ids,[f/total for f in fractions],amount))
            cb=closure.shape_key_add(name=keys[0].name);cb.value=0
            for source_key in keys[1:]:
                key=closure.shape_key_add(name=source_key.name);key.value=0
                for vertex,(ids,factors,amount) in zip(key.data,mapping):
                    delta=sum(((source_key.data[i].co-basis.data[i].co)*f for i,f in zip(ids,factors)),Vector())
                    vertex.co+=delta*amount
            if closure.data.uv_layers.active:closure.data.uv_layers.active.name=face.data.uv_layers.active.name
        bpy.ops.object.select_all(action='DESELECT');face.select_set(True)
        for closure in closures:closure.select_set(True)
        bpy.context.view_layer.objects.active=face;bpy.ops.object.join()
    bpy.ops.object.select_all(action='DESELECT')
    wardrobe=[o for o in bpy.context.scene.objects if o.type=='MESH' and o!=face]
    if wardrobe:
        for obj in wardrobe:obj.select_set(True)
        bpy.context.view_layer.objects.active=wardrobe[0]
        if len(wardrobe)>1:bpy.ops.object.join()
        wardrobe[0].name=name+'_Wardrobe'
    bpy.context.view_layer.update();images={};materials=[]
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        for material in obj.data.materials:
            if not material or material.name in [m['name'] for m in materials]:continue
            p=material.node_tree.nodes.get('Principled BSDF');record={'name':material.name,'base_color_linear':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'albedo':None}
            for node in material.node_tree.nodes:
                if node.type!='TEX_IMAGE' or not node.image:continue
                original=node.image
                if original.name not in images:
                    pixels=list(original.pixels);image=original.copy()
                    if image.packed_file:image.unpack(method='REMOVE')
                    image.pixels=pixels;path=folder/'Textures'/(str(len(images)+1).zfill(2)+'_Albedo.png');path.parent.mkdir(exist_ok=True);image.filepath_raw=str(path);image.file_format='PNG';image.save();image.pack();images[original.name]=image
                node.image=images[original.name];record['albedo']='Textures/'+pathlib.Path(node.image.filepath_raw).name
            if record['albedo']:record['base_color_linear']=[1,1,1,1]
            materials.append(record)
    # Normalize any inherited weights explicitly; every exported vertex must be
    # skinned, and clothing does not rely on runtime attachment code.
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        for v in obj.data.vertices:
            weights=[(q.group,q.weight) for q in v.groups if q.weight>0];total=sum(w for _,w in weights)
            if not weights:raise RuntimeError(name+' contains an unskinned vertex on '+obj.name)
            if abs(total-1)>.00001:
                for index,w in weights:obj.vertex_groups[index].add([v.index],w/total,'REPLACE')
        # Every renderer uses its actual rig, including garments joined above.
        arms=[m for m in obj.modifiers if m.type=='ARMATURE']
        if not arms:mod=obj.modifiers.new('Rig skin','ARMATURE');mod.object=rig
    clips=clip_curves()
    for key,clip in clips.items():author_action(face,name+'_'+key,clip)
    for k in face.data.shape_keys.key_blocks:k.value=0
    sc=bpy.context.scene;sc.render.fps=24;sc.frame_start=1;sc.frame_end=49;sc.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':obj.select_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(name+'.blend')))
    fbx=folder/(name+'_Rigged.fbx')
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',path_mode='COPY',embed_textures=True,mesh_smooth_type='FACE')
    manifest={'name':name,'source_anatomy':{'Eren':'Emre','Ece':'Ada + Efe child body','Aylin':'Ada','Zeynep':'Derya','Deniz':'Emre','Gul':'Derya','Kemal':'Emre, independently designed face silhouette and hair','Arda':'Efe + Emre child body'}[name],
       'fps':24,'rig':rig.name,'facial_mesh':face.name,'blendshapes':list(KEYS),'native_actions':list(clips),'clips':clips,'materials':materials,'hair_reference':face.get('hair_reference','approved source hair'),'identity_design':face.get('identity_design',''),
       'runtime_scripts_added':False,'integrated_into_unity':False,'production_ready':False,'status':'offline_export_validation'}
    (folder/'character.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
    # Read the exported artifact back in a clean Blender file. This catches missing
    # shape keys, missing skins, and exporter omissions rather than only checking
    # the editable source scene.
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx),use_anim=False)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];faces=[o for o in meshes if o.data.shape_keys and all(k in o.data.shape_keys.key_blocks for k in KEYS)]
    if len(rigs)!=1 or len(faces)!=1:raise RuntimeError('FBX read-back omitted required rig/facial keys: '+name)
    invalid=[]
    for o in meshes:
        for v in o.data.vertices:
            total=sum(q.weight for q in v.groups)
            if abs(total-1)>.002:invalid.append([o.name,v.index,total])
    if invalid:raise RuntimeError('Invalid skin weights after FBX read-back: '+str(invalid[:4]))
    report={'name':name,'fbx_read_back_passed':True,'armatures':len(rigs),'bones':len(rigs[0].data.bones),'renderers':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'facial_keys_verified':list(KEYS),'unweighted_vertices':0,'material_count':len(materials),'unity_play_tested':False}
    (folder/'validation.json').write_text(json.dumps(report,indent=2));print('PACKAGED_RESIDENT',json.dumps(report),flush=True)
    return report

def main():
    p=argparse.ArgumentParser();p.add_argument('--names',nargs='+',default=['Eren','Ece','Aylin','Zeynep']);args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    results=[prepare(n) for n in args.names];(OUT/'validation-latest.json').write_text(json.dumps(results,indent=2))

if __name__=='__main__':main()
