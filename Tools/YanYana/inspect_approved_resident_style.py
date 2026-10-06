"""Read the approved Blender model and render a proportional reference."""
import bpy,json,pathlib,sys
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ApprovedStyle/Efe.blend'))
report=[]
for o in bpy.context.scene.objects:
    item={'name':o.name,'type':o.type,'location':list(o.location),'scale':list(o.scale)}
    if o.type=='MESH':
        item['vertices']=len(o.data.vertices);item['materials']=[m.name for m in o.data.materials];item['bounds']=[list(o.matrix_world@Vector(c)) for c in o.bound_box]
        item['groups']=[g.name for g in o.vertex_groups]
        item['shapes']=list(o.data.shape_keys.key_blocks.keys()) if o.data.shape_keys else []
    elif o.type=='ARMATURE':item['bones']={b.name:{'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones}
    report.append(item)
(root/'.codex_tmp/approved-resident-style.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
sys.path.insert(0,str(root/'Tools/YanYana'))
from build_original_residents import stage
objs=[o for o in bpy.context.scene.objects if o.type=='MESH'];rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
camera=stage(objs[0],rig);sc=bpy.context.scene
camera.data.ortho_scale=1.55;camera.location=(0,-4,.8);camera.rotation_euler=(Vector((0,0,.63))-camera.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=700;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.filepath=str(root/'.codex_tmp/approved-efe-blender.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report,ensure_ascii=False))
