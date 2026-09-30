using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DepremUIDesignSystem
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/bolum1.unity",
        "Assets/Scenes/Bolum2.unity",
        "Assets/Scenes/Bolum3.unity",
        "Assets/Scenes/Bolum4.unity"
    };

    private static DepremUITheme theme;

    [MenuItem("Tools/Deprem UI/Apply Theme To Open Scene")]
    public static void ApplyToOpenScene()
    {
        theme = DepremUIThemeStore.LoadOrCreate();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        ApplyScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"DepremUIDesignSystem: Theme applied to {scene.name}.");
    }

    [MenuItem("Tools/Deprem UI/Apply Theme To All Game Scenes")]
    public static void ApplyToAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ApplyAllScenesInternal();
    }

    public static void ApplyAllScenesFromBatch()
    {
        ApplyAllScenesInternal();
    }

    public static void CaptureAllScenesFromBatch()
    {
        ApplyAllScenesInternal();
        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            CaptureSceneStates(scene);
        }
    }

    private static void ApplyAllScenesInternal()
    {
        theme = DepremUIThemeStore.LoadOrCreate();
        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ApplyScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("DepremUIDesignSystem: Shared UI theme applied to all four game scenes.");
    }

    private static void ApplyScene(Scene scene)
    {
        theme = theme != null ? theme : DepremUIThemeStore.LoadOrCreate();

        if (scene.name.Equals("bolum1", StringComparison.OrdinalIgnoreCase))
        {
            Bolum1QualityPolisher.Polish();
            ApplySharedTypography(scene);
            return;
        }

        ApplySharedCanvasRules(scene);
        ApplySharedTypography(scene);

        if (scene.name.Equals("Bolum2", StringComparison.OrdinalIgnoreCase))
            ApplyBolum2(scene);
        else if (scene.name.Equals("Bolum3", StringComparison.OrdinalIgnoreCase))
            ApplyBolum3(scene);
        else if (scene.name.Equals("Bolum4", StringComparison.OrdinalIgnoreCase))
            ApplyBolum4(scene);
    }

    private static void ApplySharedCanvasRules(Scene scene)
    {
        foreach (Canvas canvas in FindSceneComponents<Canvas>(scene))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
                canvasRect.localRotation = Quaternion.identity;
            }

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = theme.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = theme.matchWidthOrHeight;
        }
    }

    private static void ApplySharedTypography(Scene scene)
    {
        foreach (Canvas canvas in FindSceneComponents<Canvas>(scene))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                bool display = text.fontSize >= 48f || ContainsAny(text.name, "Title", "Score", "Number");
                bool strong = display || text.fontSize >= 25f || ContainsAny(text.name, "Label", "Progress", "Warning");
                TMP_FontAsset font = display ? theme.boldFont : strong ? theme.semiboldFont : theme.regularFont;
                if (font != null)
                    text.font = font;

                text.fontStyle = FontStyles.Normal;
                text.fontWeight = FontWeight.Regular;
                text.characterSpacing = 0f;
                text.wordSpacing = 0f;
                text.extraPadding = true;
                text.raycastTarget = false;
            }
        }
    }

    private static void ApplyBolum2(Scene scene)
    {
        Canvas canvas = FindSceneComponent<Canvas>(scene, "Bolum2Canvas");
        if (canvas == null)
            return;

        List<LevelManager> levelManagers = FindSceneComponents<LevelManager>(scene);
        if (levelManagers.Count > 0)
            levelManagers[0].ilerlemeMetniFormat = "Sabitlenen eşya: {0}/{1}";

        RectTransform instruction = FindRect(scene, "Bolum2InstructionPanel");
        if (instruction != null)
        {
            ConfigureTopRect(instruction, new Vector2(0f, -54f), new Vector2(988f, 238f));
            ApplyGlass(instruction.gameObject, theme.panelStrong, false);
            AddGlassHighlight(instruction, 900f, 4f, 108f);
            AddRail(instruction, "InstructionRail", new Vector2(-487f, 0f), new Vector2(14f, 214f), theme.warning);

            RectTransform chapterPlate = GetOrCreateImage(instruction, "ChapterPlate", theme.primary);
            ConfigureCenteredRect(chapterPlate, new Vector2(-411f, 0f), new Vector2(112f, 166f));
            ApplyRoundedImage(chapterPlate.GetComponent<Image>(), theme.primary);
            TMP_Text chapterNumber = GetOrCreateText(chapterPlate, "ChapterNumber");
            StretchFull(chapterNumber.rectTransform, new Vector2(8f, 8f));
            StyleText(chapterNumber, 54f, TextAlignmentOptions.Center, theme.ink, true, 48f);
            chapterNumber.text = "02";

            TMP_Text instructionText = instruction.GetComponentInChildren<TMP_Text>(true);
            if (instructionText != null && instructionText != chapterNumber)
            {
                ConfigureCenteredRect(instructionText.rectTransform, new Vector2(76f, 0f), new Vector2(720f, 174f));
                StyleText(instructionText, 32f, TextAlignmentOptions.Left, theme.textPrimary, true, 25f);
                instructionText.text =
                    "<size=19><color=#43D9FF>AKTIF GÖREV  /  SABİTLEME</color></size>\n" +
                    "<size=40><b>ODAYI GÜVENLİ YAP</b></size>\n" +
                    "<size=23><color=#A8C2DB>Kırmızı uyarı veren eşyaları sabitle.</color></size>";
            }
        }

        RectTransform progressPanel = FindRect(scene, "Bolum2ProgressPanel");
        if (progressPanel != null)
        {
            ConfigureTopRect(progressPanel, new Vector2(0f, -318f), new Vector2(620f, 118f));
            ApplyGlass(progressPanel.gameObject, theme.panel, false);
            AddRail(progressPanel, "ProgressRail", new Vector2(-299f, 0f), new Vector2(10f, 92f), theme.accent);

            TMP_Text progressKicker = GetOrCreateText(progressPanel, "ProgressKicker");
            ConfigureCenteredRect(progressKicker.rectTransform, new Vector2(-3f, 25f), new Vector2(540f, 28f));
            StyleText(progressKicker, 17f, TextAlignmentOptions.Left, theme.accent, true);
            progressKicker.text = "SABİTLEME İLERLEMESİ";

            TMP_Text progressText = FindDirectComponent<TMP_Text>(progressPanel, "ProgressText");
            if (progressText != null)
            {
                ConfigureCenteredRect(progressText.rectTransform, new Vector2(-3f, -20f), new Vector2(540f, 50f));
                StyleText(progressText, 30f, TextAlignmentOptions.Left, theme.textPrimary, true, 24f);
            }
        }

        RectTransform success = FindRect(scene, "Bolum2SuccessPanel");
        if (success != null)
            ConfigureBolum2Success(success);
    }

    private static void ConfigureBolum2Success(RectTransform success)
    {
        bool wasActive = success.gameObject.activeSelf;
        success.gameObject.SetActive(true);
        StretchFull(success, Vector2.zero);
        Image overlay = EnsureComponent<Image>(success.gameObject);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.98f);
        overlay.raycastTarget = true;

        RectTransform card = GetOrCreateImage(success, "Bolum2SuccessCard", theme.panelStrong);
        ConfigureCenteredRect(card, new Vector2(0f, -10f), new Vector2(940f, 1080f));
        ApplyGlass(card.gameObject, theme.panelStrong, true);
        AddRail(card, "ResultRail", new Vector2(-444f, 0f), new Vector2(12f, 974f), theme.success);
        AddRail(card, "ResultTop", new Vector2(0f, 514f), new Vector2(820f, 10f), theme.accent);

        TMP_Text kicker = GetOrCreateText(card, "ResultKicker");
        ConfigureCenteredRect(kicker.rectTransform, new Vector2(0f, 450f), new Vector2(800f, 46f));
        StyleText(kicker, 22f, TextAlignmentOptions.Left, theme.accent, true);
        kicker.text = "GÖREV RAPORU  /  BÖLÜM 02";

        TMP_Text title = GetOrCreateText(card, "ResultTitle");
        ConfigureCenteredRect(title.rectTransform, new Vector2(0f, 320f), new Vector2(800f, 170f));
        StyleText(title, 70f, TextAlignmentOptions.Left, theme.textPrimary, true, 54f);
        title.text = "ODA\nGÜVENLİ";

        TMP_Text original = FindChildRect(success, "SuccessText")?.GetComponent<TMP_Text>();
        if (original != null)
        {
            original.rectTransform.SetParent(card, false);
            ConfigureCenteredRect(original.rectTransform, new Vector2(0f, 126f), new Vector2(800f, 120f));
            StyleText(original, 29f, TextAlignmentOptions.Left, theme.textSecondary, false, 24f);
            original.text = "Tüm sabitleme noktaları tamamlandı.\nÇocuk artık güvende.";
        }

        RectTransform note = GetOrCreateImage(card, "SafetyNote", theme.panelSoft);
        ConfigureCenteredRect(note, new Vector2(0f, -72f), new Vector2(820f, 166f));
        ApplyRoundedImage(note.GetComponent<Image>(), theme.panelSoft);
        AddRail(note, "NoteRail", new Vector2(-390f, 0f), new Vector2(10f, 126f), theme.accent);
        TMP_Text noteText = GetOrCreateText(note, "SafetyNoteText");
        StretchFull(noteText.rectTransform, new Vector2(38f, 20f));
        StyleText(noteText, 25f, TextAlignmentOptions.Left, theme.textPrimary, true, 21f);
        noteText.text = "SAHA NOTU\nDolap, raf ve devrilebilecek eşyaların bağlantılarını düzenli kontrol et.";

        RectTransform status = GetOrCreateImage(card, "CompletionStatus", theme.success);
        ConfigureCenteredRect(status, new Vector2(0f, -318f), new Vector2(820f, 120f));
        ApplyRoundedImage(status.GetComponent<Image>(), theme.success);
        TMP_Text statusText = GetOrCreateText(status, "CompletionStatusText");
        StretchFull(statusText.rectTransform, new Vector2(34f, 16f));
        StyleText(statusText, 31f, TextAlignmentOptions.Left, theme.ink, true, 26f);
        statusText.text = "BÖLÜM 3'E GEÇİLİYOR  >";

        success.SetAsLastSibling();
        success.gameObject.SetActive(wasActive);
    }

    private static void ApplyBolum3(Scene scene)
    {
        RectTransform warning = FindRect(scene, "WarningPanel");
        if (warning != null)
        {
            ConfigureTopRect(warning, new Vector2(0f, -62f), new Vector2(980f, 238f));
            ApplyGlass(warning.gameObject, theme.panelStrong, true);
            AddGlassHighlight(warning, 890f, 4f, 108f);
            AddRail(warning, "WarningRail", new Vector2(-482f, 0f), new Vector2(14f, 210f), theme.danger);

            TMP_Text warningKicker = GetOrCreateText(warning, "WarningKicker");
            ConfigureCenteredRect(warningKicker.rectTransform, new Vector2(-78f, 68f), new Vector2(720f, 30f));
            StyleText(warningKicker, 18f, TextAlignmentOptions.Left, theme.danger, true);
            warningKicker.text = "ACİL DURUM  /  BÖLÜM 03";

            TMP_Text warningText = FindDirectComponent<TMP_Text>(warning, "WarningText");
            if (warningText != null)
            {
                ConfigureCenteredRect(warningText.rectTransform, new Vector2(-78f, -12f), new Vector2(720f, 126f));
                StyleText(warningText, 31f, TextAlignmentOptions.Left, theme.textPrimary, true, 24f);
                warningText.text = "<b>DEPREM BAŞLADI</b>\nGüvenli alana dokun. ÇÖK - KAPAN - TUTUN.";
            }

            Image mascot = FindDirectRect(warning, "MascotImage")?.GetComponent<Image>();
            if (mascot != null)
            {
                ConfigureCenteredRect(mascot.rectTransform, new Vector2(390f, 0f), new Vector2(138f, 138f));
                mascot.preserveAspect = true;
                mascot.raycastTarget = false;
                mascot.gameObject.SetActive(mascot.sprite != null && mascot.color.a > 0.01f);
            }
        }

        RectTransform success = FindRect(scene, "SuccessPanel");
        if (success != null)
            ConfigureBolum3Result(success, true);

        RectTransform fail = FindRectByTrimmedName(scene, "FailPanel");
        if (fail != null)
            ConfigureBolum3Result(fail, false);
    }

    private static void ConfigureBolum3Result(RectTransform panel, bool isSuccess)
    {
        bool wasActive = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);
        StretchFull(panel, Vector2.zero);
        Image overlay = EnsureComponent<Image>(panel.gameObject);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.98f);
        overlay.raycastTarget = true;

        string cardName = isSuccess ? "Bolum3SuccessCard" : "Bolum3RetryCard";
        Color semantic = isSuccess ? theme.success : theme.danger;
        RectTransform card = GetOrCreateImage(panel, cardName, theme.panelStrong);
        ConfigureCenteredRect(card, Vector2.zero, new Vector2(940f, 1040f));
        ApplyGlass(card.gameObject, theme.panelStrong, true);
        AddRail(card, "ResultRail", new Vector2(-444f, 0f), new Vector2(12f, 930f), semantic);
        AddRail(card, "ResultTop", new Vector2(0f, 494f), new Vector2(820f, 10f), theme.accent);

        TMP_Text kicker = GetOrCreateText(card, "ResultKicker");
        ConfigureCenteredRect(kicker.rectTransform, new Vector2(0f, 430f), new Vector2(800f, 44f));
        StyleText(kicker, 22f, TextAlignmentOptions.Left, theme.accent, true);
        kicker.text = isSuccess ? "GÖREV RAPORU  /  BÖLÜM 03" : "SAHA UYARISI  /  TEKRAR DENE";

        TMP_Text title = GetOrCreateText(card, "ResultTitle");
        ConfigureCenteredRect(title.rectTransform, new Vector2(0f, 280f), new Vector2(800f, 180f));
        StyleText(title, 68f, TextAlignmentOptions.Left, theme.textPrimary, true, 52f);
        title.text = isSuccess ? "GÜVENLİ\nALANDASIN" : "HIZLI\nDAVRAN";

        TMP_Text dynamicText = FindChildRect(panel, "SuccessText (TMP)")?.GetComponent<TMP_Text>();
        if (dynamicText != null)
        {
            dynamicText.rectTransform.SetParent(card, false);
            ConfigureCenteredRect(dynamicText.rectTransform, new Vector2(0f, 40f), new Vector2(800f, 250f));
            StyleText(dynamicText, 29f, TextAlignmentOptions.Left, theme.textSecondary, true, 23f);
            dynamicText.text = isSuccess
                ? "Deprem sırasında güvenli alana ulaştın.\n\nPUAN  100 / 100"
                : "Güvenli alana zamanında ulaşamadık.\nBir sonraki denemede güvenli noktaya hemen dokun.\n\nÇÖK - KAPAN - TUTUN";
        }

        RectTransform rule = GetOrCreateImage(card, "SafetyRule", theme.panelSoft);
        ConfigureCenteredRect(rule, new Vector2(0f, -190f), new Vector2(820f, 176f));
        ApplyRoundedImage(rule.GetComponent<Image>(), theme.panelSoft);
        AddRail(rule, "RuleRail", new Vector2(-390f, 0f), new Vector2(10f, 134f), semantic);
        TMP_Text ruleText = GetOrCreateText(rule, "SafetyRuleText");
        StretchFull(ruleText.rectTransform, new Vector2(38f, 22f));
        StyleText(ruleText, 25f, TextAlignmentOptions.Left, theme.textPrimary, true, 21f);
        ruleText.text = isSuccess
            ? "DOĞRU DAVRANIŞ\nÇök, kapan ve sağlam bir noktaya tutun."
            : "SONRAKİ DENEME\nGüvenli noktayı görür görmez dokun.";

        panel.SetAsLastSibling();
        panel.gameObject.SetActive(wasActive);
    }

    private static void ApplyBolum4(Scene scene)
    {
        ConfigureBolum4MissionHud(scene);
        ConfigureBolum4InfoPanel(scene, "StairInfoPanel", new Vector2(0f, 96f), new Vector2(970f, 190f), theme.warning);
        ConfigureBolum4InfoPanel(scene, "StairInfoBubble", new Vector2(0f, 310f), new Vector2(900f, 156f), theme.accent);
        ConfigureBolum4CardGame(scene);
        ConfigureBolum4Final(scene);

        RectTransform fade = FindRect(scene, "FadePanel");
        if (fade != null)
            StretchFull(fade, Vector2.zero);
        RectTransform transition = FindRect(scene, "DoorTransitionPanel");
        if (transition != null)
            StretchFull(transition, Vector2.zero);
    }

    private static void ConfigureBolum4MissionHud(Scene scene)
    {
        RectTransform missionTextRect = FindRect(scene, "Deprem");
        if (missionTextRect == null)
            return;

        Transform canvas = missionTextRect.GetComponentInParent<Canvas>()?.transform;
        if (canvas == null)
            return;

        RectTransform hud = GetOrCreateImage(canvas, "Bolum4MissionHud", theme.panelStrong);
        ConfigureTopRect(hud, new Vector2(0f, -54f), new Vector2(988f, 188f));
        ApplyGlass(hud.gameObject, theme.panelStrong, false);
        AddGlassHighlight(hud, 900f, 4f, 84f);
        AddRail(hud, "MissionRail", new Vector2(-487f, 0f), new Vector2(14f, 164f), theme.primary);

        if (missionTextRect.parent != hud)
            missionTextRect.SetParent(hud, false);
        StretchFull(missionTextRect, Vector2.zero);

        RectTransform chapterPlate = GetOrCreateImage(hud, "ChapterPlate", theme.primary);
        ConfigureCenteredRect(chapterPlate, new Vector2(-411f, 0f), new Vector2(112f, 132f));
        ApplyRoundedImage(chapterPlate.GetComponent<Image>(), theme.primary);
        TMP_Text chapter = GetOrCreateText(chapterPlate, "ChapterNumber");
        StretchFull(chapter.rectTransform, new Vector2(8f, 8f));
        StyleText(chapter, 50f, TextAlignmentOptions.Center, theme.ink, true, 44f);
        chapter.text = "04";

        TMP_Text mission = missionTextRect.GetComponent<TMP_Text>();
        if (mission != null)
        {
            StyleText(mission, 34f, TextAlignmentOptions.Left, theme.textPrimary, true, 27f);
            mission.margin = new Vector4(174f, 20f, 36f, 14f);
            mission.text =
                "<size=18><color=#43D9FF>AKTİF GÖREV  /  TAHLİYE</color></size>\n" +
                "SARSINTI SONRASI DOĞRU ADIMLAR";
        }
    }

    private static void ConfigureBolum4InfoPanel(
        Scene scene,
        string objectName,
        Vector2 bottomPosition,
        Vector2 size,
        Color semantic)
    {
        RectTransform panel = FindRect(scene, objectName);
        if (panel == null)
            return;

        ConfigureBottomRect(panel, bottomPosition, size);
        ApplyGlass(panel.gameObject, theme.panelStrong, false);
        AddRail(panel, "InfoRail", new Vector2(-size.x * 0.5f + 12f, 0f), new Vector2(10f, size.y - 38f), semantic);
        TMP_Text text = panel.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            StretchFull(text.rectTransform, new Vector2(42f, 22f));
            StyleText(text, 28f, TextAlignmentOptions.Left, theme.textPrimary, true, 22f);
        }
    }

    private static void ConfigureBolum4CardGame(Scene scene)
    {
        RectTransform panel = FindRect(scene, "CardGamePanel");
        if (panel == null)
            return;

        bool wasActive = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);
        StretchFull(panel, Vector2.zero);
        Image overlay = EnsureComponent<Image>(panel.gameObject);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.94f);
        overlay.raycastTarget = true;

        RectTransform surface = GetOrCreateImage(panel, "CardGameSurface", theme.panelStrong);
        ConfigureCenteredRect(surface, new Vector2(0f, -18f), new Vector2(980f, 1540f));
        ApplyGlass(surface.gameObject, theme.panelStrong, true);
        AddRail(surface, "GameRail", new Vector2(-464f, 0f), new Vector2(12f, 1430f), theme.warning);
        AddRail(surface, "GameTop", new Vector2(0f, 742f), new Vector2(860f, 10f), theme.accent);

        MoveInto(panel, surface, "TitleText");
        MoveInto(panel, surface, "AnswerSlots");
        MoveInto(panel, surface, "CardsArea");
        MoveInto(panel, surface, "FeedBackText");

        TMP_Text kicker = GetOrCreateText(surface, "CardGameKicker");
        ConfigureCenteredRect(kicker.rectTransform, new Vector2(0f, 668f), new Vector2(840f, 38f));
        StyleText(kicker, 20f, TextAlignmentOptions.Left, theme.accent, true);
        kicker.text = "KARAR SIRASI  /  BÖLÜM 04";

        TMP_Text title = FindDirectComponent<TMP_Text>(surface, "TitleText");
        if (title != null)
        {
            ConfigureCenteredRect(title.rectTransform, new Vector2(0f, 555f), new Vector2(840f, 150f));
            StyleText(title, 54f, TextAlignmentOptions.Left, theme.textPrimary, true, 42f);
            title.text = "DOĞRU ADIMLARI\nSIRALA";
        }

        TMP_Text description = GetOrCreateText(surface, "CardGameDescription");
        ConfigureCenteredRect(description.rectTransform, new Vector2(0f, 438f), new Vector2(840f, 74f));
        StyleText(description, 24f, TextAlignmentOptions.Left, theme.textSecondary, false, 20f);
        description.text = "Deprem sonrasında yapılacakları doğru sıraya yerleştir.";

        RectTransform slots = FindDirectRect(surface, "AnswerSlots");
        if (slots != null)
            ConfigureCardGrid(slots, new Vector2(0f, 130f), false);

        TMP_Text sourceLabel = GetOrCreateText(surface, "CardSourceLabel");
        ConfigureCenteredRect(sourceLabel.rectTransform, new Vector2(0f, -145f), new Vector2(840f, 34f));
        StyleText(sourceLabel, 18f, TextAlignmentOptions.Left, theme.warning, true);
        sourceLabel.text = "KARTLAR  /  DOKUN VE YERLEŞTİR";

        RectTransform cards = FindDirectRect(surface, "CardsArea");
        if (cards != null)
            ConfigureCardGrid(cards, new Vector2(0f, -420f), true);

        TMP_Text feedback = FindDirectComponent<TMP_Text>(surface, "FeedBackText");
        if (feedback != null)
        {
            ConfigureCenteredRect(feedback.rectTransform, new Vector2(0f, -688f), new Vector2(840f, 64f));
            StyleText(feedback, 25f, TextAlignmentOptions.Left, theme.textPrimary, true, 20f);
            feedback.text = "Kartları doğru sıraya diz.";
        }

        panel.SetAsLastSibling();
        panel.gameObject.SetActive(wasActive);
    }

    private static void ConfigureCardGrid(RectTransform root, Vector2 position, bool cards)
    {
        ConfigureCenteredRect(root, position, new Vector2(820f, 500f));
        GridLayoutGroup grid = EnsureComponent<GridLayoutGroup>(root.gameObject);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.cellSize = new Vector2(390f, 230f);
        grid.spacing = new Vector2(32f, 30f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        Color[] cardColors = { theme.primary, theme.success, theme.warning, theme.danger };
        for (int index = 0; index < root.childCount; index++)
        {
            RectTransform child = root.GetChild(index) as RectTransform;
            if (child == null)
                continue;

            child.localScale = Vector3.one;
            child.localRotation = Quaternion.identity;
            child.sizeDelta = grid.cellSize;
            Image image = EnsureComponent<Image>(child.gameObject);
            Color color = cards
                ? new Color(cardColors[Mathf.Min(index, cardColors.Length - 1)].r,
                    cardColors[Mathf.Min(index, cardColors.Length - 1)].g,
                    cardColors[Mathf.Min(index, cardColors.Length - 1)].b, 0.92f)
                : new Color(theme.panelSoft.r, theme.panelSoft.g, theme.panelSoft.b, 0.82f);
            ApplyRoundedImage(image, color);
            Outline outline = EnsureComponent<Outline>(child.gameObject);
            outline.effectColor = cards ? theme.glassHighlight : theme.glassOutline;
            outline.effectDistance = theme.outlineDistance;

            TMP_Text text = child.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                StretchFull(text.rectTransform, new Vector2(20f, 18f));
                StyleText(text, cards ? 25f : 36f, TextAlignmentOptions.Center,
                    cards ? theme.textPrimary : theme.accent, true, cards ? 19f : 28f);
                if (cards)
                {
                    string[] labels =
                    {
                        "Kendini kontrol et",
                        "Büyüklerini çağır",
                        "Çantanı al",
                        "Asansöre binme"
                    };
                    text.text = labels[Mathf.Min(index, labels.Length - 1)];
                }
            }
        }
    }

    private static void ConfigureBolum4Final(Scene scene)
    {
        RectTransform panel = FindRect(scene, "FinalSuccessPanel");
        if (panel == null)
            return;

        bool wasActive = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);
        StretchFull(panel, Vector2.zero);
        Image overlay = EnsureComponent<Image>(panel.gameObject);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.98f);
        overlay.raycastTarget = true;

        RectTransform box = FindDirectRect(panel, "FinalBox");
        if (box == null)
            box = GetOrCreateImage(panel, "FinalBox", theme.panelStrong);
        ConfigureCenteredRect(box, Vector2.zero, new Vector2(940f, 1240f));
        ApplyGlass(box.gameObject, theme.panelStrong, true);
        AddRail(box, "FinalRail", new Vector2(-444f, 0f), new Vector2(12f, 1120f), theme.success);
        AddRail(box, "FinalTop", new Vector2(0f, 592f), new Vector2(820f, 10f), theme.accent);

        TMP_Text kicker = GetOrCreateText(box, "FinalKicker");
        ConfigureCenteredRect(kicker.rectTransform, new Vector2(0f, 520f), new Vector2(800f, 44f));
        StyleText(kicker, 21f, TextAlignmentOptions.Left, theme.accent, true);
        kicker.text = "TAHLİYE RAPORU  /  BÖLÜM 04";

        TMP_Text title = FindDirectComponent<TMP_Text>(box, "TitleText");
        if (title != null)
        {
            ConfigureCenteredRect(title.rectTransform, new Vector2(0f, 382f), new Vector2(800f, 190f));
            StyleText(title, 66f, TextAlignmentOptions.Left, theme.textPrimary, true, 52f);
            title.text = "GÖREV\nTAMAMLANDI";
        }

        TMP_Text description = FindDirectComponent<TMP_Text>(box, "Açıklama");
        if (description != null)
        {
            ConfigureCenteredRect(description.rectTransform, new Vector2(0f, 235f), new Vector2(800f, 100f));
            StyleText(description, 28f, TextAlignmentOptions.Left, theme.textSecondary, false, 23f);
            description.text = "Deprem sonrası doğru tahliye adımlarını tamamladın.";
        }

        RectTransform achievementsBand = GetOrCreateImage(box, "AchievementsBand", theme.panelSoft);
        ConfigureCenteredRect(achievementsBand, new Vector2(0f, -42f), new Vector2(820f, 364f));
        ApplyRoundedImage(achievementsBand.GetComponent<Image>(), theme.panelSoft);
        AddRail(achievementsBand, "AchievementsRail", new Vector2(-390f, 0f), new Vector2(10f, 316f), theme.success);
        TMP_Text achievements = GetOrCreateText(achievementsBand, "AchievementsText");
        StretchFull(achievements.rectTransform, new Vector2(42f, 26f));
        StyleText(achievements, 27f, TextAlignmentOptions.Left, theme.textPrimary, true, 22f);
        achievements.lineSpacing = 8f;
        achievements.text =
            "KENDİNİ KONTROL ET\n" +
            "ASANSÖRÜ KULLANMA\n" +
            "MERDİVENDE DUVAR KENARINI İZLE\n" +
            "AÇIK VE GÜVENLİ ALANA ULAŞ";

        Button restart = FindDirectComponent<Button>(box, "RestartButton");
        if (restart != null)
        {
            RectTransform restartRect = restart.GetComponent<RectTransform>();
            ConfigureCenteredRect(restartRect, new Vector2(0f, -478f), new Vector2(820f, 122f));
            ConfigureButton(restart, theme.primary);
            TMP_Text restartText = restart.GetComponentInChildren<TMP_Text>(true);
            if (restartText != null)
            {
                StretchFull(restartText.rectTransform, new Vector2(34f, 14f));
                StyleText(restartText, 34f, TextAlignmentOptions.Left, theme.textPrimary, true, 28f);
                restartText.text = "TEKRAR OYNA  >";
            }
        }

        RectTransform legacyTitle = FindDirectRect(panel, "FinalTitleText");
        if (legacyTitle != null)
            legacyTitle.gameObject.SetActive(false);
        RectTransform legacyDescription = FindDirectRect(panel, "FinalAçıklama");
        if (legacyDescription != null)
            legacyDescription.gameObject.SetActive(false);

        FinalSuccessUI controller = panel.GetComponent<FinalSuccessUI>();
        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("achievementsText").objectReferenceValue = achievements;
            serialized.FindProperty("title").stringValue = "GÖREV\nTAMAMLANDI";
            serialized.FindProperty("description").stringValue = "Deprem sonrası doğru tahliye adımlarını tamamladın.";
            serialized.FindProperty("achievements").stringValue =
                "KENDİNİ KONTROL ET\n" +
                "ASANSÖRÜ KULLANMA\n" +
                "MERDİVENDE DUVAR KENARINI İZLE\n" +
                "AÇIK VE GÜVENLİ ALANA ULAŞ";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        panel.SetAsLastSibling();
        panel.gameObject.SetActive(wasActive);
    }

    private static void CaptureSceneStates(Scene scene)
    {
        Canvas canvas = FindSceneComponents<Canvas>(scene).Find(item => item.renderMode != RenderMode.WorldSpace);
        Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (canvas == null || camera == null)
        {
            Debug.LogWarning($"DepremUIDesignSystem: Capture skipped for {scene.name}; canvas or camera missing.");
            return;
        }

        List<CaptureState> states = BuildCaptureStates(scene);
        foreach (CaptureState state in states)
        {
            SceneStateSnapshot snapshot = SceneStateSnapshot.Capture(scene);
            try
            {
                state.Prepare();
                CaptureFrame(canvas, camera, $"{scene.name}-{state.Name}.png");
            }
            finally
            {
                snapshot.Restore();
            }
        }
    }

    private static List<CaptureState> BuildCaptureStates(Scene scene)
    {
        var states = new List<CaptureState>();
        if (scene.name.Equals("bolum1", StringComparison.OrdinalIgnoreCase))
        {
            states.Add(new CaptureState("Intro", () => SetExclusivePanels(scene, "MissionIntroPanel")));
            states.Add(new CaptureState("Gameplay", () => SetExclusivePanels(scene, null)));
            states.Add(new CaptureState("Success", () => SetExclusivePanels(scene, "SuccessPanel")));
        }
        else if (scene.name.Equals("Bolum2", StringComparison.OrdinalIgnoreCase))
        {
            states.Add(new CaptureState("Gameplay", () => SetObjectActive(scene, "Bolum2SuccessPanel", false)));
            states.Add(new CaptureState("Success", () => SetObjectActive(scene, "Bolum2SuccessPanel", true)));
        }
        else if (scene.name.Equals("Bolum3", StringComparison.OrdinalIgnoreCase))
        {
            states.Add(new CaptureState("Warning", () => SetBolum3State(scene, "WarningPanel")));
            states.Add(new CaptureState("Success", () => SetBolum3State(scene, "SuccessPanel")));
            states.Add(new CaptureState("Retry", () => SetBolum3State(scene, "FailPanel")));
        }
        else if (scene.name.Equals("Bolum4", StringComparison.OrdinalIgnoreCase))
        {
            states.Add(new CaptureState("Gameplay", () => SetBolum4State(scene, null)));
            states.Add(new CaptureState("CardGame", () => SetBolum4State(scene, "CardGamePanel")));
            states.Add(new CaptureState("Final", () => SetBolum4State(scene, "FinalSuccessPanel")));
        }

        return states;
    }

    private static void CaptureFrame(Canvas canvas, Camera camera, string fileName)
    {
        RenderMode originalMode = canvas.renderMode;
        Camera originalCanvasCamera = canvas.worldCamera;
        float originalPlaneDistance = canvas.planeDistance;
        RenderTexture originalTarget = camera.targetTexture;
        float originalAspect = camera.aspect;
        RenderTexture originalActive = RenderTexture.active;

        int width = Mathf.RoundToInt(theme.referenceResolution.x);
        int height = Mathf.RoundToInt(theme.referenceResolution.y);
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;
            camera.aspect = (float)width / height;
            camera.targetTexture = target;

            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate();

            Canvas.ForceUpdateCanvases();
            target.Create();
            RenderTexture.active = target;
            GL.Clear(true, true, camera.backgroundColor);
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            preview.Apply();

            string output = Path.Combine(Path.GetTempPath(), "Deprem-UI-Standard", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, preview.EncodeToPNG());
            Debug.Log($"DepremUIDesignSystem: Captured {output}");
        }
        finally
        {
            RenderTexture.active = originalActive;
            camera.targetTexture = originalTarget;
            camera.aspect = originalAspect;
            canvas.renderMode = originalMode;
            canvas.worldCamera = originalCanvasCamera;
            canvas.planeDistance = originalPlaneDistance;
            UnityEngine.Object.DestroyImmediate(preview);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void SetExclusivePanels(Scene scene, string activeName)
    {
        foreach (string name in new[] { "MissionIntroPanel", "SuccessPanel" })
            SetObjectActive(scene, name, name == activeName);
    }

    private static void SetBolum3State(Scene scene, string activeName)
    {
        SetObjectActive(scene, "WarningPanel", activeName == "WarningPanel");
        SetObjectActive(scene, "SuccessPanel", activeName == "SuccessPanel");
        RectTransform fail = FindRectByTrimmedName(scene, "FailPanel");
        if (fail != null)
            fail.gameObject.SetActive(activeName == "FailPanel");
    }

    private static void SetBolum4State(Scene scene, string activeName)
    {
        SetObjectActive(scene, "CardGamePanel", activeName == "CardGamePanel");
        RectTransform final = FindRect(scene, "FinalSuccessPanel");
        if (final != null)
        {
            final.gameObject.SetActive(true);
            CanvasGroup group = final.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = activeName == "FinalSuccessPanel" ? 1f : 0f;
                group.blocksRaycasts = activeName == "FinalSuccessPanel";
            }
        }
    }

    private static void SetObjectActive(Scene scene, string name, bool active)
    {
        GameObject target = FindSceneObject(scene, name);
        if (target != null)
            target.SetActive(active);
    }

    private static void ConfigureButton(Button button, Color color)
    {
        Image image = EnsureComponent<Image>(button.gameObject);
        ApplyRoundedImage(image, color);
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Shadow shadow = EnsureComponent<Shadow>(button.gameObject);
        shadow.effectColor = theme.shadow;
        shadow.effectDistance = new Vector2(0f, -8f);
    }

    private static void ApplyGlass(GameObject target, Color color, bool blocksRaycasts)
    {
        Image image = EnsureComponent<Image>(target);
        ApplyRoundedImage(image, color);
        image.raycastTarget = blocksRaycasts;

        Outline outline = EnsureComponent<Outline>(target);
        outline.effectColor = theme.glassOutline;
        outline.effectDistance = theme.outlineDistance;

        Shadow shadow = EnsureComponent<Shadow>(target);
        shadow.effectColor = theme.shadow;
        shadow.effectDistance = theme.shadowDistance;
    }

    private static void ApplyRoundedImage(Image image, Color color)
    {
        image.color = color;
        image.sprite = theme.roundedSprite;
        image.type = theme.roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private static void AddGlassHighlight(RectTransform parent, float width, float height, float y)
    {
        RectTransform highlight = GetOrCreateImage(parent, "GlassHighlight", theme.glassHighlight);
        ConfigureCenteredRect(highlight, new Vector2(0f, y), new Vector2(width, height));
        ApplyRoundedImage(highlight.GetComponent<Image>(), theme.glassHighlight);
        highlight.GetComponent<Image>().raycastTarget = false;
        highlight.SetAsFirstSibling();
    }

    private static RectTransform AddRail(
        RectTransform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        RectTransform rail = GetOrCreateImage(parent, name, color);
        ConfigureCenteredRect(rail, position, size);
        ApplyRoundedImage(rail.GetComponent<Image>(), color);
        rail.GetComponent<Image>().raycastTarget = false;
        return rail;
    }

    private static void StyleText(
        TMP_Text text,
        float size,
        TextAlignmentOptions alignment,
        Color color,
        bool strong,
        float minSize = -1f)
    {
        TMP_FontAsset font = strong ? theme.semiboldFont : theme.regularFont;
        if (size >= theme.displaySize - 8f && theme.boldFont != null)
            font = theme.boldFont;
        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.fontSizeMax = size;
        text.fontSizeMin = minSize > 0f ? minSize : size;
        text.enableAutoSizing = minSize > 0f;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.characterSpacing = 0f;
        text.wordSpacing = 0f;
        text.paragraphSpacing = 0f;
        text.margin = Vector4.zero;
        text.extraPadding = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private static RectTransform GetOrCreateImage(Transform parent, string name, Color color)
    {
        RectTransform rect = FindDirectRect(parent, name);
        if (rect == null)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            rect = target.GetComponent<RectTransform>();
        }

        Image image = EnsureComponent<Image>(rect.gameObject);
        image.color = color;
        SetLayerRecursively(rect.gameObject, 5);
        return rect;
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name)
    {
        RectTransform rect = FindDirectRect(parent, name);
        TMP_Text text;
        if (rect == null)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            rect = target.GetComponent<RectTransform>();
            text = target.GetComponent<TMP_Text>();
        }
        else
        {
            text = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
        }

        if (theme.regularFont != null)
            text.font = theme.regularFont;
        text.raycastTarget = false;
        SetLayerRecursively(rect.gameObject, 5);
        return text;
    }

    private static void MoveInto(Transform searchRoot, Transform parent, string name)
    {
        RectTransform rect = FindChildRect(searchRoot, name);
        if (rect != null && rect.parent != parent)
            rect.SetParent(parent, false);
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

    private static void ConfigureTopRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureBottomRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
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

    private static RectTransform FindRect(Scene scene, string name)
    {
        return FindSceneObject(scene, name)?.transform as RectTransform;
    }

    private static RectTransform FindRectByTrimmedName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return rect;
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child.gameObject;
            }
        }

        return null;
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

    private static RectTransform FindDirectRect(Transform parent, string name)
    {
        return parent.Find(name) as RectTransform;
    }

    private static T FindDirectComponent<T>(Transform parent, string name) where T : Component
    {
        RectTransform rect = FindDirectRect(parent, name);
        return rect != null ? rect.GetComponent<T>() : null;
    }

    private static TMP_Text FindFirstDirectTextExcluding(RectTransform panel, RectTransform excluded)
    {
        for (int index = 0; index < panel.childCount; index++)
        {
            Transform child = panel.GetChild(index);
            if (child == excluded)
                continue;
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return panel.GetComponentInChildren<TMP_Text>(true);
    }

    private static Image FindChildImageExcluding(RectTransform root, GameObject excluded)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject != excluded && image.transform.parent == root)
                return image;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        GameObject target = FindSceneObject(scene, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        var results = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (string value in values)
        {
            if (source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private sealed class CaptureState
    {
        public string Name { get; }
        public Action Prepare { get; }

        public CaptureState(string name, Action prepare)
        {
            Name = name;
            Prepare = prepare;
        }
    }

    private sealed class SceneStateSnapshot
    {
        private readonly List<GameObjectState> gameObjects = new List<GameObjectState>();
        private readonly List<CanvasGroupState> canvasGroups = new List<CanvasGroupState>();

        public static SceneStateSnapshot Capture(Scene scene)
        {
            var snapshot = new SceneStateSnapshot();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    snapshot.gameObjects.Add(new GameObjectState(transform.gameObject, transform.gameObject.activeSelf));
                foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true))
                    snapshot.canvasGroups.Add(new CanvasGroupState(group, group.alpha, group.blocksRaycasts, group.interactable));
            }

            return snapshot;
        }

        public void Restore()
        {
            foreach (GameObjectState state in gameObjects)
            {
                if (state.Target != null)
                    state.Target.SetActive(state.Active);
            }
            foreach (CanvasGroupState state in canvasGroups)
            {
                if (state.Target == null)
                    continue;
                state.Target.alpha = state.Alpha;
                state.Target.blocksRaycasts = state.BlocksRaycasts;
                state.Target.interactable = state.Interactable;
            }
        }
    }

    private readonly struct GameObjectState
    {
        public readonly GameObject Target;
        public readonly bool Active;

        public GameObjectState(GameObject target, bool active)
        {
            Target = target;
            Active = active;
        }
    }

    private readonly struct CanvasGroupState
    {
        public readonly CanvasGroup Target;
        public readonly float Alpha;
        public readonly bool BlocksRaycasts;
        public readonly bool Interactable;

        public CanvasGroupState(CanvasGroup target, float alpha, bool blocksRaycasts, bool interactable)
        {
            Target = target;
            Alpha = alpha;
            BlocksRaycasts = blocksRaycasts;
            Interactable = interactable;
        }
    }
}
