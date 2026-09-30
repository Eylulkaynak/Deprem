"""Own editable flat backpack inspection models; Unity authors their moving hardware."""
import importlib.util, pathlib, bpy

here=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy_art',here/'build_original_art.py')
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
def pos(p):return (p[0],-p[2],p[1])
def box(n,p,s,m,b=.025):return art.box(n,pos(p),(s[0],s[2],s[1]),m,b)
def curve(n,points,r,m):return art.curve(n,[pos(p) for p in points],r,m)

for back in (False,True):
    art.reset()
    box('PaddedFabric',(0,.085,0),(.47,.16,.67),'tealDark',.07)
    box('SoftBagBody',(0,.13,0),(.445,.105,.64),'teal',.06)
    curve('CarryHandle',[(-.07,.08,.31),(-.07,.08,.39),(.07,.08,.39),(.07,.08,.31)],.014,'coral')
    if back:
        for side in (-1,1):
            curve('ShoulderWebbing',[(side*.14,.19,-.24),(side*.14,.225,-.07),(side*.14,.225,.13),(side*.14,.19,.27)],.029,'cream')
            box('AdjustmentTail',(side*.14,.23,-.14),(.05,.012,.24),'tealDark',.008)
            for z in (-.02,.045,.11):box('WovenAdjustmentMark',(side*.14,.25,z),(.065,.009,.007),'coral',.003)
    else:
        box('FrontPocket',(0,.21,-.115),(.325,.10,.23),'coral',.045)
        curve('PocketStitch',[(-.14,.265,-.03),(0,.268,-.015),(.14,.265,-.03)],.0035,'cream')
        curve('ZipperTrack',[(-.205,.205,-.16),(-.205,.205,.06),(-.18,.205,.22),(0,.205,.28),(.18,.205,.22),(.205,.205,.06),(.205,.205,-.16)],.009,'cream')
        for side in (-1,1):
            for step in range(9):box('ZipTooth',(side*.205,.218,-.14+step*.04),(.027,.006,.008),'navy',.002)
    art.export('BackpackTrialBack' if back else 'BackpackTrialFront','Props')
print('BACKPACK TRIAL MODELS EXPORTED',flush=True)
