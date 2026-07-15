using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Score")]
    public int maxScore = 100;
    public int wrongPenalty = 25;

    private int score;

    [Header("References")]
    public EarthquakePlayerController player;
    public FeedbackUI feedbackUI;

    [Header("Result UI")]
    public GameObject successPanel;
    public GameObject failPanel;
    public TMP_Text scoreText;
    public bool loadNextSceneOnSuccess = true;
    public string nextSceneName = "Bolum4";
    public float nextSceneDelay = 2f;
    public bool reloadSceneOnFail = true;
    public float failRestartDelay = 2f;

    [Header("Earthquake")]
    public float earthquakeDuration = 5f;
    public float earthquakePower = 0.08f;
    public float safeArrivalGraceTime = 4f;
    public float earthquakeStartMessageDuration = 2f;

    private InteractableArea[] areas;

    private bool earthquakeRunning = false;
    private bool playerMoving = false;
    private bool playerSafe = false;
    private InteractableArea lastSelectedArea;
    private int lastSelectionFrame = -1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        score = maxScore;

        areas = FindObjectsByType<InteractableArea>(FindObjectsSortMode.None);

        HideAllBubbles();

        if (successPanel != null)
            successPanel.SetActive(false);

        if (failPanel != null)
            failPanel.SetActive(false);

        StartCoroutine(StartEarthquake());
    }

    #region Bubble

    public void ShowAllBubbles()
    {
        foreach (InteractableArea area in areas)
            area.ShowBubble();
    }

    public void HideAllBubbles()
    {
        foreach (InteractableArea area in areas)
            area.HideBubble();
    }

    #endregion

    public void OnAreaSelected(InteractableArea area)
    {
        if (area == null)
            return;

        if (area == lastSelectedArea && lastSelectionFrame == Time.frameCount)
            return;

        lastSelectedArea = area;
        lastSelectionFrame = Time.frameCount;

        if (!earthquakeRunning)
            return;

        if (playerMoving)
            return;

        // Guvensiz alan secildi.
        if (area.areaType == InteractableArea.AreaType.Unsafe)
        {
            score -= wrongPenalty;

            if (score < 0)
                score = 0;

            ShowMistake(area.wrongFeedbackMessage);
            return;
        }

        // Guvenli alan.
        if (player == null || area.targetPoint == null)
        {
            Debug.LogWarning("GameManager: Safe area selected but player or target point is missing.");
            return;
        }

        playerMoving = true;

        player.MoveTo(area.targetPoint, () =>
        {
            playerMoving = false;
            playerSafe = true;

            area.HideBubble();
        });
    }

    private IEnumerator StartEarthquake()
    {
        yield return null;

        earthquakeRunning = true;
        ShowAllBubbles();

        if (feedbackUI != null)
        {
            feedbackUI.ShowTimed(
                "Deprem başladı!\nGüvenli alana dokun. ÇÖK - KAPAN - TUTUN.",
                earthquakeStartMessageDuration);
            yield return new WaitForSeconds(earthquakeStartMessageDuration);
        }

        if (EarthquakeObjectsShaker.Instance != null)
        {
            yield return StartCoroutine(EarthquakeObjectsShaker.Instance.ShakeAll(earthquakeDuration));
        }
        else
        {
            yield return new WaitForSeconds(earthquakeDuration);
        }

        if (playerMoving && safeArrivalGraceTime > 0f)
        {
            float waitUntil = Time.time + safeArrivalGraceTime;
            while (playerMoving && Time.time < waitUntil)
            {
                yield return null;
            }
        }

        earthquakeRunning = false;

        HideAllBubbles();

        if (playerSafe)
        {
            if (successPanel != null)
                successPanel.SetActive(true);

            if (scoreText != null)
            {
                scoreText.text =
                    "Deprem sırasında güvenli alana ulaştın.\n\n" +
                    $"PUAN  {score} / {maxScore}";
            }

            if (loadNextSceneOnSuccess && !string.IsNullOrWhiteSpace(nextSceneName))
            {
                yield return new WaitForSeconds(nextSceneDelay);
                SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            if (failPanel != null)
                failPanel.SetActive(true);

            if (reloadSceneOnFail)
            {
                yield return new WaitForSeconds(failRestartDelay);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    private void ShowMistake(string message)
    {
        if (feedbackUI == null)
        {
            return;
        }

        StartCoroutine(MistakeRoutine(message));
    }

    private IEnumerator MistakeRoutine(string message)
    {
        feedbackUI.Show(message);

        yield return new WaitUntil(() => !feedbackUI.WaitingForClick);
    }

    public int GetScore()
    {
        return score;
    }
}
