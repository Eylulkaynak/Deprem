using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CardGameUiFixer
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Vector2 CellSize = new Vector2(170f, 200f);
    private static readonly Vector2 CardAreaSize = new Vector2(760f, 230f);
    private static readonly Vector2 SlotAreaSize = new Vector2(760f, 230f);
    private const float CardsAreaY = -260f;
    private const float AnswerSlotsY = 120f;
    private const float CellSpacing = 20f;

    [MenuItem("Tools/Card Game/Fix 2D UI Layout")]
    public static void FixLayout()
    {
        GameObject panel = GameObject.Find("CardGamePanel");
        if (panel == null)
        {
            EditorUtility.DisplayDialog(
                "Card Game UI Fixer",
                "Acik sahnede CardGamePanel bulunamadi. Lutfen kart oyunu sahnesini acip tekrar calistir.",
                "Tamam");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel, "Fix Card Game UI Layout");

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Undo.RecordObject(canvas, "Fix Card Game Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            }

            Undo.RecordObject(scaler, "Fix Card Game Canvas Scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            StretchFull(panelRect);
        }

        ResetRectTransforms(panel.transform);
        RemoveConflictingLayoutComponents(panel.transform);

        RectTransform background = FindChildRect(panel.transform, "CandyBackground");
        if (background != null)
        {
            StretchFull(background);
            background.SetAsFirstSibling();
        }

        RectTransform answerSlots = FindChildRect(panel.transform, "AnswerSlots");
        if (answerSlots != null)
        {
            ConfigureCenteredArea(answerSlots, AnswerSlotsY, SlotAreaSize);
            ConfigureGrid(answerSlots.gameObject, CellSize, GridLayoutGroup.Constraint.FixedColumnCount, 4);
            ResizeDirectChildren(answerSlots, CellSize);
        }

        RectTransform cardsArea = FindChildRect(panel.transform, "CardsArea");
        if (cardsArea != null)
        {
            ConfigureCenteredArea(cardsArea, CardsAreaY, CardAreaSize);
            ConfigureGrid(cardsArea.gameObject, CellSize, GridLayoutGroup.Constraint.FixedColumnCount, 4);
            ResizeDirectChildren(cardsArea, CellSize);
        }

        EditorUtility.SetDirty(panel);
        if (canvas != null)
        {
            EditorUtility.SetDirty(canvas);
        }

        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorUtility.DisplayDialog(
            "Card Game UI Fixer",
            "UI duzeltildi: Canvas Overlay, scaler 1920x1080, CardGamePanel/CandyBackground full stretch, CardsArea ve AnswerSlots 4 kolon duz grid olarak ayarlandi.",
            "Tamam");
    }

    private static void ResetRectTransforms(Transform root)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            Undo.RecordObject(rect, "Reset Card Game RectTransform");
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Vector3 anchoredPosition3D = rect.anchoredPosition3D;
            anchoredPosition3D.z = 0f;
            rect.anchoredPosition3D = anchoredPosition3D;
        }
    }

    private static void RemoveConflictingLayoutComponents(Transform root)
    {
        foreach (HorizontalLayoutGroup layout in root.GetComponentsInChildren<HorizontalLayoutGroup>(true))
        {
            Undo.DestroyObjectImmediate(layout);
        }

        foreach (VerticalLayoutGroup layout in root.GetComponentsInChildren<VerticalLayoutGroup>(true))
        {
            Undo.DestroyObjectImmediate(layout);
        }

        foreach (ContentSizeFitter fitter in root.GetComponentsInChildren<ContentSizeFitter>(true))
        {
            Undo.DestroyObjectImmediate(fitter);
        }
    }

    private static void StretchFull(RectTransform rect)
    {
        Undo.RecordObject(rect, "Stretch Card Game RectTransform");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureCenteredArea(RectTransform rect, float y, Vector2 size)
    {
        Undo.RecordObject(rect, "Configure Card Game Area");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureGrid(
        GameObject target,
        Vector2 cellSize,
        GridLayoutGroup.Constraint constraint,
        int constraintCount)
    {
        GridLayoutGroup grid = target.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = Undo.AddComponent<GridLayoutGroup>(target);
        }

        Undo.RecordObject(grid, "Configure Card Game Grid");
        grid.cellSize = cellSize;
        grid.spacing = new Vector2(CellSpacing, 0f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = constraint;
        grid.constraintCount = constraintCount;
    }

    private static void ResizeDirectChildren(RectTransform parent, Vector2 size)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform child = parent.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Undo.RecordObject(child, "Resize Card Game Child");
            child.sizeDelta = size;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
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
}
