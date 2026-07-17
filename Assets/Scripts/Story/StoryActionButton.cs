using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryActionButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image progressFill;
        [SerializeField] private Color idleColor = new Color32(25, 151, 151, 255);
        [SerializeField] private Color activeColor = new Color32(244, 173, 65, 255);
        [SerializeField, Min(40f)] private float swipeThreshold = 110f;

        private Action completed;
        private StoryInteractionGesture gesture;
        private int requiredGestureCount;
        private int completedGestureCount;
        private string actionLabel;
        private Vector2 dragStart;
        private bool dragging;
        private bool armed;
        private bool retryHint;

        public StoryInteractionGesture Gesture => gesture;
        public int RequiredGestureCount => requiredGestureCount;
        public int CompletedGestureCount => completedGestureCount;
        public float Progress01 => Mathf.Clamp01(completedGestureCount / (float)Mathf.Max(1, requiredGestureCount));
        public bool ShowingRetryHint => retryHint;

        private void Awake()
        {
            ResetVisual();
        }

        public void Present(string labelText, StoryInteractionGesture inputGesture, int gestureCount, Action onCompleted)
        {
            completed = onCompleted;
            gesture = inputGesture;
            requiredGestureCount = gesture == StoryInteractionGesture.RepeatedTap ? Mathf.Max(2, gestureCount) : 1;
            completedGestureCount = 0;
            actionLabel = labelText;
            dragging = false;
            retryHint = false;
            armed = true;
            ResetVisual();
            RefreshLabel();
            gameObject.SetActive(true);
        }

        public void Cancel()
        {
            completed = null;
            armed = false;
            dragging = false;
            retryHint = false;
            completedGestureCount = 0;
            ResetVisual();
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!armed || eventData.button != PointerEventData.InputButton.Left)
                return;
            if (gesture != StoryInteractionGesture.Tap && gesture != StoryInteractionGesture.RepeatedTap)
                return;

            completedGestureCount++;
            RefreshProgress();
            RefreshLabel();
            if (completedGestureCount >= requiredGestureCount)
                Complete();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!armed || !IsSwipeGesture())
                return;
            BeginSwipe(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!armed || !dragging)
                return;
            Vector2 delta = eventData.position - dragStart;
            float threshold = EffectiveSwipeThreshold();
            float directedDistance = gesture == StoryInteractionGesture.SwipeDown ? Mathf.Max(0f, -delta.y) : Mathf.Abs(delta.x);
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(directedDistance / threshold);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!armed || !dragging)
                return;
            EndSwipe(eventData.position);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!armed || !IsSwipeGesture() || eventData.button != PointerEventData.InputButton.Left)
                return;
            BeginSwipe(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!armed || !dragging || eventData.button != PointerEventData.InputButton.Left)
                return;
            EndSwipe(eventData.position);
        }

        private void Complete()
        {
            if (!armed)
                return;
            armed = false;
            dragging = false;
            Action callback = completed;
            completed = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void ResetVisual()
        {
            if (progressFill == null)
                return;
            progressFill.fillAmount = Progress01;
            progressFill.color = idleColor;
        }

        private void RefreshProgress()
        {
            if (progressFill != null)
                progressFill.fillAmount = Progress01;
        }

        private bool IsSwipeGesture()
        {
            return gesture == StoryInteractionGesture.SwipeDown || gesture == StoryInteractionGesture.SwipeHorizontal;
        }

        private void BeginSwipe(Vector2 pointerPosition)
        {
            if (dragging)
                return;
            dragStart = pointerPosition;
            dragging = true;
            retryHint = false;
            RefreshLabel();
            if (progressFill != null)
                progressFill.color = activeColor;
        }

        private void EndSwipe(Vector2 pointerPosition)
        {
            Vector2 delta = pointerPosition - dragStart;
            dragging = false;
            float threshold = EffectiveSwipeThreshold();
            bool directionValid = gesture == StoryInteractionGesture.SwipeDown
                ? delta.y <= -threshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.2f
                : Mathf.Abs(delta.x) >= threshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.2f;
            if (directionValid)
            {
                completedGestureCount = requiredGestureCount;
                RefreshProgress();
                Complete();
                return;
            }
            retryHint = true;
            ResetVisual();
            RefreshLabel();
        }

        private float EffectiveSwipeThreshold()
        {
            RectTransform rectTransform = transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (rectTransform == null || canvas == null)
                return swipeThreshold;

            float displayedWidth = rectTransform.rect.width * canvas.scaleFactor;
            return Mathf.Clamp(Mathf.Min(swipeThreshold, displayedWidth * 0.22f), 32f, swipeThreshold);
        }

        private void RefreshLabel()
        {
            if (label == null)
                return;
            string prefix = retryHint ? "TEKRAR DENE • " : string.Empty;
            switch (gesture)
            {
                case StoryInteractionGesture.RepeatedTap:
                    label.text = $"{prefix}{actionLabel} — DOKUN {completedGestureCount}/{requiredGestureCount}";
                    break;
                case StoryInteractionGesture.SwipeDown:
                    label.text = prefix + actionLabel + " — AŞAĞI KAYDIR";
                    break;
                case StoryInteractionGesture.SwipeHorizontal:
                    label.text = prefix + actionLabel + " — KAYDIR";
                    break;
                default:
                    label.text = prefix + actionLabel + " — DOKUN";
                    break;
            }
        }
    }
}
