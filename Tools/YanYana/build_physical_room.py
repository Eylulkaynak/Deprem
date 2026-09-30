"""Original, metre-scaled apartment shell and furniture with authored interaction anchors."""
import importlib.util, pathlib, math, bpy

HERE=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy_art', HERE/'build_original_art.py')
art=importlib.util.module_from_spec(spec); spec.loader.exec_module(art)
art.reset()

def pos(p): return (p[0],-p[2],p[1])
def dims(s): return (s[0],s[2],s[1])
def box(n,p,s,m,bevel=.025): return art.box(n,pos(p),dims(s),m,bevel)
def ball(n,p,s,m): return art.sphere(n,pos(p),dims(s),m)
def rod(n,a,b,r,m): return art.tube(n,pos(a),pos(b),r,m)
def anchor(n,p,scale=(1,1,1)):
    go=bpy.data.objects.new(n,None); go.location=pos(p); go.scale=dims(scale); bpy.context.collection.objects.link(go); return go
def collision(n,p,s): return anchor('COLLIDER_'+n,p,s)

# Open south/east walls are deliberate cutaways; playable floor remains continuous.
box('Architecture_Floor',(0,-.12,0),(7.8,.24,7.8),'cream',.06)
collision('Floor',(0,-.12,0),(7.8,.24,7.8))
for i in range(20):
    x=-3.7+i*.385
    box('FloorBoard', (x,.006,0),(.373,.018,7.65),'stone' if i%4 else 'cream',.005)
box('Architecture_NorthWall',(0,1.5,3.9),(7.9,3,.16),'cream',.025)
box('Architecture_WestWall',(-3.9,1.5,0),(.16,3,7.9),'cream',.025)
collision('NorthWall',(0,1.5,3.9),(7.9,3,.16)); collision('WestWall',(-3.9,1.5,0),(.16,3,7.9))
box('Skirting_North',(0,.14,3.79),(7.7,.26,.05),'tealDark',.015)
box('Skirting_West',(-3.79,.14,0),(.05,.26,7.7),'tealDark',.015)
for x in (-2.2,1.4):
    box('Window_Sill',(x,1.16,3.68),(1.65,.12,.34),'wood',.025)
    box('WindowFrame',(x,2.0,3.76),(1.65,1.68,.12),'tealDark',.04)
    box('Window_Sky',(x,2.0,3.68),(1.49,1.51,.055),'glass',.008)
    box('Window_CrossVertical',(x,2.0,3.62),(.065,1.57,.04),'cream',.01)
    box('Window_CrossHorizontal',(x,1.97,3.61),(1.51,.065,.04),'cream',.01)
    rod('Curtain_Rail',(x-.96,2.98,3.54),(x+.96,2.98,3.54),.025,'wood')
    for side in (-1,1):
        for j in range(4):
            ball('CurtainFold',(x+side*(.73+j*.053),2.05,3.54+math.sin(j)*.025),(.075,1.72,.10),'cream')

# A sofa sized for the cast, with individual upholstered forms and seams.
for x in (-3.25,-1.6):
    for z in (-.55,.45): box('Sofa_Foot',(x,.14,z),(.12,.28,.12),'wood',.03)
box('Sofa_Base',(-2.43,.32,-.06),(2.15,.35,1.12),'tealDark',.13)
box('Sofa_Back',(-2.43,.85,.39),(2.15,.93,.30),'teal',.12)
for x in (-3.39,-1.47): box('Sofa_Arm',(x,.66,-.08),(.26,.68,1.15),'teal',.115)
for x in (-2.93,-1.97):
    box('Sofa_Seat',(x,.55,-.16),(.91,.24,.83),'teal',.085)
    box('Sofa_BackCushion',(x,.94,.22),(.91,.66,.23),'teal',.085)
box('Sofa_CoralCushion',(-3.03,.79,-.19),(.44,.42,.16),'coral',.09).rotation_euler[1]=.13
box('Sofa_CreamCushion',(-1.80,.79,-.19),(.40,.40,.16),'cream',.09).rotation_euler[1]=-.12
collision('Sofa',(-2.43,.55,-.06),(2.2,1.05,1.12))

# A low, strong table: hands and cover anchors belong to the actual legs.
box('SafeTable_Top',(-.35,.94,.75),(1.48,.12,.98),'wood',.065)
for x in (-.94,.24):
    for z in (.38,1.12):
        box('SafeTable_Leg',(x,.45,z),(.115,.90,.115),'woodDark',.025)
        collision('TableLeg',(x,.45,z),(.115,.90,.115))
collision('TableTop',(-.35,.94,.75),(1.48,.12,.98))
anchor('Anchor_CoverAda',(-.53,0,.69)); anchor('Anchor_CoverEfe',(-.01,0,.69))
anchor('Anchor_HoldAda',(-.94,.45,.38)); anchor('Anchor_HoldEfe',(.24,.41,.38))
box('Living_Rug',(-.5,.025,.40),(3.8,.026,3.2),'coral',.08)
for x in (-2.32,1.32): box('Rug_Edge',(x,.041,.40),(.055,.006,3.0),'cream',.003)
for z in (-1.07,1.87): box('Rug_Edge',(-.5,.041,z),(3.55,.006,.055),'cream',.003)

# The work counter contains the real radio and flashlight work positions.
box('Workbench_Top',(1.25,.77,2.55),(2.55,.09,.85),'wood',.04)
for x in (.12,2.38):
    for z in (2.24,2.85): box('Workbench_Leg',(x,.37,z),(.10,.74,.10),'tealDark',.025)
collision('Workbench',(1.25,.42,2.55),(2.55,.83,.85))
box('Workbench_Drawer',(1.25,.62,2.16),(1.55,.22,.22),'cream',.025)
rod('Workbench_Handle',(1.0,.62,2.02),(1.5,.62,2.02),.019,'tealDark')
anchor('Anchor_FlashlightWork',(.65,.82,2.40)); anchor('Anchor_RadioWork',(1.87,.82,2.40))
anchor('Approach_Flashlight',(.65,0,1.56)); anchor('Approach_Radio',(1.87,0,1.56))

# A low packing bench. No enlarged floating inventory props.
box('PackingBench',(-2.58,.28,-2.65),(1.63,.55,1.98),'wood',.06)
box('PackingBench_Pad',(-2.58,.59,-2.65),(1.62,.13,1.98),'cream',.065)
collision('PackingBench',(-2.58,.32,-2.65),(1.65,.65,2.0))
anchor('Anchor_BagWork',(-2.58,.68,-2.25)); anchor('Approach_Bag',(-1.15,0,-2.65))

# Spatial landmarks help a child remember where things belong.
box('FamilyMap_Frame',(-3.75,1.61,1.85),(.12,.88,1.20),'wood',.045)
box('FamilyMap_Paper',(-3.67,1.61,1.85),(.04,.75,1.07),'cream',.015)
for y,z in [(1.4,1.53),(1.63,1.85),(1.84,2.14)]: ball('FamilyMap_TreeMark',(-3.63,y,z),(.03,.15,.15),'teal')
anchor('Anchor_FamilyMap',(-3.48,1.50,1.85)); anchor('Approach_FamilyMap',(-2.74,0,1.8))
anchor('Anchor_Shelf',(-3.19,0,3.05)); anchor('Approach_Shelf',(-2.55,0,2.65))
anchor('Anchor_Wardrobe',(3.18,0,3.06)); anchor('Approach_Wardrobe',(2.8,0,1.8))

# Pots and ordinary domestic clutter establish a lived-in space.
for x,z in [(-3.4,-1.13),(3.4,3.45),(2.98,-2.5)]:
    art.tube('CeramicPlantPot',pos((x,.03,z)),pos((x,.35,z)),.14,'terracotta',radius_end=.21)
    for i in range(7):
        angle=i*math.tau/7
        rod('BasilStem',(x,.33,z),(x+math.cos(angle)*.19,.65+i%3*.06,z+math.sin(angle)*.18),.013,'olive')
        ball('BasilLeaf',(x+math.cos(angle)*.19,.61+i%3*.06,z+math.sin(angle)*.18),(.20,.07,.10),'leaf')
anchor('Anchor_Start',(.5,0,-1.65)); anchor('Anchor_Efe',(-.4,0,-.4)); anchor('Anchor_Derya',(-1.65,0,2.55)); anchor('Anchor_Emre',(2.1,0,-.45))
anchor('Anchor_Exit',(3.1,0,-3.35)); anchor('Anchor_Camera',(5.5,6,-8)); anchor('Anchor_CameraLook',(0,.85,0))
anchor('Anchor_Food',(2.55,.84,2.59)); anchor('Anchor_Water',(2.45,.84,2.30)); anchor('Anchor_Aid',(-3.2,1.18,3.05))
anchor('Anchor_Blanket',(-1.77,.69,-.1)); anchor('Anchor_Card',(-1.80,.67,-2.55)); anchor('Anchor_Comfort',(-.7,1.01,.75))
anchor('Anchor_Whistle',(-1.76,.70,-2.25)); anchor('Anchor_ExitBox',(2.7,.04,-2.72))

art.export('HomeRoom','Environment')
print('PHYSICAL ROOM EXPORTED',flush=True)
