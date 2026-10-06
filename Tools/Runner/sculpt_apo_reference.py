"""Apo V3: dimensional facial, hair, tailoring and accessory sculpture.
Original geometry. The supplied character painting is projected onto the sculpt
and baked to an ordinary UV atlas, matching the game's textured character style.
No Meshy or downloaded geometry. Draft output stays outside Assets.
"""
import bpy,bmesh,math,pathlib,json,sys
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'ArtDirection/CharacterModels/ApoDetailedSculpt'
sys.path.insert(0,str(pathlib.Path(__file__).parent))
OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
S=1.85/1124;CX=620;FLOOR=1180
def wp(x,y):return ((x-CX)*S,(FLOOR-y)*S)
def sat(x):return max(0,min(1,x))
def smooth(x):x=sat(x);return x*x*(3-2*x)
def srgb(c):
    v=[int(c[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(x/12.92 if x<.04045 else ((x+.055)/1.055)**2.4 for x in v)+(1,)
def gauss(x,c,r):return math.exp(-((x-c)/r)**2)
V=[];F=[];MI=[];WEIGHT=[];PROJ=[];parts=[];materials=[]
image=bpy.data.images.load(str(ROOT/'ArtDirection/CharacterReferences/AbdullahEkinci_Detail_v2.png'),check_existing=True)
face_image=bpy.data.images.load(str(ROOT/'ArtDirection/CharacterReferences/AbdullahEkinci_FaceDetail_v3.png'),check_existing=True)
face_registration=json.loads((OUT/'face-registration.json').read_text())
if face_registration['inliers']<25 or face_registration['median_inlier_error_px']>10:raise RuntimeError('Face detail UV registration is unreliable.')
face_affine=face_registration['original_to_detail']
W,H=1254,1254
def material(name,color,rough=.72):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=srgb(color)
    nt=m.node_tree;bs=nt.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=rough;bs.inputs['Specular IOR Level'].default_value=.22
    if any(word in name.lower() for word in ('watch case','index and buckle')):bs.inputs['Metallic'].default_value=.8
    if rough>.74:
        noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=650;noise.inputs['Detail'].default_value=2
        bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.00032
        nt.links.new(noise.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Linear'
    uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='ReferenceProjection';nt.links.new(uv.outputs['UV'],tex.inputs['Vector'])
    face_tex=nt.nodes.new('ShaderNodeTexImage');face_tex.image=face_image;face_tex.interpolation='Linear'
    face_uv=nt.nodes.new('ShaderNodeUVMap');face_uv.uv_map='FaceDetailProjection';nt.links.new(face_uv.outputs['UV'],face_tex.inputs['Vector'])
    face_weight=nt.nodes.new('ShaderNodeVertexColor');face_weight.layer_name='FaceDetailWeight'
    source=nt.nodes.new('ShaderNodeMixRGB');nt.links.new(face_weight.outputs['Color'],source.inputs[0]);nt.links.new(tex.outputs['Color'],source.inputs[1]);nt.links.new(face_tex.outputs['Color'],source.inputs[2])
    attr=nt.nodes.new('ShaderNodeVertexColor');attr.layer_name='ProjectionWeight'
    mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[1].default_value=srgb(color)
    base=nt.nodes.new('ShaderNodeVertexColor');base.layer_name='SculptBaseTone';nt.links.new(base.outputs['Color'],mix.inputs[1])
    # Reject white studio background in the projection shader, not the source image.
    sep=nt.nodes.new('ShaderNodeSeparateColor');sep.mode='RGB';nt.links.new(source.outputs[0],sep.inputs['Color'])
    mn=nt.nodes.new('ShaderNodeMath');mn.operation='MINIMUM';nt.links.new(sep.outputs[0],mn.inputs[0]);nt.links.new(sep.outputs[1],mn.inputs[1])
    mn2=nt.nodes.new('ShaderNodeMath');mn2.operation='MINIMUM';nt.links.new(mn.outputs[0],mn2.inputs[0]);nt.links.new(sep.outputs[2],mn2.inputs[1])
    ramp=nt.nodes.new('ShaderNodeMapRange');ramp.inputs['From Min'].default_value=.60;ramp.inputs['From Max'].default_value=.88;ramp.inputs['To Min'].default_value=1;ramp.inputs['To Max'].default_value=0
    nt.links.new(mn2.outputs[0],ramp.inputs['Value'])
    factor=nt.nodes.new('ShaderNodeMath');factor.operation='MULTIPLY';nt.links.new(attr.outputs['Color'],factor.inputs[0]);nt.links.new(ramp.outputs[0],factor.inputs[1])
    nt.links.new(factor.outputs[0],mix.inputs[0]);nt.links.new(source.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
    materials.append(m);return len(materials)-1
SKIN=material('Warm skin','#D99A6D',.62);HAIR=material('Sculpted black hair','#241F1B',.72)
BEARD=material('Short beard','#392C23',.8);SHIRT=material('Cotton shirt','#99B8DE',.82)
PANTS=material('Navy trousers','#232D40',.83);SHOES=material('Black shoes','#1C1D21',.53)
EYES=material('Eyes','#F1DECC',.17);COLLAR=material('Tailored collar','#9CBDE1',.75)
def add(name,vs,fs,mat,bone,projection=None):
    st=len(V);V.extend(tuple(p) for p in vs);F.extend(tuple(st+i for i in f) for f in fs);MI.extend([mat]*len(fs))
    WEIGHT.extend(bone(p) if callable(bone) else {bone:1} for p in vs)
    PROJ.extend(projection or [1]*len(vs));parts.append((name,st,len(vs),mat))
def profile_value(ys,values,y):
    def slope(k):
        if k==0:return (values[1]-values[0])/(ys[1]-ys[0])
        if k==len(ys)-1:return (values[-1]-values[-2])/(ys[-1]-ys[-2])
        h0=ys[k]-ys[k-1];h1=ys[k+1]-ys[k];d0=(values[k]-values[k-1])/h0;d1=(values[k+1]-values[k])/h1
        if d0*d1<=0:return 0
        return (3*(h0+h1))/((2*h1+h0)/d0+(h1+2*h0)/d1)
    for i in range(len(ys)-1):
        if ys[i]<=y<=ys[i+1]:
            h=ys[i+1]-ys[i];t=(y-ys[i])/h
            return (2*t**3-3*t*t+1)*values[i]+(t**3-2*t*t+t)*h*slope(i)+(-2*t**3+3*t*t)*values[i+1]+(t**3-t*t)*h*slope(i+1)
    return values[0] if y<ys[0] else values[-1]
def loft(name,rows,mat,bone,seg=64,steps=5,detail=None):
    # Rows: picture y, picture center x, pixel half-width, metric front/back depth.
    vs=[];fs=[];pro=[]
    ys=[r[0] for r in rows]
    for j in range((len(rows)-1)*steps+1):
        y=ys[0]+(ys[-1]-ys[0])*j/((len(rows)-1)*steps)
        cx=profile_value(ys,[r[1] for r in rows],y);rx=profile_value(ys,[r[2] for r in rows],y)*S
        depth=profile_value(ys,[r[3] for r in rows],y)
        x,z=wp(cx,y)
        for i in range(seg):
            a=-math.pi+math.tau*i/seg;px=x+rx*math.sin(a);py=-depth*math.cos(a)
            if detail:py+=detail(px,py,z,a)
            vs.append((px,py,z));pro.append(smooth((math.cos(a)-.02)/.45))
    nr=len(vs)//seg
    for j in range(nr-1):
        for i in range(seg):fs.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    fs.extend([tuple(reversed(range(seg))),tuple((nr-1)*seg+i for i in range(seg))])
    add(name,vs,fs,mat,bone,pro)
def ell(name,c,r,mat,bone,seg=36,nr=24,proj=True):
    vs=[];fs=[];pro=[]
    for j in range(nr+1):
        ph=math.pi*j/nr
        for i in range(seg):
            a=math.tau*i/seg;vs.append((c[0]+r[0]*math.sin(ph)*math.cos(a),c[1]+r[1]*math.sin(ph)*math.sin(a),c[2]+r[2]*math.cos(ph)))
            pro.append(smooth((-math.sin(a)-.04)/.4) if proj else 0)
    for j in range(nr):
        for i in range(seg):fs.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    add(name,vs,fs,mat,bone,pro)
def body_weight(p):
    z=p[2];h=1-smooth((z-.83)/.17);return {'Hips':h,'Chest':1-h}

def torso_folds(x,y,z,a):
    # Broad diagonal compression folds at the tucked waist and underarm.
    h=0
    for side in (-1,1):
        for dz,amp in ((.947,.006),(1.002,.0045),(1.118,.0025)):
            line=dz+.19*side*x
            h+=amp*gauss(z,line,.009)*gauss(x,side*.169,.08)
        h-=.003*gauss(z,1.18-.2*side*x,.020)*gauss(x,side*.235,.033)
    h+=.0022*math.cos(x*61)*gauss(z,.918,.04)
    return -math.cos(a)*h

def sleeve_folds(x,y,z,a):
    h=0
    for j in range(4):
        line=1.027+j*.026+.12*(abs(x)-.365)*(-1 if j%2 else 1)
        h+=(.0048 if j<3 else .003)*gauss(z,line,.007)
    h+=.0045*gauss(z,.895+.1*(abs(x)-.408),.010)
    return -math.cos(a)*h

def trouser_folds(x,y,z,a):
    h=.0035*gauss(z,.180+.22*abs(x),.009)+.0027*gauss(z,.211-.16*abs(x),.011)
    h+=.0025*gauss(z,.430+.08*abs(x),.014)
    return -math.cos(a)*h

def shoe(tag,centerx):
    vs=[];fs=[];N=64
    # A flat stitched sole and a shaped toe/vamp, with a high ankle at the back.
    for j in range(10):
        t=j/9
        for i in range(N):
            a=math.tau*i/N
            ca=math.copysign(abs(math.cos(a))**.73,math.cos(a))
            sa=math.copysign(abs(math.sin(a))**.82,math.sin(a))
            taper=1-.25*math.sin(a)
            rx=.079*taper*(1-.30*t*t);ry=.142*(1-.61*t*t)
            x=centerx+rx*ca;y=-.035+.056*t*t+ry*sa
            z=.027+.052*t+.041*t*t*max(0,math.sin(a))
            if j==9:z=.117
            vs.append((x,y,z))
    for j in range(9):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    fs.extend([tuple(reversed(range(N))),tuple(9*N+i for i in range(N))])
    add('Shaped leather shoe '+tag,vs,fs,SHOES,'Foot.'+tag,[0]*len(vs))
    vs=[];fs=[]
    for z in (.009,.014,.025,.029):
        for i in range(N):
            a=math.tau*i/N
            ca=math.copysign(abs(math.cos(a))**.73,math.cos(a));sa=math.copysign(abs(math.sin(a))**.82,math.sin(a))
            vs.append((centerx+.080*(1-.25*math.sin(a))*ca,-.035+.143*sa,z))
    for j in range(3):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    fs.extend([tuple(reversed(range(N))),tuple(3*N+i for i in range(N))])
    add('Flat welted sole '+tag,vs,fs,SHOES,'Foot.'+tag,[0]*len(vs))
# Match the reference's full silhouette, including its shorter torso/leg ratio.
loft('Torso',[(301,620,61,.079),(315,620,107,.109),(343,620,151,.136),(392,620,160,.15),(490,620,154,.16),(579,620,147,.155),(630,620,143,.145),(646,620,139,.138)],SHIRT,body_weight,steps=10,detail=torso_folds)
loft('Trouser pelvis',[(632,620,139,.137),(662,620,145,.141),(712,620,146,.131),(749,620,129,.117)],PANTS,'Hips')
for side in (-1,1):
    tag='L' if side>0 else 'R'
    def legweight(p,tag=tag):
        t=smooth((p[2]-.33)/.21);return {'UpperLeg.'+tag:t,'LowerLeg.'+tag:1-t}
    cx=620+side*74
    loft('Leg '+tag,[(689,620+side*72,70,.128),(772,620+side*78,64,.11),(898,620+side*81,53,.094),(999,620+side*86,49,.089),(1098,620+side*88,45,.082),(1113,620+side*88,44,.080)],PANTS,legweight,seg=64,steps=12,detail=trouser_folds)
    shoe(tag,wp(620+side*103,1144)[0])
    def armweight(p,tag=tag):
        t=smooth((p[2]-1.0)/.16);return {'UpperArm.'+tag:t,'Forearm.'+tag:1-t}
    loft('Sleeve '+tag,[(315,620+side*90,12,.06),(336,620+side*154,32,.084),(370,620+side*183,42,.086),(403,620+side*196,43,.086),(484,620+side*220,41,.082),(545,620+side*234,38,.076),(608,620+side*245,37,.068),(652,620+side*248,31,.061),(672,620+side*248,30,.057)],SHIRT,armweight,seg=64,steps=12,detail=sleeve_folds)
    x,z=wp(620+side*247,709);ell('Palm '+tag,(x,-.006,z),(.049,.037,.073),SKIN,'Hand.'+tag)
    # Four curved fingers and a distinct thumb, in the original relaxed pose.
    for k in range(4):
        xx=620+side*(225+k*13);y=753+(9 if k in (1,2) else 0);x,z=wp(xx,y)
        ell('Finger '+tag,(x,-.004,z),(.010,.014,.044),SKIN,'Hand.'+tag,seg=16,nr=14)
    x,z=wp(620+side*216,721);ell('Thumb '+tag,(x,-.022,z),(.017,.025,.036),SKIN,'Hand.'+tag,seg=20,nr=14)
# Face is a continuous sculpt: cheekpads, muzzle, nose and brow ridges blend
# into the head, avoiding floating balls, tubes, or a helmet-like beard shell.
def face_detail(x,y,z,a):
    front=max(0,math.cos(a))**5
    _,nosez=wp(620,204);_,bridgez=wp(620,172);_,mouthz=wp(620,237)
    value=-.036*gauss(x,0,.029)*gauss(z,nosez,.023)
    value-=.014*gauss(x,0,.014)*gauss(z,bridgez,.052)
    value-=.010*gauss(x,0,.077)*gauss(z,mouthz,.039)
    smilez=wp(620,235)[1]+.010*(abs(x)/.062)**2
    value-=.0040*gauss(x,0,.052)*gauss(z,smilez+.004,.004)
    value-=.0060*gauss(x,0,.050)*gauss(z,smilez-.006,.005)
    value+=.0030*gauss(x,0,.055)*gauss(z,smilez,.0028)
    value+=.0020*gauss(x,0,.004)*gauss(z,nosez-.028,.013)
    for side in(-1,1):
        value-=.016*gauss(x,side*.080,.040)*gauss(z,wp(620,202)[1],.040)
        value+=.009*gauss(x,side*.050,.035)*gauss(z,wp(620,171)[1],.021)
        value-=.009*gauss(x,side*.024,.013)*gauss(z,nosez-.003,.012)
        value+=.005*gauss(x,side*.021,.006)*gauss(z,nosez-.011,.005)
        value-=.005*gauss(x,side*.049,.038)*gauss(z,wp(620,144)[1],.010)
        # Continuous raised eyelid rim, blended into the same skin surface.
        eye_radius=math.sqrt(((x-side*.050)/.033)**2+((z-wp(620,171)[1])/.019)**2)
        value-=.0028*gauss(eye_radius,1.05,.18)
    return front*value
headrows=[(56,620,1,.003),(61,620,28,.035),(72,620,57,.078),(90,620,71,.11),(106,620,75,.123),(129,620,76,.124),(158,620,78,.130),(188,620,79,.134),(215,620,74,.127),(239,620,68,.117),(263,620,57,.103),(280,620,40,.075),(292,620,39,.069),(316,620,43,.070),(345,620,53,.08)]
head_face_start=len(F)
loft('Face sculpt',headrows,SKIN,'Head',seg=192,steps=16,detail=face_detail)
for idx in range(head_face_start,len(F)):
    points=[V[v] for v in F[idx]];cy=sum(p[1] for p in points)/len(points);py=FLOOR-sum(p[2] for p in points)/len(points)/S
    if py<107 or (cy>0 and py<188):MI[idx]=HAIR
    elif cy>0 and 215<py<287:MI[idx]=BEARD
# Ears with concha depth, faithfully positioned at the sides of the skull.
for s in(-1,1):
    x,z=wp(620+s*82,190);ell('Ear', (x,.007,z),(.023,.028,.046),SKIN,'Head',seg=32)
# Join overlapping cloth into organic surfaces without changing the source painting.
for matid in (SHIRT,PANTS):
    fs=[f for f,mi in zip(F,MI) if mi==matid];used=sorted({v for f in fs for v in f});ids={v:i for i,v in enumerate(used)}
    mesh=bpy.data.meshes.new('Cloth');mesh.from_pydata([V[i] for i in used],[],[tuple(ids[i] for i in f) for f in fs]);mesh.update()
    ob=bpy.data.objects.new('Cloth',mesh);bpy.context.collection.objects.link(ob);bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    mod=ob.modifiers.new('Cloth union','REMESH');mod.mode='VOXEL';mod.voxel_size=.003;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Smooth tailoring','SMOOTH');mod.factor=.65;mod.iterations=3;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Game topology','DECIMATE');mod.ratio=.24;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Final cloth relaxation','SMOOTH');mod.factor=.3;mod.iterations=2;bpy.ops.object.modifier_apply(modifier=mod.name)
    kept=[(f,mi) for f,mi in zip(F,MI) if mi!=matid];F=[f for f,mi in kept];MI=[mi for f,mi in kept]
    def weight(p):
        x,y,z=p;tag='L' if x>0 else 'R'
        if matid==PANTS:
            if z>.77:return {'Hips':1}
            t=smooth((z-.33)/.21);return {'UpperLeg.'+tag:t,'LowerLeg.'+tag:1-t}
        if abs(x)>.255 and z<1.38:
            t=smooth((z-1.0)/.16);return {'UpperArm.'+tag:t,'Forearm.'+tag:1-t}
        return body_weight(p)
    verts=[tuple(v.co) for v in ob.data.vertices]
    cloth_faces=[]
    for face in ob.data.polygons:
        p=face.center;px=abs(p.x/S);py=FLOOR-p.z/S
        # Open the V neckline, leaving the continuous neck behind it.
        cloth_faces.append(tuple(face.vertices))
    # Orthographic projection should reach the front of the sleeves too.
    factors=[1.0 if v.co.z>wp(620,351)[1] and abs(v.co.x)<75*S else smooth((-v.co.y-.012)/.070) for v in ob.data.vertices]
    add('Continuous clothes',verts,cloth_faces,matid,weight,factors)
    bpy.data.objects.remove(ob,do_unlink=True)
# Unite the neck/collar/head surfaces into a continuous sculpture before the
# reference material is baked. Preserve authored material regions and skinning
# through nearest-surface transfer; the source image is unchanged.
from mathutils.bvhtree import BVHTree
surface=BVHTree.FromPolygons([Vector(v) for v in V],F)
oldV=V;oldF=F;oldMI=MI;oldWeights=WEIGHT;oldProj=PROJ
sm=bpy.data.meshes.new('ContinuousSculpt');sm.from_pydata(V,[],F);sm.update()
so=bpy.data.objects.new('ContinuousSculpt',sm);bpy.context.collection.objects.link(so)
bpy.ops.object.select_all(action='DESELECT');so.select_set(True);bpy.context.view_layer.objects.active=so
bm=bmesh.new();bm.from_mesh(sm);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(sm);bm.free()
mod=so.modifiers.new('Unified original sculpture','REMESH');mod.mode='VOXEL';mod.voxel_size=.0020;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=so.modifiers.new('Surface relaxation','SMOOTH');mod.factor=.5;mod.iterations=3;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=so.modifiers.new('Game-ready surface','DECIMATE');mod.ratio=.105;bpy.ops.object.modifier_apply(modifier=mod.name)
V=[tuple(v.co) for v in so.data.vertices];F=[tuple(p.vertices) for p in so.data.polygons];MI=[];WEIGHT=[];PROJ=[]
for vert in so.data.vertices:
    loc,normal,idx,dist=surface.find_nearest(vert.co)
    face=oldF[idx];weights=[1/max(.00001,(Vector(oldV[i])-loc).length)**2 for i in face];total=sum(weights)
    combined={};projection=0
    for i,weight in zip(face,weights):
        weight/=total;projection+=oldProj[i]*weight
        for b,w in oldWeights[i].items():combined[b]=combined.get(b,0)+w*weight
    WEIGHT.append(combined);PROJ.append(projection)
for face in so.data.polygons:
    loc,normal,idx,dist=surface.find_nearest(face.center);MI.append(oldMI[idx])
bpy.data.objects.remove(so,do_unlink=True)
# Compact away draft vertices before UVs, weights and expressions are authored.
used=sorted({i for f in F for i in f});ids={old:new for new,old in enumerate(used)}
V=[V[i] for i in used];WEIGHT=[WEIGHT[i] for i in used];PROJ=[PROJ[i] for i in used];F=[tuple(ids[i] for i in f) for f in F]
from apo_detail_geometry import enrich
detail_report=enrich(globals())
me=bpy.data.meshes.new('ApoDetailedSculpt');me.from_pydata(V,[],F);me.update()
ob=bpy.data.objects.new('ApoBody',me);bpy.context.collection.objects.link(ob)
for mat in materials:me.materials.append(mat)
for p,mi in zip(me.polygons,MI):p.material_index=mi;p.use_smooth=True
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
# Temporary projection UVs drive the bake; the source image stays unchanged.
uv=me.uv_layers.new(name='ReferenceProjection');attr=me.color_attributes.new(name='ProjectionWeight',type='FLOAT_COLOR',domain='CORNER')
baseattr=me.color_attributes.new(name='SculptBaseTone',type='FLOAT_COLOR',domain='CORNER')
for poly in me.polygons:
    for li in poly.loop_indices:
        loop=me.loops[li];p=me.vertices[loop.vertex_index].co
        c=materials[poly.material_index].diffuse_color
        if poly.material_index in (SKIN,HAIR,BEARD) and WEIGHT[loop.vertex_index].get('Head',0)>.9:
            py=FLOOR-p.z/S;angle=math.atan2(abs(p.x),-p.y)
            line=98+60*abs(math.sin(angle))**3+78*max(0,-math.cos(angle))+34*gauss(angle,1.08,.18)
            hairmask=smooth((line-py)/3)
            beardmask=smooth((py-207)/8)*smooth((287-py)/10)*smooth((1.75-angle)/.35)
            skincol=srgb('#D99A6D');haircol=srgb('#241F1B');beardcol=srgb('#493528')
            c=tuple((skincol[k]*(1-beardmask)+beardcol[k]*beardmask)*(1-hairmask)+haircol[k]*hairmask for k in range(3))+(1,)
        baseattr.data[li].color=c
for loop in me.loops:
    p=me.vertices[loop.vertex_index].co
    u=(p.x/S+CX)/W;v=1-(FLOOR-p.z/S)/H
    uv.data[loop.index].uv=(u,v);w=PROJ[loop.vertex_index];attr.data[loop.index].color=(w,w,w,1)
faceuv=me.uv_layers.new(name='FaceDetailProjection');faceattr=me.color_attributes.new(name='FaceDetailWeight',type='FLOAT_COLOR',domain='CORNER')
for loop in me.loops:
    p=me.vertices[loop.vertex_index].co;x=p.x/S+CX;y=FLOOR-p.z/S
    uu=face_affine[0][0]*x+face_affine[0][1]*y+face_affine[0][2]
    vv=face_affine[1][0]*x+face_affine[1][1]*y+face_affine[1][2]
    faceuv.data[loop.index].uv=(uu/face_image.size[0],1-vv/face_image.size[1])
    fw=WEIGHT[loop.vertex_index].get('Head',0)*smooth((p.z-1.386)/.075)
    faceattr.data[loop.index].color=(fw,fw,fw,1)
# Bake a conventional atlas for Unity and interchange exports.
bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
atlasuv=me.uv_layers.new(name='UVMap');me.uv_layers.active_index=len(me.uv_layers)-1;atlasuv.active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.0015);bpy.ops.object.mode_set(mode='OBJECT')
atlas=bpy.data.images.new('Apo_Albedo',width=4096,height=4096,alpha=True)
for mat in materials:
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=atlas;mat.node_tree.nodes.active=node
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True;scene.render.bake.margin=12
bpy.ops.object.bake(type='DIFFUSE')
atlas.filepath_raw=str(OUT/'Apo_Albedo.png');atlas.file_format='PNG';atlas.save();atlas.pack()
# Preserve material response when the model uses a single atlas in Unity.
def bake_map(name,kind):
    result=bpy.data.images.new(name,width=2048,height=2048,alpha=True)
    result.colorspace_settings.name='Non-Color'
    for mat in materials:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=result;mat.node_tree.nodes.active=node
    bpy.ops.object.bake(type=kind)
    result.filepath_raw=str(OUT/(name+'.png'));result.file_format='PNG';result.save();result.pack()
    return result
roughness=bake_map('Apo_Roughness','ROUGHNESS')
normal=bake_map('Apo_Normal','NORMAL')
# Unity's metallic workflow stores metallic in R, and smoothness in A.
for mat in materials:
    nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');em=nt.nodes.new('ShaderNodeEmission')
    metal=bs.inputs['Metallic'].default_value;em.inputs['Color'].default_value=(metal,metal,metal,1)
    nt.links.new(em.outputs[0],nt.nodes.get('Material Output').inputs['Surface'])
metallic=bake_map('Apo_Metallic','EMIT')
import numpy as np
roughpixels=np.array(roughness.pixels[:],dtype=np.float32).reshape(-1,4)
surfacepixels=np.array(metallic.pixels[:],dtype=np.float32).reshape(-1,4)
surfacepixels[:,1:3]=0;surfacepixels[:,3]=1-roughpixels[:,0]
surface_map=bpy.data.images.new('Apo_Surface',width=2048,height=2048,alpha=True)
surface_map.colorspace_settings.name='Non-Color';surface_map.pixels.foreach_set(surfacepixels.flatten())
surface_map.filepath_raw=str(OUT/'Apo_Surface.png');surface_map.file_format='PNG';surface_map.save();surface_map.pack()
baked=bpy.data.materials.new('Apo_ReferenceMaterial');baked.use_nodes=True;bs=baked.node_tree.nodes.get('Principled BSDF')
bs.inputs['Roughness'].default_value=.75;bs.inputs['Specular IOR Level'].default_value=.22
tex=baked.node_tree.nodes.new('ShaderNodeTexImage');tex.image=atlas
uvnode=baked.node_tree.nodes.new('ShaderNodeUVMap');uvnode.uv_map='UVMap';baked.node_tree.links.new(uvnode.outputs['UV'],tex.inputs['Vector']);baked.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
for im,socket in ((roughness,'Roughness'),(metallic,'Metallic')):
    t=baked.node_tree.nodes.new('ShaderNodeTexImage');t.image=im;baked.node_tree.links.new(uvnode.outputs['UV'],t.inputs['Vector']);baked.node_tree.links.new(t.outputs['Color'],bs.inputs[socket])
n=baked.node_tree.nodes.new('ShaderNodeTexImage');n.image=normal;baked.node_tree.links.new(uvnode.outputs['UV'],n.inputs['Vector'])
decode=baked.node_tree.nodes.new('ShaderNodeNormalMap');decode.uv_map='UVMap';baked.node_tree.links.new(n.outputs['Color'],decode.inputs['Color']);baked.node_tree.links.new(decode.outputs['Normal'],bs.inputs['Normal'])
me.materials.clear();me.materials.append(baked)
for p in me.polygons:p.material_index=0
# Export UVMap as the first UV channel.
reference=me.uv_layers.get('ReferenceProjection');me.uv_layers.remove(reference);me.uv_layers.active_index=0
me.uv_layers.remove(me.uv_layers.get('FaceDetailProjection'));me.uv_layers.active_index=0
# Surface expressions keep the original eye/mouth placement.
basis=ob.shape_key_add(name='Basis');blink=ob.shape_key_add(name='Blink');talk=ob.shape_key_add(name='Talk')
eyez=wp(620,171)[1];mouthz=wp(620,238)[1]
for i,v in enumerate(me.vertices):
    x,y,z=v.co
    if y<-.04 and z>1.4:
        mask=max(gauss(x,wp(589,0)[0],.026),gauss(x,wp(647,0)[0],.026))*gauss(z,eyez,.022)
        blink.data[i].co.z+=(eyez-z)*.94*mask
        talk.data[i].co.z-=.0035*gauss(x,0,.052)*gauss(z,mouthz-.009,.018)
blink.value=0;talk.value=0
arm=bpy.data.armatures.new('ApoRig');rig=bpy.data.objects.new('ApoRig',arm);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None):
    b=arm.edit_bones.new(n);b.head=h;b.tail=t
    if parent:b.parent=arm.edit_bones[parent]
bone('Root',(0,0,0),(0,0,.1));bone('Hips',(0,0,.79),(0,0,.94),'Root');bone('Chest',(0,0,.94),(0,0,1.39),'Hips')
bone('Neck',(0,0,1.39),(0,0,1.51),'Chest');bone('Head',(0,0,1.51),(0,0,1.81),'Neck')
for s in(-1,1):
    tag='L' if s>0 else 'R'
    bone('UpperArm.'+tag,(s*.265,0,1.385),(s*.374,0,1.065),'Chest')
    bone('Forearm.'+tag,(s*.374,0,1.065),(s*.407,0,.85),'UpperArm.'+tag)
    bone('Hand.'+tag,(s*.407,0,.85),(s*.406,0,.68),'Forearm.'+tag)
    bone('UpperLeg.'+tag,(s*.135,0,.81),(s*.14,0,.435),'Hips')
    bone('LowerLeg.'+tag,(s*.14,0,.435),(s*.146,0,.13),'UpperLeg.'+tag)
    bone('Foot.'+tag,(s*.146,0,.13),(s*.169,-.16,.06),'LowerLeg.'+tag)
bpy.ops.object.mode_set(mode='OBJECT')
for b in arm.bones:ob.vertex_groups.new(name=b.name)
for i,weights in enumerate(WEIGHT):
    for name,w in weights.items():
        if w>0:ob.vertex_groups[name].add([i],w,'REPLACE')
mod=ob.modifiers.new('ApoSkin','ARMATURE');mod.object=rig;ob.parent=rig
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);ob.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'ApoOriginal.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'ApoOriginal.glb'),export_format='GLB',use_selection=True)
# Soft studio lighting like the approved family renders.
scene.cycles.samples=48;scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world.color=(.18,.18,.18);scene.view_settings.view_transform='AgX'
floor=bpy.data.materials.new('Studio ivory');floor.diffuse_color=(.72,.76,.78,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.01));bpy.context.object.data.materials.append(floor)
def area(n,p,e,size):
    d=bpy.data.lights.new(n,'AREA');d.energy=e;d.shape='DISK';d.size=size;o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);o.location=p;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-3,-4,5),350,4);area('Fill',(3,-2,3),180,3);area('Rim',(1,2,4),350,3)
d=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',d);bpy.context.collection.objects.link(camera);scene.camera=camera;d.type='ORTHO';d.ortho_scale=2.12
def render(name,pos,target,scale):
    camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=scale
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ApoOriginal.blend'))
render('Front',(0,-8,2.15),(0,0,.94),2.12)
render('ThreeQuarter',(5.7,-8,2.25),(0,0,.94),2.12)
render('Face',(1.1,-8,2.1),(0,-.04,1.57),.64)
render('Profile',(8,-2.6,2.1),(0,-.04,1.57),.64)
(OUT/'provenance.json').write_text(json.dumps({'source_image':'ArtDirection/CharacterReferences/AbdullahEkinci_Reference_v1.png','geometry':'Original reference-driven sculpture, no Meshy or imported geometry','vertices':len(me.vertices),'triangles':sum(len(p.vertices)-2 for p in me.polygons),'texture':'4096px baked reference projection','texture_sources':['ArtDirection/CharacterReferences/AbdullahEkinci_Detail_v2.png','ArtDirection/CharacterReferences/AbdullahEkinci_FaceDetail_v3.png'],'material_maps':['Apo_Albedo.png (4096)','Apo_Normal.png (2048)','Apo_Surface.png (2048, metallic R / smoothness A)'],'bones':17,'sculpt_details':detail_report},indent=2))
print('APO_REFERENCE_SCULPT_COMPLETE')



