using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using UnityEditor;
using UnityEngine;

internal static class StoryAdultPoseQA
{
    public const string GameViewCapturePath = "Temp/StoryGameView.png";
    private static double denizWalkSampleAt;
    private static double denizWalkSampleDeadline;
    private static int childWalkCandidateIndex = -1;
    private static int childHoldCandidateIndex = -1;
    private static int childCoverSampleIndex = -1;

    private static readonly float[] ChildCoverSamplePhases =
    {
        0.25f, 0.40f, 0.50f, 0.60f, 0.75f, 0.90f
    };

    private static readonly (string path, string clipName)[] ChildWalkCandidates =
    {
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementBasic.fbx", "Walking_A"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementBasic.fbx", "Walking_B"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementBasic.fbx", "Walking_C"),
        ("Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx", "Armature|Walk_Formal_Loop"),
        ("Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx", "Armature|Walk_Loop"),
        (StoryAnimationLibraryBuilder.ChildNaturalWalkPath, "ChildNaturalWalk"),
        ("Assets/KidsCharacterFree/AnimationClips/Humanoid/boy_move_walk.anim", "boy_move_walk")
    };

    private static readonly (string path, string clipName)[] ChildHoldCandidates =
    {
        ("Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx", "Armature|Hit_Head"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Simulation.fbx", "Cheering"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Simulation.fbx", "Waving"),
        ("Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx", "Armature|Crouch_Idle_Loop"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementAdvanced.fbx", "Crouching"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Holding_A"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Holding_B"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Holding_C"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx", "Working_A"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx", "Hit_A"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx", "Hit_B"),
        ("Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx", "Use_Item"),
        ("Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx", "Armature|Fixing_Kneeling")
    };

    [MenuItem("Tools/Deprem Story/QA/Start Story01 Deniz Walk Preview")]
    private static void StartStory01DenizWalkPreview()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Deniz walk QA requires Play Mode.");
            return;
        }

        GameObject deniz = GameObject.Find("Deniz_12");
        StoryPlayerMovement movement = deniz != null ? deniz.GetComponent<StoryPlayerMovement>() : null;
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        if (deniz == null || movement == null || cameraController == null)
        {
            Debug.LogError("Story01 Deniz walk QA could not find Deniz_12, movement, or camera controller.");
            return;
        }

        StoryPreparationDirector preparationDirector =
            Object.FindFirstObjectByType<StoryPreparationDirector>(FindObjectsInactive.Include);
        if (preparationDirector != null)
            preparationDirector.enabled = false;
        movement.SetStoryInputLocked(false);
        cameraController.ActivateZone(StoryCameraZoneId.PreparationBagFit, true);
        Vector3 leftPreviewPoint = new Vector3(-3.8f, 0f, -1.1f);
        Vector3 rightPreviewPoint = new Vector3(0.1f, 0f, -1.1f);
        Vector3 previewDestination =
            Vector3.Distance(deniz.transform.position, rightPreviewPoint) < 0.8f
                ? leftPreviewPoint
                : rightPreviewPoint;
        if (!movement.TrySetDestination(previewDestination))
        {
            Debug.LogError($"DENIZ_WALK_QA could not start route to {previewDestination}.");
            return;
        }

        denizWalkSampleAt = EditorApplication.timeSinceStartup + 0.25d;
        denizWalkSampleDeadline = EditorApplication.timeSinceStartup + 2d;
        EditorApplication.update -= LogDenizWalkAfterFirstSteps;
        EditorApplication.update += LogDenizWalkAfterFirstSteps;
        Debug.Log($"DENIZ_WALK_QA started destination={previewDestination}");
    }

    private static void LogDenizWalkAfterFirstSteps()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= LogDenizWalkAfterFirstSteps;
            return;
        }

        if (EditorApplication.timeSinceStartup < denizWalkSampleAt)
            return;

        GameObject deniz = GameObject.Find("Deniz_12");
        Animator animator = deniz != null ? deniz.GetComponentInChildren<Animator>(true) : null;
        if (animator != null && animator.GetFloat("Speed") < 0.15f &&
            EditorApplication.timeSinceStartup < denizWalkSampleDeadline)
        {
            denizWalkSampleAt = EditorApplication.timeSinceStartup + 0.1d;
            return;
        }

        EditorApplication.update -= LogDenizWalkAfterFirstSteps;
        LogStory01DenizWalkPose();
        CaptureGameView();
    }

    [MenuItem("Tools/Deprem Story/QA/Log Story01 Deniz Walk Pose")]
    private static void LogStory01DenizWalkPose()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Deniz walk QA requires Play Mode.");
            return;
        }

        GameObject deniz = GameObject.Find("Deniz_12");
        Animator animator = deniz != null ? deniz.GetComponentInChildren<Animator>(true) : null;
        if (deniz == null || animator == null)
        {
            Debug.LogError("Story01 Deniz walk QA could not find Deniz_12 or its Animator.");
            return;
        }

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        string clipWeights = string.Empty;
        for (int i = 0; i < clips.Length; i++)
        {
            if (i > 0)
                clipWeights += ",";
            clipWeights += $"{(clips[i].clip != null ? clips[i].clip.name : "<none>")}:{clips[i].weight:F2}";
        }
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        float footSeparation = leftFoot != null && rightFoot != null
            ? Vector3.ProjectOnPlane(leftFoot.position - rightFoot.position, Vector3.up).magnitude
            : -1f;
        float hipSeparation = leftUpperLeg != null && rightUpperLeg != null
            ? Vector3.ProjectOnPlane(leftUpperLeg.position - rightUpperLeg.position, Vector3.up).magnitude
            : -1f;
        float stanceRatio = footSeparation >= 0f && hipSeparation > 0.001f
            ? footSeparation / hipSeparation
            : -1f;
        float headHeight = head != null ? head.position.y - deniz.transform.position.y : -1f;
        Debug.Log(
            $"DENIZ_WALK_QA clips=[{clipWeights}] speed={animator.GetFloat("Speed"):F2} " +
            $"moving={deniz.GetComponent<StoryPlayerMovement>()?.IsMoving} " +
            $"footSeparation={footSeparation:F3} stanceRatio={stanceRatio:F2} headHeight={headHeight:F3}");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Next Deniz Walk Candidate")]
    private static void PreviewNextDenizWalkCandidate()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Deniz walk candidate QA requires Play Mode.");
            return;
        }

        GameObject deniz = GameObject.Find("Deniz_12");
        Animator animator = deniz != null ? deniz.GetComponentInChildren<Animator>(true) : null;
        StoryPlayerMovement movement = deniz != null ? deniz.GetComponent<StoryPlayerMovement>() : null;
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        if (deniz == null || animator == null || movement == null || cameraController == null || baseController == null)
        {
            Debug.LogError("Deniz walk candidate QA could not resolve its scene references.");
            return;
        }

        childWalkCandidateIndex = (childWalkCandidateIndex + 1) % ChildWalkCandidates.Length;
        (string path, string clipName) candidate = ChildWalkCandidates[childWalkCandidateIndex];
        AnimationClip replacement = candidate.path.EndsWith(".anim")
            ? AssetDatabase.LoadAssetAtPath<AnimationClip>(candidate.path)
            : AssetDatabase.LoadAllAssetsAtPath(candidate.path).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == candidate.clipName);
        if (replacement == null)
        {
            Debug.LogError($"Deniz walk candidate QA could not load {candidate.clipName} from {candidate.path}.");
            return;
        }

        movement.Stop();
        movement.SetStoryInputLocked(true);
        AnimatorOverrideController overrides = new AnimatorOverrideController(baseController);
        List<KeyValuePair<AnimationClip, AnimationClip>> mappings = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrides.GetOverrides(mappings);
        for (int index = 0; index < mappings.Count; index++)
        {
            if (mappings[index].Key != null && mappings[index].Key.name == "ChildNaturalWalk")
                mappings[index] = new KeyValuePair<AnimationClip, AnimationClip>(mappings[index].Key, replacement);
        }
        overrides.ApplyOverrides(mappings);
        animator.runtimeAnimatorController = overrides;
        animator.SetFloat("Speed", 1f);
        animator.Play("Locomotion", 0, 0.18f);
        animator.Update(0f);
        cameraController.ActivateZone(StoryCameraZoneId.PreparationBagFit, true);

        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftFoot == null || rightFoot == null || leftUpperLeg == null || rightUpperLeg == null)
        {
            Debug.LogError("Deniz walk candidate QA could not resolve humanoid leg bones.");
            return;
        }

        Vector3 lateralAxis = Vector3.ProjectOnPlane(animator.transform.right, Vector3.up).normalized;
        float footSeparation = Mathf.Abs(Vector3.Dot(leftFoot.position - rightFoot.position, lateralAxis));
        float hipSeparation = Mathf.Abs(Vector3.Dot(leftUpperLeg.position - rightUpperLeg.position, lateralAxis));
        float stanceRatio = footSeparation / Mathf.Max(0.001f, hipSeparation);
        Debug.Log(
            $"DENIZ_WALK_CANDIDATE_QA candidate={candidate.clipName} sample=0.18 " +
            $"lateralFootSeparation={footSeparation:F3} lateralHipSeparation={hipSeparation:F3} " +
            $"lateralStanceRatio={stanceRatio:F2}");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Next Child Hold Candidate")]
    private static void PreviewNextChildHoldCandidate()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Child hold candidate QA requires Play Mode.");
            return;
        }

        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        Animator[] animators =
        {
            GameObject.Find("Deniz_12")?.GetComponentInChildren<Animator>(true),
            GameObject.Find("Can_8")?.GetComponentInChildren<Animator>(true)
        };
        if (cameraController == null || baseController == null || animators.Any(animator => animator == null))
        {
            Debug.LogError("Child hold candidate QA could not resolve its scene references.");
            return;
        }

        childHoldCandidateIndex = (childHoldCandidateIndex + 1) % ChildHoldCandidates.Length;
        (string path, string clipName) candidate = ChildHoldCandidates[childHoldCandidateIndex];
        AnimationClip replacement = AssetDatabase.LoadAllAssetsAtPath(candidate.path).OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == candidate.clipName);
        if (replacement == null)
        {
            Debug.LogError($"Child hold candidate QA could not load {candidate.clipName} from {candidate.path}.");
            return;
        }

        const float samplePhase = 0.25f;
        foreach (Animator animator in animators)
            ApplyFullBodyCandidate(animator, baseController, replacement, samplePhase);

        cameraController.ActivateZone(StoryCameraZoneId.UnderTable, true);
        Animator denizAnimator = animators[0];
        GetHandToHeadDistances(denizAnimator, out float leftDistance, out float rightDistance);
        string sampledClip = denizAnimator.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name ?? "<none>";
        Debug.Log(
            $"CHILD_HOLD_CANDIDATE_QA candidate={candidate.clipName} sampledClip={sampledClip} sample={samplePhase:F2} " +
            $"leftHandToHead={leftDistance:F3} rightHandToHead={rightDistance:F3}");
    }

    [MenuItem("Tools/Deprem Story/QA/Evaluate Child Cover Candidates")]
    private static void EvaluateChildCoverCandidates()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Child cover candidate evaluation requires Play Mode.");
            return;
        }

        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        Animator animator = GameObject.Find("Deniz_12")?.GetComponentInChildren<Animator>(true);
        if (baseController == null || animator == null)
        {
            Debug.LogError("Child cover candidate evaluation could not resolve Deniz or the story controller.");
            return;
        }

        string bestClip = string.Empty;
        float bestPhase = 0f;
        float bestScore = float.MaxValue;
        float bestLeft = 0f;
        float bestRight = 0f;
        foreach ((string path, string clipName) candidate in ChildHoldCandidates)
        {
            AnimationClip replacement = AssetDatabase.LoadAllAssetsAtPath(candidate.path).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == candidate.clipName);
            if (replacement == null)
                continue;

            for (int phaseStep = 0; phaseStep < 20; phaseStep++)
            {
                float phase = phaseStep / 20f;
                ApplyFullBodyCandidate(animator, baseController, replacement, phase);
                GetHandToHeadDistances(animator, out float leftDistance, out float rightDistance);
                // A convincing child-protection pose needs at least one hand close to the head
                // without throwing the other arm into a full T-pose. Weight the near hand most,
                // while still preferring a compact second arm.
                float near = Mathf.Min(leftDistance, rightDistance);
                float far = Mathf.Max(leftDistance, rightDistance);
                float score = near * 0.72f + far * 0.28f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestClip = candidate.clipName;
                bestPhase = phase;
                bestLeft = leftDistance;
                bestRight = rightDistance;
            }
        }

        Debug.Log(
            $"CHILD_COVER_CANDIDATE_BEST clip={bestClip} phase={bestPhase:F2} score={bestScore:F3} " +
            $"leftHandToHead={bestLeft:F3} rightHandToHead={bestRight:F3}");
    }

    private static void ApplyFullBodyCandidate(Animator animator, RuntimeAnimatorController baseController,
        AnimationClip replacement, float normalizedTime)
    {
        AnimatorOverrideController overrides = new AnimatorOverrideController(baseController);
        List<KeyValuePair<AnimationClip, AnimationClip>> mappings =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrides.GetOverrides(mappings);
        bool replaced = false;
        for (int index = 0; index < mappings.Count; index++)
        {
            if (mappings[index].Key != null && mappings[index].Key.name == "Crouching")
            {
                mappings[index] = new KeyValuePair<AnimationClip, AnimationClip>(
                    mappings[index].Key, replacement);
                replaced = true;
            }
        }
        if (!replaced)
            Debug.LogError("Child cover QA could not find Crouching in override mappings: " +
                           string.Join(", ", mappings.Select(mapping => mapping.Key?.name ?? "<null>")));
        overrides.ApplyOverrides(mappings);

        animator.runtimeAnimatorController = overrides;
        animator.Rebind();
        animator.speed = 1f;
        animator.Update(0f);
        animator.Update(0.02f);
        animator.SetFloat("Speed", 0f);
        // Inspect the candidate's raw retargeted body pose. The authored controller's synced
        // upper-body layer is deliberately muted here; otherwise it would hide the candidate's arms.
        animator.SetLayerWeight(1, 0f);
        animator.Play("Hold Cover", 0, normalizedTime);
        animator.Update(0.02f);
        animator.speed = 0f;
    }

    private static void GetHandToHeadDistances(Animator animator, out float leftDistance, out float rightDistance)
    {
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        float scale = Mathf.Max(0.001f, animator.humanScale);
        leftDistance = head != null && leftHand != null
            ? Vector3.Distance(head.position, leftHand.position) / scale
            : float.MaxValue;
        rightDistance = head != null && rightHand != null
            ? Vector3.Distance(head.position, rightHand.position) / scale
            : float.MaxValue;
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Authored Child Cover Pose")]
    private static void PreviewAuthoredChildCoverPose()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Authored child cover pose QA requires Play Mode.");
            return;
        }

        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        Animator[] animators =
        {
            GameObject.Find("Deniz_12")?.GetComponentInChildren<Animator>(true),
            GameObject.Find("Can_8")?.GetComponentInChildren<Animator>(true)
        };
        if (controller == null || cameraController == null || animators.Any(animator => animator == null))
        {
            Debug.LogError("Authored child cover pose QA could not resolve its scene references.");
            return;
        }

        foreach (Animator animator in animators)
        {
            animator.runtimeAnimatorController = controller;
            animator.Rebind();
            animator.speed = 1f;
            animator.Update(0f);
            animator.Update(0.02f);
            animator.SetLayerWeight(1, 1f);
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("StoryHold");
            animator.Update(0.02f);
            animator.Update(0.2f);
            animator.speed = 0f;
        }
        cameraController.ActivateZone(StoryCameraZoneId.UnderTable, true);
        Animator sample = animators[0];
        AnimatorClipInfo baseInfo = sample.GetCurrentAnimatorClipInfo(0).FirstOrDefault();
        AnimatorClipInfo upperInfo = sample.layerCount > 1
            ? sample.GetCurrentAnimatorClipInfo(1).FirstOrDefault()
            : default;
        string baseClip = baseInfo.clip?.name ?? "<none>";
        string upperClip = sample.layerCount > 1 ? upperInfo.clip?.name ?? "<none>" : "<missing layer>";
        GetHandToHeadDistances(sample, out float leftDistance, out float rightDistance);
        Debug.Log(
            $"CHILD_COVER_POSE_QA baseState={sample.GetCurrentAnimatorStateInfo(0).IsName("Hold Cover")} " +
            $"baseClip={baseClip}:{baseInfo.weight:F2} upperClip={upperClip}:{upperInfo.weight:F2} " +
            $"upperLayerWeight={sample.GetLayerWeight(1):F2} " +
            $"leftHandToHead={leftDistance:F3} rightHandToHead={rightDistance:F3}");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Next Waving Cover Sample")]
    private static void PreviewNextWavingCoverSample()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Waving cover sample QA requires Play Mode.");
            return;
        }

        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        AnimationClip waving = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Simulation.fbx")
            .OfType<AnimationClip>().FirstOrDefault(clip => clip.name == "Waving");
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        Animator[] animators =
        {
            GameObject.Find("Deniz_12")?.GetComponentInChildren<Animator>(true),
            GameObject.Find("Can_8")?.GetComponentInChildren<Animator>(true)
        };
        if (baseController == null || waving == null || cameraController == null ||
            animators.Any(animator => animator == null))
        {
            Debug.LogError("Waving cover sample QA could not resolve its scene references.");
            return;
        }

        childCoverSampleIndex = (childCoverSampleIndex + 1) % ChildCoverSamplePhases.Length;
        float phase = ChildCoverSamplePhases[childCoverSampleIndex];
        foreach (Animator animator in animators)
        {
            AnimatorOverrideController overrides = new AnimatorOverrideController(baseController);
            List<KeyValuePair<AnimationClip, AnimationClip>> mappings =
                new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(mappings);
            for (int index = 0; index < mappings.Count; index++)
            {
                if (mappings[index].Key != null && mappings[index].Key.name == "ChildCoverPose")
                    mappings[index] = new KeyValuePair<AnimationClip, AnimationClip>(
                        mappings[index].Key, waving);
            }

            overrides.ApplyOverrides(mappings);
            animator.runtimeAnimatorController = overrides;
            animator.speed = 0f;
            animator.SetFloat("Speed", 0f);
            animator.Play("Hold Cover", 0, phase);
            animator.Update(0f);
        }

        cameraController.ActivateZone(StoryCameraZoneId.UnderTable, true);
        Animator deniz = animators[0];
        Transform head = deniz.GetBoneTransform(HumanBodyBones.Head);
        Transform leftHand = deniz.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = deniz.GetBoneTransform(HumanBodyBones.RightHand);
        float nearestHandToHead = head != null && leftHand != null && rightHand != null
            ? Mathf.Min(Vector3.Distance(head.position, leftHand.position),
                Vector3.Distance(head.position, rightHand.position))
            : -1f;
        Debug.Log(
            $"CHILD_WAVING_COVER_SAMPLE phase={phase:F2} nearestHandToHead={nearestHandToHead:F3}");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Story01 Child Neutral Idle")]
    private static void PreviewStory01ChildNeutralIdle()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Child idle pose QA requires Play Mode.");
            return;
        }

        GameObject deniz = GameObject.Find("Deniz_12");
        Animator animator = deniz != null ? deniz.GetComponentInChildren<Animator>(true) : null;
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        if (deniz == null || animator == null || cameraController == null || controller == null)
        {
            Debug.LogError("Story01 child idle QA could not resolve Deniz, Animator, controller, or camera.");
            return;
        }

        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.speed = 1f;
        animator.SetFloat("Speed", 0f);
        animator.Play("Locomotion", 0, 0.25f);
        animator.Update(0f);
        cameraController.ActivateZone(StoryCameraZoneId.PreparationOverview, true);
        LogLateralStance(animator, "CHILD_IDLE_QA");
    }

    private static void LogLateralStance(Animator animator, string label)
    {
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftFoot == null || rightFoot == null || leftUpperLeg == null || rightUpperLeg == null)
        {
            Debug.LogError(label + " could not resolve humanoid leg bones.");
            return;
        }

        Vector3 lateralAxis = Vector3.ProjectOnPlane(animator.transform.right, Vector3.up).normalized;
        float footSeparation = Mathf.Abs(Vector3.Dot(leftFoot.position - rightFoot.position, lateralAxis));
        float hipSeparation = Mathf.Abs(Vector3.Dot(leftUpperLeg.position - rightUpperLeg.position, lateralAxis));
        float stanceRatio = footSeparation / Mathf.Max(0.001f, hipSeparation);
        AnimatorClipInfo clip = animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault();
        Debug.Log(
            $"{label} clip={clip.clip?.name ?? "<none>"} footSeparation={footSeparation:F3} " +
            $"hipSeparation={hipSeparation:F3} stanceRatio={stanceRatio:F2}");
    }

    [MenuItem("Tools/Deprem Story/QA/Preview Story01 Adult Neutral Idle")]
    private static void PreviewStory01AdultNeutralIdle()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Adult pose QA requires Play Mode.");
            return;
        }

        GameObject adult = GameObject.Find("Anne_Ayse");
        Animator animator = adult != null ? adult.GetComponentInChildren<Animator>(true) : null;
        StoryCameraController cameraController =
            Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        if (adult == null || animator == null || cameraController == null)
        {
            Debug.LogError("Story01 adult pose QA could not find Anne_Ayse, Animator, or camera controller.");
            return;
        }

        animator.Play("Adult Idle", 0, 0.25f);
        animator.Update(0f);
        cameraController.ActivateZone(StoryCameraZoneId.PreparationParent, true);

        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftFoot == null || rightFoot == null || leftUpperLeg == null || rightUpperLeg == null)
        {
            Debug.LogError("Story01 adult pose QA could not resolve humanoid leg bones.");
            return;
        }

        Vector3 lateralAxis = Vector3.ProjectOnPlane(animator.transform.right, Vector3.up).normalized;
        float footSeparation = Mathf.Abs(Vector3.Dot(leftFoot.position - rightFoot.position, lateralAxis));
        float hipSeparation = Mathf.Abs(Vector3.Dot(leftUpperLeg.position - rightUpperLeg.position, lateralAxis));
        float stanceRatio = footSeparation / Mathf.Max(0.001f, hipSeparation);
        Debug.Log(
            $"ADULT_IDLE_QA state={animator.GetCurrentAnimatorStateInfo(0).IsName("Adult Idle")} " +
            $"clip={animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} " +
            $"footSeparation={footSeparation:F3} hipSeparation={hipSeparation:F3} stanceRatio={stanceRatio:F2}");
    }

    [MenuItem("Tools/Deprem Story/QA/Capture Game View")]
    private static void CaptureGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Game View capture requires Play Mode.");
            return;
        }

        if (EditorApplication.isPaused)
            EditorApplication.isPaused = false;

        string path = Path.GetFullPath(GameViewCapturePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Temp");
        if (File.Exists(path))
            File.Delete(path);
        // CaptureScreenshot queues the readback at the end of the rendered frame. Calling
        // CaptureScreenshotAsTexture directly from an editor menu runs before that point and can
        // silently leave an old QA image behind, which previously made a broken pose look unchanged.
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("STORY_GAME_VIEW_CAPTURE_REQUESTED " + path);
    }
}
