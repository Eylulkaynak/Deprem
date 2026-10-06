"""Resculpt the continuous approved head surface without opening facial seams."""
import bpy,bmesh,math,numpy as np
from mathutils import Vector

def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)

def sculpt_connected_hair(body,rig,name,source,eye):
    from author_resident_identity import reference
    original,colors,cx,ez,scale=reference(source)
    data=body.data;current=[v.co.copy() for v in data.vertices]
    coords=np.round(np.array([p[:] for p in original]),5)
    unique,weld=np.unique(coords,axis=0,return_inverse=True)
    values=[];pinned=[]
    for p,c in zip(original,colors):
        dx=abs(p.x-cx);dz=(p.z-ez)/scale
        brown=c[0]>c[1]*1.05 and c[2]<c[1]*.98 and c.mean()<.58
        scalp=dz>.115 or (p.y>-.125*scale and dz>-.32) or dx>.135*scale
        face=p.y<-.13*scale and dx<.135*scale and dz<.105
        hair=float(brown and scalp and dz>-.36 and not face)
        # The unambiguously high crown is hair even on its light highlights.
        if dz>.16:hair=1
        values.append(hair);pinned.append(face or dz<-.38)
    v=np.bincount(weld,weights=values)/np.maximum(1,np.bincount(weld))
    locked=np.bincount(weld,weights=pinned)>0
    edges=np.array([(weld[e.vertices[0]],weld[e.vertices[1]]) for e in data.edges]);a,b=edges.T
    count=np.bincount(np.r_[a,b],minlength=len(v))
    for _ in range(18):
        total=np.bincount(np.r_[a,b],weights=np.r_[v[b],v[a]],minlength=len(v))
        v=.25*v+.75*total/np.maximum(1,count);v[locked]=0
    weights=v[weld]
    # Preserve the actual source bun as a detailed second pigtail. Its lower
    # boundary is buried in the retained crown, never on exposed facial skin.
    source_uv={l.vertex_index:data.uv_layers.active.data[l.index].uv.copy() for l in data.loops}
    bun_center=Vector((eye.x,.060,eye.z+.267))
    def bun_fit(p,sign):
        q=bun_center+(p-bun_center)*.63
        return q+Vector((sign*.105,-.004,-.037))
    if name=='Ece':
        faces=[]
        for face in data.polygons:
            p=sum((original[i] for i in face.vertices),Vector())/len(face.vertices)
            if p.z>ez+.226*scale and p.y>-.074*scale:faces.append(tuple(face.vertices))
        used=sorted({i for f in faces for i in f});mapping={old:i for i,old in enumerate(used)}
        mesh=bpy.data.meshes.new('Ece right sculpted curls')
        mesh.from_pydata([bun_fit(current[i],1) for i in used],[],[tuple(mapping[i] for i in reversed(f)) for f in faces]);mesh.update()
        ob=bpy.data.objects.new('Ece right curled pigtail',mesh);bpy.context.collection.objects.link(ob);ob.parent=rig
        uv=mesh.uv_layers.new(name='OriginalUV')
        for loop in mesh.loops:uv.data[loop.index].uv=source_uv[used[loop.vertex_index]]
        material=data.materials[0].copy();material.name='Ece second pigtail painted curls';mesh.materials.append(material)
        # Close the extracted knot inside the crown. Weld only the duplicated
        # knot, preserving the original head indices used by facial animation.
        cap=bpy.data.materials.new('Ece hidden pigtail underside');cap.use_nodes=True
        cap.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.080,.047,.034,1)
        cap.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.72
        mesh.materials.append(cap)
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00002)
        boundaries=[edge for edge in bm.edges if edge.is_boundary]
        if boundaries:
            for face in bmesh.ops.holes_fill(bm,edges=boundaries,sides=0)['faces']:face.material_index=1
        bm.to_mesh(mesh);bm.free();mesh.update()
        for face in mesh.polygons:face.use_smooth=True
        group=ob.vertex_groups.new(name='Head');group.add(list(range(len(mesh.vertices))),1,'REPLACE');mod=ob.modifiers.new('Head skin','ARMATURE');mod.object=rig
    def deform(p,index):
        q=p.copy();s=original[index];dz=(s.z-ez)/scale;amount=float(weights[index])
        if name=='Ece':
            bun=smooth((dz-.160)/.065)*smooth((s.y+.120*scale)/(.080*scale))
            target=bun_fit(p,-1)
            return p.lerp(target,bun)
        if name=='Gul':
            low=smooth((.095-dz)/.22)
            q.x=eye.x+(p.x-eye.x)*(1-.25*low)
            if p.z<eye.z+.050:q.z=eye.z+.050+(p.z-eye.z-.050)*.52
            q.y=-.060+(p.y+.060)*(1-.16*low)
        else:
            top=smooth((dz-.090)/.18)
            q.z=eye.z+.080+(p.z-eye.z-.080)*(1-.33*top)
            q.y+=.042*top;q.x-=.020*top
        return p.lerp(q,amount)
    if data.shape_keys:
        for key in data.shape_keys.key_blocks:
            for i,vertex in enumerate(key.data):vertex.co=deform(vertex.co,i)
            key.value=0
        for vertex,basis in zip(data.vertices,data.shape_keys.key_blocks[0].data):vertex.co=basis.co
    else:
        for i,vertex in enumerate(data.vertices):vertex.co=deform(vertex.co,i)
    data.update()
    normal_values=np.array([vertex.normal[:] for vertex in data.vertices]);normal_sum=np.zeros((len(unique),3));np.add.at(normal_sum,weld,normal_values)
    normal_sum/=np.maximum(np.linalg.norm(normal_sum,axis=1)[:,None],1e-12)
    attr=data.attributes.get('uncut_surface_normal') or data.attributes.new(name='uncut_surface_normal',type='FLOAT_VECTOR',domain='POINT')
    for entry,n in zip(attr.data,normal_sum[weld]):entry.vector=n
    body['hair_top']=max(vertex.co.z for vertex in data.vertices)
    if name=='Ece':body['hair_top']=max(body['hair_top'],max(v.co.z for v in ob.data.vertices))
    body['hair_reference']=source+'.blend; connected detailed hair resculpt, preserved texture and continuous facial surface.'
    body['identity_design']={'Ece':'two small curled pigtails and round child proportions','Gul':'short inward silver waves and a mature oval face','Kemal':'low swept-back silver hair, long face and broad nose'}[name]
    print('CONNECTED_HAIR',name,'top',body['hair_top'],'hair_vertices',int(sum(weights>.15)),flush=True)
