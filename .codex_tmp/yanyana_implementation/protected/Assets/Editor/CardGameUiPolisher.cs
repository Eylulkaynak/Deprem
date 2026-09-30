using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CardGameUiPolisher
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Vector2 CardSize = new Vector2(170f, 200f);
    private static readonly Color OverlayColor = new Color(0.12f, 0.10f, 0.18f, 0.35f);
    private static readonly Color ContainerColor = new Color(0.96f, 0.83f, 1f, 0.85f);
    private static readonly Color SlotColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color OutlineColor = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color ButtonColor = new Color(0.46f, 0.78f, 0.48f, 1f);

    [MenuItem("Tools/Card Game/Polish Card Game UI")]
    public static void Polish()
    {
        GameObject panel = FindSceneObject("CardGamePanel");
        if (panel == null)
        {
            Debug.LogWarning("CardGameUiPolisher: CardGamePanel not found in the open scene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Polish Card Game UI");

        ConfigureCanvas(panel);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            StretchFull(panelRect);
        }

        RectTransform background = FindChildRect(panel.transform, "CandyBackground");
        if (background != null)
        {
            StretchFull(background);
            Image backgroundImage = EnsureComponent<Image>(background.gameObject);
            Undo.RecordObject(backgroundImage, "Polish Candy Background");
            backgroundImage.color = OverlayColor;
            backgroundImage.raycastTarget = false;
            background.SetAsFirstSibling();
        }

        RectTransform container = GetOrCreateContainer(panel.transform);
        ConfigureContainer(container);

        MoveIntoContainer(panel.transform, container, "TitleText");
        MoveIntoContainer(panel.transform, container, "AnswerSlots");
        MoveIntoContainer(panel.transform, container, "CardsArea");
        MoveIntoContainer(panel.transform, container, "FeedbackText");
        MoveIntoContainer(panel.transform, container, "CheckButton");

        ConfigureTitle(container);
        ConfigureDescription(container);
        ConfigureAnswerSlots(container);
        ConfigureCardsArea(container);
        ConfigureFeedback(container);
        ConfigureButton(container);

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log("CardGameUiPolisher: Card game UI polished.");
    }

    private static void ConfigureCanvas(GameObject panel)
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Undo.RecordObject(canvas, "Polish Card Game Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        }

        Undo.RecordObject(scaler, "Polish Card Game Canvas Scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static RectTransform GetOrCreateContainer(Transform panel)
    {
        Transform existing = panel.Find("CardMiniGameContainer");
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject containerObject = new GameObject("CardMiniGameContainer", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
        Undo.RegisterCreatedObjectUndo(containerObject, "Create Card Mini Game Container");
        containerObject.transform.SetParent(panel, false);
        return containerObject.GetComponent<RectTransform>();
    }

    private static void ConfigureContainer(RectTransform container)
    {
        Undo.RecordObject(container, "Configure Card Mini Game Container");
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = new Vector2(1180f, 790f);
        container.localScale = Vector3.one;
        container.localRotation = Quaternion.identity;

        Image image = EnsureComponent<Image>(container.gameObject);
        Undo.RecordObject(image, "Configure Container Image");
        image.color = ContainerColor;
        image.raycastTarget = false;

        Outline outline = EnsureComponent<Outline>(container.gameObject);
        outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
        outline.effectDistance = new Vector2(3f, -3f);

        Shadow shadow = EnsureComponent<Shadow>(container.gameObject);
        shadow.effectColor = new Color(0.24f, 0.18f, 0.36f, 0.28f);
        shadow.effectDistance = new Vector2(0f, -10f);
    }

    private static void ConfigureTitle(RectTransform container)
    {
        TMP_Text title = FindChildComponent<TMP_Text>(container, "TitleText");
        if (title == null)
        {
            return;
        }

        RectTransform rect = title.rectTransform;
        ConfigureRect(rect, new Vector2(0f, 320f), new Vector2(1050f, 70f));
        title.text = "Sars\u0131nt\u0131 Bitti! Do\u011fru Ad\u0131mlar\u0131 S\u0131rala";
        title.fontSize = 42f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.28f, 0.22f, 0.42f, 1f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 30f;
        title.fontSizeMax = 46f;
    }

    private static void ConfigureDescription(RectTransform container)
    {
        TMP_Text description = FindChildComponent<TMP_Text>(container, "DescriptionText");
        if (description == null)
        {
            GameObject descriptionObject = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(descriptionObject, "Create Card Game Description");
            descriptionObject.transform.SetParent(container, false);
            description = descriptionObject.GetComponent<TMP_Text>();
        }

        ConfigureRect(description.rectTransform, new Vector2(0f, 260f), new Vector2(1040f, 54f));
        description.text = "Aileni g\u00fcvenli alana ula\u015ft\u0131rmak i\u00e7in kartlar\u0131 do\u011fru s\u0131raya koy.";
        description.fontSize = 25f;
        description.alignment = TextAlignmentOptions.Center;
        description.color = new Color(0.36f, 0.30f, 0.50f, 1f);
        description.enableAutoSizing = true;
        description.fontSizeMin = 18f;
        description.fontSizeMax = 28f;
    }

    private static void ConfigureAnswerSlots(RectTransform container)
    {
        RectTransform slots = FindChildRect(container, "AnswerSlots");
        if (slots == null)
        {
            return;
        }

        ConfigureRect(slots, new Vector2(0f, 95f), new Vector2(880f, 230f));
        GridLayoutGroup grid = EnsureComponent<GridLayoutGroup>(slots.gameObject);
        grid.cellSize = CardSize;
        grid.spacing = new Vector2(28f, 0f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < slots.childCount; i++)
        {
            RectTransform slot = slots.GetChild(i) as RectTransform;
            if (slot == null)
            {
                continue;
            }

            slot.sizeDelta = CardSize;
            Image image = EnsureComponent<Image>(slot.gameObject);
            image.color = SlotColor;
            Outline outline = EnsureComponent<Outline>(slot.gameObject);
            outline.effectColor = OutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            EnsureStepLabel(slot, i + 1);
        }
    }

    private static void ConfigureCardsArea(RectTransform container)
    {
        RectTransform cardsArea = FindChildRect(container, "CardsArea");
        if (cardsArea == null)
        {
            return;
        }

        ConfigureRect(cardsArea, new Vector2(0f, -190f), new Vector2(880f, 230f));
        GridLayoutGroup grid = EnsureComponent<GridLayoutGroup>(cardsArea.gameObject);
        grid.cellSize = CardSize;
        grid.spacing = new Vector2(28f, 0f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.MiddleCenter;

        Color[] colors =
        {
            new Color(0.33f, 0.66f, 0.88f, 1f),
            new Color(0.36f, 0.78f, 0.55f, 1f),
            new Color(0.92f, 0.78f, 0.32f, 1f),
            new Color(0.88f, 0.38f, 0.32f, 1f)
        };

        for (int i = 0; i < cardsArea.childCount; i++)
        {
            RectTransform card = cardsArea.GetChild(i) as RectTransform;
            if (card == null)
            {
                continue;
            }

            card.sizeDelta = CardSize;
            Image image = EnsureComponent<Image>(card.gameObject);
            image.color = colors[Mathf.Min(i, colors.Length - 1)];
            Shadow shadow = EnsureComponent<Shadow>(card.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -5f);

            TMP_Text text = card.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 25f;
                text.margin = new Vector4(12f, 10f, 12f, 10f);
            }
        }
    }

    private static void ConfigureFeedback(RectTransform container)
    {
        TMP_Text feedback = FindChildComponent<TMP_Text>(container, "FeedbackText");
        if (feedback == null)
        {
            return;
        }

        ConfigureRect(feedback.rectTransform, new Vector2(0f, -315f), new Vector2(900f, 52f));
        feedback.alignment = TextAlignmentOptions.Center;
        feedback.fontSize = 27f;
        feedback.color = new Color(0.30f, 0.24f, 0.42f, 1f);
        feedback.enableAutoSizing = true;
        feedback.fontSizeMin = 18f;
        feedback.fontSizeMax = 30f;
    }

    private static void ConfigureButton(RectTransform container)
    {
        RectTransform buttonRect = FindChildRect(container, "CheckButton");
        if (buttonRect == null)
        {
            return;
        }

        ConfigureRect(buttonRect, new Vector2(0f, -370f), new Vector2(230f, 56f));
        Image buttonImage = EnsureComponent<Image>(buttonRect.gameObject);
        buttonImage.color = ButtonColor;

        TMP_Text buttonText = buttonRect.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = "Kontrol Et";
            buttonText.fontSize = 25f;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
        }
    }

    private static void EnsureStepLabel(RectTransform slot, int stepNumber)
    {
        Transform existing = slot.Find("StepLabel");
        TMP_Text label;

        if (existing == null)
        {
            GameObject labelObject = new GameObject("StepLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create Step Label");
            labelObject.transform.SetParent(slot, false);
            label = labelObject.GetComponent<TMP_Text>();
        }
        else
        {
            label = existing.GetComponent<TMP_Text>();
        }

        if (label == null)
        {
            return;
        }

        ConfigureRect(label.rectTransform, new Vector2(0f, 0f), CardSize);
        label.text = $"{stepNumber}. Ad\u0131m";
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.42f, 0.39f, 0.50f, 0.82f);
        label.raycastTarget = false;
    }

    private static void MoveIntoContainer(Transform panel, RectTransform container, string objectName)
    {
        RectTransform rect = FindChildRect(panel, objectName);
        if (rect == null || rect == container || rect.parent == container)
        {
            return;
        }

        Undo.SetTransformParent(rect, container, $"Move {objectName} To Card Container");
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        Undo.RecordObject(rect, "Configure RectTransform");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static RectTransform FindChildRect(Transform root, string childName)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == childName)
            {
                return rect;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        RectTransform rect = FindChildRect(root, childName);
        return rect != null ? rect.GetComponent<T>() : null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(target);
        }

        return component;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

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
}
