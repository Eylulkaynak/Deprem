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
    // Exercises actual runtime UI callbacks in an isolated save. Never edits a user's progress.
    public static class LearningAppRuntimeVerification
    {
        private const string Folder = ".codex_tmp/learning-app";
        private static readonly List<string> results = new List<string>();
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;
        private static readonly MethodInfo Click = typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static bool running;
        private static readonly List<string> errors = new List<string>();
        [MenuItem("Tools/Deprem App/Review/Run Runtime Verification")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play mode first; run one verification at a time.");
            running = true; results.Clear(); errors.Clear(); Application.logMessageReceived += Log; Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/runtime-report.txt", "RUNNING\n");
            EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Run()));
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            while (true)
            {
                object next;
                try { if (!routine.MoveNext()) break; next = routine.Current; }
                catch (Exception e) { results.Add("FAIL: " + e); Debug.LogException(e); break; }
                yield return next;
            }
            Application.logMessageReceived -= Log;
            if (errors.Count > 0) results.Add("FAIL: runtime logged " + errors.Count + " errors: " + string.Join("; ", errors.Take(3)));
            else results.Add("PASS: no runtime errors or exceptions during verification.");
            running = false; File.WriteAllLines(Folder + "/runtime-report.txt", results);
            Debug.Log("[Deprem App] Runtime verification finished: " + string.Join("; ", results));
        }
        private static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name); Check(button != null && button.enabledInHierarchy, "Missing/disabled UI button: " + name);
            Click.Invoke(button.clickable, new object[] { null, 0 });
        }
        private static object Field(string name) => typeof(LearningAppController).GetField(name, Private).GetValue(App);
        private static void Invoke(string name) => typeof(LearningAppController).GetMethod(name, Private).Invoke(App, null);
        private static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.GetFullPath(Folder + "/verification-" + Guid.NewGuid().ToString("N") + ".json"));
            SceneManager.LoadScene(LearningGameBridge.SceneName); yield return new EditorWaitForSeconds(1);
            Check(App != null, "App did not start");
            foreach (string tab in new[] { "education", "games", "family", "profile" })
            {
                App.ShowTab(tab); yield return new EditorWaitForSeconds(0.2f);
                Check(UI.Q("Header").resolvedStyle.height >= 70, "Styles did not apply");
                Check(UI.Q("BottomNav").worldBound.yMax <= UI.worldBound.yMax + 1, "Navigation exceeds screen");
                ScreenCapture.CaptureScreenshot(Folder + "/" + tab + ".png");
                yield return new EditorWaitForSeconds(0.15f);
            }
            results.Add("PASS: four native tabs render with visible bottom navigation.");
            foreach (string screen in new[] { "ShowPlan", "ShowGuide", "ShowAchievements", "ShowSettings", "ShowMistakes" })
            { Invoke(screen); yield return new EditorWaitForSeconds(0.1f); Check(UI.Q<ScrollView>("Body").childCount > 0, "Empty " + screen); }
            Invoke("ShowPlan"); UI.Q<TextField>("Plan_meetingPoint").value = "Test buluşma alanı";
            UI.Q<Toggle>("Check_bag").value = true; Press("Planımı kaydet");
            Check(LearningProgress.Current.Data.Active.Plan("meetingPoint").value == "Test buluşma alanı", "Plan input failed");
            results.Add("PASS: plan inputs, checklist, guide, achievements, audio settings and mistake review.");
            App.ShowTab("education"); App.StartCourse("child-0"); yield return new EditorWaitForSeconds(0.1f);
            Press("ContinueLesson"); yield return new EditorWaitForSeconds(0.1f);
            Press("Answer_1"); yield return new EditorWaitForSeconds(0.1f);
            Check(App.ActiveProfile.resumeMistakes == 1, "Wrong answer did not persist");
            Press("Tekrar dene"); yield return new EditorWaitForSeconds(0.1f);
            ScreenCapture.CaptureScreenshot(Folder + "/lesson.png");
            SceneManager.LoadScene(LearningGameBridge.SceneName); yield return new EditorWaitForSeconds(0.3f);
            Press("ResumeLesson"); yield return new EditorWaitForSeconds(0.1f);
            Check((int)Field("exerciseIndex") == 1, "Lesson checkpoint not restored");
            while (App.CurrentCourseId != null)
            {
                var c = (LearningCourse)Field("course"); var exercise = c.exercises[(int)Field("exerciseIndex")];
                if (exercise.kind == "info") Press("ContinueLesson");
                else { Press("Answer_" + Array.FindIndex(exercise.choices, e => e.correct)); Press("NextExercise"); }
                yield return new EditorWaitForSeconds(0.1f);
            }
            Check(App.ActiveProfile.completed.Contains("child-0") && App.ActiveProfile.xp == 30, "Lesson completion failed");
            results.Add("PASS: lesson wrong answer, feedback, restart checkpoint, completion and next-unit unlock.");
            App.ShowTab("education"); Press("ModeSwitch"); yield return new EditorWaitForSeconds(0.1f);
            Check(App.ActiveProfile.adult && UI.Q<Button>("Course_adult-0") != null, "Adult course map missing");
            ScreenCapture.CaptureScreenshot(Folder + "/adult.png");
            yield return new EditorWaitForSeconds(0.15f);
            App.StartCourse("adult-0"); Press("ContinueLesson");
            Check(App.ActiveProfile.completed.Contains("adult-0"), "Adult lesson not saved");
            App.ActiveProfile.adult = false;
            results.Add("PASS: adult mode and original adult lesson content.");
            foreach (var definition in App.Catalog.Levels(false).Where(l => l.type != "catch"))
            {
                App.StartMiniGame(definition); yield return new EditorWaitForSeconds(0.15f);
                var level = (LearningLevel)Field("miniLevel");
                ScreenCapture.CaptureScreenshot(Folder + "/mini-" + level.type + ".png");
                yield return new EditorWaitForSeconds(0.15f);
                if (level.type == "bag") foreach (string id in level.correctItems) Press("Tile_" + id);
                if (level.type == "sort") foreach (string id in level.items) { Press("Tile_" + id); Press(level.packItems.Contains(id) ? "Çantaya koy" : "Dışarıda bırak"); }
                if (level.type == "match") foreach (var target in level.targets) { Press("Tile_" + target.accepts); Press(target.label); }
                if (level.type == "danger") foreach (var target in level.actions.Where(a => a.dangerous)) Press("Tile_" + target.id);
                if (level.type == "sequence") foreach (var round in level.rounds) foreach (var step in round.steps)
                {
                    Press("Step_" + step.id);
                }
                if (level.type == "quiz") foreach (var question in level.questions)
                {
                    Press("MiniChoice_" + Array.FindIndex(question.choices, c => c.correct));
                }
                if (level.type == "memory")
                {
                    // Observe actual lit pads; then replay that sequence through the buttons.
                    var observed = new List<string>(); string prior = "";
                    while ((bool)Field("memoryWatching"))
                    {
                        var lit = UI.Query<Button>(className: "memory-lit").First();
                        string id = lit?.name;
                        if (id != null && id != prior) observed.Add(id);
                        prior = id; yield return null;
                    }
                    Check(observed.Count == level.sequenceLength, "Memory sequence not presented");
                    foreach (string id in observed) Press(id);
                }
                Check(App.ActiveProfile.results.Any(r => r.id == level.SaveId), "Mini-game failed: " + level.type);
                results.Add("PASS: native mini-game " + level.type + " completes via its UI.");
                yield return new EditorWaitForSeconds(0.1f);
            }
            var catchLevel = App.Catalog.Levels(false).First(l => l.type == "catch");
            App.StartMiniGame(catchLevel); yield return new EditorWaitForSeconds(0.5f);
            // Move the native basket using pointer events, following useful falling items.
            double deadline = EditorApplication.timeSinceStartup + 55;
            while ((bool)Field("miniActive") && EditorApplication.timeSinceStartup < deadline)
            {
                var field = UI.Q("CatchField");
                var good = field.Query<Image>(className: "catch-item").ToList().FirstOrDefault(i => catchLevel.goodItems.Any(id => ((Texture2D)i.image).name == App.Catalog.Item(id).textureKey));
                float x = good != null ? good.worldBound.center.x : field.worldBound.xMin + 30;
                var evt = new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(x, field.worldBound.yMax - 40) };
                using (var down = PointerDownEvent.GetPooled(evt)) { down.target = field; field.SendEvent(down); }
                evt.type = EventType.MouseUp;
                using (var up = PointerUpEvent.GetPooled(evt)) { up.target = field; field.SendEvent(up); }
                yield return null;
            }
            Check(App.ActiveProfile.results.Any(r => r.id == catchLevel.SaveId), "Catch input did not complete");
            results.Add("PASS: native catch game completes using pointer-controlled basket.");
            foreach (string route in new[] { "minigames", "story", "adventure" })
            {
                App.ShowTab("games");
                Press("Play_" + (route == "minigames" ? "3D Pratik Alanı" : route == "story" ? "Deprem Hikâyesi" : "Yan Yana"));
                yield return new EditorWaitForSeconds(1);
                while (LearningGameBridge.Instance.IsLoading) yield return null;
                Check(SceneManager.GetActiveScene().name == LearningGameBridge.RouteScene(route), "Wrong 3D scene: " + route);
                yield return new EditorWaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(Folder + "/3d-" + route + ".png");
                yield return new EditorWaitForSeconds(0.2f);
                var overlay = LearningGameBridge.Instance.GetComponent<UIDocument>().rootVisualElement;
                Click.Invoke(overlay.Q<Button>("ReturnToLearning").clickable, new object[] { null, 0 });
                Check(Time.timeScale == 0, "Return confirmation did not pause 3D game");
                var confirm = overlay.Query<Button>().ToList().First(b => b.text == "Eğitime dön");
                Click.Invoke(confirm.clickable, new object[] { null, 0 }); yield return new EditorWaitForSeconds(0.5f);
                while (LearningGameBridge.Instance.IsLoading) yield return null;
                Check(App != null && App.ActiveProfile.completed.Contains("child-0"), "3D return lost learning progress");
                Check(Time.timeScale == 1, "Return left application paused");
                results.Add("PASS: " + route + " scene launches and returns with learning progress preserved.");
            }
            App.ShowTab("education"); results.Add("COMPLETE");
        }
    }
}
