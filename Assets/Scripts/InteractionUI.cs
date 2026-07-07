using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text interactionText;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(string text)
    {
        panel.SetActive(true);
        interactionText.text = text;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}