using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal static class StoryChapterBuilderCommon
{
    internal const string GeneratedRoot = "Assets/Story/Generated";
    internal const string MaterialRoot = GeneratedRoot + "/Materials";
    internal const string AnimationRoot = GeneratedRoot + "/Animations/Chapters";
    internal const string AudioRoot = GeneratedRoot + "/Audio";
    internal const string StoryPrefabRoot = "Assets/Story/Prefabs";
    internal const string FurnitureRoot = "Assets/ithappy/Cute_Furniture_Free/Prefabs";

    internal static readonly Color Navy = new Color32(15, 30, 46, 255);
    internal static readonly Color Teal = new Color32(25, 151, 151, 255);
    internal static readonly Color Amber = new Color32(244, 173, 65, 255);
    internal static readonly Color Coral = new Color32(224, 91, 82, 255);
    internal static readonly Color Cream = new Color32(236, 226, 204, 255);
    internal static readonly Color Wall = new Color32(191, 198, 188, 255);
    internal static readonly Color Floor = new Color32(102, 78, 63, 255);
    internal static readonly Color Concrete = new Color32(91, 99, 105, 255);
    internal static readonly Color Asphalt = new Color32(45, 51, 58, 255);
    internal static readonly Color Grass = new Color32(87, 122, 82, 255);

    internal sealed class Materials
    {
        internal Material wall;
        internal Material floor;
        internal Material concrete;
        internal Material asphalt;
        internal Material grass;
        internal Material cream;
        internal Material navy;
        internal Material teal;
        internal Material amber;
        internal Material coral;
        internal Material wood;
        internal Material metal;
        internal Material glass;
        internal Material dust;
        internal Material dark;
        internal Material white;
    }

    internal sealed class Characters
    {
        internal GameObject deniz;
        internal GameObject can;
        internal GameObject parent;
        internal Animator denizAnimator;
        internal Animator canAnimator;
        internal Animator parentAnimator;
    }

    internal sealed class ChapterUI
    {
        internal StoryUIController controller;
        internal GameObject completionPanel;
        internal TMP_Text completionDetail;
    }

    internal struct CameraSpec
    {
        internal StoryCameraZoneId zone;
        internal string name;
        internal Vector3 position;
        internal Vector3 target;
        internal float fieldOfView;
        internal bool impulse;
        internal Transform follow;
        internal float followDistance;
        internal Vector2 screenPosition;

        internal CameraSpec(StoryCameraZoneId zone, string name, Vector3 position, Vector3 target, float fieldOfView,
            bool impulse = false, Transform follow = null, float followDistance = 16f, Vector2 screenPosition = default)
        {
            this.zone = zone;
            this.name = name;
            this.position = position;
            this.target = target;
            this.fieldOfView = fieldOfView;
            this.impulse = impulse;
            this.follow = follow;
            this.followDistance = followDistance;
            this.screenPosition = screenPosition == default ? new Vector2(0f, 0.16f) : screenPosition;
        }
    }

    internal static void EnsureFolders()
    {
        EnsureFolder("Assets/Story");
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(GeneratedRoot + "/Animations");
        EnsureFolder(AnimationRoot);
        EnsureFolder("Assets/Scenes/LegacyBackups");
    }

    internal static void EnsureLegacyBackup(string source, string backup)
    {
        if (File.Exists(backup))
            return;
        if (!File.Exists(source))
            throw new FileNotFoundException("Korunacak eski sahne bulunamadı.", source);

        File.Copy(source, backup);
        AssetDatabase.ImportAsset(backup, ImportAssetOptions.ForceSynchronousImport);
    }

    internal static Materials CreateMaterials()
    {
        EnsureFolders();
        return new Materials
        {
            wall = GetOrCreateMaterial("Chapter_Wall", Wall, 0.14f),
            floor = GetOrCreateMaterial("Chapter_WarmFloor", Floor, 0.26f),
            concrete = GetOrCreateMaterial("Chapter_Concrete", Concrete, 0.12f),
            asphalt = GetOrCreateMaterial("Chapter_Asphalt", Asphalt, 0.08f),
            grass = GetOrCreateMaterial("Chapter_Grass", Grass, 0.1f),
            cream = GetOrCreateMaterial("Cream", Cream, 0.22f),
            navy = GetOrCreateMaterial("Navy", Navy, 0.2f),
            teal = GetOrCreateMaterial("Teal", Teal, 0.24f),
            amber = GetOrCreateMaterial("Amber", Amber, 0.22f),
            coral = GetOrCreateMaterial("Coral", Coral, 0.2f),
            wood = GetOrCreateMaterial("Wood", new Color32(124, 83, 60, 255), 0.28f),
            metal = GetOrCreateMaterial("Chapter_Metal", new Color32(93, 112, 123, 255), 0.48f),
            glass = GetOrCreateMaterial("WindowGlass", new Color32(75, 126, 147, 255), 0.72f, true,
                new Color(0.02f, 0.08f, 0.12f)),
            dust = GetOrCreateMaterial("Chapter_Dust", new Color32(171, 151, 118, 190), 0.02f),
            dark = GetOrCreateMaterial("Chapter_Dark", new Color32(29, 34, 39, 255), 0.12f),
            white = GetOrCreateMaterial("Chapter_White", new Color32(227, 230, 224, 255), 0.18f)
        };
    }

    internal static VolumeProfile CreateVolumeProfile()
    {
        const string path = GeneratedRoot + "/Story_Chapters_Volume.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.12f);
        bloom.scatter.Override(0.58f);

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(0.05f);
        color.contrast.Override(7f);
        color.saturation.Override(-3f);

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.17f);
        vignette.smoothness.Override(0.54f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    internal static Characters BuildFamily(Transform parent, RuntimeAnimatorController controller, bool includeParent,
        Vector3 denizPosition, Vector3 canPosition, Vector3 parentPosition,
        RuntimeAnimatorController adultController = null)
    {
        Characters result = new Characters
        {
            deniz = InstantiateCharacter(StoryPrefabRoot + "/Deniz_12.prefab", "Deniz_12", parent, denizPosition, 1.5f, controller),
            can = InstantiateCharacter(StoryPrefabRoot + "/Can_8.prefab", "Can_8", parent, canPosition, 1.26f, controller)
        };
        result.denizAnimator = result.deniz.GetComponentInChildren<Animator>(true);
        result.canAnimator = result.can.GetComponentInChildren<Animator>(true);

        if (includeParent)
        {
            result.parent = InstantiateCharacter(StoryPrefabRoot + "/Preparation/Anne_Ayse.prefab", "Anne_Ayse", parent,
                parentPosition, 1.7f, adultController != null ? adultController : controller);
            result.parentAnimator = result.parent.GetComponentInChildren<Animator>(true);
        }
        return result;
    }

    internal static GameObject InstantiateCharacter(string path, string name, Transform parent, Vector3 feetPosition,
        float targetHeight, RuntimeAnimatorController controller)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Karakter prefabı bulunamadı: " + path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localScale = Vector3.one;
        // Reusable character prefabs were captured from earlier story scenes. Their visual/rig data is shared,
        // but chapter-specific hotspot GameObjects (including their trigger colliders) must never leak into
        // another scene. Leaving only the component behind was enough to create invisible input blockers.
        StoryInteractable[] staleInteractions = instance.GetComponentsInChildren<StoryInteractable>(true);
        foreach (StoryInteractable staleInteraction in staleInteractions)
        {
            if (staleInteraction == null)
                continue;
            if (staleInteraction.gameObject == instance)
                Object.DestroyImmediate(staleInteraction);
            else
            {
                foreach (DraggableItem draggable in staleInteraction.GetComponents<DraggableItem>())
                    Object.DestroyImmediate(draggable);
                foreach (StoryPreparationItem item in staleInteraction.GetComponents<StoryPreparationItem>())
                    Object.DestroyImmediate(item);
                Object.DestroyImmediate(staleInteraction.gameObject);
            }
        }
        StoryPreparationItem[] stalePreparationItems = instance.GetComponentsInChildren<StoryPreparationItem>(true);
        foreach (StoryPreparationItem stalePreparationItem in stalePreparationItems)
        {
            if (stalePreparationItem == null)
                continue;
            if (stalePreparationItem.gameObject == instance)
                Object.DestroyImmediate(stalePreparationItem);
            else
                Object.DestroyImmediate(stalePreparationItem.gameObject);
        }
        FitToHeight(instance, feetPosition, targetHeight);
        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        return instance;
    }

    internal static StoryPlayerMovement ConfigurePlayer(GameObject deniz)
    {
        NavMeshAgent agent = deniz.GetComponent<NavMeshAgent>() ?? deniz.AddComponent<NavMeshAgent>();
        agent.speed = 1.75f;
        agent.acceleration = 9f;
        agent.angularSpeed = 540f;
        agent.radius = 0.24f;
        agent.height = 1.48f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 0.13f;

        CapsuleCollider capsule = deniz.GetComponent<CapsuleCollider>() ?? deniz.AddComponent<CapsuleCollider>();
        capsule.radius = 0.24f;
        capsule.height = 1.46f;
        capsule.center = new Vector3(0f, 0.73f, 0f);
        Rigidbody body = deniz.GetComponent<Rigidbody>() ?? deniz.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        StoryPlayerMovement movement = deniz.GetComponent<StoryPlayerMovement>() ?? deniz.AddComponent<StoryPlayerMovement>();
        SetReference(movement, "animator", deniz.GetComponentInChildren<Animator>(true));
        return movement;
    }

    internal static StorySiblingFollower ConfigureSibling(GameObject can, Transform target)
    {
        NavMeshAgent agent = can.GetComponent<NavMeshAgent>() ?? can.AddComponent<NavMeshAgent>();
        agent.speed = 1.65f;
        agent.acceleration = 8f;
        agent.angularSpeed = 520f;
        agent.radius = 0.21f;
        agent.height = 1.24f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 1.05f;
        StorySiblingFollower follower = can.GetComponent<StorySiblingFollower>() ?? can.AddComponent<StorySiblingFollower>();
        SetReference(follower, "target", target);
        SetReference(follower, "animator", can.GetComponentInChildren<Animator>(true));
        return follower;
    }

    internal static StoryCameraController BuildCameras(Transform parent, StoryCameraZoneId initialZone, CameraSpec[] specs,
        out Camera mainCamera, out CinemachineBrain brain)
    {
        Transform cameraRoot = NewChild(parent, "StoryCameras");
        GameObject main = new GameObject("Main Camera");
        main.transform.SetParent(cameraRoot);
        main.tag = "MainCamera";
        main.transform.position = specs[0].position;
        main.transform.rotation = LookAt(specs[0].position, specs[0].target);
        mainCamera = main.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(19, 25, 31, 255);
        mainCamera.nearClipPlane = 0.08f;
        mainCamera.farClipPlane = 180f;
        mainCamera.allowHDR = true;
        main.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = main.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        brain = main.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.72f);

        StoryCameraBinding[] bindings = new StoryCameraBinding[specs.Length];
        for (int i = 0; i < specs.Length; i++)
        {
            CameraSpec spec = specs[i];
            GameObject cameraObject = new GameObject(spec.name);
            cameraObject.transform.SetParent(cameraRoot);
            cameraObject.transform.position = spec.position;
            cameraObject.transform.rotation = LookAt(spec.position, spec.target);
            CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            LensSettings lens = LensSettings.Default;
            lens.FieldOfView = Mathf.Clamp(spec.fieldOfView, 38f, 50f);
            lens.NearClipPlane = 0.08f;
            lens.FarClipPlane = 180f;
            virtualCamera.Lens = lens;
            virtualCamera.Priority = 0;

            if (spec.follow != null)
            {
                virtualCamera.Follow = spec.follow;
                CinemachinePositionComposer composer = cameraObject.AddComponent<CinemachinePositionComposer>();
                composer.CameraDistance = spec.followDistance;
                composer.TargetOffset = new Vector3(0f, 0.82f, 0f);
                composer.Damping = new Vector3(0.36f, 0.28f, 0.48f);
                composer.DeadZoneDepth = 0.4f;
                composer.CenterOnActivate = false;
                composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.2f, Smoothing = 8f, IgnoreY = true };
                ScreenComposerSettings composition = ScreenComposerSettings.Default;
                composition.ScreenPosition = spec.screenPosition;
                composition.DeadZone.Enabled = true;
                composition.DeadZone.Size = new Vector2(0.1f, 0.08f);
                composition.HardLimits.Enabled = true;
                composition.HardLimits.Size = new Vector2(0.72f, 0.64f);
                composer.Composition = composition;
            }

            if (spec.impulse)
            {
                CinemachineImpulseListener listener = cameraObject.AddComponent<CinemachineImpulseListener>();
                listener.ChannelMask = 1;
                listener.Gain = 0.12f;
            }
            bindings[i] = new StoryCameraBinding { zone = spec.zone, camera = virtualCamera };
        }

        GameObject controllerObject = new GameObject("MissionCameraController");
        controllerObject.transform.SetParent(cameraRoot);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        serialized.FindProperty("initialZone").intValue = (int)initialZone;
        SerializedProperty cameraArray = serialized.FindProperty("cameras");
        cameraArray.arraySize = bindings.Length;
        for (int i = 0; i < bindings.Length; i++)
        {
            SerializedProperty element = cameraArray.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("zone").intValue = (int)bindings[i].zone;
            element.FindPropertyRelative("camera").objectReferenceValue = bindings[i].camera;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    internal static ChapterUI BuildUI(Transform parent, StoryCameraController cameraController, string canvasName,
        string completionEyebrow, string completionTitle, string completionSafety)
    {
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Regular SDF.asset");
        TMP_FontAsset semibold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-SemiBold SDF.asset");
        TMP_FontAsset bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Bold SDF.asset");
        if (regular == null || semibold == null || bold == null)
            throw new InvalidOperationException("Inter TMP fontları bulunamadı.");

        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = CreateUIRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();
        GameObject objective = CreatePanel("ObjectiveStrip", safeArea.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-42f, -112f), new Vector2(850f, 178f), new Color(0.035f, 0.075f, 0.105f, 0.94f), false);
        TMP_Text objectiveTitle = CreateText("ObjectiveTitle", objective.transform, bold, 27f, Amber,
            TextAlignmentOptions.Left, new Vector2(0f, 39f), new Vector2(760f, 40f));
        TMP_Text objectiveDetail = CreateText("ObjectiveDetail", objective.transform, regular, 23f, Color.white,
            TextAlignmentOptions.Left, new Vector2(0f, -35f), new Vector2(760f, 92f));
        Button pause = CreateButton("PauseButton", safeArea.transform, "II", bold, new Vector2(1f, 1f),
            new Vector2(-35f, -61f), new Vector2(92f, 92f), Navy, Cream);

        GameObject contextPanel = CreatePanel("ContextPanel", safeArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 412f), new Vector2(820f, 82f), new Color(0.03f, 0.07f, 0.09f, 0.9f), false);
        TMP_Text context = CreateText("ContextText", contextPanel.transform, semibold, 22f, Amber,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(760f, 58f));
        contextPanel.SetActive(false);

        GameObject subtitlePanel = CreatePanel("SubtitlePanel", safeArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 214f), new Vector2(940f, 206f), new Color(0.015f, 0.03f, 0.045f, 0.93f), false);
        TMP_Text subtitle = CreateText("SubtitleText", subtitlePanel.transform, regular, 28f, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(870f, 166f));
        subtitlePanel.SetActive(false);

        GameObject pauseOverlay = CreatePanel("PauseOverlay", safeArea.transform, Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, new Color(0.004f, 0.01f, 0.016f, 0.76f), true);
        GameObject pauseCard = CreatePanel("PausePanel", pauseOverlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(820f, 690f), new Color(0.035f, 0.07f, 0.1f, 0.99f), true);
        CreateText("PauseTitle", pauseCard.transform, bold, 44f, Cream, TextAlignmentOptions.Center,
            new Vector2(0f, 245f), new Vector2(690f, 70f)).text = "OYUN DURAKLATILDI";
        CreateText("PauseHint", pauseCard.transform, regular, 23f, new Color(0.77f, 0.83f, 0.85f),
            TextAlignmentOptions.Center, new Vector2(0f, 165f), new Vector2(670f, 75f)).text =
            "Hikâye durumu en son kontrol noktasında yerel olarak saklanır.";
        Button resume = CreateButton("ResumeButton", pauseCard.transform, "DEVAM ET", bold, Vector2.one * 0.5f,
            new Vector2(0f, 35f), new Vector2(560f, 104f), Teal, Color.white);
        Button retry = CreateButton("RetryCheckpointButton", pauseCard.transform, "SON KONTROL NOKTASINA DÖN", semibold,
            Vector2.one * 0.5f, new Vector2(0f, -105f), new Vector2(560f, 104f),
            new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        pauseOverlay.SetActive(false);

        GameObject completion = CreatePanel("ChapterCompletionCard", safeArea.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -35f), new Vector2(900f, 570f), new Color(0.035f, 0.07f, 0.1f, 0.98f), true);
        CreateText("CompletionEyebrow", completion.transform, semibold, 23f, Amber, TextAlignmentOptions.Center,
            new Vector2(0f, 215f), new Vector2(760f, 42f)).text = completionEyebrow;
        CreateText("CompletionTitle", completion.transform, bold, 45f, Cream, TextAlignmentOptions.Center,
            new Vector2(0f, 140f), new Vector2(780f, 70f)).text = completionTitle;
        TMP_Text completionDetail = CreateText("CompletionDetail", completion.transform, regular, 25f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 24f), new Vector2(770f, 150f));
        completionDetail.text = "Sahnedeki güvenli davranışlar tamamlandı.";
        CreateText("CompletionSafety", completion.transform, semibold, 22f, Teal, TextAlignmentOptions.Center,
            new Vector2(0f, -108f), new Vector2(760f, 82f)).text = completionSafety;
        Button replay = CreateButton("ReplayChapterButton", completion.transform, "PERDEYİ YENİDEN OYNA", semibold,
            Vector2.one * 0.5f, new Vector2(0f, -220f), new Vector2(580f, 94f), Teal, Color.white);
        completion.SetActive(false);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(parent);

        StoryUIController controller = canvasObject.AddComponent<StoryUIController>();
        SetReference(controller, "objectiveTitle", objectiveTitle);
        SetReference(controller, "objectiveDetail", objectiveDetail);
        SetReference(controller, "subtitle", subtitle);
        SetReference(controller, "contextPrompt", context);
        SetReference(controller, "pausePanel", pauseOverlay);
        SetReference(controller, "cameraController", cameraController);
        UnityEventTools.AddPersistentListener(pause.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(resume.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(retry.onClick, controller.RetryCheckpoint);
        UnityEventTools.AddPersistentListener(replay.onClick, controller.ReplayStory);

        return new ChapterUI
        {
            controller = controller,
            completionPanel = completion,
            completionDetail = completionDetail
        };
    }

    internal static StoryInteractable AddInteractable(GameObject visualRoot, string id, string prompt,
        StoryInteractionKind kind, Transform interactionPoint, StoryInteractionGesture gesture, StoryCameraZoneId cameraZone,
        bool interactFromAnywhere = false, int gestureCount = 1, float interactionSeconds = 1.2f, float range = 1.45f)
    {
        EnsureCollider(visualRoot);
        StoryInteractable interactable = visualRoot.GetComponent<StoryInteractable>() ??
                                         visualRoot.AddComponent<StoryInteractable>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactionId").stringValue = id;
        serialized.FindProperty("prompt").stringValue = prompt;
        serialized.FindProperty("interactionKind").intValue = (int)kind;
        serialized.FindProperty("interactionPoint").objectReferenceValue = interactionPoint;
        serialized.FindProperty("interactionRange").floatValue = range;
        serialized.FindProperty("interactionGesture").intValue = (int)gesture;
        serialized.FindProperty("requiredGestureCount").intValue = Mathf.Max(1, gestureCount);
        serialized.FindProperty("estimatedInteractionSeconds").floatValue = Mathf.Max(0.25f, interactionSeconds);
        serialized.FindProperty("gestureTarget").objectReferenceValue = null;
        serialized.FindProperty("focusCameraZone").intValue = (int)cameraZone;
        serialized.FindProperty("returnCameraAfterCompletion").boolValue = false;
        serialized.FindProperty("interactFromAnywhere").boolValue = interactFromAnywhere;
        serialized.FindProperty("autoTriggerOnPlayerEnter").boolValue = false;
        serialized.FindProperty("oneShot").boolValue = true;
        serialized.FindProperty("availableOnStart").boolValue = false;
        serialized.FindProperty("highlightRoot").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return interactable;
    }

    internal static void SetGestureTarget(StoryInteractable interactable, Transform target)
    {
        if (interactable == null)
            return;
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("gestureTarget").objectReferenceValue = target;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static Animation CreateMoveAnimation(GameObject target, string assetName, Vector3 startOffset, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        Vector3 end = target.transform.localPosition;
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
            AnimationCurve.EaseInOut(0f, end.x + startOffset.x, duration, end.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y",
            AnimationCurve.EaseInOut(0f, end.y + startOffset.y, duration, end.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z",
            AnimationCurve.EaseInOut(0f, end.z + startOffset.z, duration, end.z));
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static Animation CreateRotationAnimation(GameObject target, string assetName, Vector3 startEuler,
        Vector3 endEuler, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x",
            AnimationCurve.EaseInOut(0f, startEuler.x, duration, endEuler.x));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y",
            AnimationCurve.EaseInOut(0f, startEuler.y, duration, endEuler.y));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z",
            AnimationCurve.EaseInOut(0f, startEuler.z, duration, endEuler.z));
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static Animation CreateRockAnimation(GameObject target, string assetName, float degrees, float duration)
    {
        AnimationClip clip = GetOrCreateLegacyClip(assetName);
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(duration * 0.25f, degrees),
            new Keyframe(duration * 0.5f, -degrees * 0.7f),
            new Keyframe(duration * 0.75f, degrees * 0.35f),
            new Keyframe(duration, 0f));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", curve);
        EditorUtility.SetDirty(clip);
        return AttachAnimation(target, clip);
    }

    internal static ParticleSystem CreateDust(string name, Transform parent, Vector3 position, Materials materials,
        int burstCount = 22)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.71f, 0.64f, 0.52f, 0.5f));
        main.gravityModifier = 0.06f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.8f, 0.15f, 0.35f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = materials.dust;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return particles;
    }

    internal static AudioSource CreateAudioSource(string name, Transform parent, AudioClip clip, float volume,
        bool loop = false, bool playOnAwake = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = playOnAwake;
        source.spatialBlend = 0f;
        return source;
    }

    internal static void BuildLighting(Transform parent, VolumeProfile profile, Color color, float intensity)
    {
        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.SetParent(parent);
        sunObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = color;
        sun.intensity = intensity;
        sun.shadows = LightShadows.Soft;

        GameObject fillObject = new GameObject("StoryFillLight");
        fillObject.transform.SetParent(parent);
        fillObject.transform.position = new Vector3(0f, 6f, -2f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.78f, 0.9f, 1f);
        fill.intensity = 2.1f;
        fill.range = 16f;
        fill.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("StoryGlobalVolume");
        volumeObject.transform.SetParent(parent);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    internal static void BuildNavigation(GameObject environment)
    {
        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>() ?? environment.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    internal static void ConfigureDynamicNavigationBlocker(GameObject root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        if (!TryGetRendererBounds(root, out Bounds bounds))
            throw new InvalidOperationException(root.name + " için dinamik kapı sınırı hesaplanamadı.");

        // FBX model-prefab köklerine NavMeshObstacle eklemek Unity 6'da yok edilmiş bir
        // component referansı döndürebiliyor. Kapının fizik ve navigasyon temsilini,
        // sahneye ait düzenlenebilir bir child üzerinde tut.
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);

        GameObject blocker = new GameObject("DynamicNavigationBlocker");
        blocker.transform.SetParent(root.transform, false);
        NavMeshModifier modifier = blocker.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        NavMeshObstacle obstacle = blocker.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;

        Vector3[] corners =
        {
            new(bounds.min.x, bounds.min.y, bounds.min.z),
            new(bounds.min.x, bounds.min.y, bounds.max.z),
            new(bounds.min.x, bounds.max.y, bounds.min.z),
            new(bounds.min.x, bounds.max.y, bounds.max.z),
            new(bounds.max.x, bounds.min.y, bounds.min.z),
            new(bounds.max.x, bounds.min.y, bounds.max.z),
            new(bounds.max.x, bounds.max.y, bounds.min.z),
            new(bounds.max.x, bounds.max.y, bounds.max.z)
        };
        Bounds local = new Bounds(root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(root.transform.InverseTransformPoint(corner));
        BoxCollider blockerCollider = blocker.AddComponent<BoxCollider>();
        blockerCollider.center = local.center;
        blockerCollider.size = local.size;
        obstacle.center = local.center;
        obstacle.size = local.size;
    }

    internal static void ConfigureSession(StoryGameManager manager, StoryAct act, StoryFlag[] initialFlags)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("initialAct").intValue = (int)act;
        SerializedProperty flags = serialized.FindProperty("initialFlags");
        flags.arraySize = initialFlags?.Length ?? 0;
        for (int i = 0; i < flags.arraySize; i++)
            flags.GetArrayElementAtIndex(i).intValue = (int)initialFlags[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void AddScenesToBuildSettings(params string[] scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (string path in scenePaths)
        {
            if (scenes.Any(scene => scene.path == path))
                continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
        }
        if (changed)
            EditorBuildSettings.scenes = scenes.ToArray();
    }

    internal static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent, bool collider = true, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider existing = go.GetComponent<Collider>();
        if (!collider && existing != null)
            Object.DestroyImmediate(existing);
        return go;
    }

    internal static GameObject InstantiateFurniture(string relativePath, string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler = default, bool keepColliders = true)
    {
        string path = FurnitureRoot + "/" + relativePath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Mobilya prefabı bulunamadı: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localScale = Vector3.one;
        instance.transform.rotation = Quaternion.Euler(euler);
        if (!keepColliders)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }
        FitToSize(instance, feetPosition, targetSize);
        return instance;
    }

    internal static GameObject InstantiateAsset(string assetPath, string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler = default, bool keepColliders = true, bool ensureCollider = false,
        Material materialOverride = null)
    {
        string path = assetPath.Replace('\\', '/');
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("3B prop asseti bulunamadı: " + path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localScale = Vector3.one;
        instance.transform.rotation = Quaternion.Euler(euler);
        if (materialOverride != null)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = materialOverride;
                renderer.sharedMaterials = materials;
            }
        }
        if (!keepColliders)
        {
            // Bazı eski prop prefabları DraggableItem taşır; bu bileşen Collider zorunluluğu koyar.
            // Prop yeni Story sahnesine yalnız görsel olarak alınırken önce eski input köprüsünü sök.
            foreach (DraggableItem draggable in instance.GetComponentsInChildren<DraggableItem>(true))
                Object.DestroyImmediate(draggable);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }

        FitToSize(instance, feetPosition, targetSize);
        if (ensureCollider)
            EnsureCollider(instance);
        return instance;
    }

    internal static Transform CreatePoint(string name, Transform parent, Vector3 position, Vector3 lookAt)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent);
        point.transform.position = position;
        Vector3 direction = lookAt - position;
        if (direction.sqrMagnitude > 0.001f)
            point.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return point.transform;
    }

    internal static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    internal static void CreateWorldLabel(string name, string text, Vector3 position, Vector3 euler, float fontSize,
        Color color, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(euler);
        TextMeshPro label = root.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableAutoSizing = false;
        label.rectTransform.sizeDelta = size;
    }

    internal static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.objectReferenceValue = value;
    }

    internal static void SetArray(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " alanı bulunamadı.");
        property.arraySize = values?.Length ?? 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool emissive = false,
        Color emission = default)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip GetOrCreateLegacyClip(string assetName)
    {
        string path = AnimationRoot + "/" + assetName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = assetName, legacy = true, frameRate = 30f };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        return clip;
    }

    private static Animation AttachAnimation(GameObject target, AnimationClip clip)
    {
        bool wasActive = target.activeSelf;
        if (!wasActive)
            target.SetActive(true);
        Animation animation = target.GetComponent<Animation>();
        if (animation == null)
            animation = target.AddComponent<Animation>();
        animation.playAutomatically = false;
        animation.AddClip(clip, clip.name);
        animation.clip = clip;
        if (!wasActive)
            target.SetActive(false);
        return animation;
    }

    private static void FitToHeight(GameObject instance, Vector3 feetPosition, float targetHeight)
    {
        if (!TryGetRendererBounds(instance, out Bounds bounds))
        {
            instance.transform.position = feetPosition;
            return;
        }
        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        instance.transform.localScale *= scale;
        TryGetRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static void FitToSize(GameObject instance, Vector3 feetPosition, Vector3 targetSize)
    {
        if (!TryGetRendererBounds(instance, out Bounds bounds))
        {
            instance.transform.position = feetPosition;
            return;
        }
        float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(0.01f, bounds.size.y),
                targetSize.z / Mathf.Max(0.01f, bounds.size.z)));
        instance.transform.localScale *= scale;
        TryGetRendererBounds(instance, out bounds);
        instance.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return true;
    }

    private static void EnsureCollider(GameObject root)
    {
        if (root.GetComponentsInChildren<Collider>(true).Length > 0)
            return;
        if (!TryGetRendererBounds(root, out Bounds bounds))
        {
            BoxCollider fallback = root.AddComponent<BoxCollider>();
            fallback.size = Vector3.one;
            return;
        }

        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
        };
        Bounds local = new Bounds(root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
        foreach (Vector3 corner in corners.Skip(1))
            local.Encapsulate(root.transform.InverseTransformPoint(corner));
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = local.center;
        collider.size = local.size;
    }

    private static Quaternion LookAt(Vector3 position, Vector3 target)
    {
        return Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }

    private static GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = (anchorMin + anchorMax) * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color, bool blocksRaycasts)
    {
        GameObject panel = CreateUIRect(name, parent, anchorMin, anchorMax, position, size);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksRaycasts;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject go = CreateUIRect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, dimensions);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchor,
        Vector2 position, Vector2 size, Color background, Color foreground)
    {
        GameObject go = CreatePanel(name, parent, anchor, anchor, position, size, background, true);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        TMP_Text text = CreateText("Label", go.transform, font, 27f, foreground, TextAlignmentOptions.Center,
            Vector2.zero, size - new Vector2(24f, 18f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 27f;
        text.text = label;
        return button;
    }
}
