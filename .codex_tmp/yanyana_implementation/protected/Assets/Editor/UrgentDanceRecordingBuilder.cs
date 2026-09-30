#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only one-shot builder/recorder for the urgent Story 04 aid-point dance clip.
/// It creates a separate recording scene and never changes the gameplay scene.
/// </summary>
[InitializeOnLoad]
public static class UrgentDanceRecordingBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Story_04_RebuildPreview.unity";
    private const string RecordingScenePath = "Assets/Scenes/Story_04_DanceRecording.unity";
    private const string DanceModelPath = "Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx";
    private const string DanceClipName = "Armature|Dance_Loop";
    private const string DanceControllerPath = "Assets/Story/Generated/Animations/UrgentDanceLoop.controller";
    private const string RootDanceClipPath = "Assets/Story/Generated/Animations/UrgentRootSway.anim";
    private const string RecordingRootName = "URGENT_DANCE_RECORDING";
    private const string RecordingCameraName = "UrgentDanceCamera";
    private const string PendingKey = "UrgentDanceRecording.Pending";
    private const string OutputReadyKey = "UrgentDanceRecording.OutputReady";
    private const string MenuRoot = "Tools/Story/Urgent Dance/";

    private static RecorderController s_RecorderController;
    private static string s_OutputPath;

    private readonly struct CharacterSetup
    {
        public CharacterSetup(string name, string prefabPath, Vector3 position, float yaw, float speed)
        {
            Name = name;
            PrefabPath = prefabPath;
            Position = position;
            Yaw = yaw;
            Speed = speed;
        }

        public string Name { get; }
        public string PrefabPath { get; }
        public Vector3 Position { get; }
        public float Yaw { get; }
        public float Speed { get; }
    }

    private static readonly CharacterSetup[] Characters =
    {
        // One camera-aligned row keeps all nine dancers visible without silhouettes overlapping.
        new CharacterSetup("Dance_RescueWorker", "Assets/Story/Characters/MeshyResponders/Prefabs/RescueWorker.prefab", new Vector3(-3.435f, 0.02f, 44.329f), 153.4f, 1.04f),
        new CharacterSetup("Dance_Anne", "Assets/Story/Characters/MeshyFamily/Prefabs/Anne.prefab", new Vector3(-2.541f, 0.02f, 44.776f), 153.4f, 0.94f),
        new CharacterSetup("Dance_Deniz", "Assets/Story/Characters/MeshyFamily/Prefabs/Deniz.prefab", new Vector3(-1.647f, 0.02f, 45.223f), 153.4f, 1.06f),
        new CharacterSetup("Dance_Nermin", "Assets/Story/Characters/MeshyFamily/Prefabs/Komsu.prefab", new Vector3(-0.753f, 0.02f, 45.670f), 153.4f, 1.02f),
        new CharacterSetup("Dance_Apo", "Assets/Story/Characters/MeshyFamily/Prefabs/AbdullahEkinci.prefab", new Vector3(0.142f, 0.02f, 46.117f), 333.4f, 0.96f),
        new CharacterSetup("Dance_Can", "Assets/Story/Characters/MeshyFamily/Prefabs/Can.prefab", new Vector3(1.036f, 0.02f, 46.564f), 153.4f, 1.10f),
        new CharacterSetup("Dance_Baba", "Assets/Story/Characters/MeshyFamily/Prefabs/Baba.prefab", new Vector3(1.930f, 0.02f, 47.011f), 153.4f, 0.91f),
        new CharacterSetup("Dance_Firefighter", "Assets/Story/Characters/MeshyResponders/Prefabs/Firefighter.prefab", new Vector3(2.824f, 0.02f, 47.458f), 153.4f, 0.98f),
        new CharacterSetup("Dance_Police", "Assets/Story/Characters/MeshyResponders/Prefabs/Police.prefab", new Vector3(3.718f, 0.02f, 47.905f), 153.4f, 1.08f),
    };

    static UrgentDanceRecordingBuilder()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
            EditorApplication.delayCall += BeginRecorderInPlayMode;
    }

    public static string FinalOutputPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", "ClientExports", "Story04_AidPoint_Dance_5s.mp4"));

    [MenuItem(MenuRoot + "1 - Build Recording Scene")]
    public static void BuildRecordingScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building the recording scene.");

        if (!File.Exists(Path.GetFullPath(SourceScenePath)))
            throw new FileNotFoundException("Story 04 source scene is missing.", SourceScenePath);

        Scene current = SceneManager.GetActiveScene();
        if (current.path != SourceScenePath)
            current = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RecordingScenePath) != null)
            AssetDatabase.DeleteAsset(RecordingScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, RecordingScenePath))
            throw new InvalidOperationException("Could not create the recording scene copy.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Scene recordingScene = EditorSceneManager.OpenScene(RecordingScenePath, OpenSceneMode.Single);

        GameObject existingRoot = GameObject.Find(RecordingRootName);
        if (existingRoot != null)
            UnityEngine.Object.DestroyImmediate(existingRoot);

        AnimatorController danceController = CreateDanceController();
        AnimationClip rootDanceClip = CreateRootDanceClip();
        var recordingRoot = new GameObject(RecordingRootName);
        recordingRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(recordingRoot, recordingScene);

        int animatorCount = 0;
        foreach (CharacterSetup setup in Characters)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(setup.PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Character prefab is missing: {setup.PrefabPath}");

            var slot = new GameObject(setup.Name);
            slot.transform.SetParent(recordingRoot.transform, true);
            slot.transform.SetPositionAndRotation(setup.Position, Quaternion.Euler(0f, setup.Yaw, 0f));

            var danceMotion = new GameObject("DanceMotion");
            danceMotion.transform.SetParent(slot.transform, false);
            Animation rootAnimation = danceMotion.AddComponent<Animation>();
            rootAnimation.AddClip(rootDanceClip, rootDanceClip.name);
            rootAnimation.clip = rootDanceClip;
            rootAnimation.playAutomatically = true;
            rootAnimation.wrapMode = WrapMode.Loop;

            var instance = PrefabUtility.InstantiatePrefab(prefab, recordingScene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate: {setup.PrefabPath}");

            instance.name = "Model";
            instance.transform.SetParent(danceMotion.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                throw new InvalidOperationException($"{setup.Name} has no valid Humanoid Animator/Avatar.");

            RestoreOriginalFamilyMesh(instance, setup.Name);
            animator.gameObject.SetActive(true);
            animator.applyRootMotion = false;
            animator.speed = setup.Speed;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            bool supportsHumanoidDance = true;
            if (supportsHumanoidDance)
            {
                animator.runtimeAnimatorController = danceController;
                animator.enabled = true;
                animatorCount++;
            }
            else
            {
                animator.runtimeAnimatorController = null;
                animator.enabled = false;
            }

            foreach (SkinnedMeshRenderer skinnedRenderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinnedRenderer.enabled = true;
                skinnedRenderer.updateWhenOffscreen = true;
                skinnedRenderer.forceMatrixRecalculationPerRender = true;
            }
        }

        DisableStoryRuntime(recordingScene);
        CreateRecordingCamera(recordingRoot, recordingScene);

        EditorSceneManager.MarkSceneDirty(recordingScene);
        EditorSceneManager.SaveScene(recordingScene);
        AssetDatabase.SaveAssets();
        SessionState.SetBool(OutputReadyKey, false);

        Selection.activeGameObject = recordingRoot;
        Debug.Log($"[UrgentDance] Recording scene built with {Characters.Length} dancing characters " +
                  $"({animatorCount} full-body Humanoid + {Characters.Length - animatorCount} scene-authored sway): {RecordingScenePath}");
    }

    [MenuItem(MenuRoot + "2 - Record 5 Seconds")]
    public static void RecordFiveSeconds()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("A Play Mode transition is already in progress.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != RecordingScenePath || GameObject.Find(RecordingRootName) == null)
            BuildRecordingScene();

        Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath) ?? string.Empty);
        if (File.Exists(FinalOutputPath))
            File.Delete(FinalOutputPath);

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(OutputReadyKey, false);
        Debug.Log($"[UrgentDance] Entering Play Mode for an exact 150-frame recording: {FinalOutputPath}");
        EditorApplication.isPlaying = true;
    }

    [MenuItem(MenuRoot + "Build And Record Now")]
    public static void BuildAndRecordNow()
    {
        BuildRecordingScene();
        RecordFiveSeconds();
    }

    private static AnimatorController CreateDanceController()
    {
        AnimationClip danceClip = AssetDatabase.LoadAllAssetsAtPath(DanceModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == DanceClipName);

        if (danceClip == null || !danceClip.isHumanMotion)
            throw new InvalidOperationException($"Humanoid dance clip '{DanceClipName}' was not found in {DanceModelPath}.");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(DanceControllerPath) != null)
            AssetDatabase.DeleteAsset(DanceControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(DanceControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState danceState = stateMachine.AddState("Dance");
        danceState.motion = danceClip;
        danceState.speed = 1f;
        stateMachine.defaultState = danceState;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip CreateRootDanceClip()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(RootDanceClipPath) != null)
            AssetDatabase.DeleteAsset(RootDanceClipPath);

        var clip = new AnimationClip
        {
            name = "UrgentRootSway",
            legacy = true,
            frameRate = 30f,
            wrapMode = WrapMode.Loop
        };

        AnimationCurve x = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, -0.08f),
            new Keyframe(0.5f, 0f), new Keyframe(0.75f, 0.08f), new Keyframe(1f, 0f));
        AnimationCurve y = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, 0.10f),
            new Keyframe(0.5f, 0f), new Keyframe(0.75f, 0.10f), new Keyframe(1f, 0f));
        AnimationCurve rotationZ = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, -0.052336f),
            new Keyframe(0.5f, 0f), new Keyframe(0.75f, 0.052336f), new Keyframe(1f, 0f));
        AnimationCurve rotationW = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.25f, 0.99863f),
            new Keyframe(0.5f, 1f), new Keyframe(0.75f, 0.99863f), new Keyframe(1f, 1f));

        foreach (AnimationCurve curve in new[] { x, y, rotationZ, rotationW })
        {
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;
        }

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), x);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), y);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.x"), AnimationCurve.Constant(0f, 1f, 0f));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.y"), AnimationCurve.Constant(0f, 1f, 0f));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.z"), rotationZ);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.w"), rotationW);
        clip.EnsureQuaternionContinuity();

        AssetDatabase.CreateAsset(clip, RootDanceClipPath);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static void RestoreOriginalFamilyMesh(GameObject instance, string setupName)
    {
        string familyName = setupName switch
        {
            "Dance_Deniz" => "Deniz",
            "Dance_Can" => "Can",
            "Dance_Anne" => "Anne",
            "Dance_Nermin" => "Komsu",
            "Dance_Baba" => "Baba",
            _ => null
        };

        if (familyName == null)
            return;

        string modelPath = $"Assets/Story/Characters/MeshyFamily/{familyName}/{familyName}_Rigged.fbx";
        Mesh originalMesh = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Mesh>()
            .FirstOrDefault(mesh => mesh.name == familyName) ??
            AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Mesh>().FirstOrDefault();
        SkinnedMeshRenderer renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (originalMesh == null || renderer == null)
            throw new InvalidOperationException($"Could not restore the original recording mesh from {modelPath}.");

        renderer.sharedMesh = originalMesh;
        renderer.localBounds = originalMesh.bounds;
        if (familyName != "Deniz")
            instance.transform.localScale = Vector3.one * 0.01f;
    }

    private static void DisableStoryRuntime(Scene recordingScene)
    {
        SetSceneObjectActive(recordingScene, "_StorySession_RebuildPreview", false);
        SetSceneObjectActive(recordingScene, "_StoryEvacuationRebuildCore", false);
        SetSceneObjectActive(recordingScene, "StoryUI_Evacuation_Rebuild", false);
        SetSceneObjectActive(recordingScene, "StoryCameras", false);

        foreach (string originalCharacter in new[]
                 {
                     "Deniz_12", "Can_8", "Nermin_Neighbor_Landing", "Nermin_Neighbor_Street",
                     "Nermin_Neighbor_Assembly", "Anne_Assembly_Reunion", "Baba_Assembly_Reunion",
                     "AssemblyWorker", "AssemblyPolice", "EmergencyFirefighter"
                 })
        {
            SetSceneObjectActive(recordingScene, originalCharacter, false);
        }
    }

    private static void SetSceneObjectActive(Scene scene, string objectName, bool active)
    {
        GameObject target = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(item => item.gameObject)
            .FirstOrDefault(item => item.name == objectName);
        if (target != null)
            target.SetActive(active);
    }

    private static void CreateRecordingCamera(GameObject recordingRoot, Scene recordingScene)
    {
        foreach (Camera existingCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existingCamera.gameObject.scene == recordingScene)
                existingCamera.enabled = false;
        }

        var cameraObject = new GameObject(RecordingCameraName);
        SceneManager.MoveGameObjectToScene(cameraObject, recordingScene);
        cameraObject.transform.SetParent(recordingRoot.transform, true);

        // Match the authored CM04R_AssemblyReunion shot, which has a clean view of the aid station.
        Vector3 cameraPosition = new Vector3(2.6f, 3f, 41.2f);
        cameraObject.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.Euler(13.7087f, 333.4349f, 0f));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = true;
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.depth = 100f;
        camera.allowHDR = true;
        camera.allowMSAA = true;

        UniversalAdditionalCameraData additionalData = camera.GetUniversalAdditionalCameraData();
        additionalData.renderPostProcessing = true;
        additionalData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            EditorApplication.delayCall += BeginRecorderInPlayMode;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            s_RecorderController?.StopRecording();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            bool exists = File.Exists(FinalOutputPath);
            SessionState.SetBool(OutputReadyKey, exists);
            if (exists)
                Debug.Log($"[UrgentDance] Recording complete: {FinalOutputPath}");
        }
    }

    private static void BeginRecorderInPlayMode()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        s_OutputPath = FinalOutputPath;

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.FrameRate = 30f;
        controllerSettings.CapFrameRate = true;
        controllerSettings.ExitPlayMode = true;
        controllerSettings.SetRecordModeToFrameInterval(0, 149);

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "Story04 Aid Point Dance 5s";
        movieSettings.Enabled = true;
        movieSettings.CaptureAudio = false;
        movieSettings.CaptureAlpha = false;
        movieSettings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movieSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };
        movieSettings.OutputFile = Path.ChangeExtension(s_OutputPath, null).Replace('\\', '/');

        controllerSettings.AddRecorderSettings(movieSettings);
        RecorderOptions.VerboseMode = false;
        s_RecorderController = new RecorderController(controllerSettings);
        s_RecorderController.PrepareRecording();
        if (!s_RecorderController.StartRecording())
            throw new InvalidOperationException("Unity Recorder could not start the MP4 recording.");

        Debug.Log($"[UrgentDance] Recording 150 frames at 30 FPS: {s_OutputPath}");
    }
}

#endif
