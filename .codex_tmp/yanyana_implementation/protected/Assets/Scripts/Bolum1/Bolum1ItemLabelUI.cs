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

    [Tooltip("Esya birakildiktan sonra etiketin gorunur kalacagi sure.")]
    [Min(0f)]
    [SerializeField] private float releaseHoldDuration = 0.45f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Camera mainCamera;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private DraggableItem displayedTarget;
    private float hideAtTime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        mainCamera = Camera.main;
        rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>(true);

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (parentRect == null)
            parentRect = transform.parent as RectTransform;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (mainCamera == null || parentRect == null)
        {
            displayedTarget = null;
            SetVisible(false);
            return;
        }

        DraggableItem target = DraggableItem.ActiveDrag;
        if (target != null && target.gameObject.activeInHierarchy && target.IsDragging)
        {
            hideAtTime = Time.unscaledTime + releaseHoldDuration;
        }
        else if (displayedTarget != null &&
                 displayedTarget.gameObject.activeInHierarchy &&
                 Time.unscaledTime < hideAtTime)
        {
            target = displayedTarget;
        }
        else
        {
            displayedTarget = null;
            SetVisible(false);
            return;
        }

        if (displayedTarget != target)
        {
            displayedTarget = target;
            if (labelText != null)
                labelText.text = target.DisplayName;
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
