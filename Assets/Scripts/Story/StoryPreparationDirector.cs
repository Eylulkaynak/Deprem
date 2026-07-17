using System;
using System.Collections;
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
        [SerializeField] private StoryInteractable startFamilyPlan;
        [SerializeField] private StoryInteractable inspectEmptyBag;

        [Header("Item Decisions")]
        [SerializeField] private StoryPreparationItem[] items;
        [SerializeField] private StoryInteractable reviewSignal;
        [SerializeField] private StoryInteractable reviewFood;
        [SerializeField] private StoryInteractable reviewHealth;
        [SerializeField] private StoryInteractable reviewWarmth;

        [Header("Final Bag Check")]
        [SerializeField] private StoryInteractable testBagWeight;
        [SerializeField] private StoryInteractable adjustBagStraps;
        [SerializeField] private StoryInteractable placeBagAtExit;
        [SerializeField] private GameObject openBagRoot;
        [SerializeField] private GameObject closedBagRoot;
        [SerializeField] private GameObject wornBagRoot;
        [SerializeField] private GameObject exitShelfBagRoot;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionDetail;

        private StoryPreparationCategory currentCategory;
        private Coroutine consequenceRoutine;
        private bool categoryTransitionPending;

        private static readonly int InteractTrigger = Animator.StringToHash("StoryInteract");
        private static readonly int InspectTrigger = Animator.StringToHash("StoryInspect");
        private static readonly int PickUpTrigger = Animator.StringToHash("StoryPickUp");
        private static readonly int CallTrigger = Animator.StringToHash("StoryCall");

        public StoryPreparationCategory CurrentCategory => currentCategory;
        public StoryPreparationItem[] Items => items;

        private void Start()
        {
            gameManager ??= StoryGameManager.Instance;
            RestoreItemVisuals();
            DisableAllInteractions();
            SetBagState(true, false, false, false);
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
                    SetupFinalBagCheck();
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
            FaceEachOther(deniz, parent);
            parentAnimator?.SetTrigger(InteractTrigger);
            denizAnimator?.SetTrigger(InspectTrigger);
            startFamilyPlan?.SetAvailable(false);
            inspectEmptyBag?.SetAvailable(false);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            ui?.ShowObjective("ÇANTAYI BİRLİKTE İNCELE", "Boş afet çantasının ceplerini ve taşıma biçimini kontrol et.");
            ShowDialogue("Anne: Bu çanta deprem olurken alınmak için değil; sarsıntı bittikten sonra güvenle çıkarken yanımıza almak için hazır duracak.",
                5.5f, BeginGuidedBagMove);
        }

        public void OnBagInspected()
        {
            inspectEmptyBag?.SetAvailable(false);
            denizAnimator?.SetTrigger(InspectTrigger);
            SetBagState(true, false, false, false);
            gameManager?.CommitCheckpoint(StoryCheckpoint.BagInspected);
            DisableAllInteractions();
            ShowDialogue("Anne: Önce karanlıkta haber almayı ve birbirimizi bulmayı sağlayan parçaları seçelim.",
                4f, () => StartCategory(StoryPreparationCategory.Signal));
        }

        public void ResolveChoice(StoryPreparationItem item)
        {
            if (item == null || item.Category != currentCategory)
                return;

            FaceEachOther(deniz, parent);
            if (item.Recommended)
            {
                denizAnimator?.SetTrigger(PickUpTrigger);
                parentAnimator?.SetTrigger(InteractTrigger);
                item.Accept();
                if (item.Flag != StoryFlag.None)
                    gameManager?.SetFlag(item.Flag, true);
                // Doğru her nesneden sonra uzun bir altyazı zinciri oynatmak fiziksel
                // hazırlığı dur-kalk hâline getiriyordu. Nesne çantaya iner, kısa onay
                // görünür; ayrıntılı eğitim kategori sonundaki aile kontrolünde kalır.
                ui?.ShowContext($"ÇANTAYA EKLENDİ: {item.DisplayName}");
                OnRecommendedChoiceDialogueCompleted(item.Category);
                return;
            }

            denizAnimator?.SetTrigger(InspectTrigger);
            parentAnimator?.SetTrigger(InteractTrigger);
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

        public void OnBagWeightTested()
        {
            denizAnimator?.SetTrigger(PickUpTrigger);
            canAnimator?.SetTrigger(CallTrigger);
            SetBagState(false, false, true, false);
            testBagWeight?.SetAvailable(false);
            adjustBagStraps?.SetAvailable(false);
            FaceEachOther(deniz, can);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBagFit);
            ui?.ShowObjective("İKİ ASKINI AYARLA", "Çantayı iki omuza eşit dağıt; ellerin serbest kalsın.");
            ShowDialogue("Can: Ben de taşıyayım mı?\nAnne: Herkes yaşına uygun yük taşır. Can kendi düdüğünü bilir; ana çanta Deniz'in iki omzunda kalır.",
                7f, () => adjustBagStraps?.SetAvailable(true));
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
            ui?.ShowObjective("AİLE PLANINI BAŞLAT", "Anneyle konuş; afet çantasını neden birlikte hazırladığınızı öğren.");
            ShowDialogue("Sakin bir aile öğleden sonrası. Anne çantayı yere açtı; Deniz ve Can hangi parçaların gerçekten işe yarayacağını birlikte öğrenecek.",
                4.5f, () => startFamilyPlan?.SetAvailable(true));
        }

        private void StartCategory(StoryPreparationCategory category)
        {
            currentCategory = category;
            categoryTransitionPending = false;
            DisableCategoryReviews();
            foreach (StoryPreparationItem item in items ?? new StoryPreparationItem[0])
            {
                if (item == null)
                    continue;
                bool accepted = item.Recommended && item.Flag != StoryFlag.None && gameManager != null && gameManager.HasFlag(item.Flag);
                if (item.Category == category && !accepted)
                    item.SetAvailable(true);
                else
                    item.SetAvailable(false);
            }

            cameraController?.ActivateZone(CategoryCamera(category));
            UpdateCategoryObjective();
        }

        private void UpdateCategoryObjective()
        {
            int required = CountRequired(currentCategory);
            int selected = CountAccepted(currentCategory);
            if (selected >= required)
            {
                BeginAutomaticCategoryReview(currentCategory);
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
            parentAnimator?.SetTrigger(InteractTrigger);
            denizAnimator?.SetTrigger(InteractTrigger);
            FaceEachOther(deniz, parent);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationParent);
            gameManager?.CommitCheckpoint(checkpoint);
            ui?.ShowObjective(CategoryTitle(category) + " — TAMAM", "Anne son yerleşimi kontrol ediyor; ardından sıradaki masaya geçilecek.");
            ShowDialogue(subtitle, 5.5f, () => AdvanceAfterCategoryReview(category));
        }

        private void OnRecommendedChoiceDialogueCompleted(StoryPreparationCategory category)
        {
            if (currentCategory != category || categoryTransitionPending)
                return;

            UpdateCategoryObjective();
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
                    SetupFinalBagCheck();
                    break;
            }
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

        private void ShowCompletionCard()
        {
            if (completionPanel != null)
                completionPanel.SetActive(true);
        }

        private void SetupFinalBagCheck()
        {
            DisableAllInteractions();
            SetBagState(true, false, false, false);
            testBagWeight?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationBag);
            ui?.ShowObjective("ÇANTANIN AĞIRLIĞINI DENE", "Dizlerini bükerek çantayı kaldır; tek başına güvenle taşıyabildiğini kontrol et.");
        }

        private void SetupExitPlacement()
        {
            DisableAllInteractions();
            SetBagState(false, false, true, false);
            placeBagAtExit?.SetAvailable(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationOverview, true);
            ui?.ShowObjective("ÇANTAYI GÜVENLİ RAFA BIRAK", "Çıkışı kapatmadan, kuru ve kolay hatırlanan alçak rafa yerleştir.");
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
            if (item.ConsequenceRoot != null)
                item.ConsequenceRoot.SetActive(true);
            cameraController?.ActivateZone(StoryCameraZoneId.PreparationWrongChoice);
            yield return new WaitForSeconds(3.1f);
            if (item != null && item.ConsequenceRoot != null)
                item.ConsequenceRoot.SetActive(false);
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
                StoryPreparationCategory.Signal => reviewSignal,
                StoryPreparationCategory.Food => reviewFood,
                StoryPreparationCategory.Health => reviewHealth,
                _ => reviewWarmth
            };
        }

        private void DisableCategoryReviews()
        {
            reviewSignal?.SetAvailable(false);
            reviewFood?.SetAvailable(false);
            reviewHealth?.SetAvailable(false);
            reviewWarmth?.SetAvailable(false);
        }

        private void DisableAllInteractions()
        {
            startFamilyPlan?.SetAvailable(false);
            inspectEmptyBag?.SetAvailable(false);
            DisableCategoryReviews();
            testBagWeight?.SetAvailable(false);
            adjustBagStraps?.SetAvailable(false);
            placeBagAtExit?.SetAvailable(false);
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

        private static void FaceEachOther(Transform first, Transform second)
        {
            if (first == null || second == null)
                return;
            Vector3 direction = second.position - first.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;
            first.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            second.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
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
                StoryPreparationCategory.Signal => "Elektrik kesildiğinde çalışan ve yardım çağırmaya yarayan parçaları bul.",
                StoryPreparationCategory.Food => "Sızdırmayan, dayanıklı ve çantayı gereksiz ağırlaştırmayan parçaları seç.",
                StoryPreparationCategory.Health => "Yara bakımı, hijyen ve aile iletişim bilgileri için gerekli parçaları ayır.",
                _ => "Hafif, mevsime uygun ve kuru kalabilecek parçaları seç."
            };
        }
    }
}
