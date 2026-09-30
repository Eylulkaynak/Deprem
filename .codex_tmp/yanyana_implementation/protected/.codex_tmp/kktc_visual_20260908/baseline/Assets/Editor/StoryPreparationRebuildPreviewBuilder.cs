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

    // The old 3.27 m ceiling underside left only 14.5 cm above the family-plan
    // frame, making the room read like a compressed diorama. Keep this authored
    // height shared by the room shell, corridor shell and their practical fixtures.
    private const float PreparationCeilingCenterY = 4.10f;
    private const float PreparationCeilingBottomY = 4.03f;
    private const float PreparationCeilingInsetY = 4.015f;
    private const float PreparationCrownY = 3.89f;
    // The authored Synty discovery cabinet already owns a worktop face. The new
    // continuous slab must sit clearly above it instead of sharing the exact same
    // depth plane; even a coplanar underside can shimmer in the moving Scene View.
    private const float PreparationKitchenWorktopClearanceY = 0.004f;
    private const float PreparationKitchenWorktopThickness = 0.04f;
    private const float PreparationKitchenWorktopSurfaceOffsetY =
        PreparationKitchenWorktopClearanceY + PreparationKitchenWorktopThickness;
    // Screen-right on the right-hand wall is negative world Z. Move the complete
    // storage rhythm together so the exit console clears the back-wall return
    // without collapsing the spacing between console, mudroom and tall shelf.
    private const float PreparationRightWallCompositionOffsetZ = -0.32f;

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
    private const string PlanBoardPanelSpritePath =
        "Assets/Story/UI/ThirdParty/KenneyUIAdventure/panel_brown.png";
    private const string PlanCardPanelSpritePath =
        "Assets/Story/UI/ThirdParty/CartoonUIPack/basic white/pop up window.png";
    private const string PlanSlotHeaderSpritePath =
        "Assets/Story/UI/ThirdParty/KenneyUIAdventure/button_grey.png";
    private const string PlanHeaderRibbonSpritePath =
        "Assets/Story/UI/ThirdParty/KenneyUIAdventure/button_brown.png";
    private const string PlanBoardSpriteMaterialPath =
        "Assets/Story/Generated/Materials/Story01_PlanBoardSprite.mat";
    private const string PlanSocketMaterialPath =
        "Assets/Story/Generated/Materials/Story01_PlanSocketGhost.mat";
    private const string PlanCardSnapClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_PlanCardSnap.anim";
    private const string PlanSocketPulseClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_PlanSocketPulse.anim";
    private const string SignalDrawerOpenClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim";
    private const string KitchenCabinetOpenClipPath =
        "Assets/Story/Generated/Animations/Chapters/Story01_KitchenCabinetOpen.anim";
    private const string SignalStageClipPrefix =
        "Assets/Story/Generated/Animations/Chapters/Story01_SignalStage_";

    private sealed class PreviewWorld
    {
        internal GameObject environment;
        internal GameObject sharedHome;
        internal GameObject planCardTable;
        internal GameObject exitBagShelf;
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
        internal GameObject radioReviewModel;
        internal GameObject radioBatteryLoose;
        internal GameObject radioBatteryInserted;
        internal GameObject whistleOnCan;
        internal GameObject whistleCanTarget;
        internal Transform whistleCanPose;
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
        internal StoryInteractable reviewSignal;
        internal StoryInteractable reviewSignalFlashlightOff;
        internal StoryInteractable reviewSignalRadioBatteryInsert;
        internal StoryInteractable reviewSignalRadio;
        internal StoryInteractable reviewSignalRadioBatteryRemove;
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
        internal Transform exitShelfApproachPoint;
        internal GameObject exitShelfDropRing;
        internal Transform comfortCanPose;
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

    [MenuItem("Tools/Deprem Story/Build Story_01 Rebuild Preview (OVERWRITES SCENE)")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Story 01 sahnesi yeniden kurulacak",
                "Bu işlem Story_01_RebuildPreview sahnesini sıfırdan üretir ve elle yaptığınız " +
                "sahne düzenlemelerini ezer. Yalnız bunu bilerek istediğinizde devam edin.",
                "Rebuild Et",
                "İptal"))
            return;
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

    private static void Build(bool showDialog)
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
                new Vector3(-1.9f, 0f, -2.25f),
                new Vector3(-3.45f, 0f, -2.45f),
                new Vector3(-2.75f, 0f, -1.15f),
                adultController);
            // Opening family blocking: the children stand clear of the sofa and
            // all three characters visibly address one another instead of facing
            // the same wall in a scattered top-down tableau.
            FaceMeshyCharacterAt(family.deniz.transform, family.parent.transform.position);
            FaceMeshyCharacterAt(family.can.transform, family.parent.transform.position);
            FaceMeshyCharacterAt(
                family.parent.transform,
                (family.deniz.transform.position + family.can.transform.position) * 0.5f);
            // BuildFamily copies the legacy worn prop before this chapter turns Deniz toward
            // the family. Re-seat it after the final facing is authored so it cannot rotate
            // through the torso when the humanoid spine evaluates its rest pose.
            Transform inheritedWornBag = family.deniz.GetComponentsInChildren<Transform>(true)
                .First(candidate => candidate.name == "Deniz_WornEmergencyBag");
            StoryChapterBuilderCommon.MountEmergencyBackpackToTorso(
                family.deniz,
                inheritedWornBag.gameObject,
                family.deniz.transform.rotation,
                -0.015f,
                0.018f);
            BuildFinalBagStates(root.transform, materials, world, family.deniz.transform);

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
            StoryChapterBuilderCommon.ConfigureDialogueActors(
                ui.controller,
                new StoryChapterBuilderCommon.DialogueActorSpec(family.deniz, "Deniz"),
                new StoryChapterBuilderCommon.DialogueActorSpec(family.can, "Can"),
                new StoryChapterBuilderCommon.DialogueActorSpec(family.parent, "Anne", "Ayşe", "Ayse"));
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

            StoryChapterBuilderCommon.BuildLighting(root.transform, volume, new Color(1f, 0.88f, 0.72f), 1.05f);
            ConfigurePreparationLighting(root.transform);
            BuildBlackoutDrillTimeline(
                root.transform,
                storyDirector,
                touch,
                ui.controller,
                interactions,
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
            // NavMeshData GUID bağlantısı synchronous import sırasında yeniden eşlenebilir.
            // Son save, sahnenin transient in-memory bake yerine asset referansını taşımasını garanti eder.
            EditorSceneManager.SaveScene(scene, ScenePath);

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
        StoryChapterBuilderCommon.ApplyCanonicalIndoorHomeShell(world.sharedHome.transform);

        Transform dressing = StoryChapterBuilderCommon.NewChild(world.environment.transform, "PreparationSetDressing");
        BuildFamilyPlanBoard(dressing, materials, world);
        BuildDiscoveryFurniture(dressing, materials, world);
        BuildOpenBag(dressing, materials, world);
        BuildWrongChoiceConsequence(dressing, materials, world);
        BuildPreparationInteriorArtPass(world.environment.transform, materials, world, true);
        return world;
    }

    internal static void BuildCanonicalLivedInHomeArtForChapter(
        Transform environment,
        GameObject sharedHome,
        GameObject exitBagShelf)
    {
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));
        if (sharedHome == null)
            throw new ArgumentNullException(nameof(sharedHome));
        if (exitBagShelf == null)
            throw new ArgumentNullException(nameof(exitBagShelf));

        StoryChapterBuilderCommon.Materials materials = StoryChapterBuilderCommon.CreateMaterials();
        Transform fixtures = StoryChapterBuilderCommon.NewChild(
            environment,
            "CanonicalHomeContinuityFurniture");
        PreviewWorld continuityWorld = new PreviewWorld
        {
            environment = environment.gameObject,
            sharedHome = sharedHome,
            exitBagShelf = exitBagShelf
        };
        continuityWorld.planCardTable = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "FamilyPlanCardTable_Continuity",
            fixtures,
            new Vector3(-4.12f, 0f, 1.08f),
            new Vector3(0.92f, 0.72f, 2.05f),
            new Vector3(0f, 90f, 0f));
        FitPlanCardTableSurface(continuityWorld.planCardTable);
        continuityWorld.signalNightstandClosed = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Nightstand_02.prefab",
            "SignalNightstand_Continuity",
            fixtures,
            new Vector3(-0.76f, 0f, -3.72f),
            new Vector3(0.78f, 0.72f, 0.62f),
            Vector3.zero);

        Material syntyTownMaterial = AssetDatabase.LoadAssetAtPath<Material>(SyntyTownMaterialPath);
        if (syntyTownMaterial == null)
            throw new InvalidOperationException("POLYGON Town dolap materyali bulunamadı: " + SyntyTownMaterialPath);
        continuityWorld.foodCabinetClosed = StoryChapterBuilderCommon.InstantiateAsset(
            SyntyKitchenCounterPath,
            "KitchenLowCabinet_Continuity",
            fixtures,
            new Vector3(4.62f, 0f, -1.82f),
            new Vector3(0.92f, 1.08f, 1.5f),
            new Vector3(0f, 270f, 0f),
            false,
            false,
            syntyTownMaterial);

        // The damaged corridor is chapter-specific, but every indoor chapter must
        // inherit the exact finished Story 01 room, kitchen, lounge and entry layer.
        BuildPreparationInteriorArtPass(environment, materials, continuityWorld, false);
    }

    private static void BuildPreparationInteriorArtPass(
        Transform environment,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world,
        bool includeCorridorArt)
    {
        Transform art = StoryChapterBuilderCommon.NewChild(environment, "PreparationInteriorArtPass");

        // Story 01 is a real family home, not an open-top diorama. The ceiling has no
        // collider so it cannot alter navigation or push a fixed camera, but it closes
        // every portrait composition that previously exposed the blue skybox.
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_CeilingMain",
            PrimitiveType.Cube,
            new Vector3(0f, PreparationCeilingCenterY, 0.25f),
            new Vector3(9.76f, 0.14f, 11.26f),
            materials.cream,
            art,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_CeilingInset",
            PrimitiveType.Cube,
            new Vector3(0f, PreparationCeilingInsetY, 0.25f),
            new Vector3(8.96f, 0.035f, 10.46f),
            materials.white,
            art,
            false);

        // The shared-home shell deliberately leaves its camera-facing side open. That works for
        // the default doll-house shot, but Story 03's quake close-up looks back across that edge and
        // used to expose a huge strip of sky behind the children. This visual-only inner wall closes
        // the composition without adding a collider or changing either chapter's navigation.
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_CameraSideInteriorBackdrop",
            PrimitiveType.Cube,
            new Vector3(0f, PreparationCeilingBottomY * 0.5f, -5.58f),
            new Vector3(9.76f, PreparationCeilingBottomY, 0.12f),
            materials.wall,
            art,
            false);
        CreatePreparationTrim(
            "Story01_CameraSideWainscot",
            new Vector3(0f, 0.84f, -5.505f),
            new Vector3(9.5f, 1.32f, 0.035f),
            materials.cream,
            art);
        CreatePreparationTrim(
            "Story01_CameraSideChairRail",
            new Vector3(0f, 1.52f, -5.475f),
            new Vector3(9.56f, 0.12f, 0.055f),
            materials.teal,
            art);
        CreatePreparationTrim(
            "Story01_CameraSideCrown",
            new Vector3(0f, PreparationCrownY, -5.49f),
            new Vector3(9.62f, 0.12f, 0.07f),
            materials.wood,
            art);

        // Architectural depth: crown, chair rail and lower wall panels break the old
        // single grey slab into a warm, inhabited room. All are visual-only and stay
        // flush to the structural collider walls authored by the shared-home builder.
        CreatePreparationTrim("Story01_Crown_Left", new Vector3(-4.79f, PreparationCrownY, 0.25f),
            new Vector3(0.14f, 0.18f, 11.12f), materials.wood, art);
        CreatePreparationTrim("Story01_Crown_Right", new Vector3(4.79f, PreparationCrownY, 0.25f),
            new Vector3(0.14f, 0.18f, 11.12f), materials.wood, art);
        CreatePreparationTrim("Story01_Crown_BackLeft", new Vector3(-1.8f, PreparationCrownY, 5.79f),
            new Vector3(6.25f, 0.18f, 0.14f), materials.wood, art);
        CreatePreparationTrim("Story01_Crown_BackRight", new Vector3(4.25f, PreparationCrownY, 5.79f),
            new Vector3(1.25f, 0.18f, 0.14f), materials.wood, art);
        CreatePreparationTrim("Story01_Wainscot_Left", new Vector3(-4.865f, 0.84f, 0.25f),
            new Vector3(0.035f, 1.32f, 10.9f), materials.cream, art);
        CreatePreparationTrim("Story01_Wainscot_Right", new Vector3(4.865f, 0.84f, 0.25f),
            new Vector3(0.035f, 1.32f, 10.9f), materials.cream, art);
        CreatePreparationTrim("Story01_ChairRail_Left", new Vector3(-4.825f, 1.52f, 0.25f),
            new Vector3(0.09f, 0.12f, 11.0f), materials.teal, art);
        CreatePreparationTrim("Story01_ChairRail_Right", new Vector3(4.825f, 1.52f, 0.25f),
            new Vector3(0.09f, 0.12f, 11.0f), materials.teal, art);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_CeilingBeam_Lounge", -3f),
                     ("Story01_CeilingBeam_Center", 0.25f),
                     ("Story01_CeilingBeam_Entry", 3.5f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(0f, 3.95f, z),
                new Vector3(8.96f, 0.055f, 0.12f),
                materials.wood,
                art);
        }
        foreach ((string name, float x) in new[]
                 {
                     ("Story01_CeilingBeam_West", -2.85f),
                     ("Story01_CeilingBeam_Spine", 0.35f),
                     ("Story01_CeilingBeam_East", 2.85f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(x, 3.945f, 0.25f),
                new Vector3(0.12f, 0.06f, 10.46f),
                materials.wood,
                art);
        }
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_CeilingMedallion",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 3.93f, 0.45f),
            new Vector3(0.46f, 0.025f, 0.46f),
            materials.amber,
            art,
            false);

        BuildPreparationPlanWallFeature(art, materials);
        BuildPreparationKitchenArt(art, materials, world);
        BuildPreparationLoungeArt(art, materials, world);
        RebuildPreparationStorageVisuals(materials, world);
        BuildPreparationEntryArt(art, materials, world);
        if (includeCorridorArt)
            BuildPreparationCorridorArt(art, materials);
        BuildPreparationWallGallery(art);
        BuildPreparationPracticalLighting(art, materials);
    }

    private static void BuildPreparationPlanWallFeature(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform feature = StoryChapterBuilderCommon.NewChild(parent, "Story01_FamilyPlanFeatureWall");
        const float panelX = -4.805f;
        const float panelY = 1.88f;
        const float panelZ = 1.15f;
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_FamilyPlanBackdrop",
            PrimitiveType.Cube,
            new Vector3(panelX, panelY, panelZ),
            new Vector3(0.035f, 2.48f, 3.7f),
            materials.cream,
            feature,
            false);
        foreach ((string name, Vector3 position, Vector3 scale) in new[]
                 {
                     ("Story01_FamilyPlanFrame_Top", new Vector3(-4.775f, 3.08f, panelZ),
                         new Vector3(0.07f, 0.09f, 3.78f)),
                     ("Story01_FamilyPlanFrame_Bottom", new Vector3(-4.775f, 0.68f, panelZ),
                         new Vector3(0.07f, 0.09f, 3.78f)),
                     ("Story01_FamilyPlanFrame_Left", new Vector3(-4.775f, panelY, -0.7f),
                         new Vector3(0.07f, 2.48f, 0.09f)),
                     ("Story01_FamilyPlanFrame_Right", new Vector3(-4.775f, panelY, 3f),
                         new Vector3(0.07f, 2.48f, 0.09f))
                 })
        {
            CreatePreparationTrim(name, position, scale, materials.wood, feature);
        }

        // Small colour blocks make the wall read as a deliberately authored family
        // command centre instead of one board floating on a grey structural wall.
        foreach ((string name, float z, Material material) in new[]
                 {
                     ("Story01_FamilyPlanTab_Signal", -0.38f, materials.amber),
                     ("Story01_FamilyPlanTab_Food", -0.06f, materials.coral),
                     ("Story01_FamilyPlanTab_FirstAid", 2.36f, materials.teal),
                     ("Story01_FamilyPlanTab_Exit", 2.68f, materials.amber)
                 })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                name,
                PrimitiveType.Cube,
                new Vector3(-4.745f, 1.02f, z),
                new Vector3(0.045f, 0.16f, 0.22f),
                material,
                feature,
                false);
        }
    }

    private static void BuildPreparationKitchenArt(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        Transform kitchen = StoryChapterBuilderCommon.NewChild(parent, "Story01_KitchenZone");
        Bounds discoveryCabinetBounds = GetVisibleBounds(world.foodCabinetClosed);
        float counterTopY = discoveryCabinetBounds.max.y;
        float worktopBottomY = counterTopY + PreparationKitchenWorktopClearanceY;
        float worktopSurfaceY = counterTopY + PreparationKitchenWorktopSurfaceOffsetY;
        float baseRunTopY = counterTopY - PreparationKitchenWorktopClearanceY;
        const float runCenterZ = -1.75f;
        const float runWidth = 3.75f;
        float backsplashHeight = Mathf.Max(0.5f, 1.68f - worktopSurfaceY);
        float backsplashY = worktopSurfaceY + backsplashHeight * 0.5f;

        // One continuous working wall makes the food-discovery cabinet read as a
        // module of a real kitchen rather than a lone bedside cabinet. The rear body
        // is also the single navigation blocker for the sink, discovery and oven run;
        // its front face stays behind the interactive Synty doors.
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_KitchenBacksplash",
            PrimitiveType.Cube,
            new Vector3(4.855f, backsplashY, runCenterZ),
            new Vector3(0.04f, backsplashHeight, runWidth),
            materials.teal,
            kitchen,
            false);
        for (int index = 0; index < 11; index++)
        {
            CreatePreparationTrim(
                "Story01_KitchenTileJoint_" + index,
                new Vector3(4.825f, backsplashY, 0.125f - index * 0.375f),
                new Vector3(0.025f, backsplashHeight, 0.018f),
                materials.cream,
                kitchen);
        }
        foreach (float normalizedY in new[] { 0.33f, 0.67f })
        {
            float y = worktopSurfaceY + backsplashHeight * normalizedY;
            CreatePreparationTrim(
                "Story01_KitchenTileRow_" + Mathf.RoundToInt(normalizedY * 100f),
                // Pull horizontal grout 5 mm toward the room so its front face is
                // not coplanar with the vertical joints at every crossing.
                new Vector3(4.82f, y, runCenterZ),
                new Vector3(0.025f, 0.018f, runWidth),
                materials.cream,
                kitchen);
        }

        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_KitchenBaseRunBlocker",
            PrimitiveType.Cube,
            new Vector3(4.69f, baseRunTopY * 0.5f, runCenterZ),
            new Vector3(0.38f, baseRunTopY, runWidth),
            materials.cream,
            kitchen,
            true);
        CreatePreparationTrim(
            "Story01_KitchenContinuousCounter",
            new Vector3(4.54f, worktopBottomY + PreparationKitchenWorktopThickness * 0.5f,
                runCenterZ),
            new Vector3(0.66f, PreparationKitchenWorktopThickness, runWidth + 0.08f),
            materials.dark,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenContinuousPlinth",
            new Vector3(4.43f, 0.07f, runCenterZ),
            new Vector3(0.16f, 0.14f, runWidth - 0.08f),
            materials.navy,
            kitchen);

        // Sink module at the open end of the run.
        const float sinkZ = -0.5f;
        CreatePreparationTrim(
            "Story01_KitchenSinkBaseFace",
            new Vector3(4.37f, counterTopY * 0.5f, sinkZ),
            new Vector3(0.07f, counterTopY - 0.16f, 1.05f),
            materials.wood,
            kitchen);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_KitchenSinkDoor_Left", sinkZ - 0.265f),
                     ("Story01_KitchenSinkDoor_Right", sinkZ + 0.265f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(4.325f, counterTopY * 0.49f, z),
                new Vector3(0.025f, counterTopY - 0.25f, 0.47f),
                materials.cream,
                kitchen);
            CreatePreparationTrim(
                name + "_Handle",
                new Vector3(4.3f, counterTopY * 0.68f, z),
                new Vector3(0.025f, 0.04f, 0.16f),
                materials.metal,
                kitchen);
        }
        CreatePreparationTrim(
            "Story01_KitchenSinkRim",
            new Vector3(4.51f, worktopSurfaceY + 0.0125f, sinkZ),
            new Vector3(0.4f, 0.025f, 0.72f),
            materials.metal,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenSinkBasin",
            new Vector3(4.49f, worktopSurfaceY + 0.027f, sinkZ),
            new Vector3(0.31f, 0.026f, 0.58f),
            materials.navy,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenFaucetStem",
            new Vector3(4.74f, worktopSurfaceY + 0.17f, sinkZ),
            new Vector3(0.045f, 0.32f, 0.045f),
            materials.metal,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenFaucetSpout",
            new Vector3(4.63f, worktopSurfaceY + 0.31f, sinkZ),
            new Vector3(0.26f, 0.045f, 0.045f),
            materials.metal,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenFaucetDrop",
            new Vector3(4.51f, worktopSurfaceY + 0.265f, sinkZ),
            new Vector3(0.045f, 0.13f, 0.045f),
            materials.metal,
            kitchen);

        // A slim pull-out closes the otherwise visible dead strip between the
        // story cabinet and the cooker. It reads as usable storage, not a patch.
        const float pullOutZ = -2.43f;
        CreatePreparationTrim(
            "Story01_KitchenPullOutBase",
            new Vector3(4.57f, (counterTopY - 0.12f) * 0.5f, pullOutZ),
            new Vector3(0.52f, counterTopY - 0.12f, 0.36f),
            materials.wood,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenPullOutFront",
            new Vector3(4.28f, counterTopY * 0.46f, pullOutZ),
            new Vector3(0.065f, counterTopY * 0.72f, 0.29f),
            materials.cream,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenPullOutHandle",
            new Vector3(4.235f, counterTopY * 0.72f, pullOutZ),
            new Vector3(0.035f, 0.04f, 0.18f),
            materials.metal,
            kitchen);

        // Oven and four-burner hob close the run before the refrigerator.
        const float ovenZ = -3.08f;
        CreatePreparationTrim(
            "Story01_KitchenOvenBody",
            new Vector3(4.57f, (counterTopY - 0.12f) * 0.5f, ovenZ),
            new Vector3(0.52f, counterTopY - 0.12f, 0.98f),
            materials.wood,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenOvenDoor",
            new Vector3(4.28f, counterTopY * 0.42f, ovenZ),
            new Vector3(0.065f, counterTopY * 0.56f, 0.78f),
            materials.navy,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenOvenWindow",
            new Vector3(4.24f, counterTopY * 0.43f, ovenZ),
            new Vector3(0.025f, counterTopY * 0.33f, 0.58f),
            materials.glass,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenOvenHandle",
            new Vector3(4.2f, counterTopY * 0.72f, ovenZ),
            new Vector3(0.04f, 0.045f, 0.62f),
            materials.metal,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenOvenControls",
            new Vector3(4.26f, counterTopY * 0.82f, ovenZ),
            new Vector3(0.045f, 0.16f, 0.84f),
            materials.cream,
            kitchen);
        foreach (float z in new[] { ovenZ - 0.28f, ovenZ - 0.09f, ovenZ + 0.09f, ovenZ + 0.28f })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "Story01_KitchenOvenKnob_" + Mathf.RoundToInt((z - ovenZ + 0.4f) * 100f),
                PrimitiveType.Sphere,
                new Vector3(4.215f, counterTopY * 0.82f, z),
                Vector3.one * 0.055f,
                materials.coral,
                kitchen,
                false);
        }
        CreatePreparationTrim(
            "Story01_KitchenCooktop",
            new Vector3(4.5f, worktopSurfaceY + 0.0175f, ovenZ),
            new Vector3(0.5f, 0.035f, 0.9f),
            materials.dark,
            kitchen);
        int burnerIndex = 0;
        foreach (float x in new[] { 4.4f, 4.6f })
        foreach (float z in new[] { ovenZ - 0.24f, ovenZ + 0.24f })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "Story01_KitchenBurner_" + burnerIndex++,
                PrimitiveType.Cylinder,
                new Vector3(x, worktopSurfaceY + 0.047f, z),
                new Vector3(0.09f, 0.012f, 0.09f),
                materials.metal,
                kitchen,
                false);
        }

        Transform upperStorage = StoryChapterBuilderCommon.NewChild(
            kitchen,
            "Story01_KitchenUpperStorage");
        CreatePreparationUpperCabinet(
            "Story01_KitchenUpperSink",
            sinkZ,
            1.12f,
            upperStorage,
            materials);
        CreatePreparationUpperCabinet(
            "Story01_KitchenUpperPantry",
            -1.82f,
            1.36f,
            upperStorage,
            materials);
        CreatePreparationTrim(
            "Story01_KitchenUpperCrown",
            new Vector3(4.35f, 2.51f, -1.18f),
            new Vector3(0.1f, 0.08f, 2.56f),
            materials.teal,
            upperStorage);

        Transform hood = StoryChapterBuilderCommon.NewChild(kitchen, "Story01_KitchenRangeHood");
        CreatePreparationTrim(
            "Story01_KitchenHoodChimney",
            new Vector3(4.75f, 2.19f, ovenZ),
            new Vector3(0.2f, 0.62f, 0.46f),
            materials.metal,
            hood);
        CreatePreparationTrim(
            "Story01_KitchenHoodCanopy",
            new Vector3(4.55f, 1.82f, ovenZ),
            new Vector3(0.58f, 0.16f, 1.02f),
            materials.teal,
            hood);
        CreatePreparationTrim(
            "Story01_KitchenHoodLight",
            new Vector3(4.31f, 1.72f, ovenZ),
            new Vector3(0.08f, 0.035f, 0.64f),
            materials.amber,
            hood);

        GameObject fridge = StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Fridge_01.prefab",
            "Story01_KitchenFridge",
            kitchen,
            new Vector3(4.48f, 0f, -4.24f),
            new Vector3(0.82f, 1.86f, 0.76f),
            new Vector3(0f, 270f, 0f),
            false);
        BoxCollider fridgeBlocker = fridge.AddComponent<BoxCollider>();
        FitColliderToRenderers(fridgeBlocker, fridge);

        // Close the short exposed doll-house edge beside the refrigerator. From
        // the food camera this used to reveal raw sky and made the kitchen read
        // like scenery floating at the end of a wall.
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_KitchenEndReturnWall",
            PrimitiveType.Cube,
            new Vector3(4.22f, PreparationCeilingBottomY * 0.5f, -5.48f),
            new Vector3(1.56f, PreparationCeilingBottomY, 0.18f),
            materials.wall,
            kitchen,
            true);
        CreatePreparationTrim(
            "Story01_KitchenEndReturnSkirting",
            new Vector3(4.22f, 0.1f, -5.365f),
            new Vector3(1.56f, 0.2f, 0.07f),
            materials.cream,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenEndReturnCrown",
            new Vector3(4.22f, PreparationCrownY, -5.365f),
            new Vector3(1.56f, 0.08f, 0.07f),
            materials.wood,
            kitchen);

        CreatePreparationTrim(
            "Story01_UnderCabinetWarmStrip",
            new Vector3(4.31f, 1.635f, -1.18f),
            new Vector3(0.035f, 0.028f, 2.46f),
            materials.amber,
            kitchen);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Cutting_board_02.prefab",
            "Story01_KitchenCuttingBoard",
            kitchen,
            new Vector3(4.3f, worktopSurfaceY, 0.02f),
            new Vector3(0.34f, 0.035f, 0.25f),
            new Vector3(0f, 270f, 0f),
            false);

        // A full-length galley runner anchors the complete work wall without lifting
        // the NavMesh or making character feet hover.
        CreatePreparationTrim(
            "Story01_KitchenMatBorder",
            new Vector3(3.72f, 0.009f, runCenterZ),
            new Vector3(1.34f, 0.012f, 3.82f),
            materials.navy,
            kitchen);
        CreatePreparationTrim(
            "Story01_KitchenMatInset",
            new Vector3(3.72f, 0.016f, runCenterZ),
            new Vector3(1.12f, 0.008f, 3.54f),
            materials.coral,
            kitchen);
    }

    private static void CreatePreparationUpperCabinet(
        string name,
        float centerZ,
        float width,
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform cabinet = StoryChapterBuilderCommon.NewChild(parent, name);
        StoryChapterBuilderCommon.CreatePrimitive(
            name + "_Shell",
            PrimitiveType.Cube,
            new Vector3(4.62f, 2.08f, centerZ),
            new Vector3(0.46f, 0.82f, width),
            materials.wood,
            cabinet,
            false);
        float doorWidth = width * 0.44f;
        foreach ((string side, float z, float knobDirection) in new[]
                 {
                     ("Left", centerZ - width * 0.24f, 1f),
                     ("Right", centerZ + width * 0.24f, -1f)
                 })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                name + "_Door_" + side,
                PrimitiveType.Cube,
                new Vector3(4.355f, 2.08f, z),
                new Vector3(0.08f, 0.7f, doorWidth),
                materials.cream,
                cabinet,
                false);
            StoryChapterBuilderCommon.CreatePrimitive(
                name + "_Door_" + side + "_Knob",
                PrimitiveType.Sphere,
                new Vector3(4.29f, 2.02f, z + knobDirection * doorWidth * 0.32f),
                Vector3.one * 0.06f,
                materials.coral,
                cabinet,
                false);
        }
    }

    private static void BuildPreparationLoungeArt(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        Transform lounge = StoryChapterBuilderCommon.NewChild(parent, "Story01_LoungeZone");
        float nightstandTop = GetVisibleBounds(world.signalNightstandClosed).max.y + 0.012f;

        Transform tableLamp = StoryChapterBuilderCommon.NewChild(lounge, "Story01_SofaTableLamp");
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampBase",
            PrimitiveType.Cylinder,
            new Vector3(-0.98f, nightstandTop + 0.035f, -3.82f),
            new Vector3(0.15f, 0.035f, 0.15f),
            materials.navy,
            tableLamp,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampStem",
            PrimitiveType.Cylinder,
            new Vector3(-0.98f, nightstandTop + 0.2f, -3.82f),
            new Vector3(0.035f, 0.145f, 0.035f),
            materials.wood,
            tableLamp,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampBulb",
            PrimitiveType.Sphere,
            new Vector3(-0.98f, nightstandTop + 0.32f, -3.82f),
            new Vector3(0.075f, 0.09f, 0.075f),
            materials.amber,
            tableLamp,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampShadeLowerRim",
            PrimitiveType.Cylinder,
            new Vector3(-0.98f, nightstandTop + 0.34f, -3.82f),
            new Vector3(0.23f, 0.025f, 0.23f),
            materials.coral,
            tableLamp,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampShadeBody",
            PrimitiveType.Cylinder,
            new Vector3(-0.98f, nightstandTop + 0.43f, -3.82f),
            new Vector3(0.21f, 0.08f, 0.21f),
            materials.cream,
            tableLamp,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_TableLampShadeTopRim",
            PrimitiveType.Cylinder,
            new Vector3(-0.98f, nightstandTop + 0.52f, -3.82f),
            new Vector3(0.17f, 0.02f, 0.17f),
            materials.coral,
            tableLamp,
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_03.prefab",
            "Story01_SofaBookStack",
            lounge,
            new Vector3(-0.57f, nightstandTop, -3.82f),
            new Vector3(0.25f, 0.08f, 0.19f),
            new Vector3(0f, -12f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_08.prefab",
            "Story01_SignalNookPhoto",
            lounge,
            new Vector3(-0.76f, 1.48f, -5.47f),
            new Vector3(0.58f, 0.68f, 0.1f),
            Vector3.zero,
            false);

        float planTableTop = GetVisibleBounds(world.planCardTable).max.y + 0.01f;
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Paper_02.prefab",
            "Story01_PlanTableNotes",
            lounge,
            new Vector3(-4.43f, planTableTop, 1.10f),
            new Vector3(0.34f, 0.025f, 0.24f),
            new Vector3(0f, 18f, 0f),
            false);
    }

    private static void BuildPreparationEntryArt(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        const float mudroomZ = 3.42f + PreparationRightWallCompositionOffsetZ;
        Transform entry = StoryChapterBuilderCommon.NewChild(parent, "Story01_EntryZone");
        CreatePreparationTrim(
            "Story01_EntryRunnerBorder",
            new Vector3(2.7f, 0.009f, 4.42f),
            new Vector3(1.42f, 0.012f, 2.55f),
            materials.navy,
            entry);
        CreatePreparationTrim(
            "Story01_EntryRunnerInset",
            new Vector3(2.7f, 0.016f, 4.42f),
            new Vector3(1.18f, 0.008f, 2.29f),
            materials.teal,
            entry);

        // Resolve the entry as one shallow mudroom composition. The previous pass
        // scattered a floating plank, three coloured spheres and a large plant over
        // the wall; none of those pieces explained how they were attached or used.
        // A framed wall field, grounded shoe console and one supported coat shelf now
        // create a readable furniture hierarchy without narrowing the main route.
        CreatePreparationTrim(
            "Story01_EntryMudroomPanel",
            new Vector3(4.805f, 1.78f, mudroomZ),
            new Vector3(0.035f, 2.18f, 1.72f),
            materials.cream,
            entry);
        foreach ((string name, Vector3 position, Vector3 scale) in new[]
                 {
                     ("Story01_EntryMudroomFrame_Top", new Vector3(4.765f, 2.87f, mudroomZ),
                         new Vector3(0.07f, 0.08f, 1.8f)),
                     ("Story01_EntryMudroomFrame_Bottom", new Vector3(4.765f, 0.7f, mudroomZ),
                         new Vector3(0.07f, 0.08f, 1.8f)),
                     ("Story01_EntryMudroomFrame_Left", new Vector3(4.765f, 1.78f, mudroomZ - 0.86f),
                         new Vector3(0.07f, 2.18f, 0.08f)),
                     ("Story01_EntryMudroomFrame_Right", new Vector3(4.765f, 1.78f, mudroomZ + 0.86f),
                         new Vector3(0.07f, 2.18f, 0.08f))
                 })
        {
            CreatePreparationTrim(name, position, scale, materials.wood, entry);
        }

        Transform shoeConsole = StoryChapterBuilderCommon.NewChild(entry, "Story01_EntryShoeConsole");
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_EntryShoeConsoleBlocker",
            PrimitiveType.Cube,
            new Vector3(4.59f, 0.36f, mudroomZ),
            new Vector3(0.46f, 0.72f, 1.34f),
            materials.wood,
            shoeConsole,
            true);
        CreatePreparationTrim(
            "Story01_EntryShoeConsoleTop",
            new Vector3(4.56f, 0.755f, mudroomZ),
            new Vector3(0.54f, 0.07f, 1.44f),
            materials.wood,
            shoeConsole);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_EntryShoeDoor_Left", mudroomZ - 0.325f),
                     ("Story01_EntryShoeDoor_Right", mudroomZ + 0.325f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(4.345f, 0.39f, z),
                new Vector3(0.035f, 0.55f, 0.58f),
                materials.cream,
                shoeConsole);
        }
        CreatePreparationTrim(
            "Story01_EntryShoeConsoleDivider",
            new Vector3(4.32f, 0.39f, mudroomZ),
            new Vector3(0.025f, 0.58f, 0.035f),
            materials.wood,
            shoeConsole);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_EntryShoeHandle_Left", mudroomZ - 0.11f),
                     ("Story01_EntryShoeHandle_Right", mudroomZ + 0.11f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(4.31f, 0.43f, z),
                new Vector3(0.025f, 0.14f, 0.025f),
                materials.metal,
                shoeConsole);
        }
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Paper_02.prefab",
            "Story01_EntryMailTray",
            shoeConsole,
            new Vector3(4.43f, 0.795f, mudroomZ + 0.31f),
            new Vector3(0.24f, 0.024f, 0.3f),
            new Vector3(0f, 270f, 0f),
            false);

        GameObject plant = StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_15.prefab",
            "Story01_EntryPlant",
            entry,
            new Vector3(4.43f, 0.795f, mudroomZ - 0.4f),
            new Vector3(0.2f, 0.38f, 0.24f),
            new Vector3(0f, -12f, 0f),
            false);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_17.prefab",
            "Story01_EntryFamilyPhoto",
            entry,
            new Vector3(4.73f, 1.96f, mudroomZ),
            new Vector3(0.13f, 0.68f, 0.58f),
            new Vector3(0f, 270f, 0f),
            false);

        Transform coatRack = StoryChapterBuilderCommon.NewChild(entry, "Story01_EntryCoatRack");
        CreatePreparationTrim(
            "Story01_EntryCoatRackBackplate",
            new Vector3(4.73f, 1.42f, mudroomZ),
            new Vector3(0.08f, 0.46f, 1.22f),
            materials.wood,
            coatRack);
        CreatePreparationTrim(
            "Story01_EntryCoatRackShelf",
            new Vector3(4.58f, 1.69f, mudroomZ),
            new Vector3(0.38f, 0.075f, 1.34f),
            materials.wood,
            coatRack);
        foreach ((string suffix, float z) in new[]
                 {
                     ("302", mudroomZ - 0.4f),
                     ("382", mudroomZ + 0.4f)
                 })
        {
            CreatePreparationTrim(
                "Story01_EntryCoatRackBracketVertical_" + suffix,
                new Vector3(4.67f, 1.58f, z),
                new Vector3(0.07f, 0.22f, 0.04f),
                materials.metal,
                coatRack);
            CreatePreparationTrim(
                "Story01_EntryCoatRackBracketArm_" + suffix,
                new Vector3(4.55f, 1.65f, z),
                new Vector3(0.22f, 0.04f, 0.04f),
                materials.metal,
                coatRack);
        }
        for (int index = 0; index < 3; index++)
        {
            float z = mudroomZ - 0.3f + index * 0.3f;
            CreatePreparationTrim(
                "Story01_EntryHookBase_" + index,
                new Vector3(4.675f, 1.4f, z),
                new Vector3(0.065f, 0.19f, 0.045f),
                materials.metal,
                coatRack);
            CreatePreparationTrim(
                "Story01_EntryHookArm_" + index,
                new Vector3(4.575f, 1.36f, z),
                new Vector3(0.16f, 0.035f, 0.035f),
                materials.metal,
                coatRack);
            CreatePreparationTrim(
                "Story01_EntryHookUpturn_" + index,
                new Vector3(4.505f, 1.405f, z),
                new Vector3(0.035f, 0.12f, 0.035f),
                materials.metal,
                coatRack);
        }

        Bounds shelf = GetEnabledVisibleBounds(world.exitBagShelf);
        float exitPanelWidth = shelf.size.z + 0.16f;
        CreatePreparationTrim(
            "Story01_ExitConsoleBackdrop",
            new Vector3(4.805f, 1.6f, shelf.center.z),
            new Vector3(0.035f, 1.72f, exitPanelWidth),
            materials.cream,
            entry);
        CreatePreparationTrim(
            "Story01_ExitConsoleFrameTop",
            new Vector3(4.765f, 2.46f, shelf.center.z),
            new Vector3(0.07f, 0.08f, exitPanelWidth + 0.08f),
            materials.wood,
            entry);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_ExitConsoleFrame_Left", shelf.min.z - 0.08f),
                     ("Story01_ExitConsoleFrame_Right", shelf.max.z + 0.08f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(4.765f, 1.6f, z),
                new Vector3(0.07f, 1.72f, 0.08f),
                materials.wood,
                entry);
        }
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Clock_03.prefab",
            "Story01_ExitClock",
            entry,
            new Vector3(4.73f, Mathf.Max(1.38f, shelf.max.y + 0.27f), shelf.center.z),
            new Vector3(0.13f, 0.72f, 0.62f),
            new Vector3(0f, 270f, 0f),
            false);
    }

    private static void RebuildPreparationStorageVisuals(
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        Transform wardrobe = StorySharedHomePrefabBuilder.FindDescendant(
            world.sharedHome.transform,
            "Wardrobe_Unsecured");
        Transform shelf = StorySharedHomePrefabBuilder.FindDescendant(
            world.sharedHome.transform,
            "Shelf_Unsecured");
        if (wardrobe == null || shelf == null)
            throw new InvalidOperationException("Story 01 depolama mobilyaları bulunamadı.");

        foreach (string shelfName in new[] { "Shelf_Secured", "Shelf_Unsecured", "Shelf_Fallen" })
        {
            Transform shelfVariant = StorySharedHomePrefabBuilder.FindDescendant(
                world.sharedHome.transform,
                shelfName);
            if (shelfVariant == null)
                throw new InvalidOperationException("Story 01 raf varyantı bulunamadı: " + shelfName);
            shelfVariant.position += Vector3.forward * PreparationRightWallCompositionOffsetZ;
        }

        RebuildPreparationWardrobeVisual(wardrobe, materials);
        RebuildPreparationShelfContents(shelf);
        RebuildPreparationExitConsoleVisual(world.exitBagShelf, materials);
    }

    private static void RebuildPreparationExitConsoleVisual(
        GameObject exitShelf,
        StoryChapterBuilderCommon.Materials materials)
    {
        if (exitShelf == null)
            throw new InvalidOperationException("Story 01 çıkış konsolu bulunamadı.");

        foreach (Renderer renderer in exitShelf.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        foreach (Collider collider in exitShelf.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        Transform skin = StoryChapterBuilderCommon.NewChild(exitShelf.transform, "Story01_ExitConsoleSkin");
        const float centerX = 4.59f;
        const float centerZ = 5.35f;
        const float depth = 0.46f;
        const float width = 1.1f;
        const float height = 0.78f;
        const float frame = 0.08f;

        CreatePreparationTrim(
            "Story01_ExitConsoleBack",
            new Vector3(centerX + depth * 0.43f, height * 0.5f, centerZ),
            new Vector3(0.055f, height - 0.12f, width - 0.1f),
            materials.cream,
            skin);
        foreach ((string name, float z) in new[]
                 {
                     ("Story01_ExitConsoleSide_Left", centerZ - width * 0.5f + frame * 0.5f),
                     ("Story01_ExitConsoleSide_Right", centerZ + width * 0.5f - frame * 0.5f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(centerX, height * 0.5f, z),
                new Vector3(depth, height, frame),
                materials.wood,
                skin);
        }
        CreatePreparationTrim(
            "Story01_ExitConsoleBottom",
            new Vector3(centerX, 0.065f, centerZ),
            new Vector3(depth, 0.13f, width),
            materials.wood,
            skin);
        CreatePreparationTrim(
            "Story01_ExitConsoleMiddleShelf",
            new Vector3(centerX - 0.015f, 0.39f, centerZ),
            new Vector3(depth - 0.03f, 0.065f, width - 0.1f),
            materials.wood,
            skin);
        CreatePreparationTrim(
            "Story01_ExitConsoleDivider",
            new Vector3(centerX - 0.015f, height * 0.5f, centerZ),
            new Vector3(depth - 0.03f, height - 0.14f, 0.065f),
            materials.wood,
            skin);
        CreatePreparationTrim(
            "Story01_ExitConsoleTop",
            new Vector3(centerX - 0.02f, height + 0.045f, centerZ),
            new Vector3(depth + 0.1f, 0.09f, width + 0.12f),
            materials.wood,
            skin);
        CreatePreparationTrim(
            "Story01_ExitConsoleToeKick",
            new Vector3(centerX + 0.04f, 0.045f, centerZ),
            new Vector3(depth - 0.08f, 0.09f, width - 0.08f),
            materials.navy,
            skin);

        BoxCollider blocker = exitShelf.GetComponent<BoxCollider>();
        if (blocker == null)
            blocker = exitShelf.AddComponent<BoxCollider>();
        blocker.enabled = true;
        blocker.isTrigger = false;
        FitColliderToRenderers(blocker, skin.gameObject);
        exitShelf.transform.position += Vector3.forward * PreparationRightWallCompositionOffsetZ;
    }

    private static void RebuildPreparationWardrobeVisual(
        Transform wardrobe,
        StoryChapterBuilderCommon.Materials materials)
    {
        Bounds importedBounds = GetVisibleBounds(wardrobe.gameObject);
        Renderer[] importedRenderers = wardrobe.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in importedRenderers)
            renderer.enabled = false;
        foreach (Collider collider in wardrobe.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        Transform skin = StoryChapterBuilderCommon.NewChild(wardrobe, "Story01_WardrobeBuiltInSkin");
        float width = Mathf.Clamp(importedBounds.size.x * 1.06f, 1.24f, 1.42f);
        float height = Mathf.Clamp(importedBounds.size.y * 1.02f, 1.96f, 2.16f);
        float depth = Mathf.Clamp(importedBounds.size.z * 0.94f, 0.55f, 0.68f);
        float centerX = importedBounds.center.x;
        float baseY = Mathf.Max(0f, importedBounds.min.y);
        float topY = baseY + height;
        float frontZ = importedBounds.min.z;
        float backZ = frontZ + depth;
        const float sideWidth = 0.11f;
        const float edge = 0.13f;
        const float doorGap = 0.025f;

        CreatePreparationTrim(
            "Story01_WardrobeBack",
            new Vector3(centerX, baseY + height * 0.5f, backZ - 0.035f),
            new Vector3(width - sideWidth, height - 0.12f, 0.07f),
            materials.cream,
            skin);
        foreach ((string name, float x) in new[]
                 {
                     ("Story01_WardrobeSide_Left", centerX - width * 0.5f + sideWidth * 0.5f),
                     ("Story01_WardrobeSide_Right", centerX + width * 0.5f - sideWidth * 0.5f)
                 })
        {
            CreatePreparationTrim(
                name,
                new Vector3(x, baseY + height * 0.5f, frontZ + depth * 0.5f),
                new Vector3(sideWidth, height, depth),
                materials.wood,
                skin);
        }
        CreatePreparationTrim(
            "Story01_WardrobePlinth",
            new Vector3(centerX, baseY + 0.07f, frontZ + depth * 0.48f),
            new Vector3(width + 0.04f, 0.14f, depth + 0.04f),
            materials.wood,
            skin);
        CreatePreparationTrim(
            "Story01_WardrobeCornice",
            new Vector3(centerX, topY - 0.055f, frontZ + depth * 0.48f),
            new Vector3(width + 0.1f, 0.13f, depth + 0.08f),
            materials.wood,
            skin);

        float doorBottom = baseY + 0.16f;
        float doorTop = topY - 0.15f;
        float doorHeight = doorTop - doorBottom;
        float doorWidth = (width - edge * 2f - doorGap) * 0.5f;
        float leftDoorX = centerX - doorWidth * 0.5f - doorGap * 0.5f;
        float rightDoorX = centerX + doorWidth * 0.5f + doorGap * 0.5f;
        foreach ((string side, float x, float handleOffset) in new[]
                 {
                     ("Left", leftDoorX, doorWidth * 0.36f),
                     ("Right", rightDoorX, -doorWidth * 0.36f)
                 })
        {
            CreatePreparationTrim(
                "Story01_WardrobeDoor_" + side,
                new Vector3(x, doorBottom + doorHeight * 0.5f, frontZ + 0.03f),
                new Vector3(doorWidth, doorHeight, 0.065f),
                materials.wood,
                skin);
            CreatePreparationTrim(
                "Story01_WardrobeDoor_" + side + "_UpperInset",
                new Vector3(x, doorBottom + doorHeight * 0.71f, frontZ - 0.012f),
                new Vector3(doorWidth - 0.12f, doorHeight * 0.42f, 0.025f),
                materials.cream,
                skin);
            CreatePreparationTrim(
                "Story01_WardrobeDoor_" + side + "_LowerInset",
                new Vector3(x, doorBottom + doorHeight * 0.25f, frontZ - 0.012f),
                new Vector3(doorWidth - 0.12f, doorHeight * 0.35f, 0.025f),
                materials.teal,
                skin);
            CreatePreparationTrim(
                "Story01_WardrobeHandle_" + side,
                new Vector3(x + handleOffset, doorBottom + doorHeight * 0.53f, frontZ - 0.038f),
                new Vector3(0.025f, 0.22f, 0.025f),
                materials.metal,
                skin);
        }

        BoxCollider blocker = wardrobe.GetComponent<BoxCollider>();
        if (blocker == null)
            blocker = wardrobe.gameObject.AddComponent<BoxCollider>();
        blocker.enabled = true;
        blocker.isTrigger = false;
        FitColliderToRenderers(blocker, skin.gameObject);
    }

    private static void RebuildPreparationShelfContents(Transform shelf)
    {
        foreach (Transform legacyBook in shelf.GetComponentsInChildren<Transform>(true)
                     .Where(child => child.name.StartsWith("Book_", StringComparison.Ordinal)))
        {
            legacyBook.gameObject.SetActive(false);
        }

        Transform contents = StoryChapterBuilderCommon.NewChild(shelf, "Story01_ShelfCuratedContents");
        Transform board31 = StorySharedHomePrefabBuilder.FindDescendant(shelf, "ShelfBoard_31");
        Transform board51 = StorySharedHomePrefabBuilder.FindDescendant(shelf, "ShelfBoard_51");
        Transform board70 = StorySharedHomePrefabBuilder.FindDescendant(shelf, "ShelfBoard_70");
        Transform board87 = StorySharedHomePrefabBuilder.FindDescendant(shelf, "ShelfBoard_87");
        if (new[] { board31, board51, board70, board87 }.Any(board => board == null))
            throw new InvalidOperationException("Story 01 raf tablaları bulunamadı.");

        PlacePreparationShelfProp("Plants/Plants_15.prefab", "Story01_ShelfPlant", contents,
            board31, 0.76f, new Vector3(0.18f, 0.28f, 0.22f), 270f);
        PlacePreparationShelfProp("Decorations/Book_03.prefab", "Story01_ShelfBooks_Lower", contents,
            board51, 0.2f, new Vector3(0.18f, 0.28f, 0.36f), 270f);
        PlacePreparationShelfProp("Decorations/Book_08.prefab", "Story01_ShelfStack_Lower", contents,
            board51, 0.78f, new Vector3(0.24f, 0.19f, 0.32f), 270f);
        PlacePreparationShelfProp("Decorations/Toy_02.prefab", "Story01_ShelfToyCar", contents,
            board70, 0.2f, new Vector3(0.18f, 0.14f, 0.3f), 270f);
        PlacePreparationShelfProp("Decorations/Book_03.prefab", "Story01_ShelfBooks_Upper", contents,
            board70, 0.78f, new Vector3(0.18f, 0.24f, 0.32f), 270f);
        PlacePreparationShelfProp("Decorations/Book_08.prefab", "Story01_ShelfStack_Top", contents,
            board87, 0.5f, new Vector3(0.24f, 0.18f, 0.34f), 270f);
    }

    private static void PlacePreparationShelfProp(
        string prefab,
        string name,
        Transform parent,
        Transform board,
        float horizontal,
        Vector3 targetSize,
        float yaw)
    {
        Bounds surface = GetVisibleBounds(board.gameObject);
        float horizontalMargin = Mathf.Min(0.2f, surface.size.z * 0.2f);
        Vector3 feetPosition = new Vector3(
            surface.min.x + surface.size.x * 0.34f,
            surface.max.y + 0.006f,
            Mathf.Lerp(
                surface.min.z + horizontalMargin,
                surface.max.z - horizontalMargin,
                Mathf.Clamp01(horizontal)));
        StoryChapterBuilderCommon.InstantiateFurniture(
            prefab,
            name,
            parent,
            feetPosition,
            targetSize,
            new Vector3(0f, yaw, 0f),
            false);
    }

    private static void BuildPreparationCorridorArt(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform corridor = StoryChapterBuilderCommon.NewChild(parent, "Story01_CorridorArt");
        CreatePreparationTrim(
            "Story01_CorridorCeiling",
            new Vector3(2.5f, PreparationCeilingCenterY, 9.4f),
            new Vector3(2.86f, 0.14f, 6.64f),
            materials.cream,
            corridor);
        CreatePreparationTrim(
            "Story01_CorridorCeilingInset",
            new Vector3(2.5f, PreparationCeilingInsetY, 9.4f),
            new Vector3(2.5f, 0.035f, 6.18f),
            materials.white,
            corridor);
        // The shared prefab's corridor side walls end at 3.4 m. Extend only their
        // visible skin up to the raised ceiling; leaving these strips collider-free
        // preserves the already-approved navigation and interaction geometry.
        foreach ((string side, float x) in new[]
                 {
                     ("Left", 0.98f),
                     ("Right", 4.02f)
                 })
        {
            CreatePreparationTrim(
                "Story01_CorridorUpperWall_" + side,
                new Vector3(x, 3.715f, 9.4f),
                new Vector3(0.18f, PreparationCeilingBottomY - 3.4f, 6.8f),
                materials.wall,
                corridor);
        }
        foreach ((string side, float x) in new[]
                 {
                     ("Left", 1.085f),
                     ("Right", 3.915f)
                 })
        {
            CreatePreparationTrim(
                "Story01_CorridorWainscot_" + side,
                new Vector3(x, 0.84f, 9.4f),
                new Vector3(0.035f, 1.32f, 6.42f),
                materials.cream,
                corridor);
            CreatePreparationTrim(
                "Story01_CorridorChairRail_" + side,
                new Vector3(side == "Left" ? 1.12f : 3.88f, 1.52f, 9.4f),
                new Vector3(0.08f, 0.11f, 6.5f),
                materials.wood,
                corridor);
        }
        CreatePreparationTrim(
            "Story01_CorridorRunnerBorder",
            new Vector3(2.5f, 0.009f, 9f),
            new Vector3(1.34f, 0.012f, 5.42f),
            materials.wood,
            corridor);
        CreatePreparationTrim(
            "Story01_CorridorRunnerInset",
            new Vector3(2.5f, 0.016f, 9f),
            new Vector3(1.1f, 0.008f, 5.12f),
            materials.coral,
            corridor);
        CreatePreparationTrim(
            "Story01_CorridorEndPanel",
            new Vector3(2.5f, PreparationCeilingBottomY * 0.5f, 12.695f),
            new Vector3(2.46f, PreparationCeilingBottomY, 0.035f),
            materials.cream,
            corridor);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_21.prefab",
            "Story01_CorridorEndPhoto",
            corridor,
            new Vector3(2.5f, 1.55f, 12.65f),
            new Vector3(0.72f, 0.78f, 0.1f),
            new Vector3(0f, 180f, 0f),
            false);
        foreach (float z in new[] { 7.55f, 10.15f })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "Story01_CorridorFlushMount_" + Mathf.RoundToInt(z * 100f),
                PrimitiveType.Cylinder,
                new Vector3(2.5f, 3.98f, z),
                new Vector3(0.22f, 0.03f, 0.22f),
                materials.wood,
                corridor,
                false);
            StoryChapterBuilderCommon.CreatePrimitive(
                "Story01_CorridorDiffuser_" + Mathf.RoundToInt(z * 100f),
                PrimitiveType.Cylinder,
                new Vector3(2.5f, 3.93f, z),
                new Vector3(0.15f, 0.035f, 0.15f),
                materials.amber,
                corridor,
                false);
        }
    }

    private static void BuildPreparationWallGallery(Transform parent)
    {
        Transform gallery = StoryChapterBuilderCommon.NewChild(parent, "Story01_WallGallery");
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_21.prefab",
            "Story01_Gallery_FamilyMemory",
            gallery,
            new Vector3(-4.83f, 1.97f, 4.02f),
            new Vector3(0.12f, 0.92f, 0.76f),
            new Vector3(0f, 90f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Clock_03.prefab",
            "Story01_Gallery_Clock",
            gallery,
            new Vector3(-4.83f, 1.82f, -0.9f),
            new Vector3(0.12f, 0.58f, 0.58f),
            new Vector3(0f, 90f, 0f),
            false);
    }

    private static void BuildPreparationPracticalLighting(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform lighting = StoryChapterBuilderCommon.NewChild(parent, "Story01_PracticalLighting");
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantCanopy",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 3.94f, 0.45f),
            new Vector3(0.18f, 0.055f, 0.18f),
            materials.navy,
            lighting,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantCord",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 3.335f, 0.45f),
            new Vector3(0.025f, 0.55f, 0.025f),
            materials.navy,
            lighting,
            false);
        Transform pendant = StoryChapterBuilderCommon.NewChild(lighting, "Story01_PendantFixture");
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantShadeCap",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 2.75f, 0.45f),
            new Vector3(0.23f, 0.035f, 0.23f),
            materials.wood,
            pendant,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantShadeBody",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 2.64f, 0.45f),
            new Vector3(0.3f, 0.1f, 0.3f),
            materials.teal,
            pendant,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantLowerRim",
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 2.52f, 0.45f),
            new Vector3(0.32f, 0.025f, 0.32f),
            materials.wood,
            pendant,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Story01_PendantWarmBulb",
            PrimitiveType.Sphere,
            new Vector3(0.35f, 2.44f, 0.45f),
            new Vector3(0.09f, 0.11f, 0.09f),
            materials.amber,
            pendant,
            false);

        CreatePreparationPracticalLight(
            lighting,
            "Story01Practical_CenterLight",
            new Vector3(0.35f, 2.43f, 0.45f),
            new Color(1f, 0.77f, 0.48f),
            1.18f,
            4.8f);
        CreatePreparationPracticalLight(
            lighting,
            "Story01Practical_SofaLight",
            new Vector3(-0.98f, 1.47f, -3.72f),
            new Color(1f, 0.67f, 0.38f),
            0.82f,
            3.2f);
        CreatePreparationPracticalLight(
            lighting,
            "Story01Practical_EntryLight",
            new Vector3(2.75f, 2.62f, 6.25f),
            new Color(1f, 0.8f, 0.58f),
            1f,
            4.2f);
    }

    private static void CreatePreparationPracticalLight(
        Transform parent,
        string name,
        Vector3 position,
        Color color,
        float intensity,
        float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        // Create the Timeline binding owner while the scene object is authored. Unity
        // can discard a controller-less Animator added only during late binding before
        // scene serialization, leaving an AnimationTrack with a null scene binding.
        lightObject.AddComponent<Animator>().applyRootMotion = false;
    }

    private static GameObject CreatePreparationTrim(
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        Transform parent)
    {
        return StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Cube,
            position,
            scale,
            material,
            parent,
            false);
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

    private static Material GetOrCreatePlanBoardSpriteMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PlanBoardSpriteMaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            throw new InvalidOperationException("URP sprite shader bulunamadı.");

        material = new Material(shader) { name = "Story01_PlanBoardSprite" };
        AssetDatabase.CreateAsset(material, PlanBoardSpriteMaterialPath);
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
        const float duration = 0.96f;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.42f, 0.014f),
                new Keyframe(0.65f, -0.007f),
                new Keyframe(0.82f, 0.003f),
                new Keyframe(duration, 0f)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            new AnimationCurve(
                new Keyframe(0f, 0.12f),
                new Keyframe(0.42f, 0.024f),
                new Keyframe(0.65f, 0.038f),
                new Keyframe(0.82f, 0.009f),
                new Keyframe(duration, 0f)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, 3.2f),
                new Keyframe(0.42f, -0.9f),
                new Keyframe(0.65f, 0.45f),
                new Keyframe(0.82f, -0.15f),
                new Keyframe(duration, 0f)));
        AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0f, 1.035f),
            new Keyframe(0.42f, 0.99f),
            new Keyframe(0.65f, 1.01f),
            new Keyframe(0.82f, 0.998f),
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

    private static AnimationClip GetOrCreateSignalDrawerOpenClip()
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
            "localPosition.x",
            AnimationCurve.Constant(0f, 0.52f, 0f));
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.y",
            AnimationCurve.Constant(0f, 0.52f, 0f));
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.z",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, 0.018f),
                new Keyframe(0.38f, 0.138f),
                new Keyframe(0.52f, 0.145f)));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip GetOrCreateKitchenCabinetOpenClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(KitchenCabinetOpenClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_KitchenCabinetOpen" };
            AssetDatabase.CreateAsset(clip, KitchenCabinetOpenClipPath);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        const float duration = 0.78f;
        string leftDoorPath = "SM_Prop_Kitchen_Counter_01_Door_01";
        string rightDoorPath = "SM_Prop_Kitchen_Counter_01_Door_02";
        // Kapılar oda tarafına (dışa) açılır. Önceki işaretler panelleri gövdenin
        // içine döndürüyordu; dolap "kapısız ve boş" görünüyor, ne açıldığı
        // anlaşılmıyordu.
        clip.SetCurve(
            leftDoorPath,
            typeof(Transform),
            "localEulerAnglesRaw.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, 110f),
                new Keyframe(duration, 105f)));
        clip.SetCurve(
            rightDoorPath,
            typeof(Transform),
            "localEulerAnglesRaw.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, -110f),
                new Keyframe(duration, -105f)));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip GetOrCreateWaterSpinClip()
    {
        const string path = "Assets/Story/Animations/Generated/Story01_WaterLabelSpin.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Story01_WaterLabelSpin" };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = 60f;
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        // Pivot -150 derece döner: azimut 150'de (kameradan uzakta) duran etiket
        // net 0 dereceye, yani doğudaki inceleme kamerasına döner.
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y",
            new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.55f, -136f),
                new Keyframe(0.8f, -150f, 0f, 0f)));
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

        // Dolabı arka duvarın sol cebine tamamen oturt. Eski x=1.15 konumu
        // 2.2 m'lik geçidin içine taşıyor ve kapıdan çıkışı görsel/fiziksel
        // olarak daraltıyordu. Bütün varyantlar aynı temiz konumu kullanmalı.
        const float wardrobeWallPocketX = 0.48f;
        MoveHomeObject(home, "Wardrobe_Secured", new Vector3(wardrobeWallPocketX, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "Wardrobe_Unsecured", new Vector3(wardrobeWallPocketX, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "Wardrobe_Fallen", new Vector3(wardrobeWallPocketX, 0f, 5.38f), Vector3.zero);
        MoveHomeObject(home, "WardrobeFocus", new Vector3(wardrobeWallPocketX, 0.1f, 4.75f), Vector3.zero);

    }

    private static void ConfigurePreparationLighting(Transform root)
    {
        Light sun = StorySharedHomePrefabBuilder.FindDescendant(root, "Directional Light")
            ?.GetComponent<Light>();
        if (sun != null)
        {
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.86f, 0.7f);
            if (sun.GetComponent<Animator>() == null)
                sun.gameObject.AddComponent<Animator>().applyRootMotion = false;
        }

        Light fill = StorySharedHomePrefabBuilder.FindDescendant(root, "StoryFillLight")
            ?.GetComponent<Light>();
        if (fill != null)
        {
            fill.intensity = 1.18f;
            fill.range = 13f;
            fill.color = new Color(0.72f, 0.84f, 1f);
            if (fill.GetComponent<Animator>() == null)
                fill.gameObject.AddComponent<Animator>().applyRootMotion = false;
        }
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

        // Keep one simple collider behind the authored set, but do not use its cube
        // mesh as the visible frame. The player sees licensed sliced panels, rounded
        // card pockets, pins and real loose-paper meshes instead.
        GameObject boardBorder = StoryChapterBuilderCommon.CreatePrimitive(
            "BoardBorder", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.94f, 1.72f, 0.12f), materials.navy, board, true);
        boardBorder.transform.localPosition = new Vector3(0f, 0f, -0.025f);
        boardBorder.transform.localRotation = Quaternion.identity;
        boardBorder.GetComponent<Renderer>().enabled = false;
        GameObject boardBody = StoryChapterBuilderCommon.CreatePrimitive(
            "BoardSurface", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.76f, 1.54f, 0.10f), materials.cream, board, false);
        boardBody.transform.localPosition = new Vector3(0f, 0f, 0.045f);
        boardBody.transform.localRotation = Quaternion.identity;
        boardBody.GetComponent<Renderer>().enabled = false;
        CreateBoardSprite(
            board,
            "FamilyPlanBoardFrame_Kenney",
            PlanBoardPanelSpritePath,
            new Vector3(0f, 0f, 0.105f),
            new Vector2(3.02f, 1.80f),
            Color.white,
            SpriteDrawMode.Sliced,
            0);

        GameObject header = StoryChapterBuilderCommon.CreatePrimitive(
            "PlanHeader", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.58f, 0.34f, 0.055f), materials.navy, board, false);
        header.transform.localPosition = new Vector3(0f, 0.52f, 0.12f);
        header.transform.localRotation = Quaternion.identity;
        header.GetComponent<Renderer>().enabled = false;
        CreateBoardSprite(
            board,
            "PlanHeaderRibbon",
            PlanHeaderRibbonSpritePath,
            new Vector3(0f, 0.52f, 0.16f),
            new Vector2(2.56f, 0.39f),
            new Color(1f, 0.72f, 0.34f, 1f),
            SpriteDrawMode.Sliced,
            3);
        CreateBoardLabel(board, "PlanHeaderText", "AİLE AFET PLANI",
            new Vector3(0f, 0.52f, 0.19f), new Vector2(2.24f, 0.27f), 0.98f,
            StoryChapterBuilderCommon.Navy);
        CreateBoardLabel(board, "PlanSubheaderText", "BİZİ GÜVENDE TUTAN 3 KARAR",
            new Vector3(0f, 0.285f, 0.165f), new Vector2(2.4f, 0.16f), 0.44f,
            StoryChapterBuilderCommon.Navy);

        float[] slotX = { -0.86f, 0f, 0.86f };
        string[] slotTitles = { "TOPLANMA YERİ", "ACİL İLETİŞİM", "CAN'IN GÖREVİ" };
        for (int i = 0; i < slotX.Length; i++)
        {
            GameObject slot = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanSlot_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.74f, 0.72f, 0.045f), materials.white, board, false);
            slot.transform.localPosition = new Vector3(slotX[i], -0.15f, 0.12f);
            slot.transform.localRotation = Quaternion.identity;
            slot.GetComponent<Renderer>().enabled = false;
            CreateBoardSprite(
                board,
                "PlanSlotPaper_" + (i + 1),
                PlanCardPanelSpritePath,
                new Vector3(slotX[i], -0.15f, 0.145f),
                new Vector2(0.78f, 0.74f),
                new Color(1f, 0.95f, 0.82f, 1f),
                SpriteDrawMode.Sliced,
                2);

            GameObject slotHeader = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanSlotHeader_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.66f, 0.16f, 0.035f), materials.navy, board, false);
            slotHeader.transform.localPosition = new Vector3(slotX[i], 0.09f, 0.17f);
            slotHeader.transform.localRotation = Quaternion.identity;
            slotHeader.GetComponent<Renderer>().enabled = false;
            CreateBoardSprite(
                board,
                "PlanSlotHeaderPaper_" + (i + 1),
                PlanSlotHeaderSpritePath,
                new Vector3(slotX[i], 0.09f, 0.205f),
                new Vector2(0.72f, 0.18f),
                Color.white,
                SpriteDrawMode.Sliced,
                5);

            Transform answerGhost = StoryChapterBuilderCommon.NewChild(
                board,
                "PlanSlotAnswerGhost_" + (i + 1));
            answerGhost.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            answerGhost.localScale = Vector3.one;
            Transform socketMotion = StoryChapterBuilderCommon.NewChild(
                answerGhost,
                "PlanCardGhostPulse_" + (i + 1));
            socketMotion.localPosition = new Vector3(slotX[i], cardCenterY, cardCenterZ);
            socketMotion.localRotation = Quaternion.identity;
            socketMotion.localScale = Vector3.one;
            GameObject socket = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanCardGhostSocket_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.68f, 0.42f, 0.018f), socketMaterial, socketMotion, false);
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localRotation = Quaternion.identity;
            socket.GetComponent<Renderer>().enabled = false;
            GameObject socketInset = StoryChapterBuilderCommon.CreatePrimitive(
                "PlanCardGhostInset_" + (i + 1), PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.6f, 0.34f, 0.01f), materials.cream, socketMotion, false);
            socketInset.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            socketInset.transform.localRotation = Quaternion.identity;
            socketInset.GetComponent<Renderer>().enabled = false;
            CreateBoardSprite(
                socketMotion,
                "PlanCardGhostPaper_" + (i + 1),
                PlanCardPanelSpritePath,
                Vector3.zero,
                new Vector2(0.68f, 0.42f),
                new Color(0.42f, 0.92f, 0.66f, 0.72f),
                SpriteDrawMode.Sliced,
                7);
            CreateBoardSprite(
                board,
                "PlanSlotPocketLip_" + (i + 1),
                PlanSlotHeaderSpritePath,
                new Vector3(slotX[i], -0.39f, 0.285f),
                new Vector2(0.66f, 0.10f),
                new Color(0.95f, 0.85f, 0.56f, 1f),
                SpriteDrawMode.Sliced,
                8);
            CreateBoardPin(board, "PlanSlotPin_" + (i + 1), new Vector3(slotX[i], 0.205f, 0.275f),
                materials.amber);

            Animation socketAnimation = socketMotion.gameObject.AddComponent<Animation>();
            socketAnimation.AddClip(socketPulseClip, "Pulse");
            socketAnimation.clip = socketPulseClip;
            socketAnimation.playAutomatically = true;
            socketAnimation.wrapMode = WrapMode.Loop;
            CreateBoardLabel(board, "PlanSlotTitle_" + (i + 1), slotTitles[i],
                new Vector3(slotX[i], 0.09f, 0.255f), new Vector2(0.66f, 0.12f), 0.43f,
                Color.white);
            // The header already communicates the matching category. Keeping the
            // answer text inside the green ghost produced a second text layer under
            // the dragged card and made every word collide during magnetic hover.
            // Leave the socket as a clean visual target; only the physical card owns
            // the answer text.
        }

        world.familyPlanComplete = CreateCompletedPlanCard(
            board, "FamilyPlanCompleteMark", new Vector3(-0.86f, cardCenterY, cardCenterZ),
            materials.cream, materials.amber, "MAHALLE PARKI\nTOPLANMA YERİ",
            StoryChapterBuilderCommon.Navy, snapClip);
        world.familyContactComplete = CreateCompletedPlanCard(
            board, "FamilyContactCompleteMark", new Vector3(0f, cardCenterY, cardCenterZ),
            materials.cream, materials.teal, "MELEK TEYZE\nANKARA",
            StoryChapterBuilderCommon.Navy, snapClip);
        world.familyRoleComplete = CreateCompletedPlanCard(
            board, "FamilyCanRoleCompleteMark", new Vector3(0.86f, cardCenterY, cardCenterZ),
            materials.cream, materials.coral, "CAN\nDÜDÜK SORUMLUSU",
            StoryChapterBuilderCommon.Navy, snapClip);
    }

    private static GameObject CreateCompletedPlanCard(Transform board, string name, Vector3 localPosition,
        Material material, Material pinMaterial, string text, Color textColor, AnimationClip snapClip)
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
        card.GetComponent<Renderer>().enabled = false;
        CreateBoardSprite(
            snapMotion.transform,
            name + "_Paper",
            PlanCardPanelSpritePath,
            Vector3.zero,
            new Vector2(0.62f, 0.36f),
            new Color(1f, 0.94f, 0.78f, 1f),
            SpriteDrawMode.Sliced,
            12);
        CreateBoardPin(
            snapMotion.transform,
            name + "_Pin",
            new Vector3(0f, 0.145f, 0.035f),
            pinMaterial,
            0.042f);
        CreateBoardLabel(snapMotion.transform, name + "_Text", text,
            new Vector3(0f, -0.02f, 0.065f), new Vector2(0.53f, 0.23f), 0.72f, textColor);
        Animation animation = snapMotion.AddComponent<Animation>();
        animation.AddClip(snapClip, "Snap");
        animation.clip = snapClip;
        animation.playAutomatically = true;
        animation.wrapMode = WrapMode.Once;
        root.SetActive(false);
        return root;
    }

    private static GameObject CreateBoardSprite(Transform parent, string name, string spritePath,
        Vector3 localCenter, Vector2 localSize, Color tint, SpriteDrawMode drawMode, int sortingOrder)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ??
                        AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new FileNotFoundException("Pano sprite asseti bulunamadı: " + spritePath, spritePath);

        GameObject instance = new GameObject(name);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localCenter;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = GetOrCreatePlanBoardSpriteMaterial();
        renderer.drawMode = drawMode;
        if (drawMode == SpriteDrawMode.Simple)
        {
            Vector2 sourceSize = sprite.bounds.size;
            instance.transform.localScale = new Vector3(
                localSize.x / sourceSize.x,
                localSize.y / sourceSize.y,
                1f);
        }
        else
        {
            renderer.size = localSize;
        }
        renderer.color = tint;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return instance;
    }

    private static GameObject CreateLooseCardPanel(Transform parent, string name, Vector3 localCenter,
        Vector2 localSize)
    {
        GameObject panel = CreateBoardSprite(
            parent,
            name,
            PlanCardPanelSpritePath,
            localCenter,
            localSize,
            new Color(1f, 0.94f, 0.78f, 1f),
            SpriteDrawMode.Sliced,
            10);
        panel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return panel;
    }

    private static GameObject CreateBoardPin(Transform parent, string name, Vector3 localPosition,
        Material material, float diameter = 0.052f)
    {
        GameObject pin = StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Sphere,
            Vector3.zero,
            Vector3.one * diameter,
            material,
            parent,
            false);
        pin.transform.localPosition = localPosition;
        pin.transform.localRotation = Quaternion.identity;
        Renderer renderer = pin.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return pin;
    }

    private static void FaceMeshyCharacterAt(Transform character, Vector3 target)
    {
        Vector3 direction = target - character.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        character.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
        label.fontSizeMax = fontSize;
        label.fontSizeMin = Mathf.Max(0.24f, fontSize * 0.58f);
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.rectTransform.sizeDelta = worldSize;
        label.margin = new Vector4(0.025f, 0.012f, 0.025f, 0.012f);
        label.renderer.sortingOrder = 30;
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
        label.fontSizeMin = Mathf.Max(0.24f, fontSize * 0.56f);
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = StoryChapterBuilderCommon.Navy;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.rectTransform.sizeDelta = worldSize;
        label.margin = new Vector4(0.025f, 0.015f, 0.025f, 0.015f);
        label.renderer.sortingOrder = 30;
    }

    private static void BuildDiscoveryFurniture(Transform parent, StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world)
    {
        world.planCardTable = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "FamilyPlanCardTable",
            parent,
            new Vector3(-4.12f, 0f, 1.08f),
            new Vector3(0.92f, 0.72f, 2.05f),
            new Vector3(0f, 90f, 0f));
        FitPlanCardTableSurface(world.planCardTable);

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
        if (PrefabUtility.IsPartOfPrefabInstance(world.signalDrawerOpen))
        {
            PrefabUtility.UnpackPrefabInstance(
                world.signalDrawerOpen,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
        }
        world.signalDrawerContent = StorySharedHomePrefabBuilder.FindDescendant(
            world.signalDrawerOpen.transform,
            "Nightstand_02_Door");
        if (world.signalDrawerContent == null)
            throw new InvalidOperationException("Sinyal komodininin hareketli çekmecesi bulunamadı.");
        foreach (Collider drawerCollider in world.signalDrawerContent.GetComponentsInChildren<Collider>(true))
            drawerCollider.enabled = false;

        // Keep the furniture mesh at its authored height. Only this neutral pivot moves,
        // so the animation clip cannot alter the drawer's local X or Y.
        Transform drawerSlide = new GameObject("SignalDrawerSlide").transform;
        drawerSlide.SetParent(world.signalDrawerOpen.transform, false);
        drawerSlide.localPosition = Vector3.zero;
        drawerSlide.localRotation = Quaternion.identity;
        drawerSlide.localScale = Vector3.one;
        world.signalDrawerContent.SetParent(drawerSlide, false);

        AnimationClip drawerClip = GetOrCreateSignalDrawerOpenClip();
        Animation drawerAnimation = drawerSlide.gameObject.AddComponent<Animation>();
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
        // Dolap açıldığında içi boş görünmesin: keşif repliğindeki yedek su
        // şişeleri gerçekten iç rafta durur; masadaki şişe incelenip paketlenen
        // olandır.
        StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/Item_Su.prefab",
            "CabinetStockWater_A",
            world.foodCabinetDoorLeft.transform,
            new Vector3(4.56f, 0.12f, -1.6f),
            new Vector3(0.24f, 0.4f, 0.24f),
            new Vector3(-90f, 0f, 0f),
            false,
            false);
        StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/Item_Su.prefab",
            "CabinetStockWater_B",
            world.foodCabinetDoorLeft.transform,
            new Vector3(4.66f, 0.12f, -2f),
            new Vector3(0.24f, 0.4f, 0.24f),
            new Vector3(-90f, 20f, 0f),
            false,
            false);
        world.foodCabinetDoorLeft.SetActive(false);
        world.foodCabinetDoorRight = new GameObject("KitchenCabinetDoorRight");
        world.foodCabinetDoorRight.transform.SetParent(world.foodCabinetDoorLeft.transform);
        world.foodCabinetDoorRight.SetActive(false);

        float medicalSurfaceY = GetVisibleBounds(world.foodCabinetClosed).max.y +
                                PreparationKitchenWorktopSurfaceOffsetY + 0.012f;

        world.healthCabinetClosed = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusFirstAidPath,
            "AidServiceCase",
            parent,
            new Vector3(4.72f, medicalSurfaceY, -2.02f),
            new Vector3(0.44f, 0.31f, 0.38f),
            new Vector3(0f, 90f, 0f),
            false,
            false);

        world.healthDrawerOpen = new GameObject("AidCabinetOpenDoor");
        world.healthDrawerOpen.transform.SetParent(parent);
        world.healthDrawerOpen.transform.SetPositionAndRotation(
            new Vector3(4.72f, medicalSurfaceY, -2.02f),
            Quaternion.identity);
        world.healthDrawerOpen.SetActive(false);
        world.healthKitDisplay = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusFirstAidPath,
            "AidServiceCase_Removed",
            world.healthDrawerOpen.transform,
            new Vector3(4.64f, medicalSurfaceY, -2.04f),
            new Vector3(0.44f, 0.31f, 0.38f),
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
        // Açık durum AYNI sandıktır; üstünde bulunan battaniye bohçası belirir.
        // (Eski box-open modeli sandığı bambaşka bir karton kutuya çeviriyordu.)
        world.warmthChestOpen = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyChestPath,
            "WarmthChestOpenDoor",
            parent,
            new Vector3(-4.25f, 0f, -1.8f),
            new Vector3(0.88f, 0.62f, 0.68f),
            new Vector3(0f, 90f, 0f),
            false,
            false,
            kenneySurvivalMaterial);
        StoryChapterBuilderCommon.InstantiateAsset(
            KenneyBedrollPath,
            "WarmthChest_ContentsPeek",
            world.warmthChestOpen.transform,
            new Vector3(-4.25f, 0.6f, -1.8f),
            new Vector3(0.52f, 0.2f, 0.4f),
            new Vector3(0f, 105f, 0f),
            false,
            false,
            kenneySurvivalMaterial);
        world.warmthChestOpen.SetActive(false);
        world.exitBagShelf = StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_09.prefab",
            "ExitBagShelf",
            parent,
            new Vector3(4.25f, 0f, 5.72f),
            new Vector3(1.35f, 0.82f, 0.54f),
            new Vector3(0f, 180f, 0f));
        foreach (Renderer renderer in world.exitBagShelf.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = materials.wood;
    }

    private static void FitPlanCardTableSurface(GameObject table)
    {
        if (table == null)
            throw new InvalidOperationException("Aile planı kart masası bulunamadı.");

        // InstantiateFurniture mobilyayı en küçük hedef eksene göre uniform
        // ölçekler. Work_Table_06 için 0.72 m yükseklik sınırlayıcı olduğundan,
        // istenen yatay yüzey yerine yalnız ~0.70 x 1.24 m tabla oluşuyordu; üç
        // adet 0.74 x 0.46 m kart iki kenardan taşıyordu. Yüksekliği koruyup
        // yalnız yatay footprint'i kart dizilimine göre genişlet.
        const float desiredWorldWidth = 1.12f;
        const float desiredWorldLength = 1.92f;
        Bounds before = GetVisibleBounds(table);
        Vector3 scale = table.transform.localScale;
        table.transform.localScale = Vector3.Scale(
            scale,
            new Vector3(
                desiredWorldLength / Mathf.Max(0.01f, before.size.z),
                1f,
                desiredWorldWidth / Mathf.Max(0.01f, before.size.x)));

        Bounds after = GetVisibleBounds(table);
        Vector3 desiredFeetCenter = new Vector3(-4.0f, 0f, 1.10f);
        table.transform.position += desiredFeetCenter -
                                    new Vector3(after.center.x, after.min.y, after.center.z);
    }

    private static void ConfigureSyntyCounterOpen(GameObject counter)
    {
        Transform leftDoor = StorySharedHomePrefabBuilder.FindDescendant(
            counter.transform,
            "SM_Prop_Kitchen_Counter_01_Door_01");
        Transform rightDoor = StorySharedHomePrefabBuilder.FindDescendant(
            counter.transform,
            "SM_Prop_Kitchen_Counter_01_Door_02");
        if (leftDoor != null)
            leftDoor.localRotation = Quaternion.identity;
        if (rightDoor != null)
            rightDoor.localRotation = Quaternion.identity;

        AnimationClip openClip = GetOrCreateKitchenCabinetOpenClip();
        Animation openAnimation = counter.AddComponent<Animation>();
        openAnimation.playAutomatically = true;
        openAnimation.AddClip(openClip, openClip.name);
        openAnimation.clip = openClip;
    }

    private static void BuildOpenBag(Transform parent, StoryChapterBuilderCommon.Materials materials, PreviewWorld world)
    {
        // Çanta masanın hemen doğu ucunda durur. Eski konumu masadan 2+ metre
        // uzaktaydı; ikisini aynı anda gösteren tek kadraj kurulamıyor, oyuncu
        // "eşyayı çantaya sürükle" derken masayı karakterlerin arkasından
        // seçmeye çalışıyordu.
        Vector3 bagFloorPosition = new Vector3(1.7f, 0.02f, 0.5f);
        world.openBag = new GameObject("EmergencyBag_Open_Packing");
        world.openBag.transform.SetParent(parent);
        world.openBag.transform.position = bagFloorPosition;
        GameObject openBagVisual = StoryChapterBuilderCommon.InstantiateAsset(
            OriginalOpenBagPath,
            "EmergencyBag_Open_Visual",
            world.openBag.transform,
            bagFloorPosition,
            new Vector3(0.72f, 0.5f, 0.62f),
            new Vector3(0f, 35f, 0f),
            false);
        StoryChapterBuilderCommon.NewChild(world.openBag.transform, "OriginalBolum1BagSource");
        Bounds openBagBounds = GetVisibleBounds(openBagVisual);
        Vector3 physicalOpeningCenter = new(
            openBagBounds.center.x,
            world.openBag.transform.position.y + 0.43f,
            openBagBounds.center.z);

        foreach (BagDropZone oldZone in world.openBag.GetComponentsInChildren<BagDropZone>(true))
            Object.DestroyImmediate(oldZone);

        GameObject dropZoneObject = StoryChapterBuilderCommon.CreatePrimitive(
            "PhysicalBagOpening",
            PrimitiveType.Cube,
            physicalOpeningCenter,
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
            dropZoneObject.transform.position - Vector3.up * 0.2f,
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
        Transform radioStage = StoryChapterBuilderCommon.NewChild(parent, "SignalRadioInspectionStage");
        radioStage.localPosition = Vector3.zero;
        radioStage.localRotation = Quaternion.identity;
        radioStage.localScale = Vector3.one;
        world.radioReviewProp = radioStage.gameObject;
        world.radioReviewModel = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/Radio.prefab",
            "BagReview_EmergencyRadio",
            radioStage,
            new Vector3(1.1f, 0.81f, 0.65f),
            new Vector3(0.59f, 0.4f, 0.46f),
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
            new Vector3(1.7f, 0.02f, 0.5f),
            new Vector3(0.7f, 0.42f, 0.6f),
            new Vector3(0f, 35f, -13f),
            false);
        world.wrongChoiceConsequence.SetActive(false);
    }

    private static void BuildFinalBagStates(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials,
        PreviewWorld world,
        Transform deniz)
    {
        // Doldurma sırasında kapak açık kalır; giyildiğinde aynı turkuaz renk diliyle
        // kapanmış, çocuk ölçüsünde bir sırt çantasına dönüşür. Ham açık-çanta FBX'ini
        // doğrudan sırta asmak kapağı ve tek askıyı yana fırlatıyordu.
        world.wornBag = StoryChapterBuilderCommon.InstantiateAsset(
            OriginalOpenBagPath,
            "EmergencyBag_Worn",
            parent,
            deniz.position,
            new Vector3(0.34f, 0.44f, 0.22f),
            Vector3.zero,
            false);
        StoryChapterBuilderCommon.RestyleEmergencyBag(
            world.wornBag,
            materials,
            new Vector3(0.34f, 0.44f, 0.22f),
            false,
            true);
        StoryChapterBuilderCommon.MountEmergencyBackpackToTorso(
            deniz.gameObject,
            world.wornBag,
            deniz.rotation,
            -0.005f,
            0.015f);
        world.wornBag.SetActive(false);

        if (world.exitBagShelf == null)
            throw new InvalidOperationException("Çıkış çantası için yere basan giriş rafı bulunamadı.");
        Bounds exitShelfBounds = GetEnabledVisibleBounds(world.exitBagShelf);
        world.exitBag = StoryChapterBuilderCommon.InstantiateAsset(
            OriginalOpenBagPath,
            "EmergencyBag_ExitShelf",
            parent,
            new Vector3(exitShelfBounds.center.x, exitShelfBounds.max.y + 0.012f, exitShelfBounds.center.z - 0.03f),
            new Vector3(0.55f, 0.42f, 0.48f),
            new Vector3(0f, 205f, 0f),
            false);
        world.exitBag.SetActive(false);
    }

    private static PlayableInteractions BuildDirectWorldInteractions(Transform parent, PreviewWorld world,
        StoryChapterBuilderCommon.Characters family, StoryPreparationDirector director,
        StoryChapterBuilderCommon.Materials materials)
    {
        PlayableInteractions result = new PlayableInteractions();
        Transform interactionRoot = StoryChapterBuilderCommon.NewChild(parent, "DirectWorldInteractions");
        if (world.planCardTable == null)
            throw new InvalidOperationException("Aile planı kart masasının yüzeyi bulunamadı.");
        float planCardSurfaceY = GetVisibleBounds(world.planCardTable).max.y + 0.012f;
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
        GameObject[] planSlotAnswerGhosts =
        {
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanSlotAnswerGhost_1")?.gameObject,
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanSlotAnswerGhost_2")?.gameObject,
            StorySharedHomePrefabBuilder.FindDescendant(planBoard, "PlanSlotAnswerGhost_3")?.gameObject
        };
        if (planCardSnapTargets.Any(target => target == null))
            throw new InvalidOperationException("Aile planı manyetik kart yuvaları bulunamadı.");
        if (planSlotAnswerGhosts.Any(target => target == null))
            throw new InvalidOperationException("Aile planı cevap ghost grupları bulunamadı.");
        Transform planCardDragPlaneAnchor = StoryChapterBuilderCommon.NewChild(
            interactionRoot,
            "PlanCardSharedDragPlane");
        planCardDragPlaneAnchor.SetPositionAndRotation(
            planBoard.TransformPoint(new Vector3(0f, -0.2f, 0.62f)),
            planBoard.rotation);

        GameObject planHotspot = StoryChapterBuilderCommon.CreatePrimitive(
            "FamilyMeetingPointCard_Drag",
            PrimitiveType.Cube,
            new Vector3(-3.92f, planCardSurfaceY, 0.56f),
            new Vector3(0.74f, 0.08f, 0.46f),
            LoadMaterial("Cream"),
            interactionRoot,
            true,
            Quaternion.identity);
        Transform cardArtwork = StoryChapterBuilderCommon.NewChild(
            planHotspot.transform,
            "MeetingPointCardArtwork");
        cardArtwork.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cardArtwork.localScale = new Vector3(1f / 0.74f, 1f / 0.08f, 1f / 0.46f);
        planHotspot.GetComponent<Renderer>().enabled = false;
        CreateLooseCardPanel(
            cardArtwork,
            "MeetingPointCardPaper",
            new Vector3(0f, 0.012f, 0f),
            new Vector2(0.70f, 0.42f));
        CreateBoardPin(
            cardArtwork,
            "MeetingPointCardClip",
            new Vector3(0f, 0.035f, 0.205f),
            materials.amber,
            0.042f);
        CreatePlanCardLabel(
            cardArtwork,
            "MeetingPointCardLabel",
            "MAHALLE PARKI\nBİNALARDAN UZAK\nAÇIK ALAN",
            0.50f,
            new Vector2(0.60f, 0.30f));
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
        planDragData.FindProperty("magneticSnapResponse").floatValue = 7.5f;
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
        UnityEventTools.AddBoolPersistentListener(
            result.startFamilyPlan.OnInteracted,
            planSlotAnswerGhosts[0].SetActive,
            false);
        UnityEventTools.AddPersistentListener(
            result.startFamilyPlan.OnInteracted,
            director.OnFamilyPlanStarted);

        result.placeContactCard = BuildPlanCardInteraction(
            interactionRoot,
            planCardDragPlaneAnchor,
            planCardSnapTargets,
            "MelekContactCard_Drag",
            "MELEK TEYZE\nANKARA\n05XX 123 45 67",
            "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle",
            "Melek Teyze — şehir dışındaki yakınımız",
            new Vector3(-3.92f, planCardSurfaceY, 1.10f),
            0f,
            LoadMaterial("Cream"),
            contactDrop,
            world.familyContactComplete,
            planSlotAnswerGhosts[1],
            director.OnContactCardPlaced);
        result.assignCanWhistleRole = BuildPlanCardInteraction(
            interactionRoot,
            planCardDragPlaneAnchor,
            planCardSnapTargets,
            "CanWhistleRoleCard_Drag",
            "CAN\nDÜDÜĞÜ TAŞIR\n3 KEZ ÇALAR",
            "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle",
            "Can — düdük sorumlusu",
            new Vector3(-3.92f, planCardSurfaceY, 1.64f),
            0f,
            LoadMaterial("Cream"),
            canRoleDrop,
            world.familyRoleComplete,
            planSlotAnswerGhosts[2],
            director.OnCanWhistleRolePlaced);

        BuildCategoryDiscoveryInteractions(interactionRoot, world, director, result);

        PreviewItem[] items =
        {
            new("Flashlight", "Item_Fener.prefab", "El feneri",
                new Vector3(-0.25f, 0.81f, 0.65f), new Vector3(0.4f, 0.2f, 0.26f), 22f,
                StoryCameraZoneId.PreparationBag),
            new("Radio", "Radio.prefab", "Pilli radyo",
                new Vector3(1.1f, 0.81f, 0.65f), new Vector3(0.36f, 0.24f, 0.28f),
                new Vector3(-90f, -161f, 0f),
                StoryCameraZoneId.PreparationBag),
            new("Batteries", "Item_Pil.prefab", "Yedek pil",
                new Vector3(0.2f, 0.81f, 0.65f), new Vector3(0.19f, 0.11f, 0.15f), -12f,
                StoryCameraZoneId.PreparationBag),
            new("Whistle", "whistle.prefab", "Düdük",
                new Vector3(0.65f, 0.81f, 0.65f), new Vector3(0.3f, 0.18f, 0.24f), 12f,
                StoryCameraZoneId.PreparationBag),
            new("Water", "Item_Su.prefab", "Su",
                new Vector3(-0.35f, 1.17f, 0.3f), new Vector3(0.3f, 0.48f, 0.3f),
                new Vector3(-90f, 0f, 0f),
                StoryCameraZoneId.PreparationBag),
            new("Food", "konserve.prefab", "Konserve",
                new Vector3(0.25f, 1.14f, 0.3f), new Vector3(0.12f, 0.14f, 0.13f),
                new Vector3(-90f, 8f, 0f),
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
            "Çantayı sapından tutup yukarıdaki hedefe sürükle",
            StoryInteractionKind.Collect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationBag,
            true,
            1,
            1.15f,
            1.6f);

        GameObject bagLiftTarget = new GameObject("BagWeightLiftDropZone");
        bagLiftTarget.transform.SetParent(interactionRoot);
        // Yakın plan kadrajında üst kenardan taşmaması için kaldırma hedefi
        // çantanın 0.85 m üzerindedir.
        bagLiftTarget.transform.position = world.openBag.transform.position + Vector3.up * 0.85f;
        bagLiftTarget.transform.rotation = world.openBag.transform.rotation;
        BoxCollider bagLiftTargetCollider = bagLiftTarget.AddComponent<BoxCollider>();
        bagLiftTargetCollider.size = new Vector3(1.15f, 0.44f, 0.95f);
        bagLiftTargetCollider.isTrigger = true;
        BagDropZone bagLiftDropZone = bagLiftTarget.AddComponent<BagDropZone>();
        DraggableItem bagLiftDrag = world.openBag.AddComponent<DraggableItem>();
        ConfigureDraggable(
            bagLiftDrag,
            "Deniz'in afet çantası",
            bagLiftDropZone,
            0f,
            1.02f,
            bagLiftTarget.transform);
        SerializedObject bagLiftDragData = new SerializedObject(bagLiftDrag);
        bagLiftDragData.FindProperty("dragOnCameraPlane").boolValue = true;
        bagLiftDragData.FindProperty("faceCameraWhileDragging").boolValue = false;
        bagLiftDragData.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(result.testBagWeight, bagLiftTarget.transform);
        PlaceCompactObjectiveMarker(
            result.testBagWeight,
            bagLiftTarget.transform.position + Vector3.up * 0.3f,
            0.7f);
        UnityEventTools.AddPersistentListener(result.testBagWeight.OnInteracted, bagLiftDrag.ResetInstant);
        UnityEventTools.AddPersistentListener(result.testBagWeight.OnInteracted, director.OnBagWeightTested);

        GameObject consoleLiftVisual = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/GameConsole_01 Variant.prefab",
            "ConsoleConflict_InBag_Visual",
            world.openBag.transform,
            world.openBag.transform.position + new Vector3(0.03f, 0.3f, 0.02f),
            new Vector3(0.42f, 0.15f, 0.28f),
            new Vector3(0f, 8f, -18f),
            false,
            false);
        Transform consoleHost = StoryChapterBuilderCommon.NewChild(
            world.wrongChoiceConsequence.transform,
            "ConsoleConflict_Draggable");
        GameObject consoleVisual = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/GameConsole_01 Variant.prefab",
            "ConsoleConflict_Heavy_Visual",
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
        // Konsolun bırakılacağı masa, WrongChoice kadrajının dışında kalıyordu.
        // Sürükleme başlarken odak PreparationBag'e geçer; bu kadraj hem çantadaki
        // konsolu hem masa drop hedefini aynı anda gösterir.
        result.removeConsole = StoryChapterBuilderCommon.AddInteractable(
            consoleHost.gameObject,
            "Resolve_RemoveConsole",
            "Konsolu çantadan tutup güvenli masaya geri sürükle",
            StoryInteractionKind.HelpSibling,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationBag,
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
        world.consoleInBag = consoleLiftVisual;
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
        // Sırttaki çanta telefonda bağışlayıcı bir dokunma hedefi olmalı; Deniz'in
        // gövde kapsülü araya girse bile çantanın hacmi öne taşar.
        EnsureMinimumWorldCollider(wornBagCollider, new Vector3(0.55f, 0.5f, 0.5f));
        // Eski BagFit kadrajı oyuncunun başlangıç noktasına bakıyordu ve raf drop
        // hedefi tamamen ekran dışıydaydı; bölümün son adımı fiilen oynanamıyordu.
        // Director önce Deniz'i rafın yanına yürütür, sonra bu odak kadrajında hem
        // sırttaki çanta hem raf hedefi birlikte görünür.
        result.placeBagAtExit = StoryChapterBuilderCommon.AddInteractable(
            world.wornBag,
            "Final_PlaceBagAtExit",
            "Çantayı tutup alçak çıkış rafına doğru çek",
            StoryInteractionKind.Exit,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationExitShelf,
            true,
            1,
            1f,
            1.7f);
        // Tamamlanma kamerasını director yönetir (OnBagPlacedAtExit -> PreparationExitShelf).
        // Buradaki eski Overview dönüşü, kapanış diyaloğu sırasında kadrajı final
        // tablosundan koparıyordu.
        Bounds exitConsoleBounds = GetEnabledVisibleBounds(world.exitBagShelf);
        GameObject exitDropObject = new GameObject("ExitShelfBagDropZone");
        exitDropObject.transform.SetParent(interactionRoot);
        exitDropObject.transform.position = new Vector3(
            exitConsoleBounds.min.x - 0.05f,
            exitConsoleBounds.max.y + 0.22f,
            exitConsoleBounds.center.z);
        BoxCollider exitDropCollider = exitDropObject.AddComponent<BoxCollider>();
        exitDropCollider.size = new Vector3(0.9f, 0.8f, 1.05f);
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
        // Deniz'in final sürüklemeden önce yürüyeceği, ExitShelf kadrajında görünen
        // zemin noktası. Forward yönü rafa bakar; varışta karakter rafa döner.
        // Raf kadrajını kapatmayacak kadar geride durur; sırttaki çanta ve raf
        // hedefi ikisi de açık görünür.
        result.exitShelfApproachPoint = StoryChapterBuilderCommon.CreatePoint(
            "ExitShelfApproachPoint",
            interactionRoot,
            new Vector3(
                exitConsoleBounds.min.x - 1.05f,
                0f,
                exitConsoleBounds.center.z - 0.8f),
            exitDropObject.transform.position);

        // Bırakma alanı, düdük sahnesindekiyle aynı dille görünür bir yeşil
        // halkayla işaretlenir; oyuncu "nereye bırakacağım" sorusunu ekranda görür.
        Sprite exitRingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            StoryChapterBuilderCommon.KenneyInputPromptRoot + "/marker_circle.png");
        if (exitRingSprite == null)
            throw new InvalidOperationException("Raf hedefi için Kenney halka sprite'ı bulunamadı.");
        GameObject exitDropRing = new GameObject("ExitShelfDropRing");
        exitDropRing.transform.SetParent(exitDropObject.transform, false);
        exitDropRing.transform.localPosition = Vector3.zero;
        exitDropRing.transform.localScale = Vector3.one * 0.42f;
        exitDropRing.AddComponent<BillboardToCamera>();
        SpriteRenderer exitRingRenderer = exitDropRing.AddComponent<SpriteRenderer>();
        exitRingRenderer.sprite = exitRingSprite;
        exitRingRenderer.color = new Color32(95, 218, 142, 215);
        exitRingRenderer.sortingOrder = 42;
        exitRingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        exitRingRenderer.receiveShadows = false;
        exitDropRing.SetActive(false);
        result.exitShelfDropRing = exitDropRing;

        result.comfortItem = BuildComfortItemInteraction(interactionRoot, family, director, result);
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
        GameObject slotAnswerGhost,
        UnityAction completed)
    {
        Vector3 cardScale = new Vector3(0.74f, 0.08f, 0.46f);
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
        card.GetComponent<Renderer>().enabled = false;
        CreateLooseCardPanel(
            artwork,
            objectName + "_Paper",
            new Vector3(0f, 0.012f, 0f),
            new Vector2(0.70f, 0.42f));
        CreateBoardPin(
            artwork,
            objectName + "_Clip",
            new Vector3(0f, 0.035f, 0.195f),
            LoadMaterial("Amber"),
            0.04f);
        CreatePlanCardLabel(
            artwork,
            objectName + "_Label",
            cardLabel,
            0.50f,
            new Vector2(0.60f, 0.30f));

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
        dragData.FindProperty("magneticSnapResponse").floatValue = 7.5f;
        dragData.FindProperty("magneticSnapSurfaceOffset").floatValue = 0f;
        dragData.FindProperty("dragScale").floatValue = 1.04f;
        dragData.FindProperty("returnDuration").floatValue = 0.34f;
        dragData.FindProperty("returnArcHeight").floatValue = 0.12f;
        dragData.ApplyModifiedPropertiesWithoutUndo();

        StoryChapterBuilderCommon.SetGestureTarget(interaction, dropObject.transform);
        SetAvailableOnStart(interaction, false);
        UnityEventTools.AddBoolPersistentListener(interaction.OnInteracted, completedVisual.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(interaction.OnInteracted, card.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(interaction.OnInteracted, slotAnswerGhost.SetActive, false);
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
            "Çekmece kulpunu tutup sağ alta doğru çek",
            StoryCameraZoneId.PreparationSignal,
            StoryInteractionGesture.SwipeDiagonalDownRight);
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

        Transform foodDoor = StorySharedHomePrefabBuilder.FindDescendant(
            world.foodCabinetClosed.transform,
            "SM_Prop_Kitchen_Counter_01_Door_01");
        if (foodDoor == null)
            throw new InvalidOperationException("Mutfak dolabının sol alt kapağı bulunamadı.");
        result.discoverFood = CreatePhysicalDiscovery(
            foodDoor.gameObject,
            "Discover_FoodCabinet",
            "Sol alt dolap kapağını tutup sağa doğru kaydır",
            StoryCameraZoneId.PreparationFood);
        Bounds foodDoorBounds = GetVisibleBounds(foodDoor.gameObject);
        result.discoverFood.HighlightRoot.transform.position = new Vector3(
            foodDoorBounds.min.x - 0.08f,
            foodDoorBounds.center.y + 0.18f,
            foodDoorBounds.center.z);
        result.discoverFood.HighlightRoot.transform.localScale = Vector3.one * 0.72f;
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
        StoryPreparationItem flashlightItem = result.items.Single(item =>
            string.Equals(item.ItemId, "Flashlight", StringComparison.Ordinal));
        Transform flashlightRoot = flashlightItem.Interactable.transform;
        Vector3 flashlightPosition = flashlightRoot.position;
        Bounds flashlightBounds = GetVisibleBounds(flashlightRoot.gameObject);
        Vector3 flashlightSwitchPosition = new(
            flashlightBounds.center.x - flashlightBounds.extents.x * 0.18f,
            flashlightBounds.max.y + 0.004f,
            flashlightBounds.center.z);
        GameObject flashlightSwitch = CreateModelSurfaceHotspot(
            "FlashlightTopSwitch",
            flashlightRoot,
            flashlightSwitchPosition,
            new Vector3(0.09f, 0.07f, 0.08f));
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
        // The shared 0.68 m fallback is useful for room-scale props, but at this
        // macro camera distance it becomes wider than the portrait frame. Author
        // the switch hit volume at a still-generous phone size instead of moving
        // the camera away from the physical flashlight.
        SetMobileTouchHotspotWorldSize(result.reviewSignal, new Vector3(0.22f, 0.18f, 0.22f));
        ConfigureModelControlArrow(
            result.reviewSignal,
            flashlightSwitchPosition + Vector3.up * 0.1f);
        SetAvailableOnStart(result.reviewSignal, false);

        // Fener ışığı mercek ucundan çıkan spot konidir. Eski noktasal ışık fenerin
        // gövdesini parlatıyordu; ışık "önünden" çıkmıyordu. Işın yönü modelin uzun
        // ekseninden, işareti masa merkezinden dışarı bakacak şekilde türetilir ve
        // koni PreparationFlashlight kadrajının içindeki zemine düşer.
        Vector3 beamAxis = flashlightBounds.extents.x >= flashlightBounds.extents.z
            ? Vector3.right
            : Vector3.forward;
        float beamExtent = beamAxis == Vector3.right
            ? flashlightBounds.extents.x
            : flashlightBounds.extents.z;
        Vector3 outwardFromTable = flashlightBounds.center - new Vector3(0.45f, flashlightBounds.center.y, 0.65f);
        Vector3 beamDirection = Vector3.Dot(beamAxis, outwardFromTable) >= 0f ? beamAxis : -beamAxis;
        world.signalTestLight = new GameObject("FlashlightInspectionBeam");
        world.signalTestLight.transform.SetParent(flashlightRoot);
        world.signalTestLight.transform.position =
            flashlightBounds.center + beamDirection * (beamExtent + 0.015f);
        world.signalTestLight.transform.rotation =
            Quaternion.LookRotation((beamDirection + Vector3.down * 0.4f).normalized, Vector3.up);
        Light signalLight = world.signalTestLight.AddComponent<Light>();
        signalLight.type = LightType.Spot;
        signalLight.spotAngle = 52f;
        signalLight.color = new Color(1f, 0.95f, 0.78f);
        signalLight.intensity = 7f;
        signalLight.range = 4.2f;
        signalLight.shadows = LightShadows.None;
        world.signalTestLight.SetActive(false);

        GameObject flashlightSwitchOff = CreateModelSurfaceHotspot(
            "FlashlightTopSwitch_OffState",
            flashlightRoot,
            flashlightSwitch.transform.position,
            new Vector3(0.09f, 0.07f, 0.08f));
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
        SetMobileTouchHotspotWorldSize(result.reviewSignalFlashlightOff, new Vector3(0.22f, 0.18f, 0.22f));
        ConfigureModelControlArrow(
            result.reviewSignalFlashlightOff,
            flashlightSwitchPosition + Vector3.up * 0.1f);
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

        // FitToSize only considers renderers that are active in the hierarchy. The inspection
        // stage is hidden at runtime until the radio is selected, so temporarily expose it while
        // the battery props and their physical targets are authored.
        world.radioReviewProp.SetActive(true);
        Bounds radioBounds = GetVisibleBounds(world.radioReviewModel);
        StoryPreparationItem batteryItem = result.items.Single(item =>
            string.Equals(item.ItemId, "Batteries", StringComparison.Ordinal));
        Transform batteryRoot = batteryItem.Interactable.transform;
        Transform batteryVisual = batteryRoot.Cast<Transform>().FirstOrDefault(child =>
            child.GetComponentsInChildren<MeshRenderer>(true).Length > 0);
        if (batteryVisual == null)
            throw new InvalidOperationException("Masadaki gerçek yedek pil görseli bulunamadı.");

        // The radio test deliberately reuses WorldItem_Batteries. There is one physical
        // battery in the scene: first it moves into the radio, then the same hierarchy
        // moves back to its authored table position and becomes the regular Pack_Batteries item.
        Transform looseBatteryRoot = StoryChapterBuilderCommon.NewChild(
            batteryRoot,
            "Review_RadioBatteryLoose");
        looseBatteryRoot.SetPositionAndRotation(batteryRoot.position, Quaternion.identity);
        looseBatteryRoot.localScale = Vector3.one;
        Transform insertedBatteryRoot = StoryChapterBuilderCommon.NewChild(
            looseBatteryRoot,
            "Review_RadioBatteryInserted");
        insertedBatteryRoot.SetPositionAndRotation(batteryRoot.position, Quaternion.identity);
        insertedBatteryRoot.localScale = Vector3.one;
        batteryVisual.SetParent(insertedBatteryRoot, true);
        world.radioBatteryLoose = looseBatteryRoot.gameObject;
        world.radioBatteryInserted = insertedBatteryRoot.gameObject;

        BoxCollider looseBatteryCollider = EnsurePhysicalInteractionCollider(world.radioBatteryLoose);
        EnsureMinimumWorldCollider(looseBatteryCollider, new Vector3(0.2f, 0.12f, 0.22f));
        Bounds looseBatteryBounds = GetVisibleBounds(world.radioBatteryLoose);
        Vector3 looseBatteryPosition = batteryRoot.position;

        GameObject radioBatterySlot = new GameObject("Review_RadioBatterySlot");
        radioBatterySlot.transform.SetParent(world.radioReviewProp.transform);
        Vector3 batteryVisualOffset = looseBatteryBounds.center - looseBatteryPosition;
        Quaternion insertedBatteryRotation = Quaternion.Euler(0f, 0f, 90f);
        Vector3 insertedBatterySize = new(
            looseBatteryBounds.size.y,
            looseBatteryBounds.size.x,
            looseBatteryBounds.size.z);
        Vector3 insertedVisualCenter = new(
            radioBounds.max.x - insertedBatterySize.x * 0.58f,
            radioBounds.center.y,
            radioBounds.center.z - radioBounds.extents.z * 0.08f);
        Vector3 insertedBatteryPosition =
            insertedVisualCenter - insertedBatteryRotation * batteryVisualOffset;
        radioBatterySlot.transform.position = insertedBatteryPosition;
        radioBatterySlot.transform.rotation = insertedBatteryRotation;
        BoxCollider radioBatterySlotCollider = radioBatterySlot.AddComponent<BoxCollider>();
        radioBatterySlotCollider.size = Vector3.Max(
            insertedBatterySize * 1.12f,
            new Vector3(0.1f, 0.15f, 0.12f));
        radioBatterySlotCollider.isTrigger = true;
        BagDropZone radioBatteryDropZone = radioBatterySlot.AddComponent<BagDropZone>();

        // Radio.fbx'te ayrılabilir pil kapağı mesh'i yok. Tek, ince ve gövdeye
        // oturan koyu girinti fiziksel bırakma yerini okunur kılar; eski çok
        // parçalı ray/panel düzeni gibi ikinci bir cihaz görüntüsü üretmez.
        Vector3 batteryBaySize = new(
            0.024f,
            radioBounds.size.y * 0.3f,
            radioBounds.size.z * 0.45f);
        StoryChapterBuilderCommon.CreatePrimitive(
            "RadioBatteryBay",
            PrimitiveType.Cube,
            new Vector3(
                radioBounds.max.x + 0.006f,
                insertedVisualCenter.y,
                insertedVisualCenter.z),
            batteryBaySize,
            materials.dark,
            world.radioReviewProp.transform,
            false);

        result.reviewSignalRadioBatteryInsert = StoryChapterBuilderCommon.AddInteractable(
            world.radioBatteryLoose,
            "Review_InsertBatteryIntoRadio",
            "Masadaki yedek pili radyonun sağ yan yüzeyine sürükle",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationRadio,
            true,
            1,
            0.75f,
            1.25f);
        PlaceCompactObjectiveMarker(
            result.reviewSignalRadioBatteryInsert,
            looseBatteryBounds.center + Vector3.up * 0.13f,
            0.28f);
        DraggableItem radioBatteryInsertDrag = world.radioBatteryLoose.AddComponent<DraggableItem>();
        ConfigureDraggable(
            radioBatteryInsertDrag,
            "Yedek pil",
            radioBatteryDropZone,
            0f,
            1.05f,
            radioBatterySlot.transform);
        StoryChapterBuilderCommon.SetGestureTarget(
            result.reviewSignalRadioBatteryInsert,
            radioBatterySlot.transform);

        BoxCollider insertedBatteryCollider = EnsurePhysicalInteractionCollider(world.radioBatteryInserted);
        EnsureMinimumWorldCollider(insertedBatteryCollider, new Vector3(0.18f, 0.11f, 0.2f));
        Bounds insertedBatteryBounds = GetVisibleBounds(world.radioBatteryInserted);

        GameObject batteryStorageTarget = new GameObject("Review_RadioBatteryStorageDropZone");
        batteryStorageTarget.transform.SetParent(world.radioReviewProp.transform);
        batteryStorageTarget.transform.position = looseBatteryPosition;
        batteryStorageTarget.transform.rotation = Quaternion.identity;
        BoxCollider storageCollider = batteryStorageTarget.AddComponent<BoxCollider>();
        storageCollider.size = Vector3.Max(
            looseBatteryBounds.size * 1.5f,
            new Vector3(0.34f, 0.22f, 0.34f));
        storageCollider.isTrigger = true;
        BagDropZone batteryStorageDropZone = batteryStorageTarget.AddComponent<BagDropZone>();
        batteryStorageTarget.SetActive(false);

        result.reviewSignalRadioBatteryRemove = StoryChapterBuilderCommon.AddInteractable(
            world.radioBatteryInserted,
            "Review_RemoveBatteryFromRadio",
            "Aynı pili radyodan çıkarıp masadaki boş yerine sürükle",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.PreparationRadio,
            true,
            1,
            0.75f,
            1.25f);
        PlaceCompactObjectiveMarker(
            result.reviewSignalRadioBatteryRemove,
            insertedBatteryBounds.center + Vector3.up * 0.12f,
            0.26f);
        DraggableItem radioBatteryRemoveDrag = world.radioBatteryInserted.AddComponent<DraggableItem>();
        ConfigureDraggable(
            radioBatteryRemoveDrag,
            "Yedek pil",
            batteryStorageDropZone,
            0f,
            1.05f,
            batteryStorageTarget.transform);
        StoryChapterBuilderCommon.SetGestureTarget(
            result.reviewSignalRadioBatteryRemove,
            batteryStorageTarget.transform);

        // Radio.fbx is a single combined mesh. Its existing white power button is therefore
        // not a child transform we can select independently. Keep the interaction invisible
        // and pin it to the authored button's mesh-local centre (beside the antenna); never
        // invent a second rendered button on top of the model.
        MeshFilter radioMesh = world.radioReviewModel.GetComponentInChildren<MeshFilter>(true);
        if (radioMesh == null)
            throw new InvalidOperationException("Radyo modelinin mesh'i bulunamadı.");
        Vector3 powerButtonPosition = radioMesh.transform.TransformPoint(
            new Vector3(-0.002889f, -0.000763f, 0.004702f));
        GameObject radioPowerButton = CreateModelSurfaceHotspot(
            "Review_RadioPowerButton",
            world.radioReviewProp.transform,
            powerButtonPosition,
            new Vector3(0.085f, 0.07f, 0.085f));
        result.reviewSignalRadio = StoryChapterBuilderCommon.AddInteractable(
            radioPowerButton,
            "Review_PowerEmergencyRadio",
            "Antenin yanındaki beyaz kare açma düğmesine dokun",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.Tap,
            StoryCameraZoneId.PreparationRadio,
            true,
            1,
            0.55f,
            1.1f);
        ConfigureModelControlArrow(
            result.reviewSignalRadio,
            powerButtonPosition + Vector3.up * 0.07f,
            0.3f,
            0.18f);

        world.radioTuningBefore = new GameObject("RadioPowerState_Off");
        world.radioTuningBefore.transform.SetParent(world.radioReviewProp.transform, false);
        world.radioTuningAfter = new GameObject("RadioPowerState_On");
        world.radioTuningAfter.transform.SetParent(world.radioReviewProp.transform, false);
        world.radioTuningAfter.SetActive(false);

        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadioBatteryInsert.OnInteracted,
            radioBatterySlot.SetActive,
            false);
        UnityEventTools.AddPersistentListener(
            result.reviewSignalRadioBatteryInsert.OnInteracted,
            director.OnSignalRadioBatteryInserted);

        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadio.OnInteracted,
            world.radioTuningBefore.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadio.OnInteracted,
            world.radioTuningAfter.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadio.OnInteracted,
            batteryStorageTarget.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.reviewSignalRadio.OnInteracted,
            director.OnSignalRadioPowered);

        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadioBatteryRemove.OnInteracted,
            batteryStorageTarget.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadioBatteryRemove.OnInteracted,
            world.radioTuningBefore.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            result.reviewSignalRadioBatteryRemove.OnInteracted,
            world.radioTuningAfter.SetActive,
            false);
        UnityEventTools.AddPersistentListener(
            result.reviewSignalRadioBatteryRemove.OnInteracted,
            director.OnSignalRadioBatteryRemoved);
        world.radioReviewProp.SetActive(false);

        Transform whistleHost = StoryChapterBuilderCommon.NewChild(parent, "Review_WhistleHandoff");
        Vector3 whistleTablePosition = new(-0.05f, 0.81f, 0.65f);
        GameObject whistleVisual = StoryChapterBuilderCommon.InstantiateAsset(
            ItemRoot + "/whistle.prefab",
            "Review_WhistleHandoff_Visual",
            whistleHost,
            whistleTablePosition,
            new Vector3(0.42f, 0.26f, 0.34f),
            new Vector3(90f, 18f, 0f),
            false,
            false);
        Bounds whistleBounds = GetVisibleBounds(whistleVisual);
        whistleVisual.transform.position += Vector3.up * (whistleTablePosition.y - whistleBounds.min.y);
        Vector3 whistleWorldPosition = whistleVisual.transform.position;
        whistleVisual.transform.SetParent(parent, true);
        whistleHost.position = whistleWorldPosition;
        whistleVisual.transform.SetParent(whistleHost, true);

        world.whistleCanPose = StoryChapterBuilderCommon.NewChild(parent, "SignalWhistleCanPose");
        // Can masanın batı yanında, düdüğe (ve dolayısıyla kameraya) dönük durur.
        // Eski poz kameranın 2.6 m önünde sırtı dönüktü: ekranı kaplıyor ve
        // göğsündeki bırakma halkası kendi gövdesinin arkasında kalıyordu.
        world.whistleCanPose.position = new Vector3(-1.05f, 0f, 0.85f);
        Vector3 canToWhistle = whistleWorldPosition - world.whistleCanPose.position;
        canToWhistle.y = 0f;
        world.whistleCanPose.rotation = canToWhistle.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(canToWhistle.normalized, Vector3.up)
            : Quaternion.identity;

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
        Bounds canVisibleBounds = GetVisibleBounds(family.can);
        whistleDropObject.transform.position = new Vector3(
            canVisibleBounds.center.x,
            Mathf.Lerp(canVisibleBounds.min.y, canVisibleBounds.max.y, 0.64f),
            canVisibleBounds.center.z) + family.can.transform.forward * 0.07f;
        whistleDropObject.transform.rotation = family.can.transform.rotation;
        Vector3 canWorldScale = family.can.transform.lossyScale;
        whistleDropObject.transform.localScale = new Vector3(
            Mathf.Abs(canWorldScale.x) > 0.0001f ? 1f / canWorldScale.x : 1f,
            Mathf.Abs(canWorldScale.y) > 0.0001f ? 1f / canWorldScale.y : 1f,
            Mathf.Abs(canWorldScale.z) > 0.0001f ? 1f / canWorldScale.z : 1f);
        BoxCollider whistleDropCollider = whistleDropObject.AddComponent<BoxCollider>();
        whistleDropCollider.size = new Vector3(0.46f, 0.52f, 0.36f);
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
        result.reviewSignalRadioBatteryInsert.SetAvailable(false);
        result.reviewSignalRadio.SetAvailable(false);
        result.reviewSignalRadioBatteryRemove.SetAvailable(false);
        result.reviewSignalWhistle.SetAvailable(false);
        whistleHost.gameObject.SetActive(false);
        whistleDropObject.SetActive(false);
    }

    private static GameObject CreateModelSurfaceHotspot(
        string name,
        Transform parent,
        Vector3 worldPosition,
        Vector3 worldSize)
    {
        GameObject hotspot = new GameObject(name);
        hotspot.transform.SetParent(parent);
        hotspot.transform.position = worldPosition;
        hotspot.transform.rotation = Quaternion.identity;
        hotspot.transform.localScale = Vector3.one;
        BoxCollider collider = hotspot.AddComponent<BoxCollider>();
        collider.size = worldSize;
        collider.isTrigger = false;
        return hotspot;
    }

    private static void SetMobileTouchHotspotWorldSize(
        StoryInteractable interaction,
        Vector3 worldSize)
    {
        Transform hotspot = interaction != null
            ? interaction.transform.Find("MobileTouchHotspot")
            : null;
        BoxCollider collider = hotspot != null ? hotspot.GetComponent<BoxCollider>() : null;
        if (collider == null)
            throw new InvalidOperationException("Scene-authored mobile touch hotspot is missing.");

        Vector3 scale = hotspot.lossyScale;
        collider.size = new Vector3(
            worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
    }

    private static void ConfigureModelControlArrow(
        StoryInteractable interaction,
        Vector3 worldPosition,
        float markerScale = 0.48f,
        float arrowScale = 0.24f)
    {
        GameObject marker = interaction != null ? interaction.HighlightRoot : null;
        if (marker == null)
            return;

        marker.transform.position = worldPosition;
        marker.transform.localScale = Vector3.one * markerScale;
        Transform badge = marker.transform.Find("ObjectiveBadge");
        Transform gesture = marker.transform.Find("ObjectiveGestureIcon");
        Transform arrow = marker.transform.Find("ObjectiveArrow");
        if (badge != null)
            badge.gameObject.SetActive(false);
        if (gesture != null)
            gesture.gameObject.SetActive(false);
        if (arrow != null)
        {
            arrow.localPosition = Vector3.zero;
            arrow.localScale = Vector3.one * arrowScale;
        }
    }

    private static void PlaceCompactObjectiveMarker(
        StoryInteractable interaction,
        Vector3 worldPosition,
        float scale)
    {
        GameObject marker = interaction != null ? interaction.HighlightRoot : null;
        if (marker == null)
            return;

        marker.transform.position = worldPosition;
        marker.transform.localScale = Vector3.one * scale;
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
        float dragScale,
        Transform magneticTarget = null)
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
        if (magneticTarget != null)
        {
            StoryChapterBuilderCommon.SetArray(
                dragData,
                "magneticSnapTargets",
                new Object[] { magneticTarget });
            dragData.FindProperty("magneticSnapViewportRadius").floatValue = 0.18f;
            dragData.FindProperty("magneticSnapMinTravelPixels").floatValue = 36f;
            dragData.FindProperty("magneticSnapStrength").floatValue = 1f;
            dragData.FindProperty("magneticSnapResponse").floatValue = 7.5f;
        }
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
        if (string.Equals(definition.id, "Food", StringComparison.Ordinal))
        {
            // The can prefab is slightly squashed along its authored cylinder axis.
            // Correct that axis after fitting so it reads as an upright can, not a short barrel.
            visual.transform.localScale = Vector3.Scale(
                visual.transform.localScale,
                new Vector3(1f, 1f, 1.06f));
        }

        bool signalItem = CategoryFor(definition.id) == StoryPreparationCategory.Signal;
        if (!signalItem)
        {
            Transform tableVisual = StorySharedHomePrefabBuilder.FindDescendant(
                world.sharedHome.transform,
                "SafeTable_Visual");
            if (tableVisual == null)
                throw new InvalidOperationException("Hazırlık masasının görünür yüzeyi bulunamadı.");

            float tableSurfaceY = GetVisibleBounds(tableVisual.gameObject).max.y;
            Bounds itemBounds = GetVisibleBounds(visual);
            wrapper.position += Vector3.up * (tableSurfaceY + 0.004f - itemBounds.min.y);
        }

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
        if (string.Equals(definition.id, "Food", StringComparison.Ordinal))
        {
            // The imported can is intentionally palm-sized. Keep its invisible touch target
            // forgiving on a phone without turning the objective badge into a second prop.
            EnsureMinimumWorldCollider(wrapperCollider, new Vector3(0.2f, 0.18f, 0.2f));
            Bounds foodBounds = GetVisibleBounds(visual);
            PlaceCompactObjectiveMarker(
                interactable,
                foodBounds.center + Vector3.up * 0.12f,
                0.3f);
        }
        SetAvailableOnStart(interactable);
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
                definition.size * 0.22f,
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
        // "Etiketi yana çevir" gerçek bir çevirmedir: şişe görseli bir pivot altına
        // alınır, tarih etiketi gövdeye yapışık ve başlangıçta kameradan UZAĞA
        // bakar; kaydırma tamamlanınca pivot dönerek etiketi doğudaki inceleme
        // kamerasına çevirir. (Eski kurulum şişenin yanında havada asılı, sırtı
        // dönük bir kare bırakıyordu ve hiçbir şey dönmüyordu.)
        Bounds waterBounds = GetVisibleBounds(waterRoot.gameObject);
        float waterRadius = Mathf.Max(waterBounds.extents.x, waterBounds.extents.z);
        Vector3 waterAxisCenter = new Vector3(
            waterBounds.center.x,
            waterBounds.center.y + 0.03f,
            waterBounds.center.z);

        Transform waterSpinPivot = StoryChapterBuilderCommon.NewChild(waterRoot, "WaterSpin_Pivot");
        waterSpinPivot.position = new Vector3(waterAxisCenter.x, waterBounds.min.y, waterAxisCenter.z);
        foreach (Transform waterChild in waterRoot.Cast<Transform>()
                     .Where(child => child != waterSpinPivot &&
                                     child.GetComponentsInChildren<Renderer>(true).Length > 0)
                     .ToArray())
            waterChild.SetParent(waterSpinPivot, true);

        const float labelStartAzimuth = 150f;
        Quaternion labelRotation = Quaternion.Euler(0f, labelStartAzimuth, 0f);
        Vector3 labelOutward = labelRotation * Vector3.right;
        Vector3 labelCenter = waterAxisCenter + labelOutward * (waterRadius + 0.006f);

        Transform waterUncheckedRoot = StoryChapterBuilderCommon.NewChild(
            waterSpinPivot,
            "WaterExpiryLabel_Unchecked");
        waterUncheckedRoot.position = labelCenter;
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryLabel_Surface",
            PrimitiveType.Cube,
            labelCenter,
            new Vector3(0.014f, 0.1f, 0.14f),
            LoadMaterial("Amber"),
            waterUncheckedRoot,
            false,
            labelRotation);
        StoryChapterBuilderCommon.CreateWorldLabel(
            "WaterExpiryDateText",
            "SKT\n2027",
            labelCenter + labelOutward * 0.009f,
            Quaternion.LookRotation(-labelOutward, Vector3.up).eulerAngles,
            0.055f,
            StoryChapterBuilderCommon.Navy,
            waterUncheckedRoot,
            new Vector2(0.13f, 0.09f));

        Transform waterCheckedRoot = StoryChapterBuilderCommon.NewChild(
            waterSpinPivot,
            "WaterExpiryCheckedState");
        waterCheckedRoot.position = labelCenter;
        StoryChapterBuilderCommon.CreatePrimitive(
            "WaterExpiryCheckmark_Badge",
            PrimitiveType.Cube,
            labelCenter + labelOutward * 0.012f + Vector3.up * 0.065f,
            new Vector3(0.012f, 0.034f, 0.034f),
            LoadMaterial("Teal"),
            waterCheckedRoot,
            false,
            labelRotation);
        waterCheckedRoot.gameObject.SetActive(false);

        AnimationClip waterSpinClip = GetOrCreateWaterSpinClip();
        Animation waterSpinAnimation = waterSpinPivot.gameObject.AddComponent<Animation>();
        waterSpinAnimation.playAutomatically = false;
        waterSpinAnimation.AddClip(waterSpinClip, "WaterLabelSpin");
        waterSpinAnimation.clip = waterSpinClip;

        // Kaydırma hedefi şişenin kendisidir: collider'ı dönmeyen bir gövde hacmi.
        // Dönen pivot bu hotspot'un ALTINDADIR; böylece etkileşim görünür şişe
        // render'larını içerir (validator şartı) ama collider sabit kalır.
        GameObject waterHotspot = new GameObject("WaterInspect_Hotspot");
        waterHotspot.transform.SetParent(waterRoot);
        waterHotspot.transform.position = waterAxisCenter;
        waterSpinPivot.SetParent(waterHotspot.transform, true);
        BoxCollider waterCollider = waterHotspot.AddComponent<BoxCollider>();
        waterCollider.size = new Vector3(
            Mathf.Max(0.26f, waterRadius * 2.4f),
            Mathf.Max(0.34f, waterBounds.size.y * 1.05f),
            Mathf.Max(0.26f, waterRadius * 2.4f));
        waterCollider.isTrigger = true;

        result.inspectWaterDate = StoryChapterBuilderCommon.AddInteractable(
            waterHotspot,
            "Inspect_WaterExpiryDate",
            "Su \u015fi\u015fesinin tarih etiketini nesnenin \u00fczerinde yana \u00e7evir",
            StoryInteractionKind.Inspect,
            null,
            StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.PreparationWaterInspection,
            true,
            1,
            1.1f,
            1.5f);
        // This is a close-up bottle turn, so the room-scale fallback collider
        // would cover most of the portrait screen. Keep it comfortably tappable
        // while matching the bottle silhouette and the safe-frame contract.
        SetMobileTouchHotspotWorldSize(result.inspectWaterDate, new Vector3(0.32f, 0.4f, 0.32f));
        // Hedef nokta -z tarafında: dar dikey kadrajda içeride kalan çevirme yönü.
        Transform waterSwipeTarget = StoryChapterBuilderCommon.CreatePoint(
            "WaterExpirySwipeTarget",
            waterRoot,
            waterAxisCenter + new Vector3(0f, 0.08f, -0.32f),
            waterAxisCenter);
        StoryChapterBuilderCommon.SetGestureTarget(result.inspectWaterDate, waterSwipeTarget);
        // Yakın plan kadrajında standart dünya işareti ekranın yarısını
        // kaplıyordu; şişe başı hizasında kompakt rozet yeterli.
        PlaceCompactObjectiveMarker(
            result.inspectWaterDate,
            new Vector3(waterAxisCenter.x, waterBounds.max.y + 0.12f, waterAxisCenter.z),
            0.26f);
        result.inspectWaterDate.SetAvailable(false);
        // Kaydırma şişeyi fiziksel olarak döndürür; tarih etiketi görünür kalır ve
        // dönüş sonrasında üstüne onay rozeti eklenir.
        UnityEventTools.AddStringPersistentListener(
            result.inspectWaterDate.OnInteracted,
            waterSpinAnimation.CrossFade,
            "WaterLabelSpin");
        UnityEventTools.AddBoolPersistentListener(
            result.inspectWaterDate.OnInteracted,
            waterCheckedRoot.gameObject.SetActive,
            true);
        UnityEventTools.AddPersistentListener(
            result.inspectWaterDate.OnInteracted,
            director.OnWaterDateChecked);

        Transform bandageRoot = StoryChapterBuilderCommon.NewChild(parent, "BandageSealInspection");
        Transform foodCabinet = StorySharedHomePrefabBuilder.FindDescendant(parent.root, "KitchenLowCabinet");
        if (foodCabinet == null)
            throw new InvalidOperationException("Sargı paketi için sağlık dolabı yüzeyi bulunamadı.");
        float bandageSurfaceY = GetVisibleBounds(foodCabinet.gameObject).max.y +
                                PreparationKitchenWorktopSurfaceOffsetY + 0.012f;
        bandageRoot.position = new Vector3(4.42f, bandageSurfaceY, -1.62f);
        GameObject bandageUnchecked = StoryChapterBuilderCommon.InstantiateAsset(
            QuaterniusBandagesPath,
            "BandageSeal_Unchecked",
            bandageRoot,
            bandageRoot.position,
            new Vector3(0.36f, 0.19f, 0.24f),
            new Vector3(0f, 82f, 0f),
            false,
            false);
        // This is a visual inspection beat, not a button challenge. The health
        // discovery interaction already brought the player here; the camera and
        // dialogue now inspect the real sealed package without a second hotspot,
        // hold timer, fake checkmark or invisible collider.
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
        StoryPreparationDirector director,
        PlayableInteractions result)
    {
        Transform sourceToy = StorySharedHomePrefabBuilder.FindDescendant(
            family.deniz.transform.root,
            "Can_ToyCar");
        if (sourceToy == null)
            throw new InvalidOperationException("Can'ın rahatlatıcı oyuncak arabası ortak evde bulunamadı.");
        // Oyuncak, Warmth kadrajının ortasında, sandığın hemen yanındaki zeminde durur.
        // (Eski konum Can'ın başlangıç pozuna bağlıydı; hem kadraj kenarına düşüyordu
        // hem de Anne'nin gövdesi arkasında kalıyordu.)
        Vector3 comfortToyPosition = new Vector3(-3.5f, 0.08f, -1.85f);
        sourceToy.position = comfortToyPosition;
        sourceToy.rotation = Quaternion.Euler(0f, 25f, 0f);

        // Can, sinyal aşamasında masanın yanına taşındığı için bu sahnede yeniden
        // konumlanır. Director onu bu poza yürütür; drop zone Can'ı takip eder.
        result.comfortCanPose = StoryChapterBuilderCommon.CreatePoint(
            "ComfortCanPose",
            parent,
            new Vector3(-2.55f, 0f, -1.75f),
            comfortToyPosition);

        GameObject resultToy = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "CanComfortToy_Result",
            parent,
            result.comfortCanPose.position + new Vector3(0.26f, 0.04f, 0.18f),
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
        // Drop zone Can'ın gövdesine bağlıdır; Can nereye yürürse "ona doğru çek"
        // hedefi de oraya gider. (Eski statik zone, Can'ın inşa anındaki pozunda
        // asılı kalıyor ve Can taşındığında boş zemini işaret ediyordu.)
        GameObject dropObject = new GameObject("CanComfortToyDropZone");
        dropObject.transform.SetParent(family.can.transform);
        dropObject.transform.position = family.can.transform.position + new Vector3(0f, 0.55f, 0f);
        dropObject.transform.rotation = Quaternion.identity;
        Vector3 canScale = family.can.transform.lossyScale;
        dropObject.transform.localScale = new Vector3(
            Mathf.Abs(canScale.x) > 0.0001f ? 1f / canScale.x : 1f,
            Mathf.Abs(canScale.y) > 0.0001f ? 1f / canScale.y : 1f,
            Mathf.Abs(canScale.z) > 0.0001f ? 1f / canScale.z : 1f);
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
        StoryChapterBuilderCommon.Set(
            serialized,
            "reviewSignalRadioBatteryInsert",
            interactions.reviewSignalRadioBatteryInsert);
        StoryChapterBuilderCommon.Set(serialized, "reviewSignalRadio", interactions.reviewSignalRadio);
        StoryChapterBuilderCommon.Set(
            serialized,
            "reviewSignalRadioBatteryRemove",
            interactions.reviewSignalRadioBatteryRemove);
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
        StoryChapterBuilderCommon.Set(serialized, "signalWhistleCanPose", world.whistleCanPose);
        StoryChapterBuilderCommon.Set(serialized, "reviewFood", interactions.reviewFood);
        StoryChapterBuilderCommon.Set(serialized, "reviewHealth", interactions.reviewHealth);
        StoryChapterBuilderCommon.Set(serialized, "reviewWarmth", interactions.reviewWarmth);
        StoryChapterBuilderCommon.Set(serialized, "chooseComfortItem", interactions.comfortItem);
        StoryChapterBuilderCommon.Set(serialized, "testBagWeight", interactions.testBagWeight);
        StoryChapterBuilderCommon.Set(serialized, "removeConsole", interactions.removeConsole);
        StoryChapterBuilderCommon.Set(serialized, "testBalancedBag", interactions.testBalancedBag);
        StoryChapterBuilderCommon.Set(serialized, "adjustBagStraps", interactions.adjustBagStraps);
        StoryChapterBuilderCommon.Set(serialized, "placeBagAtExit", interactions.placeBagAtExit);
        StoryChapterBuilderCommon.Set(
            serialized,
            "exitShelfApproachPoint",
            interactions.exitShelfApproachPoint);
        StoryChapterBuilderCommon.Set(serialized, "exitShelfDropRing", interactions.exitShelfDropRing);
        StoryChapterBuilderCommon.Set(serialized, "comfortCanPose", interactions.comfortCanPose);
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
        StoryTouchManager touch, StoryUIController ui, PlayableInteractions interactions,
        PreviewWorld world, StoryChapterBuilderCommon.Characters family)
    {
        StoryInteractable placeBagAtExit = interactions.placeBagAtExit;
        // Tatbikat, çanta rafa bırakıldığı anda başlar: Deniz raf yaklaşma
        // noktasında, Can ise rahatlatıcı eşya sahnesindeki pozunda durur. Sabit
        // hedefler karakterlerin inşa anındaki değil, bu gerçek oyun anındaki
        // konumlarına göre yerleştirilir.
        Vector3 drillDenizPosition = interactions.exitShelfApproachPoint != null
            ? interactions.exitShelfApproachPoint.position
            : family.deniz.transform.position;
        Vector3 drillCanPosition = interactions.comfortCanPose != null
            ? interactions.comfortCanPose.position
            : family.can.transform.position;
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
        // Bolum1 prefab'ı kendi eski DraggableItem'ını taşır. Temizlenmezse aynı
        // GameObject'te yapılandırılmamış ikinci bir sürükleyici kalır ve TouchManager
        // GetComponent ile onu bulup feneri asla ele teslim edemezdi.
        foreach (DraggableItem stale in blackoutFlashlightObject.GetComponentsInChildren<DraggableItem>(true))
            Object.DestroyImmediate(stale);
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
            drillDenizPosition + new Vector3(0.32f, 0.84f, 0.12f);
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
            drillCanPosition + new Vector3(0.18f, 0.84f, 0.12f),
            new Vector3(0.34f, 0.2f, 0.3f),
            new Vector3(0f, 0f, 90f),
            true,
            true);
        // Düdük yalnız RepeatedTap hedefidir; prefab'tan gelen eski DraggableItem
        // kendi dokunma girdisini çalıştırıp üçlü dokunuş sayacını bozuyordu.
        foreach (DraggableItem stale in blackoutWhistleObject.GetComponentsInChildren<DraggableItem>(true))
            Object.DestroyImmediate(stale);
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
            drillCanPosition + Vector3.up * 0.72f,
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
        // Tatbikat rafın, planın ve masanın etrafında geçer; el feneri ışığı
        // odanın ortasından üç bölgeyi de eşit besler (eski konum taşınan
        // çantanın eski yerine sabitti).
        flashlight.transform.position = new Vector3(2.4f, 1.2f, 1.7f);
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

        Light daylight = StorySharedHomePrefabBuilder.FindDescendant(parent, "Directional Light")
            ?.GetComponent<Light>();
        Light fillLight = StorySharedHomePrefabBuilder.FindDescendant(parent, "StoryFillLight")
            ?.GetComponent<Light>();
        BindBlackoutLight(
            timeline,
            playable,
            daylight,
            "Gün ışığı kesintisi",
            daylight != null ? daylight.intensity : 1.05f,
            timelineFolder + "/Story_01_BlackoutSun.anim");
        BindBlackoutLight(
            timeline,
            playable,
            fillLight,
            "Oda ışığı kesintisi",
            fillLight != null ? fillLight.intensity : 1.18f,
            timelineFolder + "/Story_01_BlackoutFill.anim");

        Transform practicalRoot = StorySharedHomePrefabBuilder.FindDescendant(
            parent,
            "Story01_PracticalLighting");
        Light[] practicalLights = practicalRoot == null
            ? Array.Empty<Light>()
            : practicalRoot.GetComponentsInChildren<Light>(true)
                .Where(candidate => candidate.name.StartsWith("Story01Practical_", StringComparison.Ordinal))
                .OrderBy(candidate => candidate.name)
                .ToArray();
        for (int index = 0; index < practicalLights.Length; index++)
        {
            Light practical = practicalLights[index];
            BindBlackoutLight(
                timeline,
                playable,
                practical,
                "Pratik lamba kesintisi " + (index + 1),
                practical.intensity,
                timelineFolder + "/Story_01_BlackoutPractical_" + index + ".anim");
        }

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

        // Çanta fiziksel olarak rafa geçtiğinde karanlık tatbikatı başlar. Timeline
        // kendi etkileşim duraklarında girişi yeniden açar ve yalnız son sinyal
        // final checkpoint'ini tamamlar; böylece oluşturulmuş tatbikat oynanıştan
        // kopuk, erişilemeyen bir sahne parçası olarak kalmaz.
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            world.wornBag.SetActive,
            false);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            world.exitBag.SetActive,
            true);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            touch.SetWorldNavigationEnabled,
            false);
        UnityEventTools.AddBoolPersistentListener(
            placeBagAtExit.OnInteracted,
            touch.SetInteractionsEnabled,
            false);
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
        animator.applyRootMotion = false;
        EditorUtility.SetDirty(animator);
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
            (column - 1.5f) * 0.09f,
            -0.3f + (index % 2) * 0.01f,
            (row - 1f) * 0.06f);
    }

    private static StoryCameraController BuildCameras(Transform parent, Transform deniz,
        out Camera mainCamera, out CinemachineBrain brain)
    {
        StoryChapterBuilderCommon.CameraSpec[] specs =
        {
            new(
                StoryCameraZoneId.PreparationOverview,
                "CM_PreparationOverview_Rebuild",
                new Vector3(1.85f, 2.38f, -3.18f),
                new Vector3(-2.55f, 0.98f, -1.55f),
                44f,
                false,
                deniz,
                5.72f,
                new Vector2(0.1f, 0.04f)),
            // Paketleme yakın planı: masa üstü ve masanın yanındaki açık çanta tek
            // kadrajda, karakter kafalarının üzerinden görünür. Eski geniş açıda
            // masayla çanta arasındaki boşluk kadrajı büyütüyor, karakterler
            // masanın önünü kapatıyordu.
            new(
                StoryCameraZoneId.PreparationBag,
                "CM_PreparationBag_Rebuild",
                // RightWall_Story01'in iç yüzünden güvenli mesafede kalır.
                new Vector3(3.85f, 3.05f, -4.15f),
                new Vector3(0.62f, 0.68f, 0.5f),
                60f),
            new(
                StoryCameraZoneId.PreparationParent,
                "CM_PreparationFamilyPlan_Rebuild",
                new Vector3(3.85f, 2.34f, -2.35f),
                new Vector3(-4.25f, 1.03f, 1.15f),
                58f),
            new(
                StoryCameraZoneId.PreparationSignal,
                "CM_PreparationSignal_Rebuild",
                new Vector3(0.15f, 2.02f, -1.68f),
                new Vector3(-0.76f, 0.52f, -3.63f),
                45f),
            new(
                StoryCameraZoneId.PreparationFlashlight,
                "CM_PreparationFlashlight_Rebuild",
                new Vector3(1.35f, 1.75f, -0.35f),
                new Vector3(-0.25f, 0.86f, 0.65f),
                40f),
            new(
                StoryCameraZoneId.PreparationRadio,
                "CM_PreparationRadio_Rebuild",
                new Vector3(3.7f, 2.05f, -1.9f),
                new Vector3(0.4f, 0.88f, 0.65f),
                50f),
            new(
                StoryCameraZoneId.PreparationFood,
                "CM_PreparationKitchen_Rebuild",
                new Vector3(1.05f, 2.35f, -0.15f),
                new Vector3(4.35f, 0.84f, -1.82f),
                48f),
            new(
                StoryCameraZoneId.PreparationHealth,
                "CM_PreparationAid_Rebuild",
                new Vector3(1.25f, 2.45f, -0.05f),
                new Vector3(4.5f, 1.05f, -1.92f),
                46f),
            new(
                StoryCameraZoneId.PreparationWaterInspection,
                "CM_PreparationWaterInspection_Rebuild",
                new Vector3(1.7f, 2.02f, -1.02f),
                new Vector3(-0.4f, 1.0f, 0.3f),
                38f),
            new(
                StoryCameraZoneId.PreparationBandageInspection,
                "CM_PreparationBandageInspection_Rebuild",
                new Vector3(3.25f, 1.72f, -2.75f),
                new Vector3(4.43f, 1f, -1.55f),
                38f),
            // Sandık hattına yandan bakan bu açı, ışını Anne (kuzey) ile Deniz'in
            // başlangıç pozu (güney) arasındaki koridordan geçirir; sandık, oyuncak
            // ve Can'ın comfort pozu karakter gövdesiyle kapanmadan görünür.
            new(
                StoryCameraZoneId.PreparationWarmth,
                "CM_PreparationWarmth_Rebuild",
                new Vector3(1.2f, 2.5f, -1.95f),
                new Vector3(-4.1f, 0.35f, -1.8f),
                45f),
            new(
                StoryCameraZoneId.PreparationSiblingHandoff,
                "CM_PreparationSiblingHandoff_Rebuild",
                new Vector3(2.75f, 1.9f, -1.55f),
                new Vector3(0.12f, 0.82f, 0.32f),
                43f),
            new(
                StoryCameraZoneId.PreparationWrongChoice,
                "CM_PreparationConsole_Rebuild",
                new Vector3(3.8f, 2.3f, -2.2f),
                new Vector3(1.7f, 0.35f, 0.5f),
                42f),
            new(
                StoryCameraZoneId.PreparationBagFit,
                "CM_PreparationDeniz_Rebuild",
                new Vector3(-4.5f, 2.35f, -1.75f),
                new Vector3(-1.75f, 0.88f, -2.25f),
                45f,
                false,
                deniz,
                3.35f,
                new Vector2(0f, 0.04f)),
            new(
                StoryCameraZoneId.PreparationExitShelf,
                "CM_PreparationExitShelf_Rebuild",
                new Vector3(1.15f, 2.55f, 1.85f),
                new Vector3(4.15f, 1f, 5.3f),
                48f)
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

    private static void EnsureMinimumWorldCollider(BoxCollider collider, Vector3 minimumWorldSize)
    {
        // Imported props can carry rotated, non-uniform root scales. Dividing a desired
        // world size by lossyScale treats those axes as if they were world-aligned and can
        // turn a palm-sized prop into a room-sized invisible collider. Rebuild the box from
        // world-space corners instead so the touch volume stays centred on the visible prop.
        Bounds currentWorldBounds = collider.bounds;
        Bounds desiredWorldBounds = new Bounds(
            currentWorldBounds.center,
            Vector3.Max(currentWorldBounds.size, minimumWorldSize));
        FitColliderToWorldBounds(collider, desiredWorldBounds);
    }

    private static void FitColliderToWorldBounds(BoxCollider collider, Bounds worldBounds)
    {
        Vector3[] corners =
        {
            new(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
            new(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
            new(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
            new(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
            new(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
            new(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
            new(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
            new(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
        };
        Bounds localBounds = new Bounds(
            collider.transform.InverseTransformPoint(corners[0]),
            Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            localBounds.Encapsulate(collider.transform.InverseTransformPoint(corner));
        collider.center = localBounds.center;
        collider.size = localBounds.size;
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
        // Dokunma tabanı dünya uzayında 12 cm'dir. Bu tabanı local uzayda uygulamak,
        // büyük import kök ölçeği taşıyan modellerde (ör. 26–64x) görünmez oda
        // boyutunda collider üretip başka nesnelerin dokunuşlarını çalıyordu.
        Vector3 lossyScale = collider.transform.lossyScale;
        Vector3 localMinimum = new Vector3(
            0.12f / Mathf.Max(Mathf.Abs(lossyScale.x), 0.0001f),
            0.12f / Mathf.Max(Mathf.Abs(lossyScale.y), 0.0001f),
            0.12f / Mathf.Max(Mathf.Abs(lossyScale.z), 0.0001f));
        collider.size = Vector3.Max(local.size, localMinimum);
    }

    private static Bounds GetVisibleBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(root.name + " görünür renderer taşımıyor.");

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Bounds GetEnabledVisibleBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();
        if (renderers.Length == 0)
            throw new InvalidOperationException(root.name + " aktif görünür renderer taşımıyor.");

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
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
            Require(
                StoryChapterBuilderCommon.HasCanonicalStory01HomeShell(sharedHome.transform),
                "Story 01 kanonik ev kabuğu");

            Require(Find(root.transform, "PreparationSetDressing") != null, "Story 01 sahne giydirme kökü");
            Transform interiorArt = Find(root.transform, "PreparationInteriorArtPass");
            Transform ceiling = Find(root.transform, "Story01_CeilingMain");
            Require(interiorArt != null, "Story 01 kalıcı iç mekân art-pass kökü");
            Require(ceiling != null && ceiling.GetComponent<Collider>() == null,
                "Kamerayı ve NavMesh'i itmeyen kapalı Story 01 tavanı");
            Require(new[]
                {
                    "Story01_KitchenFridge",
                    "Story01_KitchenUpperStorage",
                    "Story01_EntryPlant",
                    "Story01_PendantFixture",
                    "Story01_Gallery_FamilyMemory"
                }.All(name => Find(root.transform, name) != null),
                "Mutfak, salon ve girişte okunur yaşam katmanları");
            Require(interiorArt.GetComponentsInChildren<Light>(true)
                    .Count(light => light.name.StartsWith("Story01Practical_", StringComparison.Ordinal)) == 3,
                "Üç bölgeli sıcak pratik iç mekân ışığı");
            Require(Find(root.transform, "FamilyPlanBoard") != null, "Aile plan panosu");
            Require(Find(root.transform, "EmergencyBag_Open_Packing") != null, "Fiziksel açık afet çantası");
            Require(Find(root.transform, "OriginalBolum1BagSource") != null,
                "Procedural kutu yerine orijinal Bölüm 1 açık çanta modeli");
            Require(Find(root.transform, "PhysicalBagOpening")?.GetComponent<BagDropZone>() != null,
                "Doğrudan bırakma hedefi");

            CinemachineCamera[] cameras = root.GetComponentsInChildren<CinemachineCamera>(true);
            Require(cameras.Length >= 10, "En az 10 bestelenmiş Cinemachine kamera");
            Require(cameras.All(camera =>
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 60f),
                "Kamera lensleri 38–60 derece mobil kabul aralığında");

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
            string[] modelSurfaceHotspotIds =
            {
                "Inspect_FlashlightSwitchOn",
                "Inspect_FlashlightSwitchOff",
                "Review_PowerEmergencyRadio"
            };
            Require(interactions.All(interaction =>
                    interaction.GetComponentsInChildren<Renderer>(true).Length > 0 ||
                    modelSurfaceHotspotIds.Contains(interaction.InteractionId)),
                "Etkileşim görünür nesneye veya gerçek model kontrolünün üstündeki dar yüzey hotspot'una bağlıdır");
            Require(interactions
                    .Where(interaction => modelSurfaceHotspotIds.Contains(interaction.InteractionId))
                    .All(interaction =>
                        interaction.GetComponent<BoxCollider>() != null &&
                        interaction.GetComponent<MeshRenderer>() == null &&
                        interaction.HighlightRoot.transform.Find("ObjectiveBadge")?.gameObject.activeSelf == false &&
                        interaction.HighlightRoot.transform.Find("ObjectiveGestureIcon")?.gameObject.activeSelf == false),
                "Model yüzeyi kontrolleri sahte primitive çizmez; yalnız küçük hedef oku kullanır");
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
            Require(discoveries[0].InteractionGesture == StoryInteractionGesture.SwipeDiagonalDownRight &&
                    discoveries.Skip(1).All(interaction =>
                        interaction.InteractionGesture == StoryInteractionGesture.SwipeHorizontal) &&
                    discoveries.All(interaction =>
                        interaction.InteractFromAnywhere && interaction.GestureTarget == null),
                "Çekmece kamera perspektifinde sağ alta çekilir; diğer mobilyalar yatay sürükleme kullanır");
            Transform signalDrawer = Find(root.transform, "SignalNightstandOpen")
                ?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "Nightstand_02_Door");
            Transform signalDrawerSlide = signalDrawer?.parent;
            Require(signalDrawer != null &&
                    signalDrawerSlide != null &&
                    signalDrawerSlide.name == "SignalDrawerSlide" &&
                    signalDrawerSlide.localPosition == Vector3.zero &&
                    signalDrawerSlide.GetComponent<Animation>()?.clip != null &&
                    AssetDatabase.GetAssetPath(signalDrawerSlide.GetComponent<Animation>().clip) ==
                    "Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim",
                "Sinyal çekmecesi Y'yi bozmayan nötr Z hareket pivotunda gerçek açılma animasyonu taşır");
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
                Find(root.transform, "WaterInspect_Hotspot")?.GetComponent<StoryInteractable>();
            Transform bandageSeal = Find(root.transform, "BandageSealInspection");
            Animation waterSpin = Find(root.transform, "WaterSpin_Pivot")?.GetComponent<Animation>();
            Require(waterDate != null &&
                    waterDate.InteractionGesture == StoryInteractionGesture.SwipeHorizontal &&
                    waterDate.GestureTarget != null &&
                    waterDate.OnInteracted.GetPersistentEventCount() >= 3 &&
                    waterSpin != null && waterSpin.clip != null,
                "Su şişesinin tarihi, şişeyi fiziksel döndüren kaydırmayla kontrol edilir");
            Require(bandageSeal != null &&
                    bandageSeal.GetComponent<StoryInteractable>() == null &&
                    bandageSeal.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    Find(root.transform, "BandageSealCheckedState") == null,
                "Sargı paketi ikinci bir basılı tutma görevi üretmeden kamera ve diyalogla incelenir");
            Require(Find(root.transform, "WaterExpiryCheckedState") != null,
                "Su etiketi fiziksel çevirmeden sonra yalnız renge bağlı olmayan sonuç görseli üretir");

            string[] reviewNames =
            {
                "Inspect_FlashlightSwitchOn",
                "Inspect_FlashlightSwitchOff",
                "Review_InsertBatteryIntoRadio",
                "Review_PowerEmergencyRadio",
                "Review_RemoveBatteryFromRadio",
                "Review_ClipWhistleToCan"
            };
            Require(reviewNames.All(name =>
                    interactions.Any(interaction => interaction.InteractionId == name)),
                "Fener, radyo pili, radyo güç düğmesi ve düdük görünür gerçek nesneleri üzerinden kontrol edilir");
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
                    physicalBagLift.gameObject == Find(root.transform, "EmergencyBag_Open_Packing")?.gameObject &&
                    physicalBagLift.InteractionGesture == StoryInteractionGesture.DragToTarget &&
                    physicalBagLift.GestureTarget != null &&
                    physicalBagLift.GestureTarget.name == "BagWeightLiftDropZone" &&
                    physicalBagLift.GetComponent<DraggableItem>() != null,
                "Konsol çatışması görünür açık çanta kaldırılarak ve konsol fiziksel sürüklenerek çözülür");
            StoryInteractable comfort =
                Find(root.transform, "CanComfortToy_Draggable")?.GetComponent<StoryInteractable>();
            Require(comfort != null &&
                    comfort.InteractionGesture == StoryInteractionGesture.DragToTarget &&
                    comfort.GetComponent<DraggableItem>() != null,
                "Can'ın rahatlatıcı eşyası doğrudan ona sürüklenir");
            Transform canRoot = Find(root.transform, "Can_8");
            Transform comfortDrop = Find(root.transform, "CanComfortToyDropZone");
            Require(canRoot != null && comfortDrop != null && comfortDrop.IsChildOf(canRoot),
                "Rahatlatıcı eşya hedefi Can'ın gövdesini takip eder; boş zemine sabitlenmez");
            Require(comfort.GestureTarget == comfortDrop,
                "Rahatlatıcı eşyanın jest hedefi Can üzerindeki bırakma bölgesidir");
            StoryInteractable finalPlacement = interactions.SingleOrDefault(interaction =>
                interaction.InteractionId == "Final_PlaceBagAtExit");
            Require(finalPlacement != null &&
                    finalPlacement.FocusCameraZone == StoryCameraZoneId.PreparationExitShelf &&
                    !finalPlacement.ReturnCameraAfterCompletion,
                "Final çanta sürüklemesi raf kadrajında yapılır; kamera final tablosundan koparılmaz");
            Require(Find(root.transform, "ExitShelfApproachPoint") != null,
                "Deniz'in final sürüklemeden önce yürüyeceği raf yaklaşma noktası");
            Require(interactions.Single(interaction =>
                        interaction.InteractionId == "Resolve_RemoveConsole")
                    .FocusCameraZone == StoryCameraZoneId.PreparationBag,
                "Konsol, masa drop hedefini de gösteren çanta kadrajında geri bırakılır");

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
