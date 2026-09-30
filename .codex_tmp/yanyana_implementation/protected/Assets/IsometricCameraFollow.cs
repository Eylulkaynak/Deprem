using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string fallbackTargetName = "Boy0";
    [SerializeField] private bool autoFindTarget = true;

    [Header("Orbit Isometric Settings")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float yawAngle = 120f;
    [SerializeField] private float pitchAngle = 0f;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1f, 0f);

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 6f;
    [SerializeField] private bool snapIfTooFar = true;
    [SerializeField] private float maxDistanceBeforeSnap = 25f;

    private void Awake()
    {
        FindTargetIfNeeded();
    }

    private void LateUpdate()
    {
        FindTargetIfNeeded();

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = CalculateCameraPosition();

        if (snapIfTooFar && Vector3.Distance(transform.position, desiredPosition) > maxDistanceBeforeSnap)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime);
        }

        LookAtTarget();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        FindTargetIfNeeded();

        if (target == null)
        {
            Debug.LogWarning("IsometricCameraFollow: Target is not assigned.");
            return;
        }

        transform.position = CalculateCameraPosition();
        LookAtTarget();
    }

    public void SetOrbitValues(float newDistance, float newHeight, float newYawAngle)
    {
        distance = newDistance;
        height = newHeight;
        yawAngle = newYawAngle;
        SnapToTarget();
    }

    public void SetCameraSettings(float newDistance, float newHeight, float newYawAngle, Vector3 newFocusOffset)
    {
        distance = newDistance;
        height = newHeight;
        yawAngle = newYawAngle;
        focusOffset = newFocusOffset;
    }

    private Vector3 CalculateCameraPosition()
    {
        Vector3 orbitOffset = Quaternion.Euler(pitchAngle, yawAngle, 0f) *
            new Vector3(0f, height, -distance);

        return target.position + orbitOffset;
    }

    private void LookAtTarget()
    {
        Vector3 focusPoint = GetFocusPoint();
        Vector3 direction = focusPoint - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private Vector3 GetFocusPoint()
    {
        if (target == null)
        {
            return transform.position;
        }

        return target.position + focusOffset;
    }

    private void FindTargetIfNeeded()
    {
        if (target != null || !autoFindTarget || string.IsNullOrEmpty(fallbackTargetName))
        {
            return;
        }

        GameObject foundTarget = GameObject.Find(fallbackTargetName);
        if (foundTarget != null)
        {
            target = foundTarget.transform;
        }
    }
}
