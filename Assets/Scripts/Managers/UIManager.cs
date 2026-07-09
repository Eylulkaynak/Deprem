using UnityEngine;
using TMPro; // TextMeshPro için

public class UIManager : MonoBehaviour
{
    [Header("UI Elemanları")]
    public GameObject uyariPaneli; // Ekrana çıkacak genel panel
    public TextMeshProUGUI uyariMetni; // Uyarı yazısının değişeceği text nesnesi

    // Bu fonksiyonu deprem başladığında veya tehlikeli objeye yaklaşıldığında çağır
    public void UyariGoster(string mesaj)
    {
        // 1. Gelen mesajı ekrana yazdır
        uyariMetni.text = mesaj;
        
        // 2. Paneli görünür yap
        uyariPaneli.SetActive(true);
        
        // 3. ZAMANI DURDUR
        Time.timeScale = 0f; 
    }

    // Bu fonksiyonu uyarı panelindeki "Anladım" butonuna bağlayacağız
    public void UyariyiKapatVeDevamEt()
    {
        // 1. Paneli gizle
        uyariPaneli.SetActive(false);
        
        // 2. ZAMANI NORMALE DÖNDÜR
        Time.timeScale = 1f; 
    }
}