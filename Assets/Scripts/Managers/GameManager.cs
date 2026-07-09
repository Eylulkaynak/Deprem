using System.Collections;
using UnityEngine;
using TMPro;

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

    [Header("Earthquake")]
    public float earthquakeDuration = 5f;
    public float earthquakePower = 0.08f;

    private InteractableArea[] areas;

    private bool earthquakeRunning = false;
    private bool playerMoving = false;
    private bool playerSafe = false;

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
        if (!earthquakeRunning)
            return;

        if (playerMoving)
            return;

        // Yanlış alan
        if (area.areaType == InteractableArea.AreaType.Unsafe)
        {
            score -= wrongPenalty;

            if (score < 0)
                score = 0;

            ShowMistake(area.wrongFeedbackMessage);
            return;
        }

        // Güvenli alan
        playerMoving = true;

    player.MoveTo(area.targetPoint, () =>
        {
        Debug.Log("MOVE CALLBACK ÇALIŞTI");

        playerMoving = false;

        playerSafe = true;

    

     area.HideBubble();
        });
    }

    IEnumerator StartEarthquake()
    {
        yield return new WaitForSeconds(1f);

        earthquakeRunning = true;

        feedbackUI.Show(" Deprem başladı!\nHemen güvenli alana git.");

        yield return new WaitForSecondsRealtime(2f);

        feedbackUI.Hide();

        ShowAllBubbles();

        if (CameraShake.Instance != null)
        {
            yield return StartCoroutine(
                CameraShake.Instance.Shake(
                    earthquakeDuration,
                    earthquakePower));
        }
        else
        {
            yield return new WaitForSeconds(earthquakeDuration);
        }

        earthquakeRunning = false;

        HideAllBubbles();

        if (playerSafe)
        if (playerSafe)
            {
                successPanel.SetActive(true);

                scoreText.text =

                $" Tebrikler!\n\n" +
                $"Deprem sırasında güvenli alana ulaştın.\n\n" +
                $"Puanın\n\n{score} / {maxScore}";
}
        else
        {
            failPanel.SetActive(true);
        }
     
    }

    private void ShowMistake(string message)
    {
        StartCoroutine(MistakeRoutine(message));
    }

    IEnumerator MistakeRoutine(string message)
    {
        Time.timeScale = 0f;

        feedbackUI.Show(message);

        yield return new WaitForSecondsRealtime(2f);

        feedbackUI.Hide();

        Time.timeScale = 1f;
    }

    public int GetScore()
    {
        return score;
    }
}