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

    private CharacterController controller;
    private Animator playerAnimator;

    private Transform targetPoint;
    private Action onArrivedCallback;
    private bool isMoving = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
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
            playerAnimator.SetFloat("Speed", 0f);

            onArrivedCallback?.Invoke();
            onArrivedCallback = null;
            return;
        }

        direction.Normalize();
        controller.Move(direction * moveSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        playerAnimator.SetFloat("Speed", 1f);
    }

    public void MoveTo(Transform point, Action onArrived = null)
    {
        if (point == null)
        {
            Debug.LogWarning("MoveTo çağrıldı ama targetPoint null!");
            return;
        }

        targetPoint = point;
        onArrivedCallback = onArrived;
        isMoving = true;
    }
}