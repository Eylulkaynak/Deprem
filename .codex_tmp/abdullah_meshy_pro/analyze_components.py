"""Report connected mesh components for the downloaded Meshy character."""

from __future__ import annotations

import argparse
import json
import sys
from collections import deque
from pathlib import Path

import bpy
from mathutils import Vector


def args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :]
    parser = argparse.ArgumentParser()
    parser.add_argument("--glb", required=True, type=Path)
    return parser.parse_args(argv)


def main() -> None:
    options = args()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(options.glb.resolve()))
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    mesh = obj.data
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)

    component_index = [-1] * len(mesh.vertices)
    components: list[list[int]] = []
    for start in range(len(mesh.vertices)):
        if component_index[start] >= 0:
            continue
        index = len(components)
        queue = deque([start])
        component_index[start] = index
        vertices: list[int] = []
        while queue:
            current = queue.popleft()
            vertices.append(current)
            for neighbor in adjacency[current]:
                if component_index[neighbor] < 0:
                    component_index[neighbor] = index
                    queue.append(neighbor)
        components.append(vertices)

    face_counts = [0] * len(components)
    for polygon in mesh.polygons:
        face_counts[component_index[polygon.vertices[0]]] += 1

    reports = []
    for index, vertices in enumerate(components):
        points = [mesh.vertices[i].co for i in vertices]
        minimum = Vector(tuple(min(point[a] for point in points) for a in range(3)))
        maximum = Vector(tuple(max(point[a] for point in points) for a in range(3)))
        reports.append(
            {
                "component": index,
                "vertices": len(vertices),
                "faces": face_counts[index],
                "min": list(minimum),
                "max": list(maximum),
                "extent": list(maximum - minimum),
            }
        )
    reports.sort(key=lambda item: item["faces"], reverse=True)
    print("[Components] " + json.dumps(reports))


if __name__ == "__main__":
    main()
