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
    // Real Game View captures, isolated progress, and layout assertions. No mock rendering.
    public static class LearningPresentationReview
    {
        private const string Folder = "ClientExports/DepremApp/PremiumUI";
        private static readonly List<string> report = new List<string>();
        private static bool running;
        private static string previousSave;
        private static int previousSize;
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/Deprem App/Review/Premium UI Screenshots and Layout")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play mode; run one review at a time.");
            running = true; report.Clear();
            Directory.CreateDirectory(Folder + "/After"); Directory.CreateDirectory(Folder + "/Reports");
            previousSave = LearningProgress.Current.SavePath;
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            previousSize = (int)viewType.GetProperty("selectedSizeIndex", Private | BindingFlags.Public).GetValue(EditorWindow.GetWindow(viewType));
            File.WriteAllText(Folder + "/Reports/layout.txt", "RUNNING\n");
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
            // Restore the prior save source without touching any of its profile values.
            LearningProgress.UseVerificationStore(previousSave);
            SceneManager.LoadScene(LearningGameBridge.SceneName);
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            viewType.GetProperty("selectedSizeIndex", Private | BindingFlags.Public).SetValue(EditorWindow.GetWindow(viewType), previousSize);
            File.WriteAllLines(Folder + "/Reports/layout.txt", report);
            running = false;
            Debug.Log("[Premium UI] " + string.Join("; ", report));
        }
        private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name);
            Check(button != null && button.enabledInHierarchy, "Missing or disabled: " + name);
            typeof(Clickable).GetMethod("SimulateSingleClick", Private).Invoke(button.clickable, new object[] { null, 0 });
        }
        private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Folder + "/After/" + name + ".png");
        private static void Dimensions(int width, int height)
        {
            // Reuse the project's resolution helper rather than appending duplicate sizes.
            typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { width, height, "Learning Review " + width + "x" + height, false });
        }
        private static void CheckLayout(string context)
        {
            var safe = UI.Q("SafeArea").worldBound;
            var nav = UI.Q("BottomNav");
            Check(nav.worldBound.yMax <= safe.yMax + 1, context + ": bottom navigation outside safe area");
            Check(UI.Q<ScrollView>("Body").worldBound.height > 100, context + ": no usable scroll area");
            foreach (var button in nav.Query<Button>().ToList())
            {
                Check(button.worldBound.height >= 44, context + ": navigation touch target too small");
                Check(button.worldBound.xMin >= safe.xMin && button.worldBound.xMax <= safe.xMax + 1, context + ": horizontal navigation overflow");
                Check(button.Q<Image>().image != null, context + ": navigation artwork missing");
            }
            Check(UI.Q<Image>(className: "institution-logo").image != null, context + ": institution logo missing");
            Check(UI.Q("Header").worldBound.yMax <= UI.Q<ScrollView>("Body").worldBound.yMin + 1, context + ": header overlap");
            report.Add("PASS: " + context + "; logo, icons, touch targets, header, scroll area and safe bottom navigation.");
        }
        private static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.GetFullPath(".codex_tmp/learning-app/presentation-" + Guid.NewGuid().ToString("N") + ".json"));
            SceneManager.LoadScene(LearningGameBridge.SceneName);
            yield return new EditorWaitForSeconds(0.7f);
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(750, 1334), new Vector2Int(1536, 2048) })
            {
                Dimensions(size.x, size.y); App.ShowTab("education");
                yield return new EditorWaitForSeconds(0.7f);
                Check(Screen.width == size.x && Screen.height == size.y, "Game View did not switch to " + size);
                CheckLayout(size.x + "x" + size.y);
                Check(UI.Q<Button>("ContinueJourney").worldBound.yMax < UI.Q("BottomNav").worldBound.yMin, "Primary lesson action is below the initial viewport");
                Capture("education-" + size.x + "x" + size.y);
                yield return new EditorWaitForSeconds(0.2f);
            }
            Dimensions(1080, 1920); App.ShowTab("education");
            yield return new EditorWaitForSeconds(0.6f);
            Press("ContinueJourney"); yield return new EditorWaitForSeconds(0.3f);
            Check(UI.Q("ModalLayer").resolvedStyle.display == DisplayStyle.Flex, "Primary action did not open lesson preview");
            Capture("lesson-preview"); yield return new EditorWaitForSeconds(0.2f);
            Press("Derse başla"); yield return new EditorWaitForSeconds(0.3f);
            Check(App.CurrentCourseId == "child-0", "Primary action opened the wrong lesson");
            Capture("lesson-info"); yield return new EditorWaitForSeconds(0.2f);
            Press("ContinueLesson"); yield return new EditorWaitForSeconds(0.3f);
            Press("Answer_0"); yield return new EditorWaitForSeconds(0.4f);
            Check(UI.Query<Button>(className: "correct").ToList().Count == 1, "Correct answer lost visual feedback");
            Capture("lesson-feedback"); yield return new EditorWaitForSeconds(0.2f);
            Press("NextExercise");
            while (App.CurrentCourseId != null)
            {
                var course = (LearningCourse)typeof(LearningAppController).GetField("course", Private).GetValue(App);
                int index = (int)typeof(LearningAppController).GetField("exerciseIndex", Private).GetValue(App);
                var exercise = course.exercises[index];
                if (exercise.kind == "info") Press("ContinueLesson");
                else { Press("Answer_" + Array.FindIndex(exercise.choices, c => c.correct)); Press("NextExercise"); }
                yield return new EditorWaitForSeconds(0.2f);
            }
            yield return new EditorWaitForSeconds(0.4f);
            Capture("lesson-reward"); yield return new EditorWaitForSeconds(0.2f);
            Check(App.ActiveProfile.xp == 30 && App.ActiveProfile.completed.Contains("child-0"), "Reward screen changed real progress");
            report.Add("PASS: primary action, preview, lesson, correct answer presentation, completion and 30 XP reward.");
            Press("FinishLesson"); yield return new EditorWaitForSeconds(0.4f);
            Capture("education-progress"); yield return new EditorWaitForSeconds(0.2f);
            foreach (string tab in new[] { "games", "family", "profile" })
            {
                Press("Nav_" + tab); yield return new EditorWaitForSeconds(0.4f);
                CheckLayout(tab); Capture(tab); yield return new EditorWaitForSeconds(0.2f);
            }
            Press("ModeSwitch"); yield return new EditorWaitForSeconds(0.4f);
            Check(App.ActiveProfile.adult, "Adult mode did not change");
            Capture("adult"); yield return new EditorWaitForSeconds(0.2f);
            report.Add("COMPLETE: real Unity Game View captures; isolated verification progress.");
        }
    }
}
