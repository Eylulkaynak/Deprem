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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class StoryPreparationSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_01_BagPreparation.unity";
    public const string OriginalScenePath = "Assets/Scenes/bolum1.unity";
    public const string BackupScenePath = "Assets/Scenes/LegacyBackups/bolum1_OriginalGameplay_2026-07-16.unity";

    private const string StoryRoot = "Assets/Story";
    private const string GeneratedRoot = StoryRoot + "/Generated";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string PrefabRoot = StoryRoot + "/Prefabs/Preparation";
    private const string ItemRoot = "Assets/Bolum1Prefab";
    private const string FurnitureRoot = "Assets/ithappy/Cute_Furniture_Free/Prefabs";
    private const string OriginalOpenBagPath = "Assets/Sprites/Bolum1/Open Backpack/model.obj";
    private const string DenizCharacterPath = "Assets/KidsCharacterFree/Prefabs/Boy0_Humanoid.prefab";
    private const string CanCharacterPath = DenizCharacterPath;
    private const string CanTexturePath = StoryRoot + "/Characters/Variants/texture_can.png";
    private const string ParentCharacterPath = StoryRoot + "/Characters/ThirdParty/RGPolyFamily/Anne_Ayse.fbx";
    private const string ParentTexturePath = StoryRoot + "/Characters/ThirdParty/RGPolyFamily/RGPoly_CityAtlas.png";

    private static readonly Color Navy = new Color32(15, 30, 46, 255);
    private static readonly Color Teal = new Color32(25, 151, 151, 255);
    private static readonly Color Amber = new Color32(244, 173, 65, 255);
    private static readonly Color Coral = new Color32(224, 91, 82, 255);
    private static readonly Color Cream = new Color32(236, 226, 204, 255);
    private static readonly Color WallColor = new Color32(203, 196, 178, 255);
    private static readonly Color FloorColor = new Color32(105, 77, 59, 255);
    private static readonly Color Mauve = new Color32(151, 94, 118, 255);

    private sealed class Materials
    {
        public Material wall;
        public Material floor;
        public Material cream;
        public Material navy;
        public Material teal;
        public Material amber;
        public Material coral;
        public Material mauve;
        public Material wood;
        public Material glass;
        public Material safeGlow;
        public Material helpGlow;
        public Material hazardGlow;
        public Material denizCharacter;
        public Material canCharacter;
        public Material parentCharacter;
    }

    private sealed class WorldReferences
    {
        public GameObject environment;
        public Transform parentStand;
        public Transform bagFocus;
        public Transform exitShelfFocus;
        public Transform signalStand;
        public Transform foodStand;
        public Transform healthStand;
        public Transform warmthStand;
        public GameObject openBag;
        public GameObject closedBag;
        public GameObject wornBag;
        public GameObject exitBag;
        public GameObject completionPanel;
        public TMP_Text completionDetail;
        public Transform packedVisualRoot;
        public Transform[] packedSlots;
        public BagDropZone bagDropZone;
        public Transform effectsRoot;
    }

    private sealed class CharacterReferences
    {
        public GameObject deniz;
        public GameObject parent;
        public GameObject can;
        public Animator denizAnimator;
        public Animator parentAnimator;
        public Animator canAnimator;
    }

    private sealed class InteractionReferences
    {
        public StoryInteractable startFamilyPlan;
        public StoryInteractable inspectBag;
        public StoryInteractable reviewSignal;
        public StoryInteractable reviewFood;
        public StoryInteractable reviewHealth;
        public StoryInteractable reviewWarmth;
        public StoryInteractable testBag;
        public StoryInteractable adjustStraps;
        public StoryInteractable placeAtExit;
        public StoryPreparationItem[] items;
    }

    private sealed class ItemDefinition
    {
        public string id;
        public string prefab;
        public string displayName;
        public StoryPreparationCategory category;
        public bool recommended;
        public StoryFlag flag;
        public string childLine;
        public string parentLine;
        public StoryInteractionGesture gesture;
        public int gestureCount;
        public Vector3 position;
        public Vector3 standPosition;
        public float yaw;
        public Vector3 size;
    }

    [MenuItem("Tools/Deprem Story/Build Story_01_BagPreparation")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Tools/Deprem Story/Build Story_01_BagPreparation (Silent)")]
    public static void BuildSilentFromMenu()
    {
        Build(false);
    }

    public static void BuildFromCommandLine()
    {
        Build(false);
    }

    private static void Build(bool showDialog)
    {
        try
        {
            EnsureFolders();
            EnsureOriginalSceneBackup();
            EnsureCharacterAssets();
            RuntimeAnimatorController storyController = StoryAnimationLibraryBuilder.BuildLibrary(false);
            RuntimeAnimatorController adultController = StoryAnimationLibraryBuilder.LoadAdultController();
            Materials materials = CreateMaterials();
            VolumeProfile volumeProfile = CreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_01_BAG_PREPARATION");
            WorldReferences world = BuildWorld(root.transform, materials);
            CharacterReferences characters = BuildCharacters(root.transform, materials, storyController, adultController);

            GameObject sessionRoot = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionRoot.AddComponent<StoryGameManager>();
            ConfigurePreparationSession(gameManager);

            GameObject core = new GameObject("_StoryPreparationCore");
            core.transform.SetParent(root.transform);
            StoryPreparationDirector director = core.AddComponent<StoryPreparationDirector>();

            StoryCameraController cameraController = BuildCameras(root.transform, characters.deniz.transform, world, out Camera mainCamera, out CinemachineBrain brain);
            StoryUIController ui = BuildUI(root.transform, cameraController, world);

            StoryPlayerMovement movement = ConfigurePlayer(characters.deniz);
            SetReference(ui, "movementOwner", movement);
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            SetReference(touch, "worldCamera", mainCamera);
            SetReference(touch, "player", movement);
            SetReference(touch, "ui", ui);
            SetReference(touch, "cameraController", cameraController);
            SerializedObject touchSettings = new SerializedObject(touch);
            touchSettings.FindProperty("directWorldGestures").boolValue = true;
            touchSettings.ApplyModifiedPropertiesWithoutUndo();

            InteractionReferences interactions = BuildInteractions(root.transform, materials, world, characters, director);
            ConfigureDirector(director, gameManager, movement, touch, cameraController, ui, characters, world, interactions);
            SetReference(cameraController, "brain", brain);
            SetReference(ui, "cameraController", cameraController);

            BuildLighting(root.transform, volumeProfile);
            BuildAmbientAudio(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            BuildNavigation(world.environment);
            SaveReusablePrefabs(world, characters);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsWithoutChangingStartupScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Selection.activeGameObject = GameObject.Find("STORY_01_BAG_PREPARATION");
            Debug.Log("Story_01_BagPreparation built successfully: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Çanta hazırlama hikâye sahnesi ve orijinal bolum1 yedeği oluşturuldu.", "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Çanta hazırlama sahnesi kurulamadı:\n" + exception.Message, "Kapat");
            throw;
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Scenes/LegacyBackups");
        EnsureFolder(StoryRoot);
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(StoryRoot + "/Prefabs");
        EnsureFolder(PrefabRoot);
        EnsureFolder(StoryRoot + "/Characters");
        EnsureFolder(StoryRoot + "/Characters/ThirdParty");
        EnsureFolder(StoryRoot + "/Characters/Variants");
        EnsureFolder(StoryRoot + "/Characters/ThirdParty/RGPolyFamily");
    }

    private static void EnsureCharacterAssets()
    {
        if (!File.Exists(Path.GetFullPath(ParentCharacterPath)))
            throw new FileNotFoundException("CC0 RG Poly anne karakteri bulunamadı.", ParentCharacterPath);
        if (!File.Exists(Path.GetFullPath(DenizCharacterPath)))
            throw new FileNotFoundException("Kids Character Free Deniz karakteri bulunamadı.", DenizCharacterPath);
        if (!File.Exists(Path.GetFullPath(CanCharacterPath)))
            throw new FileNotFoundException("Kids Character Free Can karakteri bulunamadı.", CanCharacterPath);

        ConfigurePaletteTexture(CanTexturePath);
        ConfigurePaletteTexture(ParentTexturePath);
        ConfigureHumanoidCharacter(ParentCharacterPath);
    }

    private static void ConfigurePaletteTexture(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Character texture not found: " + assetPath);
        if (importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
            importer.textureCompression == TextureImporterCompression.Uncompressed)
            return;

        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ConfigureHumanoidCharacter(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Character ModelImporter bulunamadı: " + assetPath);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void EnsureOriginalSceneBackup()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OriginalScenePath) == null)
            throw new InvalidOperationException("Orijinal bolum1 sahnesi bulunamadı: " + OriginalScenePath);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BackupScenePath) != null)
            return;

        if (!AssetDatabase.CopyAsset(OriginalScenePath, BackupScenePath))
            throw new InvalidOperationException("Orijinal bolum1 sahnesinin yedeği oluşturulamadı.");
        AssetDatabase.ImportAsset(BackupScenePath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static Materials CreateMaterials()
    {
        return new Materials
        {
            wall = GetOrCreateMaterial("Prep_Wall", WallColor, 0.18f),
            floor = GetOrCreateMaterial("Prep_WarmFloor", FloorColor, 0.28f),
            cream = GetOrCreateMaterial("Cream", Cream, 0.24f),
            navy = GetOrCreateMaterial("Navy", Navy, 0.22f),
            teal = GetOrCreateMaterial("Teal", Teal, 0.28f),
            amber = GetOrCreateMaterial("Amber", Amber, 0.24f),
            coral = GetOrCreateMaterial("Coral", Coral, 0.24f),
            mauve = GetOrCreateMaterial("Prep_Mauve", Mauve, 0.25f),
            wood = GetOrCreateMaterial("Wood", new Color32(123, 83, 61, 255), 0.3f),
            glass = GetOrCreateMaterial("WindowGlass", new Color32(70, 113, 133, 255), 0.72f, true, new Color(0.02f, 0.08f, 0.12f)),
            safeGlow = GetOrCreateMaterial("InteractionSafeGlow", Teal, 0.12f, true, Teal * 2.3f),
            helpGlow = GetOrCreateMaterial("InteractionHelpGlow", Amber, 0.12f, true, Amber * 2.3f),
            hazardGlow = GetOrCreateMaterial("InteractionHazardGlow", Coral, 0.12f, true, Coral * 2.3f),
            // Deniz was already visually approved. Keep Boy0's authored material intact.
            denizCharacter = null,
            canCharacter = GetOrCreateTexturedMaterial("Can_Boy0Variant",
                CanTexturePath, string.Empty, Color.white, 0.08f),
            parentCharacter = GetOrCreateTexturedMaterial("Anne_RGPoly_Character", ParentTexturePath,
                string.Empty, Color.white, 0.08f)
        };
    }

    private static Material GetOrCreateTexturedMaterial(string name, string baseTexturePath, string normalTexturePath,
        Color tint, float smoothness)
    {
        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexturePath);
        Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath);
        if (baseTexture == null)
            throw new InvalidOperationException("Karakter dokusu bulunamadı: " + baseTexturePath);

        Material material = GetOrCreateMaterial(name, tint, smoothness);
        material.SetTexture("_BaseMap", baseTexture);
        if (normalTexture != null)
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool emissive = false, Color emission = default)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static VolumeProfile CreateVolumeProfile()
    {
        string path = GeneratedRoot + "/Story_Preparation_Volume.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.14f);
        bloom.threshold.Override(1.15f);
        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(0.02f);
        color.contrast.Override(7f);
        color.saturation.Override(-2f);
        color.colorFilter.Override(new Color(1f, 0.96f, 0.88f));
        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.13f);
        vignette.smoothness.Override(0.38f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static WorldReferences BuildWorld(Transform parent, Materials m)
    {
        WorldReferences world = new WorldReferences();
        GameObject environment = new GameObject("Environment_PreparationHome");
        environment.transform.SetParent(parent);
        world.environment = environment;
        Transform room = NewChild(environment.transform, "FamilyRoom");

        CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0.5f), new Vector3(11f, 0.24f, 12f), m.floor, room);
        CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 1.75f, 6.45f), new Vector3(11f, 3.5f, 0.22f), m.wall, room);
        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-5.45f, 1.75f, 0.5f), new Vector3(0.22f, 3.5f, 12f), m.wall, room);
        CreatePrimitive("RightLowWall", PrimitiveType.Cube, new Vector3(5.45f, 0.58f, 0.5f), new Vector3(0.22f, 1.16f, 12f), m.wall, room);
        CreatePrimitive("BackSkirting", PrimitiveType.Cube, new Vector3(0f, 0.16f, 6.25f), new Vector3(10.6f, 0.16f, 0.11f), m.cream, room, false);
        CreatePrimitive("LeftSkirting", PrimitiveType.Cube, new Vector3(-5.25f, 0.16f, 0.5f), new Vector3(0.11f, 0.16f, 11.6f), m.cream, room, false);

        CreatePrimitive("RugBorder", PrimitiveType.Cube, new Vector3(0.25f, 0.02f, -0.4f), new Vector3(5.8f, 0.045f, 4.5f), m.navy, room, false);
        CreatePrimitive("RugMain", PrimitiveType.Cube, new Vector3(0.25f, 0.045f, -0.4f), new Vector3(5.45f, 0.045f, 4.15f), m.teal, room, false);
        CreatePrimitive("RugInset", PrimitiveType.Cube, new Vector3(0.25f, 0.072f, -0.4f), new Vector3(4.65f, 0.012f, 3.35f), m.cream, room, false);

        BuildWindow(room, m);
        InstantiateFurniture("Furniture/Couch_11.prefab", "FamilyCouch", room, new Vector3(-3.7f, 0f, 4.7f), new Vector3(0f, 180f, 0f), new Vector3(3.1f, 1.45f, 1.35f));
        InstantiateFurniture("Furniture/Coffee_Table_03.prefab", "CanActivityTable", room, new Vector3(-2.8f, 0f, 2.9f), new Vector3(0f, 15f, 0f), new Vector3(1.8f, 0.72f, 1.15f));
        InstantiateFurniture("Plants/Plants_19.prefab", "WindowPlant", room, new Vector3(4.55f, 0f, 5.55f), Vector3.zero, new Vector3(0.85f, 1.55f, 0.85f));
        InstantiateFurniture("Furniture/Nightstand_02.prefab", "ExitLowShelf", room, new Vector3(4.4f, 0f, 4.75f), new Vector3(0f, 180f, 0f), new Vector3(1.35f, 1.05f, 0.85f));
        InstantiateFurniture("Decorations/Toy_02.prefab", "CanToy", room, new Vector3(-2.35f, 0.55f, 2.72f), new Vector3(0f, -20f, 0f), new Vector3(0.5f, 0.45f, 0.5f));

        BuildPreparationStations(room, m);
        BuildWallDecor(room, m);
        BuildOpenBag(world, room, m);
        BuildBagVariants(world, room, m);
        world.effectsRoot = NewChild(parent, "PreparedWrongChoiceConsequences");

        world.parentStand = CreateFocus("ParentConversationPoint", new Vector3(0.85f, 0f, -0.1f), room);
        world.bagFocus = CreateFocus("OpenBagInteractionPoint", new Vector3(0.25f, 0f, -1.95f), room);
        world.exitShelfFocus = CreateFocus("ExitShelfInteractionPoint", new Vector3(3.9f, 0f, 4.15f), room);
        world.signalStand = CreateFocus("SignalStationPoint", new Vector3(-2.55f, 0f, 0.9f), room);
        world.foodStand = CreateFocus("FoodStationPoint", new Vector3(-0.7f, 0f, 3.65f), room);
        world.healthStand = CreateFocus("HealthStationPoint", new Vector3(3.25f, 0f, 1.9f), room);
        world.warmthStand = CreateFocus("WarmthStationPoint", new Vector3(-2.65f, 0f, 3.75f), room);
        return world;
    }

    private static void BuildWindow(Transform room, Materials m)
    {
        CreatePrimitive("WindowFrame", PrimitiveType.Cube, new Vector3(2.6f, 2.15f, 6.25f), new Vector3(3.05f, 1.65f, 0.15f), m.wood, room, false);
        CreatePrimitive("WindowGlass", PrimitiveType.Cube, new Vector3(2.6f, 2.15f, 6.15f), new Vector3(2.65f, 1.3f, 0.06f), m.glass, room, false);
        CreatePrimitive("WindowCrossV", PrimitiveType.Cube, new Vector3(2.6f, 2.15f, 6.08f), new Vector3(0.09f, 1.32f, 0.08f), m.wood, room, false);
        CreatePrimitive("WindowCrossH", PrimitiveType.Cube, new Vector3(2.6f, 2.15f, 6.08f), new Vector3(2.68f, 0.09f, 0.08f), m.wood, room, false);
        CreatePrimitive("CurtainLeft", PrimitiveType.Cube, new Vector3(0.9f, 2.05f, 6.02f), new Vector3(0.44f, 2.55f, 0.12f), m.mauve, room, false);
        CreatePrimitive("CurtainRight", PrimitiveType.Cube, new Vector3(4.3f, 2.05f, 6.02f), new Vector3(0.44f, 2.55f, 0.12f), m.mauve, room, false);
        CreatePrimitive("CurtainRod", PrimitiveType.Cylinder, new Vector3(2.6f, 3.42f, 6.0f), new Vector3(0.07f, 1.92f, 0.07f), m.navy, room, false).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private static void BuildPreparationStations(Transform room, Materials m)
    {
        BuildStation("SignalStation", new Vector3(-4.15f, 0f, 0.9f), new Vector3(2.2f, 0.78f, 3.8f), m.teal, "IŞIK & HABERLEŞME", room, m);
        BuildStation("FoodStation", new Vector3(-0.7f, 0f, 4.9f), new Vector3(3.5f, 0.78f, 1.25f), m.amber, "SU & GIDA", room, m);
        BuildStation("HealthStation", new Vector3(4.45f, 0f, 1.95f), new Vector3(1.45f, 0.78f, 3.6f), m.coral, "SAĞLIK & BELGELER", room, m);
        CreateWorldLabel("WarmthLabel", "SICAK KALMA", new Vector3(-3.55f, 2.72f, 6.22f), new Vector3(0f, 180f, 0f), 0.25f, m.amber, room);
    }

    private static void BuildStation(string name, Vector3 position, Vector3 size, Material accent, string label, Transform room, Materials m)
    {
        Transform station = NewChild(room, name);
        const string tablePath =
            "Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Work_Table_06.prefab";
        bool splitAlongZ = size.z > size.x;
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            Vector3 tablePosition = position + (splitAlongZ
                ? new Vector3(0f, 0f, side * size.z * 0.24f)
                : new Vector3(side * size.x * 0.24f, 0f, 0f));
            Vector3 tableSize = splitAlongZ
                ? new Vector3(size.x, 0.86f, size.z * 0.54f)
                : new Vector3(size.x * 0.54f, 0.86f, size.z);
            StoryChapterBuilderCommon.InstantiateAsset(
                tablePath, "WorkTable_" + (i + 1), station, tablePosition, tableSize,
                splitAlongZ ? new Vector3(0f, 90f, 0f) : Vector3.zero, true, true);
        }
        Vector3 labelPosition = position + new Vector3(0f, 1.42f, size.z * 0.48f);
        CreateWorldLabel(name + "Label", label, labelPosition, new Vector3(0f, 180f, 0f), 0.2f, accent, station);
    }

    private static void BuildWallDecor(Transform room, Materials m)
    {
        StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Picture_08.prefab",
            "FamilyPlanFrame", room, new Vector3(-2.15f, 1.76f, 6.08f),
            new Vector3(1.65f, 1.15f, 0.18f), Vector3.zero, false, false);
        CreateWorldLabel("FamilyPlanLabel", "AİLE AFET PLANI", new Vector3(-2.15f, 2.73f, 6.05f), new Vector3(0f, 180f, 0f), 0.16f, m.navy, room);
    }

    private static void BuildOpenBag(WorldReferences world, Transform room, Materials m)
    {
        GameObject bag = InstantiateModelAsset(OriginalOpenBagPath, "OpenEmergencyBag_OriginalBolum1", room,
            new Vector3(1.15f, 0.08f, -0.85f), new Vector3(0f, 30f, 0f), new Vector3(1.75f, 1.05f, 1.45f));
        NewChild(bag.transform, "OriginalBolum1BagSource");
        world.openBag = bag;
        if (!TryGetCombinedRendererBounds(bag, out Bounds bounds))
            throw new InvalidOperationException("Orijinal bolum1 çantasının sınırları hesaplanamadı.");

        BuildBagPhysicalBlocker(bag.transform, bounds);
        world.packedVisualRoot = NewChild(room, "PackedItemVisuals_OriginalBag");
        BuildOriginalBagDropZone(world, room, bounds);
    }

    private static void BuildBagPhysicalBlocker(Transform bag, Bounds bagBounds)
    {
        GameObject blocker = new GameObject("OpenBag_PhysicalBlocker");
        blocker.transform.SetParent(bag, true);
        blocker.transform.position = new Vector3(
            bagBounds.center.x,
            Mathf.Lerp(bagBounds.min.y, bagBounds.max.y, 0.42f),
            bagBounds.center.z);
        blocker.transform.rotation = Quaternion.identity;
        blocker.transform.localScale = Vector3.one;

        BoxCollider collider = blocker.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = new Vector3(
            Mathf.Max(0.62f, bagBounds.size.x * 0.88f),
            Mathf.Max(0.42f, bagBounds.size.y * 0.76f),
            Mathf.Max(0.58f, bagBounds.size.z * 0.84f));
    }

    private static void BuildBagVariants(WorldReferences world, Transform room, Materials m)
    {
        world.closedBag = BuildOriginalBagVariant("ClosedEmergencyBag_OriginalBolum1", new Vector3(1.15f, 0.03f, -0.85f),
            Quaternion.Euler(0f, 30f, -8f), room, new Vector3(1.55f, 0.94f, 1.28f));
        world.closedBag.SetActive(false);

        world.wornBag = BuildOriginalBagVariant("Deniz_WornPreparedBag", Vector3.zero,
            Quaternion.Euler(10f, 180f, 0f), room, new Vector3(1.2f, 0.72f, 0.98f));
        world.wornBag.SetActive(false);

        world.exitBag = BuildOriginalBagVariant("PreparedBag_ExitShelf", new Vector3(4.4f, 0.72f, 4.73f),
            Quaternion.Euler(0f, 210f, -5f), room, new Vector3(1.15f, 0.7f, 0.94f));
        world.exitBag.SetActive(false);
    }

    private static GameObject BuildOriginalBagVariant(string name, Vector3 position, Quaternion rotation, Transform parent, Vector3 targetSize)
    {
        GameObject bag = InstantiateModelAsset(OriginalOpenBagPath, name, parent, position, rotation.eulerAngles, targetSize);
        NewChild(bag.transform, "OriginalBolum1BagSource");
        return bag;
    }

    private static void BuildOriginalBagDropZone(WorldReferences world, Transform parent, Bounds bagBounds)
    {
        GameObject zoneRoot = new GameObject("OriginalBolum1_BagDropZone");
        zoneRoot.transform.SetParent(parent);
        zoneRoot.transform.position = new Vector3(bagBounds.center.x,
            Mathf.Lerp(bagBounds.min.y, bagBounds.max.y, 0.63f), bagBounds.center.z);

        BoxCollider collider = zoneRoot.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(bagBounds.size.x * 0.72f, bagBounds.size.y * 0.52f, bagBounds.size.z * 0.68f);
        BagDropZone dropZone = zoneRoot.AddComponent<BagDropZone>();
        world.bagDropZone = dropZone;

        Transform opening = CreateFocus("OriginalBagOpening", new Vector3(bagBounds.center.x,
            bagBounds.max.y + 0.08f, bagBounds.center.z), parent);
        Transform inside = CreateFocus("OriginalBagInside", new Vector3(bagBounds.center.x,
            Mathf.Lerp(bagBounds.min.y, bagBounds.max.y, 0.66f), bagBounds.center.z), parent);

        // Yanlış seçimler de kendi fiziksel dünya nesnelerine sahip. Yeni seçenekler eklendiğinde
        // çanta yerleşim havuzu sahne üretimini sınırlamasın diye güvenli bir üst kapasite tutuyoruz.
        world.packedSlots = new Transform[32];
        for (int index = 0; index < world.packedSlots.Length; index++)
        {
            int column = index % 4;
            int row = index / 4;
            float x = Mathf.Lerp(bagBounds.min.x + bagBounds.size.x * 0.27f,
                bagBounds.max.x - bagBounds.size.x * 0.27f, column / 3f);
            float z = Mathf.Lerp(bagBounds.min.z + bagBounds.size.z * 0.27f,
                bagBounds.max.z - bagBounds.size.z * 0.27f, row / 4f);
            float y = inside.position.y + row * 0.018f;
            world.packedSlots[index] = CreateFocus("OriginalBag_ItemSlot_" + index.ToString("00"), new Vector3(x, y, z), parent);
        }

        SerializedObject serialized = new SerializedObject(dropZone);
        serialized.FindProperty("openingPoint").objectReferenceValue = opening;
        serialized.FindProperty("insidePoint").objectReferenceValue = inside;
        SerializedProperty slots = serialized.FindProperty("itemSlots");
        slots.arraySize = world.packedSlots.Length;
        for (int index = 0; index < world.packedSlots.Length; index++)
            slots.GetArrayElementAtIndex(index).objectReferenceValue = world.packedSlots[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CharacterReferences BuildCharacters(Transform parent, Materials m,
        RuntimeAnimatorController controller, RuntimeAnimatorController adultController)
    {
        // Keep character silhouettes entirely mesh-authored; never attach Unity primitives as body or clothing parts.
        CharacterReferences refs = new CharacterReferences();
        refs.deniz = BuildCharacter("Deniz_12", DenizCharacterPath,
            new Vector3(-0.65f, 0f, -2.35f), 1.56f, parent, controller, m.denizCharacter, "CharacterSource_Boy0_Deniz");
        refs.parent = BuildCharacter("Anne_Ayse", ParentCharacterPath,
            new Vector3(2.35f, 0f, -0.1f), 1.68f, parent, adultController, m.parentCharacter,
            "CharacterSource_RGPoly_Anne");
        refs.can = BuildCharacter("Can_8", CanCharacterPath,
            new Vector3(-2.8f, 0f, 2.35f), 1.26f, parent, controller, m.canCharacter, "CharacterSource_Boy0Variant_Can");

        refs.deniz.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        refs.parent.transform.rotation = Quaternion.Euler(0f, -28f, 0f);
        refs.can.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
        refs.denizAnimator = refs.deniz.GetComponentInChildren<Animator>();
        refs.parentAnimator = refs.parent.GetComponentInChildren<Animator>();
        refs.canAnimator = refs.can.GetComponentInChildren<Animator>();

        foreach (GameObject character in new[] { refs.deniz, refs.parent, refs.can })
        {
            Sample.KidsScript legacyMovement = character.GetComponent<Sample.KidsScript>();
            if (legacyMovement != null)
                Object.DestroyImmediate(legacyMovement);
            foreach (CharacterController legacyController in character.GetComponentsInChildren<CharacterController>(true))
                Object.DestroyImmediate(legacyController);
        }

        EnsureCapsule(refs.parent, 0.28f, 1.68f);
        EnsureCapsule(refs.can, 0.23f, 1.22f);
        return refs;
    }

    private static GameObject BuildCharacter(string name, string assetPath, Vector3 feetPosition, float height,
        Transform parent, RuntimeAnimatorController controller, Material overrideMaterial, string sourceMarker)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            throw new InvalidOperationException("Humanoid karakter prefabı bulunamadı: " + assetPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = name;
        instance.transform.SetParent(parent);
        FitCharacterToHeight(instance, feetPosition, height);
        if (overrideMaterial != null)
            ApplySharedMaterial(instance, overrideMaterial);
        NewChild(instance.transform, sourceMarker);
        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid && candidate.isHuman);
            if (avatar != null)
            {
                animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
            }
        }
        if (animator == null)
            throw new InvalidOperationException(name + " karakterinde Animator bulunamadı.");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return instance;
    }

    private static void ApplySharedMaterial(GameObject root, Material material)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
                materials[index] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private static StoryPlayerMovement ConfigurePlayer(GameObject deniz)
    {
        NavMeshAgent agent = deniz.AddComponent<NavMeshAgent>();
        agent.speed = 1.75f;
        agent.acceleration = 9f;
        agent.angularSpeed = 540f;
        agent.radius = 0.24f;
        agent.height = 1.48f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 0.15f;
        CapsuleCollider collider = deniz.AddComponent<CapsuleCollider>();
        collider.radius = 0.24f;
        collider.height = 1.46f;
        collider.center = new Vector3(0f, 0.73f, 0f);
        Rigidbody body = deniz.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        StoryPlayerMovement movement = deniz.AddComponent<StoryPlayerMovement>();
        SetReference(movement, "animator", deniz.GetComponentInChildren<Animator>());
        return movement;
    }

    private static StoryCameraController BuildCameras(Transform parent, Transform player, WorldReferences world,
        out Camera mainCamera, out CinemachineBrain brain)
    {
        GameObject cameraRoot = new GameObject("PreparationCameras");
        cameraRoot.transform.SetParent(parent);
        GameObject main = new GameObject("Main Camera");
        main.tag = "MainCamera";
        main.transform.SetParent(cameraRoot.transform);
        main.transform.position = new Vector3(10.8f, 11.8f, -13.8f);
        main.transform.rotation = LookAt(main.transform.position, new Vector3(0f, 0.9f, 0.8f));
        mainCamera = main.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(18, 24, 31, 255);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 120f;
        mainCamera.allowHDR = true;
        main.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = main.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        brain = main.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.74f);

        StoryCameraBinding[] bindings =
        {
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.PreparationOverview, "CM_PreparationOverview",
                new Vector3(10.8f, 11.4f, -14.5f), new Vector3(0f, 0.95f, 0.8f), 47f, player, 20.2f, new Vector2(-0.15f, 0.18f)),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationBag, "CM_PreparationBag",
                new Vector3(3.9f, 3.0f, -4.9f), new Vector3(1.15f, 0.45f, -0.85f), 39f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationParent, "CM_PreparationParent",
                new Vector3(5.4f, 3.2f, -3.4f), new Vector3(2.1f, 1.02f, -0.1f), 39f),
            // Kategori planlarında açık çanta ön planda, seçilebilir masa eşyaları
            // arka planda kalır. Böylece oyuncu ekrandaki bir düğmeye değil,
            // doğrudan dünyadaki eşyaya dokunup çantaya sürükler.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationSignal, "CM_PreparationSignal",
                new Vector3(4.978f, 2.94f, -1.151f), new Vector3(-1.65f, 0.7f, 0.75f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationFood, "CM_PreparationFood",
                new Vector3(1.148f, 3.018f, -4.822f), new Vector3(-0.335f, 0.7f, 2.155f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationHealth, "CM_PreparationHealth",
                new Vector3(-2.16f, 2.709f, -2.192f), new Vector3(2.965f, 0.7f, 1.265f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationWarmth, "CM_PreparationWarmth",
                new Vector3(2.448f, 3.249f, -4.86f), new Vector3(-1.475f, 0.7f, 1.935f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationWrongChoice, "CM_PreparationWrongChoice",
                new Vector3(3.55f, 2.35f, -3.15f), new Vector3(1.15f, 0.45f, -0.85f), 38f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationBagFit, "CM_PreparationBagFit",
                new Vector3(4.8f, 3.6f, -7.1f), new Vector3(-0.65f, 0.82f, -2.35f), 46f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.PreparationExitShelf, "CM_PreparationExitShelf",
                new Vector3(2.0f, 2.85f, 1.4f), new Vector3(4.15f, 0.9f, 4.68f), 45f)
        };

        GameObject controllerObject = new GameObject("MissionCameraController_Preparation");
        controllerObject.transform.SetParent(cameraRoot.transform);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        serialized.FindProperty("initialZone").intValue = (int)StoryCameraZoneId.PreparationOverview;
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

    private static StoryCameraBinding MakeCamera(Transform parent, StoryCameraZoneId zone, string name, Vector3 position, Vector3 target, float fov)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = LookAt(position, target);
        CinemachineCamera camera = go.AddComponent<CinemachineCamera>();
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 120f;
        camera.Lens = lens;
        camera.Priority = 0;
        return new StoryCameraBinding { zone = zone, camera = camera };
    }

    private static StoryCameraBinding MakeFollowCamera(Transform parent, StoryCameraZoneId zone, string name,
        Vector3 position, Vector3 target, float fov, Transform player, float distance, Vector2 screenPosition)
    {
        StoryCameraBinding binding = MakeCamera(parent, zone, name, position, target, fov);
        CinemachineCamera camera = binding.camera;
        camera.Follow = player;
        CinemachinePositionComposer composer = camera.gameObject.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = distance;
        composer.TargetOffset = new Vector3(0f, 0.82f, 0f);
        composer.Damping = new Vector3(0.42f, 0.3f, 0.56f);
        composer.DeadZoneDepth = 0.45f;
        composer.CenterOnActivate = false;
        composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.22f, Smoothing = 9f, IgnoreY = true };
        ScreenComposerSettings composition = ScreenComposerSettings.Default;
        composition.ScreenPosition = screenPosition;
        composition.DeadZone.Enabled = true;
        composition.DeadZone.Size = new Vector2(0.12f, 0.08f);
        composition.HardLimits.Enabled = true;
        composition.HardLimits.Size = new Vector2(0.72f, 0.62f);
        composer.Composition = composition;
        return binding;
    }

    private static StoryUIController BuildUI(Transform parent, StoryCameraController cameraController, WorldReferences world)
    {
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Regular SDF.asset");
        TMP_FontAsset semibold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-SemiBold SDF.asset");
        TMP_FontAsset bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Bold SDF.asset");
        if (regular == null || semibold == null || bold == null)
            throw new InvalidOperationException("Inter TMP fontları bulunamadı.");

        GameObject canvasObject = new GameObject("StoryUI_Preparation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = CreateUIRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();

        GameObject objective = CreatePanel("ObjectiveStrip", safeArea.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -118f), new Vector2(780f, 196f), new Color(0.045f, 0.09f, 0.13f, 0.94f), false);
        TMP_Text objectiveTitle = CreateText("ObjectiveTitle", objective.transform, bold, 28f, Amber, TextAlignmentOptions.Left,
            new Vector2(0f, 44f), new Vector2(680f, 42f));
        TMP_Text objectiveDetail = CreateText("ObjectiveDetail", objective.transform, regular, 24f, Color.white, TextAlignmentOptions.Left,
            new Vector2(0f, -38f), new Vector2(680f, 108f));
        Button pause = CreateButton("PauseButton", safeArea.transform, "II", bold, new Vector2(1f, 1f),
            new Vector2(-36f, -64f), new Vector2(96f, 96f), Navy, Cream);

        GameObject subtitlePanel = CreatePanel("SubtitlePanel", safeArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 205f), new Vector2(940f, 176f), new Color(0.02f, 0.04f, 0.06f, 0.91f), false);
        TMP_Text subtitle = CreateText("SubtitleText", subtitlePanel.transform, regular, 28f, Color.white, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(870f, 142f));
        subtitlePanel.SetActive(false);

        GameObject pauseOverlay = CreatePanel("PauseOverlay", safeArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.005f, 0.012f, 0.018f, 0.74f), true);
        GameObject pauseCard = CreatePanel("PausePanel", pauseOverlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(820f, 760f), new Color(0.035f, 0.07f, 0.1f, 0.99f), true);
        CreateText("PauseTitle", pauseCard.transform, bold, 44f, Cream, TextAlignmentOptions.Center,
            new Vector2(0f, 275f), new Vector2(680f, 70f)).text = "OYUN DURAKLATILDI";
        CreateText("PauseHint", pauseCard.transform, regular, 23f, new Color(0.76f, 0.82f, 0.84f), TextAlignmentOptions.Center,
            new Vector2(0f, 200f), new Vector2(660f, 80f)).text = "Eşya kararları ve tamamlanan kategori kontrol noktaları yerel olarak saklanır.";
        Button resume = CreateButton("ResumeButton", pauseCard.transform, "DEVAM ET", bold, Vector2.one * 0.5f,
            new Vector2(0f, 35f), new Vector2(560f, 104f), Teal, Color.white);
        Button retry = CreateButton("RetryCheckpointButton", pauseCard.transform, "SON KONTROL NOKTASINA DÖN", semibold, Vector2.one * 0.5f,
            new Vector2(0f, -100f), new Vector2(560f, 104f), new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        CreateText("RetryHint", pauseCard.transform, regular, 20f, new Color(0.7f, 0.76f, 0.79f), TextAlignmentOptions.Center,
            new Vector2(0f, -205f), new Vector2(650f, 52f)).text = "Çantaya eklediğin kalıcı parçalar korunur.";
        pauseOverlay.SetActive(false);

        GameObject completion = CreatePanel("PreparationCompletionCard", safeArea.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -40f), new Vector2(900f, 560f), new Color(0.035f, 0.07f, 0.1f, 0.97f), true);
        CreateText("CompletionEyebrow", completion.transform, semibold, 23f, Amber, TextAlignmentOptions.Center,
            new Vector2(0f, 205f), new Vector2(740f, 42f)).text = "1. PERDE • HAZIRLIK";
        CreateText("CompletionTitle", completion.transform, bold, 46f, Cream, TextAlignmentOptions.Center,
            new Vector2(0f, 130f), new Vector2(760f, 70f)).text = "AFET ÇANTASI HAZIR";
        TMP_Text completionDetail = CreateText("CompletionDetail", completion.transform, regular, 26f, Color.white, TextAlignmentOptions.Center,
            new Vector2(0f, 22f), new Vector2(760f, 130f));
        completionDetail.text = "Temel parçalar ailece kontrol edildi.";
        CreateText("CompletionSafety", completion.transform, semibold, 23f, Teal, TextAlignmentOptions.Center,
            new Vector2(0f, -103f), new Vector2(760f, 82f)).text = "Sarsıntı sırasında çantaya koşma. Önce korun; sarsıntı durduktan sonra çıkarken al.";
        Button replay = CreateButton("ReplayPreparationButton", completion.transform, "SAHNEYİ YENİDEN OYNA", semibold, Vector2.one * 0.5f,
            new Vector2(0f, -210f), new Vector2(580f, 94f), Teal, Color.white);
        completion.SetActive(false);
        world.completionPanel = completion;
        world.completionDetail = completionDetail;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(parent);

        StoryUIController controller = canvasObject.AddComponent<StoryUIController>();
        SetReference(controller, "objectiveTitle", objectiveTitle);
        SetReference(controller, "objectiveDetail", objectiveDetail);
        SetReference(controller, "subtitle", subtitle);
        SetReference(controller, "pausePanel", pauseOverlay);
        SetReference(controller, "cameraController", cameraController);
        UnityEventTools.AddPersistentListener(pause.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(resume.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(retry.onClick, controller.RetryCheckpoint);
        UnityEventTools.AddPersistentListener(replay.onClick, controller.ReplayStory);
        return controller;
    }

    private static InteractionReferences BuildInteractions(Transform parent, Materials m, WorldReferences world,
        CharacterReferences characters, StoryPreparationDirector director)
    {
        InteractionReferences refs = new InteractionReferences();
        Transform interactionRoot = NewChild(parent, "PreparationInteractions");

        refs.startFamilyPlan = CreateHotspot("Meet_Parent", characters.parent.transform.position + Vector3.up * 0.9f,
            new Vector3(1.25f, 1.8f, 1.25f), "ANNEYLE AİLE PLANINI BAŞLAT", StoryInteractionKind.HelpSibling,
            world.parentStand, interactionRoot, m, StoryInteractionGesture.Tap, 1, 1.2f, StoryCameraZoneId.PreparationParent, false, StoryCameraZoneId.PreparationOverview, 3.2f);
        refs.inspectBag = CreateHotspot("Inspect_EmptyBag", world.openBag.transform.position + Vector3.up * 0.55f,
            new Vector3(1.55f, 1.0f, 1.35f), "ÇANTANIN CEPLERİNİ AÇ", StoryInteractionKind.Inspect,
            world.bagFocus, interactionRoot, m, StoryInteractionGesture.Tap, 1, 1.45f, StoryCameraZoneId.PreparationBag, false, StoryCameraZoneId.PreparationOverview, 2.6f);
        SetInteractFromAnywhere(refs.inspectBag);

        List<StoryPreparationItem> builtItems = new List<StoryPreparationItem>();
        ItemDefinition[] definitions = CreateItemDefinitions();
        for (int index = 0; index < definitions.Length; index++)
        {
            Transform stand = StandFor(definitions[index].category, world);
            StoryPreparationItem item = BuildDecisionItem(definitions[index], index, stand, world, m, director, interactionRoot);
            builtItems.Add(item);
        }
        refs.items = builtItems.ToArray();

        refs.reviewSignal = BuildParentReview("Review_Signal", "ANNEYLE IŞIK CEBİNİ KONTROL ET", characters.parent.transform,
            world.parentStand, m, director.ReviewSignalCategory);
        refs.reviewFood = BuildParentReview("Review_Food", "ANNEYLE SU VE GIDAYI KONTROL ET", characters.parent.transform,
            world.parentStand, m, director.ReviewFoodCategory);
        refs.reviewHealth = BuildParentReview("Review_Health", "ANNEYLE SAĞLIK DOSYASINI KONTROL ET", characters.parent.transform,
            world.parentStand, m, director.ReviewHealthCategory);
        refs.reviewWarmth = BuildParentReview("Review_Warmth", "ANNEYLE KIYAFETLERİ KONTROL ET", characters.parent.transform,
            world.parentStand, m, director.ReviewWarmthCategory);

        refs.testBag = CreateHotspot("Final_TestBagWeight", world.openBag.transform.position + Vector3.up * 0.55f,
            new Vector3(1.65f, 1.1f, 1.45f), "DİZLERİNİ BÜKÜP ÇANTAYI KALDIR", StoryInteractionKind.Collect,
            world.bagFocus, interactionRoot, m, StoryInteractionGesture.WorldHold, 1, 1.15f, StoryCameraZoneId.PreparationBag, false,
            StoryCameraZoneId.PreparationOverview, 1.4f);
        SetInteractFromAnywhere(refs.testBag);
        refs.adjustStraps = CreateHotspot("Final_AdjustStraps", characters.deniz.transform.position + Vector3.up * 0.85f,
            new Vector3(1.1f, 1.55f, 1.1f), "İKİ ASKINI EŞİTLE", StoryInteractionKind.Collect,
            CreateFocus("StrapAdjustmentPoint", characters.deniz.transform.position + new Vector3(0f, 0f, -0.5f), characters.deniz.transform),
            characters.deniz.transform, m, StoryInteractionGesture.SwipeHorizontal, 1, 1.6f, StoryCameraZoneId.PreparationBagFit, false,
            StoryCameraZoneId.PreparationOverview, 1.4f);
        SetInteractFromAnywhere(refs.adjustStraps);
        refs.placeAtExit = CreateHotspot("Final_PlaceBagAtExit", new Vector3(4.4f, 0.85f, 4.75f),
            new Vector3(1.5f, 1.65f, 1.3f), "ÇANTAYI ALÇAK RAFA BIRAK", StoryInteractionKind.Exit,
            world.exitShelfFocus, interactionRoot, m, StoryInteractionGesture.Tap, 1, 2.5f, StoryCameraZoneId.PreparationExitShelf, false,
            StoryCameraZoneId.PreparationOverview, 1.4f);
        SetInteractFromAnywhere(refs.placeAtExit);

        UnityEventTools.AddPersistentListener(refs.startFamilyPlan.OnInteracted, director.OnFamilyPlanStarted);
        UnityEventTools.AddPersistentListener(refs.inspectBag.OnInteracted, director.OnBagInspected);
        UnityEventTools.AddPersistentListener(refs.testBag.OnInteracted, director.OnBagWeightTested);
        UnityEventTools.AddPersistentListener(refs.adjustStraps.OnInteracted, director.OnBagStrapsAdjusted);
        UnityEventTools.AddPersistentListener(refs.placeAtExit.OnInteracted, director.OnBagPlacedAtExit);
        return refs;
    }

    private static StoryInteractable BuildParentReview(string name, string prompt, Transform parent, Transform interactionPoint,
        Materials m, UnityEngine.Events.UnityAction action)
    {
        StoryInteractable review = CreateHotspot(name, parent.position + Vector3.up * 0.9f, new Vector3(1.25f, 1.8f, 1.25f),
            prompt, StoryInteractionKind.HelpSibling, interactionPoint, parent, m, StoryInteractionGesture.Tap, 1, 1.2f,
            StoryCameraZoneId.PreparationParent, false, StoryCameraZoneId.PreparationOverview, 3.2f);
        UnityEventTools.AddPersistentListener(review.OnInteracted, action);
        return review;
    }

    private static ItemDefinition[] CreateItemDefinitions()
    {
        return new[]
        {
            Item("Flashlight", "Item_Fener.prefab", "El feneri", StoryPreparationCategory.Signal, true, StoryFlag.BagFlashlight,
                "Elektrikler kesilirse zemini bununla görebiliriz.", "Doğru. Feneri kapalıyken dene; ışığı insanların gözüne değil yürüdüğün zemine tut.",
                StoryInteractionGesture.Tap, 1, new Vector3(-4.45f, 0.92f, -0.05f), new Vector3(-3.65f, 0f, -0.55f), 12f, new Vector3(0.48f, 0.28f, 0.48f)),
            Item("Batteries", "Item_Pil.prefab", "Yedek pil", StoryPreparationCategory.Signal, true, StoryFlag.BagBatteries,
                "Fenerin yanına yedek pil de koyuyorum.", "Pilleri kutupları birbirine değmeyecek biçimde ayrı poşette sakla; düzenli aralıklarla son kullanma tarihini kontrol ederiz.",
                StoryInteractionGesture.RepeatedTap, 2, new Vector3(-3.75f, 0.92f, -0.05f), new Vector3(-3.35f, 0f, -0.55f), -18f, new Vector3(0.36f, 0.24f, 0.36f)),
            Item("Radio", "Radio.prefab", "Pilli radyo", StoryPreparationCategory.Signal, true, StoryFlag.BagRadio,
                "İnternet olmazsa resmi duyuruları buradan dinleriz.", "Evet. Pilli radyo, telefon şebekesi çalışmadığında yetkililerin duyurularını almamıza yardım eder.",
                StoryInteractionGesture.SwipeHorizontal, 1, new Vector3(-4.45f, 0.92f, 0.75f), new Vector3(-3.65f, 0f, 0.35f), 8f, new Vector3(0.52f, 0.35f, 0.4f)),
            Item("Whistle", "whistle.prefab", "Düdük", StoryPreparationCategory.Signal, true, StoryFlag.BagWhistle,
                "Sesimizi fazla yormadan yardım çağırabiliriz.", "Doğru. Düdük dış cepte ve kolay erişilir yerde kalmalı; oyun için değil, yerimizi belli etmek için kullanılır.",
                StoryInteractionGesture.Tap, 1, new Vector3(-3.72f, 0.92f, 0.75f), new Vector3(-3.35f, 0f, 0.35f), -12f, new Vector3(0.34f, 0.24f, 0.34f)),
            Item("Tablet", "Item_Tablet.prefab", "Tablet", StoryPreparationCategory.Signal, false, StoryFlag.None,
                "Tableti koyarsak hem haber bakar hem oyun oynarız.", "Şarjı bitebilir ve ağırdır. Önce pilsiz de işe yarayan temel parçalar; yer kalırsa yetişkinler güç kaynağını ayrıca değerlendirir.",
                StoryInteractionGesture.Tap, 1, new Vector3(-4.45f, 0.92f, 1.55f), new Vector3(-3.65f, 0f, 1.15f), 15f, new Vector3(0.52f, 0.18f, 0.42f)),
            Item("GameConsole", "GameConsole_01 Variant.prefab", "Oyun konsolu", StoryPreparationCategory.Signal, false, StoryFlag.None,
                "Can sıkılmasın diye oyun konsolunu da alalım mı?", "Bu model ağır ve şarja bağlı. Çocuklar için küçük kartlar ile kalem ekleyebiliriz; taşıma önceliği su ve güvenlik malzemelerinde.",
                StoryInteractionGesture.Tap, 1, new Vector3(-3.72f, 0.92f, 1.55f), new Vector3(-3.35f, 0f, 1.15f), -20f, new Vector3(0.42f, 0.25f, 0.34f)),
            Item("DeskLamp", "Light_05.prefab", "Prize bağlı masa lambası", StoryPreparationCategory.Signal, false, StoryFlag.None,
                "Masa lambası fenerden daha çok ışık verir.", "Elektrik kesildiğinde çalışmaz ve taşınabilir değildir. Çantada pilli fener ile yedek pil önceliklidir.",
                StoryInteractionGesture.Tap, 1, new Vector3(-4.45f, 0.92f, 2.35f), new Vector3(-3.65f, 0f, 1.95f), 10f, new Vector3(0.42f, 0.52f, 0.42f)),
            Item("DesktopComputer", "Computer_01.prefab", "Masaüstü bilgisayar", StoryPreparationCategory.Signal, false, StoryFlag.None,
                "Duyuruları bilgisayardan takip edebiliriz.", "Elektrik ve internet kesilebilir; ayrıca çantaya sığmaz. Resmî duyurular için pilli radyo daha güvenilir bir yedektir.",
                StoryInteractionGesture.SwipeHorizontal, 1, new Vector3(-3.72f, 0.92f, 2.35f), new Vector3(-3.35f, 0f, 1.95f), -12f, new Vector3(0.5f, 0.46f, 0.4f)),

            Item("Water", "Item_Su.prefab", "İçme suyu", StoryPreparationCategory.Food, true, StoryFlag.BagWater,
                "Her aile üyesi için içme suyu ayırıyoruz.", "Doğru. Sızdırmayan dayanıklı şişeyi dik bölmeye koyar, miktarı ve tarihini düzenli kontrol ederiz.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(-1.82f, 0.92f, 4.55f), new Vector3(-1.55f, 0f, 3.55f), 5f, new Vector3(0.32f, 0.58f, 0.32f)),
            Item("Food", "konserve.prefab", "Dayanıklı gıda", StoryPreparationCategory.Food, true, StoryFlag.BagFood,
                "Çabuk bozulmayan yiyecek seçiyorum.", "Evet. Yüksek kalorili, dayanıklı ve kolay açılan gıdalar uygundur; tarihlerini ailece yenileriz.",
                StoryInteractionGesture.RepeatedTap, 2, new Vector3(-0.85f, 0.92f, 4.55f), new Vector3(-0.75f, 0f, 3.55f), -8f, new Vector3(0.38f, 0.34f, 0.38f)),
            Item("GlassBottle", "camSise.prefab", "Cam şişe", StoryPreparationCategory.Food, false, StoryFlag.None,
                "Bu cam şişeye de su doldurabiliriz.", "Cam kırılıp çantayı ve elleri tehlikeye atabilir. Su için sızdırmayan, dayanıklı ve hafif bir kap seçeriz.",
                StoryInteractionGesture.Tap, 1, new Vector3(0.15f, 0.92f, 4.55f), new Vector3(0.1f, 0f, 3.55f), 10f, new Vector3(0.28f, 0.56f, 0.28f)),
            Item("Pan", "Item_Tava.prefab", "Tava", StoryPreparationCategory.Food, false, StoryFlag.None,
                "Yemek yapmak için tavayı da koyalım mı?", "Tava çantayı ağırlaştırır ve ilk 72 saatin temel ihtiyacı değildir. Hazır tüketilebilen dayanıklı gıdayı seçeriz.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(1.12f, 0.92f, 4.55f), new Vector3(0.85f, 0f, 3.55f), 22f, new Vector3(0.62f, 0.22f, 0.62f)),
            Item("Utensils", "Utensils_01.prefab", "Büyük mutfak gereçleri", StoryPreparationCategory.Food, false, StoryFlag.None,
                "Mutfak gereçlerini de yanımıza alalım mı?", "Hacim ve ağırlık kazandırırlar. Çantada kolay açılan, doğrudan tüketilebilen gıda ile içme suyuna yer ayırırız.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(-1.35f, 0.92f, 5.16f), new Vector3(-1.25f, 0f, 3.55f), -8f, new Vector3(0.52f, 0.28f, 0.42f)),
            Item("CuttingBoard", "Cutting_board_02.prefab", "Kesme tahtası", StoryPreparationCategory.Food, false, StoryFlag.None,
                "Yiyecek hazırlamak için kesme tahtasını koyalım.", "Afet çantasındaki gıda hazırlık gerektirmemeli. Kesme tahtası yerine hazır ve dayanıklı gıdayı taşırız.",
                StoryInteractionGesture.Tap, 1, new Vector3(0.15f, 0.92f, 5.16f), new Vector3(0.1f, 0f, 3.55f), 16f, new Vector3(0.54f, 0.12f, 0.38f)),

            Item("FirstAid", "FirstAidKit.prefab", "İlk yardım çantası", StoryPreparationCategory.Health, true, StoryFlag.BagFirstAid,
                "Küçük yaralanmalar için ilk yardım çantasını alıyorum.", "Doğru. İçeriğini yetişkinler kontrol eder; çocuklar bilmedikleri ilaç veya tıbbi malzemeyi tek başına kullanmaz.",
                StoryInteractionGesture.Tap, 1, new Vector3(4.1f, 0.92f, 0.68f), new Vector3(3.8f, 0f, 0.35f), 0f, new Vector3(0.5f, 0.34f, 0.4f)),
            Item("Documents", "dockument.prefab", "Belge kopyaları", StoryPreparationCategory.Health, true, StoryFlag.BagDocuments,
                "Kimlik ve önemli belgelerin kopyalarını dosyaya koyuyorum.", "Evet. Asıllar değil, gerekli fotokopiler su geçirmez dosyada; aile ve evcil hayvan bilgileri de burada olur.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(4.78f, 0.92f, 0.68f), new Vector3(3.8f, 0f, 1.0f), -6f, new Vector3(0.44f, 0.16f, 0.52f)),
            Item("Notebook", "Notebook_01.prefab", "Not defteri ve kalem", StoryPreparationCategory.Health, true, StoryFlag.BagNotebook,
                "Telefon numaralarını kâğıda da yazalım.", "Çok iyi. Şarj biterse buluşma yeri, şehir dışı irtibat kişisi ve önemli numaralar kâğıtta kalır.",
                StoryInteractionGesture.RepeatedTap, 2, new Vector3(4.1f, 0.92f, 1.65f), new Vector3(3.8f, 0f, 1.65f), 8f, new Vector3(0.46f, 0.16f, 0.48f)),
            Item("Soap", "soap.prefab", "Sabun", StoryPreparationCategory.Health, true, StoryFlag.BagSoap,
                "Ellerimizi temiz tutmak için sabun.", "Doğru. Hijyen malzemelerini yiyecekten ayrı, sızdırmayan küçük bir poşette saklarız.",
                StoryInteractionGesture.Tap, 1, new Vector3(4.78f, 0.92f, 1.65f), new Vector3(3.8f, 0f, 2.3f), -10f, new Vector3(0.34f, 0.24f, 0.34f)),
            Item("WetWipes", "Toallas Benzal.prefab", "Islak mendil", StoryPreparationCategory.Health, true, StoryFlag.BagWetWipes,
                "Su kullanamadığımızda ıslak mendil işe yarar.", "Evet. Paketi kapalı tutar, kuruyup kurumadığını düzenli kontrol ederiz.",
                StoryInteractionGesture.SwipeHorizontal, 1, new Vector3(4.1f, 0.92f, 2.62f), new Vector3(3.8f, 0f, 2.95f), 5f, new Vector3(0.46f, 0.18f, 0.36f)),
            Item("HealthToy", "Toy_03.prefab", "Büyük oyuncak", StoryPreparationCategory.Health, false, StoryFlag.None,
                "Can rahatlasın diye büyük oyuncağı da koyalım mı?", "Rahatlatıcı küçük bir eşya düşünülebilir ama bu oyuncak temel sağlık malzemelerinin yerini kaplıyor. Önceliğimiz ilk yardım ve hijyen.",
                StoryInteractionGesture.Tap, 1, new Vector3(4.78f, 0.92f, 2.62f), new Vector3(3.8f, 0f, 2.95f), -8f, new Vector3(0.42f, 0.38f, 0.42f)),
            Item("HealthNovel", "Book_08.prefab", "Kalın roman", StoryPreparationCategory.Health, false, StoryFlag.None,
                "Beklerken okumak için bu kalın kitabı alalım.", "Moral eşyaları küçük ve hafif seçilir. Bu kitap sağlık ve aile bilgisi bölümünde gereksiz ağırlık oluşturur.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(4.42f, 0.92f, 3.38f), new Vector3(3.8f, 0f, 3.2f), 12f, new Vector3(0.42f, 0.18f, 0.5f)),

            Item("Blanket", "Quilt_514.prefab", "Hafif battaniye", StoryPreparationCategory.Warmth, true, StoryFlag.BagBlanket,
                "Gece hava soğursa hafif battaniye kullanırız.", "Doğru. Kuru kalacak biçimde sıkıştırırız; ağır yorgan yerine taşınabilir battaniye veya uyku tulumu seçilir.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(-4.1f, 1.02f, 4.72f), new Vector3(-3.65f, 0f, 3.85f), 0f, new Vector3(0.78f, 0.3f, 0.62f)),
            Item("Clothing", "T-shirt.prefab", "Yedek kıyafet", StoryPreparationCategory.Warmth, true, StoryFlag.BagClothing,
                "Mevsime uygun yedek kıyafeti katlıyorum.", "Evet. Çorap ve iklime uygun kıyafet kuru poşette kalır; çocukların bedeni büyüdükçe ölçüyü yenileriz.",
                StoryInteractionGesture.SwipeHorizontal, 1, new Vector3(-3.05f, 1.02f, 4.72f), new Vector3(-2.95f, 0f, 3.85f), -12f, new Vector3(0.68f, 0.24f, 0.58f)),
            Item("HeavyQuilt", "Quilt_514.prefab", "Ağır yorgan", StoryPreparationCategory.Warmth, false, StoryFlag.None,
                "Daha kalın olduğu için ağır yorganı alalım mı?", "Çok yer kaplar ve çantayı taşınamaz hâle getirir. Kuru kalacak hafif battaniye veya uyku tulumu seçeriz.",
                StoryInteractionGesture.SwipeDown, 1, new Vector3(-2.25f, 1.02f, 4.72f), new Vector3(-2.35f, 0f, 3.85f), 7f, new Vector3(0.82f, 0.42f, 0.68f)),
            Item("WarmthBook", "Book_08.prefab", "Kalın kitap", StoryPreparationCategory.Warmth, false, StoryFlag.None,
                "Soğukta içeride beklersek bu kitabı okuyabiliriz.", "Çanta sınırlı. Küçük bir moral eşyası düşünülebilir ama sıcak kalma bölümünde battaniye ve kuru kıyafet önceliklidir.",
                StoryInteractionGesture.Tap, 1, new Vector3(-3.25f, 0.92f, 2.72f), new Vector3(-3.05f, 0f, 3.6f), -10f, new Vector3(0.42f, 0.18f, 0.5f)),
            Item("Guitar", "Guitar_01.prefab", "Gitar", StoryPreparationCategory.Warmth, false, StoryFlag.None,
                "Gitarı da götürsek moralimiz düzelir.", "Gitar değerli olabilir ama afet çantasına sığmaz ve ellerimizi meşgul eder. Tahliyede hafif ve gerekli eşyaları taşırız.",
                StoryInteractionGesture.SwipeHorizontal, 1, new Vector3(-2.45f, 0.92f, 3.05f), new Vector3(-2.55f, 0f, 3.6f), 18f, new Vector3(0.78f, 0.24f, 0.46f))
        };
    }

    private static ItemDefinition Item(string id, string prefab, string displayName, StoryPreparationCategory category,
        bool recommended, StoryFlag flag, string childLine, string parentLine, StoryInteractionGesture gesture, int gestureCount,
        Vector3 position, Vector3 standPosition, float yaw, Vector3 size)
    {
        return new ItemDefinition
        {
            id = id,
            prefab = prefab,
            displayName = displayName,
            category = category,
            recommended = recommended,
            flag = flag,
            childLine = childLine,
            parentLine = parentLine,
            gesture = gesture,
            gestureCount = gestureCount,
            position = position,
            standPosition = standPosition,
            yaw = yaw,
            size = size
        };
    }

    private static StoryPreparationItem BuildDecisionItem(ItemDefinition definition, int index, Transform categoryStand,
        WorldReferences world, Materials m, StoryPreparationDirector director, Transform interactionRoot)
    {
        GameObject wrapper = new GameObject("PreparationItem_" + definition.id);
        wrapper.transform.SetParent(interactionRoot);
        wrapper.transform.position = definition.position;
        GameObject visual = InstantiateItemPrefab(definition.prefab, definition.displayName + "_World", wrapper.transform,
            definition.position, definition.yaw, definition.size, true);

        BoxCollider choiceCollider = wrapper.AddComponent<BoxCollider>();
        choiceCollider.center = new Vector3(0f, Mathf.Max(0.28f, definition.size.y * 0.5f), 0f);
        choiceCollider.size = new Vector3(Mathf.Max(0.62f, definition.size.x * 1.45f), Mathf.Max(0.62f, definition.size.y * 1.45f), Mathf.Max(0.62f, definition.size.z * 1.45f));
        choiceCollider.isTrigger = false;

        // Orijinal bolum1 oynanışının eşyanın çanta ağzına uçup içine inme davranışı.
        // Girdi StoryTouchManager'da kalır; DraggableItem burada yalnız fiziksel animasyonu yürütür.
        DraggableItem bagMotion = wrapper.AddComponent<DraggableItem>();
        SerializedObject bagMotionData = new SerializedObject(bagMotion);
        bagMotionData.FindProperty("isCorrectItem").boolValue = definition.recommended;
        bagMotionData.FindProperty("displayName").stringValue = definition.displayName;
        bagMotionData.FindProperty("inputEnabled").boolValue = false;
        bagMotionData.FindProperty("notifyGameManager").boolValue = false;
        bagMotionData.FindProperty("tapToBagEnabled").boolValue = false;
        bagMotionData.FindProperty("moveToOpeningDuration").floatValue = 0.38f;
        bagMotionData.FindProperty("descendIntoBagDuration").floatValue = 0.3f;
        bagMotionData.FindProperty("moveToOpeningArcHeight").floatValue = 0.34f;
        bagMotionData.FindProperty("hideItemInBag").boolValue = false;
        bagMotionData.FindProperty("visibleInBagMultiplier").floatValue = 0.42f;
        bagMotionData.ApplyModifiedPropertiesWithoutUndo();
        Vector3 accessibleStand = definition.category switch
        {
            StoryPreparationCategory.Signal => new Vector3(-2.55f, 0f, definition.position.z),
            StoryPreparationCategory.Food => new Vector3(definition.position.x, 0f, 3.65f),
            StoryPreparationCategory.Health => new Vector3(3.25f, 0f, definition.position.z),
            _ => new Vector3(definition.position.x, 0f, 3.75f)
        };
        Transform interactionPoint = CreateFocus(definition.id + "StandPoint", accessibleStand, wrapper.transform);
        StoryInteractable interactable = AddInteractable(wrapper, "Choose_" + definition.id,
            (definition.recommended ? "ÇANTAYA SÜRÜKLE: " : "ÇANTADA DENE: ") + definition.displayName.ToUpperInvariant(),
            definition.recommended ? StoryInteractionKind.Collect : StoryInteractionKind.UnsafeChoice,
            interactionPoint, StoryInteractionGesture.DragToBag, 1, 1.15f,
            CategoryZone(definition.category), false, StoryCameraZoneId.PreparationOverview, 1.2f, m);

        Vector3 packedPosition = PackedSlotPosition(index, world);
        GameObject packedVisual = InstantiateItemPrefab(definition.prefab, definition.displayName + "_Packed", world.packedVisualRoot,
            packedPosition, definition.yaw, definition.size * 0.42f, false);
        foreach (Collider collider in packedVisual.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        packedVisual.SetActive(false);

        GameObject consequence = definition.recommended ? null : BuildWrongChoiceConsequence(definition.id, world.effectsRoot, m);
        StoryPreparationItem item = wrapper.AddComponent<StoryPreparationItem>();
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = definition.id;
        serialized.FindProperty("displayName").stringValue = definition.displayName;
        serialized.FindProperty("category").intValue = (int)definition.category;
        serialized.FindProperty("recommended").boolValue = definition.recommended;
        serialized.FindProperty("storyFlag").intValue = (int)definition.flag;
        serialized.FindProperty("childLine").stringValue = definition.childLine;
        serialized.FindProperty("parentLine").stringValue = definition.parentLine;
        serialized.FindProperty("director").objectReferenceValue = director;
        serialized.FindProperty("interactable").objectReferenceValue = interactable;
        serialized.FindProperty("sourceRoot").objectReferenceValue = wrapper;
        serialized.FindProperty("packedVisual").objectReferenceValue = packedVisual;
        serialized.FindProperty("consequenceRoot").objectReferenceValue = consequence;
        serialized.FindProperty("legacyBagMotion").objectReferenceValue = bagMotion;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(interactable.OnInteracted, item.Select);
        return item;
    }

    private static GameObject BuildWrongChoiceConsequence(string id, Transform parent, Materials m)
    {
        GameObject root = new GameObject("Consequence_" + id);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(1.15f, 0f, -0.85f);
        if (id == "GlassBottle")
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject shard = CreatePrimitive("SafeGlassPreview_" + i, PrimitiveType.Cube,
                    new Vector3((i % 3 - 1) * 0.2f, 0.08f, (i / 3) * 0.22f - 0.2f),
                    new Vector3(0.24f, 0.035f, 0.1f), m.hazardGlow, root.transform, false, true);
                shard.transform.localRotation = Quaternion.Euler(0f, i * 28f, (i % 2 == 0 ? 14f : -11f));
            }
        }
        else if (id == "Pan")
        {
            GameObject heavyBag = BuildOriginalBagVariant("OverweightPreviewBag_OriginalBolum1", root.transform.position,
                Quaternion.Euler(0f, 0f, -26f), root.transform, new Vector3(1.35f, 0.82f, 1.08f));
            heavyBag.transform.localPosition = new Vector3(0f, 0f, 0f);
            CreatePrimitive("WeightWarning", PrimitiveType.Cube, new Vector3(0.75f, 0.6f, -0.35f), new Vector3(0.28f, 0.9f, 0.12f), m.hazardGlow, root.transform, false, true);
        }
        else
        {
            CreatePrimitive("LowBatteryBody", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(0.9f, 0.5f, 0.12f), m.navy, root.transform, false, true);
            CreatePrimitive("LowBatteryFill", PrimitiveType.Cube, new Vector3(-0.29f, 0.62f, -0.08f), new Vector3(0.18f, 0.34f, 0.06f), m.hazardGlow, root.transform, false, true);
            CreatePrimitive("LowBatteryCap", PrimitiveType.Cube, new Vector3(0.52f, 0.62f, 0f), new Vector3(0.12f, 0.22f, 0.1f), m.navy, root.transform, false, true);
        }
        root.SetActive(false);
        return root;
    }

    private static Vector3 PackedSlotPosition(int index, WorldReferences world)
    {
        if (world.packedSlots == null || index < 0 || index >= world.packedSlots.Length || world.packedSlots[index] == null)
            throw new InvalidOperationException("Orijinal çanta item slotu bulunamadı: " + index);
        return world.packedSlots[index].position;
    }

    private static Transform StandFor(StoryPreparationCategory category, WorldReferences world)
    {
        return category switch
        {
            StoryPreparationCategory.Signal => world.signalStand,
            StoryPreparationCategory.Food => world.foodStand,
            StoryPreparationCategory.Health => world.healthStand,
            _ => world.warmthStand
        };
    }

    private static StoryCameraZoneId CategoryZone(StoryPreparationCategory category)
    {
        return category switch
        {
            StoryPreparationCategory.Signal => StoryCameraZoneId.PreparationSignal,
            StoryPreparationCategory.Food => StoryCameraZoneId.PreparationFood,
            StoryPreparationCategory.Health => StoryCameraZoneId.PreparationHealth,
            _ => StoryCameraZoneId.PreparationWarmth
        };
    }

    private static GameObject InstantiateItemPrefab(string prefabName, string name, Transform parent, Vector3 worldPosition,
        float yaw, Vector3 targetSize, bool keepColliders)
    {
        string path = ItemRoot + "/" + prefabName;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabName);
            string[] matches = AssetDatabase.FindAssets(fileName + " t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(candidate => string.Equals(Path.GetFileName(candidate), prefabName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (matches.Length > 0)
            {
                path = matches[0];
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        if (prefab == null)
            throw new InvalidOperationException("Çanta eşyası prefabı bulunamadı: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = name;
        foreach (DraggableItem draggable in instance.GetComponentsInChildren<DraggableItem>(true))
            Object.DestroyImmediate(draggable);
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one;
        if (!TryGetCombinedRendererBounds(instance, out Bounds bounds))
            throw new InvalidOperationException(name + " için görsel sınır hesaplanamadı.");
        float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(0.01f, bounds.size.y), targetSize.z / Mathf.Max(0.01f, bounds.size.z)));
        instance.transform.localScale *= scale;
        TryGetCombinedRendererBounds(instance, out bounds);
        instance.transform.position += worldPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        if (!keepColliders)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }
        return instance;
    }

    private static StoryInteractable CreateHotspot(string name, Vector3 position, Vector3 size, string prompt,
        StoryInteractionKind kind, Transform interactionPoint, Transform parent, Materials m,
        StoryInteractionGesture gesture, int gestureCount, float estimatedSeconds, StoryCameraZoneId focusZone,
        bool returnCamera, StoryCameraZoneId returnZone, float focusLinger)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return AddInteractable(root, name, prompt, kind, interactionPoint, gesture, gestureCount, estimatedSeconds,
            focusZone, returnCamera, returnZone, focusLinger, m);
    }

    private static StoryInteractable AddInteractable(GameObject root, string id, string prompt, StoryInteractionKind kind,
        Transform interactionPoint, StoryInteractionGesture gesture, int gestureCount, float estimatedSeconds,
        StoryCameraZoneId focusZone, bool returnCamera, StoryCameraZoneId returnZone, float focusLinger, Materials m)
    {
        StoryInteractable interactable = root.AddComponent<StoryInteractable>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactionId").stringValue = id;
        serialized.FindProperty("interactionKind").intValue = (int)kind;
        serialized.FindProperty("prompt").stringValue = prompt;
        serialized.FindProperty("interactionPoint").objectReferenceValue = interactionPoint;
        serialized.FindProperty("interactionRange").floatValue = 1.12f;
        serialized.FindProperty("interactionGesture").intValue = (int)gesture;
        serialized.FindProperty("requiredGestureCount").intValue = gestureCount;
        serialized.FindProperty("estimatedInteractionSeconds").floatValue = estimatedSeconds;
        serialized.FindProperty("focusCameraZone").intValue = (int)focusZone;
        serialized.FindProperty("returnCameraAfterCompletion").boolValue = returnCamera;
        serialized.FindProperty("returnCameraZone").intValue = (int)returnZone;
        serialized.FindProperty("focusLingerSeconds").floatValue = focusLinger;
        serialized.FindProperty("interactFromAnywhere").boolValue = gesture == StoryInteractionGesture.DragToBag;
        serialized.FindProperty("oneShot").boolValue = true;
        serialized.FindProperty("availableOnStart").boolValue = false;
        serialized.FindProperty("highlightRoot").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return interactable;
    }

    private static void ConfigurePreparationSession(StoryGameManager manager)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("initialAct").intValue = (int)StoryAct.Preparation;
        SerializedProperty flags = serialized.FindProperty("initialFlags");
        flags.arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInteractFromAnywhere(StoryInteractable interactable)
    {
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactFromAnywhere").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDirector(StoryPreparationDirector director, StoryGameManager gameManager,
        StoryPlayerMovement movement, StoryTouchManager touch, StoryCameraController camera, StoryUIController ui,
        CharacterReferences characters, WorldReferences world, InteractionReferences interactions)
    {
        world.wornBag.transform.SetParent(characters.deniz.transform, true);
        world.wornBag.transform.rotation = characters.deniz.transform.rotation * Quaternion.Euler(10f, 180f, 0f);
        if (TryGetCombinedRendererBounds(world.wornBag, out Bounds wornBounds))
        {
            Vector3 desiredCenter = characters.deniz.transform.position + characters.deniz.transform.up * 0.83f
                - characters.deniz.transform.forward * 0.23f;
            world.wornBag.transform.position += desiredCenter - wornBounds.center;
        }

        SerializedObject serialized = new SerializedObject(director);
        Set(serialized, "gameManager", gameManager);
        Set(serialized, "player", movement);
        Set(serialized, "touchManager", touch);
        Set(serialized, "cameraController", camera);
        Set(serialized, "ui", ui);
        Set(serialized, "deniz", characters.deniz.transform);
        Set(serialized, "parent", characters.parent.transform);
        Set(serialized, "can", characters.can.transform);
        Set(serialized, "denizAnimator", characters.denizAnimator);
        Set(serialized, "parentAnimator", characters.parentAnimator);
        Set(serialized, "canAnimator", characters.canAnimator);
        Set(serialized, "startFamilyPlan", interactions.startFamilyPlan);
        Set(serialized, "inspectEmptyBag", interactions.inspectBag);
        SetArray(serialized, "items", interactions.items.Cast<Object>().ToArray());
        Set(serialized, "reviewSignal", interactions.reviewSignal);
        Set(serialized, "reviewFood", interactions.reviewFood);
        Set(serialized, "reviewHealth", interactions.reviewHealth);
        Set(serialized, "reviewWarmth", interactions.reviewWarmth);
        Set(serialized, "testBagWeight", interactions.testBag);
        Set(serialized, "adjustBagStraps", interactions.adjustStraps);
        Set(serialized, "placeBagAtExit", interactions.placeAtExit);
        Set(serialized, "openBagRoot", world.openBag);
        Set(serialized, "closedBagRoot", world.closedBag);
        Set(serialized, "wornBagRoot", world.wornBag);
        Set(serialized, "exitShelfBagRoot", world.exitBag);
        Set(serialized, "completionPanel", world.completionPanel);
        Set(serialized, "completionDetail", world.completionDetail);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildLighting(Transform parent, VolumeProfile profile)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.48f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.28f, 0.26f, 0.23f);
        RenderSettings.ambientGroundColor = new Color(0.13f, 0.11f, 0.1f);
        RenderSettings.ambientIntensity = 1.05f;

        GameObject sunObject = new GameObject("PreparationSun");
        sunObject.transform.SetParent(parent);
        sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.88f, 0.72f);
        sun.intensity = 1.4f;
        sun.shadows = LightShadows.Soft;

        GameObject fillObject = new GameObject("PreparationWindowFill");
        fillObject.transform.SetParent(parent);
        fillObject.transform.position = new Vector3(2.6f, 2.6f, 4.8f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.62f, 0.82f, 1f);
        fill.range = 8.5f;
        fill.intensity = 2.2f;
        fill.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("PreparationPostProcessing");
        volumeObject.transform.SetParent(parent);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static void BuildAmbientAudio(Transform parent)
    {
        AudioClip calm = AssetDatabase.LoadAssetAtPath<AudioClip>(GeneratedRoot + "/Audio/calm_home.wav");
        if (calm == null)
            return;
        GameObject audioObject = new GameObject("PreparationHomeAmbience");
        audioObject.transform.SetParent(parent);
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = calm;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = 0.16f;
        source.spatialBlend = 0f;
    }

    private static void BuildNavigation(GameObject environment)
    {
        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>() ?? environment.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    private static void SaveReusablePrefabs(WorldReferences world, CharacterReferences characters)
    {
        PrefabUtility.SaveAsPrefabAsset(world.environment, PrefabRoot + "/PreparationRoom.prefab");
        PrefabUtility.SaveAsPrefabAsset(world.openBag, PrefabRoot + "/EmergencyBag_Open.prefab");
        PrefabUtility.SaveAsPrefabAsset(characters.parent, PrefabRoot + "/Anne_Ayse.prefab");
    }

    private static void AddSceneToBuildSettingsWithoutChangingStartupScene()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static GameObject InstantiateModelAsset(string assetPath, string name, Transform parent, Vector3 feetPosition,
        Vector3 euler, Vector3 targetSize)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
            throw new InvalidOperationException("Model asset bulunamadı: " + assetPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = name;
        instance.transform.rotation = Quaternion.Euler(euler);
        instance.transform.localScale = Vector3.one;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);

        if (!TryGetCombinedRendererBounds(instance, out Bounds bounds))
            throw new InvalidOperationException(name + " görsel sınırı hesaplanamadı.");
        float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(0.01f, bounds.size.y), targetSize.z / Mathf.Max(0.01f, bounds.size.z)));
        instance.transform.localScale *= scale;
        TryGetCombinedRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return instance;
    }

    private static GameObject InstantiateFurniture(string relativePath, string name, Transform parent, Vector3 feetPosition,
        Vector3 euler, Vector3 targetSize)
    {
        string path = FurnitureRoot + "/" + relativePath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Mobilya prefabı bulunamadı: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.rotation = Quaternion.Euler(euler);
        instance.transform.localScale = Vector3.one;
        if (!TryGetCombinedRendererBounds(instance, out Bounds bounds))
            throw new InvalidOperationException(name + " görsel sınırı hesaplanamadı.");
        float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(0.01f, bounds.size.y), targetSize.z / Mathf.Max(0.01f, bounds.size.z)));
        instance.transform.localScale *= scale;
        TryGetCombinedRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return instance;
    }

    private static bool TryGetCombinedRendererBounds(GameObject root, out Bounds bounds)
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

    private static void FitCharacterToHeight(GameObject instance, Vector3 feetPosition, float targetHeight)
    {
        if (!TryGetCombinedRendererBounds(instance, out Bounds bounds))
        {
            instance.transform.position = feetPosition;
            return;
        }
        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        instance.transform.localScale *= scale;
        TryGetCombinedRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static void EnsureCapsule(GameObject target, float radius, float height)
    {
        CapsuleCollider collider = target.AddComponent<CapsuleCollider>();
        collider.radius = radius;
        collider.height = height;
        collider.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static GameObject CreateWorldPrimitive(string name, PrimitiveType type, Vector3 worldPosition, Vector3 worldScale,
        Material material, Transform parent, Quaternion worldRotation)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = worldPosition;
        go.transform.rotation = worldRotation;
        go.transform.localScale = worldScale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        go.transform.SetParent(parent, true);
        return go;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent, bool collider = true, bool local = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        if (local)
            go.transform.localPosition = position;
        else
            go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
        Collider existing = go.GetComponent<Collider>();
        if (!collider && existing != null)
            Object.DestroyImmediate(existing);
        return go;
    }

    private static Transform CreateFocus(string name, Vector3 worldPosition, Transform parent)
    {
        GameObject focus = new GameObject(name);
        focus.transform.SetParent(parent);
        focus.transform.position = worldPosition;
        focus.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        return focus.transform;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static Quaternion LookAt(Vector3 position, Vector3 target)
    {
        return Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }

    private static void CreateWorldLabel(string name, string text, Vector3 position, Vector3 euler, float fontSize,
        Material accent, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(euler);
        TextMeshPro label = root.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = accent != null && accent.HasProperty("_BaseColor") ? accent.GetColor("_BaseColor") : Color.white;
        label.enableAutoSizing = false;
        label.rectTransform.sizeDelta = new Vector2(4.8f, 0.7f);
    }

    private static GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
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

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color, bool blocksRaycasts)
    {
        GameObject panel = CreateUIRect(name, parent, anchorMin, anchorMax, position, size);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksRaycasts;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
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

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchor,
        Vector2 position, Vector2 size, Color background, Color foreground)
    {
        GameObject go = CreatePanel(name, parent, anchor, anchor, position, size, background, true);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        TMP_Text text = CreateText("Label", go.transform, font, 27f, foreground, TextAlignmentOptions.Center,
            Vector2.zero, size - new Vector2(24f, 18f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 19f;
        text.fontSizeMax = 27f;
        text.text = label;
        return button;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.arraySize = values?.Length ?? 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
