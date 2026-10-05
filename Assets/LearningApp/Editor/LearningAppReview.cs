using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deprem.Learning.Editor
{
    public static class LearningAppReview
    {
        private const string Folder = ".codex_tmp/learning-app";
        [MenuItem("Tools/Deprem App/Review/Play App")]
        public static void Play()
        {
            if (EditorApplication.isPlaying) return;
            LearningAppBuilder.OpenScene(); EditorApplication.isPlaying = true;
        }
        [MenuItem("Tools/Deprem App/Review/Stop")]
        public static void Stop() { EditorApplication.isPlaying = false; }
        [MenuItem("Tools/Deprem App/Review/Capture and Inspect")]
        public static void Capture()
        {
            Directory.CreateDirectory(Folder);
            ScreenCapture.CaptureScreenshot(Folder + "/screen.png");
            var app = UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
            var lines = new List<string> { "Scene=" + SceneManager.GetActiveScene().name, "Playing=" + EditorApplication.isPlaying };
            if (app != null)
            {
                lines.Add("Tab=" + app.CurrentTab);
                var styles = app.GetComponent<UIDocument>().rootVisualElement.styleSheets;
                lines.Add("Styles=" + styles.count + " | Resource=" + Resources.Load<StyleSheet>("LearningApp/LearningStyles"));
                Walk(app.GetComponent<UIDocument>().rootVisualElement, lines, 0);
            }
            File.WriteAllLines(Folder + "/ui.txt", lines);
            Debug.Log("[Deprem App] Review saved: " + Path.GetFullPath(Folder));
        }
        private static void Walk(VisualElement element, List<string> lines, int depth)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;
            string text = element is TextElement label ? label.text : "";
            if (text.Length > 80) text = text.Substring(0, 80);
            if (element is Button || element is Label || element.name == "SafeArea" || element.name == "App" || element.name == "Body")
                lines.Add(new string(' ', depth) + element.GetType().Name + " " + element.name + " | " + element.worldBound + " | " + text);
            foreach (var child in element.Children()) Walk(child, lines, depth + 1);
        }
        // Editor-only review actions use the real button's registered clicked callback.
        [MenuItem("Tools/Deprem App/Review/Click Requested Button")]
        public static void ClickRequested()
        {
            string name = File.ReadAllText(Folder + "/button.txt").Trim();
            var app = UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
            var button = app.GetComponent<UIDocument>().rootVisualElement.Q<Button>(name);
            if (button == null || !button.enabledInHierarchy) throw new InvalidOperationException("Button missing or disabled: " + name);
            var method = typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException("UI Toolkit Clickable.SimulateSingleClick");
            method.Invoke(button.clickable, new object[] { null, 0 });
        }
        [MenuItem("Tools/Deprem App/Review/Return to App")]
        public static void Return() => LearningGameBridge.Instance.ReturnToLearning();
    }
}
