using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Bolum1GameManager : MonoBehaviour
{
    public static Bolum1GameManager Instance { get; private set; }

    [Header("Ilerleme")]
    [Tooltip("Bos birakilirsa sahnedeki dogru esyalar otomatik sayilir (0 = otomatik).")]
    [SerializeField] private int totalCorrectItemsOverride = 0;

    [Tooltip("Sag ustteki yildiz sayaci yazisi (or. 'Yildiz: 2/4').")]
    [SerializeField] private TMP_Text starText;

    [Header("Bolum Sonu")]
    [Tooltip("Tum dogru esyalar toplaninca acilacak panel.")]
    [SerializeField] private GameObject successPanel;

    [Tooltip("Son esyanin canta animasyonunun bitmesi icin beklenecek sure (saniye).")]
    [SerializeField] private float successPanelDelay = 1f;

    [Header("Geri Bildirim")]
    [SerializeField] private string wrongItemMessage =
        "Bu eğlenceli ama acil durumda önce su, yiyecek ve yardım eşyaları gerekli.";

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

        Debug.Log($"Doğru eşya: {item.name} ({PlacedCorrectCount}/{TotalCorrectItems})");
        Bolum1FeedbackUI.Instance?.ShowCorrectMark();
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
        {
            Bolum1FeedbackUI.Instance.ShowWrongMark();
            Bolum1FeedbackUI.Instance.ShowTemporaryMessage(wrongItemMessage);
        }
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
            starText.text = $"Yıldız: {PlacedCorrectCount}/{totalCorrectItems}";
    }

    private IEnumerator ShowSuccessPanelAfterDelay()
    {
        yield return new WaitForSeconds(successPanelDelay);

        Bolum1FeedbackUI.Instance?.HideMessage();

        if (successPanel != null)
            successPanel.SetActive(true);
    }

    /// <summary>
    /// Bitis panelindeki "Tekrar Oyna" butonundan cagir.
    /// Sahneyi bastan yukler; rastgele esyalar da yeniden spawn olur.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
