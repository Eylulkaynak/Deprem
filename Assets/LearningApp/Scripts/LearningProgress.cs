using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deprem.Learning
{
    [Serializable] public sealed class LearningResult { public string id; public int stars, score; }
    [Serializable] public sealed class LearningPlanValue { public string key, value; public bool done; }
    [Serializable] public sealed class LearningMistake { public string question, answer, correct; public int count; }
    [Serializable] public sealed class LearningProfile
    {
        public string id = Guid.NewGuid().ToString("N"), name = "Kahraman";
        public bool adult;
        public bool voiceEnabled = true, voiceAutoplay = true;
        public float voiceVolume = 0.9f;
        public int xp, streak, longestStreak, todayActivities;
        public string lastActivity = "", resumeCourse = "";
        public int resumeExercise, resumeMistakes;
        public List<string> completed = new List<string>();
        public List<LearningResult> results = new List<LearningResult>();
        public List<LearningPlanValue> plan = new List<LearningPlanValue>();
        public List<LearningMistake> mistakes = new List<LearningMistake>();
        public int TotalStars => results.Sum(r => r.stars);
        public int DisplayStreak(DateTime today) => DateTime.TryParseExact(lastActivity, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime previous) && (today.Date - previous).Days <= 1 ? streak : 0;
        public LearningPlanValue Plan(string key)
        {
            var value = plan.FirstOrDefault(p => p.key == key);
            if (value != null) return value;
            value = new LearningPlanValue { key = key, value = "" }; plan.Add(value); return value;
        }
        public void RecordActivity(DateTime date)
        {
            string today = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (lastActivity != today)
            {
                streak = DateTime.TryParseExact(lastActivity, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime previous) && (date.Date - previous).Days == 1 ? streak + 1 : 1;
                todayActivities = 0;
            }
            lastActivity = today; todayActivities++; longestStreak = Math.Max(longestStreak, streak);
        }
        public bool CompleteCourse(string id, DateTime date)
        {
            bool first = !completed.Contains(id);
            if (first) { completed.Add(id); xp += 30; }
            resumeCourse = ""; resumeExercise = 0; resumeMistakes = 0;
            RecordActivity(date); return first;
        }
        public void CompleteGame(string id, int stars, int score, DateTime date)
        {
            var best = results.FirstOrDefault(r => r.id == id);
            if (best == null) { best = new LearningResult { id = id }; results.Add(best); xp += 20; }
            best.stars = Math.Max(best.stars, Math.Clamp(stars, 1, 3));
            best.score = Math.Max(best.score, score); RecordActivity(date);
        }
        public void RecordMistake(string question, string answer, string correct)
        {
            var entry = mistakes.FirstOrDefault(m => m.question == question);
            if (entry == null) { entry = new LearningMistake { question = question }; mistakes.Add(entry); }
            entry.answer = answer; entry.correct = correct; entry.count++;
        }
        public void Normalize()
        {
            completed ??= new List<string>(); results ??= new List<LearningResult>();
            plan ??= new List<LearningPlanValue>(); mistakes ??= new List<LearningMistake>();
            completed = completed.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            results.RemoveAll(r => r == null || string.IsNullOrEmpty(r.id));
            plan.RemoveAll(p => p == null); mistakes.RemoveAll(m => m == null);
            if (string.IsNullOrWhiteSpace(name)) name = "Kahraman";
            voiceVolume = float.IsNaN(voiceVolume) || float.IsInfinity(voiceVolume) ? 0.9f : Mathf.Clamp01(voiceVolume);
        }
    }
    [Serializable] public sealed class LearningSave
    {
        public int version = 1;
        public int activeProfile;
        public float volume = 0.6f;
        public bool music = true, effects = true;
        public List<LearningProfile> profiles = new List<LearningProfile> { new LearningProfile() };
        public LearningProfile Active => profiles[activeProfile];
        public void Normalize()
        {
            if (version != 1) throw new InvalidDataException("Unsupported learning save version.");
            profiles ??= new List<LearningProfile>(); profiles.RemoveAll(p => p == null);
            if (profiles.Count == 0) profiles.Add(new LearningProfile());
            activeProfile = Math.Clamp(activeProfile, 0, profiles.Count - 1);
            volume = Mathf.Clamp01(volume);
            foreach (var profile in profiles) profile.Normalize();
        }
    }
    public sealed class LearningProgress
    {
        private static LearningProgress current;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => current = null;
#if UNITY_EDITOR
        public static void UseVerificationStore(string path) => current = new LearningProgress(path);
#endif
        public static LearningProgress Current => current ??= new LearningProgress(Path.Combine(Application.persistentDataPath, "learning-progress-v1.json"));
        public string SavePath { get; }
        public LearningSave Data { get; private set; }
        public string LastError { get; private set; }
        public LearningProgress(string path)
        {
            SavePath = path;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var loaded = JsonUtility.FromJson<LearningSave>(File.ReadAllText(candidate));
                    if (loaded == null) throw new InvalidDataException("Empty save");
                    loaded.Normalize(); Data = loaded; break;
                }
                catch (Exception e) { LastError = e.Message; }
            }
            Data ??= new LearningSave();
        }
        public bool Save()
        {
            try
            {
                Data.Normalize(); Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(SavePath))
                {
                    // Preserve only a valid previous generation as the recovery copy.
                    try { var prior = JsonUtility.FromJson<LearningSave>(File.ReadAllText(SavePath)); prior.Normalize(); File.Copy(SavePath, SavePath + ".bak", true); }
                    catch { File.Copy(SavePath, SavePath + ".corrupt", true); }
                    File.Delete(SavePath);
                }
                File.Move(temp, SavePath);
                LastError = null; return true;
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("Learning save failed: " + e.Message); return false; }
        }
        public void AddProfile(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 24) throw new ArgumentException("Ad 1–24 karakter olmalı.");
            Data.profiles.Add(new LearningProfile { name = name });
            Data.activeProfile = Data.profiles.Count - 1; Save();
        }
    }
}
