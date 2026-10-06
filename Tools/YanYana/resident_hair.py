"""Separate, skinned hair volumes authored in Blender, with intact face indices."""
import bpy,bmesh,math,numpy as np
from mathutils import Vector,Matrix

def replace_eren_hair(body,rig,make_material):
    old=body.data
    # Keep the finished anatomical hairline and sideburns intact. Detach the
    # modeled crown as a real solid and change its comb direction rigidly, so
    # individual locks keep their volume instead of becoming flattened plates.
    crown=old.copy();crown.name='Eren swept crown geometry'
    bm=bmesh.new();bm.from_mesh(crown)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,1.545),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
    edges=[e for e in bm.edges if e.is_boundary]
    if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=0)
    bm.to_mesh(crown);bm.free()
    pivot=Vector((.018,.012,1.545));rotation=Matrix.Rotation(math.radians(-23),4,'Z')
    for vertex in crown.vertices:vertex.co=pivot+rotation@(vertex.co-pivot)
    hair=bpy.data.objects.new('Eren separate swept crown',crown);bpy.context.collection.objects.link(hair)
    for polygon in crown.polygons:polygon.use_smooth=True
    group=hair.vertex_groups.new(name='Head');group.add(list(range(len(crown.vertices))),1,'REPLACE');hair.parent=rig;modifier=hair.modifiers.new('Crown skin','ARMATURE');modifier.object=rig
    keep=[p for p in old.polygons if max(old.vertices[i].co.z for i in p.vertices)<1.565]
    weights=[[(g.group,g.weight) for g in v.groups] for v in old.vertices];group_names=[g.name for g in body.vertex_groups]
    keys=[(key.name,[v.co.copy() for v in key.data]) for key in old.shape_keys.key_blocks] if old.shape_keys else []
    mesh=bpy.data.meshes.new('Eren face with separate haircut');mesh.from_pydata([v.co for v in old.vertices],[],[tuple(p.vertices) for p in keep]);mesh.update()
    for material in old.materials:mesh.materials.append(material)
    uv=mesh.uv_layers.new(name='Approved face UV')
    for poly,source in zip(mesh.polygons,keep):
        poly.material_index=source.material_index;poly.use_smooth=True
        for i,j in zip(poly.loop_indices,source.loop_indices):uv.data[i].uv=old.uv_layers.active.data[j].uv
    body.data=mesh;body.vertex_groups.clear();groups=[body.vertex_groups.new(name=n) for n in group_names]
    for i,values in enumerate(weights):
        for index,value in values:groups[index].add([i],value,'REPLACE')
    for name,values in keys:
        key=body.shape_key_add(name=name);key.value=0
        for vertex,value in zip(key.data,values):vertex.co=value
    return hair
