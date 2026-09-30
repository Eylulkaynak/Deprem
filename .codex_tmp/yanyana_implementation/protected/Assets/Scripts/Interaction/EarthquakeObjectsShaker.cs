using UnityEngine;
using System.Collections;

// Sahnede boş bir GameObject oluştur (örn: "EarthquakeObjectsShaker"),
// bu script'i ona ekle ve sarsılmasını istediğin eşyaları
// (üzerlerinde ObjectShake olan objeleri) "Shakeable Objects" listesine sürükle.
public class EarthquakeObjectsShaker : MonoBehaviour
{
    public static EarthquakeObjectsShaker Instance;

    [Header("Sarsılacak Eşyalar")]
    public ObjectShake[] shakeableObjects;

    [Header("Sarsıntı Ayarları")]
    public float positionPower = 0.05f; // eşyanın ne kadar yer değiştireceği
    public float rotationPower = 4f;    // eşyanın ne kadar sallanacağı (derece)

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public IEnumerator ShakeAll(float duration)
    {
        foreach (ObjectShake obj in shakeableObjects)
        {
            if (obj != null)
                StartCoroutine(obj.Shake(duration, positionPower, rotationPower));
        }

        // GameManager'ın shake bitene kadar beklemesi için
        yield return new WaitForSeconds(duration);
    }
}