using UnityEngine;

namespace Sample
{
    [RequireComponent(typeof(CharacterController))]
    public class KidsScript : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 4f;
        [SerializeField] private float rotationSpeed = 120f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;

        private Animator animator;
        private CharacterController controller;
        private Vector3 moveDirection;

        // Animator States
        private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
        private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
        private static readonly int JumpState = Animator.StringToHash("Base Layer.jump");

        // Animator Tags / Parameters
        private static readonly int JumpTag = Animator.StringToHash("Jump");
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int JumpPoseParameter = Animator.StringToHash("JumpPose");

        private void Start()
        {
            animator = GetComponent<Animator>();
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            ApplyGravity();
            Move();
            Jump();
        }

        private void Move()
        {
            float currentSpeed = Input.GetKey(KeyCode.Z) ? runSpeed : walkSpeed;

            bool isMovingForward = Input.GetKey(KeyCode.UpArrow);
            bool isTurningRight = Input.GetKey(KeyCode.RightArrow);
            bool isTurningLeft = Input.GetKey(KeyCode.LeftArrow);

            // Sağa sola dönme
            if (isTurningRight && !isTurningLeft)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            else if (isTurningLeft && !isTurningRight)
            {
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
            }

            // İleri hareket
            if (isMovingForward)
            {
                Vector3 forwardMove = transform.forward * currentSpeed;
                moveDirection.x = forwardMove.x;
                moveDirection.z = forwardMove.z;

                controller.Move(moveDirection * Time.deltaTime);

                animator.SetFloat(SpeedParameter, Input.GetKey(KeyCode.Z) ? 2f : 1f);

                if (IsGrounded() && animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag)
                {
                    animator.CrossFade(MoveState, 0.1f, 0, 0);
                }
            }
            else
            {
                moveDirection.x = 0f;
                moveDirection.z = 0f;

                animator.SetFloat(SpeedParameter, 1f);

                if (IsGrounded() && animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag)
                {
                    animator.CrossFade(IdleState, 0.1f, 0, 0);
                }
            }
        }

        private void Jump()
        {
            if (IsGrounded())
            {
                if (Input.GetKeyDown(KeyCode.S)
                    && animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag
                    && !animator.IsInTransition(0))
                {
                    moveDirection.y = jumpForce;
                    animator.SetFloat(JumpPoseParameter, moveDirection.y);
                    animator.CrossFade(JumpState, 0.1f, 0, 0);
                }
            }
            else
            {
                animator.SetFloat(JumpPoseParameter, moveDirection.y);

                if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != JumpState
                    && !animator.IsInTransition(0))
                {
                    animator.CrossFade(JumpState, 0.1f, 0, 0);
                }
            }
        }

        private void ApplyGravity()
        {
            if (IsGrounded() && moveDirection.y < 0f)
            {
                moveDirection.y = -1f;
            }

            moveDirection.y += gravity * Time.deltaTime;

            Vector3 verticalMove = new Vector3(0f, moveDirection.y, 0f);
            controller.Move(verticalMove * Time.deltaTime);
        }

        private bool IsGrounded()
        {
            if (controller.isGrounded)
            {
                return true;
            }

            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
            return Physics.Raycast(ray, 0.2f);
        }
    }
}