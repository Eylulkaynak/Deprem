"""Render new UV-region maps from authored mesh vertex regions; no source bitmap editing."""
import bpy,json,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
out=root/'Assets/YanYana/Art/Textures';out.mkdir(exist_ok=True)
for path in (root/'ArtDirection/YanYana/Characters/Wardrobe').glob('*.mask.json'):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    data=json.loads(path.read_text(encoding='utf-8-sig'));mesh=bpy.data.meshes.new('AuthoredUVRegions')
    tris=data['triangles'];mesh.from_pydata([(v['x'],v['y'],0) for v in data['uv']],[],[tris[i:i+3] for i in range(0,len(tris),3)]);mesh.update()
    attribute=mesh.color_attributes.new(name='Regions',type='FLOAT_COLOR',domain='POINT')
    for point,color in zip(attribute.data,data['colors']):point.color=(color['r'],color['g'],0,1)
    obj=bpy.data.objects.new('Wardrobe regions from character geometry',mesh);bpy.context.collection.objects.link(obj)
    uv_layer=mesh.uv_layers.new(name='RegionsUV')
    for polygon in mesh.polygons:
        for index in polygon.loop_indices:
            uv=data['uv'][mesh.loops[index].vertex_index];uv_layer.data[index].uv=(uv['x'],uv['y'])
    material=bpy.data.materials.new('Unlit regions');material.use_nodes=True;nodes=material.node_tree.nodes;nodes.clear()
    source=nodes.new('ShaderNodeVertexColor');source.layer_name='Regions';emission=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
    material.node_tree.links.new(source.outputs['Color'],emission.inputs['Color']);material.node_tree.links.new(emission.outputs[0],output.inputs['Surface']);mesh.materials.append(material)
    bpy.ops.object.camera_add(location=(.5,.5,1));camera=bpy.context.object;camera.data.type='ORTHO';camera.data.ortho_scale=1;bpy.context.scene.camera=camera
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
    scene.world.color=(0,0,0);scene.view_settings.view_transform='Raw';scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.render.filepath=str(out/(path.name.replace('.mask.json','_WardrobeRegions.png')))
    image=bpy.data.images.new('Padded garment and hair regions',1024,1024,alpha=True);image.colorspace_settings.name='Non-Color'
    texture=nodes.new('ShaderNodeTexImage');texture.image=image;nodes.active=texture
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.bake(type='EMIT',margin=8,use_clear=True)
    image.filepath_raw=scene.render.filepath;image.file_format='PNG';image.save()
    bpy.ops.wm.save_as_mainfile(filepath=str(path.with_name(path.name.replace('.mask.json','_WardrobeRegions.blend'))))
    print('UV REGION RENDER',path.stem,flush=True)
