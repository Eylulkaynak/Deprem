using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryGameManager : MonoBehaviour
    {
        public static StoryGameManager Instance { get; private set; }

        [Header("Session")]
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool loadExistingSave = true;
#if UNITY_EDITOR
        [SerializeField] private bool startFreshOnFirstEditorPlay = true;
        private static bool editorFreshStartApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEditorFreshStart()
        {
            editorFreshStartApplied = false;
        }
#endif
        [SerializeField] private StoryAct initialAct = StoryAct.Quake;
        [SerializeField] private StoryFlag[] initialFlags =
        {
            StoryFlag.WardrobeSecured,
            StoryFlag.ExitCleared,
            StoryFlag.BagWater,
            StoryFlag.BagFirstAid
        };

        public StorySessionState CurrentState { get; private set; }
        public string SavePath => Path.Combine(Application.persistentDataPath, "story-session.json");

        public event Action<StorySessionState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            bool shouldLoadExisting = loadExistingSave;
#if UNITY_EDITOR
            if (startFreshOnFirstEditorPlay && !editorFreshStartApplied)
            {
                editorFreshStartApplied = true;
                shouldLoadExisting = false;
            }
#endif
            CurrentState = shouldLoadExisting ? LoadState() : null;
            CurrentState ??= CreateInitialState();
            CurrentState.Normalize();
        }

        public void SetFlag(StoryFlag flag, bool value = true)
        {
            EnsureState();
            CurrentState.SetFlag(flag, value);
            SaveState();
        }

        public bool HasFlag(StoryFlag flag)
        {
            EnsureState();
            return CurrentState.HasFlag(flag);
        }

        public void CommitCheckpoint(StoryCheckpoint checkpoint)
        {
            EnsureState();
            CurrentState.checkpoint = checkpoint;
            CurrentState.activeScene = SceneManager.GetActiveScene().name;
            SaveState();
        }

        public void AddMistake()
        {
            EnsureState();
            CurrentState.mistakeCount++;
            SaveState();
        }

        public void BeginAct(StoryAct act)
        {
            EnsureState();
            CurrentState.activeAct = act;
            CurrentState.activeScene = SceneManager.GetActiveScene().name;
            SaveState();
        }

        public void CompleteAct(StoryAct act)
        {
            EnsureState();
            if (!CurrentState.completedActs.Contains(act))
                CurrentState.completedActs.Add(act);
            SaveState();
        }

        public void RetryCheckpoint()
        {
            SaveState();
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
                SceneManager.LoadScene(activeScene.name);
        }

        public void ContinueStory()
        {
            EnsureState();
            string sceneName = CurrentState.activeScene;
            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
                SceneManager.LoadScene(sceneName);
        }

        public void ResetStory()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            CurrentState = CreateInitialState();
            SaveState();
        }

        public void SaveState()
        {
            EnsureState();

            try
            {
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(CurrentState, true);
                File.WriteAllText(SavePath, json, new UTF8Encoding(false));
                StateChanged?.Invoke(CurrentState);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Story save could not be written: {exception.Message}", this);
            }
        }

        private StorySessionState LoadState()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return null;

                string json = File.ReadAllText(SavePath, Encoding.UTF8);
                StorySessionState state = JsonUtility.FromJson<StorySessionState>(json);
                state?.Normalize();
                return state;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Story save could not be read and will be recreated: {exception.Message}", this);
                return null;
            }
        }

        private StorySessionState CreateInitialState()
        {
            StorySessionState state = new StorySessionState
            {
                activeAct = initialAct,
                activeScene = SceneManager.GetActiveScene().name,
                checkpoint = StoryCheckpoint.None,
                flags = new List<StoryFlag>()
            };

            foreach (StoryFlag flag in initialFlags)
                state.SetFlag(flag, true);

            return state;
        }

        private void EnsureState()
        {
            CurrentState ??= CreateInitialState();
            CurrentState.Normalize();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && CurrentState != null)
                SaveState();
        }

        private void OnApplicationQuit()
        {
            if (CurrentState != null)
                SaveState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
