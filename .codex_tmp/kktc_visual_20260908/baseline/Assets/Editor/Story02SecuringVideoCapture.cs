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
internal static class Story02SecuringVideoCapture
{
    private const string ScenePath = "Assets/Scenes/Story_02_RebuildPreview.unity";
    private const string PendingKey = "Deprem.Story02SecuringCapture.Pending";
    private const string OutputKey = "Deprem.Story02SecuringCapture.Output";

    static Story02SecuringVideoCapture()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Deprem Story/Video/Record Story 02 Securing Test")]
    private static void Record()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Story 02 kaydı başlamadan önce Play Mode'dan çık.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string outputFolder = Path.GetFullPath(Path.Combine("Recordings", "Story02"));
        Directory.CreateDirectory(outputFolder);
        string output = Path.Combine(
            outputFolder,
            "Story02_SabitlemeTesti_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        SessionState.SetBool(PendingKey, true);
        SessionState.SetString(OutputKey, output);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        GameObject driverObject = new GameObject("__Story02SecuringCaptureDriver");
        driverObject.SetActive(false);
        Story02SecuringCaptureDriver driver = driverObject.AddComponent<Story02SecuringCaptureDriver>();
        driver.OutputFile = SessionState.GetString(OutputKey, string.Empty);
        driverObject.SetActive(true);
    }
}

internal sealed class Story02SecuringCaptureDriver : MonoBehaviour
{
    internal string OutputFile { get; set; }

    private RecorderController recorder;

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryHomeSafetyDirector");
        director.StopAllCoroutines();
        ClearSubtitle(ui);
        Invoke(director, "BeginShelfSecuring");

        StartRecorder();
        yield return new WaitForSecondsRealtime(1.8f);

        // Real authored interaction: Deniz hands the bracket to Anne. This triggers
        // the parent work pose, drill sequence, strap animation, dust and dialogue.
        Invoke(director, "HandShelfBracket");
        yield return new WaitForSecondsRealtime(10.1f);

        // Run the authored post-fix stability check from the same shelf point.
        Invoke(director, "TestSecuredShelf");
        yield return new WaitForSecondsRealtime(7.8f);

        recorder.StopRecording();
        File.WriteAllText(
            OutputFile + "_DONE.txt",
            "Story 02 shelf securing capture completed at " + DateTime.Now.ToString("O"));
        Debug.Log("Story 02 sabitleme videosu kaydedildi: " + OutputFile + ".mp4");
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
        movie.name = "Story 02 Securing Test — Portrait MP4";
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

    private static void ClearSubtitle(MonoBehaviour ui)
    {
        ui.StopAllCoroutines();
        Invoke(ui, "ResetSubtitleState", false);
        FieldInfo subtitleField = ui.GetType().GetField(
            "subtitle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Component subtitle = subtitleField?.GetValue(ui) as Component;
        if (subtitle != null)
        {
            GameObject displayRoot = subtitle.transform.parent != null
                ? subtitle.transform.parent.gameObject
                : subtitle.gameObject;
            displayRoot.SetActive(false);
        }
    }
}
