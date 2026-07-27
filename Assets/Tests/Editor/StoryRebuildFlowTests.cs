using System;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class StoryRebuildFlowTests
{
    private const string MainMenuScene = "Assets/Scenes/Story_Rebuild_MainMenu.unity";

    private static readonly (string path, StoryAct act, string nextLabel)[] RebuildScenes =
    {
        ("Assets/Scenes/Story_01_RebuildPreview.unity", StoryAct.Preparation, "SONRAKİ PERDE"),
        ("Assets/Scenes/Story_02_RebuildPreview.unity", StoryAct.HomeSafety, "SONRAKİ PERDE"),
        ("Assets/Scenes/Story_03_RebuildPreview.unity", StoryAct.Quake, "SONRAKİ PERDE"),
        ("Assets/Scenes/Story_04_RebuildPreview.unity", StoryAct.Evacuation, "BÖLÜM SEÇİMİ")
    };

    [Test]
    public void MainMenu_OffersContinueAndNewStoryAsPublishedBuildEntry()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScene), Is.Not.Null);
        string[] publishedScenes = EditorBuildSettings.scenes
            .Where(entry => entry.enabled)
            .Select(entry => entry.path)
            .ToArray();
        Assert.That(publishedScenes.First(), Is.EqualTo(MainMenuScene));
        Assert.That(publishedScenes,
            Is.EqualTo(new[] { MainMenuScene }.Concat(RebuildScenes.Select(item => item.path))),
            "Yayın build'i legacy mini oyunları veya eski Story referans sahnelerini paketlememeli.");

        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        StoryGameManager manager = Object.FindFirstObjectByType<StoryGameManager>();
        Assert.That(manager, Is.Not.Null);

        AssertRoundedPanelSprite("MainMenuCard");
        Image logo = FindRequired("StoryTitleLogo").GetComponent<Image>();
        Assert.That(logo, Is.Not.Null);
        Assert.That(logo.preserveAspect, Is.True);
        Assert.That(AssetDatabase.GetAssetPath(logo.sprite),
            Is.EqualTo(StoryChapterBuilderCommon.StoryLogoPath));
        Assert.That(FindRequired("StoryTitle", false), Is.Null);
        Assert.That(FindRequired("StoryEyebrowRibbon", false), Is.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/Story/UI/Brand/SOURCE.md"), Is.Not.Null);
        Assert.That(FindRequired("StoryFormatTag", false), Is.Null);
        Assert.That(FindRequired("StoryFormat", false), Is.Null);
        Assert.That(FindRequired("SaveHint", false), Is.Null);
        TMP_Text routeEyebrow = FindRequired("StoryRouteEyebrow").GetComponent<TMP_Text>();
        Assert.That(routeEyebrow.text, Is.EqualTo("HİKÂYE ROTASI"));
        Assert.That(AssetDatabase.GetAssetPath(routeEyebrow.font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath));
        AssertCasualUiSprite("ContinueStoryButton", "Yellow", "Normal");
        AssertCasualUiSprite("StartNewStoryButton", "Blue", "Normal");
        Assert.That(FindRequired("ContinueStoryButton").GetComponent<Button>().transition,
            Is.EqualTo(Selectable.Transition.SpriteSwap));
        Assert.That(AssetDatabase.GetAssetPath(
                FindRequired("ContinueStoryButton").GetComponentInChildren<TMP_Text>(true).font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulDisplayFontAssetPath));
        Assert.That(AssetDatabase.GetAssetPath(FindRequired("StoryHook").GetComponent<TMP_Text>().font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath));
        AssertButtonEvent("ContinueStoryButton", manager, nameof(StoryGameManager.ContinueStory));
        AssertButtonEvent("StartNewStoryButton", manager, nameof(StoryGameManager.StartNewStory));

        SerializedObject managerData = new SerializedObject(manager);
        Assert.That(managerData.FindProperty("initialAct").intValue,
            Is.EqualTo((int)StoryAct.Preparation));
        Assert.That(managerData.FindProperty("initialFlags").arraySize, Is.Zero);
        AssertRebuildRoute(managerData);
    }

    [TestCaseSource(nameof(RebuildSceneCases))]
    public void RebuildScene_HasPersistentRouteAndAuthoredCompletionNavigation(
        string scenePath,
        StoryAct act,
        string expectedNextLabel)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        StoryGameManager manager = Object.FindFirstObjectByType<StoryGameManager>();
        StoryUIController controller = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(manager, Is.Not.Null, scenePath);
        Assert.That(controller, Is.Not.Null, scenePath);
        AssertRebuildRoute(new SerializedObject(manager));

        GameObject nextAct = FindRequired("NextActButton");
        GameObject replay = FindRequired("ReplayChapterButton");
        GameObject selection = FindRequired("ChapterSelectionCard");
        Assert.That(selection.activeSelf, Is.False, scenePath);
        Assert.That(nextAct.GetComponentInChildren<TMP_Text>(true).text,
            Is.EqualTo(expectedNextLabel), scenePath);

        AssertButtonEvent(nextAct.name, controller, nameof(StoryUIController.ContinueAfterAct));
        AssertButtonEvent(replay.name, controller, nameof(StoryUIController.ReplayStory));
        AssertButtonEvent(
            "OpenPreparationActButton",
            controller,
            nameof(StoryUIController.OpenPreparationAct));
        AssertButtonEvent(
            "OpenHomeSafetyActButton",
            controller,
            nameof(StoryUIController.OpenHomeSafetyAct));
        AssertButtonEvent(
            "OpenQuakeActButton",
            controller,
            nameof(StoryUIController.OpenQuakeAct));
        AssertButtonEvent(
            "OpenEvacuationActButton",
            controller,
            nameof(StoryUIController.OpenEvacuationAct));
        AssertButtonEvent(
            "CloseChapterSelectionButton",
            controller,
            nameof(StoryUIController.ShowCompletionReport));

        SerializedObject uiData = new SerializedObject(controller);
        Assert.That(uiData.FindProperty("completionPanel").objectReferenceValue,
            Is.SameAs(FindRequired("ChapterCompletionCard")), scenePath);
        Assert.That(uiData.FindProperty("completionReportCard").objectReferenceValue,
            Is.SameAs(FindRequired("ChapterCompletionCard")), scenePath);
        Assert.That(uiData.FindProperty("chapterSelectionCard").objectReferenceValue,
            Is.SameAs(selection), scenePath);
        Assert.That(uiData.FindProperty("nextActButton").objectReferenceValue,
            Is.SameAs(nextAct), scenePath);
        foreach (string presentationField in new[]
                 {
                     "objectivePresentation",
                     "subtitlePresentation",
                     "contextPresentation",
                     "pausePresentation",
                     "completionPresentation",
                     "chapterSelectionPresentation"
                 })
        {
            Animation presentation =
                uiData.FindProperty(presentationField).objectReferenceValue as Animation;
            Assert.That(presentation, Is.Not.Null, scenePath + " / " + presentationField);
            Assert.That(presentation.clip, Is.Not.Null, scenePath + " / " + presentationField);
            Assert.That(presentation.GetComponent<CanvasGroup>(), Is.Not.Null,
                scenePath + " / " + presentationField);
        }

        SerializedObject directorData = new SerializedObject(FindActDirector(act));
        SerializedProperty managerReference = directorData.FindProperty("gameManager");
        if (managerReference != null)
            Assert.That(managerReference.objectReferenceValue, Is.SameAs(manager), scenePath);
    }

    [Test]
    public void StoryManager_ExposesOneContinuousFourActRouteContract()
    {
        Type manager = typeof(StoryGameManager);
        AssertPublicMethod(manager, nameof(StoryGameManager.StartNewStory));
        AssertPublicMethod(manager, nameof(StoryGameManager.ContinueStory));
        AssertPublicMethod(manager, nameof(StoryGameManager.ContinueToNextAct));
        AssertPublicMethod(manager, nameof(StoryGameManager.ReplayCurrentAct));
        AssertPublicMethod(manager, nameof(StoryGameManager.OpenAct), typeof(int));
        AssertPublicMethod(manager, nameof(StoryGameManager.GetSceneName), typeof(StoryAct));
    }

    [TestCaseSource(nameof(RebuildMarkerSceneCases))]
    public void RebuildScene_UsesAuthoredKenneyGestureMarkers(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.That(interactions, Is.Not.Empty, scenePath);

        foreach (StoryInteractable interaction in interactions)
        {
            GameObject marker = interaction.HighlightRoot;
            Assert.That(marker, Is.Not.Null, scenePath + " / " + interaction.name);
            Assert.That(marker.GetComponent<BillboardToCamera>(), Is.Not.Null,
                scenePath + " / " + interaction.name);
            Assert.That(marker.GetComponentsInChildren<MeshRenderer>(true), Is.Empty,
                scenePath + " / " + interaction.name + " primitive gosterge kullanmamali.");

            SpriteRenderer[] sprites = marker.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(sprites, Has.Length.EqualTo(3), scenePath + " / " + interaction.name);

            SpriteRenderer badge = sprites.Single(renderer => renderer.name == "ObjectiveBadge");
            SpriteRenderer icon = sprites.Single(renderer => renderer.name == "ObjectiveGestureIcon");
            SpriteRenderer arrow = sprites.Single(renderer => renderer.name == "ObjectiveArrow");
            Assert.That(AssetDatabase.GetAssetPath(badge.sprite),
                Is.EqualTo(StoryChapterBuilderCommon.KenneyInputPromptRoot + "/marker_circle.png"));
            Assert.That(AssetDatabase.GetAssetPath(icon.sprite),
                Is.EqualTo(StoryChapterBuilderCommon.KenneyInputPromptRoot + "/" +
                           ExpectedGestureIcon(interaction.InteractionGesture)));
            Assert.That(AssetDatabase.GetAssetPath(arrow.sprite),
                Is.EqualTo(StoryChapterBuilderCommon.KenneyInputPromptRoot + "/marker_arrow_down.png"));
        }

        string[] removedPrimitiveNames =
        {
            "ObjectiveMarkerDiamond",
            "ObjectiveMarkerPointer",
            "ObjectiveMarkerTip"
        };
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Any(item => removedPrimitiveNames.Contains(item.name)), Is.False, scenePath);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            StoryChapterBuilderCommon.KenneyInputPromptRoot + "/LICENSE.txt"), Is.Not.Null);
    }

    [TestCaseSource(nameof(RebuildHudSceneCases))]
    public void RebuildScene_UsesPreparationHudLanguageAcrossEveryAct(
        string scenePath,
        string expectedActValue)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        StorySafeAreaPanel safeArea = Object.FindFirstObjectByType<StorySafeAreaPanel>();
        StoryUIController controller = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(safeArea, Is.Not.Null, scenePath);
        Assert.That(controller, Is.Not.Null, scenePath);
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None), Is.Empty, scenePath);

        Canvas canvas = controller.GetComponent<Canvas>();
        CanvasScaler scaler = controller.GetComponent<CanvasScaler>();
        Assert.That(canvas, Is.Not.Null, scenePath);
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), scenePath);
        Assert.That(scaler, Is.Not.Null, scenePath);
        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)), scenePath);
        Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize), scenePath);

        GameObject objective = FindRequired("ObjectiveStrip");
        AssertRoundedPanelSprite(objective, scenePath);
        GameObject actBadge = FindRequired("ActBadge");
        Assert.That(actBadge.transform.parent, Is.SameAs(objective.transform), scenePath);
        AssertCasualUiSprite(actBadge, "Yellow", "Normal", scenePath);
        Assert.That(FindRequired("ActBadgeCaption").GetComponent<TMP_Text>().text, Is.EqualTo("PERDE"), scenePath);
        Assert.That(FindRequired("ActBadgeValue").GetComponent<TMP_Text>().text, Is.EqualTo(expectedActValue), scenePath);

        TMP_Text title = FindRequired("ObjectiveTitle").GetComponent<TMP_Text>();
        TMP_Text detail = FindRequired("ObjectiveDetail").GetComponent<TMP_Text>();
        TMP_Text context = FindRequired("ContextText").GetComponent<TMP_Text>();
        TMP_Text subtitle = FindRequired("SubtitleText").GetComponent<TMP_Text>();
        Assert.That(title.enableAutoSizing, Is.True, scenePath);
        Assert.That(title.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap), scenePath);
        Assert.That(detail.enableAutoSizing, Is.True, scenePath);
        Assert.That(context.enableAutoSizing, Is.True, scenePath);
        Assert.That(subtitle.enableAutoSizing, Is.True, scenePath);
        AssertCasualUiSprite("PauseButton", "Purple", "Normal");
        AssertCasualUiSprite("ContextPanel", "Yellow", "Normal");
        AssertRoundedPanelSprite("SubtitlePanel");
        AssertRoundedPanelSprite("PausePanel");
        AssertRoundedPanelSprite("ChapterCompletionCard");
        AssertRoundedPanelSprite("ChapterSelectionCard");
        AssertCasualUiSprite("ResumeButton", "Yellow", "Normal");
        AssertCasualUiSprite("RetryCheckpointButton", "Blue", "Normal");
        AssertCasualUiSprite("NextActButton", "Green", "Normal");
        Assert.That(FindRequired("ObjectiveAccent", false), Is.Null, scenePath);
        Assert.That(FindRequired("ContextAccent", false), Is.Null, scenePath);

        Assert.That(AssetDatabase.GetAssetPath(title.font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulDisplayFontAssetPath), scenePath);
        Assert.That(AssetDatabase.GetAssetPath(detail.font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath), scenePath);
        Assert.That(AssetDatabase.GetAssetPath(subtitle.font),
            Is.EqualTo(StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath), scenePath);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            StoryChapterBuilderCommon.CasualUiRoot + "/SOURCE.md"), Is.Not.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            StoryChapterBuilderCommon.PlayfulFontRoot + "/OFL-Lexend.txt"), Is.Not.Null);
        Assert.That(Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(image => image.sprite != null)
            .Select(image => AssetDatabase.GetAssetPath(image.sprite))
            .Any(path => path.StartsWith(StoryChapterBuilderCommon.CartoonUiRoot, StringComparison.Ordinal)),
            Is.False,
            scenePath + " eski düz Cartoon UI paketini kullanmamalı.");
    }

    [TestCaseSource(nameof(PublishedSceneCases))]
    public void PublishedScene_UsesOnlyLexendForAuthoredText(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        string[] allowedFonts =
        {
            StoryChapterBuilderCommon.PlayfulRegularFontAssetPath,
            StoryChapterBuilderCommon.PlayfulSemiboldFontAssetPath,
            StoryChapterBuilderCommon.PlayfulDisplayFontAssetPath
        };
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.That(texts, Is.Not.Empty, scenePath);

        foreach (TMP_Text text in texts)
        {
            Assert.That(text.font, Is.Not.Null, scenePath + " / " + text.name);
            string fontPath = AssetDatabase.GetAssetPath(text.font);
            Assert.That(
                allowedFonts,
                Does.Contain(fontPath),
                scenePath + " / " + text.name + " yayın UI tipografisinden sapmamalı.");
        }
    }

    [TestCaseSource(nameof(PublishedSceneCases))]
    public void PublishedScene_UsesAuthoredProceduralSkybox(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null, scenePath);
        Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.Skybox), scenePath);
        Assert.That(RenderSettings.skybox, Is.Not.Null, scenePath);
        Assert.That(
            AssetDatabase.GetAssetPath(RenderSettings.skybox),
            Is.EqualTo(StoryChapterBuilderCommon.StorySkyboxMaterialPath),
            scenePath);
        Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("Skybox/Procedural"), scenePath);
    }

    private static object[] RebuildSceneCases()
    {
        return RebuildScenes
            .Select(item => new object[] { item.path, item.act, item.nextLabel })
            .ToArray();
    }

    private static object[] RebuildMarkerSceneCases()
    {
        return RebuildScenes
            .Select(item => new object[] { item.path })
            .ToArray();
    }

    private static object[] RebuildHudSceneCases()
    {
        return RebuildScenes
            .Select(item => new object[] { item.path, $"{(int)item.act}/4" })
            .ToArray();
    }

    private static object[] PublishedSceneCases()
    {
        return new[] { MainMenuScene }
            .Concat(RebuildScenes.Select(item => item.path))
            .Select(path => new object[] { path })
            .ToArray();
    }

    private static string ExpectedGestureIcon(StoryInteractionGesture gesture)
    {
        return gesture switch
        {
            StoryInteractionGesture.DragToBag => "touch_swipe_move.png",
            StoryInteractionGesture.DragToTarget => "touch_swipe_move.png",
            StoryInteractionGesture.SwipeHorizontal => "touch_swipe_horizontal.png",
            StoryInteractionGesture.SwipeDown => "touch_swipe_down.png",
            StoryInteractionGesture.WorldHold => "touch_tap_hold.png",
            StoryInteractionGesture.RepeatedTap => "touch_tap_double.png",
            _ => "touch_tap.png"
        };
    }

    private static void AssertRebuildRoute(SerializedObject managerData)
    {
        Assert.That(managerData.FindProperty("persistAcrossScenes").boolValue, Is.True);
        Assert.That(managerData.FindProperty("loadExistingSave").boolValue, Is.True);
        Assert.That(managerData.FindProperty("preparationSceneName").stringValue,
            Is.EqualTo("Story_01_RebuildPreview"));
        Assert.That(managerData.FindProperty("homeSafetySceneName").stringValue,
            Is.EqualTo("Story_02_RebuildPreview"));
        Assert.That(managerData.FindProperty("quakeSceneName").stringValue,
            Is.EqualTo("Story_03_RebuildPreview"));
        Assert.That(managerData.FindProperty("evacuationSceneName").stringValue,
            Is.EqualTo("Story_04_RebuildPreview"));
    }

    private static MonoBehaviour FindActDirector(StoryAct act)
    {
        string typeName = act switch
        {
            StoryAct.Preparation => "StoryPreparationDirector",
            StoryAct.HomeSafety => "StoryHomeSafetyDirector",
            StoryAct.Quake => "StorySequenceDirector",
            StoryAct.Evacuation => "StoryEvacuationDirector",
            _ => throw new ArgumentOutOfRangeException(nameof(act), act, null)
        };

        MonoBehaviour director = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .SingleOrDefault(item => item != null && item.GetType().Name == typeName);
        Assert.That(director, Is.Not.Null, typeName);
        return director;
    }

    private static void AssertButtonEvent(string objectName, Object target, string methodName)
    {
        Button button = FindRequired(objectName).GetComponent<Button>();
        Assert.That(button, Is.Not.Null, objectName);
        Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1), objectName);
        Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(target), objectName);
        Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(methodName), objectName);
    }

    private static void AssertCasualUiSprite(string objectName, string palette, string state)
    {
        AssertCasualUiSprite(FindRequired(objectName), palette, state, objectName);
    }

    private static void AssertCasualUiSprite(
        GameObject target,
        string palette,
        string state,
        string context)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        Assert.That(image, Is.Not.Null, context);
        Assert.That(image.sprite, Is.Not.Null, context);
        Assert.That(
            AssetDatabase.GetAssetPath(image.sprite),
            Is.EqualTo(StoryChapterBuilderCommon.CasualUiRoot + "/Buttons/Btn_" +
                       palette + "_Large_" + state + ".png"),
            context);
    }

    private static void AssertRoundedPanelSprite(string objectName)
    {
        AssertRoundedPanelSprite(FindRequired(objectName), objectName);
    }

    private static void AssertRoundedPanelSprite(GameObject target, string context)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        Assert.That(image, Is.Not.Null, context);
        Assert.That(image.sprite, Is.Not.Null, context);
        Assert.That(
            AssetDatabase.GetAssetPath(image.sprite),
            Is.EqualTo(DepremUITheme.RoundedSpritePath),
            context);
        Assert.That(target.GetComponent<Outline>(), Is.Not.Null, context);
        Assert.That(target.GetComponent<Shadow>(), Is.Not.Null, context);
    }

    private static GameObject FindRequired(string objectName, bool required = true)
    {
        Transform target = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == objectName);
        if (required)
            Assert.That(target, Is.Not.Null, objectName);
        return target != null ? target.gameObject : null;
    }

    private static void AssertPublicMethod(Type type, string methodName, params Type[] parameters)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public,
            null,
            parameters,
            null);
        Assert.That(method, Is.Not.Null, methodName);
    }
}
