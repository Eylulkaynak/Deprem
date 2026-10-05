using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Learning;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deprem.Learning.Tests
{
    // Requires the separate local verification API on port 8791. Never uses a player's save or server.
    public sealed class LearningFamilyIntegrationTests
    {
        private static string Endpoint => Environment.GetEnvironmentVariable("DEPREM_FAMILY_TEST_URL") ?? "http://127.0.0.1:8791";
        private const string ReportDirectory = "ClientExports/DepremApp/Family";
        private static readonly MethodInfo Click = typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.NonPublic | BindingFlags.Instance);
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name); Assert.That(button.enabledInHierarchy, Is.True, name);
            Click.Invoke(button.clickable, new object[] { null, 0 });
        }
        private static IEnumerator WaitFor(Func<bool> condition, string message, float seconds = 35)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(condition(), Is.True, message);
        }
        private static void Layout(string context)
        {
            var safe = UI.Q("SafeArea").worldBound;
            Assert.That(UI.Q("BottomNav").worldBound.yMax, Is.LessThanOrEqualTo(safe.yMax + 1), context);
            Assert.That(UI.Q<ScrollView>("Body").worldBound.height, Is.GreaterThan(100), context);
            foreach (var element in UI.Q<ScrollView>("Body").Query<VisualElement>().ToList())
            {
                if (element.resolvedStyle.display == DisplayStyle.None || element.worldBound.width == 0) continue;
                Assert.That(element.worldBound.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 1), context + " " + element.name);
                Assert.That(element.worldBound.xMax, Is.LessThanOrEqualTo(safe.xMax + 1), context + " " + element.name);
            }
        }
        private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(ReportDirectory + "/" + name + ".png"));

        [UnityTest, Category("FamilyServiceIntegration")]
        public IEnumerator ChildAndParentPhonesLinkSyncRevokeAndProtectOfflineProgressThroughRealUi()
        {
            // EnterPlayMode reloads the domain. Initialize reference-type locals after it.
            yield return new EnterPlayMode();
            Directory.CreateDirectory(ReportDirectory);
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var sizeProperty = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var view = EditorWindow.GetWindow(viewType);
            int priorSize = (int)sizeProperty.GetValue(view);
            string temporary = Path.GetFullPath(".codex_tmp/family-verification/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            string previousSave = LearningProgress.Current.SavePath;
            GameObject parentPhone = null, offlinePhone = null;
            try
            {
                LearningFamilyConfig.EditorEndpointOverride = Endpoint;
                LearningProgress.UseVerificationStore(Path.Combine(temporary, "child.json"));
                SceneManager.LoadScene(LearningGameBridge.SceneName);
                yield return new WaitForSecondsRealtime(.7f);
                typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { 1080, 1920, "Family Review 1080x1920", false });
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(App, Is.Not.Null);
                App.ActiveProfile.name = "Ada";
                App.ActiveProfile.CompleteCourse("child-0", DateTime.Now);
                App.ActiveProfile.CompleteGame("child:bag-packing", 3, 900, DateTime.Now);
                App.ActiveProfile.Plan("meetingPoint").value = "PRIVATE-TEST-ADDRESS";
                var question = App.Catalog.Courses(false)[0].exercises.First(e => e.kind != "info").prompt;
                App.ActiveProfile.RecordMistake(question, "private-answer", "private-correct");
                LearningProgress.Current.Save();
                App.ShowTab("profile"); Press("Veli bağlantısı");
                Assert.That(UI.Q<Button>("ChildCreateCode").enabledInHierarchy, Is.False, "Sharing needs explicit consent");
                UI.Q<Toggle>("ChildShareConsent").value = true; Press("ChildCreateCode");
                yield return WaitFor(() => !App.FamilyService.Busy, "Child code creation timed out");
                Assert.That(UI.Q<Label>("ChildPairCode"), Is.Not.Null, UI.Q<Label>("ChildConnectionError")?.text);
                string code = UI.Q<Label>("ChildPairCode").text.Replace(" ", "");
                Assert.That(code.Length, Is.EqualTo(8));
                yield return new WaitForSecondsRealtime(.3f); Layout("child connection"); Capture("child-connection");
                yield return new WaitForSecondsRealtime(.2f);

                parentPhone = new GameObject("Family verification parent phone");
                var parentService = parentPhone.AddComponent<LearningFamilyService>();
                parentService.Initialize(new LearningProgress(Path.Combine(temporary, "parent.json")), App.Catalog);
                string email = "review-" + Guid.NewGuid().ToString("N") + "@example.test";
                bool completed = false; string error = null;
                parentService.Authenticate(email, "Birlikte2026!", "Derya", true, message => { error = message; completed = true; });
                yield return WaitFor(() => completed && !parentService.Busy, "Parent account request timed out");
                Assert.That(error, Is.Null); Assert.That(parentService.Children, Is.Empty);
                completed = false;
                parentService.LinkChild(code, message => { error = message; completed = true; });
                yield return WaitFor(() => completed && !parentService.Busy, "Two phones did not link");
                Assert.That(error, Is.Null); Assert.That(parentService.Children.Length, Is.EqualTo(1));
                string childId = parentService.Children[0].id;
                Assert.That(parentService.Children[0].progress.xp, Is.EqualTo(50));
                Assert.That(JsonUtility.ToJson(parentService.Children[0]), Does.Not.Contain("PRIVATE-TEST-ADDRESS").And.Not.Contain("private-answer"));
                completed = false;
                parentService.LinkChild(code, message => { error = message; completed = true; });
                yield return WaitFor(() => completed && !parentService.Busy, "Reused code request timed out");
                Assert.That(error, Is.Not.Null, "A consumed code must not be reusable");

                App.ActiveProfile.adult = true; App.ShowTab("profile"); Press("Veli bağlantısı");
                UI.Q<TextField>("ParentEmail").value = email; UI.Q<TextField>("ParentPassword").value = "Birlikte2026!";
                Press("ParentSubmit"); yield return WaitFor(() => !App.FamilyService.Busy, "Parent UI login timed out");
                Assert.That(UI.Q("ParentChild_" + childId), Is.Not.Null, UI.Q<Label>("ParentAuthError")?.text);
                yield return new WaitForSecondsRealtime(.3f); Layout("parent dashboard"); Capture("parent-dashboard");
                yield return new WaitForSecondsRealtime(.2f);
                foreach (var size in new[] { new Vector2Int(750, 1334), new Vector2Int(1080, 2340) })
                {
                    typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { size.x, size.y, "Family Review " + size.x + "x" + size.y, false });
                    yield return new WaitForSecondsRealtime(.4f);
                    Layout("parent dashboard " + size); Capture("parent-dashboard-" + size.x + "x" + size.y);
                    yield return new WaitForSecondsRealtime(.2f);
                }
                typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { 1080, 1920, "Family Review 1080x1920", false });
                yield return new WaitForSecondsRealtime(.3f);
                Press("ParentReport_" + childId); yield return new WaitForSecondsRealtime(.3f); Layout("child report"); Capture("child-progress");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(UI.Q<Button>("ParentUnlinkChild"), Is.Not.Null);

                App.ActiveProfile.CompleteCourse("child-1", DateTime.Now); LearningProgress.Current.Save(); App.FamilyService.MarkDirty(App.ActiveProfile.id);
                yield return WaitFor(() => {
                    var device = App.FamilyService.Device(App.ActiveProfile.id);
                    return device.uploadedJson != null && device.uploadedJson.Contains("child-1");
                }, "Background sync did not upload a new lesson after changing pages");
                completed = false; parentService.RefreshChildren(message => { error = message; completed = true; });
                yield return WaitFor(() => completed && !parentService.Busy, "Progress refresh timed out");
                Assert.That(error, Is.Null); Assert.That(parentService.Children[0].progress.completed, Does.Contain("child-1"));
                Assert.That(parentService.Children[0].progress.xp, Is.EqualTo(80));
                App.FamilyService.LockParent(); Assert.That(UI.Q<TextField>("ParentPassword"), Is.Not.Null, "Background locking should remove the parent report");

                // A real unreachable HTTP endpoint exercises loss of connectivity without touching the running API.
                LearningFamilyConfig.EditorEndpointOverride = "http://127.0.0.1:9";
                offlinePhone = new GameObject("Family verification offline phone");
                var offlineStore = new LearningProgress(Path.Combine(temporary, "offline.json"));
                offlineStore.Data.Active.CompleteCourse("child-0", DateTime.Now); offlineStore.Save();
                var offlineService = offlinePhone.AddComponent<LearningFamilyService>(); offlineService.Initialize(offlineStore, App.Catalog);
                completed = false;
                offlineService.CreateCode(offlineStore.Data.Active.id, (pair, message) => { error = message; completed = true; });
                yield return WaitFor(() => completed && !offlineService.Busy, "Offline request did not finish");
                Assert.That(error, Is.Not.Null);
                Assert.That(new LearningProgress(offlineStore.SavePath).Data.Active.xp, Is.EqualTo(30));
                completed = false; offlineService.StopSharing(offlineStore.Data.Active.id, message => { completed = true; });
                yield return WaitFor(() => completed && !offlineService.Busy, "Offline revocation did not finish");
                var persisted = new FamilyDeviceStore(offlineStore.SavePath + ".family.json", "http://127.0.0.1:9").ForProfile(offlineStore.Data.Active.id);
                Assert.That(persisted.enabled, Is.False); Assert.That(persisted.pendingStop, Is.True);
                offlineService.Shutdown(); LearningFamilyConfig.EditorEndpointOverride = Endpoint;

                App.ActiveProfile.adult = false; App.ShowTab("profile"); Press("Veli bağlantısı");
                yield return WaitFor(() => !App.FamilyService.Busy, "Guardian list did not load");
                Assert.That(UI.Q("LinkedGuardians").Query<Button>().ToList().Count, Is.EqualTo(1));
                Press("ChildStopSharing"); Press("Paylaşımı kapat");
                yield return WaitFor(() => !App.FamilyService.Busy, "Sharing revocation timed out");
                Assert.That(App.FamilyService.Device(App.ActiveProfile.id).enabled, Is.False);
                completed = false; parentService.RefreshChildren(message => completed = true);
                yield return WaitFor(() => completed && !parentService.Busy, "Revoked parent did not refresh");
                Assert.That(parentService.Children, Is.Empty);
                Assert.That(App.ActiveProfile.completed, Does.Contain("child-0").And.Contain("child-1"));
                Assert.That(App.ActiveProfile.TotalStars, Is.EqualTo(3));
                File.WriteAllText(ReportDirectory + "/runtime-verification.txt", "PASS: real Unity UI consent, expiring one-use code, two independent phone clients, parent login/dashboard/report, private-data projection, background progress sync after page changes, parent locking, unreachable-network learning persistence, queued offline revocation, live access revocation and preserved local achievements.\nPASS: 1080x1920, 750x1334 and 1080x2340 portrait UI layout.\n");
            }
            finally
            {
                App?.FamilyService?.Shutdown();
                parentPhone?.GetComponent<LearningFamilyService>()?.Shutdown(); offlinePhone?.GetComponent<LearningFamilyService>()?.Shutdown();
                if (parentPhone != null) UnityEngine.Object.Destroy(parentPhone);
                if (offlinePhone != null) UnityEngine.Object.Destroy(offlinePhone);
                LearningFamilyConfig.EditorEndpointOverride = null;
                LearningProgress.UseVerificationStore(previousSave);
                var restoredViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                restoredViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(EditorWindow.GetWindow(restoredViewType), priorSize);
            }
            yield return new ExitPlayMode();
        }
    }
}
