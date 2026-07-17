using Deprem.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;

internal static class StoryProjectVisualQA
{
    private const string Root = "Tools/Deprem Story/QA/Project Visual/";
    private static int cameraIndex = -1;

    [MenuItem(Root + "Open Story01")]
    private static void OpenStory01() => OpenScene("Assets/Scenes/Story_01_BagPreparation.unity");

    [MenuItem(Root + "Open Story02")]
    private static void OpenStory02() => OpenScene("Assets/Scenes/Story_02_HomeSafety.unity");

    [MenuItem(Root + "Open Story03")]
    private static void OpenStory03() => OpenScene("Assets/Scenes/Story_03_Quake.unity");

    [MenuItem(Root + "Open Story04")]
    private static void OpenStory04() => OpenScene("Assets/Scenes/Story_04_Evacuation.unity");

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

    private static void OpenScene(string path)
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        cameraIndex = -1;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("STORY_VISUAL_QA opened " + path);
    }
}
