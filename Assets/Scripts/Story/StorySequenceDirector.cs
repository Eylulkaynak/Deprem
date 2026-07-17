using System;
using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace Deprem.Story
{
    [Serializable]
    public sealed class StoryAuthoredBeat
    {
        public StoryInteractable interactable;
        public string objectiveTitle;
        [TextArea] public string objectiveDetail;
        [TextArea] public string completionSubtitle;
        [Min(0f)] public float delayAfter = 2.5f;
    }

    [DisallowMultipleComponent]
    public sealed class StorySequenceDirector : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryGameManager gameManager;
        [SerializeField] private StoryPlayerMovement player;
        [SerializeField] private StorySiblingFollower siblingFollower;
        [SerializeField] private StoryTouchManager touchManager;
        [SerializeField] private StoryCameraController cameraController;
        [SerializeField] private StoryUIController ui;

        [Header("Character animation")]
        [SerializeField] private Animator denizAnimator;
        [SerializeField] private Animator canAnimator;

        [Header("Scene-authored Sequences")]
        [SerializeField] private PlayableDirector earthquakeTimeline;
        [SerializeField] private PlayableDirector exitTimeline;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private Light roomLight;

        [Header("Pacing — first-play target 8–12 minutes")]
        [SerializeField, Range(60f, 90f)] private float introMinimumDuration = 60f;
        [SerializeField, Range(60f, 90f)] private float introMaximumDuration = 90f;
        [SerializeField, Range(45f, 120f)] private float quakeMinimumDuration = 90f;
        [SerializeField, Range(5f, 20f)] private float postQuakeSettleDuration = 10f;
        [SerializeField, Range(420f, 600f)] private float minimumCompletionDuration = 480f;
        [SerializeField, Range(20f, 60f)] private float corridorWarningDuration = 30f;
        [SerializeField, Min(0f)] private float estimatedTraversalDuration = 115f;

        [Header("Calm opening")]
        [SerializeField] private StoryInteractable[] introInspections;

        [Header("Quake")]
        [SerializeField] private StoryInteractable calmSibling;
        [SerializeField] private StoryInteractable crouchStep;
        [SerializeField] private StoryInteractable coverHeadStep;
        [SerializeField] private StoryInteractable safeCover;
        [SerializeField] private StoryInteractable unsafeDoor;
        [SerializeField] private StoryInteractable unsafeWindow;
        [SerializeField] private Transform quakeMovementCenter;
        [SerializeField, Range(1.5f, 3f)] private float quakeMovementRadius = 2.15f;
        [SerializeField, Range(6f, 15f)] private float safetyDecisionSeconds = 10f;

        [Header("Post-quake authored beats")]
        [SerializeField] private StoryAuthoredBeat[] postQuakeBeats;
        [SerializeField] private StoryInteractable lightWithFlashlight;
        [SerializeField] private StoryInteractable lightWithoutFlashlight;
        [SerializeField] private StoryInteractable corridorExit;
        [SerializeField] private StoryInteractable brokenGlassHazard;
        [SerializeField] private Transform postQuakeSafeReturn;

        [Header("Corridor authored beats")]
        [SerializeField] private StoryAuthoredBeat[] corridorBeats;

        [Header("Persistent consequences")]
        [SerializeField] private GameObject wardrobeSecured;
        [SerializeField] private GameObject wardrobeUnsecured;
        [SerializeField] private GameObject wardrobeFallen;
        [SerializeField] private GameObject shelfStable;
        [SerializeField] private GameObject shelfUnsecured;
        [SerializeField] private GameObject shelfFallen;
        [SerializeField] private GameObject clearExitRoute;
        [SerializeField] private GameObject clutteredExitRoute;
        [SerializeField] private GameObject playerFlashlight;
        [SerializeField] private GameObject emergencyRouteLights;
        [SerializeField] private GameObject closedDoor;
        [SerializeField] private GameObject openDoor;
        [SerializeField] private GameObject leftShoeWorld;
        [SerializeField] private GameObject rightShoeWorld;
        [SerializeField] private GameObject canShoesWorld;
        [SerializeField] private GameObject emergencyBagWorld;
        [SerializeField] private GameObject brokenGlassVisual;
        [SerializeField] private GameObject denizWornShoes;
        [SerializeField] private GameObject denizWornBag;
        [SerializeField] private GameObject canWornShoes;

        private StorySlicePhase phase;
        private int introIndex;
        private int postQuakeIndex;
        private int corridorIndex;
        private float sliceStartedTime;
        private float introStartedTime;
        private float quakeStartedTime;
        private bool quakeStarted;
        private bool quakeActive;
        private bool retrying;
        private bool hazardRetrying;
        private bool completionQueued;
        private int safetyPromptVersion;

        private static readonly int StoryResetHash = Animator.StringToHash("StoryReset");
        private static readonly int StoryInteractHash = Animator.StringToHash("StoryInteract");
        private static readonly int StoryPickUpHash = Animator.StringToHash("StoryPickUp");
        private static readonly int StoryInspectHash = Animator.StringToHash("StoryInspect");
        private static readonly int StoryCallHash = Animator.StringToHash("StoryCall");
        private static readonly int StoryFearHash = Animator.StringToHash("StoryFear");
        private static readonly int StoryDizzyHash = Animator.StringToHash("StoryDizzy");
        private static readonly int StoryCrouchHash = Animator.StringToHash("StoryCrouch");
        private static readonly int StoryCoverHash = Animator.StringToHash("StoryCover");
        private static readonly int StoryHoldHash = Animator.StringToHash("StoryHold");

        public StorySlicePhase CurrentPhase => phase;
        public float ElapsedSliceSeconds => Mathf.Max(0f, Time.time - sliceStartedTime);
        public float MinimumCompletionDuration => minimumCompletionDuration;
        public int AuthoredBeatCount => (introInspections?.Length ?? 0) + (postQuakeBeats?.Length ?? 0) +
                                         (corridorBeats?.Length ?? 0) + 7;
        public float EstimatedFirstPlayDuration => Mathf.Max(minimumCompletionDuration,
            introMinimumDuration + quakeMinimumDuration + postQuakeSettleDuration +
            SumBeatPacing(postQuakeBeats) + SumBeatPacing(corridorBeats) +
            corridorWarningDuration + estimatedTraversalDuration);

        private IEnumerator Start()
        {
            sliceStartedTime = Time.time;
            gameManager = StoryGameManager.Instance != null ? StoryGameManager.Instance : gameManager;
            ApplyPreparationState(false);
            DisableAllInteractions();

            StoryCheckpoint checkpoint = gameManager != null ? gameManager.CurrentState.checkpoint : StoryCheckpoint.None;
            bool quakeCompleted = gameManager != null && gameManager.CurrentState.completedActs.Contains(StoryAct.Quake);

            if (quakeCompleted)
            {
                SetupCompletedState();
                yield break;
            }

            if (checkpoint == StoryCheckpoint.CorridorReached)
            {
                SetupCorridorState();
                yield break;
            }

            if (AtLeast(checkpoint, StoryCheckpoint.PostQuake))
            {
                SetupPostQuake();
                yield break;
            }

            if (checkpoint == StoryCheckpoint.UnderCover)
            {
                BeginQuake(false);
                SetUnderCoverState();
                StartCoroutine(CompleteCoverSequence());
                yield break;
            }

            if (checkpoint == StoryCheckpoint.SiblingCalmed)
            {
                BeginQuake(false);
                SetSiblingCalmedState(false);
                yield break;
            }

            if (checkpoint == StoryCheckpoint.QuakeStart)
            {
                BeginQuake(false);
                yield break;
            }

            SetupIntro();
        }

        public void OnIntroInspection()
        {
            if (phase != StorySlicePhase.CalmOpening || introInspections == null || introIndex >= introInspections.Length)
                return;

            StoryInteractable completed = introInspections[introIndex];
            completed?.SetAvailable(false);
            FireStoryTrigger(denizAnimator, StoryInspectHash);
            if (introIndex == 0)
                FireStoryTrigger(canAnimator, StoryInteractHash);
            ShowIntroSubtitle(introIndex);
            introIndex++;

            if (introIndex < introInspections.Length)
            {
                StartCoroutine(EnableIntroStepAfterDelay(3.5f));
                return;
            }

            float remaining = Mathf.Max(2.5f, introMinimumDuration - (Time.time - introStartedTime));
            ui?.ShowObjective("SAKİN ANI HATIRLA", "Güvenli masa, pencere, sabit dolap ve çıkışın yerini aklında tut.");
            ui?.ShowSubtitle("Can: Bir şey olursa yanından ayrılmayacağım.  Deniz: Önce olduğumuz yerde korunacağız.", 7f);
            StartCoroutine(BeginQuakeAfterDelay(remaining));
        }

        public void OnSiblingCalmed()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;
            SetSiblingCalmedState(true);
        }

        public void OnPostQuakeHazard()
        {
            if (phase != StorySlicePhase.PostQuake || hazardRetrying)
                return;

            hazardRetrying = true;
            gameManager?.AddMistake();
            player?.Stop();
            impulseSource?.GenerateImpulseWithForce(0.18f);
            ui?.HideAction();
            ui?.ShowSubtitle("Ramak kala! Kırık cam alanına ayakkabısız girme; halının açık kenarından dolaş.", 4.5f);
            StartCoroutine(ReturnFromGlassHazard());
        }

        public void OnCoverReached()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;

            gameManager?.CommitCheckpoint(StoryCheckpoint.UnderCover);
            SetUnderCoverState();
            StartCoroutine(CompleteCoverSequence());
        }

        public void OnCrouchStep()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;
            FireStoryTrigger(denizAnimator, StoryCrouchHash);
            FireStoryTrigger(canAnimator, StoryCrouchHash);
            crouchStep?.SetAvailable(false);
            coverHeadStep?.SetAvailable(true);
            ArmSafetyDeadline(7f);
            cameraController?.ActivateZone(StoryCameraZoneId.UnderTable);
            ui?.ShowObjective("KAPAN — BAŞINI KORU", "Masanın altındaki koruma noktasında parmağını sabit tut; başını ve enseni kollarınla kapat.");
            ui?.ShowSubtitle("Deniz dizlerinin üzerine çöktü. Şimdi başını ve ensesini koruyor.", 5f);
        }

        public void OnCoverHeadStep()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;
            FireStoryTrigger(denizAnimator, StoryCoverHash);
            FireStoryTrigger(canAnimator, StoryCoverHash);
            coverHeadStep?.SetAvailable(false);
            safeCover?.SetAvailable(true);
            ArmSafetyDeadline(7f);
            ui?.ShowObjective("TUTUN — DENGENİ KORU", "Sarsıntı kamerayı oynatırken masa ayağının üzerinde basılı tut; parmağını hedefte sabit tut.");
            ui?.ShowSubtitle("Can da başını koruyor. Deniz boşta kalan eliyle masa ayağına uzanıyor.", 5f);
        }

        public void OnUnsafeChoice()
        {
            if (!quakeActive || retrying)
                return;

            retrying = true;
            safetyPromptVersion++;
            gameManager?.AddMistake();
            impulseSource?.GenerateImpulseWithForce(1.15f);
            impactSource?.Play();
            ui?.HideAction();
            ui?.ShowSubtitle("Ramak kala! Sarsıntı sürerken kapıya veya pencereye koşma. Yakınındaki güvenli noktada korun.", 5f);
            StartCoroutine(RetryFromCheckpoint());
        }

        public void OnPostQuakeStep()
        {
            if (phase != StorySlicePhase.PostQuake || postQuakeBeats == null || postQuakeIndex >= postQuakeBeats.Length)
                return;

            int completedBeatIndex = postQuakeIndex;
            StoryAuthoredBeat beat = postQuakeBeats[completedBeatIndex];
            beat?.interactable?.SetAvailable(false);
            AnimatePostQuakeBeat(postQuakeIndex);
            if (!string.IsNullOrWhiteSpace(beat?.completionSubtitle))
                ui?.ShowSubtitle(ResolveConditionalText(beat.completionSubtitle), 6.5f);
            postQuakeIndex++;

            if (completedBeatIndex == 9)
                brokenGlassHazard?.SetAvailable(false);

            if (postQuakeIndex < postQuakeBeats.Length)
                StartCoroutine(EnablePostQuakeBeatAfterDelay(beat?.delayAfter ?? 2.5f));
            else
                StartCoroutine(EnableLightChoiceAfterDelay(beat?.delayAfter ?? 2.5f));
        }

        public void OnLightPrepared()
        {
            if (phase != StorySlicePhase.PostQuake)
                return;

            lightWithFlashlight?.SetAvailable(false);
            lightWithoutFlashlight?.SetAvailable(false);
            FireStoryTrigger(denizAnimator, StoryInteractHash);
            bool hasFlashlight = HasFlag(StoryFlag.BagFlashlight);
            SetActive(playerFlashlight, hasFlashlight);
            SetActive(emergencyRouteLights, !hasFlashlight);
            corridorExit?.SetAvailable(true);
            ui?.ShowObjective("KAPIYA GÜVENLE YAKLAŞ", "Sarsıntı durdu. Can yanında; çıkış yolunu acele etmeden izle.");
            ui?.ShowSubtitle(hasFlashlight
                ? "Hazırladığın fener çalışıyor. Işığı zemine tutarak kırık parçaları gör."
                : "Çantada fener yok. Zayıf acil aydınlatmayı izleyip adımlarını yavaşlat.", 7f);
        }

        public void OnCorridorReached()
        {
            if (phase != StorySlicePhase.PostQuake)
                return;

            corridorExit?.SetAvailable(false);
            FireStoryTrigger(denizAnimator, StoryResetHash);
            FireStoryTrigger(canAnimator, StoryResetHash);
            gameManager?.CommitCheckpoint(StoryCheckpoint.CorridorReached);
            SetupCorridorState();
        }

        public void OnCorridorStep()
        {
            if (phase != StorySlicePhase.Corridor || corridorBeats == null || corridorIndex >= corridorBeats.Length)
                return;

            StoryAuthoredBeat beat = corridorBeats[corridorIndex];
            beat?.interactable?.SetAvailable(false);
            if (!string.IsNullOrWhiteSpace(beat?.completionSubtitle))
                ui?.ShowSubtitle(beat.completionSubtitle, 6.5f);
            corridorIndex++;

            if (corridorIndex < corridorBeats.Length)
                StartCoroutine(EnableCorridorBeatAfterDelay(beat?.delayAfter ?? 2.5f));
            else if (!completionQueued)
            {
                completionQueued = true;
                StartCoroutine(CompleteAfterWarningSequence());
            }
        }

        private void SetupIntro()
        {
            FireStoryTrigger(denizAnimator, StoryResetHash);
            FireStoryTrigger(canAnimator, StoryResetHash);
            phase = StorySlicePhase.CalmOpening;
            introIndex = 0;
            quakeStarted = false;
            quakeActive = false;
            introStartedTime = Time.time;
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            player?.SetNavigationEnabled(true);
            siblingFollower?.SetFollowing(false);
            cameraController?.SetImpulseEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.RoomOverview, true);
            ui?.HideAction();
            ui?.ShowObjective("ODAYI OKU — 1/4", "Can'la birlikte güvenli masayı incele.");
            ui?.ShowSubtitle("Sakin bir aile günü. Deniz ile Can salonda oyun oynuyor; evin sesleri her zamanki gibi.", 7f);
            if (introInspections != null && introInspections.Length > 0)
                introInspections[0]?.SetAvailable(true);
            StartCoroutine(IntroTimeout());
        }

        private IEnumerator IntroTimeout()
        {
            yield return new WaitForSeconds(introMaximumDuration);
            if (!quakeStarted)
                BeginQuake(true);
        }

        private IEnumerator EnableIntroStepAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.CalmOpening || introIndex >= (introInspections?.Length ?? 0))
                yield break;
            introInspections[introIndex]?.SetAvailable(true);
            string[] details =
            {
                "Can'la birlikte güvenli masayı incele.",
                "Pencerenin neden tehlikeli olabileceğini incele.",
                "Dolabın duvara sabitlenmesini kontrol et.",
                "Koridora açılan çıkışın önünü kontrol et."
            };
            ui?.ShowObjective($"ODAYI OKU — {introIndex + 1}/4", details[Mathf.Min(introIndex, details.Length - 1)]);
        }

        private void BeginQuake(bool commitCheckpoint)
        {
            if (quakeStarted)
                return;

            quakeStarted = true;
            quakeActive = true;
            phase = StorySlicePhase.Quake;
            quakeStartedTime = Time.time;
            DisableAllInteractions();
            if (commitCheckpoint)
                gameManager?.CommitCheckpoint(StoryCheckpoint.QuakeStart);
            Vector3 constraintCenter = quakeMovementCenter != null ? quakeMovementCenter.position : player != null ? player.transform.position : Vector3.zero;
            player?.SetMovementConstraint(constraintCenter, quakeMovementRadius);
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            player?.SetNavigationEnabled(true);
            siblingFollower?.SetFollowing(false);
            cameraController?.SetImpulseEnabled(true);
            cameraController?.ActivateZone(StoryCameraZoneId.QuakeClose);
            FireStoryTrigger(denizAnimator, StoryFearHash);
            FireStoryTrigger(canAnimator, StoryFearHash);
            calmSibling?.SetAvailable(true);
            unsafeDoor?.SetAvailable(true);
            unsafeWindow?.SetAvailable(true);
            ui?.ShowObjective("SARSINTI BAŞLADI", "Koşma. Can'a seslen; yalnızca kol mesafesindeki güvenli masayı kullan.");
            ui?.ShowSubtitle("Can! Benimle kal. Pencereden uzak dur!", 6f);
            earthquakeTimeline?.Play();
            impactSource?.Play();
            StartCoroutine(ImpulseLoop());
            ArmSafetyDeadline(safetyDecisionSeconds);
        }

        private void SetSiblingCalmedState(bool commit)
        {
            if (commit)
                gameManager?.CommitCheckpoint(StoryCheckpoint.SiblingCalmed);
            FireStoryTrigger(denizAnimator, StoryCallHash);
            FireStoryTrigger(canAnimator, StoryInteractHash);
            FaceSiblingsTowardsEachOther();
            calmSibling?.SetAvailable(false);
            crouchStep?.SetAvailable(true);
            coverHeadStep?.SetAvailable(false);
            safeCover?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.UnderTable);
            ArmSafetyDeadline(8f);
            ui?.ShowObjective("ÇÖK — GÜVENLİ NOKTAYA GEÇ", "Can'ı kol mesafende tutarak masanın yakın tarafındaki halkaya dokun.");
            ui?.ShowSubtitle("Deniz: Benimle kal. Önce olduğumuz yerde çökeceğiz!", 6f);
        }

        private void SetUnderCoverState()
        {
            FireStoryTrigger(denizAnimator, StoryHoldHash);
            FireStoryTrigger(canAnimator, StoryHoldHash);
            phase = StorySlicePhase.UnderCover;
            safetyPromptVersion++;
            DisableAllInteractions();
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            player?.SetNavigationEnabled(false);
            player?.ClearMovementConstraint();
            siblingFollower?.SetFollowing(false);
            cameraController?.ActivateZone(StoryCameraZoneId.UnderTable);
            ui?.ShowObjective("ÇÖK • KAPAN • TUTUN", "Başını ve enseni koru. Sarsıntı bitene kadar masaya tutun.");
            ui?.ShowSubtitle("Masa hareket ediyor; Deniz bir eliyle Can'ı, diğeriyle masa ayağını tutuyor.", 8f);
        }

        private IEnumerator CompleteCoverSequence()
        {
            string[] shelterLines =
            {
                "Dizlerini karnına çek, başını koru. Sarsıntı hâlâ sürüyor.",
                "Can nefesini düzenliyor. Dolaptan ses geliyor ama güvenli rota açık.",
                "Sarsıntı azalıyor; yine de tamamen durmadan yerinden kalkma.",
                "Son hareketleri bekle. Pencereden ve devrilebilecek eşyalardan uzak kal."
            };
            int line = 0;
            while (quakeActive)
            {
                float elapsed = Time.time - quakeStartedTime;
                float remaining = quakeMinimumDuration - elapsed;
                if (remaining <= 0f)
                    break;
                yield return new WaitForSeconds(Mathf.Min(18f, remaining));
                if (quakeActive && line < shelterLines.Length)
                    ui?.ShowSubtitle(shelterLines[line++], 8f);
            }
            EndQuake();
        }

        private void EndQuake()
        {
            if (!quakeActive)
                return;

            quakeActive = false;
            cameraController?.SetImpulseEnabled(false);
            earthquakeTimeline?.Stop();
            gameManager?.CommitCheckpoint(StoryCheckpoint.PostQuake);
            SetupPostQuake();
        }

        private void SetupPostQuake()
        {
            FireStoryTrigger(denizAnimator, StoryDizzyHash);
            FireStoryTrigger(canAnimator, StoryDizzyHash);
            phase = StorySlicePhase.PostQuake;
            quakeStarted = true;
            quakeActive = false;
            postQuakeIndex = 0;
            hazardRetrying = false;
            ResetPostQuakeEquipment();
            DisableAllInteractions();
            ApplyPreparationState(true);
            if (roomLight != null)
                roomLight.intensity = 0.45f;
            cameraController?.SetImpulseEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PostQuake);
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            player?.SetNavigationEnabled(true);
            player?.ClearMovementConstraint();
            siblingFollower?.SetFollowing(true);
            brokenGlassHazard?.SetAvailable(true);
            ui?.ShowObjective("SARSINTI DURDU — BEKLE", "Önce yeni bir hareket, düşen parça veya kırık cam sesi var mı dinle.");
            ui?.ShowSubtitle("Anne (engelin arkasından): Çocuklar, iyi misiniz? Olduğunuz yerde birbirinizi kontrol edin!", 8f);
            StartCoroutine(EnablePostQuakeBeatAfterDelay(postQuakeSettleDuration));
        }

        private IEnumerator EnablePostQuakeBeatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.PostQuake || postQuakeIndex >= (postQuakeBeats?.Length ?? 0))
                yield break;
            StoryAuthoredBeat beat = postQuakeBeats[postQuakeIndex];
            beat?.interactable?.SetAvailable(true);
            ui?.ShowObjective(beat?.objectiveTitle ?? "ÇEVREYİ KONTROL ET", ResolveConditionalText(beat?.objectiveDetail));
        }

        private IEnumerator EnableLightChoiceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.PostQuake)
                yield break;
            bool hasFlashlight = HasFlag(StoryFlag.BagFlashlight);
            StoryInteractable choice = hasFlashlight ? lightWithFlashlight : lightWithoutFlashlight;
            choice?.SetAvailable(true);
            ui?.ShowObjective(hasFlashlight ? "FENERİ DENE" : "ACİL IŞIĞI BUL",
                hasFlashlight ? "Çantadaki feneri zemine yönelt ve çalıştığını kontrol et."
                              : "Koridordaki zayıf acil aydınlatmayı görüp güvenli rotayı belirle.");
        }

        private void SetupCorridorState()
        {
            phase = StorySlicePhase.Corridor;
            corridorIndex = 0;
            quakeStarted = true;
            quakeActive = false;
            DisableAllInteractions();
            brokenGlassHazard?.SetAvailable(false);
            ApplyPreparationState(true);
            SetPostQuakeEquipmentComplete();
            bool hasFlashlight = HasFlag(StoryFlag.BagFlashlight);
            SetActive(playerFlashlight, hasFlashlight);
            SetActive(emergencyRouteLights, !hasFlashlight);
            SetDoorOpen(true);
            cameraController?.SetImpulseEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.Corridor, true);
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            player?.SetNavigationEnabled(true);
            player?.ClearMovementConstraint();
            siblingFollower?.SetFollowing(true);
            ui?.ShowObjective("KORİDOR EŞİĞİ", "Can yanında mı kontrol et; zemini ve tavandan gelen sesleri dinle.");
            ui?.ShowSubtitle("Koridor karanlık ve dar. Deniz, Can'ı önüne alıp acele etmeden ilerliyor.", 7f);
            StartCoroutine(EnableCorridorBeatAfterDelay(3f));
        }

        private IEnumerator EnableCorridorBeatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.Corridor || corridorIndex >= (corridorBeats?.Length ?? 0))
                yield break;
            StoryAuthoredBeat beat = corridorBeats[corridorIndex];
            beat?.interactable?.SetAvailable(true);
            ui?.ShowObjective(beat?.objectiveTitle ?? "KORİDORDA İLERLE", beat?.objectiveDetail);
        }

        private IEnumerator CompleteAfterWarningSequence()
        {
            phase = StorySlicePhase.Corridor;
            touchManager?.SetWorldNavigationEnabled(false);
            player?.SetNavigationEnabled(false);
            siblingFollower?.SetFollowing(false);
            ui?.ShowObjective("ARTÇI SARSINTI HABERCİSİ", "İnce titreşim ve tavandan gelen sesi fark et; merdivene koşma.");
            impulseSource?.GenerateImpulseWithForce(0.28f);
            string[] lines =
            {
                "Can: Yine mi sallanacak?  Deniz: Yanımda kal; acele etmeden bekleyeceğiz.",
                "Anne (uzaktan): Merdivene çıkmayın, ses kesilene kadar koridorda bekleyin!",
                "Deniz duvara yaslanmıyor; düşebilecek eşyalardan uzak, açık bir noktada kalıyor.",
                "Titreşim kesiliyor. Tahliye perdesi bu güvenli bekleyişten sonra başlayacak."
            };
            float warningStarted = Time.time;
            int line = 0;
            while (Time.time - warningStarted < corridorWarningDuration || ElapsedSliceSeconds < minimumCompletionDuration)
            {
                ui?.ShowSubtitle(lines[line % lines.Length], 9f);
                line++;
                yield return new WaitForSeconds(10f);
            }

            gameManager?.CompleteAct(StoryAct.Quake);
            phase = StorySlicePhase.Completed;
            ui?.ShowCompletion();
            Debug.Log($"Story_03_Quake completed in {ElapsedSliceSeconds:F1} seconds with {gameManager?.CurrentState.mistakeCount ?? 0} mistakes.", this);
        }

        private void SetupCompletedState()
        {
            FireStoryTrigger(denizAnimator, StoryResetHash);
            FireStoryTrigger(canAnimator, StoryResetHash);
            phase = StorySlicePhase.Completed;
            quakeStarted = true;
            quakeActive = false;
            DisableAllInteractions();
            ApplyPreparationState(true);
            SetPostQuakeEquipmentComplete();
            SetDoorOpen(true);
            cameraController?.SetImpulseEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.Corridor, true);
            touchManager?.SetWorldNavigationEnabled(false);
            player?.SetNavigationEnabled(false);
            siblingFollower?.SetFollowing(false);
            ui?.ShowObjective("DİKEY DİLİM TAMAMLANDI", "Koridora güvenle ulaştın; tahliye perdesi buradan devam edecek.");
            ui?.ShowCompletion();
        }

        private void ApplyPreparationState(bool afterQuake)
        {
            bool securedWardrobe = HasFlag(StoryFlag.WardrobeSecured);
            bool securedShelf = HasFlag(StoryFlag.ShelfSecured);
            bool exitCleared = HasFlag(StoryFlag.ExitCleared);

            SetActive(wardrobeSecured, securedWardrobe);
            SetActive(wardrobeUnsecured, !securedWardrobe && !afterQuake);
            SetActive(wardrobeFallen, !securedWardrobe && afterQuake);
            SetActive(shelfStable, securedShelf);
            SetActive(shelfUnsecured, !securedShelf && !afterQuake);
            SetActive(shelfFallen, !securedShelf && afterQuake);
            SetActive(clearExitRoute, exitCleared);
            SetActive(clutteredExitRoute, !exitCleared);
            SetActive(brokenGlassVisual, afterQuake);
            SetActive(playerFlashlight, false);
            SetActive(emergencyRouteLights, false);
            SetDoorOpen(false);
        }

        private void DisableAllInteractions()
        {
            ui?.HideAction();
            if (introInspections != null)
                foreach (StoryInteractable interactable in introInspections)
                    interactable?.SetAvailable(false);
            calmSibling?.SetAvailable(false);
            crouchStep?.SetAvailable(false);
            coverHeadStep?.SetAvailable(false);
            safeCover?.SetAvailable(false);
            unsafeDoor?.SetAvailable(false);
            unsafeWindow?.SetAvailable(false);
            brokenGlassHazard?.SetAvailable(false);
            if (postQuakeBeats != null)
                foreach (StoryAuthoredBeat beat in postQuakeBeats)
                    beat?.interactable?.SetAvailable(false);
            lightWithFlashlight?.SetAvailable(false);
            lightWithoutFlashlight?.SetAvailable(false);
            corridorExit?.SetAvailable(false);
            if (corridorBeats != null)
                foreach (StoryAuthoredBeat beat in corridorBeats)
                    beat?.interactable?.SetAvailable(false);
        }

        private IEnumerator BeginQuakeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!quakeStarted)
                BeginQuake(true);
        }

        private IEnumerator ImpulseLoop()
        {
            while (quakeActive)
            {
                impulseSource?.GenerateImpulseWithForce(UnityEngine.Random.Range(0.55f, 1f));
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.42f, 0.68f));
            }
        }

        private IEnumerator RetryFromCheckpoint()
        {
            yield return new WaitForSeconds(3f);
            retrying = false;
            ui?.HideAction();
            StoryCheckpoint checkpoint = gameManager != null ? gameManager.CurrentState.checkpoint : StoryCheckpoint.QuakeStart;
            if (checkpoint == StoryCheckpoint.SiblingCalmed)
                SetSiblingCalmedState(false);
            else
            {
                Vector3 constraintCenter = quakeMovementCenter != null ? quakeMovementCenter.position : player != null ? player.transform.position : Vector3.zero;
                player?.SetMovementConstraint(constraintCenter, quakeMovementRadius);
                player?.SetNavigationEnabled(true);
                touchManager?.SetWorldNavigationEnabled(true);
                calmSibling?.SetAvailable(true);
                crouchStep?.SetAvailable(false);
                coverHeadStep?.SetAvailable(false);
                safeCover?.SetAvailable(false);
                unsafeDoor?.SetAvailable(true);
                unsafeWindow?.SetAvailable(true);
                ui?.ShowObjective("CAN'I YANINA ÇAĞIR", "Sarsıntı sürerken bulunduğun yerden uzaklaşma; Can'ı kol mesafende tut.");
                ArmSafetyDeadline(safetyDecisionSeconds);
            }
        }

        private void ArmSafetyDeadline(float seconds)
        {
            int version = ++safetyPromptVersion;
            StartCoroutine(SafetyDeadline(version, seconds));
        }

        private IEnumerator SafetyDeadline(int version, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (version == safetyPromptVersion && phase == StorySlicePhase.Quake && quakeActive && !retrying)
            {
                ui?.ShowSubtitle("Çok uzun bekledin; çevredeki eşya devrilmeden yakındaki güvenli harekete geç.", 3.5f);
                OnUnsafeChoice();
            }
        }

        private IEnumerator ReturnFromGlassHazard()
        {
            yield return new WaitForSeconds(0.55f);
            if (postQuakeSafeReturn != null)
            {
                player?.Warp(postQuakeSafeReturn.position);
                player?.FaceTowards(postQuakeSafeReturn.position + postQuakeSafeReturn.forward);
            }
            hazardRetrying = false;
        }

        private void ShowIntroSubtitle(int index)
        {
            string[] subtitles =
            {
                "Deniz: Masa sağlam; sallanırken koşmadan burada Çök–Kapan–Tutun yapabiliriz.",
                "Can: Pencerenin yanından uzak duracağız, değil mi?  Deniz: Cam kırılabilir, evet.",
                HasFlag(StoryFlag.WardrobeSecured)
                    ? "Deniz: Dolap duvara sabitlenmiş; devrilme riski azalmış."
                    : "Deniz: Dolap sabit değil. Sarsıntıdan sonra bu taraftan uzak durmalıyız.",
                HasFlag(StoryFlag.ExitCleared)
                    ? "Deniz: Çıkışın önü açık; sarsıntı tamamen durunca buradan çıkabiliriz."
                    : "Deniz: Çıkışta kutular var; sarsıntıdan sonra açık kalan taraftan dolaşmalıyız."
            };
            ui?.ShowSubtitle(subtitles[Mathf.Clamp(index, 0, subtitles.Length - 1)], 7f);
        }

        private string ResolveConditionalText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return text
                .Replace("{WARDROBE}", HasFlag(StoryFlag.WardrobeSecured)
                    ? "Sabitlenen dolap yerinde kaldı; yan geçit açık."
                    : "Dolap yan geçidi kapattı; ana güvenli rota hâlâ açık.")
                .Replace("{EXIT}", HasFlag(StoryFlag.ExitCleared)
                    ? "Önceden temizlenen çıkış açık kaldı."
                    : "Çıkıştaki kutular geçişi yavaşlatıyor; işaretli taraftan dolaş.")
                .Replace("{WATER}", HasFlag(StoryFlag.BagWater)
                    ? "Su şişesi yerinde ve kapağı kapalı."
                    : "Su şişesi yok; bu eksik tahliyeyi durdurmuyor ama hazırlık raporuna işlenecek.")
                .Replace("{FIRSTAID}", HasFlag(StoryFlag.BagFirstAid)
                    ? "İlk yardım paketi kapalı gözde duruyor; şu anda kullanmak gerekmiyor."
                    : "İlk yardım paketi yok; kimse yaralı olmadığı için güvenli çıkışa devam edilebilir.");
        }

        private bool HasFlag(StoryFlag flag)
        {
            return gameManager != null && gameManager.HasFlag(flag);
        }

        private void AnimatePostQuakeBeat(int beatIndex)
        {
            ApplyPostQuakePhysicalResult(beatIndex);
            if (beatIndex >= 1 && beatIndex <= 3)
                FaceSiblingsTowardsEachOther();
            if (beatIndex <= 1)
            {
                FireStoryTrigger(denizAnimator, StoryCallHash);
                if (beatIndex == 1)
                    FireStoryTrigger(canAnimator, StoryInteractHash);
                return;
            }

            if (beatIndex <= 6 || beatIndex == 12 || beatIndex == 13)
            {
                FireStoryTrigger(denizAnimator, StoryInspectHash);
                return;
            }

            if (beatIndex == 7 || beatIndex == 8 || beatIndex == 11)
            {
                FireStoryTrigger(denizAnimator, StoryPickUpHash);
                return;
            }

            FireStoryTrigger(denizAnimator, StoryInteractHash);
            if (beatIndex == 10)
                FireStoryTrigger(canAnimator, StoryInteractHash);
        }

        private void FaceSiblingsTowardsEachOther()
        {
            if (player == null || siblingFollower == null)
                return;

            player.FaceTowards(siblingFollower.transform.position);
            Vector3 direction = player.transform.position - siblingFollower.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                siblingFollower.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ApplyPostQuakePhysicalResult(int beatIndex)
        {
            switch (beatIndex)
            {
                case 7:
                    SetActive(leftShoeWorld, false);
                    break;
                case 8:
                    SetActive(rightShoeWorld, false);
                    break;
                case 9:
                    SetActive(leftShoeWorld, false);
                    SetActive(rightShoeWorld, false);
                    SetActive(denizWornShoes, true);
                    break;
                case 10:
                    FaceSiblingsTowardsEachOther();
                    SetActive(canShoesWorld, false);
                    SetActive(canWornShoes, true);
                    break;
                case 11:
                    SetActive(emergencyBagWorld, false);
                    SetActive(denizWornBag, true);
                    break;
            }
        }

        private void ResetPostQuakeEquipment()
        {
            SetActive(leftShoeWorld, true);
            SetActive(rightShoeWorld, true);
            SetActive(canShoesWorld, true);
            SetActive(emergencyBagWorld, true);
            SetActive(denizWornShoes, false);
            SetActive(denizWornBag, false);
            SetActive(canWornShoes, false);
        }

        private void SetPostQuakeEquipmentComplete()
        {
            SetActive(leftShoeWorld, false);
            SetActive(rightShoeWorld, false);
            SetActive(canShoesWorld, false);
            SetActive(emergencyBagWorld, false);
            SetActive(denizWornShoes, true);
            SetActive(denizWornBag, true);
            SetActive(canWornShoes, true);
        }

        private static void FireStoryTrigger(Animator animator, int triggerHash)
        {
            if (animator == null || !animator.isActiveAndEnabled)
                return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash != triggerHash || parameter.type != AnimatorControllerParameterType.Trigger)
                    continue;
                animator.SetTrigger(triggerHash);
                return;
            }
        }

        private void SetDoorOpen(bool open)
        {
            SetActive(closedDoor, !open);
            SetActive(openDoor, open);
            if (open && exitTimeline != null)
                exitTimeline.Play();
        }

        private static float SumBeatPacing(StoryAuthoredBeat[] beats)
        {
            if (beats == null)
                return 0f;
            return beats.Where(beat => beat != null).Sum(beat =>
                (beat.interactable != null ? beat.interactable.EstimatedInteractionSeconds : 0f) + beat.delayAfter);
        }

        private static bool AtLeast(StoryCheckpoint value, StoryCheckpoint target)
        {
            return (int)value >= (int)target;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
