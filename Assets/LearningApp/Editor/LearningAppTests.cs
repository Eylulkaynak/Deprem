using System;
using System.IO;
using System.Linq;
using Deprem.Learning;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deprem.Learning.Tests
{
    public sealed class LearningAppTests
    {
        private string directory;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "DepremLearningTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void AllOriginalCoursesAndGameModesWereImported()
        {
            var data = LearningCatalog.Load();
            Assert.That(Resources.Load<UnityEngine.UIElements.StyleSheet>("LearningApp/LearningStyles"), Is.Not.Null);
            Assert.That(data.Courses(false).Length, Is.EqualTo(21)); Assert.That(data.Courses(true).Length, Is.EqualTo(28));
            Assert.That(data.Levels(false).Length, Is.EqualTo(8)); Assert.That(data.Levels(true).Length, Is.EqualTo(8));
            Assert.That(data.guides.Length, Is.EqualTo(16)); Assert.That(data.badges.Length, Is.EqualTo(26));
            foreach (var c in data.courses)
            {
                Assert.That(c.exercises, Is.Not.Empty, c.id);
                foreach (var e in c.exercises.Where(e => e.kind != "info")) Assert.That(e.choices.Count(a => a.correct), Is.EqualTo(1), c.id + ": " + e.prompt);
            }
        }
        [Test] public void AllImportedArtAndGameItemReferencesResolve()
        {
            var data = LearningCatalog.Load();
            foreach (var art in data.art) Assert.That(Resources.Load<Texture2D>(art.resource), Is.Not.Null, art.key);
            foreach (var level in data.levels)
                foreach (string id in (level.items ?? Array.Empty<string>()).Concat(level.goodItems ?? Array.Empty<string>()).Concat(level.badItems ?? Array.Empty<string>()).Concat(level.memoryPads ?? Array.Empty<string>()))
                    Assert.That(data.Item(id), Is.Not.Null, level.id + ": " + id);
        }
        [Test] public void NativeAppStartsBeforeEvery3DScene()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            Assert.That(enabled[0].path, Is.EqualTo(Editor.LearningAppBuilder.ScenePath));
            foreach (string route in new[] { "adventure", "story", "minigames" })
                Assert.That(enabled.Any(s => Path.GetFileNameWithoutExtension(s.path) == LearningGameBridge.RouteScene(route)), Is.True, route);
            Assert.That(LearningGameBridge.RouteScene("../../arbitrary"), Is.Null);
        }
        [Test] public void CourseReplayDoesNotFarmXpOrDuplicateCompletion()
        {
            var p = new LearningProfile(); var now = new DateTime(2026, 10, 1);
            Assert.That(p.CompleteCourse("child-0", now), Is.True); Assert.That(p.CompleteCourse("child-0", now), Is.False);
            Assert.That(p.xp, Is.EqualTo(30)); Assert.That(p.completed.Count, Is.EqualTo(1)); Assert.That(p.streak, Is.EqualTo(1));
        }
        [Test] public void StreakHandlesSameDayConsecutiveDayAndMissedDay()
        {
            var p = new LearningProfile(); var start = new DateTime(2026, 9, 29);
            p.RecordActivity(start); p.RecordActivity(start); p.RecordActivity(start.AddDays(1));
            Assert.That(p.streak, Is.EqualTo(2)); Assert.That(p.DisplayStreak(start.AddDays(3)), Is.EqualTo(0));
            p.RecordActivity(start.AddDays(3)); Assert.That(p.streak, Is.EqualTo(1)); Assert.That(p.longestStreak, Is.EqualTo(2));
        }
        [Test] public void ModeResultsStaySeparateAndBestScoreNeverRegresses()
        {
            var p = new LearningProfile(); p.CompleteGame("child:bag-packing", 3, 1000, DateTime.Now);
            p.CompleteGame("child:bag-packing", 1, 100, DateTime.Now); p.CompleteGame("adult:bag-packing", 2, 850, DateTime.Now);
            Assert.That(p.results.Count, Is.EqualTo(2)); Assert.That(p.results[0].stars, Is.EqualTo(3)); Assert.That(p.results[0].score, Is.EqualTo(1000)); Assert.That(p.xp, Is.EqualTo(40));
        }
        [Test] public void RestartRestoresProfilePlanLessonCheckpointAndMistakes()
        {
            string path = Path.Combine(directory, "save.json"); var store = new LearningProgress(path);
            store.AddProfile("İdil"); var p = store.Data.Active;
            p.adult = true; p.Plan("meetingPoint").value = "Okul bahçesi"; p.Plan("bag:check").done = true;
            p.resumeCourse = "adult-3"; p.resumeExercise = 1; p.resumeMistakes = 1;
            p.RecordMistake("Soru?", "Yanlış", "Doğru"); Assert.That(store.Save(), Is.True);
            var restored = new LearningProgress(path).Data.Active;
            Assert.That(restored.name, Is.EqualTo("İdil")); Assert.That(restored.adult, Is.True);
            Assert.That(restored.Plan("meetingPoint").value, Is.EqualTo("Okul bahçesi")); Assert.That(restored.Plan("bag:check").done, Is.True);
            Assert.That(restored.resumeCourse, Is.EqualTo("adult-3")); Assert.That(restored.resumeMistakes, Is.EqualTo(1)); Assert.That(restored.mistakes[0].correct, Is.EqualTo("Doğru"));
            Assert.That(new LearningProgress(path).Data.profiles[0].plan.Count, Is.EqualTo(0));
        }
        [Test] public void DamagedPrimaryRecoversPreviousValidGeneration()
        {
            string path = Path.Combine(directory, "save.json"); var store = new LearningProgress(path);
            store.Data.Active.name = "Ada"; store.Save(); store.Data.Active.xp = 30; store.Save();
            File.WriteAllText(path, "{broken"); var restored = new LearningProgress(path);
            Assert.That(restored.Data.Active.name, Is.EqualTo("Ada")); Assert.That(restored.Save(), Is.True);
            Assert.That(File.Exists(path + ".corrupt"), Is.True);
            Assert.That(new LearningProgress(path).Data.Active.name, Is.EqualTo("Ada"));
        }
        [Test] public void EmptyProfileNameIsRejectedWithoutChangingActiveProfile()
        {
            var store = new LearningProgress(Path.Combine(directory, "save.json"));
            Assert.Throws<ArgumentException>(() => store.AddProfile("   "));
            Assert.That(store.Data.profiles.Count, Is.EqualTo(1));
        }
        [Test] public void RandomizedRoundsUseOriginalPoolsWithoutMutatingCatalog()
        {
            var data = LearningCatalog.Load();
            foreach (var level in data.levels)
            {
                string original = JsonUtility.ToJson(level); var round = level.CreateRound();
                Assert.That(JsonUtility.ToJson(level), Is.EqualTo(original));
                if (level.type == "bag") { Assert.That(round.correctItems.Length, Is.EqualTo(level.correctCount)); Assert.That(round.correctItems.All(i => level.correctPool.Contains(i)), Is.True); }
                if (level.type == "sort") Assert.That(round.packItems.Length, Is.EqualTo(level.packCount));
                if (level.type == "match") Assert.That(round.targets.Length, Is.EqualTo(level.matchCount));
                if (level.type == "quiz") Assert.That(round.questions.Length, Is.EqualTo(level.questionCount > 0 ? Math.Min(level.questionCount, level.questions.Length) : level.questions.Length));
            }
        }

        [Test] public void NarrationCoversEveryLessonChoiceFeedbackAndGuideWithPlayableAudio()
        {
            var voices = LearningNarrationCatalog.Load();
            Assert.That(voices, Is.Not.Null);
            Assert.That(voices.voice, Does.StartWith("Nisa - Encouraging, Friendly and Soft"));
            Assert.That(voices.synthetic, Is.True);
            var entries = voices.entries.ToDictionary(e => e.text, StringComparer.Ordinal);
            void Covered(string text) { if (!string.IsNullOrWhiteSpace(text)) Assert.That(entries.ContainsKey(text), Is.True, text); }
            var catalog = LearningCatalog.Load();
            foreach (var course in catalog.courses)
                foreach (var exercise in course.exercises)
                {
                    foreach (string text in new[] { exercise.title, exercise.text, exercise.prompt, exercise.tip, exercise.success }) Covered(text);
                    foreach (var choice in exercise.choices) { Covered(choice.label); Covered(choice.feedback); }
                }
            foreach (var level in catalog.levels)
            {
                Covered(level.instruction);
                foreach (var question in level.questions ?? Array.Empty<LearningExercise>())
                { Covered(question.prompt); foreach (var choice in question.choices) Covered(choice.label); }
            }
            foreach (var guide in catalog.guides)
            { Covered(guide.title); Covered(guide.summary); foreach (string point in guide.points) Covered(point); }
            Covered("Dışarıda güvenli yer hangisi?");
            foreach (var entry in voices.entries.GroupBy(e => e.resource).Select(g => g.First()))
            {
                var clip = Resources.Load<AudioClip>(entry.resource);
                Assert.That(clip, Is.Not.Null, entry.resource);
                Assert.That(clip.length, Is.GreaterThan(.15f), entry.text);
                Assert.That(clip.channels, Is.EqualTo(1), entry.resource);
                Resources.UnloadAsset(clip);
            }
        }

        [Test] public void NarrationPreferencesMigrateOldSavesAndRemainPerProfile()
        {
            string path = Path.Combine(directory, "old-save.json");
            File.WriteAllText(path, "{\"version\":1,\"profiles\":[{\"name\":\"Ada\",\"xp\":90,\"completed\":[\"child-0\"]}]}");
            var store = new LearningProgress(path);
            Assert.That(store.Data.Active.voiceEnabled, Is.True);
            Assert.That(store.Data.Active.voiceAutoplay, Is.True);
            Assert.That(store.Data.Active.voiceVolume, Is.EqualTo(.9f).Within(.001f));
            store.Data.Active.voiceEnabled = false; store.Data.Active.voiceAutoplay = false; store.Data.Active.voiceVolume = .4f;
            store.AddProfile("Can");
            Assert.That(store.Data.Active.voiceEnabled, Is.True);
            Assert.That(store.Save(), Is.True);
            var saved = new LearningProgress(path).Data;
            Assert.That(saved.profiles[0].xp, Is.EqualTo(90));
            Assert.That(saved.profiles[0].completed, Does.Contain("child-0"));
            Assert.That(saved.profiles[0].voiceEnabled, Is.False);
            Assert.That(saved.profiles[0].voiceAutoplay, Is.False);
            Assert.That(saved.profiles[0].voiceVolume, Is.EqualTo(.4f).Within(.001f));
            Assert.That(saved.profiles[1].voiceEnabled, Is.True);
        }
    }
}
