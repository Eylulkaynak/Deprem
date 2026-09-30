using System.Reflection;
using System.Linq;
using Deprem.Story;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class StoryHomeSafetyQA
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Deprem Story/QA/Preview Home Shelf Drill")]
    private static void PreviewShelfDrill()
    {
        PreviewShelfDrillWithClip(null, null);
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Home Shelf Drill - Use Item")]
    private static void PreviewShelfDrillUseItem()
    {
        PreviewShelfDrillWithClip(
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx", "Use_Item");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Home Shelf Drill - Working B")]
    private static void PreviewShelfDrillWorkingB()
    {
        PreviewShelfDrillWithClip(
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Working_B");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Home Shelf Drill - Working C")]
    private static void PreviewShelfDrillWorkingC()
    {
        PreviewShelfDrillWithClip(
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Working_C");
    }

    private static void PreviewShelfDrillWithClip(string modelPath, string clipName)
    {
        StoryHomeSafetyDirector director = RequirePlayingDirector();
        if (director == null)
            return;
        if (!string.IsNullOrEmpty(modelPath))
            OverrideWorkClip(director, modelPath, clipName);
        PrepareSlowPreview(director);
        ActivateWorkPreview(director, "shelfParentWorkPoint", StoryCameraZoneId.HomeShelf);
        LogPreviewState(director, clipName ?? "controller-default");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Home Wardrobe Drill")]
    private static void PreviewWardrobeDrill()
    {
        StoryHomeSafetyDirector director = RequirePlayingDirector();
        if (director == null)
            return;
        PrepareSlowPreview(director);
        ActivateWorkPreview(director, "wardrobeParentWorkPoint", StoryCameraZoneId.HomeWardrobe);
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Home Unsafe Drill")]
    private static void PreviewUnsafeDrill()
    {
        StoryHomeSafetyDirector director = RequirePlayingDirector();
        if (director != null)
            director.TryUnsafeDrill();
    }

    [MenuItem("Tools/Deprem Story/QA/Log Home Drill State")]
    private static void LogDrillState()
    {
        StoryHomeSafetyDirector director = RequirePlayingDirector();
        if (director == null)
            return;

        Animator parentAnimator = GetField<Animator>(director, "parentAnimator");
        AudioSource drillAudio = GetField<AudioSource>(director, "drillWorkAudio");
        GameObject heldDrill = GetField<GameObject>(director, "parentHeldDrill");
        AnimatorStateInfo state = parentAnimator != null ? parentAnimator.GetCurrentAnimatorStateInfo(0) : default;
        Debug.Log(
            $"HOME_DRILL_QA state={GetCurrentStateName(parentAnimator, state)} " +
            $"normalized={state.normalizedTime:0.00} audioPlaying={drillAudio != null && drillAudio.isPlaying} " +
            $"clip={(drillAudio != null && drillAudio.clip != null ? drillAudio.clip.name : "none")} " +
            $"spatial={(drillAudio != null ? drillAudio.spatialBlend : 0f):0.00} " +
            $"heldVisible={heldDrill != null && heldDrill.activeInHierarchy}");
    }

    [MenuItem("Tools/Deprem Story/QA/Log Adult Work Asset")]
    private static void LogAdultWorkAsset()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath);
        AnimatorState state = controller?.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == "Adult Work");
        AnimationClip motion = state?.motion as AnimationClip;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(motion, out string guid, out long localId);
        Debug.Log($"ADULT_WORK_ASSET motion={(motion != null ? motion.name : "none")} guid={guid} localId={localId}");

        foreach (AnimationClip clip in AssetDatabase
                     .LoadAllAssetsAtPath("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx")
                     .OfType<AnimationClip>()
                     .Where(clip => clip != null && clip.name.StartsWith("Working_")))
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string clipGuid, out long clipLocalId);
            Debug.Log($"ADULT_WORK_CLIP name={clip.name} guid={clipGuid} localId={clipLocalId}");
        }
    }

    private static void LogPreviewState(StoryHomeSafetyDirector director, string clipLabel)
    {
        Transform parent = GetField<Transform>(director, "parent");
        Transform workPoint = GetField<Transform>(director, "shelfParentWorkPoint");
        AudioSource drillAudio = GetField<AudioSource>(director, "drillWorkAudio");
        GameObject heldDrill = GetField<GameObject>(director, "parentHeldDrill");
        FieldInfo stageField = typeof(StoryHomeSafetyDirector).GetField("stage", PrivateInstance);
        Debug.Log(
            $"HOME_DRILL_PREVIEW clip={clipLabel} stage={stageField?.GetValue(director)} " +
            $"parentPos={(parent != null ? parent.position.ToString("F2") : "none")} " +
            $"parentEuler={(parent != null ? parent.eulerAngles.ToString("F1") : "none")} " +
            $"workEuler={(workPoint != null ? workPoint.eulerAngles.ToString("F1") : "none")} " +
            $"audio={drillAudio != null && drillAudio.isPlaying} held={heldDrill != null && heldDrill.activeInHierarchy}");
    }

    private static StoryHomeSafetyDirector RequirePlayingDirector()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Home Safety QA önizlemesi için önce Play Mode'a gir.");
            return null;
        }

        StoryHomeSafetyDirector director =
            Object.FindFirstObjectByType<StoryHomeSafetyDirector>(FindObjectsInactive.Include);
        if (director == null)
            Debug.LogError("Aktif sahnede StoryHomeSafetyDirector bulunamadı.");
        return director;
    }

    private static void PrepareSlowPreview(StoryHomeSafetyDirector director)
    {
        FieldInfo duration = typeof(StoryHomeSafetyDirector).GetField("drillWorkSeconds", PrivateInstance);
        duration?.SetValue(director, 300f);
    }

    private static void ActivateWorkPreview(StoryHomeSafetyDirector director, string workPointField,
        StoryCameraZoneId cameraZone)
    {
        StoryCameraController camera = GetField<StoryCameraController>(director, "cameraController");
        camera?.ActivateZone(cameraZone, true);
        Transform workPoint = GetField<Transform>(director, workPointField);
        MethodInfo beginWork = typeof(StoryHomeSafetyDirector).GetMethod("BeginParentDrillWork", PrivateInstance);
        beginWork?.Invoke(director, new object[] { workPoint });
        Animator animator = GetField<Animator>(director, "parentAnimator");
        if (animator != null)
        {
            animator.Play("Adult Work", 0, 0.32f);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    private static void OverrideWorkClip(StoryHomeSafetyDirector director, string modelPath, string clipName)
    {
        Animator animator = GetField<Animator>(director, "parentAnimator");
        if (animator == null || animator.runtimeAnimatorController == null)
            return;
        RuntimeAnimatorController baseController = animator.runtimeAnimatorController is AnimatorOverrideController current
            ? current.runtimeAnimatorController
            : animator.runtimeAnimatorController;
        AnimationClip original = baseController.animationClips.FirstOrDefault(
                                     clip => clip != null && clip.name == "Working_B") ??
                                 baseController.animationClips.FirstOrDefault(
                                     clip => clip != null && clip.name == "Working_A");
        AnimationClip replacement = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && clip.name == clipName);
        if (original == null || replacement == null)
            throw new System.InvalidOperationException($"QA work clip bulunamadı: {clipName}");
        AnimatorOverrideController overrides = new AnimatorOverrideController(baseController);
        overrides[original.name] = replacement;
        animator.runtimeAnimatorController = overrides;
        animator.Rebind();
        animator.Update(0f);
    }

    private static T GetField<T>(StoryHomeSafetyDirector director, string name) where T : Object
    {
        FieldInfo field = typeof(StoryHomeSafetyDirector).GetField(name, PrivateInstance);
        return field?.GetValue(director) as T;
    }

    private static string GetCurrentStateName(Animator animator, AnimatorStateInfo state)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return "none";
        foreach (AnimationClip clip in animator.GetCurrentAnimatorClipInfo(0)
                     .Select(info => info.clip))
        {
            if (clip != null)
                return clip.name;
        }
        return state.fullPathHash.ToString();
    }
}
