using System;
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

public sealed class StoryEvacuationRebuildPreviewTests
{
    private const string ScenePath = "Assets/Scenes/Story_04_RebuildPreview.unity";

    [SetUp]
    public void OpenPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void Preview_StaysIndependentAndUsesSingleInputOwner()
    {
        Assert.That(
            EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath),
            Is.True);
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryEvacuationDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);
        StoryEvacuationDirector director = Object.FindFirstObjectByType<StoryEvacuationDirector>();
        Assert.That(director.RevisedFlow, Is.True);
        Assert.That(GetPrivate<float>(director, "revisedAftershockMinimumDuration"), Is.EqualTo(10f));
    }

    [Test]
    public void Preview_UsesPhysicalNeighborHelpFacadeClearAndReunion()
    {
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(interactions, Has.Length.EqualTo(32));

        AssertGesture(interactions, "evac.r04.corridor.inspect", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.r04.neighbor.cardboard", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.foam", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.cane", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.support", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.r04.exit.facade_clear", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.r04.street.inspect", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "evac.r04.assembly.handoff", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.r04.assembly.can", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.r04.assembly.reunion", StoryInteractionGesture.Approach);
        StoryInteractable reunion = interactions.Single(candidate =>
            candidate.InteractionId == "evac.r04.assembly.reunion");
        Assert.That(reunion.InteractionPoint.name, Is.EqualTo("R04_FamilyReunionPoint"));
        Assert.That(reunion.InteractionRange, Is.LessThanOrEqualTo(0.65f));
        Assert.That(interactions.Single(candidate =>
                candidate.InteractionId == "evac.r04.assembly.handoff").FocusCameraZone,
            Is.EqualTo(StoryCameraZoneId.EvacuationAssemblyRadio));
        Assert.That(interactions.Single(candidate =>
                candidate.InteractionId == "evac.r04.assembly.firstaid").FocusCameraZone,
            Is.EqualTo(StoryCameraZoneId.EvacuationAssemblyRadio));
        Assert.That(interactions.Single(candidate =>
                candidate.InteractionId == "evac.r04.assembly.cloth_fallback").FocusCameraZone,
            Is.EqualTo(StoryCameraZoneId.EvacuationAssemblyRadio));
        StoryInteractable corridorEntry = interactions.Single(candidate =>
            candidate.InteractionId == "evac.r04.corridor.inspect");
        Assert.That(corridorEntry.WorldSelectable, Is.False,
            "Koridor dinleme bir görünmez hotspot tıklaması değil, gerçek eşik geçişi olmalı.");
        Assert.That(corridorEntry.GetComponent<Renderer>().enabled, Is.False,
            "Oyunvari amber eşik şeridi rebuild sahnesinde görünmemeli.");
        Assert.That(corridorEntry.GetComponent<BoxCollider>().isTrigger, Is.True);
        Assert.That(PersistentMethods(corridorEntry), Does.Contain("SetTrigger"));
        string canAnimatorTargetName = Find("Can_8").GetComponentInChildren<Animator>().gameObject.name;
        Assert.That(PersistentTargets(corridorEntry), Does.Contain(canAnimatorTargetName));
        StoryInteractable streetRead = interactions.Single(candidate =>
            candidate.InteractionId == "evac.r04.street.inspect");
        Assert.That(streetRead.GestureTarget, Is.EqualTo(Find("SafeOpenSidewalk")));
        Assert.That(PersistentMethods(streetRead), Does.Contain("SetTrigger"));
        Assert.That(PersistentTargets(streetRead), Does.Contain(canAnimatorTargetName));
        Assert.That(Find("LooseFacadeSign").GetComponent<Animation>()?.clip, Is.Not.Null);
        Assert.That(Find("StreetWarningGlassShard_Slide").GetComponent<Animation>()?.clip, Is.Not.Null);
        AudioSource streetCreak = Find("StreetWarning_LooseSignCreak").GetComponent<AudioSource>();
        Assert.That(streetCreak.clip, Is.Not.Null);
        Assert.That(streetCreak.playOnAwake, Is.False);
        StoryEvacuationDirector director = Object.FindFirstObjectByType<StoryEvacuationDirector>();
        Assert.That(GetPrivate<Animation>(director, "streetInspectSignAnimation"), Is.Not.Null);
        Assert.That(GetPrivate<Animation>(director, "streetInspectShardAnimation"), Is.Not.Null);
        Assert.That(GetPrivate<AudioSource>(director, "streetInspectCreak"), Is.EqualTo(streetCreak));

        Assert.That(Find("AssemblyWorker"), Is.Not.Null);
        Assert.That(Find("Anne_Assembly_Reunion"), Is.Not.Null);
        Assert.That(Find("Baba_Assembly_Reunion"), Is.Not.Null);
        Assert.That(Find("Anne_Assembly_Reunion").gameObject.activeSelf, Is.False);
        Assert.That(Find("Baba_Assembly_Reunion").gameObject.activeSelf, Is.False);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(Find("Anne_Assembly_Reunion").eulerAngles.y, 180f)),
            Is.LessThan(0.1f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(Find("Baba_Assembly_Reunion").eulerAngles.y, 180f)),
            Is.LessThan(0.1f));
        Assert.That(Find("R04_FamilyReunionPoint"), Is.Not.Null);
        Assert.That(Find("FamilyHeadcountPending_2of4").gameObject.activeSelf, Is.True);
        Assert.That(Find("FamilyHeadcountComplete_4of4").gameObject.activeSelf, Is.False);
        Transform comfortToy = Find("CanComfortToy_Assembly");
        Assert.That(comfortToy, Is.Not.Null);
        Assert.That(comfortToy.gameObject.activeSelf, Is.False);
        Transform expectedRightHand = Find("Can_8").GetComponentInChildren<Animator>()
            .GetBoneTransform(HumanBodyBones.RightHand);
        Assert.That(comfortToy.parent, Is.SameAs(expectedRightHand));
        Assert.That(GetPrivate<GameObject>(
            Object.FindFirstObjectByType<StoryEvacuationDirector>(),
            "comfortToyAtAssembly"), Is.EqualTo(comfortToy.gameObject));
    }

    [Test]
    public void Preview_UsesPreparedItemsAndSafeFallbackObjects()
    {
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach ((string prepared, string fallback) in new[]
                 {
                     ("evac.r04.assembly.radio", "evac.r04.assembly.radio_fallback"),
                     ("evac.r04.assembly.firstaid", "evac.r04.assembly.cloth_fallback"),
                     ("evac.r04.assembly.water", "evac.r04.assembly.water_fallback"),
                     ("evac.r04.assembly.blanket", "evac.r04.assembly.windbreak_fallback"),
                     ("evac.r04.assembly.contact", "evac.r04.assembly.registry_fallback"),
                     ("evac.r04.assembly.whistle", "evac.r04.assembly.call_fallback")
                 })
        {
            Assert.That(interactions.Any(item => item.InteractionId == prepared), Is.True, prepared);
            Assert.That(interactions.Any(item => item.InteractionId == fallback), Is.True, fallback);
        }

        Assert.That(Find("RadioPrepared_Use").gameObject.activeSelf, Is.True);
        Assert.That(Find("WorkerMegaphone_Fallback").gameObject.activeSelf, Is.False);
        Assert.That(Find("FirstAidPrepared_Drag").gameObject.activeSelf, Is.True);
        Assert.That(Find("CleanCloth_Fallback_Drag").gameObject.activeSelf, Is.False);
        Assert.That(Find("WaterPrepared_Drag").gameObject.activeSelf, Is.True);
        Assert.That(Find("StationWaterCup_Fallback_Drag").gameObject.activeSelf, Is.False);
        Assert.That(Find("BlanketPrepared_Drag").gameObject.activeSelf, Is.True);
        Assert.That(Find("WindbreakTent_Fallback").gameObject.activeSelf, Is.False);
        Assert.That(Find("ContactCardPrepared_Drag").gameObject.activeSelf, Is.True);
        Assert.That(Find("RegistryPencil_Fallback").gameObject.activeSelf, Is.False);
        Assert.That(Find("RadioPhysicalTuningDial"), Is.Not.Null);
        Assert.That(Find("RadioDialMount"), Is.Not.Null);
        Assert.That(Find("RadioDialPointer"), Is.Not.Null);
        Assert.That(Find("RadioTunedIndicator").gameObject.activeSelf, Is.False);
        Assert.That(Find("WorkerRadioLiveIndicator").gameObject.activeSelf, Is.False);
        Assert.That(Find("RadioDialMount").parent.name, Is.EqualTo("RadioPrepared_Use"));
        Assert.That(Find("RadioPhysicalTuningDial").parent.name, Is.EqualTo("RadioDialMount"));
        Assert.That(Find("RadioDialPointer").parent.name, Is.EqualTo("RadioPhysicalTuningDial"));
        Assert.That(Find("RadioTunedIndicator").parent.name, Is.EqualTo("RadioPrepared_Use"));
        Assert.That(Find("WorkerRadioLiveIndicator").parent.name, Is.EqualTo("WorkerMegaphone_Fallback"));
        StoryInteractable preparedRadio = interactions.Single(item =>
            item.InteractionId == "evac.r04.assembly.radio");
        StoryInteractable fallbackRadio = interactions.Single(item =>
            item.InteractionId == "evac.r04.assembly.radio_fallback");
        Assert.That(preparedRadio.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.EvacuationAssemblyRadio));
        Assert.That(fallbackRadio.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.EvacuationAssemblyRadio));
        Assert.That(preparedRadio.ReturnCameraAfterCompletion, Is.True);
        Assert.That(preparedRadio.ReturnCameraZone, Is.EqualTo(StoryCameraZoneId.EvacuationAssembly));
        Assert.That(fallbackRadio.ReturnCameraAfterCompletion, Is.True);
        Assert.That(fallbackRadio.ReturnCameraZone, Is.EqualTo(StoryCameraZoneId.EvacuationAssembly));

        Animation tuneAnimation = Find("RadioPhysicalTuningDial").GetComponent<Animation>();
        Assert.That(tuneAnimation, Is.Not.Null);
        Assert.That(tuneAnimation.clip, Is.Not.Null);
        AudioSource preparedBroadcast = Find("RadioPreparedOfficialBroadcast").GetComponent<AudioSource>();
        AudioSource fallbackBroadcast = Find("WorkerRadioOfficialBroadcast").GetComponent<AudioSource>();
        Assert.That(preparedBroadcast.clip, Is.Not.Null);
        Assert.That(fallbackBroadcast.clip, Is.Not.Null);
        Assert.That(preparedBroadcast.playOnAwake, Is.False);
        Assert.That(fallbackBroadcast.playOnAwake, Is.False);
    }

    [Test]
    public void EveryPhysicalTransfer_FollowsFingerToItsOwnWorldTarget()
    {
        StoryInteractable[] drags = Object.FindObjectsByType<StoryInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(interaction => interaction.InteractionGesture == StoryInteractionGesture.DragToTarget)
            .ToArray();
        Assert.That(drags, Has.Length.EqualTo(9));

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
    public void Preview_HasBakedNavigationPortraitCamerasAndNoGroundInstruction()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        Assert.That(surface, Is.Not.Null);
        Assert.That(surface.navMeshData, Is.Not.Null);
        Assert.That(Find("UpperNavRamp").GetComponent<Renderer>().enabled, Is.False,
            "NavMesh yardım rampası gerçek üst basamakları kapatmamalı.");
        Assert.That(Find("LowerNavRamp").GetComponent<Renderer>().enabled, Is.False,
            "NavMesh yardım rampası gerçek alt basamakları kapatmamalı.");
        Renderer safeSidewalk = Find("SafeOpenSidewalk").GetComponent<Renderer>();
        Renderer unsafeShortcut = Find("UnsafeGlassShortcut").GetComponent<Renderer>();
        Assert.That(safeSidewalk.sharedMaterial.name, Does.Contain("Concrete"));
        Assert.That(unsafeShortcut.sharedMaterial, Is.EqualTo(safeSidewalk.sharedMaterial),
            "Güvenli cevap teal/coral zemin rengiyle verilmemeli; tehlikeyi cam ve tabela anlatmalı.");

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(14));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);

        Assert.That(Find("SafeRouteLabel"), Is.Null);
        Assert.That(Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .All(text => !string.Equals(
                text.text?.Trim(),
                "AÇIK ROTA",
                StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Exterior_UsesCuratedTownSetColoredEmergencyVehicleAndAuthoredAssembly()
    {
        Transform worldBox = Find("Story04_CityWorldBox");
        Assert.That(worldBox, Is.Not.Null);
        Assert.That(Find("CityWorldBox_Ground").GetComponent<Collider>(), Is.Null);
        foreach (string panelName in new[]
                 {
                     "CityWorldBox_NorthSky",
                     "CityWorldBox_NorthHaze",
                     "CityWorldBox_WestSky",
                     "CityWorldBox_EastSky",
                     "CityWorldBox_SouthSky"
                 })
        {
            Transform panel = Find(panelName);
            Assert.That(panel, Is.Not.Null, panelName);
            Assert.That(panel.GetComponent<Collider>(), Is.Null, panelName);
            Renderer renderer = panel.GetComponent<Renderer>();
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
                panelName);
            Assert.That(renderer.sharedMaterial.shader.name, Does.Contain("Unlit"), panelName);
        }
        Transform[] distantBlocks = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("DistantCityBlock_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(distantBlocks, Has.Length.EqualTo(10));
        Assert.That(distantBlocks.All(block => block.GetComponentsInChildren<Collider>(true).Length == 0), Is.True);
        Assert.That(distantBlocks.SelectMany(block => block.GetComponentsInChildren<Renderer>(true))
            .All(renderer => renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off), Is.True);
        Assert.That(Camera.main.backgroundColor, Is.EqualTo((Color)new Color32(112, 146, 158, 255)));

        Assert.That(Find("SyntyApartmentAcrossRoad_A"), Is.Not.Null);
        Assert.That(Find("SyntyApartmentAcrossRoad_B"), Is.Not.Null);
        Assert.That(Find("EmergencyFiretruck"), Is.Not.Null);
        Assert.That(Find("SyntyStreetLamp"), Is.Not.Null);
        Assert.That(Find("AssemblyWaitingBench"), Is.Not.Null);
        Assert.That(Find("AssemblyBoundaryTree"), Is.Not.Null);
        Assert.That(Find("AssemblyCanopyRoof_Left"), Is.Not.Null);
        Assert.That(Find("AssemblyAidStationLabel"), Is.Not.Null);
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyRearFence_", StringComparison.Ordinal)),
            Is.EqualTo(5));
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyCrowd_", StringComparison.Ordinal)),
            Is.EqualTo(3));

        Material townAtlas = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PolygonTown/Materials/PolygonTown_01_A.mat");
        Assert.That(townAtlas, Is.Not.Null);
        Renderer[] firetruckRenderers = Find("EmergencyFiretruck")
            .GetComponentsInChildren<Renderer>(true);
        Assert.That(firetruckRenderers, Is.Not.Empty);
        Assert.That(firetruckRenderers
            .Where(renderer => !renderer.name.StartsWith("EmergencyBeacon", StringComparison.Ordinal))
            .SelectMany(renderer => renderer.sharedMaterials)
            .All(material => material == townAtlas), Is.True);

        CinemachineCamera street = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(camera => camera.name == "CM04R_StreetJourney");
        Assert.That(street.Follow, Is.Null,
            "Street zone framing must stay authored and cannot jump back to Deniz's previous room.");
    }

    [Test]
    public void Cameras_FrameOpeningPairAndBothParentsAtReunion()
    {
        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        CinemachineCamera opening = cameras.Single(camera => camera.name == "CM04R_CorridorPair");
        Assert.That(opening.transform.position.z, Is.LessThan(-8f));
        Assert.That(Find("UpperCorridorBackWall"), Is.Null);

        CinemachineCamera reunion = cameras.Single(camera => camera.name == "CM04R_AssemblyReunion");
        Transform mother = Find("Anne_Assembly_Reunion");
        Transform father = Find("Baba_Assembly_Reunion");
        mother.gameObject.SetActive(true);
        father.gameObject.SetActive(true);
        Camera output = Camera.main;
        Vector3 oldPosition = output.transform.position;
        Quaternion oldRotation = output.transform.rotation;
        float oldFov = output.fieldOfView;
        try
        {
            output.transform.SetPositionAndRotation(reunion.transform.position, reunion.transform.rotation);
            output.fieldOfView = reunion.Lens.FieldOfView;
            AssertInPortraitFrame(output, mother);
            AssertInPortraitFrame(output, father);
        }
        finally
        {
            output.transform.SetPositionAndRotation(oldPosition, oldRotation);
            output.fieldOfView = oldFov;
            mother.gameObject.SetActive(false);
            father.gameObject.SetActive(false);
        }
    }

    [Test]
    public void AssemblyCareCamera_FramesChildrenNeighborAndWorker()
    {
        CinemachineCamera care = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(camera => camera.name == "CM04R_AssemblyCare");
        Transform deniz = Find("Deniz_12");
        Transform can = Find("Can_8");
        Transform neighbor = Find("Nermin_Neighbor_Assembly");
        Transform worker = Find("AssemblyWorker");
        Vector3 oldDenizPosition = deniz.position;
        Vector3 oldCanPosition = can.position;
        bool oldNeighborActive = neighbor.gameObject.activeSelf;
        Camera output = Camera.main;
        Vector3 oldPosition = output.transform.position;
        Quaternion oldRotation = output.transform.rotation;
        float oldFov = output.fieldOfView;
        try
        {
            deniz.position = new Vector3(0.35f, 0.02f, 40.25f);
            can.position = new Vector3(0.95f, 0.02f, 40.55f);
            neighbor.gameObject.SetActive(true);
            output.transform.SetPositionAndRotation(care.transform.position, care.transform.rotation);
            output.fieldOfView = care.Lens.FieldOfView;
            AssertInPortraitFrame(output, deniz);
            AssertInPortraitFrame(output, can);
            AssertInPortraitFrame(output, neighbor);
            AssertInPortraitFrame(output, worker);
        }
        finally
        {
            output.transform.SetPositionAndRotation(oldPosition, oldRotation);
            output.fieldOfView = oldFov;
            deniz.position = oldDenizPosition;
            can.position = oldCanPosition;
            neighbor.gameObject.SetActive(oldNeighborActive);
        }
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

    private static void AssertInPortraitFrame(Camera camera, Transform character)
    {
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        Vector3 viewport = camera.WorldToViewportPoint(bounds.center);
        Assert.That(viewport.z, Is.GreaterThan(0f), character.name);
        Assert.That(viewport.x, Is.InRange(0.05f, 0.95f), character.name);
        Assert.That(viewport.y, Is.InRange(0.05f, 0.95f), character.name);
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
