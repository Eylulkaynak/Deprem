using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class StoryAnimationLibraryBuilder
{
    public const string ControllerPath = "Assets/Story/Animations/Generated/StoryCharacterAnimator.controller";
    public const string AdultControllerPath = "Assets/Story/Animations/Generated/StoryAdultAnimator.controller";
    public const string ChildNeutralIdlePath = "Assets/Story/Animations/Generated/ChildNeutralIdle.anim";
    public const string ChildNaturalWalkPath = "Assets/Story/Animations/Generated/ChildNaturalWalk.anim";
    public const string ChildCoverPosePath = "Assets/Story/Animations/Generated/ChildCoverPose.anim";
    public const string ChildCoverMaskPath = "Assets/Story/Animations/Generated/ChildCoverUpperBody.mask";
    public const string ChildCoverUpperPosePath = "Assets/Story/Animations/Generated/ChildCoverUpperPose.anim";
    public const string AdultNeutralIdlePath = "Assets/Story/Animations/Generated/AdultNeutralIdle.anim";

    private const string KayKitRoot = "Assets/Story/Animations/ThirdParty/KayKit";
    private const string QuaterniusRoot = "Assets/Story/Animations/ThirdParty/Quaternius";
    private const string GeneralPath = KayKitRoot + "/Rig_Medium_General.fbx";
    private const string MovementBasicPath = KayKitRoot + "/Rig_Medium_MovementBasic.fbx";
    private const string MovementAdvancedPath = KayKitRoot + "/Rig_Medium_MovementAdvanced.fbx";
    private const string SimulationPath = KayKitRoot + "/Rig_Medium_Simulation.fbx";
    private const string ToolsPath = KayKitRoot + "/Rig_Medium_Tools.fbx";
    private const string UniversalPath = QuaterniusRoot + "/UAL1_Standard.fbx";
    private const float ChildWalkPlaybackSpeed = 1.45f;

    private static readonly string[] SourceModels =
    {
        GeneralPath,
        MovementBasicPath,
        MovementAdvancedPath,
        SimulationPath,
        ToolsPath,
        UniversalPath
    };

    [MenuItem("Tools/Deprem Story/Build Story Animation Library")]
    public static void BuildFromMenu()
    {
        BuildLibrary(false);
    }

    public static RuntimeAnimatorController BuildLibrary(bool showDialog = false)
    {
        EnsureSourcesExist();
        MeshyFamilyCharacterImporter.EnsurePrepared();
        EnsureFolder("Assets/Story/Animations/Generated");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string sourceModel in SourceModels)
            ConfigureHumanoidModel(sourceModel);

        RuntimeAnimatorController controller = CreateChildController();
        CreateAdultController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log("Story animation libraries built: " + ControllerPath + " | " + AdultControllerPath);
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Story",
                "KayKit ve Universal klipleri Humanoid olarak hazırlandı; çocuk ve yetişkin Story Animator'ları oluşturuldu.",
                "Tamam");
        return controller;
    }

    public static RuntimeAnimatorController LoadAdultController()
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AdultControllerPath);
        return controller != null ? controller : BuildAdultLibrary();
    }

    public static RuntimeAnimatorController BuildAdultLibrary()
    {
        BuildLibrary(false);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AdultControllerPath);
        if (controller == null)
            throw new InvalidOperationException("Yetişkin Story Animator oluşturulamadı: " + AdultControllerPath);
        return controller;
    }

    private static void ConfigureHumanoidModel(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("ModelImporter bulunamadi: " + assetPath);

        bool reimport = importer.animationType != ModelImporterAnimationType.Human ||
                        importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                        !importer.importAnimation || importer.importCameras || importer.importLights ||
                        importer.materialImportMode != ModelImporterMaterialImportMode.None;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips != null && clips.Length > 0)
        {
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool loop = ShouldLoop(clip.name);
                if (clip.loopTime != loop || !clip.loopPose || !clip.lockRootRotation ||
                    !clip.lockRootHeightY || !clip.lockRootPositionXZ)
                    reimport = true;

                clip.loopTime = loop;
                clip.loopPose = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.heightFromFeet = true;
            }
            importer.clipAnimations = clips;
        }

        if (reimport)
            importer.SaveAndReimport();
    }

    private static RuntimeAnimatorController CreateChildController()
    {
        // The Meshy family uses one clean Humanoid locomotion source. Deniz's generated walk has
        // the correct forward direction for these rigs, so the old 180-degree root correction is
        // deliberately not applied.
        AnimationClip childIdle = CreateCleanClipCopy(
            FindClip(GeneralPath, "Idle_A"),
            ChildNeutralIdlePath,
            "ChildNeutralIdle",
            true);
        AnimationClip meshyWalk = MeshyFamilyCharacterImporter.LoadPrimaryAnimationClip(
            MeshyFamilyCharacterImporter.DenizWalkingPath);
        if (meshyWalk == null || !meshyWalk.isHumanMotion)
            throw new InvalidOperationException("Meshy Deniz Humanoid yürüyüş klibi bulunamadı.");
        AnimationClip childWalk = CreateCleanClipCopy(
            meshyWalk, ChildNaturalWalkPath, "ChildNaturalWalk");
        AnimationClip crouching = FindClip(MovementAdvancedPath, "Crouching");
        AnimationClip coverUpperPose = CreateFrozenPose(
            FindClip(SimulationPath, "Waving"), ChildCoverUpperPosePath, "ChildCoverUpperPose", 0.25f);
        AuthorProtectiveUpperBodyPose(coverUpperPose);
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        string[] requiredStates =
        {
            "Locomotion", "Interact", "Pick Up", "Inspect", "Call Sibling", "Startle",
            "Recover Balance", "Crouch", "Protect Head", "Hold Cover", "Work"
        };
        if (IsControllerComplete(existing, requiredStates) &&
            StateGraphUsesMotion(existing, "Locomotion", "ChildNeutralIdle") &&
            StateGraphUsesMotion(existing, "Locomotion", "ChildNaturalWalk") &&
            StateGraphUsesMotionAtSpeed(
                existing,
                "Locomotion",
                "ChildNaturalWalk",
                ChildWalkPlaybackSpeed) &&
            StateUsesFootIk(existing, "Locomotion") &&
            StoryTransitionsRequireStationaryCharacter(existing) &&
            !StateGraphUsesMotion(existing, "Locomotion", "Walking_A") &&
            !StateGraphUsesMotion(existing, "Locomotion", "Walking_B") &&
            !StateGraphUsesMotion(existing, "Locomotion", "boy_move_walk") &&
            StateUsesMotion(existing, "Protect Head", "Crouching") &&
            StateUsesMotion(existing, "Hold Cover", "Crouching") &&
            HasChildCoverUpperBodyLayer(existing))
            return existing;
        if (existing != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        foreach (string trigger in RequiredTriggers)
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree locomotionTree, 0);
        locomotion.iKOnFeet = true;
        locomotionTree.blendType = BlendTreeType.Simple1D;
        locomotionTree.blendParameter = "Speed";
        locomotionTree.useAutomaticThresholds = false;
        locomotionTree.AddChild(childIdle, 0f);
        // Values above 1 remain on the same deliberate walking motion instead of selecting a run.
        locomotionTree.AddChild(childWalk, 1f);
        ChildMotion[] locomotionChildren = locomotionTree.children;
        for (int index = 0; index < locomotionChildren.Length; index++)
        {
            if (locomotionChildren[index].motion == childWalk)
                locomotionChildren[index].timeScale = ChildWalkPlaybackSpeed;
        }
        locomotionTree.children = locomotionChildren;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.defaultState = locomotion;

        AddStoryState(stateMachine, locomotion, "Interact", "StoryInteract", FindClip(GeneralPath, "Interact"), false);
        AddStoryState(stateMachine, locomotion, "Pick Up", "StoryPickUp", FindClip(GeneralPath, "PickUp"), false);
        AddStoryState(stateMachine, locomotion, "Inspect", "StoryInspect", FindClip(UniversalPath, "Armature|Interact"), false);
        AddStoryState(stateMachine, locomotion, "Call Sibling", "StoryCall", FindClip(SimulationPath, "Waving"), false);
        AddStoryState(stateMachine, locomotion, "Startle", "StoryFear", FindClip(GeneralPath, "Hit_A"), false);
        AddStoryState(stateMachine, locomotion, "Recover Balance", "StoryDizzy", FindClip(GeneralPath, "Hit_B"), false);
        AddStoryState(stateMachine, locomotion, "Crouch", "StoryCrouch", crouching, true);
        AddStoryState(stateMachine, locomotion, "Protect Head", "StoryCover", crouching, true);
        AddStoryState(stateMachine, locomotion, "Hold Cover", "StoryHold", crouching, true);
        AddStoryState(stateMachine, locomotion, "Work", "StoryWork", FindClip(ToolsPath, "Working_A"), false);

        AddResetTransition(stateMachine, locomotion);
        AddChildCoverUpperBodyLayer(controller, coverUpperPose);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static RuntimeAnimatorController CreateAdultController()
    {
        AnimationClip adultIdle = CreateCleanClipCopy(
            FindClip(GeneralPath, "Idle_A"), AdultNeutralIdlePath, "AdultNeutralIdle", true);
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AdultControllerPath);
        string[] requiredStates =
        {
            "Adult Idle", "Adult Interact", "Adult Pick Up", "Adult Inspect", "Adult Call",
            "Adult Startle", "Adult Recover", "Adult Crouch", "Adult Cover", "Adult Hold", "Adult Work"
        };
        if (IsControllerComplete(existing, requiredStates) &&
            StateUsesMotion(existing, "Adult Idle", "AdultNeutralIdle") &&
            StateUsesMotion(existing, "Adult Interact", "Interact") &&
            StateUsesMotion(existing, "Adult Pick Up", "PickUp") &&
            StateUsesMotion(existing, "Adult Work", "Working_B"))
            return existing;
        if (existing != null)
            AssetDatabase.DeleteAsset(AdultControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AdultControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        foreach (string trigger in RequiredTriggers)
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Adult Idle");
        idle.motion = adultIdle;
        idle.writeDefaultValues = true;
        stateMachine.defaultState = idle;

        // RGPoly yetişkin iskeletinin kalçası çocuk iskeletinden geniş. Çocuk idle klibindeki ayak IK
        // hedefleri doğrudan retarget edilince bacaklar gereksiz açılıyordu. Yetişkin idle'ı aynı güvenilir
        // klibin kök hareketi korunmuş, yalnız yatay ayak açıklığı ve kalça dışa açısı azaltılmış kopyasıdır.
        AddStoryState(stateMachine, idle, "Adult Interact", "StoryInteract",
            FindClip(GeneralPath, "Interact"), false);
        AddStoryState(stateMachine, idle, "Adult Pick Up", "StoryPickUp",
            FindClip(GeneralPath, "PickUp"), false);
        AddStoryState(stateMachine, idle, "Adult Inspect", "StoryInspect",
            FindClip(GeneralPath, "Interact"), false);
        AddStoryState(stateMachine, idle, "Adult Call", "StoryCall",
            FindClip(SimulationPath, "Waving"), false);
        AddStoryState(stateMachine, idle, "Adult Startle", "StoryFear",
            FindClip(GeneralPath, "Hit_A"), false);
        AddStoryState(stateMachine, idle, "Adult Recover", "StoryDizzy",
            FindClip(GeneralPath, "Hit_B"), false);
        AddStoryState(stateMachine, idle, "Adult Crouch", "StoryCrouch",
            FindClip(MovementAdvancedPath, "Crouching"), true);
        AddStoryState(stateMachine, idle, "Adult Cover", "StoryCover",
            FindClip(MovementAdvancedPath, "Crouching"), true);
        AddStoryState(stateMachine, idle, "Adult Hold", "StoryHold",
            FindClip(ToolsPath, "Working_B"), true);
        AddStoryState(stateMachine, idle, "Adult Work", "StoryWork",
            FindClip(ToolsPath, "Working_B"), false);
        AddResetTransition(stateMachine, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static bool IsControllerComplete(AnimatorController controller, IReadOnlyCollection<string> requiredStates)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        string[] parameters = controller.parameters.Select(parameter => parameter.name).ToArray();
        if (!parameters.Contains("Speed") || RequiredTriggers.Any(trigger => !parameters.Contains(trigger)))
            return false;

        string[] states = controller.layers[0].stateMachine.states.Select(child => child.state.name).ToArray();
        return requiredStates.All(states.Contains);
    }

    private static bool StateUsesMotion(AnimatorController controller, string stateName, string motionName)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
        return state != null && state.motion != null && state.motion.name == motionName;
    }

    private static bool StateGraphUsesMotion(AnimatorController controller, string stateName, string motionName)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
        return state != null && MotionGraphUsesMotion(state.motion, motionName);
    }

    private static bool StateGraphUsesMotionAtSpeed(
        AnimatorController controller,
        string stateName,
        string motionName,
        float expectedTimeScale)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
        if (state == null || !(state.motion is BlendTree tree))
            return false;

        return tree.children.Any(child =>
            child.motion != null &&
            child.motion.name == motionName &&
            Mathf.Abs(child.timeScale - expectedTimeScale) < 0.001f);
    }

    private static bool StateUsesFootIk(AnimatorController controller, string stateName)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
        return state != null && state.iKOnFeet;
    }

    private static bool StoryTransitionsRequireStationaryCharacter(AnimatorController controller)
    {
        if (controller == null || controller.layers.Length == 0)
            return false;

        AnimatorStateTransition[] storyTransitions = controller.layers[0].stateMachine.anyStateTransitions
            .Where(transition => transition.conditions.Any(condition =>
                condition.parameter != "StoryReset" &&
                RequiredTriggers.Contains(condition.parameter)))
            .ToArray();
        return storyTransitions.Length == RequiredTriggers.Length - 1 &&
               storyTransitions.All(transition => transition.conditions.Any(condition =>
                   condition.parameter == "Speed" &&
                   condition.mode == AnimatorConditionMode.Less &&
                   condition.threshold <= 0.08f));
    }

    private static bool HasChildCoverUpperBodyLayer(AnimatorController controller)
    {
        if (controller == null || controller.layers.Length < 2)
            return false;

        AnimatorControllerLayer layer = controller.layers
            .FirstOrDefault(candidate => candidate.name == "Cover Upper Body");
        if (layer == null || layer.avatarMask == null ||
            layer.avatarMask.name != "ChildCoverUpperBody" ||
            layer.syncedLayerIndex != 0 ||
            layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body) ||
            !layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head) ||
            !layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm) ||
            !layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm))
            return false;

        AnimatorStateMachine baseStateMachine = controller.layers[0].stateMachine;
        AnimatorState protect = baseStateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Protect Head");
        AnimatorState hold = baseStateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Hold Cover");
        Motion protectOverride = protect != null ? layer.GetOverrideMotion(protect) : null;
        Motion holdOverride = hold != null ? layer.GetOverrideMotion(hold) : null;
        return protectOverride != null && holdOverride != null &&
               protectOverride.name == "ChildCoverUpperPose" &&
               holdOverride.name == "ChildCoverUpperPose";
    }

    private static void AddChildCoverUpperBodyLayer(AnimatorController controller, AnimationClip coverUpperPose)
    {
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(ChildCoverMaskPath);
        if (mask == null)
        {
            mask = new AvatarMask { name = "ChildCoverUpperBody" };
            AssetDatabase.CreateAsset(mask, ChildCoverMaskPath);
        }

        for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
        // Keep hips, spine and chest on the crouching base layer. Enabling Body here lets the
        // standing source clip overwrite the crouch and produces the twisted tip-toe pose that
        // prompted this regression pass.
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
        EditorUtility.SetDirty(mask);

        controller.AddLayer("Cover Upper Body");
        AnimatorControllerLayer[] layers = controller.layers;
        AnimatorControllerLayer layer = layers[layers.Length - 1];
        layer.defaultWeight = 1f;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.avatarMask = mask;
        layer.syncedLayerIndex = 0;

        AnimatorStateMachine baseStateMachine = layers[0].stateMachine;
        foreach (AnimatorState state in baseStateMachine.states.Select(child => child.state))
        {
            if (state != null && (state.name == "Protect Head" || state.name == "Hold Cover"))
                layer.SetOverrideMotion(state, coverUpperPose);
        }

        layers[layers.Length - 1] = layer;
        controller.layers = layers;
    }

    private static AnimationClip CreateFrozenPose(AnimationClip source, string assetPath, string clipName,
        float normalizedTime)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip == null)
        {
            clip = UnityEngine.Object.Instantiate(source);
            clip.name = clipName;
            AssetDatabase.CreateAsset(clip, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(source, clip);
            clip.name = clipName;
        }

        float sampleTime = Mathf.Clamp01(normalizedTime) * source.length;
        float poseLength = Mathf.Max(1f / 30f, source.length);
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
            if (sourceCurve == null)
                continue;

            float value = sourceCurve.Evaluate(sampleTime);
            AnimationCurve frozen = AnimationCurve.Constant(0f, poseLength, value);
            frozen.preWrapMode = WrapMode.ClampForever;
            frozen.postWrapMode = WrapMode.ClampForever;
            AnimationUtility.SetEditorCurve(clip, binding, frozen);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AuthorProtectiveUpperBodyPose(AnimationClip clip)
    {
        // The source wave only raises a straight arm and reads as both hands resting on the floor
        // once combined with the crouch. Shape the humanoid muscles into an unmistakable
        // head-and-neck cover: elbows forward, forearms folded back over the crown.
        SetConstantMuscleCurve(clip, "Spine Front-Back", 0.28f);
        SetConstantMuscleCurve(clip, "Chest Front-Back", 0.34f);
        SetConstantMuscleCurve(clip, "UpperChest Front-Back", 0.24f);
        SetConstantMuscleCurve(clip, "Head Nod Down-Up", -0.32f);

        SetConstantMuscleCurve(clip, "Left Shoulder Down-Up", 0.25f);
        SetConstantMuscleCurve(clip, "Left Shoulder Front-Back", -0.24f);
        SetConstantMuscleCurve(clip, "Left Arm Down-Up", 0.82f);
        SetConstantMuscleCurve(clip, "Left Arm Front-Back", -0.38f);
        SetConstantMuscleCurve(clip, "Left Arm Twist In-Out", -0.42f);
        SetConstantMuscleCurve(clip, "Left Forearm Stretch", -0.92f);
        SetConstantMuscleCurve(clip, "Left Forearm Twist In-Out", -0.18f);
        SetConstantMuscleCurve(clip, "Left Hand Down-Up", -0.18f);
        SetConstantMuscleCurve(clip, "Left Hand In-Out", 0.08f);

        SetConstantMuscleCurve(clip, "Right Shoulder Down-Up", 0.25f);
        SetConstantMuscleCurve(clip, "Right Shoulder Front-Back", -0.24f);
        SetConstantMuscleCurve(clip, "Right Arm Down-Up", 0.82f);
        SetConstantMuscleCurve(clip, "Right Arm Front-Back", -0.38f);
        SetConstantMuscleCurve(clip, "Right Arm Twist In-Out", 0.42f);
        SetConstantMuscleCurve(clip, "Right Forearm Stretch", -0.92f);
        SetConstantMuscleCurve(clip, "Right Forearm Twist In-Out", 0.18f);
        SetConstantMuscleCurve(clip, "Right Hand Down-Up", 0.18f);
        SetConstantMuscleCurve(clip, "Right Hand In-Out", -0.08f);
        EditorUtility.SetDirty(clip);
    }

    private static void SetConstantMuscleCurve(AnimationClip clip, string propertyName, float value)
    {
        float poseLength = Mathf.Max(1f / 30f, clip.length);
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName);
        AnimationCurve curve = AnimationCurve.Constant(0f, poseLength, value);
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    private static bool MotionGraphUsesMotion(Motion motion, string motionName)
    {
        if (motion == null)
            return false;
        if (motion.name == motionName)
            return true;
        if (motion is not BlendTree tree)
            return false;
        return tree.children.Any(child => MotionGraphUsesMotion(child.motion, motionName));
    }

    private static AnimationClip CreateChildCoverPose()
    {
        AnimationClip crouching = FindClip(MovementAdvancedPath, "Crouching");
        AnimationClip waving = FindClip(SimulationPath, "Waving");
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ChildCoverPosePath);
        if (clip == null)
        {
            clip = UnityEngine.Object.Instantiate(crouching);
            clip.name = "ChildCoverPose";
            AssetDatabase.CreateAsset(clip, ChildCoverPosePath);
        }
        else
        {
            EditorUtility.CopySerialized(crouching, clip);
            clip.name = "ChildCoverPose";
        }

        // The first wave apex gives a clear hand-over-head silhouette on the actual child avatar.
        float overlayTime = waving.length * 0.25f;
        float poseLength = Mathf.Max(1f / 30f, crouching.length);
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(waving))
        {
            string property = binding.propertyName ?? string.Empty;
            bool humanoidHandIk = property.StartsWith("LeftHandT", StringComparison.Ordinal) ||
                                  property.StartsWith("LeftHandQ", StringComparison.Ordinal) ||
                                  property.StartsWith("RightHandT", StringComparison.Ordinal) ||
                                  property.StartsWith("RightHandQ", StringComparison.Ordinal);
            bool upperLimb = property.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             property.IndexOf("Arm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             property.IndexOf("Forearm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             property.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!upperLimb || humanoidHandIk)
                continue;

            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(waving, binding);
            if (sourceCurve == null)
                continue;

            float value = sourceCurve.Evaluate(overlayTime);
            AnimationCurve poseCurve = AnimationCurve.Constant(0f, poseLength, value);
            poseCurve.preWrapMode = WrapMode.ClampForever;
            poseCurve.postWrapMode = WrapMode.ClampForever;
            AnimationUtility.SetEditorCurve(clip, binding, poseCurve);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(crouching);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateCleanClipCopy(
        AnimationClip source,
        string assetPath,
        string clipName,
        bool anchorHumanoidRoot = false)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip == null)
        {
            clip = UnityEngine.Object.Instantiate(source);
            clip.name = clipName;
            AssetDatabase.CreateAsset(clip, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(source, clip);
            clip.name = clipName;
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        if (anchorHumanoidRoot)
            AnchorHumanoidIdleRoot(clip);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AnchorHumanoidIdleRoot(AnimationClip clip)
    {
        EditorCurveBinding[] rootBindings = AnimationUtility.GetCurveBindings(clip)
            .Where(binding =>
                string.IsNullOrEmpty(binding.path) &&
                (binding.propertyName.StartsWith("RootT.", StringComparison.Ordinal) ||
                 binding.propertyName.StartsWith("RootQ.", StringComparison.Ordinal)))
            .ToArray();
        if (rootBindings.Length == 0)
            throw new InvalidOperationException(clip.name + " has no humanoid root curves to anchor.");

        float duration = Mathf.Max(clip.length, 1f / Mathf.Max(clip.frameRate, 30f));
        foreach (EditorCurveBinding binding in rootBindings.Where(binding =>
                     binding.propertyName.StartsWith("RootT.", StringComparison.Ordinal)))
        {
            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(clip, binding);
            if (sourceCurve == null)
                continue;
            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                AnimationCurve.Constant(0f, duration, sourceCurve.Evaluate(0f)));
        }

        string[] rotationProperties = { "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
        Dictionary<string, EditorCurveBinding> rotationBindings = rootBindings
            .Where(binding => rotationProperties.Contains(binding.propertyName))
            .ToDictionary(binding => binding.propertyName, binding => binding);
        if (rotationProperties.Any(property => !rotationBindings.ContainsKey(property)))
            throw new InvalidOperationException(clip.name + " humanoid RootQ curves are incomplete.");

        float[] anchoredComponents =
        {
            AnimationUtility.GetEditorCurve(clip, rotationBindings["RootQ.x"]).Evaluate(0f),
            AnimationUtility.GetEditorCurve(clip, rotationBindings["RootQ.y"]).Evaluate(0f),
            AnimationUtility.GetEditorCurve(clip, rotationBindings["RootQ.z"]).Evaluate(0f),
            AnimationUtility.GetEditorCurve(clip, rotationBindings["RootQ.w"]).Evaluate(0f)
        };
        for (int index = 0; index < rotationProperties.Length; index++)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                rotationBindings[rotationProperties[index]],
                AnimationCurve.Constant(0f, duration, anchoredComponents[index]));
        }
    }

    private static void NormalizeHumanoidRootFacing(AnimationClip clip, float yawCorrection)
    {
        // Quaternius' Walk_Loop is authored with a roughly 180 degree humanoid RootQ yaw.
        // NavMesh correctly rotates the character GameObject toward its velocity, but that
        // baked RootQ then turns the rendered body back around. Correct only the root facing;
        // every muscle curve (including the legs) remains untouched.
        string[] properties = { "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
        Dictionary<string, EditorCurveBinding> bindings = AnimationUtility.GetCurveBindings(clip)
            .Where(binding => string.IsNullOrEmpty(binding.path) && properties.Contains(binding.propertyName))
            .ToDictionary(binding => binding.propertyName, binding => binding);
        if (properties.Any(property => !bindings.ContainsKey(property)))
            throw new InvalidOperationException(clip.name + " humanoid RootQ curves are incomplete.");

        AnimationCurve[] sourceCurves = properties
            .Select(property => AnimationUtility.GetEditorCurve(clip, bindings[property]))
            .ToArray();
        List<Keyframe>[] correctedKeys = properties.Select(_ => new List<Keyframe>()).ToArray();
        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * Mathf.Max(clip.frameRate, 30f)));
        Quaternion correction = Quaternion.Euler(0f, yawCorrection, 0f);
        Quaternion previous = Quaternion.identity;
        bool hasPrevious = false;

        for (int sample = 0; sample <= sampleCount; sample++)
        {
            float time = clip.length * sample / sampleCount;
            Quaternion root = new Quaternion(
                sourceCurves[0].Evaluate(time),
                sourceCurves[1].Evaluate(time),
                sourceCurves[2].Evaluate(time),
                sourceCurves[3].Evaluate(time));
            float rootMagnitudeSquared = root.x * root.x + root.y * root.y + root.z * root.z + root.w * root.w;
            root = rootMagnitudeSquared > 0.0001f ? Quaternion.Normalize(root) : Quaternion.identity;
            Quaternion corrected = Quaternion.Normalize(correction * root);
            if (hasPrevious && Quaternion.Dot(previous, corrected) < 0f)
                corrected = new Quaternion(-corrected.x, -corrected.y, -corrected.z, -corrected.w);

            correctedKeys[0].Add(new Keyframe(time, corrected.x));
            correctedKeys[1].Add(new Keyframe(time, corrected.y));
            correctedKeys[2].Add(new Keyframe(time, corrected.z));
            correctedKeys[3].Add(new Keyframe(time, corrected.w));
            previous = corrected;
            hasPrevious = true;
        }

        for (int curveIndex = 0; curveIndex < properties.Length; curveIndex++)
        {
            AnimationCurve correctedCurve = new AnimationCurve(correctedKeys[curveIndex].ToArray())
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            for (int keyIndex = 0; keyIndex < correctedCurve.length; keyIndex++)
                correctedCurve.SmoothTangents(keyIndex, 0f);
            AnimationUtility.SetEditorCurve(clip, bindings[properties[curveIndex]], correctedCurve);
        }

        EditorUtility.SetDirty(clip);
    }

    private static void AddResetTransition(AnimatorStateMachine stateMachine, AnimatorState idleState)
    {
        AnimatorStateTransition reset = stateMachine.AddAnyStateTransition(idleState);
        reset.hasExitTime = false;
        reset.duration = 0.12f;
        reset.canTransitionToSelf = false;
        reset.AddCondition(AnimatorConditionMode.If, 0f, "StoryReset");
    }

    private static void AddStoryState(AnimatorStateMachine stateMachine, AnimatorState locomotion, string stateName,
        string triggerName, AnimationClip clip, bool persistent)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = clip;
        state.writeDefaultValues = true;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0.12f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
        enter.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

        if (!persistent)
        {
            AnimatorStateTransition exit = state.AddTransition(locomotion);
            exit.hasExitTime = true;
            exit.exitTime = 0.92f;
            exit.duration = 0.15f;
        }
    }

    private static AnimationClip FindClip(string modelPath, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                                         string.Equals(candidate.name, clipName, StringComparison.Ordinal));
        if (clip == null)
        {
            string available = string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Select(candidate => candidate.name));
            throw new InvalidOperationException($"'{clipName}' klibi bulunamadi ({modelPath}). Mevcut: {available}");
        }
        return clip;
    }

    private static bool ShouldLoop(string clipName)
    {
        string value = clipName ?? string.Empty;
        return value.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Loop", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Crawling", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Crouching", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Sneaking", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Running", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Holding", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("Fixing_Kneeling", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void EnsureSourcesExist()
    {
        List<string> missing = SourceModels.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0)
            throw new FileNotFoundException("Story animasyon kaynaklari eksik: " + string.Join(", ", missing));

        string[] licenses = { KayKitRoot + "/LICENSE.txt", QuaterniusRoot + "/LICENSE.txt" };
        missing = licenses.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0)
            throw new FileNotFoundException("Ucuncu taraf lisans dosyalari eksik: " + string.Join(", ", missing));
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    public static readonly string[] RequiredTriggers =
    {
        "StoryReset", "StoryInteract", "StoryPickUp", "StoryInspect", "StoryCall",
        "StoryFear", "StoryDizzy", "StoryCrouch", "StoryCover", "StoryHold", "StoryWork"
    };
}
