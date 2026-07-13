using UnityEngine;
using TMPro;

/// <summary>
/// Ana Canvas altindaki tek bir esya etiketi.
/// Sadece suruklenen esyada gorunur; GameObject kapanmaz (LateUpdate calismaya devam eder).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Bolum1ItemLabelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Camera mainCamera;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private DraggableItem target;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        mainCamera = Camera.main;
        rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>(true);

        SetVisible(false);
    }

    public void Bind(DraggableItem item)
    {
        target = item;

        if (labelText != null && item != null)
            labelText.text = item.DisplayName;

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (target == null || mainCamera == null || parentRect == null)
        {
            SetVisible(false);
            return;
        }

        bool shouldShow = target.gameObject.activeInHierarchy && target.IsDragging;
        if (!shouldShow)
        {
            SetVisible(false);
            return;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(target.GetLabelWorldPosition());
        if (screenPoint.z < 0f)
        {
            SetVisible(false);
            return;
        }

        Camera eventCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : mainCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                eventCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
    }
}
