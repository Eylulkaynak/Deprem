using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class EarthquakePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float arrivalThreshold = 0.1f;
    public string speedParameter = "Speed";
    public float animatorWalkReferenceSpeed = 2f;
    public float maxAnimatorMoveSpeed = 2f;
    public bool useTransformFallbackWhenControllerStuck = true;
    public float stuckMoveEpsilon = 0.001f;

    private CharacterController controller;
    private Animator playerAnimator;
    private bool hasSpeedParameter;

    private Transform targetPoint;
    private Action onArrivedCallback;
    private bool isMoving = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
        hasSpeedParameter = HasAnimatorFloat(speedParameter);
        SetAnimatorSpeed(0f);
    }

    private void Update()
    {
        if (isMoving && targetPoint != null)
        {
            MoveToTarget();
        }
    }

    private void MoveToTarget()
    {
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0;
        
        if (direction.magnitude < arrivalThreshold)
        {
            isMoving = false;
            SetAnimatorSpeed(0f);

            onArrivedCallback?.Invoke();
            onArrivedCallback = null;
            return;
        }

        direction.Normalize();
        Vector3 positionBeforeMove = transform.position;
        controller.Move(direction * moveSpeed * Time.deltaTime);

        if (useTransformFallbackWhenControllerStuck &&
            (transform.position - positionBeforeMove).sqrMagnitude <= stuckMoveEpsilon * stuckMoveEpsilon)
        {
            Vector3 fallbackTarget = new Vector3(targetPoint.position.x, transform.position.y, targetPoint.position.z);
            transform.position = Vector3.MoveTowards(
                transform.position,
                fallbackTarget,
                moveSpeed * Time.deltaTime);
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        SetAnimatorSpeed(1f);
    }

    public void MoveTo(Transform point, Action onArrived = null)
    {
        if (point == null)
        {
            Debug.LogWarning("MoveTo called with a null target point.");
            return;
        }

        targetPoint = point;
        onArrivedCallback = onArrived;
        isMoving = true;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (playerAnimator == null || !hasSpeedParameter)
        {
            return;
        }

        float speedValue = 0f;
        if (speed > 0f)
        {
            speedValue = Mathf.Clamp(
                speed * moveSpeed / Mathf.Max(0.01f, animatorWalkReferenceSpeed),
                0.5f,
                maxAnimatorMoveSpeed);
        }

        playerAnimator.SetFloat(speedParameter, speedValue);
    }

    private bool HasAnimatorFloat(string parameterName)
    {
        if (playerAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in playerAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float &&
                parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
