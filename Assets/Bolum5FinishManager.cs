using UnityEngine;
using TMPro;

public class Bolum5FinishManager : MonoBehaviour
{
    public static Bolum5FinishManager Instance;

    public GameObject finishPanel;
    public TMP_Text finishText;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowFinish()
    {
        finishPanel.SetActive(true);

        finishText.text =
            "🎉 TEBRİKLER!\n\n" +
            "Güvenli şekilde toplanma alanına ulaştın.\n\n" +
            "Deprem sonrası doğru kararlar vererek\n" +
            "bölümü başarıyla tamamladın.";
    }

    public void CloseFinish()
    {
        finishPanel.SetActive(false);
    }
}