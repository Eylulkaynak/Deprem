using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bolum5QuestionManager : MonoBehaviour
{
    public static Bolum5QuestionManager Instance;

    public bool IsQuestionOpen { get; private set; }

    [Header("Questions")]
    public Bolum5QuestionData[] questions;

    [Header("UI")]
    public GameObject panel;

    public TMP_Text questionText;
    public TMP_Text resultText;

    public Button buttonA;
    public Button buttonB;

    public TMP_Text buttonAText;
    public TMP_Text buttonBText;

    public Button continueButton;

    private Bolum5QuestionData currentQuestion;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenQuestion(int id)
    {
        IsQuestionOpen = true;

        currentQuestion = questions[id];

        panel.SetActive(true);

        Bolum5PlayerController.Instance.StopMovement();

        questionText.text = currentQuestion.question;

        buttonAText.text = currentQuestion.optionA;
        buttonBText.text = currentQuestion.optionB;

        resultText.text = "";

        buttonA.interactable = true;
        buttonB.interactable = true;

        continueButton.gameObject.SetActive(false);

        buttonA.onClick.RemoveAllListeners();
        buttonB.onClick.RemoveAllListeners();

        buttonA.onClick.AddListener(() => CheckAnswer(0));
        buttonB.onClick.AddListener(() => CheckAnswer(1));
    }

    private void CheckAnswer(int answer)
    {
        buttonA.interactable = false;
        buttonB.interactable = false;

        if (answer == currentQuestion.correctAnswer)
        {
            resultText.text =
                "<color=green><b>✔ Doğru</b></color>\n\n" +
                currentQuestion.explanation;
        }
        else
        {
            resultText.text =
                "<color=red><b>✘ Yanlış</b></color>\n\n" +
                currentQuestion.explanation;
        }

        continueButton.gameObject.SetActive(true);
    }

    public void ContinueGame()
    {
        IsQuestionOpen = false;

        panel.SetActive(false);

        Bolum5PlayerController.Instance.ResumeMovement();
    }
}