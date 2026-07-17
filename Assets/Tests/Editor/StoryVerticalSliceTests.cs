using System;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public sealed class StoryVerticalSliceTests
{
    private const string ScenePath = "Assets/Scenes/Story_03_Quake.unity";

    [Test]
    public void SessionState_RoundTripsFlagsAndTurkishText()
    {
        StorySessionState state = new StorySessionState
        {
            activeScene = "Türkçe_ÇökKapanTutun",
            checkpoint = StoryCheckpoint.UnderCover,
            mistakeCount = 2
        };
        state.SetFlag(StoryFlag.BagFlashlight, true);
        state.SetFlag(StoryFlag.WardrobeSecured, true);

        string json = JsonUtility.ToJson(state);
        StorySessionState restored = JsonUtility.FromJson<StorySessionState>(json);

        Assert.That(restored.activeScene, Is.EqualTo("Türkçe_ÇökKapanTutun"));
        Assert.That(restored.checkpoint, Is.EqualTo(StoryCheckpoint.UnderCover));
        Assert.That(restored.HasFlag(StoryFlag.BagFlashlight), Is.True);
        Assert.That(restored.HasFlag(StoryFlag.WardrobeSecured), Is.True);
        Assert.That(restored.mistakeCount, Is.EqualTo(2));
    }

    [Test]
    public void StoryManager_ExposesRequiredContract()
    {
        string[] methods = { "SetFlag", "HasFlag", "CommitCheckpoint", "RetryCheckpoint", "ContinueStory" };
        Type managerType = typeof(StoryGameManager);
        foreach (string method in methods)
            Assert.That(managerType.GetMethod(method, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null, method);
    }

    [Test]
    public void SessionState_AllPreparationFlagCombinationsRoundTripAndResolveConsequences()
    {
        StoryFlag[] preparationFlags =
        {
            StoryFlag.BagFlashlight, StoryFlag.BagFirstAid, StoryFlag.BagWater,
            StoryFlag.WardrobeSecured, StoryFlag.ShelfSecured, StoryFlag.ExitCleared
        };
        GameObject managerObject = new GameObject("CombinationTestManager");
        GameObject sequenceObject = new GameObject("CombinationTestSequence");
        try
        {
            StoryGameManager manager = managerObject.AddComponent<StoryGameManager>();
            StorySequenceDirector sequence = sequenceObject.AddComponent<StorySequenceDirector>();
            FieldInfo managerField = typeof(StorySequenceDirector).GetField("gameManager", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo stateProperty = typeof(StoryGameManager).GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo resolve = typeof(StorySequenceDirector).GetMethod("ResolveConditionalText", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(managerField, Is.Not.Null);
            Assert.That(stateProperty, Is.Not.Null);
            Assert.That(resolve, Is.Not.Null);
            managerField.SetValue(sequence, manager);

            for (int mask = 0; mask < 1 << preparationFlags.Length; mask++)
            {
                StorySessionState state = new StorySessionState();
                for (int index = 0; index < preparationFlags.Length; index++)
                    state.SetFlag(preparationFlags[index], (mask & (1 << index)) != 0);

                StorySessionState restored = JsonUtility.FromJson<StorySessionState>(JsonUtility.ToJson(state));
                for (int index = 0; index < preparationFlags.Length; index++)
                    Assert.That(restored.HasFlag(preparationFlags[index]), Is.EqualTo((mask & (1 << index)) != 0),
                        $"mask={mask}, flag={preparationFlags[index]}");

                stateProperty.SetValue(manager, restored);
                string resolved = (string)resolve.Invoke(sequence, new object[] { "{WARDROBE} {EXIT} {WATER} {FIRSTAID}" });
                Assert.That(resolved, Is.Not.Empty);
                Assert.That(resolved, Does.Not.Contain("{"), $"mask={mask}");
                Assert.That(resolved, Does.Not.Contain("}"), $"mask={mask}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void StoryCheckpoints_CoverTheFiveSliceRecoveryMomentsInOrder()
    {
        StoryCheckpoint[] sliceCheckpoints =
        {
            StoryCheckpoint.QuakeStart,
            StoryCheckpoint.SiblingCalmed,
            StoryCheckpoint.UnderCover,
            StoryCheckpoint.PostQuake,
            StoryCheckpoint.CorridorReached
        };
        Assert.That(sliceCheckpoints.Distinct().Count(), Is.EqualTo(5));
        Assert.That(sliceCheckpoints.Zip(sliceCheckpoints.Skip(1), (left, right) => (int)left < (int)right).All(value => value), Is.True);
    }

    [Test]
    public void ContextAction_RepeatedTapRequiresAuthoredCountAndCompletesOnce()
    {
        GameObject eventSystemObject = new GameObject("TestEventSystem", typeof(EventSystem));
        GameObject actionObject = new GameObject("TestAction", typeof(RectTransform), typeof(StoryActionButton));
        try
        {
            int completionCount = 0;
            StoryActionButton action = actionObject.GetComponent<StoryActionButton>();
            action.Present("Bağlantıları kontrol et", StoryInteractionGesture.RepeatedTap, 3, () => completionCount++);
            PointerEventData pointer = new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
            {
                button = PointerEventData.InputButton.Left
            };
            action.OnPointerClick(pointer);
            action.OnPointerClick(pointer);
            Assert.That(completionCount, Is.Zero);
            Assert.That(action.Progress01, Is.EqualTo(2f / 3f).Within(0.001f));
            action.OnPointerClick(pointer);
            action.OnPointerClick(pointer);
            Assert.That(completionCount, Is.EqualTo(1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(actionObject);
            UnityEngine.Object.DestroyImmediate(eventSystemObject);
        }
    }

    [Test]
    public void ContextAction_SwipeDownRejectsWrongDirectionAndCompletesCorrectGesture()
    {
        GameObject eventSystemObject = new GameObject("TestEventSystem", typeof(EventSystem));
        GameObject actionObject = new GameObject("TestAction", typeof(RectTransform), typeof(StoryActionButton));
        try
        {
            bool completed = false;
            StoryActionButton action = actionObject.GetComponent<StoryActionButton>();
            action.Present("Çök", StoryInteractionGesture.SwipeDown, 1, () => completed = true);
            PointerEventData pointer = new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
            {
                button = PointerEventData.InputButton.Left,
                position = new Vector2(200f, 300f)
            };
            action.OnBeginDrag(pointer);
            pointer.position = new Vector2(360f, 305f);
            action.OnDrag(pointer);
            action.OnEndDrag(pointer);
            Assert.That(completed, Is.False);
            Assert.That(action.Progress01, Is.Zero);
            Assert.That(action.ShowingRetryHint, Is.True, "A rejected swipe must give a clear retry state.");

            pointer.position = new Vector2(200f, 300f);
            action.OnPointerDown(pointer);
            Assert.That(action.ShowingRetryHint, Is.False);
            pointer.position = new Vector2(205f, 150f);
            action.OnPointerUp(pointer);
            Assert.That(completed, Is.True);
            Assert.That(action.Progress01, Is.EqualTo(1f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(actionObject);
            UnityEngine.Object.DestroyImmediate(eventSystemObject);
        }
    }

    [Test]
    public void StoryScene_HasSingleInputOwnerAndRequiredAuthoredSystems()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.That(UnityEngine.Object.FindObjectsByType<StoryTouchManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            StoryTouchManager touchManager = UnityEngine.Object.FindFirstObjectByType<StoryTouchManager>();
            Assert.That(GetPrivate<bool>(touchManager, "directWorldGestures"), Is.True,
                "Deprem sahnesinde jestler ortadaki UI yerine nesnenin üzerinde yapılmalı.");
            Assert.That(UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            StoryPlayerMovement storyPlayer = UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None).Single();
            Assert.That(storyPlayer.GetComponentsInChildren<CharacterController>(true).Length, Is.EqualTo(0), "Deniz must be driven only by NavMeshAgent in the story slice.");
            StorySiblingFollower siblingFollower = UnityEngine.Object.FindObjectsByType<StorySiblingFollower>(FindObjectsSortMode.None).Single();
            Assert.That(siblingFollower.GetComponentsInChildren<CharacterController>(true).Length, Is.EqualTo(0), "Can must not retain the legacy character controller.");
            Assert.That(siblingFollower.Target, Is.EqualTo(storyPlayer.transform));
            Assert.That(UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length, Is.EqualTo(0));
            StoryInteractable[] interactions = UnityEngine.Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(interactions.Length, Is.GreaterThanOrEqualTo(32));
            Assert.That(interactions.Select(item => item.InteractionId).Distinct().Count(), Is.EqualTo(interactions.Length));
            Assert.That(interactions.All(item => item.EstimatedInteractionSeconds > 0f), Is.True);
            Assert.That(interactions.All(item => item.RequiredGestureCount >= 1), Is.True);
            Assert.That(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.RepeatedTap), Is.True);
            Assert.That(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.SwipeDown), Is.True);
            Assert.That(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.SwipeHorizontal), Is.True);
            Assert.That(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.Approach), Is.True);
            Assert.That(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.WorldHold), Is.True);
            Assert.That(interactions.Where(item => !item.InteractFromAnywhere).All(item =>
                NavMesh.SamplePosition(item.InteractionPoint.position, out _, 1.75f, NavMesh.AllAreas)), Is.True,
                "Every approached interaction must have a nearby baked NavMesh point.");
            Assert.That(interactions.All(HighlightMatchesInteractionPoint), Is.True,
                "Every authored highlight must be centered on its interaction point.");
            StoryGameManager manager = UnityEngine.Object.FindObjectsByType<StoryGameManager>(FindObjectsSortMode.None).Single();
            Assert.That(manager.transform.parent, Is.Null, "DontDestroyOnLoad session manager must be a scene root.");
            StorySequenceDirector sequence = UnityEngine.Object.FindObjectsByType<StorySequenceDirector>(FindObjectsSortMode.None).Single();
            Assert.That(sequence.MinimumCompletionDuration, Is.InRange(480f, 720f));
            Assert.That(sequence.EstimatedFirstPlayDuration, Is.InRange(480f, 720f));
            Assert.That(sequence.AuthoredBeatCount, Is.GreaterThanOrEqualTo(28));
            Assert.That(UnityEngine.Object.FindObjectsByType<StoryActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "Dünya etkileşimi ortadaki görev düğmesine taşınmamalı.");

            CinemachineCamera[] cameras = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            Assert.That(cameras.Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(cameras.All(camera => camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);
            Assert.That(cameras.Count(camera => camera.GetComponent<CinemachinePositionComposer>() != null && camera.Follow == storyPlayer.transform),
                Is.GreaterThanOrEqualTo(4), "Room, quake, post-quake and corridor gameplay cameras must follow Deniz.");
            CinemachineCamera tableLegCamera = cameras.Single(camera => camera.name == "CM_InspectTableLegs");
            CinemachineCamera brokenGlassCamera = cameras.Single(camera => camera.name == "CM_InspectBrokenGlass");
            StoryInteractable glassInspection = interactions.Single(item => item.InteractionId == "Post_InspectGlass");
            Assert.That(glassInspection.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.InspectBrokenGlass),
                "Broken glass must use its floor-level subject shot instead of the intact-window camera.");
            Renderer nearestTableLeg = GameObject.Find("SafeTable").GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name == "Leg")
                .OrderBy(renderer => Vector3.Distance(renderer.bounds.center, tableLegCamera.transform.position))
                .First();
            AssertPortraitSubject(tableLegCamera, nearestTableLeg.bounds.center, "nearest table leg");
            GameObject brokenGlassHazard = UnityEngine.Object
                .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(transform => transform.name == "BrokenGlass_Hazard")
                .gameObject;
            Assert.That(brokenGlassCamera.transform.position.x, Is.GreaterThan(-4.85f),
                "Broken-glass camera must remain inside the left wall; otherwise the wall occludes the entire shot.");
            AssertPortraitSubject(brokenGlassCamera, CombinedBounds(brokenGlassHazard).center, "broken glass");

            Assert.That(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(2));
            PlayableDirector quakeDirector = UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None)
                .Single(director => director.playableAsset is TimelineAsset);
            TimelineAsset quakeTimeline = (TimelineAsset)quakeDirector.playableAsset;
            string[] quakeTracks = quakeTimeline.GetOutputTracks().Select(track => track.name).ToArray();
            foreach (string trackName in new[]
                     {
                         "Masa titreşimi", "Asılı lamba salınımı", "Radyo ve hafif eşya kayması",
                         "Kapı vuruntusu", "Sabit dolap titreşimi", "Sabit raf titreşimi",
                         "Sabitlenmemiş raf devrilmesi", "Sabitlenmemiş dolap devrilmesi"
                     })
                Assert.That(quakeTracks, Does.Contain(trackName), trackName);
            Assert.That(UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).Single().navMeshData, Is.Not.Null);

            CanvasScaler scaler = UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None).Single();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f), "Tall 9:19.5 screens must preserve the authored horizontal safe width.");
            StorySafeAreaPanel safeArea = UnityEngine.Object.FindObjectsByType<StorySafeAreaPanel>(FindObjectsSortMode.None).Single();
            RectTransform subtitlePanel = FindRect("SubtitlePanel");
            RectTransform contextPanel = FindRect("ContextPrompt");
            RectTransform objectivePanel = FindRect("ObjectiveStrip");
            RectTransform pauseButton = FindRect("PauseButton");
            foreach (RectTransform rect in new[] { subtitlePanel, contextPanel, objectivePanel, pauseButton })
                Assert.That(rect.IsChildOf(safeArea.transform), Is.True, rect.name);
            Assert.That(pauseButton.sizeDelta.x, Is.GreaterThanOrEqualTo(96f), "Pause touch target width");
            Assert.That(pauseButton.sizeDelta.y, Is.GreaterThanOrEqualTo(96f), "Pause touch target height");
            Assert.That(Top(subtitlePanel) + 12f, Is.LessThanOrEqualTo(Bottom(contextPanel)), "Subtitle/context spacing");
            const float narrowSafeWidth = 1040f;
            Assert.That(objectivePanel.sizeDelta.x * 0.5f + pauseButton.sizeDelta.x + Mathf.Abs(pauseButton.anchoredPosition.x),
                Is.LessThanOrEqualTo(narrowSafeWidth * 0.5f), "Objective/pause separation on a 20 px side inset.");

            foreach (string overlayName in new[] { "PauseOverlay", "CompletionOverlay" })
            {
                RectTransform overlay = FindRect(overlayName);
                Assert.That(overlay.anchorMin, Is.EqualTo(Vector2.zero), overlayName + " anchorMin");
                Assert.That(overlay.anchorMax, Is.EqualTo(Vector2.one), overlayName + " anchorMax");
                Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True, overlayName + " blocks background input");
                Assert.That(overlay.IsChildOf(safeArea.transform), Is.True, overlayName + " respects safe area");
            }

            RectTransform shakeToggle = FindRect("ReduceShakeToggle");
            Assert.That(shakeToggle.sizeDelta.x, Is.GreaterThanOrEqualTo(600f));
            Assert.That(shakeToggle.sizeDelta.y, Is.GreaterThanOrEqualTo(96f));
            Assert.That(shakeToggle.GetComponentsInChildren<TMP_Text>(true).Any(text => text.name == "State" && !string.IsNullOrWhiteSpace(text.text)), Is.True,
                "Reduced-shake state must be communicated with text, not color alone.");

            foreach (string textName in new[] { "ObjectiveDetail", "ContextText", "SubtitleText" })
            {
                TMP_Text criticalText = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(text => text.name == textName);
                Assert.That(criticalText.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), textName + " must not ellipsize safety guidance");
            }

            Button retryButton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(button => button.name == "RetryCheckpointButton");
            Button replayButton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(button => button.name == "ReplayStoryButton");
            Button chapterButton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(button => button.name == "ChapterSelectionButton");
            AssertButtonEvent(retryButton, "RetryCheckpoint");
            AssertButtonEvent(replayButton, "ReplayStory");
            AssertButtonEvent(chapterButton, "ShowChapterSelection");

            Bounds tableBounds = CombinedBounds(GameObject.Find("SafeTable"));
            Bounds shelfBounds = CombinedBounds(GameObject.Find("Shelf_Secured"));
            Assert.That(tableBounds.Intersects(shelfBounds), Is.False, "Safe table and shelf must not overlap.");
            Assert.That(Vector2.Distance(new Vector2(tableBounds.center.x, tableBounds.center.z), new Vector2(shelfBounds.center.x, shelfBounds.center.z)), Is.GreaterThanOrEqualTo(3.2f));

            Transform[] wardrobeParts = GameObject.Find("Wardrobe_Secured").GetComponentsInChildren<Transform>(true);
            Assert.That(wardrobeParts.Count(part => part.name == "WallBracket"), Is.EqualTo(1));
            Assert.That(wardrobeParts.Count(part => part.name == "SafetyStrap"), Is.EqualTo(2));
            Transform[] wallBracketBolts = wardrobeParts
                .Where(part => (part.name == "BoltLeft" || part.name == "BoltRight") &&
                               part.parent != null && part.parent.name == "WallBracket")
                .ToArray();
            Assert.That(wallBracketBolts.Length, Is.EqualTo(2));
            Assert.That(wardrobeParts.Where(part => part.name == "SafetyStrap").All(part => part.localPosition.z > 0.25f), Is.True,
                "Wardrobe safety straps must be authored behind the furniture, toward the wall.");
            Assert.That(wallBracketBolts.All(part => part.parent.localPosition.z > 0.3f), Is.True,
                "Wardrobe anchor bolts must not float on the front doors.");

            Transform shelf = GameObject.Find("Shelf_Secured").transform;
            Assert.That(Mathf.DeltaAngle(shelf.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.1f),
                "The right-wall shelf must face into the room.");
            Transform[] shelfParts = shelf.GetComponentsInChildren<Transform>(true);
            Transform shelfAnchor = shelfParts.Single(part => part.name == "ShelfAnchorTop");
            Assert.That(shelfAnchor.localPosition.z,
                Is.GreaterThan(0.2f), "Shelf anchor must sit on the wall side.");
            Transform[] shelfBolts = shelfParts
                .Where(part => (part.name == "BoltLeft" || part.name == "BoltRight") &&
                               part.parent == shelfAnchor)
                .ToArray();
            Assert.That(shelfBolts.Length, Is.EqualTo(2));
            Assert.That(shelfBolts.All(part => part.parent.localPosition.z > 0.2f), Is.True,
                "Shelf bolts must sit on the wall side.");

            GameObject denizRoot = GameObject.Find("Deniz_12");
            GameObject canRoot = GameObject.Find("Can_8");
            foreach (GameObject character in new[] { denizRoot, canRoot })
            {
                Assert.That(character.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Any(renderer => renderer.sharedMesh != null), Is.True, character.name + " must use a rigged character mesh.");
                Assert.That(character.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.Contains("_Identity_", StringComparison.Ordinal) ||
                                 item.name.EndsWith("_Accessory", StringComparison.Ordinal)), Is.False,
                    character.name + " must not contain primitive placeholder body parts.");
            }
            Assert.That(interactions.Single(item => item.InteractionId == "Calm_Can").transform.IsChildOf(canRoot.transform), Is.True,
                "Sibling interaction must move with Can rather than remain at his spawn point.");
            Assert.That(interactions.Single(item => item.InteractionId == "Post_CallCan").transform.IsChildOf(canRoot.transform), Is.True);

            Assert.That(GameObject.Find("FamilySofa_Visual"), Is.Not.Null);
            Assert.That(GameObject.Find("SafeTable_Visual"), Is.Not.Null);
            Assert.That(GameObject.Find("Wardrobe_Visual"), Is.Not.Null);

            Assert.That(GetPrivate<GameObject>(sequence, "leftShoeWorld"), Is.Not.Null);
            Assert.That(GetPrivate<GameObject>(sequence, "rightShoeWorld"), Is.Not.Null);
            Assert.That(GetPrivate<GameObject>(sequence, "canShoesWorld"), Is.Not.Null);
            Assert.That(GetPrivate<GameObject>(sequence, "emergencyBagWorld"), Is.Not.Null);
            Assert.That(GetPrivate<GameObject>(sequence, "denizWornShoes").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "denizWornBag").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "canWornShoes").activeSelf, Is.False);

            foreach (string uprightObject in new[]
                     {
                         "FamilySofa_Visual", "SafeTable_Visual", "Wardrobe_Secured", "Wardrobe_Unsecured", "RoomPlant_Visual",
                         "LowCabinet_Visual", "EmergencyBag"
                     })
            {
                Transform[] authoredMatches = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(item => item.name == uprightObject)
                    .ToArray();
                Assert.That(authoredMatches, Is.Not.Empty, uprightObject + " must exist.");

                foreach (Transform authored in authoredMatches)
                {
                    Assert.That(Vector3.Dot(authored.up, Vector3.up), Is.GreaterThan(0.98f), uprightObject + " must remain upright.");
                    Assert.That(IsFinite(authored.position) && IsFinite(authored.lossyScale), Is.True, uprightObject + " transform must be finite.");
                }
            }

            MethodInfo faceSiblings = typeof(StorySequenceDirector).GetMethod("FaceSiblingsTowardsEachOther", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(faceSiblings, Is.Not.Null);
            faceSiblings.Invoke(sequence, null);
            Vector3 denizToCan = siblingFollower.transform.position - storyPlayer.transform.position;
            denizToCan.y = 0f;
            Vector3 canToDeniz = -denizToCan;
            Assert.That(Vector3.Dot(storyPlayer.transform.forward, denizToCan.normalized), Is.GreaterThan(0.98f));
            Assert.That(Vector3.Dot(siblingFollower.transform.forward, canToDeniz.normalized), Is.GreaterThan(0.98f));

            MethodInfo applyPhysicalResult = typeof(StorySequenceDirector).GetMethod("ApplyPostQuakePhysicalResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyPhysicalResult, Is.Not.Null);
            foreach (int beatIndex in new[] { 7, 8, 9, 10, 11 })
                applyPhysicalResult.Invoke(sequence, new object[] { beatIndex });
            Assert.That(GetPrivate<GameObject>(sequence, "leftShoeWorld").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "rightShoeWorld").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "canShoesWorld").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "emergencyBagWorld").activeSelf, Is.False);
            Assert.That(GetPrivate<GameObject>(sequence, "denizWornShoes").activeSelf, Is.True);
            Assert.That(GetPrivate<GameObject>(sequence, "denizWornBag").activeSelf, Is.True);
            Assert.That(GetPrivate<GameObject>(sequence, "canWornShoes").activeSelf, Is.True);
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    [Test]
    public void StoryScene_AllBranchesAndBeatsAreWiredWithoutFlagSoftlocks()
    {
        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StorySequenceDirector sequence = UnityEngine.Object.FindObjectsByType<StorySequenceDirector>(FindObjectsSortMode.None).Single();
            StoryInteractable[] interactions = UnityEngine.Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            AssertEvent(interactions, "Inspect_SafeTable", "OnIntroInspection");
            AssertEvent(interactions, "Inspect_Window", "OnIntroInspection");
            AssertEvent(interactions, "Inspect_Wardrobe", "OnIntroInspection");
            AssertEvent(interactions, "Inspect_Exit", "OnIntroInspection");
            AssertEvent(interactions, "Calm_Can", "OnSiblingCalmed");
            AssertEvent(interactions, "TakeCover_Crouch", "OnCrouchStep");
            AssertEvent(interactions, "TakeCover_ProtectHead", "OnCoverHeadStep");
            AssertEvent(interactions, "TakeCover_GripTable", "OnCoverReached");
            AssertEvent(interactions, "Unsafe_Door", "OnUnsafeChoice");
            AssertEvent(interactions, "Unsafe_Window", "OnUnsafeChoice");
            AssertEvent(interactions, "Hazard_BrokenGlass", "OnPostQuakeHazard");
            AssertEvent(interactions, "Post_TestFlashlight", "OnLightPrepared");
            AssertEvent(interactions, "Post_FindEmergencyLight", "OnLightPrepared");
            AssertEvent(interactions, "Exit_Corridor", "OnCorridorReached");

            Assert.That(interactions.Where(item => item.InteractionId.StartsWith("Inspect_")).All(item => !item.InteractFromAnywhere), Is.True);
            Assert.That(interactions.Single(item => item.InteractionId == "Calm_Can").InteractFromAnywhere, Is.False);
            Assert.That(interactions.Count(item => item.InteractionGesture == StoryInteractionGesture.Approach), Is.GreaterThanOrEqualTo(8));
            Assert.That(interactions.Single(item => item.InteractionId == "Hazard_BrokenGlass").WorldSelectable, Is.False);
            foreach (string visualTarget in new[] { "Inspect_Window", "Inspect_Wardrobe", "Inspect_Exit" })
            {
                StoryInteractable target = interactions.Single(item => item.InteractionId == visualTarget);
                Assert.That(target.GetComponent<BoxCollider>().bounds.center.y, Is.GreaterThan(1.2f), visualTarget);
            }

            StoryAuthoredBeat[] post = GetPrivate<StoryAuthoredBeat[]>(sequence, "postQuakeBeats");
            StoryAuthoredBeat[] corridor = GetPrivate<StoryAuthoredBeat[]>(sequence, "corridorBeats");
            Assert.That(post, Has.Length.EqualTo(15));
            Assert.That(corridor, Has.Length.EqualTo(4));
            Assert.That(post.All(beat => beat != null && beat.interactable != null), Is.True);
            Assert.That(corridor.All(beat => beat != null && beat.interactable != null), Is.True);
            foreach (StoryAuthoredBeat beat in post)
                AssertEvent(interactions, beat.interactable.InteractionId, "OnPostQuakeStep");
            foreach (StoryAuthoredBeat beat in corridor)
                AssertEvent(interactions, beat.interactable.InteractionId, "OnCorridorStep");

            string combinedConsequences = string.Join(" ", post.Select(beat => beat.completionSubtitle));
            Assert.That(combinedConsequences, Does.Contain("{WARDROBE}"));
            Assert.That(combinedConsequences, Does.Contain("{EXIT}"));
            Assert.That(combinedConsequences, Does.Contain("{WATER}"));
            Assert.That(combinedConsequences, Does.Contain("{FIRSTAID}"));

            string[] consequenceObjects =
            {
                "wardrobeSecured", "wardrobeUnsecured", "wardrobeFallen", "shelfStable", "shelfUnsecured", "shelfFallen",
                "clearExitRoute", "clutteredExitRoute", "playerFlashlight", "emergencyRouteLights",
                "closedDoor", "openDoor"
            };
            foreach (string field in consequenceObjects)
                Assert.That(GetPrivate<GameObject>(sequence, field), Is.Not.Null, field);
            Assert.That(GetPrivate<StoryInteractable>(sequence, "brokenGlassHazard"), Is.Not.Null, "brokenGlassHazard");

            StoryInteractable corridorExit = interactions.Single(item => item.InteractionId == "Exit_Corridor");
            Assert.That(corridorExit.RequiredFlag, Is.EqualTo(StoryFlag.None));
            Assert.That(corridorExit.InteractionPoint.IsChildOf(GetPrivate<GameObject>(sequence, "clearExitRoute").transform), Is.False);
            Assert.That(corridorExit.InteractionPoint.IsChildOf(GetPrivate<GameObject>(sequence, "clutteredExitRoute").transform), Is.False);
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    [Test]
    public void StoryScene_CriticalGameplayRoutesAreCompleteAndDoorDoesNotCutNavMesh()
    {
        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StoryPlayerMovement storyPlayer = UnityEngine.Object.FindFirstObjectByType<StoryPlayerMovement>();
            StoryInteractable[] interactions = UnityEngine.Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Vector3 playerStart = storyPlayer.transform.position;
            Vector3 table = interactions.Single(item => item.InteractionId == "Inspect_SafeTable").InteractionPoint.position;
            Vector3 window = interactions.Single(item => item.InteractionId == "Inspect_Window").InteractionPoint.position;
            Vector3 wardrobe = interactions.Single(item => item.InteractionId == "Inspect_Wardrobe").InteractionPoint.position;
            Vector3 exit = interactions.Single(item => item.InteractionId == "Exit_Corridor").InteractionPoint.position;
            Vector3 corridorEnd = interactions.Single(item => item.InteractionId == "Corridor_Beat_4").InteractionPoint.position;

            AssertCompleteRoute(playerStart, table, "spawn -> table");
            AssertCompleteRoute(table, window, "table -> window");
            AssertCompleteRoute(window, wardrobe, "window -> wardrobe");
            AssertCompleteRoute(wardrobe, exit, "wardrobe -> exit");
            AssertCompleteRoute(exit, corridorEnd, "open door -> corridor end");

            NavMeshModifier closedDoorModifier = GameObject.Find("Door_Closed").GetComponent<NavMeshModifier>();
            Assert.That(closedDoorModifier, Is.Not.Null);
            Assert.That(closedDoorModifier.ignoreFromBuild, Is.True, "The animated door must not leave a permanent hole in the baked NavMesh.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    [Test]
    public void BuildSettings_PreserveLegacyStartupAndAppendStorySlice()
    {
        string[] paths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
        Assert.That(paths.Length, Is.GreaterThanOrEqualTo(5));
        Assert.That(paths[0], Does.EndWith("bolum1.unity").IgnoreCase);
        Assert.That(paths[1], Does.EndWith("Bolum2.unity").IgnoreCase);
        Assert.That(paths[2], Does.EndWith("Bolum3.unity").IgnoreCase);
        Assert.That(paths[3], Does.EndWith("Bolum4.unity").IgnoreCase);
        Assert.That(paths, Does.Contain(ScenePath));
    }

    [Test]
    public void StoryAnimationLibrary_ImportsLicensedHumanoidSourcesAndRequiredStates()
    {
        string[] sources =
        {
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx",
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementBasic.fbx",
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementAdvanced.fbx",
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Simulation.fbx",
            "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_Tools.fbx",
            "Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx"
        };

        foreach (string source in sources)
        {
            ModelImporter importer = AssetImporter.GetAtPath(source) as ModelImporter;
            Assert.That(importer, Is.Not.Null, source);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human), source);
            Assert.That(importer.importAnimation, Is.True, source);
            Avatar sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(source).OfType<Avatar>().FirstOrDefault();
            Assert.That(sourceAvatar, Is.Not.Null, source);
            Assert.That(sourceAvatar.isValid, Is.True, source);
            Assert.That(sourceAvatar.isHuman, Is.True, source);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>()
                .Any(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) && clip.isHumanMotion), Is.True, source);
        }

        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Story/Animations/ThirdParty/KayKit/LICENSE.txt"), Is.Not.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Story/Animations/ThirdParty/Quaternius/LICENSE.txt"), Is.Not.Null);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        Assert.That(controller, Is.Not.Null);
        string[] parameters = controller.parameters.Select(parameter => parameter.name).ToArray();
        Assert.That(parameters, Does.Contain("Speed"));
        foreach (string trigger in StoryAnimationLibraryBuilder.RequiredTriggers)
            Assert.That(parameters, Does.Contain(trigger), trigger);

        string[] states = controller.layers[0].stateMachine.states.Select(child => child.state.name).ToArray();
        foreach (string state in new[] { "Locomotion", "Interact", "Pick Up", "Inspect", "Call Sibling", "Startle", "Recover Balance", "Crouch", "Protect Head", "Hold Cover", "Work" })
            Assert.That(states, Does.Contain(state), state);
        BlendTree locomotion = controller.layers[0].stateMachine.states
            .Single(child => child.state.name == "Locomotion").state.motion as BlendTree;
        Assert.That(locomotion, Is.Not.Null);
        string[] locomotionMotions = locomotion.children.Select(child => child.motion.name).ToArray();
        Assert.That(locomotionMotions, Does.Contain("ChildNeutralIdle"),
            "Story characters must use the corrected neutral child idle instead of the wide source stance.");
        Assert.That(locomotionMotions, Does.Contain("ChildNaturalWalk"),
            "Story karakterleri çocuk rigine göre yanal açıklığı düzeltilmiş yürüyüşü kullanmalı.");
        Assert.That(locomotionMotions, Does.Not.Contain("Walking_B"),
            "Ham KayKit yürüyüşü çocuk riginde goril duruşu ürettiği için doğrudan kullanılmamalı.");
        Assert.That(locomotionMotions, Does.Not.Contain("boy_move_walk"),
            "Eski çömelmiş/sneak görünümlü çocuk yürüyüşü Story locomotion'da kalmamalı.");
        Assert.That(locomotionMotions, Does.Not.Contain("boy_move_run"),
            "Ham NavMesh hızı karakteri yanlışlıkla koşu state'ine itmemeli.");

        Assert.That(controller.layers[0].stateMachine.states
                .Single(child => child.state.name == "Protect Head").state.motion.name,
            Is.EqualTo("Crouching"),
            "Protect Head alt gövdede güvenilir çömelme klibini kullanmalı.");
        Assert.That(controller.layers[0].stateMachine.states
                .Single(child => child.state.name == "Hold Cover").state.motion.name,
            Is.EqualTo("Crouching"),
            "Hold Cover alt gövdede güvenilir çömelme klibini kullanmalı.");
        AnimatorControllerLayer coverLayer = controller.layers
            .Single(layer => layer.name == "Cover Upper Body");
        Assert.That(coverLayer.avatarMask, Is.Not.Null);
        Assert.That(coverLayer.avatarMask.name, Is.EqualTo("ChildCoverUpperBody"));
        Assert.That(coverLayer.syncedLayerIndex, Is.EqualTo(0),
            "Üst gövde katmanı ayrı Trigger tüketmemeli; ana katmanla senkron olmalı.");
        AnimatorState protectState = controller.layers[0].stateMachine.states
            .Single(child => child.state.name == "Protect Head").state;
        AnimatorState holdState = controller.layers[0].stateMachine.states
            .Single(child => child.state.name == "Hold Cover").state;
        Assert.That(coverLayer.GetOverrideMotion(protectState).name, Is.EqualTo("ChildCoverUpperPose"));
        Assert.That(coverLayer.GetOverrideMotion(holdState).name, Is.EqualTo("ChildCoverUpperPose"));
        Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(
            StoryAnimationLibraryBuilder.ChildCoverUpperPosePath), Is.Not.Null);

        AnimatorController adult =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath);
        Assert.That(adult, Is.Not.Null);
        string[] adultParameters = adult.parameters.Select(parameter => parameter.name).ToArray();
        foreach (string trigger in StoryAnimationLibraryBuilder.RequiredTriggers)
            Assert.That(adultParameters, Does.Contain(trigger), "adult " + trigger);
        string[] adultStates = adult.layers[0].stateMachine.states.Select(child => child.state.name).ToArray();
        foreach (string state in new[] { "Adult Idle", "Adult Interact", "Adult Pick Up", "Adult Inspect", "Adult Call", "Adult Work" })
            Assert.That(adultStates, Does.Contain(state), state);
        var adultMotions = adult.layers[0].stateMachine.states
            .ToDictionary(child => child.state.name, child => child.state.motion != null ? child.state.motion.name : string.Empty);
        Assert.That(adultMotions["Adult Idle"], Is.EqualTo("AdultNeutralIdle"),
            "Anne idle klibi yetişkin iskeletinde bacakları gereksiz açmamalı.");
        AnimationClip adultIdle =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAnimationLibraryBuilder.AdultNeutralIdlePath);
        Assert.That(adultIdle, Is.Not.Null);
        EditorCurveBinding adultLeftFootX = AnimationUtility.GetCurveBindings(adultIdle)
            .Single(binding => binding.propertyName == "LeftFootT.x");
        float adultFootOffset = Mathf.Abs(AnimationUtility.GetEditorCurve(adultIdle, adultLeftFootX).Evaluate(0f));
        AnimationClip childIdle =
            AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/KidsCharacterFree/AnimationClips/Humanoid/boy_idle0.anim");
        EditorCurveBinding childLeftFootX = AnimationUtility.GetCurveBindings(childIdle)
            .Single(binding => binding.propertyName == "LeftFootT.x");
        float childFootOffset = Mathf.Abs(AnimationUtility.GetEditorCurve(childIdle, childLeftFootX).Evaluate(0f));
        Assert.That(adultFootOffset, Is.LessThan(0.001f),
            "Yetişkin nötr duruşunda ayaklar çocuk klibindeki geniş açıklıkta kalmamalı.");
        EditorCurveBinding adultLeftUpperLeg = AnimationUtility.GetCurveBindings(adultIdle)
            .Single(binding => binding.propertyName == "Left Upper Leg In-Out");
        float adultUpperLegInOut =
            AnimationUtility.GetEditorCurve(adultIdle, adultLeftUpperLeg).Evaluate(0f);
        Assert.That(adultUpperLegInOut, Is.LessThanOrEqualTo(-0.44f),
            "Anne'nin yetişkin idle pozu tekrar geniş goril duruşuna dönememeli.");

        AnimationClip naturalWalk =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAnimationLibraryBuilder.ChildNaturalWalkPath);
        Assert.That(naturalWalk, Is.Not.Null);
        EditorCurveBinding naturalWalkFootX = AnimationUtility.GetCurveBindings(naturalWalk)
            .Single(binding => binding.propertyName == "LeftFootT.x");
        AnimationClip sourceWalk = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_MovementBasic.fbx")
            .OfType<AnimationClip>().Single(clip => clip.name == "Walking_B");
        EditorCurveBinding sourceWalkFootX = AnimationUtility.GetCurveBindings(sourceWalk)
            .Single(binding => binding.propertyName == "LeftFootT.x");
        float sampleTime = Mathf.Min(naturalWalk.length, sourceWalk.length) * 0.18f;
        float correctedWalkFootX =
            Mathf.Abs(AnimationUtility.GetEditorCurve(naturalWalk, naturalWalkFootX).Evaluate(sampleTime));
        float rawWalkFootX =
            Mathf.Abs(AnimationUtility.GetEditorCurve(sourceWalk, sourceWalkFootX).Evaluate(sampleTime));
        Assert.That(correctedWalkFootX, Is.LessThan(rawWalkFootX * 0.55f),
            "Çocuk yürüyüşü ham KayKit yanal ayak açıklığını geri getirmemeli.");
        Assert.That(adultMotions["Adult Interact"], Is.EqualTo("Interact"));
        Assert.That(adultMotions["Adult Pick Up"], Is.EqualTo("PickUp"));
        Assert.That(adultMotions["Adult Work"], Is.EqualTo("Working_B"),
            "Matkap çalışması canlı doğrulanan omuz hizası pozunu kullanmalı.");
    }

    [Test]
    public void StoryScene_CharactersUseRetargetedStoryAnimatorWithoutRootMotion()
    {
        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StorySequenceDirector sequence = UnityEngine.Object.FindObjectsByType<StorySequenceDirector>(FindObjectsSortMode.None).Single();
            RuntimeAnimatorController expected = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
            Animator deniz = GetPrivate<Animator>(sequence, "denizAnimator");
            Animator can = GetPrivate<Animator>(sequence, "canAnimator");

            foreach (Animator animator in new[] { deniz, can })
            {
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.EqualTo(expected));
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(animator.avatar, Is.Not.Null);
                Assert.That(animator.avatar.isValid, Is.True);
                Assert.That(animator.avatar.isHuman, Is.True);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    private static Bounds CombinedBounds(GameObject root)
    {
        Assert.That(root, Is.Not.Null);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void AssertPortraitSubject(CinemachineCamera source, Vector3 subject, string label)
    {
        GameObject probeObject = new GameObject("PortraitCameraProbe");
        try
        {
            Camera probe = probeObject.AddComponent<Camera>();
            probe.enabled = false;
            probe.aspect = 1080f / 1920f;
            probe.fieldOfView = source.Lens.FieldOfView;
            probe.nearClipPlane = source.Lens.NearClipPlane;
            probe.farClipPlane = source.Lens.FarClipPlane;
            probe.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Vector3 viewport = probe.WorldToViewportPoint(subject);
            Assert.That(viewport.z, Is.GreaterThan(0f), label + " must be in front of the camera");
            Assert.That(viewport.x, Is.InRange(0.12f, 0.88f), label + " horizontal portrait framing");
            Assert.That(viewport.y, Is.InRange(0.14f, 0.82f), label + " vertical portrait framing");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probeObject);
        }
    }

    private static RectTransform FindRect(string name)
    {
        return UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(rect => rect.name == name);
    }

    private static float Bottom(RectTransform rect)
    {
        return rect.anchoredPosition.y - rect.sizeDelta.y * rect.pivot.y;
    }

    private static float Top(RectTransform rect)
    {
        return Bottom(rect) + rect.sizeDelta.y;
    }

    private static bool HighlightMatchesInteractionPoint(StoryInteractable interactable)
    {
        if (interactable.HighlightRoot == null || interactable.InteractionPoint == null)
            return false;
        Renderer renderer = interactable.HighlightRoot.GetComponentInChildren<Renderer>(true);
        Vector3 highlightPosition = interactable.HighlightRoot.transform.position;
        return renderer != null && Vector2.Distance(
            new Vector2(highlightPosition.x, highlightPosition.z),
            new Vector2(interactable.InteractionPoint.position.x, interactable.InteractionPoint.position.z)) <= 0.08f;
    }

    private static void AssertCompleteRoute(Vector3 from, Vector3 to, string label)
    {
        Assert.That(NavMesh.SamplePosition(from, out NavMeshHit start, 1.75f, NavMesh.AllAreas), Is.True, label + " start");
        Assert.That(NavMesh.SamplePosition(to, out NavMeshHit end, 1.75f, NavMesh.AllAreas), Is.True, label + " end");
        NavMeshPath path = new NavMeshPath();
        Assert.That(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path), Is.True, label);
        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label);
    }

    private static void AssertEvent(StoryInteractable[] interactions, string id, string methodName)
    {
        StoryInteractable interactable = interactions.Single(item => item.InteractionId == id);
        Assert.That(interactable.OnInteracted.GetPersistentEventCount(), Is.GreaterThan(0), id);
        Assert.That(Enumerable.Range(0, interactable.OnInteracted.GetPersistentEventCount())
            .Any(index => interactable.OnInteracted.GetPersistentMethodName(index) == methodName), Is.True,
            $"{id} must invoke {methodName}.");
    }

    private static void AssertButtonEvent(Button button, string methodName)
    {
        Assert.That(button, Is.Not.Null, methodName);
        Assert.That(Enumerable.Range(0, button.onClick.GetPersistentEventCount())
            .Any(index => button.onClick.GetPersistentMethodName(index) == methodName), Is.True,
            $"{button.name} must invoke {methodName}.");
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
