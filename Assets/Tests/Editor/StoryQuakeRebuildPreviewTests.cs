using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public sealed class StoryQuakeRebuildPreviewTests
{
    private const string ScenePath = "Assets/Scenes/Story_03_RebuildPreview.unity";
    private const string SharedHomePath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";

    [SetUp]
    public void OpenPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void Preview_UsesConnectedSharedHomeAndStaysOutOfBuildSettings()
    {
        Transform sharedHome = Find("StoryHome_Shared");
        Assert.That(sharedHome, Is.Not.Null);
        Assert.That(
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome.gameObject),
            Is.EqualTo(SharedHomePath));
        Assert.That(sharedHome.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(sharedHome.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(sharedHome.localScale, Is.EqualTo(Vector3.one));
        Assert.That(Find("Story03_DioramaFoundation"), Is.Not.Null);
        Assert.That(Find("Story03_DioramaUpperSlab").GetComponent<Collider>(), Is.Null);
        Assert.That(Find("Story03_DioramaLowerSlab").GetComponent<Collider>(), Is.Null);
        Assert.That(Camera.main.backgroundColor, Is.EqualTo((Color)new Color32(46, 60, 68, 255)));

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        Assert.That(surface, Is.Not.Null);
        Assert.That(surface.navMeshData, Is.Not.Null);
        Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath), Is.True);
    }

    [Test]
    public void Preview_HasOneInputOwnerNoCenterActionButtonAndRevisedPacing()
    {
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StorySequenceDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);

        StorySequenceDirector director = Object.FindFirstObjectByType<StorySequenceDirector>();
        Assert.That(director.RevisedFlow, Is.True);
        Assert.That(GetPrivate<float>(director, "introMinimumDuration"), Is.EqualTo(0f));
        Assert.That(GetPrivate<float>(director, "introMaximumDuration"), Is.EqualTo(72f));
        Assert.That(GetPrivate<float>(director, "revisedQuakeDelayAfterFamilyMoment"), Is.EqualTo(55f));
        Assert.That(GetPrivate<float>(director, "quakeMinimumDuration"), Is.EqualTo(46f));
        Assert.That(GetPrivate<float>(director, "postQuakeSettleDuration"), Is.EqualTo(2.4f));
        Assert.That(GetPrivate<float>(director, "minimumCompletionDuration"), Is.EqualTo(0f));
        Assert.That(GetPrivate<float>(director, "corridorWarningDuration"), Is.EqualTo(8f));
        StoryInteractable[] requiredIntro = GetPrivate<StoryInteractable[]>(director, "introInspections");
        StoryInteractable[] optionalIntro = GetPrivate<StoryInteractable[]>(director, "introOptionalMoments");
        Assert.That(requiredIntro.Select(item => item.InteractionId),
            Is.EqualTo(new[] { "quake.intro.wheel", "quake.intro.car" }));
        Assert.That(optionalIntro.Select(item => item.InteractionId),
            Is.EqualTo(new[] { "quake.intro.radio", "quake.intro.familyplan" }));
        Assert.That(PersistentMethods(optionalIntro[0]), Does.Contain("OnOptionalIntroRadioMoment"));
        Assert.That(PersistentMethods(optionalIntro[1]), Does.Contain("OnOptionalIntroPlanMoment"));
        Assert.That(optionalIntro.SelectMany(PersistentMethods), Does.Not.Contain("OnIntroInspection"));
        Assert.That(GetPrivate<StoryAuthoredBeat[]>(director, "postQuakeBeats"), Has.Length.EqualTo(8));
        Assert.That(GetPrivate<StoryAuthoredBeat[]>(director, "corridorBeats"), Has.Length.EqualTo(5));
    }

    [Test]
    public void Preview_UsesActualWorldObjectsForAllCoreActions()
    {
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(interactions, Has.Length.EqualTo(27));

        AssertGesture(interactions, "quake.intro.wheel", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "quake.intro.car", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "quake.intro.radio", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "quake.intro.familyplan", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "quake.calm.can", StoryInteractionGesture.Tap);
        AssertGesture(interactions, "quake.cover.crouch", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "quake.cover.head", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "quake.cover.grip", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "quake.post.glass", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "quake.post.shoes", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "quake.post.canlaces", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "quake.post.bag", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "quake.post.flashlight", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "quake.post.exit", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "quake.corridor.rubble", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "quake.corridor.parent", StoryInteractionGesture.RepeatedTap);
        AssertGesture(interactions, "quake.corridor.aftershock", StoryInteractionGesture.WorldHold);
        StoryInteractable parentSignal = interactions.Single(item => item.InteractionId == "quake.corridor.parent");
        Assert.That(parentSignal.RequiredGestureCount, Is.EqualTo(3));
        Assert.That(parentSignal.gameObject.name, Is.EqualTo("ParentDoorKnockSurface"));
        Assert.That(PersistentMethods(parentSignal), Does.Contain("SetActive"));
        Assert.That(PersistentMethods(parentSignal), Does.Contain("SetTrigger"));
        Assert.That(PersistentTargets(parentSignal), Does.Contain("CanAftershockWarning_CeilingDust"));
        Assert.That(PersistentTargets(parentSignal), Does.Contain("CanAftershockWarning_CeilingCreak"));
        StoryInteractable aftershockResponse =
            interactions.Single(item => item.InteractionId == "quake.corridor.aftershock");
        Assert.That(aftershockResponse.gameObject.name, Is.EqualTo("Can_AftershockResponseSurface"));
        Assert.That(aftershockResponse.Prompt, Does.Contain("UYARISINA GÜVEN"));

        Assert.That(Find("Can_ToyWheel_Drag"), Is.Not.Null);
        Assert.That(Find("Can_ToyWheel_Drag").position.y, Is.GreaterThan(0f));
        Assert.That(Find("SafeTable_CrouchSurface").position.y, Is.GreaterThan(0f));
        Assert.That(Find("PostQuake_ListenSurface").position.y, Is.GreaterThan(0f));
        Assert.That(Find("RadioVolumeDial_Direct"), Is.Not.Null);
        Assert.That(Find("Can_FamilyPlanDrawing"), Is.Not.Null);
        Assert.That(Find("DenizShoePair_Drag"), Is.Not.Null);
        Assert.That(Find("CorridorLightRubble_Drag"), Is.Not.Null);
        Assert.That(Find("ParentVoiceBarrier"), Is.Not.Null);
        Assert.That(Find("ParentResponseWarmSignal"), Is.Not.Null);
        Assert.That(Find("ParentResponseWarmSignal").gameObject.activeSelf, Is.False);
        ParticleSystem warningDust = Find("CanAftershockWarning_CeilingDust").GetComponent<ParticleSystem>();
        Assert.That(warningDust, Is.Not.Null);
        Assert.That(warningDust.main.playOnAwake, Is.False);
        Assert.That(Find("CanAftershockWarning_CeilingCreak").GetComponent<AudioSource>(), Is.Not.Null);
    }

    [Test]
    public void EveryPhysicalTransfer_FollowsFingerToItsOwnSceneTarget()
    {
        StoryInteractable[] drags = Object.FindObjectsByType<StoryInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(interaction => interaction.InteractionGesture == StoryInteractionGesture.DragToTarget)
            .ToArray();
        Assert.That(drags, Has.Length.EqualTo(5));

        foreach (StoryInteractable interaction in drags)
        {
            DraggableItem draggable = interaction.GetComponent<DraggableItem>();
            Assert.That(draggable, Is.Not.Null, interaction.InteractionId);
            Assert.That(draggable.DropZoneOverride, Is.Not.Null, interaction.InteractionId);
            Assert.That(interaction.GestureTarget, Is.EqualTo(draggable.DropZoneOverride.transform),
                interaction.InteractionId);
            Assert.That(GetPrivate<bool>(draggable, "inputEnabled"), Is.False, interaction.InteractionId);
            Assert.That(GetPrivate<bool>(draggable, "notifyGameManager"), Is.False, interaction.InteractionId);
            Assert.That(GetPrivate<bool>(draggable, "tapToBagEnabled"), Is.False, interaction.InteractionId);
        }
    }

    [Test]
    public void Cameras_ArePortraitComposedAndCoverShotCannotCollapseIntoTableLegsOrWall()
    {
        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(10));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);

        CinemachineCamera cover = cameras.Single(camera => camera.name == "CM03R_UnderTableTwoShot");
        Assert.That(cover.transform.position.y, Is.GreaterThanOrEqualTo(2.3f));
        Assert.That(cover.transform.position.z, Is.LessThan(-4f));
        Assert.That(Vector3.Angle(
            cover.transform.forward,
            (new Vector3(-0.4f, 0.75f, 0.1f) - cover.transform.position).normalized), Is.LessThan(0.5f));

        CinemachineCamera corridor = cameras.Single(camera => camera.name == "CM03R_CorridorLong");
        RaycastHit[] hits = Physics.RaycastAll(
            corridor.transform.position,
            corridor.transform.forward,
            30f,
            ~0,
            QueryTriggerInteraction.Ignore);
        RaycastHit first = hits.OrderBy(hit => hit.distance).First();
        Assert.That(
            new[] { "CorridorLeftWall", "CorridorRightWall" }.Contains(first.transform.name),
            Is.False,
            "Koridor kamerası ilk olarak duvar göstermemeli.");
        Transform warningDust = Find("CanAftershockWarning_CeilingDust");
        Assert.That(Vector3.Angle(
                corridor.transform.forward,
                (warningDust.position - corridor.transform.position).normalized),
            Is.LessThan(21f),
            "Can'ın önce fark ettiği tavan tozu koridor kamerasının üst sınırında kaybolmamalı.");
        CinemachineCamera glass = cameras.Single(camera => camera.name == "CM03R_GlassSafeRoute");
        Transform brokenGlass = Find("BrokenGlass_Hazard");
        Assert.That(Vector3.Angle(
            glass.transform.forward,
            (brokenGlass.position + Vector3.up * 0.14f - glass.transform.position).normalized),
            Is.LessThan(1f),
            "Kırık cam kadrajı pencereyi değil doğrudan zemin tehlikesini merkezlemeli.");
    }

    [Test]
    public void EarthquakeTimeline_IsSceneAuthoredThreePhaseAndFortyEightSeconds()
    {
        PlayableDirector director = Object.FindObjectsByType<PlayableDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(candidate => candidate.name == "EarthquakeTimeline_Rebuild_48s");
        Assert.That(director.playableAsset, Is.Not.Null);
        Assert.That(director.playableAsset.duration, Is.InRange(45d, 50d));
        ParticleSystem[] particles = Object.FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(particles, Has.Length.GreaterThanOrEqualTo(6));
        Assert.That(Find("Phase1_CeilingDust"), Is.Not.Null);
        Assert.That(Find("Phase2_ShelfDust"), Is.Not.Null);
        Assert.That(Find("Phase3_FinalDust"), Is.Not.Null);
        Assert.That(Find("Phase2_ShelfDebrisChips"), Is.Not.Null);
        Assert.That(Find("Phase2_WardrobePlasterChips"), Is.Not.Null);
        Assert.That(Find("Phase3_DoorwayPlasterChips"), Is.Not.Null);
        Assert.That(particles.Count(system =>
                system.GetComponent<ParticleSystemRenderer>().renderMode ==
                ParticleSystemRenderMode.Mesh),
            Is.GreaterThanOrEqualTo(3),
            "Darbe anında yalnız kare billboard değil, dönen fiziksel döküntü parçaları da görünmeli.");
        ParticleSystem ceilingDust = Find("Phase1_CeilingDust").GetComponent<ParticleSystem>();
        Assert.That(ceilingDust.noise.enabled, Is.True);
        Material dustMaterial = ceilingDust.GetComponent<ParticleSystemRenderer>().sharedMaterial;
        Assert.That(dustMaterial, Is.Not.Null);
        Assert.That(dustMaterial.GetTexture("_BaseMap"), Is.Not.Null,
            "Toz billboard'u dokusuz kare olarak çizilmemeli.");

        Assert.That(Find("Wardrobe_Secured").gameObject.activeSelf, Is.True);
        Assert.That(Find("Wardrobe_Unsecured").gameObject.activeSelf, Is.False);
        Assert.That(Find("Shelf_Secured").gameObject.activeSelf, Is.True);
        Assert.That(Find("ExitRoute_Cleared").gameObject.activeSelf, Is.True);
    }

    [Test]
    public void PreparationComfortChoice_HasPhysicalAndNarrativePostQuakePayoff()
    {
        StorySequenceDirector director = Object.FindFirstObjectByType<StorySequenceDirector>();
        GameObject comfortToy = GetPrivate<GameObject>(director, "canComfortItem");
        StoryAuthoredBeat[] postQuakeBeats = GetPrivate<StoryAuthoredBeat[]>(director, "postQuakeBeats");
        StoryGameManager manager = Object.FindFirstObjectByType<StoryGameManager>();
        StoryFlag[] initialFlags = GetPrivate<StoryFlag[]>(manager, "initialFlags");

        Assert.That(comfortToy, Is.Not.Null);
        Assert.That(comfortToy.name, Is.EqualTo("Can_ComfortToy_PostQuake"));
        Assert.That(comfortToy.transform.IsChildOf(Find("Can_8")), Is.True);
        Assert.That(postQuakeBeats[1].completionSubtitle, Does.Contain("{COMFORT}"));
        Assert.That(initialFlags, Does.Contain(StoryFlag.BagComfortItem));
    }

    [Test]
    public void HomeSafetyPlayCorner_HasPreparedAndUnpreparedStory03Consequences()
    {
        Transform prepared = Find("CanReadingNest_Safe_Story03");
        Transform unprepared = Find("CanReadingNest_RiskImpact_Story03");
        Transform clearRoute = Find("ExitRoute_Cleared");
        Transform clutteredRoute = Find("ExitRoute_ClutteredButPassable");

        Assert.That(prepared, Is.Not.Null);
        Assert.That(unprepared, Is.Not.Null);
        Assert.That(prepared.IsChildOf(clearRoute), Is.True);
        Assert.That(unprepared.IsChildOf(clutteredRoute), Is.True);
        Assert.That(prepared.gameObject.activeInHierarchy, Is.True,
            "Story02 tamamlanmış varsayılan rotada Can'ın güvenli okuma köşesi görünmeli.");
        Assert.That(unprepared.gameObject.activeInHierarchy, Is.False,
            "Hazırlanmamış sonuç yalnız geçiş yolu temiz değilse görünmeli.");
        Assert.That(
            unprepared.GetComponentsInChildren<Transform>(true)
                .Count(candidate => candidate.name.StartsWith("ShelfImpactDebris_")),
            Is.EqualTo(4));
    }

    [Test]
    public void Preview_DisablesRealtimeShadows()
    {
        Assert.That(Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .All(light => light.shadows == LightShadows.None), Is.True);
        Assert.That(Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .All(renderer =>
                renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off &&
                !renderer.receiveShadows), Is.True);
    }

    private static void AssertGesture(
        StoryInteractable[] interactions,
        string id,
        StoryInteractionGesture gesture)
    {
        StoryInteractable interaction = interactions.Single(candidate => candidate.InteractionId == id);
        Assert.That(interaction.InteractionGesture, Is.EqualTo(gesture), id);
        Assert.That(interaction.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1), id);
    }

    private static Transform Find(string name)
    {
        return Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(transform => transform.name == name);
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }

    private static string[] PersistentMethods(StoryInteractable interaction)
    {
        return Enumerable.Range(0, interaction.OnInteracted.GetPersistentEventCount())
            .Select(interaction.OnInteracted.GetPersistentMethodName)
            .ToArray();
    }

    private static string[] PersistentTargets(StoryInteractable interaction)
    {
        return Enumerable.Range(0, interaction.OnInteracted.GetPersistentEventCount())
            .Select(interaction.OnInteracted.GetPersistentTarget)
            .Where(target => target != null)
            .Select(target => target.name)
            .ToArray();
    }
}
