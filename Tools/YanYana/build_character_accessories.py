"""Original metre-scale wardrobe details; approved character surfaces remain the base."""
import importlib.util, pathlib, math, bpy
here=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy_art',here/'build_original_art.py')
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
def p(v):return(-v[0],-v[2],v[1])
def line(n,points,r,m):return a.curve(n,[p(v) for v in points],r,m)
def box(n,pos,size,m,b=.01):return a.box(n,p(pos),(size[0],size[2],size[1]),m,b)
def ell(n,pos,size,m):return a.sphere(n,p(pos),(size[0],size[2],size[1]),m)
def out(n):a.export(n,'Props')

a.reset()
for side in (-1,1):
    line('RoundSpectacleRim',[(side*.068+math.cos(t*math.tau/24)*.057, math.sin(t*math.tau/24)*.047,0) for t in range(25)],.004,'brown')
    line('SpectacleTemple',[(side*.125,0,0),(side*.15,.004,-.055),(side*.159,-.016,-.14)],.0035,'brown')
line('SoftNoseBridge',[(-.012,.004,0),(0,.014,.007),(.012,.004,0)],.0035,'metal')
out('YusufRoundGlasses')

a.reset()
for side in (-1,1):
    lobe=ell('SoftSilverMoustache',(side*.029,-.003,0),(.036,.013,.013),'silver')
    lobe.rotation_euler[1]=side*math.radians(11)
    for i in range(3):line('SmallHairGroove',[(side*(.007+i*.006),.004,.012),(side*(.032+i*.006),-.006,.011)],.0012,'cream')
out('YusufMoustache')

a.reset()
for y in (-.095,0,.095):ell('HornCardiganButton',(0,y,0),(.008,.008,.004),'woodDark')
out('CardiganButtons')

a.reset()
line('KnittedScarfCollar',[(-.115,.032,-.01),(-.13,.018,-.12),(0,0,-.15),(.13,.018,-.12),(.115,.032,-.01)],.028,'coral')
box('ScarfFold',(.078,-.065,.005),(.06,.18,.028),'coral',.019)
for i in range(4):line('ScarfFringe',[(.053+i*.015,-.15,.005),(.053+i*.015,-.178,.005)],.003,'cream')
out('NeighborCoralScarf')

a.reset()
# A soft brim and six crown panels give a recognisable silhouette without covering the face.
ell('SoftHatCrown',(0,.035,-.015),(.176,.075,.17),'mustard')
ell('HatBrim',(0,-.019,0),(.212,.013,.206),'mustard')
line('HatRibbon',[(math.cos(i*math.tau/32)*.168,-.006,math.sin(i*math.tau/32)*.166-.015) for i in range(33)],.010,'tealDark')
out('NeighborSunHat')

a.reset()
box('CrossbodyBag',(0,0,0),(.22,.23,.075),'teal',.027)
box('FoldedBagFlap',(0,.053,.041),(.205,.12,.02),'tealDark',.013)
ell('BagClasp',(0,.008,.055),(.012,.009,.005),'mustard')
line('WovenBagStrap',[(-.083,.11,.045),(-.42,.50,.07),(-.43,.55,-.16),(-.30,.45,-.36),(.096,.11,-.06)],.014,'cream')
out('NeighborCrossbodyBag')

a.reset()
line('HairBand',[(math.cos(i*math.pi/24)*.18,math.sin(i*math.pi/24)*.115,0) for i in range(25)],.009,'teal')
for side in (-1,1):ell('FabricBow',(side*.021,.105,.011),(.025,.016,.012),'coral')
ell('BowKnot',(0,.105,.023),(.010,.012,.008),'mustard')
out('NeighborHairBand')

a.reset()
box('ShirtPocket',(0,0,0),(.10,.105,.012),'cream',.009)
line('PocketSeam',[(-.043,.043,.01),(-.043,-.036,.01),(0,-.049,.01),(.043,-.036,.01),(.043,.043,.01)],.002,'olive')
for x in (-.019,.008):line('Pencil',[(x,.015,.018),(x,.085,.018)],.0035,'mustard' if x<0 else 'coral')
out('NeighborPocket')
print('EIGHT ORIGINAL WARDROBE MODELS EXPORTED',flush=True)
