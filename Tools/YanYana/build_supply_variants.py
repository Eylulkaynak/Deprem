"""Editable package variants from this adventure's own Blender props only."""
import pathlib,runpy,bpy
root=pathlib.Path(__file__).resolve().parents[2]
author=runpy.run_path(str(root/'Tools/YanYana/build_original_art.py'))
for key in ('Water','Food'):
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Props'/f'{key}.blend'))
    author['MATS'].clear()
    if key=='Water':
        bpy.data.objects['Cap'].location=(.090,-.025,.014)
        author['tube']('ExposedBottleMouth',(0,0,.27),(0,0,.276),.023,'tealDark')
    else:
        author['box']('OpenTear',(0,-.003,.224),(.105,.043,.009),'black',.003)
        flap=author['box']('RaisedWrapperFlap',(.025,.025,.234),(.072,.050,.008),'cream',.003)
        flap.rotation_euler.x=.35
    author['export'](key+'_Opened')
print('Two editable supply variants exported; original sources unchanged.')
