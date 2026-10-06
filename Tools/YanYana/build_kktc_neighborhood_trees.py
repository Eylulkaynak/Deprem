"""Original, fully three-dimensional olive/carob park trees. Editor assets only."""
import importlib.util
import math
import pathlib
import sys
import bpy

spec = importlib.util.spec_from_file_location('original', pathlib.Path(__file__).with_name('build_original_art.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
if '--staging' in args:
    folder = a.ROOT / '.codex_tmp/neighborhood_design_20261005'
    a.SOURCE = folder / 'Source'; a.MODELS = folder / 'Models'
    (a.SOURCE / 'Environment').mkdir(parents=True, exist_ok=True); a.MODELS.mkdir(parents=True, exist_ok=True)
a.PALETTE.update(oliveDark='#64806A', oliveMid='#7B916E', oliveLight='#8FA47A', oliveBark='#806347')

for name, broad in [('KKTC_OliveTree', False), ('KKTC_CarobTree', True)]:
    a.reset()
    a.tube('GroundedTrunk', (0, 0, .02), (.08, .025, 2.47), .145, 'oliveBark', radius_end=.09, vertices=16)
    for i in range(7):
        angle=i*2.4
        x,y=math.cos(angle),math.sin(angle)
        a.tube('GrowingBranch', (.03, 0, 1.65 + (i%3)*.19), (x*.73, y*.67, 2.87 + (i%2)*.22),
               .061, 'oliveBark', radius_end=.026, vertices=12)
    crown=[(-.68,-.25,3.22,.85,.79,.74),(.60,-.18,3.43,.93,.82,.82),
           (-.12,.67,3.44,.81,.80,.75),(-.32,-.70,3.59,.76,.80,.79),
           (.34,.39,3.91,.85,.79,.79),(-.27,-.13,4.15,.79,.83,.71),
           (-.85,.37,3.77,.65,.65,.66),(.86,.39,3.75,.67,.63,.68)]
    for i,(x,y,z,sx,sy,sz) in enumerate(crown):
        horizontal=1.07 if broad else 1.0
        a.sphere('SolidLeafMass', (x*horizontal,y*horizontal,z-.17 if broad else z),
                 (sx*horizontal,sy*horizontal,sz*.94), ['oliveDark','oliveMid','oliveLight'][i%3],segments=16,rings=12)
    groups={}
    for o in list(bpy.context.scene.objects):
        if o.type=='MESH': groups.setdefault(o.data.materials[0].name,[]).append(o)
    for material,parts in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in parts:o.select_set(True)
        bpy.context.view_layer.objects.active=parts[0]
        bpy.ops.object.convert(target='MESH');bpy.ops.object.join();bpy.context.object.name='TreeVolume_'+material
    a.export(name, 'Environment')
    print('CLOSED TREE EXPORTED',name,flush=True)
