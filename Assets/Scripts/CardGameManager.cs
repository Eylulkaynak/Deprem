using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CardGameManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject cardGamePanel;
    public MonoBehaviour playerMovementScript;
    public bool hidePanelOnStart = true;

    [Header("Background")]
    public Image panelBackgroundImage;
    [Range(0f, 1f)]
    public float panelBackgroundAlpha = 0.65f;

    [Header("Cards")]
    public Transform cardsArea;
    public DraggableCard[] cards;

    [Header("Slots")]
    public DropSlot[] slots;

    [Header("UI")]
    public TMP_Text feedbackText;
    public GameObject checkButton;
    public bool hideCheckButton = true;
    public bool autoCheckWhenAllSlotsFilled = true;
    public RectTransform shakeTarget;
    public float shakeDuration = 0.25f;
    public float shakeAmount = 12f;

    [Header("Success")]
    public float successDelay = 1.5f;
    public GameObject nextMissionPanel;
    public DoorMissionManager doorMissionManager;
    public UnityEvent onCorrectSequence;

    private Coroutine checkRoutine;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        FindPanelIfNeeded();
        FindCardsAreaIfNeeded();
        FindBackgroundIfNeeded();
        FindShakeTargetIfNeeded();
        FindCheckButtonIfNeeded();
        FindCardsIfNeeded();
        ApplyBackgroundAlpha();
        ApplyCheckButtonVisibility();
    }

    private void Start()
    {
        if (hidePanelOnStart)
        {
            HideCardGame();
        }
    }

    public void ShowCardGame()
    {
        FindPanelIfNeeded();
        FindCardsAreaIfNeeded();
        FindBackgroundIfNeeded();
        FindShakeTargetIfNeeded();
        FindCheckButtonIfNeeded();
        FindCardsIfNeeded();
        ApplyBackgroundAlpha();
        ApplyCheckButtonVisibility();

        if (cardGamePanel != null)
        {
            cardGamePanel.SetActive(true);
        }

        if (nextMissionPanel != null)
        {
            nextMissionPanel.SetActive(false);
        }

        SetPlayerMovementEnabled(false);
    }

    public void HideCardGame()
    {
        if (cardGamePanel != null)
        {
            cardGamePanel.SetActive(false);
        }

        SetPlayerMovementEnabled(true);
    }

    public void CheckAnswer()
    {
        if (checkRoutine != null)
        {
            return;
        }

        if (!AreAllSlotsFilled())
        {
            SetFeedback("\u00d6nce t\u00fcm kartlar\u0131 yerle\u015ftir!");
            PlayShake();
            return;
        }

        if (!IsSequenceCorrect())
        {
            checkRoutine = StartCoroutine(WrongAnswerRoutine());
            return;
        }

        checkRoutine = StartCoroutine(CorrectAnswerRoutine());
    }

    public void OnCardPlacedInSlot()
    {
        if (!autoCheckWhenAllSlotsFilled || checkRoutine != null)
        {
            return;
        }

        if (AreAllSlotsFilled())
        {
            CheckAnswer();
        }
    }

    public void ResetAllCardsToStartArea()
    {
        FindCardsIfNeeded();

        if (slots != null)
        {
            foreach (DropSlot slot in slots)
            {
                if (slot != null)
                {
                    slot.ClearSlot();
                }
            }
        }

        if (cards != null)
        {
            foreach (DraggableCard card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.currentSlot = null;
                card.ReturnToStartArea();
            }
        }

        RefreshCardsAreaLayout();
    }

    private IEnumerator WrongAnswerRoutine()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        SetFeedback("Yanl\u0131\u015f s\u0131ra! Tekrar dene.");
        yield return ShakeRoutine();
        ResetAllCardsToStartArea();
        checkRoutine = null;
    }

    private IEnumerator CorrectAnswerRoutine()
    {
        SetFeedback("Do\u011fru s\u0131ralama!");

        if (successDelay > 0f)
        {
            yield return new WaitForSeconds(successDelay);
        }

        HideCardGame();

        if (doorMissionManager != null)
        {
            doorMissionManager.StartDoorMission();
        }
        else if (nextMissionPanel != null)
        {
            nextMissionPanel.SetActive(true);
        }

        onCorrectSequence?.Invoke();
        checkRoutine = null;
    }

    private bool AreAllSlotsFilled()
    {
        if (slots == null || slots.Length == 0)
        {
            return false;
        }

        foreach (DropSlot slot in slots)
        {
            if (slot == null || slot.currentCard == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSequenceCorrect()
    {
        if (slots == null || slots.Length == 0)
        {
            return false;
        }

        foreach (DropSlot slot in slots)
        {
            if (slot == null || !slot.IsCorrect())
            {
                return false;
            }
        }

        return true;
    }

    private void PlayShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        FindShakeTargetIfNeeded();

        if (shakeTarget == null)
        {
            shakeRoutine = null;
            yield break;
        }

        Vector2 originalPosition = shakeTarget.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            Vector2 offset = Random.insideUnitCircle * shakeAmount;
            shakeTarget.anchoredPosition = originalPosition + offset;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        shakeTarget.anchoredPosition = originalPosition;
        shakeRoutine = null;
    }

    private void RefreshCardsAreaLayout()
    {
        if (cardsArea == null)
        {
            return;
        }

        RectTransform cardsAreaRect = cardsArea as RectTransform;
        if (cardsAreaRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsAreaRect);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void SetPlayerMovementEnabled(bool isEnabled)
    {
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = isEnabled;
        }
    }

    private void FindPanelIfNeeded()
    {
        if (cardGamePanel != null)
        {
            return;
        }

        Transform panelTransform = transform.Find("CardGamePanel");

        if (panelTransform == null)
        {
            GameObject panelObject = GameObject.Find("CardGamePanel");
            panelTransform = panelObject != null ? panelObject.transform : null;
        }

        if (panelTransform == null)
        {
            foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (sceneObject.name == "CardGamePanel" &&
                    sceneObject.scene.IsValid() &&
                    sceneObject.hideFlags == HideFlags.None)
                {
                    panelTransform = sceneObject.transform;
                    break;
                }
            }
        }

        if (panelTransform != null)
        {
            cardGamePanel = panelTransform.gameObject;
        }
    }

    private void FindCardsAreaIfNeeded()
    {
        if (cardsArea != null)
        {
            return;
        }

        if (cardGamePanel != null)
        {
            Transform areaTransform = cardGamePanel.transform.Find("CardsArea");
            if (areaTransform != null)
            {
                cardsArea = areaTransform;
                return;
            }
        }

        GameObject areaObject = GameObject.Find("CardsArea");
        if (areaObject != null)
        {
            cardsArea = areaObject.transform;
        }
    }

    private void FindBackgroundIfNeeded()
    {
        if (panelBackgroundImage != null || cardGamePanel == null)
        {
            return;
        }

        Transform backgroundTransform = cardGamePanel.transform.Find("CandyBackground");
        if (backgroundTransform != null)
        {
            panelBackgroundImage = backgroundTransform.GetComponent<Image>();
        }

        if (panelBackgroundImage == null)
        {
            panelBackgroundImage = cardGamePanel.GetComponent<Image>();
        }
    }

    private void FindShakeTargetIfNeeded()
    {
        if (shakeTarget != null)
        {
            return;
        }

        if (cardGamePanel != null)
        {
            shakeTarget = cardGamePanel.GetComponent<RectTransform>();
        }
    }

    private void FindCheckButtonIfNeeded()
    {
        if (checkButton != null || cardGamePanel == null)
        {
            return;
        }

        Transform buttonTransform = cardGamePanel.transform.Find("CheckButton");
        if (buttonTransform != null)
        {
            checkButton = buttonTransform.gameObject;
        }
    }

    private void FindCardsIfNeeded()
    {
        if (cards != null && cards.Length > 0)
        {
            return;
        }

        if (cardGamePanel != null)
        {
            cards = cardGamePanel.GetComponentsInChildren<DraggableCard>(true);
            if (cards.Length > 0)
            {
                return;
            }
        }

        cards = FindSceneObjectsOfType<DraggableCard>();
    }

    private T[] FindSceneObjectsOfType<T>() where T : Component
    {
        T[] sceneObjects = Resources.FindObjectsOfTypeAll<T>();
        int validCount = 0;

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            if (sceneObjects[i] != null &&
                sceneObjects[i].gameObject.scene.IsValid() &&
                sceneObjects[i].hideFlags == HideFlags.None)
            {
                validCount++;
            }
        }

        T[] validObjects = new T[validCount];
        int index = 0;

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            if (sceneObjects[i] != null &&
                sceneObjects[i].gameObject.scene.IsValid() &&
                sceneObjects[i].hideFlags == HideFlags.None)
            {
                validObjects[index] = sceneObjects[i];
                index++;
            }
        }

        return validObjects;
    }

    private void ApplyBackgroundAlpha()
    {
        FindBackgroundIfNeeded();

        if (panelBackgroundImage == null)
        {
            return;
        }

        Color color = panelBackgroundImage.color;
        color.a = panelBackgroundAlpha;
        panelBackgroundImage.color = color;
    }

    private void ApplyCheckButtonVisibility()
    {
        if (checkButton != null)
        {
            checkButton.SetActive(!hideCheckButton);
        }
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
}
