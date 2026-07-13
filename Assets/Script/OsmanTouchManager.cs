using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class OsmanTouchManager : MonoBehaviour
{
    public Camera cam;

    [Header("Mobile Tap Fallback")]
    public bool useScreenSpaceFallback = true;
    public float fallbackScreenPickRadius = 220f;
    public bool scaleFallbackRadiusWithScreenSize = true;
    public float fallbackReferenceShortSide = 1080f;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            return;
        }

        if (IsPointerOverUi(pointerId))
        {
            if (useScreenSpaceFallback)
            {
                TryFallbackTap(screenPosition);
            }

            return;
        }

        CheckTap(screenPosition);
    }

    private void CheckTap(Vector2 screenPosition)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            DrillFixSequence drill = hit.collider.GetComponentInParent<DrillFixSequence>();
            if (drill != null && drill.isActiveAndEnabled)
            {
                drill.OnTapped();
                return;
            }

            HazardMover mover = hit.collider.GetComponentInParent<HazardMover>();
            if (mover != null && mover.isActiveAndEnabled)
            {
                mover.OnTapped();
                return;
            }

            WobbleFixer wobble = hit.collider.GetComponentInParent<WobbleFixer>();
            if (wobble != null && wobble.isActiveAndEnabled)
            {
                wobble.OnTapped();
                return;
            }
        }

        if (useScreenSpaceFallback)
        {
            TryFallbackTap(screenPosition);
        }
    }

    private bool TryFallbackTap(Vector2 screenPosition)
    {
        Component bestTarget = null;
        float bestDistance = float.MaxValue;
        float effectiveRadius = GetEffectiveFallbackRadius();

        CheckFallbackCandidates(FindObjectsByType<DrillFixSequence>(FindObjectsSortMode.None), screenPosition, ref bestTarget, ref bestDistance);
        CheckFallbackCandidates(FindObjectsByType<HazardMover>(FindObjectsSortMode.None), screenPosition, ref bestTarget, ref bestDistance);
        CheckFallbackCandidates(FindObjectsByType<WobbleFixer>(FindObjectsSortMode.None), screenPosition, ref bestTarget, ref bestDistance);

        if (bestTarget == null || bestDistance > effectiveRadius)
        {
            return false;
        }

        if (bestTarget is DrillFixSequence drill)
        {
            drill.OnTapped();
            return true;
        }

        if (bestTarget is HazardMover mover)
        {
            mover.OnTapped();
            return true;
        }

        if (bestTarget is WobbleFixer wobble)
        {
            wobble.OnTapped();
            return true;
        }

        return false;
    }

    private void CheckFallbackCandidates<T>(T[] candidates, Vector2 screenPosition, ref Component bestTarget, ref float bestDistance)
        where T : Component
    {
        foreach (T candidate in candidates)
        {
            Behaviour behaviour = candidate as Behaviour;
            if (candidate == null || behaviour == null || !behaviour.isActiveAndEnabled || IsAlreadyHandled(candidate))
            {
                continue;
            }

            if (!TryGetCandidateScreenPosition(candidate, out Vector2 candidateScreenPosition))
            {
                continue;
            }

            float distance = Vector2.Distance(screenPosition, candidateScreenPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }
    }

    private bool IsAlreadyHandled(Component candidate)
    {
        if (candidate is DrillFixSequence drill)
        {
            return drill.IsFixed();
        }

        if (candidate is HazardMover mover)
        {
            return mover.IsMoved();
        }

        if (candidate is WobbleFixer wobble)
        {
            return wobble.IsFixed();
        }

        return false;
    }

    private bool TryGetCandidateScreenPosition(Component candidate, out Vector2 screenPosition)
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        Vector3 worldPosition = GetCandidateWorldPosition(candidate);
        Vector3 projectedPosition = cam.WorldToScreenPoint(worldPosition);
        if (projectedPosition.z < 0f)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        screenPosition = projectedPosition;
        return true;
    }

    private Vector3 GetCandidateWorldPosition(Component candidate)
    {
        if (candidate is DrillFixSequence drill &&
            drill.targetRing != null &&
            drill.targetRing.activeInHierarchy)
        {
            return GetRendererCenterOrTransformPosition(drill.targetRing.transform);
        }

        return GetRendererCenterOrTransformPosition(candidate.transform);
    }

    private Vector3 GetRendererCenterOrTransformPosition(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds.center : root.position;
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
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
