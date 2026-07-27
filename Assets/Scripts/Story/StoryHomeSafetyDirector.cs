using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Deprem.Story
{
    public enum StoryHomeSafetyStage
    {
        Opening,
        RouteTesting,
        NeighborVisit,
        PlacingNeighborPlan,
        Inspecting,
        MappingRiskZones,
        ClearingExit,
        TestingClearedExit,
        LoweringShelfItems,
        MarkingShelfAnchor,
        SecuringShelf,
        TestingShelf,
        SecuringWardrobe,
        TestingWardrobe,
        TestingExit,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed class StoryHomeSafetyDirector : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryGameManager gameManager;
        [SerializeField] private StoryPlayerMovement player;
        [SerializeField] private StoryTouchManager touchManager;
        [SerializeField] private StoryCameraController cameraController;
        [SerializeField] private StoryUIController ui;

        [Header("Characters")]
        [SerializeField] private Transform deniz;
        [SerializeField] private Transform parent;
        [SerializeField] private Transform can;
        [SerializeField] private Animator denizAnimator;
        [SerializeField] private Animator parentAnimator;
        [SerializeField] private Animator canAnimator;

        [Header("Opening Route Test")]
        [SerializeField] private StoryInteractable testInitialRoute;
        [SerializeField] private GameObject initialRouteCarStart;
        [SerializeField] private GameObject initialRouteCarBlocked;
        [SerializeField] private Animation initialRouteCarAnimation;

        [Header("Neighbor Visit")]
        [SerializeField] private StoryInteractable openDoorForNermin;
        [SerializeField] private StoryInteractable returnNerminEnvelope;
        [SerializeField] private GameObject nermin;
        [SerializeField] private GameObject nerminEnvelopeStart;
        [SerializeField] private GameObject nerminEnvelopeReturned;
        [SerializeField] private StoryInteractable placeEvacuationPlan;
        [SerializeField] private GameObject evacuationPlanInHand;
        [SerializeField] private GameObject evacuationPlan;
        [SerializeField] private Animation nerminExitAnimation;

        [Header("Room Inspection")]
        [SerializeField] private StoryInteractable inspectWardrobe;
        [SerializeField] private StoryInteractable inspectShelf;
        [SerializeField] private StoryInteractable inspectExit;

        [Header("Physical Risk Map")]
        [SerializeField] private bool physicalRouteFlow;
        [SerializeField] private GameObject shelfRiskZoneUnstable;
        [SerializeField] private GameObject shelfRiskZoneSecured;
        [SerializeField] private GameObject wardrobeRiskZoneUnstable;
        [SerializeField] private GameObject wardrobeRiskZoneSecured;
        [SerializeField] private GameObject canReadingNestRisk;
        [SerializeField] private GameObject canReadingNestSafe;

        [Header("Clear Exit")]
        [SerializeField] private StoryInteractable moveShoes;
        [SerializeField] private StoryInteractable moveToy;
        [SerializeField] private StoryInteractable moveParcel;
        [SerializeField] private GameObject[] exitClutterStart;
        [SerializeField] private GameObject[] exitClutterStored;
        [SerializeField] private Animation[] exitMoveAnimations;
        [SerializeField] private StoryInteractable testClearedExitDoor;

        [Header("Lower Shelf Items")]
        [SerializeField] private StoryInteractable lowerBooks;
        [SerializeField] private StoryInteractable lowerVase;
        [SerializeField] private StoryInteractable lowerFrame;
        [SerializeField] private StoryInteractable handShelfBracket;
        [SerializeField] private GameObject[] shelfItemsHigh;
        [SerializeField] private GameObject[] shelfItemsLow;
        [SerializeField] private Animation[] shelfMoveAnimations;
        [SerializeField] private StoryInteractable markShelfAnchor;
        [SerializeField] private Animation shelfSecureAnimation;
        [SerializeField] private GameObject shelfAnchorStrap;
        [SerializeField] private ParticleSystem shelfDust;
        [SerializeField] private StoryInteractable testSecuredShelf;
        [SerializeField] private Animation shelfStabilityAnimation;

        [Header("Secure Wardrobe")]
        [SerializeField] private StoryInteractable testWardrobe;
        [SerializeField] private StoryInteractable markWardrobeAnchors;
        [SerializeField] private StoryInteractable handWardrobeStrap;
        [SerializeField] private Animation wardrobeRockAnimation;
        [SerializeField] private Animation wardrobeSecureAnimation;
        [SerializeField] private GameObject wardrobeAnchorStrap;
        [SerializeField] private ParticleSystem wardrobeDust;
        [SerializeField] private StoryInteractable testSecuredWardrobe;

        [Header("Safe Near Misses")]
        [SerializeField] private StoryInteractable unsafeHeavyLift;
        [SerializeField] private StoryInteractable unsafeDrill;
        [SerializeField] private Animation heavyLiftNearMissAnimation;
        [SerializeField] private Animation drillNearMissAnimation;

        [Header("Adult Drill Work")]
        [SerializeField] private Transform shelfParentWorkPoint;
        [SerializeField] private Transform wardrobeParentWorkPoint;
        [SerializeField] private GameObject parentHeldDrill;
        [SerializeField] private AudioSource drillWorkAudio;
        [SerializeField, Min(0.5f)] private float drillWorkSeconds = 2.35f;

        [Header("Final Exit Test")]
        [SerializeField] private StoryInteractable testExitDoor;
        [SerializeField] private Transform exitStandPoint;
        [SerializeField] private GameObject closedDoor;
        [SerializeField] private GameObject openDoor;
        [SerializeField] private Animation doorOpenAnimation;
        [SerializeField] private GameObject finalRouteCarStart;
        [SerializeField] private GameObject finalRouteCarFinish;
        [SerializeField] private GameObject canToyCarPocket;
        [SerializeField] private Animation finalRouteCarAnimation;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionDetail;

        private readonly bool[] inspected = new bool[3];
        private readonly bool[] exitMoved = new bool[3];
        private readonly bool[] shelfMoved = new bool[3];
        private StoryHomeSafetyStage stage;
        private Vector3 parentHomePosition;
        private Quaternion parentHomeRotation;
        private Coroutine drillWorkRoutine;

        private static readonly int InspectTrigger = Animator.StringToHash("StoryInspect");
        private static readonly int InteractTrigger = Animator.StringToHash("StoryInteract");
        private static readonly int PickUpTrigger = Animator.StringToHash("StoryPickUp");
        private static readonly int CallTrigger = Animator.StringToHash("StoryCall");
        private static readonly int WorkTrigger = Animator.StringToHash("StoryWork");
        private static readonly int ResetTrigger = Animator.StringToHash("StoryReset");

        public StoryHomeSafetyStage Stage => stage;
        public int InspectionsCompleted => inspected.Count(value => value);
        public int ExitItemsMoved => exitMoved.Count(value => value);
        public int ShelfItemsMoved => shelfMoved.Count(value => value);

        private void Awake()
        {
            if (parent == null)
                return;
            parentHomePosition = parent.position;
            parentHomeRotation = parent.rotation;
        }

        private void Start()
        {
            if (StoryGameManager.Instance != null)
                gameManager = StoryGameManager.Instance;
            gameManager?.BeginAct(StoryAct.HomeSafety);
            DisableAllInteractions();
            RestoreWorldState();
            if (completionPanel != null)
                completionPanel.SetActive(false);

            StoryCheckpoint checkpoint = gameManager?.CurrentState?.checkpoint ?? StoryCheckpoint.None;
            switch (checkpoint)
            {
                case StoryCheckpoint.HomeSafetyComplete:
                    ShowCompletedState();
                    break;
                case StoryCheckpoint.HomeWardrobeSecured:
                    BeginFinalExitTest();
                    break;
                case StoryCheckpoint.HomeShelfPrepared:
                    BeginWardrobeSafety();
                    break;
                case StoryCheckpoint.HomeExitCleared:
                    BeginShelfSafety();
                    break;
                default:
                    StartOpening();
                    break;
            }
        }

        public void InspectWardrobe()
        {
            if (physicalRouteFlow && stage == StoryHomeSafetyStage.MappingRiskZones)
                SetActive(wardrobeRiskZoneUnstable, true);
            RegisterInspection(0, inspectWardrobe, StoryCameraZoneId.HomeWardrobe,
                physicalRouteFlow
                    ? "Deniz parmağını dolabın yüksekliğinden zemine indirdi. Turuncu alan, dolabın sabitlenmezse kapatabileceği geçişi gösterdi.\nAnne: Ağır dolabı çekmiyoruz; düşme alanını okuyup bağlantıyı yetişkin olarak ben yapacağım."
                    : "Deniz: Dolap uzun ve duvara bağlı görünmüyor.\nAnne: İçini boşaltmaya çalışmayacağız; önce riskini işaretleyip sabitlemeyi ben yapacağım.");
        }

        public void InspectShelf()
        {
            if (physicalRouteFlow && stage == StoryHomeSafetyStage.MappingRiskZones)
                SetActive(shelfRiskZoneUnstable, true);
            RegisterInspection(1, inspectShelf, StoryCameraZoneId.HomeShelf,
                physicalRouteFlow
                    ? "Can raftan zemine uzanan turuncu alanı gördü.\nCan: Saksı buraya düşerse yol kapanır.\nAnne: Hafif eşyaları aşağı alabilirsiniz; raf bağlantısını ben kontrol edeceğim."
                    : "Can: Üst raftaki seramik saksı sallanırsa düşebilir.\nAnne: Hafif eşyaları aşağı alabilirsiniz. Rafın duvar bağlantısını yetişkin kontrol eder.");
        }

        public void InspectExit()
        {
            if (physicalRouteFlow)
            {
                SetActive(canReadingNestRisk, false);
                SetActive(canReadingNestSafe, true);
                canAnimator?.SetTrigger(PickUpTrigger);
                RegisterInspection(2, inspectExit, StoryCameraZoneId.HomeOverview,
                    "Can'ın okuma minderi turuncu düşme alanından ortak halının güvenli köşesine taşındı.\n" +
                    "Can: Raf devrilse bile burada üstüme gelmez.\n" +
                    "Deniz: Güvenli oda, yalnız mobilyayı değil nerede oturduğumuzu da düşünmek demek.");
                return;
            }
            RegisterInspection(2, inspectExit, StoryCameraZoneId.HomeExit,
                "Deniz: Ayakkabı, oyuncak ve paket kapının açılacağı yerde.\nAnne: Çıkış yolu gündelik hayatta da boş kalmalı; bunları kendi yerlerine taşıyalım.");
        }

        public void TestInitialRoute()
        {
            if (stage != StoryHomeSafetyStage.RouteTesting)
                return;

            testInitialRoute?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            denizAnimator?.SetTrigger(InteractTrigger);
            SetActive(initialRouteCarStart, false);
            SetActive(initialRouteCarBlocked, true);
            Play(initialRouteCarAnimation);
            ShowDialogue(
                "Can'ın arabası ayakkabıya çarpıp durdu.\nCan: Araba geçemedi.\nDeniz: Biz de karanlıkta aynı yerde takılırız.",
                7.2f,
                physicalRouteFlow ? BeginExitClearing : BeginNeighborVisit);
        }

        public void OpenDoorForNermin()
        {
            if (stage != StoryHomeSafetyStage.NeighborVisit)
                return;

            openDoorForNermin?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            SetActive(closedDoor, false);
            SetActive(openDoor, true);
            SetActive(nermin, true);
            SetActive(nerminEnvelopeStart, true);
            Play(doorOpenAnimation);
            parentAnimator?.SetTrigger(CallTrigger);
            ShowDialogue(
                "Nermin: Apartmanın yeni tahliye planını getirdim.\nCan: Asansör niye çizilmemiş?\nNermin: Sarsıntıdan sonra merdiven kullanılır; yolu açık tutan biri bana yeter.",
                9.2f,
                () =>
                {
                    returnNerminEnvelope?.SetAvailable(true);
                    ui?.ShowObjective(
                        "DÜŞEN ZARFI NERMİN'E VER",
                        "Zarfı yerden tutup Nermin'in açık eline bırak.");
                });
        }

        public void ReturnNerminEnvelope()
        {
            if (stage != StoryHomeSafetyStage.NeighborVisit)
                return;

            returnNerminEnvelope?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            SetActive(nerminEnvelopeStart, false);
            SetActive(nerminEnvelopeReturned, true);
            SetActive(evacuationPlanInHand, placeEvacuationPlan != null);
            SetActive(evacuationPlan, placeEvacuationPlan == null);
            ShowDialogue(
                "Deniz zarfı Nermin'in eline verdi. İçinden apartmanın yeni tahliye planı çıktı.\nNermin: Matkap sesini duyarsam artık nedenini bilirim.",
                8.2f,
                placeEvacuationPlan != null ? BeginPlanPlacement : FinishNeighborVisit);
        }

        public void PlaceEvacuationPlan()
        {
            if (stage != StoryHomeSafetyStage.PlacingNeighborPlan)
                return;

            placeEvacuationPlan?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            SetActive(evacuationPlanInHand, false);
            SetActive(evacuationPlan, true);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeOverview);
            ShowDialogue(
                "Plan aile panosunun yanına yerleşti. Asansör işareti yoktu; merdiven rotası ve Nermin'in dairesi aynı çizgide görünüyordu.",
                7.2f,
                FinishNeighborVisit);
        }

        public void TestClearedExitDoor()
        {
            if (stage != StoryHomeSafetyStage.TestingClearedExit)
                return;

            testClearedExitDoor?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            SetActive(closedDoor, false);
            SetActive(openDoor, true);
            Play(doorOpenAnimation);
            ShowDialogue(
                "Kapı hiçbir eşyaya çarpmadan tam açıldı. Zemindeki kesintisiz çizgi artık oyuncak arabadan eşiğe kadar görülebiliyordu.",
                7.2f,
                () =>
                {
                    SetActive(closedDoor, true);
                    SetActive(openDoor, false);
                    BeginShelfSafety();
                });
        }

        public void MarkShelfAnchor()
        {
            if (stage != StoryHomeSafetyStage.MarkingShelfAnchor)
                return;

            markShelfAnchor?.SetAvailable(false);
            denizAnimator?.SetTrigger(InspectTrigger);
            ShowDialogue(
                "Deniz rafın üst bağlantı noktasını işaretledi. Anne duvar dikmesini kontrol edip uygun bağlantı parçasını çalışma örtüsüne koydu.",
                6.8f,
                BeginShelfSecuring);
        }

        public void TestSecuredShelf()
        {
            if (stage != StoryHomeSafetyStage.TestingShelf)
                return;

            testSecuredShelf?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeShelf);
            denizAnimator?.SetTrigger(InspectTrigger);
            Play(shelfStabilityAnimation);
            SetActive(shelfRiskZoneUnstable, false);
            SetActive(shelfRiskZoneSecured, true);
            ShowDialogue(
                "Anne rafı aynı noktadan hafifçe sınadı. Önceki sallanma yoktu; ağır kitaplar da artık alt bölmedeydi.",
                7.2f,
                BeginWardrobeSafety);
        }

        public void TestSecuredWardrobe()
        {
            if (stage != StoryHomeSafetyStage.TestingWardrobe)
                return;

            testSecuredWardrobe?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            denizAnimator?.SetTrigger(InspectTrigger);
            Play(wardrobeRockAnimation);
            SetActive(wardrobeRiskZoneUnstable, false);
            SetActive(wardrobeRiskZoneSecured, true);
            ShowDialogue(
                "Anne aynı kontrollü testi tekrarladı. Kayış gerildi, dolap duvardan ayrılmadı.\nDeniz: Risk aynı yerdeydi; davranışı değişti.",
                7.6f,
                BeginFinalExitTest);
        }

        public void MoveExitShoes()
        {
            CompleteExitMove(0, moveShoes, "Ayakkabılar kapının önünden alçak ayakkabılığa taşındı.");
        }

        public void MoveExitToy()
        {
            CompleteExitMove(1, moveToy, "Can oyuncağını geçiş yolundan kaldırıp oyuncak sepetine bıraktı.");
        }

        public void MoveExitParcel()
        {
            CompleteExitMove(2, moveParcel, "Hafif paket duvar kenarındaki saklama alanına alındı; kapı yayı tamamen boş.");
        }

        public void LowerShelfBooks()
        {
            CompleteShelfMove(0, lowerBooks, "Kalın kitaplar üst raftan diz hizasındaki bölmeye indirildi.");
        }

        public void LowerShelfVase()
        {
            CompleteShelfMove(1, lowerVase, "Kırılabilir seramik saksı üst raftan kapaklı alt dolaba taşındı.");
        }

        public void LowerShelfFrame()
        {
            CompleteShelfMove(2, lowerFrame, "Çerçeve raf kenarından alındı; düşebilecek hafif eşya kalmadı.");
        }

        public void HandShelfBracket()
        {
            if (stage != StoryHomeSafetyStage.SecuringShelf)
                return;

            handShelfBracket?.SetAvailable(false);
            DisableNearMisses();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeShelf);
            denizAnimator?.SetTrigger(PickUpTrigger);
            BeginParentDrillWork(shelfParentWorkPoint);
            SetActive(shelfAnchorStrap, true);
            Play(shelfSecureAnimation);
            shelfDust?.Play();
            gameManager?.SetFlag(StoryFlag.ShelfSecured, true);
            gameManager?.CommitCheckpoint(StoryCheckpoint.HomeShelfPrepared);
            ShowDialogue(
                "Deniz bağlantı parçasını uzattı. Anne rafı duvar dikmesine sabitledi; çocuklar matkap veya ağır rafla uğraşmadı.",
                9.2f, () =>
                {
                    ReturnParentHome();
                    if (testSecuredShelf != null)
                    {
                        BeginShelfStabilityTest();
                        return;
                    }
                    BeginWardrobeSafety();
                });
        }

        public void TestWardrobe()
        {
            if (stage != StoryHomeSafetyStage.SecuringWardrobe)
                return;

            testWardrobe?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            denizAnimator?.SetTrigger(InspectTrigger);
            Play(wardrobeRockAnimation);
            wardrobeDust?.Play();
            ShowDialogue(
                "Dolap hafifçe esnedi. Deniz zorlamayı bıraktı.\nAnne: Ağır mobilyayı çekmiyoruz; yalnızca bağlantı noktalarını işaretliyoruz.",
                6.8f, () =>
                {
                    markWardrobeAnchors?.SetAvailable(true);
                    ui?.ShowObjective("BAĞLANTI NOKTALARINI İŞARETLE", "Dolabın üst iki köşesindeki duvar bağlantı yerlerine doğrudan dokun.");
                });
        }

        public void MarkWardrobeAnchors()
        {
            if (stage != StoryHomeSafetyStage.SecuringWardrobe)
                return;

            markWardrobeAnchors?.SetAvailable(false);
            denizAnimator?.SetTrigger(InspectTrigger);
            ShowDialogue(
                "Deniz iki bağlantı noktasını kalemle işaretledi. Anne uygun dübel ve kayışı hazırladı.",
                5.8f, () =>
                {
                    handWardrobeStrap?.SetAvailable(true);
                    ui?.ShowObjective("KAYIŞI ANNEYE UZAT", "Kayışı sahnedeki Deniz'in elinden Anne'nin çalışma alanına doğru çek.");
                });
        }

        public void HandWardrobeStrap()
        {
            if (stage != StoryHomeSafetyStage.SecuringWardrobe)
                return;

            handWardrobeStrap?.SetAvailable(false);
            DisableNearMisses();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            denizAnimator?.SetTrigger(PickUpTrigger);
            BeginParentDrillWork(wardrobeParentWorkPoint);
            SetActive(wardrobeAnchorStrap, true);
            Play(wardrobeSecureAnimation);
            wardrobeDust?.Play();
            gameManager?.SetFlag(StoryFlag.WardrobeSecured, true);
            gameManager?.CommitCheckpoint(StoryCheckpoint.HomeWardrobeSecured);
            ShowDialogue(
                "Anne kayışı iki noktadan sabitledi ve dolabı yeniden kontrol etti. Ağır işi yetişkin yaptı; Deniz güvenli mesafede kaldı.",
                9.2f, () =>
                {
                    ReturnParentHome();
                    if (testSecuredWardrobe != null)
                    {
                        BeginWardrobeStabilityTest();
                        return;
                    }
                    BeginFinalExitTest();
                });
        }

        public void TryUnsafeHeavyLift()
        {
            unsafeHeavyLift?.SetAvailable(false);
            gameManager?.AddMistake();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            Play(heavyLiftNearMissAnimation);
            Play(wardrobeRockAnimation);
            FaceParentTowards(unsafeHeavyLift != null ? unsafeHeavyLift.transform : null);
            parentAnimator?.SetTrigger(CallTrigger);
            ShowDialogue(
                "Anne: Dur Deniz! Ağır mobilyayı tek başına çekmek devrilme ve sıkışma riski oluşturur. Yalnızca hafif eşyaları taşıyoruz.",
                7f, ReturnToCurrentStageCamera);
        }

        public void TryUnsafeDrill()
        {
            unsafeDrill?.SetAvailable(false);
            gameManager?.AddMistake();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeParent);
            Play(drillNearMissAnimation);
            FaceParentTowards(unsafeDrill != null ? unsafeDrill.transform : null);
            parentAnimator?.SetTrigger(PickUpTrigger);
            ShowDialogue(
                "Anne matkabı çalıştırmadan fişten çekip kendi çalışma alanına aldı. Elektrikli alet ve duvar bağlantısı yetişkin sorumluluğunda.",
                7f, ReturnToCurrentStageCamera);
        }

        public void TestExitDoor()
        {
            if (stage != StoryHomeSafetyStage.TestingExit)
                return;

            testExitDoor?.SetAvailable(false);
            DisableNearMisses();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeFinalTest);
            SetActive(finalRouteCarStart, false);
            SetActive(finalRouteCarFinish, true);
            Play(finalRouteCarAnimation);
            SetActive(closedDoor, false);
            SetActive(openDoor, true);
            Play(doorOpenAnimation);
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            bool moving = player != null && exitStandPoint != null &&
                          player.MoveTo(exitStandPoint, openDoor != null ? openDoor.transform.position : exitStandPoint.position,
                              FinishExitTest);
            if (!moving)
                FinishExitTest();
        }

        private void StartOpening()
        {
            stage = StoryHomeSafetyStage.Opening;
            gameManager?.CommitCheckpoint(StoryCheckpoint.HomeSafetyStart);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeOverview, true);
            if (testInitialRoute != null)
            {
                ui?.ShowObjective(
                    "ÇIKIŞ YOLUNU BİRLİKTE DENE",
                    "Can'ın oyuncak arabasını tutup kapıya giden rota boyunca sürükle.");
                ShowDialogue(
                    "Çanta giriş rafındaydı. Can'ın oyuncak arabası yine kapıya giderken bir şeye takıldı.\nAnne: Bu kez arabaya değil, yoluna bakın.",
                    7.5f,
                    BeginRouteTest);
                return;
            }

            ui?.ShowObjective(
                "EVİ GÖZÜNLE TARA",
                "Dolap, raf ve çıkış yolunu sahnenin içinde incele; riskleri konuşarak bul.");
            ShowDialogue(
                "Çanta hazırlandıktan sonra aile salona geri döndü. Anne, Deniz ile Can'a odadaki eşyaların depremde nasıl davranabileceğini sordu.",
                7.5f,
                BeginInspection);
        }

        private void BeginRouteTest()
        {
            stage = StoryHomeSafetyStage.RouteTesting;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            testInitialRoute?.SetAvailable(true);
            ui?.ShowObjective(
                "ARABAYI KAPIYA DOĞRU SÜR",
                "Oyuncak arabayı doğrudan tut; kapıya giden çizgi boyunca bırakmadan taşı.");
        }

        private void BeginNeighborVisit()
        {
            stage = StoryHomeSafetyStage.NeighborVisit;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            openDoorForNermin?.SetAvailable(true);
            returnNerminEnvelope?.SetAvailable(false);
            ui?.ShowObjective(
                "KAPIYI NERMİN İÇİN AÇ",
                "Kapı kolunu sahnede yana çek; temizlenmesi gereken açılma yayını gör.");
        }

        private void FinishNeighborVisit()
        {
            if (physicalRouteFlow)
            {
                Play(nerminExitAnimation);
                ShowDialogue(
                    "Nermin kapıdan çıkarken bastonunu açık kalan çizginin üstüne koydu.\nNermin: Arabaya yetişemem ama sizi aşağıda yakalarım.",
                    6.4f,
                    () =>
                    {
                        CloseNeighborVisit();
                        BeginRiskMapping();
                    });
                return;
            }

            CloseNeighborVisit();
            BeginInspection();
        }

        private void CloseNeighborVisit()
        {
            SetActive(nerminEnvelopeReturned, false);
            SetActive(evacuationPlanInHand, false);
            SetActive(nermin, false);
            SetActive(closedDoor, true);
            SetActive(openDoor, false);
            SetActive(initialRouteCarBlocked, false);
        }

        private void BeginPlanPlacement()
        {
            stage = StoryHomeSafetyStage.PlacingNeighborPlan;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeOverview);
            placeEvacuationPlan?.SetAvailable(true);
            ui?.ShowObjective(
                "TAHLİYE PLANINI AİLE PANOSUNA AS",
                "Planı Nermin'in elinden tutup sol duvardaki aile panosuna bırak.");
        }

        private void BeginInspection()
        {
            stage = StoryHomeSafetyStage.Inspecting;
            inspectWardrobe?.SetAvailable(!inspected[0]);
            inspectShelf?.SetAvailable(!inspected[1]);
            inspectExit?.SetAvailable(!inspected[2]);
            EnableNearMisses();
            UpdateInspectionObjective();
        }

        private void BeginRiskMapping()
        {
            stage = StoryHomeSafetyStage.MappingRiskZones;
            inspectWardrobe?.SetAvailable(!inspected[0]);
            inspectShelf?.SetAvailable(!inspected[1]);
            inspectExit?.SetAvailable(!inspected[2]);
            EnableNearMisses();
            UpdateRiskMapObjective();
        }

        private void RegisterInspection(int index, StoryInteractable interactable, StoryCameraZoneId zone, string subtitle)
        {
            bool mappingPhysicalRisk = physicalRouteFlow && stage == StoryHomeSafetyStage.MappingRiskZones;
            if ((!mappingPhysicalRisk && stage != StoryHomeSafetyStage.Inspecting) ||
                index < 0 ||
                index >= inspected.Length ||
                inspected[index])
                return;

            inspected[index] = true;
            interactable?.SetAvailable(false);
            cameraController?.ActivateZone(zone);
            denizAnimator?.SetTrigger(InspectTrigger);
            ShowDialogue(subtitle, 6.5f, () =>
            {
                int requiredInspections = inspected.Length;
                if (InspectionsCompleted >= requiredInspections)
                {
                    if (mappingPhysicalRisk)
                    {
                        gameManager?.CommitCheckpoint(StoryCheckpoint.HomeExitCleared);
                        BeginShelfSafety();
                        return;
                    }
                    BeginExitClearing();
                    return;
                }

                cameraController?.ActivateZone(StoryCameraZoneId.HomeOverview);
                if (mappingPhysicalRisk)
                    UpdateRiskMapObjective();
                else
                    UpdateInspectionObjective();
            });
        }

        private void UpdateInspectionObjective()
        {
            ui?.ShowObjective($"ODAYI GÖZÜNLE TARA — {InspectionsCompleted}/3",
                "Riskli nesnenin kendisine dokun: uzun dolap, yüksek raf ve kapı önü.");
        }

        private void UpdateRiskMapObjective()
        {
            ui?.ShowObjective(
                $"RİSKİ GÖR, YAŞAM ALANINI DEĞİŞTİR — {Math.Min(InspectionsCompleted, 3)}/3",
                "Raf ve dolabın düşme alanını zemine indir; sonra Can'ın minderini turuncu alanın dışına taşı.");
        }

        private void BeginExitClearing()
        {
            stage = StoryHomeSafetyStage.ClearingExit;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            moveShoes?.SetAvailable(!exitMoved[0]);
            moveToy?.SetAvailable(!exitMoved[1]);
            moveParcel?.SetAvailable(!exitMoved[2]);
            ui?.ShowObjective($"ÇIKIŞ YOLUNU FİZİKSEL OLARAK AÇ — {ExitItemsMoved}/3",
                "Ayakkabı, oyuncak ve hafif paketi doğrudan tutup sahnedeki saklama yerlerine çek.");
        }

        private void CompleteExitMove(int index, StoryInteractable interactable, string subtitle)
        {
            if (stage != StoryHomeSafetyStage.ClearingExit || index < 0 || index >= exitMoved.Length || exitMoved[index])
                return;

            exitMoved[index] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            SetActive(Index(exitClutterStart, index), false);
            SetActive(Index(exitClutterStored, index), true);
            Play(Index(exitMoveAnimations, index));
            ShowDialogue(subtitle, 4.8f, () =>
            {
                if (ExitItemsMoved >= exitMoved.Length)
                {
                    gameManager?.SetFlag(StoryFlag.ExitCleared, true);
                    SetActive(initialRouteCarBlocked, false);
                    if (physicalRouteFlow)
                    {
                        ShowDialogue(
                            "Kapının açılma yayı tamamen boşaldı. Tam o anda zil çaldı; temizlenen yolu bu kez gerçekten kapıyı açarak sınayacaklardı.",
                            6.8f,
                            BeginNeighborVisit);
                        return;
                    }

                    gameManager?.CommitCheckpoint(StoryCheckpoint.HomeExitCleared);
                    ShowDialogue(
                        "Ayakkabı, oyuncak kutusu ve paket artık kapının açılma yayının dışında. Şimdi kapının gerçekten tam açıldığını sınayalım.",
                        6.5f,
                        testClearedExitDoor != null ? BeginClearedExitTest : BeginShelfSafety);
                    return;
                }

                ui?.ShowObjective($"ÇIKIŞ YOLUNU FİZİKSEL OLARAK AÇ — {ExitItemsMoved}/3",
                    "Kalan nesneyi parmağınla tutup kendi saklama alanına çek.");
            });
        }

        private void BeginClearedExitTest()
        {
            stage = StoryHomeSafetyStage.TestingClearedExit;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeExit);
            testClearedExitDoor?.SetAvailable(true);
            ui?.ShowObjective(
                "TEMİZLENEN KAPI YAYINI SINAYIN",
                "Kapı kolunu yana çek; kapının ayakkabı, kutu veya pakete çarpmadan açıldığını gör.");
        }

        private void BeginShelfSafety()
        {
            stage = StoryHomeSafetyStage.LoweringShelfItems;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeShelf);
            lowerBooks?.SetAvailable(!shelfMoved[0]);
            lowerVase?.SetAvailable(!shelfMoved[1]);
            lowerFrame?.SetAvailable(!shelfMoved[2]);
            EnableNearMisses();
            ui?.ShowObjective($"ÜST RAFTAKİ HAFİF EŞYALARI İNDİR — {ShelfItemsMoved}/3",
                "Kitap, seramik saksı ve çerçeveyi kendi üzerlerinden aşağı çek; ağır rafı yerinden oynatma.");
        }

        private void CompleteShelfMove(int index, StoryInteractable interactable, string subtitle)
        {
            if (stage != StoryHomeSafetyStage.LoweringShelfItems || index < 0 || index >= shelfMoved.Length || shelfMoved[index])
                return;

            shelfMoved[index] = true;
            interactable?.SetAvailable(false);
            denizAnimator?.SetTrigger(PickUpTrigger);
            SetActive(Index(shelfItemsHigh, index), false);
            SetActive(Index(shelfItemsLow, index), true);
            Play(Index(shelfMoveAnimations, index));
            ShowDialogue(subtitle, 4.8f, () =>
            {
                if (ShelfItemsMoved >= shelfMoved.Length)
                {
                    if (markShelfAnchor != null)
                    {
                        stage = StoryHomeSafetyStage.MarkingShelfAnchor;
                        markShelfAnchor.SetAvailable(true);
                        cameraController?.ActivateZone(StoryCameraZoneId.HomeShelf);
                        ui?.ShowObjective(
                            "RAF BAĞLANTI NOKTASINI İŞARETLE",
                            "Rafın üst köşesindeki güvenli bağlantı noktasına iki kez doğrudan dokun.");
                        return;
                    }

                    BeginShelfSecuring();
                    return;
                }

                ui?.ShowObjective($"ÜST RAFTAKİ HAFİF EŞYALARI İNDİR — {ShelfItemsMoved}/3",
                    "Kalan hafif eşyayı doğrudan aşağı indir.");
            });
        }

        private void BeginShelfSecuring()
        {
            stage = StoryHomeSafetyStage.SecuringShelf;
            handShelfBracket?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.HomeParent);
            ui?.ShowObjective(
                "RAF BAĞLANTISINI ANNEYE VER",
                "Bağlantı parçasını Deniz'in elinden Anne'nin çalışma alanına doğru çek.");
        }

        private void BeginShelfStabilityTest()
        {
            stage = StoryHomeSafetyStage.TestingShelf;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeShelf);
            testSecuredShelf?.SetAvailable(true);
            ui?.ShowObjective(
                "SABİTLENEN RAFI AYNI YERDEN SINAYIN",
                "Rafın yanındaki kontrol noktasında basılı tut; önceki sallanmayla karşılaştır.");
        }

        private void BeginWardrobeSafety()
        {
            stage = StoryHomeSafetyStage.SecuringWardrobe;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            testWardrobe?.SetAvailable(true);
            markWardrobeAnchors?.SetAvailable(false);
            handWardrobeStrap?.SetAvailable(false);
            EnableNearMisses();
            ui?.ShowObjective("DOLABIN DENGESİNİ KONTROL ET", "Dolabın yan yüzünde basılı tut; sallanma görürsen zorlamayı bırak.");
        }

        private void BeginFinalExitTest()
        {
            stage = StoryHomeSafetyStage.TestingExit;
            DisableAllInteractions();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeFinalTest);
            SetActive(finalRouteCarStart, true);
            SetActive(finalRouteCarFinish, false);
            testExitDoor?.SetAvailable(true);
            ui?.ShowObjective(
                "ROTAYI BİR KEZ DAHA DENE",
                finalRouteCarStart != null
                    ? "Can'ın arabasını aynı başlangıçtan kapı eşiğine kadar doğrudan sürükle."
                    : "Kapı kolunu yana çek; kapının ve yürüyüş rotasının tamamen açıldığını gör.");
        }

        private void BeginWardrobeStabilityTest()
        {
            stage = StoryHomeSafetyStage.TestingWardrobe;
            cameraController?.ActivateZone(StoryCameraZoneId.HomeWardrobe);
            testSecuredWardrobe?.SetAvailable(true);
            ui?.ShowObjective(
                "SABİTLENEN DOLABI TEKRAR SINAYIN",
                "Dolabın aynı yan kontrol noktasında basılı tut; kayışın sonucu görünür olsun.");
        }

        private void FinishExitTest()
        {
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            gameManager?.CompleteAct(StoryAct.HomeSafety);
            gameManager?.CommitCheckpoint(StoryCheckpoint.HomeSafetyComplete);
            stage = StoryHomeSafetyStage.Completed;
            if (physicalRouteFlow && canToyCarPocket != null)
            {
                SetActive(finalRouteCarFinish, false);
                SetActive(canToyCarPocket, true);
                canAnimator?.SetTrigger(PickUpTrigger);
            }
            else
            {
                canAnimator?.SetTrigger(CallTrigger);
            }
            ui?.ShowObjective("2. PERDE TAMAMLANDI", "Çıkış açık; raf ve dolap yetişkin tarafından sabitlendi.");
            if (completionDetail != null)
                completionDetail.text =
                    "Çıkış yolu temizlendi • Hafif eşyalar aşağı alındı • Ağır sabitlemeleri yetişkin yaptı";
            ShowDialogue(
                finalRouteCarFinish != null
                    ? physicalRouteFlow
                        ? "Oyuncak araba eski engellerin yerinden hiç durmadan geçti. Can arabayı yerden alıp cebine koydu.\nKoridordan Nermin'in bastonu duyuldu: Ben aşağıda sizi bekliyorum.\nCan: Geçti.\nDeniz: Biz de geçeriz."
                        : "Oyuncak araba ayakkabıların, kutunun ve paketin eski yerlerinden hiç durmadan geçti.\nCan: Geçti.\nDeniz: Biz de geçeriz."
                    : "Can kapıdan rahatça geçebildi. Deniz, hazırlığın yalnızca eşya toplamak değil; evde düşebilecek ve yolu kapatabilecek riskleri azaltmak olduğunu gördü.",
                8f, () => SetActive(completionPanel, true));
        }

        private void ShowCompletedState()
        {
            stage = StoryHomeSafetyStage.Completed;
            DisableAllInteractions();
            cameraController?.ActivateZone(StoryCameraZoneId.HomeFinalTest, true);
            SetActive(closedDoor, false);
            SetActive(openDoor, true);
            SetActive(finalRouteCarStart, false);
            SetActive(finalRouteCarFinish, !physicalRouteFlow);
            SetActive(canToyCarPocket, physicalRouteFlow);
            SetActive(shelfRiskZoneUnstable, false);
            SetActive(shelfRiskZoneSecured, true);
            SetActive(wardrobeRiskZoneUnstable, false);
            SetActive(wardrobeRiskZoneSecured, true);
            SetActive(completionPanel, true);
            ui?.ShowObjective("2. PERDE TAMAMLANDI", "Ev içindeki yapısal olmayan riskler azaltıldı.");
        }

        private void RestoreWorldState()
        {
            EndParentDrillWork();
            ReturnParentHome();
            bool exitReady = gameManager != null && gameManager.HasFlag(StoryFlag.ExitCleared);
            bool shelfReady = gameManager != null && gameManager.HasFlag(StoryFlag.ShelfSecured);
            bool wardrobeReady = gameManager != null && gameManager.HasFlag(StoryFlag.WardrobeSecured);
            StoryCheckpoint checkpoint = gameManager?.CurrentState?.checkpoint ?? StoryCheckpoint.None;
            bool preludeComplete = checkpoint is StoryCheckpoint.HomeExitCleared
                or StoryCheckpoint.HomeShelfPrepared
                or StoryCheckpoint.HomeWardrobeSecured
                or StoryCheckpoint.HomeSafetyComplete;
            for (int i = 0; i < exitMoved.Length; i++)
            {
                exitMoved[i] = exitReady;
                SetActive(Index(exitClutterStart, i), !exitReady);
                SetActive(Index(exitClutterStored, i), exitReady);
            }
            for (int i = 0; i < shelfMoved.Length; i++)
            {
                shelfMoved[i] = shelfReady;
                SetActive(Index(shelfItemsHigh, i), !shelfReady);
                SetActive(Index(shelfItemsLow, i), shelfReady);
            }
            SetActive(shelfAnchorStrap, shelfReady);
            SetActive(wardrobeAnchorStrap, wardrobeReady);
            SetActive(initialRouteCarStart, !preludeComplete);
            SetActive(initialRouteCarBlocked, false);
            SetActive(nermin, false);
            SetActive(nerminEnvelopeStart, false);
            SetActive(nerminEnvelopeReturned, false);
            SetActive(evacuationPlanInHand, false);
            SetActive(evacuationPlan, preludeComplete);
            SetActive(finalRouteCarStart, false);
            SetActive(finalRouteCarFinish, false);
            SetActive(canToyCarPocket, false);
            SetActive(shelfRiskZoneUnstable, physicalRouteFlow && preludeComplete && !shelfReady);
            SetActive(shelfRiskZoneSecured, physicalRouteFlow && shelfReady);
            SetActive(wardrobeRiskZoneUnstable, physicalRouteFlow && preludeComplete && !wardrobeReady);
            SetActive(wardrobeRiskZoneSecured, physicalRouteFlow && wardrobeReady);
            SetActive(canReadingNestRisk, physicalRouteFlow && !preludeComplete);
            SetActive(canReadingNestSafe, physicalRouteFlow && preludeComplete);
            SetActive(closedDoor, true);
            SetActive(openDoor, false);
        }

        private void DisableAllInteractions()
        {
            foreach (StoryInteractable interactable in new[]
                     {
                         inspectWardrobe, inspectShelf, inspectExit, moveShoes, moveToy, moveParcel,
                         lowerBooks, lowerVase, lowerFrame, handShelfBracket, testWardrobe,
                         markWardrobeAnchors, handWardrobeStrap, unsafeHeavyLift, unsafeDrill, testExitDoor,
                         testInitialRoute, openDoorForNermin, returnNerminEnvelope, placeEvacuationPlan,
                         testClearedExitDoor, markShelfAnchor, testSecuredShelf, testSecuredWardrobe
                     })
                interactable?.SetAvailable(false);
        }

        private void EnableNearMisses()
        {
            unsafeHeavyLift?.SetAvailable(true);
            unsafeDrill?.SetAvailable(true);
        }

        private void DisableNearMisses()
        {
            unsafeHeavyLift?.SetAvailable(false);
            unsafeDrill?.SetAvailable(false);
        }

        private void ReturnToCurrentStageCamera()
        {
            cameraController?.ActivateZone(stage switch
            {
                StoryHomeSafetyStage.RouteTesting or StoryHomeSafetyStage.NeighborVisit
                    or StoryHomeSafetyStage.ClearingExit or StoryHomeSafetyStage.TestingClearedExit
                    => StoryCameraZoneId.HomeExit,
                StoryHomeSafetyStage.PlacingNeighborPlan => StoryCameraZoneId.HomeOverview,
                StoryHomeSafetyStage.MappingRiskZones => StoryCameraZoneId.HomeOverview,
                StoryHomeSafetyStage.LoweringShelfItems or StoryHomeSafetyStage.MarkingShelfAnchor
                    or StoryHomeSafetyStage.SecuringShelf or StoryHomeSafetyStage.TestingShelf
                    => StoryCameraZoneId.HomeShelf,
                StoryHomeSafetyStage.SecuringWardrobe or StoryHomeSafetyStage.TestingWardrobe
                    => StoryCameraZoneId.HomeWardrobe,
                StoryHomeSafetyStage.TestingExit => StoryCameraZoneId.HomeFinalTest,
                _ => StoryCameraZoneId.HomeOverview
            });
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

        private static T Index<T>(T[] values, int index) where T : class
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : null;
        }

        private static void Play(Animation animation)
        {
            if (animation != null)
                animation.Play();
        }

        private void BeginParentDrillWork(Transform workPoint)
        {
            EndParentDrillWork();
            if (parent != null && workPoint != null)
            {
                parent.position = workPoint.position;
                parent.rotation = workPoint.rotation;
            }

            SetActive(unsafeDrill != null ? unsafeDrill.gameObject : null, false);
            SetActive(parentHeldDrill, true);
            parentAnimator?.ResetTrigger(ResetTrigger);
            parentAnimator?.SetTrigger(WorkTrigger);
            if (drillWorkAudio != null && drillWorkAudio.clip != null)
            {
                drillWorkAudio.Stop();
                drillWorkAudio.Play();
            }
            drillWorkRoutine = StartCoroutine(FinishParentDrillWorkAfterDelay());
        }

        private IEnumerator FinishParentDrillWorkAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.5f, drillWorkSeconds));
            drillWorkRoutine = null;
            EndParentDrillWork();
        }

        private void EndParentDrillWork()
        {
            if (drillWorkRoutine != null)
            {
                StopCoroutine(drillWorkRoutine);
                drillWorkRoutine = null;
            }
            if (drillWorkAudio != null)
                drillWorkAudio.Stop();
            SetActive(parentHeldDrill, false);
            SetActive(unsafeDrill != null ? unsafeDrill.gameObject : null, true);
            if (parentAnimator != null)
            {
                parentAnimator.ResetTrigger(WorkTrigger);
                parentAnimator.SetTrigger(ResetTrigger);
            }
        }

        private void ReturnParentHome()
        {
            if (parent == null)
                return;
            parent.position = parentHomePosition;
            parent.rotation = parentHomeRotation;
        }

        private void FaceParentTowards(Transform target)
        {
            if (parent == null || target == null)
                return;
            Vector3 direction = target.position - parent.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                parent.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void OnDisable()
        {
            EndParentDrillWork();
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
                target.SetActive(value);
        }
    }
}
