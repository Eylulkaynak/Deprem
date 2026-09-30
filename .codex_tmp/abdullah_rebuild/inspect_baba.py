import bpy
from mathutils import Vector

FBX_PATH = r"C:\Users\Gokturk\Documents\GitHub\Deprem\ArtDirection\CharacterModels\MeshyFamily\Baba\BlenderRigged\Baba_Rigged.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX_PATH, use_anim=False)

print("=== OBJECTS ===")
for obj in bpy.context.scene.objects:
    print(f"{obj.name!r} type={obj.type} parent={obj.parent.name if obj.parent else None!r}")
    if obj.type == "MESH":
        world = obj.matrix_world
        corners = [world @ Vector(corner) for corner in obj.bound_box]
        mins = tuple(min(v[i] for v in corners) for i in range(3))
        maxs = tuple(max(v[i] for v in corners) for i in range(3))
        print(
            f"  verts={len(obj.data.vertices)} polys={len(obj.data.polygons)} "
            f"materials={[slot.material.name if slot.material else None for slot in obj.material_slots]} "
            f"bounds={mins}..{maxs}"
        )
        print(f"  groups={[g.name for g in obj.vertex_groups][:40]}")
        for modifier in obj.modifiers:
            print(f"  modifier={modifier.name!r} type={modifier.type}")
    elif obj.type == "ARMATURE":
        print(f"  bones={[bone.name for bone in obj.data.bones]}")
        for bone in obj.data.bones:
            print(f"  bone {bone.name}: head={tuple(round(v, 3) for v in bone.head_local)} tail={tuple(round(v, 3) for v in bone.tail_local)}")

mesh_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
print("=== WEIGHTED BOUNDS ===")
for group_name in ("Head", "Neck", "Chest", "Spine", "Hips", "LeftHand", "RightHand"):
    group = mesh_obj.vertex_groups.get(group_name)
    if not group:
        continue
    points = []
    for vertex in mesh_obj.data.vertices:
        try:
            if group.weight(vertex.index) > 0.45:
                points.append(mesh_obj.matrix_world @ vertex.co)
        except RuntimeError:
            pass
    mins = tuple(min(v[i] for v in points) for i in range(3)) if points else None
    maxs = tuple(max(v[i] for v in points) for i in range(3)) if points else None
    print(f"{group_name}: count={len(points)} bounds={mins}..{maxs}")

print("=== CONNECTED COMPONENTS ===")
adj = [[] for _ in mesh_obj.data.vertices]
for edge in mesh_obj.data.edges:
    a, b = edge.vertices
    adj[a].append(b)
    adj[b].append(a)
seen = set()
components = []
for start in range(len(adj)):
    if start in seen:
        continue
    stack = [start]
    seen.add(start)
    indices = []
    while stack:
        current = stack.pop()
        indices.append(current)
        for neighbour in adj[current]:
            if neighbour not in seen:
                seen.add(neighbour)
                stack.append(neighbour)
    points = [mesh_obj.matrix_world @ mesh_obj.data.vertices[index].co for index in indices]
    mins = tuple(min(v[i] for v in points) for i in range(3))
    maxs = tuple(max(v[i] for v in points) for i in range(3))
    components.append((len(indices), mins, maxs))
for count, mins, maxs in sorted(components, reverse=True)[:30]:
    print(f"component verts={count} bounds={tuple(round(v, 3) for v in mins)}..{tuple(round(v, 3) for v in maxs)}")

print("=== MATERIALS ===")
for mat in bpy.data.materials:
    print(f"{mat.name!r} nodes={mat.use_nodes}")
    if mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type == "TEX_IMAGE":
                print(f"  image={node.image.filepath if node.image else None!r}")
