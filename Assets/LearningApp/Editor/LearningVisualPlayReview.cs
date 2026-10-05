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
    // Real button/pointer callbacks, isolated saves, muted audio, and Game View captures.
    public static class LearningVisualPlayReview
    {
        private const string Folder = "ClientExports/DepremApp/VisualPlay";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> report = new List<string>();
        private static readonly List<string> errors = new List<string>();
        private static string previousSave;
        private static int previousSize;
        private static bool running, reloadLocked;
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;
        private static object Field(string name) => typeof(LearningAppController).GetField(name, Private).GetValue(App);
        private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name); Check(button != null && button.enabledInHierarchy, "Missing/disabled " + name);
            typeof(Clickable).GetMethod("SimulateSingleClick", Private).Invoke(button.clickable, new object[] { null, 0 });
        }
        private static void Log(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message); }
        private static EditorWindow View => EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        private static PropertyInfo Size => View.GetType().GetProperty("selectedSizeIndex", Private | BindingFlags.Public);
        private static void Dimensions(int w, int h) => typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { w, h, "Visual Play " + w + "x" + h, false });
        private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Folder + "/" + name + ".png");

        [MenuItem("Tools/Deprem App/Review/Visual Play Verification")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play mode; run one review at a time.");
            running = true; report.Clear(); errors.Clear(); previousSave = LearningProgress.Current.SavePath; previousSize = (int)Size.GetValue(View);
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/verification.txt", "RUNNING\n");
            Application.logMessageReceived += Log;
            EditorApplication.LockReloadAssemblies(); reloadLocked = true;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            try { EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Run())); }
            catch { running = false; Application.logMessageReceived -= Log; ReleaseReloadLock(); throw; }
        }
        private static void OnPlayModeChanged(PlayModeStateChange state)
        { if (state == PlayModeStateChange.ExitingPlayMode) ReleaseReloadLock(); }
        private static void ReleaseReloadLock()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (!reloadLocked) return;
            reloadLocked = false; EditorApplication.UnlockReloadAssemblies();
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
            Application.logMessageReceived -= Log;
            report.Add(errors.Count == 0 ? "PASS: no runtime errors." : "FAIL: " + string.Join("; ", errors));
            try
            {
                LearningProgress.UseVerificationStore(previousSave);
                if (EditorApplication.isPlaying) SceneManager.LoadScene(LearningGameBridge.SceneName);
                Size.SetValue(View, previousSize);
                File.WriteAllLines(Folder + "/verification.txt", report);
            }
            finally { running = false; ReleaseReloadLock(); }
        }
        private static bool Visible(VisualElement element)
        {
            for (var node = element; node != null; node = node.parent)
                if (node.resolvedStyle.display == DisplayStyle.None || node.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }
        private static void CheckVisualStage(string game)
        {
            var body = UI.Q<ScrollView>("Body");
            Check(!body.Query<Label>().ToList().Any(l => Visible(l) && !string.IsNullOrWhiteSpace(l.text) && (!l.ClassListContains("play-caption") || l.text.Length > 30)), game + ": long instruction in child gameplay");
            Check(!body.Query<Button>().ToList().Any(b => Visible(b) && !string.IsNullOrWhiteSpace(b.text)), game + ": written button in child gameplay");
            Check(body.Query<Image>().ToList().All(i => i.image != null), game + ": missing artwork");
            foreach (var button in body.Query<Button>().ToList().Where(Visible))
            {
                Check(button.worldBound.width >= 44 && button.worldBound.height >= 44, game + ": small touch target " + button.name);
                Check(button.worldBound.xMin >= body.worldBound.xMin - 1 && button.worldBound.xMax <= body.worldBound.xMax + 1, game + ": horizontal overflow " + button.name);
            }
            Check(UI.Q("MiniGuide") != null && UI.Q("MiniProgress") != null, game + ": missing visual guide/progress");
        }
        private static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.GetFullPath(".codex_tmp/learning-app/visual-" + Guid.NewGuid().ToString("N") + ".json"));
            LearningProgress.Current.Data.music = false; LearningProgress.Current.Data.effects = false;
            SceneManager.LoadScene(LearningGameBridge.SceneName); Dimensions(1080,1920); yield return new EditorWaitForSeconds(.8f);
            App.ShowTab("games"); yield return new EditorWaitForSeconds(.3f); Capture("games"); yield return new EditorWaitForSeconds(.2f);
            foreach (bool adult in new[] { false, true })
            {
                App.ActiveProfile.adult = adult;
                foreach (var definition in App.Catalog.Levels(adult).Where(l => l.type != "catch"))
                {
                    // Include every quiz scenario, including options previously missing images.
                    var source = JsonUtility.FromJson<LearningLevel>(JsonUtility.ToJson(definition));
                    if (source.type == "quiz") source.questionCount = source.questions.Length;
                    App.StartMiniGame(source); yield return new EditorWaitForSeconds(.3f);
                    var level = (LearningLevel)Field("miniLevel");
                    if (!adult) { CheckVisualStage(level.type); Capture(level.type); yield return new EditorWaitForSeconds(.15f); }
                    if (level.type == "bag")
                    {
                        Press("Tile_" + level.items.First(id => !level.correctItems.Contains(id)));
                        Check((int)Field("miniDone") == 0 && (int)Field("miniMistakes") == 1, "Incorrect bag item changed progress");
                        if (!adult) { Check(UI.Q("MiniFeedback").Q("Symbol_replay") != null, "Wrong answer lacks shape feedback"); Press("MiniBack"); Press("ResumeMiniGame"); }
                        foreach (string id in level.correctItems) Press("Tile_" + id);
                    }
                    if (level.type == "sort") foreach (string id in level.items) { Press("Tile_" + id); Press(level.packItems.Contains(id) ? "Çantaya koy" : "Dışarıda bırak"); }
                    if (level.type == "match") foreach (var target in level.targets) { Press("Tile_" + target.accepts); Press(target.label); }
                    if (level.type == "danger") foreach (var target in level.actions.Where(a => a.dangerous)) Press("Tile_" + target.id);
                    if (level.type == "sequence") foreach (var round in level.rounds) foreach (var step in round.steps) Press("Step_" + step.id);
                    if (level.type == "quiz") foreach (var question in level.questions)
                    {
                        yield return new EditorWaitForSeconds(.1f);
                        if (!adult) CheckVisualStage("quiz: " + question.prompt);
                        Press("MiniChoice_" + Array.FindIndex(question.choices, c => c.correct));
                    }
                    if (level.type == "memory")
                    {
                        var observed = new List<string>(); string prior = null;
                        double deadline = EditorApplication.timeSinceStartup + 20;
                        while ((bool)Field("memoryWatching") && EditorApplication.timeSinceStartup < deadline)
                        {
                            var lit = UI.Query<Button>(className: "memory-lit").First(); string id = lit?.name;
                            if (id != null && id != prior) observed.Add(id);
                            prior = id; yield return null;
                        }
                        Check(observed.Count == level.sequenceLength, "Memory visual sequence incomplete");
                        foreach (string id in observed) Press(id);
                    }
                    Check(!(bool)Field("miniActive") && App.ActiveProfile.results.Any(r => r.id == level.SaveId), "Game did not finish: " + level.SaveId);
                    if (!adult) { Check(UI.Q<Button>("ReplayMiniGame") != null, "Missing icon replay"); Capture("reward"); yield return new EditorWaitForSeconds(.15f); Press("FinishMiniGame"); }
                    report.Add("PASS: " + level.SaveId + " completes through UI with sound disabled.");
                }
            }
            App.ActiveProfile.adult = false;
            foreach (var size in new[] { new Vector2Int(750,1334), new Vector2Int(1536,2048) })
            {
                Dimensions(size.x,size.y);
                foreach (var level in App.Catalog.Levels(false))
                {
                    App.StartMiniGame(level); yield return new EditorWaitForSeconds(.3f); CheckVisualStage(level.type + " " + size);
                    if (level.type == "sort") { Capture("sort-" + size.x); yield return new EditorWaitForSeconds(.15f); }
                }
                report.Add("PASS: all eight child games at " + size + ": short optional captions, artwork present, 44+ touch targets, no horizontal overflow.");
            }
            Dimensions(1080,1920);
            foreach (bool adult in new[] { false, true })
            {
                App.ActiveProfile.adult = adult;
                var level = App.Catalog.Levels(adult).First(l => l.type == "catch"); App.StartMiniGame(level); yield return new EditorWaitForSeconds(.4f);
                if (!adult) { CheckVisualStage("catch"); Capture("catch"); yield return new EditorWaitForSeconds(.2f); }
                double deadline = EditorApplication.timeSinceStartup + 55;
                while ((bool)Field("miniActive") && EditorApplication.timeSinceStartup < deadline)
                {
                    var field = UI.Q("CatchField");
                    var good = field.Query<Image>(className:"catch-item").ToList().FirstOrDefault(i => level.goodItems.Any(id => ((Texture2D)i.image).name == App.Catalog.Item(id).textureKey));
                    float x = good != null ? good.worldBound.center.x : field.worldBound.xMin + 30;
                    var e = new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(x,field.worldBound.yMax-40) };
                    using (var down = PointerDownEvent.GetPooled(e)) { down.target = field; field.SendEvent(down); }
                    e.type = EventType.MouseUp;
                    using (var up = PointerUpEvent.GetPooled(e)) { up.target = field; field.SendEvent(up); }
                    yield return null;
                }
                Check(!(bool)Field("miniActive"), "Catch did not finish"); report.Add("PASS: " + level.SaveId + " via pointer input.");
            }
            report.Add("COMPLETE: all 16 game modes; all child quiz scenarios; silent play; isolated save restored.");
        }
    }
}
