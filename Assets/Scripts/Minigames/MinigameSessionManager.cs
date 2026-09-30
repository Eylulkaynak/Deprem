using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Minigames
{
    public enum MinigameGesture
    {
        Tap,
        RepeatedTap,
        SwipeDown,
        SwipeHorizontal,
        Hold,
        DragToTarget
    }

    [Serializable]
    public sealed class MinigameAnimatorTrigger
    {
        public Animator animator;
        public string triggerName = string.Empty;

        public void Fire()
        {
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
                animator.SetTrigger(triggerName);
        }
    }

    [Serializable]
    public sealed class MinigameActionDefinition
    {
        public string actionId = string.Empty;
        [TextArea(1, 2)] public string actionLabel = string.Empty;
        public Collider targetCollider;
        public bool isCorrect = true;
        [TextArea(2, 4)] public string rejectionFeedback = "Bu güvenli seçim değil. Aynı adımı yeniden dene.";
        public Transform dragTarget;
        [Tooltip("Yatay veya serbest kaydırmalarda beklenen ekran yönü. Sıfır ise iki yön de kabul edilir.")]
        public Vector2 expectedSwipeDirection;
        [Min(1)] public int requiredActivations = 1;
        public Color guideColor = new Color32(79, 226, 216, 255);
        public GameObject highlightRoot;
        public GameObject targetHighlightRoot;
        public GameObject acceptedVisual;
        public bool hideTargetAfterAccept;
        [Min(0f)] public float authoredMoveSeconds = 0.48f;
        public AudioClip acceptedSfx;
        public AudioClip rejectedSfx;
        [Tooltip("Accepted input can play pre-authored legacy Animations without adding a game-specific runtime script.")]
        public Animation[] acceptedAnimations = Array.Empty<Animation>();
        public UnityEvent onAccepted = new UnityEvent();
        public UnityEvent onRejected = new UnityEvent();

        [NonSerialized] public bool consumed;
        [NonSerialized] public int activationCount;
        [NonSerialized] public Vector3 startPosition;
        [NonSerialized] public Quaternion startRotation;
        [NonSerialized] public Vector3 startScale;
        [NonSerialized] public bool startActive;

        public Transform TargetTransform => targetCollider != null ? targetCollider.transform : null;

        public void CaptureInitialState()
        {
            ResetAcceptedAnimation();
            Transform target = TargetTransform;
            if (target == null)
                return;
            startPosition = target.position;
            startRotation = target.rotation;
            startScale = target.localScale;
            startActive = target.gameObject.activeSelf;
            consumed = false;
            activationCount = 0;
            if (highlightRoot != null)
                highlightRoot.SetActive(false);
            if (targetHighlightRoot != null)
                targetHighlightRoot.SetActive(false);
            if (acceptedVisual != null)
                acceptedVisual.SetActive(false);
        }

        public void RestoreInitialState()
        {
            ResetAcceptedAnimation();
            Transform target = TargetTransform;
            if (target == null)
                return;
            target.gameObject.SetActive(startActive);
            target.SetPositionAndRotation(startPosition, startRotation);
            target.localScale = startScale;
            consumed = false;
            activationCount = 0;
            if (highlightRoot != null)
                highlightRoot.SetActive(false);
            if (targetHighlightRoot != null)
                targetHighlightRoot.SetActive(false);
            if (acceptedVisual != null)
                acceptedVisual.SetActive(false);
        }

        private void ResetAcceptedAnimation()
        {
            if (acceptedAnimations == null)
                return;
            for (int i = 0; i < acceptedAnimations.Length; i++)
            {
                Animation animation = acceptedAnimations[i];
                if (animation == null)
                    continue;
                animation.Stop();
                animation.Rewind();
                animation.Sample();
            }
        }
    }

    [Serializable]
    public sealed class MinigameCharacterCue
    {
        public Transform characterRoot;
        public Animator animator;
        public string enterTrigger = "StoryTalk";
        public float modelYawOffset;
        public bool faceCamera = true;
        public bool useStagePosition;
        public Vector3 stagePosition;
    }

    [Serializable]
    public sealed class MinigameStageDefinition
    {
        public string stageId = string.Empty;
        [TextArea(2, 4)] public string objective = string.Empty;
        [TextArea(2, 5)] public string subtitle = string.Empty;
        public AudioClip voiceClip;
        public MinigameGesture gesture = MinigameGesture.Tap;
        [TextArea(2, 4)] public string gestureInstruction = string.Empty;
        [Tooltip("Doğru kaynak ve hedef kılavuzlarını aşama başlar başlamaz gösterir.")]
        public bool showGuidesOnEnter = true;
        [Min(1)] public int requiredSuccesses = 1;
        [Min(0f)] public float hintDelaySeconds = 11f;
        [Min(0)] public int mistakePenalty = 100;
        [Min(0)] public int hintOrResetPenalty = 50;
        [Min(0f)] public float stageTimeLimitSeconds;
        [Min(0.25f)] public float holdDurationSeconds = 1.35f;
        [Header("Presentation")]
        public Transform cameraPose;
        [Range(28f, 60f)] public float cameraFieldOfView = 42f;
        [Min(0f)] public float cameraTransitionSeconds = 0.52f;
        public MinigameCharacterCue[] characterCues = Array.Empty<MinigameCharacterCue>();
        public MinigameAnimatorTrigger[] enterAnimatorTriggers = Array.Empty<MinigameAnimatorTrigger>();
        [Tooltip("Yalnız bu aşama aktifken görünmesi gereken önceden yerleştirilmiş nesne grubu.")]
        public GameObject stageRoot;
        public MinigameActionDefinition[] actions = Array.Empty<MinigameActionDefinition>();
        public Animation[] playOnEnter = Array.Empty<Animation>();
        public Animation[] playOnSuccess = Array.Empty<Animation>();
        public Animation[] playOnReset = Array.Empty<Animation>();
        public MinigameAnimatorTrigger[] successAnimatorTriggers = Array.Empty<MinigameAnimatorTrigger>();
        public UnityEvent onEnter = new UnityEvent();
        public UnityEvent onSuccess = new UnityEvent();
        public UnityEvent onReset = new UnityEvent();
    }

    /// <summary>
    /// Beş yeni oyunun sahnede author edilmiş aşamalarını çalıştıran tek ortak runtime yöneticisi.
    /// Dekor üretmez, Find kullanmaz ve yalnız Inspector referanslarıyla çalışır.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class MinigameSessionManager : MonoBehaviour
    {
        private const int RaycastBufferSize = 32;

        [Header("Identity")]
        [SerializeField] private string minigameId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private bool externalResultOnly;
        [SerializeField] private string hubSceneName = "Minigame_Hub";

        [Header("Explicit Startup")]
        [SerializeField] private MinigameProgressManager progressManager;
        [SerializeField] private PlayableDirector introTimeline;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MinigameStageDefinition[] stages = Array.Empty<MinigameStageDefinition>();

        [Header("HUD")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private RectTransform feedbackRect;
        [SerializeField] private Image holdProgress;
        [SerializeField] private CanvasGroup gestureCoachGroup;
        [SerializeField] private TMP_Text gestureVerbText;
        [SerializeField] private TMP_Text gestureDetailText;
        [SerializeField] private TMP_Text gestureProgressText;
        [SerializeField] private RectTransform gestureMotionRoot;
        [SerializeField] private TMP_Text gestureArrowText;
        [SerializeField] private Image gestureIconImage;
        [SerializeField] private Sprite tapGestureSprite;
        [SerializeField] private Sprite repeatedTapGestureSprite;
        [SerializeField] private Sprite swipeDownGestureSprite;
        [SerializeField] private Sprite swipeHorizontalGestureSprite;
        [SerializeField] private Sprite holdGestureSprite;
        [SerializeField] private Sprite dragGestureSprite;
        [SerializeField] private RectTransform safeAreaRect;
        [SerializeField] private CanvasGroup worldGestureGroup;
        [SerializeField] private RectTransform worldGestureRoot;
        [SerializeField] private Image worldGestureIconImage;
        [SerializeField] private RectTransform worldGestureTrail;
        [SerializeField] private Image worldGestureTrailImage;
        [SerializeField] private TMP_Text nextActionText;
        [SerializeField] private TMP_Text speakerBadgeText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDetailText;
        [SerializeField] private TMP_Text resultStarsText;
        [SerializeField] private TMP_Text resultCoinsText;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioMixerSnapshot gameplaySnapshot;
        [SerializeField] private AudioMixerSnapshot voiceSnapshot;
        [SerializeField] private AudioMixerSnapshot silenceSnapshot;

        [Header("Input Tuning")]
        [SerializeField, Min(40f)] private float swipeThresholdPixels = 95f;
        [SerializeField, Min(24f)] private float dropRadiusPixels = 145f;
        [SerializeField, Min(0f)] private float stageTransitionSeconds = 0.68f;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[RaycastBufferSize];
        private int activeStageIndex = -1;
        private int stageSuccessCount;
        private int score = 1000;
        private int mistakeCount;
        private int externalMistakeCount;
        private float sessionStartedAt;
        private float stageStartedAt;
        private bool hintCharged;
        private bool inputLocked;
        private bool completed;
        private bool voiceSnapshotActive;
        private bool silenceSnapshotRequested;
        private MinigameActionDefinition pointerAction;
        private bool pointerPressLatched;
        private Vector2 lastPointerPosition;
        private Vector2 pointerStart;
        private Vector3 pointerObjectStart;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private float holdStartedAt;
        private Coroutine transitionRoutine;
        private Coroutine cameraRoutine;
        private Coroutine feedbackRoutine;
        private int lastDisplayedSecond = int.MinValue;
        private Vector2 gestureMotionOrigin;
        private float feedbackPulseStartedAt;

        public string MinigameId => minigameId;
        public int Score => score;
        public int MistakeCount => mistakeCount;
        public int ActiveStageIndex => activeStageIndex;
        public int StageCount => stages?.Length ?? 0;
        public bool IsCompleted => completed;
        public bool ExternalResultOnly => externalResultOnly;

        private IEnumerator Start()
        {
            if (progressManager == null)
            {
                enabled = false;
                Debug.LogError("MinigameSessionManager yapılandırma hatası: Progress manager referansı eksik.", this);
                yield break;
            }
            progressManager.LoadNow();
            if (!ValidateConfiguration(out string validationError))
            {
                enabled = false;
                Debug.LogError("MinigameSessionManager yapılandırma hatası: " + validationError, this);
                yield break;
            }

            sessionStartedAt = Time.unscaledTime;
            score = 1000;
            if (titleText != null)
                titleText.text = displayName;
            if (resultPanel != null)
                resultPanel.SetActive(false);
            if (feedbackGroup != null)
                feedbackGroup.alpha = 0f;
            if (holdProgress != null)
                holdProgress.fillAmount = 0f;
            if (gestureMotionRoot != null)
                gestureMotionOrigin = gestureMotionRoot.anchoredPosition;
            if (gestureCoachGroup != null)
                gestureCoachGroup.alpha = 0f;

            if (externalResultOnly)
                yield break;

            for (int i = 0; i < stages.Length; i++)
                if (stages[i].stageRoot != null)
                    stages[i].stageRoot.SetActive(false);

            if (introTimeline != null && introTimeline.playableAsset != null)
            {
                introTimeline.Play();
                while (introTimeline.state == PlayState.Playing)
                    yield return null;
            }

            BeginStage(0);
        }

        private void Update()
        {
            UpdateVoiceDucking();
            FadeFeedbackWhenIdle();
            AnimateFeedbackToast();
            if (!externalResultOnly && !completed && activeStageIndex >= 0)
            {
                AnimateGestureCoach(stages[activeStageIndex]);
                UpdateWorldGestureGuide(stages[activeStageIndex]);
            }
            if (externalResultOnly || completed || inputLocked || activeStageIndex < 0)
                return;

            MinigameStageDefinition stage = stages[activeStageIndex];
            float stageElapsed = Time.unscaledTime - stageStartedAt;
            UpdateStageTimer(stage, stageElapsed);
            if (!hintCharged && stage.hintDelaySeconds > 0f && stageElapsed >= stage.hintDelaySeconds)
                RevealHint(stage);
            if (stage.stageTimeLimitSeconds > 0f && stageElapsed >= stage.stageTimeLimitSeconds)
            {
                ApplyPenalty(stage.hintOrResetPenalty);
                ShowFeedback("Süre doldu; yalnız bu adımı birlikte yeniden deniyoruz.", false);
                ResetCurrentStage();
                return;
            }

            if (pointerAction != null)
            {
                UpdateActivePointer(stage);
                return;
            }

            if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
                return;

            MinigameActionDefinition action = FindActionAtScreenPoint(stage, screenPosition);
            if (action == null || action.consumed)
                return;

            BeginPointerAction(stage, action, screenPosition);
        }

        public bool ValidateConfiguration(out string error)
        {
            if (progressManager == null)
            {
                error = "Progress manager referansı eksik.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(minigameId))
            {
                error = "Minigame kimliği eksik.";
                return false;
            }
            if (externalResultOnly)
            {
                error = string.Empty;
                return true;
            }
            if (worldCamera == null)
            {
                error = "World camera referansı eksik.";
                return false;
            }
            if (stages == null || stages.Length == 0)
            {
                error = "En az bir aşama gerekli.";
                return false;
            }
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                MinigameStageDefinition stage = stages[stageIndex];
                if (stage == null || stage.actions == null || stage.actions.Length == 0)
                {
                    error = $"{stageIndex + 1}. aşamanın etkileşim hedefi yok.";
                    return false;
                }
                int correctCount = 0;
                for (int actionIndex = 0; actionIndex < stage.actions.Length; actionIndex++)
                {
                    MinigameActionDefinition action = stage.actions[actionIndex];
                    if (action == null || action.targetCollider == null)
                    {
                        error = $"{stageIndex + 1}. aşamada Collider referansı eksik.";
                        return false;
                    }
                    if (action.isCorrect)
                        correctCount++;
                    action.CaptureInitialState();
                }
                if (correctCount < Mathf.Max(1, stage.requiredSuccesses))
                {
                    error = $"{stageIndex + 1}. aşamada yeterli doğru hedef yok.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void RecordCorrectAction()
        {
            if (!externalResultOnly && !completed && activeStageIndex >= 0)
                RegisterSuccess(stages[activeStageIndex], null);
        }

        public void RecordMistake()
        {
            if (!externalResultOnly && !completed && activeStageIndex >= 0)
                RejectAction(stages[activeStageIndex], null, "Aynı güvenlik adımını yeniden dene.");
        }

        public void CompleteCurrentStage()
        {
            if (externalResultOnly || completed || activeStageIndex < 0)
                return;
            stageSuccessCount = Mathf.Max(1, stages[activeStageIndex].requiredSuccesses) - 1;
            RegisterSuccess(stages[activeStageIndex], null);
        }

        public void ResetCurrentStage()
        {
            if (externalResultOnly || completed || activeStageIndex < 0)
                return;

            MinigameStageDefinition stage = stages[activeStageIndex];
            pointerAction = null;
            inputLocked = false;
            stageSuccessCount = 0;
            hintCharged = false;
            stageStartedAt = Time.unscaledTime;
            lastDisplayedSecond = int.MinValue;
            if (holdProgress != null)
                holdProgress.fillAmount = 0f;
            RestoreActions(stage);
            SetStageGuides(stage, stage.showGuidesOnEnter);
            if (stage.stageRoot != null)
                stage.stageRoot.SetActive(true);
            PlayAnimations(stage.playOnReset);
            stage.onReset.Invoke();
            PlayStageVoice(stage);
            UpdateHud(stage);
        }

        public void ReportExternalMistake()
        {
            if (externalResultOnly && !completed)
                externalMistakeCount++;
        }

        public void CompleteExternalEvacuation25D()
        {
            if (!externalResultOnly || completed)
                return;
            int stars = externalMistakeCount == 0 ? 3 : externalMistakeCount <= 2 ? 2 : 1;
            int resultScore = Mathf.Max(0, 1000 - externalMistakeCount * 100);
            CompleteExternal(stars, resultScore);
        }

        public void ReportExternalFiretruck(int collisions, int collectedCoins, int availableCoins)
        {
            if (!externalResultOnly || completed)
                return;
            int stars = collisions == 0 && collectedCoins >= 36 && availableCoins >= 42
                ? 3
                : collisions <= 2 && collectedCoins >= 24 ? 2 : 1;
            int resultScore = Mathf.Clamp(1000 - collisions * 100 - Mathf.Max(0, 36 - collectedCoins) * 5, 0, 1000);
            CompleteExternal(stars, resultScore);
        }

        public void ReturnToHub()
        {
            if (!string.IsNullOrWhiteSpace(hubSceneName))
                SceneManager.LoadScene(hubSceneName);
        }

        public void RetryScene()
        {
            Scene scene = gameObject.scene;
            if (scene.IsValid())
                SceneManager.LoadScene(scene.name);
        }

        public void UseSilenceSnapshot()
        {
            silenceSnapshotRequested = true;
            if (silenceSnapshot != null)
                silenceSnapshot.TransitionTo(0.18f);
        }

        public void UseGameplaySnapshot()
        {
            silenceSnapshotRequested = false;
            if (gameplaySnapshot != null)
                gameplaySnapshot.TransitionTo(0.18f);
        }

        private void BeginStage(int index)
        {
            if (index >= stages.Length)
            {
                CompleteAuthoredGame();
                return;
            }

            activeStageIndex = index;
            stageSuccessCount = 0;
            stageStartedAt = Time.unscaledTime;
            hintCharged = false;
            inputLocked = false;
            lastDisplayedSecond = int.MinValue;
            MinigameStageDefinition stage = stages[index];
            for (int i = 0; i < stages.Length; i++)
                if (stages[i].stageRoot != null)
                    stages[i].stageRoot.SetActive(i == index);
            for (int i = 0; i < stage.actions.Length; i++)
            {
                stage.actions[i].consumed = false;
                stage.actions[i].activationCount = 0;
            }
            ApplyCharacterStage(stage);
            HideHighlights(stage);
            PlayAnimations(stage.playOnEnter);
            FireAnimatorTriggers(stage.enterAnimatorTriggers);
            stage.onEnter.Invoke();
            PlayStageVoice(stage);
            UpdateHud(stage);
            OrientCharacterCues(stage, true);
            if (cameraRoutine != null)
                StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(EnterStageCamera(stage));
        }

        private IEnumerator EnterStageCamera(MinigameStageDefinition stage)
        {
            inputLocked = true;
            Transform pose = stage.cameraPose;
            float duration = pose == null ? 0f : Mathf.Max(0f, stage.cameraTransitionSeconds);
            Vector3 startPosition = worldCamera.transform.position;
            Quaternion startRotation = worldCamera.transform.rotation;
            float startFov = worldCamera.fieldOfView;
            Vector3 targetPosition = pose != null ? pose.position : startPosition;
            Quaternion targetRotation = pose != null ? pose.rotation : startRotation;
            float targetFov = Mathf.Clamp(stage.cameraFieldOfView, 28f, 60f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = t * t * (3f - 2f * t);
                worldCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, eased),
                    Quaternion.Slerp(startRotation, targetRotation, eased));
                worldCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, eased);
                yield return null;
            }
            worldCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            worldCamera.fieldOfView = targetFov;
            stageStartedAt = Time.unscaledTime;
            lastDisplayedSecond = int.MinValue;
            SetStageGuides(stage, stage.showGuidesOnEnter);
            inputLocked = false;
            cameraRoutine = null;
        }

        private void BeginPointerAction(MinigameStageDefinition stage, MinigameActionDefinition action, Vector2 screenPosition)
        {
            pointerAction = action;
            if (gestureCoachGroup != null)
                gestureCoachGroup.alpha = 0.42f;
            pointerStart = screenPosition;
            holdStartedAt = Time.unscaledTime;
            Transform target = action.TargetTransform;
            pointerObjectStart = target.position;
            dragPlane = new Plane(worldCamera.transform.forward, target.position);
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            dragOffset = dragPlane.Raycast(ray, out float distance)
                ? target.position - ray.GetPoint(distance)
                : Vector3.zero;

            if (stage.gesture == MinigameGesture.Tap || stage.gesture == MinigameGesture.RepeatedTap)
            {
                CompletePointerAction(stage, screenPosition);
                return;
            }
            if (holdProgress != null)
                holdProgress.fillAmount = 0f;
        }

        private void UpdateActivePointer(MinigameStageDefinition stage)
        {
            if (pointerAction == null)
                return;

            if (stage.gesture == MinigameGesture.Hold)
            {
                if (!TryGetPointerHeld(out Vector2 heldPosition))
                {
                    RejectAction(stage, pointerAction, "Erken bıraktın; hedefin üzerinde basılı tut.");
                    pointerAction = null;
                    if (holdProgress != null)
                        holdProgress.fillAmount = 0f;
                    return;
                }
                float drift = Vector2.Distance(pointerStart, heldPosition);
                if (drift > 95f)
                {
                    RejectAction(stage, pointerAction, "Parmağını güvenli hedefin üzerinde sabit tut.");
                    pointerAction = null;
                    if (holdProgress != null)
                        holdProgress.fillAmount = 0f;
                    return;
                }
                float progress = Mathf.Clamp01((Time.unscaledTime - holdStartedAt) / Mathf.Max(0.25f, stage.holdDurationSeconds));
                if (holdProgress != null)
                    holdProgress.fillAmount = progress;
                if (progress >= 1f)
                {
                    CompletePointerAction(stage, heldPosition);
                    if (holdProgress != null)
                        holdProgress.fillAmount = 0f;
                }
                return;
            }

            if (stage.gesture == MinigameGesture.DragToTarget && TryGetPointerHeld(out Vector2 dragPosition))
            {
                Ray ray = worldCamera.ScreenPointToRay(dragPosition);
                if (dragPlane.Raycast(ray, out float distance))
                {
                    Vector3 targetPosition = ray.GetPoint(distance) + dragOffset;
                    pointerAction.TargetTransform.position = Vector3.Lerp(
                        pointerAction.TargetTransform.position,
                        targetPosition,
                        1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                }
            }

            if (TryGetPointerUp(out Vector2 releasePosition))
                CompletePointerAction(stage, releasePosition);
        }

        private void CompletePointerAction(MinigameStageDefinition stage, Vector2 releasePosition)
        {
            MinigameActionDefinition action = pointerAction;
            pointerAction = null;
            if (action == null)
                return;

            bool gestureAccepted = stage.gesture switch
            {
                MinigameGesture.Tap => true,
                MinigameGesture.RepeatedTap => true,
                MinigameGesture.Hold => true,
                MinigameGesture.SwipeDown => pointerStart.y - releasePosition.y >= swipeThresholdPixels,
                MinigameGesture.SwipeHorizontal => IsExpectedSwipe(action, releasePosition),
                MinigameGesture.DragToTarget => IsNearDragTarget(action, releasePosition),
                _ => false
            };

            if (!gestureAccepted || !action.isCorrect)
            {
                Transform target = action.TargetTransform;
                if (target != null)
                    target.SetPositionAndRotation(action.startPosition, action.startRotation);
                string feedback = !gestureAccepted
                    ? GestureCorrection(stage.gesture)
                    : action.rejectionFeedback;
                RejectAction(stage, action, feedback);
                return;
            }

            action.activationCount++;
            if (action.activationCount < Mathf.Max(1, action.requiredActivations))
            {
                PlaySfx(action.acceptedSfx);
                ShowFeedback($"Ritim doğru • {action.activationCount}/{Mathf.Max(1, action.requiredActivations)}", true);
                SetStageGuides(stage, true);
                return;
            }

            action.consumed = true;
            if (action.highlightRoot != null)
                action.highlightRoot.SetActive(false);
            if (action.targetHighlightRoot != null)
                action.targetHighlightRoot.SetActive(false);
            PlayAnimations(action.acceptedAnimations);
            action.onAccepted.Invoke();
            PlaySfx(action.acceptedSfx);
            if (action.dragTarget != null || action.hideTargetAfterAccept)
                StartCoroutine(AnimateAcceptedAction(stage, action));
            else
            {
                if (action.acceptedVisual != null)
                    action.acceptedVisual.SetActive(true);
                RegisterSuccess(stage, action);
            }
        }

        private bool IsExpectedSwipe(MinigameActionDefinition action, Vector2 releasePosition)
        {
            Vector2 delta = releasePosition - pointerStart;
            if (Mathf.Abs(delta.x) < swipeThresholdPixels)
                return false;
            Vector2 expected = action != null ? action.expectedSwipeDirection : Vector2.zero;
            if (expected.sqrMagnitude < 0.01f)
                return true;
            return Vector2.Dot(delta.normalized, expected.normalized) >= 0.68f;
        }

        private IEnumerator AnimateAcceptedAction(MinigameStageDefinition stage, MinigameActionDefinition action)
        {
            inputLocked = true;
            Transform target = action.TargetTransform;
            Vector3 fromPosition = target.position;
            Quaternion fromRotation = target.rotation;
            Vector3 toPosition = action.dragTarget != null ? action.dragTarget.position : fromPosition;
            Quaternion toRotation = action.dragTarget != null ? action.dragTarget.rotation : fromRotation;
            float duration = Mathf.Max(0.05f, action.authoredMoveSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                target.position = Vector3.Lerp(fromPosition, toPosition, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.12f);
                target.rotation = Quaternion.Slerp(fromRotation, toRotation, eased);
                yield return null;
            }
            target.SetPositionAndRotation(toPosition, toRotation);
            if (action.acceptedVisual != null)
                action.acceptedVisual.SetActive(true);
            if (action.hideTargetAfterAccept)
                target.gameObject.SetActive(false);
            inputLocked = false;
            RegisterSuccess(stage, action);
        }

        private void RegisterSuccess(MinigameStageDefinition stage, MinigameActionDefinition action)
        {
            stageSuccessCount++;
            UpdateGestureProgress(stage);
            ShowFeedback(stageSuccessCount >= Mathf.Max(1, stage.requiredSuccesses)
                ? "Doğru ve güvenli karar."
                : $"Doğru • {stageSuccessCount}/{Mathf.Max(1, stage.requiredSuccesses)}", true);
            if (stageSuccessCount < Mathf.Max(1, stage.requiredSuccesses))
            {
                SetStageGuides(stage, true);
                return;
            }

            inputLocked = true;
            HideHighlights(stage);
            PlayAnimations(stage.playOnSuccess);
            FireAnimatorTriggers(stage.successAnimatorTriggers);
            stage.onSuccess.Invoke();
            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AdvanceAfterDelay());
        }

        private IEnumerator AdvanceAfterDelay()
        {
            float endAt = Time.unscaledTime + Mathf.Max(0f, stageTransitionSeconds);
            while (Time.unscaledTime < endAt)
                yield return null;
            transitionRoutine = null;
            BeginStage(activeStageIndex + 1);
        }

        private void RejectAction(MinigameStageDefinition stage, MinigameActionDefinition action, string feedback)
        {
            mistakeCount++;
            ApplyPenalty(stage.mistakePenalty);
            if (action != null)
            {
                action.onRejected.Invoke();
                PlaySfx(action.rejectedSfx);
                Transform target = action.TargetTransform;
                if (target != null)
                    target.SetPositionAndRotation(action.startPosition, action.startRotation);
            }
            SetStageGuides(stage, true);
            PlayAnimations(stage.playOnReset);
            stage.onReset.Invoke();
            ShowFeedback(string.IsNullOrWhiteSpace(feedback) ? "Aynı adımı güvenle yeniden dene." : feedback, false);
            stageStartedAt = Time.unscaledTime;
            hintCharged = false;
            UpdateHud(stage);
        }

        private void RevealHint(MinigameStageDefinition stage)
        {
            hintCharged = true;
            ApplyPenalty(stage.hintOrResetPenalty);
            SetStageGuides(stage, true);
            ShowFeedback("İpucu: parlayan kaynak ile hedefi ve hareket kartını izle.", true);
            UpdateHud(stage);
        }

        private void CompleteAuthoredGame()
        {
            int stars = MinigameProgressManager.StarsForScore(score);
            int coins = MinigameProgressManager.CoinsForStars(stars);
            CompleteResult(stars, score, coins);
        }

        private void CompleteExternal(int stars, int resultScore)
        {
            int coins = MinigameProgressManager.CoinsForStars(stars);
            CompleteResult(stars, resultScore, coins);
        }

        private void CompleteResult(int stars, int resultScore, int coins)
        {
            completed = true;
            inputLocked = true;
            if (gestureCoachGroup != null)
                gestureCoachGroup.alpha = 0f;
            silenceSnapshotRequested = false;
            score = resultScore;
            float elapsed = Mathf.Max(0.01f, Time.unscaledTime - sessionStartedAt);
            progressManager.RecordResult(minigameId, stars, resultScore, coins, elapsed);
            if (resultPanel != null)
                resultPanel.SetActive(true);
            if (resultTitleText != null)
                resultTitleText.text = stars == 3 ? "KUSURSUZ GÖREV" : stars == 2 ? "GÜVENLİ TAMAMLAMA" : "GÖREV TAMAMLANDI";
            if (resultDetailText != null)
                resultDetailText.text = $"{resultScore} PUAN  •  {FormatTime(elapsed)}  •  {mistakeCount + externalMistakeCount} DÜZELTME";
            if (resultStarsText != null)
                resultStarsText.text = stars + " / 3 YILDIZ";
            if (resultCoinsText != null)
                resultCoinsText.text = "+" + coins + " İMO COIN";
            if (gameplaySnapshot != null)
                gameplaySnapshot.TransitionTo(0.2f);
        }

        private void PlayStageVoice(MinigameStageDefinition stage)
        {
            if (subtitleText != null)
                subtitleText.text = stage.subtitle;
            UpdateSpeakerBadge(stage.subtitle);
            if (voiceSource == null || stage.voiceClip == null)
                return;
            voiceSource.Stop();
            voiceSource.clip = stage.voiceClip;
            voiceSource.Play();
            if (voiceSnapshot != null)
            {
                voiceSnapshot.TransitionTo(0.12f);
                voiceSnapshotActive = true;
            }
        }

        private void UpdateVoiceDucking()
        {
            if (!voiceSnapshotActive || voiceSource == null || voiceSource.isPlaying)
                return;
            voiceSnapshotActive = false;
            AudioMixerSnapshot returnSnapshot = silenceSnapshotRequested ? silenceSnapshot : gameplaySnapshot;
            if (returnSnapshot != null)
                returnSnapshot.TransitionTo(0.28f);
        }

        private void UpdateHud(MinigameStageDefinition stage)
        {
            if (objectiveText != null)
                objectiveText.text = stage.objective;
            if (stageText != null)
                stageText.text = $"AŞAMA {activeStageIndex + 1:00}/{stages.Length:00}";
            if (scoreText != null)
                scoreText.text = score.ToString("0000") + " PUAN";
            ConfigureGestureCoach(stage);
            UpdateStageTimer(stage, 0f);
        }

        private void UpdateStageTimer(MinigameStageDefinition stage, float elapsed)
        {
            if (timerText == null)
                return;
            float displayed = stage.stageTimeLimitSeconds > 0f
                ? Mathf.Max(0f, stage.stageTimeLimitSeconds - elapsed)
                : Mathf.Max(0f, Time.unscaledTime - sessionStartedAt);
            int second = Mathf.CeilToInt(displayed);
            if (second == lastDisplayedSecond)
                return;
            lastDisplayedSecond = second;
            if (stage.stageTimeLimitSeconds > 0f)
                timerText.SetText("{0:00} sn", second);
            else
                timerText.text = FormatTime(displayed);
        }

        private MinigameActionDefinition FindActionAtScreenPoint(MinigameStageDefinition stage, Vector2 screenPosition)
        {
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, 200f, ~0, QueryTriggerInteraction.Collide);
            for (int hitIndex = 1; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit value = hitBuffer[hitIndex];
                int insertion = hitIndex - 1;
                while (insertion >= 0 && hitBuffer[insertion].distance > value.distance)
                {
                    hitBuffer[insertion + 1] = hitBuffer[insertion];
                    insertion--;
                }
                hitBuffer[insertion + 1] = value;
            }
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider hitCollider = hitBuffer[hitIndex].collider;
                for (int actionIndex = 0; actionIndex < stage.actions.Length; actionIndex++)
                {
                    MinigameActionDefinition action = stage.actions[actionIndex];
                    if (!action.consumed && action.targetCollider != null &&
                        (hitCollider == action.targetCollider || hitCollider.transform.IsChildOf(action.targetCollider.transform)))
                        return action;
                }
            }
            return null;
        }

        private bool IsNearDragTarget(MinigameActionDefinition action, Vector2 releasePosition)
        {
            if (action.dragTarget == null)
                return false;
            Vector3 screen = worldCamera.WorldToScreenPoint(action.dragTarget.position);
            return screen.z > 0f && Vector2.Distance(releasePosition, screen) <= dropRadiusPixels;
        }

        private void RestoreActions(MinigameStageDefinition stage)
        {
            for (int i = 0; i < stage.actions.Length; i++)
                stage.actions[i].RestoreInitialState();
        }

        private static void HideHighlights(MinigameStageDefinition stage)
        {
            for (int i = 0; i < stage.actions.Length; i++)
            {
                if (stage.actions[i].highlightRoot != null)
                    stage.actions[i].highlightRoot.SetActive(false);
                if (stage.actions[i].targetHighlightRoot != null)
                    stage.actions[i].targetHighlightRoot.SetActive(false);
            }
        }

        private static void SetStageGuides(MinigameStageDefinition stage, bool visible)
        {
            if (stage?.actions == null)
                return;
            HideHighlights(stage);
            if (!visible)
                return;
            MinigameActionDefinition action = NextGuideAction(stage);
            if (action == null)
                return;
            if (action.highlightRoot != null)
                action.highlightRoot.SetActive(true);
            if (action.targetHighlightRoot != null)
                action.targetHighlightRoot.SetActive(true);
        }

        private static void PlayAnimations(Animation[] animations)
        {
            if (animations == null)
                return;
            for (int i = 0; i < animations.Length; i++)
                if (animations[i] != null)
                    animations[i].Play();
        }

        private static void FireAnimatorTriggers(MinigameAnimatorTrigger[] triggers)
        {
            if (triggers == null)
                return;
            for (int i = 0; i < triggers.Length; i++)
                triggers[i]?.Fire();
        }

        private void ApplyPenalty(int penalty)
        {
            score = Mathf.Max(0, score - Mathf.Max(0, penalty));
            if (scoreText != null)
                scoreText.text = score.ToString("0000") + " PUAN";
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip);
        }

        private void ShowFeedback(string message, bool positive)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = positive
                    ? new Color32(114, 236, 222, 255)
                    : new Color32(255, 190, 92, 255);
            }
            if (feedbackGroup != null)
            {
                feedbackGroup.alpha = 1f;
                feedbackPulseStartedAt = Time.unscaledTime;
                if (feedbackRect != null)
                    feedbackRect.localScale = Vector3.one * 0.88f;
                if (feedbackRoutine != null)
                    StopCoroutine(feedbackRoutine);
                feedbackRoutine = StartCoroutine(HideFeedbackAfter(2.25f));
            }
        }

        private IEnumerator HideFeedbackAfter(float seconds)
        {
            float hideAt = Time.unscaledTime + seconds;
            while (Time.unscaledTime < hideAt)
                yield return null;
            feedbackRoutine = null;
        }

        private void FadeFeedbackWhenIdle()
        {
            if (feedbackGroup == null || feedbackRoutine != null || feedbackGroup.alpha <= 0f)
                return;
            feedbackGroup.alpha = Mathf.MoveTowards(feedbackGroup.alpha, 0f, Time.unscaledDeltaTime * 2.6f);
        }

        private void AnimateFeedbackToast()
        {
            if (feedbackRect == null || feedbackGroup == null || feedbackGroup.alpha <= 0f)
                return;
            float elapsed = Time.unscaledTime - feedbackPulseStartedAt;
            float t = Mathf.Clamp01(elapsed / 0.22f);
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.055f;
            feedbackRect.localScale = Vector3.one * Mathf.Lerp(0.88f, overshoot, t);
        }

        private void ConfigureGestureCoach(MinigameStageDefinition stage)
        {
            if (gestureCoachGroup != null)
                gestureCoachGroup.alpha = 1f;
            if (gestureVerbText != null)
                gestureVerbText.text = GestureVerb(stage.gesture);
            if (gestureDetailText != null)
                gestureDetailText.text = string.IsNullOrWhiteSpace(stage.gestureInstruction)
                    ? DefaultGestureInstruction(stage.gesture)
                    : stage.gestureInstruction;
            Sprite gestureSprite = GestureSprite(stage.gesture);
            if (gestureArrowText != null)
            {
                gestureArrowText.text = GestureSymbol(stage.gesture);
                gestureArrowText.enabled = gestureSprite == null;
            }
            if (gestureIconImage != null)
            {
                gestureIconImage.sprite = gestureSprite;
                gestureIconImage.enabled = gestureIconImage.sprite != null;
            }
            if (gestureMotionRoot != null)
            {
                gestureMotionRoot.anchoredPosition = gestureMotionOrigin;
                gestureMotionRoot.localScale = Vector3.one;
            }
            UpdateGestureProgress(stage);
        }

        private Sprite GestureSprite(MinigameGesture gesture)
        {
            return gesture switch
            {
                MinigameGesture.RepeatedTap => repeatedTapGestureSprite != null ? repeatedTapGestureSprite : tapGestureSprite,
                MinigameGesture.SwipeDown => swipeDownGestureSprite,
                MinigameGesture.SwipeHorizontal => swipeHorizontalGestureSprite,
                MinigameGesture.Hold => holdGestureSprite,
                MinigameGesture.DragToTarget => dragGestureSprite,
                _ => tapGestureSprite
            };
        }

        private void UpdateWorldGestureGuide(MinigameStageDefinition stage)
        {
            if (worldGestureGroup == null || worldGestureRoot == null || safeAreaRect == null ||
                worldCamera == null || stage == null || !stage.showGuidesOnEnter || completed)
            {
                if (worldGestureGroup != null)
                    worldGestureGroup.alpha = 0f;
                return;
            }

            MinigameActionDefinition action = NextGuideAction(stage);
            if (action == null || action.TargetTransform == null)
            {
                worldGestureGroup.alpha = 0f;
                return;
            }

            Vector3 projectedSource = worldCamera.WorldToScreenPoint(action.targetCollider.bounds.center);
            if (projectedSource.z <= 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    safeAreaRect, projectedSource, null, out Vector2 sourceLocal))
            {
                worldGestureGroup.alpha = 0f;
                return;
            }

            Vector2 movement = Vector2.zero;
            bool showTrail = false;
            if (stage.gesture == MinigameGesture.DragToTarget && action.dragTarget != null)
            {
                Vector3 projectedTarget = worldCamera.WorldToScreenPoint(action.dragTarget.position);
                if (projectedTarget.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        safeAreaRect, projectedTarget, null, out Vector2 targetLocal))
                {
                    movement = targetLocal - sourceLocal;
                    showTrail = movement.sqrMagnitude > 64f;
                }
            }
            else if (stage.gesture == MinigameGesture.SwipeDown)
            {
                movement = Vector2.down * 150f;
                showTrail = true;
            }
            else if (stage.gesture == MinigameGesture.SwipeHorizontal)
            {
                Vector2 expected = action.expectedSwipeDirection.sqrMagnitude > 0.01f
                    ? action.expectedSwipeDirection.normalized
                    : Vector2.right;
                movement = expected * 165f;
                showTrail = true;
            }

            float cycle = Mathf.Repeat(Time.unscaledTime, 1.35f) / 1.35f;
            float eased = cycle * cycle * (3f - 2f * cycle);
            worldGestureRoot.anchoredPosition = sourceLocal + movement * eased;
            worldGestureRoot.localScale = Vector3.one * (stage.gesture == MinigameGesture.Tap ||
                                                         stage.gesture == MinigameGesture.RepeatedTap ||
                                                         stage.gesture == MinigameGesture.Hold
                ? 1f + Mathf.Sin(cycle * Mathf.PI) * 0.11f
                : 1f);

            if (worldGestureIconImage != null)
            {
                worldGestureIconImage.sprite = GestureSprite(stage.gesture);
                worldGestureIconImage.color = action.guideColor;
                worldGestureIconImage.enabled = worldGestureIconImage.sprite != null;
            }
            if (nextActionText != null)
            {
                nextActionText.text = string.IsNullOrWhiteSpace(action.actionLabel)
                    ? stage.gestureInstruction
                    : action.actionLabel;
            }
            if (worldGestureTrail != null)
            {
                worldGestureTrail.gameObject.SetActive(showTrail);
                if (showTrail)
                {
                    float length = movement.magnitude;
                    worldGestureTrail.anchoredPosition = sourceLocal + movement * 0.5f;
                    worldGestureTrail.sizeDelta = new Vector2(length, 10f);
                    worldGestureTrail.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg);
                    if (worldGestureTrailImage != null)
                        worldGestureTrailImage.color = new Color(
                            action.guideColor.r, action.guideColor.g, action.guideColor.b, 0.72f);
                }
            }
            worldGestureGroup.alpha = Mathf.MoveTowards(
                worldGestureGroup.alpha,
                inputLocked || pointerAction != null ? 0.18f : 0.96f,
                Time.unscaledDeltaTime * 7f);
        }

        private static MinigameActionDefinition NextGuideAction(MinigameStageDefinition stage)
        {
            if (stage?.actions == null)
                return null;
            for (int i = 0; i < stage.actions.Length; i++)
            {
                MinigameActionDefinition action = stage.actions[i];
                if (action != null && action.isCorrect && !action.consumed)
                    return action;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (completed || activeStageIndex < 0 || stages == null || activeStageIndex >= stages.Length)
                return;
            OrientCharacterCues(stages[activeStageIndex], false);
        }

        private void OrientCharacterCues(MinigameStageDefinition stage, bool immediate)
        {
            if (stage?.characterCues == null || worldCamera == null)
                return;
            Vector3 cameraPosition = stage.cameraPose != null
                ? stage.cameraPose.position
                : worldCamera.transform.position;
            for (int i = 0; i < stage.characterCues.Length; i++)
            {
                MinigameCharacterCue cue = stage.characterCues[i];
                if (cue == null || cue.characterRoot == null)
                    continue;
                if (cue.faceCamera)
                {
                    Vector3 direction = cameraPosition - cue.characterRoot.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up) *
                                            Quaternion.Euler(0f, cue.modelYawOffset, 0f);
                        cue.characterRoot.rotation = immediate
                            ? target
                            : Quaternion.Slerp(cue.characterRoot.rotation, target,
                                1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
                    }
                }
                if (immediate && cue.animator != null && !string.IsNullOrWhiteSpace(cue.enterTrigger))
                    cue.animator.SetTrigger(cue.enterTrigger);
            }
        }

        private void ApplyCharacterStage(MinigameStageDefinition stage)
        {
            if (stages == null)
                return;
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                MinigameCharacterCue[] cues = stages[stageIndex]?.characterCues;
                if (cues == null)
                    continue;
                for (int cueIndex = 0; cueIndex < cues.Length; cueIndex++)
                {
                    Transform root = cues[cueIndex]?.characterRoot;
                    if (root != null)
                        root.gameObject.SetActive(false);
                }
            }
            if (stage?.characterCues == null)
                return;
            for (int cueIndex = 0; cueIndex < stage.characterCues.Length; cueIndex++)
            {
                MinigameCharacterCue cue = stage.characterCues[cueIndex];
                if (cue?.characterRoot == null)
                    continue;
                if (cue.useStagePosition)
                    cue.characterRoot.position = cue.stagePosition;
                cue.characterRoot.gameObject.SetActive(true);
            }
        }

        private void UpdateGestureProgress(MinigameStageDefinition stage)
        {
            if (gestureProgressText == null)
                return;
            int required = Mathf.Max(1, stage.requiredSuccesses);
            gestureProgressText.SetText("{0}/{1} HEDEF", Mathf.Clamp(stageSuccessCount, 0, required), required);
        }

        private void AnimateGestureCoach(MinigameStageDefinition stage)
        {
            if (gestureMotionRoot == null || gestureCoachGroup == null)
                return;
            float cycle = Mathf.Repeat(Time.unscaledTime, 1.35f) / 1.35f;
            float eased = cycle * cycle * (3f - 2f * cycle);
            Vector2 offset = Vector2.zero;
            float scale = 1f;
            switch (stage.gesture)
            {
                case MinigameGesture.SwipeDown:
                    offset.y = -52f * eased;
                    break;
                case MinigameGesture.SwipeHorizontal:
                    offset.x = Mathf.Sin(cycle * Mathf.PI * 2f) * 48f;
                    break;
                case MinigameGesture.DragToTarget:
                    offset.x = 62f * eased;
                    break;
                case MinigameGesture.Hold:
                    scale = 1f + 0.13f * Mathf.Sin(cycle * Mathf.PI);
                    break;
                default:
                    scale = 1f - 0.13f * Mathf.Sin(cycle * Mathf.PI);
                    break;
            }
            gestureMotionRoot.anchoredPosition = gestureMotionOrigin + offset;
            gestureMotionRoot.localScale = Vector3.one * scale;
            float targetAlpha = pointerAction == null ? 1f : 0.42f;
            gestureCoachGroup.alpha = Mathf.MoveTowards(
                gestureCoachGroup.alpha,
                targetAlpha,
                Time.unscaledDeltaTime * 6f);
        }

        private void UpdateSpeakerBadge(string subtitle)
        {
            if (speakerBadgeText == null)
                return;
            string speaker = "REHBER";
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                int separator = subtitle.IndexOf(':');
                if (separator > 0 && separator <= 18)
                    speaker = subtitle.Substring(0, separator).Trim().ToUpperInvariant();
            }
            speakerBadgeText.text = speaker;
        }

        private static string GestureVerb(MinigameGesture gesture)
        {
            return gesture switch
            {
                MinigameGesture.RepeatedTap => "RİTİMLE DOKUN",
                MinigameGesture.SwipeDown => "AŞAĞI KAYDIR",
                MinigameGesture.SwipeHorizontal => "YATAY KAYDIR",
                MinigameGesture.Hold => "BASILI TUT",
                MinigameGesture.DragToTarget => "SÜRÜKLE • BIRAK",
                _ => "DOKUN"
            };
        }

        private static string DefaultGestureInstruction(MinigameGesture gesture)
        {
            return gesture switch
            {
                MinigameGesture.RepeatedTap => "Gösterilen ritim tamamlanana kadar aynı hedefe dokun.",
                MinigameGesture.SwipeDown => "Parlayan nesneden başla; parmağını aşağı indir.",
                MinigameGesture.SwipeHorizontal => "Parlayan noktadan başla; yatay yönde kaydır.",
                MinigameGesture.Hold => "Halka tamamlanana kadar parmağını kaldırma.",
                MinigameGesture.DragToTarget => "Parlayan nesneyi tutup işaretli hedefe taşı.",
                _ => "Parlayan güvenli hedefe bir kez dokun."
            };
        }

        private static string GestureSymbol(MinigameGesture gesture)
        {
            return gesture switch
            {
                MinigameGesture.RepeatedTap => "•••",
                MinigameGesture.SwipeDown => "↓",
                MinigameGesture.SwipeHorizontal => "↔",
                MinigameGesture.Hold => "●",
                MinigameGesture.DragToTarget => "→",
                _ => "●"
            };
        }

        private static string GestureCorrection(MinigameGesture gesture)
        {
            return gesture switch
            {
                MinigameGesture.RepeatedTap => "Aynı hedefte gösterilen dokunma ritmini tamamla.",
                MinigameGesture.SwipeDown => "Hareketi nesnenin üzerinde aşağı doğru tamamla.",
                MinigameGesture.SwipeHorizontal => "Rotayı yatay sürükleme hareketiyle ver.",
                MinigameGesture.DragToTarget => "Nesneyi parlayan gerçek hedefe kadar sürükle.",
                MinigameGesture.Hold => "Hedefin üzerinde basılı tutmayı sürdür.",
                _ => "Aynı güvenlik adımını yeniden dene."
            };
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private bool TryGetPointerDown(out Vector2 position, out int pointerId)
        {
            if (TryReadPressedPointer(out position, out pointerId))
            {
                lastPointerPosition = position;
                if (pointerPressLatched)
                    return false;
                pointerPressLatched = true;
                return true;
            }

            pointerPressLatched = false;
            position = lastPointerPosition;
            pointerId = -1;
            return false;
        }

        private bool TryGetPointerHeld(out Vector2 position)
        {
            if (TryReadPressedPointer(out position, out _))
            {
                lastPointerPosition = position;
                pointerPressLatched = true;
                return true;
            }

            position = lastPointerPosition;
            return false;
        }

        private bool TryGetPointerUp(out Vector2 position)
        {
            if (TryReadPressedPointer(out position, out _))
            {
                lastPointerPosition = position;
                pointerPressLatched = true;
                return false;
            }

            position = lastPointerPosition;
            if (!pointerPressLatched)
                return false;
            pointerPressLatched = false;
            return true;
        }

        private static bool TryReadPressedPointer(out Vector2 position, out int pointerId)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null &&
                UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
                pointerId = 0;
                return true;
            }
#endif
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    position = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                pointerId = -1;
                return true;
            }
#endif
            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            position = default;
            pointerId = -1;
            return false;
        }
    }
}
