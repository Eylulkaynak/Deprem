using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Minigames;
using Deprem.Story;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MinigamePackageEditorTests
{
    private static readonly string[] AuthoredScenes =
    {
        MinigameSceneCatalog.AftershockCoverPath,
        MinigameSceneCatalog.RoomSafetyPath,
        MinigameSceneCatalog.EmergencyBagRushPath,
        MinigameSceneCatalog.EmergencyCorridorPath,
        MinigameSceneCatalog.RubbleSignalPath
    };

    [Test]
    public void BuildCatalog_HasMainStoryHubAndEightOpenMinigamesInExactOrder()
    {
        string[] enabled = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        Assert.That(enabled, Is.EqualTo(MinigameSceneCatalog.OrderedScenePaths));
        Assert.That(enabled, Has.Length.EqualTo(14));
        Assert.That(MinigameSceneCatalog.OrderedScenePaths.Skip(6).ToArray(), Has.Length.EqualTo(8));
        foreach (string path in enabled)
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null, path);
    }

    [TestCaseSource(nameof(AuthoredSceneCases))]
    public void AuthoredScene_SatisfiesRootsReferencesPortraitAudioAndMobileBudgets(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        string[] requiredRoots =
        {
            "Environment", "Characters", "Gameplay", "Cameras",
            "LightingVFX", "Audio", "UI", "EventSystem"
        };
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (string rootName in requiredRoots)
            Assert.That(roots.Count(root => root.name == rootName), Is.EqualTo(1), scenePath + " / " + rootName);

        MinigameSessionManager session = Object.FindFirstObjectByType<MinigameSessionManager>(FindObjectsInactive.Include);
        Assert.That(session, Is.Not.Null, scenePath);
        Assert.That(session.ExternalResultOnly, Is.False, scenePath);
        Assert.That(session.ValidateConfiguration(out string error), Is.True, scenePath + " / " + error);
        bool isRoomSafety = scenePath == MinigameSceneCatalog.RoomSafetyPath;
        Assert.That(session.StageCount, Is.GreaterThanOrEqualTo(isRoomSafety ? 6 : 7), scenePath);
        Assert.That(Object.FindFirstObjectByType<StorySafeAreaPanel>(FindObjectsInactive.Include), Is.Not.Null, scenePath);
        Assert.That(Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include), Is.Not.Null, scenePath);

        Camera camera = GetPrivate<Camera>(session, "worldCamera");
        Assert.That(camera, Is.Not.Null, scenePath);
        MinigameStageDefinition[] stages = GetPrivate<MinigameStageDefinition[]>(session, "stages");
        Assert.That(stages.Select(stage => stage.stageId).Distinct().Count(), Is.EqualTo(stages.Length),
            scenePath + " / aşama kimlikleri benzersiz olmalı");
        Assert.That(stages.Select(stage => stage.gesture).Distinct().Count(),
            Is.GreaterThanOrEqualTo(isRoomSafety ? 2 : 3),
            scenePath + " / oynanış sahne sözleşmesindeki gesture çeşitliliğini korumalı");

        Vector3 originalCameraPosition = camera.transform.position;
        Quaternion originalCameraRotation = camera.transform.rotation;
        float originalFieldOfView = camera.fieldOfView;
        float originalAspect = camera.aspect;
        foreach (MinigameStageDefinition stage in stages)
        {
            Assert.That(stage, Is.Not.Null, scenePath);
            Assert.That(stage.stageRoot, Is.Not.Null, scenePath + " / " + stage.stageId);
            Assert.That(stage.cameraPose, Is.Not.Null, scenePath + " / " + stage.stageId + " / kamera pozu");
            Assert.That(stage.cameraFieldOfView, Is.InRange(28f, 60f), scenePath + " / " + stage.stageId);
            Assert.That(stage.subtitle, Is.Not.Empty, scenePath + " / " + stage.stageId);
            Assert.That(stage.voiceClip, Is.Not.Null, scenePath + " / " + stage.stageId);
            Assert.That(stage.gestureInstruction, Is.Not.Empty, scenePath + " / " + stage.stageId);
            Assert.That(stage.actions, Is.Not.Empty, scenePath + " / " + stage.stageId);
            Assert.That(stage.actions.All(action => action != null && action.targetCollider != null), Is.True,
                scenePath + " / " + stage.stageId);
            Assert.That(stage.actions.Where(action => action.isCorrect).All(action =>
                    !string.IsNullOrWhiteSpace(action.actionLabel)), Is.True,
                scenePath + " / " + stage.stageId + " / görünür doğru eylem etiketi");
            Assert.That(stage.actions.Count(action => action.isCorrect), Is.GreaterThanOrEqualTo(stage.requiredSuccesses),
                scenePath + " / " + stage.stageId + " / yeterli doğru eylem");
            if (stage.gesture == MinigameGesture.DragToTarget)
                Assert.That(stage.actions.Where(action => action.isCorrect).All(action => action.dragTarget != null), Is.True,
                    scenePath + " / " + stage.stageId + " / sürükleme hedefi");
            if (stage.gesture == MinigameGesture.RepeatedTap)
                Assert.That(stage.actions.Where(action => action.isCorrect)
                    .All(action => action.requiredActivations >= 2), Is.True,
                    scenePath + " / " + stage.stageId + " / tekrarlı dokunma sayısı");

            Assert.That(stage.characterCues, Is.Not.Empty, scenePath + " / " + stage.stageId + " / karakter sunumu");
            Assert.That(stage.characterCues.All(cue => cue != null && cue.characterRoot != null), Is.True,
                scenePath + " / " + stage.stageId + " / karakter referansı");

            camera.transform.SetPositionAndRotation(stage.cameraPose.position, stage.cameraPose.rotation);
            camera.fieldOfView = stage.cameraFieldOfView;
            camera.aspect = 1080f / 1920f;
            foreach (MinigameActionDefinition action in stage.actions)
            {
                AssertWorldPointInPortraitFrame(camera, action.targetCollider.bounds.center,
                    scenePath + " / " + stage.stageId + " / " + action.actionId + " kaynak");
                if (stage.gesture == MinigameGesture.DragToTarget && action.isCorrect)
                    AssertWorldPointInPortraitFrame(camera, action.dragTarget.position,
                        scenePath + " / " + stage.stageId + " / " + action.actionId + " hedef");
            }
        }
        camera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
        camera.fieldOfView = originalFieldOfView;
        camera.aspect = originalAspect;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(lights.Length, Is.LessThanOrEqualTo(3), scenePath);
        Assert.That(lights.Count(light => light.shadows != LightShadows.None), Is.LessThanOrEqualTo(1), scenePath);
        Assert.That(Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .All(animator => !animator.applyRootMotion), Is.True, scenePath);
        Transform characters = roots.Single(root => root.name == "Characters").transform;
        foreach (Transform character in characters)
        {
            Renderer[] characterRenderers = character.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (characterRenderers.Length == 0)
                continue;
            Bounds bounds = characterRenderers[0].bounds;
            foreach (Renderer renderer in characterRenderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            Assert.That(Mathf.Abs(bounds.min.y - character.position.y), Is.LessThanOrEqualTo(0.02f),
                scenePath + " / " + character.name + " ayak-zemin teması");
        }
        Assert.That(Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(renderer => renderer.enabled)
            .Any(renderer => new[] { "Debug", "Placeholder", "TargetDisk", "GroundPad", "DragVolume" }
                .Any(token => renderer.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)), Is.False, scenePath);
    }

    [Test]
    public void NeutralIdleClips_HaveLessThanTwelveMillimetresVerticalRootDrift()
    {
        string[] paths =
        {
            StoryAnimationLibraryBuilder.ChildNeutralIdlePath,
            StoryAnimationLibraryBuilder.AdultNeutralIdlePath
        };
        foreach (string path in paths)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            Assert.That(clip, Is.Not.Null, path);
            EditorCurveBinding[] verticalBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.propertyName.EndsWith(".y", StringComparison.OrdinalIgnoreCase) &&
                    (binding.propertyName.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     binding.propertyName.IndexOf("MotionT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     binding.propertyName.IndexOf("localPosition", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
            foreach (EditorCurveBinding binding in verticalBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float min = curve.keys.Min(key => key.value);
                float max = curve.keys.Max(key => key.value);
                Assert.That(max - min, Is.LessThanOrEqualTo(0.012f), path + " / " + binding.propertyName);
            }
        }
    }

    [Test]
    public void Hub_HasEightPlayableSceneCaptureCardsAndBestResultFields()
    {
        EditorSceneManager.OpenScene(MinigameSceneCatalog.HubPath, OpenSceneMode.Single);
        MinigameHubManager manager = Object.FindFirstObjectByType<MinigameHubManager>(FindObjectsInactive.Include);
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager.CardCount, Is.EqualTo(8));
        MinigameHubCardBinding[] cards = GetPrivate<MinigameHubCardBinding[]>(manager, "cards");
        Assert.That(cards.Select(card => card.minigameId).Distinct().Count(), Is.EqualTo(8));
        Assert.That(cards.Select(card => card.sceneName).ToArray(), Is.EqualTo(new[]
        {
            "Story_04_FiretruckRunner",
            "Minigame_FirefighterExtinguish",
            "Minigame_Evacuation_25D",
            "Minigame_AftershockCover",
            "Minigame_RoomSafety",
            "Minigame_EmergencyBagRush",
            "Minigame_EmergencyCorridor",
            "Minigame_RubbleSignal"
        }));
        foreach (MinigameHubCardBinding card in cards)
        {
            Assert.That(card.playButton, Is.Not.Null, card.minigameId);
            Assert.That(card.playButton.interactable, Is.True, card.minigameId);
            Assert.That(card.resultText, Is.Not.Null, card.minigameId);
            Assert.That(card.starsText, Is.Not.Null, card.minigameId);
            Assert.That(card.coinText, Is.Not.Null, card.minigameId);
            Assert.That(card.timeText, Is.Not.Null, card.minigameId);
            Image capture = card.playButton.transform.parent.Find("SceneCapture")?.GetComponent<Image>();
            Assert.That(capture, Is.Not.Null, card.minigameId);
            Assert.That(capture.sprite, Is.Not.Null, card.minigameId);
            Assert.That(card.playButton.onClick.GetPersistentEventCount(), Is.EqualTo(1), card.minigameId);
            Assert.That(card.playButton.onClick.GetPersistentTarget(0), Is.EqualTo(manager), card.minigameId);
            Assert.That(card.playButton.onClick.GetPersistentMethodName(0),
                Is.EqualTo(nameof(MinigameHubManager.OpenScene)), card.minigameId);
        }
    }

    [Test]
    public void AftershockCover_HasRoomScaleShakeAndVisibleImpactChoreography()
    {
        EditorSceneManager.OpenScene(MinigameSceneCatalog.AftershockCoverPath, OpenSceneMode.Single);
        MinigameSessionManager session = Object.FindFirstObjectByType<MinigameSessionManager>(
            FindObjectsInactive.Include);
        Assert.That(session, Is.Not.Null);
        MinigameStageDefinition[] stages = GetPrivate<MinigameStageDefinition[]>(session, "stages");
        Assert.That(stages, Has.Length.EqualTo(10));

        string[] firstImpactClips = stages[0].playOnEnter
            .Where(animation => animation != null && animation.clip != null)
            .Select(animation => animation.clip.name)
            .ToArray();
        foreach (string expected in new[]
                 {
                     "Aftershock_RoomShake",
                     "Aftershock_CameraShake",
                     "Aftershock_LampSwing",
                     "Aftershock_BookcaseRattle",
                     "Aftershock_TableRattle",
                     "Aftershock_SideTableRattle",
                     "Aftershock_FamilyPhotoFall",
                     "Aftershock_BookA_Fall",
                     "Aftershock_BookB_Fall",
                     "Aftershock_PlantTopple",
                     "Aftershock_MugFallAndBreak",
                     "Aftershock_WindowCrackReveal"
                 })
            Assert.That(firstImpactClips, Does.Contain(expected), expected);

        foreach (int quakeStart in new[] { 0, 3, 6 })
        {
            Assert.That(stages[quakeStart].playOnEnter, Has.Length.GreaterThanOrEqualTo(12),
                stages[quakeStart].stageId);
            Assert.That(stages[quakeStart].onEnter.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(8),
                stages[quakeStart].stageId);
        }
        for (int index = 0; index < 9; index++)
            Assert.That(stages[index].playOnEnter, Has.Length.GreaterThanOrEqualTo(6), stages[index].stageId);

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (string expectedObject in new[]
                 {
                     "AftershockWindowCrack",
                     "AftershockSideTableMug",
                     "AftershockPlasterBurst",
                     "AftershockGlassShards",
                     "AftershockObjectDebris",
                     "AftershockRumble",
                     "AftershockWoodImpact",
                     "AftershockGlassImpact",
                     "AftershockStoneImpact"
                 })
            Assert.That(transforms.Any(item => item.name == expectedObject), Is.True, expectedObject);

        Assert.That(stages[9].onEnter.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(15));
        Assert.That(Enumerable.Range(0, stages[9].onEnter.GetPersistentEventCount())
            .Select(stages[9].onEnter.GetPersistentMethodName), Does.Contain("Stop"));
    }

    [Test]
    public void Progress_UsesOnlyPersonalBestCoinsAndCapsEightGamesAt400()
    {
        string fileName = "minigame-profile-editor-test-" + Guid.NewGuid().ToString("N") + ".json";
        GameObject owner = new GameObject("ProgressTest");
        GameObject reopenedOwner = null;
        MinigameProgressManager progress = owner.AddComponent<MinigameProgressManager>();
        SetPrivate(progress, "profileFileName", fileName);
        SetPrivate(progress, "loadOnAwake", false);
        try
        {
            progress.LoadNow();
            for (int index = 0; index < 8; index++)
            {
                string id = "game-" + index;
                progress.RecordResult(id, 3, 1000, 50, 140f - index);
                progress.RecordResult(id, 1, 600, 30, 180f);
            }
            Assert.That(progress.TotalBestCoins, Is.EqualTo(400));
            Assert.That(progress.GetRecord("game-0").bestCoins, Is.EqualTo(50));
            Assert.That(progress.GetRecord("game-0").completionCount, Is.EqualTo(2));
            Assert.That(progress.GetRecord("game-0").bestTimeSeconds, Is.EqualTo(140f));
            Assert.That(MinigameProgressManager.StarsForScore(900), Is.EqualTo(3));
            Assert.That(MinigameProgressManager.StarsForScore(700), Is.EqualTo(2));
            Assert.That(MinigameProgressManager.CoinsForStars(1), Is.EqualTo(30));

            reopenedOwner = new GameObject("ProgressReopenTest");
            MinigameProgressManager reopened = reopenedOwner.AddComponent<MinigameProgressManager>();
            SetPrivate(reopened, "profileFileName", fileName);
            SetPrivate(reopened, "loaded", false);
            reopened.LoadNow();
            Assert.That(reopened.TotalBestCoins, Is.EqualTo(400));
            Assert.That(reopened.GetRecord("game-0").bestCoins, Is.EqualTo(50));
            Assert.That(reopened.GetRecord("game-0").completionCount, Is.EqualTo(2));
        }
        finally
        {
            if (File.Exists(progress.ProfilePath))
                File.Delete(progress.ProfilePath);
            if (reopenedOwner != null)
                Object.DestroyImmediate(reopenedOwner);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void ExistingMinigames_ReportResultsWithoutReplacingTheirResultScreens()
    {
        EditorSceneManager.OpenScene(MinigameSceneCatalog.FiretruckPath, OpenSceneMode.Single);
        MinigameSessionManager firetruck = Object.FindObjectsByType<MinigameSessionManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(session => session.MinigameId == "firetruck-runner");
        Assert.That(firetruck.ExternalResultOnly, Is.True);

        EditorSceneManager.OpenScene(MinigameSceneCatalog.Evacuation25DPath, OpenSceneMode.Single);
        MinigameSessionManager evacuation = Object.FindObjectsByType<MinigameSessionManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(session => session.MinigameId == "evacuation-25d");
        Assert.That(evacuation.ExternalResultOnly, Is.True);
        StairChoiceManager[] choices = Object.FindObjectsByType<StairChoiceManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(choices, Is.Not.Empty);
        Assert.That(choices.All(choice => choice.onWrongChoice != null &&
            Enumerable.Range(0, choice.onWrongChoice.GetPersistentEventCount()).Any(index =>
                choice.onWrongChoice.GetPersistentTarget(index) == evacuation &&
                choice.onWrongChoice.GetPersistentMethodName(index) == nameof(MinigameSessionManager.ReportExternalMistake))),
            Is.True);
        Assert.That(Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item.InteractionId == "evac25d.street.15.assembly").OnInteracted
            .GetPersistentEventCount(), Is.GreaterThan(0));
    }

    [Test]
    public void DraggableItem_ExposesBackwardCompatibleAcceptedAndRejectedEvents()
    {
        GameObject owner = new GameObject("DraggableEventsTest");
        try
        {
            owner.AddComponent<BoxCollider>();
            DraggableItem item = owner.AddComponent<DraggableItem>();
            Assert.That(item.OnAccepted, Is.Not.Null);
            Assert.That(item.OnRejected, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void RoomSafety_UsesAdultDrillFlowAndContainsNoStrapStages()
    {
        EditorSceneManager.OpenScene(MinigameSceneCatalog.RoomSafetyPath, OpenSceneMode.Single);
        MinigameSessionManager session = Object.FindFirstObjectByType<MinigameSessionManager>(
            FindObjectsInactive.Include);
        Assert.That(session, Is.Not.Null);
        MinigameStageDefinition[] stages = GetPrivate<MinigameStageDefinition[]>(session, "stages");
        Assert.That(stages.Select(stage => stage.stageId).ToArray(), Is.EqualTo(new[]
        {
            "clear-exit",
            "lower-objects",
            "shelf-anchors",
            "wardrobe-anchors",
            "quake-test",
            "review"
        }));

        foreach (string stageId in new[] { "shelf-anchors", "wardrobe-anchors" })
        {
            MinigameStageDefinition stage = stages.Single(candidate => candidate.stageId == stageId);
            Assert.That(stage.gesture, Is.EqualTo(MinigameGesture.Tap), stageId);
            Assert.That(stage.requiredSuccesses, Is.EqualTo(2), stageId);
            Assert.That(stage.playOnSuccess, Has.Length.EqualTo(1), stageId);
            Assert.That(stage.playOnSuccess[0].clip.name, Does.Contain("AdultDrill"), stageId);
            Assert.That(stage.successAnimatorTriggers, Has.Length.EqualTo(1), stageId);
            Assert.That(stage.successAnimatorTriggers[0].triggerName, Is.EqualTo("StoryWork"), stageId);

            AudioSource drillAudio = stage.stageRoot.GetComponentInChildren<AudioSource>(true);
            Assert.That(drillAudio, Is.Not.Null, stageId);
            Assert.That(drillAudio.clip, Is.Not.Null, stageId);
            Assert.That(drillAudio.clip.name, Is.EqualTo("freesound_community-power-drill-90294"), stageId);
            Assert.That(drillAudio.spatialBlend, Is.EqualTo(1f).Within(0.001f), stageId);
            Assert.That(stage.onSuccess.GetPersistentEventCount(), Is.EqualTo(2), stageId);
            Assert.That(Enumerable.Range(0, stage.onSuccess.GetPersistentEventCount())
                .Select(stage.onSuccess.GetPersistentMethodName), Does.Contain("Play"), stageId);
            Assert.That(stage.stageRoot.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name.EndsWith("InstalledDrillHardware", StringComparison.Ordinal)), Is.True, stageId);
        }

        string[] objectNames = stages
            .SelectMany(stage => stage.stageRoot.GetComponentsInChildren<Transform>(true))
            .Select(item => item.name)
            .ToArray();
        foreach (string removedMechanic in new[]
                 {
                     "SafetyStrapCart", "AdultStrapHandoff", "StrapTensionCart",
                     "TensionDemonstrationBoard", "RoomTensionLeft", "RoomTensionRight"
                 })
            Assert.That(objectNames, Does.Not.Contain(removedMechanic), removedMechanic);
    }

    private static object[] AuthoredSceneCases() => AuthoredScenes.Select(path => new object[] { path }).ToArray();

    private static void AssertWorldPointInPortraitFrame(Camera camera, Vector3 worldPoint, string context)
    {
        Vector3 viewport = camera.WorldToViewportPoint(worldPoint);
        Assert.That(viewport.z, Is.GreaterThan(0f), context + " kameranın arkasında");
        Assert.That(viewport.x, Is.InRange(0.04f, 0.96f), context + " yatay portre kadrajı dışında");
        Assert.That(viewport.y, Is.InRange(0.08f, 0.92f), context + " dikey portre kadrajı dışında");
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
