"""Fit individually sculpted cuts from the family's detailed hair surfaces.

Only hair is sampled; faces, eyes, rigs, and clothes are never transplanted.
All production and baking happen offline in Blender.
"""
import bpy,json,pathlib,math,numpy as np
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
REF=ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'

def smooth(x):
    t=max(0,min(1,x));return t*t*(3-2*t)

def make_reference_hair(body,rig,name,eye):
    source={'Ece':'Derya','Gul':'Ada','Kemal':'Yusuf'}[name]
    data=json.loads((REF/(source+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
    group=data['groups'][0];points=[Vector((p['x'],-p['z'],p['y'])) for p in data['vertices']]
    uvs=[Vector((p['x'],p['y'])) for p in data['uv']]
    tex=bpy.data.images.load(str(REF/group['texture']),check_existing=True)
    pix=np.array(tex.pixels[:],dtype=np.float32).reshape(tex.size[1],tex.size[0],4)
    colors=[pix[int(max(0,min(1,p.y))*(tex.size[1]-1)),int(max(0,min(1,p.x))*(tex.size[0]-1)),:3] for p in uvs]
    trace=json.loads((ROOT/'.codex_tmp'/(source.lower()+'-mouth-trace.json')).read_text())
    sx=sum(e['center'][0] for e in trace['eyes'])/2;sz=sum(e['center'][1] for e in trace['eyes'])/2
    sy=sum(sum(e['bounds'][2])/2 for e in trace['eyes'])/2
    faces=[]
    indices=group['indices']
    for j in range(0,len(indices),3):
        ids=indices[j:j+3];p=sum((points[i] for i in ids),Vector())/3;c=sum((colors[i] for i in ids),np.zeros(3))/3
        dx=abs(p.x-sx);dz=p.z-sz
        if dz<(-.31 if source=='Derya' else -.085):continue
        if -.140<dz<.045 and .125<dx<.235 and -.165<p.y<.055:continue
        front_face=p.y<-.12 and dx<.133 and dz<.085
        skin=c.mean()>.59 or (c[0]>.60 and c[1]>.34 and c[2]>.25)
        if dz<.085 and c[0]>.55 and c.mean()>.40:continue
        if dz<-.085 and c[2]>c[1]*.99:continue
        # Keep the complete upper scalp, including its real hair/skin edge.
        # Colour-cutting this border makes white shards and open triangles.
        region=dz>.112 or ((dz>.075 or p.y>-.040 or dx>.130) and not skin)
        if region and not front_face:faces.append(tuple(reversed(ids)))
    used=sorted(set(i for f in faces for i in f));mapping={old:i for i,old in enumerate(used)}
    def fit(p):
        dx=p.x-sx;dy=p.y-sy;dz=p.z-sz
        if name=='Ece':
            # Compact jaw-length waves, with the long adult curls turned inward.
            width=.81-.13*smooth(-dz/.24)
            x=eye.x+dx*width
            y=eye.y+dy*.83
            z=eye.z+(.090+(dz-.112)*.75 if dz>0 else .006+dz*.43)
            y-=.026*smooth(-dz/.21)*smooth((p.y+.02)/.16)
        elif name=='Gul':
            # Remove the tall bun silhouette by laying its curls along the
            # rear crown; retain the small interlocking frontal waves.
            x=eye.x-dx*1.11
            y=eye.y+dy*1.00
            height=.090+(dz-.112)*.94
            bun=smooth((dz-.20)/.04)*smooth((p.y+.13)/.08)
            z=eye.z+height*(1-bun)+(.1727-.030*smooth((dz-.20)/.07))*bun
            y-=.018*smooth((dz-.20)/.10)
            x+=.020*smooth((dz-.15)/.15)
        else:
            # Receding compact side part, mirrored from the reference flow.
            x=eye.x-dx*.90
            y=eye.y+dy*.95
            z=eye.z+(.090+(dz-.112)*.70 if dz>0 else .0116+dz*.85)
            y+=.020*smooth((-p.y-.15)/.10)*smooth((dz-.12)/.08)
        return Vector((x,y,z))
    verts=[fit(points[i]) for i in used]
    # Blend the lower forehead strip into the recipient's own skin surface.
    # The original eyes, brows, nose, and mouth stay on the recipient mesh.
    from mathutils.bvhtree import BVHTree
    body.data.calc_loop_triangles();body_triangles=[tuple(t.vertices) for t in body.data.loop_triangles]
    body_tree=BVHTree.FromPolygons([v.co for v in body.data.vertices],body_triangles)
    body_uv={l.vertex_index:body.data.uv_layers.active.data[l.index].uv.copy() for l in body.data.loops}
    body_image=next(n.image for m in body.data.materials for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image)
    body_pixels=np.array(body_image.pixels[:],dtype=np.float32).reshape(body_image.size[1],body_image.size[0],4)
    skin_samples=[];forehead_points=[]
    for vertex in body.data.vertices:
        if vertex.index not in body_uv:continue
        p=vertex.co;uv=body_uv[vertex.index]
        c=body_pixels[int(uv.y*(body_image.size[1]-1)),int(uv.x*(body_image.size[0]-1)),:3]
        if p.y<-.15 and abs(p.x-eye.x)<.145 and c[0]>.65 and c[1]>.40 and c[0]>c[1]*1.08 and c[1]>c[2]*1.08:
            if eye.z-.08<p.z<eye.z+.105:skin_samples.append(c)
            if eye.z+.035<p.z<eye.z+.104:forehead_points.append(p.copy())
    matched_skin=np.median(skin_samples,axis=0)
    fpoints=np.array([p[:] for p in forehead_points]);dx=fpoints[:,0]-eye.x;dz=fpoints[:,2]-eye.z-.090
    design=np.column_stack((np.ones(len(dx)),dx,dx*dx,dz))
    curve=np.linalg.lstsq(design,fpoints[:,1],rcond=None)[0]
    skin_weights=[];skin_colors=[];skin_normals=[]
    for q,old in zip(verts,used):
        p=points[old];c=colors[old];factor=0;shade=matched_skin;normal=Vector((0,-1,0))
        if p.y<-.145 and abs(p.x-sx)<.16 and p.z>sz+.095 and c[0]>.60 and c[1]>.34:
            factor=1-smooth((q.z-eye.z-.090)/.030)
            if forehead_points:
                dx=q.x-eye.x;dz=q.z-eye.z-.090;target=curve[0]+curve[1]*dx+curve[2]*dx*dx+curve[3]*dz
                q.y=q.y*(1-factor)+(target+.006)*factor
                normal=Vector(((q.x-eye.x)*1.4,-1,.04)).normalized()
            else:factor=0
        skin_weights.append(factor if c[0]>.60 and c[1]>.34 else 0)
        skin_colors.append(tuple(float(x/12.92 if x<.04045 else ((x+.055)/1.055)**2.4) for x in shade)+(1,))
        skin_normals.append(normal)
    # A mirrored sculpt needs its winding reversed once more.
    reverse=name in ('Gul','Kemal')
    polys=[tuple(mapping[i] for i in (reversed(f) if reverse else f)) for f in faces]
    mesh=bpy.data.meshes.new(name+' shaped reference hair');mesh.from_pydata(verts,[],polys);mesh.update()
    ob=bpy.data.objects.new(name+' tailored hair silhouette',mesh);bpy.context.collection.objects.link(ob);ob.parent=rig
    uv=mesh.uv_layers.new(name='SculptedHairUV')
    for loop in mesh.loops:uv.data[loop.index].uv=uvs[used[loop.vertex_index]]
    for face in mesh.polygons:face.use_smooth=True
    mesh.update()
    # UV seams split vertices in the source asset. They must still shade as
    # one continuous sculpt after the haircut is fitted to a different head.
    _,weld=np.unique(np.round(np.array([v.co[:] for v in mesh.vertices]),5),axis=0,return_inverse=True)
    normals=np.zeros((int(weld.max())+1,3));np.add.at(normals,weld,np.array([v.normal[:] for v in mesh.vertices]))
    normals/=np.maximum(np.linalg.norm(normals,axis=1)[:,None],1e-12)
    smooth_normals=[Vector(n).lerp(skin_normals[i],skin_weights[i]).normalized() for i,n in enumerate(normals[weld])]
    mesh.normals_split_custom_set_from_vertices(smooth_normals)
    skin_attr=mesh.color_attributes.new(name='RecipientForehead',type='FLOAT_COLOR',domain='POINT')
    blend_attr=mesh.attributes.new(name='ForeheadBlend',type='FLOAT',domain='POINT')
    for i in range(len(verts)):skin_attr.data[i].color=skin_colors[i];blend_attr.data[i].value=skin_weights[i]
    mat=bpy.data.materials.new(name+' reference hair shading');mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;shader=nodes['Principled BSDF'];shader.inputs['Roughness'].default_value=.65
    image=nodes.new('ShaderNodeTexImage');image.image=tex
    if name=='Ece':links.new(image.outputs['Color'],shader.inputs['Base Color'])
    else:
        lum=nodes.new('ShaderNodeRGBToBW');links.new(image.outputs['Color'],lum.inputs[0])
        ratio=nodes.new('ShaderNodeMath');ratio.operation='DIVIDE';links.new(lum.outputs[0],ratio.inputs[0]);ratio.inputs[1].default_value=.055
        shade=nodes.new('ShaderNodeMath');shade.operation='POWER';links.new(ratio.outputs[0],shade.inputs[0]);shade.inputs[1].default_value=.53
        low=nodes.new('ShaderNodeMath');low.operation='MAXIMUM';links.new(shade.outputs[0],low.inputs[0]);low.inputs[1].default_value=.36
        high=nodes.new('ShaderNodeMath');high.operation='MINIMUM';links.new(low.outputs[0],high.inputs[0]);high.inputs[1].default_value=1.35
        tint=nodes.new('ShaderNodeVectorMath');tint.operation='MULTIPLY';tint.inputs[0].default_value=(.39,.38,.35) if name=='Gul' else (.23,.235,.23);links.new(high.outputs[0],tint.inputs[1])
        rgb=nodes.new('ShaderNodeSeparateColor');links.new(image.outputs['Color'],rgb.inputs[0])
        mask=nodes.new('ShaderNodeMapRange');mask.clamp=True;mask.interpolation_type='SMOOTHSTEP';links.new(rgb.outputs[0],mask.inputs['Value']);mask.inputs['From Min'].default_value=.18;mask.inputs['From Max'].default_value=.43;mask.inputs['To Min'].default_value=1;mask.inputs['To Max'].default_value=0
        mix=nodes.new('ShaderNodeMixRGB');links.new(mask.outputs[0],mix.inputs[0]);links.new(image.outputs['Color'],mix.inputs[1]);links.new(tint.outputs['Vector'],mix.inputs[2]);links.new(mix.outputs[0],shader.inputs['Base Color'])
    previous=shader.inputs['Base Color'].links[0].from_socket
    skin_node=nodes.new('ShaderNodeVertexColor');skin_node.layer_name='RecipientForehead'
    blend_node=nodes.new('ShaderNodeAttribute');blend_node.attribute_name='ForeheadBlend'
    join=nodes.new('ShaderNodeMixRGB');links.new(blend_node.outputs['Fac'],join.inputs[0]);links.new(previous,join.inputs[1]);links.new(skin_node.outputs['Color'],join.inputs[2]);links.new(join.outputs[0],shader.inputs['Base Color'])
    mesh.materials.append(mat)
    from resident_materials import bake_material
    bake_material(ob,name+'_SculptedHair',ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name)
    mesh.materials[0].name=name+' fitted sculpted hair albedo'
    group=ob.vertex_groups.new(name='Head');group.add(list(range(len(verts))),1,'REPLACE')
    solid=ob.modifiers.new('Closed hair edges','SOLIDIFY');solid.thickness=.0035;solid.offset=-1
    bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=solid.name)
    mesh=ob.data
    fixed=[n.vector.copy() for n in mesh.corner_normals]
    for face in mesh.polygons:
        for li in face.loop_indices:
            if fixed[li].dot(face.normal)<0:fixed[li]=-fixed[li]
    mesh.normals_split_custom_set(fixed)
    arm=ob.modifiers.new('Head skin','ARMATURE');arm.object=rig
    body['hair_reference']='Detailed approved-family '+source+' hair surface; independently cut and fitted for '+name+'.'
    body['identity_design']={'Ece':'jaw-length inward waves and a broad side part','Gul':'short interlocking silver curls without the tall bun','Kemal':'compact mirrored silver side part with a receding forehead'}[name]
    print('REFERENCE_HAIR',name,source,len(mesh.vertices),len(mesh.polygons),flush=True)
    return ob
