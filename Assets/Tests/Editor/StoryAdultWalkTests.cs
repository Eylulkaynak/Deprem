using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YanYana.Editor;
using Object = UnityEngine.Object;

public sealed class StoryAdultWalkTests
{
    private static string lastMetrics;
    private static bool manualRun;

    // Also usable while another editor workflow owns Play Mode; this only creates
    // disposable preview scenes and never enters or exits the player's scene.
    [MenuItem("Tools/Yan Yana/QA/Validate Adult Walking Feet")]
    private static void ValidateFromMenu()
    {
        var report = new List<string>();
        var checks = new StoryAdultWalkTests();
        manualRun = true;
        foreach (string name in new[] { "Derya", "Emre", "Yusuf", "Idil", "Bora" })
        {
            lastMetrics = "";
            try { checks.AdultFeetRemainUprightThroughWalkCycleAndSpeedBlends(name); report.Add("PASS " + lastMetrics); }
            catch (Exception error) { report.Add("FAIL " + name + ": " + lastMetrics + " " + error.Message); }
        }
        try { checks.AdultOverridePreservesStoryPosesAndChildControllers(); report.Add("PASS adult/neighbor assignments, child controllers and story poses"); }
        catch (Exception error) { report.Add("FAIL controller wiring: " + error.Message); }
        Directory.CreateDirectory("ClientExports/YanYana/Reports");
        File.WriteAllLines("ClientExports/YanYana/Reports/adult-walk-validation.txt", report);
        manualRun = false;
        Debug.Log(string.Join("\n", report));
    }
    [TestCase("Derya")]
    [TestCase("Emre")]
    [TestCase("Yusuf")]
    [TestCase("Idil")]
    [TestCase("Bora")]
    public void AdultFeetRemainUprightThroughWalkCycleAndSpeedBlends(string name)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root + "/" + name + ".prefab");
            var actor = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(actor, scene);
            var animator = actor.GetComponentInChildren<Animator>();
            Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), Is.EqualTo(YanYanaAdultLocomotion.ControllerPath));
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.SetFloat("Speed", 0); animator.Play("Duruş ve yürüyüş", 0, 0); animator.Update(0);
            var feet = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
            var toes = new[] { animator.GetBoneTransform(HumanBodyBones.LeftToes), animator.GetBoneTransform(HumanBodyBones.RightToes) };
            // Calibrate a sole frame from the real foot/toe bones in the planted idle pose.
            var soleFrames = Enumerable.Range(0, 2).Select(side => Quaternion.Inverse(feet[side].rotation) *
                Quaternion.LookRotation(Vector3.ProjectOnPlane(toes[side].position - feet[side].position, Vector3.up).normalized, Vector3.up)).ToArray();
            float minimumUp = 1, maximumBank = 0, maximumYaw = 0, maximumFrameTurn = 0, maximumSeam = 0;
            string worstAt = "";
            foreach (float speed in new[] { 0f, .45f, .9f, 1.35f, 1.8f, 2.2f })
            {
                var previous = new Quaternion[2]; var first = new Quaternion[2];
                animator.SetFloat("Speed", speed);
                for (int frame = 0; frame <= 120; frame++)
                {
                    animator.Play("Duruş ve yürüyüş", 0, frame / 120f); animator.Update(0);
                    for (int side = 0; side < 2; side++)
                    {
                        Quaternion sole = feet[side].rotation * soleFrames[side];
                        Vector3 up = sole * Vector3.up;
                        minimumUp = Mathf.Min(minimumUp, up.y);
                        maximumBank = Mathf.Max(maximumBank, Mathf.Abs(Mathf.Atan2(-(sole * Vector3.right).y, up.y) * Mathf.Rad2Deg));
                        maximumYaw = Mathf.Max(maximumYaw, Vector3.Angle(Vector3.ProjectOnPlane(sole * Vector3.forward, Vector3.up), actor.transform.forward));
                        if (frame == 0) first[side] = sole;
                        else if (Quaternion.Angle(previous[side], sole) > maximumFrameTurn)
                        {
                            maximumFrameTurn = Quaternion.Angle(previous[side], sole);
                            worstAt = $"speed={speed} frame={frame} side={side}";
                        }
                        if (frame == 120) maximumSeam = Mathf.Max(maximumSeam, Quaternion.Angle(first[side], sole));
                        previous[side] = sole;
                    }
                }
            }
            lastMetrics = $"{name}: min sole up={minimumUp:F3}; bank={maximumBank:F2}; yaw={maximumYaw:F2}; frame turn={maximumFrameTurn:F2}; seam={maximumSeam:F2}; worst={worstAt}";
            if (!manualRun) TestContext.WriteLine(lastMetrics);
            Assert.That(minimumUp, Is.GreaterThan(.68f), "A shoe must not fold to vertical or turn upside down.");
            Assert.That(maximumBank, Is.LessThan(13f), "The ankle must not roll sideways during a step or speed blend.");
            Assert.That(maximumYaw, Is.LessThan(55f), "The foot must not twist across the direction of travel.");
            Assert.That(maximumFrameTurn, Is.LessThan(15f), "A step must not contain a sudden ankle flip.");
            Assert.That(maximumSeam, Is.LessThan(1f), "Looping must not snap the ankles.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void AdultOverridePreservesStoryPosesAndChildControllers()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(YanYanaAdultLocomotion.ControllerPath);
        var shared = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(YanYanaAdultLocomotion.BaseControllerPath);
        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAdultWalkBuilder.ClipPath);
        Assert.That(controller.runtimeAnimatorController, Is.SameAs(shared));
        foreach (var clip in shared.animationClips)
            Assert.That(controller[clip], Is.SameAs(clip.name == "ChildNaturalWalk" ? walk : clip), clip.name);
        foreach (string name in new[] { "Ada", "Efe" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root + "/" + name + ".prefab");
            Assert.That(prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.SameAs(shared), name);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { YanYanaCharacterVariants.Root }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.SameAs(controller), prefab.name);
        }
    }
}
