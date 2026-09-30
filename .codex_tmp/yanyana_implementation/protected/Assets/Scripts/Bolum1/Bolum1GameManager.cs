using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Bolum1GameManager : MonoBehaviour
{
    public static Bolum1GameManager Instance { get; private set; }

    [Header("Ilerleme")]
    [Tooltip("Bos birakilirsa sahnedeki dogru esyalar otomatik sayilir (0 = otomatik).")]
    [SerializeField] private int totalCorrectItemsOverride = 0;

    [Tooltip("Sag ustteki yildiz sayaci yazisi (or. 'Yildiz: 2/4').")]
    [SerializeField] private TMP_Text starText;

    [Tooltip("HUD uzerindeki sayisal ilerleme (or. '3 / 13').")]
    [SerializeField] private TMP_Text progressText;

    [Tooltip("HUD uzerindeki yatay ilerleme cubugunun dolan gorseli.")]
    [SerializeField] private Image progressFill;

    [Header("Bolum Sonu")]
    [Tooltip("Tum dogru esyalar toplaninca acilacak panel.")]
    [SerializeField] private GameObject successPanel;

    [Tooltip("Son esyanin canta animasyonunun bitmesi icin beklenecek sure (saniye).")]
    [SerializeField] private float successPanelDelay = 1f;

    [Tooltip("Bolum bitince siradaki sahneye otomatik gec.")]
    [SerializeField] private bool loadNextSceneOnComplete = true;

    [SerializeField] private string nextSceneName = "Bolum2";

    [SerializeField] private float nextSceneDelay = 1.5f;

    [Header("Geri Bildirim")]
    [SerializeField] private string wrongItemMessage =
        "Acil durumda once su, yiyecek, fener, radyo, ilk yardim ve temel esyalar gerekli.";

    private readonly HashSet<DraggableItem> placedCorrectItems = new HashSet<DraggableItem>();
    private int totalCorrectItems;

    public int PlacedCorrectCount => placedCorrectItems.Count;
    public int TotalCorrectItems => totalCorrectItems;
    public bool IsCompleted { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        totalCorrectItems = totalCorrectItemsOverride > 0
            ? totalCorrectItemsOverride
            : CountCorrectItemsInScene();

        if (successPanel != null)
            successPanel.SetActive(false);

        UpdateStarText();
    }

    public void OnCorrectItemPlaced(DraggableItem item)
    {
        if (item == null || !placedCorrectItems.Add(item))
            return;

        Debug.Log($"Dogru esya: {item.name} ({PlacedCorrectCount}/{TotalCorrectItems})");
        Bolum1FeedbackUI.Instance?.ShowCorrectItem(item.DisplayName);
        Bolum1HintSystem.Instance?.ResetTimer();
        UpdateStarText();

        if (!IsCompleted && PlacedCorrectCount >= totalCorrectItems)
        {
            IsCompleted = true;
            StartCoroutine(ShowSuccessPanelAfterDelay());
        }
    }

    public void OnWrongItemPlaced(DraggableItem item)
    {
        if (item == null)
            return;

        if (Bolum1FeedbackUI.Instance != null)
            Bolum1FeedbackUI.Instance.ShowWrongItem(wrongItemMessage);
        else
            Debug.Log(wrongItemMessage);

        item.ReturnToStart();
    }

    private int CountCorrectItemsInScene()
    {
        int count = 0;
        foreach (DraggableItem item in FindObjectsByType<DraggableItem>(FindObjectsSortMode.None))
        {
            if (item.IsCorrectItem)
                count++;
        }

        return count;
    }

    private void UpdateStarText()
    {
        if (starText != null)
        {
            starText.text = progressText != null
                ? "<b>AFET CANTANI HAZIRLA</b>\n<size=21><color=#9FAEAA>Gerekli esyalari sec</color></size>"
                : $"<b>AFET CANTANI HAZIRLA</b>\nGerekli esyalari sec  {PlacedCorrectCount}/{totalCorrectItems}";
        }

        if (progressText != null)
            progressText.text = $"{PlacedCorrectCount:00}/{totalCorrectItems:00}";

        if (progressFill != null)
        {
            float normalizedProgress = totalCorrectItems > 0
                ? (float)PlacedCorrectCount / totalCorrectItems
                : 0f;

            progressFill.enabled = normalizedProgress > 0f;
            RectTransform fillRect = progressFill.rectTransform;
            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = normalizedProgress;
            fillRect.anchorMax = anchorMax;
        }
    }

    private IEnumerator ShowSuccessPanelAfterDelay()
    {
        yield return new WaitForSeconds(successPanelDelay);

        Bolum1FeedbackUI.Instance?.HideMessage();

        if (successPanel != null)
        {
            successPanel.transform.SetAsLastSibling();
            successPanel.SetActive(true);
        }

        if (loadNextSceneOnComplete)
        {
            yield return new WaitForSeconds(nextSceneDelay);
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
            return;

        SceneManager.LoadScene(nextSceneName);
    }

    public void ContinueToNextScene()
    {
        LoadNextScene();
    }
}
