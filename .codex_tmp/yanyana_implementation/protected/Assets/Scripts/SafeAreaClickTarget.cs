using UnityEngine;
using UnityEngine.EventSystems;

public class SafeAreaClickTarget : MonoBehaviour
{
    public SafeAreaMissionManager safeAreaMissionManager;

    private void Awake()
    {
        WarnIfColliderMissing();
    }

    private void OnValidate()
    {
        WarnIfColliderMissing();
    }

    private void OnMouseDown()
    {
        if (IsPointerOverUi())
        {
            return;
        }

        NotifyClicked();
    }

    public void NotifyClicked()
    {
        Debug.Log("Safe area target clicked");

        if (safeAreaMissionManager == null)
        {
            safeAreaMissionManager = FindFirstObjectByType<SafeAreaMissionManager>();
        }

        if (safeAreaMissionManager != null)
        {
            safeAreaMissionManager.OnSafeAreaTargetClicked(this);
            return;
        }

        Debug.LogWarning("SafeAreaClickTarget: SafeAreaMissionManager reference is not assigned and could not be found.");
    }

    [ContextMenu("Add SphereCollider If Missing")]
    public void AddSphereColliderIfMissing()
    {
        if (GetComponent<Collider>() == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = false;
        }
    }

    private void WarnIfColliderMissing()
    {
        if (gameObject.scene.IsValid() && GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"SafeAreaClickTarget '{name}' needs a Collider for mouse/touch selection.");
        }
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (UnityEngine.Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId);
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
        {
            return EventSystem.current.IsPointerOverGameObject(0);
        }
#endif

        return false;
    }
}
