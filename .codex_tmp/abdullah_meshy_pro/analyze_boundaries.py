"""Report open boundary loops near the Meshy character's right waist."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


def arguments() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--glb", required=True, type=Path)
    return parser.parse_args(argv)


def main() -> None:
    args = arguments()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(args.glb.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    boundary_edges = [edge for edge in bm.edges if len(edge.link_faces) == 1]
    boundary_set = set(boundary_edges)
    seen: set[object] = set()
    reports: list[dict[str, object]] = []
    for start in boundary_edges:
        if start in seen:
            continue
        stack = [start]
        seen.add(start)
        edges = []
        vertices = set()
        while stack:
            edge = stack.pop()
            edges.append(edge)
            vertices.update(edge.verts)
            for vertex in edge.verts:
                for neighbor in vertex.link_edges:
                    if neighbor in boundary_set and neighbor not in seen:
                        seen.add(neighbor)
                        stack.append(neighbor)
        points = [vertex.co for vertex in vertices]
        minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        center = (minimum + maximum) * 0.5
        if center.x < 0.15 or not 0.35 < center.z < 0.80:
            continue
        reports.append(
            {
                "edges": len(edges),
                "vertices": len(vertices),
                "min": list(minimum),
                "max": list(maximum),
                "center": list(center),
                "points": [list(point) for point in points],
            }
        )
    reports.sort(key=lambda item: ((Vector(item["center"]) - Vector((0.44, -0.15, 0.57))).length))
    print("[BoundaryLoops] " + json.dumps(reports[:40]))
    bm.free()


if __name__ == "__main__":
    main()
