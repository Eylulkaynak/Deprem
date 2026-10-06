"""Cut an animatable lip seam into the existing textured anatomical surface.

Unlike an overlaid replacement face, every new lip vertex interpolates original
UVs and skin weights. The base face, eyelids and brow paint remain on their mesh.
Offline Blender authoring only; does not add Unity runtime code.
"""
import bpy,math,pathlib,json,sys,numpy as np,argparse
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import color
OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop/Eren'
KEYS=('Mouth_A','Mouth_E','Mouth_O','Blink','Fear','Surprise','Smile')

def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def g(t,c,r):return math.exp(-((t-c)/r)**2)

class SurfaceExpressions:
    def __init__(self,obj,source,trace,name):
        self.obj=obj;self.old=obj.data;self.name=name;self.src=[Vector((p['x'],-p['z'],p['y'])) for p in source['vertices']]
        self.amp=.78 if name in ('Ece','Arda') else 1.0
        self.trace=trace
        if name!='Eren':
            trace=dict(trace);trace.setdefault('scale',1.0)
            scale=trace['scale'];cx=trace['center_x'];a,b,c=trace['quadratic_z_coefficients']
            self.src=[Vector((.018+(p.x-cx)/scale,p.y,1.30431465+(p.z-c)/scale)) for p in self.src]
            trace=dict(trace);trace['quadratic_z_coefficients']=[a*scale,b,1.30431465]
            trace['eyes']=[dict(center=[.018+(e['center'][0]-cx)/scale,1.30431465+(e['center'][1]-c)/scale]) for e in trace['eyes']]
        self.v=[p.co.copy() for p in self.old.vertices];self.normals=[p.normal.copy() for p in self.old.vertices]
        self.weights=[{q.group:q.weight for q in v.groups} for v in self.old.vertices];self.group_names=[g.name for g in obj.vertex_groups]
        self.uv=[None]*len(self.v)
        for l in self.old.loops:self.uv[l.vertex_index]=Vector(self.old.uv_layers.active.data[l.index].uv)
        self.oldkeys={k.name:[p.co.copy() for p in k.data] for k in self.old.shape_keys.key_blocks} if self.old.shape_keys else {'Basis':list(self.v)}
        self.faces=[];self.mi=[];self.cuts={};self.roles={};self.segments=[];self.tris=[(tuple(p.vertices),p.material_index) for p in self.old.polygons]
        self.coef=trace['quadratic_z_coefficients'];self.cx=.018-self.coef[1]/(2*self.coef[0]);self.width=.055
        self.params={'Mouth_A':(.021,.004,1),'Mouth_E':(.009,.002,1.10),'Mouth_O':(.024,.008,.37),'Fear':(.004,.002,1),'Surprise':(.029,.009,.43),'Smile':(.007,.002,1.06)}
        ws=[g(p.x,self.cx,.012)*g(self.q(p),0,.018)*smooth((-p.y-.1)/.09) for p in self.src];self.final_cx=sum(v.x*w for v,w in zip(self.v,ws))/sum(ws)
        self.lidfits=[]
        for eye in trace['eyes']:
            ex,ez=eye['center']
            rows=[];ys=[]
            for p,v in zip(self.src,self.v):
                dx=p.x-ex;dz=p.z-ez;r=(dx/.036)**2+(dz/.030)**2
                if p.y<-.14 and 1.35<r<3.4:rows.append([1,dx,dz,dx*dx,dx*dz,dz*dz]);ys.append(v.y)
            linear=np.linalg.lstsq(np.array(rows)[:,:3],np.array(ys),rcond=None)[0]
            coef=np.r_[linear,0.,0.,0.];self.lidfits.append((ex,ez,coef))

    def midpoint(self,i,j,cache):
        edge=tuple(sorted((i,j)))
        if edge in cache:return cache[edge]
        n=len(self.v);cache[edge]=n;base=(self.v[i]+self.v[j])*.5;curved=sum((base-(base-self.v[k]).dot(self.normals[k])*self.normals[k] for k in (i,j)),Vector())*.5;delta=(curved-base)*.85
        self.v.append(base+delta);self.src.append((self.src[i]+self.src[j])*.5);self.uv.append((self.uv[i]+self.uv[j])*.5);self.normals.append((self.normals[i]+self.normals[j]).normalized())
        w={}
        for a in (i,j):
            for k,value in self.weights[a].items():w[k]=w.get(k,0)+value*.5
        self.weights.append(w)
        for name,data in self.oldkeys.items():data.append((data[i]+data[j])*.5+delta)
        return n

    def refine_lids(self):
        for level in range(2):
            cache={};out=[]
            for ids,mi in self.tris:
                ps=[self.src[i] for i in ids];inside=any(p.y<-.13 and any(((p.x-ex)/.070)**2+((p.z-ez)/.069)**2<1 for ex,ez,c in self.lidfits) for p in ps)
                if inside and len(ids)==3:
                    a,b,c=ids;ab=self.midpoint(a,b,cache);bc=self.midpoint(b,c,cache);ca=self.midpoint(c,a,cache)
                    out.extend([((a,ab,ca),mi),((ab,b,bc),mi),((ca,bc,c),mi),((ab,bc,ca),mi)])
                else:out.append((ids,mi))
            self.tris=out

    def refine_mouth(self):
        for level in range(1):
            cache={}
            for ids,mi in self.tris:
                p=sum((self.src[i] for i in ids),Vector())/len(ids)
                if p.y<-.12 and abs(p.x-self.cx)<.13 and abs(self.q(p))<.115:
                    for i,j in zip(ids,ids[1:]+ids[:1]):self.midpoint(i,j,cache)
            out=[]
            for ids,mi in self.tris:
                ring=[]
                for i,j in zip(ids,ids[1:]+ids[:1]):
                    ring.append(i)
                    if tuple(sorted((i,j))) in cache:ring.append(cache[tuple(sorted((i,j)))])
                if len(ring)==len(ids):out.append((ids,mi));continue
                n=len(self.v);count=len(ids);base=sum((self.v[i] for i in ids),Vector())/count;curved=sum((base-(base-self.v[i]).dot(self.normals[i])*self.normals[i] for i in ids),Vector())/count;delta=(curved-base)*.85
                self.v.append(base+delta);self.src.append(sum((self.src[i] for i in ids),Vector())/count);self.uv.append(sum((self.uv[i] for i in ids),Vector((0,0)))/count);self.normals.append(sum((self.normals[i] for i in ids),Vector()).normalized());w={}
                for i in ids:
                    for k,value in self.weights[i].items():w[k]=w.get(k,0)+value/count
                self.weights.append(w)
                for key,data in self.oldkeys.items():data.append(sum((data[i] for i in ids),Vector())/count+delta)
                for i,j in zip(ring,ring[1:]+ring[:1]):out.append(((n,i,j),mi))
            self.tris=out

    def q(self,p):
        x=p.x-.018;a,b,c=self.coef
        return p.z-(a*x*x+b*x+c)

    def cut(self,i,j,role):
        edge=tuple(sorted((i,j)));token=(edge,role)
        if token in self.cuts:return self.cuts[token]
        pa,pb=self.src[i],self.src[j];qa=self.q(pa);lo=0.;hi=1.
        for _ in range(30):
            mid=(lo+hi)*.5;qm=self.q(pa.lerp(pb,mid))
            if (qm>=0)==(qa>=0):lo=mid
            else:hi=mid
        t=(lo+hi)*.5;n=len(self.v);self.cuts[token]=n
        self.src.append(pa.lerp(pb,t));self.v.append(self.v[i].lerp(self.v[j],t));self.normals.append(self.normals[i].lerp(self.normals[j],t).normalized());self.uv.append(self.uv[i].lerp(self.uv[j],t))
        w={}
        for bone,value in self.weights[i].items():w[bone]=w.get(bone,0)+value*(1-t)
        for bone,value in self.weights[j].items():w[bone]=w.get(bone,0)+value*t
        self.weights.append(w);self.roles[n]=role
        for name,data in self.oldkeys.items():data.append(data[i].lerp(data[j],t))
        return n

    def split_surface(self):
        for polygon,mi in self.tris:
            ids=list(polygon);qs=[self.q(self.src[i]) for i in ids]
            if min(qs)<0<max(qs):
                edges=[]
                for role in ('U','L'):
                    clipped=[]
                    for k,i in enumerate(ids):
                        j=ids[(k+1)%len(ids)];a=qs[k];b=qs[(k+1)%len(ids)];inside=a>=0 if role=='U' else a<=0
                        if inside:clipped.append(i)
                        if a*b<0:
                            clipped.append(self.cut(i,j,role))
                            if role=='U':edges.append(tuple(sorted((i,j))))
                    if len(clipped)>=3:self.faces.append(tuple(clipped));self.mi.append(mi)
                if len(edges)==2:
                    ia=self.cuts[(edges[0],'U')];ib=self.cuts[(edges[1],'U')];p=(self.src[ia]+self.src[ib])*.5
                    if p.y<-.115 and abs(p.x-self.cx)<self.width*1.08:self.segments.append((edges[0],edges[1]))
            else:self.faces.append(tuple(ids));self.mi.append(mi)
        self.skin_count=len(self.v)

    def expression(self,index,key,role=None):
        p=self.src[index];q=self.v[index].copy();front=smooth((-p.y-.085)/.095)
        if key=='Blink':
            for ex,ez,coeff in self.lidfits:
                r=((p.x-ex)/.034)**2+((p.z-ez)/.030)**2;q.y+=.009*(1-smooth((r-.35)/.75))*front
        if key in ('Fear','Surprise','Smile'):
            # Move the existing eye and lid surface, with its original painted eye.
            for ex,ez,coeff in self.lidfits:
                dx=p.x-ex;dz=p.z-ez;horizontal=g(dx,0,.037);vertical=g(dz,0,.042);eye=horizontal*vertical*front
                if key=='Blink':
                    plateau=1-smooth((abs(dz)-.038)/.020);across=1-smooth((abs(dx)-.037)/.023);amount=across*plateau*front
                    centerline=-.003+.003*(dx/.038)**2;q.z-=(dz-centerline)*.993*amount
                    closedz=dz*(1-.993*amount)+centerline*.993*amount
                    target=float(np.dot(coeff,[1,dx,closedz,dx*dx,dx*closedz,closedz*closedz]))-.0007
                    flatten=(1-smooth(((dx/.040)**2+(dz/.035)**2-1)/1.9))*front
                    q.y+=(target-q.y)*flatten
                elif key=='Smile':q.z-=dz*.13*eye
                elif key in ('Fear','Surprise'):q.z+=dz*(.09 if key=='Fear' else .17)*eye
            for side,(ex,ez,coeff) in zip((-1,1),self.lidfits):
                brow=g(p.x,ex,.056)*g(p.z,ez+.071,.027)*front
                if key=='Surprise':q.z+=.021*brow
                elif key=='Fear':q.z+=brow*(.016-.022*smooth(side*(p.x-ex)/.045))
                elif key=='Smile':q.z+=.003*brow
        if key in self.params:
            low,up,width=self.params[key];low*=self.amp;up*=self.amp;dx=p.x-self.cx;u=dx/self.width;f=math.sqrt(max(0,1-u*u))**1.4
            cutq=self.q(p);r=role or self.roles.get(index) or ('U' if cutq>=0 else 'L')
            fade=g(cutq,0,.076 if r=='L' else .031)*front
            q.z+=(up if r=='U' else -low)*f*fade
            q.y-=low*.18*f*fade
            q.x+=(q.x-self.final_cx)*(width-1)*g(dx,0,.106)*g(cutq,0,.065)*front
            corner=g(abs(dx),self.width*.83,.028)*g(cutq,0,.035)*front
            if key=='Fear':q.z-=.012*corner
            if key=='Smile':q.z+=.009*corner
            if key in ('Mouth_A','Mouth_E','Mouth_O','Surprise'):q.z-=.004*(min(1,abs(u))**2)*g(cutq,0,.035)*front*g(dx,0,.09)
            if key in ('Mouth_O','Surprise'):
                a,b,c=self.coef;mouthcurve=(a*(p.x-.018)**2+b*(p.x-.018))-(a*(self.cx-.018)**2+b*(self.cx-.018))
                q.z-=mouthcurve*.93*g(dx,0,.105)*g(cutq,0,.064)*front
                q.z-=.006*g(dx,0,.105)*g(cutq,0,.026)*front
        return q

    def append(self,p,source,weights,uv=(0,0)):
        n=len(self.v);self.v.append(Vector(p));self.src.append(Vector(source));self.weights.append(dict(weights));self.uv.append(Vector(uv));self.normals.append(Vector())
        for key,data in self.oldkeys.items():data.append(Vector(p))
        return n

    def interior(self,mouthmat,teethmat,tonguemat):
        self.extra={k:{} for k in KEYS}
        # A single ordered interior avoids doubled strips at UV seams.
        edges=set(e for pair in self.segments for e in pair);chain=sorted([self.cuts[(e,'U')] for e in edges],key=lambda i:self.src[i].x)
        unique=[]
        for i in chain:
            if not unique or abs(self.src[i].x-self.src[unique[-1]].x)>.0001:unique.append(i)
        N=128;R=14;rows=[];samples=[]
        for x in np.linspace(self.src[unique[0]].x,self.src[unique[-1]].x,N):
            a,b=unique[0],unique[1]
            for i,j in zip(unique[:-1],unique[1:]):
                if self.src[i].x<=x<=self.src[j].x:a,b=i,j;break
            t=(x-self.src[a].x)/max(1e-8,self.src[b].x-self.src[a].x);base=self.v[a].lerp(self.v[b],t);orig=self.src[a].lerp(self.src[b],t)
            samples.append((a,b,t,base,orig))
        for j in range(R):
            t=j/(R-1);depth=.014*math.sin(math.pi*t);row=[]
            for a,b,f,base,orig in samples:
                vi=self.append(base+Vector((0,depth,0)),orig,self.weights[a]);row.append(vi)
                for key in KEYS:
                    upper=self.expression(a,key,'U').lerp(self.expression(b,key,'U'),f);lower=self.expression(a,key,'L').lerp(self.expression(b,key,'L'),f)
                    self.extra[key][vi]=upper.lerp(lower,t)+Vector((0,depth,0))
            rows.append(row)
        for j in range(R-1):
            for i in range(N-1):self.faces.append((rows[j][i],rows[j+1][i],rows[j+1][i+1],rows[j][i+1]));self.mi.append(mouthmat)
        tooth=[]
        for a,b,f,base,orig in samples:
            if abs(orig.x-self.cx)>self.width*.68:continue
            ids=[self.append(base+Vector((0,.002,0)),orig,self.weights[a]) for _ in range(2)];tooth.append(ids)
            for key in KEYS:
                p=self.expression(a,key,'U').lerp(self.expression(b,key,'U'),f)+Vector((0,.002,0));amount=.0055 if key in ('Mouth_A','Mouth_E','Smile') else 0
                self.extra[key][ids[0]]=p;self.extra[key][ids[1]]=p+Vector((0,.001,-amount))
        for a,b in zip(tooth[:-1],tooth[1:]):self.faces.append((a[0],a[1],b[1],b[0]));self.mi.append(teethmat)

    def modeled_lids(self,lashmat):
        # Preserve the original eyes. Real upper/lower lids close over them, rather
        # than crushing painted irises into the surrounding cheek geometry.
        source_faces=[tuple(p.vertices) for p in self.old.polygons]
        src_bvh=BVHTree.FromPolygons(self.src[:len(self.old.vertices)],source_faces)
        def project(x,z):
            hit,n,pi,dist=src_bvh.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
            if hit is None:return None
            a,b,c=source_faces[pi];v0=self.src[b]-self.src[a];v1=self.src[c]-self.src[a];v2=hit-self.src[a]
            d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);d20=v2.dot(v0);d21=v2.dot(v1);den=d00*d11-d01*d01
            v=(d11*d20-d01*d21)/den;w=(d00*d21-d01*d20)/den;t=1-v-w
            return self.v[a]*t+self.v[b]*v+self.v[c]*w,self.uv[a]*t+self.uv[b]*v+self.uv[c]*w,self.weights[a],hit
        for side,(ex,ez,coeff) in zip((-1,1),self.lidfits):
            color_ref=project(ex+side*.050,ez+.026);anchor=color_ref[1]
            N=58;R=14;rx=.0365
            for upper in (True,False):
                ids=[]
                for j in range(R):
                    t=j/(R-1)
                    for i in range(N):
                        u=-1+2*i/(N-1);root=math.sqrt(max(.0001,1-u*u));x=ex+rx*u
                        outside=(.032 if upper else -.032)*root;close=-.009*root
                        # Resting lid is tucked under skin at the upper/lower rim.
                        neutral_z=ez+outside+(close-outside)*t*.002
                        closed_z=ez+outside*(1-t)+close*t
                        neutral=project(x,neutral_z);closed=project(x,closed_z)
                        if neutral is None or closed is None:raise RuntimeError('Lid projection missed anatomical face')
                        p=neutral[0]+Vector((0,.004,0));q=closed[0].copy();dx=x-ex;dz=closed_z-ez;target=float(np.dot(coeff,[1,dx,dz,dx*dx,dx*dz,dz*dz]));blend=smooth(t/.35)*root
                        q.y=q.y*(1-blend)+target*blend-(.0001+.0035*smooth(t/.30)*root)
                        color_at=project(ex+side*.050,closed_z)
                        vi=self.append(p,neutral[3],neutral[2],anchor);ids.append(vi)
                        for key in KEYS:self.extra[key][vi]=q if key=='Blink' else p
                for j in range(R-1):
                    for i in range(N-1):a=ids[j*N+i];b=ids[j*N+i+1];c=ids[(j+1)*N+i+1];d=ids[(j+1)*N+i];self.faces.append((d,c,b,a) if upper else (a,b,c,d));self.mi.append(0)
            # A thin, softly curved closed-lid crease covers the meeting edge.
            ids=[]
            for i in range(N):
                u=-1+2*i/(N-1);root=math.sqrt(max(.0001,1-u*u));x=ex+rx*u
                for dz in (-.00055,.00055):
                    q=project(x,ez-.009*root+dz*root);p=project(x,ez+.032*root)
                    vi=self.append(p[0]+Vector((0,.005,0)),p[3],p[2],anchor);ids.append(vi)
                    pos=q[0].copy();dx=x-ex;dz=-.009*root;target=float(np.dot(coeff,[1,dx,dz,dx*dx,dx*dz,dz*dz]));pos.y=pos.y*(1-root)+target*root-.0039
                    for key in KEYS:self.extra[key][vi]=pos if key=='Blink' else self.v[vi]
            for i in range(N-1):self.faces.append((ids[2*i+2],ids[2*i+3],ids[2*i+1],ids[2*i]));self.mi.append(lashmat)

    def build(self):
        mats=list(self.old.materials)
        for name,hex in [(self.name+' mouth interior','#4D2F33'),(self.name+' teeth','#F4E7CB'),(self.name+' tongue','#A66C72'),(self.name+' eyelid crease','#745140')]:
            m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=color(hex);p.inputs['Roughness'].default_value=.65;mats.append(m)
        self.refine_mouth();self.split_surface()
        # The editable family export was made from Unity's clockwise triangles.
        # Its source shells have negative signed volume in Blender. Correct the
        # inherited skin before adding the already outward-facing lids/cavity.
        self.faces=[tuple(reversed(face)) for face in self.faces]
        self.normals=[-normal for normal in self.normals]
        self.interior(len(mats)-4,len(mats)-3,len(mats)-2);self.modeled_lids(len(mats)-1)
        mesh=bpy.data.meshes.new(self.name+' facial articulation');mesh.from_pydata(self.v,[],self.faces);mesh.update()
        self.obj.data=mesh
        for m in mats:mesh.materials.append(m)
        for p,mi in zip(mesh.polygons,self.mi):p.material_index=mi;p.use_smooth=True
        uv=mesh.uv_layers.new(name='PreservedFamilyUV')
        for l in mesh.loops:uv.data[l.index].uv=self.uv[l.vertex_index]
        self.obj.vertex_groups.clear();groups=[self.obj.vertex_groups.new(name=n) for n in self.group_names]
        for i,w in enumerate(self.weights):
            for group,weight in w.items():groups[group].add([i],weight,'REPLACE')
        for name,data in self.oldkeys.items():
            k=self.obj.shape_key_add(name=name,from_mix=False);k.value=0
            for p,v in zip(k.data,data):p.co=v
        for name in KEYS:
            k=self.obj.shape_key_add(name=name,from_mix=False);k.value=0
            for i in range(self.skin_count):k.data[i].co=self.expression(i,name)
            for i,p in self.extra[name].items():k.data[i].co=p
        # Preserve the approved surface's interpolated shading across newly
        # tessellated triangles. Interior and eyelid normals remain geometric.
        if hasattr(mesh,'normals_split_custom_set_from_vertices'):mesh.normals_split_custom_set_from_vertices(self.normals)
        self.obj['facial_rig']='Textured anatomical surface with split lip seam, mouth cavity, teeth, tongue, blinking and brow/jaw blendshapes.'
        self.obj['source_winding_corrected']=True
        return {'source_vertices':len(self.old.vertices),'vertices':len(mesh.vertices),'cut_segments':len(self.segments),'mouth_center_x':self.cx,'facial_keys':list(KEYS),'preserved_keys':list(self.oldkeys)}

def render_review(obj,author):
    # Render a complete expression sheet from geometry. No bitmap substitutions.
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=48
    if not sc.world:sc.world=bpy.data.worlds.new('Face review world')
    sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.70,.78,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.30
    for name,pos,power,size in [('Key',(-3,-4,5),600,3),('Fill',(3,-2,3),150,3),('Rim',(1,3,4),300,2.5)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);sc.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1.4))-o.location).to_track_quat('-Z','Y').to_euler()
    eyez=sum(e[1] for e in author.lidfits)/2;weights=[g(p.z,eyez,.025)*g(p.x,.018,.14)*smooth((-p.y-.12)/.05) for p in author.src[:len(author.old.vertices)]];focus=sum(v.z*w for v,w in zip(author.v,weights))/sum(weights)
    d=bpy.data.cameras.new('Expression review');cam=bpy.data.objects.new('Expression review',d);sc.collection.objects.link(cam);sc.camera=cam;d.type='ORTHO';d.ortho_scale=.59 if author.name!='Ece' else .51;cam.location=(.045,-4,focus+.025);cam.rotation_euler=(Vector((.018,0,focus+.025))-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=-.15;sc.render.resolution_x=760;sc.render.resolution_y=760;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
    for name,values in [('Neutral',{}),('Speaking',{'Mouth_A':.85}),('ClosedEyes',{'Blink':1}),('Surprised',{'Surprise':.9}),('Worried',{'Fear':.85}),('Smile',{'Smile':.85})]:
        for k in obj.data.shape_keys.key_blocks:k.value=values.get(k.name,0)
        sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    for k in obj.data.shape_keys.key_blocks:k.value=0

def main():
    global OUT
    parser=argparse.ArgumentParser();parser.add_argument('--names',nargs='*',default=['Eren','Ece','Aylin','Zeynep']);parser.add_argument('--skip-render',action='store_true');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for name in args.names:
        template={'Eren':'Emre','Ece':'Ada','Aylin':'Ada','Zeynep':'Derya','Deniz':'Emre','Gul':'Derya','Kemal':'Yusuf','Arda':'Efe'}[name];OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name
        bpy.ops.wm.open_mainfile(filepath=str(OUT/(name+'.blend')));bpy.context.preferences.filepaths.save_version=0
        source=json.loads((ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'/(template+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
        trace=json.loads((ROOT/'.codex_tmp'/('eren-mouth-trace.json' if template=='Emre' else template.lower()+'-mouth-trace.json')).read_text())
        obj=bpy.data.objects[name+'_SculptedBody'];author=SurfaceExpressions(obj,source,trace,name);report=author.build()
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'_Expressions.blend')));(OUT/'expression-authoring.json').write_text(json.dumps(report,indent=2))
        if not args.skip_render:render_review(obj,author)
        print('SURFACE_EXPRESSIONS',name,json.dumps(report),flush=True)

if __name__=='__main__':main()
