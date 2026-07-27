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
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

public static class StoryPreparationRebuildPreviewBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_01_RebuildPreview.unity";

    private const string SharedHomePath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";
    private const string ItemRoot = "Assets/Bolum1Prefab";
    private const string OriginalOpenBagPath = "Assets/Sprites/Bolum1/Open Backpack/model.obj";
    private const string ClosedBackpackPath = "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Backpack.fbx";
    private const string SyntyKitchenCounterPath =
        "Assets/PolygonTown/Models/Props/SM_Prop_Kitchen_Counter_01.fbx";
    private const string SyntyTownMaterialPath =
        "Assets/PolygonTown/Materials/PolygonTown_01_A.mat";
    private const string QuaterniusFirstAidPath =
        "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/FirstAidKit.fbx";
    private const string QuaterniusBandagesPath =
        "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Bandages.fbx";
    private const string KenneyBedrollPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx";
    private const string KenneyChestPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/chest.fbx";
    private const string KenneyOpenBoxPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/box-open.fbx";
    private const string KenneySurvivalMaterialPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Materials/KenneySurvival_Atlas.mat";
    private const string PlanSocketMaterialPath =
        "Assets/Story/Generated/Materials/Story01_PlanSocketGhost.mat";
    private const string PlanCardSnapClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_PlanCardSnap.anim";
    private const string PlanSocketPulseClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_PlanSocketPulse.anim";
    private const string SignalDrawerOpenClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim";
    private const string SignalStageClipPrefix =
        "Assets/Story/Generated/Animations/Chapters/Story01_SignalStage_";

    private sealed class PreviewWorld
    {
        internal GameObject environment;
        internal GameObject sharedHome;
        internal GameObject openBag;
        internal GameObject wornBag;
        internal GameObject exitBag;
        internal BagDropZone bagDropZone;
        internal Transform bagOpening;
        internal Transform bagPackedRoot;
        internal GameObject familyPlanComplete;
        internal GameObject familyContactComplete;
        internal GameObject familyRoleComplete;
        internal GameObject wrongChoiceConsequence;
        internal GameObject signalNightstandClosed;
        internal GameObject signalDrawerOpen;
        internal Transform signalDrawerContent;
        internal GameObject foodCabinetClosed;
        internal GameObject foodCabinetDoorLeft;
        internal GameObject foodCabinetDoorRight;
        internal GameObject healthCabinetClosed;
        internal GameObject healthDrawerOpen;
        internal GameObject healthKitDisplay;
        internal GameObject warmthChestClosed;
        internal GameObject warmthChestOpen;
        internal GameObject signalTestLight;
        internal GameObject foodPocketOpen;
        internal GameObject foodPocketClosed;
        internal GameObject healthSealOpen;
        internal GameObject healthSealClosed;
        internal GameObject warmthZipperOpen;
        internal GameObject warmthZipperClosed;
        internal GameObject radioTuningBefore;
        internal GameObject radioTuningAfter;
        internal GameObject radioReviewProp;
        internal GameObject whistleOnCan;
        internal GameObject whistleCanTarget;
        internal GameObject consoleInBag;
        internal GameObject consoleReturned;
    }

    private sealed class PlayableInteractions
    {
        internal StoryInteractable startFamilyPlan;
        internal StoryInteractable placeContactCard;
        internal StoryInteractable assignCanWhistleRole;
        internal StoryInteractable inspectBag;
        internal StoryInteractable discoverSignal;
        internal StoryInteractable discoverFood;
        internal StoryInteractable discoverHealth;
        internal StoryInteractable discoverWarmth;
        internal StoryInteractable inspectWaterDate;
        internal StoryInteractable inspectBandageSeal;
        internal StoryInteractable reviewSignal;
        internal StoryInteractable reviewSignalFlashlightOff;
        internal StoryInteractable reviewSignalRadio;
        internal StoryInteractable reviewSignalWhistle;
        internal Transform signalFlashlightApproachPoint;
        internal StoryInteractable reviewFood;
        internal StoryInteractable reviewHealth;
        internal StoryInteractable reviewWarmth;
        internal StoryInteractable comfortItem;
        internal StoryInteractable testBagWeight;
        internal StoryInteractable removeConsole;
        internal StoryInteractable testBalancedBag;
        internal StoryInteractable adjustBagStraps;
        internal StoryInteractable placeBagAtExit;
        internal StoryPreparationItem[] items;
        internal StoryInteractable[] signalDrawerItems;
    }

    private readonly struct PreviewItem
    {
        internal readonly string id;
        internal readonly string prefab;
        internal readonly string displayName;
        internal readonly Vector3 position;
        internal readonly Vector3 size;
        internal readonly Vector3 euler;
        internal readonly StoryCameraZoneId cameraZone;
        internal readonly bool recommended;

        internal PreviewItem(string id, string prefab, string displayName, Vector3 position, Vector3 size, float yaw,
            StoryCameraZoneId cameraZone, bool recommended = true)
        {
            this.id = id;
            this.prefab = prefab;
            this.displayName = displayName;
            this.position = position;
            this.size = size;
            euler = new Vector3(0f, yaw, 0f);
            this.cameraZone = cameraZone;
            this.recommended = recommended;
        }

        internal PreviewItem(string id, string prefab, string displayName, Vector3 position, Vector3 size,
            Vector3 euler, StoryCameraZoneId cameraZone, bool recommended = true)
        {
            this.id = id;
            this.prefab = prefab;
            this.displayName = displayName;
            this.position = position;
            this.size = size;
            this.euler = euler;
            this.cameraZone = cameraZone;
            this.recommended = recommended;
        }
    }

    [MenuItem("Tools/Deprem Story/Build Story_01 Rebuild Preview")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Tools/Deprem Story/Build Story_01 Rebuild Preview (Silent)")]
    public static void BuildSilentFromMenu()
    {
        Build(false);
    }

    [MenuItem("Tools/Deprem Story/QA/Stop Play Mode")]
    public static void StopPlayModeFromMenu()
    {
        EditorApplication.isPlaying = false;
    }

    public static void Build(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Story 01 rebuild önizlemesi Play Mode dışında üretilmelidir.");

        try
        {
            RequireAsset(SharedHomePath);
            StoryChapterBuilderCommon.EnsureFolders();

            RuntimeAnimatorController childController = StoryAnimationLibraryBuilder.BuildLibrary(false);
            RuntimeAnimatorController adultController = StoryAnimationLibraryBuilder.LoadAdultController();
            StoryChapterBuilderCommon.Materials materials = LoadExistingMaterials();
            UnityEngine.Rendering.VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_01_REBUILD_PREVIEW");

            PreviewWorld world = BuildWorld(root.transform, materials);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                true,
                new Vector3(-0.8f, 0f, -2.5f),
                new Vector3(-2.25f, 0f, -3.15f),
                new Vector3(1.65f, 0f, 3.55f),
                adultController);
            BuildFinalBagStates(root.transform, world, family.deniz.transform);

            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);
            StoryCameraController cameraController = BuildCameras(root.transform, family.deniz.transform,
                out Camera mainCamera, out CinemachineBrain brain);
            StoryChapterBuilderCommon.ChapterUI ui = StoryChapterBuilderCommon.BuildUI(
                root.transform,
                cameraController,
                "Story01PreviewCanvas",
                "1. PERDE • HAZIRLIK",
                "AFET ÇANTASI HAZIR",
                "Çantayı sarsıntı sırasında değil, sarsıntı durduktan sonra çıkarken al.",
                StoryAct.Preparation);
            StoryChapterBuilderCommon.ConfigureDialogueVoices(
                ui.controller,
                "Assets/Story/Audio/Voices/Story01/manifest.json",
                "Assets/Story/Audio/Voices/Story01");
            StoryChapterBuilderCommon.SetReference(ui.controller, "movementOwner", movement);
            ConfigurePreviewText(root.transform);

            GameObject core = new GameObject("_Story01PreviewCore");
            core.transform.SetParent(root.transform);
            StoryPreparationDirector storyDirector = core.AddComponent<StoryPreparationDirector>();
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", ui.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchData = new SerializedObject(touch);
            touchData.FindProperty("directWorldGestures").boolValue = true;
            touchData.FindProperty("worldSwipeThreshold").floatValue = 52f;
            touchData.ApplyModifiedPropertiesWithoutUndo();

            GameObject session = new GameObject("_StorySession");
            StoryGameManager gameManager = session.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(gameManager, StoryAct.Preparation, Array.Empty<StoryFlag>());
            StoryChapterBuilderCommon.ConfigureRebuildStoryRoute(gameManager);

            PlayableInteractions interactions = BuildDirectWorldInteractions(
                root.transform,
                world,
                family,
                storyDirector,
                materials);
            ConfigureStoryDirector(
                storyDirector,
                gameManager,
                movement,
                touch,
                cameraController,
                ui,
                family,
                world,
                interactions);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(ui.controller, "cameraController", cameraController);

            StoryChapterBuilderCommon.BuildLighting(root.transform, volume, new Color(1f, 0.88f, 0.72f), 1.32f);
            BuildBlackoutDrillTimeline(
                root.transform,
                storyDirector,
                touch,
                ui.controller,
                interactions.placeBagAtExit,
                world,
                family);
            AudioClip ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(
                StoryChapterBuilderCommon.AudioRoot + "/calm_home.wav");
            if (ambience != null)
                StoryChapterBuilderCommon.CreateAudioSource(
                    "PreparationHomeAmbience", root.transform, ambience, 0.14f, true, true);
            StoryChapterBuilderCommon.CreateLicensedAmbience(
                "PreparationRoomTone",
                root.transform,
                "sfx100v2_loop_ambient_01.ogg",
                0.045f);
            StoryChapterBuilderCommon.AttachInteractionAudioLayer(
                root.transform,
                "Story01_ObjectInteractionAudio");
            StoryChapterBuilderCommon.DisableShadows(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            StoryChapterBuilderCommon.BuildNavigation(world.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            StoryPreparationRebuildPreviewValidator.Validate(false);
            Selection.activeGameObject = root;
            Debug.Log("Story_01_RebuildPreview üretildi: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Ortak ev kullanan Story 01 rebuild önizlemesi üretildi.\n" +
                    "Mevcut Story_01_BagPreparation sahnesi değiştirilmedi.",
                    "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 01 rebuild önizlemesi üretilemedi:\n" + exception.Message,
                    "Kapat");
            throw;
        }
    }

    private static PreviewWorld BuildWorld(Transform parent, StoryChapterBuilderCommon.Materials materials)
    {
        PreviewWorld world = new PreviewWorld();
        world.environment = new GameObject("PreparationEnvironment");
        world.environment.transform.SetParent(parent);

        GameObject sharedHomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHomePath);
        world.sharedHome = (GameObject)PrefabUtility.InstantiatePrefab(sharedHomePrefab, world.environment.transform);
        world.sharedHome.name = "StoryHome_Shared";
        world.sharedHome.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        world.sharedHome.transform.localScale = Vector3.one;
        ConfigurePreparationHomeState(world.sharedHome.transform);
        NormalizePreparationProportions(world.sharedHome.transform);

        Transform dressing = StoryChapterBuilderCommon.NewChild(world.environment.transform, "PreparationSetDressing");
        BuildFamilyPlanBoard(dressing, materials, world);
        BuildDiscoveryFurniture(dressing, materials, world);
        BuildOpenBag(dressing, materials, world);
        BuildWrongChoiceConsequence(dressing, materials, world);
        return world;
    }

    private static StoryChapterBuilderCommon.Materials LoadExistingMaterials()
    {
        return new StoryChapterBuilderCommon.Materials
        {
            wall = LoadMaterial("Chapter_Wall"),
            floor = LoadMaterial("Chapter_WarmFloor"),
            concrete = LoadMaterial("Chapter_Concrete"),
            asphalt = LoadMaterial("Chapter_Asphalt"),
            grass = LoadMaterial("Chapter_Grass"),
            cream = LoadMaterial("Cream"),
            navy = LoadMaterial("Navy"),
            teal = LoadMaterial("Teal"),
            amber = LoadMaterial("Amber"),
            coral = LoadMaterial("Coral"),
            wood = LoadMaterial("Wood"),
            metal = LoadMaterial("Chapter_Metal"),
            glass = LoadMaterial("WindowGlass"),
            dust = LoadMaterial("Chapter_Dust"),
            dark = LoadMaterial("Chapter_Dark"),
            white = LoadMaterial("Chapter_White")
        };
    }

    private static Material LoadMaterial(string name)
    {
        string path = StoryChapterBuilderCommon.MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new InvalidOperationException("Hazır Story materyali bulunamadı: " + path);
        return material;
    }

    private static Material GetOrCreatePlanSocketMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PlanSocketMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader bulunamadı.");
            material = new Material(shader) { name = "Story01_PlanSocketGhost" };
            AssetDatabase.CreateAsset(material, PlanSocketMaterialPath);
        }

        Color mint = new Color32(91, 211, 151, 255);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", mint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", mint);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip GetOrCreatePlanCardSnapClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlanCardSnapClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_PlanCardSnap" };
            AssetDatabase.CreateAsset(clip, PlanCardSnapClipPath);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        const float duration = 0.68f;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.22f, 0.024f),
                new Keyframe(0.4f, -0.012f),
                new Keyframe(0.55f, 0.006f),
                new Keyframe(duration, 0f)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            new AnimationCurve(
                new Keyframe(0f, 0.22f),
                new Keyframe(0.22f, 0.022f),
                new Keyframe(0.4f, 0.052f),
                new Keyframe(0.55f, 0.012f),
                new Keyframe(duration, 0f)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, 6f),
                new Keyframe(0.22f, -3.2f),
                new Keyframe(0.4f, 1.6f),
                new Keyframe(0.55f, -0.6f),
                new Keyframe(duration, 0f)));
        AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0f, 1.07f),
            new Keyframe(0.22f, 0.98f),
            new Keyframe(0.4f, 1.02f),
            new Keyframe(0.55f, 0.995f),
            new Keyframe(duration, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", scaleCurve);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.y", scaleCurve);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.z", scaleCurve);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip GetOrCreatePlanSocketPulseClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlanSocketPulseClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_PlanSocketPulse" };
            AssetDatabase.CreateAsset(clip, PlanSocketPulseClipPath);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Loop;
        const float duration = 1.6f;
        AnimationCurve scale = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(duration * 0.5f, 1.035f),
            new Keyframe(duration, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", scale);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.y", scale);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.z",
            AnimationCurve.Constant(0f, duration, 1f));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip GetOrCreateSignalDrawerOpenClip(float closedZ, float openZ)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SignalDrawerOpenClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_SignalDrawerOpen" };
            AssetDatabase.CreateAsset(clip, SignalDrawerOpenClipPath);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.z",
            new AnimationCurve(
                new Keyframe(0f, closedZ),
                new Keyframe(0.08f, closedZ + 0.025f),
                new Keyframe(0.38f, openZ + 0.025f),
                new Keyframe(0.52f, openZ)));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip GetOrCreateSignalStageClip(
        string itemId,
        Vector3 drawerPosition,
        Vector3 tablePosition)
    {
        string path = SignalStageClipPrefix + itemId + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_SignalStage_" + itemId };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        const float duration = 0.68f;
        float liftY = Mathf.Max(drawerPosition.y, tablePosition.y) + 0.34f;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            new AnimationCurve(
                new Keyframe(0f, drawerPosition.x),
                new Keyframe(0.3f, Mathf.Lerp(drawerPosition.x, tablePosition.x, 0.48f)),
                new Keyframe(duration, tablePosition.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y",
            new AnimationCurve(
                new Keyframe(0f, drawerPosition.y),
                new Keyframe(0.3f, liftY),
                new Keyframe(duration, tablePosition.y)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            new AnimationCurve(
                new Keyframe(0f, drawerPosition.z),
                new Keyframe(0.3f, Mathf.Lerp(drawerPosition.z, tablePosition.z, 0.48f)),
                new Keyframe(duration, tablePosition.z)));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void NormalizePreparationProportions(Transform home)
    {
        Transform safeTable = StorySharedHomePrefabBuilder.FindDescendant(home, "SafeTable");
        if (safeTable != null)
        {
            Vector3 scale = safeTable.localScale;
            safeTable.localScale = new Vector3(scale.x, scale.y * 0.74f, scale.z);
        }
    }

    private static void ConfigurePreparationHomeState(Transform home)
    {
        SetActive(home, "Wardrobe_Secured", false);
        SetActive(home, "Wardrobe_Unsecured", true);
        SetActive(home, "Wardrobe_Fallen", false);
        SetActive(home, "Shelf_Secured", false);
        SetActive(home, "Shelf_Unsecured", true);
        SetActive(home, "Shelf_Fallen", false);
        SetActive(home, "Door", true);
        SetActive(home, "Door_Open", false);
        SetActive(home, "ExitRoute_Cleared", false);
        SetActive(home, "ExitRoute_ClutteredButPassable", false);
        SetActive(home, "Shoes_PostQuake", false);
        SetActive(home, "BrokenGlass_Hazard", false);
        SetActive(home, "EmergencyBag", false);
        SetActive(home, "HangingLamp_QuakeMotion", false);
        SetActive(home, "LooseProps_QuakeMotion", false);
        SetActive(home, "FamilyBoardGame", false);
        SetActive(home, "LowCabinet", false);

        // Story 03'te pencerenin solunda çalışan dolap yerleşimi, Story 01'in
        // daha geniş hazırlık kadrajında perde/camla üst üste biniyordu. Üç
        // varyantı da aynı temiz duvar cebine taşı; aktif varyant değişse bile
        // mobilya hiçbir zaman pencerenin içine girmesin.
        MoveHomeObject(home, "Wardrobe_Secured", new Vector3(1.15f, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "Wardrobe_Unsecured", new Vector3(1.15f, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "Wardrobe_Fallen", new Vector3(1.15f, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "WardrobeFocus", new Vector3(1.15f, 0.1f, 4.75f), Vector3.zero);

        // Story 01 is viewed from the open front of the room. The shared quake set
        // uses a waist-high right wall for its exterior camera, which makes the
        // preparation room read as an unfinished platform and exposes the back of
        // the tall shelf. Complete that side wall here; all Story 01 cameras stay
        // inside it, so the room remains visible without looking structurally open.
        Transform rightWall = StorySharedHomePrefabBuilder.FindDescendant(home, "LowRightWall");
        if (rightWall != null)
        {
            rightWall.name = "RightWall_Story01";
            rightWall.localPosition = new Vector3(5f, 1.7f, 0.25f);
            rightWall.localRotation = Quaternion.identity;
            rightWall.localScale = new Vector3(0.22f, 3.4f, 11.5f);
        }

        // Preserve the authored wall-decor corrections made in Story 01. The large
        // frame asset's front faces the wall at its import rotation, and the small
        // frame otherwise overlaps the family emergency-plan board.
        SetHomeObjectLocalTransform(
            home,
            "ArtFrameLarge",
            new Vector3(4.179999f, 2.08f, 5.898f),
            new Vector3(0f, 180f, 0f),
            Vector3.one * 1.83469164f);
        SetHomeObjectLocalTransform(
            home,
            "SmallFrame",
            new Vector3(-4.85f, 1.91999984f, -2.484f),
            new Vector3(0f, 90f, 0f),
            Vector3.one * 1.038648f);

        // Keep the decorative plant in its own wall pocket. It must not read as
        // another objective or grow out of the warmth storage prop.
        PlaceHomeObjectOnFloor(home, "RoomPlant", new Vector2(-4.25f, -0.45f));
    }

    private static void BuildFamilyPlanBoard(Transform parent, StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        const float cardCenterY = -0.2f;
        const float cardCenterZ = 0.245f;
        Material socketMaterial = GetOrCreatePlanSocketMaterial();
        AnimationClip snapClip = GetOrCreatePlanCardSnapClip();
        AnimationClip socketPulseClip = GetOrCreatePlanSocketPulseClip();
        Transform board = StoryChapterBuilderCommon.NewChild(parent, "FamilyPlanBoard");
        board.SetPositionAndRotation(new Vector3(-4.82f, 2.02f, 1.15f), Quaternion.Euler(0f, 90f, 0f));
        GameObject boardBorder = StoryChapterBuilderCommon.CreatePrimitive(
            "BoardBorder", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.94f, 1.72f, 0.12f), materials.navy, board, true);
        boardBorder.transform.localPosition = new Vector3(0f, 0f, -0.025f);
        boardBorder.transform.localRotation = Quaternion.identity;
        GameObject boardBody = StoryChapterBuilderCommon.CreatePrimitive(
            "BoardSurface", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.78f, 1.56f, 0.10f), materials.cream, board, false);
        boardBody.transform.localPosition = new Vector3(0f, 0f, 0.045f);
        boardBody.transform.localRotation = Quaternion.identity;

        GameObject header = StoryChapterBuilderCommon.CreatePrimitive(
            "PlanHeader", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.58f, 0.34f, 0.055f), materials.navy, board, false);
        header.transform.localPosition = new Vector3(0f, 0.52f, 0.12f);
        header.transform.localRotation = Quaternion.identity;
        CreateBoardLabel(board, "PlanHeaderText", "AİLE AFET PLANI",
            new Vector3(0f, 0.52f, 0.16f), new Vector2(2.38f, 0.27f), 1.05f, StoryChapterBuilderCommon.Cream);
        CreateBoardLabel(board, "PlanSubheaderText", "KARTLARI BAŞLIKLARLA EŞLEŞTİR",
            new Vector3(0f, 0.29f, 0.16f), new Vector2(2.4f, 0.16f), 0.46f, StoryChapterBuilderCommon.Navy);

        float[] slotX = { -0.86f, 0f, 0.86f };
        string[] slotTitles = { "TOPLANMA ALANI", "İLETİŞİM KİŞİSİ", "CAN'IN GÖREVİ" };
        string[] slotHints =
        {
            "BİNALARDAN UZAK\nAÇIK ALAN",
            "BAŞKA ŞEHİRDE\nBİR YAKIN",
            "DÜDÜKLE\nYARDIM ÇAĞIR"
        };
        for (int i = 0; i < slotX.Length; i++)
        {
            GameObject slot = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanSlot_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.74f, 0.72f, 0.045f), materials.white, board, false);
            slot.transform.localPosition = new Vector3(slotX[i], -0.15f, 0.12f);
            slot.transform.localRotation = Quaternion.identity;
            GameObject slotHeader = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanSlotHeader_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.66f, 0.16f, 0.035f), materials.navy, board, false);
            slotHeader.transform.localPosition = new Vector3(slotX[i], 0.09f, 0.17f);
            slotHeader.transform.localRotation = Quaternion.identity;
            Transform socketMotion = StoryChapterBuilderCommon.NewChild(
                board,
                "PlanCardGhostPulse_" + (i + 1));
            socketMotion.localPosition = new Vector3(slotX[i], cardCenterY, cardCenterZ);
            socketMotion.localRotation = Quaternion.identity;
            socketMotion.localScale = Vector3.one;
            GameObject socket = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanCardGhostSocket_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.68f, 0.42f, 0.018f), socketMaterial, socketMotion, false);
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localRotation = Quaternion.identity;
            GameObject socketInset = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanCardGhostInset_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.6f, 0.34f, 0.01f), materials.cream, socketMotion, false);
            socketInset.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            socketInset.transform.localRotation = Quaternion.identity;
            Animation socketAnimation = socketMotion.gameObject.AddComponent<Animation>();
            socketAnimation.AddClip(socketPulseClip, "Pulse");
            socketAnimation.clip = socketPulseClip;
            socketAnimation.playAutomatically = true;
            socketAnimation.wrapMode = WrapMode.Loop;
            CreateBoardLabel(board, "PlanSlotTitle_" + (i + 1), slotTitles[i],
                new Vector3(slotX[i], 0.09f, 0.205f), new Vector2(0.62f, 0.12f), 0.52f,
                Color.white);
            CreateBoardLabel(board, "PlanSlotHint_" + (i + 1), slotHints[i],
                new Vector3(slotX[i], -0.24f, 0.18f), new Vector2(0.62f, 0.3f), 0.54f,
                new Color(0.18f, 0.23f, 0.27f, 1f));
        }

        world.familyPlanComplete = CreateCompletedPlanCard(
            board, "FamilyPlanCompleteMark", new Vector3(-0.86f, cardCenterY, cardCenterZ),
            materials.cream, "MAHALLE PARKI\nTOPLANMA YERİ", StoryChapterBuilderCommon.Navy, snapClip);
        world.familyContactComplete = CreateCompletedPlanCard(
            board, "FamilyContactCompleteMark", new Vector3(0f, cardCenterY, cardCenterZ),
            materials.cream, "MELEK TEYZE\nANKARA", StoryChapterBuilderCommon.Navy, snapClip);
        world.familyRoleComplete = CreateCompletedPlanCard(
            board, "FamilyCanRoleCompleteMark", new Vector3(0.86f, cardCenterY, cardCenterZ),
            materials.cream, "CAN\nDÜDÜK SORUMLUSU", StoryChapterBuilderCommon.Navy, snapClip);
    }

    private static GameObject CreateCompletedPlanCard(Transform board, string name, Vector3 localPosition,
        Material material, string text, Color textColor, AnimationClip snapClip)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(board);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        GameObject snapMotion = new GameObject(name + "_SnapMotion");
        snapMotion.transform.SetParent(root.transform, false);
        snapMotion.transform.localPosition = Vector3.zero;
        snapMotion.transform.localRotation = Quaternion.identity;
        snapMotion.transform.localScale = Vector3.one;
        GameObject card = StoryChapterBuilderCommon.CreatePrimitive(
            name + "_Card", PrimitiveType.Cube, Vector3.zero,
            new Vector3(0.62f, 0.36f, 0.055f), material, snapMotion.transform, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;
        CreateBoardLabel(snapMotion.transform, name + "_Text", text,
            new Vector3(0f, 0f, 0.065f), new Vector2(0.55f, 0.26f), 0.76f, textColor);
        Animation animation = snapMotion.AddComponent<Animation>();
        animation.AddClip(snapClip, "Snap");
        animation.clip = snapClip;
        animation.playAutomatically = true;
        animation.wrapMode = WrapMode.Once;
        root.SetActive(false);
        return root;
    }

    private static void CreateBoardLabel(Transform parent, string name, string text, Vector3 localPosition,
        Vector2 worldSize, float fontSize, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.localPosition = localPosition;
        // The board faces the room through its local -Z side. Rotate world-space
        // text so it is read from the gameplay camera instead of as mirrored back-face text.
        root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        // TextMeshPro world text already uses scene units. The old 0.1 scale made
        // every board caption technically present but unreadable in the gameplay camera.
        root.transform.localScale = Vector3.one;
        TextMeshPro label = root.AddComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath);
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.rectTransform.sizeDelta = worldSize;
    }

    private static void CreatePlanCardLabel(Transform artwork, string name, string text, float fontSize,
        Vector2 worldSize)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(artwork, false);
        root.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        root.transform.localScale = Vector3.one;
        TextMeshPro label = root.AddComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath);
        label.text = text;
        label.fontSize = fontSize;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = 0.42f;
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = StoryChapterBuilderCommon.Navy;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.rectTransform.sizeDelta = worldSize;
    }

    private static void BuildDiscoveryFurniture(Transform parent, StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "FamilyPlanCardTable",
            parent,
            new Vector3(-4.12f, 0f, 1.08f),
            new Vector3(0.92f, 0.72f, 2.05f),
            new Vector3(0f, 90f, 0f));

        world.signalNightstandClosed = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Nightstand_02.prefab",
            "SignalNightstand",
            parent,
            new Vector3(-0.76f, 0f, -3.72f),
            new Vector3(0.78f, 0.72f, 0.62f),
            Vector3.zero);
        world.signalDrawerOpen = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Nightstand_02.prefab",
            "SignalNightstandOpen",
            parent,
            new Vector3(-0.76f, 0f, -3.72f),
            new Vector3(0.78f, 0.72f, 0.62f),
            Vector3.zero);
        world.signalDrawerContent = StorySharedHomePrefabBuilder.FindDescendant(
            world.signalDrawerOpen.transform,
            "Nightstand_02_Door");
        if (world.signalDrawerContent == null)
            throw new InvalidOperationException("Sinyal komodininin hareketli çekmecesi bulunamadı.");
        foreach (Collider drawerCollider in world.signalDrawerContent.GetComponentsInChildren<Collider>(true))
            drawerCollider.enabled = false;
        float drawerClosedZ = world.signalDrawerContent.localPosition.z;
        Renderer signalDrawerRenderer = world.signalDrawerContent.GetComponent<Renderer>();
        if (signalDrawerRenderer == null)
            throw new InvalidOperationException("Sinyal komodininin çekmece render'ı bulunamadı.");
        float drawerTravel = Mathf.Clamp(signalDrawerRenderer.localBounds.size.z * 0.5f, 0.16f, 0.2f);
        float drawerOpenZ = drawerClosedZ + drawerTravel;
        AnimationClip drawerClip = GetOrCreateSignalDrawerOpenClip(drawerClosedZ, drawerOpenZ);
        Animation drawerAnimation = world.signalDrawerContent.gameObject.AddComponent<Animation>();
        drawerAnimation.playAutomatically = true;
        drawerAnimation.AddClip(drawerClip, drawerClip.name);
        drawerAnimation.clip = drawerClip;
        world.signalDrawerOpen.SetActive(false);

        Material syntyTownMaterial = AssetDatabase.LoadAssetAtPath<Material>(SyntyTownMaterialPath);
        if (syntyTownMaterial == null)
            throw new InvalidOperationException("POLYGON Town dolap materyali bulunamadı: " + SyntyTownMaterialPath);

        world.foodCabinetClosed = StoryChapterBuilderCommon.InstantiateAsset(
            SyntyKitchenCounterPath,
            "KitchenLowCabinet",
            parent,
            new Vector3(4.62f, 0f, -1.82f),
            new Vector3(0.92f, 1.08f, 1.5f),
            new Vector3(0f, 270f, 0f),
            false,
            false,
            syntyTownMaterial);
        world.foodCabinetDoorLeft = StoryChapterBuilderCommon.InstantiateAsset(
            SyntyKitchenCounterPath,
            "KitchenCabinetDoorLeft",
            parent,
            new Vector3(4.62f, 0f, -1.82f),
            new Vector3(0.92f, 1.08f, 1.5f),
            new Vector3(0f, 270f, 0f),
            false,
            false,
            syntyTownMaterial);
        ConfigureSyntyCounterOpen(world.foodCabinetDoorLeft);
        world.foodCabinetDoorLeft.SetActive(false);
        world.foodCabinetDoorRight = new GameObject("KitchenCabinetDoorRight");
        world.foodCabinetDoorRight.transform.SetParent(world.foodCabinetDoorLeft.transform);
        world.foodCabinetDoorRight.SetActive(false);

        world.healthCabinetClosed = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusFirstAidPath,
            "AidServiceCase",
            parent,
            new Vector3(4.76f, 0.85f, -1.98f),
            new Vector3(0.58f, 0.43f, 0.5f),
            new Vector3(0f, 90f, 0f),
            false,
            false);

        world.healthDrawerOpen = new GameObject("AidCabinetOpenDoor");
        world.healthDrawerOpen.transform.SetParent(parent);
        world.healthDrawerOpen.transform.SetPositionAndRotation(
            new Vector3(4.76f, 0.85f, -1.98f),
            Quaternion.identity);
        world.healthDrawerOpen.SetActive(false);
        world.healthKitDisplay = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusFirstAidPath,
            "AidServiceCase_Removed",
            world.healthDrawerOpen.transform,
            new Vector3(4.68f, 0.85f, -2.03f),
            new Vector3(0.58f, 0.43f, 0.5f),
            new Vector3(0f, 102f, -3f),
            false,
            false);
        world.healthKitDisplay.SetActive(false);

        Material kenneySurvivalMaterial = AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath);
        if (kenneySurvivalMaterial == null)
            throw new InvalidOperationException("Kenney survival materyali bulunamadı: " + KenneySurvivalMaterialPath);

        world.warmthChestClosed = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyChestPath,
            "WarmthChest",
            parent,
            new Vector3(-4.25f, 0f, -1.8f),
            new Vector3(0.88f, 0.62f, 0.68f),
            new Vector3(0f, 90f, 0f),
            false,
            false,
            kenneySurvivalMaterial);
        world.warmthChestOpen = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyOpenBoxPath,
            "WarmthChestOpenDoor",
            parent,
            new Vector3(-4.25f, 0f, -1.8f),
            new Vector3(0.88f, 0.62f, 0.68f),
            new Vector3(0f, 90f, 0f),
            false,
            false,
            kenneySurvivalMaterial);
        world.warmthChestOpen.SetActive(false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_09.prefab",
            "ExitBagShelf",
            parent,
            new Vector3(4.25f, 0.18f, 5.72f),
            new Vector3(1.25f, 0.78f, 0.5f),
            new Vector3(0f, 180f, 0f));
    }

    private static void ConfigureSyntyCounterOpen(GameObject counter)
    {
        Transform leftDoor = StorySharedHomePrefabBuilder.FindDescendant(
            counter.transform,
            "SM_Prop_Kitchen_Counter_01_Door_01");
        Transform rightDoor = StorySharedHomePrefabBuilder.FindDescendant(
            counter.transform,
            "SM_Prop_Kitchen_Counter_01_Door_02");
        Transform drawer = StorySharedHomePrefabBuilder.FindDescendant(
            counter.transform,
            "SM_Prop_Kitchen_Counter_01_Drawer_01");
        if (leftDoor != null)
            leftDoor.localRotation = Quaternion.Euler(0f, -105f, 0f);
        if (rightDoor != null)
            rightDoor.localRotation = Quaternion.Euler(0f, 105f, 0f);
        if (drawer != null)
            drawer.localPosition += new Vector3(0f, 0f, -0.3f);
    }

    private static void BuildOpenBag(Transform parent, StoryChapterBuilderCommon.Materials materials, PreviewWorld world)
    {
        Vector3 bagFloorPosition = new Vector3(1.55f, 0.02f, -1.55f);
        world.openBag = new GameObject("EmergencyBag_Open_Packing");
        world.openBag.transform.SetParent(parent);
        world.openBag.transform.position = bagFloorPosition;
        StoryChapterBuilderCommon.InstantiateAsset(
            OriginalOpenBagPath,
            "EmergencyBag_Open_Visual",
            world.openBag.transform,
            bagFloorPosition,
            new Vector3(0.72f, 0.5f, 0.62f),
            new Vector3(0f, 35f, 0f),
            false);
        StoryChapterBuilderCommon.NewChild(world.openBag.transform, "OriginalBolum1BagSource");

        foreach (BagDropZone oldZone in world.openBag.GetComponentsInChildren<BagDropZone>(true))
            Object.DestroyImmediate(oldZone);

        GameObject dropZoneObject = StoryChapterBuilderCommon.CreatePrimitive(
            "PhysicalBagOpening",
            PrimitiveType.Cube,
            world.openBag.transform.position + new Vector3(0f, 0.43f, 0f),
            new Vector3(0.6f, 0.2f, 0.46f),
            materials.teal,
            world.openBag.transform,
            true);
        Renderer dropRenderer = dropZoneObject.GetComponent<Renderer>();
        if (dropRenderer != null)
            dropRenderer.enabled = false;
        BoxCollider dropCollider = dropZoneObject.GetComponent<BoxCollider>();
        dropCollider.isTrigger = true;
        BagDropZone dropZone = dropZoneObject.AddComponent<BagDropZone>();
        world.bagDropZone = dropZone;

        world.bagOpening = StoryChapterBuilderCommon.CreatePoint(
            "BagOpeningPoint",
            dropZoneObject.transform,
            dropZoneObject.transform.position + Vector3.up * 0.1f,
            dropZoneObject.transform.position);
        Transform inside = StoryChapterBuilderCommon.CreatePoint(
            "BagInsidePoint",
            dropZoneObject.transform,
            dropZoneObject.transform.position - Vector3.up * 0.08f,
            dropZoneObject.transform.position);
        world.bagPackedRoot = StoryChapterBuilderCommon.NewChild(dropZoneObject.transform, "PackedItemResults");

        SerializedObject dropData = new SerializedObject(dropZone);
        dropData.FindProperty("openingPoint").objectReferenceValue = world.bagOpening;
        dropData.FindProperty("insidePoint").objectReferenceValue = inside;
        dropData.FindProperty("itemSlots").arraySize = 0;
        dropData.ApplyModifiedPropertiesWithoutUndo();
        BuildBagReviewHardware(parent, materials, world);
    }

    private static void BuildBagReviewHardware(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        Vector3 bagPosition = world.openBag.transform.position;
        world.radioReviewProp = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/Radio.prefab",
            "BagReview_EmergencyRadio",
            parent,
            bagPosition + new Vector3(0.15f, 0.34f, 0.04f),
            new Vector3(0.3f, 0.2f, 0.18f),
            new Vector3(-90f, -163f, 0f),
            false,
            false);
        world.radioReviewProp.SetActive(false);
    }

    private static void BuildWrongChoiceConsequence(Transform parent, StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        world.wrongChoiceConsequence = new GameObject("ConsoleWeightConsequence");
        world.wrongChoiceConsequence.transform.SetParent(parent);
        StoryChapterBuilderCommon.InstantiateAsset(
            OriginalOpenBagPath,
            "EmergencyBag_HeavySlumped",
            world.wrongChoiceConsequence.transform,
            new Vector3(1.55f, 0.02f, -1.55f),
            new Vector3(0.7f, 0.42f, 0.6f),
            new Vector3(0f, 35f, -13f),
            false);
        world.wrongChoiceConsequence.SetActive(false);
    }

    private static void BuildFinalBagStates(Transform parent, PreviewWorld world, Transform deniz)
    {
        world.wornBag = StoryChapterBuilderCommon.InstantiateAsset(
            ClosedBackpackPath,
            "EmergencyBag_Worn",
            parent,
            deniz.position,
            new Vector3(0.42f, 0.5f, 0.3f),
            new Vector3(10f, 180f, 0f),
            false);
        world.wornBag.transform.SetParent(deniz, true);
        world.wornBag.transform.position = deniz.position + Vector3.up * 0.76f - deniz.forward * 0.18f;
        world.wornBag.transform.rotation = deniz.rotation * Quaternion.Euler(10f, 180f, 0f);
        world.wornBag.SetActive(false);

        world.exitBag = StoryChapterBuilderCommon.InstantiateAsset(
            ClosedBackpackPath,
            "EmergencyBag_ExitShelf",
            parent,
            new Vector3(4.25f, 0.96f, 5.63f),
            new Vector3(0.48f, 0.52f, 0.34f),
            new Vector3(0f, 170f, 0f),
            false);
        world.exitBag.SetActive(false);
    }

    private static PlayableInteractions BuildDirectWorldInteractions(Transform parent, PreviewWorld world,
        StoryChapterBuilderCommon.Characters family, StoryPreparationDirector director,
        StoryChapterBuilderCommon.Materials materials)
    {
        PlayableInteractions result = new PlayableInteractions();
        Transform interactionRoot = StoryChapterBuilderCommon.NewChild(parent, "DirectWorldInteractions");
        Transform planBoard = StorySharedHomePrefabBuilder.FindDescendant(parent, "FamilyPlanBoard");
        if (planBoard == null)
            throw new InvalidOperationException("Aile planı panosu bulunamadı.");
        Vector3 meetingPointDrop = planBoard.TransformPoint(new Vector3(-0.86f, -0.2f, 0.245f));
        Vector3 contactDrop = planBoard.TransformPoint(new Vector3(0f, -0.2f, 0.245f));
        Vector3 canRoleDrop = planBoard.TransformPoint(new Vector3(0.86f, -0.2f, 0.245f));
        Transform[] planCardSnapTargets =
        {
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanCardGhostPulse_1"),
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanCardGhostPulse_2"),
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanCardGhostPulse_3")
        };
        if (planCardSnapTargets.Any(target => target == null))
            throw new InvalidOperationException("Aile planı manyetik kart yuvaları bulunamadı.");
        Transform planCardDragPlaneAnchor = StoryChapterBuilderCommon.NewChild(
            interactionRoot,
            "PlanCardSharedDragPlane");
        planCardDragPlaneAnchor.SetPositionAndRotation(
            planBoard.TransformPoint(new Vector3(0f, -0.2f, 0.62f)),
            planBoard.rotation);

        GameObject planHotspot = StoryChapterBuilderCommon.CreatePrimitive(
            "FamilyMeetingPointCard_Drag",
            PrimitiveType.Cube,
            new Vector3(-3.92f, 0.78f, 0.62f),
            new Vector3(0.82f, 0.08f, 0.54f),
            LoadMaterial("Cream"),
            interactionRoot,
            true,
            Quaternion.Euler(0f, 12f, 0f));
        Transform cardArtwork = StoryChapterBuilderCommon.NewChild(
            planHotspot.transform,
            "MeetingPointCardArtwork");
        cardArtwork.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cardArtwork.localScale = new Vector3(1f / 0.82f, 1f / 0.08f, 1f / 0.54f);
        CreatePlanCardLabel(
            cardArtwork,
            "MeetingPointCardLabel",
            "MAHALLE PARKI\nBİNALARDAN UZAK\nAÇIK ALAN",
            0.72f,
            new Vector2(0.68f, 0.38f));
        result.startFamilyPlan = StoryChapterBuilderCommon.AddInteractable(
            planHotspot,
            "Inspect_FamilyPlanBoard",
            "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationParent,
            true,
            1,
            0.8f,
            2f);
        GameObject planDropObject = new GameObject("FamilyPlanCardDropZone");
        planDropObject.transform.SetParent(interactionRoot);
        Vector3 planDropCenter = meetingPointDrop;
        planDropObject.transform.SetPositionAndRotation(planDropCenter, Quaternion.Euler(0f, 90f, 0f));
        BoxCollider planDropCollider = planDropObject.AddComponent<BoxCollider>();
        planDropCollider.size = new Vector3(0.4f, 0.75f, 0.75f);
        planDropCollider.isTrigger = true;
        BagDropZone planDropZone = planDropObject.AddComponent<BagDropZone>();
        DraggableItem planDrag = planHotspot.AddComponent<DraggableItem>();
        SerializedObject planDragData = new SerializedObject(planDrag);
        planDragData.FindProperty("isCorrectItem").boolValue = true;
        planDragData.FindProperty("displayName").stringValue = "Mahalle parkı kartı";
        planDragData.FindProperty("inputEnabled").boolValue = false;
        planDragData.FindProperty("notifyGameManager").boolValue = false;
        planDragData.FindProperty("tapToBagEnabled").boolValue = false;
        planDragData.FindProperty("dropZoneOverride").objectReferenceValue = planDropZone;
        planDragData.FindProperty("dragLift").floatValue = 0.06f;
        planDragData.FindProperty("dragOnCameraPlane").boolValue = true;
        planDragData.FindProperty("dragPlaneAnchor").objectReferenceValue = planCardDragPlaneAnchor;
        planDragData.FindProperty("clampDragMinimumY").boolValue = true;
        planDragData.FindProperty("dragMinimumWorldY").floatValue = 1.08f;
        planDragData.FindProperty("faceCameraWhileDragging").boolValue = true;
        planDragData.FindProperty("dragHoverWobbleDegrees").floatValue = 1.6f;
        planDragData.FindProperty("dragHoverWobbleSpeed").floatValue = 1.45f;
        StoryChapterBuilderCommon.SetArray(
            planDragData,
            "magneticSnapTargets",
            planCardSnapTargets.Cast<Object>().ToArray());
        planDragData.FindProperty("magneticSnapViewportRadius").floatValue = 0.2f;
        planDragData.FindProperty("magneticSnapMinTravelPixels").floatValue = 48f;
        planDragData.FindProperty("magneticSnapStrength").floatValue = 1f;
        planDragData.FindProperty("magneticSnapSurfaceOffset").floatValue = 0f;
        planDragData.FindProperty("dragScale").floatValue = 1.04f;
        planDragData.FindProperty("returnDuration").floatValue = 0.34f;
        planDragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        planDragData.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(result.startFamilyPlan, planDropObject.transform);
        UnityEventTools.AddBoolPersistentListener(
            result.startFamilyPlan.OnInteracted,
            world.familyPlanComplete.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.startFamilyPlan.OnInteracted,
            planHotspot.SetActive,
            false);
        UnityEventTools.AddPersistentListener(
            result.startFamilyPlan.OnInteracted,
            director.OnFamilyPlanStarted);

        result.placeContactCard = BuildPlanCardInteraction(
            interactionRoot,
            planCardDragPlaneAnchor,
            planCardSnapTargets,
            "MelekContactCard_Drag",
            "MELEK TEYZE\nANKARA'DA YAŞIYOR\n05XX 123 45 67",
            "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle",
            "Melek Teyze — şehir dışındaki yakınımız",
            new Vector3(-3.92f, 0.78f, 1.08f),
            6f,
            LoadMaterial("Cream"),
            contactDrop,
            world.familyContactComplete,
            director.OnContactCardPlaced);
        result.assignCanWhistleRole = BuildPlanCardInteraction(
            interactionRoot,
            planCardDragPlaneAnchor,
            planCardSnapTargets,
            "CanWhistleRoleCard_Drag",
            "CAN\nDÜDÜĞÜ TAŞIR\nYARDIM İÇİN 3 KEZ ÇALAR",
            "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle",
            "Can — düdük sorumlusu",
            new Vector3(-3.92f, 0.78f, 1.52f),
            -7f,
            LoadMaterial("Cream"),
            canRoleDrop,
            world.familyRoleComplete,
            director.OnCanWhistleRolePlaced);

        BuildCategoryDiscoveryInteractions(interactionRoot, world, director, result);

        PreviewItem[] items =
        {
            new("Flashlight", "Item_Fener.prefab", "El feneri",
                new Vector3(-0.25f, 0.81f, 0.65f), new Vector3(0.4f, 0.2f, 0.26f), 22f,
                StoryCameraZoneId.PreparationBag),
            new("Batteries", "Item_Pil.prefab", "Yedek pil",
                new Vector3(0.2f, 0.81f, 0.65f), new Vector3(0.28f, 0.16f, 0.22f), -12f,
                StoryCameraZoneId.PreparationBag),
            new("Whistle", "whistle.prefab", "Düdük",
                new Vector3(0.65f, 0.81f, 0.65f), new Vector3(0.3f, 0.18f, 0.24f), 12f,
                StoryCameraZoneId.PreparationBag),
            new("Radio", "Radio.prefab", "Pilli radyo",
                new Vector3(1.1f, 0.81f, 0.65f), new Vector3(0.36f, 0.24f, 0.28f),
                new Vector3(-90f, -161f, 0f),
                StoryCameraZoneId.PreparationBag),
            new("Water", "Item_Su.prefab", "Su",
                new Vector3(-0.35f, 1.17f, 0.3f), new Vector3(0.3f, 0.48f, 0.3f),
                new Vector3(-90f, 0f, 0f),
                StoryCameraZoneId.PreparationBag),
            new("Food", "konserve.prefab", "Konserve",
                new Vector3(0.25f, 1.14f, 0.3f), new Vector3(0.3f, 0.3f, 0.3f), 8f,
                StoryCameraZoneId.PreparationBag),
            new("FirstAid", "FirstAidKit.prefab", "İlk yardım çantası",
                new Vector3(-0.1f, 1.14f, 0.38f), new Vector3(0.58f, 0.4f, 0.38f), -12f,
                StoryCameraZoneId.PreparationBag),
            new("Documents", "dockument.prefab", "Belge kopyaları",
                new Vector3(0.85f, 1.14f, 0.38f), new Vector3(0.42f, 0.08f, 0.52f), -6f,
                StoryCameraZoneId.PreparationBag),
            new("Blanket", "Quilt_514.prefab", "İnce battaniye",
                new Vector3(-0.15f, 1.14f, 0.4f), new Vector3(0.74f, 0.26f, 0.32f), 8f,
                StoryCameraZoneId.PreparationBag),
            new("Clothes", "T-shirt.prefab", "Yedek kıyafet",
                new Vector3(0.8f, 1.14f, 0.4f), new Vector3(0.52f, 0.18f, 0.56f), -8f,
                StoryCameraZoneId.PreparationBag),
            new("Console", "GameConsole_01 Variant.prefab", "Oyun konsolu",
                new Vector3(0.45f, 1.14f, 0.9f), new Vector3(0.58f, 0.18f, 0.38f), 12f,
                StoryCameraZoneId.PreparationBag, false),
            new("GlassBottle", "camSise.prefab", "Cam şişe",
                new Vector3(0.85f, 1.17f, 0.3f), new Vector3(0.26f, 0.46f, 0.26f),
                new Vector3(-90f, -10f, 0f),
                StoryCameraZoneId.PreparationBag, false),
            new("Pan", "Item_Tava.prefab", "Ağır tava",
                new Vector3(0.45f, 1.14f, 0.9f), new Vector3(0.66f, 0.2f, 0.48f),
                new Vector3(0f, 15f, 72f),
                StoryCameraZoneId.PreparationBag, false)
        };

        List<StoryPreparationItem> builtItems = new List<StoryPreparationItem>();
        for (int i = 0; i < items.Length; i++)
        {
            StoryPreparationItem builtItem =
                BuildDirectItem(items[i], i, interactionRoot, world, director, materials);
            builtItems.Add(builtItem);
        }
        result.items = builtItems.ToArray();
        StoryPreparationItem waterInspectionItem =
            builtItems.Single(item => item.ItemId == "Water");
        // DiscoverFoodCategory first clears the previous table contents. Run this
        // scene-authored activation afterwards so the bottle that owns the date
        // label is actually present when the inspection interaction unlocks.
        UnityEventTools.AddBoolPersistentListener(
            result.discoverFood.OnInteracted,
            waterInspectionItem.Interactable.gameObject.SetActive,
            true);
        result.signalDrawerItems = BuildSignalDrawerItemInteractions(
            world,
            builtItems,
            director,
            materials);
        BuildPrePackInspectionInteractions(interactionRoot, director, result);
        BuildPhysicalReviewInteractions(interactionRoot, world, family, director, materials, result);

        BoxCollider bagLiftCollider = world.openBag.AddComponent<BoxCollider>();
        FitColliderToRenderers(bagLiftCollider, world.openBag);
        result.testBagWeight = StoryChapterBuilderCommon.AddInteractable(
            world.openBag,
            "Final_TestBagWeight",
            "Çantanın sapını doğrudan basılı tutup kaldır",
            StoryInteractionKind.Collect,
            null,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PreparationBag,
            true,
            1,
            1.15f,
            1.6f);
        UnityEventTools.AddPersistentListener(result.testBagWeight.OnInteracted, director.OnBagWeightTested);

        Transform consoleHost = StoryChapterBuilderCommon.NewChild(
            interactionRoot,
            "ConsoleConflict_Draggable");
        GameObject consoleVisual = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/GameConsole_01 Variant.prefab",
            "ConsoleConflict_InBag_Visual",
            consoleHost,
            world.openBag.transform.position + new Vector3(0.03f, 0.3f, 0.02f),
            new Vector3(0.42f, 0.15f, 0.28f),
            new Vector3(0f, 8f, -18f),
            false,
            false);
        Vector3 consoleWorldPosition = consoleVisual.transform.position;
        consoleVisual.transform.SetParent(interactionRoot, true);
        consoleHost.position = consoleWorldPosition;
        consoleVisual.transform.SetParent(consoleHost, true);
        BoxCollider consoleCollider = consoleHost.gameObject.AddComponent<BoxCollider>();
        FitColliderToRenderers(consoleCollider, consoleVisual);
        result.removeConsole = StoryChapterBuilderCommon.AddInteractable(
            consoleHost.gameObject,
            "Resolve_RemoveConsole",
            "Konsolu çantadan tutup güvenli masaya geri sürükle",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationWrongChoice,
            true,
            1,
            1f,
            1.7f);
        GameObject consoleDropObject = new GameObject("ConsoleReturnTableDropZone");
        consoleDropObject.transform.SetParent(interactionRoot);
        consoleDropObject.transform.position = new Vector3(0.45f, 0.9f, 0.65f);
        BoxCollider consoleDropCollider = consoleDropObject.AddComponent<BoxCollider>();
        consoleDropCollider.size = new Vector3(1.4f, 0.7f, 1.0f);
        consoleDropCollider.isTrigger = true;
        BagDropZone consoleDropZone = consoleDropObject.AddComponent<BagDropZone>();
        DraggableItem consoleDrag = consoleHost.gameObject.AddComponent<DraggableItem>();
        ConfigureDraggable(
            consoleDrag,
            "Can'ın oyun konsolu",
            consoleDropZone,
            0.12f,
            1.04f);
        StoryChapterBuilderCommon.SetGestureTarget(
            result.removeConsole,
            consoleDropObject.transform);
        world.consoleReturned = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/GameConsole_01 Variant.prefab",
            "ConsoleConflict_ReturnedToTable",
            interactionRoot,
            new Vector3(0.45f, 0.92f, 0.65f),
            new Vector3(0.68f, 0.2f, 0.44f),
            new Vector3(0f, -18f, 0f),
            false,
            false);
        world.consoleReturned.SetActive(false);
        world.consoleInBag = consoleHost.gameObject;
        world.consoleInBag.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(
            result.removeConsole.OnInteracted,
            world.consoleInBag.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.removeConsole.OnInteracted,
            world.consoleReturned.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.removeConsole.OnInteracted,
            director.OnConsoleRemoved);

        result.removeConsole.SetAvailable(false);

        BoxCollider wornBagCollider = world.wornBag.AddComponent<BoxCollider>();
        FitColliderToRenderers(wornBagCollider, world.wornBag);
        result.placeBagAtExit = StoryChapterBuilderCommon.AddInteractable(
            world.wornBag,
            "Final_PlaceBagAtExit",
            "Çantayı tutup alçak çıkış rafına doğru çek",
            StoryInteractionKind.Exit,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationBagFit,
            true,
            1,
            1f,
            1.7f);
        SerializedObject placeData = new SerializedObject(result.placeBagAtExit);
        placeData.FindProperty("returnCameraAfterCompletion").boolValue = true;
        placeData.FindProperty("returnCameraZone").intValue = (int)StoryCameraZoneId.PreparationOverview;
        placeData.FindProperty("focusLingerSeconds").floatValue = 1.4f;
        placeData.ApplyModifiedPropertiesWithoutUndo();
        GameObject exitDropObject = new GameObject("ExitShelfBagDropZone");
        exitDropObject.transform.SetParent(interactionRoot);
        exitDropObject.transform.position = new Vector3(4.25f, 1.04f, 5.63f);
        BoxCollider exitDropCollider = exitDropObject.AddComponent<BoxCollider>();
        exitDropCollider.size = new Vector3(1.25f, 1f, 1.1f);
        exitDropCollider.isTrigger = true;
        BagDropZone exitDropZone = exitDropObject.AddComponent<BagDropZone>();
        DraggableItem exitBagDrag = world.wornBag.AddComponent<DraggableItem>();
        ConfigureDraggable(
            exitBagDrag,
            "Deniz'in afet çantası",
            exitDropZone,
            0.12f,
            1.03f);
        StoryChapterBuilderCommon.SetGestureTarget(result.placeBagAtExit, exitDropObject.transform);

        result.comfortItem = BuildComfortItemInteraction(interactionRoot, family, director);
        return result;
    }

    private static StoryInteractable BuildPlanCardInteraction(
        Transform parent,
        Transform dragPlaneAnchor,
        Transform[] magneticSnapTargets,
        string objectName,
        string cardLabel,
        string prompt,
        string displayName,
        Vector3 sourcePosition,
        float yaw,
        Material material,
        Vector3 dropPosition,
        GameObject completedVisual,
        UnityAction completed)
    {
        Vector3 cardScale = new Vector3(0.78f, 0.08f, 0.52f);
        GameObject card = StoryChapterBuilderCommon.CreatePrimitive(
            objectName,
            PrimitiveType.Cube,
            sourcePosition,
            cardScale,
            material,
            parent,
            true,
            Quaternion.Euler(0f, yaw, 0f));
        Transform artwork = StoryChapterBuilderCommon.NewChild(card.transform, objectName + "_Artwork");
        artwork.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        artwork.localScale = new Vector3(1f / cardScale.x, 1f / cardScale.y, 1f / cardScale.z);
        CreatePlanCardLabel(
            artwork,
            objectName + "_Label",
            cardLabel,
            0.68f,
            new Vector2(0.66f, 0.38f));

        StoryInteractable interaction = StoryChapterBuilderCommon.AddInteractable(
            card,
            objectName,
            prompt,
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationParent,
            true,
            1,
            0.8f,
            2f);

        GameObject dropObject = new GameObject(objectName + "_DropZone");
        dropObject.transform.SetParent(parent);
        dropObject.transform.SetPositionAndRotation(dropPosition, Quaternion.Euler(0f, 90f, 0f));
        BoxCollider dropCollider = dropObject.AddComponent<BoxCollider>();
        dropCollider.size = new Vector3(0.4f, 0.62f, 0.62f);
        dropCollider.isTrigger = true;
        BagDropZone dropZone = dropObject.AddComponent<BagDropZone>();

        DraggableItem draggable = card.AddComponent<DraggableItem>();
        SerializedObject dragData = new SerializedObject(draggable);
        dragData.FindProperty("isCorrectItem").boolValue = true;
        dragData.FindProperty("displayName").stringValue = displayName;
        dragData.FindProperty("inputEnabled").boolValue = false;
        dragData.FindProperty("notifyGameManager").boolValue = false;
        dragData.FindProperty("tapToBagEnabled").boolValue = false;
        dragData.FindProperty("dropZoneOverride").objectReferenceValue = dropZone;
        dragData.FindProperty("dragLift").floatValue = 0.06f;
        dragData.FindProperty("dragOnCameraPlane").boolValue = true;
        dragData.FindProperty("dragPlaneAnchor").objectReferenceValue = dragPlaneAnchor;
        dragData.FindProperty("clampDragMinimumY").boolValue = true;
        dragData.FindProperty("dragMinimumWorldY").floatValue = 1.08f;
        dragData.FindProperty("faceCameraWhileDragging").boolValue = true;
        dragData.FindProperty("dragHoverWobbleDegrees").floatValue = 1.6f;
        dragData.FindProperty("dragHoverWobbleSpeed").floatValue = 1.45f;
        StoryChapterBuilderCommon.SetArray(
            dragData,
            "magneticSnapTargets",
            magneticSnapTargets.Cast<Object>().ToArray());
        dragData.FindProperty("magneticSnapViewportRadius").floatValue = 0.2f;
        dragData.FindProperty("magneticSnapMinTravelPixels").floatValue = 48f;
        dragData.FindProperty("magneticSnapStrength").floatValue = 1f;
        dragData.FindProperty("magneticSnapSurfaceOffset").floatValue = 0f;
        dragData.FindProperty("dragScale").floatValue = 1.04f;
        dragData.FindProperty("returnDuration").floatValue = 0.34f;
        dragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        dragData.ApplyModifiedPropertiesWithoutUndo();

        StoryChapterBuilderCommon.SetGestureTarget(interaction, dropObject.transform);
        SetAvailableOnStart(interaction, false);
        UnityEventTools.AddBoolPersistentListener(interaction.OnInteracted, completedVisual.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(interaction.OnInteracted, card.SetActive, false);
        UnityEventTools.AddPersistentListener(interaction.OnInteracted, completed);
        return interaction;
    }

    private static void BuildCategoryDiscoveryInteractions(
        Transform parent,
        PreviewWorld world,
        StoryPreparationDirector director,
        PlayableInteractions result)
    {
        result.discoverSignal = CreatePhysicalDiscovery(
            world.signalNightstandClosed,
            "Discover_SignalDrawer",
            "Çekmece kulpunu tutup aşağı doğru çek",
            StoryCameraZoneId.PreparationSignal,
            StoryInteractionGesture.SwipeDown);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverSignal.OnInteracted,
            world.signalNightstandClosed.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverSignal.OnInteracted,
            world.signalDrawerOpen.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.discoverSignal.OnInteracted,
            director.DiscoverSignalCategory);

        result.discoverFood = CreatePhysicalDiscovery(
            world.foodCabinetClosed,
            "Discover_FoodCabinet",
            "Alt dolap kapağına bas ve sağa ya da sola kaydır",
            StoryCameraZoneId.PreparationFood);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverFood.OnInteracted,
            world.foodCabinetClosed.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverFood.OnInteracted,
            world.foodCabinetDoorLeft.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverFood.OnInteracted,
            world.foodCabinetDoorRight.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.discoverFood.OnInteracted,
            director.DiscoverFoodCategory);

        result.discoverHealth = CreatePhysicalDiscovery(
            world.healthCabinetClosed,
            "Discover_HealthDrawer",
            "Dolabın üstündeki ilk yardım çantasına bas ve kendine doğru kaydır",
            StoryCameraZoneId.PreparationHealth);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverHealth.OnInteracted,
            world.healthCabinetClosed.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverHealth.OnInteracted,
            world.healthDrawerOpen.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverHealth.OnInteracted,
            world.healthKitDisplay.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.discoverHealth.OnInteracted,
            director.DiscoverHealthCategory);

        result.discoverWarmth = CreatePhysicalDiscovery(
            world.warmthChestClosed,
            "Discover_WarmthChest",
            "Sandığa bas ve sağa ya da sola kaydır",
            StoryCameraZoneId.PreparationWarmth);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverWarmth.OnInteracted,
            world.warmthChestClosed.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.discoverWarmth.OnInteracted,
            world.warmthChestOpen.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.discoverWarmth.OnInteracted,
            director.DiscoverWarmthCategory);
    }

    private static StoryInteractable CreatePhysicalDiscovery(
        GameObject physicalObject,
        string name,
        string prompt,
        StoryCameraZoneId cameraZone,
        StoryInteractionGesture gesture = StoryInteractionGesture.SwipeHorizontal)
    {
        if (physicalObject == null)
            throw new InvalidOperationException("Fiziksel keşif nesnesi bulunamadı: " + name);
        EnsurePhysicalInteractionCollider(physicalObject);
        StoryInteractable interaction = StoryChapterBuilderCommon.AddInteractable(
            physicalObject,
            name,
            prompt,
            StoryInteractionKind.Inspect,
            null,
            gesture,
            cameraZone,
            true,
            1,
            0.9f,
            1.6f);
        interaction.SetAvailable(false);
        return interaction;
    }

    private static void BuildPhysicalReviewInteractions(
        Transform parent,
        PreviewWorld world,
        StoryChapterBuilderCommon.Characters family,
        StoryPreparationDirector director,
        StoryChapterBuilderCommon.Materials materials,
        PlayableInteractions result)
    {
        Vector3 bag = world.openBag.transform.position;
        StoryPreparationItem flashlightItem = result.items.Single(item =>
            string.Equals(item.ItemId, "Flashlight", StringComparison.Ordinal));
        Transform flashlightRoot = flashlightItem.Interactable.transform;
        Vector3 flashlightPosition = flashlightRoot.position;
        GameObject flashlightSwitch = StoryChapterBuilderCommon.CreatePrimitive(
            "FlashlightTopSwitch",
            PrimitiveType.Cylinder,
            flashlightPosition + new Vector3(0f, 0.13f, 0.015f),
            new Vector3(0.055f, 0.025f, 0.055f),
            materials.amber,
            flashlightRoot,
            false,
            Quaternion.Euler(90f, 0f, 0f));
        result.reviewSignal = StoryChapterBuilderCommon.AddInteractable(
            flashlightSwitch,
            "Inspect_FlashlightSwitchOn",
            "El fenerinin üstündeki düğmeye dokun",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.PreparationFlashlight,
            true,
            1,
            0.55f,
            1.1f);
        SetAvailableOnStart(result.reviewSignal, false);

        world.signalTestLight = new GameObject("FlashlightInspectionBeam");
        world.signalTestLight.transform.SetParent(flashlightRoot);
        world.signalTestLight.transform.position = flashlightPosition + new Vector3(0f, 0.16f, 0.08f);
        Light signalLight = world.signalTestLight.AddComponent<Light>();
        signalLight.type = LightType.Point;
        signalLight.color = new Color(0.7f, 0.9f, 1f);
        signalLight.intensity = 2.4f;
        signalLight.range = 3.8f;
        signalLight.shadows = LightShadows.None;
        world.signalTestLight.SetActive(false);

        GameObject flashlightSwitchOff = StoryChapterBuilderCommon.CreatePrimitive(
            "FlashlightTopSwitch_OffState",
            PrimitiveType.Cylinder,
            flashlightSwitch.transform.position,
            new Vector3(0.055f, 0.025f, 0.055f),
            materials.teal,
            flashlightRoot,
            false,
            Quaternion.Euler(90f, 0f, 0f));
        result.reviewSignalFlashlightOff = StoryChapterBuilderCommon.AddInteractable(
            flashlightSwitchOff,
            "Inspect_FlashlightSwitchOff",
            "El fenerinin düğmesine yeniden dokun",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.PreparationFlashlight,
            true,
            1,
            0.55f,
            1.1f);
        SetAvailableOnStart(result.reviewSignalFlashlightOff, false);
        flashlightSwitchOff.SetActive(false);

        result.signalFlashlightApproachPoint = StoryChapterBuilderCommon.CreatePoint(
            "FlashlightInspectionApproachPoint",
            parent,
            flashlightPosition + new Vector3(-0.95f, -0.81f, -0.35f),
            flashlightPosition);

        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignal.OnInteracted,
            flashlightSwitch.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignal.OnInteracted,
            flashlightSwitchOff.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignal.OnInteracted,
            world.signalTestLight.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.reviewSignal.OnInteracted,
            director.OnSignalFlashlightSwitchedOn);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalFlashlightOff.OnInteracted,
            world.signalTestLight.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalFlashlightOff.OnInteracted,
            flashlightSwitchOff.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalFlashlightOff.OnInteracted,
            flashlightSwitch.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.reviewSignalFlashlightOff.OnInteracted,
            director.OnSignalFlashlightSwitchedOff);

        EnsurePhysicalInteractionCollider(world.radioReviewProp);
        result.reviewSignalRadio = StoryChapterBuilderCommon.AddInteractable(
            world.radioReviewProp,
            "Review_TuneEmergencyRadio",
            "Radyo frekans düğmesini nesnenin üstünden yana çevir",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.PreparationBag,
            true,
            1,
            1.1f,
            1.4f);
        ConfigureReviewSwipe(
            result.reviewSignalRadio,
            parent,
            bag + new Vector3(0.7f, 0.47f, 0.05f));
        UnityEventTools.AddPersistentListener(
            result.reviewSignalRadio.OnInteracted,
            director.OnSignalRadioTuned);

        Transform whistleHost = StoryChapterBuilderCommon.NewChild(parent, "Review_WhistleHandoff");
        GameObject whistleVisual = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/whistle.prefab",
            "Review_WhistleHandoff_Visual",
            whistleHost,
            new Vector3(-3.65f, 0.82f, -2.25f),
            new Vector3(0.7f, 0.42f, 0.62f),
            new Vector3(0f, 0f, 90f),
            false,
            false);
        Vector3 whistleWorldPosition = whistleVisual.transform.position;
        whistleVisual.transform.SetParent(parent, true);
        whistleHost.position = whistleWorldPosition;
        whistleVisual.transform.SetParent(whistleHost, true);
        BoxCollider whistleCollider = whistleHost.gameObject.AddComponent<BoxCollider>();
        FitColliderToRenderers(whistleCollider, whistleVisual);
        result.reviewSignalWhistle = StoryChapterBuilderCommon.AddInteractable(
            whistleHost.gameObject,
            "Review_ClipWhistleToCan",
            "Düdüğü tutup Can'ın göğsündeki klipse sürükle",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationSiblingHandoff,
            true,
            1,
            0.9f,
            1.5f);
        GameObject whistleDropObject = new GameObject("Review_WhistleCanDropZone");
        whistleDropObject.transform.SetParent(family.can.transform);
        whistleDropObject.transform.localPosition = new Vector3(0f, 0.78f, 0.05f);
        whistleDropObject.transform.localRotation = Quaternion.identity;
        BoxCollider whistleDropCollider = whistleDropObject.AddComponent<BoxCollider>();
        whistleDropCollider.size = new Vector3(0.8f, 1.15f, 0.8f);
        whistleDropCollider.isTrigger = true;
        BagDropZone whistleDropZone = whistleDropObject.AddComponent<BagDropZone>();
        Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            StoryChapterBuilderCommon.KenneyInputPromptRoot + "/marker_circle.png");
        if (targetSprite == null)
            throw new InvalidOperationException("Can düdük hedefi için Kenney halka sprite'ı bulunamadı.");
        GameObject targetSocket = new GameObject("Can_WhistleTargetSocket");
        targetSocket.transform.SetParent(whistleDropObject.transform);
        targetSocket.transform.localPosition = Vector3.zero;
        targetSocket.transform.localRotation = Quaternion.identity;
        targetSocket.transform.localScale = Vector3.one * 0.34f;
        targetSocket.AddComponent<BillboardToCamera>();
        SpriteRenderer targetRenderer = targetSocket.AddComponent<SpriteRenderer>();
        targetRenderer.sprite = targetSprite;
        targetRenderer.color = new Color32(95, 218, 142, 215);
        targetRenderer.sortingOrder = 42;
        targetRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        targetRenderer.receiveShadows = false;
        DraggableItem whistleDrag = whistleHost.gameObject.AddComponent<DraggableItem>();
        ConfigureDraggable(
            whistleDrag,
            "Can'ın düdüğü",
            whistleDropZone,
            0.1f,
            1.04f);
        StoryChapterBuilderCommon.SetGestureTarget(
            result.reviewSignalWhistle,
            whistleDropObject.transform);
        world.whistleCanTarget = whistleDropObject;

        world.whistleOnCan = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/whistle.prefab",
            "Can_WhistleClipped",
            family.can.transform,
            family.can.transform.position + new Vector3(0.16f, 0.82f, 0.08f),
            new Vector3(0.22f, 0.14f, 0.2f),
            new Vector3(0f, 0f, 90f),
            false,
            false);
        world.whistleOnCan.transform.SetParent(family.can.transform, true);
        world.whistleOnCan.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalWhistle.OnInteracted,
            whistleHost.gameObject.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalWhistle.OnInteracted,
            world.whistleOnCan.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalWhistle.OnInteracted,
            whistleDropObject.SetActive,
            false);
        UnityEventTools.AddPersistentListener(
            result.reviewSignalWhistle.OnInteracted,
            director.ReviewSignalCategory);

        result.reviewSignal.SetAvailable(false);
        result.reviewSignalFlashlightOff.SetAvailable(false);
        result.reviewSignalRadio.SetAvailable(false);
        result.reviewSignalWhistle.SetAvailable(false);
        whistleHost.gameObject.SetActive(false);
        whistleDropObject.SetActive(false);
    }

    private static void ConfigureReviewSwipe(
        StoryInteractable interaction,
        Transform parent,
        Vector3 targetPosition)
    {
        Transform target = StoryChapterBuilderCommon.CreatePoint(
            interaction.name + "_SwipeTarget",
            parent,
            targetPosition,
            interaction.transform.position);
        StoryChapterBuilderCommon.SetGestureTarget(interaction, target);
    }

    private static void ConfigureDraggable(
        DraggableItem draggable,
        string displayName,
        BagDropZone dropZone,
        float dragLift,
        float dragScale)
    {
        SerializedObject dragData = new SerializedObject(draggable);
        dragData.FindProperty("isCorrectItem").boolValue = true;
        dragData.FindProperty("displayName").stringValue = displayName;
        dragData.FindProperty("inputEnabled").boolValue = false;
        dragData.FindProperty("notifyGameManager").boolValue = false;
        dragData.FindProperty("tapToBagEnabled").boolValue = false;
        dragData.FindProperty("dropZoneOverride").objectReferenceValue = dropZone;
        dragData.FindProperty("dragLift").floatValue = dragLift;
        dragData.FindProperty("dragScale").floatValue = dragScale;
        dragData.FindProperty("returnDuration").floatValue = 0.34f;
        dragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        dragData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StoryInteractable[] BuildSignalDrawerItemInteractions(
        PreviewWorld world,
        IReadOnlyList<StoryPreparationItem> stagedItems,
        StoryPreparationDirector director,
        StoryChapterBuilderCommon.Materials materials)
    {
        PreviewItem[] drawerItems =
        {
            new("Flashlight", "Item_Fener.prefab", "El feneri",
                new Vector3(-0.88f, 0.47f, -3.75f), new Vector3(0.25f, 0.13f, 0.14f), 22f,
                StoryCameraZoneId.PreparationSignal),
            new("Batteries", "Item_Pil.prefab", "Yedek pil",
                new Vector3(-0.64f, 0.47f, -3.75f), new Vector3(0.16f, 0.11f, 0.13f), -12f,
                StoryCameraZoneId.PreparationSignal),
            new("Whistle", "whistle.prefab", "Düdük",
                new Vector3(-0.88f, 0.47f, -3.63f), new Vector3(0.16f, 0.09f, 0.13f), 12f,
                StoryCameraZoneId.PreparationSignal),
            new("Radio", "Radio.prefab", "Pilli radyo",
                new Vector3(-0.64f, 0.47f, -3.63f), new Vector3(0.23f, 0.14f, 0.17f),
                new Vector3(-90f, -161f, 0f),
                StoryCameraZoneId.PreparationSignal)
        };

        StoryInteractable[] interactions = new StoryInteractable[drawerItems.Length];
        bool drawerWasActive = world.signalDrawerOpen.activeSelf;
        world.signalDrawerOpen.SetActive(true);
        try
        {
            for (int i = 0; i < drawerItems.Length; i++)
            {
                PreviewItem definition = drawerItems[i];
                StoryPreparationItem stagedItem = stagedItems.Single(item =>
                    string.Equals(item.ItemId, definition.id, StringComparison.Ordinal));

                Transform wrapper = StoryChapterBuilderCommon.NewChild(
                    world.signalDrawerContent,
                    "DrawerItem_" + definition.id);
                wrapper.position = definition.position;
                GameObject visual = BuildDirectItemVisual(
                    definition,
                    "DrawerItem_" + definition.id + "_Visual",
                    wrapper,
                    definition.position,
                    definition.size,
                    materials);
                BoxCollider collider = wrapper.gameObject.AddComponent<BoxCollider>();
                FitColliderToRenderers(collider, visual);

                StoryInteractable interaction = StoryChapterBuilderCommon.AddInteractable(
                    wrapper.gameObject,
                    "Take_" + definition.id,
                    definition.displayName + " — dokunup masaya al",
                    StoryInteractionKind.Collect,
                    null,
                    StoryInteractionGesture.Tap,
                    StoryCameraZoneId.PreparationSignal,
                    true,
                    1,
                    0.8f,
                    1.2f);
                SetAvailableOnStart(interaction, false);
                GameObject marker = interaction.HighlightRoot;
                if (marker != null)
                {
                    // The signal camera is a tight drawer close-up. Full-size world
                    // prompts overlap here, so each choice gets a compact badge fixed
                    // directly above its own physical item.
                    marker.transform.position = definition.position + Vector3.up * 0.19f;
                    marker.transform.localScale = Vector3.one * 0.16f;
                }
                UnityEventTools.AddBoolPersistentListener(
                    interaction.OnInteracted,
                    wrapper.gameObject.SetActive,
                    false);
                UnityEventTools.AddPersistentListener(
                    interaction.OnInteracted,
                    stagedItem.StageForSelection);
                UnityEventTools.AddPersistentListener(
                    interaction.OnInteracted,
                    director.OnSignalItemStaged);
                interaction.SetAvailable(false);
                interactions[i] = interaction;
            }
        }
        finally
        {
            world.signalDrawerOpen.SetActive(drawerWasActive);
        }

        return interactions;
    }

    private static Vector3 SignalDrawerOpenPosition(string itemId)
    {
        return itemId switch
        {
            "Flashlight" => new Vector3(-0.88f, 0.47f, -3.36f),
            "Batteries" => new Vector3(-0.64f, 0.47f, -3.36f),
            "Whistle" => new Vector3(-0.88f, 0.47f, -3.24f),
            "Radio" => new Vector3(-0.64f, 0.47f, -3.24f),
            _ => throw new ArgumentOutOfRangeException(nameof(itemId), itemId, null)
        };
    }

    private static StoryPreparationItem BuildDirectItem(PreviewItem definition, int index, Transform parent,
        PreviewWorld world, StoryPreparationDirector director, StoryChapterBuilderCommon.Materials materials)
    {
        Transform wrapper = StoryChapterBuilderCommon.NewChild(parent, "WorldItem_" + definition.id);
        wrapper.position = definition.position;
        GameObject visual = BuildDirectItemVisual(
            definition,
            definition.displayName,
            wrapper,
            definition.position,
            definition.size,
            materials);

        BoxCollider wrapperCollider = wrapper.gameObject.AddComponent<BoxCollider>();
        FitColliderToRenderers(wrapperCollider, visual);

        foreach (DraggableItem stale in wrapper.GetComponentsInChildren<DraggableItem>(true))
            Object.DestroyImmediate(stale);
        DraggableItem draggable = wrapper.gameObject.AddComponent<DraggableItem>();
        SerializedObject dragData = new SerializedObject(draggable);
        dragData.FindProperty("isCorrectItem").boolValue = definition.recommended;
        dragData.FindProperty("displayName").stringValue = definition.displayName;
        dragData.FindProperty("inputEnabled").boolValue = false;
        dragData.FindProperty("notifyGameManager").boolValue = false;
        dragData.FindProperty("tapToBagEnabled").boolValue = false;
        dragData.FindProperty("hideItemInBag").boolValue = true;
        dragData.FindProperty("dropZoneOverride").objectReferenceValue = world.bagDropZone;
        dragData.ApplyModifiedPropertiesWithoutUndo();

        StoryInteractable interactable = StoryChapterBuilderCommon.AddInteractable(
            wrapper.gameObject,
            "Pack_" + definition.id,
            definition.cameraZone == StoryCameraZoneId.PreparationSignal
                ? definition.displayName + " — çekmecenin içinden seç"
                : definition.recommended
                    ? definition.displayName + " — çantanın açık ağzına sürükle"
                    : definition.displayName + " — ağırlığını çantada dene",
            definition.recommended ? StoryInteractionKind.Collect : StoryInteractionKind.UnsafeChoice,
            null,
            definition.cameraZone == StoryCameraZoneId.PreparationSignal
                ? StoryInteractionGesture.Tap
                : StoryInteractionGesture.DragToBag,
            definition.cameraZone,
            true,
            1,
            1f,
            1.5f);
        SetAvailableOnStart(interactable);
        bool signalItem = CategoryFor(definition.id) == StoryPreparationCategory.Signal;
        if (signalItem)
        {
            AnimationClip stageClip = GetOrCreateSignalStageClip(
                definition.id,
                SignalDrawerOpenPosition(definition.id),
                definition.position);
            Animation stageAnimation = wrapper.gameObject.AddComponent<Animation>();
            stageAnimation.playAutomatically = true;
            stageAnimation.AddClip(stageClip, stageClip.name);
            stageAnimation.clip = stageClip;
        }

        GameObject packed = null;
        if (definition.recommended)
        {
            Vector3 packedPosition = world.bagOpening.position + PackedOffset(index);
            packed = BuildDirectItemVisual(
                definition,
                "Packed_" + definition.id,
                world.bagPackedRoot,
                packedPosition,
                definition.size * 0.34f,
                materials);
            foreach (Collider collider in packed.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (DraggableItem stale in packed.GetComponentsInChildren<DraggableItem>(true))
                Object.DestroyImmediate(stale);
            packed.SetActive(false);
        }

        Transform stateHost = StoryChapterBuilderCommon.NewChild(parent, "ItemState_" + definition.id);
        StoryPreparationItem item = stateHost.gameObject.AddComponent<StoryPreparationItem>();
        SerializedObject itemData = new SerializedObject(item);
        itemData.FindProperty("itemId").stringValue = definition.id;
        itemData.FindProperty("displayName").stringValue = definition.displayName;
        itemData.FindProperty("category").intValue = (int)CategoryFor(definition.id);
        itemData.FindProperty("recommended").boolValue = definition.recommended;
        itemData.FindProperty("storyFlag").intValue = (int)FlagFor(definition.id);
        itemData.FindProperty("childLine").stringValue = WrongChildLine(definition.id);
        itemData.FindProperty("parentLine").stringValue = WrongParentLine(definition.id);
        itemData.FindProperty("director").objectReferenceValue = director;
        itemData.FindProperty("interactable").objectReferenceValue = interactable;
        itemData.FindProperty("sourceRoot").objectReferenceValue = wrapper.gameObject;
        itemData.FindProperty("packedVisual").objectReferenceValue = packed;
        itemData.FindProperty("consequenceRoot").objectReferenceValue =
            definition.recommended ? null : world.wrongChoiceConsequence;
        itemData.FindProperty("legacyBagMotion").objectReferenceValue = draggable;
        itemData.FindProperty("hideSourceWhenUnavailable").boolValue = true;
        itemData.FindProperty("stageDelay").floatValue = signalItem ? 0.74f : 0f;
        itemData.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(interactable.OnInteracted, item.Select);
        // Each category owns this same table at a different story beat. Authoring
        // every category as visible at once creates a pile even though gameplay
        // reveals only the current category.
        item.SetAvailable(false);
        return item;
    }

    private static GameObject BuildDirectItemVisual(PreviewItem definition, string name, Transform parent,
        Vector3 position, Vector3 size, StoryChapterBuilderCommon.Materials materials)
    {
        if (string.Equals(definition.id, "FirstAid", StringComparison.Ordinal))
        {
            return StoryAuthoredPropFactory.CreateFirstAidKit(
                name,
                parent,
                position,
                size,
                definition.euler,
                materials.coral,
                materials.navy,
                materials.cream,
                materials.amber,
                false);
        }

        if (string.Equals(definition.id, "Blanket", StringComparison.Ordinal))
        {
            return StoryChapterBuilderCommon.InstantiateAsset(
                KenneyBedrollPath,
                name,
                parent,
                position,
                size,
                definition.euler,
                false,
                false,
                AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath));
        }

        return StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/" + definition.prefab,
            name,
            parent,
            position,
            size,
            definition.euler,
            false,
            false);
    }

    private static void BuildPrePackInspectionInteractions(
        Transform parent,
        StoryPreparationDirector director,
        PlayableInteractions result)
    {
        StoryPreparationItem water = result.items.First(item => item.ItemId == "Water");
        Transform waterRoot = water.Interactable.transform;
        Vector3 waterLabelPosition = waterRoot.position + new Vector3(-0.16f, 0.21f, 0f);

        Transform waterUncheckedRoot = StoryChapterBuilderCommon.NewChild(
            waterRoot,
            "WaterExpiryLabel_Unchecked");
        waterUncheckedRoot.position = waterLabelPosition;
        BoxCollider waterCollider = waterUncheckedRoot.gameObject.AddComponent<BoxCollider>();
        waterCollider.size = new Vector3(0.22f, 0.3f, 0.42f);
        waterCollider.isTrigger = true;
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryLabel_Surface",
            PrimitiveType.Cube,
            waterLabelPosition,
            new Vector3(0.018f, 0.095f, 0.13f),
            LoadMaterial("Amber"),
            waterUncheckedRoot,
            false);
        StoryChapterBuilderCommon.CreateWorldLabel(
            "WaterExpiryDateText",
            "SKT\n2027",
            waterLabelPosition + new Vector3(-0.011f, 0f, 0f),
            new Vector3(0f, -90f, 0f),
            0.055f,
            StoryChapterBuilderCommon.Navy,
            waterUncheckedRoot,
            new Vector2(0.14f, 0.09f));

        Transform waterCheckedRoot = StoryChapterBuilderCommon.NewChild(
            waterRoot,
            "WaterExpiryCheckedState");
        waterCheckedRoot.position = waterLabelPosition;
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryCheckedSurface",
            PrimitiveType.Cube,
            waterLabelPosition,
            new Vector3(0.018f, 0.095f, 0.13f),
            LoadMaterial("Teal"),
            waterCheckedRoot,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryCheckmarkShort",
            PrimitiveType.Cube,
            waterLabelPosition + new Vector3(-0.012f, -0.014f, -0.022f),
            new Vector3(0.012f, 0.02f, 0.05f),
            LoadMaterial("Navy"),
            waterCheckedRoot,
            false,
            Quaternion.Euler(25f, 0f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryCheckmarkLong",
            PrimitiveType.Cube,
            waterLabelPosition + new Vector3(-0.012f, 0.01f, 0.03f),
            new Vector3(0.012f, 0.02f, 0.075f),
            LoadMaterial("Navy"),
            waterCheckedRoot,
            false,
            Quaternion.Euler(-35f, 0f, 0f));
        waterCheckedRoot.gameObject.SetActive(false);

        result.inspectWaterDate = StoryChapterBuilderCommon.AddInteractable(
            waterUncheckedRoot.gameObject,
            "Inspect_WaterExpiryDate",
            "Su \u015fi\u015fesinin tarih etiketini nesnenin \u00fczerinde yana \u00e7evir",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.PreparationFood,
            true,
            1,
            1.1f,
            1.5f);
        Transform waterSwipeTarget = StoryChapterBuilderCommon.CreatePoint(
            "WaterExpirySwipeTarget",
            waterRoot,
            waterLabelPosition + new Vector3(0f, 0f, 0.3f),
            waterLabelPosition);
        StoryChapterBuilderCommon.SetGestureTarget(result.inspectWaterDate, waterSwipeTarget);
        result.inspectWaterDate.SetAvailable(false);
        UnityEventTools.AddBoolPersistentListener(
            result.inspectWaterDate.OnInteracted,
            waterUncheckedRoot.gameObject.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.inspectWaterDate.OnInteracted,
            waterCheckedRoot.gameObject.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.inspectWaterDate.OnInteracted,
            director.OnWaterDateChecked);

        Transform bandageRoot = StoryChapterBuilderCommon.NewChild(parent, "BandageSealInspection");
        bandageRoot.position = new Vector3(4.43f, 0.85f, -1.55f);
        GameObject bandageUnchecked = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusBandagesPath,
            "BandageSeal_Unchecked",
            bandageRoot,
            bandageRoot.position,
            new Vector3(0.52f, 0.28f, 0.34f),
            new Vector3(0f, 82f, 0f),
            false,
            false);
        Transform bandageCheckedRoot = StoryChapterBuilderCommon.NewChild(
            bandageRoot,
            "BandageSealCheckedState");
        bandageCheckedRoot.position = bandageRoot.position;
        StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusBandagesPath,
            "BandageSeal_Checked",
            bandageCheckedRoot,
            bandageRoot.position,
            new Vector3(0.52f, 0.28f, 0.34f),
            new Vector3(0f, 82f, 0f),
            false,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "BandageSealCheckmarkShort",
            PrimitiveType.Cube,
            bandageRoot.position + new Vector3(0.075f, 0.3f, -0.012f),
            new Vector3(0.06f, 0.02f, 0.03f),
            LoadMaterial("Navy"),
            bandageCheckedRoot,
            false,
            Quaternion.Euler(0f, 35f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "BandageSealCheckmarkLong",
            PrimitiveType.Cube,
            bandageRoot.position + new Vector3(0.125f, 0.3f, 0.012f),
            new Vector3(0.1f, 0.02f, 0.03f),
            LoadMaterial("Navy"),
            bandageCheckedRoot,
            false,
            Quaternion.Euler(0f, -35f, 0f));
        bandageCheckedRoot.gameObject.SetActive(false);

        BoxCollider bandageCollider = bandageRoot.gameObject.AddComponent<BoxCollider>();
        bandageCollider.size = new Vector3(0.58f, 0.3f, 0.42f);
        bandageCollider.center = new Vector3(0f, 0.08f, 0f);
        bandageCollider.isTrigger = true;
        result.inspectBandageSeal = StoryChapterBuilderCommon.AddInteractable(
            bandageRoot.gameObject,
            "Inspect_BandagePackageSeal",
            "Sarg\u0131 paketinin m\u00fch\u00fcr \u015feridinde bas\u0131l\u0131 tut",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PreparationHealth,
            true,
            1,
            1.2f,
            1.5f);
        result.inspectBandageSeal.SetAvailable(false);
        UnityEventTools.AddBoolPersistentListener(
            result.inspectBandageSeal.OnInteracted,
            bandageUnchecked.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.inspectBandageSeal.OnInteracted,
            bandageCheckedRoot.gameObject.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.inspectBandageSeal.OnInteracted,
            director.OnBandageSealChecked);
    }

    private static StoryPreparationCategory CategoryFor(string id)
    {
        return id switch
        {
            "Water" or "Food" or "GlassBottle" or "Pan" => StoryPreparationCategory.Food,
            "FirstAid" or "Documents" => StoryPreparationCategory.Health,
            "Blanket" or "Clothes" or "Console" => StoryPreparationCategory.Warmth,
            _ => StoryPreparationCategory.Signal
        };
    }

    private static StoryFlag FlagFor(string id)
    {
        return id switch
        {
            "Flashlight" => StoryFlag.BagFlashlight,
            "Batteries" => StoryFlag.BagBatteries,
            "Whistle" => StoryFlag.BagWhistle,
            "Radio" => StoryFlag.BagRadio,
            "Water" => StoryFlag.BagWater,
            "Food" => StoryFlag.BagFood,
            "FirstAid" => StoryFlag.BagFirstAid,
            "Documents" => StoryFlag.BagDocuments,
            "Blanket" => StoryFlag.BagBlanket,
            "Clothes" => StoryFlag.BagClothing,
            _ => StoryFlag.None
        };
    }

    private static string WrongChildLine(string id)
    {
        return id switch
        {
            "Console" => "Can bunu da çantaya koymak istiyor; ama ana çantada yer ve ağırlık sınırlı.",
            "GlassBottle" => "Cam şişe çarpınca kırılıp çantanın içindekileri tehlikeye atabilir.",
            "Pan" => "Bu tava tek başına çantayı gereksiz ağırlaştırıyor.",
            _ => "Bu seçim çantanın güvenli kullanımını zorlaştırabilir."
        };
    }

    private static string WrongParentLine(string id)
    {
        return id switch
        {
            "Console" => "Can için küçük bir rahatlatıcı eşya seçebiliriz; ağır konsol temel malzemelerin yerini almamalı.",
            "GlassBottle" => "Suyu sızdırmaz, hafif ve kırılmayan şişede taşırız.",
            "Pan" => "Temel malzemeleri Deniz'in güvenle taşıyabileceği ağırlıkta tutuyoruz.",
            _ => "Önceliğimiz hafif, dayanıklı ve gerçekten gerekli malzemeler."
        };
    }

    private static StoryInteractable BuildComfortItemInteraction(
        Transform parent,
        StoryChapterBuilderCommon.Characters family,
        StoryPreparationDirector director)
    {
        Transform sourceToy = StorySharedHomePrefabBuilder.FindDescendant(
            family.deniz.transform.root,
            "Can_ToyCar");
        if (sourceToy == null)
            throw new InvalidOperationException("Can'ın rahatlatıcı oyuncak arabası ortak evde bulunamadı.");
        sourceToy.position = family.can.transform.position + new Vector3(-0.42f, 0.08f, 0.12f);
        sourceToy.rotation = Quaternion.Euler(0f, 25f, 0f);

        GameObject resultToy = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "CanComfortToy_Result",
            parent,
            family.can.transform.position + new Vector3(0.26f, 0.04f, 0.18f),
            new Vector3(0.42f, 0.24f, 0.58f),
            new Vector3(0f, 25f, 0f),
            false);
        resultToy.SetActive(false);

        sourceToy.gameObject.SetActive(false);
        GameObject dragHost = new GameObject("CanComfortToy_Draggable");
        dragHost.transform.SetParent(parent);
        dragHost.transform.position = sourceToy.position;
        dragHost.transform.rotation = Quaternion.identity;
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "CanComfortToy_Draggable_Visual",
            dragHost.transform,
            sourceToy.position,
            new Vector3(0.42f, 0.24f, 0.58f),
            new Vector3(0f, 25f, 0f),
            false);
        BoxCollider sourceCollider = dragHost.AddComponent<BoxCollider>();
        FitColliderToRenderers(sourceCollider, dragHost);
        sourceCollider.isTrigger = false;
        StoryInteractable comfort = StoryChapterBuilderCommon.AddInteractable(
            dragHost,
            "Give_CanComfortToy",
            "Can'ın oyuncağını tutup ona doğru çek",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationWarmth,
            true,
            1,
            0.9f,
            1.5f);
        GameObject dropObject = new GameObject("CanComfortToyDropZone");
        dropObject.transform.SetParent(parent);
        dropObject.transform.position = family.can.transform.position + new Vector3(0f, 0.55f, 0f);
        BoxCollider dropCollider = dropObject.AddComponent<BoxCollider>();
        dropCollider.size = new Vector3(0.9f, 1.2f, 0.9f);
        dropCollider.isTrigger = true;
        BagDropZone dropZone = dropObject.AddComponent<BagDropZone>();
        DraggableItem draggable = dragHost.AddComponent<DraggableItem>();
        SerializedObject dragData = new SerializedObject(draggable);
        dragData.FindProperty("isCorrectItem").boolValue = true;
        dragData.FindProperty("displayName").stringValue = "Can'ın küçük arabası";
        dragData.FindProperty("inputEnabled").boolValue = false;
        dragData.FindProperty("notifyGameManager").boolValue = false;
        dragData.FindProperty("tapToBagEnabled").boolValue = false;
        dragData.FindProperty("dropZoneOverride").objectReferenceValue = dropZone;
        dragData.FindProperty("dragLift").floatValue = 0.12f;
        dragData.FindProperty("dragScale").floatValue = 1.05f;
        dragData.FindProperty("returnDuration").floatValue = 0.34f;
        dragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        dragData.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(comfort, dropObject.transform);
        comfort.SetAvailable(false);
        UnityEventTools.AddBoolPersistentListener(comfort.OnInteracted, dragHost.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(comfort.OnInteracted, resultToy.SetActive, true);
        UnityEventTools.AddPersistentListener(comfort.OnInteracted, director.OnComfortItemChosen);
        return comfort;
    }

    private static void ConfigureStoryDirector(StoryPreparationDirector director, StoryGameManager gameManager,
        StoryPlayerMovement movement, StoryTouchManager touch, StoryCameraController camera,
        StoryChapterBuilderCommon.ChapterUI ui, StoryChapterBuilderCommon.Characters family, PreviewWorld world,
        PlayableInteractions interactions)
    {
        SerializedObject serialized = new SerializedObject(director);
        StoryChapterBuilderCommon.Set(serialized, "gameManager", gameManager);
        StoryChapterBuilderCommon.Set(serialized, "player", movement);
        StoryChapterBuilderCommon.Set(serialized, "touchManager", touch);
        StoryChapterBuilderCommon.Set(serialized, "cameraController", camera);
        StoryChapterBuilderCommon.Set(serialized, "ui", ui.controller);
        StoryChapterBuilderCommon.Set(serialized, "deniz", family.deniz.transform);
        StoryChapterBuilderCommon.Set(serialized, "parent", family.parent.transform);
        StoryChapterBuilderCommon.Set(serialized, "can", family.can.transform);
        StoryChapterBuilderCommon.Set(serialized, "denizAnimator", family.denizAnimator);
        StoryChapterBuilderCommon.Set(serialized, "parentAnimator", family.parentAnimator);
        StoryChapterBuilderCommon.Set(serialized, "canAnimator", family.canAnimator);
        serialized.FindProperty("revisedFlow").boolValue = true;
        StoryChapterBuilderCommon.Set(serialized, "startFamilyPlan", interactions.startFamilyPlan);
        StoryChapterBuilderCommon.Set(serialized, "placeContactCard", interactions.placeContactCard);
        StoryChapterBuilderCommon.Set(serialized, "assignCanWhistleRole", interactions.assignCanWhistleRole);
        StoryChapterBuilderCommon.Set(serialized, "inspectEmptyBag", interactions.inspectBag);
        StoryChapterBuilderCommon.Set(serialized, "discoverSignal", interactions.discoverSignal);
        StoryChapterBuilderCommon.Set(serialized, "discoverFood", interactions.discoverFood);
        StoryChapterBuilderCommon.Set(serialized, "discoverHealth", interactions.discoverHealth);
        StoryChapterBuilderCommon.Set(serialized, "discoverWarmth", interactions.discoverWarmth);
        StoryChapterBuilderCommon.Set(serialized, "inspectWaterDate", interactions.inspectWaterDate);
        StoryChapterBuilderCommon.Set(serialized, "inspectBandageSeal", interactions.inspectBandageSeal);
        StoryChapterBuilderCommon.SetArray(
            serialized,
            "items",
            interactions.items.Cast<Object>().ToArray());
        StoryChapterBuilderCommon.SetArray(
            serialized,
            "signalDrawerItems",
            interactions.signalDrawerItems.Cast<Object>().ToArray());
        StoryChapterBuilderCommon.Set(serialized, "reviewSignal", interactions.reviewSignal);
        StoryChapterBuilderCommon.Set(
            serialized,
            "reviewSignalFlashlightOff",
            interactions.reviewSignalFlashlightOff);
        StoryChapterBuilderCommon.Set(serialized, "reviewSignalRadio", interactions.reviewSignalRadio);
        StoryChapterBuilderCommon.Set(serialized, "reviewSignalWhistle", interactions.reviewSignalWhistle);
        StoryChapterBuilderCommon.Set(
            serialized,
            "signalFlashlightApproachPoint",
            interactions.signalFlashlightApproachPoint);
        StoryChapterBuilderCommon.Set(serialized, "signalRadioReviewRoot", world.radioReviewProp);
        StoryChapterBuilderCommon.Set(
            serialized,
            "signalRadioTuningBeforeRoot",
            world.radioTuningBefore);
        StoryChapterBuilderCommon.Set(serialized, "signalWhistleTargetRoot", world.whistleCanTarget);
        StoryChapterBuilderCommon.Set(serialized, "reviewFood", interactions.reviewFood);
        StoryChapterBuilderCommon.Set(serialized, "reviewHealth", interactions.reviewHealth);
        StoryChapterBuilderCommon.Set(serialized, "reviewWarmth", interactions.reviewWarmth);
        StoryChapterBuilderCommon.Set(serialized, "chooseComfortItem", interactions.comfortItem);
        StoryChapterBuilderCommon.Set(serialized, "testBagWeight", interactions.testBagWeight);
        StoryChapterBuilderCommon.Set(serialized, "removeConsole", interactions.removeConsole);
        StoryChapterBuilderCommon.Set(serialized, "testBalancedBag", interactions.testBalancedBag);
        StoryChapterBuilderCommon.Set(serialized, "adjustBagStraps", interactions.adjustBagStraps);
        StoryChapterBuilderCommon.Set(serialized, "placeBagAtExit", interactions.placeBagAtExit);
        StoryChapterBuilderCommon.Set(serialized, "consoleConflictRoot", world.wrongChoiceConsequence);
        StoryChapterBuilderCommon.Set(serialized, "consoleInBagRoot", world.consoleInBag);
        StoryChapterBuilderCommon.Set(serialized, "consoleReturnedRoot", world.consoleReturned);
        StoryChapterBuilderCommon.Set(serialized, "openBagRoot", world.openBag);
        StoryChapterBuilderCommon.Set(serialized, "closedBagRoot", null);
        StoryChapterBuilderCommon.Set(serialized, "wornBagRoot", world.wornBag);
        StoryChapterBuilderCommon.Set(serialized, "exitShelfBagRoot", world.exitBag);
        StoryChapterBuilderCommon.Set(serialized, "completionPanel", ui.completionPanel);
        StoryChapterBuilderCommon.Set(serialized, "completionDetail", ui.completionDetail);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildBlackoutDrillTimeline(Transform parent, StoryPreparationDirector storyDirector,
        StoryTouchManager touch, StoryUIController ui, StoryInteractable placeBagAtExit,
        PreviewWorld world, StoryChapterBuilderCommon.Characters family)
    {
        const string timelineFolder = "Assets/Story/Generated/Timelines";
        const string timelinePath = timelineFolder + "/Story_01_BlackoutDrill.playable";
        const string endSignalPath = timelineFolder + "/Story_01_BlackoutComplete.signal";
        EnsureAssetFolder(timelineFolder);
        DeleteAssetIfPresent(timelinePath);
        DeleteAssetIfPresent(endSignalPath);
        DeleteAssetIfPresent(timelineFolder + "/Blackout_FindCan_Pause.signal");

        GameObject timelineRoot = new GameObject("PreparationBlackoutDrill");
        timelineRoot.transform.SetParent(parent);
        PlayableDirector playable = timelineRoot.AddComponent<PlayableDirector>();
        playable.playOnAwake = false;
        playable.extrapolationMode = DirectorWrapMode.None;

        EnsurePhysicalInteractionCollider(family.can);
        StoryInteractable blackoutCan = StoryChapterBuilderCommon.AddInteractable(
            family.can,
            "Blackout_FindCan",
            "Işık Can'a ulaştığında omzunda basılı tut",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PreparationWarmth,
            true,
            1,
            1.2f,
            1.5f);
        blackoutCan.SetAvailable(false);

        Transform planBoard = StorySharedHomePrefabBuilder.FindDescendant(parent, "FamilyPlanBoard");
        Transform safeTable = StorySharedHomePrefabBuilder.FindDescendant(parent, "SafeTable");
        if (planBoard == null || safeTable == null)
            throw new InvalidOperationException("Karanlık tatbikatı için aile panosu ve güvenli masa bulunamadı.");

        EnsurePhysicalInteractionCollider(planBoard.gameObject);
        StoryInteractable blackoutPlanFocus = StoryChapterBuilderCommon.AddInteractable(
            planBoard.gameObject,
            "Blackout_FocusFamilyPlan",
            "Feneri aile planının üstünde basılı tut",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PreparationParent,
            true,
            1,
            0.7f,
            1.6f);
        blackoutPlanFocus.SetAvailable(false);

        EnsurePhysicalInteractionCollider(safeTable.gameObject);
        StoryInteractable blackoutTableFocus = StoryChapterBuilderCommon.AddInteractable(
            safeTable.gameObject,
            "Blackout_FocusSafeTable",
            "Feneri güvenli masanın altına tut",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.PreparationBag,
            true,
            1,
            0.7f,
            1.8f);
        blackoutTableFocus.SetAvailable(false);

        GameObject blackoutFlashlightObject = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/Item_Fener.prefab",
            "Blackout_FlashlightInOuterPocket",
            timelineRoot.transform,
            world.exitBag.transform.position + new Vector3(-0.18f, 0.34f, -0.16f),
            new Vector3(0.32f, 0.17f, 0.2f),
            new Vector3(0f, 18f, 22f),
            true,
            true);
        StoryInteractable blackoutFlashlight = StoryChapterBuilderCommon.AddInteractable(
            blackoutFlashlightObject,
            "Blackout_RetrieveFlashlight",
            "Dış cepte görünen feneri tutup eline doğru sürükle",
            StoryInteractionKind.Collect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationExitShelf,
            true,
            1,
            0.9f,
            1.5f);
        GameObject flashlightHandTarget = new GameObject("BlackoutFlashlightHandDropZone");
        flashlightHandTarget.transform.SetParent(timelineRoot.transform);
        flashlightHandTarget.transform.position =
            family.deniz.transform.position + new Vector3(0.32f, 0.84f, 0.12f);
        BoxCollider flashlightHandCollider = flashlightHandTarget.AddComponent<BoxCollider>();
        flashlightHandCollider.size = new Vector3(0.75f, 0.9f, 0.75f);
        flashlightHandCollider.isTrigger = true;
        BagDropZone flashlightHandDropZone = flashlightHandTarget.AddComponent<BagDropZone>();
        DraggableItem blackoutFlashlightDrag = blackoutFlashlightObject.AddComponent<DraggableItem>();
        ConfigureDraggable(
            blackoutFlashlightDrag,
            "Dış cepteki el feneri",
            flashlightHandDropZone,
            0.1f,
            1.05f);
        StoryChapterBuilderCommon.SetGestureTarget(
            blackoutFlashlight,
            flashlightHandTarget.transform);
        blackoutFlashlight.SetAvailable(false);
        blackoutFlashlightObject.SetActive(false);

        GameObject blackoutWhistleObject = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/whistle.prefab",
            "Blackout_Whistle",
            timelineRoot.transform,
            family.can.transform.position + new Vector3(0.18f, 0.84f, 0.12f),
            new Vector3(0.34f, 0.2f, 0.3f),
            new Vector3(0f, 0f, 90f),
            true,
            true);
        StoryInteractable blackoutWhistle = StoryChapterBuilderCommon.AddInteractable(
            blackoutWhistleObject,
            "Blackout_UseWhistle",
            "Can'ın göğsündeki düdüğe doğrudan üç kısa kez dokun",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.RepeatedTap,
            StoryCameraZoneId.PreparationWarmth,
            true,
            3,
            0.9f,
            1.5f);
        blackoutWhistle.SetAvailable(false);
        blackoutWhistleObject.SetActive(false);

        Vector3 flashlightOrigin = world.exitBag.transform.position + new Vector3(-0.2f, 1.05f, -0.1f);
        GameObject planBeam = CreatePreparedBlackoutSpotlight(
            timelineRoot.transform,
            "BlackoutBeam_FamilyPlan",
            flashlightOrigin,
            planBoard.position,
            new Color(0.68f, 0.88f, 1f));
        GameObject tableBeam = CreatePreparedBlackoutSpotlight(
            timelineRoot.transform,
            "BlackoutBeam_SafeTable",
            flashlightOrigin,
            safeTable.position + Vector3.up * 0.3f,
            new Color(0.68f, 0.88f, 1f));
        GameObject canBeam = CreatePreparedBlackoutSpotlight(
            timelineRoot.transform,
            "BlackoutBeam_Can",
            flashlightOrigin,
            family.can.transform.position + Vector3.up * 0.72f,
            new Color(0.75f, 0.92f, 1f));

        UnityEventTools.AddBoolPersistentListener(
            blackoutCan.OnInteracted,
            blackoutCan.SetAvailable,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutCan.OnInteracted,
            tableBeam.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutCan.OnInteracted,
            canBeam.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            blackoutCan.OnInteracted,
            touch.SetInteractionsEnabled,
            false);
        UnityEventTools.AddPersistentListener(blackoutCan.OnInteracted, playable.Play);
        UnityEventTools.AddStringPersistentListener(
            blackoutCan.OnInteracted,
            ui.ShowContext,
            "Can yanında. Fener onu görünür kıldı; şimdi aile planındaki düdük görevini prova edin.");

        UnityEventTools.AddBoolPersistentListener(
            blackoutFlashlight.OnInteracted,
            blackoutFlashlight.SetAvailable,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutFlashlight.OnInteracted,
            blackoutFlashlightObject.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutFlashlight.OnInteracted,
            blackoutPlanFocus.SetAvailable,
            true);
        UnityEventTools.AddStringPersistentListener(
            blackoutFlashlight.OnInteracted,
            ui.ShowContext,
            "Fener elinde. Işığı doğrudan aile planına tutarak aramaya başla.");

        UnityEventTools.AddBoolPersistentListener(
            blackoutPlanFocus.OnInteracted,
            blackoutPlanFocus.SetAvailable,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutPlanFocus.OnInteracted,
            planBeam.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            blackoutPlanFocus.OnInteracted,
            blackoutTableFocus.SetAvailable,
            true);
        UnityEventTools.AddStringPersistentListener(
            blackoutPlanFocus.OnInteracted,
            ui.ShowContext,
            "Aile planı yerinde. Şimdi ışığı güvenli masanın altına yönelt.");

        UnityEventTools.AddBoolPersistentListener(
            blackoutTableFocus.OnInteracted,
            blackoutTableFocus.SetAvailable,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutTableFocus.OnInteracted,
            planBeam.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutTableFocus.OnInteracted,
            tableBeam.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            blackoutTableFocus.OnInteracted,
            blackoutCan.SetAvailable,
            true);
        UnityEventTools.AddStringPersistentListener(
            blackoutTableFocus.OnInteracted,
            ui.ShowContext,
            "Masa altı güvenli ve boş. Can'ın sesine dönüp ışığı doğrudan ona tut.");

        UnityEventTools.AddBoolPersistentListener(
            blackoutWhistle.OnInteracted,
            blackoutWhistle.SetAvailable,
            false);
        UnityEventTools.AddBoolPersistentListener(
            blackoutWhistle.OnInteracted,
            touch.SetInteractionsEnabled,
            false);
        UnityEventTools.AddPersistentListener(blackoutWhistle.OnInteracted, playable.Play);

        GameObject flashlight = new GameObject("BlackoutFlashlightBeam");
        flashlight.transform.SetParent(timelineRoot.transform);
        flashlight.transform.position = new Vector3(1.45f, 1.1f, -1.4f);
        Light flashlightLight = flashlight.AddComponent<Light>();
        flashlightLight.type = LightType.Point;
        flashlightLight.color = new Color(0.72f, 0.9f, 1f);
        flashlightLight.range = 7.5f;
        flashlightLight.intensity = 2.5f;
        flashlightLight.shadows = LightShadows.Soft;
        flashlight.SetActive(false);

        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, timelinePath);

        ActivationTrack flashlightTrack = timeline.CreateTrack<ActivationTrack>(null, "Fener ışığı");
        TimelineClip flashlightClip = flashlightTrack.CreateDefaultClip();
        flashlightClip.start = 3.0d;
        flashlightClip.duration = 13.0d;
        playable.SetGenericBinding(flashlightTrack, flashlight);

        BindBlackoutLight(
            timeline,
            playable,
            StorySharedHomePrefabBuilder.FindDescendant(parent, "Directional Light")?.GetComponent<Light>(),
            "Gün ışığı kesintisi",
            1.32f,
            timelineFolder + "/Story_01_BlackoutSun.anim");
        BindBlackoutLight(
            timeline,
            playable,
            StorySharedHomePrefabBuilder.FindDescendant(parent, "StoryFillLight")?.GetComponent<Light>(),
            "Oda ışığı kesintisi",
            2.1f,
            timelineFolder + "/Story_01_BlackoutFill.anim");

        SignalReceiver receiver = timelineRoot.AddComponent<SignalReceiver>();
        AddTimelineMessage(timeline, playable, receiver, timelineFolder, "BlackoutLine_01", 0.15d, ui,
            "Can: Elektrikler gidince ne yapıyoruz?");
        AddBlackoutPause(
            timeline,
            playable,
            receiver,
            timelineFolder,
            "Blackout_RetrieveFlashlight_Pause",
            1.9d,
            touch,
            ui,
            blackoutFlashlight,
            blackoutFlashlightObject,
            "Çıkışın yanındaki çantada görünen feneri tutup Deniz'in eline sürükle.");
        AddTimelineMessage(timeline, playable, receiver, timelineFolder, "BlackoutLine_02", 3.2d, ui,
            "Deniz: Can burada. Feneri hazırladığımız için karanlıkta birbirimizi görebiliyoruz.");
        AddTimelineMessage(timeline, playable, receiver, timelineFolder, "BlackoutLine_03", 10.2d, ui,
            "Anne: Karanlıkta çıkışa koşmuyoruz; sakin kalıp güvenli yolu kontrol ediyoruz.");
        AddBlackoutPause(
            timeline,
            playable,
            receiver,
            timelineFolder,
            "Blackout_Whistle_Pause",
            12d,
            touch,
            ui,
            blackoutWhistle,
            blackoutWhistleObject,
            "Can'ın göğsündeki düdüğe doğrudan üç kısa kez dokun.");
        AddTimelineMessage(timeline, playable, receiver, timelineFolder, "BlackoutLine_04", 15d, ui,
            "Can: Düdüğüm bende. Ayrılırsak üç kısa sesle yerimi belli edebilirim.");

        SignalAsset endSignal = ScriptableObject.CreateInstance<SignalAsset>();
        endSignal.name = "Story_01_BlackoutComplete";
        AssetDatabase.CreateAsset(endSignal, endSignalPath);
        SignalTrack endTrack = timeline.CreateTrack<SignalTrack>(null, "Tatbikat tamamlandı");
        SignalEmitter endEmitter = endTrack.CreateMarker<SignalEmitter>(22d);
        endEmitter.asset = endSignal;
        UnityEvent endReaction = new UnityEvent();
        UnityEventTools.AddPersistentListener(endReaction, ui.HideContext);
        UnityEventTools.AddBoolPersistentListener(endReaction, planBeam.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(endReaction, tableBeam.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(endReaction, canBeam.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(endReaction, touch.SetWorldNavigationEnabled, true);
        UnityEventTools.AddBoolPersistentListener(endReaction, touch.SetInteractionsEnabled, true);
        UnityEventTools.AddPersistentListener(endReaction, storyDirector.OnBagPlacedAtExit);
        receiver.AddReaction(endSignal, endReaction);
        playable.SetGenericBinding(endTrack, receiver);
        playable.playableAsset = timeline;

        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            touch.SetWorldNavigationEnabled,
            false);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            touch.SetInteractionsEnabled,
            false);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            world.wornBag.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            world.exitBag.SetActive,
            true);
        UnityEventTools.AddPersistentListener(placeBagAtExit.OnInteracted, playable.Play);
    }

    private static GameObject CreatePreparedBlackoutSpotlight(
        Transform parent,
        string name,
        Vector3 origin,
        Vector3 target,
        Color color)
    {
        GameObject beam = new GameObject(name);
        beam.transform.SetParent(parent);
        beam.transform.SetPositionAndRotation(
            origin,
            Quaternion.LookRotation((target - origin).normalized, Vector3.up));
        Light light = beam.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.range = Mathf.Max(7.5f, Vector3.Distance(origin, target) + 1.2f);
        light.intensity = 25f;
        light.innerSpotAngle = 21f;
        light.spotAngle = 38f;
        light.shadows = LightShadows.None;
        beam.SetActive(false);
        return beam;
    }

    private static void BindBlackoutLight(TimelineAsset timeline, PlayableDirector playable, Light light,
        string trackName, float normalIntensity, string clipPath)
    {
        if (light == null)
            return;

        DeleteAssetIfPresent(clipPath);
        AnimationClip clip = new AnimationClip
        {
            name = Path.GetFileNameWithoutExtension(clipPath),
            frameRate = 30f
        };
        clip.SetCurve(
            string.Empty,
            typeof(Light),
            "m_Intensity",
            new AnimationCurve(
                new Keyframe(0f, normalIntensity),
                new Keyframe(1.35f, normalIntensity),
                new Keyframe(1.6f, normalIntensity * 0.035f),
                new Keyframe(20.3f, normalIntensity * 0.035f),
                new Keyframe(21.8f, normalIntensity)));
        AssetDatabase.CreateAsset(clip, clipPath);

        Animator animator = light.GetComponent<Animator>() ?? light.gameObject.AddComponent<Animator>();
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip timelineClip = track.CreateClip(clip);
        timelineClip.start = 0d;
        timelineClip.duration = 22d;
        playable.SetGenericBinding(track, animator);
    }

    private static void AddTimelineMessage(TimelineAsset timeline, PlayableDirector playable, SignalReceiver receiver,
        string folder, string assetName, double time, StoryUIController ui, string message)
    {
        string path = folder + "/" + assetName + ".signal";
        DeleteAssetIfPresent(path);
        SignalAsset signal = ScriptableObject.CreateInstance<SignalAsset>();
        signal.name = assetName;
        AssetDatabase.CreateAsset(signal, path);
        SignalTrack track = timeline.CreateTrack<SignalTrack>(null, assetName);
        SignalEmitter emitter = track.CreateMarker<SignalEmitter>(time);
        emitter.asset = signal;
        UnityEvent reaction = new UnityEvent();
        UnityEventTools.AddStringPersistentListener(reaction, ui.ShowNarratedContext, message);
        receiver.AddReaction(signal, reaction);
        playable.SetGenericBinding(track, receiver);
    }

    private static void AddBlackoutPause(
        TimelineAsset timeline,
        PlayableDirector playable,
        SignalReceiver receiver,
        string folder,
        string assetName,
        double time,
        StoryTouchManager touch,
        StoryUIController ui,
        StoryInteractable interaction,
        GameObject visual,
        string message)
    {
        string path = folder + "/" + assetName + ".signal";
        DeleteAssetIfPresent(path);
        SignalAsset signal = ScriptableObject.CreateInstance<SignalAsset>();
        signal.name = assetName;
        AssetDatabase.CreateAsset(signal, path);
        SignalTrack track = timeline.CreateTrack<SignalTrack>(null, assetName);
        SignalEmitter emitter = track.CreateMarker<SignalEmitter>(time);
        emitter.asset = signal;

        UnityEvent reaction = new UnityEvent();
        UnityEventTools.AddPersistentListener(reaction, playable.Pause);
        UnityEventTools.AddBoolPersistentListener(reaction, touch.SetInteractionsEnabled, true);
        if (visual != null)
            UnityEventTools.AddBoolPersistentListener(reaction, visual.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(reaction, interaction.SetAvailable, true);
        UnityEventTools.AddStringPersistentListener(reaction, ui.ShowContext, message);
        receiver.AddReaction(signal, reaction);
        playable.SetGenericBinding(track, receiver);
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string normalized = folderPath.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void DeleteAssetIfPresent(string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static Vector3 PackedOffset(int index)
    {
        int column = index % 4;
        int row = index / 4;
        return new Vector3(
            (column - 1.5f) * 0.11f,
            -0.18f + (index % 2) * 0.015f,
            (row - 1.5f) * 0.08f);
    }

    private static StoryCameraController BuildCameras(Transform parent, Transform deniz,
        out Camera mainCamera, out CinemachineBrain brain)
    {
        StoryChapterBuilderCommon.CameraSpec[] specs =
        {
            new(
                StoryCameraZoneId.PreparationOverview,
                "CM_PreparationOverview_Rebuild",
                new Vector3(-2.6f, 11.2f, -13.2f),
                new Vector3(-0.1f, 0.9f, 0.7f),
                48f,
                false,
                deniz,
                18.5f,
                new Vector2(-0.2f, 0.17f)),
            new(
                StoryCameraZoneId.PreparationBag,
                "CM_PreparationBag_Rebuild",
                new Vector3(4.65f, 3.1f, -4.35f),
                new Vector3(0.9f, 0.72f, -0.45f),
                40f),
            new(
                StoryCameraZoneId.PreparationParent,
                "CM_PreparationFamilyPlan_Rebuild",
                new Vector3(2.65f, 3.45f, -1.55f),
                new Vector3(-4.35f, 1.45f, 1.15f),
                50f),
            new(
                StoryCameraZoneId.PreparationSignal,
                "CM_PreparationSignal_Rebuild",
                new Vector3(-0.76f, 1.28f, -2.15f),
                new Vector3(-0.76f, 0.49f, -3.48f),
                38f),
            new(
                StoryCameraZoneId.PreparationFlashlight,
                "CM_PreparationFlashlight_Rebuild",
                new Vector3(-0.18f, 1.24f, -0.18f),
                new Vector3(-0.25f, 0.86f, 0.65f),
                38f),
            new(
                StoryCameraZoneId.PreparationFood,
                "CM_PreparationKitchen_Rebuild",
                new Vector3(0.45f, 2.75f, -0.35f),
                new Vector3(4.16f, 0.82f, -1.82f),
                43f),
            new(
                StoryCameraZoneId.PreparationHealth,
                "CM_PreparationAid_Rebuild",
                new Vector3(0.85f, 2.85f, -0.15f),
                new Vector3(4.58f, 1.08f, -1.82f),
                43f),
            new(
                StoryCameraZoneId.PreparationWarmth,
                "CM_PreparationWarmth_Rebuild",
                new Vector3(0.2f, 2.65f, 0.45f),
                new Vector3(-4.25f, 0.38f, -1.8f),
                45f),
            new(
                StoryCameraZoneId.PreparationSiblingHandoff,
                "CM_PreparationSiblingHandoff_Rebuild",
                new Vector3(-2.9f, 2.2f, -6.2f),
                new Vector3(-2.9f, 0.72f, -2.7f),
                50f),
            new(
                StoryCameraZoneId.PreparationWrongChoice,
                "CM_PreparationConsole_Rebuild",
                new Vector3(4.25f, 2.45f, -3.65f),
                new Vector3(1.55f, 0.32f, -1.55f),
                42f),
            new(
                StoryCameraZoneId.PreparationBagFit,
                "CM_PreparationDeniz_Rebuild",
                new Vector3(2.8f, 2.35f, 1.25f),
                new Vector3(-0.65f, 0.88f, -2.38f),
                45f),
            new(
                StoryCameraZoneId.PreparationExitShelf,
                "CM_PreparationExitShelf_Rebuild",
                new Vector3(1.45f, 2.85f, 2.05f),
                new Vector3(4.25f, 0.92f, 5.62f),
                43f)
        };
        return StoryChapterBuilderCommon.BuildCameras(
            parent,
            StoryCameraZoneId.PreparationOverview,
            specs,
            out mainCamera,
            out brain);
    }

    private static void ConfigurePreviewText(Transform root)
    {
        TMP_Text objectiveTitle = StorySharedHomePrefabBuilder.FindDescendant(root, "ObjectiveTitle")
            ?.GetComponent<TMP_Text>();
        TMP_Text objectiveDetail = StorySharedHomePrefabBuilder.FindDescendant(root, "ObjectiveDetail")
            ?.GetComponent<TMP_Text>();
        if (objectiveTitle != null)
            objectiveTitle.text = "ÇANTAYI HAZIRLA";
        if (objectiveDetail != null)
            objectiveDetail.text = "Eşyayı basılı tut; açık çantanın içine sürükleyip bırak.";
    }

    private static void SetAvailableOnStart(StoryInteractable interactable, bool available = true)
    {
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("availableOnStart").boolValue = available;
        serialized.FindProperty("oneShot").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static BoxCollider EnsurePhysicalInteractionCollider(GameObject physicalObject)
    {
        BoxCollider collider = physicalObject.GetComponent<BoxCollider>();
        if (collider == null)
            collider = physicalObject.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        FitColliderToRenderers(collider, physicalObject);
        return collider;
    }

    private static void FitColliderToRenderers(BoxCollider collider, GameObject visual)
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

    private static void SetActive(Transform root, string objectName, bool active)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, objectName);
        if (target != null)
            target.gameObject.SetActive(active);
    }

    private static void MoveHomeObject(Transform root, string objectName, Vector3 localPosition, Vector3 localEuler)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, objectName);
        if (target == null)
            return;

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEuler);
    }

    private static void SetHomeObjectLocalTransform(
        Transform root,
        string objectName,
        Vector3 localPosition,
        Vector3 localEuler,
        Vector3 localScale)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, objectName);
        if (target == null)
            return;

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEuler);
        target.localScale = localScale;
    }

    private static void PlaceHomeObjectOnFloor(Transform root, string objectName, Vector2 floorPosition)
    {
        Transform target = StorySharedHomePrefabBuilder.FindDescendant(root, objectName);
        if (target == null)
            return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);

        target.position += new Vector3(
            floorPosition.x - bounds.center.x,
            -bounds.min.y,
            floorPosition.y - bounds.center.z);
    }

    private static void RequireAsset(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Gerekli Story asseti bulunamadı.", path);
    }
}

public static class StoryPreparationRebuildPreviewValidator
{
    [MenuItem("Tools/Deprem Story/Validate Story_01 Rebuild Preview")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    [MenuItem("Tools/Deprem Story/Validate Story_01 Rebuild Preview (Silent)")]
    public static void ValidateSilentFromMenu()
    {
        Validate(false);
    }

    public static void Validate(bool showDialog)
    {
        if (!File.Exists(StoryPreparationRebuildPreviewBuilder.ScenePath))
            throw new FileNotFoundException(
                "Story 01 rebuild önizleme sahnesi bulunamadı.",
                StoryPreparationRebuildPreviewBuilder.ScenePath);

        Scene scene = SceneManager.GetSceneByPath(StoryPreparationRebuildPreviewBuilder.ScenePath);
        bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
        if (openedForValidation)
            scene = EditorSceneManager.OpenScene(
                StoryPreparationRebuildPreviewBuilder.ScenePath,
                OpenSceneMode.Additive);

        try
        {
            GameObject root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == "STORY_01_REBUILD_PREVIEW");
            Require(root != null, "STORY_01_REBUILD_PREVIEW kökü");

            GameObject[] sharedHomes = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name == "StoryHome_Shared")
                .Select(transform => transform.gameObject)
                .ToArray();
            Require(sharedHomes.Length == 1, "Tam olarak bir ortak ev instance'ı");
            GameObject sharedHome = sharedHomes[0];
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome);
            Require(sourcePath == StorySharedHomePrefabBuilder.PrefabPath,
                "Ortak ev bağlantılı StoryHome_Shared prefab instance'ı");
            Require(sharedHome.transform.localPosition.sqrMagnitude < 0.0001f, "Ortak ev local position identity");
            Require(Quaternion.Angle(sharedHome.transform.localRotation, Quaternion.identity) < 0.01f,
                "Ortak ev local rotation identity");
            Require((sharedHome.transform.localScale - Vector3.one).sqrMagnitude < 0.0001f,
                "Ortak ev local scale identity");

            Require(Find(root.transform, "PreparationSetDressing") != null, "Story 01 sahne giydirme kökü");
            Require(Find(root.transform, "FamilyPlanBoard") != null, "Aile plan panosu");
            Require(Find(root.transform, "EmergencyBag_Open_Packing") != null, "Fiziksel açık afet çantası");
            Require(Find(root.transform, "OriginalBolum1BagSource") != null,
                "Procedural kutu yerine orijinal Bölüm 1 açık çanta modeli");
            Require(Find(root.transform, "PhysicalBagOpening")?.GetComponent<BagDropZone>() != null,
                "Doğrudan bırakma hedefi");

            CinemachineCamera[] cameras = root.GetComponentsInChildren<CinemachineCamera>(true);
            Require(cameras.Length >= 10, "En az 10 bestelenmiş Cinemachine kamera");
            Require(cameras.All(camera =>
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f),
                "Kamera lensleri 38–50 derece aralığında");

            Require(root.GetComponentsInChildren<StoryTouchManager>(true).Length == 1, "Tek StoryTouchManager");
            Require(root.GetComponentsInChildren<StoryPlayerMovement>(true).Length == 1, "Tek StoryPlayerMovement");
            StoryPreparationDirector preparationDirector =
                root.GetComponentsInChildren<StoryPreparationDirector>(true).SingleOrDefault();
            Require(preparationDirector != null, "Tek StoryPreparationDirector");
            Require(preparationDirector.RevisedFlow, "Fiziksel keşif ve çanta kontrolü kullanan yeni hazırlık akışı");
            Require(root.GetComponentsInChildren<StoryActionButton>(true).Length == 0,
                "Ortada görev/aksiyon butonu yok");

            StoryInteractable[] interactions = root.GetComponentsInChildren<StoryInteractable>(true);
            Require(interactions.Length >= 34, "Aile planı, keşif, eşya, araç testi, çatışma ve karanlık prova etkileşimleri");
            Require(interactions.All(interaction => interaction.HighlightRoot != null),
                "Her aktif görev nesnesinde sahne içi hedef işaretçisi");
            Require(interactions.All(interaction =>
                    interaction.GetComponentsInChildren<Renderer>(true).Length > 0),
                "Her etkileşim görünür gerçek nesnenin collider'ına bağlıdır; boş hotspot yoktur");
            StoryInteractable firstMission = Find(root.transform, "FamilyMeetingPointCard_Drag")
                ?.GetComponent<StoryInteractable>();
            Require(firstMission?.HighlightRoot != null &&
                    Find(firstMission.HighlightRoot.transform, "FirstMissionTargetGuide") == null,
                "İlk görev işareti okunacak kartı gösterir; doğru pano bölümünü ele vermez");
            Require(interactions.Count(interaction =>
                    interaction.InteractionGesture == StoryInteractionGesture.DragToBag) >= 13,
                "Masaya alınan haberleşme araçları dahil 13 nesne çantaya sürüklenebilir");
            string[] signalItemIds =
            {
                "Pack_Flashlight",
                "Pack_Batteries",
                "Pack_Whistle",
                "Pack_Radio"
            };
            Require(signalItemIds.All(id => interactions.Any(interaction =>
                    interaction.InteractionId == id &&
                    interaction.InteractionGesture == StoryInteractionGesture.DragToBag &&
                    interaction.FocusCameraZone == StoryCameraZoneId.PreparationBag)),
                "Masaya alınan dört haberleşme aracı çantaya sürüklenir");
            string[] signalDrawerItemIds =
            {
                "Take_Flashlight",
                "Take_Batteries",
                "Take_Whistle",
                "Take_Radio"
            };
            Require(signalDrawerItemIds.All(id => interactions.Any(interaction =>
                    interaction.InteractionId == id &&
                    interaction.InteractionGesture == StoryInteractionGesture.Tap &&
                    interaction.FocusCameraZone == StoryCameraZoneId.PreparationSignal)),
                "Dört haberleşme aracı açık çekmecenin içinden masaya alınır");
            Require(interactions.Where(interaction =>
                    interaction.InteractionGesture == StoryInteractionGesture.DragToBag)
                    .All(interaction => interaction.InteractFromAnywhere),
                "Çanta eşyaları merkez buton gerektirmeden doğrudan tutulabilir");

            string[] discoveryNames =
            {
                "Discover_SignalDrawer",
                "Discover_FoodCabinet",
                "Discover_HealthDrawer",
                "Discover_WarmthChest"
            };
            StoryInteractable[] discoveries = discoveryNames
                .Select(id => interactions.SingleOrDefault(interaction =>
                    interaction.InteractionId == id))
                .ToArray();
            Require(discoveries.All(interaction => interaction != null), "Dört fiziksel mobilya keşfi");
            Require(discoveries[0].InteractionGesture == StoryInteractionGesture.SwipeDown &&
                    discoveries.Skip(1).All(interaction =>
                        interaction.InteractionGesture == StoryInteractionGesture.SwipeHorizontal) &&
                    discoveries.All(interaction =>
                        interaction.InteractFromAnywhere && interaction.GestureTarget == null),
                "Çekmece aşağı çekilir; diğer mobilyalar doğrudan yatay sürükleme kullanır");
            Transform signalDrawer = Find(root.transform, "SignalNightstandOpen")
                ?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "Nightstand_02_Door");
            Require(signalDrawer != null &&
                    signalDrawer.GetComponent<Animation>()?.clip != null &&
                    AssetDatabase.GetAssetPath(signalDrawer.GetComponent<Animation>().clip) ==
                    "Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim",
                "Sinyal çekmecesi sahneye yazılmış gerçek açılma animasyonu taşır");
            Require(signalDrawerItemIds.All(id =>
                interactions.Single(interaction => interaction.InteractionId == id)
                    .transform.IsChildOf(signalDrawer)),
                "Çekmece seçim nesneleri hareketli çekmece tablasına bağlıdır");
            Require(signalDrawerItemIds.All(id =>
                Mathf.Abs(interactions.Single(interaction => interaction.InteractionId == id)
                    .transform.position.y - 0.47f) < 0.02f),
                "Haberleşme araçlarının tabanı çekmece içinde oturur; havada değildir");
            Require(signalItemIds.All(id =>
            {
                StoryInteractable interaction =
                    interactions.Single(candidate => candidate.InteractionId == id);
                Animation animation = interaction.GetComponent<Animation>();
                return !interaction.transform.IsChildOf(signalDrawer) &&
                       animation?.clip != null &&
                       AssetDatabase.GetAssetPath(animation.clip) ==
                       "Assets/Story/Generated/Animations/Chapters/Story01_SignalStage_" +
                       id.Substring("Pack_".Length) + ".anim";
            }), "Çekmece seçimleri masadaki ayrı sürüklenebilir eşyalara sahne animasyonuyla taşınır");
            StoryInteractable waterDate =
                Find(root.transform, "WaterExpiryLabel_Unchecked")?.GetComponent<StoryInteractable>();
            StoryInteractable bandageSeal =
                Find(root.transform, "BandageSealInspection")?.GetComponent<StoryInteractable>();
            Require(waterDate != null &&
                    waterDate.InteractionGesture == StoryInteractionGesture.SwipeHorizontal &&
                    waterDate.GestureTarget != null &&
                    waterDate.OnInteracted.GetPersistentEventCount() >= 3,
                "Su şişesinin tarihi nesnenin üzerinde yana çevrilerek kontrol edilir");
            Require(bandageSeal != null &&
                    bandageSeal.InteractionGesture == StoryInteractionGesture.WorldHold &&
                    bandageSeal.OnInteracted.GetPersistentEventCount() >= 3,
                "Kapalı sargı paketinin mührü nesnenin üzerinde basılı tutularak kontrol edilir");
            Require(Find(root.transform, "WaterExpiryCheckedState") != null &&
                    Find(root.transform, "BandageSealCheckedState") != null,
                "Her iki kontrol de yalnız renge bağlı olmayan fiziksel sonuç görseli üretir");

            string[] reviewNames =
            {
                "Inspect_FlashlightSwitchOn",
                "Inspect_FlashlightSwitchOff",
                "Review_TuneEmergencyRadio",
                "Review_ClipWhistleToCan"
            };
            Require(reviewNames.All(name =>
                    interactions.Any(interaction => interaction.InteractionId == name)),
                "Fener, radyo ve düdük görünür gerçek nesneleri üzerinden kontrol edilir");
            Require(new[]
                {
                    "Review_CloseFoodPocket",
                    "Review_SealDocumentPouch",
                    "Review_CloseMainZipper"
                }.All(id => interactions.All(interaction => interaction.InteractionId != id)),
                "Modelde bulunmayan cep, mühür ve fermuar için sahte hotspot üretilmez");
            Require(Find(root.transform, "MelekContactCard_Drag")?.GetComponent<DraggableItem>() != null &&
                    Find(root.transform, "CanWhistleRoleCard_Drag")?.GetComponent<DraggableItem>() != null,
                "Aile planında toplanma alanı, iletişim kişisi ve Can'ın görevi ayrı fiziksel kartlardır");
            StoryInteractable consoleRemoval =
                Find(root.transform, "ConsoleConflict_Draggable")?.GetComponent<StoryInteractable>();
            StoryInteractable physicalBagLift = interactions.SingleOrDefault(interaction =>
                interaction.InteractionId == "Final_TestBagWeight");
            Require(consoleRemoval != null &&
                    consoleRemoval.InteractionGesture == StoryInteractionGesture.DragToTarget &&
                    consoleRemoval.GetComponent<DraggableItem>() != null &&
                    physicalBagLift != null &&
                    physicalBagLift.gameObject == Find(root.transform, "EmergencyBag_Open_Packing")?.gameObject,
                "Konsol çatışması görünür açık çanta kaldırılarak ve konsol fiziksel sürüklenerek çözülür");
            StoryInteractable comfort =
                Find(root.transform, "CanComfortToy_Draggable")?.GetComponent<StoryInteractable>();
            Require(comfort != null &&
                    comfort.InteractionGesture == StoryInteractionGesture.DragToTarget &&
                    comfort.GetComponent<DraggableItem>() != null,
                "Can'ın rahatlatıcı eşyası doğrudan ona sürüklenir");

            StoryPreparationItem[] preparationItems = root.GetComponentsInChildren<StoryPreparationItem>(true);
            Require(preparationItems.Length == 13, "10 gerekli ve 3 güvenli yanlış seçim nesnesi");
            Require(preparationItems.Count(item => item.Recommended) == 10, "10 gerekli çanta eşyası");
            Require(preparationItems.Count(item => !item.Recommended) == 3, "3 ramak kala seçimi");
            Require(preparationItems.Where(item => item.Recommended).All(item => item.Flag != StoryFlag.None),
                "Her gerekli eşya kalıcı StoryFlag üretir");
            Require(preparationItems.Where(item => item.Recommended).Select(item => item.Flag).Distinct().Count() == 10,
                "Gerekli eşya StoryFlag'leri benzersiz");
            Require(Enum.GetValues(typeof(StoryPreparationCategory)).Cast<StoryPreparationCategory>()
                    .All(category => preparationItems.Any(item => item.Recommended && item.Category == category)),
                "Dört hazırlık kategorisinin tamamı oynanabilir");

            PlayableDirector blackout = root.GetComponentsInChildren<PlayableDirector>(true)
                .SingleOrDefault(candidate => candidate.name == "PreparationBlackoutDrill");
            Require(blackout != null && blackout.playableAsset != null, "Sahne tabanlı karanlık prova Timeline'ı");
            Require(AssetDatabase.GetAssetPath(blackout.playableAsset) ==
                    "Assets/Story/Generated/Timelines/Story_01_BlackoutDrill.playable",
                "Karanlık prova Timeline asset bağlantısı");
            StoryInteractable blackoutCan =
                interactions.SingleOrDefault(interaction =>
                    interaction.InteractionId == "Blackout_FindCan");
            StoryInteractable blackoutFlashlight =
                interactions.SingleOrDefault(interaction =>
                    interaction.InteractionId == "Blackout_RetrieveFlashlight");
            StoryInteractable blackoutPlanFocus =
                interactions.SingleOrDefault(interaction =>
                    interaction.InteractionId == "Blackout_FocusFamilyPlan");
            StoryInteractable blackoutTableFocus =
                interactions.SingleOrDefault(interaction =>
                    interaction.InteractionId == "Blackout_FocusSafeTable");
            StoryInteractable blackoutWhistle =
                Find(root.transform, "Blackout_Whistle")?.GetComponent<StoryInteractable>();
            Require(blackoutCan != null &&
                    blackoutCan.InteractionGesture == StoryInteractionGesture.WorldHold,
                "Karanlıkta Can doğrudan omzunda basılı tutularak bulunur");
            Require(blackoutFlashlight != null &&
                    blackoutFlashlight.InteractionGesture == StoryInteractionGesture.DragToTarget &&
                    blackoutFlashlight.GestureTarget != null &&
                    blackoutFlashlight.GetComponent<DraggableItem>() != null,
                "Karanlıkta görünür fener çantadan Deniz'in eline fiziksel sürüklenir");
            Require(blackoutPlanFocus != null &&
                    blackoutPlanFocus.InteractionGesture == StoryInteractionGesture.WorldHold &&
                    blackoutTableFocus != null &&
                    blackoutTableFocus.InteractionGesture == StoryInteractionGesture.WorldHold,
                "Karanlıkta fener aile planı ve güvenli masa üstünde doğrudan yönlendirilir");
            Require(new[]
                {
                    "BlackoutBeam_FamilyPlan",
                    "BlackoutBeam_SafeTable",
                    "BlackoutBeam_Can"
                }.All(name => Find(root.transform, name)?.GetComponent<Light>()?.type == LightType.Spot),
                "Üç hazırlıklı fener doğrultusunun sahne Spot Light'ları");
            Require(blackoutWhistle != null &&
                    blackoutWhistle.InteractionGesture == StoryInteractionGesture.RepeatedTap &&
                    blackoutWhistle.RequiredGestureCount == 3,
                "Karanlık prova üç fiziksel düdük dokunuşu ister");

            Require(Find(root.transform, "SignalStation") == null, "Kategori masası SignalStation kaldırıldı");
            Require(Find(root.transform, "FoodStation") == null, "Kategori masası FoodStation kaldırıldı");
            Require(Find(root.transform, "HealthStation") == null, "Kategori masası HealthStation kaldırıldı");
            Require(Find(root.transform, "WarmthLabel") == null, "Dünya kategori etiketi kaldırıldı");

            NavMeshSurface surface = root.GetComponentInChildren<NavMeshSurface>(true);
            Require(surface != null && surface.navMeshData != null, "Sahneye özel baked NavMesh");
            Require(EditorBuildSettings.scenes.Any(entry =>
                    entry.enabled && entry.path == StoryPreparationRebuildPreviewBuilder.ScenePath),
                "Story 01 yayın sahnesi Build Settings'te");

            Debug.Log(
                $"Story_01_RebuildPreview doğrulandı: cameras={cameras.Length}, " +
                $"interactions={interactions.Length}, navMesh={surface.navMeshData.name}");
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 01 rebuild önizlemesi yapısal kalite kapısını geçti.",
                    "Tamam");
        }
        finally
        {
            if (openedForValidation && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Transform Find(Transform root, string name)
    {
        return StorySharedHomePrefabBuilder.FindDescendant(root, name);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("Story_01_RebuildPreview doğrulama hatası: " + label);
    }
}
