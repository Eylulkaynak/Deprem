using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class StoryEvacuationSceneBuilder
{
    public const string RebuildPreviewScenePath = "Assets/Scenes/Story_04_RebuildPreview.unity";
    private const string ItemPrefabRoot = "Assets/Bolum1Prefab";
    private const string AdultPrefabPath = "Assets/Story/Prefabs/Preparation/Anne_Ayse.prefab";
    private const string CuratedTownEnvironmentRoot = "Assets/Story/Environment/SyntyTown";
    private const string KenneyBedrollPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx";
    private const string KenneySurvivalMaterialPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Materials/KenneySurvival_Atlas.mat";

    private sealed class RebuildWorld
    {
        internal EvacuationWorld route;
        internal GameObject cardboardBlocking;
        internal GameObject cardboardCleared;
        internal Animation cardboardAnimation;
        internal GameObject neighborSupportBracelet;
        internal GameObject facadeSafePoint;
        internal GameObject worker;
        internal GameObject mother;
        internal GameObject father;
        internal Animation motherApproach;
        internal Animation fatherApproach;
        internal GameObject checkInClipboard;
        internal GameObject familyHeadcountPending;
        internal GameObject familyHeadcountComplete;
        internal GameObject treatmentTray;
        internal GameObject nerminCupTarget;
        internal GameObject radioPrepared;
        internal GameObject radioFallback;
        internal GameObject radioTuningDial;
        internal GameObject radioTunedIndicator;
        internal GameObject workerRadioIndicator;
        internal Animation radioTuneAnimation;
        internal AudioSource radioPreparedBroadcast;
        internal AudioSource workerRadioBroadcast;
        internal Animation streetWarningSignAnimation;
        internal Animation streetWarningShardAnimation;
        internal AudioSource streetWarningCreak;
        internal GameObject streetWarningShard;
        internal GameObject firstAidPrepared;
        internal GameObject firstAidFallback;
        internal GameObject waterPrepared;
        internal GameObject waterFallback;
        internal GameObject blanketPrepared;
        internal GameObject blanketFallback;
        internal GameObject contactPrepared;
        internal GameObject contactFallback;
        internal GameObject familyCallCard;
        internal GameObject comfortToyAtAssembly;
        internal GameObject reunionTarget;
    }

    private sealed class RebuildInteractions
    {
        internal StoryInteractable inspectCorridor;
        internal StoryInteractable chooseStairs;
        internal StoryInteractable tryElevator;
        internal StoryInteractable reachUpperLanding;
        internal StoryInteractable holdHandrail;
        internal StoryInteractable reachLowerLanding;
        internal StoryInteractable callNeighbor;
        internal StoryInteractable moveCardboard;
        internal StoryInteractable moveFoam;
        internal StoryInteractable moveCane;
        internal StoryInteractable guideNeighbor;
        internal StoryInteractable openBuildingExit;
        internal StoryInteractable moveAwayFromFacade;
        internal StoryInteractable inspectStreetHazard;
        internal StoryInteractable takeSafeSidewalk;
        internal StoryInteractable tryUnsafeShortcut;
        internal StoryInteractable readAssemblySign;
        internal StoryInteractable handNeighborToWorker;
        internal StoryInteractable checkCan;
        internal StoryInteractable useRadio;
        internal StoryInteractable listenWorkerRadio;
        internal StoryInteractable useFirstAid;
        internal StoryInteractable useStationCloth;
        internal StoryInteractable giveWater;
        internal StoryInteractable useWaterStation;
        internal StoryInteractable giveBlanket;
        internal StoryInteractable moveToWindbreak;
        internal StoryInteractable useContactCard;
        internal StoryInteractable useRegistrySheet;
        internal StoryInteractable useWhistle;
        internal StoryInteractable callFamily;
        internal StoryInteractable reuniteFamily;
    }

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Build Story_04_RebuildPreview")]
    public static void BuildRebuildPreviewFromMenu() => BuildRebuildPreview(true);

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Build Story_04_RebuildPreview (Silent)")]
    public static void BuildRebuildPreviewSilentFromMenu() => BuildRebuildPreview(false);

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Validate Story_04_RebuildPreview")]
    public static void ValidateRebuildPreviewFromMenu() => ValidateRebuildPreview(true);

    public static void BuildRebuildPreviewFromCommandLine() => BuildRebuildPreview(false);

    private static void BuildRebuildPreview(bool showDialog)
    {
        try
        {
            StoryChapterBuilderCommon.EnsureFolders();
            StoryChapterBuilderCommon.Materials materials = StoryChapterBuilderCommon.CreateMaterials();
            UnityEngine.Rendering.VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();
            RuntimeAnimatorController childController = StoryAnimationLibraryBuilder.BuildLibrary(false);
            RuntimeAnimatorController adultController = StoryAnimationLibraryBuilder.LoadAdultController();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_04_REBUILD_PREVIEW");
            EvacuationWorld route = BuildWorld(root.transform, materials, adultController);
            RemoveInstructionalGroundLanguage(route.environment.transform, materials);

            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                false,
                new Vector3(-0.45f, 4.03f, -4.4f),
                new Vector3(0.65f, 4.03f, -4.75f),
                Vector3.zero);

            route.flashlightBeam.transform.SetParent(family.deniz.transform, true);
            route.flashlightBeam.transform.localPosition = new Vector3(0f, 0.9f, 0.18f);
            route.flashlightBeam.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

            RebuildWorld world = BuildRebuildWorld(
                root.transform,
                route,
                family,
                materials,
                adultController);

            GameObject sessionObject = new GameObject("_StorySession_RebuildPreview");
            StoryGameManager gameManager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(
                gameManager,
                StoryAct.Evacuation,
                new[]
                {
                    StoryFlag.BagReady,
                    StoryFlag.BagFlashlight,
                    StoryFlag.BagWhistle,
                    StoryFlag.BagRadio,
                    StoryFlag.BagFirstAid,
                    StoryFlag.BagWater,
                    StoryFlag.BagBlanket,
                    StoryFlag.BagDocuments,
                    StoryFlag.BagComfortItem,
                    StoryFlag.WardrobeSecured,
                    StoryFlag.ShelfSecured,
                    StoryFlag.ExitCleared
                });
            StoryChapterBuilderCommon.ConfigureRebuildStoryRoute(gameManager);

            GameObject core = new GameObject("_StoryEvacuationRebuildCore");
            core.transform.SetParent(root.transform);
            StoryEvacuationDirector director = core.AddComponent<StoryEvacuationDirector>();
            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);
            StorySiblingFollower follower = StoryChapterBuilderCommon.ConfigureSibling(
                family.can,
                family.deniz.transform);

            StoryChapterBuilderCommon.CameraSpec[] cameraSpecs =
            {
                new(StoryCameraZoneId.EvacuationCorridor, "CM04R_CorridorPair",
                    new Vector3(2.4f, 6.3f, -8.4f), new Vector3(0f, 4.55f, -2.65f), 47f, true),
                new(StoryCameraZoneId.EvacuationElevator, "CM04R_ElevatorEvidence",
                    new Vector3(-1.35f, 6.55f, -5.45f), new Vector3(2.15f, 5.1f, -2.4f), 48f, true),
                new(StoryCameraZoneId.EvacuationStairDoor, "CM04R_StairDoorReveal",
                    new Vector3(1.5f, 5.45f, -3.65f), new Vector3(0f, 4.95f, 0.1f), 42f, true),
                new(StoryCameraZoneId.EvacuationStairsTop, "CM04R_StairDescent",
                    new Vector3(3.9f, 5.15f, 0.55f), new Vector3(0f, 2.9f, 4.35f), 44f, true,
                    family.deniz.transform, 7.4f, new Vector2(-0.12f, 0.12f)),
                new(StoryCameraZoneId.EvacuationLanding, "CM04R_AftershockTwoShot",
                    new Vector3(4.15f, 3.75f, 5.95f), new Vector3(0.15f, 2.55f, 8.1f), 43f, true),
                new(StoryCameraZoneId.EvacuationLowerLanding, "CM04R_LowerLandingReveal",
                    new Vector3(2.05f, 2.75f, 13.35f), new Vector3(-0.15f, 0.75f, 18.05f), 43f, true),
                new(StoryCameraZoneId.EvacuationNeighbor, "CM04R_NerminHelp",
                    new Vector3(1.85f, 2.7f, 15.2f), new Vector3(-0.15f, 0.85f, 18.05f), 48f, true),
                new(StoryCameraZoneId.EvacuationBuildingDoor, "CM04R_ExitDoor",
                    new Vector3(2.0f, 2.8f, 17.35f), new Vector3(0f, 1.05f, 21.2f), 42f, true),
                new(StoryCameraZoneId.EvacuationBuildingFront, "CM04R_FacadeClear",
                    new Vector3(5.1f, 3.45f, 25.7f), new Vector3(0f, 1.55f, 21.3f), 47f, true),
                new(StoryCameraZoneId.EvacuationStreetInspect, "CM04R_StreetHazardRead",
                    new Vector3(4.9f, 3.35f, 24.9f), new Vector3(-0.65f, 0.35f, 28.7f), 44f, true),
                new(StoryCameraZoneId.EvacuationStreet, "CM04R_StreetJourney",
                    new Vector3(7.2f, 4.6f, 29.1f), new Vector3(1.9f, 1.05f, 35.8f), 48f, true),
                new(StoryCameraZoneId.EvacuationAssembly, "CM04R_AssemblyReunion",
                    new Vector3(0.8f, 3.25f, 35.6f), new Vector3(0.4f, 1.0f, 41.8f), 48f, true),
                new(StoryCameraZoneId.EvacuationAssemblyRadio, "CM04R_AssemblyCare",
                    new Vector3(3.0f, 3.0f, 36.5f), new Vector3(-1.0f, 1.0f, 41.2f), 48f, true),
                new(StoryCameraZoneId.EvacuationHazard, "CM04R_NearMiss",
                    new Vector3(-4.35f, 3.15f, 31.3f), new Vector3(-1.3f, 0.65f, 27.3f), 44f, true)
            };
            StoryCameraController cameraController = StoryChapterBuilderCommon.BuildCameras(
                root.transform,
                StoryCameraZoneId.EvacuationCorridor,
                cameraSpecs,
                out Camera mainCamera,
                out CinemachineBrain brain);
            mainCamera.backgroundColor = new Color32(112, 146, 158, 255);
            StoryChapterBuilderCommon.ChapterUI ui = StoryChapterBuilderCommon.BuildUI(
                root.transform,
                cameraController,
                "StoryUI_Evacuation_Rebuild",
                "4. PERDE • TAHLİYE",
                "AİLE TOPLANMA ALANINDA BULUŞTU",
                "Hazırlık eşyaları kullanıldı; Nermin görevliye ulaştı ve aile fiziksel olarak yeniden buluştu.",
                StoryAct.Evacuation);

            StoryChapterBuilderCommon.SetReference(ui.controller, "movementOwner", movement);
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", ui.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchData = new SerializedObject(touch);
            touchData.FindProperty("directWorldGestures").boolValue = true;
            touchData.ApplyModifiedPropertiesWithoutUndo();

            RebuildInteractions interactions = BuildRebuildInteractions(world, family, director);
            ConfigureRebuildDirector(
                director,
                gameManager,
                movement,
                touch,
                cameraController,
                ui,
                family,
                follower,
                world,
                interactions);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(ui.controller, "cameraController", cameraController);

            StoryChapterBuilderCommon.BuildLighting(
                root.transform,
                volume,
                new Color(0.74f, 0.84f, 0.95f),
                1.04f);
            StoryChapterBuilderCommon.CreateLicensedAmbience(
                "DistantCityAndAssemblyAmbience",
                root.transform,
                "sfx100v2_loop_highway.ogg",
                0.09f);
            StoryChapterBuilderCommon.CreateLicensedAmbience(
                "OutdoorAirLayer",
                root.transform,
                "sfx100v2_loop_ambient_04.ogg",
                0.035f);
            StoryChapterBuilderCommon.AttachInteractionAudioLayer(
                root.transform,
                "Story04_ObjectInteractionAudio");
            StoryChapterBuilderCommon.DisableShadows(root.transform);

            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            StoryChapterBuilderCommon.BuildNavigation(route.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateRebuildPreview(false);
            Selection.activeGameObject = root;
            Debug.Log("Story_04_RebuildPreview built successfully: " + RebuildPreviewScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story_04 rebuild önizleme sahnesi üretildi ve doğrulandı.",
                    "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story_04 rebuild üretilemedi:\n" + exception.Message,
                    "Tamam");
            throw;
        }
    }

    private static RebuildWorld BuildRebuildWorld(
        Transform root,
        EvacuationWorld route,
        StoryChapterBuilderCommon.Characters family,
        StoryChapterBuilderCommon.Materials materials,
        RuntimeAnimatorController adultController)
    {
        Transform routeRoot = route.environment.transform.Find("ContinuousEvacuationRoute");
        RebuildWorld world = new RebuildWorld { route = route };

        ReplaceNeighborLandingProps(route, routeRoot);

        Transform looseFacadeSign = route.streetHazard.GetComponentsInChildren<Transform>(true)
            .First(candidate => candidate.name == "LooseFacadeSign");
        Vector3 settledSignRotation = looseFacadeSign.localEulerAngles;
        world.streetWarningSignAnimation = StoryChapterBuilderCommon.CreateRotationAnimation(
            looseFacadeSign.gameObject,
            "Story04Rebuild_LooseSignWarning",
            settledSignRotation + new Vector3(0f, 0f, -8f),
            settledSignRotation,
            0.9f);
        world.streetWarningShard = StoryChapterBuilderCommon.CreatePrimitive(
            "StreetWarningGlassShard_Slide",
            PrimitiveType.Cube,
            new Vector3(-0.52f, 0.055f, 27.42f),
            new Vector3(0.22f, 0.035f, 0.34f),
            materials.glass,
            routeRoot,
            false,
            Quaternion.Euler(4f, 28f, 7f));
        world.streetWarningShardAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.streetWarningShard,
            "Story04Rebuild_GlassShardWarningSlide",
            new Vector3(-0.48f, 0.14f, -0.32f),
            0.72f);
        world.streetWarningCreak = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            "StreetWarning_LooseSignCreak",
            routeRoot,
            looseFacadeSign.position,
            StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_metal_02.ogg"),
            0.34f,
            0.86f,
            1.3f,
            13f);

        world.cardboardBlocking = InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "NeighborCardboard_Blocking_Drag",
            routeRoot,
            new Vector3(0.05f, 0.02f, 17.3f),
            new Vector3(0.62f, 0.5f, 0.56f),
            new Vector3(0f, -12f, 0f),
            true);
        world.cardboardCleared = InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "NeighborCardboard_Cleared",
            routeRoot,
            new Vector3(1.82f, 0.02f, 17.15f),
            new Vector3(0.62f, 0.5f, 0.56f),
            new Vector3(0f, 8f, 0f),
            true);
        world.cardboardCleared.SetActive(false);
        world.cardboardAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.cardboardCleared,
            "Story04Rebuild_NeighborCardboardCleared",
            world.cardboardBlocking.transform.localPosition - world.cardboardCleared.transform.localPosition,
            0.62f);

        world.neighborSupportBracelet = StoryChapterBuilderCommon.CreatePrimitive(
            "NerminSupportBracelet_Hold",
            PrimitiveType.Sphere,
            route.neighborAtLanding.transform.position + new Vector3(0.38f, 0.92f, -0.03f),
            new Vector3(0.13f, 0.13f, 0.13f),
            materials.amber,
            route.neighborAtLanding.transform,
            true);

        world.facadeSafePoint = StoryChapterBuilderCommon.CreatePrimitive(
            "FacadeClearPavingPoint",
            PrimitiveType.Cylinder,
            new Vector3(0.55f, 0.025f, 25.25f),
            new Vector3(0.7f, 0.035f, 0.7f),
            materials.concrete,
            routeRoot,
            true);

        BuildCityWorldBox(routeRoot, materials);
        BuildStreetDressing(routeRoot, materials);
        BuildAssemblySet(root, routeRoot, family, materials, adultController, world);
        return world;
    }

    private static void ReplaceNeighborLandingProps(
        EvacuationWorld route,
        Transform routeRoot)
    {
        if (route.debrisBlocking != null)
            Object.DestroyImmediate(route.debrisBlocking);
        if (route.debrisCleared != null)
            Object.DestroyImmediate(route.debrisCleared);

        route.debrisBlocking = InstantiateTownEnvironment(
            "SM_Prop_FloorMat_01.fbx",
            "NeighborFloorMat_Blocking_Drag",
            routeRoot,
            new Vector3(0.68f, 0.02f, 17.92f),
            new Vector3(0.78f, 0.78f, 0.78f),
            new Vector3(0f, -14f, 0f),
            true);
        route.debrisCleared = InstantiateTownEnvironment(
            "SM_Prop_FloorMat_01.fbx",
            "NeighborFloorMat_Cleared",
            routeRoot,
            new Vector3(1.82f, 0.02f, 18.18f),
            new Vector3(0.78f, 0.78f, 0.78f),
            new Vector3(0f, 6f, 0f),
            true);
        route.debrisCleared.SetActive(false);
        route.debrisAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            route.debrisCleared,
            "Story04Rebuild_FloorMatCleared",
            route.debrisBlocking.transform.localPosition - route.debrisCleared.transform.localPosition,
            0.62f);
    }

    private static void BuildCityWorldBox(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform worldBox = StoryChapterBuilderCommon.NewChild(routeRoot, "Story04_CityWorldBox");
        GameObject ground = StoryChapterBuilderCommon.CreatePrimitive(
            "CityWorldBox_Ground",
            PrimitiveType.Cube,
            new Vector3(0f, -0.34f, 18f),
            new Vector3(48f, 0.2f, 82f),
            materials.concrete,
            worldBox,
            false);
        ConfigureBackdropRenderer(ground, true);

        foreach ((string name, Vector3 position, Vector3 scale, Material material) panel in new[]
                 {
                     ("CityWorldBox_NorthSky", new Vector3(0f, 2.75f, 59f),
                         new Vector3(120f, 5.5f, 0.25f), materials.sky),
                     ("CityWorldBox_NorthHaze", new Vector3(0f, 1.4f, 58.75f),
                         new Vector3(120f, 2.8f, 0.3f), materials.sky),
                     ("CityWorldBox_WestSky", new Vector3(-24f, 3.5f, 18f),
                         new Vector3(0.25f, 7f, 82f), materials.sky),
                     ("CityWorldBox_EastSky", new Vector3(24f, 3.5f, 18f),
                         new Vector3(0.25f, 7f, 82f), materials.sky),
                     ("CityWorldBox_SouthSky", new Vector3(0f, 3.5f, -23f),
                         new Vector3(52f, 7f, 0.25f), materials.sky)
                 })
        {
            GameObject backdrop = StoryChapterBuilderCommon.CreatePrimitive(
                panel.name,
                PrimitiveType.Cube,
                panel.position,
                panel.scale,
                panel.material,
                worldBox,
                false);
            ConfigureBackdropRenderer(backdrop, false);
        }

        (Vector3 position, Vector3 size, Vector3 euler)[] distantBlocks =
        {
            (new Vector3(-17f, 0f, 52f), new Vector3(6.4f, 8.1f, 6.2f), new Vector3(0f, 180f, 0f)),
            (new Vector3(-10.2f, 0f, 52.7f), new Vector3(6.8f, 9.2f, 6.5f), new Vector3(0f, 180f, 0f)),
            (new Vector3(-3.2f, 0f, 52.2f), new Vector3(6.5f, 7.7f, 6.2f), new Vector3(0f, 180f, 0f)),
            (new Vector3(3.8f, 0f, 52.8f), new Vector3(6.7f, 9.5f, 6.4f), new Vector3(0f, 180f, 0f)),
            (new Vector3(10.8f, 0f, 52.1f), new Vector3(6.5f, 8.3f, 6.1f), new Vector3(0f, 180f, 0f)),
            (new Vector3(17.5f, 0f, 52.6f), new Vector3(6.2f, 7.6f, 5.9f), new Vector3(0f, 180f, 0f)),
            (new Vector3(-14.5f, 0f, 17f), new Vector3(6.3f, 8.6f, 6.1f), new Vector3(0f, 90f, 0f)),
            (new Vector3(-14.5f, 0f, 38f), new Vector3(6.8f, 9.4f, 6.5f), new Vector3(0f, 90f, 0f)),
            (new Vector3(14.5f, 0f, 18f), new Vector3(6.5f, 8.0f, 6.2f), new Vector3(0f, -90f, 0f)),
            (new Vector3(14.5f, 0f, 40f), new Vector3(6.7f, 9.0f, 6.4f), new Vector3(0f, -90f, 0f))
        };
        for (int index = 0; index < distantBlocks.Length; index++)
        {
            (Vector3 position, Vector3 size, Vector3 euler) block = distantBlocks[index];
            GameObject building = InstantiateTownEnvironment(
                index % 2 == 0 ? "SM_Bld_House_Preset_04.fbx" : "SM_Bld_House_Preset_06.fbx",
                "DistantCityBlock_" + (index + 1).ToString("00"),
                worldBox,
                block.position,
                block.size,
                block.euler);
            ConfigureBackdropRenderer(building, false);
        }
    }

    private static void ConfigureBackdropRenderer(GameObject root, bool receiveShadows)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    private static void BuildStreetDressing(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform dressing = StoryChapterBuilderCommon.NewChild(routeRoot, "RebuildStreetDressing");
        StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Env_ResidentBuilding_03.prefab",
            "StreetBackgroundBuilding_A",
            dressing,
            new Vector3(-7.2f, 0f, 31.5f),
            new Vector3(7.6f, 9.2f, 8.2f),
            new Vector3(0f, 90f, 0f),
            false,
            false);
        StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Env_CommercialBuilding_02.prefab",
            "StreetBackgroundBuilding_B",
            dressing,
            new Vector3(-7.0f, 0f, 40.8f),
            new Vector3(7.2f, 7.8f, 7.4f),
            new Vector3(0f, 90f, 0f),
            false,
            false);

        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) building in new[]
                 {
                     ("SM_Bld_House_Preset_04.fbx", "SyntyApartmentAcrossRoad_A",
                         new Vector3(7.0f, 0f, 27.3f), new Vector3(6.8f, 6.7f, 6.2f),
                         new Vector3(0f, -90f, 0f)),
                     ("SM_Bld_House_Preset_06.fbx", "SyntyApartmentAcrossRoad_B",
                         new Vector3(7.2f, 0f, 38.0f), new Vector3(7.2f, 7.1f, 6.8f),
                         new Vector3(0f, -90f, 0f))
                 })
            InstantiateTownEnvironment(
                building.asset,
                building.name,
                dressing,
                building.position,
                building.size,
                building.euler);

        foreach ((string asset, string name, Vector3 position, float height) tree in new[]
                 {
                     ("Prop_Tree_04.prefab", "StreetTree_A", new Vector3(-4.6f, 0f, 34.4f), 3.5f),
                     ("Prop_Tree_06.prefab", "StreetTree_B", new Vector3(5.3f, 0f, 45.5f), 3.1f)
                 })
            StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + tree.asset,
                tree.name,
                dressing,
                tree.position,
                new Vector3(2.3f, tree.height, 2.3f),
                Vector3.zero,
                false,
                false);

        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) prop in new[]
                 {
                     ("SM_Env_Tree_01.fbx", "TownTree_Facade", new Vector3(-4.35f, 0f, 25.8f),
                         new Vector3(2.7f, 4.6f, 2.7f), Vector3.zero),
                     ("SM_Env_Tree_02.fbx", "TownTree_Assembly", new Vector3(4.5f, 0f, 44.7f),
                         new Vector3(2.9f, 4.8f, 2.9f), new Vector3(0f, 28f, 0f)),
                     ("SM_Env_Bush_01.fbx", "TownBush_Facade", new Vector3(-3.3f, 0f, 23.4f),
                         new Vector3(1.3f, 0.85f, 1.15f), new Vector3(0f, 15f, 0f)),
                     ("SM_Env_Bush_02.fbx", "TownBush_Assembly", new Vector3(3.0f, 0f, 44.9f),
                         new Vector3(1.5f, 0.9f, 1.2f), new Vector3(0f, -24f, 0f)),
                     ("SM_Prop_ParkBench_01.fbx", "StreetRestBench", new Vector3(-3.55f, 0f, 37.8f),
                         new Vector3(2.2f, 1.05f, 0.9f), new Vector3(0f, 90f, 0f)),
                     ("SM_Prop_RubbishBin_01.fbx", "StreetRubbishBin", new Vector3(-3.45f, 0f, 39.3f),
                         new Vector3(0.72f, 1.0f, 0.72f), new Vector3(0f, -20f, 0f)),
                     ("SM_Prop_Sign_BusStop_01.fbx", "DamagedBusStopLandmark", new Vector3(2.3f, 0f, 34.4f),
                         new Vector3(0.65f, 2.55f, 0.45f), new Vector3(0f, 180f, 0f)),
                     ("SM_Prop_StreetSign_Arrow_01.fbx", "AssemblyDirectionSign", new Vector3(2.1f, 0f, 39.2f),
                         new Vector3(1.4f, 2.3f, 0.5f), new Vector3(0f, 180f, 0f))
                 })
            InstantiateTownEnvironment(
                prop.asset,
                prop.name,
                dressing,
                prop.position,
                prop.size,
                prop.euler);

        GameObject firetruck = InstantiateTownEnvironment(
            "SM_Veh_Firetruck_01.fbx",
            "EmergencyFiretruck",
            dressing,
            new Vector3(4.15f, 0f, 37.0f),
            new Vector3(2.45f, 2.45f, 5.8f),
            Vector3.zero);
        firetruck.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        BuildEmergencyVehicleLights(firetruck.transform, materials);

        for (int i = 0; i < 3; i++)
            StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/Prop_RoadCone_0" + (i + 1) + ".prefab",
                "EmergencyLaneCone_" + i,
                dressing,
                new Vector3(2.25f, 0.02f, 29.5f + i * 3.2f),
                new Vector3(0.42f, 0.68f, 0.42f),
                new Vector3(0f, i * 18f, 0f),
                false,
                false);

        foreach (float z in new[] { 29.0f, 36.5f })
        {
            GameObject post = InstantiateTownEnvironment(
                "SM_Prop_Streetlamp_01.fbx",
                "SyntyStreetLamp",
                dressing,
                new Vector3(2.1f, 0f, z),
                new Vector3(0.65f, 3.45f, 0.65f),
                new Vector3(0f, 90f, 0f));
            GameObject lamp = StoryChapterBuilderCommon.CreatePrimitive(
                "StreetLampGlow",
                PrimitiveType.Sphere,
                new Vector3(2.1f, 3.3f, z),
                new Vector3(0.24f, 0.18f, 0.24f),
                materials.amber,
                post.transform,
                false);
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.38f);
            light.intensity = 0.8f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;
        }

        for (int i = 0; i < 4; i++)
        {
            float z = 40.7f + i * 1.35f;
            InstantiateTownEnvironment(
                "SM_Env_Fence_White_Straight_01.fbx",
                "AssemblyBoundaryFence_" + i,
                dressing,
                new Vector3(-4.7f, 0f, z),
                new Vector3(0.18f, 1.15f, 1.45f),
                new Vector3(0f, 90f, 0f));
        }

        StoryAuthoredPropFactory.CreateDebrisCluster(
            "FacadeEdgeDebris",
            dressing,
            new Vector3(-2.9f, 0.02f, 24.8f),
            new Vector3(1.3f, 0.38f, 1.1f),
            new Vector3(0f, 22f, 0f),
            materials.concrete,
            materials.cream);
    }

    private static void BuildAssemblySet(
        Transform root,
        Transform routeRoot,
        StoryChapterBuilderCommon.Characters family,
        StoryChapterBuilderCommon.Materials materials,
        RuntimeAnimatorController adultController,
        RebuildWorld world)
    {
        Transform set = StoryChapterBuilderCommon.NewChild(routeRoot, "RebuildAssemblySet");
        world.route.assemblySign.transform.position = new Vector3(-0.75f, 0.04f, 43.15f);
        BuildOpenAssemblyCanopy(set, materials);

        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyWorkerTable",
            PrimitiveType.Cube,
            new Vector3(-2.45f, 0.72f, 41.1f),
            new Vector3(2.55f, 0.18f, 0.9f),
            materials.wood,
            set,
            true);
        StoryChapterBuilderCommon.CreatePrimitive(
            "WorkerTableLegL",
            PrimitiveType.Cube,
            new Vector3(-3.35f, 0.35f, 41.1f),
            new Vector3(0.14f, 0.7f, 0.14f),
            materials.dark,
            set,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "WorkerTableLegR",
            PrimitiveType.Cube,
            new Vector3(-1.55f, 0.35f, 41.1f),
            new Vector3(0.14f, 0.7f, 0.14f),
            materials.dark,
            set,
            false);

        world.worker = StoryChapterBuilderCommon.InstantiateCharacter(
            AdultPrefabPath,
            "AssemblyWorker",
            set,
            new Vector3(-2.65f, 0.02f, 42.15f),
            1.68f,
            adultController);
        AddWorkerVest(world.worker.transform, materials);

        world.mother = StoryChapterBuilderCommon.InstantiateCharacter(
            AdultPrefabPath,
            "Anne_Assembly_Reunion",
            set,
            new Vector3(0.7f, 0.02f, 41.55f),
            1.68f,
            adultController);
        world.father = StoryChapterBuilderCommon.InstantiateCharacter(
            AdultPrefabPath,
            "Baba_Assembly_Reunion",
            set,
            new Vector3(1.55f, 0.02f, 41.85f),
            1.78f,
            adultController);
        world.mother.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        world.father.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        AddFatherAccessory(world.father.transform, materials);
        world.motherApproach = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.mother,
            "Story04Rebuild_MotherApproach",
            new Vector3(0f, 0f, 2.4f),
            1.8f);
        world.fatherApproach = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.father,
            "Story04Rebuild_FatherApproach",
            new Vector3(0f, 0f, 2.7f),
            1.9f);
        world.mother.SetActive(false);
        world.father.SetActive(false);
        world.reunionTarget = world.mother;

        BuildAssemblyDressing(set, materials, adultController);

        world.checkInClipboard = CreateClipboard(
            "AssemblyCheckInClipboard",
            set,
            new Vector3(-2.45f, 0.88f, 40.92f),
            materials);
        BuildFamilyHeadcountMarks(
            world.checkInClipboard,
            materials,
            out world.familyHeadcountPending,
            out world.familyHeadcountComplete);
        world.treatmentTray = StoryChapterBuilderCommon.CreatePrimitive(
            "WorkerTreatmentTray_Target",
            PrimitiveType.Cube,
            new Vector3(-1.55f, 0.88f, 41.12f),
            new Vector3(0.65f, 0.05f, 0.5f),
            materials.metal,
            set,
            true);
        world.nerminCupTarget = StoryChapterBuilderCommon.CreatePrimitive(
            "NerminCup_Target",
            PrimitiveType.Cylinder,
            new Vector3(-0.95f, 0.58f, 41.15f),
            new Vector3(0.18f, 0.05f, 0.18f),
            materials.cream,
            set,
            true);

        world.radioPrepared = InstantiateItem(
            "RadioPrepared_Use",
            "Radio.prefab",
            set,
            new Vector3(-3.15f, 0.92f, 41.0f),
            new Vector3(0.55f, 0.36f, 0.32f),
            new Vector3(-90f, -141f, 0f));
        BuildPhysicalRadioPayoff(materials, world);
        world.radioFallback = CreateMegaphone(
            "WorkerMegaphone_Fallback",
            set,
            new Vector3(-3.15f, 1.05f, 41.2f),
            materials);
        BuildWorkerRadioPayoff(materials, world);
        world.firstAidPrepared = StoryAuthoredPropFactory.CreateFirstAidKit(
            "FirstAidPrepared_Drag",
            set,
            new Vector3(-1.9f, 0.96f, 40.85f),
            new Vector3(0.6f, 0.42f, 0.38f),
            new Vector3(0f, -8f, 0f),
            materials.coral,
            materials.navy,
            materials.cream,
            materials.amber,
            true);
        world.firstAidFallback = StoryAuthoredPropFactory.CreateFoldedCloth(
            "CleanCloth_Fallback_Drag",
            set,
            new Vector3(-2.0f, 0.91f, 41.28f),
            new Vector3(0.42f, 0.09f, 0.34f),
            Vector3.zero,
            materials.cream,
            materials.teal,
            true);
        world.waterPrepared = InstantiateItem(
            "WaterPrepared_Drag",
            "Item_Su.prefab",
            set,
            new Vector3(-0.95f, 0.88f, 40.72f),
            new Vector3(0.25f, 0.46f, 0.25f),
            new Vector3(-90f, 0f, 0f));
        world.waterFallback = StoryAuthoredPropFactory.CreateCeramicMug(
            "StationWaterCup_Fallback_Drag",
            set,
            new Vector3(-0.95f, 0.84f, 40.92f),
            new Vector3(0.3f, 0.35f, 0.3f),
            Vector3.zero,
            materials.cream,
            materials.teal);
        world.blanketPrepared = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyBedrollPath,
            "BlanketPrepared_Drag",
            set,
            new Vector3(0.95f, 0.22f, 41.0f),
            new Vector3(0.76f, 0.24f, 0.3f),
            new Vector3(0f, 10f, 0f),
            false,
            true,
            AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath));
        world.blanketFallback = StoryChapterBuilderCommon.CreatePrimitive(
            "WindbreakTent_Fallback",
            PrimitiveType.Cube,
            new Vector3(2.25f, 1.05f, 42.1f),
            new Vector3(2.0f, 2.05f, 0.12f),
            materials.amber,
            set,
            true);
        world.contactPrepared = InstantiateItem(
            "ContactCardPrepared_Drag",
            "dockument.prefab",
            set,
            new Vector3(-2.75f, 0.95f, 40.84f),
            new Vector3(0.4f, 0.08f, 0.52f),
            new Vector3(0f, -6f, 0f));
        world.contactFallback = CreatePencil(
            "RegistryPencil_Fallback",
            set,
            new Vector3(-2.2f, 0.94f, 40.88f),
            materials);

        world.familyCallCard = CreateFamilyCallCard(
            "FamilyCallCard_Fallback",
            set,
            new Vector3(1.1f, 0.95f, 41.18f),
            materials);
        ReplaceVoiceSignal(world.route, world.familyCallCard);

        world.radioFallback.SetActive(false);
        world.firstAidFallback.SetActive(false);
        world.waterFallback.SetActive(false);
        world.blanketFallback.SetActive(false);
        world.contactFallback.SetActive(false);
        world.familyCallCard.SetActive(false);

        Transform canWarmTarget = StoryChapterBuilderCommon.NewChild(
            family.can.transform,
            "CanBlanketDropAnchor");
        canWarmTarget.localPosition = new Vector3(0f, 0.7f, 0f);

        Transform canRightHand = StoryChapterBuilderCommon.FindHumanoidBone(
            family.can,
            HumanBodyBones.RightHand);
        if (canRightHand == null)
            throw new InvalidOperationException("Can sağ el kemiği bulunamadı.");

        world.comfortToyAtAssembly = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Toy_02.prefab",
            "CanComfortToy_Assembly",
            canRightHand,
            canRightHand.position,
            new Vector3(0.18f, 0.1f, 0.25f),
            Vector3.zero,
            false);
        world.comfortToyAtAssembly.transform.localPosition = new Vector3(0.08f, -0.02f, 0.03f);
        world.comfortToyAtAssembly.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        world.comfortToyAtAssembly.SetActive(false);
    }

    private static GameObject InstantiateTownEnvironment(
        string fileName,
        string name,
        Transform parent,
        Vector3 position,
        Vector3 size,
        Vector3 euler,
        bool ensureCollider = false)
    {
        Material townAtlas = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PolygonTown/Materials/PolygonTown_01_A.mat");
        if (townAtlas == null)
            throw new InvalidOperationException("Curated Synty Town atlas material could not be loaded.");

        return StoryChapterBuilderCommon.InstantiateAsset(
            CuratedTownEnvironmentRoot + "/" + fileName,
            name,
            parent,
            position,
            size,
            euler,
            false,
            ensureCollider,
            townAtlas);
    }

    private static void BuildOpenAssemblyCanopy(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach ((string name, Vector3 position, float roll) roof in new[]
                 {
                     ("AssemblyCanopyRoof_Left", new Vector3(-3.25f, 2.48f, 42.25f), -9f),
                     ("AssemblyCanopyRoof_Right", new Vector3(-1.55f, 2.48f, 42.25f), 9f)
                 })
            StoryChapterBuilderCommon.CreatePrimitive(
                roof.name,
                PrimitiveType.Cube,
                roof.position,
                new Vector3(1.85f, 0.12f, 3.35f),
                materials.teal,
                set,
                false,
                Quaternion.Euler(0f, 0f, roof.roll));

        foreach (Vector3 position in new[]
                 {
                     new Vector3(-4.0f, 1.2f, 40.72f),
                     new Vector3(-0.8f, 1.2f, 40.72f),
                     new Vector3(-4.0f, 1.2f, 43.78f),
                     new Vector3(-0.8f, 1.2f, 43.78f)
                 })
            StoryChapterBuilderCommon.CreatePrimitive(
                "AssemblyCanopyPost",
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.055f, 1.2f, 0.055f),
                materials.metal,
                set,
                false);

        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyCanopyHeader",
            PrimitiveType.Cube,
            new Vector3(-2.4f, 2.14f, 40.68f),
            new Vector3(3.25f, 0.32f, 0.1f),
            materials.cream,
            set,
            false);
        StoryChapterBuilderCommon.CreateWorldLabel(
            "AssemblyAidStationLabel",
            "YARDIM NOKTASI",
            new Vector3(-2.4f, 2.14f, 40.61f),
            Vector3.zero,
            1.45f,
            StoryChapterBuilderCommon.Navy,
            set,
            new Vector2(2.6f, 0.36f));
    }

    private static void BuildEmergencyVehicleLights(
        Transform vehicle,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach ((string name, Vector3 offset, Material material, Color color) beacon in new[]
                 {
                     ("EmergencyBeaconRed", new Vector3(-0.38f, 2.12f, 0.45f), materials.coral,
                         new Color(1f, 0.12f, 0.08f)),
                     ("EmergencyBeaconBlue", new Vector3(0.38f, 2.12f, 0.45f), materials.teal,
                         new Color(0.08f, 0.45f, 1f))
                 })
        {
            GameObject glow = StoryChapterBuilderCommon.CreatePrimitive(
                beacon.name,
                PrimitiveType.Cube,
                vehicle.TransformPoint(beacon.offset),
                new Vector3(0.28f, 0.12f, 0.18f),
                beacon.material,
                vehicle,
                false);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = beacon.color;
            light.intensity = 1.1f;
            light.range = 4.8f;
            light.shadows = LightShadows.None;
        }
    }

    private static void BuildAssemblyDressing(
        Transform set,
        StoryChapterBuilderCommon.Materials materials,
        RuntimeAnimatorController adultController)
    {
        InstantiateTownEnvironment(
            "SM_Prop_ParkBench_01.fbx",
            "AssemblyWaitingBench",
            set,
            new Vector3(2.65f, 0f, 43.7f),
            new Vector3(2.35f, 1.1f, 0.95f),
            new Vector3(0f, 180f, 0f));
        InstantiateTownEnvironment(
            "SM_Prop_RubbishBin_01.fbx",
            "AssemblyRubbishBin",
            set,
            new Vector3(3.6f, 0f, 41.2f),
            new Vector3(0.72f, 1f, 0.72f),
            new Vector3(0f, 20f, 0f));
        InstantiateTownEnvironment(
            "SM_Env_Tree_02.fbx",
            "AssemblyBoundaryTree",
            set,
            new Vector3(-1.25f, 0f, 47.25f),
            new Vector3(3.3f, 5.5f, 3.3f),
            new Vector3(0f, 18f, 0f));
        InstantiateTownEnvironment(
            "SM_Env_Bush_02.fbx",
            "AssemblyBoundaryBush",
            set,
            new Vector3(2.65f, 0f, 46.45f),
            new Vector3(1.8f, 1.05f, 1.3f),
            new Vector3(0f, -12f, 0f));
        for (int i = 0; i < 5; i++)
            InstantiateTownEnvironment(
                "SM_Env_Fence_White_Straight_01.fbx",
                "AssemblyRearFence_" + i,
                set,
                new Vector3(-3.0f + i * 1.5f, 0f, 46.25f),
                new Vector3(1.5f, 1.1f, 0.18f),
                Vector3.zero);

        foreach ((string prefab, string name, Vector3 position, float height, float yaw) person in new[]
                 {
                     ("Character_Roadworker_01.prefab", "AssemblyCrowd_Roadworker",
                         new Vector3(-4.0f, 0.02f, 44.45f), 1.74f, 35f),
                     ("Character_Tourist_01.prefab", "AssemblyCrowd_Tourist",
                         new Vector3(2.65f, 0.02f, 44.1f), 1.68f, -145f),
                     ("Character_HipsterGirl_01.prefab", "AssemblyCrowd_Resident",
                         new Vector3(3.25f, 0.02f, 42.2f), 1.64f, -110f)
                 })
        {
            GameObject crowd = StoryChapterBuilderCommon.InstantiateCharacter(
                StoryChapterBuilderCommon.SyntyCityCharacterRoot + "/" + person.prefab,
                person.name,
                set,
                person.position,
                person.height,
                adultController);
            crowd.transform.rotation = Quaternion.Euler(0f, person.yaw, 0f);
        }

        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblySupplyCrate_A",
            PrimitiveType.Cube,
            new Vector3(-4.0f, 0.28f, 41.0f),
            new Vector3(0.85f, 0.55f, 0.75f),
            materials.wood,
            set,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblySupplyCrate_B",
            PrimitiveType.Cube,
            new Vector3(-4.05f, 0.74f, 41.2f),
            new Vector3(0.7f, 0.42f, 0.62f),
            materials.cream,
            set,
            false,
            Quaternion.Euler(0f, -8f, 0f));

        GameObject stationLight = StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyStationLight",
            PrimitiveType.Sphere,
            new Vector3(-2.5f, 2.35f, 41.45f),
            new Vector3(0.2f, 0.15f, 0.2f),
            materials.cream,
            set,
            false);
        Light light = stationLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.62f);
        light.intensity = 1.3f;
        light.range = 5.2f;
        light.shadows = LightShadows.None;
    }

    private static void ReplaceVoiceSignal(EvacuationWorld routeWorld, GameObject familyCallCard)
    {
        if (routeWorld.voiceSignalWorld != null)
            Object.DestroyImmediate(routeWorld.voiceSignalWorld);
        routeWorld.voiceSignalWorld = familyCallCard;
    }

    private static RebuildInteractions BuildRebuildInteractions(
        RebuildWorld world,
        StoryChapterBuilderCommon.Characters family,
        StoryEvacuationDirector director)
    {
        RebuildInteractions result = new RebuildInteractions();
        Transform points = StoryChapterBuilderCommon.NewChild(
            world.route.environment.transform,
            "EvacuationRebuildInteractionPoints");

        Transform corridorPoint = Point(points, "R04_CorridorPoint", new Vector3(0f, 4.02f, -1.25f),
            new Vector3(0f, 4.8f, 0f));
        Transform stairsPoint = Point(points, "R04_StairDoorPoint", new Vector3(0f, 4.02f, -0.8f),
            world.route.stairDoorClosed.transform.position);
        Transform elevatorPoint = Point(points, "R04_ElevatorPoint", new Vector3(1.2f, 4.02f, -2.0f),
            world.route.elevatorButton.transform.position);
        Transform upperPoint = Point(points, "R04_UpperLandingPoint", new Vector3(0f, 2.02f, 8.0f),
            world.route.handrail.transform.position);
        Transform lowerPoint = Point(points, "R04_LowerLandingPoint", new Vector3(0.25f, 0.02f, 17.25f),
            world.route.neighborAtLanding.transform.position);
        Transform neighborPoint = Point(points, "R04_NeighborPoint", new Vector3(0.1f, 0.02f, 18.0f),
            world.route.neighborAtLanding.transform.position + Vector3.up);
        Transform exitPoint = Point(points, "R04_ExitPoint", new Vector3(0f, 0.02f, 20.0f),
            world.route.buildingDoorClosed.transform.position);
        Transform facadePoint = Point(points, "R04_FacadePoint", world.facadeSafePoint.transform.position,
            new Vector3(0f, 1.2f, 21.1f));
        Transform streetPoint = Point(points, "R04_StreetPoint", new Vector3(0.65f, 0.02f, 25.4f),
            world.route.streetHazard.transform.position + Vector3.up);
        Transform assemblyPoint = Point(points, "R04_AssemblyPoint", new Vector3(0.35f, 0.02f, 40.25f),
            world.route.assemblySign.transform.position + Vector3.up);
        Transform reunionPoint = Point(points, "R04_FamilyReunionPoint", new Vector3(0.95f, 0.02f, 41.25f),
            world.mother.transform.position + Vector3.up);

        result.inspectCorridor = Add04RInteraction(world.route.corridorThreshold,
            "evac.r04.corridor.inspect", "Kapı eşiğini geçip koridoru dinle", StoryInteractionKind.Inspect,
            corridorPoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationCorridor);
        Renderer corridorThresholdRenderer = world.route.corridorThreshold.GetComponent<Renderer>();
        if (corridorThresholdRenderer != null)
            corridorThresholdRenderer.enabled = false;
        BoxCollider corridorThresholdCollider = world.route.corridorThreshold.GetComponent<BoxCollider>();
        if (corridorThresholdCollider != null)
            corridorThresholdCollider.isTrigger = true;
        SerializedObject corridorTrigger = new SerializedObject(result.inspectCorridor);
        corridorTrigger.FindProperty("autoTriggerOnPlayerEnter").boolValue = true;
        corridorTrigger.ApplyModifiedPropertiesWithoutUndo();
        result.chooseStairs = Add04RInteraction(world.route.stairDoorClosed,
            "evac.r04.route.stairs", "Merdiven kapısını yana çek", StoryInteractionKind.Exit, stairsPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationStairDoor, false, 1, 1.1f, 2f);
        result.tryElevator = Add04RInteraction(world.route.elevatorButton,
            "evac.r04.route.elevator_unsafe", "Asansör çağrı düğmesini yokla", StoryInteractionKind.UnsafeChoice,
            elevatorPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationElevator, false, 1, 0.55f, 2f);
        result.reachUpperLanding = Add04RInteraction(world.route.upperLanding,
            "evac.r04.stairs.upper", "İlk sahanlığa kontrollü in", StoryInteractionKind.Exit, upperPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationLanding, false, 1, 1.4f, 2.4f);
        result.holdHandrail = Add04RInteraction(world.route.handrail,
            "evac.r04.aftershock.handrail", "Korkuluğun üzerinde basılı tut", StoryInteractionKind.TakeCover,
            upperPoint, StoryInteractionGesture.WorldHold, StoryCameraZoneId.EvacuationLanding, false, 1, 1.8f, 2.2f);
        result.reachLowerLanding = Add04RInteraction(world.route.lowerLanding,
            "evac.r04.stairs.lower", "Alt sahanlığa kontrollü in", StoryInteractionKind.Exit, lowerPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationLowerLanding, false, 1, 1.4f, 2.5f);
        result.callNeighbor = Add04RInteraction(world.route.neighborAtLanding,
            "evac.r04.neighbor.ask", "Nermin teyzeye iyi olup olmadığını sor", StoryInteractionKind.HelpSibling,
            neighborPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationNeighbor, false, 1, 0.75f, 2.2f);

        result.moveCardboard = Add04RDrag(world.cardboardBlocking, world.cardboardCleared,
            "evac.r04.neighbor.cardboard", "Karton kutuyu boş duvar kenarına sürükle",
            StoryInteractionKind.HelpSibling, StoryCameraZoneId.EvacuationNeighbor, points,
            new Vector3(1.1f, 0.8f, 0.95f));
        result.moveFoam = Add04RDrag(world.route.debrisBlocking, world.route.debrisCleared,
            "evac.r04.neighbor.foam", "Hafif köpüğü duvar dibine sürükle",
            StoryInteractionKind.HelpSibling, StoryCameraZoneId.EvacuationNeighbor, points,
            new Vector3(1.45f, 0.75f, 1.15f));
        result.moveCane = Add04RDrag(world.route.caneBlocked, world.route.caneReachable,
            "evac.r04.neighbor.cane", "Bastonu Nermin teyzenin eline sürükle",
            StoryInteractionKind.HelpSibling, StoryCameraZoneId.EvacuationNeighbor,
            world.route.neighborAtLanding.transform, new Vector3(0.85f, 1.4f, 0.8f));
        result.guideNeighbor = Add04RInteraction(world.neighborSupportBracelet,
            "evac.r04.neighbor.support", "Nermin teyzenin yanında basılı tut",
            StoryInteractionKind.HelpSibling, neighborPoint, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationNeighbor, false, 1, 1.5f, 2.2f);

        result.openBuildingExit = Add04RInteraction(world.route.buildingDoorClosed,
            "evac.r04.exit.door", "Dış kapıyı yana çek", StoryInteractionKind.Exit, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationBuildingDoor, false, 1, 1.1f, 2.2f);
        result.moveAwayFromFacade = Add04RInteraction(world.facadeSafePoint,
            "evac.r04.exit.facade_clear", "Bina cephesinden açık noktaya geç", StoryInteractionKind.Exit,
            facadePoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationBuildingFront, false, 1, 1.2f, 3f);
        result.inspectStreetHazard = Add04RInteraction(world.route.streetHazard,
            "evac.r04.street.inspect", "Cam sınırından açık kaldırıma doğru sür", StoryInteractionKind.Inspect,
            streetPoint, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationStreetInspect, false, 1, 1.15f, 3f);
        StoryChapterBuilderCommon.SetGestureTarget(
            result.inspectStreetHazard,
            world.route.safeSidewalk.transform);
        result.takeSafeSidewalk = Add04RInteraction(world.route.safeSidewalk,
            "evac.r04.street.safe", "Açık yan kaldırımdan ilerle", StoryInteractionKind.Exit,
            streetPoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationStreet, false, 1, 1.4f, 3f);
        result.tryUnsafeShortcut = Add04RInteraction(world.route.unsafeShortcut,
            "evac.r04.street.unsafe", "Camlı kestirmeyi dene", StoryInteractionKind.UnsafeChoice,
            streetPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationHazard, false, 1, 0.6f, 3f);

        result.readAssemblySign = Add04RInteraction(world.route.assemblySign,
            "evac.r04.assembly.sign", "Toplanma levhasını doğrula", StoryInteractionKind.Inspect,
            assemblyPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationAssembly, false, 1, 0.75f, 3f);
        result.handNeighborToWorker = Add04RInteraction(world.route.neighborAtAssembly,
            "evac.r04.assembly.handoff", "Nermin teyzenin yanında basılı tut",
            StoryInteractionKind.HelpSibling, assemblyPoint, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationAssemblyRadio, false, 1, 1.2f, 3f);
        result.checkCan = Add04RInteraction(family.can,
            "evac.r04.assembly.can", "Can'ın elini ve nefesini kontrol et",
            StoryInteractionKind.HelpSibling, assemblyPoint, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationAssembly, true, 1, 1.1f, 3f);
        result.useRadio = Add04RInteraction(world.radioPrepared,
            "evac.r04.assembly.radio", "Radyo ayar düğmesini çevir",
            StoryInteractionKind.Inspect, assemblyPoint, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationAssemblyRadio, false, 1, 1.0f, 3.2f);
        result.listenWorkerRadio = Add04RInteraction(world.radioFallback,
            "evac.r04.assembly.radio_fallback", "Görevlinin hoparlörünü dinle",
            StoryInteractionKind.Inspect, assemblyPoint, StoryInteractionGesture.Tap,
            StoryCameraZoneId.EvacuationAssemblyRadio, false, 1, 0.8f, 3.2f);
        Configure04RReturnCamera(result.useRadio, StoryCameraZoneId.EvacuationAssembly, 0.85f);
        Configure04RReturnCamera(result.listenWorkerRadio, StoryCameraZoneId.EvacuationAssembly, 0.85f);
        result.useFirstAid = Add04RDrag(world.firstAidPrepared, world.treatmentTray,
            "evac.r04.assembly.firstaid", "İlk yardım setini tedavi tepsisine sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssemblyRadio, points,
            new Vector3(0.85f, 0.4f, 0.7f));
        result.useStationCloth = Add04RDrag(world.firstAidFallback, world.treatmentTray,
            "evac.r04.assembly.cloth_fallback", "Temiz bezi tedavi tepsisine sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssemblyRadio, points,
            new Vector3(0.85f, 0.4f, 0.7f));
        result.giveWater = Add04RDrag(world.waterPrepared, world.nerminCupTarget,
            "evac.r04.assembly.water", "Su şişesini Nermin teyzeye sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssembly,
            world.route.neighborAtAssembly.transform, new Vector3(0.8f, 0.8f, 0.8f));
        result.useWaterStation = Add04RDrag(world.waterFallback, world.nerminCupTarget,
            "evac.r04.assembly.water_fallback", "Dağıtım bardağını Nermin teyzeye sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssembly,
            world.route.neighborAtAssembly.transform, new Vector3(0.8f, 0.8f, 0.8f));
        Transform canBlanketAnchor = family.can.transform.Find("CanBlanketDropAnchor");
        result.giveBlanket = Add04RDrag(world.blanketPrepared, canBlanketAnchor.gameObject,
            "evac.r04.assembly.blanket", "Battaniyeyi Can'ın omuzlarına sürükle",
            StoryInteractionKind.HelpSibling, StoryCameraZoneId.EvacuationAssembly,
            family.can.transform, new Vector3(1.0f, 1.35f, 0.9f));
        result.moveToWindbreak = Add04RInteraction(world.blanketFallback,
            "evac.r04.assembly.windbreak_fallback", "Can'la rüzgâr kesen tenteye geç",
            StoryInteractionKind.HelpSibling, assemblyPoint, StoryInteractionGesture.Approach,
            StoryCameraZoneId.EvacuationAssembly, false, 1, 1.1f, 3.2f);
        result.useContactCard = Add04RDrag(world.contactPrepared, world.checkInClipboard,
            "evac.r04.assembly.contact", "Aile iletişim kartını kayıt panosuna sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssembly, points,
            new Vector3(0.8f, 0.4f, 0.75f));
        result.useRegistrySheet = Add04RInteraction(world.contactFallback,
            "evac.r04.assembly.registry_fallback", "Kayıt kalemini sayfa üzerinde çek",
            StoryInteractionKind.Inspect, assemblyPoint, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationAssembly, false, 1, 1f, 3.2f);
        result.useWhistle = Add04RInteraction(world.route.whistleWorld,
            "evac.r04.assembly.whistle", "Düdüğe üç kısa kez dokun",
            StoryInteractionKind.Collect, assemblyPoint, StoryInteractionGesture.RepeatedTap,
            StoryCameraZoneId.EvacuationAssembly, true, 3, 1.1f, 3.5f);
        result.callFamily = Add04RInteraction(world.familyCallCard,
            "evac.r04.assembly.call_fallback", "Aile çağrı kartında basılı tut",
            StoryInteractionKind.HelpSibling, assemblyPoint, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationAssembly, true, 1, 1.2f, 3.5f);
        result.reuniteFamily = Add04RInteraction(world.reunionTarget,
            "evac.r04.assembly.reunion", "Anne'ye dokunup yanlarına git",
            StoryInteractionKind.HelpSibling, reunionPoint, StoryInteractionGesture.Approach,
            StoryCameraZoneId.EvacuationAssembly, false, 1, 1.2f, 0.65f);

        UnityEventTools.AddStringPersistentListener(
            result.inspectCorridor.OnInteracted,
            family.canAnimator.SetTrigger,
            "StoryInspect");
        UnityEventTools.AddPersistentListener(result.inspectCorridor.OnInteracted, director.InspectCorridor);
        UnityEventTools.AddPersistentListener(result.chooseStairs.OnInteracted, director.ChooseStairs);
        UnityEventTools.AddPersistentListener(result.tryElevator.OnInteracted, director.TryElevator);
        UnityEventTools.AddPersistentListener(result.reachUpperLanding.OnInteracted, director.ReachUpperLanding);
        UnityEventTools.AddPersistentListener(result.holdHandrail.OnInteracted, director.HoldHandrail);
        UnityEventTools.AddPersistentListener(result.reachLowerLanding.OnInteracted, director.ReachLowerLanding);
        UnityEventTools.AddPersistentListener(result.callNeighbor.OnInteracted, director.CallNeighbor);
        UnityEventTools.AddPersistentListener(result.moveCardboard.OnInteracted, director.MoveNeighborCardboard);
        UnityEventTools.AddPersistentListener(result.moveFoam.OnInteracted, director.ClearLightDebris);
        UnityEventTools.AddPersistentListener(result.moveCane.OnInteracted, director.MoveNeighborCane);
        UnityEventTools.AddPersistentListener(result.guideNeighbor.OnInteracted, director.GuideNeighbor);
        UnityEventTools.AddPersistentListener(result.openBuildingExit.OnInteracted, director.OpenBuildingExit);
        UnityEventTools.AddPersistentListener(result.moveAwayFromFacade.OnInteracted, director.MoveAwayFromFacade);
        UnityEventTools.AddStringPersistentListener(
            result.inspectStreetHazard.OnInteracted,
            family.canAnimator.SetTrigger,
            "StoryCall");
        UnityEventTools.AddPersistentListener(result.inspectStreetHazard.OnInteracted, director.InspectStreetHazard);
        UnityEventTools.AddPersistentListener(result.takeSafeSidewalk.OnInteracted, director.TakeSafeSidewalk);
        UnityEventTools.AddPersistentListener(result.tryUnsafeShortcut.OnInteracted, director.TryUnsafeShortcut);
        UnityEventTools.AddPersistentListener(result.readAssemblySign.OnInteracted, director.ReadAssemblySign);
        UnityEventTools.AddPersistentListener(result.handNeighborToWorker.OnInteracted, director.HandNeighborToWorker);
        UnityEventTools.AddPersistentListener(result.checkCan.OnInteracted, director.CheckCan);
        UnityEventTools.AddPersistentListener(result.useRadio.OnInteracted, director.UseRadio);
        UnityEventTools.AddPersistentListener(result.listenWorkerRadio.OnInteracted, director.ListenWorkerRadio);
        UnityEventTools.AddPersistentListener(result.useFirstAid.OnInteracted, director.UseFirstAid);
        UnityEventTools.AddPersistentListener(result.useStationCloth.OnInteracted, director.UseStationCloth);
        UnityEventTools.AddPersistentListener(result.giveWater.OnInteracted, director.GiveWater);
        UnityEventTools.AddPersistentListener(result.useWaterStation.OnInteracted, director.UseWaterStation);
        UnityEventTools.AddPersistentListener(result.giveBlanket.OnInteracted, director.GiveBlanket);
        UnityEventTools.AddPersistentListener(result.moveToWindbreak.OnInteracted, director.MoveToWindbreak);
        UnityEventTools.AddPersistentListener(result.useContactCard.OnInteracted, director.UseContactCard);
        UnityEventTools.AddPersistentListener(result.useRegistrySheet.OnInteracted, director.UseRegistrySheet);
        UnityEventTools.AddPersistentListener(result.useWhistle.OnInteracted, director.UseWhistle);
        UnityEventTools.AddPersistentListener(result.callFamily.OnInteracted, director.CallFamily);
        UnityEventTools.AddPersistentListener(result.reuniteFamily.OnInteracted, director.ReuniteFamily);
        return result;
    }

    private static void ConfigureRebuildDirector(
        StoryEvacuationDirector director,
        StoryGameManager manager,
        StoryPlayerMovement player,
        StoryTouchManager touch,
        StoryCameraController camera,
        StoryChapterBuilderCommon.ChapterUI ui,
        StoryChapterBuilderCommon.Characters family,
        StorySiblingFollower follower,
        RebuildWorld world,
        RebuildInteractions interactions)
    {
        SerializedObject data = new SerializedObject(director);
        StoryChapterBuilderCommon.Set(data, "gameManager", manager);
        StoryChapterBuilderCommon.Set(data, "player", player);
        StoryChapterBuilderCommon.Set(data, "touchManager", touch);
        StoryChapterBuilderCommon.Set(data, "cameraController", camera);
        StoryChapterBuilderCommon.Set(data, "ui", ui.controller);
        data.FindProperty("revisedFlow").boolValue = true;
        data.FindProperty("revisedAftershockMinimumDuration").floatValue = 10f;
        StoryChapterBuilderCommon.Set(data, "deniz", family.deniz.transform);
        StoryChapterBuilderCommon.Set(data, "can", family.can.transform);
        StoryChapterBuilderCommon.Set(data, "neighbor", world.route.neighborAtLanding.transform);
        StoryChapterBuilderCommon.Set(data, "denizAnimator", family.denizAnimator);
        StoryChapterBuilderCommon.Set(data, "canAnimator", family.canAnimator);
        StoryChapterBuilderCommon.Set(data, "neighborAnimator", world.route.neighborAnimator);
        StoryChapterBuilderCommon.Set(data, "canFollower", follower);
        StoryChapterBuilderCommon.Set(data, "flashlightBeam", world.route.flashlightBeam);
        StoryChapterBuilderCommon.Set(data, "emergencyLightRoute", world.route.emergencyLightRoute);
        StoryChapterBuilderCommon.Set(data, "inspectCorridor", interactions.inspectCorridor);
        StoryChapterBuilderCommon.Set(data, "chooseStairs", interactions.chooseStairs);
        StoryChapterBuilderCommon.Set(data, "tryElevator", interactions.tryElevator);
        StoryChapterBuilderCommon.Set(data, "stairDoorAnimation", world.route.stairDoorAnimation);
        StoryChapterBuilderCommon.Set(data, "elevatorNearMissAnimation", world.route.elevatorAnimation);
        StoryChapterBuilderCommon.Set(data, "elevatorAudio", world.route.elevatorAudio);
        StoryChapterBuilderCommon.Set(data, "stairDoorClosed", world.route.stairDoorClosed);
        StoryChapterBuilderCommon.Set(data, "stairDoorOpen", world.route.stairDoorOpen);
        StoryChapterBuilderCommon.Set(data, "reachUpperLanding", interactions.reachUpperLanding);
        StoryChapterBuilderCommon.Set(data, "holdHandrail", interactions.holdHandrail);
        StoryChapterBuilderCommon.Set(data, "reachLowerLanding", interactions.reachLowerLanding);
        StoryChapterBuilderCommon.Set(data, "aftershockImpulse", world.route.aftershockImpulse);
        StoryChapterBuilderCommon.Set(data, "aftershockDust", world.route.aftershockDust);
        StoryChapterBuilderCommon.Set(data, "aftershockAudio", world.route.aftershockAudio);
        StoryChapterBuilderCommon.Set(data, "callNeighbor", interactions.callNeighbor);
        StoryChapterBuilderCommon.Set(data, "moveNeighborCardboard", interactions.moveCardboard);
        StoryChapterBuilderCommon.Set(data, "moveNeighborCane", interactions.moveCane);
        StoryChapterBuilderCommon.Set(data, "clearLightDebris", interactions.moveFoam);
        StoryChapterBuilderCommon.Set(data, "guideNeighbor", interactions.guideNeighbor);
        StoryChapterBuilderCommon.Set(data, "caneBlocked", world.route.caneBlocked);
        StoryChapterBuilderCommon.Set(data, "caneReachable", world.route.caneReachable);
        StoryChapterBuilderCommon.Set(data, "lightDebrisBlocking", world.route.debrisBlocking);
        StoryChapterBuilderCommon.Set(data, "lightDebrisCleared", world.route.debrisCleared);
        StoryChapterBuilderCommon.Set(data, "cardboardBlocking", world.cardboardBlocking);
        StoryChapterBuilderCommon.Set(data, "cardboardCleared", world.cardboardCleared);
        StoryChapterBuilderCommon.Set(data, "neighborAtLanding", world.route.neighborAtLanding);
        StoryChapterBuilderCommon.Set(data, "neighborAtStreet", world.route.neighborAtStreet);
        StoryChapterBuilderCommon.Set(data, "neighborAtAssembly", world.route.neighborAtAssembly);
        StoryChapterBuilderCommon.Set(data, "caneMoveAnimation", world.route.caneAnimation);
        StoryChapterBuilderCommon.Set(data, "debrisMoveAnimation", world.route.debrisAnimation);
        StoryChapterBuilderCommon.Set(data, "cardboardMoveAnimation", world.cardboardAnimation);
        StoryChapterBuilderCommon.Set(data, "neighborRiseAnimation", world.route.neighborRiseAnimation);
        StoryChapterBuilderCommon.Set(data, "openBuildingExit", interactions.openBuildingExit);
        StoryChapterBuilderCommon.Set(data, "moveAwayFromFacade", interactions.moveAwayFromFacade);
        StoryChapterBuilderCommon.Set(data, "outsideStandPoint", world.route.outsideStandPoint);
        StoryChapterBuilderCommon.Set(data, "buildingDoorClosed", world.route.buildingDoorClosed);
        StoryChapterBuilderCommon.Set(data, "buildingDoorOpen", world.route.buildingDoorOpen);
        StoryChapterBuilderCommon.Set(data, "buildingDoorAnimation", world.route.buildingDoorAnimation);
        StoryChapterBuilderCommon.Set(data, "inspectStreetHazard", interactions.inspectStreetHazard);
        StoryChapterBuilderCommon.Set(data, "takeSafeSidewalk", interactions.takeSafeSidewalk);
        StoryChapterBuilderCommon.Set(data, "tryUnsafeShortcut", interactions.tryUnsafeShortcut);
        StoryChapterBuilderCommon.Set(data, "assemblyApproachPoint", world.route.assemblyApproachPoint);
        StoryChapterBuilderCommon.Set(data, "unsafeShortcutAnimation", world.route.unsafeShortcutAnimation);
        StoryChapterBuilderCommon.Set(data, "streetDust", world.route.streetDust);
        StoryChapterBuilderCommon.Set(data, "streetInspectSignAnimation", world.streetWarningSignAnimation);
        StoryChapterBuilderCommon.Set(data, "streetInspectShardAnimation", world.streetWarningShardAnimation);
        StoryChapterBuilderCommon.Set(data, "streetInspectCreak", world.streetWarningCreak);
        StoryChapterBuilderCommon.Set(data, "readAssemblySign", interactions.readAssemblySign);
        StoryChapterBuilderCommon.Set(data, "checkCan", interactions.checkCan);
        StoryChapterBuilderCommon.Set(data, "checkNeighbor", interactions.handNeighborToWorker);
        StoryChapterBuilderCommon.Set(data, "useWhistle", interactions.useWhistle);
        StoryChapterBuilderCommon.Set(data, "callFamily", interactions.callFamily);
        StoryChapterBuilderCommon.Set(data, "whistleWorld", world.route.whistleWorld);
        StoryChapterBuilderCommon.Set(data, "voiceSignalWorld", world.familyCallCard);
        StoryChapterBuilderCommon.Set(data, "handNeighborToWorker", interactions.handNeighborToWorker);
        StoryChapterBuilderCommon.Set(data, "useRadio", interactions.useRadio);
        StoryChapterBuilderCommon.Set(data, "listenWorkerRadio", interactions.listenWorkerRadio);
        StoryChapterBuilderCommon.Set(data, "useFirstAid", interactions.useFirstAid);
        StoryChapterBuilderCommon.Set(data, "useStationCloth", interactions.useStationCloth);
        StoryChapterBuilderCommon.Set(data, "giveWater", interactions.giveWater);
        StoryChapterBuilderCommon.Set(data, "useWaterStation", interactions.useWaterStation);
        StoryChapterBuilderCommon.Set(data, "giveBlanket", interactions.giveBlanket);
        StoryChapterBuilderCommon.Set(data, "moveToWindbreak", interactions.moveToWindbreak);
        StoryChapterBuilderCommon.Set(data, "useContactCard", interactions.useContactCard);
        StoryChapterBuilderCommon.Set(data, "useRegistrySheet", interactions.useRegistrySheet);
        StoryChapterBuilderCommon.Set(data, "reuniteFamily", interactions.reuniteFamily);
        StoryChapterBuilderCommon.Set(data, "workerAtAssembly", world.worker);
        StoryChapterBuilderCommon.Set(data, "radioPreparedWorld", world.radioPrepared);
        StoryChapterBuilderCommon.Set(data, "radioFallbackWorld", world.radioFallback);
        StoryChapterBuilderCommon.Set(data, "radioTuneAnimation", world.radioTuneAnimation);
        StoryChapterBuilderCommon.Set(data, "radioTunedIndicator", world.radioTunedIndicator);
        StoryChapterBuilderCommon.Set(data, "workerRadioIndicator", world.workerRadioIndicator);
        StoryChapterBuilderCommon.Set(data, "radioPreparedBroadcast", world.radioPreparedBroadcast);
        StoryChapterBuilderCommon.Set(data, "workerRadioBroadcast", world.workerRadioBroadcast);
        StoryChapterBuilderCommon.Set(data, "firstAidPreparedWorld", world.firstAidPrepared);
        StoryChapterBuilderCommon.Set(data, "firstAidFallbackWorld", world.firstAidFallback);
        StoryChapterBuilderCommon.Set(data, "waterPreparedWorld", world.waterPrepared);
        StoryChapterBuilderCommon.Set(data, "waterFallbackWorld", world.waterFallback);
        StoryChapterBuilderCommon.Set(data, "blanketPreparedWorld", world.blanketPrepared);
        StoryChapterBuilderCommon.Set(data, "blanketFallbackWorld", world.blanketFallback);
        StoryChapterBuilderCommon.Set(data, "contactPreparedWorld", world.contactPrepared);
        StoryChapterBuilderCommon.Set(data, "contactFallbackWorld", world.contactFallback);
        StoryChapterBuilderCommon.Set(data, "comfortToyAtAssembly", world.comfortToyAtAssembly);
        StoryChapterBuilderCommon.Set(data, "motherAtAssembly", world.mother);
        StoryChapterBuilderCommon.Set(data, "fatherAtAssembly", world.father);
        StoryChapterBuilderCommon.Set(data, "motherApproachAnimation", world.motherApproach);
        StoryChapterBuilderCommon.Set(data, "fatherApproachAnimation", world.fatherApproach);
        StoryChapterBuilderCommon.Set(data, "familyHeadcountPendingWorld", world.familyHeadcountPending);
        StoryChapterBuilderCommon.Set(data, "familyHeadcountCompleteWorld", world.familyHeadcountComplete);
        StoryChapterBuilderCommon.Set(data, "completionPanel", ui.completionPanel);
        StoryChapterBuilderCommon.Set(data, "completionDetail", ui.completionDetail);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StoryInteractable Add04RInteraction(
        GameObject source,
        string id,
        string prompt,
        StoryInteractionKind kind,
        Transform interactionPoint,
        StoryInteractionGesture gesture,
        StoryCameraZoneId zone,
        bool fromAnywhere = false,
        int gestureCount = 1,
        float seconds = 1.2f,
        float range = 1.55f)
    {
        StoryInteractable interactable = StoryChapterBuilderCommon.AddInteractable(
            source,
            id,
            prompt,
            kind,
            interactionPoint,
            gesture,
            zone,
            fromAnywhere,
            gestureCount,
            seconds,
            range);
        SerializedObject data = new SerializedObject(interactable);
        data.FindProperty("returnCameraAfterCompletion").boolValue = false;
        data.FindProperty("focusLingerSeconds").floatValue = 0.35f;
        data.ApplyModifiedPropertiesWithoutUndo();
        return interactable;
    }

    private static void Configure04RReturnCamera(
        StoryInteractable interactable,
        StoryCameraZoneId returnZone,
        float lingerSeconds)
    {
        SerializedObject data = new SerializedObject(interactable);
        data.FindProperty("returnCameraAfterCompletion").boolValue = true;
        data.FindProperty("returnCameraZone").intValue = (int)returnZone;
        data.FindProperty("focusLingerSeconds").floatValue = lingerSeconds;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StoryInteractable Add04RDrag(
        GameObject source,
        GameObject visibleTarget,
        string id,
        string prompt,
        StoryInteractionKind kind,
        StoryCameraZoneId zone,
        Transform dropParent,
        Vector3 dropSize)
    {
        StoryInteractable interactable = Add04RInteraction(
            source,
            id,
            prompt,
            kind,
            source.transform,
            StoryInteractionGesture.DragToTarget,
            zone,
            false,
            1,
            1.25f,
            3.2f);
        BagDropZone dropZone = Create04RDropZone(
            "Drop_" + id.Replace('.', '_'),
            dropParent,
            Get04RBounds(visibleTarget).center,
            dropSize);
        Configure04RDrag(interactable, dropZone);
        return interactable;
    }

    private static BagDropZone Create04RDropZone(
        string name,
        Transform parent,
        Vector3 worldPosition,
        Vector3 size)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = worldPosition;
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return zone.AddComponent<BagDropZone>();
    }

    private static void Configure04RDrag(StoryInteractable interactable, BagDropZone dropZone)
    {
        BoxCollider collider = interactable.GetComponent<BoxCollider>() ??
                               interactable.gameObject.AddComponent<BoxCollider>();
        Fit04RCollider(collider, interactable.gameObject);
        collider.isTrigger = false;
        DraggableItem draggable = interactable.GetComponent<DraggableItem>() ??
                                  interactable.gameObject.AddComponent<DraggableItem>();
        SerializedObject data = new SerializedObject(draggable);
        data.FindProperty("isCorrectItem").boolValue = true;
        data.FindProperty("displayName").stringValue = interactable.Prompt;
        data.FindProperty("inputEnabled").boolValue = false;
        data.FindProperty("notifyGameManager").boolValue = false;
        data.FindProperty("tapToBagEnabled").boolValue = false;
        data.FindProperty("dropZoneOverride").objectReferenceValue = dropZone;
        data.FindProperty("dragLift").floatValue = 0.12f;
        data.FindProperty("dragScale").floatValue = 1.04f;
        data.FindProperty("returnDuration").floatValue = 0.34f;
        data.FindProperty("returnArcHeight").floatValue = 0.12f;
        data.ApplyModifiedPropertiesWithoutUndo();
        StoryChapterBuilderCommon.SetGestureTarget(interactable, dropZone.transform);
    }

    private static Transform Point(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 lookAt)
    {
        return StoryChapterBuilderCommon.CreatePoint(name, parent, position, lookAt);
    }

    private static GameObject InstantiateItem(
        string name,
        string prefabName,
        Transform parent,
        Vector3 position,
        Vector3 size,
        Vector3 euler)
    {
        return StoryChapterBuilderCommon.InstantiateAsset(
            ItemPrefabRoot + "/" + prefabName,
            name,
            parent,
            position,
            size,
            euler,
            true,
            true);
    }

    private static GameObject CreateClipboard(
        string name,
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        StoryChapterBuilderCommon.CreatePrimitive(
            "Board",
            PrimitiveType.Cube,
            position,
            new Vector3(0.55f, 0.045f, 0.72f),
            materials.wood,
            root.transform,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "Paper",
            PrimitiveType.Cube,
            position + Vector3.up * 0.05f,
            new Vector3(0.45f, 0.02f, 0.6f),
            materials.cream,
            root.transform,
            false);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(0.6f, 0.15f, 0.8f);
        return root;
    }

    private static void BuildFamilyHeadcountMarks(
        GameObject clipboard,
        StoryChapterBuilderCommon.Materials materials,
        out GameObject pending,
        out GameObject complete)
    {
        pending = new GameObject("FamilyHeadcountPending_2of4");
        pending.transform.SetParent(clipboard.transform);
        complete = new GameObject("FamilyHeadcountComplete_4of4");
        complete.transform.SetParent(clipboard.transform);

        Vector3 origin = clipboard.transform.position + new Vector3(-0.18f, 0.075f, 0.08f);
        for (int index = 0; index < 4; index++)
        {
            Vector3 position = origin + Vector3.right * (index * 0.12f);
            StoryChapterBuilderCommon.CreatePrimitive(
                "PendingFamilyMark_" + (index + 1),
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.075f, 0.012f, 0.075f),
                index < 2 ? materials.teal : materials.cream,
                pending.transform,
                false);
            StoryChapterBuilderCommon.CreatePrimitive(
                "CompleteFamilyMark_" + (index + 1),
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.075f, 0.012f, 0.075f),
                materials.teal,
                complete.transform,
                false);
        }

        complete.SetActive(false);
    }

    private static GameObject CreateMegaphone(
        string name,
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        StoryChapterBuilderCommon.CreatePrimitive(
            "Horn",
            PrimitiveType.Cylinder,
            position,
            new Vector3(0.18f, 0.35f, 0.18f),
            materials.coral,
            root.transform,
            false,
            Quaternion.Euler(90f, 0f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive(
            "Handle",
            PrimitiveType.Cube,
            position + new Vector3(0f, -0.28f, 0f),
            new Vector3(0.12f, 0.35f, 0.12f),
            materials.dark,
            root.transform,
            false);
        root.AddComponent<BoxCollider>().size = new Vector3(0.6f, 0.8f, 0.6f);
        return root;
    }

    private static void BuildPhysicalRadioPayoff(
        StoryChapterBuilderCommon.Materials materials,
        RebuildWorld world)
    {
        Bounds bounds = Get04RBounds(world.radioPrepared);
        Vector3 front = bounds.center + new Vector3(0f, 0f, -bounds.extents.z - 0.025f);
        GameObject dialMount = new GameObject("RadioDialMount");
        dialMount.transform.position = front + new Vector3(bounds.extents.x * 0.48f, 0f, 0f);
        dialMount.transform.rotation = Quaternion.Euler(90f, 0f, -38f);
        dialMount.transform.SetParent(world.radioPrepared.transform, true);
        world.radioTuningDial = StoryChapterBuilderCommon.CreatePrimitive(
            "RadioPhysicalTuningDial",
            PrimitiveType.Cylinder,
            dialMount.transform.position,
            new Vector3(0.09f, 0.035f, 0.09f),
            materials.amber,
            world.radioPrepared.transform.parent,
            true);
        world.radioTuningDial.transform.rotation = dialMount.transform.rotation;
        world.radioTuningDial.transform.SetParent(dialMount.transform, true);
        world.radioTuningDial.transform.localPosition = Vector3.zero;
        world.radioTuningDial.transform.localRotation = Quaternion.identity;
        GameObject dialPointer = StoryChapterBuilderCommon.CreatePrimitive(
            "RadioDialPointer",
            PrimitiveType.Cube,
            Vector3.zero,
            Vector3.one,
            materials.dark,
            world.radioTuningDial.transform,
            false);
        dialPointer.transform.localPosition = new Vector3(0f, -1.08f, 0.28f);
        dialPointer.transform.localRotation = Quaternion.identity;
        dialPointer.transform.localScale = new Vector3(0.18f, 0.18f, 0.5f);
        world.radioTuneAnimation = StoryChapterBuilderCommon.CreateRotationAnimation(
            world.radioTuningDial,
            "Story04Rebuild_RadioTune",
            Vector3.zero,
            new Vector3(0f, 72f, 0f),
            0.72f);

        world.radioTunedIndicator = StoryChapterBuilderCommon.CreatePrimitive(
            "RadioTunedIndicator",
            PrimitiveType.Sphere,
            front + new Vector3(-bounds.extents.x * 0.42f, bounds.extents.y * 0.22f, 0f),
            new Vector3(0.045f, 0.045f, 0.024f),
            materials.teal,
            world.radioPrepared.transform.parent,
            false);
        world.radioTunedIndicator.transform.SetParent(world.radioPrepared.transform, true);
        world.radioTunedIndicator.SetActive(false);

        AudioClip broadcast = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_loop_machine_01.ogg");
        world.radioPreparedBroadcast = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            "RadioPreparedOfficialBroadcast",
            world.radioPrepared.transform.parent,
            bounds.center,
            broadcast,
            0.055f,
            0.92f,
            0.7f,
            7f);
        world.radioPreparedBroadcast.transform.SetParent(world.radioPrepared.transform, true);
        world.radioPreparedBroadcast.loop = true;
        world.radioPreparedBroadcast.playOnAwake = false;
    }

    private static void BuildWorkerRadioPayoff(
        StoryChapterBuilderCommon.Materials materials,
        RebuildWorld world)
    {
        Bounds bounds = Get04RBounds(world.radioFallback);
        world.workerRadioIndicator = StoryChapterBuilderCommon.CreatePrimitive(
            "WorkerRadioLiveIndicator",
            PrimitiveType.Sphere,
            bounds.center + new Vector3(
                -bounds.extents.x * 0.38f,
                bounds.extents.y * 0.6f,
                -bounds.extents.z - 0.032f),
            new Vector3(0.05f, 0.05f, 0.05f),
            materials.teal,
            world.radioFallback.transform.parent,
            false);
        world.workerRadioIndicator.transform.SetParent(world.radioFallback.transform, true);
        world.workerRadioIndicator.SetActive(false);

        AudioClip broadcast = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_loop_machine_01.ogg");
        world.workerRadioBroadcast = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            "WorkerRadioOfficialBroadcast",
            world.radioFallback.transform.parent,
            bounds.center,
            broadcast,
            0.055f,
            0.92f,
            0.7f,
            7f);
        world.workerRadioBroadcast.transform.SetParent(world.radioFallback.transform, true);
        world.workerRadioBroadcast.loop = true;
        world.workerRadioBroadcast.playOnAwake = false;
    }

    private static GameObject CreatePencil(
        string name,
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        return StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Cylinder,
            position,
            new Vector3(0.035f, 0.32f, 0.035f),
            materials.amber,
            parent,
            true,
            Quaternion.Euler(90f, 0f, 28f));
    }

    private static GameObject CreateFamilyCallCard(
        string name,
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        GameObject card = StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Cube,
            position,
            new Vector3(0.42f, 0.06f, 0.52f),
            materials.amber,
            parent,
            true,
            Quaternion.Euler(0f, 12f, 0f));
        StoryChapterBuilderCommon.CreateWorldLabel(
            "FamilySignalCardLabel",
            "AİLE",
            position + new Vector3(0f, 0.05f, -0.27f),
            new Vector3(90f, 0f, 0f),
            1.3f,
            StoryChapterBuilderCommon.Navy,
            card.transform,
            new Vector2(0.7f, 0.25f));
        return card;
    }

    private static void AddWorkerVest(
        Transform worker,
        StoryChapterBuilderCommon.Materials materials)
    {
        StoryChapterBuilderCommon.CreatePrimitive(
            "WorkerSafetyVest",
            PrimitiveType.Cube,
            worker.position + new Vector3(0f, 1.08f, 0f),
            new Vector3(0.48f, 0.5f, 0.22f),
            materials.amber,
            worker,
            false);
    }

    private static void AddFatherAccessory(
        Transform father,
        StoryChapterBuilderCommon.Materials materials)
    {
        StoryChapterBuilderCommon.CreatePrimitive(
            "FatherBlueCap",
            PrimitiveType.Cylinder,
            father.position + new Vector3(0f, 1.72f, 0f),
            new Vector3(0.3f, 0.08f, 0.3f),
            materials.navy,
            father,
            false);
    }

    private static void RemoveInstructionalGroundLanguage(
        Transform environment,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach (string labelName in new[] { "SafeRouteLabel", "VoiceSignalLabel", "UpperCorridorBackWall" })
        {
            Transform label = environment.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.name == labelName);
            if (label != null)
                Object.DestroyImmediate(label.gameObject);
        }

        foreach ((string objectName, Material material) in new[]
                 {
                     ("SafeOpenSidewalk", materials.concrete),
                     ("UnsafeGlassShortcut", materials.concrete)
                 })
        {
            Transform surface = environment.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.name == objectName);
            Renderer renderer = surface != null ? surface.GetComponent<Renderer>() : null;
            if (renderer != null)
                renderer.sharedMaterial = material;
        }
    }

    private static Bounds Get04RBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.one * 0.5f);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void Fit04RCollider(BoxCollider collider, GameObject target)
    {
        Bounds bounds = Get04RBounds(target);
        collider.center = target.transform.InverseTransformPoint(bounds.center);
        Vector3 lossy = target.transform.lossyScale;
        collider.size = new Vector3(
            bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(lossy.x)),
            bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(lossy.y)),
            bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(lossy.z)));
    }

    public static void ValidateRebuildPreview(bool showDialog)
    {
        if (!File.Exists(RebuildPreviewScenePath))
            throw new FileNotFoundException("Story 04 rebuild önizleme sahnesi bulunamadı.", RebuildPreviewScenePath);

        Scene scene = SceneManager.GetSceneByPath(RebuildPreviewScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(RebuildPreviewScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == "STORY_04_REBUILD_PREVIEW");
            Require04(root != null, "STORY_04_REBUILD_PREVIEW kökü");
            Require04(
                root.GetComponentsInChildren<StoryEvacuationDirector>(true).Length == 1,
                "tek StoryEvacuationDirector");
            StoryEvacuationDirector director =
                root.GetComponentInChildren<StoryEvacuationDirector>(true);
            Require04(director.RevisedFlow, "revisedFlow açık olması");
            Require04(
                root.GetComponentsInChildren<StoryTouchManager>(true).Length == 1,
                "tek StoryTouchManager");
            Require04(
                root.GetComponentsInChildren<StoryPlayerMovement>(true).Length == 1,
                "tek StoryPlayerMovement");
            Require04(
                root.GetComponentsInChildren<StoryActionButton>(true).Length == 0,
                "merkez görev butonu bulunmaması");

            StoryInteractable[] interactions = root.GetComponentsInChildren<StoryInteractable>(true);
            Require04(interactions.Length == 32,
                $"32 doğrudan dünya etkileşimi (gerçek: {interactions.Length})");
            StoryInteractable[] drags = interactions
                .Where(candidate => candidate.InteractionGesture == StoryInteractionGesture.DragToTarget)
                .ToArray();
            Require04(drags.Length == 9, "dokuz fiziksel hedefli sürükleme");
            foreach (StoryInteractable drag in drags)
            {
                DraggableItem draggable = drag.GetComponent<DraggableItem>();
                Require04(draggable != null, drag.InteractionId + " DraggableItem");
                Require04(draggable.DropZoneOverride != null, drag.InteractionId + " hedef alanı");
                Require04(
                    drag.GestureTarget == draggable.DropZoneOverride.transform,
                    drag.InteractionId + " gerçek hedef bağlantısı");
            }

            string[] required =
            {
                "evac.r04.neighbor.cardboard",
                "evac.r04.neighbor.foam",
                "evac.r04.neighbor.cane",
                "evac.r04.exit.facade_clear",
                "evac.r04.assembly.handoff",
                "evac.r04.assembly.firstaid",
                "evac.r04.assembly.water",
                "evac.r04.assembly.blanket",
                 "evac.r04.assembly.contact",
                 "evac.r04.assembly.reunion"
            };
            foreach (string id in required)
                Require04(interactions.Any(candidate => candidate.InteractionId == id), id);
            StoryInteractable checkCan = interactions.Single(candidate =>
                candidate.InteractionId == "evac.r04.assembly.can");
            Require04(
                checkCan.InteractionGesture == StoryInteractionGesture.WorldHold,
                "Can kontrolünün doğrudan dünya üzerinde basılı tutma olması");
            Transform comfortToy = Find04(root, "CanComfortToy_Assembly");
            Require04(comfortToy != null, "Can teselli oyuncağı");
            Require04(!comfortToy.gameObject.activeSelf, "Can teselli oyuncağı başlangıçta gizli");
            Transform can = Find04(root, "Can_8");
            Transform canRightHand = can != null
                ? StoryChapterBuilderCommon.FindHumanoidBone(can.gameObject, HumanBodyBones.RightHand)
                : null;
            Require04(comfortToy.parent != null && comfortToy.parent == canRightHand,
                "Can teselli oyuncağının sağ ele bağlı olması");
            Require04(
                new SerializedObject(director).FindProperty("comfortToyAtAssembly").objectReferenceValue ==
                comfortToy.gameObject,
                "Can teselli oyuncağının yönetmene bağlı olması");

            CinemachineCamera[] cameras = root.GetComponentsInChildren<CinemachineCamera>(true);
            Require04(cameras.Length == 14, "on dört bestelenmiş Cinemachine kamera");
            foreach (CinemachineCamera camera in cameras)
                Require04(
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f,
                    camera.name + " lens aralığı");

            Require04(
                root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true).Length == 1,
                "tek NavMeshSurface");
            Require04(
                root.GetComponentInChildren<Unity.AI.Navigation.NavMeshSurface>(true).navMeshData != null,
                "baked NavMesh");
            Require04(Find04(root, "AssemblyWorker") != null, "toplanma görevlisi");
            Require04(Find04(root, "Anne_Assembly_Reunion") != null, "Anne buluşma karakteri");
            Require04(Find04(root, "Baba_Assembly_Reunion") != null, "Baba buluşma karakteri");
            Require04(!Find04(root, "Anne_Assembly_Reunion").gameObject.activeSelf, "Anne başlangıçta gizli");
            Require04(!Find04(root, "Baba_Assembly_Reunion").gameObject.activeSelf, "Baba başlangıçta gizli");
            Require04(Find04(root, "FamilyHeadcountPending_2of4") != null, "iki kişilik bekleyen aile sayımı");
            Require04(Find04(root, "FamilyHeadcountComplete_4of4") != null, "dört kişilik tamamlanan aile sayımı");
            Require04(Find04(root, "FamilyHeadcountPending_2of4").gameObject.activeSelf, "bekleyen aile sayımı başlangıçta görünür");
            Require04(!Find04(root, "FamilyHeadcountComplete_4of4").gameObject.activeSelf, "tamamlanan aile sayımı başlangıçta gizli");
            Require04(Find04(root, "RadioPhysicalTuningDial") != null, "fiziksel radyo ayar düğmesi");
            Require04(Find04(root, "RadioDialMount") != null, "radyo düğmesi sabit montajı");
            Require04(Find04(root, "RadioDialPointer") != null, "radyo düğmesi yön çizgisi");
            Require04(Find04(root, "RadioTunedIndicator") != null, "hazırlanmış radyo yayın göstergesi");
            Require04(Find04(root, "WorkerRadioLiveIndicator") != null, "fallback hoparlör yayın göstergesi");
            Require04(Find04(root, "SafeRouteLabel") == null, "zeminde AÇIK ROTA talimatı bulunmaması");
            Require04(
                root.GetComponentsInChildren<TMP_Text>(true)
                    .All(text => !string.Equals(text.text?.Trim(), "AÇIK ROTA", StringComparison.OrdinalIgnoreCase)),
                "AÇIK ROTA yazısı bulunmaması");
            Require04(
                EditorBuildSettings.scenes.Any(candidate =>
                    candidate.enabled && candidate.path == RebuildPreviewScenePath),
                "Story 04 yayın sahnesinin Build Settings'te olması");

            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_04 rebuild doğrulaması başarılı.", "Tamam");
        }
        finally
        {
            if (opened && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Transform Find04(GameObject root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == name);
    }

    private static void Require04(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("Story_04 rebuild doğrulaması başarısız: " + label);
    }
}
