using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    [Serializable] public sealed class LearningNarrationEntry { public string text, spoken, resource; }
    [Serializable] public sealed class LearningNarrationCatalog
    {
        public int version;
        public string voice;
        public bool synthetic;
        public LearningNarrationEntry[] entries;
        public static LearningNarrationCatalog Load()
        {
            var asset = Resources.Load<TextAsset>("LearningApp/Narration/manifest");
            return asset == null ? null : JsonUtility.FromJson<LearningNarrationCatalog>(asset.text);
        }
    }

    // Owns a single offline voice channel. Page changes cancel pending loads and speech.
    public sealed partial class LearningAppController
    {
        private AudioSource narrator;
        private AudioClip narrationClip;
        private readonly Dictionary<string, string> narrationResources = new Dictionary<string, string>(StringComparer.Ordinal);
        private string[] narrationContext = Array.Empty<string>();
        private int narrationRevision;
        private bool narrationActive, narrationPaused;
        private Button narrationReplay, narrationStop;
        private Label narrationStatus;
        private readonly List<string> missingNarration = new List<string>();
        public bool IsNarrating => narrationActive;
        public string[] MissingNarration => missingNarration.ToArray();
        private const string RetryVoice = "Birlikte bir daha düşünelim.";
        private const string TryAgainVoice = "Denemeye devam edebilirsin. Öğrenmek için zamanın var.";
        private const string LessonCompleteVoice = "Bu bölümü tamamladın. İstersen biraz dinlenebilir, sonra devam edebilirsin.";
        private const string GameCompleteVoice = "Oyunu tamamladın. Denediğin için teşekkür ederim. İstersen yeniden oynayabiliriz.";

        private void InitializeNarration()
        {
            if (narrator == null) narrator = gameObject.AddComponent<AudioSource>();
            narrator.playOnAwake = false; narrator.loop = false; narrator.spatialBlend = 0; narrator.priority = 32;
            narrationResources.Clear();
            var voices = LearningNarrationCatalog.Load();
            if (voices?.entries != null)
                foreach (var entry in voices.entries)
                    if (!string.IsNullOrEmpty(entry.text) && !string.IsNullOrEmpty(entry.resource))
                        narrationResources[entry.text] = entry.resource;
        }

        private IEnumerable<string> ExerciseVoice(LearningExercise exercise)
        {
            if (exercise.kind == "info") return new[] { exercise.title, exercise.text };
            return new[] { exercise.prompt, "Seçenekleri dinleyelim." }.Concat(exercise.choices.Select(c => c.label));
        }

        private void Narrate(IEnumerable<string> lines, bool? automatic = null)
        {
            StopNarration();
            narrationContext = lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            UpdateNarrationControls();
            if ((automatic ?? !Profile.adult) && Profile.voiceEnabled && Profile.voiceAutoplay) PlayNarration();
        }

        private void PlayNarration()
        {
            StopNarration();
            if (narrator == null || !Profile.voiceEnabled || narrationContext.Length == 0 || narrationPaused || store.Data.volume * Profile.voiceVolume <= .001f) return;
            narrationActive = true;
            narrator.volume = store.Data.volume * Profile.voiceVolume;
            UpdateNarrationControls();
            StartCoroutine(PlayNarrationSequence(narrationContext, narrationRevision));
        }

        private IEnumerator PlayNarrationSequence(string[] lines, int revision)
        {
            // Avoid speaking over a tap or an immediately replaced page.
            yield return new WaitForSecondsRealtime(.18f);
            foreach (string line in lines)
            {
                if (revision != narrationRevision) yield break;
                if (!narrationResources.TryGetValue(line, out string resource))
                {
                    if (!missingNarration.Contains(line)) missingNarration.Add(line);
                    continue;
                }
                var request = Resources.LoadAsync<AudioClip>(resource);
                yield return request;
                if (revision != narrationRevision) yield break;
                narrationClip = request.asset as AudioClip;
                if (narrationClip == null)
                {
                    if (!missingNarration.Contains(line)) missingNarration.Add(line);
                    continue;
                }
                if (narrationClip.loadState == AudioDataLoadState.Unloaded) narrationClip.LoadAudioData();
                float loadDeadline = Time.realtimeSinceStartup + 10f;
                while (narrationClip != null && narrationClip.loadState == AudioDataLoadState.Loading && revision == narrationRevision && Time.realtimeSinceStartup < loadDeadline) yield return null;
                if (revision != narrationRevision) yield break;
                if (narrationClip == null || narrationClip.loadState != AudioDataLoadState.Loaded)
                {
                    if (!missingNarration.Contains(line)) missingNarration.Add(line);
                    ReleaseNarrationClip(); continue;
                }
                while (narrationPaused && revision == narrationRevision) yield return null;
                if (revision != narrationRevision) yield break;
                narrator.clip = narrationClip;
                narrator.Play();
                while (revision == narrationRevision && (narrator.isPlaying || narrationPaused)) yield return null;
                if (revision != narrationRevision) yield break;
                ReleaseNarrationClip();
                yield return new WaitForSecondsRealtime(.14f);
            }
            if (revision != narrationRevision) yield break;
            narrationActive = false; UpdateNarrationControls();
        }

        public void StopNarration()
        {
            narrationRevision++; narrationActive = false;
            if (narrator != null) narrator.Stop();
            ReleaseNarrationClip();
            UpdateNarrationControls();
        }

        private void ReleaseNarrationClip()
        {
            if (narrator != null) narrator.clip = null;
            if (narrationClip != null) Resources.UnloadAsset(narrationClip);
            narrationClip = null;
        }

        private void ResetNarrationPage()
        {
            StopNarration(); narrationContext = Array.Empty<string>();
            narrationReplay = narrationStop = null; narrationStatus = null;
        }

        private void NarrationControls(VisualElement parent)
        {
            var controls = Box(parent, "narration-controls"); controls.name = "NarrationControls";
            narrationReplay = Button(controls, "Dinle", PlayNarration, "secondary compact", "NarrationReplay", false);
            narrationReplay.tooltip = "Bu bölümü kadın anlatıcıdan dinle";
            narrationStop = Button(controls, "Durdur", StopNarration, "secondary compact", "NarrationStop", false);
            narrationStatus = Text(controls, "", "small narration-status");
            UpdateNarrationControls();
        }

        private void UpdateNarrationControls()
        {
            bool enabled = store != null && Profile.voiceEnabled;
            narrationReplay?.SetEnabled(enabled && narrationContext.Length > 0);
            if (narrationReplay != null) narrationReplay.text = narrationActive ? "Baştan dinle" : "Dinle";
            narrationStop?.SetEnabled(narrationActive);
            if (narrationStatus != null) narrationStatus.text = !enabled ? "Anlatım kapalı" : narrationActive ? "Dinliyoruz…" : "Kendi hızında";
        }

        private void UpdateNarrationMix()
        {
            if (narrator == null || music == null || store == null) return;
            narrator.volume = store.Data.volume * Profile.voiceVolume;
            if (narrationActive && narrator.volume <= .001f) StopNarration();
            float target = store.Data.volume * .38f * (narrationActive ? .18f : 1f);
            music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * .8f);
        }

        private void PauseNarration(bool pause)
        {
            narrationPaused = pause;
            if (narrator == null || !narrationActive) return;
            if (pause) narrator.Pause(); else narrator.UnPause();
        }

        private void AddNarrationSettings()
        {
            Text(body, "Sesli anlatım", "section-title");
            Text(body, "Türkçe kadın sesi · Çevrimdışı dinlenir", "small");
            Text(body, "Anlatıcı sesi yapay zekâyla oluşturulmuştur.", "small");
            var enabled = new Toggle("Sesli anlatımı aç") { value = Profile.voiceEnabled, name = "VoiceEnabled" }; body.Add(enabled);
            enabled.RegisterValueChangedCallback(e => { Profile.voiceEnabled = e.newValue; if (!e.newValue) StopNarration(); Save(); });
            var autoplay = new Toggle("Çocuk modunda kendiliğinden oku") { value = Profile.voiceAutoplay, name = "VoiceAutoplay" }; body.Add(autoplay);
            autoplay.RegisterValueChangedCallback(e => { Profile.voiceAutoplay = e.newValue; if (!e.newValue) StopNarration(); Save(); });
            var volume = new Slider("Anlatıcı ses düzeyi", 0, 1) { value = Profile.voiceVolume, name = "VoiceVolume" }; body.Add(volume);
            volume.RegisterValueChangedCallback(e => { Profile.voiceVolume = e.newValue; UpdateNarrationMix(); });
            volume.RegisterCallback<PointerCaptureOutEvent>(_ => Save());
            Button(body, "Sesi dinle", () => {
                Narrate(new[] { "Merhaba. Bugün seninle birlikte öğreneceğiz. Acele etmene gerek yok." }, false);
                PlayNarration();
            }, "secondary", "VoicePreview", false);
            NarrationControls(body);
        }
    }
}
