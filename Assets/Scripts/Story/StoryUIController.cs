using System;
using System.Collections;
using System.Collections.Generic;
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

        [Serializable]
        private sealed class DialogueActorBinding
        {
            public string[] aliases = Array.Empty<string>();
            public Transform actorRoot;
            public Transform head;
            public Transform mouth;

            [NonSerialized] public Vector3 mouthRestScale;
            [NonSerialized] public Quaternion appliedHeadOffset = Quaternion.identity;
            [NonSerialized] public float appliedHeadYaw;
            [NonSerialized] public float appliedHeadPitch;
            [NonSerialized] public bool initialized;

            public string PrimaryAlias => aliases != null && aliases.Length > 0
                ? aliases[0]
                : string.Empty;
        }

        private readonly struct DialogueMarker
        {
            public readonly int index;
            public readonly int tokenLength;
            public readonly string alias;

            public DialogueMarker(int index, int tokenLength, string alias)
            {
                this.index = index;
                this.tokenLength = tokenLength;
                this.alias = alias;
            }
        }

        private sealed class DialogueSegment
        {
            public string alias;
            public float endNormalized;
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

        [Header("Dialogue Actors")]
        [SerializeField] private DialogueActorBinding[] dialogueActors = Array.Empty<DialogueActorBinding>();
        [SerializeField, Range(5f, 25f)] private float dialogueHeadYawLimit = 25f;
        [SerializeField, Range(3f, 12f)] private float dialogueHeadPitchLimit = 12f;
        [SerializeField, Min(15f)] private float dialogueBodyTurnSpeed = 90f;
        [SerializeField, Min(1f)] private float dialogueHeadDamping = 14f;

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
        private Coroutine dialoguePerformanceRoutine;
        private Action subtitleCompleted;
        private Action subtitleCompletedAfterPointerRelease;
        private bool subtitleActive;
        private bool subtitleRevealComplete;
        private bool subtitleAdvanceRequested;
        private bool paused;
        private bool consumeWorldPointerUntilRelease;
        private DialogueActorBinding activeDialogueActor;
        private DialogueActorBinding activeDialogueListener;
        private string activeDialogueSpeakerAlias = string.Empty;
        private bool dialoguePerformanceActive;
        private bool dialoguePerformanceUsesVoice;
        private readonly float[] dialogueAudioSamples = new float[64];
        private int dialogueVoiceLastTimeSamples = -1;
        private float dialogueVoiceLastAdvanceAt;
        private float activeDialogueEnvelope;

        public event Action<bool> WorldInputBlockChanged;

        public bool SubtitleActive => subtitleActive;
        public bool SubtitleRevealComplete => subtitleRevealComplete;
        public bool WorldInputBlocked => paused || subtitleActive || consumeWorldPointerUntilRelease;
        public AudioSource ActiveDialogueVoiceSource => dialogueVoiceSource;
        public string ActiveDialogueSpeakerAlias => activeDialogueSpeakerAlias;
        public Transform ActiveDialogueMouth => activeDialogueActor?.mouth;
        public Transform ActiveDialogueListenerRoot => activeDialogueListener?.actorRoot;
        public float ActiveDialogueHeadYaw => activeDialogueActor?.appliedHeadYaw ?? 0f;
        public float ActiveDialogueHeadPitch => activeDialogueActor?.appliedHeadPitch ?? 0f;
        public float ActiveDialogueEnvelope => activeDialogueEnvelope;
        public int DialogueActorCount => dialogueActors?.Length ?? 0;

        private void Awake()
        {
            InitializeDialogueActors();
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

        private void LateUpdate()
        {
            if (dialoguePerformanceActive)
                UpdateDialoguePerformanceVisuals();
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
            float effectiveDuration = Mathf.Max(duration, voicedDuration);
            StartDialoguePerformance(text, effectiveDuration, voiceClip != null);
            subtitleRoutine = StartCoroutine(ShowSubtitleTypewriter(text, effectiveDuration));
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
            StopDialoguePerformance();
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

        private void InitializeDialogueActors()
        {
            if (dialogueActors == null)
                return;

            foreach (DialogueActorBinding actor in dialogueActors)
            {
                if (actor == null || actor.actorRoot == null || actor.head == null || actor.mouth == null)
                    continue;

                actor.mouthRestScale = actor.mouth.localScale;
                actor.appliedHeadOffset = Quaternion.identity;
                actor.appliedHeadYaw = 0f;
                actor.appliedHeadPitch = 0f;
                actor.initialized = true;
            }
        }

        private void StartDialoguePerformance(string text, float duration, bool usesVoice)
        {
            StopDialoguePerformance();
            List<DialogueSegment> segments = ParseDialogueSegments(text);
            if (segments.Count == 0 || dialogueActors == null || dialogueActors.Length == 0)
                return;

            dialoguePerformanceUsesVoice = usesVoice;
            dialogueVoiceLastTimeSamples = -1;
            dialogueVoiceLastAdvanceAt = Time.unscaledTime;
            dialoguePerformanceRoutine = StartCoroutine(
                DriveDialoguePerformance(segments, Mathf.Max(0.05f, duration)));
        }

        private IEnumerator DriveDialoguePerformance(List<DialogueSegment> segments, float duration)
        {
            dialoguePerformanceActive = true;
            float elapsed = 0f;
            int activeSegment = -1;
            while (subtitleActive && elapsed < duration)
            {
                if (!paused)
                    elapsed += Time.unscaledDeltaTime;

                float normalized = Mathf.Clamp01(elapsed / duration);
                int nextSegment = segments.Count - 1;
                for (int index = 0; index < segments.Count; index++)
                {
                    if (normalized <= segments[index].endNormalized)
                    {
                        nextSegment = index;
                        break;
                    }
                }

                if (nextSegment != activeSegment)
                {
                    activeSegment = nextSegment;
                    ActivateDialogueSegment(segments[activeSegment].alias);
                }
                yield return null;
            }

            dialoguePerformanceRoutine = null;
            ClearDialoguePerformanceVisuals();
        }

        private List<DialogueSegment> ParseDialogueSegments(string text)
        {
            string source = text ?? string.Empty;
            var markers = new List<DialogueMarker>();
            if (dialogueActors != null)
            {
                foreach (DialogueActorBinding actor in dialogueActors)
                {
                    if (actor?.aliases == null)
                        continue;
                    foreach (string rawAlias in actor.aliases)
                    {
                        string alias = rawAlias?.Trim();
                        if (string.IsNullOrEmpty(alias))
                            continue;

                        string token = alias + ":";
                        int searchFrom = 0;
                        while (searchFrom < source.Length)
                        {
                            int found = source.IndexOf(token, searchFrom, StringComparison.OrdinalIgnoreCase);
                            if (found < 0)
                                break;
                            searchFrom = found + token.Length;
                            if (found > 0 && !char.IsWhiteSpace(source[found - 1]))
                                continue;
                            markers.Add(new DialogueMarker(found, token.Length, alias));
                        }
                    }
                }
            }

            markers.Sort((left, right) =>
            {
                int order = left.index.CompareTo(right.index);
                return order != 0 ? order : right.tokenLength.CompareTo(left.tokenLength);
            });
            for (int index = markers.Count - 1; index > 0; index--)
            {
                if (markers[index].index == markers[index - 1].index)
                    markers.RemoveAt(index);
            }

            var aliases = new List<string>();
            var weights = new List<float>();
            if (markers.Count == 0)
            {
                aliases.Add(string.Empty);
                weights.Add(SpokenCharacterWeight(source));
            }
            else
            {
                if (markers[0].index > 0)
                {
                    string narration = source.Substring(0, markers[0].index);
                    if (!string.IsNullOrWhiteSpace(narration))
                    {
                        aliases.Add(string.Empty);
                        weights.Add(SpokenCharacterWeight(narration));
                    }
                }

                for (int index = 0; index < markers.Count; index++)
                {
                    DialogueMarker marker = markers[index];
                    int contentStart = marker.index + marker.tokenLength;
                    int contentEnd = index + 1 < markers.Count ? markers[index + 1].index : source.Length;
                    string spoken = contentEnd > contentStart
                        ? source.Substring(contentStart, contentEnd - contentStart)
                        : string.Empty;
                    aliases.Add(marker.alias);
                    weights.Add(SpokenCharacterWeight(spoken));
                }
            }

            float totalWeight = 0f;
            foreach (float weight in weights)
                totalWeight += weight;
            totalWeight = Mathf.Max(1f, totalWeight);

            var result = new List<DialogueSegment>(aliases.Count);
            float cumulative = 0f;
            for (int index = 0; index < aliases.Count; index++)
            {
                cumulative += weights[index];
                result.Add(new DialogueSegment
                {
                    alias = aliases[index],
                    endNormalized = Mathf.Clamp01(cumulative / totalWeight)
                });
            }
            return result;
        }

        private static float SpokenCharacterWeight(string text)
        {
            int count = 0;
            foreach (char character in text ?? string.Empty)
            {
                if (!char.IsWhiteSpace(character))
                    count++;
            }
            return Mathf.Max(1f, count);
        }

        private void ActivateDialogueSegment(string alias)
        {
            RestAllDialogueMouths();
            RemoveAllDialogueHeadOffsets();
            activeDialogueSpeakerAlias = alias ?? string.Empty;
            activeDialogueActor = ResolveDialogueActor(activeDialogueSpeakerAlias);
            activeDialogueListener = FindNearestDialogueListener(activeDialogueActor);
        }

        private DialogueActorBinding ResolveDialogueActor(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias) || dialogueActors == null)
                return null;

            DialogueActorBinding inactiveMatch = null;
            foreach (DialogueActorBinding actor in dialogueActors)
            {
                if (actor == null || !actor.initialized || actor.aliases == null)
                    continue;
                bool matches = false;
                foreach (string candidate in actor.aliases)
                {
                    if (string.Equals(candidate?.Trim(), alias.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        matches = true;
                        break;
                    }
                }
                if (!matches)
                    continue;
                if (actor.actorRoot.gameObject.activeInHierarchy)
                    return actor;
                inactiveMatch ??= actor;
            }
            return inactiveMatch != null && inactiveMatch.actorRoot.gameObject.activeInHierarchy
                ? inactiveMatch
                : null;
        }

        private DialogueActorBinding FindNearestDialogueListener(DialogueActorBinding speaker)
        {
            if (speaker == null || speaker.actorRoot == null || dialogueActors == null)
                return null;

            DialogueActorBinding best = null;
            float bestDistance = float.PositiveInfinity;
            Vector3 speakerPosition = speaker.head != null ? speaker.head.position : speaker.actorRoot.position;
            foreach (DialogueActorBinding candidate in dialogueActors)
            {
                if (candidate == null || candidate == speaker || !candidate.initialized ||
                    candidate.actorRoot == null || candidate.actorRoot == speaker.actorRoot ||
                    !candidate.actorRoot.gameObject.activeInHierarchy)
                    continue;
                Vector3 listenerPosition = candidate.head != null ? candidate.head.position : candidate.actorRoot.position;
                float distance = (listenerPosition - speakerPosition).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private void UpdateDialoguePerformanceVisuals()
        {
            float envelope = DialogueSpeechEnvelope();
            activeDialogueEnvelope = envelope;
            float mouthBlend = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            if (dialogueActors != null)
            {
                foreach (DialogueActorBinding actor in dialogueActors)
                {
                    if (actor == null || !actor.initialized || actor.mouth == null)
                        continue;
                    Vector3 target = actor.mouthRestScale;
                    if (actor == activeDialogueActor && actor.actorRoot.gameObject.activeInHierarchy)
                    {
                        target = new Vector3(
                            actor.mouthRestScale.x * Mathf.Lerp(1f, 0.82f, envelope),
                            // The authored mouth mesh is only a few millimetres
                            // tall.  4.8x stayed below the visible runtime
                            // aperture threshold on a portrait phone; 5.6x keeps
                            // the motion readable without changing geometry.
                            actor.mouthRestScale.y * Mathf.Lerp(1f, 5.6f, envelope),
                            actor.mouthRestScale.z);
                    }
                    actor.mouth.localScale = Vector3.Lerp(actor.mouth.localScale, target, mouthBlend);
                }
            }

            if (activeDialogueActor == null || activeDialogueListener == null)
                return;
            ApplyDialogueGaze(activeDialogueActor, activeDialogueListener);
            ApplyDialogueGaze(activeDialogueListener, activeDialogueActor);
        }

        private float DialogueSpeechEnvelope()
        {
            if (!dialoguePerformanceUsesVoice)
                return SubtitleSpeechEnvelope();
            if (dialogueVoiceSource == null || dialogueVoiceSource.clip == null ||
                !dialogueVoiceSource.isPlaying)
                return SubtitleSpeechEnvelope();

            int currentTimeSamples = dialogueVoiceSource.timeSamples;
            if (currentTimeSamples != dialogueVoiceLastTimeSamples)
            {
                dialogueVoiceLastTimeSamples = currentTimeSamples;
                dialogueVoiceLastAdvanceAt = Time.unscaledTime;
            }
            else if (Time.unscaledTime - dialogueVoiceLastAdvanceAt > 0.08f)
            {
                // Headless runners and output-disabled devices can report an
                // isPlaying source whose DSP playhead is frozen.  A frozen RMS
                // would hold the mouth permanently open, so use the specified
                // subtitle rhythm until the voice playhead advances again.
                return SubtitleSpeechEnvelope();
            }

            dialogueVoiceSource.GetOutputData(dialogueAudioSamples, 0);
            float rms = DialogueRms(dialogueAudioSamples);
            if (rms <= 0.0001f)
            {
                // GetOutputData can be silent in headless/Test Runner audio even
                // while the source is advancing.  Sample the actual voice clip
                // at the same playhead so runtime tests and muted devices still
                // use voice RMS instead of freezing the mouth at rest.
                AudioClip clip = dialogueVoiceSource.clip;
                int frames = Mathf.Max(1, dialogueAudioSamples.Length / Mathf.Max(1, clip.channels));
                int offset = Mathf.Clamp(dialogueVoiceSource.timeSamples, 0, Mathf.Max(0, clip.samples - frames));
                if (clip.GetData(dialogueAudioSamples, offset))
                    rms = DialogueRms(dialogueAudioSamples);
            }

            // A truly silent/missing voice region falls back to subtitle rhythm;
            // otherwise the recorded waveform remains the driver.
            if (rms <= 0.0001f)
                return SubtitleSpeechEnvelope();
            float rmsEnvelope = Mathf.Clamp01(Mathf.InverseLerp(0.0008f, 0.3f, rms));
            // Mastered voice clips can hold an almost constant short-window RMS.
            // Keep RMS as the amplitude driver, then add a small syllabic carrier
            // so a sustained vowel does not freeze the mouth at one aperture.
            float articulation = 0.35f +
                                 Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9.5f +
                                                    dialogueVoiceSource.time * 1.7f)) * 0.65f;
            return rmsEnvelope * articulation;
        }

        private static float DialogueRms(float[] samples)
        {
            float energy = 0f;
            foreach (float sample in samples)
                energy += sample * sample;
            return samples.Length > 0 ? Mathf.Sqrt(energy / samples.Length) : 0f;
        }

        private static float SubtitleSpeechEnvelope()
        {
            return 0.2f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8.5f)) * 0.62f;
        }

        private void ApplyDialogueGaze(DialogueActorBinding actor, DialogueActorBinding target)
        {
            if (actor?.actorRoot == null || actor.head == null || target?.actorRoot == null)
                return;

            Vector3 targetPosition = target.head != null ? target.head.position : target.actorRoot.position;
            Vector3 flatDirection = targetPosition - actor.actorRoot.position;
            flatDirection.y = 0f;
            StoryPlayerMovement movingPlayer = actor.actorRoot.GetComponent<StoryPlayerMovement>();
            if (flatDirection.sqrMagnitude > 0.0025f && (movingPlayer == null || !movingPlayer.IsMoving))
            {
                Quaternion bodyTarget = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                actor.actorRoot.rotation = Quaternion.RotateTowards(
                    actor.actorRoot.rotation,
                    bodyTarget,
                    dialogueBodyTurnSpeed * Time.unscaledDeltaTime);
            }

            Vector3 headDirection = targetPosition - actor.head.position;
            if (headDirection.sqrMagnitude < 0.0001f)
                return;
            Vector3 localDirection = actor.actorRoot.InverseTransformDirection(headDirection.normalized);
            float horizontal = Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z);
            float desiredYaw = Mathf.Clamp(
                Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg,
                -dialogueHeadYawLimit,
                dialogueHeadYawLimit);
            float desiredPitch = Mathf.Clamp(
                -Mathf.Atan2(localDirection.y, Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg,
                -dialogueHeadPitchLimit,
                dialogueHeadPitchLimit);
            float blend = 1f - Mathf.Exp(-dialogueHeadDamping * Time.unscaledDeltaTime);
            actor.appliedHeadYaw = Mathf.Lerp(actor.appliedHeadYaw, desiredYaw, blend);
            actor.appliedHeadPitch = Mathf.Lerp(actor.appliedHeadPitch, desiredPitch, blend);

            Quaternion animationRotation = actor.head.localRotation * Quaternion.Inverse(actor.appliedHeadOffset);
            actor.appliedHeadOffset = Quaternion.Euler(
                actor.appliedHeadPitch,
                actor.appliedHeadYaw,
                0f);
            actor.head.localRotation = animationRotation * actor.appliedHeadOffset;
        }

        private void StopDialoguePerformance()
        {
            if (dialoguePerformanceRoutine != null)
            {
                StopCoroutine(dialoguePerformanceRoutine);
                dialoguePerformanceRoutine = null;
            }
            ClearDialoguePerformanceVisuals();
        }

        private void ClearDialoguePerformanceVisuals()
        {
            dialoguePerformanceActive = false;
            dialoguePerformanceUsesVoice = false;
            activeDialogueEnvelope = 0f;
            RestAllDialogueMouths();
            RemoveAllDialogueHeadOffsets();
            activeDialogueActor = null;
            activeDialogueListener = null;
            activeDialogueSpeakerAlias = string.Empty;
        }

        private void RestAllDialogueMouths()
        {
            if (dialogueActors == null)
                return;
            foreach (DialogueActorBinding actor in dialogueActors)
            {
                if (actor != null && actor.initialized && actor.mouth != null)
                    actor.mouth.localScale = actor.mouthRestScale;
            }
        }

        private void RemoveAllDialogueHeadOffsets()
        {
            if (dialogueActors == null)
                return;
            foreach (DialogueActorBinding actor in dialogueActors)
            {
                if (actor == null || !actor.initialized || actor.head == null)
                    continue;
                actor.head.localRotation = actor.head.localRotation * Quaternion.Inverse(actor.appliedHeadOffset);
                actor.appliedHeadOffset = Quaternion.identity;
                actor.appliedHeadYaw = 0f;
                actor.appliedHeadPitch = 0f;
            }
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
