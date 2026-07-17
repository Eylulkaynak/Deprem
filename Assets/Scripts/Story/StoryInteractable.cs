using System;
using UnityEngine;
using UnityEngine.Events;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryInteractable : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private string interactionId;
        [SerializeField] private StoryInteractionKind interactionKind;
        [SerializeField] private string prompt = "Etkileş";
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0.25f)] private float interactionRange = 1.35f;
        [SerializeField] private StoryInteractionGesture interactionGesture = StoryInteractionGesture.Tap;
        [SerializeField, Min(1)] private int requiredGestureCount = 1;
        [SerializeField, Min(0.25f)] private float estimatedInteractionSeconds = 1.25f;
        [Tooltip("Sürükleme yönü olan etkileşimlerde parmağın gitmesi gereken sahne hedefi.")]
        [SerializeField] private Transform gestureTarget;
        [SerializeField] private StoryCameraZoneId focusCameraZone = StoryCameraZoneId.None;
        [SerializeField] private bool returnCameraAfterCompletion;
        [SerializeField] private StoryCameraZoneId returnCameraZone = StoryCameraZoneId.RoomOverview;
        [SerializeField, Min(0f)] private float focusLingerSeconds = 1.4f;
        [SerializeField] private bool interactFromAnywhere;
        [SerializeField] private bool autoTriggerOnPlayerEnter;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private bool availableOnStart = true;
        [SerializeField] private StoryFlag requiredFlag = StoryFlag.None;
        [SerializeField] private GameObject highlightRoot;

        [Header("Scene Events")]
        [SerializeField] private UnityEvent onInteracted = new UnityEvent();
        [SerializeField] private UnityEvent onUnavailable = new UnityEvent();

        private bool available;
        private bool consumed;

        public string InteractionId => interactionId;
        public string Prompt => prompt;
        public StoryInteractionKind InteractionKind => interactionKind;
        public StoryInteractionGesture InteractionGesture => interactionGesture;
        public int RequiredGestureCount => requiredGestureCount;
        public float InteractionSeconds => estimatedInteractionSeconds;
        public float EstimatedInteractionSeconds => estimatedInteractionSeconds + focusLingerSeconds;
        public Transform GestureTarget => gestureTarget;
        public StoryCameraZoneId FocusCameraZone => focusCameraZone;
        public bool ReturnCameraAfterCompletion => returnCameraAfterCompletion;
        public StoryCameraZoneId ReturnCameraZone => returnCameraZone;
        public float FocusLingerSeconds => focusLingerSeconds;
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => interactionRange;
        public bool InteractFromAnywhere => interactFromAnywhere;
        public bool WorldSelectable => !autoTriggerOnPlayerEnter;
        public StoryFlag RequiredFlag => requiredFlag;
        public GameObject HighlightRoot => highlightRoot;
        public bool IsAvailable => available && !consumed;
        public UnityEvent OnInteracted => onInteracted;
        public UnityEvent OnUnavailable => onUnavailable;

        private void Awake()
        {
            available = availableOnStart;
            RefreshVisual();
        }

        public bool RequestInteraction(StoryPlayerMovement player)
        {
            return PrepareInteraction(player, CompletePreparedInteraction);
        }

        public bool PrepareInteraction(StoryPlayerMovement player, Action ready)
        {
            if (!IsAvailable || !RequiredFlagIsPresent())
            {
                onUnavailable.Invoke();
                return false;
            }

            Transform point = interactionPoint != null ? interactionPoint : transform;
            if (interactFromAnywhere || player == null || Vector3.Distance(player.transform.position, point.position) <= interactionRange)
            {
                player?.FaceTowards(transform.position);
                ready?.Invoke();
                return true;
            }

            return player.MoveTo(point, transform.position, () =>
            {
                Vector3 offset = player.transform.position - point.position;
                offset.y = 0f;
                float allowedDistance = interactionRange + 0.3f;
                if (offset.sqrMagnitude <= allowedDistance * allowedDistance)
                    ready?.Invoke();
            });
        }

        public void CompletePreparedInteraction()
        {
            CompleteInteraction();
        }

        public void SetAvailable(bool value)
        {
            available = value;
            if (value && !oneShot)
                consumed = false;
            RefreshVisual();
        }

        public void ResetInteraction()
        {
            consumed = false;
            available = availableOnStart;
            RefreshVisual();
        }

        private void CompleteInteraction()
        {
            if (!IsAvailable)
                return;

            if (oneShot)
                consumed = true;
            RefreshVisual();
            onInteracted.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoTriggerOnPlayerEnter || !IsAvailable)
                return;

            if (other.GetComponentInParent<StoryPlayerMovement>() != null)
                CompleteInteraction();
        }

        private bool RequiredFlagIsPresent()
        {
            return requiredFlag == StoryFlag.None ||
                   (StoryGameManager.Instance != null && StoryGameManager.Instance.HasFlag(requiredFlag));
        }

        private void RefreshVisual()
        {
            if (highlightRoot != null)
                highlightRoot.SetActive(IsAvailable);
        }
    }
}
