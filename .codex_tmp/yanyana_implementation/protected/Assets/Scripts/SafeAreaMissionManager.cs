using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public string speedParameter = "Speed";
    public float animatorWalkReferenceSpeed = 2f;
    public float maxAnimatorMoveSpeed = 2f;
    public bool freeClickToMove = true;
    public LayerMask walkableLayers = ~0;
    [Range(0f, 1f)] public float minWalkableNormalY = 0.45f;
    public bool useTransformFallbackWhenControllerStuck = true;
    public float stuckMoveEpsilon = 0.001f;

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

        if (TryGetClickOrTouchPosition(out Vector2 screenPosition, out int pointerId) &&
            !IsPointerOverUi(pointerId))
        {
            if (TrySelectSafeAreaTarget(screenPosition))
            {
                return;
            }

            if (freeClickToMove)
            {
                TrySelectFreeMoveTarget(screenPosition);
            }
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
            moveRoutine = null;
        }

        Debug.Log("Safe area movement started");
        missionActive = false;
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
        ShowInfoMessage("Acik ve guvenli alana ilerle.");
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
        ShowInfoMessage("Harika! Guvenli toplanma alanina ulastin.");
        ShowFinalPanel();
        moveRoutine = null;
    }

    private void StartWalkingToFreeTarget(Vector3 worldPosition)
    {
        if (targetReached || playerTransform == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        moveRoutine = StartCoroutine(MoveToFreeTargetRoutine(worldPosition));
    }

    private IEnumerator MoveToFreeTargetRoutine(Vector3 worldPosition)
    {
        if (playerTransform == null)
        {
            SetAnimatorSpeed(0f);
            yield break;
        }

        SetAnimatorSpeed(1f);

        while (true)
        {
            Vector3 currentPosition = playerTransform.position;
            Vector3 flatTarget = new Vector3(worldPosition.x, currentPosition.y, worldPosition.z);
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

        SetAnimatorSpeed(0f);
        moveRoutine = null;
    }

    private bool TryGetClickOrTouchPosition(out Vector2 screenPosition, out int pointerId)
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

    private bool TrySelectSafeAreaTarget(Vector2 screenPosition)
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (raycastCamera == null)
        {
            return false;
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        if (hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            SafeAreaClickTarget clickTarget = hit.collider.GetComponentInParent<SafeAreaClickTarget>();
            if (clickTarget != null && clickTarget.isActiveAndEnabled)
            {
                OnSafeAreaTargetClicked(clickTarget);
                return true;
            }
        }

        return false;
    }

    private void TrySelectFreeMoveTarget(Vector2 screenPosition)
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
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, walkableLayers);
        if (hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<SafeAreaClickTarget>() != null)
            {
                return;
            }

            if (hit.normal.y < minWalkableNormalY)
            {
                continue;
            }

            StartWalkingToFreeTarget(hit.point);
            return;
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
            Vector3 beforeMove = playerTransform.position;
            playerCharacterController.Move(move * Time.deltaTime);
            ApplyTransformFallbackIfStuck(beforeMove, horizontalMove);
            return;
        }

        if (playerTransform != null)
        {
            playerTransform.position += horizontalMove * Time.deltaTime;
        }
    }

    private void ApplyTransformFallbackIfStuck(Vector3 beforeMove, Vector3 horizontalMove)
    {
        if (!useTransformFallbackWhenControllerStuck || playerTransform == null || horizontalMove.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 beforeFlat = new Vector2(beforeMove.x, beforeMove.z);
        Vector2 afterFlat = new Vector2(playerTransform.position.x, playerTransform.position.z);
        if ((afterFlat - beforeFlat).sqrMagnitude > stuckMoveEpsilon * stuckMoveEpsilon)
        {
            return;
        }

        playerTransform.position += horizontalMove * Time.deltaTime;
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
            finalTitleText.text = "Gorev Tamamlandi!";
        }

        if (finalDescriptionText != null)
        {
            finalDescriptionText.text = "Deprem sonrasi dogru adimlari ogrendin.";
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

    private bool HasAnimatorSpeedParameter()
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParameter))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float &&
                parameter.name == speedParameter)
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
