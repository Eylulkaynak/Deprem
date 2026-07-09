using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncu belirli sure etkilesime girmezse masadaki dogru esyalardan birini
/// yanip sondurur. Etkilesim olana kadar ayni esya uzerinde
/// "efekt -> kisa bekleme -> efekt" dongusu surer.
/// Oyuncu surukleme yapinca dongu durur ve ana sayac bastan baslar.
/// </summary>
public class Bolum1HintSystem : MonoBehaviour
{
    public static Bolum1HintSystem Instance { get; private set; }

    [Header("Zamanlama")]
    [Tooltip("Ilk ipucu oncesi beklenecek hareketsizlik suresi (saniye).")]
    [SerializeField] private float idleTimeBeforeHint = 10f;

    [Tooltip("Efekt bittikten sonra tekrar oynatilmadan once beklenecek sure (saniye).")]
    [SerializeField] private float delayBetweenPulses = 2f;

    [Header("Yanip Sonme Efekti")]
    [Tooltip("Esyanin buyuyecegi oran (1.2 = %20 buyur).")]
    [SerializeField] private float pulseScale = 1.2f;

    [Tooltip("Tek bir yanip sonmenin suresi (saniye).")]
    [SerializeField] private float pulseDuration = 0.5f;

    [Tooltip("Her efektte kac kez yanip sonecegi.")]
    [SerializeField] private int pulseCount = 3;

    private readonly List<DraggableItem> allItems = new List<DraggableItem>();
    private float idleTimer;
    private int nextHintIndex;
    private Coroutine hintLoopRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        allItems.AddRange(FindObjectsByType<DraggableItem>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        // Oyuncu etkilesime girdi: dongu varsa durdur, ana sayaci sifirla
        if (DraggableItem.IsAnyDragging)
        {
            StopHintLoop();
            idleTimer = 0f;
            return;
        }

        // Ipucu dongusu calisiyorsa ana sayac islemez
        if (hintLoopRoutine != null)
            return;

        idleTimer += Time.deltaTime;

        if (idleTimer >= idleTimeBeforeHint)
        {
            idleTimer = 0f;
            hintLoopRoutine = StartCoroutine(HintLoop());
        }
    }

    /// <summary>Dogru esya konunca cagrilir: donguyu durdurur, sayaci sifirlar.</summary>
    public void ResetTimer()
    {
        StopHintLoop();
        idleTimer = 0f;
    }

    private void StopHintLoop()
    {
        if (hintLoopRoutine != null)
        {
            StopCoroutine(hintLoopRoutine);
            hintLoopRoutine = null;
        }
    }

    private IEnumerator HintLoop()
    {
        DraggableItem hintItem = PickNextHintItem();

        while (hintItem != null)
        {
            // Esya bu arada cantaya girdiyse / kullanilamaz olduysa yenisini sec
            if (!hintItem.IsIdleOnTable || hintItem.IsInBag)
            {
                hintItem = PickNextHintItem();
                if (hintItem == null)
                    break;
            }

            hintItem.PlayHintPulse(pulseScale, pulseDuration, pulseCount);

            // Efekt suresi + efektler arasi bekleme
            yield return new WaitForSeconds(pulseDuration * pulseCount + delayBetweenPulses);
        }

        hintLoopRoutine = null;
    }

    private DraggableItem PickNextHintItem()
    {
        List<DraggableItem> candidates = new List<DraggableItem>();

        foreach (DraggableItem item in allItems)
        {
            if (item != null && item.IsCorrectItem && !item.IsInBag && item.IsIdleOnTable)
                candidates.Add(item);
        }

        if (candidates.Count == 0)
            return null;

        // Her ipucu oturumunda siradaki esyaya gec
        DraggableItem chosen = candidates[nextHintIndex % candidates.Count];
        nextHintIndex++;
        return chosen;
    }
}
