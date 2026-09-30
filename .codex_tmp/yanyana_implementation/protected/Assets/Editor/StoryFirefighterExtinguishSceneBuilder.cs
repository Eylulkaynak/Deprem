using System;
using System.Collections.Generic;
using System.IO;
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

public static class StoryFirefighterExtinguishSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Minigame_FirefighterExtinguish.unity";

    private const string FirefighterPrefabPath =
        "Assets/Story/Characters/MeshyResponders/Prefabs/Firefighter.prefab";
    private const string FiretruckAssetPath =
        "Assets/Story/Environment/SyntyTown/SM_Veh_Firetruck_01.fbx";
    private const string FiretruckAtlasMaterialPath =
        "Assets/PolygonTown/Materials/PolygonTown_01_A.mat";
    private const string CityTownPropRoot =
        "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";
    private const string GeneratedMaterialRoot = "Assets/Story/Generated/Minigames/Materials";
    private const string ThumbnailPath =
        "Assets/Story/UI/Minigames/Thumbnails/firefighter-extinguish.png";

    [MenuItem("Tools/Deprem Story/Minigames/Firefighter Extinguish/Build")]
    public static void BuildFromMenu() => Build(true);

    [MenuItem("Tools/Deprem Story/Minigames/Firefighter Extinguish/Build Silent")]
    public static void BuildSilentFromMenu() => Build(false);

    public static void BuildFromCommandLine() => Build(false);

    private static void Build(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("İtfaiyeci minigame sahnesi Play Mode dışında üretilmelidir.");

        StoryChapterBuilderCommon.EnsureFolders();
        EnsureFolder("Assets/Story/Generated/Minigames");
        EnsureFolder(GeneratedMaterialRoot);
        EnsureFolder("Assets/Story/UI/Minigames");
        EnsureFolder("Assets/Story/UI/Minigames/Thumbnails");

        StoryChapterBuilderCommon.Materials materials = StoryChapterBuilderCommon.CreateMaterials();
        VolumeProfile volume = StoryChapterBuilderCommon.CreateVolumeProfile();
        Material flameMaterial = CreateEffectMaterial(
            "Firefighter_Flame", Color.white, true, true);
        Material smokeMaterial = CreateEffectMaterial(
            "Firefighter_Smoke", Color.white, false, true);
        Material waterMaterial = CreateEffectMaterial(
            "Firefighter_Water", Color.white, true, false);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform environment = NewRoot("Environment");
        Transform characters = NewRoot("Characters");
        Transform gameplay = NewRoot("Gameplay");
        Transform cameras = NewRoot("Cameras");
        Transform lightingVfx = NewRoot("LightingVFX");
        Transform audioRoot = NewRoot("Audio");
        Transform uiRoot = NewRoot("UI");
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        BuildEnvironment(environment, materials);
        GameObject firefighter = BuildFirefighter(characters);
        NozzleRigRefs nozzle = BuildNozzle(firefighter, materials);
        Camera camera = BuildCamera(cameras);
        StoryChapterBuilderCommon.BuildLighting(
            lightingVfx,
            volume,
            new Color(1f, 0.77f, 0.58f),
            0.62f);
        BuildEmergencyLighting(lightingVfx);

        List<FireTargetBinding> fires = BuildFires(
            gameplay,
            environment,
            materials,
            flameMaterial,
            smokeMaterial);
        LineRenderer waterStream = BuildWaterStream(gameplay, waterMaterial);
        ParticleSystem waterImpact = BuildWaterImpact(gameplay, waterMaterial);
        AudioSource sprayAudio = BuildAudio(
            audioRoot,
            "WaterSprayLoop",
            "sfx100v2_loop_water_03.ogg",
            0.18f,
            true);
        AudioSource extinguishAudio = BuildAudio(
            audioRoot,
            "FireExtinguishedSfx",
            "sfx100v2_items_01.ogg",
            0.42f,
            false);

        GameObject managerObject = new GameObject("_FirefighterExtinguishManager");
        managerObject.transform.SetParent(gameplay, false);
        MinigameProgressManager progress = managerObject.AddComponent<MinigameProgressManager>();
        FirefighterExtinguishManager manager = managerObject.AddComponent<FirefighterExtinguishManager>();
        HudRefs hud = BuildHud(uiRoot, manager);
        ConfigureManager(
            manager,
            progress,
            camera,
            firefighter,
            nozzle,
            waterStream,
            waterImpact,
            fires,
            hud,
            sprayAudio,
            extinguishAudio);
        manager.PoseForEditorPreview(fires[1].hitCollider.bounds.center + Vector3.up * 0.25f);

        StoryChapterBuilderCommon.DisableShadows(environment);
        StoryChapterBuilderCommon.DisableShadows(characters);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureThumbnail(camera);
        MinigameSceneCatalog.PublishBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Selection.activeGameObject = managerObject;

        if (!manager.ValidateConfiguration(out string validationError))
            throw new InvalidOperationException(validationError);
        Debug.Log("Firefighter extinguish minigame built successfully: " + ScenePath);
        if (showDialog)
            EditorUtility.DisplayDialog(
                "Deprem Minigames",
                "İtfaiyeci yangın söndürme minigame sahnesi üretildi.",
                "Tamam");
    }

    private static void BuildEnvironment(
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color32(26, 37, 48, 255);
        RenderSettings.fogStartDistance = 13f;
        RenderSettings.fogEndDistance = 34f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(62, 73, 82, 255);

        CreatePrimitive("ResponseGround", PrimitiveType.Cube,
            new Vector3(0f, -0.12f, 2.5f), new Vector3(12.5f, 0.24f, 16f),
            materials.asphalt, parent, true);
        CreatePrimitive("BackWarehouse", PrimitiveType.Cube,
            new Vector3(0f, 3.6f, 8.7f), new Vector3(12.5f, 7.2f, 0.42f),
            materials.concrete, parent, true);
        CreatePrimitive("LeftBoundary", PrimitiveType.Cube,
            new Vector3(-6.15f, 2.2f, 2.7f), new Vector3(0.32f, 4.4f, 12f),
            materials.dark, parent, true);
        CreatePrimitive("RightBoundary", PrimitiveType.Cube,
            new Vector3(6.15f, 2.2f, 2.7f), new Vector3(0.32f, 4.4f, 12f),
            materials.dark, parent, true);
        CreatePrimitive("WarehouseDoor", PrimitiveType.Cube,
            new Vector3(0.3f, 2.25f, 8.42f), new Vector3(5.4f, 4.5f, 0.12f),
            materials.metal, parent, false);
        for (int stripe = -3; stripe <= 3; stripe++)
            CreatePrimitive("DoorRib_" + stripe, PrimitiveType.Cube,
                new Vector3(0.3f, 2.25f + stripe * 0.58f, 8.33f),
                new Vector3(5.15f, 0.07f, 0.06f), materials.dark, parent, false);
        CreatePrimitive("EmergencyBanner", PrimitiveType.Cube,
            new Vector3(0.3f, 5.35f, 8.28f), new Vector3(4.8f, 0.52f, 0.08f),
            materials.coral, parent, false);
        CreatePrimitive("LeftCurb", PrimitiveType.Cube,
            new Vector3(-5.25f, 0.08f, 2.3f), new Vector3(0.42f, 0.16f, 12.8f),
            materials.white, parent, false);
        CreatePrimitive("RightCurb", PrimitiveType.Cube,
            new Vector3(5.25f, 0.08f, 2.3f), new Vector3(0.42f, 0.16f, 12.8f),
            materials.white, parent, false);
        CreatePrimitive("LeftLoadingWall", PrimitiveType.Cube,
            new Vector3(-5.25f, 2.0f, 6.2f), new Vector3(0.5f, 4f, 5.2f),
            materials.concrete, parent, true);
        CreatePrimitive("RightLoadingWall", PrimitiveType.Cube,
            new Vector3(5.25f, 2.0f, 6.2f), new Vector3(0.5f, 4f, 5.2f),
            materials.concrete, parent, true);
        CreatePrimitive("OverheadPipeA", PrimitiveType.Cylinder,
            new Vector3(-3.65f, 4.65f, 8.08f), new Vector3(0.15f, 1.45f, 0.15f),
            materials.metal, parent, false, Quaternion.Euler(0f, 0f, 90f));
        CreatePrimitive("OverheadPipeB", PrimitiveType.Cylinder,
            new Vector3(3.55f, 4.65f, 8.08f), new Vector3(0.15f, 1.55f, 0.15f),
            materials.metal, parent, false, Quaternion.Euler(0f, 0f, 90f));
        for (int index = 0; index < 5; index++)
        {
            float x = -4.2f + index * 2.1f;
            CreatePrimitive("HazardStripe_" + index, PrimitiveType.Cube,
                new Vector3(x, 0.04f, 7.92f), new Vector3(1.05f, 0.025f, 0.18f),
                index % 2 == 0 ? materials.coral : materials.dark, parent, false,
                Quaternion.Euler(0f, 28f, 0f));
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(FiretruckAssetPath) != null)
        {
            Material firetruckAtlas = AssetDatabase.LoadAssetAtPath<Material>(FiretruckAtlasMaterialPath);
            if (firetruckAtlas == null)
                throw new InvalidOperationException("Polygon Town itfaiye aracı atlas materyali bulunamadı.");

            StoryChapterBuilderCommon.InstantiateAsset(
                FiretruckAssetPath,
                "ResponseFiretruck_PolygonTownMesh",
                parent,
                new Vector3(3.25f, 0f, 7.15f),
                new Vector3(4.6f, 4.0f, 8.8f),
                new Vector3(0f, 204f, 0f),
                false,
                false,
                firetruckAtlas);
        }

        for (int index = 0; index < 3; index++)
        {
            float x = -3.95f + index * 3.95f;
            CreatePrimitive("SafetyCone_" + index, PrimitiveType.Cylinder,
                new Vector3(x, 0.25f, 0.25f), new Vector3(0.22f, 0.25f, 0.22f),
                materials.coral, parent, false);
            CreatePrimitive("ConeStripe_" + index, PrimitiveType.Cylinder,
                new Vector3(x, 0.29f, 0.25f), new Vector3(0.24f, 0.045f, 0.24f),
                materials.white, parent, false);
        }
    }

    private static GameObject BuildFirefighter(Transform parent)
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Story/Animations/Generated/StoryAdultAnimator.controller");
        GameObject firefighter = StoryChapterBuilderCommon.InstantiateCharacter(
            FirefighterPrefabPath,
            "Firefighter_Player_MeshyResponder",
            parent,
            new Vector3(-1.85f, 0f, -0.55f),
            2.62f,
            controller);
        Vector3 direction = new Vector3(0.2f, 0f, 3.4f) - firefighter.transform.position;
        direction.y = 0f;
        firefighter.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return firefighter;
    }

    private static NozzleRigRefs BuildNozzle(
        GameObject firefighter,
        StoryChapterBuilderCommon.Materials materials)
    {
        Transform chest = StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.Chest) ??
                          StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.Spine);
        GameObject rigObject = new GameObject("NozzleAimRig");
        rigObject.transform.SetParent(firefighter.transform, true);
        rigObject.transform.position = chest != null
            ? chest.position + firefighter.transform.forward * 0.42f - Vector3.up * 0.16f
            : firefighter.transform.position + new Vector3(0f, 1.28f, 0.42f);
        rigObject.transform.rotation = firefighter.transform.rotation;

        GameObject nozzleVisual = CreatePrimitive(
            "NozzleBody",
            PrimitiveType.Cylinder,
            rigObject.transform.position,
            new Vector3(0.075f, 0.29f, 0.075f),
            materials.metal,
            rigObject.transform,
            false,
            rigObject.transform.rotation * Quaternion.Euler(90f, 0f, 0f));
        nozzleVisual.transform.localPosition = Vector3.zero;
        nozzleVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject nozzleTipVisual = CreatePrimitive(
            "NozzleTipVisual", PrimitiveType.Cylinder, rigObject.transform.position,
            new Vector3(0.105f, 0.11f, 0.105f), materials.dark,
            rigObject.transform, false,
            rigObject.transform.rotation * Quaternion.Euler(90f, 0f, 0f));
        nozzleTipVisual.transform.localPosition = new Vector3(0f, 0f, 0.29f);
        nozzleTipVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject handle = CreatePrimitive(
            "NozzleHandle", PrimitiveType.Cube, rigObject.transform.position,
            new Vector3(0.16f, 0.22f, 0.07f), materials.dark,
            rigObject.transform, false);
        handle.transform.localPosition = new Vector3(0.11f, -0.13f, -0.08f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        Transform tip = NewPoint("WaterNozzleTip", rigObject.transform, new Vector3(0f, 0f, 0.41f));
        Transform rightGrip = NewPoint("RightHandGrip", rigObject.transform, new Vector3(0.105f, -0.045f, -0.13f));
        Transform leftGrip = NewPoint("LeftHandGrip", rigObject.transform, new Vector3(-0.085f, 0.015f, 0.15f));

        GameObject hoseObject = new GameObject("AuthoredHose");
        hoseObject.transform.SetParent(firefighter.transform, false);
        LineRenderer hose = hoseObject.AddComponent<LineRenderer>();
        hose.useWorldSpace = true;
        hose.positionCount = 6;
        hose.startWidth = 0.095f;
        hose.endWidth = 0.085f;
        hose.numCapVertices = 7;
        hose.numCornerVertices = 7;
        hose.sharedMaterial = materials.dark;
        Vector3 start = rigObject.transform.TransformPoint(new Vector3(0f, 0f, -0.34f));
        Vector3 basePoint = firefighter.transform.position + Vector3.up * 0.08f;
        hose.SetPositions(new[]
        {
            start,
            firefighter.transform.position + new Vector3(-0.18f, 0.72f, -0.08f),
            firefighter.transform.position + new Vector3(-0.48f, 0.24f, -0.18f),
            basePoint + new Vector3(-0.95f, 0f, -0.6f),
            basePoint + new Vector3(-2.2f, 0f, -1.05f),
            basePoint + new Vector3(-3.4f, 0f, -1.25f)
        });
        return new NozzleRigRefs
        {
            rig = rigObject.transform,
            tip = tip,
            rightGrip = rightGrip,
            leftGrip = leftGrip,
            supplyHose = hose
        };
    }

    private static Camera BuildCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Firefighter Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.45f, 3.15f, -8.9f);
        cameraObject.transform.LookAt(new Vector3(0.2f, 1.25f, 3.55f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 49f;
        camera.nearClipPlane = 0.15f;
        camera.farClipPlane = 120f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(16, 27, 39, 255);
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void BuildEmergencyLighting(Transform parent)
    {
        GameObject emergency = new GameObject("EmergencyWarmFill");
        emergency.transform.SetParent(parent, false);
        emergency.transform.position = new Vector3(-3.1f, 3.0f, 1.4f);
        Light light = emergency.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color32(255, 126, 66, 255);
        light.intensity = 2.1f;
        light.range = 10f;
        light.shadows = LightShadows.None;

        GameObject blue = new GameObject("EmergencyBlueBounce");
        blue.transform.SetParent(parent, false);
        blue.transform.position = new Vector3(3.6f, 3.15f, 5.4f);
        Light blueLight = blue.AddComponent<Light>();
        blueLight.type = LightType.Point;
        blueLight.color = new Color32(52, 142, 255, 255);
        blueLight.intensity = 1.75f;
        blueLight.range = 8f;
        blueLight.shadows = LightShadows.None;
    }

    private static List<FireTargetBinding> BuildFires(
        Transform gameplay,
        Transform environment,
        StoryChapterBuilderCommon.Materials materials,
        Material flameMaterial,
        Material smokeMaterial)
    {
        Vector3[] positions =
        {
            new Vector3(-0.45f, 0f, 1.45f),
            new Vector3(1.45f, 0f, 2.75f),
            new Vector3(-1.05f, 0f, 4.15f),
            new Vector3(1.75f, 0f, 5.2f),
            new Vector3(-0.15f, 0f, 6.65f)
        };
        List<FireTargetBinding> result = new List<FireTargetBinding>(positions.Length);
        for (int index = 0; index < positions.Length; index++)
        {
            Vector3 position = positions[index];
            GameObject target = new GameObject("FireTarget_" + (index + 1));
            target.transform.SetParent(gameplay, false);
            target.transform.position = position;
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, index == 2 ? 1.05f : 0.72f, 0f);
            collider.size = index == 2
                ? new Vector3(1.65f, 2.35f, 1.3f)
                : new Vector3(1.55f, 1.75f, 1.55f);

            BuildBurningTargetProp(index, position, environment, materials);
            CreatePrimitive("ScorchMark_" + (index + 1), PrimitiveType.Cylinder,
                position + Vector3.up * 0.018f, new Vector3(0.82f, 0.018f, 0.82f),
                materials.dark, environment, false);

            GameObject visual = new GameObject("FireVisual_" + (index + 1));
            visual.transform.SetParent(target.transform, false);
            visual.transform.localPosition = new Vector3(0f, index == 2 ? 0.72f : 0.48f, 0f);
            BuildFlameParticles(visual.transform, flameMaterial, index);
            BuildSmokeParticles(visual.transform, smokeMaterial, index);
            Light glow = visual.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color32(255, 105, 32, 255);
            glow.intensity = 3.4f;
            glow.range = 4.2f;
            glow.shadows = LightShadows.None;
            result.Add(new FireTargetBinding
            {
                hitCollider = collider,
                visualRoot = visual.transform,
                glow = glow
            });
        }
        return result;
    }

    private static void BuildBurningTargetProp(
        int index,
        Vector3 position,
        Transform parent,
        StoryChapterBuilderCommon.Materials materials)
    {
        string path;
        Vector3 size;
        Vector3 rotation;
        switch (index)
        {
            case 0:
                path = CityTownPropRoot + "/Prop_OilBerrel_01.prefab";
                size = new Vector3(1.05f, 1.3f, 1.05f);
                rotation = new Vector3(0f, -18f, 0f);
                break;
            case 1:
                path = CityTownPropRoot + "/Prop_CTPTrashCan_02.prefab";
                size = new Vector3(1.2f, 1.45f, 1.2f);
                rotation = new Vector3(0f, 22f, 0f);
                break;
            case 2:
                path = CityTownPropRoot + "/Prop_ElectracityCabinet_03.prefab";
                size = new Vector3(1.65f, 2.15f, 1.15f);
                rotation = new Vector3(0f, -12f, 0f);
                break;
            case 3:
                path = CityTownPropRoot + "/Prop_OilBerrel_01.prefab";
                size = new Vector3(1.1f, 1.38f, 1.1f);
                rotation = new Vector3(0f, 31f, 0f);
                break;
            default:
                path = CityTownPropRoot + "/Prop_TrashBag_03.prefab";
                size = new Vector3(1.65f, 1.05f, 1.55f);
                rotation = new Vector3(0f, -27f, 0f);
                break;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                path, "BurningProp_" + (index + 1), parent, position,
                size, rotation, false, false);
            return;
        }

        CreatePrimitive("BurningFallback_" + (index + 1), PrimitiveType.Cube,
            position + Vector3.up * 0.45f, new Vector3(1f, 0.9f, 0.9f),
            materials.dark, parent, false, Quaternion.Euler(rotation));
    }

    private static void BuildFlameParticles(Transform parent, Material material, int seed)
    {
        GameObject flameObject = new GameObject("Flames");
        flameObject.transform.SetParent(parent, false);
        // Particle cones emit on local +Z; turn them upward so the fire rises instead of
        // streaking sideways across the props.
        flameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem particles = flameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.05f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.42f, 0.82f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color32(255, 219, 70, 255),
            new Color32(255, 74, 20, 245));
        main.maxParticles = 78;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        particles.randomSeed = (uint)(1400 + seed * 31);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 38f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 13f;
        shape.radius = 0.34f;
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(255, 240, 110, 255), 0f),
                new GradientColorKey(new Color32(255, 76, 22, 255), 0.62f),
                new GradientColorKey(new Color32(80, 22, 16, 255), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.8f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.08f)));
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 4;
    }

    private static void BuildSmokeParticles(Transform parent, Material material, int seed)
    {
        GameObject smokeObject = new GameObject("Smoke");
        smokeObject.transform.SetParent(parent, false);
        smokeObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        smokeObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem particles = smokeObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.32f, 0.72f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color32(58, 64, 68, 150),
            new Color32(95, 98, 98, 95));
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        particles.randomSeed = (uint)(2400 + seed * 41);
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 7f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 17f;
        shape.radius = 0.32f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 3;
    }

    private static LineRenderer BuildWaterStream(Transform parent, Material material)
    {
        GameObject streamObject = new GameObject("WaterStream");
        streamObject.transform.SetParent(parent, false);
        LineRenderer line = streamObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 14;
        line.startWidth = 0.13f;
        line.endWidth = 0.07f;
        line.numCapVertices = 5;
        line.numCornerVertices = 3;
        line.sharedMaterial = material;
        line.startColor = new Color32(226, 249, 255, 255);
        line.endColor = new Color32(77, 188, 255, 205);
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private static ParticleSystem BuildWaterImpact(Transform parent, Material material)
    {
        GameObject impactObject = new GameObject("WaterImpact");
        impactObject.transform.SetParent(parent, false);
        ParticleSystem particles = impactObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color32(224, 249, 255, 255),
            new Color32(72, 184, 255, 220));
        main.maxParticles = 70;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 58f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.13f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 8;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static HudRefs BuildHud(Transform parent, FirefighterExtinguishManager manager)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);
        GameObject canvasObject = new GameObject(
            "FirefighterExtinguishHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = StoryChapterBuilderCommon.CreateUIRect(
            "SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();
        GameObject header = StoryChapterBuilderCommon.CreatePanel(
            "MissionHeader", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -78f), new Vector2(920f, 126f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text title = StoryChapterBuilderCommon.CreateText(
            "Title", header.transform, bold, 34f, new Color32(255, 190, 58, 255),
            TextAlignmentOptions.Left, new Vector2(-18f, 25f), new Vector2(780f, 46f));
        title.text = "YANGINA MÜDAHALE";
        TMP_Text mission = StoryChapterBuilderCommon.CreateText(
            "Mission", header.transform, semibold, 20f, Color.white,
            TextAlignmentOptions.Left, new Vector2(-18f, -25f), new Vector2(780f, 44f));
        mission.text = "BASILI TUT • SUYU ALEVE YÖNLENDİR";

        GameObject timerCard = StoryChapterBuilderCommon.CreatePanel(
            "TimerCard", safeArea.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(168f, -258f), new Vector2(276f, 82f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        TMP_Text timer = StoryChapterBuilderCommon.CreateText(
            "Timer", timerCard.transform, bold, 31f, StoryChapterBuilderCommon.Navy,
            TextAlignmentOptions.Center, new Vector2(0f, 8f), new Vector2(230f, 44f));
        timer.text = "32 sn";
        Image timerTrack = AddImage(
            "TimerTrack", timerCard.transform, new Vector2(0f, -27f), new Vector2(222f, 7f),
            new Color32(167, 180, 187, 255));
        Image timerFill = AddImage(
            "TimerFill", timerTrack.transform, Vector2.zero, new Vector2(222f, 7f),
            new Color32(241, 91, 62, 255));
        RectTransform fillRect = timerFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillAmount = 1f;

        GameObject fireCard = StoryChapterBuilderCommon.CreatePanel(
            "FireCounterCard", safeArea.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-168f, -258f), new Vector2(276f, 82f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text remaining = StoryChapterBuilderCommon.CreateText(
            "RemainingFire", fireCard.transform, bold, 28f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 7f), new Vector2(240f, 40f));
        remaining.text = "05 YANGIN";
        TMP_Text fireCaption = StoryChapterBuilderCommon.CreateText(
            "FireCaption", fireCard.transform, semibold, 13f, new Color32(255, 187, 83, 255),
            TextAlignmentOptions.Center, new Vector2(0f, -25f), new Vector2(230f, 22f));
        fireCaption.text = "KALAN HEDEF";

        GameObject hint = StoryChapterBuilderCommon.CreatePanel(
            "ControlHint", safeArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 78f), new Vector2(760f, 70f),
            Color.white, false, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBluePanel);
        TMP_Text hintText = StoryChapterBuilderCommon.CreateText(
            "HintText", hint.transform, bold, 22f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(690f, 44f));
        hintText.text = "BASILI TUT  •  ALEVE NİŞAN AL";
        hintText.enableAutoSizing = true;
        hintText.fontSizeMin = 18f;
        hintText.fontSizeMax = 22f;

        Image overlay = AddStretchImage(
            "CompletionPanel", safeArea.transform, new Color32(6, 17, 33, 238), true);
        GameObject resultCard = StoryChapterBuilderCommon.CreatePanel(
            "ResultCard", overlay.transform,
            Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, 30f), new Vector2(870f, 720f),
            Color.white, true, StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulCreamPanel);
        TMP_Text resultTitle = StoryChapterBuilderCommon.CreateText(
            "ResultTitle", resultCard.transform, bold, 55f, StoryChapterBuilderCommon.Navy,
            TextAlignmentOptions.Center, new Vector2(0f, 225f), new Vector2(760f, 110f));
        resultTitle.text = "TÜM YANGINLAR SÖNDÜ!";
        resultTitle.enableAutoSizing = true;
        resultTitle.fontSizeMin = 38f;
        resultTitle.fontSizeMax = 55f;
        TMP_Text resultDetail = StoryChapterBuilderCommon.CreateText(
            "ResultDetail", resultCard.transform, semibold, 31f, new Color32(52, 78, 96, 255),
            TextAlignmentOptions.Center, new Vector2(0f, 72f), new Vector2(720f, 150f));
        resultDetail.text = "5/5 yangın • 00.0 saniye";
        Button retry = StoryChapterBuilderCommon.CreateButton(
            "RetryButton", resultCard.transform, "TEKRAR OYNA", bold,
            Vector2.one * 0.5f, new Vector2(0f, -90f), new Vector2(590f, 105f),
            StoryChapterBuilderCommon.Coral, Color.white);
        Button hub = StoryChapterBuilderCommon.CreateButton(
            "ReturnHubButton", resultCard.transform, "MERKEZE DÖN", bold,
            Vector2.one * 0.5f, new Vector2(0f, -220f), new Vector2(590f, 105f),
            StoryChapterBuilderCommon.Teal, Color.white);
        UnityEventTools.AddPersistentListener(retry.onClick, manager.Restart);
        UnityEventTools.AddPersistentListener(hub.onClick, manager.ReturnToHub);
        overlay.gameObject.SetActive(false);

        return new HudRefs
        {
            timer = timer,
            remaining = remaining,
            mission = mission,
            timerFill = timerFill,
            completionPanel = overlay.gameObject,
            resultTitle = resultTitle,
            resultDetail = resultDetail
        };
    }

    private static void ConfigureManager(
        FirefighterExtinguishManager manager,
        MinigameProgressManager progress,
        Camera camera,
        GameObject firefighter,
        NozzleRigRefs nozzle,
        LineRenderer waterStream,
        ParticleSystem waterImpact,
        List<FireTargetBinding> fires,
        HudRefs hud,
        AudioSource sprayAudio,
        AudioSource extinguishAudio)
    {
        SerializedObject data = new SerializedObject(manager);
        data.FindProperty("worldCamera").objectReferenceValue = camera;
        data.FindProperty("nozzleTip").objectReferenceValue = nozzle.tip;
        data.FindProperty("waterStream").objectReferenceValue = waterStream;
        data.FindProperty("waterImpact").objectReferenceValue = waterImpact;
        data.FindProperty("firefighterRoot").objectReferenceValue = firefighter.transform;
        data.FindProperty("spineBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.Spine);
        data.FindProperty("chestBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.Chest) ??
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.Spine);
        data.FindProperty("rightUpperArmBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.RightUpperArm);
        data.FindProperty("rightLowerArmBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.RightLowerArm);
        data.FindProperty("rightHandBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.RightHand);
        data.FindProperty("leftUpperArmBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.LeftUpperArm);
        data.FindProperty("leftLowerArmBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.LeftLowerArm);
        data.FindProperty("leftHandBone").objectReferenceValue =
            StoryChapterBuilderCommon.FindHumanoidBone(firefighter, HumanBodyBones.LeftHand);
        data.FindProperty("nozzleRig").objectReferenceValue = nozzle.rig;
        data.FindProperty("rightNozzleGrip").objectReferenceValue = nozzle.rightGrip;
        data.FindProperty("leftNozzleGrip").objectReferenceValue = nozzle.leftGrip;
        data.FindProperty("supplyHose").objectReferenceValue = nozzle.supplyHose;
        SerializedProperty fireData = data.FindProperty("fires");
        fireData.arraySize = fires.Count;
        for (int index = 0; index < fires.Count; index++)
        {
            SerializedProperty entry = fireData.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("hitCollider").objectReferenceValue = fires[index].hitCollider;
            entry.FindPropertyRelative("visualRoot").objectReferenceValue = fires[index].visualRoot;
            entry.FindPropertyRelative("glow").objectReferenceValue = fires[index].glow;
        }
        data.FindProperty("timerText").objectReferenceValue = hud.timer;
        data.FindProperty("remainingText").objectReferenceValue = hud.remaining;
        data.FindProperty("missionText").objectReferenceValue = hud.mission;
        data.FindProperty("timerFill").objectReferenceValue = hud.timerFill;
        data.FindProperty("completionPanel").objectReferenceValue = hud.completionPanel;
        data.FindProperty("resultTitleText").objectReferenceValue = hud.resultTitle;
        data.FindProperty("resultDetailText").objectReferenceValue = hud.resultDetail;
        data.FindProperty("sprayAudio").objectReferenceValue = sprayAudio;
        data.FindProperty("extinguishAudio").objectReferenceValue = extinguishAudio;
        data.FindProperty("progressManager").objectReferenceValue = progress;
        data.FindProperty("gameDuration").floatValue = 32f;
        data.FindProperty("extinguishSeconds").floatValue = 0.85f;
        data.FindProperty("sprayRadius").floatValue = 1.05f;
        data.FindProperty("aimPlaneHeight").floatValue = 0.65f;
        data.FindProperty("bodyTurnSpeed").floatValue = 9.5f;
        data.FindProperty("upperBodyAimWeight").floatValue = 0.78f;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static AudioSource BuildAudio(
        Transform parent,
        string name,
        string clipName,
        float volume,
        bool loop)
    {
        GameObject audioObject = new GameObject(name);
        audioObject.transform.SetParent(parent, false);
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = StoryChapterBuilderCommon.LoadLicensedSfx(clipName);
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static Material CreateEffectMaterial(
        string name,
        Color color,
        bool additive,
        bool softParticle)
    {
        string path = GeneratedMaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                        Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("URP unlit shader bulunamadı.");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetShaderPassEnabled("DepthOnly", false);
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.renderQueue = 3000;
        if (softParticle)
        {
            Texture2D texture = GetOrCreateSoftParticleTexture();
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D GetOrCreateSoftParticleTexture()
    {
        const string path = GeneratedMaterialRoot + "/Firefighter_SoftParticle.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
            return texture;
        texture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true)
        {
            name = "Firefighter_SoftParticle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float dx = (x + 0.5f) / 64f * 2f - 1f;
            float dy = (y + 0.5f) / 64f * 2f - 1f;
            float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            alpha = alpha * alpha * (3f - 2f * alpha);
            pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static void CaptureThumbnail(Camera camera)
    {
        const int width = 540;
        const int height = 960;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.aspect = width / (float)height;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            string absolute = Path.Combine(
                Directory.GetCurrentDirectory(),
                ThumbnailPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? Directory.GetCurrentDirectory());
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(ThumbnailPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(ThumbnailPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("İtfaiyeci thumbnail importer bulunamadı.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    private static Transform NewRoot(string name) => new GameObject(name).transform;

    private static Transform NewPoint(string name, Transform parent, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
        point.transform.localRotation = Quaternion.identity;
        return point.transform;
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

    private static Image AddImage(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = StoryChapterBuilderCommon.CreateUIRect(
            name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image AddStretchImage(string name, Transform parent, Color color, bool raycast)
    {
        GameObject imageObject = StoryChapterBuilderCommon.CreateUIRect(
            name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static void EnsureFolder(string path)
    {
        string normalized = path.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalized))
            return;
        string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
        string folder = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folder))
            throw new InvalidOperationException("Klasör yolu geçersiz: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private sealed class HudRefs
    {
        internal TMP_Text timer;
        internal TMP_Text remaining;
        internal TMP_Text mission;
        internal Image timerFill;
        internal GameObject completionPanel;
        internal TMP_Text resultTitle;
        internal TMP_Text resultDetail;
    }

    private sealed class NozzleRigRefs
    {
        internal Transform rig;
        internal Transform tip;
        internal Transform rightGrip;
        internal Transform leftGrip;
        internal LineRenderer supplyHose;
    }
}
