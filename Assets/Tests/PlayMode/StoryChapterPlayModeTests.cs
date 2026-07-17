using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class StoryChapterPlayModeTests
{
    [UnityTest]
    public IEnumerator HomeSafety_CompletesPhysicalRiskReductionWithoutSoftlock()
    {
        yield return LoadFreshScene("Story_02_HomeSafety");
        MonoBehaviour director = FindBehaviour("StoryHomeSafetyDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");

        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "Inspecting");

        Invoke(director, "TryUnsafeHeavyLift");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "Inspecting");

        foreach (string method in new[] { "InspectWardrobe", "InspectShelf", "InspectExit" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "ClearingExit");

        foreach (string method in new[] { "MoveExitShoes", "MoveExitToy", "MoveExitParcel" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "LoweringShelfItems");
        AssertFlag(manager, "ExitCleared", true);
        Assert.That(Find("ExitShoes_Start").activeSelf, Is.False);
        Assert.That(Find("ExitShoes_Stored").activeSelf, Is.True);

        foreach (string method in new[] { "LowerShelfBooks", "LowerShelfVase", "LowerShelfFrame" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "SecuringShelf");
        Invoke(director, "HandShelfBracket");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertFlag(manager, "ShelfSecured", true);
        AssertStage(director, "SecuringWardrobe");
        Assert.That(Find("ShelfWallBracket").activeSelf, Is.True);

        Invoke(director, "TestWardrobe");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "MarkWardrobeAnchors");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "HandWardrobeStrap");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertFlag(manager, "WardrobeSecured", true);
        AssertStage(director, "TestingExit");
        Assert.That(Find("WardrobeAnchorStrap").activeSelf, Is.True);

        Invoke(director, "TestExitDoor");
        float timeout = Time.realtimeSinceStartup + 6f;
        while (Property(director, "Stage").GetValue(director).ToString() != "Completed" &&
               Time.realtimeSinceStartup < timeout)
            yield return null;
        AssertStage(director, "Completed");
        if ((bool)Property(ui, "SubtitleActive").GetValue(ui))
            yield return AdvanceSubtitlesUntilIdle(ui);

        AssertCheckpoint(manager, "HomeSafetyComplete");
        Assert.That(Find("HomeExitDoor_Closed").activeSelf, Is.False);
        Assert.That(Find("HomeExitDoor_Open").activeSelf, Is.True);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator Evacuation_CompletesStairsAftershockNeighborStreetAndAssembly()
    {
        yield return LoadFreshScene("Story_04_Evacuation");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");

        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "CorridorCheck");
        Invoke(director, "InspectCorridor");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "RouteChoice");

        Invoke(director, "TryElevator");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "RouteChoice");
        Invoke(director, "ChooseStairs");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertFlag(manager, "ElevatorAvoided", true);
        AssertStage(director, "UpperStairs");

        Invoke(director, "ReachUpperLanding");
        AssertStage(director, "Aftershock");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "HoldHandrail");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertCheckpoint(manager, "AftershockHeld");
        AssertStage(director, "LowerStairs");

        Invoke(director, "ReachLowerLanding");
        AssertStage(director, "HelpingNeighbor");
        Invoke(director, "CallNeighbor");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "MoveNeighborCane");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "ClearLightDebris");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "GuideNeighbor");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertFlag(manager, "NeighborAssisted", true);
        AssertStage(director, "BuildingExit");

        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        Invoke(player, "Warp", new Vector3(0f, 0.02f, 20f));
        Invoke(director, "OpenBuildingExit");
        yield return WaitForSubtitle(ui, 7f);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "StreetRoute");
        Assert.That(Find("BuildingExitDoor_Closed").activeSelf, Is.False);
        Assert.That(Find("BuildingExitDoor_Open").activeSelf, Is.True);
        Assert.That(player.transform.position.z, Is.GreaterThan(22.4f),
            "Bina çıkışı tamamlandı denmeden önce Deniz kapının fiziksel olarak dışına yürümüş olmalı.");

        Invoke(director, "TryUnsafeShortcut");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "StreetRoute");
        Invoke(director, "InspectStreetHazard");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "TakeSafeSidewalk");
        MonoBehaviour touch = FindBehaviour("StoryTouchManager");
        Assert.That((bool)Field(touch, "worldNavigationEnabled").GetValue(touch), Is.False,
            "Toplanma alanına yönlendirilmiş yürüyüş sürerken serbest zemin dokunuşu rotayı bozamamalı.");
        yield return WaitForSubtitle(ui, 12f);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "AssemblyChecks");
        Assert.That((bool)Field(touch, "worldNavigationEnabled").GetValue(touch), Is.True);

        foreach (string method in new[] { "ReadAssemblySign", "CheckCan", "CheckNeighbor", "UseWhistle" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "Completed");
        AssertCheckpoint(manager, "AssemblyHeadcountComplete");
        AssertFlag(manager, "StairRouteCompleted", true);
        AssertFlag(manager, "AssemblyHeadcountComplete", true);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator Evacuation_PreparationFlagsSwitchPlacedLightingAndSignalRoutes()
    {
        yield return LoadFreshScene("Story_04_Evacuation");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        yield return AdvanceSubtitlesUntilIdle(ui);

        SetFlag(manager, "BagFlashlight", false);
        SetFlag(manager, "BagWhistle", false);
        InvokeNonPublic(director, "RestoreWorldState");
        InvokeNonPublic(director, "BeginAssemblyChecks");
        Assert.That(Find("DenizFlashlightBeam").activeSelf, Is.False);
        Assert.That(Find("WeakEmergencyLightRoute").activeSelf, Is.True);
        Assert.That(Find("WhistleWorld").activeSelf, Is.False);
        Assert.That(Find("VoiceSignalWorld").activeSelf, Is.True);

        SetFlag(manager, "BagFlashlight", true);
        SetFlag(manager, "BagWhistle", true);
        InvokeNonPublic(director, "RestoreWorldState");
        InvokeNonPublic(director, "BeginAssemblyChecks");
        Assert.That(Find("DenizFlashlightBeam").activeSelf, Is.True);
        Assert.That(Find("WeakEmergencyLightRoute").activeSelf, Is.False);
        Assert.That(Find("WhistleWorld").activeSelf, Is.True);
        Assert.That(Find("VoiceSignalWorld").activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator DialogueClosingTap_RemainsConsumedUntilPointerRelease()
    {
        yield return LoadFreshScene("Story_02_HomeSafety");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");

        Assert.That((bool)Property(ui, "SubtitleActive").GetValue(ui), Is.True);
        InvokeNonPublic(ui, "ResetSubtitleState", false);

        Assert.That((bool)Property(ui, "SubtitleActive").GetValue(ui), Is.False);
        Assert.That((bool)Property(ui, "WorldInputBlocked").GetValue(ui), Is.True,
            "Diyaloğu kapatan basış, aynı karede dünya dokunuşuna dönüşmemeli.");
        Assert.That((bool)Property(player, "StoryInputLocked").GetValue(player), Is.True,
            "Karakter diyalog kapanış basışı tamamen bırakılana kadar kilitli kalmalı.");

        yield return null;
        yield return null;
        Assert.That((bool)Property(ui, "WorldInputBlocked").GetValue(ui), Is.False,
            "Basış bırakıldıktan sonra sonraki bağımsız dünya dokunuşu yeniden açılmalı.");
        Assert.That((bool)Property(player, "StoryInputLocked").GetValue(player), Is.False);
    }

    private static IEnumerator LoadFreshScene(string sceneName)
    {
        MonoBehaviour existing = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
            yield return null;
        }

        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        if (File.Exists(savePath))
            File.Delete(savePath);
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, sceneName + " Build Settings'e eklenmiş olmalı.");
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
    }

    private static IEnumerator WaitForSubtitle(MonoBehaviour ui, float seconds)
    {
        float timeout = Time.realtimeSinceStartup + seconds;
        while (!(bool)Property(ui, "SubtitleActive").GetValue(ui) && Time.realtimeSinceStartup < timeout)
            yield return null;
        Assert.That((bool)Property(ui, "SubtitleActive").GetValue(ui), Is.True,
            "Otomatik yürüyüş hedefe varıp hikâye diyaloğunu başlatmalı.");
    }

    private static IEnumerator AdvanceSubtitlesUntilIdle(MonoBehaviour ui)
    {
        int advanced = 0;
        do
        {
            Assert.That((bool)Property(ui, "SubtitleActive").GetValue(ui), Is.True);
            Assert.That((bool)Property(ui, "WorldInputBlocked").GetValue(ui), Is.True);
            Assert.That((bool)InvokeWithResult(ui, "TryHandlePrimaryTap"), Is.True);
            Assert.That((bool)Property(ui, "SubtitleRevealComplete").GetValue(ui), Is.True);
            Assert.That((bool)InvokeWithResult(ui, "TryHandlePrimaryTap"), Is.True);
            yield return null;
            advanced++;
            Assert.That(advanced, Is.LessThanOrEqualTo(4), "Diyalog callback zinciri sonsuza girmemeli.");
        }
        while ((bool)Property(ui, "SubtitleActive").GetValue(ui));
    }

    private static void SetFlag(MonoBehaviour manager, string flagName, bool value)
    {
        MethodInfo method = manager.GetType().GetMethod("SetFlag", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        Type enumType = method.GetParameters()[0].ParameterType;
        method.Invoke(manager, new[] { Enum.Parse(enumType, flagName), (object)value });
    }

    private static void AssertFlag(MonoBehaviour manager, string flagName, bool expected)
    {
        MethodInfo method = manager.GetType().GetMethod("HasFlag", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        Type enumType = method.GetParameters()[0].ParameterType;
        bool actual = (bool)method.Invoke(manager, new[] { Enum.Parse(enumType, flagName) });
        Assert.That(actual, Is.EqualTo(expected), flagName);
    }

    private static void AssertStage(MonoBehaviour director, string expected)
    {
        Assert.That(Property(director, "Stage").GetValue(director).ToString(), Is.EqualTo(expected));
    }

    private static void AssertCheckpoint(MonoBehaviour manager, string expected)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        object checkpoint = state.GetType().GetField("checkpoint").GetValue(state);
        Assert.That(checkpoint.ToString(), Is.EqualTo(expected));
    }

    private static MonoBehaviour FindBehaviour(string typeName)
    {
        MonoBehaviour result = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == typeName);
        Assert.That(result, Is.Not.Null, typeName);
        return result;
    }

    private static GameObject Find(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
        Assert.That(transform, Is.Not.Null, name);
        return transform.gameObject;
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, name);
        return property;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field;
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static void InvokeNonPublic(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static object InvokeWithResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }
}
