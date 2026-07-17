using System.Collections.Generic;
using System.IO;
using Deprem.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;

internal static class StoryProjectVisualQA
{
    private const string Root = "Tools/Deprem Story/QA/Project Visual/";
    private const string CaptureRoot = "Temp/StoryCameraQA";
    private static int cameraIndex = -1;
    private static readonly List<(StoryCameraZoneId zone, string name, CinemachineCamera camera)> CaptureTargets = new();
    private static int captureIndex;
    private static string captureSceneName;

    [MenuItem(Root + "Open Story01")]
    private static void OpenStory01() => OpenScene("Assets/Scenes/Story_01_BagPreparation.unity");

    [MenuItem(Root + "Open Story02")]
    private static void OpenStory02() => OpenScene("Assets/Scenes/Story_02_HomeSafety.unity");

    [MenuItem(Root + "Open Story03")]
    private static void OpenStory03() => OpenScene("Assets/Scenes/Story_03_Quake.unity");

    [MenuItem(Root + "Open Story04")]
    private static void OpenStory04() => OpenScene("Assets/Scenes/Story_04_Evacuation.unity");

    [MenuItem(Root + "Runtime Load Story01")]
    private static void RuntimeLoadStory01() => RuntimeLoadScene("Story_01_BagPreparation");

    [MenuItem(Root + "Runtime Load Story02")]
    private static void RuntimeLoadStory02() => RuntimeLoadScene("Story_02_HomeSafety");

    [MenuItem(Root + "Runtime Load Story03")]
    private static void RuntimeLoadStory03() => RuntimeLoadScene("Story_03_Quake");

    [MenuItem(Root + "Runtime Load Story04")]
    private static void RuntimeLoadStory04() => RuntimeLoadScene("Story_04_Evacuation");

    [MenuItem(Root + "Next Camera")]
    private static void NextCamera()
    {
        StoryCameraController controller =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        if (!Application.isPlaying || controller == null)
        {
            Debug.LogWarning("Camera QA için bir Story sahnesini Play Mode'da çalıştır.");
            return;
        }

        if (EditorApplication.isPaused)
            EditorApplication.isPaused = false;

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty cameras = serialized.FindProperty("cameras");
        if (cameras == null || cameras.arraySize == 0)
        {
            Debug.LogError("Aktif Story sahnesinde kamera binding'i bulunamadı.");
            return;
        }

        cameraIndex = (cameraIndex + 1) % cameras.arraySize;
        SerializedProperty binding = cameras.GetArrayElementAtIndex(cameraIndex);
        StoryCameraZoneId zone =
            (StoryCameraZoneId)binding.FindPropertyRelative("zone").intValue;
        CinemachineCamera camera =
            binding.FindPropertyRelative("camera").objectReferenceValue as CinemachineCamera;
        controller.ActivateZone(zone, true);
        Debug.Log($"STORY_VISUAL_QA camera={cameraIndex + 1}/{cameras.arraySize} zone={zone} name={camera?.name}");
    }

    [MenuItem(Root + "Next Camera", true)]
    private static bool CanCycleCamera()
    {
        return Application.isPlaying &&
               Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include) != null;
    }

    [MenuItem(Root + "Capture All Cameras")]
    private static void CaptureAllCameras()
    {
        StoryCameraController controller =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        if (!Application.isPlaying || controller == null)
        {
            Debug.LogWarning("Camera capture QA için bir Story sahnesini Play Mode'da çalıştır.");
            return;
        }

        if (EditorApplication.isPaused)
            EditorApplication.isPaused = false;

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty cameras = serialized.FindProperty("cameras");
        if (cameras == null || cameras.arraySize == 0)
        {
            Debug.LogError("Aktif Story sahnesinde kamera binding'i bulunamadı.");
            return;
        }

        CancelCameraCapture();
        CaptureTargets.Clear();
        for (int index = 0; index < cameras.arraySize; index++)
        {
            SerializedProperty binding = cameras.GetArrayElementAtIndex(index);
            StoryCameraZoneId zone =
                (StoryCameraZoneId)binding.FindPropertyRelative("zone").intValue;
            CinemachineCamera camera =
                binding.FindPropertyRelative("camera").objectReferenceValue as CinemachineCamera;
            CaptureTargets.Add((zone, camera != null ? camera.name : "MissingCamera", camera));
        }

        captureSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Directory.CreateDirectory(Path.GetFullPath(CaptureRoot));
        captureIndex = 0;
        Debug.Log($"STORY_CAMERA_CAPTURE_BEGIN scene={captureSceneName} count={CaptureTargets.Count}");
        CaptureAllCamerasSynchronously();
    }

    [MenuItem(Root + "Capture All Cameras", true)]
    private static bool CanCaptureAllCameras()
    {
        return CanCycleCamera();
    }

    private static void CaptureAllCamerasSynchronously()
    {
        Camera outputCamera = null;
        Vector3 originalPosition = default;
        Quaternion originalRotation = default;
        float originalFieldOfView = 0f;
        float originalNearClip = 0f;
        float originalFarClip = 0f;
        bool cameraStateCaptured = false;
        try
        {
            outputCamera = Camera.main;
            if (outputCamera == null)
                throw new System.InvalidOperationException("Camera QA could not resolve Main Camera.");

            originalPosition = outputCamera.transform.position;
            originalRotation = outputCamera.transform.rotation;
            originalFieldOfView = outputCamera.fieldOfView;
            originalNearClip = outputCamera.nearClipPlane;
            originalFarClip = outputCamera.farClipPlane;
            cameraStateCaptured = true;
            for (captureIndex = 0; captureIndex < CaptureTargets.Count; captureIndex++)
            {
                (StoryCameraZoneId zone, string name, CinemachineCamera camera) target =
                    CaptureTargets[captureIndex];
                if (target.camera == null)
                    continue;

                // Render the authored camera transform directly. This avoids stale Game-view
                // buffers when Unity is paused/unfocused and gives deterministic world-only
                // composition evidence without advancing story time.
                outputCamera.transform.SetPositionAndRotation(
                    target.camera.transform.position, target.camera.transform.rotation);
                outputCamera.fieldOfView = target.camera.Lens.FieldOfView;
                outputCamera.nearClipPlane = target.camera.Lens.NearClipPlane;
                outputCamera.farClipPlane = target.camera.Lens.FarClipPlane;
                RaycastHit[] centerHits = Physics.RaycastAll(
                    new Ray(outputCamera.transform.position, outputCamera.transform.forward),
                    120f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(centerHits, (left, right) => left.distance.CompareTo(right.distance));
                string centerHit = centerHits.Length > 0
                    ? $"{HierarchyPath(centerHits[0].transform)}@{centerHits[0].distance:F2}"
                    : "<none>";
                Debug.Log($"STORY_CAMERA_AUTHORED_RAY zone={target.zone} hit={centerHit}");

                string fileName =
                    $"{captureSceneName}_{captureIndex + 1:D2}_{Sanitize(target.zone + "_" + target.name)}.png";
                string path = Path.GetFullPath(Path.Combine(CaptureRoot, fileName));
                CaptureCameraToPng(outputCamera, path);
            }

            Debug.Log($"STORY_CAMERA_CAPTURE_COMPLETE scene={captureSceneName} count={CaptureTargets.Count}");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (cameraStateCaptured && outputCamera != null)
            {
                outputCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                outputCamera.fieldOfView = originalFieldOfView;
                outputCamera.nearClipPlane = originalNearClip;
                outputCamera.farClipPlane = originalFarClip;
            }
            CancelCameraCapture();
        }
    }

    private static void CaptureCameraToPng(Camera camera, string path)
    {
        const int width = 540;
        const int height = 960;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture renderTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            renderTarget.Create();
            camera.targetTexture = renderTarget;
            RenderTexture.active = renderTarget;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
            Object.DestroyImmediate(texture);
        }
    }

    private static void CancelCameraCapture()
    {
        CaptureTargets.Clear();
        captureIndex = 0;
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace(' ', '_');
    }

    [MenuItem(Root + "Reveal Current Subtitle")]
    private static void RevealCurrentSubtitle()
    {
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>(FindObjectsInactive.Include);
        if (Application.isPlaying && ui != null && ui.SubtitleActive)
            ui.TryHandlePrimaryTap();
    }

    [MenuItem(Root + "Reveal Current Subtitle", true)]
    private static bool CanRevealSubtitle()
    {
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>(FindObjectsInactive.Include);
        return Application.isPlaying && ui != null && ui.SubtitleActive;
    }

    [MenuItem(Root + "Log Active Camera Rays")]
    private static void LogActiveCameraRays()
    {
        Camera camera = Camera.main;
        if (!Application.isPlaying || camera == null)
        {
            Debug.LogWarning("Camera ray QA requires a running Story scene.");
            return;
        }

        Vector2[] samples =
        {
            new(0.5f, 0.5f), new(0.25f, 0.5f), new(0.75f, 0.5f),
            new(0.5f, 0.25f), new(0.5f, 0.75f)
        };
        foreach (Vector2 sample in samples)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(sample.x, sample.y, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 120f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            string description = hits.Length == 0
                ? "<none>"
                : string.Join(" | ", System.Array.ConvertAll(hits,
                    hit => $"{HierarchyPath(hit.transform)}@{hit.distance:F2}"));
            Debug.Log($"STORY_CAMERA_RAY sample={sample.x:F2},{sample.y:F2} hits={description}");
        }
    }

    private static string HierarchyPath(Transform target)
    {
        if (target == null)
            return "<missing>";
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }

    private static void OpenScene(string path)
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        cameraIndex = -1;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("STORY_VISUAL_QA opened " + path);
    }

    private static void RuntimeLoadScene(string sceneName)
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Runtime Story scene load requires Play Mode.");
            return;
        }

        CancelCameraCapture();
        cameraIndex = -1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        Debug.Log("STORY_VISUAL_QA runtime loaded " + sceneName);
    }
}
