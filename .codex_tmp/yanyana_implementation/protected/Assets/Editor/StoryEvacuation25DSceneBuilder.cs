using System;
using System.Collections.Generic;
using System.Linq;
using Deprem.Minigames;
using Deprem.Story;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Editor-only authoring for a long, strict side-on 2.5D post-earthquake street journey.
/// Gameplay is assembled from existing scene components; this builder adds no runtime system.
/// </summary>
public static class StoryEvacuation25DSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Minigame_Evacuation_25D.unity";

    private const string TownRoot = "Assets/Story/Environment/SyntyTown";
    private const string CityRoot =
        "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";
    private const float SidewalkTopY = 0.28f;
    private const float WalkY = SidewalkTopY + 0.002f;
    private const float FrontLaneZ = -0.34f;
    private const float RearLaneZ = 0.28f;

    [MenuItem("Tools/Deprem Story/Build 2.5D Evacuation Minigame")]
    public static void BuildFromMenu() => Build(true);

    [MenuItem("Tools/Deprem Story/Build 2.5D Evacuation Minigame (Silent)")]
    public static void BuildSilentFromMenu() => Build(false);

    [MenuItem("Tools/Deprem Story/Validate 2.5D Evacuation Minigame")]
    public static void ValidateFromMenu()
    {
        ValidateScene(SceneManager.GetActiveScene());
        Debug.Log("Post-earthquake street minigame validation passed.");
    }

    private static void Build(bool showDialog)
    {
        try
        {
            StoryChapterBuilderCommon.EnsureFolders();
            Palette palette = BuildPalette();
            RuntimeAnimatorController characterController =
                StoryAnimationLibraryBuilder.BuildLibrary(false);
            VolumeProfile volumeProfile = StoryChapterBuilderCommon.CreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MINIGAME_POST_QUAKE_STREET_25D");

            CameraRig cameras = BuildCameraRig(root.transform);
            BuildLighting(root.transform, volumeProfile);
            WorldRefs world = BuildWorld(root.transform, palette);
            CharacterRefs characters = BuildCharacters(root.transform, characterController);
            UiRefs ui = BuildUi(root.transform);
            MinigameSessionManager resultReporter = BuildResultReporter(root.transform);
            PuzzleRefs puzzle = BuildPuzzle(
                root.transform, cameras, world, characters, ui, palette);
            WireProgression(cameras, world, characters, ui, puzzle, resultReporter);
            EnsureEventSystem(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            MinigameSceneCatalog.PublishBuildSettings();
            AssetDatabase.SaveAssets();
            ValidateScene(scene);
            Selection.activeGameObject = cameras.mainCamera.gameObject;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(
                    new Vector3(-35.75f, 2.3f, 0f),
                    Quaternion.identity,
                    3.4f,
                    true);
                sceneView.Repaint();
            }

            Debug.Log("Post-earthquake 2.5D street journey built successfully: " + ScenePath);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "86 metrelik, 15 olaylı deprem sonrası sokak tahliye rotası üretildi.",
                    "Tamam");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", exception.Message, "Tamam");
            throw;
        }
    }

    private static Palette BuildPalette()
    {
        return new Palette
        {
            common = StoryChapterBuilderCommon.CreateMaterials(),
            asphalt = GetOrCreateMaterial(
                "Evac25D_StreetAsphalt", new Color32(55, 63, 72, 255), 0.08f, Color.black),
            sidewalk = GetOrCreateMaterial(
                "Evac25D_StreetSidewalk", new Color32(177, 170, 157, 255), 0.12f, Color.black),
            curb = GetOrCreateMaterial(
                "Evac25D_StreetCurb", new Color32(220, 211, 191, 255), 0.14f, Color.black),
            facadeDark = GetOrCreateMaterial(
                "Evac25D_FacadeShadow", new Color32(45, 54, 62, 255), 0.05f, Color.black),
            glass = GetOrCreateMaterial(
                "Evac25D_BrokenGlass", new Color32(81, 193, 224, 255), 0.42f,
                new Color32(12, 70, 92, 255)),
            safe = GetOrCreateMaterial(
                "Evac25D_Safe", new Color32(61, 205, 133, 255), 0.25f,
                new Color32(12, 92, 50, 255)),
            danger = GetOrCreateMaterial(
                "Evac25D_Danger", new Color32(244, 78, 68, 255), 0.2f,
                new Color32(122, 18, 14, 255)),
            amber = GetOrCreateMaterial(
                "Evac25D_Amber", new Color32(255, 186, 45, 255), 0.22f,
                new Color32(120, 62, 4, 255)),
            gas = GetOrCreateMaterial(
                "Evac25D_GasWarning", new Color32(236, 205, 79, 255), 0.14f,
                new Color32(86, 66, 8, 255)),
            rescue = GetOrCreateMaterial(
                "Evac25D_RescueBlue", new Color32(53, 151, 224, 255), 0.2f,
                new Color32(10, 54, 103, 255))
        };
    }

    private static CameraRig BuildCameraRig(Transform root)
    {
        Transform cameraRoot = NewChild(root, "SIDE_ON_STREET_CAMERAS");
        GameObject mainObject = new GameObject("Main Camera");
        mainObject.transform.SetParent(cameraRoot);
        mainObject.tag = "MainCamera";
        mainObject.transform.position = new Vector3(-35.75f, 2.3f, -18f);
        mainObject.transform.rotation = Quaternion.identity;

        Camera mainCamera = mainObject.AddComponent<Camera>();
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 3.4f;
        mainCamera.nearClipPlane = 0.08f;
        mainCamera.farClipPlane = 150f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(111, 155, 184, 255);
        mainCamera.allowHDR = true;
        mainCamera.allowMSAA = true;
        mainObject.AddComponent<AudioListener>();

        CinemachineBrain brain = mainObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, 0.52f);

        StoryCameraZoneId[] zones =
        {
            StoryCameraZoneId.RoomOverview,
            StoryCameraZoneId.EvacuationCorridor,
            StoryCameraZoneId.EvacuationStairDoor,
            StoryCameraZoneId.EvacuationStairsTop,
            StoryCameraZoneId.EvacuationLanding,
            StoryCameraZoneId.EvacuationLowerLanding,
            StoryCameraZoneId.EvacuationNeighbor,
            StoryCameraZoneId.EvacuationBuildingDoor,
            StoryCameraZoneId.EvacuationBuildingFront,
            StoryCameraZoneId.EvacuationHazard,
            StoryCameraZoneId.EvacuationStreet,
            StoryCameraZoneId.EvacuationAssembly
        };
        string[] names =
        {
            "CM_01_DamagedFacade",
            "CM_02_GasLine",
            "CM_03_RubbleBlock",
            "CM_04_MasonryClear",
            "CM_05_SafeSideChoice",
            "CM_06_BrokenGlass",
            "CM_07_InjuredNeighbor",
            "CM_08_StreetGate",
            "CM_09_UtilityPoleChoice",
            "CM_10_EmergencyLane",
            "CM_11_FallenSign",
            "CM_12_AssemblyPark"
        };
        float[] xPositions =
        {
            -35.75f, -28.7f, -22f, -17.35f, -11.15f, -0.35f,
            8.65f, 15.2f, 22.4f, 28.8f, 35.55f, 43.2f
        };
        float[] yPositions =
        {
            2.3f, 2.25f, 2.2f, 2.15f, 2.25f, 2.05f,
            2.05f, 2.15f, 2.35f, 2.05f, 1.95f, 2.05f
        };
        float[] sizes =
        {
            3.4f, 3.15f, 3.15f, 3.3f, 3.35f, 3.3f,
            3.4f, 3.2f, 3.4f, 3.3f, 3.3f, 3.2f
        };

        CinemachineCamera[] virtualCameras = new CinemachineCamera[zones.Length];
        for (int index = 0; index < zones.Length; index++)
        {
            GameObject cameraObject = new GameObject(names[index]);
            cameraObject.transform.SetParent(cameraRoot);
            cameraObject.transform.position = new Vector3(
                xPositions[index], yPositions[index], -18f);
            cameraObject.transform.rotation = Quaternion.identity;
            CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            LensSettings lens = LensSettings.Default;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = sizes[index];
            lens.NearClipPlane = 0.08f;
            lens.FarClipPlane = 150f;
            virtualCamera.Lens = lens;
            virtualCamera.Priority = 0;
            virtualCameras[index] = virtualCamera;
        }

        GameObject controllerObject = new GameObject("StreetCameraController");
        controllerObject.transform.SetParent(cameraRoot);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject data = new SerializedObject(controller);
        RequireProperty(data, "brain").objectReferenceValue = brain;
        RequireProperty(data, "initialZone").intValue = (int)StoryCameraZoneId.RoomOverview;
        RequireProperty(data, "activePriority").intValue = 30;
        RequireProperty(data, "standbyPriority").intValue = 0;
        RequireProperty(data, "transitionInputPadding").floatValue = 0.08f;
        SerializedProperty bindings = RequireProperty(data, "cameras");
        bindings.arraySize = zones.Length;
        for (int index = 0; index < zones.Length; index++)
        {
            SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
            binding.FindPropertyRelative("zone").intValue = (int)zones[index];
            binding.FindPropertyRelative("camera").objectReferenceValue = virtualCameras[index];
        }
        data.ApplyModifiedPropertiesWithoutUndo();

        return new CameraRig
        {
            mainCamera = mainCamera,
            brain = brain,
            controller = controller,
            virtualCameras = virtualCameras
        };
    }

    private static void BuildLighting(Transform root, VolumeProfile profile)
    {
        GameObject sunObject = new GameObject("PostQuake_AfternoonSun");
        sunObject.transform.SetParent(root);
        sunObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color32(255, 226, 195, 255);
        sun.intensity = 1.08f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.62f;

        foreach (float x in new[] { -36f, -28f, -20f, -12f, -4f, 4f, 12f, 20f, 28f, 36f, 44f })
        {
            Color color = x < 36f
                ? new Color32(255, 207, 171, 255)
                : new Color32(176, 226, 255, 255);
            CreatePointLight(root, "StreetFill_" + x.ToString("00"), new Vector3(x, 4.4f, -2f),
                color, 1.32f, 6.4f);
        }

        GameObject volumeObject = new GameObject("PostQuake_GlobalVolume");
        volumeObject.transform.SetParent(root);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 5f;
        volume.sharedProfile = profile;
    }

    private static WorldRefs BuildWorld(Transform root, Palette palette)
    {
        Transform worldRoot = NewChild(root, "POST_QUAKE_STREET_WORLD");
        Transform architecture = NewChild(worldRoot, "01_CONTINUOUS_STREET");

        CreatePrimitive("WorldSpan_Start", PrimitiveType.Cube, new Vector3(-40f, -1.2f, 4f),
            Vector3.one * 0.05f, palette.common.sky, architecture, false);
        CreatePrimitive("WorldSpan_End", PrimitiveType.Cube, new Vector3(47f, -1.2f, 4f),
            Vector3.one * 0.05f, palette.common.sky, architecture, false);

        CreatePrimitive("PanoramicSky_PostQuake", PrimitiveType.Cube,
            new Vector3(4f, 4.3f, 9.0f), new Vector3(92f, 11.5f, 0.18f),
            palette.common.sky, architecture, false);
        CreatePrimitive("DistantCityHaze", PrimitiveType.Cube,
            new Vector3(4f, 2.35f, 8.8f), new Vector3(92f, 4.3f, 0.12f),
            palette.common.horizon, architecture, false);

        CreatePrimitive("StreetSidewalk_Run", PrimitiveType.Cube,
            new Vector3(4f, 0.14f, 0.72f), new Vector3(88f, 0.28f, 4.0f),
            palette.sidewalk, architecture, true);
        CreatePrimitive("StreetCurb_Run", PrimitiveType.Cube,
            new Vector3(4f, -0.02f, -1.34f), new Vector3(88f, 0.46f, 0.25f),
            palette.curb, architecture, false);
        CreatePrimitive("StreetRoadFront_Run", PrimitiveType.Cube,
            new Vector3(4f, -0.66f, -1.5f), new Vector3(88f, 1.22f, 0.34f),
            palette.asphalt, architecture, false);

        for (int index = 0; index < 18; index++)
        {
            float x = -38f + index * 5f;
            CreatePrimitive("RoadLaneStripe_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(x, -0.55f, -1.69f), new Vector3(2.55f, 0.11f, 0.04f),
                palette.common.cream, architecture, false);
        }
        for (int index = 0; index < 22; index++)
        {
            float x = -39f + index * 4f;
            CreatePrimitive("SidewalkJoint_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(x, 0.285f, -1.08f), new Vector3(0.045f, 0.035f, 0.52f),
                palette.facadeDark, architecture, false);
        }

        BuildCityBackdrop(architecture, palette);
        BuildStreetDressing(worldRoot, palette);

        Transform gameplay = NewChild(worldRoot, "02_STREET_GAMEPLAY_PROPS");
        return BuildGameplayProps(gameplay, palette);
    }

    private static void BuildCityBackdrop(Transform parent, Palette palette)
    {
        string[] prefabs =
        {
            "Env_ResidentBuilding_03.prefab",
            "Env_CommercialBuilding_01.prefab",
            "Env_ResidentBuilding_01.prefab",
            "Env_CommercialBuilding_03.prefab",
            "Env_ResidentBuilding_02.prefab",
            "Env_CompanyBuilding_01.prefab",
            "Env_CommercialBuilding_04.prefab",
            "Env_ResidentBuilding_04.prefab",
            "Env_Motel_05.prefab",
            "Env_CommercialBuilding_02.prefab"
        };
        float[] centers = { -36f, -28f, -20f, -12f, -4f, 4f, 12f, 20f, 28f, 36f };
        float[] heights = { 6.9f, 6.3f, 7.1f, 6.5f, 6.8f, 7.2f, 6.4f, 7.0f, 6.6f, 6.8f };

        for (int index = 0; index < prefabs.Length; index++)
        {
            GameObject building = InstantiateCity(
                prefabs[index],
                "DamagedFacadeBuilding_" + (index + 1).ToString("00"),
                parent,
                new Vector3(centers[index], 0.28f, 4.85f),
                new Vector3(7.65f, heights[index], 4.8f),
                new Vector3(0f, 180f, 0f));
            FitFacadeToBounds(building, new Vector3(centers[index], 0.28f, 4.85f),
                new Vector3(7.65f, heights[index], 4.8f));
            ConfigureSetRenderer(building, true, true);

            float crackX = centers[index] + (index % 2 == 0 ? -1.25f : 1.2f);
            float crackY = 3.0f + (index % 3) * 0.45f;
            BuildFacadeCrack(parent, "FacadeCrack_" + (index + 1).ToString("00"),
                new Vector3(crackX, crackY, 2.3f), palette.facadeDark, index % 2 == 0);

            if (index % 2 == 0)
            {
                BuildBrokenWindow(parent, "BrokenWindow_" + (index + 1).ToString("00"),
                    new Vector3(centers[index] + 1.45f, 3.65f, 2.26f), palette);
            }
            if (index == 0 || index == 2 || index == 5 || index == 7)
            {
                BuildRubblePile(parent, "FacadeRubble_" + (index + 1).ToString("00"),
                    new Vector3(centers[index] - 1.4f, 0.29f, 1.72f),
                    0.82f, palette.common.concrete, palette.common.wood);
            }
        }

        // The foreground facades already form a continuous street wall. Large flat cubes
        // behind them read as editor blockout panels from an oblique Scene View, so leave
        // the far depth to the authored sky/haze instead of faking a second skyline.
    }

    private static void BuildStreetDressing(Transform parent, Palette palette)
    {
        Transform dangerDressing = NewChild(parent, "03_POST_QUAKE_SET_DRESSING");

        for (int index = 0; index < 10; index++)
        {
            float x = -38f + index * 7.4f;
            InstantiateCity(
                "Prop_RoadCone_0" + (index % 3 + 1) + ".prefab",
                "SafetyCone_" + index.ToString("00"),
                dangerDressing,
                new Vector3(x, 0.29f, -0.72f),
                new Vector3(0.36f, 0.58f, 0.36f),
                new Vector3(0f, index * 23f, 0f));
        }

        foreach (float x in new[] { -32f, -10f, 9f, 31f, 42f })
        {
            GameObject lamp = InstantiateTown(
                TownRoot + "/SM_Prop_Streetlamp_01.fbx",
                "StandingStreetLamp_" + x.ToString("00"),
                dangerDressing,
                new Vector3(x, 0.29f, 2.15f),
                new Vector3(0.48f, 3.05f, 0.48f),
                Vector3.zero);
            ConfigureSetRenderer(lamp, true, true);
        }

        GameObject pickup = InstantiateTown(
            TownRoot + "/SM_Veh_Pickup_01.fbx",
            "AbandonedPickup_PostQuake",
            dangerDressing,
            new Vector3(18.2f, 0.29f, 2.05f),
            new Vector3(3.85f, 1.42f, 1.25f),
            new Vector3(0f, 270f, -2f));
        ConfigureSetRenderer(pickup, true, true);

        GameObject firetruck = InstantiateTown(
            TownRoot + "/SM_Veh_Firetruck_01.fbx",
            "EmergencyFiretruck_Approach",
            dangerDressing,
            new Vector3(30.4f, 0.29f, 2.18f),
            new Vector3(5.05f, 1.72f, 1.4f),
            new Vector3(0f, 270f, 0f));
        ConfigureSetRenderer(firetruck, false, true);
        CreatePrimitive("FiretruckBlueLamp", PrimitiveType.Sphere,
            new Vector3(29.55f, 1.92f, 0.82f), new Vector3(0.14f, 0.1f, 0.08f),
            palette.rescue, dangerDressing, false);
        CreatePrimitive("FiretruckRedLamp", PrimitiveType.Sphere,
            new Vector3(29.88f, 1.92f, 0.82f), new Vector3(0.14f, 0.1f, 0.08f),
            palette.danger, dangerDressing, false);

        BuildLeaningUtilityPole(dangerDressing, palette);
        BuildAssemblyPark(dangerDressing, palette);
    }

    private static void BuildLeaningUtilityPole(Transform parent, Palette palette)
    {
        CreatePrimitive("LeaningUtilityPole", PrimitiveType.Cylinder,
            new Vector3(22.4f, 2.65f, 1.35f), new Vector3(0.2f, 2.65f, 0.2f),
            palette.common.wood, parent, false, Quaternion.Euler(0f, 0f, -19f));
        CreatePrimitive("UtilityCrossArm", PrimitiveType.Cube,
            new Vector3(23.15f, 4.98f, 1.32f), new Vector3(1.55f, 0.13f, 0.16f),
            palette.common.wood, parent, false, Quaternion.Euler(0f, 0f, -19f));
        BuildBeam("SaggingPowerLine_A", parent,
            new Vector3(20.1f, 4.85f, 1.26f), new Vector3(24.4f, 3.62f, 1.18f),
            0.045f, palette.facadeDark);
        BuildBeam("SaggingPowerLine_B", parent,
            new Vector3(20.2f, 4.45f, 1.34f), new Vector3(25.25f, 1.05f, 0.55f),
            0.045f, palette.facadeDark);
        BuildBeam("LiveWireOnGround", parent,
            new Vector3(24.95f, 0.34f, 0.15f), new Vector3(26.0f, 0.34f, -0.35f),
            0.07f, palette.danger);
        CreatePrimitive("LiveWireWarningGlow", PrimitiveType.Sphere,
            new Vector3(25.65f, 0.5f, -0.46f), new Vector3(0.18f, 0.12f, 0.08f),
            palette.amber, parent, false);
    }

    private static void BuildAssemblyPark(Transform parent, Palette palette)
    {
        CreatePrimitive("AssemblyParkGround", PrimitiveType.Cube,
            new Vector3(42.7f, 0.34f, 2.45f), new Vector3(10.3f, 0.14f, 1.55f),
            palette.common.grass, parent, false);
        for (int index = 0; index < 4; index++)
        {
            InstantiateTown(
                TownRoot + "/SM_Env_Fence_White_Straight_01.fbx",
                "AssemblyFence_" + index.ToString("00"),
                parent,
                new Vector3(39.2f + index * 2.45f, 0.3f, 3.05f),
                new Vector3(2.3f, 0.92f, 0.16f),
                Vector3.zero);
        }
        InstantiateCity("Prop_Tree_04.prefab", "AssemblyTree_Left", parent,
            new Vector3(41.25f, 0.29f, 4.05f), new Vector3(1.45f, 3.45f, 1.45f),
            new Vector3(0f, 14f, 0f));
        InstantiateCity("Prop_Tree_02.prefab", "AssemblyTree_Right", parent,
            new Vector3(45.45f, 0.29f, 4.05f), new Vector3(1.35f, 3.05f, 1.35f),
            new Vector3(0f, -12f, 0f));
        InstantiateTown(TownRoot + "/SM_Prop_ParkBench_01.fbx", "AssemblyBench", parent,
            new Vector3(40.4f, 0.29f, 1.75f), new Vector3(1.75f, 0.82f, 0.72f),
            new Vector3(0f, 180f, 0f));

        CreatePrimitive("RescueTentBack", PrimitiveType.Cube,
            new Vector3(43.4f, 1.55f, 3.35f), new Vector3(3.25f, 2.45f, 0.12f),
            palette.common.cream, parent, false);
        BuildBeam("RescueTentRoof_Left", parent,
            new Vector3(41.75f, 2.55f, 3.0f), new Vector3(43.4f, 3.15f, 3.0f),
            0.28f, palette.rescue);
        BuildBeam("RescueTentRoof_Right", parent,
            new Vector3(43.4f, 3.15f, 3.0f), new Vector3(45.05f, 2.55f, 3.0f),
            0.28f, palette.rescue);
        foreach (float x in new[] { 41.85f, 44.95f })
        {
            CreatePrimitive("RescueTentPost_" + x.ToString("00"), PrimitiveType.Cube,
                new Vector3(x, 1.4f, 3.0f), new Vector3(0.11f, 2.25f, 0.11f),
                palette.common.metal, parent, false);
        }
        CreatePrimitive("AidStationHeaderPlate", PrimitiveType.Cube,
            new Vector3(43.4f, 2.34f, 2.82f), new Vector3(2.35f, 0.42f, 0.08f),
            palette.rescue, parent, false);
        WorldText("YARDIM NOKTASI", parent, new Vector3(43.4f, 2.34f, 2.7f),
            0.66f, Color.white);
        CreatePrimitive("AidStationMedicalPlate", PrimitiveType.Cube,
            new Vector3(43.4f, 1.43f, 2.82f), new Vector3(0.76f, 0.76f, 0.08f),
            palette.common.cream, parent, false);
        CreatePrimitive("AidStationCross_H", PrimitiveType.Cube,
            new Vector3(43.4f, 1.43f, 2.7f), new Vector3(0.5f, 0.16f, 0.05f),
            palette.danger, parent, false);
        CreatePrimitive("AidStationCross_V", PrimitiveType.Cube,
            new Vector3(43.4f, 1.43f, 2.69f), new Vector3(0.16f, 0.5f, 0.05f),
            palette.danger, parent, false);

        foreach ((string asset, string name, Vector3 position, float height) person in new[]
                 {
                     ("Assets/Story/Characters/MeshyFamily/Prefabs/Anne.prefab",
                         "Assembly_Anne", new Vector3(42.95f, WalkY, 1.08f), 1.62f),
                     ("Assets/Story/Characters/MeshyFamily/Prefabs/Baba.prefab",
                         "Assembly_Baba", new Vector3(44.35f, WalkY, 1.25f), 1.7f)
                 })
        {
            GameObject visual = StoryChapterBuilderCommon.InstantiateAsset(
                person.asset, person.name, parent, person.position,
                new Vector3(0.92f, person.height, 1.25f), new Vector3(0f, 270f, 0f),
                false, false);
            RemovePhysicsRecursive(visual);
            ConfigureSetRenderer(visual, true, true);
            PrepareStaticCharacter(visual, SidewalkTopY);
        }
    }

    private static WorldRefs BuildGameplayProps(Transform parent, Palette palette)
    {
        WorldRefs refs = new WorldRefs();

        // 01 — identify the safe direction away from a visibly damaged facade.
        refs.facadeClear = new GameObject("01_FacadeClearanceInteractable");
        refs.facadeClear.transform.SetParent(parent);
        refs.facadeClear.transform.position = new Vector3(-34.75f, 0.34f, -0.18f);
        refs.facadeDanger = NewState(refs.facadeClear.transform, "FacadeDanger_ACTIVE");
        BuildSafetyBollard(refs.facadeDanger.transform, "ClearanceBollard",
            new Vector3(-34.75f, SidewalkTopY, -0.38f),
            palette.amber, palette.common.cream);
        BuildSafetyBollard(refs.facadeDanger.transform, "FacadeBarrier_Left",
            new Vector3(-37.55f, SidewalkTopY, -0.28f),
            palette.danger, palette.common.cream);
        BuildSafetyBollard(refs.facadeDanger.transform, "FacadeBarrier_Right",
            new Vector3(-36.55f, SidewalkTopY, -0.28f),
            palette.danger, palette.common.cream);
        CreatePrimitive("FacadeBarrierCrossbar", PrimitiveType.Cube,
            new Vector3(-37.05f, 0.86f, -0.3f), new Vector3(1.12f, 0.1f, 0.09f),
            palette.danger, refs.facadeDanger.transform, false,
            Quaternion.Euler(0f, 0f, -5f));
        CreatePrimitive("LooseFacadeCornice_Stage01", PrimitiveType.Cube,
            new Vector3(-36.8f, 3.62f, 2.08f), new Vector3(2.25f, 0.26f, 0.38f),
            palette.common.concrete, parent, false,
            Quaternion.Euler(0f, 0f, -8f));
        refs.facadeSafe = NewState(refs.facadeClear.transform, "FacadeBuffer_SAFE");
        refs.facadeSafe.SetActive(false);
        refs.facadeDust = BuildDust(
            parent, "FacadeFallingDust", new Vector3(-36.85f, 3.48f, 1.75f),
            palette.common.dust, new Color32(155, 140, 122, 190), 17f, 85);

        // 02 — hold position in a clear pocket during an aftershock.
        refs.aftershockSpot = new GameObject("02_AftershockSafeSpotInteractable");
        refs.aftershockSpot.transform.SetParent(parent);
        refs.aftershockSpot.transform.position = new Vector3(-34.25f, 0.34f, -0.18f);
        refs.aftershockUnsafe = NewState(refs.aftershockSpot.transform, "Aftershock_ALERT");
        InstantiateCity("Prop_RoadCone_02.prefab", "AftershockOpenAreaCone",
            refs.aftershockUnsafe.transform,
            new Vector3(-34.25f, WalkY, -0.24f), new Vector3(0.4f, 0.62f, 0.4f),
            Vector3.zero);
        refs.aftershockSafe = NewState(refs.aftershockSpot.transform, "Aftershock_HELD");
        refs.aftershockSafe.SetActive(false);
        refs.aftershockDust = BuildDust(
            parent, "AftershockDustCloud", new Vector3(-34.25f, 1.2f, 1.35f),
            palette.common.dust, new Color32(177, 153, 126, 170), 11f, 52);

        // 03 — shut a damaged exterior gas line.
        refs.gasValve = new GameObject("03_StreetGasValveInteractable");
        refs.gasValve.transform.SetParent(parent);
        refs.gasValve.transform.position = new Vector3(-28.35f, 1.02f, -0.02f);
        CreatePrimitive("GasMeterCabinet", PrimitiveType.Cube,
            new Vector3(-28.35f, 0.92f, 0.38f), new Vector3(1.05f, 1.42f, 0.38f),
            palette.common.metal, refs.gasValve.transform, false);
        CreatePrimitive("StreetGasPipeVertical", PrimitiveType.Cube,
            new Vector3(-29.05f, 1.18f, 0.28f), new Vector3(0.12f, 2.15f, 0.12f),
            palette.common.metal, refs.gasValve.transform, false);
        CreatePrimitive("StreetGasPipeBend", PrimitiveType.Cube,
            new Vector3(-28.7f, 2.22f, 0.28f), new Vector3(0.82f, 0.12f, 0.12f),
            palette.common.metal, refs.gasValve.transform, false);
        refs.gasOpen = BuildValveState(refs.gasValve.transform, "GasValve_OPEN",
            new Vector3(-28.35f, 1.05f, -0.14f), palette.danger, 0f);
        refs.gasClosed = BuildValveState(refs.gasValve.transform, "GasValve_CLOSED",
            new Vector3(-28.35f, 1.05f, -0.16f), palette.safe, 45f);
        refs.gasClosed.SetActive(false);
        refs.gasLeak = BuildDust(
            parent, "GasLeakVisibleHazard", new Vector3(-28.7f, 2.25f, 0.2f),
            palette.common.dust, new Color32(232, 204, 76, 180), 20f, 70);

        // 04 — inspect and knock down loose rubble before passing.
        refs.rubbleCheck = new GameObject("04_LooseRubbleCheckInteractable");
        refs.rubbleCheck.transform.SetParent(parent);
        refs.rubbleCheck.transform.position = new Vector3(-22.15f, 0.85f, -0.08f);
        refs.rubbleBlocked = NewState(refs.rubbleCheck.transform, "LooseRubble_BLOCKED");
        BuildRubblePile(refs.rubbleBlocked.transform, "LooseMasonry",
            new Vector3(-22.15f, WalkY, 0.15f), 1.1f,
            palette.common.concrete, palette.common.wood);
        CreatePrimitive("LooseWallSlab", PrimitiveType.Cube,
            new Vector3(-22.15f, 1.55f, 0.22f), new Vector3(1.7f, 0.22f, 0.42f),
            palette.common.concrete, refs.rubbleBlocked.transform, false,
            Quaternion.Euler(0f, 0f, 17f));
        refs.rubbleCleared = NewState(refs.rubbleCheck.transform, "LooseRubble_CHECKED");
        refs.rubbleCleared.SetActive(false);

        // 05 — drag a fallen masonry block out of the walking lane.
        refs.masonry = CreateDraggableRubble(
            parent, "05_DraggableMasonryBlock", new Vector3(-16.45f, WalkY, -0.12f),
            palette.common.concrete, palette.common.wood);
        CreateDropTarget(parent, "MasonryClearZone", new Vector3(-18.25f, 0.78f, 0.5f),
            new Vector3(1.18f, 1.05f, 0.92f), new Vector3(-18.25f, WalkY, 0.5f),
            palette.amber, out refs.masonryDropZone, out refs.masonrySnap);
        refs.masonryDragPlane = CreateDragPlane(
            parent, "MasonryDragPlane", new Vector3(-17.3f, 0.84f, -0.08f));
        refs.masonryCleared = NewState(parent, "MasonryLane_CLEAR");
        refs.masonryCleared.SetActive(false);

        // 07 — mark a broken-glass field before the family crosses it.
        refs.glassMark = new GameObject("07_BrokenGlassMarkInteractable");
        refs.glassMark.transform.SetParent(parent);
        refs.glassMark.transform.position = new Vector3(-10.65f, 0.54f, -0.12f);
        refs.glassUnmarked = NewState(refs.glassMark.transform, "GlassField_UNMARKED");
        BuildBrokenGlassField(refs.glassUnmarked.transform, new Vector3(-10.65f, 0.32f, -0.18f),
            palette.glass, 11);
        refs.glassMarked = NewState(refs.glassMark.transform, "GlassField_MARKED");
        CreatePrimitive("GlassCautionPostLeft", PrimitiveType.Cube,
            new Vector3(-11.6f, 0.78f, -0.18f), new Vector3(0.1f, 0.95f, 0.1f),
            palette.amber, refs.glassMarked.transform, false);
        CreatePrimitive("GlassCautionPostRight", PrimitiveType.Cube,
            new Vector3(-9.7f, 0.78f, -0.18f), new Vector3(0.1f, 0.95f, 0.1f),
            palette.amber, refs.glassMarked.transform, false);
        BuildBeam("GlassCautionTape", refs.glassMarked.transform,
            new Vector3(-11.58f, 1.0f, -0.22f), new Vector3(-9.72f, 1.0f, -0.22f),
            0.08f, palette.amber);
        refs.glassMarked.SetActive(false);

        // 08 — place a broad cover over the next glass patch.
        BuildBrokenGlassField(parent, new Vector3(-1.25f, 0.32f, -0.12f), palette.glass, 14);
        refs.glassCover = CreateBoard(
            parent, "08_DraggableGlassCoverBoard", new Vector3(0.55f, WalkY, -0.08f),
            new Vector3(1.45f, 0.13f, 0.72f), palette.common.wood);
        CreateDropTarget(parent, "GlassCoverTarget", new Vector3(-1.25f, 0.58f, -0.12f),
            new Vector3(1.65f, 0.72f, 0.98f), new Vector3(-1.25f, WalkY, -0.12f),
            palette.safe, out refs.glassDropZone, out refs.glassSnap);
        refs.glassDragPlane = CreateDragPlane(
            parent, "GlassBoardDragPlane", new Vector3(-0.35f, 0.72f, -0.1f));
        refs.glassCovered = CreatePrimitive("GlassField_COVERED", PrimitiveType.Cube,
            new Vector3(-1.25f, 0.36f, -0.12f), new Vector3(1.58f, 0.12f, 0.82f),
            palette.common.wood, parent, false);
        refs.glassCovered.SetActive(false);

        // 09 — check the injured neighbor before attempting to move on.
        refs.injuredCheck = new GameObject("09_InjuredNeighborCheckInteractable");
        refs.injuredCheck.transform.SetParent(parent);
        refs.injuredCheck.transform.position = new Vector3(7.15f, 0.95f, -0.08f);
        refs.injuredAlert = NewState(refs.injuredCheck.transform, "InjuredNeighbor_ALERT");
        refs.injuredChecked = NewState(refs.injuredCheck.transform, "InjuredNeighbor_CHECKED");
        refs.injuredChecked.SetActive(false);
        CreatePrimitive("NeighborBenchSeat", PrimitiveType.Cube,
            new Vector3(7.15f, 0.76f, 0.62f), new Vector3(1.25f, 0.13f, 0.62f),
            palette.common.wood, refs.injuredCheck.transform, false);
        CreatePrimitive("NeighborBenchLeg_Left", PrimitiveType.Cube,
            new Vector3(6.74f, 0.51f, 0.64f), new Vector3(0.12f, 0.45f, 0.46f),
            palette.common.metal, refs.injuredCheck.transform, false);
        CreatePrimitive("NeighborBenchLeg_Right", PrimitiveType.Cube,
            new Vector3(7.56f, 0.51f, 0.64f), new Vector3(0.12f, 0.45f, 0.46f),
            palette.common.metal, refs.injuredCheck.transform, false);

        // 10 — drag the first-aid kit to the neighbor.
        refs.firstAid = CreateFirstAidKit(
            parent, "10_DraggableFirstAidKit", new Vector3(10.15f, WalkY, -0.12f), palette);
        CreateDropTarget(parent, "FirstAidNeighborTarget", new Vector3(7.55f, 0.92f, 0.15f),
            new Vector3(1.05f, 1.35f, 0.88f), new Vector3(7.55f, WalkY, 0.15f),
            palette.safe, out refs.firstAidDropZone, out refs.firstAidSnap);
        refs.firstAidDragPlane = CreateDragPlane(
            parent, "FirstAidDragPlane", new Vector3(8.85f, 0.85f, -0.08f));
        refs.firstAidDelivered = NewState(parent, "FirstAid_DELIVERED");
        CreatePrimitive("DeliveredBandage", PrimitiveType.Cube,
            new Vector3(7.3f, 1.15f, -0.12f), new Vector3(0.56f, 0.18f, 0.07f),
            palette.common.cream, refs.firstAidDelivered.transform, false,
            Quaternion.Euler(0f, 0f, 18f));
        CreatePrimitive("DeliveredBandageCross", PrimitiveType.Cube,
            new Vector3(7.3f, 1.15f, -0.2f), new Vector3(0.18f, 0.5f, 0.05f),
            palette.common.cream, refs.firstAidDelivered.transform, false,
            Quaternion.Euler(0f, 0f, 18f));
        refs.firstAidDelivered.SetActive(false);

        // 11 — force open a jammed exterior gate.
        refs.gate = new GameObject("11_JammedStreetGateInteractable");
        refs.gate.transform.SetParent(parent);
        refs.gate.transform.position = new Vector3(15.35f, 1.15f, -0.02f);
        CreatePrimitive("GatePostLeft", PrimitiveType.Cube,
            new Vector3(14.4f, 1.28f, 0.35f), new Vector3(0.18f, 2.15f, 0.2f),
            palette.common.metal, refs.gate.transform, false);
        CreatePrimitive("GatePostRight", PrimitiveType.Cube,
            new Vector3(16.3f, 1.28f, 0.35f), new Vector3(0.18f, 2.15f, 0.2f),
            palette.common.metal, refs.gate.transform, false);
        refs.gateClosed = NewState(refs.gate.transform, "StreetGate_CLOSED");
        for (int index = 0; index < 4; index++)
        {
            CreatePrimitive("GateBar_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(14.72f + index * 0.42f, 1.35f, 0.18f),
                new Vector3(0.1f, 1.75f, 0.11f), palette.common.metal,
                refs.gateClosed.transform, false);
        }
        refs.gateOpen = NewState(refs.gate.transform, "StreetGate_OPEN");
        for (int index = 0; index < 4; index++)
        {
            CreatePrimitive("OpenGateBar_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(16.38f + index * 0.13f, 1.35f, 0.18f),
                new Vector3(0.09f, 1.75f, 0.1f), palette.safe,
                refs.gateOpen.transform, false,
                Quaternion.Euler(0f, 0f, -10f));
        }
        refs.gateOpen.SetActive(false);

        // 13 — slide a barrier out of the emergency vehicle lane.
        refs.fireLane = new GameObject("13_EmergencyLaneBarrierInteractable");
        refs.fireLane.transform.SetParent(parent);
        refs.fireLane.transform.position = new Vector3(28.45f, 0.82f, -0.18f);
        refs.fireLaneBlocked = NewState(refs.fireLane.transform, "EmergencyLane_BLOCKED");
        CreatePrimitive("BarrierPostLeft", PrimitiveType.Cube,
            new Vector3(27.55f, 0.72f, -0.18f), new Vector3(0.16f, 0.95f, 0.16f),
            palette.danger, refs.fireLaneBlocked.transform, false);
        CreatePrimitive("BarrierPostRight", PrimitiveType.Cube,
            new Vector3(29.35f, 0.72f, -0.18f), new Vector3(0.16f, 0.95f, 0.16f),
            palette.danger, refs.fireLaneBlocked.transform, false);
        CreatePrimitive("EmergencyLaneCrossbar", PrimitiveType.Cube,
            new Vector3(28.45f, 0.98f, -0.2f), new Vector3(1.95f, 0.18f, 0.14f),
            palette.danger, refs.fireLaneBlocked.transform, false);
        refs.fireLaneClear = NewState(refs.fireLane.transform, "EmergencyLane_CLEAR");
        CreatePrimitive("MovedBarrier", PrimitiveType.Cube,
            new Vector3(30.1f, 0.48f, 0.55f), new Vector3(1.6f, 0.16f, 0.14f),
            palette.safe, refs.fireLaneClear.transform, false,
            Quaternion.Euler(0f, 0f, -8f));
        refs.fireLaneClear.SetActive(false);

        // 14 — drag a fallen sign away from the route.
        refs.fallenSign = InstantiateCity(
            "Prop_StreetSign_Footpath_01.prefab", "14_DraggableFallenStreetSign", parent,
            new Vector3(35.25f, WalkY, -0.1f), new Vector3(1.35f, 1.32f, 0.3f),
            new Vector3(0f, 180f, -58f));
        EnsureRootBoxCollider(refs.fallenSign).isTrigger = false;
        CreateDropTarget(parent, "FallenSignClearZone", new Vector3(37.2f, 0.82f, 0.62f),
            new Vector3(1.2f, 1.15f, 0.95f), new Vector3(37.2f, WalkY, 0.62f),
            palette.amber, out refs.signDropZone, out refs.signSnap);
        refs.signDragPlane = CreateDragPlane(
            parent, "FallenSignDragPlane", new Vector3(36.2f, 0.82f, -0.05f));
        refs.signCleared = NewState(parent, "FallenSignRoute_CLEAR");
        refs.signCleared.SetActive(false);

        // 15 — confirm family headcount at the assembly point.
        refs.beacon = new GameObject("15_AssemblyHeadcountInteractable");
        refs.beacon.transform.SetParent(parent);
        refs.beacon.transform.position = new Vector3(43f, 0.36f, -0.08f);
        InstantiateTown(TownRoot + "/SM_Prop_Sign_BusStop_01.fbx", "AssemblyWayfindingPost",
            refs.beacon.transform, new Vector3(42.3f, WalkY, 0.05f),
            new Vector3(0.46f, 1.9f, 0.32f), new Vector3(0f, 180f, 0f));
        CreatePrimitive("AssemblySignFace", PrimitiveType.Cube,
            new Vector3(42.3f, 1.62f, -0.25f), new Vector3(0.78f, 0.42f, 0.08f),
            palette.safe, refs.beacon.transform, false);
        WorldText("AİLE", refs.beacon.transform, new Vector3(42.3f, 1.62f, -0.36f),
            0.58f, Color.white);
        CreatePrimitive("AssemblyBeaconHousing", PrimitiveType.Cylinder,
            new Vector3(42.3f, 2.15f, -0.02f), new Vector3(0.16f, 0.08f, 0.16f),
            palette.common.metal, refs.beacon.transform, false,
            Quaternion.Euler(90f, 0f, 0f));
        refs.beaconOff = CreatePrimitive("AssemblyBeacon_OFF", PrimitiveType.Sphere,
            new Vector3(42.3f, 2.15f, -0.14f), new Vector3(0.13f, 0.09f, 0.08f),
            palette.facadeDark, refs.beacon.transform, false);
        refs.beaconOn = CreatePrimitive("AssemblyBeacon_ON", PrimitiveType.Sphere,
            new Vector3(42.3f, 2.15f, -0.15f), new Vector3(0.16f, 0.12f, 0.09f),
            palette.safe, refs.beacon.transform, false);
        refs.beaconOn.SetActive(false);
        refs.confetti = BuildConfetti(parent, palette, new Vector3(43f, 5.1f, -1.5f));

        return refs;
    }

    private static CharacterRefs BuildCharacters(Transform root, RuntimeAnimatorController controller)
    {
        Transform characterRoot = NewChild(root, "STREET_CHARACTERS_25D");
        StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
            characterRoot, controller, false,
            new Vector3(-36.65f, WalkY, FrontLaneZ),
            new Vector3(-37.25f, WalkY, RearLaneZ),
            Vector3.zero);

        GameObject deniz = WrapCharacter(family.deniz, characterRoot, "Deniz_Street25D_Actor");
        GameObject can = WrapCharacter(family.can, characterRoot, "Can_Street25D_Actor");
        StoryPlayerMovement touchProxy =
            deniz.GetComponent<StoryPlayerMovement>() ?? deniz.AddComponent<StoryPlayerMovement>();
        NavMeshAgent agent = deniz.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;
        PrepareScriptedActor(deniz);
        PrepareScriptedActor(can);

        Vector3[][] denizPaths =
        {
            Path(-35.8f, -32.2f, -28.75f),
            Path(-27.4f, -24.8f, -22.25f),
            Path(-21.2f, -18.9f, -16.55f),
            Path(-14.4f, -10.7f, -6.7f, -3.6f, -1.35f),
            Path(0.15f, 3.4f, 6.15f),
            Path(7.7f, 11.2f, 15.0f),
            Path(16.5f, 19.2f, 21.35f),
            Path(23.2f, 27.2f, 31.2f, 34.45f),
            Path(36.0f, 39.2f, 42.35f)
        };
        Vector3[][] canPaths = denizPaths
            .Select(path => path.Select(point => point + new Vector3(-0.52f, 0f, 0.62f)).ToArray())
            .ToArray();
        float[] speeds = { 3.0f, 3.05f, 3.0f, 3.2f, 3.1f, 3.15f, 3.05f, 3.4f, 3.55f };

        StairPathWalker[] denizSegments = new StairPathWalker[denizPaths.Length];
        StairPathWalker[] canSegments = new StairPathWalker[canPaths.Length];
        for (int index = 0; index < denizPaths.Length; index++)
        {
            denizSegments[index] = ConfigureWalker(
                deniz, "DenizStreetRoute_" + (index + 1).ToString("00"),
                denizPaths[index], speeds[index]);
            canSegments[index] = ConfigureWalker(
                can, "CanStreetRoute_" + (index + 1).ToString("00"),
                canPaths[index], speeds[index] * 1.05f);
        }

        GameObject neighbor = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/Story/Characters/MeshyFamily/Prefabs/Komsu.prefab",
            "InjuredNeighbor_Street25D",
            characterRoot,
            new Vector3(7.15f, WalkY, 0.25f),
            1.58f,
            StoryAnimationLibraryBuilder.LoadAdultInjuredController());
        neighbor.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
        RemovePhysicsRecursive(neighbor);
        ConfigureSetRenderer(neighbor, true, true);
        PrepareStaticCharacter(neighbor, SidewalkTopY);

        return new CharacterRefs
        {
            deniz = deniz,
            can = can,
            neighbor = neighbor,
            touchProxy = touchProxy,
            denizSegments = denizSegments,
            canSegments = canSegments
        };
    }

    private static Vector3[] Path(params float[] xPositions)
    {
        return xPositions.Select(x => new Vector3(x, WalkY, FrontLaneZ)).ToArray();
    }

    private static GameObject WrapCharacter(GameObject visual, Transform parent, string actorName)
    {
        GameObject actor = new GameObject(actorName);
        actor.transform.SetParent(parent);
        actor.transform.position = visual.transform.position;
        actor.transform.rotation = Quaternion.identity;
        actor.transform.localScale = Vector3.one;
        visual.name += "_Visual";
        visual.transform.SetParent(actor.transform, true);
        actor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        return actor;
    }

    private static void PrepareScriptedActor(GameObject actor)
    {
        // These characters follow authored 2.5D street paths. StairPathWalker already
        // supports transform movement when no CharacterController is present, which
        // keeps the route deterministic and prevents decorative street colliders from
        // blocking a story segment.
        RemoveRootPhysics(actor);
        Vector3 actorPosition = actor.transform.position;
        actor.transform.position = new Vector3(actorPosition.x, WalkY, actorPosition.z);

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        PrepareAnimator(animator);
        Transform visualRoot = actor.transform.Cast<Transform>()
            .FirstOrDefault(child => child.name.EndsWith("_Visual", StringComparison.Ordinal));
        AlignRendererBottom(actor, visualRoot != null ? visualRoot : actor.transform, SidewalkTopY);
    }

    private static void PrepareStaticCharacter(GameObject character, float floorY)
    {
        Animator animator = character.GetComponentInChildren<Animator>(true);
        PrepareAnimator(animator);
        AlignRendererBottom(character, character.transform, floorY);
        if (animator != null)
            animator.enabled = false;
    }

    private static void PrepareAnimator(Animator animator)
    {
        if (animator == null)
            return;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.stabilizeFeet = false;
        if (animator.runtimeAnimatorController != null && animator.gameObject.activeInHierarchy)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private static void AlignRendererBottom(
        GameObject rendererRoot, Transform movableRoot, float floorY)
    {
        Renderer[] renderers = rendererRoot.GetComponentsInChildren<Renderer>(false)
            .Where(renderer => renderer != null && renderer.enabled &&
                               renderer is not ParticleSystemRenderer &&
                               renderer is not TrailRenderer &&
                               renderer is not LineRenderer)
            .ToArray();
        if (renderers.Length == 0 || movableRoot == null)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        float adjustment = floorY - 0.004f - bounds.min.y;
        if (!float.IsNaN(adjustment) && !float.IsInfinity(adjustment))
            movableRoot.position += Vector3.up * adjustment;
    }

    private static StairPathWalker ConfigureWalker(
        GameObject actor, string pathName, Vector3[] positions, float speed)
    {
        Transform pathRoot = NewChild(actor.transform.parent, pathName);
        Transform[] points = new Transform[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            points[index] = NewChild(pathRoot, "Point_" + index.ToString("00"));
            points[index].position = positions[index];
        }

        StairPathWalker walker = actor.AddComponent<StairPathWalker>();
        walker.pathPoints = points;
        walker.moveSpeed = speed;
        walker.rotationSpeed = 14f;
        walker.stoppingDistance = 0.34f;
        walker.useTransformFallbackWhenControllerStuck = true;
        walker.stuckMoveEpsilon = 0.001f;
        walker.gravity = -24f;
        walker.groundedGravity = -3f;
        walker.animator = actor.GetComponentInChildren<Animator>(true);
        walker.animatorWalkReferenceSpeed = 1.7f;
        walker.maxAnimatorMoveSpeed = 1.8f;
        walker.onPathCompleted = new UnityEvent();
        return walker;
    }

    private static UiRefs BuildUi(Transform root)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out _, out TMP_FontAsset semibold, out TMP_FontAsset bold);

        GameObject canvasObject = new GameObject("PostQuakeStreet_UI", typeof(RectTransform));
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.55f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject safeArea = StoryChapterBuilderCommon.CreateUIRect(
            "SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject titlePanel = StoryChapterBuilderCommon.CreatePanel(
            "StreetMissionTitlePanel", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -54f), new Vector2(860f, 96f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text title = StoryChapterBuilderCommon.CreateText(
            "Title", titlePanel.transform, bold, 29f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 14f), new Vector2(760f, 44f));
        title.text = "DEPREM SONRASI GÜVENLİ İLERLE";
        TMP_Text subtitle = StoryChapterBuilderCommon.CreateText(
            "Subtitle", titlePanel.transform, semibold, 15f, new Color32(178, 226, 247, 255),
            TextAlignmentOptions.Center, new Vector2(0f, -24f), new Vector2(760f, 24f));
        subtitle.text = "15 YOL OLAYI  •  2.5D SOKAK TAHLİYESİ";

        string[] stageTitles =
        {
            "HASARLI CEPHEDEN UZAKLAŞ",
            "ARTÇI SIRASINDA AÇIK NOKTADA BEKLE",
            "SOKAKTAKİ GAZ VANASINI KAPAT",
            "GEVŞEK MOLOZU KONTROL ET",
            "BETON PARÇASINI YÜRÜME HATTINDAN ÇEK",
            "BİNALARDAN UZAK GÜVENLİ TARAFI SEÇ",
            "KIRIK CAM ALANINI İŞARETLE",
            "KORUYUCU TAHTAYI CAMLARIN ÜSTÜNE KOY",
            "YARALI KOMŞUYU KONTROL ET",
            "İLK YARDIM ÇANTASINI KOMŞUYA ULAŞTIR",
            "SIKIŞAN SOKAK KAPISINI AÇ",
            "DEVRİLEN DİREKTEN UZAK ROTAYI SEÇ",
            "ACİL ARAÇ ŞERİDİNİ AÇ",
            "DEVRİLEN LEVHAYI YOLDAN ÇEK",
            "TOPLANMA ALANINDA SAYIM YAP"
        };
        string[] stageDetails =
        {
            "Düşebilecek cam ve sıva hattından açık tarafa çık.",
            "Koşma; binalardan uzakta dengeni koruyarak basılı tut.",
            "Kokuyu fark ettin. Vanayı yatay hareketle kapat.",
            "Geçmeden önce gevşek parçaya üç kez kontrollü dokun.",
            "Ağır parçayı turuncu güvenli boşluğa sürükle.",
            "Cephe dibi ve taşıt yolu yerine açık şeridi seç.",
            "Kimse basmadan önce tehlike bandını sabitle.",
            "Geniş tahtayı yeşil hedefe sürükleyip geçiş oluştur.",
            "Bilinç ve durum kontrolü için üç kez seslen.",
            "Kırmızı çantayı yeşil yardım alanına bırak.",
            "Kapıyı geçiş yönünde yatay hareketle aç.",
            "Enerjili hat ve eğik direğin uzağındaki yolu seç.",
            "Bariyeri yana çek; itfaiye güzergâhını boş bırak.",
            "Levhayı yol kenarındaki turuncu alana taşı.",
            "İşaret üzerinde basılı tut ve aile sayımını tamamla."
        };
        Color[] colors =
        {
            new Color32(244, 78, 68, 255), new Color32(255, 186, 45, 255),
            new Color32(244, 78, 68, 255), new Color32(255, 186, 45, 255),
            new Color32(255, 186, 45, 255), new Color32(61, 205, 133, 255),
            new Color32(81, 193, 224, 255), new Color32(61, 205, 133, 255),
            new Color32(244, 78, 68, 255), new Color32(61, 205, 133, 255),
            new Color32(53, 151, 224, 255), new Color32(255, 186, 45, 255),
            new Color32(53, 151, 224, 255), new Color32(255, 186, 45, 255),
            new Color32(61, 205, 133, 255)
        };

        GameObject[] stages = new GameObject[15];
        for (int index = 0; index < stages.Length; index++)
        {
            stages[index] = CreateStageCard(
                safeArea.transform, semibold, bold, index + 1, 15,
                stageTitles[index], stageDetails[index], colors[index]);
            stages[index].SetActive(index == 0);
        }

        GameObject gestureHint = StoryChapterBuilderCommon.CreatePanel(
            "GestureHint", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -166f), new Vector2(510f, 36f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        TMP_Text hint = StoryChapterBuilderCommon.CreateText(
            "HintText", gestureHint.transform, semibold, 13.5f,
            new Color32(32, 55, 80, 255), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(470f, 22f));
        hint.text = "DOKUN  •  BASILI TUT  •  ÇEK  •  SÜRÜKLE";

        GameObject bubbleObject = StoryChapterBuilderCommon.CreatePanel(
            "StreetChoiceFeedbackBubble", safeArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, -220f), new Vector2(920f, 145f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBluePanel);
        CanvasGroup bubbleGroup = bubbleObject.AddComponent<CanvasGroup>();
        TMP_Text bubbleText = StoryChapterBuilderCommon.CreateText(
            "ChoiceFeedback", bubbleObject.transform, semibold, 26f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(830f, 102f));
        bubbleText.enableAutoSizing = true;
        bubbleText.fontSizeMin = 19f;
        StairInfoBubbleUI bubble = bubbleObject.AddComponent<StairInfoBubbleUI>();
        bubble.canvasGroup = bubbleGroup;
        bubble.bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubble.messageText = bubbleText;
        bubble.hiddenPosition = new Vector2(0f, -220f);
        bubble.shownPosition = new Vector2(0f, 365f);
        bubble.hideOnAwake = true;

        GameObject successOverlay = StoryChapterBuilderCommon.CreatePanel(
            "SuccessOverlay_PostQuakeStreet", safeArea.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color32(7, 20, 38, 232), true);
        GameObject successCard = StoryChapterBuilderCommon.CreatePanel(
            "SuccessCard", successOverlay.transform,
            Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, 30f), new Vector2(880f, 750f),
            Color.white, true, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        TMP_Text successKicker = StoryChapterBuilderCommon.CreateText(
            "SuccessKicker", successCard.transform, bold, 29f,
            new Color32(40, 178, 111, 255), TextAlignmentOptions.Center,
            new Vector2(0f, 270f), new Vector2(740f, 44f));
        successKicker.text = "15 / 15 SOKAK OLAYI TAMAMLANDI";
        TMP_Text successTitle = StoryChapterBuilderCommon.CreateText(
            "SuccessTitle", successCard.transform, bold, 57f,
            new Color32(20, 39, 68, 255), TextAlignmentOptions.Center,
            new Vector2(0f, 155f), new Vector2(780f, 110f));
        successTitle.text = "TOPLANMA ALANINDASIN!";
        TMP_Text successDetail = StoryChapterBuilderCommon.CreateText(
            "SuccessDetail", successCard.transform, semibold, 27f,
            new Color32(58, 81, 102, 255), TextAlignmentOptions.Center,
            new Vector2(0f, 30f), new Vector2(740f, 160f));
        successDetail.text =
            "Hasarlı cephelerden ve enerji hatlarından uzak kaldın,\n" +
            "yaralıya yardım edip acil araç yolunu açık bıraktın.";
        GameObject reward = StoryChapterBuilderCommon.CreatePanel(
            "RewardBadge", successCard.transform,
            Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -125f), new Vector2(460f, 92f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulYellowBadge);
        TMP_Text rewardText = StoryChapterBuilderCommon.CreateText(
            "RewardText", reward.transform, bold, 32f,
            new Color32(25, 43, 68, 255), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(410f, 58f));
        rewardText.text = "+50 IMO COIN";

        GameObject bridgeObject = new GameObject("StreetSceneReloadBridge");
        bridgeObject.transform.SetParent(root);
        DoorMissionManager reload = bridgeObject.AddComponent<DoorMissionManager>();
        reload.nextSceneName = "Minigame_Evacuation_25D";
        Button restart = StoryChapterBuilderCommon.CreateButton(
            "RestartButton", successCard.transform, "TEKRAR OYNA", semibold,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -285f), new Vector2(520f, 108f),
            new Color32(60, 169, 244, 255), Color.white);
        UnityEventTools.AddPersistentListener(restart.onClick, reload.LoadNextScene);
        successOverlay.SetActive(false);

        return new UiRefs { stages = stages, bubble = bubble, successPanel = successOverlay };
    }

    private static GameObject CreateStageCard(
        Transform parent, TMP_FontAsset semibold, TMP_FontAsset bold,
        int index, int total, string title, string detail, Color badgeColor)
    {
        GameObject panel = StoryChapterBuilderCommon.CreatePanel(
            "Stage_" + index.ToString("00"), parent,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 114f), new Vector2(940f, 150f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        GameObject badge = StoryChapterBuilderCommon.CreatePanel(
            "Badge", panel.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(78f, 0f), new Vector2(108f, 92f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBlueBadge);
        Image badgeImage = badge.GetComponent<Image>();
        if (badgeImage != null)
            badgeImage.color = badgeColor;
        TMP_Text badgeText = StoryChapterBuilderCommon.CreateText(
            "BadgeText", badge.transform, bold, 22f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(94f, 56f));
        badgeText.text = index.ToString("00") + "/" + total;
        badgeText.enableAutoSizing = true;
        badgeText.fontSizeMin = 16f;
        TMP_Text titleText = StoryChapterBuilderCommon.CreateText(
            "StageTitle", panel.transform, bold, 25f,
            new Color32(20, 39, 68, 255), TextAlignmentOptions.Left,
            new Vector2(92f, 23f), new Vector2(690f, 48f));
        titleText.text = title;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 18f;
        TMP_Text detailText = StoryChapterBuilderCommon.CreateText(
            "StageDetail", panel.transform, semibold, 17.5f,
            new Color32(68, 89, 106, 255), TextAlignmentOptions.Left,
            new Vector2(92f, -28f), new Vector2(690f, 42f));
        detailText.text = detail;
        detailText.enableAutoSizing = true;
        detailText.fontSizeMin = 15f;
        return panel;
    }

    private static PuzzleRefs BuildPuzzle(
        Transform root,
        CameraRig cameras,
        WorldRefs world,
        CharacterRefs characters,
        UiRefs ui,
        Palette palette)
    {
        Transform systems = NewChild(root, "STREET_GAMEPLAY_MANAGERS");

        GameObject touchObject = new GameObject("StoryTouchManager_PostQuakeStreet");
        touchObject.transform.SetParent(systems);
        StoryTouchManager touch = touchObject.AddComponent<StoryTouchManager>();
        SerializedObject touchData = new SerializedObject(touch);
        RequireProperty(touchData, "worldCamera").objectReferenceValue = cameras.mainCamera;
        RequireProperty(touchData, "player").objectReferenceValue = characters.touchProxy;
        RequireProperty(touchData, "cameraController").objectReferenceValue = cameras.controller;
        RequireProperty(touchData, "directWorldGestures").boolValue = true;
        RequireProperty(touchData, "worldSwipeThreshold").floatValue = 58f;
        RequireProperty(touchData, "maxRayDistance").floatValue = 150f;
        touchData.ApplyModifiedPropertiesWithoutUndo();

        StoryInteractable facadeClear = CreateInteractable(
            world.facadeClear, "evac25d.street.01.facade_clear",
            "HASARLI CEPHEDEN AÇIK TARAFA ÇIK",
            StoryInteractionKind.Exit, StoryInteractionGesture.Tap,
            StoryCameraZoneId.RoomOverview, true, 1, 0.4f);
        PositionHighlight(facadeClear, new Vector3(-34.75f, 0.78f, -0.62f));

        StoryInteractable aftershock = CreateInteractable(
            world.aftershockSpot, "evac25d.street.02.aftershock",
            "ARTÇI BİTENE KADAR AÇIK NOKTADA BASILI TUT",
            StoryInteractionKind.TakeCover, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.RoomOverview, false, 1, 1.45f);
        ConfigureCameraReturn(aftershock, StoryCameraZoneId.EvacuationCorridor, 0.42f);

        StoryInteractable gasValve = CreateInteractable(
            world.gasValve, "evac25d.street.03.gas_valve",
            "GAZ VANASINI YATAY HAREKETLE KAPAT",
            StoryInteractionKind.Inspect, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationCorridor, false, 1, 0.8f);
        ConfigureCameraReturn(gasValve, StoryCameraZoneId.EvacuationStairDoor, 0.42f);

        StoryInteractable rubbleCheck = CreateInteractable(
            world.rubbleCheck, "evac25d.street.04.rubble_check",
            "GEVŞEK MOLOZA ÜÇ KEZ KONTROLLÜ DOKUN",
            StoryInteractionKind.Inspect, StoryInteractionGesture.RepeatedTap,
            StoryCameraZoneId.EvacuationStairDoor, false, 3, 0.95f);
        ConfigureCameraReturn(rubbleCheck, StoryCameraZoneId.EvacuationStairsTop, 0.42f);

        ConfigureDraggable(
            world.masonry, world.masonryDropZone, world.masonrySnap, world.masonryDragPlane, 0.24f);
        StoryInteractable masonry = CreateInteractable(
            world.masonry, "evac25d.street.05.masonry_drag",
            "BETON PARÇASINI YOL KENARINDAKİ DİREKLERİN ARASINA SÜRÜKLE",
            StoryInteractionKind.HelpSibling, StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.EvacuationStairsTop, false, 1, 1.1f, world.masonrySnap);
        ConfigureCameraReturn(masonry, StoryCameraZoneId.EvacuationLanding, 0.42f);

        StoryInteractable glassMark = CreateInteractable(
            world.glassMark, "evac25d.street.07.glass_mark",
            "KIRIK CAM ALANINI İŞARETLEMEK İÇİN BASILI TUT",
            StoryInteractionKind.Inspect, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationLanding, false, 1, 1.15f);
        ConfigureCameraReturn(glassMark, StoryCameraZoneId.EvacuationLowerLanding, 0.42f);

        ConfigureDraggable(
            world.glassCover, world.glassDropZone, world.glassSnap, world.glassDragPlane, 0.24f);
        StoryInteractable glassCover = CreateInteractable(
            world.glassCover, "evac25d.street.08.glass_cover",
            "TAHTAYI KIRIK CAMLARIN ÜZERİNE SÜRÜKLE",
            StoryInteractionKind.HelpSibling, StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.EvacuationLowerLanding, false, 1, 1.05f, world.glassSnap);
        ConfigureCameraReturn(glassCover, StoryCameraZoneId.EvacuationNeighbor, 0.42f);

        StoryInteractable injuredCheck = CreateInteractable(
            world.injuredCheck, "evac25d.street.09.injured_check",
            "YARALI KOMŞUYA ÜÇ KEZ SESLEN",
            StoryInteractionKind.HelpSibling, StoryInteractionGesture.RepeatedTap,
            StoryCameraZoneId.EvacuationNeighbor, false, 3, 0.9f);

        ConfigureDraggable(
            world.firstAid, world.firstAidDropZone, world.firstAidSnap, world.firstAidDragPlane, 0.26f);
        StoryInteractable firstAid = CreateInteractable(
            world.firstAid, "evac25d.street.10.first_aid",
            "İLK YARDIM ÇANTASINI YARALI KOMŞUYA SÜRÜKLE",
            StoryInteractionKind.HelpSibling, StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.EvacuationNeighbor, false, 1, 1.05f, world.firstAidSnap);
        ConfigureCameraReturn(firstAid, StoryCameraZoneId.EvacuationBuildingDoor, 0.42f);

        StoryInteractable gate = CreateInteractable(
            world.gate, "evac25d.street.11.gate",
            "SIKIŞAN KAPIYI GEÇİŞ YÖNÜNDE ÇEK",
            StoryInteractionKind.Exit, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationBuildingDoor, false, 1, 0.85f);
        ConfigureCameraReturn(gate, StoryCameraZoneId.EvacuationBuildingFront, 0.42f);

        StoryInteractable fireLane = CreateInteractable(
            world.fireLane, "evac25d.street.13.fire_lane",
            "BARİYERİ YANA ÇEK VE ACİL ARAÇ ŞERİDİNİ AÇ",
            StoryInteractionKind.Exit, StoryInteractionGesture.SwipeHorizontal,
            StoryCameraZoneId.EvacuationHazard, false, 1, 0.8f);
        ConfigureCameraReturn(fireLane, StoryCameraZoneId.EvacuationStreet, 0.42f);

        ConfigureDraggable(
            world.fallenSign, world.signDropZone, world.signSnap, world.signDragPlane, 0.24f);
        StoryInteractable fallenSign = CreateInteractable(
            world.fallenSign, "evac25d.street.14.fallen_sign",
            "LEVHAYI TURUNCU YOL KENARINA SÜRÜKLE",
            StoryInteractionKind.HelpSibling, StoryInteractionGesture.DragToTarget,
            StoryCameraZoneId.EvacuationStreet, false, 1, 1.05f, world.signSnap);
        ConfigureCameraReturn(fallenSign, StoryCameraZoneId.EvacuationAssembly, 0.42f);

        StoryInteractable assembly = CreateInteractable(
            world.beacon, "evac25d.street.15.assembly",
            "TOPLANMA İŞARETİNDE BASILI TUT VE SAYIM YAP",
            StoryInteractionKind.Exit, StoryInteractionGesture.WorldHold,
            StoryCameraZoneId.EvacuationAssembly, false, 1, 1.5f);

        GameObject firstChoiceRoot = new GameObject("06_SafeStreetSideChoices");
        firstChoiceRoot.transform.SetParent(systems);
        StairChoiceTarget unsafeFacade = CreateChoiceTarget(
            firstChoiceRoot.transform, "Choice_UnsafeFacadeSide",
            new Vector3(-12.65f, 1.32f, -0.72f),
            StairChoiceTarget.StairChoice.Elevator, palette.danger, "CEPHE", Color.white);
        StairChoiceTarget unsafeRoad = CreateChoiceTarget(
            firstChoiceRoot.transform, "Choice_UnsafeVehicleLane",
            new Vector3(-11.15f, 0.82f, -0.72f),
            StairChoiceTarget.StairChoice.MiddleStairs, palette.amber, "YOL",
            new Color32(34, 46, 64, 255));
        StairChoiceTarget safeOpenSide = CreateChoiceTarget(
            firstChoiceRoot.transform, "Choice_SafeOpenStreetSide",
            new Vector3(-9.65f, 1.32f, -0.72f),
            StairChoiceTarget.StairChoice.WallSide, palette.safe, "AÇIK", Color.white);

        GameObject firstManagerObject = new GameObject("06_SafeStreetChoiceManager");
        firstManagerObject.transform.SetParent(systems);
        StairChoiceManager firstChoice = firstManagerObject.AddComponent<StairChoiceManager>();
        firstChoice.raycastCamera = cameras.mainCamera;
        firstChoice.choicesEnabled = false;
        firstChoice.choiceTargets = new[] { unsafeFacade, unsafeRoad, safeOpenSide };
        firstChoice.choicesRoot = firstChoiceRoot;
        firstChoice.infoBubbleUI = ui.bubble;
        firstChoice.introMessage = "Cephe dibi, taşıt yolu ve açık taraf: hangisi güvenli?";
        firstChoice.elevatorMessage = "Hasarlı cepheden cam ve sıva düşebilir. Binadan uzaklaş.";
        firstChoice.middleStairsMessage = "Taşıt yolu acil araçlara ait. Açık yaya tarafını seç.";
        firstChoice.wallSideMessage = "Doğru! Binalardan uzak, açık tarafta ilerle.";
        firstChoice.pathCompletedMessage = "Güvenli sokak tarafı seçildi.";
        firstChoice.hideChoicesOnCorrect = true;
        firstChoice.stairPathWalker = null;
        firstChoice.onCorrectChoice = new UnityEvent();
        firstChoice.onWrongChoice = new UnityEvent();
        firstChoiceRoot.SetActive(false);

        GameObject secondChoiceRoot = new GameObject("12_UtilityPoleRouteChoices");
        secondChoiceRoot.transform.SetParent(systems);
        StairChoiceTarget unsafePole = CreateChoiceTarget(
            secondChoiceRoot.transform, "Choice_UnsafeLeaningPole",
            new Vector3(21.0f, 1.45f, -0.72f),
            StairChoiceTarget.StairChoice.Elevator, palette.danger, "DİREK", Color.white);
        StairChoiceTarget unsafeWire = CreateChoiceTarget(
            secondChoiceRoot.transform, "Choice_UnsafeLiveWire",
            new Vector3(22.65f, 0.78f, -0.72f),
            StairChoiceTarget.StairChoice.MiddleStairs, palette.amber, "HAT",
            new Color32(34, 46, 64, 255));
        StairChoiceTarget safeDetour = CreateChoiceTarget(
            secondChoiceRoot.transform, "Choice_SafeUtilityDetour",
            new Vector3(23.65f, 1.45f, -0.72f),
            StairChoiceTarget.StairChoice.WallSide, palette.safe, "DOLAN", Color.white);

        GameObject secondManagerObject = new GameObject("12_UtilityPoleChoiceManager");
        secondManagerObject.transform.SetParent(systems);
        StairChoiceManager secondChoice = secondManagerObject.AddComponent<StairChoiceManager>();
        secondChoice.raycastCamera = cameras.mainCamera;
        secondChoice.choicesEnabled = false;
        secondChoice.choiceTargets = new[] { unsafePole, unsafeWire, safeDetour };
        secondChoice.choicesRoot = secondChoiceRoot;
        secondChoice.infoBubbleUI = ui.bubble;
        secondChoice.introMessage = "Eğik direk ve enerjili hat çevresinde hangi rota güvenli?";
        secondChoice.elevatorMessage = "Direk artçıda devrilebilir. Altından geçme.";
        secondChoice.middleStairsMessage = "Kablo enerjili olabilir. Yaklaşma ve dokunma.";
        secondChoice.wallSideMessage = "Doğru! Geniş bir yay çizerek tehlikeden uzak geç.";
        secondChoice.pathCompletedMessage = "Enerji hattı güvenle geride kaldı.";
        secondChoice.hideChoicesOnCorrect = true;
        secondChoice.stairPathWalker = null;
        secondChoice.onCorrectChoice = new UnityEvent();
        secondChoice.onWrongChoice = new UnityEvent();
        secondChoiceRoot.SetActive(false);

        return new PuzzleRefs
        {
            touch = touch,
            facadeClear = facadeClear,
            aftershock = aftershock,
            gasValve = gasValve,
            rubbleCheck = rubbleCheck,
            masonry = masonry,
            firstChoice = firstChoice,
            firstChoiceRoot = firstChoiceRoot,
            glassMark = glassMark,
            glassCover = glassCover,
            injuredCheck = injuredCheck,
            firstAid = firstAid,
            gate = gate,
            secondChoice = secondChoice,
            secondChoiceRoot = secondChoiceRoot,
            fireLane = fireLane,
            fallenSign = fallenSign,
            assembly = assembly
        };
    }

    private static void WireProgression(
        CameraRig cameras,
        WorldRefs world,
        CharacterRefs characters,
        UiRefs ui,
        PuzzleRefs puzzle,
        MinigameSessionManager resultReporter)
    {
        if (cameras.controller == null)
            throw new InvalidOperationException("Street camera controller is missing.");

        UnityEventTools.AddPersistentListener(
            puzzle.firstChoice.onWrongChoice, resultReporter.ReportExternalMistake);
        UnityEventTools.AddPersistentListener(
            puzzle.secondChoice.onWrongChoice, resultReporter.ReportExternalMistake);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.facadeClear.OnInteracted, world.facadeDanger.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.facadeClear.OnInteracted, world.facadeSafe.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.facadeClear.OnInteracted, world.facadeDust.gameObject.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.facadeClear.OnInteracted, puzzle.aftershock.SetAvailable, true);
        WireStage(puzzle.facadeClear.OnInteracted, ui, 0, 1);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.aftershock.OnInteracted, world.aftershockUnsafe.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.aftershock.OnInteracted, world.aftershockSafe.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.aftershock.OnInteracted, world.aftershockDust.gameObject.SetActive, false);
        WireStage(puzzle.aftershock.OnInteracted, ui, 1, 2);
        WireMovement(puzzle.aftershock.OnInteracted, characters, 0);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[0].onPathCompleted, puzzle.gasValve.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.gasValve.OnInteracted, world.gasLeak.gameObject.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.gasValve.OnInteracted, world.gasOpen.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.gasValve.OnInteracted, world.gasClosed.SetActive, true);
        WireStage(puzzle.gasValve.OnInteracted, ui, 2, 3);
        WireMovement(puzzle.gasValve.OnInteracted, characters, 1);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[1].onPathCompleted, puzzle.rubbleCheck.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.rubbleCheck.OnInteracted, world.rubbleBlocked.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.rubbleCheck.OnInteracted, world.rubbleCleared.SetActive, true);
        WireStage(puzzle.rubbleCheck.OnInteracted, ui, 3, 4);
        WireMovement(puzzle.rubbleCheck.OnInteracted, characters, 2);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[2].onPathCompleted, puzzle.masonry.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.masonry.OnInteracted, world.masonry.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.masonry.OnInteracted, world.masonryCleared.SetActive, true);
        WireStage(puzzle.masonry.OnInteracted, ui, 4, 5);
        UnityEventTools.AddPersistentListener(
            puzzle.masonry.OnInteracted, puzzle.firstChoice.StartMission);

        WireStage(puzzle.firstChoice.onCorrectChoice, ui, 5, 6);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.firstChoice.onCorrectChoice, puzzle.firstChoiceRoot.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.firstChoice.onCorrectChoice, puzzle.glassMark.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.glassMark.OnInteracted, world.glassUnmarked.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.glassMark.OnInteracted, world.glassMarked.SetActive, true);
        WireStage(puzzle.glassMark.OnInteracted, ui, 6, 7);
        WireMovement(puzzle.glassMark.OnInteracted, characters, 3);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[3].onPathCompleted, puzzle.glassCover.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.glassCover.OnInteracted, world.glassCover.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.glassCover.OnInteracted, world.glassCovered.SetActive, true);
        WireStage(puzzle.glassCover.OnInteracted, ui, 7, 8);
        WireMovement(puzzle.glassCover.OnInteracted, characters, 4);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[4].onPathCompleted, puzzle.injuredCheck.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.injuredCheck.OnInteracted, world.injuredAlert.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.injuredCheck.OnInteracted, world.injuredChecked.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.injuredCheck.OnInteracted, puzzle.firstAid.SetAvailable, true);
        WireStage(puzzle.injuredCheck.OnInteracted, ui, 8, 9);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.firstAid.OnInteracted, world.firstAid.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.firstAid.OnInteracted, world.firstAidDelivered.SetActive, true);
        WireStage(puzzle.firstAid.OnInteracted, ui, 9, 10);
        WireMovement(puzzle.firstAid.OnInteracted, characters, 5);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[5].onPathCompleted, puzzle.gate.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.gate.OnInteracted, world.gateClosed.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.gate.OnInteracted, world.gateOpen.SetActive, true);
        WireStage(puzzle.gate.OnInteracted, ui, 10, 11);
        WireMovement(puzzle.gate.OnInteracted, characters, 6);
        UnityEventTools.AddPersistentListener(
            characters.denizSegments[6].onPathCompleted, puzzle.secondChoice.StartMission);

        WireStage(puzzle.secondChoice.onCorrectChoice, ui, 11, 12);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.secondChoice.onCorrectChoice, puzzle.secondChoiceRoot.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.secondChoice.onCorrectChoice, puzzle.fireLane.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.fireLane.OnInteracted, world.fireLaneBlocked.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.fireLane.OnInteracted, world.fireLaneClear.SetActive, true);
        WireStage(puzzle.fireLane.OnInteracted, ui, 12, 13);
        WireMovement(puzzle.fireLane.OnInteracted, characters, 7);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[7].onPathCompleted, puzzle.fallenSign.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.fallenSign.OnInteracted, world.fallenSign.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.fallenSign.OnInteracted, world.signCleared.SetActive, true);
        WireStage(puzzle.fallenSign.OnInteracted, ui, 13, 14);
        WireMovement(puzzle.fallenSign.OnInteracted, characters, 8);
        UnityEventTools.AddBoolPersistentListener(
            characters.denizSegments[8].onPathCompleted, puzzle.assembly.SetAvailable, true);

        UnityEventTools.AddBoolPersistentListener(
            puzzle.assembly.OnInteracted, world.beaconOff.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.assembly.OnInteracted, world.beaconOn.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.assembly.OnInteracted, ui.stages[14].SetActive, false);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.assembly.OnInteracted, ui.successPanel.SetActive, true);
        UnityEventTools.AddPersistentListener(puzzle.assembly.OnInteracted, ui.bubble.Hide);
        UnityEventTools.AddBoolPersistentListener(
            puzzle.assembly.OnInteracted, world.confetti.Play, true);
        UnityEventTools.AddPersistentListener(
            puzzle.assembly.OnInteracted, resultReporter.CompleteExternalEvacuation25D);
    }

    private static MinigameSessionManager BuildResultReporter(Transform parent)
    {
        GameObject reporterObject = new GameObject("_MinigameResultReporter");
        reporterObject.transform.SetParent(parent);
        MinigameProgressManager progress = reporterObject.AddComponent<MinigameProgressManager>();
        MinigameSessionManager reporter = reporterObject.AddComponent<MinigameSessionManager>();
        SerializedObject data = new SerializedObject(reporter);
        data.FindProperty("minigameId").stringValue = "evacuation-25d";
        data.FindProperty("displayName").stringValue = "GÜVENLİ TAHLİYE 2.5D";
        data.FindProperty("externalResultOnly").boolValue = true;
        data.FindProperty("progressManager").objectReferenceValue = progress;
        data.ApplyModifiedPropertiesWithoutUndo();
        return reporter;
    }

    private static void WireStage(UnityEvent source, UiRefs ui, int current, int next)
    {
        UnityEventTools.AddBoolPersistentListener(source, ui.stages[current].SetActive, false);
        UnityEventTools.AddBoolPersistentListener(source, ui.stages[next].SetActive, true);
    }

    private static void WireMovement(UnityEvent source, CharacterRefs characters, int segment)
    {
        if (segment < 0 || segment >= characters.denizSegments.Length ||
            segment >= characters.canSegments.Length)
            throw new ArgumentOutOfRangeException(nameof(segment));
        UnityEventTools.AddPersistentListener(
            source, characters.denizSegments[segment].StartWalkingPath);
        UnityEventTools.AddPersistentListener(
            source, characters.canSegments[segment].StartWalkingPath);
    }

    private static StoryInteractable CreateInteractable(
        GameObject visualRoot,
        string id,
        string prompt,
        StoryInteractionKind kind,
        StoryInteractionGesture gesture,
        StoryCameraZoneId cameraZone,
        bool availableOnStart,
        int gestureCount,
        float interactionSeconds,
        Transform gestureTarget = null)
    {
        Transform point = NewChild(visualRoot.transform, "InteractionPoint_Street25D");
        point.position = visualRoot.transform.position;
        StoryInteractable interactable = StoryChapterBuilderCommon.AddInteractable(
            visualRoot, id, prompt, kind, point, gesture, cameraZone,
            true, gestureCount, interactionSeconds, 12f);
        ConfigureAvailability(interactable, availableOnStart, true);
        SerializedObject data = new SerializedObject(interactable);
        if (gestureTarget != null)
            RequireProperty(data, "gestureTarget").objectReferenceValue = gestureTarget;
        SerializedProperty highlight = RequireProperty(data, "highlightRoot");
        GameObject generatedMarker = highlight.objectReferenceValue as GameObject;
        // The generic circular hand marker reads like an editor/debug widget in this
        // grounded street scene. Interaction remains fully discoverable through the
        // stage card, physical props and the large invisible mobile hotspot.
        highlight.objectReferenceValue = null;
        data.ApplyModifiedPropertiesWithoutUndo();
        if (generatedMarker != null)
            Object.DestroyImmediate(generatedMarker);
        return interactable;
    }

    private static void ConfigureAvailability(
        StoryInteractable interactable, bool availableOnStart, bool oneShot)
    {
        SerializedObject data = new SerializedObject(interactable);
        RequireProperty(data, "availableOnStart").boolValue = availableOnStart;
        RequireProperty(data, "oneShot").boolValue = oneShot;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCameraReturn(
        StoryInteractable interactable, StoryCameraZoneId returnZone, float lingerSeconds)
    {
        SerializedObject data = new SerializedObject(interactable);
        RequireProperty(data, "returnCameraAfterCompletion").boolValue = true;
        RequireProperty(data, "returnCameraZone").intValue = (int)returnZone;
        RequireProperty(data, "focusLingerSeconds").floatValue = Mathf.Max(0f, lingerSeconds);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PositionHighlight(StoryInteractable interactable, Vector3 worldPosition)
    {
        SerializedObject data = new SerializedObject(interactable);
        SerializedProperty property = RequireProperty(data, "highlightRoot");
        if (property.objectReferenceValue is GameObject marker)
            marker.transform.position = worldPosition;
    }

    private static void ConfigureDraggable(
        GameObject item,
        BagDropZone dropZone,
        Transform snap,
        Transform dragPlane,
        float snapRadius)
    {
        DraggableItem draggable = item.GetComponent<DraggableItem>() ??
                                  item.AddComponent<DraggableItem>();
        SerializedObject data = new SerializedObject(draggable);
        RequireProperty(data, "isCorrectItem").boolValue = true;
        RequireProperty(data, "inputEnabled").boolValue = false;
        RequireProperty(data, "notifyGameManager").boolValue = false;
        RequireProperty(data, "dragLift").floatValue = 0.03f;
        RequireProperty(data, "dragOnCameraPlane").boolValue = true;
        RequireProperty(data, "dragPlaneAnchor").objectReferenceValue = dragPlane;
        RequireProperty(data, "faceCameraWhileDragging").boolValue = false;
        RequireProperty(data, "magneticSnapViewportRadius").floatValue = snapRadius;
        RequireProperty(data, "magneticSnapMinTravelPixels").floatValue = 28f;
        RequireProperty(data, "magneticSnapStrength").floatValue = 1f;
        RequireProperty(data, "magneticSnapResponse").floatValue = 12f;
        RequireProperty(data, "magneticSnapSurfaceOffset").floatValue = 0f;
        RequireProperty(data, "tapToBagEnabled").boolValue = false;
        RequireProperty(data, "dropZoneOverride").objectReferenceValue = dropZone;
        RequireProperty(data, "hideItemInBag").boolValue = false;
        RequireProperty(data, "returnDuration").floatValue = 0.32f;
        SerializedProperty targets = RequireProperty(data, "magneticSnapTargets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = snap;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateScene(Scene scene)
    {
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException("Post-quake street scene is not the active saved scene.");

        Camera camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
            .Single(candidate => candidate.name == "Main Camera");
        if (!camera.orthographic)
            throw new InvalidOperationException("The street minigame camera must be orthographic.");
        if (Vector3.Angle(camera.transform.forward, Vector3.forward) > 0.1f)
            throw new InvalidOperationException("The camera must remain exactly side-on along +Z.");
        if (camera.orthographicSize > 3.4f || Mathf.Abs(camera.transform.eulerAngles.z) > 0.01f)
            throw new InvalidOperationException("The platformer shot must remain close and level.");

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform spanStart = transforms.FirstOrDefault(item => item.name == "WorldSpan_Start");
        Transform spanEnd = transforms.FirstOrDefault(item => item.name == "WorldSpan_End");
        if (spanStart == null || spanEnd == null ||
            Mathf.Abs(spanEnd.position.x - spanStart.position.x) < 80f)
            throw new InvalidOperationException("The street route must span at least 80 world units.");

        Transform sidewalk = transforms.FirstOrDefault(item => item.name == "StreetSidewalk_Run");
        Transform road = transforms.FirstOrDefault(item => item.name == "StreetRoadFront_Run");
        if (sidewalk == null || road == null || sidewalk.localScale.x < 85f ||
            sidewalk.localScale.z < 3.8f || road.localScale.x < 85f)
            throw new InvalidOperationException("The route needs one physically continuous street and sidewalk.");
        if (transforms.Count(item => item.name.StartsWith("DamagedFacadeBuilding_", StringComparison.Ordinal)) < 9)
            throw new InvalidOperationException("The street needs a continuous damaged city facade.");

        CinemachineCamera[] virtualCameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (virtualCameras.Length < 12)
            throw new InvalidOperationException("The route needs twelve authored street camera zones.");
        if (virtualCameras.Any(vcam =>
                vcam.Lens.ModeOverride != LensSettings.OverrideModes.Orthographic ||
                vcam.Lens.OrthographicSize > 3.4f ||
                Vector3.Angle(vcam.transform.forward, Vector3.forward) > 0.1f))
            throw new InvalidOperationException("Every street camera must be close, orthographic and side-on.");

        StoryCameraController cameraController = Object.FindFirstObjectByType<StoryCameraController>();
        if (cameraController == null)
            throw new InvalidOperationException("The street camera controller is missing.");
        SerializedObject cameraControllerData = new SerializedObject(cameraController);
        SerializedProperty cameraBindings = RequireProperty(cameraControllerData, "cameras");
        Dictionary<StoryCameraZoneId, CinemachineCamera> cameraByZone =
            new Dictionary<StoryCameraZoneId, CinemachineCamera>();
        for (int index = 0; index < cameraBindings.arraySize; index++)
        {
            SerializedProperty binding = cameraBindings.GetArrayElementAtIndex(index);
            StoryCameraZoneId zone = (StoryCameraZoneId)binding.FindPropertyRelative("zone").intValue;
            CinemachineCamera zoneCamera =
                binding.FindPropertyRelative("camera").objectReferenceValue as CinemachineCamera;
            if (zoneCamera != null)
                cameraByZone[zone] = zoneCamera;
        }

        void RequireVisibleInPortrait(string label, Vector3 worldPoint, CinemachineCamera zoneCamera)
        {
            const float portraitAspect = 9f / 16f;
            const float safeEdge = 0.14f;
            float halfWidth = zoneCamera.Lens.OrthographicSize * portraitAspect;
            if (Mathf.Abs(worldPoint.x - zoneCamera.transform.position.x) > halfWidth - safeEdge)
                throw new InvalidOperationException(
                    label + " is outside the playable portrait framing of " + zoneCamera.name + ".");
        }

        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        string[] requiredIds =
        {
            "evac25d.street.01.facade_clear",
            "evac25d.street.02.aftershock",
            "evac25d.street.03.gas_valve",
            "evac25d.street.04.rubble_check",
            "evac25d.street.05.masonry_drag",
            "evac25d.street.07.glass_mark",
            "evac25d.street.08.glass_cover",
            "evac25d.street.09.injured_check",
            "evac25d.street.10.first_aid",
            "evac25d.street.11.gate",
            "evac25d.street.13.fire_lane",
            "evac25d.street.14.fallen_sign",
            "evac25d.street.15.assembly"
        };
        if (interactions.Length != requiredIds.Length ||
            requiredIds.Any(id => interactions.All(item => item.InteractionId != id)))
            throw new InvalidOperationException("The authored 13 interactions do not match the street route.");
        if (interactions.Any(item =>
                item.InteractionId.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Prompt.IndexOf("suyu", StringComparison.OrdinalIgnoreCase) >= 0))
            throw new InvalidOperationException("A water-diversion interaction leaked into the street game.");
        if (interactions.Any(item => item.HighlightRoot != null))
            throw new InvalidOperationException("Generic circular objective markers must not appear in the street scene.");
        foreach (StoryInteractable interaction in interactions)
        {
            if (!cameraByZone.TryGetValue(interaction.FocusCameraZone, out CinemachineCamera zoneCamera))
                throw new InvalidOperationException(
                    interaction.InteractionId + " has no authored camera-zone binding.");
            RequireVisibleInPortrait(
                interaction.InteractionId + " interaction point",
                interaction.InteractionPoint.position,
                zoneCamera);
            if (interaction.GestureTarget != null)
            {
                RequireVisibleInPortrait(
                    interaction.InteractionId + " gesture target",
                    interaction.GestureTarget.position,
                    zoneCamera);
            }
        }
        if (transforms.Any(item =>
                item.name == "UnsafeWaterTowardElectric" ||
                item.name == "SafeWaterToDrain" ||
                item.name.IndexOf("WaterValve", StringComparison.OrdinalIgnoreCase) >= 0))
            throw new InvalidOperationException("Legacy water-puzzle objects are still present.");
        string[] placeholderTokens =
        {
            "DistantCityBlock_", "SafeDistancePad", "AftershockRing_",
            "RubbleSafeMarker", "GroundPad", "TargetChevron", "ChoiceBadge",
            "NeighborSeatCushion"
        };
        if (transforms.Any(item => placeholderTokens.Any(token =>
                item.name.IndexOf(token, StringComparison.Ordinal) >= 0)))
            throw new InvalidOperationException("A debug pad, circle or blockout backdrop leaked into the final scene.");
        string[] allowedDiegeticLabels = { "YARDIM NOKTASI", "AİLE" };
        TextMeshPro[] worldLabels = Object.FindObjectsByType<TextMeshPro>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (worldLabels.Any(label => !allowedDiegeticLabels.Contains(label.text)))
            throw new InvalidOperationException("Non-diegetic debug text is still visible in the street world.");

        if (Object.FindObjectsByType<StairChoiceManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 2 ||
            Object.FindObjectsByType<StairChoiceTarget>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 6)
            throw new InvalidOperationException("Two three-way street route choices are required.");
        CinemachineCamera firstChoiceCamera = virtualCameras.Single(item => item.name == "CM_05_SafeSideChoice");
        CinemachineCamera utilityChoiceCamera = virtualCameras.Single(item => item.name == "CM_09_UtilityPoleChoice");
        foreach (StairChoiceTarget target in Object.FindObjectsByType<StairChoiceTarget>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            CinemachineCamera choiceCamera = target.transform.IsChildOf(
                transforms.Single(item => item.name == "06_SafeStreetSideChoices"))
                ? firstChoiceCamera
                : utilityChoiceCamera;
            RequireVisibleInPortrait(target.name, target.transform.position, choiceCamera);
        }
        if (Object.FindObjectsByType<DraggableItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 4 ||
            Object.FindObjectsByType<BagDropZone>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 4)
            throw new InvalidOperationException("Four physical street drag puzzles are required.");

        StairPathWalker[] walkers = Object.FindObjectsByType<StairPathWalker>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (walkers.Length != 18)
            throw new InvalidOperationException("Both children need all nine street movement legs.");
        foreach (StairPathWalker walker in walkers)
        {
            if (walker.pathPoints == null || walker.pathPoints.Length < 3)
                throw new InvalidOperationException(walker.name + " has an incomplete street path.");
            if (walker.pathPoints.Any(point =>
                    point == null || Mathf.Abs(point.position.z) > 0.95f ||
                    Mathf.Abs(point.position.y - WalkY) > 0.08f))
                throw new InvalidOperationException(walker.name + " leaves the single-level 2.5D street plane.");
        }

        foreach (string actorName in new[] { "Deniz_Street25D_Actor", "Can_Street25D_Actor" })
        {
            Transform actor = transforms.FirstOrDefault(item => item.name == actorName);
            if (actor == null)
                throw new InvalidOperationException(actorName + " is missing.");
            Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled &&
                                   renderer is not ParticleSystemRenderer &&
                                   renderer is not TrailRenderer &&
                                   renderer is not LineRenderer)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(actorName + " has no visible character renderer.");
            float lowestPoint = renderers.Min(renderer => renderer.bounds.min.y);
            if (Mathf.Abs(lowestPoint - (SidewalkTopY - 0.004f)) > 0.025f)
                throw new InvalidOperationException(actorName + " is not planted on the sidewalk.");
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.applyRootMotion || animator.stabilizeFeet)
                throw new InvalidOperationException(actorName + " has unsafe idle/root-motion settings.");
        }

        Transform injuredNeighbor = transforms.FirstOrDefault(item => item.name == "InjuredNeighbor_Street25D");
        Animator injuredAnimator = injuredNeighbor != null
            ? injuredNeighbor.GetComponentInChildren<Animator>(true)
            : null;
        if (injuredAnimator == null || injuredAnimator.enabled ||
            injuredAnimator.runtimeAnimatorController == null ||
            injuredAnimator.runtimeAnimatorController.name != "StoryAdultInjuredAnimator")
        {
            throw new InvalidOperationException(
                "The injured neighbor must use the frozen seated pose instead of a hovering idle.");
        }

        for (int index = 1; index <= 15; index++)
        {
            string name = "Stage_" + index.ToString("00");
            if (transforms.All(item => item.name != name))
                throw new InvalidOperationException(name + " UI card is missing.");
        }
        if (Object.FindFirstObjectByType<StoryTouchManager>() == null)
            throw new InvalidOperationException("The existing StoryTouchManager is missing.");
    }

    private static GameObject CreateDraggableRubble(
        Transform parent, string name, Vector3 feetPosition, Material concrete, Material wood)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = feetPosition;
        CreatePrimitive("ConcreteChunk_A", PrimitiveType.Cube,
            feetPosition + new Vector3(-0.25f, 0.35f, 0f), new Vector3(0.68f, 0.62f, 0.52f),
            concrete, root.transform, false, Quaternion.Euler(8f, 12f, -8f));
        CreatePrimitive("ConcreteChunk_B", PrimitiveType.Cube,
            feetPosition + new Vector3(0.28f, 0.28f, 0.04f), new Vector3(0.58f, 0.5f, 0.48f),
            concrete, root.transform, false, Quaternion.Euler(-4f, -8f, 15f));
        CreatePrimitive("Rebar", PrimitiveType.Cylinder,
            feetPosition + new Vector3(0.14f, 0.65f, -0.2f), new Vector3(0.055f, 0.46f, 0.055f),
            wood, root.transform, false, Quaternion.Euler(0f, 0f, -52f));
        EnsureRootBoxCollider(root).isTrigger = false;
        return root;
    }

    private static GameObject CreateBoard(
        Transform parent, string name, Vector3 feetPosition, Vector3 size, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = feetPosition;
        CreatePrimitive("BoardVisual", PrimitiveType.Cube,
            feetPosition + Vector3.up * (size.y * 0.5f), size,
            material, root.transform, false, Quaternion.Euler(0f, 0f, -5f));
        CreatePrimitive("BoardBrace_A", PrimitiveType.Cube,
            feetPosition + new Vector3(-size.x * 0.22f, size.y * 0.56f, -size.z * 0.53f),
            new Vector3(0.1f, size.y * 0.85f, 0.06f),
            material, root.transform, false);
        CreatePrimitive("BoardBrace_B", PrimitiveType.Cube,
            feetPosition + new Vector3(size.x * 0.22f, size.y * 0.56f, -size.z * 0.53f),
            new Vector3(0.1f, size.y * 0.85f, 0.06f),
            material, root.transform, false);
        EnsureRootBoxCollider(root).isTrigger = false;
        return root;
    }

    private static GameObject CreateFirstAidKit(
        Transform parent, string name, Vector3 feetPosition, Palette palette)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = feetPosition;
        CreatePrimitive("FirstAidCase", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.38f, 0f), new Vector3(0.82f, 0.65f, 0.42f),
            palette.danger, root.transform, false);
        CreatePrimitive("FirstAidHandle", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.77f, 0f), new Vector3(0.38f, 0.12f, 0.16f),
            palette.facadeDark, root.transform, false);
        CreatePrimitive("FirstAidCross_H", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.4f, -0.23f), new Vector3(0.42f, 0.13f, 0.05f),
            palette.common.white, root.transform, false);
        CreatePrimitive("FirstAidCross_V", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.4f, -0.24f), new Vector3(0.13f, 0.42f, 0.05f),
            palette.common.white, root.transform, false);
        EnsureRootBoxCollider(root).isTrigger = false;
        return root;
    }

    private static void CreateDropTarget(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 size,
        Vector3 snapPosition,
        Material material,
        out BagDropZone dropZone,
        out Transform snap)
    {
        GameObject target = new GameObject(name);
        target.transform.SetParent(parent);
        target.transform.position = center;
        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        dropZone = target.AddComponent<BagDropZone>();
        // Drop zones are generous invisible volumes. Only roadside disposal targets get
        // diegetic cones/bollards; glass and first-aid targets are the hazard/person itself.
        if (name.Contains("Masonry", StringComparison.Ordinal))
        {
            BuildSafetyBollard(target.transform, "RoadsideBay_Left",
                new Vector3(center.x - size.x * 0.4f, SidewalkTopY, center.z),
                material, null);
            BuildSafetyBollard(target.transform, "RoadsideBay_Right",
                new Vector3(center.x + size.x * 0.4f, SidewalkTopY, center.z),
                material, null);
        }
        else if (name.Contains("FallenSign", StringComparison.Ordinal))
        {
            InstantiateCity("Prop_RoadCone_03.prefab", "RoadsideSignClearanceCone",
                target.transform,
                new Vector3(center.x + size.x * 0.32f, WalkY, center.z),
                new Vector3(0.38f, 0.58f, 0.38f), Vector3.zero);
        }
        snap = NewChild(target.transform, "SnapPoint");
        snap.position = snapPosition;
        snap.rotation = Quaternion.identity;
    }

    private static Transform CreateDragPlane(Transform parent, string name, Vector3 position)
    {
        Transform plane = NewChild(parent, name);
        plane.position = position;
        plane.rotation = Quaternion.identity;
        return plane;
    }

    private static GameObject BuildValveState(
        Transform parent, string name, Vector3 position, Material material, float angle)
    {
        GameObject state = new GameObject(name);
        state.transform.SetParent(parent);
        state.transform.position = position;
        CreatePrimitive("ValveHub", PrimitiveType.Cylinder, position,
            new Vector3(0.18f, 0.07f, 0.18f), material, state.transform, false,
            Quaternion.Euler(90f, 0f, 0f));
        CreatePrimitive("ValveSpoke_A", PrimitiveType.Cube, position,
            new Vector3(0.7f, 0.08f, 0.08f), material, state.transform, false,
            Quaternion.Euler(0f, 0f, angle));
        CreatePrimitive("ValveSpoke_B", PrimitiveType.Cube, position,
            new Vector3(0.08f, 0.7f, 0.08f), material, state.transform, false,
            Quaternion.Euler(0f, 0f, angle));
        return state;
    }

    private static void BuildBrokenGlassField(
        Transform parent, Vector3 center, Material material, int count)
    {
        for (int index = 0; index < count; index++)
        {
            float offsetX = ((index * 37) % 100) / 100f * 1.75f - 0.875f;
            float offsetZ = ((index * 61) % 100) / 100f * 0.75f - 0.375f;
            float width = 0.18f + (index % 4) * 0.045f;
            CreatePrimitive("GlassShard_" + index.ToString("00"), PrimitiveType.Cube,
                center + new Vector3(offsetX, 0.025f + (index % 3) * 0.008f, offsetZ),
                new Vector3(width, 0.035f, 0.11f), material, parent, false,
                Quaternion.Euler(0f, (index * 29) % 180, (index * 17) % 75));
        }
    }

    private static void BuildRubblePile(
        Transform parent, string name, Vector3 feetPosition, float width,
        Material concrete, Material wood)
    {
        Transform root = NewChild(parent, name);
        for (int index = 0; index < 8; index++)
        {
            float normalized = index / 7f;
            float x = feetPosition.x + (normalized - 0.5f) * width * 1.55f;
            float y = feetPosition.y + 0.12f + (index % 3) * 0.13f;
            float z = feetPosition.z + ((index * 43) % 7 - 3) * 0.07f;
            CreatePrimitive("Rubble_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(x, y, z),
                new Vector3(0.28f + (index % 2) * 0.12f, 0.22f + (index % 3) * 0.08f, 0.3f),
                index == 6 ? wood : concrete, root, false,
                Quaternion.Euler(index * 7f, index * 13f, index % 2 == 0 ? 14f : -11f));
        }
    }

    private static void BuildBrokenWindow(
        Transform parent, string name, Vector3 center, Palette palette)
    {
        Transform root = NewChild(parent, name);
        CreatePrimitive("WindowVoid", PrimitiveType.Cube, center,
            new Vector3(0.92f, 0.96f, 0.08f), palette.facadeDark, root, false);
        for (int index = 0; index < 5; index++)
        {
            float x = center.x - 0.35f + index * 0.175f;
            CreatePrimitive("HangingGlass_" + index.ToString("00"), PrimitiveType.Cube,
                new Vector3(x, center.y + (index % 2 == 0 ? 0.28f : -0.27f), center.z - 0.08f),
                new Vector3(0.12f, 0.22f + (index % 3) * 0.065f, 0.04f),
                palette.glass, root, false,
                Quaternion.Euler(0f, 0f, -18f + index * 9f));
        }
        BuildFacadeCrack(root, "WindowCrack", center + new Vector3(0.34f, -0.12f, -0.1f),
            palette.common.cream, true);
    }

    private static void BuildFacadeCrack(
        Transform parent, string name, Vector3 origin, Material material, bool mirror)
    {
        Transform root = NewChild(parent, name);
        float sign = mirror ? -1f : 1f;
        Vector3 a = origin;
        Vector3 b = origin + new Vector3(0.34f * sign, -0.48f, 0f);
        Vector3 c = b + new Vector3(-0.2f * sign, -0.42f, 0f);
        Vector3 d = b + new Vector3(0.42f * sign, -0.22f, 0f);
        BuildBeam("CrackMain_A", root, a, b, 0.055f, material);
        BuildBeam("CrackMain_B", root, b, c, 0.05f, material);
        BuildBeam("CrackBranch", root, b, d, 0.045f, material);
    }

    private static GameObject BuildBeam(
        string name, Transform parent, Vector3 start, Vector3 end,
        float thickness, Material material)
    {
        Vector3 direction = end - start;
        GameObject beam = CreatePrimitive(name, PrimitiveType.Cube,
            (start + end) * 0.5f,
            new Vector3(thickness, direction.magnitude, thickness),
            material, parent, false);
        if (direction.sqrMagnitude > 0.0001f)
            beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        return beam;
    }

    private static void BuildSafetyBollard(
        Transform parent, string name, Vector3 feetPosition,
        Material bodyMaterial, Material bandMaterial)
    {
        Transform root = NewChild(parent, name);
        CreatePrimitive("RubberBase", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.035f, 0f),
            new Vector3(0.22f, 0.07f, 0.2f), bodyMaterial, root, false);
        CreatePrimitive("FlexiblePost", PrimitiveType.Cube,
            feetPosition + new Vector3(0f, 0.36f, 0f),
            new Vector3(0.11f, 0.65f, 0.1f), bodyMaterial, root, false);
        if (bandMaterial != null)
        {
            CreatePrimitive("ReflectiveBand", PrimitiveType.Cube,
                feetPosition + new Vector3(0f, 0.48f, -0.055f),
                new Vector3(0.14f, 0.11f, 0.025f), bandMaterial, root, false);
        }
    }

    private static void BuildGroundChevron(
        Transform parent, string name, Vector3 center, Material material, float size)
    {
        Transform root = NewChild(parent, name);
        CreatePrimitive("ChevronLeft", PrimitiveType.Cube,
            center + new Vector3(-size * 0.18f, 0f, 0f),
            new Vector3(size * 0.55f, 0.07f, 0.06f), material, root, false,
            Quaternion.Euler(0f, 0f, -28f));
        CreatePrimitive("ChevronRight", PrimitiveType.Cube,
            center + new Vector3(size * 0.18f, 0f, 0f),
            new Vector3(size * 0.55f, 0.07f, 0.06f), material, root, false,
            Quaternion.Euler(0f, 0f, 28f));
    }

    private static StairChoiceTarget CreateChoiceTarget(
        Transform parent,
        string name,
        Vector3 position,
        StairChoiceTarget.StairChoice choice,
        Material material,
        string symbol,
        Color symbolColor)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.62f;
        StairChoiceTarget target = root.AddComponent<StairChoiceTarget>();
        target.choice = choice;
        if (choice == StairChoiceTarget.StairChoice.Elevator)
        {
            BuildSafetyBollard(root.transform, "BlockedRoute_Left",
                new Vector3(position.x - 0.34f, SidewalkTopY, position.z),
                material, null);
            BuildSafetyBollard(root.transform, "BlockedRoute_Right",
                new Vector3(position.x + 0.34f, SidewalkTopY, position.z),
                material, null);
            CreatePrimitive("BlockedRouteCrossbar", PrimitiveType.Cube,
                new Vector3(position.x, 0.86f, position.z - 0.08f),
                new Vector3(0.82f, 0.1f, 0.08f), material, root.transform, false,
                Quaternion.Euler(0f, 0f, -7f));
        }
        else if (choice == StairChoiceTarget.StairChoice.MiddleStairs)
        {
            InstantiateCity("Prop_RoadCone_01.prefab", "VehicleLaneWarningCone",
                root.transform,
                new Vector3(position.x, WalkY, position.z),
                new Vector3(0.42f, 0.64f, 0.42f), Vector3.zero);
        }
        else
        {
            BuildSafetyBollard(root.transform, "OpenRoute_Left",
                new Vector3(position.x - 0.36f, SidewalkTopY, position.z),
                material, null);
            BuildSafetyBollard(root.transform, "OpenRoute_Right",
                new Vector3(position.x + 0.36f, SidewalkTopY, position.z),
                material, null);
        }
        return target;
    }

    private static ParticleSystem BuildDust(
        Transform parent, string name, Vector3 position, Material material,
        Color color, float rate, int maxParticles)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
        main.startColor = color;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.22f;
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return particles;
    }

    private static ParticleSystem BuildConfetti(
        Transform parent, Palette palette, Vector3 position)
    {
        GameObject root = new GameObject("StreetSuccessConfetti");
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 1.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color32(255, 104, 82, 255), new Color32(56, 207, 145, 255));
        main.maxParticles = 320;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 180) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(4.2f, 0.1f, 0.8f);
        root.GetComponent<ParticleSystemRenderer>().sharedMaterial = palette.common.coral;
        return particles;
    }

    private static GameObject NewState(Transform parent, string name)
    {
        GameObject state = new GameObject(name);
        state.transform.SetParent(parent);
        return state;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType type,
        Vector3 position,
        Vector3 scale,
        Material material,
        Transform parent,
        bool collider,
        Quaternion? rotation = null)
    {
        return StoryChapterBuilderCommon.CreatePrimitive(
            name, type, position, scale, material, parent, collider, rotation);
    }

    private static void FitFacadeToBounds(
        GameObject facade, Vector3 feetPosition, Vector3 targetSize)
    {
        Renderer[] renderers = facade.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer != null && renderer.enabled &&
                               renderer is not ParticleSystemRenderer &&
                               renderer is not TrailRenderer &&
                               renderer is not LineRenderer)
            .ToArray();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        const float minimumDimension = 0.000001f;
        Vector3 scale = facade.transform.localScale;
        scale.x *= targetSize.x / Mathf.Max(minimumDimension, bounds.size.x);
        scale.y *= targetSize.y / Mathf.Max(minimumDimension, bounds.size.y);
        scale.z *= targetSize.z / Mathf.Max(minimumDimension, bounds.size.z);
        facade.transform.localScale = scale;

        bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        facade.transform.position += feetPosition -
                                     new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static GameObject InstantiateCity(
        string fileName, string name, Transform parent,
        Vector3 feetPosition, Vector3 targetSize, Vector3 euler)
    {
        GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
            CityRoot + "/" + fileName, name, parent, feetPosition, targetSize, euler,
            false, false);
        ConfigureSetRenderer(instance, true, true);
        return instance;
    }

    private static GameObject InstantiateTown(
        string assetPath, string name, Transform parent,
        Vector3 feetPosition, Vector3 targetSize, Vector3 euler)
    {
        Material townAtlas = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PolygonTown/Materials/PolygonTown_01_A.mat");
        if (townAtlas == null)
            throw new InvalidOperationException("Curated Polygon Town atlas material is missing.");
        GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
            assetPath, name, parent, feetPosition, targetSize, euler,
            false, false, townAtlas);
        ConfigureSetRenderer(instance, true, true);
        return instance;
    }

    private static void ConfigureSetRenderer(
        GameObject root, bool castShadows, bool receiveShadows)
    {
        if (root == null)
            return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode =
                castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    private static void RemoveRootPhysics(GameObject root)
    {
        foreach (Rigidbody body in root.GetComponents<Rigidbody>())
            Object.DestroyImmediate(body);
        foreach (Collider collider in root.GetComponents<Collider>())
            Object.DestroyImmediate(collider);
        CharacterController controller = root.GetComponent<CharacterController>();
        if (controller != null)
            Object.DestroyImmediate(controller);
    }

    private static void RemovePhysicsRecursive(GameObject root)
    {
        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        foreach (CharacterController controller in root.GetComponentsInChildren<CharacterController>(true))
            Object.DestroyImmediate(controller);
    }

    private static BoxCollider EnsureRootBoxCollider(GameObject root)
    {
        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null)
            collider = root.AddComponent<BoxCollider>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer is not ParticleSystemRenderer).ToArray();
        if (renderers.Length == 0)
        {
            collider.center = Vector3.up * 0.5f;
            collider.size = new Vector3(0.8f, 1f, 0.6f);
            return collider;
        }

        Bounds world = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            world.Encapsulate(renderer.bounds);
        Vector3[] corners =
        {
            new(world.min.x, world.min.y, world.min.z),
            new(world.min.x, world.min.y, world.max.z),
            new(world.min.x, world.max.y, world.min.z),
            new(world.min.x, world.max.y, world.max.z),
            new(world.max.x, world.min.y, world.min.z),
            new(world.max.x, world.min.y, world.max.z),
            new(world.max.x, world.max.y, world.min.z),
            new(world.max.x, world.max.y, world.max.z)
        };
        Bounds local = new Bounds(root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(root.transform.InverseTransformPoint(corner));
        collider.center = local.center;
        collider.size = local.size;
        return collider;
    }

    private static TextMeshPro WorldText(
        string value, Transform parent, Vector3 position, float fontSize, Color color)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out _, out TMP_FontAsset semibold, out TMP_FontAsset bold);
        GameObject root = new GameObject("WorldText_" + value);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * 0.1f;
        TextMeshPro text = root.AddComponent<TextMeshPro>();
        text.font = value.Length <= 2 ? bold : semibold;
        text.text = value;
        text.fontSize = fontSize * 10f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.rectTransform.sizeDelta = new Vector2(16f, 3.2f);
        return text;
    }

    private static Material GetOrCreateMaterial(
        string assetName, Color baseColor, float smoothness, Color emission)
    {
        string path = "Assets/Story/Generated/Materials/" + assetName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * 0.7f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreatePointLight(
        Transform parent, string name, Vector3 position,
        Color color, float intensity, float range)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        Light light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static SerializedProperty RequireProperty(SerializedObject data, string name)
    {
        SerializedProperty property = data.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException(
                data.targetObject.GetType().Name + "." + name + " was not found.");
        return property;
    }

    private static void EnsureEventSystem(Transform root)
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(root);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private sealed class Palette
    {
        internal StoryChapterBuilderCommon.Materials common;
        internal Material asphalt;
        internal Material sidewalk;
        internal Material curb;
        internal Material facadeDark;
        internal Material glass;
        internal Material safe;
        internal Material danger;
        internal Material amber;
        internal Material gas;
        internal Material rescue;
    }

    private sealed class CameraRig
    {
        internal Camera mainCamera;
        internal CinemachineBrain brain;
        internal StoryCameraController controller;
        internal CinemachineCamera[] virtualCameras;
    }

    private sealed class WorldRefs
    {
        internal GameObject facadeClear;
        internal GameObject facadeDanger;
        internal GameObject facadeSafe;
        internal ParticleSystem facadeDust;

        internal GameObject aftershockSpot;
        internal GameObject aftershockUnsafe;
        internal GameObject aftershockSafe;
        internal ParticleSystem aftershockDust;

        internal GameObject gasValve;
        internal GameObject gasOpen;
        internal GameObject gasClosed;
        internal ParticleSystem gasLeak;

        internal GameObject rubbleCheck;
        internal GameObject rubbleBlocked;
        internal GameObject rubbleCleared;

        internal GameObject masonry;
        internal BagDropZone masonryDropZone;
        internal Transform masonrySnap;
        internal Transform masonryDragPlane;
        internal GameObject masonryCleared;

        internal GameObject glassMark;
        internal GameObject glassUnmarked;
        internal GameObject glassMarked;

        internal GameObject glassCover;
        internal BagDropZone glassDropZone;
        internal Transform glassSnap;
        internal Transform glassDragPlane;
        internal GameObject glassCovered;

        internal GameObject injuredCheck;
        internal GameObject injuredAlert;
        internal GameObject injuredChecked;

        internal GameObject firstAid;
        internal BagDropZone firstAidDropZone;
        internal Transform firstAidSnap;
        internal Transform firstAidDragPlane;
        internal GameObject firstAidDelivered;

        internal GameObject gate;
        internal GameObject gateClosed;
        internal GameObject gateOpen;

        internal GameObject fireLane;
        internal GameObject fireLaneBlocked;
        internal GameObject fireLaneClear;

        internal GameObject fallenSign;
        internal BagDropZone signDropZone;
        internal Transform signSnap;
        internal Transform signDragPlane;
        internal GameObject signCleared;

        internal GameObject beacon;
        internal GameObject beaconOff;
        internal GameObject beaconOn;
        internal ParticleSystem confetti;
    }

    private sealed class CharacterRefs
    {
        internal GameObject deniz;
        internal GameObject can;
        internal GameObject neighbor;
        internal StoryPlayerMovement touchProxy;
        internal StairPathWalker[] denizSegments;
        internal StairPathWalker[] canSegments;
    }

    private sealed class UiRefs
    {
        internal GameObject[] stages;
        internal StairInfoBubbleUI bubble;
        internal GameObject successPanel;
    }

    private sealed class PuzzleRefs
    {
        internal StoryTouchManager touch;
        internal StoryInteractable facadeClear;
        internal StoryInteractable aftershock;
        internal StoryInteractable gasValve;
        internal StoryInteractable rubbleCheck;
        internal StoryInteractable masonry;
        internal StairChoiceManager firstChoice;
        internal GameObject firstChoiceRoot;
        internal StoryInteractable glassMark;
        internal StoryInteractable glassCover;
        internal StoryInteractable injuredCheck;
        internal StoryInteractable firstAid;
        internal StoryInteractable gate;
        internal StairChoiceManager secondChoice;
        internal GameObject secondChoiceRoot;
        internal StoryInteractable fireLane;
        internal StoryInteractable fallenSign;
        internal StoryInteractable assembly;
    }
}
