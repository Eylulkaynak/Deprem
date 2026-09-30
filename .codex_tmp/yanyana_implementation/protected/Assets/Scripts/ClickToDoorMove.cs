using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ClickToDoorMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.6f;
    public bool useTransformFallbackWhenControllerStuck = true;
    public float stuckMoveEpsilon = 0.001f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Free Click Movement")]
    public bool freeClickToMove = true;
    public Camera raycastCamera;
    public LayerMask walkableLayers = ~0;
    [Range(0f, 1f)] public float minWalkableNormalY = 0.45f;

    [Header("Animator")]
    public Animator animator;
    public string speedParameter = "Speed";
    public float animatorWalkReferenceSpeed = 2f;
    public float maxAnimatorMoveSpeed = 2f;

    [Header("Events")]
    public UnityEvent onReachedDoor;

    private CharacterController controller;
    private Vector3 targetPosition;
    private float verticalVelocity;
    private bool hasTarget;
    private bool targetTriggersDoorEvent;
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
        ReadFreeClickTarget();

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
        targetTriggersDoorEvent = true;
    }

    public void StopMoving()
    {
        hasTarget = false;
        targetTriggersDoorEvent = false;
        SetAnimatorSpeed(0f);
    }

    private void SetFreeTarget(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
        targetTriggersDoorEvent = false;
    }

    private void MoveToTarget()
    {
        Vector3 currentPosition = transform.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
        Vector3 toTarget = flatTarget - currentPosition;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            bool shouldTriggerDoorEvent = targetTriggersDoorEvent;
            StopMoving();

            if (shouldTriggerDoorEvent)
            {
                onReachedDoor?.Invoke();
            }

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
            Vector3 beforeMove = transform.position;
            controller.Move(move * Time.deltaTime);
            ApplyTransformFallbackIfStuck(beforeMove);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);
    }

    private void ApplyTransformFallbackIfStuck(Vector3 beforeMove)
    {
        if (!useTransformFallbackWhenControllerStuck)
        {
            return;
        }

        Vector2 beforeFlat = new Vector2(beforeMove.x, beforeMove.z);
        Vector2 afterFlat = new Vector2(transform.position.x, transform.position.z);
        if ((afterFlat - beforeFlat).sqrMagnitude > stuckMoveEpsilon * stuckMoveEpsilon)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
        transform.position = Vector3.MoveTowards(
            currentPosition,
            flatTarget,
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

    private void ReadFreeClickTarget()
    {
        if (!freeClickToMove)
        {
            return;
        }

        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            return;
        }

        if (IsPointerOverUi(pointerId))
        {
            return;
        }

        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (raycastCamera == null)
        {
            return;
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, walkableLayers);
        if (hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsMissionSelectionHit(hit))
            {
                return;
            }
        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.normal.y < minWalkableNormalY)
            {
                continue;
            }

            SetFreeTarget(hit.point);
            return;
        }
    }

    private bool IsMissionSelectionHit(RaycastHit hit)
    {
        return hit.collider.GetComponentInParent<DoorTarget>() != null ||
            hit.collider.GetComponentInParent<StairChoiceTarget>() != null ||
            hit.collider.GetComponentInParent<SafeAreaClickTarget>() != null;
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
