using UnityEngine;

/// <summary>
/// Spawn sonrasi masadaki esyalar icin ana Canvas altinda UI label uretir.
/// Tek Canvas, N UI child — her esyaya ayri Canvas acilmaz.
/// </summary>
public class Bolum1ItemLabelManager : MonoBehaviour
{
    public static Bolum1ItemLabelManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("UI_ItemLabel prefabi (Image + TMP).")]
    [SerializeField] private Bolum1ItemLabelUI labelPrefab;

    [Tooltip("Canvas altindaki ItemLabels bos objesi.")]
    [SerializeField] private Transform labelsParent;

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
        CreateLabelsForSceneItems();
    }

    public void CreateLabelsForSceneItems()
    {
        if (labelPrefab == null || labelsParent == null)
        {
            Debug.LogWarning("Bolum1ItemLabelManager: labelPrefab veya labelsParent bagli degil.");
            return;
        }

        // Eski label varsa temizle (tekrar cagrilirsa)
        for (int i = labelsParent.childCount - 1; i >= 0; i--)
            Destroy(labelsParent.GetChild(i).gameObject);

        DraggableItem[] items = FindObjectsByType<DraggableItem>(FindObjectsSortMode.None);
        foreach (DraggableItem item in items)
        {
            if (item == null)
                continue;

            Bolum1ItemLabelUI label = Instantiate(labelPrefab, labelsParent);
            label.name = $"Label_{item.DisplayName}";
            label.Bind(item);
        }
    }
}
