using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyun basinda dogru ve yanlis esya prefablarindan rastgele secip
/// spawn noktalarina yerlestirir. Prefabin kendi rotasyonu korunur;
/// spawn noktasindan sadece konum alinir.
/// </summary>
public class Bolum1ItemSpawner : MonoBehaviour
{
    [Header("Prefab Havuzlari")]
    [Tooltip("Dogru esya prefablari (13 tane vs.). Aralarindan rastgele secilir.")]
    [SerializeField] private List<DraggableItem> correctItemPrefabs = new List<DraggableItem>();

    [Tooltip("Yanlis esya prefablari. Aralarindan rastgele secilir.")]
    [SerializeField] private List<DraggableItem> wrongItemPrefabs = new List<DraggableItem>();

    [Header("Kac Tane Secilecek")]
    [Tooltip("Her oyunda masaya gelecek dogru esya sayisi.")]
    [SerializeField] private int correctItemCount = 6;

    [Tooltip("Her oyunda masaya gelecek yanlis esya sayisi.")]
    [SerializeField] private int wrongItemCount = 3;

    [Header("Spawn Noktalari")]
    [Tooltip("Masadaki bos noktalar. En az dogru+yanlis sayisi kadar olmali.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("Spawn edilen esyalarin toplanacagi parent (bos birakilabilir).")]
    [SerializeField] private Transform itemsParent;

    private void Awake()
    {
        SpawnItems();
    }

    private void SpawnItems()
    {
        int totalToSpawn = correctItemCount + wrongItemCount;

        if (spawnPoints.Count < totalToSpawn)
        {
            Debug.LogWarning($"Bolum1ItemSpawner: {totalToSpawn} esya icin {spawnPoints.Count} spawn noktasi var. Eksik noktalar atlanacak.");
            totalToSpawn = spawnPoints.Count;
        }

        // Secilecek prefablari belirle
        List<DraggableItem> selected = new List<DraggableItem>();
        selected.AddRange(PickRandom(correctItemPrefabs, correctItemCount));
        selected.AddRange(PickRandom(wrongItemPrefabs, wrongItemCount));

        // Spawn noktalarini karistir, sirayla dagit
        List<Transform> shuffledPoints = new List<Transform>(spawnPoints);
        Shuffle(shuffledPoints);

        for (int i = 0; i < selected.Count && i < totalToSpawn; i++)
        {
            DraggableItem prefab = selected[i];
            Transform point = shuffledPoints[i];

            if (prefab == null || point == null)
                continue;

            // Prefabin kendi rotasyonu korunur, konum spawn noktasindan gelir
            Instantiate(prefab, point.position, prefab.transform.rotation, itemsParent);
        }
    }

    /// <summary>Listeden tekrarsiz rastgele 'count' eleman secer.</summary>
    private static List<DraggableItem> PickRandom(List<DraggableItem> pool, int count)
    {
        List<DraggableItem> copy = new List<DraggableItem>(pool);
        Shuffle(copy);

        if (count > copy.Count)
        {
            Debug.LogWarning($"Bolum1ItemSpawner: Havuzda {copy.Count} prefab var, {count} isteniyor. Hepsi kullanilacak.");
            count = copy.Count;
        }

        return copy.GetRange(0, count);
    }

    /// <summary>Fisher-Yates karistirma.</summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
