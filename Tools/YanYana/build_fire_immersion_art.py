"""Author the two-grip nozzle and closed, volumetric flame meshes in Blender."""
import bpy, math, importlib.util
from pathlib import Path

root=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy',root/'build_original_art.py')
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
bpy.context.preferences.filepaths.save_version=0

# Same local frame as the existing model: nozzle axis is Blender +Z.
a.reset()
a.tube('RearCoupling',(0,0,-.08),(0,0,.015),.049,'metal',vertices=24)
a.tube('CouplingRubber',(0,0,-.055),(0,0,-.015),.055,'black',vertices=24)
a.tube('ValveBody',(0,0,.005),(0,0,.15),.055,'tealDark',vertices=24)
a.tube('ForwardBarrel',(0,0,.15),(0,0,.34),.040,'navy',vertices=24)
for z in [.165,.183,.201,.219,.237,.255,.273]:
    a.tube('ForwardGripRib',(0,0,z),(0,0,z+.012),.044,'black',vertices=24)
a.tube('AdjustableGoldCollar',(0,0,.29),(0,0,.345),.058,'mustard',vertices=24)
a.tube('MuzzleShell',(0,0,.345),(0,0,.405),.054,'metal',radius_end=.046,vertices=24)
a.tube('DarkBore',(0,0,.404),(0,0,.410),.034,'black',vertices=24)
a.tube('Outlet',(0,0,.410),(0,0,.414),.018,'metal',vertices=20)
# A rear vertical grip, separate from the left hand's forward barrel support.
a.tube('RearHandGrip',(0,-.025,.04),(0,-.155,.00),.016,'black',vertices=16)
a.tube('GripEnd',(0,-.145,.003),(0,-.160,-.002),.022,'mustard',vertices=16)
a.curve('TriggerGuard',[(0,-.02,.10),(0,-.105,.10),(0,-.14,.02)],.007,'metal')
a.tube('Trigger',(0,-.025,.07),(0,-.075,.06),.007,'mustard')
a.curve('ValveBail',[(-.053,0,.07),(-.065,.10,.06),(0,.125,.06),(.065,.10,.06),(.053,0,.07)],.010,'red')
a.export('HoseNozzle')

a.reset()
def tongue(name, height, width, offset, seed):
    vertices=[];uv=[];faces=[];rings=18;segments=18
    for j in range(rings+1):
        t=j/rings
        # Broad rounded base with a gently twisting tip; every surface is closed.
        r=width*(.68+.55*math.sin(math.pi*t))*((1-t)**.82)
        r=max(.002,r)
        bendx=math.sin(t*2.8+seed)*.12*t*t
        bendy=math.sin(t*4.1+seed)*.055*t*t
        for i in range(segments):
            angle=i/segments*math.tau
            radius=r*(1+.08*math.sin(3*angle+t*4+seed))
            vertices.append((offset[0]+math.cos(angle)*radius+bendx,offset[1]+math.sin(angle)*radius*.78+bendy,offset[2]+t*height))
            uv.append((i/segments+seed,t))
    for j in range(rings):
        for i in range(segments):
            n=j*segments+i;k=j*segments+(i+1)%segments
            faces.append((n,k,k+segments,n+segments))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple(rings*segments+i for i in range(segments)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    uv_layer=mesh.uv_layers.new(name='FlameFlow')
    for poly in mesh.polygons:
        for loop in poly.loop_indices:uv_layer.data[loop].uv=uv[mesh.loops[loop].vertex_index]
    a.finish(obj,name,'mustard')

tongue('FlameBody',.87,.165,(0,0,.0),.1)
tongue('FlameLeft',.58,.108,(-.135,.025,.015),1.7)
tongue('FlameRight',.69,.105,(.125,.01,.015),3.1)
tongue('FlameFront',.40,.104,(.0,-.105,.01),4.3)
a.export('FireVolume')
a.reset()
a.tube('SocketBody',(0,0,-.055),(0,0,.055),.052,'metal',vertices=24)
for z in [-.055,-.043,.032,.044]:a.tube('LockingRing',(0,0,z),(0,0,z+.013),.063,'metal',vertices=24)
a.tube('SocketBore',(0,0,-.056),(0,0,-.058),.037,'black',vertices=24)
for x in [-.064,.064]:a.box('LatchEar',(x,0,-.015),(.021,.045,.06),'mustard',.005)
a.export('CouplingSocket')
a.reset()
a.tube('PlugBody',(0,0,-.065),(0,0,.065),.045,'tealDark',vertices=24)
a.tube('JoiningCollar',(0,0,.032),(0,0,.078),.054,'metal',vertices=24)
a.tube('JoiningBore',(0,0,.078),(0,0,.079),.032,'black',vertices=24)
for x in [-.05,.05]:a.box('BajonetLug',(x,0,.050),(.023,.043,.022),'mustard',.004)
a.curve('FlexibleHose',[(0,0,-.062),(-.018,.006,-.19),(-.085,.022,-.30),(-.20,.022,-.40)],.031,'tealDark')
a.export('CouplingPlug')
a.reset()
def point(v):return(-v[0],-v[2],v[1])
a.box('PumpCase',point((.10,-.06,.25)),(.42,.29,.12),'tealDark',.026)
a.box('TopPlate',point((.10,.008,.25)),(.38,.25,.018),'metal',.008)
for x in [-.07,.27]:
    for z in [.16,.34]:a.sphere('PlateBolt',point((x,.023,z)),(.01,.01,.006),'navy',segments=12,rings=8)
a.box('SafetyMark',point((-.055,.020,.27)),(.085,.08,.004),'mustard',.005)
a.export('CouplingStation')
a.reset()
a.curve('Wheel',[((math.cos(i/32*math.tau))*.094,(math.sin(i/32*math.tau))*.094,.01) for i in range(33)],.011,'coral')
for i in range(3):
    angle=i*math.tau/3;a.tube('Spoke',(0,0,.01),(math.cos(angle)*.087,math.sin(angle)*.087,.01),.009,'coral',vertices=12)
a.tube('WheelCenter',(0,0,-.035),(0,0,.022),.024,'metal',vertices=16)
a.export('CouplingWheel')
print('Authored nozzle, volumetric fire and coupling props with editable .blend sources.',flush=True)
