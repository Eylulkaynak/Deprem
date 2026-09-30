using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class StoryManualGameplayRecorder
{
    private const string Story01Scene = "Assets/Scenes/Story_01_RebuildPreview.unity";
    private const string WindowTitle = "Recorder";
    private static RecorderControllerSettings activeSettings;

    [MenuItem("Tools/Deprem Story/Recording/Prepare Story 01 Manual Recording")]
    private static void PrepareStory01Recording()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!File.Exists(Path.GetFullPath(Story01Scene)))
        {
            Debug.LogError("Story 01 scene was not found: " + Story01Scene);
            return;
        }

        if (!EditorApplication.isPlaying)
            EditorSceneManager.OpenScene(Story01Scene, OpenSceneMode.Single);

        string outputFolder = Path.GetFullPath(Path.Combine("Recordings", "Story01"));
        Directory.CreateDirectory(outputFolder);

        activeSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        activeSettings.name = "Story 01 Manual Gameplay Recording";
        activeSettings.SetRecordModeToManual();
        activeSettings.FrameRate = 60f;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Story 01 — 1080x1920 MP4";
        movie.Enabled = true;
        movie.CaptureAudio = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1080,
            OutputHeight = 1920
        };
        movie.OutputFile = Path.Combine(
            outputFolder,
            "Story01_FullGameplay_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        activeSettings.AddRecorderSettings(movie);

        RecorderWindow window = EditorWindow.GetWindow<RecorderWindow>(false, WindowTitle);
        window.SetRecorderControllerSettings(activeSettings);
        window.Show();
        window.Focus();
        Debug.Log("Story 01 Recorder hazır: 1080x1920, 60 FPS, MP4 ve oyun sesi açık. " +
                  "Recorder penceresindeki START RECORDING kaydı başlatır; Play Mode'dan çıkmak kaydı bitirir. " +
                  "Çıktı: " + outputFolder);
    }

    [MenuItem("Tools/Deprem Story/Recording/Start Story 01 Manual Recording")]
    private static void StartStory01Recording()
    {
        PrepareStory01Recording();
        if (activeSettings == null)
            return;

        EditorWindow.GetWindow<RecorderWindow>(false, WindowTitle).StartRecording();
    }

    [MenuItem("Tools/Deprem Story/Recording/Stop Manual Recording")]
    private static void StopRecording()
    {
        EditorWindow.GetWindow<RecorderWindow>(false, WindowTitle).StopRecording();
    }
}
