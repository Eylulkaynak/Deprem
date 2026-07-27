using System;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class StoryRebuildFlowMenuBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_Rebuild_MainMenu.unity";
    private const string SharedHomePrefabPath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";
    private static readonly string[] PublishedScenePaths =
    {
        ScenePath,
        "Assets/Scenes/Story_01_RebuildPreview.unity",
        "Assets/Scenes/Story_02_RebuildPreview.unity",
        "Assets/Scenes/Story_03_RebuildPreview.unity",
        "Assets/Scenes/Story_04_RebuildPreview.unity"
    };

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Build Story Main Menu")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Build Story Main Menu (Silent)")]
    public static void BuildSilentFromMenu()
    {
        Build(false);
    }

    public static void Build(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Story ana menüsü Play Mode dışında üretilmelidir.");

        try
        {
            StoryChapterBuilderCommon.EnsureFolders();
            StoryAnimationLibraryBuilder.BuildLibrary(false);

            GameObject sharedHomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHomePrefabPath);
            if (sharedHomePrefab == null)
                throw new InvalidOperationException("Ortak aile evi prefabı bulunamadı: " + SharedHomePrefabPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_REBUILD_MAIN_MENU");

            Transform set = StoryChapterBuilderCommon.NewChild(root.transform, "MainMenuLivingRoom");
            GameObject home = (GameObject)PrefabUtility.InstantiatePrefab(sharedHomePrefab, set);
            home.name = "StoryHome_Shared_Menu";
            home.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            ConfigureHomeForMenu(home.transform);

            RuntimeAnimatorController childController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                StoryAnimationLibraryBuilder.ControllerPath);
            StoryChapterBuilderCommon.Characters family = StoryChapterBuilderCommon.BuildFamily(
                root.transform,
                childController,
                false,
                new Vector3(-0.72f, 0f, 0.35f),
                new Vector3(0.62f, 0f, 0.72f),
                Vector3.zero);
            family.deniz.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            family.can.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

            StoryChapterBuilderCommon.CameraSpec[] cameras =
            {
                new(
                    StoryCameraZoneId.RoomOverview,
                    "CM_MENU_FamilyRoom",
                    new Vector3(5.5f, 4.75f, -6.9f),
                    new Vector3(-0.15f, 0.92f, 0.55f),
                    43f)
            };
            StoryCameraController cameraController = StoryChapterBuilderCommon.BuildCameras(
                root.transform,
                StoryCameraZoneId.RoomOverview,
                cameras,
                out Camera mainCamera,
                out CinemachineBrain brain);
            StoryChapterBuilderCommon.SetReference(cameraController, "brain", brain);
            mainCamera.backgroundColor = new Color32(10, 17, 24, 255);

            StoryChapterBuilderCommon.BuildLighting(
                root.transform,
                StoryChapterBuilderCommon.CreateVolumeProfile(),
                new Color32(255, 224, 181, 255),
                1.08f);
            AudioClip roomTone = AssetDatabase.LoadAssetAtPath<AudioClip>(
                StoryChapterBuilderCommon.AudioRoot + "/calm_home.wav");
            if (roomTone != null)
                StoryChapterBuilderCommon.CreateAudioSource(
                    "MainMenuRoomTone",
                    root.transform,
                    roomTone,
                    0.055f,
                    true,
                    true);

            GameObject sessionObject = new GameObject("_StorySession_MainMenu");
            StoryGameManager manager = sessionObject.AddComponent<StoryGameManager>();
            StoryChapterBuilderCommon.ConfigureSession(
                manager,
                StoryAct.Preparation,
                Array.Empty<StoryFlag>());
            StoryChapterBuilderCommon.ConfigureRebuildStoryRoute(manager);
            SerializedObject managerData = new SerializedObject(manager);
            SerializedProperty freshStart = managerData.FindProperty("startFreshOnFirstEditorPlay");
            if (freshStart != null)
                freshStart.boolValue = false;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            BuildMenuUI(root.transform, manager);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PublishBuildRoute();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate(false);
            Selection.activeGameObject = root;

            Debug.Log("Story rebuild ana menüsü üretildi: " + ScenePath);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Hikâye ana menüsü üretildi.\nYeni dört perdelik akış Build Settings'te aktiftir.",
                    "Tamam");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", exception.Message, "Kapat");
            throw;
        }
    }

    [MenuItem("Tools/Deprem Story/Rebuild Preview/Validate Story Main Menu")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    public static void Validate(bool showDialog)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StoryGameManager manager = Object.FindFirstObjectByType<StoryGameManager>();
        if (manager == null)
            throw new InvalidOperationException("Ana menüde StoryGameManager bulunamadı.");
        if (Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("Ana menü tek ana kamera kullanmalıdır.");
        if (Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("Ana menü tek EventSystem kullanmalıdır.");

        Button continueButton = FindRequired("ContinueStoryButton").GetComponent<Button>();
        Button newStoryButton = FindRequired("StartNewStoryButton").GetComponent<Button>();
        if (continueButton == null || continueButton.onClick.GetPersistentEventCount() != 1)
            throw new InvalidOperationException("Devam Et düğmesi hikâye manager'ına bağlı değil.");
        if (newStoryButton == null || newStoryButton.onClick.GetPersistentEventCount() != 1)
            throw new InvalidOperationException("Yeni Hikâye düğmesi hikâye manager'ına bağlı değil.");
        string[] publishedScenes = EditorBuildSettings.scenes
            .Where(entry => entry.enabled)
            .Select(entry => entry.path)
            .ToArray();
        if (!publishedScenes.SequenceEqual(PublishedScenePaths))
            throw new InvalidOperationException(
                "Build Settings yalnız ana menü ve yeni dört perdelik hikâye rotasını içermeli.");
        if (!scene.IsValid())
            throw new InvalidOperationException("Ana menü sahnesi geçerli değil.");

        Debug.Log("Story rebuild ana menü doğrulaması geçti.");
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Story", "Ana menü doğrulaması geçti.", "Tamam");
    }

    private static void PublishBuildRoute()
    {
        EditorBuildSettings.scenes = PublishedScenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void BuildMenuUI(Transform parent, StoryGameManager manager)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);

        GameObject canvasObject = new GameObject(
            "StoryMainMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = StoryChapterBuilderCommon.CreateUIRect(
            "SafeArea",
            canvasObject.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();
        StoryChapterBuilderCommon.CreatePanel(
            "CinematicShade",
            safeArea.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.01f, 0.025f, 0.04f, 0.18f),
            false);

        GameObject titleLogo = StoryChapterBuilderCommon.CreateUIRect(
            "StoryTitleLogo",
            safeArea.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, 505f),
            new Vector2(930f, 480f));
        Image logoImage = titleLogo.AddComponent<Image>();
        logoImage.sprite = StoryChapterBuilderCommon.LoadStoryLogoSprite();
        logoImage.type = Image.Type.Simple;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        Shadow logoShadow = titleLogo.AddComponent<Shadow>();
        logoShadow.effectColor = new Color32(5, 12, 28, 130);
        logoShadow.effectDistance = new Vector2(0f, -12f);
        logoShadow.useGraphicAlpha = true;

        GameObject card = StoryChapterBuilderCommon.CreatePanel(
            "MainMenuCard",
            safeArea.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, -490f),
            new Vector2(920f, 690f),
            Color.white,
            true,
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulNavyPanel);

        GameObject briefingPanel = StoryChapterBuilderCommon.CreatePanel(
            "StoryBriefingPanel",
            card.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, 262f),
            new Vector2(800f, 116f),
            Color.clear,
            false);
        TMP_Text hook = StoryChapterBuilderCommon.CreateText(
            "StoryHook",
            briefingPanel.transform,
            semibold,
            36f,
            new Color32(245, 249, 255, 255),
            TextAlignmentOptions.Center,
            Vector2.zero,
            new Vector2(770f, 104f));
        hook.text = "Evi hazırla, kardeşini koru,\ngüvenli alana birlikte ulaş.";
        hook.enableAutoSizing = true;
        hook.fontSizeMin = 30f;
        hook.fontSizeMax = 36f;

        TMP_Text routeEyebrow = StoryChapterBuilderCommon.CreateText(
            "StoryRouteEyebrow",
            card.transform,
            semibold,
            24f,
            new Color32(164, 218, 250, 255),
            TextAlignmentOptions.Center,
            new Vector2(0f, 154f),
            new Vector2(500f, 40f));
        routeEyebrow.text = "HİKÂYE ROTASI";
        routeEyebrow.characterSpacing = 2f;
        routeEyebrow.textWrappingMode = TextWrappingModes.NoWrap;

        StoryChapterBuilderCommon.CreatePanel(
            "ActRouteLine",
            card.transform,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            new Vector2(0f, 66f),
            new Vector2(590f, 8f),
            new Color32(104, 200, 255, 185),
            false);
        CreateRouteBadge(card.transform, "PreparationRouteBadge", bold, new Vector2(-285f, 66f),
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulYellowBadge, "1", "HAZIRLIK");
        CreateRouteBadge(card.transform, "SafetyRouteBadge", bold, new Vector2(-95f, 66f),
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulBlueBadge, "2", "GÜVENLİ EV");
        CreateRouteBadge(card.transform, "QuakeRouteBadge", bold, new Vector2(95f, 66f),
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulPurpleBadge, "3", "DEPREM");
        CreateRouteBadge(card.transform, "EvacuationRouteBadge", bold, new Vector2(285f, 66f),
            StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulGreenBadge, "4", "TAHLİYE");

        Button continueButton = StoryChapterBuilderCommon.CreateButton(
            "ContinueStoryButton",
            card.transform,
            "DEVAM ET",
            bold,
            Vector2.one * 0.5f,
            new Vector2(0f, -108f),
            new Vector2(650f, 126f),
            StoryChapterBuilderCommon.Teal,
            Color.white);
        Button newStoryButton = StoryChapterBuilderCommon.CreateButton(
            "StartNewStoryButton",
            card.transform,
            "YENİ HİKÂYE",
            semibold,
            Vector2.one * 0.5f,
            new Vector2(0f, -250f),
            new Vector2(650f, 116f),
            new Color(0.12f, 0.2f, 0.25f, 1f),
            StoryChapterBuilderCommon.Cream);
        UnityEventTools.AddPersistentListener(continueButton.onClick, manager.ContinueStory);
        UnityEventTools.AddPersistentListener(newStoryButton.onClick, manager.StartNewStory);

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(parent);
    }

    private static void CreateRouteBadge(
        Transform parent,
        string name,
        TMP_FontAsset font,
        Vector2 position,
        StoryChapterBuilderCommon.StoryUIPanelStyle style,
        string number,
        string caption)
    {
        GameObject badge = StoryChapterBuilderCommon.CreatePanel(
            name,
            parent,
            Vector2.one * 0.5f,
            Vector2.one * 0.5f,
            position,
            new Vector2(136f, 94f),
            Color.white,
            false,
            style);
        TMP_Text numberText = StoryChapterBuilderCommon.CreateText(
            name + "Number",
            badge.transform,
            font,
            43f,
            style == StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulYellowBadge
                ? StoryChapterBuilderCommon.Navy
                : Color.white,
            TextAlignmentOptions.Center,
            new Vector2(0f, 7f),
            new Vector2(90f, 55f));
        numberText.text = number;
        if (style != StoryChapterBuilderCommon.StoryUIPanelStyle.PlayfulYellowBadge)
            StoryChapterBuilderCommon.ApplyDisplayTextStyle(numberText);
        TMP_Text label = StoryChapterBuilderCommon.CreateText(
            name + "Caption",
            badge.transform,
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath),
            25f,
            new Color32(245, 249, 255, 255),
            TextAlignmentOptions.Center,
            new Vector2(0f, -70f),
            new Vector2(188f, 48f));
        label.text = caption;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 21f;
        label.fontSizeMax = 25f;
    }

    private static void ConfigureHomeForMenu(Transform home)
    {
        SetActive(home, "Wardrobe_Secured", true);
        SetActive(home, "Wardrobe_Unsecured", false);
        SetActive(home, "Wardrobe_Fallen", false);
        SetActive(home, "Shelf_Secured", true);
        SetActive(home, "Shelf_Unsecured", false);
        SetActive(home, "Shelf_Fallen", false);
        SetActive(home, "ExitRoute_Cleared", false);
        SetActive(home, "ExitRoute_ClutteredButPassable", false);
        SetActive(home, "BrokenGlass_Hazard", false);
        SetActive(home, "Door", true);
        SetActive(home, "Door_Open", false);
    }

    private static void SetActive(Transform root, string name, bool active)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == name);
        if (target != null)
            target.gameObject.SetActive(active);
    }

    private static GameObject FindRequired(string name)
    {
        Transform target = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == name);
        if (target == null)
            throw new InvalidOperationException("Ana menü nesnesi bulunamadı: " + name);
        return target.gameObject;
    }
}
