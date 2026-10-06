"""Locate the actual painted eyes/lips on each approved anatomical template."""
import bpy,json,pathlib,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2]
for name in ('Ada','Derya','Efe','Yusuf'):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'/(name+'.blend')))
    o=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    verts=[v.co.copy() for v in o.data.vertices];faces=[tuple(p.vertices) for p in o.data.polygons];bvh=BVHTree.FromPolygons(verts,faces);uvs=[None]*len(verts)
    for l in o.data.loops:uvs[l.vertex_index]=Vector(o.data.uv_layers.active.data[l.index].uv)
    im=next(n.image for n in o.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');pixels=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
    def sample(x,z):
        p,n,idx,dist=bvh.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if p is None:return None
        i,j,k=faces[idx];a=verts[i];v0=verts[j]-a;v1=verts[k]-a;v2=p-a;d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);d20=v2.dot(v0);d21=v2.dot(v1);den=d00*d11-d01*d01
        if abs(den)<1e-15:return None
        v=(d11*d20-d01*d21)/den;w=(d00*d21-d01*d20)/den;uv=uvs[i]*(1-v-w)+uvs[j]*v+uvs[k]*w;c=pixels[int(uv.y*(im.size[1]-1)),int(uv.x*(im.size[0]-1)),:3]
        return tuple(float(q) for q in c),float(p.y)
    headz=rig.data.bones['Head'].head_local.z;cx=rig.data.bones['Head'].head_local.x;scale=headz/1.214
    eyes=[]
    for xmin,xmax in ((cx-.16*scale,cx-.006*scale),(cx+.006*scale,cx+.16*scale)):
        points=[]
        for x in np.linspace(xmin,xmax,105):
            for z in np.linspace(headz+.015*scale,headz+.29*scale,160):
                s=sample(float(x),float(z))
                if s is None:continue
                c,y=s
                if min(c)>.70 and abs(c[0]-c[1])<.075 and abs(c[1]-c[2])<.085 and y<-.11*scale:points.append((x,z,y))
        a=np.array(points);zmin,zmax=a[:,1].min(),a[:,1].max();left,right=a[:,0].min()-.025*scale,a[:,0].max()+.025*scale;yfront=a[:,2].min()+.032*scale
        for x in np.linspace(max(xmin,left),min(xmax,right),105):
            for z in np.linspace(zmin,zmax,65):
                s=sample(float(x),float(z))
                if s is not None:
                    c,y=s
                    if max(c)<.29 and y<yfront:points.append((x,z,y))
        a=np.array(points);bounds=[(float(a[:,i].min()),float(a[:,i].max())) for i in range(3)];eyes.append({'bounds':bounds,'center':[sum(bounds[i])*.5 for i in range(2)]})
    eyez=sum(e['center'][1] for e in eyes)/2;cx=sum(e['center'][0] for e in eyes)/2
    scale=(eyes[1]['center'][0]-eyes[0]['center'][0])/.1314615
    # Central mouth search excludes the nose and chin. The lip is the strongest
    # narrow dark valley in this strip, not merely the darkest skin pixel.
    zrange={'Ada':(.968,.992),'Derya':(1.215,1.253),'Efe':(.806,.839),'Yusuf':(1.217,1.260)}[name]
    zs=np.linspace(*zrange,260);scores=[]
    for z in zs:
        cs=[sample(float(x),float(z)) for x in np.linspace(cx-.017*scale,cx+.017*scale,13)];cs=[q for q in cs if q]
        scores.append(np.mean([c[0]*.10+c[1]*.65+c[2]*.25 for c,y in cs]))
    arr=np.array(scores);baseline=np.convolve(np.pad(arr,(18,18),mode='edge'),np.ones(37)/37,mode='valid');centerz=float(zs[np.argmin(arr-baseline)])
    trace=[]
    for x in np.linspace(cx-.071*scale,cx+.071*scale,61):
        expected=centerz+.008*scale*((x-cx)/(.055*scale))**2;candidates=[]
        for z in np.linspace(expected-.006*scale,expected+.006*scale,71):
            s=sample(float(x),float(z))
            if s:
                c,y=s;score=c[0]*.10+c[1]*.65+c[2]*.25;candidates.append((score,float(z),c,y))
        if candidates:
            a=min(candidates);trace.append({'x':float(x),'z':a[1],'score':a[0],'rgb':a[2],'y':a[3]})
    central=[r for r in trace if abs(r['x']-cx)<.053*scale];coef=np.polyfit([r['x']-cx for r in central],[r['z'] for r in central],2)
    report={'source':name,'center_x':cx,'scale':scale,'head_z':headz,'quadratic_z_coefficients':coef.tolist(),'trace':trace,'eyes':eyes}
    (ROOT/'.codex_tmp'/(name.lower()+'-mouth-trace.json')).write_text(json.dumps(report,indent=2))
    print(name,'head',headz,'center',cx,centerz,'scale',scale,'coef',coef.tolist(),'eyes',eyes,flush=True)
