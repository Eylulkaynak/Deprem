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
        [SerializeField] private StoryInteractable guideNeighbor;
        [SerializeField] private GameObject caneBlocked;
        [SerializeField] private GameObject caneReachable;
        [SerializeField] private GameObject lightDebrisBlocking;
        [SerializeField] private GameObject lightDebrisCleared;
        [SerializeField] private GameObject neighborAtLanding;
        [SerializeField] private GameObject neighborAtStreet;
        [SerializeField] private GameObject neighborAtAssembly;
        [SerializeField] private Animation caneMoveAnimation;
        [SerializeField] private Animation debrisMoveAnimation;
        [SerializeField] private Animation neighborRiseAnimation;

        [Header("Building Exit")]
        [SerializeField] private StoryInteractable openBuildingExit;
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

        [Header("Assembly Area")]
        [SerializeField] private StoryInteractable readAssemblySign;
        [SerializeField] private StoryInteractable checkCan;
        [SerializeField] private StoryInteractable checkNeighbor;
        [SerializeField] private StoryInteractable useWhistle;
        [SerializeField] private StoryInteractable callFamily;
        [SerializeField] private GameObject whistleWorld;
        [SerializeField] private GameObject voiceSignalWorld;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionDetail;

        private readonly bool[] neighborTasks = new bool[2];
        private readonly bool[] assemblyChecks = new bool[4];
        private StoryEvacuationStage stage;

        private static readonly int InspectTrigger = Animator.StringToHash("StoryInspect");
        private static readonly int InteractTrigger = Animator.StringToHash("StoryInteract");
        private static readonly int PickUpTrigger = Animator.StringToHash("StoryPickUp");
        private static readonly int CallTrigger = Animator.StringToHash("StoryCall");

        public StoryEvacuationStage Stage => stage;
        public int NeighborTasksCompleted => neighborTasks.Count(value => value);
        public int AssemblyChecksCompleted => assemblyChecks.Count(value => value);

        private void Start()
        {
            gameManager ??= StoryGameManager.Instance;
            gameManager?.BeginAct(StoryAct.Evacuation);
            DisableAllInteractions();
            RestoreWorldState();
            if (completionPanel != null)
                completionPanel.SetActive(false);

            StoryCheckpoint checkpoint = gameManager?.CurrentState?.checkpoint ?? StoryCheckpoint.None;
            switch (checkpoint)
            {
                case StoryCheckpoint.AssemblyHeadcountComplete:
                case StoryCheckpoint.AssemblyAreaReached:
                    ShowCompletedState();
                    break;
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
                "Deniz kapı eşiğinde durdu; tavandan yeni parça sesi gelmediğini, Can'ın yanında olduğunu ve merdiven yönünün açık kaldığını kontrol etti.",
                6.8f, BeginRouteChoice);
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
            aftershockAudio?.Stop();
            aftershockDust?.Stop();
            gameManager?.CommitCheckpoint(StoryCheckpoint.AftershockHeld);
            denizAnimator?.SetTrigger(InteractTrigger);
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
                    moveNeighborCane?.SetAvailable(!neighborTasks[0]);
                    clearLightDebris?.SetAvailable(!neighborTasks[1]);
                    UpdateNeighborObjective();
                });
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
            if (stage != StoryEvacuationStage.HelpingNeighbor || NeighborTasksCompleted < neighborTasks.Length)
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
            ShowDialogue(
                "Ana kaldırımın bir kısmında cam ve gevşek tabela var. Deniz yolu kısaltmak yerine açık görüşlü yan kaldırımı seçti; araç yolunu acil ekipler için boş bıraktı.",
                6.8f, () =>
                {
                    cameraController?.ActivateZone(StoryCameraZoneId.EvacuationStreet);
                    takeSafeSidewalk?.SetAvailable(true);
                    ui?.ShowObjective("AÇIK YAN KALDIRIMDAN İLERLE", "Sarı yön levhasının yanındaki güvenli yola dokun; Can'ı arkanda tut.");
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
            RegisterAssemblyCheck(0, readAssemblySign,
                "Toplanma alanı levhasındaki mahalle adı aile planındaki yerle eşleşiyor. Yol, acil araç girişini kapatmıyor.");
        }

        public void CheckCan()
        {
            RegisterAssemblyCheck(1, checkCan,
                "Deniz Can'ın yanında olduğunu, ayakkabılarının bağlı kaldığını ve yeni bir yaralanması olmadığını kontrol etti.");
        }

        public void CheckNeighbor()
        {
            RegisterAssemblyCheck(2, checkNeighbor,
                "Nermin teyze bastonuyla güvenli alana ulaştı. Deniz onu görevliye gösterdi; tek başına sağlık müdahalesi yapmadı.");
        }

        public void UseWhistle()
        {
            CompleteFamilySignal(useWhistle,
                "Deniz düdüğü kısa aralıklarla kullandı. Aile, kalabalıkta sesini tüketmeden planlanan işareti duydu.");
        }

        public void CallFamily()
        {
            CompleteFamilySignal(callFamily,
                "Çantada düdük yoktu. Deniz belirlenen aile adını yüksek ve kısa aralıklarla seslenerek aynı buluşma noktasında kaldı.");
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
            ui?.ShowObjective("GÜVENLİ DÜŞEY ROTAYI SEÇ", "Merdiven kapısı ve asansör sahnede gerçek nesnelerdir; güvenli olanın kendisiyle etkileş.");
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
            if (stage != StoryEvacuationStage.HelpingNeighbor || index < 0 || index >= neighborTasks.Length || neighborTasks[index])
                return;

            neighborTasks[index] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            Play(animation);
            SetActive(blocked, false);
            SetActive(cleared, true);
            ShowDialogue(subtitle, 5.5f, () =>
            {
                if (NeighborTasksCompleted >= neighborTasks.Length)
                {
                    guideNeighbor?.SetAvailable(true);
                    ui?.ShowObjective("NİNEYE DENGE DESTEĞİ VER", "Bastonunu kavramasını bekle; kolunun yanında basılı tutarak birlikte ayağa kalkın.");
                    return;
                }
                UpdateNeighborObjective();
            });
        }

        private void UpdateNeighborObjective()
        {
            ui?.ShowObjective($"HAFİF ENGELLERİ KENARA AL — {NeighborTasksCompleted}/2",
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
            gameManager?.CommitCheckpoint(StoryCheckpoint.BuildingExited);
            SetActive(neighborAtLanding, false);
            SetActive(neighborAtStreet, true);
            cameraController?.ActivateZone(StoryCameraZoneId.EvacuationBuildingFront);
            ShowDialogue(
                "Üçü bina cephesinden uzak açık noktaya çıktı. Deniz girişin önünde beklemedi; artçıların hasarlı cepheden parça düşürebileceğini hatırladı.",
                6.8f, BeginStreetRoute);
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
                completionDetail.text =
                    "Merdiven kullanıldı • Artçıda duruldu • Komşuya hafif destek verildi • Toplanma kontrolü tamamlandı";
            ShowDialogue(
                "Anne birkaç dakika sonra görevli yönlendirmesiyle aynı levhaya ulaştı. Aile, caddeyi kapatmadan alanda kaldı ve resmî duyuruları bekledi.",
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
            neighborTasks[0] = neighborHelped;
            neighborTasks[1] = neighborHelped;
            SetActive(neighborAtLanding, !neighborHelped);
            SetActive(neighborAtStreet, false);
            SetActive(neighborAtAssembly, false);

            SetActive(stairDoorClosed, true);
            SetActive(stairDoorOpen, false);
            SetActive(buildingDoorClosed, true);
            SetActive(buildingDoorOpen, false);
        }

        private void DisableAllInteractions()
        {
            foreach (StoryInteractable interactable in new[]
                     {
                         inspectCorridor, chooseStairs, tryElevator, reachUpperLanding, holdHandrail,
                         reachLowerLanding, callNeighbor, moveNeighborCane, clearLightDebris, guideNeighbor,
                         openBuildingExit, inspectStreetHazard, takeSafeSidewalk, tryUnsafeShortcut,
                         readAssemblySign, checkCan, checkNeighbor, useWhistle, callFamily
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
