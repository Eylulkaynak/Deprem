using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        foreach (WobbleFixer w in FindObjectsOfType<WobbleFixer>())
            hazardObjects.Add(w.gameObject);
        foreach (HazardMover m in FindObjectsOfType<HazardMover>())
            hazardObjects.Add(m.gameObject);
        foreach (DrillFixSequence d in FindObjectsOfType<DrillFixSequence>())
            hazardObjects.Add(d.gameObject);

        totalHazards = hazardObjects.Count;

        Debug.Log("=== TEHLIKELER ===");
        foreach (GameObject go in hazardObjects)
            Debug.Log("  - " + go.name);
        Debug.Log("Toplam: " + totalHazards);
    }

    public void HazardFixed(GameObject hazardObject)
    {
        if (hazardObject == null || bitti) return;
        if (fixedObjects.Contains(hazardObject)) return;

        fixedObjects.Add(hazardObject);
        Debug.Log("Cozuldu: " + hazardObject.name + "  (" + fixedObjects.Count + " / " + totalHazards + ")");

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
    }

    public int CozulenSayisi() => fixedObjects.Count;
    public int ToplamSayi() => totalHazards;
    public bool OyunBittiMi() => bitti;
}