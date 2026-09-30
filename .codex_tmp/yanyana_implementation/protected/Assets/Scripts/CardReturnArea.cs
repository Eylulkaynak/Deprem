using UnityEngine;
using UnityEngine.EventSystems;

public class CardReturnArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        DraggableCard card = eventData.pointerDrag.GetComponent<DraggableCard>();

        if (card == null)
        {
            return;
        }

        card.ReturnToArea(transform);
    }
}
