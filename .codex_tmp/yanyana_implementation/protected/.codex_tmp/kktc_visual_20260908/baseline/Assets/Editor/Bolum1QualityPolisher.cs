using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class Bolum1QualityPolisher
{
    private const string ScenePath = "Assets/Scenes/bolum1.unity";
    private const string PolishVersionMarker = "Bolum1_UI_Identity_V2";
    private const string RoundedSpritePath = DepremUITheme.RoundedSpritePath;
    private const string RegularFontSourcePath = "Assets/Fonts/Bolum1/Inter-Regular.ttf";
    private const string SemiboldFontSourcePath = "Assets/Fonts/Bolum1/Inter-SemiBold.ttf";
    private const string BoldFontSourcePath = "Assets/Fonts/Bolum1/Inter-Bold.ttf";
    private const string RegularFontAssetPath = DepremUITheme.RegularFontPath;
    private const string SemiboldFontAssetPath = DepremUITheme.SemiboldFontPath;
    private const string BoldFontAssetPath = DepremUITheme.BoldFontPath;

    private static Color Ink => uiTheme != null ? uiTheme.ink : new Color(0.018f, 0.045f, 0.11f, 0.97f);
    private static Color Panel => uiTheme != null ? uiTheme.panel : new Color(0.035f, 0.11f, 0.24f, 0.88f);
    private static Color DeepGreen => uiTheme != null ? uiTheme.panelSoft : new Color(0.025f, 0.28f, 0.58f, 1f);
    private static Color Amber => uiTheme != null ? uiTheme.primary : new Color(0.08f, 0.42f, 1f, 1f);
    private static Color Acid => uiTheme != null ? uiTheme.accent : new Color(0.26f, 0.85f, 1f, 1f);
    private static Color SoftWhite => uiTheme != null ? uiTheme.textPrimary : new Color(0.95f, 0.98f, 1f, 1f);
    private static Color Muted => uiTheme != null ? uiTheme.textSecondary : new Color(0.66f, 0.76f, 0.86f, 1f);

    private static DepremUITheme uiTheme;
    private static TMP_FontAsset sharedFont;
    private static TMP_FontAsset regularFont;
    private static TMP_FontAsset semiboldFont;
    private static TMP_FontAsset boldFont;
    private static Sprite roundedSprite;

    [InitializeOnLoadMethod]
    private static void ApplyToUnpolishedOpenScene()
    {
        EditorApplication.delayCall += () =>
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                scene.IsValid() &&
                scene.name.Equals("bolum1", System.StringComparison.OrdinalIgnoreCase) &&
                FindSceneObject(PolishVersionMarker) == null)
            {
                Polish();
            }
        };
    }

    [MenuItem("Tools/Deprem/Polish Bolum 1")]
    public static void Polish()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Equals("bolum1", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Bolum1QualityPolisher: Open the bolum1 scene first.");
            return;
        }

        Canvas canvas = FindSceneComponent<Canvas>("Canvas");
        Bolum1GameManager gameManager = FindSceneComponent<Bolum1GameManager>("Bolum1GameManager");
        Bolum1FeedbackUI feedback = FindSceneComponent<Bolum1FeedbackUI>("Bolum1FeedbackUI");
        Bolum1HintSystem hintSystem = FindSceneComponent<Bolum1HintSystem>("Bolum1HintSystem");

        if (canvas == null || gameManager == null || feedback == null)
        {
            Debug.LogError("Bolum1QualityPolisher: Required Bolum 1 objects are missing.");
            return;
        }

        uiTheme = DepremUIThemeStore.LoadOrCreate();
        TMP_Text existingText = FindSceneComponent<TMP_Text>("StarText");
        sharedFont = existingText != null ? existingText.font : null;
        regularFont = uiTheme.regularFont != null
            ? uiTheme.regularFont
            : GetOrCreateFontAsset(RegularFontSourcePath, RegularFontAssetPath, "Inter Regular SDF");
        semiboldFont = uiTheme.semiboldFont != null
            ? uiTheme.semiboldFont
            : GetOrCreateFontAsset(SemiboldFontSourcePath, SemiboldFontAssetPath, "Inter SemiBold SDF");
        boldFont = uiTheme.boldFont != null
            ? uiTheme.boldFont
            : GetOrCreateFontAsset(BoldFontSourcePath, BoldFontAssetPath, "Inter Bold SDF");
        roundedSprite = uiTheme.roundedSprite != null ? uiTheme.roundedSprite : GetOrCreateRoundedSprite();

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Polish Bolum 1 UI");
        Undo.RegisterCompleteObjectUndo(gameManager, "Configure Bolum 1 Manager");
        Undo.RegisterCompleteObjectUndo(feedback, "Configure Bolum 1 Feedback");

        ConfigureCanvas(canvas);
        ConfigureHud(canvas.transform, gameManager);
        ConfigureFeedback(canvas.transform, feedback);
        ConfigureIntro(canvas.transform);
        ConfigureSuccess(canvas.transform, gameManager);
        ConfigureHintSystem(hintSystem);
        ConfigureLighting();
        EnsureVersionMarker(canvas.transform);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(feedback);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Bolum1QualityPolisher: Bolum 1 quality pass applied and scene saved.");
    }

    public static void PolishFromBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Polish();
    }

    public static void CapturePreviewFromBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Polish();

        Canvas canvas = FindSceneComponent<Canvas>("Canvas");
        Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (canvas == null || camera == null)
        {
            Debug.LogError("Bolum1QualityPolisher: Camera or canvas is missing for preview capture.");
            return;
        }

        RenderMode originalRenderMode = canvas.renderMode;
        Camera originalCanvasCamera = canvas.worldCamera;
        float originalPlaneDistance = canvas.planeDistance;
        RenderTexture originalTarget = camera.targetTexture;
        float originalAspect = camera.aspect;
        RenderTexture originalActive = RenderTexture.active;
        GameObject intro = FindSceneObject("MissionIntroPanel");
        GameObject success = FindSceneObject("SuccessPanel");
        bool introWasActive = intro != null && intro.activeSelf;
        bool successWasActive = success != null && success.activeSelf;

        RenderTexture target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
        Texture2D preview = new Texture2D(1080, 1920, TextureFormat.RGB24, false);

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;
            camera.aspect = 1080f / 1920f;
            camera.targetTexture = target;

            CapturePreviewState(canvas, camera, target, preview, "Bolum1V2Preview.png");

            if (intro != null && success != null)
            {
                intro.SetActive(false);
                success.SetActive(false);
                CapturePreviewState(canvas, camera, target, preview, "Bolum1V2Gameplay.png");

                success.SetActive(true);
                CapturePreviewState(canvas, camera, target, preview, "Bolum1V2Success.png");
            }
        }
        finally
        {
            if (intro != null)
                intro.SetActive(introWasActive);
            if (success != null)
                success.SetActive(successWasActive);

            RenderTexture.active = originalActive;
            camera.targetTexture = originalTarget;
            camera.aspect = originalAspect;
            canvas.renderMode = originalRenderMode;
            canvas.worldCamera = originalCanvasCamera;
            canvas.planeDistance = originalPlaneDistance;
            Object.DestroyImmediate(preview);
            Object.DestroyImmediate(target);
        }
    }

    private static void CapturePreviewState(
        Canvas canvas,
        Camera camera,
        RenderTexture target,
        Texture2D preview,
        string fileName)
    {
        canvas.enabled = false;
        canvas.enabled = true;

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            text.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();
        target.DiscardContents();
        RenderTexture.active = target;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        camera.Render();
        RenderTexture.active = target;
        preview.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
        preview.Apply();

        string previewPath = Path.Combine(Path.GetTempPath(), "Deprem-Bolum1-V2", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
        File.WriteAllBytes(previewPath, preview.EncodeToPNG());
        Debug.Log($"Bolum1QualityPolisher: Preview captured at {previewPath}");
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = uiTheme != null ? uiTheme.referenceResolution : new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = uiTheme != null ? uiTheme.matchWidthOrHeight : 0.5f;
    }

    private static void ConfigureHud(Transform canvas, Bolum1GameManager manager)
    {
        RectTransform hud = FindChildRect(canvas, "HUDPanel");
        TMP_Text missionText = FindSceneComponent<TMP_Text>("StarText");
        if (hud == null || missionText == null)
            return;

        ConfigureTopAnchoredRect(hud, new Vector2(0f, -124f), new Vector2(988f, 208f));

        Image hudImage = EnsureComponent<Image>(hud.gameObject);
        ApplyRoundedStyle(hudImage, new Color(Panel.r, Panel.g, Panel.b, 0.84f));
        hudImage.raycastTarget = false;

        Outline hudOutline = EnsureComponent<Outline>(hud.gameObject);
        hudOutline.effectColor = new Color(Acid.r, Acid.g, Acid.b, 0.24f);
        hudOutline.effectDistance = new Vector2(2f, -2f);

        Shadow hudShadow = EnsureComponent<Shadow>(hud.gameObject);
        hudShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        hudShadow.effectDistance = new Vector2(0f, -10f);

        RectTransform hudHighlight = GetOrCreateImage(
            hud,
            "HudGlassHighlight",
            new Color(SoftWhite.r, SoftWhite.g, SoftWhite.b, 0.2f));
        ConfigureCenteredRect(hudHighlight, new Vector2(0f, 96f), new Vector2(900f, 4f));
        ApplyRoundedStyle(
            hudHighlight.GetComponent<Image>(),
            new Color(SoftWhite.r, SoftWhite.g, SoftWhite.b, 0.2f));
        hudHighlight.GetComponent<Image>().raycastTarget = false;
        hudHighlight.SetAsFirstSibling();

        RectTransform accent = GetOrCreateImage(hud, "HudAccent", Amber);
        ConfigureCenteredRect(accent, new Vector2(-487f, 0f), new Vector2(14f, 208f));
        ApplyRoundedStyle(accent.GetComponent<Image>(), Amber);
        accent.GetComponent<Image>().raycastTarget = false;

        RectTransform chapterPlate = GetOrCreateImage(hud, "ChapterPlate", Amber);
        ConfigureCenteredRect(chapterPlate, new Vector2(-414f, 0f), new Vector2(118f, 148f));
        ApplyRoundedStyle(chapterPlate.GetComponent<Image>(), new Color(Amber.r, Amber.g, Amber.b, 0.96f));
        chapterPlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text chapterNumber = GetOrCreateText(chapterPlate, "ChapterNumber");
        StretchFull(chapterNumber.rectTransform, new Vector2(8f, 5f));
        ConfigureText(chapterNumber, 58f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        UseDisplayFont(chapterNumber);
        chapterNumber.text = "01";

        TMP_Text activeLabel = GetOrCreateText(hud, "ActiveMissionLabel");
        ConfigureCenteredRect(activeLabel.rectTransform, new Vector2(-170f, 60f), new Vector2(330f, 34f));
        ConfigureText(activeLabel, 19f, TextAlignmentOptions.Left, Acid, FontStyles.Bold);
        activeLabel.text = "AKTIF GOREV  /  HAZIRLIK";

        ConfigureCenteredRect(missionText.rectTransform, new Vector2(-95f, 18f), new Vector2(450f, 70f));
        ConfigureText(missionText, 31f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
        missionText.enableAutoSizing = true;
        missionText.fontSizeMin = 23f;
        missionText.fontSizeMax = 32f;
        missionText.margin = Vector4.zero;
        missionText.text = "<b>AFET CANTANI HAZIRLA</b>\n<size=21><color=#9FAEAA>Gerekli esyalari sec</color></size>";

        TMP_Text progressLabel = GetOrCreateText(hud, "ProgressLabel");
        ConfigureCenteredRect(progressLabel.rectTransform, new Vector2(-230f, -54f), new Vector2(220f, 28f));
        ConfigureText(progressLabel, 17f, TextAlignmentOptions.Left, Muted, FontStyles.Bold);
        progressLabel.text = "CANTA DOLUMU";

        RectTransform track = GetOrCreateImage(hud, "ProgressTrack", new Color(0.16f, 0.21f, 0.21f, 1f));
        ConfigureCenteredRect(track, new Vector2(-80f, -78f), new Vector2(512f, 26f));
        ApplyRoundedStyle(track.GetComponent<Image>(), new Color(0.16f, 0.21f, 0.21f, 0.86f));
        track.GetComponent<Image>().raycastTarget = false;

        RectTransform fillRect = GetOrCreateImage(track, "ProgressFill", Acid);
        StretchFull(fillRect, new Vector2(3f, 3f));
        Image fill = fillRect.GetComponent<Image>();
        fill.type = Image.Type.Simple;
        fill.enabled = false;
        fill.raycastTarget = false;

        for (int index = 1; index < 13; index++)
        {
            RectTransform divider = GetOrCreateImage(track, $"ProgressDivider{index:00}", Ink);
            float dividerX = -256f + 512f * index / 13f;
            ConfigureCenteredRect(divider, new Vector2(dividerX, 0f), new Vector2(4f, 26f));
            divider.GetComponent<Image>().raycastTarget = false;
            divider.SetAsLastSibling();
        }

        RectTransform counterPlate = GetOrCreateImage(hud, "CounterPlate", Acid);
        ConfigureCenteredRect(counterPlate, new Vector2(402f, 0f), new Vector2(154f, 148f));
        ApplyRoundedStyle(counterPlate.GetComponent<Image>(), new Color(Acid.r, Acid.g, Acid.b, 0.96f));
        counterPlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text progressText = GetOrReparentText(hud, counterPlate, "ProgressText");
        ConfigureCenteredRect(progressText.rectTransform, new Vector2(0f, 12f), new Vector2(140f, 68f));
        ConfigureText(progressText, 38f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        UseDisplayFont(progressText);
        progressText.text = "00/13";

        TMP_Text counterLabel = GetOrReparentText(hud, counterPlate, "CounterLabel");
        ConfigureCenteredRect(counterLabel.rectTransform, new Vector2(0f, -44f), new Vector2(140f, 30f));
        ConfigureText(counterLabel, 16f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        counterLabel.text = "ESYA";

        missionText.rectTransform.SetAsLastSibling();
        progressText.rectTransform.SetAsLastSibling();
        counterLabel.rectTransform.SetAsLastSibling();

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("progressText").objectReferenceValue = progressText;
        managerObject.FindProperty("progressFill").objectReferenceValue = fill;
        managerObject.FindProperty("successPanelDelay").floatValue = 0.65f;
        managerObject.FindProperty("loadNextSceneOnComplete").boolValue = false;
        managerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFeedback(Transform canvas, Bolum1FeedbackUI feedback)
    {
        RectTransform messageRoot = FindChildRect(canvas, "MessageBg");
        if (messageRoot != null)
        {
            ConfigureTopAnchoredRect(messageRoot, new Vector2(0f, -286f), new Vector2(850f, 100f));
            Image image = EnsureComponent<Image>(messageRoot.gameObject);
            ApplyRoundedStyle(image, new Color(Panel.r, Panel.g, Panel.b, 0.84f));
            image.raycastTarget = false;

            Outline outline = EnsureComponent<Outline>(messageRoot.gameObject);
            outline.effectColor = new Color(Acid.r, Acid.g, Acid.b, 0.26f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform statusRail = GetOrCreateImage(messageRoot, "StatusRail", Amber);
            ConfigureCenteredRect(statusRail, new Vector2(-410f, 0f), new Vector2(10f, 76f));
            ApplyRoundedStyle(statusRail.GetComponent<Image>(), Amber);
            statusRail.GetComponent<Image>().raycastTarget = false;

            TMP_Text message = messageRoot.GetComponentInChildren<TMP_Text>(true);
            if (message != null)
            {
                ConfigureText(message, 27f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
                message.enableAutoSizing = true;
                message.fontSizeMin = 21f;
                message.fontSizeMax = 28f;
                message.margin = new Vector4(42f, 12f, 24f, 12f);
            }
        }

        ConfigureMark(canvas, "CorrectMark", new Vector2(0f, 205f));
        ConfigureMark(canvas, "WrongMark", new Vector2(0f, 205f));

        AudioSource source = EnsureComponent<AudioSource>(feedback.gameObject);
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0.9f;

        SerializedObject feedbackObject = new SerializedObject(feedback);
        feedbackObject.FindProperty("audioSource").objectReferenceValue = source;
        feedbackObject.FindProperty("charactersPerSecond").floatValue = 45f;
        feedbackObject.FindProperty("holdDuration").floatValue = 2.2f;
        feedbackObject.FindProperty("markDuration").floatValue = 0.72f;
        feedbackObject.FindProperty("positiveMessageDuration").floatValue = 1.15f;
        feedbackObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMark(Transform canvas, string name, Vector2 position)
    {
        RectTransform mark = FindChildRect(canvas, name);
        if (mark == null)
            return;

        ConfigureCenteredRect(mark, position, new Vector2(150f, 150f));
        Image image = mark.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        Shadow shadow = EnsureComponent<Shadow>(mark.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -7f);
    }

    private static void ConfigureIntro(Transform canvas)
    {
        RectTransform intro = GetOrCreateImage(canvas, "MissionIntroPanel", new Color(0.01f, 0.025f, 0.025f, 0.78f));
        StretchFull(intro, Vector2.zero);
        Image introBackdrop = intro.GetComponent<Image>();
        introBackdrop.sprite = null;
        introBackdrop.type = Image.Type.Simple;
        introBackdrop.color = new Color(0.006f, 0.018f, 0.055f, 0.58f);
        introBackdrop.raycastTarget = true;

        RectTransform fieldFrame = GetOrCreateImage(
            intro,
            "IntroFieldFrame",
            new Color(Acid.r, Acid.g, Acid.b, 0.24f));
        ConfigureCenteredRect(fieldFrame, new Vector2(-20f, -25f), new Vector2(1178f, 1438f));
        ApplyRoundedStyle(
            fieldFrame.GetComponent<Image>(),
            new Color(Acid.r, Acid.g, Acid.b, 0.24f));
        fieldFrame.GetComponent<Image>().raycastTarget = false;

        RectTransform slash = GetOrCreateImage(intro, "IntroFieldSheet", new Color(Panel.r, Panel.g, Panel.b, 0.84f));
        ConfigureCenteredRect(slash, new Vector2(-20f, -25f), new Vector2(1160f, 1420f));
        ApplyRoundedStyle(slash.GetComponent<Image>(), new Color(Panel.r, Panel.g, Panel.b, 0.84f));
        slash.GetComponent<Image>().raycastTarget = false;
        fieldFrame.SetAsFirstSibling();
        slash.SetSiblingIndex(1);

        RectTransform topLine = GetOrReparentImage(intro, slash, "TopAccent", Amber);
        ConfigureCenteredRect(topLine, new Vector2(-473f, 20f), new Vector2(14f, 1290f));
        ApplyRoundedStyle(topLine.GetComponent<Image>(), Amber);
        topLine.GetComponent<Image>().raycastTarget = false;

        TMP_Text ghostNumber = GetOrReparentText(intro, slash, "GhostChapterNumber");
        ConfigureCenteredRect(ghostNumber.rectTransform, new Vector2(375f, 530f), new Vector2(330f, 250f));
        ConfigureText(ghostNumber, 210f, TextAlignmentOptions.Center, new Color(Amber.r, Amber.g, Amber.b, 0.14f), FontStyles.Bold);
        UseDisplayFont(ghostNumber);
        ghostNumber.text = "01";

        TMP_Text chapter = GetOrReparentText(intro, slash, "ChapterLabel");
        ConfigureCenteredRect(chapter.rectTransform, new Vector2(-165f, 545f), new Vector2(560f, 52f));
        ConfigureText(chapter, 25f, TextAlignmentOptions.Left, Acid, FontStyles.Bold);
        chapter.text = "SAHA G\u00d6REVI  /  B\u00d6L\u00dcM 01";

        RectTransform badge = GetOrReparentImage(intro, slash, "MissionBadge", Amber);
        ConfigureCenteredRect(badge, new Vector2(-385f, 401f), new Vector2(104f, 104f));
        ApplyRoundedStyle(badge.GetComponent<Image>(), Amber);
        badge.GetComponent<Image>().raycastTarget = false;

        RectTransform crossHorizontal = GetOrCreateImage(badge, "CrossHorizontal", Ink);
        ConfigureCenteredRect(crossHorizontal, Vector2.zero, new Vector2(58f, 18f));
        crossHorizontal.GetComponent<Image>().raycastTarget = false;

        RectTransform crossVertical = GetOrCreateImage(badge, "CrossVertical", Ink);
        ConfigureCenteredRect(crossVertical, Vector2.zero, new Vector2(18f, 58f));
        crossVertical.GetComponent<Image>().raycastTarget = false;

        TMP_Text title = GetOrReparentText(intro, slash, "IntroTitle");
        ConfigureCenteredRect(title.rectTransform, new Vector2(80f, 335f), new Vector2(720f, 190f));
        ConfigureText(title, 72f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
        UseDisplayFont(title);
        title.enableAutoSizing = true;
        title.fontSizeMin = 54f;
        title.fontSizeMax = 74f;
        title.lineSpacing = -8f;
        title.text = "AFET \u00c7ANTANI\nHAZIRLA";

        TMP_Text body = GetOrReparentText(intro, slash, "IntroBody");
        ConfigureCenteredRect(body.rectTransform, new Vector2(-5f, 150f), new Vector2(850f, 112f));
        ConfigureText(body, 30f, TextAlignmentOptions.Left, Muted, FontStyles.Normal);
        body.lineSpacing = 5f;
        body.text = "Masadaki e\u015fyalar\u0131 incele. Afet sonras\u0131nda\nihtiyac\u0131n olacak 13 par\u00e7ay\u0131 ay\u0131r.";

        RectTransform ruleBand = GetOrReparentImage(intro, slash, "MissionRule", Acid);
        ConfigureCenteredRect(ruleBand, new Vector2(20f, -17f), new Vector2(900f, 112f));
        ApplyRoundedStyle(ruleBand.GetComponent<Image>(), new Color(Acid.r, Acid.g, Acid.b, 0.94f));
        ruleBand.GetComponent<Image>().raycastTarget = false;
        Outline ruleOutline = EnsureComponent<Outline>(ruleBand.gameObject);
        ruleOutline.effectColor = new Color(0f, 0f, 0f, 0.38f);
        ruleOutline.effectDistance = new Vector2(4f, -4f);

        TMP_Text ruleText = GetOrCreateText(ruleBand, "MissionRuleText");
        StretchFull(ruleText.rectTransform, new Vector2(34f, 16f));
        ConfigureText(ruleText, 24f, TextAlignmentOptions.Left, Ink, FontStyles.Bold);
        ruleText.text = "HEDEF  /  13 GEREKLI E\u015eYA\nKURAL  /  GEREKSIZLER MASADA";

        GetOrReparentImage(intro, slash, "IntroStep01", DeepGreen);
        GetOrReparentImage(intro, slash, "IntroStep02", DeepGreen);
        GetOrReparentImage(intro, slash, "IntroStep03", DeepGreen);
        ConfigureIntroStep(slash, "IntroStep01", new Vector2(-280f, -180f), "01", "DOKUN", "E\u015fyay\u0131 incele");
        ConfigureIntroStep(slash, "IntroStep02", new Vector2(20f, -180f), "02", "KARAR VER", "Gerekli mi?");
        ConfigureIntroStep(slash, "IntroStep03", new Vector2(320f, -180f), "03", "\u00c7ANTAYA AT", "Do\u011fruysa topla");

        GetOrReparentRect(intro, slash, "StartMissionButton");
        Button startButton = GetOrCreateButton(slash, "StartMissionButton", Amber);
        ConfigureCenteredRect(startButton.GetComponent<RectTransform>(), new Vector2(20f, -385f), new Vector2(900f, 118f));
        ConfigureButtonColors(startButton, Amber);
        ConfigureButtonText(startButton, "G\u00d6REVE BA\u015eLA", 35f, SoftWhite);
        TMP_Text startLabel = startButton.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
        {
            startLabel.alignment = TextAlignmentOptions.Left;
            startLabel.margin = new Vector4(38f, 0f, 140f, 0f);
        }

        RectTransform arrowPlate = GetOrCreateImage(startButton.transform, "ArrowPlate", Acid);
        ConfigureCenteredRect(arrowPlate, new Vector2(391f, 0f), new Vector2(94f, 94f));
        ApplyRoundedStyle(arrowPlate.GetComponent<Image>(), Acid);
        arrowPlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text arrow = GetOrCreateText(arrowPlate, "Arrow");
        StretchFull(arrow.rectTransform, new Vector2(8f, 8f));
        ConfigureText(arrow, 54f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        arrow.text = ">";
        ClearPersistentListeners(startButton);
        UnityEventTools.AddBoolPersistentListener(startButton.onClick, intro.gameObject.SetActive, false);

        TMP_Text hint = GetOrReparentText(intro, slash, "InputHint");
        ConfigureCenteredRect(hint.rectTransform, new Vector2(20f, -480f), new Vector2(900f, 54f));
        ConfigureText(hint, 23f, TextAlignmentOptions.Left, Muted, FontStyles.Normal);
        hint.text = "DOKUNARAK SE\u00c7  /  TUTUP \u00c7ANTAYA S\u00dcR\u00dcKLE";

        intro.SetAsLastSibling();
        intro.gameObject.SetActive(true);
    }

    private static void ConfigureSuccess(Transform canvas, Bolum1GameManager manager)
    {
        RectTransform success = FindChildRect(canvas, "SuccessPanel");
        if (success == null)
            return;

        string[] legacySuccessNames = { "Text (TMP)", "LegacySuccessText" };
        foreach (string legacyName in legacySuccessNames)
        {
            RectTransform legacyMessage = FindDirectChildRect(success, legacyName);
            if (legacyMessage != null)
                Undo.DestroyObjectImmediate(legacyMessage.gameObject);
        }

        GraphicRaycaster nestedRaycaster = success.GetComponent<GraphicRaycaster>();
        if (nestedRaycaster != null)
            Undo.DestroyObjectImmediate(nestedRaycaster);

        Canvas nestedCanvas = success.GetComponent<Canvas>();
        if (nestedCanvas != null)
            Undo.DestroyObjectImmediate(nestedCanvas);

        StretchFull(success, Vector2.zero);
        Image overlay = EnsureComponent<Image>(success.gameObject);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(0.006f, 0.02f, 0.065f, 1f);
        overlay.raycastTarget = true;

        RectTransform cardFrame = GetOrCreateImage(
            success,
            "SuccessCardFrame",
            new Color(Acid.r, Acid.g, Acid.b, 0.26f));
        ConfigureCenteredRect(cardFrame, new Vector2(0f, -20f), new Vector2(958f, 1138f));
        cardFrame.localRotation = Quaternion.identity;
        ApplyRoundedStyle(
            cardFrame.GetComponent<Image>(),
            new Color(Acid.r, Acid.g, Acid.b, 0.26f));
        cardFrame.GetComponent<Image>().raycastTarget = false;

        Color cardColor = new Color(Panel.r, Panel.g, Panel.b, 0.82f);
        RectTransform card = GetOrCreateImage(
            success,
            "SuccessCard",
            cardColor);
        ConfigureCenteredRect(card, new Vector2(0f, -20f), new Vector2(940f, 1120f));
        card.localRotation = Quaternion.identity;
        ApplyRoundedStyle(card.GetComponent<Image>(), cardColor);
        Outline cardOutline = EnsureComponent<Outline>(card.gameObject);
        cardOutline.effectColor = new Color(SoftWhite.r, SoftWhite.g, SoftWhite.b, 0.28f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        Shadow cardShadow = EnsureComponent<Shadow>(card.gameObject);
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
        cardShadow.effectDistance = new Vector2(0f, -18f);

        RectTransform sheen = GetOrCreateImage(
            card,
            "CardSheen",
            new Color(Acid.r, Acid.g, Acid.b, 0.08f));
        ConfigureCenteredRect(sheen, new Vector2(0f, 470f), new Vector2(820f, 86f));
        ApplyRoundedStyle(
            sheen.GetComponent<Image>(),
            new Color(Acid.r, Acid.g, Acid.b, 0.08f));
        sheen.GetComponent<Image>().raycastTarget = false;
        sheen.SetAsFirstSibling();

        cardFrame.SetAsFirstSibling();
        card.SetAsLastSibling();

        RectTransform reportRail = GetOrCreateImage(card, "ReportRail", Amber);
        ConfigureCenteredRect(reportRail, new Vector2(-444f, 0f), new Vector2(12f, 1010f));
        ApplyRoundedStyle(reportRail.GetComponent<Image>(), Amber);
        reportRail.GetComponent<Image>().raycastTarget = false;

        RectTransform reportTop = GetOrCreateImage(card, "ReportTop", Acid);
        ConfigureCenteredRect(reportTop, new Vector2(0f, 532f), new Vector2(820f, 10f));
        ApplyRoundedStyle(reportTop.GetComponent<Image>(), Acid);
        reportTop.GetComponent<Image>().raycastTarget = false;

        TMP_Text kicker = GetOrReparentText(card, sheen, "SuccessKicker");
        StretchFull(kicker.rectTransform, new Vector2(24f, 12f));
        ConfigureText(kicker, 23f, TextAlignmentOptions.Left, Acid, FontStyles.Bold);
        kicker.text = "G\u00d6REV RAPORU  /  01";
        kicker.rectTransform.SetAsLastSibling();

        Sprite successSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Bolum1/correcticon.png");
        RectTransform icon = GetOrCreateImage(card, "SuccessIcon", Color.white);
        ConfigureCenteredRect(icon, new Vector2(-390f, 390f), new Vector2(100f, 100f));
        Image iconImage = icon.GetComponent<Image>();
        iconImage.sprite = successSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        TMP_Text title = GetOrCreateText(card, "SuccessTitle");
        ConfigureCenteredRect(title.rectTransform, new Vector2(-20f, 340f), new Vector2(500f, 190f));
        ConfigureText(title, 76f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
        UseDisplayFont(title);
        title.enableAutoSizing = true;
        title.fontSizeMin = 58f;
        title.fontSizeMax = 78f;
        title.lineSpacing = -8f;
        title.text = "\u00c7ANTA\nHAZIR";

        Color scoreColor = new Color(Amber.r, Amber.g, Amber.b, 0.92f);
        RectTransform scorePlate = GetOrCreateImage(card, "ScorePlate", scoreColor);
        ConfigureCenteredRect(scorePlate, new Vector2(315f, 332f), new Vector2(230f, 212f));
        scorePlate.localRotation = Quaternion.identity;
        ApplyRoundedStyle(scorePlate.GetComponent<Image>(), scoreColor);
        scorePlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text score = GetOrCreateText(scorePlate, "SuccessScore");
        ConfigureCenteredRect(score.rectTransform, new Vector2(0f, 18f), new Vector2(210f, 104f));
        ConfigureText(score, 58f, TextAlignmentOptions.Center, SoftWhite, FontStyles.Bold);
        UseDisplayFont(score);
        score.text = "13/13";

        TMP_Text scoreLabel = GetOrCreateText(scorePlate, "SuccessScoreLabel");
        ConfigureCenteredRect(scoreLabel.rectTransform, new Vector2(0f, -60f), new Vector2(210f, 44f));
        ConfigureText(scoreLabel, 18f, TextAlignmentOptions.Center, SoftWhite, FontStyles.Bold);
        scoreLabel.text = "DO\u011eRU E\u015eYA";

        TMP_Text subtitle = GetOrCreateText(card, "SuccessSubtitle");
        ConfigureCenteredRect(subtitle.rectTransform, new Vector2(-18f, 168f), new Vector2(800f, 64f));
        ConfigureText(subtitle, 31f, TextAlignmentOptions.Left, Amber, FontStyles.Bold);
        subtitle.text = "HAZIRLIK KONTROL\u00dc TAMAMLANDI";

        TMP_Text body = GetOrCreateText(card, "SuccessBody");
        ConfigureCenteredRect(body.rectTransform, new Vector2(0f, 62f), new Vector2(800f, 110f));
        ConfigureText(body, 28f, TextAlignmentOptions.Left, Muted, FontStyles.Normal);
        body.lineSpacing = 5f;
        body.text = "13 temel ihtiyac\u0131 do\u011fru ay\u0131rd\u0131n.\nAfet \u00f6ncesi haz\u0131rl\u0131k ad\u0131m\u0131 tamam.";

        Color tipColor = new Color(DeepGreen.r, DeepGreen.g, DeepGreen.b, 0.58f);
        RectTransform tipBand = GetOrCreateImage(card, "SafetyTip", tipColor);
        ConfigureCenteredRect(tipBand, new Vector2(0f, -92f), new Vector2(820f, 154f));
        ApplyRoundedStyle(tipBand.GetComponent<Image>(), tipColor);
        tipBand.GetComponent<Image>().raycastTarget = false;

        RectTransform tipAccent = GetOrCreateImage(tipBand, "TipAccent", Acid);
        ConfigureCenteredRect(tipAccent, new Vector2(-390f, 0f), new Vector2(10f, 112f));
        ApplyRoundedStyle(tipAccent.GetComponent<Image>(), Acid);
        tipAccent.GetComponent<Image>().raycastTarget = false;

        TMP_Text tip = GetOrCreateText(tipBand, "SafetyTipText");
        StretchFull(tip.rectTransform, new Vector2(38f, 20f));
        ConfigureText(tip, 26f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
        tip.lineSpacing = 4f;
        tip.text = "SAHA NOTU\n\u00c7antan\u0131 kap\u0131ya yak\u0131n, kolay ula\u015f\u0131lan bir yerde tut.";

        RectTransform stamp = GetOrCreateImage(card, "CompletionStamp", Acid);
        ConfigureCenteredRect(stamp, new Vector2(275f, -260f), new Vector2(268f, 98f));
        stamp.localRotation = Quaternion.identity;
        ApplyRoundedStyle(stamp.GetComponent<Image>(), new Color(Acid.r, Acid.g, Acid.b, 0.94f));
        stamp.GetComponent<Image>().raycastTarget = false;

        TMP_Text stampText = GetOrCreateText(stamp, "CompletionStampText");
        StretchFull(stampText.rectTransform, new Vector2(12f, 8f));
        ConfigureText(stampText, 34f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        stampText.text = "HAZIR";

        Color buttonColor = new Color(Amber.r, Amber.g, Amber.b, 0.96f);
        Button continueButton = GetOrCreateButton(card, "ContinueButton", buttonColor);
        ConfigureCenteredRect(continueButton.GetComponent<RectTransform>(), new Vector2(0f, -442f), new Vector2(820f, 118f));
        ConfigureButtonColors(continueButton, buttonColor);
        ConfigureButtonText(continueButton, "B\u00d6L\u00dcM 2'YE GE\u00c7", 34f, SoftWhite);
        TMP_Text continueLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (continueLabel != null)
        {
            continueLabel.alignment = TextAlignmentOptions.Left;
            continueLabel.margin = new Vector4(38f, 0f, 140f, 0f);
        }

        RectTransform continueArrowPlate = GetOrCreateImage(continueButton.transform, "ArrowPlate", Acid);
        ConfigureCenteredRect(continueArrowPlate, new Vector2(351f, 0f), new Vector2(94f, 94f));
        ApplyRoundedStyle(continueArrowPlate.GetComponent<Image>(), Acid);
        continueArrowPlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text continueArrow = GetOrCreateText(continueArrowPlate, "Arrow");
        StretchFull(continueArrow.rectTransform, new Vector2(8f, 8f));
        ConfigureText(continueArrow, 50f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        continueArrow.text = ">";
        ClearPersistentListeners(continueButton);
        UnityEventTools.AddPersistentListener(continueButton.onClick, manager.ContinueToNextScene);

        success.SetAsLastSibling();
        success.gameObject.SetActive(false);
    }

    private static void ConfigureIntroStep(
        Transform parent,
        string name,
        Vector2 position,
        string number,
        string title,
        string detail)
    {
        Color stepColor = new Color(DeepGreen.r, DeepGreen.g, DeepGreen.b, 0.72f);
        RectTransform step = GetOrCreateImage(parent, name, stepColor);
        ConfigureCenteredRect(step, position, new Vector2(270f, 116f));
        ApplyRoundedStyle(step.GetComponent<Image>(), stepColor);
        step.GetComponent<Image>().raycastTarget = false;

        RectTransform numberPlate = GetOrCreateImage(step, "NumberPlate", Amber);
        ConfigureCenteredRect(numberPlate, new Vector2(-100f, 0f), new Vector2(54f, 92f));
        ApplyRoundedStyle(numberPlate.GetComponent<Image>(), new Color(Amber.r, Amber.g, Amber.b, 0.94f));
        numberPlate.GetComponent<Image>().raycastTarget = false;

        TMP_Text numberText = GetOrCreateText(numberPlate, "Number");
        StretchFull(numberText.rectTransform, new Vector2(4f, 4f));
        ConfigureText(numberText, 28f, TextAlignmentOptions.Center, Ink, FontStyles.Bold);
        numberText.text = number;

        TMP_Text titleText = GetOrCreateText(step, "Title");
        ConfigureCenteredRect(titleText.rectTransform, new Vector2(25f, 22f), new Vector2(176f, 36f));
        ConfigureText(titleText, 21f, TextAlignmentOptions.Left, SoftWhite, FontStyles.Bold);
        titleText.text = title;

        TMP_Text detailText = GetOrCreateText(step, "Detail");
        ConfigureCenteredRect(detailText.rectTransform, new Vector2(25f, -22f), new Vector2(176f, 36f));
        ConfigureText(detailText, 18f, TextAlignmentOptions.Left, Muted, FontStyles.Normal);
        detailText.text = detail;
    }

    private static void ConfigureHintSystem(Bolum1HintSystem hintSystem)
    {
        if (hintSystem == null)
            return;

        SerializedObject hintObject = new SerializedObject(hintSystem);
        hintObject.FindProperty("idleTimeBeforeHint").floatValue = 6.5f;
        hintObject.FindProperty("delayBetweenPulses").floatValue = 1.5f;
        hintObject.FindProperty("pulseScale").floatValue = 1.28f;
        hintObject.FindProperty("pulseDuration").floatValue = 0.42f;
        hintObject.FindProperty("pulseCount").intValue = 2;
        hintObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureLighting()
    {
        GameObject items = FindSceneObject("Items");
        if (items == null)
            return;

        Renderer[] renderers = items.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        GameObject lightObject = FindSceneObject("Bolum1_TableFillLight");
        if (lightObject == null)
        {
            lightObject = new GameObject("Bolum1_TableFillLight", typeof(Light));
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Bolum 1 Table Light");
        }

        lightObject.transform.position = bounds.center + Vector3.up * 2.4f;
        Light light = EnsureComponent<Light>(lightObject);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.62f, 1f);
        light.intensity = 1.05f;
        light.range = Mathf.Max(5f, bounds.extents.magnitude * 2.2f);
        light.shadows = LightShadows.None;
    }

    private static void EnsureVersionMarker(Transform canvas)
    {
        if (FindDirectChildRect(canvas, PolishVersionMarker) != null)
            return;

        GameObject marker = new GameObject(PolishVersionMarker, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(marker, "Create Bolum 1 UI version marker");
        marker.transform.SetParent(canvas, false);
        marker.SetActive(false);
        SetLayerRecursively(marker, 5);
    }

    private static RectTransform GetOrCreateImage(Transform parent, string name, Color color)
    {
        RectTransform rect = FindDirectChildRect(parent, name);
        if (rect == null)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(target, $"Create {name}");
            target.transform.SetParent(parent, false);
            rect = target.GetComponent<RectTransform>();
        }

        Image image = EnsureComponent<Image>(rect.gameObject);
        image.color = color;
        SetLayerRecursively(rect.gameObject, 5);
        return rect;
    }

    private static RectTransform GetOrReparentImage(
        Transform searchRoot,
        Transform parent,
        string name,
        Color color)
    {
        GetOrReparentRect(searchRoot, parent, name);
        return GetOrCreateImage(parent, name, color);
    }

    private static TMP_FontAsset GetOrCreateFontAsset(
        string sourcePath,
        string assetPath,
        string assetName)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"Bolum1QualityPolisher: Font source missing at {sourcePath}");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            96,
            10,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            false);
        if (fontAsset == null)
            return null;

        const string uiCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "\u00c7\u011e\u0130\u00d6\u015e\u00dc\u00e7\u011f\u0131\u00f6\u015f\u00fc\u2019";

        fontAsset.name = assetName;
        if (!fontAsset.TryAddCharacters(uiCharacters, out string missingCharacters))
            Debug.LogWarning($"Bolum1QualityPolisher: Missing glyphs in {assetName}: {missingCharacters}");

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.material.name = $"{assetName} Material";
        fontAsset.material.hideFlags = HideFlags.None;

        AssetDatabase.CreateAsset(fontAsset, assetPath);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas == null || AssetDatabase.Contains(atlas))
                continue;

            atlas.name = $"{assetName} Atlas";
            atlas.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }

        if (!AssetDatabase.Contains(fontAsset.material))
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static void ApplyRoundedStyle(Image image, Color color)
    {
        image.color = color;
        if (roundedSprite == null)
            roundedSprite = GetOrCreateRoundedSprite();

        if (roundedSprite == null)
            return;

        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private static Sprite GetOrCreateRoundedSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        if (sprite != null)
            return sprite;

        const int size = 128;
        const float radius = 28f;
        float halfSize = (size - 1f) * 0.5f;
        float innerExtent = halfSize - radius;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x - halfSize) - innerExtent;
                float qy = Mathf.Abs(y - halfSize) - innerExtent;
                float outsideX = Mathf.Max(qx, 0f);
                float outsideY = Mathf.Max(qy, 0f);
                float signedDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) +
                                       Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - signedDistance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        texture.name = "Bolum1 Rounded 28";
        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        Directory.CreateDirectory(Path.GetDirectoryName(RoundedSpritePath));
        File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(RoundedSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name)
    {
        RectTransform rect = FindDirectChildRect(parent, name);
        TMP_Text text;
        if (rect == null)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(target, $"Create {name}");
            target.transform.SetParent(parent, false);
            rect = target.GetComponent<RectTransform>();
            text = target.GetComponent<TMP_Text>();
        }
        else
        {
            text = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
        }

        TMP_FontAsset defaultFont = regularFont != null ? regularFont : sharedFont;
        if (defaultFont != null)
            text.font = defaultFont;

        text.raycastTarget = false;
        SetLayerRecursively(rect.gameObject, 5);
        return text;
    }

    private static TMP_Text GetOrReparentText(Transform searchRoot, Transform parent, string name)
    {
        GetOrReparentRect(searchRoot, parent, name);
        return GetOrCreateText(parent, name);
    }

    private static RectTransform GetOrReparentRect(Transform searchRoot, Transform parent, string name)
    {
        RectTransform rect = FindChildRect(searchRoot, name);
        if (rect != null && rect.parent != parent)
            Undo.SetTransformParent(rect, parent, $"Move {name} into its layout container");

        return rect;
    }

    private static Button GetOrCreateButton(Transform parent, string name, Color color)
    {
        RectTransform rect = FindDirectChildRect(parent, name);
        if (rect == null)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(target, $"Create {name}");
            target.transform.SetParent(parent, false);
            rect = target.GetComponent<RectTransform>();
        }

        Image image = EnsureComponent<Image>(rect.gameObject);
        ApplyRoundedStyle(image, color);
        image.raycastTarget = true;
        Button button = EnsureComponent<Button>(rect.gameObject);
        button.targetGraphic = image;

        TMP_Text label = GetOrCreateText(rect, "Label");
        StretchFull(label.rectTransform, new Vector2(24f, 12f));
        SetLayerRecursively(rect.gameObject, 5);
        return button;
    }

    private static void ConfigureButtonText(Button button, string label, float fontSize, Color color)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        ConfigureText(text, fontSize, TextAlignmentOptions.Center, color, FontStyles.Bold);
        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize - 8f;
        text.fontSizeMax = fontSize;
    }

    private static void ConfigureButtonColors(Button button, Color baseColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        Shadow shadow = EnsureComponent<Shadow>(button.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
        shadow.effectDistance = new Vector2(0f, -8f);
    }

    private static void ClearPersistentListeners(Button button)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
    }

    private static void ConfigureText(TMP_Text text, float size, TextAlignmentOptions alignment, Color color, FontStyles style)
    {
        bool useSemibold = (style & FontStyles.Bold) != 0;
        TMP_FontAsset selectedFont = useSemibold ? semiboldFont : regularFont;
        if (selectedFont == null)
            selectedFont = sharedFont;

        if (selectedFont != null)
            text.font = selectedFont;

        text.fontSize = size;
        text.fontSizeMin = size;
        text.fontSizeMax = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;
        text.characterSpacing = 0f;
        text.wordSpacing = 0f;
        text.lineSpacing = 0f;
        text.paragraphSpacing = 0f;
        text.extraPadding = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static void UseDisplayFont(TMP_Text text)
    {
        if (boldFont != null)
            text.font = boldFont;

        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
    }

    private static void ConfigureCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureTopAnchoredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void StretchFull(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-inset.x * 2f, -inset.y * 2f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static RectTransform FindChildRect(Transform root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
                return rect;
        }

        return null;
    }

    private static RectTransform FindDirectChildRect(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child as RectTransform;
    }

    private static T FindSceneComponent<T>(string objectName) where T : Component
    {
        GameObject target = FindSceneObject(objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject.name == objectName &&
                sceneObject.scene.IsValid() &&
                sceneObject.hideFlags == HideFlags.None)
            {
                return sceneObject;
            }
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
