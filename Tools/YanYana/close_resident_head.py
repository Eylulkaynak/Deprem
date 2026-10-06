"""Close the actual cut boundary, retaining its silhouette and skin shading."""
import bpy, math, pathlib, numpy as np
from collections import defaultdict
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def close_head(body,rig,name,eye,mesh_factory):
    data=body.data;points=np.array([v.co[:] for v in data.vertices]);unique,weld=np.unique(np.round(points,5),axis=0,return_inverse=True)
    canonical={int(w):i for i,w in enumerate(weld)};edges=defaultdict(list)
    for polygon in data.polygons:
        ids=list(polygon.vertices)
        for a,b in zip(ids,ids[1:]+ids[:1]):
            a,b=int(weld[a]),int(weld[b]);edges[tuple(sorted((a,b)))].append((a,b))
    boundary=[entry[0] for entry in edges.values() if len(entry)==1];adj=defaultdict(set)
    for a,b in boundary:adj[a].add(b);adj[b].add(a)
    seen=set();components=[]
    for start in adj:
        if start in seen:continue
        stack=[start];component=set()
        while stack:
            index=stack.pop()
            if index in seen:continue
            seen.add(index);component.add(index);stack.extend(adj[index]-seen)
        if len(component)>5 and max(unique[i,2] for i in component)>eye.z-.20:components.append(component)
    image=next(node.image for material in data.materials for node in material.node_tree.nodes if node.type=='TEX_IMAGE' and node.image)
    pixels=np.array(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4)
    uv={loop.vertex_index:data.uv_layers.active.data[loop.index].uv.copy() for loop in data.loops}
    def sample(index):
        point=uv.get(index,Vector((0,0)));return pixels[int(max(0,min(1,point.y))*(image.size[1]-1)),int(max(0,min(1,point.x))*(image.size[0]-1)),:3]
    skin_samples=[sample(v.index) for v in data.vertices if v.index in uv and v.co.y<-.15 and abs(v.co.x-eye.x)<.07 and eye.z-.13<v.co.z<eye.z+.06]
    skin_samples=[c for c in skin_samples if c[0]>.65 and c[1]>.38 and c[2]>.25]
    skin=np.median(skin_samples,axis=0) if skin_samples else np.array([.86,.69,.57])
    vs=[];fs=[];colors=[];boundary_normals={};normal_attr=data.attributes.get('uncut_surface_normal')
    for component in components:
        ids=sorted(component);mapping={v:i for i,v in enumerate(ids)};outline=[(a,b) for a,b in boundary if a in component and b in component]
        ps=np.array([points[canonical[i]] for i in ids]);large=np.ptp(ps[:,2])>.14
        if large:
            # The long-haired source's opening extends into the coat. That
            # part is a garment boundary, not a surface to fan into the skull.
            outline=[(a,b) for a,b in outline if min(points[canonical[a],2],points[canonical[b],2])>eye.z-(.140 if name=='Ece' else .155)]
        elif ps[:,2].max()<eye.z-.155:
            continue
        center=Vector((eye.x,.015 if name!='Ece' else -.015,eye.z+.025)) if large else Vector(ps.mean(0))
        rings=20 if large else 2;base=len(vs);n=len(ids)
        for ring in range(rings):
            t=ring/rings;radius=math.cos(t*math.pi/2)
            for index in ids:
                original=canonical[index];p=Vector(points[original]);q=center+(p-center)*radius
                if large:q.y=p.y+(center.y-p.y)*math.sin(t*math.pi/2)
                vs.append(q);c=sample(original)
                if not(c[0]>.65 and c[0]>c[1]*1.08 and c[1]>c[2]*1.08):c=skin
                colors.append(c*(1-t*.8)+skin*t*.8)
                if ring==0 and normal_attr:boundary_normals[len(vs)-1]=-Vector(normal_attr.data[original].vector)
        if large:
            for ring in range(1,rings):
                for repeat in range(10):
                    updates={}
                    for index in ids:
                        neighbors=[mapping[j] for j in adj[index] if j in mapping]
                        if neighbors:
                            vi=base+ring*n+mapping[index]
                            mean=sum((vs[base+ring*n+j] for j in neighbors),Vector())/len(neighbors)
                            updates[vi]=vs[vi].lerp(mean,.65)
                    for vi,point in updates.items():vs[vi]=point
        tip=len(vs);vs.append(center);colors.append(skin)
        for ring in range(rings-1):
            for a,b in outline:
                a,b=mapping[a],mapping[b];fs.append((base+ring*n+a,base+ring*n+b,base+(ring+1)*n+b,base+(ring+1)*n+a))
        for a,b in outline:fs.append((base+(rings-1)*n+mapping[a],base+(rings-1)*n+mapping[b],tip))
    material=bpy.data.materials.new(name+' matching closure skin');material.use_nodes=True
    ob=mesh_factory(name+' fitted back of head',vs,fs,material,rig)
    # Boundary skin must follow the same Head/Neck/Chest blend as the existing
    # face. A rigid Head-only patch tears away when the idle pose turns.
    data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in data.loop_triangles]
    tree=BVHTree.FromPolygons([v.co for v in data.vertices],triangles)
    source_groups=[g.name for g in body.vertex_groups];ob.vertex_groups.clear()
    groups=[ob.vertex_groups.new(name=group) for group in source_groups]
    for vertex in ob.data.vertices:
        hit,normal,index,distance=tree.find_nearest(vertex.co);ids=triangles[index]
        a,b,c=(data.vertices[i].co for i in ids);u=b-a;v=c-a;p=hit-a
        uu=u.dot(u);uv=u.dot(v);vv=v.dot(v);den=uu*vv-uv*uv
        if abs(den)<1e-15:fractions=(1.,0.,0.)
        else:
            beta=(vv*p.dot(u)-uv*p.dot(v))/den;gamma=(uu*p.dot(v)-uv*p.dot(u))/den
            fractions=(max(0,1-beta-gamma),max(0,beta),max(0,gamma))
        weights=defaultdict(float)
        for i,factor in zip(ids,fractions):
            for weight in data.vertices[i].groups:weights[weight.group]+=weight.weight*factor
        total=sum(weights.values())
        for group,weight in weights.items():
            if weight>0:groups[group].add([vertex.index],weight/total,'REPLACE')
    colors_attr=ob.data.color_attributes.new(name='MatchedSkin',type='FLOAT_COLOR',domain='POINT')
    for entry,c in zip(colors_attr.data,colors):entry.color=tuple(float(x/12.92 if x<.04045 else ((x+.055)/1.055)**2.4) for x in c)+(1,)
    node=material.node_tree.nodes.new('ShaderNodeVertexColor');node.layer_name='MatchedSkin';material.node_tree.links.new(node.outputs['Color'],material.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    ob.data.uv_layers.new(name='OriginalUV');bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
    from resident_materials import bake_material
    folder=pathlib.Path(__file__).resolve().parents[2]/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name
    bake_material(ob,name+'_HeadClosure',folder);ob.data.materials[0].name=name+' matched closure skin albedo'
    print('FITTED_HEAD_CLOSURE',name,len(components),len(vs),flush=True)
