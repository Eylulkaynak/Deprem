using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

public static partial class StoryVerticalSliceBuilder
{
    public const string RebuildPreviewScenePath = "Assets/Scenes/Story_03_RebuildPreview.unity";
    private const string SharedHomePrefabPath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";
    private const string RebuildTimelinePath = TimelineRoot + "/Story_03_Rebuild_Earthquake.playable";
    private const string CuratedTownEnvironmentRoot = "Assets/Story/Environment/SyntyTown";
    private const double RebuildQuakeDuration = 48d;

    private sealed class RebuildProps
    {
        public GameObject sharedHome;
        public GameObject wheel;
        public GameObject toyCar;
        public GameObject toyCarFinish;
        public GameObject radioDial;
        public GameObject radioSoundWaves;
        public AudioSource radioAudio;
        public GameObject familyPlanCard;
        public GameObject familyPlanMarked;
        public GameObject shoePairRoot;
        public GameObject corridorReflector;
        public GameObject corridorRubble;
        public GameObject corridorRubbleTarget;
        public GameObject parentVoiceBarrier;
        public GameObject parentDoorKnockSurface;
        public GameObject parentResponseSignal;
        public ParticleSystem aftershockWarningDust;
        public AudioSource aftershockWarningCreak;
        public GameObject comfortToyAfterQuake;
    }

    [MenuItem("Tools/Deprem Story/Build Story_03 Rebuild Preview")]
    public static void BuildRebuildPreviewFromMenu()
    {
        BuildRebuildPreview(true);
    }

    [MenuItem("Tools/Deprem Story/Build Story_03 Rebuild Preview (Silent)")]
    public static void BuildRebuildPreviewSilentFromMenu()
    {
        BuildRebuildPreview(false);
    }

    public static void BuildRebuildPreview(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Story 03 rebuild önizlemesi Play Mode dışında üretilmelidir.");

        try
        {
            RequireRebuildAsset(SharedHomePrefabPath);
            EnsureFolders();
            StoryChapterBuilderCommon.EnsureFolders();
            StoryAnimationLibraryBuilder.BuildLibrary(false);
            CreateAudioAssets();
            Materials materials = CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_03_REBUILD_PREVIEW");
            WorldReferences world = BuildRebuildWorld(root.transform, materials, out RebuildProps props);

            RuntimeAnimatorController childController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                StoryAnimationLibraryBuilder.ControllerPath);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                false,
                new Vector3(-0.72f, 0f, -0.82f),
                new Vector3(0.42f, 0f, -0.34f),
                Vector3.zero);
            family.deniz.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            family.can.transform.rotation = Quaternion.Euler(0f, -32f, 0f);

            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);
            StorySiblingFollower follower = StoryChapterBuilderCommon.ConfigureSibling(
                family.can,
                family.deniz.transform);
            ConfigureRebuildCharacterProps(world, family, props);

            StoryCameraController cameraController = BuildRebuildCameras(
                root.transform,
                family.deniz.transform,
                out Camera mainCamera,
                out CinemachineBrain brain);
            StoryChapterBuilderCommon.ChapterUI chapterUi = StoryChapterBuilderCommon.BuildUI(
                root.transform,
                cameraController,
                "Story03RebuildCanvas",
                "3. PERDE • DEPREM",
                "KARDEŞLER KORİDORA ULAŞTI",
                "Deniz ve Can sarsıntı sırasında koşmadı; Çök–Kapan–Tutun yaptı ve sarsıntı durduktan sonra hazırlandı.",
                StoryAct.Quake);
            StoryChapterBuilderCommon.SetReference(chapterUi.controller, "movementOwner", movement);

            GameObject sessionObject = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(
                gameManager,
                StoryAct.Quake,
                new[]
                {
                    StoryFlag.BagReady,
                    StoryFlag.BagFlashlight,
                    StoryFlag.BagFirstAid,
                    StoryFlag.BagWater,
                    StoryFlag.BagComfortItem,
                    StoryFlag.WardrobeSecured,
                    StoryFlag.ShelfSecured,
                    StoryFlag.ExitCleared
                });
            StoryChapterBuilderCommon.ConfigureRebuildStoryRoute(gameManager);

            GameObject core = new GameObject("_Story03RebuildCore");
            core.transform.SetParent(root.transform);
            StorySequenceDirector sequence = core.AddComponent<StorySequenceDirector>();
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", chapterUi.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchData = new SerializedObject(touch);
            touchData.FindProperty("directWorldGestures").boolValue = true;
            touchData.ApplyModifiedPropertiesWithoutUndo();

            InteractionReferences interactions = BuildRebuildInteractions(
                root.transform,
                world,
                props,
                family,
                sequence);
            PlayableDirector quakeTimeline = BuildRebuildEarthquakeTimeline(
                root.transform,
                materials,
                world,
                out CinemachineImpulseSource impulse,
                out AudioSource impact);
            BuildLighting(root.transform, CreateVolumeProfile(), out Light roomLight);
            BuildAmbientAudio(root.transform);
            StoryChapterBuilderCommon.CreateLicensedAmbience(
                "QuakeOpeningRoomTone",
                root.transform,
                "sfx100v2_loop_ambient_03.ogg",
                0.035f);
            StoryChapterBuilderCommon.AttachInteractionAudioLayer(
                root.transform,
                "Story03_ObjectInteractionAudio");
            GameObject playerFlashlight = BuildPlayerFlashlight(family.deniz.transform);
            GameObject emergencyLights = BuildEmergencyRouteLights(root.transform, materials);

            ConfigureSequence(
                sequence,
                gameManager,
                movement,
                follower,
                touch,
                cameraController,
                chapterUi.controller,
                family.denizAnimator,
                family.canAnimator,
                quakeTimeline,
                impulse,
                impact,
                roomLight,
                interactions,
                world,
                playerFlashlight,
                emergencyLights,
                props.comfortToyAfterQuake);
            ConfigureRebuildSequence(sequence);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(chapterUi.controller, "cameraController", cameraController);
            StoryChapterBuilderCommon.DisableShadows(root.transform);

            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            StoryChapterBuilderCommon.BuildNavigation(world.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            StoryQuakeRebuildPreviewValidator.Validate(false);
            Selection.activeGameObject = root;
            Debug.Log("Story_03_RebuildPreview üretildi: " + RebuildPreviewScenePath);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Ortak aile evinde revize Story 03 önizlemesi üretildi.\n" +
                    "Mevcut Story_03_Quake sahnesi ve Build Settings değiştirilmedi.",
                    "Tamam");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 03 rebuild önizlemesi üretilemedi:\n" + exception.Message,
                    "Kapat");
            }
            throw;
        }
    }

    private static WorldReferences BuildRebuildWorld(
        Transform parent,
        Materials materials,
        out RebuildProps props)
    {
        WorldReferences world = new WorldReferences();
        props = new RebuildProps();
        world.environment = new GameObject("QuakeEnvironment_Rebuild");
        world.environment.transform.SetParent(parent);

        GameObject sharedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHomePrefabPath);
        props.sharedHome = (GameObject)PrefabUtility.InstantiatePrefab(
            sharedPrefab,
            world.environment.transform);
        props.sharedHome.name = "StoryHome_Shared";
        props.sharedHome.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        props.sharedHome.transform.localScale = Vector3.one;

        Transform home = props.sharedHome.transform;
        world.wardrobeSecured = FindRebuildRequired(home, "Wardrobe_Secured").gameObject;
        world.wardrobeUnsecured = FindRebuildRequired(home, "Wardrobe_Unsecured").gameObject;
        world.wardrobeFallen = FindRebuildRequired(home, "Wardrobe_Fallen").gameObject;
        world.safeTable = FindRebuildRequired(home, "SafeTable").gameObject;
        world.shelfStable = FindRebuildRequired(home, "Shelf_Secured").gameObject;
        world.shelfUnsecured = FindRebuildRequired(home, "Shelf_Unsecured").gameObject;
        world.shelfFallen = FindRebuildRequired(home, "Shelf_Fallen").gameObject;
        world.hangingLamp = FindRebuildRequired(home, "HangingLamp_QuakeMotion").gameObject;
        world.looseProps = FindRebuildRequired(home, "LooseProps_QuakeMotion").gameObject;
        world.clearExit = FindRebuildRequired(home, "ExitRoute_Cleared").gameObject;
        world.clutteredExit = FindRebuildRequired(home, "ExitRoute_ClutteredButPassable").gameObject;
        world.closedDoor = FindRebuildRequired(home, "Door").gameObject;
        world.openDoor = FindRebuildRequired(home, "Door_Open").gameObject;
        StoryChapterBuilderCommon.RestyleEmergencyBag(
            FindRebuildRequired(home, "EmergencyBag").gameObject,
            StoryChapterBuilderCommon.CreateMaterials(),
            new Vector3(0.64f, 0.7f, 0.4f),
            false);
        world.emergencyBagWorld = FindRebuildRequired(home, "EmergencyBag").gameObject;
        world.brokenGlassVisual = FindRebuildRequired(home, "BrokenGlass_Hazard").gameObject;
        world.canShoesWorld = FindRebuildRequired(home, "CanShoes_World").gameObject;
        world.tableFocus = FindRebuildRequired(home, "TableFocus");
        world.windowFocus = FindRebuildRequired(home, "WindowFocus");
        world.wardrobeFocus = FindRebuildRequired(home, "WardrobeFocus");
        world.exitInspectFocus = FindRebuildRequired(home, "ExitInspectFocus");
        world.glassFocus = FindRebuildRequired(home, "GlassHazardFocus");
        world.leftShoeFocus = FindRebuildRequired(home, "LeftShoeFocus");
        world.rightShoeFocus = FindRebuildRequired(home, "RightShoeFocus");
        world.shoesFocus = FindRebuildRequired(home, "ShoesFocus");
        world.canShoesFocus = FindRebuildRequired(home, "CanShoesFocus");
        world.bagFocus = FindRebuildRequired(home, "BagFocus");
        world.bagStrapFocus = FindRebuildRequired(home, "BagStrapFocus");
        world.flashlightFocus = FindRebuildRequired(home, "FlashlightFocus");
        world.emergencyLightFocus = FindRebuildRequired(home, "EmergencyLightPreviewFocus");
        world.corridorFocus = FindRebuildRequired(home, "CorridorFocus");
        world.corridorStepFoci = new[]
        {
            FindRebuildRequired(home, "CorridorThresholdFocus"),
            FindRebuildRequired(home, "CorridorVoiceFocus"),
            FindRebuildRequired(home, "CorridorAftershockFocus")
        };

        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(world.closedDoor);
        ConfigureRebuildHomeState(home);
        NormalizeRebuildHomeLayout(home);
        BuildRebuildStory02Payoff(world, materials);
        BuildRebuildOpeningSet(world, props, materials);
        BuildRebuildShoePair(world, props, materials);
        BuildRebuildCorridorSet(world, props, materials);
        BuildRebuildDioramaFoundation(world.environment.transform, materials);
        return world;
    }

    private static void BuildRebuildDioramaFoundation(Transform environment, Materials materials)
    {
        Transform foundation = StoryChapterBuilderCommon.NewChild(environment, "Story03_DioramaFoundation");
        GameObject upperSlab = StoryChapterBuilderCommon.CreatePrimitive(
            "Story03_DioramaUpperSlab",
            PrimitiveType.Cube,
            new Vector3(0f, -0.31f, 3.4f),
            new Vector3(17.5f, 0.28f, 25f),
            materials.corridor,
            foundation,
            false);
        GameObject lowerSlab = StoryChapterBuilderCommon.CreatePrimitive(
            "Story03_DioramaLowerSlab",
            PrimitiveType.Cube,
            new Vector3(0f, -0.5f, 3.4f),
            new Vector3(18.2f, 0.16f, 25.7f),
            materials.navy,
            foundation,
            false);
        foreach (Renderer renderer in upperSlab.GetComponentsInChildren<Renderer>(true)
                     .Concat(lowerSlab.GetComponentsInChildren<Renderer>(true)))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void ConfigureRebuildHomeState(Transform home)
    {
        SetRebuildActive(home, "Wardrobe_Secured", true);
        SetRebuildActive(home, "Wardrobe_Unsecured", false);
        SetRebuildActive(home, "Wardrobe_Fallen", false);
        SetRebuildActive(home, "Shelf_Secured", true);
        SetRebuildActive(home, "Shelf_Unsecured", false);
        SetRebuildActive(home, "Shelf_Fallen", false);
        SetRebuildActive(home, "ExitRoute_Cleared", true);
        SetRebuildActive(home, "ExitRoute_ClutteredButPassable", false);
        SetRebuildActive(home, "Door", true);
        SetRebuildActive(home, "Door_Open", false);
        SetRebuildActive(home, "BrokenGlass_Hazard", false);
        SetRebuildActive(home, "EmergencyBag", true);
        SetRebuildActive(home, "Shoes_PostQuake", true);
    }

    private static void NormalizeRebuildHomeLayout(Transform home)
    {
        PlaceRebuildHomeObject(home, "Wardrobe_Secured",
            new Vector3(-4.55f, 0f, 2.7f), new Vector3(0f, -90f, 0f));
        PlaceRebuildHomeObject(home, "Wardrobe_Unsecured",
            new Vector3(-4.55f, 0f, 2.7f), new Vector3(0f, -90f, 0f));
        PlaceRebuildHomeObject(home, "Wardrobe_Fallen",
            new Vector3(-4.02f, 0.38f, 2.7f), new Vector3(0f, -90f, 78f));
        PlaceRebuildHomeObject(home, "WardrobeFocus",
            new Vector3(-3.72f, 0.1f, 2.7f), Vector3.zero);
    }

    private static void PlaceRebuildHomeObject(
        Transform home,
        string objectName,
        Vector3 localPosition,
        Vector3 localEuler)
    {
        Transform target = FindRebuildRequired(home, objectName);
        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEuler);
    }

    private static void BuildRebuildStory02Payoff(
        WorldReferences world,
        Materials materials)
    {
        BuildRebuildReadingNest(
            "CanReadingNest_Safe_Story03",
            world.clearExit.transform,
            new Vector3(0.95f, 0.045f, -0.72f),
            new Vector3(0f, 14f, 0f),
            materials.teal,
            materials.amber,
            materials.wood,
            materials.navy,
            false);
        BuildRebuildReadingNest(
            "CanReadingNest_RiskImpact_Story03",
            world.clutteredExit.transform,
            new Vector3(3.08f, 0.045f, 1.55f),
            new Vector3(0f, -18f, -9f),
            materials.amber,
            materials.coral,
            materials.wood,
            materials.navy,
            true);
    }

    private static GameObject BuildRebuildReadingNest(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 euler,
        Material rugMaterial,
        Material cushionMaterial,
        Material woodMaterial,
        Material darkMaterial,
        bool impacted)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));

        StoryChapterBuilderCommon.CreatePrimitive(
            "ReadingMat",
            PrimitiveType.Cylinder,
            position,
            impacted
                ? new Vector3(0.7f, 0.025f, 0.56f)
                : new Vector3(0.82f, 0.035f, 0.68f),
            rugMaterial,
            root.transform,
            false,
            impacted ? Quaternion.Euler(0f, -18f, 7f) : Quaternion.identity);
        StoryChapterBuilderCommon.CreatePrimitive(
            "ReadingCushion",
            PrimitiveType.Sphere,
            position + new Vector3(-0.12f, impacted ? 0.075f : 0.12f, 0.03f),
            impacted
                ? new Vector3(0.5f, 0.11f, 0.34f)
                : new Vector3(0.48f, 0.18f, 0.42f),
            cushionMaterial,
            root.transform,
            false,
            Quaternion.Euler(impacted ? 14f : 0f, euler.y - 8f, impacted ? 18f : -4f));
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_08.prefab",
            impacted ? "CanComicBook_Damaged" : "CanComicBook_Safe",
            root.transform,
            position + new Vector3(0.24f, impacted ? 0.055f : 0.095f, -0.08f),
            new Vector3(0.34f, 0.08f, 0.46f),
            new Vector3(impacted ? 19f : 0f, euler.y + 18f, impacted ? -24f : 0f),
            false);

        if (impacted)
        {
            for (int index = 0; index < 4; index++)
            {
                StoryChapterBuilderCommon.CreatePrimitive(
                    "ShelfImpactDebris_" + (index + 1),
                    PrimitiveType.Cube,
                    position + new Vector3(-0.32f + index * 0.21f, 0.075f + index * 0.012f, 0.2f - index * 0.09f),
                    new Vector3(0.18f + index * 0.025f, 0.08f, 0.13f),
                    index % 2 == 0 ? woodMaterial : darkMaterial,
                    root.transform,
                    false,
                    Quaternion.Euler(index * 11f, index * 27f, index % 2 == 0 ? 12f : -16f));
            }
        }
        return root;
    }

    private static void BuildRebuildOpeningSet(
        WorldReferences world,
        RebuildProps props,
        Materials materials)
    {
        Transform dressing = StoryChapterBuilderCommon.NewChild(
            world.environment.transform,
            "QuakeRebuildOpeningProps");
        props.toyCar = FindRebuildRequired(props.sharedHome.transform, "Can_ToyCar").gameObject;
        Bounds carBounds = GetRebuildBounds(props.toyCar);

        props.wheel = StoryAuthoredPropFactory.CreateToyWheel(
            "Can_ToyWheel_Drag",
            dressing,
            world.tableFocus.position + new Vector3(0.92f, 0.02f, -0.72f),
            new Vector3(0.24f, 0.22f, 0.14f),
            new Vector3(90f, 0f, 0f),
            materials.navy,
            materials.cream,
            materials.amber,
            true);
        props.toyCarFinish = StoryChapterBuilderCommon.CreatePrimitive(
            "Can_ToyCar_FinishMarker",
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 0.035f, -0.12f),
            new Vector3(0.62f, 0.018f, 0.62f),
            materials.safeGlow,
            dressing,
            false);
        props.toyCarFinish.GetComponent<Renderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject radioBody = FindRebuildRequired(props.sharedHome.transform, "RadioBody").gameObject;
        Bounds radioBounds = GetRebuildBounds(radioBody);
        props.radioDial = StoryChapterBuilderCommon.CreatePrimitive(
            "RadioVolumeDial_Direct",
            PrimitiveType.Cylinder,
            radioBounds.center + new Vector3(radioBounds.extents.x + 0.025f, 0f, -radioBounds.extents.z * 0.62f),
            new Vector3(0.11f, 0.045f, 0.11f),
            materials.amber,
            dressing,
            true,
            Quaternion.Euler(0f, 0f, 90f));
        props.radioSoundWaves = StoryChapterBuilderCommon.CreatePrimitive(
            "RadioSoundWaves",
            PrimitiveType.Cube,
            radioBounds.center + new Vector3(0f, radioBounds.extents.y + 0.15f, 0f),
            new Vector3(Mathf.Max(0.3f, radioBounds.size.x * 0.7f), 0.035f, 0.035f),
            materials.glow,
            dressing,
            false);
        props.radioAudio = props.radioDial.AddComponent<AudioSource>();
        props.radioAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/calm_home.wav");
        props.radioAudio.loop = true;
        props.radioAudio.playOnAwake = true;
        props.radioAudio.volume = 0.08f;
        props.radioAudio.spatialBlend = 0.4f;

        props.familyPlanCard = StoryChapterBuilderCommon.CreatePrimitive(
            "Can_FamilyPlanDrawing",
            PrimitiveType.Cube,
            new Vector3(1.05f, 0.74f, -0.28f),
            new Vector3(0.72f, 0.04f, 0.52f),
            materials.cream,
            dressing,
            true,
            Quaternion.Euler(0f, -14f, 0f));
        props.familyPlanMarked = StoryChapterBuilderCommon.CreatePrimitive(
            "FamilyPlan_AssemblyMark",
            PrimitiveType.Cylinder,
            props.familyPlanCard.transform.position + new Vector3(0.18f, 0.055f, 0.08f),
            new Vector3(0.13f, 0.015f, 0.13f),
            materials.amber,
            dressing,
            false);
        props.familyPlanMarked.SetActive(false);

        if (carBounds.size.sqrMagnitude <= 0.0001f)
            throw new InvalidOperationException("Can'ın oyuncak arabası görsel sınır üretmedi.");
    }

    private static void BuildRebuildShoePair(WorldReferences world, RebuildProps props, Materials materials)
    {
        GameObject originalLeft = FindRebuildRequired(props.sharedHome.transform, "LeftShoe").gameObject;
        GameObject originalRight = FindRebuildRequired(props.sharedHome.transform, "RightShoe").gameObject;
        Bounds leftBounds = GetRebuildBounds(originalLeft);
        Bounds rightBounds = GetRebuildBounds(originalRight);
        Vector3 feetPosition = new Vector3(
            (leftBounds.center.x + rightBounds.center.x) * 0.5f,
            Mathf.Min(leftBounds.min.y, rightBounds.min.y),
            (leftBounds.center.z + rightBounds.center.z) * 0.5f);
        props.shoePairRoot = StoryAuthoredPropFactory.CreateShoePair(
            "DenizShoePair_Drag",
            world.environment.transform,
            feetPosition,
            new Vector3(0.88f, 0.28f, 0.74f),
            new Vector3(0f, -8f, 0f),
            materials.coral,
            materials.navy,
            true);
        world.leftShoeWorld = props.shoePairRoot.transform.Find("LeftShoe").gameObject;
        world.leftShoeWorld.name = "DenizShoePair_Left";
        world.rightShoeWorld = props.shoePairRoot.transform.Find("RightShoe").gameObject;
        world.rightShoeWorld.name = "DenizShoePair_Right";
        originalLeft.SetActive(false);
        originalRight.SetActive(false);
    }

    private static void BuildRebuildCorridorSet(
        WorldReferences world,
        RebuildProps props,
        Materials materials)
    {
        Transform dressing = StoryChapterBuilderCommon.NewChild(
            world.environment.transform,
            "QuakeRebuildCorridorProps");

        // Ortak ev prefabındaki koridor kasıtlı olarak yalın bir navigasyon kabuğudur.
        // Bu perde, oyuncunun gördüğü yüzeyleri Synty parçaları ve sahne-yerleşimli ışıklarla giydirir.
        Transform corridor = FindRebuildRequired(props.sharedHome.transform, "Corridor");
        SetRebuildRendererMaterial(FindRebuildRequired(corridor, "CorridorFloor"), materials.corridor);
        SetRebuildRendererMaterial(FindRebuildRequired(corridor, "CorridorLeftWall"), materials.wall);
        SetRebuildRendererMaterial(FindRebuildRequired(corridor, "CorridorRightWall"), materials.wall);
        SetRebuildRendererMaterial(FindRebuildRequired(corridor, "CorridorEnd"), materials.wall);
        for (int i = 0; i < 4; i++)
            SetRebuildRendererMaterial(
                FindRebuildRequired(corridor, "CeilingBeam_" + i),
                i % 2 == 0 ? materials.wood : materials.corridor);
        foreach (Renderer routeMarker in world.clearExit.GetComponentsInChildren<Renderer>(true))
            routeMarker.enabled = false;

        StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorCeiling",
            PrimitiveType.Cube,
            new Vector3(2.5f, 3.21f, 9.4f),
            new Vector3(2.92f, 0.12f, 6.72f),
            materials.wall,
            dressing,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorLowerPanel_Left",
            PrimitiveType.Cube,
            new Vector3(1.085f, 0.67f, 9.4f),
            new Vector3(0.045f, 1.18f, 6.58f),
            materials.cream,
            dressing,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorLowerPanel_Right",
            PrimitiveType.Cube,
            new Vector3(3.915f, 0.67f, 9.4f),
            new Vector3(0.045f, 1.18f, 6.58f),
            materials.cream,
            dressing,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorSkirting_Left",
            PrimitiveType.Cube,
            new Vector3(1.12f, 0.11f, 9.4f),
            new Vector3(0.07f, 0.22f, 6.55f),
            materials.wood,
            dressing,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorSkirting_Right",
            PrimitiveType.Cube,
            new Vector3(3.88f, 0.11f, 9.4f),
            new Vector3(0.07f, 0.22f, 6.55f),
            materials.wood,
            dressing,
            false);

        props.corridorReflector = StoryChapterBuilderCommon.CreatePrimitive(
            "CorridorReflectiveRoute",
            PrimitiveType.Cube,
            new Vector3(2.5f, 0.014f, 8.48f),
            new Vector3(1.08f, 0.028f, 4.55f),
            materials.navy,
            dressing,
            true);
        foreach (float x in new[] { 2.03f, 2.97f })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "CorridorRunner_Edge",
                PrimitiveType.Cube,
                new Vector3(x, 0.031f, 8.48f),
                new Vector3(0.035f, 0.008f, 4.38f),
                materials.cream,
                dressing,
                false);
        }
        props.corridorRubble = InstantiateRebuildTownAsset(
            "SM_Prop_CardboardBox_01.fbx",
            "CorridorLightRubble_Drag",
            dressing,
            new Vector3(2.88f, 0.015f, 8.35f),
            new Vector3(0.5f, 0.32f, 0.4f),
            new Vector3(0f, 22f, 4f),
            true);
        props.corridorRubbleTarget = InstantiateRebuildTownAsset(
            "SM_Prop_FloorMat_01.fbx",
            "CorridorRubble_SafeSide",
            dressing,
            new Vector3(3.48f, 0.012f, 8.12f),
            new Vector3(0.72f, 0.035f, 0.78f),
            Vector3.zero,
            false);

        BuildRebuildApartmentDoor(
            dressing,
            "NeighbourDoor_Left_3A",
            new Vector3(1.07f, 0.01f, 7.62f),
            new Vector3(0.17f, 2.55f, 1.2f),
            new Vector3(0f, 90f, 0f));
        BuildRebuildApartmentDoor(
            dressing,
            "NeighbourDoor_Right_3B",
            new Vector3(3.93f, 0.01f, 9.15f),
            new Vector3(0.17f, 2.55f, 1.2f),
            new Vector3(0f, -90f, 0f));
        BuildRebuildApartmentDoor(
            dressing,
            "StairwellDoor_Story04",
            new Vector3(2.5f, 0.01f, 12.73f),
            new Vector3(1.28f, 2.62f, 0.17f),
            Vector3.zero);

        InstantiateRebuildTownAsset(
            "SM_Prop_CeilingExit_01.fbx",
            "StairwellExitSign",
            dressing,
            new Vector3(2.5f, 2.67f, 12.59f),
            new Vector3(0.82f, 0.3f, 0.14f),
            Vector3.zero,
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_LetterBox_01.fbx",
            "NeighbourLetterBox_Left",
            dressing,
            new Vector3(1.19f, 1.08f, 7.62f),
            new Vector3(0.13f, 0.35f, 0.46f),
            new Vector3(0f, 90f, 0f),
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_LetterBox_01.fbx",
            "NeighbourLetterBox_Right",
            dressing,
            new Vector3(3.81f, 1.08f, 9.15f),
            new Vector3(0.13f, 0.35f, 0.46f),
            new Vector3(0f, -90f, 0f),
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_FloorMat_01.fbx",
            "NeighbourMat_Left",
            dressing,
            new Vector3(1.48f, 0.012f, 7.62f),
            new Vector3(0.72f, 0.03f, 0.54f),
            new Vector3(0f, 90f, 0f),
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_FloorMat_01.fbx",
            "NeighbourMat_Right",
            dressing,
            new Vector3(3.52f, 0.012f, 9.15f),
            new Vector3(0.72f, 0.03f, 0.54f),
            new Vector3(0f, -90f, 0f),
            false);

        InstantiateRebuildTownAsset(
            "SM_Prop_Clock_01.fbx",
            "CorridorClock_AftershockCue",
            dressing,
            new Vector3(3.84f, 1.72f, 10.62f),
            new Vector3(0.12f, 0.52f, 0.52f),
            new Vector3(0f, -90f, 0f),
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_Lightswitch_01.fbx",
            "CorridorLightSwitch",
            dressing,
            new Vector3(3.84f, 1.08f, 11.52f),
            new Vector3(0.08f, 0.22f, 0.16f),
            new Vector3(0f, -90f, 0f),
            false);
        InstantiateRebuildTownAsset(
            "SM_Prop_PotPlant_01.fbx",
            "CorridorPlant_KnockedButClear",
            dressing,
            new Vector3(3.52f, 0.015f, 10.52f),
            new Vector3(0.58f, 0.72f, 0.58f),
            new Vector3(0f, -18f, 13f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_17.prefab",
            "CorridorFamilyPicture_Left",
            dressing,
            new Vector3(1.16f, 1.45f, 8.7f),
            new Vector3(0.1f, 0.64f, 0.52f),
            new Vector3(0f, 90f, -7f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_21.prefab",
            "CorridorFamilyPicture_Right",
            dressing,
            new Vector3(3.84f, 1.55f, 7.45f),
            new Vector3(0.1f, 0.58f, 0.48f),
            new Vector3(0f, -90f, 4f),
            false);

        BuildRebuildApartmentDoor(
            dressing,
            "ParentSideDoor_Damaged",
            new Vector3(1.07f, 0.01f, 10.18f),
            new Vector3(0.17f, 2.55f, 1.2f),
            new Vector3(0f, 90f, -3f));
        props.parentVoiceBarrier = InstantiateRebuildTownAsset(
            "SM_Prop_Bookshelf_01.fbx",
            "ParentVoiceBarrier",
            dressing,
            new Vector3(1.18f, 0.015f, 10.18f),
            new Vector3(0.62f, 2.2f, 1.38f),
            new Vector3(0f, 88f, -8f),
            true);
        props.parentDoorKnockSurface = StoryChapterBuilderCommon.CreatePrimitive(
            "ParentDoorKnockSurface",
            PrimitiveType.Cube,
            new Vector3(1.7f, 1.15f, 10.18f),
            new Vector3(0.08f, 1.18f, 0.48f),
            materials.wood,
            dressing,
            true,
            Quaternion.Euler(0f, 0f, -3f));
        props.parentResponseSignal = BuildParentResponseSignal(
            dressing,
            materials,
            new Vector3(1.76f, 1.22f, 10.18f));
        InstantiateRebuildTownAsset(
            "SM_Prop_CardboardBox_01.fbx",
            "ParentVoiceBarrier_Debris",
            dressing,
            new Vector3(1.55f, 0.015f, 10.65f),
            new Vector3(0.48f, 0.34f, 0.42f),
            new Vector3(8f, 34f, 11f),
            false);
        for (int i = 0; i < 3; i++)
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "ParentVoiceBarrier_LooseBook_" + i,
                PrimitiveType.Cube,
                new Vector3(1.56f + i * 0.19f, 0.08f + i * 0.025f, 9.95f + i * 0.16f),
                new Vector3(0.34f, 0.055f, 0.22f),
                i % 2 == 0 ? materials.coral : materials.amber,
                dressing,
                false,
                Quaternion.Euler(4f, 18f + i * 27f, 6f));
        }

        for (int i = 0; i < 2; i++)
        {
            float z = i == 0 ? 7.25f : 10.55f;
            InstantiateRebuildTownAsset(
                "SM_Prop_CeilingLight_01.fbx",
                "CorridorCeilingFixture_" + i,
                dressing,
                new Vector3(2.5f, 3.02f, z),
                new Vector3(0.76f, 0.16f, 0.38f),
                Vector3.zero,
                false);
            BuildRebuildCorridorLight(
                dressing,
                "CorridorLightPool_" + i,
                new Vector3(2.5f, 2.76f, z),
                i == 0 ? new Color32(205, 229, 226, 255) : new Color32(238, 192, 125, 255),
                i == 0 ? 1.25f : 0.85f);
        }

        Vector3 aftershockWarningPosition = new Vector3(2.72f, 2.48f, 11.62f);
        props.aftershockWarningDust = CreateRebuildDust(
            "CanAftershockWarning_CeilingDust",
            dressing,
            aftershockWarningPosition,
            new Vector3(1.45f, 0.22f, 1.05f),
            materials.dust,
            0f,
            false,
            22f);
        ParticleSystem.MainModule warningDustMain = props.aftershockWarningDust.main;
        warningDustMain.playOnAwake = false;
        props.aftershockWarningDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        props.aftershockWarningCreak = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            "CanAftershockWarning_CeilingCreak",
            dressing,
            aftershockWarningPosition,
            StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_wood_04.ogg"),
            0.34f,
            0.88f,
            1.2f,
            10f);
    }

    private static GameObject InstantiateRebuildTownAsset(
        string fileName,
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 size,
        Vector3 euler,
        bool ensureCollider)
    {
        Material townAtlas = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PolygonTown/Materials/PolygonTown_01_A.mat");
        if (townAtlas == null)
            throw new InvalidOperationException("Synty Town atlas malzemesi bulunamadı.");

        return StoryChapterBuilderCommon.InstantiateAsset(
            CuratedTownEnvironmentRoot + "/" + fileName,
            name,
            parent,
            feetPosition,
            size,
            euler,
            false,
            ensureCollider,
            townAtlas);
    }

    private static void BuildRebuildApartmentDoor(
        Transform parent,
        string name,
        Vector3 feetPosition,
        Vector3 size,
        Vector3 euler)
    {
        InstantiateRebuildTownAsset(
            "SM_Bld_House_Door_01.fbx",
            name,
            parent,
            feetPosition,
            size,
            euler,
            false);
    }

    private static GameObject BuildParentResponseSignal(
        Transform parent,
        Materials materials,
        Vector3 position)
    {
        GameObject root = new GameObject("ParentResponseWarmSignal");
        root.transform.SetParent(parent);
        root.transform.position = position;

        Light light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.28f);
        light.intensity = 2.2f;
        light.range = 3.6f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;

        for (int i = 0; i < 3; i++)
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "ParentResponseLightSlit_" + i,
                PrimitiveType.Cube,
                position + new Vector3(0.035f + i * 0.018f, 0.28f - i * 0.27f, 0f),
                new Vector3(0.035f, 0.16f + i * 0.035f, 0.26f + i * 0.08f),
                materials.amber,
                root.transform,
                false,
                Quaternion.Euler(0f, 0f, -3f));
        }

        root.SetActive(false);
        return root;
    }

    private static void BuildRebuildCorridorLight(
        Transform parent,
        string name,
        Vector3 position,
        Color color,
        float intensity)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = 4.4f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    private static void SetRebuildRendererMaterial(Transform root, Material material)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
    }

    private static void ConfigureRebuildCharacterProps(
        WorldReferences world,
        StoryChapterBuilderCommon.Characters family,
        RebuildProps props)
    {
        world.denizWornShoes = FindRebuildRequired(family.deniz.transform, "Deniz_12_WornShoes").gameObject;
        world.denizWornBag = FindRebuildRequired(family.deniz.transform, "Deniz_WornEmergencyBag").gameObject;
        world.canWornShoes = FindRebuildRequired(family.can.transform, "Can_8_WornShoes").gameObject;
        world.denizWornShoes.SetActive(false);
        world.denizWornBag.SetActive(false);
        world.canWornShoes.SetActive(false);

        Transform canRightHand = StoryChapterBuilderCommon.FindHumanoidBone(
            family.can,
            HumanBodyBones.RightHand);
        if (canRightHand == null)
            throw new InvalidOperationException("Can sağ el kemiği bulunamadı.");
        props.comfortToyAfterQuake = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "Can_ComfortToy_PostQuake",
            canRightHand,
            canRightHand.position,
            new Vector3(0.24f, 0.14f, 0.34f),
            Vector3.zero,
            false);
        props.comfortToyAfterQuake.transform.localPosition = new Vector3(0.08f, -0.02f, 0.03f);
        props.comfortToyAfterQuake.transform.localRotation = Quaternion.Euler(20f, 0f, 90f);
        props.comfortToyAfterQuake.SetActive(false);
    }

    private static StoryCameraController BuildRebuildCameras(
        Transform parent,
        Transform player,
        out Camera mainCamera,
        out CinemachineBrain brain)
    {
        GameObject cameraRoot = new GameObject("Story03RebuildCameras");
        cameraRoot.transform.SetParent(parent);

        GameObject main = new GameObject("Main Camera");
        main.tag = "MainCamera";
        main.transform.SetParent(cameraRoot.transform);
        main.transform.position = new Vector3(8.8f, 9.4f, -11.8f);
        main.transform.rotation = LookAt(main.transform.position, new Vector3(0f, 0.95f, 1.1f));
        mainCamera = main.AddComponent<Camera>();
        mainCamera.backgroundColor = new Color32(46, 60, 68, 255);
        StoryChapterBuilderCommon.ConfigureStorySkybox(mainCamera);
        mainCamera.nearClipPlane = 0.08f;
        mainCamera.farClipPlane = 140f;
        mainCamera.allowHDR = true;
        main.AddComponent<AudioListener>();
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData =
            main.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        brain = main.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            0.72f);

        StoryCameraBinding[] bindings =
        {
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.RoomOverview, "CM03R_RoomOverview",
                new Vector3(8.8f, 9.4f, -11.8f), new Vector3(0f, 0.95f, 1.1f), 48f,
                player, 17.2f, new Vector2(-0.2f, 0.16f)),
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.QuakeClose, "CM03R_QuakeClose",
                new Vector3(4.1f, 4.35f, -5.5f), new Vector3(-0.15f, 0.92f, -0.1f), 44f,
                player, 6.8f, new Vector2(-0.08f, 0.13f)),
            // Kritik koruma planı masanın içine veya yalnızca ayaklara girmez:
            // tabla, iki çocuk ve tutulan ön ayak aynı portre kadrajındadır.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.UnderTable, "CM03R_UnderTableTwoShot",
                new Vector3(2.5f, 2.35f, -4.4f), new Vector3(-0.4f, 0.75f, 0.1f), 48f),
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.PostQuake, "CM03R_PostQuake",
                new Vector3(4.65f, 4.85f, -6.2f), new Vector3(0.1f, 0.85f, 0.7f), 45f,
                player, 8.6f, new Vector2(-0.13f, 0.15f)),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.Corridor, "CM03R_CorridorLong",
                new Vector3(3.62f, 2.48f, 4.72f), new Vector3(2.32f, 0.72f, 9.78f), 44f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectTable, "CM03R_TableFamilyMoment",
                new Vector3(4.8f, 3.4f, -5.8f), new Vector3(0.15f, 0.72f, 0.1f), 48f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWindow, "CM03R_WindowRisk",
                new Vector3(3.15f, 3.05f, -0.85f), new Vector3(-2.2f, 1.65f, 5.55f), 44f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWardrobe, "CM03R_PreparationConsequence",
                new Vector3(0.15f, 3.05f, -0.4f), new Vector3(-4.2f, 1.35f, 2.7f), 44f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectExit, "CM03R_ExitAndCorridor",
                new Vector3(0.18f, 2.28f, 0.52f), new Vector3(2.5f, 0.82f, 6.9f), 45f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectBrokenGlass, "CM03R_GlassSafeRoute",
                new Vector3(0.55f, 3.85f, 1.15f), new Vector3(-2.91f, 0.16f, 4.83f), 40f)
        };

        GameObject controllerObject = new GameObject("MissionCameraController_Story03Rebuild");
        controllerObject.transform.SetParent(cameraRoot.transform);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        serialized.FindProperty("initialZone").intValue = (int)StoryCameraZoneId.RoomOverview;
        SerializedProperty cameras = serialized.FindProperty("cameras");
        cameras.arraySize = bindings.Length;
        for (int i = 0; i < bindings.Length; i++)
        {
            SerializedProperty binding = cameras.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("zone").intValue = (int)bindings[i].zone;
            binding.FindPropertyRelative("camera").objectReferenceValue = bindings[i].camera;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static InteractionReferences BuildRebuildInteractions(
        Transform parent,
        WorldReferences world,
        RebuildProps props,
        StoryChapterBuilderCommon.Characters family,
        StorySequenceDirector sequence)
    {
        Transform interactionRoot = StoryChapterBuilderCommon.NewChild(parent, "Story03RebuildInteractions");
        InteractionReferences refs = new InteractionReferences();

        StoryInteractable wheel = AddRebuildDrag(
            props.wheel,
            props.toyCar,
            "quake.intro.wheel",
            "TEKERİ OYUNCAK ARABAYA TAK",
            StoryInteractionKind.Collect,
            StoryCameraZoneId.InspectTable,
            interactionRoot,
            new Vector3(0.85f, 0.65f, 0.85f));
        StoryInteractable rollCar = AddRebuildDrag(
            props.toyCar,
            props.toyCarFinish,
            "quake.intro.car",
            "ARABAYI MASANIN ALTINDAN CAN'A SÜR",
            StoryInteractionKind.HelpSibling,
            StoryCameraZoneId.InspectTable,
            interactionRoot,
            new Vector3(1.15f, 0.45f, 1.15f));
        StoryInteractable radio = AddRebuildInteraction(
            props.radioDial,
            "quake.intro.radio",
            "RADYO DÜĞMESİNİ SOLA ÇEVİR",
            StoryInteractionKind.Inspect,
            props.radioDial.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.RoomOverview,
            false,
            1,
            1.2f,
            2.2f);
        StoryInteractable planDrawing = AddRebuildInteraction(
            props.familyPlanCard,
            "quake.intro.familyplan",
            "ÇİZİMDEKİ BULUŞMA ALANINI İŞARETLE",
            StoryInteractionKind.HelpSibling,
            props.familyPlanCard.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.InspectTable,
            false,
            1,
            1.8f,
            2.2f);
        UnityEventTools.AddPersistentListener(radio.OnInteracted, props.radioAudio.Pause);
        UnityEventTools.AddBoolPersistentListener(
            radio.OnInteracted,
            props.radioSoundWaves.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            planDrawing.OnInteracted,
            props.familyPlanMarked.SetActive,
            true);
        UnityEventTools.AddPersistentListener(radio.OnInteracted, sequence.OnOptionalIntroRadioMoment);
        UnityEventTools.AddPersistentListener(planDrawing.OnInteracted, sequence.OnOptionalIntroPlanMoment);
        refs.intro = new[] { wheel, rollCar };
        refs.introOptional = new[] { radio, planDrawing };
        foreach (StoryInteractable interaction in refs.intro)
            UnityEventTools.AddPersistentListener(interaction.OnInteracted, sequence.OnIntroInspection);

        GameObject calmSurface = CreateRebuildSurface(
            "Can_CalmTouchSurface",
            family.can.transform,
            new Vector3(0f, 0.7f, 0f),
            new Vector3(0.78f, 1.35f, 0.72f));
        refs.calmSibling = AddRebuildInteraction(
            calmSurface,
            "quake.calm.can",
            "CAN'A DOKUN — BENİMLE KAL",
            StoryInteractionKind.HelpSibling,
            calmSurface.transform,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.QuakeClose,
            true,
            1,
            1.1f,
            2.5f);
        GameObject crouchSurface = CreateRebuildSurface(
            "SafeTable_CrouchSurface",
            interactionRoot,
            world.tableFocus.position + new Vector3(0.25f, 0.12f, -0.12f),
            new Vector3(2.6f, 0.42f, 1.75f),
            true);
        refs.crouchStep = AddRebuildInteraction(
            crouchSurface,
            "quake.cover.crouch",
            "MASANIN GÜVENLİ TARAFINA GEÇ",
            StoryInteractionKind.TakeCover,
            world.tableFocus,
            StoryInteractionGesture.Approach,
            StoryCameraZoneId.UnderTable,
            false,
            1,
            2.2f,
            1.65f);
        GameObject coverSurface = CreateRebuildSurface(
            "Deniz_ProtectHeadSurface",
            family.deniz.transform,
            new Vector3(0f, 0.92f, 0.08f),
            new Vector3(0.72f, 0.68f, 0.62f));
        refs.coverHeadStep = AddRebuildInteraction(
            coverSurface,
            "quake.cover.head",
            "DENİZ'İN BAŞINDA BASILI TUT",
            StoryInteractionKind.TakeCover,
            coverSurface.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.UnderTable,
            true,
            1,
            2.1f,
            2f);
        GameObject gripSurface = CreateRebuildSurface(
            "SafeTable_GripLegSurface",
            interactionRoot,
            new Vector3(-0.67f, 0.54f, 0.08f),
            new Vector3(0.58f, 1.02f, 0.58f),
            true);
        refs.safeCover = AddRebuildInteraction(
            gripSurface,
            "quake.cover.grip",
            "MASA AYAĞINDA BASILI TUT",
            StoryInteractionKind.TakeCover,
            gripSurface.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.UnderTable,
            true,
            1,
            3f,
            2f);
        refs.unsafeDoor = AddRebuildInteraction(
            CreateRebuildSurface(
                "UnsafeDoorTouchSurface",
                interactionRoot,
                world.closedDoor.transform.position + Vector3.up * 1.1f,
                new Vector3(1.7f, 2.3f, 0.72f),
                true),
            "quake.unsafe.door",
            "KAPIYA KOŞMAYI DENE",
            StoryInteractionKind.UnsafeChoice,
            world.exitInspectFocus,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.InspectExit,
            true);
        refs.unsafeWindow = AddRebuildInteraction(
            CreateRebuildSurface(
                "UnsafeWindowTouchSurface",
                interactionRoot,
                world.windowFocus.position,
                new Vector3(2.8f, 2.4f, 0.72f),
                true),
            "quake.unsafe.window",
            "PENCEREYE YÖNELMEYİ DENE",
            StoryInteractionKind.UnsafeChoice,
            world.windowFocus,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.InspectWindow,
            true);

        StoryInteractable listen = AddRebuildInteraction(
            CreateRebuildSurface(
                "PostQuake_ListenSurface",
                interactionRoot,
                world.tableFocus.position + new Vector3(0f, 0.18f, 0f),
                new Vector3(2.25f, 0.55f, 1.5f),
                true),
            "quake.post.listen",
            "OLDUĞUN YERDE SESLERİ DİNLE",
            StoryInteractionKind.Inspect,
            world.tableFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PostQuake,
            true,
            1,
            2.8f);
        StoryInteractable checkCan = AddRebuildInteraction(
            CreateRebuildSurface(
                "Can_PostCheckSurface",
                family.can.transform,
                new Vector3(0f, 0.67f, 0f),
                new Vector3(0.78f, 1.32f, 0.72f)),
            "quake.post.checkcan",
            "CAN'IN OMZUNDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            family.can.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.QuakeClose,
            true,
            1,
            2.6f);
        StoryInteractable inspectGlass = AddRebuildInteraction(
            world.brokenGlassVisual,
            "quake.post.glass",
            "CAM SINIRINI PARMAĞINLA TAKİP ET",
            StoryInteractionKind.Inspect,
            world.glassFocus,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.InspectBrokenGlass,
            true,
            1,
            1.5f);
        StoryInteractable shoes = AddRebuildDrag(
            props.shoePairRoot,
            family.deniz,
            "quake.post.shoes",
            "AYAKKABI ÇİFTİNİ DENİZ'İN AYAKLARINA SÜRÜKLE",
            StoryInteractionKind.Collect,
            StoryCameraZoneId.PostQuake,
            interactionRoot,
            new Vector3(1.05f, 0.5f, 1.05f));
        StoryInteractable canLaces = AddRebuildInteraction(
            world.canShoesWorld,
            "quake.post.canlaces",
            "CAN'IN BAĞCIKLARINDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            world.canShoesFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.QuakeClose,
            false,
            1,
            2.5f,
            2.2f);
        StoryInteractable bag = AddRebuildDrag(
            world.emergencyBagWorld,
            family.deniz,
            "quake.post.bag",
            "AFET ÇANTASINI DENİZ'E SÜRÜKLE",
            StoryInteractionKind.Collect,
            StoryCameraZoneId.PostQuake,
            interactionRoot,
            new Vector3(1.2f, 1.45f, 1.05f));
        GameObject consequenceSurface = CreateRebuildSurface(
            "PreparationConsequenceTouchSurface",
            interactionRoot,
            world.wardrobeFocus.position,
            new Vector3(1.75f, 2.8f, 1.3f),
            true);
        StoryInteractable consequences = AddRebuildInteraction(
            consequenceSurface,
            "quake.post.consequence",
            "DOLAPTAN ÇIKIŞA DOĞRU GÜVENLİ ROTAYI ÇİZ",
            StoryInteractionKind.Inspect,
            world.wardrobeFocus,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.InspectWardrobe,
            false,
            1,
            1.6f);
        StoryChapterBuilderCommon.SetGestureTarget(consequences, world.exitInspectFocus);
        Bounds familyPlanBounds = GetRebuildBounds(props.familyPlanCard);
        StoryInteractable familyPlan = AddRebuildInteraction(
            CreateRebuildSurface(
                "FamilyPlan_PostRouteSurface",
                interactionRoot,
                familyPlanBounds.center,
                Vector3.Max(familyPlanBounds.size, new Vector3(0.75f, 0.18f, 0.58f)),
                true),
            "quake.post.familyplan",
            "AİLE PLANINDAKİ BULUŞMA NOKTASINI TAKİP ET",
            StoryInteractionKind.Inspect,
            props.familyPlanCard.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.InspectExit,
            false,
            1,
            1.5f);
        StoryChapterBuilderCommon.SetGestureTarget(familyPlan, world.exitInspectFocus);

        refs.postQuakeBeats = new[]
        {
            Beat(listen, "ÖNCE DİNLE", "Yeni bir hareket veya düşme sesi var mı birkaç saniye dinle.",
                "Sarsıntı durmuş görünüyor. Yalnızca uzaktan küçük parçaların sesi geliyor.", 2.2f),
            Beat(checkCan, "CAN'I KONTROL ET", "Can'ın omzunda kal; cevap, nefes ve hareketini birlikte gözle.",
                "Can hemen cevap veriyor ve kollarını hareket ettirebiliyor. {COMFORT}", 2.5f),
            Beat(inspectGlass, "ZEMİNİ OKU", "Kırık cama yaklaşmadan halının açık kenarını parmağınla takip et.",
                "Cam pencere tarafına saçılmış. Halının açık kenarı ayakkabılara ulaşmak için güvenli.", 2.4f),
            Beat(shoes, "AYAKLARINI KORU", "Ayakkabı çiftini doğrudan Deniz'in ayaklarına sürükle.",
                "Deniz ayakkabılarını giyiyor; kırık parçalara karşı ayakları korunuyor.", 2.5f),
            Beat(canLaces, "CAN'A YARDIM ET", "Can'ın bağcıklarında basılı tutup gevşek kısmı toparla.",
                "Can'ın ayakkabıları da hazır. İki kardeş artık zeminde güvenle ilerleyebilir.", 2.5f),
            Beat(bag, "AFET ÇANTASINI AL", "Çantayı düşen eşyalardan uzak tarafından tutup Deniz'e sürükle.",
                "Afet çantası sırtta. Deniz'in iki eli de Can'a yardım etmek için serbest.", 2.8f),
            Beat(consequences, "HAZIRLIĞIN SONUCUNU GÖR", "Dolaptan kapıya uzanan güvenli hattı sahnede takip et.",
                "{WARDROBE} {EXIT}", 3f),
            Beat(familyPlan, "AİLE PLANINI HATIRLA", "Çizimdeki buluşma noktasından kapı yönüne doğru çizgiyi takip et.",
                "Anne (engelin arkasından): Biz buradayız. Siz koridora çıkın; toplanma alanında buluşacağız.", 3.2f)
        };

        refs.lightWithFlashlight = AddRebuildInteraction(
            world.denizWornBag,
            "quake.post.flashlight",
            "ÇANTANIN DIŞ CEBİNDEN FENERİ YANA ÇEK",
            StoryInteractionKind.Collect,
            world.denizWornBag.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.PostQuake,
            true);
        StoryChapterBuilderCommon.SetGestureTarget(refs.lightWithFlashlight, world.flashlightFocus);
        Bounds reflectorBounds = GetRebuildBounds(props.corridorReflector);
        refs.lightWithoutFlashlight = AddRebuildInteraction(
            CreateRebuildSurface(
                "EmergencyRouteLight_ChoiceSurface",
                interactionRoot,
                reflectorBounds.center,
                Vector3.Max(reflectorBounds.size, new Vector3(0.6f, 0.2f, 1.2f)),
                true),
            "quake.post.emergencylight",
            "ACİL IŞIK HATTINA DOKUN",
            StoryInteractionKind.Inspect,
            props.corridorReflector.transform,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.InspectExit,
            true);
        refs.exit = AddRebuildInteraction(
            CreateRebuildSurface(
                "ExitDoorSwipeSurface",
                interactionRoot,
                world.closedDoor.transform.position + Vector3.up * 1.05f,
                new Vector3(1.7f, 2.25f, 0.75f),
                true),
            "quake.post.exit",
            "KAPIYI KENDİ ÜZERİNDEN YANA ÇEK",
            StoryInteractionKind.Exit,
            world.exitInspectFocus,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.InspectExit,
            false,
            1,
            1.4f);
        StoryChapterBuilderCommon.SetGestureTarget(refs.exit, world.corridorFocus);

        refs.brokenGlassHazard = AddRebuildInteraction(
            CreateRebuildSurface(
                "BrokenGlassHazardTrigger",
                interactionRoot,
                world.glassFocus.position + new Vector3(0f, -0.38f, 0f),
                new Vector3(1.45f, 0.48f, 1.25f),
                true),
            "quake.hazard.glass",
            "KIRIK CAM TEHLİKESİ",
            StoryInteractionKind.UnsafeChoice,
            world.glassFocus,
            StoryInteractionGesture.Approach,
            StoryCameraZoneId.InspectBrokenGlass,
            false,
            1,
            0.6f);
        SerializedObject hazard = new SerializedObject(refs.brokenGlassHazard);
        hazard.FindProperty("autoTriggerOnPlayerEnter").boolValue = true;
        hazard.FindProperty("oneShot").boolValue = false;
        hazard.ApplyModifiedPropertiesWithoutUndo();

        StoryInteractable holdCan = AddRebuildInteraction(
            CreateRebuildSurface(
                "Can_CorridorHandSurface",
                family.can.transform,
                new Vector3(-0.18f, 0.62f, 0.02f),
                new Vector3(0.62f, 0.9f, 0.62f)),
            "quake.corridor.hand",
            "CAN'IN ELİNDE BASILI TUT",
            StoryInteractionKind.HelpSibling,
            family.can.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.Corridor,
            true,
            1,
            2.2f);
        StoryInteractable scanFloor = AddRebuildInteraction(
            props.corridorReflector,
            "quake.corridor.scan",
            "IŞIĞI ZEMİNDE İLERİ SÜR",
            StoryInteractionKind.Inspect,
            props.corridorReflector.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.Corridor,
            false,
            1,
            1.5f);
        StoryChapterBuilderCommon.SetGestureTarget(scanFloor, world.corridorStepFoci[1]);
        StoryInteractable moveRubble = AddRebuildDrag(
            props.corridorRubble,
            props.corridorRubbleTarget,
            "quake.corridor.rubble",
            "HAFİF PARÇAYI KORİDORUN KENARINA SÜRÜKLE",
            StoryInteractionKind.Collect,
            StoryCameraZoneId.Corridor,
            interactionRoot,
            new Vector3(0.9f, 0.55f, 0.9f));
        StoryInteractable answerParent = AddRebuildInteraction(
            props.parentDoorKnockSurface,
            "quake.corridor.parent",
            "KAPININ AÇIKTAKİ KENARINA 3 KEZ VUR",
            StoryInteractionKind.HelpSibling,
            props.parentDoorKnockSurface.transform,
            StoryInteractionGesture.RepeatedTap,
            StoryCameraZoneId.Corridor,
            false,
            3,
            1.8f,
            2.2f);
        UnityEventTools.AddBoolPersistentListener(
            answerParent.OnInteracted,
            props.parentResponseSignal.SetActive,
            true);
        UnityEventTools.AddStringPersistentListener(
            answerParent.OnInteracted,
            family.canAnimator.SetTrigger,
            "StoryCall");
        UnityEventTools.AddBoolPersistentListener(
            answerParent.OnInteracted,
            props.aftershockWarningDust.Play,
            true);
        UnityEventTools.AddPersistentListener(
            answerParent.OnInteracted,
            props.aftershockWarningCreak.Play);
        StoryInteractable aftershock = AddRebuildInteraction(
            CreateRebuildSurface(
                "Can_AftershockResponseSurface",
                family.can.transform,
                new Vector3(0f, 0.7f, 0f),
                new Vector3(0.82f, 1.32f, 0.76f)),
            "quake.corridor.aftershock",
            "CAN'IN UYARISINA GÜVEN — YANINDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            family.can.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.Corridor,
            true,
            1,
            3f);
        refs.corridorBeats = new[]
        {
            Beat(holdCan, "KORİDORDA BİRLİKTE KAL", "Can'ın elini doğrudan tut; aceleyle öne geçme.",
                "Deniz, Can'ın elini buluyor. İkisi kapı eşiğini birlikte geçiyor.", 2.4f),
            Beat(scanFloor, "IŞIĞI ZEMİNDE GEZDİR", "Feneri veya acil ışığı koridor çizgisi boyunca sür.",
                "Işık, zemindeki hafif parçayı ve açık kalan yan hattı gösteriyor.", 2.4f),
            Beat(moveRubble, "HAFİF ENGELİ KENARA AL", "Yalnızca hafif parçayı gerçek kenar alanına sürükle.",
                "Geçiş genişledi. Deniz ağır moloza dokunmadan Can için açık bir adım alanı bırakıyor.", 2.5f),
            Beat(answerParent, "BİZ İYİYİZ DİYE SES VER",
                "Devrilmiş dolaba dokunma; kapının açıkta kalan gerçek kenarına üç kez vur.",
                "Deniz kapının kenarına üç kez vuruyor: Biz iyiyiz, Can yanımda! Aralıktan sıcak bir ışık geliyor. Baba cevap verirken tavandan ince toz dökülüyor; Can sesi önce fark edip iç duvarı gösteriyor.", 2.8f),
            Beat(aftershock, "CAN BİR SES DUYDU",
                "Can'ın işaret ettiği iç duvar tarafında yanında kal; ince titreşim bitene kadar omzunda basılı tut.",
                "Can: Dur, tavandan yine ses geliyor. Deniz onun uyarısına güvenip yanında çöküyor; ikisi de merdivene koşmuyor.", 2.2f)
        };

        UnityEventTools.AddPersistentListener(refs.calmSibling.OnInteracted, sequence.OnSiblingCalmed);
        UnityEventTools.AddPersistentListener(refs.crouchStep.OnInteracted, sequence.OnCrouchStep);
        UnityEventTools.AddPersistentListener(refs.coverHeadStep.OnInteracted, sequence.OnCoverHeadStep);
        UnityEventTools.AddPersistentListener(refs.safeCover.OnInteracted, sequence.OnCoverReached);
        UnityEventTools.AddPersistentListener(refs.unsafeDoor.OnInteracted, sequence.OnUnsafeChoice);
        UnityEventTools.AddPersistentListener(refs.unsafeWindow.OnInteracted, sequence.OnUnsafeChoice);
        UnityEventTools.AddPersistentListener(refs.brokenGlassHazard.OnInteracted, sequence.OnPostQuakeHazard);
        foreach (BeatDefinition beat in refs.postQuakeBeats)
            UnityEventTools.AddPersistentListener(beat.interactable.OnInteracted, sequence.OnPostQuakeStep);
        UnityEventTools.AddPersistentListener(refs.lightWithFlashlight.OnInteracted, sequence.OnLightPrepared);
        UnityEventTools.AddPersistentListener(refs.lightWithoutFlashlight.OnInteracted, sequence.OnLightPrepared);
        UnityEventTools.AddPersistentListener(refs.exit.OnInteracted, sequence.OnCorridorReached);
        foreach (BeatDefinition beat in refs.corridorBeats)
            UnityEventTools.AddPersistentListener(beat.interactable.OnInteracted, sequence.OnCorridorStep);
        return refs;
    }

    private static StoryInteractable AddRebuildInteraction(
        GameObject source,
        string id,
        string prompt,
        StoryInteractionKind kind,
        Transform interactionPoint,
        StoryInteractionGesture gesture,
        StoryCameraZoneId cameraZone,
        bool fromAnywhere = false,
        int gestureCount = 1,
        float interactionSeconds = 1.2f,
        float range = 1.55f)
    {
        StoryInteractable interaction = StoryChapterBuilderCommon.AddInteractable(
            source,
            id,
            prompt,
            kind,
            interactionPoint,
            gesture,
            cameraZone,
            fromAnywhere,
            gestureCount,
            interactionSeconds,
            range);
        SerializedObject serialized = new SerializedObject(interaction);
        serialized.FindProperty("returnCameraAfterCompletion").boolValue = false;
        serialized.FindProperty("focusLingerSeconds").floatValue = 0.35f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return interaction;
    }

    private static StoryInteractable AddRebuildDrag(
        GameObject source,
        GameObject target,
        string id,
        string prompt,
        StoryInteractionKind kind,
        StoryCameraZoneId cameraZone,
        Transform dropZoneParent,
        Vector3 dropZoneSize)
    {
        StoryInteractable interaction = AddRebuildInteraction(
            source,
            id,
            prompt,
            kind,
            source.transform,
            StoryInteractionGesture.DragToTarget,
            cameraZone,
            false,
            1,
            1.25f,
            2.5f);
        BagDropZone dropZone = CreateRebuildDropZone(
            "Drop_" + id.Replace('.', '_'),
            dropZoneParent,
            GetRebuildBounds(target).center,
            dropZoneSize);
        ConfigureRebuildDrag(interaction, dropZone);
        return interaction;
    }

    private static BagDropZone CreateRebuildDropZone(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 size)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = position;
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return zone.AddComponent<BagDropZone>();
    }

    private static void ConfigureRebuildDrag(
        StoryInteractable interaction,
        BagDropZone dropZone)
    {
        BoxCollider[] boxColliders = interaction.GetComponents<BoxCollider>();
        BoxCollider collider = boxColliders.Length > 0
            ? boxColliders[0]
            : interaction.gameObject.AddComponent<BoxCollider>();
        if (collider == null)
            throw new InvalidOperationException(interaction.InteractionId + " için kök BoxCollider oluşturulamadı.");
        FitRebuildCollider(collider, interaction.gameObject);
        collider.isTrigger = false;

        DraggableItem draggable = interaction.GetComponent<DraggableItem>() ??
                                  interaction.gameObject.AddComponent<DraggableItem>();
        SerializedObject data = new SerializedObject(draggable);
        data.FindProperty("isCorrectItem").boolValue = true;
        data.FindProperty("displayName").stringValue = interaction.Prompt;
        data.FindProperty("inputEnabled").boolValue = false;
        data.FindProperty("notifyGameManager").boolValue = false;
        data.FindProperty("tapToBagEnabled").boolValue = false;
        data.FindProperty("dropZoneOverride").objectReferenceValue = dropZone;
        data.FindProperty("dragLift").floatValue = 0.12f;
        data.FindProperty("dragScale").floatValue = 1.04f;
        data.FindProperty("returnDuration").floatValue = 0.34f;
        data.FindProperty("returnArcHeight").floatValue = 0.12f;
        data.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(interaction, dropZone.transform);
    }

    private static GameObject CreateRebuildSurface(
        string name,
        Transform parent,
        Vector3 positionOrLocal,
        Vector3 size,
        bool worldPosition = false)
    {
        GameObject surface = new GameObject(name);
        surface.transform.SetParent(parent);
        if (worldPosition)
            surface.transform.position = positionOrLocal;
        else
            surface.transform.localPosition = positionOrLocal;
        BoxCollider collider = surface.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return surface;
    }

    private static PlayableDirector BuildRebuildEarthquakeTimeline(
        Transform parent,
        Materials materials,
        WorldReferences world,
        out CinemachineImpulseSource impulse,
        out AudioSource impactSource)
    {
        GameObject timelineRoot = new GameObject("EarthquakeTimeline_Rebuild_48s");
        timelineRoot.transform.SetParent(parent);
        PlayableDirector director = timelineRoot.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;

        GameObject fxRoot = new GameObject("EarthquakeFX_SceneAuthored_Rebuild");
        fxRoot.transform.SetParent(timelineRoot.transform);
        ParticleSystem ceilingDust = CreateRebuildDust(
            "Phase1_CeilingDust",
            fxRoot.transform,
            new Vector3(0f, 2.82f, 0.7f),
            new Vector3(8f, 0.5f, 8f),
            materials.dust,
            0.4f,
            true,
            10f);
        ParticleSystem shelfDust = CreateRebuildDust(
            "Phase2_ShelfDust",
            fxRoot.transform,
            new Vector3(4.15f, 2.4f, 1.55f),
            new Vector3(1.8f, 0.7f, 1.5f),
            materials.dust,
            12f,
            false,
            30f);
        ParticleSystem finalDust = CreateRebuildDust(
            "Phase3_FinalDust",
            fxRoot.transform,
            new Vector3(1.8f, 2.65f, 4.7f),
            new Vector3(3.4f, 0.8f, 2.6f),
            materials.dust,
            34f,
            false,
            38f);
        ParticleSystem shelfChips = CreateRebuildDebrisBurst(
            "Phase2_ShelfDebrisChips",
            fxRoot.transform,
            new Vector3(4.15f, 2.18f, 1.55f),
            materials.wood,
            11.05f,
            22);
        ParticleSystem wardrobeChips = CreateRebuildDebrisBurst(
            "Phase2_WardrobePlasterChips",
            fxRoot.transform,
            new Vector3(-4.15f, 2.2f, 2.7f),
            materials.cream,
            17.05f,
            34);
        ParticleSystem doorwayChips = CreateRebuildDebrisBurst(
            "Phase3_DoorwayPlasterChips",
            fxRoot.transform,
            new Vector3(1.8f, 2.35f, 4.7f),
            materials.corridor,
            34.05f,
            28);
        ceilingDust.gameObject.SetActive(true);
        shelfDust.gameObject.SetActive(true);
        finalDust.gameObject.SetActive(true);
        shelfChips.gameObject.SetActive(true);
        wardrobeChips.gameObject.SetActive(true);
        doorwayChips.gameObject.SetActive(true);

        AudioSource rumble = fxRoot.AddComponent<AudioSource>();
        rumble.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/quake_rumble.wav");
        rumble.loop = true;
        rumble.playOnAwake = true;
        rumble.volume = 0.44f;
        rumble.spatialBlend = 0f;

        GameObject flickerObject = new GameObject("QuakeFlickerLight_Rebuild");
        flickerObject.transform.SetParent(fxRoot.transform);
        flickerObject.transform.position = new Vector3(0f, 2.72f, 0.2f);
        Light flickerLight = flickerObject.AddComponent<Light>();
        flickerLight.type = LightType.Point;
        flickerLight.range = 9.5f;
        flickerLight.intensity = 1.15f;
        flickerLight.color = new Color(1f, 0.63f, 0.38f);
        Animator flickerAnimator = flickerObject.AddComponent<Animator>();

        TimelineAsset timeline = CreateTimelineAsset(RebuildTimelinePath);
        ActivationTrack activation = timeline.CreateTrack<ActivationTrack>(null, "00 • Deprem VFX ve Ses");
        activation.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
        TimelineClip activeClip = activation.CreateDefaultClip();
        activeClip.duration = RebuildQuakeDuration;
        director.SetGenericBinding(activation, fxRoot);

        AnimationClip flickerClip = CreateAnimationAsset("Story03Rebuild_LightFlicker");
        flickerClip.wrapMode = WrapMode.Loop;
        flickerClip.SetCurve(string.Empty, typeof(Light), "m_Intensity", new AnimationCurve(
            new Keyframe(0f, 1.15f),
            new Keyframe(0.3f, 0.42f),
            new Keyframe(0.55f, 1.3f),
            new Keyframe(0.9f, 0.72f),
            new Keyframe(1.2f, 1.15f)));
        BindRebuildLoop(timeline, director, flickerAnimator, "03 • Işık değişimleri", flickerClip);
        BindRebuildLoop(
            timeline,
            director,
            world.safeTable,
            "01 • Masa yatay titreşimi",
            CreateRattleClip(
                "Story03Rebuild_SafeTable_Rattle",
                world.safeTable.transform,
                0.038f,
                0.028f,
                1.05f));
        BindRebuildLoop(
            timeline,
            director,
            world.hangingLamp,
            "01 • Asılı lamba salınımı",
            CreateSwingClip("Story03Rebuild_HangingLamp_Swing"));
        BindRebuildLoop(
            timeline,
            director,
            world.looseProps,
            "01 • Radyo ve hafif eşya kayması",
            CreateRattleClip(
                "Story03Rebuild_LooseProps_Slide",
                world.looseProps.transform,
                0.1f,
                0.075f,
                1.8f));
        BindRebuildLoop(
            timeline,
            director,
            world.closedDoor,
            "03 • Kapı vuruntusu",
            CreateRattleClip(
                "Story03Rebuild_Door_Rattle",
                world.closedDoor.transform,
                0.01f,
                0.022f,
                2.4f));
        BindRebuildLoop(
            timeline,
            director,
            world.wardrobeSecured,
            "02 • Sabit dolap sonucu",
            CreateRattleClip(
                "Story03Rebuild_SecuredWardrobe_Rattle",
                world.wardrobeSecured.transform,
                0.015f,
                0.01f,
                0.7f));
        BindRebuildLoop(
            timeline,
            director,
            world.shelfStable,
            "02 • Sabit raf sonucu",
            CreateRattleClip(
                "Story03Rebuild_SecuredShelf_Rattle",
                world.shelfStable.transform,
                0.014f,
                0.01f,
                0.65f));
        BindRebuildOneShot(
            timeline,
            director,
            world.shelfUnsecured,
            "02 • Sabitlenmemiş raf düşüşü",
            CreateFallClip("Story03Rebuild_UnsecuredShelf_Fall", -64f, 5.2f),
            11d);
        BindRebuildOneShot(
            timeline,
            director,
            world.wardrobeUnsecured,
            "02 • Sabitlenmemiş dolap düşüşü",
            CreateFallClip("Story03Rebuild_UnsecuredWardrobe_Fall", 74f, 6.4f),
            17d);
        GameObject detailAudioObject = new GameObject("QuakeDetailAudio_Rebuild");
        detailAudioObject.transform.SetParent(fxRoot.transform);
        AudioSource detailAudio = detailAudioObject.AddComponent<AudioSource>();
        detailAudio.playOnAwake = false;
        detailAudio.volume = 0.52f;
        detailAudio.spatialBlend = 0.18f;
        detailAudio.dopplerLevel = 0f;
        AudioTrack detailTrack = timeline.CreateTrack<AudioTrack>(null, "04 • Darbe ve döküntü sesleri");
        director.SetGenericBinding(detailTrack, detailAudio);
        AddRebuildAudioCue(detailTrack, "sfx100v2_metal_hit_02.ogg", 2.1d);
        AddRebuildAudioCue(detailTrack, "sfx100v2_wood_hit_01.ogg", 6.8d);
        AddRebuildAudioCue(detailTrack, "sfx100v2_stones_02.ogg", 11.1d);
        AddRebuildAudioCue(detailTrack, "sfx100v2_wood_hit_03.ogg", 17.2d);
        AddRebuildAudioCue(detailTrack, "sfx100v2_stones_03.ogg", 26.4d);
        AddRebuildAudioCue(detailTrack, "sfx100v2_door_04.ogg", 37.8d);
        director.playableAsset = timeline;
        fxRoot.SetActive(false);

        GameObject impulseObject = new GameObject("EarthquakeImpulseSource_Rebuild");
        impulseObject.transform.SetParent(timelineRoot.transform);
        impulse = impulseObject.AddComponent<CinemachineImpulseSource>();
        impulse.ImpulseDefinition.ImpulseChannel = 1;
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        impulse.ImpulseDefinition.ImpulseDuration = 0.46f;
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulse.DefaultVelocity = new Vector3(0.58f, 0.18f, 0.46f);

        impactSource = timelineRoot.AddComponent<AudioSource>();
        impactSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/quake_impact.wav");
        impactSource.playOnAwake = false;
        impactSource.volume = 0.78f;
        impactSource.spatialBlend = 0f;
        return director;
    }

    private static void BindRebuildLoop(
        TimelineAsset timeline,
        PlayableDirector director,
        Object target,
        string trackName,
        AnimationClip clip)
    {
        Animator animator;
        if (target is GameObject gameObject)
            animator = gameObject.GetComponent<Animator>() ?? gameObject.AddComponent<Animator>();
        else
            animator = target as Animator;
        if (animator == null || clip == null)
            return;

        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.duration = RebuildQuakeDuration;
        if (motion.asset is AnimationPlayableAsset playable)
            playable.loop = AnimationPlayableAsset.LoopMode.On;
        director.SetGenericBinding(track, animator);
    }

    private static void BindRebuildOneShot(
        TimelineAsset timeline,
        PlayableDirector director,
        GameObject target,
        string trackName,
        AnimationClip clip,
        double start)
    {
        Animator animator = target.GetComponent<Animator>() ?? target.AddComponent<Animator>();
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.start = start;
        motion.duration = clip.length;
        director.SetGenericBinding(track, animator);
    }

    private static void AddRebuildAudioCue(AudioTrack track, string clipFileName, double start)
    {
        AudioClip clip = StoryChapterBuilderCommon.LoadLicensedSfx(clipFileName);
        if (track == null || clip == null)
            return;

        TimelineClip cue = track.CreateClip<AudioPlayableAsset>();
        cue.displayName = clip.name;
        cue.start = start;
        cue.duration = clip.length;
        if (cue.asset is AudioPlayableAsset playable)
        {
            playable.clip = clip;
            playable.loop = false;
        }
    }

    private static ParticleSystem CreateRebuildDust(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 boxSize,
        Material material,
        float startDelay,
        bool loop,
        float rate)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = loop;
        main.duration = loop ? 6f : 1.4f;
        main.startDelay = startDelay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.65f, 0.53f, 0.5f));
        main.gravityModifier = 0.08f;
        main.maxParticles = 170;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = loop ? rate : 0f;
        if (!loop)
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(rate)) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        noise.frequency = 0.42f;
        noise.scrollSpeed = 0.08f;
        noise.damping = true;
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.28f),
                new Keyframe(0.22f, 0.82f),
                new Keyframe(1f, 1.45f)));
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.76f, 0.69f, 0.58f), 0f),
                new GradientColorKey(new Color(0.84f, 0.78f, 0.68f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.12f),
                new GradientAlphaKey(0.34f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = fade;
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        return particles;
    }

    private static ParticleSystem CreateRebuildDebrisBurst(
        string name,
        Transform parent,
        Vector3 position,
        Material material,
        float startDelay,
        int count)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.35f;
        main.startDelay = startDelay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.72f;
        main.maxParticles = Mathf.Max(48, count);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;
        shape.radiusThickness = 1f;

        ParticleSystem.CollisionModule collision = particles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.dampen = 0.38f;
        collision.bounce = 0.12f;
        collision.lifetimeLoss = 0.32f;
        collision.maxCollisionShapes = 32;

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = GetOrCreateRebuildDebrisChipMesh();
        renderer.material = material;
        renderer.alignment = ParticleSystemRenderSpace.World;
        return particles;
    }

    private static Mesh GetOrCreateRebuildDebrisChipMesh()
    {
        string path = GeneratedRoot + "/QuakeDebrisChip.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null)
            return mesh;

        mesh = new Mesh { name = "QuakeDebrisChip" };
        mesh.vertices = new[]
        {
            new Vector3(-0.55f, -0.22f, -0.32f),
            new Vector3(0.48f, -0.18f, -0.28f),
            new Vector3(0.34f, 0.24f, -0.18f),
            new Vector3(-0.42f, 0.18f, -0.24f),
            new Vector3(-0.28f, -0.12f, 0.36f),
            new Vector3(0.38f, -0.1f, 0.3f),
            new Vector3(0.18f, 0.2f, 0.26f)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 4, 2, 4, 6,
            3, 0, 4
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void ConfigureRebuildSequence(StorySequenceDirector sequence)
    {
        SerializedObject serialized = new SerializedObject(sequence);
        serialized.FindProperty("revisedFlow").boolValue = true;
        serialized.FindProperty("revisedShoesBeatIndex").intValue = 3;
        serialized.FindProperty("revisedCanShoesBeatIndex").intValue = 4;
        serialized.FindProperty("revisedBagBeatIndex").intValue = 5;
        serialized.FindProperty("introMinimumDuration").floatValue = 0f;
        serialized.FindProperty("introMaximumDuration").floatValue = 72f;
        serialized.FindProperty("revisedQuakeDelayAfterFamilyMoment").floatValue = 55f;
        serialized.FindProperty("quakeMinimumDuration").floatValue = 46f;
        serialized.FindProperty("postQuakeSettleDuration").floatValue = 2.4f;
        serialized.FindProperty("minimumCompletionDuration").floatValue = 0f;
        serialized.FindProperty("corridorWarningDuration").floatValue = 8f;
        serialized.FindProperty("estimatedTraversalDuration").floatValue = 150f;
        serialized.FindProperty("quakeMovementRadius").floatValue = 2.35f;
        serialized.FindProperty("safetyDecisionSeconds").floatValue = 11f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FitRebuildCollider(BoxCollider collider, GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one * 0.55f;
            return;
        }

        Bounds local = new Bounds(
            root.transform.InverseTransformPoint(renderers[0].bounds.center),
            Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                local.Encapsulate(root.transform.InverseTransformPoint(point));
            }
        }
        collider.center = local.center;
        collider.size = Vector3.Max(local.size, Vector3.one * 0.18f);
    }

    private static Bounds GetRebuildBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 0.25f);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Transform FindRebuildRequired(Transform root, string name)
    {
        Transform match = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == name);
        if (match == null)
            throw new InvalidOperationException(name + " Story 03 rebuild sahnesinde bulunamadı.");
        return match;
    }

    private static void SetRebuildActive(Transform root, string name, bool active)
    {
        FindRebuildRequired(root, name).gameObject.SetActive(active);
    }

    private static void RequireRebuildAsset(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Story 03 rebuild asseti bulunamadı.", path);
    }
}

public static class StoryQuakeRebuildPreviewValidator
{
    [MenuItem("Tools/Deprem Story/Validate Story_03 Rebuild Preview")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    [MenuItem("Tools/Deprem Story/Validate Story_03 Rebuild Preview (Silent)")]
    public static void ValidateSilentFromMenu()
    {
        Validate(false);
    }

    public static void Validate(bool showDialog)
    {
        if (!File.Exists(StoryVerticalSliceBuilder.RebuildPreviewScenePath))
        {
            throw new FileNotFoundException(
                "Story 03 rebuild önizleme sahnesi bulunamadı.",
                StoryVerticalSliceBuilder.RebuildPreviewScenePath);
        }

        Scene scene = SceneManager.GetSceneByPath(StoryVerticalSliceBuilder.RebuildPreviewScenePath);
        bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
        if (openedForValidation)
        {
            scene = EditorSceneManager.OpenScene(
                StoryVerticalSliceBuilder.RebuildPreviewScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            GameObject root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == "STORY_03_REBUILD_PREVIEW");
            Require(root != null, "STORY_03_REBUILD_PREVIEW kökü");

            GameObject sharedHome = root.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name == "StoryHome_Shared")
                .Select(candidate => candidate.gameObject)
                .SingleOrDefault();
            Require(sharedHome != null, "tek StoryHome_Shared instance'ı");
            Require(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome) ==
                StorySharedHomePrefabBuilder.PrefabPath,
                "bağlantılı ortak ev prefabı");
            Require(root.GetComponentsInChildren<StorySequenceDirector>(true).Length == 1,
                "tek StorySequenceDirector");
            StorySequenceDirector director =
                root.GetComponentInChildren<StorySequenceDirector>(true);
            Require(director.RevisedFlow, "revisedFlow açık olması");
            Require(Mathf.Approximately(director.MinimumCompletionDuration, 0f),
                "yapay 480 saniye kapısının bulunmaması");
            Require(root.GetComponentsInChildren<StoryTouchManager>(true).Length == 1,
                "tek StoryTouchManager");
            Require(root.GetComponentsInChildren<StoryPlayerMovement>(true).Length == 1,
                "tek StoryPlayerMovement");
            Require(root.GetComponentsInChildren<StoryActionButton>(true).Length == 0,
                "merkez görev butonu bulunmaması");
            Require(root.GetComponentsInChildren<Transform>(true)
                    .Any(candidate => candidate.name == "Can_ComfortToy_PostQuake"),
                "Story 01 rahatlatıcı eşya kararının Story 03 fiziksel karşılığı");
            Require(root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true).Length == 1,
                "sahneye ait tek NavMeshSurface");

            SerializedObject directorData = new SerializedObject(director);
            Require(
                directorData.FindProperty("introInspections").arraySize == 2,
                "iki parçalı zorunlu aile oyunu açılışı");
            Require(
                directorData.FindProperty("introOptionalMoments").arraySize == 2,
                "radyo ve aile planının isteğe bağlı kalması");
            Require(
                Mathf.Approximately(
                    directorData.FindProperty("revisedQuakeDelayAfterFamilyMoment").floatValue,
                    55f),
                "açılış aile anından sonra görünmez 55 saniyelik dramatik pencere");

            StoryInteractable[] interactions = root.GetComponentsInChildren<StoryInteractable>(true);
            Require(interactions.Length >= 24, "en az 24 doğrudan dünya etkileşimi");
            StoryInteractable[] drags = interactions
                .Where(candidate => candidate.InteractionGesture == StoryInteractionGesture.DragToTarget)
                .ToArray();
            Require(drags.Length >= 5, "en az beş fiziksel hedefli sürükleme");
            foreach (StoryInteractable drag in drags)
            {
                DraggableItem draggable = drag.GetComponent<DraggableItem>();
                Require(draggable != null, drag.InteractionId + " DraggableItem");
                Require(draggable.DropZoneOverride != null, drag.InteractionId + " hedef alanı");
                Require(drag.GestureTarget == draggable.DropZoneOverride.transform,
                    drag.InteractionId + " görünür hedef bağlantısı");
            }

            string[] required =
            {
                "quake.intro.wheel",
                "quake.intro.car",
                "quake.cover.head",
                "quake.cover.grip",
                "quake.post.shoes",
                "quake.post.bag",
                "quake.post.consequence",
                "quake.corridor.rubble",
                "quake.corridor.aftershock"
            };
            foreach (string id in required)
                Require(interactions.Any(candidate => candidate.InteractionId == id), id);

            CinemachineCamera[] cameras = root.GetComponentsInChildren<CinemachineCamera>(true);
            Require(cameras.Length == 10, "on bestelenmiş Cinemachine kamera");
            foreach (CinemachineCamera camera in cameras)
            {
                Require(
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f,
                    camera.name + " lens aralığı");
            }
            CinemachineCamera coverCamera = cameras.SingleOrDefault(
                candidate => candidate.name == "CM03R_UnderTableTwoShot");
            Require(coverCamera != null, "iki çocuklu masa koruma kamerası");
            Require(coverCamera.transform.position.y >= 2.3f,
                "masa koruma kamerasının yalnızca ayakları göstermemesi");

            PlayableDirector quakeTimeline = root.GetComponentsInChildren<PlayableDirector>(true)
                .SingleOrDefault(candidate => candidate.name == "EarthquakeTimeline_Rebuild_48s");
            Require(quakeTimeline != null, "48 saniyelik deprem Timeline'ı");
            Require(quakeTimeline.playableAsset != null, "deprem Timeline asseti");
            Require(
                quakeTimeline.playableAsset.duration >= 45d &&
                quakeTimeline.playableAsset.duration <= 50d,
                "deprem Timeline süresi 45–50 saniye");
            Require(root.GetComponentsInChildren<ParticleSystem>(true).Length >= 3,
                "üç fazlı sahne-authored toz VFX'i");
            Require(
                EditorBuildSettings.scenes.Any(candidate =>
                    candidate.enabled && candidate.path == StoryVerticalSliceBuilder.RebuildPreviewScenePath),
                "Story 03 yayın sahnesinin Build Settings'te olması");

            Debug.Log(
                $"Story_03_RebuildPreview doğrulandı: interactions={interactions.Length}, " +
                $"directDrags={drags.Length}, cameras={cameras.Length}, timeline={quakeTimeline.playableAsset.duration:F1}s");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 03 rebuild önizlemesi yapısal doğrulamayı geçti.",
                    "Tamam");
            }
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
            throw new InvalidOperationException("Story 03 rebuild doğrulama hatası: " + label);
    }
}
