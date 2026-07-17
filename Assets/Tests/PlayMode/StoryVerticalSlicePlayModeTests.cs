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
