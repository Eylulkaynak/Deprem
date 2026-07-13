using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class StairChoiceManager : MonoBehaviour
{
    [Header("Selection")]
    public Camera raycastCamera;
    public bool choicesEnabled = true;
    public StairChoiceTarget[] choiceTargets;
    public GameObject choicesRoot;

    [Header("Info Bubble")]
    public StairInfoBubbleUI infoBubbleUI;
    public string introMessage = "Deprem sonrasi merdivenleri kullanirken sakin ol. En guvenli yolu sec.";
    public string elevatorMessage = "Asansor deprem sonrasi tehlikelidir. Kullanma!";
    public string middleStairsMessage = "Merdivenin ortasindan inmek guvenli degil. Duvar kenarindan ilerle.";
    public string wallSideMessage = "Dogru secim! Duvar dibinden dikkatlice in.";
    public string pathCompletedMessage = "Harika! Merdivenleri guvenli sekilde indin.";
    public bool hideBubbleOnStart = false;

    [Header("Correct Choice")]
    public bool hideChoicesOnCorrect = true;
    public StairPathWalker stairPathWalker;
    public UnityEvent onCorrectChoice;

    private bool pathEventRegistered;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        CacheChoiceTargetsIfNeeded();
        ValidateChoiceColliders();
        RegisterPathCompletedEvent();
    }

    private void OnDestroy()
    {
        if (stairPathWalker != null && pathEventRegistered)
        {
            stairPathWalker.onPathCompleted.RemoveListener(HandlePathCompleted);
        }
    }

    private void Update()
    {
        if (!isActiveAndEnabled || !choicesEnabled)
        {
            return;
        }

        if (TryGetClickOrTouchPosition(out Vector2 screenPosition, out int pointerId) &&
            !IsPointerOverUi(pointerId))
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

        RegisterPathCompletedEvent();

        if (hideBubbleOnStart && infoBubbleUI != null)
        {
            infoBubbleUI.Hide();
        }

        ShowMessage(introMessage);
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
                ShowMessageAndShake(elevatorMessage);
                break;

            case StairChoiceTarget.StairChoice.MiddleStairs:
                ShowMessageAndShake(middleStairsMessage);
                break;

            case StairChoiceTarget.StairChoice.WallSide:
                HandleCorrectChoice();
                break;
        }
    }

    private void HandleCorrectChoice()
    {
        ShowMessage(wallSideMessage);
        choicesEnabled = false;

        if (hideChoicesOnCorrect)
        {
            SetChoicesVisible(false);
        }

        if (stairPathWalker != null)
        {
            stairPathWalker.StartWalkingPath();
        }

        onCorrectChoice?.Invoke();
    }

    private void HandlePathCompleted()
    {
        ShowMessage(pathCompletedMessage);
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
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        if (hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            StairChoiceTarget target = hit.collider.GetComponentInParent<StairChoiceTarget>();
            if (target != null && target.isActiveAndEnabled)
            {
                HandleChoice(target);
                return;
            }
        }
    }

    private void ShowMessage(string message)
    {
        if (infoBubbleUI != null)
        {
            infoBubbleUI.ShowMessage(message);
        }
    }

    private void ShowMessageAndShake(string message)
    {
        if (infoBubbleUI != null)
        {
            infoBubbleUI.ShowMessageAndShake(message);
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
            choiceTargets = FindObjectsByType<StairChoiceTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }

    private void RegisterPathCompletedEvent()
    {
        if (stairPathWalker == null || pathEventRegistered)
        {
            return;
        }

        stairPathWalker.onPathCompleted.AddListener(HandlePathCompleted);
        pathEventRegistered = true;
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
