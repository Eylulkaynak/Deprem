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

    // GameManager buradan bekleyecek
    public bool WaitingForClick { get; private set; }

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(string message)
    {
        StopAllCoroutines();

        panel.SetActive(true);
        messageText.text = message;
        mascotImage.sprite = mascotWorried;

        WaitingForClick = true;

        // Oyunu durdur
        Time.timeScale = 0f;

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

        // Animasyon bittikten sonra tıklamayı bekle
        while (WaitingForClick)
        {
        #if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        #else
            if (Input.GetMouseButtonDown(0))
        #endif
            {
                Hide();
            }

            yield return null;
        }
    }

    public void Hide()
    {
        WaitingForClick = false;

        panel.SetActive(false);

        // Oyunu devam ettir
        Time.timeScale = 1f;
    }
}