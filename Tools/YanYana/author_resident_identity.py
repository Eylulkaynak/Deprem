"""Blender-only, individually designed hair meshes with preserved facial indices."""
import bpy,math,json,pathlib,numpy as np
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
REF=ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'

def smooth(x):
    x=max(0,min(1,x));return x*x*(3-2*x)

def reference(name):
    data=json.loads((REF/(name+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
    points=[Vector((p['x'],-p['z'],p['y'])) for p in data['vertices']]
    image=bpy.data.images.load(str(REF/data['groups'][0]['texture']),check_existing=True)
    pixels=np.array(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4)
    uv=data['uv'];colors=[]
    for p in uv:
        u,v=(p['x'],p['y']) if isinstance(p,dict) else p
        colors.append(pixels[int(max(0,min(1,v))*(image.size[1]-1)),int(max(0,min(1,u))*(image.size[0]-1)),:3])
    trace=json.loads((ROOT/'.codex_tmp'/('eren-mouth-trace.json' if name=='Emre' else name.lower()+'-mouth-trace.json')).read_text())
    eyes=trace['eyes'];ez=sum(e['center'][1] for e in eyes)/2;cx=sum(e['center'][0] for e in eyes)/2
    scale=(eyes[1]['center'][0]-eyes[0]['center'][0])/.1314615
    return points,colors,cx,ez,scale

def remove_inherited_hair(body,source,name):
    points,colors,cx,ez,scale=reference(source);old=body.data
    if len(points)!=len(old.vertices):raise RuntimeError('Hair authoring must precede facial remeshing: '+name)
    hair=[];jewelry=[]
    for index,(p,c) in enumerate(zip(points,colors)):
        x,y,z=p;dx=abs(x-cx)
        face=y<-.095*scale and dx<.143*scale and z<ez+.105*scale
        skin=float(c.mean())>.49 and max(c)>.64 and z<ez+.22*scale
        brown=c[0]>c[1]*1.03 and c[2]<c[1]*1.10 and max(c)<.70
        crown=z>ez+.118*scale
        outer=(y>-.020*scale and z>ez-.39*scale) or (dx>.15*scale and z>ez-.33*scale)
        hair.append((crown or (outer and brown)) and not(face or skin))
        jewelry.append(name=='Ece' and ez-.24*scale<z<ez+.025*scale and dx>.12*scale and c[0]>.44 and c[1]>.28 and c[2]<c[1]*.53)
    if name=='Ece':
        for _ in range(3):
            expanded=jewelry.copy()
            for polygon in old.polygons:
                if any(jewelry[i] for i in polygon.vertices):
                    for i in polygon.vertices:
                        p=points[i]
                        if abs(p.x-cx)>.125*scale and p.z<ez+.006*scale:expanded[i]=True
            jewelry=expanded
    # Locate the redesigned eyes through their preserved original vertex indices.
    ws=[math.exp(-((p.z-ez)/(.038*scale))**2)*math.exp(-((abs(p.x-cx)-.065*scale)/(.035*scale))**2) if p.y<-.13*scale else 0 for p in points]
    current=sum((v.co*w for v,w in zip(old.vertices,ws)),Vector())/sum(ws)
    return current,hair,jewelry

def mesh(name,verts,faces,material,rig):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);data.materials.append(material)
    for p in data.polygons:p.use_smooth=True
    ob.parent=rig;group=ob.vertex_groups.new(name='Head');group.add(list(range(len(verts))),1,'REPLACE')
    mod=ob.modifiers.new('Head skin','ARMATURE');mod.object=rig
    return ob

def catmull(points,t):
    u=t*(len(points)-1);j=min(len(points)-2,int(u));f=u-j
    a=points[max(0,j-1)];b=points[j];c=points[j+1];d=points[min(len(points)-1,j+2)]
    return .5*((2*b)+(-a+c)*f+(2*a-5*b+4*c-d)*f*f+(-a+3*b-3*c+d)*f*f*f)

def lock(name,path,width,depth,center,material,rig):
    points=[Vector(p) for p in path];N=16;R=32;vs=[];fs=[]
    for j in range(R+1):
        t=j/R;p=catmull(points,t);tangent=(catmull(points,min(1,t+.004))-catmull(points,max(0,t-.004))).normalized()
        normal=(p-center).normalized();u=normal.cross(tangent)
        if u.length<.01:u=Vector((1,0,0)).cross(tangent)
        u.normalize();v=tangent.cross(u).normalized()
        taper=max(.025,math.sin(math.pi*(.12+.88*t))**.50)
        for i in range(N):
            a=math.tau*i/N
            ridge=1+.035*math.cos(a*3+t*4)
            vs.append(p+u*(width*taper*math.cos(a))+v*(depth*taper*math.sin(a)*ridge))
    for j in range(R):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    fs.append(tuple(range(N-1,-1,-1)));fs.append(tuple(R*N+i for i in range(N)))
    return mesh(name,vs,fs,material,rig)

def make_hair(body,rig,name,source,material_factory):
    eye,hair_mask,jewelry=remove_inherited_hair(body,source,name);cx=eye.x;ez=eye.z
    # The inherited scans contain upper-arm influence in the ears/hair and
    # shoulder influence above the jaw. Rebind the redesigned head as a head,
    # with a gradual transition through the neck, before fitting its closure.
    head_group=body.vertex_groups.get('Head')
    for vertex in body.data.vertices:
        amount=smooth((vertex.co.z-ez+.25)/.11)
        if amount<=0:continue
        weights=[(entry.group,entry.weight) for entry in vertex.groups]
        original_head=sum(weight for index,weight in weights if index==head_group.index)
        for index,weight in weights:
            if index==head_group.index:continue
            value=weight*(1-amount)
            if value<.00001:body.vertex_groups[index].remove([vertex.index])
            else:body.vertex_groups[index].add([vertex.index],value,'REPLACE')
        head_group.add([vertex.index],amount+original_head*(1-amount),'REPLACE')
    from connected_resident_hair import sculpt_connected_hair
    sculpt_connected_hair(body,rig,name,source,eye)
    return
    # Keep the face intact. Recede only its upper scalp smoothly underneath
    # the new coiffure instead of cutting colour-classified holes around brows.
    def retreat(p):
        p=p.copy();p.y+=.080*smooth((p.z-ez-.085)/.075)*smooth((-p.y-.05)/.10);return p
    if body.data.shape_keys:
        for key in body.data.shape_keys.key_blocks:
            for vertex in key.data:vertex.co=retreat(vertex.co)
        for vertex,basis in zip(body.data.vertices,body.data.shape_keys.key_blocks[0].data):vertex.co=basis.co
    else:
        for vertex in body.data.vertices:vertex.co=retreat(vertex.co)
    body.data.update()
    if name=='Ece':
        center=Vector((cx,.005,ez+.063));radii=Vector((.177,.257,.199));front=1.47;side=2.04;back=2.32;hexes=('#472E26','#50342A','#3E2823')
    elif name=='Gul':
        center=Vector((cx,.006,ez+.064));radii=Vector((.194,.279,.225));front=1.40;side=1.89;back=2.03;hexes=('#8E8B84','#ABA89F','#76756F')
    else:
        center=Vector((cx,.006,ez+.061));radii=Vector((.172,.310,.207));front=1.10;side=1.55;back=1.68;hexes=('#626260','#85837E','#50504D')
    materials=[material_factory(name+' designed hair '+str(i),h,rough=.58) for i,h in enumerate(hexes)]
    def limit(theta):
        c=math.cos(theta)
        return side+(front-side)*max(0,c)**3+(back-side)*max(0,-c)**2
    # Retain a single uninterrupted facial patch, ears, neck and clothing.
    # A geometric boundary behind the new hair avoids UV-island classification
    # tearing the brows or stretching individual vertices across the neck.
    original,colors,refcx,refez,scale=reference(source);old=body.data;keep=[]
    # Identify the two painted brows as connected dark patches, so the broad
    # old ellipse cannot retain a piece of the previous fringe as an eyebrow.
    _,bweld=np.unique(np.round(np.array([p[:] for p in original]),5),axis=0,return_inverse=True)
    candidates={};vertex_faces={}
    for poly in old.polygons:
        p=sum((original[i] for i in poly.vertices),Vector())/len(poly.vertices)
        c=sum((colors[i] for i in poly.vertices),np.zeros(3))/len(poly.vertices)
        if p.y<-.14*scale and abs(p.x-refcx)<.17*scale and .025*scale<p.z-refez<.15*scale and c.mean()<.48 and c[0]>c[1]*1.07 and c[2]<c[1]*.99:
            candidates[poly.index]=p
            for vi in poly.vertices:vertex_faces.setdefault(int(bweld[vi]),[]).append(poly.index)
    components=[];seen=set()
    for start in candidates:
        if start in seen:continue
        stack=[start];members=[]
        while stack:
            i=stack.pop()
            if i in seen:continue
            seen.add(i);members.append(i)
            for vi in old.polygons[i].vertices:stack.extend(j for j in vertex_faces[int(bweld[vi])] if j not in seen)
        if len(members)>3:
            center=sum((candidates[i] for i in members),Vector())/len(members);components.append((center,members))
    brow_faces=set()
    for sign in (-1,1):
        if not components:continue
        center,members=min(components,key=lambda item:((item[0].x-refcx-sign*.066*scale)/scale)**2+((item[0].z-refez-.075*scale)/scale)**2)
        brow_faces.update(i for i in members if abs(candidates[i].x-refcx-sign*.066*scale)<.049*scale and abs(candidates[i].z-refez-.075*scale)<.030*scale and candidates[i].y>center.y-.020*scale)
        print('PROTECTED_BROW',name,sign,len(members),list(center),flush=True)
    group_names={g.index:g.name for g in body.vertex_groups}
    for polygon in old.polygons:
        current=sum((old.vertices[i].co for i in polygon.vertices),Vector())/len(polygon.vertices)
        # End the old front scalp along one continuous line underneath the
        # new fringe. Moving isolated ray-hit vertices stretches scan triangles.
        if current.z>ez+.170 and current.y<-.085:continue
        p=sum((original[i] for i in polygon.vertices),Vector())/len(polygon.vertices)
        c=sum((colors[i] for i in polygon.vertices),np.zeros(3))/len(polygon.vertices)
        dx=abs(p.x-refcx)
        face=p.y<-.080*scale and dx<.190*scale and refez-.235*scale<p.z<refez+.190*scale
        neck=dx<.077*scale and p.z<refez-.125*scale
        ear=((dx-.192*scale)/(.052*scale))**2+((p.y+.075*scale)/(.095*scale))**2+((p.z-refez+.038*scale)/(.075*scale))**2<1
        brown=c[0]>c[1]*1.055 and ((c[2]<c[1]*.86 and float(c.mean())<.58) or (float(c.mean())<.30 and c[0]>c[1]*1.35))
        brow=polygon.index in brow_faces
        facial_feature=p.y<-.135*scale and ((dx<.126*scale and refez-.22*scale<p.z<refez+.052*scale) or brow)
        hair_region=(p.z>refez-.39*scale and (dx>.122*scale or p.y>-.065*scale or p.z>refez+.052*scale))
        inherited_hair=brown and hair_region and not facial_feature
        if ear:inherited_hair=False
        if face and p.y<-.085*scale and refez-.110*scale<p.z<refez+.190*scale:inherited_hair=False
        if name=='Gul' and p.y>-.15*scale and refez-.20*scale<p.z<refez-.025*scale and c.mean()<.35 and max(c)<.50:inherited_hair=True
        if name=='Gul' and p.y>-.01 and dx>.095*scale and refez-.29*scale<p.z<refez-.13*scale and c.mean()<.40 and max(c)<.60:inherited_hair=True
        head_weight=sum(g.weight for i in polygon.vertices for g in old.vertices[i].groups if group_names[g.group]=='Head')/len(polygon.vertices)
        body_part=p.z<refez-.130*scale and (head_weight<.45 or (p.z<refez-.215*scale and c.mean()>.58 and dx<.09*scale))
        # Long source hair may have chest weights. Texture and position, not
        # the inherited bone label, determine those strands.
        ring=name=='Ece' and any(jewelry[i] for i in polygon.vertices)
        if name=='Kemal' and p.z<refez-.13*scale:
            keep.append(polygon);continue
        if (face or neck or ear or body_part) and not (ring or inherited_hair):keep.append(polygon)
    # Drop disconnected remnants after the scalp boundary is cut. Weld only for
    # connectivity analysis; keep original UV vertices and facial indices intact.
    coords=np.round(np.array([p[:] for p in original]),5)
    _,weld=np.unique(coords,axis=0,return_inverse=True);parents=list(range(int(weld.max())+1))
    def find(i):
        while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
        return i
    for polygon in keep:
        first=find(int(weld[polygon.vertices[0]]))
        for index in polygon.vertices[1:]:parents[find(int(weld[index]))]=first
    roots=set()
    for polygon in keep:
        p=sum((original[i] for i in polygon.vertices),Vector())/len(polygon.vertices)
        if p.z<refez-.285*scale or (p.y<-.12*scale and abs(p.x-refcx)<.132*scale and refez-.22*scale<p.z<refez+.101*scale):roots.add(find(int(weld[polygon.vertices[0]])))
    keep=[polygon for polygon in keep if find(int(weld[polygon.vertices[0]])) in roots]
    weights=[[(g.group,g.weight) for g in v.groups] for v in old.vertices];names=[g.name for g in body.vertex_groups]
    # Average across duplicate UV vertices before the boundary is cut, so the
    # face retains smooth shading along UV seams and the new scalp edge.
    normal_values=np.array([v.normal[:] for v in old.vertices]);sums=np.zeros((int(weld.max())+1,3));np.add.at(sums,weld,normal_values)
    lengths=np.linalg.norm(sums,axis=1);sums/=np.maximum(lengths[:,None],1e-12)
    keys=[(k.name,[v.co.copy() for v in k.data]) for k in old.shape_keys.key_blocks] if old.shape_keys else []
    data=bpy.data.meshes.new(name+' continuous facial patch');data.from_pydata([v.co.copy() for v in old.vertices],[],[tuple(p.vertices) for p in keep]);data.update()
    for m in old.materials:data.materials.append(m)
    uv=data.uv_layers.new(name=old.uv_layers.active.name)
    for polygon,source_polygon in zip(data.polygons,keep):
        polygon.use_smooth=True;polygon.material_index=source_polygon.material_index
        for a,b in zip(polygon.loop_indices,source_polygon.loop_indices):uv.data[a].uv=old.uv_layers.active.data[b].uv
    body.data=data;body.vertex_groups.clear();groups=[body.vertex_groups.new(name=n) for n in names]
    attribute=data.attributes.new(name='uncut_surface_normal',type='FLOAT_VECTOR',domain='POINT')
    for entry,normal in zip(attribute.data,sums[weld]):entry.vector=normal
    for i,ws in enumerate(weights):
        for index,w in ws:groups[index].add([i],w,'REPLACE')
    for key_name,positions in keys:
        key=body.shape_key_add(name=key_name);key.value=0
        for vertex,p in zip(key.data,positions):vertex.co=p
    from close_resident_head import close_head
    close_head(body,rig,name,eye,mesh)
    print('DESIGNED_HAIR',name,'removed_faces',len(old.polygons)-len(keep),'eye',list(eye),flush=True)
    from reference_resident_hair import make_reference_hair
    make_reference_hair(body,rig,name,eye)
    return
    def surface(theta,phi,offset=0):
        wave=1+(.012 if name=='Gul' else .006)*math.cos(theta*7+phi*3)*math.sin(phi)
        q=Vector((radii.x*math.sin(phi)*math.sin(theta)*wave,-radii.y*math.sin(phi)*math.cos(theta)*wave,radii.z*math.cos(phi)))
        return center+q+q.normalized()*offset
    N=112;R=42;vs=[];fs=[]
    for j in range(R+1):
        for i in range(N):
            theta=math.tau*i/N;vs.append(surface(theta,limit(theta)*j/R))
    for j in range(R):
        for i in range(N):fs.append((j*N+i,(j+1)*N+i,(j+1)*N+(i+1)%N,j*N+(i+1)%N))
    bottom=len(vs);vs.append(Vector((center.x,center.y,ez+(.17 if name=='Kemal' else .15))))
    for i in range(N):fs.append((R*N+i,bottom,R*N+(i+1)%N))
    cap=mesh(name+' tailored hair silhouette',vs,fs,materials[0],rig)
    if name=='Ece':
        # A rounded chin-length bob, with a deliberately separate soft fringe.
        for k in range(14):
            theta=.53+(math.tau-1.06)*k/13;end=limit(theta)
            path=[surface(theta-.34*(1-t)+.055*math.sin(t*4+k),.12+(end-.12)*t,.012+.007*math.sin(t*math.pi)) for t in np.linspace(0,1,7)]
            lock(name+' curved bob lock '+str(k),path,.047+.005*math.sin(k*2),.017,center,materials[0],rig)
        for k,x in enumerate((-.076,.004,.077)):
            path=[(cx+x*.45+.025,.015,ez+.257),(cx+x*.78+.025,-.116,ez+.240),(cx+x-.020,-.246,ez+.180),(cx+x+.009,-.296,ez+.067+.018*(k==2))]
            lock(name+' swept fringe '+str(k),path,.061 if k<2 else .050,.023,center,materials[0],rig)
        lock(name+' crown sweep',[(cx+.11,.052,ez+.214),(cx+.07,-.011,ez+.267),(cx-.033,-.081,ez+.254),(cx-.109,-.121,ez+.208)],.046,.018,center,materials[0],rig)
        for sign in (-1,1):
            lock(name+' cheek framing bob '+str(sign),[(cx+sign*.118,-.176,ez+.20),(cx+sign*.141,-.230,ez+.113),(cx+sign*.147,-.225,ez+.012),(cx+sign*.157,-.153,ez-.075)],.043,.022,center,materials[0],rig)
    elif name=='Gul':
        # Short, swept silver layers; no inherited long brown waves.
        for k in range(14):
            theta=math.tau*k/14;end=limit(theta)
            path=[surface(theta-.52*(1-t)+.14*math.sin(t*4+k*.7),.13+(end-.13)*t,.013+.018*math.sin(t*math.pi)) for t in np.linspace(0,1,7)]
            lock(name+' silver wave '+str(k),path,.053+.007*math.sin(k*2),.024,center,materials[1 if k%6==1 else 0],rig)
        for k in range(4):
            x=-.075+k*.050
            path=[(cx+.062+k*.014,-.107,ez+.260),(cx+x+.055,-.199,ez+.232),(cx+x,-.254,ez+.184),(cx+x-.036,-.252,ez+.116)]
            lock(name+' soft side fringe '+str(k),path,.042,.021,center,materials[k%2],rig)
        for k in range(3):
            lock(name+' crown wave '+str(k),[(cx+.12,.105-k*.055,ez+.212),(cx+.068,.048-k*.05,ez+.291),(cx-.048,-.015-k*.046,ez+.277),(cx-.138,-.043-k*.049,ez+.205)],.053,.026,center,materials[0],rig)
        lock(name+' side swept temple',[(cx+.045,-.213,ez+.228),(cx+.108,-.309,ez+.17),(cx+.145,-.335,ez+.092),(cx+.169,-.255,ez+.012)],.048,.025,center,materials[0],rig)
    else:
        # Low side-parted silver hair, with a visible forehead and tidy temples.
        for k in range(18):
            theta=math.tau*k/18;end=limit(theta)
            if math.cos(theta)>.70:continue
            path=[surface(theta-.38*(1-t),.14+(end-.14)*t,.008) for t in np.linspace(0,1,6)]
            lock(name+' combed side layer '+str(k),path,.022,.010,center,materials[1 if k%7==0 else 0],rig)
        for k in range(4):
            path=[(cx+.130,-.080+k*.018,ez+.160+k*.020),(cx+.100,-.220+k*.024,ez+.180+k*.022),(cx-.030,-.295+k*.022,ez+.148+k*.028),(cx-.145,-.230+k*.035,ez+.110+k*.026)]
            lock(name+' side parted crown '+str(k),path,.034,.014,center,materials[0],rig)
        lock(name+' short crown sweep',[(cx+.109,.050,ez+.198),(cx+.058,-.008,ez+.271),(cx-.055,-.093,ez+.263),(cx-.127,-.105,ez+.206)],.043,.019,center,materials[0],rig)
    # Fuse the broad locks into an actual continuous sculpt: intersection
    # seams and hard cap edges must not survive as separate plastic pieces.
    hair_objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m in materials for m in o.data.materials)]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in hair_objects:
        bpy.context.view_layer.objects.active=ob
        for mod in list(ob.modifiers):
            if mod.type=='ARMATURE':ob.modifiers.remove(mod)
            else:bpy.ops.object.modifier_apply(modifier=mod.name)
        ob.select_set(True)
    bpy.context.view_layer.objects.active=cap;bpy.ops.object.join()
    cap.data.remesh_voxel_size=.0028;cap.data.remesh_voxel_adaptivity=0
    bpy.ops.object.voxel_remesh()
    mod=cap.modifiers.new('Sculpted soft transitions','SMOOTH');mod.factor=.65;mod.iterations=8;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=cap.modifiers.new('Game surface reduction','DECIMATE');mod.ratio=.13;bpy.ops.object.modifier_apply(modifier=mod.name)
    cap.data.materials.clear();cap.data.materials.append(materials[0])
    for polygon in cap.data.polygons:polygon.use_smooth=True;polygon.material_index=0
    for edge in cap.data.edges:edge.use_edge_sharp=False
    bpy.ops.object.shade_smooth()
    # These faces sit forward of the rig origin. Match the short anatomical
    # rear skull depth instead of mirroring forehead depth behind the head.
    for vertex in cap.data.vertices:
        p=vertex.co
        p.y-=.160*smooth((p.y+.080)/.280)
    if name in ('Kemal','Gul'):
        for vertex in cap.data.vertices:
            p=vertex.co;side_fit=smooth((abs(p.x-cx)-.09)/.07)*(1-smooth((p.z-ez-.20)/.08))
            p.x=cx+(p.x-cx)*(1+.045*side_fit)
            if p.y<0:p.y-=.010*side_fit
    cap.vertex_groups.clear();cap.vertex_groups.new(name='Head').add(list(range(len(cap.data.vertices))),1,'REPLACE')
    mod=cap.modifiers.new('Head skin','ARMATURE');mod.object=rig
    body['hair_reference']='Independent '+name+' haircut, designed as sculpted volumetric Blender surfaces in the approved family style.'
    body['identity_design']={'Ece':'round child face, soft fringe and chin-length bob','Gul':'longer mature face and short silver waves','Kemal':'unmustached longer face and low side-parted salt-and-pepper hair'}[name]

