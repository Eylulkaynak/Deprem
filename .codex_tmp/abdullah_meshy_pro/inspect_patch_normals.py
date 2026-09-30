"""Inspect the repaired shirt face and its neighboring surface normals."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bmesh
import bpy


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--blend", required=True, type=Path)
    args = parser.parse_args(argv)
    bpy.ops.wm.open_mainfile(filepath=str(args.blend.resolve()))
    obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    patch_index = next(
        index
        for index, material in enumerate(obj.data.materials)
        if material and material.name == "AbdullahEkinci_ShirtPatch"
    )
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    patch_faces = [face for face in bm.faces if face.material_index == patch_index]
    records = []
    for face in patch_faces:
        neighbors = {
            neighbor
            for edge in face.edges
            for neighbor in edge.link_faces
            if neighbor is not face
        }
        records.append(
            {
                "vertices": len(face.verts),
                "normal": list(face.normal),
                "center": list(face.calc_center_median()),
                "neighbors": [list(neighbor.normal) for neighbor in neighbors],
                "neighbor_centers": [
                    list(neighbor.calc_center_median()) for neighbor in neighbors
                ],
            }
        )
    print("[PatchNormals] " + json.dumps(records))
    bm.free()


if __name__ == "__main__":
    main()
