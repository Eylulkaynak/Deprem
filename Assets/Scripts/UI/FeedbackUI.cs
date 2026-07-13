using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FeedbackUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text messageText;
    public Image mascotImage;
    public Sprite mascotWorried;
    public Sprite mascotHappy;

    [Header("Animasyon")]
    public float popDuration = 0.25f;

    public bool WaitingForClick { get; private set; }

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ReleasePauseState();
    }

    private void OnDestroy()
    {
        ReleasePauseState();
    }

    public void Show(string message)
    {
        StopAllCoroutines();

        if (panel == null)
        {
            Debug.LogWarning("FeedbackUI: Panel reference is not assigned.");
            WaitingForClick = false;
            return;
        }

        panel.SetActive(true);

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (mascotImage != null)
        {
            mascotImage.sprite = mascotWorried;
        }

        WaitingForClick = true;
        Time.timeScale = 0f;

        StartCoroutine(PopIn());
    }

    public void ShowTimed(string message, float duration)
    {
        StopAllCoroutines();
        ReleasePauseState();

        if (panel == null)
        {
            Debug.LogWarning("FeedbackUI: Panel reference is not assigned.");
            return;
        }

        panel.SetActive(true);
        panel.transform.localScale = Vector3.one;

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (mascotImage != null)
        {
            mascotImage.sprite = mascotWorried;
        }

        WaitingForClick = false;
        Time.timeScale = 1f;
        StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator PopIn()
    {
        if (panel == null)
        {
            Hide();
            yield break;
        }

        panel.transform.localScale = Vector3.zero;

        float t = 0f;

        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;

            float scale = Mathf.SmoothStep(0f, 1f, t / popDuration);
            panel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        panel.transform.localScale = Vector3.one;

        while (WaitingForClick)
        {
            if (panel == null || !panel.activeInHierarchy)
            {
                ReleasePauseState();
                yield break;
            }

            if (WasPointerPressed())
            {
                Hide();
            }

            yield return null;
        }
    }

    public void Hide()
    {
        WaitingForClick = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void ReleasePauseState()
    {
        if (!WaitingForClick)
        {
            return;
        }

        WaitingForClick = false;
        Time.timeScale = 1f;
    }

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (!WaitingForClick && panel != null)
        {
            panel.SetActive(false);
        }
    }

    private bool WasPointerPressed()
    {
        if (UnityEngine.Input.touchCount > 0 &&
            UnityEngine.Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            return true;
        }

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return false;
    }
}
