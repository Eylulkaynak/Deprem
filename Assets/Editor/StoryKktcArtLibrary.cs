using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Editor-only art preparation and repeatable Rebuild scene migration.</summary>
public static class StoryKktcArtLibrary
{
    public const string Root = "Assets/Story/Art/KKTC";
    public static readonly string[] Scenes = Enumerable.Range(1, 4)
        .Select(i => $"Assets/Scenes/Story_{i:00}_RebuildPreview.unity").ToArray();
    private const string VisualName = "KKTC_AuthoredVisual";
    private static readonly Dictionary<string, (string hex, float rough, float metal)> Palette = new()
    {
        ["CanvasTeal"] = ("347E7F", .77f, 0), ["CanvasDeep"] = ("235456", .82f, 0),
        ["Lining"] = ("233B45", .92f, 0), ["Webbing"] = ("D7B987", .88f, 0),
        ["Stitch"] = ("E6D9BE", .83f, 0), ["Rubber"] = ("26353C", .76f, 0),
        ["WarmWhite"] = ("F3EAD8", .63f, 0), ["Terracotta"] = ("B76749", .8f, 0),
        ["Ochre"] = ("DDAA53", .65f, 0), ["Metal"] = ("B0B9B8", .3f, .7f),
        ["Brass"] = ("C4A36A", .35f, .65f), ["Lens"] = ("A6D9D7", .17f, .2f),
        ["Paper"] = ("EEE0BA", .85f, 0), ["BlueInk"] = ("274657", .75f, 0),
        ["Water"] = ("8FC6CE", .3f, .08f), ["Willow"] = ("B89461", .82f, 0),
        ["WillowLight"] = ("D8B57D", .85f, 0), ["Olive"] = ("657750", .8f, 0),
        ["Wood"] = ("875B3C", .74f, 0), ["Ceramic"] = ("ECE2CC", .24f, 0),
        ["Coffee"] = ("32281F", .5f, 0), ["SkinTape"] = ("DBB88F", .9f, 0)
    };

    [MenuItem("Tools/Deprem Story/KKTC/1 Prepare Art Library")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Meshes");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var pair in Palette)
        {
            string path = Root + "/Materials/KKTC_" + pair.Key + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.name = "KKTC_" + pair.Key;
            ColorUtility.TryParseHtmlString("#" + pair.Value.hex, out Color color);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 1f - pair.Value.rough);
            mat.SetFloat("_Metallic", pair.Value.metal);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }
        foreach (string path in Directory.GetFiles(Root + "/Models", "*.fbx"))
        {
            string normalized = path.Replace('\\', '/');
            ModelImporter importer = AssetImporter.GetAtPath(normalized) as ModelImporter;
            if (importer != null && (importer.importAnimation || importer.importCameras || importer.importLights || !importer.isReadable))
            {
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(normalized);
            if (model == null)
                throw new InvalidOperationException("KKTC FBX import failed: " + normalized);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = Path.GetFileNameWithoutExtension(path);
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                    {
                        string key = m != null ? m.name.Replace(" (Instance)", "") : "KKTC_WarmWhite";
                        return AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + key + ".mat")
                               ?? Material("WarmWhite");
                    }).ToArray();
                BakePaletteMesh(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/" + instance.name + ".prefab");
            }
            finally { Object.DestroyImmediate(instance); }
        }
        AssetDatabase.SaveAssets();
        CreateSwitchController();
        Debug.Log("KKTC_ART_LIBRARY_READY");
    }

    private static void BakePaletteMesh(GameObject instance)
    {
        // A colour/metallic atlas keeps the authored palette while reducing each static
        // prop to one renderer. Controls remain separate for authored animation.
        string[] keys=Palette.Keys.ToArray();
        Texture2D colors=new(8,8,TextureFormat.RGBA32,false);
        Texture2D surface=new(8,8,TextureFormat.RGBA32,false,true);
        for(int i=0;i<64;i++)
        {
            var value=Palette[keys[Mathf.Min(i,keys.Length-1)]];
            ColorUtility.TryParseHtmlString("#"+value.hex,out Color color);
            colors.SetPixel(i%8,i/8,color); surface.SetPixel(i%8,i/8,new(value.metal,value.metal,value.metal,1-value.rough));
        }
        colors.Apply();surface.Apply();colors.filterMode=surface.filterMode=FilterMode.Point;
        colors.wrapMode=surface.wrapMode=TextureWrapMode.Clamp;
        var baseMap=SaveArtAsset(colors,Root+"/Materials/PropPalette.asset");
        var metalMap=SaveArtAsset(surface,Root+"/Materials/PropSurface.asset");
        string materialPath=Root+"/Materials/KKTC_PropAtlas.mat";
        Material atlas=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(atlas==null){atlas=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(atlas,materialPath);}
        atlas.SetColor("_BaseColor",Color.white);atlas.SetTexture("_BaseMap",baseMap);atlas.SetTexture("_MetallicGlossMap",metalMap);
        atlas.SetFloat("_Smoothness",1);atlas.EnableKeyword("_METALLICSPECGLOSSMAP");atlas.enableInstancing=true;EditorUtility.SetDirty(atlas);
        var staticParts=new List<CombineInstance>();var temporary=new List<Mesh>();int part=0;
        foreach(MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter=renderer.GetComponent<MeshFilter>();if(filter==null || filter.sharedMesh==null)continue;
            bool control=renderer.name=="PowerSwitch" || renderer.name=="BatteryCover";
            var parts=new List<CombineInstance>();
            for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
            {
                Mesh copy=Object.Instantiate(filter.sharedMesh);temporary.Add(copy);
                string key=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)].name.Replace("KKTC_","");
                int index=Mathf.Max(0,Array.IndexOf(keys,key));
                copy.uv=Enumerable.Repeat(new Vector2((index%8+.5f)/8,(index/8+.5f)/8),copy.vertexCount).ToArray();
                var entry=new CombineInstance{mesh=copy,subMeshIndex=sub,transform=control?Matrix4x4.identity:instance.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix};
                if(control)parts.Add(entry);else staticParts.Add(entry);
            }
            if(control)
            {
                Mesh mesh=new(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);
                filter.sharedMesh=SaveArtAsset(mesh,Root+"/Meshes/"+instance.name+"_Control_"+part+++".asset");
                renderer.sharedMaterial=atlas;
            }
            else {renderer.enabled=false;filter.sharedMesh=null;}
        }
        Mesh combined=new(){name=instance.name+"_Surface",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        combined.CombineMeshes(staticParts.ToArray(),true,true);
        GameObject body=new("KKTC_StaticSurface");body.transform.SetParent(instance.transform,false);
        body.AddComponent<MeshFilter>().sharedMesh=SaveArtAsset(combined,Root+"/Meshes/"+instance.name+"_Surface.asset");
        body.AddComponent<MeshRenderer>().sharedMaterial=atlas;
        foreach(Mesh mesh in temporary)Object.DestroyImmediate(mesh);
    }

    private static T SaveArtAsset<T>(T value,string path) where T:Object
    {
        T saved=AssetDatabase.LoadAssetAtPath<T>(path);
        if(saved==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(value);return saved;
    }

    [MenuItem("Tools/Deprem Story/KKTC/2 Apply Props To Rebuild Scenes")]
    public static void ApplyProps()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        string previous = SceneManager.GetActiveScene().path;
        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ApplyPropsToScene(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (File.Exists(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("KKTC_REBUILD_PROPS_APPLIED");
    }

    public static void ApplyPropsToScene(Scene scene)
    {
        if (!Scenes.Contains(scene.path) && !scene.name.Contains("Rebuild")) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Backpack_Open.prefab") == null) return;
        Transform[] all = Transforms(scene);
        List<string> changes = new();
        HashSet<Transform> updated = new();
        string[] bagNames = { "EmergencyBag_Open_Packing", "EmergencyBag_Worn", "EmergencyBag_ExitShelf",
            "EmergencyBag_HeavySlumped", "Deniz_WornEmergencyBag", "EmergencyBag" };
        foreach (Transform bag in all.Where(t => t != null && bagNames.Contains(t.name)).ToArray())
        {
            bool opened = bag.name.Contains("Open") || bag.name.Contains("Slumped");
            bool worn = bag.name.Contains("Worn");
            ReplaceVisual(bag, opened ? "Backpack_Open" : "Backpack_Closed", .46f, true);
            if (worn)
            {
                Animator actor = bag.GetComponentInParent<Animator>(true);
                Transform visual = bag.Find(VisualName);
                Transform carry = Descendant(visual, "BackCarryAnchor");
                if (actor != null && actor.isHuman && carry != null)
                {
                    Transform chest = actor.GetBoneTransform(HumanBodyBones.Chest) ?? actor.GetBoneTransform(HumanBodyBones.Spine);
                    // The imported Visual has a 180-degree forward correction. The
                    // navigation root, rather than the Animator, defines the person's front.
                    Transform person = actor.transform.parent != null ? actor.transform.parent : actor.transform;
                    visual.rotation = Quaternion.LookRotation(-person.forward, Vector3.up);
                    if (chest != null) bag.position += chest.position - person.forward * .095f - Vector3.up * .09f - carry.position;
                }
            }
            updated.Add(bag);
            changes.Add(bag.name + " -> Backpack " + (opened ? "open" : "closed"));
        }
        foreach (Transform candidate in all)
        {
            if (candidate == null || HasAncestorIn(candidate, updated) || IsArt(candidate)) continue;
            GameObject instance = PrefabUtility.GetNearestPrefabInstanceRoot(candidate.gameObject);
            if (instance == null || instance.transform != candidate) continue;
            string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance).Replace('\\', '/').ToLowerInvariant();
            string model = ModelForSource(source);
            if (model == null) continue;
            // A previous run keeps the original instance only as an interaction/animation anchor.
            ReplaceVisual(candidate, model, 0, false);
            updated.Add(candidate);
            changes.Add(candidate.name + " -> " + model);
        }
        foreach (Transform candidate in all)
        {
            if (candidate == null || IsArt(candidate) || updated.Contains(candidate) || HasAncestorIn(candidate, updated)) continue;
            string model = candidate.name switch
            {
                "Packed_Flashlight" => "Flashlight", "Packed_Radio" => "Radio",
                "Packed_Batteries" => "Batteries", "Packed_Whistle" => "Whistle",
                "Packed_Water" => "WaterBottle", "Packed_Food" => "FoodCan",
                "Packed_FirstAid" => "FirstAid", "Packed_Documents" => "Documents",
                "Packed_Blanket" => "Blanket", "Packed_Clothes" => "Clothes",
                "WorldItem_FirstAid" or "Assembly_FirstAidKit" or "FirstAidPrepared_Drag" => "FirstAid",
                "SealedBandagePackage" or "BandageInspectionPackage" or "BandageSeal_Unchecked" => "Bandage",
                "ExitShoes_Start" or "DenizShoePair_Drag" => "ShoePair",
                "ExitShoes_Stored" => "ShoePair",
                "NeighborCane_Blocked" => "WalkingCane",
                "NerminEnvelope_Start" or "Nermin_EvacuationPlan_InHand" => "Documents",
                "ShelfBracketInHand" => "SafetyBracket",
                "WardrobeHandStrap" => "SafetyStrap",
                "Can_ToyCar_FinalStart" or "Can_ToyCar_InitialRoute" => "ToyCar",
                "StationWaterCup_Fallback_Drag" => "Cup",
                "CleanCloth_Fallback_Drag" => "Clothes",
                _ => null
            };
            if (model == null) continue;
            ReplaceVisual(candidate, model, 0, false);
            updated.Add(candidate);
            changes.Add(candidate.name + " -> " + model);
        }
        NormalizeItemScales(scene);
        RepairPreparationHardware(scene);
        RepairBagAnchors(scene);
        foreach(Transform t in Transforms(scene))
        {
            if(t.name=="Story01_ShelfStack_Lower" || t.name=="Story01_ShelfStack_Top")
            {
                Transform visual=t.Find(VisualName);if(visual==null)continue;
                Bounds b=BoundsOf(visual);float bottom=b.min.y;
                visual.localScale*=.115f/Mathf.Max(.001f,b.size.y);
                visual.position+=Vector3.up*(bottom-BoundsOf(visual).min.y);
            }
            if(t.name=="KKTC_AuthoredVisual" && (t.parent.name.Contains("Shoes") || t.parent.name=="DenizShoePair_Drag" || t.parent.name.Contains("Parcel")))
            {
                Bounds b=BoundsOf(t);float max=t.parent.name.Contains("Parcel")?.42f:.345f;
                float factor=Mathf.Min(1,max/Mathf.Max(b.size.x,b.size.z));
                t.localScale*=factor;t.position+=Vector3.up*(b.min.y-BoundsOf(t).min.y);
            }
            if(t.name.StartsWith("DrawerItem_") && t.GetComponent<StoryInteractable>() is StoryInteractable item && item.HighlightRoot!=null)
            {
                Bounds b=BoundsOf(t);var point=item.HighlightRoot.transform.position;
                item.HighlightRoot.transform.position=new(point.x,b.max.y+.12f,point.z);
            }
        }
        Directory.CreateDirectory("ClientExports/KKTC/Reports");
        File.WriteAllLines("ClientExports/KKTC/Reports/" + scene.name + "_PropMigration.txt", changes);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static bool HasAncestorIn(Transform value, HashSet<Transform> candidates)
    {
        if (InPackedResult(value)) return false;
        for (Transform parent = value.parent; parent != null; parent = parent.parent)
            if (candidates.Contains(parent)) return true;
        return false;
    }

    private static void SetWorldScale(Transform visual,float scale)
    {
        Vector3 current=visual.lossyScale;
        visual.localScale=Vector3.Scale(visual.localScale,new Vector3(scale/Mathf.Max(.00001f,Mathf.Abs(current.x)),
            scale/Mathf.Max(.00001f,Mathf.Abs(current.y)),scale/Mathf.Max(.00001f,Mathf.Abs(current.z))));
    }

    private static void NormalizeItemScales(Scene scene)
    {
        foreach(Transform visual in Transforms(scene).Where(t=>t.name==VisualName))
        {
            Transform item=visual.GetComponentsInParent<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("WorldItem_") || t.name=="BagReview_EmergencyRadio");
            if(item==null || InPackedResult(visual))continue;
            Bounds before=BoundsOf(visual);
            SetWorldScale(visual,item.name=="WorldItem_Whistle"?.8f:1f);
            Bounds after=BoundsOf(visual);
            float bottom=scene.name.Contains("01")?.784f:before.min.y;
            visual.position+=new Vector3(before.center.x-after.center.x,bottom-after.min.y,before.center.z-after.center.z);
        }
    }

    private static bool InPackedResult(Transform value)
    {
        for (Transform parent = value; parent != null; parent = parent.parent)
            if (parent.name.StartsWith("Packed_", StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool IsArt(Transform value)
    {
        for (Transform p = value; p != null; p = p.parent)
            if (p.name == VisualName || p.name.StartsWith("KKTC_")) return true;
        return false;
    }

    private static string ModelForSource(string source)
    {
        if (source.StartsWith(Root.ToLowerInvariant())) return null;
        if (source.Contains("item_fener")) return "Flashlight";
        if (source.EndsWith("/radio.prefab")) return "Radio";
        if (source.Contains("item_pil")) return "Batteries";
        if (source.Contains("item_su")) return "WaterBottle";
        if (source.Contains("konserve")) return "FoodCan";
        if (source.Contains("dockument")) return "Documents";
        if (source.Contains("whistle.prefab")) return "Whistle";
        if (source.Contains("bedroll-packed")) return "Blanket";
        if (source.EndsWith("/toy_02.prefab")) return "ToyCar";
        if (source.Contains("t-shirt.prefab")) return "Clothes";
        if (source.Contains("gameconsole_01")) return "Console";
        if (source.Contains("camsise")) return "GlassBottle";
        if (source.Contains("item_tava")) return "Pan";
        if (source.Contains("cardboardbox_01")) return "Parcel";
        if (source.Contains("book_08.prefab")) return "BookStack";
        if (source.Contains("plants_05.prefab")) return "OliveVase";
        return null;
    }

    public static Transform ReplaceVisual(Transform target, string model, float height, bool upright)
    {
        string prefabPath = Root + "/Prefabs/" + model + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;
        Transform old = target.Find(VisualName);
        bool hadPreviousVisual = old != null;
        Bounds before = BoundsOf(target);
        if (before.size.sqrMagnitude < .000001f) before = BoundsOf(target, true);
        Quaternion rotation = Quaternion.Euler(0, target.eulerAngles.y, 0);
        if (model == "WalkingCane") rotation = target.rotation;
        Vector3 position = new(before.center.x, before.min.y, before.center.z);
        // Store placement in the authored visual; repeated migration must not shrink an asset.
        Vector3 previousScale = old != null ? old.lossyScale : Vector3.zero;
        if (old != null)
        {
            position = old.position;
            rotation = old.rotation;
            Object.DestroyImmediate(old.gameObject);
        }
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (IsMarker(renderer.transform) || (!InPackedResult(target) && InPackedResult(renderer.transform))) continue;
            renderer.enabled = false;
        }
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target);
        visual.name = VisualName;
        visual.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        visual.transform.localScale = Vector3.one;
        // Cancel legacy model scales such as (20,20,16); the replacement has metre geometry.
        Vector3 parentScale = target.lossyScale;
        visual.transform.localScale = new Vector3(1f / Mathf.Max(.00001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(.00001f, Mathf.Abs(parentScale.y)), 1f / Mathf.Max(.00001f, Mathf.Abs(parentScale.z)));
        Bounds natural = BoundsOf(visual.transform);
        float scale;
        if (height > 0) scale = height / Mathf.Max(.001f, natural.size.y);
        else if (previousScale != Vector3.zero) scale = previousScale.x;
        else
        {
            float targetExtent = Mathf.Max(before.size.x, before.size.y, before.size.z);
            float naturalExtent = Mathf.Max(natural.size.x, natural.size.y, natural.size.z);
            // Small handheld assets keep their authored scale and remain easy to inspect.
            scale = InPackedResult(target)
                ? Mathf.Clamp(targetExtent / Mathf.Max(.001f, naturalExtent), .05f, .75f)
                : Mathf.Clamp(targetExtent / Mathf.Max(.001f, naturalExtent), .65f, 1.6f);
        }
        visual.transform.localScale *= scale;
        visual.transform.rotation = rotation;
        Bounds placed = BoundsOf(visual.transform);
        if (!hadPreviousVisual)
            visual.transform.position += position - new Vector3(placed.center.x, placed.min.y, placed.center.z);
        else visual.transform.position = position;
        return visual.transform;
    }

    private static bool IsMarker(Transform t) => t.GetComponentInParent<TMPro.TMP_Text>() != null ||
        t.name.Contains("Marker") || t.name.Contains("Indicator") || t.name.Contains("Highlight") ||
        t.name.Contains("Arrow") || t.name.Contains("Gesture") || t.name.Contains("Label");

    public static Bounds BoundsOf(Transform root, bool includeDisabled = false)
    {
        bool found = false;
        Bounds bounds = new(root.position, Vector3.zero);
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if ((!includeDisabled && !renderer.enabled) || IsMarker(renderer.transform)) continue;
            Bounds local = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (!found) { bounds = new Bounds(corner, Vector3.zero); found = true; }
                else bounds.Encapsulate(corner);
            }
        }
        return bounds;
    }

    private static void RepairPreparationHardware(Scene scene)
    {
        if (!scene.name.Contains("01")) return;
        Transform[] all = Transforms(scene);
        Transform flashlight = Find(all, "WorldItem_Flashlight");
        Transform switchAnchor = Descendant(flashlight, "SwitchAnchor");
        Transform switchMesh = Descendant(flashlight, "PowerSwitch");
        foreach (string name in new[] { "FlashlightTopSwitch", "FlashlightTopSwitch_OffState" })
        {
            Transform hotspot = Find(all, name);
            if (hotspot != null && switchAnchor != null) hotspot.position = switchAnchor.position;
        }
        Transform beamAnchor = Descendant(flashlight, "BeamAnchor");
        Transform beam = Find(all, "FlashlightInspectionBeam");
        if (beam != null && beamAnchor != null)
        {
            beam.position = beamAnchor.position;
            Transform visual = flashlight.GetComponentsInChildren<Transform>(true).First(t => t.name == VisualName);
            beam.rotation = Quaternion.LookRotation(-visual.right, Vector3.up);
        }
        if (switchMesh != null)
        {
            RuntimeAnimatorController switchController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "/FlashlightSwitch.controller");
            Animator animator = switchMesh.GetComponent<Animator>();
            if (animator == null) animator = switchMesh.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = switchController;
            foreach (var entry in new[] { ("FlashlightTopSwitch", "On"), ("FlashlightTopSwitch_OffState", "Off") })
            {
                StoryInteractable interaction = Find(all, entry.Item1)?.GetComponent<StoryInteractable>();
                if (interaction != null)
                    for (int i = interaction.OnInteracted.GetPersistentEventCount() - 1; i >= 0; i--)
                        if (interaction.OnInteracted.GetPersistentTarget(i) == null &&
                            interaction.OnInteracted.GetPersistentMethodName(i) == "SetTrigger")
                            UnityEventTools.RemovePersistentListener(interaction.OnInteracted, i);
                if (interaction != null && !HasListener(interaction, animator, "SetTrigger"))
                    UnityEventTools.AddStringPersistentListener(interaction.OnInteracted, animator.SetTrigger, entry.Item2);
            }
        }
        Transform radio = Find(all, "BagReview_EmergencyRadio");
        Transform looseRoot=Find(all,"WorldItem_Batteries");
        if(looseRoot!=null)looseRoot.position=new Vector3(1.4f,.81f,.38f);
        AnimationClip stageClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Generated/Animations/Chapters/Story01_SignalStage_Batteries.anim");
        if(stageClip!=null && looseRoot!=null)
        {
            foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(stageClip))
            {
                if(binding.path!="" || !binding.propertyName.Contains("LocalPosition."))continue;
                AnimationCurve curve=AnimationUtility.GetEditorCurve(stageClip,binding);var keys=curve.keys;
                int axis=binding.propertyName.EndsWith(".x")?0:binding.propertyName.EndsWith(".y")?1:2;
                float difference=looseRoot.localPosition[axis]-keys[keys.Length-1].value;
                for(int i=0;i<keys.Length;i++)keys[i].value+=difference*(keys[i].time/keys[keys.Length-1].time);
                curve.keys=keys;AnimationUtility.SetEditorCurve(stageClip,binding,curve);
            }
            EditorUtility.SetDirty(stageClip);
        }
        Transform storage=Find(all,"Review_RadioBatteryStorageDropZone");
        if(storage!=null && looseRoot!=null)storage.position=looseRoot.position;
        Transform socket = Descendant(radio, "BatterySocket");
        Transform slot = Find(all, "Review_RadioBatterySlot");
        if (slot != null && socket != null) slot.SetPositionAndRotation(socket.position, socket.rotation);
        Transform battery = Find(all, "Review_RadioBatteryInserted");
        Transform batteryVisual = Descendant(battery, VisualName);
        Transform radioVisual = Descendant(radio, VisualName);
        if (batteryVisual != null && radioVisual != null)
        {
            float desiredScale = radioVisual.lossyScale.x;
            batteryVisual.rotation = battery.rotation;
            SetWorldScale(batteryVisual,desiredScale);
            Transform insert = Descendant(batteryVisual, "InsertAnchor");
            if (insert != null) batteryVisual.position += battery.position - insert.position;
        }
        Transform radioSwitch = Find(all, "Review_RadioPowerButton");
        Transform radioSwitchAnchor = Descendant(radio, "SwitchAnchor");
        if (radioSwitch != null && radioSwitchAnchor != null) radioSwitch.position = radioSwitchAnchor.position;
        StoryInteractable radioInteraction=radioSwitch?.GetComponent<StoryInteractable>();
        if(radioInteraction!=null && radioInteraction.HighlightRoot!=null)
            radioInteraction.HighlightRoot.transform.position=radioSwitchAnchor.position+Vector3.up*.07f;
        Transform oldBay = Find(all, "RadioBatteryBay");
        if (oldBay != null)
            foreach (Renderer renderer in oldBay.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        Transform water=Find(all,"WorldItem_Water");
        Transform waterVisual=Descendant(water,VisualName);
        if(waterVisual!=null)
        {
            Bounds bottle=BoundsOf(waterVisual);Vector3 outward=Quaternion.Euler(0,150,0)*Vector3.right;
            Vector3 label=new Vector3(bottle.center.x,bottle.min.y+.083f,bottle.center.z)+outward*.042f;
            foreach(string name in new[]{"WaterExpiryLabel_Unchecked","WaterExpiryCheckedState"})
            {
                Transform part=Find(all,name);if(part==null)continue;
                part.position=label;SetWorldScale(part,.6f);
            }
            Transform hotspot=Find(all,"WaterInspect_Hotspot");
            var interaction=hotspot?.GetComponent<StoryInteractable>();
            if(interaction?.HighlightRoot!=null)interaction.HighlightRoot.transform.position=bottle.center+Vector3.up*(bottle.extents.y+.065f);
            Transform swipe=Find(all,"WaterExpirySwipeTarget");if(swipe!=null)swipe.position=bottle.center-new Vector3(0,0,.22f);
        }
    }

    private static bool HasListener(StoryInteractable item, Object target, string method)
    {
        for (int i = 0; i < item.OnInteracted.GetPersistentEventCount(); i++)
            if (item.OnInteracted.GetPersistentTarget(i) == target && item.OnInteracted.GetPersistentMethodName(i) == method)
                return true;
        return false;
    }

    private static RuntimeAnimatorController CreateSwitchController()
    {
        const string path = Root + "/FlashlightSwitch.controller";
        var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (existing != null) return existing;
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("On", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Off", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        foreach (string name in new[] { "Off", "On" })
        {
            AnimationClip clip = new() { name = "FlashlightSwitch_" + name };
            // Scale around the model's own origin; no runtime animation script is required.
            clip.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, name == "On" ? .55f : 1f,
                .12f, name == "On" ? .55f : 1f));
            AssetDatabase.CreateAsset(clip, Root + "/FlashlightSwitch_" + name + ".anim");
            var state = machine.AddState(name);
            state.motion = clip;
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = .06f;
            transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, name);
            if (name == "Off") machine.defaultState = state;
        }
        return controller;
    }

    private static void RepairBagAnchors(Scene scene)
    {
        Transform[] all = Transforms(scene);
        Transform bag = Find(all, "EmergencyBag_Open_Packing");
        Transform mouth = Descendant(bag, "MouthAnchor");
        Transform inside = Descendant(bag, "InsideAnchor");
        if (mouth == null) return;
        // Move the parent drop volume first; moving it after its child opening
        // would apply the displacement twice on a fresh rebuild.
        foreach (string name in new[] { "PhysicalBagOpening", "BagOpeningPoint" })
        {
            Transform point = Find(all, name);
            if (point != null) point.position = mouth.position;
        }
        Transform insidePoint = Find(all, "BagInsidePoint");
        if (insidePoint != null && inside != null) insidePoint.position = inside.position;
        int index = 0;
        foreach (Transform packed in bag.GetComponentsInChildren<Transform>(true)
                     .Where(t => t.name.StartsWith("Packed_", StringComparison.Ordinal)))
        {
            Transform visual = packed.Find(VisualName);
            if (visual == null) continue;
            // Full-size packed items: orient flat fabric/documents upright against the
            // lining, and place small equipment in the remaining front compartment.
            string kind=packed.name.Replace("Packed_","");
            Transform loose=Find(all,"WorldItem_"+kind);
            Transform looseVisual=Descendant(loose,VisualName);
            float scale=looseVisual!=null?looseVisual.lossyScale.x:1f;
            visual.rotation=bag.rotation;
            if(kind=="Blanket" || kind=="Clothes" || kind=="Documents")visual.rotation*=Quaternion.Euler(90,0,0);
            if(kind=="Flashlight")visual.rotation*=Quaternion.Euler(0,0,90);
            SetWorldScale(visual,scale);
            Vector3 offset=kind switch
            {
                "Blanket"=>new(0,-.20f,-.015f), "Clothes"=>new(0,-.16f,.052f),
                "Documents"=>new(0,-.10f,-.054f), "Radio"=>new(.02f,-.18f,.014f),
                "Water"=>new(-.09f,-.14f,.015f), "FirstAid"=>new(0,-.085f,.035f),
                "Food"=>new(.09f,-.25f,.01f), "Flashlight"=>new(.105f,-.15f,-.015f),
                "Batteries"=>new(-.08f,-.04f,.025f), "Whistle"=>new(.08f,-.055f,.035f),
                _=>new(0,-.2f,0)
            };
            Bounds bounds = BoundsOf(visual);
            Vector3 seat = mouth.position + bag.TransformDirection(offset);
            // Keep the visible packed results under the rim while retaining item scale.
            seat.y=Mathf.Min(seat.y,mouth.position.y-bounds.extents.y-.025f);
            visual.position += seat - bounds.center;
            index++;
        }
    }

    public static Transform[] Transforms(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
    public static Transform Find(Transform[] all, string name) => all.FirstOrDefault(t => t != null && t.name == name);
    public static Transform Descendant(Transform root, string name) => root == null ? null :
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    public static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/KKTC_" + name + ".mat");

    [MenuItem("Tools/Deprem Story/KKTC/Inspect Authored Models")]
    public static void InspectModels()
    {
        Directory.CreateDirectory("ClientExports/KKTC/Reports");
        List<string> lines = new();
        foreach (string file in Directory.GetFiles(Root + "/Prefabs", "*.prefab"))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\', '/'));
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                lines.Add(prefab.name + " bounds=" + BoundsOf(instance.transform));
                foreach (Transform t in instance.GetComponentsInChildren<Transform>(true).Where(t =>
                             t.name.Contains("Anchor") || t.name.Contains("Socket") || t.name == "PowerSwitch"))
                    lines.Add("  " + t.name + " position=" + t.position.ToString("F4") + " rotation=" + t.eulerAngles);
            }
            finally { Object.DestroyImmediate(instance); }
        }
        File.WriteAllLines("ClientExports/KKTC/Reports/ModelDimensions.txt", lines);
    }
}
