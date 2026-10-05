using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Uses the existing reflection contract so the runtime remains Assembly-CSharp.
public sealed class ThreeDPolishPlayModeTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static MonoBehaviour Find(string type) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(b => b != null && b.GetType().Name == type);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Private | BindingFlags.Public).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private | BindingFlags.Public).SetValue(obj, value);
    static object Property(object obj, string name) => obj.GetType().GetProperty(name).GetValue(obj);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethods(Private | BindingFlags.Public)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length &&
            m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(matches => matches))
        .Invoke(obj, args);
    static IEnumerator Load(string scene)
    {
        Time.timeScale = 1;
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
        yield return null;
    }
    static IEnumerator Ready(MonoBehaviour session)
    {
        float deadline = Time.realtimeSinceStartup + 8;
        while ((int)Property(session, "ActiveStageIndex") < 0 || (bool)Field(session, "inputLocked"))
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Stage camera did not release input.");
            yield return null;
        }
    }
    [TearDown] public void RestoreClock()
    {
        Time.timeScale = 1; AudioListener.pause = false;
        var journey = Type.GetType("Deprem.Minigames.MinigameScenarioJourney, Assembly-CSharp");
        journey?.GetMethod("Cancel").Invoke(null, null);
    }

    [UnityTest]
    public IEnumerator StreetHazards_ReportAndDetourPreserveDamagedUtilitiesAndLooseRubble()
    {
        yield return Load("Minigame_Evacuation_25D");
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var gas = all.First(b => b.GetType().Name == "StoryInteractable" && (string)Property(b, "InteractionId") == "evac25d.street.03.gas_valve");
        var rubble = all.First(b => b.GetType().Name == "StoryInteractable" && (string)Property(b, "InteractionId") == "evac25d.street.04.rubble_check");
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var open = transforms.First(t => t.name == "GasValve_OPEN");
        var closed = transforms.First(t => t.name == "GasValve_CLOSED");
        var debris = transforms.First(t => t.name == "LooseRubble_BLOCKED");
        foreach (var interaction in new[] { gas, rubble })
        {
            Assert.AreEqual("Tap", Property(interaction, "InteractionGesture").ToString());
            Call(interaction, "SetAvailable", true);
            Assert.IsTrue((bool)Call(interaction, "RequestInteraction", new object[] { null }));
            yield return null;
            Assert.That(interaction.GetComponentsInChildren<Collider>(true).Where(c => c.enabled).All(c => c.name == "HazardResponse"));
        }
        Assert.IsTrue(open.gameObject.activeSelf, "Reporting must not turn a damaged gas valve.");
        Assert.IsFalse(closed.gameObject.activeSelf);
        Assert.IsTrue(debris.gameObject.activeSelf, "Pointing out a safe route must not remove unstable debris.");
    }

    [UnityTest]
    public IEnumerator ScenarioJourney_RequiresCompletionPreservesRetryAndVisitsEveryPhase()
    {
        yield return Load("Minigame_Hub");
        var hub = Find("MinigameHubManager");
        Call(hub, "StartScenarioJourney");
        yield return null;
        Assert.AreEqual("Minigame_EmergencyBagRush", SceneManager.GetActiveScene().name);
        var session = Find("MinigameSessionManager");
        yield return Ready(session);
        Call(session, "ReturnToHub");
        yield return null;
        Assert.AreEqual("Minigame_Hub", SceneManager.GetActiveScene().name, "Leaving an unfinished game must stop the journey.");
        Call(Find("MinigameHubManager"), "StartScenarioJourney");
        yield return null;
        Call(Find("MinigameSessionManager"), "RetryScene");
        yield return null;
        var journey = Type.GetType("Deprem.Minigames.MinigameScenarioJourney, Assembly-CSharp");
        Assert.AreEqual(0, journey.GetProperty("StepIndex").GetValue(null), "Retry discarded the scenario position.");
        string[] scenes = { "Minigame_EmergencyBagRush", "Minigame_RoomSafety", "Minigame_AftershockCover",
            "Minigame_Evacuation_25D", "Minigame_EmergencyCorridor", "Minigame_RubbleSignal" };
        for (int index = 0; index < scenes.Length; index++)
        {
            Assert.AreEqual(scenes[index], SceneManager.GetActiveScene().name);
            session = Find("MinigameSessionManager");
            if ((bool)Property(session, "ExternalResultOnly")) Call(session, "CompleteExternalEvacuation25D");
            else { yield return Ready(session); Call(session, "CompleteAuthoredGame"); }
            var result = (GameObject)Field(session, "resultPanel");
            Assert.IsNotNull(result, "The evacuation game needs a navigable result card.");
            Assert.IsTrue(result.activeInHierarchy);
            var button = result.transform.Find("ResultCard/HubButton").GetComponent<UnityEngine.UI.Button>();
            var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            yield return null;
        }
        Assert.AreEqual("Minigame_Hub", SceneManager.GetActiveScene().name);
        Assert.IsFalse((bool)journey.GetProperty("IsActive").GetValue(null));
    }

    [UnityTest]
    public IEnumerator SharedStage_PauseFreezesTimerAndCancelsHoldWithoutPenalty()
    {
        yield return Load("Minigame_AftershockCover");
        var session = Find("MinigameSessionManager");
        yield return Ready(session);
        object stage = Property(session, "VisualStage");
        object action = ((Array)Field(stage, "actions")).Cast<object>().First(a => (bool)Field(a, "isCorrect"));
        Set(stage, "gesture", Enum.Parse(Field(stage, "gesture").GetType(), "Hold"));
        Set(stage, "holdDurationSeconds", .25f);
        Set(stage, "stageTimeLimitSeconds", .4f);
        Set(session, "stageStartedAt", Time.time);
        int score = (int)Property(session, "Score");
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            var collider = (Collider)Field(action, "targetCollider");
            var camera = (Camera)Field(session, "worldCamera");
            Vector2 point = camera.WorldToScreenPoint(collider.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            InputSystem.Update();
            Call(session, "BeginPointerAction", stage, action, point);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.7f);
            Assert.AreEqual(0, Property(session, "ActiveStageIndex"));
            Assert.AreEqual(score, Property(session, "Score"), "A modal pause charged a timer/hint penalty.");
            Assert.IsFalse((bool)Field(action, "consumed"), "A paused hold completed itself.");
            Assert.IsNull(Field(session, "pointerAction"), "Pause must release the interrupted gesture.");
            Time.timeScale = 1;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            InputSystem.Update();
            Call(session, "Update");
            Assert.AreEqual(score, Property(session, "Score"), "Pause duration leaked into the timer on resume.");
        }
        finally { InputSystem.RemoveDevice(mouse); }
    }

    [UnityTest]
    public IEnumerator ResetStage_CancelsPendingSnapAndDelayedAdvance()
    {
        yield return Load("Minigame_EmergencyBagRush");
        var session = Find("MinigameSessionManager");
        yield return Ready(session);
        object stage = Property(session, "VisualStage");
        object action = ((Array)Field(stage, "actions")).Cast<object>().First(a => (bool)Field(a, "isCorrect"));
        Set(stage, "gesture", Enum.Parse(Field(stage, "gesture").GetType(), "Tap"));
        Set(action, "authoredMoveSeconds", .45f);
        var target = new GameObject("Test snap destination");
        try
        {
            target.transform.position = ((Collider)Field(action, "targetCollider")).transform.position + Vector3.right;
            Set(action, "dragTarget", target.transform);
            Call(session, "BeginPointerAction", stage, action, Vector2.zero);
            Assert.IsNotNull(Field(session, "acceptedActionRoutine"));
            Call(session, "ResetCurrentStage");
            yield return Ready(session);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(0, Field(session, "stageSuccessCount"), "An old snap awarded success after reset.");
            Assert.IsFalse((bool)Field(action, "consumed"));
            Assert.AreEqual(0, Property(session, "ActiveStageIndex"));
            Set(session, "stageTransitionSeconds", .3f);
            Call(session, "CompleteCurrentStage");
            Call(session, "ResetCurrentStage");
            yield return new WaitForSecondsRealtime(.7f);
            Assert.AreEqual(0, Property(session, "ActiveStageIndex"), "An old stage transition survived reset.");
        }
        finally { Object.Destroy(target); }
    }

    [UnityTest]
    public IEnumerator StageCamera_PauseKeepsFrameAndResumesTransition()
    {
        yield return Load("Minigame_RoomSafety");
        var session = Find("MinigameSessionManager");
        yield return Ready(session);
        var camera = (Camera)Field(session, "worldCamera");
        camera.transform.position += Vector3.right;
        Call(session, "ResetCurrentStage");
        Time.timeScale = 0;
        yield return null;
        Vector3 position = camera.transform.position;
        yield return new WaitForSecondsRealtime(.7f);
        Assert.That(Vector3.Distance(position, camera.transform.position), Is.LessThan(.001f));
        Assert.IsTrue((bool)Field(session, "inputLocked"));
        Time.timeScale = 1;
        yield return Ready(session);
        Assert.That(Vector3.Distance(position, camera.transform.position), Is.GreaterThan(.1f));
    }

    [UnityTest]
    public IEnumerator VehicleAndFire_PauseFreezesGameplayAndResumes()
    {
        yield return Load("Story_04_FiretruckRunner");
        var runner = Find("FiretruckRunnerManager");
        Call(runner, "BeginRun");
        yield return new WaitForSecondsRealtime(.1f);
        Time.timeScale = 0;
        yield return null;
        var truck = (Transform)Field(runner, "truck");
        Vector3 position = truck.position;
        float seconds = (float)Property(runner, "RemainingSeconds");
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(Vector3.Distance(position, truck.position), Is.LessThan(.001f));
        Assert.AreEqual(seconds, (float)Property(runner, "RemainingSeconds"), .01f);
        Time.timeScale = 1;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(truck.position.z, Is.GreaterThan(position.z));
        yield return Load("Minigame_FirefighterExtinguish");
        var fire = Find("FirefighterExtinguishManager");
        Time.timeScale = 0;
        yield return null;
        seconds = (float)Property(fire, "RemainingSeconds");
        yield return new WaitForSecondsRealtime(.4f);
        Assert.AreEqual(seconds, (float)Property(fire, "RemainingSeconds"), .01f);
        Time.timeScale = 1;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That((float)Property(fire, "RemainingSeconds"), Is.LessThan(seconds));
    }

    [UnityTest]
    public IEnumerator ReturnDialog_FreezesStoryDialogueAndRestoresExistingPause()
    {
        yield return Load("Deprem_App");
        yield return null;
        var bridge = Find("LearningGameBridge");
        yield return Load("Story_01_RebuildPreview");
        var ui = Find("StoryUIController");
        Call(ui, "ShowSubtitle", "Deniz: Birlikte hazırlanalım.", .5f);
        Call(bridge, "RequestReturn");
        yield return new WaitForSecondsRealtime(1.8f);
        Assert.IsTrue((bool)Property(ui, "SubtitleActive"), "The story advanced behind the return dialog.");
        Assert.IsTrue((bool)Property(ui, "WorldInputBlocked"));
        Assert.IsTrue(AudioListener.pause);
        Assert.IsFalse((bool)Call(ui, "TryHandlePrimaryTap"), "A modal tap advanced the paused story.");
        Call(bridge, "CloseDialog");
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(AudioListener.pause);
        Call(ui, "SetPaused", true);
        Call(bridge, "RequestReturn");
        Call(bridge, "CloseDialog");
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(AudioListener.pause, "Closing the return modal unpaused the game's own pause menu.");
        var panel = (GameObject)Field(ui, "pausePanel");
        foreach (var group in panel.GetComponentsInChildren<CanvasGroup>(true))
            Assert.That(group.alpha, Is.GreaterThan(.9f), "Pause controls froze at transparent alpha.");
        Call(ui, "SetPaused", false);
    }
}
