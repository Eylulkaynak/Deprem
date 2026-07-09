using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StairChoiceManager : MonoBehaviour
{
    [Header("Selection")]
    public Camera raycastCamera;
    public bool choicesEnabled = true;
    public StairChoiceTarget[] choiceTargets;
    public GameObject choicesRoot;

    [Header("Feedback")]
    public TMP_Text feedbackText;
    public StairInfoMessageUI infoMessageUI;
    public float messageVisibleDuration = 3f;
    public string introMessage = "Deprem sonras\u0131 merdivenleri kullan\u0131rken sakin ol. En g\u00fcvenli yolu se\u00e7.";
    public string elevatorMessage = "Asans\u00f6r deprem sonras\u0131 tehlikelidir. Kullanma!";
    public string middleStairsMessage = "Merdivenin ortas\u0131ndan inmek g\u00fcvenli de\u011fil. Duvar kenar\u0131ndan ilerle.";
    public string wallSideMessage = "Do\u011fru se\u00e7im! Duvar dibinden dikkatlice in.";

    [Header("Shake")]
    public Transform shakeTarget;
    public float shakeDuration = 0.25f;
    public float shakeAmount = 0.08f;

    [Header("Correct Choice")]
    public bool hideChoicesOnCorrect = true;
    public StairPathWalker stairPathWalker;
    public UnityEvent onCorrectChoice;

    private Coroutine shakeRoutine;
    private Vector3 originalShakePosition;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (shakeTarget == null && raycastCamera != null)
        {
            shakeTarget = raycastCamera.transform;
        }

        CacheChoiceTargetsIfNeeded();
        ValidateChoiceColliders();
    }

    private void Update()
    {
        if (!isActiveAndEnabled || !choicesEnabled)
        {
            return;
        }

        if (TryGetClickOrTouchPosition(out Vector2 screenPosition))
        {
            TrySelectChoice(screenPosition);
        }
    }

    public void StartMission()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (shakeTarget == null && raycastCamera != null)
        {
            shakeTarget = raycastCamera.transform;
        }

        SetFeedback(string.Empty);
        ShowInfoMessage(introMessage);
        EnableChoices();
        ValidateChoiceColliders();
    }

    public void EnableChoices()
    {
        choicesEnabled = true;
        SetChoicesVisible(true);
    }

    public void DisableChoices()
    {
        choicesEnabled = false;
        SetChoicesVisible(false);
    }

    public void HandleChoice(StairChoiceTarget target)
    {
        if (target == null || !choicesEnabled)
        {
            return;
        }

        Debug.Log($"Stair choice selected: {target.choice}");

        switch (target.choice)
        {
            case StairChoiceTarget.StairChoice.Elevator:
                SetFeedback(elevatorMessage);
                ShowInfoMessage(elevatorMessage);
                PlayShake();
                break;

            case StairChoiceTarget.StairChoice.MiddleStairs:
                SetFeedback(middleStairsMessage);
                ShowInfoMessage(middleStairsMessage);
                PlayShake();
                break;

            case StairChoiceTarget.StairChoice.WallSide:
                HandleCorrectChoice();
                break;
        }
    }

    private void HandleCorrectChoice()
    {
        SetFeedback(wallSideMessage);
        ShowInfoMessage(wallSideMessage);

        if (hideChoicesOnCorrect)
        {
            SetChoicesVisible(false);
        }

        choicesEnabled = false;

        if (stairPathWalker != null)
        {
            stairPathWalker.StartWalkingPath();
        }

        onCorrectChoice?.Invoke();
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

    private void TrySelectChoice(Vector2 screenPosition)
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

        StairChoiceTarget target = hit.collider.GetComponentInParent<StairChoiceTarget>();
        if (target != null)
        {
            HandleChoice(target);
        }
    }

    private void PlayShake()
    {
        if (shakeTarget == null)
        {
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeTarget.localPosition = originalShakePosition;
        }

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        originalShakePosition = shakeTarget.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            Vector3 offset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f);

            shakeTarget.localPosition = originalShakePosition + offset;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalShakePosition;
        shakeRoutine = null;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void ShowInfoMessage(string message)
    {
        if (infoMessageUI != null)
        {
            if (!infoMessageUI.gameObject.activeSelf)
            {
                infoMessageUI.gameObject.SetActive(true);
            }

            infoMessageUI.ShowMessage(message, messageVisibleDuration);
        }
    }

    private void SetChoicesVisible(bool isVisible)
    {
        if (choicesRoot != null)
        {
            choicesRoot.SetActive(isVisible);
            return;
        }

        CacheChoiceTargetsIfNeeded();

        if (choiceTargets == null)
        {
            return;
        }

        foreach (StairChoiceTarget target in choiceTargets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(isVisible);
            }
        }
    }

    private void CacheChoiceTargetsIfNeeded()
    {
        if (choiceTargets != null && choiceTargets.Length > 0)
        {
            return;
        }

        if (choicesRoot != null)
        {
            choiceTargets = choicesRoot.GetComponentsInChildren<StairChoiceTarget>(true);
        }
        else
        {
            choiceTargets = FindObjectsOfType<StairChoiceTarget>(true);
        }
    }

    private void ValidateChoiceColliders()
    {
        CacheChoiceTargetsIfNeeded();

        if (choiceTargets == null)
        {
            return;
        }

        foreach (StairChoiceTarget target in choiceTargets)
        {
            if (target != null && target.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"StairChoiceManager: '{target.name}' has no Collider, so it cannot be selected.");
            }
        }
    }
}
