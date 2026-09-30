using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 180f;
    public float tapRotationSpeed = 8f;
    public float tapStoppingDistance = 0.25f;
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

    [Header("Tap To Move")]
    public bool tapToMoveEnabled = true;
    public Camera raycastCamera;
    public LayerMask walkableLayers = ~0;
    [Range(0f, 1f)] public float minWalkableNormalY = 0.45f;

    private CharacterController controller;
    private float verticalVelocity;
    private bool hasSpeedParameter;
    private Vector3 tapTargetPosition;
    private bool hasTapTarget;
    private float currentAnimatorMoveAmount;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        hasSpeedParameter = HasAnimatorFloat(speedParameter);
    }

    private void Update()
    {
        ReadTapTarget();
        MoveCharacter();
        UpdateAnimator();
    }

    private void MoveCharacter()
    {
        float forwardInput = Input.GetAxisRaw("Vertical");
        float turnInput = Input.GetAxisRaw("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        bool hasKeyboardInput = Mathf.Abs(forwardInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f;

        if (hasKeyboardInput)
        {
            hasTapTarget = false;

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                transform.Rotate(Vector3.up, turnInput * rotationSpeed * Time.deltaTime);
            }

            Vector3 keyboardMove = transform.forward * forwardInput * currentSpeed;
            ApplyGravity();
            Vector3 beforeMove = transform.position;
            controller.Move((keyboardMove + Vector3.up * verticalVelocity) * Time.deltaTime);
            ApplyTransformFallbackIfStuck(beforeMove, keyboardMove);
            currentAnimatorMoveAmount = Mathf.Abs(forwardInput) * (isRunning ? runSpeed : walkSpeed);
            return;
        }

        Vector3 horizontalMove = Vector3.zero;
        if (hasTapTarget)
        {
            Vector3 flatTarget = new Vector3(tapTargetPosition.x, transform.position.y, tapTargetPosition.z);
            Vector3 toTarget = flatTarget - transform.position;

            if (toTarget.magnitude <= tapStoppingDistance)
            {
                hasTapTarget = false;
            }
            else
            {
                Vector3 moveDirection = toTarget.normalized;
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    tapRotationSpeed * Time.deltaTime);
                horizontalMove = moveDirection * walkSpeed;
            }
        }

        ApplyGravity();

        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;
        Vector3 beforeTapMove = transform.position;
        controller.Move(move * Time.deltaTime);
        ApplyTransformFallbackIfStuck(beforeTapMove, horizontalMove);
        currentAnimatorMoveAmount = horizontalMove.magnitude;
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
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void UpdateAnimator()
    {
        if (animator == null || !hasSpeedParameter)
        {
            return;
        }

        float speedValue = 0f;
        if (currentAnimatorMoveAmount > 0.01f)
        {
            speedValue = Mathf.Clamp(
                currentAnimatorMoveAmount / Mathf.Max(0.01f, animatorWalkReferenceSpeed),
                0.5f,
                maxAnimatorMoveSpeed);
        }

        animator.SetFloat(speedParameter, speedValue);
    }

    private void ReadTapTarget()
    {
        if (!tapToMoveEnabled)
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
            if (hit.normal.y < minWalkableNormalY)
            {
                continue;
            }

            tapTargetPosition = hit.point;
            hasTapTarget = true;
            return;
        }
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
