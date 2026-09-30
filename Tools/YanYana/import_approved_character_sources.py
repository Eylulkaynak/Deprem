"""Compatibility entry point for the current single-armature wardrobe importer."""
import pathlib
import runpy

runpy.run_path(str(pathlib.Path(__file__).with_name('import_character_wardrobe_sources.py')), run_name='__main__')
