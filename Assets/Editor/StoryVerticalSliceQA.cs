using System;
using Deprem.Story;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

internal static class StoryVerticalSliceQA
{
    private const string PreviewCompletionMenu = "Tools/Deprem Story/QA/Preview Completion UI";
    private const string RunEditModeTestsMenu = "Tools/Deprem Story/QA/Run Story EditMode Tests";
    private const string RunPlayModeTestsMenu = "Tools/Deprem Story/QA/Run Story PlayMode Tests";
    private const string PreviewTableCameraMenu = "Tools/Deprem Story/QA/Preview Camera/Table Shelter";
    private const string PreviewWindowCameraMenu = "Tools/Deprem Story/QA/Preview Camera/Window Danger";
    private const string PreviewGlassCameraMenu = "Tools/Deprem Story/QA/Preview Camera/Broken Glass";

    private static TestRunnerApi testRunner;
    private static StoryTestCallbacks testCallbacks;
    private static bool testsRunning;

    [MenuItem(PreviewCompletionMenu)]
    private static void PreviewCompletionUi()
    {
        StoryUIController storyUi = UnityEngine.Object.FindFirstObjectByType<StoryUIController>();
        if (storyUi == null)
        {
            Debug.LogWarning("Completion UI preview requires Story_03_Quake to be running in Play Mode.");
            return;
        }

        storyUi.ShowCompletion();
    }

    [MenuItem(PreviewCompletionMenu, true)]
    private static bool CanPreviewCompletionUi()
    {
        return Application.isPlaying && UnityEngine.Object.FindFirstObjectByType<StoryUIController>() != null;
    }

    [MenuItem(PreviewTableCameraMenu)]
    private static void PreviewTableCamera() => PreviewCamera(StoryCameraZoneId.InspectTable);

    [MenuItem(PreviewWindowCameraMenu)]
    private static void PreviewWindowCamera() => PreviewCamera(StoryCameraZoneId.InspectWindow);

    [MenuItem(PreviewGlassCameraMenu)]
    private static void PreviewGlassCamera() => PreviewCamera(StoryCameraZoneId.InspectBrokenGlass);

    [MenuItem(PreviewTableCameraMenu, true)]
    [MenuItem(PreviewWindowCameraMenu, true)]
    [MenuItem(PreviewGlassCameraMenu, true)]
    private static bool CanPreviewCamera()
    {
        return Application.isPlaying && UnityEngine.Object.FindFirstObjectByType<StoryCameraController>() != null;
    }

    private static void PreviewCamera(StoryCameraZoneId zone)
    {
        StoryCameraController controller = UnityEngine.Object.FindFirstObjectByType<StoryCameraController>();
        if (controller == null)
            return;
        GameObject brokenGlass = Array.Find(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            candidate => candidate.name == "BrokenGlass_Hazard")?.gameObject;
        if (zone == StoryCameraZoneId.InspectBrokenGlass && brokenGlass != null)
            brokenGlass.SetActive(true);
        controller.ActivateZone(zone, true);
    }

    [MenuItem(RunEditModeTestsMenu)]
    private static void RunEditModeTests()
    {
        RunTests(TestMode.EditMode, "Assembly-CSharp-Editor", "EditMode");
    }

    [MenuItem(RunPlayModeTestsMenu)]
    private static void RunPlayModeTests()
    {
        RunTests(TestMode.PlayMode, "Deprem.Story.PlayModeTests", "PlayMode");
    }

    [MenuItem(RunEditModeTestsMenu, true)]
    [MenuItem(RunPlayModeTestsMenu, true)]
    private static bool CanRunTests()
    {
        return !testsRunning && !EditorApplication.isCompiling;
    }

    private static void RunTests(TestMode mode, string assemblyName, string label)
    {
        if (testsRunning)
        {
            Debug.LogWarning("Story QA tests are already running.");
            return;
        }

        testsRunning = true;
        testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
        testCallbacks = new StoryTestCallbacks(label, () =>
        {
            testsRunning = false;
            if (testRunner != null && testCallbacks != null)
                testRunner.UnregisterCallbacks(testCallbacks);
        });
        testRunner.RegisterCallbacks(testCallbacks);
        Filter filter = new Filter
        {
            testMode = mode,
            assemblyNames = new[] { assemblyName }
        };
        Debug.Log($"Story QA {label} tests started for {assemblyName}.");
        testRunner.Execute(new ExecutionSettings(filter));
    }

    private sealed class StoryTestCallbacks : ICallbacks
    {
        private readonly string label;
        private readonly Action completed;

        public StoryTestCallbacks(string label, Action completed)
        {
            this.label = label;
            this.completed = completed;
        }

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string summary = $"Story QA {label}: {result.PassCount} passed, {result.FailCount} failed, " +
                             $"{result.SkipCount} skipped, {result.InconclusiveCount} inconclusive.";
            if (result.FailCount > 0)
                Debug.LogError(summary);
            else
                Debug.Log(summary);
            completed?.Invoke();
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }
    }
}
