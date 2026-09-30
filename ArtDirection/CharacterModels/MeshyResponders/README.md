# Meshy Responders

Game-ready stylized emergency-response characters generated in Meshy and prepared for the Deprem Unity project.

| Role | Source | Geometry | Unity asset |
| --- | --- | ---: | --- |
| Police | `Meshy_AI_Character_output.fbx` | 4,203 triangles | `Assets/Story/Characters/MeshyResponders/Police/Police.fbx` |
| Firefighter | `Meshy_AI_Character_output (1).fbx` | 4,203 triangles | `Assets/Story/Characters/MeshyResponders/Firefighter/Firefighter.fbx` |
| RescueWorker (AFAD) | `Meshy_AI_Character_output (2).fbx` | 4,344 triangles | `Assets/Story/Characters/MeshyResponders/RescueWorker/RescueWorker.fbx` |

All three characters are normalized to 1.8 m, use a single 4K base-color texture, and include a generated humanoid armature with a maximum of four influences per vertex. Unity imports them as Humanoid, assigns URP/Lit materials, and builds reusable prefabs under `Assets/Story/Characters/MeshyResponders/Prefabs`.

Each role folder contains:

- `GameReady`: rigged and unrigged FBX, Blender source, OBJ/MTL, and base-color texture.
- `Preview`: orthographic turntable views and a posed rig/deformation check.
- `Source/Metadata.json`: source filename, dimensions, and mesh statistics.
