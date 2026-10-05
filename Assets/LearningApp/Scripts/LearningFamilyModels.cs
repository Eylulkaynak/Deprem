using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Deprem.Learning
{
    [Serializable] public sealed class FamilyReviewTopic { public string courseId; public int count; }
    [Serializable] public sealed class FamilySnapshot
    {
        public string name, lastActivity = "", resumeCourse = "";
        public int xp, streak, longestStreak, todayActivities, resumeExercise;
        public string[] completed = Array.Empty<string>();
        public LearningResult[] results = Array.Empty<LearningResult>();
        public FamilyReviewTopic[] reviewTopics = Array.Empty<FamilyReviewTopic>();
        public int TotalStars => (results ?? Array.Empty<LearningResult>()).Sum(r => r.stars);
        public int DisplayStreak(DateTime today)
        {
            if (!DateTime.TryParseExact(lastActivity, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var previous)) return 0;
            int days = (today.Date - previous).Days;
            return days >= 0 && days <= 1 ? streak : 0;
        }
        // Explicit projection keeps plans, addresses, answers, and adult lessons on the device.
        public static FamilySnapshot FromProfile(LearningProfile profile, LearningCatalog catalog)
        {
            var courses = catalog.Courses(false);
            var courseIds = new HashSet<string>(courses.Select(c => c.id));
            var gameIds = new HashSet<string>(catalog.Levels(false).Select(g => g.SaveId));
            var done = profile.completed.Where(courseIds.Contains).Distinct().OrderBy(id => id).ToArray();
            var games = profile.results.Where(r => gameIds.Contains(r.id) && r.stars > 0).GroupBy(r => r.id)
                .Select(g => new LearningResult { id = g.Key, stars = Math.Clamp(g.Max(r => r.stars), 1, 3), score = Math.Clamp(g.Max(r => r.score), 0, 1000000) }).ToArray();
            var topics = courses.Select(c => new FamilyReviewTopic {
                courseId = c.id,
                count = (int)Math.Clamp(profile.mistakes.Where(m => c.exercises.Any(e => e.kind != "info" && e.prompt == m.question)).Sum(m => (long)m.count), 0L, 1000000L)
            }).Where(t => t.count > 0).ToArray();
            bool hasResume = courseIds.Contains(profile.resumeCourse);
            return new FamilySnapshot {
                name = profile.name, completed = done, results = games, reviewTopics = topics,
                xp = done.Length * 30 + games.Length * 20,
                streak = Math.Clamp(profile.streak, 0, 10000), longestStreak = Math.Clamp(profile.longestStreak, 0, 10000),
                todayActivities = Math.Clamp(profile.todayActivities, 0, 10000), lastActivity = profile.lastActivity ?? "",
                resumeCourse = hasResume ? profile.resumeCourse : "", resumeExercise = hasResume ? Math.Clamp(profile.resumeExercise, 0, 100) : 0
            };
        }
    }
    [Serializable] public sealed class FamilyProgressRequest { public FamilySnapshot progress; }
    [Serializable] public sealed class FamilyChild { public string id, linkedAt, updatedAt; public FamilySnapshot progress; }
    [Serializable] public sealed class FamilyChildrenResponse { public FamilyChild[] children = Array.Empty<FamilyChild>(); public string serverTime; }
    [Serializable] public sealed class FamilyGuardian { public string id, name, linkedAt; }
    [Serializable] public sealed class FamilyGuardiansResponse { public FamilyGuardian[] guardians = Array.Empty<FamilyGuardian>(); }
    [Serializable] public sealed class FamilyPairCode { public string code, expiresAt; }
    [Serializable] public sealed class FamilyAuthRequest { public string email, password, name; }
    [Serializable] public sealed class FamilySession { public string token, email, name, expiresAt; }
    [Serializable] public sealed class FamilyCodeRequest { public string code; }
    [Serializable] public sealed class FamilyOk { public bool ok; public string id, updatedAt; }
    [Serializable] public sealed class FamilyError { public string error, message; }
    public sealed class FamilyResponse<T> where T : class
    {
        public T Value;
        public string Error;
        public long Status;
        public bool Success => Value != null && Error == null;
    }

    [Serializable] public sealed class LearningFamilyConfig
    {
        public string serviceUrl = "";
        public int timeoutSeconds = 15;
#if UNITY_EDITOR
        // Verification can choose a separate service without editing a build asset.
        public static string EditorEndpointOverride;
#endif
        public static LearningFamilyConfig Load()
        {
            var asset = Resources.Load<TextAsset>("LearningApp/FamilyConnection");
            var result = asset == null ? new LearningFamilyConfig() : JsonUtility.FromJson<LearningFamilyConfig>(asset.text);
            result ??= new LearningFamilyConfig();
#if UNITY_EDITOR
            result.serviceUrl = EditorEndpointOverride ?? (string.IsNullOrWhiteSpace(result.serviceUrl) ? "http://127.0.0.1:8787" : result.serviceUrl);
#endif
            result.timeoutSeconds = Math.Clamp(result.timeoutSeconds, 5, 30);
            bool localHttp = false;
#if UNITY_EDITOR
            localHttp = true;
#endif
            result.serviceUrl = NormalizeEndpoint(result.serviceUrl, localHttp);
            return result;
        }
        public static string NormalizeEndpoint(string value, bool allowLocalHttp)
        {
            if (!Uri.TryCreate((value ?? "").Trim(), UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return "";
            if (uri.Scheme != "https" && !(allowLocalHttp && uri.Scheme == "http" && uri.IsLoopback)) return "";
            return uri.AbsoluteUri.TrimEnd('/');
        }
    }

    [Serializable] public sealed class FamilyDeviceCredential
    {
        public string profileId, token, lastSyncUtc = "";
        public bool enabled, pendingStop;
        [NonSerialized] public string uploadedJson, syncError;
        [NonSerialized] public float retryAfter;
        [NonSerialized] public int failures;
    }
    [Serializable] public sealed class FamilyDeviceSave
    {
        public int version = 1;
        public string serviceUrl;
        public List<FamilyDeviceCredential> children = new List<FamilyDeviceCredential>();
    }
    public sealed class FamilyDeviceStore
    {
        private readonly string path;
        public FamilyDeviceSave Data { get; private set; }
        public string LastError { get; private set; }
        public FamilyDeviceStore(string path, string endpoint)
        {
            this.path = path;
            Data = new FamilyDeviceSave { serviceUrl = endpoint };
            if (!File.Exists(path)) return;
            try
            {
                var loaded = JsonUtility.FromJson<FamilyDeviceSave>(File.ReadAllText(path));
                if (loaded == null || loaded.version != 1 || loaded.children == null) throw new InvalidDataException();
                // A build pointing to a different service must never forward the old service's credentials.
                if (loaded.serviceUrl != endpoint) return;
                if (loaded.children.Any(c => c == null || string.IsNullOrEmpty(c.profileId) ||
                    (!string.IsNullOrEmpty(c.token) && !System.Text.RegularExpressions.Regex.IsMatch(c.token, "^d_[A-Za-z0-9_-]{43}$")))) throw new InvalidDataException();
                Data = loaded;
            }
            catch { LastError = "Bağlantı kaydı okunamadı. Paylaşım bu cihazda kapatıldı."; }
            // Fail closed: never restore a backup which might re-enable revoked sharing.
        }
        public FamilyDeviceCredential ForProfile(string id) => Data.children.FirstOrDefault(c => c.profileId == id);
        public FamilyDeviceCredential Add(string id)
        {
            var entry = ForProfile(id);
            if (entry != null) return entry;
            entry = new FamilyDeviceCredential { profileId = id }; Data.children.Add(entry); return entry;
        }
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Data));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                LastError = null; return true;
            }
            catch { LastError = "Bağlantı ayarı kaydedilemedi. Cihazdaki boş alanı kontrol edin."; return false; }
        }
        public static string NewDeviceToken()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return "d_" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
