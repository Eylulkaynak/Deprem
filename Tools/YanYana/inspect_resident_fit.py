import bpy,pathlib,json
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop/Eren/Eren.blend'))
body=bpy.data.objects['Eren_SculptedBody'];rig=bpy.data.objects['Eren_Rig']
print('MATRIX',body.matrix_world,rig.matrix_world)
print('POSE',[(b.name,tuple(round(x,4) for row in b.matrix_basis for x in row)) for b in rig.pose.bones if max(abs((b.matrix_basis-Matrix.Identity(4))[i][j]) for i in range(4) for j in range(4))>.0001])
print('GROUPS',[(g.index,g.name) for g in body.vertex_groups])
deps=bpy.context.evaluated_depsgraph_get();me=body.evaluated_get(deps).to_mesh();bvh=BVHTree.FromPolygons([v.co for v in me.vertices],[tuple(p.vertices) for p in me.polygons])
for z in (.65,.75,.85,.95,1.05,1.15):
    hit=bvh.ray_cast(Vector((.018,-1,z)),Vector((0,1,0)))
    print('BODY FRONT',z,hit[0])
for o in bpy.context.scene.objects:
    if 'vest panel' not in o.name:continue
    m=o.evaluated_get(deps).to_mesh();b=BVHTree.FromPolygons([v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons])
    for z in (.65,.75,.85,.95,1.05,1.15):
        hit=b.ray_cast(Vector((.090,-1,z)),Vector((0,1,0)));bhit=bvh.ray_cast(Vector((.090,-1,z)),Vector((0,1,0)))
        print('PANEL',o.name,z,hit[0],'BODY',bhit[0])
