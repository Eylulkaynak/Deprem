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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class StoryEvacuationSceneBuilder
{
    public const string RebuildPreviewScenePath = "Assets/Scenes/Story_04_RebuildPreview.unity";
    private const string ItemPrefabRoot = "Assets/Bolum1Prefab";
    private const string AdultPrefabPath = "Assets/Story/Prefabs/Preparation/Anne_Ayse.prefab";
    private const string ResponderPrefabRoot = "Assets/Story/Characters/MeshyResponders/Prefabs";
    private const string FirefighterPrefabPath = ResponderPrefabRoot + "/Firefighter.prefab";
    private const string RescueWorkerPrefabPath = ResponderPrefabRoot + "/RescueWorker.prefab";
    private const string PolicePrefabPath = ResponderPrefabRoot + "/Police.prefab";
    private const string CuratedTownEnvironmentRoot = "Assets/Story/Environment/SyntyTown";
    private const string KenneyNatureModelRoot =
        "Assets/Story/Environment/ThirdParty/KenneyNature/Models";
    private const string ParkGrassMaterialPath =
        "Assets/Story/Generated/Materials/Story04_ParkGrass.mat";
    private const string KenneyBedrollPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx";
    private const string KenneySurvivalMaterialPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Materials/KenneySurvival_Atlas.mat";
    private const string AmbientMeshFolder = "Assets/Story/Generated/Meshes";
    private const string CityPropRoot =
        "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";
    private const string PropaneTankPath =
        "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/PropaneTank.fbx";

    private sealed class RebuildWorld
    {
        internal EvacuationWorld route;
        internal GameObject cardboardBlocking;
        internal GameObject cardboardCleared;
        internal Animation cardboardAnimation;
        internal GameObject neighborSupportBracelet;
        internal GameObject facadeSafePoint;
        internal GameObject worker;
        internal GameObject firefighter;
        internal GameObject police;
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
        internal GameObject gasLeakHazard;
        internal GameObject gasLeakInspectionSource;
        internal GameObject gasLeakUnsafeSource;
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
        internal Transform[] navigationAnchors;
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
        const string failureLogPath = "Temp/Story04RebuildLastFailure.txt";
        if (File.Exists(failureLogPath))
            File.Delete(failureLogPath);
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
            BuildUpperCorridorInterior(route, materials);
            RestyleRebuildEmergencyLights(route, materials);
            RestyleRebuildStairwell(route, materials);
            BuildRebuildArchitecturalFinish(route, materials);
            RemoveInstructionalGroundLanguage(route.environment.transform, materials);

            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                false,
                new Vector3(-0.1f, 4.03f, -4.4f),
                new Vector3(0.65f, 4.03f, -4.75f),
                Vector3.zero);

            BindFlashlightToHand(route, family.deniz);
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
            // Tahliye rotası iki ayrı ~2 m kot inişi içerir. Varsayılan 1.8 m
            // yarıçap alt kotu bulamıyor; 3.25 m ise aynı X/Z isteğinde daha uzak
            // ara rampayı seçip karakteri hedefe 4.7 m kala durduruyordu. 2.25 m
            // yalnız komşu katın gerçek baked iniş noktasını kapsar.
            SerializedObject evacuationMovement = new SerializedObject(movement);
            evacuationMovement.FindProperty("directSampleRadius").floatValue = 2.25f;
            evacuationMovement.ApplyModifiedPropertiesWithoutUndo();
            StorySiblingFollower follower = StoryChapterBuilderCommon.ConfigureSibling(
                family.can,
                family.deniz.transform);

            StoryChapterBuilderCommon.CameraSpec[] cameraSpecs =
            {
                new(StoryCameraZoneId.EvacuationCorridor, "CM04R_CorridorPair",
                    new Vector3(0.5f, 6.4f, -10.35f), new Vector3(1.05f, 4.65f, -2.4f), 47f, true),
                new(StoryCameraZoneId.EvacuationElevator, "CM04R_ElevatorEvidence",
                    new Vector3(-1.35f, 6.55f, -5.45f), new Vector3(2.1f, 5.05f, -1.6f), 52f, true),
                new(StoryCameraZoneId.EvacuationStairDoor, "CM04R_StairDoorReveal",
                    new Vector3(0.5f, 6.4f, -10.35f), new Vector3(0f, 4.9f, -0.1f), 44f, true),
                new(StoryCameraZoneId.EvacuationStairsTop, "CM04R_StairDescent",
                    // Merdiven kovası artık gerçek bir dış kabuk ve tavana sahip.
                    // Kamera da açık duvar hilesi kullanmak yerine kabuğun içinde,
                    // korkuluğun üstünden iki sahanlığı birlikte okuyacak konumda.
                    new Vector3(1.82f, 6.32f, 0.55f), new Vector3(0f, 2.35f, 6.5f), 52f, true),
                new(StoryCameraZoneId.EvacuationLanding, "CM04R_AftershockTwoShot",
                    // Sahanlığın içinde kalan bu açı korkuluğun üzerinden dış
                    // cepheye bakmaz; ayak basılan alanı ve tutamağı birlikte okutur.
                    new Vector3(0.82f, 4.8f, 2.35f), new Vector3(0.82f, 2.45f, 8.1f), 50f, true),
                new(StoryCameraZoneId.EvacuationLowerLanding, "CM04R_LowerLandingReveal",
                    // Stay inside the lower stair flight.  x=1.45 sat beyond the
                    // parapet and rendered the exterior facade instead of the landing.
                    new Vector3(0.25f, 2.55f, 12.75f), new Vector3(-0.05f, 0.72f, 17.5f), 48f, true),
                new(StoryCameraZoneId.EvacuationNeighbor, "CM04R_NerminHelp",
                    // Nermin solda, üç hafif hedef sağ duvar şeridinde. Dar
                    // 20:9 portrede Nermin'in destek eli soldan, baston kaynağı
                    // sağdan kırpılıyordu. Kamera sahanlığın içinde kalırken 60°
                    // lens iki uçtaki gerçek dokunma/drop hacimlerini aynı güvenli
                    // kadrajda tutar. Hafif sol bakış payı Nermin'in gövde ve
                    // destek-eli collider köşelerini de 20:9 ekranın içine alır.
                    new Vector3(0f, 2.0f, 11.2f), new Vector3(0.12f, 0.68f, 18.15f), 60f, true),
                new(StoryCameraZoneId.EvacuationBuildingDoor, "CM04R_ExitDoor",
                    // The former x=2 camera sat against the landing wall.  As the
                    // door swung open that wall filled most of the portrait frame.
                    // Keep this fixed shot near the corridor centre with the whole
                    // threshold directly ahead.
                    new Vector3(0.65f, 2.4f, 17.0f), new Vector3(0f, 1.05f, 21.2f), 42f, true),
                new(StoryCameraZoneId.EvacuationBuildingFront, "CM04R_FacadeClear",
                    // Eski yakın açı yalnız kapının altını ve boş kaldırımı gösteriyordu.
                    // Geri çekilen ana plan güvenli parke noktasını, girişi, pencereleri
                    // ve tamamlanmış çatı çizgisini aynı portre kadrajına alır.
                    new Vector3(0f, 5.6f, 35f), new Vector3(0f, 3.1f, 22.9f), 60f, true),
                new(StoryCameraZoneId.EvacuationStreetInspect, "CM04R_StreetHazardRead",
                    new Vector3(2.0f, 2.85f, 31.25f), new Vector3(-2.12f, 1.0f, 28.25f), 42f, true),
                new(StoryCameraZoneId.EvacuationStreet, "CM04R_StreetJourney",
                    // Bu rota planı stage başlangıcından bağımsız, authored bir
                    // sokak ana planıdır. Takip composer'ı sahne kurulduğu anda
                    // Deniz hâlâ üst koridordayken kamerayı çatının içine çekiyor
                    // ve QA/render önizlemesini bütünüyle kapatıyordu. Sabit uzak
                    // açı güvenli kaldırım, toplanma girişi ve karakter yürüyüşünü
                    // aynı kadrajda tutar; builder clearance denetimine de girer.
                    new Vector3(-0.5f, 4.2f, 58f), new Vector3(-0.5f, 0.9f, 30.7f), 50f, true),
                new(StoryCameraZoneId.EvacuationAssembly, "CM04R_AssemblyReunion",
                    new Vector3(2.6f, 3.0f, 41.2f), new Vector3(-0.7f, 1.2f, 47.8f), 50f, true),
                new(StoryCameraZoneId.EvacuationAssemblyRadio, "CM04R_AssemblyCare",
                    // Kayıt masasından Nermin'e kadar tüm bakım istasyonunu tek
                    // güvenli kadrajda tutan uzak ana plan.
                    new Vector3(2.0f, 5.75f, 31.5f), new Vector3(-2.1f, 1.1f, 45.8f), 50f, true),
                new(StoryCameraZoneId.EvacuationHazard, "CM04R_NearMiss",
                    new Vector3(2.2f, 2.4f, 25.6f), new Vector3(-1.25f, 0.3f, 28.2f), 44f, true)
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
            StoryChapterBuilderCommon.ConfigureDialogueActors(
                ui.controller,
                new StoryChapterBuilderCommon.DialogueActorSpec(family.deniz, "Deniz"),
                new StoryChapterBuilderCommon.DialogueActorSpec(family.can, "Can"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.route.neighborAtLanding, "Nermin", "Nermin Teyze"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.route.neighborAtStreet, "Nermin", "Nermin Teyze"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.route.neighborAtAssembly, "Nermin", "Nermin Teyze"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.worker, "Görevli", "Gorevli"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.mother, "Anne", "Ayşe", "Ayse"),
                new StoryChapterBuilderCommon.DialogueActorSpec(world.father, "Baba"));

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
            AudioSource cityAmbience = StoryChapterBuilderCommon.CreateLicensedAmbience(
                "DistantCityAndAssemblyAmbience",
                root.transform,
                "sfx100v2_loop_highway.ogg",
                0.09f);
            ConfigureExteriorAmbience(cityAmbience, new Vector3(4f, 1.4f, 38f), 26f);
            AudioSource outdoorAir = StoryChapterBuilderCommon.CreateLicensedAmbience(
                "OutdoorAirLayer",
                root.transform,
                "sfx100v2_loop_ambient_04.ogg",
                0.035f);
            ConfigureExteriorAmbience(outdoorAir, new Vector3(0f, 1.3f, 29f), 18f);
            StoryChapterBuilderCommon.AttachInteractionAudioLayer(
                root.transform,
                "Story04_ObjectInteractionAudio");
            StoryChapterBuilderCommon.DisableShadows(root.transform);

            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            StoryChapterBuilderCommon.BuildNavigation(route.environment);
            SnapAndValidateRebuildRoutes(family, world, interactions);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            StoryKktcSceneArt.ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene, RebuildPreviewScenePath);
            ValidateRebuildPreview(false);
            if (File.Exists(failureLogPath))
                File.Delete(failureLogPath);
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
            Directory.CreateDirectory("Temp");
            File.WriteAllText(failureLogPath, exception.ToString());
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

        ReplaceNeighborLandingProps(route, routeRoot, materials);
        FaceCharacterTowards(route.neighborAtLanding, new Vector3(0.1f, 0.02f, 17.9f));
        FaceCharacterTowards(route.neighborAtStreet, new Vector3(0.3f, 0.02f, 24.1f));
        FaceCharacterTowards(route.neighborAtAssembly, new Vector3(-2.3f, 0.02f, 41.8f));

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

        BuildGasLeakChoice(world, routeRoot, materials);

        // The authored indoor floor ends before the outdoor slab starts. The
        // closed door's dynamic obstacle hid that disconnected seam in editor
        // validation, so runtime destination resolution fell back to the inside
        // of the doorway. This collider is baked as walkable threshold geometry;
        // its renderer stays off and the closed-door obstacle still blocks it
        // until the story actually opens the door.
        GameObject buildingExitNavBridge = StoryChapterBuilderCommon.CreatePrimitive(
            "BuildingExitNavBridge",
            PrimitiveType.Cube,
            new Vector3(0f, -0.045f, 22.1f),
            new Vector3(1.7f, 0.12f, 4.6f),
            materials.concrete,
            routeRoot,
            true);
        buildingExitNavBridge.GetComponent<Renderer>().enabled = false;

        // NavMesh baking erodes both sides of the narrow imported frame by the
        // agent radius, leaving two valid but disconnected islands even with the
        // threshold slab present. A static scene-authored link spans only that
        // doorway seam. Exterior objectives remain unavailable until the opaque
        // door has been opened, so the link cannot skip the story gate.
        GameObject buildingExitLinkObject = new GameObject("BuildingExitThresholdNavLink");
        buildingExitLinkObject.transform.SetParent(routeRoot, false);
        NavMeshLink buildingExitLink = buildingExitLinkObject.AddComponent<NavMeshLink>();
        buildingExitLink.agentTypeID = 0;
        // Span only the eroded door-frame seam.  The previous 2.6 m link carried
        // both children through several full walk cycles as one off-mesh step,
        // which read as sliding/broken feet immediately after leaving the house.
        buildingExitLink.startPoint = new Vector3(0f, 0.09f, 20.5f);
        buildingExitLink.endPoint = new Vector3(0f, 0.09f, 21.6f);
        buildingExitLink.width = 1.15f;
        buildingExitLink.bidirectional = true;
        buildingExitLink.autoUpdate = false;

        world.cardboardBlocking = InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "NeighborCardboard_Blocking_Drag",
            routeRoot,
            // Do not stack every interaction prop into one unreadable heap.
            // The box sits farthest along the wall, with a clear silhouette.
            new Vector3(1.42f, 0.02f, 18.75f),
            new Vector3(0.5f, 0.4f, 0.46f),
            new Vector3(0f, -8f, 0f),
            true);
        // Cleared props form one believable right-wall staging cluster.  They do
        // not migrate farther up the centre of the exit route.
        world.cardboardCleared = InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "NeighborCardboard_Cleared",
            routeRoot,
            new Vector3(1.62f, 0.02f, 19.55f),
            new Vector3(0.5f, 0.4f, 0.46f),
            new Vector3(0f, 6f, 0f),
            true);
        world.cardboardCleared.SetActive(false);
        world.cardboardAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            world.cardboardCleared,
            "Story04Rebuild_NeighborCardboardCleared",
            world.cardboardBlocking.transform.localPosition - world.cardboardCleared.transform.localPosition,
            0.62f);

        Transform neighborRightHand = StoryChapterBuilderCommon.FindHumanoidBone(
            route.neighborAtLanding,
            HumanBodyBones.RightHand);
        Transform neighborRightLowerArm = StoryChapterBuilderCommon.FindHumanoidBone(
            route.neighborAtLanding,
            HumanBodyBones.RightLowerArm);
        if (neighborRightHand == null || neighborRightLowerArm == null)
            throw new InvalidOperationException("Nermin sağ el/bilek kemikleri bulunamadı.");
        Vector3 forearmDirection = (neighborRightHand.position - neighborRightLowerArm.position).normalized;
        Vector3 braceletPosition = Vector3.Lerp(neighborRightHand.position, neighborRightLowerArm.position, 0.12f);
        Quaternion braceletRotation = Quaternion.FromToRotation(Vector3.up, forearmDirection);
        world.neighborSupportBracelet = StoryAuthoredPropFactory.CreateBracelet(
            "NerminSupportBracelet_Hold",
            neighborRightHand,
            braceletPosition,
            new Vector3(0.115f, 0.028f, 0.115f),
            braceletRotation.eulerAngles,
            materials.amber,
            true);
        // Bileklik telefonda rahat basılabilir bir hedef olmalı; el kemiğinin
        // tuhaf ölçekleri altında dünya uzayında en az ~20 cm'lik hacim korunur.
        BoxCollider braceletCollider = world.neighborSupportBracelet.GetComponent<BoxCollider>();
        if (braceletCollider != null)
        {
            Vector3 braceletScale = braceletCollider.transform.lossyScale;
            Vector3 braceletMinimum = new Vector3(
                0.2f / Mathf.Max(Mathf.Abs(braceletScale.x), 0.0001f),
                0.16f / Mathf.Max(Mathf.Abs(braceletScale.y), 0.0001f),
                0.2f / Mathf.Max(Mathf.Abs(braceletScale.z), 0.0001f));
            braceletCollider.size = Vector3.Max(braceletCollider.size, braceletMinimum);
        }
        Transform braceletRing = world.neighborSupportBracelet.transform.Find("BraceletRing");
        if (braceletRing == null)
            throw new InvalidOperationException("Nermin destek bilekliği halka görseli bulunamadı.");
        braceletRing.localPosition = new Vector3(-1.21f, -0.99f, 0.64f);
        braceletRing.localRotation = Quaternion.Euler(0f, 0f, -20.366f);
        braceletRing.localScale = Vector3.one * 2.8915f;

        // Etkileşim hacmi görünmez kalır; altındaki farklı yönlü kaldırım taşları
        // oyuncuya doğal bir açık alan verir. Eski parlak mavi disk, bitmiş çevre
        // yerine debug hedefi gibi görünüyordu.
        world.facadeSafePoint = StoryChapterBuilderCommon.CreatePrimitive(
            "FacadeClearPavingPoint",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.025f, 25.0f),
            new Vector3(0.78f, 0.035f, 0.78f),
            materials.concrete,
            routeRoot,
            true);
        world.facadeSafePoint.GetComponent<Collider>().isTrigger = true;
        world.facadeSafePoint.GetComponent<Renderer>().enabled = false;
        for (int index = -1; index <= 1; index++)
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                "FacadeClearPaver_" + (index + 1),
                PrimitiveType.Cube,
                new Vector3(index * 0.43f, 0.008f, 25.0f + Mathf.Abs(index) * 0.035f),
                new Vector3(0.38f, 0.016f, 0.66f),
                index == 0 ? materials.cream : materials.concrete,
                routeRoot,
                false,
                Quaternion.Euler(0f, index * 4f, 0f));
        }

        BuildCityWorldBox(routeRoot, materials);
        BuildStreetDressing(routeRoot, materials);
        BuildAssemblySet(root, routeRoot, family, materials, adultController, world);
        return world;
    }

    private static void BuildUpperCorridorInterior(
        EvacuationWorld route,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform routeRoot = route.environment.transform.Find("ContinuousEvacuationRoute");
        if (routeRoot == null)
            throw new InvalidOperationException("Tahliye koridor kökü bulunamadı.");

        // The first Story 04 camera needs enough depth to frame Deniz, Can, the unsafe
        // elevator and the stair door in a portrait shot. Extend the actual corridor
        // around that authored camera instead of placing the camera outside a missing wall.
        Transform floor = routeRoot.Find("UpperCorridorFloor");
        Transform leftWall = routeRoot.Find("UpperCorridorLeftWall");
        Transform rightWall = routeRoot.Find("UpperCorridorRightWall");
        Transform backWall = routeRoot.Find("UpperCorridorBackWall");
        if (floor == null || leftWall == null || rightWall == null || backWall == null)
            throw new InvalidOperationException("Üst koridor kabuğu eksik; kapalı kamera hacmi üretilemedi.");

        floor.localPosition = new Vector3(0f, 3.9f, -5.4f);
        floor.localScale = new Vector3(5f, 0.2f, 11f);
        leftWall.localPosition = new Vector3(-2.5f, 5.35f, -5.4f);
        leftWall.localScale = new Vector3(0.18f, 2.9f, 11f);
        rightWall.localPosition = new Vector3(2.45f, 5.4f, -6.65f);
        rightWall.localScale = new Vector3(0.3f, 3.12f, 8.5f);
        backWall.localPosition = new Vector3(0f, 5.35f, -10.86f);
        backWall.localScale = new Vector3(5f, 2.9f, 0.18f);

        StoryChapterBuilderCommon.CreatePrimitive(
            "UpperCorridorCeiling",
            PrimitiveType.Cube,
            new Vector3(0f, 6.88f, -5.4f),
            new Vector3(5.55f, 0.22f, 11.05f),
            materials.wall,
            routeRoot,
            false);

        StoryChapterBuilderCommon.CreatePrimitive(
            "ElevatorWallUpperSeal",
            PrimitiveType.Cube,
            new Vector3(2.45f, 6.58f, -1.15f),
            new Vector3(0.3f, 0.78f, 2.55f),
            materials.wall,
            routeRoot,
            false);

        // The objective ribbon already names both choices. Large world-space labels above
        // these narrow openings were clipped by the lintels in portrait framing and read as
        // text leaking through the wall, so the authored doors themselves carry the choice.

        // The imported door has two glass insets. Back only those insets so the
        // exterior cannot show through; a door-sized slab visibly protrudes past
        // this model's 0.83 m leaf and reads as a large wall panel.
        GameObject upperGlassBacking = StoryChapterBuilderCommon.CreatePrimitive(
            "StairDoorGlassBacking_Upper",
            PrimitiveType.Cube,
            new Vector3(0f, 5.28f, 0.14f),
            new Vector3(0.46f, 0.65f, 0.025f),
            materials.wall,
            routeRoot,
            false);
        upperGlassBacking.transform.SetParent(route.stairDoorClosed.transform, true);

        GameObject lowerGlassBacking = StoryChapterBuilderCommon.CreatePrimitive(
            "StairDoorGlassBacking_Lower",
            PrimitiveType.Cube,
            new Vector3(0f, 4.47f, 0.14f),
            new Vector3(0.46f, 0.48f, 0.025f),
            materials.wall,
            routeRoot,
            false);
        lowerGlassBacking.transform.SetParent(route.stairDoorClosed.transform, true);
    }

    private static void RestyleRebuildEmergencyLights(
        EvacuationWorld route,
        StoryChapterBuilderCommon.Materials materials)
    {
        if (route?.emergencyLightRoute == null)
            throw new InvalidOperationException("Story 04 acil aydınlatma rotası bulunamadı.");

        Transform[] lamps = route.emergencyLightRoute.GetComponentsInChildren<Transform>(true)
            .Where(candidate => candidate.parent == route.emergencyLightRoute.transform &&
                                candidate.name == "EmergencyLamp")
            .ToArray();
        if (lamps.Length != 4)
            throw new InvalidOperationException($"Story 04 dört acil aplik bekliyordu, bulunan: {lamps.Length}");

        (string name, Vector3 position, Vector3 euler)[] specs =
        {
            ("EmergencySconce_UpperCorridor", new Vector3(1.9f, 5.62f, -0.62f), Vector3.zero),
            ("EmergencySconce_UpperLanding", new Vector3(-2.39f, 3.62f, 7.8f), new Vector3(0f, -90f, 0f)),
            ("EmergencySconce_LowerLanding", new Vector3(2.39f, 1.62f, 16.8f), new Vector3(0f, 90f, 0f)),
            ("EmergencySconce_BuildingExit", new Vector3(0f, 2.48f, 20.55f), Vector3.zero)
        };

        for (int index = 0; index < lamps.Length; index++)
        {
            Transform lamp = lamps[index];
            Renderer[] importedRenderers = lamp.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in importedRenderers)
                renderer.enabled = false;
            foreach (Collider collider in lamp.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            lamp.name = specs[index].name;
            lamp.SetPositionAndRotation(specs[index].position, Quaternion.Euler(specs[index].euler));
            lamp.localScale = Vector3.one;

            Create04RLocalPrimitive("WallPlate", PrimitiveType.Cube, lamp,
                Vector3.zero, new Vector3(0.3f, 0.42f, 0.07f), materials.metal);
            Create04RLocalPrimitive("ProtectiveHood", PrimitiveType.Cube, lamp,
                new Vector3(0f, 0.14f, -0.085f), new Vector3(0.34f, 0.08f, 0.18f), materials.wood);
            Create04RLocalPrimitive("AmberLens", PrimitiveType.Sphere, lamp,
                new Vector3(0f, -0.035f, -0.105f), new Vector3(0.2f, 0.24f, 0.1f), materials.amber);
            Create04RLocalPrimitive("LowerGuard", PrimitiveType.Cube, lamp,
                new Vector3(0f, -0.19f, -0.07f), new Vector3(0.25f, 0.045f, 0.14f), materials.dark);

            Light light = lamp.GetComponent<Light>();
            if (light == null)
                light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.63f, 0.28f);
            light.intensity = 1.15f;
            light.range = 4.2f;
            light.shadows = LightShadows.None;
        }
    }

    private static void RestyleRebuildStairwell(
        EvacuationWorld route,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform routeRoot = route.environment.transform.Find("ContinuousEvacuationRoute");
        if (routeRoot == null)
            throw new InvalidOperationException("Story 04 merdiven rota kökü bulunamadı.");

        Transform[] steps = routeRoot.GetComponentsInChildren<Transform>(true)
            .Where(candidate => candidate.name.StartsWith("UpperVisibleStep_", StringComparison.Ordinal) ||
                                candidate.name.StartsWith("LowerVisibleStep_", StringComparison.Ordinal))
            .ToArray();
        if (steps.Length != 26)
            throw new InvalidOperationException($"Story 04 merdiveninde 26 basamak bekleniyordu, bulunan: {steps.Length}");

        foreach (Transform step in steps)
        {
            Renderer renderer = step.GetComponent<Renderer>();
            if (renderer == null)
                throw new InvalidOperationException(step.name + " görünür renderer taşımıyor.");
            renderer.sharedMaterial = materials.concrete;
            Bounds bounds = renderer.bounds;
            StoryChapterBuilderCommon.CreatePrimitive(
                step.name + "_Nosing",
                PrimitiveType.Cube,
                new Vector3(bounds.center.x, bounds.max.y + 0.012f, bounds.min.z + 0.04f),
                new Vector3(bounds.size.x * 0.94f, 0.024f, 0.075f),
                materials.cream,
                routeRoot,
                false);
        }

        Renderer upperLandingRenderer = route.upperLanding != null
            ? route.upperLanding.GetComponent<Renderer>()
            : null;
        if (upperLandingRenderer == null)
            throw new InvalidOperationException("Story 04 üst sahanlık yüzeyi bulunamadı.");
        upperLandingRenderer.sharedMaterial = materials.concrete;
        StoryChapterBuilderCommon.CreatePrimitive(
            "UpperLandingSafetyEdge",
            PrimitiveType.Cube,
            new Vector3(0f, 2.012f, 7.13f),
            new Vector3(3.15f, 0.024f, 0.085f),
            materials.amber,
            routeRoot,
            false);

        Build04RHandrailRun(routeRoot, "UpperFlightHandrail",
            new Vector3(1.72f, 4.62f, 0.55f), new Vector3(1.72f, 2.72f, 7.05f), materials);
        Build04RHandrailRun(routeRoot, "LowerFlightHandrail",
            new Vector3(1.72f, 2.52f, 9.45f), new Vector3(1.72f, 0.66f, 16.12f), materials);

        StoryChapterBuilderCommon.CreatePrimitive(
            "UpperLandingRightWall_Rebuild",
            PrimitiveType.Cube,
            new Vector3(2.5f, 3.35f, 8.05f),
            new Vector3(0.18f, 3.1f, 2.7f),
            materials.wall,
            routeRoot,
            true);
    }

    private static void BuildRebuildArchitecturalFinish(
        EvacuationWorld route,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform routeRoot = route.environment.transform.Find("ContinuousEvacuationRoute");
        if (routeRoot == null)
            throw new InvalidOperationException("Story 04 mimari bitiş kökü bulunamadı.");

        BuildClosedStairwellShell(routeRoot, materials);
        BuildInteriorArchitecturalDetails(routeRoot, materials);
        BuildPrimaryBuildingExterior(routeRoot, materials);
    }

    private static void BuildClosedStairwellShell(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform shell = StoryChapterBuilderCommon.NewChild(routeRoot, "Story04_ClosedArchitectureShell");

        // The old stair cameras relied on a missing right wall and an open roof.
        // Close the actual volume instead so no portrait angle can see the skybox
        // over a half-height wall. These are real static blockers and are present
        // before the scene NavMesh is baked.
        StoryChapterBuilderCommon.CreatePrimitive(
            "StairwellRightUpperWall_Rebuild",
            PrimitiveType.Cube,
            new Vector3(2.5f, 4.55f, 8.18f),
            new Vector3(0.18f, 4.58f, 16.35f),
            materials.wall,
            shell,
            true);
        StoryChapterBuilderCommon.CreatePrimitive(
            "StairwellLeftUpperSeal_Rebuild",
            PrimitiveType.Cube,
            new Vector3(-2.5f, 6.03f, 8.18f),
            new Vector3(0.18f, 1.68f, 16.35f),
            materials.wall,
            shell,
            true);
        StoryChapterBuilderCommon.CreatePrimitive(
            "ElevatorForwardWallSeal_Rebuild",
            PrimitiveType.Cube,
            new Vector3(2.45f, 5.35f, -0.7f),
            new Vector3(0.3f, 2.9f, 1.25f),
            materials.wall,
            shell,
            true);
        GameObject stairwellCeiling = StoryChapterBuilderCommon.CreatePrimitive(
            "StairwellCeiling_Rebuild",
            PrimitiveType.Cube,
            new Vector3(0f, 6.86f, 8.18f),
            new Vector3(5.2f, 0.22f, 16.35f),
            materials.wall,
            shell,
            true);
        NavMeshModifier stairwellCeilingModifier = stairwellCeiling.AddComponent<NavMeshModifier>();
        stairwellCeilingModifier.ignoreFromBuild = true;

        GameObject lowerLandingCeiling = StoryChapterBuilderCommon.CreatePrimitive(
            "LowerLandingCeiling_Rebuild",
            PrimitiveType.Cube,
            new Vector3(0f, 2.86f, 18.72f),
            new Vector3(5.2f, 0.2f, 4.95f),
            materials.wall,
            shell,
            true);
        NavMeshModifier lowerLandingCeilingModifier = lowerLandingCeiling.AddComponent<NavMeshModifier>();
        lowerLandingCeilingModifier.ignoreFromBuild = true;
        StoryChapterBuilderCommon.CreatePrimitive(
            "StairwellLowerBulkhead_Rebuild",
            PrimitiveType.Cube,
            new Vector3(0f, 4.86f, 16.47f),
            new Vector3(5.2f, 4.0f, 0.2f),
            materials.wall,
            shell,
            true);
    }

    private static void BuildInteriorArchitecturalDetails(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform finish = StoryChapterBuilderCommon.NewChild(routeRoot, "Story04_InteriorArchitecturalFinish");

        // Warm lower wall panels, skirting and a thin wayfinding stripe break the
        // former single-colour corridor without placing collision in the route.
        foreach ((string side, float x, float z, float length) panel in new[]
                 {
                     ("Left", -2.39f, -5.4f, 10.45f),
                     ("Right", 2.285f, -6.65f, 8.18f)
                 })
        {
            Create04RLocalPrimitive(
                "UpperCorridorWainscot_" + panel.side,
                PrimitiveType.Cube,
                finish,
                new Vector3(panel.x, 4.58f, panel.z),
                new Vector3(0.045f, 1.0f, panel.length),
                materials.concrete);
            Create04RLocalPrimitive(
                "UpperCorridorSkirting_" + panel.side,
                PrimitiveType.Cube,
                finish,
                new Vector3(panel.x + (panel.x < 0f ? 0.015f : -0.015f), 4.1f, panel.z),
                new Vector3(0.065f, 0.15f, panel.length),
                materials.dark);
            Create04RLocalPrimitive(
                "UpperCorridorWayfindingBand_" + panel.side,
                PrimitiveType.Cube,
                finish,
                new Vector3(panel.x + (panel.x < 0f ? 0.02f : -0.02f), 5.12f, panel.z),
                new Vector3(0.055f, 0.09f, panel.length),
                materials.teal);
        }

        foreach (float z in new[] { -9.25f, -6.2f, -3.15f })
            Create04RLocalPrimitive(
                "UpperCorridorCeilingJoint_" + Mathf.RoundToInt(Mathf.Abs(z) * 10f),
                PrimitiveType.Cube,
                finish,
                new Vector3(0f, 6.755f, z),
                new Vector3(4.55f, 0.025f, 0.075f),
                materials.metal);
        BuildInteriorCeilingFixture(finish, "UpperCorridorCeilingFixture_A", new Vector3(0f, 6.69f, -7.75f), materials);
        BuildInteriorCeilingFixture(finish, "UpperCorridorCeilingFixture_B", new Vector3(0f, 6.69f, -3.9f), materials);

        InstantiateTownEnvironment(
            "SM_Prop_Rug_01.fbx",
            "UpperCorridorRunnerRug",
            finish,
            new Vector3(0f, 4.012f, -5.8f),
            new Vector3(1.25f, 0.045f, 2.7f),
            Vector3.zero);

        foreach ((string name, float z) door in new[]
                 {
                     ("CorridorApartmentDoor_31", -8.15f),
                     ("CorridorApartmentDoor_32", -4.35f)
                 })
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                DoorPath,
                door.name,
                finish,
                new Vector3(-2.38f, 4.02f, door.z),
                new Vector3(0.14f, 2.15f, 1.02f),
                new Vector3(0f, 90f, 0f),
                false,
                false);
            Create04RLocalPrimitive(
                door.name + "_NumberPlate",
                PrimitiveType.Cube,
                finish,
                new Vector3(-2.275f, 5.55f, door.z + 0.37f),
                new Vector3(0.045f, 0.2f, 0.28f),
                materials.amber);
        }

        BuildWallNoticeBoard(
            finish,
            "UpperCorridorNoticeBoard",
            new Vector3(-2.325f, 5.58f, -1.55f),
            1.18f,
            materials);
        BuildWallFireCabinet(
            finish,
            new Vector3(-2.31f, 4.82f, -2.45f),
            materials);
        BuildWallNoticeBoard(
            finish,
            "LowerLandingResidentBoard",
            new Vector3(-2.325f, 1.62f, 19.42f),
            1.02f,
            materials);
        InstantiateTownEnvironment(
            "SM_Prop_LetterBox_01.fbx",
            "LowerLandingLetterBoxes",
            finish,
            new Vector3(-2.35f, 0.62f, 20.12f),
            new Vector3(0.18f, 0.72f, 0.86f),
            new Vector3(0f, 90f, 0f));

        foreach (float x in new[] { -2.39f, 2.39f })
        {
            Create04RLocalPrimitive(
                x < 0f ? "StairwellWayfindingBand_Left" : "StairwellWayfindingBand_Right",
                PrimitiveType.Cube,
                finish,
                new Vector3(x, 3.62f, 8.2f),
                new Vector3(0.04f, 0.1f, 15.9f),
                materials.teal);
        }
        BuildStairwellWallPanel(finish, "StairwellWallPanel_Upper", new Vector3(2.385f, 4.75f, 3.75f), materials);
        BuildStairwellWallPanel(finish, "StairwellWallPanel_Lower", new Vector3(2.385f, 4.75f, 10.95f), materials);
    }

    private static void BuildInteriorCeilingFixture(
        Transform parent,
        string name,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform fixture = StoryChapterBuilderCommon.NewChild(parent, name);
        fixture.localPosition = position;
        Create04RLocalPrimitive(
            "Housing",
            PrimitiveType.Cube,
            fixture,
            Vector3.zero,
            new Vector3(0.54f, 0.025f, 0.24f),
            materials.metal);
        Create04RLocalPrimitive(
            "Diffuser",
            PrimitiveType.Cube,
            fixture,
            new Vector3(0f, -0.026f, 0f),
            new Vector3(0.46f, 0.015f, 0.17f),
            materials.cream);
        GameObject lightObject = new GameObject("WarmCeilingLight");
        lightObject.transform.SetParent(fixture, false);
        lightObject.transform.localPosition = new Vector3(0f, -0.065f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.62f);
        light.intensity = 0.38f;
        light.range = 3.2f;
        light.shadows = LightShadows.None;
    }

    private static void BuildWallNoticeBoard(
        Transform parent,
        string name,
        Vector3 position,
        float height,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform board = StoryChapterBuilderCommon.NewChild(parent, name);
        board.localPosition = position;
        Create04RLocalPrimitive(
            "WoodBacking",
            PrimitiveType.Cube,
            board,
            Vector3.zero,
            new Vector3(0.07f, height, 1.28f),
            materials.wood);
        Create04RLocalPrimitive(
            "NoticePaper_A",
            PrimitiveType.Cube,
            board,
            new Vector3(0.055f, 0.18f, -0.32f),
            new Vector3(0.035f, height * 0.38f, 0.42f),
            materials.cream);
        Create04RLocalPrimitive(
            "NoticePaper_B",
            PrimitiveType.Cube,
            board,
            new Vector3(0.057f, -0.17f, 0.31f),
            new Vector3(0.035f, height * 0.3f, 0.38f),
            materials.teal);
        Create04RLocalPrimitive(
            "NoticePin",
            PrimitiveType.Sphere,
            board,
            new Vector3(0.085f, 0.36f, -0.32f),
            Vector3.one * 0.055f,
            materials.coral);
    }

    private static void BuildWallFireCabinet(
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform cabinet = StoryChapterBuilderCommon.NewChild(parent, "UpperCorridorFireCabinet");
        cabinet.localPosition = position;
        Create04RLocalPrimitive(
            "CabinetBody",
            PrimitiveType.Cube,
            cabinet,
            Vector3.zero,
            new Vector3(0.12f, 0.82f, 0.5f),
            materials.coral);
        Create04RLocalPrimitive(
            "CabinetGlass",
            PrimitiveType.Cube,
            cabinet,
            new Vector3(0.075f, 0f, 0f),
            new Vector3(0.04f, 0.56f, 0.32f),
            materials.glass);
        Create04RLocalPrimitive(
            "ExtinguisherBody",
            PrimitiveType.Cylinder,
            cabinet,
            new Vector3(0.105f, -0.06f, 0f),
            new Vector3(0.105f, 0.23f, 0.105f),
            materials.coral);
    }

    private static void BuildStairwellWallPanel(
        Transform parent,
        string name,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform panel = StoryChapterBuilderCommon.NewChild(parent, name);
        panel.localPosition = position;
        Create04RLocalPrimitive(
            "GlassPanel",
            PrimitiveType.Cube,
            panel,
            Vector3.zero,
            new Vector3(0.045f, 1.1f, 1.22f),
            materials.glass);
        Create04RLocalPrimitive(
            "FrameTop",
            PrimitiveType.Cube,
            panel,
            new Vector3(-0.035f, 0.61f, 0f),
            new Vector3(0.065f, 0.09f, 1.38f),
            materials.dark);
        Create04RLocalPrimitive(
            "FrameBottom",
            PrimitiveType.Cube,
            panel,
            new Vector3(-0.035f, -0.61f, 0f),
            new Vector3(0.065f, 0.09f, 1.38f),
            materials.dark);
        foreach (float z in new[] { -0.66f, 0.66f })
            Create04RLocalPrimitive(
                z < 0f ? "FrameSideA" : "FrameSideB",
                PrimitiveType.Cube,
                panel,
                new Vector3(-0.035f, 0f, z),
                new Vector3(0.065f, 1.3f, 0.08f),
                materials.dark);
        Create04RLocalPrimitive(
            "FrameMullion",
            PrimitiveType.Cube,
            panel,
            new Vector3(-0.04f, 0f, 0f),
            new Vector3(0.07f, 1.16f, 0.055f),
            materials.metal);
    }

    private static void BuildPrimaryBuildingExterior(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform exterior = StoryChapterBuilderCommon.NewChild(routeRoot, "Story04_PrimaryBuildingExteriorFinish");

        // A full roof deck and parapet close the apartment block silhouette from
        // every street camera. Rooftop service pieces give the long block believable
        // scale instead of leaving a flat wall cut off against the skybox.
        Create04RLocalPrimitive(
            "PrimaryBuildingRoofDeck",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0f, 8.96f, 4.98f),
            new Vector3(5.6f, 0.22f, 31.9f),
            materials.dark);
        foreach ((string name, Vector3 position, Vector3 scale) parapet in new[]
                 {
                     ("PrimaryBuildingParapet_Front", new Vector3(0f, 9.34f, 20.96f), new Vector3(5.72f, 0.78f, 0.28f)),
                     ("PrimaryBuildingParapet_Back", new Vector3(0f, 9.34f, -11.0f), new Vector3(5.72f, 0.78f, 0.28f)),
                     ("PrimaryBuildingParapet_Left", new Vector3(-2.72f, 9.34f, 4.98f), new Vector3(0.28f, 0.78f, 31.7f)),
                     ("PrimaryBuildingParapet_Right", new Vector3(2.72f, 9.34f, 4.98f), new Vector3(0.28f, 0.78f, 31.7f))
                 })
            Create04RLocalPrimitive(
                parapet.name,
                PrimitiveType.Cube,
                exterior,
                parapet.position,
                parapet.scale,
                materials.wall);

        foreach ((string name, Vector3 position, Vector3 scale) cap in new[]
                 {
                     ("PrimaryBuildingParapetCap_Front", new Vector3(0f, 9.78f, 20.96f), new Vector3(5.86f, 0.1f, 0.38f)),
                     ("PrimaryBuildingParapetCap_Back", new Vector3(0f, 9.78f, -11.0f), new Vector3(5.86f, 0.1f, 0.38f)),
                     ("PrimaryBuildingParapetCap_Left", new Vector3(-2.72f, 9.78f, 4.98f), new Vector3(0.38f, 0.1f, 31.9f)),
                     ("PrimaryBuildingParapetCap_Right", new Vector3(2.72f, 9.78f, 4.98f), new Vector3(0.38f, 0.1f, 31.9f))
                 })
            Create04RLocalPrimitive(
                cap.name,
                PrimitiveType.Cube,
                exterior,
                cap.position,
                cap.scale,
                materials.dark);

        Create04RLocalPrimitive(
            "PrimaryBuildingFacadeCornice",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0f, 8.86f, 21.2f),
            new Vector3(5.78f, 0.25f, 0.82f),
            materials.dark);
        foreach ((float y, Material material) band in new[]
                 {
                     (3.36f, materials.teal),
                     (5.46f, materials.cream),
                     (7.56f, materials.teal)
                 })
            Create04RLocalPrimitive(
                "PrimaryFacadeFloorBand_" + Mathf.RoundToInt(band.y * 10f),
                PrimitiveType.Cube,
                exterior,
                new Vector3(0f, band.y, 21.235f),
                new Vector3(5.18f, 0.11f, 0.18f),
                band.material);

        for (int floor = 0; floor < 3; floor++)
        {
            float y = 2.3f + floor * 2.1f;
            BuildFacadeWindowFrame(exterior, "PrimaryFacadeWindowFrame_L" + floor, -2.15f, y, materials);
            BuildFacadeWindowFrame(exterior, "PrimaryFacadeWindowFrame_R" + floor, 2.15f, y, materials);
        }

        Create04RLocalPrimitive(
            "PrimaryEntranceCanopy",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0f, 3.42f, 21.65f),
            new Vector3(2.46f, 0.14f, 0.92f),
            materials.dark);
        Create04RLocalPrimitive(
            "PrimaryEntranceCanopyFascia",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0f, 3.34f, 22.07f),
            new Vector3(2.5f, 0.18f, 0.12f),
            materials.teal);

        foreach (float x in new[] { -2.78f, 2.78f })
        {
            Create04RLocalPrimitive(
                x < 0f ? "PrimaryFacadeDownpipe_Left" : "PrimaryFacadeDownpipe_Right",
                PrimitiveType.Cylinder,
                exterior,
                new Vector3(x, 4.35f, 21.23f),
                new Vector3(0.055f, 4.28f, 0.055f),
                materials.metal);
            for (int bracket = 0; bracket < 3; bracket++)
                Create04RLocalPrimitive(
                    (x < 0f ? "PrimaryDownpipeBracketL_" : "PrimaryDownpipeBracketR_") + bracket,
                    PrimitiveType.Cube,
                    exterior,
                    new Vector3(x, 1.7f + bracket * 2.45f, 21.15f),
                    new Vector3(0.18f, 0.055f, 0.11f),
                    materials.dark);
        }

        foreach (float x in new[] { -2.15f, 2.15f })
        {
            Create04RLocalPrimitive(
                x < 0f ? "PrimaryFacadeFlowerBox_Left" : "PrimaryFacadeFlowerBox_Right",
                PrimitiveType.Cube,
                exterior,
                new Vector3(x, 1.57f, 21.48f),
                new Vector3(1.05f, 0.2f, 0.32f),
                materials.wood);
            for (int plant = -1; plant <= 1; plant++)
                Create04RLocalPrimitive(
                    (x < 0f ? "PrimaryFlowerL_" : "PrimaryFlowerR_") + (plant + 1),
                    PrimitiveType.Sphere,
                    exterior,
                    new Vector3(x + plant * 0.3f, 1.78f, 21.5f),
                    new Vector3(0.17f, 0.22f, 0.17f),
                    materials.grass);
        }

        GameObject waterStore = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_RoofWaterStore_01.prefab",
            "PrimaryRoofWaterStore",
            exterior,
            new Vector3(1.25f, 9.08f, 4.4f),
            new Vector3(1.18f, 1.5f, 1.18f),
            new Vector3(0f, 18f, 0f),
            false,
            false);
        GameObject roofVent = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_RoofVent_03.prefab",
            "PrimaryRoofVent",
            exterior,
            new Vector3(-1.35f, 9.08f, 12.2f),
            new Vector3(0.82f, 1.05f, 0.82f),
            new Vector3(0f, -12f, 0f),
            false,
            false);
        GameObject chimney = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_Chimney_02.prefab",
            "PrimaryRoofChimney",
            exterior,
            new Vector3(-1.25f, 9.08f, -4.4f),
            new Vector3(0.88f, 1.3f, 0.88f),
            new Vector3(0f, 10f, 0f),
            false,
            false);
        ConfigureBackdropRenderer(waterStore, false);
        ConfigureBackdropRenderer(roofVent, false);
        ConfigureBackdropRenderer(chimney, false);

        Create04RLocalPrimitive(
            "PrimaryRoofServiceCabin",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0.65f, 9.58f, -1.8f),
            new Vector3(1.45f, 1.0f, 1.7f),
            materials.wall);
        Create04RLocalPrimitive(
            "PrimaryRoofServiceCabinCap",
            PrimitiveType.Cube,
            exterior,
            new Vector3(0.65f, 10.12f, -1.8f),
            new Vector3(1.62f, 0.12f, 1.88f),
            materials.dark);
    }

    private static void BuildFacadeWindowFrame(
        Transform parent,
        string name,
        float x,
        float y,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform frame = StoryChapterBuilderCommon.NewChild(parent, name);
        frame.localPosition = new Vector3(x, y, 21.275f);
        foreach ((string partName, Vector3 position, Vector3 scale) part in new[]
                 {
                     ("FrameTop", new Vector3(0f, 0.64f, 0f), new Vector3(1.4f, 0.08f, 0.16f)),
                     ("FrameBottom", new Vector3(0f, -0.64f, 0f), new Vector3(1.4f, 0.08f, 0.16f)),
                     ("FrameLeft", new Vector3(-0.66f, 0f, 0f), new Vector3(0.08f, 1.32f, 0.16f)),
                     ("FrameRight", new Vector3(0.66f, 0f, 0f), new Vector3(0.08f, 1.32f, 0.16f)),
                     ("FrameMullion", Vector3.zero, new Vector3(0.055f, 1.16f, 0.18f)),
                     ("FrameTransom", Vector3.zero, new Vector3(1.18f, 0.05f, 0.18f))
                 })
            Create04RLocalPrimitive(
                part.partName,
                PrimitiveType.Cube,
                frame,
                part.position,
                part.scale,
                part.partName == "FrameMullion" || part.partName == "FrameTransom"
                    ? materials.metal
                    : materials.dark);
        Create04RLocalPrimitive(
            "WindowSill",
            PrimitiveType.Cube,
            frame,
            new Vector3(0f, -0.73f, 0.08f),
            new Vector3(1.5f, 0.11f, 0.34f),
            materials.cream);
    }

    private static void Build04RHandrailRun(
        Transform parent,
        string name,
        Vector3 start,
        Vector3 end,
        StoryChapterBuilderCommon.Materials materials)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        StoryChapterBuilderCommon.CreatePrimitive(
            name,
            PrimitiveType.Cylinder,
            (start + end) * 0.5f,
            new Vector3(0.055f, length * 0.5f, 0.055f),
            materials.teal,
            parent,
            false,
            Quaternion.FromToRotation(Vector3.up, direction.normalized));

        for (int index = 0; index < 4; index++)
        {
            Vector3 railPoint = Vector3.Lerp(start, end, index / 3f);
            float postHeight = 0.82f;
            StoryChapterBuilderCommon.CreatePrimitive(
                name + "_Post_" + index,
                PrimitiveType.Cylinder,
                railPoint - Vector3.up * postHeight * 0.5f,
                new Vector3(0.045f, postHeight * 0.5f, 0.045f),
                materials.metal,
                parent,
                false);
        }
    }

    private static GameObject Create04RLocalPrimitive(
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

    private static void ConfigureExteriorAmbience(
        AudioSource source,
        Vector3 position,
        float maxDistance)
    {
        if (source == null)
            return;

        source.transform.position = position;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 4f;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
    }

    private static void BuildGasLeakChoice(
        RebuildWorld world,
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform hazard = StoryChapterBuilderCommon.NewChild(routeRoot, "StreetGasLeakChoice");
        hazard.position = new Vector3(-2.2f, 0.02f, 28.25f);
        world.gasLeakHazard = hazard.gameObject;

        for (int index = 0; index < 3; index++)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                CityPropRoot + "/Prop_ACVent_Stright.prefab",
                "DamagedGasPipe_Straight_" + (char)('A' + index),
                hazard,
                new Vector3(-2.35f, 0.04f + index * 0.58f, 28.2f),
                new Vector3(0.48f, 0.62f, 0.58f),
                new Vector3(0f, 0f, 90f),
                false,
                false);
        }
        world.gasLeakInspectionSource = StoryChapterBuilderCommon.InstantiateAsset(
            CityPropRoot + "/Prop_ACVent_Cornor.prefab",
            "DamagedGasPipe_BrokenElbow",
            hazard,
            new Vector3(-2.32f, 1.56f, 28.2f),
            new Vector3(0.62f, 0.58f, 0.62f),
            new Vector3(0f, 0f, 18f),
            false,
            false);
        world.gasLeakUnsafeSource = StoryChapterBuilderCommon.InstantiateAsset(
            PropaneTankPath,
            "GasServiceCylinder_Fallen",
            hazard,
            new Vector3(-2.05f, 0.05f, 28.78f),
            new Vector3(0.48f, 0.78f, 0.48f),
            new Vector3(78f, 8f, 20f),
            false,
            false);

        foreach ((string name, Vector3 position, float yaw) cone in new[]
                 {
                     ("GasLeakCone_A", new Vector3(-1.35f, 0.03f, 27.45f), -8f),
                     ("GasLeakCone_B", new Vector3(-1.35f, 0.03f, 29.05f), 11f)
                 })
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                CityPropRoot + "/Prop_RoadCone_01.prefab",
                cone.name,
                hazard,
                cone.position,
                new Vector3(0.34f, 0.62f, 0.34f),
                new Vector3(0f, cone.yaw, 0f),
                false,
                false);
        }

        if (world.route.streetDust != null)
            world.route.streetDust.gameObject.SetActive(false);
        ParticleSystem vapor = StoryChapterBuilderCommon.CreateDust(
            "GasLeakVapor",
            hazard,
            new Vector3(-2.03f, 1.67f, 28.18f),
            materials,
            0);
        ParticleSystem.MainModule main = vapor.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.8f;
        main.maxParticles = 28;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.82f, 0.9f, 0.72f, 0.42f));
        main.gravityModifier = -0.015f;
        ParticleSystem.EmissionModule emission = vapor.emission;
        emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
        emission.rateOverTime = 8f;
        ParticleSystem.ShapeModule shape = vapor.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.06f;
        shape.rotation = new Vector3(0f, 90f, 0f);
        world.route.streetDust = vapor;

        AudioSource hiss = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            "GasLeak_Hiss",
            hazard,
            vapor.transform.position,
            StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_air_03.ogg"),
            0.16f,
            0.82f,
            1.2f,
            11f);
        hiss.loop = true;
        hiss.playOnAwake = false;
        world.streetWarningCreak = hiss;
    }

    private static void ReplaceNeighborLandingProps(
        EvacuationWorld route,
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        if (route.debrisBlocking != null)
            Object.DestroyImmediate(route.debrisBlocking);
        if (route.debrisCleared != null)
            Object.DestroyImmediate(route.debrisCleared);

        // The imported floor mat was a thick, grey, cube-like prop being called
        // "foam".  Use two light packing pads with a visible retaining strip so
        // the object, scale and requested action agree.
        route.debrisBlocking = CreateNeighborFoamBundle(
            "NeighborFoam_Blocking_Drag",
            routeRoot,
            // Middle prop is slightly inboard and separated from both cane and
            // box, so all three read as distinct floor hazards rather than a pile.
            new Vector3(0.85f, 0.02f, 17.55f),
            -10f,
            materials);
        route.debrisCleared = CreateNeighborFoamBundle(
            "NeighborFoam_Cleared",
            routeRoot,
            new Vector3(1.58f, 0.02f, 18.72f),
            7f,
            materials);
        route.debrisCleared.SetActive(false);
        route.debrisAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            route.debrisCleared,
            "Story04Rebuild_FloorMatCleared",
            route.debrisBlocking.transform.localPosition - route.debrisCleared.transform.localPosition,
            0.62f);

        if (route.caneBlocked != null)
            Object.DestroyImmediate(route.caneBlocked);
        if (route.caneReachable != null)
            Object.DestroyImmediate(route.caneReachable);
        route.caneBlocked = StoryAuthoredPropFactory.CreateWalkingCane(
            "NeighborCane_Blocked",
            routeRoot,
            // Full-size 98 cm cane, laid flat beside (not on) the lower ramp.
            // The older 86 cm fit read as a toy and the closer placement put its
            // touch ray behind the ramp collider.
            new Vector3(1.12f, 0.02f, 16.88f),
            new Vector3(0.36f, 0.16f, 0.98f),
            new Vector3(90f, 0f, 0f),
            materials.wood,
            materials.dark,
            true);
        route.caneReachable = StoryAuthoredPropFactory.CreateWalkingCane(
            "NeighborCane_Reachable",
            routeRoot,
            new Vector3(-0.9f, 0.02f, 18.08f),
            // The curved handle makes the authored mesh wider than its shaft.
            // Leave enough X allowance for the uniform fit to reach a real
            // 98 cm walking-cane height instead of shrinking to toy scale.
            new Vector3(0.4f, 0.98f, 0.2f),
            new Vector3(0f, 0f, 6f),
            materials.wood,
            materials.dark,
            true);
        route.caneReachable.SetActive(false);
        route.caneAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(
            route.caneReachable,
            "Story04Rebuild_CaneReachable",
            route.caneBlocked.transform.localPosition - route.caneReachable.transform.localPosition,
            0.62f);
    }

    private static GameObject CreateNeighborFoamBundle(
        string name,
        Transform parent,
        Vector3 position,
        float yaw,
        StoryChapterBuilderCommon.Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject lower = Create04RLocalPrimitive(
            "FoamPad_Lower",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(-0.1f, 0.055f, 0f),
            new Vector3(0.42f, 0.11f, 0.34f),
            materials.cream);
        lower.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);
        GameObject upper = Create04RLocalPrimitive(
            "FoamPad_Upper",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(0.17f, 0.075f, 0.035f),
            new Vector3(0.32f, 0.13f, 0.28f),
            materials.cream);
        upper.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
        Create04RLocalPrimitive(
            "PackingRetainer",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(0.02f, 0.135f, 0.02f),
            new Vector3(0.055f, 0.045f, 0.38f),
            materials.teal);
        return root;
    }

    private static void BuildCityWorldBox(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform worldBox = StoryChapterBuilderCommon.NewChild(routeRoot, "Story04_CityWorldBox");
        GameObject ground = StoryChapterBuilderCommon.CreatePrimitive(
            "CityWorldBox_Ground",
            PrimitiveType.Cube,
            new Vector3(0f, -0.34f, 39f),
            new Vector3(36f, 0.2f, 54f),
            materials.grass,
            worldBox,
            false);
        ConfigureBackdropRenderer(ground, true);

        Transform outdoorGround = routeRoot.Find("OutdoorGround");
        if (outdoorGround != null)
            outdoorGround.GetComponent<Renderer>().sharedMaterial = materials.grass;
        Transform defaultRoad = routeRoot.Find("EmergencyVehicleRoad");
        if (defaultRoad != null)
            defaultRoad.GetComponent<Renderer>().enabled = false;
        foreach (string obsoleteSurface in new[] { "SafeOpenSidewalk", "UnsafeGlassShortcut" })
        {
            Transform surface = routeRoot.Find(obsoleteSurface);
            if (surface != null)
                surface.GetComponent<Renderer>().enabled = false;
        }
        for (int index = 0; index < 8; index++)
        {
            Transform dash = routeRoot.Find("RoadDash_" + index);
            if (dash != null)
                dash.GetComponent<Renderer>().enabled = false;
        }

        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) road in new[]
                 {
                     ("Env_Road_Free.prefab", "MainStreetVisual_SouthA",
                         new Vector3(5.6f, 0.01f, 18.7f), new Vector3(5.4f, 0.6f, 5.4f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_Road_Free.prefab", "MainStreetVisual_SouthB",
                         new Vector3(5.6f, 0.01f, 24.1f), new Vector3(5.4f, 0.6f, 5.4f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_Road_Free.prefab", "MainStreetVisual_A",
                         new Vector3(5.6f, 0.01f, 29.5f), new Vector3(5.4f, 0.6f, 5.4f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_Road_Free.prefab", "MainStreetVisual_B",
                         new Vector3(5.6f, 0.01f, 34.9f), new Vector3(5.4f, 0.6f, 5.4f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_Road_Free.prefab", "ParkCrosswalkVisual",
                         new Vector3(5.6f, 0.01f, 40.3f), new Vector3(5.4f, 0.6f, 5.4f),
                         new Vector3(0f, 90f, 0f))
                 })
        {
            GameObject roadVisual = StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + road.asset,
                road.name,
                worldBox,
                road.position,
                road.size,
                road.euler,
                false,
                false);
            ConfigureBackdropRenderer(roadVisual, false);
        }

        for (int index = 0; index < 7; index++)
        {
            GameObject stripe = StoryChapterBuilderCommon.CreatePrimitive(
                "ParkCrosswalkStripe_" + index.ToString("00"),
                PrimitiveType.Cube,
                new Vector3(5.6f, 0.075f, 39.4f + index * 0.3f),
                new Vector3(4.65f, 0.025f, 0.18f),
                materials.cream,
                worldBox,
                false);
            ConfigureBackdropRenderer(stripe, false);
        }

        // Keep the road legible as one neighbourhood street. Curbs frame the asphalt,
        // the east pavement gives the opposite facades a believable address, and the
        // short lane marks stop before the single park crossing.
        GameObject eastSidewalk = StoryChapterBuilderCommon.CreatePrimitive(
            "StreetEastSidewalkVisual",
            PrimitiveType.Cube,
            new Vector3(9.55f, 0.028f, 29.5f),
            new Vector3(2.2f, 0.055f, 27.0f),
            materials.concrete,
            worldBox,
            false);
        ConfigureBackdropRenderer(eastSidewalk, false);

        foreach ((string name, Vector3 position, Vector3 size) curb in new[]
                 {
                     ("StreetWestCurb_South", new Vector3(2.78f, 0.085f, 27.6f),
                         new Vector3(0.18f, 0.14f, 23.2f)),
                     ("StreetWestCurb_North", new Vector3(2.78f, 0.085f, 42.25f),
                         new Vector3(0.18f, 0.14f, 1.45f)),
                     ("StreetEastCurb_South", new Vector3(8.42f, 0.085f, 27.6f),
                         new Vector3(0.18f, 0.14f, 23.2f)),
                     ("StreetEastCurb_North", new Vector3(8.42f, 0.085f, 42.25f),
                         new Vector3(0.18f, 0.14f, 1.45f))
                 })
        {
            GameObject curbVisual = StoryChapterBuilderCommon.CreatePrimitive(
                curb.name,
                PrimitiveType.Cube,
                curb.position,
                curb.size,
                materials.cream,
                worldBox,
                false);
            ConfigureBackdropRenderer(curbVisual, false);
        }

        for (int index = 0; index < 6; index++)
        {
            GameObject laneDash = StoryChapterBuilderCommon.CreatePrimitive(
                "MainStreetLaneDash_" + index.ToString("00"),
                PrimitiveType.Cube,
                new Vector3(5.6f, 0.073f, 18.6f + index * 3.35f),
                new Vector3(0.09f, 0.025f, 1.15f),
                materials.cream,
                worldBox,
                false);
            ConfigureBackdropRenderer(laneDash, false);
        }

        GameObject parkCurbRamp = StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyParkCurbRamp",
            PrimitiveType.Cube,
            new Vector3(2.6f, 0.045f, 40.3f),
            new Vector3(0.62f, 0.045f, 2.35f),
            materials.concrete,
            worldBox,
            false);
        ConfigureBackdropRenderer(parkCurbRamp, false);

        GameObject evacuationSidewalk = StoryChapterBuilderCommon.CreatePrimitive(
            "EvacuationSidewalkVisual",
            PrimitiveType.Cube,
            new Vector3(0.85f, 0.026f, 32.09f),
            new Vector3(3.0f, 0.052f, 21.82f),
            materials.concrete,
            worldBox,
            false);
        ConfigureBackdropRenderer(evacuationSidewalk, false);

        // The procedural skybox is the horizon. Large unlit cubes used here previously
        // enclosed the route like a grey concrete box and made the assembly area unsafe.
        (string asset, Vector3 position, Vector3 size, Vector3 euler)[] distantBlocks =
        {
            ("Env_CompanyBuilding_02.prefab", new Vector3(-12.4f, 0f, 62.0f),
                new Vector3(7.0f, 8.1f, 6.2f), new Vector3(0f, 180f, 0f)),
            ("Env_ResidentBuilding_05.prefab", new Vector3(-4.2f, 0f, 61.8f),
                new Vector3(6.5f, 7.3f, 5.8f), new Vector3(0f, 180f, 0f)),
            ("Env_CommercialBuilding_04.prefab", new Vector3(4.1f, 0f, 61.8f),
                new Vector3(7.0f, 6.8f, 6.0f), new Vector3(0f, 180f, 0f)),
            ("Env_ResidentBuilding_02.prefab", new Vector3(12.2f, 0f, 62.0f),
                new Vector3(6.6f, 7.2f, 5.8f), new Vector3(0f, 180f, 0f))
        };
        for (int index = 0; index < distantBlocks.Length; index++)
        {
            (string asset, Vector3 position, Vector3 size, Vector3 euler) block = distantBlocks[index];
            GameObject building = StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + block.asset,
                "DistantCityBlock_" + (index + 1).ToString("00"),
                worldBox,
                block.position,
                block.size,
                block.euler,
                false,
                false);
            ConfigureBackdropRenderer(building, false);
        }

        CombineStaticMeshRenderers(
            worldBox,
            "Story04_RoadSurfaceBatch",
            worldBox.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer =>
                    renderer.name.StartsWith("ParkCrosswalkStripe_", StringComparison.Ordinal) ||
                    renderer.name.StartsWith("MainStreetLaneDash_", StringComparison.Ordinal) ||
                    renderer.name.StartsWith("StreetWestCurb_", StringComparison.Ordinal) ||
                    renderer.name.StartsWith("StreetEastCurb_", StringComparison.Ordinal) ||
                    renderer.name == "StreetEastSidewalkVisual" ||
                    renderer.name == "AssemblyParkCurbRamp" ||
                    renderer.name == "EvacuationSidewalkVisual"));
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

    private static Material GetOrCreateParkGrassMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ParkGrassMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "Story04_ParkGrass" };
            AssetDatabase.CreateAsset(material, ParkGrassMaterialPath);
        }

        Color parkGreen = new Color32(94, 151, 91, 255);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", parkGreen);
        else
            material.color = parkGreen;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.04f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildStreetDressing(
        Transform routeRoot,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform dressing = StoryChapterBuilderCommon.NewChild(routeRoot, "RebuildStreetDressing");
        // A short, readable neighbourhood street: aligned facades frame the evacuation route
        // without turning the assembly park into a canyon of random buildings.
        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) building in new[]
                 {
                     ("Env_ResidentBuilding_03.prefab", "StreetBackgroundBuilding_A",
                         new Vector3(-7.1f, 0f, 27.2f), new Vector3(6.2f, 6.5f, 5.6f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_CommercialBuilding_02.prefab", "StreetBackgroundBuilding_B",
                         new Vector3(-7.25f, 0f, 34.0f), new Vector3(6.4f, 5.8f, 5.6f),
                         new Vector3(0f, 90f, 0f)),
                     ("Env_ResidentBuilding_06.prefab", "StreetCornerBuilding_C",
                         new Vector3(12.0f, 0f, 26.8f), new Vector3(5.8f, 6.5f, 5.5f),
                         new Vector3(0f, -90f, 0f)),
                     ("Env_CompanyBuilding_01.prefab", "StreetCornerBuilding_D",
                         new Vector3(12.2f, 0f, 34.6f), new Vector3(6.0f, 6.8f, 5.7f),
                         new Vector3(0f, -90f, 0f))
                 })
        {
            GameObject facade = StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + building.asset,
                building.name,
                dressing,
                building.position,
                building.size,
                building.euler,
                false,
                false);
            ConfigureBackdropRenderer(facade, false);
        }

        BuildNeighborhoodStreetDepth(dressing, materials);

        foreach ((string asset, string name, Vector3 position, Vector3 size, float yaw) tree in new[]
                 {
                     ("tree_small.fbx", "TownTree_Facade", new Vector3(-4.35f, 0.02f, 25.8f),
                         new Vector3(2.3f, 3.6f, 2.3f), -8f),
                     ("tree_oak.fbx", "StreetTree_A", new Vector3(-4.6f, 0.02f, 34.4f),
                         new Vector3(2.8f, 4.2f, 2.8f), 16f),
                     ("tree_detailed.fbx", "StreetTree_B", new Vector3(7.4f, 0.02f, 48.4f),
                         new Vector3(2.7f, 4.0f, 2.7f), -18f)
                 })
        {
            GameObject streetTree = InstantiateNatureAsset(
                tree.asset,
                tree.name,
                dressing,
                tree.position,
                tree.size,
                new Vector3(0f, tree.yaw, 0f),
                null);
            ApplyNatureTreePalette(streetTree, materials.wood, materials.grass);
            ConfigureBackdropRenderer(streetTree, false);
        }

        BuildStreetNaturePatches(dressing, materials);

        GameObject streetBin = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_CTPTrashCan_01.prefab",
            "StreetRubbishBin",
            dressing,
            new Vector3(-6.45f, 0f, 38.3f),
            new Vector3(0.72f, 1.0f, 0.72f),
            new Vector3(0f, -20f, 0f),
            false,
            false);
        ConfigureBackdropRenderer(streetBin, false);

        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) prop in new[]
                 {
                     ("SM_Prop_ParkBench_01.fbx", "StreetRestBench", new Vector3(-8.0f, 0f, 38.3f),
                         new Vector3(2.2f, 0.9f, 0.85f), Vector3.zero),
                     ("SM_Prop_Sign_BusStop_01.fbx", "DamagedBusStopLandmark", new Vector3(2.3f, 0f, 34.4f),
                         new Vector3(0.65f, 2.55f, 0.45f), new Vector3(0f, 180f, 9f))
                 })
        {
            InstantiateTownEnvironment(
                prop.asset,
                prop.name,
                dressing,
                prop.position,
                prop.size,
                prop.euler);
        }

        GameObject firetruck = InstantiateTownEnvironment(
            "SM_Veh_Firetruck_01.fbx",
            "EmergencyFiretruck",
            dressing,
            new Vector3(4.15f, 0f, 30.8f),
            new Vector3(2.45f, 2.45f, 5.8f),
            Vector3.zero);
        firetruck.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        BuildEmergencyVehicleLights(firetruck.transform, materials);

        for (int i = 0; i < 3; i++)
            StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/Prop_RoadCone_0" + (i + 1) + ".prefab",
                "EmergencyLaneCone_" + i,
                dressing,
                new Vector3(2.25f, 0.02f, 27.6f + i * 2.55f),
                new Vector3(0.42f, 0.68f, 0.42f),
                new Vector3(0f, i * 18f, 0f),
                false,
                false);

        foreach (float z in new[] { 29.0f, 36.5f })
            BuildStoryStreetLamp(dressing, new Vector3(2.1f, 0.02f, z), materials);

        for (int i = 0; i < 6; i++)
        {
            float z = 39.75f + i * 2.55f;
            InstantiateTownEnvironment(
                "SM_Env_Fence_White_Straight_01.fbx",
                "AssemblyParkWestFence_" + i,
                dressing,
                new Vector3(-14.15f, 0f, z),
                new Vector3(0.18f, 1.0f, 2.45f),
                new Vector3(0f, 90f, 0f));
        }

        BuildStreetQuakeAftermath(dressing, materials);
    }

    private static void BuildNeighborhoodStreetDepth(
        Transform dressing,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform depth = StoryChapterBuilderCommon.NewChild(dressing, "Story04_NeighborhoodDepth");

        // The original street ended after one thin row of four facades. A second,
        // staggered row gives the mobile cameras roof overlap and neighbourhood
        // depth while remaining entirely outside the playable pavement.
        (string asset, Vector3 position, Vector3 size, Vector3 euler)[] buildings =
        {
            ("Env_ResidentBuilding_02.prefab", new Vector3(-14.25f, 0f, 22.8f),
                new Vector3(5.9f, 6.9f, 5.3f), new Vector3(0f, 90f, 0f)),
            ("Env_CompanyBuilding_02.prefab", new Vector3(-14.5f, 0f, 31.0f),
                new Vector3(6.2f, 7.6f, 5.6f), new Vector3(0f, 90f, 0f)),
            ("Env_ResidentBuilding_05.prefab", new Vector3(-14.2f, 0f, 39.1f),
                new Vector3(5.8f, 6.8f, 5.3f), new Vector3(0f, 90f, 0f)),
            ("Env_ResidentBuilding_04.prefab", new Vector3(17.5f, 0f, 22.9f),
                new Vector3(5.9f, 6.7f, 5.3f), new Vector3(0f, -90f, 0f)),
            ("Env_CommercialBuilding_03.prefab", new Vector3(17.7f, 0f, 31.3f),
                new Vector3(6.4f, 6.5f, 5.6f), new Vector3(0f, -90f, 0f)),
            ("Env_ResidentBuilding_01.prefab", new Vector3(17.45f, 0f, 39.7f),
                new Vector3(5.8f, 6.9f, 5.3f), new Vector3(0f, -90f, 0f))
        };
        for (int index = 0; index < buildings.Length; index++)
        {
            (string asset, Vector3 position, Vector3 size, Vector3 euler) building = buildings[index];
            GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + building.asset,
                "NeighborhoodDepthBuilding_" + (index + 1).ToString("00"),
                depth,
                building.position,
                building.size,
                building.euler,
                false,
                false);
            ConfigureBackdropRenderer(instance, false);
        }

        Transform frontage = StoryChapterBuilderCommon.NewChild(depth, "StreetFrontageDetails");
        foreach ((string name, Vector3 position, Vector3 scale, float roll, Material material) awning in new[]
                 {
                     ("WestCommercialAwning", new Vector3(-3.8f, 2.28f, 34.0f),
                         new Vector3(1.2f, 0.12f, 2.5f), -8f, materials.teal),
                     ("EastCompanyAwning", new Vector3(9.0f, 2.38f, 34.65f),
                         new Vector3(1.15f, 0.12f, 2.45f), 8f, materials.cream)
                 })
        {
            GameObject awningPanel = Create04RLocalPrimitive(
                awning.name,
                PrimitiveType.Cube,
                frontage,
                awning.position,
                awning.scale,
                awning.material);
            awningPanel.transform.localRotation = Quaternion.Euler(0f, 0f, awning.roll);
        }

        foreach ((string asset, string name, Vector3 position, Vector3 size, Vector3 euler) detail in new[]
                 {
                     ("Prop_ElectracityCabinet_02.prefab", "WestFacadeUtilityCabinet",
                         new Vector3(-4.05f, 0.02f, 31.15f), new Vector3(0.58f, 1.15f, 0.42f),
                         new Vector3(0f, 90f, 0f)),
                     ("Prop_ElectracityCabinet_01.prefab", "EastFacadeUtilityCabinet",
                         new Vector3(9.18f, 0.02f, 29.0f), new Vector3(0.55f, 1.05f, 0.4f),
                         new Vector3(0f, -90f, 0f)),
                     ("Prop_ACVent_Stright.prefab", "WestFacadeAC_A",
                         new Vector3(-4.05f, 3.45f, 26.35f), new Vector3(0.75f, 0.55f, 0.35f),
                         new Vector3(0f, 90f, 0f)),
                     ("Prop_ACVent_Stright.prefab", "WestFacadeAC_B",
                         new Vector3(-4.05f, 3.55f, 35.2f), new Vector3(0.75f, 0.55f, 0.35f),
                         new Vector3(0f, 90f, 0f)),
                     ("Prop_ACVent_Cross.prefab", "EastFacadeAC_A",
                         new Vector3(9.18f, 3.5f, 27.1f), new Vector3(0.74f, 0.58f, 0.36f),
                         new Vector3(0f, -90f, 0f)),
                     ("Prop_ACVent_Cross.prefab", "EastFacadeAC_B",
                         new Vector3(9.18f, 3.6f, 35.65f), new Vector3(0.74f, 0.58f, 0.36f),
                         new Vector3(0f, -90f, 0f)),
                     ("Prop_Plant_01.prefab", "WestStorefrontPlanter",
                         new Vector3(-4.05f, 0.02f, 28.35f), new Vector3(0.72f, 0.9f, 0.72f),
                         new Vector3(0f, 12f, 0f)),
                     ("Prop_Plant_03.prefab", "EastStorefrontPlanter",
                         new Vector3(9.22f, 0.02f, 32.15f), new Vector3(0.76f, 0.95f, 0.76f),
                         new Vector3(0f, -15f, 0f))
                 })
        {
            GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
                StreetSignRoot + "/" + detail.asset,
                detail.name,
                frontage,
                detail.position,
                detail.size,
                detail.euler,
                false,
                false);
            ConfigureBackdropRenderer(instance, false);
        }

        // Preserve the authored hierarchy for inspection, but submit the entire
        // distant row as one renderer. It is collider-free backdrop geometry and
        // never needs per-building visibility or animation at runtime.
        CombineStaticMeshRenderers(
            depth,
            "Story04_NeighborhoodDepthBatch",
            depth.GetComponentsInChildren<MeshRenderer>(true));
    }

    private static GameObject BuildStoryStreetLamp(
        Transform parent,
        Vector3 position,
        StoryChapterBuilderCommon.Materials materials)
    {
        // The imported SM_Prop_Streetlamp mesh read as a thick grey table lamp
        // after being fitted into a tall target box.  Build a deliberate outdoor
        // silhouette instead: grounded foot, three-metre pole, road-facing arm,
        // enclosed head and a renderer-free light below the lens.
        Transform root = StoryChapterBuilderCommon.NewChild(parent, "StoryStreetLamp");
        root.position = position;

        Create04RLocalPrimitive(
            "StreetLampBaseFoot",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0f, 0.09f, 0f),
            new Vector3(0.22f, 0.09f, 0.22f),
            materials.dark);
        Create04RLocalPrimitive(
            "StreetLampBaseCollar",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0f, 0.25f, 0f),
            new Vector3(0.12f, 0.08f, 0.12f),
            materials.metal);
        Create04RLocalPrimitive(
            "StreetLampPole",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0f, 1.55f, 0f),
            new Vector3(0.065f, 1.25f, 0.065f),
            materials.dark);

        GameObject arm = Create04RLocalPrimitive(
            "StreetLampRoadArm",
            PrimitiveType.Cylinder,
            root,
            new Vector3(-0.31f, 2.83f, 0f),
            new Vector3(0.052f, 0.34f, 0.052f),
            materials.dark);
        arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject brace = Create04RLocalPrimitive(
            "StreetLampArmBrace",
            PrimitiveType.Cylinder,
            root,
            new Vector3(-0.2f, 2.69f, 0f),
            new Vector3(0.038f, 0.22f, 0.038f),
            materials.metal);
        brace.transform.localRotation = Quaternion.Euler(0f, 0f, 48f);

        Create04RLocalPrimitive(
            "StreetLampHeadHousing",
            PrimitiveType.Cube,
            root,
            new Vector3(-0.66f, 2.79f, 0f),
            new Vector3(0.3f, 0.13f, 0.25f),
            materials.dark);
        Create04RLocalPrimitive(
            "StreetLampAmberLens",
            PrimitiveType.Sphere,
            root,
            new Vector3(-0.66f, 2.7f, 0f),
            new Vector3(0.2f, 0.075f, 0.16f),
            materials.amber);

        GameObject lightObject = new GameObject("StreetLampLight");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.localPosition = new Vector3(-0.66f, 2.62f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.68f, 0.38f);
        light.intensity = 1.05f;
        light.range = 5.5f;
        light.spotAngle = 78f;
        light.innerSpotAngle = 42f;
        light.shadows = LightShadows.None;
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return root.gameObject;
    }

    private static void BuildStreetQuakeAftermath(
        Transform dressing,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform aftermath = StoryChapterBuilderCommon.NewChild(dressing, "StreetQuakeAftermath");
        // Damage stays along the building edge and opposite pavement. The playable
        // pavement and the assembly lawn remain clear, so the scene reads as an
        // earthquake response without making the designated safe area unsafe.
        StoryAuthoredPropFactory.CreateDebrisCluster(
            "FacadeEdgeDebris",
            aftermath,
            new Vector3(-2.9f, 0.02f, 24.8f),
            new Vector3(1.3f, 0.38f, 1.1f),
            new Vector3(0f, 22f, 0f),
            materials.concrete,
            materials.cream,
            false);
        StoryAuthoredPropFactory.CreateDebrisCluster(
            "StreetQuakeMasonry_Facade",
            aftermath,
            new Vector3(-2.65f, 0.025f, 22.15f),
            new Vector3(1.25f, 0.34f, 0.8f),
            new Vector3(0f, -16f, 0f),
            materials.concrete,
            materials.cream,
            false);
        StoryAuthoredPropFactory.CreateDebrisCluster(
            "StreetQuakeMasonry_Shops",
            aftermath,
            new Vector3(-4.35f, 0.025f, 31.0f),
            new Vector3(1.15f, 0.3f, 0.78f),
            new Vector3(0f, 28f, 0f),
            materials.concrete,
            materials.cream,
            false);
        StoryAuthoredPropFactory.CreateDebrisCluster(
            "StreetQuakeMasonry_EastCurb",
            aftermath,
            new Vector3(9.15f, 0.025f, 33.0f),
            new Vector3(0.9f, 0.24f, 0.62f),
            new Vector3(0f, -24f, 0f),
            materials.concrete,
            materials.cream,
            false);
        StoryAuthoredPropFactory.CreateGlassShards(
            "StreetQuakeShopWindowGlass",
            aftermath,
            new Vector3(-4.2f, 0.035f, 32.05f),
            new Vector3(1.05f, 0.08f, 0.72f),
            materials.glass,
            8,
            false);

        foreach ((string name, Vector3 position, Vector3 size, float roll) crack in new[]
                 {
                     ("FacadeWindowCrack_Main", new Vector3(-2.15f, 2.3f, 21.225f),
                         new Vector3(0.045f, 0.82f, 0.025f), 17f),
                     ("FacadeWindowCrack_BranchA", new Vector3(-2.02f, 2.48f, 21.228f),
                         new Vector3(0.04f, 0.38f, 0.025f), -38f),
                     ("FacadeWindowCrack_BranchB", new Vector3(-2.31f, 2.12f, 21.228f),
                         new Vector3(0.04f, 0.42f, 0.025f), 52f),
                     ("FacadePlasterCrack_Main", new Vector3(2.98f, 1.45f, 21.205f),
                         new Vector3(0.05f, 0.92f, 0.025f), -13f),
                     ("FacadePlasterCrack_Branch", new Vector3(2.84f, 1.68f, 21.208f),
                         new Vector3(0.04f, 0.48f, 0.025f), 44f)
                 })
            StoryChapterBuilderCommon.CreatePrimitive(
                crack.name,
                PrimitiveType.Cube,
                crack.position,
                crack.size,
                materials.navy,
                aftermath,
                false,
                Quaternion.Euler(0f, 0f, crack.roll));

        GameObject cordon = StoryChapterBuilderCommon.CreatePrimitive(
            "EmergencyCordonTape",
            PrimitiveType.Cube,
            new Vector3(2.25f, 0.58f, 30.15f),
            new Vector3(0.055f, 0.055f, 5.05f),
            materials.amber,
            aftermath,
            false);
        ConfigureBackdropRenderer(cordon, false);

        CombineStaticMeshRenderers(
            aftermath,
            "Story04_QuakeAftermathBatch",
            aftermath.GetComponentsInChildren<MeshRenderer>(true));
    }

    private static void BuildStreetNaturePatches(
        Transform dressing,
        StoryChapterBuilderCommon.Materials materials)
    {
        Material grassMaterial = GetOrCreateParkGrassMaterial();
        (Vector3 position, float yaw, bool leafy)[] vergeGrass =
        {
            (new Vector3(-5.25f, 0.08f, 24.9f), -18f, true),
            (new Vector3(-5.55f, 0.08f, 27.9f), 16f, false),
            (new Vector3(-5.15f, 0.08f, 31.2f), -24f, true),
            (new Vector3(-5.6f, 0.08f, 35.5f), 28f, false),
            (new Vector3(-6.9f, 0.08f, 37.4f), -14f, true),
            (new Vector3(9.35f, 0.08f, 41.2f), 22f, false),
            (new Vector3(9.55f, 0.08f, 45.0f), -26f, true),
            (new Vector3(9.35f, 0.08f, 49.7f), 12f, false)
        };
        for (int index = 0; index < vergeGrass.Length; index++)
        {
            (Vector3 position, float yaw, bool leafy) clump = vergeGrass[index];
            InstantiateNatureAsset(
                clump.leafy ? "grass_leafsLarge.fbx" : "grass_large.fbx",
                "StreetGrassClump_" + index.ToString("00"),
                dressing,
                clump.position,
                clump.leafy ? new Vector3(0.68f, 0.46f, 0.68f) : new Vector3(0.76f, 0.4f, 0.76f),
                new Vector3(0f, clump.yaw, 0f),
                grassMaterial);
        }

        foreach ((string asset, string name, Vector3 position, float yaw) plant in new[]
                 {
                     ("plant_flatShort.fbx", "StreetPlant_Facade", new Vector3(-5.1f, 0.08f, 30.3f), 12f),
                     ("plant_bushDetailed.fbx", "StreetPlant_Bench", new Vector3(-5.75f, 0.08f, 37.65f), -18f),
                     ("plant_flatTall.fbx", "StreetPlant_ParkEdge", new Vector3(9.5f, 0.08f, 47.2f), 24f)
                 })
            InstantiateNatureAsset(
                plant.asset,
                plant.name,
                dressing,
                plant.position,
                new Vector3(0.7f, 0.52f, 0.7f),
                new Vector3(0f, plant.yaw, 0f),
                grassMaterial);
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
        Transform assemblyLawn = routeRoot.Find("AssemblyGrass");
        if (assemblyLawn != null)
        {
            assemblyLawn.name = "AssemblyParkLawn";
            assemblyLawn.position = new Vector3(-6.1f, 0.015f, 46.25f);
            assemblyLawn.localScale = new Vector3(16.8f, 0.05f, 19.5f);
            assemblyLawn.GetComponent<Renderer>().sharedMaterial = GetOrCreateParkGrassMaterial();
            // The legacy AssemblyGrass was visual-only.  Without authored physics
            // geometry the final park disappears from the baked NavMesh after a
            // scene reload, so every assembly approach stops at the curb.
            BoxCollider lawnCollider = assemblyLawn.GetComponent<BoxCollider>();
            if (lawnCollider == null)
                lawnCollider = assemblyLawn.gameObject.AddComponent<BoxCollider>();
            lawnCollider.isTrigger = false;
        }
        GameObject entryWalkway = StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyParkWalkway",
            PrimitiveType.Cube,
            new Vector3(-2.3f, 0.052f, 40.25f),
            new Vector3(4.8f, 0.035f, 3.4f),
            materials.cream,
            set,
            false);
        entryWalkway.GetComponent<Renderer>().enabled = false;
        BuildAssemblyParkLayout(set, materials);
        world.route.assemblySign.transform.position = new Vector3(1.15f, 0.04f, 40.35f);
        // Gizli kaynak nesnenin renderer'ları kapatılır; collider'ı ise görünen
        // levhanın arkasındaki scene-authored dokunma hacmidir. 9:16'da en az
        // 48 px genişlik için yalnız yatay ekseni biraz daha bağışlayıcı tut.
        world.route.assemblySign.transform.localScale = Vector3.Scale(
            world.route.assemblySign.transform.localScale,
            new Vector3(0.82f, 0.68f, 0.68f));
        foreach (Renderer renderer in world.route.assemblySign.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        world.route.assemblyApproachPoint.position = new Vector3(-2.3f, 0.02f, 40.15f);
        world.route.assemblyApproachPoint.rotation = Quaternion.LookRotation(
            new Vector3(-2.3f, 1.0f, 45.8f) - world.route.assemblyApproachPoint.position,
            Vector3.up);
        world.route.neighborAtAssembly.transform.position = new Vector3(-0.35f, 0.02f, 47.25f);
        world.route.whistleWorld.transform.position = new Vector3(-0.62f, 0.97f, 45.72f);
        BuildAssemblyMeetingPointSign(set, materials);
        BuildOpenAssemblyCanopy(set, materials);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "AssemblyWorkerTable",
            set,
            new Vector3(-3.55f, 0.02f, 45.8f),
            new Vector3(2.15f, 0.88f, 1.0f),
            new Vector3(0f, 180f, 0f),
            true);
        BuildAssemblyTableLabels(set, materials);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "AssemblyCareTable",
            set,
            new Vector3(-1.05f, 0.02f, 45.8f),
            new Vector3(2.15f, 0.88f, 1.0f),
            new Vector3(0f, 180f, 0f),
            true);

        world.worker = StoryChapterBuilderCommon.InstantiateCharacter(
            RescueWorkerPrefabPath,
            "AssemblyWorker",
            set,
            new Vector3(-2.3f, 0.02f, 47.35f),
            1.72f,
            adultController);
        world.worker.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Emergency responders use the same Humanoid adult controller as the
        // family cast. Their placement communicates role without extra UI:
        // AFAD owns registration/handoff, police controls the park entrance and
        // the firefighter remains beside the emergency vehicle and hazard lane.
        world.police = StoryChapterBuilderCommon.InstantiateCharacter(
            PolicePrefabPath,
            "AssemblyPolice",
            set,
            new Vector3(1.55f, 0.02f, 41.65f),
            1.76f,
            adultController);
        FaceCharacterTowards(world.police, world.route.assemblyApproachPoint.position);

        world.firefighter = StoryChapterBuilderCommon.InstantiateCharacter(
            FirefighterPrefabPath,
            "EmergencyFirefighter",
            set,
            new Vector3(3.15f, 0.02f, 32.55f),
            1.78f,
            adultController);
        FaceCharacterTowards(world.firefighter, new Vector3(0.2f, 0.02f, 29.1f));

        world.mother = StoryChapterBuilderCommon.InstantiateCharacter(
            AdultPrefabPath,
            "Anne_Assembly_Reunion",
            set,
            new Vector3(-0.65f, 0.02f, 49.45f),
            1.68f,
            adultController);
        world.father = StoryChapterBuilderCommon.InstantiateCharacter(
            AdultPrefabPath,
            "Baba_Assembly_Reunion",
            set,
            new Vector3(0.35f, 0.02f, 49.55f),
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
            new Vector3(-4.12f, 0.95f, 45.62f),
            materials);
        BuildFamilyHeadcountMarks(
            world.checkInClipboard,
            materials,
            out world.familyHeadcountPending,
            out world.familyHeadcountComplete);
        world.treatmentTray = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/Cartoon Kitchen Interior/Cartoon Kitchen Interior/Files/Exports/Plate.fbx",
            "WorkerTreatmentTray_Target",
            set,
            new Vector3(-1.25f, 0.95f, 45.82f),
            new Vector3(0.58f, 0.08f, 0.46f),
            Vector3.zero,
            true,
            true,
            materials.metal);
        world.nerminCupTarget = StoryAuthoredPropFactory.CreateCeramicMug(
            "NerminCup_Target",
            set,
            new Vector3(-0.48f, 0.95f, 45.82f),
            new Vector3(0.22f, 0.26f, 0.22f),
            Vector3.zero,
            materials.cream,
            materials.teal,
            true);

        world.radioPrepared = InstantiateItem(
            "RadioPrepared_Use",
            "Radio.prefab",
            set,
            new Vector3(-3.05f, 0.97f, 45.72f),
            new Vector3(0.55f, 0.36f, 0.32f),
            new Vector3(-90f, -141f, 0f));
        BuildPhysicalRadioPayoff(materials, world);
        world.radioFallback = CreateMegaphone(
            "WorkerMegaphone_Fallback",
            set,
            new Vector3(-3.05f, 1.05f, 45.88f),
            materials);
        BuildWorkerRadioPayoff(materials, world);
        world.firstAidPrepared = StoryAuthoredPropFactory.CreateFirstAidKit(
            "FirstAidPrepared_Drag",
            set,
            new Vector3(-1.82f, 0.98f, 45.6f),
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
            new Vector3(-1.82f, 0.96f, 45.93f),
            new Vector3(0.42f, 0.09f, 0.34f),
            Vector3.zero,
            materials.cream,
            materials.teal,
            true);
        world.waterPrepared = InstantiateItem(
            "WaterPrepared_Drag",
            "Item_Su.prefab",
            set,
            new Vector3(-0.55f, 0.97f, 45.55f),
            new Vector3(0.25f, 0.46f, 0.25f),
            new Vector3(-90f, 0f, 0f));
        world.waterFallback = StoryAuthoredPropFactory.CreateCeramicMug(
            "StationWaterCup_Fallback_Drag",
            set,
            new Vector3(-0.55f, 0.96f, 45.78f),
            new Vector3(0.3f, 0.35f, 0.3f),
            Vector3.zero,
            materials.cream,
            materials.teal);
        world.blanketPrepared = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyBedrollPath,
            "BlanketPrepared_Drag",
            set,
            new Vector3(-4.95f, 1.02f, 48.25f),
            new Vector3(0.76f, 0.24f, 0.3f),
            new Vector3(0f, 10f, 0f),
            false,
            true,
            AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath));
        world.blanketFallback = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Tent.fbx",
            "WindbreakTent_Fallback",
            set,
            new Vector3(0.2f, 0.02f, 47.6f),
            new Vector3(2.0f, 1.7f, 1.35f),
            new Vector3(0f, 180f, 0f),
            true,
            true,
            materials.amber);
        world.contactPrepared = InstantiateItem(
            "ContactCardPrepared_Drag",
            "dockument.prefab",
            set,
            new Vector3(-3.72f, 0.96f, 45.88f),
            new Vector3(0.4f, 0.08f, 0.52f),
            new Vector3(0f, -6f, 0f));
        world.contactFallback = CreatePencil(
            "RegistryPencil_Fallback",
            set,
            new Vector3(-3.72f, 0.96f, 45.58f),
            materials);

        world.familyCallCard = CreateFamilyCallCard(
            "FamilyCallCard_Fallback",
            set,
            new Vector3(0.02f, 0.95f, 45.86f),
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
                     ("AssemblyCanopyRoof_Left", new Vector3(-3.55f, 2.92f, 47.15f), -7f),
                     ("AssemblyCanopyRoof_Right", new Vector3(-1.05f, 2.92f, 47.15f), 7f)
                 })
            StoryChapterBuilderCommon.CreatePrimitive(
                roof.name,
                PrimitiveType.Cube,
                roof.position,
                new Vector3(2.65f, 0.12f, 3.0f),
                materials.teal,
                set,
                false,
                Quaternion.Euler(0f, 0f, roof.roll));

        foreach (Vector3 position in new[]
                 {
                     new Vector3(-4.85f, 1.42f, 45.68f),
                     new Vector3(0.25f, 1.42f, 45.68f),
                     new Vector3(-4.85f, 1.42f, 48.62f),
                     new Vector3(0.25f, 1.42f, 48.62f)
                 })
            StoryChapterBuilderCommon.CreatePrimitive(
                "AssemblyCanopyPost",
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.055f, 1.42f, 0.055f),
                materials.metal,
                set,
                false);

        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyCanopyHeader",
            PrimitiveType.Cube,
            new Vector3(-2.3f, 2.52f, 45.64f),
            new Vector3(5.05f, 0.36f, 0.1f),
            materials.cream,
            set,
            false);
        StoryChapterBuilderCommon.CreateWorldLabel(
            "AssemblyAidStationLabel",
            "AFET YARDIM NOKTASI",
            new Vector3(-2.15f, 2.52f, 45.51f),
            Vector3.zero,
            1.35f,
            StoryChapterBuilderCommon.Navy,
            set,
            new Vector2(3.85f, 0.4f));

        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyAidCross_Vertical",
            PrimitiveType.Cube,
            new Vector3(-4.43f, 2.52f, 45.5f),
            new Vector3(0.1f, 0.23f, 0.035f),
            materials.coral,
            set,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyAidCross_Horizontal",
            PrimitiveType.Cube,
            new Vector3(-4.43f, 2.52f, 45.49f),
            new Vector3(0.23f, 0.1f, 0.035f),
            materials.coral,
            set,
            false);
    }

    private static void BuildAssemblyParkLayout(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        GameObject centralPromenade = StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyCentralPromenade",
            PrimitiveType.Cube,
            new Vector3(-2.3f, 0.054f, 42.85f),
            new Vector3(3.6f, 0.035f, 5.6f),
            materials.cream,
            set,
            false);
        centralPromenade.GetComponent<Renderer>().enabled = false;
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyAidPlaza",
            PrimitiveType.Cube,
            new Vector3(-2.3f, 0.058f, 47.05f),
            new Vector3(6.2f, 0.04f, 3.2f),
            materials.concrete,
            set,
            false);

        BuildAssemblyTerrainRelief(set, materials);

        foreach ((string asset, string name, Vector3 position, Vector3 size, float yaw) tree in new[]
                 {
                     ("tree_oak.fbx", "AssemblyParkTree_WestFront",
                         new Vector3(-12.65f, 0.02f, 40.4f), new Vector3(3.2f, 4.6f, 3.2f), -12f),
                     ("tree_detailed.fbx", "AssemblyParkTree_WestMiddle",
                         new Vector3(-13.0f, 0.02f, 46.6f), new Vector3(3.4f, 5.0f, 3.4f), 18f),
                     ("tree_tall.fbx", "AssemblyParkTree_WestRear",
                         new Vector3(-12.2f, 0.02f, 52.9f), new Vector3(2.8f, 4.8f, 2.8f), -28f),
                     ("tree_oak.fbx", "AssemblyParkTree_NorthWest",
                         new Vector3(-8.2f, 0.02f, 54.25f), new Vector3(3.0f, 4.35f, 3.0f), 8f),
                     ("tree_small.fbx", "AssemblyParkTree_NorthEast",
                         new Vector3(-4.45f, 0.02f, 54.35f), new Vector3(2.5f, 3.45f, 2.5f), -18f),
                     ("tree_detailed.fbx", "AssemblyParkTree_EastRear",
                         new Vector3(1.0f, 0.02f, 52.8f), new Vector3(2.8f, 4.15f, 2.8f), 22f)
                 })
        {
            GameObject parkTree = InstantiateNatureAsset(
                tree.asset,
                tree.name,
                set,
                tree.position,
                tree.size,
                new Vector3(0f, tree.yaw, 0f),
                null);
            ApplyNatureTreePalette(parkTree, materials.wood, materials.grass);
            ConfigureBackdropRenderer(parkTree, false);
        }

        BuildAssemblyMeadowPlants(set, materials);

        InstantiateTownEnvironment(
            "SM_Prop_ParkBench_01.fbx",
            "AssemblyPromenadeBenchWest",
            set,
            new Vector3(-11.7f, 0f, 46.0f),
            new Vector3(0.85f, 0.9f, 2.2f),
            new Vector3(0f, 90f, 0f));
        InstantiateTownEnvironment(
            "SM_Prop_ParkBench_01.fbx",
            "AssemblyWaitingBench",
            set,
            new Vector3(-6.2f, 0f, 53.0f),
            new Vector3(2.2f, 0.9f, 0.85f),
            Vector3.zero);
    }

    private static void BuildAssemblyTerrainRelief(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach ((string asset, string name, Vector3 position, Vector3 size, float yaw, Material material) patch in
                 new[]
                 {
                     ("ground_pathBendBank.fbx", "AssemblySoilBed_West",
                         new Vector3(-11.65f, 0.045f, 46.9f), new Vector3(3.5f, 0.16f, 3.2f), 24f,
                         materials.wood),
                     ("ground_pathRocks.fbx", "AssemblySoilBed_East",
                         new Vector3(0.15f, 0.045f, 49.9f), new Vector3(3.0f, 0.14f, 2.55f), -18f,
                         materials.wood)
                 })
        {
            InstantiateNatureAsset(
                patch.asset,
                patch.name,
                set,
                patch.position,
                patch.size,
                new Vector3(0f, patch.yaw, 0f),
                patch.material);
        }

        foreach ((string asset, string name, Vector3 position, float yaw, Vector3 size) path in new[]
                 {
                     ("path_stoneEnd.fbx", "AssemblyNaturePath_Entry",
                         new Vector3(-2.15f, 0.055f, 40.25f), -4f, new Vector3(1.15f, 0.07f, 1.0f)),
                     ("path_stone.fbx", "AssemblyNaturePath_Lower",
                         new Vector3(-2.4f, 0.055f, 41.55f), 8f, new Vector3(1.35f, 0.07f, 1.2f)),
                     ("path_stoneCircle.fbx", "AssemblyNaturePath_Middle",
                         new Vector3(-2.15f, 0.055f, 42.85f), -5f, new Vector3(1.4f, 0.07f, 1.25f)),
                     ("path_stoneEnd.fbx", "AssemblyNaturePath_Aid",
                         new Vector3(-2.4f, 0.055f, 44.05f), 176f, new Vector3(1.15f, 0.07f, 1.0f))
                 })
        {
            InstantiateNatureAsset(
                path.asset,
                path.name,
                set,
                path.position,
                path.size,
                new Vector3(0f, path.yaw, 0f),
                materials.concrete);
        }

        // Continue the authored stone path all the way to the curb ramp. This makes the
        // playable route visually continuous from the zebra crossing to the aid canopy.
        Transform connectorSource = StoryChapterBuilderCommon.NewChild(set, "ParkEntryConnectorSource");
        for (int index = 0; index < 4; index++)
            InstantiateNatureAsset(
                "path_stone.fbx",
                "AssemblyEntryConnectorPath_" + index.ToString("00"),
                connectorSource,
                new Vector3(2.05f - index * 1.05f, 0.055f, 40.3f),
                new Vector3(1.05f, 0.07f, 0.92f),
                new Vector3(0f, 90f, 0f),
                materials.concrete);
        CombineStaticMeshRenderers(
            connectorSource,
            "Story04_ParkEntryPathBatch",
            connectorSource.GetComponentsInChildren<MeshRenderer>(true));
    }

    private static void BuildAssemblyMeadowPlants(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        Material grassMaterial = GetOrCreateParkGrassMaterial();
        (Vector3 position, float scale, float yaw, bool leafy)[] clumps =
        {
            (new Vector3(-11.95f, 0.08f, 39.35f), 0.68f, -18f, false),
            (new Vector3(-10.85f, 0.12f, 40.55f), 0.82f, 22f, true),
            (new Vector3(-9.55f, 0.14f, 42.15f), 0.72f, -35f, false),
            (new Vector3(-12.15f, 0.09f, 43.25f), 0.62f, 14f, false),
            (new Vector3(-11.35f, 0.11f, 45.15f), 0.88f, -8f, true),
            (new Vector3(-12.0f, 0.12f, 47.85f), 0.76f, 31f, false),
            (new Vector3(-10.65f, 0.15f, 49.35f), 0.92f, -24f, true),
            (new Vector3(-9.35f, 0.15f, 51.25f), 0.72f, 18f, false),
            (new Vector3(-7.85f, 0.1f, 52.95f), 0.8f, -12f, true),
            (new Vector3(-6.15f, 0.09f, 53.65f), 0.62f, 28f, false),
            (new Vector3(-4.75f, 0.1f, 53.0f), 0.74f, -32f, true),
            (new Vector3(-3.15f, 0.1f, 53.75f), 0.66f, 20f, false),
            (new Vector3(-1.65f, 0.09f, 52.85f), 0.78f, -16f, true),
            (new Vector3(-0.15f, 0.1f, 51.45f), 0.65f, 38f, false),
            (new Vector3(0.75f, 0.09f, 49.15f), 0.84f, -20f, true),
            (new Vector3(0.85f, 0.08f, 46.65f), 0.65f, 12f, false),
            (new Vector3(0.55f, 0.08f, 43.35f), 0.72f, -28f, true),
            (new Vector3(-7.6f, 0.08f, 41.15f), 0.62f, 32f, false),
            (new Vector3(-8.3f, 0.1f, 46.35f), 0.7f, -14f, true),
            (new Vector3(-6.95f, 0.1f, 49.95f), 0.66f, 24f, false)
        };
        for (int index = 0; index < clumps.Length; index++)
        {
            (Vector3 position, float scale, float yaw, bool leafy) clump = clumps[index];
            Vector3 size = clump.leafy
                ? new Vector3(0.86f, 0.58f, 0.86f) * clump.scale
                : new Vector3(0.96f, 0.5f, 0.96f) * clump.scale;
            InstantiateNatureAsset(
                clump.leafy ? "grass_leafsLarge.fbx" : "grass_large.fbx",
                "AssemblyGrassClump_" + index.ToString("00"),
                set,
                clump.position,
                size,
                new Vector3(0f, clump.yaw, 0f),
                grassMaterial);
        }

        foreach ((string asset, string name, Vector3 position, Vector3 size, float yaw) plant in new[]
                 {
                     ("plant_bushDetailed.fbx", "AssemblyMeadowPlant_WestA",
                         new Vector3(-10.15f, 0.1f, 43.5f), new Vector3(0.95f, 0.62f, 0.9f), 12f),
                     ("plant_flatTall.fbx", "AssemblyMeadowPlant_WestB",
                         new Vector3(-11.0f, 0.1f, 48.1f), new Vector3(0.7f, 0.68f, 0.7f), -22f),
                     ("plant_bushDetailed.fbx", "AssemblyMeadowPlant_NorthA",
                         new Vector3(-6.75f, 0.1f, 52.6f), new Vector3(0.85f, 0.56f, 0.82f), 18f),
                     ("plant_flatShort.fbx", "AssemblyMeadowPlant_NorthB",
                         new Vector3(-2.2f, 0.1f, 52.25f), new Vector3(0.72f, 0.48f, 0.72f), -16f),
                     ("plant_bushDetailed.fbx", "AssemblyMeadowPlant_East",
                         new Vector3(0.1f, 0.1f, 48.35f), new Vector3(0.8f, 0.52f, 0.78f), 26f)
                 })
            InstantiateNatureAsset(
                plant.asset,
                plant.name,
                set,
                plant.position,
                plant.size,
                new Vector3(0f, plant.yaw, 0f),
                grassMaterial);

        foreach ((string asset, string name, Vector3 position, Vector3 size, float yaw) rock in new[]
                 {
                     ("rock_smallFlatA.fbx", "AssemblyMeadowRock_WestA",
                         new Vector3(-10.8f, 0.06f, 46.5f), new Vector3(0.58f, 0.2f, 0.46f), 18f),
                     ("rock_smallFlatB.fbx", "AssemblyMeadowRock_WestB",
                         new Vector3(-8.7f, 0.06f, 50.55f), new Vector3(0.48f, 0.17f, 0.42f), -12f),
                     ("rock_smallFlatC.fbx", "AssemblyMeadowRock_North",
                         new Vector3(-4.9f, 0.06f, 52.55f), new Vector3(0.54f, 0.18f, 0.48f), 28f),
                     ("rock_largeC.fbx", "AssemblyMeadowRock_East",
                         new Vector3(0.35f, 0.06f, 50.2f), new Vector3(0.72f, 0.27f, 0.62f), -22f)
                 })
            InstantiateNatureAsset(
                rock.asset,
                rock.name,
                set,
                rock.position,
                rock.size,
                new Vector3(0f, rock.yaw, 0f),
                materials.concrete);
    }

    private static GameObject InstantiateNatureAsset(
        string fileName,
        string name,
        Transform parent,
        Vector3 position,
        Vector3 size,
        Vector3 euler,
        Material materialOverride)
    {
        GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
            KenneyNatureModelRoot + "/" + fileName,
            name,
            parent,
            position,
            size,
            euler,
            false,
            false,
            materialOverride);
        ConfigureBackdropRenderer(instance, false);
        return instance;
    }

    private static void ApplyNatureTreePalette(
        GameObject tree,
        Material bark,
        Material leaves)
    {
        foreach (Renderer renderer in tree.GetComponentsInChildren<Renderer>(true))
        {
            Material[] palette = renderer.sharedMaterials;
            for (int index = 0; index < palette.Length; index++)
            {
                Material source = palette[index];
                Color sourceColor = source != null && source.HasProperty("_Color")
                    ? source.color
                    : Color.green;
                bool foliageColor =
                    sourceColor.g > sourceColor.r * 1.08f ||
                    sourceColor.b > sourceColor.r * 1.08f;
                palette[index] = foliageColor ? leaves : bark;
            }
            renderer.sharedMaterials = palette;
        }
    }

    private static void BuildAssemblyTableLabels(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach ((string rootName, string labelName, string text, Vector3 position) label in new[]
                 {
                     ("AssemblyRegistryTableHeader", "AssemblyRegistryTableLabel", "KAYIT",
                         new Vector3(-3.55f, 0.73f, 45.26f)),
                     ("AssemblyCareTableHeader", "AssemblyCareTableLabel", "İLK YARDIM",
                         new Vector3(-1.05f, 0.73f, 45.26f))
                 })
        {
            StoryChapterBuilderCommon.CreatePrimitive(
                label.rootName,
                PrimitiveType.Cube,
                label.position,
                new Vector3(1.72f, 0.38f, 0.055f),
                materials.navy,
                set,
                false);
            StoryChapterBuilderCommon.CreateWorldLabel(
                label.labelName,
                label.text,
                label.position + new Vector3(0f, 0f, -0.031f),
                Vector3.zero,
                1.05f,
                StoryChapterBuilderCommon.Cream,
                set,
                new Vector2(1.58f, 0.34f));
        }
    }

    private static void BuildAssemblyMeetingPointSign(
        Transform set,
        StoryChapterBuilderCommon.Materials materials)
    {
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyMeetingSignPost_Left",
            PrimitiveType.Cylinder,
            new Vector3(0.78f, 0.48f, 40.35f),
            new Vector3(0.03f, 0.52f, 0.03f),
            materials.metal,
            set,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyMeetingSignPost_Right",
            PrimitiveType.Cylinder,
            new Vector3(1.52f, 0.48f, 40.35f),
            new Vector3(0.03f, 0.52f, 0.03f),
            materials.metal,
            set,
            false);
        StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyMeetingSignPanel",
            PrimitiveType.Cube,
            new Vector3(1.15f, 0.87f, 40.33f),
            new Vector3(0.92f, 0.34f, 0.07f),
            materials.teal,
            set,
            false);
        StoryChapterBuilderCommon.CreateWorldLabel(
            "AssemblyMeetingSignLabel",
            "TOPLANMA ALANI",
            new Vector3(1.15f, 0.87f, 40.28f),
            Vector3.zero,
            0.42f,
            StoryChapterBuilderCommon.Cream,
            set,
            new Vector2(0.82f, 0.26f));
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
        GameObject assemblyBin = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_CTPTrashCan_02.prefab",
            "AssemblyRubbishBin",
            set,
            new Vector3(-8.85f, 0f, 48.0f),
            new Vector3(0.72f, 1f, 0.72f),
            new Vector3(0f, 20f, 0f),
            false,
            false);
        ConfigureBackdropRenderer(assemblyBin, false);
        for (int i = 0; i < 7; i++)
            InstantiateTownEnvironment(
                "SM_Env_Fence_White_Straight_01.fbx",
                "AssemblyRearFence_" + i,
                set,
                new Vector3(-12.8f + i * 2.2f, 0f, 55.65f),
                new Vector3(2.2f, 1.0f, 0.18f),
                Vector3.zero);

        InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "AssemblySupplyCrate_A",
            set,
            new Vector3(-5.25f, 0.02f, 48.15f),
            new Vector3(0.85f, 0.55f, 0.75f),
            new Vector3(0f, 6f, 0f));
        InstantiateTownEnvironment(
            "SM_Prop_CardboardBox_01.fbx",
            "AssemblySupplyCrate_B",
            set,
            new Vector3(-5.2f, 0.52f, 48.2f),
            new Vector3(0.7f, 0.42f, 0.62f),
            new Vector3(0f, -8f, 0f));

        GameObject stationLight = StoryChapterBuilderCommon.CreatePrimitive(
            "AssemblyStationLight",
            PrimitiveType.Sphere,
            new Vector3(-2.3f, 2.78f, 47.15f),
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

    private static void ConfigureAuthoredAmbientCharacter(GameObject character)
    {
        foreach (Animator animator in character.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    private readonly struct AmbientVertexKey : IEquatable<AmbientVertexKey>
    {
        private readonly int x;
        private readonly int y;
        private readonly int z;
        private readonly int normalBin;

        internal AmbientVertexKey(int x, int y, int z, int normalBin)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.normalBin = normalBin;
        }

        public bool Equals(AmbientVertexKey other) =>
            x == other.x && y == other.y && z == other.z && normalBin == other.normalBin;

        public override bool Equals(object obj) => obj is AmbientVertexKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x;
                hash = hash * 397 ^ y;
                hash = hash * 397 ^ z;
                hash = hash * 397 ^ normalBin;
                return hash;
            }
        }
    }

    private static void ReplaceWithStaticAmbientProxy(
        GameObject character,
        string assetStem,
        int gridResolution)
    {
        SkinnedMeshRenderer skinned = character.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned == null || skinned.sharedMesh == null)
            throw new InvalidOperationException(character.name + " ambient mesh could not be baked.");

        Animator animator = character.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        Mesh baked = new Mesh { name = assetStem + "_Baked" };
        skinned.BakeMesh(baked, false);
        Mesh simplified = CreateClusteredAmbientMesh(baked, gridResolution);
        Object.DestroyImmediate(baked);

        if (!AssetDatabase.IsValidFolder(AmbientMeshFolder))
            AssetDatabase.CreateFolder("Assets/Story/Generated", "Meshes");
        string meshPath = AmbientMeshFolder + "/" + assetStem + ".asset";
        AssetDatabase.DeleteAsset(meshPath);
        simplified.name = assetStem;
        AssetDatabase.CreateAsset(simplified, meshPath);

        GameObject proxy = new GameObject(assetStem + "_LowDetailMesh");
        // BakeMesh already contains the visual hierarchy's scale. Parent the proxy at
        // the character root and preserve only the rendered mesh's world pose; keeping
        // it under the scaled Visual node would apply the import scale twice.
        Vector3 renderedPosition = skinned.transform.position;
        Quaternion renderedRotation = skinned.transform.rotation;
        proxy.transform.SetParent(character.transform.parent, false);
        proxy.transform.position = renderedPosition;
        proxy.transform.rotation = renderedRotation;
        proxy.transform.localScale = Vector3.one;
        proxy.isStatic = true;
        proxy.AddComponent<MeshFilter>().sharedMesh = simplified;
        MeshRenderer renderer = proxy.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = skinned.sharedMaterials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        float groundOffset = character.transform.position.y + 0.002f - renderer.bounds.min.y;
        proxy.transform.position += Vector3.up * groundOffset;

        skinned.enabled = false;
        if (animator != null)
            animator.enabled = false;
    }

    private static Mesh CreateClusteredAmbientMesh(Mesh source, int gridResolution)
    {
        Vector3[] sourceVertices = source.vertices;
        Vector3[] sourceNormals = source.normals;
        Vector2[] sourceUv = source.uv;
        Bounds bounds = source.bounds;
        float largestAxis = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        float cellSize = Mathf.Max(largestAxis / Mathf.Max(gridResolution, 8), 0.0001f);

        Dictionary<AmbientVertexKey, int> clusters = new Dictionary<AmbientVertexKey, int>();
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> counts = new List<int>();
        int[] remap = new int[sourceVertices.Length];

        for (int index = 0; index < sourceVertices.Length; index++)
        {
            Vector3 position = sourceVertices[index];
            Vector3 normal = sourceNormals.Length == sourceVertices.Length
                ? sourceNormals[index]
                : Vector3.up;
            Vector2 texture = sourceUv.Length == sourceVertices.Length
                ? sourceUv[index]
                : Vector2.zero;
            AmbientVertexKey key = new AmbientVertexKey(
                Mathf.FloorToInt((position.x - bounds.min.x) / cellSize),
                Mathf.FloorToInt((position.y - bounds.min.y) / cellSize),
                Mathf.FloorToInt((position.z - bounds.min.z) / cellSize),
                AmbientNormalBin(normal));
            if (!clusters.TryGetValue(key, out int clusterIndex))
            {
                clusterIndex = positions.Count;
                clusters.Add(key, clusterIndex);
                positions.Add(Vector3.zero);
                normals.Add(Vector3.zero);
                uv.Add(Vector2.zero);
                counts.Add(0);
            }

            positions[clusterIndex] += position;
            normals[clusterIndex] += normal;
            uv[clusterIndex] += texture;
            counts[clusterIndex]++;
            remap[index] = clusterIndex;
        }

        for (int index = 0; index < positions.Count; index++)
        {
            float inverse = 1f / Mathf.Max(counts[index], 1);
            positions[index] *= inverse;
            normals[index] = (normals[index] * inverse).normalized;
            uv[index] *= inverse;
        }

        List<int>[] subMeshTriangles = new List<int>[source.subMeshCount];
        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            int[] sourceTriangles = source.GetTriangles(subMesh);
            List<int> triangles = new List<int>(sourceTriangles.Length);
            for (int index = 0; index + 2 < sourceTriangles.Length; index += 3)
            {
                int a = remap[sourceTriangles[index]];
                int b = remap[sourceTriangles[index + 1]];
                int c = remap[sourceTriangles[index + 2]];
                if (a == b || b == c || c == a)
                    continue;
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
            subMeshTriangles[subMesh] = triangles;
        }

        Mesh result = new Mesh
        {
            name = source.name + "_AmbientLOD",
            indexFormat = positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        result.SetVertices(positions);
        result.SetNormals(normals);
        result.SetUVs(0, uv);
        result.subMeshCount = source.subMeshCount;
        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            result.SetTriangles(subMeshTriangles[subMesh], subMesh, false);
        result.RecalculateBounds();
        if (uv.Count == positions.Count)
            result.RecalculateTangents();
        MeshUtility.Optimize(result);
        return result;
    }

    private static int AmbientNormalBin(Vector3 normal)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            return normal.x >= 0f ? 0 : 1;
        if (absolute.y >= absolute.x && absolute.y >= absolute.z)
            return normal.y >= 0f ? 2 : 3;
        return normal.z >= 0f ? 4 : 5;
    }

    private static GameObject CombineStaticMeshRenderers(
        Transform parent,
        string batchName,
        IEnumerable<MeshRenderer> sourceRenderers)
    {
        MeshRenderer[] sources = sourceRenderers
            .Where(renderer => renderer != null && renderer.enabled &&
                               renderer.GetComponent<MeshFilter>()?.sharedMesh != null)
            .Distinct()
            .ToArray();
        if (sources.Length == 0)
            return null;

        Dictionary<Material, List<CombineInstance>> byMaterial =
            new Dictionary<Material, List<CombineInstance>>();
        List<Material> materialOrder = new List<Material>();
        Matrix4x4 worldToParent = parent.worldToLocalMatrix;
        foreach (MeshRenderer source in sources)
        {
            Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
            Material[] sourceMaterials = source.sharedMaterials;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                Material material = sourceMaterials.Length > 0
                    ? sourceMaterials[Mathf.Min(subMesh, sourceMaterials.Length - 1)]
                    : null;
                if (material == null)
                    continue;
                if (!byMaterial.TryGetValue(material, out List<CombineInstance> materialInstances))
                {
                    materialInstances = new List<CombineInstance>();
                    byMaterial.Add(material, materialInstances);
                    materialOrder.Add(material);
                }
                materialInstances.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMesh,
                    transform = worldToParent * source.transform.localToWorldMatrix
                });
            }
        }

        List<Mesh> materialMeshes = new List<Mesh>();
        List<CombineInstance> finalInstances = new List<CombineInstance>();
        foreach (Material material in materialOrder)
        {
            Mesh materialMesh = new Mesh
            {
                name = batchName + "_" + material.name,
                indexFormat = IndexFormat.UInt32
            };
            materialMesh.CombineMeshes(byMaterial[material].ToArray(), true, true, false);
            materialMeshes.Add(materialMesh);
            finalInstances.Add(new CombineInstance
            {
                mesh = materialMesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            });
        }

        Mesh combined = new Mesh
        {
            name = batchName,
            indexFormat = IndexFormat.UInt32
        };
        combined.CombineMeshes(finalInstances.ToArray(), false, false, false);
        combined.RecalculateBounds();
        MeshUtility.Optimize(combined);
        foreach (Mesh temporary in materialMeshes)
            Object.DestroyImmediate(temporary);

        if (!AssetDatabase.IsValidFolder(AmbientMeshFolder))
            AssetDatabase.CreateFolder("Assets/Story/Generated", "Meshes");
        string assetPath = AmbientMeshFolder + "/" + batchName + ".asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(combined, assetPath);

        GameObject batch = new GameObject(batchName);
        batch.transform.SetParent(parent, false);
        batch.isStatic = true;
        batch.AddComponent<MeshFilter>().sharedMesh = combined;
        MeshRenderer batchRenderer = batch.AddComponent<MeshRenderer>();
        batchRenderer.sharedMaterials = materialOrder.ToArray();
        ConfigureBackdropRenderer(batch, false);
        foreach (MeshRenderer source in sources)
            source.enabled = false;
        return batch;
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
        // Alt sahanlık varışı merdivenin merkezindeki açık zemindir. Yardım
        // aşamasındaki bütün taşınabilir eşyalar sağ duvar şeridine alınmıştır;
        // böylece bu hedefin ışını sonraki aşamanın collider'larını delmez.
        Transform lowerPoint = Point(points, "R04_LowerLandingPoint", new Vector3(-0.24f, 0.02f, 17.22f),
            world.route.neighborAtLanding.transform.position);
        Transform neighborPoint = Point(points, "R04_NeighborPoint", new Vector3(-0.3f, 0.02f, 18.0f),
            world.route.neighborAtLanding.transform.position + Vector3.up);
        Transform exitPoint = Point(points, "R04_ExitPoint", new Vector3(0f, 0.02f, 20.0f),
            world.route.buildingDoorClosed.transform.position);
        Transform facadePoint = Point(points, "R04_FacadePoint", world.facadeSafePoint.transform.position,
            new Vector3(0f, 1.2f, 21.1f));
        // Cepheden çıkar çıkmaz oyuncunun ayak dibinde kalan eski hedef, yüksek
        // takip kamerasının altına düşüyordu. Aynı açık kaldırım üzerindeki bu
        // ileri nokta hem gerçek bir yürüyüş üretir hem hedefi portrede gösterir.
        Transform streetPoint = Point(points, "R04_StreetPoint", new Vector3(0.65f, 0.02f, 29.2f),
            world.route.streetHazard.transform.position + Vector3.up);
        Transform gasLeakPoint = Point(points, "R04_GasLeakPoint", new Vector3(-0.55f, 0.02f, 27.0f),
            world.gasLeakHazard.transform.position + Vector3.up);
        Transform assemblyPoint = Point(points, "R04_AssemblyPoint", new Vector3(-2.3f, 0.02f, 40.15f),
            new Vector3(-2.3f, 1.0f, 45.8f));
        // Keep the approach point clear of Nermin's baked body collider.  The old
        // point sat only a few centimetres in front of her and therefore had no
        // walkable NavMesh sample even though it looked like open plaza space.
        Transform reunionPoint = Point(points, "R04_FamilyReunionPoint", new Vector3(-1.55f, 0.02f, 48.3f),
            world.mother.transform.position + Vector3.up);

        result.navigationAnchors = new[]
        {
            corridorPoint, stairsPoint, elevatorPoint, upperPoint, lowerPoint,
            neighborPoint, exitPoint, facadePoint, streetPoint, gasLeakPoint,
            assemblyPoint, reunionPoint
        };

        // Landing floor and sidewalk meshes are several metres wide. Making the
        // whole floor the interaction collider meant most of the target was
        // necessarily outside a portrait screen. These scene-authored trigger
        // pads represent the exact standing spot without changing the visible
        // environment or adding runtime targeting code.
        GameObject upperLandingTarget = Create04RStageHotspot(
            points, "R04_UpperLandingHotspot", upperPoint.position, new Vector3(0.82f, 0.18f, 0.82f));
        GameObject lowerLandingTarget = Create04RStageHotspot(
            points, "R04_LowerLandingHotspot", lowerPoint.position, new Vector3(0.9f, 0.18f, 0.9f));
        GameObject safeSidewalkTarget = Create04RStageHotspot(
            points, "R04_SafeSidewalkHotspot", streetPoint.position, new Vector3(1.35f, 1.2f, 1.35f));

        result.inspectCorridor = Add04RInteraction(world.route.corridorThreshold,
            "evac.r04.corridor.inspect", "Kapı eşiğini geçip koridoru dinle", StoryInteractionKind.Inspect,
            corridorPoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationCorridor);
        Renderer corridorThresholdRenderer = world.route.corridorThreshold.GetComponent<Renderer>();
        if (corridorThresholdRenderer != null)
            corridorThresholdRenderer.enabled = false;
        BoxCollider corridorThresholdCollider = world.route.corridorThreshold.GetComponent<BoxCollider>();
        if (corridorThresholdCollider != null)
        {
            Vector3 corridorTriggerSize = corridorThresholdCollider.size;
            // Görsel eşik yalnız 8 cm yüksekliğinde olduğu için ayak tabanına değen
            // trigger fizik adımına göre kaçabiliyordu. Hacmi koridor tarafına doğru
            // büyüt; başlangıç noktası dışarıda, NavMesh varış noktası içeride kalsın.
            corridorTriggerSize.y = Mathf.Max(corridorTriggerSize.y, 20f);
            corridorTriggerSize.z = Mathf.Max(corridorTriggerSize.z, 2f);
            corridorThresholdCollider.size = corridorTriggerSize;
            Vector3 corridorTriggerCenter = corridorThresholdCollider.center;
            corridorTriggerCenter.z = Mathf.Min(corridorTriggerCenter.z, -0.35f);
            corridorThresholdCollider.center = corridorTriggerCenter;
            corridorThresholdCollider.isTrigger = true;
        }
        SerializedObject corridorTrigger = new SerializedObject(result.inspectCorridor);
        corridorTrigger.FindProperty("autoTriggerOnPlayerEnter").boolValue = true;
        corridorTrigger.ApplyModifiedPropertiesWithoutUndo();
        result.chooseStairs = Add04RInteraction(world.route.stairDoorClosed,
            "evac.r04.route.stairs", "Merdiven kapısını yana çek", StoryInteractionKind.Exit, stairsPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationStairDoor, false, 1, 1.1f, 2f);
        result.tryElevator = Add04RInteraction(world.route.elevatorButton,
            "evac.r04.route.elevator_unsafe", "Asansör çağrı düğmesini yokla", StoryInteractionKind.UnsafeChoice,
            elevatorPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationElevator, false, 1, 0.55f, 2f);
        result.reachUpperLanding = Add04RInteraction(upperLandingTarget,
            "evac.r04.stairs.upper", "İlk sahanlığa kontrollü in", StoryInteractionKind.Exit, upperPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationLanding, false, 1, 1.4f, 2.4f);
        result.holdHandrail = Add04RInteraction(world.route.handrail,
            "evac.r04.aftershock.handrail", "Korkuluğun üzerinde basılı tut", StoryInteractionKind.TakeCover,
            upperPoint, StoryInteractionGesture.WorldHold, StoryCameraZoneId.EvacuationLanding, false, 1, 1.8f, 2.2f);
        result.reachLowerLanding = Add04RInteraction(lowerLandingTarget,
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
            facadePoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationBuildingFront, false, 1, 1.2f, 0.65f);
        result.inspectStreetHazard = Add04RInteraction(world.gasLeakInspectionSource,
            "evac.r04.street.inspect", "Hasarlı boruyu güvenli mesafeden incele", StoryInteractionKind.Inspect,
            gasLeakPoint, StoryInteractionGesture.Tap,
            StoryCameraZoneId.EvacuationStreetInspect, false, 1, 0.7f, 3f);
        result.takeSafeSidewalk = Add04RInteraction(safeSidewalkTarget,
            "evac.r04.street.safe", "Gaz kaçağından uzak açık kaldırımdan ilerle", StoryInteractionKind.Exit,
            streetPoint, StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationStreet, false, 1, 1.4f, 3f);
        result.tryUnsafeShortcut = Add04RInteraction(world.gasLeakUnsafeSource,
            "evac.r04.street.gas_unsafe", "Tıslayan hasarlı gaz borusuna yaklaş", StoryInteractionKind.UnsafeChoice,
            gasLeakPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationHazard, false, 1, 0.6f, 3f);

        result.readAssemblySign = Add04RInteraction(world.route.assemblySign,
            "evac.r04.assembly.sign", "Toplanma levhasını doğrula", StoryInteractionKind.Inspect,
            // Levha alan girişindedir ve Assembly kamerasının ARKASINDA kalır;
            // sokak yolculuğu kadrajı levhayı gerçekten gösterir.
            assemblyPoint, StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationStreet, false, 1, 0.75f, 3f);
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
            StoryInteractionKind.HelpSibling, StoryCameraZoneId.EvacuationAssemblyRadio,
            family.can.transform, new Vector3(1.0f, 1.35f, 0.9f));
        result.moveToWindbreak = Add04RInteraction(world.blanketFallback,
            "evac.r04.assembly.windbreak_fallback", "Can'la rüzgâr kesen tenteye geç",
            StoryInteractionKind.HelpSibling, assemblyPoint, StoryInteractionGesture.Approach,
            StoryCameraZoneId.EvacuationAssembly, false, 1, 1.1f, 3.2f);
        result.useContactCard = Add04RDrag(world.contactPrepared, world.checkInClipboard,
            "evac.r04.assembly.contact", "Aile iletişim kartını kayıt panosuna sürükle",
            StoryInteractionKind.Collect, StoryCameraZoneId.EvacuationAssemblyRadio, points,
            new Vector3(0.8f, 0.4f, 0.75f));
        result.useRegistrySheet = Add04RInteraction(world.contactFallback,
            "evac.r04.assembly.registry_fallback", "Kayıt kalemini sayfa üzerinde çek",
            StoryInteractionKind.Inspect, assemblyPoint, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationAssemblyRadio, false, 1, 1f, 3.2f);
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

        // Toplanma alanı bir merkez sahnedir ve alan navmesh'i kısmî: yaklaşma
        // yürüyüşü bazı istasyonlarda hiç tamamlanamıyor, dokunuşlar sessizce
        // ölüyordu. Bölüm 1'deki gibi tüm istasyonlar doğrudan dokunuşla çalışır;
        // kamera odak bölgeleri zaten her istasyonu kadrajlıyor.
        foreach (StoryInteractable hubInteraction in new[]
                 {
                     result.readAssemblySign, result.handNeighborToWorker, result.useRadio,
                     result.listenWorkerRadio, result.useFirstAid, result.useStationCloth,
                     result.giveWater, result.useWaterStation, result.giveBlanket,
                     result.moveToWindbreak, result.useContactCard, result.useRegistrySheet,
                     result.reuniteFamily
                 })
        {
            SerializedObject hubData = new SerializedObject(hubInteraction);
            hubData.FindProperty("interactFromAnywhere").boolValue = true;
            hubData.ApplyModifiedPropertiesWithoutUndo();
        }

        // StoryCall uses the grounded child talking motion; the generic Waving
        // source is deliberately not bound because it inverts this rig's ankle.
        // These remain scene-authored persistent event bindings.
        UnityEventTools.AddStringPersistentListener(
            result.inspectCorridor.OnInteracted,
            family.canAnimator.SetTrigger,
            "StoryCall");
        UnityEventTools.AddStringPersistentListener(
            result.inspectStreetHazard.OnInteracted,
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

        // Drag colliders are added above, so convert the final physical volumes
        // into carving blockers here.  Their child colliders still resolve to the
        // parent StoryInteractable/DraggableItem, while agents can no longer step
        // over a box, foam pad or cane on the landing.
        foreach (GameObject landingProp in new[]
                 {
                     world.cardboardBlocking, world.cardboardCleared,
                     world.route.debrisBlocking, world.route.debrisCleared,
                     world.route.caneBlocked, world.route.caneReachable
                 })
            StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(landingProp);
        return result;
    }

    private static GameObject Create04RStageHotspot(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 size)
    {
        GameObject hotspot = new GameObject(name);
        hotspot.transform.SetParent(parent);
        hotspot.transform.position = position + Vector3.up * (size.y * 0.5f);
        BoxCollider collider = hotspot.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        NavMeshModifier modifier = hotspot.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        return hotspot;
    }

    private static void SnapAndValidateRebuildRoutes(
        StoryChapterBuilderCommon.Characters family,
        RebuildWorld world,
        RebuildInteractions interactions)
    {
        Snap04Anchor(family.deniz.transform, "Deniz başlangıcı", 2f);
        Snap04Anchor(family.can.transform, "Can başlangıcı", 2f);
        foreach (Transform anchor in interactions.navigationAnchors)
            Snap04Anchor(anchor, anchor.name, 2.5f);
        Snap04Anchor(world.route.outsideStandPoint, "bina dışı başlangıcı", 2.5f);
        Snap04Anchor(world.route.assemblyApproachPoint, "toplanma yaklaşımı", 2.5f);

        Transform[] route = interactions.navigationAnchors;
        Validate04Route(family.deniz.transform, route[0], "başlangıç → koridor");
        Validate04Route(route[0], route[1], "koridor → merdiven kapısı");
        // The next story stage begins after the stair door interaction has
        // deactivated its carving blocker. Validate that stage in the same state;
        // checking through the still-closed door produces a false partial path.
        bool stairDoorWasActive = world.route.stairDoorClosed.activeSelf;
        NavMeshSurface navSurface = world.route.environment.GetComponent<NavMeshSurface>();
        world.route.stairDoorClosed.SetActive(false);
        // NavMeshObstacle carving is applied by the native navigation update. The
        // synchronous builder cannot yield a frame, so re-add the baked data after
        // disabling the blocker to validate the exact post-door stage immediately.
        navSurface.RemoveData();
        navSurface.AddData();
        foreach (NavMeshLink link in world.route.environment.GetComponentsInChildren<NavMeshLink>(true))
            link.UpdateLink();
        try
        {
            Validate04Route(route[1], route[3], "merdiven kapısı → üst sahanlık");
            Validate04Route(route[3], route[4], "üst sahanlık → alt sahanlık");
            Validate04Route(route[4], route[5], "alt sahanlık → komşu");
            Validate04Route(route[5], route[6], "komşu → bina çıkışı");
        }
        finally
        {
            world.route.stairDoorClosed.SetActive(stairDoorWasActive);
            navSurface.RemoveData();
            navSurface.AddData();
            foreach (NavMeshLink link in world.route.environment.GetComponentsInChildren<NavMeshLink>(true))
                link.UpdateLink();
        }
        // Exterior validation must mirror the story state after the building
        // door has been opened. It also starts at the preceding indoor exit
        // point, otherwise two individually valid islands can masquerade as a
        // complete evacuation route.
        bool buildingDoorWasActive = world.route.buildingDoorClosed.activeSelf;
        world.route.buildingDoorClosed.SetActive(false);
        Refresh04Navigation(navSurface, world.route.environment.transform);
        try
        {
            Snap04Anchor(world.route.outsideStandPoint, "bina dışı başlangıcı", 0.8f);
            for (int index = 7; index < route.Length; index++)
                Snap04Anchor(route[index], route[index].name, 0.8f);
            Snap04Anchor(world.route.assemblyApproachPoint, "toplanma yaklaşımı", 0.8f);

            Validate04Route(route[6], world.route.outsideStandPoint, "bina çıkışı → dış başlangıç");
            Validate04Route(world.route.outsideStandPoint, route[7], "bina dışı → cepheden uzaklaşma");
            Validate04Route(route[7], route[9], "cephe → gaz kaçağı inceleme");
            Validate04Route(route[7], route[8], "cephe → güvenli kaldırım");
            Validate04Route(route[8], route[10], "güvenli kaldırım → toplanma alanı");
            Validate04Route(world.route.assemblyApproachPoint, route[11], "toplanma girişi → aile buluşması");
        }
        finally
        {
            world.route.buildingDoorClosed.SetActive(buildingDoorWasActive);
            Refresh04Navigation(navSurface, world.route.environment.transform);
        }
    }

    private static void Refresh04Navigation(NavMeshSurface surface, Transform environment)
    {
        surface.RemoveData();
        surface.AddData();
        foreach (NavMeshLink link in environment.GetComponentsInChildren<NavMeshLink>(true))
            link.UpdateLink();
    }

    private static void Snap04Anchor(Transform anchor, string label, float radius)
    {
        if (anchor == null || !NavMesh.SamplePosition(anchor.position, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            throw new InvalidOperationException(
                $"Story 04 '{label}' anchor'ı baked NavMesh üzerinde değil: " +
                (anchor != null ? anchor.position.ToString() : "<null>"));
        }
        anchor.position = hit.position;
    }

    private static void Validate04Route(Transform from, Transform to, string label)
    {
        NavMeshPath path = new NavMeshPath();
        bool complete = NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) &&
                        path.status == NavMeshPathStatus.PathComplete;
        if (!complete)
        {
            string corners = path.corners != null && path.corners.Length > 0
                ? string.Join(" → ", path.corners.Select(point => point.ToString()))
                : "<köşe yok>";
            string stairSamples = label == "merdiven kapısı → üst sahanlık"
                ? "; samples=" + Describe04UpperStairSamples()
                : string.Empty;
            throw new InvalidOperationException(
                $"Story 04 baked NavMesh rotası yarım: {label}; " +
                $"status={path.status}, from={from.position}, to={to.position}, corners={corners}{stairSamples}");
        }
    }

    private static string Describe04UpperStairSamples()
    {
        List<string> samples = new List<string>();
        for (float z = -1f; z <= 8.01f; z += 0.5f)
        {
            float expectedY = z < 0f ? 4.05f : 4.12f - 0.267f * z;
            bool found = NavMesh.SamplePosition(
                new Vector3(0f, expectedY, z),
                out NavMeshHit hit,
                0.48f,
                NavMesh.AllAreas);
            samples.Add(found ? $"{z:F1}:{hit.position}" : $"{z:F1}:X");
        }
        return string.Join(" | ", samples);
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

    private static void FaceCharacterTowards(GameObject character, Vector3 target)
    {
        if (character == null)
            return;

        Vector3 direction = target - character.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            character.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
        foreach (string labelName in new[] { "SafeRouteLabel", "VoiceSignalLabel" })
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
                    camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 60f,
                    camera.name + " lens aralığı");

            Require04(
                root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true).Length == 1,
                "tek NavMeshSurface");
            Require04(
                root.GetComponentInChildren<Unity.AI.Navigation.NavMeshSurface>(true).navMeshData != null,
                "baked NavMesh");
            Require04(Find04(root, "AssemblyWorker") != null, "toplanma görevlisi");
            Require04(
                UsesHumanoidResponderPrefab(root, "AssemblyWorker", RescueWorkerPrefabPath),
                "toplanma görevlisinin AFAD Humanoid prefabı olması");
            Require04(
                UsesHumanoidResponderPrefab(root, "AssemblyPolice", PolicePrefabPath),
                "toplanma alanı girişinde polis Humanoid prefabı");
            Require04(
                UsesHumanoidResponderPrefab(root, "EmergencyFirefighter", FirefighterPrefabPath),
                "itfaiye aracının yanında itfaiyeci Humanoid prefabı");
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

    private static bool UsesHumanoidResponderPrefab(GameObject root, string objectName, string prefabPath)
    {
        Transform responder = Find04(root, objectName);
        if (responder == null)
            return false;

        string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(responder.gameObject);
        Animator animator = responder.GetComponentInChildren<Animator>(true);
        return sourcePath == prefabPath &&
               animator != null &&
               animator.avatar != null &&
               animator.avatar.isValid &&
               animator.avatar.isHuman;
    }

    private static void Require04(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("Story_04 rebuild doğrulaması başarısız: " + label);
    }
}
