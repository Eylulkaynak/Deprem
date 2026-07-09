using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FeedbackUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text messageText;
    public Image mascotImage;
    public Sprite mascotWorried;
    public Sprite mascotHappy;

    [Header("Animasyon")]
    public float popDuration = 0.25f;

    public void Show(string message)
    {
        panel.SetActive(true);
        messageText.text = message;
        mascotImage.sprite = mascotWorried;
        StartCoroutine(PopIn());
    }

    private IEnumerator PopIn()
    {
        panel.transform.localScale = Vector3.zero;
        float t = 0f;

        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float scale = Mathf.SmoothStep(0f, 1f, t / popDuration);
            panel.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        panel.transform.localScale = Vector3.one;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}