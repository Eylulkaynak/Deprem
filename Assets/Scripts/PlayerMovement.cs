using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 180f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Animator")]
    public Animator animator;
    public string speedParameter = "Speed";

    private CharacterController controller;
    private float verticalVelocity;
    private bool hasSpeedParameter;

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
        MoveCharacter();
        UpdateAnimator();
    }

    private void MoveCharacter()
    {
        float forwardInput = Input.GetAxisRaw("Vertical");
        float turnInput = Input.GetAxisRaw("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (Mathf.Abs(turnInput) > 0.01f)
        {
            transform.Rotate(Vector3.up, turnInput * rotationSpeed * Time.deltaTime);
        }

        Vector3 horizontalMove = transform.forward * forwardInput * currentSpeed;
        ApplyGravity();

        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
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

        float forwardInput = Mathf.Abs(Input.GetAxisRaw("Vertical"));
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float speedValue = 0f;
        if (forwardInput > 0.01f)
        {
            speedValue = isRunning ? 2f : 1f;
        }

        animator.SetFloat(speedParameter, speedValue);
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
