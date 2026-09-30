"""Original editable toys for the ordinary-day carrying lesson. Metre-scale authored assets."""
import importlib.util,pathlib,bpy,math
here=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy_art',here/'build_original_art.py')
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
def pos(p):return(p[0],-p[2],p[1])
def box(n,p,s,m,b=.02):return art.box(n,pos(p),(s[0],s[2],s[1]),m,b)
def curve(n,ps,r,m):return art.curve(n,[pos(p) for p in ps],r,m)

art.reset()
box('LightCardboardBase',(0,-.02,0),(.72,.08,.20),'wood',.035)
for side in (-1,1):
    box('FoldedLongRim',(0,.035,side*.091),(.70,.065,.018),'teal',.012)
    box('FoldedShortRim',(side*.35,.035,0),(.018,.065,.20),'teal',.012)
    for x in (-.14,.14):
        curve('WideCarryLoop',[(x-.035,.025,side*.09),(x-.035,.04,side*.20),(x+.035,.04,side*.20),(x+.035,.025,side*.09)],.009,'cream')
    for z in (-.08,.08):
        curve('EndCarryLoop',[(side*.35,.025,z-.018),(side*.46,.04,z-.018),(side*.46,.04,z+.018),(side*.35,.025,z+.018)],.008,'cream')
for i in range(3):
    box('WoodenToyBridgePiece',(-.22+i*.22,.028,0),(.19,.035,.13),'woodDark' if i==1 else 'wood',.015)
    box('ColouredPieceEnd',(-.22+i*.22,.05,-.032),(.14,.008,.028),'coral' if i%2 else 'mustard',.003)
box('FamilyTreeTrunk',(.255,.065,.025),(.019,.01,.043),'woodDark',.003)
art.sphere('FamilyTreeCrown',pos((.255,.073,.043)),(.064,.058,.016),'teal')
art.export('IntroToyKit','Props')

art.reset()
box('LowToyShelfTop',(0,.54,0),(.86,.055,.26),'wood',.025)
for x in (-.32,.32):
    for z in (-.065,.065):box('RoundedShelfLeg',(x,.255,z),(.055,.51,.055),'tealDark',.015)
box('ShelfLowerBrace',(0,.17,0),(.67,.035,.10),'cream',.012)
art.export('IntroToyStand','Props')

art.reset()
for i in range(3):
    cushion=box('SoftPlayCushion',(0,.11+i*.215,0),(.46,.21,.24),('coral','cream','teal')[i],.065)
    box('WovenCushionSeam',(0,.11+i*.215,-.119),(.34,.013,.010),'cream' if i!=1 else 'coral',.004)
art.export('IntroCushionStack','Props')
print('INTRO PLAY KIT, LOW STAND AND SOFT CUSHIONS EXPORTED',flush=True)
