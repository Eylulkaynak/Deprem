"""Export facial UV triangles used by the offline beard texture pass."""

from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


ROOT = Path(r"C:\Users\Gokturk\Documents\GitHub\Deprem")
BASE_FBX = ROOT / "ArtDirection/CharacterModels/MeshyFamily/Baba/BlenderRigged/Baba_Rigged.fbx"
OUTPUT = ROOT / ".codex_tmp/abdullah_rebuild/beard_uv.npz"


def add_polygon_triangles(target: list[list[tuple[float, float]]], polygon, uv_data) -> None:
    polygon_uvs = [(float(uv_data[index].uv.x), float(uv_data[index].uv.y)) for index in polygon.loop_indices]
    for index in range(1, len(polygon_uvs) - 1):
        target.append([polygon_uvs[0], polygon_uvs[index], polygon_uvs[index + 1]])


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(BASE_FBX), use_anim=False)
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
uv_data = mesh.data.uv_layers.active.data

full_beard_triangles = []
for polygon in mesh.data.polygons:
    center = sum((mesh.data.vertices[index].co for index in polygon.vertices), Vector()) / len(polygon.vertices)
    facial_surface = center.y < -5.0 and abs(center.x) < 17.4
    full_beard = facial_surface and 39.8 < center.z < 53.8
    if full_beard:
        add_polygon_triangles(full_beard_triangles, polygon, uv_data)

np.savez_compressed(
    OUTPUT,
    full_beard=np.asarray(full_beard_triangles, dtype=np.float32),
)
print(f"Exported {len(full_beard_triangles)} beard UV triangles to {OUTPUT}")
