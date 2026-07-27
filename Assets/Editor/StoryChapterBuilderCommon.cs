using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal static class StoryChapterBuilderCommon
{
    internal const string GeneratedRoot = "Assets/Story/Generated";
    internal const string MaterialRoot = GeneratedRoot + "/Materials";
    internal const string AnimationRoot = GeneratedRoot + "/Animations/Chapters";
    internal const string AudioRoot = GeneratedRoot + "/Audio";
    internal const string LicensedSfxRoot = "Assets/Story/Audio/ThirdParty/rubberduck_sfx100v2";
    internal const string StoryPrefabRoot = "Assets/Story/Prefabs";
    internal const string FurnitureRoot = "Assets/ithappy/Cute_Furniture_Free/Prefabs";
    internal const string SyntyTownCharacterRoot = "Assets/PolygonTown/Prefabs/Characters";
    internal const string SyntyCityCharacterRoot = "Assets/POLYGONCityCharacters/Prefabs";
    internal const string SyntyMiniCharacterRoot = "Assets/PolygonMini_CityCharacters";
    internal const string MeshyFamilyPrefabRoot = "Assets/Story/Characters/MeshyFamily/Prefabs";
    internal const string ChibiCharacterPath =
        "Assets/Story/Characters/ThirdParty/StylooChibi/FBX/merchantpr.fbx";
    internal const string KenneyMiniDenizPath =
        "Assets/Story/Characters/ThirdParty/KenneyMini/FBX/character-male-a.fbx";
    internal const string KenneyMiniCanPath =
        "Assets/Story/Characters/ThirdParty/KenneyMini/FBX/character-male-d.fbx";
    internal const string StorySkyboxMaterialPath = MaterialRoot + "/Story_ProceduralSkybox.mat";
    internal const string KenneyInputPromptRoot = "Assets/Story/UI/ThirdParty/KenneyInputPrompts";
    internal const string KenneyUiAdventureRoot = "Assets/Story/UI/ThirdParty/KenneyUIAdventure";
    internal const string CartoonUiRoot = "Assets/Story/UI/ThirdParty/CartoonUIPack";
    internal const string CasualUiRoot = "Assets/Story/UI/ThirdParty/CasualUILab";
    internal const string StoryLogoPath = "Assets/Story/UI/Brand/DepremLogo.png";
    internal const string PlayfulFontRoot = "Assets/Fonts/StoryPlayful";
    internal const string PlayfulRegularFontAssetPath = PlayfulFontRoot + "/Lexend Regular SDF.asset";
    internal const string PlayfulSemiboldFontAssetPath = PlayfulFontRoot + "/Lexend SemiBold SDF.asset";
    internal const string PlayfulDisplayFontAssetPath = PlayfulFontRoot + "/Lexend Bold SDF.asset";
    internal const string RebuildPreparationSceneName = "Story_01_RebuildPreview";
    internal const string RebuildHomeSafetySceneName = "Story_02_RebuildPreview";
    internal const string RebuildQuakeSceneName = "Story_03_RebuildPreview";
    internal const string RebuildEvacuationSceneName = "Story_04_RebuildPreview";
    private static bool syntyMiniMaterialsPrepared;

    internal static readonly Color Navy = new Color32(15, 30, 46, 255);
    internal static readonly Color Teal = new Color32(25, 151, 151, 255);
    internal static readonly Color Amber = new Color32(244, 173, 65, 255);
    internal static readonly Color Coral = new Color32(224, 91, 82, 255);
    internal static readonly Color Cream = new Color32(236, 226, 204, 255);
    internal static readonly Color Wall = new Color32(191, 198, 188, 255);
    internal static readonly Color Floor = new Color32(102, 78, 63, 255);
    internal static readonly Color Concrete = new Color32(91, 99, 105, 255);
    internal static readonly Color Asphalt = new Color32(45, 51, 58, 255);
    internal static readonly Color Grass = new Color32(87, 122, 82, 255);

    internal enum StoryUIPanelStyle
    {
        Flat,
        AdventurePaper,
        AdventureSteel,
        AdventureBanner,
        AdventureRoundPaper,
        PlayfulNavyPanel,
        PlayfulBluePanel,
        PlayfulCreamPanel,
        PlayfulYellowTag,
        PlayfulBlueTag,
        PlayfulYellowBadge,
        PlayfulBlueBadge,
        PlayfulPurpleBadge,
        PlayfulGreenBadge
    }

    internal sealed class Materials
    {
        internal Material wall;
        internal Material floor;
        internal Material concrete;
        internal Material asphalt;
        internal Material grass;
        internal Material cream;
        internal Material navy;
        internal Material teal;
        internal Material amber;
        internal Material coral;
        internal Material wood;
        internal Material metal;
        internal Material glass;
        internal Material dust;
        internal Material dark;
        internal Material white;
        internal Material sky;
        internal Material horizon;
    }

    internal sealed class Characters
    {
        internal GameObject deniz;
        internal GameObject can;
        internal GameObject parent;
        internal Animator denizAnimator;
        internal Animator canAnimator;
        internal Animator parentAnimator;
    }

    internal sealed class ChapterUI
    {
        internal StoryUIController controller;
        internal GameObject completionPanel;
        internal TMP_Text completionDetail;
    }

    [Serializable]
    private sealed class DialogueVoiceManifest
    {
        public DialogueVoiceManifestEntry[] entries;
    }

    [Serializable]
    private sealed class DialogueVoiceManifestEntry
    {
        public string id;
        public string subtitle;
    }

    internal struct CameraSpec
    {
        internal StoryCameraZoneId zone;
        internal string name;
        internal Vector3 position;
        internal Vector3 target;
        internal float fieldOfView;
        internal bool impulse;
        internal Transform follow;
        internal float followDistance;
        internal Vector2 screenPosition;

        internal CameraSpec(StoryCameraZoneId zone, string name, Vector3 position, Vector3 target, float fieldOfView,
            bool impulse = false, Transform follow = null, float followDistance = 16f, Vector2 screenPosition = default)
        {
            this.zone = zone;
            this.name = name;
            this.position = position;
            this.target = target;
            this.fieldOfView = fieldOfView;
            this.impulse = impulse;
            this.follow = follow;
            this.followDistance = followDistance;
            this.screenPosition = screenPosition == default ? new Vector2(0f, 0.16f) : screenPosition;
        }
    }

    internal static void EnsureFolders()
    {
        EnsureFolder("Assets/Story");
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(GeneratedRoot + "/Animations");
        EnsureFolder(AnimationRoot);
        EnsureFolder("Assets/Scenes/LegacyBackups");
    }

    internal static void EnsureLegacyBackup(string source, string backup)
    {
        if (File.Exists(backup))
            return;
        if (!File.Exists(source))
            throw new FileNotFoundException("Korunacak eski sahne bulunamadı.", source);

        File.Copy(source, backup);
        AssetDatabase.ImportAsset(backup, ImportAssetOptions.ForceSynchronousImport);
    }

    internal static Materials CreateMaterials()
    {
        EnsureFolders();
        return new Materials
        {
            wall = GetOrCreateMaterial("Chapter_Wall", Wall, 0.14f),
            floor = GetOrCreateMaterial("Chapter_WarmFloor", Floor, 0.26f),
            concrete = GetOrCreateMaterial("Chapter_Concrete", Concrete, 0.12f),
            asphalt = GetOrCreateMaterial("Chapter_Asphalt", Asphalt, 0.08f),
            grass = GetOrCreateMaterial("Chapter_Grass", Grass, 0.1f),
            cream = GetOrCreateMaterial("Cream", Cream, 0.22f),
            navy = GetOrCreateMaterial("Navy", Navy, 0.2f),
            teal = GetOrCreateMaterial("Teal", Teal, 0.24f),
            amber = GetOrCreateMaterial("Amber", Amber, 0.22f),
            coral = GetOrCreateMaterial("Coral", Coral, 0.2f),
            wood = GetOrCreateMaterial("Wood", new Color32(124, 83, 60, 255), 0.28f),
            metal = GetOrCreateMaterial("Chapter_Metal", new Color32(93, 112, 123, 255), 0.48f),
            glass = GetOrCreateMaterial("WindowGlass", new Color32(75, 126, 147, 255), 0.72f, true,
                new Color(0.02f, 0.08f, 0.12f)),
            dust = GetOrCreateMaterial("Chapter_Dust", new Color32(171, 151, 118, 190), 0.02f),
            dark = GetOrCreateMaterial("Chapter_Dark", new Color32(29, 34, 39, 255), 0.12f),
            white = GetOrCreateMaterial("Chapter_White", new Color32(227, 230, 224, 255), 0.18f),
            sky = GetOrCreateUnlitMaterial("Chapter_SkyBackdrop", new Color32(112, 146, 158, 255)),
            horizon = GetOrCreateUnlitMaterial("Chapter_HorizonHaze", new Color32(84, 111, 121, 255))
        };
    }

    internal static VolumeProfile CreateVolumeProfile()
    {
        const string path = GeneratedRoot + "/Story_Chapters_Volume.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.12f);
        bloom.scatter.Override(0.58f);

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(0.05f);
        color.contrast.Override(7f);
        color.saturation.Override(-3f);

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.17f);
        vignette.smoothness.Override(0.54f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    internal static Characters BuildFamily(Transform parent, RuntimeAnimatorController controller, bool includeParent,
        Vector3 denizPosition, Vector3 canPosition, Vector3 parentPosition,
        RuntimeAnimatorController adultController = null)
    {
        EnsureKenneyMiniCharacterImportSettings();
        Characters result = new Characters
        {
            deniz = InstantiateCharacter(StoryPrefabRoot + "/Deniz_12.prefab", "Deniz_12", parent, denizPosition, 1.5f, controller),
            can = InstantiateCharacter(StoryPrefabRoot + "/Can_8.prefab", "Can_8", parent, canPosition, 1.26f, controller)
        };
        result.denizAnimator = result.deniz.GetComponentInChildren<Animator>(true);
        result.canAnimator = result.can.GetComponentInChildren<Animator>(true);

        if (includeParent)
        {
            result.parent = InstantiateCharacter(StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab", "Anne_Ayse", parent,
                parentPosition, 1.7f, adultController != null ? adultController : controller);
            result.parentAnimator = result.parent.GetComponentInChildren<Animator>(true);
        }
        return result;
    }

    internal static GameObject InstantiateCharacter(string path, string name, Transform parent, Vector3 feetPosition,
        float targetHeight, RuntimeAnimatorController controller)
    {
        string resolvedPath = ResolveCharacterPrefabPath(path, name);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
        if (prefab == null)
            throw new InvalidOperationException("Character prefab was not found: " + resolvedPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        GameObject sourceMarker = new GameObject("CharacterSource_" + Path.GetFileNameWithoutExtension(resolvedPath));
        sourceMarker.transform.SetParent(instance.transform, false);
        instance.transform.localScale = Vector3.one;
        // Reusable character prefabs were captured from earlier story scenes. Their visual/rig data is shared,
        // but chapter-specific hotspot GameObjects (including their trigger colliders) must never leak into
        // another scene. Leaving only the component behind was enough to create invisible input blockers.
        StoryInteractable[] staleInteractions = instance.GetComponentsInChildren<StoryInteractable>(true);
        foreach (StoryInteractable staleInteraction in staleInteractions)
        {
            if (staleInteraction == null)
                continue;
            if (staleInteraction.gameObject == instance)
                Object.DestroyImmediate(staleInteraction);
            else
            {
                foreach (DraggableItem draggable in staleInteraction.GetComponents<DraggableItem>())
                    Object.DestroyImmediate(draggable);
                foreach (StoryPreparationItem item in staleInteraction.GetComponents<StoryPreparationItem>())
                    Object.DestroyImmediate(item);
                Object.DestroyImmediate(staleInteraction.gameObject);
            }
        }
        StoryPreparationItem[] stalePreparationItems = instance.GetComponentsInChildren<StoryPreparationItem>(true);
        foreach (StoryPreparationItem stalePreparationItem in stalePreparationItems)
        {
            if (stalePreparationItem == null)
                continue;
            if (stalePreparationItem.gameObject == instance)
                Object.DestroyImmediate(stalePreparationItem);
            else
                Object.DestroyImmediate(stalePreparationItem.gameObject);
        }
        if (string.Equals(resolvedPath, ChibiCharacterPath, StringComparison.Ordinal))
            ConfigureChibiAppearance(instance, name);
        FitToHeight(instance, feetPosition, targetHeight);
        CopyLegacyCostumeProps(path, instance, name);
        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.stabilizeFeet = true;
            GroundCharacterFromHumanoidFeet(instance, animator, feetPosition.y);
        }
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return instance;
    }

    private static void GroundCharacterFromHumanoidFeet(GameObject instance, Animator animator, float floorY)
    {
        if (instance == null || animator == null || !animator.isHuman || animator.avatar == null ||
            !animator.avatar.isValid || animator.runtimeAnimatorController == null)
            return;

        bool wasActive = instance.activeSelf;
        if (!wasActive)
            instance.SetActive(true);

        animator.Rebind();
        animator.Update(0f);

        // Character root must remain on the NavMesh surface. Some generated humanoid
        // avatars report an exaggerated feetBottomHeight, which used to move the whole
        // root below the floor and was then made worse when NavMeshAgent snapped it back.
        // Keep the navigation root at the authored feet point and offset only the visual
        // rig from the actually rendered idle-pose bounds.
        Vector3 rootPosition = instance.transform.position;
        instance.transform.position = new Vector3(rootPosition.x, floorY, rootPosition.z);
        Renderer[] activeRenderers = instance.GetComponentsInChildren<Renderer>(false)
            .Where(renderer => renderer != null && renderer.enabled)
            .ToArray();
        if (activeRenderers.Length > 0)
        {
            Bounds renderedBounds = activeRenderers[0].bounds;
            foreach (Renderer renderer in activeRenderers.Skip(1))
                renderedBounds.Encapsulate(renderer.bounds);
            // A tiny negative bias keeps the shoe sole visually planted throughout the walk
            // cycle. The previous positive clearance made both feet read as floating whenever
            // the retargeted clip reached its passing pose.
            float visualLift = floorY - 0.006f - renderedBounds.min.y;
            if (!float.IsNaN(visualLift) && !float.IsInfinity(visualLift))
            {
                Transform visualRoot = animator.transform;
                if (visualRoot != instance.transform)
                    visualRoot.position += Vector3.up * visualLift;
                else
                    instance.transform.position += Vector3.up * visualLift;
            }
        }

        if (!wasActive)
            instance.SetActive(false);
    }

    private static string ResolveCharacterPrefabPath(string fallbackPath, string roleName)
    {
        MeshyFamilyCharacterImporter.EnsurePrepared();
        if (roleName.IndexOf("Deniz", StringComparison.OrdinalIgnoreCase) >= 0)
            return MeshyFamilyPrefabRoot + "/Deniz.prefab";
        if (roleName.IndexOf("Can", StringComparison.OrdinalIgnoreCase) >= 0)
            return MeshyFamilyPrefabRoot + "/Can.prefab";
        if (roleName.IndexOf("Nermin", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Neighbor", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Komsu", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Komşu", StringComparison.OrdinalIgnoreCase) >= 0)
            return MeshyFamilyPrefabRoot + "/Komsu.prefab";
        if (roleName.IndexOf("Worker", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Görevli", StringComparison.OrdinalIgnoreCase) >= 0)
            return SyntyCityCharacterRoot + "/Character_Paramedic_01.prefab";
        if (roleName.IndexOf("Baba", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Father", StringComparison.OrdinalIgnoreCase) >= 0)
            return MeshyFamilyPrefabRoot + "/Baba.prefab";
        if (roleName.IndexOf("Anne", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Mother", StringComparison.OrdinalIgnoreCase) >= 0)
            return MeshyFamilyPrefabRoot + "/Anne.prefab";
        return fallbackPath;
    }

    private static void EnsureSyntyMiniMaterials()
    {
        if (syntyMiniMaterialsPrepared ||
            !AssetDatabase.IsValidFolder(SyntyMiniCharacterRoot))
            return;

        syntyMiniMaterialsPrepared = true;
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            throw new InvalidOperationException("URP/Lit shader could not be resolved for Synty MINI characters.");

        bool changed = false;
        string materialRoot = SyntyMiniCharacterRoot + "/Materials";
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { materialRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == urpLit)
                continue;

            Texture mainTexture = material.HasProperty("_MainTex")
                ? material.GetTexture("_MainTex")
                : null;
            Vector2 textureScale = material.HasProperty("_MainTex")
                ? material.GetTextureScale("_MainTex")
                : Vector2.one;
            Vector2 textureOffset = material.HasProperty("_MainTex")
                ? material.GetTextureOffset("_MainTex")
                : Vector2.zero;
            Color baseColor = material.HasProperty("_Color")
                ? material.GetColor("_Color")
                : Color.white;

            material.shader = urpLit;
            material.SetTexture("_BaseMap", mainTexture);
            material.SetTextureScale("_BaseMap", textureScale);
            material.SetTextureOffset("_BaseMap", textureOffset);
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.24f);
            EditorUtility.SetDirty(material);
            changed = true;
        }

        if (changed)
            AssetDatabase.SaveAssets();
    }

    private static string FindImportedSyntyMiniCharacter(params string[] preferredNames)
    {
        foreach (string preferredName in preferredNames)
        {
            string searchToken = preferredName
                .Replace("Character_", string.Empty)
                .Replace("School_", "School")
                .Replace("_01", string.Empty)
                .Replace("_02", string.Empty);
            string[] guids = AssetDatabase.FindAssets(searchToken + " t:Prefab");
            string normalizedPreferredName = NormalizeAssetName(preferredName);
            string match = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                    path.IndexOf("mini", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    path.IndexOf("character", StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(path =>
                    NormalizeAssetName(Path.GetFileNameWithoutExtension(path))
                        .Contains(normalizedPreferredName))
                .OrderByDescending(path =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        preferredName,
                        StringComparison.OrdinalIgnoreCase))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(match))
                return match;
        }

        return null;
    }

    private static string NormalizeAssetName(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static void EnsureKenneyMiniCharacterImportSettings()
    {
        EnsureHumanoidModelImport(KenneyMiniDenizPath);
        EnsureHumanoidModelImport(KenneyMiniCanPath);
    }

    private static void EnsureHumanoidModelImport(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Kenney Mini karakter modeli içe aktarılamadı: " + path);

        bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                       importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                       !importer.importAnimation;
        if (!changed)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureChibiAppearance(GameObject character, string roleName)
    {
        string role = ChibiRoleKey(roleName);
        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>(true))
        {
            string part = ChibiPart(renderer.name);
            if (part == "Hat")
            {
                renderer.gameObject.SetActive(false);
                continue;
            }

            Material template = ChibiTemplateMaterial(part, renderer.sharedMaterial);
            if (template == null)
                continue;
            renderer.sharedMaterial = GetOrCreateChibiRoleMaterial(
                role,
                part,
                template,
                ChibiTint(role, part));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static string ChibiRoleKey(string roleName)
    {
        if (roleName.IndexOf("Can", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Can";
        if (roleName.IndexOf("Deniz", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Deniz";
        if (roleName.IndexOf("Baba", StringComparison.OrdinalIgnoreCase) >= 0 ||
            roleName.IndexOf("Father", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Baba";
        return "Anne";
    }

    private static string ChibiPart(string rendererName)
    {
        return rendererName.ToLowerInvariant() switch
        {
            "bottes" => "Shoes",
            "character_low" => "Skin",
            "chemise" => "Top",
            "eyelashes" => "Lashes",
            "eyes" => "Eyes",
            "hairone" => "Hair",
            "hat" => "Hat",
            "pants" => "Bottom",
            "tooth" => "Teeth",
            _ => "Skin"
        };
    }

    private static Material ChibiTemplateMaterial(string part, Material fallback)
    {
        string templatePart = part switch
        {
            "Shoes" => "Shoes",
            "Top" => "Top",
            "Lashes" => "Lashes",
            "Eyes" or "Teeth" => "Eyes",
            "Hair" => "Hair",
            "Bottom" => "Bottom",
            _ => "Skin"
        };
        return AssetDatabase.LoadAssetAtPath<Material>(
                   MaterialRoot + "/Anne_Chibi_" + templatePart + ".mat") ??
               fallback;
    }

    private static Material GetOrCreateChibiRoleMaterial(
        string role,
        string part,
        Material template,
        Color tint)
    {
        string path = MaterialRoot + "/Chibi_" + role + "_" + part + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(template);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = template.shader;
            material.CopyPropertiesFromMaterial(template);
        }

        material.name = "Chibi_" + role + "_" + part;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Color ChibiTint(string role, string part)
    {
        if (part is "Skin" or "Eyes" or "Lashes" or "Teeth")
            return Color.white;

        return (role, part) switch
        {
            ("Deniz", "Top") => new Color32(74, 169, 211, 255),
            ("Deniz", "Bottom") => new Color32(48, 66, 112, 255),
            ("Deniz", "Hair") => new Color32(54, 37, 28, 255),
            ("Deniz", "Shoes") => new Color32(241, 174, 62, 255),
            ("Can", "Top") => new Color32(225, 91, 82, 255),
            ("Can", "Bottom") => new Color32(35, 129, 135, 255),
            ("Can", "Hair") => new Color32(92, 55, 35, 255),
            ("Can", "Shoes") => new Color32(61, 91, 160, 255),
            ("Baba", "Top") => new Color32(64, 117, 174, 255),
            ("Baba", "Bottom") => new Color32(45, 55, 82, 255),
            ("Baba", "Hair") => new Color32(45, 34, 29, 255),
            ("Baba", "Shoes") => new Color32(55, 60, 66, 255),
            ("Anne", "Top") => new Color32(164, 91, 177, 255),
            ("Anne", "Bottom") => new Color32(55, 85, 112, 255),
            ("Anne", "Hair") => new Color32(88, 51, 34, 255),
            ("Anne", "Shoes") => new Color32(107, 62, 72, 255),
            _ => Color.white
        };
    }

    internal static Transform FindHumanoidBone(GameObject character, HumanBodyBones bone)
    {
        if (character == null)
            return null;
        Animator animator = character.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.avatar != null && animator.avatar.isHuman)
        {
            Transform humanoidBone = animator.GetBoneTransform(bone);
            if (humanoidBone != null)
                return humanoidBone;
        }

        string[] fallbackNames = bone == HumanBodyBones.RightHand
            ? new[] { "IteamSlot.R", "Hand.R", "hand_r", "DEF-hand.R" }
            : new[] { bone.ToString() };
        return character.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => fallbackNames.Any(name =>
                candidate.name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    private static void CopyLegacyCostumeProps(string carrierPath, GameObject destination, string roleName)
    {
        GameObject carrier = AssetDatabase.LoadAssetAtPath<GameObject>(carrierPath);
        if (carrier == null)
            return;

        if (roleName.IndexOf("Deniz", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            CopyLegacyChild(carrier, destination, "Deniz_12_WornShoes");
            GameObject wornBag = CopyLegacyChild(carrier, destination, "Deniz_WornEmergencyBag");
            if (wornBag != null)
                RestyleEmergencyBag(wornBag, CreateMaterials(), new Vector3(0.46f, 0.58f, 0.28f), false);
        }
        else if (roleName.IndexOf("Can", StringComparison.OrdinalIgnoreCase) >= 0)
            CopyLegacyChild(carrier, destination, "Can_8_WornShoes");
    }

    private static GameObject CopyLegacyChild(GameObject carrier, GameObject destination, string childName)
    {
        Transform source = carrier.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == childName);
        if (source == null)
            return null;

        Vector3 localPosition = carrier.transform.InverseTransformPoint(source.position);
        Quaternion localRotation = Quaternion.Inverse(carrier.transform.rotation) * source.rotation;
        Vector3 carrierScale = carrier.transform.lossyScale;
        Vector3 sourceScale = source.lossyScale;
        Vector3 localScale = new Vector3(
            sourceScale.x / Mathf.Max(0.0001f, carrierScale.x),
            sourceScale.y / Mathf.Max(0.0001f, carrierScale.y),
            sourceScale.z / Mathf.Max(0.0001f, carrierScale.z));

        GameObject clone = Object.Instantiate(source.gameObject, destination.transform);
        clone.name = childName;
        clone.transform.localPosition = localPosition;
        clone.transform.localRotation = localRotation;
        clone.transform.localScale = localScale;
        foreach (StoryInteractable interaction in clone.GetComponentsInChildren<StoryInteractable>(true))
            Object.DestroyImmediate(interaction);
        foreach (DraggableItem draggable in clone.GetComponentsInChildren<DraggableItem>(true))
            Object.DestroyImmediate(draggable);
        foreach (StoryPreparationItem item in clone.GetComponentsInChildren<StoryPreparationItem>(true))
            Object.DestroyImmediate(item);
        foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        clone.SetActive(false);
        return clone;
    }

    internal static GameObject RestyleEmergencyBag(
        GameObject bagRoot,
        Materials materials,
        Vector3 targetSize,
        bool open)
    {
        if (bagRoot == null)
            throw new ArgumentNullException(nameof(bagRoot));

        bool wasActive = bagRoot.activeSelf;
        bagRoot.SetActive(true);
        Transform oldCleanVisual = bagRoot.transform.Find("CleanEmergencyBackpackVisual");
        if (oldCleanVisual != null)
            Object.DestroyImmediate(oldCleanVisual.gameObject);

        Vector3 feetPosition = bagRoot.transform.position;
        if (TryGetSizingBounds(bagRoot, out Bounds oldBounds))
            feetPosition = new Vector3(oldBounds.center.x, oldBounds.min.y, oldBounds.center.z);

        foreach (Renderer renderer in bagRoot.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        Vector3 uprightEuler = new Vector3(0f, bagRoot.transform.eulerAngles.y, 0f);
        GameObject cleanVisual = StoryAuthoredPropFactory.CreateEmergencyBackpack(
            "CleanEmergencyBackpackVisual",
            bagRoot.transform,
            feetPosition,
            targetSize,
            uprightEuler,
            materials.coral,
            materials.teal,
            materials.cream,
            materials.navy,
            open,
            false);
        bagRoot.SetActive(wasActive);
        return cleanVisual;
    }

    internal static StoryPlayerMovement ConfigurePlayer(GameObject deniz)
    {
        NavMeshAgent agent = deniz.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = deniz.AddComponent<NavMeshAgent>();
        agent.speed = 1.75f;
        agent.acceleration = 9f;
        agent.angularSpeed = 540f;
        agent.radius = 0.24f;
        agent.height = 1.48f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 0.13f;

        CapsuleCollider capsule = deniz.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = deniz.AddComponent<CapsuleCollider>();
        capsule.radius = 0.24f;
        capsule.height = 1.46f;
        capsule.center = new Vector3(0f, 0.73f, 0f);
        Rigidbody body = deniz.GetComponent<Rigidbody>();
        if (body == null)
            body = deniz.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        StoryPlayerMovement movement = deniz.GetComponent<StoryPlayerMovement>();
        if (movement == null)
            movement = deniz.AddComponent<StoryPlayerMovement>();
        SetReference(movement, "animator", deniz.GetComponentInChildren<Animator>(true));
        return movement;
    }

    internal static StorySiblingFollower ConfigureSibling(GameObject can, Transform target)
    {
        NavMeshAgent agent = can.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = can.AddComponent<NavMeshAgent>();
        agent.speed = 1.65f;
        agent.acceleration = 8f;
        agent.angularSpeed = 520f;
        agent.radius = 0.21f;
        agent.height = 1.24f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 1.05f;
        StorySiblingFollower follower = can.GetComponent<StorySiblingFollower>();
        if (follower == null)
            follower = can.AddComponent<StorySiblingFollower>();
        SetReference(follower, "target", target);
        SetReference(follower, "animator", can.GetComponentInChildren<Animator>(true));
        return follower;
    }

    internal static StoryCameraController BuildCameras(Transform parent, StoryCameraZoneId initialZone, CameraSpec[] specs,
        out Camera mainCamera, out CinemachineBrain brain)
    {
        Transform cameraRoot = NewChild(parent, "StoryCameras");
        GameObject main = new GameObject("Main Camera");
        main.transform.SetParent(cameraRoot);
        main.tag = "MainCamera";
        main.transform.position = specs[0].position;
        main.transform.rotation = LookAt(specs[0].position, specs[0].target);
        mainCamera = main.AddComponent<Camera>();
        mainCamera.backgroundColor = new Color32(46, 60, 68, 255);
        ConfigureStorySkybox(mainCamera);
        mainCamera.nearClipPlane = 0.08f;
        mainCamera.farClipPlane = 180f;
        mainCamera.allowHDR = true;
        main.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = main.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        brain = main.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.72f);

        StoryCameraBinding[] bindings = new StoryCameraBinding[specs.Length];
        for (int i = 0; i < specs.Length; i++)
        {
            CameraSpec spec = specs[i];
            GameObject cameraObject = new GameObject(spec.name);
            cameraObject.transform.SetParent(cameraRoot);
            cameraObject.transform.position = spec.position;
            cameraObject.transform.rotation = LookAt(spec.position, spec.target);
            CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            LensSettings lens = LensSettings.Default;
            lens.FieldOfView = Mathf.Clamp(spec.fieldOfView, 38f, 50f);
            lens.NearClipPlane = 0.08f;
            lens.FarClipPlane = 180f;
            virtualCamera.Lens = lens;
            virtualCamera.Priority = 0;

            if (spec.follow != null)
            {
                virtualCamera.Follow = spec.follow;
                CinemachinePositionComposer composer = cameraObject.AddComponent<CinemachinePositionComposer>();
                composer.CameraDistance = spec.followDistance;
                composer.TargetOffset = new Vector3(0f, 0.82f, 0f);
                composer.Damping = new Vector3(0.36f, 0.28f, 0.48f);
                composer.DeadZoneDepth = 0.4f;
                composer.CenterOnActivate = false;
                composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.2f, Smoothing = 8f, IgnoreY = true };
                ScreenComposerSettings composition = ScreenComposerSettings.Default;
                composition.ScreenPosition = spec.screenPosition;
                composition.DeadZone.Enabled = true;
                composition.DeadZone.Size = new Vector2(0.1f, 0.08f);
                composition.HardLimits.Enabled = true;
                composition.HardLimits.Size = new Vector2(0.72f, 0.64f);
                composer.Composition = composition;
            }

            if (spec.impulse)
            {
                CinemachineImpulseListener listener = cameraObject.AddComponent<CinemachineImpulseListener>();
                listener.ChannelMask = 1;
                listener.Gain = 0.12f;
            }
            bindings[i] = new StoryCameraBinding { zone = spec.zone, camera = virtualCamera };
        }

        GameObject controllerObject = new GameObject("MissionCameraController");
        controllerObject.transform.SetParent(cameraRoot);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        serialized.FindProperty("initialZone").intValue = (int)initialZone;
        SerializedProperty cameraArray = serialized.FindProperty("cameras");
        cameraArray.arraySize = bindings.Length;
        for (int i = 0; i < bindings.Length; i++)
        {
            SerializedProperty element = cameraArray.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("zone").intValue = (int)bindings[i].zone;
            element.FindPropertyRelative("camera").objectReferenceValue = bindings[i].camera;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    internal static void ConfigureStorySkybox(Camera camera)
    {
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(StorySkyboxMaterialPath);
        if (skybox == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                throw new InvalidOperationException("Procedural skybox shader bulunamadı.");
            skybox = new Material(shader) { name = "Story_ProceduralSkybox" };
            AssetDatabase.CreateAsset(skybox, StorySkyboxMaterialPath);
        }

        if (skybox.HasProperty("_SunDisk"))
            skybox.SetFloat("_SunDisk", 2f);
        if (skybox.HasProperty("_SunSize"))
            skybox.SetFloat("_SunSize", 0.035f);
        if (skybox.HasProperty("_SunSizeConvergence"))
            skybox.SetFloat("_SunSizeConvergence", 5f);
        if (skybox.HasProperty("_AtmosphereThickness"))
            skybox.SetFloat("_AtmosphereThickness", 1.08f);
        if (skybox.HasProperty("_SkyTint"))
            skybox.SetColor("_SkyTint", new Color32(118, 174, 214, 255));
        if (skybox.HasProperty("_GroundColor"))
            skybox.SetColor("_GroundColor", new Color32(48, 82, 104, 255));
        if (skybox.HasProperty("_Exposure"))
            skybox.SetFloat("_Exposure", 1.04f);
        EditorUtility.SetDirty(skybox);

        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.82f;
        RenderSettings.reflectionIntensity = 0.35f;
        if (camera != null)
            camera.clearFlags = CameraClearFlags.Skybox;
    }

    internal static ChapterUI BuildUI(Transform parent, StoryCameraController cameraController, string canvasName,
        string completionEyebrow, string completionTitle, string completionSafety, StoryAct? storyAct = null)
    {
        LoadPlayfulStoryFonts(out TMP_FontAsset regular, out TMP_FontAsset semibold, out TMP_FontAsset bold);

        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = CreateUIRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();
        GameObject objective = CreatePanel(
            "ObjectiveStrip",
            safeArea.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-54f, -94f),
            new Vector2(870f, 174f),
            Color.white,
            false,
            StoryUIPanelStyle.PlayfulNavyPanel);

        bool hasStoryRoute = storyAct.HasValue;
        float objectiveTextX = hasStoryRoute ? 86f : 8f;
        float objectiveTextWidth = hasStoryRoute ? 620f : 750f;
        if (hasStoryRoute)
        {
            GameObject actBadge = CreatePanel(
                "ActBadge",
                objective.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(72f, 0f),
                new Vector2(112f, 112f),
                Color.white,
                false,
                StoryUIPanelStyle.PlayfulYellowBadge);
            TMP_Text actBadgeCaption = CreateText(
                "ActBadgeCaption",
                actBadge.transform,
                semibold,
                14f,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 22f),
                new Vector2(84f, 24f));
            actBadgeCaption.textWrappingMode = TextWrappingModes.NoWrap;
            actBadgeCaption.text = "PERDE";
            TMP_Text actBadgeValue = CreateText(
                "ActBadgeValue",
                actBadge.transform,
                bold,
                30f,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, -14f),
                new Vector2(84f, 40f));
            actBadgeValue.textWrappingMode = TextWrappingModes.NoWrap;
            actBadgeValue.text = $"{(int)storyAct.Value}/4";
        }

        TMP_Text objectiveTitle = CreateText("ObjectiveTitle", objective.transform, bold, 28f, Amber,
            TextAlignmentOptions.Left, new Vector2(objectiveTextX, 38f), new Vector2(objectiveTextWidth, 42f));
        objectiveTitle.textWrappingMode = TextWrappingModes.NoWrap;
        objectiveTitle.enableAutoSizing = true;
        objectiveTitle.fontSizeMin = 20f;
        objectiveTitle.fontSizeMax = 28f;
        TMP_Text objectiveDetail = CreateText("ObjectiveDetail", objective.transform, semibold, 26f, Color.white,
            TextAlignmentOptions.Left, new Vector2(objectiveTextX, -30f), new Vector2(objectiveTextWidth, 80f));
        objectiveDetail.enableAutoSizing = true;
        objectiveDetail.fontSizeMin = 20f;
        objectiveDetail.fontSizeMax = 26f;
        Button pause = CreateButton("PauseButton", safeArea.transform, string.Empty, bold, new Vector2(1f, 1f),
            new Vector2(-35f, -68f), new Vector2(104f, 104f), Teal, Cream);

        GameObject contextPanel = CreatePanel(
            "ContextPanel",
            safeArea.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 412f),
            new Vector2(820f, 112f),
            Color.white,
            false,
            StoryUIPanelStyle.PlayfulYellowTag);
        TMP_Text context = CreateText("ContextText", contextPanel.transform, bold, 25f, Navy,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(750f, 62f));
        context.enableAutoSizing = true;
        context.fontSizeMin = 20f;
        context.fontSizeMax = 25f;
        contextPanel.SetActive(false);

        GameObject subtitlePanel = CreatePanel(
            "SubtitlePanel",
            safeArea.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 214f),
            new Vector2(940f, 212f),
            Color.white,
            false,
            StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text subtitle = CreateText("SubtitleText", subtitlePanel.transform, semibold, 31f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(870f, 166f));
        subtitle.enableAutoSizing = true;
        subtitle.fontSizeMin = 25f;
        subtitle.fontSizeMax = 31f;
        subtitlePanel.SetActive(false);

        GameObject pauseOverlay = CreatePanel("PauseOverlay", safeArea.transform, Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, new Color(0.004f, 0.01f, 0.016f, 0.82f), true);
        GameObject pauseCard = CreatePanel(
            "PausePanel",
            pauseOverlay.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            Vector2.zero,
            new Vector2(840f, 570f),
            Color.white,
            true,
            StoryUIPanelStyle.PlayfulNavyPanel);
        GameObject pauseRibbon = CreatePanel("PauseRibbon", pauseCard.transform, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(0f, 222f), new Vector2(690f, 136f), Color.white, false,
            StoryUIPanelStyle.PlayfulYellowTag);
        TMP_Text pauseTitle = CreateText("PauseTitle", pauseRibbon.transform, bold, 45f, Navy,
            TextAlignmentOptions.Center, new Vector2(0f, 4f), new Vector2(560f, 72f));
        pauseTitle.text = "OYUN DURAKLATILDI";
        CreateText("PauseHint", pauseCard.transform, semibold, 29f, Cream,
            TextAlignmentOptions.Center, new Vector2(0f, 108f), new Vector2(670f, 86f)).text =
            "Kaldığın yer kontrol noktasında güvende.\nHazır olduğunda maceraya dön.";
        Button resume = CreateButton("ResumeButton", pauseCard.transform, "DEVAM ET", bold, Vector2.one * 0.5f,
            new Vector2(0f, -8f), new Vector2(570f, 112f), Teal, Color.white);
        Button retry = CreateButton("RetryCheckpointButton", pauseCard.transform, "KONTROL NOKTASINA DÖN", semibold,
            Vector2.one * 0.5f, new Vector2(0f, -150f), new Vector2(570f, 112f),
            new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        pauseOverlay.SetActive(false);

        float completionHeight = hasStoryRoute ? 760f : 600f;
        GameObject completion = CreatePanel(
            "ChapterCompletionCard",
            safeArea.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, -35f),
            new Vector2(900f, completionHeight),
            Color.white,
            true,
            StoryUIPanelStyle.PlayfulNavyPanel);
        GameObject completionRibbon = CreatePanel("CompletionRibbon", completion.transform, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(0f, hasStoryRoute ? 310f : 225f), new Vector2(720f, 132f),
            Color.white, false, StoryUIPanelStyle.PlayfulGreenBadge);
        TMP_Text completionEyebrowText = CreateText("CompletionEyebrow", completionRibbon.transform, bold, 24f, Cream,
            TextAlignmentOptions.Center, new Vector2(0f, 3f), new Vector2(600f, 48f));
        completionEyebrowText.text = completionEyebrow;
        ApplyDisplayTextStyle(completionEyebrowText);
        TMP_Text completionTitleText = CreateText("CompletionTitle", completion.transform, bold, 43f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, hasStoryRoute ? 205f : 125f), new Vector2(760f, 108f));
        completionTitleText.text = completionTitle;
        completionTitleText.enableAutoSizing = true;
        completionTitleText.fontSizeMin = 29f;
        completionTitleText.fontSizeMax = 43f;
        ApplyDisplayTextStyle(completionTitleText);
        TMP_Text completionDetail = CreateText("CompletionDetail", completion.transform, semibold, 29f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, hasStoryRoute ? 108f : 30f), new Vector2(760f, 76f));
        completionDetail.text = "Güvenli adımları tamamladın.";
        CreateText("CompletionSafety", completion.transform, semibold, 26f, Amber,
            TextAlignmentOptions.Center, new Vector2(0f, hasStoryRoute ? 20f : -58f),
            new Vector2(760f, 88f)).text = completionSafety;
        Button nextAct = null;
        if (hasStoryRoute)
        {
            nextAct = CreateButton(
                "NextActButton",
                completion.transform,
                storyAct == StoryAct.Evacuation ? "BÖLÜM SEÇİMİ" : "SONRAKİ PERDE",
                bold,
                Vector2.one * 0.5f,
                new Vector2(0f, -140f),
                new Vector2(590f, 108f),
                Teal,
                Color.white);
        }
        Button replay = CreateButton("ReplayChapterButton", completion.transform, "PERDEYİ YENİDEN OYNA", semibold,
            Vector2.one * 0.5f, new Vector2(0f, -205f), new Vector2(590f, 104f),
            new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        if (hasStoryRoute)
            replay.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -280f);
        completion.SetActive(false);

        GameObject chapterSelection = null;
        Button preparationButton = null;
        Button homeSafetyButton = null;
        Button quakeButton = null;
        Button evacuationButton = null;
        Button chapterBackButton = null;
        if (hasStoryRoute)
        {
            chapterSelection = CreatePanel(
                "ChapterSelectionCard",
                safeArea.transform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(0f, -10f),
                new Vector2(920f, 850f),
                Color.white,
                true,
                StoryUIPanelStyle.PlayfulNavyPanel);
            GameObject chapterRibbon = CreatePanel("ChapterSelectionRibbon", chapterSelection.transform,
                Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 335f), new Vector2(720f, 140f),
                Color.white, false, StoryUIPanelStyle.PlayfulPurpleBadge);
            TMP_Text chapterTitle = CreateText("ChapterSelectionTitle", chapterRibbon.transform, bold, 44f, Cream,
                TextAlignmentOptions.Center, new Vector2(0f, 3f), new Vector2(600f, 70f));
            chapterTitle.text = "BÖLÜM SEÇİMİ";
            ApplyDisplayTextStyle(chapterTitle);
            CreateText(
                "ChapterSelectionHint",
                chapterSelection.transform,
                semibold,
                29f,
                Cream,
                TextAlignmentOptions.Center,
                new Vector2(0f, 230f),
                new Vector2(760f, 76f)).text =
                "Hazırlık seçimlerin korunur.\nİstediğin perdeyi yeniden oynayabilirsin.";

            preparationButton = CreateButton(
                "OpenPreparationActButton",
                chapterSelection.transform,
                "1\nHAZIRLIK",
                bold,
                Vector2.one * 0.5f,
                new Vector2(-185f, 60f),
                new Vector2(330f, 174f),
                Teal,
                Color.white);
            homeSafetyButton = CreateButton(
                "OpenHomeSafetyActButton",
                chapterSelection.transform,
                "2\nEVİ GÜVENLİ YAP",
                bold,
                Vector2.one * 0.5f,
                new Vector2(185f, 60f),
                new Vector2(330f, 174f),
                Navy,
                Cream);
            quakeButton = CreateButton(
                "OpenQuakeActButton",
                chapterSelection.transform,
                "3\nDEPREM",
                bold,
                Vector2.one * 0.5f,
                new Vector2(-185f, -148f),
                new Vector2(330f, 174f),
                Navy,
                Cream);
            evacuationButton = CreateButton(
                "OpenEvacuationActButton",
                chapterSelection.transform,
                "4\nTAHLİYE",
                bold,
                Vector2.one * 0.5f,
                new Vector2(185f, -148f),
                new Vector2(330f, 174f),
                Teal,
                Color.white);
            chapterBackButton = CreateButton(
                "CloseChapterSelectionButton",
                chapterSelection.transform,
                "SONUÇLARA DÖN",
                semibold,
                Vector2.one * 0.5f,
                new Vector2(0f, -330f),
                new Vector2(540f, 96f),
                new Color(0.12f, 0.2f, 0.25f, 1f),
                Cream);
            chapterSelection.SetActive(false);
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(parent);

        StoryUIController controller = canvasObject.AddComponent<StoryUIController>();
        AudioSource dialogueVoiceSource = canvasObject.AddComponent<AudioSource>();
        dialogueVoiceSource.playOnAwake = false;
        dialogueVoiceSource.loop = false;
        dialogueVoiceSource.spatialBlend = 0f;
        dialogueVoiceSource.volume = 0.92f;
        dialogueVoiceSource.priority = 32;
        Animation objectivePresentation = CreateUIPresentationAnimation(
            objective, "StoryUI_ObjectiveStrip_In", new Vector2(0f, 34f), 0.28f);
        Animation subtitlePresentation = CreateUIPresentationAnimation(
            subtitlePanel, "StoryUI_SubtitlePanel_In", new Vector2(0f, -34f), 0.24f);
        Animation contextPresentation = CreateUIPresentationAnimation(
            contextPanel, "StoryUI_ContextPanel_In", new Vector2(0f, -22f), 0.2f);
        Animation pausePresentation = CreateUIPresentationAnimation(
            pauseCard, "StoryUI_PauseCard_In", Vector2.zero, 0.26f);
        Animation completionPresentation = CreateUIPresentationAnimation(
            completion, "StoryUI_CompletionCard_In", new Vector2(0f, -24f), 0.32f);
        Animation chapterSelectionPresentation = chapterSelection != null
            ? CreateUIPresentationAnimation(
                chapterSelection, "StoryUI_ChapterSelection_In", new Vector2(0f, -24f), 0.3f)
            : null;
        SetReference(controller, "objectiveTitle", objectiveTitle);
        SetReference(controller, "objectiveDetail", objectiveDetail);
        SetReference(controller, "subtitle", subtitle);
        SetReference(controller, "contextPrompt", context);
        SetReference(controller, "objectivePresentation", objectivePresentation);
        SetReference(controller, "subtitlePresentation", subtitlePresentation);
        SetReference(controller, "contextPresentation", contextPresentation);
        SetReference(controller, "pausePresentation", pausePresentation);
        SetReference(controller, "completionPresentation", completionPresentation);
        if (chapterSelectionPresentation != null)
            SetReference(controller, "chapterSelectionPresentation", chapterSelectionPresentation);
        SetReference(controller, "dialogueVoiceSource", dialogueVoiceSource);
        SetReference(controller, "pausePanel", pauseOverlay);
        SetReference(controller, "completionPanel", completion);
        SetReference(controller, "completionReportCard", completion);
        if (chapterSelection != null)
            SetReference(controller, "chapterSelectionCard", chapterSelection);
        if (nextAct != null)
        {
            SetReference(controller, "nextActButton", nextAct.gameObject);
            SetReference(controller, "nextActButtonLabel", nextAct.GetComponentInChildren<TMP_Text>(true));
        }
        SetReference(controller, "cameraController", cameraController);
        UnityEventTools.AddPersistentListener(pause.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(resume.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(retry.onClick, controller.RetryCheckpoint);
        UnityEventTools.AddPersistentListener(replay.onClick, controller.ReplayStory);
        if (nextAct != null)
            UnityEventTools.AddPersistentListener(nextAct.onClick, controller.ContinueAfterAct);
        if (preparationButton != null)
            UnityEventTools.AddPersistentListener(preparationButton.onClick, controller.OpenPreparationAct);
        if (homeSafetyButton != null)
            UnityEventTools.AddPersistentListener(homeSafetyButton.onClick, controller.OpenHomeSafetyAct);
        if (quakeButton != null)
            UnityEventTools.AddPersistentListener(quakeButton.onClick, controller.OpenQuakeAct);
        if (evacuationButton != null)
            UnityEventTools.AddPersistentListener(evacuationButton.onClick, controller.OpenEvacuationAct);
        if (chapterBackButton != null)
            UnityEventTools.AddPersistentListener(chapterBackButton.onClick, controller.ShowCompletionReport);

        return new ChapterUI
        {
            controller = controller,
            completionPanel = completion,
            completionDetail = completionDetail
        };
    }

    internal static void ConfigureDialogueVoices(
        StoryUIController controller,
        string manifestPath,
        string clipFolder)
    {
        if (controller == null)
            throw new ArgumentNullException(nameof(controller));

        TextAsset manifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
        if (manifestAsset == null)
            throw new InvalidOperationException("Seslendirme manifesti bulunamadı: " + manifestPath);

        DialogueVoiceManifest manifest = JsonUtility.FromJson<DialogueVoiceManifest>(manifestAsset.text);
        DialogueVoiceManifestEntry[] entries = manifest?.entries ?? Array.Empty<DialogueVoiceManifestEntry>();
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty bindings = serializedController.FindProperty("dialogueVoices");
        bindings.arraySize = entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            DialogueVoiceManifestEntry entry = entries[i];
            string clipPath = clipFolder.TrimEnd('/', '\\') + "/" + entry.id + ".mp3";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
                throw new InvalidOperationException("Seslendirme klibi bulunamadı: " + clipPath);

            SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("subtitle").stringValue = entry.subtitle;
            binding.FindPropertyRelative("clip").objectReferenceValue = clip;
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    internal static StoryInteractable AddInteractable(GameObject visualRoot, string id, string prompt,
        StoryInteractionKind kind, Transform interactionPoint, StoryInteractionGesture gesture, StoryCameraZoneId cameraZone,
        bool interactFromAnywhere = false, int gestureCount = 1, float interactionSeconds = 1.2f, float range = 1.45f)
    {
        EnsureCollider(visualRoot);
        GameObject objectiveMarker = CreateObjectiveMarker(visualRoot, gesture);
        StoryInteractable interactable = visualRoot.GetComponent<StoryInteractable>() ??
                                         visualRoot.AddComponent<StoryInteractable>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactionId").stringValue = id;
        serialized.FindProperty("prompt").stringValue = prompt;
        serialized.FindProperty("interactionKind").intValue = (int)kind;
        serialized.FindProperty("interactionPoint").objectReferenceValue = interactionPoint;
        serialized.FindProperty("interactionRange").floatValue = range;
        serialized.FindProperty("interactionGesture").intValue = (int)gesture;
        serialized.FindProperty("requiredGestureCount").intValue = Mathf.Max(1, gestureCount);
        serialized.FindProperty("estimatedInteractionSeconds").floatValue = Mathf.Max(0.25f, interactionSeconds);
        serialized.FindProperty("gestureTarget").objectReferenceValue = null;
        serialized.FindProperty("focusCameraZone").intValue = (int)cameraZone;
        serialized.FindProperty("returnCameraAfterCompletion").boolValue = false;
        serialized.FindProperty("interactFromAnywhere").boolValue = interactFromAnywhere;
        serialized.FindProperty("autoTriggerOnPlayerEnter").boolValue = false;
        serialized.FindProperty("oneShot").boolValue = true;
        serialized.FindProperty("availableOnStart").boolValue = false;
        serialized.FindProperty("highlightRoot").objectReferenceValue = objectiveMarker;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (objectiveMarker != null)
            objectiveMarker.SetActive(false);
        return interactable;
    }

    private static GameObject CreateObjectiveMarker(GameObject visualRoot, StoryInteractionGesture gesture)
    {
        Sprite backgroundSprite = LoadObjectiveMarkerSprite("marker_circle.png");
        Sprite arrowSprite = LoadObjectiveMarkerSprite("marker_arrow_down.png");
        Sprite gestureSprite = LoadObjectiveMarkerSprite(ObjectiveMarkerIconName(gesture));
        if (backgroundSprite == null || arrowSprite == null || gestureSprite == null)
            return null;

        Vector3 center = visualRoot.transform.position;
        float top = center.y + 0.72f;
        if (TryGetRendererBounds(visualRoot, out Bounds bounds))
        {
            center = bounds.center;
            top = bounds.max.y + Mathf.Clamp(bounds.size.y * 0.18f, 0.22f, 0.48f);
        }

        GameObject marker = new GameObject("ObjectiveMarker");
        marker.transform.SetParent(visualRoot.transform.parent);
        marker.transform.position = new Vector3(center.x, top + 0.28f, center.z);
        marker.transform.localScale = Vector3.one;
        marker.AddComponent<BillboardToCamera>();

        CreateObjectiveMarkerSprite(
            "ObjectiveBadge",
            backgroundSprite,
            marker.transform,
            Vector3.zero,
            0.5f,
            Navy,
            40);
        CreateObjectiveMarkerSprite(
            "ObjectiveGestureIcon",
            gestureSprite,
            marker.transform,
            new Vector3(0f, 0f, -0.01f),
            0.31f,
            Cream,
            41);
        CreateObjectiveMarkerSprite(
            "ObjectiveArrow",
            arrowSprite,
            marker.transform,
            new Vector3(0f, -0.39f, -0.01f),
            0.19f,
            Amber,
            41);
        return marker;
    }

    private static string ObjectiveMarkerIconName(StoryInteractionGesture gesture)
    {
        return gesture switch
        {
            StoryInteractionGesture.DragToBag => "touch_swipe_move.png",
            StoryInteractionGesture.DragToTarget => "touch_swipe_move.png",
            StoryInteractionGesture.SwipeHorizontal => "touch_swipe_horizontal.png",
            StoryInteractionGesture.SwipeDown => "touch_swipe_down.png",
            StoryInteractionGesture.WorldHold => "touch_tap_hold.png",
            StoryInteractionGesture.RepeatedTap => "touch_tap_double.png",
            _ => "touch_tap.png"
        };
    }

    private static Sprite LoadObjectiveMarkerSprite(string fileName)
    {
        string path = KenneyInputPromptRoot + "/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;

        if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 128f ||
            importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 128;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreateObjectiveMarkerSprite(string name, Sprite sprite, Transform parent,
        Vector3 localPosition, float scale, Color color, int sortingOrder)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    internal static void SetGestureTarget(StoryInteractable interactable, Transform target)
    {
        if (interactable == null)
            return;
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("gestureTarget").objectReferenceValue = target;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static Animation CreateMoveAnimation(GameObject target, string assetName, Vector3 startOffset, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        Vector3 end = target.transform.localPosition;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            AnimationCurve.EaseInOut(0f, end.x + startOffset.x, duration, end.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y",
            AnimationCurve.EaseInOut(0f, end.y + startOffset.y, duration, end.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            AnimationCurve.EaseInOut(0f, end.z + startOffset.z, duration, end.z));
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static Animation CreateRotationAnimation(GameObject target, string assetName, Vector3 startEuler,
        Vector3 endEuler, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x",
            AnimationCurve.EaseInOut(0f, startEuler.x, duration, endEuler.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y",
            AnimationCurve.EaseInOut(0f, startEuler.y, duration, endEuler.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z",
            AnimationCurve.EaseInOut(0f, startEuler.z, duration, endEuler.z));
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static Animation CreateRockAnimation(GameObject target, string assetName, float degrees, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(duration * 0.25f, degrees),
            new Keyframe(duration * 0.5f, -degrees * 0.7f),
            new Keyframe(duration * 0.75f, degrees * 0.35f),
            new Keyframe(duration, 0f));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", curve);
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static ParticleSystem CreateDust(string name, Transform parent, Vector3 position, Materials materials,
        int burstCount = 22)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.71f, 0.64f, 0.52f, 0.5f));
        main.gravityModifier = 0.06f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.8f, 0.15f, 0.35f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = materials.dust;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return particles;
    }

    internal static AudioSource CreateAudioSource(string name, Transform parent, AudioClip clip, float volume,
        bool loop = false, bool playOnAwake = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = playOnAwake;
        source.spatialBlend = 0f;
        return source;
    }

    internal static AudioClip LoadLicensedSfx(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return AssetDatabase.LoadAssetAtPath<AudioClip>(LicensedSfxRoot + "/" + fileName);
    }

    internal static AudioSource CreateSpatialAudioSource(
        string name,
        Transform parent,
        Vector3 position,
        AudioClip clip,
        float volume,
        float spatialBlend = 0.72f,
        float minDistance = 1.4f,
        float maxDistance = 14f)
    {
        AudioSource source = CreateAudioSource(name, parent, clip, volume);
        source.transform.position = position;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.priority = 96;
        return source;
    }

    internal static AudioSource CreateLicensedAmbience(
        string name,
        Transform parent,
        string clipFileName,
        float volume)
    {
        AudioClip clip = LoadLicensedSfx(clipFileName);
        return clip != null
            ? CreateAudioSource(name, parent, clip, volume, true, true)
            : null;
    }

    internal static int AttachInteractionAudioLayer(Transform chapterRoot, string layerName)
    {
        if (chapterRoot == null)
            throw new ArgumentNullException(nameof(chapterRoot));

        Transform audioRoot = NewChild(chapterRoot, layerName);
        StoryInteractable[] interactions = chapterRoot.GetComponentsInChildren<StoryInteractable>(true)
            .OrderBy(interactable => interactable.InteractionId, StringComparer.Ordinal)
            .ToArray();
        int attached = 0;
        for (int i = 0; i < interactions.Length; i++)
        {
            StoryInteractable interactable = interactions[i];
            string clipName = ChooseInteractionSfx(interactable);
            AudioClip clip = LoadLicensedSfx(clipName);
            if (clip == null)
                continue;

            Vector3 position = interactable.InteractionPoint != null
                ? interactable.InteractionPoint.position
                : interactable.transform.position;
            AudioSource source = CreateSpatialAudioSource(
                "SFX_" + SanitizeAudioName(interactable.InteractionId),
                audioRoot,
                position,
                clip,
                InteractionSfxVolume(interactable));
            UnityEventTools.AddPersistentListener(interactable.OnInteracted, source.Play);
            attached++;
        }

        return attached;
    }

    private static string ChooseInteractionSfx(StoryInteractable interactable)
    {
        string id = (interactable.InteractionId ?? string.Empty).ToLowerInvariant();
        string prompt = (interactable.Prompt ?? string.Empty).ToLowerInvariant();
        string key = id + " " + prompt;
        int variant = StableAudioVariant(key);

        if (ContainsAny(key, "whistle", "düdük"))
            return Pick(variant, "sfx100v2_air_01.ogg", "sfx100v2_air_02.ogg", "sfx100v2_air_03.ogg");
        if (ContainsAny(key, "glass", "window", "cam", "pencere"))
            return Pick(variant, "sfx100v2_glass_02.ogg", "sfx100v2_glass_03.ogg", "sfx100v2_glass_05.ogg");
        if (ContainsAny(key, "door", "exit", "stair", "kapı", "çıkış", "merdiven"))
            return Pick(variant, "sfx100v2_door_01.ogg", "sfx100v2_door_03.ogg", "sfx100v2_door_05.ogg");
        if (ContainsAny(key, "rubble", "debris", "stone", "moloz", "enkaz", "taş"))
            return Pick(variant, "sfx100v2_stones_01.ogg", "sfx100v2_stones_02.ogg", "sfx100v2_stones_03.ogg");
        if (ContainsAny(key, "elevator", "rail", "cane", "radio", "metal", "asansör", "korkuluk", "baston"))
            return Pick(variant, "sfx100v2_metal_01.ogg", "sfx100v2_metal_03.ogg", "sfx100v2_metal_05.ogg");
        if (ContainsAny(key, "wardrobe", "shelf", "table", "book", "dolap", "raf", "masa", "kitap"))
            return Pick(variant, "sfx100v2_wood_01.ogg", "sfx100v2_wood_03.ogg", "sfx100v2_wood_hit_02.ogg");
        if (ContainsAny(key, "switch", "light", "flashlight", "fener", "ışık"))
            return Pick(variant, "sfx100v2_switch_01.ogg", "sfx100v2_switch_02.ogg");

        if (interactable.InteractionKind == StoryInteractionKind.UnsafeChoice)
            return Pick(variant, "sfx100v2_hit_01.ogg", "sfx100v2_metal_hit_01.ogg", "sfx100v2_wood_hit_03.ogg");
        if (interactable.InteractionGesture == StoryInteractionGesture.DragToBag ||
            interactable.InteractionGesture == StoryInteractionGesture.DragToTarget)
            return Pick(variant, "sfx100v2_items_01.ogg", "sfx100v2_items_02.ogg");
        if (interactable.InteractionGesture == StoryInteractionGesture.Approach)
            return Pick(variant, "sfx100v2_footstep_01.ogg", "sfx100v2_footstep_02.ogg");
        if (interactable.InteractionKind == StoryInteractionKind.Collect ||
            interactable.InteractionKind == StoryInteractionKind.HelpSibling)
            return Pick(variant, "sfx100v2_items_01.ogg", "sfx100v2_items_02.ogg");
        if (interactable.InteractionKind == StoryInteractionKind.TakeCover)
            return Pick(variant, "sfx100v2_wood_hit_01.ogg", "sfx100v2_wood_hit_02.ogg");

        return Pick(variant, "sfx100v2_switch_01.ogg", "sfx100v2_switch_02.ogg");
    }

    private static float InteractionSfxVolume(StoryInteractable interactable)
    {
        if (interactable.InteractionKind == StoryInteractionKind.UnsafeChoice)
            return 0.48f;
        if (interactable.InteractionKind == StoryInteractionKind.TakeCover)
            return 0.4f;
        if (interactable.InteractionGesture == StoryInteractionGesture.Approach)
            return 0.24f;
        return 0.34f;
    }

    private static int StableAudioVariant(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return hash & int.MaxValue;
        }
    }

    private static string Pick(int variant, params string[] choices)
    {
        return choices[variant % choices.Length];
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            if (value.Contains(needles[i]))
                return true;
        }

        return false;
    }

    private static string SanitizeAudioName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Interaction";

        char[] characters = value.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            if (!char.IsLetterOrDigit(characters[i]) && characters[i] != '_')
                characters[i] = '_';
        }

        return new string(characters);
    }

    internal static void BuildLighting(Transform parent, VolumeProfile profile, Color color, float intensity)
    {
        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.SetParent(parent);
        sunObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = color;
        sun.intensity = intensity;
        sun.shadows = LightShadows.None;

        GameObject fillObject = new GameObject("StoryFillLight");
        fillObject.transform.SetParent(parent);
        fillObject.transform.position = new Vector3(0f, 6f, -2f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.78f, 0.9f, 1f);
        fill.intensity = 2.1f;
        fill.range = 16f;
        fill.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("StoryGlobalVolume");
        volumeObject.transform.SetParent(parent);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    internal static void DisableShadows(Transform root)
    {
        if (root == null)
            return;

        foreach (Light light in root.GetComponentsInChildren<Light>(true))
            light.shadows = LightShadows.None;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    internal static void BuildNavigation(GameObject environment)
    {
        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>() ?? environment.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    internal static void ConfigureDynamicNavigationBlocker(GameObject root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        if (!TryGetRendererBounds(root, out Bounds bounds))
            throw new InvalidOperationException(root.name + " için dinamik kapı sınırı hesaplanamadı.");

        // FBX model-prefab köklerine NavMeshObstacle eklemek Unity 6'da yok edilmiş bir
        // component referansı döndürebiliyor. Kapının fizik ve navigasyon temsilini,
        // sahneye ait düzenlenebilir bir child üzerinde tut.
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);

        GameObject blocker = new GameObject("DynamicNavigationBlocker");
        blocker.transform.SetParent(root.transform, false);
        NavMeshModifier modifier = blocker.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        NavMeshObstacle obstacle = blocker.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;

        Vector3[] corners =
        {
            new(bounds.min.x, bounds.min.y, bounds.min.z),
            new(bounds.min.x, bounds.min.y, bounds.max.z),
            new(bounds.min.x, bounds.max.y, bounds.min.z),
            new(bounds.min.x, bounds.max.y, bounds.max.z),
            new(bounds.max.x, bounds.min.y, bounds.min.z),
            new(bounds.max.x, bounds.min.y, bounds.max.z),
            new(bounds.max.x, bounds.max.y, bounds.min.z),
            new(bounds.max.x, bounds.max.y, bounds.max.z)
        };
        Bounds local = new Bounds(root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(root.transform.InverseTransformPoint(corner));
        BoxCollider blockerCollider = blocker.AddComponent<BoxCollider>();
        blockerCollider.center = local.center;
        blockerCollider.size = local.size;
        obstacle.center = local.center;
        obstacle.size = local.size;
    }

    internal static void ConfigureSession(StoryGameManager manager, StoryAct act, StoryFlag[] initialFlags)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("initialAct").intValue = (int)act;
        SerializedProperty flags = serialized.FindProperty("initialFlags");
        flags.arraySize = initialFlags?.Length ?? 0;
        for (int i = 0; i < flags.arraySize; i++)
            flags.GetArrayElementAtIndex(i).intValue = (int)initialFlags[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void ConfigureRebuildStoryRoute(StoryGameManager manager)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("persistAcrossScenes").boolValue = true;
        serialized.FindProperty("loadExistingSave").boolValue = true;
        serialized.FindProperty("preparationSceneName").stringValue = RebuildPreparationSceneName;
        serialized.FindProperty("homeSafetySceneName").stringValue = RebuildHomeSafetySceneName;
        serialized.FindProperty("quakeSceneName").stringValue = RebuildQuakeSceneName;
        serialized.FindProperty("evacuationSceneName").stringValue = RebuildEvacuationSceneName;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void AddScenesToBuildSettings(params string[] scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (string path in scenePaths)
        {
            if (scenes.Any(scene => scene.path == path))
                continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
        }
        if (changed)
            EditorBuildSettings.scenes = scenes.ToArray();
    }

    internal static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent, bool collider = true, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider existing = go.GetComponent<Collider>();
        if (!collider && existing != null)
            Object.DestroyImmediate(existing);
        return go;
    }

    internal static GameObject InstantiateFurniture(string relativePath, string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler = default, bool keepColliders = true)
    {
        string path = FurnitureRoot + "/" + relativePath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Mobilya prefabı bulunamadı: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localScale = NormalizedAuthoredScale(instance.transform.localScale);
        instance.transform.rotation = Quaternion.Euler(euler);
        if (!keepColliders)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }
        FitToSize(instance, feetPosition, targetSize);
        return instance;
    }

    internal static GameObject InstantiateAsset(string assetPath, string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler = default, bool keepColliders = true, bool ensureCollider = false,
        Material materialOverride = null)
    {
        string path = assetPath.Replace('\\', '/');
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("3B prop asseti bulunamadı: " + path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        // Legacy prop prefabs often use a large root scale for import units and a smaller
        // non-uniform ratio for their intended silhouette. Keep the ratio, discard the magnitude.
        instance.transform.localScale = NormalizedAuthoredScale(instance.transform.localScale);
        instance.transform.rotation = Quaternion.Euler(euler);
        if (materialOverride != null)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = materialOverride;
                renderer.sharedMaterials = materials;
            }
        }
        if (!keepColliders)
        {
            // Bazı eski prop prefabları DraggableItem taşır; bu bileşen Collider zorunluluğu koyar.
            // Prop yeni Story sahnesine yalnız görsel olarak alınırken önce eski input köprüsünü sök.
            foreach (DraggableItem draggable in instance.GetComponentsInChildren<DraggableItem>(true))
                Object.DestroyImmediate(draggable);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }

        FitToSize(instance, feetPosition, targetSize);
        if (ensureCollider)
            EnsureCollider(instance);
        return instance;
    }

    internal static Transform CreatePoint(string name, Transform parent, Vector3 position, Vector3 lookAt)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent);
        point.transform.position = position;
        Vector3 direction = lookAt - position;
        if (direction.sqrMagnitude > 0.001f)
            point.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return point.transform;
    }

    internal static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    internal static void CreateWorldLabel(string name, string text, Vector3 position, Vector3 euler, float fontSize,
        Color color, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(euler);
        TextMeshPro label = root.AddComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PlayfulSemiboldFontAssetPath);
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableAutoSizing = false;
        label.rectTransform.sizeDelta = size;
    }

    internal static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
    }

    internal static void SetArray(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.arraySize = values?.Length ?? 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool emissive = false,
        Color emission = default)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateUnlitMaterial(string name, Color color)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip GetOrCreateLegacyClip(string assetName)
    {
        string path = AnimationRoot + "/" + assetName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = assetName, legacy = true, frameRate = 30f };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        return clip;
    }

    private static Animation AttachAnimation(GameObject target, AnimationClip clip)
    {
        bool wasActive = target.activeSelf;
        if (!wasActive)
            target.SetActive(true);
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.playAutomatically = false;
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        if (!wasActive)
            target.SetActive(false);
        return animation;
    }

    private static void FitToHeight(GameObject instance, Vector3 feetPosition, float targetHeight)
    {
        if (!TryGetRendererBounds(instance, out Bounds bounds))
        {
            instance.transform.position = feetPosition;
            return;
        }
        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        instance.transform.localScale *= scale;
        TryGetRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static void FitToSize(GameObject instance, Vector3 feetPosition, Vector3 targetSize)
    {
        if (!TryGetSizingBounds(instance, out Bounds bounds))
        {
            instance.transform.position = feetPosition;
            return;
        }
        // Imported props range from centimetre-scale meshes to models authored in metres.
        // A 1 cm denominator floor made small FBX/OBJ props permanently ten times too small.
        const float minimumDimension = 0.000001f;
        float scale = Mathf.Min(targetSize.x / Mathf.Max(minimumDimension, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(minimumDimension, bounds.size.y),
                targetSize.z / Mathf.Max(minimumDimension, bounds.size.z)));
        instance.transform.localScale *= scale;
        TryGetSizingBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static Vector3 NormalizedAuthoredScale(Vector3 authoredScale)
    {
        float magnitude = Mathf.Max(
            Mathf.Abs(authoredScale.x),
            Mathf.Max(Mathf.Abs(authoredScale.y), Mathf.Abs(authoredScale.z)));
        return magnitude < 0.0001f ? Vector3.one : authoredScale / magnitude;
    }

    private static bool TryGetSizingBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer =>
                renderer != null &&
                renderer.enabled &&
                renderer.gameObject.activeInHierarchy &&
                renderer is not ParticleSystemRenderer &&
                renderer is not TrailRenderer &&
                renderer is not LineRenderer &&
                renderer is not SpriteRenderer &&
                renderer.name.IndexOf("Marker", StringComparison.OrdinalIgnoreCase) < 0 &&
                renderer.name.IndexOf("Indicator", StringComparison.OrdinalIgnoreCase) < 0 &&
                renderer.name.IndexOf("Prompt", StringComparison.OrdinalIgnoreCase) < 0 &&
                renderer.name.IndexOf("Highlight", StringComparison.OrdinalIgnoreCase) < 0 &&
                renderer.name.IndexOf("Arrow", StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bool initialized = false;
        bounds = default;
        foreach (Renderer renderer in renderers)
        {
            Bounds local = renderer.localBounds;
            Matrix4x4 matrix = renderer.localToWorldMatrix;
            Vector3 min = local.min;
            Vector3 max = local.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = matrix.MultiplyPoint3x4(new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z));
                if (!initialized)
                {
                    bounds = new Bounds(corner, Vector3.zero);
                    initialized = true;
                }
                else
                    bounds.Encapsulate(corner);
            }
        }

        return initialized;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return true;
    }

    private static void EnsureCollider(GameObject root)
    {
        if (root.GetComponentsInChildren<Collider>(true).Length > 0)
            return;
        if (!TryGetRendererBounds(root, out Bounds bounds))
        {
            BoxCollider fallback = root.AddComponent<BoxCollider>();
            fallback.size = Vector3.one;
            return;
        }

        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
        };
        Bounds local = new Bounds(root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(root.transform.InverseTransformPoint(corner));
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = local.center;
        collider.size = local.size;
    }

    private static Quaternion LookAt(Vector3 position, Vector3 target)
    {
        return Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }

    internal static void LoadPlayfulStoryFonts(
        out TMP_FontAsset regular,
        out TMP_FontAsset semibold,
        out TMP_FontAsset bold)
    {
        regular = GetOrCreateStoryFontAsset(
            PlayfulFontRoot + "/Lexend-Regular.ttf",
            PlayfulRegularFontAssetPath,
            "Lexend Regular SDF");
        semibold = GetOrCreateStoryFontAsset(
            PlayfulFontRoot + "/Lexend-SemiBold.ttf",
            PlayfulSemiboldFontAssetPath,
            "Lexend SemiBold SDF");
        bold = GetOrCreateStoryFontAsset(
            PlayfulFontRoot + "/Lexend-Bold.ttf",
            PlayfulDisplayFontAssetPath,
            "Lexend Bold SDF");
        if (regular == null || semibold == null || bold == null)
            throw new InvalidOperationException("Hikâye fontları üretilemedi.");
    }

    private static TMP_FontAsset GetOrCreateStoryFontAsset(string sourcePath, string assetPath, string assetName)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
            throw new InvalidOperationException("Hikâye font kaynağı bulunamadı: " + sourcePath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            92,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            false);
        if (fontAsset == null)
            throw new InvalidOperationException("TextMesh Pro fontu üretilemedi: " + sourcePath);

        const string uiCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÇĞİÖŞÜçğıöşüÂâÎîÛû’“”•–—…";
        fontAsset.name = assetName;
        if (!fontAsset.TryAddCharacters(uiCharacters, out string missingCharacters))
            throw new InvalidOperationException(assetName + " eksik Türkçe karakterler içeriyor: " + missingCharacters);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.material.name = assetName + " Material";
        fontAsset.material.hideFlags = HideFlags.None;
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas == null || AssetDatabase.Contains(atlas))
                continue;
            atlas.name = assetName + " Atlas";
            atlas.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }
        if (!AssetDatabase.Contains(fontAsset.material))
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    internal static GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = (anchorMin + anchorMax) * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    internal static GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size,
        Color color,
        bool blocksRaycasts,
        StoryUIPanelStyle style = StoryUIPanelStyle.Flat)
    {
        GameObject panel = CreateUIRect(name, parent, anchorMin, anchorMax, position, size);
        Image image = panel.AddComponent<Image>();
        image.color = style == StoryUIPanelStyle.Flat ? color : Color.white;
        image.raycastTarget = blocksRaycasts;
        if (style != StoryUIPanelStyle.Flat)
        {
            bool playfulPanel = style is
                StoryUIPanelStyle.PlayfulNavyPanel or
                StoryUIPanelStyle.PlayfulBluePanel or
                StoryUIPanelStyle.PlayfulCreamPanel;
            bool playfulAsset = style is
                StoryUIPanelStyle.PlayfulYellowTag or
                StoryUIPanelStyle.PlayfulBlueTag or
                StoryUIPanelStyle.PlayfulYellowBadge or
                StoryUIPanelStyle.PlayfulBlueBadge or
                StoryUIPanelStyle.PlayfulPurpleBadge or
                StoryUIPanelStyle.PlayfulGreenBadge;
            if (playfulPanel)
            {
                image.sprite = LoadRoundedPanelSprite();
                image.type = Image.Type.Sliced;
                image.color = style switch
                {
                    StoryUIPanelStyle.PlayfulBluePanel => new Color32(65, 166, 245, 248),
                    StoryUIPanelStyle.PlayfulCreamPanel => new Color32(255, 247, 222, 250),
                    _ => new Color32(18, 30, 54, 250)
                };
                Shadow shadow = panel.AddComponent<Shadow>();
                shadow.effectColor = new Color32(8, 17, 35, 165);
                shadow.effectDistance = new Vector2(0f, -12f);
                shadow.useGraphicAlpha = true;
                Outline outline = panel.AddComponent<Outline>();
                outline.effectColor = style == StoryUIPanelStyle.PlayfulCreamPanel
                    ? new Color32(24, 39, 68, 255)
                    : new Color32(103, 202, 255, 255);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = true;
            }
            else if (playfulAsset)
            {
                string palette = style switch
                {
                    StoryUIPanelStyle.PlayfulBlueTag or StoryUIPanelStyle.PlayfulBlueBadge => "Blue",
                    StoryUIPanelStyle.PlayfulPurpleBadge => "Purple",
                    StoryUIPanelStyle.PlayfulGreenBadge => "Green",
                    _ => "Yellow"
                };
                image.sprite = LoadCasualUISprite(palette, "Normal");
                image.type = Image.Type.Sliced;
            }
            else
            {
                string fileName = style switch
                {
                    StoryUIPanelStyle.AdventurePaper => "panel_brown.png",
                    StoryUIPanelStyle.AdventureSteel => "panel_grey.png",
                    StoryUIPanelStyle.AdventureBanner => "banner_modern.png",
                    StoryUIPanelStyle.AdventureRoundPaper => "round_brown.png",
                    _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
                };
                image.sprite = LoadKenneyAdventureSprite(fileName);
                image.type = style is StoryUIPanelStyle.AdventureBanner or StoryUIPanelStyle.AdventureRoundPaper
                    ? Image.Type.Simple
                    : Image.Type.Sliced;
                image.preserveAspect = style == StoryUIPanelStyle.AdventureRoundPaper;
            }
        }
        return panel;
    }

    internal static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject go = CreateUIRect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, dimensions);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    internal static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchor,
        Vector2 position, Vector2 size, Color background, Color foreground)
    {
        bool pauseButton = string.Equals(name, "PauseButton", StringComparison.Ordinal);
        bool destructive = ApproximatelyColor(background, Coral);
        string palette = ButtonPalette(name, destructive);
        Color labelColor = palette == "Yellow" ? Navy : Color.white;

        GameObject go = CreateUIRect(name, parent, anchor, anchor, position, size);
        Image image = go.AddComponent<Image>();
        image.sprite = LoadCasualUISprite(palette, "Normal");
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = button.spriteState;
        spriteState.highlightedSprite = LoadCasualUISprite(palette, "Hover");
        spriteState.selectedSprite = spriteState.highlightedSprite;
        spriteState.pressedSprite = LoadCasualUISprite(palette, "Pressed");
        spriteState.disabledSprite = LoadCasualUISprite("Blue", "Pressed");
        button.spriteState = spriteState;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        TMP_FontAsset displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PlayfulDisplayFontAssetPath) ?? font;
        if (pauseButton)
        {
            CreatePauseBar(go.transform, "PauseBarLeft", -13f);
            CreatePauseBar(go.transform, "PauseBarRight", 13f);
        }
        else
        {
            float maxSize = size.y >= 150f ? 40f : size.y >= 108f ? 40f : 35f;
            TMP_Text text = CreateText("Label", go.transform, displayFont, maxSize, labelColor,
                TextAlignmentOptions.Center, new Vector2(0f, -4f), size - new Vector2(42f, 30f));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = size.y >= 150f ? 26f : size.y >= 108f ? 25f : 22f;
            text.fontSizeMax = maxSize;
            text.fontStyle = FontStyles.Normal;
            text.lineSpacing = -2f;
            if (palette != "Yellow")
            {
                text.outlineWidth = 0.08f;
                text.outlineColor = new Color32(20, 31, 58, 230);
            }
            text.text = label;
        }
        return button;
    }

    private static Animation CreateUIPresentationAnimation(
        GameObject target,
        string assetName,
        Vector2 slideOffset,
        float duration)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
            throw new InvalidOperationException(target.name + " için RectTransform bulunamadı.");

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();
        if (group == null)
            throw new InvalidOperationException(target.name + " için CanvasGroup oluşturulamadı.");
        group.alpha = 1f;
        Vector2 end = rect.anchoredPosition;
        Vector2 start = end + slideOffset;
        float settle = duration * 0.72f;
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        clip.SetCurve(string.Empty, typeof(RectTransform), "m_AnchoredPosition.x",
            AnimationCurve.EaseInOut(0f, start.x, duration, end.x));
        clip.SetCurve(string.Empty, typeof(RectTransform), "m_AnchoredPosition.y",
            AnimationCurve.EaseInOut(0f, start.y, duration, end.y));
        clip.SetCurve(string.Empty, typeof(RectTransform), "m_LocalScale.x",
            new AnimationCurve(
                new Keyframe(0f, 0.94f),
                new Keyframe(settle, 1.025f),
                new Keyframe(duration, 1f)));
        clip.SetCurve(string.Empty, typeof(RectTransform), "m_LocalScale.y",
            new AnimationCurve(
                new Keyframe(0f, 0.94f),
                new Keyframe(settle, 1.025f),
                new Keyframe(duration, 1f)));
        clip.SetCurve(string.Empty, typeof(CanvasGroup), "m_Alpha",
            AnimationCurve.EaseInOut(0f, 0f, duration * 0.68f, 1f));
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    private static void CreatePauseBar(Transform parent, string name, float x)
    {
        GameObject bar = CreateUIRect(
            name,
            parent,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(x, 5f),
            new Vector2(14f, 46f));
        Image image = bar.AddComponent<Image>();
        image.sprite = LoadRoundedPanelSprite();
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
        Outline outline = bar.AddComponent<Outline>();
        outline.effectColor = new Color32(20, 31, 58, 225);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static string ButtonPalette(string name, bool destructive)
    {
        if (destructive)
            return "Red";
        if (name.Contains("Preparation", StringComparison.Ordinal))
            return "Yellow";
        if (name.Contains("HomeSafety", StringComparison.Ordinal))
            return "Blue";
        if (name.Contains("Quake", StringComparison.Ordinal))
            return "Purple";
        if (name.Contains("Evacuation", StringComparison.Ordinal) ||
            name.Contains("NextAct", StringComparison.Ordinal))
            return "Green";
        if (name.Contains("Pause", StringComparison.Ordinal))
            return "Purple";
        if (name.Contains("NewStory", StringComparison.Ordinal) ||
            name.Contains("Retry", StringComparison.Ordinal) ||
            name.Contains("Replay", StringComparison.Ordinal) ||
            name.Contains("Close", StringComparison.Ordinal))
            return "Blue";
        return "Yellow";
    }

    internal static Image CreateCartoonIcon(
        string name,
        Transform parent,
        string styleFolder,
        string fileName,
        Vector2 position,
        Vector2 size)
    {
        GameObject iconObject = CreateUIRect(
            name,
            parent,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            position,
            size);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = LoadCartoonUISprite(styleFolder, fileName);
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = Color.white;
        icon.raycastTarget = false;
        return icon;
    }

    internal static void ApplyDisplayTextStyle(TMP_Text text)
    {
        if (text == null)
            return;
        text.fontStyle = FontStyles.Normal;
        text.outlineWidth = 0.08f;
        text.outlineColor = new Color32(15, 30, 46, 225);
        Shadow shadow = text.gameObject.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(8, 17, 28, 150);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = true;
    }

    internal static Sprite LoadCasualUISprite(string palette, string state)
    {
        string path = CasualUiRoot + "/Buttons/Btn_" + palette + "_Large_" + state + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Casual UI sprite bulunamadı: " + path);

        Vector4 border = new Vector4(48f, 42f, 48f, 46f);
        bool needsImport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.spritePixelsPerUnit != 100f ||
            importer.spriteBorder != border ||
            importer.mipmapEnabled ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (needsImport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Casual UI sprite import edilemedi: " + path);
        return sprite;
    }

    internal static Sprite LoadStoryLogoSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(StoryLogoPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Hikâye logosu bulunamadı: " + StoryLogoPath);

        bool needsImport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.maxTextureSize != 1024 ||
            importer.textureCompression != TextureImporterCompression.CompressedHQ;
        if (needsImport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = Vector4.zero;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StoryLogoPath);
        if (sprite == null)
            throw new InvalidOperationException("Hikâye logosu import edilemedi: " + StoryLogoPath);
        return sprite;
    }

    private static Sprite LoadRoundedPanelSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DepremUITheme.RoundedSpritePath);
        if (sprite == null)
            throw new InvalidOperationException("Yuvarlatılmış panel sprite'ı bulunamadı.");
        return sprite;
    }

    internal static Sprite LoadCartoonUISprite(string styleFolder, string fileName)
    {
        string path = CartoonUiRoot + "/" + styleFolder + "/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Cartoon UI sprite bulunamadı: " + path);

        Vector4 border = CartoonUiBorder(fileName);
        bool needsImport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.spritePixelsPerUnit != 100f ||
            importer.spriteBorder != border ||
            importer.mipmapEnabled ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (needsImport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Cartoon UI sprite import edilemedi: " + path);
        return sprite;
    }

    internal static Sprite LoadKenneyAdventureSprite(string fileName)
    {
        string path = KenneyUiAdventureRoot + "/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Kenney UI sprite bulunamadı: " + path);

        Vector4 border = KenneyAdventureBorder(fileName);
        bool needsImport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.spritePixelsPerUnit != 100f ||
            importer.spriteBorder != border ||
            importer.mipmapEnabled ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (needsImport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Kenney UI sprite import edilemedi: " + path);
        return sprite;
    }

    private static Vector4 KenneyAdventureBorder(string fileName)
    {
        if (fileName.StartsWith("panel_", StringComparison.Ordinal))
            return new Vector4(24f, 24f, 24f, 24f);
        if (fileName.StartsWith("button_", StringComparison.Ordinal))
            return new Vector4(18f, 14f, 18f, 14f);
        return Vector4.zero;
    }

    private static Vector4 CartoonUiBorder(string fileName)
    {
        if (fileName == "pop up window.png")
            return new Vector4(54f, 54f, 54f, 54f);
        if (fileName.StartsWith("button ", StringComparison.Ordinal))
            return new Vector4(32f, 27f, 32f, 31f);
        return Vector4.zero;
    }

    private static bool ApproximatelyColor(Color left, Color right)
    {
        Vector4 delta = (Vector4)(left - right);
        return delta.sqrMagnitude < 0.0001f;
    }
}
