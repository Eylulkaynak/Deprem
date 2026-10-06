import bpy,sys,pathlib,json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from author_resident_identity import reference
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'.codex_tmp/resident-hair-rollout-20261006/identity-review.blend'))
sc=bpy.context.scene;cam=sc.camera;deps=bpy.context.evaluated_depsgraph_get()
refs={'Gul':reference('Derya'),'Ece':reference('Ada')}
for x,y in [(1291,498),(1484,479),(1250,451),(1461,450),(1264,438),(982,221),(692,224),(958,207)]:
    p=cam.matrix_world@Vector(((x/1650-.5)*2.10,(.5-y/850)*(2.10*850/1650),0))
    hit,loc,n,idx,obj,matrix=sc.ray_cast(deps,p,cam.matrix_world.to_quaternion()@Vector((0,0,-1)))
    row={'pixel':[x,y],'hit':hit}
    if hit:
        row.update(object=obj.name,location=list(loc),face=idx)
        if 'SculptedBody' in obj.name:
            name=obj.name.split('_')[0];poly=obj.data.polygons[idx];points,colors,cx,ez,scale=refs[name]
            row['source']=[sum(points[i][k] for i in poly.vertices)/len(poly.vertices) for k in range(3)]
            row['color']=[sum(float(colors[i][k]) for i in poly.vertices)/len(poly.vertices) for k in range(3)]
            row['current']=[sum(obj.data.vertices[i].co[k] for i in poly.vertices)/len(poly.vertices) for k in range(3)]
    print('PROBE',json.dumps(row),flush=True)
