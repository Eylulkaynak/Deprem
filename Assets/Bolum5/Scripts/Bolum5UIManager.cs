using TMPro;
using UnityEngine;

public class Bolum5UIManager : MonoBehaviour
{
    public static Bolum5UIManager Instance;

    public GameObject warningPanel;
    public TMP_Text warningText;

    private void Awake()
    {
        Instance = this;
    }

    public void UyariGoster(string message)
    {
        warningPanel.SetActive(true);
        warningText.text = message;

        Bolum5PlayerController.Instance.StopMovement();
    }

    public void UyariyiKapat()
    {
        warningPanel.SetActive(false);

        Bolum5PlayerController.Instance.ResumeMovement();
    }
}