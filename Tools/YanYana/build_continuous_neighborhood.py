"""Original metre-scaled circulation, stairs and neighborhood ground."""
import importlib.util,pathlib,math,bpy
spec=importlib.util.spec_from_file_location('yy',pathlib.Path(__file__).with_name('build_original_art.py'));a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a);a.reset()
def p(v):return(-v[0],-v[2],v[1])
def d(v):return(v[0],v[2],v[1])
def box(n,v,s,m,b=.04):return a.box(n,p(v),d(s),m,b)
def marker(n,v,s=None):
 o=bpy.data.objects.new(n,None);o.location=p(v);o.scale=d(s or (1,1,1));bpy.context.collection.objects.link(o)
def floor(n,v,s,m='cream',visible=None):
 # Keep the proven walking collision volume; visible surfaces meet edge to edge.
 draw_v,draw_s=visible or (v,s)
 box(n,draw_v,draw_s,m);marker('COLLIDER_'+n,v,s)
box('MahalleToprakTabani',(0,-.80,-10),(55,.25,48),'sand',.3)
box('ApartmanTemeli',(0,-.37,0),(7.8,.62,7.8),'terracotta',.06)
floor('Sahanlik',(-3.1,-.1,-4.62),(1.9,.2,1.5),visible=((-3.1,-.1,-4.64),(1.9,.2,1.44)))
for i in range(3):floor('Basamak'+str(i),(-3.1,-.16*(i+1)-.08,-5.62-i*.38),(1.9,.16,.42),'stone')
floor('Avlu',(-3.1,-.60,-8.2),(7.6,.24,3.8),'stone',visible=((-3.1,-.60,-8.045),(7.6,.24,2.91)))
floor('MahalleZemini',(-2,-.64,-19),(29,.32,19),'stone')
floor('AnaYol',(-4,-.469,-16),(4.8,.025,14),'cream',visible=((-4,-.469,-14.6),(4.8,.025,11.2)))
floor('YanYol',(6,-.467,-17),(3.6,.025,15),'cream',visible=((6,-.467,-14.85),(3.6,.025,10.7)))
box('YanYolDevam',(6,-.467,-24.15),(3.6,.025,.70),'cream')
floor('Baglanti',(.5,-.465,-22),(14,.025,3.6),'cream')
for side in (-1,1):
 for z in (-4.1,-4.8,-5.45,-6.1,-6.65):
  y=0 if z>-5.45 else -.16*(1+int((-z-5.45)/.38))
  a.tube('Korkuluk',p((-3.1+side*.91,y+.1,z)),p((-3.1+side*.91,y+.88,z)),.035,'tealDark')
 a.tube('KorkulukTutma',p((-3.1+side*.91,.87,-4.05)),p((-3.1+side*.91,.4,-6.7)),.047,'teal')
# Street trees and planters are authored as a coherent path, leaving clear passage.
for x,z in [(-9,-12),(11,-13),(-10,-22),(10,-26),(2,-25)]:
 box('AgacYatagi',(x,-.38,z),(1.55,.24,1.55),'terracotta',.12)
 a.tube('AgacGovde',p((x,-.3,z)),p((x,2.0,z)),.13,'wood')
 for dx,dz,dy,r in [(-.4,0,1.9,.8),(.35,0,2.2,.8),(0,.35,2.7,.75)]:a.sphere('YuvarlakTac',p((x+dx,dy,z+dz)),(r,r,r),'leafLight' if dx>0 else 'leaf')
for x,z in [(-7,-11),(1,-15),(10,-21),(-11,-25)]:
 a.tube('SokakLambasi',p((x,-.4,z)),p((x,2.6,z)),.045,'tealDark');box('LambaBasligi',(x,2.63,z),(.42,.12,.42),'mustard',.05)
# Two recognizable meeting areas, with the same illustrated tree landmark as the family map.
for x,z in [(-7,-26),(8,-25)]:
 box('ToplanmaZemini',(x,-.455,z),(6,.03,4),'teal',.15)
 for dx in (-1,1):
  a.tube('AlanIsareti',p((x+dx*2.6,-.44,z+1.6)),p((x+dx*2.6,1.45,z+1.6)),.035,'cream')
  box('ResimliTabela',(x+dx*2.6,1.3,z+1.6),(.70,.55,.06),'tealDark')
for n,v in {'StairsStart':(-3.1,0,-4.3),'StairsEnd':(-3.1,-.48,-7.5),'Yusuf':(-1.25,-.48,-8.1),'FacadeReport':(-5.8,-.48,-11.5),'Idil':(-4,-.48,-15),'FireFocus':(-4,-.48,-18.3),'Aid':(3,-.48,-22),'PrimaryMeeting':(-7,-.48,-26),'AlternateMeeting':(8,-.48,-25)}.items():marker('Street_'+n,v)
a.export('NeighborhoodGround','Environment');print('NEIGHBORHOOD EXPORTED',flush=True)
