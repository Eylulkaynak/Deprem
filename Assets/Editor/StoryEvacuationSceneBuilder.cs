using System;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class StoryEvacuationSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_04_Evacuation.unity";
    private const string LegacyPath = "Assets/Scenes/Bolum4.unity";
    private const string BackupPath = "Assets/Scenes/LegacyBackups/Bolum4_OriginalGameplay_2026-07-17.unity";
    private const string DoorPath = "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Door2.fbx";
    private const string ElevatorPath = "Assets/asansör/SM_Prop_Elevator_Enterance_01.fbx";
    private const string EmergencyLampPath =
        "Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Light_05.prefab";
    private const string StreetSignRoot =
        "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";

    private sealed class EvacuationWorld
    {
        internal GameObject environment;
        internal GameObject flashlightBeam;
        internal GameObject emergencyLightRoute;
        internal GameObject corridorThreshold;
        internal GameObject stairDoorClosed;
        internal GameObject stairDoorOpen;
        internal GameObject elevatorButton;
        internal Animation stairDoorAnimation;
        internal Animation elevatorAnimation;
        internal AudioSource elevatorAudio;
        internal GameObject upperLanding;
        internal GameObject handrail;
        internal GameObject lowerLanding;
        internal CinemachineImpulseSource aftershockImpulse;
        internal ParticleSystem aftershockDust;
        internal AudioSource aftershockAudio;
        internal GameObject neighborAtLanding;
        internal GameObject neighborAtStreet;
        internal GameObject neighborAtAssembly;
        internal Animator neighborAnimator;
        internal GameObject neighborSupportHand;
        internal GameObject caneBlocked;
        internal GameObject caneReachable;
        internal GameObject debrisBlocking;
        internal GameObject debrisCleared;
        internal Animation caneAnimation;
        internal Animation debrisAnimation;
        internal Animation neighborRiseAnimation;
        internal GameObject buildingDoorClosed;
        internal GameObject buildingDoorOpen;
        internal Transform outsideStandPoint;
        internal Animation buildingDoorAnimation;
        internal GameObject streetHazard;
        internal GameObject safeSidewalk;
        internal GameObject unsafeShortcut;
        internal Transform assemblyApproachPoint;
        internal Animation unsafeShortcutAnimation;
        internal ParticleSystem streetDust;
        internal GameObject assemblySign;
        internal GameObject whistleWorld;
        internal GameObject voiceSignalWorld;
    }

    private sealed class EvacuationInteractions
    {
        internal StoryInteractable inspectCorridor;
        internal StoryInteractable chooseStairs;
        internal StoryInteractable tryElevator;
        internal StoryInteractable reachUpperLanding;
        internal StoryInteractable holdHandrail;
        internal StoryInteractable reachLowerLanding;
        internal StoryInteractable callNeighbor;
        internal StoryInteractable moveNeighborCane;
        internal StoryInteractable clearLightDebris;
        internal StoryInteractable guideNeighbor;
        internal StoryInteractable openBuildingExit;
        internal StoryInteractable inspectStreetHazard;
        internal StoryInteractable takeSafeSidewalk;
        internal StoryInteractable tryUnsafeShortcut;
        internal StoryInteractable readAssemblySign;
        internal StoryInteractable checkCan;
        internal StoryInteractable checkNeighbor;
        internal StoryInteractable useWhistle;
        internal StoryInteractable callFamily;
    }

    [MenuItem("Tools/Deprem Story/Build Story_04_Evacuation")]
    public static void BuildFromMenu() => Build(true);

    [MenuItem("Tools/Deprem Story/Build Story_04_Evacuation (Silent)")]
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
            GameObject root = new GameObject("STORY_04_EVACUATION");
            EvacuationWorld world = BuildWorld(root.transform, materials, adultController);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(root.transform, controller, false,
                new Vector3(-0.45f, 4.03f, -4.4f), new Vector3(0.65f, 4.03f, -4.75f), Vector3.zero);

            world.flashlightBeam.transform.SetParent(family.deniz.transform, true);
            world.flashlightBeam.transform.localPosition = new Vector3(0f, 0.9f, 0.18f);
            world.flashlightBeam.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

            GameObject sessionObject = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(gameManager, StoryAct.Evacuation,
                new[]
                {
                    StoryFlag.BagReady, StoryFlag.BagFlashlight, StoryFlag.BagWhistle,
                    StoryFlag.WardrobeSecured, StoryFlag.ShelfSecured, StoryFlag.ExitCleared
                });

            GameObject core = new GameObject("_StoryEvacuationCore");
            core.transform.SetParent(root.transform);
            StoryEvacuationDirector director = core.AddComponent<StoryEvacuationDirector>();
            StoryPlayerMovement movement = StoryChapterBuilderCommon.ConfigurePlayer(family.deniz);
            StorySiblingFollower follower = StoryChapterBuilderCommon.ConfigureSibling(family.can, family.deniz.transform);

            StoryChapterBuilderCommon.CameraSpec[] cameraSpecs =
            {
                new(StoryCameraZoneId.EvacuationCorridor, "CM_Evac_Corridor", new Vector3(1.9f, 6.3f, -5.2f),
                    new Vector3(0f, 4.03f, -0.65f), 44f, true),
                new(StoryCameraZoneId.EvacuationElevator, "CM_Evac_Elevator", new Vector3(-1.65f, 6.8f, -5.55f),
                    new Vector3(2.25f, 5.05f, -2.55f), 48f, true),
                new(StoryCameraZoneId.EvacuationStairDoor, "CM_Evac_StairDoor", new Vector3(1.8f, 5.85f, -3.0f),
                    new Vector3(0f, 5.05f, 0.0f), 40f, true),
                new(StoryCameraZoneId.EvacuationStairsTop, "CM_Evac_StairsTop", new Vector3(1.9f, 6.4f, -1.6f),
                    new Vector3(0f, 3.2f, 3.9f), 42f, true),
                new(StoryCameraZoneId.EvacuationLanding, "CM_Evac_Landing", new Vector3(-1.9f, 4.8f, 2.2f),
                    new Vector3(0.7f, 2.35f, 8.15f), 46f, true),
                new(StoryCameraZoneId.EvacuationLowerLanding, "CM_Evac_LowerLanding", new Vector3(1.9f, 2.6f, 13.6f),
                    new Vector3(0f, 0.65f, 18.0f), 40f, true),
                new(StoryCameraZoneId.EvacuationNeighbor, "CM_Evac_Neighbor", new Vector3(2.0f, 3.4f, 10.5f),
                    new Vector3(-0.65f, 0.75f, 18.0f), 48f, true),
                new(StoryCameraZoneId.EvacuationBuildingDoor, "CM_Evac_BuildingDoor", new Vector3(2.0f, 3.0f, 17.0f),
                    new Vector3(0f, 1.1f, 21.18f), 40f, true),
                new(StoryCameraZoneId.EvacuationBuildingFront, "CM_Evac_BuildingFront", new Vector3(0f, 4.5f, 32.5f),
                    new Vector3(0f, 4.0f, 20.9f), 45f, true),
                new(StoryCameraZoneId.EvacuationStreetInspect, "CM_Evac_StreetInspect", new Vector3(5.4f, 2.7f, 23.0f),
                    new Vector3(-1.05f, 0.7f, 27.7f), 42f, true),
                new(StoryCameraZoneId.EvacuationStreet, "CM_Evac_Street", new Vector3(6.8f, 4.25f, 25.75f),
                    new Vector3(0f, 0.95f, 32.6f), 43f, true, family.deniz.transform, 13.5f, new Vector2(-0.08f, 0.1f)),
                new(StoryCameraZoneId.EvacuationAssembly, "CM_Evac_Assembly", new Vector3(5.2f, 2.9f, 36.85f),
                    new Vector3(0f, 1.05f, 41.75f), 41f, true),
                new(StoryCameraZoneId.EvacuationHazard, "CM_Evac_Hazard", new Vector3(-4.65f, 2.2f, 25.7f),
                    new Vector3(-1.2f, 0.7f, 29.15f), 38f, true)
            };
            StoryCameraController cameraController = StoryChapterBuilderCommon.BuildCameras(root.transform,
                StoryCameraZoneId.EvacuationCorridor, cameraSpecs, out Camera mainCamera, out CinemachineBrain brain);
            StoryChapterBuilderCommon.ChapterUI ui = StoryChapterBuilderCommon.BuildUI(root.transform, cameraController,
                "StoryUI_Evacuation", "4. PERDE • TAHLİYE", "TOPLANMA ALANINA ULAŞILDI",
                "Asansör kullanılmadı; artçıda duruldu ve bina cephelerinden uzak açık rota izlendi.");

            StoryChapterBuilderCommon.SetReference(ui.controller, "movementOwner", movement);
            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            StoryChapterBuilderCommon.SetReference(touch, "worldCamera", mainCamera);
            StoryChapterBuilderCommon.SetReference(touch, "player", movement);
            StoryChapterBuilderCommon.SetReference(touch, "ui", ui.controller);
            StoryChapterBuilderCommon.SetReference(touch, "cameraController", cameraController);
            SerializedObject touchSerialized = new SerializedObject(touch);
            touchSerialized.FindProperty("directWorldGestures").boolValue = true;
            touchSerialized.ApplyModifiedPropertiesWithoutUndo();

            EvacuationInteractions interactions = BuildInteractions(world, family, director);
            ConfigureDirector(director, gameManager, movement, touch, cameraController, ui, family, follower, world,
                interactions);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            StoryChapterBuilderCommon.SetReference(ui.controller, "cameraController", cameraController);

            StoryChapterBuilderCommon.BuildLighting(root.transform, volume, new Color(0.78f, 0.88f, 1f), 1.05f);
            AudioClip ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(StoryChapterBuilderCommon.AudioRoot + "/calm_home.wav");
            if (ambience != null)
                StoryChapterBuilderCommon.CreateAudioSource("DistantCityAmbience", root.transform, ambience, 0.075f, true, true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            StoryChapterBuilderCommon.BuildNavigation(world.environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            StoryChapterBuilderCommon.AddScenesToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeGameObject = root;
            Debug.Log("Story_04_Evacuation built successfully: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_04_Evacuation oynanabilir hikâye sahnesi üretildi.", "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_04 üretilemedi:\n" + exception.Message, "Tamam");
            throw;
        }
    }

    private static EvacuationWorld BuildWorld(Transform parent, StoryChapterBuilderCommon.Materials m,
        RuntimeAnimatorController controller)
    {
        EvacuationWorld world = new EvacuationWorld();
        world.environment = new GameObject("EvacuationEnvironment");
        world.environment.transform.SetParent(parent);
        Transform route = StoryChapterBuilderCommon.NewChild(world.environment.transform, "ContinuousEvacuationRoute");
        BuildCorridor(route, m, world);
        BuildStairwell(route, m, world);
        BuildLowerLanding(route, m, controller, world);
        BuildExterior(route, m, world);
        BuildPreparedLighting(route, m, world);
        BuildEffects(route, m, world);
        return world;
    }

    private static void BuildCorridor(Transform route, StoryChapterBuilderCommon.Materials m, EvacuationWorld world)
    {
        StoryChapterBuilderCommon.CreatePrimitive("UpperCorridorFloor", PrimitiveType.Cube, new Vector3(0f, 3.9f, -3.0f),
            new Vector3(5f, 0.2f, 6.2f), m.concrete, route);
        StoryChapterBuilderCommon.CreatePrimitive("UpperCorridorLeftWall", PrimitiveType.Cube,
            new Vector3(-2.5f, 5.35f, -3f), new Vector3(0.18f, 2.9f, 6.2f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("UpperCorridorRightWall", PrimitiveType.Cube,
            new Vector3(2.5f, 5.35f, -4.25f), new Vector3(0.18f, 2.9f, 3.7f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("UpperCorridorBackWall", PrimitiveType.Cube,
            new Vector3(0f, 5.35f, -6.05f), new Vector3(5f, 2.9f, 0.18f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("StairDoorWall_Left", PrimitiveType.Cube,
            new Vector3(-1.75f, 5.35f, 0.08f), new Vector3(1.5f, 2.9f, 0.18f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("StairDoorWall_Right", PrimitiveType.Cube,
            new Vector3(1.75f, 5.35f, 0.08f), new Vector3(1.5f, 2.9f, 0.18f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("StairDoorWall_Lintel", PrimitiveType.Cube,
            new Vector3(0f, 6.53f, 0.08f), new Vector3(2f, 0.54f, 0.18f), m.wall, route);
        world.corridorThreshold = StoryChapterBuilderCommon.CreatePrimitive("CorridorSafetyThreshold", PrimitiveType.Cube,
            new Vector3(0f, 4.03f, -0.65f), new Vector3(3.5f, 0.08f, 0.72f), m.amber, route, true);

        GameObject elevatorFrame = StoryChapterBuilderCommon.InstantiateAsset(
            ElevatorPath, "ElevatorFrame", route, new Vector3(2.15f, 3.94f, -2.55f),
            new Vector3(0.5f, 2.5f, 2.25f), new Vector3(0f, 90f, 0f), true, true);
        // The source elevator mesh is almost black from the corridor angle. Authored panels and
        // trim keep the unsafe elevator choice visually legible without adding runtime setup.
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorDoorPanel_A", PrimitiveType.Cube,
            new Vector3(2.32f, 5.04f, -2.98f), new Vector3(0.12f, 2.18f, 0.78f),
            m.metal, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorDoorPanel_B", PrimitiveType.Cube,
            new Vector3(2.32f, 5.04f, -2.12f), new Vector3(0.12f, 2.18f, 0.78f),
            m.metal, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorDoorSplit", PrimitiveType.Cube,
            new Vector3(2.25f, 5.04f, -2.55f), new Vector3(0.035f, 2.18f, 0.055f),
            m.navy, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorHeader", PrimitiveType.Cube,
            new Vector3(2.28f, 6.2f, -2.55f), new Vector3(0.16f, 0.14f, 1.86f),
            m.navy, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorCallPlate", PrimitiveType.Cube,
            new Vector3(2.3f, 5.05f, -1.34f), new Vector3(0.14f, 0.42f, 0.24f),
            m.navy, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("ElevatorCallLight", PrimitiveType.Sphere,
            new Vector3(2.21f, 5.05f, -1.34f), new Vector3(0.09f, 0.09f, 0.09f),
            m.coral, route, false);
        world.elevatorButton = StoryChapterBuilderCommon.CreatePrimitive("ElevatorCallButton", PrimitiveType.Cube,
            new Vector3(2.1f, 5.05f, -1.22f), new Vector3(0.13f, 0.38f, 0.24f), m.coral, route, true);
        world.elevatorButton.GetComponent<Renderer>().enabled = false;
        StoryChapterBuilderCommon.CreateWorldLabel("ElevatorLabel", "ASANSÖR", new Vector3(2.08f, 6.35f, -2.55f),
            new Vector3(0f, -90f, 0f), 2.3f, StoryChapterBuilderCommon.Cream, route, new Vector2(1.8f, 0.4f));
        world.elevatorAnimation = StoryChapterBuilderCommon.CreateRockAnimation(elevatorFrame,
            "Evac_ElevatorPowerDrop", 1.6f, 0.55f);

        world.stairDoorClosed = StoryChapterBuilderCommon.InstantiateAsset(
            DoorPath, "StairDoor_Closed", route, new Vector3(0f, 4f, 0.02f),
            new Vector3(1.85f, 2.24f, 0.2f), Vector3.zero, true, true);
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(world.stairDoorClosed);
        world.stairDoorOpen = StoryChapterBuilderCommon.InstantiateAsset(
            DoorPath, "StairDoor_Open", route, new Vector3(-0.82f, 4f, 0.82f),
            new Vector3(0.2f, 2.24f, 1.85f), new Vector3(0f, -92f, 0f), true, true);
        world.stairDoorAnimation = StoryChapterBuilderCommon.CreateRotationAnimation(world.stairDoorOpen,
            "Evac_StairDoorOpen", Vector3.zero, new Vector3(0f, -92f, 0f), 0.68f);
        world.stairDoorOpen.SetActive(false);
    }

    private static void BuildStairwell(Transform route, StoryChapterBuilderCommon.Materials m, EvacuationWorld world)
    {
        Quaternion slope = Quaternion.Euler(15.5f, 0f, 0f);
        GameObject upperNavRamp = StoryChapterBuilderCommon.CreatePrimitive(
            "UpperNavRamp", PrimitiveType.Cube, new Vector3(0f, 3.0f, 3.65f),
            new Vector3(3.25f, 0.22f, 7.55f), m.concrete, route, true, slope);
        upperNavRamp.GetComponent<Renderer>().enabled = false;
        for (int i = 0; i < 13; i++)
        {
            float t = i / 12f;
            float z = Mathf.Lerp(0.25f, 7.05f, t);
            float y = Mathf.Lerp(3.95f, 2.05f, t);
            StoryChapterBuilderCommon.CreatePrimitive("UpperVisibleStep_" + i, PrimitiveType.Cube,
                new Vector3(0f, y - 0.06f, z), new Vector3(3.15f, 0.12f, 0.5f), m.cream, route, false);
        }

        world.upperLanding = StoryChapterBuilderCommon.CreatePrimitive("UpperLandingFloor", PrimitiveType.Cube,
            new Vector3(0f, 1.9f, 8.25f), new Vector3(5f, 0.2f, 2.45f), m.amber, route, true);
        world.handrail = StoryChapterBuilderCommon.CreatePrimitive("UpperLandingHandrail", PrimitiveType.Cylinder,
            new Vector3(1.72f, 2.95f, 8.1f), new Vector3(0.07f, 1.2f, 0.07f), m.teal, route, true,
            Quaternion.Euler(90f, 0f, 0f));
        StoryChapterBuilderCommon.CreatePrimitive("HandrailPostA", PrimitiveType.Cylinder,
            new Vector3(1.72f, 2.45f, 7.1f), new Vector3(0.06f, 0.55f, 0.06f), m.metal, route);
        StoryChapterBuilderCommon.CreatePrimitive("HandrailPostB", PrimitiveType.Cylinder,
            new Vector3(1.72f, 2.45f, 9.1f), new Vector3(0.06f, 0.55f, 0.06f), m.metal, route);

        GameObject lowerNavRamp = StoryChapterBuilderCommon.CreatePrimitive(
            "LowerNavRamp", PrimitiveType.Cube, new Vector3(0f, 1.0f, 12.85f),
            new Vector3(3.25f, 0.22f, 7.55f), m.concrete, route, true, slope);
        lowerNavRamp.GetComponent<Renderer>().enabled = false;
        for (int i = 0; i < 13; i++)
        {
            float t = i / 12f;
            float z = Mathf.Lerp(9.45f, 16.25f, t);
            float y = Mathf.Lerp(1.95f, 0.05f, t);
            StoryChapterBuilderCommon.CreatePrimitive("LowerVisibleStep_" + i, PrimitiveType.Cube,
                new Vector3(0f, y - 0.06f, z), new Vector3(3.15f, 0.12f, 0.5f), m.cream, route, false);
        }
        StoryChapterBuilderCommon.CreatePrimitive("StairwellLeftWall", PrimitiveType.Cube,
            new Vector3(-2.5f, 2.6f, 8.2f), new Vector3(0.18f, 5.2f, 16.5f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("StairwellRightLowWall", PrimitiveType.Cube,
            new Vector3(2.5f, 1.1f, 11.8f), new Vector3(0.18f, 2.2f, 9.2f), m.wall, route);
    }

    private static void BuildLowerLanding(Transform route, StoryChapterBuilderCommon.Materials m,
        RuntimeAnimatorController controller, EvacuationWorld world)
    {
        world.lowerLanding = StoryChapterBuilderCommon.CreatePrimitive("LowerLandingFloor", PrimitiveType.Cube,
            new Vector3(0f, -0.1f, 18.7f), new Vector3(5f, 0.2f, 5.0f), m.concrete, route, true);
        StoryChapterBuilderCommon.CreatePrimitive("LowerLandingLeftWall", PrimitiveType.Cube,
            new Vector3(-2.5f, 1.35f, 18.7f), new Vector3(0.18f, 2.9f, 5f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("LowerLandingRightWall", PrimitiveType.Cube,
            new Vector3(2.5f, 1.35f, 18.7f), new Vector3(0.18f, 2.9f, 5f), m.wall, route);

        world.neighborAtLanding = StoryChapterBuilderCommon.InstantiateCharacter(
            StoryChapterBuilderCommon.StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab", "Nermin_Neighbor_Landing",
            route, new Vector3(-0.8f, 0.02f, 18.35f), 1.58f, controller);
        world.neighborAnimator = world.neighborAtLanding.GetComponentInChildren<Animator>(true);
        world.neighborAtStreet = StoryChapterBuilderCommon.InstantiateCharacter(
            StoryChapterBuilderCommon.StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab", "Nermin_Neighbor_Street",
            route, new Vector3(-0.8f, 0.02f, 24.65f), 1.58f, controller);
        world.neighborAtAssembly = StoryChapterBuilderCommon.InstantiateCharacter(
            StoryChapterBuilderCommon.StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab", "Nermin_Neighbor_Assembly",
            route, new Vector3(-1.25f, 0.02f, 41.1f), 1.58f, controller);
        world.neighborAtStreet.SetActive(false);
        world.neighborAtAssembly.SetActive(false);
        world.neighborSupportHand = StoryChapterBuilderCommon.CreatePrimitive("NeighborSupportHand", PrimitiveType.Sphere,
            new Vector3(-0.25f, 0.95f, 18.18f), new Vector3(0.18f, 0.18f, 0.18f), m.amber,
            world.neighborAtLanding.transform, true);
        world.neighborSupportHand.GetComponent<Renderer>().enabled = false;

        world.caneBlocked = StoryAuthoredPropFactory.CreateWalkingCane(
            "NeighborCane_Blocked", route, new Vector3(-1.88f, 0.02f, 17.55f),
            new Vector3(0.85f, 1.15f, 0.22f), new Vector3(0f, 0f, 58f), m.wood, m.dark);
        world.caneReachable = StoryAuthoredPropFactory.CreateWalkingCane(
            "NeighborCane_Reachable", route, new Vector3(-0.9f, 0.02f, 18.05f),
            new Vector3(0.34f, 1.15f, 0.22f), new Vector3(0f, 0f, 12f), m.teal, m.dark);
        world.caneReachable.SetActive(false);

        world.debrisBlocking = StoryAuthoredPropFactory.CreateDebrisCluster(
            "LightDebris_Blocking", route, new Vector3(0.62f, 0.02f, 18.12f),
            new Vector3(1.55f, 0.5f, 1.15f), new Vector3(0f, 18f, 0f), m.coral, m.cream);
        world.debrisCleared = StoryAuthoredPropFactory.CreateDebrisCluster(
            "LightDebris_Cleared", route, new Vector3(1.72f, 0.02f, 19.35f),
            new Vector3(1.35f, 0.46f, 1.0f), new Vector3(0f, -12f, 0f), m.teal, m.cream);
        world.debrisCleared.SetActive(false);

        world.caneAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(world.caneReachable,
            "Evac_CaneToNeighbor", world.caneBlocked.transform.localPosition - world.caneReachable.transform.localPosition, 0.62f);
        world.debrisAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(world.debrisCleared,
            "Evac_DebrisCleared", world.debrisBlocking.transform.localPosition - world.debrisCleared.transform.localPosition, 0.62f);
        world.neighborRiseAnimation = StoryChapterBuilderCommon.CreateMoveAnimation(world.neighborAtLanding,
            "Evac_NeighborRise", new Vector3(0f, -0.42f, 0f), 0.8f);

        world.buildingDoorClosed = StoryChapterBuilderCommon.InstantiateAsset(
            DoorPath, "BuildingExitDoor_Closed", route, new Vector3(0f, 0f, 21.18f),
            new Vector3(1.9f, 2.24f, 0.2f), Vector3.zero, true, true);
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(world.buildingDoorClosed);
        world.buildingDoorOpen = StoryChapterBuilderCommon.InstantiateAsset(
            DoorPath, "BuildingExitDoor_Open", route, new Vector3(-0.84f, 0f, 22f),
            new Vector3(0.2f, 2.24f, 1.9f), new Vector3(0f, -92f, 0f), true, true);
        world.buildingDoorAnimation = StoryChapterBuilderCommon.CreateRotationAnimation(world.buildingDoorOpen,
            "Evac_BuildingDoorOpen", Vector3.zero, new Vector3(0f, -92f, 0f), 0.68f);
        world.buildingDoorOpen.SetActive(false);
        world.outsideStandPoint = StoryChapterBuilderCommon.CreatePoint("OutsideStandPoint", route,
            new Vector3(0f, 0.02f, 24.0f), new Vector3(0f, 1f, 21.2f));
    }

    private static void BuildExterior(Transform route, StoryChapterBuilderCommon.Materials m, EvacuationWorld world)
    {
        StoryChapterBuilderCommon.CreatePrimitive("OutdoorGround", PrimitiveType.Cube, new Vector3(0f, -0.12f, 33.0f),
            new Vector3(11f, 0.24f, 24f), m.concrete, route);
        StoryChapterBuilderCommon.CreatePrimitive("EmergencyVehicleRoad", PrimitiveType.Cube,
            new Vector3(3.9f, 0.015f, 33.0f), new Vector3(3.0f, 0.05f, 22f), m.asphalt, route, false);
        for (int i = 0; i < 8; i++)
            StoryChapterBuilderCommon.CreatePrimitive("RoadDash_" + i, PrimitiveType.Cube,
                new Vector3(3.9f, 0.05f, 23.5f + i * 2.8f), new Vector3(0.12f, 0.04f, 1.15f), m.cream, route, false);

        // Cephe tek parça olursa görsel kapı açılsa bile NavMesh ve fizik duvarın
        // arkasında kalır. Gerçek kapı boşluğunu sol/sağ dikme ve lento ile kur.
        StoryChapterBuilderCommon.CreatePrimitive("BuildingFacade_Left", PrimitiveType.Cube,
            new Vector3(-2.38f, 4.4f, 20.85f), new Vector3(2.45f, 8.8f, 0.55f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("BuildingFacade_Right", PrimitiveType.Cube,
            new Vector3(2.38f, 4.4f, 20.85f), new Vector3(2.45f, 8.8f, 0.55f), m.wall, route);
        StoryChapterBuilderCommon.CreatePrimitive("BuildingFacade_Lintel", PrimitiveType.Cube,
            new Vector3(0f, 5.65f, 20.85f), new Vector3(2.3f, 6.3f, 0.55f), m.wall, route);
        for (int floor = 0; floor < 3; floor++)
        {
            StoryChapterBuilderCommon.CreatePrimitive("FacadeWindowL_" + floor, PrimitiveType.Cube,
                new Vector3(-2.15f, 2.3f + floor * 2.1f, 21.16f), new Vector3(1.2f, 1.15f, 0.08f), m.glass, route, false);
            StoryChapterBuilderCommon.CreatePrimitive("FacadeWindowR_" + floor, PrimitiveType.Cube,
                new Vector3(2.15f, 2.3f + floor * 2.1f, 21.16f), new Vector3(1.2f, 1.15f, 0.08f), m.glass, route, false);
        }
        StoryChapterBuilderCommon.CreatePrimitive("FacadeEntranceHeader", PrimitiveType.Cube,
            new Vector3(0f, 3.18f, 21.18f), new Vector3(2.5f, 0.28f, 0.18f), m.teal, route, false);
        // Keep the dark foundation trim on the facade, but preserve the actual door
        // opening. A single full-width band previously passed through the door mesh.
        StoryChapterBuilderCommon.CreatePrimitive("FacadeBaseBand_Left", PrimitiveType.Cube,
            new Vector3(-2.38f, 0.42f, 21.18f), new Vector3(2.45f, 0.34f, 0.18f),
            m.navy, route, false);
        StoryChapterBuilderCommon.CreatePrimitive("FacadeBaseBand_Right", PrimitiveType.Cube,
            new Vector3(2.38f, 0.42f, 21.18f), new Vector3(2.45f, 0.34f, 0.18f),
            m.navy, route, false);
        StoryChapterBuilderCommon.CreateWorldLabel("BuildingAddressLabel", "BLOK A",
            new Vector3(0f, 3.58f, 21.22f), new Vector3(0f, 180f, 0f), 1.65f,
            StoryChapterBuilderCommon.Cream, route, new Vector2(1.8f, 0.36f));

        world.streetHazard = new GameObject("StreetGlassAndLooseSign_Hazard");
        world.streetHazard.transform.SetParent(route);
        StoryAuthoredPropFactory.CreateGlassShards(
            "StreetGlassShards", world.streetHazard.transform, new Vector3(-1.25f, 0.03f, 28.15f),
            new Vector3(2.1f, 0.1f, 1.55f), m.glass, 11, true);
        StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_StreetSign_Stop.prefab", "LooseFacadeSign", world.streetHazard.transform,
            new Vector3(-2.5f, 1.3f, 25.9f), new Vector3(2.25f, 2.25f, 0.5f),
            new Vector3(0f, 0f, -12f), true, true);

        world.unsafeShortcut = StoryChapterBuilderCommon.CreatePrimitive("UnsafeGlassShortcut", PrimitiveType.Cube,
            new Vector3(-1.25f, 0.035f, 30.0f), new Vector3(2.3f, 0.07f, 5.2f), m.coral, route, true);
        world.safeSidewalk = StoryChapterBuilderCommon.CreatePrimitive("SafeOpenSidewalk", PrimitiveType.Cube,
            new Vector3(0.72f, 0.045f, 33.0f), new Vector3(2.0f, 0.09f, 14.0f), m.teal, route, true);
        StoryChapterBuilderCommon.CreateWorldLabel("SafeRouteLabel", "AÇIK ROTA", new Vector3(0.72f, 0.12f, 32.8f),
            new Vector3(90f, 0f, 0f), 2.1f, StoryChapterBuilderCommon.Cream, route, new Vector2(2.4f, 0.45f));

        StoryChapterBuilderCommon.CreatePrimitive("AssemblyGrass", PrimitiveType.Cube,
            new Vector3(0f, 0.015f, 42.0f), new Vector3(6.5f, 0.05f, 5.8f), m.grass, route, false);
        world.assemblySign = StoryChapterBuilderCommon.InstantiateAsset(
            StreetSignRoot + "/Prop_StreetSign_Empty.prefab", "AssemblyAreaSign", route,
            new Vector3(0f, 0.04f, 42.7f), new Vector3(2.5f, 2.7f, 0.55f), Vector3.zero,
            true, true, m.teal);
        StoryChapterBuilderCommon.CreateWorldLabel("AssemblyLabel", "AFET TOPLANMA\nALANI", new Vector3(0f, 2.05f, 42.58f),
            Vector3.zero, 2.25f, StoryChapterBuilderCommon.Cream, world.assemblySign.transform, new Vector2(2.4f, 0.8f));
        world.assemblyApproachPoint = StoryChapterBuilderCommon.CreatePoint("AssemblyApproachPoint", route,
            new Vector3(0.35f, 0.02f, 40.3f), new Vector3(0f, 1f, 42.7f));

        world.whistleWorld = BuildWhistle(route, m, new Vector3(1.25f, 0.95f, 41.05f));
        world.voiceSignalWorld = StoryChapterBuilderCommon.CreatePrimitive("VoiceSignalWorld", PrimitiveType.Sphere,
            new Vector3(1.25f, 1.1f, 41.05f), new Vector3(0.28f, 0.28f, 0.28f), m.amber, route, true);
        world.voiceSignalWorld.GetComponent<Renderer>().enabled = false;
        StoryChapterBuilderCommon.CreateWorldLabel("VoiceSignalLabel", "SES", new Vector3(1.25f, 1.55f, 41.05f),
            Vector3.zero, 1.8f, StoryChapterBuilderCommon.Cream, route, new Vector2(1.2f, 0.35f));
        world.unsafeShortcutAnimation = StoryChapterBuilderCommon.CreateRockAnimation(world.streetHazard,
            "Evac_StreetHazardFall", 7f, 0.82f);
        world.streetDust = StoryChapterBuilderCommon.CreateDust("StreetFacadeDust", route,
            new Vector3(-1.1f, 2.0f, 26.1f), m, 28);
    }

    private static void BuildPreparedLighting(Transform route, StoryChapterBuilderCommon.Materials m,
        EvacuationWorld world)
    {
        world.flashlightBeam = new GameObject("DenizFlashlightBeam");
        Light flashlight = world.flashlightBeam.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.color = new Color(1f, 0.92f, 0.72f);
        flashlight.intensity = 5.2f;
        flashlight.range = 9f;
        flashlight.spotAngle = 48f;
        flashlight.innerSpotAngle = 25f;
        flashlight.shadows = LightShadows.Soft;

        world.emergencyLightRoute = new GameObject("WeakEmergencyLightRoute");
        world.emergencyLightRoute.transform.SetParent(route);
        foreach (Vector3 position in new[]
                 {
                     new Vector3(1.9f, 5.8f, -0.6f), new Vector3(-1.8f, 3.7f, 7.8f),
                     new Vector3(1.8f, 1.7f, 16.8f), new Vector3(0f, 2.5f, 20.4f)
                 })
        {
            GameObject lightObject = StoryChapterBuilderCommon.InstantiateAsset(
                EmergencyLampPath, "EmergencyLamp", world.emergencyLightRoute.transform,
                position - Vector3.up * 0.16f, new Vector3(0.34f, 0.34f, 0.34f), Vector3.zero,
                false, false, m.amber);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.63f, 0.28f);
            light.intensity = 1.35f;
            light.range = 4.2f;
            light.shadows = LightShadows.None;
        }
    }

    private static void BuildEffects(Transform route, StoryChapterBuilderCommon.Materials m, EvacuationWorld world)
    {
        GameObject impulseObject = new GameObject("AftershockImpulseSource");
        impulseObject.transform.SetParent(route);
        impulseObject.transform.position = new Vector3(0f, 2.1f, 8.2f);
        world.aftershockImpulse = impulseObject.AddComponent<CinemachineImpulseSource>();
        world.aftershockImpulse.ImpulseDefinition.ImpulseChannel = 1;
        world.aftershockImpulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        world.aftershockImpulse.ImpulseDefinition.ImpulseDuration = 0.72f;
        world.aftershockImpulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        world.aftershockDust = StoryChapterBuilderCommon.CreateDust("AftershockStairDust", route,
            new Vector3(0f, 3.3f, 8.2f), m, 34);
        AudioClip rumble = AssetDatabase.LoadAssetAtPath<AudioClip>(StoryChapterBuilderCommon.AudioRoot + "/quake_rumble.wav");
        AudioClip impact = AssetDatabase.LoadAssetAtPath<AudioClip>(StoryChapterBuilderCommon.AudioRoot + "/quake_impact.wav");
        world.aftershockAudio = StoryChapterBuilderCommon.CreateAudioSource("AftershockAudio", route, rumble, 0.32f, true);
        world.elevatorAudio = StoryChapterBuilderCommon.CreateAudioSource("ElevatorPowerAudio", route, impact, 0.18f);
    }

    private static GameObject BuildWhistle(Transform parent, StoryChapterBuilderCommon.Materials m, Vector3 position)
    {
        return StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Bolum1Prefab/whistle.prefab", "WhistleWorld", parent,
            position - Vector3.up * 0.22f, new Vector3(0.62f, 0.44f, 0.38f),
            new Vector3(0f, 0f, 90f), true, true);
    }

    private static EvacuationInteractions BuildInteractions(EvacuationWorld world,
        StoryChapterBuilderCommon.Characters family, StoryEvacuationDirector director)
    {
        EvacuationInteractions interactions = new EvacuationInteractions();
        Transform points = StoryChapterBuilderCommon.NewChild(world.environment.transform, "EvacuationInteractionPoints");
        Transform corridorPoint = StoryChapterBuilderCommon.CreatePoint("CorridorCheckPoint", points,
            new Vector3(0f, 4.02f, -1.25f), new Vector3(0f, 4.8f, 0f));
        Transform stairsPoint = StoryChapterBuilderCommon.CreatePoint("StairDoorPoint", points,
            new Vector3(0f, 4.02f, -0.8f), world.stairDoorClosed.transform.position);
        Transform elevatorPoint = StoryChapterBuilderCommon.CreatePoint("ElevatorPoint", points,
            new Vector3(1.2f, 4.02f, -2.0f), world.elevatorButton.transform.position);
        Transform upperPoint = StoryChapterBuilderCommon.CreatePoint("UpperLandingPoint", points,
            new Vector3(0f, 2.02f, 8.0f), world.handrail.transform.position);
        Transform lowerPoint = StoryChapterBuilderCommon.CreatePoint("LowerLandingPoint", points,
            new Vector3(0.25f, 0.02f, 17.25f), world.neighborAtLanding.transform.position);
        Transform neighborPoint = StoryChapterBuilderCommon.CreatePoint("NeighborHelpPoint", points,
            new Vector3(0.1f, 0.02f, 18.0f), world.neighborAtLanding.transform.position + Vector3.up);
        Transform exitPoint = StoryChapterBuilderCommon.CreatePoint("BuildingExitPoint", points,
            new Vector3(0f, 0.02f, 20.0f), world.buildingDoorClosed.transform.position);
        Transform streetPoint = StoryChapterBuilderCommon.CreatePoint("StreetInspectPoint", points,
            new Vector3(0.65f, 0.02f, 25.4f), world.streetHazard.transform.position + Vector3.up);
        Transform assemblyPoint = StoryChapterBuilderCommon.CreatePoint("AssemblyCheckPoint", points,
            new Vector3(0.35f, 0.02f, 40.25f), world.assemblySign.transform.position + Vector3.up);

        interactions.inspectCorridor = StoryChapterBuilderCommon.AddInteractable(world.corridorThreshold,
            "evac.corridor.inspect", "KORİDORU DİNLE VE KONTROL ET", StoryInteractionKind.Inspect, corridorPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationCorridor);
        interactions.chooseStairs = StoryChapterBuilderCommon.AddInteractable(world.stairDoorClosed,
            "evac.route.stairs", "MERDİVEN KAPISINI YANA ÇEK", StoryInteractionKind.Exit, stairsPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationStairDoor, false, 1, 1.1f, 2f);
        interactions.tryElevator = StoryChapterBuilderCommon.AddInteractable(world.elevatorButton,
            "evac.route.elevator_unsafe", "ASANSÖR DÜĞMESİNİ YOKLA", StoryInteractionKind.UnsafeChoice, elevatorPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationElevator, false, 1, 0.55f, 2f);
        interactions.reachUpperLanding = StoryChapterBuilderCommon.AddInteractable(world.upperLanding,
            "evac.stairs.upper_landing", "İLK SAHANLIĞA KONTROLLÜ İN", StoryInteractionKind.Exit, upperPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationLanding, false, 1, 1.4f, 2.2f);
        interactions.holdHandrail = StoryChapterBuilderCommon.AddInteractable(world.handrail,
            "evac.aftershock.handrail", "KORKULUKTA BASILI TUT", StoryInteractionKind.TakeCover, upperPoint,
            StoryInteractionGesture.WorldHold, StoryCameraZoneId.EvacuationLanding, false, 1, 1.4f, 2.1f);
        interactions.reachLowerLanding = StoryChapterBuilderCommon.AddInteractable(world.lowerLanding,
            "evac.stairs.lower_landing", "ALT SAHANLIĞA KONTROLLÜ İN", StoryInteractionKind.Exit, lowerPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationLowerLanding, false, 1, 1.4f, 2.4f);
        interactions.callNeighbor = StoryChapterBuilderCommon.AddInteractable(world.neighborAtLanding,
            "evac.neighbor.call", "NERMİN TEYZEYE SESLEN", StoryInteractionKind.HelpSibling, neighborPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationNeighbor, false, 1, 0.7f, 2.1f);
        interactions.moveNeighborCane = StoryChapterBuilderCommon.AddInteractable(world.caneBlocked,
            "evac.neighbor.cane", "BASTONU UZANABİLECEĞİ YERE ÇEK", StoryInteractionKind.HelpSibling, neighborPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationNeighbor, false, 1, 0.95f, 2.2f);
        interactions.clearLightDebris = StoryChapterBuilderCommon.AddInteractable(world.debrisBlocking,
            "evac.neighbor.light_debris", "HAFİF PARÇAYI KENARA İT", StoryInteractionKind.HelpSibling, neighborPoint,
            StoryInteractionGesture.SwipeDown, StoryCameraZoneId.EvacuationNeighbor, false, 1, 0.95f, 2.2f);
        interactions.guideNeighbor = StoryChapterBuilderCommon.AddInteractable(world.neighborSupportHand,
            "evac.neighbor.support", "YANINDA BASILI TUTARAK DESTEK OL", StoryInteractionKind.HelpSibling, neighborPoint,
            StoryInteractionGesture.WorldHold, StoryCameraZoneId.EvacuationNeighbor, false, 1, 1.35f, 2.1f);
        interactions.openBuildingExit = StoryChapterBuilderCommon.AddInteractable(world.buildingDoorClosed,
            "evac.exit.building", "DIŞ KAPIYI YANA ÇEK", StoryInteractionKind.Exit, exitPoint,
            StoryInteractionGesture.SwipeHorizontal, StoryCameraZoneId.EvacuationBuildingDoor, false, 1, 1.1f, 2.1f);
        interactions.inspectStreetHazard = StoryChapterBuilderCommon.AddInteractable(world.streetHazard,
            "evac.street.inspect", "CAM VE CEPHE RİSKİNİ İNCELE", StoryInteractionKind.Inspect, streetPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationStreetInspect, false, 1, 0.8f, 3f);
        interactions.takeSafeSidewalk = StoryChapterBuilderCommon.AddInteractable(world.safeSidewalk,
            "evac.street.safe_route", "AÇIK YAN KALDIRIMDAN İLERLE", StoryInteractionKind.Exit, streetPoint,
            StoryInteractionGesture.Approach, StoryCameraZoneId.EvacuationStreet, false, 1, 1.4f, 3f);
        interactions.tryUnsafeShortcut = StoryChapterBuilderCommon.AddInteractable(world.unsafeShortcut,
            "evac.street.unsafe_shortcut", "CAMLI KESTİRMEYİ DENE", StoryInteractionKind.UnsafeChoice, streetPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationHazard, false, 1, 0.6f, 3f);
        interactions.readAssemblySign = StoryChapterBuilderCommon.AddInteractable(world.assemblySign,
            "evac.assembly.sign", "TOPLANMA LEVHASINI DOĞRULA", StoryInteractionKind.Inspect, assemblyPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationAssembly, false, 1, 0.7f, 2.8f);
        interactions.checkCan = StoryChapterBuilderCommon.AddInteractable(family.can,
            "evac.assembly.can", "CAN'I KONTROL ET", StoryInteractionKind.HelpSibling, assemblyPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationAssembly, true, 1, 0.7f);
        interactions.checkNeighbor = StoryChapterBuilderCommon.AddInteractable(world.neighborAtAssembly,
            "evac.assembly.neighbor", "KOMŞUNUN ULAŞTIĞINI DOĞRULA", StoryInteractionKind.HelpSibling, assemblyPoint,
            StoryInteractionGesture.Tap, StoryCameraZoneId.EvacuationAssembly, true, 1, 0.7f);
        interactions.useWhistle = StoryChapterBuilderCommon.AddInteractable(world.whistleWorld,
            "evac.assembly.whistle", "DÜDÜĞÜ KISA ARALIKLARLA KULLAN", StoryInteractionKind.Collect, assemblyPoint,
            StoryInteractionGesture.RepeatedTap, StoryCameraZoneId.EvacuationAssembly, true, 2, 0.9f);
        interactions.callFamily = StoryChapterBuilderCommon.AddInteractable(world.voiceSignalWorld,
            "evac.assembly.voice", "AİLE İŞARETİNİ SESLEN", StoryInteractionKind.HelpSibling, assemblyPoint,
            StoryInteractionGesture.WorldHold, StoryCameraZoneId.EvacuationAssembly, true, 1, 1.1f);

        UnityEventTools.AddPersistentListener(interactions.inspectCorridor.OnInteracted, director.InspectCorridor);
        UnityEventTools.AddPersistentListener(interactions.chooseStairs.OnInteracted, director.ChooseStairs);
        UnityEventTools.AddPersistentListener(interactions.tryElevator.OnInteracted, director.TryElevator);
        UnityEventTools.AddPersistentListener(interactions.reachUpperLanding.OnInteracted, director.ReachUpperLanding);
        UnityEventTools.AddPersistentListener(interactions.holdHandrail.OnInteracted, director.HoldHandrail);
        UnityEventTools.AddPersistentListener(interactions.reachLowerLanding.OnInteracted, director.ReachLowerLanding);
        UnityEventTools.AddPersistentListener(interactions.callNeighbor.OnInteracted, director.CallNeighbor);
        UnityEventTools.AddPersistentListener(interactions.moveNeighborCane.OnInteracted, director.MoveNeighborCane);
        UnityEventTools.AddPersistentListener(interactions.clearLightDebris.OnInteracted, director.ClearLightDebris);
        UnityEventTools.AddPersistentListener(interactions.guideNeighbor.OnInteracted, director.GuideNeighbor);
        UnityEventTools.AddPersistentListener(interactions.openBuildingExit.OnInteracted, director.OpenBuildingExit);
        UnityEventTools.AddPersistentListener(interactions.inspectStreetHazard.OnInteracted, director.InspectStreetHazard);
        UnityEventTools.AddPersistentListener(interactions.takeSafeSidewalk.OnInteracted, director.TakeSafeSidewalk);
        UnityEventTools.AddPersistentListener(interactions.tryUnsafeShortcut.OnInteracted, director.TryUnsafeShortcut);
        UnityEventTools.AddPersistentListener(interactions.readAssemblySign.OnInteracted, director.ReadAssemblySign);
        UnityEventTools.AddPersistentListener(interactions.checkCan.OnInteracted, director.CheckCan);
        UnityEventTools.AddPersistentListener(interactions.checkNeighbor.OnInteracted, director.CheckNeighbor);
        UnityEventTools.AddPersistentListener(interactions.useWhistle.OnInteracted, director.UseWhistle);
        UnityEventTools.AddPersistentListener(interactions.callFamily.OnInteracted, director.CallFamily);
        return interactions;
    }

    private static void ConfigureDirector(StoryEvacuationDirector director, StoryGameManager manager,
        StoryPlayerMovement player, StoryTouchManager touch, StoryCameraController camera,
        StoryChapterBuilderCommon.ChapterUI ui, StoryChapterBuilderCommon.Characters family,
        StorySiblingFollower follower, EvacuationWorld world, EvacuationInteractions interactions)
    {
        SerializedObject serialized = new SerializedObject(director);
        StoryChapterBuilderCommon.Set(serialized, "gameManager", manager);
        StoryChapterBuilderCommon.Set(serialized, "player", player);
        StoryChapterBuilderCommon.Set(serialized, "touchManager", touch);
        StoryChapterBuilderCommon.Set(serialized, "cameraController", camera);
        StoryChapterBuilderCommon.Set(serialized, "ui", ui.controller);
        StoryChapterBuilderCommon.Set(serialized, "deniz", family.deniz.transform);
        StoryChapterBuilderCommon.Set(serialized, "can", family.can.transform);
        StoryChapterBuilderCommon.Set(serialized, "neighbor", world.neighborAtLanding.transform);
        StoryChapterBuilderCommon.Set(serialized, "denizAnimator", family.denizAnimator);
        StoryChapterBuilderCommon.Set(serialized, "canAnimator", family.canAnimator);
        StoryChapterBuilderCommon.Set(serialized, "neighborAnimator", world.neighborAnimator);
        StoryChapterBuilderCommon.Set(serialized, "canFollower", follower);
        StoryChapterBuilderCommon.Set(serialized, "flashlightBeam", world.flashlightBeam);
        StoryChapterBuilderCommon.Set(serialized, "emergencyLightRoute", world.emergencyLightRoute);
        StoryChapterBuilderCommon.Set(serialized, "inspectCorridor", interactions.inspectCorridor);
        StoryChapterBuilderCommon.Set(serialized, "chooseStairs", interactions.chooseStairs);
        StoryChapterBuilderCommon.Set(serialized, "tryElevator", interactions.tryElevator);
        StoryChapterBuilderCommon.Set(serialized, "stairDoorAnimation", world.stairDoorAnimation);
        StoryChapterBuilderCommon.Set(serialized, "elevatorNearMissAnimation", world.elevatorAnimation);
        StoryChapterBuilderCommon.Set(serialized, "elevatorAudio", world.elevatorAudio);
        StoryChapterBuilderCommon.Set(serialized, "stairDoorClosed", world.stairDoorClosed);
        StoryChapterBuilderCommon.Set(serialized, "stairDoorOpen", world.stairDoorOpen);
        StoryChapterBuilderCommon.Set(serialized, "reachUpperLanding", interactions.reachUpperLanding);
        StoryChapterBuilderCommon.Set(serialized, "holdHandrail", interactions.holdHandrail);
        StoryChapterBuilderCommon.Set(serialized, "reachLowerLanding", interactions.reachLowerLanding);
        StoryChapterBuilderCommon.Set(serialized, "aftershockImpulse", world.aftershockImpulse);
        StoryChapterBuilderCommon.Set(serialized, "aftershockDust", world.aftershockDust);
        StoryChapterBuilderCommon.Set(serialized, "aftershockAudio", world.aftershockAudio);
        StoryChapterBuilderCommon.Set(serialized, "callNeighbor", interactions.callNeighbor);
        StoryChapterBuilderCommon.Set(serialized, "moveNeighborCane", interactions.moveNeighborCane);
        StoryChapterBuilderCommon.Set(serialized, "clearLightDebris", interactions.clearLightDebris);
        StoryChapterBuilderCommon.Set(serialized, "guideNeighbor", interactions.guideNeighbor);
        StoryChapterBuilderCommon.Set(serialized, "caneBlocked", world.caneBlocked);
        StoryChapterBuilderCommon.Set(serialized, "caneReachable", world.caneReachable);
        StoryChapterBuilderCommon.Set(serialized, "lightDebrisBlocking", world.debrisBlocking);
        StoryChapterBuilderCommon.Set(serialized, "lightDebrisCleared", world.debrisCleared);
        StoryChapterBuilderCommon.Set(serialized, "neighborAtLanding", world.neighborAtLanding);
        StoryChapterBuilderCommon.Set(serialized, "neighborAtStreet", world.neighborAtStreet);
        StoryChapterBuilderCommon.Set(serialized, "neighborAtAssembly", world.neighborAtAssembly);
        StoryChapterBuilderCommon.Set(serialized, "caneMoveAnimation", world.caneAnimation);
        StoryChapterBuilderCommon.Set(serialized, "debrisMoveAnimation", world.debrisAnimation);
        StoryChapterBuilderCommon.Set(serialized, "neighborRiseAnimation", world.neighborRiseAnimation);
        StoryChapterBuilderCommon.Set(serialized, "openBuildingExit", interactions.openBuildingExit);
        StoryChapterBuilderCommon.Set(serialized, "outsideStandPoint", world.outsideStandPoint);
        StoryChapterBuilderCommon.Set(serialized, "buildingDoorClosed", world.buildingDoorClosed);
        StoryChapterBuilderCommon.Set(serialized, "buildingDoorOpen", world.buildingDoorOpen);
        StoryChapterBuilderCommon.Set(serialized, "buildingDoorAnimation", world.buildingDoorAnimation);
        StoryChapterBuilderCommon.Set(serialized, "inspectStreetHazard", interactions.inspectStreetHazard);
        StoryChapterBuilderCommon.Set(serialized, "takeSafeSidewalk", interactions.takeSafeSidewalk);
        StoryChapterBuilderCommon.Set(serialized, "tryUnsafeShortcut", interactions.tryUnsafeShortcut);
        StoryChapterBuilderCommon.Set(serialized, "assemblyApproachPoint", world.assemblyApproachPoint);
        StoryChapterBuilderCommon.Set(serialized, "unsafeShortcutAnimation", world.unsafeShortcutAnimation);
        StoryChapterBuilderCommon.Set(serialized, "streetDust", world.streetDust);
        StoryChapterBuilderCommon.Set(serialized, "readAssemblySign", interactions.readAssemblySign);
        StoryChapterBuilderCommon.Set(serialized, "checkCan", interactions.checkCan);
        StoryChapterBuilderCommon.Set(serialized, "checkNeighbor", interactions.checkNeighbor);
        StoryChapterBuilderCommon.Set(serialized, "useWhistle", interactions.useWhistle);
        StoryChapterBuilderCommon.Set(serialized, "callFamily", interactions.callFamily);
        StoryChapterBuilderCommon.Set(serialized, "whistleWorld", world.whistleWorld);
        StoryChapterBuilderCommon.Set(serialized, "voiceSignalWorld", world.voiceSignalWorld);
        StoryChapterBuilderCommon.Set(serialized, "completionPanel", ui.completionPanel);
        StoryChapterBuilderCommon.Set(serialized, "completionDetail", ui.completionDetail);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
