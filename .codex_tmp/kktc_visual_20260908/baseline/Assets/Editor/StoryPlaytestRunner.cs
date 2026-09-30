using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Temp/story_playtest_request.txt bayrağı göründüğünde rebuild sahnelerinin
/// motor içi walkthrough testlerini (StoryRebuildWalkthroughPlayModeTests)
/// Unity Test Runner üzerinden başlatır ve sonuçları
/// Temp/StoryPlayabilityAudit/playtest_results.txt dosyasına yazar. Editör Play
/// Mode'a otomatik girip çıkar; koşu domain reload'ları aştığı için callback'ler
/// her yüklemede yeniden kaydedilir.
/// </summary>
[InitializeOnLoad]
internal static class StoryPlaytestRunner
{
    private const string RequestFlagPath = "Temp/story_playtest_request.txt";
    private const string ResultsPath = "Temp/StoryPlayabilityAudit/playtest_results.txt";
    private const string DonePath = "Temp/StoryPlayabilityAudit/playtest_DONE.txt";
    private const string TestPrefix = "StoryRebuildWalkthroughPlayModeTests.";

    private static readonly TestRunnerApi Api;

    static StoryPlaytestRunner()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new ResultWriter());
        EditorApplication.delayCall += TryRunFromRequestFlag;
    }

    [MenuItem("Tools/Deprem Story/QA/Playability Audit/Run In-Engine Walkthrough Tests")]
    private static void RunFromMenu()
    {
        Execute();
    }

    [MenuItem("Tools/Deprem Story/QA/Playability Audit/Run Story 04 Walkthrough Only")]
    private static void RunStory04FromMenu()
    {
        Execute(TestPrefix + "Story04_Evacuation_PlaysToCompletion");
    }

    private static void TryRunFromRequestFlag()
    {
        string flagFullPath = Path.GetFullPath(RequestFlagPath);
        if (!File.Exists(flagFullPath))
            return;

        // Playability denetimi sahneleri sırayla açarken Play Mode'a girme; denetim
        // bitince walkthrough mevcut authored sahneler üzerinde başlar. QA zinciri
        // artık hiçbir sahneyi otomatik rebuild etmez.
        if (File.Exists(Path.GetFullPath("Temp/story_playability_audit_request.txt")) ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRunFromRequestFlag;
            return;
        }

        File.Delete(flagFullPath);
        Execute();
    }

    private static void Execute(params string[] testNames)
    {
        Directory.CreateDirectory(Path.GetFullPath("Temp/StoryPlayabilityAudit"));
        File.WriteAllText(
            Path.GetFullPath(ResultsPath),
            $"[{DateTime.Now:HH:mm:ss}] PLAYTEST_BEGIN — motor içi walkthrough başlatıldı\n");
        if (File.Exists(Path.GetFullPath(DonePath)))
            File.Delete(Path.GetFullPath(DonePath));

        if (testNames == null || testNames.Length == 0)
        {
            testNames = new[]
            {
                TestPrefix + "Story01_Preparation_PlaysToCompletion",
                TestPrefix + "Story02_HomeSafety_PlaysToCompletion",
                TestPrefix + "Story03_Quake_PlaysToCompletion",
                TestPrefix + "Story04_Evacuation_PlaysToCompletion"
            };
        }

        Filter filter = new Filter
        {
            testMode = TestMode.PlayMode,
            assemblyNames = new[] { "Deprem.Story.PlayModeTests" },
            testNames = testNames
        };
        Api.Execute(new ExecutionSettings(filter));
    }

    private sealed class ResultWriter : ICallbacks
    {
        // Callback'ler editördeki HER test koşusunu duyar (Test Runner penceresi
        // dahil). Yalnız bu koşucunun walkthrough testleri raporlanır; yabancı
        // koşular sonuç dosyasını ve DONE bayrağını kirletmez.
        private bool ownsCurrentRun;

        private static bool IsWalkthrough(ITestAdaptor test)
        {
            return test != null && test.FullName != null && test.FullName.StartsWith(TestPrefix, StringComparison.Ordinal);
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            ownsCurrentRun = ContainsWalkthrough(testsToRun);
        }

        private static bool ContainsWalkthrough(ITestAdaptor test)
        {
            if (test == null)
                return false;
            if (!test.IsSuite)
                return IsWalkthrough(test);
            return test.Children != null && test.Children.Any(ContainsWalkthrough);
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (!ownsCurrentRun)
                return;
            ownsCurrentRun = false;
            Append($"[{DateTime.Now:HH:mm:ss}] PLAYTEST_DONE toplam={result.PassCount + result.FailCount} " +
                   $"geçti={result.PassCount} kaldı={result.FailCount} atlandı={result.SkipCount}");
            File.WriteAllText(Path.GetFullPath(DonePath), DateTime.Now.ToString("O"));
        }

        public void TestStarted(ITestAdaptor test)
        {
            if (!test.IsSuite && IsWalkthrough(test))
                Append($"[{DateTime.Now:HH:mm:ss}] BAŞLADI {test.Name}");
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite || !IsWalkthrough(result.Test))
                return;
            string message = string.IsNullOrEmpty(result.Message)
                ? string.Empty
                : " — " + result.Message.Replace("\n", " | ");
            if (message.Length > 700)
                message = message.Substring(0, 700) + "…";
            Append($"[{DateTime.Now:HH:mm:ss}] {result.TestStatus.ToString().ToUpperInvariant()} " +
                   $"{result.Test.Name} ({result.Duration:F0}sn){message}");
        }

        private static void Append(string line)
        {
            File.AppendAllText(Path.GetFullPath(ResultsPath), line + "\n");
        }
    }
}
