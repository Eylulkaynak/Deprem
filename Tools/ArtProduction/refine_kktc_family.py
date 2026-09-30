"""Refine existing approved family geometry while preserving identity, UVs and rig.
Editor/offline production only. Does not edit original source assets.
"""
import argparse
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

p=argparse.ArgumentParser()
p.add_argument('--project',required=True)
p.add_argument('--roles',default='Deniz,Can,Anne,Baba,Komsu,Police,Firefighter,RescueWorker')
o=p.parse_args(sys.argv[sys.argv.index('--')+1:])
project=Path(o.project)
output=project/'Assets/Story/Art/KKTC/Characters'
sources=project/'ArtDirection/KKTC/Characters'
output.mkdir(parents=True,exist_ok=True)
sources.mkdir(parents=True,exist_ok=True)
report=[]

for role in o.roles.split(','):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    original=project/f'Assets/Story/Characters/MeshyFamily/{role}/{role}_Rigged.fbx'
    if not original.exists(): original=project/f'Assets/Story/Characters/MeshyResponders/{role}/{role}.fbx'
    bpy.ops.import_scene.fbx(filepath=str(original),use_anim=False,automatic_bone_orientation=False)
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    armatures=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    if not meshes or not armatures:
        raise RuntimeError(f'{role}: source must contain weighted mesh and armature')
    triangles_before=0
    for mesh in meshes:
        triangles_before+=sum(len(f.vertices)-2 for f in mesh.data.polygons)
        bpy.context.view_layer.objects.active=mesh
        bpy.ops.object.select_all(action='DESELECT')
        mesh.select_set(True)
        # A light surface relaxation removes the image-reconstruction ripples without
        # remeshing hands or changing the approved face silhouette/UV correspondence.
        # UV seam vertices must move together, otherwise smoothing separates the
        # hair and clothes into visible cracks under directional lighting.
        seams={}
        local_extent=max(max(v.co[axis] for v in mesh.data.vertices)-min(v.co[axis] for v in mesh.data.vertices) for axis in range(3))
        quant=max(local_extent*1e-6,1e-7)
        for vertex in mesh.data.vertices:
            key=tuple(round(float(c)/quant) for c in vertex.co)
            seams.setdefault(key,[]).append(vertex.index)
        polish=mesh.modifiers.new('Surface polish','SMOOTH')
        polish.factor=.12
        polish.iterations=3
        bpy.ops.object.modifier_apply(modifier=polish.name)
        for indices in seams.values():
            if len(indices)<2: continue
            average=sum((mesh.data.vertices[i].co for i in indices),Vector())/len(indices)
            for i in indices: mesh.data.vertices[i].co=average
        mesh.data.update()
        for face in mesh.data.polygons:
            face.use_smooth=True
        # Bind weights remain normalized and limited to the mobile skinning budget.
        if mesh.vertex_groups:
            bpy.ops.object.vertex_group_limit_total(limit=4)
            bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    for rig in armatures:
        rig.data.pose_position='REST'
    base_color=original.parent/(role+'_BaseColor.png')
    normal_path=original.parent/(role+'_Normal.png')
    if base_color.exists():
        material=bpy.data.materials.new(role+'_KKTC_Editable')
        material.use_nodes=True
        nodes=material.node_tree.nodes;links=material.node_tree.links
        shader=nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.73
        texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(base_color),check_existing=True)
        links.new(texture.outputs['Color'],shader.inputs['Base Color'])
        if normal_path.exists():
            texture_normal=nodes.new('ShaderNodeTexImage');texture_normal.image=bpy.data.images.load(str(normal_path),check_existing=True)
            texture_normal.image.colorspace_settings.name='Non-Color'
            normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.28
            links.new(texture_normal.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
        for mesh in meshes:
            mesh.data.materials.clear();mesh.data.materials.append(material)
        bpy.ops.file.pack_all()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes+armatures: obj.select_set(True)
    bpy.context.view_layer.objects.active=armatures[0]
    bpy.ops.wm.save_as_mainfile(filepath=str(sources/(role+'_Refined.blend')))
    bpy.ops.export_scene.fbx(filepath=str(output/(role+'_Rigged.fbx')),use_selection=True,
        object_types={'ARMATURE','MESH','EMPTY'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,
        use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
    report.append({'role':role,'triangles':triangles_before,'meshes':len(meshes),
                   'bones':sum(len(a.data.bones) for a in armatures),
                   'source':str(original.relative_to(project)),
                   'treatment':'surface relaxation 0.12 x 3; smooth normals; four normalized influences; UVs retained'})
    print('KKTC_FAMILY_REFINED',role,triangles_before,flush=True)
(sources/'RefinementReport.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
