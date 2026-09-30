# Abdullah Ekinci Character Production Notes

## Identity and permission context

- Character subject: Abdullah Ekinci, identified by the project owner as the president of the Chamber of Civil Engineers (IMO) and the project's employer.
- Permission context: on 2026-09-01, the project owner stated in the production chat that Abdullah Ekinci personally requested to appear in the game.
- This note records the production context reported in chat; it is not a substitute for a signed likeness release if one is required later.

## Reference

- Approved stylized image reference: `ArtDirection/CharacterReferences/AbdullahEkinci_Reference_v1.png`
- Visual language: the approved family character style under `ArtDirection/CharacterReferences/`.

## Meshy generation

- Date: 2026-09-01
- Tool/model: Meshy T2, Smart Topology
- Target polygon count: 15,000
- Result: 14,849 triangle faces, 10,587 vertices
- Texture: enabled
- Pose correction: disabled because it required Pro; the reference already uses a clean A-pose
- License selected: CC BY 4.0
- Publishing: the model was not published to Meshy Community
- Geometry task ID: `01a05c46-d2da-7648-8809-b72555f54f0e`
- Textured task ID: `01a05c46-dbf9-779e-8d40-95d43f8ff175`

## Download and attribution

- FBX download was blocked by the Meshy Free-plan subscription gate on 2026-09-01; no purchase was made.
- Required attribution for the Free-plan output: `Model created with Meshy - CC BY 4.0 License`.
- No Meshy geometry or texture was extracted or used in the downloadable local package below.

## Downloadable local production asset

- Date: 2026-09-01
- Reconstruction: official `stabilityai/TripoSR` image-to-3D model, run locally from `AbdullahEkinci_Reference_v1.png`.
- TripoSR code and pretrained model license: MIT.
- Cleanup: Z-up normalization, -Y character forward, voxel surface fusion, UV re-atlas, 2048 px base-color rebake and Unity-oriented FBX export in Blender 5.2 LTS.
- Game mesh: approximately 15,000 triangles and 7,500 Blender vertices before OBJ export splitting.
- Rig: Blender humanoid-style armature using the project's existing family auto-rig tool; no Unity runtime code was added.

Primary files:

- `GameReady/AbdullahEkinci_GameReady_Rigged.fbx`: recommended Unity import.
- `GameReady/AbdullahEkinci_GameReady_Unrigged.fbx`: clean unrigged fallback.
- `GameReady/AbdullahEkinci_GameReady.obj` + `.mtl`: portable mesh source.
- `GameReady/AbdullahEkinci_BaseColor.png`: 2048 px base-color texture.
- `GameReady/AbdullahEkinci_GameReady.blend`: editable Blender source.
- `AbdullahEkinci_Downloadable_Package.zip`: complete portable package.

The higher-density reconstruction and its editable Blender source are retained under `Source/`. Preview renders and the approved 2D reference are under `Preview/`.

## Unity integration

- Rigged model: `Assets/Story/Characters/MeshyFamily/AbdullahEkinci/AbdullahEkinci_Rigged.fbx`
- Base-color texture: `Assets/Story/Characters/MeshyFamily/AbdullahEkinci/AbdullahEkinci_BaseColor.png`
- URP material: `Assets/Story/Characters/MeshyFamily/AbdullahEkinci/AbdullahEkinci_URP.mat`
- Reusable prefab variant: `Assets/Story/Characters/MeshyFamily/Prefabs/AbdullahEkinci.prefab`
- Unity verification: 15,000 triangles, 10,668 imported vertices, one submesh, UVs/normals/tangents present, 1.8 m height, valid Human avatar created from this model.
- Import policy: animations, cameras, lights, visibility and embedded material import disabled; external URP/Lit material uses the 2048 px base-color texture.
