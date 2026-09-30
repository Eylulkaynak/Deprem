// Scene/content authoring. This file is excluded from every player build.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    [Serializable] public class YYAction
    {
        public string label, model, gesture, next, flags, target, effect;
        public bool safe = true;
    }
    [Serializable] public class YYBeat
    {
        public string id, station, title, line, role, camera, condition, yes, no, effect;
        public int chapter;
        public YYAction[] actions;
    }
    [Serializable] public class YYCampaign
    {
        public string title, subtitle;
        public int schemaVersion;
        public string[] chapters, flags, endings;
        public int[] chapterMinutes;
        public YYBeat[] beats;
    }

    public static partial class YanYanaAdventureBuilder
    {
        public const string Root = "Assets/YanYana/";
        public const string ScenePath = Root + "Scenes/YanYana_Adventure.unity";
        public const string SavePrefix = "Deprem.YanYana.v1.";
        static YYCampaign campaign;
        static GameObject flow, world, castRoot, interactions, presentation, uiRoot;
        static Camera camera;
        static CinemachineBrain brain;
        static TMP_FontAsset font;
        static TMP_Text goalText, lineText, chapterText, roleText, gestureText, endingText;
        static Image portraitImage;
        static GameObject pausePanel, titlePanel, endingPanel, rolePanel, hintPanel;
        static TMP_Text roleBannerText, hintText;
        static GameObject darkRoot, shelfDamage, wardrobeDamage, exitBlock, shelfFixed, wardrobeFixed;
        static GameObject bagWorn, comfortWorn, radioReady, familySign, alternativeSign, mainBarricade;
        static Light sun, torch;
        static AudioSource ambience, feedback, speech;
        static Sprite circle, panelSprite;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, GameObject> cast = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, StoryPlayerMovement> movers = new Dictionary<string, StoryPlayerMovement>();
        static readonly Dictionary<string, Vector3> stations = new Dictionary<string, Vector3>();
        static readonly Dictionary<string, GameObject> controllers = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, GameObject> beatWorlds = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, CinemachineCamera> views = new Dictionary<string, CinemachineCamera>();
        static readonly Dictionary<string, FirefighterExtinguishManager> fireManagers = new Dictionary<string, FirefighterExtinguishManager>();
        static readonly Dictionary<string, List<GameObject>> targets = new Dictionary<string, List<GameObject>>();
        static readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
        static readonly string[] Names = { "Ada", "Efe", "Derya", "Emre", "Yusuf", "Idil", "Bora" };
        static StorySiblingFollower follower;
        static MinigameProgressManager progress;
        static AnimatorController actorController;

        // Superseded checklist prototype; retained only as source history.
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave play mode before authoring.");
            if (SceneManager.GetActiveScene().isDirty && SceneManager.GetActiveScene().path != ScenePath && !SceneManager.GetActiveScene().GetRootGameObjects().Any(x => x.name == "01 Akış — native Visual Scripting"))
                throw new InvalidOperationException("The current scene has unsaved work. Save it before building Yan Yana.");
            try
            {
                Directory.CreateDirectory(Root + "Scenes");
                Directory.CreateDirectory(Root + "Art/Materials");
                Directory.CreateDirectory(Root + "Animation");
                Directory.CreateDirectory(Root + "UI/Generated");
                Directory.CreateDirectory("ClientExports/YanYana/Reports");
                campaign = JsonUtility.FromJson<YYCampaign>(File.ReadAllText(Root + "Content/Campaign.json"));
                mats.Clear(); cast.Clear(); movers.Clear(); stations.Clear(); controllers.Clear(); beatWorlds.Clear();
                views.Clear(); targets.Clear(); portraits.Clear(); fireManagers.Clear(); sounds.Clear();
                ImportArt();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                flow = new GameObject("01 Akış — native Visual Scripting");
                world = new GameObject("02 Dünya — özgün mahalle");
                castRoot = new GameObject("03 Karakterler — yeni kadro");
                interactions = new GameObject("04 Etkileşimler — dokun, taşı, dene");
                presentation = new GameObject("05 Sunum — kamera, ışık, ses");
                uiRoot = new GameObject("06 Arayüz ve kayıt");
                CreateOwnFont();
                CreatePresentation();
                CreateWorld();
                CreateCharacters();
                CreateUI();
                BakeNavigation();
                CreateCampaign();
                ConfigurePersistence();
                ConfigureMenus();
                ConfigureWorldState();
                ConfigureAmbientMotion();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
                AssetDatabase.SaveAssets();
                WriteInventory();
                Selection.activeGameObject = flow;
                Debug.Log("YAN YANA BUILD OK: " + campaign.beats.Length + " authored beats / four endings. " + ScenePath);
            }
            catch (Exception e)
            {
                File.WriteAllText("ClientExports/YanYana/Reports/authoring-error.txt", e.ToString());
                Debug.LogException(e);
            }
        }

        static GameObject Group(string name, Transform parent, Vector3 position = default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go;
        }
        static void Serialized(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var s = new SerializedObject(target); var p = s.FindProperty(property);
            if (p == null) throw new InvalidOperationException(target.GetType().Name + "." + property);
            p.objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Number(UnityEngine.Object target, string property, float value)
        {
            var s = new SerializedObject(target); var p = s.FindProperty(property);
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = (int)value; else p.floatValue = value;
            s.ApplyModifiedPropertiesWithoutUndo();
        }
        static void TextValue(UnityEngine.Object target, string property, string value)
        {
            var s = new SerializedObject(target); s.FindProperty(property).stringValue = value; s.ApplyModifiedPropertiesWithoutUndo();
        }
        static Color Hex(string hex) { UnityEngine.ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static void CreateOwnFont()
        {
            Directory.CreateDirectory(Root + "UI/Fonts");
            string path = Root + "UI/Fonts/YanYanaNunito.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!font)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/StoryPlayful/Nunito-SemiBold.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = "Yan Yana Nunito"; AssetDatabase.CreateAsset(font, path);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            string letters = new string(Enumerable.Range(32, 224).Select(i => (char)i).ToArray()) + "ĞğİıŞşÇçÖöÜü’–—…→↗↓✦●♡Ⅱ×";
            font.TryAddCharacters(letters, out string missing);
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font);
            File.WriteAllText("ClientExports/YanYana/Reports/font-glyphs.txt", "Own atlas; unsupported decorative glyphs are replaced: " + missing);
        }
        static string SafeText(string value)
        {
            if (string.IsNullOrEmpty(value) || !font) return value;
            return new string(value.Select(c => char.IsControl(c) || font.HasCharacter(c) ? c : c == '→' ? ':' : c == 'Ⅱ' ? '=' : c == '✦' ? '*' : c == '●' ? 'o' : c == '♡' ? '+' : ' ').ToArray());
        }
        static Material Mat(string name, string hex)
        {
            if (mats.TryGetValue(name, out var existing)) return existing;
            string path = Root + "Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", Hex(hex)); m.SetFloat("_Smoothness", .2f); m.enableInstancing = true;
            mats[name] = m; return m;
        }
        static Mesh softBoxMesh;
        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 scale, Material material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if(type==PrimitiveType.Cube && softBoxMesh)go.GetComponent<MeshFilter>().sharedMesh=softBoxMesh;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static GameObject Model(string name, Transform parent, Vector3 p, float scale = 1, float yaw = 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Art/Models/" + name + ".fbx");
            if (!asset) throw new FileNotFoundException("Missing original model: " + name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var originals = renderer.sharedMaterials;
                for (int j = 0; j < originals.Length; j++)
                    if (originals[j] && mats.TryGetValue(originals[j].name.Replace(" (Instance)", ""), out var converted)) originals[j] = converted;
                renderer.sharedMaterials = originals;
            }
            return go;
        }

        static void ImportArt()
        {
            AssetDatabase.Refresh();
            var importedSoft=AssetDatabase.LoadAllAssetsAtPath(Root+"Art/Models/RoundedUnitBox.fbx").OfType<Mesh>().FirstOrDefault();
            if(importedSoft)
            {
                var copy=UnityEngine.Object.Instantiate(importedSoft);var b=copy.bounds;var vertices=copy.vertices;
                for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((vertices[i].x-b.center.x)/b.size.x,(vertices[i].y-b.center.y)/b.size.y,(vertices[i].z-b.center.z)/b.size.z);
                copy.vertices=vertices;copy.RecalculateBounds();copy.name="YanYanaRoundedUnit";
                string path=Root+"Art/Models/RoundedUnitBox.asset";softBoxMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(softBoxMesh){EditorUtility.CopySerialized(copy,softBoxMesh);UnityEngine.Object.DestroyImmediate(copy);}else {AssetDatabase.CreateAsset(copy,path);softBoxMesh=copy;}
            }
            var palette = new Dictionary<string, string> {
                {"skin","#DBA17B"},{"skinLight","#F1BE96"},{"blush","#D98270"},{"hair","#34231E"},{"hairLight","#5C3B28"},
                {"silver","#A8A59F"},{"cream","#F4E7CD"},{"white","#FEFAED"},{"teal","#318F91"},{"tealDark","#276165"},
                {"coral","#D76E54"},{"navy","#263B50"},{"mustard","#EAB64E"},{"olive","#758165"},{"sand","#BB9B73"},
                {"brown","#604938"},{"black","#232B2F"},{"sole","#E6DDC9"},{"iris","#71412C"},{"pupil","#171F25"},
                {"lime","#D0D960"},{"metal","#9EAAAC"},{"wood","#A5734A"},{"woodDark","#684834"},{"leaf","#628461"},
                {"leafLight","#91AA74"},{"terracotta","#BC7658"},{"blue","#78A8B0"},{"red","#C95543"},{"stone","#E1CBA9"},
                {"water","#85CAD0"},{"glass","#B7D4D2"}
            };
            foreach (var item in palette) Mat("YY_" + item.Key, item.Value);
            Mat("Floor", "#DDD0B4"); Mat("Plaster", "#F1DEC0"); Mat("Road", "#9AA5A0"); Mat("Rug", "#C56851");
            Mat("Ink", "#244D50"); Mat("Paper", "#FFF3DB"); Mat("Glow", "#FFD581"); Mat("Soil", "#8D795E");
            foreach (string name in Names)
            {
                string path = Root + "Art/Models/" + name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.isReadable = false;
                var hd = importer.humanDescription;
                var human = new List<HumanBone>();
                foreach (string bone in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "LeftShoulder", "RightShoulder", "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftFoot", "RightFoot" })
                    human.Add(new HumanBone { boneName = bone, humanName = bone, limit = new HumanLimit { useDefaultValues = true } });
                hd.human = human.ToArray(); hd.armStretch = .05f; hd.legStretch = .05f; hd.upperArmTwist = .5f; hd.lowerArmTwist = .5f; hd.upperLegTwist = .5f; hd.lowerLegTwist = .5f; hd.feetSpacing = 0;
                importer.humanDescription = hd;
                importer.SaveAndReimport();
                string portraitPath = Root + "UI/Portraits/" + name + ".png";
                var ti = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.maxTextureSize = 512;
                ti.SaveAndReimport(); portraits[name] = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            }
            foreach (string path in Directory.GetFiles(Root + "Audio", "*.wav", SearchOption.AllDirectories))
                sounds[Path.GetFileNameWithoutExtension(path)] = AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\', '/'));
            CreateActorController();
            circle = CreateUISprite("TouchCircle", true);
            panelSprite = CreateUISprite("SoftPanel", false);
        }

        static Sprite CreateUISprite(string name, bool round)
        {
            const int size = 128; var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - 63.5f), dy = Mathf.Abs(y - 63.5f);
                float d = round ? Mathf.Sqrt(dx * dx + dy * dy) - 61 : Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx - 37, 0), 2) + Mathf.Pow(Mathf.Max(dy - 37, 0), 2)) - 25;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d)));
            }
            t.Apply(); string path = Root + "UI/Generated/" + name + ".png"; File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t); AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spriteBorder = round ? Vector4.zero : Vector4.one * 32;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void CreateActorController()
        {
            string path = Root + "Animation/AdventureCharacters.controller";
            actorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (actorController) AssetDatabase.DeleteAsset(path);
            actorController = AnimatorController.CreateAnimatorControllerAtPath(path);
            actorController.AddParameter("Speed", AnimatorControllerParameterType.Float);
            actorController.AddParameter("Pose", AnimatorControllerParameterType.Int);
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Animations/Generated/ChildNeutralIdle.anim");
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Animations/Generated/ChildNaturalWalk.anim");
            if (!idle || !walk) throw new InvalidOperationException("Existing motion source clips are missing.");
            var machine = actorController.layers[0].stateMachine;
            var locomotion = machine.AddState("Duruş ve yürüyüş"); machine.defaultState = locomotion;
            var tree = new BlendTree { name = "Yürüyüş hızı", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, actorController); tree.AddChild(idle, 0); tree.AddChild(walk, 1.8f); locomotion.motion = tree;
            var cover = machine.AddState("Çök, korun, tutun");
            cover.motion = CreateAdventureCoverPose();
            var enter = locomotion.AddTransition(cover); enter.hasExitTime = false; enter.duration = .25f; enter.AddCondition(AnimatorConditionMode.Equals, 1, "Pose");
            var leave = cover.AddTransition(locomotion); leave.hasExitTime = false; leave.duration = .35f; leave.AddCondition(AnimatorConditionMode.Equals, 0, "Pose");
            var wave=machine.AddState("Aile işareti");wave.motion=CreateFamilyWavePose();var toWave=locomotion.AddTransition(wave);toWave.hasExitTime=false;toWave.duration=.15f;toWave.AddCondition(AnimatorConditionMode.Equals,2,"Pose");var fromWave=wave.AddTransition(locomotion);fromWave.hasExitTime=true;fromWave.exitTime=1;fromWave.duration=.3f;
        }

        static void CreatePresentation()
        {
            var cam = Group("Ana kamera — tek AudioListener", presentation.transform, new Vector3(5, 8, -11));
            camera = cam.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 6.4f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 55; camera.backgroundColor = Hex("#C9DAD3");
            camera.clearFlags = CameraClearFlags.SolidColor; camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.transform.LookAt(new Vector3(0, 1, 0)); cam.AddComponent<AudioListener>(); cam.AddComponent<PhysicsRaycaster>();
            brain = cam.AddComponent<CinemachineBrain>(); brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .75f);
            var sunObject = Group("Ada ışığı", presentation.transform); sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = Hex("#FFF4E5"); sun.intensity = 1.05f; sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48, -34, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#BCCDC8"); RenderSettings.ambientEquatorColor = Hex("#CAB995"); RenderSettings.ambientGroundColor = Hex("#8E9B90");
            RenderSettings.ambientIntensity = .75f;
            var volume = Group("Sıcak renk ve yumuşak vurgular", presentation.transform).AddComponent<Volume>(); volume.isGlobal = true;
            string volumePath = Root + "Art/YanYanaColor.asset"; var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, volumePath); }
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, profile); }
            tone.mode.Override(TonemappingMode.ACES); volume.sharedProfile = profile;
            RenderSettings.fog = true; RenderSettings.fogColor = Hex("#C9DAD3"); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 34; RenderSettings.fogEndDistance = 53;
            ambience = Group("Mahalle ortamı", presentation.transform).AddComponent<AudioSource>(); ambience.loop = true; ambience.volume = .24f;
            if (sounds.TryGetValue("neighborhood", out var ambient)) { ambience.clip = ambient; ambience.playOnAwake = true; }
            feedback = Group("Dokunma ve etkileşim sesleri", presentation.transform).AddComponent<AudioSource>(); feedback.playOnAwake = false; feedback.volume = .45f;
            speech = Group("Kısa Türkçe konuşmalar", presentation.transform).AddComponent<AudioSource>(); speech.playOnAwake = false; speech.volume = .8f;
            progress = Group("Yeni maceraya ait etkinlik kaydı", flow.transform).AddComponent<MinigameProgressManager>();
            TextValue(progress, "profileFileName", "Deprem.YanYana.v1.activities.json");
            var es = Group("EventSystem — tek giriş yönlendirici", uiRoot.transform); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
        }

        static void BakeNavigation()
        {
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideTileSize = true; surface.tileSize = 128;
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("The adventure has no baked navigation data.");
            string navPath=Root+"Scenes/PhysicalAdventureNavigation.asset";
            var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
            if(existing)
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(surface.navMeshData,existing);
                surface.navMeshData=existing;EditorUtility.SetDirty(existing);surface.AddData();
            }
            else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            // Native agents otherwise enable before NavMeshSurface.OnEnable in standalone players.
            // The authored Start graph enables them after every surface has registered its data.
            foreach(var agent in UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(UnityEngine.AI.NavMesh.SamplePosition(agent.transform.position,out var hit,.6f,UnityEngine.AI.NavMesh.AllAreas))
                    agent.baseOffset=agent.transform.position.y-hit.position.y;
                agent.enabled=false;
            }
            foreach(var mover in UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsInactive.Include,FindObjectsSortMode.None))mover.enabled=false;
            foreach(var sibling in UnityEngine.Object.FindObjectsByType<StorySiblingFollower>(FindObjectsInactive.Include,FindObjectsSortMode.None))sibling.enabled=false;
        }

        static void WriteInventory()
        {
            var all = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var machines = all.SelectMany(x => x.GetComponents<ScriptMachine>()).ToArray();
            File.WriteAllText("ClientExports/YanYana/Reports/scene-inventory.json", "{\n  \"scene\": \"" + ScenePath + "\",\n  \"beats\": " + campaign.beats.Length + ",\n  \"scriptMachines\": " + machines.Length + ",\n  \"graphUnits\": " + machines.Sum(x => x.graph?.units.Count ?? 0) + ",\n  \"transforms\": " + all.Length + ",\n  \"newRuntimeCSharp\": 0\n}");
        }
    }
}
