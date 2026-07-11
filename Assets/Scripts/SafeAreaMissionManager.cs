using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SafeAreaMissionManager : MonoBehaviour
{
    [Header("Fade")]
    public GameObject fadePanel;
    public Image fadeImage;
    public float fadeDuration = 0.75f;
    public float waitOnBlackScreen = 0.25f;

    [Header("Areas")]
    public GameObject safeAreaRoot;
    public bool hideSafeAreaOnStart = true;
    public GameObject stairAreaRoot;
    public bool hideStairAreaOnTransition = true;

    [Header("Player")]
    public Transform playerTransform;
    public CharacterController playerCharacterController;
    public Transform safeAreaSpawnPoint;
    public bool useSpawnRotation = true;
    public float extraYRotationOffset;
    public Transform safeAreaTarget;
    public SafeAreaClickTarget safeAreaTargetClickObject;
    public Animator animator;

    [Header("UI")]
    public StairInfoBubbleUI infoBubbleUI;
    public GameObject finalSuccessPanel;
    public FinalSuccessUI finalSuccessUI;
    public TMP_Text finalTitleText;
    public TMP_Text finalDescriptionText;

    [Header("Camera")]
    public Camera raycastCamera;
    public IsometricCameraFollow isometricCameraFollow;
    public bool useSafeAreaCameraSettings = true;
    public float safeAreaCameraDistance = 6f;
    public float safeAreaCameraHeight = 6f;
    public float safeAreaCameraYawAngle = 45f;
    public Vector3 safeAreaCameraFocusOffset = new Vector3(0f, 1f, 0f);

    [Header("Movement")]
    public bool autoWalkToSafeArea;
    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.5f;
    public float gravity = -20f;
    public float groundedGravity = -2f;

    private Coroutine missionRoutine;
    private Coroutine moveRoutine;
    private float verticalVelocity;
    private bool hasSpeedParameter;
    private bool missionActive;
    private bool targetReached;

    private void Awake()
    {
        ResolveReferences();

        if (hideSafeAreaOnStart && safeAreaRoot != null)
        {
            safeAreaRoot.SetActive(false);
        }

        SetFadeAlpha(0f, false);
        HideFinalPanel();
        SetAnimatorSpeed(0f);
        ValidateClickTarget();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!missionActive || targetReached || autoWalkToSafeArea)
        {
            return;
        }

        if (TryGetClickOrTouchPosition(out Vector2 screenPosition))
        {
            TrySelectSafeAreaTarget(screenPosition);
        }
    }

    public void StartSafeAreaTransition()
    {
        Debug.Log("SafeAreaMissionManager: StartSafeAreaTransition called.");

        if (missionRoutine != null)
        {
            StopCoroutine(missionRoutine);
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        missionRoutine = StartCoroutine(SafeAreaTransitionRoutine());
    }

    public void OnSafeAreaTargetClicked(SafeAreaClickTarget clickedTarget)
    {
        Debug.Log("SafeAreaMissionManager: Safe area target click received.");

        if (!missionActive || targetReached)
        {
            Debug.LogWarning("SafeAreaMissionManager: Target click ignored because mission is not active or target was already reached.");
            return;
        }

        if (safeAreaTargetClickObject != null && clickedTarget != safeAreaTargetClickObject)
        {
            Debug.LogWarning("SafeAreaMissionManager: Clicked target is not the assigned Safe Area Target Click Object.");
            return;
        }

        StartWalkingToSafeAreaTarget();
    }

    public void StartWalkingToSafeAreaTarget()
    {
        if (targetReached)
        {
            return;
        }

        if (safeAreaTarget == null)
        {
            Debug.LogWarning("SafeAreaMissionManager: Safe Area Target is not assigned.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("SafeAreaMissionManager: Player Transform is not assigned.");
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        Debug.Log("Safe area movement started");
        moveRoutine = StartCoroutine(MoveToSafeAreaTargetRoutine());
    }

    private IEnumerator SafeAreaTransitionRoutine()
    {
        ResolveReferences();
        missionActive = false;
        targetReached = false;
        HideFinalPanel();
        SetFadeAlpha(0f, true);

        yield return FadeTo(1f);

        if (waitOnBlackScreen > 0f)
        {
            yield return new WaitForSeconds(waitOnBlackScreen);
        }

        if (safeAreaRoot != null)
        {
            safeAreaRoot.SetActive(true);
        }

        if (hideStairAreaOnTransition && stairAreaRoot != null && stairAreaRoot != safeAreaRoot)
        {
            stairAreaRoot.SetActive(false);
        }

        TeleportPlayerToSafeAreaSpawn();
        SnapCameraToPlayer();

        yield return FadeTo(0f);
        SetFadeAlpha(0f, false);

        missionActive = true;
        ShowInfoMessage("A\u00e7\u0131k ve g\u00fcvenli alana ilerle.");
        ValidateClickTarget();

        if (autoWalkToSafeArea)
        {
            StartWalkingToSafeAreaTarget();
        }

        missionRoutine = null;
    }

    private IEnumerator MoveToSafeAreaTargetRoutine()
    {
        if (playerTransform == null || safeAreaTarget == null)
        {
            SetAnimatorSpeed(0f);
            yield break;
        }

        SetAnimatorSpeed(1f);

        while (true)
        {
            Vector3 currentPosition = playerTransform.position;
            Vector3 targetPosition = safeAreaTarget.position;
            Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
            Vector3 toTarget = flatTarget - currentPosition;

            if (toTarget.magnitude <= stoppingDistance)
            {
                break;
            }

            Vector3 direction = toTarget.normalized;
            RotatePlayerTowards(direction);
            MovePlayer(direction);
            SetAnimatorSpeed(1f);
            yield return null;
        }

        targetReached = true;
        missionActive = false;
        SetAnimatorSpeed(0f);
        Debug.Log("SafeAreaMissionManager: Safe area target reached.");
        ShowInfoMessage("Harika! G\u00fcvenli toplanma alan\u0131na ula\u015ft\u0131n.");
        ShowFinalPanel();
        moveRoutine = null;
    }

    private bool TryGetClickOrTouchPosition(out Vector2 screenPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        screenPosition = Vector2.zero;
        return false;
    }

    private void TrySelectSafeAreaTarget(Vector2 screenPosition)
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (raycastCamera == null)
        {
            return;
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        SafeAreaClickTarget clickTarget = hit.collider.GetComponentInParent<SafeAreaClickTarget>();
        if (clickTarget != null)
        {
            OnSafeAreaTargetClicked(clickTarget);
        }
    }

    private void TeleportPlayerToSafeAreaSpawn()
    {
        if (playerTransform == null || safeAreaSpawnPoint == null)
        {
            return;
        }

        if (playerCharacterController == null)
        {
            playerCharacterController = playerTransform.GetComponent<CharacterController>();
        }

        bool controllerWasEnabled = playerCharacterController != null && playerCharacterController.enabled;

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = false;
        }

        playerTransform.position = safeAreaSpawnPoint.position;

        if (useSpawnRotation)
        {
            playerTransform.rotation = safeAreaSpawnPoint.rotation *
                Quaternion.Euler(0f, extraYRotationOffset, 0f);
        }

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = controllerWasEnabled;
        }

        verticalVelocity = 0f;
        Debug.Log($"SafeAreaMissionManager: Player teleported to {playerTransform.position}, rotation {playerTransform.eulerAngles}.");
    }

    private void SnapCameraToPlayer()
    {
        if (isometricCameraFollow == null && Camera.main != null)
        {
            isometricCameraFollow = Camera.main.GetComponent<IsometricCameraFollow>();
        }

        if (isometricCameraFollow == null)
        {
            Debug.LogWarning("SafeAreaMissionManager: IsometricCameraFollow is not assigned and was not found on Camera.main.");
            return;
        }

        if (useSafeAreaCameraSettings)
        {
            isometricCameraFollow.SetCameraSettings(
                safeAreaCameraDistance,
                safeAreaCameraHeight,
                safeAreaCameraYawAngle,
                safeAreaCameraFocusOffset);
        }

        isometricCameraFollow.SnapToTarget();
    }

    private void RotatePlayerTowards(Vector3 direction)
    {
        if (playerTransform == null || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        playerTransform.rotation = Quaternion.Slerp(
            playerTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void MovePlayer(Vector3 direction)
    {
        Vector3 horizontalMove = direction * moveSpeed;
        ApplyGravity();
        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;

        if (playerCharacterController != null && playerCharacterController.enabled)
        {
            playerCharacterController.Move(move * Time.deltaTime);
            return;
        }

        if (playerTransform != null)
        {
            playerTransform.position += horizontalMove * Time.deltaTime;
        }
    }

    private void ApplyGravity()
    {
        if (playerCharacterController == null || !playerCharacterController.enabled)
        {
            verticalVelocity = 0f;
            return;
        }

        if (playerCharacterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            SetFadeAlpha(alpha, true);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetFadeAlpha(targetAlpha, true);
    }

    private void SetFadeAlpha(float alpha, bool isVisible)
    {
        if (fadePanel != null)
        {
            fadePanel.SetActive(isVisible || alpha > 0f);
        }

        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void ShowInfoMessage(string message)
    {
        if (infoBubbleUI != null)
        {
            infoBubbleUI.ShowMessage(message);
        }
    }

    private void ShowFinalPanel()
    {
        if (finalSuccessUI != null)
        {
            SetLegacyFinalTextsActive(false);
            finalSuccessUI.ShowFinalPanel();
            return;
        }

        if (finalSuccessPanel != null)
        {
            finalSuccessPanel.SetActive(true);
        }

        if (finalTitleText != null)
        {
            finalTitleText.text = "G\u00f6rev Tamamland\u0131!";
        }

        if (finalDescriptionText != null)
        {
            finalDescriptionText.text = "Deprem sonras\u0131 do\u011fru ad\u0131mlar\u0131 \u00f6\u011frendin.";
        }
    }

    private void HideFinalPanel()
    {
        SetLegacyFinalTextsActive(false);

        if (finalSuccessUI != null)
        {
            finalSuccessUI.Hide();
            return;
        }

        if (finalSuccessPanel != null)
        {
            finalSuccessPanel.SetActive(false);
        }
    }

    private void SetLegacyFinalTextsActive(bool isActive)
    {
        if (finalTitleText != null)
        {
            finalTitleText.gameObject.SetActive(isActive);
        }

        if (finalDescriptionText != null)
        {
            finalDescriptionText.gameObject.SetActive(isActive);
        }
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator != null && hasSpeedParameter)
        {
            animator.SetFloat("Speed", speed);
        }
    }

    private bool HasAnimatorSpeedParameter()
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float &&
                parameter.name == "Speed")
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateClickTarget()
    {
        if (safeAreaTargetClickObject == null)
        {
            Debug.LogWarning("SafeAreaMissionManager: Safe Area Target Click Object is not assigned.");
            return;
        }

        if (safeAreaTargetClickObject.GetComponent<Collider>() == null)
        {
            Debug.LogWarning("SafeAreaMissionManager: Safe Area Target Click Object has no Collider.");
        }
    }

    private void ResolveReferences()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.Find("Boy0");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerCharacterController == null && playerTransform != null)
        {
            playerCharacterController = playerTransform.GetComponent<CharacterController>();
        }

        if (animator == null && playerTransform != null)
        {
            animator = playerTransform.GetComponentInChildren<Animator>();
        }

        if (fadeImage == null && fadePanel != null)
        {
            fadeImage = fadePanel.GetComponentInChildren<Image>(true);
        }

        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (isometricCameraFollow == null && Camera.main != null)
        {
            isometricCameraFollow = Camera.main.GetComponent<IsometricCameraFollow>();
        }

        if (safeAreaTargetClickObject == null && safeAreaTarget != null)
        {
            safeAreaTargetClickObject = safeAreaTarget.GetComponent<SafeAreaClickTarget>();
        }

        if (finalSuccessUI == null && finalSuccessPanel != null)
        {
            finalSuccessUI = finalSuccessPanel.GetComponent<FinalSuccessUI>();
        }

        hasSpeedParameter = HasAnimatorSpeedParameter();
    }
}
