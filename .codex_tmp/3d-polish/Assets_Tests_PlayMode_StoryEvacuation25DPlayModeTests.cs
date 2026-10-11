using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Regression for the strict side-on post-earthquake street journey.
/// It completes 13 physical interactions, two route choices and nine movement legs
/// while guarding against the removed water-diversion concept returning.
/// </summary>
public sealed class StoryEvacuation25DPlayModeTests
{
#if UNITY_EDITOR
    private const string ScenePath = "Assets/Scenes/Minigame_Evacuation_25D.unity";
    private const float SidewalkTopY = 0.28f;
    private const float WalkY = SidewalkTopY + 0.002f;

    [UnityTest, Timeout(180000)]
    public IEnumerator FifteenStageRoute_PlaysToAssemblySuccess()
    {
        Scene loaded = EditorSceneManager.LoadSceneInPlayMode(
            ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loaded.IsValid(), Is.True, ScenePath);
        yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        MonoBehaviour[] allBehaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour[] interactions = allBehaviours
            .Where(item => item != null && item.GetType().Name == "StoryInteractable")
            .ToArray();
        Assert.That(interactions.Length, Is.EqualTo(13),
            "13 direct street interactions + 2 safe-route choices = 15 stages.");
        Assert.That(interactions.Count(item =>
                Property(item, "InteractionGesture").GetValue(item).ToString() == "DragToTarget"),
            Is.EqualTo(4), "Masonry, glass cover, first aid and fallen sign must be real drag puzzles.");
        Assert.That(allBehaviours.Count(item =>
                item != null && item.GetType().Name == "StairPathWalker"),
            Is.EqualTo(18), "Two children need nine authored street movement legs each.");

        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.orthographic, Is.True, "The minigame must remain strict side-on 2.5D.");
        Assert.That(Vector3.Angle(camera.transform.forward, Vector3.forward), Is.LessThan(0.1f));
        Assert.That(camera.orthographicSize, Is.LessThanOrEqualTo(3.4f));

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform RequireTransform(string objectName)
        {
            Transform found = transforms.FirstOrDefault(item => item.name == objectName);
            Assert.That(found, Is.Not.Null, objectName + " is missing from the authored street.");
            return found;
        }

        Transform sidewalk = RequireTransform("StreetSidewalk_Run");
        Transform road = RequireTransform("StreetRoadFront_Run");
        Assert.That(sidewalk.localScale.x, Is.GreaterThanOrEqualTo(85f));
        Assert.That(sidewalk.localScale.z, Is.GreaterThanOrEqualTo(3.8f));
        Assert.That(road.localScale.x, Is.GreaterThanOrEqualTo(85f));
        Assert.That(transforms.Count(item => item.name.StartsWith("DamagedFacadeBuilding_")),
            Is.GreaterThanOrEqualTo(10), "The route needs a continuous damaged city backdrop.");

        string[] forbiddenPlaceholderTokens =
        {
            "DistantCityBlock_", "SafeDistancePad", "AftershockRing_",
            "RubbleSafeMarker", "GroundPad", "TargetChevron", "ChoiceBadge",
            "NeighborSeatCushion"
        };
        Assert.That(transforms.Any(item => forbiddenPlaceholderTokens.Any(token =>
                item.name.Contains(token, System.StringComparison.Ordinal))),
            Is.False, "Debug pads, circular markers or blockout backdrops are visible.");
        Assert.That(interactions.All(item =>
                Property(item, "HighlightRoot").GetValue(item) == null),
            Is.True, "Generic circular objective markers must be removed.");
        string[] allowedWorldLabels = { "YARDIM NOKTASI", "AİLE" };
        Assert.That(allBehaviours
            .Where(item => item != null && item.GetType().Name == "TextMeshPro")
            .All(label => allowedWorldLabels.Contains(
                (string)label.GetType().GetProperty("text")?.GetValue(label))), Is.True,
            "Only believable in-world emergency signage may remain.");

        Assert.That(transforms.Any(item =>
                item.name == "UnsafeWaterTowardElectric" ||
                item.name == "SafeWaterToDrain" ||
                item.name.Contains("WaterValve")),
            Is.False, "The removed water-diversion game leaked back into the scene.");
        Assert.That(interactions.Any(item =>
                ((string)Property(item, "InteractionId").GetValue(item))
                .Contains("water", System.StringComparison.OrdinalIgnoreCase)),
            Is.False);

        foreach (string dragName in new[]
                 {
                     "05_DraggableMasonryBlock",
                     "08_DraggableGlassCoverBoard",
                     "10_DraggableFirstAidKit",
                     "14_DraggableFallenStreetSign"
                 })
        {
            Transform drag = RequireTransform(dragName);
            Renderer[] renderers = drag.GetComponentsInChildren<Renderer>(true)
                .Where(item => item is not ParticleSystemRenderer).ToArray();
            Assert.That(renderers.Length, Is.GreaterThan(0));
            float lowest = renderers.Min(item => item.bounds.min.y);
            Assert.That(Mathf.Abs(lowest - SidewalkTopY), Is.LessThan(0.08f),
                dragName + " is visibly floating above or buried in the sidewalk.");
        }

        foreach (MonoBehaviour walker in allBehaviours
                     .Where(item => item != null && item.GetType().Name == "StairPathWalker"))
        {
            IEnumerable<object> points = ((System.Collections.IEnumerable)Field(walker, "pathPoints")
                .GetValue(walker)).Cast<object>();
            Assert.That(points.Count(), Is.GreaterThanOrEqualTo(3));
            foreach (Transform point in points.Cast<Transform>())
            {
                Assert.That(Mathf.Abs(point.position.y - WalkY), Is.LessThan(0.02f));
                Assert.That(Mathf.Abs(point.position.z), Is.LessThan(0.95f));
            }
        }

        Transform deniz = RequireTransform("Deniz_Street25D_Actor");
        Transform can = RequireTransform("Can_Street25D_Actor");
        float LowestVisiblePoint(Transform actor)
        {
            Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item is not ParticleSystemRenderer &&
                               item is not TrailRenderer && item is not LineRenderer)
                .ToArray();
            Assert.That(renderers.Length, Is.GreaterThan(0), actor.name);
            return renderers.Min(item => item.bounds.min.y);
        }
        void AssertPlanted(Transform actor)
        {
            Assert.That(Mathf.Abs(LowestVisiblePoint(actor) - (SidewalkTopY - 0.004f)),
                Is.LessThan(0.035f), actor.name + " is floating above the sidewalk.");
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.stabilizeFeet, Is.False);
        }

        AssertPlanted(deniz);
        AssertPlanted(can);
        float denizIdleLow = float.PositiveInfinity;
        float denizIdleHigh = float.NegativeInfinity;
        float canIdleLow = float.PositiveInfinity;
        float canIdleHigh = float.NegativeInfinity;
        for (int sample = 0; sample < 8; sample++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            float denizFeet = LowestVisiblePoint(deniz);
            float canFeet = LowestVisiblePoint(can);
            denizIdleLow = Mathf.Min(denizIdleLow, denizFeet);
            denizIdleHigh = Mathf.Max(denizIdleHigh, denizFeet);
            canIdleLow = Mathf.Min(canIdleLow, canFeet);
            canIdleHigh = Mathf.Max(canIdleHigh, canFeet);
        }
        Assert.That(denizIdleHigh - denizIdleLow, Is.LessThan(0.012f),
            "Deniz idle pose bobs vertically like a hover animation.");
        Assert.That(canIdleHigh - canIdleLow, Is.LessThan(0.012f),
            "Can idle pose bobs vertically like a hover animation.");

        MonoBehaviour Find(string id) => interactions.Single(item =>
            (string)Property(item, "InteractionId").GetValue(item) == id);
        IEnumerator CompleteWhenAvailable(string id)
        {
            MonoBehaviour interaction = Find(id);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!(bool)Property(interaction, "IsAvailable").GetValue(interaction) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)Property(interaction, "IsAvailable").GetValue(interaction),
                Is.True, id + " never became available.");
            MethodInfo request = interaction.GetType().GetMethod(
                "RequestInteraction", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(request, Is.Not.Null);
            Assert.That((bool)request.Invoke(interaction, new object[] { null }),
                Is.True, id + " rejected completion.");
            yield return null;
        }

        Time.timeScale = 8f;
        try
        {
            yield return CompleteWhenAvailable("evac25d.street.01.facade_clear");
            yield return CompleteWhenAvailable("evac25d.street.02.aftershock");
            yield return CompleteWhenAvailable("evac25d.street.03.gas_valve");
            yield return CompleteWhenAvailable("evac25d.street.04.rubble_check");
            yield return CompleteWhenAvailable("evac25d.street.05.masonry_drag");

            MonoBehaviour[] choices = allBehaviours
                .Where(item => item != null && item.GetType().Name == "StairChoiceManager")
                .ToArray();
            Assert.That(choices.Length, Is.EqualTo(2));
            MonoBehaviour streetSideChoice = choices.Single(item => item.name.Contains("06_"));
            Assert.That((bool)Field(streetSideChoice, "choicesEnabled").GetValue(streetSideChoice), Is.True);
            InvokeSafeChoice(streetSideChoice);
            yield return null;

            yield return CompleteWhenAvailable("evac25d.street.07.glass_mark");
            yield return CompleteWhenAvailable("evac25d.street.08.glass_cover");
            yield return CompleteWhenAvailable("evac25d.street.09.injured_check");
            yield return CompleteWhenAvailable("evac25d.street.10.first_aid");
            yield return CompleteWhenAvailable("evac25d.street.11.gate");

            MonoBehaviour utilityChoice = choices.Single(item => item.name.Contains("12_"));
            float choiceDeadline = Time.realtimeSinceStartup + 15f;
            while (!(bool)Field(utilityChoice, "choicesEnabled").GetValue(utilityChoice) &&
                   Time.realtimeSinceStartup < choiceDeadline)
                yield return null;
            Assert.That((bool)Field(utilityChoice, "choicesEnabled").GetValue(utilityChoice),
                Is.True, "Utility-pole route choice never opened.");
            InvokeSafeChoice(utilityChoice);
            yield return null;

            yield return CompleteWhenAvailable("evac25d.street.13.fire_lane");
            yield return CompleteWhenAvailable("evac25d.street.14.fallen_sign");
            yield return CompleteWhenAvailable("evac25d.street.15.assembly");

            GameObject success = GameObject.Find("SuccessOverlay_PostQuakeStreet");
            Assert.That(success, Is.Not.Null);
            Assert.That(success.activeInHierarchy, Is.True, "15/15 success overlay did not open.");

            Assert.That(deniz.position.x, Is.GreaterThan(41.7f));
            Assert.That(can.position.x, Is.GreaterThan(41.1f));
            AssertPlanted(deniz);
            AssertPlanted(can);
        }
        finally
        {
            Time.timeScale = 1f;
        }
    }

    private static void InvokeSafeChoice(MonoBehaviour manager)
    {
        IEnumerable<object> targets = ((System.Collections.IEnumerable)Field(manager, "choiceTargets")
            .GetValue(manager)).Cast<object>();
        object safe = targets.Single(target =>
            Field(target, "choice").GetValue(target).ToString() == "WallSide");
        MethodInfo handle = manager.GetType().GetMethod(
            "HandleChoice", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(handle, Is.Not.Null);
        handle.Invoke(manager, new[] { safe });
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, target.GetType().Name + "." + name);
        return property;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
        return field;
    }
#endif
}
