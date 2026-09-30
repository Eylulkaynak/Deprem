using UnityEngine;
using System.Collections;

// Bu script'i sarsılmasını istediğin HER eşyaya (dolap, yatak, masa, lamba vb.) ekle.
public class ObjectShake : MonoBehaviour
{
    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private float seed;

    void Awake()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;

        // Her eşya farklı bir "noise" ile sarsılsın diye rastgele bir seed veriyoruz.
        // Böylece tüm eşyalar birebir aynı anda aynı yöne gitmez, daha doğal görünür.
        seed = Random.Range(0f, 1000f);
    }

    public IEnumerator Shake(float duration, float positionPower, float rotationPower)
    {
        float t = 0f;

        while (t < duration)
        {
            float noiseX = (Mathf.PerlinNoise(seed, Time.time * 12f) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(seed + 10f, Time.time * 9f) - 0.5f) * 2f;
            float noiseZ = (Mathf.PerlinNoise(seed + 20f, Time.time * 12f) - 0.5f) * 2f;

            transform.localPosition = startLocalPos + new Vector3(noiseX, noiseY, noiseZ) * positionPower;

            float rotZ = (Mathf.PerlinNoise(seed + 30f, Time.time * 8f) - 0.5f) * 2f * rotationPower;
            transform.localRotation = startLocalRot * Quaternion.Euler(0f, 0f, rotZ);

            t += Time.deltaTime;
            yield return null;
        }

        // Sarsıntı bitince eşya eski yerine geri dönsün
        transform.localPosition = startLocalPos;
        transform.localRotation = startLocalRot;
    }
}