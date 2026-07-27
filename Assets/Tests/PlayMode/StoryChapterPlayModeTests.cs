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
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

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
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");

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

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator RebuildStoryRoute_PreservesOneSessionFlagsAndActBoundariesAcrossFourScenes()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_Rebuild_MainMenu.unity");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        int persistentManagerId = manager.GetInstanceID();

        Invoke(manager, "StartNewStory");
        yield return WaitForScenePath("Assets/Scenes/Story_01_RebuildPreview.unity");
        manager = FindBehaviour("StoryGameManager");
        Assert.That(manager.GetInstanceID(), Is.EqualTo(persistentManagerId));
        AssertCurrentAct(manager, "Preparation");
        yield return WaitForCheckpoint(manager, "PreparationStart");

        SetFlag(manager, "BagFlashlight", true);
        SetFlag(manager, "BagComfortItem", true);
        SetFlag(manager, "BagRadio", true);
        SetFlag(manager, "BagFirstAid", true);
        SetFlag(manager, "BagWater", true);
        SetFlag(manager, "BagWhistle", true);
        SetFlag(manager, "BagBlanket", true);
        SetFlag(manager, "BagDocuments", true);
        InvokeEnum(manager, "CompleteAct", "Preparation");
        Invoke(manager, "ContinueToNextAct");

        yield return WaitForScenePath("Assets/Scenes/Story_02_RebuildPreview.unity");
        manager = FindBehaviour("StoryGameManager");
        Assert.That(manager.GetInstanceID(), Is.EqualTo(persistentManagerId));
        AssertCurrentAct(manager, "HomeSafety");
        yield return WaitForCheckpoint(manager, "HomeSafetyStart");
        AssertCompletedAct(manager, "Preparation", true);
        AssertFlag(manager, "BagFlashlight", true);
        AssertFlag(manager, "BagComfortItem", true);
        AssertStage(FindBehaviour("StoryHomeSafetyDirector"), "Opening");

        SetFlag(manager, "WardrobeSecured", true);
        SetFlag(manager, "ShelfSecured", true);
        SetFlag(manager, "ExitCleared", true);
        InvokeEnum(manager, "CompleteAct", "HomeSafety");
        Invoke(manager, "ContinueToNextAct");

        yield return WaitForScenePath("Assets/Scenes/Story_03_RebuildPreview.unity");
        manager = FindBehaviour("StoryGameManager");
        Assert.That(manager.GetInstanceID(), Is.EqualTo(persistentManagerId));
        AssertCurrentAct(manager, "Quake");
        AssertCheckpoint(manager, "None");
        AssertCompletedAct(manager, "Preparation", true);
        AssertCompletedAct(manager, "HomeSafety", true);
        AssertFlag(manager, "WardrobeSecured", true);
        AssertFlag(manager, "ShelfSecured", true);
        AssertFlag(manager, "ExitCleared", true);
        MonoBehaviour quakeDirector = FindBehaviour("StorySequenceDirector");
        Assert.That(
            Property(quakeDirector, "CurrentPhase").GetValue(quakeDirector).ToString(),
            Is.EqualTo("CalmOpening"),
            "Önceki perdenin sayısal checkpoint değeri deprem perdesini atlatmamalı.");
        Assert.That(Find("Wardrobe_Secured").activeSelf, Is.True);
        Assert.That(Find("Wardrobe_Fallen").activeSelf, Is.False);
        Assert.That(Find("Shelf_Secured").activeSelf, Is.True);
        Assert.That(Find("Shelf_Fallen").activeSelf, Is.False);
        Assert.That(Find("ExitRoute_Cleared").activeSelf, Is.True);
        Assert.That(Find("ExitRoute_ClutteredButPassable").activeSelf, Is.False);
        Assert.That(Find("CanReadingNest_Safe_Story03").activeInHierarchy, Is.True);
        Assert.That(Find("CanReadingNest_RiskImpact_Story03").activeInHierarchy, Is.False);
        InvokeNonPublic(quakeDirector, "ApplyPreparationState", true);
        Assert.That(Find("Can_ComfortToy_PostQuake").activeSelf, Is.True);
        Assert.That(Find("Wardrobe_Fallen").activeSelf, Is.False);
        Assert.That(Find("Shelf_Fallen").activeSelf, Is.False);

        InvokeEnum(manager, "CompleteAct", "Quake");
        Invoke(manager, "ContinueToNextAct");

        yield return WaitForScenePath("Assets/Scenes/Story_04_RebuildPreview.unity");
        manager = FindBehaviour("StoryGameManager");
        Assert.That(manager.GetInstanceID(), Is.EqualTo(persistentManagerId));
        AssertCurrentAct(manager, "Evacuation");
        yield return WaitForCheckpoint(manager, "EvacuationStart");
        AssertCompletedAct(manager, "Preparation", true);
        AssertCompletedAct(manager, "HomeSafety", true);
        AssertCompletedAct(manager, "Quake", true);
        AssertFlag(manager, "BagFlashlight", true);
        AssertFlag(manager, "WardrobeSecured", true);
        AssertStage(FindBehaviour("StoryEvacuationDirector"), "Opening");
        Assert.That(Find("DenizFlashlightBeam").activeSelf, Is.True);
        Assert.That(Find("WeakEmergencyLightRoute").activeSelf, Is.False);
        Assert.That(Find("RadioPrepared_Use").activeSelf, Is.True);
        Assert.That(Find("WorkerMegaphone_Fallback").activeSelf, Is.False);
        Assert.That(Find("FirstAidPrepared_Drag").activeSelf, Is.True);
        Assert.That(Find("CleanCloth_Fallback_Drag").activeSelf, Is.False);
        Assert.That(Find("WaterPrepared_Drag").activeSelf, Is.True);
        Assert.That(Find("StationWaterCup_Fallback_Drag").activeSelf, Is.False);
        Assert.That(Find("BlanketPrepared_Drag").activeSelf, Is.True);
        Assert.That(Find("WindbreakTent_Fallback").activeSelf, Is.False);
        Assert.That(Find("ContactCardPrepared_Drag").activeSelf, Is.True);
        Assert.That(Find("RegistryPencil_Fallback").activeSelf, Is.False);
        Assert.That(Find("WhistleWorld").activeSelf, Is.True);

        InvokeEnum(manager, "CompleteAct", "Evacuation");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        Invoke(ui, "ContinueAfterAct");
        yield return null;
        Assert.That(SceneManager.GetActiveScene().path,
            Is.EqualTo("Assets/Scenes/Story_04_RebuildPreview.unity"));
        Assert.That(Find("ChapterSelectionCard").activeSelf, Is.True);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.False);

        Invoke(ui, "OpenQuakeAct");
        yield return WaitForScenePath("Assets/Scenes/Story_03_RebuildPreview.unity");
        manager = FindBehaviour("StoryGameManager");
        Assert.That(manager.GetInstanceID(), Is.EqualTo(persistentManagerId));
        AssertCurrentAct(manager, "Quake");
        AssertCheckpoint(manager, "None");
        AssertCompletedAct(manager, "Preparation", true);
        AssertCompletedAct(manager, "HomeSafety", true);
        AssertCompletedAct(manager, "Quake", false);
        AssertCompletedAct(manager, "Evacuation", false);
        AssertFlag(manager, "BagComfortItem", true);
        AssertFlag(manager, "WardrobeSecured", true);
    }

    [UnityTest]
    public IEnumerator HomeSafetyRebuild_UsesClearedDoorRiskFootprintsAndPocketedRouteCar()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_02_RebuildPreview.unity");
        MonoBehaviour director = FindBehaviour("StoryHomeSafetyDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");

        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "RouteTesting");

        Invoke(director, "TestInitialRoute");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "ClearingExit");

        foreach (string method in new[] { "MoveExitShoes", "MoveExitToy", "MoveExitParcel" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "NeighborVisit");
        AssertFlag(manager, "ExitCleared", true);
        AssertCheckpoint(manager, "HomeSafetyStart");

        Invoke(director, "OpenDoorForNermin");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "ReturnNerminEnvelope");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "PlacingNeighborPlan");
        Invoke(director, "PlaceEvacuationPlan");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "MappingRiskZones");
        Assert.That(Find("Nermin_Neighbor").activeSelf, Is.False);

        Invoke(director, "InspectShelf");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That(Find("ShelfFallZone_Unstable").activeSelf, Is.True);
        AssertStage(director, "MappingRiskZones");
        Invoke(director, "InspectWardrobe");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That(Find("WardrobeFallZone_Unstable").activeSelf, Is.True);
        AssertStage(director, "MappingRiskZones");
        Assert.That(Find("CanReadingNest_Risk").activeSelf, Is.True);
        Assert.That(Find("CanReadingNest_Safe").activeSelf, Is.False);
        Invoke(director, "InspectExit");
        Assert.That(Find("CanReadingNest_Risk").activeSelf, Is.False);
        Assert.That(Find("CanReadingNest_Safe").activeSelf, Is.True);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "LoweringShelfItems");
        AssertCheckpoint(manager, "HomeExitCleared");

        foreach (string method in new[] { "LowerShelfBooks", "LowerShelfVase", "LowerShelfFrame" })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "MarkingShelfAnchor");
        Invoke(director, "MarkShelfAnchor");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "HandShelfBracket");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "TestingShelf");
        Invoke(director, "TestSecuredShelf");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That(Find("ShelfFallZone_Unstable").activeSelf, Is.False);
        Assert.That(Find("ShelfFallZone_Stabilized").activeSelf, Is.True);

        Invoke(director, "TestWardrobe");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "MarkWardrobeAnchors");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "HandWardrobeStrap");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "TestingWardrobe");
        Invoke(director, "TestSecuredWardrobe");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That(Find("WardrobeFallZone_Unstable").activeSelf, Is.False);
        Assert.That(Find("WardrobeFallZone_Stabilized").activeSelf, Is.True);
        AssertStage(director, "TestingExit");

        Invoke(director, "TestExitDoor");
        float timeout = Time.realtimeSinceStartup + 12f;
        while (Property(director, "Stage").GetValue(director).ToString() != "Completed" &&
               Time.realtimeSinceStartup < timeout)
            yield return null;
        AssertStage(director, "Completed");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertCheckpoint(manager, "HomeSafetyComplete");
        Assert.That(Find("Can_ToyCar_FinalFinish").activeSelf, Is.False);
        Assert.That(Find("Can_ToyCar_Pocket").activeSelf, Is.True);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator QuakeRebuild_CompletesCoverEquipmentCorridorAndAftershockWithoutSoftlock()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_03_RebuildPreview.unity");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");

        Field(director, "quakeMinimumDuration").SetValue(director, 0.05f);
        Field(director, "postQuakeSettleDuration").SetValue(director, 0.01f);
        Field(director, "corridorWarningDuration").SetValue(director, 0.05f);
        Field(director, "revisedQuakeDelayAfterFamilyMoment").SetValue(director, 0.05f);
        yield return AdvanceSubtitlesUntilIdle(ui);

        MonoBehaviour[] requiredIntro = ((Array)Field(director, "introInspections").GetValue(director))
            .Cast<MonoBehaviour>()
            .ToArray();
        MonoBehaviour[] optionalIntro = ((Array)Field(director, "introOptionalMoments").GetValue(director))
            .Cast<MonoBehaviour>()
            .ToArray();
        Assert.That(requiredIntro.Select(item => Property(item, "InteractionId").GetValue(item).ToString()),
            Is.EqualTo(new[] { "quake.intro.wheel", "quake.intro.car" }));
        Assert.That(optionalIntro.Select(item => Property(item, "InteractionId").GetValue(item).ToString()),
            Is.EqualTo(new[] { "quake.intro.radio", "quake.intro.familyplan" }));
        Assert.That(optionalIntro.All(item => (bool)Property(item, "IsAvailable").GetValue(item)), Is.True);

        Invoke(director, "OnIntroInspection");
        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(),
            Is.EqualTo("CalmOpening"));
        yield return new WaitForSeconds(1.45f);
        Assert.That((bool)Property(requiredIntro[1], "IsAvailable").GetValue(requiredIntro[1]), Is.True);
        Invoke(director, "OnIntroInspection");

        float introTimeout = Time.realtimeSinceStartup + 2f;
        while (Property(director, "CurrentPhase").GetValue(director).ToString() != "Quake" &&
               Time.realtimeSinceStartup < introTimeout)
            yield return null;
        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(), Is.EqualTo("Quake"));
        Assert.That(optionalIntro.All(item => !(bool)Property(item, "IsAvailable").GetValue(item)), Is.True);
        Invoke(director, "OnSiblingCalmed");
        Invoke(director, "OnCrouchStep");
        Invoke(director, "OnCoverHeadStep");
        Invoke(director, "OnCoverReached");

        float timeout = Time.realtimeSinceStartup + 4f;
        while (Property(director, "CurrentPhase").GetValue(director).ToString() != "PostQuake" &&
               Time.realtimeSinceStartup < timeout)
            yield return null;
        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(), Is.EqualTo("PostQuake"));
        AssertCheckpoint(manager, "PostQuake");

        for (int i = 0; i < 8; i++)
            Invoke(director, "OnPostQuakeStep");
        Invoke(director, "OnLightPrepared");
        Invoke(director, "OnCorridorReached");
        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(), Is.EqualTo("Corridor"));
        AssertCheckpoint(manager, "CorridorReached");
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < 3; i++)
            Invoke(director, "OnCorridorStep");
        MonoBehaviour parentKnock = FindStoryInteractable("ParentDoorKnockSurface");
        object parentResponse = Property(parentKnock, "OnInteracted").GetValue(parentKnock);
        Invoke(parentResponse, "Invoke");
        yield return null;
        ParticleSystem canWarningDust = Find("CanAftershockWarning_CeilingDust").GetComponent<ParticleSystem>();
        AudioSource canWarningCreak = Find("CanAftershockWarning_CeilingCreak").GetComponent<AudioSource>();
        Assert.That(canWarningDust.isPlaying, Is.True,
            "Ebeveyn cevabı sırasında Can'ın önce fark ettiği tavan tozu sahnede başlamalı.");
        Assert.That(canWarningCreak.isPlaying, Is.True,
            "Can'ın uyarısını başlatan gıcırtı onun bulunduğu koridor tarafında duyulmalı.");
        Animator canAnimator = Find("Can_8").GetComponentInChildren<Animator>(true);
        Assert.That(canAnimator, Is.Not.Null,
            "Can'Ä±n Animator bileÅŸeni chibi rig hiyerarÅŸisinde bulunmalÄ±.");
        int callSiblingHash = Animator.StringToHash("Call Sibling");
        bool sawCanWarningAnimation = false;
        float animationTimeout = Time.realtimeSinceStartup + 1.5f;
        while (!sawCanWarningAnimation && Time.realtimeSinceStartup < animationTimeout)
        {
            sawCanWarningAnimation =
                canAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == callSiblingHash ||
                (canAnimator.IsInTransition(0) &&
                 canAnimator.GetNextAnimatorStateInfo(0).shortNameHash == callSiblingHash);
            if (!sawCanWarningAnimation)
                yield return null;
        }
        Assert.That(sawCanWarningAnimation, Is.True,
            "Can artçıyı yalnız altyazıda değil, otomatik karakter tepkisiyle önce fark etmeli.");
        Invoke(director, "OnCorridorStep");
        timeout = Time.realtimeSinceStartup + 4f;
        while (Property(director, "CurrentPhase").GetValue(director).ToString() != "Completed" &&
               Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(), Is.EqualTo("Completed"));
        Assert.That(Find("Deniz_WornEmergencyBag").activeSelf, Is.True);
        Assert.That(Find("Can_ComfortToy_PostQuake").activeSelf, Is.True);
        Assert.That(Find("ParentVoiceBarrier"), Is.Not.Null);
        Assert.That(Find("StairwellDoor_Story04"), Is.Not.Null);
        Assert.That(
            Find("ExitRoute_Cleared").GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled),
            Is.True,
            "Temiz çıkış sonucu fiziksel boşlukla okunmalı; zeminde oyunvari rota karoları görünmemeli.");
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator EvacuationRebuild_CompletesPhysicalNeighborEquipmentAndVisibleReunion()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_04_RebuildPreview.unity");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        Field(director, "revisedAftershockMinimumDuration").SetValue(director, 0.05f);

        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "CorridorCheck");
        Assert.That(Find("CanComfortToy_Assembly").activeSelf, Is.True);
        MonoBehaviour corridorEntry = FindStoryInteractable("CorridorSafetyThreshold");
        Assert.That((bool)Property(corridorEntry, "WorldSelectable").GetValue(corridorEntry), Is.False);
        InvokeNonPublic(corridorEntry, "OnTriggerEnter", player.GetComponent<Collider>());
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "RouteChoice");
        Invoke(director, "ChooseStairs");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "UpperStairs");

        Invoke(director, "ReachUpperLanding");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "HoldHandrail");
        yield return new WaitForSecondsRealtime(0.08f);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "LowerStairs");

        Invoke(director, "ReachLowerLanding");
        Invoke(director, "CallNeighbor");
        yield return AdvanceSubtitlesUntilIdle(ui);
        foreach (string method in new[]
                 {
                     "MoveNeighborCardboard",
                     "ClearLightDebris",
                     "MoveNeighborCane",
                     "GuideNeighbor"
                 })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        AssertStage(director, "BuildingExit");
        AssertFlag(manager, "NeighborAssisted", true);

        Invoke(player, "Warp", new Vector3(0f, 0.02f, 20f));
        Invoke(director, "OpenBuildingExit");
        yield return WaitForSubtitle(ui, 7f);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "FacadeClear");
        Invoke(director, "MoveAwayFromFacade");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "StreetRoute");

        MonoBehaviour streetRead = FindStoryInteractable("StreetGlassAndLooseSign_Hazard");
        Invoke(streetRead, "CompletePreparedInteraction");
        yield return null;
        Assert.That(Find("LooseFacadeSign").GetComponent<Animation>().isPlaying, Is.True);
        Assert.That(Find("StreetWarningGlassShard_Slide").GetComponent<Animation>().isPlaying, Is.True);
        Assert.That(Find("StreetFacadeDust").GetComponent<ParticleSystem>().isPlaying, Is.True);
        Assert.That(Find("StreetWarning_LooseSignCreak").GetComponent<AudioSource>().isPlaying, Is.True);
        yield return AdvanceSubtitlesUntilIdle(ui);
        Invoke(director, "TakeSafeSidewalk");
        yield return WaitForSubtitle(ui, 12f);
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "AssemblyChecks");
        AssertCheckpoint(manager, "AssemblyAreaReached");
        foreach (string passiveWorldResult in new[]
                 {
                     "AssemblyAreaSign",
                     "RadioPrepared_Use",
                     "WaterPrepared_Drag",
                     "BlanketPrepared_Drag",
                     "ContactCardPrepared_Drag"
                 })
        {
            MonoBehaviour interaction = FindStoryInteractable(passiveWorldResult);
            Assert.That((bool)Property(interaction, "IsAvailable").GetValue(interaction), Is.False,
                passiveWorldResult + " ayrı bir kontrol listesi adımı olarak açılmamalı.");
        }
        Assert.That(Find("RadioTunedIndicator").activeSelf, Is.True);
        Assert.That(Find("RadioPreparedOfficialBroadcast").GetComponent<AudioSource>().isPlaying, Is.True);

        foreach (string method in new[]
                 {
                     "HandNeighborToWorker",
                     "UseFirstAid",
                     "UseWhistle"
                 })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }
        Assert.That(Find("Anne_Assembly_Reunion").activeSelf, Is.True);
        Assert.That(Find("Baba_Assembly_Reunion").activeSelf, Is.True);
        Assert.That(Find("FamilyHeadcountPending_2of4").activeSelf, Is.True);
        Assert.That(Find("FamilyHeadcountComplete_4of4").activeSelf, Is.False);
        Invoke(director, "ReuniteFamily");
        yield return AdvanceSubtitlesUntilIdle(ui);
        AssertStage(director, "Completed");
        AssertCheckpoint(manager, "AssemblyHeadcountComplete");
        AssertFlag(manager, "AssemblyHeadcountComplete", true);
        Assert.That(Find("FamilyHeadcountPending_2of4").activeSelf, Is.False);
        Assert.That(Find("FamilyHeadcountComplete_4of4").activeSelf, Is.True);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator EvacuationRebuild_AllMissingEquipmentUsesSafeFallbacksWithoutSoftlock()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_04_RebuildPreview.unity");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        yield return AdvanceSubtitlesUntilIdle(ui);

        foreach (string flag in new[]
                 {
                     "BagRadio",
                     "BagFirstAid",
                      "BagWater",
                      "BagBlanket",
                      "BagDocuments",
                      "BagWhistle",
                      "BagComfortItem"
                 })
            SetFlag(manager, flag, false);
        InvokeNonPublic(director, "RestoreWorldState");
        InvokeNonPublic(director, "BeginAssemblyChecks");
        yield return AdvanceSubtitlesUntilIdle(ui);

        Assert.That(Find("RadioPrepared_Use").activeSelf, Is.False);
        Assert.That(Find("WorkerMegaphone_Fallback").activeSelf, Is.True);
        Assert.That(Find("FirstAidPrepared_Drag").activeSelf, Is.False);
        Assert.That(Find("CleanCloth_Fallback_Drag").activeSelf, Is.True);
        Assert.That(Find("WaterPrepared_Drag").activeSelf, Is.False);
        Assert.That(Find("StationWaterCup_Fallback_Drag").activeSelf, Is.True);
        Assert.That(Find("BlanketPrepared_Drag").activeSelf, Is.False);
        Assert.That(Find("WindbreakTent_Fallback").activeSelf, Is.True);
        Assert.That(Find("ContactCardPrepared_Drag").activeSelf, Is.False);
        Assert.That(Find("RegistryPencil_Fallback").activeSelf, Is.True);
        Assert.That(Find("FamilyCallCard_Fallback").activeSelf, Is.True);
        Assert.That(Find("CanComfortToy_Assembly").activeSelf, Is.False);
        Assert.That(Find("WorkerRadioLiveIndicator").activeSelf, Is.True);
        Assert.That(Find("WorkerRadioOfficialBroadcast").GetComponent<AudioSource>().isPlaying, Is.True);

        foreach (string method in new[]
                 {
                     "HandNeighborToWorker",
                     "UseStationCloth",
                     "CallFamily",
                     "ReuniteFamily"
                 })
        {
            Invoke(director, method);
            yield return AdvanceSubtitlesUntilIdle(ui);
        }

        AssertStage(director, "Completed");
        AssertCheckpoint(manager, "AssemblyHeadcountComplete");
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }
#endif

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
#if UNITY_EDITOR
        string scenePath = "Assets/Scenes/" + sceneName + ".unity";
        Scene loadedScene = EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loadedScene.IsValid(), Is.True, scenePath);
        yield return null;
#else
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, sceneName + " Build Settings'e eklenmiş olmalı.");
        while (!load.isDone)
            yield return null;
#endif
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator PreparationRebuild_PlanCardUsesItsVisibleWorldTargetWithoutUiAction()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_01_RebuildPreview.unity");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        yield return AdvanceSubtitlesUntilIdle(ui);

        GameObject planCardObject = Find("FamilyMeetingPointCard_Drag");
        MonoBehaviour planCard = planCardObject.GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "StoryInteractable");
        MonoBehaviour draggable = planCardObject.GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "DraggableItem");
        GameObject completedMark = Find("FamilyPlanCompleteMark");
        MonoBehaviour cameras = FindBehaviour("StoryCameraController");
        Invoke(planCard, "SetAvailable", true);
        MethodInfo activateZone = cameras.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == "ActivateZone" && method.GetParameters().Length == 2);
        activateZone.Invoke(cameras, new[]
        {
            Enum.Parse(activateZone.GetParameters()[0].ParameterType, "PreparationParent"),
            (object)true
        });
        yield return null;
        yield return new WaitForSecondsRealtime(0.15f);

        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        Vector2 sourceScreen = camera.WorldToScreenPoint(
            planCardObject.GetComponent<Renderer>().bounds.center);
        Transform gestureTarget = (Transform)Property(planCard, "GestureTarget").GetValue(planCard);
        Vector2 targetScreen = camera.WorldToScreenPoint(gestureTarget.position);
        Assert.That(sourceScreen.x, Is.InRange(0f, Screen.width));
        Assert.That(sourceScreen.y, Is.InRange(0f, Screen.height));
        Assert.That(targetScreen.x, Is.InRange(0f, Screen.width));
        Assert.That(targetScreen.y, Is.InRange(0f, Screen.height));

        float sourceWorldY = planCardObject.transform.position.y;
        Assert.That((bool)InvokeWithResult(draggable, "BeginManagedDrag", sourceScreen), Is.True);
        yield return null;
        Invoke(draggable, "UpdateManagedDrag", targetScreen);
        yield return null;
        Assert.That(planCardObject.transform.position.y, Is.GreaterThan(sourceWorldY + 0.5f),
            "Kart parmak yukarı sürüklendiğinde masa yüksekliğine kilitlenmemeli.");
        Assert.That(Vector3.Dot(planCardObject.transform.up, -camera.transform.forward), Is.GreaterThan(0.9f),
            "Tutulan kart düz masada yatmak yerine kameraya dönüp dik okunmalı.");
        Assert.That((bool)InvokeWithResult(draggable, "EndManagedDrag", targetScreen), Is.True,
            "Kart, kamerada görünen fiziksel pano hedefinin üzerine bırakılınca kabul edilmeli.");

        Invoke(planCard, "CompletePreparedInteraction");
        yield return null;
        Assert.That(planCardObject.activeSelf, Is.False);
        Assert.That(completedMark.activeSelf, Is.True);
        Assert.That(Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Any(component => component != null && component.GetType().Name == "StoryActionButton"), Is.False,
            "Fiziksel kart yerleştirmesi merkez ekran aksiyon butonuna düşmemeli.");
    }

    [UnityTest]
    public IEnumerator PreparationRebuild_FoodAndHealthRequireObjectLevelChecksBeforePacking()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_01_RebuildPreview.unity");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryPreparationDirector");
        yield return AdvanceSubtitlesUntilIdle(ui);

        FieldInfo categoryField = Field(director, "currentCategory");
        categoryField.SetValue(director, Enum.Parse(categoryField.FieldType, "Food"));
        MonoBehaviour waterCheck = FindStoryInteractable("WaterExpiryLabel_Unchecked");
        MonoBehaviour waterItem = FindStoryInteractable("WorldItem_Water");
        Invoke(director, "DiscoverFoodCategory");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That((bool)Property(waterCheck, "IsAvailable").GetValue(waterCheck), Is.True);
        Assert.That((bool)Property(waterItem, "IsAvailable").GetValue(waterItem), Is.False);

        Invoke(waterCheck, "CompletePreparedInteraction");
        yield return null;
        Assert.That(Find("WaterExpiryLabel_Unchecked").activeSelf, Is.False);
        Assert.That(Find("WaterExpiryCheckedState").activeSelf, Is.True);
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That((bool)Property(waterItem, "IsAvailable").GetValue(waterItem), Is.True);

        categoryField.SetValue(director, Enum.Parse(categoryField.FieldType, "Health"));
        MonoBehaviour bandageCheck = FindStoryInteractable("BandageSealInspection");
        MonoBehaviour firstAid = FindStoryInteractable("WorldItem_FirstAid");
        Invoke(director, "DiscoverHealthCategory");
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That((bool)Property(bandageCheck, "IsAvailable").GetValue(bandageCheck), Is.True);
        Assert.That((bool)Property(firstAid, "IsAvailable").GetValue(firstAid), Is.False);

        Invoke(bandageCheck, "CompletePreparedInteraction");
        yield return null;
        Assert.That(Find("BandageSeal_Unchecked").activeSelf, Is.False);
        Assert.That(Find("BandageSealCheckedState").activeSelf, Is.True);
        yield return AdvanceSubtitlesUntilIdle(ui);
        Assert.That((bool)Property(firstAid, "IsAvailable").GetValue(firstAid), Is.True);
    }

    [UnityTest]
    public IEnumerator PreparationRebuild_BlackoutSearchChainsPocketLightTargetsSiblingAndWhistle()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_01_RebuildPreview.unity");

        MonoBehaviour pocket = FindStoryInteractable("Blackout_OpenOuterPocket");
        MonoBehaviour focusPlan = FindStoryInteractable("Blackout_FocusFamilyPlan");
        MonoBehaviour focusTable = FindStoryInteractable("Blackout_FocusSafeTable");
        MonoBehaviour findCan = FindStoryInteractable("Blackout_FindCan");
        MonoBehaviour whistle = FindStoryInteractable("Blackout_Whistle");
        GameObject planBeam = Find("BlackoutBeam_FamilyPlan");
        GameObject tableBeam = Find("BlackoutBeam_SafeTable");
        GameObject canBeam = Find("BlackoutBeam_Can");
        Behaviour playable = Object.FindObjectsByType<Behaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Single(component => component.GetType().Name == "PlayableDirector" &&
                                 component.name == "PreparationBlackoutDrill");

        Invoke(pocket, "SetAvailable", true);
        Invoke(pocket, "CompletePreparedInteraction");
        Assert.That((bool)Property(focusPlan, "IsAvailable").GetValue(focusPlan), Is.True);

        Invoke(focusPlan, "CompletePreparedInteraction");
        Assert.That(planBeam.activeSelf, Is.True);
        Assert.That((bool)Property(focusTable, "IsAvailable").GetValue(focusTable), Is.True);

        Invoke(focusTable, "CompletePreparedInteraction");
        Assert.That(planBeam.activeSelf, Is.False);
        Assert.That(tableBeam.activeSelf, Is.True);
        Assert.That((bool)Property(findCan, "IsAvailable").GetValue(findCan), Is.True);

        Invoke(findCan, "CompletePreparedInteraction");
        yield return null;
        Assert.That(tableBeam.activeSelf, Is.False);
        Assert.That(canBeam.activeSelf, Is.True);
        Assert.That(Property(playable, "state").GetValue(playable).ToString(), Is.EqualTo("Playing"));

        Invoke(whistle.gameObject, "SetActive", true);
        Invoke(whistle, "SetAvailable", true);
        Assert.That((int)Property(whistle, "RequiredGestureCount").GetValue(whistle), Is.EqualTo(3));
        Assert.That((bool)Property(whistle, "IsAvailable").GetValue(whistle), Is.True);
    }

    private static IEnumerator LoadFreshSceneByPath(string scenePath)
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
        Scene loaded = EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loaded.IsValid(), Is.True, scenePath);
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
    }

    private static IEnumerator WaitForScenePath(string scenePath)
    {
        float timeout = Time.realtimeSinceStartup + 12f;
        while (SceneManager.GetActiveScene().path != scenePath &&
               Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
    }

    private static IEnumerator WaitForCheckpoint(MonoBehaviour manager, string expected)
    {
        float timeout = Time.realtimeSinceStartup + 4f;
        while (CheckpointName(manager) != expected &&
               Time.realtimeSinceStartup < timeout)
            yield return null;

        AssertCheckpoint(manager, expected);
    }
#endif

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
        int releaseFrames = 0;
        while (true)
        {
            bool subtitleActive = (bool)Property(ui, "SubtitleActive").GetValue(ui);
            if (!subtitleActive)
            {
                if (!(bool)Property(ui, "WorldInputBlocked").GetValue(ui))
                    break;

                yield return null;
                if ((bool)Property(ui, "SubtitleActive").GetValue(ui))
                {
                    releaseFrames = 0;
                    continue;
                }

                releaseFrames++;
                Assert.That(releaseFrames, Is.LessThanOrEqualTo(4),
                    "Altyazı pointer koruması parmak bırakıldıktan sonra kalkmalı.");
                continue;
            }

            Assert.That((bool)Property(ui, "WorldInputBlocked").GetValue(ui), Is.True);
            Assert.That((bool)InvokeWithResult(ui, "TryHandlePrimaryTap"), Is.True);
            Assert.That((bool)Property(ui, "SubtitleRevealComplete").GetValue(ui), Is.True);
            Assert.That((bool)InvokeWithResult(ui, "TryHandlePrimaryTap"), Is.True);
            yield return null;
            advanced++;
            releaseFrames = 0;
            Assert.That(advanced, Is.LessThanOrEqualTo(10), "Diyalog callback zinciri sonsuza girmemeli.");
        }
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
        Assert.That(CheckpointName(manager), Is.EqualTo(expected));
    }

    private static string CheckpointName(MonoBehaviour manager)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        object checkpoint = state.GetType().GetField("checkpoint").GetValue(state);
        return checkpoint.ToString();
    }

    private static void AssertCurrentAct(MonoBehaviour manager, string expected)
    {
        Assert.That(Property(manager, "CurrentAct").GetValue(manager).ToString(), Is.EqualTo(expected));
    }

    private static void AssertCompletedAct(MonoBehaviour manager, string actName, bool expected)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        IEnumerable completed = (IEnumerable)state.GetType().GetField("completedActs").GetValue(state);
        bool contains = completed.Cast<object>().Any(value => value.ToString() == actName);
        Assert.That(contains, Is.EqualTo(expected), actName);
    }

    private static MonoBehaviour FindBehaviour(string typeName)
    {
        MonoBehaviour result = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == typeName);
        Assert.That(result, Is.Not.Null, typeName);
        return result;
    }

    private static MonoBehaviour FindStoryInteractable(string gameObjectName)
    {
        return Find(gameObjectName).GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "StoryInteractable");
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

    private static void InvokeEnum(object target, string name, string enumValue)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == 1);
        Type enumType = method.GetParameters()[0].ParameterType;
        method.Invoke(target, new[] { Enum.Parse(enumType, enumValue) });
    }

    private static object InvokeWithResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }
}
