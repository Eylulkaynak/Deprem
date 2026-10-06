"""Read approved Blender sources and record anatomical dimensions, without edits."""
import bpy,json,pathlib
root=pathlib.Path(__file__).resolve().parents[2];result={}
for name in ['Efe','Ada','Derya','Emre']:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ApprovedStyle'/f'{name}.blend'))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    data={'meshes':[],'rigs':[]}
    for o in meshes:
        data['meshes'].append({'name':o.name,'verts':len(o.data.vertices),'bounds':[(min(v.co[k] for v in o.data.vertices),max(v.co[k] for v in o.data.vertices)) for k in range(3)],'matrix':[list(r) for r in o.matrix_world],'materials':[m.name for m in o.data.materials]})
    for r in rigs:
        data['rigs'].append({'name':r.name,'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local)} for b in r.data.bones if b.name in ['Head','Neck','Chest','Hips','LeftUpperArm','LeftHand']}})
    result[name]=data
(root/'.codex_tmp/character-anatomy.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
