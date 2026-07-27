using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StoryHomeSafetyRebuildPreviewTests
{
    private const string ScenePath = "Assets/Scenes/Story_02_RebuildPreview.unity";
    private const string SharedHomePath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";

    [SetUp]
    public void OpenPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void Preview_UsesConnectedSharedHomeAndIndependentNavigation()
    {
        GameObject root = GameObject.Find("STORY_02_REBUILD_PREVIEW");
        Assert.That(root, Is.Not.Null);

        Transform sharedHome = Find("StoryHome_Shared");
        Assert.That(sharedHome, Is.Not.Null);
        Assert.That(
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome.gameObject),
            Is.EqualTo(SharedHomePath));
        Assert.That(sharedHome.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(sharedHome.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(sharedHome.localScale, Is.EqualTo(Vector3.one));

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        Assert.That(surface, Is.Not.Null);
        Assert.That(surface.navMeshData, Is.Not.Null);
        Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath), Is.True,
            "Onaylanan Story 02 sahnesi yayın rotasında olmalı.");
    }

    [Test]
    public void Preview_HasSingleInputOwnerNoCenterButtonAndSixComposedCameras()
    {
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryHomeSafetyDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(6));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);
        Assert.That(cameras.Select(camera => camera.name).Distinct().Count(), Is.EqualTo(6));
    }

    [Test]
    public void Preview_HasRouteNeighborSafetyAndFinalBeatsAsWorldInteractions()
    {
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(interactions, Has.Length.EqualTo(23));

        AssertGesture(interactions, "home.route.initial", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "home.neighbor.door", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "home.neighbor.envelope", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "home.neighbor.plan", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "home.inspect.wardrobe", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "home.inspect.shelf", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "home.risk.safeplay", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "home.shelf.mark", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.shelf.retest", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.wardrobe.test", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.wardrobe.mark", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.wardrobe.retest", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.exit.final", StoryInteractionGesture.DragToTarget);

        Assert.That(Find("Nermin_Neighbor"), Is.Not.Null);
        Assert.That(Find("Nermin_Cane"), Is.Not.Null);
        Assert.That(Find("NerminEnvelope_Start"), Is.Not.Null);
        Assert.That(Find("Nermin_EvacuationPlan"), Is.Not.Null);
        Assert.That(Find("Can_ToyCar_InitialRoute"), Is.Not.Null);
        Assert.That(Find("Can_ToyCar_FinalStart"), Is.Not.Null);
        Assert.That(Find("Can_ToyCar_FinalFinish"), Is.Not.Null);
        Assert.That(Find("Can_ToyCar_Pocket"), Is.Not.Null);
        Assert.That(Find("ShelfFallZone_Unstable"), Is.Not.Null);
        Assert.That(Find("ShelfFallZone_Stabilized"), Is.Not.Null);
        Assert.That(Find("WardrobeFallZone_Unstable"), Is.Not.Null);
        Assert.That(Find("WardrobeFallZone_Stabilized"), Is.Not.Null);
        Assert.That(Find("CanReadingNest_Risk"), Is.Not.Null);
        Assert.That(Find("CanReadingNest_Safe"), Is.Not.Null);
    }

    [Test]
    public void EveryPhysicalTransfer_FollowsFingerToItsOwnSceneTarget()
    {
        StoryInteractable[] drags = Object.FindObjectsByType<StoryInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(interaction => interaction.InteractionGesture == StoryInteractionGesture.DragToTarget)
            .ToArray();
        Assert.That(drags, Has.Length.EqualTo(13));

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
            Assert.That(interaction.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1),
                interaction.InteractionId);
        }

        string[] requiredTransfers =
        {
            "home.exit.shoes",
            "home.exit.toy",
            "home.exit.parcel",
            "home.neighbor.plan",
            "home.risk.safeplay",
            "home.shelf.books",
            "home.shelf.vase",
            "home.shelf.frame",
            "home.shelf.bracket",
            "home.wardrobe.strap"
        };
        foreach (string id in requiredTransfers)
            Assert.That(drags.Any(interaction => interaction.InteractionId == id), Is.True, id);
    }

    [Test]
    public void Director_WiresPreludeCheckpointsAdultWorkAndFinalRouteWithoutSoftlock()
    {
        StoryHomeSafetyDirector director = Object.FindFirstObjectByType<StoryHomeSafetyDirector>();
        Assert.That(GetPrivate<StoryInteractable>(director, "testInitialRoute"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "openDoorForNermin"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "returnNerminEnvelope"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "nermin"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "placeEvacuationPlan"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "evacuationPlanInHand"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "evacuationPlan"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "finalRouteCarStart"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "finalRouteCarFinish"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "canToyCarPocket"), Is.Not.Null);
        Assert.That(GetPrivate<bool>(director, "physicalRouteFlow"), Is.True);
        Assert.That(GetPrivate<GameObject>(director, "shelfRiskZoneUnstable"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "shelfRiskZoneSecured"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "wardrobeRiskZoneUnstable"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "wardrobeRiskZoneSecured"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "canReadingNestRisk"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "canReadingNestSafe"), Is.Not.Null);

        Assert.That(GetPrivate<Transform>(director, "shelfParentWorkPoint"), Is.Not.Null);
        Assert.That(GetPrivate<Transform>(director, "wardrobeParentWorkPoint"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "parentHeldDrill"), Is.Not.Null);
        Assert.That(GetPrivate<AudioSource>(director, "drillWorkAudio"), Is.Not.Null);

        Assert.That(GetPrivate<StoryInteractable>(director, "moveShoes"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "moveToy"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "moveParcel"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "inspectExit"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "testClearedExitDoor"), Is.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "lowerBooks"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "lowerVase"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "lowerFrame"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "markShelfAnchor"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "handShelfBracket"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "testSecuredShelf"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "handWardrobeStrap"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "testSecuredWardrobe"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "testExitDoor"), Is.Not.Null);
    }

    [Test]
    public void InitialWorldState_PreservesStory01ConsequencesAndStory03Continuity()
    {
        Assert.That(Find("Wardrobe_Unsecured").gameObject.activeSelf, Is.True);
        Assert.That(Find("Wardrobe_Secured").gameObject.activeSelf, Is.False);
        Assert.That(Find("Shelf_Unsecured").gameObject.activeSelf, Is.True);
        Assert.That(Find("Shelf_Secured").gameObject.activeSelf, Is.False);
        Assert.That(Find("EmergencyBag").gameObject.activeSelf, Is.True,
            "Story 01'de hazırlanan çanta Story 02 evinde görünür kalmalı.");
        Assert.That(Find("ExitShoes_Stored").gameObject.activeSelf, Is.False);
        Assert.That(Find("ShelfBooks_Low").gameObject.activeSelf, Is.False);
        Assert.That(Find("WardrobeAnchorStrap").gameObject.activeSelf, Is.False);
        Assert.That(Find("ShelfWallBracket").gameObject.activeSelf, Is.False);
        Assert.That(Find("Nermin_Neighbor").gameObject.activeSelf, Is.False);
        Assert.That(Find("Nermin_EvacuationPlan").gameObject.activeSelf, Is.False);
        Assert.That(Find("Nermin_EvacuationPlan_InHand").gameObject.activeSelf, Is.False);
        Assert.That(Find("Can_ToyCar_FinalStart").gameObject.activeSelf, Is.False);
        Assert.That(Find("Can_ToyCar_Pocket").gameObject.activeSelf, Is.False);
        Assert.That(Find("ShelfFallZone_Unstable").gameObject.activeSelf, Is.False);
        Assert.That(Find("ShelfFallZone_Stabilized").gameObject.activeSelf, Is.False);
        Assert.That(Find("WardrobeFallZone_Unstable").gameObject.activeSelf, Is.False);
        Assert.That(Find("WardrobeFallZone_Stabilized").gameObject.activeSelf, Is.False);
        Assert.That(Find("CanReadingNest_Risk").gameObject.activeSelf, Is.True);
        Assert.That(Find("CanReadingNest_Safe").gameObject.activeSelf, Is.False);
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
}
