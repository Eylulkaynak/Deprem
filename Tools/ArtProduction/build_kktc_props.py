"""Author the KKTC story prop collection in Blender. No game runtime generation.

Run with Blender --background --python this_file -- --project PROJECT.
Sources, FBX models, material palette and review renders are reproducible.
All dimensions are metres; Blender Z is up, -Y is the presentation front.
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

args = argparse.ArgumentParser()
args.add_argument('--project', required=True)
args.add_argument('--only', default='')
opt = args.parse_args(sys.argv[sys.argv.index('--') + 1:])
project = Path(opt.project)
out = project / 'Assets/Story/Art/KKTC/Models'
source = project / 'ArtDirection/KKTC/Props'
reviews = project / 'ClientExports/KKTC/Props'
for directory in (out, source, reviews):
    directory.mkdir(parents=True, exist_ok=True)

PALETTE = {
    'CanvasTeal': ('347E7F', .77, 0), 'CanvasDeep': ('235456', .82, 0),
    'Lining': ('233B45', .92, 0), 'Webbing': ('D7B987', .88, 0),
    'Stitch': ('E6D9BE', .83, 0), 'Rubber': ('26353C', .76, 0),
    'WarmWhite': ('F3EAD8', .63, 0), 'Terracotta': ('B76749', .8, 0),
    'Ochre': ('DDAA53', .65, 0), 'Metal': ('B0B9B8', .3, .7),
    'Brass': ('C4A36A', .35, .65), 'Lens': ('A6D9D7', .17, .2),
    'Paper': ('EEE0BA', .85, 0), 'BlueInk': ('274657', .75, 0),
    'Water': ('8FC6CE', .3, .08), 'Willow': ('B89461', .82, 0),
    'WillowLight': ('D8B57D', .85, 0), 'Olive': ('657750', .8, 0),
    'Wood': ('875B3C', .74, 0), 'Ceramic': ('ECE2CC', .24, 0),
    'Coffee': ('32281F', .5, 0), 'SkinTape': ('DBB88F', .9, 0),
}
materials = {}
active = []


def material(name):
    if name in materials:
        return materials[name]
    color, rough, metal = PALETTE[name]
    srgb = tuple(int(color[i:i+2], 16) / 255 for i in (0, 2, 4))
    rgb = tuple(v / 12.92 if v <= .04045 else ((v+.055)/1.055)**2.4 for v in srgb)
    mat = bpy.data.materials.get('KKTC_' + name) or bpy.data.materials.new('KKTC_' + name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    if name.startswith('Canvas') or name == 'Lining':
        noise = mat.node_tree.nodes.new('ShaderNodeTexNoise')
        noise.inputs['Scale'].default_value = 250
        bump = mat.node_tree.nodes.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .12
        bump.inputs['Distance'].default_value = .0008
        mat.node_tree.links.new(noise.outputs['Fac'], bump.inputs['Height'])
        mat.node_tree.links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    materials[name] = mat
    return mat


def finish(obj, name, mat=None):
    obj.name = name
    if mat:
        obj.data.materials.append(material(mat))
    if obj.type == 'MESH':
        for face in obj.data.polygons:
            face.use_smooth = True
    active.append(obj)
    return obj


def box(name, pos, size, mat, radius=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = finish(bpy.context.object, name, mat)
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if radius:
        bevel = obj.modifiers.new('Soft manufactured edges', 'BEVEL')
        bevel.width = min(radius, min(size) * .44)
        bevel.segments = 3
        weighted = obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
        weighted.keep_sharp = True
    return obj


def cylinder(name, pos, radius, depth, mat, axis='Z', vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=pos)
    obj = finish(bpy.context.object, name, mat)
    if axis == 'X':
        obj.rotation_euler.y = math.pi / 2
    elif axis == 'Y':
        obj.rotation_euler.x = math.pi / 2
    bevel = obj.modifiers.new('Edge bevel', 'BEVEL')
    bevel.width = min(radius * .12, depth * .15, .0025)
    bevel.segments = 2
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def sphere(name, pos, size, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=pos)
    obj = finish(bpy.context.object, name, mat)
    obj.scale = size
    return obj


def tube(name, points, radius, mat, resolution=3):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 8
    curve.bevel_depth = radius
    curve.bevel_resolution = resolution
    curve.use_fill_caps = True
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points)-1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat)


def text(name, value, pos, size, mat, rotation=(math.pi/2, 0, 0)):
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body, curve.align_x, curve.align_y = value, 'CENTER', 'CENTER'
    curve.size, curve.extrude, curve.resolution_u = size, .00015, 3
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.location, obj.rotation_euler = pos, rotation
    return finish(obj, name, mat)


def anchor(name, pos, rotation=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location, obj.rotation_euler = pos, rotation
    active.append(obj)
    return obj


def ellipse(width, depth, z, n=64, exponent=3):
    result = []
    for i in range(n):
        a = 2 * math.pi * i / n
        c, s = math.cos(a), math.sin(a)
        result.append((width * math.copysign(abs(c)**(2/exponent), c),
                       depth * math.copysign(abs(s)**(2/exponent), s), z))
    return result


def loft(name, rings, mat, cap_bottom=True, cap_top=False, reverse=False):
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = []
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
    if cap_bottom:
        faces.append(tuple(reversed(range(n))))
    if cap_top:
        faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    if reverse:
        faces = [tuple(reversed(face)) for face in faces]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat)


def seam_ring(name, width, depth, z, mat='Stitch', radius=.0008):
    pts = ellipse(width, depth, z, 40)
    return tube(name, pts + [pts[0]], radius, mat, 1)


def ribbon(name, points, width, thickness, mat):
    # Subdivide the authored path with a Catmull-Rom curve; padded fabric bends
    # continuously instead of forming rigid polygon corners.
    if len(points)>3 and name in {'PaddedShoulderStrap','StrapWebbing'}:
        source=[Vector(p) for p in points]
        sampled=[]
        for i in range(len(source)-1):
            a,b,c,d=source[max(0,i-1)],source[i],source[i+1],source[min(len(source)-1,i+2)]
            for step in range(6):
                t=step/6
                sampled.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
        points=sampled+[source[-1]]
    rings = []
    for x, y, z in points:
        rings.append([(x-width/2,y-thickness/2,z), (x+width/2,y-thickness/2,z),
                      (x+width/2,y+thickness/2,z), (x-width/2,y+thickness/2,z)])
    return loft(name, rings, mat, True, True)


def backpack(opened=False):
    loft('CanvasBody', [ellipse(w,d,z) for z,w,d in [(0,.135,.062),(.014,.155,.076),
          (.055,.17,.092),(.24,.165,.083),(.34,.153,.071),(.375,.143,.065)]], 'CanvasTeal')
    loft('PaddedInterior', [ellipse(w,d,z) for z,w,d in [(.025,.13,.058),(.08,.157,.077),
          (.33,.143,.061),(.375,.135,.057)]], 'Lining', True, False, True)
    loft('BoundMouthEdge', [ellipse(.143,.065,.375), ellipse(.135,.057,.376)], 'CanvasDeep', False)
    box('ReinforcedBase', (0,0,.018), (.3,.15,.037), 'CanvasDeep', .015)
    box('FrontPocket', (0,-.088,.115), (.262,.044,.175), 'CanvasDeep', .018)
    tube('PocketZip', [(-.118,-.11,.188),(0,-.114,.198),(.118,-.11,.188)], .0022, 'Rubber')
    box('PocketPull',(.09,-.117,.181),(.012,.004,.025),'Brass',.002)
    box('WovenNamePatch',(0,-.113,.119),(.078,.004,.034),'Webbing',.004)
    text('EmergencyMark','HAZIR',(0,-.116,.119),.014,'BlueInk')
    for side in [-1,1]:
        box('SidePocket', (side*.158,.005,.105), (.044,.11,.145), 'CanvasDeep', .02)
        tube('SidePocketHem',[(side*.169,-.038,.17),(side*.184,0,.18),(side*.169,.042,.17)],.002,'Webbing')
        pts=[(side*.082,.062,.349),(side*.095,.14,.437),(side*.095,.21,.355),
             (side*.11,.215,.22),(side*.135,.155,.105),(side*.139,.071,.06)]
        ribbon('PaddedShoulderStrap',pts,.034,.009,'CanvasDeep')
        ribbon('StrapWebbing',[(x,y+.005,z) for x,y,z in pts],.018,.002,'Webbing')
        box('StrapAdjuster',(side*.132,.164,.118),(.026,.01,.024),'Rubber',.002)
        tube('FrontPanelStitch',[(side*.145,-.075,.035),(side*.148,-.086,.23),(side*.135,-.068,.347)],.0008,'Stitch',1)
    tube('CarryHandle',[(-.058,.058,.354),(-.047,.074,.422),(0,.078,.435),(.047,.074,.422),(.058,.058,.354)],.008,'CanvasDeep')
    seam_ring('MainZipperTeeth',.145,.067,.354,'Brass',.0014)
    seam_ring('BaseStitch',.157,.078,.046)
    if not opened:
        loft('Lid',[ellipse(.143,.065,.372),ellipse(.138,.061,.393),ellipse(.111,.048,.411),
                         ellipse(.01,.006,.423)],'CanvasTeal',False,True)
        tube('LidStitch',[(-.114,-.045,.4),(0,-.052,.416),(.114,-.045,.4)],.0008,'Stitch')
    else:
        lid=box('FoldedBackLid',(0,.093,.402),(.262,.018,.13),'CanvasTeal',.015)
        lid.rotation_euler.x=math.radians(-28)
    anchor('MouthAnchor',(0,0,.365))
    anchor('InsideAnchor',(0,0,.14))
    anchor('BackCarryAnchor',(0,.105,.23))
    anchor('HandGripAnchor',(0,.078,.43))
    anchor('OuterPocketAnchor',(0,-.12,.175))


def flashlight():
    cylinder('AluminiumBody',(0,0,.039),.026,.185,'CanvasDeep','X')
    for i in range(9):
        cylinder('GripRib',(-.065+i*.012,0,.039),.028,.003,'Rubber','X')
    cylinder('BatteryCap',(-.101,0,.039),.028,.022,'Metal','X')
    cylinder('HeadHousing',(.102,0,.039),.041,.045,'CanvasTeal','X')
    cylinder('ReflectorRim',(.127,0,.039),.036,.008,'Metal','X')
    cylinder('Optic',(.132,0,.039),.03,.003,'Lens','X')
    cylinder('LEDCenter',(.134,0,.039),.008,.001,'WarmWhite','X')
    box('PowerSwitch',(0,-.001,.07),(.025,.023,.008),'Ochre',.006)
    text('PowerSymbol','I',(0,-.001,.0748),.012,'BlueInk',(0,0,0))
    tube('WristLoop',[(-.108,0,.039),(-.143,-.013,.02),(-.17,.005,.009),(-.14,.025,.02),(-.108,0,.04)],.002,'Webbing')
    anchor('SwitchAnchor',(0,0,.076))
    anchor('BeamAnchor',(.137,0,.039),(0,math.pi/2,0))
    anchor('HandGripAnchor',(-.02,0,.039))


def radio():
    # The right wall is deliberately open: the cavity is modelled, not a decal.
    box('MainBody',(-.025,0,.083),(.155,.073,.164),'CanvasTeal',.012)
    box('RearPanel',(.066,.032,.083),(.027,.012,.15),'CanvasDeep',.004)
    box('FrontPanel',(.066,-.032,.083),(.027,.012,.15),'CanvasTeal',.004)
    box('BayRoof',(.076,0,.132),(.046,.072,.064),'CanvasTeal',.007)
    box('BayFloor',(.076,0,.022),(.046,.072,.037),'CanvasTeal',.007)
    box('BatteryWell',(.055,0,.083),(.005,.052,.103),'Rubber',.003)
    cover=box('BatteryCover',(.114,.049,.072),(.005,.054,.064),'CanvasDeep',.004)
    cover.rotation_euler.z=-math.pi/4
    cylinder('CoverHinge',(.095,.030,.072),.0027,.06,'Metal')
    for y in [-.015,.015]:
        box('LowerContact',(.076,y,.043),(.019,.012,.004),'Metal',.001)
        tube('SpringContact',[(.068,y,.100),(.078,y-.004,.10),(.081,y+.004,.098),(.072,y,.097)],.0007,'Metal',1)
    text('PolarityPlus','+',(.088,-.036,.096),.009,'WarmWhite')
    text('PolarityMinus','-',(.088,-.036,.044),.009,'WarmWhite')
    cylinder('SpeakerInset',(-.041,-.039,.07),.045,.006,'Rubber','Y')
    for i in range(-6,7):
        z=.071+i*.0057
        half=math.sqrt(max(0,.04**2-(i*.0057)**2))
        box('SpeakerGrille',(-.041,-.043,z),(half*2,.003,.002),'CanvasDeep',.0006)
    box('TuningDisplay',(-.016,-.04,.138),(.111,.004,.023),'WarmWhite',.003)
    for i in range(13):
        box('TuningTick',(-.063+i*.007,-.043,.138),(.0007,.001,.007 if i%3 else .013),'BlueInk',0)
    box('TuningNeedle',(-.007,-.044,.14),(.0015,.001,.018),'Terracotta',0)
    cylinder('TuningKnob',(.034,-.046,.074),.017,.012,'Ochre','Y')
    box('PowerSwitch',(.011,0,.173),(.035,.029,.012),'WarmWhite',.004)
    tube('Handle',[(-.065,.019,.154),(-.059,.019,.204),(.025,.019,.204),(.032,.019,.154)],.006,'Rubber')
    tube('Antenna',[(-.081,.016,.169),(-.105,.016,.282)],.002,'Metal')
    text('RadioLabel','RADYO',(-.018,-.042,.026),.011,'WarmWhite')
    anchor('BatterySocket',(.079,0,.044),(0,0,math.pi/2))
    anchor('SwitchAnchor',(.011,0,.18))
    anchor('HandGripAnchor',(-.015,.019,.204))


def batteries():
    for i,x in enumerate([-.009,.009]):
        cylinder('AACell',(x,0,.026),.0073,.049,'Ochre')
        cylinder('CellSleeve',(x,0,.034),.0074,.026,'CanvasDeep')
        cylinder('PositiveButton',(x,0,.052),.0027,.003,'Metal')
        cylinder('NegativeEnd',(x,0,.0014),.0063,.002,'Metal')
        text('CellPolarity','+',(x,-.0075,.043),.009,'WarmWhite')
        text('CellType','AA',(x,-.0075,.03),.007,'WarmWhite')
    box('ReusableCellClip',(0,.002,.012),(.036,.016,.016),'Rubber',.002)
    anchor('InsertAnchor',(0,0,0))


def bottle():
    loft('WaterBottle',[ellipse(r,r,z,48,2) for r,z in [(.032,0),(.037,.008),(.035,.03),
        (.034,.13),(.032,.16),(.018,.187),(.015,.192),(.015,.208)]],'Water',True,True)
    cylinder('PaperLabel',(0,0,.096),.0352,.07,'WarmWhite')
    text('WaterLabel','SU',(0,-.0358,.106),.027,'CanvasDeep')
    text('Volume','500 ml',(0,-.0358,.079),.01,'BlueInk')
    for z in [.025,.037,.146,.157]:
        seam_ring('BottleRib',.035,.035,z,'Water',.0015)
    cylinder('TamperRing',(0,0,.2),.018,.005,'CanvasDeep')
    cylinder('ScrewCap',(0,0,.215),.018,.023,'CanvasTeal')
    for i in range(24):
        a=i*math.tau/24
        box('CapGrip',(.018*math.cos(a),.018*math.sin(a),.215),(.0015,.0015,.018),'CanvasDeep',.0003)
    anchor('DateLabelAnchor',(0,-.036,.065))


def first_aid():
    box('SoftCase',(0,0,.075),(.22,.075,.14),'Terracotta',.025)
    for y in [-.039,.039]:
        tube('CaseSeam',[(-.09,y,.019),(-.102,y,.036),(-.102,y,.112),(-.085,y,.136),
                           (.085,y,.136),(.102,y,.112),(.102,y,.036),(.09,y,.019)],.001,'Stitch',1)
    box('FirstAidHorizontal',(0,-.04,.081),(.065,.004,.022),'WarmWhite',.002)
    box('FirstAidVertical',(0,-.041,.081),(.022,.004,.064),'WarmWhite',.002)
    tube('CaseHandle',[(-.035,0,.14),(-.025,0,.167),(.025,0,.167),(.035,0,.14)],.004,'CanvasDeep')
    box('ZipPull',(.107,0,.096),(.006,.009,.023),'Brass',.002)


def whistle():
    cylinder('WhistleChamber',(0,0,.018),.018,.036,'Ochre','X')
    box('Mouthpiece',(0,-.03,.025),(.026,.049,.015),'Ochre',.006)
    box('MouthOpening',(0,-.055,.025),(.018,.002,.005),'Rubber',.001)
    box('AirWindow',(0,-.008,.036),(.014,.016,.002),'Rubber',.001)
    tube('Lanyard',[(.017,.005,.02),(.04,.03,.006),(0,.06,.003),(-.038,.027,.006),(-.017,.005,.02)],.0018,'CanvasDeep')


def can():
    cylinder('Tin',(0,0,.057),.039,.112,'Metal')
    cylinder('FoodLabel',(0,0,.054),.0395,.094,'Ochre')
    cylinder('CanLid',(0,0,.114),.04,.003,'Metal')
    seam_ring('LidRim',.037,.037,.116,'Metal',.001)
    tube('RingPull',[(-.005,-.012,.118),(-.009,.005,.118),(0,.015,.118),(.009,.005,.118),(.005,-.012,.118)],.0014,'Metal')
    text('FoodName','GIDA',(0,-.0402,.062),.018,'BlueInk')
    text('FoodWeight','200 g',(0,-.0402,.04),.009,'BlueInk')


def envelope():
    box('DocumentEnvelope',(0,0,.004),(.23,.15,.007),'Paper',.002)
    tube('EnvelopeFlap',[(-.112,.07,.008),(0,-.018,.009),(.112,.07,.008)],.0006,'Webbing',1)
    cylinder('Seal',(0,-.02,.009),.011,.001,'CanvasTeal')
    text('DocumentLabel','AILE BELGELERI',(0,-.052,.0083),.012,'BlueInk',(0,0,0))


def blanket():
    for i in range(5):
        obj=box('FoldedWoolLayer',(0,0,.013+i*.012),(.26-i*.004,.18,.024),'CanvasTeal' if i%2==0 else 'CanvasDeep',.01)
    for x in [-.092,.092]:
        box('BlanketStripe',(x,-.001,.075),(.008,.177,.001),'Stitch',.001)
    for i in range(20):
        x=-.12+i*.012
        tube('Fringe',[(x,-.086,.035),(x+.002,-.099,.032),(x-.001,-.105,.028)],.001,'Webbing',1)


def cane():
    tube('CaneShaft',[(0,0,.025),(0,0,.64),(.006,0,.8),(.04,0,.85),(.103,0,.84)],.012,'Wood')
    tube('RubberHandle',[(.036,0,.846),(.074,0,.849),(.108,0,.838)],.015,'Rubber')
    cylinder('RubberFoot',(0,0,.018),.018,.036,'Rubber')
    cylinder('HeightCollar',(0,0,.34),.014,.024,'Brass')


def toy_car():
    box('ToyBody',(0,0,.027),(.12,.06,.034),'Terracotta',.012)
    box('ToyCabin',(-.006,0,.052),(.058,.052,.033),'Ochre',.01)
    for y in [-.027,.027]:
        box('ToyWindow',(-.008,y,.055),(.042,.002,.016),'Lens',.004)
    for x in [-.037,.037]:
        for y in [-.032,.032]:
            cylinder('ToyWheel',(x,y,.018),.018,.013,'Rubber','Y',20)
            cylinder('WheelHub',(x,y*1.18,.018),.008,.002,'Metal','Y',16)
    for y in [-.018,.018]:
        box('Headlight',(.061,y,.032),(.002,.013,.01),'WarmWhite',.003)


def bandage():
    box('SterileWrapper',(0,0,.012),(.12,.083,.021),'WarmWhite',.006)
    box('BandageLabel',(0,0,.023),(.097,.061,.001),'Paper',.003)
    text('SterileText','STERIL',(0,0,.025),.014,'CanvasDeep',(0,0,0))
    for x in [-.054,.054]:
        box('SealedEdge',(x,0,.013),(.006,.078,.022),'CanvasTeal',.002)


def soap():
    box('SoapBar',(0,0,.017),(.09,.056,.032),'Ceramic',.018)
    text('SoapEmboss','SABUN',(0,0,.034),.011,'Webbing',(0,0,0))


def wipes():
    box('WipesPack',(0,0,.026),(.145,.09,.05),'WarmWhite',.015)
    box('ResealLid',(0,0,.052),(.075,.049,.007),'CanvasTeal',.012)
    text('WipesLabel','MENDIL',(0,-.031,.051),.009,'CanvasDeep',(0,0,0))


def basket():
    loft('WovenBowl',[ellipse(w,d,z,64,2) for w,d,z in [(.08,.065,.003),(.106,.079,.028),(.116,.086,.068)]],'Willow',True,False)
    for z in [.014,.023,.032,.041,.05,.059]:
        seam_ring('HorizontalWeave',.083+z*.49,.066+z*.29,z,'WillowLight',.0015)
    for i in range(40):
        a=i*math.tau/40
        tube('UprightWeave',[(r*math.cos(a),r*.74*math.sin(a),z) for r,z in [(.081,.006),(.097,.033),(.118,.069)]],.0011,'Willow',1)
    seam_ring('BraidedRim',.117,.087,.069,'WillowLight',.003)
    # Three bread rolls make this a used kitchen object.
    for x,y in [(-.038,-.005),(.033,.018),(.013,-.028)]:
        sphere('BreadRoll',(x,y,.04),(.04,.027,.025),'Paper')


def coffee_set():
    box('ServingTray',(0,0,.007),(.27,.17,.012),'Wood',.014)
    for x in [-.068,.068]:
        cylinder('Saucer',(x,-.016,.016),.035,.006,'Ceramic')
        loft('CoffeeCup', [[(a+x,b-.016,c) for a,b,c in ellipse(r,r,z,32,2)] for r,z in [(.018,.019),(.025,.055),(.024,.058)]], 'Ceramic',True,False)
        cylinder('Coffee',(x,-.016,.054),.0215,.001,'Coffee')
        tube('CupHandle',[(x+.022,-.016,.048),(x+.037,-.016,.047),(x+.037,-.016,.029),(x+.021,-.016,.028)],.003,'Ceramic')
    cylinder('Cezve',(0,.043,.045),.026,.062,'Brass')
    tube('CezveHandle',[(.02,.043,.063),(.057,.065,.084),(.09,.082,.098)],.004,'Wood')


def lace():
    box('LinenCloth',(0,0,.001),(.62,.38,.002),'WarmWhite',.001)
    # Geometric drawn-thread embroidery inspired by Lefkara table linens.
    for edge in [-1,1]:
        for i in range(17):
            x=-.27+i*.034
            y=edge*.151
            tube('LefkaraDiamond',[(x-.012,y,.0025),(x,y-.012,.0025),(x+.012,y,.0025),(x,y+.012,.0025),(x-.012,y,.0025)],.0006,'Webbing',1)
    for edge in [-1,1]:
        for i in range(8):
            y=-.117+i*.034
            x=edge*.285
            tube('LefkaraSideDiamond',[(x-.011,y,.0025),(x,y-.011,.0025),(x+.011,y,.0025),(x,y+.011,.0025),(x-.011,y,.0025)],.0006,'Webbing',1)


def plant():
    loft('TerracottaPot',[ellipse(r,r,z,40,2) for r,z in [(.065,0),(.085,.11),(.091,.115),(.091,.13),(.08,.133)]],'Terracotta',True,False)
    cylinder('Soil',(0,0,.122),.078,.009,'Wood')
    for i in range(9):
        a=i*2.399
        x,y=.042*math.cos(a),.042*math.sin(a)
        z=.20+(i%4)*.025
        tube('Stem',[(0,0,.12),(x*.5,y*.5,z*.7),(x,y,z)],.002,'Olive',1)
        for side in [-1,1]:
            leaf=sphere('BasilLeaf',(x+side*.018,y,z-.005),(.025,.012,.006),'Olive')
            leaf.rotation_euler=(.2*side,.4*side,a)


def clothes():
    box('FoldedTrousers',(0,0,.016),(.22,.17,.032),'CanvasDeep',.013)
    box('FoldedTShirt',(0,-.005,.042),(.22,.165,.035),'Ochre',.015)
    tube('ShirtCollar',[(-.032,.073,.058),(0,.044,.061),(.032,.073,.058)],.003,'Webbing')
    for x in [-.095,.095]:
        tube('ShirtSeam',[(x,-.074,.057),(x,.02,.057),(x*.8,.07,.057)],.0006,'Stitch',1)


def console():
    box('HandheldConsole',(0,0,.019),(.18,.091,.038),'CanvasDeep',.022)
    box('ScreenBezel',(0,0,.039),(.10,.074,.006),'Rubber',.008)
    box('Screen',(0,0,.043),(.088,.060,.002),'Lens',.006)
    for x in [-.068,.068]:
        cylinder('ThumbStick',(x,.017,.042),.011,.01,'Rubber')
    for x,y in [(.068,-.02),(.057,-.03),(.079,-.03)]:
        cylinder('ConsoleButton',(x,y,.040),.004,.007,'Ochre',vertices=16)
    box('DPadHorizontal',(-.068,-.022,.042),(.022,.007,.005),'WarmWhite',.002)
    box('DPadVertical',(-.068,-.022,.042),(.007,.022,.005),'WarmWhite',.002)


def parcel():
    box('CorrugatedCarton',(0,0,.105),(.25,.22,.21),'Willow',.01)
    box('PackingTape',(0,0,.211),(.035,.214,.002),'Webbing',.001)
    tube('LidFold',[(-.12,0,.211),(.12,0,.211)],.0006,'Wood',1)
    box('AddressLabel',(.065,-.04,.213),(.066,.078,.001),'Paper',.001)
    text('ParcelLabel','EV',( .065,-.04,.215),.02,'BlueInk',(0,0,0))


def shoes():
    for x in [-.065,.065]:
        loft('RubberSole', [[(a+x,b,c) for a,b,c in ellipse(.05,.108,z,40,3)] for z in [.006,.025]],'WarmWhite',True,True)
        loft('ShoeUpper', [[(a+x,b,c) for a,b,c in ellipse(w,d,z,40,3)] for w,d,z in [(.048,.103,.025),(.045,.088,.06),(.024,.045,.098)]],'CanvasDeep',False,True)
        for j in range(5):
            y=-.029+j*.012
            tube('CottonLace',[(x-.025,y,.074),(x+.025,y+.005,.074)],.0014,'WarmWhite',1)
        tube('HeelTab',[(x,.077,.074),(x,.086,.098),(x,.06,.089)],.003,'Ochre')


def bracket():
    box('SteelBracketBack',(0,.027,.04),(.069,.005,.08),'Metal',.002)
    box('SteelBracketFoot',(0,0,.003),(.069,.06,.006),'Metal',.002)
    for x in [-.021,.021]:
        cylinder('ScrewSeat',(x,.023,.056),.006,.002,'Rubber','Y',20)
        cylinder('Screw',(x,.022,.056),.004,.003,'Metal','Y',20)
        box('ScrewSlot',(x,.020,.056),(.006,.001,.001),'Rubber',0)
    tube('BracketFold',[(-.033,.023,.008),(.033,.023,.008)],.0014,'Metal')


def safety_strap():
    ribbon('SafetyWebbing',[(-.14,0,.005),(-.07,.012,.006),(0,.025,.008),(.09,.018,.006),(.14,0,.005)],.037,.004,'CanvasDeep')
    for x in [-.14,.14]:
        box('AnchorPlate',(x,0,.007),(.042,.049,.008),'Metal',.004)
        cylinder('AnchorScrew',(x,0,.013),.005,.004,'Metal',vertices=20)
    box('StrapBuckle',(.042,.018,.012),(.045,.032,.013),'Ochre',.004)
    box('BuckleCenter',(.042,.017,.019),(.024,.026,.002),'Rubber',.002)


def book_stack():
    for i,(color,yaw) in enumerate([('CanvasDeep',-.03),('Terracotta',.04),('Ochre',0)]):
        z=.006+i*.022
        box('BookPages',(0,0,z+.008),(.151,.109,.016),'Paper',.001)
        for cover_z in [z,z+.018]:
            b=box('BookCover',(0,0,cover_z),(.16,.12,.003),color,.002)
            b.rotation_euler.z=yaw
        box('BookSpine',(-.078,0,z+.008),(.005,.12,.021),color,.002)


def vase():
    loft('CeramicVase',[ellipse(r,r,z,48,2) for r,z in [(.055,.0),(.076,.036),(.078,.095),(.049,.16),(.041,.20),(.044,.205),(.037,.207)]],'Ceramic',True,False)
    for z in [.07,.086,.102]:
        seam_ring('PaintedVaseBand',.078,.078,z,'CanvasTeal',.001)
    for i in range(4):
        a=i*2.399
        tube('OliveBranch',[(0,0,.14),(.025*math.cos(a),.025*math.sin(a),.25),(.065*math.cos(a),.065*math.sin(a),.33)],.002,'Wood',1)
        for j in range(3):
            sphere('OliveLeaf',(.025*math.cos(a)+j*.014,.025*math.sin(a),.24+j*.025),(.021,.006,.004),'Olive')


def glass_bottle():
    loft('GlassBottle',[ellipse(r,r,z,48,2) for r,z in [(.033,0),(.039,.016),(.039,.19),(.018,.225),(.017,.285)]],'Olive',True,True)
    cylinder('BottleCap',(0,0,.289),.019,.019,'Brass')
    cylinder('BottlePaperLabel',(0,0,.12),.0395,.07,'Paper')
    text('OilLabel','ZEYTIN',(0,-.040,.123),.014,'CanvasDeep')


def pan():
    loft('PanBowl',[ellipse(r,r,z,64,2) for r,z in [(.085,.006),(.11,.032),(.115,.05),(.109,.05),(.095,.016)]],'Rubber',True,False)
    seam_ring('PanRim',.113,.113,.048,'Metal',.002)
    tube('PanHandle',[(.1,0,.029),(.15,0,.04),(.24,0,.05)],.014,'Wood')


def cup():
    loft('Cup',[ellipse(r,r,z,40,2) for r,z in [(.031,0),(.041,.071),(.043,.09),(.038,.09),(.034,.008)]],'Ceramic',True,False)
    tube('CupHandle',[(.037,0,.075),(.069,0,.075),(.069,0,.032),(.035,0,.026)],.005,'CanvasTeal')


MODELS={'Backpack_Open':lambda:backpack(True),'Backpack_Closed':lambda:backpack(False),
 'Flashlight':flashlight,'Radio':radio,'Batteries':batteries,'WaterBottle':bottle,
 'FirstAid':first_aid,'Whistle':whistle,'FoodCan':can,'Documents':envelope,
 'Blanket':blanket,'WalkingCane':cane,'ToyCar':toy_car,'Bandage':bandage,
 'Soap':soap,'WetWipes':wipes,'BreadBasket':basket,'CoffeeSet':coffee_set,
 'LefkaraCloth':lace,'BasilPot':plant,'Clothes':clothes,'Console':console,
 'Parcel':parcel,'ShoePair':shoes,'SafetyBracket':bracket,'SafetyStrap':safety_strap,
 'BookStack':book_stack,'OliveVase':vase,'GlassBottle':glass_bottle,'Pan':pan,'Cup':cup}


def review(name, objects):
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=24
    scene.cycles.use_denoising=True
    scene.render.resolution_x=960
    scene.render.resolution_y=960
    scene.render.resolution_percentage=100
    scene.world.color=(.3,.3,.3)
    scene.view_settings.view_transform='AgX'
    coords=[o.matrix_world @ Vector(c) for o in objects if o.type=='MESH' for c in o.bound_box]
    lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    center=(lo+hi)*.5
    extent=max(hi-lo)
    bpy.ops.object.camera_add(location=center+Vector((1.05,-1.7,.95))*extent)
    camera=bpy.context.object
    camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'
    camera.data.ortho_scale=extent*1.45
    camera.data.lens=55
    camera.data.clip_start=.001
    scene.camera=camera
    for pos,power,size in [((-.9,-1.5,2),140,1.6),((1.5,-.4,1),90,1.4),((.2,1,1.5),100,1)]:
        bpy.ops.object.light_add(type='AREA',location=center+Vector(pos)*extent)
        light=bpy.context.object
        light.data.energy=power*extent*extent
        light.data.shape='DISK'
        light.data.size=size*extent
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=extent*200,location=(0,0,lo.z-.002))
    ground=bpy.context.object
    ground.data.materials.append(material('WarmWhite'))
    scene.render.filepath=str(reviews/(name+'.png'))
    bpy.ops.render.render(write_still=True)


catalog_path=source/'catalog.json'
catalog=json.loads(catalog_path.read_text(encoding='utf-8')) if opt.only and catalog_path.exists() else []
for name,build in MODELS.items():
    if opt.only and name not in opt.only.split(','):
        continue
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    active.clear()
    build()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in active:
        obj.select_set(True)
        if obj.type in {'CURVE','FONT'}:
            bpy.context.view_layer.objects.active=obj
            bpy.ops.object.convert(target='MESH')
        obj.select_set(False)
    # Apply modelling modifiers once and batch static surfaces by material. Functional
    # controls stay separate objects so Unity can animate them with authored clips.
    bpy.ops.object.select_all(action='DESELECT')
    for obj in list(active):
        if obj.type=='MESH':
            obj.select_set(True)
            bpy.context.view_layer.objects.active=obj
            bpy.ops.object.convert(target='MESH')
            obj.select_set(False)
    groups={}
    separate={'PowerSwitch','BatteryCover','Lid','FoldedBackLid'}
    for obj in active:
        if obj.type=='MESH' and obj.name not in separate:
            key=obj.data.materials[0].name if len(obj.data.materials) else 'Default'
            groups.setdefault(key,[]).append(obj)
    for key,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join()
        objects[0].name='Surface_'+key
    active[:]=list(bpy.context.scene.objects)
    root=bpy.data.objects.new(name,None)
    bpy.context.collection.objects.link(root)
    for obj in active:
        obj.parent=root
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(source/(name+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root]+active:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,
       object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
       bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False,
       bake_anim=False,path_mode='AUTO',mesh_smooth_type='FACE')
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in active if o.type=='MESH')
    catalog=[entry for entry in catalog if entry['id']!=name]
    catalog.append({'id':name,'triangles':triangles,
                    'source':str((source/(name+'.blend')).relative_to(project)),
                    'model':str((out/(name+'.fbx')).relative_to(project))})
    review(name,active)
    print('KKTC_MODEL_READY',name,triangles,flush=True)

(out.parent/'palette.json').write_text(json.dumps(PALETTE,indent=2),encoding='utf-8')
(source/'catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
