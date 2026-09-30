"""Read-only Blender neutral/arm-pose checks for all thirteen editable character files."""
import bpy,json,pathlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
folder=root/'ArtDirection/YanYana/Characters/ApprovedStyle';results=[]
for path in sorted(folder.glob('*.mesh.json')):
    data=json.loads(path.read_text(encoding='utf-8-sig'));bpy.ops.wm.open_mainfile(filepath=str(folder/(data['name']+'.blend')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');surface=max((o for o in bpy.context.scene.objects if o.type=='MESH'),key=lambda o:len(o.data.vertices))
    graph=bpy.context.evaluated_depsgraph_get();graph.update();evaluated=surface.evaluated_get(graph);mesh=evaluated.to_mesh()
    rest=[v.co.copy() for v in mesh.vertices];error=max((a-b.co).length for a,b in zip(rest,surface.data.vertices));evaluated.to_mesh_clear()
    movements={}
    for name,alias in [('LeftUpperArm','LUpperArm'),('RightUpperArm','RUpperArm'),('RightLowerArm','RLowerArm')]:
        bone=rig.pose.bones.get(name) or rig.pose.bones.get(alias)
        if bone is None:raise RuntimeError(data['name']+' is missing '+name)
        bone.rotation_mode='XYZ';bone.rotation_euler.x=.30;graph.update()
        evaluated=surface.evaluated_get(graph);mesh=evaluated.to_mesh();movements[name]=max((v.co-rest[i]).length for i,v in enumerate(mesh.vertices));evaluated.to_mesh_clear()
        bone.rotation_euler.x=0;graph.update()
    shapes={}
    if data['name'] in ('Ada','Efe','Idil'):
        if not surface.data.shape_keys:raise RuntimeError(data['name']+' lost editable hand shapes')
        for key in list(surface.data.shape_keys.key_blocks)[1:]:
            key.value=1;graph.update();evaluated=surface.evaluated_get(graph);mesh=evaluated.to_mesh();shapes[key.name]=max((v.co-rest[i]).length for i,v in enumerate(mesh.vertices));evaluated.to_mesh_clear();key.value=0;graph.update()
        if len(shapes)!=8 or min(shapes.values())<.001:raise RuntimeError(data['name']+' has missing or empty hand shapes')
    results.append(dict(character=data['name'],neutralDeltaMetres=error,armPoseMovementMetres=movements,handShapeMovementMetres=shapes,armatures=sum(o.type=='ARMATURE' for o in bpy.context.scene.objects),passed=error<.0001 and min(movements.values())>.02))
    print(data['name'],'neutral',error,'arm deformation',movements,flush=True)
out=root/'ClientExports/YanYana/Reports/editable-rig-pose.json';out.write_text(json.dumps(results,indent=2),encoding='utf-8')
if not all(row['passed'] for row in results):raise RuntimeError('Editable rig pose verification failed')
