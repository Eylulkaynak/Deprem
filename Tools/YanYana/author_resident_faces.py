"""Blender character authoring, kept outside Unity until visual review passes.

Approved anatomy is a sculpting template. Facial proportions, face topology,
modeled eyes/lids/mouth, expressions, hair and clothing are authored separately.
This is an offline asset tool, not game runtime code.
"""
from __future__ import annotations
import argparse, json, math, pathlib, sys
import bpy, bmesh, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import color, stage, Sculpture, CAST
OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentSculpts'
SHAPES=('Mouth_A','Mouth_E','Mouth_O','Blink','Fear','Surprise','Smile')

def clamp(t):return max(0,min(1,t))
def smooth(t):t=clamp(t);return t*t*(3-2*t)
def gauss(x,z,cx,cz,rx,rz):return math.exp(-((x-cx)/rx)**2-((z-cz)/rz)**2)

def material(name,hex,rough=.66):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=color(hex)
    p.inputs['Roughness'].default_value=rough;p.inputs['Specular IOR Level'].default_value=.2
    m.diffuse_color=color(hex)
    return m

class Face:
    def __init__(self,spec):
        self.s=spec;self.name=spec['name'];self.v=[];self.f=[];self.mi=[];self.colors=[];self.keys={k:[] for k in SHAPES}
        self.skin=bpy.data.materials.new(self.name+' painted skin');self.skin.use_nodes=True
        p=self.skin.node_tree.nodes['Principled BSDF'];p.inputs['Roughness'].default_value=.66;p.inputs['Specular IOR Level'].default_value=.22
        p.inputs['Subsurface Weight'].default_value=.045
        attr=self.skin.node_tree.nodes.new('ShaderNodeVertexColor');attr.layer_name='SkinPaint';self.skin.node_tree.links.new(attr.outputs['Color'],p.inputs['Base Color'])
        self.mats=[self.skin,material('Eye ivory','#F5EEE5',.4),material('Iris amber','#50351E',.4),material('Pupil','#181519',.3),material('Eye reflection','#FFFDF5',.25),material('Lid edge','#6C4439'),material('Mouth cavity','#593338'),material('Soft lip','#B9826F'),material('Teeth','#FFF2D9'),material('Tongue','#C27B7C')]
        for i in (1,2,3):
            m=self.mats[i];a=m.node_tree.nodes.new('ShaderNodeVertexColor');a.layer_name='SkinPaint';m.node_tree.links.new(a.outputs['Color'],m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
        self.cx=.012;self.ez=.905;self.ex=.071;self.erx=.032;self.erz=.028;self.mz=.823

    def add(self,name,verts,faces,mat=0,keys=None,colors=None):
        start=len(self.v);self.v.extend([tuple(p) for p in verts]);self.f.extend([tuple(start+i for i in f) for f in faces]);self.mi.extend([mat]*len(faces))
        self.colors.extend(colors or [self.skincolor(Vector(p)) for p in verts])
        for k in SHAPES:self.keys[k].extend([tuple(p) for p in (keys or {}).get(k,verts)])

    def sculpt(self):
        self.scene=bpy.context.scene;s=self.s
        self.ez=s.get('eye_z',.906);self.ex=s.get('eye_spacing',.072);self.erx=s.get('eye_width',.032);self.erz=s.get('eye_height',.030);self.mz=s.get('mouth_z',.824)
        self.basecolor=color(s.get('skin','#E7AF87'))
        # New continuous topology, fitted to measured family proportions. It does
        # not contain the old character mesh, painted eyes or torn hair boundaries.
        self.profile=[(.778,.002,.002,-.035),(.786,.062,.064,-.035),(.798,.104,.096,-.034),(.816,.143,.128,-.026),(.845,.177,.157,-.016),(.880,.184,.178,-.003),(.925,.178,.187,.001),(.975,.179,.172,.0),(1.025,.172,.157,.008),(1.072,.151,.133,.012),(1.115,.115,.095,.012),(1.150,.035,.030,.012),(1.156,.001,.001,.012)]
        vertices=[];N=144;R=148
        for j in range(R+1):
            z=.778+(1.156-.778)*j/R;wx,dy,cy=self.profile_at(z)
            for i in range(N):
                a=-math.pi+math.tau*i/N;x=self.cx+math.sin(a)*wx
                if math.cos(a)>=0:y=self.surface(x,z)
                else:y=cy+dy*math.sqrt(max(0,1-math.sin(a)**2))
                vertices.append(Vector((x,y,z)))
        keep=[]
        for j in range(R):
          for i in range(N):
            inds=(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i)
            mid=sum((vertices[q] for q in inds),Vector())/4
            eye=any(((mid.x-self.cx-side*self.ex)/(self.erx*1.21))**2+((mid.z-self.ez)/(self.erz*1.22))**2<1 for side in (-1,1))
            mouth=((mid.x-self.cx)/.077)**2+((mid.z-self.mz)/.035)**2<1
            if mid.y<-.09 and (eye or mouth):continue
            keep.append(inds)
        keys={}
        for k in SHAPES:
            vs=[]
            for p in vertices:
                vs.append(self.jaw(p,k))
            keys[k]=vs
        self.add('Sculpted skin',vertices,keep,keys=keys)

    def profile_at(self,z):
        values=self.profile
        if z<=values[0][0]:return values[0][1:]
        if z>=values[-1][0]:return values[-1][1:]
        for j in range(len(values)-1):
            if values[j][0]<=z<=values[j+1][0]:
                a=values[j];b=values[j+1];t=(z-a[0])/(b[0]-a[0]);out=[]
                # Cubic Hermite interpolation avoids ridges between profile rings.
                for k in range(1,4):
                    before=values[max(0,j-1)];after=values[min(len(values)-1,j+2)]
                    ma=(b[k]-before[k])/(b[0]-before[0])*(b[0]-a[0]);mb=(after[k]-a[k])/(after[0]-a[0])*(b[0]-a[0])
                    out.append((2*t**3-3*t*t+1)*a[k]+(t**3-2*t*t+t)*ma+(-2*t**3+3*t*t)*b[k]+(t**3-t*t)*mb)
                cheek=math.exp(-((z-.858)/.068)**2);jaw=math.exp(-((z-.787)/.046)**2)
                out[0]*=self.s.get('face_width',1)+self.s.get('cheeks',0)*cheek+self.s.get('chin',0)*jaw
                return out

    def surface(self,x,z):
        wx,dy,cy=self.profile_at(z);u=abs((x-self.cx)/max(.001,wx))
        y=cy-dy*max(0,1-min(1,u)**2.7)**(1/2.7)
        y-=self.s.get('nose_size',.042)*gauss(x,z,self.cx,.866,.031,.024)
        y-=.005*gauss(x,z,self.cx,.889,.017,.037)
        y-=.004*sum(gauss(x,z,self.cx+side*.108,.859,.040,.039) for side in (-1,1))
        return y

    def jaw(self,p,key):
        q=Vector(p);w=gauss(q.x,q.z,self.cx,.789,.090,.049)*clamp((-q.y-.04)/.09)
        q.z-=w*dict(Mouth_A=.009,Mouth_E=.002,Mouth_O=.006,Surprise=.008,Fear=.003).get(key,0)
        return q

    def skincolor(self,p):
        rgb=list(self.basecolor[:3])
        # Very subtle warm cheek glaze, painted on the new face instead of copying features.
        blush=sum(gauss(p.x,p.z,self.cx+side*.117,.866,.031,.027) for side in (-1,1))*.13
        rgb[1]*=1-blush;rgb[2]*=1-blush*.6
        return (*rgb,1)

    def eye(self,side):
        cx=self.cx+side*self.ex;ez=self.ez;rx=self.erx;rz=self.erz;N=64;R=9
        def eyeloc(a,r,key='Basis',push=0):
            h={'Blink':.012,'Smile':.79,'Fear':1.06,'Surprise':1.14}.get(key,1)
            tilt=side*.07;dx=math.cos(a)*rx*r;dz=math.sin(a)*rz*r
            if r<=1:dz*=h
            else:dz*=h+(1-h)*smooth((r-1)/.52)
            x=cx+dx;z=ez+dz+dx*tilt
            y=self.surface(x,z)-.0002-.006*max(0,1-r*r)-push
            return Vector((x,y,z))
        # Eyelid skin goes from the opening to a wide, surface-matched outer rim.
        def lids(k):return [eyeloc(i*math.tau/N,1+.53*j/(R-1),k) for j in range(R) for i in range(N)]
        fs=[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(R-1) for i in range(N)]
        self.add('Continuous eyelid',lids('Basis'),fs,keys={k:lids(k) for k in SHAPES})
        def disk(k,radius=1,dx=0,dz=0,push=0):
            out=[]
            for j in range(7):
                r=radius*(.001+.999*j/6)
                for i in range(N):
                    p=eyeloc(i*math.tau/N,r,k,push);p.x+=dx;p.z+=dz*(.012 if k=='Blink' else 1);out.append(p)
            return out
        df=[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(6) for i in range(N)]
        start=len(self.mi);cols=[]
        for j in range(7):
            for i in range(N):
                a=i*math.tau/N
                if j<=3:c=color('#191A20')
                elif j<=5:
                    base=color('#906337');f=.73+.12*math.sin(a*17)+.12*math.sin(a*29)+.17*max(0,-math.sin(a));c=tuple(q*f for q in base[:3])+(1,)
                else:c=color('#F6F0E8')
                cols.append(c)
        self.add('One continuous eyeball',disk('Basis'),df,1,keys={k:disk(k) for k in SHAPES},colors=cols)
        for j in range(6):
            mat=3 if j<3 else 2 if j<5 else 1
            for i in range(N):self.mi[start+j*N+i]=mat
        # One restrained light catch; small enough to read like the approved family.
        def highlight(k):
            center=eyeloc(2.08,.47,k,.0012);out=[]
            for i in range(20):
                a=i*math.tau/20;out.append(center+Vector((math.cos(a)*.0035,0,math.sin(a)*.0040*(.012 if k=='Blink' else 1))))
            return out
        self.add('Eye catchlight',highlight('Basis'),[tuple(range(20))],4,keys={k:highlight(k) for k in SHAPES})
        # The upper edge is a fine tapered lash, not a thick ring around the eye.
        def lash(k):
            out=[]
            for j in range(36):
                a=math.pi*j/35
                for q in (-1,1):
                    p=eyeloc(a,1+q*.022,k,.0004);out.append(p)
            return out
        self.add('Upper lid edge',lash('Basis'),[(j*2,j*2+1,j*2+3,j*2+2) for j in range(35)],5,keys={k:lash(k) for k in SHAPES})

    def brows(self):
        for side in (-1,1):
            def shape(k):
                out=[]
                for j in range(28):
                    t=j/27;x=self.cx+side*(.029+.095*t);z=.962+.012*math.sin(t*math.pi)-.010*t
                    if k=='Fear':z+=(1-t)*.022-t*.006
                    if k=='Surprise':z+=.024
                    if k=='Smile':z+=.003
                    width=.0048*(.35+.65*math.sin(t*math.pi))
                    for q in (-1,1):out.append((x,self.surface(x,z)-.002,z+q*width))
                return out
            self.add('Sculpted eyebrow',shape('Basis'),[(2*j,2*j+1,2*j+3,2*j+2) for j in range(27)],5,keys={k:shape(k) for k in SHAPES})

    def mouth(self):
        N=80;R=12
        def outline(a,k):
            width,height,curve={'Basis':(.053,.0016,.012),'Mouth_A':(.047,.021,.008),'Mouth_E':(.061,.009,.011),'Mouth_O':(.025,.020,.002),'Fear':(.044,.010,-.009),'Surprise':(.029,.024,.001),'Smile':(.061,.006,.016),'Blink':(.053,.0016,.012)}[k]
            u=math.cos(a);v=math.sin(a)
            return Vector((self.cx+width*u,0,self.mz+height*v+curve*u*u))
        def patch(k):
            out=[]
            for j in range(R):
                t=j/(R-1)
                for i in range(N):
                    a=i*math.tau/N;inner=outline(a,k);outer=Vector((self.cx+math.cos(a)*.095,0,self.mz+math.sin(a)*.044))
                    p=inner.lerp(outer,t);p.y=self.surface(p.x,p.z)-.0001-.0008*(1-t)**2;out.append(self.jaw(p,k))
            return out
        fs=[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(R-1) for i in range(N)]
        self.add('Continuous mouth skin',patch('Basis'),fs,keys={k:patch(k) for k in SHAPES})
        def cavity(k):
            out=[]
            for j in range(6):
                r=.001+.999*j/5
                for i in range(N):
                    p=outline(i*math.tau/N,k);p.x=self.cx+(p.x-self.cx)*r;p.z=self.mz+(p.z-self.mz)*r
                    p.y=self.surface(p.x,p.z)+.0015+.005*(1-r*r);out.append(self.jaw(p,k))
            return out
        self.add('Mouth interior',cavity('Basis'),[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(5) for i in range(N)],6,keys={k:cavity(k) for k in SHAPES})
        def lip(k):
            out=[]
            for i in range(N):
                a=i*math.tau/N
                for r in (1,1.035):
                    p=outline(a,k);p.x=self.cx+(p.x-self.cx)*r;p.z+=math.sin(a)*.0006*(r-1)/.035;p.y=self.surface(p.x,p.z)-.0024;out.append(self.jaw(p,k))
            return out
        self.add('Subtle mouth edge',lip('Basis'),[(2*i,2*i+1,2*((i+1)%N)+1,2*((i+1)%N)) for i in range(N)],7,keys={k:lip(k) for k in SHAPES})
        def teeth(k):
            opened=k in ('Mouth_A','Mouth_E','Fear','Smile')
            out=[]
            for i in range(28):
                a=.64+(math.pi-1.28)*i/27;p=outline(a,k)
                for v in (0,1):
                    q=p.copy();q.z-=(.005 if opened else .00005)*v;q.y=self.surface(q.x,q.z)+.0002;out.append(self.jaw(q,k))
            return out
        self.add('Upper teeth',teeth('Basis'),[(2*i,2*i+1,2*i+3,2*i+2) for i in range(27)],8,keys={k:teeth(k) for k in SHAPES})

    def ears(self):
        for side in (-1,1):
            vs=[];fs=[];N=40;R=20
            for j in range(R+1):
                phi=math.pi*j/R
                for i in range(N):
                    a=i*math.tau/N;x=self.cx+side*.184+math.sin(phi)*math.cos(a)*.031;y=-.002+math.sin(phi)*math.sin(a)*.024;z=.881+math.cos(phi)*.045
                    vs.append((x,y,z))
            for j in range(R):
                for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
            cols=[]
            for p in vs:
                c=list(self.skincolor(Vector(p)));c[1]*=.93;c[2]*=.96;cols.append(c)
            self.add('Soft modeled ear',vs,fs,colors=cols)

    def finish(self):
        mesh=bpy.data.meshes.new(self.name+' authored facial surface');mesh.from_pydata(self.v,[],self.f);mesh.update()
        obj=bpy.data.objects.new('FaceSurface',mesh);bpy.context.collection.objects.link(obj)
        for m in self.mats:mesh.materials.append(m)
        for p,mi in zip(mesh.polygons,self.mi):p.material_index=mi;p.use_smooth=True
        attr=mesh.color_attributes.new(name='SkinPaint',type='FLOAT_COLOR',domain='POINT')
        for v,c in zip(attr.data,self.colors):v.color=c
        obj.shape_key_add(name='Basis',from_mix=False).value=0
        for k in SHAPES:
            key=obj.shape_key_add(name=k,from_mix=False);key.value=0
            for p,q in zip(key.data,self.keys[k]):p.co=q
        obj['authorship']='New Blender mesh and facial topology, fitted to measurements of the approved visual style.'
        return obj

def bob(face):
    verts=[];faces=[];N=128;R=40;cx=face.cx
    for j in range(R+1):
        t=j/R
        for i in range(N):
            a=-math.pi+math.tau*i/N;aa=abs(a);transition=smooth((aa-.87)/.37)
            fringe=1.025-.037*math.exp(-((a-.22)/.33)**2)-.026*math.exp(-((a+.47)/.25)**2)
            bottom=(1-transition)*fringe+transition*(.781+.008*math.cos(a*3))
            z=1.170+(bottom-1.170)*t
            rad=math.sqrt(max(.0001,1-((z-.972)/.198)**2)) if z>=.972 else 1.0+.065*math.sin((.972-z)/.205*math.pi)
            rad+=.006*math.cos(a*37+.4*math.sin(t*math.pi))*math.sin(t*math.pi*.85)
            verts.append((cx+math.sin(a)*.205*rad,-math.cos(a)*(.225 if math.cos(a)>0 else .189)*rad+.004,z))
    for j in range(R):
        for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    faces.append(tuple(reversed(range(N))))
    for i in range(N):
        x,y,z=verts[R*N+i];verts.append((cx+(x-cx)*.93,y*.93,z+.012))
    for i in range(N):faces.append((R*N+i,R*N+(i+1)%N,(R+1)*N+(i+1)%N,(R+1)*N+i))
    me=bpy.data.meshes.new('Sculpted bob');me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new('Sculpted bob',me);bpy.context.collection.objects.link(ob);me.materials.append(material('Chestnut hair','#342720',.76))
    for p in me.polygons:p.use_smooth=True
    sub=ob.modifiers.new('Smooth silhouette','SUBSURF');sub.levels=1
    return ob

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version=0
    # Single-character review gate: no automatic batch generation or Unity import.
    face=Face(dict(name='Ece',face_width=1.01,cheeks=.06,chin=-.06,face_length=.92,eye_spacing=.071))
    face.sculpt();face.eye(-1);face.eye(1);face.brows();face.mouth();face.ears();obj=face.finish();hair=bob(face)
    out=OUT/'Ece';out.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Ece_FacialStudy.blend'))
    cam=stage(obj,None);cam.data.ortho_scale=.56;cam.location=(.012,-4,.966);cam.rotation_euler=(Vector((.012,0,.951))-cam.location).to_track_quat('-Z','Y').to_euler()
    sc=bpy.context.scene;sc.cycles.samples=32;sc.render.resolution_x=640;sc.render.resolution_y=640;sc.render.resolution_percentage=100
    for label,values in [('Neutral',{}),('Speaking',{'Mouth_A':.85}),('Blink',{'Blink':1}),('Surprise',{'Surprise':.8}),('Fear',{'Fear':.8})]:
        for k in obj.data.shape_keys.key_blocks:k.value=values.get(k.name,0)
        sc.render.filepath=str(out/(label+'.png'));bpy.ops.render.render(write_still=True)
    for k in obj.data.shape_keys.key_blocks:k.value=0
    print('FACIAL_STUDY_READY',out,flush=True)

if __name__=='__main__':main()
