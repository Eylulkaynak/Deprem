using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Minigames
{
    [Serializable]
    public sealed class MinigameHubCardBinding
    {
        public string minigameId = string.Empty;
        public string sceneName = string.Empty;
        public Button playButton;
        public TMP_Text resultText;
        public TMP_Text starsText;
        public TMP_Text coinText;
        public TMP_Text timeText;
    }

    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public sealed class MinigameHubManager : MonoBehaviour
    {
        [SerializeField] private MinigameProgressManager progressManager;
        [SerializeField] private MinigameHubCardBinding[] cards = Array.Empty<MinigameHubCardBinding>();
        [SerializeField] private TMP_Text totalCoinText;
        [SerializeField] private string mainMenuSceneName = "Story_Rebuild_MainMenu";

        public int CardCount => cards?.Length ?? 0;

        private void Start()
        {
            MinigameScenarioJourney.Cancel();
            progressManager.LoadNow();
            RefreshCards();
        }

        public void RefreshCards()
        {
            if (progressManager == null || cards == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                MinigameHubCardBinding card = cards[i];
                if (card == null)
                    continue;
                MinigameResultRecord record = progressManager.GetRecord(card.minigameId);
                bool hasResult = record != null && record.completionCount > 0;
                if (card.playButton != null)
                    card.playButton.interactable = !string.IsNullOrWhiteSpace(card.sceneName);
                if (card.resultText != null)
                    card.resultText.text = hasResult ? "EN İYİ SONUÇ" : "İLK GÖREV HAZIR";
                if (card.starsText != null)
                {
                    int stars = hasResult ? record.bestStars : 0;
                    card.starsText.text = stars + " / 3 YILDIZ";
                }
                if (card.coinText != null)
                    card.coinText.text = (hasResult ? record.bestCoins : 0) + " / 50 İMO";
                if (card.timeText != null)
                    card.timeText.text = hasResult ? FormatTime(record.bestTimeSeconds) : "--:--";
            }

            if (totalCoinText != null)
                totalCoinText.text = progressManager.TotalBestCoins + " / " +
                                     MinigameProgressManager.MaxBestCoins + " İMO COIN";
        }

        public void OpenScene(string sceneName)
        {
            MinigameScenarioJourney.Cancel();
            if (!string.IsNullOrWhiteSpace(sceneName))
                SceneManager.LoadScene(sceneName);
        }

        public void ReturnToMainMenu()
        {
            MinigameScenarioJourney.Cancel();
            if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
                SceneManager.LoadScene(mainMenuSceneName);
        }

        public void StartScenarioJourney()
        {
            if (!MinigameScenarioJourney.Start())
                Debug.LogError("Deprem senaryosunun bir sahnesi derlemeye eklenmemiş.", this);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
