// Offline Blender import and Editor scene authoring; no runtime component.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static class YanYanaResidentWorkshop
    {
        public const string Root = "Assets/YanYana/Characters/ResidentWorkshop";
        public const string Source = "ArtDirection/YanYana/Characters/ResidentWorkshop/ReviewPack";
        public const string FaceGraph = "Blender yüzü · konuşma ve tepkiler";
        public static readonly string[] Names = { "Eren", "Ece", "Aylin", "Zeynep", "Deniz", "Gul", "Kemal", "Arda" };
        public static readonly string[] ShapeNames = { "Mouth_A", "Mouth_E", "Mouth_O", "Blink", "Fear", "Surprise", "Smile" };
        static readonly string[] HumanBones = { "Hips", "Spine", "Chest", "Neck", "Head", "LeftShoulder", "RightShoulder", "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftFoot", "RightFoot", "LeftToes", "RightToes" };
        [Serializable] class Character { public string name, source_anatomy; public MaterialEntry[] materials; }
        [Serializable] class MaterialEntry { public string name, albedo; public float[] base_color_linear; public float roughness; }
        [Serializable] public class SpeechData { public SpeechLine[] lines; }
        [Serializable] public class SpeechLine { public string actor, audio, line; public Frame[] frames; }
        [Serializable] public class Frame { public float time, a, e, o; }
        public static SpeechData ReadSpeech() => JsonUtility.FromJson<SpeechData>(File.ReadAllText(Root + "/speech-curves.json"));
        public static string Identity(string sceneName)
        {
            string n = sceneName.Replace("Park sakini · ", "").Split('·')[0].Trim().Split(' ')[0];
            return n == "Aslı" ? "Asli" : n == "Gül" ? "Gul" : n;
        }
        public static bool IsChild(string name) => name == "Ece" || name == "Arda";
        public static GameObject Prefab(string sceneName)
        {
            string n = Identity(sceneName);
            return Names.Contains(n) ? AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + n + "/" + n + ".prefab") : null;
        }
        public static Transform[] Residents() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Where(x => x.name.StartsWith("Park sakini · ")).ToArray();
        public static SkinnedMeshRenderer Face(GameObject actor) => actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(s => s.sharedMesh && s.sharedMesh.GetBlendShapeIndex("Mouth_A") >= 0);

        [MenuItem("Tools/Yan Yana/Art/Import Resident Workshop")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing.");
            Directory.CreateDirectory(Root + "/Speech");
            foreach (string name in Names)
            {
                string folder = Root + "/" + name;
                Directory.CreateDirectory(folder + "/Textures"); Directory.CreateDirectory(folder + "/Materials");
                File.Copy(Source + "/" + name + "/" + name + "_Rigged.fbx", folder + "/" + name + ".fbx", true);
                File.Copy(Source + "/" + name + "/character.json", folder + "/character.json", true);
                foreach (string texture in Directory.GetFiles(Source + "/" + name + "/Textures", "*.png"))
                    File.Copy(texture, folder + "/Textures/" + Path.GetFileName(texture), true);
            }
            File.Copy("ArtDirection/YanYana/Characters/NewResidents/speech-curves.json", Root + "/speech-curves.json", true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new List<string> { "Blender workshop: eight resculpted characters based on the approved family anatomy." };
            foreach (string name in Names)
            {
                string folder = Root + "/" + name, path = folder + "/" + name + ".fbx";
                var character = JsonUtility.FromJson<Character>(File.ReadAllText(folder + "/character.json"));
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                foreach (var entry in character.materials)
                {
                    string safeName = string.Concat(entry.name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    string matPath = folder + "/Materials/" + safeName + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
                    var c = entry.base_color_linear;
                    mat.SetColor("_BaseColor", new Color(c[0], c[1], c[2], c[3]).gamma);
                    mat.SetFloat("_Smoothness", 1 - entry.roughness); mat.SetFloat("_Cull", 2);
                    if (!string.IsNullOrEmpty(entry.albedo))
                    {
                        string tp = folder + "/" + entry.albedo;
                        var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                        ti.sRGBTexture = true; ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.SaveAndReimport();
                        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); mat.SetColor("_BaseColor", Color.white);
                    }
                    EditorUtility.SetDirty(mat);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), mat);
                }
                // Texture reimports can reload unsaved ModelImporter settings. Configure the rig last.
                importer.animationType = ModelImporterAnimationType.Human; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false; importer.importBlendShapes = true; importer.importCameras = false; importer.importLights = false;
                importer.isReadable = false; importer.optimizeGameObjects = false; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import; importer.importBlendShapeNormals = ModelImporterNormals.None;
                var hd = importer.humanDescription;
                hd.human = HumanBones.Select(b => new HumanBone { boneName = b, humanName = b, limit = new HumanLimit { useDefaultValues = true } }).ToArray();
                hd.armStretch = .05f; hd.legStretch = .05f; hd.upperArmTwist = hd.lowerArmTwist = hd.upperLegTwist = hd.lowerLegTwist = .5f;
                importer.humanDescription = hd;
                importer.SaveAndReimport();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source); instance.name = "Blender · " + name;
                try
                {
                    var animator = instance.GetComponentInChildren<Animator>(); var skin = Face(instance);
                    if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException(name + ": invalid humanoid avatar");
                    if (!skin) throw new InvalidOperationException(name + ": facial mesh missing");
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = YanYanaAdultLocomotion.ForCharacter(IsChild(name) ? "Efe" : name, AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(YanYanaAdultLocomotion.BaseControllerPath));
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    foreach (string shape in ShapeNames)
                    {
                        int index = skin.sharedMesh.GetBlendShapeIndex(shape);
                        if (index < 0) throw new InvalidOperationException(name + ": missing " + shape);
                        skin.SetBlendShapeWeight(index, 0);
                    }
                    foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                        renderer.localBounds = new Bounds(new Vector3(0, .9f, 0), new Vector3(2, 2.7f, 1.6f));
                    PrefabUtility.SaveAsPrefabAsset(instance, folder + "/" + name + ".prefab");
                    report.Add("PASS " + name + " | humanoid avatar valid | 7 facial shapes | source anatomy: " + character.source_anatomy);
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            foreach (var line in ReadSpeech().lines.Where(l => Names.Contains(l.actor)))
            {
                string path = Root + "/Speech/" + line.audio + "_" + line.actor + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
                clip.ClearCurves(); clip.legacy = true; clip.frameRate = 30; clip.name = line.audio + "_" + line.actor;
                foreach (string vowel in new[] { "A", "E", "O" })
                {
                    var curve = new AnimationCurve(line.frames.Select(f => new Keyframe(f.time, 100 * (vowel == "A" ? f.a : vowel == "E" ? f.e : f.o))).ToArray());
                    for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
                    clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.Mouth_" + vowel, curve);
                }
                EditorUtility.SetDirty(clip);
            }
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("ClientExports/YanYana/Reports");
            File.WriteAllLines("ClientExports/YanYana/Reports/resident-workshop-import.txt", report);
            Debug.Log("RESIDENT_WORKSHOP_IMPORTED: eight valid humanoids and speech curves.");
        }

        [MenuItem("Tools/Yan Yana/QA/Inspect Resident Roots")]
        public static void InspectRoots()
        {
            var rows = new List<string>();
            foreach (var actor in Residents().Where(a => Names.Contains(Identity(a.name))))
            {
                rows.Add(actor.name + " | root components: " + string.Join(", ", actor.GetComponents<Component>().Select(c => c.GetType().Name)));
                foreach (Transform child in actor) rows.Add("  " + child.name + " | skins=" + child.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length + " | meshes=" + child.GetComponentsInChildren<MeshRenderer>(true).Length);
            }
            Directory.CreateDirectory("ClientExports/YanYana/Reports"); File.WriteAllLines("ClientExports/YanYana/Reports/resident-workshop-roots.txt", rows);
        }

        [MenuItem("Tools/Yan Yana/Art/Install Resident Workshop In Park")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != YanYanaAdventureBuilder.ScenePath) throw new InvalidOperationException("Open YanYana_Adventure first.");
            var actors = Residents().Where(a => Names.Contains(Identity(a.name))).ToArray();
            if (actors.Length != Names.Length) throw new InvalidOperationException("Expected eight corresponding resident roots.");
            foreach (var actor in actors)
            {
                string name = Identity(actor.name); var prefab = Prefab(name);
                if (!prefab) throw new FileNotFoundException(name);
                if (PrefabUtility.IsPartOfPrefabInstance(actor)) PrefabUtility.UnpackPrefabInstance(actor.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var oldAnimators = actor.GetComponentsInChildren<Animator>(true);
                var branches = new HashSet<Transform>();
                foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    AddBranch(skin.transform); foreach (var bone in skin.bones) if (bone) AddBranch(bone);
                }
                void AddBranch(Transform target)
                {
                    if (target == actor) return;
                    while (target.parent && target.parent != actor) target = target.parent;
                    if (target.parent == actor) branches.Add(target);
                }
                // Only old model/skeleton branches are replaced. Story roots and scene accessories retain identity.
                foreach (var branch in branches) UnityEngine.Object.DestroyImmediate(branch.gameObject);
                actor.localScale = Vector3.one;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, actor);
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); visual.transform.localScale = Vector3.one;
                var animator = visual.GetComponentInChildren<Animator>();
                foreach (var old in oldAnimators.Where(a => a))
                {
                    foreach (var component in actor.GetComponents<Component>().Where(c => c && c != old))
                    {
                        var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                        while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == old) property.objectReferenceValue = animator;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    UnityEngine.Object.DestroyImmediate(old);
                }
                var mover = actor.GetComponent<StoryPlayerMovement>();
                if (mover) { var so = new SerializedObject(mover); so.FindProperty("animator").objectReferenceValue = animator; so.ApplyModifiedPropertiesWithoutUndo(); }
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Update(0);
                Ground(visual, actor.position.y + .004f);
                var hit = actor.GetComponent<CapsuleCollider>();
                if (hit) { hit.height = IsChild(name) ? 1.25f : 1.75f; hit.center = Vector3.up * hit.height * .5f; }
            }
            YanYanaAdventureBuilder.AuthorWorkshopFaces();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            InspectRoots(); Debug.Log("RESIDENT_WORKSHOP_INSTALLED: eight visuals replaced, 22 resident story roots retained.");
        }

        [MenuItem("Tools/Yan Yana/Art/Update Saved Park Residents")]
        public static void UpdateSavedPark()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing.");
            var previous = SceneManager.GetActiveScene();
            var park = SceneManager.GetSceneByPath(YanYanaAdventureBuilder.ScenePath);
            bool opened = !park.IsValid() || !park.isLoaded;
            if (opened) park = EditorSceneManager.OpenScene(YanYanaAdventureBuilder.ScenePath, OpenSceneMode.Additive);
            try { SceneManager.SetActiveScene(park); Install(); }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && park.IsValid() && park.isLoaded) EditorSceneManager.CloseScene(park, true);
            }
        }

        public static void Ground(GameObject visual, float y)
        {
            var mesh = new Mesh(); float lowest = float.PositiveInfinity, highest = float.NegativeInfinity;
            try
            {
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    skin.BakeMesh(mesh); foreach (var vertex in mesh.vertices) { float h = skin.transform.TransformPoint(vertex).y; lowest = Mathf.Min(lowest, h); highest = Mathf.Max(highest, h); }
                }
                if (highest - lowest < .6f || highest - lowest > 2.8f) throw new InvalidOperationException("Unexpected resident dimensions: " + visual.name + " height=" + (highest - lowest));
                visual.transform.position += Vector3.up * (y - lowest);
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
    }

    public static partial class YanYanaAdventureBuilder
    {
        public static void AuthorWorkshopFaces()
        {
            var scene = SceneManager.GetActiveScene(); var root = scene.GetRootGameObjects().First(x => x.name.StartsWith("01 Akış"));
            var voice = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<AudioSource>(true)).First(a => a.name == "Kısa Türkçe konuşmalar");
            var lines = YanYanaResidentWorkshop.ReadSpeech().lines;
            foreach (var resident in YanYanaResidentWorkshop.Residents())
            {
                var actor = resident.gameObject; string identity = YanYanaResidentWorkshop.Identity(actor.name);
                if (!YanYanaResidentWorkshop.Names.Contains(identity)) continue;
                var skin = YanYanaResidentWorkshop.Face(actor);
                foreach (var old in actor.GetComponents<ScriptMachine>().Where(m => m.graph?.title == YanYanaResidentWorkshop.FaceGraph).ToArray()) UnityEngine.Object.DestroyImmediate(old);
                var g = new YanYanaGraphAuthor(actor, YanYanaResidentWorkshop.FaceGraph); var tick = g.Add(new Unity.VisualScripting.LateUpdate()); var p = tick.trigger;
                ControlOutput Shape(ControlOutput before, string shape, object value) => g.Do(before, typeof(SkinnedMeshRenderer), "SetBlendShapeWeight", skin, new[] { typeof(int), typeof(float) }, skin.sharedMesh.GetBlendShapeIndex(shape), value);
                foreach (string shape in new[] { "Mouth_A", "Mouth_E", "Mouth_O" }) p = Shape(p, shape, 0f);
                float seed = Array.IndexOf(YanYanaResidentWorkshop.Names, identity) * .29f;
                var cycle = g.Call(typeof(Mathf), "Repeat", null, new[] { typeof(float), typeof(float) }, Sum(g, g.Get(typeof(Time), "time"), seed), 4.3f).result;
                var rise = g.Call(typeof(Mathf), "InverseLerp", null, new[] { typeof(float), typeof(float), typeof(float) }, 3.92f, 4.04f, cycle).result;
                var fall = g.Call(typeof(Mathf), "InverseLerp", null, new[] { typeof(float), typeof(float), typeof(float) }, 4.20f, 4.04f, cycle).result;
                p = Shape(p, "Blink", g.Binary<ScalarMultiply>(g.Binary<ScalarMultiply>(rise, fall), 100f));
                var quake = Or(g, Is(g, g.Var("Phase", root), 1), Is(g, g.Var("Phase", root), 31)); object worried = quake;
                if (identity == "Ece") worried = Or(g, quake, And(g, Is(g, g.Var("ParkChildHelped", root), 0), Is(g, g.Var("ParkChildNoticed", root), 1)));
                p = Shape(p, "Fear", g.Binary<ScalarMultiply>(g.Call(typeof(Convert), "ToSingle", null, OneBool, worried).result, identity == "Ece" ? 60f : 48f));
                var surprised = identity == "Eren" ? And(g, Is(g, g.Var("GasNoticed", root), 1), Is(g, g.Var("GasRetreated", root), 0)) : And(g, Is(g, g.Var("ParkChildAsked", root), 1), Is(g, g.Var("ParkChildHelped", root), 0));
                p = Shape(p, "Surprise", g.Binary<ScalarMultiply>(g.Call(typeof(Convert), "ToSingle", null, OneBool, surprised).result, identity == "Eren" ? 45f : identity == "Aylin" ? 35f : 0f));
                p = Shape(p, "Smile", g.Binary<ScalarMultiply>(g.Call(typeof(Convert), "ToSingle", null, OneBool, And(g, Is(g, g.Var("ParkChildHelped", root), 1), Is(g, worried, false))).result, identity == "Ece" || identity == "Aylin" ? 65f : 22f));
                var matching = lines.Where(l => l.actor == identity).ToArray();
                if (matching.Length > 0)
                {
                    var playing = g.Branch(p, g.Get(typeof(AudioSource), "isPlaying", voice));
                    var sw = g.Add(new SwitchOnString { options = matching.Select(l => l.audio).ToList() });
                    g.Bind(sw.selector, g.Get(typeof(UnityEngine.Object), "name", g.Get(typeof(AudioSource), "clip", voice))); g.Link(playing.ifTrue, sw.enter);
                    for (int i = 0; i < matching.Length; i++)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(YanYanaResidentWorkshop.Root + "/Speech/" + matching[i].audio + "_" + identity + ".anim");
                        g.Do(sw.branches[i].Value, typeof(AnimationClip), "SampleAnimation", clip, new[] { typeof(GameObject), typeof(float) }, skin.gameObject, g.Get(typeof(AudioSource), "time", voice));
                    }
                }
                g.Dirty();
            }
        }
    }
}
