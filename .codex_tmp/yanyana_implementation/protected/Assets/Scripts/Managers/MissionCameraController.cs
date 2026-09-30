using System.Collections;
using UnityEngine;

public class MissionCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform[] cameraPoints;

    [Header("Progress")]
    [Min(1)]
    [SerializeField] private int progressPerPoint = 1;
    [SerializeField] private bool cyclePoints = true;

    [Header("Transition")]
    [Min(0f)]
    [SerializeField] private float transitionDuration = 0.65f;
    [SerializeField] private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine transitionRoutine;
    private int currentPointIndex = -1;

    public void SetProgress(int completedCount, bool instant = false)
    {
        if (!TryResolveCamera() || cameraPoints == null || cameraPoints.Length == 0)
            return;

        int pointIndex = Mathf.Max(0, completedCount) / Mathf.Max(1, progressPerPoint);
        pointIndex = cyclePoints
            ? pointIndex % cameraPoints.Length
            : Mathf.Min(pointIndex, cameraPoints.Length - 1);

        MoveToPoint(pointIndex, instant);
    }

    public void MoveToPoint(int pointIndex, bool instant = false)
    {
        if (!TryResolveCamera() || cameraPoints == null || cameraPoints.Length == 0)
            return;

        pointIndex = Mathf.Clamp(pointIndex, 0, cameraPoints.Length - 1);
        Transform cameraPoint = cameraPoints[pointIndex];
        if (cameraPoint == null || (!instant && pointIndex == currentPointIndex))
            return;

        currentPointIndex = pointIndex;
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        if (instant || transitionDuration <= 0f)
        {
            ApplyPose(cameraPoint);
            transitionRoutine = null;
            return;
        }

        transitionRoutine = StartCoroutine(MoveCamera(cameraPoint));
    }

    private IEnumerator MoveCamera(Transform cameraPoint)
    {
        Transform cameraTransform = targetCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / transitionDuration);
            float curved = transitionCurve.Evaluate(normalized);

            cameraTransform.position = Vector3.LerpUnclamped(
                startPosition,
                cameraPoint.position,
                curved);
            cameraTransform.rotation = Quaternion.SlerpUnclamped(
                startRotation,
                cameraPoint.rotation,
                curved);
            yield return null;
        }

        ApplyPose(cameraPoint);
        transitionRoutine = null;
    }

    private void ApplyPose(Transform cameraPoint)
    {
        targetCamera.transform.SetPositionAndRotation(
            cameraPoint.position,
            cameraPoint.rotation);
    }

    private bool TryResolveCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null;
    }

    private void OnValidate()
    {
        progressPerPoint = Mathf.Max(1, progressPerPoint);
        transitionDuration = Mathf.Max(0f, transitionDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraPoints == null)
            return;

        Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.9f);
        for (int index = 0; index < cameraPoints.Length; index++)
        {
            Transform point = cameraPoints[index];
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(point.position, 0.15f);
            Gizmos.DrawRay(point.position, point.forward * 0.75f);

            if (index > 0 && cameraPoints[index - 1] != null)
                Gizmos.DrawLine(cameraPoints[index - 1].position, point.position);
        }
    }
}
