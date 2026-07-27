using System;
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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class StoryHomeSafetySceneBuilder
{
    public const string RebuildPreviewScenePath = "Assets/Scenes/Story_02_RebuildPreview.unity";
    private const string SharedHomePrefabPath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";

    [MenuItem("Tools/Deprem Story/Build Story_02 Rebuild Preview")]
    public static void BuildRebuildPreviewFromMenu()
    {
        BuildRebuildPreview(true);
    }

    [MenuItem("Tools/Deprem Story/Build Story_02 Rebuild Preview (Silent)")]
    public static void BuildRebuildPreviewSilentFromMenu()
    {
        BuildRebuildPreview(false);
    }

    public static void BuildRebuildPreview(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Story 02 rebuild önizlemesi Play Mode dışında üretilmelidir.");

        try
        {
            RequirePreviewAsset(SharedHomePrefabPath);
            StoryChapterBuilderCommon.EnsureFolders();
            StoryChapterBuilderCommon.Materials materials = LoadPreviewMaterials();
            UnityEngine.Rendering.VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();
            RuntimeAnimatorController childController = StoryAnimationLibraryBuilder.BuildLibrary(false);
            RuntimeAnimatorController adultController = StoryAnimationLibraryBuilder.LoadAdultController();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_02_REBUILD_PREVIEW");
            HomeWorld world = BuildRebuildPreviewWorld(root.transform, materials, adultController);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                true,
                new Vector3(-0.75f, 0f, -2.25f),
                new Vector3(-1.95f, 0f, -2.85f),
                new Vector3(1.65f, 0f, 2.75f),
                adultController);
            BuildRebuildPreviewCharacterPayoffs(family, world);
            BuildParentDrillRig(world, family.parent.transform);

            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);
            StoryCameraController cameraController = BuildRebuildPreviewCameras(
                root.transform,
                family.deniz.transform,
                family.parent.transform,
                out Camera mainCamera,
                out CinemachineBrain brain);
            StoryChapterBuilderCommon.ChapterUI ui = StoryChapterBuilderCommon.BuildUI(
                root.transform,
                cameraController,
                "Story02RebuildCanvas",
                "2. PERDE • EVİ GÜVENLİ YAP",
                "EVİN RİSKLERİ AZALTILDI",
                "Deniz riski bulur ve hafif eşyayı taşır; ağır sabitlemeyi yetişkin yapar.",
                StoryAct.HomeSafety);
            StoryChapterBuilderCommon.SetReference(ui.controller, "movementOwner", movement);
            ConfigureRebuildPreviewText(root.transform);

            GameObject sessionObject = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(
                gameManager,
                StoryAct.HomeSafety,
                new[]
                {
                    StoryFlag.BagReady,
                    StoryFlag.BagFlashlight,
                    StoryFlag.BagFirstAid,
                    StoryFlag.BagWater,
                    StoryFlag.BagComfortItem
                });
            StoryChapterBuilderCommon.ConfigureRebuildStoryRoute(gameManager);

            GameObject core = new GameObject("_Story02RebuildCore");
            core.transform.SetParent(root.transform);
            StoryHomeSafetyDirector director = core.AddComponent<StoryHomeSafetyDirector>();
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", ui.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchData = new SerializedObject(touch);
            touchData.FindProperty("directWorldGestures").boolValue = true;
            touchData.ApplyModifiedPropertiesWithoutUndo();

            HomeInteractions interactions = BuildRebuildPreviewInteractions(
                world,
                family,
                director);
            ConfigureDirector(
                director,
                gameManager,
                movement,
                touch,
                cameraController,
                ui,
                family,
                world,
                interactions);
            ConfigureRebuildPreviewDirector(director, world, interactions);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(ui.controller, "cameraController", cameraController);

            StoryChapterBuilderCommon.BuildLighting(
                root.transform,
                volume,
                new Color(1f, 0.88f, 0.72f),
                1.32f);
            AudioClip ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(
                StoryChapterBuilderCommon.AudioRoot + "/calm_home.wav");
            if (ambience != null)
                StoryChapterBuilderCommon.CreateAudioSource(
                    "HomeSafetyRebuildAmbience",
                    root.transform,
                    ambience,
                    0.14f,
                    true,
                    true);
            StoryChapterBuilderCommon.CreateLicensedAmbience(
                "HomeSafetyRoomTone",
                root.transform,
                "sfx100v2_loop_ambient_02.ogg",
                0.04f);
            StoryChapterBuilderCommon.AttachInteractionAudioLayer(
                root.transform,
                "Story02_ObjectInteractionAudio");
            StoryChapterBuilderCommon.DisableShadows(root.transform);

            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            StoryChapterBuilderCommon.BuildNavigation(world.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            StoryHomeSafetyRebuildPreviewValidator.Validate(false);
            Selection.activeGameObject = root;
            Debug.Log("Story_02_RebuildPreview üretildi: " + RebuildPreviewScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Ortak aile evinde Story 02 rebuild önizlemesi üretildi.\n" +
                    "Mevcut Story_02_HomeSafety sahnesi ve Build Settings değiştirilmedi.",
                    "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 02 rebuild önizlemesi üretilemedi:\n" + exception.Message,
                    "Kapat");
            throw;
        }
    }

    private static HomeWorld BuildRebuildPreviewWorld(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        RuntimeAnimatorController adultController)
    {
        HomeWorld world = new HomeWorld();
        world.environment = new GameObject("HomeSafetyEnvironment_Rebuild");
        world.environment.transform.SetParent(parent);

        GameObject sharedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHomePrefabPath);
        world.sharedHome = (GameObject)PrefabUtility.InstantiatePrefab(
            sharedPrefab,
            world.environment.transform);
        world.sharedHome.name = "StoryHome_Shared";
        world.sharedHome.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        world.sharedHome.transform.localScale = Vector3.one;
        ConfigureRebuildPreviewHomeState(world.sharedHome.transform);
        StoryChapterBuilderCommon.RestyleEmergencyBag(
            FindPreviewRequired(world.sharedHome.transform, "EmergencyBag").gameObject,
            materials,
            new Vector3(0.64f, 0.7f, 0.4f),
            false);

        Transform dressing = StoryChapterBuilderCommon.NewChild(
            world.environment.transform,
            "HomeSafetyRebuildSetDressing");

        world.wardrobe = FindPreviewRequired(world.sharedHome.transform, "Wardrobe_Unsecured").gameObject;
        world.shelfFrame = FindPreviewRequired(world.sharedHome.transform, "Shelf_Unsecured").gameObject;
        world.closedDoor = FindPreviewRequired(world.sharedHome.transform, "Door").gameObject;
        world.openDoor = FindPreviewRequired(world.sharedHome.transform, "Door_Open").gameObject;
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(world.closedDoor);

        BuildRebuildPreviewRiskHotspots(dressing, materials, world);
        BuildRebuildPreviewRiskFootprints(dressing, materials, world);
        BuildRebuildPreviewSafePlayCorner(dressing, materials, world);
        BuildRebuildPreviewExit(dressing, materials, world);
        BuildRebuildPreviewShelf(dressing, materials, world);
        BuildRebuildPreviewWardrobe(dressing, materials, world);
        BuildRebuildPreviewRouteCars(dressing, world);
        BuildRebuildPreviewNeighbor(dressing, materials, adultController, world);
        BuildRebuildPreviewContinuity(dressing, materials, world);

        // Ağır mobilya ramak kalası ayrı bir "görev kutusu" üzerinden değil,
        // dolabın görünmez yan yüzündeki doğrudan tutma alanından çalışır.
        world.unsafeHeavyBox = world.wardrobeTestFace;
        world.unsafeDrill = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/Drill/Drill_01.obj",
            "UnsafePoweredDrill_Rebuild",
            dressing,
            new Vector3(1.6f, 0.42f, 2.55f),
            new Vector3(0.95f, 0.62f, 0.42f),
            new Vector3(0f, 90f, -8f),
            false,
            true);

        world.exitAnimations = CreatePreviewMoveAnimations(
            world.exitStart,
            world.exitStored,
            "Story02Rebuild_ExitMove_",
            0.58f);
        world.shelfAnimations = CreatePreviewMoveAnimations(
            world.shelfHigh,
            world.shelfLow,
            "Story02Rebuild_ShelfMove_",
            0.62f);
        world.shelfSecureAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.shelfAnchorStrap,
            "Story02Rebuild_ShelfBracketSecure",
            new Vector3(-2.45f, -1.65f, 1.1f),
            0.72f);
        world.shelfStabilityAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.shelfFrame,
            "Story02Rebuild_ShelfStabilityTest",
            0.75f,
            0.7f);
        world.wardrobeRockAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.wardrobe,
            "Story02Rebuild_WardrobeRock",
            2.8f,
            0.85f);
        world.wardrobeSecureAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.wardrobeAnchorStrap,
            "Story02Rebuild_WardrobeStrapSecure",
            new Vector3(5.83f, -1.85f, -0.03f),
            0.78f);
        world.heavyAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.unsafeHeavyBox,
            "Story02Rebuild_HeavyNearMiss",
            5f,
            0.7f);
        world.drillAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.unsafeDrill,
            "Story02Rebuild_DrillNearMiss",
            12f,
            0.65f);
        world.doorAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.openDoor,
            "Story02Rebuild_DoorOpen",
            new Vector3(0.72f, 0f, -0.72f),
            0.72f);
        world.initialRouteCarAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.initialRouteCarBlocked,
            "Story02Rebuild_InitialCarBlocked",
            8f,
            0.52f);
        world.finalRouteCarAnimation = StoryChapterBuilderCommon.CreateRockAnimation(
            world.finalRouteCarFinish,
            "Story02Rebuild_FinalCarFinish",
            4f,
            0.48f);
        world.shelfDust = StoryChapterBuilderCommon.CreateDust(
            "ShelfFixDust_Rebuild",
            dressing,
            new Vector3(4.45f, 2.48f, 1.55f),
            materials,
            18);
        world.wardrobeDust = StoryChapterBuilderCommon.CreateDust(
            "WardrobeFixDust_Rebuild",
            dressing,
            new Vector3(-4.55f, 2.72f, 2.7f),
            materials,
            20);
        return world;
    }

    private static void ConfigureRebuildPreviewHomeState(Transform home)
    {
        SetPreviewActive(home, "Wardrobe_Secured", false);
        SetPreviewActive(home, "Wardrobe_Unsecured", true);
        SetPreviewActive(home, "Wardrobe_Fallen", false);
        SetPreviewActive(home, "Shelf_Secured", false);
        SetPreviewActive(home, "Shelf_Unsecured", true);
        SetPreviewActive(home, "Shelf_Fallen", false);
        SetPreviewActive(home, "Door", true);
        SetPreviewActive(home, "Door_Open", false);
        SetPreviewActive(home, "ExitRoute_Cleared", false);
        SetPreviewActive(home, "ExitRoute_ClutteredButPassable", false);
        SetPreviewActive(home, "Shoes_PostQuake", false);
        SetPreviewActive(home, "BrokenGlass_Hazard", false);
        SetPreviewActive(home, "EmergencyBag", true);

        // The canonical Story 03 wardrobe was authored against the back-left window.
        // In the wider Story 02 shots its body and curtain occupied the same silhouette.
        // Turn all three variants onto the solid left wall so the window, exit and fall
        // zone remain three distinct reads throughout the chapter.
        PlaceHomeVariant(home, "Wardrobe_Secured",
            new Vector3(-4.55f, 0f, 2.7f), new Vector3(0f, -90f, 0f));
        PlaceHomeVariant(home, "Wardrobe_Unsecured",
            new Vector3(-4.55f, 0f, 2.7f), new Vector3(0f, -90f, 0f));
        PlaceHomeVariant(home, "Wardrobe_Fallen",
            new Vector3(-4.02f, 0.38f, 2.7f), new Vector3(0f, -90f, 78f));
        PlaceHomeVariant(home, "WardrobeFocus",
            new Vector3(-3.72f, 0.1f, 2.7f), Vector3.zero);

        // Story 02 supplies its own movable shelf contents. Keeping the decorative
        // prefab books underneath them made the shelf read as an accidental pile.
        foreach (string shelfName in new[] { "Shelf_Secured", "Shelf_Unsecured", "Shelf_Fallen" })
        {
            Transform shelf = FindPreviewRequired(home, shelfName);
            foreach (Transform child in shelf.GetComponentsInChildren<Transform>(true))
            {
                if (child != shelf && child.name.StartsWith("Book_", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
        }
    }

    private static void PlaceHomeVariant(
        Transform home,
        string objectName,
        Vector3 localPosition,
        Vector3 localEuler)
    {
        Transform target = FindPreviewRequired(home, objectName);
        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEuler);
    }

    private static void BuildRebuildPreviewRiskHotspots(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        world.wardrobeInspectFace = CreatePreviewHotspot(
            "WardrobeRiskFace_Rebuild",
            new Vector3(-4.16f, 1.45f, 2.7f),
            new Vector3(0.16f, 2.55f, 1.45f),
            materials.wood,
            parent);
        world.wardrobeTestFace = CreatePreviewHotspot(
            "WardrobeSideTest_Rebuild",
            new Vector3(-4.48f, 1.38f, 2.03f),
            new Vector3(0.72f, 2.35f, 0.14f),
            materials.navy,
            parent);
    }

    private static void BuildRebuildPreviewRiskFootprints(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        world.shelfRiskZoneUnstable = BuildRiskFootprint(
            "ShelfFallZone_Unstable",
            parent,
            materials.amber,
            new Vector3(3.8f, 0.035f, 1.55f),
            new Vector3(-0.42f, 0f, 0f),
            new Vector3(0.16f, 0.035f, 0.84f),
            5);
        world.shelfRiskZoneSecured = BuildRiskFootprint(
            "ShelfFallZone_Stabilized",
            parent,
            materials.teal,
            new Vector3(4.02f, 0.04f, 1.55f),
            new Vector3(-0.24f, 0f, 0f),
            new Vector3(0.1f, 0.04f, 0.92f),
            3);
        world.wardrobeRiskZoneUnstable = BuildRiskFootprint(
            "WardrobeFallZone_Unstable",
            parent,
            materials.amber,
            new Vector3(-4.02f, 0.035f, 2.7f),
            new Vector3(0.42f, 0f, 0f),
            new Vector3(0.16f, 0.035f, 0.82f),
            5);
        world.wardrobeRiskZoneSecured = BuildRiskFootprint(
            "WardrobeFallZone_Stabilized",
            parent,
            materials.teal,
            new Vector3(-4.06f, 0.04f, 2.7f),
            new Vector3(0.24f, 0f, 0f),
            new Vector3(0.1f, 0.04f, 0.9f),
            3);

        world.shelfRiskZoneUnstable.SetActive(false);
        world.shelfRiskZoneSecured.SetActive(false);
        world.wardrobeRiskZoneUnstable.SetActive(false);
        world.wardrobeRiskZoneSecured.SetActive(false);
    }

    private static GameObject BuildRiskFootprint(
        string name,
        Transform parent,
        Material material,
        Vector3 firstPosition,
        Vector3 step,
        Vector3 dashSize,
        int dashCount)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        for (int index = 0; index < dashCount; index++)
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "FloorDash_" + (index + 1),
                PrimitiveType.Cube,
                firstPosition + step * index,
                dashSize,
                material,
                root.transform,
                false);
        }
        return root;
    }

    private static void BuildRebuildPreviewSafePlayCorner(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        world.canReadingNestRisk = BuildReadingNest(
            "CanReadingNest_Risk",
            parent,
            new Vector3(3.08f, 0.045f, 1.55f),
            new Vector3(0f, -10f, 0f),
            materials.amber,
            materials.coral);
        world.canReadingNestSafe = BuildReadingNest(
            "CanReadingNest_Safe",
            parent,
            new Vector3(0.95f, 0.045f, -0.72f),
            new Vector3(0f, 14f, 0f),
            materials.teal,
            materials.amber);
        world.canReadingNestSafe.SetActive(false);
    }

    private static GameObject BuildReadingNest(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 euler,
        Material rugMaterial,
        Material cushionMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));

        StoryChapterBuilderCommon.CreatePrimitive(
            "ReadingMat",
            PrimitiveType.Cylinder,
            position,
            new Vector3(0.82f, 0.035f, 0.68f),
            rugMaterial,
            root.transform,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "ReadingCushion",
            PrimitiveType.Sphere,
            position + new Vector3(-0.12f, 0.12f, 0.03f),
            new Vector3(0.48f, 0.18f, 0.42f),
            cushionMaterial,
            root.transform,
            false,
            Quaternion.Euler(0f, euler.y - 8f, -4f));
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_08.prefab",
            "CanComicBook",
            root.transform,
            position + new Vector3(0.24f, 0.095f, -0.08f),
            new Vector3(0.34f, 0.08f, 0.46f),
            new Vector3(0f, euler.y + 18f, 0f),
            false);
        return root;
    }

    private static void BuildRebuildPreviewExit(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        GameObject shoes = StoryAuthoredPropFactory.CreateShoePair(
            "ExitShoes_Start",
            parent,
            new Vector3(2.05f, 0f, 4.35f),
            new Vector3(0.92f, 0.28f, 0.72f),
            new Vector3(0f, 14f, 0f),
            materials.coral,
            materials.navy);
        GameObject shoesStored = StoryAuthoredPropFactory.CreateShoePair(
            "ExitShoes_Stored",
            parent,
            new Vector3(4.2f, 0f, 4.35f),
            new Vector3(0.78f, 0.25f, 0.76f),
            new Vector3(0f, 90f, 0f),
            materials.teal,
            materials.navy);
        GameObject toy = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "ExitToy_Start",
            parent,
            new Vector3(2.82f, 0f, 4.92f),
            new Vector3(0.62f, 0.32f, 0.68f),
            new Vector3(0f, 8f, 0f),
            true);
        GameObject toyStored = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "ExitToy_Stored",
            parent,
            new Vector3(4.08f, 0f, 3.55f),
            new Vector3(0.62f, 0.32f, 0.68f),
            new Vector3(0f, -5f, 0f),
            true);
        GameObject parcel = StoryAuthoredPropFactory.CreateParcel(
            "ExitParcel_Start",
            parent,
            new Vector3(2.55f, 0f, 5.55f),
            new Vector3(0.72f, 0.58f, 0.64f),
            new Vector3(0f, -12f, 0f),
            materials.amber,
            materials.cream,
            materials.coral,
            true);
        GameObject parcelStored = StoryAuthoredPropFactory.CreateParcel(
            "ExitParcel_Stored",
            parent,
            new Vector3(4.12f, 0f, 5.65f),
            new Vector3(0.72f, 0.58f, 0.64f),
            new Vector3(0f, 7f, 0f),
            materials.teal,
            materials.cream,
            materials.amber,
            true);

        shoesStored.SetActive(false);
        toyStored.SetActive(false);
        parcelStored.SetActive(false);
        world.exitStart = new[] { shoes, toy, parcel };
        world.exitStored = new[] { shoesStored, toyStored, parcelStored };
        world.exitStandPoint = StoryChapterBuilderCommon.CreatePoint(
            "ExitStandPoint_Rebuild",
            parent,
            new Vector3(2.15f, 0f, 4.05f),
            new Vector3(2.65f, 0.9f, 6.55f));
    }

    private static void BuildRebuildPreviewShelf(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        GameObject booksHigh = new GameObject("ShelfBooks_High");
        booksHigh.transform.SetParent(parent);
        GameObject booksLow = new GameObject("ShelfBooks_Low");
        booksLow.transform.SetParent(parent);
        for (int index = 0; index < 2; index++)
        {
            float z = 1.28f + index * 0.42f;
            StoryChapterBuilderCommon.InstantiateFurniture(
                index % 2 == 0 ? "Decorations/Book_03.prefab" : "Decorations/Book_08.prefab",
                "Book_" + index,
                booksHigh.transform,
                new Vector3(4.17f, 1.46f, z),
                new Vector3(0.18f, 0.46f + index * 0.025f, 0.34f),
                new Vector3(0f, 90f, index * 3f),
                false);
            StoryChapterBuilderCommon.InstantiateFurniture(
                index % 2 == 0 ? "Decorations/Book_03.prefab" : "Decorations/Book_08.prefab",
                "Book_" + index,
                booksLow.transform,
                new Vector3(4.0f, 0.28f, z),
                new Vector3(0.18f, 0.46f + index * 0.025f, 0.34f),
                new Vector3(0f, 90f, index * 3f),
                false);
        }

        GameObject vaseHigh = StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_05.prefab",
            "ShelfVase_High",
            parent,
            new Vector3(4.16f, 1.45f, 1.02f),
            new Vector3(0.28f, 0.36f, 0.28f),
            new Vector3(0f, 18f, 0f),
            false);
        GameObject vaseLow = StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_05.prefab",
            "ShelfVase_Low",
            parent,
            new Vector3(3.86f, 0.28f, 1.02f),
            new Vector3(0.28f, 0.36f, 0.28f),
            new Vector3(0f, -15f, 0f),
            false);
        GameObject frameHigh = StoryAuthoredPropFactory.CreatePictureFrame(
            "ShelfFrame_High",
            parent,
            new Vector3(4.17f, 1.44f, 2.02f),
            new Vector3(0.1f, 0.34f, 0.32f),
            new Vector3(0f, 90f, 0f),
            materials.wood,
            materials.cream,
            materials.amber,
            false);
        GameObject frameLow = StoryAuthoredPropFactory.CreatePictureFrame(
            "ShelfFrame_Low",
            parent,
            new Vector3(3.9f, 0.28f, 2.02f),
            new Vector3(0.1f, 0.34f, 0.32f),
            new Vector3(0f, 90f, 0f),
            materials.wood,
            materials.cream,
            materials.amber,
            false);

        booksLow.SetActive(false);
        vaseLow.SetActive(false);
        frameLow.SetActive(false);
        world.shelfHigh = new[] { booksHigh, vaseHigh, frameHigh };
        world.shelfLow = new[] { booksLow, vaseLow, frameLow };
        world.shelfBracket = StoryAuthoredPropFactory.CreateMetalBracket(
            "ShelfBracketInHand",
            parent,
            new Vector3(1.1f, 0.68f, 2.25f),
            new Vector3(0.48f, 0.3f, 0.4f),
            new Vector3(0f, -24f, 0f),
            materials.metal,
            materials.amber);
        world.shelfAnchorStrap = StoryAuthoredPropFactory.CreateMetalBracket(
            "ShelfWallBracket",
            parent,
            new Vector3(4.48f, 2.38f, 1.55f),
            new Vector3(1.36f, 0.48f, 0.38f),
            new Vector3(0f, 90f, 0f),
            materials.metal,
            materials.teal,
            false);
        world.shelfAnchorStrap.SetActive(false);
        world.shelfAnchorMark = CreatePreviewHotspot(
            "ShelfAnchorMark_Hotspot",
            new Vector3(4.18f, 2.18f, 1.55f),
            new Vector3(0.28f, 0.38f, 0.52f),
            materials.amber,
            parent);
        world.shelfStabilityHandle = CreatePreviewHotspot(
            "ShelfStabilityHandle_Hotspot",
            new Vector3(4.02f, 1.18f, 1.55f),
            new Vector3(0.22f, 0.72f, 0.72f),
            materials.teal,
            parent);
    }

    private static void BuildRebuildPreviewWardrobe(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        world.wardrobeTestHandle = CreatePreviewHotspot(
            "WardrobeTestHandle_Rebuild",
            new Vector3(-4.12f, 1.32f, 2.42f),
            new Vector3(0.18f, 0.55f, 0.28f),
            materials.metal,
            parent);
        world.wardrobeAnchorMarks = StoryAuthoredPropFactory.CreateMetalBracket(
            "WardrobeAnchorMarks",
            parent,
            new Vector3(-4.78f, 2.72f, 2.7f),
            new Vector3(0.34f, 0.42f, 1.15f),
            Vector3.zero,
            materials.metal,
            materials.amber);
        world.wardrobeHandStrap = StoryAuthoredPropFactory.CreateMetalBracket(
            "WardrobeHandStrap",
            parent,
            new Vector3(0.95f, 0.7f, 2.4f),
            new Vector3(0.78f, 0.36f, 0.42f),
            new Vector3(0f, -18f, 0f),
            materials.metal,
            materials.teal);
        world.wardrobeAnchorStrap = StoryAuthoredPropFactory.CreateMetalBracket(
            "WardrobeAnchorStrap",
            parent,
            new Vector3(-4.78f, 2.68f, 2.7f),
            new Vector3(0.36f, 0.46f, 1.28f),
            Vector3.zero,
            materials.metal,
            materials.teal,
            false);
        world.wardrobeAnchorStrap.SetActive(false);
        world.wardrobeStabilityHandle = CreatePreviewHotspot(
            "WardrobeStabilityHandle_Hotspot",
            new Vector3(-4.12f, 1.15f, 2.98f),
            new Vector3(0.18f, 0.62f, 0.32f),
            materials.teal,
            parent);
    }

    private static void BuildRebuildPreviewRouteCars(Transform parent, HomeWorld world)
    {
        world.initialRouteCarStart = FindPreviewRequired(
            world.sharedHome.transform,
            "Can_ToyCar").gameObject;
        world.initialRouteCarStart.name = "Can_ToyCar_InitialRoute";
        world.initialRouteCarStart.transform.localScale *= 1.35f;
        world.initialRouteCarBlocked = Object.Instantiate(world.initialRouteCarStart, parent);
        world.initialRouteCarBlocked.name = "Can_ToyCar_Blocked";
        world.initialRouteCarBlocked.transform.position = new Vector3(2.02f, 0.08f, 4.05f);
        world.initialRouteCarBlocked.SetActive(false);

        world.finalRouteCarStart = Object.Instantiate(world.initialRouteCarStart, parent);
        world.finalRouteCarStart.name = "Can_ToyCar_FinalStart";
        world.finalRouteCarStart.transform.position = new Vector3(-1.15f, 0.08f, -2.7f);
        world.finalRouteCarStart.SetActive(false);
        world.finalRouteCarFinish = Object.Instantiate(world.initialRouteCarStart, parent);
        world.finalRouteCarFinish.name = "Can_ToyCar_FinalFinish";
        world.finalRouteCarFinish.transform.position = new Vector3(2.62f, 0.08f, 6.02f);
        world.finalRouteCarFinish.SetActive(false);
    }

    private static void BuildRebuildPreviewCharacterPayoffs(
        StoryChapterBuilderCommon.Characters family,
        HomeWorld world)
    {
        Transform pocket = family.canAnimator != null && family.canAnimator.isHuman
            ? family.canAnimator.GetBoneTransform(HumanBodyBones.Hips)
            : family.can.transform;
        if (pocket == null)
            pocket = family.can.transform;

        world.canToyCarPocket = Object.Instantiate(world.initialRouteCarStart, pocket);
        world.canToyCarPocket.name = "Can_ToyCar_Pocket";
        foreach (Collider collider in world.canToyCarPocket.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        foreach (Animation animation in world.canToyCarPocket.GetComponentsInChildren<Animation>(true))
            Object.DestroyImmediate(animation);

        world.canToyCarPocket.transform.localPosition = new Vector3(0.13f, -0.16f, 0.075f);
        world.canToyCarPocket.transform.localRotation = Quaternion.Euler(18f, 82f, 4f);
        world.canToyCarPocket.transform.localScale = Vector3.one;
        if (TryGetBounds(world.canToyCarPocket, out Bounds bounds))
        {
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            world.canToyCarPocket.transform.localScale *= 0.18f / Mathf.Max(0.01f, longest);
        }
        world.canToyCarPocket.SetActive(false);
    }

    private static void BuildRebuildPreviewNeighbor(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        RuntimeAnimatorController adultController,
        HomeWorld world)
    {
        world.nermin = StoryChapterBuilderCommon.InstantiateCharacter(
            StoryChapterBuilderCommon.StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab",
            "Nermin_Neighbor",
            parent,
            new Vector3(1.45f, 0f, 5.2f),
            1.62f,
            adultController);
        world.nermin.transform.rotation = Quaternion.Euler(0f, 198f, 0f);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Nermin_Scarf",
            PrimitiveType.Cube,
            world.nermin.transform.position + new Vector3(0f, 1.28f, 0f),
            new Vector3(0.42f, 0.12f, 0.14f),
            materials.coral,
            world.nermin.transform,
            false,
            Quaternion.Euler(0f, 28f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "Nermin_Coat",
            PrimitiveType.Cube,
            world.nermin.transform.position + new Vector3(0f, 0.78f, 0.05f),
            new Vector3(0.5f, 0.66f, 0.2f),
            materials.teal,
            world.nermin.transform,
            false,
            Quaternion.Euler(0f, 18f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "Nermin_Cane",
            PrimitiveType.Cylinder,
            world.nermin.transform.position + new Vector3(0.32f, 0.48f, 0.1f),
            new Vector3(0.045f, 0.48f, 0.045f),
            materials.wood,
            world.nermin.transform,
            false,
            Quaternion.Euler(0f, 0f, -8f));
        world.nerminDoorHandle = CreatePreviewHotspot(
            "NerminDoorHandle_Hotspot",
            new Vector3(2.95f, 1.05f, 6.05f),
            new Vector3(0.28f, 0.22f, 0.28f),
            materials.metal,
            parent);
        world.nerminEnvelopeStart = StoryAuthoredPropFactory.CreateEnvelope(
            "NerminEnvelope_Start",
            parent,
            new Vector3(1.78f, 0.06f, 4.92f),
            new Vector3(0.34f, 0.06f, 0.24f),
            new Vector3(0f, 18f, 0f),
            materials.cream,
            materials.amber,
            materials.coral);
        world.nerminEnvelopeReturned = StoryAuthoredPropFactory.CreateEnvelope(
            "NerminEnvelope_Returned",
            parent,
            new Vector3(1.55f, 1.03f, 5.12f),
            new Vector3(0.3f, 0.05f, 0.22f),
            new Vector3(0f, 18f, 0f),
            materials.cream,
            materials.amber,
            materials.coral);
        world.nermin.SetActive(false);
        world.nerminEnvelopeStart.SetActive(false);
        world.nerminEnvelopeReturned.SetActive(false);

        Vector3 nerminStart = world.nermin.transform.localPosition;
        world.nermin.transform.localPosition = nerminStart + new Vector3(0.9f, 0f, 1.9f);
        world.nerminExitAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.nermin,
            "Story02Rebuild_NerminCorridorExit",
            new Vector3(-0.9f, 0f, -1.9f),
            2.1f);
        world.nermin.transform.localPosition = nerminStart;
    }

    private static void BuildRebuildPreviewContinuity(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        HomeWorld world)
    {
        Transform board = StoryChapterBuilderCommon.NewChild(parent, "FamilyPlanBoard_Continuity");
        board.SetPositionAndRotation(
            new Vector3(-4.84f, 2.0f, 1.05f),
            Quaternion.Euler(0f, 90f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "Board",
            PrimitiveType.Cube,
            Vector3.zero,
            new Vector3(2.5f, 1.35f, 0.1f),
            materials.wood,
            board,
            false);
        world.evacuationPlanInHand = StoryChapterBuilderCommon.CreatePrimitive(
            "Nermin_EvacuationPlan_InHand",
            PrimitiveType.Cube,
            new Vector3(1.55f, 1.02f, 5.12f),
            new Vector3(0.3f, 0.05f, 0.22f),
            materials.cream,
            parent,
            true,
            Quaternion.Euler(0f, 18f, 0f));
        world.evacuationPlan = StoryChapterBuilderCommon.CreatePrimitive(
            "Nermin_EvacuationPlan",
            PrimitiveType.Cube,
            new Vector3(-4.77f, 1.92f, 1.1f),
            new Vector3(0.04f, 0.86f, 0.62f),
            materials.cream,
            parent,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "PlanRouteLine",
            PrimitiveType.Cube,
            new Vector3(-4.74f, 1.92f, 1.1f),
            new Vector3(0.025f, 0.09f, 0.42f),
            materials.teal,
            world.evacuationPlan.transform,
            false);
        world.evacuationPlanInHand.SetActive(false);
        world.evacuationPlan.SetActive(false);
    }

    private static StoryCameraController BuildRebuildPreviewCameras(
        Transform parent,
        Transform deniz,
        Transform adult,
        out Camera mainCamera,
        out CinemachineBrain brain)
    {
        StoryChapterBuilderCommon.CameraSpec[] specs =
        {
            new(
                StoryCameraZoneId.HomeOverview,
                "CM_HomeOverview_Rebuild",
                new Vector3(10.2f, 11.2f, -13.2f),
                new Vector3(-0.05f, 0.9f, 0.95f),
                48f,
                false,
                deniz,
                18.5f,
                new Vector2(-0.18f, 0.16f)),
            new(
                StoryCameraZoneId.HomeWardrobe,
                "CM_HomeWardrobe_Rebuild",
                new Vector3(0.15f, 3.15f, -0.35f),
                new Vector3(-4.2f, 1.38f, 2.7f),
                44f),
            new(
                StoryCameraZoneId.HomeShelf,
                "CM_HomeShelf_Rebuild",
                new Vector3(-0.7f, 3.15f, -1.65f),
                new Vector3(4.2f, 1.35f, 1.58f),
                43f),
            new(
                StoryCameraZoneId.HomeExit,
                "CM_HomeExit_Rebuild",
                new Vector3(2.35f, 4.0f, -2.6f),
                new Vector3(2.55f, 0.72f, 4.9f),
                43f),
            new(
                StoryCameraZoneId.HomeParent,
                "CM_HomeParentWork_Rebuild",
                new Vector3(4.3f, 3.2f, 4.9f),
                new Vector3(1.45f, 1.0f, 2.45f),
                44f),
            new(
                StoryCameraZoneId.HomeFinalTest,
                "CM_HomeFinalRoute_Rebuild",
                new Vector3(10.8f, 12.2f, -15.2f),
                new Vector3(0.45f, 0.5f, 1.75f),
                50f)
        };
        return StoryChapterBuilderCommon.BuildCameras(
            parent,
            StoryCameraZoneId.HomeOverview,
            specs,
            out mainCamera,
            out brain);
    }

    private static HomeInteractions BuildRebuildPreviewInteractions(
        HomeWorld world,
        StoryChapterBuilderCommon.Characters family,
        StoryHomeSafetyDirector director)
    {
        HomeInteractions interactions = new HomeInteractions();
        Transform points = StoryChapterBuilderCommon.NewChild(
            world.environment.transform,
            "HomeInteractionPoints_Rebuild");
        Transform routePoint = StoryChapterBuilderCommon.CreatePoint(
            "InitialRouteStand",
            points,
            new Vector3(-0.75f, 0f, -2.05f),
            new Vector3(2.2f, 0.4f, 4.3f));
        Transform exitPoint = StoryChapterBuilderCommon.CreatePoint(
            "ExitInspectStand",
            points,
            new Vector3(1.75f, 0f, 3.75f),
            new Vector3(2.65f, 0.8f, 5.55f));
        Transform shelfPoint = StoryChapterBuilderCommon.CreatePoint(
            "ShelfStand",
            points,
            new Vector3(3.0f, 0f, 1.55f),
            new Vector3(4.3f, 1.2f, 1.55f));
        Transform wardrobePoint = StoryChapterBuilderCommon.CreatePoint(
            "WardrobeStand",
            points,
            new Vector3(-3.08f, 0f, 2.7f),
            new Vector3(-4.45f, 1.25f, 2.7f));
        Transform parentPoint = StoryChapterBuilderCommon.CreatePoint(
            "ParentWorkStand",
            points,
            new Vector3(1.15f, 0f, 2.35f),
            family.parent.transform.position + Vector3.up);
        Transform safePlayPoint = StoryChapterBuilderCommon.CreatePoint(
            "CanSafePlayStand",
            points,
            new Vector3(2.18f, 0f, 1.35f),
            world.canReadingNestRisk.transform.position + Vector3.up * 0.25f);
        world.shelfParentWorkPoint = StoryChapterBuilderCommon.CreatePoint(
            "ShelfParentWorkPoint",
            points,
            new Vector3(3.55f, 0f, 1.55f),
            new Vector3(4.5f, 1.25f, 1.55f));
        world.wardrobeParentWorkPoint = StoryChapterBuilderCommon.CreatePoint(
            "WardrobeParentWorkPoint",
            points,
            new Vector3(-3.05f, 0f, 3.18f),
            new Vector3(-4.55f, 1.35f, 2.7f));

        interactions.testInitialRoute = StoryChapterBuilderCommon.AddInteractable(
            world.initialRouteCarStart,
            "home.route.initial",
            "ARABAYI KAPI ROTASINA SÜR",
            StoryInteractionKind.Inspect,
            routePoint,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.HomeExit,
            false,
            1,
            1.6f,
            2.1f);
        ConfigurePreviewDrag(
            interactions.testInitialRoute,
            CreatePreviewDropZone(
                "Drop_InitialRouteBlocked",
                points,
                world.initialRouteCarBlocked.transform.position,
                new Vector3(1.15f, 0.35f, 1.15f)));

        interactions.openDoorForNermin = StoryChapterBuilderCommon.AddInteractable(
            world.nerminDoorHandle,
            "home.neighbor.door",
            "KAPI KOLUNU YANA ÇEK",
            StoryInteractionKind.Inspect,
            exitPoint,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.HomeExit,
            false,
            1,
            1.1f,
            2f);
        StoryChapterBuilderCommon.SetGestureTarget(
            interactions.openDoorForNermin,
            world.openDoor.transform);

        interactions.returnNerminEnvelope = StoryChapterBuilderCommon.AddInteractable(
            world.nerminEnvelopeStart,
            "home.neighbor.envelope",
            "ZARFI NERMİN'İN ELİNE VER",
            StoryInteractionKind.HelpSibling,
            exitPoint,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.HomeExit,
            false,
            1,
            1.2f,
            2f);
        ConfigurePreviewDrag(
            interactions.returnNerminEnvelope,
            CreatePreviewDropZone(
                "Drop_NerminHand",
                points,
                world.nerminEnvelopeReturned.transform.position,
                new Vector3(0.75f, 0.75f, 0.75f)));
        interactions.placeEvacuationPlan = AddPreviewDragInteraction(
            world.evacuationPlanInHand,
            world.evacuationPlan,
            "home.neighbor.plan",
            "TAHLİYE PLANINI AİLE PANOSUNA AS",
            StoryInteractionKind.Collect,
            exitPoint,
            StoryCameraZoneId.HomeOverview,
            points,
            new Vector3(0.85f, 1.0f, 0.85f));

        interactions.inspectWardrobe = StoryChapterBuilderCommon.AddInteractable(
            world.wardrobeInspectFace,
            "home.inspect.wardrobe",
            "DOLABIN DÜŞME ALANINI ZEMİNE İNDİR",
            StoryInteractionKind.Inspect,
            wardrobePoint,
            StoryInteractionGesture.SwipeDown,
            StoryCameraZoneId.HomeWardrobe,
            false,
            1,
            1.15f,
            2.2f);
        interactions.inspectShelf = StoryChapterBuilderCommon.AddInteractable(
            world.shelfFrame,
            "home.inspect.shelf",
            "RAFIN DÜŞME ALANINI ZEMİNE İNDİR",
            StoryInteractionKind.Inspect,
            shelfPoint,
            StoryInteractionGesture.SwipeDown,
            StoryCameraZoneId.HomeShelf,
            false,
            1,
            1.15f,
            2.2f);
        interactions.inspectExit = AddPreviewDragInteraction(
            world.canReadingNestRisk,
            world.canReadingNestSafe,
            "home.risk.safeplay",
            "CAN'IN OKUMA KÖŞESİNİ TURUNCU ALANDAN TAŞI",
            StoryInteractionKind.HelpSibling,
            safePlayPoint,
            StoryCameraZoneId.HomeOverview,
            points,
            new Vector3(1.4f, 0.35f, 1.2f));
        interactions.moveShoes = AddPreviewDragInteraction(
            world.exitStart[0],
            world.exitStored[0],
            "home.exit.shoes",
            "AYAKKABILARI AYAKKABILIĞA TAŞI",
            StoryInteractionKind.Collect,
            exitPoint,
            StoryCameraZoneId.HomeExit,
            points,
            new Vector3(1.1f, 0.45f, 1.0f));
        interactions.moveToy = AddPreviewDragInteraction(
            world.exitStart[1],
            world.exitStored[1],
            "home.exit.toy",
            "OYUNCAK KUTUSUNU OYUN ALANINA TAŞI",
            StoryInteractionKind.Collect,
            exitPoint,
            StoryCameraZoneId.HomeExit,
            points,
            new Vector3(1.15f, 0.65f, 1.15f));
        interactions.moveParcel = AddPreviewDragInteraction(
            world.exitStart[2],
            world.exitStored[2],
            "home.exit.parcel",
            "PAKETİ KAPI YAYININ DIŞINA TAŞI",
            StoryInteractionKind.Collect,
            exitPoint,
            StoryCameraZoneId.HomeExit,
            points,
            new Vector3(1.15f, 0.65f, 1.0f));

        interactions.lowerBooks = AddPreviewDragInteraction(
            world.shelfHigh[0],
            world.shelfLow[0],
            "home.shelf.books",
            "AĞIR KİTAPLARI ALT RAFA İNDİR",
            StoryInteractionKind.Collect,
            shelfPoint,
            StoryCameraZoneId.HomeShelf,
            points,
            new Vector3(0.9f, 0.75f, 1.35f));
        interactions.lowerVase = AddPreviewDragInteraction(
            world.shelfHigh[1],
            world.shelfLow[1],
            "home.shelf.vase",
            "SAKSIYI ALÇAK GÜVENLİ YÜZEYE TAŞI",
            StoryInteractionKind.Collect,
            shelfPoint,
            StoryCameraZoneId.HomeShelf,
            points,
            new Vector3(0.9f, 0.75f, 0.9f));
        interactions.lowerFrame = AddPreviewDragInteraction(
            world.shelfHigh[2],
            world.shelfLow[2],
            "home.shelf.frame",
            "ÇERÇEVEYİ DUVARA YAKIN YUVAYA KAYDIR",
            StoryInteractionKind.Collect,
            shelfPoint,
            StoryCameraZoneId.HomeShelf,
            points,
            new Vector3(0.85f, 0.75f, 0.85f));
        interactions.markShelfAnchor = StoryChapterBuilderCommon.AddInteractable(
            world.shelfAnchorMark,
            "home.shelf.mark",
            "RAF BAĞLANTI PLAKASINDA BASILI TUT",
            StoryInteractionKind.Inspect,
            shelfPoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeShelf,
            false,
            1,
            1.2f,
            2.2f);
        interactions.handShelfBracket = AddPreviewDragInteraction(
            world.shelfBracket,
            family.parent,
            "home.shelf.bracket",
            "BAĞLANTI PARÇASINI ANNEYE VER",
            StoryInteractionKind.HelpSibling,
            parentPoint,
            StoryCameraZoneId.HomeParent,
            points,
            new Vector3(1.05f, 1.35f, 1.05f));
        interactions.testSecuredShelf = StoryChapterBuilderCommon.AddInteractable(
            world.shelfStabilityHandle,
            "home.shelf.retest",
            "SABİTLENEN RAFI TEKRAR SINAYIN",
            StoryInteractionKind.Inspect,
            shelfPoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeShelf,
            false,
            1,
            1.0f,
            2.2f);

        interactions.testWardrobe = StoryChapterBuilderCommon.AddInteractable(
            world.wardrobeTestHandle,
            "home.wardrobe.test",
            "DOLABIN YANINDA BASILI TUT",
            StoryInteractionKind.Inspect,
            wardrobePoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeWardrobe,
            false,
            1,
            1.15f,
            2.1f);
        interactions.markWardrobeAnchors = StoryChapterBuilderCommon.AddInteractable(
            world.wardrobeAnchorMarks,
            "home.wardrobe.mark",
            "DOLABIN BAĞLANTI ŞERİDİNDE BASILI TUT",
            StoryInteractionKind.Inspect,
            wardrobePoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeWardrobe,
            false,
            1,
            1.25f,
            2.5f);
        interactions.handWardrobeStrap = AddPreviewDragInteraction(
            world.wardrobeHandStrap,
            family.parent,
            "home.wardrobe.strap",
            "SABİTLEME KAYIŞINI ANNEYE VER",
            StoryInteractionKind.HelpSibling,
            parentPoint,
            StoryCameraZoneId.HomeParent,
            points,
            new Vector3(1.05f, 1.35f, 1.05f));
        interactions.testSecuredWardrobe = StoryChapterBuilderCommon.AddInteractable(
            world.wardrobeStabilityHandle,
            "home.wardrobe.retest",
            "SABİTLENEN DOLABI TEKRAR SINAYIN",
            StoryInteractionKind.Inspect,
            wardrobePoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeWardrobe,
            false,
            1,
            1.0f,
            2.2f);

        interactions.unsafeHeavyLift = StoryChapterBuilderCommon.AddInteractable(
            world.unsafeHeavyBox,
            "home.unsafe.heavy",
            "AĞIR DOLABI ÇEKMEYİ DENE",
            StoryInteractionKind.UnsafeChoice,
            wardrobePoint,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.HomeWardrobe,
            false,
            1,
            0.75f,
            2f);
        interactions.unsafeDrill = StoryChapterBuilderCommon.AddInteractable(
            world.unsafeDrill,
            "home.unsafe.drill",
            "MATKABA DOKUN",
            StoryInteractionKind.UnsafeChoice,
            parentPoint,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.HomeParent,
            false,
            1,
            0.6f,
            2f);
        interactions.testExitDoor = AddPreviewDragInteraction(
            world.finalRouteCarStart,
            world.finalRouteCarFinish,
            "home.exit.final",
            "ARABAYI AÇIK KAPI EŞİĞİNE SÜR",
            StoryInteractionKind.Exit,
            routePoint,
            StoryCameraZoneId.HomeFinalTest,
            points,
            new Vector3(1.15f, 0.35f, 1.15f));

        UnityEventTools.AddPersistentListener(interactions.testInitialRoute.OnInteracted, director.TestInitialRoute);
        UnityEventTools.AddPersistentListener(interactions.openDoorForNermin.OnInteracted, director.OpenDoorForNermin);
        UnityEventTools.AddPersistentListener(interactions.returnNerminEnvelope.OnInteracted, director.ReturnNerminEnvelope);
        UnityEventTools.AddPersistentListener(interactions.placeEvacuationPlan.OnInteracted, director.PlaceEvacuationPlan);
        UnityEventTools.AddPersistentListener(interactions.inspectWardrobe.OnInteracted, director.InspectWardrobe);
        UnityEventTools.AddPersistentListener(interactions.inspectShelf.OnInteracted, director.InspectShelf);
        UnityEventTools.AddPersistentListener(interactions.inspectExit.OnInteracted, director.InspectExit);
        UnityEventTools.AddBoolPersistentListener(
            interactions.inspectExit.OnInteracted,
            world.canReadingNestRisk.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            interactions.inspectExit.OnInteracted,
            world.canReadingNestSafe.SetActive,
            true);
        UnityEventTools.AddPersistentListener(interactions.moveShoes.OnInteracted, director.MoveExitShoes);
        UnityEventTools.AddPersistentListener(interactions.moveToy.OnInteracted, director.MoveExitToy);
        UnityEventTools.AddPersistentListener(interactions.moveParcel.OnInteracted, director.MoveExitParcel);
        UnityEventTools.AddPersistentListener(interactions.lowerBooks.OnInteracted, director.LowerShelfBooks);
        UnityEventTools.AddPersistentListener(interactions.lowerVase.OnInteracted, director.LowerShelfVase);
        UnityEventTools.AddPersistentListener(interactions.lowerFrame.OnInteracted, director.LowerShelfFrame);
        UnityEventTools.AddPersistentListener(interactions.markShelfAnchor.OnInteracted, director.MarkShelfAnchor);
        UnityEventTools.AddPersistentListener(interactions.handShelfBracket.OnInteracted, director.HandShelfBracket);
        UnityEventTools.AddPersistentListener(interactions.testSecuredShelf.OnInteracted, director.TestSecuredShelf);
        UnityEventTools.AddPersistentListener(interactions.testWardrobe.OnInteracted, director.TestWardrobe);
        UnityEventTools.AddPersistentListener(interactions.markWardrobeAnchors.OnInteracted, director.MarkWardrobeAnchors);
        UnityEventTools.AddPersistentListener(interactions.handWardrobeStrap.OnInteracted, director.HandWardrobeStrap);
        UnityEventTools.AddPersistentListener(interactions.testSecuredWardrobe.OnInteracted, director.TestSecuredWardrobe);
        UnityEventTools.AddPersistentListener(interactions.unsafeHeavyLift.OnInteracted, director.TryUnsafeHeavyLift);
        UnityEventTools.AddPersistentListener(interactions.unsafeDrill.OnInteracted, director.TryUnsafeDrill);
        UnityEventTools.AddPersistentListener(interactions.testExitDoor.OnInteracted, director.TestExitDoor);
        return interactions;
    }

    private static StoryInteractable AddPreviewDragInteraction(
        GameObject source,
        GameObject target,
        string id,
        string prompt,
        StoryInteractionKind kind,
        Transform interactionPoint,
        StoryCameraZoneId cameraZone,
        Transform dropZoneParent,
        Vector3 dropZoneSize)
    {
        StoryInteractable interaction = StoryChapterBuilderCommon.AddInteractable(
            source,
            id,
            prompt,
            kind,
            interactionPoint,
            StoryInteractionGesture.DragToTarget,
            cameraZone,
            false,
            1,
            1.25f,
            2.4f);
        BagDropZone zone = CreatePreviewDropZone(
            "Drop_" + id.Replace('.', '_'),
            dropZoneParent,
            target.transform.position,
            dropZoneSize);
        ConfigurePreviewDrag(interaction, zone);
        return interaction;
    }

    private static BagDropZone CreatePreviewDropZone(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 size)
    {
        GameObject zoneObject = new GameObject(name);
        zoneObject.transform.SetParent(parent);
        zoneObject.transform.position = position;
        BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return zoneObject.AddComponent<BagDropZone>();
    }

    private static void ConfigurePreviewDrag(StoryInteractable interaction, BagDropZone target)
    {
        if (interaction == null || target == null)
            throw new InvalidOperationException("Story 02 hedefli sürükleme bağlantısı eksik.");

        GameObject source = interaction.gameObject;
        BoxCollider rootCollider = source.GetComponent<BoxCollider>() ?? source.AddComponent<BoxCollider>();
        FitPreviewCollider(rootCollider, source);
        rootCollider.isTrigger = false;

        DraggableItem draggable = source.GetComponent<DraggableItem>() ?? source.AddComponent<DraggableItem>();
        SerializedObject dragData = new SerializedObject(draggable);
        dragData.FindProperty("isCorrectItem").boolValue = true;
        dragData.FindProperty("displayName").stringValue = interaction.Prompt;
        dragData.FindProperty("inputEnabled").boolValue = false;
        dragData.FindProperty("notifyGameManager").boolValue = false;
        dragData.FindProperty("tapToBagEnabled").boolValue = false;
        dragData.FindProperty("dropZoneOverride").objectReferenceValue = target;
        dragData.FindProperty("dragLift").floatValue = 0.12f;
        dragData.FindProperty("dragScale").floatValue = 1.04f;
        dragData.FindProperty("returnDuration").floatValue = 0.36f;
        dragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        dragData.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(interaction, target.transform);
    }

    private static void ConfigureRebuildPreviewDirector(
        StoryHomeSafetyDirector director,
        HomeWorld world,
        HomeInteractions interactions)
    {
        SerializedObject serialized = new SerializedObject(director);
        StoryChapterBuilderCommon.Set(serialized, "testInitialRoute", interactions.testInitialRoute);
        StoryChapterBuilderCommon.Set(serialized, "initialRouteCarStart", world.initialRouteCarStart);
        StoryChapterBuilderCommon.Set(serialized, "initialRouteCarBlocked", world.initialRouteCarBlocked);
        StoryChapterBuilderCommon.Set(serialized, "initialRouteCarAnimation", world.initialRouteCarAnimation);
        StoryChapterBuilderCommon.Set(serialized, "openDoorForNermin", interactions.openDoorForNermin);
        StoryChapterBuilderCommon.Set(serialized, "returnNerminEnvelope", interactions.returnNerminEnvelope);
        StoryChapterBuilderCommon.Set(serialized, "nermin", world.nermin);
        StoryChapterBuilderCommon.Set(serialized, "nerminEnvelopeStart", world.nerminEnvelopeStart);
        StoryChapterBuilderCommon.Set(serialized, "nerminEnvelopeReturned", world.nerminEnvelopeReturned);
        StoryChapterBuilderCommon.Set(serialized, "placeEvacuationPlan", interactions.placeEvacuationPlan);
        StoryChapterBuilderCommon.Set(serialized, "evacuationPlanInHand", world.evacuationPlanInHand);
        StoryChapterBuilderCommon.Set(serialized, "evacuationPlan", world.evacuationPlan);
        StoryChapterBuilderCommon.Set(serialized, "nerminExitAnimation", world.nerminExitAnimation);
        serialized.FindProperty("physicalRouteFlow").boolValue = true;
        StoryChapterBuilderCommon.Set(serialized, "shelfRiskZoneUnstable", world.shelfRiskZoneUnstable);
        StoryChapterBuilderCommon.Set(serialized, "shelfRiskZoneSecured", world.shelfRiskZoneSecured);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeRiskZoneUnstable", world.wardrobeRiskZoneUnstable);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeRiskZoneSecured", world.wardrobeRiskZoneSecured);
        StoryChapterBuilderCommon.Set(serialized, "canReadingNestRisk", world.canReadingNestRisk);
        StoryChapterBuilderCommon.Set(serialized, "canReadingNestSafe", world.canReadingNestSafe);
        StoryChapterBuilderCommon.Set(serialized, "testClearedExitDoor", interactions.testClearedExitDoor);
        StoryChapterBuilderCommon.Set(serialized, "markShelfAnchor", interactions.markShelfAnchor);
        StoryChapterBuilderCommon.Set(serialized, "testSecuredShelf", interactions.testSecuredShelf);
        StoryChapterBuilderCommon.Set(serialized, "shelfStabilityAnimation", world.shelfStabilityAnimation);
        StoryChapterBuilderCommon.Set(serialized, "testSecuredWardrobe", interactions.testSecuredWardrobe);
        StoryChapterBuilderCommon.Set(serialized, "finalRouteCarStart", world.finalRouteCarStart);
        StoryChapterBuilderCommon.Set(serialized, "finalRouteCarFinish", world.finalRouteCarFinish);
        StoryChapterBuilderCommon.Set(serialized, "canToyCarPocket", world.canToyCarPocket);
        StoryChapterBuilderCommon.Set(serialized, "finalRouteCarAnimation", world.finalRouteCarAnimation);
        serialized.FindProperty("drillWorkSeconds").floatValue = 8.2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Animation[] CreatePreviewMoveAnimations(
        GameObject[] starts,
        GameObject[] results,
        string namePrefix,
        float duration)
    {
        Animation[] animations = new Animation[Math.Min(starts.Length, results.Length)];
        for (int index = 0; index < animations.Length; index++)
        {
            Vector3 offset = starts[index].transform.localPosition - results[index].transform.localPosition;
            animations[index] = StoryChapterBuilderCommon.CreateMoveAnimation(
                results[index],
                namePrefix + index,
                offset,
                duration);
        }
        return animations;
    }

    private static GameObject CreatePreviewHotspot(
        string name,
        Vector3 position,
        Vector3 size,
        Material material,
        Transform parent)
    {
        GameObject hotspot = StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Cube,
            position,
            size,
            material,
            parent,
            true);
        Renderer renderer = hotspot.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
        return hotspot;
    }

    private static void ConfigureRebuildPreviewText(Transform root)
    {
        TMP_Text title = StorySharedHomePrefabBuilder.FindDescendant(root, "ObjectiveTitle")
            ?.GetComponent<TMP_Text>();
        TMP_Text detail = StorySharedHomePrefabBuilder.FindDescendant(root, "ObjectiveDetail")
            ?.GetComponent<TMP_Text>();
        if (title != null)
            title.text = "ÇIKIŞ ROTASINI DENE";
        if (detail != null)
            detail.text = "Oyuncak arabayı çıkış kapısına kadar sürükle.";
    }

    private static StoryChapterBuilderCommon.Materials LoadPreviewMaterials()
    {
        return new StoryChapterBuilderCommon.Materials
        {
            wall = LoadPreviewMaterial("Chapter_Wall"),
            floor = LoadPreviewMaterial("Chapter_WarmFloor"),
            concrete = LoadPreviewMaterial("Chapter_Concrete"),
            asphalt = LoadPreviewMaterial("Chapter_Asphalt"),
            grass = LoadPreviewMaterial("Chapter_Grass"),
            cream = LoadPreviewMaterial("Cream"),
            navy = LoadPreviewMaterial("Navy"),
            teal = LoadPreviewMaterial("Teal"),
            amber = LoadPreviewMaterial("Amber"),
            coral = LoadPreviewMaterial("Coral"),
            wood = LoadPreviewMaterial("Wood"),
            metal = LoadPreviewMaterial("Chapter_Metal"),
            glass = LoadPreviewMaterial("WindowGlass"),
            dust = LoadPreviewMaterial("Chapter_Dust"),
            dark = LoadPreviewMaterial("Chapter_Dark"),
            white = LoadPreviewMaterial("Chapter_White")
        };
    }

    private static Material LoadPreviewMaterial(string name)
    {
        string path = StoryChapterBuilderCommon.MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new InvalidOperationException("Hazır Story materyali bulunamadı: " + path);
        return material;
    }

    private static void FitPreviewCollider(BoxCollider collider, GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one * 0.5f;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
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
        Bounds local = new Bounds(collider.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(collider.transform.InverseTransformPoint(corner));
        collider.center = local.center;
        collider.size = Vector3.Max(local.size, Vector3.one * 0.12f);
    }

    private static Transform FindPreviewRequired(Transform root, string name)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, name);
        if (target == null)
            throw new InvalidOperationException("Story 02 ortak ev nesnesi bulunamadı: " + name);
        return target;
    }

    private static void SetPreviewActive(Transform root, string name, bool active)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, name);
        if (target != null)
            target.gameObject.SetActive(active);
    }

    private static void RequirePreviewAsset(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Story 02 rebuild asseti bulunamadı.", path);
    }
}

public static class StoryHomeSafetyRebuildPreviewValidator
{
    [MenuItem("Tools/Deprem Story/Validate Story_02 Rebuild Preview")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    [MenuItem("Tools/Deprem Story/Validate Story_02 Rebuild Preview (Silent)")]
    public static void ValidateSilentFromMenu()
    {
        Validate(false);
    }

    public static void Validate(bool showDialog)
    {
        if (!File.Exists(StoryHomeSafetySceneBuilder.RebuildPreviewScenePath))
            throw new FileNotFoundException(
                "Story 02 rebuild önizleme sahnesi bulunamadı.",
                StoryHomeSafetySceneBuilder.RebuildPreviewScenePath);

        Scene scene = SceneManager.GetSceneByPath(StoryHomeSafetySceneBuilder.RebuildPreviewScenePath);
        bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
        if (openedForValidation)
            scene = EditorSceneManager.OpenScene(
                StoryHomeSafetySceneBuilder.RebuildPreviewScenePath,
                OpenSceneMode.Additive);

        try
        {
            GameObject root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == "STORY_02_REBUILD_PREVIEW");
            Require(root != null, "STORY_02_REBUILD_PREVIEW kökü");

            GameObject sharedHome = root.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name == "StoryHome_Shared")
                .Select(candidate => candidate.gameObject)
                .SingleOrDefault();
            Require(sharedHome != null, "tek StoryHome_Shared instance'ı");
            Require(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome) ==
                StorySharedHomePrefabBuilder.PrefabPath,
                "bağlantılı ortak ev prefabı");
            Require(
                root.GetComponentsInChildren<NavMeshSurface>(true).Length == 1,
                "sahneye ait tek NavMeshSurface");
            Require(
                root.GetComponentsInChildren<StoryHomeSafetyDirector>(true).Length == 1,
                "tek StoryHomeSafetyDirector");
            Require(
                root.GetComponentsInChildren<StoryTouchManager>(true).Length == 1,
                "tek StoryTouchManager");
            Require(
                root.GetComponentsInChildren<StoryPlayerMovement>(true).Length == 1,
                "tek StoryPlayerMovement");
            Require(
                root.GetComponentsInChildren<StoryActionButton>(true).Length == 0,
                "merkez görev butonu bulunmaması");

            StoryInteractable[] interactions = root.GetComponentsInChildren<StoryInteractable>(true);
            Require(interactions.Length >= 22, "en az 22 oynanabilir sahne içi etkileşim");
            StoryInteractable[] directDrags = interactions
                .Where(candidate => candidate.InteractionGesture == StoryInteractionGesture.DragToTarget)
                .ToArray();
            Require(directDrags.Length >= 12, "en az 12 doğrudan hedefli sürükleme");
            foreach (StoryInteractable interaction in directDrags)
            {
                DraggableItem draggable = interaction.GetComponent<DraggableItem>();
                Require(draggable != null, interaction.InteractionId + " DraggableItem");
                Require(draggable.DropZoneOverride != null, interaction.InteractionId + " fiziksel hedef alanı");
                Require(interaction.GestureTarget == draggable.DropZoneOverride.transform,
                    interaction.InteractionId + " görünür jest hedefi");
            }

            Require(
                interactions.Any(candidate => candidate.InteractionId == "home.route.initial"),
                "ilk oyuncak araba rota testi");
            Require(
                interactions.Any(candidate => candidate.InteractionId == "home.neighbor.door"),
                "Nermin için doğrudan kapı açma");
            Require(
                interactions.Any(candidate => candidate.InteractionId == "home.neighbor.envelope"),
                "Nermin zarfını fiziksel geri verme");
            Require(
                interactions.Any(candidate => candidate.InteractionId == "home.exit.final"),
                "final oyuncak araba rota testi");
            Require(
                root.GetComponentsInChildren<Transform>(true).Any(candidate => candidate.name == "Nermin_Neighbor"),
                "Nermin sahne karakteri");
            Require(
                root.GetComponentsInChildren<CinemachineCamera>(true).Length == 6,
                "altı bestelenmiş Cinemachine kamera");
            foreach (CinemachineCamera camera in root.GetComponentsInChildren<CinemachineCamera>(true))
                Require(
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f,
                    camera.name + " lens aralığı");
            Require(
                EditorBuildSettings.scenes.Any(candidate =>
                    candidate.enabled && candidate.path == StoryHomeSafetySceneBuilder.RebuildPreviewScenePath),
                "Story 02 yayın sahnesinin Build Settings'te olması");

            Debug.Log(
                $"Story_02_RebuildPreview doğrulandı: interactions={interactions.Length}, " +
                $"directDrags={directDrags.Length}, cameras=6, sharedHome=connected");
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 02 rebuild önizlemesi yapısal doğrulamayı geçti.",
                    "Tamam");
        }
        finally
        {
            if (openedForValidation && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("Story 02 rebuild doğrulama hatası: " + label);
    }
}
