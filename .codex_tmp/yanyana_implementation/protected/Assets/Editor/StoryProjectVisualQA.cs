using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;

internal static class StoryProjectVisualQA
{
    private const string Root = "Tools/Deprem Story/QA/Project Visual/";
    private const string CaptureRoot = "Temp/StoryCameraQA";
    private const string HudCaptureRoot = "Temp/StoryHudQA";
    private static int cameraIndex = -1;
    private static readonly List<(StoryCameraZoneId zone, string name, CinemachineCamera camera)> CaptureTargets = new();
    private static int captureIndex;
    private static string captureSceneName;

    [MenuItem(Root + "Open Story01")]
    private static void OpenStory01() => OpenScene("Assets/Scenes/Story_01_RebuildPreview.unity");

    [MenuItem(Root + "Open Story02")]
    private static void OpenStory02() => OpenScene("Assets/Scenes/Story_02_RebuildPreview.unity");

    [MenuItem(Root + "Open Story03")]
    private static void OpenStory03() => OpenScene("Assets/Scenes/Story_03_RebuildPreview.unity");

    [MenuItem(Root + "Open Story04")]
    private static void OpenStory04() => OpenScene("Assets/Scenes/Story_04_RebuildPreview.unity");

    [MenuItem(Root + "Runtime Load Story01")]
    private static void RuntimeLoadStory01() => RuntimeLoadScene("Story_01_RebuildPreview");

    [MenuItem(Root + "Runtime Load Story02")]
    private static void RuntimeLoadStory02() => RuntimeLoadScene("Story_02_RebuildPreview");

    [MenuItem(Root + "Runtime Load Story03")]
    private static void RuntimeLoadStory03() => RuntimeLoadScene("Story_03_RebuildPreview");

    [MenuItem(Root + "Runtime Load Story04")]
    private static void RuntimeLoadStory04() => RuntimeLoadScene("Story_04_RebuildPreview");

    [MenuItem(Root + "Legacy/Open Story01 Reference")]
    private static void OpenLegacyStory01() => OpenScene("Assets/Scenes/Story_01_BagPreparation.unity");

    [MenuItem(Root + "Legacy/Open Story02 Reference")]
    private static void OpenLegacyStory02() => OpenScene("Assets/Scenes/Story_02_HomeSafety.unity");

    [MenuItem(Root + "Legacy/Open Story03 Reference")]
    private static void OpenLegacyStory03() => OpenScene("Assets/Scenes/Story_03_Quake.unity");

    [MenuItem(Root + "Legacy/Open Story04 Reference")]
    private static void OpenLegacyStory04() => OpenScene("Assets/Scenes/Story_04_Evacuation.unity");

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
        if (controller == null)
        {
            Debug.LogWarning("Camera capture QA için aktif Story sahnesinde bir StoryCameraController bulunmalı.");
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
        return Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include) != null;
    }

    [MenuItem(Root + "Capture Current HUD")]
    private static void CaptureCurrentHud()
    {
        StoryUIController ui =
            Object.FindFirstObjectByType<StoryUIController>(FindObjectsInactive.Include);
        Canvas canvas = ui != null
            ? ui.GetComponent<Canvas>()
            : Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (canvas == null || camera == null)
        {
            Debug.LogError("HUD capture icin aktif Story sahnesinde Canvas ve Camera bulunmali.");
            return;
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string outputRoot = Path.GetFullPath(HudCaptureRoot);
        Directory.CreateDirectory(outputRoot);
        CaptureHudToPng(canvas, camera, Path.Combine(outputRoot, sceneName + "_9x16.png"), 540, 960);
        CaptureHudToPng(canvas, camera, Path.Combine(outputRoot, sceneName + "_9x19_5.png"), 540, 1170);
        Debug.Log($"STORY_HUD_CAPTURE_COMPLETE scene={sceneName} root={outputRoot}");
    }

    [MenuItem(Root + "Capture Current HUD", true)]
    private static bool CanCaptureCurrentHud()
    {
        StoryUIController ui =
            Object.FindFirstObjectByType<StoryUIController>(FindObjectsInactive.Include);
        Canvas canvas = ui != null
            ? ui.GetComponent<Canvas>()
            : Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        return canvas != null &&
               Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include) != null;
    }

    [MenuItem(Root + "Capture Story01 Blackout Search")]
    private static void CaptureStory01BlackoutSearch()
    {
        CinemachineCamera[] sceneCameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        string[] cameraNames =
        {
            "CM_PreparationFamilyPlan_Rebuild",
            "CM_PreparationBag_Rebuild",
            "CM_PreparationWarmth_Rebuild"
        };
        CinemachineCamera[] views = cameraNames
            .Select(name => sceneCameras.FirstOrDefault(candidate => candidate.name == name))
            .ToArray();
        Camera outputCamera = Camera.main;
        if (views.Any(view => view == null) || outputCamera == null)
        {
            Debug.LogWarning("Story01 karanlık QA için Story_01_RebuildPreview sahnesini aç.");
            return;
        }

        string[] beamNames =
        {
            "BlackoutBeam_FamilyPlan",
            "BlackoutBeam_SafeTable",
            "BlackoutBeam_Can"
        };
        GameObject[] beams = beamNames.Select(FindSceneObject).ToArray();
        if (beams.Any(beam => beam == null))
        {
            Debug.LogError("Story01 karanlık QA fener doğrultularını bulamadı.");
            return;
        }

        Light[] sceneLights = Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Dictionary<Light, float> originalIntensities = sceneLights.ToDictionary(light => light, light => light.intensity);
        Dictionary<GameObject, bool> originalBeamStates = beams.ToDictionary(beam => beam, beam => beam.activeSelf);
        Vector3 originalPosition = outputCamera.transform.position;
        Quaternion originalRotation = outputCamera.transform.rotation;
        float originalFieldOfView = outputCamera.fieldOfView;
        float originalNearClip = outputCamera.nearClipPlane;
        float originalFarClip = outputCamera.farClipPlane;

        try
        {
            Directory.CreateDirectory(Path.GetFullPath(CaptureRoot));
            foreach (Light light in sceneLights)
            {
                if (!light.name.StartsWith("BlackoutBeam_"))
                    light.intensity *= 0.045f;
            }

            for (int index = 0; index < beams.Length; index++)
            {
                foreach (GameObject beam in beams)
                    beam.SetActive(false);
                beams[index].SetActive(true);
                outputCamera.transform.SetPositionAndRotation(
                    views[index].transform.position,
                    views[index].transform.rotation);
                outputCamera.fieldOfView = views[index].Lens.FieldOfView;
                outputCamera.nearClipPlane = views[index].Lens.NearClipPlane;
                outputCamera.farClipPlane = views[index].Lens.FarClipPlane;

                string path = Path.GetFullPath(Path.Combine(
                    CaptureRoot,
                    $"Story_01_RebuildPreview_Blackout_{index + 1:D2}_{beamNames[index]}.png"));
                CaptureCameraToPng(outputCamera, path);
            }

            Debug.Log($"STORY_BLACKOUT_CAPTURE_COMPLETE count={beams.Length} root={Path.GetFullPath(CaptureRoot)}");
        }
        finally
        {
            foreach ((Light light, float intensity) in originalIntensities)
            {
                if (light != null)
                    light.intensity = intensity;
            }
            foreach ((GameObject beam, bool active) in originalBeamStates)
            {
                if (beam != null)
                    beam.SetActive(active);
            }
            outputCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            outputCamera.fieldOfView = originalFieldOfView;
            outputCamera.nearClipPlane = originalNearClip;
            outputCamera.farClipPlane = originalFarClip;
        }
    }

    [MenuItem(Root + "Capture Story01 Blackout Search", true)]
    private static bool CanCaptureStory01BlackoutSearch()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Story_01_RebuildPreview";
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

                GameObject scenarioVisual = ResolveScenarioVisual(target.zone);
                bool scenarioVisualWasActive = scenarioVisual != null && scenarioVisual.activeSelf;
                try
                {
                    if (scenarioVisual != null)
                        scenarioVisual.SetActive(true);

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
                    string tallFileName =
                        $"{captureSceneName}_{captureIndex + 1:D2}_{Sanitize(target.zone + "_" + target.name)}_Tall9x19_5.png";
                    string tallPath = Path.GetFullPath(Path.Combine(CaptureRoot, tallFileName));
                    CaptureCameraToPng(outputCamera, tallPath, 540, 1170);
                }
                finally
                {
                    if (scenarioVisual != null)
                        scenarioVisual.SetActive(scenarioVisualWasActive);
                }
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

    private static GameObject ResolveScenarioVisual(StoryCameraZoneId zone)
    {
        if (zone != StoryCameraZoneId.InspectBrokenGlass)
            return null;

        Transform[] transforms =
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == "BrokenGlass_Hazard")
                return candidate.gameObject;
        }

        return null;
    }

    private static GameObject FindSceneObject(string name)
    {
        Transform[] transforms =
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == name)
                return candidate.gameObject;
        }

        return null;
    }

    private static void CaptureCameraToPng(Camera camera, string path, int width = 540, int height = 960)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        RenderTexture renderTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            renderTarget.Create();
            camera.targetTexture = renderTarget;
            camera.aspect = (float)width / height;
            RenderTexture.active = renderTarget;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
            Object.DestroyImmediate(texture);
        }
    }

    private static void CaptureHudToPng(Canvas canvas, Camera camera, string path, int width, int height)
    {
        RenderMode previousMode = canvas.renderMode;
        Camera previousCanvasCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        RenderTexture renderTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = Mathf.Max(1f, camera.nearClipPlane + 0.5f);
            camera.targetTexture = renderTarget;
            camera.aspect = (float)width / height;
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                text.SetAllDirty();
                text.ForceMeshUpdate();
            }

            Canvas.ForceUpdateCanvases();
            renderTarget.Create();
            RenderTexture.active = renderTarget;
            GL.Clear(true, true, camera.backgroundColor);
            camera.Render();
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCanvasCamera;
            canvas.planeDistance = previousPlaneDistance;
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
