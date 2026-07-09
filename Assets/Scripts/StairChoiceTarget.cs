using UnityEngine;

public class StairChoiceTarget : MonoBehaviour
{
    public enum StairChoice
    {
        Elevator,
        MiddleStairs,
        WallSide
    }

    public StairChoice choice;

    private void Awake()
    {
        WarnIfColliderMissing();
    }

    private void OnValidate()
    {
        WarnIfColliderMissing();
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
            Debug.LogWarning($"StairChoiceTarget '{name}' needs a Collider for mouse/touch selection.");
        }
    }
}
