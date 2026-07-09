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

    [Header("Sesler")]
    [Tooltip("Sesleri calacak AudioSource. Bos birakilirsa bu objeye otomatik eklenir.")]
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
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

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
        if (messageText == null)
            return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(TypeMessage(message));
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

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private IEnumerator TypeMessage(string message)
    {
        messageText.text = message;
        messageText.maxVisibleCharacters = 0;

        if (Root != null)
            Root.SetActive(true);

        // TMP'nin karakter sayisini dogru hesaplamasi icin mesh guncelle
        messageText.ForceMeshUpdate();
        int totalCharacters = messageText.textInfo.characterCount;

        float visibleCount = 0f;
        while (visibleCount < totalCharacters)
        {
            visibleCount += charactersPerSecond * Time.deltaTime;
            messageText.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visibleCount), totalCharacters);
            yield return null;
        }

        messageText.maxVisibleCharacters = totalCharacters;

        yield return new WaitForSeconds(holdDuration);

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
        yield return new WaitForSeconds(markDuration);
        mark.SetActive(false);
        markRoutine = null;
    }
}
