"""Original rounded tabletop landing board; metre-scale editable Blender source."""
import importlib.util,pathlib,bpy
spec=importlib.util.spec_from_file_location('yy_art',pathlib.Path(__file__).with_name('build_original_art.py'))
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
bpy.context.preferences.filepaths.save_version=0
art.reset()
art.box('RoundedLandingBoard',(0,0,0),(.40,.86,.024),'wood',.012)
for z in (-.39,.39):art.box('SoftEdgeBand',(0,z,.014),(.37,.035,.018),'teal',.008)
art.export('IntroTrayDock','Props')
