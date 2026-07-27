using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
internal static class StoryPersistentTestResultReporter
{
    private const string ReportRoot = "Temp/StoryTestQA";
    private static readonly ResultCallbacks Callbacks = new ResultCallbacks();

    static StoryPersistentTestResultReporter()
    {
        TestRunnerApi.RegisterTestCallback(Callbacks, 250);
    }

    private sealed class ResultCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string root = Path.GetFullPath(ReportRoot);
            Directory.CreateDirectory(root);
            string summary =
                $"COMPLETED pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount} " +
                $"inconclusive={result.InconclusiveCount} duration={result.Duration:F3}";
            string mode = ModeName(result.Test.TestMode);
            File.WriteAllText(Path.Combine(root, mode + ".status.txt"), summary);
            TestRunnerApi.SaveResultToFile(
                result,
                Path.Combine(root, mode + ".results.xml"));
            if (result.FailCount == 0)
                Debug.Log("STORY_TEST_REPORT " + summary);
            else
                Debug.LogError("STORY_TEST_REPORT " + summary);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren)
                return;

            string root = Path.GetFullPath(ReportRoot);
            Directory.CreateDirectory(root);
            string line =
                $"{result.TestStatus}\t{result.Duration:F3}\t{result.FullName}\t{result.Message}" +
                Environment.NewLine;
            File.AppendAllText(
                Path.Combine(root, ModeName(result.Test.TestMode) + ".tests.txt"),
                line);
        }

        private static string ModeName(TestMode mode)
        {
            if ((mode & TestMode.PlayMode) != 0)
                return "PlayMode";
            if ((mode & TestMode.EditMode) != 0)
                return "EditMode";
            return "Tests";
        }
    }
}
