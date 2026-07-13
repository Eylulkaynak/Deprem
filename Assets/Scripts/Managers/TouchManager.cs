using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class TouchManager : MonoBehaviour
{
    [Header("Debug")]
    public bool debugMode;

    [Header("Mobile Tap Fallback")]
    public float fallbackScreenPickRadius = 180f;
    public bool scaleFallbackRadiusWithScreenSize = true;
    public float fallbackReferenceShortSide = 1080f;

    void Update()
    {
        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
            return;

        if (IsPointerOverUi(pointerId))
        {
            TrySelectNearestAreaByScreenPosition(screenPosition);
            return;
        }

        CheckTouch(screenPosition);
    }

    void CheckTouch(Vector2 screenPos)
    {
        if (Camera.main == null)
        {
            Debug.LogError("TouchManager: Camera.main bulunamadi! Main Camera tag'i 'MainCamera' mi kontrol et.");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (debugMode)
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (debugMode)
                    Debug.Log($"[TouchManager] Raycast carpti: '{hit.collider.gameObject.name}' (mesafe: {hit.distance:F2})");

                InteractableArea area = hit.collider.GetComponentInParent<InteractableArea>();

                if (area == null)
                    continue;

                if (debugMode)
                    Debug.Log($"[TouchManager] InteractableArea bulundu: {area.areaType}");

                if (GameManager.Instance != null)
                    GameManager.Instance.OnAreaSelected(area);

                return;
            }

            if (debugMode)
                Debug.LogWarning("[TouchManager] Raycast carpti ama InteractableArea bulunamadi.");

            TrySelectNearestAreaByScreenPosition(screenPos);
        }
        else
        {
            if (debugMode)
                Debug.Log("[TouchManager] Raycast hicbir seye carpmadi.");

            TrySelectNearestAreaByScreenPosition(screenPos);
        }
    }

    private bool TrySelectNearestAreaByScreenPosition(Vector2 screenPos)
    {
        if (Camera.main == null || GameManager.Instance == null)
        {
            return false;
        }

        InteractableArea[] areas = FindObjectsByType<InteractableArea>(FindObjectsSortMode.None);
        InteractableArea nearestArea = null;
        float nearestDistance = float.MaxValue;

        foreach (InteractableArea area in areas)
        {
            if (area == null || !area.isActiveAndEnabled)
            {
                continue;
            }

            if (area.bubble != null && !area.bubble.activeInHierarchy)
            {
                continue;
            }

            Vector3 worldPosition = area.bubble != null ? area.bubble.transform.position : area.transform.position;
            Vector3 areaScreenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            if (areaScreenPosition.z < 0f)
            {
                continue;
            }

            float distance = Vector2.Distance(screenPos, areaScreenPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestArea = area;
            }
        }

        if (nearestArea == null || nearestDistance > GetEffectiveFallbackRadius())
        {
            return false;
        }

        if (debugMode)
        {
            Debug.Log($"[TouchManager] Fallback area secildi: {nearestArea.name} ({nearestDistance:F1}px)");
        }

        GameManager.Instance.OnAreaSelected(nearestArea);
        return true;
    }

    private float GetEffectiveFallbackRadius()
    {
        if (!scaleFallbackRadiusWithScreenSize)
        {
            return fallbackScreenPickRadius;
        }

        float shortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
        float referenceShortSide = Mathf.Max(1f, fallbackReferenceShortSide);
        return fallbackScreenPickRadius * shortSide / referenceShortSide;
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        if (UnityEngine.Input.touchCount > 0)
        {
            UnityEngine.Touch touch = UnityEngine.Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            screenPosition = UnityEngine.Input.mousePosition;
            pointerId = -1;
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0;
            return true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }
#endif

        screenPosition = Vector2.zero;
        pointerId = -1;
        return false;
    }

    private bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
