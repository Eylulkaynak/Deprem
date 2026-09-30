"""Original editable Yan Yana meshes. Run with Blender --background --python.

No imported character/environment meshes or third-party modelling packs are used.
All forms, rigs, materials and portraits are authored here and saved as .blend/FBX.
"""
import bpy
import math
import json
import pathlib
import random
import sys
from mathutils import Vector, Quaternion

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtDirection/YanYana'
MODELS = ROOT / 'Assets/YanYana/Art/Models'
PORTRAITS = ROOT / 'Assets/YanYana/UI/Portraits'
for folder in (SOURCE/'Characters', SOURCE/'Props', SOURCE/'Environment', MODELS, PORTRAITS):
    folder.mkdir(parents=True, exist_ok=True)
random.seed(20260913)

PALETTE = {
 'skin': '#DBA17B', 'skinLight': '#F1BE96', 'blush': '#D98270', 'hair': '#34231E',
 'hairLight': '#5C3B28', 'silver': '#A8A59F', 'cream': '#F4E7CD', 'white': '#FEFAED',
 'teal': '#318F91', 'tealDark': '#276165', 'coral': '#D76E54', 'navy': '#263B50',
 'mustard': '#EAB64E', 'olive': '#758165', 'sand': '#BB9B73', 'brown': '#604938',
 'black': '#232B2F', 'sole': '#E6DDC9', 'iris': '#71412C', 'pupil': '#171F25',
 'lime': '#D0D960', 'metal': '#9EAAAC', 'wood': '#A5734A', 'woodDark': '#684834',
 'leaf': '#628461', 'leafLight': '#91AA74', 'terracotta': '#BC7658', 'blue': '#78A8B0',
 'red': '#C95543', 'stone': '#E1CBA9', 'water': '#85CAD0', 'glass': '#B7D4D2'
}
MATS = {}

def rgba(value):
    def linear(c): return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4
    return tuple(linear(int(value[i:i+2], 16)/255) for i in (1, 3, 5)) + (1,)

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.materials):
        if not block.users:
            bpy.data.materials.remove(block)
    MATS.clear()

def mat(name):
    if name in MATS:
        return MATS[name]
    material = bpy.data.materials.new('YY_' + name)
    material.diffuse_color = rgba(PALETTE.get(name, name))
    material.use_nodes = True
    principled = material.node_tree.nodes.get('Principled BSDF')
    principled.inputs['Base Color'].default_value = material.diffuse_color
    principled.inputs['Roughness'].default_value = 0.66 if name not in ('iris', 'pupil', 'glass', 'metal') else 0.29
    if name == 'metal':
        principled.inputs['Metallic'].default_value = 0.6
    MATS[name] = material
    return material

def finish(obj, name, material, bone=None):
    obj.name = name
    obj.data.materials.append(mat(material))
    if obj.type == 'MESH':
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    if bone and obj.type == 'MESH':
        group = obj.vertex_groups.new(name=bone)
        group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    return obj

def sphere(name, pos, scale, material, bone=None, segments=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=pos)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bone)

def box(name, pos, size, material, bevel=0.035, bone=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new('Rounded crafted edges', 'BEVEL')
    modifier.width = min(bevel, min(size)*0.42)
    modifier.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.modifiers.new('Weighted soft normals', 'WEIGHTED_NORMAL')
    return finish(obj, name, material, bone)

def tube(name, start, end, radius, material, bone=None, radius_end=None, vertices=20):
    start, end = Vector(start), Vector(end)
    delta = end - start
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius,
        radius2=radius_end if radius_end is not None else radius,
        depth=delta.length, location=(start+end)/2)
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    mod = obj.modifiers.new('Rounded ends', 'BEVEL')
    mod.width = radius * 0.45
    mod.segments = 3
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, name, material, bone)

def curve(name, points, radius, material, bone=None):
    data = bpy.data.curves.new(name, 'CURVE')
    data.dimensions = '3D'
    data.bevel_depth = radius
    data.bevel_resolution = 3
    spline = data.splines.new('BEZIER')
    spline.bezier_points.add(len(points)-1)
    for bp, point in zip(spline.bezier_points, points):
        bp.co = point
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    obj.select_set(False)
    return finish(obj, name, material, bone)

def profile(name, rings, material, bone=None, segments=32):
    vertices, faces = [], []
    for z, x_radius, y_radius in rings:
        for i in range(segments):
            angle = i/segments*math.tau
            vertices.append((math.cos(angle)*x_radius, math.sin(angle)*y_radius, z))
    for row in range(len(rings)-1):
        for i in range(segments):
            a, b = row*segments+i, row*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))), tuple((len(rings)-1)*segments+i for i in range(segments))])
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj=bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, material, bone)

def export(name, category='Props'):
    source = SOURCE/category/(name+'.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type in ('MESH', 'ARMATURE', 'EMPTY'):
            obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'.fbx')), use_selection=True,
        object_types={'MESH','ARMATURE','EMPTY'}, apply_unit_scale=True,
        add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y',
        use_mesh_modifiers=True, path_mode='AUTO')

CAST=[
 dict(name='Ada',height=1.36,child=True,female=True,shirt='cream',pants='teal',shoe='coral',hair='hair',style='overalls'),
 dict(name='Efe',height=1.13,child=True,female=False,shirt='mustard',pants='navy',shoe='teal',hair='hairLight',style='shorts'),
 dict(name='Derya',height=1.70,child=False,female=True,shirt='coral',pants='olive',shoe='cream',hair='hair',style='cardigan'),
 dict(name='Emre',height=1.81,child=False,female=False,shirt='navy',pants='sand',shoe='brown',hair='hair',style='jacket'),
 dict(name='Yusuf',height=1.70,child=False,female=False,shirt='olive',pants='brown',shoe='brown',hair='silver',style='vest'),
 dict(name='Idil',height=1.74,child=False,female=True,shirt='navy',pants='navy',shoe='black',hair='hair',style='firefighter'),
 dict(name='Bora',height=1.80,child=False,female=False,shirt='teal',pants='navy',shoe='black',hair='hair',style='responder'),
]

def character(spec):
    reset()
    h=spec['height']; child=spec['child']; female=spec['female']; name=spec['name']
    head_z=h*(0.827 if child else 0.866)
    hr=h*(0.153 if child else 0.113)
    hip=h*0.445; knee=h*0.247; ankle=h*0.075
    neck=h*(0.665 if child else 0.745)
    shoulder=h*(0.640 if child else 0.703)
    shoulder_x=h*(0.141 if child else 0.146)
    elbow_x=h*0.285; wrist_x=h*0.418; finger_x=h*0.458
    leg_x=h*0.085
    data=bpy.data.armatures.new(name+'_Skeleton')
    rig=bpy.data.objects.new(name+'_Rig', data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(bname, start, end, parent=None):
        b=data.edit_bones.new(bname); b.head=start; b.tail=end
        if parent: b.parent=data.edit_bones[parent]
    bone('Hips',(0,0,hip),(0,0,hip+h*.08))
    bone('Spine',(0,0,hip+h*.08),(0,0,shoulder-h*.075),'Hips')
    bone('Chest',(0,0,shoulder-h*.075),(0,0,neck),'Spine')
    bone('Neck',(0,0,neck),(0,0,head_z-hr*.48),'Chest')
    bone('Head',(0,0,head_z-hr*.48),(0,0,head_z+hr),'Neck')
    for side,s in [('Left',1),('Right',-1)]:
        bone(side+'Shoulder',(0,0,shoulder),(s*shoulder_x,0,shoulder),'Chest')
        bone(side+'UpperArm',(s*shoulder_x,0,shoulder),(s*elbow_x,0,shoulder),side+'Shoulder')
        bone(side+'LowerArm',(s*elbow_x,0,shoulder),(s*wrist_x,0,shoulder),side+'UpperArm')
        bone(side+'Hand',(s*wrist_x,0,shoulder),(s*finger_x,0,shoulder),side+'LowerArm')
        bone(side+'UpperLeg',(s*leg_x,0,hip),(s*leg_x,0,knee),'Hips')
        bone(side+'LowerLeg',(s*leg_x,0,knee),(s*leg_x,0,ankle),side+'UpperLeg')
        bone(side+'Foot',(s*leg_x,0,ankle),(s*leg_x,-h*.10,h*.04),side+'LowerLeg')
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.select_set(False)
    # Rounded continuous torso profiles rather than stacked primitive blocks.
    torso=[(hip-h*.018,h*.14,h*.083),(hip+h*.025,h*.147,h*.09),
           (hip+h*.10,h*.12,h*.087),(shoulder-h*.07,h*.151,h*.095),
           (shoulder-h*.023,h*.16,h*.085),(shoulder+h*.012,h*.13,h*.075),
           (neck,h*.046,h*.045)]
    profile(name+'_TailoredTop',torso,spec['shirt'],'Chest')
    sphere(name+'_Waist',(0,0,hip),(h*.146,h*.092,h*.075),spec['pants'],'Hips')
    tube('Neck',(0,0,neck-h*.01),(0,0,head_z-hr*.45),h*.045,'skinLight','Neck')
    for side,s in [('Left',1),('Right',-1)]:
        pants_end=knee+h*.035 if spec['style']=='shorts' else ankle+h*.017
        tube(side+'_Trouser',(s*leg_x,0,hip),(s*leg_x,0,knee),h*.080,spec['pants'],side+'UpperLeg',h*.065)
        material='skinLight' if spec['style']=='shorts' else spec['pants']
        tube(side+'_Shin',(s*leg_x,0,knee+h*.013),(s*leg_x,0,ankle),h*.055,material,side+'LowerLeg',h*.046)
        if spec['style']=='shorts':
            tube(side+'_ShortHem',(s*leg_x,0,knee+h*.005),(s*leg_x,0,knee+h*.042),h*.068,'navy',side+'UpperLeg')
            tube(side+'_Sock',(s*leg_x,0,ankle-h*.01),(s*leg_x,0,ankle+h*.045),h*.048,'cream',side+'LowerLeg')
        foot=(s*leg_x,-h*.034,h*.044)
        box(side+'_Sole',(foot[0],foot[1]-h*.016,h*.016),(h*.119,h*.199,h*.031),'sole',h*.014,side+'Foot')
        sphere(side+'_Shoe',foot,(h*.064,h*.115,h*.047),spec['shoe'],side+'Foot')
        for i in range(3):
            box(side+'_Lace'+str(i),(s*leg_x,-h*.055+i*h*.013,h*.077),(h*.062,h*.008,h*.006),'cream',h*.002,side+'Foot')
        sleeve_material='cream' if spec['style']=='vest' else ('navy' if spec['style']=='responder' else spec['shirt'])
        tube(side+'_UpperSleeve',(s*shoulder_x,0,shoulder),(s*elbow_x,0,shoulder),h*.058,sleeve_material,side+'UpperArm',h*.043)
        short=child or spec['style']=='responder'
        foremat='skinLight' if short else sleeve_material
        tube(side+'_Forearm',(s*elbow_x,0,shoulder),(s*wrist_x,0,shoulder),h*.043,foremat,side+'LowerArm',h*.028)
        palm_material='black' if spec['style']=='firefighter' else 'skinLight'
        sphere(side+'_Palm',(s*(wrist_x+h*.021),0,shoulder),(h*.036,h*.023,h*.029),palm_material,side+'Hand')
        for i in range(4):
            y=(i-1.5)*h*.012
            tube(side+'_Finger'+str(i),(s*(wrist_x+h*.03),y,shoulder),(s*(finger_x+h*.003),y,shoulder-h*.003),h*.008,palm_material,side+'Hand',h*.006,12)
        sphere(side+'_Thumb',(s*(wrist_x+h*.026),-h*.030,shoulder-h*.006),(h*.020,h*.010,h*.012),palm_material,side+'Hand',16,10)
    # Sculpted head, cheeks, eyelids, irises, hair and fine face contours.
    sphere('Head_SoftSculpt',(0,0,head_z),(hr*.93,hr*.79,hr),'skinLight','Head',40,28)
    sphere('Jaw',(0,-hr*.15,head_z-hr*.42),(hr*.71,hr*.60,hr*.54),'skinLight','Head',32,20)
    for s in (-1,1):
        sphere('Ear',(s*hr*.92,0,head_z-hr*.03),(hr*.17,hr*.14,hr*.25),'skinLight','Head')
        sphere('Ear_Inset',(s*hr*.98,-hr*.10,head_z-hr*.03),(hr*.085,hr*.04,hr*.14),'blush','Head',16,12)
        eye_x=s*hr*.38; eye_y=-hr*.735; eye_z=head_z+hr*.105
        sphere('Eye_White',(eye_x,eye_y,eye_z),(hr*.265,hr*.112,hr*.30),'white','Head',28,20)
        sphere('Iris',(eye_x,eye_y-hr*.098,eye_z),(hr*.139,hr*.051,hr*.179),'iris','Head',24,16)
        sphere('Pupil',(eye_x,eye_y-hr*.141,eye_z),(hr*.078,hr*.025,hr*.118),'pupil','Head',20,14)
        sphere('Eye_Spark',(eye_x-hr*.042,eye_y-hr*.164,eye_z+hr*.068),(hr*.039,hr*.012,hr*.049),'white','Head',16,10)
        curve('Brow',[(eye_x-s*hr*.21,-hr*.74,eye_z+hr*.32),(eye_x,-hr*.78,eye_z+hr*.38),(eye_x+s*hr*.21,-hr*.69,eye_z+hr*.30)],hr*.042,spec['hair'],'Head')
        sphere('Cheek',(s*hr*.56,-hr*.61,head_z-hr*.30),(hr*.20,hr*.022,hr*.085),'blush','Head',20,12)
    sphere('Nose',(0,-hr*.84,head_z-hr*.18),(hr*.13,hr*.17,hr*.14),'skinLight','Head')
    curve('Smile',[(-hr*.28,-hr*.684,head_z-hr*.45),(0,-hr*.767,head_z-hr*.52),(hr*.28,-hr*.684,head_z-hr*.45)],hr*.020,'brown','Head')
    # Hair cap and overlapping sculpted locks, with deliberately different silhouettes.
    sphere('HairCap',(0,hr*.02,head_z+hr*.39),(hr*.965,hr*.80,hr*.76),spec['hair'],'Head',32,20)
    for i in range(11 if name!='Efe' else 21):
        angle=i*(math.tau/(11 if name!='Efe' else 21))
        x=math.cos(angle)*hr*.74; y=math.sin(angle)*hr*.61
        z=head_z+hr*(.65+.14*math.sin(i*2.7))
        lock=sphere('HairLock_'+str(i),(x,y,z),(hr*.33,hr*.31,hr*.36),spec['hair'],'Head',20,14)
        lock.rotation_euler[1]=math.sin(i)*.3
    for i in range(4):
        sphere('Fringe_'+str(i),(-hr*.52+i*hr*.31,-hr*.55,head_z+hr*(.58+.12*math.sin(i))),(hr*.34,hr*.32,hr*.27),spec['hair'],'Head')
    if female and name!='Idil':
        for s in (-1,1):
            for i in range(4):
                sphere('SideHair',(s*hr*(.85+.08*math.sin(i)),hr*.08,head_z+hr*(.35-i*.30)),(hr*.27,hr*.42,hr*.34),spec['hair'],'Head')
        if name=='Ada':
            for i in range(4):
                sphere('Ponytail',(-hr*(1.03+i*.045),hr*.20,head_z+hr*(.34-i*.27)),(hr*.32,hr*.31,hr*.38),'hair','Head')
            sphere('CoralHairTie',(-hr*1.06,hr*.08,head_z+hr*.52),(hr*.20,hr*.12,hr*.15),'coral','Head')
    if name=='Yusuf':
        for s in (-1,1):
            sphere('Moustache',(s*hr*.14,-hr*.78,head_z-hr*.34),(hr*.22,hr*.085,hr*.10),'silver','Head')
    if name=='Emre':
        curve('BeardContour',[(-hr*.6,-hr*.55,head_z-hr*.39),(-hr*.40,-hr*.60,head_z-hr*.63),(0,-hr*.57,head_z-hr*.77),(hr*.40,-hr*.60,head_z-hr*.63),(hr*.6,-hr*.55,head_z-hr*.39)],hr*.06,'hair','Head')
    # Distinctive readable clothing construction.
    if spec['style']=='overalls':
        box('OverallBib',(0,-h*.084,shoulder-h*.096),(h*.18,h*.018,h*.18),'teal',h*.028,'Chest')
        for s in (-1,1):
            box('OverallStrap',(s*h*.067,-h*.080,shoulder-h*.005),(h*.030,h*.020,h*.16),'teal',h*.007,'Chest')
            sphere('BrassButton',(s*h*.067,-h*.097,shoulder-h*.055),(h*.008,h*.004,h*.008),'mustard','Chest',12,8)
        box('BibPocket',(0,-h*.099,shoulder-h*.14),(h*.10,h*.014,h*.071),'tealDark',h*.013,'Chest')
    else:
        box('FrontPlacket',(0,-h*.095,(hip+shoulder)/2),(h*.012,h*.013,shoulder-hip),'cream' if spec['style']=='vest' else spec['shirt'],h*.004,'Chest')
        for s in (-1,1):
            box('Pocket',(s*h*.075,-h*.098,shoulder-h*.10),(h*.06,h*.02,h*.052),'coral' if name=='Bora' else spec['shirt'],h*.008,'Chest')
    if name in ('Idil','Bora'):
        box('Belt',(0,-h*.003,hip+h*.006),(h*.285,h*.19,h*.035),'black',h*.012,'Hips')
        for s in (-1,1):
            box('UtilityPouch',(s*h*.10,-h*.103,hip),(h*.067,h*.041,h*.078),'black',h*.009,'Hips')
        box('Radio',(h*.105,-h*.12,shoulder-h*.05),(h*.035,h*.025,h*.064),'black',h*.007,'Chest')
        tube('RadioAntenna',(h*.105,-h*.12,shoulder-h*.015),(h*.105,-h*.12,shoulder+h*.04),h*.004,'black','Chest',vertices=12)
    if name=='Idil':
        sphere('HelmetShell',(0,hr*.015,head_z+hr*.52),(hr*1.10,hr*.98,hr*.75),'mustard','Head',36,24)
        box('HelmetBrim',(0,-hr*.10,head_z+hr*.35),(hr*2.26,hr*1.92,hr*.11),'mustard',hr*.04,'Head')
        box('HelmetBadge',(0,-hr*.99,head_z+hr*.61),(hr*.32,hr*.06,hr*.38),'navy',hr*.07,'Head')
        for z in (hip+h*.08,shoulder-h*.13):
            box('ReflectiveStripe',(0,-h*.098,z),(h*.29,h*.01,h*.022),'lime',h*.003,'Chest')
        for side,s in [('Left',1),('Right',-1)]:
            tube('LegReflector',(s*leg_x,0,ankle+h*.10),(s*leg_x,0,ankle+h*.13),h*.051,'lime',side+'LowerLeg',vertices=20)
    # Join into one skinned renderer while retaining material slots and bone groups.
    bpy.ops.object.select_all(action='DESELECT')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.object.join()
    mesh=bpy.context.object; mesh.name=name+'_OriginalSkinnedMesh'
    modifier=mesh.modifiers.new('Humanoid deformation','ARMATURE'); modifier.object=rig
    mesh.parent=rig
    export(name,'Characters')
    # The portrait is rendered from the authored 3D mesh, not generated as a stand-in.
    render_portrait(name,head_z,hr,rig)

def render_portrait(name,head_z,hr,rig):
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=20
    scene.cycles.use_denoising=True
    scene.render.resolution_x=384; scene.render.resolution_y=384; scene.render.resolution_percentage=100
    scene.render.film_transparent=True
    scene.world.color=(.08,.08,.08)
    scene.view_settings.view_transform='AgX'
    scene.view_settings.look='AgX - Medium High Contrast'
    scene.view_settings.exposure=-.25
    for side,sign in [('Left',1),('Right',-1)]:
        bone=rig.pose.bones[side+'UpperArm']
        basis=bone.bone.matrix_local.to_3x3().to_quaternion()
        bone.rotation_mode='QUATERNION'
        bone.rotation_quaternion=basis.inverted() @ Quaternion((0,1,0), sign*1.2) @ basis
    bpy.context.view_layer.update()
    bpy.ops.object.camera_add(location=(0,-2.6,head_z+.12))
    cam=bpy.context.object
    direction=Vector((0,0,head_z-hr*.22))-cam.location
    cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO'; cam.data.ortho_scale=hr*3.25
    scene.camera=cam
    for pos,energy,size in [((-2,-3,4),240,3),((2,-1,2.5),75,3),((0,2,3),130,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos)
        light=bpy.context.object; light.data.energy=energy; light.data.shape='DISK'; light.data.size=size
        light.rotation_euler=(Vector((0,0,head_z))-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PORTRAITS/(name+'.png'))
    bpy.ops.render.render(write_still=True)

def prop(name):
    reset()
    if name.startswith('Backpack'):
        box('PaddedBack',(0,.055,.30),(.36,.10,.52),'tealDark',.065)
        box('BagBody',(0,-.025,.27),(.39,.19,.44),'teal',.07)
        box('FrontPocket',(0,-.15,.19),(.29,.09,.23),'coral',.042)
        curve('PocketZip',[(-.115,-.199,.245),(0,-.201,.255),(.115,-.199,.245)],.006,'cream')
        for s in (-1,1):
            curve('PaddedStrap',[(s*.12,.09,.50),(s*.13,.19,.38),(s*.13,.18,.18),(s*.12,.08,.09)],.023,'tealDark')
            box('SidePocket',(s*.208,-.015,.18),(.055,.12,.19),'tealDark',.018)
        curve('CarryHandle',[(-.07,.04,.535),(-.06,.04,.59),(.06,.04,.59),(.07,.04,.535)],.014,'coral')
        if name.endswith('Open'):
            box('OpenMouth',(0,-.015,.50),(.30,.16,.035),'navy',.02)
            box('FoldedLid',(0,.12,.56),(.34,.045,.20),'teal',.035).rotation_euler[0]=-.4
        else:
            box('Lid',(0,-.015,.49),(.38,.20,.09),'teal',.04)
            curve('MainZip',[(-.17,-.11,.46),(0,-.123,.49),(.17,-.11,.46)],.006,'cream')
    elif name=='Flashlight':
        tube('Grip',(0,0,.02),(0,0,.21),.032,'tealDark',radius_end=.038)
        tube('LampHead',(0,0,.20),(0,0,.29),.058,'mustard',radius_end=.065)
        tube('Lens',(0,0,.284),(0,0,.295),.052,'white')
        box('Switch',(0,-.039,.17),(.028,.014,.04),'coral',.008)
        for z in (.055,.075,.095,.115):
            tube('GripRing',(0,0,z),(0,0,z+.008),.035,'teal')
    elif name=='Radio':
        box('Body',(0,0,.12),(.30,.105,.23),'coral',.028)
        box('Speaker',( -.065,-.057,.10),(.13,.012,.14),'navy',.02)
        for x in range(5):
            for z in range(5):
                sphere('SpeakerHole',(-.115+x*.025,-.066,.052+z*.025),(.005,.003,.005),'black',segments=10,rings=6)
        box('Display',(.081,-.06,.16),(.09,.014,.055),'cream',.005)
        sphere('Dial',(.09,-.07,.067),(.025,.012,.025),'mustard')
        tube('Antenna',(.11,0,.23),(.16,0,.49),.005,'metal',vertices=12)
        curve('Handle',[(-.08,0,.24),(-.07,0,.30),(.07,0,.30),(.08,0,.24)],.009,'navy')
        box('BatteryDoor',(0,.06,.085),(.20,.018,.10),'tealDark',.01)
    elif name=='Battery':
        tube('Cell',(0,0,0),(0,0,.105),.022,'mustard')
        tube('Contact',(0,0,.104),(0,0,.115),.009,'metal')
        tube('Sleeve',(0,0,.034),(0,0,.066),.0225,'navy')
    elif name=='Water':
        profile('Bottle',[(.0,.05,.05),(.025,.055,.055),(.18,.053,.053),(.23,.035,.035),(.27,.025,.025)],'water')
        tube('Cap',(0,0,.265),(0,0,.293),.028,'teal')
        tube('Label',(0,0,.085),(0,0,.155),.055,'cream')
        box('DateMark',(0,-.056,.12),(.056,.006,.025),'teal',.004)
    elif name in ('Food','FirstAid','Bandage','Documents','Blanket','Soap','FamilyCard'):
        sizes={'Food':(.15,.07,.22),'FirstAid':(.29,.12,.23),'Bandage':(.12,.04,.14),'Documents':(.20,.014,.14),
               'Blanket':(.29,.17,.12),'Soap':(.12,.065,.06),'FamilyCard':(.21,.015,.14)}
        size=sizes[name]; color={'Food':'mustard','FirstAid':'coral','Bandage':'cream','Documents':'cream','Blanket':'mustard','Soap':'cream','FamilyCard':'teal'}[name]
        box(name,(0,0,size[2]/2),size,color,.018)
        if name=='FirstAid':
            box('MedicalBarH',(0,-.065,.12),(.11,.009,.035),'cream',.004)
            box('MedicalBarV',(0,-.065,.12),(.035,.009,.11),'cream',.004)
            curve('CaseHandle',[(-.05,0,.24),(-.05,0,.285),(.05,0,.285),(.05,0,.24)],.01,'cream')
        if name=='Blanket':
            for z in (.023,.046,.069,.092):
                box('Fold',(0,-.086,z),(.26,.007,.006),'cream',.003)
        if name in ('Documents','FamilyCard'):
            for x in (-.065,0,.065):
                sphere('FamilyIcon',(x,-.01,.086),(.021,.006,.021),'cream' if name=='FamilyCard' else 'teal')
                box('FamilyBody',(x,-.01,.045),(.029,.007,.04),'cream' if name=='FamilyCard' else 'teal',.005)
    elif name=='Whistle':
        box('Body',(0,0,.025),(.075,.055,.05),'mustard',.02)
        box('Mouthpiece',(0,-.052,.028),(.038,.065,.028),'mustard',.007)
        box('AirSlot',(0,-.012,.052),(.026,.020,.006),'black',.002)
        curve('Cord',[(-.04,.035,.025),(-.08,.12,.025),(.08,.12,.025),(.04,.035,.025)],.004,'teal')
    elif name=='ComfortFox':
        sphere('Body',(0,0,.10),(.066,.045,.09),'coral')
        sphere('Head',(0,-.002,.20),(.073,.054,.064),'coral')
        for s in (-1,1):
            sphere('Ear',(s*.046,0,.255),(.022,.024,.039),'coral')
            sphere('Paw',(s*.064,-.01,.10),(.025,.025,.05),'coral')
            sphere('Foot',(s*.04,-.02,.025),(.032,.045,.025),'coral')
            sphere('Eye',(s*.028,-.052,.21),(.007,.005,.01),'black')
        sphere('Muzzle',(0,-.051,.182),(.037,.018,.025),'cream')
        sphere('Nose',(0,-.07,.189),(.01,.008,.008),'black')
    elif name=='ShoePair':
        for s in (-1,1):
            box('Sole',(s*.065,-.018,.013),(.10,.19,.023),'sole',.01)
            sphere('Upper',(s*.065,0,.043),(.052,.101,.041),'coral')
            for y in (-.045,-.025,-.005): box('Lace',(s*.065,y,.075),(.06,.008,.007),'cream',.003)
    elif name=='WalkingCane':
        tube('Shaft',(0,0,.035),(0,0,.78),.012,'brown')
        curve('Grip',[(0,0,.76),(0,-.005,.84),(.035,-.005,.87),(.09,-.005,.85)],.016,'brown')
        tube('RubberFoot',(0,0,0),(0,0,.05),.021,'black')
    elif name in ('Parcel','ToyBox','Cushion'):
        size=(.36,.25,.24) if name!='Cushion' else (.45,.38,.10)
        box(name,(0,0,size[2]/2),size,'sand' if name=='Parcel' else ('teal' if name=='ToyBox' else 'mustard'),.04)
        if name=='Parcel':
            box('Tape',(0,0,size[2]+.002),(.055,.25,.005),'cream',.001)
        if name=='ToyBox':
            box('Lid',(0,0,.25),(.38,.27,.035),'cream',.014)
    elif name=='Bracket':
        box('Vertical',(0,0,.08),(.07,.025,.16),'metal',.004)
        box('Horizontal',(0,-.065,.01),(.07,.13,.025),'metal',.004)
        for z in (.035,.12): sphere('Screw',(0,-.015,z),(.013,.007,.013),'navy',segments=12,rings=8)
    elif name=='SafetyStrap':
        box('Webbing',(0,0,.18),(.04,.012,.36),'teal',.004)
        box('Buckle',(0,-.01,.15),(.067,.022,.056),'metal',.008)
    elif name=='HoseNozzle':
        tube('Body',(0,0,.02),(0,0,.27),.045,'navy')
        tube('Collar',(0,0,.20),(0,0,.25),.059,'mustard')
        tube('Mouth',(0,0,.27),(0,0,.33),.047,'metal',radius_end=.04)
        curve('TwoHandBail',[(-.045,0,.08),(-.18,.04,.08),(-.18,.04,.12),(.18,.04,.12),(.18,.04,.08),(.045,0,.08)],.012,'tealDark')
        for x in (-.12,.12):
            tube('GripSleeve',(x-.045,.04,.12),(x+.045,.04,.12),.013,'black')
    elif name=='Cone':
        box('Base',(0,0,.025),(.35,.35,.05),'navy',.025)
        tube('Cone',(0,0,.05),(0,0,.50),.14,'coral',radius_end=.02)
        tube('Reflector',(0,0,.28),(0,0,.34),.078,'cream',radius_end=.06)
    elif name=='Bench':
        for x in (-.55,.55):
            box('Leg',(x,0,.25),(.08,.40,.50),'tealDark',.02)
            box('BackPost',(x,.16,.65),(.06,.07,.65),'tealDark',.015)
        for y in (-.15,0,.15): box('SeatSlat',(0,y,.48),(1.38,.115,.055),'wood',.012)
        for z in (.68,.84): box('BackSlat',(0,.18,z),(1.38,.05,.12),'wood',.012)
    elif name=='Plant':
        profile('Pot',[(0,.12,.12),(.03,.135,.135),(.24,.17,.17),(.26,.175,.175)],'terracotta')
        for i in range(8):
            a=i*math.tau/8
            end=(math.cos(a)*.16,math.sin(a)*.16,.45+(.06 if i%2 else 0))
            tube('Stem',(0,0,.24),end,.009,'leaf')
            leaf=sphere('Leaf',end,(.047,.11,.024),'leafLight' if i%2 else 'leaf')
            leaf.rotation_euler=(.5,0,-a)
    elif name=='Table':
        box('Tabletop',(0,0,.76),(1.48,.91,.10),'wood',.045)
        for x in (-.61,.61):
            for y in (-.33,.33):box('Leg',(x,y,.37),(.11,.11,.74),'woodDark',.018)
        for y in (-.34,.34):box('Apron',(0,y,.65),(1.27,.07,.14),'wood',.015)
    elif name in ('Shelf','Wardrobe'):
        width=.86 if name=='Shelf' else 1.12
        height=1.64 if name=='Shelf' else 1.92
        depth=.32 if name=='Shelf' else .50
        box('Back',(0,depth/2-.025,height/2),(width,.05,height),'cream',.015)
        for s in (-1,1):box('Side',(s*(width/2-.035),0,height/2),(.07,depth,height),'wood',.015)
        for z in (0.08,height-.04):box('Frame',(0,0,z),(width,depth,.07),'wood',.015)
        if name=='Shelf':
            for z in (.45,.86,1.27):box('ShelfBoard',(0,0,z),(width-.10,depth,.045),'wood',.01)
            for i in range(9):
                box('Book',(-.31+i*.075,-.06,.59),(.05,.20,.23+(.04 if i%2 else 0)),('teal','coral','mustard')[i%3],.005)
        else:
            for s in (-1,1):
                box('Door',(s*width/4,-depth/2-.015,height/2),(width/2-.04,.05,height-.14),'teal',.025)
                box('Inset',(s*width/4,-depth/2-.043,height*.64),(width/2-.12,.012,height*.51),'tealDark',.014)
                sphere('Handle',(s*.07,-depth/2-.075,.93),(.018,.025,.028),'mustard')
    else:
        raise ValueError(name)
    export(name)

def facade(name,wall_color,shutter_color,variant):
    reset()
    box('Plaster',(0,.20,2.7),(4.6,.42,5.4),wall_color,.055)
    box('StoneFoot',(0,-.06,.24),(4.68,.16,.48),'stone',.025)
    box('Cornice',(0,-.03,5.35),(4.85,.59,.14),'cream',.025)
    for x in (-1.18,1.18):
        for z in (1.56,3.84):
            box('WindowRecess',(x,-.035,z),(1.12,.12,1.43),'navy',.025)
            box('Glass',(x,-.11,z),(.77,.025,1.12),'glass',.014)
            for dx in (-.42,.42):box('Frame',(x+dx,-.14,z),(.06,.07,1.24),'cream',.008)
            for dz in (-.62,0,.62):box('Frame',(x,-.14,z+dz),(.87,.07,.055),'cream',.006)
            for s in (-1,1):
                sx=x+s*.67
                box('Shutter',(sx,-.10,z),(.37,.07,1.31),shutter_color,.016)
                for i in range(8):box('ShutterSlat',(sx,-.15,z-.49+i*.14),(.30,.04,.071),shutter_color,.008)
            box('Sill',(x,-.23,z-.70),(1.35,.39,.085),'stone',.022)
    for i in range(17):
        tube('RoofTile',(-2.28+i*.285,-.15,5.47),(-2.28+i*.285,.61,5.59),.16,'terracotta',vertices=12)
    for x in (-2.18,2.18):
        tube('RainPipe',(x,-.11,.3),(x,-.11,5.22),.037,'terracotta',vertices=16)
    if variant:
        box('BalconyFloor',(0,-.57,3.10),(2.7,1.03,.13),'stone',.025)
        for x in [i*.19-1.24 for i in range(14)]:
            tube('BalconyRail',(x,-1.0,3.14),(x,-1.0,4.02),.018,'tealDark',vertices=12)
        tube('BalconyTop',(-1.32,-1,4.03),(1.32,-1,4.03),.034,'tealDark',vertices=16)
    export(name,'Environment')

def main():
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    mode=args[0] if args else 'all'
    if mode in ('all','characters'):
        for spec in CAST:
            print('AUTHOR CHARACTER',spec['name'],flush=True)
            character(spec)
    props=['BackpackClosed','BackpackOpen','Flashlight','Radio','Battery','Water','Food','FirstAid','Bandage','Documents',
           'Blanket','Soap','FamilyCard','Whistle','ComfortFox','ShoePair','WalkingCane','Parcel','ToyBox','Cushion',
           'Bracket','SafetyStrap','HoseNozzle','Cone','Bench','Plant','Table','Shelf','Wardrobe']
    if mode in ('all','props'):
        for name in props:
            print('AUTHOR PROP',name,flush=True)
            prop(name)
        for i,(wall,shutter) in enumerate([('cream','teal'),('sand','coral'),('stone','blue'),('cream','olive')]):
            facade('Facade_'+str(i+1),wall,shutter,i%2)
    report={'origin':'Original procedural Blender authoring; no imported mesh assets',
            'cast':CAST,'props':props,'environment':['Facade_1','Facade_2','Facade_3','Facade_4'],
            'palette':PALETTE,'sourceTool':'Tools/YanYana/build_original_art.py'}
    (SOURCE/'ArtManifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('YAN YANA ORIGINAL ART COMPLETE',flush=True)

if __name__=='__main__': main()
