using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Deprem.Minigames
{
    [Serializable]
    public sealed class MinigameResultRecord
    {
        public string minigameId = string.Empty;
        public int bestStars;
        public int bestScore;
        public int bestCoins;
        public float bestTimeSeconds;
        public int completionCount;

        public void Normalize()
        {
            minigameId ??= string.Empty;
            bestStars = Mathf.Clamp(bestStars, 0, 3);
            bestScore = Mathf.Max(0, bestScore);
            bestCoins = Mathf.Clamp(bestCoins, 0, 50);
            bestTimeSeconds = Mathf.Max(0f, bestTimeSeconds);
            completionCount = Mathf.Max(0, completionCount);
        }
    }

    [Serializable]
    public sealed class MinigameProfileData
    {
        public int schemaVersion = 1;
        public List<MinigameResultRecord> results = new List<MinigameResultRecord>();

        public void Normalize()
        {
            schemaVersion = Mathf.Max(1, schemaVersion);
            results ??= new List<MinigameResultRecord>();
            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (results[i] == null || string.IsNullOrWhiteSpace(results[i].minigameId))
                {
                    results.RemoveAt(i);
                    continue;
                }

                results[i].Normalize();
            }
        }
    }

    /// <summary>
    /// Sahneye açık referansla yerleştirilen kalıcı minigame profil deposu.
    /// Singleton veya DontDestroyOnLoad kullanmaz; bütün sahneler aynı JSON dosyasını okur.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MinigameProgressManager : MonoBehaviour
    {
        private const string DefaultProfileFileName = "minigame-profile.json";
        public const int MaxBestCoins = 400;

        [SerializeField] private string profileFileName = DefaultProfileFileName;
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private UnityEvent onProfileLoaded = new UnityEvent();
        [SerializeField] private UnityEvent onProfileChanged = new UnityEvent();

        private MinigameProfileData profile = new MinigameProfileData();
        private bool loaded;

        public bool IsLoaded => loaded;
        public string ProfilePath => Path.Combine(
            Application.persistentDataPath,
            string.IsNullOrWhiteSpace(profileFileName) ? DefaultProfileFileName : profileFileName);
        public int TotalBestCoins
        {
            get
            {
                int total = 0;
                if (profile?.results == null)
                    return total;

                for (int i = 0; i < profile.results.Count; i++)
                    total += Mathf.Clamp(profile.results[i].bestCoins, 0, 50);
                return Mathf.Clamp(total, 0, MaxBestCoins);
            }
        }

        private void Awake()
        {
            if (loadOnAwake)
                LoadNow();
        }

        public void LoadNow()
        {
            if (loaded)
                return;

            profile = new MinigameProfileData();
            string path = ProfilePath;
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    MinigameProfileData loadedProfile = JsonUtility.FromJson<MinigameProfileData>(json);
                    if (loadedProfile != null)
                        profile = loadedProfile;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Minigame profili okunamadı; güvenli boş profil kullanılıyor. " + exception.Message);
                profile = new MinigameProfileData();
            }

            profile.Normalize();
            loaded = true;
            onProfileLoaded.Invoke();
        }

        public MinigameResultRecord GetRecord(string minigameId)
        {
            LoadNow();
            if (string.IsNullOrWhiteSpace(minigameId))
                return null;

            for (int i = 0; i < profile.results.Count; i++)
            {
                MinigameResultRecord candidate = profile.results[i];
                if (string.Equals(candidate.minigameId, minigameId, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        public void RecordResult(string minigameId, int stars, int score, int coins, float elapsedSeconds)
        {
            if (string.IsNullOrWhiteSpace(minigameId))
                throw new ArgumentException("Minigame kimliği boş olamaz.", nameof(minigameId));

            LoadNow();
            MinigameResultRecord record = GetRecord(minigameId);
            if (record == null)
            {
                record = new MinigameResultRecord { minigameId = minigameId };
                profile.results.Add(record);
            }

            stars = Mathf.Clamp(stars, 1, 3);
            score = Mathf.Max(0, score);
            coins = Mathf.Clamp(coins, 0, 50);
            elapsedSeconds = Mathf.Max(0f, elapsedSeconds);

            record.completionCount++;
            record.bestStars = Mathf.Max(record.bestStars, stars);
            record.bestScore = Mathf.Max(record.bestScore, score);
            record.bestCoins = Mathf.Max(record.bestCoins, coins);
            if (elapsedSeconds > 0f &&
                (record.bestTimeSeconds <= 0f || elapsedSeconds < record.bestTimeSeconds))
                record.bestTimeSeconds = elapsedSeconds;

            record.Normalize();
            SaveNow();
            onProfileChanged.Invoke();
        }

        public void SaveNow()
        {
            LoadNow();
            profile.Normalize();
            string path = ProfilePath;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporaryPath, path);
        }

        public static int StarsForScore(int score)
        {
            if (score >= 900)
                return 3;
            return score >= 700 ? 2 : 1;
        }

        public static int CoinsForStars(int stars)
        {
            return stars >= 3 ? 50 : stars == 2 ? 40 : 30;
        }
    }
}
