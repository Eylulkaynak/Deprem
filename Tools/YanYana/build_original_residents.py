"""Author original residents in Blender, with modeled faces and editable expression keys.

Offline art production only. No imported character geometry, no Unity runtime code.
Run: blender --background --python this_file.py -- --names Ece Eren --render
"""
from __future__ import annotations
import argparse, hashlib, json, math, pathlib, random, sys
import bpy
from mathutils import Vector

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/YanYana/Characters/NewResidents'
# Rejected batch is quarantined outside Unity. Drafts must stay outside Assets.
UNITY = OUT / 'DraftFBX'
SHAPES = ('Mouth_A', 'Mouth_E', 'Mouth_O', 'Blink', 'Fear', 'Surprise', 'Smile')

def mix(a,b,t): return a+(b-a)*t
def clamp(x,a=0,b=1): return max(a,min(b,x))
def smooth(x): x=clamp(x); return x*x*(3-2*x)
def color(value):
    v=value.lstrip('#'); c=[int(v[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(x/12.92 if x<.04045 else ((x+.055)/1.055)**2.4 for x in c)+(1,)

# Face proportions, silhouettes and hairstyles are individually authored. Heights
# refer to the model, rather than scaling one source mesh into different people.
CAST = [
 dict(name='Ece', h=1.18, age='child', sex='f', head=(1.06,.94,1.02), jaw=.78, cheek=1.13, eyes=(1.10,1.10,1.02), nose=(.83,.85), hair='bob', haircolor='#241A1E', skin='#E8AD7B', top='#B491CA', bottom='#61416F', shoe='#CD655B', outfit='dungarees', build=.89, freckles=True),
 dict(name='Aylin', h=1.77, age='adult', sex='f', head=(.90,1.07,1.09), jaw=.70, cheek=1.00, eyes=(.92,.92,.95), nose=(.88,1.15), hair='curly_pony', haircolor='#70331C', skin='#D89A69', top='#77815A', bottom='#494A46', shoe='#70442E', outfit='jacket', build=.94),
 dict(name='Zeynep', h=1.64, age='adult', sex='f', head=(1.00,.96,.95), jaw=.98, cheek=1.04, eyes=(1.00,.92,1.03), nose=(1.14,.88), hair='braids', haircolor='#231914', skin='#BA7D50', top='#BDD050', bottom='#30425B', shoe='#38352F', outfit='vest', build=1.02),
 dict(name='Eren', h=1.76, age='adult', sex='m', head=(1.08,1.12,1.01), jaw=1.09, cheek=1.16, eyes=(.91,.96,.96), nose=(1.19,1.11), hair='receding', haircolor='#271E18', skin='#BD8257', top='#E98639', bottom='#A0926C', shoe='#3D3B35', outfit='vest', build=1.30),
 dict(name='Mina', h=1.12, age='child', sex='f', head=(.95,1.02,1.03), jaw=.71, cheek=1.08, eyes=(1.05,1.12,1.02), nose=(.88,.78), hair='pigtails', haircolor='#5D311F', skin='#D99A6C', top='#E48C7A', bottom='#487C89', shoe='#E2B65C', outfit='dress', build=.85),
 dict(name='Can', h=1.26, age='child', sex='m', head=(1.02,.91,1.01), jaw=.86, cheek=1.05, eyes=(1.03,.96,1.08), nose=(.98,.88), hair='curls', haircolor='#1F1918', skin='#B5784F', top='#D7A94E', bottom='#526A74', shoe='#E5DDD0', outfit='shorts', build=.96),
 dict(name='Elif', h=1.35, age='child', sex='f', head=(.90,1.01,1.13), jaw=.70, cheek=.99, eyes=(.94,1.06,.95), nose=(.82,1.0), hair='side_pony', haircolor='#8D4327', skin='#E9B080', top='#6E9E98', bottom='#D1B07F', shoe='#8B5845', outfit='tunic', build=.88, freckles=True),
 dict(name='Arda', h=1.17, age='child', sex='m', head=(1.13,.97,.95), jaw=.90, cheek=1.15, eyes=(.97,1.02,1.07), nose=(1.12,.76), hair='spikes', haircolor='#493026', skin='#D7A074', top='#7E94C1', bottom='#805F4B', shoe='#D79A4C', outfit='shorts', build=1.07),
 dict(name='Lale', h=1.08, age='child', sex='f', head=(1.03,1.04,.97), jaw=.85, cheek=1.11, eyes=(1.13,1.04,.95), nose=(.89,.81), hair='buns', haircolor='#231B19', skin='#A56748', top='#D6A9AA', bottom='#6F8296', shoe='#D5C277', outfit='dungarees', build=.94),
 dict(name='Umut', h=1.42, age='child', sex='m', head=(.91,.99,1.08), jaw=.82, cheek=.97, eyes=(.94,.91,1.0), nose=(.89,1.16), hair='sidepart', haircolor='#3C2D23', skin='#D5A27B', top='#B9654A', bottom='#465967', shoe='#D4C8AA', outfit='hoodie', build=.88),
 dict(name='Deniz', h=1.83, age='adult', sex='m', head=(.92,1.00,1.08), jaw=.98, cheek=.99, eyes=(.94,.94,1.02), nose=(.92,1.13), hair='quiff', haircolor='#281F1B', skin='#BF8E69', top='#577C95', bottom='#C5AF88', shoe='#685348', outfit='shirt', build=1.02),
 dict(name='Asli', h=1.61, age='adult', sex='f', head=(1.07,.96,.96), jaw=.78, cheek=1.16, eyes=(1.06,.98,1.0), nose=(.97,.84), hair='wavy', haircolor='#4B2520', skin='#DEA276', top='#A15764', bottom='#737A58', shoe='#675043', outfit='cardigan', build=1.12),
 dict(name='Ozan', h=1.91, age='adult', sex='m', head=(.88,1.05,1.11), jaw=.86, cheek=.96, eyes=(.89,.98,.97), nose=(.84,1.28), hair='crop', haircolor='#47362D', skin='#D6A583', top='#687D65', bottom='#454D5C', shoe='#D0C1A9', outfit='polo', build=.88),
 dict(name='Selma', h=1.69, age='adult', sex='f', head=(.94,1.02,1.02), jaw=.89, cheek=1.02, eyes=(.95,1.01,1.07), nose=(1.03,1.03), hair='pixie', haircolor='#55392C', skin='#C58C63', top='#AE8757', bottom='#5A6580', shoe='#5A3829', outfit='tunic', build=.96),
 dict(name='Mert', h=1.74, age='adult', sex='m', head=(1.08,1.00,.98), jaw=1.09, cheek=1.05, eyes=(1.04,.92,1.01), nose=(1.08,.98), hair='curls', haircolor='#201B19', skin='#9C6548', top='#76659B', bottom='#84745B', shoe='#48453E', outfit='hoodie', build=1.14),
 dict(name='Gul', h=1.57, age='adult', sex='f', head=(1.10,1.07,.96), jaw=.91, cheek=1.13, eyes=(1.01,.95,.99), nose=(1.10,.91), hair='low_bun', haircolor='#5D3825', skin='#D7A07C', top='#6A947F', bottom='#927652', shoe='#654234', outfit='dress', build=1.17),
 dict(name='Cem', h=1.80, age='adult', sex='m', head=(.99,1.08,1.01), jaw=.88, cheek=1.09, eyes=(.96,1.04,1.03), nose=(1.03,1.08), hair='waves', haircolor='#422923', skin='#CD956A', top='#B77351', bottom='#365A69', shoe='#DBCEAD', outfit='jacket', build=1.05),
 dict(name='Nermin', h=1.65, age='adult', sex='f', head=(.88,.95,1.13), jaw=.66, cheek=1.01, eyes=(.94,1.06,1.05), nose=(.81,1.16), hair='long_braid', haircolor='#342923', skin='#D4A282', top='#688FA9', bottom='#BAA787', shoe='#865746', outfit='cardigan', build=.87),
 dict(name='Orhan', h=1.68, age='adult', sex='m', head=(1.09,.93,1.00), jaw=1.02, cheek=1.07, eyes=(.91,.99,1.07), nose=(1.19,.94), hair='bald', haircolor='#48392E', skin='#CB9369', top='#B3A178', bottom='#5D6F77', shoe='#624C36', outfit='polo', build=1.16),
 dict(name='Suna', h=1.75, age='adult', sex='f', head=(.97,1.00,1.05), jaw=.76, cheek=1.04, eyes=(1.02,1.01,.95), nose=(.95,1.05), hair='coils', haircolor='#34231B', skin='#AC724E', top='#CC9670', bottom='#675978', shoe='#513B30', outfit='shirt', build=1.00),
 dict(name='Kemal', h=1.73, age='elder', sex='m', head=(.95,1.09,1.10), jaw=.89, cheek=.97, eyes=(.89,.90,1.02), nose=(1.07,1.28), hair='receding', haircolor='#A5A19A', skin='#D2A080', top='#8C8267', bottom='#64615A', shoe='#5C4635', outfit='cardigan', build=.95),
 dict(name='Ali', h=1.62, age='elder', sex='m', head=(1.10,1.04,.94), jaw=1.07, cheek=1.13, eyes=(.97,.88,1.01), nose=(1.25,1.03), hair='bald', haircolor='#D1CCC1', skin='#C49373', top='#678582', bottom='#8B7D66', shoe='#51463B', outfit='shirt', build=1.17),
]

class Sculpture:
    def __init__(self,spec):
        self.s=spec; self.h=spec['h']; self.v=[]; self.f=[]; self.mi=[]; self.weights=[]; self.uv=[]; self.keys={n:{} for n in SHAPES}; self.parts=[]
        self.materials=[]; self.matindex={}; self.matmeta=[]
        self.child=spec['age']=='child'; self.w=spec['h']*.116*spec['head'][0]*(1.21 if self.child else 1)
        self.d=spec['h']*.096*spec['head'][1]*(1.20 if self.child else 1)
        self.hh=spec['h']*(.345 if self.child else .272)*spec['head'][2]
        self.hz=spec['h']-self.hh*.48
        self.eye_z=self.hz+self.hh*.022; self.eye_x=self.w*.45*spec['eyes'][2]
        self.mouth_z=self.hz-self.hh*.265; self.mw=self.w*.35; self.face_y=-self.d*.967
        self.make_material('Skin',spec['skin'],.56,subsurface=.075)
        self.make_material('Lip','#AD6752',.48,subsurface=.04)
        self.make_material('Mouth','#361920',.68)
        self.make_material('Tongue','#BD6970',.45)
        self.make_material('Ivory','#FFF4DD',.32)
        self.make_material('EyeWhite','#FDF5E6',.16)
        self.make_material('Iris','#815127' if spec['name'] not in ('Ece','Elif','Cem','Suna') else '#6D7941',.23)
        self.make_material('Pupil','#131115',.13)
        self.make_material('Hair',spec['haircolor'],.68)
        self.make_material('HairLight',self.lighten(spec['haircolor'],.065),.64)
        self.make_material('Top',spec['top'],.83)
        self.make_material('Bottom',spec['bottom'],.82)
        self.make_material('Shoe',spec['shoe'],.60)
        self.make_material('Cream','#E6DAC1',.79)
        self.make_material('Detail','#DAAC5A',.40)
        self.make_material('Under',spec['top'] if spec['outfit']=='dungarees' else '#7292B2' if spec['outfit']=='vest' else '#E4CEB0',.80)
        self.make_material('Seam',self.lighten(spec['top'],-.22),.8)

    @staticmethod
    def lighten(c,amount):
        v=[int(c.lstrip('#')[i:i+2],16) for i in (0,2,4)]
        return '#'+''.join(f'{round(clamp(x/255+amount)*255):02X}' for x in v)

    def make_material(self,n,c,rough,subsurface=0):
        m=bpy.data.materials.new(self.s['name']+'_'+n); m.use_nodes=True
        bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=color(c); bs.inputs['Roughness'].default_value=rough
        bs.inputs['Subsurface Weight'].default_value=subsurface
        if n in ('Top','Bottom','Under'):
            tex=m.node_tree.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=165;tex.inputs['Detail'].default_value=2
            bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.13;bump.inputs['Distance'].default_value=.0008
            m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
        self.matindex[n]=len(self.materials);self.materials.append(m);self.matmeta.append(dict(name=m.name,color=c,roughness=rough))

    def add(self,name,verts,faces,mat,bone='Head',morph=None,uv=None):
        start=len(self.v);self.v.extend([tuple(p) for p in verts]);self.f.extend([tuple(start+i for i in f) for f in faces]);self.mi.extend([self.matindex[mat]]*len(faces));self.parts.append(dict(name=name,start=start,count=len(verts)))
        self.weights.extend([bone(p) if callable(bone) else {bone:1} for p in verts]);self.uv.extend(uv or [(0,0)]*len(verts))
        if morph:
            for key,vs in morph.items():
                for i,p in enumerate(vs): self.keys[key][start+i]=tuple(p)

    def ellipsoid(self,name,center,scale,mat,bone='Head',seg=20,rings=12,rot=None,morph=None):
        coords=[];uv=[]
        for j in range(rings+1):
            phi=math.pi*j/rings
            for i in range(seg):
                a=2*math.pi*i/seg;u=Vector((math.sin(phi)*math.cos(a),math.sin(phi)*math.sin(a),math.cos(phi)))
                p=Vector((u.x*scale[0],u.y*scale[1],u.z*scale[2]))
                if rot:p=rot@p
                coords.append(tuple(p+Vector(center)));uv.append((i/seg,j/rings))
        faces=[(j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i) for j in range(rings) for i in range(seg)]
        keys={key:[fn(Vector(p)) for p in coords] for key,fn in (morph or {}).items()}
        self.add(name,coords,faces,mat,bone,keys,uv)

    def tube(self,name,path,radius,mat,bone='Head',sides=8,morph=None):
        v=[];uv=[];p=[Vector(x) for x in path]
        for i,c in enumerate(p):
            tangent=(p[min(i+1,len(p)-1)]-p[max(0,i-1)]).normalized();u=tangent.cross(Vector((0,-1,0)))
            if u.length<.01:u=tangent.cross(Vector((1,0,0)))
            u.normalize();w=tangent.cross(u).normalized();r=radius[i] if isinstance(radius,list) else radius
            for k in range(sides):
                t=k*math.tau/sides;v.append(tuple(c+r*(math.cos(t)*u+math.sin(t)*w)));uv.append((k/sides,i/(len(p)-1)))
        f=[(i*sides+k,i*sides+(k+1)%sides,(i+1)*sides+(k+1)%sides,(i+1)*sides+k) for i in range(len(p)-1) for k in range(sides)]
        f.extend([tuple(reversed(range(sides))),tuple((len(p)-1)*sides+k for k in range(sides))])
        keys={key:[fn(Vector(x)) for x in v] for key,fn in (morph or {}).items()};self.add(name,v,f,mat,bone,keys,uv)

    def loft(self,name,rings,mat,bone,sides=28):
        v=[];uv=[]
        for j,(x,y,z,rx,ry) in enumerate(rings):
            for i in range(sides):
                t=i*math.tau/sides;v.append((x+math.sin(t)*rx,y-math.cos(t)*ry,z));uv.append((i/sides,j/(len(rings)-1)))
        faces=[(j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i) for j in range(len(rings)-1) for i in range(sides)]
        faces.extend([tuple(reversed(range(sides))),tuple((len(rings)-1)*sides+i for i in range(sides))]);self.add(name,v,faces,mat,bone,uv=uv)

    def head_surface(self,t,a):
        z=self.hz+(t-.5)*self.hh
        radial=math.sqrt(max(.00001,1-(2*t-1)**2))
        jaw=mix(self.s['jaw'],1,smooth(t/.58))
        cheek=1+(self.s['cheek']-1)*math.exp(-((t-.38)/.20)**2)
        x=self.w*radial*jaw*cheek*math.sin(a)
        y=-self.d*radial*math.cos(a)
        # A shallow sculpted bridge and cheek pads are part of the head surface.
        front=max(0,math.cos(a))**8
        y-=self.d*.065*front*math.exp(-((t-.32)/.13)**2)
        if self.s.get('sculpted_nose'):
            nz=self.hz-self.hh*.077
            y-=front*self.d*.30*math.exp(-((x/(self.w*.17))**2+((z-nz)/(self.hh*.073))**2))
            y-=front*self.d*.085*math.exp(-((x/(self.w*.105))**2+((z-nz-self.hh*.07)/(self.hh*.13))**2))
        return Vector((x,y,z))

    def face(self):
        seg=128;rings=84;v=[];uv=[]
        for j in range(rings+1):
            t=.001+.998*j/rings
            for i in range(seg):v.append(self.head_surface(t,-math.pi+math.tau*i/seg));uv.append((i/seg,t))
        # The mouth has an actual hole in the head, bounded by a skin patch.
        i0,i1,j0,j1=52,76,12,28
        faces=[]
        ex=self.w*.288*self.s['eyes'][0]*self.s.get('eye_factor',(1,1))[0];ez=self.hh*.124*self.s['eyes'][1]*self.s.get('eye_factor',(1,1))[1]
        for j in range(rings):
            for i in range(seg):
                if i0<=i<i1 and j0<=j<j1:continue
                inds=(j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i);mid=sum((v[k] for k in inds),Vector())*.25
                eyehole=mid.y<0 and any(((mid.x-side*self.eye_x)/(ex*.99))**2+((mid.z-self.eye_z)/(ez*.93))**2<1 for side in (-1,1))
                if not eyehole:faces.append(inds)
        def jawmove(p,key):
            q=p.copy();f=math.exp(-((p.x/(self.w*.60))**4))*clamp((self.hz-p.z)/(self.hh*.25))*clamp(-p.y/self.d)
            drop={'Mouth_A':.034,'Mouth_E':.013,'Mouth_O':.026,'Fear':.017,'Surprise':.031}.get(key,0)*self.h
            q.z-=f*drop;q.y-=f*drop*.18;return q
        self.add('Sculpted head',v,faces,'Skin',morph={key:[jawmove(p,key) for p in v] for key in ('Mouth_A','Mouth_E','Mouth_O','Fear','Surprise')},uv=uv)
        perimeter=[v[j0*seg+i] for i in range(i0,i1)]+[v[j*seg+i1] for j in range(j0,j1)]+[v[j1*seg+i] for i in range(i1,i0,-1)]+[v[j*seg+i0] for j in range(j1,j0,-1)]
        settings=self.mouth_settings();N=len(perimeter);R=7
        def patch(key):
            result=[]
            for j in range(R):
                q=j/(R-1)
                for outer in perimeter:
                    a=math.atan2((outer.z-self.mouth_z)/(.10*self.hh),outer.x/(self.w*.52))
                    inner=self.mouthpoint(a,1,key)
                    edge=jawmove(outer,key);p=inner.lerp(edge,q);p.y-=math.sin(math.pi*q)*.003*self.h;result.append(p)
            return result
        pf=[((j+1)*N+i,(j+1)*N+(i+1)%N,j*N+(i+1)%N,j*N+i) for j in range(R-1) for i in range(N)]
        self.add('Skin around mouth opening',patch('Basis'),pf,'Skin',morph={k:patch(k) for k in settings if k!='Basis'})
        # The ear rim, concha and antihelix are real geometry.
        for side in (-1,1):
            c=(side*self.w*.955,-self.d*.04,self.hz-self.hh*.08)
            self.ellipsoid('Ear',c,(self.w*.205,self.d*.19,self.hh*.145),'Skin',seg=20)
            self.ellipsoid('Ear concha',(c[0],c[1]-.030*self.h,c[2]),(self.w*.116,.008*self.h,self.hh*.085),'Lip',seg=16,rings=10)
            path=[(c[0]+side*self.w*.088*math.sin(a),c[1]-.037*self.h,c[2]+self.hh*.09*math.cos(a)) for a in [i*math.pi/9 for i in range(15)]]
            self.tube('Ear cartilage',path,.006*self.h,'Skin')
        self.nose();self.eyes();self.mouth();self.hair()
        if self.s.get('freckles'):
            for side in (-1,1):
                for i in range(5):
                    x=side*(self.w*.43+(i%3)*self.w*.105);z=self.eye_z-self.hh*(.19+(i//3)*.032)
                    y=-self.d*math.sqrt(max(.1,1-(x/self.w)**2))-.008*self.h
                    self.ellipsoid('Freckle',(x,y,z),(.0017*self.h,.0013*self.h,.0015*self.h),'HairLight',seg=8,rings=4)
        if self.s['age']=='elder':
            for side in (-1,1):
                for k in range(2):
                    x=side*(self.eye_x+self.w*.27);z=self.eye_z-(.005+k*.012)*self.h
                    self.tube('Smile crease',[(x,-self.d*.74,z),(x+side*.02*self.h,-self.d*.66,z-.005*self.h)],.0017*self.h,'Lip',sides=5)

    def nose(self):
        if self.s.get('sculpted_nose'):
            for side in (-1,1):
                z=self.hz-self.hh*.102;x=side*self.w*.089;t=(z-self.hz)/self.hh+.5;a=math.asin(x/(self.w*math.sqrt(1-(2*t-1)**2)));p=self.head_surface(t,a)
                self.ellipsoid('Small nostril',(p.x,p.y-.0007*self.h,p.z),(.0022*self.h,.001*self.h,.0011*self.h),'Lip',seg=12,rings=6)
            return
        nw=self.w*.175*self.s['nose'][0];nz=self.hh*.137*self.s['nose'][1];z=self.hz-self.hh*.077
        self.ellipsoid('Nose bridge',(0,-self.d*.975,z+nz*.45),(nw*.63,self.d*.16,nz),'Skin',seg=24,rings=16)
        self.ellipsoid('Nose tip',(0,-self.d*1.125,z),(nw,self.d*.245,nz*.56),'Skin',seg=24,rings=16)
        for side in (-1,1):
            self.ellipsoid('Nose wing',(side*nw*.79,-self.d*1.08,z-nz*.18),(nw*.53,self.d*.136,nz*.37),'Skin',seg=16,rings=10)
            self.ellipsoid('Nostril',(side*nw*.62,-self.d*1.217,z-nz*.32),(nw*.23,.003*self.h,nz*.155),'Lip',seg=12,rings=8)

    def eyes(self):
        s=self.s; ex=self.w*.288*s['eyes'][0]*s.get('eye_factor',(1,1))[0];ez=self.hh*.124*s['eyes'][1]*s.get('eye_factor',(1,1))[1];ey=self.d*.22
        for side in (-1,1):
            cx=side*self.eye_x;cy=-self.d*.79;cz=self.eye_z
            self.ellipsoid('Left sclera' if side==1 else 'Right sclera',(cx,cy,cz),(ex,ey,ez),'EyeWhite',seg=28,rings=20)
            ir=ex*.71
            self.ellipsoid('Iris outer ring',(cx,cy-ey*.957,cz),(ir,.006*self.h,ir*1.08),'Pupil',seg=24,rings=12)
            self.ellipsoid('Iris',(cx,cy-ey*.988-.002*self.h,cz),(ir*.88,.005*self.h,ir*.95),'Iris',seg=28,rings=12)
            self.ellipsoid('Pupil',(cx,cy-ey-.006*self.h,cz),(ir*.58,.004*self.h,ir*.68),'Pupil',seg=24,rings=12)
            self.ellipsoid('Eye catchlight',(cx-ir*.26,cy-ey-.009*self.h,cz+ir*.36),(ir*.21,.0018*self.h,ir*.24),'Ivory',seg=12,rings=8)
            self.ellipsoid('Eye small catchlight',(cx+ir*.27,cy-ey-.009*self.h,cz-ir*.32),(ir*.09,.0017*self.h,ir*.10),'Ivory',seg=10,rings=6)
            # Skin annulus closes across the eyeball when Blink is driven.
            v=[];f=[];blink=[];fear=[];surprise=[];N=48;R=5
            for j in range(R):
                q=j/(R-1)
                for i in range(N):
                    a=i*math.tau/N;dx=math.cos(a);dz=math.sin(a)
                    ix=ex*.92*dx;iz=ez*.86*dz
                    ox=ex*1.30*dx;oz=ez*1.28*dz
                    x=cx+mix(ix,ox,q);z=cz+mix(iz,oz,q)
                    inner_y=cy-ey*math.sqrt(max(.08,1-(ix/ex)**2-(iz/ez)**2))-.002*self.h
                    tt=clamp((z-self.hz)/self.hh+.5,.001,.999);rad=math.sqrt(max(.001,1-(2*tt-1)**2));jaw=mix(self.s['jaw'],1,smooth(tt/.58));cheek=1+(self.s['cheek']-1)*math.exp(-((tt-.38)/.20)**2);xr=self.w*rad*jaw*cheek
                    angle=math.asin(clamp(x/xr,-.999,.999));outer_y=self.head_surface(tt,angle).y-.0004*self.h
                    y=mix(inner_y,outer_y,q)-math.sin(q*math.pi)*.004*self.h
                    v.append((x,y,z));blink.append((x,mix(cy-ey-.018*self.h,outer_y,q**4),cz+mix(iz*.008,oz,q)))
                    fear.append((x,y,z+(1-q)*dz*.008*self.h));surprise.append((x,y,z+(1-q)*dz*.012*self.h))
            for j in range(R-1):
                for i in range(N):f.append(((j+1)*N+i,(j+1)*N+(i+1)%N,j*N+(i+1)%N,j*N+i))
            self.add('Eyelid skin',v,f,'Skin',morph={'Blink':blink,'Fear':fear,'Surprise':surprise})
            # The lash rim follows the aperture, including blink.
            def blinkrim(p):
                q=p.copy();q.z=cz+(q.z-cz)*.02;q.y=cy-ey-.020*self.h;return q
            path=[]
            for i in range(25):
                a=i*math.pi/24;xx=ex*.925*math.cos(a);zz=ez*.865*math.sin(a)
                yy=cy-ey*math.sqrt(max(.07,1-(xx/ex)**2-(zz/ez)**2))-.003*self.h
                path.append((cx+xx,yy,cz+zz))
            self.tube('Upper eyelid rim',path,.0026*self.h,'Hair' if s['sex']=='f' else 'Lip',sides=6,morph={'Blink':blinkrim,'Fear':lambda p: p+Vector((0,0,.006*self.h)),'Surprise':lambda p:p+Vector((0,0,.010*self.h))})
            # Independent brows communicate fear through their inner ends.
            path=[]
            for i in range(13):
                t=i/12;x=cx+mix(-ex*.90,ex*.90,t);z=cz+ez*1.43+math.sin(t*math.pi)*self.hh*.020
                path.append((x,-self.d*math.sqrt(max(.2,1-(x/(self.w*1.12))**2))-.008*self.h,z))
            def browfear(p):
                inner=clamp(1-abs(p.x)/(self.w*.78));return p+Vector((0,-.002*self.h,(inner*.028-.008)*self.h))
            self.tube('Eyebrow',path,[self.h*.0045*(.65+.8*math.sin(i*math.pi/12)) for i in range(13)],'Hair',sides=8,morph={'Fear':browfear,'Surprise':lambda p:p+Vector((0,0,.022*self.h)),'Smile':lambda p:p+Vector((0,0,.004*self.h))})

    @staticmethod
    def mouth_settings():
        return {'Basis':(1,.0028,.008),'Mouth_A':(1,.029,-.007),'Mouth_E':(1.17,.014,.001),'Mouth_O':(.57,.025,-.003),'Fear':(.94,.016,-.013),'Surprise':(.64,.029,-.003),'Smile':(1.14,.009,.017)}

    def mouthpoint(self,t,r,key,lip=False):
        ws,opening,smile=self.mouth_settings()[key];x=math.cos(t)*self.mw*ws*r;z=self.mouth_z+math.sin(t)*opening*self.h*r+smile*self.h*(abs(math.cos(t))**2)-opening*self.h*.31
        y=-self.d*.965-.010*self.h+.018*self.h*(x/(self.mw*ws))**2
        if lip:y-=.0025*self.h
        return Vector((x,y,z))

    def mouth(self):
        h=self.h;zc=self.mouth_z;yc=-self.d*.965-.010*h;w=self.mw
        settings=self.mouth_settings();mouthpoint=self.mouthpoint
        # A bowl with depth; its shape changes together with the lips.
        def bowl(key):
            return [mouthpoint(i*math.tau/40,j/5,key)+Vector((0,(1-j/5)*.018*h,0)) for j in range(6) for i in range(40)]
        f=[((j+1)*40+i,(j+1)*40+(i+1)%40,j*40+(i+1)%40,j*40+i) for j in range(5) for i in range(40)]
        self.add('Mouth cavity',bowl('Basis'),f,'Mouth',morph={k:bowl(k) for k in settings if k!='Basis'})
        # Upper and lower vermilion border: neutral lips and phoneme contours.
        for upper in (True,False):
            a0=0 if upper else math.pi
            path=[mouthpoint(a0+math.pi*i/28,1,'Basis',True) for i in range(29)]
            radius=[h*.004*(.4+.65*math.sin(i*math.pi/28)) for i in range(29)]
            # Build identical tube topology for all shape keys using path offsets.
            before=len(self.v);self.tube('Upper lip' if upper else 'Lower lip',path,radius,'Lip',sides=8)
            for key in settings:
                if key=='Basis':continue
                target=[mouthpoint(a0+math.pi*i/28,1,key,True) for i in range(29)]
                for j in range(29):
                    delta=target[j]-path[j]
                    for k in range(8):self.keys[key][before+j*8+k]=tuple(Vector(self.v[before+j*8+k])+delta)
        # Teeth are hidden behind the resting lip; never painted on the face.
        for i in range(6):
            x=(i-2.5)*w*.265;top=zc+.013*h;cy=yc+.013*h
            def teethmove(p,key):
                ws,opening,smile=settings[key];q=p.copy();q.x*=ws;q.y-=.009*h if opening>.009 else 0;q.z-=.008*h if opening>.009 else 0;return q
            self.ellipsoid('Upper tooth',(x,cy,top),(w*.137,.005*h,.0055*h),'Ivory',seg=12,rings=8,morph={k:(lambda p,kk=k:teethmove(p,kk)) for k in settings if k!='Basis'})
        self.ellipsoid('Tongue',(0,yc+.010*h,zc-.001*h),(w*.60,.010*h,.002*h),'Tongue',seg=20,rings=8,morph={k:(lambda p,kk=k: Vector((p.x*settings[kk][0],p.y-.007*h,p.z-settings[kk][1]*h*.9))) for k in settings if k!='Basis'})

    def hair(self):
        style=self.s['hair'];h=self.h;w=self.w;d=self.d;hh=self.hh;hz=self.hz
        # Scalp follows the authored cranium and opens around the forehead.
        seg=48;rings=12;v=[];f=[]
        for j in range(rings+1):
            q=j/rings
            for i in range(seg):
                a=-math.pi+math.tau*i/seg;front=max(0,math.cos(a));boundary=.40+.40*front
                if style in ('bald','receding'):boundary=.54+.42*front
                elif style in ('bob','pigtails','buns'):boundary=.33+(.55 if self.s.get('style_study') else .46)*front
                t=mix(boundary,.998,q);p=self.head_surface(t,a);p.x*=1.035;p.y*=1.035;p.z+=.009*h;v.append(p)
        for j in range(rings):
            for i in range(seg):f.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
        if style!='bald':self.add('Sculpted scalp',v,f,'Hair')
        def lock(name,path,r,light=False):
            self.tube(name,path,[r*(.45+.55*math.sin(math.pi*(i+.6)/(len(path)+.2))) for i in range(len(path))],'HairLight' if light else 'Hair',sides=9)
        if style=='bob':
            # Continuous blunt bob volume; grooves are shallow sculpted ridges.
            vs=[];fs=[];N=48;R=15
            for j in range(R):
                t=j/(R-1)
                for i in range(N):
                    a=mix(.69,math.tau-.69,i/(N-1));rr=.73+.43*math.sin(t*math.pi*.72);zz=hz+hh*.36-t*hh*.86
                    vs.append((math.sin(a)*w*rr,-math.cos(a)*d*(rr+.04),zz+.006*h*math.cos(i*.6)*t))
            for j in range(R-1):
                for i in range(N-1):fs.append((j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i))
            self.add('Continuous bob haircut',vs,fs,'Hair')
            for i in range(15):
                a=mix(.72,math.tau-.72,i/14);path=[]
                for j in range(14):
                    t=j/13;rr=.74+.43*math.sin(t*math.pi*.72);path.append((math.sin(a)*w*rr,-math.cos(a)*d*(rr+.04),hz+hh*.36-t*hh*.85))
                lock('Bob sculpted groove',path,w*.018,i%3==0)
        if style in ('wavy','long_braid','pigtails'):
            count=14
            for i in range(count):
                a=.68+((math.tau-1.36)*i/(count-1));a=a if a<math.pi else a-math.tau
                path=[]
                for j in range(12):
                    t=j/11;zz=hz+hh*.39-t*hh*(.93 if style=='bob' else 1.04)
                    rr=math.sin(min(1,t*1.5)*math.pi*.5)
                    xx=math.sin(a)*w*(.68+.46*rr);yy=-math.cos(a)*d*(.72+.47*rr)
                    if style!='bob':xx+=math.sin(t*math.tau*1.6+i)*w*.09;yy+=math.cos(t*math.tau*1.5+i)*d*.09
                    path.append((xx,yy,zz))
                lock('Bob hair panel' if style=='bob' else 'Flowing hair lock',path,w*.15,i%4==0)
        if style in ('curls','coils','curly_pony'):
            rng=random.Random(self.s['name'])
            for i in range(36 if style!='curly_pony' else 22):
                a=rng.uniform(-math.pi,math.pi);t=rng.uniform(.69,.96);p=self.head_surface(t,a);p*=1.026;p.z-=hz*.026;p.z+=.019*h
                r=w*rng.uniform(.13,.20);self.ellipsoid('Sculpted curl',p,(r,r*.90,r*1.1),'Hair' if i%5 else 'HairLight',seg=12,rings=8)
            if style=='curly_pony':
                for i in range(15):
                    t=i/14;p=(w*.63+math.sin(t*math.tau*2)*w*.12,d*.8+t*d*.55,hz+hh*.53-t*hh*.88)
                    self.ellipsoid('Ponytail curl',p,(w*.32,w*.25,hh*.17),'Hair' if i%3 else 'HairLight',seg=16,rings=10)
        if style in ('braids','long_braid','pigtails'):
            sides=(-1,1) if style!='long_braid' else (1,)
            for side in sides:
                for i in range(16):
                    t=i/15;z=hz+hh*.40-t*hh*1.30;x=side*w*(.55+.55*math.sin(min(1,t*1.7)*math.pi*.5));y=d*.1+math.sin(t*math.pi)*d*.55
                    for weave in (-1,1):self.ellipsoid('Braided hair',(x+weave*w*.075*math.cos(i*2),y+weave*w*.06*math.sin(i*2),z),(w*.13,w*.13,hh*.064),'HairLight' if i%4==0 else 'Hair',seg=12,rings=8)
                self.ellipsoid('Hair tie',(x,y,z),(w*.13,w*.13,hh*.035),'Detail',seg=12,rings=8)
        if style in ('buns','low_bun','side_pony'):
            sites=[(-w*.84,d*.02,hz+hh*.30),(w*.84,d*.02,hz+hh*.30)] if style=='buns' else [(w*.70 if style=='side_pony' else 0,d*.97,hz-hh*.05)]
            for site in sites:
                self.ellipsoid('Hair bun',site,(w*.40,d*.46,hh*.23),'Hair',seg=24,rings=16)
                for i in range(6):
                    a=i*math.pi/6;path=[(site[0]+w*.37*math.cos(t),site[1]+d*.42*math.sin(t),site[2]+hh*.13*math.sin(t+a)) for t in [j*math.tau/25 for j in range(26)]]
                    lock('Bun twist',path,w*.022,i%3==0)
        if style=='bob':
            vs=[];fs=[];N=40;R=12
            for j in range(R):
                t=j/(R-1)
                for i in range(N):
                    a=mix(-1.12,1.12,i/(N-1));boundary=(.77 if self.s.get('style_study') else .799)+.018*math.sin(a*5);tt=mix(.978,boundary,t);p=self.head_surface(tt,a);p.x*=1.03;p.y*=1.05;p.z+=.006*h;vs.append(p)
            for j in range(R-1):
                for i in range(N-1):fs.append((j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i))
            self.add('Continuous blunt fringe',vs,fs,'Hair')
        if style not in ('bob','bald','receding','braids','coils','curls','buns','low_bun','curly_pony','long_braid'):
            for i in range(9):
                x=mix(-w*.82,w*.79,i/8);path=[]
                for j in range(11):
                    t=j/10;xx=x+(-.26*w*math.sin(t*math.pi) if style in ('sidepart','quiff','pixie','waves') else .06*w*math.sin(t*math.pi))
                    zz=hz+hh*(.47-.26*t)+hh*(.13 if style in ('quiff','spikes') else .035)*math.sin(t*math.pi)
                    if style=='bob':zz=hz+hh*(.44-.19*t)+.007*h*math.cos(i)
                    yy=-d*(.25+.75*t);path.append((xx,yy,zz))
                lock('Individual fringe lock',path,w*(.125 if style!='pixie' else .10),i%4==0)
        if style in ('bald','receding'):
            for side in (-1,1):
                for i in range(7):
                    path=[(side*w*(.96-.14*t),d*(.1+.45*t),hz+hh*(.03+.30*t)-i*.002*h) for t in [j/9 for j in range(10)]]
                    lock('Temple hair',path,w*.034,i%3==0)

    def body(self):
        h=self.h;s=self.s;child=self.child;build=s['build'];outfit=s['outfit']
        hip=h*(.365 if child else .405);shoulder=h*(.60 if child else .663);neck=h*(.665 if child else .714)
        sw=h*.13*build;wa=h*.106*build;depth=h*.080*(.90+build*.12);hipw=h*.103*build
        self.joints={'Hips':((0,0,hip),(0,0,hip+.075*h),None),'Spine':((0,0,hip+.075*h),(0,0,shoulder-.10*h),'Hips'),'Chest':((0,0,shoulder-.10*h),(0,0,shoulder),'Spine'),'Neck':((0,0,shoulder),(0,0,neck),'Chest'),'Head':((0,0,neck),(0,0,self.hz+hh*.2 if (hh:=self.hh) else self.hz),'Neck')}
        def torso_weights(p):
            z=p[2]
            if z<hip+.07*h:return {'Hips':1}
            if z<shoulder-.10*h:
                q=clamp((z-hip-.07*h)/(.1*h));return {'Hips':1-q,'Spine':q}
            q=clamp((z-(shoulder-.10*h))/(.07*h));return {'Spine':1-q,'Chest':q}
        self.loft('Neck',[(0,0,shoulder-.018*h,h*.046,h*.041),(0,0,neck+.038*h,h*.050,h*.045)],'Skin','Neck')
        shirtmat='Under' if outfit in ('jacket','vest','cardigan','dungarees') else 'Top'
        self.loft('Shirt tailored body',[(0,0,hip-.008*h,hipw,depth*.90),(0,0,hip+.025*h,hipw*1.05,depth),(0,0,hip+.115*h,wa,depth*1.07),(0,0,shoulder-.055*h,sw*.96,depth*.99),(0,0,shoulder-.009*h,sw*.84,depth*.86),(0,0,shoulder+.014*h,h*.048,h*.047)],shirtmat,torso_weights)
        # Ribbed collar and lower hem follow body topology.
        for z,rx,ry,mat in [(shoulder+.008*h,h*.052,h*.049,shirtmat),(hip+.007*h,hipw*1.035,depth*.93,shirtmat)]:
            path=[(math.sin(i*math.tau/40)*rx,-math.cos(i*math.tau/40)*ry,z) for i in range(41)];self.tube('Garment collar or hem',path,.004*h,mat,torso_weights)
        if outfit in ('jacket','vest','cardigan'):
            # Two separate front panels, curved around the torso with an opening.
            for side in (-1,1):
                verts=[];faces=[];nr=12;nc=14
                for j in range(nr):
                    t=j/(nr-1);z=mix(hip-.023*h,shoulder+.003*h,t);rx=mix(hipw*1.13,sw*1.08,t);ry=depth*(1.15+.035*math.sin(t*math.pi))
                    for i in range(nc):
                        a=side*mix(.18,math.pi+.08,i/(nc-1));verts.append((math.sin(a)*rx,-math.cos(a)*ry,z))
                for j in range(nr-1):
                    for i in range(nc-1):faces.append((j*nc+i,j*nc+i+1,(j+1)*nc+i+1,(j+1)*nc+i))
                self.add('Open garment panel',verts,faces,'Top',torso_weights)
                edge=[(side*mix(hipw,sw*.84,t)*.19,-depth*1.065,mix(hip-.023*h,shoulder,t)) for t in [i/12 for i in range(13)]]
                self.tube('Jacket opening piping',edge,.003*h,'Seam',torso_weights)
                px=side*hipw*.63;pz=hip+.055*h
                self.ellipsoid('Sewn patch pocket',(px,-depth*1.067,pz),(hipw*.27,.004*h,.031*h),'Top',torso_weights,seg=16,rings=8)
                self.tube('Pocket stitching',[(px-hipw*.23,-depth*1.09,pz+.023*h),(px+hipw*.23,-depth*1.09,pz+.023*h)],.0017*h,'Seam',torso_weights)
                if outfit=='vest':
                    for zz in (hip+.044*h,hip+.12*h):
                        t=clamp((zz-(hip-.023*h))/(shoulder-hip+.026*h));rx=mix(hipw*1.13,sw*1.08,t);ry=depth*(1.15+.035*math.sin(t*math.pi));path=[(side*math.sin(a)*(rx+.003*h),-math.cos(a)*(ry+.003*h),zz) for a in [mix(.19,math.pi-.2,i/20) for i in range(21)]];self.tube('Reflective vest tape',path,.010*h,'Cream',torso_weights,sides=6)
        if outfit in ('shirt','polo','jacket'):
            for i in range(4):self.ellipsoid('Shirt button',(0,-depth*1.065,shoulder-.045*h-i*.040*h),(.003*h,.002*h,.003*h),'Detail',torso_weights,seg=10,rings=6)
        if outfit=='hoodie':
            self.ellipsoid('Folded fabric hood',(0,depth*.67,shoulder-.015*h),(sw*.63,.041*h,.047*h),'Top','Chest',seg=24)
            for side in (-1,1):self.tube('Hood drawcord',[(side*.025*h,-depth*.92,shoulder-.012*h),(side*.027*h,-depth*1.075,shoulder-.085*h)],.0016*h,'Cream','Chest',sides=6)
            self.ellipsoid('Kangaroo pocket',(0,-depth*1.015,hip+.075*h),(wa*.61,.007*h,.040*h),'Top',torso_weights,seg=20)
        if outfit=='dungarees':
            self.ellipsoid('Overall bib',(0,-depth*1.024,hip+.115*h),(wa*.67,.008*h,.071*h),'Bottom',torso_weights,seg=24)
            for side in (-1,1):
                path=[(side*.045*h,-depth*1.05,hip+.14*h),(side*.045*h,-depth*.84,shoulder+.005*h),(side*.045*h,depth*.82,shoulder+.005*h),(side*.045*h,depth*.98,hip+.14*h)]
                self.tube('Overall shoulder strap',path,.009*h,'Bottom',torso_weights,sides=8)
                self.ellipsoid('Overall buckle',(side*.045*h,-depth*1.086,hip+.168*h),(.007*h,.003*h,.009*h),'Detail','Chest',seg=12,rings=8)
        for side,label in ((1,'Left'),(-1,'Right')):
            sh=Vector((side*sw*.89,0,shoulder-.013*h));el=Vector((side*(sw+.082*h),0,shoulder-.15*h));wr=Vector((side*(sw+.13*h),-.008*h,shoulder-.265*h));end=wr+Vector((side*.015*h,-.003*h,-.067*h))
            self.joints.update({label+'Shoulder':((side*.032*h,0,shoulder-.014*h),tuple(sh),'Chest'),label+'UpperArm':(tuple(sh),tuple(el),label+'Shoulder'),label+'LowerArm':(tuple(el),tuple(wr),label+'UpperArm'),label+'Hand':(tuple(wr),tuple(end),label+'LowerArm')})
            def armweights(p,L=label,E=el.z):
                q=smooth((E+.035*h-p[2])/(.07*h));return {L+'UpperArm':1-q,L+'LowerArm':q}
            points=[sh.lerp(el,t) for t in (0,.18,.55,.85,1)]+[el.lerp(wr,t) for t in (.22,.55,.82,1)]
            self.tube('Arm skin',points,[h*r*build**.3 for r in (.037,.040,.034,.031,.030,.028,.025,.020,.019)],'Skin',armweights,sides=18)
            long=outfit in ('jacket','cardigan','hoodie','tunic')
            cuff=el.lerp(wr,.75) if long else sh.lerp(el,.59)
            path=[sh.lerp(cuff,t) for t in (0,.12,.35,.6,.87,1)]
            self.tube('Garment sleeve',path,[h*r*build**.4 for r in (.048,.051,.048,.046,.040,.039)],'Top' if long else shirtmat,armweights,sides=20)
            self.ellipsoid('Soft shoulder seam',sh.lerp(cuff,.10),(h*.047*build**.4,h*.046,h*.046),'Top' if long else shirtmat,label+'UpperArm',seg=20,rings=12)
            self.hand(wr,end,label,side)
            hx=side*h*.052*build;knee=h*.223;ankle=h*.055;footy=-h*.021
            self.joints.update({label+'UpperLeg':((hx,0,hip),(hx,-.004*h,knee),'Hips'),label+'LowerLeg':((hx,-.004*h,knee),(hx,0,ankle),label+'UpperLeg'),label+'Foot':((hx,0,ankle),(hx,-.086*h,.033*h),label+'LowerLeg'),label+'Toes':((hx,-.086*h,.033*h),(hx,-.118*h,.031*h),label+'Foot')})
            def legweights(p,L=label):
                q=smooth((knee+.035*h-p[2])/(.07*h));return {L+'UpperLeg':1-q,L+'LowerLeg':q}
            self.loft('Leg skin',[(hx,0,ankle,h*.026,h*.027),(hx,0,knee,h*.034,h*.035),(hx,0,hip,h*.047*build,h*.046)],'Skin',legweights,sides=20)
            short=outfit in ('shorts','dungarees')
            bottom=hip-.15*h if short else ankle+.014*h
            self.loft('Trouser leg',[(hx,0,bottom,h*.048*build,h*.043),(hx,0,bottom+.016*h,h*.051*build,h*.045),(hx,0,mix(bottom,hip,.54),h*.051*build,h*.049),(hx,0,hip+.015*h,h*.056*build,h*.049)],'Bottom',legweights,sides=24)
            self.ellipsoid('Shoe sole',(hx,footy-.026*h,.017*h),(.044*h,.094*h,.016*h),'Cream',label+'Foot',seg=24,rings=10)
            self.ellipsoid('Shoe upper',(hx,footy-.026*h,.042*h),(.043*h,.088*h,.032*h),'Shoe',label+'Foot',seg=24,rings=12)
            for i in range(3):self.tube('Shoe lace',[(hx-.025*h,-.050*h-i*.012*h,.065*h-i*.002*h),(hx+.025*h,-.050*h-i*.012*h,.065*h-i*.002*h)],.0017*h,'Cream',label+'Foot',sides=6)
            if short:
                self.loft('Sock',[(hx,0,ankle+.015*h,h*.028,h*.028),(hx,0,ankle+.061*h,h*.028,h*.028)],'Cream',label+'LowerLeg',sides=18)
                path=[(hx+math.sin(i*math.tau/24)*h*.0282,-math.cos(i*math.tau/24)*h*.0282,ankle+.051*h) for i in range(25)];self.tube('Sock stripe',path,.003*h,'Top',label+'LowerLeg',sides=6)
        if outfit in ('dress','tunic'):
            self.loft('Flared dress' if outfit=='dress' else 'Tunic hem',[(0,0,hip-(.14*h if outfit=='dress' else .04*h),hipw*(1.36 if outfit=='dress' else 1.11),depth*1.24),(0,0,hip+.01*h,hipw*1.03,depth),(0,0,hip+.09*h,wa,depth*.99)],'Top',torso_weights,sides=40)

    def hand(self,wrist,end,label,side):
        h=self.h;center=wrist.lerp(end,.48)
        self.ellipsoid('Palm',center,(.027*h,.016*h,.038*h),'Skin',label+'Hand',seg=18,rings=12)
        for i in range(4):
            x=center.x+side*(i-1.5)*.011*h;z=center.z-.026*h;length=(.038,.047,.045,.034)[i]*h
            path=[(x,-.003*h,z),(x+side*.003*h,-.008*h,z-length*.55),(x+side*.005*h,-.015*h,z-length)]
            self.tube('Separate finger',path,[.0067*h,.0063*h,.0048*h],'Skin',label+'Hand',sides=9)
            self.ellipsoid('Fingertip',path[-1],(.0049*h,.0049*h,.0055*h),'Skin',label+'Hand',seg=10,rings=6)
        self.tube('Thumb',[tuple(center+Vector((-side*.018*h,-.004*h,.014*h))),tuple(center+Vector((-side*.035*h,-.012*h,.0))),tuple(center+Vector((-side*.040*h,-.016*h,-.016*h)))],[.010*h,.008*h,.006*h],'Skin',label+'Hand',sides=10)

    def finish(self):
        # Weld coincident skin borders, including the mouth patch, so their
        # normals and deformation remain continuous instead of forming a seam.
        lookup={};remap=[];v=[];weights=[];uv=[]
        for i,p in enumerate(self.v):
            code=tuple(round(x,7) for x in p)
            if code not in lookup:lookup[code]=len(v);v.append(p);weights.append(self.weights[i]);uv.append(self.uv[i])
            remap.append(lookup[code])
        self.f=[tuple(remap[i] for i in face) for face in self.f]
        self.keys={k:{remap[i]:p for i,p in d.items()} for k,d in self.keys.items()}
        self.v=v;self.weights=weights;self.uv=uv
        name=self.s['name'];mesh=bpy.data.meshes.new(name+'_OriginalSurface');mesh.from_pydata(self.v,[],self.f);mesh.update()
        obj=bpy.data.objects.new('CharacterSurface',mesh);bpy.context.collection.objects.link(obj)
        for mat in self.materials:mesh.materials.append(mat)
        for poly,mi in zip(mesh.polygons,self.mi):poly.material_index=mi;poly.use_smooth=True
        uv=mesh.uv_layers.new(name='ArtUV')
        for poly in mesh.polygons:
            for li in poly.loop_indices:uv.data[li].uv=self.uv[mesh.loops[li].vertex_index]
        obj.shape_key_add(name='Basis',from_mix=False).value=0
        for n,changes in self.keys.items():
            key=obj.shape_key_add(name=n,from_mix=False);key.value=0
            for i,p in changes.items():key.data[i].co=p
        arm=bpy.data.armatures.new(name+'_Skeleton');rig=bpy.data.objects.new(name+'_Rig',arm);bpy.context.collection.objects.link(rig)
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
        for n,(a,b,parent) in self.joints.items():
            bone=arm.edit_bones.new(n);bone.head=a;bone.tail=b
            if parent:bone.parent=arm.edit_bones[parent]
        bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
        groups={n:obj.vertex_groups.new(name=n) for n in self.joints}
        for i,weights in enumerate(self.weights):
            total=sum(weights.values())
            for n,w in weights.items():
                if w>1e-5:groups[n].add([i],w/total,'REPLACE')
        mod=obj.modifiers.new('Humanoid skin','ARMATURE');mod.object=rig;obj.parent=rig
        obj['authorship']='New mesh authored in Blender; no existing character meshes imported.'
        rig['character']=name;rig['expression_keys']=', '.join(SHAPES)
        return obj,rig

def stage(obj,rig):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
    if not scene.world:scene.world=bpy.data.worlds.new('Resident review world')
    scene.world.color=(.7,.7,.7)
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.72,.78,.85,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.50
    floor=bpy.data.materials.new('Studio ivory');floor.diffuse_color=(.82,.81,.76,1)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005));bpy.context.object.name='Review studio floor';bpy.context.object.data.materials.append(floor)
    for name,pos,power,size in [('Key',(-3,-4,5),480,4),('Fill',(3,-2,3),280,3),('Rim',(1,3,4),420,3)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
    cam=bpy.data.cameras.new('Review Camera');camera=bpy.data.objects.new('Review Camera',cam);scene.collection.objects.link(camera);scene.camera=camera;cam.type='ORTHO';scene.view_settings.view_transform='AgX'
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    return camera

def render(obj,rig,spec,out):
    camera=stage(obj,rig);scene=bpy.context.scene;h=spec['h'];camera.data.ortho_scale=h*1.24;camera.location=(h*.11,-h*3.6,h*.73);target=Vector((0,0,h*.50));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.resolution_percentage=100
    scene.render.filepath=str(out/'FullBody.png');bpy.ops.render.render(write_still=True)
    # Expression proof is rendered from the actual mesh and its shape keys.
    camera.data.ortho_scale=h*.49;camera.location=(0,-h*3.6,h*.85);target=Vector((0,-h*.045,h*.865));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.resolution_x=640;scene.render.resolution_y=640
    for title,keys in [('Neutral',{}),('Speaking',{'Mouth_A':.9}),('Fear',{'Fear':1}),('Surprise',{'Surprise':1}),('Blink',{'Blink':1}),('Smile',{'Smile':1})]:
        for key in obj.data.shape_keys.key_blocks:key.value=keys.get(key.name,0)
        scene.render.filepath=str(out/(title+'.png'));bpy.ops.render.render(write_still=True)
    for key in obj.data.shape_keys.key_blocks:key.value=0

def main():
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    parser=argparse.ArgumentParser();parser.add_argument('--names',nargs='*');parser.add_argument('--render',action='store_true');args=parser.parse_args(argv)
    bpy.context.preferences.filepaths.save_version=0;OUT.mkdir(parents=True,exist_ok=True);UNITY.mkdir(parents=True,exist_ok=True)
    for spec in CAST:
        if args.names and spec['name'] not in args.names:continue
        bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.unit_settings.system='METRIC'
        sculpt=Sculpture(spec);sculpt.body();sculpt.face();obj,rig=sculpt.finish();name=spec['name'];out=OUT/name;out.mkdir(parents=True,exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')))
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.export_scene.fbx(filepath=str(UNITY/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=False,path_mode='STRIP')
        report=dict(**spec,vertices=len(obj.data.vertices),triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),shapes=list(SHAPES),bones=len(rig.data.bones),geometry_sha256=hashlib.sha256(str(sculpt.v).encode()).hexdigest(),parts=sculpt.parts,materials=sculpt.matmeta,source='Original Blender geometry; no base character imported')
        (out/'manifest.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8');(UNITY/(name+'.materials.json')).write_text(json.dumps(sculpt.matmeta,indent=2),encoding='utf-8')
        print('ORIGINAL_RESIDENT_READY',name,len(obj.data.vertices),report['triangles'],flush=True)
        if args.render:render(obj,rig,spec,out)
    (OUT/'cast.json').write_text(json.dumps(CAST,indent=2,ensure_ascii=False),encoding='utf-8')

if __name__=='__main__':main()
