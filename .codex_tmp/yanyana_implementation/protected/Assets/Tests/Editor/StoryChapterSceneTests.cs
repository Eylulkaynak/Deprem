using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StoryChapterSceneTests
{
    private const string HomeScene = "Assets/Scenes/Story_02_HomeSafety.unity";
    private const string EvacuationScene = "Assets/Scenes/Story_04_Evacuation.unity";

    [Test]
    public void LegacyScenes_ArePreservedByteForByte()
    {
        CollectionAssert.AreEqual(
            File.ReadAllBytes("Assets/Scenes/Bolum2.unity"),
            File.ReadAllBytes("Assets/Scenes/LegacyBackups/Bolum2_OriginalGameplay_2026-07-17.unity"));
        CollectionAssert.AreEqual(
            File.ReadAllBytes("Assets/Scenes/Bolum4.unity"),
            File.ReadAllBytes("Assets/Scenes/LegacyBackups/Bolum4_OriginalGameplay_2026-07-17.unity"));
    }

    [Test]
    public void HomeSafetyScene_HasDirectPhysicalInteractionsAndSixCameras()
    {
        EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
        Assert.That(Object.FindObjectsByType<StoryHomeSafetyDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1));
        AssertCommonSceneContract(16, 6);

        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        AssertGesture(interactions, "home.exit.shoes", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "home.exit.toy", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "home.exit.parcel", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "home.shelf.books", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "home.shelf.vase", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "home.shelf.frame", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "home.wardrobe.test", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "home.wardrobe.mark", StoryInteractionGesture.RepeatedTap);
        AssertGesture(interactions, "home.exit.final", StoryInteractionGesture.SwipeHorizontal);
        Assert.That(Find("ExitShoes_Stored").activeSelf, Is.False);
        Assert.That(Find("ShelfBooks_Low").activeSelf, Is.False);
        Assert.That(Find("WardrobeAnchorStrap").activeSelf, Is.False);
        Assert.That(Find("ShelfWallBracket").activeSelf, Is.False);

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);

        RuntimeAnimatorController adultController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath);
        GameObject parent = Find("Anne_Ayse");
        Assert.That(parent.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.EqualTo(adultController),
            "Anne çocuk Animator Controller'ını kullanmamalı.");
        Assert.That(parent.GetComponentsInChildren<StoryInteractable>(true), Is.Empty,
            "Hazırlık sahnesinden kalan görünmez hotspotlar anne prefabıyla taşınmamalı.");

        StoryHomeSafetyDirector director = Object.FindFirstObjectByType<StoryHomeSafetyDirector>();
        GameObject heldDrill = GetPrivate<GameObject>(director, "parentHeldDrill");
        AudioSource drillAudio = GetPrivate<AudioSource>(director, "drillWorkAudio");
        Assert.That(heldDrill, Is.Not.Null);
        Assert.That(heldDrill.activeSelf, Is.False);
        Assert.That(heldDrill.transform.parent.name, Is.EqualTo("ParentDrillHandMount"));
        Assert.That(Quaternion.Angle(heldDrill.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f),
            "Eldeki matkap el yuvasının yönünü miras almalı; dünya rotasyonunda kilitli kalmamalı.");
        Assert.That(drillAudio, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(drillAudio.clip),
            Is.EqualTo("Assets/Script/freesound_community-power-drill-90294.mp3"));
        Assert.That(drillAudio.playOnAwake, Is.False);
        Assert.That(drillAudio.loop, Is.True);
        Assert.That(drillAudio.spatialBlend, Is.EqualTo(1f).Within(0.001f));

        Transform shelfWork = GetPrivate<Transform>(director, "shelfParentWorkPoint");
        Transform wardrobeWork = GetPrivate<Transform>(director, "wardrobeParentWorkPoint");
        Vector3 shelfDirection = Find("ShelfWallBracket").transform.position - shelfWork.position;
        shelfDirection.y = 0f;
        Vector3 wardrobeDirection = Find("WardrobeAnchorStrap").transform.position - wardrobeWork.position;
        wardrobeDirection.y = 0f;
        Assert.That(Vector3.Dot(shelfWork.forward, shelfDirection.normalized), Is.GreaterThan(0.99f));
        Assert.That(Vector3.Dot(wardrobeWork.forward, wardrobeDirection.normalized), Is.GreaterThan(0.99f));
        Assert.That(Vector3.Dot(shelfWork.up, Vector3.up), Is.GreaterThan(0.999f),
            "Yetişkin çalışma noktası yalnız yatay eksende dönmeli; karakter duvara doğru devrilmemeli.");
        Assert.That(Vector3.Dot(wardrobeWork.up, Vector3.up), Is.GreaterThan(0.999f),
            "Yetişkin çalışma noktası yalnız yatay eksende dönmeli; karakter dolaba doğru devrilmemeli.");

        foreach (string id in new[] { "home.exit.shoes", "home.exit.toy", "home.exit.parcel", "home.exit.final" })
            Assert.That(interactions.Single(item => item.InteractionId == id).GestureTarget, Is.Not.Null, id);
    }

    [Test]
    public void EvacuationScene_HasContinuousRouteConsequencesAndAuthoredCameraCoverage()
    {
        EditorSceneManager.OpenScene(EvacuationScene, OpenSceneMode.Single);
        Assert.That(Object.FindObjectsByType<StoryEvacuationDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1));
        AssertCommonSceneContract(19, 13);

        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        AssertGesture(interactions, "evac.route.stairs", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "evac.route.elevator_unsafe", StoryInteractionGesture.Tap);
        AssertGesture(interactions, "evac.stairs.upper_landing", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.aftershock.handrail", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.neighbor.cane", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "evac.neighbor.light_debris", StoryInteractionGesture.SwipeDown);
        AssertGesture(interactions, "evac.neighbor.support", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.street.safe_route", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.assembly.whistle", StoryInteractionGesture.RepeatedTap);

        Assert.That(Object.FindFirstObjectByType<CinemachineImpulseSource>(), Is.Not.Null);
        Assert.That(Find("DenizFlashlightBeam"), Is.Not.Null);
        Assert.That(Find("WeakEmergencyLightRoute"), Is.Not.Null);
        Assert.That(Find("UpperNavRamp"), Is.Not.Null);
        Assert.That(Find("LowerNavRamp"), Is.Not.Null);
        Assert.That(Find("AssemblyAreaSign"), Is.Not.Null);

        RuntimeAnimatorController adultController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath);
        foreach (string neighbor in new[] { "Nermin_Neighbor_Landing", "Nermin_Neighbor_Street", "Nermin_Neighbor_Assembly" })
            Assert.That(Find(neighbor).GetComponentInChildren<Animator>(true).runtimeAnimatorController,
                Is.EqualTo(adultController), neighbor);
    }

    private static void AssertCommonSceneContract(int interactionCount, int cameraCount)
    {
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1), "Sahnede tek dünya input sahibi olmalı.");
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Has.Length.EqualTo(interactionCount));
        Assert.That(Object.FindFirstObjectByType<NavMeshSurface>(), Is.Not.Null);
        Assert.That(Object.FindFirstObjectByType<NavMeshSurface>().navMeshData, Is.Not.Null,
            "Sahnenin NavMesh verisi build edilmiş olmalı.");

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(cameraCount));
        foreach (CinemachineCamera camera in cameras)
            Assert.That(camera.Lens.FieldOfView, Is.InRange(38f, 50f), camera.name);

        Assert.That(Find("SafeArea")?.GetComponent<StorySafeAreaPanel>(), Is.Not.Null);
        Assert.That(Object.FindObjectsByType<StoryActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            Is.Empty, "Ortada görev yaptıran bir aksiyon butonu olmamalı.");
        Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(component => component.GetType().Name),
            Has.None.Matches<string>(name => name is "LevelManager" or "CardGameManager" or "DoorMissionManager" or
                "SafeAreaMissionManager" or "StairChoiceManager" or "TouchManager"));
        Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(text => (text.text ?? string.Empty).ToUpperInvariant()),
            Has.None.Contains("TIKLA"), "Görev mekaniği merkezde 'Tıkla' yazan UI'a dönmemeli.");
    }

    private static void AssertGesture(StoryInteractable[] interactions, string id, StoryInteractionGesture gesture)
    {
        StoryInteractable interactable = interactions.Single(item => item.InteractionId == id);
        Assert.That(interactable.InteractionGesture, Is.EqualTo(gesture), id);
        Assert.That(interactable.GetComponentsInChildren<Collider>(true), Is.Not.Empty,
            id + " doğrudan sahnedeki nesne üzerinden raycast almalı.");
    }

    private static GameObject Find(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == name);
        return transform != null ? transform.gameObject : null;
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }
}
