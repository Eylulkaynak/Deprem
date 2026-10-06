"""Closed, metre-scaled Mediterranean houses for the continuous KKTC scene.

Editor assets only. All sides, roofs and entrances are modelled; no billboard
or runtime mesh generation is used. --staging keeps Unity untouched during QA.
Run Blender --background --python this_file -- --staging (or omit --staging).
"""
import importlib.util
import math
import pathlib
import sys

import bpy
from mathutils import Matrix, Vector

spec = importlib.util.spec_from_file_location(
    "original", pathlib.Path(__file__).with_name("build_original_art.py"))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if "--staging" in args:
    stage = a.ROOT / ".codex_tmp/neighborhood_design_20261005"
    a.SOURCE = stage / "Source"
    a.MODELS = stage / "Models"
    (a.SOURCE / "Environment").mkdir(parents=True, exist_ok=True)
    a.MODELS.mkdir(parents=True, exist_ok=True)

a.PALETTE.update(limePlaster="#E9DFC5", peachPlaster="#DDB49B",
                 paleOchre="#DCCBA8", roofClay="#B87455",
                 roofClayLight="#C98766", roofClayDark="#A7664C")


def window(at, yaw, shutter, upper=False):
    before = set(bpy.context.scene.objects)
    a.box("DeepWindowRecess", (0, -.024, 0), (1.03, .11, 1.40), "navy", .028)
    a.box("WindowGlass", (0, -.096, 0), (.77, .032, 1.16), "glass", .015)
    for x in (-.425, .425):
        a.box("WindowFrame", (x, -.134, 0), (.072, .075, 1.28), "cream", .012)
    for z in (-.62, 0, .62):
        a.box("WindowFrame", (0, -.135, z), (.90, .075, .065), "cream", .01)
    for side in (-1, 1):
        x = side * .66
        a.box("TimberShutter", (x, -.106, 0), (.36, .085, 1.35), shutter, .022)
        for i in range(8):
            a.box("ShutterLouvre", (x, -.163, -.51 + i * .145),
                  (.28, .033, .075), shutter, .008)
    a.box("LimestoneSill", (0, -.21, -.73), (1.46, .35, .10), "stone", .027)
    rotation = Matrix.Rotation(math.radians(yaw), 3, "Z")
    for obj in set(bpy.context.scene.objects) - before:
        obj.location = rotation @ obj.location + Vector(at)
        obj.rotation_euler = (rotation.to_quaternion() @ obj.rotation_euler.to_quaternion()).to_euler()


def roof():
    width, front, back, ridge, eave, top = 3.36, -.34, 6.34, 3., 5.43, 6.08
    verts = [(-width, front, eave), (width, front, eave),
             (-width, ridge, top), (width, ridge, top),
             (-width, back, eave), (width, back, eave),
             (-width, front, eave - .11), (width, front, eave - .11),
             (-width, back, eave - .11), (width, back, eave - .11)]
    faces = [(0, 1, 3, 2), (2, 3, 5, 4), (6, 8, 9, 7),
             (0, 6, 7, 1), (4, 5, 9, 8), (0, 2, 4, 8, 6), (1, 7, 9, 5, 3)]
    mesh = bpy.data.meshes.new("ClosedRoofBase")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("ClosedRoofBase", mesh)
    bpy.context.collection.objects.link(obj)
    a.finish(obj, "ClosedRoofBase", "roofClayDark")
    # Solid gables close both sides of the roof, above the two-storey shell.
    for side in (-1, 1):
        x = side * 3.10
        vertices = [(x, 0, 5.30), (x, 6, 5.30), (x, 3, 6.02)]
        gable = bpy.data.meshes.new("LimeGable")
        gable.from_pydata(vertices, [], [(0, 1, 2), (2, 1, 0)])
        gable.update()
        go = bpy.data.objects.new("ClosedLimeGable", gable)
        bpy.context.collection.objects.link(go)
        a.finish(go, "ClosedLimeGable", current_wall)
    # Individual clay channels follow the pitch, then are consolidated by material.
    for slope in (-1, 1):
        for row in range(8):
            y0 = ridge + slope * row * .416
            y1 = ridge + slope * min((row + 1) * .416 + .065, 3.34)
            z0 = top - abs(y0 - ridge) * (top - eave) / 3.34
            z1 = top - abs(y1 - ridge) * (top - eave) / 3.34
            for column in range(24):
                x = -3.22 + column * .28
                color = ("roofClay", "roofClayLight", "roofClay", "roofClayDark")[(column + row * 3) % 4]
                a.tube("ClayRoofChannel", (x, y0, z0 + .034),
                       (x, y1, z1 + .034), .136, color, vertices=10)
    a.tube("RoundedRidgeCap", (-3.36, 3, 6.13), (3.36, 3, 6.13),
           .155, "roofClayLight", vertices=14)


def consolidate():
    # Each house imports as ~16 static material meshes, not hundreds of parts.
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    groups = {}
    for obj in objects:
        key = obj.data.materials[0].name
        groups.setdefault(key, []).append(obj)
    for material, parts in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.object.convert(target="MESH")
        bpy.ops.object.join()
        bpy.context.object.name = "HouseVolume_" + material


def house(name, wall, shutter, balcony):
    global current_wall
    current_wall = wall
    a.reset()
    # True 6.2 x 6.0 m solid shell: the back/side views are fully modelled.
    a.box("ClosedTwoStoreyShell", (0, 3, 2.65), (6.20, 6.00, 5.30), wall, .075)
    a.box("ContinuousStonePlinth", (0, 3, .21), (6.31, 6.10, .42), "stone", .032)
    a.box("FullDepthCornice", (0, 3, 5.28), (6.46, 6.24, .17), "cream", .035)
    a.box("FloorStringCourse", (0, 3, 2.87), (6.30, 6.10, .09), "cream", .018)
    for x in (-1.82, 1.82):
        for z in (1.52, 3.93):
            window((x, -.01, z), 0, shutter)
    for side in (-1, 1):
        for y in (1.5, 4.5):
            for z in (1.52, 3.93):
                window((side * 3.105, y, z), side * 90, shutter)
    for x in (-1.82, 1.82):
        for z in (1.52, 3.93):
            window((x, 6.01, z), 180, shutter)
    a.box("EntranceDoor", (0, -.075, 1.06), (1.13, .12, 2.04), "tealDark", .045)
    for z in (.59, 1.51):
        a.box("DoorRecessPanel", (0, -.153, z), (.85, .035, .68), shutter, .028)
    for x in (-.65, .65):
        a.box("DoorStoneFrame", (x, -.10, 1.13), (.16, .19, 2.20), "stone", .032)
    a.box("DoorStoneLintel", (0, -.12, 2.26), (1.46, .23, .19), "stone", .04)
    a.box("GroundedDoorstep", (0, -.28, .065), (1.62, .65, .13), "stone", .04)
    a.sphere("BrassDoorKnob", (.39, -.183, 1.10), (.035, .028, .035), "mustard", segments=12, rings=8)
    a.box("DoorLampBracket", (.99, -.12, 2.17), (.13, .16, .25), "tealDark", .018)
    a.box("WarmDoorLamp", (.99, -.21, 2.17), (.09, .09, .17), "mustard", .024)
    if balcony:
        a.box("UpperBalconyDoor", (0, -.06, 3.99), (1.10, .12, 1.82), "navy", .022)
        a.box("UpperBalconyGlass", (0, -.14, 4.03), (.86, .03, 1.46), "glass", .015)
        for x in (-.52, .52):
            a.box("BalconyDoorFrame", (x, -.16, 3.99), (.06, .06, 1.83), "cream", .015)
        a.box("BalconyFloor", (0, -.66, 3.05), (2.96, 1.22, .15), "stone", .036)
        for x in [i * .19 - 1.33 for i in range(15)]:
            a.tube("BalconyBaluster", (x, -1.18, 3.12), (x, -1.18, 4.02), .020, "tealDark", vertices=10)
        a.tube("BalconyTopRail", (-1.42, -1.18, 4.04), (1.42, -1.18, 4.04), .034, "tealDark", vertices=12)
        for x in (-1.42, 1.42):
            a.tube("BalconyReturnRail", (x, -1.18, 4.04), (x, -.08, 4.04), .034, "tealDark", vertices=12)
            for y in (-.95, -.7, -.45, -.2):
                a.tube("BalconySideBaluster", (x, y, 3.12), (x, y, 4.03), .020, "tealDark", vertices=10)
    else:
        # A small central shuttered opening keeps the second floor inhabited.
        a.box("SmallUpperShutter", (0, -.085, 4.07), (.74, .14, .97), shutter, .025)
        for z in [3.72 + i * .11 for i in range(7)]:
            a.box("SmallShutterLouvre", (0, -.17, z), (.64, .035, .055), shutter, .006)
    roof()
    a.box("Chimney", (-1.95, 4.62, 5.93), (.47, .52, 1.35), wall, .043)
    a.box("ChimneyStoneCap", (-1.95, 4.62, 6.63), (.61, .66, .12), "stone", .03)
    for x in (-2.96, 2.96):
        a.tube("RainwaterPipe", (x, -.075, .31), (x, -.075, 5.25), .039, "terracotta", vertices=12)
    consolidate()
    a.export(name, "Environment")
    print("CLOSED KKTC HOUSE EXPORTED", name, flush=True)


for specification in [
    ("KKTC_House_1", "limePlaster", "teal", True),
    ("KKTC_House_2", "peachPlaster", "olive", False),
    ("KKTC_House_3", "paleOchre", "blue", True),
    ("KKTC_House_4", "cream", "tealDark", False),
]:
    house(*specification)
