using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryUIController : MonoBehaviour
    {
        [Serializable]
        private sealed class DialogueVoiceBinding
        {
            [TextArea] public string subtitle;
            public AudioClip clip;
        }

        [Header("HUD")]
        [SerializeField] private TMP_Text objectiveTitle;
        [SerializeField] private TMP_Text objectiveDetail;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text contextPrompt;
        [SerializeField] private StoryActionButton actionButton;
        [SerializeField, Min(10f)] private float subtitleCharactersPerSecond = 52f;
        [SerializeField] private StoryPlayerMovement movementOwner;

        [Header("Presentation")]
        [SerializeField] private Animation objectivePresentation;
        [SerializeField] private Animation subtitlePresentation;
        [SerializeField] private Animation contextPresentation;
        [SerializeField] private Animation pausePresentation;
        [SerializeField] private Animation completionPresentation;
        [SerializeField] private Animation chapterSelectionPresentation;

        [Header("Dialogue Voice")]
        [SerializeField] private AudioSource dialogueVoiceSource;
        [SerializeField] private DialogueVoiceBinding[] dialogueVoices = Array.Empty<DialogueVoiceBinding>();

        [Header("Menus")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private GameObject completionReportCard;
        [SerializeField] private GameObject chapterSelectionCard;
        [SerializeField] private GameObject nextActButton;
        [SerializeField] private TMP_Text nextActButtonLabel;
        [SerializeField] private Toggle reduceShakeToggle;
        [SerializeField] private TMP_Text reduceShakeState;
        [SerializeField] private TMP_Text completionPreparationValue;
        [SerializeField] private TMP_Text completionProtectionValue;
        [SerializeField] private TMP_Text completionCooperationValue;
        [SerializeField] private StoryCameraController cameraController;

        private Coroutine subtitleRoutine;
        private Coroutine contextRoutine;
        private Action subtitleCompleted;
        private Action subtitleCompletedAfterPointerRelease;
        private bool subtitleActive;
        private bool subtitleRevealComplete;
        private bool subtitleAdvanceRequested;
        private bool paused;
        private bool consumeWorldPointerUntilRelease;

        public event Action<bool> WorldInputBlockChanged;

        public bool SubtitleActive => subtitleActive;
        public bool SubtitleRevealComplete => subtitleRevealComplete;
        public bool WorldInputBlocked => paused || subtitleActive || consumeWorldPointerUntilRelease;

        private void Awake()
        {
            movementOwner ??= FindFirstObjectByType<StoryPlayerMovement>(FindObjectsInactive.Include);
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (completionPanel != null)
                completionPanel.SetActive(false);
            if (chapterSelectionCard != null)
                chapterSelectionCard.SetActive(false);

            bool reducedShake = PlayerPrefs.GetInt("story.reduceShake", 0) == 1;
            if (reduceShakeToggle != null)
                reduceShakeToggle.SetIsOnWithoutNotify(reducedShake);
            UpdateReducedShakeState(reducedShake);
            cameraController?.SetReducedShake(reducedShake);
            ApplyWorldInputLock();
        }

        public void ShowObjective(string title, string detail)
        {
            if (objectiveTitle != null)
                objectiveTitle.text = title;
            if (objectiveDetail != null)
                objectiveDetail.text = detail;
            PlayPresentation(objectivePresentation);
        }

        public void ShowSubtitle(string text)
        {
            ShowSubtitle(text, 4f);
        }

        public void ShowSubtitle(string text, float duration)
        {
            ShowSubtitle(text, duration, null);
        }

        public void ShowSubtitle(string text, float duration, Action completed)
        {
            if (subtitle == null)
            {
                completed?.Invoke();
                return;
            }

            if (subtitleRoutine != null)
                StopCoroutine(subtitleRoutine);
            ResetSubtitleState(false);
            subtitleCompleted = completed;
            SetSubtitleActive(true);
            AudioClip voiceClip = PlayDialogueVoice(text);
            float voicedDuration = voiceClip != null ? voiceClip.length + 0.25f : 0f;
            subtitleRoutine = StartCoroutine(ShowSubtitleTypewriter(text, Mathf.Max(duration, voicedDuration)));
        }

        public bool TryHandlePrimaryTap()
        {
            if (!subtitleActive)
                return false;

            if (!subtitleRevealComplete)
            {
                CompleteSubtitleReveal();
                return true;
            }

            subtitleAdvanceRequested = true;
            return true;
        }

        public void NotifyPrimaryPointerReleased()
        {
            if (!consumeWorldPointerUntilRelease || paused || subtitleActive)
                return;

            consumeWorldPointerUntilRelease = false;
            ApplyWorldInputLock();
            WorldInputBlockChanged?.Invoke(WorldInputBlocked);
            Action completed = subtitleCompletedAfterPointerRelease;
            subtitleCompletedAfterPointerRelease = null;
            completed?.Invoke();
        }

        public void ShowContext(string text)
        {
            if (contextPrompt == null)
                return;

            if (contextRoutine != null)
                StopCoroutine(contextRoutine);
            contextRoutine = StartCoroutine(ShowTemporarily(contextPrompt, text, 2.25f));
        }

        public void ShowNarratedContext(string text)
        {
            if (contextPrompt == null)
                return;

            if (contextRoutine != null)
                StopCoroutine(contextRoutine);
            AudioClip voiceClip = PlayDialogueVoice(text);
            float duration = voiceClip != null ? Mathf.Max(2.25f, voiceClip.length + 0.15f) : 2.25f;
            contextRoutine = StartCoroutine(ShowTemporarily(contextPrompt, text, duration));
        }

        public void HideContext()
        {
            if (contextRoutine != null)
            {
                StopCoroutine(contextRoutine);
                contextRoutine = null;
            }
            if (contextPrompt == null)
                return;

            contextPrompt.text = string.Empty;
            GameObject displayRoot = contextPrompt.transform.parent != null
                ? contextPrompt.transform.parent.gameObject
                : contextPrompt.gameObject;
            displayRoot.SetActive(false);
        }

        public void PresentAction(string label, StoryInteractionGesture gesture, int gestureCount, System.Action completed)
        {
            actionButton?.Present(label, gesture, gestureCount, completed);
        }

        public void HideAction()
        {
            actionButton?.Cancel();
        }

        public void TogglePause()
        {
            SetPaused(!paused);
        }

        public void RetryCheckpoint()
        {
            SetPaused(false);
            StoryGameManager manager = StoryGameManager.Instance;
            if (manager != null)
            {
                manager.RetryCheckpoint();
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
                SceneManager.LoadScene(activeScene.name);
        }

        public void ReplayStory()
        {
            SetPaused(false);
            StoryGameManager manager = StoryGameManager.Instance;
            if (manager != null)
            {
                manager.ReplayCurrentAct();
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
                SceneManager.LoadScene(activeScene.name);
        }

        public void ContinueAfterAct()
        {
            SetPaused(false);
            StoryGameManager manager = StoryGameManager.Instance;
            if (manager == null)
                return;

            if (manager.IsFinalAct)
            {
                ShowChapterSelection();
                return;
            }

            manager.ContinueToNextAct();
        }

        public void OpenPreparationAct()
        {
            StoryGameManager.Instance?.OpenAct((int)StoryAct.Preparation);
        }

        public void OpenHomeSafetyAct()
        {
            StoryGameManager.Instance?.OpenAct((int)StoryAct.HomeSafety);
        }

        public void OpenQuakeAct()
        {
            StoryGameManager.Instance?.OpenAct((int)StoryAct.Quake);
        }

        public void OpenEvacuationAct()
        {
            StoryGameManager.Instance?.OpenAct((int)StoryAct.Evacuation);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            if (pausePanel != null)
                pausePanel.SetActive(paused);
            if (paused)
                PlayPresentation(pausePresentation);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            ApplyWorldInputLock();
            WorldInputBlockChanged?.Invoke(WorldInputBlocked);
        }

        public void SetReducedShake(bool reduced)
        {
            PlayerPrefs.SetInt("story.reduceShake", reduced ? 1 : 0);
            PlayerPrefs.Save();
            UpdateReducedShakeState(reduced);
            cameraController?.SetReducedShake(reduced);
        }

        public void ShowCompletion()
        {
            SetPaused(false);
            RefreshCompletionReport();
            RefreshCompletionRoute();
            if (completionPanel != null)
                completionPanel.SetActive(true);
            ShowCompletionReport();
        }

        public void HideCompletion()
        {
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        public void ShowChapterSelection()
        {
            RefreshCompletionRoute();
            if (completionReportCard != null)
                completionReportCard.SetActive(false);
            if (chapterSelectionCard != null)
            {
                chapterSelectionCard.SetActive(true);
                PlayPresentation(chapterSelectionPresentation);
            }
        }

        public void ShowCompletionReport()
        {
            if (completionReportCard != null)
            {
                completionReportCard.SetActive(true);
                PlayPresentation(completionPresentation);
            }
            if (chapterSelectionCard != null)
                chapterSelectionCard.SetActive(false);
        }

        private void RefreshCompletionRoute()
        {
            StoryGameManager manager = StoryGameManager.Instance;
            bool hasRoute = manager != null && nextActButton != null;
            if (nextActButton != null)
                nextActButton.SetActive(hasRoute);
            if (nextActButtonLabel != null)
                nextActButtonLabel.text = manager != null && manager.IsFinalAct
                    ? "BÖLÜM SEÇİMİ"
                    : "SONRAKİ PERDE";
        }

        private IEnumerator ShowTemporarily(TMP_Text target, string text, float duration)
        {
            GameObject displayRoot = target.transform.parent != null ? target.transform.parent.gameObject : target.gameObject;
            target.text = text;
            displayRoot.SetActive(true);
            PlayPresentation(contextPresentation);
            float remaining = Mathf.Max(0f, duration);
            while (remaining > 0f)
            {
                if (!paused)
                    remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
            displayRoot.SetActive(false);
        }

        private IEnumerator ShowSubtitleTypewriter(string text, float duration)
        {
            GameObject displayRoot = subtitle.transform.parent != null
                ? subtitle.transform.parent.gameObject
                : subtitle.gameObject;
            subtitle.text = text ?? string.Empty;
            subtitle.maxVisibleCharacters = 0;
            displayRoot.SetActive(true);
            PlayPresentation(subtitlePresentation);
            subtitle.ForceMeshUpdate();

            SetSubtitleActive(true);
            subtitleRevealComplete = subtitle.textInfo.characterCount == 0;
            subtitleAdvanceRequested = false;

            int totalCharacters = subtitle.textInfo.characterCount;
            float elapsed = 0f;
            float revealed = 0f;
            while (!subtitleRevealComplete)
            {
                if (!paused)
                {
                    float delta = Time.unscaledDeltaTime;
                    elapsed += delta;
                    revealed += Mathf.Max(10f, subtitleCharactersPerSecond) * delta;
                    subtitle.maxVisibleCharacters = Mathf.Min(totalCharacters, Mathf.FloorToInt(revealed));
                    if (subtitle.maxVisibleCharacters >= totalCharacters)
                        CompleteSubtitleReveal();
                }
                yield return null;
            }

            float remaining = Mathf.Max(1.25f, Mathf.Max(0f, duration) - elapsed);
            while (!subtitleAdvanceRequested && remaining > 0f)
            {
                if (!paused)
                    remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            ResetSubtitleState(true);
        }

        private void CompleteSubtitleReveal()
        {
            if (subtitle == null)
                return;

            subtitle.maxVisibleCharacters = int.MaxValue;
            subtitleRevealComplete = true;
        }

        private void ResetSubtitleState(bool invokeCompleted)
        {
            Action completed = invokeCompleted ? subtitleCompleted : null;
            subtitleCompleted = null;
            subtitleRoutine = null;
            if (dialogueVoiceSource != null)
                dialogueVoiceSource.Stop();
            SetSubtitleActive(false);
            subtitleRevealComplete = false;
            subtitleAdvanceRequested = false;

            if (subtitle != null)
            {
                subtitle.maxVisibleCharacters = int.MaxValue;
                GameObject displayRoot = subtitle.transform.parent != null
                    ? subtitle.transform.parent.gameObject
                    : subtitle.gameObject;
                displayRoot.SetActive(false);
            }

            if (completed != null && consumeWorldPointerUntilRelease)
            {
                // Altyazıyı kapatan dokunuş bırakılmadan sahne callback'ini çalıştırırsak,
                // dünya giriş kilidi otomatik MoveTo gibi kontrollü hikâye hareketlerini de
                // reddeder. Callback'i pointer guard kalktığı kareye taşı.
                subtitleCompletedAfterPointerRelease = completed;
                return;
            }

            completed?.Invoke();
        }

        private AudioClip PlayDialogueVoice(string text)
        {
            if (dialogueVoiceSource == null || dialogueVoices == null || string.IsNullOrEmpty(text))
                return null;

            dialogueVoiceSource.Stop();
            for (int i = 0; i < dialogueVoices.Length; i++)
            {
                DialogueVoiceBinding binding = dialogueVoices[i];
                if (binding == null || binding.clip == null ||
                    !string.Equals(binding.subtitle, text, StringComparison.Ordinal))
                    continue;

                dialogueVoiceSource.clip = binding.clip;
                dialogueVoiceSource.Play();
                return binding.clip;
            }

            return null;
        }

        private void SetSubtitleActive(bool value)
        {
            if (subtitleActive == value)
            {
                ApplyWorldInputLock();
                return;
            }

            bool wasActive = subtitleActive;
            subtitleActive = value;
            if (wasActive && !value)
            {
                // Altyazıyı ilerleten dokunuş aynı Unity karesinde tekrar dünya dokunuşu olarak
                // okunabilir. Parmak/fare bırakılana kadar bu tek basışı tüket; sonraki bağımsız
                // dokunuş normal biçimde dünyaya gider.
                consumeWorldPointerUntilRelease = true;
            }
            ApplyWorldInputLock();
            WorldInputBlockChanged?.Invoke(WorldInputBlocked);
        }

        private void ApplyWorldInputLock()
        {
            movementOwner ??= FindFirstObjectByType<StoryPlayerMovement>(FindObjectsInactive.Include);
            movementOwner?.SetStoryInputLocked(WorldInputBlocked);
        }

        private void UpdateReducedShakeState(bool reduced)
        {
            if (reduceShakeState != null)
                reduceShakeState.text = reduced ? "AÇIK" : "KAPALI";
        }

        private static void PlayPresentation(Animation presentation)
        {
            if (presentation == null || presentation.clip == null)
                return;

            presentation.Stop();
            presentation.Rewind();
            presentation.Play();
        }

        private void RefreshCompletionReport()
        {
            StorySessionState state = StoryGameManager.Instance != null ? StoryGameManager.Instance.CurrentState : null;
            if (completionPreparationValue != null)
            {
                bool wardrobe = state?.HasFlag(StoryFlag.WardrobeSecured) ?? false;
                bool exit = state?.HasFlag(StoryFlag.ExitCleared) ?? false;
                completionPreparationValue.text = wardrobe && exit
                    ? "Dolap sabit kaldı; çıkış yolu açık kaldı."
                    : wardrobe
                        ? "Dolap sabit kaldı; çıkışta dikkatli alternatif rota kullanıldı."
                        : exit
                            ? "Çıkış açık kaldı; devrilen dolabın yan geçidinden uzak duruldu."
                            : "Ana güvenli rota kullanıldı; hazırlık eksikleri riski artırdı.";
            }

            if (completionProtectionValue != null)
            {
                int mistakes = state?.mistakeCount ?? 0;
                completionProtectionValue.text = mistakes == 0
                    ? "Çök • Kapan • Tutun doğru sırayla uygulandı."
                    : $"Çök • Kapan • Tutun tamamlandı; {mistakes} ramak kala güvenle düzeltildi.";
            }

            if (completionCooperationValue != null)
                completionCooperationValue.text = "Can kontrol edildi; kardeşler birlikte koridora çıktı.";
        }

        private void OnDestroy()
        {
            ResetSubtitleState(false);
            if (paused)
                Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
