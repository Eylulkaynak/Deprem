"""Create a clean, static OBJ from a Meshy FBX for Mixamo auto-rigging.

Dependency: pip install ufbx
"""

from __future__ import annotations

import argparse
import math
import shutil
from pathlib import Path

import ufbx


def _f(value: float) -> str:
    return f"{value:.9g}"


def _normal(value: ufbx.Vec3) -> tuple[float, float, float]:
    length = math.sqrt(value.x * value.x + value.y * value.y + value.z * value.z)
    if length <= 1.0e-12:
        return 0.0, 1.0, 0.0
    return value.x / length, value.y / length, value.z / length


def _transform_position(matrix: ufbx.Matrix, value: ufbx.Vec3) -> ufbx.Vec3:
    return ufbx.Vec3(
        matrix.c0.x * value.x
        + matrix.c1.x * value.y
        + matrix.c2.x * value.z
        + matrix.c3.x,
        matrix.c0.y * value.x
        + matrix.c1.y * value.y
        + matrix.c2.y * value.z
        + matrix.c3.y,
        matrix.c0.z * value.x
        + matrix.c1.z * value.y
        + matrix.c2.z * value.z
        + matrix.c3.z,
    )


def _transform_direction(matrix: ufbx.Matrix, value: ufbx.Vec3) -> ufbx.Vec3:
    return ufbx.Vec3(
        matrix.c0.x * value.x + matrix.c1.x * value.y + matrix.c2.x * value.z,
        matrix.c0.y * value.x + matrix.c1.y * value.y + matrix.c2.y * value.z,
        matrix.c0.z * value.x + matrix.c1.z * value.y + matrix.c2.z * value.z,
    )


def _find_texture(source_folder: Path, suffix: str) -> Path | None:
    return next(source_folder.rglob(f"*{suffix}"), None)


def export_character(
    role: str,
    source_fbx: Path,
    output_folder: Path,
    target_faces: int | None,
    x_scale: float,
) -> None:
    output_folder.mkdir(parents=True, exist_ok=True)
    source_folder = source_fbx.parent

    textures = list(source_folder.rglob("*.png"))
    for texture in textures:
        shutil.copy2(texture, output_folder / texture.name)

    diffuse = _find_texture(source_folder, "_texture.png")
    normal = _find_texture(source_folder, "_normal.png")
    roughness = _find_texture(source_folder, "_roughness.png")
    metallic = _find_texture(source_folder, "_metallic.png")
    if diffuse is None:
        raise FileNotFoundError(f"Diffuse texture missing beside {source_fbx}")

    material_name = f"{role}_Material"
    mtl_lines = [
        f"newmtl {material_name}",
        "Ka 1 1 1",
        "Kd 1 1 1",
        "Ks 0 0 0",
        "d 1",
        "illum 2",
        f"map_Kd {diffuse.name}",
    ]
    if normal:
        mtl_lines.append(f"map_Bump {normal.name}")
    if roughness:
        mtl_lines.append(f"map_Pr {roughness.name}")
    if metallic:
        mtl_lines.append(f"map_Pm {metallic.name}")
    (output_folder / f"{role}.mtl").write_text(
        "\n".join(mtl_lines) + "\n", encoding="utf-8"
    )

    scene = ufbx.load_file(str(source_fbx), generate_missing_normals=True)
    obj_path = output_folder / f"{role}.obj"

    if target_faces is not None:
        _write_simplified_obj(
            scene, obj_path, role, material_name, target_faces
        )
        return

    with obj_path.open("w", encoding="utf-8", newline="\n") as output:
        output.write("# Clean static OBJ generated from the approved Meshy FBX.\n")
        output.write(f"mtllib {role}.mtl\n")
        output.write(f"o {role}\n")

        position_offset = 1
        uv_offset = 1
        normal_offset = 1

        for mesh in scene.meshes:
            for instance_index, node in enumerate(mesh.instances):
                group_name = f"{mesh.name or 'mesh'}_{instance_index}".replace(" ", "_")
                output.write(f"g {group_name}\n")
                output.write(f"usemtl {material_name}\n")

                geometry_matrix = node.geometry_to_world
                normal_matrix = ufbx.get_compatible_matrix_for_normals(node)

                for value in mesh.vertex_position.values:
                    point = _transform_position(geometry_matrix, value)
                    output.write(
                        f"v {_f(point.x * x_scale)} {_f(point.y)} {_f(point.z)}\n"
                    )

                for value in mesh.vertex_uv.values:
                    output.write(f"vt {_f(value.x)} {_f(value.y)}\n")

                for value in mesh.vertex_normal.values:
                    transformed = _transform_direction(normal_matrix, value)
                    x, y, z = _normal(transformed)
                    output.write(f"vn {_f(x)} {_f(y)} {_f(z)}\n")

                for face in mesh.faces:
                    corners: list[str] = []
                    for index in range(
                        face.index_begin, face.index_begin + face.num_indices
                    ):
                        position_index = (
                            position_offset + mesh.vertex_position.indices[index]
                        )
                        uv_index = uv_offset + mesh.vertex_uv.indices[index]
                        normal_index = normal_offset + mesh.vertex_normal.indices[index]
                        corners.append(
                            f"{position_index}/{uv_index}/{normal_index}"
                        )

                    if mesh.reversed_winding:
                        corners.reverse()

                    for corner in range(1, len(corners) - 1):
                        output.write(
                            f"f {corners[0]} {corners[corner]} "
                            f"{corners[corner + 1]}\n"
                        )

                position_offset += len(mesh.vertex_position.values)
                uv_offset += len(mesh.vertex_uv.values)
                normal_offset += len(mesh.vertex_normal.values)


def _write_simplified_obj(
    scene: ufbx.Scene,
    obj_path: Path,
    role: str,
    material_name: str,
    target_faces: int,
) -> None:
    import fast_simplification
    import numpy as np

    positions: list[tuple[float, float, float]] = []
    uvs: list[tuple[float, float]] = []
    faces: list[tuple[int, int, int]] = []
    vertex_map: dict[tuple[int, int, int, int, int], int] = {}

    for mesh_index, mesh in enumerate(scene.meshes):
        for instance_index, node in enumerate(mesh.instances):
            geometry_matrix = node.geometry_to_world
            for face in mesh.faces:
                corners: list[int] = []
                for index in range(
                    face.index_begin, face.index_begin + face.num_indices
                ):
                    key = (
                        mesh_index,
                        instance_index,
                        mesh.vertex_position.indices[index],
                        mesh.vertex_uv.indices[index],
                        mesh.vertex_normal.indices[index],
                    )
                    vertex_index = vertex_map.get(key)
                    if vertex_index is None:
                        point = _transform_position(
                            geometry_matrix,
                            mesh.vertex_position.values[key[2]],
                        )
                        uv = mesh.vertex_uv.values[key[3]]
                        vertex_index = len(positions)
                        vertex_map[key] = vertex_index
                        positions.append((point.x, point.y, point.z))
                        uvs.append((uv.x, uv.y))
                    corners.append(vertex_index)

                if mesh.reversed_winding:
                    corners.reverse()
                for corner in range(1, len(corners) - 1):
                    faces.append((corners[0], corners[corner], corners[corner + 1]))

    points = np.asarray(positions, dtype=np.float64)
    triangles = np.asarray(faces, dtype=np.int32)
    uv_values = np.asarray(uvs, dtype=np.float64)
    points_out, faces_out, collapses = fast_simplification.simplify(
        points,
        triangles,
        target_count=target_faces,
        agg=5.0,
        return_collapses=True,
    )
    replay_points, replay_faces, original_to_new = (
        fast_simplification.replay_simplification(
            points.astype(np.float32), triangles, collapses
        )
    )

    if (
        replay_points.shape == points_out.shape
        and replay_faces.shape == faces_out.shape
    ):
        points_out = replay_points
        faces_out = replay_faces

    uv_out = np.zeros((len(points_out), 2), dtype=np.float64)
    uv_counts = np.zeros(len(points_out), dtype=np.float64)
    np.add.at(uv_out, original_to_new, uv_values)
    np.add.at(uv_counts, original_to_new, 1.0)
    uv_out /= np.maximum(uv_counts[:, None], 1.0)

    normals = np.zeros_like(points_out)
    edge_a = points_out[faces_out[:, 1]] - points_out[faces_out[:, 0]]
    edge_b = points_out[faces_out[:, 2]] - points_out[faces_out[:, 0]]
    face_normals = np.cross(edge_a, edge_b)
    for corner in range(3):
        np.add.at(normals, faces_out[:, corner], face_normals)
    lengths = np.linalg.norm(normals, axis=1)
    normals /= np.maximum(lengths[:, None], 1.0e-12)

    with obj_path.open("w", encoding="utf-8", newline="\n") as output:
        output.write("# Clean simplified OBJ for Mixamo auto-rigging.\n")
        output.write(f"mtllib {role}.mtl\n")
        output.write(f"o {role}\n")
        output.write(f"usemtl {material_name}\n")
        for point in points_out:
            output.write(f"v {_f(point[0])} {_f(point[1])} {_f(point[2])}\n")
        for uv in uv_out:
            output.write(f"vt {_f(uv[0])} {_f(uv[1])}\n")
        for normal in normals:
            output.write(
                f"vn {_f(normal[0])} {_f(normal[1])} {_f(normal[2])}\n"
            )
        for face in faces_out:
            a, b, c = face + 1
            output.write(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--role", required=True)
    parser.add_argument("--fbx", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--target-faces", type=int)
    parser.add_argument("--x-scale", type=float, default=1.0)
    args = parser.parse_args()
    export_character(
        args.role,
        args.fbx.resolve(),
        args.output.resolve(),
        args.target_faces,
        args.x_scale,
    )


if __name__ == "__main__":
    main()
