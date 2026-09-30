using UnityEngine;
using System.Collections;

// ============================================================
//  CocukUyanir.cs
//  1) Kalkma animasyonu + ayni anda yatagin KENARINA kayar
//  2) Yere iner
//  3) Hedefe yurur (artik yataktan gecmez)
//  4) Kaybolur
//
//  KURULUM:
//  - "Yatak Kenari" icin bos obje olustur, yatagin yanina koy.
//  - "Hedef" icin bos obje olustur, kapiya koy.
// ============================================================
public class CocukUyanir : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    public string kalkTrigger = "Kalk";
    public string yuruTrigger = "Yuru";

    [Header("Noktalar")]
    [Tooltip("Cocugun kalkarken kayacagi yer (yatagin kenari/yani)")]
    public Transform yatakKenari;
    [Tooltip("Cocugun yuruyup gidecegi son nokta (kapi)")]
    public Transform hedef;

    [Header("Kalkma")]
    [Tooltip("Kalkma animasyonu kac saniye surer")]
    public float kalkmaSuresi = 2f;
    [Tooltip("Kalkarken kenara kayma suresi (kalkmaSuresi'nden kisa olsun)")]
    public float kenaraKaymaSuresi = 0.3f;
    [Tooltip("Acikken kalkma baslar baslamaz ANINDA kenara isinlanir (gomulmeyi tamamen engeller)")]
    public bool anindaKenaraGec = false;

    [Header("Yere Inme")]
    public float zeminY = 0f;
    public float zeminOffset = 0f;
    public float inmeSuresi = 0.6f;
    public bool otomatikZeminBul = true;

    [Header("Yurume")]
    public float yurumeHizi = 50f;
    public float kaybolmaSuresi = 0.5f;

    [Header("Ses (opsiyonel)")]
    public AudioClip kalkmaSesi;

    private bool basladi = false;
    private AudioSource audioSource;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        audioSource = GetComponent<AudioSource>();
    }

    public void Uyan()
    {
        if (basladi) return;
        basladi = true;
        StartCoroutine(UyanmaRutini());
    }

    IEnumerator UyanmaRutini()
    {
        // ---- 1) KALKMA + ayni anda yatagin kenarina kay ----
        if (animator != null && !string.IsNullOrEmpty(kalkTrigger))
            animator.SetTrigger(kalkTrigger);

        if (kalkmaSesi != null && audioSource != null)
            audioSource.PlayOneShot(kalkmaSesi);

        if (yatakKenari != null && anindaKenaraGec)
        {
            // ANINDA kenara gec (gomulme olmaz)
            transform.position = yatakKenari.position;
            yield return new WaitForSeconds(kalkmaSuresi);
        }
        else if (yatakKenari != null)
        {
            Vector3 baslangicPos = transform.position;
            Vector3 kenarPos = yatakKenari.position;

            float t = 0f;
            float dur = Mathf.Max(0.05f, kenaraKaymaSuresi);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                k = k * k * (3f - 2f * k);   // yumusak
                transform.position = Vector3.Lerp(baslangicPos, kenarPos, k);
                yield return null;
            }
            transform.position = kenarPos;

            // kalkma animasyonu bitene kadar bekle (kalan sure)
            float kalan = kalkmaSuresi - kenaraKaymaSuresi;
            if (kalan > 0f) yield return new WaitForSeconds(kalan);
        }
        else
        {
            // kenar noktasi yoksa sadece bekle
            yield return new WaitForSeconds(kalkmaSuresi);
        }

        // ---- 2) YERE IN ----
        float hedefY = zeminY;

        if (otomatikZeminBul)
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                hedefY = hit.point.y;
                Debug.Log("Zemin bulundu: Y = " + hedefY + " (" + hit.collider.name + ")");
            }
            else
            {
                Debug.LogWarning("Zemin bulunamadi, zeminY kullanilacak: " + zeminY);
            }
        }

        hedefY += zeminOffset;

        Vector3 baslangic = transform.position;
        Vector3 yerePos = new Vector3(baslangic.x, hedefY, baslangic.z);

        float t2 = 0f;
        float dur2 = Mathf.Max(0.05f, inmeSuresi);
        while (t2 < dur2)
        {
            t2 += Time.deltaTime;
            float k = t2 / dur2;
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(baslangic, yerePos, k);
            yield return null;
        }
        transform.position = yerePos;

        // ---- 3) HEDEFE YURU ----
        if (animator != null && !string.IsNullOrEmpty(yuruTrigger))
            animator.SetTrigger(yuruTrigger);

        if (hedef != null)
        {
            Vector3 yon = hedef.position - transform.position;
            yon.y = 0f;
            if (yon.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(yon);

            while (true)
            {
                Vector3 hedefPos = new Vector3(hedef.position.x, transform.position.y, hedef.position.z);
                if (Vector3.Distance(transform.position, hedefPos) < 0.1f) break;

                transform.position = Vector3.MoveTowards(
                    transform.position, hedefPos, yurumeHizi * Time.deltaTime);
                yield return null;
            }
        }

        // ---- 4) KAYBOL ----
        yield return new WaitForSeconds(kaybolmaSuresi);
        gameObject.SetActive(false);
    }

    // Scene'de noktalari gormek icin
    void OnDrawGizmosSelected()
    {
        if (yatakKenari != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, yatakKenari.position);
            Gizmos.DrawWireSphere(yatakKenari.position, 0.3f);
        }
        if (hedef != null)
        {
            Gizmos.color = Color.green;
            Vector3 from = (yatakKenari != null) ? yatakKenari.position : transform.position;
            Gizmos.DrawLine(from, hedef.position);
            Gizmos.DrawWireSphere(hedef.position, 0.3f);
        }
    }
}
