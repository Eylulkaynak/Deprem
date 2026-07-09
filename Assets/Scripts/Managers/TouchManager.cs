using UnityEngine;

public class TouchManager : MonoBehaviour
{
    [Header("Debug")]
    public bool debugMode = true;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            CheckTouch(Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            CheckTouch(Input.GetTouch(0).position);
#endif
    }

    void CheckTouch(Vector2 screenPos)
    {
        if (Camera.main == null)
        {
            Debug.LogError("TouchManager: Camera.main bulunamadı! Main Camera'nın Tag'i 'MainCamera' mi kontrol et.");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (debugMode)
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (debugMode)
                Debug.Log($"[TouchManager] Raycast çarptı: '{hit.collider.gameObject.name}' (mesafe: {hit.distance:F2})");

            InteractableArea area = hit.collider.GetComponent<InteractableArea>();

            if (area == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[TouchManager] '{hit.collider.gameObject.name}' üzerinde InteractableArea component'i YOK. " +
                                      $"Bu obje tıklamayı engelliyor olabilir (önündeki başka bir collider).");
                return;
            }

            if (debugMode)
                Debug.Log($"[TouchManager] InteractableArea bulundu: {area.areaType}");

            GameManager.Instance.OnAreaSelected(area);
        }
        else
        {
            if (debugMode)
                Debug.Log("[TouchManager] Raycast hiçbir şeye çarpmadı.");
        }
    }
}