using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Card Info")]
    public int correctOrder;
    public CardGameManager cardGameManager;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private RectTransform dragRoot;

    private Transform startParent;
    private int startSiblingIndex;
    private Transform returnParent;
    private int returnSiblingIndex;
    private DropSlot returnSlot;
    private Vector2 dragOffset;
    private bool wasDroppedSuccessfully;

    public DropSlot currentSlot;
    public DropSlot PreviousSlot => returnSlot;

    public void OnPointerClick(PointerEventData eventData)
    {
        ResolveManagerIfNeeded();

        if (cardGameManager != null)
        {
            cardGameManager.SelectCard(this);
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        parentCanvas = GetComponentInParent<Canvas>();
        CaptureStartTransformIfNeeded();
    }

    private void Start()
    {
        CaptureStartTransformIfNeeded();
        NormalizeRectTransform();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CaptureStartTransformIfNeeded();
        wasDroppedSuccessfully = false;
        returnParent = transform.parent;
        returnSiblingIndex = transform.GetSiblingIndex();
        returnSlot = currentSlot != null ? currentSlot : GetParentSlot();

        if (returnSlot != null)
        {
            returnSlot.ClearSlot(this);
            currentSlot = null;
        }

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        Transform dragParent = parentCanvas != null ? parentCanvas.transform : transform.root;
        transform.SetParent(dragParent, true);
        transform.SetAsLastSibling();
        NormalizeRectTransformKeepingWorldPosition();
        CacheDragOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragRoot != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition))
        {
            rectTransform.anchoredPosition = pointerPosition + dragOffset;
            return;
        }

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;

        if (!wasDroppedSuccessfully)
        {
            canvasGroup.blocksRaycasts = false;
            wasDroppedSuccessfully = TryDropOnSlotUnderPointer(eventData);
        }

        canvasGroup.blocksRaycasts = true;

        if (!wasDroppedSuccessfully)
        {
            ReturnToPreviousPlace();
        }
    }

    public void SnapToSlot(DropSlot slot)
    {
        if (slot == null)
        {
            ReturnToPreviousPlace();
            return;
        }

        currentSlot = slot;
        wasDroppedSuccessfully = true;

        transform.SetParent(slot.transform, false);
        transform.SetAsLastSibling();
        MatchSlotSize(slot);
        SnapToCenter();
    }

    public void ReturnToArea(Transform areaTransform)
    {
        CaptureStartTransformIfNeeded();

        if (areaTransform == null)
        {
            ReturnToStartArea();
            return;
        }

        currentSlot = null;
        returnSlot = null;
        wasDroppedSuccessfully = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        transform.SetParent(areaTransform, false);
        transform.SetAsLastSibling();
        NormalizeRectTransform();
    }

    public void ReturnToPreviousPlace()
    {
        if (returnSlot != null && returnSlot.CanAccept(this))
        {
            returnSlot.AcceptCard(this);
            return;
        }

        if (returnParent != null)
        {
            DropSlot parentSlot = returnParent.GetComponent<DropSlot>();
            if (parentSlot != null)
            {
                if (parentSlot.CanAccept(this))
                {
                    parentSlot.AcceptCard(this);
                    return;
                }

                ReturnToStartArea();
                return;
            }

            currentSlot = null;
            transform.SetParent(returnParent, false);
            transform.SetSiblingIndex(Mathf.Clamp(returnSiblingIndex, 0, returnParent.childCount - 1));
            NormalizeRectTransform();
            return;
        }

        ReturnToStartArea();
    }

    public void ReturnToStartArea()
    {
        CaptureStartTransformIfNeeded();
        currentSlot = null;
        returnSlot = null;
        wasDroppedSuccessfully = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (startParent == null)
        {
            return;
        }

        transform.SetParent(startParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(startSiblingIndex, 0, startParent.childCount - 1));
        NormalizeRectTransform();
    }

    private void MatchSlotSize(DropSlot slot)
    {
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            rectTransform.sizeDelta = slotRect.rect.size;
        }
    }

    private void SnapToCenter()
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }

    private void NormalizeRectTransform()
    {
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        Vector3 anchoredPosition = rectTransform.anchoredPosition3D;
        anchoredPosition.z = 0f;
        rectTransform.anchoredPosition3D = anchoredPosition;
    }

    private void NormalizeRectTransformKeepingWorldPosition()
    {
        Vector3 worldPosition = rectTransform.position;
        Vector2 size = rectTransform.rect.size;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.position = worldPosition;
        NormalizeRectTransform();
    }

    private void CaptureStartTransformIfNeeded()
    {
        if (startParent != null)
        {
            return;
        }

        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();
    }

    private void ResolveManagerIfNeeded()
    {
        if (cardGameManager != null)
        {
            return;
        }

        cardGameManager = GetComponentInParent<CardGameManager>();

        if (cardGameManager == null)
        {
            cardGameManager = FindFirstObjectByType<CardGameManager>();
        }
    }

    private void CacheDragOffset(PointerEventData eventData)
    {
        dragRoot = transform.parent as RectTransform;

        if (dragRoot == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition))
        {
            dragOffset = Vector2.zero;
            return;
        }

        dragOffset = rectTransform.anchoredPosition - pointerPosition;
    }

    private DropSlot GetParentSlot()
    {
        if (transform.parent == null)
        {
            return null;
        }

        return transform.parent.GetComponent<DropSlot>();
    }

    private bool TryDropOnSlotUnderPointer(PointerEventData eventData)
    {
        if (eventData == null || EventSystem.current == null)
        {
            return false;
        }

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            DropSlot slot = result.gameObject.GetComponentInParent<DropSlot>();
            if (slot != null && slot.CanAccept(this))
            {
                slot.AcceptCard(this);
                return true;
            }
        }

        return false;
    }
}
