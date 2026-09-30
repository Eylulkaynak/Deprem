"""Editable approved characters, complete wardrobe, one matching armature per character."""
import bpy,json,pathlib,math
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]/'ArtDirection/YanYana/Characters/ApprovedStyle'
bpy.context.preferences.filepaths.save_version=0
def p(v):return(v['x'],-v['z'],v['y'])
def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def rgb(c):return tuple(linear(c[k]) for k in ('r','g','b'))+(c.get('a',1),)
def material(name,group):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.72
    def bind(socket,value):
        if hasattr(value,'is_output'):links.new(value,socket)
        else:socket.default_value=value[:3] if socket.type=='VECTOR' and isinstance(value,tuple) else value
    def mathnode(op,x,y=None):
        n=nodes.new('ShaderNodeMath');n.operation=op;bind(n.inputs[0],x)
        if y is not None:bind(n.inputs[1],y)
        return n.outputs[0]
    def smooth(x,lo,hi):
        t=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('DIVIDE',mathnode('SUBTRACT',x,lo),hi-lo),0),1)
        return mathnode('MULTIPLY',mathnode('MULTIPLY',t,t),mathnode('SUBTRACT',3,mathnode('MULTIPLY',2,t)))
    def vector(op,x,y):
        n=nodes.new('ShaderNodeVectorMath');n.operation=op;bind(n.inputs[0],x);bind(n.inputs[1],y);return n.outputs[0]
    def split(value):
        n=nodes.new('ShaderNodeSeparateColor');n.mode='RGB';bind(n.inputs['Color'],value);return n.outputs[0],n.outputs[1],n.outputs[2]
    def texture(path,data=False):
        n=nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(root/path),check_existing=True)
        if data:n.image.colorspace_settings.name='Non-Color'
        return n.outputs['Color']
    def mix(f,a,b):
        n=nodes.new('ShaderNodeMixRGB');bind(n.inputs[0],f);bind(n.inputs[1],a);bind(n.inputs[2],b);return n.outputs[0]
    color=texture(group['texture']) if group.get('texture') else rgb(group['color'])
    if group.get('regionTexture'):
        regions=split(texture(group['regionTexture'],True));gamma=nodes.new('ShaderNodeGamma');bind(gamma.inputs[0],color);gamma.inputs[1].default_value=1/2.2;r,g,b=split(gamma.outputs[0])
        female=group['wardrobeMode']>.5
        if female:factor=mathnode('MULTIPLY',smooth(mathnode('DIVIDE',b,mathnode('MAXIMUM',g,.01)),.86,.905),smooth(mathnode('DIVIDE',r,mathnode('MAXIMUM',g,.01)),1.08,1.18))
        else:factor=mathnode('MULTIPLY',smooth(mathnode('SUBTRACT',b,r),.005,.025),smooth(mathnode('SUBTRACT',b,g),.005,.025))
        lr,lg,lb=split(color);lum=mathnode('ADD',mathnode('ADD',mathnode('MULTIPLY',lr,.2126),mathnode('MULTIPLY',lg,.7152)),mathnode('MULTIPLY',lb,.0722))
        shade=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('SQRT',mathnode('DIVIDE',lum,.24 if female else .016)),.35),1.45)
        colored=vector('MULTIPLY',rgb(group['wardrobeColor']),shade)
        color=mix(mathnode('MULTIPLY',factor,regions[0]),color,colored)
        if group['greyAmount']>.01:
            grey=mathnode('MULTIPLY',regions[1],mathnode('SUBTRACT',1,smooth(r,.44,.58)))
            shade=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('SQRT',mathnode('DIVIDE',lum,.055)),.24),1.25)
            color=mix(grey,color,vector('MULTIPLY',rgb(group['greyColor']),shade))
    bind(bsdf.inputs['Base Color'],color);return mat

for path in sorted(root.glob('*.mesh.json')):
    data=json.loads(path.read_text(encoding='utf-8-sig'));bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    arm=bpy.data.armatures.new(data['name']+'_EditableRig');rig=bpy.data.objects.new(data['name']+'_Rig',arm);bpy.context.collection.objects.link(rig);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    bones=data['surfaces'][0]['bones'];created=[]
    for i,bone in enumerate(bones):
        edit=arm.edit_bones.new(bone['name']);edit.head=p(bone['point']);child=next((b for b in bones if b['parent']==i),None);edit.tail=p(child['point']) if child else Vector(edit.head)+Vector((0,0,.05))
        if (edit.tail-edit.head).length<.005:edit.tail=Vector(edit.head)+Vector((0,0,.05))
        created.append(edit)
    for i,bone in enumerate(bones):
        if bone['parent']>=0:created[i].parent=created[bone['parent']]
    bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
    for si,surface in enumerate(data['surfaces']):
        mesh=bpy.data.meshes.new(surface.get('name') or data['name']+'_Surface');faces=[];slots=[]
        for mi,group in enumerate(surface['groups']):
            indices=group['indices']
            for i in range(0,len(indices),3):faces.append((indices[i+2],indices[i+1],indices[i]));slots.append(mi)
        mesh.from_pydata([p(v) for v in surface['vertices']],[],faces);mesh.update();obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj)
        if surface.get('shapes'):
            basis=obj.shape_key_add(name='Basis')
            for shape in surface['shapes']:
                key=obj.shape_key_add(name=shape['name']);key.value=0
                for i,delta in enumerate(shape['deltas']):key.data[i].co=basis.data[i].co+Vector(p(delta))
        if surface['uv']:
            uv=mesh.uv_layers.new(name='OriginalUV')
            for poly in mesh.polygons:
                for li in poly.loop_indices:
                    point=surface['uv'][mesh.loops[li].vertex_index];uv.data[li].uv=(point['x'],point['y'])
        for mi,group in enumerate(surface['groups']):mesh.materials.append(material(data['name']+'_'+str(si)+'_'+str(mi),group))
        for poly,slot in zip(mesh.polygons,slots):poly.material_index=slot;poly.use_smooth=True
        groups=[obj.vertex_groups.new(name=b['name']) for b in bones]
        for vi,weight in enumerate(surface['weights']):
            for index,w in zip(weight['bones'],weight['values']):
                if w>0:groups[index].add([vi],w,'REPLACE')
        mod=obj.modifiers.new('Editable skin weights','ARMATURE');mod.object=rig;obj.parent=rig
    bpy.context.scene.unit_settings.system='METRIC';bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(root/(data['name']+'.blend')));print('COMPLETE EDITABLE CHARACTER',data['name'],len(data['surfaces']),'surfaces',flush=True)
