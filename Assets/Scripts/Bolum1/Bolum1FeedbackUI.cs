using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Yanlis esya uyarisini harf harf (typewriter) yazar.
/// Yazi ve opsiyonel arka plan, messageRoot uzerinden acilip kapanir.
/// </summary>
public class Bolum1FeedbackUI : MonoBehaviour
{
    public static Bolum1FeedbackUI Instance { get; private set; }

    [Header("Yazi")]
    [Tooltip("Uyari yazisi (TextMeshPro).")]
    [SerializeField] private TMP_Text messageText;

    [Tooltip("Yazi + arka plan iceren kok obje. Bos birakilirsa yazinin kendisi acilip kapanir.")]
    [SerializeField] private GameObject messageRoot;

    [Header("Typewriter")]
    [Tooltip("Saniyede yazilacak harf sayisi.")]
    [SerializeField] private float charactersPerSecond = 30f;

    [Tooltip("Yazi tamamlandiktan sonra ekranda kalma suresi (saniye).")]
    [SerializeField] private float holdDuration = 2f;

    [Header("Dogru / Yanlis Isareti")]
    [Tooltip("Dogru secimde kisa sure gorunen tik objesi (UI GameObject).")]
    [SerializeField] private GameObject correctMarkRoot;

    [Tooltip("Yanlis secimde kisa sure gorunen carpi objesi (UI GameObject).")]
    [SerializeField] private GameObject wrongMarkRoot;

    [Tooltip("Tik/carpinin ekranda kalma suresi (saniye).")]
    [SerializeField] private float markDuration = 0.6f;

    [Header("Mesaj Renkleri")]
    [SerializeField] private Color positiveTextColor = new Color(0.35f, 1f, 0.72f, 1f);
    [SerializeField] private Color warningTextColor = new Color(1f, 0.82f, 0.3f, 1f);
    [SerializeField] private float positiveMessageDuration = 1.1f;

    [Header("Sesler")]
    [Tooltip("Sesleri calacak, sahnede hazirlanan AudioSource.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Dogru esyada calacak ses.")]
    [SerializeField] private AudioClip correctSound;

    [Tooltip("Yanlis esyada calacak ses.")]
    [SerializeField] private AudioClip wrongSound;

    [Range(0f, 1f)]
    [Tooltip("Ses siddeti.")]
    [SerializeField] private float soundVolume = 1f;

    private Coroutine messageRoutine;
    private Coroutine markRoutine;

    private GameObject Root => messageRoot != null ? messageRoot : (messageText != null ? messageText.gameObject : null);

    private void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        if (Root != null)
            Root.SetActive(false);

        if (correctMarkRoot != null)
            correctMarkRoot.SetActive(false);

        if (wrongMarkRoot != null)
            wrongMarkRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Mesaji harf harf yazar, bekler ve gizler.</summary>
    public void ShowTemporaryMessage(string message)
    {
        BeginMessage(message, warningTextColor, holdDuration, true);
    }

    public void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (Root != null)
            Root.SetActive(false);

        if (markRoutine != null)
        {
            StopCoroutine(markRoutine);
            markRoutine = null;
        }

        HideMark(correctMarkRoot);
        HideMark(wrongMarkRoot);
    }

    public void ShowCorrectMark()
    {
        ShowMark(correctMarkRoot);
        PlaySound(correctSound);
    }

    public void ShowWrongMark()
    {
        ShowMark(wrongMarkRoot);
        PlaySound(wrongSound);
    }

    public void ShowCorrectItem(string displayName)
    {
        ShowCorrectMark();
        BeginMessage($"{displayName} cantaya eklendi!", positiveTextColor, positiveMessageDuration, false);
    }

    public void ShowWrongItem(string message)
    {
        ShowWrongMark();
        ShowTemporaryMessage(message);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void BeginMessage(string message, Color textColor, float duration, bool typewriter)
    {
        if (messageText == null)
            return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowMessage(message, textColor, duration, typewriter));
    }

    private IEnumerator ShowMessage(string message, Color textColor, float duration, bool typewriter)
    {
        messageText.color = textColor;
        messageText.text = message;
        messageText.maxVisibleCharacters = typewriter ? 0 : int.MaxValue;

        if (Root != null)
            Root.SetActive(true);

        if (typewriter)
        {
            messageText.ForceMeshUpdate();
            int totalCharacters = messageText.textInfo.characterCount;

            float visibleCount = 0f;
            while (visibleCount < totalCharacters)
            {
                visibleCount += charactersPerSecond * Time.unscaledDeltaTime;
                messageText.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visibleCount), totalCharacters);
                yield return null;
            }

            messageText.maxVisibleCharacters = totalCharacters;
        }

        yield return new WaitForSecondsRealtime(duration);

        if (Root != null)
            Root.SetActive(false);

        messageRoutine = null;
    }

    private void ShowMark(GameObject mark)
    {
        if (mark == null)
            return;

        if (markRoutine != null)
            StopCoroutine(markRoutine);

        if (correctMarkRoot != null)
            correctMarkRoot.SetActive(false);

        if (wrongMarkRoot != null)
            wrongMarkRoot.SetActive(false);

        markRoutine = StartCoroutine(ShowMarkRoutine(mark));
    }

    private IEnumerator ShowMarkRoutine(GameObject mark)
    {
        mark.SetActive(true);

        Transform markTransform = mark.transform;
        Vector3 restingScale = Vector3.one;
        float elapsed = 0f;
        const float popDuration = 0.16f;
        const float settleDuration = 0.12f;

        markTransform.localScale = restingScale * 0.65f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            markTransform.localScale = Vector3.LerpUnclamped(restingScale * 0.65f, restingScale * 1.18f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            markTransform.localScale = Vector3.LerpUnclamped(restingScale * 1.18f, restingScale, t);
            yield return null;
        }

        float remainingDuration = Mathf.Max(0f, markDuration - popDuration - settleDuration);
        if (remainingDuration > 0f)
            yield return new WaitForSecondsRealtime(remainingDuration);

        markTransform.localScale = restingScale;
        mark.SetActive(false);
        markRoutine = null;
    }

    private static void HideMark(GameObject mark)
    {
        if (mark == null)
            return;

        mark.transform.localScale = Vector3.one;
        mark.SetActive(false);
    }
}
