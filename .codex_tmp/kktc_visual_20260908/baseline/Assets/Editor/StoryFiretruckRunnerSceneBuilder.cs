using System;
using System.Collections.Generic;
using System.Linq;
using Deprem.Minigames;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class StoryFiretruckRunnerSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_04_FiretruckRunner.unity";

    private const string SyntyTownRoot = "Assets/Story/Environment/SyntyTown";
    private const string CoinHudIconPath = "Assets/Story/Collectibles/IMOCoin/IMOCoin_UI_Icon.png";
    private const string CityPrefabRoot =
        "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";

    [MenuItem("Tools/Deprem Story/Build Story 04 Firetruck Runner")]
    public static void BuildFromMenu() => Build(true);

    [MenuItem("Tools/Deprem Story/Build Story 04 Firetruck Runner (Silent)")]
    public static void BuildSilentFromMenu() => Build(false);

    public static void BuildFromCommandLine() => Build(false);

    private static void Build(bool showDialog)
    {
        try
        {
            StoryChapterBuilderCommon.EnsureFolders();
            StoryChapterBuilderCommon.Materials materials = StoryChapterBuilderCommon.CreateMaterials();
            VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_04_FIRETRUCK_RUNNER");

            BuildRoadAndCity(root.transform, materials);
            List<Transform> obstacles = BuildObstacles(root.transform, materials);
            List<Transform> coins = BuildCoinCollectibles(root.transform, materials);
            Transform truck = BuildFiretruck(root.transform, materials, out Light redBeacon, out Light blueBeacon);
            Camera runnerCamera = BuildCamera(root.transform, truck);
            BuildLighting(root.transform, materials, volume);
            AudioSource engineAudio = BuildAudio(truck, "RunnerEngineLoop", "sfx100v2_loop_machine_01.ogg", 0.055f, true);
            AudioSource hitAudio = BuildAudio(root.transform, "RunnerImpact", "sfx100v2_metal_hit_01.ogg", 0.22f, false);
            AudioSource coinAudio = BuildAudio(root.transform, "IMOCoinPickup", "sfx100v2_items_01.ogg", 0.34f, false);

            GameObject managerObject = new GameObject("_FiretruckRunnerManager");
            managerObject.transform.SetParent(root.transform);
            MinigameProgressManager progress = managerObject.AddComponent<MinigameProgressManager>();
            MinigameSessionManager resultReporter = managerObject.AddComponent<MinigameSessionManager>();
            ConfigureExternalReporter(
                resultReporter,
                progress,
                "firetruck-runner",
                "İTFAİYE ACİL ROTA");
            FiretruckRunnerManager manager = managerObject.AddComponent<FiretruckRunnerManager>();
            RunnerHud hud = BuildHud(root.transform, manager);
            ConfigureManager(
                manager,
                truck,
                runnerCamera,
                obstacles,
                coins,
                redBeacon,
                blueBeacon,
                engineAudio,
                hitAudio,
                coinAudio,
                hud,
                resultReporter);

            StoryChapterBuilderCommon.DisableShadows(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            MinigameSceneCatalog.PublishBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeGameObject = truck.gameObject;

            Debug.Log("Story_04_FiretruckRunner built successfully: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "30 saniyelik itfaiye runner sahnesi üretildi.",
                    "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", exception.Message, "Tamam");
            throw;
        }
    }

    private static void BuildRoadAndCity(
        Transform root,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform city = NewChild(root, "RunnerCity");
        const float startZ = -20f;
        const float roadLength = 540f;
        const float centerZ = startZ + roadLength * 0.5f;

        CreatePrimitive("ContinuousAsphalt", PrimitiveType.Cube,
            new Vector3(0f, -0.08f, centerZ), new Vector3(11.8f, 0.18f, roadLength),
            materials.asphalt, city, false);
        CreatePrimitive("Sidewalk_Left", PrimitiveType.Cube,
            new Vector3(-7.25f, 0.03f, centerZ), new Vector3(2.7f, 0.22f, roadLength),
            materials.concrete, city, false);
        CreatePrimitive("Sidewalk_Right", PrimitiveType.Cube,
            new Vector3(7.25f, 0.03f, centerZ), new Vector3(2.7f, 0.22f, roadLength),
            materials.concrete, city, false);
        CreatePrimitive("GrassVerge_Left", PrimitiveType.Cube,
            new Vector3(-11.4f, -0.02f, centerZ), new Vector3(5.6f, 0.16f, roadLength),
            materials.grass, city, false);
        CreatePrimitive("GrassVerge_Right", PrimitiveType.Cube,
            new Vector3(11.4f, -0.02f, centerZ), new Vector3(5.6f, 0.16f, roadLength),
            materials.grass, city, false);
        CreatePrimitive("RoadEdge_Left", PrimitiveType.Cube,
            new Vector3(-5.82f, 0.12f, centerZ), new Vector3(0.16f, 0.12f, roadLength),
            materials.white, city, false);
        CreatePrimitive("RoadEdge_Right", PrimitiveType.Cube,
            new Vector3(5.82f, 0.12f, centerZ), new Vector3(0.16f, 0.12f, roadLength),
            materials.white, city, false);

        Transform markings = NewChild(city, "RoadMarkings");
        for (float z = -8f; z < 520f; z += 10f)
        {
            for (int divider = -1; divider <= 1; divider += 2)
            {
                CreatePrimitive("LaneDash", PrimitiveType.Cube,
                    new Vector3(divider * 1.55f, 0.035f, z), new Vector3(0.12f, 0.035f, 4.8f),
                    materials.white, markings, false);
            }
        }

        foreach (float crosswalkZ in new[] { 122f, 268f, 414f })
        {
            for (int stripe = -5; stripe <= 5; stripe++)
            {
                CreatePrimitive("CrosswalkStripe", PrimitiveType.Cube,
                    new Vector3(stripe * 1.02f, 0.04f, crosswalkZ), new Vector3(0.66f, 0.035f, 2.9f),
                    materials.white, markings, false);
            }
        }

        BuildRoadside(city, materials);
        BuildFinishLandmark(city, materials);
    }

    private static void BuildRoadside(
        Transform city,
        StoryChapterBuilderCommon.Materials materials)
    {
        string[] buildings =
        {
            "Env_ResidentBuilding_01.prefab",
            "Env_ResidentBuilding_03.prefab",
            "Env_CommercialBuilding_02.prefab",
            "Env_CompanyBuilding_01.prefab",
            "Env_ResidentBuilding_05.prefab",
            "Env_CommercialBuilding_04.prefab"
        };

        Transform architecture = NewChild(city, "RoadsideArchitecture");
        for (int index = 0; index < 30; index++)
        {
            float z = 2f + index * 18f;
            float heightLeft = 15.5f + (index % 5) * 2.45f;
            float heightRight = 17f + ((index + 2) % 5) * 2.2f;
            InstantiateAsset(
                CityPrefabRoot + "/" + buildings[index % buildings.Length],
                "RunnerBuilding_Front_Left_" + index,
                architecture,
                new Vector3(-12.05f, 0.12f, z),
                new Vector3(7.6f, heightLeft, 15.9f),
                new Vector3(0f, 90f, 0f));
            InstantiateAsset(
                CityPrefabRoot + "/" + buildings[(index + 3) % buildings.Length],
                "RunnerBuilding_Front_Right_" + index,
                architecture,
                new Vector3(12.05f, 0.12f, z + 9f),
                new Vector3(7.6f, heightRight, 15.9f),
                new Vector3(0f, -90f, 0f));
        }

        string[] skylineBuildings =
        {
            "Env_CompanyBuilding_01.prefab",
            "Env_CommercialBuilding_04.prefab",
            "Env_ResidentBuilding_05.prefab",
            "Env_CommercialBuilding_02.prefab"
        };
        for (int index = 0; index < 19; index++)
        {
            float z = -6f + index * 29f;
            float heightLeft = 27f + (index % 4) * 4.2f;
            float heightRight = 29f + ((index + 1) % 4) * 3.8f;
            InstantiateAsset(
                CityPrefabRoot + "/" + skylineBuildings[index % skylineBuildings.Length],
                "RunnerBuilding_Skyline_Left_" + index,
                architecture,
                new Vector3(-20.7f, 0.05f, z + 6f),
                new Vector3(11.5f, heightLeft, 24f),
                new Vector3(0f, 90f, 0f));
            InstantiateAsset(
                CityPrefabRoot + "/" + skylineBuildings[(index + 2) % skylineBuildings.Length],
                "RunnerBuilding_Skyline_Right_" + index,
                architecture,
                new Vector3(20.7f, 0.05f, z + 20f),
                new Vector3(11.5f, heightRight, 24f),
                new Vector3(0f, -90f, 0f));
        }

        Transform streetProps = NewChild(city, "StreetFurniture");
        for (int index = 0; index < 18; index++)
        {
            float z = 8f + index * 29f;
            float side = index % 2 == 0 ? -1f : 1f;
            InstantiateAsset(
                CityPrefabRoot + "/Prop_Tree_0" + (index % 6 + 1) + ".prefab",
                "RunnerTree_" + index,
                streetProps,
                new Vector3(side * 9.2f, 0.13f, z),
                new Vector3(2.4f, 4.4f + index % 3, 2.4f),
                new Vector3(0f, index * 31f, 0f));

            string lampPath = SyntyTownRoot + "/SM_Prop_Streetlamp_01.fbx";
            InstantiateAsset(
                lampPath,
                "RunnerStreetlamp_" + index,
                streetProps,
                new Vector3(-side * 6.65f, 0.13f, z + 12f),
                new Vector3(0.55f, 3.8f, 0.55f),
                new Vector3(0f, side > 0f ? -90f : 90f, 0f));
        }

        foreach ((float z, float side) sign in new[]
                 {
                     (52f, -1f), (158f, 1f), (304f, -1f), (446f, 1f)
                 })
        {
            InstantiateAsset(
                CityPrefabRoot + "/Prop_StreetSign_Speed_30.prefab",
                "EmergencySpeedSign_" + sign.z,
                streetProps,
                new Vector3(sign.side * 6.7f, 0.13f, sign.z),
                new Vector3(0.62f, 2.45f, 0.62f),
                new Vector3(0f, sign.side > 0f ? -90f : 90f, 0f));
        }
    }

    private static List<Transform> BuildObstacles(
        Transform root,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform obstacleRoot = NewChild(root, "RunnerObstacles");
        List<Transform> obstacles = new();

        AddConeWave(obstacleRoot, obstacles, -1, 48f, 0);
        AddConeWave(obstacleRoot, obstacles, 1, 76f, 1);
        AddCardboard(obstacleRoot, obstacles, 0, 103f, 0);
        AddPickup(obstacleRoot, obstacles, -1, 132f, 0);
        AddBarrier(obstacleRoot, obstacles, -1, 163f, materials, 0);
        AddBarrier(obstacleRoot, obstacles, 0, 163f, materials, 1);
        AddDebris(obstacleRoot, obstacles, 1, 198f, materials, 0);
        AddDebris(obstacleRoot, obstacles, 0, 198f, materials, 1);
        AddConeWave(obstacleRoot, obstacles, -1, 232f, 2);
        AddCardboard(obstacleRoot, obstacles, 1, 262f, 1);
        AddBarrier(obstacleRoot, obstacles, -1, 293f, materials, 2);
        AddBarrier(obstacleRoot, obstacles, 1, 293f, materials, 3);
        AddPickup(obstacleRoot, obstacles, 0, 326f, 1);
        AddConeWave(obstacleRoot, obstacles, 1, 355f, 3);
        AddDebris(obstacleRoot, obstacles, -1, 386f, materials, 2);
        AddBarrier(obstacleRoot, obstacles, 0, 417f, materials, 4);
        AddBarrier(obstacleRoot, obstacles, 1, 417f, materials, 5);
        AddConeWave(obstacleRoot, obstacles, -1, 452f, 4);
        AddConeWave(obstacleRoot, obstacles, 0, 452f, 5);

        return obstacles;
    }

    private static List<Transform> BuildCoinCollectibles(
        Transform root,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform coinRoot = NewChild(root, "IMOCoinCollectibles");
        List<Transform> coins = new();
        Sprite coinFaceSprite = LoadCoinHudSprite();
        (float z, int lane)[] routes =
        {
            (24f, 0), (53f, 0), (82f, -1), (112f, 1),
            (143f, 1), (174f, -1), (209f, -1), (239f, 0),
            (272f, 0), (303f, -1), (335f, -1), (366f, 0),
            (397f, 1), (428f, -1)
        };

        int coinIndex = 0;
        foreach ((float z, int lane) route in routes)
        {
            for (int step = 0; step < 3; step++)
            {
                float z = route.z + step * 4.35f;
                Transform coin = NewChild(coinRoot, "IMOCoin_" + coinIndex.ToString("00"));
                coin.position = new Vector3(route.lane * 3.1f, 1.22f, z);
                BuildCoinVisual(coin, materials, coinFaceSprite);
                coins.Add(coin);
                coinIndex++;
            }
        }

        return coins;
    }

    private static void BuildCoinVisual(
        Transform coin,
        StoryChapterBuilderCommon.Materials materials,
        Sprite coinFaceSprite)
    {
        Vector3 center = coin.position;
        Quaternion faceRotation = Quaternion.Euler(90f, 0f, 0f);

        CreatePrimitive(
            "GoldCoinBody",
            PrimitiveType.Cylinder,
            center,
            new Vector3(2.08f, 0.14f, 2.08f),
            materials.amber,
            coin,
            false,
            faceRotation);

        BuildCoinSpriteFace(coin, coinFaceSprite, -1f);
        BuildCoinSpriteFace(coin, coinFaceSprite, 1f);
    }

    private static void BuildCoinSpriteFace(
        Transform coin,
        Sprite coinFaceSprite,
        float side)
    {
        GameObject face = new GameObject(side < 0f ? "FrontIMOCoinArtwork" : "BackIMOCoinArtwork");
        face.transform.SetParent(coin, false);
        face.transform.localPosition = new Vector3(0f, 0f, side * 0.151f);
        face.transform.localRotation = Quaternion.Euler(0f, side < 0f ? 0f : 180f, 0f);

        float spriteWidth = Mathf.Max(0.01f, coinFaceSprite.bounds.size.x);
        float uniformScale = 2.13f / spriteWidth;
        face.transform.localScale = Vector3.one * uniformScale;

        SpriteRenderer renderer = face.AddComponent<SpriteRenderer>();
        renderer.sprite = coinFaceSprite;
        renderer.color = Color.white;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Transform BuildFiretruck(
        Transform root,
        StoryChapterBuilderCommon.Materials materials,
        out Light redBeacon,
        out Light blueBeacon)
    {
        GameObject truckRoot = new GameObject("RunnerFiretruck");
        truckRoot.transform.SetParent(root);
        truckRoot.transform.position = new Vector3(0f, 0.12f, 8f);

        InstantiateAsset(
            SyntyTownRoot + "/SM_Veh_Firetruck_01.fbx",
            "RunnerFiretruckVisual",
            truckRoot.transform,
            truckRoot.transform.position,
            new Vector3(2.75f, 2.75f, 6.15f),
            Vector3.zero);

        GameObject shadow = CreatePrimitive(
            "TruckGroundShadow",
            PrimitiveType.Cylinder,
            truckRoot.transform.position + new Vector3(0f, -0.02f, 0.1f),
            new Vector3(1.45f, 0.018f, 2.85f),
            materials.dark,
            truckRoot.transform,
            false);
        shadow.transform.localPosition = new Vector3(0f, -0.12f, 0.1f);

        redBeacon = null;
        blueBeacon = null;
        return truckRoot.transform;
    }

    private static Camera BuildCamera(Transform root, Transform truck)
    {
        GameObject cameraObject = new GameObject("Runner Camera");
        cameraObject.transform.SetParent(root);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = truck.position + new Vector3(0f, 5.8f, -11.8f);
        cameraObject.transform.LookAt(truck.position + new Vector3(0f, 1.15f, 8.5f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 58f;
        camera.nearClipPlane = 0.15f;
        camera.farClipPlane = 700f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(105, 151, 181, 255);
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void BuildLighting(
        Transform root,
        StoryChapterBuilderCommon.Materials materials,
        VolumeProfile volume)
    {
        StoryChapterBuilderCommon.BuildLighting(
            root,
            volume,
            new Color(0.94f, 0.83f, 0.7f),
            1.14f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color32(127, 159, 175, 255);
        RenderSettings.fogStartDistance = 115f;
        RenderSettings.fogEndDistance = 360f;
        RenderSettings.ambientIntensity = 1.08f;

        CreatePrimitive("RunnerHorizon", PrimitiveType.Cube,
            new Vector3(0f, 18f, 535f), new Vector3(90f, 36f, 2f),
            materials.horizon, root, false);
    }

    private static void BuildFinishLandmark(
        Transform city,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform finish = NewChild(city, "FinishLandmark");
        CreatePrimitive("FinishArch_Left", PrimitiveType.Cube,
            new Vector3(-5.55f, 2.7f, 500f), new Vector3(0.5f, 5.4f, 0.5f),
            materials.coral, finish, false);
        CreatePrimitive("FinishArch_Right", PrimitiveType.Cube,
            new Vector3(5.55f, 2.7f, 500f), new Vector3(0.5f, 5.4f, 0.5f),
            materials.coral, finish, false);
        CreatePrimitive("FinishArch_Top", PrimitiveType.Cube,
            new Vector3(0f, 5.15f, 500f), new Vector3(11.6f, 0.5f, 0.5f),
            materials.coral, finish, false);
        CreatePrimitive("FinishBeacon", PrimitiveType.Sphere,
            new Vector3(0f, 5.8f, 500f), new Vector3(0.8f, 0.8f, 0.8f),
            materials.amber, finish, false);
    }

    private static AudioSource BuildAudio(
        Transform parent,
        string name,
        string clipName,
        float volume,
        bool loop)
    {
        GameObject audioObject = new GameObject(name);
        audioObject.transform.SetParent(parent);
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = StoryChapterBuilderCommon.LoadLicensedSfx(clipName);
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private static RunnerHud BuildHud(Transform root, FiretruckRunnerManager manager)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);

        GameObject canvasObject = new GameObject("FiretruckRunnerHUD");
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject safeArea = UiRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();

        GameObject objective = StoryChapterBuilderCommon.CreatePanel(
            "ObjectiveStrip",
            safeArea.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -98f),
            new Vector2(870f, 174f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        GameObject actBadge = StoryChapterBuilderCommon.CreatePanel(
            "ActBadge",
            objective.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(72f, 0f),
            new Vector2(112f, 112f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulYellowBadge);
        UiText("ActBadgeCaption", actBadge.transform, semibold, 14f, new Color32(18, 35, 60, 255),
            "PERDE", new Vector2(0f, 22f), new Vector2(84f, 24f));
        UiText("ActBadgeValue", actBadge.transform, bold, 30f, new Color32(18, 35, 60, 255),
            "4/4", new Vector2(0f, -14f), new Vector2(84f, 40f));
        TMP_Text title = UiText("ObjectiveTitle", objective.transform, bold, 28f,
            new Color32(255, 190, 46, 255), "İTFAİYE ARACINI SÜR",
            new Vector2(86f, 38f), new Vector2(620f, 42f));
        title.alignment = TextAlignmentOptions.Left;
        TMP_Text mission = UiText("MissionText", objective.transform, semibold, 25f, Color.white,
            "ACİL ÇAĞRIYA ULAŞ", new Vector2(86f, -30f), new Vector2(620f, 76f));
        mission.alignment = TextAlignmentOptions.Left;
        mission.enableAutoSizing = true;
        mission.fontSizeMin = 19f;
        mission.fontSizeMax = 25f;

        GameObject timerCard = StoryChapterBuilderCommon.CreatePanel(
            "TimerCard", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-220f, -254f), new Vector2(400f, 116f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        TMP_Text timer = UiText("Timer", timerCard.transform, bold, 45f, new Color32(18, 35, 60, 255),
            "30 sn", new Vector2(-94f, 10f), new Vector2(180f, 70f));
        TMP_Text distance = UiText("Distance", timerCard.transform, semibold, 21f, new Color32(59, 87, 105, 255),
            "ACİL ROTA  •  000 m", new Vector2(82f, 10f), new Vector2(196f, 58f));
        distance.enableAutoSizing = true;
        distance.fontSizeMin = 17f;
        distance.fontSizeMax = 21f;
        Image fillTrack = UiImage("TimerTrack", timerCard.transform,
            Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -42f), new Vector2(336f, 10f),
            new Color32(178, 190, 198, 255));
        Image timerFill = UiImage("TimerFill", fillTrack.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(336f, 10f),
            new Color32(241, 91, 62, 255));
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillOrigin = 0;
        timerFill.fillAmount = 1f;

        GameObject coinCard = StoryChapterBuilderCommon.CreatePanel(
            "IMOCoinCard", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(258f, -254f), new Vector2(300f, 116f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        GameObject coinIconObject = UiRect("IMOCoinIcon", coinCard.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(65f, 0f), new Vector2(98f, 98f));
        Image coinIcon = coinIconObject.AddComponent<Image>();
        coinIcon.sprite = LoadCoinHudSprite();
        coinIcon.preserveAspect = true;
        coinIcon.raycastTarget = false;
        TMP_Text coinCount = UiText("IMOCoinCount", coinCard.transform, bold, 34f, Color.white,
            "00 / 42", new Vector2(58f, 12f), new Vector2(160f, 52f));
        UiText("CoinCaption", coinCard.transform, semibold, 16f, new Color32(173, 218, 246, 255),
            "TOPLANAN", new Vector2(58f, -25f), new Vector2(160f, 28f));

        TMP_Text countdown = UiText("Countdown", safeArea.transform, bold, 178f, Color.white,
            "3", new Vector2(0f, 160f), new Vector2(720f, 260f));
        countdown.outlineWidth = 0.18f;
        countdown.outlineColor = new Color32(12, 27, 48, 240);

        GameObject instruction = StoryChapterBuilderCommon.CreatePanel(
            "ControlHint", safeArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 132f), new Vector2(820f, 126f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        UiText("LeftArrow", instruction.transform, bold, 50f, Color.white,
            "‹", new Vector2(-325f, 8f), new Vector2(90f, 80f));
        UiText("RightArrow", instruction.transform, bold, 50f, Color.white,
            "›", new Vector2(325f, 8f), new Vector2(90f, 80f));
        UiText("HintLabel", instruction.transform, semibold, 27f, new Color32(230, 239, 245, 255),
            "BASILI TUT + YÖNLENDİR  •  A / D", new Vector2(0f, 8f), new Vector2(680f, 72f));

        Image flashImage = UiImage("HitFlash", safeArea.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(255, 62, 42, 170));
        flashImage.raycastTarget = false;
        CanvasGroup hitFlash = flashImage.gameObject.AddComponent<CanvasGroup>();
        hitFlash.alpha = 0f;
        hitFlash.blocksRaycasts = false;

        GameObject completion = BuildCompletionPanel(safeArea.transform, manager, semibold, bold, out TMP_Text completionStats);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(root);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return new RunnerHud
        {
            timer = timer,
            distance = distance,
            coinCount = coinCount,
            mission = mission,
            countdown = countdown,
            timerFill = timerFill,
            hitFlash = hitFlash,
            completionPanel = completion,
            completionStats = completionStats
        };
    }

    private static GameObject BuildCompletionPanel(
        Transform parent,
        FiretruckRunnerManager manager,
        TMP_FontAsset semibold,
        TMP_FontAsset bold,
        out TMP_Text stats)
    {
        Image overlay = UiImage("RunnerCompletionPanel", parent,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(8, 19, 36, 236));
        Image card = UiImage("CompletionCard", overlay.transform,
            Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 20f), new Vector2(850f, 610f),
            new Color32(255, 247, 224, 255));
        UiText("SuccessKicker", card.transform, bold, 32f, new Color32(233, 75, 48, 255),
            "ACİL MÜDAHALE", new Vector2(0f, 210f), new Vector2(720f, 50f));
        TMP_Text success = UiText("SuccessTitle", card.transform, bold, 62f, new Color32(18, 35, 60, 255),
            "GÖREV TAMAMLANDI!", new Vector2(0f, 130f), new Vector2(760f, 92f));
        success.enableAutoSizing = true;
        success.fontSizeMin = 42f;
        stats = UiText("CompletionStats", card.transform, semibold, 34f, new Color32(54, 81, 99, 255),
            "GÜVENLİ VARIŞ\n000 m  •  0 temas", new Vector2(0f, 0f), new Vector2(720f, 130f));

        Button restart = StoryChapterBuilderCommon.CreateButton(
            "RestartRunnerButton",
            card.transform,
            "TEKRAR SÜR",
            semibold,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -185f),
            new Vector2(520f, 112f),
            new Color32(241, 91, 62, 255),
            Color.white);
        UnityEventTools.AddPersistentListener(restart.onClick, manager.Restart);
        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    private static void ConfigureManager(
        FiretruckRunnerManager manager,
        Transform truck,
        Camera runnerCamera,
        List<Transform> obstacles,
        List<Transform> coins,
        Light redBeacon,
        Light blueBeacon,
        AudioSource engineAudio,
        AudioSource hitAudio,
        AudioSource coinAudio,
        RunnerHud hud,
        MinigameSessionManager resultReporter)
    {
        SerializedObject data = new SerializedObject(manager);
        data.FindProperty("truck").objectReferenceValue = truck;
        data.FindProperty("runnerCamera").objectReferenceValue = runnerCamera;
        SerializedProperty obstacleData = data.FindProperty("obstacles");
        obstacleData.arraySize = obstacles.Count;
        for (int i = 0; i < obstacles.Count; i++)
            obstacleData.GetArrayElementAtIndex(i).objectReferenceValue = obstacles[i];
        SerializedProperty coinData = data.FindProperty("coins");
        coinData.arraySize = coins.Count;
        for (int i = 0; i < coins.Count; i++)
            coinData.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
        data.FindProperty("redBeacon").objectReferenceValue = redBeacon;
        data.FindProperty("blueBeacon").objectReferenceValue = blueBeacon;
        data.FindProperty("engineAudio").objectReferenceValue = engineAudio;
        data.FindProperty("hitAudio").objectReferenceValue = hitAudio;
        data.FindProperty("coinAudio").objectReferenceValue = coinAudio;
        data.FindProperty("timerText").objectReferenceValue = hud.timer;
        data.FindProperty("distanceText").objectReferenceValue = hud.distance;
        data.FindProperty("coinCountText").objectReferenceValue = hud.coinCount;
        data.FindProperty("missionText").objectReferenceValue = hud.mission;
        data.FindProperty("countdownText").objectReferenceValue = hud.countdown;
        data.FindProperty("timerFill").objectReferenceValue = hud.timerFill;
        data.FindProperty("hitFlash").objectReferenceValue = hud.hitFlash;
        data.FindProperty("completionPanel").objectReferenceValue = hud.completionPanel;
        data.FindProperty("completionStats").objectReferenceValue = hud.completionStats;
        data.FindProperty("resultReporter").objectReferenceValue = resultReporter;
        data.FindProperty("runDuration").floatValue = 30f;
        data.FindProperty("forwardSpeed").floatValue = 14.5f;
        data.FindProperty("laneWidth").floatValue = 3.1f;
        data.FindProperty("laneChangeSpeed").floatValue = 6.4f;
        data.FindProperty("steeringResponse").floatValue = 5.5f;
        data.FindProperty("countdownDuration").floatValue = 3.25f;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureExternalReporter(
        MinigameSessionManager reporter,
        MinigameProgressManager progress,
        string id,
        string title)
    {
        SerializedObject data = new SerializedObject(reporter);
        data.FindProperty("minigameId").stringValue = id;
        data.FindProperty("displayName").stringValue = title;
        data.FindProperty("externalResultOnly").boolValue = true;
        data.FindProperty("progressManager").objectReferenceValue = progress;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddConeWave(Transform parent, List<Transform> obstacles, int lane, float z, int index)
    {
        Transform wave = NewChild(parent, "RunnerObstacle_ConeWave_" + index);
        wave.position = LanePosition(lane, z);
        for (int cone = -1; cone <= 1; cone++)
        {
            InstantiateAsset(
                CityPrefabRoot + "/Prop_RoadCone_0" + (Mathf.Abs(cone) + 1) + ".prefab",
                "Cone_" + cone,
                wave,
                wave.position + new Vector3(cone * 0.62f, 0f, Mathf.Abs(cone) * 0.42f),
                new Vector3(0.5f, 0.78f, 0.5f),
                new Vector3(0f, cone * 17f, 0f));
        }
        obstacles.Add(wave);
    }

    private static void AddCardboard(Transform parent, List<Transform> obstacles, int lane, float z, int index)
    {
        GameObject box = InstantiateAsset(
            SyntyTownRoot + "/SM_Prop_CardboardBox_01.fbx",
            "RunnerObstacle_Cardboard_" + index,
            parent,
            LanePosition(lane, z),
            new Vector3(1.65f, 1.28f, 1.55f),
            new Vector3(0f, index * 23f - 12f, 0f));
        obstacles.Add(box.transform);
    }

    private static void AddPickup(Transform parent, List<Transform> obstacles, int lane, float z, int index)
    {
        GameObject pickup = InstantiateAsset(
            SyntyTownRoot + "/SM_Veh_Pickup_01.fbx",
            "RunnerObstacle_StalledPickup_" + index,
            parent,
            LanePosition(lane, z),
            new Vector3(2.45f, 2.05f, 5.25f),
            Vector3.zero);
        obstacles.Add(pickup.transform);
    }

    private static void AddBarrier(
        Transform parent,
        List<Transform> obstacles,
        int lane,
        float z,
        StoryChapterBuilderCommon.Materials materials,
        int index)
    {
        Transform barrier = NewChild(parent, "RunnerObstacle_Barrier_" + index);
        barrier.position = LanePosition(lane, z);
        CreatePrimitive("BarrierBody", PrimitiveType.Cube,
            barrier.position + new Vector3(0f, 0.75f, 0f), new Vector3(2.45f, 0.48f, 0.36f),
            materials.white, barrier, false);
        for (int stripe = -2; stripe <= 2; stripe++)
        {
            CreatePrimitive("WarningStripe", PrimitiveType.Cube,
                barrier.position + new Vector3(stripe * 0.5f, 0.76f, -0.2f), new Vector3(0.24f, 0.54f, 0.06f),
                materials.coral, barrier, false, Quaternion.Euler(0f, 0f, -24f));
        }
        CreatePrimitive("BarrierFoot_Left", PrimitiveType.Cube,
            barrier.position + new Vector3(-0.85f, 0.23f, 0f), new Vector3(0.22f, 0.85f, 0.22f),
            materials.metal, barrier, false);
        CreatePrimitive("BarrierFoot_Right", PrimitiveType.Cube,
            barrier.position + new Vector3(0.85f, 0.23f, 0f), new Vector3(0.22f, 0.85f, 0.22f),
            materials.metal, barrier, false);
        obstacles.Add(barrier);
    }

    private static void AddDebris(
        Transform parent,
        List<Transform> obstacles,
        int lane,
        float z,
        StoryChapterBuilderCommon.Materials materials,
        int index)
    {
        Transform debris = NewChild(parent, "RunnerObstacle_Debris_" + index);
        debris.position = LanePosition(lane, z);
        for (int piece = 0; piece < 6; piece++)
        {
            float x = -0.95f + piece * 0.38f;
            float depth = piece % 2 == 0 ? -0.45f : 0.35f;
            CreatePrimitive("DebrisPiece_" + piece, piece % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                debris.position + new Vector3(x, 0.18f + piece % 2 * 0.12f, depth),
                new Vector3(0.45f + piece % 2 * 0.18f, 0.3f, 0.5f),
                piece % 2 == 0 ? materials.concrete : materials.wood,
                debris, false, Quaternion.Euler(piece * 13f, piece * 27f, piece * 9f));
        }
        obstacles.Add(debris);
    }

    private static Vector3 LanePosition(int lane, float z) => new(lane * 3.1f, 0.12f, z);

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

    private static GameObject InstantiateAsset(
        string path,
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 size,
        Vector3 euler)
    {
        Material materialOverride = null;
        if (path.StartsWith(SyntyTownRoot + "/", StringComparison.Ordinal))
        {
            materialOverride = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/PolygonTown/Materials/PolygonTown_01_A.mat");
            if (materialOverride == null)
                throw new InvalidOperationException("Polygon Town atlas material could not be loaded.");
        }

        GameObject instance = StoryChapterBuilderCommon.InstantiateAsset(
            path, name, parent, feetPosition, size, euler, false, false, materialOverride);
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        return instance;
    }

    private static Sprite LoadCoinHudSprite()
    {
        AssetDatabase.ImportAsset(CoinHudIconPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(CoinHudIconPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("İMO coin HUD texture importer could not be loaded.");

        bool requiresReimport = importer.textureType != TextureImporterType.Sprite ||
                                importer.spriteImportMode != SpriteImportMode.Single ||
                                !importer.alphaIsTransparency ||
                                importer.mipmapEnabled ||
                                importer.wrapMode != TextureWrapMode.Clamp;
        if (requiresReimport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinHudIconPath);
        if (sprite == null)
            throw new InvalidOperationException("İMO coin HUD sprite could not be loaded: " + CoinHudIconPath);
        return sprite;
    }

    private static GameObject UiRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size)
    {
        return StoryChapterBuilderCommon.CreateUIRect(name, parent, anchorMin, anchorMax, position, size);
    }

    private static Image UiImage(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject gameObject = UiRect(name, parent, anchorMin, anchorMax, position, size);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text UiText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        string value,
        Vector2 position,
        Vector2 size)
    {
        TMP_Text text = StoryChapterBuilderCommon.CreateText(
            name, parent, font, fontSize, color, TextAlignmentOptions.Center, position, size);
        text.text = value;
        text.fontStyle = FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private sealed class RunnerHud
    {
        internal TMP_Text timer;
        internal TMP_Text distance;
        internal TMP_Text coinCount;
        internal TMP_Text mission;
        internal TMP_Text countdown;
        internal Image timerFill;
        internal CanvasGroup hitFlash;
        internal GameObject completionPanel;
        internal TMP_Text completionStats;
    }
}
