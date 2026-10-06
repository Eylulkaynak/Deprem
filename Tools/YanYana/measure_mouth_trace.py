"""Locate the mouth painted on the approved surface before cutting the lip seam."""
import bpy,json,pathlib,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');verts=[v.co.copy() for v in o.data.vertices];faces=[tuple(p.vertices) for p in o.data.polygons]
bvh=BVHTree.FromPolygons(verts,faces);uvs=[None]*len(verts)
for l in o.data.loops:uvs[l.vertex_index]=Vector(o.data.uv_layers.active.data[l.index].uv)
im=next(n.image for n in o.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');pixels=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
def sample(x,z):
    p,n,idx,dist=bvh.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
    if p is None:return None
    i,j,k=faces[idx];a=verts[i];v0=verts[j]-a;v1=verts[k]-a;v2=p-a;d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);d20=v2.dot(v0);d21=v2.dot(v1);den=d00*d11-d01*d01
    if abs(den)<1e-15:return None
    v=(d11*d20-d01*d21)/den;w=(d00*d21-d01*d20)/den;uv=uvs[i]*(1-v-w)+uvs[j]*v+uvs[k]*w
    c=pixels[int(uv.y*(im.size[1]-1)),int(uv.x*(im.size[0]-1)),:3]
    return tuple(float(q) for q in c),float(p.y)
trace=[]
for x in np.linspace(-.065,.101,51):
    expected=1.309+.010*((x-.018)/.071)**2;candidates=[]
    for z in np.linspace(expected-.007,expected+.007,89):
        s=sample(float(x),float(z))
        if s is None:continue
        c,y=s;score=c[0]*.15+c[1]*.60+c[2]*.25+abs(z-expected)*.30;candidates.append((score,float(z),c,y))
    a=min(candidates);trace.append({'x':float(x),'z':a[1],'score':a[0],'rgb':a[2],'y':a[3]})
central=[r for r in trace if abs(r['x']-.018)<.057];coef=np.polyfit([r['x']-.018 for r in central],[r['z'] for r in central],2)
eyes=[]
for xmin,xmax in [(-.10,.025),(.026,.150)]:
 points=[]
 for x in np.linspace(xmin,xmax,105):
  for z in np.linspace(1.355,1.463,96):
   s=sample(float(x),float(z))
   if s is not None:
    c,y=s
    if c[0]>.78 and c[1]>.77 and c[2]>.72:points.append((x,z,y))
 a=np.array(points);bounds=[(float(a[:,i].min()),float(a[:,i].max())) for i in range(3)];eyes.append({'bounds':bounds,'center':[sum(bounds[i])*.5 for i in range(2)]})
report={'center_x':.018,'quadratic_z_coefficients':[float(c) for c in coef],'trace':trace,'eyes':eyes}
(ROOT/'.codex_tmp/eren-mouth-trace.json').write_text(json.dumps(report,indent=2));print('MOUTH_TRACE_COEFFICIENTS',coef.tolist());print('EYES',json.dumps(eyes));print(json.dumps(trace[::5]))
