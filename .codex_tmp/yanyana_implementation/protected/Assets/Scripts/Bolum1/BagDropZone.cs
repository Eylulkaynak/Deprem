using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BagDropZone : MonoBehaviour
{
    public static BagDropZone Instance { get; private set; }

    [Header("Drop")]
    [SerializeField] private float snapHeightOffset = 0.05f;
    [SerializeField] private float insideDepthOffset = 0.05f;

    [Header("Optional Exact Points")]
    [SerializeField] private Transform openingPoint;
    [SerializeField] private Transform insidePoint;

    [Header("Gorunur Esya Slotlari")]
    [Tooltip("Cantada gorunur kalacak esyalarin dizilecegi noktalar. Sirayla dolar.")]
    [SerializeField] private Transform[] itemSlots;

    private Collider zoneCollider;
    private int nextSlotIndex;

    private void Awake()
    {
        Instance = this;
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        // The item is held slightly above the table/bag while under the finger.
        // A visible drop over the opening should depend on the opening footprint, not drag height.
        Bounds bounds = zoneCollider.bounds;
        return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x &&
               worldPoint.z >= bounds.min.z && worldPoint.z <= bounds.max.z;
    }

    /// <summary>
    /// Kamera açısı nedeniyle sürükleme düzlemi çantanın fiziksel yüksekliğinden farklı olsa bile,
    /// oyuncunun ekranda gördüğü çanta ağzına bırakmayı kabul eder. Yalnız bırakma anında çağrılır.
    /// </summary>
    public bool ContainsScreenPoint(Vector2 screenPoint, Camera worldCamera)
    {
        if (worldCamera == null || zoneCollider == null)
            return false;

        Bounds bounds = zoneCollider.bounds;
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        bool anyPointInFront = false;

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 worldCorner = new Vector3(
                (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
            Vector3 projected = worldCamera.WorldToScreenPoint(worldCorner);
            if (projected.z <= 0f)
                continue;

            anyPointInFront = true;
            minX = Mathf.Min(minX, projected.x);
            minY = Mathf.Min(minY, projected.y);
            maxX = Mathf.Max(maxX, projected.x);
            maxY = Mathf.Max(maxY, projected.y);
        }

        if (!anyPointInFront)
            return false;

        float padding = Mathf.Max(16f, Mathf.Min(Screen.width, Screen.height) * 0.015f);
        return screenPoint.x >= minX - padding && screenPoint.x <= maxX + padding &&
               screenPoint.y >= minY - padding && screenPoint.y <= maxY + padding;
    }

    public Vector3 GetSnapPosition(Vector3 worldPoint)
    {
        Bounds bounds = zoneCollider.bounds;
        Vector3 snapped = new Vector3(
            Mathf.Clamp(worldPoint.x, bounds.min.x, bounds.max.x),
            bounds.max.y + snapHeightOffset,
            Mathf.Clamp(worldPoint.z, bounds.min.z, bounds.max.z)
        );

        return snapped;
    }

    public Vector3 GetInsidePosition()
    {
        if (insidePoint != null)
            return insidePoint.position;

        Bounds bounds = zoneCollider.bounds;
        return new Vector3(
            bounds.center.x,
            bounds.max.y - insideDepthOffset,
            bounds.center.z
        );
    }

    public Vector3 GetOpeningPosition(Vector3 worldPoint)
    {
        if (openingPoint != null)
            return openingPoint.position;

        Bounds bounds = zoneCollider.bounds;
        return new Vector3(
            Mathf.Clamp(worldPoint.x, bounds.min.x, bounds.max.x),
            bounds.max.y + snapHeightOffset,
            Mathf.Clamp(worldPoint.z, bounds.min.z, bounds.max.z)
        );
    }

    /// <summary>
    /// Siradaki bos slotu verir. Slot kalmadiysa false doner
    /// (esya bu durumda gizlenmelidir).
    /// </summary>
    public bool TryClaimSlot(out Vector3 slotPosition)
    {
        while (nextSlotIndex < (itemSlots?.Length ?? 0))
        {
            Transform slot = itemSlots[nextSlotIndex];
            nextSlotIndex++;

            if (slot != null)
            {
                slotPosition = slot.position;
                return true;
            }
        }

        slotPosition = default;
        return false;
    }
}
