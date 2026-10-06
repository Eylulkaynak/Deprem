"""Original Apo / Abdullah Ekinci sculpture, authored from the approved 2D reference.
Offline Blender production. No Meshy geometry, no downloaded character meshes.
Blender 5.x: --background --factory-startup --python Tools/Runner/build_apo.py
"""
import bpy, math, json, pathlib
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtDirection/CharacterModels/ApoOriginal'
UNITY=ROOT/'Assets/Story/Characters/ApoOriginal'
OUT.mkdir(parents=True,exist_ok=True); UNITY.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
V=[]; F=[]; M=[]; W=[]; PARTS=[]; mats=[]; colors={}
def col(s):
    x=[int(s[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(a/12.92 if a<=.04045 else ((a+.055)/1.055)**2.4 for a in x)+(1,)
def mat(n,c,r=.55,metal=0):
    m=bpy.data.materials.new(n);m.diffuse_color=col(c);m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=col(c);b.inputs['Roughness'].default_value=r;b.inputs['Metallic'].default_value=metal
    if n=='Skin':b.inputs['Subsurface Weight'].default_value=.07
    colors[n]={'color':c,'roughness':r,'metallic':metal};mats.append(m);return len(mats)-1
SKIN=mat('Skin','#E7A16C'); LIP=mat('Lips','#B77556'); INNER=mat('EarInset','#CA845B')
HAIR=mat('Hair','#201D1C',.43); RIDGE=mat('HairRidges','#322B26',.5); BEARD=mat('Beard','#3A2B23',.7)
SHIRT=mat('Shirt','#A3C6EE',.76); SEAM=mat('ShirtSeams','#789DC4',.74); COLLAR=mat('Collar','#B3D2F1',.72)
PANTS=mat('Trousers','#243247',.8); BLACK=mat('Leather','#202124',.42); SOLE=mat('Soles','#111619',.62)
STEEL=mat('Buckle','#8F989D',.3,.7); WHITE=mat('EyeWhite','#FFF3E3',.24); IRIS=mat('Iris','#764522',.3)
PUPIL=mat('Pupil','#10151C',.2); LIGHT=mat('Catchlight','#FFFFFF',.15); MOUTH=mat('Smile','#60392D',.65)
def add(name,v,f,material,bone):
    st=len(V);V.extend([tuple(p) for p in v]);F.extend([tuple(st+k for k in face) for face in f]);M.extend([material]*len(f))
    W.extend([bone(p) if callable(bone) else {bone:1} for p in v]);PARTS.append((name,st,len(v)))
def ell(name,c,r,ma,bone='Head',seg=32,rings=20):
    v=[];f=[]
    for j in range(rings+1):
        p=math.pi*j/rings
        for i in range(seg):
            a=math.tau*i/seg;v.append((c[0]+r[0]*math.sin(p)*math.cos(a),c[1]+r[1]*math.sin(p)*math.sin(a),c[2]+r[2]*math.cos(p)))
    for j in range(rings):
        for i in range(seg):f.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    add(name,v,f,ma,bone)
def loft(name,rings,ma,bone='Chest',seg=40):
    v=[];f=[]
    for x,y,z,rx,ry in rings:
        for i in range(seg):
            a=math.tau*i/seg;v.append((x+rx*math.sin(a),y-ry*math.cos(a),z))
    for j in range(len(rings)-1):
        for i in range(seg):f.append((j*seg+i,(j+1)*seg+i,(j+1)*seg+(i+1)%seg,j*seg+(i+1)%seg))
    f.extend([tuple(reversed(range(seg))),tuple((len(rings)-1)*seg+i for i in range(seg))]);add(name,v,f,ma,bone)
def tube(name,path,rs,ma,bone='Head',seg=10):
    v=[];f=[];p=[Vector(x) for x in path]
    for j,c in enumerate(p):
        d=(p[min(j+1,len(p)-1)]-p[max(0,j-1)]).normalized();u=d.cross(Vector((0,1,0)))
        if u.length<.01:u=d.cross(Vector((1,0,0)))
        u.normalize();w=d.cross(u).normalized();r=rs[j] if isinstance(rs,list) else rs
        for k in range(seg):
            a=math.tau*k/seg;v.append(c+r*(math.cos(a)*u+math.sin(a)*w))
    for j in range(len(p)-1):
        for i in range(seg):f.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    f.extend([tuple(reversed(range(seg))),tuple((len(p)-1)*seg+i for i in range(seg))]);add(name,v,f,ma,bone)
def panel(name,points,ma,bone='Chest',depth=.005):
    n=len(points);v=points+[(x,y+depth,z) for x,y,z in points]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    add(name,v,f,ma,bone)
def torso_weight(p):
    t=max(0,min(1,(p[2]-1.05)/.28))
    return {'Hips':1-t,'Chest':t}
# Tailored silhouette with a rounded belly and broad shoulders.
loft('Shirt',[(0,0,1.00,.236,.135),(0,-.005,1.035,.252,.145),(0,-.01,1.14,.258,.153),(0,-.005,1.27,.253,.146),(0,0,1.39,.273,.134),(0,0,1.46,.282,.117),(0,0,1.50,.234,.103),(0,0,1.525,.106,.086)],SHIRT,torso_weight)
loft('Neck',[(0,0,1.45,.077,.078),(0,0,1.53,.076,.071),(0,0,1.58,.088,.076)],SKIN,'Neck',32)
loft('Waist',[(0,0,.86,.236,.127),(0,0,.99,.234,.136),(0,0,1.015,.231,.135)],PANTS,'Hips')
loft('Belt',[(0,0,.982,.239,.141),(0,0,1.018,.241,.143)],BLACK,'Hips')
panel('Buckle',[(-.034,-.150,.982),(.034,-.150,.982),(.034,-.150,1.02),(-.034,-.150,1.02)],STEEL,'Hips',.004)
panel('BuckleInset',[(-.023,-.156,.990),(.023,-.156,.990),(.023,-.156,1.012),(-.023,-.156,1.012)],BLACK,'Hips')
for x in [-.165,.165]:
    panel('BeltLoop',[(x-.008,-.133,.972),(x+.008,-.133,.972),(x+.008,-.133,1.03),(x-.008,-.133,1.03)],PANTS,'Hips')
for side in [-1,1]:
    tag='L' if side>0 else 'R'; leg='UpperLeg.'+tag; lower='LowerLeg.'+tag
    def lw(p,leg=leg,lower=lower):
        t=max(0,min(1,(p[2]-.43)/.18));return {leg:t,lower:1-t}
    loft('TrouserLeg.'+tag,[(side*.132,.005,.14,.082,.075),(side*.135,.005,.19,.086,.076),(side*.137,.005,.42,.084,.083),(side*.138,0,.52,.091,.091),(side*.132,0,.75,.109,.114),(side*.121,0,.91,.122,.129)],PANTS,lw)
    ell('Shoe.'+tag,(side*.136,-.066,.088),(.095,.184,.08),BLACK,'Foot.'+tag)
    ell('Sole.'+tag,(side*.136,-.066,.041),(.098,.185,.027),SOLE,'Foot.'+tag)
    for k in range(3):
        tube('Laces',[(side*.136-.040,-.093+k*.025,.152-k*.006),(side*.136+.040,-.093+k*.025,.152-k*.006)],.004,SOLE,'Foot.'+tag,6)
    arm='UpperArm.'+tag; fore='Forearm.'+tag; hand='Hand.'+tag
    def aw(p,arm=arm,fore=fore):
        t=max(0,min(1,(p[2]-1.15)/.13));return {arm:t,fore:1-t}
    loft('Sleeve.'+tag,[(side*.352,-.012,.957,.050,.062),(side*.358,-.01,1.02,.064,.075),(side*.347,0,1.14,.072,.079),(side*.323,0,1.28,.085,.082),(side*.278,0,1.44,.105,.095),(side*.249,0,1.48,.083,.082)],SHIRT,aw,32)
    loft('Cuff.'+tag,[(side*.353,-.012,.938,.052,.064),(side*.355,-.012,.993,.056,.069)],COLLAR,fore,32)
    ell('Palm.'+tag,(side*.359,-.015,.888),(.048,.039,.078),SKIN,hand,24,16)
    for j in range(4):
        x=side*(.330+j*.019)
        z=.855-(.012 if j in (1,2) else 0)
        tube('Finger.'+tag,[(x,-.021,.88),(x+side*.004,-.029,z),(x+side*.004,-.04,z-.047),(x,-.045,z-.054)],[.011,.011,.009,.004],SKIN,hand,10)
    tube('Thumb.'+tag,[(side*.325,-.032,.915),(side*.305,-.045,.883),(side*.305,-.055,.861)],[.016,.014,.007],SKIN,hand,12)
    ell('CuffButton.'+tag,(side*.352,-.083,.971),(.007,.004,.007),WHITE,fore,16,8)
# Collar, placket and buttons follow the shirt surface.
for s in [-1,1]:
    panel('CollarFold',[(s*.071,-.079,1.538),(s*.133,-.118,1.49),(s*.109,-.146,1.416),(s*.034,-.141,1.482)],COLLAR)
tube('Placket',[(0,-.141,1.02),(0,-.164,1.13),(0,-.151,1.29),(0,-.14,1.45)],.008,COLLAR,'Chest',8)
for z,y in [(1.06,-.151),(1.16,-.165),(1.26,-.153),(1.36,-.148),(1.44,-.147)]:
    ell('Button',(0,y-.007,z),(.007,.0038,.007),WHITE,'Chest',16,8)
# Wristwatch on left wrist.
ell('WatchBand',(.355,-.01,.957),(.056,.070,.017),BLACK,'Forearm.L',24,12)
ell('WatchCase',(.355,-.079,.960),(.022,.006,.026),STEEL,'Forearm.L',24,12)
ell('WatchFace',(.355,-.085,.960),(.018,.004,.022),BLACK,'Forearm.L',24,12)
tube('WatchHands',[(.349,-.090,.967),(.355,-.091,.960),(.364,-.090,.964)],.0017,WHITE,'Forearm.L',6)
# Continuous head surface with broad jaw and cheek planes.
HZ=1.662; HH=.448
def face(t,a,offset=0):
    r=math.sqrt(max(.000001,1-(2*t-1)**2)); jaw=.86+.14*min(1,t/.40)
    x=.166*r*jaw*math.sin(a)
    y=-.142*r*math.cos(a)
    y-=.010*max(0,math.cos(a))**6*math.exp(-((t-.33)/.17)**2)
    z=HZ+(t-.5)*HH
    return (x+math.sin(a)*offset,y-math.cos(a)*offset,z)
v=[];f=[];ns=80;nr=50
for j in range(nr+1):
    for i in range(ns):v.append(face(.001+.998*j/nr,-math.pi+math.tau*i/ns))
for j in range(nr):
    for i in range(ns):f.append((j*ns+i,j*ns+(i+1)%ns,(j+1)*ns+(i+1)%ns,(j+1)*ns+i))
add('Head',v,f,SKIN,'Head')
for s in [-1,1]:
    ell('Ear',(s*.168,.004,1.664),(.028,.027,.053),SKIN)
    ell('EarConcha',(s*.181,-.018,1.664),(.013,.012,.034),INNER)
    ell('Tragus',(s*.167,-.026,1.650),(.012,.012,.021),SKIN)
# Shaped nose bridge and tip, no flat facial texture.
ell('NoseBridge',(0,-.138,1.666),(.023,.024,.061),SKIN)
ell('NoseTip',(0,-.170,1.628),(.029,.027,.022),SKIN)
for s in [-1,1]:
    ell('NoseWing',(s*.025,-.155,1.621),(.017,.020,.012),SKIN)
    ell('Nostril',(s*.022,-.170,1.614),(.008,.003,.004),INNER,seg=16,rings=8)
# Almond eye outlines, layered irises, soft raised brows.
for s in [-1,1]:
    ex=s*.068;ez=1.698
    ell('EyeWhite',(ex,-.135,ez),(.035,.023,.032),WHITE)
    ell('Iris',(ex,-.157,ez),(.019,.006,.021),IRIS,seg=28,rings=16)
    ell('Pupil',(ex,-.162,ez),(.011,.003,.014),PUPIL,seg=24,rings=12)
    ell('EyeGlint',(ex-.005,-.1655,ez+.009),(.005,.0018,.0055),LIGHT,seg=16,rings=8)
    upper=[];lower=[]
    for j in range(17):
        a=math.pi*j/16
        upper.append((ex+.036*math.cos(a),-.142-.013*math.sin(a),ez+.031*math.sin(a)))
        lower.append((ex+.036*math.cos(a),-.142-.013*math.sin(a),ez-.026*math.sin(a)))
    tube('UpperLid',upper,.0045,SKIN)
    tube('LowerLid',lower,.003,SKIN)
    tube('UpperLash',[(x,y-.001,z-.002) for x,y,z in upper],.0018,BEARD,seg=6)
    brow=[]
    for j in range(14):
        a=j/13;x=ex+(a-.5)*.081
        brow.append((x,-.143+abs(x)*.13,1.753+.014*math.sin(math.pi*a)))
    tube('Eyebrow',brow,[.004+.006*max(0,math.sin(math.pi*j/13))**.5 for j in range(14)],HAIR,seg=10)
# Beard conforms to the jaw; individual short grooves add direction without noisy triangles.
v=[];f=[];cols=60;rows=16
for j in range(rows+1):
    for i in range(cols+1):
        a=-1.72+3.44*i/cols
        top=.30+.24*(abs(a)/1.72)**1.35
        t=.015+(top-.015)*j/rows
        v.append(face(t,a,.0028))
for j in range(rows):
    for i in range(cols):k=j*(cols+1)+i;f.append((k,k+1,k+cols+2,k+cols+1))
add('SculptedBeard',v,f,BEARD,'Head')
for s in [-1,1]:
    pts=[]
    for i in range(14):
        t=i/13;pts.append((s*(.007+.052*t),-.159+.035*t*t,1.595-.011*t+.003*math.sin(t*math.pi)))
    tube('Moustache',pts,[.005+.008*max(0,math.sin(math.pi*i/13))**.5 for i in range(14)],BEARD,seg=10)
# A small warm smile sits inside beard, with a skin patch and shaped lips.
ell('MouthSkin',(0,-.133,1.569),(.052,.013,.027),SKIN)
for name,z,ma,r in [('UpperLip',1.571,LIP,.004),('SmileLine',1.567,MOUTH,.0027),('LowerLip',1.560,LIP,.005)]:
    pts=[]
    for j in range(23):
        t=j/22;x=(t-.5)*.087;pts.append((x,-.149+.012*(abs(x)/.044)**2,z+.012*(abs(x)/.044)**2))
    tube(name,pts,[r*(.4+.6*math.sin(math.pi*j/22)) for j in range(23)],ma,seg=8)
# Swept-back hair cap, attached at a precise hairline.
def hair(t,a):
    base=.83-.18*abs(math.sin(a))-.30*max(0,-math.cos(a))
    u=base+(1-base)*t
    x,y,z=face(u,a,.009*(1-t))
    z+=.016+.018*max(0,math.cos(a))*(max(0,math.sin(math.pi*t))**.6)
    y+=.018*t
    return (x,y,z)
v=[];f=[];hs=80;hr=28
for j in range(hr+1):
    for i in range(hs):v.append(hair(j/hr,-math.pi+math.tau*i/hs))
for j in range(hr):
    for i in range(hs):f.append((j*hs+i,j*hs+(i+1)%hs,(j+1)*hs+(i+1)%hs,(j+1)*hs+i))
add('SweptHairCap',v,f,HAIR,'Head')
for k in range(25):
    a=-1.55+3.10*k/24;path=[]
    for j in range(22):
        t=.025+.83*j/21;aa=a+.35*math.sin(t*math.pi)
        x,y,z=hair(t,aa);path.append((x,y-.001,z+.0015))
    tube('CombedStrand',path,[.001+.0015*math.sin(math.pi*j/21) for j in range(22)],RIDGE,seg=5)
# Fuse only the tailored clothing pieces offline so the shoulders and crotch
# have continuous surfaces, rather than intersecting primitive silhouettes.
for cloth, label in [(SHIRT,'TailoredShirt'),(PANTS,'TailoredTrousers')]:
    selected=[f for f,ma in zip(F,M) if ma==cloth]
    used=sorted({i for f in selected for i in f});remap={old:new for new,old in enumerate(used)}
    cm=bpy.data.meshes.new(label);cm.from_pydata([V[i] for i in used],[],[tuple(remap[i] for i in f) for f in selected]);cm.update()
    co=bpy.data.objects.new(label,cm);bpy.context.collection.objects.link(co)
    bpy.context.view_layer.objects.active=co;co.select_set(True)
    import bmesh
    bm=bmesh.new();bm.from_mesh(cm);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(cm);bm.free()
    mod=co.modifiers.new('SeamlessTailoring','REMESH');mod.mode='VOXEL';mod.voxel_size=.0045;mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=co.modifiers.new('SoftCloth','SMOOTH');mod.factor=1;mod.iterations=5;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=co.modifiers.new('GameTopology','DECIMATE');mod.ratio=.24;bpy.ops.object.modifier_apply(modifier=mod.name)
    kept=[(f,ma) for f,ma in zip(F,M) if ma!=cloth];F=[x[0] for x in kept];M=[x[1] for x in kept]
    def clothweight(p):
        x,y,z=p
        tag='L' if x>0 else 'R'
        if cloth==PANTS:
            if z>.84:return {'Hips':1}
            t=max(0,min(1,(z-.43)/.18));return {'UpperLeg.'+tag:t,'LowerLeg.'+tag:1-t}
        if abs(x)>.25 and z<1.44:
            t=max(0,min(1,(z-1.15)/.13));return {'UpperArm.'+tag:t,'Forearm.'+tag:1-t}
        return torso_weight(p)
    add(label,[tuple(v.co) for v in co.data.vertices],[tuple(p.vertices) for p in co.data.polygons],cloth,clothweight)
    bpy.data.objects.remove(co,do_unlink=True)
# Assemble one editable skinned mesh and repair winding geometrically.
mesh=bpy.data.meshes.new('ApoSculpture');mesh.from_pydata(V,[],F);mesh.materials.clear()
for m in mats:mesh.materials.append(m)
mesh.update();obj=bpy.data.objects.new('ApoBody',mesh);bpy.context.collection.objects.link(obj)
for p,mi in zip(mesh.polygons,M):p.material_index=mi;p.use_smooth=True
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
import bmesh
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
obj.shape_key_add(name='Basis')
blink=obj.shape_key_add(name='Blink')
for name,start,count in PARTS:
    if name in ('EyeWhite','Iris','Pupil','EyeGlint','UpperLid','LowerLid','UpperLash'):
        for idx in range(start,start+count):
            p=blink.data[idx].co;p.z=1.698+(p.z-1.698)*.025
talk=obj.shape_key_add(name='Talk')
for name,start,count in PARTS:
    if name in ('LowerLip','SmileLine'):
        for idx in range(start,start+count):talk.data[idx].co.z-=.010
# A usable named rig for future animation, with hand-authored skin weights.
arm=bpy.data.armatures.new('ApoRig');rig=bpy.data.objects.new('ApoRig',arm);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None):
    b=arm.edit_bones.new(n);b.head=h;b.tail=t
    if parent:b.parent=arm.edit_bones[parent]
bone('Root',(0,0,0),(0,0,.12))
bone('Hips',(0,0,.89),(0,0,1.06),'Root');bone('Chest',(0,0,1.06),(0,0,1.43),'Hips')
bone('Neck',(0,0,1.43),(0,0,1.55),'Chest');bone('Head',(0,0,1.55),(0,0,1.84),'Neck')
for s in [-1,1]:
    t='L' if s>0 else 'R'
    bone('UpperArm.'+t,(s*.243,0,1.46),(s*.342,0,1.19),'Chest')
    bone('Forearm.'+t,(s*.342,0,1.19),(s*.354,-.012,.957),'UpperArm.'+t)
    bone('Hand.'+t,(s*.354,-.012,.957),(s*.357,-.021,.835),'Forearm.'+t)
    bone('UpperLeg.'+t,(s*.128,0,.92),(s*.137,0,.52),'Hips')
    bone('LowerLeg.'+t,(s*.137,0,.52),(s*.135,0,.15),'UpperLeg.'+t)
    bone('Foot.'+t,(s*.135,0,.15),(s*.135,-.18,.07),'LowerLeg.'+t)
bpy.ops.object.mode_set(mode='OBJECT')
for b in arm.bones:obj.vertex_groups.new(name=b.name)
for i,weights in enumerate(W):
    for b,w in weights.items():
        if w>0:obj.vertex_groups[b].add([i],w,'REPLACE')
mod=obj.modifiers.new('ApoSkin','ARMATURE');mod.object=rig;obj.parent=rig
# Export only original authored objects.
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(UNITY/'ApoOriginal.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_custom_props=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'ApoOriginal.glb'),export_format='GLB',use_selection=True)
(OUT/'materials.json').write_text(json.dumps(colors,indent=2))
(UNITY/'materials.json').write_text(json.dumps(colors,indent=2))
(OUT/'model-info.json').write_text(json.dumps({'source':'Original offline sculpture from AbdullahEkinci_Reference_v1.png','vertices':len(V),'faces':len(F),'bones':len(arm.bones),'blendshapes':['Blink','Talk'],'height_m':1.91,'meshy_used':False},indent=2))
# Studio preview; kept in the editable source file, excluded from FBX.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world.color=(.22,.22,.22);scene.view_settings.view_transform='AgX'
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.006));floor=bpy.context.object;floor.name='StudioFloor';fm=mat('Studio','#E4E9EE',.8);floor.data.materials.append(mats[fm])
def area(n,loc,power,size):
    data=bpy.data.lights.new(n,'AREA');data.energy=power;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-3,-4,5),400,4);area('Fill',(3,-2,3),140,3);area('Rim',(1,2,4),260,3)
data=bpy.data.cameras.new('Portrait');cam=bpy.data.objects.new('Portrait',data);bpy.context.collection.objects.link(cam)
scene.camera=cam;data.type='ORTHO';data.ortho_scale=2.22
cam.location=(2.5,-8,2.8);cam.rotation_euler=(Vector((0,0,1.02))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ApoOriginal.blend'))
scene.render.filepath=str(OUT/'ApoOriginal_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
cam.location=(0,-8,2.5);cam.rotation_euler=(Vector((0,0,1.02))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(OUT/'ApoOriginal_Front.png');bpy.ops.render.render(write_still=True)
print('APO_ORIGINAL_COMPLETE',len(V),len(F))




