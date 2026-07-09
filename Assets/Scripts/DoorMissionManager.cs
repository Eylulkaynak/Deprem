using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DoorMissionManager : MonoBehaviour
{
    [Header("Player")]
    public ClickToDoorMove clickToDoorMove;
    public MonoBehaviour wasdMovementScript;

    [Header("Door")]
    public DoorTarget doorTarget;
    public Camera raycastCamera;

    [Header("Mission UI")]
    public GameObject goToDoorIndicator;
    public TMP_Text missionText;
    public string missionMessage = "Kap\u0131ya ilerle";

    [Header("Fade")]
    public GameObject fadePanel;
    public Image fadeImage;
    public float fadeDuration = 0.75f;
    public float waitOnBlackScreen = 0.25f;

    [Header("Area Transition")]
    public GameObject nextAreaRoot;
    public bool hideNextAreaOnStart = true;
    public GameObject previousAreaRoot;
    public bool hidePreviousAreaOnTransition = true;
    public Transform nextAreaSpawnPoint;
    public Transform playerTransform;
    public CharacterController playerCharacterController;
    public Transform cameraTransform;
    public Transform nextCameraPoint;
    public StairChoiceManager stairChoiceManager;

    [Header("Legacy Transition")]
    public GameObject transitionPanel;
    public bool loadSceneOnReachedDoor;
    public string nextSceneName = "StairMission";

    private bool missionActive;
    private bool doorReached;
    private bool transitionRunning;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerTransform == null && clickToDoorMove != null)
        {
            playerTransform = clickToDoorMove.transform;
        }

        if (playerCharacterController == null && playerTransform != null)
        {
            playerCharacterController = playerTransform.GetComponent<CharacterController>();
        }

        if (fadeImage == null && fadePanel != null)
        {
            fadeImage = fadePanel.GetComponentInChildren<Image>(true);
        }

        if (clickToDoorMove != null)
        {
            clickToDoorMove.enabled = false;
            clickToDoorMove.onReachedDoor.AddListener(HandleReachedDoor);
        }

        if (hideNextAreaOnStart && nextAreaRoot != null)
        {
            nextAreaRoot.SetActive(false);
        }

        SetMissionVisuals(false);
        SetTransitionPanel(false);
        SetFadeAlpha(0f, false);
    }

    private void OnDestroy()
    {
        if (clickToDoorMove != null)
        {
            clickToDoorMove.onReachedDoor.RemoveListener(HandleReachedDoor);
        }
    }

    private void Update()
    {
        if (!missionActive || doorReached)
        {
            return;
        }

        if (TryGetClickOrTouchPosition(out Vector2 screenPosition))
        {
            TrySelectDoor(screenPosition);
        }
    }

    public void StartDoorMission()
    {
        missionActive = true;
        doorReached = false;
        transitionRunning = false;

        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (missionText != null)
        {
            missionText.text = missionMessage;
        }

        SetMissionVisuals(true);
        SetTransitionPanel(false);
        SetFadeAlpha(0f, false);

        if (wasdMovementScript != null)
        {
            wasdMovementScript.enabled = false;
        }

        if (clickToDoorMove != null)
        {
            clickToDoorMove.StopMoving();
            clickToDoorMove.enabled = true;
        }
    }

    public void MovePlayerToDoor(DoorTarget target)
    {
        if (!missionActive || doorReached || target == null || clickToDoorMove == null)
        {
            return;
        }

        clickToDoorMove.SetDoorTarget(target.GetTargetPosition());
    }

    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName) && nextSceneName.Trim().Length > 0)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public IEnumerator FadeAndRevealNextArea()
    {
        transitionRunning = true;
        SetFadeAlpha(0f, true);

        yield return FadeTo(1f);

        if (waitOnBlackScreen > 0f)
        {
            yield return new WaitForSeconds(waitOnBlackScreen);
        }

        if (nextAreaRoot != null)
        {
            nextAreaRoot.SetActive(true);
        }

        if (hidePreviousAreaOnTransition && previousAreaRoot != null)
        {
            HidePreviousAreaSafely();
        }

        MovePlayerToNextAreaSpawn();
        MoveCameraToNextAreaPoint();

        yield return FadeTo(0f);
        SetFadeAlpha(0f, false);

        if (stairChoiceManager != null)
        {
            stairChoiceManager.StartMission();
        }

        transitionRunning = false;
    }

    private void HandleReachedDoor()
    {
        if (doorReached)
        {
            return;
        }

        doorReached = true;
        missionActive = false;
        SetMissionVisuals(false);

        if (clickToDoorMove != null)
        {
            clickToDoorMove.StopMoving();
            clickToDoorMove.enabled = false;
        }

        if (loadSceneOnReachedDoor)
        {
            LoadNextScene();
            return;
        }

        if (!transitionRunning)
        {
            StartCoroutine(FadeAndRevealNextArea());
        }
    }

    private void HidePreviousAreaSafely()
    {
        if (previousAreaRoot == null)
        {
            return;
        }

        if (nextAreaRoot == previousAreaRoot)
        {
            Debug.LogWarning("DoorMissionManager: Previous Area Root and Next Area Root are the same object. Previous area was not hidden.");
            return;
        }

        ProtectTransformFromPreviousArea(playerTransform, "Player Transform");
        ProtectTransformFromPreviousArea(cameraTransform, "Camera Transform");
        ProtectTransformFromPreviousArea(transform, "DoorMissionManager");

        if (fadePanel != null)
        {
            Canvas fadeCanvas = fadePanel.GetComponentInParent<Canvas>();
            ProtectTransformFromPreviousArea(fadeCanvas != null ? fadeCanvas.transform : fadePanel.transform, "Fade Canvas/Panel");
        }

        if (stairChoiceManager != null)
        {
            ProtectTransformFromPreviousArea(stairChoiceManager.transform, "StairChoiceManager");
        }

        previousAreaRoot.SetActive(false);
    }

    private void ProtectTransformFromPreviousArea(Transform protectedTransform, string label)
    {
        if (protectedTransform == null || previousAreaRoot == null)
        {
            return;
        }

        if (!protectedTransform.IsChildOf(previousAreaRoot.transform))
        {
            return;
        }

        Debug.LogWarning($"DoorMissionManager: {label} is under Previous Area Root. It was detached before hiding the previous area.");
        protectedTransform.SetParent(null, true);
    }

    private void MovePlayerToNextAreaSpawn()
    {
        if (playerTransform == null || nextAreaSpawnPoint == null)
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

        playerTransform.position = nextAreaSpawnPoint.position;
        playerTransform.rotation = nextAreaSpawnPoint.rotation;

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = controllerWasEnabled;
        }
    }

    private void MoveCameraToNextAreaPoint()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("DoorMissionManager: Camera Transform is not assigned and Camera.main was not found.");
            return;
        }

        if (nextCameraPoint == null)
        {
            Debug.LogWarning("DoorMissionManager: Next Camera Point is not assigned. Camera was not moved.");
            return;
        }

        cameraTransform.position = nextCameraPoint.position;
        cameraTransform.rotation = nextCameraPoint.rotation;
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

    private void TrySelectDoor(Vector2 screenPosition)
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

        DoorTarget target = hit.collider.GetComponentInParent<DoorTarget>();
        if (target != null)
        {
            MovePlayerToDoor(target);
        }
    }

    private void SetMissionVisuals(bool isVisible)
    {
        if (goToDoorIndicator != null)
        {
            goToDoorIndicator.SetActive(isVisible);
        }

        if (missionText != null)
        {
            missionText.gameObject.SetActive(isVisible);
        }
    }

    private void SetTransitionPanel(bool isVisible)
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(isVisible);
        }
    }
}
