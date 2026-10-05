using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deprem.Minigames
{
    // A practice run advances only from a completed game's result. Existing
    // per-game saves continue to own scores; abandoning a run awards nothing.
    public static class MinigameScenarioJourney
    {
        static readonly string[] Scenes =
        {
            "Minigame_EmergencyBagRush", "Minigame_RoomSafety", "Minigame_AftershockCover",
            "Minigame_Evacuation_25D", "Minigame_EmergencyCorridor", "Minigame_RubbleSignal"
        };
        static readonly string[] Phases =
        {
            "Hazırlık · Çanta", "Hazırlık · Güvenli oda", "Deprem anı · Korun",
            "Deprem sonrası · Tahliye", "Deprem sonrası · Yardım yolu", "Arama kurtarma · Sinyali bul"
        };
        public static int StepIndex { get; private set; } = -1;
        public static int StepCount => Scenes.Length;
        public static bool IsActive => StepIndex >= 0 && StepIndex < Scenes.Length;
        public static bool IsCurrentScene => IsActive && SceneManager.GetActiveScene().name == Scenes[StepIndex];
        public static bool IsLastStep => IsActive && StepIndex == Scenes.Length - 1;
        public static string Phase => IsActive ? Phases[StepIndex] : "";
        public static string NextPhase => IsActive && !IsLastStep ? Phases[StepIndex + 1] : "Senaryo tamamlandı";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Cancel() => StepIndex = -1;

        public static bool Start()
        {
            foreach (string scene in Scenes)
                if (!Application.CanStreamedLevelBeLoaded(scene)) return false;
            StepIndex = 0;
            Time.timeScale = 1;
            SceneManager.LoadScene(Scenes[0]);
            return true;
        }
        public static bool ContinueAfterCompletion()
        {
            if (!IsCurrentScene) return false;
            StepIndex++;
            Time.timeScale = 1;
            if (StepIndex < Scenes.Length) SceneManager.LoadScene(Scenes[StepIndex]);
            else { Cancel(); SceneManager.LoadScene("Minigame_Hub"); }
            return true;
        }
    }
}
