using System;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

namespace Deprem.Story
{
    public enum StoryEvacuationStage
    {
        Opening,
        CorridorCheck,
        RouteChoice,
        UpperStairs,
        Aftershock,
        LowerStairs,
        HelpingNeighbor,
        BuildingExit,
        FacadeClear,
        StreetRoute,
        AssemblyChecks,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed class StoryEvacuationDirector : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryGameManager gameManager;
        [SerializeField] private StoryPlayerMovement player;
        [SerializeField] private StoryTouchManager touchManager;
        [SerializeField] private StoryCameraController cameraController;
        [SerializeField] private StoryUIController ui;

        [Header("Flow Variant")]
        [Tooltip("Bağımsız Story 04 rebuild sahnesindeki fiziksel komşu yardımı, cepheden uzaklaşma, ekipman kullanımı ve aile buluşması akışını kullanır.")]
        [SerializeField] private bool revisedFlow;
        [SerializeField, Min(8f)] private float revisedAftershockMinimumDuration = 10f;

        [Header("Characters")]
        [SerializeField] private Transform deniz;
        [SerializeField] private Transform can;
        [SerializeField] private Transform neighbor;
        [SerializeField] private Animator denizAnimator;
        [SerializeField] private Animator canAnimator;
        [SerializeField] private Animator neighborAnimator;
        [SerializeField] private StorySiblingFollower canFollower;

        [Header("Prepared Variants")]
        [SerializeField] private GameObject flashlightBeam;
        [SerializeField] private GameObject emergencyLightRoute;

        [Header("Corridor And Route Choice")]
        [SerializeField] private StoryInteractable inspectCorridor;
        [SerializeField] private StoryInteractable chooseStairs;
        [SerializeField] private StoryInteractable tryElevator;
        [SerializeField] private Animation stairDoorAnimation;
        [SerializeField] private Animation elevatorNearMissAnimation;
        [SerializeField] private AudioSource elevatorAudio;
        [SerializeField] private GameObject stairDoorClosed;
        [SerializeField] private GameObject stairDoorOpen;

        [Header("Stair Route")]
        [SerializeField] private StoryInteractable reachUpperLanding;
        [SerializeField] private StoryInteractable holdHandrail;
        [SerializeField] private StoryInteractable reachLowerLanding;
        [SerializeField] private CinemachineImpulseSource aftershockImpulse;
        [SerializeField] private ParticleSystem aftershockDust;
        [SerializeField] private AudioSource aftershockAudio;

        [Header("Neighbor Help")]
        [SerializeField] private StoryInteractable callNeighbor;
        [SerializeField] private StoryInteractable moveNeighborCane;
        [SerializeField] private StoryInteractable clearLightDebris;
        [SerializeField] private StoryInteractable moveNeighborCardboard;
        [SerializeField] private StoryInteractable guideNeighbor;
        [SerializeField] private GameObject caneBlocked;
        [SerializeField] private GameObject caneReachable;
        [SerializeField] private GameObject lightDebrisBlocking;
        [SerializeField] private GameObject lightDebrisCleared;
        [SerializeField] private GameObject cardboardBlocking;
        [SerializeField] private GameObject cardboardCleared;
        [SerializeField] private GameObject neighborAtLanding;
        [SerializeField] private GameObject neighborAtStreet;
        [SerializeField] private GameObject neighborAtAssembly;
        [SerializeField] private Animation caneMoveAnimation;
        [SerializeField] private Animation debrisMoveAnimation;
        [SerializeField] private Animation cardboardMoveAnimation;
        [SerializeField] private Animation neighborRiseAnimation;

        [Header("Building Exit")]
        [SerializeField] private StoryInteractable openBuildingExit;
        [SerializeField] private StoryInteractable moveAwayFromFacade;
        [SerializeField] private Transform outsideStandPoint;
        [SerializeField] private GameObject buildingDoorClosed;
        [SerializeField] private GameObject buildingDoorOpen;
        [SerializeField] private Animation buildingDoorAnimation;

        [Header("Street Route")]
        [SerializeField] private StoryInteractable inspectStreetHazard;
        [SerializeField] private StoryInteractable takeSafeSidewalk;
        [SerializeField] private StoryInteractable tryUnsafeShortcut;
        [SerializeField] private Transform assemblyApproachPoint;
        [SerializeField] private Animation unsafeShortcutAnimation;
        [SerializeField] private ParticleSystem streetDust;
        [SerializeField] private Animation streetInspectSignAnimation;
        [SerializeField] private Animation streetInspectShardAnimation;
        [SerializeField] private AudioSource streetInspectCreak;

        [Header("Assembly Area")]
        [SerializeField] private StoryInteractable readAssemblySign;
        [SerializeField] private StoryInteractable checkCan;
        [SerializeField] private StoryInteractable checkNeighbor;
        [SerializeField] private StoryInteractable useWhistle;
        [SerializeField] private StoryInteractable callFamily;
        [SerializeField] private GameObject whistleWorld;
        [SerializeField] private GameObject voiceSignalWorld;

        [Header("Revised Assembly Area")]
        [SerializeField] private StoryInteractable handNeighborToWorker;
        [SerializeField] private StoryInteractable useRadio;
        [SerializeField] private StoryInteractable listenWorkerRadio;
        [SerializeField] private StoryInteractable useFirstAid;
        [SerializeField] private StoryInteractable useStationCloth;
        [SerializeField] private StoryInteractable giveWater;
        [SerializeField] private StoryInteractable useWaterStation;
        [SerializeField] private StoryInteractable giveBlanket;
        [SerializeField] private StoryInteractable moveToWindbreak;
        [SerializeField] private StoryInteractable useContactCard;
        [SerializeField] private StoryInteractable useRegistrySheet;
        [SerializeField] private StoryInteractable reuniteFamily;
        [SerializeField] private GameObject workerAtAssembly;
        [SerializeField] private GameObject radioPreparedWorld;
        [SerializeField] private GameObject radioFallbackWorld;
        [SerializeField] private Animation radioTuneAnimation;
        [SerializeField] private GameObject radioTunedIndicator;
        [SerializeField] private GameObject workerRadioIndicator;
        [SerializeField] private AudioSource radioPreparedBroadcast;
        [SerializeField] private AudioSource workerRadioBroadcast;
        [SerializeField] private GameObject firstAidPreparedWorld;
        [SerializeField] private GameObject firstAidFallbackWorld;
        [SerializeField] private GameObject waterPreparedWorld;
        [SerializeField] private GameObject waterFallbackWorld;
        [SerializeField] private GameObject blanketPreparedWorld;
        [SerializeField] private GameObject blanketFallbackWorld;
        [SerializeField] private GameObject contactPreparedWorld;
        [SerializeField] private GameObject contactFallbackWorld;
        [SerializeField] private GameObject comfortToyAtAssembly;
        [SerializeField] private GameObject motherAtAssembly;
        [SerializeField] private GameObject fatherAtAssembly;
        [SerializeField] private Animation motherApproachAnimation;
        [SerializeField] private Animation fatherApproachAnimation;
        [SerializeField] private GameObject familyHeadcountPendingWorld;
        [SerializeField] private GameObject familyHeadcountCompleteWorld;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionDetail;

        private readonly bool[] neighborTasks = new bool[3];
        private readonly bool[] assemblyChecks = new bool[4];
        private StoryEvacuationStage stage;
        private float revisedAftershockStartedAt;
        private bool revisedAftershockCompletionQueued;
        private int revisedAssemblyIndex;

        private static readonly int InspectTrigger = Animator.StringToHash("StoryInspect");
        private static readonly int InteractTrigger = Animator.StringToHash("StoryInteract");
        private static readonly int PickUpTrigger = Animator.StringToHash("StoryPickUp");
        private static readonly int CallTrigger = Animator.StringToHash("StoryCall");

        public StoryEvacuationStage Stage => stage;
        public bool RevisedFlow => revisedFlow;
        public int NeighborTasksCompleted => revisedFlow
            ? neighborTasks.Count(value => value)
            : neighborTasks.Take(2).Count(value => value);
        public int AssemblyChecksCompleted => revisedFlow
            ? revisedAssemblyIndex
            : assemblyChecks.Count(value => value);
        private int RequiredNeighborTasks => revisedFlow ? 3 : 2;

        private void Start()
        {
            if (StoryGameManager.Instance != null)
                gameManager = StoryGameManager.Instance;
            gameManager?.BeginAct(StoryAct.Evacuation);
            DisableAllInteractions();
            RestoreWorldState();
            if (completionPanel != null)
                completionPanel.SetActive(false);

            StoryCheckpoint checkpoint = gameManager?.CurrentState?.checkpoint ?? StoryCheckpoint.None;
            switch (checkpoint)
            {
                case StoryCheckpoint.AssemblyHeadcountComplete:
                    ShowCompletedState();
                    break;
                case StoryCheckpoint.AssemblyAreaReached:
                case StoryCheckpoint.StreetRouteCleared:
                    BeginAssemblyChecks();
                    break;
                case StoryCheckpoint.BuildingExited:
                    BeginStreetRoute();
                    break;
                case StoryCheckpoint.NeighborHelped:
                    BeginBuildingExit();
                    break;
                case StoryCheckpoint.AftershockHeld:
                    BeginLowerStairRoute();
                    break;
                case StoryCheckpoint.StairwellEntered:
                    BeginUpperStairRoute();
                    break;
                default:
                    StartOpening();
                    break;
            }
        }

        public void InspectCorridor()
        {
            if (stage != StoryEvacuationStage.CorridorCheck)
                return;

            inspectCorridor?.SetAvailable(false);
            denizAnimator?.SetTrigger(InspectTrigger);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationCorridor);
            ShowDialogue(
                "Can: Tavan sustu.  Deniz: Merdiven yolu açık; asansörün paneli karanlık. Önce kapıyı kontrol edelim.",
                4.8f, BeginRouteChoice);
        }

        public void ChooseStairs()
        {
            if (stage != StoryEvacuationStage.RouteChoice)
                return;

            chooseStairs?.SetAvailable(false);
            tryElevator?.SetAvailable(false);
            gameManager?.SetFlag(StoryFlag.ElevatorAvoided, true);
            gameManager?.CommitCheckpoint(StoryCheckpoint.StairwellEntered);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationStairsTop);
            SetActive(stairDoorClosed, false);
            SetActive(stairDoorOpen, true);
            Play(stairDoorAnimation);
            denizAnimator?.SetTrigger(InteractTrigger);
            ShowDialogue(
                "Deniz merdiven kapısını kontrollü açtı. Asansör kullanılmadı; Can korkuluğun iç tarafında ve Deniz'in hemen arkasında kaldı.",
                7f, BeginUpperStairRoute);
        }

        public void TryElevator()
        {
            if (stage != StoryEvacuationStage.RouteChoice)
                return;

            tryElevator?.SetAvailable(false);
            gameManager?.AddMistake();
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationElevator);
            Play(elevatorNearMissAnimation);
            elevatorAudio?.Play();
            ShowDialogue(
                "Düğme bir an yandı, sonra koridor ışığı söndü. Deprem sonrasında elektrik kesilebilir ve asansör katlar arasında kalabilir; güvenli rota merdiven.",
                7.2f, () =>
                {
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationCorridor);
                    chooseStairs?.SetAvailable(true);
                    ui?.ShowObjective("MERDİVEN KAPISINI AÇ", "Asansörü bırak; merdiven kapısının kolunu doğrudan yana çek.");
                });
        }

        public void ReachUpperLanding()
        {
            if (stage != StoryEvacuationStage.UpperStairs)
                return;

            reachUpperLanding?.SetAvailable(false);
            stage = StoryEvacuationStage.Aftershock;
            revisedAftershockStartedAt = Time.unscaledTime;
            revisedAftershockCompletionQueued = false;
            canFollower?.SetFollowing(false);
            touchManager?.SetWorldNavigationEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationLanding);
            aftershockImpulse?.GenerateImpulse();
            aftershockDust?.Play();
            aftershockAudio?.Play();
            ui?.ShowObjective("ARTÇI — KORKULUĞA TUTUN", "Koşma veya basamak değiştirme; korkuluk üzerinde basılı tut.");
            ShowDialogue(
                "Merdiven boşluğundan ince bir titreşim geçti. Deniz bulunduğu basamakta kaldı, Can'a çömelmesini söyledi ve korkuluğa uzandı.",
                6.5f, () => holdHandrail?.SetAvailable(true));
        }

        public void HoldHandrail()
        {
            if (stage != StoryEvacuationStage.Aftershock)
                return;

            holdHandrail?.SetAvailable(false);
            denizAnimator?.SetTrigger(InteractTrigger);
            if (revisedFlow)
            {
                if (revisedAftershockCompletionQueued)
                    return;

                revisedAftershockCompletionQueued = true;
                StartCoroutine(FinishRevisedAftershockAfterMinimumDuration());
                return;
            }

            FinishAftershock();
        }

        private IEnumerator FinishRevisedAftershockAfterMinimumDuration()
        {
            float finishAt = revisedAftershockStartedAt + revisedAftershockMinimumDuration;
            while (stage == StoryEvacuationStage.Aftershock && Time.unscaledTime < finishAt)
                yield return null;

            if (stage == StoryEvacuationStage.Aftershock)
                FinishAftershock();
        }

        private void FinishAftershock()
        {
            aftershockAudio?.Stop();
            aftershockDust?.Stop();
            gameManager?.CommitCheckpoint(StoryCheckpoint.AftershockHeld);
            ShowDialogue(
                "Artçı sona erdi. Deniz korkuluğu bırakmadan önce birkaç saniye dinledi; basamaklarda yeni bir kırık olmadığını gördü.",
                6.5f, BeginLowerStairRoute);
        }

        public void ReachLowerLanding()
        {
            if (stage != StoryEvacuationStage.LowerStairs)
                return;

            reachLowerLanding?.SetAvailable(false);
            stage = StoryEvacuationStage.HelpingNeighbor;
            canFollower?.SetFollowing(false);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationNeighbor);
            callNeighbor?.SetAvailable(true);
            ui?.ShowObjective("KOMŞUYA SESLEN", "Komşuya yaklaş; ağır parçayı kaldırmadan önce iyi olup olmadığını sor.");
        }

        public void CallNeighbor()
        {
            if (stage != StoryEvacuationStage.HelpingNeighbor)
                return;

            callNeighbor?.SetAvailable(false);
            denizAnimator?.SetTrigger(CallTrigger);
            neighborAnimator?.SetTrigger(CallTrigger);
            ShowDialogue(
                "Deniz: Nermin teyze, iyi misiniz?\nNermin: İyiyim; bastonum kutunun arkasında kaldı. Ağır dolaba dokunmayın, yalnızca hafif parçaları kenara alın.",
                7f, () =>
                {
                    if (revisedFlow)
                    {
                        moveNeighborCardboard?.SetAvailable(!neighborTasks[2]);
                        ui?.ShowObjective(
                            "KUTUYU GEÇİŞTEN AL",
                            "Hafif karton kutuyu doğrudan boş duvar kenarına sürükle; ağır dolaba dokunma.");
                        return;
                    }

                    moveNeighborCane?.SetAvailable(!neighborTasks[0]);
                    clearLightDebris?.SetAvailable(!neighborTasks[1]);
                    UpdateNeighborObjective();
                });
        }

        public void MoveNeighborCardboard()
        {
            CompleteNeighborTask(2, moveNeighborCardboard, cardboardMoveAnimation, cardboardBlocking, cardboardCleared,
                "Deniz hafif karton kutuyu kaldırmadan, zeminde boş duvar kenarına sürükledi. Nermin teyzenin dizlerinin önü açıldı.");
        }

        public void MoveNeighborCane()
        {
            CompleteNeighborTask(0, moveNeighborCane, caneMoveAnimation, caneBlocked, caneReachable,
                "Baston hafif kutunun arkasından çekilip Nermin teyzenin uzanabileceği yere bırakıldı.");
        }

        public void ClearLightDebris()
        {
            CompleteNeighborTask(1, clearLightDebris, debrisMoveAnimation, lightDebrisBlocking, lightDebrisCleared,
                "Yürüyüş yolundaki hafif köpük parça kenara itildi; ağır dolap ve duvar parçasına dokunulmadı.");
        }

        public void GuideNeighbor()
        {
            if (stage != StoryEvacuationStage.HelpingNeighbor || NeighborTasksCompleted < RequiredNeighborTasks)
                return;

            guideNeighbor?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationNeighbor);
            denizAnimator?.SetTrigger(InteractTrigger);
            neighborAnimator?.SetTrigger(InteractTrigger);
            Play(neighborRiseAnimation);
            SetActive(neighborAtLanding, false);
            gameManager?.SetFlag(StoryFlag.NeighborAssisted, true);
            gameManager?.CommitCheckpoint(StoryCheckpoint.NeighborHelped);
            ShowDialogue(
                "Deniz Nermin teyzenin kolunu çekmedi; bastonunu kavramasını bekleyip yanında yürüdü. Can merdivenin iç tarafında yolu açık tuttu.",
                7.2f, BeginBuildingExit);
        }

        public void OpenBuildingExit()
        {
            if (stage != StoryEvacuationStage.BuildingExit)
                return;

            openBuildingExit?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            SetActive(buildingDoorClosed, false);
            SetActive(buildingDoorOpen, true);
            Play(buildingDoorAnimation);
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            canFollower?.SetFollowing(true);
            StartCoroutine(BeginBuildingExitTraversal());
        }

        private IEnumerator BeginBuildingExitTraversal()
        {
            // Carving obstacle devre dışı kaldıktan sonra NavMesh güncellemesi bir sonraki
            // pre-update'e sarkabilir. Aynı karede rota istersek kapı görsel olarak açılıp
            // yürüyüş reddedilir ve hikâye yanlışlıkla dışarı çıkılmış sayılır.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                yield return null;
                if (stage != StoryEvacuationStage.BuildingExit)
                    yield break;

                bool moving = player != null && outsideStandPoint != null &&
                              player.MoveTo(outsideStandPoint,
                                  buildingDoorOpen != null
                                      ? buildingDoorOpen.transform.position
                                      : outsideStandPoint.position,
                                  FinishBuildingExit);
                if (moving)
                    yield break;
            }

            // Authored NavMesh beklenmedik biçimde güncellenemezse oyuncuyu kapalı akışta
            // bırakma. Bu yalnızca son güvenlik ağıdır; normal akış yukarıdaki rota ile yürür.
            if (player != null && outsideStandPoint != null)
                player.Warp(outsideStandPoint.position);
            FinishBuildingExit();
        }

        public void InspectStreetHazard()
        {
            if (stage != StoryEvacuationStage.StreetRoute)
                return;

            inspectStreetHazard?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationStreetInspect);
            denizAnimator?.SetTrigger(InspectTrigger);
            Play(streetInspectSignAnimation);
            Play(streetInspectShardAnimation);
            streetDust?.Play();
            streetInspectCreak?.Play();
            ShowDialogue(
                "Tabela gıcırdayıp bir cam parçası kaldırım sınırına kaydı. Can: Cam burada uzuyor. Deniz: Açık yan kaldırım sağda; araç yoluna çıkmadan oradan gidiyoruz.",
                5.8f, () =>
                {
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationStreet);
                    takeSafeSidewalk?.SetAvailable(true);
                    ui?.ShowObjective(
                        "AÇIK YAN KALDIRIMDAN İLERLE",
                        "Can'ın gösterdiği, camdan ve bina cephesinden uzak yan kaldırıma dokun.");
                });
        }

        public void TryUnsafeShortcut()
        {
            if (stage != StoryEvacuationStage.StreetRoute)
                return;

            tryUnsafeShortcut?.SetAvailable(false);
            gameManager?.AddMistake();
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationHazard);
            Play(unsafeShortcutAnimation);
            streetDust?.Play();
            player?.Stop();
            if (outsideStandPoint != null)
                player?.Warp(outsideStandPoint.position);
            ShowDialogue(
                "Gevşek tabela sallandı ve camlı bölüm kapandı. Kimse yaralanmadı; Deniz bina cephesinden ve düşebilecek parçalardan uzak açık kaldırımla yeniden deneyecek.",
                7f, () =>
                {
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
                    inspectStreetHazard?.SetAvailable(true);
                });
        }

        public void TakeSafeSidewalk()
        {
            if (stage != StoryEvacuationStage.StreetRoute)
                return;

            takeSafeSidewalk?.SetAvailable(false);
            tryUnsafeShortcut?.SetAvailable(false);
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            canFollower?.SetFollowing(true);
            StartCoroutine(BeginSafeStreetTraversal());
        }

        private IEnumerator BeginSafeStreetTraversal()
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                yield return null;
                if (stage != StoryEvacuationStage.StreetRoute)
                    yield break;

                bool moving = player != null && assemblyApproachPoint != null &&
                              player.MoveTo(assemblyApproachPoint, FinishSafeStreetRoute);
                if (moving)
                    yield break;
            }

            if (player != null && assemblyApproachPoint != null)
                player.Warp(assemblyApproachPoint.position);
            FinishSafeStreetRoute();
        }

        public void ReadAssemblySign()
        {
            if (revisedFlow)
            {
                return;
            }

            RegisterAssemblyCheck(0, readAssemblySign,
                "Toplanma alanı levhasındaki mahalle adı aile planındaki yerle eşleşiyor. Yol, acil araç girişini kapatmıyor.");
        }

        public void CheckCan()
        {
            if (revisedFlow)
            {
                return;
            }

            RegisterAssemblyCheck(1, checkCan,
                "Deniz Can'ın yanında olduğunu, ayakkabılarının bağlı kaldığını ve yeni bir yaralanması olmadığını kontrol etti.");
        }

        public void CheckNeighbor()
        {
            if (revisedFlow)
            {
                HandNeighborToWorker();
                return;
            }

            RegisterAssemblyCheck(2, checkNeighbor,
                "Nermin teyze bastonuyla güvenli alana ulaştı. Deniz onu görevliye gösterdi; tek başına sağlık müdahalesi yapmadı.");
        }

        public void UseWhistle()
        {
            if (revisedFlow)
            {
                CompleteRevisedAssemblyStep(
                    2,
                    useWhistle,
                    "Deniz düdüğü üç kısa aralıkla kullandı. Can sesi tüketmeden aynı noktada bekledi. Kalabalığın öte yanında Anne aynı aile işaretini duydu.",
                    CallTrigger);
                return;
            }

            CompleteFamilySignal(useWhistle,
                "Deniz düdüğü kısa aralıklarla kullandı. Aile, kalabalıkta sesini tüketmeden planlanan işareti duydu.");
        }

        public void CallFamily()
        {
            if (revisedFlow)
            {
                CompleteRevisedAssemblyStep(
                    2,
                    callFamily,
                    "Çantada düdük yoktu. Deniz planlanan aile adını kısa aralıklarla seslendi; Can sarı simgeyi kaldırdı ve ikisi yerinden ayrılmadı. Baba karşılık verdi.",
                    CallTrigger);
                return;
            }

            CompleteFamilySignal(callFamily,
                "Çantada düdük yoktu. Deniz belirlenen aile adını yüksek ve kısa aralıklarla seslenerek aynı buluşma noktasında kaldı.");
        }

        public void HandNeighborToWorker()
        {
            neighborAnimator?.SetTrigger(CallTrigger);
            CompleteRevisedAssemblyStep(
                0,
                handNeighborToWorker != null ? handNeighborToWorker : checkNeighbor,
                HasFlag(StoryFlag.BagWater)
                    ? "Nermin teyze görevliye kendi durumunu anlattı. Deniz yanında kaldı; hazırladıkları kapalı su şişesi masada hazırdı. Görevli bastonu Nermin'in elinde bırakıp oturma yerini gösterdi."
                    : "Nermin teyze görevliye kendi durumunu anlattı. Deniz yanında kaldı; görevli dağıtım masasındaki kapalı bardağı ve oturma yerini gösterdi. Çocuklar yetişkin müdahalesini üstlenmedi.",
                InteractTrigger);
        }

        public void UseRadio()
        {
            Play(radioTuneAnimation);
            SetActive(radioTunedIndicator, true);
            radioPreparedBroadcast?.Play();
            CompleteRevisedAssemblyStep(
                3,
                useRadio,
                "Deniz düğmeyi parazit azalıncaya kadar çevirdi; turkuaz ibre sabitlendi ve düşük sesli resmî yayın radyodan gerçekten başladı. Yayında artçı riski ve kıyıdan uzak durma uyarısı vardı.",
                InspectTrigger);
        }

        public void ListenWorkerRadio()
        {
            SetActive(workerRadioIndicator, true);
            workerRadioBroadcast?.Play();
            CompleteRevisedAssemblyStep(
                3,
                listenWorkerRadio,
                "Çantada radyo yoktu. Görevlinin hoparlöründeki turkuaz ışık yandı; Deniz yanında kaldı ve aynı düşük sesli resmî artçı uyarısını dinledi.",
                InspectTrigger);
        }

        public void UseFirstAid()
        {
            canAnimator?.SetTrigger(InteractTrigger);
            CompleteRevisedAssemblyStep(
                1,
                useFirstAid,
                BuildCanCareSubtitle(true),
                PickUpTrigger);
        }

        public void UseStationCloth()
        {
            canAnimator?.SetTrigger(InteractTrigger);
            CompleteRevisedAssemblyStep(
                1,
                useStationCloth,
                BuildCanCareSubtitle(false),
                PickUpTrigger);
        }

        public void GiveWater()
        {
            CompleteRevisedAssemblyStep(
                5,
                giveWater,
                "Deniz şişeyi Nermin teyzeye uzattı. Nermin teyze oturup birkaç yudum aldı; şişe ortak alana bırakılmadı.",
                PickUpTrigger);
        }

        public void UseWaterStation()
        {
            CompleteRevisedAssemblyStep(
                5,
                useWaterStation,
                "Çantada su yoktu. Görevli kapalı bir bardak verdi; Deniz sırayı ve dağıtım masasını kapatmadan Nermin teyzeye ulaştırdı.",
                PickUpTrigger);
        }

        public void GiveBlanket()
        {
            CompleteRevisedAssemblyStep(
                6,
                giveBlanket,
                "Deniz ince battaniyeyi Can'ın omuzlarına örttü. Can nefesini toparladı ve kalabalığı yeniden dinlemeye başladı.",
                InteractTrigger);
        }

        public void MoveToWindbreak()
        {
            CompleteRevisedAssemblyStep(
                6,
                moveToWindbreak,
                "Çantada battaniye yoktu. Deniz Can'ı görevlinin gösterdiği rüzgâr kesen tentenin içine götürdü.",
                InteractTrigger);
        }

        public void UseContactCard()
        {
            CompleteRevisedAssemblyStep(
                7,
                useContactCard,
                "Aile iletişim kartındaki isim ve buluşma noktası kayıt panosuna aktarıldı. Deniz kartı görevliye bırakmadan geri aldı.",
                InspectTrigger);
        }

        public void UseRegistrySheet()
        {
            CompleteRevisedAssemblyStep(
                7,
                useRegistrySheet,
                "Kart yoktu. Deniz aile adını ve çocuk sayısını görevlinin kayıt sayfasına yazdı; alandan ayrılıp ailesini aramadı.",
                InspectTrigger);
        }

        public void ReuniteFamily()
        {
            if (!revisedFlow ||
                stage != StoryEvacuationStage.AssemblyChecks ||
                revisedAssemblyIndex != 3)
                return;

            SetActive(familyHeadcountPendingWorld, false);
            SetActive(familyHeadcountCompleteWorld, true);
            canAnimator?.SetTrigger(InteractTrigger);
            CompleteRevisedAssemblyStep(
                3,
                reuniteFamily,
                "Deniz Anne'ye doğru yürüdü; Can hemen yanında kaldı. Anne Can'ın göz hizasına indi, Baba Deniz'in omzuna dokundu ve görevli dört kişiyi aynı noktada gördü.",
                InteractTrigger);
        }

        private void StartOpening()
        {
            stage = StoryEvacuationStage.Opening;
            gameManager?.CommitCheckpoint(StoryCheckpoint.EvacuationStart);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationCorridor, true);
            canFollower?.SetFollowing(true);
            ui?.ShowObjective("KORİDORU DİNLE", "Sarsıntı durdu; kapı eşiğine yaklaşarak yeni düşme sesi ve açık rota kontrolü yap.");
            ShowDialogue(
                "Koridordaki ince titreşim durdu. Engel arkasından Anne'nin sesi geldi: “Birlikte kalın, merdiveni kullanın; aşağıda buluşacağız.”",
                7.2f, () =>
                {
                    stage = StoryEvacuationStage.CorridorCheck;
                    inspectCorridor?.SetAvailable(true);
                });
        }

        private void BeginRouteChoice()
        {
            stage = StoryEvacuationStage.RouteChoice;
            chooseStairs?.SetAvailable(true);
            tryElevator?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationCorridor);
            ui?.ShowObjective(
                "MERDİVEN KAPISINI KONTROL ET",
                "Asansör paneli karanlık; merdiven kapısının gerçek kolunu yana çek.");
        }

        private void BeginUpperStairRoute()
        {
            stage = StoryEvacuationStage.UpperStairs;
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            canFollower?.SetFollowing(true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationStairsTop);
            reachUpperLanding?.SetAvailable(true);
            ui?.ShowObjective("İLK SAHANLIĞA İN", "Basamakların iç tarafındaki aydınlık sahanlığa dokun; koşmadan Can'la birlikte ilerle.");
        }

        private void BeginLowerStairRoute()
        {
            stage = StoryEvacuationStage.LowerStairs;
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            canFollower?.SetFollowing(true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationLowerLanding);
            reachLowerLanding?.SetAvailable(true);
            ui?.ShowObjective("ALT SAHANLIĞA DEVAM ET", "Artçı bitti; korkuluğun iç tarafından alt sahanlığa kontrollü ilerle.");
        }

        private void CompleteNeighborTask(int index, StoryInteractable interactable, Animation animation,
            GameObject blocked, GameObject cleared, string subtitle)
        {
            if (stage != StoryEvacuationStage.HelpingNeighbor ||
                index < 0 ||
                index >= RequiredNeighborTasks ||
                neighborTasks[index])
                return;

            neighborTasks[index] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            Play(animation);
            SetActive(blocked, false);
            SetActive(cleared, true);
            ShowDialogue(subtitle, 5.5f, () =>
            {
                if (revisedFlow)
                {
                    if (index == 2)
                    {
                        clearLightDebris?.SetAvailable(!neighborTasks[1]);
                        ui?.ShowObjective(
                            "KÖPÜK PARÇAYI KENARA AL",
                            "Yumuşak köpük parçasını duvar dibindeki boş alana sürükle.");
                        return;
                    }

                    if (index == 1)
                    {
                        moveNeighborCane?.SetAvailable(!neighborTasks[0]);
                        ui?.ShowObjective(
                            "BASTONU NERMİN TEYZEYE ULAŞTIR",
                            "Bastonu sapından tutup elinin yanındaki boş alana sürükle.");
                        return;
                    }
                }

                if (NeighborTasksCompleted >= RequiredNeighborTasks)
                {
                    guideNeighbor?.SetAvailable(true);
                    ui?.ShowObjective("NERMİN TEYZENİN YANINDA KAL", "Bastonunu kendisi kavrasın; kolunu çekmeden yanında basılı tut.");
                    return;
                }
                UpdateNeighborObjective();
            });
        }

        private void UpdateNeighborObjective()
        {
            if (revisedFlow)
            {
                ui?.ShowObjective(
                    "NİNEYE GÜVENLİ GEÇİŞ AÇ",
                    "Kutuyu, hafif köpüğü ve bastonu sahnedeki gerçek hedeflerine taşı; ağır dolaba dokunma.");
                return;
            }

            ui?.ShowObjective(
                $"HAFİF ENGELLERİ KENARA AL — {NeighborTasksCompleted}/2",
                "Bastonu erişime çek ve yalnızca hafif köpük parçayı kenara it; ağır dolaba dokunma.");
        }

        private void BeginBuildingExit()
        {
            stage = StoryEvacuationStage.BuildingExit;
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            canFollower?.SetFollowing(true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            openBuildingExit?.SetAvailable(true);
            ui?.ShowObjective("BİNA ÇIKIŞINI KONTROLLÜ AÇ", "Dış kapı kolunu yana çek; önce açık alanı gör, sonra birlikte dışarı çık.");
        }

        private void FinishBuildingExit()
        {
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            gameManager?.SetFlag(StoryFlag.StairRouteCompleted, true);
            SetActive(neighborAtLanding, false);
            SetActive(neighborAtStreet, true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            if (revisedFlow)
            {
                stage = StoryEvacuationStage.FacadeClear;
                moveAwayFromFacade?.SetAvailable(true);
                ui?.ShowObjective(
                    "BİNA CEPHESİNDEN UZAKLAŞ",
                    "Kapı önünde bekleme; üç kişiyi açık kaldırım noktasına götür.");
                ShowDialogue(
                    "Dışarı çıktılar ama henüz güvende değillerdi. Deniz yukarıdaki kırık camı gördü ve kapı önünde durmadı.",
                    4.8f);
                return;
            }

            gameManager?.CommitCheckpoint(StoryCheckpoint.BuildingExited);
            ShowDialogue(
                "Üçü bina cephesinden uzak açık noktaya çıktı. Deniz girişin önünde beklemedi; artçıların hasarlı cepheden parça düşürebileceğini hatırladı.",
                6.8f, BeginStreetRoute);
        }

        public void MoveAwayFromFacade()
        {
            if (stage != StoryEvacuationStage.FacadeClear)
                return;

            moveAwayFromFacade?.SetAvailable(false);
            gameManager?.CommitCheckpoint(StoryCheckpoint.BuildingExited);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            ShowDialogue(
                "Deniz, Can ve Nermin teyze bina yüksekliğinden uzak açık noktaya geçti. Şimdi toplanma alanına giden sokağı okuyabilirler.",
                5.5f, BeginStreetRoute);
        }

        private void BeginStreetRoute()
        {
            stage = StoryEvacuationStage.StreetRoute;
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            inspectStreetHazard?.SetAvailable(true);
            tryUnsafeShortcut?.SetAvailable(true);
            ui?.ShowObjective("SOKAĞI OKU", "Camlı kestirme ile açık yan kaldırımın fiziksel durumunu incele.");
        }

        private void FinishSafeStreetRoute()
        {
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            gameManager?.CommitCheckpoint(StoryCheckpoint.StreetRouteCleared);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationAssembly);
            SetActive(neighborAtStreet, false);
            SetActive(neighborAtAssembly, true);
            ShowDialogue(
                "Deniz araç yoluna çıkmadan ve bina cephelerinden uzak kalarak toplanma levhasına ulaştı. Can ve Nermin teyze yanındaydı.",
                6.5f, BeginAssemblyChecks);
        }

        private void BeginAssemblyChecks()
        {
            stage = StoryEvacuationStage.AssemblyChecks;
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationAssembly);
            SetActive(neighborAtLanding, false);
            SetActive(neighborAtStreet, false);
            SetActive(neighborAtAssembly, true);
            if (revisedFlow)
            {
                gameManager?.CommitCheckpoint(StoryCheckpoint.AssemblyAreaReached);
                revisedAssemblyIndex = 0;
                BeginRevisedAssemblyArrival();
                return;
            }

            readAssemblySign?.SetAvailable(!assemblyChecks[0]);
            checkCan?.SetAvailable(!assemblyChecks[1]);
            checkNeighbor?.SetAvailable(!assemblyChecks[2]);
            bool hasWhistle = gameManager != null && gameManager.HasFlag(StoryFlag.BagWhistle);
            useWhistle?.SetAvailable(hasWhistle && !assemblyChecks[3]);
            callFamily?.SetAvailable(!hasWhistle && !assemblyChecks[3]);
            SetActive(whistleWorld, hasWhistle);
            SetActive(voiceSignalWorld, !hasWhistle);
            UpdateAssemblyObjective();
        }

        private void CompleteRevisedAssemblyStep(
            int expectedIndex,
            StoryInteractable interactable,
            string subtitle,
            int animationTrigger)
        {
            if (!revisedFlow ||
                stage != StoryEvacuationStage.AssemblyChecks ||
                revisedAssemblyIndex != expectedIndex)
                return;

            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(animationTrigger);
            ShowDialogue(subtitle, 5.5f, () =>
            {
                revisedAssemblyIndex++;
                ActivateRevisedAssemblyStep();
            });
        }

        private void BeginRevisedAssemblyArrival()
        {
            bool hasRadio = HasFlag(StoryFlag.BagRadio);
            if (hasRadio)
            {
                Play(radioTuneAnimation);
                SetActive(radioTunedIndicator, true);
                radioPreparedBroadcast?.Play();
            }
            else
            {
                SetActive(workerRadioIndicator, true);
                workerRadioBroadcast?.Play();
            }

            bool hasComfortItem = HasFlag(StoryFlag.BagComfortItem);
            SetActive(comfortToyAtAssembly, hasComfortItem);
            canAnimator?.SetTrigger(InspectTrigger);
            ShowDialogue(
                hasRadio
                    ? "Can levhadaki sarı simgeyi aile planındaki işaretle eşleştirdi. Görevli üç kişiyi gördü; çantadaki radyo ayarı yakalayıp resmî artçı duyurusunu sahnenin içinden vermeye başladı."
                    : "Can levhadaki sarı simgeyi aile planındaki işaretle eşleştirdi. Görevli üç kişiyi gördü; çantada radyo olmadığı için masadaki hoparlörün ışığı yanıp aynı resmî artçı duyurusunu verdi.",
                6.2f,
                ActivateRevisedAssemblyStep);
        }

        private string BuildCanCareSubtitle(bool usedPreparedSet)
        {
            string treatment = usedPreparedSet
                ? "Deniz ilk yardım setini doğrudan tedavi tepsisine sürükledi; Can'ın elini yetişkin görevli temizleyip kapattı."
                : "Çantada ilk yardım seti yoktu. Deniz kapalı temiz bezi doğrudan tedavi tepsisine sürükledi; uygulamayı yetişkin görevli yaptı.";
            string comfort = HasFlag(StoryFlag.BagComfortItem)
                ? " Can küçük arabayı diğer elinde tuttu: “Evde açtığımız yolu hatırlattı.”"
                : " Deniz izin isteyip Can'ın boşta kalan elini tuttu; ikisi birlikte nefesini yavaşlattı.";
            string warmth = HasFlag(StoryFlag.BagBlanket)
                ? " Hazırladıkları battaniye Can'ın yanında hazırdı."
                : " Görevli onları rüzgâr kesen tentenin açık tarafına aldı.";
            return treatment + comfort + warmth;
        }

        private void ActivateRevisedAssemblyStep()
        {
            DisableRevisedAssemblyInteractions();
            switch (revisedAssemblyIndex)
            {
                case 0:
                    handNeighborToWorker?.SetAvailable(true);
                    ui?.ShowObjective(
                        "NERMİN TEYZEYİ GÖREVLİYLE BULUŞTUR",
                        "Nermin teyzenin yanında basılı tut; durumunu kendisi anlatsın.");
                    break;
                case 1:
                    SetPreparedChoice(
                        StoryFlag.BagFirstAid,
                        useFirstAid,
                        useStationCloth,
                        "CAN İÇİN TEMİZ MALZEMEYİ ULAŞTIR",
                        "İlk yardım setini ya da görevli masasındaki kapalı temiz bezi doğrudan tedavi tepsisine sürükle.");
                    break;
                case 2:
                {
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationAssembly);
                    bool hasWhistle = HasFlag(StoryFlag.BagWhistle);
                    useWhistle?.SetAvailable(hasWhistle);
                    callFamily?.SetAvailable(!hasWhistle);
                    SetActive(whistleWorld, hasWhistle);
                    SetActive(voiceSignalWorld, !hasWhistle);
                    ui?.ShowObjective(
                        "AİLE İŞARETİNİ KULLAN",
                        hasWhistle
                            ? "Düdüğün kendisine üç kısa kez dokun."
                            : "Aile çağrı noktasında basılı tut; alandan ayrılma.");
                    break;
                }
                case 3:
                    SetActive(motherAtAssembly, true);
                    SetActive(fatherAtAssembly, true);
                    Play(motherApproachAnimation);
                    Play(fatherApproachAnimation);
                    reuniteFamily?.SetAvailable(true);
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationAssembly);
                    ui?.ShowObjective(
                        "AİLENİN YANINA GİT",
                        "Anne'ye sahnede dokun; Deniz yanlarına yürüsün, Can onu takip etsin.");
                    break;
                default:
                    FinishEvacuation();
                    break;
            }
        }

        private void SetPreparedChoice(
            StoryFlag flag,
            StoryInteractable prepared,
            StoryInteractable fallback,
            string objective,
            string detail)
        {
            bool preparedAvailable = HasFlag(flag);
            prepared?.SetAvailable(preparedAvailable);
            fallback?.SetAvailable(!preparedAvailable);
            ui?.ShowObjective(objective, detail);
        }

        private bool HasFlag(StoryFlag flag)
        {
            return gameManager != null && gameManager.HasFlag(flag);
        }

        private void DisableRevisedAssemblyInteractions()
        {
            foreach (StoryInteractable interactable in new[]
                     {
                         readAssemblySign, checkCan, handNeighborToWorker, useRadio, listenWorkerRadio,
                         useFirstAid, useStationCloth, useWhistle, callFamily,
                         giveWater, useWaterStation, giveBlanket, moveToWindbreak, useContactCard,
                         useRegistrySheet, reuniteFamily
                     })
                interactable?.SetAvailable(false);
        }

        private void RegisterAssemblyCheck(int index, StoryInteractable interactable, string subtitle)
        {
            if (stage != StoryEvacuationStage.AssemblyChecks || index < 0 || index >= assemblyChecks.Length || assemblyChecks[index])
                return;

            assemblyChecks[index] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(index == 1 ? InteractTrigger : InspectTrigger);
            ShowDialogue(subtitle, 5.5f, CompleteAssemblyCheckOrContinue);
        }

        private void CompleteFamilySignal(StoryInteractable interactable, string subtitle)
        {
            if (stage != StoryEvacuationStage.AssemblyChecks || assemblyChecks[3])
                return;

            assemblyChecks[3] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(CallTrigger);
            ShowDialogue(subtitle, 5.5f, CompleteAssemblyCheckOrContinue);
        }

        private void CompleteAssemblyCheckOrContinue()
        {
            if (AssemblyChecksCompleted >= assemblyChecks.Length)
            {
                FinishEvacuation();
                return;
            }
            UpdateAssemblyObjective();
        }

        private void UpdateAssemblyObjective()
        {
            ui?.ShowObjective($"TOPLANMA ALANI KONTROLÜ — {AssemblyChecksCompleted}/4",
                "Levhayı doğrula, Can'ı ve komşuyu kontrol et, aile planındaki kısa işareti kullan.");
        }

        private void FinishEvacuation()
        {
            stage = StoryEvacuationStage.Completed;
            DisableAllInteractions();
            gameManager?.SetFlag(StoryFlag.AssemblyHeadcountComplete, true);
            gameManager?.CompleteAct(StoryAct.Evacuation);
            gameManager?.CommitCheckpoint(StoryCheckpoint.AssemblyHeadcountComplete);
            ui?.ShowObjective("4. PERDE TAMAMLANDI", "Aile planındaki toplanma alanına güvenli rota ve yardımlaşmayla ulaşıldı.");
            if (completionDetail != null)
                completionDetail.text = revisedFlow
                    ? "Hazırlık kararları sahnede sonuç verdi • Nermin görevliye ulaştı • Aile kadrajda yeniden buluştu"
                    : "Merdiven kullanıldı • Artçıda duruldu • Komşuya hafif destek verildi • Toplanma kontrolü tamamlandı";
            ShowDialogue(
                revisedFlow
                    ? "Aile artık aynı kadrajdaydı. Deniz, Can, Anne ve Baba alandan ayrılmadan resmî duyuruyu dinledi; Nermin teyze görevlinin yanında güvendeydi."
                    : "Anne birkaç dakika sonra görevli yönlendirmesiyle aynı levhaya ulaştı. Aile, caddeyi kapatmadan alanda kaldı ve resmî duyuruları bekledi.",
                8f, () => SetActive(completionPanel, true));
        }

        private void ShowCompletedState()
        {
            stage = StoryEvacuationStage.Completed;
            DisableAllInteractions();
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationAssembly, true);
            SetActive(neighborAtLanding, false);
            SetActive(neighborAtStreet, false);
            SetActive(neighborAtAssembly, true);
            if (revisedFlow)
            {
                SetActive(motherAtAssembly, true);
                SetActive(fatherAtAssembly, true);
                SetActive(familyHeadcountPendingWorld, false);
                SetActive(familyHeadcountCompleteWorld, true);
            }
            SetActive(completionPanel, true);
            ui?.ShowObjective("4. PERDE TAMAMLANDI", "Toplanma alanında aile sayımı ve yardımlaşma tamamlandı.");
        }

        private void RestoreWorldState()
        {
            bool flashlight = gameManager != null && gameManager.HasFlag(StoryFlag.BagFlashlight);
            SetActive(flashlightBeam, flashlight);
            SetActive(emergencyLightRoute, !flashlight);

            bool neighborHelped = gameManager != null && gameManager.HasFlag(StoryFlag.NeighborAssisted);
            SetActive(caneBlocked, !neighborHelped);
            SetActive(caneReachable, neighborHelped);
            SetActive(lightDebrisBlocking, !neighborHelped);
            SetActive(lightDebrisCleared, neighborHelped);
            SetActive(cardboardBlocking, !neighborHelped);
            SetActive(cardboardCleared, neighborHelped);
            neighborTasks[0] = neighborHelped;
            neighborTasks[1] = neighborHelped;
            neighborTasks[2] = neighborHelped;
            SetActive(neighborAtLanding, !neighborHelped);
            SetActive(neighborAtStreet, false);
            SetActive(neighborAtAssembly, false);

            SetActive(stairDoorClosed, true);
            SetActive(stairDoorOpen, false);
            SetActive(buildingDoorClosed, true);
            SetActive(buildingDoorOpen, false);

            if (revisedFlow)
            {
                bool radio = HasFlag(StoryFlag.BagRadio);
                bool firstAid = HasFlag(StoryFlag.BagFirstAid);
                bool water = HasFlag(StoryFlag.BagWater);
                bool blanket = HasFlag(StoryFlag.BagBlanket);
                bool contact = HasFlag(StoryFlag.BagDocuments);
                bool whistle = HasFlag(StoryFlag.BagWhistle);
                SetActive(radioPreparedWorld, radio);
                SetActive(radioFallbackWorld, !radio);
                SetActive(radioTunedIndicator, false);
                SetActive(workerRadioIndicator, false);
                radioPreparedBroadcast?.Stop();
                workerRadioBroadcast?.Stop();
                SetActive(firstAidPreparedWorld, firstAid);
                SetActive(firstAidFallbackWorld, !firstAid);
                SetActive(waterPreparedWorld, water);
                SetActive(waterFallbackWorld, !water);
                SetActive(blanketPreparedWorld, blanket);
                SetActive(blanketFallbackWorld, !blanket);
                SetActive(contactPreparedWorld, contact);
                SetActive(contactFallbackWorld, !contact);
                SetActive(comfortToyAtAssembly, HasFlag(StoryFlag.BagComfortItem));
                SetActive(whistleWorld, whistle);
                SetActive(voiceSignalWorld, !whistle);
                SetActive(workerAtAssembly, true);
                SetActive(motherAtAssembly, false);
                SetActive(fatherAtAssembly, false);
                SetActive(familyHeadcountPendingWorld, true);
                SetActive(familyHeadcountCompleteWorld, false);
            }
        }

        private void DisableAllInteractions()
        {
            foreach (StoryInteractable interactable in new[]
                     {
                         inspectCorridor, chooseStairs, tryElevator, reachUpperLanding, holdHandrail,
                         reachLowerLanding, callNeighbor, moveNeighborCane, clearLightDebris, moveNeighborCardboard,
                         guideNeighbor, openBuildingExit, moveAwayFromFacade, inspectStreetHazard, takeSafeSidewalk,
                         tryUnsafeShortcut, readAssemblySign, checkCan, checkNeighbor, useWhistle, callFamily,
                         handNeighborToWorker, useRadio, listenWorkerRadio, useFirstAid, useStationCloth,
                         giveWater, useWaterStation, giveBlanket, moveToWindbreak, useContactCard,
                         useRegistrySheet, reuniteFamily
                     })
                interactable?.SetAvailable(false);
        }

        private void ShowDialogue(string text, float duration, Action completed = null)
        {
            if (ui != null)
            {
                ui.ShowSubtitle(text, duration, completed);
                return;
            }
            completed?.Invoke();
        }

        private static void Play(Animation animation)
        {
            if (animation != null)
                animation.Play();
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
                target.SetActive(value);
        }
    }
}
