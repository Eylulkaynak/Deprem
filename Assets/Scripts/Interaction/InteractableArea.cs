using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableArea : MonoBehaviour
{
    public enum AreaType
    {
        Safe,
        Unsafe
    }

    [Header("Area")]
    public AreaType areaType;

    [Header("Safe Area")]
    public Transform targetPoint;

    [Header("Unsafe Area")]
    [TextArea]
    public string wrongFeedbackMessage = "Bu guvenli bir alan degil!";

    [Header("Bubble")]
    public GameObject bubble;

    public void ShowBubble()
    {
        if (bubble != null)
        {
            InteractableBubbleClickTarget.Bind(bubble, this);
            bubble.SetActive(true);
        }
    }

    public void HideBubble()
    {
        if (bubble != null)
            bubble.SetActive(false);
    }

    public void Select()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAreaSelected(this);
        }
    }
}

public sealed class InteractableBubbleClickTarget : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    private InteractableArea area;

    public static void Bind(GameObject bubbleObject, InteractableArea owner)
    {
        if (bubbleObject == null || owner == null)
        {
            return;
        }

        InteractableBubbleClickTarget target = bubbleObject.GetComponent<InteractableBubbleClickTarget>();
        if (target == null)
        {
            target = bubbleObject.AddComponent<InteractableBubbleClickTarget>();
        }

        target.area = owner;
        EnsureUiHitArea(bubbleObject);

        if (bubbleObject.GetComponent<SpriteRenderer>() != null &&
            bubbleObject.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = bubbleObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectArea();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SelectArea();
    }

    private void OnMouseDown()
    {
        SelectArea();
    }

    private void SelectArea()
    {
        if (area != null)
        {
            area.Select();
        }
    }

    private static void EnsureUiHitArea(GameObject bubbleObject)
    {
        if (bubbleObject.GetComponent<RectTransform>() == null)
        {
            return;
        }

        Graphic graphic = bubbleObject.GetComponent<Graphic>();
        if (graphic == null)
        {
            Image hitArea = bubbleObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;
            return;
        }

        graphic.raycastTarget = true;
    }
}
