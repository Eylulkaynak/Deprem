using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// ============================================================
//  LevelManager.cs
//  Tehlikeleri sayar. Hepsi cozulunce:
//  1) Cocuk uyanir, yurur, kaybolur
//  2) "Tebrikler" paneli acilir
// ============================================================
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Cocuk")]
    [Tooltip("Yatakta uyuyan cocuk (CocukUyanir scripti olan obje)")]
    public CocukUyanir cocuk;
    [Tooltip("Cocuk gittikten sonra panel kac saniye sonra acilsin")]
    public float panelBeklemeSuresi = 1f;

    [Header("Bitis Ekrani")]
    public GameObject tebriklerPaneli;

    [Header("Arayuz")]
    public TMP_Text ilerlemeYazisi;
    public string ilerlemeMetniFormat = "Sabitlenen eşya: {0}/{1}";

    public bool sonrakiSahneyeGec = true;
    public string sonrakiSahneAdi = "Bolum3";
    public float sonrakiSahneBeklemeSuresi = 1.8f;

    [Header("Gorev Kamerasi")]
    public MissionCameraController missionCamera;

    [Header("Ses (opsiyonel)")]
    public AudioClip zaferSesi;

    private int totalHazards = 0;
    private bool bitti = false;
    private AudioSource audioSource;
    private HashSet<GameObject> fixedObjects = new HashSet<GameObject>();

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

        HashSet<GameObject> hazardObjects = new HashSet<GameObject>();

        foreach (WobbleFixer w in FindObjectsByType<WobbleFixer>(FindObjectsSortMode.None))
            hazardObjects.Add(w.gameObject);
        foreach (HazardMover m in FindObjectsByType<HazardMover>(FindObjectsSortMode.None))
            hazardObjects.Add(m.gameObject);
        foreach (DrillFixSequence d in FindObjectsByType<DrillFixSequence>(FindObjectsSortMode.None))
            hazardObjects.Add(d.gameObject);

        totalHazards = hazardObjects.Count;

        Debug.Log("=== TEHLIKELER ===");
        foreach (GameObject go in hazardObjects)
            Debug.Log("  - " + go.name);
        Debug.Log("Toplam: " + totalHazards);

        UpdateProgressUI();
        missionCamera?.SetProgress(0, true);
    }

    public void HazardFixed(GameObject hazardObject)
    {
        if (hazardObject == null || bitti) return;
        if (fixedObjects.Contains(hazardObject)) return;

        fixedObjects.Add(hazardObject);
        Debug.Log("Cozuldu: " + hazardObject.name + "  (" + fixedObjects.Count + " / " + totalHazards + ")");
        UpdateProgressUI();
        missionCamera?.SetProgress(fixedObjects.Count);

        if (fixedObjects.Count >= totalHazards)
        {
            bitti = true;
            StartCoroutine(OyunBitti());
        }
    }

    IEnumerator OyunBitti()
    {
        Debug.Log("*** TUM TEHLIKELER COZULDU - COCUK UYANIYOR ***");

        // 1) Cocugu uyandir
        if (cocuk != null)
        {
            cocuk.Uyan();

            // cocuk kaybolana kadar bekle
            while (cocuk != null && cocuk.gameObject.activeSelf)
                yield return null;
        }

        // 2) Panel ac
        yield return new WaitForSeconds(panelBeklemeSuresi);

        if (zaferSesi != null) audioSource.PlayOneShot(zaferSesi);
        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(true);

        Debug.Log("*** ODA GUVENLI ***");

        if (sonrakiSahneyeGec && !string.IsNullOrWhiteSpace(sonrakiSahneAdi))
        {
            yield return new WaitForSeconds(sonrakiSahneBeklemeSuresi);
            SceneManager.LoadScene(sonrakiSahneAdi);
        }
    }

    public int CozulenSayisi() => fixedObjects.Count;
    public int ToplamSayi() => totalHazards;
    public bool OyunBittiMi() => bitti;

    private void UpdateProgressUI()
    {
        if (ilerlemeYazisi == null)
            return;

        ilerlemeYazisi.text = string.Format(ilerlemeMetniFormat, fixedObjects.Count, totalHazards);
    }
}
