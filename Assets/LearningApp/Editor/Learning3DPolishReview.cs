using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deprem.Learning.Editor
{
    [InitializeOnLoad]
    public static class Learning3DPolishReview
    {
        const string Folder = "ClientExports/DepremApp/Polish3D";
        const string ActiveKey = "Deprem.3DPolish.ReviewActive";
        const string ReloadKey = "Deprem.3DPolish.ReloadLocked";
        const string RestorePendingKey = "Deprem.3DPolish.RestorePending";
        static readonly TestRunnerApi Api;
        static Learning3DPolishReview()
        {
            EditorApplication.delayCall += FinishPendingRestore;
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new Results());
            Application.logMessageReceived += (message, stack, type) =>
            {
                // A disconnected editor MCP client can log while a game test is
                // playing. Expect only that transport error; gameplay logs remain failures.
                if (type == LogType.Error && message.StartsWith("[MCP Unity] WebSocket error:", StringComparison.Ordinal) &&
                    SessionState.GetBool(ActiveKey, false) && SessionState.GetBool(ActiveKey + ".Leaf", false))
                {
                    LogAssert.Expect(type, message);
                    File.AppendAllText(Report + ".txt", "INFRASTRUCTURE: expected editor MCP transport log.\n");
                }
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
                {
                    EditorApplication.LockReloadAssemblies(); SessionState.SetBool(ReloadKey, true);
                }
                if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(ReloadKey, false))
                {
                    EditorApplication.UnlockReloadAssemblies(); SessionState.SetBool(ReloadKey, false);
                }
                if (state == PlayModeStateChange.EnteredEditMode) FinishPendingRestore();
            };
        }
        [MenuItem("Tools/Deprem App/Review/3D Gameplay Verification")]
        public static void Run()
        {
            Begin("gameplay", new[]
            {
                "ThreeDPolishPlayModeTests",
                "StoryEvacuation25DPlayModeTests.FifteenStageRoute_PlaysToAssemblySuccess",
                "MinigamePackagePlayModeTests.AftershockCover_ErrorCorrectionThenEndToEndCompletion",
                "MinigamePackagePlayModeTests.EmergencyBagRush_EndToEndCompletion",
                "MinigamePackagePlayModeTests.EmergencyCorridor_EndToEndCompletion",
                "MinigamePackagePlayModeTests.RoomSafety_EndToEndCompletion",
                "MinigamePackagePlayModeTests.RubbleSignal_EndToEndCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story01_Preparation_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story02_HomeSafety_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story03_Quake_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story04_Evacuation_PlaysToCompletion"
            });
        }
        [MenuItem("Tools/Deprem App/Review/3D Regression Verification")]
        public static void Regressions() => Begin("regressions", new[]
        {
            "ThreeDPolishPlayModeTests",
            "StoryEvacuation25DPlayModeTests.FifteenStageRoute_PlaysToAssemblySuccess",
            "MinigamePackagePlayModeTests.RoomSafety_EndToEndCompletion"
        });
        [MenuItem("Tools/Deprem App/Review/3D Bag Input Verification")]
        public static void BagInput() => Begin("bag-input", new[] { "MinigamePackagePlayModeTests.EmergencyBagRush_EndToEndCompletion" });
        [MenuItem("Tools/Deprem App/Review/3D Preparation Verification")]
        public static void Preparation() => Begin("preparation", new[]
        {
            "MinigamePackagePlayModeTests.EmergencyBagRush_EndToEndCompletion",
            "MinigamePackagePlayModeTests.RoomSafety_EndToEndCompletion",
            "StoryRebuildWalkthroughPlayModeTests.Story01_Preparation_PlaysToCompletion"
        });
        static string Report => Folder + "/" + SessionState.GetString(ActiveKey + ".Report", "gameplay");
        static void FinishPendingRestore()
        {
            if (!SessionState.GetBool(RestorePendingKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Runtime managers can save once more while Play mode exits. Restore only
            // after that teardown, so the test never replaces the player's real save.
            Restore(); SessionState.SetBool(RestorePendingKey, false);
        }
        static void Begin(string reportName, string[] tests)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Leave Play mode and finish compilation first.");
            Directory.CreateDirectory(Folder);
            FinishPendingRestore();
            if (SessionState.GetBool(ActiveKey, false)) Restore();
            Backup();
            SessionState.SetString(ActiveKey + ".Report", reportName);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ActiveKey + ".Leaf", false);
            File.WriteAllText(Report + ".txt", "RUNNING: " + reportName + ".\n");
            Api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = tests
            }));
        }
        static readonly string[] Saves = { "story-session.json", "minigame-profile.json" };
        static void Backup()
        {
            foreach (string name in Saves)
            {
                string source = Path.Combine(Application.persistentDataPath, name);
                string destination = Folder + "/original-" + name;
                SessionState.SetBool(ActiveKey + name, File.Exists(source));
                if (File.Exists(source)) File.Copy(source, destination, true);
            }
            SessionState.SetBool(ActiveKey + ".BackupReady", true);
        }
        [MenuItem("Tools/Deprem App/Review/Restore 3D Gameplay Saves")]
        public static void Restore()
        {
            if (!SessionState.GetBool(ActiveKey + ".BackupReady", false))
                throw new InvalidOperationException("No 3D gameplay save snapshot exists in this editor session.");
            foreach (string name in Saves)
            {
                string destination = Path.Combine(Application.persistentDataPath, name);
                if (SessionState.GetBool(ActiveKey + name, false)) File.Copy(Folder + "/original-" + name, destination, true);
                else if (File.Exists(destination)) File.Delete(destination);
            }
        }
        sealed class Results : ICallbacks
        {
            static bool IsSelected(ITestAdaptor test) => test != null && !test.IsSuite &&
                (test.FullName.StartsWith("ThreeDPolishPlayModeTests.", StringComparison.Ordinal) ||
                 test.FullName.StartsWith("MinigamePackagePlayModeTests.", StringComparison.Ordinal) ||
                 test.FullName.StartsWith("StoryRebuildWalkthroughPlayModeTests.", StringComparison.Ordinal) ||
                 test.FullName.StartsWith("StoryEvacuation25DPlayModeTests.", StringComparison.Ordinal));
            static bool ContainsSelected(ITestResultAdaptor result) => result.HasChildren
                ? result.Children.Any(ContainsSelected) : IsSelected(result.Test);
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test)
            {
                if (SessionState.GetBool(ActiveKey, false) && IsSelected(test))
                {
                    SessionState.SetBool(ActiveKey + ".Leaf", true);
                    File.AppendAllText(Report + ".txt", "START " + test.FullName + "\n");
                }
            }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (SessionState.GetBool(ActiveKey, false) && IsSelected(result.Test))
                {
                    SessionState.SetBool(ActiveKey + ".Leaf", false);
                    File.AppendAllText(Report + ".txt", result.TestStatus + " " + result.FullName + " " + result.Message + "\n");
                }
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(ActiveKey, false) || !ContainsSelected(result)) return;
                TestRunnerApi.SaveResultToFile(result, Report + ".results.xml");
                File.AppendAllText(Report + ".txt", $"COMPLETE pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount}\n");
                SessionState.SetBool(ActiveKey, false);
                SessionState.SetBool(RestorePendingKey, true);
                EditorApplication.delayCall += FinishPendingRestore;
            }
        }
    }
}
