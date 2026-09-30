using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
internal static class StoryVideoCaptureRunner
{
    private const string MontageTestName = "StoryVideoCapturePlayModeTests.RecordNormalSpeedMontageClips";
    private const string FullWalkthroughTestName =
        "StoryRebuildWalkthroughPlayModeTests.RecordAllFourScenesAtNormalSpeedWithRecorder";
    private const string MontageResultPath = "Temp/StoryVideoCapture/capture_TEST_RESULT.txt";
    private const string FullWalkthroughResultPath = "Recordings/RebuildFull/latest_TEST_RESULT.txt";
    private static readonly TestRunnerApi Api;

    static StoryVideoCaptureRunner()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new CaptureResultWriter());
    }

    [MenuItem("Tools/Deprem Story/Video/Capture Normal Speed Montage Clips")]
    private static void Capture()
    {
        ExecuteCaptureTest(MontageTestName, MontageResultPath);
    }

    [MenuItem("Tools/Deprem Story/Video/Record Full Rebuild Walkthrough (Normal Speed + Audio)")]
    private static void RecordFullWalkthrough()
    {
        ExecuteCaptureTest(FullWalkthroughTestName, FullWalkthroughResultPath);
    }

    private static void ExecuteCaptureTest(string testName, string resultPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(resultPath)) ?? "Temp");
        File.WriteAllText(
            Path.GetFullPath(resultPath),
            $"[{DateTime.Now:O}] CAPTURE_TEST_BEGIN test={testName}{Environment.NewLine}");

        Filter filter = new Filter
        {
            testMode = TestMode.PlayMode,
            assemblyNames = new[] { "Deprem.Story.PlayModeTests" },
            testNames = new[] { testName }
        };
        Api.Execute(new ExecutionSettings(filter));
    }

    private sealed class CaptureResultWriter : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string resultPath = ResultPathForContainedTest(result);
            if (resultPath == null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(resultPath)) ?? "Temp");
            File.AppendAllText(
                Path.GetFullPath(resultPath),
                $"[{DateTime.Now:O}] CAPTURE_TEST_DONE passed={result.PassCount} " +
                $"failed={result.FailCount} skipped={result.SkipCount}{Environment.NewLine}");
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            string resultPath = ResultPathForTest(result?.Test?.FullName);
            if (resultPath == null)
                return;

            string message = string.IsNullOrWhiteSpace(result.Message)
                ? string.Empty
                : " — " + result.Message.Replace('\n', ' ').Replace('\r', ' ');
            File.AppendAllText(
                Path.GetFullPath(resultPath),
                $"[{DateTime.Now:O}] {result.TestStatus.ToString().ToUpperInvariant()}" + message +
                Environment.NewLine);
        }

        private static string ResultPathForContainedTest(ITestResultAdaptor result)
        {
            if (result == null)
                return null;
            if (!result.Test.IsSuite)
                return ResultPathForTest(result.Test.FullName);

            if (result.Children == null)
                return null;
            foreach (ITestResultAdaptor child in result.Children)
            {
                string path = ResultPathForContainedTest(child);
                if (path != null)
                    return path;
            }
            return null;
        }

        private static string ResultPathForTest(string testName)
        {
            if (testName == MontageTestName)
                return MontageResultPath;
            if (testName == FullWalkthroughTestName)
                return FullWalkthroughResultPath;
            return null;
        }
    }
}
