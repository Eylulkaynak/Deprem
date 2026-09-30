using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-only evidence from the actual Cinemachine state during existing tests.</summary>
[InitializeOnLoad]
public static class StoryKktcTrackingQA
{
    const string Key = "KKTC.TrackingQA";
    static double next;
    static bool sawPlay;
    static StoryKktcTrackingQA()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseString(Key);
        };
    }

    [MenuItem("Tools/Deprem Story/KKTC/10 Capture Tracking During Next PlayMode Run")]
    public static void Arm()
    {
        string path = "ClientExports/KKTC/TrackingReview/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(path);
        File.WriteAllText(path + "/Frames.tsv", "Frame\tScene\tTime\tShot\tCameraPosition\tCameraRotation\tDeniz\tDenizViewport\tCentralHit\tObjective\n");
        SessionState.SetString(Key, path);
        SessionState.SetInt(Key + ".frame", 0);
        next = 0; sawPlay = false;
        Debug.Log("KKTC_TRACKING_CAPTURE_ARMED " + path);
    }

    static void Tick()
    {
        string path = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(path)) return;
        if (!EditorApplication.isPlaying)
        {
            if (sawPlay && !EditorApplication.isPlayingOrWillChangePlaymode)
                SessionState.EraseString(Key);
            return;
        }
        sawPlay = true;
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + .5;
        Camera camera = Camera.main;
        if (camera == null) return;
        var brain = camera.GetComponent<CinemachineBrain>();
        string shot = brain?.ActiveVirtualCamera?.Name ?? "none";
        if (!shot.Contains("Overview") && !shot.Contains("PreparationDeniz") && !shot.Contains("ExitShelf")) return;
        var deniz = UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None).FirstOrDefault();
        if (deniz == null) return;
        int frame = SessionState.GetInt(Key + ".frame", 0);
        SessionState.SetInt(Key + ".frame", frame + 1);
        string objective = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == "ObjectiveTitle")?.text ?? "";
        Physics.Raycast(camera.ViewportPointToRay(new Vector3(.5f,.5f)), out RaycastHit hit, 30, ~0, QueryTriggerInteraction.Ignore);
        File.AppendAllText(path + "/Frames.tsv", $"{frame}\t{SceneManager.GetActiveScene().name}\t{Time.time:F2}\t{shot}\t{camera.transform.position:F3}\t{camera.transform.eulerAngles:F1}\t{deniz.transform.position:F3}\t{camera.WorldToViewportPoint(deniz.transform.position + Vector3.up * .85f):F3}\t{hit.collider?.name}\t{objective.Replace('\n',' ')}\n");
        StoryKktcVisualQA.Capture(camera, path + "/frame_" + frame.ToString("D4") + ".png", 360, 640);
    }
}
