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
using UnityEngine.AI;
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
    public void Exterior_ReportsMobileRenderBudget()
    {
        Transform[] roots =
        {
            Find("Story04_CityWorldBox"),
            Find("RebuildStreetDressing"),
            Find("RebuildAssemblySet")
        };
        Renderer[] renderers = roots
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();
        long staticVertices = roots
            .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
            .Where(filter => filter.gameObject.activeInHierarchy && filter.sharedMesh != null && filter.GetComponent<Renderer>() != null && filter.GetComponent<Renderer>().enabled)
            .Sum(filter => (long)filter.sharedMesh.vertexCount);
        long skinnedVertices = roots
            .SelectMany(root => root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sharedMesh != null)
            .Sum(renderer => (long)renderer.sharedMesh.vertexCount);

        Debug.Log($"Story04 exterior budget: {renderers.Length} renderers, " +
                  $"{staticVertices + skinnedVertices} instanced vertices.");
        Assert.That(renderers.Length, Is.LessThanOrEqualTo(180));
        // Keep the authored Meshy evacuees intact. The former 110K ceiling was only met by
        // destructively clustering their UV/vertex islands, which visibly tore the characters.
        Assert.That(staticVertices + skinnedVertices, Is.LessThanOrEqualTo(210000));
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
        Assert.That(GetPrivate<float>(Object.FindFirstObjectByType<StoryPlayerMovement>(), "directSampleRadius"),
            Is.EqualTo(2.25f).Within(0.01f),
            "Katlar arası hedef çözümü alt sahanlığı bulmalı; geniş arama ara rampaya sapmamalı.");
        Assert.That(Object.FindObjectsByType<StoryEvacuationDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);
        StoryEvacuationDirector director = Object.FindFirstObjectByType<StoryEvacuationDirector>();
        Assert.That(director.RevisedFlow, Is.True);
        Assert.That(GetPrivate<float>(director, "revisedAftershockMinimumDuration"), Is.EqualTo(10f));
        Assert.That(Find("DistantCityAndAssemblyAmbience").GetComponent<AudioSource>().spatialBlend, Is.EqualTo(1f));
        Assert.That(Find("OutdoorAirLayer").GetComponent<AudioSource>().spatialBlend, Is.EqualTo(1f));

        Transform deniz = Find("Deniz_12");
        Transform rightHand = StoryChapterBuilderCommon.FindHumanoidBone(
            deniz.gameObject,
            HumanBodyBones.RightHand);
        Transform flashlightGrip = Find("DenizFlashlightGripSocket");
        Transform flashlightRig = Find("DenizFlashlightBeam");
        Transform flashlightVisual = Find("DenizHandFlashlight");
        Assert.That(rightHand, Is.Not.Null);
        Assert.That(flashlightGrip.parent, Is.EqualTo(rightHand));
        Assert.That(flashlightRig.parent, Is.EqualTo(flashlightGrip),
            "Fener ışığı karakter kökünde yüzmemeli; Deniz'in sağ el rig'ini izlemeli.");
        Assert.That(flashlightVisual.parent, Is.EqualTo(flashlightRig));
        Assert.That(flashlightVisual.GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
            "Açık ışığın elde görünen bir fener modeli olmalı.");
        Bounds flashlightBounds = WorldBoundsOf(flashlightVisual);
        Assert.That(Mathf.Max(flashlightBounds.size.x,
                Mathf.Max(flashlightBounds.size.y, flashlightBounds.size.z)),
            Is.InRange(0.18f, 0.26f), "Fener çocuk elinde okunabilir ama orantılı büyüklükte olmalı.");
        Assert.That(Vector3.Distance(flashlightRig.position, rightHand.position), Is.LessThan(0.14f));
        Assert.That(Vector3.Angle(flashlightRig.forward, deniz.forward), Is.LessThan(15f),
            "Spot ışık fenerin ileri ve hafif aşağı yönünden çıkmalı.");
    }

    [Test]
    public void Preview_UsesPhysicalNeighborHelpFacadeClearAndReunion()
    {
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(interactions, Has.Length.EqualTo(32));

        AssertGesture(interactions, "evac.r04.corridor.inspect", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.r04.route.elevator_unsafe", StoryInteractionGesture.Tap);
        AssertGesture(interactions, "evac.r04.route.stairs", StoryInteractionGesture.SwipeHorizontal);
        AssertGesture(interactions, "evac.r04.neighbor.cardboard", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.foam", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.cane", StoryInteractionGesture.DragToTarget);
        AssertGesture(interactions, "evac.r04.neighbor.support", StoryInteractionGesture.WorldHold);
        AssertGesture(interactions, "evac.r04.exit.facade_clear", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.r04.street.inspect", StoryInteractionGesture.Tap);
        AssertGesture(interactions, "evac.r04.street.safe", StoryInteractionGesture.Approach);
        AssertGesture(interactions, "evac.r04.street.gas_unsafe", StoryInteractionGesture.Tap);
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
        Assert.That(streetRead.gameObject, Is.EqualTo(Find("DamagedGasPipe_BrokenElbow").gameObject));
        Assert.That(streetRead.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.EvacuationStreetInspect));
        Assert.That(PersistentMethods(streetRead), Does.Contain("SetTrigger"));
        Assert.That(PersistentTargets(streetRead), Does.Contain(canAnimatorTargetName));
        Assert.That(Find("LooseFacadeSign").GetComponent<Animation>()?.clip, Is.Not.Null);
        Assert.That(Find("StreetWarningGlassShard_Slide").GetComponent<Animation>()?.clip, Is.Not.Null);
        Assert.That(Find("FacadeClearPavingPoint").GetComponent<Renderer>().enabled, Is.False,
            "Güvenli kaldırım hedefi görünür primitive disk olmamalı.");
        Transform supportBracelet = Find("NerminSupportBracelet_Hold");
        Transform braceletRing = supportBracelet.Find("BraceletRing");
        Assert.That(braceletRing, Is.Not.Null,
            "Destek bilekliği tek bir küre yerine halka mesh olarak okunmalı.");
        Assert.That(braceletRing.localPosition, Is.EqualTo(new Vector3(-1.21f, -0.99f, 0.64f)));
        Assert.That(braceletRing.localEulerAngles.z, Is.EqualTo(339.634f).Within(0.01f));
        Assert.That(braceletRing.localScale, Is.EqualTo(Vector3.one * 2.8915f));
        Transform neighbor = Find("Nermin_Neighbor_Landing");
        Vector3 neighborToChildren = new Vector3(0.1f, neighbor.position.y, 17.9f) - neighbor.position;
        neighborToChildren.y = 0f;
        Assert.That(Vector3.Angle(neighbor.forward, neighborToChildren), Is.LessThan(2f),
            "Nermin sahanlıkta çocuklara sırtını dönmemeli; konuşma yaklaşım noktasına bakmalı.");
        StoryInteractable neighborConversation = interactions.Single(candidate =>
            candidate.InteractionId == "evac.r04.neighbor.ask");
        Assert.That(neighborConversation.gameObject, Is.EqualTo(neighbor.gameObject),
            "Nermin dekor NPC olmamalı; konuşma doğrudan karakterin üzerinden başlamalı.");
        Assert.That(PersistentMethods(neighborConversation), Does.Contain("CallNeighbor"));
        Transform neighborRightHand = StoryChapterBuilderCommon.FindHumanoidBone(
            neighbor.gameObject,
            HumanBodyBones.RightHand);
        Assert.That(neighborRightHand, Is.Not.Null);
        Assert.That(supportBracelet.parent, Is.EqualTo(neighborRightHand),
            "Destek bilekliği karakter kökünde dünya koordinatında yüzmemeli; sağ bileği izlemeli.");
        Assert.That(Vector3.Distance(supportBracelet.position, neighborRightHand.position),
            Is.LessThan(0.12f), "Destek bilekliği bileğin dışına taşmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name == "BraceletRing"), Is.EqualTo(1));
        AudioSource gasHiss = Find("GasLeak_Hiss").GetComponent<AudioSource>();
        Assert.That(gasHiss.clip, Is.Not.Null);
        Assert.That(gasHiss.playOnAwake, Is.False);
        Assert.That(gasHiss.loop, Is.True);
        ParticleSystem gasVapor = Find("GasLeakVapor").GetComponent<ParticleSystem>();
        Assert.That(gasVapor.main.playOnAwake, Is.False);
        Assert.That(gasVapor.main.loop, Is.True);
        Assert.That(Find("DamagedGasPipe_Straight_A"), Is.Not.Null);
        Assert.That(Find("DamagedGasPipe_Straight_B"), Is.Not.Null);
        Assert.That(Find("DamagedGasPipe_Straight_C"), Is.Not.Null);
        Assert.That(Find("DamagedGasPipe_BrokenElbow"), Is.Not.Null);
        Assert.That(Find("GasServiceCylinder_Fallen"), Is.Not.Null);
        StoryEvacuationDirector director = Object.FindFirstObjectByType<StoryEvacuationDirector>();
        Assert.That(GetPrivate<Animation>(director, "streetInspectSignAnimation"), Is.Not.Null);
        Assert.That(GetPrivate<Animation>(director, "streetInspectShardAnimation"), Is.Not.Null);
        Assert.That(GetPrivate<AudioSource>(director, "streetInspectCreak"), Is.EqualTo(gasHiss));
        Assert.That(GetPrivate<ParticleSystem>(director, "streetDust"), Is.EqualTo(gasVapor));

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
        Assert.That(Find("BlanketPrepared_Drag").position.y, Is.GreaterThan(0.75f),
            "Hazırlanmış battaniye park girişinde yerde değil, yardım erzaklarının üstünde durmalı.");
        Assert.That(Find("WindbreakTent_Fallback").gameObject.activeSelf, Is.False);
        Assert.That(Find("ContactCardPrepared_Drag").gameObject.activeSelf, Is.True);
        Assert.That(Find("RegistryPencil_Fallback").gameObject.activeSelf, Is.False);
        Assert.That(Find("WorkerTreatmentTray_Target").GetComponentInChildren<MeshFilter>(true).sharedMesh.name,
            Is.Not.EqualTo("Cube"), "Tedavi tepsisi düz primitive blok olmamalı.");
        Assert.That(Find("NerminCup_Target").Find("CupHandle"), Is.Not.Null,
            "Su hedefi saplı gerçek kupa olarak okunmalı.");
        Assert.That(Find("WindbreakTent_Fallback").GetComponentInChildren<MeshFilter>(true).sharedMesh.name,
            Is.Not.EqualTo("Cube"), "Rüzgâr siperi düz primitive duvar olmamalı.");
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
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 60f), Is.True);

        Assert.That(Find("SafeRouteLabel"), Is.Null);
        Assert.That(Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .All(text => !string.Equals(
                text.text?.Trim(),
                "AÇIK ROTA",
                StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void StairwellAndLandingProps_AreFinishedArchitectureAtHumanScale()
    {
        Transform[] sconces = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("EmergencySconce_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(sconces, Has.Length.EqualTo(4));
        foreach (Transform sconce in sconces)
        {
            Assert.That(sconce.Find("WallPlate"), Is.Not.Null, sconce.name);
            Assert.That(sconce.Find("AmberLens"), Is.Not.Null, sconce.name);
            Assert.That(sconce.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True,
                sconce.name + " duvar üzerinde görünür ama rota ışınını engellememeli.");
        }

        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.EndsWith("_Nosing", StringComparison.Ordinal)), Is.EqualTo(26));
        Assert.That(Find("UpperFlightHandrail"), Is.Not.Null);
        Assert.That(Find("LowerFlightHandrail"), Is.Not.Null);
        Assert.That(Find("UpperLandingRightWall_Rebuild"), Is.Not.Null);
        Assert.That(Find("UpperLandingSafetyEdge"), Is.Not.Null);
        Assert.That(Find("UpperLandingFloor").GetComponent<Renderer>().sharedMaterial.name,
            Does.Contain("Concrete"));

        Bounds blockedCane = WorldBoundsOf(Find("NeighborCane_Blocked"));
        Bounds reachableCane = AuthoredBoundsOf(Find("NeighborCane_Reachable"));
        Assert.That(blockedCane.min.y, Is.InRange(0.015f, 0.04f));
        Assert.That(Mathf.Max(blockedCane.size.x, blockedCane.size.z), Is.InRange(0.9f, 1.02f),
            "Yerdeki baston da teslim edilen bastonla aynı gerçek insan ölçeğini korumalı.");
        Assert.That(blockedCane.size.y, Is.LessThanOrEqualTo(0.17f),
            "Yerdeki baston kazma gibi dik durmamalı.");
        Assert.That(reachableCane.size.y, Is.InRange(0.9f, 1.02f),
            "Teslim edilen baston insan boyuna uygun dikey oranda olmalı.");
        Assert.That(WorldBoundsOf(Find("NeighborCardboard_Blocking_Drag")).min.y,
            Is.InRange(0.015f, 0.04f));
        Bounds foam = WorldBoundsOf(Find("NeighborFoam_Blocking_Drag"));
        Assert.That(foam.min.y,
            Is.InRange(0.015f, 0.04f));
        Assert.That(foam.size.y, Is.LessThanOrEqualTo(0.22f),
            "Hafif köpük kalın bir zemin küpü gibi görünmemeli.");
        Assert.That(Find("NeighborFoam_Blocking_Drag").Find("FoamPad_Lower"), Is.Not.Null);
        Assert.That(Find("NeighborFoam_Blocking_Drag").Find("FoamPad_Upper"), Is.Not.Null);

        Transform neighborPoint = Find("R04_NeighborPoint");
        foreach (string propName in new[]
                 {
                     "NeighborCardboard_Blocking_Drag", "NeighborFoam_Blocking_Drag", "NeighborCane_Blocked"
                 })
        {
            Transform prop = Find(propName);
            Assert.That(prop.position.x, Is.GreaterThan(0.8f), propName + " sağ duvar şeridinde olmalı.");
            Assert.That(neighborPoint.position.x, Is.LessThan(prop.position.x - 1f),
                propName + " komşuya giden yürüyüş aksını kapatmamalı.");
            Assert.That(prop.GetComponentInChildren<NavMeshObstacle>(true),
                Is.Not.Null.And.Property("carving").True,
                propName + " karakter tarafından üzerinden geçilebilir olmamalı.");
            Transform hotspot = prop.Find("MobileTouchHotspot");
            Assert.That(hotspot, Is.Not.Null, propName + " mobil dokunma hotspot'u eksik.");
            Assert.That(hotspot.GetComponent<Collider>(), Is.Not.Null.And.Property("enabled").True,
                propName + " navigasyon blocker'ına çevrilirken dokunma collider'ını kaybetmemeli.");
        }

        NavMeshLink exitLink = Find("BuildingExitThresholdNavLink").GetComponent<NavMeshLink>();
        Assert.That(exitLink, Is.Not.Null);
        Assert.That(Mathf.Abs(exitLink.endPoint.z - exitLink.startPoint.z), Is.LessThanOrEqualTo(1.15f),
            "Kapı linki doğal tek eşik adımından uzun bir kayma olmamalı.");
        Assert.That(exitLink.width, Is.GreaterThanOrEqualTo(1.1f));

        Transform safePoint = Find("FacadeClearPavingPoint");
        Assert.That(safePoint.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("FacadeClearPaver_", StringComparison.Ordinal)),
            Is.EqualTo(3), "Güvenli cephe noktası debug diski yerine kaldırım taşı olarak okunmalı.");
    }

    [Test]
    public void ArchitectureShellRoofAndStreetDepth_AreCompleteAndCollisionSafe()
    {
        Transform shell = Find("Story04_ClosedArchitectureShell");
        Assert.That(shell, Is.Not.Null);
        foreach (string blockerName in new[]
                 {
                     "StairwellRightUpperWall_Rebuild",
                     "StairwellLeftUpperSeal_Rebuild",
                     "StairwellCeiling_Rebuild",
                     "LowerLandingCeiling_Rebuild",
                     "StairwellLowerBulkhead_Rebuild",
                     "ElevatorForwardWallSeal_Rebuild"
                 })
        {
            Transform blocker = Find(blockerName);
            Assert.That(blocker, Is.Not.Null, blockerName);
            Assert.That(blocker.GetComponent<Collider>(), Is.Not.Null.And.Property("enabled").True,
                blockerName + " gerçek mimari kabuğun parçası ve NavMesh blocker'ı olmalı.");
        }

        Bounds upperCeiling = WorldBoundsOf(Find("StairwellCeiling_Rebuild"));
        Assert.That(WorldBoundsOf(Find("StairwellRightUpperWall_Rebuild")).max.y,
            Is.GreaterThanOrEqualTo(upperCeiling.min.y - 0.01f),
            "Sağ merdiven duvarı ile tavan arasında skybox gösteren açıklık kalmamalı.");
        Assert.That(WorldBoundsOf(Find("StairwellLeftUpperSeal_Rebuild")).max.y,
            Is.GreaterThanOrEqualTo(upperCeiling.min.y - 0.01f),
            "Sol merdiven duvarı ile tavan arasında skybox gösteren açıklık kalmamalı.");
        foreach (string ceilingName in new[] { "StairwellCeiling_Rebuild", "LowerLandingCeiling_Rebuild" })
        {
            NavMeshModifier modifier = Find(ceilingName).GetComponent<NavMeshModifier>();
            Assert.That(modifier, Is.Not.Null.And.Property("ignoreFromBuild").True,
                ceilingName + " kamera kabuğunu kapatmalı ama üstünde sahte yürünebilir NavMesh katı üretmemeli.");
        }

        Transform interior = Find("Story04_InteriorArchitecturalFinish");
        Assert.That(interior, Is.Not.Null);
        Assert.That(interior.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Koridor dekoru etkileşim ışınını veya yürüme rotasını kapatmamalı.");
        foreach (string detailName in new[]
                 {
                     "UpperCorridorRunnerRug",
                     "CorridorApartmentDoor_31",
                     "CorridorApartmentDoor_32",
                     "UpperCorridorNoticeBoard",
                     "LowerLandingResidentBoard",
                     "LowerLandingLetterBoxes"
                 })
            Assert.That(Find(detailName), Is.Not.Null, detailName);
        Light[] corridorLights = interior.GetComponentsInChildren<Light>(true)
            .Where(light => light.name == "WarmCeilingLight")
            .ToArray();
        Assert.That(corridorLights, Has.Length.EqualTo(2));
        Assert.That(corridorLights.All(light => light.shadows == LightShadows.None), Is.True);
        Bounds fixtureHousing = WorldBoundsOf(Find("UpperCorridorCeilingFixture_A").Find("Housing"));
        Assert.That(fixtureHousing.size.y, Is.LessThanOrEqualTo(0.035f),
            "Tavan armatürü ağır bir asma tavan kirişi gibi görünmemeli.");
        Assert.That(Mathf.Max(fixtureHousing.size.x, fixtureHousing.size.z), Is.LessThanOrEqualTo(0.56f));

        Transform exterior = Find("Story04_PrimaryBuildingExteriorFinish");
        Assert.That(exterior, Is.Not.Null);
        Assert.That(exterior.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Cephe ve çatı süsleri oynanış collider'ı üretmemeli.");
        Bounds roof = WorldBoundsOf(Find("PrimaryBuildingRoofDeck"));
        Assert.That(roof.size.x, Is.GreaterThanOrEqualTo(5.5f));
        Assert.That(roof.size.z, Is.GreaterThanOrEqualTo(31.5f),
            "Ana apartmanın koridoru ve cephesi boyunca gerçek bir çatı bulunmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("PrimaryBuildingParapet_", StringComparison.Ordinal)),
            Is.EqualTo(4));
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("PrimaryBuildingParapetCap_", StringComparison.Ordinal)),
            Is.EqualTo(4));
        Assert.That(Find("PrimaryBuildingParapet_Front").GetComponent<Renderer>().sharedMaterial,
            Is.Not.SameAs(Find("PrimaryBuildingRoofDeck").GetComponent<Renderer>().sharedMaterial),
            "Parapet gövdesi tepede ağır siyah bir blok gibi okunmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("PrimaryFacadeWindowFrame_", StringComparison.Ordinal)),
            Is.EqualTo(6));
        foreach (string roofDetail in new[]
                 {
                     "PrimaryRoofWaterStore", "PrimaryRoofVent", "PrimaryRoofChimney",
                     "PrimaryRoofServiceCabin", "PrimaryRoofServiceCabinCap"
                 })
            Assert.That(Find(roofDetail), Is.Not.Null, roofDetail);

        Transform[] depthBuildings = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("NeighborhoodDepthBuilding_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(depthBuildings, Has.Length.EqualTo(6));
        Assert.That(depthBuildings.All(building => Mathf.Abs(building.position.x) > 12f), Is.True,
            "İkinci bina sırası oynanış rotasının dışında kalmalı.");
        Assert.That(depthBuildings.All(building =>
            building.GetComponentsInChildren<Collider>(true).Length == 0), Is.True);
        Assert.That(depthBuildings.SelectMany(building => building.GetComponentsInChildren<Renderer>(true))
            .All(renderer => renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off), Is.True);
        MeshRenderer[] enabledDepthRenderers = Find("Story04_NeighborhoodDepth")
            .GetComponentsInChildren<MeshRenderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();
        Assert.That(enabledDepthRenderers, Has.Length.EqualTo(1),
            "Uzak ikinci bina sırası mobilde tek statik batch olarak çizilmeli.");
        Assert.That(enabledDepthRenderers[0].name, Is.EqualTo("Story04_NeighborhoodDepthBatch"));
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
            Assert.That(panel, Is.Null, panelName + " must not enclose the city skyline.");
        }
        Assert.That(Camera.main.clearFlags, Is.EqualTo(CameraClearFlags.Skybox));
        Assert.That(RenderSettings.skybox, Is.Not.Null);
        Assert.That(Find("CityWorldBox_Ground").GetComponent<Renderer>().sharedMaterial.name,
            Does.Contain("Grass"));
        Transform[] distantBlocks = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("DistantCityBlock_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(distantBlocks, Has.Length.EqualTo(4));
        Assert.That(distantBlocks.All(block => block.GetComponentsInChildren<Collider>(true).Length == 0), Is.True);
        Assert.That(distantBlocks.SelectMany(block => block.GetComponentsInChildren<Renderer>(true))
            .All(renderer => renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off), Is.True);
        Assert.That(Camera.main.backgroundColor, Is.EqualTo((Color)new Color32(112, 146, 158, 255)));

        Assert.That(Find("StreetCornerBuilding_C"), Is.Not.Null);
        Assert.That(Find("StreetCornerBuilding_D"), Is.Not.Null);
        Assert.That(Find("EmergencyFiretruck"), Is.Not.Null);
        Assert.That(Find("SyntyStreetLamp"), Is.Null,
            "Kalın gri iç mekân lambası gibi okunan eski imported mesh sahnede kalmamalı.");
        Transform[] streetLamps = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name == "StoryStreetLamp")
            .ToArray();
        Assert.That(streetLamps, Has.Length.EqualTo(2));
        foreach (Transform streetLamp in streetLamps)
        {
            Assert.That(streetLamp.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.name == "StreetLampGlow"),
                Is.False, "Sokak lambaları havada duran primitive glow mesh'i üretmemeli.");
            Transform lightAnchor = streetLamp.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "StreetLampLight");
            Light streetLight = lightAnchor.GetComponent<Light>();
            Assert.That(streetLight, Is.Not.Null);
            Assert.That(streetLight.type, Is.EqualTo(LightType.Spot));
            Assert.That(lightAnchor.GetComponent<Renderer>(), Is.Null,
                "Işık kaynağı görünür küre veya küp olmamalı.");
            Bounds lampBounds = WorldBoundsOf(streetLamp);
            Assert.That(lampBounds.min.y, Is.InRange(0.015f, 0.035f),
                "Sokak lambası zemine oturmalı.");
            Assert.That(lampBounds.size.y, Is.InRange(2.75f, 3.1f),
                "Sokak lambası masa lambası değil, yaklaşık üç metrelik dış mekân direği olmalı.");
            Transform pole = streetLamp.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "StreetLampPole");
            Bounds poleBounds = WorldBoundsOf(pole);
            Assert.That(poleBounds.size.y, Is.GreaterThan(2.4f));
            Assert.That(Mathf.Max(poleBounds.size.x, poleBounds.size.z), Is.LessThan(0.16f),
                "Direk kalın bir dolap/kolon gibi görünmemeli.");
            Transform housing = streetLamp.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "StreetLampHeadHousing");
            Assert.That(housing.position.x, Is.LessThan(streetLamp.position.x - 0.45f),
                "Armatür kolu yol tarafına uzanmalı.");
            Assert.That(lightAnchor.position.y,
                Is.InRange(lampBounds.max.y - 0.4f, lampBounds.max.y + 0.02f),
                "Işık kaynağı sokak lambasının başına bağlı kalmalı.");
        }
        Assert.That(Find("AssemblyParkLawn"), Is.Not.Null);
        Assert.That(Find("AssemblyParkWalkway"), Is.Not.Null);
        Assert.That(Find("AssemblyParkLawn").GetComponent<Renderer>().sharedMaterial.name,
            Does.Contain("Story04_ParkGrass"));
        Bounds parkBounds = Find("AssemblyParkLawn").GetComponent<Renderer>().bounds;
        Renderer[] roadRenderers = Find("ParkCrosswalkVisual").GetComponentsInChildren<Renderer>(true);
        Bounds emergencyRoadBounds = roadRenderers[0].bounds;
        for (int index = 1; index < roadRenderers.Length; index++)
            emergencyRoadBounds.Encapsulate(roadRenderers[index].bounds);
        Assert.That(parkBounds.max.x, Is.LessThan(emergencyRoadBounds.min.x),
            "Toplanma parkı araç yoluyla üst üste binip z-fighting üretmemeli.");
        Assert.That(Find("EvacuationSidewalkVisual"), Is.Not.Null);
        foreach (string roadName in new[]
                 {
                     "MainStreetVisual_SouthA",
                     "MainStreetVisual_SouthB",
                     "MainStreetVisual_A",
                     "MainStreetVisual_B",
                     "ParkCrosswalkVisual"
                 })
        {
            Transform road = Find(roadName);
            Assert.That(road, Is.Not.Null, roadName);
            Assert.That(road.GetComponentsInChildren<Collider>(true), Is.Empty, roadName);
            Assert.That(road.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("Env_Road_Free"),
                roadName + " must use unmarked asphalt; authored markings provide the only crossing.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(road.eulerAngles.y, 90f)), Is.LessThan(0.1f),
                roadName + " must run in the same north-south direction as the evacuation vehicle, " +
                "not create a parallel asphalt column.");
        }
        Bounds southRoad = WorldBoundsOf(Find("MainStreetVisual_SouthA"));
        Bounds southMiddleRoad = WorldBoundsOf(Find("MainStreetVisual_SouthB"));
        Bounds continuousRoad = WorldBoundsOf(Find("MainStreetVisual_A"));
        Bounds middleRoad = WorldBoundsOf(Find("MainStreetVisual_B"));
        Bounds crossingRoad = WorldBoundsOf(Find("ParkCrosswalkVisual"));
        Assert.That(southMiddleRoad.center.x, Is.EqualTo(southRoad.center.x).Within(0.01f));
        Assert.That(middleRoad.center.x, Is.EqualTo(continuousRoad.center.x).Within(0.01f));
        Assert.That(crossingRoad.center.x, Is.EqualTo(continuousRoad.center.x).Within(0.01f));
        Assert.That(southMiddleRoad.min.z, Is.EqualTo(southRoad.max.z).Within(0.02f),
            "First two southern asphalt segments must meet edge-to-edge.");
        Assert.That(continuousRoad.min.z, Is.EqualTo(southMiddleRoad.max.z).Within(0.02f),
            "Southern asphalt and the response route must meet edge-to-edge.");
        Assert.That(middleRoad.min.z, Is.EqualTo(continuousRoad.max.z).Within(0.02f),
            "First and second asphalt segments must meet edge-to-edge.");
        Assert.That(crossingRoad.min.z, Is.EqualTo(middleRoad.max.z).Within(0.02f),
            "Second asphalt segment and park crossing must meet edge-to-edge.");
        continuousRoad.Encapsulate(southRoad);
        continuousRoad.Encapsulate(southMiddleRoad);
        continuousRoad.Encapsulate(middleRoad);
        continuousRoad.Encapsulate(crossingRoad);
        Assert.That(continuousRoad.size.z, Is.GreaterThanOrEqualTo(26.9f),
            "The city route must read as a real street leg rather than a short road patch.");
        Bounds firetruckBounds = WorldBoundsOf(Find("EmergencyFiretruck"));
        Assert.That(firetruckBounds.min.x, Is.GreaterThan(continuousRoad.min.x));
        Assert.That(firetruckBounds.max.x, Is.LessThan(continuousRoad.max.x));
        Assert.That(firetruckBounds.min.z, Is.GreaterThan(continuousRoad.min.z));
        Assert.That(firetruckBounds.max.z, Is.LessThan(continuousRoad.max.z));
        Assert.That(Find("EmergencyVehicleRoad").GetComponent<Renderer>().enabled, Is.False);
        Assert.That(Find("AssemblyDirectionSign"), Is.Null,
            "Toplanma alanı zaten görünürken büyük yön oku park girişinde kullanılmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("ParkCrosswalkStripe_", StringComparison.Ordinal)),
            Is.EqualTo(7), "Park girişinde tek ve okunabilir bir yaya geçidi bulunmalı.");
        Assert.That(Find("NorthBoulevardVisual_A"), Is.Null);
        Assert.That(Find("NorthBoulevardVisual_B"), Is.Null);
        Assert.That(Find("NorthBoulevardVisual_C"), Is.Null);
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(transform => transform.name.StartsWith("CityRoadCenterLine_", StringComparison.Ordinal)),
            Is.False);
        foreach (string benchName in new[]
                 {
                     "StreetRestBench",
                     "AssemblyPromenadeBenchWest",
                     "AssemblyWaitingBench"
                 })
        {
            Transform bench = Find(benchName);
            Assert.That(bench, Is.Not.Null, benchName);
            Bounds benchBounds = WorldBoundsOf(bench);
            Assert.That(Mathf.Max(benchBounds.size.x, benchBounds.size.z), Is.InRange(1.65f, 2.1f),
                benchName + " must read as a believable two-person park bench, not a toy or oversized prop.");
            Assert.That(benchBounds.size.y, Is.InRange(0.75f, 0.95f),
                benchName + " must remain at a believable seated human height.");
        }
        Assert.That(WorldBoundsOf(Find("StreetRestBench")).Intersects(
                WorldBoundsOf(Find("StreetRubbishBin"))), Is.False,
            "Street rubbish bin must not clip into its neighboring bench.");
        foreach (string assemblyBenchName in new[]
                 {
                     "AssemblyPromenadeBenchWest",
                     "AssemblyWaitingBench"
                 })
        foreach (string clearObjectName in new[]
                 {
                     "AssemblyRubbishBin",
                     "AssemblyWorkerTable",
                     "AssemblyCareTable"
                 })
            Assert.That(WorldBoundsOf(Find(assemblyBenchName)).Intersects(
                    WorldBoundsOf(Find(clearObjectName))), Is.False,
                assemblyBenchName + " must remain clear of " + clearObjectName + ".");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyParkWestFence_", StringComparison.Ordinal)),
            Is.EqualTo(6), "Batı çiti büyütülmüş parkın gerçek dış sınırında devam etmeli.");
        Assert.That(Find("AssemblyParkWestFence_0").position.x, Is.LessThan(-14f));
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("StreetGrassClump_", StringComparison.Ordinal)),
            Is.EqualTo(8), "Dış rota yalnız yeşil zeminden oluşmamalı; sokak kenarlarında gerçek çim meshleri olmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("StreetRelief_", StringComparison.Ordinal)),
            Is.Zero, "Yola veya zemine giren yapay relief levhaları dış rotada kullanılmamalı.");
        Transform[] streetGrass = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("StreetGrassClump_", StringComparison.Ordinal))
            .ToArray();
        Bounds[] visibleRoadBounds = new[]
            {
                "MainStreetVisual_SouthA",
                "MainStreetVisual_SouthB",
                "MainStreetVisual_A",
                "MainStreetVisual_B",
                "ParkCrosswalkVisual"
            }
            .Select(name => WorldBoundsOf(Find(name)))
            .ToArray();
        foreach (Transform grass in streetGrass)
        foreach (Bounds roadBounds in visibleRoadBounds)
        {
            Bounds grassBounds = WorldBoundsOf(grass);
            bool overlapsRoadHorizontally =
                grassBounds.min.x < roadBounds.max.x && grassBounds.max.x > roadBounds.min.x &&
                grassBounds.min.z < roadBounds.max.z && grassBounds.max.z > roadBounds.min.z;
            Assert.That(overlapsRoadHorizontally, Is.False, grass.name + " must remain outside asphalt.");
        }
        Assert.That(Find("AssemblyParkLawn").localScale.x * Find("AssemblyParkLawn").localScale.z,
            Is.GreaterThanOrEqualTo(325f), "Toplanma parkı yardım çadırının etrafında geniş bir açık alan bırakmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyParkTree_", StringComparison.Ordinal)),
            Is.EqualTo(6), "Park çevresinde saksı yerine gövdeli çevre ağaçları bulunmalı.");
        Transform[] grassClumps = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("AssemblyGrassClump_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(grassClumps, Has.Length.EqualTo(20),
            "Yeşil zemin gerçek çim değildir; parkta görünür Kenney çim tutamları bulunmalı.");
        Assert.That(grassClumps.All(clump => clump.GetComponentInChildren<MeshFilter>(true) != null &&
                                             clump.GetComponentInChildren<MeshFilter>(true).sharedMesh.name != "Cube"),
            Is.True, "Çim kümeleri primitive mesh olmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyRelief_", StringComparison.Ordinal)),
            Is.Zero, "Park zemini üzerinde levha gibi duran yapay relief parçaları bulunmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblySoilBed_", StringComparison.Ordinal)),
            Is.EqualTo(2), "Dış çevrede organik biçimli toprak bitki yatakları bulunmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyNaturePath_", StringComparison.Ordinal)),
            Is.EqualTo(4), "Düz beyaz platform yerine zemine oturan taş park yolu kullanılmalı.");
        Assert.That(Find("AssemblyParkWalkway").GetComponent<Renderer>().enabled, Is.False);
        Assert.That(Find("AssemblyCentralPromenade").GetComponent<Renderer>().enabled, Is.False);
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Story/Environment/ThirdParty/KenneyNature/LICENSE_KENNEY_NATURE_KIT.txt"),
            Is.Not.Null, "Kenney Nature CC0 lisansı projede tutulmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(transform => transform.name.StartsWith("AssemblyFlowerPot_", StringComparison.Ordinal)),
            Is.False, "Oyuncak görünümlü saksılar toplanma parkında kullanılmamalı.");
        Assert.That(Find("AssemblyCanopyRoof_Left"), Is.Not.Null);
        Assert.That(Find("AssemblyAidStationLabel"), Is.Not.Null);
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyRearFence_", StringComparison.Ordinal)),
            Is.EqualTo(7));
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyCrowd_", StringComparison.Ordinal)),
            Is.Zero, "Toplanma alanına Meshy ailesiyle uyumsuz Synty kalabalık atılmamalı.");

        Vector3 assembly = Find("AssemblyParkLawn").position;
        Transform[] nearbyHazards = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("DistantCityBlock_", StringComparison.Ordinal) ||
                                transform.name.StartsWith("StreetBackgroundBuilding_", StringComparison.Ordinal) ||
                                transform.name.StartsWith("StreetCornerBuilding_", StringComparison.Ordinal) ||
                                transform.name == "EmergencyFiretruck")
            .ToArray();
        Assert.That(nearbyHazards, Is.Not.Empty);
        foreach (Transform hazard in nearbyHazards)
        {
            Vector2 delta = new Vector2(hazard.position.x - assembly.x, hazard.position.z - assembly.z);
            Assert.That(delta.magnitude, Is.GreaterThanOrEqualTo(10f),
                hazard.name + " is too close to the assembly area's clear core.");

            Renderer[] renderers = hazard.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, hazard.name);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            float edgeX = Mathf.Max(Mathf.Abs(assembly.x - bounds.center.x) - bounds.extents.x, 0f);
            float edgeZ = Mathf.Max(Mathf.Abs(assembly.z - bounds.center.z) - bounds.extents.z, 0f);
            Assert.That(new Vector2(edgeX, edgeZ).magnitude, Is.GreaterThanOrEqualTo(6.5f),
                hazard.name + " renderer intrudes into the assembly park's 6.5 metre clear core.");
        }

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
            "Sokak ana planı sahne açılışındaki koridor konumuna bağlanıp çatının içine çekilmemeli.");
        Assert.That(street.GetComponent<CinemachinePositionComposer>(), Is.Null,
            "Stage-bağımsız sokak kadrajı authored sabit transformunu korumalı.");
        Assert.That(street.transform.position.z, Is.EqualTo(58f).Within(0.01f));
        Physics.SyncTransforms();
        Collider[] streetCameraOverlaps = Physics.OverlapSphere(
                street.transform.position,
                0.2f,
                ~0,
                QueryTriggerInteraction.Ignore)
            .Where(collider => collider != null && collider.GetComponentInParent<Animator>() == null)
            .ToArray();
        Assert.That(streetCameraOverlaps, Is.Empty,
            "Sokak kamerası çatı, tente veya cephe collider'ının içinde kalmamalı.");

        foreach (string buildingName in new[] { "StreetBackgroundBuilding_A", "StreetBackgroundBuilding_B" })
        {
            Bounds buildingBounds = WorldBoundsOf(Find(buildingName));
            Assert.That(buildingBounds.max.x, Is.LessThan(-3.7f),
                buildingName + " güvenli kaldırımın ve takip kamerasının önüne taşmamalı.");
        }
    }

    [Test]
    public void Cameras_FrameOpeningPairAndBothParentsAtReunion()
    {
        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        CinemachineCamera opening = cameras.Single(camera => camera.name == "CM04R_CorridorPair");
        Assert.That(opening.transform.position.x, Is.InRange(-2.3f, 2.3f),
            "The corridor camera must remain inside the corridor side walls.");
        Assert.That(opening.transform.position.z, Is.InRange(-10.65f, -0.25f),
            "The corridor camera must remain inside the enclosed corridor, not outside the back wall.");
        Transform corridorBackWall = Find("UpperCorridorBackWall");
        Assert.That(corridorBackWall, Is.Not.Null,
            "The corridor needs a real back wall so the exterior sky cannot leak into the shot.");
        Assert.That(corridorBackWall.position.z, Is.LessThan(opening.transform.position.z - 0.3f),
            "The authored camera must sit in front of the corridor's physical back wall.");
        Assert.That(Find("UpperCorridorCeiling"), Is.Not.Null);
        CinemachineCamera neighborCamera = cameras.Single(camera => camera.name == "CM04R_NerminHelp");
        Assert.That(neighborCamera.transform.position.x, Is.InRange(-2.25f, 2.25f));
        Assert.That(neighborCamera.transform.position.y, Is.LessThan(2.2f),
            "Komşu kamerası sahanlık parapetinin dışından/üstünden bakmamalı.");
        Assert.That(neighborCamera.transform.position.z, Is.InRange(11f, 11.5f),
            "Komşu kamerası Nermin ile sağ duvar hedeflerini aynı portre güvenli alanına sığdıracak kadar geride olmalı.");
        Assert.That(neighborCamera.Lens.FieldOfView, Is.EqualTo(60f).Within(0.01f),
            "20:9 portrede Nermin'in destek eliyle duvar şeridindeki drag hedefleri aynı güvenli kadrajda kalmalı.");
        CinemachineCamera exitCamera = cameras.Single(camera => camera.name == "CM04R_ExitDoor");
        Assert.That(exitCamera.transform.position.x, Is.InRange(-1.2f, 1.2f),
            "Çıkış kamerası yan duvara yaslanıp kapı açılırken kadrajı kapatmamalı.");
        Assert.That(exitCamera.transform.position.y, Is.InRange(2f, 2.7f));
        Assert.That(exitCamera.transform.position.z, Is.InRange(16.5f, 17.5f));
        Assert.That(Physics.OverlapSphere(exitCamera.transform.position, 0.18f)
                .All(collider => collider.isTrigger), Is.True,
            "Sabit çıkış kamerası fiziksel bir duvar/collider hacminin içinde olmamalı.");
        Transform stairDoor = Find("StairDoor_Closed");
        Assert.That(Find("StairDoorOpaqueBacking"), Is.Null,
            "A full-door backing protrudes beyond the imported door mesh.");
        Transform upperGlassBacking = Find("StairDoorGlassBacking_Upper");
        Transform lowerGlassBacking = Find("StairDoorGlassBacking_Lower");
        Assert.That(upperGlassBacking.parent, Is.EqualTo(stairDoor));
        Assert.That(lowerGlassBacking.parent, Is.EqualTo(stairDoor));

        Bounds doorLeafBounds = WorldBoundsOf(stairDoor.Find("Door"));
        foreach (Transform glassBacking in new[] { upperGlassBacking, lowerGlassBacking })
        {
            Bounds backingBounds = WorldBoundsOf(glassBacking);
            Assert.That(backingBounds.size.x, Is.LessThan(doorLeafBounds.size.x * 0.65f));
            Assert.That(backingBounds.min.x, Is.GreaterThan(doorLeafBounds.min.x));
            Assert.That(backingBounds.max.x, Is.LessThan(doorLeafBounds.max.x));
            Assert.That(backingBounds.min.y, Is.GreaterThan(doorLeafBounds.min.y));
            Assert.That(backingBounds.max.y, Is.LessThan(doorLeafBounds.max.y));
        }

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
            output.transform.SetPositionAndRotation(opening.transform.position, opening.transform.rotation);
            output.fieldOfView = opening.Lens.FieldOfView;
            AssertInPortraitFrame(output, Find("ElevatorFrame"));
            AssertInPortraitFrame(output, Find("StairDoor_Closed"));

            output.transform.SetPositionAndRotation(exitCamera.transform.position, exitCamera.transform.rotation);
            output.fieldOfView = exitCamera.Lens.FieldOfView;
            AssertInPortraitFrame(output, Find("BuildingExitDoor_Closed"));

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
    public void Corridor_IsClosedReadableAndUsesARealFlashlightGrip()
    {
        Assert.That(Find("ElevatorWallUpperSeal"), Is.Not.Null,
            "The side-wall opening above the elevator must not reveal the exterior sky.");
        Assert.That(Find("StairChoiceHeaderText"), Is.Null,
            "A world-space stair label must not leak through the doorway lintel.");
        Assert.That(Find("ElevatorChoiceHeaderText"), Is.Null,
            "A world-space elevator label must not leak through the corridor wall.");

        Transform grip = Find("DenizFlashlightGripSocket");
        Transform beam = Find("DenizFlashlightBeam");
        Transform visual = Find("DenizHandFlashlight");
        Assert.That(grip.parent.name, Is.EqualTo("RightHand"));
        Assert.That(beam.parent, Is.EqualTo(grip));
        Assert.That(beam.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(Quaternion.Angle(beam.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(visual.GetComponent<Renderer>().bounds.center, grip.position),
            Is.LessThan(0.08f), "The flashlight handle must cross the palm instead of floating behind the hand.");
    }

    [Test]
    public void Children_HaveHeadBoundFaceRigsWithoutRuntimeScriptsOrColliders()
    {
        foreach (string rigName in new[] { "Deniz_12_FaceRig", "Can_8_FaceRig" })
        {
            Transform rig = Find(rigName);
            Assert.That(rig, Is.Not.Null, rigName);
            Assert.That(rig.IsChildOf(Find(rigName.StartsWith("Deniz", StringComparison.Ordinal)
                ? "Deniz_12"
                : "Can_8")), Is.True, rigName);
            Assert.That(rig.GetComponentsInChildren<Collider>(true), Is.Empty,
                rigName + " must remain a visual-only authored rig.");
            Animation faceAnimation = rig.GetComponent<Animation>();
            Assert.That(faceAnimation, Is.Not.Null, rigName);
            Assert.That(faceAnimation.clip, Is.Not.Null, rigName);
            Assert.That(faceAnimation.playAutomatically, Is.True, rigName);
            foreach (string feature in new[] { "Brow_L", "Brow_R", "Eyelid_L", "Eyelid_R", "Mouth_Center" })
            {
                Assert.That(rig.Find(feature), Is.Not.Null, rigName + "/" + feature);
                Assert.That(rig.Find(feature).lossyScale.magnitude, Is.LessThan(0.12f),
                    rigName + "/" + feature + " must remain a subtle facial detail, not a face-sized bar.");
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(faceAnimation.clip)
                         .Where(binding => binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal)))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(faceAnimation.clip, binding);
                Assert.That(curve.keys.Max(key => Mathf.Abs(key.value)), Is.LessThan(0.12f),
                    binding.path + "/" + binding.propertyName + " must not restore an omitted scale axis to one.");
            }
        }
    }

    [Test]
    public void DoorOpenings_HugTheImportedFramesWithoutOversizedGaps()
    {
        AssertDoorReveal("StairDoor_Closed", "StairDoorWall_Left", "StairDoorWall_Right",
            "StairDoorWall_Lintel");
        AssertDoorReveal("BuildingExitDoor_Closed", "BuildingFacade_Left", "BuildingFacade_Right",
            "BuildingFacade_Lintel");

        Bounds exitFrame = Find("BuildingExitDoor_Closed").Find("DoorFrame")
            .GetComponent<Renderer>().bounds;
        Bounds leftBand = Find("FacadeBaseBand_Left").GetComponent<Renderer>().bounds;
        Bounds rightBand = Find("FacadeBaseBand_Right").GetComponent<Renderer>().bounds;
        Assert.That(leftBand.max.x - exitFrame.min.x, Is.InRange(0.015f, 0.04f));
        Assert.That(exitFrame.max.x - rightBand.min.x, Is.InRange(0.015f, 0.04f));
    }

    [Test]
    public void BuildingThreshold_FloorsMeetAtOneEdgeWithoutStackedSlabs()
    {
        Bounds landing = Find("LowerLandingFloor").GetComponent<Renderer>().bounds;
        Bounds outdoors = Find("OutdoorGround").GetComponent<Renderer>().bounds;
        Bounds sidewalk = Find("EvacuationSidewalkVisual").GetComponent<Renderer>().bounds;

        Assert.That(Mathf.Abs(landing.max.z - outdoors.min.z), Is.LessThan(0.002f),
            "İç sahanlık ile dış zemin üst üste binmeden kapı eşiğinde birleşmeli.");
        Assert.That(Mathf.Abs(sidewalk.min.z - outdoors.min.z), Is.LessThan(0.002f),
            "Tahliye kaldırımı bina içine uzanmamalı.");
        Assert.That(Mathf.Abs(sidewalk.min.y - outdoors.max.y), Is.LessThan(0.002f),
            "Kaldırım dış zeminin içine gömülmemeli.");
        Assert.That(Find("NeighborhoodEntrancePaving"), Is.Null,
            "Aynı eşikte ikinci bir zemin levhası üretilmemeli.");
    }

    [Test]
    public void Exterior_HasBatchedEarthquakeResponse()
    {
        Transform[] ambientMeshes = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("Story04_Ambient", StringComparison.Ordinal) &&
                                transform.name.EndsWith("_LowDetailMesh", StringComparison.Ordinal))
            .ToArray();
        Assert.That(ambientMeshes, Is.Empty,
            "The clustered ambient proxy system must not be used for character meshes.");
        foreach (Transform ambient in ambientMeshes)
        {
            MeshFilter meshFilter = ambient.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = ambient.GetComponent<MeshRenderer>();
            Assert.That(meshFilter, Is.Not.Null, ambient.name);
            Assert.That(meshRenderer, Is.Not.Null.And.Property("enabled").True, ambient.name);
            Assert.That(meshFilter.sharedMesh.vertexCount, Is.LessThan(7000),
                ambient.name + " mobil sahne iÃ§in dÃ¼ÅŸÃ¼k detaylÄ± olmalÄ±.");
            Assert.That(meshRenderer.bounds.min.y, Is.InRange(0f, 0.055f),
                ambient.name + " Ã§imde havada kalmamalÄ± veya zemine gÃ¶mÃ¼lmemeli.");
        }

        Transform aftermath = Find("StreetQuakeAftermath");
        Assert.That(aftermath, Is.Not.Null);
        foreach (string evidence in new[]
                 {
                     "FacadeEdgeDebris",
                     "StreetQuakeMasonry_Facade",
                     "StreetQuakeMasonry_Shops",
                     "StreetQuakeMasonry_EastCurb",
                     "StreetQuakeShopWindowGlass",
                     "FacadeWindowCrack_Main",
                     "EmergencyCordonTape"
                 })
            Assert.That(Find(evidence), Is.Not.Null, evidence);
        Assert.That(aftermath.GetComponentsInChildren<MeshRenderer>(true)
                .Count(renderer => renderer.enabled),
            Is.EqualTo(1), "Deprem izleri tek mobil mesh batch'inde Ã§izilmeli.");
        Assert.That(Find("Story04_QuakeAftermathBatch").GetComponent<MeshRenderer>().enabled, Is.True);
        Assert.That(aftermath.GetComponentsInChildren<Collider>(true)
                .All(collider => !collider.enabled),
            Is.True, "GÃ¶rsel hasar gÃ¼venli tahliye rotasÄ±nda gizli engel Ã¼retmemeli.");

        Assert.That(Find("Story04_RoadSurfaceBatch").GetComponent<MeshRenderer>().enabled, Is.True);
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("MainStreetLaneDash_", StringComparison.Ordinal)),
            Is.EqualTo(6), "Uzatılmış yol boyunca altı okunabilir şerit parçası bulunmalı.");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(transform => transform.name.StartsWith("AssemblyEntryConnectorPath_", StringComparison.Ordinal)),
            Is.EqualTo(4), "Yaya geÃ§idi taÅŸ park yoluna fiziksel olarak baÄŸlanmalÄ±.");
    }

    [Test]
    public void StreetJourneyCamera_FramesEmergencyResponseAndLivingAssemblyArea()
    {
        CinemachineCamera street = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(camera => camera.name == "CM04R_StreetJourney");
        Camera output = Camera.main;
        Vector3 oldPosition = output.transform.position;
        Quaternion oldRotation = output.transform.rotation;
        float oldFov = output.fieldOfView;
        try
        {
            output.transform.SetPositionAndRotation(street.transform.position, street.transform.rotation);
            output.fieldOfView = street.Lens.FieldOfView;
            AssertInPortraitFrame(output, Find("EmergencyFiretruck"));
            AssertInPortraitFrame(output, Find("AssemblyWorker"));
        }
        finally
        {
            output.transform.SetPositionAndRotation(oldPosition, oldRotation);
            output.fieldOfView = oldFov;
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
            deniz.position = new Vector3(-2.55f, 0.02f, 41.8f);
            can.position = new Vector3(-2.05f, 0.02f, 42.05f);
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
    public void NearMissCamera_FramesGasLeakWithoutFacadeTreeBlockingTheCenter()
    {
        CinemachineCamera nearMiss = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(camera => camera.name == "CM04R_NearMiss");
        Transform gasLeak = Find("DamagedGasPipe_BrokenElbow");
        Transform facadeTree = Find("TownTree_Facade");
        Camera output = Camera.main;
        Vector3 oldPosition = output.transform.position;
        Quaternion oldRotation = output.transform.rotation;
        float oldFov = output.fieldOfView;
        try
        {
            output.transform.SetPositionAndRotation(nearMiss.transform.position, nearMiss.transform.rotation);
            output.fieldOfView = nearMiss.Lens.FieldOfView;
            AssertInPortraitFrame(output, gasLeak);

            Renderer treeRenderer = facadeTree.GetComponentInChildren<Renderer>(true);
            Vector3 treeViewport = output.WorldToViewportPoint(treeRenderer.bounds.center);
            bool treeOutsideCenter =
                treeViewport.z <= 0f ||
                treeViewport.x < 0.15f ||
                treeViewport.x > 0.85f ||
                treeViewport.y < 0.15f ||
                treeViewport.y > 0.85f;
            Assert.That(treeOutsideCenter, Is.True,
                "Yakın ıskalama kamerasının ortasını cephe ağacı kapatmamalı.");
        }
        finally
        {
            output.transform.SetPositionAndRotation(oldPosition, oldRotation);
            output.fieldOfView = oldFov;
        }
    }

    [Test]
    public void StreetInspectionCamera_FramesThePhysicalGasSourceBeforeItsSoundStarts()
    {
        CinemachineCamera inspection = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(camera => camera.name == "CM04R_StreetHazardRead");
        Transform brokenPipe = Find("DamagedGasPipe_BrokenElbow");
        Transform fallenCylinder = Find("GasServiceCylinder_Fallen");
        Camera output = Camera.main;
        Vector3 oldPosition = output.transform.position;
        Quaternion oldRotation = output.transform.rotation;
        float oldFov = output.fieldOfView;
        try
        {
            output.transform.SetPositionAndRotation(inspection.transform.position, inspection.transform.rotation);
            output.fieldOfView = inspection.Lens.FieldOfView;
            AssertInPortraitFrame(output, brokenPipe);
            AssertInPortraitFrame(output, fallenCylinder);
        }
        finally
        {
            output.transform.SetPositionAndRotation(oldPosition, oldRotation);
            output.fieldOfView = oldFov;
        }
    }

    [Test]
    public void Preview_UsesOneSoftShadowSun()
    {
        Assert.That(Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(light => light.shadows == LightShadows.Soft), Is.EqualTo(1));
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

    private static void AssertDoorReveal(
        string doorName,
        string leftWallName,
        string rightWallName,
        string lintelName)
    {
        Transform door = Find(doorName);
        Assert.That(door, Is.Not.Null, doorName);
        Transform frame = door.Find("DoorFrame");
        Assert.That(frame, Is.Not.Null, doorName + "/DoorFrame");

        Bounds frameBounds = frame.GetComponent<Renderer>().bounds;
        Bounds leftBounds = Find(leftWallName).GetComponent<Renderer>().bounds;
        Bounds rightBounds = Find(rightWallName).GetComponent<Renderer>().bounds;
        Bounds lintelBounds = Find(lintelName).GetComponent<Renderer>().bounds;

        Assert.That(leftBounds.max.x - frameBounds.min.x, Is.InRange(0.015f, 0.04f), leftWallName);
        Assert.That(frameBounds.max.x - rightBounds.min.x, Is.InRange(0.015f, 0.04f), rightWallName);
        Assert.That(frameBounds.max.y - lintelBounds.min.y, Is.InRange(0.015f, 0.04f), lintelName);
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
        Bounds bounds = WorldBoundsOf(character);
        Vector3 viewport = camera.WorldToViewportPoint(bounds.center);
        Assert.That(viewport.z, Is.GreaterThan(0f), character.name);
        Assert.That(viewport.x, Is.InRange(0.05f, 0.95f), character.name);
        Assert.That(viewport.y, Is.InRange(0.05f, 0.95f), character.name);
    }

    private static Bounds WorldBoundsOf(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static Bounds AuthoredBoundsOf(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
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
