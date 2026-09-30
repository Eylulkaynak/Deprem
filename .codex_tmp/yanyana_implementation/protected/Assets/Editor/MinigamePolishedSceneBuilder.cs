using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Minigames;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Beş polished deprem minigame sahnesi ve merkez için editor-only authoring.
/// Bütün runtime davranışı üç ortak manager ve sahneye bağlı UnityEvent verileriyle kurulur.
/// </summary>
public static partial class MinigamePolishedSceneBuilder
{
    private const string GeneratedRoot = "Assets/Story/Generated/Minigames";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string TimelineRoot = GeneratedRoot + "/Timelines";
    private const string ThumbnailRoot = "Assets/Story/UI/Minigames/Thumbnails";
    private const string VoiceRoot = "Assets/Story/Audio/Voices/Minigames";
    private const string MixerPath = "Assets/Story/Audio/DepremMinigames.mixer";
    private const string TownRoot = "Assets/Story/Environment/SyntyTown";
    private const string CityRoot = "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";
    private const string FurnitureRoot = "Assets/ithappy/Cute_Furniture_Free/Prefabs";
    private const string SurvivalRoot = "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX";

    private static readonly string[] RequiredRoots =
    {
        "Environment", "Characters", "Gameplay", "Cameras",
        "LightingVFX", "Audio", "UI", "EventSystem"
    };

    private static readonly Dictionary<string, VoiceManifest> VoiceManifestCache =
        new Dictionary<string, VoiceManifest>(StringComparer.Ordinal);

    [MenuItem("Tools/Deprem Story/Minigames/Build All Polished Minigames")]
    public static void BuildAllFromMenu() => BuildAll(true);

    [MenuItem("Tools/Deprem Story/Minigames/Build All Polished Minigames (Silent)")]
    public static void BuildAllSilentFromMenu() => BuildAll(false);

    [MenuItem("Tools/Deprem Story/Minigames/Validate All Polished Minigames")]
    public static void ValidateAllFromMenu() => ValidateAll(true);

    [MenuItem("Tools/Deprem Story/Minigames/Capture Polished QA Only")]
    public static void CapturePolishedQaFromMenu()
    {
        CaptureAllThumbnails();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MinigameSceneCatalog.HubPath) != null)
            CaptureHubQa();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Minigame portre QA kareleri yenilendi.");
    }

    [MenuItem("Tools/Deprem Story/Minigames/Aftershock Cover/Build")]
    public static void BuildAftershockFromMenu() => BuildAftershockCover(true);

    [MenuItem("Tools/Deprem Story/Minigames/Aftershock Cover/Build Silent")]
    public static void BuildAftershockSilentFromMenu() => BuildAftershockCover(false);

    [MenuItem("Tools/Deprem Story/Minigames/Aftershock Cover/Validate")]
    public static void ValidateAftershockFromMenu() => ValidateSceneAtPath(MinigameSceneCatalog.AftershockCoverPath, true);

    [MenuItem("Tools/Deprem Story/Minigames/Room Safety/Build")]
    public static void BuildRoomSafetyFromMenu() => BuildRoomSafety(true);

    [MenuItem("Tools/Deprem Story/Minigames/Room Safety/Build Silent")]
    public static void BuildRoomSafetySilentFromMenu() => BuildRoomSafety(false);

    [MenuItem("Tools/Deprem Story/Minigames/Room Safety/Validate")]
    public static void ValidateRoomSafetyFromMenu() => ValidateSceneAtPath(MinigameSceneCatalog.RoomSafetyPath, true);

    [MenuItem("Tools/Deprem Story/Minigames/Room Safety/Capture QA")]
    public static void CaptureRoomSafetyQaFromMenu()
    {
        const string qaRoot = "Assets/Screenshots/MinigameQA";
        EnsureFolder("Assets/Screenshots");
        EnsureFolder(qaRoot);
        foreach (string guid in AssetDatabase.FindAssets("room-safety t:Texture2D", new[] { qaRoot }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        HubCardSpec roomSafety = HubCardSpecs().First(spec => spec.id == "room-safety");
        CaptureEveryAuthoredStage(roomSafety, qaRoot, new[]
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(720, 1280),
            new Vector2Int(1440, 2560)
        });
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Room Safety portre QA kareleri yenilendi.");
    }

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Bag Rush/Build")]
    public static void BuildBagRushFromMenu() => BuildEmergencyBagRush(true);

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Bag Rush/Build Silent")]
    public static void BuildBagRushSilentFromMenu() => BuildEmergencyBagRush(false);

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Bag Rush/Validate")]
    public static void ValidateBagRushFromMenu() => ValidateSceneAtPath(MinigameSceneCatalog.EmergencyBagRushPath, true);

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Corridor/Build")]
    public static void BuildCorridorFromMenu() => BuildEmergencyCorridor(true);

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Corridor/Build Silent")]
    public static void BuildCorridorSilentFromMenu() => BuildEmergencyCorridor(false);

    [MenuItem("Tools/Deprem Story/Minigames/Emergency Corridor/Validate")]
    public static void ValidateCorridorFromMenu() => ValidateSceneAtPath(MinigameSceneCatalog.EmergencyCorridorPath, true);

    [MenuItem("Tools/Deprem Story/Minigames/Rubble Signal/Build")]
    public static void BuildRubbleSignalFromMenu() => BuildRubbleSignal(true);

    [MenuItem("Tools/Deprem Story/Minigames/Rubble Signal/Build Silent")]
    public static void BuildRubbleSignalSilentFromMenu() => BuildRubbleSignal(false);

    [MenuItem("Tools/Deprem Story/Minigames/Rubble Signal/Validate")]
    public static void ValidateRubbleSignalFromMenu() => ValidateSceneAtPath(MinigameSceneCatalog.RubbleSignalPath, true);

    [MenuItem("Tools/Deprem Story/Minigames/Hub/Build")]
    public static void BuildHubFromMenu() => BuildHub(true);

    [MenuItem("Tools/Deprem Story/Minigames/Hub/Build Silent")]
    public static void BuildHubSilentFromMenu() => BuildHub(false);

    [MenuItem("Tools/Deprem Story/Minigames/Hub/Validate")]
    public static void ValidateHubFromMenu() => ValidateHub(true);

    public static void BuildAll(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Minigame builder Play Mode dışında çalıştırılmalıdır.");

        EnsureFolders();
        StoryAnimationLibraryBuilder.BuildLibrary(false);
        EnsureAudioMixer();
        BuildAftershockCover(false);
        BuildRoomSafety(false);
        BuildEmergencyBagRush(false);
        BuildEmergencyCorridor(false);
        BuildRubbleSignal(false);
        IntegrateExistingMinigames();
        CaptureAllThumbnails();
        BuildHub(false);
        CaptureHubQa();
        StoryRebuildFlowMenuBuilder.Build(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        MinigameSceneCatalog.PublishBuildSettings();
        AssetDatabase.SaveAssets();
        ValidateAll(false);

        Debug.Log("Altı polished deprem minigame sahnesi, merkez ve entegrasyon tamamlandı.");
        if (showDialog)
            EditorUtility.DisplayDialog(
                "Deprem Minigames",
                "Altı yeni sahne, sekiz kartlı merkez, ses/altyazı ve merkezi build kataloğu üretildi.",
                "Tamam");
    }

    private static void IntegrateExistingMinigames()
    {
        StoryFiretruckRunnerSceneBuilder.BuildFromCommandLine();
        StoryFirefighterExtinguishSceneBuilder.BuildFromCommandLine();
        // 2.5D sahne kardeş görevde polished halde üretildi. Builder entegrasyonu
        // kalıcı olduğu için yalnız sahne yoksa yeniden üret; kullanıcı sahnesini gereksiz yere ezme.
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MinigameSceneCatalog.Evacuation25DPath) == null)
            StoryEvacuation25DSceneBuilder.BuildSilentFromMenu();
        else
            IntegrateEvacuation25DInPlace();
    }

    private static SceneContext CreateContext(
        string scenePath,
        string id,
        string title,
        string voiceFolder,
        Vector3 cameraPosition,
        Vector3 cameraTarget,
        Color skyColor,
        bool night,
        bool searchSilence = false)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneContext context = new SceneContext
        {
            scene = scene,
            scenePath = scenePath,
            id = id,
            title = title,
            voiceFolder = voiceFolder,
            materials = CreateMaterials(),
            audio = EnsureAudioMixer()
        };

        context.environment = new GameObject("Environment").transform;
        context.characters = new GameObject("Characters").transform;
        context.gameplay = new GameObject("Gameplay").transform;
        context.cameras = new GameObject("Cameras").transform;
        context.lightingVfx = new GameObject("LightingVFX").transform;
        context.audioRoot = new GameObject("Audio").transform;
        context.uiRoot = new GameObject("UI").transform;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        context.camera = BuildCamera(
            context.cameras,
            cameraPosition,
            cameraTarget,
            skyColor,
            scene.name);
        context.intro = BuildIntroTimeline(
            context.cameras,
            context.camera,
            scene.name,
            cameraPosition + new Vector3(0f, 0.7f, -1.25f),
            cameraPosition,
            1.25f);
        BuildLighting(context, night);
        BuildAudio(context, searchSilence);
        context.progress = context.gameplay.gameObject.AddComponent<MinigameProgressManager>();
        context.session = context.gameplay.gameObject.AddComponent<MinigameSessionManager>();
        context.hud = BuildGameplayHud(context);
        SetField(context.session, "minigameId", id);
        SetField(context.session, "displayName", title);
        SetField(context.session, "progressManager", context.progress);
        SetField(context.session, "introTimeline", context.intro);
        SetField(context.session, "worldCamera", context.camera);
        SetField(context.session, "gameplaySnapshot", context.audio.gameplay);
        SetField(context.session, "voiceSnapshot", context.audio.voiceDucked);
        SetField(context.session, "silenceSnapshot", context.audio.searchSilence);
        BindHud(context.session, context.hud);
        return context;
    }

    private static void FinishScene(SceneContext context, MinigameStageDefinition[] stages, bool showDialog)
    {
        SetField(context.session, "stages", stages);
        SetField(context.session, "voiceSource", context.voiceSource);
        SetField(context.session, "sfxSource", context.sfxSource);
        SetField(context.session, "swipeThresholdPixels", 92f);
        SetField(context.session, "dropRadiusPixels", 152f);
        EditorUtility.SetDirty(context.session);

        if (stages.Length > 0)
        {
            context.hud.title.text = context.title;
            context.hud.objective.text = stages[0].objective;
            context.hud.subtitle.text = stages[0].subtitle;
            context.hud.stage.text = $"AŞAMA 1 / {stages.Length}";
            context.hud.score.text = "PUAN 1000";
            context.hud.timer.text = stages[0].stageTimeLimitSeconds > 0f
                ? $"SÜRE {Mathf.CeilToInt(stages[0].stageTimeLimitSeconds):00}"
                : "SÜRE --";
        }

        MarkEnvironmentStatic(context.environment);
        ConfigureCharacterAnimators(context.characters);
        EditorSceneManager.MarkSceneDirty(context.scene);
        EditorSceneManager.SaveScene(context.scene, context.scenePath);
        AssetDatabase.SaveAssets();
        ValidateScene(context.scene, context.scenePath);
        MinigameSceneCatalog.PublishBuildSettings();
        Selection.activeGameObject = context.camera.gameObject;

        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Minigames", context.title + " sahnesi üretildi.", "Tamam");
    }

    private static Camera BuildCamera(
        Transform parent,
        Vector3 position,
        Vector3 target,
        Color background,
        string sceneName)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;
        cameraObject.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 46f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 180f;
        camera.backgroundColor = background;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        return camera;
    }

    private static PlayableDirector BuildIntroTimeline(
        Transform parent,
        Camera camera,
        string sceneName,
        Vector3 from,
        Vector3 to,
        float duration)
    {
        string safeName = Sanitize(sceneName);
        string timelinePath = TimelineRoot + "/" + safeName + "_Intro.playable";
        string clipPath = TimelineRoot + "/" + safeName + "_IntroCamera.anim";
        DeleteOwnedAsset(timelinePath);
        DeleteOwnedAsset(clipPath);

        // Animate a camera child instead of the bound Animator root. Timeline treats
        // root Transform curves as root motion, which can replace the authored world
        // pose with an origin-relative pose when playback starts. The rig owns the
        // final gameplay pose; stopping or completing the Timeline therefore always
        // restores the exact portrait composition authored by BuildCamera.
        GameObject rigObject = new GameObject("IntroCameraRig", typeof(Animator));
        rigObject.transform.SetParent(parent);
        rigObject.transform.position = to;
        rigObject.transform.rotation = camera.transform.rotation;
        camera.transform.SetParent(rigObject.transform, true);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        Vector3 fromLocal = rigObject.transform.InverseTransformPoint(from);

        AnimationClip animationClip = new AnimationClip { name = safeName + "_IntroCamera", legacy = false };
        animationClip.SetCurve(camera.name, typeof(Transform), "m_LocalPosition.x",
            AnimationCurve.EaseInOut(0f, fromLocal.x, duration, 0f));
        animationClip.SetCurve(camera.name, typeof(Transform), "m_LocalPosition.y",
            AnimationCurve.EaseInOut(0f, fromLocal.y, duration, 0f));
        animationClip.SetCurve(camera.name, typeof(Transform), "m_LocalPosition.z",
            AnimationCurve.EaseInOut(0f, fromLocal.z, duration, 0f));
        AssetDatabase.CreateAsset(animationClip, clipPath);

        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = safeName + "_Intro";
        AssetDatabase.CreateAsset(timeline, timelinePath);
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Intro Camera Glide");
        TimelineClip timelineClip = track.CreateClip(animationClip);
        timelineClip.displayName = "Portrait Establishing Shot";
        timelineClip.duration = duration;

        PlayableDirector director = parent.gameObject.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        director.playableAsset = timeline;
        director.SetGenericBinding(track, rigObject.GetComponent<Animator>());
        return director;
    }

    private static void BuildLighting(SceneContext context, bool night)
    {
        GameObject sunObject = new GameObject("Key Directional Light");
        sunObject.transform.SetParent(context.lightingVfx);
        sunObject.transform.rotation = Quaternion.Euler(night ? 34f : 48f, night ? -36f : -28f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = night ? new Color32(154, 188, 255, 255) : new Color32(255, 226, 187, 255);
        sun.intensity = night ? 0.78f : 1.08f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.62f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;

        GameObject fillObject = new GameObject("Cyan Fill Light");
        fillObject.transform.SetParent(context.lightingVfx);
        fillObject.transform.position = new Vector3(-3.5f, 4.8f, -2f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color32(77, 216, 230, 255);
        fill.intensity = night ? 3.1f : 1.65f;
        fill.range = 12f;
        fill.shadows = LightShadows.None;

        GameObject warmObject = new GameObject("Amber Practical Light");
        warmObject.transform.SetParent(context.lightingVfx);
        warmObject.transform.position = new Vector3(3.5f, 3.2f, 1.5f);
        Light warm = warmObject.AddComponent<Light>();
        warm.type = LightType.Point;
        warm.color = new Color32(255, 178, 91, 255);
        warm.intensity = night ? 2.1f : 1.0f;
        warm.range = 8f;
        warm.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("Global Volume");
        volumeObject.transform.SetParent(context.lightingVfx);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.sharedProfile = StoryChapterBuilderCommon.CreateVolumeProfile();
    }

    private static void BuildAudio(SceneContext context, bool searchSilence)
    {
        context.voiceSource = CreateAudioSource("Voice", context.audioRoot, false, 1f, context.audio.voiceGroup);
        context.sfxSource = CreateAudioSource("SFX", context.audioRoot, false, 0.75f, context.audio.sfxGroup);
        AudioSource ambience = CreateAudioSource("Ambience", context.audioRoot, true, 0.11f, context.audio.ambienceGroup);
        ambience.clip = StoryChapterBuilderCommon.LoadLicensedSfx(
            searchSilence ? "sfx100v2_loop_wind_01.ogg" : "sfx100v2_loop_air_conditioner_01.ogg");
        ambience.playOnAwake = ambience.clip != null;
    }

    private static AudioSource CreateAudioSource(
        string name,
        Transform parent,
        bool loop,
        float volume,
        AudioMixerGroup group)
    {
        GameObject audioObject = new GameObject(name);
        audioObject.transform.SetParent(parent);
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.volume = volume;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = group;
        source.spatialBlend = 0f;
        return source;
    }

    private static GameplayHud BuildGameplayHud(SceneContext context)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);

        GameObject canvasObject = new GameObject(
            "MinigameCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(context.uiRoot, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = StoryChapterBuilderCommon.CreateUIRect(
            "SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();

        GameObject topCard = StoryChapterBuilderCommon.CreatePanel(
            "MissionHeader",
            safeArea.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -132f),
            new Vector2(980f, 210f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text title = StoryChapterBuilderCommon.CreateText(
            "MinigameTitle", topCard.transform, bold, 38f, Color.white,
            TextAlignmentOptions.Left, new Vector2(-215f, 58f), new Vector2(500f, 58f));
        title.enableAutoSizing = true;
        title.fontSizeMin = 26f;
        title.fontSizeMax = 38f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text stage = StoryChapterBuilderCommon.CreateText(
            "StageCounter", topCard.transform, semibold, 24f, new Color32(118, 223, 239, 255),
            TextAlignmentOptions.Right, new Vector2(300f, 60f), new Vector2(250f, 42f));
        TMP_Text objective = StoryChapterBuilderCommon.CreateText(
            "Objective", topCard.transform, semibold, 31f, new Color32(252, 246, 226, 255),
            TextAlignmentOptions.Left, new Vector2(0f, -10f), new Vector2(880f, 76f));
        objective.enableAutoSizing = true;
        objective.fontSizeMin = 25f;
        objective.fontSizeMax = 31f;
        TMP_Text score = StoryChapterBuilderCommon.CreateText(
            "Score", topCard.transform, bold, 25f, new Color32(255, 193, 86, 255),
            TextAlignmentOptions.Left, new Vector2(-270f, -78f), new Vector2(280f, 40f));
        TMP_Text timer = StoryChapterBuilderCommon.CreateText(
            "Timer", topCard.transform, bold, 25f, Color.white,
            TextAlignmentOptions.Right, new Vector2(315f, -78f), new Vector2(220f, 40f));

        GameObject subtitleCard = StoryChapterBuilderCommon.CreatePanel(
            "SubtitleCard",
            safeArea.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 105f),
            new Vector2(980f, 142f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text subtitle = StoryChapterBuilderCommon.CreateText(
            "Subtitle", subtitleCard.transform, semibold, 27f, Color.white,
            TextAlignmentOptions.Left, new Vector2(76f, 0f), new Vector2(700f, 94f));
        subtitle.enableAutoSizing = true;
        subtitle.fontSizeMin = 24f;
        subtitle.fontSizeMax = 27f;
        GameObject speakerBadgeRoot = StoryChapterBuilderCommon.CreateUIRect(
            "SpeakerBadge", subtitleCard.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-378f, 0f), new Vector2(154f, 66f));
        Image speakerBadgeBack = speakerBadgeRoot.AddComponent<Image>();
        speakerBadgeBack.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Yellow", "Normal");
        speakerBadgeBack.type = Image.Type.Sliced;
        speakerBadgeBack.color = new Color32(255, 190, 75, 255);
        speakerBadgeBack.raycastTarget = false;
        TMP_Text speakerBadge = StoryChapterBuilderCommon.CreateText(
            "SpeakerName", speakerBadgeRoot.transform, bold, 24f, new Color32(13, 34, 53, 255),
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(138f, 54f));
        speakerBadge.raycastTarget = false;

        GameObject gestureCard = StoryChapterBuilderCommon.CreatePanel(
            "GestureCoach",
            safeArea.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 250f),
            new Vector2(980f, 128f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBluePanel);
        CanvasGroup gestureCoachGroup = gestureCard.AddComponent<CanvasGroup>();
        gestureCoachGroup.alpha = 0f;
        gestureCoachGroup.blocksRaycasts = false;
        Image gestureCardImage = gestureCard.GetComponent<Image>();
        if (gestureCardImage != null)
            gestureCardImage.raycastTarget = false;

        GameObject motionRootObject = StoryChapterBuilderCommon.CreateUIRect(
            "GestureMotion", gestureCard.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-409f, 0f), new Vector2(90f, 90f));
        RectTransform gestureMotionRoot = motionRootObject.GetComponent<RectTransform>();
        Image motionDisc = motionRootObject.AddComponent<Image>();
        motionDisc.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Yellow", "Normal");
        motionDisc.type = Image.Type.Sliced;
        motionDisc.color = new Color32(255, 191, 72, 255);
        motionDisc.raycastTarget = false;
        TMP_Text gestureArrow = StoryChapterBuilderCommon.CreateText(
            "GestureSymbolFallback", motionRootObject.transform, bold, 42f, new Color32(13, 33, 52, 255),
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(74f, 70f));
        gestureArrow.raycastTarget = false;
        GameObject gestureIconObject = StoryChapterBuilderCommon.CreateUIRect(
            "GestureIcon", motionRootObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image gestureIcon = gestureIconObject.AddComponent<Image>();
        gestureIcon.preserveAspect = true;
        gestureIcon.color = new Color32(14, 38, 57, 255);
        gestureIcon.raycastTarget = false;
        TMP_Text gestureVerb = StoryChapterBuilderCommon.CreateText(
            "GestureVerb", gestureCard.transform, bold, 29f, new Color32(255, 196, 78, 255),
            TextAlignmentOptions.Left, new Vector2(-135f, 29f), new Vector2(280f, 42f));
        gestureVerb.enableAutoSizing = true;
        gestureVerb.fontSizeMin = 23f;
        gestureVerb.fontSizeMax = 29f;
        gestureVerb.raycastTarget = false;
        TMP_Text gestureDetail = StoryChapterBuilderCommon.CreateText(
            "GestureDetail", gestureCard.transform, semibold, 24f, Color.white,
            TextAlignmentOptions.Left, new Vector2(66f, -25f), new Vector2(700f, 48f));
        gestureDetail.enableAutoSizing = true;
        gestureDetail.fontSizeMin = 19f;
        gestureDetail.fontSizeMax = 24f;
        gestureDetail.raycastTarget = false;
        TMP_Text gestureProgress = StoryChapterBuilderCommon.CreateText(
            "GestureProgress", gestureCard.transform, bold, 22f, new Color32(117, 238, 223, 255),
            TextAlignmentOptions.Right, new Vector2(368f, 31f), new Vector2(188f, 36f));
        gestureProgress.raycastTarget = false;

        GameObject worldTrailObject = StoryChapterBuilderCommon.CreateUIRect(
            "WorldGestureTrail", safeArea.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(150f, 10f));
        RectTransform worldTrail = worldTrailObject.GetComponent<RectTransform>();
        Image worldTrailImage = worldTrailObject.AddComponent<Image>();
        worldTrailImage.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Blue", "Normal");
        worldTrailImage.type = Image.Type.Sliced;
        worldTrailImage.color = new Color32(79, 226, 216, 185);
        worldTrailImage.raycastTarget = false;
        worldTrailObject.SetActive(false);

        GameObject worldGestureObject = StoryChapterBuilderCommon.CreateUIRect(
            "WorldGestureCoach", safeArea.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(126f, 126f));
        RectTransform worldGestureRoot = worldGestureObject.GetComponent<RectTransform>();
        CanvasGroup worldGestureGroup = worldGestureObject.AddComponent<CanvasGroup>();
        worldGestureGroup.alpha = 0f;
        worldGestureGroup.blocksRaycasts = false;
        Image worldGestureIcon = worldGestureObject.AddComponent<Image>();
        worldGestureIcon.preserveAspect = true;
        worldGestureIcon.color = new Color32(79, 226, 216, 255);
        worldGestureIcon.raycastTarget = false;
        GameObject worldLabelCard = StoryChapterBuilderCommon.CreatePanel(
            "WorldActionLabel", worldGestureObject.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, 98f), new Vector2(430f, 62f), Color.white, false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text nextActionText = StoryChapterBuilderCommon.CreateText(
            "NextAction", worldLabelCard.transform, bold, 22f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(388f, 44f));
        nextActionText.enableAutoSizing = true;
        nextActionText.fontSizeMin = 17f;
        nextActionText.fontSizeMax = 22f;
        DisableUiRaycasts(worldLabelCard);

        GameObject feedbackCard = StoryChapterBuilderCommon.CreatePanel(
            "FeedbackToast",
            safeArea.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, -450f),
            new Vector2(820f, 126f),
            Color.white,
            false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBluePanel);
        CanvasGroup feedbackGroup = feedbackCard.AddComponent<CanvasGroup>();
        feedbackGroup.alpha = 0f;
        TMP_Text feedback = StoryChapterBuilderCommon.CreateText(
            "FeedbackText", feedbackCard.transform, bold, 29f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(740f, 84f));
        feedback.enableAutoSizing = true;
        feedback.fontSizeMin = 22f;
        feedback.fontSizeMax = 29f;

        GameObject holdRoot = StoryChapterBuilderCommon.CreateUIRect(
            "HoldProgress", safeArea.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -260f), new Vector2(170f, 170f));
        Image holdBack = holdRoot.AddComponent<Image>();
        holdBack.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Blue", "Pressed");
        holdBack.type = Image.Type.Filled;
        holdBack.fillMethod = Image.FillMethod.Radial360;
        holdBack.fillAmount = 1f;
        holdBack.color = new Color32(16, 38, 60, 0);
        GameObject holdFillObject = StoryChapterBuilderCommon.CreateUIRect(
            "HoldFill", holdRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image holdFill = holdFillObject.AddComponent<Image>();
        holdFill.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Yellow", "Normal");
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Radial360;
        holdFill.fillOrigin = 2;
        holdFill.fillClockwise = true;
        holdFill.fillAmount = 0f;
        holdFill.raycastTarget = false;

        DisableUiRaycasts(topCard);
        DisableUiRaycasts(subtitleCard);
        DisableUiRaycasts(feedbackCard);
        DisableUiRaycasts(holdRoot);
        feedbackGroup.blocksRaycasts = false;

        ResultHud result = BuildResultHud(safeArea.transform, bold, semibold, context.session);
        return new GameplayHud
        {
            title = title,
            objective = objective,
            subtitle = subtitle,
            stage = stage,
            score = score,
            timer = timer,
            feedback = feedback,
            feedbackGroup = feedbackGroup,
            feedbackRect = feedbackCard.GetComponent<RectTransform>(),
            holdProgress = holdFill,
            gestureCoachGroup = gestureCoachGroup,
            gestureVerb = gestureVerb,
            gestureDetail = gestureDetail,
            gestureProgress = gestureProgress,
            gestureMotionRoot = gestureMotionRoot,
            gestureArrow = gestureArrow,
            gestureIcon = gestureIcon,
            safeAreaRect = safeArea.GetComponent<RectTransform>(),
            worldGestureGroup = worldGestureGroup,
            worldGestureRoot = worldGestureRoot,
            worldGestureIcon = worldGestureIcon,
            worldGestureTrail = worldTrail,
            worldGestureTrailImage = worldTrailImage,
            nextActionText = nextActionText,
            speakerBadge = speakerBadge,
            result = result
        };
    }

    private static ResultHud BuildResultHud(
        Transform parent,
        TMP_FontAsset bold,
        TMP_FontAsset semibold,
        MinigameSessionManager session)
    {
        GameObject overlay = StoryChapterBuilderCommon.CreatePanel(
            "ResultPanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color32(6, 17, 33, 236), true);
        GameObject card = StoryChapterBuilderCommon.CreatePanel(
            "ResultCard", overlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(900f, 830f), Color.white, true,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text title = StoryChapterBuilderCommon.CreateText(
            "ResultTitle", card.transform, bold, 52f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 280f), new Vector2(780f, 90f));
        TMP_Text stars = StoryChapterBuilderCommon.CreateText(
            "ResultStars", card.transform, bold, 82f, new Color32(255, 191, 72, 255),
            TextAlignmentOptions.Center, new Vector2(0f, 150f), new Vector2(720f, 110f));
        TMP_Text detail = StoryChapterBuilderCommon.CreateText(
            "ResultDetail", card.transform, semibold, 29f, new Color32(205, 228, 238, 255),
            TextAlignmentOptions.Center, new Vector2(0f, 42f), new Vector2(760f, 100f));
        TMP_Text coins = StoryChapterBuilderCommon.CreateText(
            "ResultCoins", card.transform, bold, 44f, new Color32(98, 231, 215, 255),
            TextAlignmentOptions.Center, new Vector2(0f, -72f), new Vector2(700f, 72f));
        Button retry = StoryChapterBuilderCommon.CreateButton(
            "RetryButton", card.transform, "TEKRAR OYNA", bold, Vector2.one * 0.5f,
            new Vector2(0f, -205f), new Vector2(620f, 116f),
            StoryChapterBuilderCommon.Amber, StoryChapterBuilderCommon.Navy);
        Button hub = StoryChapterBuilderCommon.CreateButton(
            "HubButton", card.transform, "MİNİ OYUNLAR", bold, Vector2.one * 0.5f,
            new Vector2(0f, -338f), new Vector2(620f, 112f),
            StoryChapterBuilderCommon.Teal, Color.white);
        UnityEventTools.AddPersistentListener(retry.onClick, session.RetryScene);
        UnityEventTools.AddPersistentListener(hub.onClick, session.ReturnToHub);
        overlay.SetActive(false);
        return new ResultHud
        {
            root = overlay,
            title = title,
            detail = detail,
            stars = stars,
            coins = coins
        };
    }

    private static void BindHud(MinigameSessionManager session, GameplayHud hud)
    {
        SetField(session, "titleText", hud.title);
        SetField(session, "objectiveText", hud.objective);
        SetField(session, "subtitleText", hud.subtitle);
        SetField(session, "stageText", hud.stage);
        SetField(session, "scoreText", hud.score);
        SetField(session, "timerText", hud.timer);
        SetField(session, "feedbackText", hud.feedback);
        SetField(session, "feedbackGroup", hud.feedbackGroup);
        SetField(session, "feedbackRect", hud.feedbackRect);
        SetField(session, "holdProgress", hud.holdProgress);
        SetField(session, "gestureCoachGroup", hud.gestureCoachGroup);
        SetField(session, "gestureVerbText", hud.gestureVerb);
        SetField(session, "gestureDetailText", hud.gestureDetail);
        SetField(session, "gestureProgressText", hud.gestureProgress);
        SetField(session, "gestureMotionRoot", hud.gestureMotionRoot);
        SetField(session, "gestureArrowText", hud.gestureArrow);
        SetField(session, "gestureIconImage", hud.gestureIcon);
        SetField(session, "tapGestureSprite", LoadGestureSprite("touch_tap.png"));
        SetField(session, "repeatedTapGestureSprite", LoadGestureSprite("touch_tap_double.png"));
        SetField(session, "swipeDownGestureSprite", LoadGestureSprite("touch_swipe_down.png"));
        SetField(session, "swipeHorizontalGestureSprite", LoadGestureSprite("touch_swipe_horizontal.png"));
        SetField(session, "holdGestureSprite", LoadGestureSprite("touch_tap_hold.png"));
        SetField(session, "dragGestureSprite", LoadGestureSprite("touch_swipe_move.png"));
        SetField(session, "safeAreaRect", hud.safeAreaRect);
        SetField(session, "worldGestureGroup", hud.worldGestureGroup);
        SetField(session, "worldGestureRoot", hud.worldGestureRoot);
        SetField(session, "worldGestureIconImage", hud.worldGestureIcon);
        SetField(session, "worldGestureTrail", hud.worldGestureTrail);
        SetField(session, "worldGestureTrailImage", hud.worldGestureTrailImage);
        SetField(session, "nextActionText", hud.nextActionText);
        SetField(session, "speakerBadgeText", hud.speakerBadge);
        SetField(session, "resultPanel", hud.result.root);
        SetField(session, "resultTitleText", hud.result.title);
        SetField(session, "resultDetailText", hud.result.detail);
        SetField(session, "resultStarsText", hud.result.stars);
        SetField(session, "resultCoinsText", hud.result.coins);
    }

    private static Sprite LoadGestureSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Story/UI/ThirdParty/KenneyInputPrompts/" + fileName);
    }

    private static void DisableUiRaycasts(GameObject root)
    {
        if (root == null)
            return;
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private static MaterialPack CreateMaterials()
    {
        EnsureFolders();
        return new MaterialPack
        {
            navy = GetOrCreateMaterial("Minigame_Navy", new Color32(13, 28, 48, 255), 0.22f),
            cream = GetOrCreateMaterial("Minigame_Cream", new Color32(241, 232, 211, 255), 0.28f),
            cyan = GetOrCreateMaterial("Minigame_Cyan", new Color32(54, 195, 204, 255), 0.32f, true),
            amber = GetOrCreateMaterial("Minigame_Amber", new Color32(225, 151, 48, 255), 0.28f, true),
            danger = GetOrCreateMaterial("Minigame_Danger", new Color32(188, 64, 61, 255), 0.22f, true),
            safe = GetOrCreateMaterial("Minigame_Safe", new Color32(52, 154, 119, 255), 0.25f, true),
            wood = GetOrCreateMaterial("Minigame_Wood", new Color32(132, 86, 59, 255), 0.3f),
            wall = GetOrCreateMaterial("Minigame_Wall", new Color32(207, 205, 193, 255), 0.24f),
            floor = GetOrCreateMaterial("Minigame_Floor", new Color32(78, 58, 49, 255), 0.3f),
            concrete = GetOrCreateMaterial("Minigame_Concrete", new Color32(89, 97, 104, 255), 0.18f),
            asphalt = GetOrCreateMaterial("Minigame_Asphalt", new Color32(40, 48, 57, 255), 0.12f),
            glass = GetOrCreateGlassMaterial(),
            dark = GetOrCreateMaterial("Minigame_Dark", new Color32(22, 29, 37, 255), 0.18f),
            white = GetOrCreateMaterial("Minigame_White", new Color32(233, 236, 229, 255), 0.24f)
        };
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool emission = false)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.04f);
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            Color emissionColor = color.linear * 1.45f;
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateGlassMaterial()
    {
        Material material = GetOrCreateMaterial(
            "Minigame_Glass", new Color32(93, 185, 213, 110), 0.78f, false);
        material.DisableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", Color.black);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AudioSetup EnsureAudioMixer()
    {
        EnsureFolders();
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
            MethodInfo createMethod = controllerType?.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (createMethod == null)
                throw new InvalidOperationException("AudioMixerController factory bulunamadı.");
            createMethod.Invoke(null, new object[] { MixerPath });
            AssetDatabase.SaveAssets();
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }
        if (mixer == null)
            throw new InvalidOperationException("Minigame AudioMixer üretilemedi.");

        EnsureMixerGroup(mixer, "Voice");
        EnsureMixerGroup(mixer, "SFX");
        EnsureMixerGroup(mixer, "Ambience");
        EnsureMixerSnapshot(mixer, "Gameplay");
        EnsureMixerSnapshot(mixer, "VoiceDucked");
        EnsureMixerSnapshot(mixer, "SearchSilence");
        AssetDatabase.SaveAssets();
        mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        return new AudioSetup
        {
            mixer = mixer,
            voiceGroup = FindMixerGroup(mixer, "Voice"),
            sfxGroup = FindMixerGroup(mixer, "SFX"),
            ambienceGroup = FindMixerGroup(mixer, "Ambience"),
            gameplay = mixer.FindSnapshot("Gameplay") ?? mixer.FindSnapshot("Snapshot"),
            voiceDucked = mixer.FindSnapshot("VoiceDucked") ?? mixer.FindSnapshot("Snapshot"),
            searchSilence = mixer.FindSnapshot("SearchSilence") ?? mixer.FindSnapshot("Snapshot")
        };
    }

    private static void EnsureMixerGroup(AudioMixer mixer, string name)
    {
        if (mixer.FindMatchingGroups(name).Any(group => group.name == name))
            return;
        Type controllerType = mixer.GetType();
        MethodInfo method = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(candidate => candidate.Name == "CreateNewGroup" &&
                                         candidate.GetParameters().Length >= 1 &&
                                         candidate.GetParameters()[0].ParameterType == typeof(string));
        if (method == null)
            throw new InvalidOperationException("AudioMixer group factory bulunamadı: " + name);
        ParameterInfo[] parameters = method.GetParameters();
        object[] arguments = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i == 0)
                arguments[i] = name;
            else if (parameters[i].ParameterType == typeof(bool))
                arguments[i] = false;
            else
                arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
        }
        object created = method.Invoke(mixer, arguments);
        PropertyInfo masterProperty = controllerType.GetProperty(
            "masterGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo attachMethod = controllerType.GetMethod(
            "AddChildToParent",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object master = masterProperty?.GetValue(mixer);
        if (created == null || master == null || attachMethod == null)
            throw new InvalidOperationException("AudioMixer group bağlama API bulunamadı: " + name);
        attachMethod.Invoke(mixer, new[] { created, master });
        EditorUtility.SetDirty(mixer);
    }

    private static void EnsureMixerSnapshot(AudioMixer mixer, string name)
    {
        if (mixer.FindSnapshot(name) != null)
            return;
        Type controllerType = mixer.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo snapshotsProperty = controllerType.GetProperty("snapshots", flags);
        PropertyInfo targetProperty = controllerType.GetProperty("TargetSnapshot", flags);
        MethodInfo cloneMethod = controllerType.GetMethod(
            "CloneNewSnapshotFromTarget", flags, null, new[] { typeof(bool) }, null);
        Array snapshots = snapshotsProperty?.GetValue(mixer) as Array;
        if (targetProperty == null || cloneMethod == null || snapshots == null || snapshots.Length == 0)
            throw new InvalidOperationException("AudioMixer snapshot clone API bulunamadı: " + name);

        targetProperty.SetValue(mixer, snapshots.GetValue(0));
        cloneMethod.Invoke(mixer, new object[] { false });
        Object created = targetProperty.GetValue(mixer) as Object;
        if (created == null)
            throw new InvalidOperationException("AudioMixer snapshot üretilemedi: " + name);
        created.name = name;
        EditorUtility.SetDirty(created);
        EditorUtility.SetDirty(mixer);
    }

    private static AudioMixerGroup FindMixerGroup(AudioMixer mixer, string name)
    {
        AudioMixerGroup group = mixer.FindMatchingGroups(name).FirstOrDefault(candidate => candidate.name == name);
        if (group == null)
            throw new InvalidOperationException("AudioMixer group bulunamadı: " + name);
        return group;
    }

    private static Type FindEditorType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }
        return null;
    }

    private static MinigameStageDefinition Stage(
        SceneContext context,
        string id,
        string cueId,
        string objective,
        MinigameGesture gesture,
        GameObject stageRoot,
        MinigameActionDefinition[] actions,
        int required = 1,
        float hintSeconds = 11f,
        float timeLimit = 0f,
        float holdSeconds = 1.35f,
        string gestureInstruction = null,
        bool showGuides = true)
    {
        VoiceCue cue = LoadVoiceCue(context.voiceFolder, cueId);
        return new MinigameStageDefinition
        {
            stageId = id,
            objective = objective,
            subtitle = cue.subtitle,
            voiceClip = cue.clip,
            gesture = gesture,
            gestureInstruction = gestureInstruction ?? AuthoredGestureInstruction(context.id, id, gesture),
            showGuidesOnEnter = showGuides,
            requiredSuccesses = Mathf.Max(1, required),
            hintDelaySeconds = hintSeconds,
            mistakePenalty = 100,
            hintOrResetPenalty = 50,
            stageTimeLimitSeconds = timeLimit,
            holdDurationSeconds = holdSeconds,
            stageRoot = stageRoot,
            actions = actions ?? Array.Empty<MinigameActionDefinition>()
        };
    }

    private static MinigameStageDefinition PresentStage(
        SceneContext context,
        MinigameStageDefinition stage,
        string poseName,
        Vector3 cameraPosition,
        Vector3 cameraTarget,
        float fieldOfView,
        params MinigameCharacterCue[] characterCues)
    {
        GameObject poseObject = new GameObject("StageCamera_" + Sanitize(poseName));
        poseObject.transform.SetParent(context.cameras);
        poseObject.transform.position = cameraPosition;
        poseObject.transform.rotation = Quaternion.LookRotation(cameraTarget - cameraPosition, Vector3.up);
        stage.cameraPose = poseObject.transform;
        stage.cameraFieldOfView = Mathf.Clamp(fieldOfView, 28f, 60f);
        stage.cameraTransitionSeconds = 0.52f;
        stage.characterCues = characterCues ?? Array.Empty<MinigameCharacterCue>();
        return stage;
    }

    private static MinigameCharacterCue CharacterCue(
        GameObject character,
        float modelYawOffset,
        bool talks = false)
    {
        return new MinigameCharacterCue
        {
            characterRoot = character != null ? character.transform : null,
            animator = character != null ? character.GetComponentInChildren<Animator>(true) : null,
            enterTrigger = talks ? "StoryTalk" : string.Empty,
            modelYawOffset = modelYawOffset,
            faceCamera = true
        };
    }

    private static MinigameCharacterCue CharacterCueAt(
        GameObject character,
        float modelYawOffset,
        bool talks,
        Vector3 stagePosition)
    {
        MinigameCharacterCue cue = CharacterCue(character, modelYawOffset, talks);
        cue.useStagePosition = true;
        cue.stagePosition = stagePosition;
        return cue;
    }

    private static MinigameActionDefinition PolishAction(
        MinigameActionDefinition action,
        string label,
        Color guideColor,
        Vector2 expectedSwipeDirection = default,
        int requiredActivations = 1)
    {
        action.actionLabel = label ?? string.Empty;
        action.guideColor = guideColor;
        action.expectedSwipeDirection = expectedSwipeDirection;
        action.requiredActivations = Mathf.Max(1, requiredActivations);
        return action;
    }

    private static string AuthoredGestureInstruction(
        string minigameId,
        string stageId,
        MinigameGesture gesture)
    {
        string key = minigameId + "/" + stageId;
        return key switch
        {
            "aftershock-cover/aftershock-1" => "Deniz'in gövdesinde başla; parmağını tek hareketle aşağı indir.",
            "aftershock-cover/aftershock-2" => "Baş ve ense bölgesindeki camgöbeği köşelere bir kez dokun.",
            "aftershock-cover/aftershock-3" => "Masa ayağında halka dolana kadar basılı tut; erken bırakma.",
            "aftershock-cover/aftershock-4" => "Karanlıkta Deniz'in siluetinde başla ve aşağı kaydır.",
            "aftershock-cover/aftershock-5" => "Camdan uzakta, baş ve ense koruma alanına bir kez dokun.",
            "aftershock-cover/aftershock-6" => "Masa ayağını bul; sarsıntı bitene kadar basılı tut.",
            "aftershock-cover/aftershock-7" => "Ustalık turu: Deniz'in üzerinde aşağı kaydırarak çök.",
            "aftershock-cover/aftershock-8" => "Ustalık turu: baş ve ense bölgesine dokunarak kapan.",
            "aftershock-cover/aftershock-9" => "Ustalık turu: masa ayağında halka dolana kadar tutun.",
            "aftershock-cover/aftershock-10" => "Sarsıntı tamamen durunca Can'ın omzuna bir kez dokun.",

            "room-safety/clear-exit" => "Ayakkabı, oyuncak ve koliyi tutup SEPET etiketli hedefe taşı.",
            "room-safety/lower-objects" => "Yüksek raftaki sıradaki nesneyi alçak dolaptaki eş yerine sürükle.",
            "room-safety/shelf-anchors" => "Sol ve sağ pilot işaretine sırayla dokun; ikinci işaretten sonra anne matkapla sabitler.",
            "room-safety/wardrobe-anchors" => "Dolabın iki üst bağlantı işaretine dokun; matkabı yalnız yetişkin kullanır.",
            "room-safety/quake-test" => "Hazır olduğunda öndeki SARSINTI TESTİ konsoluna bir kez dokun.",
            "room-safety/review" => "Önce/sonra sonucunu incele; sertifikaya dokunup tamamla.",

            "emergency-bag-rush/signal-round" => "Dört sinyal aracını sırayla tutup ÇANTAYA hedefe bırak.",
            "emergency-bag-rush/blackout-proof" => "Elektrik kesilince yanan fener kontrolüne bir kez dokun.",
            "emergency-bag-rush/food-water" => "Kapalı su ve dayanıklı gıdayı ÇANTAYA hedefe taşı.",
            "emergency-bag-rush/health-docs" => "Sağlık, belge ve battaniyeyi sırayla çantaya yerleştir.",
            "emergency-bag-rush/weight-test" => "Çanta sapında başla ve tartıya oturtmak için aşağı kaydır.",
            "emergency-bag-rush/remove-console" => "Ağır konsolu tutup GERİ KOY tepsisine sürükle.",
            "emergency-bag-rush/comfort" => "Yalnız bir küçük rahatlatıcı eşyayı çantaya taşı.",
            "emergency-bag-rush/straps" => "Askı tokasında başla; parmağını dışa doğru yatay kaydır.",

            "emergency-corridor/route-cars" => "Mavi A ve sarı B rota kartlarını aynı renkli güvenli ceplere taşı.",
            "emergency-corridor/barriers" => "Batı ve doğu bariyer komut kartlarını eş giriş hedeflerine sürükle.",
            "emergency-corridor/crowd" => "YAYA rota kartını GÜVENLİ KALDIRIM hedefine sürükle.",
            "emergency-corridor/detour" => "Rota kolunda başla ve hasarlı cepheden uzağa, sağa kaydır.",
            "emergency-corridor/cross-traffic" => "Batı ve doğu kırmızı trafik kontrollerine sırayla dokun.",
            "emergency-corridor/green-wave" => "Yeşil dalga kontrolünde halka dolana kadar basılı tut.",
            "emergency-corridor/arrival" => "İtfaiye geçiş kartını açık koridor hedefine sürükle ve geçişi izle.",

            "rubble-signal/calibrate" => "Bilinen ritmi tanıtmak için kalibrasyon pedine üç kez dokun.",
            "rubble-signal/sector-scan" => "A, B ve C sensörlerinin her birinde parmağını sağa kaydır.",
            "rubble-signal/noise" => "Dalga formlarını karşılaştır; üç eşit kısa tepeye dokun.",
            "rubble-signal/primary" => "Masadaki yönlü mikrofonu B sektörüne doğru sağa çevir.",
            "rubble-signal/triangulate" => "İkinci sensörü tutup KESİŞİM etiketli noktaya sürükle.",
            "rubble-signal/mark" => "Yeşil güvenli boşluk bayrağında doğrulama halkası dolana kadar tut.",
            "rubble-signal/rescue" => "Profesyonel ekip kartını işaretli güvenli boşluğa sürükle.",

            _ => gesture switch
            {
                MinigameGesture.DragToTarget => "Parlayan nesneyi tutup etiketli hedefe taşı.",
                MinigameGesture.Hold => "Parlayan hedefte halka dolana kadar basılı tut.",
                MinigameGesture.SwipeDown => "Parlayan hedefte başla ve aşağı kaydır.",
                MinigameGesture.SwipeHorizontal => "Parlayan hedefte başla ve yatay kaydır.",
                MinigameGesture.RepeatedTap => "Parlayan hedefe gösterilen ritimde tekrar dokun.",
                _ => "Parlayan güvenli hedefe bir kez dokun."
            }
        };
    }

    private static MinigameActionDefinition Action(
        string id,
        GameObject carrier,
        bool correct,
        Transform dragTarget = null,
        string rejection = null,
        bool hideAfterAccept = false,
        GameObject acceptedVisual = null)
    {
        Collider collider = carrier.GetComponent<Collider>();
        if (collider == null)
            collider = AddBoundsCollider(carrier);
        return new MinigameActionDefinition
        {
            actionId = id,
            targetCollider = collider,
            isCorrect = correct,
            rejectionFeedback = string.IsNullOrWhiteSpace(rejection)
                ? "Bu güvenli seçim değil. Aynı adımı yeniden dene."
                : rejection,
            dragTarget = dragTarget,
            highlightRoot = CreateRim(carrier, correct ? "CyanRim" : "AmberRim", correct),
            targetHighlightRoot = dragTarget != null ? dragTarget.Find("DropGuide")?.gameObject : null,
            acceptedVisual = acceptedVisual,
            hideTargetAfterAccept = hideAfterAccept,
            authoredMoveSeconds = 0.48f,
            acceptedSfx = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_items_01.ogg"),
            rejectedSfx = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_ui_error_01.ogg")
        };
    }

    private static GameObject CreateAssetCarrier(
        Transform parent,
        string name,
        string assetPath,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler = default,
        Material overrideMaterial = null)
    {
        GameObject carrier = new GameObject(name);
        carrier.transform.SetParent(parent);
        carrier.transform.position = feetPosition;
        GameObject visual = StoryChapterBuilderCommon.InstantiateAsset(
            assetPath,
            name + "_Visual",
            carrier.transform,
            feetPosition,
            targetSize,
            euler,
            false,
            false,
            overrideMaterial);
        FitCarrierVisualToSize(visual, feetPosition, targetSize);
        AddBoundsCollider(carrier);
        return carrier;
    }

    private static GameObject CreateFurnitureCarrier(
        Transform parent,
        string name,
        string relativePath,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler = default)
    {
        GameObject carrier = new GameObject(name);
        carrier.transform.SetParent(parent);
        carrier.transform.position = feetPosition;
        GameObject visual = StoryChapterBuilderCommon.InstantiateFurniture(
            relativePath,
            name + "_Visual",
            carrier.transform,
            feetPosition,
            targetSize,
            euler,
            false);
        FitCarrierVisualToSize(visual, feetPosition, targetSize);
        AddBoundsCollider(carrier);
        return carrier;
    }

    private static GameObject CreateInteractivePrimitive(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 position,
        Vector3 scale,
        Material material,
        Vector3 euler = default)
    {
        return StoryChapterBuilderCommon.CreatePrimitive(
            name,
            type,
            position,
            scale,
            material,
            parent,
            true,
            Quaternion.Euler(euler));
    }

    private static Collider AddBoundsCollider(GameObject root)
    {
        Collider existing = root.GetComponent<Collider>();
        if (existing != null)
            return existing;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        if (renderers.Length == 0)
        {
            collider.size = Vector3.one * 0.5f;
            return collider;
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z)) * 1.08f;
        return collider;
    }

    private static GameObject CreateRim(GameObject target, string name, bool cyan)
    {
        GameObject rim = new GameObject(name);
        rim.transform.SetParent(target.transform, false);
        Bounds bounds = CombinedBounds(target);
        Vector3 localCenter = target.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        Vector3 size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z));
        Material material = cyan ? CreateMaterials().cyan : CreateMaterials().amber;
        float halfWidth = Mathf.Clamp(size.x * 0.57f, 0.28f, 1.25f);
        float halfHeight = Mathf.Clamp(size.y * 0.57f, 0.28f, 1.35f);
        float armWidth = Mathf.Clamp(size.x * 0.24f, 0.16f, 0.42f);
        float armHeight = Mathf.Clamp(size.y * 0.24f, 0.16f, 0.42f);
        float thickness = Mathf.Clamp(Mathf.Max(size.x, size.y) * 0.028f, 0.025f, 0.055f);
        float front = localCenter.z - Mathf.Max(0.06f, size.z * 0.56f);
        Vector2[] corners =
        {
            new Vector2(-halfWidth, -halfHeight),
            new Vector2(-halfWidth, halfHeight),
            new Vector2(halfWidth, -halfHeight),
            new Vector2(halfWidth, halfHeight)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            float horizontalDirection = corners[i].x < 0f ? 1f : -1f;
            float verticalDirection = corners[i].y < 0f ? 1f : -1f;
            CreateLocalPrimitive(
                rim.transform,
                "GuideCornerH_" + i,
                PrimitiveType.Cube,
                new Vector3(
                    localCenter.x + corners[i].x + horizontalDirection * armWidth * 0.5f,
                    localCenter.y + corners[i].y,
                    front),
                new Vector3(armWidth, thickness, thickness),
                material);
            CreateLocalPrimitive(
                rim.transform,
                "GuideCornerV_" + i,
                PrimitiveType.Cube,
                new Vector3(
                    localCenter.x + corners[i].x,
                    localCenter.y + corners[i].y + verticalDirection * armHeight * 0.5f,
                    front),
                new Vector3(thickness, armHeight, thickness),
                material);
        }
        rim.SetActive(false);
        return rim;
    }

    private static GameObject CreateActionVolume(
        Transform parent,
        string name,
        Vector3 worldPosition,
        Vector3 size)
    {
        GameObject action = new GameObject(name);
        action.transform.SetParent(parent);
        action.transform.position = worldPosition;
        BoxCollider collider = action.AddComponent<BoxCollider>();
        collider.size = size;
        return action;
    }

    private static Transform CreateDropTarget(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default,
        string label = "HEDEF",
        Material accent = null)
    {
        accent ??= context.materials.cyan;
        GameObject target = new GameObject(name);
        target.transform.SetParent(parent);
        target.transform.position = position;
        target.transform.rotation = Quaternion.Euler(euler);
        GameObject guide = new GameObject("DropGuide");
        guide.transform.SetParent(target.transform, false);
        guide.transform.localPosition = Vector3.zero;
        guide.transform.rotation = Quaternion.LookRotation(
            guide.transform.position - context.camera.transform.position,
            Vector3.up);
        const float halfWidth = 0.48f;
        const float halfHeight = 0.42f;
        const float arm = 0.24f;
        const float thickness = 0.035f;
        Vector2[] corners =
        {
            new Vector2(-halfWidth, -halfHeight), new Vector2(-halfWidth, halfHeight),
            new Vector2(halfWidth, -halfHeight), new Vector2(halfWidth, halfHeight)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            float horizontalDirection = corners[i].x < 0f ? 1f : -1f;
            float verticalDirection = corners[i].y < 0f ? 1f : -1f;
            CreateLocalPrimitive(guide.transform, "TargetCornerH_" + i, PrimitiveType.Cube,
                new Vector3(corners[i].x + horizontalDirection * arm * 0.5f, corners[i].y, 0f),
                new Vector3(arm, thickness, thickness), accent);
            CreateLocalPrimitive(guide.transform, "TargetCornerV_" + i, PrimitiveType.Cube,
                new Vector3(corners[i].x, corners[i].y + verticalDirection * arm * 0.5f, 0f),
                new Vector3(thickness, arm, thickness), accent);
        }
        CreateLocalPrimitive(guide.transform, "TargetLabelBack", PrimitiveType.Cube,
            new Vector3(0f, 0.63f, 0.025f), new Vector3(0.72f, 0.22f, 0.045f), context.materials.navy);
        CreateLocalPrimitive(guide.transform, "TargetLabelAccent", PrimitiveType.Cube,
            new Vector3(0f, 0.52f, -0.015f), new Vector3(0.64f, 0.035f, 0.025f), accent);
        CreateLocalWorldText(guide.transform, "TargetLabel", label,
            new Vector3(0f, 0.63f, -0.01f), Vector3.zero,
            new Vector2(0.68f, 0.2f), 1.4f, context.materials.cream.color);
        guide.SetActive(false);
        return target.transform;
    }

    private static void BuildIndoorShell(SceneContext context, Color accent, bool bedroom = false)
    {
        MaterialPack m = context.materials;
        CreateStaticPrimitive(context.environment, "Floor", PrimitiveType.Cube,
            new Vector3(0f, -0.12f, 0.6f), new Vector3(10f, 0.24f, 8.8f), m.floor);
        CreateStaticPrimitive(context.environment, "BackWall", PrimitiveType.Cube,
            new Vector3(0f, 3.6f, 4.65f), new Vector3(10f, 7.2f, 0.22f), m.wall);
        CreateStaticPrimitive(context.environment, "LeftWall", PrimitiveType.Cube,
            new Vector3(-5f, 3.6f, 0.6f), new Vector3(0.22f, 7.2f, 8.2f), m.wall);
        CreateStaticPrimitive(context.environment, "RightWall", PrimitiveType.Cube,
            new Vector3(5f, 3.6f, 0.6f), new Vector3(0.22f, 7.2f, 8.2f), m.wall);
        CreateStaticPrimitive(context.environment, "Ceiling", PrimitiveType.Cube,
            new Vector3(0f, 6.15f, 0.6f), new Vector3(10f, 0.18f, 8.2f), m.cream, false);
        Material trim = ScopedMaterial(context, "ArchitecturalTrim", new Color32(43, 61, 68, 255), 0.34f);
        Material accentTrim = ScopedMaterial(
            context,
            "ArchitecturalAccent",
            Color.Lerp(accent, new Color32(239, 230, 207, 255), 0.38f),
            0.3f);
        CreateStaticPrimitive(context.environment, "BackBaseboard", PrimitiveType.Cube,
            new Vector3(0f, 0.16f, 4.48f), new Vector3(9.72f, 0.28f, 0.15f), trim, false);
        if (bedroom)
        {
            CreateStaticPrimitive(context.environment, "BedroomLowerWallPanel", PrimitiveType.Cube,
                new Vector3(0f, 0.64f, 4.43f), new Vector3(9.66f, 0.86f, 0.065f),
                ScopedMaterial(context, "BedroomLowerWall", new Color32(225, 219, 199, 255), 0.22f), false);
        }
        CreateStaticPrimitive(context.environment, "BackChairRail", PrimitiveType.Cube,
            new Vector3(0f, 1.08f, 4.47f), new Vector3(9.72f, 0.11f, 0.13f), accentTrim, false);
        CreateStaticPrimitive(context.environment, "BackCrownMoulding", PrimitiveType.Cube,
            new Vector3(0f, 5.66f, 4.45f), new Vector3(9.72f, 0.22f, 0.17f), m.cream, false);
        for (int panel = -3; panel <= 3; panel++)
        {
            CreateStaticPrimitive(context.environment, "WallPanelTrim_" + panel, PrimitiveType.Cube,
                new Vector3(panel * 1.35f, 0.62f, 4.39f), new Vector3(0.045f, 0.72f, 0.055f),
                bedroom ? accentTrim : trim, false);
        }
        if (bedroom)
        {
            CreateStaticPrimitive(context.environment, "BedroomRug", PrimitiveType.Cube,
                new Vector3(0f, 0.015f, 0.2f), new Vector3(5.8f, 0.035f, 4f),
                ScopedMaterial(context, "BedroomRug", new Color32(211, 190, 155, 255), 0.18f), false);
            CreateStaticPrimitive(context.environment, "BedroomRugInset", PrimitiveType.Cube,
                new Vector3(0f, 0.038f, 0.2f), new Vector3(4.95f, 0.018f, 3.15f),
                ScopedMaterial(context, "BedroomRugInset", new Color32(86, 147, 153, 255), 0.14f), false);
        }
        else
        {
            CreateStaticPrimitive(context.environment, "RoomRug", PrimitiveType.Cube,
                new Vector3(0f, 0.018f, 0.55f), new Vector3(5.8f, 0.035f, 3.65f),
                ScopedMaterial(context, "RoomRug", new Color32(64, 93, 104, 255), 0.16f), false);
            CreateStaticPrimitive(context.environment, "RoomRugInset", PrimitiveType.Cube,
                new Vector3(0f, 0.042f, 0.55f), new Vector3(5.15f, 0.018f, 3.05f),
                ScopedMaterial(context, "RoomRugInset", new Color32(163, 132, 93, 255), 0.16f), false);
        }
    }

    private static GameObject CreateStaticPrimitive(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool collider = true,
        Vector3 euler = default)
    {
        GameObject go = StoryChapterBuilderCommon.CreatePrimitive(
            name, type, position, scale, material, parent, collider, Quaternion.Euler(euler));
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    private static RuntimeAnimatorController StoryCharacterController()
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            StoryAnimationLibraryBuilder.ControllerPath);
        if (controller == null)
            controller = StoryAnimationLibraryBuilder.BuildLibrary(false);
        return controller;
    }

    private static StoryChapterBuilderCommon.Characters BuildFamily(
        SceneContext context,
        Vector3 deniz,
        Vector3 can,
        Vector3 parent,
        bool includeParent = true)
    {
        return StoryChapterBuilderCommon.BuildFamily(
            context.characters,
            StoryCharacterController(),
            includeParent,
            deniz,
            can,
            parent,
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath));
    }

    private static Animation CreateMoveAnimation(
        GameObject target,
        string name,
        Vector3 from,
        Vector3 to,
        float duration,
        bool loop = false)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        AnimationClip clip = new AnimationClip { name = Sanitize(name), legacy = true, wrapMode = loop ? WrapMode.Loop : WrapMode.Once };
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AuthoredCurve(from.x, to.x, duration));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", AuthoredCurve(from.y, to.y, duration));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z", AuthoredCurve(from.z, to.z, duration));
        AssetDatabase.CreateAsset(clip, path);
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        animation.playAutomatically = false;
        return animation;
    }

    private static Animation CreateRotateAnimation(
        GameObject target,
        string name,
        Vector3 fromEuler,
        Vector3 toEuler,
        float duration)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        AnimationClip clip = new AnimationClip { name = Sanitize(name), legacy = true, wrapMode = WrapMode.Once };
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x", AuthoredCurve(fromEuler.x, toEuler.x, duration));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", AuthoredCurve(fromEuler.y, toEuler.y, duration));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", AuthoredCurve(fromEuler.z, toEuler.z, duration));
        AssetDatabase.CreateAsset(clip, path);
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        animation.playAutomatically = false;
        return animation;
    }

    private static Animation CreateRockAnimation(GameObject target, string name, float degrees, float duration)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        Vector3 euler = target.transform.localEulerAngles;
        AnimationClip clip = new AnimationClip { name = Sanitize(name), legacy = true, wrapMode = WrapMode.Once };
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.Constant(0f, duration, euler.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Constant(0f, duration, euler.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(
            new Keyframe(0f, euler.z),
            new Keyframe(duration * 0.18f, euler.z + degrees),
            new Keyframe(duration * 0.4f, euler.z - degrees * 0.8f),
            new Keyframe(duration * 0.62f, euler.z + degrees * 0.55f),
            new Keyframe(duration * 0.82f, euler.z - degrees * 0.3f),
            new Keyframe(duration, euler.z)));
        AssetDatabase.CreateAsset(clip, path);
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        animation.playAutomatically = false;
        return animation;
    }

    private static Animation CreateQuakeLoopAnimation(
        GameObject target,
        string name,
        Vector3 positionAmplitude,
        Vector3 eulerAmplitude,
        float duration)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        float cycle = Mathf.Max(0.34f, duration);
        Vector3 position = target.transform.localPosition;
        Vector3 euler = target.transform.localEulerAngles;
        float[] times =
        {
            0f,
            cycle * 0.14f,
            cycle * 0.3f,
            cycle * 0.47f,
            cycle * 0.65f,
            cycle * 0.83f,
            cycle
        };
        float[] patternA = { 0f, 1f, -0.82f, 0.62f, -1f, 0.46f, 0f };
        float[] patternB = { 0f, -0.72f, 1f, -0.9f, 0.58f, -0.36f, 0f };
        float[] patternC = { 0f, 0.55f, -1f, 0.78f, -0.52f, 1f, 0f };

        AnimationClip clip = new AnimationClip
        {
            name = Sanitize(name),
            legacy = true,
            wrapMode = WrapMode.Loop,
            frameRate = 30f
        };
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            OffsetCurve(times, patternA, position.x, positionAmplitude.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y",
            OffsetCurve(times, patternB, position.y, positionAmplitude.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            OffsetCurve(times, patternC, position.z, positionAmplitude.z));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x",
            OffsetCurve(times, patternB, euler.x, eulerAmplitude.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y",
            OffsetCurve(times, patternC, euler.y, eulerAmplitude.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z",
            OffsetCurve(times, patternA, euler.z, eulerAmplitude.z));
        AssetDatabase.CreateAsset(clip, path);
        return AttachLegacyAnimation(target, clip);
    }

    private static Animation CreateImpactFallAnimation(
        GameObject target,
        string name,
        Vector3 endWorldOffset,
        Vector3 endEulerOffset,
        float duration)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        float authoredDuration = Mathf.Max(0.55f, duration);
        Vector3 startPosition = target.transform.localPosition;
        Vector3 endOffset = target.transform.parent != null
            ? target.transform.parent.InverseTransformVector(endWorldOffset)
            : endWorldOffset;
        Vector3 endPosition = startPosition + endOffset;
        Vector3 startEuler = target.transform.localEulerAngles;
        Vector3 endEuler = startEuler + endEulerOffset;

        AnimationClip clip = new AnimationClip
        {
            name = Sanitize(name),
            legacy = true,
            wrapMode = WrapMode.ClampForever,
            frameRate = 30f
        };
        float settle = authoredDuration * 0.18f;
        float release = authoredDuration * 0.34f;
        float impact = authoredDuration * 0.82f;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", new AnimationCurve(
            new Keyframe(0f, startPosition.x),
            new Keyframe(settle, startPosition.x + endOffset.x * 0.04f),
            new Keyframe(release, startPosition.x - endOffset.x * 0.08f),
            new Keyframe(impact, endPosition.x + endOffset.x * 0.06f),
            new Keyframe(authoredDuration, endPosition.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", new AnimationCurve(
            new Keyframe(0f, startPosition.y),
            new Keyframe(settle, startPosition.y + 0.035f),
            new Keyframe(release, startPosition.y + 0.06f),
            new Keyframe(impact, endPosition.y - 0.025f),
            new Keyframe(authoredDuration, endPosition.y)));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z", new AnimationCurve(
            new Keyframe(0f, startPosition.z),
            new Keyframe(settle, startPosition.z + endOffset.z * 0.03f),
            new Keyframe(release, startPosition.z - endOffset.z * 0.06f),
            new Keyframe(impact, endPosition.z),
            new Keyframe(authoredDuration, endPosition.z)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(
            new Keyframe(0f, startEuler.x),
            new Keyframe(release, startEuler.x + endEulerOffset.x * 0.08f),
            new Keyframe(impact, startEuler.x + endEulerOffset.x * 0.97f),
            new Keyframe(authoredDuration, endEuler.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(
            new Keyframe(0f, startEuler.y),
            new Keyframe(release, startEuler.y + endEulerOffset.y * 0.1f),
            new Keyframe(impact, startEuler.y + endEulerOffset.y * 0.98f),
            new Keyframe(authoredDuration, endEuler.y)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(
            new Keyframe(0f, startEuler.z),
            new Keyframe(settle, startEuler.z - Mathf.Sign(endEulerOffset.z) * 3.5f),
            new Keyframe(release, startEuler.z + endEulerOffset.z * 0.12f),
            new Keyframe(impact, startEuler.z + endEulerOffset.z * 1.02f),
            new Keyframe(authoredDuration, endEuler.z)));
        AssetDatabase.CreateAsset(clip, path);
        return AttachLegacyAnimation(target, clip);
    }

    private static Animation CreateImpactRevealAnimation(
        GameObject target,
        string name,
        float revealDelay,
        float revealDuration)
    {
        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        float revealAt = Mathf.Max(0f, revealDelay);
        float end = revealAt + Mathf.Max(0.08f, revealDuration);
        AnimationClip clip = new AnimationClip
        {
            name = Sanitize(name),
            legacy = true,
            wrapMode = WrapMode.ClampForever,
            frameRate = 30f
        };
        AnimationCurve scale = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(revealAt, 0f),
            new Keyframe(end, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", scale);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.y", scale);
        clip.SetCurve(string.Empty, typeof(Transform), "localScale.z", scale);
        AssetDatabase.CreateAsset(clip, path);
        return AttachLegacyAnimation(target, clip);
    }

    private static AnimationCurve OffsetCurve(
        float[] times,
        float[] pattern,
        float baseValue,
        float amplitude)
    {
        Keyframe[] keys = new Keyframe[times.Length];
        for (int index = 0; index < times.Length; index++)
            keys[index] = new Keyframe(times[index], baseValue + pattern[index] * amplitude);
        return new AnimationCurve(keys);
    }

    private static Animation AttachLegacyAnimation(GameObject target, AnimationClip clip)
    {
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        animation.playAutomatically = false;
        return animation;
    }

    private static ParticleSystem CreateAftershockImpactBurst(
        string name,
        Transform parent,
        Vector3 position,
        Material material,
        int count,
        float startDelay,
        bool glass)
    {
        GameObject owner = new GameObject(name);
        owner.transform.SetParent(parent);
        owner.transform.position = position;
        ParticleSystem particles = owner.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.28f;
        main.startDelay = Mathf.Max(0f, startDelay);
        main.startLifetime = glass
            ? new ParticleSystem.MinMaxCurve(0.8f, 1.5f)
            : new ParticleSystem.MinMaxCurve(0.65f, 1.25f);
        main.startSpeed = glass
            ? new ParticleSystem.MinMaxCurve(0.85f, 1.85f)
            : new ParticleSystem.MinMaxCurve(0.5f, 1.25f);
        main.startSize = glass
            ? new ParticleSystem.MinMaxCurve(0.045f, 0.13f)
            : new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = glass ? 0.92f : 0.68f;
        main.maxParticles = Mathf.Max(64, count);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = glass
            ? new Vector3(1.7f, 1.35f, 0.12f)
            : new Vector3(0.75f, 0.2f, 0.45f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = glass ? 0.22f : 0.14f;
        noise.frequency = 0.55f;
        ParticleSystemRenderer renderer = owner.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = glass
            ? ParticleSystemRenderMode.Stretch
            : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = glass ? 0.65f : 0.2f;
        renderer.velocityScale = glass ? 0.3f : 0.08f;
        return particles;
    }

    private static GameObject CreateAftershockBookStack(
        SceneContext context,
        string name,
        Vector3 feetPosition,
        Vector3 euler,
        bool mirrored)
    {
        GameObject root = CreateAssemblyRoot(context.environment, name, feetPosition, euler);
        float direction = mirrored ? -1f : 1f;
        CreateLocalPrimitive(root.transform, "BottomBook", PrimitiveType.Cube,
            new Vector3(0f, 0.035f, 0f), new Vector3(0.48f, 0.07f, 0.34f),
            context.materials.danger, new Vector3(0f, direction * 3f, 0f));
        CreateLocalPrimitive(root.transform, "MiddleBook", PrimitiveType.Cube,
            new Vector3(direction * 0.025f, 0.102f, 0.012f), new Vector3(0.43f, 0.064f, 0.31f),
            context.materials.wood, new Vector3(0f, -direction * 5f, 0f));
        CreateLocalPrimitive(root.transform, "TopBook", PrimitiveType.Cube,
            new Vector3(-direction * 0.018f, 0.163f, -0.008f), new Vector3(0.46f, 0.058f, 0.29f),
            context.materials.amber, new Vector3(0f, direction * 4f, 0f));
        return root;
    }

    private static Animation CreateDrillWorkAnimation(
        GameObject drillRoot,
        string name,
        Vector3 awayPoint,
        Vector3 leftWorkPoint,
        Vector3 rightWorkPoint,
        float duration)
    {
        float authoredDuration = Mathf.Max(1.8f, duration);
        float[] normalizedTimes =
        {
            0f, 0.16f, 0.24f, 0.31f, 0.38f, 0.45f, 0.5f,
            0.62f, 0.69f, 0.76f, 0.83f, 0.9f, 1f
        };
        Vector3 shakeA = new Vector3(0.026f, 0.018f, -0.012f);
        Vector3 shakeB = new Vector3(-0.022f, -0.015f, 0.01f);
        Vector3[] positions =
        {
            awayPoint,
            Vector3.Lerp(awayPoint, leftWorkPoint, 0.78f),
            leftWorkPoint,
            leftWorkPoint + shakeA,
            leftWorkPoint + shakeB,
            leftWorkPoint + shakeA * 0.65f,
            leftWorkPoint,
            rightWorkPoint,
            rightWorkPoint + shakeA,
            rightWorkPoint + shakeB,
            rightWorkPoint + shakeA * 0.65f,
            rightWorkPoint,
            awayPoint
        };
        float[] roll = { 0f, 0f, 0f, 2.2f, -2f, 1.4f, 0f, 0f, 2.2f, -2f, 1.4f, 0f, 0f };

        float[] times = new float[normalizedTimes.Length];
        float[] x = new float[positions.Length];
        float[] y = new float[positions.Length];
        float[] z = new float[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            times[index] = normalizedTimes[index] * authoredDuration;
            x[index] = positions[index].x;
            y[index] = positions[index].y;
            z[index] = positions[index].z;
        }

        string path = TimelineRoot + "/" + Sanitize(name) + ".anim";
        DeleteOwnedAsset(path);
        AnimationClip clip = new AnimationClip
        {
            name = Sanitize(name),
            legacy = true,
            wrapMode = WrapMode.Once
        };
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AuthoredKeyCurve(times, x));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", AuthoredKeyCurve(times, y));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z", AuthoredKeyCurve(times, z));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", AuthoredKeyCurve(times, roll));
        AssetDatabase.CreateAsset(clip, path);

        drillRoot.transform.localPosition = awayPoint;
        Animation animation = drillRoot.GetComponent<Animation>();
        if (animation == null)
            animation = drillRoot.AddComponent<Animation>();
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        animation.playAutomatically = false;
        return animation;
    }

    private static AudioSource CreateDrillWorkAudio(SceneContext context, GameObject owner, AudioClip clip)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = context.audio.sfxGroup;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0.44f;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 0.8f;
        source.maxDistance = 13f;
        return source;
    }

    private static AnimationCurve AuthoredKeyCurve(float[] times, float[] values)
    {
        if (times == null || values == null || times.Length != values.Length || times.Length == 0)
            throw new ArgumentException("Anahtarlı animasyon eğrisi için eş uzunlukta veri gerekir.");
        Keyframe[] keys = new Keyframe[times.Length];
        for (int index = 0; index < times.Length; index++)
            keys[index] = new Keyframe(times[index], values[index]);
        AnimationCurve curve = new AnimationCurve(keys);
        for (int index = 0; index < curve.length; index++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
        }
        return curve;
    }

    private static AnimationCurve AuthoredCurve(float from, float to, float duration)
    {
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, from, duration, to);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        return curve;
    }

    private static VoiceCue LoadVoiceCue(string folder, string cueId)
    {
        VoiceManifest manifest = LoadVoiceManifest(folder);
        VoiceManifestEntry entry = manifest.entries.FirstOrDefault(candidate => candidate.id == cueId);
        if (entry == null)
            throw new InvalidOperationException($"Ses manifest cue bulunamadı: {folder}/{cueId}");
        return new VoiceCue
        {
            subtitle = entry.subtitle,
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceRoot}/{folder}/{cueId}.mp3")
        };
    }

    private static VoiceManifest LoadVoiceManifest(string folder)
    {
        if (VoiceManifestCache.TryGetValue(folder, out VoiceManifest cached))
            return cached;
        string path = $"{VoiceRoot}/{folder}/manifest.json";
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null)
            throw new InvalidOperationException("Ses manifesti bulunamadı: " + path);
        VoiceManifest manifest = JsonUtility.FromJson<VoiceManifest>(asset.text);
        if (manifest == null || manifest.entries == null || manifest.entries.Length == 0)
            throw new InvalidOperationException("Ses manifesti boş: " + path);
        VoiceManifestCache[folder] = manifest;
        return manifest;
    }

    private static Bounds CombinedBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds rendererBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                rendererBounds.Encapsulate(renderers[i].bounds);
            return rendererBounds;
        }
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            Bounds colliderBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                colliderBounds.Encapsulate(colliders[i].bounds);
            return colliderBounds;
        }
        return new Bounds(root.transform.position, Vector3.one * 0.5f);
    }

    private static void SetField(Object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " alanı bulunamadı.");
        field.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Story");
        EnsureFolder("Assets/Story/Generated");
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(TimelineRoot);
        EnsureFolder("Assets/Story/UI");
        EnsureFolder("Assets/Story/UI/Minigames");
        EnsureFolder(ThumbnailRoot);
        EnsureFolder("Assets/Story/Audio");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Geçersiz klasör yolu: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void DeleteOwnedAsset(string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static string Sanitize(string value)
    {
        char[] chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray();
        return new string(chars);
    }

    private static void BuildAftershockCover(bool showDialog)
    {
        SceneContext context = CreateContext(
            MinigameSceneCatalog.AftershockCoverPath,
            "aftershock-cover",
            "ARTÇI! ÇÖK–KAPAN–TUTUN",
            "AftershockCover",
            new Vector3(0f, 3.55f, -8.85f),
            new Vector3(0f, 1.32f, 0.45f),
            new Color32(12, 23, 42, 255),
            true);
        BuildIndoorShell(context, new Color32(72, 194, 210, 255));
        GameObject darkAftershockTint = CreateDimmingOverlay(
            context,
            "DarkAftershockScreenTint",
            new Color(0.015f, 0.035f, 0.08f, 0.48f));

        GameObject table = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Kitchen_Table_09.prefab",
            "SafeDiningTable",
            context.environment,
            new Vector3(0.1f, 0f, 0.45f),
            new Vector3(3.25f, 1.45f, 2.15f),
            new Vector3(0f, 90f, 0f),
            true);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Armchair_18.prefab",
            "LivingRoomArmchair",
            context.environment,
            new Vector3(-3.65f, 0f, 1.25f),
            new Vector3(1.65f, 1.75f, 1.6f),
            new Vector3(0f, 30f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Couch_11.prefab",
            "FamilyCouch",
            context.environment,
            new Vector3(-3.55f, 0f, 2.75f),
            new Vector3(2.55f, 1.45f, 1.25f),
            new Vector3(0f, 110f, 0f),
            false);
        GameObject sideTable = StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Coffee_Table_03.prefab",
            "LivingRoomSideTable",
            context.environment,
            new Vector3(-2.25f, 0f, 1.55f),
            new Vector3(1.2f, 0.62f, 0.82f),
            new Vector3(0f, 20f, 0f),
            false);
        GameObject livingRoomPlant = StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_15.prefab",
            "LivingRoomPlant",
            context.environment,
            new Vector3(-4.35f, 0f, 3.75f),
            new Vector3(0.9f, 1.55f, 0.9f),
            Vector3.zero,
            false);
        GameObject familyWallArt = StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_17.prefab",
            "FamilyWallArt",
            context.environment,
            new Vector3(1.55f, 1.65f, 4.28f),
            new Vector3(1.35f, 1.05f, 0.12f),
            new Vector3(0f, 180f, 0f),
            false);
        GameObject shelf = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Prop_Bookshelf_01.fbx",
            "SecuredBookcase",
            context.environment,
            new Vector3(3.78f, 0f, 3.75f),
            new Vector3(1.55f, 3.2f, 0.72f),
            new Vector3(0f, 180f, 0f),
            false);
        GameObject windowFrame = CreateStaticPrimitive(
            context.environment, "WindowFrame", PrimitiveType.Cube,
            new Vector3(-2.8f, 2.5f, 4.42f), new Vector3(2.7f, 2.5f, 0.14f),
            context.materials.cream, false);
        CreateStaticPrimitive(
            context.environment, "WindowGlass", PrimitiveType.Cube,
            new Vector3(-2.8f, 2.5f, 4.30f), new Vector3(2.28f, 2.12f, 0.055f),
            context.materials.glass, false);
        CreateStaticPrimitive(context.environment, "WindowMullionVertical", PrimitiveType.Cube,
            new Vector3(-2.8f, 2.5f, 4.2f), new Vector3(0.09f, 2.14f, 0.09f),
            context.materials.navy, false);
        CreateStaticPrimitive(context.environment, "WindowMullionHorizontal", PrimitiveType.Cube,
            new Vector3(-2.8f, 2.5f, 4.18f), new Vector3(2.3f, 0.09f, 0.09f),
            context.materials.navy, false);
        GameObject windowCrack = new GameObject("AftershockWindowCrack");
        windowCrack.transform.SetParent(context.environment, false);
        windowCrack.transform.position = new Vector3(-2.8f, 2.5f, 4.12f);
        Material crackMaterial = ScopedMaterial(
            context,
            "WindowCrack",
            new Color32(20, 31, 42, 255),
            0.18f);
        CreateLocalPrimitive(windowCrack.transform, "CrackSpine", PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.045f, 1.1f, 0.025f), crackMaterial,
            new Vector3(0f, 0f, 18f));
        CreateLocalPrimitive(windowCrack.transform, "CrackBranchA", PrimitiveType.Cube,
            new Vector3(-0.26f, 0.22f, 0f), new Vector3(0.035f, 0.62f, 0.024f), crackMaterial,
            new Vector3(0f, 0f, -48f));
        CreateLocalPrimitive(windowCrack.transform, "CrackBranchB", PrimitiveType.Cube,
            new Vector3(0.32f, -0.08f, 0f), new Vector3(0.035f, 0.72f, 0.024f), crackMaterial,
            new Vector3(0f, 0f, 62f));
        CreateLocalPrimitive(windowCrack.transform, "CrackBranchC", PrimitiveType.Cube,
            new Vector3(-0.4f, -0.34f, 0f), new Vector3(0.03f, 0.46f, 0.022f), crackMaterial,
            new Vector3(0f, 0f, 56f));
        CreateLocalPrimitive(windowCrack.transform, "CrackBranchD", PrimitiveType.Cube,
            new Vector3(0.22f, 0.42f, 0f), new Vector3(0.03f, 0.42f, 0.022f), crackMaterial,
            new Vector3(0f, 0f, -58f));
        windowCrack.transform.localScale = Vector3.zero;
        CreateStaticPrimitive(context.environment, "CurtainLeft", PrimitiveType.Cube,
            new Vector3(-4.15f, 2.45f, 4.08f), new Vector3(0.38f, 2.65f, 0.13f),
            context.materials.navy, false);
        CreateStaticPrimitive(context.environment, "CurtainRight", PrimitiveType.Cube,
            new Vector3(-1.45f, 2.45f, 4.08f), new Vector3(0.38f, 2.65f, 0.13f),
            context.materials.navy, false);
        StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Bld_House_Door_01.fbx", "ExitDoor", context.environment,
            new Vector3(3.6f, 0f, 4.34f), new Vector3(1.65f, 3.05f, 0.24f),
            new Vector3(0f, 180f, 0f), false);

        DressLivingRoom(context);
        Transform duplicateMug = context.environment.Find("FamilyMug");
        if (duplicateMug != null)
            Object.DestroyImmediate(duplicateMug.gameObject);
        Transform looseFloorCushion = context.environment.Find("SafeFloorCushion");
        if (looseFloorCushion != null)
            Object.DestroyImmediate(looseFloorCushion.gameObject);
        Transform sideTableBook = context.environment.Find("SideTableBook");
        if (sideTableBook != null)
        {
            sideTableBook.position = new Vector3(-1.98f, 0.625f, 1.58f);
            sideTableBook.rotation = Quaternion.Euler(0f, 10f, 0f);
            sideTableBook.SetParent(sideTable.transform, true);
        }
        CreateWorldLabelPlate(
            context,
            context.environment,
            "CoverProtocolSign",
            "ÇÖK • KAPAN • TUTUN",
            new Vector3(0f, 3.55f, 4.12f),
            context.materials.cyan,
            2.9f);
        StoryAuthoredPropFactory.CreateSafetyStrap(
            "BookcaseSafetyStrap", context.environment, new Vector3(3.78f, 2.82f, 4.04f),
            new Vector3(1.2f, 0.22f, 0.16f), Vector3.zero,
            ScopedMaterial(context, "BookcaseWebbing", new Color32(48, 75, 82, 255), 0.26f),
            ScopedMaterial(context, "BookcaseHardware", new Color32(132, 139, 141, 255), 0.62f),
            context.materials.amber, false);
        GameObject looseBookA = CreateAftershockBookStack(
            context, "BookcaseLooseBookA", new Vector3(3.45f, 1.585f, 3.42f),
            new Vector3(0f, 7f, 0f), false);
        GameObject looseBookB = CreateAftershockBookStack(
            context, "BookcaseLooseBookB", new Vector3(3.94f, 1.585f, 3.42f),
            new Vector3(0f, -6f, 0f), true);
        looseBookA.transform.SetParent(shelf.transform, true);
        looseBookB.transform.SetParent(shelf.transform, true);
        GameObject sideTableMug = StoryAuthoredPropFactory.CreateCeramicMug(
            "AftershockSideTableMug",
            context.environment,
            new Vector3(-2.52f, 0.625f, 1.52f),
            new Vector3(0.2f, 0.26f, 0.2f),
            new Vector3(0f, -12f, 0f),
            context.materials.cream,
            context.materials.navy,
            false);
        sideTableMug.transform.SetParent(sideTable.transform, true);

        GameObject lampPivot = new GameObject("HangingLamp");
        lampPivot.transform.SetParent(context.lightingVfx);
        lampPivot.transform.position = new Vector3(-0.25f, 4.9f, 0.3f);
        CreateInteractivePrimitive(
            lampPivot.transform, "LampStem", PrimitiveType.Cylinder,
            lampPivot.transform.position + Vector3.down * 0.75f,
            new Vector3(0.045f, 0.75f, 0.045f), context.materials.dark);
        CreateInteractivePrimitive(
            lampPivot.transform, "LampShade", PrimitiveType.Sphere,
            lampPivot.transform.position + Vector3.down * 1.48f,
            new Vector3(0.58f, 0.22f, 0.58f), context.materials.amber);
        CreateInteractivePrimitive(
            lampPivot.transform, "LampInnerShade", PrimitiveType.Sphere,
            lampPivot.transform.position + Vector3.down * 1.52f,
            new Vector3(0.42f, 0.16f, 0.42f), context.materials.cream);
        CreateInteractivePrimitive(
            lampPivot.transform, "WarmBulb", PrimitiveType.Sphere,
            lampPivot.transform.position + Vector3.down * 1.7f,
            Vector3.one * 0.17f, context.materials.amber);
        // The earlier pass only rocked the table for 2.2 seconds. In portrait gameplay most of
        // the other motion was outside the player's focal area, so the aftershock read as a table
        // animation instead of a room-scale event. These authored loops move the room shell,
        // camera hierarchy and several independently weighted props without adding runtime code.
        Animation roomShake = CreateQuakeLoopAnimation(
            context.environment.gameObject,
            "Aftershock_RoomShake",
            new Vector3(0.055f, 0.018f, 0.04f),
            new Vector3(0.22f, 0.3f, 0.42f),
            0.58f);
        Animation cameraShake = CreateQuakeLoopAnimation(
            context.cameras.gameObject,
            "Aftershock_CameraShake",
            new Vector3(0.038f, 0.028f, 0.02f),
            new Vector3(0.34f, 0.42f, 0.55f),
            0.46f);
        Animation lampShake = CreateQuakeLoopAnimation(
            lampPivot,
            "Aftershock_LampSwing",
            new Vector3(0.035f, 0.018f, 0.03f),
            new Vector3(9f, 4f, 15f),
            1.16f);
        Animation shelfShake = CreateQuakeLoopAnimation(
            shelf,
            "Aftershock_BookcaseRattle",
            new Vector3(0.035f, 0.01f, 0.025f),
            new Vector3(0.25f, 0.55f, 1.7f),
            0.52f);
        Animation tableShake = CreateQuakeLoopAnimation(
            table,
            "Aftershock_TableRattle",
            new Vector3(0.045f, 0.012f, 0.038f),
            new Vector3(0.18f, 0.45f, 0.8f),
            0.48f);
        Animation sideTableShake = CreateQuakeLoopAnimation(
            sideTable,
            "Aftershock_SideTableRattle",
            new Vector3(0.04f, 0.01f, 0.03f),
            new Vector3(0.2f, 0.4f, 1.4f),
            0.5f);

        Animation wallArtFall = CreateImpactFallAnimation(
            familyWallArt,
            "Aftershock_FamilyPhotoFall",
            new Vector3(0.34f, -1.42f, -0.2f),
            new Vector3(22f, 10f, 82f),
            1.35f);
        Animation bookAFall = CreateImpactFallAnimation(
            looseBookA,
            "Aftershock_BookA_Fall",
            new Vector3(0.52f, -1.34f, -0.5f),
            new Vector3(70f, 92f, 110f),
            1.05f);
        Animation bookBFall = CreateImpactFallAnimation(
            looseBookB,
            "Aftershock_BookB_Fall",
            new Vector3(-0.34f, -1.34f, -0.42f),
            new Vector3(64f, -105f, -86f),
            1.22f);
        Animation plantFall = CreateImpactFallAnimation(
            livingRoomPlant,
            "Aftershock_PlantTopple",
            new Vector3(-0.28f, 0f, -0.24f),
            new Vector3(0f, -10f, -68f),
            1.55f);
        Animation mugFall = CreateImpactFallAnimation(
            sideTableMug,
            "Aftershock_MugFallAndBreak",
            new Vector3(-0.48f, -0.5f, -0.3f),
            new Vector3(165f, 80f, 128f),
            0.95f);
        Animation crackReveal = CreateImpactRevealAnimation(
            windowCrack,
            "Aftershock_WindowCrackReveal",
            0.82f,
            0.18f);

        ParticleSystem dust = StoryChapterBuilderCommon.CreateDust(
            "AftershockDust",
            context.lightingVfx,
            new Vector3(0f, 2.7f, 3.8f),
            StoryChapterBuilderCommon.CreateMaterials(),
            20);
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem plasterBurst = CreateAftershockImpactBurst(
            "AftershockPlasterBurst",
            context.lightingVfx,
            new Vector3(0.6f, 4.8f, 4.05f),
            context.materials.cream,
            32,
            0.85f,
            false);
        ParticleSystem glassBurst = CreateAftershockImpactBurst(
            "AftershockGlassShards",
            context.lightingVfx,
            new Vector3(-2.8f, 2.5f, 4.0f),
            context.materials.glass,
            44,
            1.35f,
            true);
        ParticleSystem objectDebris = CreateAftershockImpactBurst(
            "AftershockObjectDebris",
            context.lightingVfx,
            new Vector3(3.7f, 1.65f, 3.35f),
            context.materials.wood,
            26,
            1.8f,
            false);
        plasterBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        glassBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        objectDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        AudioSource quakeRumble = CreateAudioSource(
            "AftershockRumble",
            context.audioRoot,
            true,
            0.52f,
            context.audio.ambienceGroup);
        quakeRumble.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Story/Generated/Audio/quake_rumble.wav");
        AudioSource woodImpact = CreateAudioSource(
            "AftershockWoodImpact",
            context.audioRoot,
            false,
            0.82f,
            context.audio.sfxGroup);
        woodImpact.clip = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_wood_hit_03.ogg");
        AudioSource glassImpact = CreateAudioSource(
            "AftershockGlassImpact",
            context.audioRoot,
            false,
            0.88f,
            context.audio.sfxGroup);
        glassImpact.clip = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_glass_06.ogg");
        AudioSource stoneImpact = CreateAudioSource(
            "AftershockStoneImpact",
            context.audioRoot,
            false,
            0.76f,
            context.audio.sfxGroup);
        stoneImpact.clip = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_stones_03.ogg");

        Animation[] continuousQuakeAnimations =
        {
            roomShake,
            cameraShake,
            lampShake,
            shelfShake,
            tableShake,
            sideTableShake
        };
        Animation[] impactAnimations =
        {
            wallArtFall,
            bookAFall,
            bookBFall,
            plantFall,
            mugFall,
            crackReveal
        };

        StoryChapterBuilderCommon.Characters family = BuildFamily(
            context,
            new Vector3(-0.72f, 0f, -0.35f),
            new Vector3(0.78f, 0f, -0.1f),
            new Vector3(-3.05f, 0f, 0.92f),
            true);
        FaceCharacterToCamera(family.deniz, context.camera, 12f);
        FaceCharacterToCamera(family.can, context.camera, -12f);
        FaceCharacterToCamera(family.parent, context.camera, -8f);
        Animator denizAnimator = family.denizAnimator;
        Animator canAnimator = family.canAnimator;

        List<MinigameStageDefinition> stages = new List<MinigameStageDefinition>();
        string[] cueIds =
        {
            "01_main_crouch", "02_main_cover", "03_main_hold",
            "04_dark_crouch", "05_dark_cover", "06_dark_hold",
            "07_master_crouch", "08_master_cover", "09_master_hold",
            "10_check_sibling"
        };
        string[] objectives =
        {
            "Deniz'in üzerinde AŞAĞI KAYDIR • ÇÖK",
            "Deniz'in başına DOKUN • KAPAN",
            "Masa ayağında BASILI TUT • TUTUN",
            "Karanlık artçı: Deniz'in üzerinde AŞAĞI KAYDIR",
            "Baş ve ense korumasını DOKUNARAK tamamla",
            "Masa ayağını artçı bitene kadar BASILI TUT",
            "İşaretsiz tur: Önce AŞAĞI KAYDIR ve ÇÖK",
            "İşaretsiz tur: Başını KAPANARAK koru",
            "İşaretsiz tur: Masa ayağına BASILI TUT",
            "Tamamen durdu • Can'ı DOKUNARAK kontrol et"
        };
        MinigameGesture[] gestures =
        {
            MinigameGesture.SwipeDown, MinigameGesture.Tap, MinigameGesture.Hold,
            MinigameGesture.SwipeDown, MinigameGesture.Tap, MinigameGesture.Hold,
            MinigameGesture.SwipeDown, MinigameGesture.Tap, MinigameGesture.Hold,
            MinigameGesture.Tap
        };

        for (int i = 0; i < cueIds.Length; i++)
        {
            GameObject stageRoot = new GameObject($"Stage_{i + 1:00}_{Sanitize(cueIds[i])}");
            stageRoot.transform.SetParent(context.gameplay);
            stageRoot.SetActive(i == 0);
            Vector3 correctPosition = i == 2 || i == 5 || i == 8
                ? new Vector3(0.18f, 0.72f, 0.15f)
                : i == 9
                    ? family.can.transform.position + Vector3.up * 0.85f
                    : family.deniz.transform.position + Vector3.up * (i % 3 == 1 ? 1.15f : 0.78f);
            Vector3 correctSize = i == 2 || i == 5 || i == 8
                ? new Vector3(1.45f, 1.35f, 1.2f)
                : new Vector3(1.05f, 1.65f, 0.9f);
            GameObject correctTarget = CreateActionVolume(
                stageRoot.transform, "RealObjectTarget", correctPosition, correctSize);
            GameObject windowTarget = i % 3 == 0
                ? CreateActionVolume(stageRoot.transform, "UnsafeWindowChoice",
                    new Vector3(-2.75f, 2.25f, 4.05f), new Vector3(2.4f, 2.4f, 0.8f))
                : null;
            GameObject doorTarget = i % 3 == 0
                ? CreateActionVolume(stageRoot.transform, "UnsafeDoorChoice",
                    new Vector3(3.6f, 1.45f, 4.05f), new Vector3(1.7f, 2.9f, 0.8f))
                : null;
            string actionLabel = i == 9
                ? "CAN'IN OMZUNA DOKUN"
                : i % 3 == 0
                    ? "DENİZ'İN GÖVDESİNDEN AŞAĞI KAYDIR"
                    : i % 3 == 1
                        ? "BAŞ VE ENSE KORUMASINA DOKUN"
                        : "MASA AYAĞINI HALKA DOLANA KADAR TUT";
            MinigameActionDefinition[] actions = i == 9
                ? new[]
                {
                    PolishAction(Action("check-can", correctTarget, true), actionLabel, context.materials.cyan.color)
                }
                : i % 3 == 0
                    ? new[]
                    {
                        PolishAction(Action("safe-motion", correctTarget, true), actionLabel, context.materials.cyan.color),
                        Action("window", windowTarget, false, null,
                            "Pencereye yönelme; camdan uzak kal ve yalnız mevcut hareketi yeniden dene."),
                        Action("door", doorTarget, false, null,
                            "Kapıya koşma; sarsıntı sürerken bulunduğun güvenli örtüde kal.")
                    }
                    : new[]
                {
                    PolishAction(Action("safe-motion", correctTarget, true), actionLabel, context.materials.cyan.color)
                };
            MinigameStageDefinition stage = Stage(
                context,
                "aftershock-" + (i + 1),
                cueIds[i],
                objectives[i],
                gestures[i],
                stageRoot,
                actions,
                1,
                i >= 6 ? 15f : 10f,
                22f,
                1.5f);
            stage.showGuidesOnEnter = !(i >= 6 && i <= 8);
            Vector3 stageCameraPosition = (i % 3) switch
            {
                0 => new Vector3(0f, 3.05f, -7.15f),
                1 => new Vector3(-0.35f, 2.65f, -5.9f),
                _ => new Vector3(0.9f, 2.15f, -5.35f)
            };
            Vector3 stageCameraTarget = (i % 3) switch
            {
                0 => new Vector3(0.3f, 0.95f, 0.15f),
                1 => new Vector3(-0.25f, 1.05f, 0.1f),
                _ => new Vector3(0.05f, 0.78f, 0.3f)
            };
            if (i == 9)
            {
                stageCameraPosition = new Vector3(0f, 2.75f, -6.1f);
                stageCameraTarget = new Vector3(0f, 0.95f, 0.1f);
            }
            bool denizTalks = stage.subtitle.StartsWith("Deniz:", StringComparison.Ordinal);
            bool canTalks = stage.subtitle.StartsWith("Can:", StringComparison.Ordinal);
            PresentStage(
                context,
                stage,
                "Aftershock_" + (i + 1),
                stageCameraPosition,
                stageCameraTarget,
                i == 9 ? 45f : i % 3 == 0 ? 60f : i % 3 == 2 ? 40f : 41f,
                CharacterCue(family.deniz, 12f, denizTalks),
                CharacterCue(family.can, -12f, canTalks));
            string trigger = i % 3 == 0 && i < 9
                ? "StoryCrouch"
                : i % 3 == 1 && i < 9 ? "StoryCover" : i < 9 ? "StoryHold" : string.Empty;
            if (!string.IsNullOrEmpty(trigger))
            {
                stage.successAnimatorTriggers = new[]
                {
                    new MinigameAnimatorTrigger { animator = denizAnimator, triggerName = trigger },
                    new MinigameAnimatorTrigger { animator = canAnimator, triggerName = trigger }
                };
            }
            if (i < 9)
                stage.playOnEnter = continuousQuakeAnimations;
            if (i == 0 || i == 3 || i == 6)
            {
                stage.playOnEnter = continuousQuakeAnimations.Concat(impactAnimations).ToArray();
                UnityEventTools.AddBoolPersistentListener(stage.onEnter, dust.Play, true);
                UnityEventTools.AddBoolPersistentListener(stage.onEnter, plasterBurst.Play, true);
                UnityEventTools.AddBoolPersistentListener(stage.onEnter, glassBurst.Play, true);
                UnityEventTools.AddBoolPersistentListener(stage.onEnter, objectDebris.Play, true);
                UnityEventTools.AddPersistentListener(stage.onEnter, quakeRumble.Play);
                UnityEventTools.AddFloatPersistentListener(stage.onEnter, woodImpact.PlayDelayed, 0.72f);
                UnityEventTools.AddFloatPersistentListener(stage.onEnter, glassImpact.PlayDelayed, 1.28f);
                UnityEventTools.AddFloatPersistentListener(stage.onEnter, stoneImpact.PlayDelayed, 1.72f);
            }
            stages.Add(stage);
        }

        GameObject warmPractical = context.lightingVfx.Find("Amber Practical Light")?.gameObject;
        GameObject keyLight = context.lightingVfx.Find("Key Directional Light")?.gameObject;
        if (warmPractical != null)
        {
            UnityEventTools.AddBoolPersistentListener(stages[3].onEnter, warmPractical.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(stages[6].onEnter, warmPractical.SetActive, true);
        }
        if (keyLight != null)
        {
            UnityEventTools.AddBoolPersistentListener(stages[3].onEnter, keyLight.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(stages[6].onEnter, keyLight.SetActive, true);
        }
        UnityEventTools.AddBoolPersistentListener(stages[3].onEnter, darkAftershockTint.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(stages[6].onEnter, darkAftershockTint.SetActive, false);
        foreach (Animation animation in continuousQuakeAnimations)
            UnityEventTools.AddPersistentListener(stages[9].onEnter, animation.Stop);
        UnityEventTools.AddPersistentListener(stages[9].onEnter, quakeRumble.Stop);
        UnityEventTools.AddPersistentListener(stages[9].onEnter, woodImpact.Stop);
        UnityEventTools.AddPersistentListener(stages[9].onEnter, glassImpact.Stop);
        UnityEventTools.AddPersistentListener(stages[9].onEnter, stoneImpact.Stop);
        UnityEventTools.AddBoolPersistentListener(stages[9].onEnter, dust.Stop, true);
        UnityEventTools.AddBoolPersistentListener(stages[9].onEnter, plasterBurst.Stop, true);
        UnityEventTools.AddBoolPersistentListener(stages[9].onEnter, glassBurst.Stop, true);
        UnityEventTools.AddBoolPersistentListener(stages[9].onEnter, objectDebris.Stop, true);
        stages[9].successAnimatorTriggers = new[]
        {
            new MinigameAnimatorTrigger { animator = denizAnimator, triggerName = "StoryInteract" },
            new MinigameAnimatorTrigger { animator = canAnimator, triggerName = "StoryCall" }
        };

        FinishScene(context, stages.ToArray(), showDialog);
    }

    private static void BuildRoomSafety(bool showDialog)
    {
        SceneContext context = CreateContext(
            MinigameSceneCatalog.RoomSafetyPath,
            "room-safety",
            "GÜVENLİ ODA CHALLENGE",
            "RoomSafety",
            new Vector3(0f, 3.9f, -9.35f),
            new Vector3(0f, 1.38f, 0.85f),
            new Color32(111, 157, 178, 255),
            false);
        BuildIndoorShell(context, new Color32(244, 173, 65, 255), true);
        SetField(context.session, "stageTransitionSeconds", 2.7f);
        AudioClip drillClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Script/freesound_community-power-drill-90294.mp3");
        if (drillClip == null)
            throw new InvalidOperationException("Bölüm 2 matkap sesi bulunamadı.");

        StoryChapterBuilderCommon.Characters family = BuildFamily(
            context,
            new Vector3(-2.65f, 0f, -0.52f),
            new Vector3(-3.55f, 0f, 1.05f),
            new Vector3(3.15f, 0f, -0.05f),
            true);
        FaceCharacterToCamera(family.deniz, context.camera, 18f);
        FaceCharacterToCamera(family.can, context.camera, -14f);
        FaceCharacterToCamera(family.parent, context.camera, -20f);

        CreateBedroomExitDoor(context, context.environment, new Vector3(0f, 0f, 4.33f));
        CreateFurnitureCarrier(
            context.environment,
            "ChildBed",
            "Furniture/Bed_07.prefab",
            new Vector3(-3.55f, 0f, 0.75f),
            new Vector3(1.45f, 0.92f, 2.45f),
            new Vector3(0f, 2f, 0f));
        CreateFurnitureCarrier(
            context.environment,
            "ChildDesk",
            "Furniture/Work_Table_06.prefab",
            new Vector3(3.72f, 0f, 0.78f),
            new Vector3(1.62f, 0.86f, 0.86f),
            new Vector3(0f, 88f, 0f));
        CreateFurnitureCarrier(
            context.environment,
            "BedroomWallArt",
            "Decorations/Picture_08.prefab",
            new Vector3(0f, 3.18f, 4.25f),
            new Vector3(1.02f, 0.78f, 0.1f),
            new Vector3(0f, 180f, 0f));
        DressBedroom(context);

        List<MinigameStageDefinition> stages = new List<MinigameStageDefinition>();

        GameObject clearRoot = NewStageRoot(context, "Stage_01_ClearExit", true);
        CreateBedroomRunner(
            context,
            clearRoot.transform,
            new Vector3(0f, 0.052f, 1.55f),
            new Vector3(1.34f, 0.04f, 4.35f));
        GameObject exitStorageBasket = CreateStorageBasket(context, clearRoot.transform, "ExitStorageBasket",
            new Vector3(1.28f, 0f, 3.2f), new Vector3(0.72f, 0.42f, 0.58f));
        exitStorageBasket.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
        Transform exitBasket = CreateDropTarget(
            context, clearRoot.transform, "ExitClearBasket", new Vector3(1.28f, 0.54f, 3.2f), default, "SEPET");
        GameObject shoe = StoryAuthoredPropFactory.CreateShoePair(
            "ExitShoes", clearRoot.transform, new Vector3(-0.16f, 0.08f, 0.15f),
            new Vector3(0.58f, 0.25f, 0.64f), new Vector3(0f, 172f, 0f),
            ScopedMaterial(context, "ShoeUpper", new Color32(33, 91, 105, 255), 0.32f),
            context.materials.cream, true);
        GameObject toy = CreateAssetCarrier(clearRoot.transform, "ToyCar",
            FurnitureRoot + "/Decorations/Toy_02.prefab",
            new Vector3(0.22f, 0.08f, 1.35f), new Vector3(0.58f, 0.32f, 0.42f),
            new Vector3(0f, -12f, 0f));
        GameObject box = StoryAuthoredPropFactory.CreateParcel(
            "ExitCardboardBox", clearRoot.transform, new Vector3(-0.12f, 0.08f, 2.45f),
            new Vector3(0.62f, 0.52f, 0.54f), new Vector3(0f, 8f, 0f),
            ScopedMaterial(context, "ParcelCardboard", new Color32(157, 111, 70, 255), 0.2f),
            context.materials.amber, context.materials.cream, true);
        MinigameStageDefinition clearStage = Stage(context, "clear-exit", "01_clear_exit",
            "SIRAYLA TEMİZLE • AYAKKABI → OYUNCAK → KOLİ",
            MinigameGesture.DragToTarget, clearRoot,
            new[]
            {
                PolishAction(Action("shoes", shoe, true, exitBasket, null, true), "1/3 • AYAKKABI → SEPET", context.materials.cyan.color),
                PolishAction(Action("toy", toy, true, exitBasket, null, true), "2/3 • OYUNCAK → SEPET", context.materials.cyan.color),
                PolishAction(Action("box", box, true, exitBasket, null, true), "3/3 • KOLİ → SEPET", context.materials.cyan.color)
            }, 3, 10f, 32f, 1.35f,
            "Yalnız camgöbeği çerçeveli nesneyi sepete sürükle; sıra kendiliğinden ilerler.");
        PresentStage(context, clearStage, "Room_ClearExit",
            new Vector3(0.08f, 3.45f, -7.55f), new Vector3(0.05f, 0.58f, 1.62f), 43f,
            CharacterCueAt(family.deniz, 18f, false, new Vector3(-1.35f, 0f, 1.52f)),
            CharacterCueAt(family.parent, -20f, true, new Vector3(1.78f, 0f, 2.15f)));
        stages.Add(clearStage);

        GameObject lowerRoot = NewStageRoot(context, "Stage_02_LowerObjects", false);
        CreateLowSafetyCabinet(
            context, lowerRoot.transform, "LowSafeCabinet", new Vector3(2.35f, 0f, 3.72f), 2.75f);
        Transform bookShelfTarget = CreateDropTarget(
            context, lowerRoot.transform, "LowSafeShelfTargetBook", new Vector3(1.5f, 1.0f, 3.46f), default, "KİTAP");
        Transform vaseShelfTarget = CreateDropTarget(
            context, lowerRoot.transform, "LowSafeShelfTargetVase", new Vector3(2.35f, 1.0f, 3.46f), default, "VAZO");
        Transform frameShelfTarget = CreateDropTarget(
            context, lowerRoot.transform, "LowSafeShelfTargetFrame", new Vector3(3.2f, 1.0f, 3.46f), default, "ÇERÇEVE");
        CreateLocalPrimitive(lowerRoot.transform, "HighObjectShelf", PrimitiveType.Cube,
            new Vector3(2.35f, 2.38f, 4.14f), new Vector3(2.78f, 0.13f, 0.58f), context.materials.wood);
        CreateLocalPrimitive(lowerRoot.transform, "HighShelfBracketLeft", PrimitiveType.Cube,
            new Vector3(1.52f, 2.11f, 4.27f), new Vector3(0.11f, 0.5f, 0.11f), context.materials.dark);
        CreateLocalPrimitive(lowerRoot.transform, "HighShelfBracketRight", PrimitiveType.Cube,
            new Vector3(3.18f, 2.11f, 4.27f), new Vector3(0.11f, 0.5f, 0.11f), context.materials.dark);
        GameObject book = CreateAssetCarrier(lowerRoot.transform, "HighBook",
            FurnitureRoot + "/Decorations/Book_08.prefab", new Vector3(1.5f, 2.46f, 3.84f),
            new Vector3(0.52f, 0.18f, 0.48f), new Vector3(0f, -8f, 0f));
        GameObject vase = CreateCeramicVase(
            context, lowerRoot.transform, "HighVase", new Vector3(2.35f, 2.46f, 3.84f));
        vase.transform.localScale = Vector3.one * 0.62f;
        GameObject frame = StoryAuthoredPropFactory.CreatePictureFrame(
            "HighFrame", lowerRoot.transform, new Vector3(3.2f, 2.46f, 3.84f),
            new Vector3(0.52f, 0.62f, 0.11f), new Vector3(0f, -4f, 0f),
            context.materials.wood, context.materials.cream, context.materials.amber, true);
        MinigameStageDefinition lowerStage = Stage(context, "lower-objects", "02_lower_objects",
            "Kitap, vazo ve çerçeveyi alt güvenli rafa SÜRÜKLE",
            MinigameGesture.DragToTarget, lowerRoot,
            new[]
            {
                PolishAction(Action("book", book, true, bookShelfTarget), "1/3 • KİTABI SOL GÜVENLİ YERE İNDİR", context.materials.cyan.color),
                PolishAction(Action("vase", vase, true, vaseShelfTarget), "2/3 • VAZOYU ORTA GÜVENLİ YERE İNDİR", context.materials.cyan.color),
                PolishAction(Action("frame", frame, true, frameShelfTarget), "3/3 • ÇERÇEVEYİ SAĞ GÜVENLİ YERE İNDİR", context.materials.cyan.color)
            }, 3, 11f, 32f);
        PresentStage(context, lowerStage, "Room_LowerObjects",
            new Vector3(2.35f, 3.25f, -5.95f), new Vector3(2.35f, 1.48f, 3.62f), 39f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(0.95f, 0f, 2.18f)));
        stages.Add(lowerStage);

        GameObject shelfAnchorRoot = NewStageRoot(context, "Stage_03_ShelfAnchors", false);
        CreateAssetCarrier(
            shelfAnchorRoot.transform,
            "ShelfAnchoringFurniture",
            "Assets/Story/Prefabs/Shelf_Stable.prefab",
            new Vector3(2.65f, 0f, 3.75f),
            new Vector3(1.5f, 2.25f, 0.72f),
            new Vector3(0f, 180f, 0f));
        CreateCompactWorkshopCart(
            context, shelfAnchorRoot.transform, "ShelfDrillCart",
            new Vector3(0.35f, 0f, 1.35f), new Vector3(1.35f, 0.74f, 0.76f), context.materials.cyan);
        GameObject shelfAnchorA = CreateDrillPilotMarker(
            context, shelfAnchorRoot.transform, "ShelfPilotLeft",
            new Vector3(2.28f, 2.5f, 3.97f), context.materials.cyan);
        GameObject shelfAnchorB = CreateDrillPilotMarker(
            context, shelfAnchorRoot.transform, "ShelfPilotRight",
            new Vector3(3.02f, 2.5f, 3.97f), context.materials.cyan);
        GameObject shelfInstalledHardware = CreateDrilledAnchorHardware(
            context, shelfAnchorRoot.transform, "ShelfInstalledDrillHardware",
            new Vector3(2.65f, 2.5f, 3.99f), 1.22f, context.materials.safe);
        shelfInstalledHardware.SetActive(false);

        Vector3 shelfDrillAway = new Vector3(0.35f, 0.8f, 1.35f);
        GameObject shelfDrill = CreateAssetCarrier(
            shelfAnchorRoot.transform, "ShelfAdultDrill",
            "Assets/Sprites/Drill/Drill_01.obj", shelfDrillAway,
            new Vector3(0.72f, 0.38f, 0.28f), new Vector3(0f, 90f, -4f));
        Collider shelfDrillCollider = shelfDrill.GetComponent<Collider>();
        if (shelfDrillCollider != null)
            shelfDrillCollider.enabled = false;
        Animation shelfDrillAnimation = CreateDrillWorkAnimation(
            shelfDrill, "RoomSafety_ShelfAdultDrill", shelfDrillAway,
            new Vector3(2.28f, 2.29f, 3.64f), new Vector3(3.02f, 2.29f, 3.64f), 2.5f);
        AudioSource shelfDrillAudio = CreateDrillWorkAudio(context, shelfDrill, drillClip);
        ParticleSystem shelfDrillDust = StoryChapterBuilderCommon.CreateDust(
            "RoomSafety_ShelfDrillDust", shelfAnchorRoot.transform,
            new Vector3(2.65f, 2.52f, 3.84f), StoryChapterBuilderCommon.CreateMaterials(), 14);
        shelfDrillDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        MinigameStageDefinition shelfAnchorStage = Stage(context, "shelf-anchors", "03_shelf_anchor",
            "İki duvar bağlantısını DOKUNARAK işaretle • Matkabı yetişkin kullansın",
            MinigameGesture.Tap, shelfAnchorRoot,
            new[]
            {
                PolishAction(Action("shelf-left", shelfAnchorA, true),
                    "SOL PİLOT NOKTASINI İŞARETLE", context.materials.cyan.color),
                PolishAction(Action("shelf-right", shelfAnchorB, true, acceptedVisual: shelfInstalledHardware),
                    "SAĞ PİLOT NOKTASINI İŞARETLE", context.materials.cyan.color)
            }, 2, 10f, 30f);
        shelfAnchorStage.playOnSuccess = new[] { shelfDrillAnimation };
        shelfAnchorStage.successAnimatorTriggers = new[]
        {
            new MinigameAnimatorTrigger { animator = family.parentAnimator, triggerName = "StoryWork" }
        };
        UnityEventTools.AddPersistentListener(shelfAnchorStage.onSuccess, shelfDrillAudio.Play);
        UnityEventTools.AddPersistentListener(shelfAnchorStage.onSuccess, shelfDrillDust.Play);
        PresentStage(context, shelfAnchorStage, "Room_ShelfAnchors",
            new Vector3(1.35f, 3.2f, -5.5f), new Vector3(1.58f, 1.42f, 3.0f), 45f,
            CharacterCueAt(family.parent, -20f, true, new Vector3(0.1f, 0f, 2.05f)));
        stages.Add(shelfAnchorStage);

        GameObject wardrobeAnchorRoot = NewStageRoot(context, "Stage_04_WardrobeAnchors", false);
        CreateAssetCarrier(
            wardrobeAnchorRoot.transform,
            "SecuredWardrobe",
            "Assets/Story/Prefabs/Wardrobe_Secured.prefab",
            new Vector3(-2.65f, 0f, 3.75f),
            new Vector3(1.55f, 2.3f, 0.82f),
            new Vector3(0f, 180f, 0f));
        CreateCompactWorkshopCart(
            context, wardrobeAnchorRoot.transform, "WardrobeDrillCart",
            new Vector3(-0.35f, 0f, 1.35f), new Vector3(1.35f, 0.74f, 0.76f), context.materials.amber);
        GameObject wardrobeAnchorA = CreateDrillPilotMarker(
            context, wardrobeAnchorRoot.transform, "WardrobePilotLeft",
            new Vector3(-3.02f, 2.53f, 3.97f), context.materials.amber);
        GameObject wardrobeAnchorB = CreateDrillPilotMarker(
            context, wardrobeAnchorRoot.transform, "WardrobePilotRight",
            new Vector3(-2.28f, 2.53f, 3.97f), context.materials.amber);
        GameObject wardrobeInstalledHardware = CreateDrilledAnchorHardware(
            context, wardrobeAnchorRoot.transform, "WardrobeInstalledDrillHardware",
            new Vector3(-2.65f, 2.53f, 3.99f), 1.24f, context.materials.safe);
        wardrobeInstalledHardware.SetActive(false);

        Vector3 wardrobeDrillAway = new Vector3(-0.35f, 0.8f, 1.35f);
        GameObject wardrobeDrill = CreateAssetCarrier(
            wardrobeAnchorRoot.transform, "WardrobeAdultDrill",
            "Assets/Sprites/Drill/Drill_01.obj", wardrobeDrillAway,
            new Vector3(0.72f, 0.38f, 0.28f), new Vector3(0f, 90f, -4f));
        Collider wardrobeDrillCollider = wardrobeDrill.GetComponent<Collider>();
        if (wardrobeDrillCollider != null)
            wardrobeDrillCollider.enabled = false;
        Animation wardrobeDrillAnimation = CreateDrillWorkAnimation(
            wardrobeDrill, "RoomSafety_WardrobeAdultDrill", wardrobeDrillAway,
            new Vector3(-3.02f, 2.32f, 3.64f), new Vector3(-2.28f, 2.32f, 3.64f), 2.5f);
        AudioSource wardrobeDrillAudio = CreateDrillWorkAudio(context, wardrobeDrill, drillClip);
        ParticleSystem wardrobeDrillDust = StoryChapterBuilderCommon.CreateDust(
            "RoomSafety_WardrobeDrillDust", wardrobeAnchorRoot.transform,
            new Vector3(-2.65f, 2.55f, 3.84f), StoryChapterBuilderCommon.CreateMaterials(), 14);
        wardrobeDrillDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        MinigameStageDefinition wardrobeAnchorStage = Stage(context, "wardrobe-anchors", "04_wardrobe_anchor",
            "Dolabın iki üst bağlantısını DOKUNARAK işaretle • Anne matkapla sabitlesin",
            MinigameGesture.Tap, wardrobeAnchorRoot,
            new[]
            {
                PolishAction(Action("wardrobe-left", wardrobeAnchorA, true),
                    "SOL ÜST BAĞLANTIYI İŞARETLE", context.materials.amber.color),
                PolishAction(Action("wardrobe-right", wardrobeAnchorB, true, acceptedVisual: wardrobeInstalledHardware),
                    "SAĞ ÜST BAĞLANTIYI İŞARETLE", context.materials.amber.color)
            }, 2, 10f, 30f);
        wardrobeAnchorStage.playOnSuccess = new[] { wardrobeDrillAnimation };
        wardrobeAnchorStage.successAnimatorTriggers = new[]
        {
            new MinigameAnimatorTrigger { animator = family.parentAnimator, triggerName = "StoryWork" }
        };
        UnityEventTools.AddPersistentListener(wardrobeAnchorStage.onSuccess, wardrobeDrillAudio.Play);
        UnityEventTools.AddPersistentListener(wardrobeAnchorStage.onSuccess, wardrobeDrillDust.Play);
        PresentStage(context, wardrobeAnchorStage, "Room_WardrobeAnchors",
            new Vector3(-1.35f, 3.2f, -5.5f), new Vector3(-1.55f, 1.44f, 3.0f), 47f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(-0.7f, 0f, 1.7f)),
            CharacterCueAt(family.parent, -20f, false, new Vector3(0.05f, 0f, 2.04f)));
        stages.Add(wardrobeAnchorStage);

        GameObject testRoot = NewStageRoot(context, "Stage_05_QuakeTest", false);
        GameObject beforeShelf = CreateAssetCarrier(
            testRoot.transform,
            "UnsecuredShelfBefore",
            TownRoot + "/SM_Prop_Bookshelf_01.fbx",
            new Vector3(-2.35f, 0f, 3.75f),
            new Vector3(1.45f, 2.3f, 0.72f),
            new Vector3(0f, 180f, 3.5f));
        GameObject shelf = CreateAssetCarrier(
            testRoot.transform,
            "WallShelfAfter",
            "Assets/Story/Prefabs/Shelf_Stable.prefab",
            new Vector3(2.35f, 0f, 3.75f),
            new Vector3(1.45f, 2.3f, 0.72f),
            new Vector3(0f, 180f, 0f));
        CreateDrilledAnchorHardware(
            context, testRoot.transform, "ShelfInstalledDrillHardware",
            new Vector3(2.35f, 2.08f, 4.02f), 1.18f, context.materials.safe);
        CreateLocalPrimitive(testRoot.transform, "BeforeCrossA", PrimitiveType.Cube,
            new Vector3(-2.35f, 2.62f, 3.34f), new Vector3(0.12f, 0.72f, 0.06f), context.materials.danger,
            new Vector3(0f, 0f, 45f));
        CreateLocalPrimitive(testRoot.transform, "BeforeCrossB", PrimitiveType.Cube,
            new Vector3(-2.35f, 2.62f, 3.34f), new Vector3(0.12f, 0.72f, 0.06f), context.materials.danger,
            new Vector3(0f, 0f, -45f));
        CreateLocalPrimitive(testRoot.transform, "AfterCheckShort", PrimitiveType.Cube,
            new Vector3(2.15f, 2.58f, 3.34f), new Vector3(0.12f, 0.42f, 0.06f), context.materials.safe,
            new Vector3(0f, 0f, -42f));
        CreateLocalPrimitive(testRoot.transform, "AfterCheckLong", PrimitiveType.Cube,
            new Vector3(2.48f, 2.69f, 3.34f), new Vector3(0.12f, 0.78f, 0.06f), context.materials.safe,
            new Vector3(0f, 0f, 43f));
        CreateLocalPrimitive(testRoot.transform, "TestConsolePedestal", PrimitiveType.Cube,
            new Vector3(0f, 0.62f, 1.72f), new Vector3(0.18f, 1.1f, 0.18f), context.materials.dark);
        CreateLocalPrimitive(testRoot.transform, "TestConsolePedestalBase", PrimitiveType.Cube,
            new Vector3(0f, 0.09f, 1.72f), new Vector3(0.72f, 0.14f, 0.52f), context.materials.dark);
        GameObject testButton = CreateLayeredWallConsole(
            context, testRoot.transform, "QuakeTestConsole",
            new Vector3(0f, 1.32f, 1.65f), Vector3.zero,
            context.materials.amber, "SARSINTI TESTİ");
        Animation beforeRock = CreateRockAnimation(beforeShelf, "RoomSafety_BeforeShelfFall", 16f, 2.25f);
        Animation afterRock = CreateRockAnimation(shelf, "RoomSafety_SecuredShelfTest", 1.8f, 2.25f);
        MinigameStageDefinition testStage = Stage(context, "quake-test", "07_quake_test",
            "Test konsoluna DOKUN • ÖNCE/SONRA",
            MinigameGesture.Tap, testRoot,
            new[]
            {
                PolishAction(Action("test", testButton, true), "SARSINTI TESTİNİ BAŞLAT", context.materials.amber.color)
            }, 1, 11f, 25f);
        testStage.playOnSuccess = new[] { beforeRock, afterRock };
        PresentStage(context, testStage, "Room_QuakeTest",
            new Vector3(0f, 3.55f, -7.8f), new Vector3(0f, 1.42f, 3.2f), 43f,
            CharacterCueAt(family.can, -14f, true, new Vector3(-1.08f, 0f, 1.15f)),
            CharacterCueAt(family.parent, -20f, false, new Vector3(1.12f, 0f, 1.15f)));
        stages.Add(testStage);

        GameObject reviewRoot = NewStageRoot(context, "Stage_06_Review", false);
        CreateLocalPrimitive(reviewRoot.transform, "CertificateEaselLeft", PrimitiveType.Cube,
            new Vector3(1.47f, 0.92f, 3.78f), new Vector3(0.11f, 1.72f, 0.12f), context.materials.wood,
            new Vector3(0f, 0f, -8f));
        CreateLocalPrimitive(reviewRoot.transform, "CertificateEaselRight", PrimitiveType.Cube,
            new Vector3(2.53f, 0.92f, 3.78f), new Vector3(0.11f, 1.72f, 0.12f), context.materials.wood,
            new Vector3(0f, 0f, 8f));
        CreateLocalPrimitive(reviewRoot.transform, "CertificateEaselShelf", PrimitiveType.Cube,
            new Vector3(2f, 1.36f, 3.68f), new Vector3(1.38f, 0.12f, 0.32f), context.materials.wood);
        CreateLocalPrimitive(reviewRoot.transform, "CertificateEaselFoot", PrimitiveType.Cube,
            new Vector3(2f, 0.08f, 3.86f), new Vector3(1.48f, 0.12f, 0.58f), context.materials.dark);
        GameObject review = CreateSafetyCertificate(
            context, reviewRoot.transform, "SafetyReviewCertificate",
            new Vector3(2f, 1.82f, 3.58f), Vector3.zero);
        MinigameStageDefinition reviewStage = Stage(context, "review", "08_review",
            "Güvenlik sertifikasına DOKUN ve odayı tamamla",
            MinigameGesture.Tap, reviewRoot,
            new[]
            {
                PolishAction(Action("review", review, true), "GÜVENLİ ODA KONTROLÜNÜ ONAYLA", context.materials.safe.color)
            }, 1, 14f, 24f);
        PresentStage(context, reviewStage, "Room_Review",
            new Vector3(1f, 3.05f, -6.85f), new Vector3(1f, 1.62f, 3.25f), 40f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(-0.2f, 0f, 1.52f)));
        stages.Add(reviewStage);

        FinishScene(context, stages.ToArray(), showDialog);
    }

    private static GameObject NewStageRoot(SceneContext context, string name, bool active)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(context.gameplay);
        root.SetActive(active);
        return root;
    }

    private static void BuildEmergencyBagRush(bool showDialog)
    {
        SceneContext context = CreateContext(
            MinigameSceneCatalog.EmergencyBagRushPath,
            "emergency-bag-rush",
            "ÇANTA 30",
            "EmergencyBagRush",
            new Vector3(0f, 4.0f, -9.65f),
            new Vector3(0f, 1.25f, 0.7f),
            new Color32(74, 103, 114, 255),
            false);
        BuildIndoorShell(context, new Color32(247, 169, 61, 255));
        StoryChapterBuilderCommon.Characters family = BuildFamily(
            context,
            new Vector3(-2.75f, 0f, 0.2f),
            new Vector3(-2.15f, 0f, 1.55f),
            new Vector3(2.7f, 0f, 2.2f),
            true);
        FaceCharacterToCamera(family.deniz, context.camera, 18f);
        FaceCharacterToCamera(family.can, context.camera, -12f);
        FaceCharacterToCamera(family.parent, context.camera, -20f);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Fridge_01.prefab",
            "PreparationFridge",
            context.environment,
            new Vector3(-4.15f, 0f, 3.75f),
            new Vector3(1.55f, 2.8f, 1.15f),
            new Vector3(0f, 180f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_06.prefab",
            "PreparationCounter",
            context.environment,
            new Vector3(3.25f, 0f, 3.82f),
            new Vector3(2.8f, 1.25f, 0.92f),
            new Vector3(0f, 180f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Clock_03.prefab",
            "PreparationClock",
            context.environment,
            new Vector3(1.65f, 2.2f, 4.3f),
            new Vector3(0.72f, 0.72f, 0.1f),
            new Vector3(0f, 180f, 0f),
            false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Plants/Plants_05.prefab",
            "KitchenPlant",
            context.environment,
            new Vector3(4.3f, 1.23f, 3.7f),
            new Vector3(0.7f, 0.95f, 0.7f),
            Vector3.zero,
            false);
        DressKitchen(context);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Kitchen_Table_09.prefab",
            "PreparationTable",
            context.environment,
            new Vector3(0f, 0f, 0.55f),
            new Vector3(5.7f, 1.25f, 2.65f),
            new Vector3(0f, 90f, 0f),
            false);
        GameObject packingMat = CreateAssemblyRoot(
            context.environment, "PackingScaleMat", new Vector3(0f, 1.22f, 0.68f), Vector3.zero);
        CreateLocalPrimitive(packingMat.transform, "RubberScaleSurface", PrimitiveType.Cube,
            Vector3.zero, new Vector3(2.15f, 0.08f, 1.55f),
            ScopedMaterial(context, "PackingScaleRubber", new Color32(32, 45, 50, 255), 0.22f));
        CreateLocalPrimitive(packingMat.transform, "ScaleDial", PrimitiveType.Cylinder,
            new Vector3(0.78f, 0.09f, -0.5f), new Vector3(0.24f, 0.035f, 0.24f),
            context.materials.cream);
        CreateLocalPrimitive(packingMat.transform, "ScaleNeedle", PrimitiveType.Cube,
            new Vector3(0.78f, 0.14f, -0.5f), new Vector3(0.035f, 0.02f, 0.18f),
            context.materials.danger, new Vector3(0f, 28f, 0f));
        GameObject bag = StoryAuthoredPropFactory.CreateEmergencyBackpack(
            "EmergencyBag", context.environment, new Vector3(0f, 1.28f, 1.0f),
            new Vector3(1.45f, 1.62f, 1.02f), new Vector3(0f, 180f, 0f),
            ScopedMaterial(context, "BagBody", new Color32(26, 64, 76, 255), 0.36f),
            ScopedMaterial(context, "BagPocket", new Color32(17, 41, 50, 255), 0.3f),
            context.materials.amber, ScopedMaterial(context, "BagInside", new Color32(21, 26, 30, 255), 0.18f),
            true, true);
        Transform bagOpening = CreateDropTarget(context, context.gameplay, "BagOpeningTarget", new Vector3(0f, 2.15f, 1.0f), default, "ÇANTAYA");
        Transform rejectTray = CreateDropTarget(context, context.gameplay, "HeavyItemReturnTray", new Vector3(0.95f, 1.32f, -0.05f), default, "GERİ KOY");
        GameObject blackoutTint = CreateDimmingOverlay(
            context,
            "BlackoutScreenTint",
            new Color(0.015f, 0.035f, 0.075f, 0.56f));

        GameObject flashlightGlow = context.lightingVfx.Find("Amber Practical Light").gameObject;
        flashlightGlow.name = "BlackoutFlashlightGlow";
        flashlightGlow.transform.position = new Vector3(0f, 2.2f, 0.2f);
        Light flashlightLight = flashlightGlow.GetComponent<Light>();
        flashlightLight.type = LightType.Spot;
        flashlightLight.color = new Color32(222, 245, 255, 255);
        flashlightLight.intensity = 4.2f;
        flashlightLight.range = 8f;
        flashlightLight.spotAngle = 46f;
        flashlightLight.shadows = LightShadows.None;
        flashlightGlow.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
        flashlightGlow.SetActive(false);

        List<MinigameStageDefinition> stages = new List<MinigameStageDefinition>();

        GameObject signalRoot = NewStageRoot(context, "Stage_01_SignalRound", true);
        CreatePreparationMat(context, signalRoot.transform, "SignalEquipmentMat", "SİNYAL EKİPMANI",
            new Vector3(-0.08f, 1.245f, -0.32f), new Vector3(4.35f, 0.05f, 0.9f), context.materials.cyan);
        GameObject flashlight = CreateEmergencyFlashlight(
            context, signalRoot.transform, "Flashlight", new Vector3(-1.18f, 1.3f, -0.12f), new Vector3(0f, -8f, 0f));
        flashlight.transform.localScale = Vector3.one * 0.7f;
        GameObject radio = CreateEmergencyRadio(
            context, signalRoot.transform, "EmergencyRadio", new Vector3(-0.72f, 1.28f, -0.1f), new Vector3(0f, -4f, 0f));
        radio.transform.localScale = Vector3.one * 0.72f;
        GameObject battery = CreateBatteryPack(
            context, signalRoot.transform, "SpareBatteries", new Vector3(0.16f, 1.28f, -0.12f), new Vector3(0f, 8f, 0f));
        battery.transform.localScale = Vector3.one * 0.78f;
        GameObject whistle = CreateEmergencyWhistle(
            context, signalRoot.transform, "Whistle", new Vector3(0.78f, 1.3f, -0.12f), new Vector3(0f, -12f, 0f));
        whistle.transform.localScale = Vector3.one * 0.65f;
        GameObject wrongPan = CreateKitchenPan(
            context, signalRoot.transform, "WrongPan", new Vector3(1.05f, 1.28f, 0.08f), new Vector3(0f, -15f, 0f));
        wrongPan.transform.localScale = Vector3.one * 0.55f;
        MinigameStageDefinition signalStage = Stage(context, "signal-round", "01_signal",
            "30 SN • Fener, radyo, pil ve düdüğü ÇANTAYA SÜRÜKLE",
            MinigameGesture.DragToTarget, signalRoot,
            new[]
            {
                PolishAction(Action("flashlight", flashlight, true, bagOpening, null, true), "1/4 • FENERİ ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                PolishAction(Action("radio", radio, true, bagOpening, null, true), "2/4 • RADYOYU ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                PolishAction(Action("battery", battery, true, bagOpening, null, true), "3/4 • YEDEK PİLİ ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                PolishAction(Action("whistle", whistle, true, bagOpening, null, true), "4/4 • DÜDÜĞÜ ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                Action("pan", wrongPan, false, bagOpening, "Tava temel sinyal malzemesi değil; doğru eşya masaya geri döner.")
            }, 4, 8f, 30f);
        PresentStage(context, signalStage, "Bag_SignalRound",
            new Vector3(0f, 3.15f, -7.15f), new Vector3(0f, 1.35f, 0.35f), 39f,
            CharacterCueAt(family.parent, -20f, true, new Vector3(1.0f, 0f, 2.2f)));
        stages.Add(signalStage);

        GameObject blackoutRoot = NewStageRoot(context, "Stage_02_BlackoutProof", false);
        CreatePreparationMat(context, blackoutRoot.transform, "BlackoutProofMat", "KESİNTİ TESTİ",
            new Vector3(-1.25f, 1.245f, -0.3f), new Vector3(1.7f, 0.05f, 0.85f), context.materials.amber);
        GameObject blackoutFlashlight = CreateEmergencyFlashlight(
            context, blackoutRoot.transform, "PoweredFlashlight", new Vector3(-1.25f, 1.3f, -0.12f),
            new Vector3(0f, -8f, 0f));
        MinigameActionDefinition blackoutAction = Action("power-proof", blackoutFlashlight, true);
        PolishAction(blackoutAction, "FENERİN DÜĞMESİNE DOKUN • IŞIĞI TEST ET", context.materials.amber.color);
        UnityEventTools.AddBoolPersistentListener(blackoutAction.onAccepted, flashlightGlow.SetActive, true);
        MinigameStageDefinition blackoutStage = Stage(context, "blackout-proof", "02_blackout",
            "Elektrik kesildi • Hazır feneri DOKUNARAK yak",
            MinigameGesture.Tap, blackoutRoot,
            new[] { blackoutAction }, 1, 10f, 22f);
        UnityEventTools.AddBoolPersistentListener(blackoutStage.onEnter, blackoutTint.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(blackoutAction.onAccepted, blackoutTint.SetActive, false);
        PresentStage(context, blackoutStage, "Bag_Blackout",
            new Vector3(-0.15f, 2.7f, -5.65f), new Vector3(-0.9f, 1.45f, -0.05f), 35f,
            CharacterCueAt(family.can, -12f, true, new Vector3(0.65f, 0f, 1.8f)));
        stages.Add(blackoutStage);

        GameObject foodRoot = NewStageRoot(context, "Stage_03_FoodWaterRound", false);
        CreatePreparationMat(context, foodRoot.transform, "FoodWaterMat", "SU + DAYANIKLI GIDA",
            new Vector3(-0.05f, 1.245f, -0.32f), new Vector3(3.9f, 0.05f, 0.9f), context.materials.cyan);
        GameObject water = CreateSealedBottle(
            context, foodRoot.transform, "SealedWater", new Vector3(-0.98f, 1.27f, -0.12f), false);
        GameObject cannedFood = CreateDurableFoodCan(
            context, foodRoot.transform, "DurableFood", new Vector3(-0.25f, 1.27f, -0.12f));
        GameObject glassBottle = CreateSealedBottle(
            context, foodRoot.transform, "WrongGlassBottle", new Vector3(1.15f, 1.27f, -0.12f), true);
        water.transform.localScale = Vector3.one * 0.8f;
        glassBottle.transform.localScale = Vector3.one * 0.8f;
        MinigameStageDefinition foodStage = Stage(context, "food-water", "03_food_water",
            "30 SN • Kapalı su ve dayanıklı yiyeceği ÇANTAYA SÜRÜKLE",
            MinigameGesture.DragToTarget, foodRoot,
            new[]
            {
                PolishAction(Action("water", water, true, bagOpening, null, true), "1/2 • KAPALI SUYU ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                PolishAction(Action("food", cannedFood, true, bagOpening, null, true), "2/2 • DAYANIKLI GIDAYI ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                Action("glass", glassBottle, false, bagOpening, "Kırılabilir cam şişe yerine kapalı ve dayanıklı suyu seç.")
            }, 2, 8f, 30f);
        PresentStage(context, foodStage, "Bag_FoodWater",
            new Vector3(0f, 3.05f, -6.85f), new Vector3(0f, 1.35f, 0.35f), 38f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(1.0f, 0f, 2.1f)));
        stages.Add(foodStage);

        GameObject healthRoot = NewStageRoot(context, "Stage_04_HealthDocsRound", false);
        CreatePreparationMat(context, healthRoot.transform, "HealthDocumentsMat", "SAĞLIK + BELGE + ISI",
            new Vector3(0.5f, 1.245f, -0.28f), new Vector3(5.75f, 0.05f, 0.95f), context.materials.amber);
        GameObject firstAid = StoryAuthoredPropFactory.CreateFirstAidKit(
            "FirstAidKit", healthRoot.transform, new Vector3(-0.72f, 1.34f, -0.05f),
            new Vector3(0.9f, 0.68f, 0.5f), Vector3.zero,
            context.materials.navy, context.materials.dark, context.materials.white,
            context.materials.amber, true);
        GameObject documents = StoryAuthoredPropFactory.CreateEnvelope(
            "DocumentFolder", healthRoot.transform, new Vector3(0.15f, 1.34f, -0.05f),
            new Vector3(0.96f, 0.22f, 0.68f), new Vector3(0f, 15f, 0f),
            context.materials.cream, ScopedMaterial(context, "DocumentFlap", new Color32(214, 198, 164, 255), 0.18f),
            context.materials.danger, true);
        GameObject blanket = StoryAuthoredPropFactory.CreateFoldedCloth(
            "EmergencyBlanket", healthRoot.transform, new Vector3(1.0f, 1.34f, -0.05f),
            new Vector3(0.95f, 0.38f, 0.72f), new Vector3(0f, -10f, 0f),
            ScopedMaterial(context, "BlanketCloth", new Color32(96, 144, 145, 255), 0.18f),
            context.materials.cream, true);
        GameObject tablet = CreateAssetCarrier(healthRoot.transform, "WrongTablet",
            "Assets/Bolum1Prefab/Item_Tablet.prefab", new Vector3(1.78f, 1.34f, 0.28f), new Vector3(0.75f, 0.48f, 0.12f));
        MinigameStageDefinition healthStage = Stage(context, "health-docs", "04_health_docs",
            "30 SN • İlk yardım, belge dosyası ve battaniyeyi ÇANTAYA SÜRÜKLE",
            MinigameGesture.DragToTarget, healthRoot,
            new[]
            {
                PolishAction(Action("first-aid", firstAid, true, bagOpening, null, true), "1/3 • İLK YARDIM SETİNİ YERLEŞTİR", context.materials.amber.color),
                PolishAction(Action("documents", documents, true, bagOpening, null, true), "2/3 • BELGE DOSYASINI YERLEŞTİR", context.materials.amber.color),
                PolishAction(Action("blanket", blanket, true, bagOpening, null, true), "3/3 • BATTANİYEYİ YERLEŞTİR", context.materials.amber.color),
                Action("tablet", tablet, false, bagOpening, "Tablet temel malzeme değil; gerekli üç parçayı tamamla.")
            }, 3, 8f, 30f);
        PresentStage(context, healthStage, "Bag_HealthDocs",
            new Vector3(0.45f, 3.0f, -7.0f), new Vector3(0.45f, 1.36f, 0.4f), 39f,
            CharacterCueAt(family.parent, -20f, true, new Vector3(1.1f, 0f, 2.2f)));
        stages.Add(healthStage);

        GameObject weightRoot = NewStageRoot(context, "Stage_05_WeightTest", false);
        GameObject bagTarget = CreateActionVolume(weightRoot.transform, "BagWeightHandle",
            bag.transform.position + Vector3.up * 0.72f, new Vector3(1.45f, 1.65f, 1.0f));
        MinigameStageDefinition weightStage = Stage(context, "weight-test", "05_weight_test",
            "Çanta sapından AŞAĞI KAYDIR • TARTIYI BASTIR",
            MinigameGesture.SwipeDown, weightRoot,
            new[]
            {
                PolishAction(Action("weight", bagTarget, true), "ÇANTA SAPINDAN AŞAĞI KAYDIR", context.materials.amber.color)
            }, 1, 10f, 24f);
        weightStage.playOnSuccess = new[] { CreateRockAnimation(bag, "BagRush_HeavyBagTest", 7f, 1.35f) };
        PresentStage(context, weightStage, "Bag_WeightTest",
            new Vector3(0.1f, 2.65f, -5.35f), new Vector3(0f, 1.55f, 0.95f), 34f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(-0.9f, 0f, 2.1f)));
        stages.Add(weightStage);

        GameObject consoleRoot = NewStageRoot(context, "Stage_06_RemoveConsole", false);
        CreatePreparationMat(context, consoleRoot.transform, "RemoveHeavyItemMat", "AĞIR EŞYA • GERİ KOY",
            new Vector3(0f, 1.245f, -0.3f), new Vector3(3.5f, 0.05f, 0.9f), context.materials.danger);
        CreateStorageBasket(context, consoleRoot.transform, "HeavyItemReturnBasket",
            new Vector3(0.95f, 1.2f, -0.05f), new Vector3(0.7f, 0.28f, 0.54f));
        GameObject console = CreateAssetCarrier(consoleRoot.transform, "HeavyGameConsole",
            "Assets/Bolum1Prefab/GameConsole_01 Variant.prefab", new Vector3(-0.65f, 1.3f, -0.08f), new Vector3(0.68f, 0.34f, 0.52f));
        MinigameStageDefinition removeStage = Stage(context, "remove-console", "06_remove_console",
            "30 SN • Ağır konsolu dönüş tepsisine SÜRÜKLE",
            MinigameGesture.DragToTarget, consoleRoot,
            new[]
            {
                PolishAction(Action("console", console, true, rejectTray, null, true),
                    "AĞIR KONSOLU ÇANTADAN ÇIKAR • GERİ KOY", context.materials.danger.color)
            }, 1, 9f, 30f);
        PresentStage(context, removeStage, "Bag_RemoveConsole",
            new Vector3(0.1f, 2.8f, -5.9f), new Vector3(0f, 1.35f, 0.4f), 36f,
            CharacterCueAt(family.can, -12f, true, new Vector3(1.0f, 0f, 2.0f)));
        stages.Add(removeStage);

        GameObject comfortRoot = NewStageRoot(context, "Stage_07_ComfortChoice", false);
        CreatePreparationMat(context, comfortRoot.transform, "ComfortChoiceMat", "YALNIZ 1 KÜÇÜK EŞYA",
            new Vector3(0.2f, 1.245f, -0.3f), new Vector3(3.0f, 0.05f, 0.9f), context.materials.cyan);
        GameObject toyCar = CreateFurnitureCarrier(comfortRoot.transform, "SmallComfortCar",
            "Decorations/Toy_02.prefab", new Vector3(-0.72f, 1.34f, -0.05f),
            new Vector3(0.66f, 0.33f, 0.44f));
        GameObject largeToy = CreateFurnitureCarrier(comfortRoot.transform, "WrongLargeToy",
            "Decorations/Toy_03.prefab", new Vector3(1.18f, 1.34f, -0.05f),
            new Vector3(0.72f, 0.66f, 0.58f));
        MinigameStageDefinition comfortStage = Stage(context, "comfort", "07_comfort",
            "Yalnız BİR küçük rahatlatıcı eşyayı ÇANTAYA SÜRÜKLE",
            MinigameGesture.DragToTarget, comfortRoot,
            new[]
            {
                PolishAction(Action("small-car", toyCar, true, bagOpening, null, true),
                    "KÜÇÜK OYUNCAĞI ÇANTAYA YERLEŞTİR", context.materials.cyan.color),
                Action("large-toy", largeToy, false, bagOpening, "Bu eşya çantayı gereksiz ağırlaştırır; yalnız küçük bir eşya seç.")
            }, 1, 11f, 26f);
        PresentStage(context, comfortStage, "Bag_Comfort",
            new Vector3(0.15f, 2.75f, -5.85f), new Vector3(0.2f, 1.35f, 0.35f), 36f,
            CharacterCueAt(family.parent, -20f, true, new Vector3(1.0f, 0f, 2.1f)));
        stages.Add(comfortStage);

        GameObject strapsRoot = NewStageRoot(context, "Stage_08_AdjustStraps", false);
        GameObject strapLeft = CreateBackpackAdjustmentStrap(
            context, strapsRoot.transform, "LeftShoulderStrap", new Vector3(-0.38f, 1.48f, 0.42f), -1f);
        GameObject strapRight = CreateBackpackAdjustmentStrap(
            context, strapsRoot.transform, "RightShoulderStrap", new Vector3(0.38f, 1.48f, 0.42f), 1f);
        MinigameStageDefinition strapsStage = Stage(context, "straps", "08_straps",
            "İki omuz askısını YATAY KAYDIRARAK ayarla",
            MinigameGesture.SwipeHorizontal, strapsRoot,
            new[]
            {
                PolishAction(Action("left-strap", strapLeft, true), "SOL ASKIDAN SOLA KAYDIR",
                    context.materials.cyan.color, Vector2.left),
                PolishAction(Action("right-strap", strapRight, true), "SAĞ ASKIDAN SAĞA KAYDIR",
                    context.materials.cyan.color, Vector2.right)
            }, 2, 12f, 30f);
        PresentStage(context, strapsStage, "Bag_Straps",
            new Vector3(0f, 2.45f, -4.8f), new Vector3(0f, 1.55f, 0.72f), 32f,
            CharacterCueAt(family.deniz, 18f, true, new Vector3(-0.9f, 0f, 2.0f)));
        stages.Add(strapsStage);

        GameObject keyLight = context.lightingVfx.Find("Key Directional Light")?.gameObject;
        GameObject fillLight = context.lightingVfx.Find("Cyan Fill Light")?.gameObject;
        if (keyLight != null)
        {
            UnityEventTools.AddBoolPersistentListener(stages[1].onEnter, keyLight.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(stages[2].onEnter, keyLight.SetActive, true);
        }
        if (fillLight != null)
        {
            UnityEventTools.AddBoolPersistentListener(stages[1].onEnter, fillLight.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(stages[2].onEnter, fillLight.SetActive, true);
        }
        UnityEventTools.AddBoolPersistentListener(stages[2].onEnter, flashlightGlow.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(stages[2].onEnter, blackoutTint.SetActive, false);

        FinishScene(context, stages.ToArray(), showDialog);
    }

    private static void BuildEmergencyCorridor(bool showDialog)
    {
        SceneContext context = CreateContext(
            MinigameSceneCatalog.EmergencyCorridorPath,
            "emergency-corridor",
            "ACİL KORİDOR",
            "EmergencyCorridor",
            new Vector3(8.15f, 7.55f, -11.25f),
            new Vector3(0f, 0.72f, 0.55f),
            new Color32(25, 47, 75, 255),
            true);
        BuildIntersection(context);

        RuntimeAnimatorController adultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            StoryAnimationLibraryBuilder.AdultControllerPath);
        GameObject coordinator = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/Story/Characters/MeshyFamily/Prefabs/Baba.prefab",
            "EmergencyCoordinator",
            context.characters,
            new Vector3(-0.35f, 0.12f, -3.05f),
            1.78f,
            adultController);
        FaceCharacterToCamera(coordinator, context.camera, 18f);
        GameObject teamA = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/Story/Characters/MeshyFamily/Prefabs/Anne.prefab",
            "BarrierTeamLead",
            context.characters,
            new Vector3(4.15f, 0.12f, -2.4f),
            1.72f,
            adultController);
        FaceCharacterToCamera(teamA, context.camera, -18f);
        GameObject dispatchPodium = CreateDispatchPodium(
            context, new Vector3(1.05f, 0.12f, -1.85f), new Vector3(0f, 12f, 0f));
        dispatchPodium.transform.localScale = new Vector3(0.82f, 1f, 0.82f);

        GameObject blockedCarA = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Pickup_01.fbx", "BlockedCarA", context.environment,
            new Vector3(-1.7f, 0.12f, -0.4f), new Vector3(1.8f, 1.25f, 3.4f), Vector3.zero, false);
        GameObject blockedCarB = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Pickup_01.fbx", "BlockedCarB", context.environment,
            new Vector3(1.5f, 0.12f, 0.55f), new Vector3(1.8f, 1.25f, 3.4f), new Vector3(0f, 180f, 0f), false);
        GameObject parkedCarA = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Pickup_01.fbx", "ParkedCarA", context.environment,
            new Vector3(-3.25f, 0.12f, 2.45f), new Vector3(1.8f, 1.25f, 3.4f), new Vector3(0f, 90f, 0f), false);
        GameObject parkedCarB = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Pickup_01.fbx", "ParkedCarB", context.environment,
            new Vector3(2.05f, 0.12f, 2.45f), new Vector3(1.8f, 1.25f, 3.4f), new Vector3(0f, -90f, 0f), false);
        Material responderBlue = ScopedMaterial(context, "ResponderVehicleBlue", new Color32(34, 83, 103, 255), 0.46f);
        Material responderCream = ScopedMaterial(context, "ResponderVehicleCream", new Color32(202, 190, 153, 255), 0.42f);
        StyleVehicle(context, blockedCarA, responderBlue, "ResponderGlassBlue");
        StyleVehicle(context, parkedCarA, responderBlue, "ResponderGlassBlue");
        StyleVehicle(context, blockedCarB, responderCream, "ResponderGlassCream");
        StyleVehicle(context, parkedCarB, responderCream, "ResponderGlassCream");
        AddVehicleRoofAccent(context, blockedCarA, "DriverCueA", context.materials.cyan);
        AddVehicleRoofAccent(context, blockedCarB, "DriverCueB", context.materials.amber);
        AddVehicleRoofAccent(context, parkedCarA, "ParkedCueA", context.materials.cyan);
        AddVehicleRoofAccent(context, parkedCarB, "ParkedCueB", context.materials.amber);
        parkedCarA.SetActive(false);
        parkedCarB.SetActive(false);
        Transform pocketLeft = CreateDropTarget(context, context.gameplay, "SafePocketLeft",
            new Vector3(-3.0f, 0.65f, 2.2f), default, "A • GÜVENLİ CEP", context.materials.cyan);
        Transform pocketRight = CreateDropTarget(context, context.gameplay, "SafePocketRight",
            new Vector3(1.7f, 0.65f, 2.2f), default, "B • GÜVENLİ CEP", context.materials.amber);
        CreateParkingPocketMarkings(context, "SafePocketMarkingLeft", new Vector3(-3.05f, 0.165f, 2.2f), 0f);
        CreateParkingPocketMarkings(context, "SafePocketMarkingRight", new Vector3(1.75f, 0.165f, 2.2f), 0f);
        StoryChapterBuilderCommon.InstantiateAsset(
            CityRoot + "/Prop_StreetSign_Parking.prefab", "SafePocketParkingSignLeft", context.environment,
            new Vector3(-4.2f, 0.18f, 3.55f), new Vector3(0.55f, 2.1f, 0.55f), Vector3.zero, false);
        StoryChapterBuilderCommon.InstantiateAsset(
            CityRoot + "/Prop_StreetSign_Parking.prefab", "SafePocketParkingSignRight", context.environment,
            new Vector3(2.8f, 0.18f, 3.55f), new Vector3(0.55f, 2.1f, 0.55f), Vector3.zero, false);

        List<MinigameStageDefinition> stages = new List<MinigameStageDefinition>();
        GameObject routeRoot = NewStageRoot(context, "Stage_01_RouteCars", true);
        pocketLeft.SetParent(routeRoot.transform, true);
        pocketRight.SetParent(routeRoot.transform, true);
        CreateRoadChevronTrail(context, routeRoot.transform, "RouteATrail",
            new Vector3(-1.75f, 0.2f, -0.15f), new Vector3(-2.35f, 0f, 2.35f), 6, context.materials.cyan);
        CreateRoadChevronTrail(context, routeRoot.transform, "RouteBTrail",
            new Vector3(1.65f, 0.2f, 0.75f), new Vector3(1.7f, 0f, 1.55f), 6, context.materials.amber);
        GameObject routeA = CreateRouteToken(context, routeRoot.transform, "RouteCommandA",
            new Vector3(0.66f, 1.52f, -1.72f), new Vector3(-12f, 12f, 0f), false, "A");
        GameObject routeB = CreateRouteToken(context, routeRoot.transform, "RouteCommandB",
            new Vector3(1.46f, 1.5f, -1.62f), new Vector3(-12f, 12f, 0f), true, "B");
        routeA.transform.localScale = Vector3.one * 1.32f;
        routeB.transform.localScale = Vector3.one * 1.32f;
        Animation carRouteA = CreateMoveAnimation(blockedCarA, "Corridor_DriverAUsesPocket",
            blockedCarA.transform.localPosition, new Vector3(-3.25f, 0.12f, 2.45f), 1.45f);
        Animation carRouteB = CreateMoveAnimation(blockedCarB, "Corridor_DriverBUsesPocket",
            blockedCarB.transform.localPosition, new Vector3(2.05f, 0.12f, 2.45f), 1.45f);
        MinigameActionDefinition routeActionA = PolishAction(
            Action("route-a", routeA, true, pocketLeft, null, true),
            "1/2 • MAVİ A KARTINI MAVİ A CEBİNE SÜRÜKLE", context.materials.cyan.color);
        MinigameActionDefinition routeActionB = PolishAction(
            Action("route-b", routeB, true, pocketRight, null, true),
            "2/2 • SARI B KARTINI SARI B CEBİNE SÜRÜKLE", context.materials.amber.color);
        routeActionA.acceptedAnimations = new[] { carRouteA };
        routeActionB.acceptedAnimations = new[] { carRouteB };
        routeActionA.authoredMoveSeconds = 1.5f;
        routeActionB.authoredMoveSeconds = 1.5f;
        MinigameStageDefinition routeStage = Stage(context, "route-cars", "01_route_cars",
            "ARAÇLARI DEĞİL ROTA KARTLARINI eş renkli ceplere SÜRÜKLE",
            MinigameGesture.DragToTarget, routeRoot,
            new[] { routeActionA, routeActionB }, 2, 10f, 38f);
        PresentStage(context, routeStage, "Corridor_RouteCars",
            new Vector3(6.8f, 4.2f, -9.2f), new Vector3(0.15f, 0.85f, 0.15f), 42f,
            CharacterCueAt(coordinator, 18f, true, new Vector3(-0.6f, 0.12f, -1.45f)));
        stages.Add(routeStage);

        GameObject barrierRoot = NewStageRoot(context, "Stage_02_BarrierMarks", false);
        GameObject barrierMarkA = CreateRouteToken(context, barrierRoot.transform, "BarrierCommandWest",
            new Vector3(0.68f, 1.52f, -1.72f), new Vector3(-12f, 12f, 0f), false, "BATI");
        GameObject barrierMarkB = CreateRouteToken(context, barrierRoot.transform, "BarrierCommandEast",
            new Vector3(1.48f, 1.5f, -1.62f), new Vector3(-12f, 12f, 0f), true, "DOĞU");
        barrierMarkA.transform.localScale = Vector3.one * 1.22f;
        barrierMarkB.transform.localScale = Vector3.one * 1.22f;
        Transform barrierTargetA = CreateDropTarget(context, barrierRoot.transform, "WestRoadClosureTarget",
            new Vector3(-3.15f, 0.82f, 0.05f), default, "BATI GİRİŞİ", context.materials.cyan);
        Transform barrierTargetB = CreateDropTarget(context, barrierRoot.transform, "EastRoadClosureTarget",
            new Vector3(2.25f, 0.82f, 0.05f), default, "DOĞU GİRİŞİ", context.materials.amber);
        GameObject barrierVisualA = CreateRoadBarricade(context, "TeamBarrierA",
            new Vector3(-4.5f, 0.18f, -2.8f), new Vector3(0f, 90f, 0f), true);
        GameObject barrierVisualB = CreateRoadBarricade(context, "TeamBarrierB",
            new Vector3(4.5f, 0.18f, -2.8f), new Vector3(0f, 90f, 0f), true);
        Animation barrierMoveA = CreateMoveAnimation(barrierVisualA, "Corridor_TeamClosesWest",
            barrierVisualA.transform.localPosition, new Vector3(-3.15f, 0.18f, 0.05f), 1.35f);
        Animation barrierMoveB = CreateMoveAnimation(barrierVisualB, "Corridor_TeamClosesEast",
            barrierVisualB.transform.localPosition, new Vector3(2.25f, 0.18f, 0.05f), 1.35f);
        MinigameActionDefinition barrierActionA = PolishAction(
            Action("barrier-a", barrierMarkA, true, barrierTargetA, null, true),
            "1/2 • BATI BARİYER KARTINI BATI GİRİŞİNE SÜRÜKLE", context.materials.cyan.color);
        MinigameActionDefinition barrierActionB = PolishAction(
            Action("barrier-b", barrierMarkB, true, barrierTargetB, null, true),
            "2/2 • DOĞU BARİYER KARTINI DOĞU GİRİŞİNE SÜRÜKLE", context.materials.amber.color);
        barrierActionA.acceptedAnimations = new[] { barrierMoveA };
        barrierActionB.acceptedAnimations = new[] { barrierMoveB };
        barrierActionA.authoredMoveSeconds = 1.4f;
        barrierActionB.authoredMoveSeconds = 1.4f;
        MinigameStageDefinition barrierStage = Stage(context, "barriers", "02_barriers",
            "İki BARİYER KOMUT KARTINI eş girişlere SÜRÜKLE • bariyerleri yetişkin ekip taşır",
            MinigameGesture.DragToTarget, barrierRoot,
            new[] { barrierActionA, barrierActionB }, 2, 11f, 34f);
        PresentStage(context, barrierStage, "Corridor_BarrierTeam",
            new Vector3(6.8f, 4.15f, -9.1f), new Vector3(0f, 0.8f, 0.05f), 42f,
            CharacterCueAt(coordinator, 18f, true, new Vector3(-0.6f, 0.12f, -1.45f)),
            CharacterCueAt(teamA, -18f, false, new Vector3(2.3f, 0.12f, -1.1f)));
        stages.Add(barrierStage);

        GameObject crowdWaiting = CreatePedestrianGroup(context, "CrowdWaiting",
            new Vector3(-2.8f, 0.18f, 0.45f), true);
        GameObject crowdRoot = NewStageRoot(context, "Stage_03_CrowdRoute", false);
        crowdWaiting.transform.SetParent(crowdRoot.transform, true);
        CreateRoadChevronTrail(context, crowdRoot.transform, "PedestrianSafeTrail",
            new Vector3(-2.55f, 0.21f, 0.5f), new Vector3(-0.55f, 0f, 1.15f), 4, context.materials.cyan);
        GameObject crowdCommand = CreateRouteToken(context, crowdRoot.transform, "CrowdRouteCommand",
            new Vector3(1.05f, 1.5f, -1.68f), new Vector3(-12f, 12f, 0f), false, "YAYA");
        crowdCommand.transform.localScale = Vector3.one * 1.28f;
        Transform sidewalkTarget = CreateDropTarget(context, crowdRoot.transform, "SafeSidewalkTarget",
            new Vector3(-3.1f, 0.9f, 1.55f), default, "GÜVENLİ KALDIRIM", context.materials.cyan);
        GameObject crowdSafe = CreatePedestrianGroup(context, "CrowdSafe",
            new Vector3(-3.1f, 0.18f, 1.55f), false);
        Animation crowdMove = CreateMoveAnimation(crowdWaiting, "Corridor_CrowdUsesSidewalk",
            crowdWaiting.transform.localPosition, new Vector3(-3.1f, 0.18f, 1.55f), 1.4f);
        MinigameActionDefinition crowdAction = PolishAction(
            Action("crowd-route", crowdCommand, true, sidewalkTarget, null, true),
            "YAYA ROTA KARTINI GÜVENLİ KALDIRIMA SÜRÜKLE", context.materials.cyan.color);
        crowdAction.acceptedAnimations = new[] { crowdMove };
        crowdAction.authoredMoveSeconds = 1.45f;
        MinigameStageDefinition crowdStage = Stage(context, "crowd", "03_pedestrians",
            "YAYA ROTA KARTINI sürükle • kalabalık yol yerine kaldırımda ilerlesin",
            MinigameGesture.DragToTarget, crowdRoot,
            new[] { crowdAction }, 1, 11f, 30f);
        PresentStage(context, crowdStage, "Corridor_Pedestrians",
            new Vector3(6.4f, 3.8f, -8.4f), new Vector3(-0.6f, 0.8f, 0.25f), 42f,
            CharacterCueAt(coordinator, 18f, true, new Vector3(-0.7f, 0.12f, -1.45f)));
        stages.Add(crowdStage);

        GameObject facadeRoot = NewStageRoot(context, "Stage_04_FacadeDetour", false);
        GameObject safeRoute = new GameObject("SafeDetourSignal");
        safeRoute.transform.SetParent(facadeRoot.transform);
        CreateRoadChevronTrail(context, safeRoute.transform, "SafeChevron",
            new Vector3(0.8f, 0.2f, 2.45f), new Vector3(0.85f, 0f, 0.55f), 6,
            ScopedMaterial(context, "SafeRouteMarking", new Color32(61, 183, 191, 255), 0.18f));
        AddBoundsCollider(safeRoute);
        GameObject dangerRoute = new GameObject("DamagedFacadeRoute");
        dangerRoute.transform.SetParent(facadeRoot.transform);
        CreateRoadChevronTrail(context, dangerRoute.transform, "DangerChevron",
            new Vector3(-0.65f, 0.2f, 2.45f), new Vector3(-0.85f, 0f, 0.55f), 5,
            ScopedMaterial(context, "DangerRouteMarking", new Color32(154, 63, 59, 255), 0.16f));
        GameObject detourSlider = CreateDispatchSlider(context, facadeRoot.transform, "DetourRouteSlider",
            new Vector3(1.05f, 1.5f, -1.68f), new Vector3(-12f, 12f, 0f),
            context.materials.danger, context.materials.cyan, "CEPHE → AÇIK ROTA");
        Animation detourSlide = CreateMoveAnimation(detourSlider, "Corridor_SelectOpenDetour",
            detourSlider.transform.localPosition, detourSlider.transform.localPosition + new Vector3(0.76f, 0f, 0f), 0.62f);
        MinigameActionDefinition detourAction = PolishAction(
            Action("safe-detour", detourSlider, true),
            "ROTA KOLUNU HASARLI CEPHEDEN UZAĞA SAĞA KAYDIR", context.materials.cyan.color, Vector2.right);
        detourAction.acceptedAnimations = new[] { detourSlide };
        detourAction.authoredMoveSeconds = 0.68f;
        MinigameStageDefinition detourStage = Stage(context, "detour", "04_facade",
            "Hasarlı cepheden uzaklaş: rota kolunu AÇIK CAMGÖBEĞİ YOLA doğru SAĞA KAYDIR",
            MinigameGesture.SwipeHorizontal, facadeRoot,
            new[] { detourAction }, 1, 10f, 27f);
        PresentStage(context, detourStage, "Corridor_FacadeDetour",
            new Vector3(6.2f, 3.85f, -8.2f), new Vector3(0f, 0.9f, 0.35f), 42f,
            CharacterCueAt(coordinator, 18f, true, new Vector3(-0.6f, 0.12f, -1.45f)));
        stages.Add(detourStage);

        GameObject trafficRoot = NewStageRoot(context, "Stage_05_StopCrossTraffic", false);
        GameObject stopNorth = CreateGuardedControl(context, trafficRoot.transform, "StopNorthControl",
            new Vector3(0.66f, 1.5f, -1.72f), new Vector3(-12f, 12f, 0f), context.materials.danger);
        GameObject stopSouth = CreateGuardedControl(context, trafficRoot.transform, "StopSouthControl",
            new Vector3(1.46f, 1.48f, -1.62f), new Vector3(-12f, 12f, 0f), context.materials.danger);
        MinigameStageDefinition trafficStage = Stage(context, "cross-traffic", "05_cross_traffic",
            "BATI ve DOĞU kırmızı durdurma kontrollerine sırayla DOKUN",
            MinigameGesture.Tap, trafficRoot,
            new[]
            {
                PolishAction(Action("stop-north", stopNorth, true), "1/2 • BATI TRAFİĞİNİ DURDUR", context.materials.danger.color),
                PolishAction(Action("stop-south", stopSouth, true), "2/2 • DOĞU TRAFİĞİNİ DURDUR", context.materials.danger.color)
            }, 2, 10f, 26f);
        PresentStage(context, trafficStage, "Corridor_StopCrossTraffic",
            new Vector3(6.0f, 3.55f, -7.8f), new Vector3(0.4f, 1.0f, -0.3f), 40f,
            CharacterCueAt(coordinator, 18f, true, new Vector3(-0.45f, 0.12f, -1.4f)));
        stages.Add(trafficStage);

        GameObject greenRoot = NewStageRoot(context, "Stage_06_GreenWave", false);
        GameObject greenControl = CreateGuardedControl(context, greenRoot.transform, "GreenWaveControl",
            new Vector3(1.05f, 1.5f, -1.68f), new Vector3(-12f, 12f, 0f), context.materials.safe);
        MinigameStageDefinition greenStage = Stage(context, "green-wave", "06_green_wave",
            "Acil koridor sinyali sabitlenene kadar YEŞİL DALGA kontrolünde BASILI TUT",
            MinigameGesture.Hold, greenRoot,
            new[] { PolishAction(Action("green-wave", greenControl, true), "HALKA DOLANA KADAR BASILI TUT", context.materials.safe.color) },
            1, 10f, 24f, 1.8f);
        UnityEventTools.AddBoolPersistentListener(greenStage.onEnter, blockedCarA.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(greenStage.onEnter, blockedCarB.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(greenStage.onEnter, parkedCarA.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(greenStage.onEnter, parkedCarB.SetActive, true);
        PresentStage(context, greenStage, "Corridor_GreenWave",
            new Vector3(4.2f, 2.45f, -8.8f), new Vector3(0f, 0.85f, 1.0f), 36f,
            CharacterCueAt(teamA, -18f, true, new Vector3(1.9f, 0.12f, -1.05f)));
        stages.Add(greenStage);

        GameObject firetruck = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Firetruck_01.fbx", "EmergencyFiretruck", context.environment,
            new Vector3(0f, 0.12f, -8.5f), new Vector3(2.15f, 1.75f, 4.5f), Vector3.zero, false);
        StyleVehicle(
            context,
            firetruck,
            ScopedMaterial(context, "FiretruckBody", new Color32(153, 48, 43, 255), 0.44f),
            "FiretruckGlass");
        AddVehicleRoofAccent(context, firetruck, "EmergencyLightBar", context.materials.danger);
        Animation firetruckArrival = CreateMoveAnimation(
            firetruck, "Corridor_FiretruckArrival",
            firetruck.transform.localPosition,
            firetruck.transform.localPosition + new Vector3(0f, 0f, 16f),
            3.2f);
        GameObject arrivalRoot = NewStageRoot(context, "Stage_07_Arrival", false);
        GameObject arrivalBadge = CreateRouteToken(context, arrivalRoot.transform, "ArrivalDispatchApproval",
            new Vector3(1.05f, 1.5f, -1.68f), new Vector3(-12f, 12f, 0f), false, "İTFAİYE");
        arrivalBadge.transform.localScale = Vector3.one * 1.3f;
        Transform corridorGate = CreateDropTarget(context, arrivalRoot.transform, "OpenCorridorGate",
            new Vector3(0f, 0.9f, -0.65f), default, "AÇIK KORİDOR", context.materials.safe);
        MinigameActionDefinition arrivalAction = PolishAction(
            Action("arrival", arrivalBadge, true, corridorGate, null, true),
            "İTFAİYE GEÇİŞ KARTINI AÇIK KORİDORA SÜRÜKLE", context.materials.safe.color);
        arrivalAction.acceptedAnimations = new[] { firetruckArrival };
        arrivalAction.authoredMoveSeconds = 3.25f;
        MinigameStageDefinition arrivalStage = Stage(context, "arrival", "07_arrival",
            "İTFAİYE GEÇİŞ KARTINI açık koridora SÜRÜKLE • sonra sinematik geçişi izle",
            MinigameGesture.DragToTarget, arrivalRoot,
            new[] { arrivalAction }, 1, 14f, 32f);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, blockedCarA.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, blockedCarB.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, parkedCarA.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, parkedCarB.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, barrierVisualA.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, barrierVisualB.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, crowdWaiting.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(arrivalStage.onEnter, crowdSafe.SetActive, true);
        PresentStage(context, arrivalStage, "Corridor_FiretruckArrival",
            new Vector3(4.2f, 2.45f, -8.8f), new Vector3(0f, 0.85f, 1.0f), 36f,
            CharacterCueAt(teamA, -18f, false, new Vector3(1.9f, 0.12f, -1.05f)));
        stages.Add(arrivalStage);

        FinishScene(context, stages.ToArray(), showDialog);
    }

    private static void BuildIntersection(SceneContext context)
    {
        MaterialPack m = context.materials;
        CreateStaticPrimitive(context.environment, "IntersectionGround", PrimitiveType.Cube,
            new Vector3(0f, 0f, 0f), new Vector3(12f, 0.24f, 12f), m.asphalt);
        CreateStaticPrimitive(context.environment, "NorthSidewalk", PrimitiveType.Cube,
            new Vector3(0f, 0.14f, 4.8f), new Vector3(12f, 0.28f, 2.3f), m.concrete);
        CreateStaticPrimitive(context.environment, "SouthSidewalk", PrimitiveType.Cube,
            new Vector3(0f, 0.14f, -4.8f), new Vector3(12f, 0.28f, 2.3f), m.concrete);
        CreateStaticPrimitive(context.environment, "WestSidewalk", PrimitiveType.Cube,
            new Vector3(-4.8f, 0.14f, 0f), new Vector3(2.3f, 0.28f, 12f), m.concrete);
        CreateStaticPrimitive(context.environment, "EastSidewalk", PrimitiveType.Cube,
            new Vector3(4.8f, 0.14f, 0f), new Vector3(2.3f, 0.28f, 12f), m.concrete);
        for (int i = -2; i <= 2; i++)
        {
            CreateStaticPrimitive(context.environment, "LaneMarkerNS_" + i, PrimitiveType.Cube,
                new Vector3(0f, 0.15f, i * 2.1f), new Vector3(0.1f, 0.025f, 1.15f), m.cream, false);
            CreateStaticPrimitive(context.environment, "LaneMarkerEW_" + i, PrimitiveType.Cube,
                new Vector3(i * 2.1f, 0.15f, 0f), new Vector3(1.15f, 0.025f, 0.1f), m.cream, false);
        }
        for (int stripe = -3; stripe <= 3; stripe++)
        {
            CreateStaticPrimitive(context.environment, "CrosswalkNorth_" + stripe, PrimitiveType.Cube,
                new Vector3(stripe * 0.58f, 0.165f, 3.35f), new Vector3(0.34f, 0.028f, 1.15f),
                m.cream, false);
            CreateStaticPrimitive(context.environment, "CrosswalkWest_" + stripe, PrimitiveType.Cube,
                new Vector3(-3.35f, 0.165f, stripe * 0.58f), new Vector3(1.15f, 0.028f, 0.34f),
                m.cream, false);
        }
        Vector3[] signalPositions =
        {
            new Vector3(-4.25f, 0.18f, 4.1f), new Vector3(4.25f, 0.18f, 4.1f),
            new Vector3(-4.25f, 0.18f, -4.1f), new Vector3(4.25f, 0.18f, -4.1f)
        };
        for (int index = 0; index < signalPositions.Length; index++)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                CityRoot + "/Prop_StreetSign_TrafficSignals.prefab",
                "TrafficSignal_" + index,
                context.environment,
                signalPositions[index],
                new Vector3(0.75f, 3.3f, 0.75f),
                new Vector3(0f, 45f + index * 90f, 0f),
                false);
        }
        for (int index = 0; index < 6; index++)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                CityRoot + "/Prop_RoadCone_0" + (index % 3 + 1) + ".prefab",
                "AuthoredRoadCone_" + index,
                context.environment,
                new Vector3(-2.15f + index * 0.86f, 0.18f, -3.55f + (index % 2) * 0.3f),
                new Vector3(0.42f, 0.72f, 0.42f),
                Vector3.zero,
                false);
        }
        string[] buildingPrefabs =
        {
            "Env_CommercialBuilding_01.prefab", "Env_ResidentBuilding_03.prefab",
            "Env_CompanyBuilding_02.prefab", "Env_ResidentBuilding_05.prefab"
        };
        Vector3[] positions =
        {
            new Vector3(-5.75f, 0.18f, 5.8f), new Vector3(5.75f, 0.18f, 5.8f),
            new Vector3(-6.35f, 0.18f, 0f), new Vector3(6.35f, 0.18f, 0f)
        };
        for (int i = 0; i < buildingPrefabs.Length; i++)
        {
            GameObject building = StoryChapterBuilderCommon.InstantiateAsset(
                CityRoot + "/" + buildingPrefabs[i], "CityCorner_" + i,
                context.environment, positions[i], new Vector3(4.65f, 7.2f, 4.65f),
                new Vector3(0f, 45f + i * 90f, 0f), false);
            GameObjectUtility.SetStaticEditorFlags(building, StaticEditorFlags.BatchingStatic);
        }
        Vector3[] lampPositions =
        {
            new Vector3(-4.35f, 0.18f, 4.35f), new Vector3(4.35f, 0.18f, 4.35f),
            new Vector3(-4.35f, 0.18f, -4.35f), new Vector3(4.35f, 0.18f, -4.35f)
        };
        for (int index = 0; index < lampPositions.Length; index++)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                TownRoot + "/SM_Prop_Streetlamp_01.fbx", "Streetlamp_" + index,
                context.environment, lampPositions[index], new Vector3(0.48f, 3.7f, 0.48f),
                new Vector3(0f, index * 90f, 0f), false);
        }
        StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Prop_ParkBench_01.fbx", "CornerBench", context.environment,
            new Vector3(-4.85f, 0.18f, 1.45f), new Vector3(2.0f, 1.05f, 0.75f),
            new Vector3(0f, 90f, 0f), false);
        StoryAuthoredPropFactory.CreateDebrisCluster(
            "DamagedFacadeDebris", context.environment, new Vector3(-3.4f, 0.2f, 4.05f),
            new Vector3(2.3f, 0.72f, 1.2f), new Vector3(0f, 18f, 0f),
            context.materials.concrete, context.materials.dark, false);
        StoryChapterBuilderCommon.InstantiateAsset(
            CityRoot + "/Prop_StreetSign_NoEntery.prefab", "DamagedFacadeWarningSign", context.environment,
            new Vector3(-3.95f, 0.18f, 3.45f), new Vector3(0.62f, 2.25f, 0.62f),
            new Vector3(0f, 24f, 0f), false);
    }

    private static void BuildRubbleSignal(bool showDialog)
    {
        SceneContext context = CreateContext(
            MinigameSceneCatalog.RubbleSignalPath,
            "rubble-signal",
            "ENKAZDA SİNYAL",
            "RubbleSignal",
            new Vector3(7.05f, 5.25f, -10.65f),
            new Vector3(0f, 1.22f, 0.95f),
            new Color32(7, 16, 31, 255),
            true,
            true);
        BuildRescueSite(context);

        RuntimeAnimatorController adultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            StoryAnimationLibraryBuilder.AdultControllerPath);
        GameObject operatorCharacter = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/Story/Characters/MeshyFamily/Prefabs/Baba.prefab",
            "SensorOperator",
            context.characters,
            new Vector3(-2.65f, 0f, -1.7f),
            1.78f,
            adultController);
        FaceCharacterToCamera(operatorCharacter, context.camera, 18f);
        DressRescueSite(context);

        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab",
            "SensorConsoleTable",
            context.environment,
            new Vector3(-1.62f, 0f, -0.18f),
            new Vector3(3.25f, 1.2f, 1.55f),
            new Vector3(0f, 35f, 0f),
            false);
        Vector3 consoleFacing = Quaternion.LookRotation(
            new Vector3(-1.62f, 1.45f, -0.25f) - context.camera.transform.position,
            Vector3.up).eulerAngles;
        GameObject console = CreateLayeredWallConsole(
            context, context.environment, "DirectionalSensorConsole",
            new Vector3(-1.62f, 1.48f, -0.25f), consoleFacing,
            context.materials.cyan, "SENSÖR DİZİSİ");
        console.transform.localScale = new Vector3(1.42f, 1.18f, 1.0f);

        GameObject sensorA = CreateGroundSensorPod(
            context, context.environment, "SectorAReceiver",
            new Vector3(-1.55f, 0.18f, 2.08f), new Vector3(0f, 18f, 0f), context.materials.cyan, "A");
        GameObject sensorB = CreateGroundSensorPod(
            context, context.environment, "SectorBReceiver",
            new Vector3(0.35f, 0.18f, 2.28f), new Vector3(0f, -12f, 0f), context.materials.amber, "B");
        GameObject sensorC = CreateGroundSensorPod(
            context, context.environment, "SectorCReceiver",
            new Vector3(2.15f, 0.18f, 2.02f), new Vector3(0f, -32f, 0f), context.materials.danger, "C");

        AudioSource waterSource = CreateSpatialSource(context, "WaterDripSignal", new Vector3(-1.55f, 1.1f, 2.08f),
            "sfx100v2_loop_water_01.ogg", 0.38f);
        AudioSource metalSource = CreateSpatialSource(context, "MetalCreakSignal", new Vector3(2.15f, 1.2f, 2.02f),
            "sfx100v2_metal_03.ogg", 0.4f);
        AudioSource humanSource = CreateSpatialSource(context, "ThreeHumanKnocksSignal", new Vector3(0.35f, 0.8f, 2.28f),
            "sfx100v2_wood_hit_02.ogg", 0.52f);

        List<MinigameStageDefinition> stages = new List<MinigameStageDefinition>();
        GameObject calibrateRoot = NewStageRoot(context, "Stage_01_Calibration", true);
        Vector3 calibrationControlPosition = new Vector3(-0.42f, 1.28f, -0.48f);
        Vector3 calibrationControlFacing = Quaternion.LookRotation(
            calibrationControlPosition - context.camera.transform.position,
            Vector3.up).eulerAngles;
        GameObject calibratePad = CreateGuardedControl(
            context, calibrateRoot.transform, "CalibrationPad",
            calibrationControlPosition, calibrationControlFacing, context.materials.amber);
        MinigameActionDefinition calibrate = PolishAction(
            Action("calibrate", calibratePad, true),
            "BİLİNEN RİTİM • PEDA ÜÇ KEZ DOKUN", context.materials.amber.color, default, 3);
        calibrate.acceptedSfx = StoryChapterBuilderCommon.LoadLicensedSfx("sfx100v2_wood_hit_02.ogg");
        MinigameStageDefinition calibrationStage = Stage(context, "calibrate", "01_calibrate",
            "Kalibrasyon pedine ÜÇ KEZ DOKUN • bilinen üçlü vuruş ritmini tanıt",
            MinigameGesture.RepeatedTap, calibrateRoot,
            new[] { calibrate }, 1, 10f, 25f);
        UnityEventTools.AddPersistentListener(calibrationStage.onEnter, context.session.UseSilenceSnapshot);
        PresentStage(context, calibrationStage, "Rubble_Calibration",
            new Vector3(3.65f, 2.75f, -4.7f), new Vector3(-0.75f, 1.15f, -0.1f), 36f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(-2.3f, 0f, -1.4f)));
        stages.Add(calibrationStage);

        GameObject scanRoot = NewStageRoot(context, "Stage_02_SectorScan", false);
        Material scanBeam = ScopedMaterial(context, "SensorScanBeam", new Color32(43, 185, 199, 255), 0.18f, true);
        CreateDashedBeam(scanRoot.transform, "ScanLinkA", new Vector3(-1.2f, 0.35f, 0.25f), sensorA.transform.position + Vector3.up * 0.3f, scanBeam, 7, 0.026f);
        CreateDashedBeam(scanRoot.transform, "ScanLinkB", new Vector3(-0.95f, 0.35f, 0.25f), sensorB.transform.position + Vector3.up * 0.3f, scanBeam, 8, 0.026f);
        CreateDashedBeam(scanRoot.transform, "ScanLinkC", new Vector3(-0.7f, 0.35f, 0.25f), sensorC.transform.position + Vector3.up * 0.3f, scanBeam, 9, 0.026f);
        MinigameActionDefinition scanA = PolishAction(Action("sector-a", sensorA, true),
            "1/3 • A SENSÖRÜNÜ SAĞA SÜPÜR", context.materials.cyan.color, Vector2.right);
        MinigameActionDefinition scanB = PolishAction(Action("sector-b", sensorB, true),
            "2/3 • B SENSÖRÜNÜ SAĞA SÜPÜR", context.materials.amber.color, Vector2.right);
        MinigameActionDefinition scanC = PolishAction(Action("sector-c", sensorC, true),
            "3/3 • C SENSÖRÜNÜ SAĞA SÜPÜR", context.materials.danger.color, Vector2.right);
        scanA.acceptedAnimations = new[] { CreateRotateAnimation(sensorA, "Rubble_ScanSectorA", sensorA.transform.localEulerAngles, sensorA.transform.localEulerAngles + Vector3.up * 34f, 0.62f) };
        scanB.acceptedAnimations = new[] { CreateRotateAnimation(sensorB, "Rubble_ScanSectorB", sensorB.transform.localEulerAngles, sensorB.transform.localEulerAngles + Vector3.up * 34f, 0.62f) };
        scanC.acceptedAnimations = new[] { CreateRotateAnimation(sensorC, "Rubble_ScanSectorC", sensorC.transform.localEulerAngles, sensorC.transform.localEulerAngles + Vector3.up * 34f, 0.62f) };
        MinigameStageDefinition scanStage = Stage(context, "sector-scan", "02_scan",
            "A, B ve C yönlü sensörlerini sırayla SAĞA SÜPÜR • enkaza girme",
            MinigameGesture.SwipeHorizontal, scanRoot,
            new[] { scanA, scanB, scanC }, 3, 11f, 36f);
        PresentStage(context, scanStage, "Rubble_SectorSweep",
            new Vector3(0.6f, 4.0f, -7.2f), new Vector3(0.3f, 0.75f, 1.7f), 44f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(-2.45f, 0f, -1.35f)));
        stages.Add(scanStage);

        GameObject noiseRoot = NewStageRoot(context, "Stage_03_SeparateNoise", false);
        GameObject waterWave = CreateWaveformChoiceCard(
            context, noiseRoot.transform, "WaterWaveformChoice",
            new Vector3(-1.85f, 1.72f, -0.38f), Vector3.zero, context.materials.cyan,
            new[] { 0.2f, 0.42f, 0.2f, 0.42f, 0.2f, 0.42f }, "SU • DÜZENLİ");
        GameObject humanWave = CreateWaveformChoiceCard(
            context, noiseRoot.transform, "HumanTripleKnockChoice",
            new Vector3(-0.95f, 1.72f, -0.18f), Vector3.zero, context.materials.amber,
            new[] { 0.12f, 0.9f, 0.12f, 0.9f, 0.12f, 0.9f }, "İNSAN • ÜÇLÜ");
        GameObject metalWave = CreateWaveformChoiceCard(
            context, noiseRoot.transform, "MetalCreakChoice",
            new Vector3(-0.05f, 1.72f, 0.02f), Vector3.zero, context.materials.danger,
            new[] { 0.18f, 0.35f, 0.72f, 0.58f, 0.4f, 0.2f }, "METAL • UZUN");
        waterWave.transform.localScale = Vector3.one * 0.84f;
        humanWave.transform.localScale = Vector3.one * 0.84f;
        metalWave.transform.localScale = Vector3.one * 0.84f;
        MinigameActionDefinition waterChoice = PolishAction(Action("water", waterWave, false, null,
            "Düzenli damla sesi çevresel gürültüdür; üç kısa vuruşu ara."),
            "SU • DÜZENLİ DAMLA", context.materials.cyan.color);
        MinigameActionDefinition metalChoice = PolishAction(Action("metal", metalWave, false, null,
            "Uzun metal gıcırtısı yapı sesidir; üç kısa vuruşu ara."),
            "METAL • UZUN GICIRTI", context.materials.danger.color);
        MinigameActionDefinition humanChoice = PolishAction(Action("human", humanWave, true),
            "İNSAN • ÜÇ EŞİT KISA TEPE", context.materials.amber.color);
        waterChoice.rejectedSfx = null;
        metalChoice.rejectedSfx = null;
        humanChoice.acceptedSfx = null;
        UnityEventTools.AddPersistentListener(waterChoice.onRejected, waterSource.Play);
        UnityEventTools.AddPersistentListener(metalChoice.onRejected, metalSource.Play);
        UnityEventTools.AddPersistentListener(humanChoice.onAccepted, humanSource.Play);
        MinigameStageDefinition noiseStage = Stage(context, "noise", "03_noise",
            "Üç kısa insan vuruşu dalga formuna DOKUN",
            MinigameGesture.Tap, noiseRoot,
            new[] { waterChoice, humanChoice, metalChoice }, 1, 12f, 28f);
        PresentStage(context, noiseStage, "Rubble_WaveformCompare",
            new Vector3(0.3f, 2.75f, -6.4f), new Vector3(-0.95f, 1.5f, -0.08f), 44f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(0.72f, 0f, 0.85f)));
        stages.Add(noiseStage);

        GameObject primaryRoot = NewStageRoot(context, "Stage_04_PrimarySector", false);
        GameObject directionalMic = CreateDirectionalMicrophone(context, primaryRoot.transform,
            "HandheldDirectionalMicrophone", new Vector3(-0.52f, 1.2f, 0.12f), new Vector3(0f, -28f, 0f));
        Animation aimMic = CreateRotateAnimation(directionalMic, "Rubble_AimMicrophoneAtB",
            directionalMic.transform.localEulerAngles, directionalMic.transform.localEulerAngles + Vector3.up * 58f, 0.72f);
        MinigameActionDefinition primaryAction = PolishAction(Action("aim-b", directionalMic, true),
            "YÖNLÜ MİKROFONU B SEKTÖRÜNE DOĞRU SAĞA ÇEVİR", context.materials.amber.color, Vector2.right);
        primaryAction.targetHighlightRoot = CreateRim(sensorB, "PrimarySectorBGuide", true);
        primaryAction.acceptedAnimations = new[] { aimMic };
        primaryAction.authoredMoveSeconds = 0.78f;
        MinigameStageDefinition primaryStage = Stage(context, "primary", "04_primary",
            "En temiz üçlü vuruş için masadaki YÖNLÜ MİKROFONU B sektörüne doğru SAĞA ÇEVİR",
            MinigameGesture.SwipeHorizontal, primaryRoot,
            new[] { primaryAction }, 1, 12f, 28f);
        PresentStage(context, primaryStage, "Rubble_AimAtSectorB",
            new Vector3(4.15f, 2.9f, -3.95f), new Vector3(-0.05f, 1.1f, 1.05f), 35f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(-2.15f, 0f, -1.15f)));
        stages.Add(primaryStage);

        GameObject triangulateRoot = NewStageRoot(context, "Stage_05_Triangulate", false);
        GameObject secondSensor = CreateGroundSensorPod(
            context, triangulateRoot.transform, "SecondDirectionalSensor",
            new Vector3(-0.25f, 0.18f, -0.48f), new Vector3(0f, 28f, 0f), context.materials.cyan, "2");
        Transform triangulationTarget = CreateDropTarget(context, triangulateRoot.transform, "TriangulationLineTarget",
            new Vector3(0.85f, 1.05f, 2.7f), new Vector3(0f, 25f, 0f), "KESİŞİM", context.materials.cyan);
        Material beamMaterial = ScopedMaterial(context, "TriangulationBeam", new Color32(63, 202, 215, 255), 0.22f, true);
        CreateDashedBeam(triangulateRoot.transform, "BeamFromA",
            sensorA.transform.position + Vector3.up * 0.55f, new Vector3(0.85f, 0.72f, 2.7f),
            beamMaterial, 8, 0.035f);
        CreateDashedBeam(triangulateRoot.transform, "BeamFromSecond",
            secondSensor.transform.position + Vector3.up * 0.55f, new Vector3(0.85f, 0.72f, 2.7f),
            beamMaterial, 9, 0.035f);
        MinigameStageDefinition triangulateStage = Stage(context, "triangulate", "05_triangulate",
            "İkinci sensörü kesişen camgöbeği çizgiye SÜRÜKLE",
            MinigameGesture.DragToTarget, triangulateRoot,
            new[] { PolishAction(Action("sensor-two", secondSensor, true, triangulationTarget),
                "2 NUMARALI SENSÖRÜ KESİŞİM NOKTASINA SÜRÜKLE", context.materials.cyan.color) }, 1, 11f, 30f);
        PresentStage(context, triangulateStage, "Rubble_Triangulation",
            new Vector3(4.8f, 3.5f, -5.8f), new Vector3(0.3f, 0.7f, 1.1f), 41f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(-2.25f, 0f, -1.25f)));
        stages.Add(triangulateStage);

        GameObject markRoot = NewStageRoot(context, "Stage_06_MarkSafeVoid", false);
        GameObject safeVoid = CreateRescueMarkerFlag(
            context, markRoot.transform, "SafeVoidMarker",
            new Vector3(0.85f, 0.18f, 2.7f), new Vector3(0f, -18f, 0f));
        GameObject verifiedSafeVoid = CreateRescueMarkerFlag(
            context, context.environment, "VerifiedSafeVoidMarker",
            new Vector3(0.85f, 0.18f, 2.7f), new Vector3(0f, -18f, 0f));
        verifiedSafeVoid.SetActive(false);
        GameObject unstableSlab = CreateActionVolume(markRoot.transform, "UnstableSlabChoice",
            new Vector3(-0.7f, 1.05f, 3.05f), new Vector3(1.1f, 1.25f, 0.9f));
        MinigameActionDefinition safeVoidAction = PolishAction(
            Action("safe-void", safeVoid, true, null, null, true, verifiedSafeVoid),
            "YEŞİL İŞARETTE DOĞRULAMA HALKASI DOLANA KADAR TUT", context.materials.safe.color);
        MinigameStageDefinition markStage = Stage(context, "mark", "06_mark",
            "Yalnız doğrulanan YEŞİL GÜVENLİ BOŞLUK işaretinde BASILI TUT",
            MinigameGesture.Hold, markRoot,
            new[]
            {
                safeVoidAction,
                Action("slab", unstableSlab, false, null, "Dengesiz döşemeye yaklaşma; yalnız doğrulanan güvenli boşluğu işaretle.")
            }, 1, 12f, 30f, 1.65f);
        PresentStage(context, markStage, "Rubble_MarkSafeVoid",
            new Vector3(4.35f, 2.65f, -2.55f), new Vector3(0.9f, 0.8f, 2.55f), 34f,
            CharacterCueAt(operatorCharacter, 18f, true, new Vector3(-2.15f, 0f, -0.95f)));
        stages.Add(markStage);

        GameObject rescueRoot = NewStageRoot(context, "Stage_07_ProfessionalRescue", false);
        rescueRoot.SetActive(true);
        GameObject firefighter = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/POLYGONCityCharacters/Prefabs/Character_FireFighter_01.prefab",
            "RubbleFirefighter",
            rescueRoot.transform,
            new Vector3(0.25f, 0f, -0.2f),
            1.78f,
            adultController);
        // POLYGON City responder meshes use the opposite authored forward axis
        // from the Meshy family, so include the 180-degree model-space correction.
        FaceCharacterToCamera(firefighter, context.camera, 192f);
        GameObject paramedic = StoryChapterBuilderCommon.InstantiateCharacter(
            "Assets/POLYGONCityCharacters/Prefabs/Character_Paramedic_01.prefab",
            "RubbleParamedic",
            rescueRoot.transform,
            new Vector3(1.72f, 0f, -0.05f),
            1.74f,
            adultController);
        FaceCharacterToCamera(paramedic, context.camera, 168f);
        GameObject dispatch = CreateRouteToken(
            context, rescueRoot.transform, "ProfessionalDispatchCard",
            new Vector3(0.35f, 0.72f, -0.48f), calibrationControlFacing, false, "EKİP");
        dispatch.transform.localScale = Vector3.one * 1.28f;
        Transform rescueTarget = CreateDropTarget(context, rescueRoot.transform, "VerifiedVoidDispatchTarget",
            new Vector3(0.85f, 1.15f, 2.7f), default, "DOĞRULANDI", context.materials.safe);
        Animation firefighterApproach = CreateMoveAnimation(firefighter, "RubbleSignal_FirefighterApproach",
            firefighter.transform.localPosition, new Vector3(0.3f, 0f, 1.55f), 2.8f);
        Animation paramedicApproach = CreateMoveAnimation(paramedic, "RubbleSignal_ParamedicApproach",
            paramedic.transform.localPosition, new Vector3(1.55f, 0f, 1.35f), 2.8f);
        rescueRoot.SetActive(false);
        MinigameActionDefinition rescueAction = PolishAction(
            Action("dispatch", dispatch, true, rescueTarget, null, true),
            "PROFESYONEL EKİP KARTINI GÜVENLİ BOŞLUĞA SÜRÜKLE", context.materials.safe.color);
        rescueAction.acceptedAnimations = new[] { firefighterApproach, paramedicApproach };
        rescueAction.authoredMoveSeconds = 3.05f;
        MinigameStageDefinition rescueStage = Stage(context, "rescue", "07_rescue",
            "PROFESYONEL EKİP kartını işaretli güvenli boşluğa SÜRÜKLE • enkaza sen girme",
            MinigameGesture.DragToTarget, rescueRoot,
            new[] { rescueAction }, 1, 14f, 32f);
        UnityEventTools.AddPersistentListener(rescueStage.onEnter, context.session.UseGameplaySnapshot);
        UnityEventTools.AddBoolPersistentListener(rescueStage.onEnter, verifiedSafeVoid.SetActive, true);
        PresentStage(context, rescueStage, "Rubble_ProfessionalRescue",
            new Vector3(4.5f, 2.6f, -6.2f), new Vector3(0.5f, 0.85f, 1.1f), 38f,
            CharacterCue(firefighter, 192f, false), CharacterCue(paramedic, 168f, false));
        stages.Add(rescueStage);

        FinishScene(context, stages.ToArray(), showDialog);
    }

    private static void BuildRescueSite(SceneContext context)
    {
        CreateStaticPrimitive(context.environment, "RescueGround", PrimitiveType.Cube,
            new Vector3(0f, -0.12f, 0.6f), new Vector3(12f, 0.24f, 9f),
            ScopedMaterial(context, "RescueGround", new Color32(50, 60, 68, 255), 0.12f));
        StoryChapterBuilderCommon.InstantiateAsset(
            CityRoot + "/Env_ResidentBuilding_04.prefab", "DamagedBuildingShell", context.environment,
            new Vector3(0.9f, 0.05f, 5.7f), new Vector3(8.2f, 7.0f, 4.8f),
            new Vector3(0f, 180f, -2f), false);
        CreateStaticPrimitive(context.environment, "CollapsedFacadeSlab", PrimitiveType.Cube,
            new Vector3(1.65f, 1.75f, 4.05f), new Vector3(4.25f, 0.34f, 1.55f),
            context.materials.concrete, false, new Vector3(9f, -12f, -18f));
        Mesh rubbleA = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Story/Generated/Props/Meshes/RubbleChunkA.asset");
        Mesh rubbleB = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Story/Generated/Props/Meshes/RubbleChunkB.asset");
        for (int i = 0; i < 18; i++)
        {
            Mesh mesh = i % 2 == 0 ? rubbleA : rubbleB;
            GameObject chunk = new GameObject("AuthoredRubble_" + i, typeof(MeshFilter), typeof(MeshRenderer));
            chunk.transform.SetParent(context.environment);
            chunk.transform.position = new Vector3(-1.2f + (i % 6) * 0.82f, 0.25f + (i / 6) * 0.48f, 3.0f + (i % 3) * 0.38f);
            chunk.transform.rotation = Quaternion.Euler((i * 17f) % 45f, (i * 43f) % 180f, (i * 29f) % 55f);
            chunk.transform.localScale = Vector3.one * (0.72f + (i % 4) * 0.12f);
            chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
            chunk.GetComponent<MeshRenderer>().sharedMaterial = context.materials.concrete;
            GameObjectUtility.SetStaticEditorFlags(chunk, StaticEditorFlags.BatchingStatic);
        }
        for (int post = 0; post < 6; post++)
        {
            float x = -4.6f + post * 1.84f;
            CreateStaticPrimitive(context.environment, "RescuePerimeterPost_" + post, PrimitiveType.Cylinder,
                new Vector3(x, 0.62f, 1.68f), new Vector3(0.07f, 0.62f, 0.07f),
                context.materials.dark, false);
        }
        for (int mastIndex = 0; mastIndex < 2; mastIndex++)
        {
            float x = mastIndex == 0 ? -4.25f : 4.25f;
            CreateStaticPrimitive(context.environment, "SearchLightMast_" + mastIndex, PrimitiveType.Cylinder,
                new Vector3(x, 1.55f, 0.35f), new Vector3(0.09f, 1.55f, 0.09f),
                context.materials.dark, false);
            CreateStaticPrimitive(context.environment, "SearchLightHead_" + mastIndex, PrimitiveType.Cylinder,
                new Vector3(x, 3.08f, 0.35f), new Vector3(0.38f, 0.22f, 0.38f),
                context.materials.amber, false, new Vector3(90f, mastIndex == 0 ? 25f : -25f, 0f));
        }
        for (int marker = 0; marker < 7; marker++)
        {
            CreateStaticPrimitive(context.environment, "PerimeterTape_" + marker, PrimitiveType.Cube,
                new Vector3(-3.6f + marker * 1.2f, 0.9f, 1.75f),
                new Vector3(0.72f, 0.07f, 0.08f),
                marker % 2 == 0 ? context.materials.amber : context.materials.dark,
                false, new Vector3(0f, 0f, marker % 2 == 0 ? 3f : -3f));
        }
        for (int rebar = 0; rebar < 7; rebar++)
        {
            CreateStaticPrimitive(context.environment, "ExposedRebar_" + rebar, PrimitiveType.Cylinder,
                new Vector3(-1.55f + rebar * 0.55f, 0.75f + (rebar % 3) * 0.42f, 3.35f),
                new Vector3(0.035f, 1.1f + (rebar % 2) * 0.35f, 0.035f),
                ScopedMaterial(context, "Rebar", new Color32(62, 55, 51, 255), 0.36f),
                false, new Vector3(62f + rebar * 3f, rebar * 21f, 12f));
        }
        for (int cone = 0; cone < 4; cone++)
        {
            StoryChapterBuilderCommon.InstantiateAsset(
                CityRoot + "/Prop_RoadCone_0" + (cone % 3 + 1) + ".prefab",
                "RescueCone_" + cone,
                context.environment,
                new Vector3(-3.6f + cone * 2.4f, 0f, 1.45f),
                new Vector3(0.42f, 0.72f, 0.42f),
                Vector3.zero,
                false);
        }
        GameObject rescueVehicle = StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Veh_Firetruck_01.fbx", "RescueSupportVehicle", context.environment,
            new Vector3(4.7f, 0.05f, 4.45f), new Vector3(2.0f, 1.65f, 4.15f),
            new Vector3(0f, -42f, 0f), false);
        StyleVehicle(
            context,
            rescueVehicle,
            ScopedMaterial(context, "RescueVehicleBody", new Color32(149, 51, 46, 255), 0.44f),
            "RescueVehicleGlass");
        AddVehicleRoofAccent(context, rescueVehicle, "RescueLightBar", context.materials.danger);
    }

    private static void BuildWaveformDisplay(SceneContext context, Transform parent)
    {
        Color[] colors =
        {
            new Color32(79, 202, 231, 255),
            new Color32(247, 169, 61, 255),
            new Color32(216, 73, 67, 255)
        };
        float[][] patterns =
        {
            new[] { 0.2f, 0.45f, 0.2f, 0.45f, 0.2f, 0.45f },
            new[] { 0.18f, 0.95f, 0.18f, 0.95f, 0.18f, 0.95f },
            new[] { 0.15f, 0.35f, 0.7f, 0.55f, 0.4f, 0.22f }
        };
        string[] names = { "WaterWave", "HumanTripleWave", "MetalCreakWave" };
        for (int row = 0; row < 3; row++)
        {
            Material material = GetOrCreateMaterial("Waveform_" + names[row], colors[row], 0.25f, true);
            for (int column = 0; column < patterns[row].Length; column++)
            {
                CreateStaticPrimitive(parent, names[row] + "Bar_" + column, PrimitiveType.Cube,
                    new Vector3(-3.55f + column * 0.34f, 1.42f + row * 0.18f, -0.18f),
                    new Vector3(0.18f, patterns[row][column] * 0.5f, 0.08f), material, false);
            }
        }
    }

    private static AudioSource CreateSpatialSource(
        SceneContext context,
        string name,
        Vector3 position,
        string clipName,
        float volume)
    {
        AudioSource source = StoryChapterBuilderCommon.CreateSpatialAudioSource(
            name,
            context.audioRoot,
            position,
            StoryChapterBuilderCommon.LoadLicensedSfx(clipName),
            volume,
            1f,
            0.8f,
            16f);
        source.outputAudioMixerGroup = context.audio.sfxGroup;
        source.playOnAwake = false;
        return source;
    }

    private static void BuildHub(bool showDialog)
    {
        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MaterialPack materials = CreateMaterials();
        AudioSetup audio = EnsureAudioMixer();
        SceneContext context = new SceneContext
        {
            scene = scene,
            scenePath = MinigameSceneCatalog.HubPath,
            id = "minigame-hub",
            title = "MİNİ OYUNLAR",
            materials = materials,
            audio = audio,
            environment = new GameObject("Environment").transform,
            characters = new GameObject("Characters").transform,
            gameplay = new GameObject("Gameplay").transform,
            cameras = new GameObject("Cameras").transform,
            lightingVfx = new GameObject("LightingVFX").transform,
            audioRoot = new GameObject("Audio").transform,
            uiRoot = new GameObject("UI").transform
        };
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        context.camera = BuildCamera(
            context.cameras,
            new Vector3(0f, 4.4f, -11.8f),
            new Vector3(0f, 1.75f, 1.2f),
            new Color32(8, 19, 36, 255),
            "Minigame_Hub");
        BuildLighting(context, true);
        BuildHubDiorama(context);

        MinigameProgressManager progress = context.gameplay.gameObject.AddComponent<MinigameProgressManager>();
        MinigameHubManager manager = context.gameplay.gameObject.AddComponent<MinigameHubManager>();
        HubUi hubUi = BuildHubUi(context, manager);
        SetField(manager, "progressManager", progress);
        SetField(manager, "cards", hubUi.cards);
        SetField(manager, "totalCoinText", hubUi.totalCoinText);
        SetField(manager, "mainMenuSceneName", "Story_Rebuild_MainMenu");

        MarkEnvironmentStatic(context.environment);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MinigameSceneCatalog.HubPath);
        MinigameSceneCatalog.PublishBuildSettings();
        AssetDatabase.SaveAssets();
        ValidateHub(false);
        MinigameHubManager savedManager = Object.FindFirstObjectByType<MinigameHubManager>(FindObjectsInactive.Include);
        Selection.activeGameObject = savedManager != null ? savedManager.gameObject : null;
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Minigames", "Sekiz kartlı polished minigame merkezi üretildi.", "Tamam");
    }

    private static void BuildHubDiorama(SceneContext context)
    {
        CreateStaticPrimitive(context.environment, "CommandRoomFloor", PrimitiveType.Cube,
            new Vector3(0f, -0.12f, 1f), new Vector3(13f, 0.24f, 10f), context.materials.dark);
        CreateStaticPrimitive(context.environment, "CommandRoomBack", PrimitiveType.Cube,
            new Vector3(0f, 3.8f, 5.6f), new Vector3(13f, 7.6f, 0.25f), context.materials.navy);
        CreateStaticPrimitive(context.environment, "CommandRoomLowerRail", PrimitiveType.Cube,
            new Vector3(0f, 1.45f, 5.38f), new Vector3(11.8f, 0.08f, 0.1f),
            ScopedMaterial(context, "HubRail", new Color32(54, 114, 126, 255), 0.28f), false);
        CreateStaticPrimitive(context.environment, "CommandRoomUpperRail", PrimitiveType.Cube,
            new Vector3(0f, 4.45f, 5.36f), new Vector3(8.8f, 0.055f, 0.08f),
            ScopedMaterial(context, "HubAmberRail", new Color32(170, 118, 43, 255), 0.26f), false);

        int monitorCount = HubCardSpecs().Length;
        for (int index = 0; index < monitorCount; index++)
        {
            float x = (index - (monitorCount - 1) * 0.5f) * 1.35f;
            CreateStaticPrimitive(context.environment, "MissionMonitorFrame_" + (index + 1), PrimitiveType.Cube,
                new Vector3(x, 2.95f, 5.36f), new Vector3(1.05f, 1.55f, 0.11f),
                ScopedMaterial(context, "HubMonitorFrame", new Color32(48, 62, 70, 255), 0.5f), false);
            CreateStaticPrimitive(context.environment, "MissionMonitorGlow_" + (index + 1), PrimitiveType.Cube,
                new Vector3(x, 2.95f, 5.28f), new Vector3(0.82f, 1.28f, 0.045f),
                index % 2 == 0
                    ? ScopedMaterial(context, "HubMonitorCyan", new Color32(22, 80, 93, 255), 0.34f)
                    : ScopedMaterial(context, "HubMonitorAmber", new Color32(100, 72, 35, 255), 0.32f),
                false);
        }

        GameObject coin = CreateStaticPrimitive(context.environment, "IMO_Coin_Sculpture", PrimitiveType.Cylinder,
            new Vector3(0f, 1.15f, 4.72f), new Vector3(0.72f, 0.12f, 0.72f), context.materials.amber, false,
            new Vector3(90f, 0f, 0f));
        CreateStaticPrimitive(coin.transform, "CoinInset", PrimitiveType.Cylinder,
            coin.transform.position + new Vector3(0f, 0f, -0.13f), new Vector3(0.46f, 0.08f, 0.46f),
            context.materials.navy, false, new Vector3(90f, 0f, 0f));
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Work_Table_06.prefab", "HubCommandTable", context.environment,
            new Vector3(0f, 0f, 1.8f), new Vector3(4.8f, 1.18f, 1.7f),
            new Vector3(0f, 180f, 0f), false);
    }

    private static HubUi BuildHubUi(SceneContext context, MinigameHubManager manager)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);
        GameObject canvasObject = new GameObject(
            "MinigameHubCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(context.uiRoot, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = StoryChapterBuilderCommon.CreateUIRect(
            "SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();
        Image veil = safeArea.AddComponent<Image>();
        veil.color = new Color32(5, 16, 31, 126);
        veil.raycastTarget = false;

        GameObject header = StoryChapterBuilderCommon.CreatePanel(
            "HubHeader", safeArea.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -160f), new Vector2(980f, 250f), Color.white, false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        TMP_Text eyebrow = StoryChapterBuilderCommon.CreateText(
            "Eyebrow", header.transform, semibold, 24f, new Color32(99, 224, 233, 255),
            TextAlignmentOptions.Center, new Vector2(0f, 74f), new Vector2(860f, 40f));
        eyebrow.text = "DEPREME HAZIRLIK • 8 SİNEMATİK GÖREV";
        TMP_Text title = StoryChapterBuilderCommon.CreateText(
            "Title", header.transform, bold, 50f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 12f), new Vector2(880f, 76f));
        title.text = "MİNİ OYUNLAR";
        TMP_Text total = StoryChapterBuilderCommon.CreateText(
            "TotalCoin", header.transform, bold, 31f, new Color32(255, 195, 78, 255),
            TextAlignmentOptions.Center, new Vector2(0f, -72f), new Vector2(820f, 52f));
        total.text = "0 / 400 İMO COIN";

        Button back = StoryChapterBuilderCommon.CreateButton(
            "BackButton", safeArea.transform, "ANA MENÜ",
            bold, new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(620f, 100f),
            StoryChapterBuilderCommon.Teal, Color.white);
        UnityEventTools.AddPersistentListener(back.onClick, manager.ReturnToMainMenu);

        GameObject viewportObject = StoryChapterBuilderCommon.CreateUIRect(
            "CardsViewport", safeArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
            new Vector2(0f, 12f), new Vector2(1010f, -445f));
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color32(0, 0, 0, 3);
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();

        HubCardSpec[] specs = HubCardSpecs();
        float contentHeight = 470f + Mathf.Max(0, specs.Length - 1) * 365f;
        GameObject contentObject = StoryChapterBuilderCommon.CreateUIRect(
            "CardsContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, new Vector2(0f, contentHeight));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        ScrollRect scroll = viewportObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 50f;

        MinigameHubCardBinding[] bindings = new MinigameHubCardBinding[specs.Length];
        for (int index = 0; index < specs.Length; index++)
        {
            HubCardSpec spec = specs[index];
            float y = -230f - index * 365f;
            bindings[index] = BuildHubCard(content, manager, spec, new Vector2(0f, y), regular, semibold, bold);
        }

        return new HubUi { cards = bindings, totalCoinText = total };
    }

    private static MinigameHubCardBinding BuildHubCard(
        Transform parent,
        MinigameHubManager manager,
        HubCardSpec spec,
        Vector2 position,
        TMP_FontAsset regular,
        TMP_FontAsset semibold,
        TMP_FontAsset bold)
    {
        GameObject card = StoryChapterBuilderCommon.CreatePanel(
            spec.id + "_Card", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            position, new Vector2(930f, 338f), Color.white, false,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);
        GameObject thumbObject = StoryChapterBuilderCommon.CreateUIRect(
            "SceneCapture", card.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-345f, 0f), new Vector2(168f, 298f));
        Image thumbnail = thumbObject.AddComponent<Image>();
        thumbnail.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ThumbnailRoot}/{spec.id}.png");
        thumbnail.color = thumbnail.sprite != null ? Color.white : new Color32(17, 46, 70, 255);
        thumbnail.preserveAspect = true;
        thumbnail.raycastTarget = false;
        GameObject captureFrame = StoryChapterBuilderCommon.CreateUIRect(
            "CaptureFrame", thumbObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image captureFrameImage = captureFrame.AddComponent<Image>();
        captureFrameImage.sprite = StoryChapterBuilderCommon.LoadCasualUISprite("Blue", "Normal");
        captureFrameImage.type = Image.Type.Sliced;
        captureFrameImage.color = new Color32(91, 224, 233, 135);
        captureFrameImage.raycastTarget = false;

        TMP_Text name = StoryChapterBuilderCommon.CreateText(
            "GameTitle", card.transform, bold, 31f, Color.white,
            TextAlignmentOptions.Left, new Vector2(100f, 103f), new Vector2(620f, 70f));
        name.enableAutoSizing = true;
        name.fontSizeMin = 23f;
        name.fontSizeMax = 31f;
        name.text = spec.title;
        TMP_Text result = StoryChapterBuilderCommon.CreateText(
            "ResultLabel", card.transform, regular, 20f, new Color32(143, 188, 211, 255),
            TextAlignmentOptions.Left, new Vector2(100f, 54f), new Vector2(620f, 34f));
        result.text = "İLK GÖREV HAZIR";
        TMP_Text stars = StoryChapterBuilderCommon.CreateText(
            "Stars", card.transform, bold, 30f, new Color32(255, 191, 72, 255),
            TextAlignmentOptions.Left, new Vector2(-52f, 2f), new Vector2(300f, 46f));
        stars.text = "0 / 3 YILDIZ";
        TMP_Text coins = StoryChapterBuilderCommon.CreateText(
            "Coins", card.transform, semibold, 21f, new Color32(98, 231, 215, 255),
            TextAlignmentOptions.Right, new Vector2(275f, 2f), new Vector2(250f, 42f));
        coins.text = "0 / 50 İMO";
        TMP_Text time = StoryChapterBuilderCommon.CreateText(
            "BestTime", card.transform, semibold, 20f, new Color32(219, 229, 235, 255),
            TextAlignmentOptions.Left, new Vector2(-52f, -47f), new Vector2(300f, 38f));
        time.text = "--:--";
        Button play = StoryChapterBuilderCommon.CreateButton(
            "PlayButton", card.transform, "OYNA", bold, Vector2.one * 0.5f,
            new Vector2(230f, -104f), new Vector2(350f, 78f), StoryChapterBuilderCommon.Amber,
            StoryChapterBuilderCommon.Navy);
        UnityEventTools.AddStringPersistentListener(play.onClick, manager.OpenScene, spec.sceneName);

        return new MinigameHubCardBinding
        {
            minigameId = spec.id,
            sceneName = spec.sceneName,
            playButton = play,
            resultText = result,
            starsText = stars,
            coinText = coins,
            timeText = time
        };
    }

    private static HubCardSpec[] HubCardSpecs()
    {
        return new[]
        {
            new HubCardSpec("firetruck-runner", "İTFAİYE ACİL KOŞU", "Story_04_FiretruckRunner", MinigameSceneCatalog.FiretruckPath),
            new HubCardSpec("firefighter-extinguish", "ALEVE SU TUT", "Minigame_FirefighterExtinguish", MinigameSceneCatalog.FirefighterExtinguishPath),
            new HubCardSpec("evacuation-25d", "GÜVENLİ TAHLİYE 2.5D", "Minigame_Evacuation_25D", MinigameSceneCatalog.Evacuation25DPath),
            new HubCardSpec("aftershock-cover", "ARTÇI! ÇÖK–KAPAN–TUTUN", "Minigame_AftershockCover", MinigameSceneCatalog.AftershockCoverPath),
            new HubCardSpec("room-safety", "GÜVENLİ ODA CHALLENGE", "Minigame_RoomSafety", MinigameSceneCatalog.RoomSafetyPath),
            new HubCardSpec("emergency-bag-rush", "ÇANTA 30", "Minigame_EmergencyBagRush", MinigameSceneCatalog.EmergencyBagRushPath),
            new HubCardSpec("emergency-corridor", "ACİL KORİDOR", "Minigame_EmergencyCorridor", MinigameSceneCatalog.EmergencyCorridorPath),
            new HubCardSpec("rubble-signal", "ENKAZDA SİNYAL", "Minigame_RubbleSignal", MinigameSceneCatalog.RubbleSignalPath)
        };
    }

    private static void IntegrateEvacuation25DInPlace()
    {
        Scene scene = EditorSceneManager.OpenScene(MinigameSceneCatalog.Evacuation25DPath, OpenSceneMode.Single);
        MinigameSessionManager reporter = Object.FindObjectsByType<MinigameSessionManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.MinigameId == "evacuation-25d");
        if (reporter == null)
        {
            GameObject owner = new GameObject("_MinigameResultReporter");
            MinigameProgressManager progress = owner.AddComponent<MinigameProgressManager>();
            reporter = owner.AddComponent<MinigameSessionManager>();
            SetField(reporter, "minigameId", "evacuation-25d");
            SetField(reporter, "displayName", "GÜVENLİ TAHLİYE 2.5D");
            SetField(reporter, "externalResultOnly", true);
            SetField(reporter, "progressManager", progress);
        }

        StairChoiceManager[] choices = Object.FindObjectsByType<StairChoiceManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StairChoiceManager choice in choices)
        {
            choice.onWrongChoice ??= new UnityEngine.Events.UnityEvent();
            AddListenerOnce(choice.onWrongChoice, reporter, nameof(MinigameSessionManager.ReportExternalMistake),
                reporter.ReportExternalMistake);
            EditorUtility.SetDirty(choice);
        }

        StoryInteractable assembly = Object.FindObjectsByType<StoryInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.InteractionId == "evac25d.street.15.assembly");
        if (assembly == null)
            throw new InvalidOperationException("2.5D final toplanma etkileşimi bulunamadı; sahne korunarak entegre edilemedi.");
        AddListenerOnce(assembly.OnInteracted, reporter,
            nameof(MinigameSessionManager.CompleteExternalEvacuation25D), reporter.CompleteExternalEvacuation25D);
        EditorUtility.SetDirty(assembly);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddListenerOnce(
        UnityEngine.Events.UnityEvent source,
        Object target,
        string methodName,
        UnityEngine.Events.UnityAction action)
    {
        for (int index = 0; index < source.GetPersistentEventCount(); index++)
        {
            if (source.GetPersistentTarget(index) == target && source.GetPersistentMethodName(index) == methodName)
                return;
        }
        UnityEventTools.AddPersistentListener(source, action);
    }

    private static void CaptureAllThumbnails()
    {
        EnsureFolders();
        HubCardSpec[] specs = HubCardSpecs();
        for (int index = 0; index < specs.Length; index++)
            CaptureScene(specs[index].scenePath, $"{ThumbnailRoot}/{specs[index].id}.png", 540, 960, true);

        string qaRoot = "Assets/Screenshots/MinigameQA";
        EnsureFolder("Assets/Screenshots");
        EnsureFolder(qaRoot);
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { qaRoot }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        HubCardSpec[] authored = specs.Skip(3).ToArray();
        Vector2Int[] resolutions =
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(720, 1280),
            new Vector2Int(1440, 2560)
        };
        foreach (HubCardSpec spec in authored)
            CaptureEveryAuthoredStage(spec, qaRoot, resolutions);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void CaptureEveryAuthoredStage(
        HubCardSpec spec,
        string qaRoot,
        Vector2Int[] resolutions)
    {
        EditorSceneManager.OpenScene(spec.scenePath, OpenSceneMode.Single);
        Camera camera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderByDescending(candidate => candidate.depth)
            .FirstOrDefault();
        MinigameSessionManager session = Object.FindFirstObjectByType<MinigameSessionManager>(FindObjectsInactive.Include);
        if (camera == null || session == null)
            throw new InvalidOperationException("Aşama QA kamera/session bulunamadı: " + spec.scenePath);
        MinigameStageDefinition[] stages = GetFieldValue<MinigameStageDefinition[]>(session, "stages");
        TMP_Text objective = GetFieldValue<TMP_Text>(session, "objectiveText");
        TMP_Text subtitle = GetFieldValue<TMP_Text>(session, "subtitleText");
        TMP_Text stageLabel = GetFieldValue<TMP_Text>(session, "stageText");
        TMP_Text timer = GetFieldValue<TMP_Text>(session, "timerText");
        TMP_Text title = GetFieldValue<TMP_Text>(session, "titleText");
        TMP_Text score = GetFieldValue<TMP_Text>(session, "scoreText");
        TMP_Text gestureVerb = GetFieldValue<TMP_Text>(session, "gestureVerbText");
        TMP_Text gestureDetail = GetFieldValue<TMP_Text>(session, "gestureDetailText");
        TMP_Text gestureProgress = GetFieldValue<TMP_Text>(session, "gestureProgressText");
        TMP_Text gestureArrow = GetFieldValue<TMP_Text>(session, "gestureArrowText");
        Image gestureIcon = GetFieldValue<Image>(session, "gestureIconImage");
        TMP_Text speakerBadge = GetFieldValue<TMP_Text>(session, "speakerBadgeText");
        CanvasGroup gestureGroup = GetFieldValue<CanvasGroup>(session, "gestureCoachGroup");
        CanvasGroup feedbackGroup = GetFieldValue<CanvasGroup>(session, "feedbackGroup");
        GameObject resultPanel = GetFieldValue<GameObject>(session, "resultPanel");
        if (title != null)
            title.text = GetFieldValue<string>(session, "displayName");
        if (score != null)
            score.text = "1000 PUAN";
        if (feedbackGroup != null)
            feedbackGroup.alpha = 0f;
        if (resultPanel != null)
            resultPanel.SetActive(false);

        for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
        {
            MinigameStageDefinition stage = stages[stageIndex];
            if (stage.cameraPose != null)
            {
                camera.transform.SetPositionAndRotation(stage.cameraPose.position, stage.cameraPose.rotation);
                camera.fieldOfView = stage.cameraFieldOfView;
            }
            for (int index = 0; index < stages.Length; index++)
            {
                if (stages[index].stageRoot != null)
                    stages[index].stageRoot.SetActive(index == stageIndex);
                SetQaGuides(stages[index], false);
            }
            SetQaGuides(stage, stage.showGuidesOnEnter);
            ConfigureQaCharacterCues(stages, stage, camera);
            if (objective != null)
                objective.text = stage.objective;
            if (subtitle != null)
                subtitle.text = stage.subtitle;
            if (stageLabel != null)
                stageLabel.text = $"AŞAMA {stageIndex + 1} / {stages.Length}";
            if (timer != null)
                timer.text = stage.stageTimeLimitSeconds > 0f
                    ? $"SÜRE {Mathf.CeilToInt(stage.stageTimeLimitSeconds):00}"
                    : "SÜRE --";
            if (gestureVerb != null)
                gestureVerb.text = QaGestureVerb(stage.gesture);
            if (gestureDetail != null)
                gestureDetail.text = stage.gestureInstruction;
            if (gestureProgress != null)
                gestureProgress.text = $"0/{Mathf.Max(1, stage.requiredSuccesses)} HEDEF";
            if (gestureArrow != null)
            {
                gestureArrow.text = QaGestureSymbol(stage.gesture);
                gestureArrow.enabled = false;
            }
            if (gestureIcon != null)
            {
                gestureIcon.sprite = QaGestureSprite(session, stage.gesture);
                gestureIcon.enabled = gestureIcon.sprite != null;
            }
            if (gestureGroup != null)
                gestureGroup.alpha = 1f;
            if (speakerBadge != null)
                speakerBadge.text = QaSpeaker(stage.subtitle);
            stage.onEnter?.Invoke();
            ConfigureQaStagePose(spec.id, stageIndex);
            Canvas.ForceUpdateCanvases();

            foreach (Vector2Int resolution in resolutions)
            {
                string safeStage = Sanitize(stage.stageId).ToLowerInvariant();
                CaptureCurrentCamera(
                    camera,
                    $"{qaRoot}/{spec.id}_{stageIndex + 1:00}_{safeStage}_{resolution.x}x{resolution.y}.png",
                    resolution.x,
                    resolution.y,
                    false,
                    true);
            }
        }
    }

    private static void SetQaGuides(MinigameStageDefinition stage, bool visible)
    {
        if (stage?.actions == null)
            return;

        for (int index = 0; index < stage.actions.Length; index++)
        {
            MinigameActionDefinition action = stage.actions[index];
            if (action?.highlightRoot != null)
                action.highlightRoot.SetActive(false);
            if (action?.targetHighlightRoot != null)
                action.targetHighlightRoot.SetActive(false);
        }

        if (!visible)
            return;

        for (int index = 0; index < stage.actions.Length; index++)
        {
            MinigameActionDefinition action = stage.actions[index];
            if (action == null || !action.isCorrect)
                continue;

            if (action.highlightRoot != null)
                action.highlightRoot.SetActive(true);
            if (action.targetHighlightRoot != null)
                action.targetHighlightRoot.SetActive(true);
            break;
        }
    }

    private static void ConfigureQaCharacterCues(
        MinigameStageDefinition[] allStages,
        MinigameStageDefinition activeStage,
        Camera camera)
    {
        if (allStages == null || camera == null)
            return;
        for (int stageIndex = 0; stageIndex < allStages.Length; stageIndex++)
        {
            MinigameCharacterCue[] cues = allStages[stageIndex]?.characterCues;
            if (cues == null)
                continue;
            for (int cueIndex = 0; cueIndex < cues.Length; cueIndex++)
            {
                Transform root = cues[cueIndex]?.characterRoot;
                if (root != null)
                    root.gameObject.SetActive(false);
            }
        }
        if (activeStage?.characterCues == null)
            return;
        for (int cueIndex = 0; cueIndex < activeStage.characterCues.Length; cueIndex++)
        {
            MinigameCharacterCue cue = activeStage.characterCues[cueIndex];
            if (cue?.characterRoot == null)
                continue;
            if (cue.useStagePosition)
                cue.characterRoot.position = cue.stagePosition;
            cue.characterRoot.gameObject.SetActive(true);
            if (!cue.faceCamera)
                continue;
            Vector3 direction = camera.transform.position - cue.characterRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                cue.characterRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) *
                                             Quaternion.Euler(0f, cue.modelYawOffset, 0f);
        }
    }

    private static string QaGestureVerb(MinigameGesture gesture)
    {
        return gesture switch
        {
            MinigameGesture.RepeatedTap => "RİTİMLE DOKUN",
            MinigameGesture.SwipeDown => "AŞAĞI KAYDIR",
            MinigameGesture.SwipeHorizontal => "YATAY KAYDIR",
            MinigameGesture.Hold => "BASILI TUT",
            MinigameGesture.DragToTarget => "SÜRÜKLE • BIRAK",
            _ => "DOKUN"
        };
    }

    private static string QaGestureSymbol(MinigameGesture gesture)
    {
        return gesture switch
        {
            MinigameGesture.RepeatedTap => "•••",
            MinigameGesture.SwipeDown => "↓",
            MinigameGesture.SwipeHorizontal => "↔",
            MinigameGesture.Hold => "●",
            MinigameGesture.DragToTarget => "→",
            _ => "●"
        };
    }

    private static Sprite QaGestureSprite(MinigameSessionManager session, MinigameGesture gesture)
    {
        string field = gesture switch
        {
            MinigameGesture.RepeatedTap => "repeatedTapGestureSprite",
            MinigameGesture.SwipeDown => "swipeDownGestureSprite",
            MinigameGesture.SwipeHorizontal => "swipeHorizontalGestureSprite",
            MinigameGesture.Hold => "holdGestureSprite",
            MinigameGesture.DragToTarget => "dragGestureSprite",
            _ => "tapGestureSprite"
        };
        return GetFieldValue<Sprite>(session, field);
    }

    private static string QaSpeaker(string subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
            return "REHBER";
        int separator = subtitle.IndexOf(':');
        return separator > 0 && separator <= 18
            ? subtitle.Substring(0, separator).Trim().ToUpperInvariant()
            : "REHBER";
    }

    private static void ConfigureQaStagePose(string minigameId, int stageIndex)
    {
        if (minigameId == "aftershock-cover")
        {
            bool darkRound = stageIndex >= 3 && stageIndex <= 5;
            SetQaObjectActive("DarkAftershockScreenTint", darkRound);
            SetQaObjectActive("Amber Practical Light", !darkRound);
            SetQaObjectActive("Key Directional Light", !darkRound);
            return;
        }

        if (minigameId == "emergency-bag-rush")
        {
            bool blackout = stageIndex == 1;
            SetQaObjectActive("BlackoutScreenTint", blackout);
            SetQaObjectActive("Key Directional Light", !blackout);
            SetQaObjectActive("Cyan Fill Light", !blackout);
            SetQaObjectActive("BlackoutFlashlightGlow", false);
            return;
        }

        if (minigameId == "rubble-signal")
        {
            SetQaObjectActive("VerifiedSafeVoidMarker", stageIndex == 6);
            return;
        }

        if (minigameId != "emergency-corridor" || stageIndex != 6)
            return;

        GameObject firetruck = FindQaObject("EmergencyFiretruck");
        if (firetruck == null)
            return;
        firetruck.SetActive(true);
        firetruck.transform.position = new Vector3(0f, 0.12f, 2.65f);
        firetruck.transform.rotation = Quaternion.identity;
        SetQaObjectActive("BlockedCarA", false);
        SetQaObjectActive("BlockedCarB", false);
    }

    private static GameObject FindQaObject(string objectName)
    {
        Transform match = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == objectName);
        return match != null ? match.gameObject : null;
    }

    private static void SetQaObjectActive(string objectName, bool active)
    {
        GameObject target = FindQaObject(objectName);
        if (target != null)
            target.SetActive(active);
    }

    private static void CaptureHubQa()
    {
        string qaRoot = "Assets/Screenshots/MinigameQA";
        Vector2Int[] resolutions =
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(720, 1280),
            new Vector2Int(1440, 2560)
        };
        foreach (Vector2Int resolution in resolutions)
        {
            CaptureScene(
                MinigameSceneCatalog.HubPath,
                $"{qaRoot}/minigame-hub_{resolution.x}x{resolution.y}.png",
                resolution.x,
                resolution.y,
                false,
                true);
        }
    }

    private static void CaptureScene(
        string scenePath,
        string outputPath,
        int width,
        int height,
        bool thumbnail,
        bool includeOverlayUi = false)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            throw new InvalidOperationException("Capture sahnesi bulunamadı: " + scenePath);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Camera camera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderByDescending(candidate => candidate.depth)
            .FirstOrDefault();
        if (camera == null)
            throw new InvalidOperationException("Capture kamerası bulunamadı: " + scenePath);

        CaptureCurrentCamera(camera, outputPath, width, height, thumbnail, includeOverlayUi);
    }

    private static void CaptureCurrentCamera(
        Camera camera,
        string outputPath,
        int width,
        int height,
        bool thumbnail,
        bool includeOverlayUi)
    {
        List<CanvasCaptureState> canvasStates = new List<CanvasCaptureState>();
        if (includeOverlayUi)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;
                canvasStates.Add(new CanvasCaptureState(canvas));
            }
        }

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        float previousAspect = camera.aspect;
        int previousCullingMask = camera.cullingMask;
        const int captureUiLayer = 31;
        int captureUiMask = 1 << captureUiLayer;
        List<Transform> uiTransforms = new List<Transform>();
        List<int> uiLayers = new List<int>();
        HashSet<int> capturedTransformIds = new HashSet<int>();
        GameObject uiCameraObject = null;
        RenderTexture uiRenderTexture = null;
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
        try
        {
            camera.aspect = width / (float)height;
            camera.targetTexture = renderTexture;
            // Direct Camera.Render does not run the normal Game View canvas positioning pass.
            // Rebind every active screen-space canvas after the authored stage camera moves;
            // otherwise TMP meshes can retain the preceding camera plane and be back-face culled.
            for (int index = 0; index < canvasStates.Count; index++)
                canvasStates[index].RebindForCapture(camera);
            for (int canvasIndex = 0; canvasIndex < canvasStates.Count; canvasIndex++)
            {
                Transform[] hierarchy = canvasStates[canvasIndex].Canvas.GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < hierarchy.Length; transformIndex++)
                {
                    Transform target = hierarchy[transformIndex];
                    if (target == null || !capturedTransformIds.Add(target.GetInstanceID()))
                        continue;
                    uiTransforms.Add(target);
                    uiLayers.Add(target.gameObject.layer);
                    target.gameObject.layer = captureUiLayer;
                }
            }
            camera.cullingMask = previousCullingMask & ~captureUiMask;
            Canvas.ForceUpdateCanvases();
            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            RefreshQaTextMeshes(texts);
            Canvas.ForceUpdateCanvases();
            // Warm the camera/canvas render graph once. Direct editor rendering can otherwise
            // reuse a stale TMP CanvasRenderer batch after a stage camera or aspect change.
            camera.Render();
            RefreshQaTextMeshes(texts);
            Canvas.ForceUpdateCanvases();
            uiCameraObject = new GameObject("QA UI Capture Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera uiCamera = uiCameraObject.AddComponent<Camera>();
            uiCamera.CopyFrom(camera);
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = Color.clear;
            uiCamera.cullingMask = captureUiMask;
            uiCamera.depth = camera.depth + 100f;
            uiCamera.allowHDR = false;
            uiRenderTexture = RenderTexture.GetTemporary(
                width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            uiCamera.targetTexture = uiRenderTexture;
            uiCamera.Render();
            RenderTexture.active = renderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, width, height, 0f);
            Graphics.DrawTexture(new Rect(0f, 0f, width, height), uiRenderTexture);
            GL.PopMatrix();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            string absolute = Path.Combine(
                Directory.GetCurrentDirectory(), outputPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? Directory.GetCurrentDirectory());
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            camera.cullingMask = previousCullingMask;
            for (int index = 0; index < uiTransforms.Count; index++)
            {
                if (uiTransforms[index] != null)
                    uiTransforms[index].gameObject.layer = uiLayers[index];
            }
            if (uiCameraObject != null)
                Object.DestroyImmediate(uiCameraObject);
            if (uiRenderTexture != null)
                RenderTexture.ReleaseTemporary(uiRenderTexture);
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(texture);
            for (int index = 0; index < canvasStates.Count; index++)
                canvasStates[index].Restore();
            if (canvasStates.Count > 0)
                Canvas.ForceUpdateCanvases();
        }

        if (!thumbnail)
            return;
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Thumbnail importer bulunamadı: " + outputPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static void RefreshQaTextMeshes(TMP_Text[] texts)
    {
        if (texts == null)
            return;
        for (int index = 0; index < texts.Length; index++)
        {
            TMP_Text target = texts[index];
            if (target == null || !target.gameObject.activeInHierarchy || !target.enabled)
                continue;
            target.SetAllDirty();
            target.ForceMeshUpdate(true, true);
            target.Rebuild(CanvasUpdate.PreRender);
        }
    }

    private static void ValidateAll(bool showDialog)
    {
        List<string> errors = new List<string>();
        string[] authoredPaths =
        {
            MinigameSceneCatalog.AftershockCoverPath,
            MinigameSceneCatalog.RoomSafetyPath,
            MinigameSceneCatalog.EmergencyBagRushPath,
            MinigameSceneCatalog.EmergencyCorridorPath,
            MinigameSceneCatalog.RubbleSignalPath
        };
        foreach (string path in authoredPaths)
        {
            try
            {
                ValidateSceneAtPath(path, false);
            }
            catch (Exception exception)
            {
                errors.Add(Path.GetFileNameWithoutExtension(path) + ": " + exception.Message);
            }
        }
        try
        {
            ValidateHub(false);
        }
        catch (Exception exception)
        {
            errors.Add("Minigame_Hub: " + exception.Message);
        }

        string[] buildPaths = EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).ToArray();
        if (!buildPaths.SequenceEqual(MinigameSceneCatalog.OrderedScenePaths))
            errors.Add("Build Settings merkezi katalog sırasıyla eşleşmiyor.");
        foreach (string path in MinigameSceneCatalog.OrderedScenePaths)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                errors.Add("Build sahnesi eksik: " + path);

        if (errors.Count > 0)
            throw new InvalidOperationException("Minigame paket doğrulaması başarısız:\n- " + string.Join("\n- ", errors));
        Debug.Log("Minigame paket doğrulaması başarılı: 6 yeni sahne, 8 kart ve 14 build sahnesi.");
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Minigames", "Tüm minigame doğrulamaları başarılı.", "Tamam");
    }

    private static void ValidateSceneAtPath(string path, bool showDialog)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            throw new InvalidOperationException("Sahne bulunamadı: " + path);
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        ValidateScene(scene, path);
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Minigames", scene.name + " doğrulaması başarılı.", "Tamam");
    }

    private static void ValidateScene(Scene scene, string path)
    {
        foreach (string requiredRoot in RequiredRoots)
        {
            int matches = scene.GetRootGameObjects().Count(root => root.name == requiredRoot);
            if (matches != 1)
                throw new InvalidOperationException($"{scene.name}: {requiredRoot} kökü tam bir kez bulunmalı; {matches} bulundu.");
        }

        MinigameSessionManager[] sessions = Object.FindObjectsByType<MinigameSessionManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (sessions.Length != 1 || sessions[0].ExternalResultOnly)
            throw new InvalidOperationException(scene.name + ": tek ve authored session manager gerekli.");
        MinigameSessionManager session = sessions[0];
        if (!session.ValidateConfiguration(out string error))
            throw new InvalidOperationException(scene.name + ": " + error);
        if (GetFieldValue<PlayableDirector>(session, "introTimeline") == null)
            throw new InvalidOperationException(scene.name + ": giriş Timeline referansı eksik.");

        Camera camera = GetFieldValue<Camera>(session, "worldCamera");
        if (camera == null || camera.GetComponent<UniversalAdditionalCameraData>() == null)
            throw new InvalidOperationException(scene.name + ": URP portre kamera sözleşmesi eksik.");
        MinigameStageDefinition[] stages = GetFieldValue<MinigameStageDefinition[]>(session, "stages");
        float previousAspect = camera.aspect;
        camera.aspect = 1080f / 1920f;
        try
        {
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                MinigameStageDefinition stage = stages[stageIndex];
                if (string.IsNullOrWhiteSpace(stage.subtitle) || stage.voiceClip == null)
                    throw new InvalidOperationException($"{scene.name}: {stage.stageId} ses/altyazı cue eksik.");
                foreach (MinigameActionDefinition action in stage.actions)
                {
                    if (action?.targetCollider == null)
                        throw new InvalidOperationException($"{scene.name}: {stage.stageId} Inspector hedefi eksik.");
                    Vector3 viewport = camera.WorldToViewportPoint(action.targetCollider.bounds.center);
                    if (viewport.z <= 0f || viewport.x < -0.18f || viewport.x > 1.18f ||
                        viewport.y < -0.1f || viewport.y > 1.1f)
                        throw new InvalidOperationException(
                            $"{scene.name}: {stage.stageId}/{action.actionId} portre kamera dışında ({viewport}).");
                    if (stage.gesture == MinigameGesture.DragToTarget && action.isCorrect && action.dragTarget == null)
                        throw new InvalidOperationException($"{scene.name}: {stage.stageId} sürükleme hedefi eksik.");
                }
            }
        }
        finally
        {
            camera.aspect = previousAspect;
        }

        string[] forbidden = { "Debug", "Placeholder", "TargetDisk", "GroundPad", "YellowSemicircle", "DragVolume" };
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
            if (renderer.enabled && forbidden.Any(token => renderer.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException(scene.name + ": görünür authoring placeholder bulundu: " + renderer.name);

        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Animator animator in animators)
            if (animator.applyRootMotion)
                throw new InvalidOperationException(scene.name + ": root motion açık bırakılmış: " + animator.name);
        Transform charactersRoot = scene.GetRootGameObjects()
            .Single(root => root.name == "Characters")
            .transform;
        foreach (Transform character in charactersRoot)
        {
            Renderer[] characterRenderers = character.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled)
                .ToArray();
            if (characterRenderers.Length == 0)
                continue;
            Bounds characterBounds = characterRenderers[0].bounds;
            foreach (Renderer renderer in characterRenderers.Skip(1))
                characterBounds.Encapsulate(renderer.bounds);
            float soleOffset = Mathf.Abs(characterBounds.min.y - character.position.y);
            if (soleOffset > 0.02f)
                throw new InvalidOperationException(
                    $"{scene.name}: {character.name} ayak-zemin sapması {soleOffset * 100f:0.0} cm; sınır 2 cm.");
        }
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (lights.Length > 3 || lights.Count(light => light.shadows != LightShadows.None) > 1)
            throw new InvalidOperationException(scene.name + ": mobil ışık bütçesi aşıldı.");
        if (Object.FindFirstObjectByType<StorySafeAreaPanel>(FindObjectsInactive.Include) == null)
            throw new InvalidOperationException(scene.name + ": UI SafeArea paneli eksik.");
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            throw new InvalidOperationException(scene.name + ": EventSystem eksik.");
    }

    private static void ValidateHub(bool showDialog)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MinigameSceneCatalog.HubPath) == null)
            throw new InvalidOperationException("Minigame_Hub sahnesi bulunamadı.");
        Scene scene = EditorSceneManager.OpenScene(MinigameSceneCatalog.HubPath, OpenSceneMode.Single);
        foreach (string requiredRoot in RequiredRoots)
            if (scene.GetRootGameObjects().Count(root => root.name == requiredRoot) != 1)
                throw new InvalidOperationException("Hub kök sözleşmesi eksik: " + requiredRoot);
        MinigameHubManager manager = Object.FindFirstObjectByType<MinigameHubManager>(FindObjectsInactive.Include);
        int expectedCardCount = HubCardSpecs().Length;
        if (manager == null || manager.CardCount != expectedCardCount)
            throw new InvalidOperationException("Hub tam sekiz açık kart içermiyor.");
        MinigameHubCardBinding[] cards = GetFieldValue<MinigameHubCardBinding[]>(manager, "cards");
        if (cards.Length != expectedCardCount || cards.Any(card => card == null || card.playButton == null || !card.playButton.interactable))
            throw new InvalidOperationException("Hub kart bağlantısı veya oynat düğmesi eksik.");
        foreach (HubCardSpec spec in HubCardSpecs())
            if (AssetDatabase.LoadAssetAtPath<Sprite>($"{ThumbnailRoot}/{spec.id}.png") == null)
                throw new InvalidOperationException("Gerçek sahne thumbnail'i eksik: " + spec.id);
        if (Object.FindFirstObjectByType<StorySafeAreaPanel>(FindObjectsInactive.Include) == null)
            throw new InvalidOperationException("Hub safe area eksik.");
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Minigames", "Hub doğrulaması başarılı.", "Tamam");
    }

    private static void MarkEnvironmentStatic(Transform root)
    {
        if (root == null)
            return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.GetComponentInParent<Animation>(true) != null)
                continue;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    private static void ConfigureCharacterAnimators(Transform root)
    {
        if (root == null)
            return;
        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            EditorUtility.SetDirty(animator);
        }
    }

    private static T GetFieldValue<T>(Object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " alanı bulunamadı.");
        return (T)field.GetValue(target);
    }

    [Serializable]
    private sealed class VoiceManifest
    {
        public string scene;
        public VoiceManifestEntry[] entries = Array.Empty<VoiceManifestEntry>();
    }

    [Serializable]
    private sealed class VoiceManifestEntry
    {
        public string id;
        public string subtitle;
    }

    private sealed class VoiceCue
    {
        public string subtitle;
        public AudioClip clip;
    }

    private sealed class SceneContext
    {
        internal Scene scene;
        internal string scenePath;
        internal string id;
        internal string title;
        internal string voiceFolder;
        internal Transform environment;
        internal Transform characters;
        internal Transform gameplay;
        internal Transform cameras;
        internal Transform lightingVfx;
        internal Transform audioRoot;
        internal Transform uiRoot;
        internal Camera camera;
        internal PlayableDirector intro;
        internal MinigameProgressManager progress;
        internal MinigameSessionManager session;
        internal GameplayHud hud;
        internal MaterialPack materials;
        internal AudioSetup audio;
        internal AudioSource voiceSource;
        internal AudioSource sfxSource;
    }

    private sealed class MaterialPack
    {
        internal Material navy;
        internal Material cream;
        internal Material cyan;
        internal Material amber;
        internal Material danger;
        internal Material safe;
        internal Material wood;
        internal Material wall;
        internal Material floor;
        internal Material concrete;
        internal Material asphalt;
        internal Material glass;
        internal Material dark;
        internal Material white;
    }

    private sealed class AudioSetup
    {
        internal AudioMixer mixer;
        internal AudioMixerGroup voiceGroup;
        internal AudioMixerGroup sfxGroup;
        internal AudioMixerGroup ambienceGroup;
        internal AudioMixerSnapshot gameplay;
        internal AudioMixerSnapshot voiceDucked;
        internal AudioMixerSnapshot searchSilence;
    }

    private sealed class GameplayHud
    {
        internal TMP_Text title;
        internal TMP_Text objective;
        internal TMP_Text subtitle;
        internal TMP_Text stage;
        internal TMP_Text score;
        internal TMP_Text timer;
        internal TMP_Text feedback;
        internal CanvasGroup feedbackGroup;
        internal RectTransform feedbackRect;
        internal Image holdProgress;
        internal CanvasGroup gestureCoachGroup;
        internal TMP_Text gestureVerb;
        internal TMP_Text gestureDetail;
        internal TMP_Text gestureProgress;
        internal RectTransform gestureMotionRoot;
        internal TMP_Text gestureArrow;
        internal Image gestureIcon;
        internal RectTransform safeAreaRect;
        internal CanvasGroup worldGestureGroup;
        internal RectTransform worldGestureRoot;
        internal Image worldGestureIcon;
        internal RectTransform worldGestureTrail;
        internal Image worldGestureTrailImage;
        internal TMP_Text nextActionText;
        internal TMP_Text speakerBadge;
        internal ResultHud result;
    }

    private sealed class ResultHud
    {
        internal GameObject root;
        internal TMP_Text title;
        internal TMP_Text detail;
        internal TMP_Text stars;
        internal TMP_Text coins;
    }

    private sealed class HubUi
    {
        internal MinigameHubCardBinding[] cards;
        internal TMP_Text totalCoinText;
    }

    private readonly struct CanvasCaptureState
    {
        private readonly Canvas canvas;
        private readonly RenderMode renderMode;
        private readonly Camera worldCamera;
        private readonly float planeDistance;
        private readonly bool wasEnabled;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 localScale;
        private readonly Vector2 sizeDelta;

        internal Canvas Canvas => canvas;

        internal CanvasCaptureState(Canvas canvas)
        {
            this.canvas = canvas;
            renderMode = canvas.renderMode;
            worldCamera = canvas.worldCamera;
            planeDistance = canvas.planeDistance;
            wasEnabled = canvas.enabled;
            RectTransform rect = canvas.transform as RectTransform;
            position = canvas.transform.position;
            rotation = canvas.transform.rotation;
            localScale = canvas.transform.localScale;
            sizeDelta = rect != null ? rect.sizeDelta : Vector2.zero;
        }

        internal void Restore()
        {
            if (canvas == null)
                return;
            canvas.enabled = false;
            canvas.renderMode = renderMode;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = planeDistance;
            canvas.transform.SetPositionAndRotation(position, rotation);
            canvas.transform.localScale = localScale;
            if (canvas.transform is RectTransform rect)
                rect.sizeDelta = sizeDelta;
            canvas.enabled = wasEnabled;
        }

        internal void RebindForCapture(Camera captureCamera)
        {
            if (canvas == null || captureCamera == null)
                return;
            canvas.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = captureCamera;
            RectTransform rect = canvas.transform as RectTransform;
            float referenceHeight = rect != null && rect.rect.height > 1f ? rect.rect.height : 1920f;
            // Keep the capture-only world canvas several metres out. At the normal
            // screen-space plane distance, narrow authored FOVs make TMP's editor
            // renderer cull glyph meshes because the resulting lossy scale is tiny.
            float distance = Mathf.Max(5f, captureCamera.nearClipPlane + 0.1f);
            float worldHeight = 2f * distance * Mathf.Tan(captureCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            canvas.transform.SetPositionAndRotation(
                captureCamera.transform.position + captureCamera.transform.forward * distance,
                captureCamera.transform.rotation);
            canvas.transform.localScale = Vector3.one * (worldHeight / referenceHeight);
            canvas.enabled = true;
        }
    }

    private readonly struct HubCardSpec
    {
        internal readonly string id;
        internal readonly string title;
        internal readonly string sceneName;
        internal readonly string scenePath;

        internal HubCardSpec(string id, string title, string sceneName, string scenePath)
        {
            this.id = id;
            this.title = title;
            this.sceneName = sceneName;
            this.scenePath = scenePath;
        }
    }
}
