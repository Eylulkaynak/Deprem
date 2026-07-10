using UnityEngine;

public class SafeAreaClickTarget : MonoBehaviour
{
    public SafeAreaMissionManager safeAreaMissionManager;

    private void Awake()
    {
        EnsureCollider();
    }

    private void OnValidate()
    {
        WarnIfColliderMissing();
    }

    private void OnMouseDown()
    {
        NotifyClicked();
    }

    public void NotifyClicked()
    {
        Debug.Log("Safe area target clicked");

        if (safeAreaMissionManager == null)
        {
            safeAreaMissionManager = FindObjectOfType<SafeAreaMissionManager>();
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

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = false;
        Debug.LogWarning($"SafeAreaClickTarget '{name}' had no Collider. A SphereCollider was added automatically.");
    }
}
