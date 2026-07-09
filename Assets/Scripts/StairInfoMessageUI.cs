using System.Collections;
using TMPro;
using UnityEngine;

public class StairInfoMessageUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;
    public RectTransform panelRect;
    public TMP_Text messageText;

    [Header("Animation")]
    public float animationDuration = 0.35f;
    public float visibleDuration = 3f;
    public float slideDistance = 120f;
    public bool hideOnAwake = true;

    private Coroutine messageRoutine;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (panelRect == null)
        {
            panelRect = transform as RectTransform;
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        if (panelRect != null)
        {
            shownPosition = panelRect.anchoredPosition;
            hiddenPosition = shownPosition + Vector2.left * slideDistance;
        }

        if (hideOnAwake)
        {
            SetInstantVisible(false);
        }
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, visibleDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(ShowMessageRoutine(Mathf.Max(0f, duration)));
    }

    public void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowMessageRoutine(float duration)
    {
        SetPanelActive(true);
        yield return Animate(0f, 1f, hiddenPosition, shownPosition);

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
            yield return HideRoutine();
        }

        messageRoutine = null;
    }

    private IEnumerator HideRoutine()
    {
        yield return Animate(1f, 0f, shownPosition, hiddenPosition);
        SetPanelActive(false);
        messageRoutine = null;
    }

    private IEnumerator Animate(float fromAlpha, float toAlpha, Vector2 fromPosition, Vector2 toPosition)
    {
        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            SetVisual(Mathf.Lerp(fromAlpha, toAlpha, smoothT), Vector2.Lerp(fromPosition, toPosition, smoothT));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetVisual(toAlpha, toPosition);
    }

    private void SetInstantVisible(bool isVisible)
    {
        SetPanelActive(isVisible);
        SetVisual(isVisible ? 1f : 0f, isVisible ? shownPosition : hiddenPosition);
    }

    private void SetVisual(float alpha, Vector2 anchoredPosition)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha > 0.01f;
            canvasGroup.blocksRaycasts = alpha > 0.01f;
        }

        if (panelRect != null)
        {
            panelRect.anchoredPosition = anchoredPosition;
        }
    }

    private void SetPanelActive(bool isActive)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (panelRoot == gameObject)
        {
            if (!panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            return;
        }

        panelRoot.SetActive(isActive);
    }
}
