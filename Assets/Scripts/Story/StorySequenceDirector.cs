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

        [Header("Flow variant")]
        [Tooltip("Yeni Story 03 önizlemesindeki doğal açılış, sıkıştırılmış deprem sonrası akış ve kısa koridor artçısını kullanır.")]
        [SerializeField] private bool revisedFlow;
        [SerializeField, Min(0)] private int revisedShoesBeatIndex = 3;
        [SerializeField, Min(0)] private int revisedCanShoesBeatIndex = 4;
        [SerializeField, Min(0)] private int revisedBagBeatIndex = 5;

        [Header("Pacing — first-play target 8–12 minutes")]
        [SerializeField, Min(0f)] private float introMinimumDuration = 60f;
        [SerializeField, Min(1f)] private float introMaximumDuration = 90f;
        [SerializeField, Min(1f)] private float quakeMinimumDuration = 90f;
        [SerializeField, Min(0f)] private float postQuakeSettleDuration = 10f;
        [SerializeField, Min(0f)] private float minimumCompletionDuration = 480f;
        [SerializeField, Min(0f)] private float corridorWarningDuration = 30f;
        [SerializeField, Min(0f)] private float estimatedTraversalDuration = 115f;

        [Header("Calm opening")]
        [SerializeField] private StoryInteractable[] introInspections;
        [SerializeField] private StoryInteractable[] introOptionalMoments;
        [SerializeField, Min(0f)] private float revisedQuakeDelayAfterFamilyMoment = 55f;

        [Header("Quake")]
        [SerializeField] private StoryInteractable calmSibling;
        [SerializeField] private StoryInteractable crouchStep;
        [SerializeField] private StoryInteractable coverHeadStep;
        [SerializeField] private StoryInteractable safeCover;
        // Çök-Kapan-Tutun sıra-eşleme ilerlemesi: 0=çök bekleniyor, 1=kapan, 2=tutun.
        private int cktProgress;
        [SerializeField] private StoryInteractable unsafeDoor;
        [SerializeField] private StoryInteractable unsafeWindow;
        [SerializeField] private Transform denizCoverAnchor;
        [SerializeField] private Transform canCoverAnchor;
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
        [SerializeField] private GameObject canComfortItem;

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
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        public StorySlicePhase CurrentPhase => phase;
        public float ElapsedSliceSeconds => Mathf.Max(0f, Time.time - sliceStartedTime);
        public float MinimumCompletionDuration => minimumCompletionDuration;
        public bool RevisedFlow => revisedFlow;
        public int AuthoredBeatCount => (introInspections?.Length ?? 0) + (introOptionalMoments?.Length ?? 0) +
                                          (postQuakeBeats?.Length ?? 0) +
                                          (corridorBeats?.Length ?? 0) + 7;
        public float EstimatedFirstPlayDuration => Mathf.Max(minimumCompletionDuration,
            introMinimumDuration + quakeMinimumDuration + postQuakeSettleDuration +
            SumBeatPacing(postQuakeBeats) + SumBeatPacing(corridorBeats) +
            corridorWarningDuration + estimatedTraversalDuration);

        private IEnumerator Start()
        {
            sliceStartedTime = Time.time;
            gameManager = StoryGameManager.Instance != null ? StoryGameManager.Instance : gameManager;
            gameManager?.BeginAct(StoryAct.Quake);
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
            // Süs jest tetikleri kaldırıldı: retarget klipler Meshy rig'de pozu bozuyor.

            if (revisedFlow)
            {
                ShowRevisedIntroSubtitle(introIndex);
                introIndex++;
                if (introIndex < introInspections.Length)
                {
                    StartCoroutine(EnableIntroStepAfterDelay(1.35f));
                    return;
                }

                ui?.ShowObjective(
                    "EVDE SIRADAN BİR AN",
                    "Can çizimine geri döndü; radyo ve mutfak sesleri evin içinde sürüyor.");
                StartCoroutine(BeginQuakeAfterDelay(revisedQuakeDelayAfterFamilyMoment));
                return;
            }

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

        public void OnOptionalIntroRadioMoment()
        {
            if (!revisedFlow || phase != StorySlicePhase.CalmOpening || quakeStarted)
                return;

            ui?.ShowSubtitle(
                "Anne: Sofra birazdan hazır çocuklar.\nDeniz: Tamam anne, radyoyu kısıyorum.",
                5.5f);
        }

        public void OnOptionalIntroPlanMoment()
        {
            if (!revisedFlow || phase != StorySlicePhase.CalmOpening || quakeStarted)
                return;

            ui?.ShowSubtitle(
                "Can: Parkı turuncuya boyadım.\nDeniz: Yanına güneş de çizelim; kolay hatırlarız.",
                5.5f);
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
            if (cktProgress != 2)
            {
                OnWrongCktOrder("TUTUN en son gelir");
                return;
            }

            gameManager?.CommitCheckpoint(StoryCheckpoint.UnderCover);
            SetUnderCoverState();
            StartCoroutine(CompleteCoverSequence());
        }

        /// <summary>
        /// Çök-Kapan-Tutun bir sıra-eşleme mini oyunudur: üç adım da aynı anda
        /// ekrandadır, oyuncu doğru sırayı kendisi uygular. Yanlış adım seçilirse
        /// dizi başa sarar ve kısa bir sarsıntı + ipucu verilir.
        /// </summary>
        private void OnWrongCktOrder(string hint)
        {
            if (phase != StorySlicePhase.Quake || !quakeActive || retrying)
                return;

            cktProgress = 0;
            gameManager?.AddMistake();
            impulseSource?.GenerateImpulseWithForce(0.5f);
            FireStoryTrigger(denizAnimator, StoryResetHash);
            FireStoryTrigger(canAnimator, StoryResetHash);
            player?.SetNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            cameraController?.ActivateZone(StoryCameraZoneId.QuakeClose);
            ResetCktSteps();
            ArmSafetyDeadline(10f);
            ui?.ShowObjective(
                "SIRA KARIŞTI — BAŞTAN: ÇÖK → KAPAN → TUTUN",
                "Önce çömel, sonra başını koru, en son masa ayağına tutun.");
            ui?.ShowSubtitle("Deniz: Sıra önemli! " + hint + ". Baştan: önce ÇÖK!", 4f);
        }

        private void ResetCktSteps()
        {
            foreach (StoryInteractable step in new[] { crouchStep, coverHeadStep, safeCover })
            {
                if (step == null)
                    continue;
                step.ResetInteraction();
                step.SetAvailable(true);
            }
        }

        public void OnCrouchStep()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;
            if (cktProgress != 0)
            {
                OnWrongCktOrder("Çökme adımı en başta");
                return;
            }

            cktProgress = 1;
            FireStoryTrigger(denizAnimator, StoryCrouchHash);
            FireStoryTrigger(canAnimator, StoryCrouchHash);
            crouchStep?.SetAvailable(false);
            touchManager?.SetInteractionsEnabled(false);
            player?.SetNavigationEnabled(false);
            siblingFollower?.SetFollowing(false);
            cameraController?.ActivateZone(StoryCameraZoneId.UnderTable);
            ui?.ShowObjective("MASANIN ALTINA GEÇ", "Deniz ve Can birlikte çömelerek masanın altındaki ayrı koruma noktalarına giriyor.");
            ui?.ShowSubtitle("Deniz: Yanımdan ayrılma Can. Birlikte masanın altına geçiyoruz.", 4f);
            StartCoroutine(EnterUnderTablePose());
        }

        public void OnCoverHeadStep()
        {
            if (phase != StorySlicePhase.Quake || !quakeActive)
                return;
            if (cktProgress != 1)
            {
                OnWrongCktOrder("Başını korumadan önce çök");
                return;
            }

            cktProgress = 2;
            FireStoryTrigger(denizAnimator, StoryCoverHash);
            FireStoryTrigger(canAnimator, StoryCoverHash);
            coverHeadStep?.SetAvailable(false);
            safeCover?.SetAvailable(true);
            // The table-leg action is a deliberate long hold through the strongest shake.
            // Its deadline must not expire while the player is still holding correctly.
            ArmSafetyDeadline(12f);
            ui?.ShowObjective("TUTUN — DENGENİ KORU", "Sarsıntı kamerayı oynatırken masa ayağının üzerinde basılı tut; parmağını hedefte sabit tut.");
            ui?.ShowSubtitle("Deniz: Başını ve enseni koru Can. Ben de masa ayağına tutunuyorum.", 5f);
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
            // Cam sınırı okunduktan sonra yönetilen güvenli rota artık aynı tehlike trigger'ıyla
            // cezalandırılmamalı. Ayakkabıya yürüyüş başlamadan kapat; cam görseli sahnede kalır.
            if (revisedFlow && completedBeatIndex == Mathf.Max(0, revisedShoesBeatIndex - 1))
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
            bool hasFlashlight = HasFlag(StoryFlag.BagFlashlight);
            SetActive(playerFlashlight, hasFlashlight);
            SetActive(emergencyRouteLights, !hasFlashlight);
            if (revisedFlow)
                cameraController?.ActivateZone(StoryCameraZoneId.InspectExit);
            corridorExit?.SetAvailable(true);
            ui?.ShowObjective("KAPIYA GÜVENLE YAKLAŞ", "Sarsıntı durdu. Can yanında; çıkış yolunu acele etmeden izle.");
            ui?.ShowSubtitle(revisedFlow
                ? hasFlashlight
                    ? "Deniz: Fener çantada, buldum. Işığı yere tutayım.\nCan: Önümüzü görüyorum şimdi."
                    : "Deniz: Fenerimiz yok. Acil lambanın aydınlattığı taraftan, ağır ağır gidelim."
                : hasFlashlight
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
            ui?.ShowObjective(
                revisedFlow ? "CAN'IN OYUNUNA YARDIM ET" : "ODAYI OKU — 1/4",
                revisedFlow
                    ? "Masanın üzerindeki oyuncak tekerini doğrudan arabaya sürükle."
                    : "Can'la birlikte güvenli masayı incele.");
            ui?.ShowSubtitle(
                revisedFlow
                    ? "Can: Teker yine çıktı Deniz. Şunu takmama yardım eder misin?\nDeniz: Getir bakalım."
                    : "Sakin bir aile günü. Deniz ile Can salonda oyun oynuyor; evin sesleri her zamanki gibi.",
                7f);
            if (introInspections != null && introInspections.Length > 0)
                introInspections[0]?.SetAvailable(true);
            if (revisedFlow && introOptionalMoments != null)
                foreach (StoryInteractable optionalMoment in introOptionalMoments)
                    optionalMoment?.SetAvailable(true);
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
            if (revisedFlow)
            {
                string[] revisedDetails =
                {
                    "Masanın üzerindeki tekeri doğrudan oyuncak arabaya sürükle.",
                    "Oyuncak arabayı masadan Can'ın önündeki hedefe sür.",
                    "Radyonun sesini kendi düğmesi üzerinden biraz kıs.",
                    "Can'ın çizimindeki aile buluşma noktasını birlikte işaretleyin."
                };
                string[] revisedTitles =
                {
                    "CAN'IN OYUNUNA YARDIM ET",
                    "ARABAYI CAN'A GÖNDER",
                    "RADYONUN SESİNİ KIS",
                    "ÇİZİMİ BİRLİKTE TAMAMLA"
                };
                ui?.ShowObjective(
                    revisedTitles[Mathf.Min(introIndex, revisedTitles.Length - 1)],
                    revisedDetails[Mathf.Min(introIndex, revisedDetails.Length - 1)]);
                yield break;
            }

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
            // Can'ın çocuk rig'inde genel Hit_A retarget klibi sol ayağı ters çeviriyor.
            // Deprem tepkisi yüz/beden yönelimiyle okunur; taban pozu korunur.
            calmSibling?.SetAvailable(true);
            unsafeDoor?.SetAvailable(true);
            unsafeWindow?.SetAvailable(true);
            ui?.ShowObjective("SARSINTI BAŞLADI", "Koşma. Can'ın omzunda basılı tut; sonra yakındaki güvenli masaya geçin.");
            ui?.ShowSubtitle("Deniz: Can! Benimle kal. Pencereden uzak dur!", 6f);
            earthquakeTimeline?.Play();
            impactSource?.Play();
            StartCoroutine(ImpulseLoop());
            ArmSafetyDeadline(safetyDecisionSeconds);
        }

        private void SetSiblingCalmedState(bool commit)
        {
            if (commit)
                gameManager?.CommitCheckpoint(StoryCheckpoint.SiblingCalmed);
            siblingFollower?.SetFollowing(true);
            FaceSiblingsTowardsEachOther();
            calmSibling?.SetAvailable(false);
            // Çök-Kapan-Tutun mini oyunu: üç adım birden ekrandadır; oyuncu doğru
            // sırayı kendisi kurar, yanlış adım diziyi başa sarar.
            cktProgress = 0;
            ResetCktSteps();
            // Masaya giriş halkası seçilene kadar geniş iki-çocuk kadrajını koru. Alçak
            // masa-altı kamerasına erken geçmek gerçek hedefi ekran dışına atıyordu.
            cameraController?.ActivateZone(StoryCameraZoneId.QuakeClose);
            ArmSafetyDeadline(10f);
            ui?.ShowObjective(
                "SIRAYI SEN UYGULA: ÇÖK → KAPAN → TUTUN",
                "Üç adım da ekranda. Doğru sırayla uygula; yanlış adımı seçersen baştan başlarsın.");
            ui?.ShowSubtitle("Deniz: Benimle kal. Sırayı hatırla: önce ÇÖK, sonra KAPAN, sonra TUTUN!", 6f);
        }

        private void SetUnderCoverState()
        {
            SetChildrenAtCoverAnchors();
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
            ui?.ShowSubtitle("Deniz: Buradayım Can. Başını koru, masaya tutun. Sarsıntı bitene kadar kalkmıyoruz.", 8f);
        }

        private IEnumerator EnterUnderTablePose()
        {
            if (player == null || siblingFollower == null || denizCoverAnchor == null || canCoverAnchor == null)
            {
                FinishEnteringUnderTable();
                yield break;
            }

            Vector3 denizStart = player.transform.position;
            Vector3 canStart = siblingFollower.transform.position;
            Quaternion denizStartRotation = player.transform.rotation;
            Quaternion canStartRotation = siblingFollower.transform.rotation;
            const float duration = 1.05f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 0.5f - Mathf.Cos(t * Mathf.PI) * 0.5f;
                player.SetAuthoredPose(
                    Vector3.Lerp(denizStart, denizCoverAnchor.position, eased),
                    Quaternion.Slerp(denizStartRotation, denizCoverAnchor.rotation, eased));
                siblingFollower.SetAuthoredPose(
                    Vector3.Lerp(canStart, canCoverAnchor.position, eased),
                    Quaternion.Slerp(canStartRotation, canCoverAnchor.rotation, eased));
                yield return null;
            }

            SetChildrenAtCoverAnchors();
            FinishEnteringUnderTable();
        }

        private void FinishEnteringUnderTable()
        {
            touchManager?.SetInteractionsEnabled(true);
            coverHeadStep?.SetAvailable(true);
            ArmSafetyDeadline(7f);
            ui?.ShowObjective("KAPAN — BAŞINI KORU", "Deniz'in başında basılı tut; iki çocuk da başını ve ensesini kollarıyla kapatsın.");
            ui?.ShowSubtitle("Can: Seni görüyorum.\nDeniz: Ben de seni. Başımızı ve ensemizi koruyalım.", 5f);
        }

        private void SetChildrenAtCoverAnchors()
        {
            if (denizCoverAnchor != null)
                player?.SetAuthoredPose(denizCoverAnchor.position, denizCoverAnchor.rotation);
            if (canCoverAnchor != null)
                siblingFollower?.SetAuthoredPose(canCoverAnchor.position, canCoverAnchor.rotation);
        }

        private IEnumerator CompleteCoverSequence()
        {
            string[] shelterLines =
            {
                "Deniz: Biraz daha Can. Başını koru, hâlâ sallanıyor.",
                "Can: Dolaptan ses geldi!\nDeniz: Burada kalalım, birlikte nefes alalım.",
                "Can: Azaldı mı?\nDeniz: Azalıyor. Tamamen durmasını bekleyelim.",
                "Deniz: Daha kalkmıyoruz. Camdan ve dolaptan uzağız burada."
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
            Vector3 safeReturn = postQuakeSafeReturn != null
                ? postQuakeSafeReturn.position
                : player != null ? player.transform.position : Vector3.zero;
            player?.ReleaseAuthoredPose(safeReturn);
            siblingFollower?.ReleaseAuthoredPose(safeReturn + Vector3.right * 0.9f);
            ResetCoverPose(denizAnimator);
            ResetCoverPose(canAnimator);
            StartCoroutine(EnterPostQuakeRecoveryPose());
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
            ui?.ShowObjective(
                revisedFlow ? "SARSINTI DURDU — BİRBİRİNİZİ KONTROL EDİN" : "SARSINTI DURDU — BEKLE",
                revisedFlow
                    ? "Can'a doğrudan dokunup birkaç saniye yanında kal; sonra zemini okuyun."
                    : "Önce yeni bir hareket, düşen parça veya kırık cam sesi var mı dinle.");
            ui?.ShowSubtitle(
                revisedFlow
                    ? "Anne: Çocuklar, iyi misiniz? Ses verin bana!"
                    : "Anne (engelin arkasından): Çocuklar, iyi misiniz? Olduğunuz yerde birbirinizi kontrol edin!",
                8f);
            StartCoroutine(EnablePostQuakeBeatAfterDelay(postQuakeSettleDuration));
        }

        private static void ResetCoverPose(Animator animator)
        {
            if (animator == null)
                return;

            animator.SetFloat(SpeedHash, 0f);
            FireStoryTrigger(animator, StoryResetHash);
            // Hold Cover kalıcı bir state'tir. Dizzy tetikleyicisini aynı karede
            // göndermek reset geçişiyle yarışıyor ve Can'ın ayaklarını cover
            // pozunda ters bırakıyordu. Taban pozu aynı karede geri yüklenir;
            // kısa toparlanma hareketi bir sonraki adımda başlatılır.
            animator.Play(LocomotionHash, 0, 0f);
            animator.Update(0f);
        }

        private IEnumerator EnterPostQuakeRecoveryPose()
        {
            yield return new WaitForSeconds(0.16f);
            FireStoryTrigger(denizAnimator, StoryDizzyHash);
            FireStoryTrigger(canAnimator, StoryDizzyHash);
        }

        private IEnumerator EnablePostQuakeBeatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.PostQuake || postQuakeIndex >= (postQuakeBeats?.Length ?? 0))
                yield break;
            StoryAuthoredBeat beat = postQuakeBeats[postQuakeIndex];

            // Dinlemek fiziksel bir nesne etkileşimi değildir. Masanın içine gömülü görünmez bir
            // "basılı tut" yüzeyi istemek sahte oynanış üretiyordu. Bu ilk an çevre sesiyle kendi
            // süresini yaşar; oyuncunun ilk gerçek girdisi görünür Can'ı kontrol etmektir.
            if (revisedFlow && beat?.interactable != null &&
                beat.interactable.InteractionId == "quake.post.listen")
            {
                beat.interactable.SetAvailable(false);
                ui?.ShowObjective(
                    beat.objectiveTitle ?? "ÖNCE DİNLE",
                    ResolveConditionalText(beat.objectiveDetail));
                yield return new WaitForSeconds(Mathf.Max(2.8f, beat.delayAfter));
                if (phase == StorySlicePhase.PostQuake && postQuakeIndex == 0)
                    OnPostQuakeStep();
                yield break;
            }

            PresentCurrentPostQuakeBeat();
        }

        private void PresentCurrentPostQuakeBeat()
        {
            if (phase != StorySlicePhase.PostQuake || postQuakeIndex >= (postQuakeBeats?.Length ?? 0))
                return;

            StoryAuthoredBeat beat = postQuakeBeats[postQuakeIndex];
            if (revisedFlow && beat?.interactable != null &&
                beat.interactable.FocusCameraZone != StoryCameraZoneId.None)
                cameraController?.ActivateZone(beat.interactable.FocusCameraZone);
            beat?.interactable?.SetAvailable(true);
            ui?.ShowObjective(
                beat?.objectiveTitle ?? "ÇEVREYİ KONTROL ET",
                ResolveConditionalText(beat?.objectiveDetail));
        }

        private IEnumerator EnableLightChoiceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase != StorySlicePhase.PostQuake)
                yield break;
            bool hasFlashlight = HasFlag(StoryFlag.BagFlashlight);
            StoryInteractable choice = hasFlashlight ? lightWithFlashlight : lightWithoutFlashlight;
            if (revisedFlow && choice != null && choice.FocusCameraZone != StoryCameraZoneId.None)
                cameraController?.ActivateZone(choice.FocusCameraZone);
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
            ui?.ShowObjective(
                "KORİDOR EŞİĞİ",
                revisedFlow
                    ? "Can'ın elini tut; feneri zeminde gezdirerek güvenli adımları bul."
                    : "Can yanında mı kontrol et; zemini ve tavandan gelen sesleri dinle.");
            ui?.ShowSubtitle(
                revisedFlow
                    ? "Anne: Sesinizi duyuyorum çocuklar. Birlikte kalın.\nDeniz: Can, elin burada mı? Tamam, yanımdasın."
                    : "Koridor karanlık ve dar. Deniz, Can'ı önüne alıp acele etmeden ilerliyor.",
                7f);
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

            if (revisedFlow)
            {
                float duration = Mathf.Max(3f, corridorWarningDuration);
                ui?.ShowSubtitle("Can: Dur Deniz… Tavandan yine ses geliyor.", 4f);
                yield return new WaitForSeconds(Mathf.Min(4f, duration));
                ui?.ShowSubtitle("Deniz: Duydum. Yanımda kal, merdivene koşmayalım. Geçmesini bekleyelim.", 5f);
                yield return new WaitForSeconds(Mathf.Max(0f, duration - 4f));
                CompleteSlice();
                yield break;
            }

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

            CompleteSlice();
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
            SetActive(canComfortItem, afterQuake && HasFlag(StoryFlag.BagComfortItem));
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
            if (introOptionalMoments != null)
                foreach (StoryInteractable optionalMoment in introOptionalMoments)
                    optionalMoment?.SetAvailable(false);
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
                if (revisedFlow)
                {
                    ui?.ShowSubtitle("Can: Deniz, yanımda kal!\nDeniz: Buradayım. Birlikte masanın altına geçelim.", 4f);
                    yield break;
                }
                ui?.ShowSubtitle("Çok uzun bekledin; çevredeki eşya devrilmeden yakındaki güvenli harekete geç.", 3.5f);
                OnUnsafeChoice();
            }
        }

        private IEnumerator ReturnFromGlassHazard()
        {
            yield return new WaitForSeconds(0.55f);
            while (player != null && player.StoryInputLocked)
                yield return null;
            if (postQuakeSafeReturn != null)
            {
                bool moving = player != null && player.MoveTo(
                    postQuakeSafeReturn,
                    postQuakeSafeReturn.position + postQuakeSafeReturn.forward,
                    () => hazardRetrying = false);
                if (moving)
                    yield break;

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

        private void ShowRevisedIntroSubtitle(int index)
        {
            string[] subtitles =
            {
                "Can: Tam oturdu! Teker artık yerinden çıkmıyor.",
                "Can: Hah, şimdi gidiyor! Ben resmimi bitireyim.",
                "Anne: Çocuklar, sofrayı kuruyorum.\nDeniz: Tamam anne, radyonun sesini de kıstım.",
                "Can: Parkı turuncuya boyadım. Ayrılırsak burada buluşacağız, değil mi?\nDeniz: Evet, aile planımızdaki yerde."
            };
            ui?.ShowSubtitle(subtitles[Mathf.Clamp(index, 0, subtitles.Length - 1)], 5.5f);
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
                    : "İlk yardım paketi yok; kimse yaralı olmadığı için güvenli çıkışa devam edilebilir.")
                .Replace("{COMFORT}", HasFlag(StoryFlag.BagComfortItem)
                    ? "Can, hazırlıkta Deniz'in ona verdiği oyuncak arabayı göğsüne çekip nefesini düzenliyor."
                    : "Can, Deniz'in elini tutup nefesini onunla birlikte yavaşlatıyor.");
        }

        private bool HasFlag(StoryFlag flag)
        {
            return gameManager != null && gameManager.HasFlag(flag);
        }

        private void AnimatePostQuakeBeat(int beatIndex)
        {
            ApplyPostQuakePhysicalResult(beatIndex);
            if (revisedFlow)
            {
                if (beatIndex == 0)
                {
                    FaceSiblingsTowardsEachOther();
                    FireStoryTrigger(denizAnimator, StoryCallHash);
                    // Can stays on the recovered idle pose here. The generic Interact clip is
                    // retargeted incorrectly for this rig and turns the left foot upside-down.
                }
                else if (beatIndex == revisedShoesBeatIndex || beatIndex == revisedBagBeatIndex)
                    FireStoryTrigger(denizAnimator, StoryPickUpHash);
                else
                    FireStoryTrigger(denizAnimator, StoryInteractHash);
                return;
            }

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
            siblingFollower.FaceTowards(player.transform.position);
        }

        private void ApplyPostQuakePhysicalResult(int beatIndex)
        {
            if (revisedFlow)
            {
                if (beatIndex == revisedShoesBeatIndex)
                {
                    SetActive(leftShoeWorld, false);
                    SetActive(rightShoeWorld, false);
                    SetActive(denizWornShoes, true);
                }
                else if (beatIndex == revisedCanShoesBeatIndex)
                {
                    FaceSiblingsTowardsEachOther();
                    SetActive(canShoesWorld, false);
                    SetActive(canWornShoes, true);
                }
                else if (beatIndex == revisedBagBeatIndex)
                {
                    SetActive(emergencyBagWorld, false);
                    SetActive(denizWornBag, true);
                }
                return;
            }

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

            // Story states are authored as stationary actions. A stale follower/agent Speed value
            // can otherwise make one child miss the same-frame transition while the other enters it.
            animator.SetFloat(SpeedHash, 0f);

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

        private void CompleteSlice()
        {
            gameManager?.CompleteAct(StoryAct.Quake);
            phase = StorySlicePhase.Completed;
            ui?.ShowCompletion();
            Debug.Log($"Story_03_Quake completed in {ElapsedSliceSeconds:F1} seconds with {gameManager?.CurrentState.mistakeCount ?? 0} mistakes.", this);
        }
    }
}
