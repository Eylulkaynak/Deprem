using System.Collections;
using TMPro;
using UnityEngine;

public class EarthquakeEventManager : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeEarthquake = 2f;
    public float earthquakeDuration = 3f;

    [Header("Camera Shake")]
    public Camera targetCamera;
    public float shakeAmount = 0.08f;

    [Header("UI")]
    public TMP_Text warningText;
    public string warningMessage = "Deprem oluyor! Sakin ol.";

    [Header("Card Game")]
    public CardGameManager cardGameManager;

    private Coroutine earthquakeRoutine;
    private Vector3 originalCameraLocalPosition;
    private IsometricCameraFollow cameraFollow;
    private bool cameraFollowWasEnabled;
    private bool cameraFollowPaused;
    private bool hasOriginalCameraLocalPosition;

    private void Start()
    {
        ResolveCamera();

        SetWarningVisible(false);
        earthquakeRoutine = StartCoroutine(EarthquakeSequence());
    }

    public void StartEarthquakeNow()
    {
        if (earthquakeRoutine != null)
        {
            StopCoroutine(earthquakeRoutine);
            ResetCameraPosition();
            RestoreCameraFollow();
        }

        earthquakeRoutine = StartCoroutine(RunEarthquake());
    }

    private void OnDisable()
    {
        if (earthquakeRoutine != null)
        {
            StopCoroutine(earthquakeRoutine);
            earthquakeRoutine = null;
        }

        ResetCameraPosition();
        RestoreCameraFollow();
    }

    private IEnumerator EarthquakeSequence()
    {
        if (delayBeforeEarthquake > 0f)
        {
            yield return new WaitForSeconds(delayBeforeEarthquake);
        }

        yield return RunEarthquake();
    }

    private IEnumerator RunEarthquake()
    {
        ResolveCamera();

        if (targetCamera != null)
        {
            originalCameraLocalPosition = targetCamera.transform.localPosition;
            hasOriginalCameraLocalPosition = true;
        }

        PauseCameraFollow();
        ShowWarning();

        float elapsedTime = 0f;
        while (elapsedTime < earthquakeDuration)
        {
            ShakeCamera();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ResetCameraPosition();
        RestoreCameraFollow();
        SetWarningVisible(false);
        ShowCardGame();
        earthquakeRoutine = null;
    }

    private void ShakeCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 shakeOffset = new Vector3(
            Random.Range(-shakeAmount, shakeAmount),
            Random.Range(-shakeAmount, shakeAmount),
            0f);

        targetCamera.transform.localPosition = originalCameraLocalPosition + shakeOffset;
    }

    private void ResetCameraPosition()
    {
        if (targetCamera != null && hasOriginalCameraLocalPosition)
        {
            targetCamera.transform.localPosition = originalCameraLocalPosition;
        }
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        cameraFollow = targetCamera != null ? targetCamera.GetComponent<IsometricCameraFollow>() : null;
    }

    private void PauseCameraFollow()
    {
        if (cameraFollow == null)
        {
            return;
        }

        cameraFollowWasEnabled = cameraFollow.enabled;
        cameraFollow.enabled = false;
        cameraFollowPaused = true;
    }

    private void RestoreCameraFollow()
    {
        if (cameraFollow == null || !cameraFollowPaused)
        {
            return;
        }

        cameraFollow.enabled = cameraFollowWasEnabled;
        cameraFollowPaused = false;

        if (cameraFollow.enabled)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private void ShowWarning()
    {
        if (warningText == null)
        {
            return;
        }

        warningText.text = warningMessage;
        SetWarningVisible(true);
    }

    private void SetWarningVisible(bool isVisible)
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(isVisible);
        }
    }

    private void ShowCardGame()
    {
        if (cardGameManager == null)
        {
            Debug.LogWarning("EarthquakeEventManager: CardGameManager reference is not assigned.");
            return;
        }

        cardGameManager.ShowCardGame();
    }
}
