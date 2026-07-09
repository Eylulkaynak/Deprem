using UnityEngine;

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
        SendTargetToMission();
    }

    public void SendTargetToMission()
    {
        if (doorMissionManager == null)
        {
            doorMissionManager = FindObjectOfType<DoorMissionManager>();
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
}
