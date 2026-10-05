using System;
using System.IO;
using System.Linq;
using Deprem.Learning;
using NUnit.Framework;
using UnityEngine;

namespace Deprem.Learning.Tests
{
    public sealed class LearningFamilyTests
    {
        private string directory;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "DepremFamilyTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void SharedSnapshotProjectsOnlyKnownChildAchievementsAndTopics()
        {
            var catalog = LearningCatalog.Load(); var profile = new LearningProfile { name = "Ada", adult = true };
            profile.CompleteCourse("child-0", DateTime.Now); profile.CompleteCourse("adult-0", DateTime.Now);
            profile.CompleteGame("child:bag-packing", 3, 900, DateTime.Now); profile.CompleteGame("adult:bag-packing", 2, 850, DateTime.Now);
            profile.Plan("meetingPoint").value = "PRIVATE-ADDRESS";
            var exercise = catalog.Courses(false)[0].exercises.First(e => e.kind != "info");
            profile.RecordMistake(exercise.prompt, "PRIVATE-ANSWER", "PRIVATE-CORRECT"); profile.resumeCourse = "adult-0";
            var snapshot = FamilySnapshot.FromProfile(profile, catalog);
            Assert.That(snapshot.completed, Is.EqualTo(new[] { "child-0" }));
            Assert.That(snapshot.results.Select(r => r.id), Is.EqualTo(new[] { "child:bag-packing" }));
            Assert.That(snapshot.reviewTopics.Single().courseId, Is.EqualTo("child-0"));
            Assert.That(snapshot.xp, Is.EqualTo(50)); Assert.That(snapshot.resumeCourse, Is.Empty);
            string json = JsonUtility.ToJson(snapshot);
            foreach (string secret in new[] { "PRIVATE-ADDRESS", "PRIVATE-ANSWER", "PRIVATE-CORRECT", "adult-0", "meetingPoint" }) Assert.That(json, Does.Not.Contain(secret));
        }
        [Test] public void DeviceCredentialsSurviveRestartAndRemainPerProfileAndService()
        {
            string path = Path.Combine(directory, "devices.json");
            var store = new FamilyDeviceStore(path, "https://family.example");
            var ada = store.Add("ada"); ada.token = FamilyDeviceStore.NewDeviceToken(); ada.enabled = true;
            var can = store.Add("can"); can.token = FamilyDeviceStore.NewDeviceToken();
            Assert.That(ada.token, Is.Not.EqualTo(can.token)); Assert.That(store.Save(), Is.True);
            var loaded = new FamilyDeviceStore(path, "https://family.example");
            Assert.That(loaded.ForProfile("ada").enabled, Is.True); Assert.That(loaded.ForProfile("can").enabled, Is.False);
            Assert.That(loaded.ForProfile("ada").token, Is.EqualTo(ada.token));
            Assert.That(new FamilyDeviceStore(path, "https://different.example").Data.children, Is.Empty);
            Assert.That(File.ReadAllText(path), Does.Not.Contain("password").And.Not.Contain("email").And.Not.Contain("parent"));
        }
        [Test] public void CorruptConnectionStateNeverRestoresAnEnabledBackup()
        {
            string path = Path.Combine(directory, "devices.json"); var store = new FamilyDeviceStore(path, "https://family.example");
            var child = store.Add("ada"); child.token = FamilyDeviceStore.NewDeviceToken(); child.enabled = true; store.Save();
            File.Copy(path, path + ".bak"); File.WriteAllText(path, "{broken");
            var restored = new FamilyDeviceStore(path, "https://family.example");
            Assert.That(restored.Data.children, Is.Empty); Assert.That(restored.LastError, Is.Not.Empty);
        }
        [Test] public void OfflineRevocationIsPersistedWithoutChangingLearningProgress()
        {
            string progressPath = Path.Combine(directory, "learning.json"); var learning = new LearningProgress(progressPath);
            learning.Data.Active.CompleteCourse("child-0", DateTime.Now); learning.Save();
            var family = new FamilyDeviceStore(progressPath + ".family.json", "https://family.example");
            var device = family.Add(learning.Data.Active.id); device.token = FamilyDeviceStore.NewDeviceToken(); device.enabled = true; family.Save();
            device.enabled = false; device.pendingStop = true; family.Save();
            var restored = new FamilyDeviceStore(progressPath + ".family.json", "https://family.example").ForProfile(device.profileId);
            Assert.That(restored.enabled, Is.False); Assert.That(restored.pendingStop, Is.True);
            Assert.That(new LearningProgress(progressPath).Data.Active.completed, Does.Contain("child-0"));
            Assert.That(new LearningProgress(progressPath).Data.Active.xp, Is.EqualTo(30));
        }
        [TestCase("http://family.example", false, "")]
        [TestCase("http://127.0.0.1:8787", false, "")]
        [TestCase("http://127.0.0.1:8787", true, "http://127.0.0.1:8787")]
        [TestCase("https://family.example/", false, "https://family.example")]
        [TestCase("https://name:secret@family.example", false, "")]
        [TestCase("https://family.example/?token=secret", false, "")]
        [TestCase("https://family.example/#secret", false, "")]
        [TestCase("", false, "")]
        public void MobileEndpointsRequireHttpsAndNeverEmbedSecrets(string input, bool local, string expected)
        { Assert.That(LearningFamilyConfig.NormalizeEndpoint(input, local), Is.EqualTo(expected)); }
        [Test] public void ExistingLearningSavesDoNotAutomaticallyEnableSharing()
        {
            string path = Path.Combine(directory, "old-learning.json");
            File.WriteAllText(path, "{\"version\":1,\"profiles\":[{\"name\":\"Ada\",\"xp\":90,\"completed\":[\"child-0\"]}]}");
            var learning = new LearningProgress(path);
            var family = new FamilyDeviceStore(path + ".family.json", "https://family.example");
            Assert.That(learning.Data.Active.xp, Is.EqualTo(90)); Assert.That(family.Data.children, Is.Empty);
            Assert.That(Resources.Load<UnityEngine.UIElements.StyleSheet>("LearningApp/FamilyStyles"), Is.Not.Null);
        }
    }
}
