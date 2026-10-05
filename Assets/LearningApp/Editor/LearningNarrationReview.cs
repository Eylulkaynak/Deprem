using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deprem.Learning.Editor
{
    public sealed class LearningNarrationImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/LearningApp/Resources/LearningApp/Narration/Clips/", StringComparison.Ordinal)) return;
            Configure((AudioImporter)assetImporter);
        }
        internal static void Configure(AudioImporter importer)
        {
            importer.forceToMono = true; importer.loadInBackground = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
        }
    }

    public static class LearningNarrationReview
    {
        private const string Folder = "ClientExports/DepremApp/Narration";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> report = new List<string>();
        private static bool running;
        private static string previousSave;
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;

        [MenuItem("Tools/Deprem App/Narration/Configure Audio Imports")]
        public static void ConfigureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/LearningApp/Resources/LearningApp/Narration/Clips" }))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (importer.forceToMono && importer.loadInBackground && importer.defaultSampleSettings.loadType == AudioClipLoadType.CompressedInMemory && !importer.defaultSampleSettings.preloadAudioData) continue;
                LearningNarrationImporter.Configure(importer); importer.SaveAndReimport();
            }
            Debug.Log("[Narration] Offline voice import settings applied.");
        }

        [MenuItem("Tools/Deprem App/Narration/Verify Runtime")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play mode first.");
            running = true; report.Clear(); previousSave = LearningProgress.Current.SavePath;
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/runtime-report.txt", "RUNNING\n");
            EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Run()));
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            while (true)
            {
                object next;
                try { if (!routine.MoveNext()) break; next = routine.Current; }
                catch (Exception e) { report.Add("FAIL: " + e); break; }
                yield return next;
            }
            App?.StopNarration();
            LearningProgress.UseVerificationStore(previousSave);
            SceneManager.LoadScene(LearningGameBridge.SceneName);
            File.WriteAllLines(Folder + "/runtime-report.txt", report);
            running = false; Debug.Log("[Narration] " + string.Join("; ", report));
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); report.Add("PASS: " + message); }
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name);
            if (button == null || !button.enabledInHierarchy) throw new InvalidOperationException("Missing/disabled " + name);
            typeof(Clickable).GetMethod("SimulateSingleClick", Private).Invoke(button.clickable, new object[] { null, 0 });
        }
        private static object Field(string name) => typeof(LearningAppController).GetField(name, Private).GetValue(App);
        private static void Invoke(string name, params object[] args) => typeof(LearningAppController).GetMethod(name, Private).Invoke(App, args);
        private static EditorWaitForSeconds Wait(float seconds = .5f) => new EditorWaitForSeconds(seconds);

        private static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.GetFullPath(Folder + "/verification-" + Guid.NewGuid().ToString("N") + ".json"));
            SceneManager.LoadScene(LearningGameBridge.SceneName); yield return Wait(1);
            App.StartCourse("child-0"); yield return Wait(1);
            var voice = (AudioSource)Field("narrator"); var music = (AudioSource)Field("music");
            Check(App.IsNarrating && voice.isPlaying && voice.clip != null, "Child lesson automatically plays real offline audio.");
            Check(music.volume < LearningProgress.Current.Data.volume * .2f, "Music ducks under voice.");
            Check(UI.Q<Button>("NarrationReplay").worldBound.height >= 44, "Narration control has a touch-sized target.");
            ScreenCapture.CaptureScreenshot(Folder + "/lesson.png");
            Invoke("OnApplicationPause", true); yield return Wait(.3f);
            Check(!voice.isPlaying && App.IsNarrating, "Application pause suspends narration.");
            Invoke("OnApplicationPause", false); yield return Wait(.3f);
            Check(voice.isPlaying, "Application resume continues narration.");
            Press("NarrationStop"); yield return Wait(.7f);
            Check(!App.IsNarrating && !voice.isPlaying && music.volume > LearningProgress.Current.Data.volume * .3f, "Stop silences voice and restores music.");
            Press("NarrationReplay"); yield return Wait(.8f);
            Check(voice.isPlaying, "Replay restarts the current lesson.");
            Press("ContinueLesson"); yield return Wait(.8f);
            Press("Answer_1"); yield return Wait(.8f);
            Check(App.ActiveProfile.resumeMistakes == 1 && App.IsNarrating, "Wrong answer records progress and plays supportive feedback.");
            Press("Tekrar dene"); Press("Answer_0"); yield return Wait(.8f);
            Check(App.IsNarrating, "Correct answer plays its explanation.");
            App.ShowTab("profile"); yield return Wait(.5f);
            Check(!App.IsNarrating && !voice.isPlaying, "Page navigation cancels old speech.");
            App.StartCourse("child-0"); App.ShowTab("profile"); yield return Wait(.8f);
            Check(!App.IsNarrating && !voice.isPlaying, "Immediate navigation also cancels a queued voice start.");
            Invoke("Narrate", new[] { "Doğru", "Yanlış" }, false); Invoke("PlayNarration");
            var heard = new HashSet<string>(); double deadline = EditorApplication.timeSinceStartup + 15;
            while (App.IsNarrating && EditorApplication.timeSinceStartup < deadline)
            {
                if (voice.isPlaying && voice.clip != null) heard.Add(voice.clip.name);
                yield return null;
            }
            yield return Wait(.5f);
            Check(heard.Count == 2 && !App.IsNarrating && voice.clip == null, "A multi-clip narration finishes naturally and releases its audio.");
            Check(music.volume > LearningProgress.Current.Data.volume * .3f, "Natural completion restores the music level.");
            float masterVolume = LearningProgress.Current.Data.volume;
            LearningProgress.Current.Data.volume = 0;
            App.StartMiniGame(App.Catalog.Levels(false).First(l => l.type == "memory")); yield return Wait(.4f);
            Check(!App.IsNarrating, "Muted audio does not delay the memory game with silent speech.");
            LearningProgress.Current.Data.volume = masterVolume;
            Invoke("ShowSettings"); UI.Q<Toggle>("VoiceAutoplay").value = false;
            App.StartCourse("child-0"); yield return Wait(.5f);
            Check(!App.IsNarrating, "Autoplay can be disabled independently.");
            Press("NarrationReplay"); yield return Wait(.8f);
            Check(voice.isPlaying, "Manual listen works with autoplay off.");
            Invoke("ShowSettings"); UI.Q<Toggle>("VoiceEnabled").value = false;
            App.StartCourse("child-0"); yield return Wait(.5f);
            Check(!App.IsNarrating && !UI.Q<Button>("NarrationReplay").enabledSelf, "Voice-off prevents automatic and manual speech.");
            App.ActiveProfile.voiceEnabled = App.ActiveProfile.voiceAutoplay = true;
            App.ActiveProfile.voiceVolume = .4f; App.ActiveProfile.adult = true;
            App.StartCourse("adult-0"); yield return Wait(.5f);
            Check(!App.IsNarrating, "Adult lessons wait for an explicit listen action.");
            Press("NarrationReplay"); yield return Wait(.8f);
            Check(voice.isPlaying && Mathf.Abs(voice.volume - LearningProgress.Current.Data.volume * .4f) < .01f, "Adult manual playback respects voice volume.");
            App.ActiveProfile.adult = false;
            foreach (var level in App.Catalog.Levels(false))
            {
                App.StartMiniGame(level); yield return Wait(.8f);
                Check(App.IsNarrating && voice.isPlaying, "Mini-game speaks: " + level.id);
                if (level.type == "quiz")
                {
                    var round = (LearningLevel)Field("miniLevel");
                    var context = (string[])Field("narrationContext");
                    Check(context.Skip(context.Length - round.questions[0].choices.Length).SequenceEqual(round.questions[0].choices.Select(c => c.label)), "Shuffled quiz choices are read in displayed order.");
                }
                if (level.type == "memory") Check((bool)Field("memoryWatching"), "Memory game waits while its instruction is spoken.");
            }
            App.ShowTab("profile"); Invoke("ShowGuide");
            Press("ReadGuide_" + App.Catalog.guides.First(g => !g.adult).title); yield return Wait(.8f);
            Check(voice.isPlaying, "Guide card has manual narration.");
            Check(App.MissingNarration.Length == 0, "No missing narration was requested.");
            App.StopNarration(); Invoke("ShowSettings"); yield return Wait(.3f);
            ScreenCapture.CaptureScreenshot(Folder + "/settings.png"); yield return Wait(.2f);
        }
    }
}
