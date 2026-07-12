using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Info")]
    public int slotOrder;

    [Header("Manager")]
    public CardGameManager cardGameManager;

    public DraggableCard currentCard;

    public void OnDrop(PointerEventData eventData)
    {
        DraggableCard card = GetDraggedCard(eventData);

        if (card == null || !CanAccept(card))
        {
            return;
        }

        AcceptCard(card);
    }

    public bool CanAccept(DraggableCard card)
    {
        if (card == null)
        {
            return false;
        }

        return currentCard == null || currentCard == card;
    }

    public void AcceptCard(DraggableCard card)
    {
        if (!CanAccept(card))
        {
            return;
        }

        currentCard = card;
        card.SnapToSlot(this);
        NotifyCardPlaced();
    }

    public void ClearSlot()
    {
        currentCard = null;
    }

    public void ClearSlot(DraggableCard card)
    {
        if (currentCard == card)
        {
            currentCard = null;
        }
    }

    public bool IsCorrect()
    {
        if (currentCard == null)
        {
            return false;
        }

        return currentCard.correctOrder == slotOrder;
    }

    private DraggableCard GetDraggedCard(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return null;
        }

        return eventData.pointerDrag.GetComponent<DraggableCard>();
    }

    private void NotifyCardPlaced()
    {
        if (cardGameManager == null)
        {
            cardGameManager = FindObjectOfType<CardGameManager>();
        }

        if (cardGameManager != null)
        {
            cardGameManager.OnCardPlacedInSlot();
        }
    }
}
