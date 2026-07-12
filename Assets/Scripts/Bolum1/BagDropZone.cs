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
        return zoneCollider.bounds.Contains(worldPoint);
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
