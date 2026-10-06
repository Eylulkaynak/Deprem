"""Use the project's existing UV region masks, then bake portable albedo in Blender."""
import bpy,pathlib,json,sys,math
ROOT=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import color

def bake_material(obj,name,out):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1
    image=bpy.data.images.new(name+' baked character albedo',2048,2048,alpha=True)
    for mat in obj.data.materials:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.nodes.active=node
    bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'},use_clear=True,margin=8)
    target=out/(name+'_Albedo.png');pending=out/(name+'_Albedo.writing.png')
    image.filepath_raw=str(pending);image.file_format='PNG';image.save()
    pending.replace(target);image.filepath_raw=str(target);image.pack()
    mat=bpy.data.materials.new(name+' portable approved palette');mat.use_nodes=True;node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
    shader=mat.node_tree.nodes['Principled BSDF'];shader.inputs['Roughness'].default_value=.72;mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color']);obj.data.materials.clear();obj.data.materials.append(mat)
    for poly in obj.data.polygons:poly.material_index=0

def approved_palette(obj,name,source,hex,out,grey=False):
    # The reviewed project's spatial masks isolate cloth and hair from skin and
    # eyes. Keep their feathered boundaries instead of painting whole triangles.
    template='Deniz' if source=='Emre' else 'Selma';folder=ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'
    group=json.loads((folder/(template+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]['groups'][0]
    mat=bpy.data.materials.new(name+' approved regional palette');mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links
    def bind(socket,v):
        if hasattr(v,'is_output'):links.new(v,socket)
        else:socket.default_value=v[:3] if socket.type=='VECTOR' and isinstance(v,tuple) else v
    def mathnode(op,a,b=None):
        n=nodes.new('ShaderNodeMath');n.operation=op;bind(n.inputs[0],a)
        if b is not None:bind(n.inputs[1],b)
        return n.outputs[0]
    def smooth(x,lo,hi):
        t=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('DIVIDE',mathnode('SUBTRACT',x,lo),hi-lo),0),1)
        return mathnode('MULTIPLY',mathnode('MULTIPLY',t,t),mathnode('SUBTRACT',3,mathnode('MULTIPLY',2,t)))
    def split(value):
        n=nodes.new('ShaderNodeSeparateColor');n.mode='RGB';bind(n.inputs[0],value);return n.outputs[0],n.outputs[1],n.outputs[2]
    def tex(path,data=False):
        n=nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(folder/path),check_existing=True)
        if data:n.image.colorspace_settings.name='Non-Color'
        return n.outputs['Color']
    def multiply(a,b):
        n=nodes.new('ShaderNodeVectorMath');n.operation='MULTIPLY';bind(n.inputs[0],a);bind(n.inputs[1],b);return n.outputs[0]
    def mix(f,a,b):
        n=nodes.new('ShaderNodeMixRGB');bind(n.inputs[0],f);bind(n.inputs[1],a);bind(n.inputs[2],b);return n.outputs[0]
    base=tex(group['texture']);regions=split(tex(group['regionTexture'],True));gamma=nodes.new('ShaderNodeGamma');bind(gamma.inputs[0],base);gamma.inputs[1].default_value=1/2.2;r,g,b=split(gamma.outputs[0])
    if source=='Derya':factor=mathnode('MULTIPLY',smooth(mathnode('DIVIDE',b,mathnode('MAXIMUM',g,.01)),.86,.905),smooth(mathnode('DIVIDE',r,mathnode('MAXIMUM',g,.01)),1.08,1.18))
    else:factor=mathnode('MULTIPLY',smooth(mathnode('SUBTRACT',b,r),.005,.025),smooth(mathnode('SUBTRACT',b,g),.005,.025))
    lr,lg,lb=split(base);lum=mathnode('ADD',mathnode('ADD',mathnode('MULTIPLY',lr,.2126),mathnode('MULTIPLY',lg,.7152)),mathnode('MULTIPLY',lb,.0722));shade=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('SQRT',mathnode('DIVIDE',lum,.24 if source=='Derya' else .016)),.35),1.45)
    cloth_region=regions[0]
    lower_hair=None
    if source=='Derya':
        # The inherited UV cloth mask includes a few low hair tips. Skeletal
        # head weights protect the entire original hair surface while baking.
        def field(name,values):
            attr=obj.data.attributes.get(name) or obj.data.attributes.new(name=name,type='FLOAT',domain='POINT')
            for point,value in zip(attr.data,values):point.value=value
            node=nodes.new('ShaderNodeAttribute');node.attribute_name=name
            return node.outputs['Fac']
        groups={group.index:group.name for group in obj.vertex_groups}
        head=[sum(w.weight for w in vertex.groups if groups[w.group]=='Head') for vertex in obj.data.vertices]
        head_field=field(name+'_head_region',head)
        cloth_region=mathnode('MULTIPLY',cloth_region,mathnode('SUBTRACT',1,smooth(head_field,.25,.65)))
        def ramp(value,lo,hi):
            t=max(0,min(1,(value-lo)/(hi-lo)));return t*t*(3-2*t)
        tips=[]
        for vertex,weight in zip(obj.data.vertices,head):
            x,y,z=vertex.co
            sides=max(ramp(abs(x-.018),.095,.125),ramp(y,-.13,-.085))
            tips.append(sides*ramp(z,1.03,1.11)*(1-ramp(z,1.44,1.49)))
        lower_hair=field(name+'_lower_hair_region',tips)
        # Low wave tips inherit Chest weights in the source. Protect their
        # dark source color by position as well as bone influence.
        dark_hair=mathnode('MULTIPLY',lower_hair,mathnode('SUBTRACT',1,smooth(r,.40,.58)))
        cloth_region=mathnode('MULTIPLY',cloth_region,mathnode('SUBTRACT',1,dark_hair))
    result=mix(mathnode('MULTIPLY',factor,cloth_region),base,multiply(color(hex),shade))
    if grey:
        hair_region=mathnode('MAXIMUM',regions[1],lower_hair) if lower_hair is not None else regions[1]
        factor=mathnode('MULTIPLY',hair_region,mathnode('SUBTRACT',1,smooth(r,.44,.58)));shade=mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('SQRT',mathnode('DIVIDE',lum,.055)),.24),1.25);result=mix(factor,result,multiply(color('#A3A29A'),shade))
    shader=nodes['Principled BSDF'];bind(shader.inputs['Base Color'],result);shader.inputs['Roughness'].default_value=.72;obj.data.materials.clear();obj.data.materials.append(mat)
    for poly in obj.data.polygons:poly.material_index=0
    bake_material(obj,name,out)
