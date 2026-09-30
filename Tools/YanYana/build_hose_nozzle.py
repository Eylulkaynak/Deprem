"""Rebuild only the original editable two-hand nozzle, using the shared art palette."""
import importlib.util,pathlib,bpy
here=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('yy_art',here/'build_original_art.py')
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
bpy.context.preferences.filepaths.save_version=0
art.prop('HoseNozzle')
