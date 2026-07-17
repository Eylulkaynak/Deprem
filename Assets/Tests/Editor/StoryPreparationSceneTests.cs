using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public sealed class StoryPreparationSceneTests
{
    private const string ScenePath = "Assets/Scenes/Story_01_BagPreparation.unity";
    private const string OriginalScenePath = "Assets/Scenes/bolum1.unity";
    private const string BackupScenePath = "Assets/Scenes/LegacyBackups/bolum1_OriginalGameplay_2026-07-16.unity";

    [SetUp]
    public void OpenPreparationScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void OriginalGameplayBackup_IsByteIdenticalAndIndependent()
    {
        Assert.That(File.Exists(OriginalScenePath), Is.True);
        Assert.That(File.Exists(BackupScenePath), Is.True);
        CollectionAssert.AreEqual(File.ReadAllBytes(OriginalScenePath), File.ReadAllBytes(BackupScenePath));
        Assert.That(ScenePath, Is.Not.EqualTo(OriginalScenePath));
        Assert.That(ScenePath, Is.Not.EqualTo(BackupScenePath));
    }

    [Test]
    public void Scene_HasStoryCoreAndNoLegacyBagGameplayManagers()
    {
        Assert.That(GameObject.Find("STORY_01_BAG_PREPARATION"), Is.Not.Null);
        Assert.That(Object.FindObjectsByType<StoryPreparationDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        StoryTouchManager touchManager = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivateValue<bool>(touchManager, "directWorldGestures"), Is.True);
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        DraggableItem[] itemBridges = Object.FindObjectsByType<DraggableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(itemBridges, Has.Length.EqualTo(26));
        Assert.That(itemBridges.All(item => !GetPrivateValue<bool>(item, "inputEnabled")), Is.True);
        Assert.That(itemBridges.All(item => !GetPrivateValue<bool>(item, "notifyGameManager")), Is.True);
        Assert.That(Object.FindObjectsByType<Bolum1GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        Assert.That(Object.FindObjectsByType<BagDropZone>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(item => item.name == "InteractionMarker"), Is.False,
            "World interactions must use the authored object colliders, not colored debug rings or arrows.");
        StoryInteractable familyPlan = FindInactive("Meet_Parent").GetComponent<StoryInteractable>();
        Assert.That(familyPlan.ReturnCameraAfterCompletion, Is.False,
            "The parent camera must stay composed until the dialogue itself advances.");
    }

    [Test]
    public void ItemDecisions_CoverAllOriginalItemsAndRequiredAfadGroups()
    {
        StoryPreparationItem[] items = Object.FindObjectsByType<StoryPreparationItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(items, Has.Length.EqualTo(26));
        Assert.That(items.Count(item => item.Recommended), Is.EqualTo(13));
        Assert.That(items.Count(item => !item.Recommended), Is.EqualTo(13));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Signal), Is.EqualTo(8));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Food), Is.EqualTo(6));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Health), Is.EqualTo(7));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Warmth), Is.EqualTo(5));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Signal && item.Recommended), Is.EqualTo(4));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Food && item.Recommended), Is.EqualTo(2));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Health && item.Recommended), Is.EqualTo(5));
        Assert.That(items.Count(item => item.Category == StoryPreparationCategory.Warmth && item.Recommended), Is.EqualTo(2));
        Assert.That(items.Where(item => item.Recommended).Select(item => item.Flag).Distinct().Count(), Is.EqualTo(13));
        Assert.That(items.Where(item => item.Recommended).All(item => item.Flag != StoryFlag.None), Is.True);
        Assert.That(items.Where(item => !item.Recommended).All(item => item.Flag == StoryFlag.None), Is.True);
        Assert.That(items.All(item => item.Interactable.InteractionGesture == StoryInteractionGesture.DragToBag), Is.True);
    }

    [Test]
    public void EveryItem_HasDialoguePhysicalResultAndPersistentSceneEvent()
    {
        StoryPreparationItem[] items = Object.FindObjectsByType<StoryPreparationItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StoryPreparationItem item in items)
        {
            Assert.That(item.DisplayName, Is.Not.Empty, item.name);
            Assert.That(item.ChildLine, Is.Not.Empty, item.name);
            Assert.That(item.ParentLine, Is.Not.Empty, item.name);
            Assert.That(item.Interactable, Is.Not.Null, item.name);
            Assert.That(item.Interactable.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1), item.name);
            GameObject packedVisual = GetPrivate<GameObject>(item, "packedVisual");
            Assert.That(packedVisual, Is.Not.Null, item.name);
            Assert.That(packedVisual.activeSelf, Is.False, item.name);
            if (!item.Recommended)
                Assert.That(item.ConsequenceRoot, Is.Not.Null, item.name);
        }
    }

    [Test]
    public void AllApproachPoints_AreOnReachableNavMesh()
    {
        StoryPlayerMovement player = Object.FindFirstObjectByType<StoryPlayerMovement>();
        Assert.That(player, Is.Not.Null);
        Assert.That(NavMesh.SamplePosition(player.transform.position, out NavMeshHit start, 1.5f, NavMesh.AllAreas), Is.True);
        foreach (StoryInteractable interactable in Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Assert.That(NavMesh.SamplePosition(interactable.InteractionPoint.position, out NavMeshHit destination, 1.5f, NavMesh.AllAreas),
                Is.True, interactable.InteractionId);
            NavMeshPath path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(start.position, destination.position, NavMesh.AllAreas, path), Is.True, interactable.InteractionId);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), interactable.InteractionId);
        }
    }

    [Test]
    public void Cameras_AreComposedWithinPreparationLensStandard()
    {
        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(10));
        Assert.That(cameras.All(camera => camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);
        foreach (string cameraName in new[]
                 {
                     "CM_PreparationOverview", "CM_PreparationBag", "CM_PreparationParent", "CM_PreparationSignal",
                     "CM_PreparationFood", "CM_PreparationHealth", "CM_PreparationWarmth", "CM_PreparationWrongChoice",
                     "CM_PreparationBagFit", "CM_PreparationExitShelf"
                 })
            Assert.That(cameras.Any(camera => camera.name == cameraName), Is.True, cameraName);

        AssertPortraitPoint(cameras.Single(camera => camera.name == "CM_PreparationBag"),
            CombinedBounds(GameObject.Find("OpenEmergencyBag_OriginalBolum1")).center, "bag close-up");
        AssertPortraitPoint(cameras.Single(camera => camera.name == "CM_PreparationParent"),
            CombinedBounds(GameObject.Find("Anne_Ayse")).center, "parent conversation");
    }

    [Test]
    public void CategoryCameras_KeepEveryTableItemAndOpenBagInDirectDragFrame()
    {
        GameObject openBag = GameObject.Find("OpenEmergencyBag_OriginalBolum1");
        Assert.That(openBag, Is.Not.Null);
        Vector3 bagCenter = CombinedBounds(openBag).center;

        foreach ((StoryPreparationCategory category, string cameraName) in new[]
                 {
                     (StoryPreparationCategory.Signal, "CM_PreparationSignal"),
                     (StoryPreparationCategory.Food, "CM_PreparationFood"),
                     (StoryPreparationCategory.Health, "CM_PreparationHealth"),
                     (StoryPreparationCategory.Warmth, "CM_PreparationWarmth")
                 })
        {
            CinemachineCamera source = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(camera => camera.name == cameraName);
            AssertPortraitPoint(source, bagCenter, cameraName + " open bag");

            StoryPreparationItem[] categoryItems = Object.FindObjectsByType<StoryPreparationItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.Category == category)
                .ToArray();
            Assert.That(categoryItems, Is.Not.Empty, category.ToString());
            foreach (StoryPreparationItem item in categoryItems)
                AssertPortraitPoint(source, CombinedBounds(item.gameObject).center, cameraName + " " + item.DisplayName);
        }
    }

    [Test]
    public void Characters_HaveDistinctHeightsAndNoPrimitiveBodyParts()
    {
        GameObject deniz = GameObject.Find("Deniz_12");
        GameObject parent = GameObject.Find("Anne_Ayse");
        GameObject can = GameObject.Find("Can_8");
        Assert.That(deniz, Is.Not.Null);
        Assert.That(parent, Is.Not.Null);
        Assert.That(can, Is.Not.Null);
        float denizHeight = CombinedBounds(deniz).size.y;
        float parentHeight = CombinedBounds(parent).size.y;
        float canHeight = CombinedBounds(can).size.y;
        Assert.That(parentHeight, Is.GreaterThan(denizHeight));
        Assert.That(denizHeight, Is.GreaterThan(canHeight));
        Assert.That(FindChild(deniz, "CharacterSource_Boy0_Deniz"), Is.Not.Null);
        Assert.That(FindChild(deniz, "CharacterSource_LowPolyPeople_Deniz"), Is.Null,
            "Deniz already had an approved Boy0 look and must never be replaced by the adult Low Poly People pack.");
        Assert.That(FindChild(parent, "CharacterSource_RGPoly_Anne"), Is.Not.Null);
        Assert.That(FindChild(can, "CharacterSource_Boy0Variant_Can"), Is.Not.Null);
        Assert.That(FindChild(parent, "CharacterSource_LowPolyPeople_Anne"), Is.Null);
        Assert.That(FindChild(can, "CharacterSource_LowPolyPeople_Can"), Is.Null);
        Assert.That(parent.GetComponentInChildren<Animator>(true).runtimeAnimatorController,
            Is.EqualTo(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                StoryAnimationLibraryBuilder.AdultControllerPath)),
            "Anne çocuk locomotion/etkileşim kliplerini kullanmamalı.");
        string[] denizMeshes = deniz.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.sharedMesh != null).Select(renderer => renderer.sharedMesh.name).OrderBy(name => name).ToArray();
        string[] canMeshes = can.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.sharedMesh != null).Select(renderer => renderer.sharedMesh.name).OrderBy(name => name).ToArray();
        Assert.That(denizMeshes, Is.Not.Empty);
        Assert.That(canMeshes, Is.Not.Empty);
        Material denizMaterial = deniz.GetComponentsInChildren<SkinnedMeshRenderer>(true).First().sharedMaterial;
        Material canMaterial = can.GetComponentsInChildren<SkinnedMeshRenderer>(true).First().sharedMaterial;
        Assert.That(denizMaterial, Is.Not.SameAs(canMaterial), "Deniz ve Can farklı renk varyantları kullanmalı.");
        string[] removedPlaceholderParts =
        {
            "Anne_Cardigan", "Anne_CardiganTrim", "Anne_HairCap", "Anne_HairBun",
            "Can_BeanieTop", "Can_BeanieBand", "Can_Hoodie", "Can_HoodiePocket", "Deniz_NeckBand"
        };
        foreach (GameObject character in new[] { deniz, parent, can })
        {
            Assert.That(character.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Any(renderer => renderer.sharedMesh != null), Is.True, character.name + " must use a rigged character mesh.");
            Assert.That(character.GetComponentsInChildren<Transform>(true)
                .Any(item => removedPlaceholderParts.Contains(item.name)), Is.False,
                character.name + " must not contain primitive placeholder body parts.");
        }
    }

    [Test]
    public void Scene_HasMinimalSafeAreaUiAndPhysicalBagStates()
    {
        Assert.That(GameObject.Find("StoryUI_Preparation"), Is.Not.Null);
        Assert.That(GameObject.Find("SafeArea"), Is.Not.Null);
        Assert.That(GameObject.Find("ObjectiveStrip"), Is.Not.Null);
        Assert.That(FindInactive("SubtitlePanel"), Is.Not.Null);
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(ui, Is.Not.Null);
        Assert.That(GetPrivateValue<float>(ui, "subtitleCharactersPerSecond"), Is.GreaterThanOrEqualTo(10f));
        Assert.That(FindInactive("ContextPrompt"), Is.Null);
        Assert.That(FindInactive("ContextActionButton"), Is.Null);
        Assert.That(Object.FindObjectsByType<StoryActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        Assert.That(GameObject.Find("OpenEmergencyBag_OriginalBolum1"), Is.Not.Null);
        GameObject bagBlocker = GameObject.Find("OpenBag_PhysicalBlocker");
        Assert.That(bagBlocker, Is.Not.Null,
            "Ana karakter açık çantanın içinden geçmemeli; çanta NavMesh üretimine giren fiziksel bir engel taşımalı.");
        BoxCollider bagCollider = bagBlocker.GetComponent<BoxCollider>();
        Assert.That(bagCollider, Is.Not.Null);
        Assert.That(bagCollider.isTrigger, Is.False);
        Assert.That(bagCollider.size.x, Is.GreaterThan(0.6f));
        Assert.That(bagCollider.size.z, Is.GreaterThan(0.55f));
        Assert.That(GameObject.Find("OriginalBolum1_BagDropZone"), Is.Not.Null);
        Assert.That(FindInactive("ClosedEmergencyBag_OriginalBolum1"), Is.Not.Null);
        Assert.That(FindInactive("Deniz_WornPreparedBag"), Is.Not.Null);
        Assert.That(FindInactive("PreparedBag_ExitShelf"), Is.Not.Null);
        foreach (string bagName in new[]
                 {
                     "OpenEmergencyBag_OriginalBolum1", "ClosedEmergencyBag_OriginalBolum1",
                     "Deniz_WornPreparedBag", "PreparedBag_ExitShelf", "OverweightPreviewBag_OriginalBolum1"
                 })
        {
            Transform bag = FindInactive(bagName);
            Assert.That(bag, Is.Not.Null, bagName);
            Assert.That(bag.GetComponentsInChildren<Transform>(true).Any(item => item.name == "OriginalBolum1BagSource"),
                Is.True, bagName + " must use the original bolum1 bag model");
        }
        Assert.That(FindInactive("PackedItemVisuals_OriginalBag").childCount, Is.EqualTo(26));
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(item => item.name.StartsWith("OriginalBag_ItemSlot_")), Is.EqualTo(32));
        Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sprites/Bolum1/Open Backpack/model.obj"), Is.Not.Null);
        foreach (string interactionName in new[]
                 {
                     "Inspect_EmptyBag", "Final_TestBagWeight", "Final_AdjustStraps", "Final_PlaceBagAtExit"
                 })
        {
            StoryInteractable interactable = FindInactive(interactionName).GetComponent<StoryInteractable>();
            Assert.That(interactable, Is.Not.Null, interactionName);
            Assert.That(interactable.InteractFromAnywhere, Is.True, interactionName + " must react on the touched world object");
        }
    }

    private static T GetPrivate<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(target) as T;
    }

    private static T GetPrivateValue<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }

    private static Transform FindChild(GameObject root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
    }

    private static Transform FindInactive(string name)
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
    }

    private static Bounds CombinedBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void AssertPortraitPoint(CinemachineCamera source, Vector3 worldPoint, string label)
    {
        GameObject probeObject = new GameObject("PreparationPortraitCameraProbe");
        try
        {
            Camera probe = probeObject.AddComponent<Camera>();
            probe.enabled = false;
            probe.aspect = 1080f / 1920f;
            probe.fieldOfView = source.Lens.FieldOfView;
            probe.nearClipPlane = source.Lens.NearClipPlane;
            probe.farClipPlane = source.Lens.FarClipPlane;
            probe.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Vector3 viewport = probe.WorldToViewportPoint(worldPoint);
            Assert.That(viewport.z, Is.GreaterThan(0f), label + " camera front");
            Assert.That(viewport.x, Is.InRange(0.08f, 0.92f), label + " horizontal direct-drag frame");
            Assert.That(viewport.y, Is.InRange(0.12f, 0.82f), label + " vertical direct-drag frame");
        }
        finally
        {
            Object.DestroyImmediate(probeObject);
        }
    }
}
