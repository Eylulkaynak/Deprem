using UnityEngine;
using TMPro;

public class EarthquakeGameManager : MonoBehaviour
{
    public static EarthquakeGameManager Instance;

    [Header("Player")]
    [SerializeField] private EarthquakePlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private GameObject successPanel;

    private int currentStep = 0;

    public int CurrentStep => currentStep;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (successPanel != null)
            successPanel.SetActive(false);
    }

    public void OnCorrectAction(int step)
    {
        if (step != currentStep)
            return;

        switch (currentStep)
        {
            case 0:
                playerController.DoDrop();
                break;

            case 1:
                playerController.DoCover();
                break;

            case 2:
                playerController.DoHold();
                break;
        }

        currentStep++;

        if (currentStep >= 3)
        {
            GameCompleted();
        }
    }

    public void OnWrongAction(string message)
    {
        warningPanel.SetActive(true);
        warningText.text = message;

        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        warningPanel.SetActive(false);
        Time.timeScale = 1;
    }

    private void GameCompleted()
    {
        successPanel.SetActive(true);
        Time.timeScale = 0;
    }
}