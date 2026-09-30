using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class Story03CoverVideoCapture
{
    private const string ScenePath = "Assets/Scenes/Story_03_RebuildPreview.unity";
    private const string PendingKey = "Deprem.Story03CoverCapture.Pending";
    private const string OutputKey = "Deprem.Story03CoverCapture.Output";

    static Story03CoverVideoCapture()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Deprem Story/Video/Record Story 03 Clean Cover Test")]
    private static void Record()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Story 03 kaydı başlamadan önce Play Mode'dan çık.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string outputFolder = Path.GetFullPath(Path.Combine("Recordings", "Story03"));
        Directory.CreateDirectory(outputFolder);
        string output = Path.Combine(
            outputFolder,
            "Story03_CokKapanTutun_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        SessionState.SetBool(PendingKey, true);
        SessionState.SetString(OutputKey, output);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        GameObject driverObject = new GameObject("__Story03CoverCaptureDriver");
        driverObject.SetActive(false);
        Story03CoverCaptureDriver driver = driverObject.AddComponent<Story03CoverCaptureDriver>();
        driver.OutputFile = SessionState.GetString(OutputKey, string.Empty);
        driverObject.SetActive(true);
    }
}

internal sealed class Story03CoverCaptureDriver : MonoBehaviour
{
    internal string OutputFile { get; set; }
    private RecorderController recorder;

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        director.StopAllCoroutines();
        ClearSubtitle(ui);

        Invoke(director, "BeginQuake", false);
        Invoke(director, "SetSiblingCalmedState", false);
        Hide("QuakeRebuildOpeningProps", "Can_ToyCar", "Can_ToyCar_FinishMarker", "FamilyBoardGame");
        DisableQuakeDust();
        ActivateCameraZone(cameraController, "QuakeClose");

        StartRecorder();
        yield return new WaitForSecondsRealtime(2.25f);
        Invoke(director, "OnCrouchStep");
        ActivateCameraZone(cameraController, "QuakeClose");

        yield return new WaitForSecondsRealtime(2.6f);
        Invoke(director, "OnCoverHeadStep");
        ActivateCameraZone(cameraController, "QuakeClose");

        yield return new WaitForSecondsRealtime(2.5f);
        Invoke(director, "OnCoverReached");
        ActivateCameraZone(cameraController, "QuakeClose");
        yield return new WaitForSecondsRealtime(3.4f);

        recorder.StopRecording();
        File.WriteAllText(
            OutputFile + "_DONE.txt",
            "Story 03 clean cover capture completed at " + DateTime.Now.ToString("O"));
        Debug.Log("Story 03 Çök-Kapan-Tutun videosu kaydedildi: " + OutputFile + ".mp4");
        yield return new WaitForSecondsRealtime(0.5f);
        EditorApplication.ExitPlaymode();
    }

    private void StartRecorder()
    {
        RecorderControllerSettings controllerSettings =
            ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30f;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Story 03 Clean Cover Test — Portrait MP4";
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
        movie.OutputFile = OutputFile;
        controllerSettings.AddRecorderSettings(movie);

        recorder = new RecorderController(controllerSettings);
        recorder.PrepareRecording();
        recorder.StartRecording();
    }

    private static MonoBehaviour FindBehaviour(string typeName)
    {
        MonoBehaviour result = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == typeName);
        if (result == null)
            throw new InvalidOperationException(typeName + " bulunamadı.");
        return result;
    }

    private static object Invoke(MonoBehaviour target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        return method.Invoke(target, args);
    }

    private static void ActivateCameraZone(MonoBehaviour cameraController, string zoneName)
    {
        MethodInfo activate = cameraController.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == "ActivateZone" && method.GetParameters().Length == 2);
        object zone = Enum.Parse(activate.GetParameters()[0].ParameterType, zoneName);
        activate.Invoke(cameraController, new[] { zone, (object)true });
    }

    private static void Hide(params string[] names)
    {
        foreach (Transform item in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (item != null && names.Contains(item.name))
                item.gameObject.SetActive(false);
        }
    }

    private static void DisableQuakeDust()
    {
        foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (particles == null ||
                (!particles.name.Contains("Dust") && !particles.name.Contains("Debris")))
                continue;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
        }
    }

    private static void ClearSubtitle(MonoBehaviour ui)
    {
        ui.StopAllCoroutines();
        Invoke(ui, "ResetSubtitleState", false);
        FieldInfo subtitleField = ui.GetType().GetField(
            "subtitle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Component subtitle = subtitleField?.GetValue(ui) as Component;
        if (subtitle == null)
            return;
        GameObject displayRoot = subtitle.transform.parent != null
            ? subtitle.transform.parent.gameObject
            : subtitle.gameObject;
        displayRoot.SetActive(false);
    }
}
