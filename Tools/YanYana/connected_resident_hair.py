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
    # Long low locks inherited shoulder/chest weights in the reference scan.
    # Bind the classified hair to the head even below the jaw; a height-only
    # neck blend otherwise stretches the tips when the idle pose turns its head.
    head=body.vertex_groups.get('Head')
    for vertex,weight in zip(data.vertices,weights):
        amount=smooth((float(weight)-.03)/.27)
        if amount<=0:continue
        previous=[(g.group,g.weight) for g in vertex.groups]
        old_head=sum(w for index,w in previous if index==head.index)
        for index,w in previous:
            if index==head.index:continue
            remaining=w*(1-amount)
            if remaining<.00001:body.vertex_groups[index].remove([vertex.index])
            else:body.vertex_groups[index].add([vertex.index],remaining,'REPLACE')
        head.add([vertex.index],amount+old_head*(1-amount),'REPLACE')
    # Form two complete knots from the reference's curled top surface. A
    # mirrored underside closes the knot without a flat exposed cut face.
    source_uv={l.vertex_index:data.uv_layers.active.data[l.index].uv.copy() for l in data.loops}
    if name=='Ece':
        faces=[]
        for face in data.polygons:
            p=sum((original[i] for i in face.vertices),Vector())/len(face.vertices)
            if p.z>ez+.226*scale and p.y>-.074*scale:faces.append(tuple(face.vertices))
        used=sorted({i for f in faces for i in f});mapping={old:i for i,old in enumerate(used)}
        mesh=bpy.data.meshes.new('Ece reference curls')
        mesh.from_pydata([current[i] for i in used],[],[tuple(mapping[i] for i in reversed(f)) for f in faces]);mesh.update()
        uv=mesh.uv_layers.new(name='OriginalUV')
        for loop in mesh.loops:uv.data[loop.index].uv=source_uv[used[loop.vertex_index]]
        material=data.materials[0].copy();material.name='Ece pigtail painted curls';mesh.materials.append(material)
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00002)
        boundary={v for edge in bm.edges if edge.is_boundary for v in edge.verts}
        plane=sum(v.co.z for v in boundary)/len(boundary)
        for vertex in boundary:vertex.co.z=plane
        duplicate=bmesh.ops.duplicate(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces))['geom']
        mirror_verts=[v for v in duplicate if isinstance(v,bmesh.types.BMVert)]
        mirror_faces=[f for f in duplicate if isinstance(f,bmesh.types.BMFace)]
        for vertex in mirror_verts:vertex.co.z=2*plane-vertex.co.z
        bmesh.ops.reverse_faces(bm,faces=mirror_faces)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00002)
        # Round the welded equator while retaining the sculpted curl peaks.
        seam=[v for v in bm.verts if abs(v.co.z-plane)<.007]
        for _ in range(3):bmesh.ops.smooth_vert(bm,verts=seam,factor=.45,use_axis_x=True,use_axis_y=True,use_axis_z=True)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(mesh);bm.free();mesh.update()
        for face in mesh.polygons:face.use_smooth=True
        center=Vector(((max(v.co.x for v in mesh.vertices)+min(v.co.x for v in mesh.vertices))*.5,(max(v.co.y for v in mesh.vertices)+min(v.co.y for v in mesh.vertices))*.5,plane))
        for sign in (-1,1):
            knot=mesh.copy();ob=bpy.data.objects.new('Ece '+('left' if sign<0 else 'right')+' curled pigtail',knot);bpy.context.collection.objects.link(ob);ob.parent=rig
            for vertex in knot.vertices:
                offset=(vertex.co-center)*.50
                # Present the sculpted curl cap toward the viewer; its joined
                # equator runs around the side/back, below the crown overlap.
                offset=Vector((offset.x,-offset.z,offset.y*1.10))
                vertex.co=Vector((eye.x+sign*.104,.035,eye.z+.228))+offset
            group=ob.vertex_groups.new(name='Head');group.add(list(range(len(knot.vertices))),1,'REPLACE');mod=ob.modifiers.new('Head skin','ARMATURE');mod.object=rig
        bpy.data.meshes.remove(mesh)
    def deform(p,index):
        q=p.copy();s=original[index];dz=(s.z-ez)/scale;amount=float(weights[index])
        if name=='Ece':
            bun=smooth((dz-.160)/.065)*smooth((s.y+.120*scale)/(.080*scale))
            target=Vector((eye.x+(p.x-eye.x)*.65,.045+(p.y-.045)*.70,eye.z+.19+(p.z-eye.z-.19)*.12))
            return p.lerp(target,bun)
        if name=='Gul':
            low=smooth((.095-dz)/.22)
            q.x=eye.x+(p.x-eye.x)*(1-.34*low)
            if p.z<eye.z+.050:q.z=eye.z+.050+(p.z-eye.z-.050)*.36
            q.y=-.060+(p.y+.060)*(1-.22*low)
        elif name=='Deniz':
            # A diagonal, flatter side sweep retains the reference's detailed
            # locks but has a different outline from Eren's tall upright quiff.
            top=smooth((dz-.080)/.20)
            q.x=eye.x+(p.x-eye.x)*(1-.07*top)+.037*top
            q.y+=.012*top
            q.z=eye.z+.085+(p.z-eye.z-.085)*(1-.10*top)
            q.z-=.010*top*smooth((p.x-eye.x+.13)/.26)
        elif name=='Kemal':
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
    body['identity_design']={'Ece':'two small curled pigtails and round child proportions','Gul':'short inward silver waves and a mature oval face','Kemal':'low swept-back silver hair, long face and broad nose','Deniz':'asymmetric low side-swept locks and a slender face','Zeynep':'long chestnut waves with head-bound tips'}[name]
    print('CONNECTED_HAIR',name,'top',body['hair_top'],'hair_vertices',int(sum(weights>.15)),flush=True)
