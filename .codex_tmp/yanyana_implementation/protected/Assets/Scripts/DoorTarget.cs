using UnityEngine;
using UnityEngine.EventSystems;

public class DoorTarget : MonoBehaviour
{
    [Header("Mission")]
    public DoorMissionManager doorMissionManager;

    [Header("Target")]
    public Transform walkTarget;

    private void Awake()
    {
        if (walkTarget == null)
        {
            walkTarget = transform;
        }
    }

    private void OnMouseDown()
    {
        if (IsPointerOverUi())
        {
            return;
        }

        SendTargetToMission();
    }

    public void SendTargetToMission()
    {
        if (doorMissionManager == null)
        {
            doorMissionManager = FindFirstObjectByType<DoorMissionManager>();
        }

        if (doorMissionManager != null)
        {
            doorMissionManager.MovePlayerToDoor(this);
        }
    }

    public Vector3 GetTargetPosition()
    {
        if (walkTarget != null)
        {
            return walkTarget.position;
        }

        return transform.position;
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (UnityEngine.Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId);
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
        {
            return EventSystem.current.IsPointerOverGameObject(0);
        }
#endif

        return false;
    }
}
