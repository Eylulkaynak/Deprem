using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public bool useMainCameraIfEmpty = true;

    private void LateUpdate()
    {
        if (cameraTransform == null && useMainCameraIfEmpty && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 direction = transform.position - cameraTransform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
