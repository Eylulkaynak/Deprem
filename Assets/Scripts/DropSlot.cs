using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Slot Info")]
    public int slotOrder;

    [Header("Manager")]
    public CardGameManager cardGameManager;

    public DraggableCard currentCard;

    public void OnPointerClick(PointerEventData eventData)
    {
        ResolveManagerIfNeeded();

        if (cardGameManager != null)
        {
            cardGameManager.HandleSlotTapped(this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableCard card = GetDraggedCard(eventData);

        if (card == null)
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
        if (card == null)
        {
            return;
        }

        DropSlot replacementSlot = card.currentSlot != null ? card.currentSlot : card.PreviousSlot;
        if (card.currentSlot != null && card.currentSlot != this)
        {
            card.currentSlot.ClearSlot(card);
        }

        if (currentCard != null && currentCard != card)
        {
            DraggableCard displacedCard = currentCard;
            currentCard = null;

            if (replacementSlot != null &&
                replacementSlot != this &&
                replacementSlot.CanAccept(displacedCard))
            {
                replacementSlot.AcceptCard(displacedCard);
            }
            else
            {
                displacedCard.ReturnToStartArea();
            }
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
        ResolveManagerIfNeeded();

        if (cardGameManager != null)
        {
            cardGameManager.OnCardPlacedInSlot();
        }
    }

    private void ResolveManagerIfNeeded()
    {
        if (cardGameManager == null)
        {
            cardGameManager = FindFirstObjectByType<CardGameManager>();
        }
    }
}
