using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
        public GameObject entryBagShelf;
        public GameObject denizFearFace;
        public GameObject canFearFace;
    }

    private sealed class RebuildDestructionProps
    {
        public GameObject familyPhoto;
        public GameObject looseBookA;
        public GameObject looseBookB;
        public GameObject plant;
        public GameObject mug;
        public GameObject windowCrack;
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
            ConfigureRebuildCharacterProps(world, family, props, materials);

            StoryCameraController cameraController = BuildRebuildCameras(
                root.transform,
                family.deniz.transform,
                world,
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
            StoryChapterBuilderCommon.ConfigureDialogueActors(
                chapterUi.controller,
                new StoryChapterBuilderCommon.DialogueActorSpec(family.deniz, "Deniz"),
                new StoryChapterBuilderCommon.DialogueActorSpec(family.can, "Can"));
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
                props,
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
            SnapRebuildNavigationAnchors(family, interactions);
            ReframeCoverCamera(root.transform, interactions);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);

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
        StoryChapterBuilderCommon.ApplyCanonicalIndoorHomeShell(home);
        BuildRebuildPendantLamp(world.hangingLamp, materials);
        // Masa altına sığınma bu bölümün çekirdek mekaniği: kabuğun çocuk-ölçek
        // ezmesi (%74) yalnız bu sahnede geri açılır. Tam yükseklikte tabla altı
        // ~0.92 m olur; derinleştirilmiş çömelme pozuyla kafalar tablayı geçmez.
        // Masa üstü intro eşyaları yüzeyden ölçüldüğü için otomatik uyum sağlar.
        Vector3 quakeTableScale = world.safeTable.transform.localScale;
        world.safeTable.transform.localScale = new Vector3(
            quakeTableScale.x,
            quakeTableScale.y / 0.74f,
            quakeTableScale.z);
        // The tabletop remains a real physics surface, but it must not be treated as
        // a low ceiling by the upright-agent NavMesh bake.  The four leg colliders
        // still participate in the bake, leaving a genuine route between them for
        // the authored crouch/cover anchors instead of making the whole table void.
        Transform safeTableTop = FindRebuildRequired(world.safeTable.transform, "Top");
        NavMeshModifier safeTableTopModifier = safeTableTop.GetComponent<NavMeshModifier>();
        if (safeTableTopModifier == null)
            safeTableTopModifier = safeTableTop.gameObject.AddComponent<NavMeshModifier>();
        safeTableTopModifier.ignoreFromBuild = true;
        BuildRebuildStory02Payoff(world, materials);
        BuildRebuildOpeningSet(world, props, materials);
        BuildRebuildShoePair(world, props, materials);
        BuildRebuildEntryStorage(world, props);
        StoryPreparationRebuildPreviewBuilder.BuildCanonicalLivedInHomeArtForChapter(
            world.environment.transform,
            props.sharedHome,
            props.entryBagShelf);
        Bounds continuityBagShelfBounds = GetRebuildBounds(props.entryBagShelf);
        PlaceRebuildPropOnSurface(
            world.emergencyBagWorld,
            new Vector2(continuityBagShelfBounds.center.x, continuityBagShelfBounds.center.z),
            continuityBagShelfBounds.max.y + 0.01f);
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

    private static void BuildRebuildPendantLamp(GameObject lampRoot, Materials materials)
    {
        if (lampRoot == null)
            throw new InvalidOperationException("Story 03 asılı lamba kökü bulunamadı.");

        lampRoot.transform.SetPositionAndRotation(
            new Vector3(0.35f, 3.18f, 0.45f),
            Quaternion.identity);

        Transform importedVisual = lampRoot.transform.Find("HangingLamp_Visual");
        if (importedVisual == null)
            throw new InvalidOperationException("Story 03 eski lamba görseli bulunamadı.");
        foreach (Renderer renderer in importedVisual.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        foreach (Collider collider in importedVisual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        Light importedLight = lampRoot.GetComponent<Light>();
        if (importedLight != null)
            importedLight.enabled = false;

        Transform visual = StoryChapterBuilderCommon.NewChild(lampRoot.transform, "PendantLamp_AnchoredVisual");
        CreateRebuildLampPart("CeilingPlate", PrimitiveType.Cylinder, visual,
            new Vector3(0f, -0.035f, 0f), new Vector3(0.2f, 0.035f, 0.2f), materials.navy);
        CreateRebuildLampPart("SuspensionCable", PrimitiveType.Cylinder, visual,
            new Vector3(0f, -0.29f, 0f), new Vector3(0.018f, 0.25f, 0.018f), materials.navy);
        CreateRebuildLampPart("PendantShade", PrimitiveType.Cylinder, visual,
            new Vector3(0f, -0.57f, 0f), new Vector3(0.27f, 0.105f, 0.27f), materials.teal);
        CreateRebuildLampPart("PendantLowerRim", PrimitiveType.Cylinder, visual,
            new Vector3(0f, -0.675f, 0f), new Vector3(0.29f, 0.025f, 0.29f), materials.wood);
        CreateRebuildLampPart("VisibleWarmBulb", PrimitiveType.Sphere, visual,
            new Vector3(0f, -0.71f, 0f), new Vector3(0.085f, 0.105f, 0.085f), materials.amber);

        GameObject lightObject = new GameObject("PendantBulbLight");
        lightObject.transform.SetParent(visual, false);
        lightObject.transform.localPosition = new Vector3(0f, -0.71f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.58f);
        light.intensity = 1.25f;
        light.range = 4.2f;
        light.shadows = LightShadows.None;
    }

    private static GameObject CreateRebuildLampPart(
        string name,
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
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
        // The shared home controller is almost as wide as Can and reads as a fourth objective.
        // Story 03 keeps this table focused on its three authored interactions.
        FindRebuildRequired(props.sharedHome.transform, "FamilyBoardGame").gameObject.SetActive(false);

        props.toyCar = FindRebuildRequired(props.sharedHome.transform, "Can_ToyCar").gameObject;
        props.toyCar.transform.localScale *= 0.7f;
        Bounds tableBounds = GetRebuildBounds(world.safeTable);
        PlaceRebuildPropOnSurface(
            props.toyCar,
            new Vector2(tableBounds.center.x + 0.34f, tableBounds.center.z + 0.02f),
            tableBounds.max.y + 0.012f);
        Bounds carBounds = GetRebuildBounds(props.toyCar);

        props.wheel = StoryAuthoredPropFactory.CreateToyWheel(
            "Can_ToyWheel_Drag",
            dressing,
            world.tableFocus.position + new Vector3(0.92f, 0.02f, -0.72f),
            new Vector3(0.12f, 0.11f, 0.065f),
            new Vector3(90f, 0f, 0f),
            materials.navy,
            materials.cream,
            materials.amber,
            true);
        PlaceRebuildPropOnSurface(
            props.wheel,
            new Vector2(tableBounds.center.x + 0.62f, tableBounds.center.z + 0.02f),
            tableBounds.max.y + 0.012f);
        props.toyCarFinish = StoryChapterBuilderCommon.CreatePrimitive(
            "Can_ToyCar_FinishMarker",
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 0.035f, -0.12f),
            new Vector3(0.38f, 0.018f, 0.38f),
            materials.safeGlow,
            dressing,
            false);
        props.toyCarFinish.GetComponent<Renderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        props.toyCarFinish.SetActive(false);

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

        Vector3 planPosition = new Vector3(
            tableBounds.center.x - 0.5f,
            tableBounds.max.y + 0.014f,
            tableBounds.center.z + 0.12f);
        Quaternion planRotation = Quaternion.Euler(0f, -8f, 0f);
        props.familyPlanCard = StoryChapterBuilderCommon.CreatePrimitive(
            "Can_FamilyPlanDrawing",
            PrimitiveType.Cube,
            planPosition,
            new Vector3(0.46f, 0.018f, 0.32f),
            materials.cream,
            dressing,
            true,
            planRotation);

        // Make the plan read as Can's drawing instead of a blank beige slab.
        CreatePlanDrawingDetail(
            "FamilyPlan_Home",
            PrimitiveType.Cube,
            planPosition,
            planRotation,
            new Vector3(-0.11f, 0.016f, -0.045f),
            new Vector3(0.1f, 0.012f, 0.085f),
            materials.coral,
            dressing);
        CreatePlanDrawingDetail(
            "FamilyPlan_Route",
            PrimitiveType.Cube,
            planPosition,
            planRotation,
            new Vector3(0.025f, 0.016f, 0.015f),
            new Vector3(0.035f, 0.012f, 0.19f),
            materials.teal,
            dressing);
        CreatePlanDrawingDetail(
            "FamilyPlan_MeetingPoint",
            PrimitiveType.Cylinder,
            planPosition,
            planRotation,
            new Vector3(0.12f, 0.018f, 0.075f),
            new Vector3(0.075f, 0.012f, 0.075f),
            materials.amber,
            dressing);
        props.familyPlanMarked = StoryChapterBuilderCommon.CreatePrimitive(
            "FamilyPlan_AssemblyMark",
            PrimitiveType.Cylinder,
            planPosition + planRotation * new Vector3(0.12f, 0.03f, 0.075f),
            new Vector3(0.085f, 0.012f, 0.085f),
            materials.amber,
            dressing,
            false);
        props.familyPlanMarked.SetActive(false);

        if (carBounds.size.sqrMagnitude <= 0.0001f)
            throw new InvalidOperationException("Can'ın oyuncak arabası görsel sınır üretmedi.");
    }

    private static void CreatePlanDrawingDetail(
        string name,
        PrimitiveType primitiveType,
        Vector3 planPosition,
        Quaternion planRotation,
        Vector3 localOffset,
        Vector3 scale,
        Material material,
        Transform parent)
    {
        StoryChapterBuilderCommon.CreatePrimitive(
            name,
            primitiveType,
            planPosition + planRotation * localOffset,
            scale,
            material,
            parent,
            false,
            planRotation);
    }

    private static void BuildRebuildShoePair(WorldReferences world, RebuildProps props, Materials materials)
    {
        // Use the two actual Deniz shoe roots. Searching for the generic child names
        // "LeftShoe" / "RightShoe" selected Can's pair instead and left these two giant
        // duplicate shoes scattered beside the rug.
        GameObject originalLeft = FindRebuildRequired(props.sharedHome.transform, "DenizLeftShoe_World").gameObject;
        GameObject originalRight = FindRebuildRequired(props.sharedHome.transform, "DenizRightShoe_World").gameObject;
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
            new Vector3(0.42f, 0.14f, 0.34f),
            new Vector3(0f, -8f, 0f),
            materials.coral,
            materials.cream,
            true);
        world.leftShoeWorld = props.shoePairRoot.transform.Find("LeftShoe").gameObject;
        world.leftShoeWorld.name = "DenizShoePair_Left";
        world.rightShoeWorld = props.shoePairRoot.transform.Find("RightShoe").gameObject;
        world.rightShoeWorld.name = "DenizShoePair_Right";

        Bounds originalCanBounds = GetRebuildBounds(world.canShoesWorld);
        GameObject legacyCanShoes = world.canShoesWorld;
        legacyCanShoes.name = "CanShoes_World_Legacy";
        legacyCanShoes.SetActive(false);
        world.canShoesWorld = StoryAuthoredPropFactory.CreateShoePair(
            "CanShoes_World",
            world.environment.transform,
            new Vector3(originalCanBounds.center.x, originalCanBounds.min.y, originalCanBounds.center.z),
            new Vector3(0.38f, 0.13f, 0.31f),
            new Vector3(0f, 4f, 0f),
            materials.amber,
            materials.cream,
            true);

        originalLeft.SetActive(false);
        originalRight.SetActive(false);
    }

    private static void BuildRebuildEntryStorage(WorldReferences world, RebuildProps props)
    {
        GameObject shoeBench = StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_09.prefab",
            "EntryShoeBench_Continuity",
            world.environment.transform,
            new Vector3(4.72f, 0f, 4.25f),
            new Vector3(0.48f, 0.5f, 1.55f),
            new Vector3(0f, 90f, 0f));
        // InstantiateFurniture preserves aspect ratio, so the shallow wall depth
        // would otherwise collapse this cabinet into a 74 cm cube.  On this y=90
        // placement local X is the wall-parallel world-Z axis: extend only that
        // axis to make a proper two-seat shoe bench without blocking the doorway.
        shoeBench.transform.localScale = Vector3.Scale(
            shoeBench.transform.localScale,
            new Vector3(2.05f, 1f, 1f));
        Bounds shoeBenchBounds = GetRebuildBounds(shoeBench);
        PlaceRebuildPropOnSurface(
            props.shoePairRoot,
            new Vector2(4.72f, 3.88f),
            shoeBenchBounds.max.y + 0.01f);
        PlaceRebuildPropOnSurface(
            world.canShoesWorld,
            new Vector2(4.72f, 4.58f),
            shoeBenchBounds.max.y + 0.01f);

        Bounds denizShoesBounds = GetRebuildBounds(props.shoePairRoot);
        world.leftShoeFocus.position = denizShoesBounds.center;
        world.rightShoeFocus.position = denizShoesBounds.center;
        world.shoesFocus.position = denizShoesBounds.center;
        world.canShoesFocus.position = GetRebuildBounds(world.canShoesWorld).center;

        GameObject bagShelf = StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_09.prefab",
            "EntryBagShelf_Continuity",
            world.environment.transform,
            new Vector3(4.72f, 0f, 5.28f),
            new Vector3(0.48f, 0.66f, 0.58f),
            new Vector3(0f, 90f, 0f));
        Bounds bagShelfBounds = GetRebuildBounds(bagShelf);
        PlaceRebuildPropOnSurface(
            world.emergencyBagWorld,
            new Vector2(4.72f, 5.28f),
            bagShelfBounds.max.y + 0.01f);

        // These are permanent entry furniture, not walkable platforms.  Keep their
        // physical volumes and carve them from the runtime NavMesh so agents cannot
        // use cabinet tops as a route toward the corridor.
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(shoeBench);
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(bagShelf);
        props.entryBagShelf = bagShelf;
    }

    private static void PlaceRebuildPropOnSurface(GameObject prop, Vector2 horizontalCenter, float surfaceY)
    {
        Bounds bounds = GetRebuildBounds(prop);
        prop.transform.position += new Vector3(
            horizontalCenter.x - bounds.center.x,
            surfaceY - bounds.min.y,
            horizontalCenter.y - bounds.center.z);
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
        RebuildProps props,
        Materials materials)
    {
        world.denizWornShoes = FindRebuildRequired(family.deniz.transform, "Deniz_12_WornShoes").gameObject;
        world.denizWornBag = FindRebuildRequired(family.deniz.transform, "Deniz_WornEmergencyBag").gameObject;
        world.canWornShoes = FindRebuildRequired(family.can.transform, "Can_8_WornShoes").gameObject;
        world.denizWornShoes.SetActive(false);
        world.denizWornBag.SetActive(false);
        world.canWornShoes.SetActive(false);
        StoryChapterBuilderCommon.MountEmergencyBackpackToTorso(
            family.deniz,
            world.denizWornBag,
            family.deniz.transform.rotation);
        props.denizFearFace = BuildRebuildFearFace(family.deniz, materials);
        props.canFearFace = BuildRebuildFearFace(family.can, materials);

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

    private static GameObject BuildRebuildFearFace(GameObject character, Materials materials)
    {
        Transform head = StoryChapterBuilderCommon.FindHumanoidBone(character, HumanBodyBones.Head);
        if (head == null)
            throw new InvalidOperationException(character.name + " korku ifadesi için kafa kemiği bulunamadı.");

        Materials resolved = materials ?? throw new ArgumentNullException(nameof(materials));
        bool isCan = character.name.IndexOf("Can", StringComparison.OrdinalIgnoreCase) >= 0;
        float scale = isCan ? 0.84f : 1f;
        Vector3 forward = character.transform.forward.normalized;
        Vector3 up = character.transform.up.normalized;
        Vector3 right = character.transform.right.normalized;
        Vector3 faceCenter = head.position + up * (0.096f * scale) + forward * (0.132f * scale);
        GameObject root = new GameObject(character.name + "_QuakeFearFace");
        root.transform.SetParent(head, true);
        root.transform.SetPositionAndRotation(faceCenter, character.transform.rotation);

        StoryChapterBuilderCommon.CreatePrimitive(
            "FearMouth_Open",
            PrimitiveType.Sphere,
            faceCenter - up * ((isCan ? 0.1f : 0.086f) * scale) +
            forward * ((isCan ? 0.06f : 0.027f) * scale),
            new Vector3(0.027f, 0.032f, 0.014f) * scale,
            resolved.navy,
            root.transform,
            false,
            character.transform.rotation);
        StoryChapterBuilderCommon.CreatePrimitive(
            "FearBrow_Left",
            PrimitiveType.Cube,
            faceCenter - right * (0.05f * scale) + up * (0.043f * scale) +
            forward * (0.028f * scale),
            new Vector3(0.045f, 0.007f, 0.009f) * scale,
            resolved.wood,
            root.transform,
            false,
            character.transform.rotation * Quaternion.Euler(0f, 0f, 18f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "FearBrow_Right",
            PrimitiveType.Cube,
            faceCenter + right * (0.05f * scale) + up * (0.043f * scale) +
            forward * (0.028f * scale),
            new Vector3(0.045f, 0.007f, 0.009f) * scale,
            resolved.wood,
            root.transform,
            false,
            character.transform.rotation * Quaternion.Euler(0f, 0f, -18f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "FearSweatDrop",
            PrimitiveType.Sphere,
            faceCenter + right * (0.11f * scale) + up * (0.045f * scale) +
            forward * (0.03f * scale),
            new Vector3(0.011f, 0.028f, 0.009f) * scale,
            resolved.safeGlow,
            root.transform,
            false,
            character.transform.rotation * Quaternion.Euler(0f, 0f, -12f));
        root.SetActive(false);
        return root;
    }

    private static StoryCameraController BuildRebuildCameras(
        Transform parent,
        Transform player,
        WorldReferences world,
        out Camera mainCamera,
        out CinemachineBrain brain)
    {
        GameObject cameraRoot = new GameObject("Story03RebuildCameras");
        cameraRoot.transform.SetParent(parent);

        GameObject main = new GameObject("Main Camera");
        main.tag = "MainCamera";
        main.transform.SetParent(cameraRoot.transform);
        main.transform.position = new Vector3(1.35f, 2.7f, -5.15f);
        main.transform.rotation = LookAt(main.transform.position, new Vector3(0.15f, 0.9f, 0.45f));
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
            // Açılışta Deniz ile masa üstündeki iki gerçek oyun parçasını dar telefon
            // kadrajında birlikte tut. Oyuncuya bağlı eski takip planı Deniz'i merkeze
            // çekerken tekeri sağdan ekran dışına atıyordu.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.RoomOverview, "CM03R_RoomOverview",
                new Vector3(1.35f, 2.7f, -5.15f), new Vector3(0.15f, 0.9f, 0.45f), 56f),
            // Deprem başladığında oyuncu Can'a dokunmak zorunda. Oyuncuyu tek başına takip eden dar
            // portre kadraj Can'ı ekran dışına atıyordu; bu sabit iki-çocuk planı Deniz, Can ve masayı
            // aynı 9:16 kadrajda tutar.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.QuakeClose, "CM03R_QuakeClose",
                new Vector3(-3.35f, 2.18f, 3.25f), new Vector3(-0.15f, 0.88f, -0.2f), 42f),
            // Kritik koruma planı masanın içine veya yalnızca ayaklara girmez:
            // tabla, iki çocuk ve tutulan ön ayak aynı portre kadrajındadır.
            // Sola/geriye alınmış plan: tutulan sol ön masa ayağı (grip yüzeyi) da
            // tabla ve iki çocukla birlikte dikey kadrajın içindedir.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.UnderTable, "CM03R_UnderTableTwoShot",
                new Vector3(0.2f, 0.9f, -8f), new Vector3(0.45f, 0.42f, 0.55f), 50f),
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.PostQuake, "CM03R_PostQuake",
                new Vector3(2.25f, 2.62f, -4.2f), new Vector3(0.1f, 0.85f, 0.7f), 45f,
                player, 6.35f, new Vector2(-0.04f, 0.08f)),
            // Yüksek ve geriden iki-çocuk koridor planı: eşikte el ele duran çocuklar,
            // zemin ışığı, moloz bırakma kenarı ve uzaktaki ebeveyn kapısı aynı dar
            // telefon kadrajında okunur.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.Corridor, "CM03R_CorridorLong",
                new Vector3(2.5f, 5f, -2f), new Vector3(2.4f, 0.75f, 8.2f), 52f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectTable, "CM03R_TableFamilyMoment",
                new Vector3(3.45f, 2.42f, -3.65f), new Vector3(0.15f, 0.72f, 0.1f), 44f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWindow, "CM03R_WindowRisk",
                new Vector3(4.2f, 3.5f, -2f), new Vector3(-2.2f, 1.65f, 5.55f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWardrobe,
                "CM03R_PreparationConsequence",
                // Sabit 50 derece lensle hazırlanmış/hazırlanmamış sonuçları
                // 20:9 güvenli alanına birlikte sığdıracak daha uzak plan.
                new Vector3(1.9f, 10.7f, -16.5f), new Vector3(-0.5f, 0.8f, 4.3f), 50f),
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectExit, "CM03R_ExitAndCorridor",
                // 20:9 portrait is horizontally tighter than 9:16.  Aim a few
                // centimetres farther toward the wall storage so Can's laces stay
                // inside the mobile-safe x range without moving the prop off its
                // authored shoe-bench position.
                new Vector3(0.5f, 3.5f, -1.5f), new Vector3(3.52f, 0.8f, 5f), 52f),
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

    private static StoryCameraBinding MakeRebuildRouteCamera(
        Transform parent,
        StoryCameraZoneId zone,
        string name,
        Transform routeStart,
        Transform routeEnd)
    {
        Vector3 midpoint = (routeStart.position + routeEnd.position) * 0.5f;
        Vector3 position = midpoint + new Vector3(0f, 8.6f, -1.35f);
        StoryCameraBinding binding = MakeCamera(
            parent,
            zone,
            name,
            position,
            midpoint + Vector3.up * 0.35f,
            49f);

        // Portrait framing is narrow horizontally. Roll this high room plan so the long
        // wardrobe-to-door route reads vertically on screen and both real endpoints remain visible.
        Vector3 route = routeEnd.position - routeStart.position;
        Quaternion baseRotation = binding.camera.transform.rotation;
        float horizontal = Vector3.Dot(route, baseRotation * Vector3.right);
        float vertical = Vector3.Dot(route, baseRotation * Vector3.up);
        float roll = Mathf.Atan2(-horizontal, vertical) * Mathf.Rad2Deg;
        binding.camera.transform.rotation = baseRotation * Quaternion.AngleAxis(roll, Vector3.forward);
        return binding;
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

        // Masa üstündeki intro nesnelerinin kendi transform'ları etkileşim noktası
        // olarak kullanılamaz: NavMesh örneklemesi masa TABLASINDAKİ kopuk adaya
        // düşüyor ve MoveTo hiç tamamlanamıyordu (dokun → hiçbir şey olmaz).
        // Yaklaşma hedefleri zemindeki bu duruş noktalarıdır.
        Transform tableIntroStand = StoryChapterBuilderCommon.CreatePoint(
            "TableIntroStand",
            interactionRoot,
            new Vector3(0.5f, 0f, -0.75f),
            new Vector3(0.5f, 1f, 0.6f));
        Transform radioStand = StoryChapterBuilderCommon.CreatePoint(
            "RadioStand",
            interactionRoot,
            new Vector3(3.05f, 0f, -3.2f),
            new Vector3(3.84f, 0.97f, -4f));
        Transform entryStorageStand = StoryChapterBuilderCommon.CreatePoint(
            "EntryStorageFloorStand",
            interactionRoot,
            new Vector3(3.55f, 0f, 4.35f),
            new Vector3(4.72f, 0.62f, 4.35f));
        void SetInteractionPoint(StoryInteractable interaction, Transform point)
        {
            SerializedObject data = new SerializedObject(interaction);
            data.FindProperty("interactionPoint").objectReferenceValue = point;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

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
            "ARABAYI MASADAN CAN'IN ÖNÜNDEKİ HEDEFE SÜR",
            StoryInteractionKind.HelpSibling,
            StoryCameraZoneId.InspectTable,
            interactionRoot,
            new Vector3(1.15f, 0.45f, 1.15f));
        ConfigureRebuildReturnCamera(rollCar, StoryCameraZoneId.RoomOverview, 0.45f);
        SetInteractionPoint(wheel, tableIntroStand);
        SetInteractionPoint(rollCar, tableIntroStand);
        // Teker parmak için çok küçüktü (21 px). Dokunma hacmi dünya uzayında en az
        // 22 cm olacak şekilde büyütülür; büyük import kök ölçeklerine karşı taban
        // local değil dünya biriminde uygulanır.
        BoxCollider wheelCollider = wheel.GetComponent<BoxCollider>();
        if (wheelCollider != null)
        {
            Vector3 wheelScale = wheelCollider.transform.lossyScale;
            Vector3 wheelMinimum = new Vector3(
                0.22f / Mathf.Max(Mathf.Abs(wheelScale.x), 0.0001f),
                0.14f / Mathf.Max(Mathf.Abs(wheelScale.y), 0.0001f),
                0.22f / Mathf.Max(Mathf.Abs(wheelScale.z), 0.0001f));
            wheelCollider.size = Vector3.Max(wheelCollider.size, wheelMinimum);
        }
        UnityEventTools.AddBoolPersistentListener(
            wheel.OnInteracted,
            props.toyCarFinish.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            rollCar.OnInteracted,
            props.toyCarFinish.SetActive,
            false);
        StoryInteractable radio = AddRebuildInteraction(
            props.radioDial,
            "quake.intro.radio",
            "RADYO DÜĞMESİNİ SOLA ÇEVİR",
            StoryInteractionKind.Inspect,
            radioStand,
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
            tableIntroStand,
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

        // Enter from the reachable front edge, then move both crouched children to separate
        // authored positions inside the table's actual visible footprint.
        Transform coverAnchors = StoryChapterBuilderCommon.NewChild(interactionRoot, "CoverAnchors");
        Transform coverApproachAnchor = StoryChapterBuilderCommon.NewChild(coverAnchors, "Deniz_CoverApproachAnchor");
        Transform denizCoverAnchor = StoryChapterBuilderCommon.NewChild(coverAnchors, "Deniz_CoverAnchor");
        Transform canCoverAnchor = StoryChapterBuilderCommon.NewChild(coverAnchors, "Can_CoverAnchor");
        Bounds coverTableBounds = GetRebuildBounds(world.safeTable);
        Vector3 coverCenter = coverTableBounds.center;
        // İki çocuğu tablanın içinde ayrı noktalara yerleştir. Nihai yönleri
        // NavMesh snap işleminden sonra birbirlerine bakacak şekilde hesaplanır;
        // böylece koruma pozu mekanik biçimde aynı yöne bakan iki model gibi durmaz.
        float sideOffset = Mathf.Min(0.58f, coverTableBounds.extents.x * 0.64f);
        float insideZ = coverCenter.z;
        Vector3 denizCoverPosition = new Vector3(
            coverCenter.x - sideOffset,
            family.deniz.transform.position.y,
            insideZ);
        Vector3 canCoverPosition = new Vector3(
            coverCenter.x + sideOffset,
            family.can.transform.position.y,
            insideZ);
        coverApproachAnchor.position = new Vector3(
            denizCoverPosition.x,
            denizCoverPosition.y,
            coverTableBounds.min.z - 0.34f);
        coverApproachAnchor.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        refs.coverApproachAnchor = coverApproachAnchor;
        denizCoverAnchor.position = denizCoverPosition;
        canCoverAnchor.position = canCoverPosition;
        denizCoverAnchor.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        canCoverAnchor.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        refs.denizCoverAnchor = denizCoverAnchor;
        refs.canCoverAnchor = canCoverAnchor;
        refs.postQuakeSafeReturn = StoryChapterBuilderCommon.CreatePoint(
            "PostQuakeSafeReturn",
            coverAnchors,
            new Vector3(-0.72f, 0f, -0.82f),
            coverCenter);
        Debug.Log(
            $"Story03 cover layout: table={coverTableBounds}, Deniz={denizCoverAnchor.position}, " +
            $"Can={canCoverAnchor.position}, separation={Vector3.Distance(denizCoverAnchor.position, canCoverAnchor.position):F3}");

        Transform canShoulderFocus = StoryChapterBuilderCommon.NewChild(family.can.transform, "Can_ShoulderFocus");
        Transform canChest = StoryChapterBuilderCommon.FindHumanoidBone(family.can, HumanBodyBones.UpperChest) ??
                             StoryChapterBuilderCommon.FindHumanoidBone(family.can, HumanBodyBones.Chest);
        canShoulderFocus.SetParent(canChest != null ? canChest : family.can.transform, false);
        canShoulderFocus.localPosition = canChest != null ? new Vector3(0.08f, 0.02f, 0f) : new Vector3(0f, 0.72f, 0f);
        Vector3 quakeCameraPosition = new Vector3(-3.35f, 2.18f, 3.25f);
        Vector3 canUpperBodyPoint = family.can.transform.position + Vector3.up * 1.02f;
        Vector3 calmCanProxyPosition = Vector3.Lerp(quakeCameraPosition, canUpperBodyPoint, 0.66f);
        GameObject calmCanSurface = CreateRebuildSurface(
            "Can_QuakeCalmTouchProxy",
            interactionRoot,
            calmCanProxyPosition,
            new Vector3(0.58f, 0.76f, 0.12f),
            true);
        calmCanSurface.transform.rotation = Quaternion.LookRotation(
            (quakeCameraPosition - calmCanProxyPosition).normalized,
            Vector3.up);
        refs.calmSibling = AddRebuildInteraction(
            calmCanSurface,
            "quake.calm.can",
            "CAN'IN OMZUNDA BASILI TUT — BENİMLE KAL",
            StoryInteractionKind.HelpSibling,
            calmCanSurface.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.QuakeClose,
            true,
            1,
            1.7f,
            2.5f);
        AttachInteractionMarker(refs.calmSibling, canShoulderFocus, 0.34f, 0.72f);
        GameObject crouchSurface = CreateRebuildSurface(
            "SafeTable_CrouchSurface",
            interactionRoot,
            // Masanın altındaki eski dokunma düzlemi ön ayağın arkasında kalıyordu.
            // Görünmez seçim hacmini tablanın hemen üstüne koy; rozet masayı işaret eder,
            // gerçek yaklaşma hedefi ise aşağıdaki zemin anchor'ı olarak kalır.
            new Vector3(coverTableBounds.center.x, coverTableBounds.max.y + 0.18f, insideZ),
            new Vector3(coverTableBounds.size.x * 0.7f, 0.24f, coverTableBounds.size.z * 0.58f),
            true);
        refs.crouchStep = AddRebuildInteraction(
            crouchSurface,
            "quake.cover.crouch",
            "1) ÇÖK — MASANIN GÜVENLİ TARAFINA GEÇ",
            StoryInteractionKind.TakeCover,
            coverApproachAnchor,
            StoryInteractionGesture.Approach,
            StoryCameraZoneId.UnderTable,
            false,
            1,
            2.2f,
            1.65f);
        Transform denizHeadFocus = StoryChapterBuilderCommon.NewChild(family.deniz.transform, "Deniz_HeadFocus");
        Transform denizHead = StoryChapterBuilderCommon.FindHumanoidBone(family.deniz, HumanBodyBones.Head);
        denizHeadFocus.SetParent(denizHead != null ? denizHead : family.deniz.transform, false);
        denizHeadFocus.localPosition = denizHead != null ? new Vector3(0f, 0.03f, 0.02f) : new Vector3(0f, 0.58f, 0.04f);
        refs.coverHeadStep = AddRebuildInteraction(
            family.deniz,
            "quake.cover.head",
            "2) KAPAN — DENİZ'İN BAŞINDA BASILI TUT",
            StoryInteractionKind.TakeCover,
            denizHeadFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.UnderTable,
            true,
            1,
            2.1f,
            2f);
        AttachInteractionMarker(refs.coverHeadStep, denizHeadFocus, 0.28f, 0.58f);
        GameObject gripSurface = CreateRebuildSurface(
            "SafeTable_GripLegSurface",
            interactionRoot,
            new Vector3(
                coverTableBounds.min.x + 0.18f,
                0.38f,
                coverTableBounds.min.z + 0.12f),
            new Vector3(0.4f, 0.82f, 0.4f),
            true);
        refs.safeCover = AddRebuildInteraction(
            gripSurface,
            "quake.cover.grip",
            "3) TUTUN — MASA AYAĞINDA BASILI TUT",
            StoryInteractionKind.TakeCover,
            gripSurface.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.UnderTable,
            true,
            1,
            8.5f,
            2f);
        AttachInteractionMarker(refs.safeCover, gripSurface.transform, 0.1f, 0.48f);
        // Sol ön ayak dikey kadrajın sınırına çok yakın; rozet ayağın kendisini kapatmadan
        // ekranda kalacak kadar içeri alınır. Etkileşim hacmi gerçek bacağın üzerinde kalır.
        refs.safeCover.HighlightRoot.transform.localPosition += Vector3.right * 0.34f;
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
        Vector3 postCanUpperBodyPoint = refs.postQuakeSafeReturn.position
                                        + Vector3.right * 0.9f
                                        + Vector3.up * 0.82f;
        Vector3 postCanProxyPosition = Vector3.Lerp(
            quakeCameraPosition,
            postCanUpperBodyPoint,
            0.66f);
        GameObject postCanSurface = CreateRebuildSurface(
            "Can_PostCheckTouchProxy",
            interactionRoot,
            postCanProxyPosition,
            new Vector3(0.62f, 0.82f, 0.12f),
            true);
        postCanSurface.transform.rotation = Quaternion.LookRotation(
            (quakeCameraPosition - postCanProxyPosition).normalized,
            Vector3.up);
        StoryInteractable checkCan = AddRebuildInteraction(
            postCanSurface,
            "quake.post.checkcan",
            "CAN'IN OMZUNDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            postCanSurface.transform,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.QuakeClose,
            true,
            1,
            2.6f);
        AttachInteractionMarker(checkCan, canShoulderFocus, 0.34f, 0.72f);
        GameObject glassTraceSurface = CreateRebuildSurface(
            "PostQuake_GlassTraceSurface",
            interactionRoot,
            new Vector3(world.glassFocus.position.x, 0.16f, world.glassFocus.position.z),
            new Vector3(1.55f, 0.26f, 1.35f),
            true);
        StoryInteractable inspectGlass = AddRebuildInteraction(
            glassTraceSurface,
            "quake.post.glass",
            "CAM SINIRINI PARMAĞINLA TAKİP ET",
            StoryInteractionKind.Inspect,
            glassTraceSurface.transform,
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
            StoryCameraZoneId.InspectExit,
            family.deniz.transform,
            new Vector3(1.05f, 0.5f, 1.05f));
        StoryInteractable canLaces = AddRebuildInteraction(
            world.canShoesWorld,
            "quake.post.canlaces",
            "CAN'IN BAĞCIKLARINDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            world.canShoesFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.InspectExit,
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
            StoryCameraZoneId.InspectExit,
            family.deniz.transform,
            new Vector3(1.2f, 1.45f, 1.05f));
        // All three entry tasks are performed from the clear floor in front of
        // the storage.  Using the shoe/bag transforms themselves sampled the
        // cabinet top as a valid NavMesh island and made Deniz climb the unit.
        SetInteractionPoint(shoes, entryStorageStand);
        SetInteractionPoint(canLaces, entryStorageStand);
        SetInteractionPoint(bag, entryStorageStand);
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
        Vector3 inspectTableCameraPosition = new Vector3(3.45f, 2.42f, -3.65f);
        Vector3 familyPlanProxyPosition = Vector3.Lerp(
            inspectTableCameraPosition,
            familyPlanBounds.center,
            0.64f);
        GameObject familyPlanSurface = CreateRebuildSurface(
            "FamilyPlan_PostRouteSurface",
            interactionRoot,
            familyPlanProxyPosition,
            new Vector3(0.72f, 0.54f, 0.12f),
            true);
        familyPlanSurface.transform.rotation = Quaternion.LookRotation(
            (inspectTableCameraPosition - familyPlanProxyPosition).normalized,
            Vector3.up);
        StoryInteractable familyPlan = AddRebuildInteraction(
            familyPlanSurface,
            "quake.post.familyplan",
            "AİLE PLANINDAKİ BULUŞMA NOKTASINI TAKİP ET",
            StoryInteractionKind.Inspect,
            familyPlanSurface.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.InspectTable,
            true,
            1,
            1.5f);
        AttachInteractionMarker(familyPlan, props.familyPlanCard.transform, 0.24f, 0.54f);
        // Yön doğrulama hedefi kadraj İÇİNDE, kapı yönünde yakın bir işaretçidir.
        // Odanın öbür ucundaki gerçek çıkış odağı InspectTable kadrajına girmiyordu.
        Vector3 planExitDirection = world.exitInspectFocus.position - familyPlanBounds.center;
        planExitDirection.y = 0f;
        Transform planSwipeCue = StoryChapterBuilderCommon.CreatePoint(
            "PlanRouteSwipeCue",
            interactionRoot,
            familyPlanBounds.center + planExitDirection.normalized * 0.85f,
            world.exitInspectFocus.position);
        StoryChapterBuilderCommon.SetGestureTarget(familyPlan, planSwipeCue);

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

        Transform flashlightPullTarget = StoryChapterBuilderCommon.NewChild(
            world.denizWornBag.transform,
            "Deniz_WornBag_FlashlightPullTarget");
        flashlightPullTarget.position = world.denizWornBag.transform.position
                                          + family.deniz.transform.right * 0.52f
                                          + Vector3.up * 0.06f;
        refs.lightWithFlashlight = AddRebuildInteraction(
            world.denizWornBag,
            "quake.post.flashlight",
            "ÇANTANIN DIŞ CEBİNDEN FENERİ YANA ÇEK",
            StoryInteractionKind.Collect,
            world.denizWornBag.transform,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.PostQuake,
            true);
        // Fener artık giriş rafında değil, takılmış çantanın dış cebinde. Hareket hedefini
        // de çantaya bağla; karakter yürüyüp döndüğünde ekranda kilometrelerce uzakta kalan
        // eski raf odağına değil, fiziksel olarak cebin hemen yanına sürüklenir.
        StoryChapterBuilderCommon.SetGestureTarget(refs.lightWithFlashlight, flashlightPullTarget);
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
            // Reflektör hattı koridordadır; InspectExit kadrajında yarısı kenardan
            // taşıyordu. Odak, hattı gerçekten gösteren koridor planına geçer.
            StoryCameraZoneId.Corridor,
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

        Transform canLeftHand = StoryChapterBuilderCommon.FindHumanoidBone(
            family.can,
            HumanBodyBones.LeftHand);
        if (canLeftHand == null)
            throw new InvalidOperationException("Can sol el kemiği bulunamadı.");
        Transform corridorHandFocus = StoryChapterBuilderCommon.NewChild(
            canLeftHand,
            "Can_CorridorHandFocus");
        corridorHandFocus.localPosition = new Vector3(-0.015f, 0f, 0.025f);
        GameObject corridorHandSurface = CreateRebuildSurface(
            "Can_CorridorHandSurface",
            corridorHandFocus,
            Vector3.zero,
            new Vector3(0.42f, 0.42f, 0.42f));
        StoryInteractable holdCan = AddRebuildInteraction(
            corridorHandSurface,
            "quake.corridor.hand",
            "CAN'IN ELİNDE BASILI TUT",
            StoryInteractionKind.HelpSibling,
            corridorHandFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.Corridor,
            true,
            1,
            2.2f);
        AttachInteractionMarker(holdCan, corridorHandFocus, 0.24f, 0.52f);
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
        GameObject aftershockSurface = CreateRebuildSurface(
            "Can_AftershockResponseSurface",
            canShoulderFocus,
            Vector3.zero,
            new Vector3(0.58f, 0.64f, 0.46f));
        StoryInteractable aftershock = AddRebuildInteraction(
            aftershockSurface,
            "quake.corridor.aftershock",
            "CAN'IN UYARISINA GÜVEN — YANINDA BASILI TUT",
            StoryInteractionKind.HelpSibling,
            canShoulderFocus,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.Corridor,
            true,
            1,
            3f);
        AttachInteractionMarker(aftershock, canShoulderFocus, 0.3f, 0.64f);
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

    private static void SnapRebuildNavigationAnchors(
        StoryChapterBuilderCommon.Characters family,
        InteractionReferences interactions)
    {
        SnapToBakedNavMesh(family.deniz.transform, "Deniz başlangıcı", 1.2f);
        SnapToBakedNavMesh(family.can.transform, "Can başlangıcı", 1.2f);
        SnapToBakedNavMesh(interactions.coverApproachAnchor, "masa cover yaklaşımı", 1.2f);
        SnapToBakedNavMesh(interactions.denizCoverAnchor, "Deniz masa cover pozu", 0.55f);
        SnapToBakedNavMesh(interactions.canCoverAnchor, "Can masa cover pozu", 0.55f);
        SnapToBakedNavMesh(interactions.postQuakeSafeReturn, "deprem sonrası dönüş", 1.2f);

        Vector3 denizToCan = interactions.canCoverAnchor.position - interactions.denizCoverAnchor.position;
        denizToCan.y = 0f;
        if (denizToCan.sqrMagnitude < 0.01f)
            throw new InvalidOperationException("Story 03 cover anchor'ları birbirinden ayrı değil.");
        interactions.denizCoverAnchor.rotation = Quaternion.LookRotation(denizToCan.normalized, Vector3.up);
        interactions.canCoverAnchor.rotation = Quaternion.LookRotation(-denizToCan.normalized, Vector3.up);
    }

    private static void ReframeCoverCamera(Transform root, InteractionReferences interactions)
    {
        CinemachineCamera coverCamera = root.GetComponentsInChildren<CinemachineCamera>(true)
            .SingleOrDefault(candidate => candidate.name == "CM03R_UnderTableTwoShot");
        if (coverCamera == null || interactions?.denizCoverAnchor == null || interactions.canCoverAnchor == null)
            throw new InvalidOperationException("Story 03 iki çocuklu cover kamerası/anchor'ları bulunamadı.");

        Vector3 midpoint = Vector3.Lerp(
            interactions.denizCoverAnchor.position,
            interactions.canCoverAnchor.position,
            0.5f) + Vector3.up * 0.42f;
        // The gripped front-left leg landed just outside x=0.10 on the narrower
        // 20:9 portrait profile.  Keep the two-child midpoint composition, with a
        // tiny left bias that brings the real hold surface into the safe frame.
        midpoint += Vector3.left * 0.08f;
        coverCamera.transform.rotation = Quaternion.LookRotation(
            (midpoint - coverCamera.transform.position).normalized,
            Vector3.up);
    }

    private static void SnapToBakedNavMesh(Transform anchor, string label, float radius)
    {
        if (anchor == null)
            throw new InvalidOperationException("Story 03 NavMesh anchor eksik: " + label);
        if (!NavMesh.SamplePosition(anchor.position, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            throw new InvalidOperationException(
                $"Story 03 '{label}' anchor'ı baked NavMesh üzerinde değil: {anchor.position}");
        }

        anchor.position = hit.position;
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

    private static void AttachInteractionMarker(
        StoryInteractable interaction,
        Transform followTarget,
        float verticalOffset,
        float scale)
    {
        if (interaction == null || interaction.HighlightRoot == null || followTarget == null)
            return;

        Transform marker = interaction.HighlightRoot.transform;
        marker.SetParent(followTarget, false);
        marker.localPosition = new Vector3(0f, verticalOffset, 0f);
        marker.localRotation = Quaternion.identity;
        marker.localScale = Vector3.one * scale;
    }

    private static void ConfigureRebuildReturnCamera(
        StoryInteractable interaction,
        StoryCameraZoneId returnZone,
        float lingerSeconds)
    {
        SerializedObject data = new SerializedObject(interaction);
        data.FindProperty("returnCameraAfterCompletion").boolValue = true;
        data.FindProperty("returnCameraZone").intValue = (int)returnZone;
        data.FindProperty("focusLingerSeconds").floatValue = lingerSeconds;
        data.ApplyModifiedPropertiesWithoutUndo();
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

    private static RebuildDestructionProps BuildRebuildDestructionSet(
        Transform quakeFxRoot,
        WorldReferences world,
        Materials materials)
    {
        Bounds tableBounds = GetRebuildBounds(world.safeTable);
        Bounds shelfBounds = GetRebuildBounds(world.shelfStable);
        RebuildDestructionProps props = new RebuildDestructionProps
        {
            familyPhoto = StoryChapterBuilderCommon.InstantiateFurniture(
                "Decorations/Picture_17.prefab",
                "Quake_FallingFamilyPhoto",
                quakeFxRoot,
                new Vector3(-2.28f, 1.92f, 5.55f),
                new Vector3(0.94f, 0.78f, 0.11f),
                new Vector3(0f, 180f, 0f),
                false),
            looseBookA = StoryChapterBuilderCommon.InstantiateFurniture(
                "Decorations/Book_03.prefab",
                "Quake_LooseShelfBook_A",
                quakeFxRoot,
                new Vector3(shelfBounds.center.x, shelfBounds.max.y + 0.012f, shelfBounds.center.z - 0.22f),
                new Vector3(0.28f, 0.11f, 0.38f),
                new Vector3(0f, 82f, 0f),
                false),
            looseBookB = StoryChapterBuilderCommon.InstantiateFurniture(
                "Decorations/Book_08.prefab",
                "Quake_LooseShelfBook_B",
                quakeFxRoot,
                new Vector3(shelfBounds.center.x, shelfBounds.max.y + 0.012f, shelfBounds.center.z + 0.23f),
                new Vector3(0.3f, 0.1f, 0.4f),
                new Vector3(0f, 96f, 0f),
                false),
            plant = StoryChapterBuilderCommon.InstantiateFurniture(
                "Plants/Plants_15.prefab",
                "Quake_TopplingLivingPlant",
                quakeFxRoot,
                new Vector3(-4.18f, 0f, 4.42f),
                new Vector3(0.72f, 1.22f, 0.72f),
                new Vector3(0f, 18f, 0f),
                false),
            mug = StoryAuthoredPropFactory.CreateCeramicMug(
                "Quake_FallingTableMug",
                quakeFxRoot,
                new Vector3(tableBounds.center.x - 0.72f, tableBounds.max.y + 0.012f,
                    tableBounds.center.z + 0.42f),
                new Vector3(0.2f, 0.26f, 0.2f),
                new Vector3(0f, -18f, 0f),
                materials.cream,
                materials.navy,
                false)
        };

        props.windowCrack = new GameObject("Quake_WindowCrackReveal");
        props.windowCrack.transform.SetParent(quakeFxRoot);
        props.windowCrack.transform.SetPositionAndRotation(
            new Vector3(-2.62f, 2.38f, 5.61f),
            Quaternion.identity);
        foreach ((string name, Vector3 offset, Vector3 size, float roll) in new[]
                 {
                     ("Crack_Spine", Vector3.zero, new Vector3(0.035f, 1.08f, 0.022f), 16f),
                     ("Crack_Branch_A", new Vector3(-0.23f, 0.22f, 0f), new Vector3(0.03f, 0.58f, 0.02f), -46f),
                     ("Crack_Branch_B", new Vector3(0.3f, -0.04f, 0f), new Vector3(0.03f, 0.68f, 0.02f), 61f),
                     ("Crack_Branch_C", new Vector3(-0.35f, -0.31f, 0f), new Vector3(0.026f, 0.44f, 0.018f), 53f)
                 })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                name,
                PrimitiveType.Cube,
                props.windowCrack.transform.position + offset,
                size,
                materials.navy,
                props.windowCrack.transform,
                false,
                Quaternion.Euler(0f, 0f, roll));
        }
        props.windowCrack.transform.localScale = Vector3.zero;

        BuildPersistentPostQuakeRoomDamage(world.brokenGlassVisual, materials);
        return props;
    }

    private static void BuildPersistentPostQuakeRoomDamage(GameObject brokenGlassRoot, Materials materials)
    {
        if (brokenGlassRoot == null)
            throw new ArgumentNullException(nameof(brokenGlassRoot));

        bool wasActive = brokenGlassRoot.activeSelf;
        brokenGlassRoot.SetActive(true);
        Transform damage = StoryChapterBuilderCommon.NewChild(
            brokenGlassRoot.transform,
            "Story03_PostQuakeRoomDamage");
        StoryChapterBuilderCommon.CreatePrimitive(
            "PostQuake_FallenPhotoFrame",
            PrimitiveType.Cube,
            new Vector3(-2.03f, 0.045f, 4.88f),
            new Vector3(0.9f, 0.07f, 0.68f),
            materials.wood,
            damage,
            false,
            Quaternion.Euler(4f, -18f, 7f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "PostQuake_FallenPhotoGlass",
            PrimitiveType.Cube,
            new Vector3(-2.03f, 0.086f, 4.88f),
            new Vector3(0.72f, 0.018f, 0.5f),
            materials.glass,
            damage,
            false,
            Quaternion.Euler(4f, -18f, 7f));
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_15.prefab",
            "PostQuake_ToppledLivingPlant",
            damage,
            new Vector3(-4.12f, 0.04f, 4.14f),
            new Vector3(1.12f, 0.68f, 0.72f),
            new Vector3(0f, 22f, -74f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_03.prefab",
            "PostQuake_ScatteredBook_A",
            damage,
            new Vector3(3.68f, 0.025f, 1.2f),
            new Vector3(0.42f, 0.1f, 0.3f),
            new Vector3(12f, 34f, 9f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_08.prefab",
            "PostQuake_ScatteredBook_B",
            damage,
            new Vector3(4.08f, 0.03f, 0.86f),
            new Vector3(0.4f, 0.11f, 0.29f),
            new Vector3(-8f, -21f, 14f),
            false);
        for (int index = 0; index < 7; index++)
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "PostQuake_MugShard_" + (index + 1),
                PrimitiveType.Cube,
                new Vector3(-0.68f + index * 0.11f, 0.035f + (index % 2) * 0.012f,
                    0.56f - index * 0.045f),
                new Vector3(0.07f + (index % 3) * 0.018f, 0.025f, 0.055f),
                index % 2 == 0 ? materials.cream : materials.navy,
                damage,
                false,
                Quaternion.Euler(index * 13f, index * 31f, index % 2 == 0 ? 18f : -22f));
        }
        brokenGlassRoot.SetActive(wasActive);
    }

    private static PlayableDirector BuildRebuildEarthquakeTimeline(
        Transform parent,
        Materials materials,
        WorldReferences world,
        RebuildProps props,
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
        RebuildDestructionProps destruction = BuildRebuildDestructionSet(
            fxRoot.transform,
            world,
            materials);
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

        BindRebuildActivation(
            timeline,
            director,
            props.denizFearFace,
            "00 • Deniz korku ifadesi",
            RebuildQuakeDuration);
        BindRebuildActivation(
            timeline,
            director,
            props.canFearFace,
            "00 • Can korku ifadesi",
            RebuildQuakeDuration);
        BindRebuildLoop(
            timeline,
            director,
            world.environment,
            "00 • Tüm oda ve koridor yapı titreşimi",
            CreateRattleClip(
                "Story03Rebuild_WholeHome_Rumble",
                world.environment.transform,
                0.032f,
                0.024f,
                0.52f));

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
        BindRebuildOneShot(
            timeline,
            director,
            destruction.familyPhoto,
            "02 • Aile fotoğrafı düşüşü",
            CreateImpactFallClip(
                "Story03Rebuild_FamilyPhoto_Fall",
                destruction.familyPhoto.transform,
                new Vector3(0.28f, -1.82f, -0.34f),
                new Vector3(32f, 12f, 84f),
                1.25f),
            3.6d);
        BindRebuildOneShot(
            timeline,
            director,
            destruction.looseBookA,
            "02 • Raf kitabı A düşüşü",
            CreateImpactFallClip(
                "Story03Rebuild_BookA_Fall",
                destruction.looseBookA.transform,
                new Vector3(-0.48f, -1.62f, -0.56f),
                new Vector3(78f, 96f, 112f),
                1.05f),
            8.1d);
        BindRebuildOneShot(
            timeline,
            director,
            destruction.looseBookB,
            "02 • Raf kitabı B düşüşü",
            CreateImpactFallClip(
                "Story03Rebuild_BookB_Fall",
                destruction.looseBookB.transform,
                new Vector3(0.36f, -1.58f, -0.48f),
                new Vector3(62f, -108f, -88f),
                1.18f),
            11.2d);
        BindRebuildOneShot(
            timeline,
            director,
            destruction.mug,
            "03 • Kupa düşüşü ve kırılması",
            CreateImpactFallClip(
                "Story03Rebuild_Mug_Fall",
                destruction.mug.transform,
                new Vector3(-0.62f, -0.86f, -0.34f),
                new Vector3(168f, 82f, 126f),
                0.9f),
            14.4d);
        BindRebuildOneShot(
            timeline,
            director,
            destruction.plant,
            "03 • Salon bitkisi devrilmesi",
            CreateImpactFallClip(
                "Story03Rebuild_Plant_Topple",
                destruction.plant.transform,
                new Vector3(-0.26f, 0f, -0.22f),
                new Vector3(0f, -12f, -72f),
                1.48f),
            18.2d);
        BindRebuildOneShot(
            timeline,
            director,
            destruction.windowCrack,
            "03 • Pencere çatlağı görünümü",
            CreateScaleRevealClip(
                "Story03Rebuild_WindowCrack_Reveal",
                0.22f),
            24.6d);
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
        AddRebuildAudioCue(detailTrack, "sfx100v2_glass_06.ogg", 14.45d);
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
        GameObject target,
        string trackName,
        AnimationClip clip)
    {
        if (target == null || clip == null)
            return;

        // Unity can return a destroyed-object "fake null" here while editor-authored assets are
        // being reimported. The overloaded equality check catches it; C#'s ?? operator does not.
        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
            animator = target.AddComponent<Animator>();
        BindRebuildLoop(timeline, director, animator, trackName, clip);
    }

    private static void BindRebuildLoop(
        TimelineAsset timeline,
        PlayableDirector director,
        Animator animator,
        string trackName,
        AnimationClip clip)
    {
        if (animator == null || clip == null)
            return;

        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.duration = RebuildQuakeDuration;
        if (motion.asset is AnimationPlayableAsset playable)
            playable.loop = AnimationPlayableAsset.LoopMode.On;
        director.SetGenericBinding(track, animator);
        // Creating the next external .anim asset can force an AssetDatabase import while this
        // Timeline is still being assembled. Persist each loop track immediately so Unity does
        // not reload the playable and silently drop the just-authored room-motion channel.
        EditorUtility.SetDirty(track);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssetIfDirty(timeline);
    }

    private static void BindRebuildOneShot(
        TimelineAsset timeline,
        PlayableDirector director,
        GameObject target,
        string trackName,
        AnimationClip clip,
        double start)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
            animator = target.AddComponent<Animator>();
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.start = start;
        motion.duration = clip.length;
        director.SetGenericBinding(track, animator);
    }

    private static void BindRebuildActivation(
        TimelineAsset timeline,
        PlayableDirector director,
        GameObject target,
        string trackName,
        double duration)
    {
        if (timeline == null || director == null || target == null)
            return;
        target.SetActive(false);
        ActivationTrack track = timeline.CreateTrack<ActivationTrack>(null, trackName);
        track.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
        TimelineClip clip = track.CreateDefaultClip();
        clip.duration = duration;
        director.SetGenericBinding(track, target);
    }

    private static AnimationClip CreateImpactFallClip(
        string name,
        Transform target,
        Vector3 localOffset,
        Vector3 eulerOffset,
        float duration)
    {
        AnimationClip clip = CreateAnimationAsset(name);
        clip.wrapMode = WrapMode.ClampForever;
        Vector3 startPosition = target.localPosition;
        Vector3 endPosition = startPosition + localOffset;
        Vector3 startEuler = target.localEulerAngles;
        Vector3 endEuler = startEuler + eulerOffset;
        float anticipation = Mathf.Min(0.16f, duration * 0.18f);
        float impact = duration * 0.72f;

        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.x", new AnimationCurve(
            new Keyframe(0f, startPosition.x),
            new Keyframe(anticipation, startPosition.x),
            new Keyframe(impact, Mathf.Lerp(startPosition.x, endPosition.x, 0.84f)),
            new Keyframe(duration, endPosition.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.y", new AnimationCurve(
            new Keyframe(0f, startPosition.y),
            new Keyframe(anticipation, startPosition.y + 0.025f),
            new Keyframe(impact, Mathf.Lerp(startPosition.y, endPosition.y, 0.9f)),
            new Keyframe(duration, endPosition.y)));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.z", new AnimationCurve(
            new Keyframe(0f, startPosition.z),
            new Keyframe(anticipation, startPosition.z),
            new Keyframe(impact, Mathf.Lerp(startPosition.z, endPosition.z, 0.82f)),
            new Keyframe(duration, endPosition.z)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(
            new Keyframe(0f, startEuler.x),
            new Keyframe(anticipation, startEuler.x),
            new Keyframe(duration, endEuler.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(
            new Keyframe(0f, startEuler.y),
            new Keyframe(anticipation, startEuler.y),
            new Keyframe(duration, endEuler.y)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(
            new Keyframe(0f, startEuler.z),
            new Keyframe(anticipation, startEuler.z),
            new Keyframe(duration, endEuler.z)));
        return clip;
    }

    private static AnimationClip CreateScaleRevealClip(string name, float duration)
    {
        AnimationClip clip = CreateAnimationAsset(name);
        clip.wrapMode = WrapMode.ClampForever;
        AnimationCurve reveal = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(duration * 0.22f, 0f),
            new Keyframe(duration * 0.72f, 1.08f),
            new Keyframe(duration, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.x", reveal);
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.y", new AnimationCurve(reveal.keys));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.z", new AnimationCurve(reveal.keys));
        return clip;
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
        // The former 46-second minimum left the player watching subtitles after the first short
        // hold. The playable actions now occupy most of a tighter 22-second quake beat.
        serialized.FindProperty("quakeMinimumDuration").floatValue = 22f;
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
            Require(
                StoryChapterBuilderCommon.HasCanonicalStory01HomeShell(sharedHome.transform),
                "Story 01 ile aynı kanonik ev kabuğu");
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
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 52f,
                    camera.name + " lens aralığı");
            }
            CinemachineCamera coverCamera = cameras.SingleOrDefault(
                candidate => candidate.name == "CM03R_UnderTableTwoShot");
            Require(coverCamera != null, "iki çocuklu masa koruma kamerası");
            Require(coverCamera.transform.position.y >= 0.62f && coverCamera.transform.position.y <= 0.95f,
                "masa koruma kamerasının tabla altında iki yüzü de gösteren çocuk hizasında olması");

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
