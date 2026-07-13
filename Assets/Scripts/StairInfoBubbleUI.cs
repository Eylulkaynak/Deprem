using System.Collections;
using TMPro;
using UnityEngine;

public class StairInfoBubbleUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform bubbleRect;
    public TMP_Text messageText;

    [Header("Positions")]
    public Vector2 hiddenPosition = new Vector2(-520f, 120f);
    public Vector2 shownPosition = new Vector2(260f, 120f);

    [Header("Animation")]
    public float showDuration = 0.35f;
    public float hideDuration = 0.25f;
    public float shakeDuration = 0.25f;
    public float shakeAmount = 12f;
    public bool hideOnAwake = true;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (bubbleRect == null)
        {
            bubbleRect = transform as RectTransform;
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        gameObject.SetActive(true);

        if (hideOnAwake)
        {
            SetVisual(0f, hiddenPosition);
        }
    }

    public void ShowMessage(string message)
    {
        SetMessage(message);
        StartAnimation(ShowRoutine());
    }

    public void ShowMessageAndShake(string message)
    {
        SetMessage(message);
        StartAnimation(ShowAndShakeRoutine());
    }

    public void Hide()
    {
        StartAnimation(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        gameObject.SetActive(true);
        yield return AnimateTo(1f, shownPosition, showDuration);
    }

    private IEnumerator ShowAndShakeRoutine()
    {
        gameObject.SetActive(true);
        yield return AnimateTo(1f, shownPosition, showDuration);
        yield return ShakeRoutine();
    }

    private IEnumerator HideRoutine()
    {
        yield return AnimateTo(0f, hiddenPosition, hideDuration);
    }

    private IEnumerator AnimateTo(float targetAlpha, Vector2 targetPosition, float duration)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        Vector2 startPosition = bubbleRect != null ? bubbleRect.anchoredPosition : targetPosition;
        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsedTime < safeDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / safeDuration);
            SetVisual(
                Mathf.Lerp(startAlpha, targetAlpha, t),
                Vector2.Lerp(startPosition, targetPosition, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetVisual(targetAlpha, targetPosition);
    }

    private IEnumerator ShakeRoutine()
    {
        if (bubbleRect == null)
        {
            yield break;
        }

        Vector2 basePosition = bubbleRect.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            bubbleRect.anchoredPosition = basePosition + Random.insideUnitCircle * shakeAmount;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bubbleRect.anchoredPosition = basePosition;
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(routine);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void SetVisual(float alpha, Vector2 anchoredPosition)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (bubbleRect != null)
        {
            bubbleRect.anchoredPosition = anchoredPosition;
        }
    }
}
