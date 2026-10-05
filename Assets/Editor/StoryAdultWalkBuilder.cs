using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>Bakes adult ankle alignment into a Humanoid clip; no runtime bone overrides.</summary>
public static class StoryAdultWalkBuilder
{
    public const string ClipPath = "Assets/Story/Animations/Generated/AdultNaturalWalk.anim";

    [MenuItem("Tools/Deprem Story/Build Adult Walk")]
    public static void Build()
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAnimationLibraryBuilder.ChildNaturalWalkPath);
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAnimationLibraryBuilder.ChildNeutralIdlePath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyFamilyCharacterImporter.Root + "/Prefabs/Baba.prefab");
        if (!source || !idle || !prefab) throw new InvalidOperationException("Adult walk source assets are missing.");
        var scene = EditorSceneManager.NewPreviewScene();
        var graph = PlayableGraph.Create("Bake adult walk");
        HumanPoseHandler handler = null;
        AnimationClip result = null;
        try
        {
            var actor = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(actor, scene);
            var animator = actor.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false; animator.stabilizeFeet = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // GetHumanPose returns world-space body data; SetHumanPose consumes root-local
            // data. Bake with an identity root so the round trip cannot add the prefab yaw.
            animator.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            animator.transform.localScale = Vector3.one;
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer = AnimationMixerPlayable.Create(graph, 2);
            var rest = AnimationClipPlayable.Create(graph, idle);
            var walk = AnimationClipPlayable.Create(graph, source);
            rest.SetApplyFootIK(false); walk.SetApplyFootIK(false);
            graph.Connect(rest, 0, mixer, 0); graph.Connect(walk, 0, mixer, 1);
            AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(mixer);
            graph.Play(); mixer.SetInputWeight(0, 1); graph.Evaluate(0);
            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var feet = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
            var toes = new[] { animator.GetBoneTransform(HumanBodyBones.LeftToes), animator.GetBoneTransform(HumanBodyBones.RightToes) };
            Vector3 facingDirection = Vector3.ProjectOnPlane(
                (toes[0].position - feet[0].position).normalized + (toes[1].position - feet[1].position).normalized, Vector3.up).normalized;
            Quaternion facing = Quaternion.LookRotation(facingDirection, Vector3.up);
            var soleFrames = new Quaternion[2];
            var muscleIndices = new int[2][];
            var curves = new Dictionary<int, List<Keyframe>>();
            for (int side = 0; side < 2; side++)
            {
                Vector3 forward = Vector3.ProjectOnPlane(toes[side].position - feet[side].position, Vector3.up).normalized;
                soleFrames[side] = Quaternion.Inverse(feet[side].rotation) * Quaternion.LookRotation(forward, Vector3.up);
                string prefix = side == 0 ? "Left " : "Right ";
                muscleIndices[side] = new[] { "Lower Leg Twist In-Out", "Foot Up-Down", "Foot Twist In-Out" }
                    .Select(suffix => Array.IndexOf(HumanTrait.MuscleName, prefix + suffix)).ToArray();
                foreach (int index in muscleIndices[side]) curves.Add(index, new List<Keyframe>());
            }
            mixer.SetInputWeight(0, 0); mixer.SetInputWeight(1, 1);
            var pose = new HumanPose();
            int samples = Mathf.CeilToInt(source.length * 60);
            float worstError = 0;
            for (int sample = 0; sample <= samples; sample++)
            {
                float time = source.length * sample / samples;
                walk.SetTime(time); graph.Evaluate(0); handler.GetHumanPose(ref pose);
                for (int side = 0; side < 2; side++)
                {
                    Quaternion sole = feet[side].rotation * soleFrames[side];
                    Vector3 forward = sole * Vector3.forward;
                    float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(forward.y, -1, 1)) * Mathf.Rad2Deg, -15, 40);
                    Quaternion target = facing * Quaternion.Euler(pitch, side == 0 ? -8 : 8, 0) * Quaternion.Inverse(soleFrames[side]);
                    SolveAnkle(handler, ref pose, feet[side], muscleIndices[side], target);
                    worstError = Mathf.Max(worstError, Quaternion.Angle(feet[side].rotation, target));
                }
                foreach (var pair in curves) pair.Value.Add(new Keyframe(time, pose.muscles[pair.Key]));
            }
            if (worstError > 2f)
                throw new InvalidOperationException($"Adult ankle bake did not converge ({worstError:F2} degrees); existing clip preserved.");
            result = UnityEngine.Object.Instantiate(source); result.name = "AdultNaturalWalk";
            foreach (var pair in curves)
            {
                // A short circular filter removes single-frame retargeting jitter and
                // keeps both ends of the loop identical.
                Keyframe[] keys = pair.Value.ToArray();
                for (int key = 0; key <= samples; key++)
                {
                    float sum = 0;
                    int[] weights = { 1, 4, 6, 4, 1 };
                    for (int offset = -2; offset <= 2; offset++)
                        sum += pair.Value[(key + offset + samples) % samples].value * weights[offset + 2];
                    keys[key].value = sum / 16f;
                }
                var curve = new AnimationCurve(keys);
                for (int key = 0; key < curve.length; key++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(result, EditorCurveBinding.FloatCurve("", typeof(Animator), HumanTrait.MuscleName[pair.Key]), curve);
            }
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (saved) EditorUtility.CopySerialized(result, saved);
            else { AssetDatabase.CreateAsset(result, ClipPath); saved = result; result = null; }
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssets();
            Debug.Log($"Adult walk baked; maximum ankle target error {worstError:F2} degrees.");
        }
        finally
        {
            handler?.Dispose(); graph.Destroy();
            if (result) UnityEngine.Object.DestroyImmediate(result);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void SolveAnkle(HumanPoseHandler handler, ref HumanPose pose, Transform foot, int[] indices, Quaternion target)
    {
        for (int iteration = 0; iteration < 24; iteration++)
        {
            handler.SetHumanPose(ref pose);
            Quaternion current = foot.rotation;
            Vector3 error = RotationVector(target * Quaternion.Inverse(current));
            if (error.magnitude < .002f) break;
            var jacobian = Matrix4x4.identity;
            for (int axis = 0; axis < 3; axis++)
            {
                int index = indices[axis]; float value = pose.muscles[index];
                pose.muscles[index] = value + .01f; handler.SetHumanPose(ref pose);
                Vector3 derivative = RotationVector(foot.rotation * Quaternion.Inverse(current)) / .01f;
                jacobian.SetColumn(axis, new Vector4(derivative.x, derivative.y, derivative.z, 0));
                pose.muscles[index] = value;
            }
            Matrix4x4 normal = jacobian.transpose * jacobian;
            normal.m00 += .001f; normal.m11 += .001f; normal.m22 += .001f;
            Vector3 delta = normal.inverse.MultiplyVector(jacobian.transpose.MultiplyVector(error));
            for (int axis = 0; axis < 3; axis++)
                pose.muscles[indices[axis]] = Mathf.Clamp(pose.muscles[indices[axis]] + Mathf.Clamp(delta[axis], -.3f, .3f), -1, 1);
        }
        handler.SetHumanPose(ref pose);
    }

    private static Vector3 RotationVector(Quaternion rotation)
    {
        rotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180) angle -= 360;
        return Mathf.Abs(angle) < .0001f ? Vector3.zero : axis * (angle * Mathf.Deg2Rad);
    }
}
