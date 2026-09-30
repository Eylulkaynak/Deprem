using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryPreparationDirector : MonoBehaviour
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

        [Header("Opening")]
        [SerializeField] private bool revisedFlow;
        [SerializeField] private StoryInteractable startFamilyPlan;
        [SerializeField] private StoryInteractable placeContactCard;
        [SerializeField] private StoryInteractable assignCanWhistleRole;
        [SerializeField] private StoryInteractable inspectEmptyBag;

        [Header("Environmental Discovery")]
        [SerializeField] private StoryInteractable discoverSignal;
        [SerializeField] private StoryInteractable discoverFood;
        [SerializeField] private StoryInteractable discoverHealth;
        [SerializeField] private StoryInteractable discoverWarmth;
        [SerializeField] private StoryInteractable inspectWaterDate;
        [SerializeField] private StoryInteractable inspectBandageSeal;

        [Header("Item Decisions")]
        [SerializeField] private StoryPreparationItem[] items;
        [SerializeField] private StoryInteractable[] signalDrawerItems;
        [SerializeField] private StoryInteractable reviewSignal;
        [SerializeField] private StoryInteractable reviewSignalFlashlightOff;
        [SerializeField] private StoryInteractable reviewSignalRadioBatteryInsert;
        [SerializeField] private StoryInteractable reviewSignalRadio;
        [SerializeField] private StoryInteractable reviewSignalRadioBatteryRemove;
        [SerializeField] private StoryInteractable reviewSignalWhistle;
        [SerializeField] private Transform signalFlashlightApproachPoint;
        [SerializeField] private GameObject signalRadioReviewRoot;
        [SerializeField] private GameObject signalRadioTuningBeforeRoot;
        [SerializeField] private GameObject signalWhistleTargetRoot;
        [SerializeField] private Transform signalWhistleCanPose;
        [SerializeField] private StoryInteractable reviewFood;
        [SerializeField] private StoryInteractable reviewHealth;
        [SerializeField] private StoryInteractable reviewWarmth;

        [Header("Final Bag Check")]
        [SerializeField] private StoryInteractable chooseComfortItem;
        [Tooltip("Rahatlatıcı eşya sahnesinde Can'ın yürüyeceği, Warmth kadrajında görünen poz.")]
        [SerializeField] private Transform comfortCanPose;
        [Tooltip("Final çanta yerleştirmeden önce Deniz'in yürüyeceği, ExitShelf kadrajında görünen zemin noktası.")]
        [SerializeField] private Transform exitShelfApproachPoint;
        [Tooltip("Raf üzerindeki görünür yeşil bırakma halkası; yalnız final sürükleme sırasında açık kalır.")]
        [SerializeField] private GameObject exitShelfDropRing;
        [SerializeField] private StoryInteractable testBagWeight;
        [SerializeField] private StoryInteractable removeConsole;
        [SerializeField] private StoryInteractable testBalancedBag;
        [SerializeField] private StoryInteractable adjustBagStraps;
        [SerializeField] private StoryInteractable placeBagAtExit;
        [SerializeField] private GameObject consoleConflictRoot;
        [SerializeField] private GameObject consoleInBagRoot;
        [SerializeField] private GameObject consoleReturnedRoot;
        [SerializeField] private GameObject openBagRoot;
        [SerializeField] private GameObject closedBagRoot;
        [SerializeField] private GameObject wornBagRoot;
        [SerializeField] private GameObject exitShelfBagRoot;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionDetail;

        private StoryPreparationCategory currentCategory;
        private Coroutine consequenceRoutine;
        private Coroutine signalPhaseRoutine;
        private Coroutine packingAdvanceRoutine;
        private Coroutine characterFacingRoutine;
        private Coroutine canRepositionRoutine;
        private bool categoryTransitionPending;
        private bool signalPackingPhase;
        private bool signalHandoffCanReady;
        private bool signalHandoffDialogueDone;
        private StoryPreparationItem activeFlashlightInspection;
        private StoryPreparationItem activeRadioInspection;
        private readonly HashSet<string> explainedPackingItems = new HashSet<string>();

        public StoryPreparationCategory CurrentCategory => currentCategory;
        public StoryPreparationItem[] Items => items;
        public bool RevisedFlow => revisedFlow;

        public bool TryBeginItemExplanation(StoryInteractable interaction)
        {
            if (!revisedFlow || categoryTransitionPending || interaction == null ||
                interaction.InteractionGesture != StoryInteractionGesture.DragToBag)
                return false;

            StoryPreparationItem item = (items ?? Array.Empty<StoryPreparationItem>())
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    candidate.Recommended &&
                    candidate.Interactable == interaction &&
                    candidate.Category == currentCategory);
            if (item == null || explainedPackingItems.Contains(item.ItemId) ||
                NextRequiredItem(currentCategory) != item)
                return false;

            if (string.Equals(item.ItemId, "Flashlight", StringComparison.Ordinal))
                return BeginSignalFlashlightInspection(item);
            if (string.Equals(item.ItemId, "Radio", StringComparison.Ordinal))
                return BeginSignalRadioInspection(item);

            SetCategoryItemInteractions(currentCategory, null);
            player?.Stop();
            player?.FaceTowards(interaction.transform.position);
            // Eşya açıklamalarında süs Inspect/Interact tetikleri yok: retarget
            // edilmiş tek seferlik klipler bu rig'lerde eğilmiş/bükülmüş pozlar
            // bırakıyordu. Konuşma vurgusunu ses senkronlu Talk sistemi taşır.
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ui?.ShowObjective(ItemExplanationTitle(item.ItemId), ItemPurpose(item.ItemId));
            ShowDialogue(
                ItemExplanationDialogue(item.ItemId),
                6f,
                () => CompleteItemExplanation(item));
            return true;
        }

        private void Start()
        {
            if (StoryGameManager.Instance != null)
                gameManager = StoryGameManager.Instance;
            gameManager?.BeginAct(StoryAct.Preparation);
            RestoreItemVisuals();
            DisableAllInteractions();
            SetBagState(true, false, false, false);
            SeedNaturalIdlePhases();
            if (completionPanel != null)
                completionPanel.SetActive(false);

            StoryCheckpoint checkpoint = gameManager?.CurrentState?.checkpoint ?? StoryCheckpoint.None;
            switch (checkpoint)
            {
                case StoryCheckpoint.PreparationComplete:
                    ShowCompletedState();
                    break;
                case StoryCheckpoint.BagFitted:
                    SetupExitPlacement();
                    break;
                case StoryCheckpoint.WarmthPacked:
                    if (revisedFlow)
                    {
                        if (gameManager == null || !gameManager.HasFlag(StoryFlag.BagComfortItem))
                            SetupConsoleConflict();
                        else
                            SetupFinalBagCheck();
                    }
                    else
                    {
                        SetupFinalBagCheck();
                    }
                    break;
                case StoryCheckpoint.HealthPacked:
                    StartCategory(StoryPreparationCategory.Warmth);
                    break;
                case StoryCheckpoint.FoodPacked:
                    StartCategory(StoryPreparationCategory.Health);
                    break;
                case StoryCheckpoint.CommunicationPacked:
                    StartCategory(StoryPreparationCategory.Food);
                    break;
                case StoryCheckpoint.BagInspected:
                    StartCategory(StoryPreparationCategory.Signal);
                    break;
                default:
                    StartOpening();
                    break;
            }
        }

        public void OnFamilyPlanStarted()
        {
            if (revisedFlow)
            {
                FaceEachOther(deniz, can);
                // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
                startFamilyPlan?.SetAvailable(false);
                placeContactCard?.SetAvailable(false);
                assignCanWhistleRole?.SetAvailable(false);
                inspectEmptyBag?.SetAvailable(false);
                cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
                ui?.ShowObjective(
                    "AİLE PLANINI TAMAMLA • 2/3",
                    "“Melek Teyze” kartındaki ipuçlarını oku ve doğru bölümü seç.");
                ShowDialogue(
                    "Deniz: Mahalle parkı binalardan uzak açık alan; bu yüzden toplanma yerimiz.\nAnne: Şimdi Melek Teyze kartını oku. Ankara'da yaşaması, planın hangi bölümüne ait olduğunu gösteriyor.",
                    8f,
                    () => placeContactCard?.SetAvailable(true));
                return;
            }

            FaceEachOther(deniz, parent);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            startFamilyPlan?.SetAvailable(false);
            inspectEmptyBag?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            ui?.ShowObjective("ÇANTAYI BİRLİKTE İNCELE", "Boş afet çantasının ceplerini ve taşıma biçimini kontrol et.");
            ShowDialogue("Anne: Bu çanta deprem olurken alınmak için değil; sarsıntı bittikten sonra güvenle çıkarken yanımıza almak için hazır duracak.",
                5.5f, BeginGuidedBagMove);
        }

        public void OnContactCardPlaced()
        {
            if (!revisedFlow)
                return;

            placeContactCard?.SetAvailable(false);
            assignCanWhistleRole?.SetAvailable(false);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            FaceEachOther(parent, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            ui?.ShowObjective(
                "AİLE PLANINI TAMAMLA • 3/3",
                "Can'ın sorumluluk kartını oku ve doğru bölüme yerleştir.");
            ShowDialogue(
                "Deniz: Melek Teyze başka şehirde olduğu için ortak iletişim kişimiz.\nAnne: Son kart Can'ın afet sırasında üstleneceği sorumluluğu anlatıyor; yazıyı okuyup başlığını bul.",
                7.5f,
                () => assignCanWhistleRole?.SetAvailable(true));
        }

        public void OnCanWhistleRolePlaced()
        {
            if (!revisedFlow)
                return;

            assignCanWhistleRole?.SetAvailable(false);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            FaceEachOther(deniz, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            ui?.ShowObjective(
                "ÇANTAYI HAZIRLAMAYA BAŞLA",
                "Çanta zaten açık. İlk olarak karanlıkta haberleşmeyi sağlayan parçaları bul.");
            ShowDialogue(
                "Deniz: Sarsıntı durmadan çantaya koşmak yok.\nCan: Önce yanında kalacağım. Sarsıntı durunca düdüğüm bende olacak.\nAnne: Çanta ancak planla birlikte işe yarar.",
                8f,
                OnBagInspected);
        }

        public void OnBagInspected()
        {
            inspectEmptyBag?.SetAvailable(false);
            SetBagState(true, false, false, false);
            gameManager?.CommitCheckpoint(StoryCheckpoint.BagInspected);
            DisableAllInteractions();
            if (revisedFlow)
            {
                StartCategory(StoryPreparationCategory.Signal);
                return;
            }
            ShowDialogue("Anne: Önce karanlıkta haber almayı ve birbirimizi bulmayı sağlayan parçaları seçelim.",
                4f, () => StartCategory(StoryPreparationCategory.Signal));
        }

        public void ResolveChoice(StoryPreparationItem item)
        {
            if (item == null || item.Category != currentCategory)
                return;

            if (item.Recommended)
            {
                if (revisedFlow && !explainedPackingItems.Contains(item.ItemId))
                {
                    item.LegacyBagMotion?.ReturnToStart();
                    item.Interactable?.SetAvailable(true);
                    ui?.ShowContext("Önce eşyaya dokunup ne işe yaradığını dinle.");
                    return;
                }

                // Eşyanın masadan çantaya hareketi StoryPreparationItem tarafından zaten
                // fiziksel olarak oynatılıyor. KayKit pickup klibini burada ikinci kez
                // tetiklemek Meshy çocuk riginde ayakları zemine gömüyordu.
                player?.Stop();
                if (openBagRoot != null)
                    player?.FaceTowards(openBagRoot.transform.position);
                item.Accept();
                if (item.Flag != StoryFlag.None)
                    gameManager?.SetFlag(item.Flag, true);
                // Kullanım açıklaması sürüklemeden önce oynatıldı. Yerleştirmeden sonra
                // yalnız kısa onay göster; nesne çantaya inmeden sıradakini açma.
                ui?.ShowContext($"ÇANTAYA EKLENDİ: {item.DisplayName}");
                if (revisedFlow)
                {
                    SetCategoryItemInteractions(item.Category, null);
                    if (packingAdvanceRoutine != null)
                        StopCoroutine(packingAdvanceRoutine);
                    float placementDelay = item.LegacyBagMotion != null
                        ? item.LegacyBagMotion.BagEntryDuration + 0.08f
                        : 0.35f;
                    packingAdvanceRoutine = StartCoroutine(
                        AdvancePackingAfterPlacement(item.Category, placementDelay));
                }
                else
                {
                    OnRecommendedChoiceDialogueCompleted(item.Category);
                }
                return;
            }

            FaceEachOther(deniz, parent);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            item.Reject();
            gameManager?.AddMistake();
            ui?.ShowSubtitle($"Deniz: {item.ChildLine}\nAnne: {item.ParentLine}", 7f);
            if (consequenceRoutine != null)
                StopCoroutine(consequenceRoutine);
            consequenceRoutine = StartCoroutine(ShowNearMissConsequence(item));
        }

        public void ReviewSignalCategory()
        {
            CompleteCategory(StoryPreparationCategory.Signal, StoryCheckpoint.CommunicationPacked,
                "Anne: Fener ve radyo pilleri ayrı poşette; düdük dış cepte. Elektrik kesilse bile haber alıp yerimizi belli edebiliriz.");
        }

        public void OnSignalFlashlightTested()
        {
            OnSignalFlashlightSwitchedOn();
        }

        public void OnSignalFlashlightSwitchedOn()
        {
            if (!revisedFlow)
            {
                ReviewSignalCategory();
                return;
            }

            reviewSignal?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationFlashlight);
            ui?.ShowObjective(
                "FENER AÇILDI",
                "Işık çalışıyor. Açık alev kullanmadan karanlıkta güvenli yolu görmemizi sağlar.");
            ui?.ShowContext("El feneri çalışıyor.");
            ShowDialogue(
                "Deniz: Açıldı! Elektrik kesilirse karanlıkta yolu bununla görebiliriz.\n" +
                "Anne: Evet. Mum yerine el feneri kullanır, yedek pilini de yanında tutarız.",
                5.8f,
                () =>
                {
                    if (activeFlashlightInspection == null)
                        return;
                    reviewSignalFlashlightOff?.SetAvailable(true);
                    ui?.ShowObjective(
                        "FENERİ KAPAT",
                        "Üstündeki düğmeye yeniden dokun; pili boşa harcamadan çantaya hazırlayalım.");
                    ui?.ShowContext("Şimdi aynı düğmeden feneri kapat.");
                });
        }

        public void OnSignalFlashlightSwitchedOff()
        {
            if (!revisedFlow || activeFlashlightInspection == null)
                return;

            reviewSignalFlashlightOff?.SetAvailable(false);
            StoryPreparationItem item = activeFlashlightInspection;
            activeFlashlightInspection = null;
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            touchManager?.SetInteractionsEnabled(true);
            touchManager?.SetWorldNavigationEnabled(true);
            CompleteItemExplanation(item);
            ui?.ShowContext("Fener kapatıldı ve çantaya yerleştirmeye hazır.");
        }

        public void OnSignalRadioBatteryInserted()
        {
            if (!revisedFlow || activeRadioInspection == null)
                return;

            reviewSignalRadioBatteryInsert?.SetAvailable(false);
            reviewSignalRadio?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationRadio);
            ui?.ShowObjective(
                "BEYAZ GÜÇ DÜĞMESİNE DOKUN",
                "Radyonun sağ üstünde, antenin yanındaki beyaz kare düğmeye doğrudan dokun.");
            ui?.ShowContext("Sarı ayar parçasına değil; antenin dibindeki beyaz kare düğmeye dokun.");
        }

        public void OnSignalRadioPowered()
        {
            if (!revisedFlow || activeRadioInspection == null)
                return;

            reviewSignalRadio?.SetAvailable(false);
            reviewSignalRadioBatteryRemove?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationRadio);
            ui?.ShowObjective(
                "RADYO ÇALIŞIYOR",
                "Resmî yayın duyuldu. Dinleme bitince aynı pili çıkarıp masadaki yerine geri koyacağız.");
            ShowDialogue(
                "Radyo: Acil durumlarda doğrulanmamış bilgileri paylaşmayın; resmî duyuruları takip edin.\n" +
                "Anne: Radyo çalışıyor. Şimdi kapatıp pili ayrı tutalım; böylece çantada boş yere tükenmez.",
                6.2f,
                () =>
                {
                    if (activeRadioInspection == null)
                        return;
                    reviewSignalRadioBatteryRemove?.SetAvailable(true);
                    ui?.ShowObjective(
                        "AYNI PİLİ GERİ ÇIKAR",
                        "Radyodaki pili tutup masadaki boş pil yerine geri sürükle.");
                    ui?.ShowContext("Radyoya taktığın pilin kendisini çıkar; yeni pil oluşmayacak.");
                });
        }

        public void OnSignalRadioTuned()
        {
            OnSignalRadioPowered();
        }

        public void OnSignalRadioBatteryRemoved()
        {
            if (!revisedFlow || activeRadioInspection == null)
                return;

            reviewSignalRadioBatteryRemove?.SetAvailable(false);
            ui?.ShowObjective(
                "RADYO TESTİ TAMAM",
                "Radyo çalıştı ve aynı yedek pil masaya döndü. Önce radyoyu, ardından pili açık çantaya yerleştir.");
            ShowDialogue(
                "Deniz: Yayını duydum; radyo çalışıyor.\n" +
                "Anne: Güzel. Aynı pili masaya geri aldık; şimdi radyoyu ve pili sırayla çantaya koyabiliriz.",
                4.8f,
                () =>
                {
                    StoryPreparationItem item = activeRadioInspection;
                    activeRadioInspection = null;
                    if (signalRadioReviewRoot != null)
                        signalRadioReviewRoot.SetActive(false);
                    if (item?.Interactable != null)
                        item.Interactable.gameObject.SetActive(true);
                    cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
                    touchManager?.SetInteractionsEnabled(true);
                    touchManager?.SetWorldNavigationEnabled(true);
                    CompleteItemExplanation(item);
                });
        }

        private void BeginSignalWhistleHandoff()
        {
            if (reviewSignalWhistle != null)
            {
                reviewSignalWhistle.gameObject.SetActive(true);
                reviewSignalWhistle.SetAvailable(true);
            }
            if (signalWhistleTargetRoot != null)
                signalWhistleTargetRoot.SetActive(true);

            cameraController?.ActivateZone(StoryCameraZoneId.PreparationSiblingHandoff);
            ui?.ShowObjective(
                "DÜDÜĞÜ CAN'A VER",
                "Soldaki küçük düdüğü tut; aynı ekrandaki Can'ın göğsündeki yeşil halkaya sürükleyip bırak.");
            ui?.ShowContext("Düdük ve hedef aynı kadrajda. Yeşil halka bırakma alanıdır.");
        }

        public void ReviewFoodCategory()
        {
            CompleteCategory(StoryPreparationCategory.Food, StoryCheckpoint.FoodPacked,
                "Anne: Su sızdırmaz bölümde, dayanıklı yiyecek yanında. Çantayı gereksiz ağırlaştırmadığımız için Deniz tek başına taşıyabilir.");
        }

        public void ReviewHealthCategory()
        {
            CompleteCategory(StoryPreparationCategory.Health, StoryCheckpoint.HealthPacked,
                "Anne: İlk yardım çantası yetişkin gözetiminde kullanılır. Belge kopyaları ve aile notu su geçirmez dosyada kalır.");
        }

        public void ReviewWarmthCategory()
        {
            CompleteCategory(StoryPreparationCategory.Warmth, StoryCheckpoint.WarmthPacked,
                "Anne: Hafif battaniye ve mevsime uygun yedek kıyafet sıcak kalmamıza yardım eder. Şimdi çantanın ağırlığını deneyelim.");
        }

        public void DiscoverSignalCategory()
        {
            RevealCategory(StoryPreparationCategory.Signal);
        }

        public void OnSignalItemStaged()
        {
            if (!revisedFlow || currentCategory != StoryPreparationCategory.Signal ||
                categoryTransitionPending)
                return;

            // Çekmece eşyası kendi sahne animasyonuyla masadaki yuvasına gider.
            // Karakter kadraj dışında olduğu için KayKit pickup klibini ayrıca
            // çalıştırmak yalnızca Meshy ayak yüksekliğini bozuyordu.
            SetRemainingSignalDrawerItemsAvailable();
            foreach (StoryPreparationItem item in items ?? Array.Empty<StoryPreparationItem>())
            {
                if (item != null && item.Category == StoryPreparationCategory.Signal)
                    item.Interactable?.SetAvailable(false);
            }

            int staged = CountStagedSignalDrawerItems();
            int required = CountRequired(StoryPreparationCategory.Signal);
            if (staged < required)
            {
                cameraController?.ActivateZone(StoryCameraZoneId.PreparationSignal);
                ui?.ShowObjective(
                    $"ÇEKMECEDEN GEREKLİLERİ SEÇ — {staged}/{required}",
                    "Fener, yedek pil, düdük ve radyoya dokun; seçtiklerin masaya gitsin.");
                ui?.ShowContext($"Masaya gönderildi. Çekmecede {required - staged} gerekli eşya kaldı.");
                return;
            }

            SetSignalDrawerItemsAvailable(false);
            ui?.ShowObjective(
                "SEÇİLENLER MASAYA GELİYOR",
                "Dört gerekli eşya hazır. Hepsi masaya yerleşince çantayı birlikte dolduracağız.");
            ui?.ShowContext("Gerekli parçaların hepsi seçildi.");
            if (signalPhaseRoutine != null)
                StopCoroutine(signalPhaseRoutine);
            signalPhaseRoutine = StartCoroutine(BeginSignalPackingAfterSelection());
        }

        public void DiscoverFoodCategory()
        {
            if (revisedFlow && inspectWaterDate != null)
            {
                discoverFood?.SetAvailable(false);
                cameraController?.ActivateZone(StoryCameraZoneId.PreparationFood);
                ui?.ShowObjective(
                    "DOLAP A\u00c7ILDI \u2014 \u0130\u00c7ER\u0130 BAK",
                    "Kapaklar a\u00e7\u0131l\u0131rken dolapta kal; su \u015fi\u015fesini ve dayan\u0131kl\u0131 g\u0131day\u0131 bul.");
                ShowDialogue(
                    "Deniz: Kapaklar a\u00e7\u0131ld\u0131; i\u00e7eride yedek su \u015fi\u015feleri duruyor. Masadaki \u015fi\u015fenin kapa\u011f\u0131na ve tarih etiketine yak\u0131ndan bakal\u0131m.",
                    4.6f,
                    () => BeginPrePackInspection(
                        StoryPreparationCategory.Food,
                        inspectWaterDate,
                        "SUYUN TAR\u0130H\u0130N\u0130 KONTROL ET",
                        "\u015ei\u015fenin kendi etiketini yana \u00e7evir; tarihi ve kapa\u011f\u0131 birlikte kontrol et.",
                        "Can: B\u00fcy\u00fck \u015fi\u015fe daha \u00e7ok su demek.\nDeniz: Ancak ta\u015f\u0131yabiliyorsak, kapa\u011f\u0131 sa\u011flamsa ve tarihi ge\u00e7memi\u015fse. Etiketi birlikte okuyal\u0131m."));
                return;
            }

            RevealCategory(StoryPreparationCategory.Food);
        }

        public void DiscoverHealthCategory()
        {
            if (revisedFlow)
            {
                BeginAutomaticBandageInspection();
                return;
            }

            RevealCategory(StoryPreparationCategory.Health);
        }

        private void BeginAutomaticBandageInspection()
        {
            if (currentCategory != StoryPreparationCategory.Health || categoryTransitionPending)
                return;

            DisablePrePackInspections();
            DiscoveryFor(StoryPreparationCategory.Health)?.SetAvailable(false);
            foreach (StoryPreparationItem item in items ?? Array.Empty<StoryPreparationItem>())
            {
                if (item != null)
                    item.SetAvailable(false);
            }

            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBandageInspection);
            ui?.ShowObjective(
                "KAPALI PAKET\u0130 G\u00d6ZLE KONTROL ET",
                "Kamera yakla\u015f\u0131rken paketin kenarlar\u0131n\u0131 ve a\u00e7\u0131lmam\u0131\u015f ambalaj\u0131n\u0131 incele.");
            ShowDialogue(
                "Can: \u0130lk yard\u0131m kutusundaki her \u015feyi biz mi kullanaca\u011f\u0131z?\n" +
                "Deniz: Hay\u0131r. Bu paketin kenarlar\u0131 a\u00e7\u0131lmam\u0131\u015f; ambalaj\u0131 sa\u011flam.\n" +
                "Anne: Do\u011fru. Malzemeyi a\u00e7madan sorumlu yeti\u015fkine ula\u015ft\u0131r\u0131yoruz.",
                6.2f,
                () => RevealCategory(StoryPreparationCategory.Health));
        }

        public void OnWaterDateChecked()
        {
            if (!revisedFlow || currentCategory != StoryPreparationCategory.Food)
                return;

            inspectWaterDate?.SetAvailable(false);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ShowDialogue(
                "Deniz: Tarih uygun, kapak sa\u011flam.\nCan: O zaman daha b\u00fcy\u00fck olan\u0131 de\u011fil, ta\u015f\u0131yabildi\u011fimiz \u015fi\u015feyi alal\u0131m.",
                5.5f,
                () => RevealCategory(StoryPreparationCategory.Food));
        }

        public void OnBandageSealChecked()
        {
            if (!revisedFlow || currentCategory != StoryPreparationCategory.Health)
                return;

            inspectBandageSeal?.SetAvailable(false);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ShowDialogue(
                "Deniz: M\u00fch\u00fcr a\u00e7\u0131lmam\u0131\u015f.\nAnne: G\u00fczel. \u0130la\u00e7 se\u00e7miyorsunuz; kapal\u0131 malzemeyi bulup sorumlu yeti\u015fkine veriyorsunuz.",
                5.5f,
                () => RevealCategory(StoryPreparationCategory.Health));
        }

        public void DiscoverWarmthCategory()
        {
            RevealCategory(StoryPreparationCategory.Warmth);
        }

        public void OnComfortItemChosen()
        {
            if (!revisedFlow)
                return;

            chooseComfortItem?.SetAvailable(false);
            gameManager?.SetFlag(StoryFlag.BagComfortItem, true);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            FaceEachOther(deniz, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationWarmth);
            ui?.ShowObjective(
                "CAN'IN KÜÇÜK SEÇİMİ",
                "Temel malzemelerin yerini almayan tek bir küçük rahatlatıcı eşya seçildi.");
            ShowDialogue(
                "Can: Küçük arabam dış cepte kalabilir mi?\nDeniz: Bir tane küçük şey olur. Korkarsan sana aile planını ve birlikte olduğumuzu hatırlatır.",
                6.5f,
                SetupFinalBagCheck);
        }

        public void OnBagWeightTested()
        {
            if (revisedFlow)
            {
                testBagWeight?.SetAvailable(false);
                removeConsole?.SetAvailable(false);
                SetBagState(false, false, false, false);
                if (consoleConflictRoot != null)
                    consoleConflictRoot.SetActive(true);
                // Do not force the KayKit PickUp/Waving one-shots here. Their limb proportions
                // do not retarget cleanly to the Meshy children: Deniz floated above the rug and
                // Can's ankle folded backwards. The speaker-aware dialogue system below already
                // gives both children a grounded, readable reaction using ChildTalkingIdle.
                cameraController?.ActivateZone(StoryCameraZoneId.PreparationWrongChoice);
                ui?.ShowObjective(
                    "AĞIRLIĞIN NEDENİNİ BUL",
                    "Çantanın ağzından görünen oyun konsolunu tutup masaya geri sürükle.");
                ShowDialogue(
                    "Deniz: Can, çanta birden ağırlaştı.\nCan: Evden çıkarsak konsol burada kalacak.\nDeniz: Ben de bazı şeyleri bırakmak istemem.\nAnne: Küçük bir hatıra taşıyabiliriz; ama taşıyamadığımız çanta kimseye yardım etmez.",
                    10f,
                    () => removeConsole?.SetAvailable(true));
                return;
            }

            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            SetBagState(false, false, true, false);
            testBagWeight?.SetAvailable(false);
            adjustBagStraps?.SetAvailable(false);
            FaceEachOther(deniz, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBagFit);
            ui?.ShowObjective("İKİ ASKINI AYARLA", "Çantayı iki omuza eşit dağıt; ellerin serbest kalsın.");
            ShowDialogue("Can: Ben de taşıyayım mı?\nAnne: Herkes yaşına uygun yük taşır. Can kendi düdüğünü bilir; ana çanta Deniz'in iki omzunda kalır.",
                7f, () => adjustBagStraps?.SetAvailable(true));
        }

        public void OnConsoleRemoved()
        {
            if (!revisedFlow)
                return;

            removeConsole?.SetAvailable(false);
            SetBagState(true, false, false, false);
            if (consoleConflictRoot != null)
                consoleConflictRoot.SetActive(false);
            if (consoleInBagRoot != null)
                consoleInBagRoot.SetActive(false);
            if (consoleReturnedRoot != null)
                consoleReturnedRoot.SetActive(true);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationWarmth);
            // Can repliğini söylerken oyuncağının yanına yürür; diyalog bittiğinde
            // Warmth kadrajının içinde, sürükleme hedefi olarak hazırdır.
            PositionCan(comfortCanPose, null);
            ShowDialogue(
                "Can: Küçük arabamı taşıyabilirim. Konsol eve göz kulak olsun.\nDeniz: Döndüğümüzde ilk sen açarsın.",
                6.5f,
                SetupComfortChoice);
        }

        public void OnBalancedBagWeightTested()
        {
            if (!revisedFlow)
                return;

            testBalancedBag?.SetAvailable(false);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            SetBagState(false, false, true, false);
            adjustBagStraps?.SetAvailable(false);
            FaceEachOther(deniz, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBagFit);
            ui?.ShowObjective("İKİ ASKINI AYARLA", "Çantayı iki omuza eşit dağıt; ellerin serbest kalsın.");
            ShowDialogue(
                "Can: Bu kez düşmedin.\nAnne: Ağırlık dengeli. Şimdi iki askıyı da ayarlayıp ellerini serbest bırak.",
                7f,
                () => adjustBagStraps?.SetAvailable(true));
        }

        public void OnBagStrapsAdjusted()
        {
            adjustBagStraps?.SetAvailable(false);
            placeBagAtExit?.SetAvailable(false);
            gameManager?.CommitCheckpoint(StoryCheckpoint.BagFitted);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationOverview);
            ui?.ShowObjective("ÇANTAYI GÜVENLİ RAFA BIRAK", "Çıkışı kapatmadan, kuru ve kolay hatırlanan alçak rafa yerleştir.");
            ShowDialogue("Anne: İki askı omuzda, eller serbest. Çanta ağır gelirse içindekileri yetişkinle yeniden düzenleriz.",
                5.5f, () => placeBagAtExit?.SetAvailable(true));
        }

        public void OnBagPlacedAtExit()
        {
            placeBagAtExit?.SetAvailable(false);
            if (exitShelfDropRing != null)
                exitShelfDropRing.SetActive(false);
            // Final sürükleme için kapatılan serbest dolaşım, kapanış ve karanlık
            // tatbikat akışına eski varsayılan durumuyla devredilir.
            touchManager?.SetWorldNavigationEnabled(true);
            SetBagState(false, false, false, true);
            gameManager?.SetFlag(StoryFlag.BagReady, true);
            gameManager?.CompleteAct(StoryAct.Preparation);
            gameManager?.CommitCheckpoint(StoryCheckpoint.PreparationComplete);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationExitShelf);
            ui?.ShowObjective("1. PERDE TAMAMLANDI", "Afet çantası aile planındaki yerinde; çıkış yolu açık.");
            if (completionPanel != null)
                completionPanel.SetActive(false);
            if (completionDetail != null)
            {
                int corrected = gameManager?.CurrentState?.mistakeCount ?? 0;
                completionDetail.text = corrected == 0
                    ? "Işık, haberleşme, su, gıda, sağlık, belge ve sıcak kalma parçaları ailece kontrol edildi."
                    : $"Tüm temel parçalar ailece kontrol edildi. {corrected} riskli seçim, gerekçesi görülerek güvenle düzeltildi.";
            }
            ShowDialogue("Anne: Deprem sırasında bu rafa koşmuyoruz. Sarsıntı durur, birbirimizi kontrol eder, sonra güvenle çıkarken çantayı alırız.",
                8f, ShowCompletionCard);
        }

        private void StartOpening()
        {
            gameManager?.CommitCheckpoint(StoryCheckpoint.PreparationStart);
            currentCategory = StoryPreparationCategory.Signal;
            // Aile planı açılışında da bolum1'in gerçek, açık çantası sahnede kalır.
            // Paralel bir kapalı çanta modeli göstermiyoruz; oyuncu dokunduğu aynı çantayı doldurur.
            SetBagState(true, false, false, false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationOverview, true);
            startFamilyPlan?.SetAvailable(false);
            ui?.ShowObjective(
                "AİLE PLANINI TAMAMLA • 1/3",
                "“Mahalle Parkı” kartını oku; panodaki anlamca doğru bölüme sürükle.");
            if (revisedFlow)
            {
                ShowDialogue(
                    "Can: Oyuncak arabam da çantaya girebilir mi?\nAnne: Önce aile planını ve gerçekten gerekli malzemeleri hazırlayalım. Yer kalırsa bir küçük eşya seçeriz.\nDeniz: İlk kart Mahalle Parkı; üzerindeki bilgiyi okuyup doğru başlığa taşıyalım.",
                    10f,
                    () =>
                    {
                        cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
                        startFamilyPlan?.SetAvailable(true);
                    });
            }
            else
            {
                ShowDialogue("Sakin bir aile öğleden sonrası. Anne çantayı yere açtı; Deniz ve Can hangi parçaların gerçekten işe yarayacağını birlikte öğrenecek.",
                    4.5f, () => startFamilyPlan?.SetAvailable(true));
            }
        }

        private void StartCategory(StoryPreparationCategory category)
        {
            if (packingAdvanceRoutine != null)
            {
                StopCoroutine(packingAdvanceRoutine);
                packingAdvanceRoutine = null;
            }
            currentCategory = category;
            categoryTransitionPending = false;
            signalPackingPhase = false;
            DisableCategoryDiscoveries();
            DisableCategoryReviews();
            DisablePrePackInspections();
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item == null)
                    continue;
                bool accepted = item.Recommended && item.Flag != StoryFlag.None && gameManager != null && gameManager.HasFlag(item.Flag);
                if (!revisedFlow && item.Category == category && !accepted)
                    item.SetAvailable(true);
                else
                    item.SetAvailable(false);
            }

            cameraController?.ActivateZone(CategoryCamera(category));
            if (revisedFlow)
            {
                DiscoveryFor(category)?.SetAvailable(true);
                ui?.ShowObjective(
                    CategoryDiscoveryTitle(category),
                    CategoryDiscoveryDetail(category));
            }
            else
            {
                UpdateCategoryObjective();
            }
        }

        private void UpdateCategoryObjective()
        {
            int required = CountRequired(currentCategory);
            if (revisedFlow && currentCategory == StoryPreparationCategory.Signal && !signalPackingPhase)
            {
                int staged = CountStagedSignalDrawerItems();
                ui?.ShowObjective(
                    $"ÇEKMECEDEN GEREKLİLERİ SEÇ — {staged}/{required}",
                    "Fener, yedek pil, düdük ve radyoya dokun; önce hepsini masaya gönder.");
                return;
            }

            int selected = CountAccepted(currentCategory);
            if (selected >= required)
            {
                if (revisedFlow)
                    BeginPhysicalCategoryReview(currentCategory);
                else
                    BeginAutomaticCategoryReview(currentCategory);
                return;
            }

            if (revisedFlow)
            {
                StoryPreparationItem next = NextRequiredItem(currentCategory);
                SetCategoryItemInteractions(currentCategory, next);
                if (next != null)
                {
                    bool explained = explainedPackingItems.Contains(next.ItemId);
                    ui?.ShowObjective(
                        explained ? ItemPackingTitle(next.ItemId) : ItemExplanationTitle(next.ItemId),
                        explained
                            ? $"{next.DisplayName} nesnesini tut; açık çantanın ağzına sürükleyip bırak."
                            : ItemPurpose(next.ItemId) + " Öğrenmek için nesneye dokun.");
                }
                return;
            }

            ui?.ShowObjective($"{CategoryTitle(currentCategory)} — {selected}/{required}", CategoryDetail(currentCategory));
        }

        private void CompleteCategory(StoryPreparationCategory category, StoryCheckpoint checkpoint, string subtitle)
        {
            if (categoryTransitionPending)
                return;

            if (currentCategory != category || !CategoryComplete(category))
            {
                ui?.ShowContext("Bu bölümde gerekli parçalar henüz tamamlanmadı.");
                return;
            }

            categoryTransitionPending = true;
            ReviewFor(category)?.SetAvailable(false);
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item != null && item.Category == category)
                    item.SetAvailable(false);
            }
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            FaceEachOther(deniz, parent);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            gameManager?.CommitCheckpoint(checkpoint);
            ui?.ShowObjective(CategoryTitle(category) + " — TAMAM", "Anne son yerleşimi kontrol ediyor; ardından evin sıradaki bölümüne geçilecek.");
            ShowDialogue(subtitle, 5.5f, () => AdvanceAfterCategoryReview(category));
        }

        private void OnRecommendedChoiceDialogueCompleted(StoryPreparationCategory category)
        {
            if (currentCategory != category || categoryTransitionPending)
                return;

            UpdateCategoryObjective();
        }

        private void CompleteItemExplanation(StoryPreparationItem item)
        {
            if (item == null || currentCategory != item.Category || categoryTransitionPending ||
                IsAccepted(item))
                return;

            explainedPackingItems.Add(item.ItemId);
            player?.Stop();
            if (openBagRoot != null)
                player?.FaceTowards(openBagRoot.transform.position);
            SetCategoryItemInteractions(item.Category, item);
            ui?.ShowObjective(
                ItemPackingTitle(item.ItemId),
                $"{item.DisplayName} nesnesini tut; açık çantanın ağzına sürükleyip bırak.");
            ui?.ShowContext("Şimdi aynı eşyayı çantaya yerleştir.");
        }

        private IEnumerator AdvancePackingAfterPlacement(
            StoryPreparationCategory category,
            float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            packingAdvanceRoutine = null;
            if (currentCategory == category && !categoryTransitionPending)
                UpdateCategoryObjective();
        }

        private StoryPreparationItem NextRequiredItem(StoryPreparationCategory category)
        {
            return (items ?? Array.Empty<StoryPreparationItem>())
                .FirstOrDefault(item =>
                    item != null &&
                    item.Category == category &&
                    item.Recommended &&
                    !IsAccepted(item));
        }

        private bool IsAccepted(StoryPreparationItem item)
        {
            return item != null &&
                   item.Recommended &&
                   item.Flag != StoryFlag.None &&
                   gameManager != null &&
                   gameManager.HasFlag(item.Flag);
        }

        private void SetCategoryItemInteractions(
            StoryPreparationCategory category,
            StoryPreparationItem activeItem)
        {
            foreach (StoryPreparationItem item in items ?? Array.Empty<StoryPreparationItem>())
            {
                if (item == null || item.Category != category || item.Interactable == null)
                    continue;
                item.Interactable.SetAvailable(item == activeItem && !IsAccepted(item));
            }
        }

        private void BeginPrePackInspection(
            StoryPreparationCategory category,
            StoryInteractable interaction,
            string title,
            string detail,
            string dialogue)
        {
            if (currentCategory != category || categoryTransitionPending || interaction == null)
                return;

            DisablePrePackInspections();
            DiscoveryFor(category)?.SetAvailable(false);
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item?.Interactable == null)
                    continue;

                bool ownsInspectionObject =
                    interaction.transform == item.Interactable.transform ||
                    interaction.transform.IsChildOf(item.Interactable.transform);
                if (ownsInspectionObject)
                {
                    // The inspection target lives on the real bottle. Keep that same physical
                    // object visible while disabling its later bag-drag interaction.
                    item.Interactable.gameObject.SetActive(true);
                    item.Interactable.SetAvailable(false);
                }
                else
                {
                    item.SetAvailable(false);
                }
            }

            cameraController?.ActivateZone(interaction.FocusCameraZone);
            ui?.ShowObjective(title, detail);
            ShowDialogue(dialogue, 5.5f, () => interaction.SetAvailable(true));
        }

        private void RevealCategory(StoryPreparationCategory category)
        {
            if (!revisedFlow || currentCategory != category || categoryTransitionPending)
                return;

            DisablePrePackInspections();
            DiscoveryFor(category)?.SetAvailable(false);
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item == null)
                    continue;
                bool accepted = item.Recommended && item.Flag != StoryFlag.None &&
                                gameManager != null && gameManager.HasFlag(item.Flag);
                item.SetAvailable(
                    item.Category == category &&
                    category != StoryPreparationCategory.Signal &&
                    !accepted);
            }
            SetSignalDrawerItemsAvailable(category == StoryPreparationCategory.Signal);

            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            cameraController?.ActivateZone(
                category == StoryPreparationCategory.Signal
                    ? StoryCameraZoneId.PreparationSignal
                    : StoryCameraZoneId.PreparationBag);
            ui?.ShowContext(CategoryDiscoveryResult(category));
            UpdateCategoryObjective();
        }

        private void BeginPhysicalCategoryReview(StoryPreparationCategory category)
        {
            if (category == StoryPreparationCategory.Signal)
            {
                SetSignalDrawerItemsAvailable(false);
                reviewSignal?.SetAvailable(false);
                reviewSignalFlashlightOff?.SetAvailable(false);
                reviewSignalRadioBatteryInsert?.SetAvailable(false);
                reviewSignalRadio?.SetAvailable(false);
                reviewSignalRadioBatteryRemove?.SetAvailable(false);
                if (signalRadioReviewRoot != null)
                    signalRadioReviewRoot.SetActive(false);
                if (reviewSignalWhistle != null)
                    reviewSignalWhistle.gameObject.SetActive(false);
                if (signalWhistleTargetRoot != null)
                    signalWhistleTargetRoot.SetActive(false);
                // Can yürürken diyalog paralel akar; ölü bekleme olmaz. Sürükleme
                // ancak hem Can pozuna varınca hem konuşma bitince açılır.
                signalHandoffCanReady = false;
                signalHandoffDialogueDone = false;
                PositionCanForSignalHandoff(() =>
                {
                    signalHandoffCanReady = true;
                    TryBeginSignalWhistleHandoff();
                });
                PresentSignalWhistleDialogue();
                return;
            }

            // Food, health and warmth already end with direct manipulation of the
            // real bottle label, sealed first-aid pack and packed clothing. Do not
            // append invisible "close a pocket/zipper" hotspots that the bag model
            // cannot physically show.
            switch (category)
            {
                case StoryPreparationCategory.Food:
                    ReviewFoodCategory();
                    return;
                case StoryPreparationCategory.Health:
                    ReviewHealthCategory();
                    return;
                case StoryPreparationCategory.Warmth:
                    ReviewWarmthCategory();
                    return;
            }

            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item != null && item.Category == category)
                    item.SetAvailable(false);
            }

            StoryInteractable review = ReviewFor(category);
            review?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ui?.ShowObjective(
                CategoryReviewTitle(category),
                CategoryReviewDetail(category));
        }

        private bool BeginSignalFlashlightInspection(StoryPreparationItem item)
        {
            if (item == null || signalFlashlightApproachPoint == null || reviewSignal == null ||
                reviewSignalFlashlightOff == null)
                return false;

            activeFlashlightInspection = item;
            SetCategoryItemInteractions(item.Category, null);
            reviewSignal.SetAvailable(false);
            reviewSignalFlashlightOff.SetAvailable(false);
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            ui?.ShowObjective(
                "FENERİN YANINA GİT",
                "Deniz masadaki el fenerine yaklaşacak; sonra üstündeki düğmeyi doğrudan deneyeceksin.");

            bool moving = player != null &&
                          player.MoveTo(
                              signalFlashlightApproachPoint,
                              item.Interactable.transform.position,
                              PresentSignalFlashlightSwitch);
            if (!moving)
                PresentSignalFlashlightSwitch();
            return true;
        }

        private void PresentSignalFlashlightSwitch()
        {
            if (activeFlashlightInspection == null)
                return;

            player?.FaceTowards(activeFlashlightInspection.Interactable.transform.position);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationFlashlight);
            touchManager?.SetInteractionsEnabled(true);
            touchManager?.SetWorldNavigationEnabled(false);
            reviewSignal?.SetAvailable(true);
            ui?.ShowObjective(
                "FENERİ DENE",
                "Yakın plandaki el fenerinin üstündeki küçük düğmeye dokun.");
            ui?.ShowContext("Düğme fenerin üstünde; doğrudan nesneye dokun.");
        }

        private bool BeginSignalRadioInspection(StoryPreparationItem item)
        {
            if (item == null || signalRadioReviewRoot == null ||
                reviewSignalRadioBatteryInsert == null || reviewSignalRadio == null ||
                reviewSignalRadioBatteryRemove == null)
                return false;

            activeRadioInspection = item;
            SetCategoryItemInteractions(item.Category, null);
            item.Interactable.gameObject.SetActive(false);
            signalRadioReviewRoot.SetActive(true);
            if (signalRadioTuningBeforeRoot != null)
                signalRadioTuningBeforeRoot.SetActive(true);
            reviewSignalRadio.SetAvailable(false);
            reviewSignalRadioBatteryRemove.SetAvailable(false);
            reviewSignalRadioBatteryInsert.SetAvailable(true);
            touchManager?.SetInteractionsEnabled(true);
            touchManager?.SetWorldNavigationEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationRadio);
            // Tek seferlik jest tetiği yok: retarget klipler Meshy rig'de pozu bozuyor.
            ui?.ShowObjective(
                "YEDEK PİLİ RADYOYA TAK",
                "Masadaki yedek pili tutup radyonun sağ yan yüzeyine sürükle.");
            ui?.ShowContext("Pili radyonun sağ yanına bırak; yerine kendiliğinden oturur.");
            return true;
        }

        private void PositionCanForSignalHandoff(Action completed)
        {
            PositionCan(signalWhistleCanPose, completed);
        }

        private void PositionCan(Transform pose, Action completed)
        {
            if (can == null || pose == null)
            {
                completed?.Invoke();
                return;
            }

            can.gameObject.SetActive(true);
            if (canRepositionRoutine != null)
                StopCoroutine(canRepositionRoutine);
            canRepositionRoutine = StartCoroutine(SmoothPositionCan(pose, completed));
        }

        private IEnumerator SmoothPositionCan(Transform pose, Action completed)
        {
            Vector3 startPosition = can.position;
            Quaternion startRotation = can.rotation;
            Vector3 targetPosition = pose.position;
            Quaternion targetRotation = pose.rotation;
            Vector3 travelDirection = targetPosition - startPosition;
            travelDirection.y = 0f;
            bool hasTravel = travelDirection.sqrMagnitude > 0.0025f;
            Quaternion travelRotation = hasTravel
                ? Quaternion.LookRotation(travelDirection.normalized, Vector3.up)
                : targetRotation;
            // Gerçek çocuk yürüyüş hızı. Eski 0.9 sn tavanı 4-5 metrelik yolu
            // kayarak ışınlanmaya çeviriyordu.
            float distance = Vector3.Distance(startPosition, targetPosition);
            float duration = Mathf.Clamp(distance / 1.35f, 0.38f, 4.2f);
            // Animator'ın Speed parametresi oyuncu hareketiyle aynı birimde (m/sn)
            // çalışır; gerçek lerp hızını vermek ayak kaymasını önler.
            float animatorSpeed = duration > 0.01f ? distance / duration : 0f;
            float elapsed = 0f;
            if (hasTravel && canAnimator != null && canAnimator.gameObject.activeInHierarchy)
            {
                // Önceki beat'ten kalan Interact/Talk pozu yürüyüşe bükülmüş kollarla
                // taşınmasın; Can temiz Locomotion durumunda yola çıkar.
                canAnimator.Play("Locomotion", 0, 0f);
                canAnimator.Update(0f);
            }
            canAnimator?.SetFloat("Speed", hasTravel ? animatorSpeed : 0f);

            while (elapsed < duration && can != null && pose != null)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                if (hasTravel)
                {
                    float travel = Mathf.Clamp01(normalized / 0.72f);
                    float travelEased = travel * travel * (3f - 2f * travel);
                    can.position = Vector3.Lerp(startPosition, targetPosition, travelEased);
                    can.rotation = travel < 1f
                        ? Quaternion.Slerp(startRotation, travelRotation, Mathf.Clamp01(travel * 2.5f))
                        : Quaternion.Slerp(
                            travelRotation,
                            targetRotation,
                            Mathf.SmoothStep(0f, 1f, (normalized - 0.72f) / 0.28f));
                    canAnimator?.SetFloat("Speed", travel < 1f ? animatorSpeed : 0f);
                }
                else
                {
                    float eased = normalized * normalized * (3f - 2f * normalized);
                    can.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                    canAnimator?.SetFloat("Speed", 0f);
                }
                yield return null;
            }

            if (can != null)
            {
                can.SetPositionAndRotation(targetPosition, targetRotation);
                canAnimator?.SetFloat("Speed", 0f);
                // Varışta ekstra Interact tetiği yok: retarget edilmiş tek seferlik
                // klipler Meshy rig'de bükülmüş kol pozu bırakıyordu.
            }
            canRepositionRoutine = null;
            completed?.Invoke();
        }

        private void TryBeginSignalWhistleHandoff()
        {
            if (signalHandoffCanReady && signalHandoffDialogueDone)
                BeginSignalWhistleHandoff();
        }

        private void PresentSignalWhistleDialogue()
        {
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationSiblingHandoff);
            ui?.ShowObjective(
                "CAN'I DİNLE",
                "Can masaya geliyor. Konuşmayı ilerlet; sonra masadaki düdüğü göğsündeki yeşil klipse sürükle.");
            ShowDialogue(
                "Can: Düdük benim görevimdi. Acil durumda üç kısa kez çalacağım.\n" +
                "Anne: Evet, düdüğü şimdi Can'ın klipsine takalım.",
                5.2f,
                () =>
                {
                    signalHandoffDialogueDone = true;
                    TryBeginSignalWhistleHandoff();
                });
            // Deniz is only listening in this shot. Skip the outgoing inspection clip
            // immediately so its retargeted transition cannot leave both shoes floating.
            if (denizAnimator != null && denizAnimator.gameObject.activeInHierarchy)
            {
                denizAnimator.Play("Locomotion", 0, 0f);
                denizAnimator.Update(0f);
            }
        }

        private void BeginAutomaticCategoryReview(StoryPreparationCategory category)
        {
            switch (category)
            {
                case StoryPreparationCategory.Signal:
                    ReviewSignalCategory();
                    break;
                case StoryPreparationCategory.Food:
                    ReviewFoodCategory();
                    break;
                case StoryPreparationCategory.Health:
                    ReviewHealthCategory();
                    break;
                default:
                    ReviewWarmthCategory();
                    break;
            }
        }

        private void BeginGuidedBagMove()
        {
            inspectEmptyBag?.SetAvailable(false);
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);

            bool movementStarted = player != null && inspectEmptyBag != null &&
                player.MoveTo(inspectEmptyBag.InteractionPoint, inspectEmptyBag.transform.position, EndGuidedBagMove);
            if (!movementStarted)
                EndGuidedBagMove();
        }

        private IEnumerator BeginSignalPackingAfterSelection()
        {
            yield return new WaitForSeconds(0.82f);
            signalPhaseRoutine = null;
            if (currentCategory != StoryPreparationCategory.Signal || categoryTransitionPending)
                yield break;

            signalPackingPhase = true;
            foreach (StoryPreparationItem item in items ?? Array.Empty<StoryPreparationItem>())
            {
                if (item == null || item.Category != StoryPreparationCategory.Signal)
                    continue;

                bool accepted = item.Recommended && item.Flag != StoryFlag.None &&
                                gameManager != null && gameManager.HasFlag(item.Flag);
                item.SetAvailable(!accepted);
            }
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ui?.ShowContext("Seçim tamamlandı. Her parçayı önce tanıyıp sonra çantaya yerleştireceğiz.");
            UpdateCategoryObjective();
        }

        private void EndGuidedBagMove()
        {
            touchManager?.SetWorldNavigationEnabled(true);
            touchManager?.SetInteractionsEnabled(true);
            inspectEmptyBag?.SetAvailable(true);
        }

        private void AdvanceAfterCategoryReview(StoryPreparationCategory category)
        {
            categoryTransitionPending = false;
            switch (category)
            {
                case StoryPreparationCategory.Signal:
                    StartCategory(StoryPreparationCategory.Food);
                    break;
                case StoryPreparationCategory.Food:
                    StartCategory(StoryPreparationCategory.Health);
                    break;
                case StoryPreparationCategory.Health:
                    StartCategory(StoryPreparationCategory.Warmth);
                    break;
                default:
                    if (revisedFlow && (gameManager == null ||
                                        !gameManager.HasFlag(StoryFlag.BagComfortItem)))
                        SetupConsoleConflict();
                    else
                        SetupFinalBagCheck();
                    break;
            }
        }

        private void SetupConsoleConflict()
        {
            DisableAllInteractions();
            SetBagState(true, false, false, false);
            if (consoleConflictRoot != null)
                consoleConflictRoot.SetActive(false);
            if (consoleInBagRoot != null)
                consoleInBagRoot.SetActive(false);
            if (consoleReturnedRoot != null)
                consoleReturnedRoot.SetActive(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ui?.ShowObjective(
                "ÇANTAYI KALDIRMAYI DENE",
                "Can son bir eşya ekledi. Konuşma bitince kamera çantayı gösterecek.");
            ShowDialogue(
                "Can: Temel malzemeler tamam. Oyun konsolunu da koydum; oyuncak arabamdan daha eğlenceli.\nAnne: Önce çantanın taşıyabildiğimiz ağırlıkta kalıp kalmadığını birlikte görelim.",
                7f,
                () =>
                {
                    SetBagState(true, false, false, false);
                    if (consoleConflictRoot != null)
                        consoleConflictRoot.SetActive(false);
                    if (consoleInBagRoot != null)
                        consoleInBagRoot.SetActive(true);
                    cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
                    ui?.ShowObjective(
                        "ÇANTAYI YUKARI KALDIR",
                        "Görünen çantayı sapından tut; yukarıdaki hedefe sürükleyip bırak.");
                    testBagWeight?.SetAvailable(true);
                });
        }

        private void SetupComfortChoice()
        {
            DisableAllInteractions();
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationWarmth);
            ui?.ShowObjective(
                "CAN DA PLANA KATILIYOR",
                "Can'ın küçük oyuncak arabasını doğrudan ona doğru çek; temel malzemeler çantada kalacak.");
            // Can sinyal aşamasında masanın yanına taşınmıştı. "Ona doğru çek" hedefi
            // Can'ı takip ettiği için önce Can kadraja, oyuncağının yanına yürür;
            // etkileşim ancak hedef gerçekten görünür olduğunda açılır.
            PositionCan(comfortCanPose, () => chooseComfortItem?.SetAvailable(true));
            ShowDialogue(
                "Anne: Başta konuştuğumuz küçük eşya için şimdi yer var.\nCan: O zaman yalnızca oyuncak arabamı seçiyorum; konsol evde kalacak.",
                5.5f);
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

        private void SeedNaturalIdlePhases()
        {
            PlayIdlePhase(denizAnimator, "Locomotion", 0.14f);
            PlayIdlePhase(canAnimator, "Locomotion", 0.58f);
            PlayIdlePhase(parentAnimator, "Adult Idle", 0.34f);
        }

        private static void PlayIdlePhase(Animator animator, string stateName, float phase)
        {
            if (animator == null)
                return;
            int stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
                return;
            animator.SetFloat("Speed", 0f);
            animator.Play(stateHash, 0, Mathf.Repeat(phase, 1f));
        }

        private void OnDisable()
        {
            if (characterFacingRoutine != null)
            {
                StopCoroutine(characterFacingRoutine);
                characterFacingRoutine = null;
            }
            if (canRepositionRoutine != null)
            {
                StopCoroutine(canRepositionRoutine);
                canRepositionRoutine = null;
            }
            canAnimator?.SetFloat("Speed", 0f);
        }

        private void ShowCompletionCard()
        {
            if (completionPanel != null)
                completionPanel.SetActive(true);
        }

        private void SetupFinalBagCheck()
        {
            DisableAllInteractions();
            SetBagState(false, false, true, false);
            if (revisedFlow)
            {
                if (consoleConflictRoot != null)
                    consoleConflictRoot.SetActive(false);
                if (consoleInBagRoot != null)
                    consoleInBagRoot.SetActive(false);
                if (consoleReturnedRoot != null)
                    consoleReturnedRoot.SetActive(true);
            }
            gameManager?.CommitCheckpoint(StoryCheckpoint.BagFitted);
            // Sırttaki çanta ile raf aynı sabit kadraja sığmıyor; önce takip kamerası
            // altında Deniz rafın yanına yürür, sonra ExitShelf kadrajında hem çanta
            // hem bırakma hedefi birlikte görünürken sürükleme açılır.
            touchManager?.SetInteractionsEnabled(false);
            touchManager?.SetWorldNavigationEnabled(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationOverview);
            ui?.ShowObjective(
                "ÇANTAYI GÜVENLİ RAFA TAŞI",
                "Deniz çıkış rafına yürüyecek; ardından sırtındaki çantayı rafa sürükleyip bırakacaksın.");
            ShowDialogue(
                "Deniz: Konsol masada, küçük araba dış cepte. Çanta artık dengeli.\n" +
                "Anne: İki askı omzunda ve ellerin serbest. Şimdi çantayı çıkışı kapatmayacak alçak rafa bırak.",
                6.5f,
                BeginExitShelfWalk);
        }

        private void BeginExitShelfWalk()
        {
            bool moving = player != null && exitShelfApproachPoint != null &&
                          player.MoveTo(exitShelfApproachPoint, PresentExitShelfPlacement);
            if (moving)
                return;

            // Rota kurulamazsa oyuncuyu kilitli bırakma; karakteri doğrudan rafın
            // yanına al ki final adım her koşulda oynanabilir kalsın.
            if (player != null && exitShelfApproachPoint != null)
            {
                player.Warp(exitShelfApproachPoint.position);
                player.FaceTowards(exitShelfApproachPoint.position + exitShelfApproachPoint.forward);
            }
            PresentExitShelfPlacement();
        }

        private void PresentExitShelfPlacement()
        {
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationExitShelf);
            touchManager?.SetInteractionsEnabled(true);
            placeBagAtExit?.SetAvailable(true);
            if (exitShelfDropRing != null)
                exitShelfDropRing.SetActive(true);
            ui?.ShowObjective(
                "ÇANTAYI GÜVENLİ RAFA BIRAK",
                "Sırtındaki çantayı tutup raftaki yeşil halkaya sürükleyip bırak.");
            ui?.ShowContext("Çanta Deniz'in sırtında; tut ve yeşil halkaya sürükle.");
        }

        private void SetupExitPlacement()
        {
            DisableAllInteractions();
            SetBagState(false, false, true, false);
            // Checkpoint'ten dönüşte de aynı oynanabilir kadraj kurulur: Deniz raf
            // yanında, çanta ve bırakma hedefi aynı ekranda.
            touchManager?.SetWorldNavigationEnabled(false);
            touchManager?.SetInteractionsEnabled(true);
            if (player != null && exitShelfApproachPoint != null)
            {
                player.Warp(exitShelfApproachPoint.position);
                player.FaceTowards(exitShelfApproachPoint.position + exitShelfApproachPoint.forward);
            }
            placeBagAtExit?.SetAvailable(true);
            if (exitShelfDropRing != null)
                exitShelfDropRing.SetActive(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationExitShelf, true);
            ui?.ShowObjective("ÇANTAYI GÜVENLİ RAFA BIRAK", "Sırtındaki çantayı raftaki yeşil halkaya sürükleyip bırak.");
        }

        private void ShowCompletedState()
        {
            DisableAllInteractions();
            SetBagState(false, false, false, true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationExitShelf, true);
            ui?.ShowObjective("1. PERDE TAMAMLANDI", "Afet çantası aile planındaki yerinde; çıkış yolu açık.");
            if (completionPanel != null)
                completionPanel.SetActive(true);
        }

        private IEnumerator ShowNearMissConsequence(StoryPreparationItem item)
        {
            if (revisedFlow)
                SetBagState(false, false, false, false);
            if (item.ConsequenceRoot != null)
                item.ConsequenceRoot.SetActive(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationWrongChoice);
            yield return new WaitForSeconds(3.1f);
            if (item != null && item.ConsequenceRoot != null)
                item.ConsequenceRoot.SetActive(false);
            if (revisedFlow)
                SetBagState(true, false, false, false);
            cameraController?.ActivateZone(CategoryCamera(currentCategory));
            consequenceRoutine = null;
        }

        private void RestoreItemVisuals()
        {
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item == null)
                    continue;
                bool accepted = item.Recommended && item.Flag != StoryFlag.None && gameManager != null && gameManager.HasFlag(item.Flag);
                item.Restore(accepted);
            }
            RestoreSignalDrawerItems();
        }

        private void RestoreSignalDrawerItems()
        {
            foreach (StoryInteractable interaction in signalDrawerItems ?? Array.Empty<StoryInteractable>())
            {
                if (interaction == null)
                    continue;
                StoryPreparationItem item = SignalItemFor(interaction);
                bool accepted = item != null && item.Flag != StoryFlag.None &&
                                gameManager != null && gameManager.HasFlag(item.Flag);
                interaction.gameObject.SetActive(!accepted);
                if (!accepted)
                    interaction.ResetInteraction();
                interaction.SetAvailable(false);
            }
        }

        private void SetSignalDrawerItemsAvailable(bool available)
        {
            foreach (StoryInteractable interaction in signalDrawerItems ?? Array.Empty<StoryInteractable>())
            {
                if (interaction == null)
                    continue;
                StoryPreparationItem item = SignalItemFor(interaction);
                bool accepted = item != null && item.Flag != StoryFlag.None &&
                                gameManager != null && gameManager.HasFlag(item.Flag);
                if (accepted)
                {
                    interaction.SetAvailable(false);
                    interaction.gameObject.SetActive(false);
                    continue;
                }

                if (available && !interaction.gameObject.activeSelf)
                    interaction.gameObject.SetActive(true);
                interaction.SetAvailable(available);
            }
        }

        private void SetRemainingSignalDrawerItemsAvailable()
        {
            foreach (StoryInteractable interaction in signalDrawerItems ?? Array.Empty<StoryInteractable>())
            {
                if (interaction == null)
                    continue;
                interaction.SetAvailable(interaction.gameObject.activeSelf);
            }
        }

        private int CountStagedSignalDrawerItems()
        {
            return (signalDrawerItems ?? Array.Empty<StoryInteractable>())
                .Count(interaction => interaction != null && !interaction.gameObject.activeSelf);
        }

        private StoryPreparationItem SignalItemFor(StoryInteractable interaction)
        {
            string interactionId = interaction != null ? interaction.InteractionId : string.Empty;
            return (items ?? Array.Empty<StoryPreparationItem>())
                .FirstOrDefault(item =>
                    item != null &&
                    item.Category == StoryPreparationCategory.Signal &&
                    string.Equals(interactionId, "Take_" + item.ItemId, StringComparison.Ordinal));
        }

        private int CountRequired(StoryPreparationCategory category)
        {
            return (items ?? new StoryPreparationItem[0]).Count(item => item != null && item.Category == category && item.Recommended);
        }

        private int CountAccepted(StoryPreparationCategory category)
        {
            return (items ?? new StoryPreparationItem[0]).Count(item => item != null && item.Category == category && item.Recommended &&
                item.Flag != StoryFlag.None && gameManager != null && gameManager.HasFlag(item.Flag));
        }

        private bool CategoryComplete(StoryPreparationCategory category)
        {
            int required = CountRequired(category);
            return required > 0 && CountAccepted(category) >= required;
        }

        private StoryInteractable ReviewFor(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => reviewSignalRadio,
                StoryPreparationCategory.Food => reviewFood,
                StoryPreparationCategory.Health => reviewHealth,
                _ => reviewWarmth
            };
        }

        private void DisableCategoryReviews()
        {
            reviewSignal?.SetAvailable(false);
            reviewSignalFlashlightOff?.SetAvailable(false);
            reviewSignalRadioBatteryInsert?.SetAvailable(false);
            reviewSignalRadio?.SetAvailable(false);
            reviewSignalRadioBatteryRemove?.SetAvailable(false);
            reviewSignalWhistle?.SetAvailable(false);
            if (reviewSignalWhistle != null)
                reviewSignalWhistle.gameObject.SetActive(false);
            if (signalWhistleTargetRoot != null)
                signalWhistleTargetRoot.SetActive(false);
            if (signalRadioReviewRoot != null)
                signalRadioReviewRoot.SetActive(false);
            if (signalRadioTuningBeforeRoot != null)
                signalRadioTuningBeforeRoot.SetActive(false);
            reviewFood?.SetAvailable(false);
            reviewHealth?.SetAvailable(false);
            reviewWarmth?.SetAvailable(false);
        }

        private void DisableCategoryDiscoveries()
        {
            discoverSignal?.SetAvailable(false);
            discoverFood?.SetAvailable(false);
            discoverHealth?.SetAvailable(false);
            discoverWarmth?.SetAvailable(false);
        }

        private void DisablePrePackInspections()
        {
            inspectWaterDate?.SetAvailable(false);
            inspectBandageSeal?.SetAvailable(false);
        }

        private StoryInteractable DiscoveryFor(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => discoverSignal,
                StoryPreparationCategory.Food => discoverFood,
                StoryPreparationCategory.Health => discoverHealth,
                _ => discoverWarmth
            };
        }

        private void DisableAllInteractions()
        {
            if (signalPhaseRoutine != null)
            {
                StopCoroutine(signalPhaseRoutine);
                signalPhaseRoutine = null;
            }
            if (packingAdvanceRoutine != null)
            {
                StopCoroutine(packingAdvanceRoutine);
                packingAdvanceRoutine = null;
            }
            activeFlashlightInspection = null;
            activeRadioInspection = null;
            startFamilyPlan?.SetAvailable(false);
            placeContactCard?.SetAvailable(false);
            assignCanWhistleRole?.SetAvailable(false);
            inspectEmptyBag?.SetAvailable(false);
            DisableCategoryDiscoveries();
            DisableCategoryReviews();
            DisablePrePackInspections();
            chooseComfortItem?.SetAvailable(false);
            testBagWeight?.SetAvailable(false);
            removeConsole?.SetAvailable(false);
            testBalancedBag?.SetAvailable(false);
            adjustBagStraps?.SetAvailable(false);
            placeBagAtExit?.SetAvailable(false);
            if (exitShelfDropRing != null)
                exitShelfDropRing.SetActive(false);
            SetSignalDrawerItemsAvailable(false);
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
                item?.SetAvailable(false);
        }

        private void SetBagState(bool open, bool closed, bool worn, bool atExit)
        {
            if (openBagRoot != null)
                openBagRoot.SetActive(open);
            if (closedBagRoot != null)
                closedBagRoot.SetActive(closed);
            if (wornBagRoot != null)
                wornBagRoot.SetActive(worn);
            if (exitShelfBagRoot != null)
                exitShelfBagRoot.SetActive(atExit);
        }

        private void FaceEachOther(Transform first, Transform second)
        {
            if (first == null || second == null)
                return;
            Vector3 direction = second.position - first.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            player?.Stop();
            if (characterFacingRoutine != null)
                StopCoroutine(characterFacingRoutine);
            bool playerFacesFirst = player != null && first == deniz;
            bool playerFacesSecond = player != null && second == deniz;
            if (playerFacesFirst)
                player.FaceTowards(second.position);
            else if (playerFacesSecond)
                player.FaceTowards(first.position);
            characterFacingRoutine = StartCoroutine(SmoothFaceEachOther(
                first,
                second,
                direction,
                playerFacesFirst,
                playerFacesSecond));
        }

        private IEnumerator SmoothFaceEachOther(
            Transform first,
            Transform second,
            Vector3 direction,
            bool playerFacesFirst,
            bool playerFacesSecond)
        {
            Quaternion firstStart = first.rotation;
            Quaternion secondStart = second.rotation;
            Quaternion firstTarget = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion secondTarget = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            const float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration && first != null && second != null)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = normalized * normalized * (3f - 2f * normalized);
                if (!playerFacesFirst)
                    first.rotation = Quaternion.Slerp(firstStart, firstTarget, eased);
                if (!playerFacesSecond)
                    second.rotation = Quaternion.Slerp(secondStart, secondTarget, eased);
                yield return null;
            }

            if (first != null && !playerFacesFirst)
                first.rotation = firstTarget;
            if (second != null && !playerFacesSecond)
                second.rotation = secondTarget;
            characterFacingRoutine = null;
        }

        private static StoryCameraZoneId CategoryCamera(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => StoryCameraZoneId.PreparationSignal,
                StoryPreparationCategory.Food => StoryCameraZoneId.PreparationFood,
                StoryPreparationCategory.Health => StoryCameraZoneId.PreparationHealth,
                _ => StoryCameraZoneId.PreparationWarmth
            };
        }

        private static string ItemExplanationTitle(string itemId)
        {
            return itemId switch
            {
                "Flashlight" => "EL FENERİ NE İŞE YARAR?",
                "Batteries" => "YEDEK PİL NE İŞE YARAR?",
                "Whistle" => "DÜDÜK NE İŞE YARAR?",
                "Radio" => "PİLLİ RADYO NE İŞE YARAR?",
                "Water" => "SUYU NEDEN HAZIRLIYORUZ?",
                "Food" => "KONSERVE NE İŞE YARAR?",
                "FirstAid" => "İLK YARDIM SETİ NE İŞE YARAR?",
                "Documents" => "BELGE KOPYALARI NEDEN GEREKLİ?",
                "Blanket" => "BATTANİYE NE İŞE YARAR?",
                "Clothes" => "YEDEK KIYAFET NEDEN GEREKLİ?",
                _ => "BU EŞYA NE İŞE YARAR?"
            };
        }

        private static string ItemPackingTitle(string itemId)
        {
            return itemId switch
            {
                "Flashlight" => "EL FENERİNİ ÇANTAYA KOY",
                "Batteries" => "YEDEK PİLİ ÇANTAYA KOY",
                "Whistle" => "DÜDÜĞÜ ÇANTAYA KOY",
                "Radio" => "PİLLİ RADYOYU ÇANTAYA KOY",
                "Water" => "SUYU ÇANTAYA KOY",
                "Food" => "KONSERVEYİ ÇANTAYA KOY",
                "FirstAid" => "İLK YARDIM SETİNİ ÇANTAYA KOY",
                "Documents" => "BELGE KOPYALARINI ÇANTAYA KOY",
                "Blanket" => "BATTANİYEYİ ÇANTAYA KOY",
                "Clothes" => "YEDEK KIYAFETİ ÇANTAYA KOY",
                _ => "EŞYAYI ÇANTAYA KOY"
            };
        }

        private static string ItemPurpose(string itemId)
        {
            return itemId switch
            {
                "Flashlight" => "Elektrik kesildiğinde güvenli yolu görmemizi sağlar; açık alev kullanmayız.",
                "Batteries" => "Fenerin ve radyonun enerjisi biterse cihazları yeniden çalıştırır.",
                "Whistle" => "Sesimiz duyulmadığında yerimizi kısa düdüklerle belli etmemizi sağlar.",
                "Radio" => "Telefon ve internet çalışmasa bile resmî afet duyurularını dinlememizi sağlar.",
                "Water" => "Tahliye sonrasında güvenli içme suyuna hemen ulaşmamızı sağlar.",
                "Food" => "Pişirme gerektirmeden enerji veren, uzun süre dayanabilen yiyecektir.",
                "FirstAid" => "Yetişkinin küçük yaralanmalara güvenli biçimde müdahale etmesi için malzeme taşır.",
                "Documents" => "Kimlik ve iletişim bilgilerine telefon çalışmasa bile ulaşmamızı sağlar.",
                "Blanket" => "Soğukta vücut ısımızı korur ve dinlenirken sıcak kalmamıza yardım eder.",
                "Clothes" => "Islanan veya kirlenen giysileri kuru, mevsime uygun kıyafetlerle değiştirmemizi sağlar.",
                _ => "Afet sonrasında temel bir ihtiyacı karşılar."
            };
        }

        private static string ItemExplanationDialogue(string itemId)
        {
            return itemId switch
            {
                "Flashlight" =>
                    "Deniz: Elektrik kesilirse karanlıkta yolu bununla görürüz.\nAnne: Evet; mum yakmadan güvenli hareket etmek için el fenerini hazır tutarız.",
                "Batteries" =>
                    "Can: Fenerin pili biterse karanlıkta mı kalırız?\nDeniz: Bu yüzden fener ve radyo için uygun yedek pilleri ayrı taşırız.",
                "Whistle" =>
                    "Can: Düdüğü neden seslenmek yerine kullanıyoruz?\nAnne: Sesimiz yorulsa bile üç kısa düdükle yerimizi belli edebiliriz.",
                "Radio" =>
                    "Deniz: İnternet kesilirse doğru haberi nereden alacağız?\nAnne: Pilli radyodan yalnız resmî afet duyurularını dinleyeceğiz.",
                "Water" =>
                    "Can: Bu şişe çıkınca hemen içmek için mi?\nDeniz: Evet; kapağı sağlam ve tarihi uygun güvenli suyu yanımıza alırız.",
                "Food" =>
                    "Deniz: Konserve, pişirmeden yenebildiği ve uzun süre dayandığı için uygundur.\nAnne: Hafif olanı seçeriz; çantayı taşıyamayacağımız kadar doldurmayız.",
                "FirstAid" =>
                    "Can: Yaralanınca bunu ben mi kullanacağım?\nAnne: Hayır; seti yetişkine ulaştırırsın, müdahaleyi sorumlu yetişkin yapar.",
                "Documents" =>
                    "Deniz: Telefon açılmazsa numaraları ve kimlik bilgilerini buradan buluruz.\nAnne: Kopyaları su geçirmez kılıfta saklarız.",
                "Blanket" =>
                    "Can: İnce battaniye gerçekten işe yarar mı?\nDeniz: Evet; az yer kaplar ve dışarıda beklerken vücut ısımızı korur.",
                "Clothes" =>
                    "Anne: Islak giysi bizi hızla üşütebilir.\nDeniz: Bu yüzden mevsime uygun kuru bir yedek kıyafet taşıyoruz.",
                _ => "Anne: Bu eşyanın görevini öğrendik. Şimdi doğru yere yerleştirebiliriz."
            };
        }

        private static string CategoryTitle(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "IŞIK VE HABERLEŞME",
                StoryPreparationCategory.Food => "SU VE DAYANIKLI GIDA",
                StoryPreparationCategory.Health => "SAĞLIK VE AİLE BİLGİSİ",
                _ => "SICAK KALMA VE GİYİM"
            };
        }

        private static string CategoryDetail(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "Masada hazırladığın dört parçayı açık çantanın içine sürükleyip yerleştir.",
                StoryPreparationCategory.Food => "Sızdırmayan, dayanıklı ve çantayı gereksiz ağırlaştırmayan parçaları seç.",
                StoryPreparationCategory.Health => "Yara bakımı, hijyen ve aile iletişim bilgileri için gerekli parçaları ayır.",
                _ => "Hafif, mevsime uygun ve kuru kalabilecek parçaları seç."
            };
        }

        private static string CategoryDiscoveryTitle(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "KOMODİNİN ÇEKMECESİNİ AÇ",
                StoryPreparationCategory.Food => "MUTFAK DOLABINI AÇ",
                StoryPreparationCategory.Health => "İLK YARDIM DOLABINI AÇ",
                _ => "BATTANİYE SANDIĞINI AÇ"
            };
        }

        private static string CategoryDiscoveryDetail(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "Sarı işaretli çekmece kulpunu tut; aşağı doğru çekip bırak.",
                StoryPreparationCategory.Food => "Alt dolaptaki sol kapağın üzerindeki el işaretini tut; parmağını sağa kaydırıp bırak.",
                StoryPreparationCategory.Health => "Sarı işaretli ilk yardım dolabına bas; sağa ya da sola kaydırıp bırak.",
                _ => "Sarı işaretli sandığa bas; sağa ya da sola kaydırıp bırak."
            };
        }

        private static string CategoryDiscoveryResult(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "Çekmece açıldı. Fener, yedek pil, düdük ve radyoya dokun; önce hepsini masaya gönder.",
                StoryPreparationCategory.Food => "Dolap açıldı. Kırılmayan su şişesi ile dayanıklı gıda, ağır ve kırılabilir eşyalardan ayrılıyor.",
                StoryPreparationCategory.Health => "Sağlık çekmecesi açıldı. İlk yardım seti ve aile bilgi kopyaları birlikte saklanıyor.",
                _ => "Sandık açıldı. İnce battaniye ve mevsime uygun kıyafet çantayı gereksiz ağırlaştırmıyor."
            };
        }

        private static string CategoryReviewTitle(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "FENERİ VE RADYOYU DENE",
                StoryPreparationCategory.Food => "SU CEBİNİ KAPAT",
                StoryPreparationCategory.Health => "BELGE KILIFINI MÜHÜRLE",
                _ => "ANA FERMUARI KAPAT"
            };
        }

        private static string CategoryReviewDetail(StoryPreparationCategory category)
        {
            return category switch
            {
                StoryPreparationCategory.Signal => "Çantanın yanındaki fener anahtarına iki kez dokun; ışığın çalıştığını gör.",
                StoryPreparationCategory.Food => "Su şişesinin bulunduğu dış cebin fermuarını yana çek.",
                StoryPreparationCategory.Health => "Su geçirmez belge kılıfının ağzını parmağınla bastırarak kapat.",
                _ => "Battaniye ve kıyafetin üstündeki ana fermuarı yana çek."
            };
        }
    }
}
