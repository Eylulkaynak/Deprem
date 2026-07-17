using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class StoryVerticalSlicePlayModeTests
{
    [UnityTest]
    public IEnumerator StoryScene_BootsWithOneInputOwnerAndCoreRuntimeObjects()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_03_Quake", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, "Story_03_Quake must remain available through Build Settings.");

        while (!load.isDone)
            yield return null;

        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Story_03_Quake"));
        foreach (string objectName in new[]
                 {
                     "STORY_03_QUAKE", "_StorySession", "Deniz_12", "Can_8",
                     "StoryUI", "EventSystem", "Main Camera"
                 })
            Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName);

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(behaviours.Count(item => item != null && item.GetType().Name == "StoryTouchManager"),
            Is.EqualTo(1), "All world input must still have exactly one runtime owner.");
        Assert.That(Camera.main, Is.Not.Null, "The Cinemachine Brain must have an enabled Main Camera.");
    }

    [UnityTest]
    public IEnumerator DialogueStopsCanAsWellAsDeniz()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_03_Quake", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour ui = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");
        MonoBehaviour player = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour follower = behaviours.Single(item => item != null && item.GetType().Name == "StorySiblingFollower");
        Assert.That(ui, Is.Not.Null);
        Assert.That(player, Is.Not.Null);
        Assert.That(follower, Is.Not.Null);

        while ((bool)Property(ui, "SubtitleActive").GetValue(ui))
        {
            Invoke(ui, "TryHandlePrimaryTap");
            Invoke(ui, "TryHandlePrimaryTap");
            yield return null;
        }
        while ((bool)Property(ui, "WorldInputBlocked").GetValue(ui))
            yield return null;

        NavMeshAgent followerAgent = follower.GetComponent<NavMeshAgent>();
        Assert.That(followerAgent.isOnNavMesh, Is.True);
        Assert.That(NavMesh.SamplePosition(player.transform.position + new Vector3(-1.8f, 0f, -0.4f),
            out NavMeshHit canPoint, 2.5f, NavMesh.AllAreas), Is.True);
        followerAgent.Warp(canPoint.position);
        Invoke(follower, "SetFollowing", true);

        MonoBehaviour farTarget = behaviours
            .Where(candidate => candidate != null && candidate.GetType().Name == "StoryInteractable")
            .OrderByDescending(candidate => Vector3.Distance(
                ((Transform)Property(candidate, "InteractionPoint").GetValue(candidate)).position,
                player.transform.position))
            .First();
        Vector3 targetPosition = ((Transform)Property(farTarget, "InteractionPoint").GetValue(farTarget)).position;
        Assert.That((bool)Invoke(player, "TrySetDestination", targetPosition), Is.True);
        yield return new WaitForSecondsRealtime(0.28f);

        Invoke(ui, "ShowSubtitle", "Deniz ve Can konuşma boyunca yerinde kalır.", 10f);
        yield return null;
        Vector3 stoppedSiblingPosition = follower.transform.position;
        yield return new WaitForSecondsRealtime(0.35f);

        Assert.That((bool)Property(player, "StoryInputLocked").GetValue(player), Is.True);
        Assert.That(followerAgent.hasPath, Is.False,
            "Ana hikâye diyaloğu açıldığında Can'ın eski takip rotası da temizlenmeli.");
        Assert.That(Vector3.Distance(follower.transform.position, stoppedSiblingPosition), Is.LessThan(0.025f));
        Animator followerAnimator = follower.GetComponentInChildren<Animator>(true);
        Assert.That(followerAnimator.GetFloat("Speed"), Is.LessThan(0.05f));
    }

    [UnityTest]
    public IEnumerator CoverTriggerUsesCrouchAndKeepsHandsAtHead()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_03_Quake", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        MonoBehaviour sequence = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StorySequenceDirector");
        sequence.enabled = false;

        foreach (string characterName in new[] { "Deniz_12", "Can_8" })
        {
            GameObject character = GameObject.Find(characterName);
            Assert.That(character, Is.Not.Null, characterName);
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, characterName + " Animator");

            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);
            animator.SetLayerWeight(1, 1f);
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("StoryHold");
        }

        yield return new WaitForSecondsRealtime(0.35f);

        foreach (string characterName in new[] { "Deniz_12", "Can_8" })
        {
            GameObject character = GameObject.Find(characterName);
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Hold Cover"), Is.True,
                characterName + " StoryHold trigger must enter the persistent cover state.");
            Assert.That(animator.GetCurrentAnimatorClipInfo(0).Single().clip.name, Is.EqualTo("Crouching"),
                characterName + " lower body must remain in a stable crouch.");
            Assert.That(animator.GetCurrentAnimatorClipInfo(1).Single().clip.name,
                Is.EqualTo("ChildCoverUpperPose"),
                characterName + " synced upper-body protection pose must be active.");
            Assert.That(animator.GetLayerWeight(1), Is.GreaterThan(0.99f));

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.That(head, Is.Not.Null);
            Assert.That(leftHand, Is.Not.Null);
            Assert.That(rightHand, Is.Not.Null);
            float scale = Mathf.Max(0.001f, animator.humanScale);
            float leftDistance = Vector3.Distance(leftHand.position, head.position) / scale;
            float rightDistance = Vector3.Distance(rightHand.position, head.position) / scale;
            Assert.That(Mathf.Max(leftDistance, rightDistance), Is.LessThan(0.95f),
                $"{characterName} must visibly protect the head; left={leftDistance:F3}, right={rightDistance:F3}.");

            CapsuleCollider capsule = character.GetComponent<CapsuleCollider>();
            Assert.That(head.position.y - character.transform.position.y,
                Is.LessThan(capsule.height * 0.78f),
                characterName + " must not pop back into a standing/tip-toe pose under cover.");
        }
    }

    [UnityTest]
    public IEnumerator ChildLocomotionDoesNotReturnToWideGorillaStance()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_03_Quake", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        MonoBehaviour sequence = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StorySequenceDirector");
        sequence.enabled = false;

        foreach (string characterName in new[] { "Deniz_12", "Can_8" })
        {
            GameObject character = GameObject.Find(characterName);
            Animator animator = character?.GetComponentInChildren<Animator>(true);
            Assert.That(character, Is.Not.Null, characterName + " root");
            Assert.That(animator, Is.Not.Null, characterName + " Animator");
            animator.speed = 1f;
            animator.Rebind();
            animator.SetFloat("Speed", 0f);
            animator.Play("Locomotion", 0, 0.25f);
            animator.Update(0f);

            float idleRatio = LateralStanceRatio(animator);
            Assert.That(idleRatio, Is.InRange(0.05f, 0.35f),
                $"{characterName} neutral idle must keep the feet below the hips; ratio={idleRatio:F2}.");
            Transform idleHead = animator.GetBoneTransform(HumanBodyBones.Head);
            Assert.That(idleHead, Is.Not.Null);
            float idleHeadHeight = idleHead.position.y - character.transform.position.y;

            animator.SetFloat("Speed", 1f);
            animator.Play("Locomotion", 0, 0.18f);
            animator.Update(0f);
            float walkRatio = LateralStanceRatio(animator);
            Assert.That(walkRatio, Is.LessThan(0.75f),
                $"{characterName} walk must not retarget into the old wide gorilla stance; ratio={walkRatio:F2}.");
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Assert.That(head, Is.Not.Null);
            Assert.That(head.position.y - character.transform.position.y,
                Is.GreaterThan(idleHeadHeight * 0.88f),
                characterName + " walk must retain the child rig's upright hip and knee posture.");
            Assert.That(animator.GetCurrentAnimatorClipInfo(0)
                    .Any(info => info.clip != null && info.clip.name == "ChildNaturalWalk"),
                Is.True, characterName + " must use the corrected child walk clip.");
        }
    }

    private static float LateralStanceRatio(Animator animator)
    {
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Assert.That(leftFoot, Is.Not.Null);
        Assert.That(rightFoot, Is.Not.Null);
        Assert.That(leftUpperLeg, Is.Not.Null);
        Assert.That(rightUpperLeg, Is.Not.Null);

        Vector3 lateralAxis = Vector3.ProjectOnPlane(animator.transform.right, Vector3.up).normalized;
        float footSeparation = Mathf.Abs(Vector3.Dot(leftFoot.position - rightFoot.position, lateralAxis));
        float hipSeparation = Mathf.Abs(Vector3.Dot(leftUpperLeg.position - rightUpperLeg.position, lateralAxis));
        return footSeparation / Mathf.Max(0.001f, hipSeparation);
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, name);
        return property;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name &&
                                 candidate.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(target, arguments);
    }
}
