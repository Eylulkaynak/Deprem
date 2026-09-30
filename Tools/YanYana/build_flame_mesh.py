"""Original rounded, curling flame tongues; editable meshes, no downloaded FX."""
import importlib.util
import math
import pathlib
import bpy

spec = importlib.util.spec_from_file_location('yy', pathlib.Path(__file__).with_name('build_original_art.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)
a.reset()


def tongue(name, height, radius, curl, material, location, parent):
    # Rounded shoulders followed by an S-shaped taper, with enough rings to
    # preserve the silhouette without a subdivision modifier in the player.
    samples = [(0, .40), (.06, .70), (.15, .94), (.26, 1), (.38, .90),
               (.50, .73), (.62, .55), (.73, .40), (.82, .29),
               (.90, .19), (.96, .09), (1, .015)]
    ob = a.profile(name, [(t * height, r * radius, r * radius * .72)
                         for t, r in samples], material, segments=24)
    for v in ob.data.vertices:
        t = v.co.z / height
        v.co.x += curl * (math.sin(t * math.pi * 1.5) * .46 + t * t)
        v.co.y += radius * .16 * math.sin(t * math.pi)
    ob.location = location
    ob.parent = parent
    return ob


for i, (x, y, h, r, curl) in enumerate([
        (0, .025, .83, .20, .24),
        (-.20, -.015, .52, .145, -.19),
        (.19, .04, .64, .16, .18)]):
    group = bpy.data.objects.new('FlameLobe_' + str(i), None)
    bpy.context.collection.objects.link(group)
    tongue('EmberShell_' + str(i), h, r, curl, 'coral', (x, y, 0), group)
    tongue('GoldFlame_' + str(i), h * .76, r * .70, curl * .66,
           'mustard', (x - curl * .035, y - r * .54, .008), group)
    tongue('WarmHeart_' + str(i), h * .43, r * .40, curl * .22,
           'cream', (x, y - r * .92, .018), group)

a.export('FlameTuft', 'Props')
print('ORIGINAL CURLED FLAME: three lobes, nine layered meshes', flush=True)
