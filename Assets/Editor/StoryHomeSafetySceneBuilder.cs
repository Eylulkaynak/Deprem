using System;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StoryHomeSafetySceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_02_HomeSafety.unity";
    private const string LegacyPath = "Assets/Scenes/Bolum2.unity";
    private const string BackupPath = "Assets/Scenes/LegacyBackups/Bolum2_OriginalGameplay_2026-07-17.unity";

    private sealed class HomeWorld
    {
        internal GameObject environment;
        internal GameObject wardrobe;
        internal GameObject wardrobeInspectFace;
        internal GameObject wardrobeTestFace;
        internal GameObject shelfFrame;
        internal GameObject exitThreshold;
        internal GameObject closedDoor;
        internal GameObject openDoor;
        internal Transform exitStandPoint;
        internal GameObject[] exitStart;
        internal GameObject[] exitStored;
        internal Animation[] exitAnimations;
        internal GameObject[] shelfHigh;
        internal GameObject[] shelfLow;
        internal Animation[] shelfAnimations;
        internal GameObject shelfBracket;
        internal GameObject shelfAnchorStrap;
        internal Animation shelfSecureAnimation;
        internal ParticleSystem shelfDust;
        internal GameObject wardrobeTestHandle;
        internal GameObject wardrobeAnchorMarks;
        internal GameObject wardrobeHandStrap;
        internal GameObject wardrobeAnchorStrap;
        internal Animation wardrobeRockAnimation;
        internal Animation wardrobeSecureAnimation;
        internal ParticleSystem wardrobeDust;
        internal GameObject unsafeHeavyBox;
        internal GameObject unsafeDrill;
        internal GameObject parentHeldDrill;
        internal AudioSource drillWorkAudio;
        internal Transform shelfParentWorkPoint;
        internal Transform wardrobeParentWorkPoint;
        internal Animation heavyAnimation;
        internal Animation drillAnimation;
        internal Animation doorAnimation;
    }

    private sealed class HomeInteractions
    {
        internal StoryInteractable inspectWardrobe;
        internal StoryInteractable inspectShelf;
        internal StoryInteractable inspectExit;
        internal StoryInteractable moveShoes;
        internal StoryInteractable moveToy;
        internal StoryInteractable moveParcel;
        internal StoryInteractable lowerBooks;
        internal StoryInteractable lowerVase;
        internal StoryInteractable lowerFrame;
        internal StoryInteractable handShelfBracket;
        internal StoryInteractable testWardrobe;
        internal StoryInteractable markWardrobeAnchors;
        internal StoryInteractable handWardrobeStrap;
        internal StoryInteractable unsafeHeavyLift;
        internal StoryInteractable unsafeDrill;
        internal StoryInteractable testExitDoor;
    }

    [MenuItem("Tools/Deprem Story/Build Story_02_HomeSafety")]
    public static void BuildFromMenu() => Build(true);

    [MenuItem("Tools/Deprem Story/Build Story_02_HomeSafety (Silent)")]
    public static void BuildSilentFromMenu() => Build(false);

    public static void BuildFromCommandLine() => Build(false);

    private static void Build(bool showDialog)
    {
        try
        {
            StoryChapterBuilderCommon.EnsureFolders();
            StoryChapterBuilderCommon.EnsureLegacyBackup(LegacyPath, BackupPath);
            StoryChapterBuilderCommon.Materials materials = StoryChapterBuilderCommon.CreateMaterials();
            UnityEngine.Rendering.VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();
            RuntimeAnimatorController controller = StoryAnimationLibraryBuilder.BuildLibrary(false);
            RuntimeAnimatorController adultController = StoryAnimationLibraryBuilder.LoadAdultController();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_02_HOME_SAFETY");
            HomeWorld world = BuildWorld(root.transform, materials);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(root.transform, controller, true,
                new Vector3(-0.5f, 0f, -1.4f), new Vector3(-1.55f, 0f, -1.0f), new Vector3(1.25f, 0f, 0.15f),
                adultController);
            BuildParentDrillRig(world, family.parent.transform);

            GameObject sessionObject = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(gameManager, StoryAct.HomeSafety,
                new[] { StoryFlag.BagReady, StoryFlag.BagFlashlight, StoryFlag.BagFirstAid, StoryFlag.BagWater });

            GameObject core = new GameObject("_StoryHomeSafetyCore");
            core.transform.SetParent(root.transform);
            StoryHomeSafetyDirector director = core.AddComponent<StoryHomeSafetyDirector>();
            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);

            StoryChapterBuilderCommon.CameraSpec[] cameraSpecs =
            {
                new(StoryCameraZoneId.HomeOverview, "CM_Home_Overview", new Vector3(8.6f, 8.8f, -10.4f),
                    new Vector3(0f, 0.95f, 0.35f), 44f, false, family.deniz.transform, 15.2f, new Vector2(-0.08f, 0.1f)),
                new(StoryCameraZoneId.HomeWardrobe, "CM_Home_Wardrobe", new Vector3(0.2f, 3.6f, -1.8f),
                    new Vector3(-4.15f, 1.55f, 2.2f), 46f),
                new(StoryCameraZoneId.HomeShelf, "CM_Home_Shelf", new Vector3(1.4f, 3.1f, 2.0f),
                    new Vector3(4.05f, 1.35f, 4.95f), 40f),
                new(StoryCameraZoneId.HomeExit, "CM_Home_Exit", new Vector3(0.2f, 2.45f, -2.0f),
                    new Vector3(3.65f, 0.72f, -4.82f), 40f),
                new(StoryCameraZoneId.HomeParent, "CM_Home_ParentWork", new Vector3(-1.6f, 2.7f, -1.8f),
                    new Vector3(1.25f, 0.98f, 0.15f), 38f, false, family.parent.transform, 4.8f,
                    new Vector2(0.12f, 0.08f)),
                new(StoryCameraZoneId.HomeFinalTest, "CM_Home_FinalExit", new Vector3(0.65f, 2.1f, -2.45f),
                    new Vector3(3.65f, 0.72f, -5.2f), 38f)
            };
            StoryCameraController cameraController = StoryChapterBuilderCommon.BuildCameras(root.transform,
                StoryCameraZoneId.HomeOverview, cameraSpecs, out Camera mainCamera, out CinemachineBrain brain);
            StoryChapterBuilderCommon.ChapterUI ui = StoryChapterBuilderCommon.BuildUI(root.transform, cameraController,
                "StoryUI_HomeSafety", "2. PERDE • EVİ GÜVENLİ YAP", "EVİN RİSKLERİ AZALTILDI",
                "Çocuklar ağır mobilya veya elektrikli alet kullanmaz; sabitlemeleri yetişkin yapar.");

            StoryChapterBuilderCommon.SetReference(ui.controller, "movementOwner", movement);
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", ui.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchSerialized = new SerializedObject(touch);
            touchSerialized.FindProperty("directWorldGestures").boolValue = true;
            touchSerialized.ApplyModifiedPropertiesWithoutUndo();

            HomeInteractions interactions = BuildInteractions(world, family, director);
            ConfigureDirector(director, gameManager, movement, touch, cameraController, ui, family, world, interactions);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(ui.controller, "cameraController", cameraController);

            StoryChapterBuilderCommon.BuildLighting(root.transform, volume, new Color(1f, 0.88f, 0.72f), 1.35f);
            AudioClip ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(StoryChapterBuilderCommon.AudioRoot + "/calm_home.wav");
            if (ambience != null)
                StoryChapterBuilderCommon.CreateAudioSource("HomeSafetyAmbience", root.transform, ambience, 0.14f, true, true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            StoryChapterBuilderCommon.BuildNavigation(world.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            StoryChapterBuilderCommon.AddScenesToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeGameObject = root;
            Debug.Log("Story_02_HomeSafety built successfully: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_02_HomeSafety oynanabilir hikâye sahnesi üretildi.", "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_02 üretilemedi:\n" + exception.Message, "Tamam");
            throw;
        }
    }

    private static HomeWorld BuildWorld(Transform parent, StoryChapterBuilderCommon.Materials m)
    {
        HomeWorld world = new HomeWorld();
        GameObject environment = new GameObject("HomeSafetyEnvironment");
        environment.transform.SetParent(parent);
        world.environment = environment;
        Transform room = StoryChapterBuilderCommon.NewChild(environment.transform, "FamilyLivingRoom");

        StoryChapterBuilderCommon.CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f),
            new Vector3(12f, 0.24f, 12f), m.floor, room);
        StoryChapterBuilderCommon.CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 1.8f, 5.9f),
            new Vector3(12f, 3.6f, 0.22f), m.wall, room);
        StoryChapterBuilderCommon.CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-5.9f, 1.8f, 0f),
            new Vector3(0.22f, 3.6f, 12f), m.wall, room);
        StoryChapterBuilderCommon.CreatePrimitive("RightWallRear", PrimitiveType.Cube, new Vector3(5.9f, 1.8f, 1.75f),
            new Vector3(0.22f, 3.6f, 8.5f), m.wall, room);
        StoryChapterBuilderCommon.CreatePrimitive("FrontWallLeft", PrimitiveType.Cube, new Vector3(-1.2f, 1.8f, -5.9f),
            new Vector3(9.4f, 3.6f, 0.22f), m.wall, room);
        StoryChapterBuilderCommon.CreatePrimitive("FrontWallRight", PrimitiveType.Cube, new Vector3(5.25f, 1.8f, -5.9f),
            new Vector3(1.3f, 3.6f, 0.22f), m.wall, room);

        BuildWindow(room, m);
        StoryChapterBuilderCommon.InstantiateFurniture("Furniture/Couch_11.prefab", "FamilyCouch", room,
            new Vector3(-0.4f, 0f, 4.7f), new Vector3(3.6f, 1.3f, 1.35f), new Vector3(0f, 180f, 0f));
        StoryChapterBuilderCommon.InstantiateFurniture("Furniture/Coffee_Table_03.prefab", "CoffeeTable", room,
            new Vector3(0.2f, 0f, 1.75f), new Vector3(2.4f, 0.78f, 1.4f));
        StoryChapterBuilderCommon.InstantiateFurniture("Furniture/Armchair_18.prefab", "ReadingChair", room,
            new Vector3(-3.2f, 0f, -2.25f), new Vector3(1.45f, 1.35f, 1.45f), new Vector3(0f, 35f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive("Rug", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0.7f),
            new Vector3(5.5f, 0.05f, 4.2f), m.navy, room, false);
        StoryChapterBuilderCommon.CreatePrimitive("RugInset", PrimitiveType.Cube, new Vector3(0f, 0.052f, 0.7f),
            new Vector3(5.1f, 0.025f, 3.8f), m.teal, room, false);

        world.wardrobe = StoryChapterBuilderCommon.InstantiateFurniture("Furniture/Closet_02.prefab", "TallWardrobe", room,
            new Vector3(-4.35f, 0f, 2.45f), new Vector3(1.75f, 2.85f, 0.9f), new Vector3(0f, 180f, 0f));
        world.wardrobeInspectFace = StoryChapterBuilderCommon.CreatePrimitive("WardrobeRiskFace", PrimitiveType.Cube,
            new Vector3(-4.35f, 1.48f, 1.94f), new Vector3(1.38f, 2.48f, 0.08f), m.wood, room, true);
        world.wardrobeInspectFace.GetComponent<Renderer>().enabled = false;
        world.wardrobeTestFace = StoryChapterBuilderCommon.CreatePrimitive("WardrobeSideTest", PrimitiveType.Cube,
            new Vector3(-5.22f, 1.45f, 2.45f), new Vector3(0.08f, 2.35f, 0.72f), m.navy, room, true);
        world.wardrobeTestFace.GetComponent<Renderer>().enabled = false;
        world.wardrobeTestHandle = StoryChapterBuilderCommon.CreatePrimitive("WardrobeTestHandle", PrimitiveType.Cylinder,
            new Vector3(-5.27f, 1.35f, 2.05f), new Vector3(0.055f, 0.24f, 0.055f), m.metal, room, true,
            Quaternion.Euler(90f, 0f, 0f));
        world.wardrobeTestHandle.GetComponent<Renderer>().enabled = false;
        world.wardrobeAnchorMarks = StoryAuthoredPropFactory.CreateMetalBracket("WardrobeAnchorMarks", room,
            new Vector3(-4.35f, 2.72f, 2.78f), new Vector3(1.15f, 0.42f, 0.34f),
            new Vector3(0f, 180f, 0f), m.metal, m.amber);
        world.wardrobeHandStrap = StoryAuthoredPropFactory.CreateMetalBracket("WardrobeHandStrap", room,
            new Vector3(0.85f, 0.72f, 1.65f), new Vector3(0.78f, 0.36f, 0.42f),
            new Vector3(0f, -18f, 0f), m.metal, m.teal);
        world.wardrobeAnchorStrap = StoryAuthoredPropFactory.CreateMetalBracket("WardrobeAnchorStrap", room,
            new Vector3(-4.35f, 2.68f, 2.82f), new Vector3(1.28f, 0.46f, 0.36f),
            new Vector3(0f, 180f, 0f), m.metal, m.teal, false);
        world.wardrobeAnchorStrap.SetActive(false);

        world.shelfFrame = BuildShelf(room, m);
        world.shelfBracket = StoryAuthoredPropFactory.CreateMetalBracket("ShelfBracketInHand", room,
            new Vector3(0.6f, 0.72f, 1.8f), new Vector3(0.48f, 0.3f, 0.4f),
            new Vector3(0f, -24f, 0f), m.metal, m.amber);
        world.shelfAnchorStrap = StoryAuthoredPropFactory.CreateMetalBracket("ShelfWallBracket", room,
            new Vector3(4.05f, 2.38f, 5.42f), new Vector3(1.36f, 0.48f, 0.38f),
            new Vector3(0f, 180f, 0f), m.metal, m.teal, false);
        world.shelfAnchorStrap.SetActive(false);

        BuildExit(room, m, world);
        BuildExitClutter(room, m, world);
        BuildShelfItems(room, m, world);

        world.unsafeHeavyBox = StoryAuthoredPropFactory.CreateParcel("UnsafeHeavyFurnitureBox", room,
            new Vector3(-3.65f, 0f, 1.55f), new Vector3(1.05f, 0.84f, 0.86f),
            new Vector3(0f, -8f, 0f), m.coral, m.cream, m.amber);
        StoryChapterBuilderCommon.CreateWorldLabel("HeavyBoxLabel", "AĞIR", new Vector3(-3.65f, 0.86f, 1.1f),
            Vector3.zero, 2.3f, StoryChapterBuilderCommon.Cream, room, new Vector2(1.4f, 0.4f));
        world.unsafeDrill = BuildDrillProp(room, m);

        world.exitAnimations = new Animation[3];
        for (int i = 0; i < world.exitAnimations.Length; i++)
        {
            Vector3 offset = world.exitStart[i].transform.localPosition - world.exitStored[i].transform.localPosition;
            world.exitAnimations[i] = StoryChapterBuilderCommon.CreateMoveAnimation(world.exitStored[i],
                "Home_ExitMove_" + i, offset, 0.58f);
        }
        world.shelfAnimations = new Animation[3];
        for (int i = 0; i < world.shelfAnimations.Length; i++)
        {
            Vector3 offset = world.shelfHigh[i].transform.localPosition - world.shelfLow[i].transform.localPosition;
            world.shelfAnimations[i] = StoryChapterBuilderCommon.CreateMoveAnimation(world.shelfLow[i],
                "Home_ShelfMove_" + i, offset, 0.62f);
        }
        world.shelfSecureAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(world.shelfAnchorStrap,
            "Home_ShelfBracketSecure", new Vector3(0f, -0.55f, -0.9f), 0.7f);
        world.wardrobeRockAnimation = StoryChapterBuilderCommon.CreateRockAnimation(world.wardrobe,
            "Home_WardrobeRock", 2.8f, 0.85f);
        world.wardrobeSecureAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(world.wardrobeAnchorStrap,
            "Home_WardrobeStrapSecure", new Vector3(4.8f, -1.7f, -1.2f), 0.78f);
        world.heavyAnimation = StoryChapterBuilderCommon.CreateRockAnimation(world.unsafeHeavyBox,
            "Home_HeavyNearMiss", 5f, 0.7f);
        world.drillAnimation = StoryChapterBuilderCommon.CreateRockAnimation(world.unsafeDrill,
            "Home_DrillNearMiss", 12f, 0.65f);
        world.shelfDust = StoryChapterBuilderCommon.CreateDust("ShelfFixDust", room, new Vector3(4.05f, 2.55f, 5.45f), m, 18);
        world.wardrobeDust = StoryChapterBuilderCommon.CreateDust("WardrobeFixDust", room,
            new Vector3(-4.35f, 2.75f, 2.75f), m, 20);
        return world;
    }

    private static void BuildWindow(Transform room, StoryChapterBuilderCommon.Materials m)
    {
        StoryChapterBuilderCommon.CreatePrimitive("WindowFrame", PrimitiveType.Cube, new Vector3(1.9f, 2.15f, 5.75f),
            new Vector3(3.1f, 1.65f, 0.16f), m.wood, room, false);
        StoryChapterBuilderCommon.CreatePrimitive("WindowGlass", PrimitiveType.Cube, new Vector3(1.9f, 2.15f, 5.65f),
            new Vector3(2.72f, 1.3f, 0.06f), m.glass, room, false);
        StoryChapterBuilderCommon.CreatePrimitive("WindowCross", PrimitiveType.Cube, new Vector3(1.9f, 2.15f, 5.58f),
            new Vector3(0.08f, 1.32f, 0.07f), m.cream, room, false);
        StoryChapterBuilderCommon.CreatePrimitive("CurtainLeft", PrimitiveType.Cube, new Vector3(0.18f, 2.0f, 5.55f),
            new Vector3(0.42f, 2.45f, 0.12f), m.amber, room, false);
        StoryChapterBuilderCommon.CreatePrimitive("CurtainRight", PrimitiveType.Cube, new Vector3(3.62f, 2.0f, 5.55f),
            new Vector3(0.42f, 2.45f, 0.12f), m.amber, room, false);
    }

    private static GameObject BuildShelf(Transform room, StoryChapterBuilderCommon.Materials m)
    {
        return StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Bookcase.fbx", "HighWallShelf", room,
            new Vector3(4.05f, 0f, 5.2f), new Vector3(2.5f, 2.78f, 0.78f),
            new Vector3(0f, 180f, 0f), false, true);
    }

    private static void BuildExit(Transform room, StoryChapterBuilderCommon.Materials m, HomeWorld world)
    {
        world.closedDoor = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Door1.fbx", "HomeExitDoor_Closed", room,
            new Vector3(3.65f, 0f, -5.72f), new Vector3(1.75f, 2.3f, 0.22f),
            Vector3.zero, false, true);
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(world.closedDoor);
        world.openDoor = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Door1.fbx", "HomeExitDoor_Open", room,
            new Vector3(2.86f, 0f, -4.92f), new Vector3(0.22f, 2.3f, 1.75f),
            new Vector3(0f, -92f, 0f), false, false);
        world.openDoor.SetActive(false);
        world.exitThreshold = StoryChapterBuilderCommon.CreatePrimitive("ExitThreshold", PrimitiveType.Cube,
            new Vector3(3.65f, 0.05f, -5.05f), new Vector3(2.1f, 0.1f, 1.18f), m.amber, room, true);
        world.exitStandPoint = StoryChapterBuilderCommon.CreatePoint("ExitStandPoint", room,
            new Vector3(3.65f, 0f, -4.15f), new Vector3(3.65f, 0.9f, -5.7f));
        world.doorAnimation = StoryChapterBuilderCommon.CreateRotationAnimation(world.openDoor, "Home_ExitDoorOpen",
            Vector3.zero, new Vector3(0f, -92f, 0f), 0.72f);
    }

    private static void BuildExitClutter(Transform room, StoryChapterBuilderCommon.Materials m, HomeWorld world)
    {
        GameObject shoes = StoryAuthoredPropFactory.CreateShoePair("ExitShoes_Start", room,
            new Vector3(3.4f, 0f, -4.68f), new Vector3(0.92f, 0.28f, 0.72f),
            new Vector3(0f, 8f, 0f), m.coral, m.navy);
        GameObject shoesStored = StoryAuthoredPropFactory.CreateShoePair("ExitShoes_Stored", room,
            new Vector3(5.1f, 0f, -3.6f), new Vector3(0.78f, 0.25f, 0.76f),
            new Vector3(0f, 90f, 0f), m.teal, m.navy);

        GameObject toy = StoryChapterBuilderCommon.InstantiateFurniture("Decorations/Toy_03.prefab", "ExitToy_Start", room,
            new Vector3(4.05f, 0f, -4.52f), new Vector3(0.72f, 0.65f, 0.72f));
        GameObject toyStored = StoryChapterBuilderCommon.InstantiateFurniture("Decorations/Toy_03.prefab", "ExitToy_Stored", room,
            new Vector3(5.08f, 0f, -2.55f), new Vector3(0.72f, 0.65f, 0.72f));
        GameObject parcel = StoryAuthoredPropFactory.CreateParcel("ExitParcel_Start", room,
            new Vector3(4.18f, 0f, -5.08f), new Vector3(0.8f, 0.6f, 0.72f),
            new Vector3(0f, -12f, 0f), m.amber, m.cream, m.coral);
        GameObject parcelStored = StoryAuthoredPropFactory.CreateParcel("ExitParcel_Stored", room,
            new Vector3(5.05f, 0f, -1.75f), new Vector3(0.8f, 0.6f, 0.72f),
            new Vector3(0f, 7f, 0f), m.teal, m.cream, m.amber);

        shoesStored.SetActive(false);
        toyStored.SetActive(false);
        parcelStored.SetActive(false);
        world.exitStart = new[] { shoes, toy, parcel };
        world.exitStored = new[] { shoesStored, toyStored, parcelStored };
    }

    private static void BuildShelfItems(Transform room, StoryChapterBuilderCommon.Materials m, HomeWorld world)
    {
        GameObject booksHigh = new GameObject("ShelfBooks_High");
        booksHigh.transform.SetParent(room);
        for (int i = 0; i < 4; i++)
            StoryChapterBuilderCommon.InstantiateFurniture(
                i % 2 == 0 ? "Decorations/Book_03.prefab" : "Decorations/Book_08.prefab", "Book_" + i,
                booksHigh.transform, new Vector3(3.45f + i * 0.25f, 2.16f, 4.76f),
                new Vector3(0.22f, 0.58f + i * 0.035f, 0.42f), new Vector3(0f, 180f, i * 2f), false);
        GameObject booksLow = new GameObject("ShelfBooks_Low");
        booksLow.transform.SetParent(room);
        for (int i = 0; i < 4; i++)
            StoryChapterBuilderCommon.InstantiateFurniture(
                i % 2 == 0 ? "Decorations/Book_03.prefab" : "Decorations/Book_08.prefab", "Book_" + i,
                booksLow.transform, new Vector3(3.18f + i * 0.25f, 0.29f, 4.76f),
                new Vector3(0.22f, 0.58f + i * 0.035f, 0.42f), new Vector3(0f, 180f, i * 2f), false);

        GameObject vaseHigh = StoryChapterBuilderCommon.InstantiateFurniture("Plants/Plants_05.prefab",
            "ShelfVase_High", room, new Vector3(4.75f, 2.16f, 4.78f),
            new Vector3(0.62f, 0.72f, 0.62f), new Vector3(0f, 18f, 0f), false);
        GameObject vaseLow = StoryChapterBuilderCommon.InstantiateFurniture("Plants/Plants_05.prefab",
            "ShelfVase_Low", room, new Vector3(4.75f, 0.29f, 4.78f),
            new Vector3(0.62f, 0.72f, 0.62f), new Vector3(0f, -15f, 0f), false);
        GameObject frameHigh = StoryChapterBuilderCommon.InstantiateFurniture("Decorations/Picture_08.prefab", "ShelfFrame_High", room,
            new Vector3(4.2f, 2.16f, 4.72f), new Vector3(0.7f, 0.75f, 0.18f));
        GameObject frameLow = StoryChapterBuilderCommon.InstantiateFurniture("Decorations/Picture_08.prefab", "ShelfFrame_Low", room,
            new Vector3(4.2f, 0.29f, 4.72f), new Vector3(0.7f, 0.75f, 0.18f));
        booksLow.SetActive(false);
        vaseLow.SetActive(false);
        frameLow.SetActive(false);
        world.shelfHigh = new[] { booksHigh, vaseHigh, frameHigh };
        world.shelfLow = new[] { booksLow, vaseLow, frameLow };
    }

    private static GameObject BuildDrillProp(Transform room, StoryChapterBuilderCommon.Materials m)
    {
        return StoryChapterBuilderCommon.InstantiateAsset("Assets/Sprites/Drill/Drill_01.obj",
            "UnsafePoweredDrill", room, new Vector3(1.35f, 0.45f, 0.85f),
            new Vector3(0.95f, 0.62f, 0.42f), new Vector3(0f, 90f, -8f), false, true);
    }

    private static void BuildParentDrillRig(HomeWorld world, Transform parent)
    {
        Transform handSlot = parent.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == "IteamSlot.R") ??
                             parent.GetComponentsInChildren<Transform>(true)
                                 .FirstOrDefault(candidate => candidate.name == "Hand.R");
        if (handSlot == null)
            throw new InvalidOperationException("Anne karakterinde sağ el eşya yuvası bulunamadı.");

        GameObject mountObject = new GameObject("ParentDrillHandMount");
        mountObject.transform.SetParent(handSlot, false);
        mountObject.transform.localPosition = new Vector3(0.02f, 0.015f, 0.025f);
        mountObject.transform.localRotation = Quaternion.Euler(0f, 90f, -8f);

        world.parentHeldDrill = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/Drill/Drill_01.obj", "ParentHeldPoweredDrill", mountObject.transform,
            mountObject.transform.position, new Vector3(0.48f, 0.31f, 0.24f), Vector3.zero, false, false);
        // InstantiateAsset dünya rotasyonunu uygular. Elde taşınan prop ise el yuvasının yerel
        // yönünü izlemeli; aksi halde animasyon sırasında matkap ucu zemine dönük kalır.
        world.parentHeldDrill.transform.localRotation = Quaternion.identity;
        if (TryGetBounds(world.parentHeldDrill, out Bounds heldBounds))
            world.parentHeldDrill.transform.position += mountObject.transform.position - heldBounds.center;

        AudioClip drillClip =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Script/freesound_community-power-drill-90294.mp3");
        if (drillClip == null)
            throw new InvalidOperationException("Matkap sesi bulunamadı: freesound_community-power-drill-90294.mp3");

        world.drillWorkAudio = world.parentHeldDrill.AddComponent<AudioSource>();
        world.drillWorkAudio.clip = drillClip;
        world.drillWorkAudio.volume = 0.42f;
        world.drillWorkAudio.loop = true;
        world.drillWorkAudio.playOnAwake = false;
        world.drillWorkAudio.spatialBlend = 1f;
        world.drillWorkAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        world.drillWorkAudio.minDistance = 0.8f;
        world.drillWorkAudio.maxDistance = 13f;
        world.parentHeldDrill.SetActive(false);
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static HomeInteractions BuildInteractions(HomeWorld world,
        StoryChapterBuilderCommon.Characters family, StoryHomeSafetyDirector director)
    {
        HomeInteractions interactions = new HomeInteractions();
        Transform points = StoryChapterBuilderCommon.NewChild(world.environment.transform, "HomeInteractionPoints");
        Transform wardrobePoint = StoryChapterBuilderCommon.CreatePoint("WardrobeStand", points,
            new Vector3(-3.15f, 0f, 1.5f), world.wardrobe.transform.position + Vector3.up);
        Transform shelfPoint = StoryChapterBuilderCommon.CreatePoint("ShelfStand", points,
            new Vector3(2.65f, 0f, 3.2f), world.shelfFrame.transform.position + Vector3.up);
        Transform exitPoint = StoryChapterBuilderCommon.CreatePoint("ExitInspectStand", points,
            new Vector3(2.75f, 0f, -3.85f), world.closedDoor.transform.position);
        Transform parentPoint = StoryChapterBuilderCommon.CreatePoint("ParentWorkStand", points,
            new Vector3(0.1f, 0f, 0.75f), new Vector3(1.25f, 1f, 0.15f));
        Vector3 shelfFacing = world.shelfAnchorStrap.transform.position;
        shelfFacing.y = 0f;
        world.shelfParentWorkPoint = StoryChapterBuilderCommon.CreatePoint("ShelfParentWorkPoint", points,
            new Vector3(2.72f, 0f, 4.52f), shelfFacing);
        Vector3 wardrobeFacing = world.wardrobeAnchorStrap.transform.position;
        wardrobeFacing.y = 0f;
        world.wardrobeParentWorkPoint = StoryChapterBuilderCommon.CreatePoint("WardrobeParentWorkPoint", points,
            new Vector3(-3.0f, 0f, 2.72f), wardrobeFacing);

        interactions.inspectWardrobe = StoryChapterBuilderCommon.AddInteractable(world.wardrobeInspectFace,
            "home.inspect.wardrobe", "DOLABIN DENGESİNİ İNCELE", StoryInteractionKind.Inspect, wardrobePoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.HomeWardrobe);
        interactions.inspectShelf = StoryChapterBuilderCommon.AddInteractable(world.shelfFrame,
            "home.inspect.shelf", "ÜST RAFI İNCELE", StoryInteractionKind.Inspect, shelfPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.HomeShelf);
        interactions.inspectExit = StoryChapterBuilderCommon.AddInteractable(world.exitThreshold,
            "home.inspect.exit", "KAPI ÖNÜNÜ İNCELE", StoryInteractionKind.Inspect, exitPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.HomeExit);

        interactions.moveShoes = StoryChapterBuilderCommon.AddInteractable(world.exitStart[0],
            "home.exit.shoes", "AYAKKABILARI AYAKKABILIĞA ÇEK", StoryInteractionKind.Collect, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeExit, false, 1, 0.9f, 2.1f);
        interactions.moveToy = StoryChapterBuilderCommon.AddInteractable(world.exitStart[1],
            "home.exit.toy", "OYUNCAĞI SEPETE ÇEK", StoryInteractionKind.Collect, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeExit, false, 1, 0.9f, 2.1f);
        interactions.moveParcel = StoryChapterBuilderCommon.AddInteractable(world.exitStart[2],
            "home.exit.parcel", "HAFİF PAKETİ KENARA ÇEK", StoryInteractionKind.Collect, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeExit, false, 1, 0.9f, 2.1f);

        interactions.lowerBooks = StoryChapterBuilderCommon.AddInteractable(world.shelfHigh[0],
            "home.shelf.books", "KİTAPLARI ALT RAFA İNDİR", StoryInteractionKind.Collect, shelfPoint,
            StoryInteractionGesture.SwipeDown, StoryCameraZoneId.HomeShelf, false, 1, 0.95f, 2.4f);
        interactions.lowerVase = StoryChapterBuilderCommon.AddInteractable(world.shelfHigh[1],
            "home.shelf.vase", "SERAMİK SAKSIYI KAPAKLI BÖLMEYE İNDİR", StoryInteractionKind.Collect, shelfPoint,
            StoryInteractionGesture.SwipeDown, StoryCameraZoneId.HomeShelf, false, 1, 0.95f, 2.4f);
        interactions.lowerFrame = StoryChapterBuilderCommon.AddInteractable(world.shelfHigh[2],
            "home.shelf.frame", "ÇERÇEVEYİ ALT RAFA İNDİR", StoryInteractionKind.Collect, shelfPoint,
            StoryInteractionGesture.SwipeDown, StoryCameraZoneId.HomeShelf, false, 1, 0.95f, 2.4f);
        interactions.handShelfBracket = StoryChapterBuilderCommon.AddInteractable(world.shelfBracket,
            "home.shelf.bracket", "BAĞLANTI PARÇASINI ANNEYE ÇEK", StoryInteractionKind.HelpSibling, parentPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeParent, true, 1, 1.1f);

        interactions.testWardrobe = StoryChapterBuilderCommon.AddInteractable(world.wardrobeTestHandle,
            "home.wardrobe.test", "DOLABIN YANINDA BASILI TUT", StoryInteractionKind.Inspect, wardrobePoint,
            StoryInteractionGesture.WorldHold, StoryCameraZoneId.HomeWardrobe, false, 1, 1.15f, 2.1f);
        interactions.markWardrobeAnchors = StoryChapterBuilderCommon.AddInteractable(world.wardrobeAnchorMarks,
            "home.wardrobe.mark", "İKİ BAĞLANTI NOKTASINI İŞARETLE", StoryInteractionKind.Inspect, wardrobePoint,
            StoryInteractionGesture.RepeatedTap, StoryCameraZoneId.HomeWardrobe, false, 2, 0.8f, 2.5f);
        interactions.handWardrobeStrap = StoryChapterBuilderCommon.AddInteractable(world.wardrobeHandStrap,
            "home.wardrobe.strap", "KAYIŞI ANNEYE ÇEK", StoryInteractionKind.HelpSibling, parentPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeParent, true, 1, 1.1f);

        interactions.unsafeHeavyLift = StoryChapterBuilderCommon.AddInteractable(world.unsafeHeavyBox,
            "home.unsafe.heavy", "AĞIR KUTUYU ÇEKMEYİ DENE", StoryInteractionKind.UnsafeChoice, wardrobePoint,
            StoryInteractionGesture.WorldHold, StoryCameraZoneId.HomeWardrobe, false, 1, 0.75f, 2f);
        interactions.unsafeDrill = StoryChapterBuilderCommon.AddInteractable(world.unsafeDrill,
            "home.unsafe.drill", "MATKABI ALMAYI DENE", StoryInteractionKind.UnsafeChoice, parentPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.HomeParent, false, 1, 0.6f, 2f);
        interactions.testExitDoor = StoryChapterBuilderCommon.AddInteractable(world.closedDoor,
            "home.exit.final", "KAPI KOLUNU YANA ÇEK", StoryInteractionKind.Exit, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.HomeFinalTest, false, 1, 1.1f, 2.1f);

        StoryChapterBuilderCommon.SetGestureTarget(interactions.moveShoes, world.exitStored[0].transform);
        StoryChapterBuilderCommon.SetGestureTarget(interactions.moveToy, world.exitStored[1].transform);
        StoryChapterBuilderCommon.SetGestureTarget(interactions.moveParcel, world.exitStored[2].transform);
        StoryChapterBuilderCommon.SetGestureTarget(interactions.handShelfBracket, family.parent.transform);
        StoryChapterBuilderCommon.SetGestureTarget(interactions.handWardrobeStrap, family.parent.transform);
        StoryChapterBuilderCommon.SetGestureTarget(interactions.testExitDoor, world.openDoor.transform);

        UnityEventTools.AddPersistentListener(interactions.inspectWardrobe.OnInteracted, director.InspectWardrobe);
        UnityEventTools.AddPersistentListener(interactions.inspectShelf.OnInteracted, director.InspectShelf);
        UnityEventTools.AddPersistentListener(interactions.inspectExit.OnInteracted, director.InspectExit);
        UnityEventTools.AddPersistentListener(interactions.moveShoes.OnInteracted, director.MoveExitShoes);
        UnityEventTools.AddPersistentListener(interactions.moveToy.OnInteracted, director.MoveExitToy);
        UnityEventTools.AddPersistentListener(interactions.moveParcel.OnInteracted, director.MoveExitParcel);
        UnityEventTools.AddPersistentListener(interactions.lowerBooks.OnInteracted, director.LowerShelfBooks);
        UnityEventTools.AddPersistentListener(interactions.lowerVase.OnInteracted, director.LowerShelfVase);
        UnityEventTools.AddPersistentListener(interactions.lowerFrame.OnInteracted, director.LowerShelfFrame);
        UnityEventTools.AddPersistentListener(interactions.handShelfBracket.OnInteracted, director.HandShelfBracket);
        UnityEventTools.AddPersistentListener(interactions.testWardrobe.OnInteracted, director.TestWardrobe);
        UnityEventTools.AddPersistentListener(interactions.markWardrobeAnchors.OnInteracted, director.MarkWardrobeAnchors);
        UnityEventTools.AddPersistentListener(interactions.handWardrobeStrap.OnInteracted, director.HandWardrobeStrap);
        UnityEventTools.AddPersistentListener(interactions.unsafeHeavyLift.OnInteracted, director.TryUnsafeHeavyLift);
        UnityEventTools.AddPersistentListener(interactions.unsafeDrill.OnInteracted, director.TryUnsafeDrill);
        UnityEventTools.AddPersistentListener(interactions.testExitDoor.OnInteracted, director.TestExitDoor);
        return interactions;
    }

    private static void ConfigureDirector(StoryHomeSafetyDirector director, StoryGameManager manager,
        StoryPlayerMovement player, StoryTouchManager touch, StoryCameraController camera,
        StoryChapterBuilderCommon.ChapterUI ui, StoryChapterBuilderCommon.Characters family, HomeWorld world,
        HomeInteractions interactions)
    {
        SerializedObject serialized = new SerializedObject(director);
        StoryChapterBuilderCommon.Set(serialized, "gameManager", manager);
        StoryChapterBuilderCommon.Set(serialized, "player", player);
        StoryChapterBuilderCommon.Set(serialized, "touchManager", touch);
        StoryChapterBuilderCommon.Set(serialized, "cameraController", camera);
        StoryChapterBuilderCommon.Set(serialized, "ui", ui.controller);
        StoryChapterBuilderCommon.Set(serialized, "deniz", family.deniz.transform);
        StoryChapterBuilderCommon.Set(serialized, "parent", family.parent.transform);
        StoryChapterBuilderCommon.Set(serialized, "can", family.can.transform);
        StoryChapterBuilderCommon.Set(serialized, "denizAnimator", family.denizAnimator);
        StoryChapterBuilderCommon.Set(serialized, "parentAnimator", family.parentAnimator);
        StoryChapterBuilderCommon.Set(serialized, "canAnimator", family.canAnimator);
        StoryChapterBuilderCommon.Set(serialized, "inspectWardrobe", interactions.inspectWardrobe);
        StoryChapterBuilderCommon.Set(serialized, "inspectShelf", interactions.inspectShelf);
        StoryChapterBuilderCommon.Set(serialized, "inspectExit", interactions.inspectExit);
        StoryChapterBuilderCommon.Set(serialized, "moveShoes", interactions.moveShoes);
        StoryChapterBuilderCommon.Set(serialized, "moveToy", interactions.moveToy);
        StoryChapterBuilderCommon.Set(serialized, "moveParcel", interactions.moveParcel);
        StoryChapterBuilderCommon.SetArray(serialized, "exitClutterStart", world.exitStart);
        StoryChapterBuilderCommon.SetArray(serialized, "exitClutterStored", world.exitStored);
        StoryChapterBuilderCommon.SetArray(serialized, "exitMoveAnimations", world.exitAnimations);
        StoryChapterBuilderCommon.Set(serialized, "lowerBooks", interactions.lowerBooks);
        StoryChapterBuilderCommon.Set(serialized, "lowerVase", interactions.lowerVase);
        StoryChapterBuilderCommon.Set(serialized, "lowerFrame", interactions.lowerFrame);
        StoryChapterBuilderCommon.Set(serialized, "handShelfBracket", interactions.handShelfBracket);
        StoryChapterBuilderCommon.SetArray(serialized, "shelfItemsHigh", world.shelfHigh);
        StoryChapterBuilderCommon.SetArray(serialized, "shelfItemsLow", world.shelfLow);
        StoryChapterBuilderCommon.SetArray(serialized, "shelfMoveAnimations", world.shelfAnimations);
        StoryChapterBuilderCommon.Set(serialized, "shelfSecureAnimation", world.shelfSecureAnimation);
        StoryChapterBuilderCommon.Set(serialized, "shelfAnchorStrap", world.shelfAnchorStrap);
        StoryChapterBuilderCommon.Set(serialized, "shelfDust", world.shelfDust);
        StoryChapterBuilderCommon.Set(serialized, "testWardrobe", interactions.testWardrobe);
        StoryChapterBuilderCommon.Set(serialized, "markWardrobeAnchors", interactions.markWardrobeAnchors);
        StoryChapterBuilderCommon.Set(serialized, "handWardrobeStrap", interactions.handWardrobeStrap);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeRockAnimation", world.wardrobeRockAnimation);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeSecureAnimation", world.wardrobeSecureAnimation);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeAnchorStrap", world.wardrobeAnchorStrap);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeDust", world.wardrobeDust);
        StoryChapterBuilderCommon.Set(serialized, "unsafeHeavyLift", interactions.unsafeHeavyLift);
        StoryChapterBuilderCommon.Set(serialized, "unsafeDrill", interactions.unsafeDrill);
        StoryChapterBuilderCommon.Set(serialized, "heavyLiftNearMissAnimation", world.heavyAnimation);
        StoryChapterBuilderCommon.Set(serialized, "drillNearMissAnimation", world.drillAnimation);
        StoryChapterBuilderCommon.Set(serialized, "shelfParentWorkPoint", world.shelfParentWorkPoint);
        StoryChapterBuilderCommon.Set(serialized, "wardrobeParentWorkPoint", world.wardrobeParentWorkPoint);
        StoryChapterBuilderCommon.Set(serialized, "parentHeldDrill", world.parentHeldDrill);
        StoryChapterBuilderCommon.Set(serialized, "drillWorkAudio", world.drillWorkAudio);
        StoryChapterBuilderCommon.Set(serialized, "testExitDoor", interactions.testExitDoor);
        StoryChapterBuilderCommon.Set(serialized, "exitStandPoint", world.exitStandPoint);
        StoryChapterBuilderCommon.Set(serialized, "closedDoor", world.closedDoor);
        StoryChapterBuilderCommon.Set(serialized, "openDoor", world.openDoor);
        StoryChapterBuilderCommon.Set(serialized, "doorOpenAnimation", world.doorAnimation);
        StoryChapterBuilderCommon.Set(serialized, "completionPanel", ui.completionPanel);
        StoryChapterBuilderCommon.Set(serialized, "completionDetail", ui.completionDetail);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
