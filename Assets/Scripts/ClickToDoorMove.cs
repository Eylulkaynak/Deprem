using UnityEngine;
using UnityEngine.Events;

public class ClickToDoorMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.6f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Animator")]
    public Animator animator;
    public string speedParameter = "Speed";

    [Header("Events")]
    public UnityEvent onReachedDoor;

    private CharacterController controller;
    private Vector3 targetPosition;
    private float verticalVelocity;
    private bool hasTarget;
    private bool hasSpeedParameter;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        hasSpeedParameter = HasAnimatorFloat(speedParameter);
        SetAnimatorSpeed(0f);
    }

    private void OnDisable()
    {
        SetAnimatorSpeed(0f);
    }

    private void Update()
    {
        if (!hasTarget)
        {
            ApplyIdleGravity();
            SetAnimatorSpeed(0f);
            return;
        }

        MoveToTarget();
    }

    public void SetDoorTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        SetDoorTarget(target.position);
    }

    public void SetDoorTarget(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }

    public void StopMoving()
    {
        hasTarget = false;
        SetAnimatorSpeed(0f);
    }

    private void MoveToTarget()
    {
        Vector3 currentPosition = transform.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
        Vector3 toTarget = flatTarget - currentPosition;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            StopMoving();
            onReachedDoor?.Invoke();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        RotateTowards(moveDirection);
        MoveCharacter(moveDirection);
        SetAnimatorSpeed(1f);
    }

    private void RotateTowards(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void MoveCharacter(Vector3 moveDirection)
    {
        Vector3 horizontalMove = moveDirection * moveSpeed;
        ApplyGravity();

        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;

        if (controller != null)
        {
            controller.Move(move * Time.deltaTime);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);
    }

    private void ApplyIdleGravity()
    {
        if (controller == null)
        {
            return;
        }

        ApplyGravity();
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller == null)
        {
            return;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator != null && hasSpeedParameter)
        {
            animator.SetFloat(speedParameter, speed);
        }
    }

    private bool HasAnimatorFloat(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
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
