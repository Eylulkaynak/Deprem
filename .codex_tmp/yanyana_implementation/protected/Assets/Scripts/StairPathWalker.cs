using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StairPathWalker : MonoBehaviour
{
    [Header("Path")]
    public Transform[] pathPoints;
    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.35f;
    public bool useTransformFallbackWhenControllerStuck = true;
    public float stuckMoveEpsilon = 0.001f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Animator")]
    public Animator animator;
    public string speedParameter = "Speed";
    public float animatorWalkReferenceSpeed = 2f;
    public float maxAnimatorMoveSpeed = 2f;

    [Header("Events")]
    public UnityEvent onPathCompleted;

    private CharacterController characterController;
    private Coroutine walkRoutine;
    private float verticalVelocity;
    private bool hasSpeedParameter;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

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

    public void StartWalkingPath()
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.LogWarning("StairPathWalker: No path points assigned.");
            SetAnimatorSpeed(0f);
            return;
        }

        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
        }

        walkRoutine = StartCoroutine(WalkPathRoutine());
    }

    public void StopWalking()
    {
        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }

        SetAnimatorSpeed(0f);
    }

    private IEnumerator WalkPathRoutine()
    {
        SetAnimatorSpeed(1f);

        for (int i = 0; i < pathPoints.Length; i++)
        {
            Transform point = pathPoints[i];
            if (point == null)
            {
                continue;
            }

            yield return WalkToPoint(point);
        }

        SetAnimatorSpeed(0f);
        walkRoutine = null;
        onPathCompleted?.Invoke();
    }

    private IEnumerator WalkToPoint(Transform point)
    {
        while (point != null)
        {
            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = point.position;
            Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
            Vector3 toTarget = flatTarget - currentPosition;

            if (toTarget.magnitude <= stoppingDistance)
            {
                yield break;
            }

            Vector3 moveDirection = toTarget.normalized;
            RotateTowards(moveDirection);
            MoveCharacter(moveDirection);
            SetAnimatorSpeed(1f);
            yield return null;
        }
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

        if (characterController != null)
        {
            Vector3 beforeMove = transform.position;
            characterController.Move(move * Time.deltaTime);
            ApplyTransformFallbackIfStuck(beforeMove, horizontalMove);
            return;
        }

        transform.position += horizontalMove * Time.deltaTime;
    }

    private void ApplyTransformFallbackIfStuck(Vector3 beforeMove, Vector3 horizontalMove)
    {
        if (!useTransformFallbackWhenControllerStuck || horizontalMove.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 beforeFlat = new Vector2(beforeMove.x, beforeMove.z);
        Vector2 afterFlat = new Vector2(transform.position.x, transform.position.z);
        if ((afterFlat - beforeFlat).sqrMagnitude > stuckMoveEpsilon * stuckMoveEpsilon)
        {
            return;
        }

        transform.position += horizontalMove * Time.deltaTime;
    }

    private void ApplyGravity()
    {
        if (characterController == null)
        {
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator != null && hasSpeedParameter)
        {
            float speedValue = 0f;
            if (speed > 0f)
            {
                speedValue = Mathf.Clamp(
                    speed * moveSpeed / Mathf.Max(0.01f, animatorWalkReferenceSpeed),
                    0.5f,
                    maxAnimatorMoveSpeed);
            }

            animator.SetFloat(speedParameter, speedValue);
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
