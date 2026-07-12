using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinalSuccessUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;
    public RectTransform boxRect;
    public Image overlayImage;
    public Image boxImage;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text achievementsText;

    [Header("Buttons")]
    public Button restartButton;
    public Button quitButton;

    [Header("Objects To Disable On Show")]
    public MonoBehaviour[] movementScriptsToDisable;
    public GameObject[] objectsToHideOnShow;

    [Header("Text")]
    public string title = "G\u00f6rev Tamamland\u0131!";
    public string description = "Deprem sonras\u0131 do\u011fru ad\u0131mlar\u0131 \u00f6\u011frendin.";
    [TextArea(3, 6)]
    public string achievements =
        "\u2713 Sars\u0131nt\u0131 bitince kendini kontrol ettin.\n" +
        "\u2713 Asans\u00f6r\u00fc kullanmad\u0131n.\n" +
        "\u2713 Merdivenleri duvar kenar\u0131ndan dikkatli indin.\n" +
        "\u2713 A\u00e7\u0131k ve g\u00fcvenli alana ula\u015ft\u0131n.";

    [Header("Animation")]
    public float showDuration = 0.45f;
    public Vector3 hiddenScale = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 shownScale = Vector3.one;
    public bool hideOnAwake = true;

    private Coroutine showRoutine;

    private void Awake()
    {
        ResolveReferences();
        WireButtons();

        if (hideOnAwake)
        {
            SetVisibleInstant(false);
        }
    }

    public void ShowFinalPanel()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }

        ResolveReferences();

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("FinalSuccessUI: Final panel animation could not start because this object is inactive in the hierarchy. Keep the Canvas and parent objects active, then hide the panel with CanvasGroup.");
            return;
        }

        ApplyText();
        DisableGameplayObjects();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void Show()
    {
        ShowFinalPanel();
    }

    public void Hide()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        SetVisibleInstant(false);
    }

    public void RestartGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("FinalSuccessUI: QuitGame called. Application.Quit does not close the Unity Editor.");
#else
        Application.Quit();
#endif
    }

    private IEnumerator ShowRoutine()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, showDuration);

        while (elapsedTime < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            SetVisual(t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetVisual(1f);
        showRoutine = null;
    }

    private void ResolveReferences()
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
    }

    private void WireButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void ApplyText()
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (achievementsText != null)
        {
            achievementsText.text = achievements;
        }
    }

    private void DisableGameplayObjects()
    {
        if (movementScriptsToDisable != null)
        {
            foreach (MonoBehaviour movementScript in movementScriptsToDisable)
            {
                if (movementScript != null)
                {
                    movementScript.enabled = false;
                }
            }
        }

        if (objectsToHideOnShow != null)
        {
            foreach (GameObject objectToHide in objectsToHideOnShow)
            {
                if (objectToHide != null)
                {
                    objectToHide.SetActive(false);
                }
            }
        }
    }

    private void SetVisibleInstant(bool isVisible)
    {
        if (panelRoot != null && panelRoot != gameObject)
        {
            panelRoot.SetActive(isVisible);
        }
        else if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }

        SetVisual(isVisible ? 1f : 0f);
    }

    private void SetVisual(float t)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = t;
            canvasGroup.interactable = t > 0.99f;
            canvasGroup.blocksRaycasts = t > 0.01f;
        }

        if (boxRect != null)
        {
            boxRect.localScale = Vector3.Lerp(hiddenScale, shownScale, t);
        }

        if (overlayImage != null)
        {
            Color color = overlayImage.color;
            color.a = Mathf.Lerp(0f, 0.72f, t);
            overlayImage.color = color;
        }

        if (boxImage != null)
        {
            Color color = boxImage.color;
            color.a = Mathf.Lerp(0f, 0.94f, t);
            boxImage.color = color;
        }
    }
}
