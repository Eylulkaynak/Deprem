using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject uyariPaneli;
    public TextMeshProUGUI uyariMetni;

    private bool pausedByThisManager;

    private void OnDisable()
    {
        ReleasePauseState();
    }

    private void OnDestroy()
    {
        ReleasePauseState();
    }

    public void UyariGoster(string mesaj)
    {
        bool hasVisiblePanel = uyariPaneli != null;

        if (uyariMetni != null)
        {
            uyariMetni.text = mesaj;
        }

        if (hasVisiblePanel)
        {
            uyariPaneli.SetActive(true);
        }

        if (!hasVisiblePanel)
        {
            return;
        }

        Time.timeScale = 0f;
        pausedByThisManager = true;
    }

    public void UyariyiKapatVeDevamEt()
    {
        if (uyariPaneli != null)
        {
            uyariPaneli.SetActive(false);
        }

        ReleasePauseState();
    }

    private void ReleasePauseState()
    {
        if (!pausedByThisManager)
        {
            return;
        }

        pausedByThisManager = false;
        Time.timeScale = 1f;
    }
}
