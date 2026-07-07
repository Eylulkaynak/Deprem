using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class EarthquakePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Safe Spot")]
    public Transform safeSpot;

    private CharacterController controller;
    private Animator playerAnimator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized;

        if (move.magnitude > 0.1f)
        {
            // Hareket
            controller.Move(move * moveSpeed * Time.deltaTime);

            // Karakteri hareket yönüne döndür
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Hareket animasyonu
            playerAnimator.SetFloat("Speed", 2f);
        }
        else
        {
            // Idle animasyonu
            playerAnimator.SetFloat("Speed", 1f);
        }
    }

    // Çök
    public void DoDrop()
    {
        if (safeSpot != null)
        {
            transform.SetPositionAndRotation(safeSpot.position, safeSpot.rotation);
        }

        playerAnimator.SetTrigger("Drop");
        Debug.Log("Drop");
    }

    // Kapan
    public void DoCover()
    {
        playerAnimator.SetTrigger("Cover");
        Debug.Log("Cover");
    }

    // Tutun
    public void DoHold()
    {
        playerAnimator.SetTrigger("Hold");
        Debug.Log("Hold");
    }
}