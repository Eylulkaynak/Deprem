using System.Linq;
using System.Reflection;
using Deprem.Story;
using NUnit.Framework;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
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
        Assert.That(GetPrivate<float>(director, "quakeMinimumDuration"), Is.EqualTo(22f));
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
        AssertGesture(interactions, "quake.calm.can", StoryInteractionGesture.WorldHold);
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
        Transform denizCoverAnchor = Find("Deniz_CoverAnchor");
        Transform canCoverAnchor = Find("Can_CoverAnchor");
        Vector3 coverSeparation = denizCoverAnchor.position - canCoverAnchor.position;
        coverSeparation.y = 0f;
        Assert.That(coverSeparation.magnitude, Is.GreaterThanOrEqualTo(1.1f),
            "Deniz ve Can koruma animasyonunda aynÄ± kÃ¶k noktasÄ±na yerleÅŸmemeli.");
        StoryInteractable crouch = interactions.Single(item => item.InteractionId == "quake.cover.crouch");
        Assert.That(crouch.InteractionPoint, Is.EqualTo(denizCoverAnchor));
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
        string cameraFovs = string.Join(", ", cameras
            .OrderBy(camera => camera.name)
            .Select(camera => $"{camera.name}={camera.Lens.FieldOfView:F3}"));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 37.99f && camera.Lens.FieldOfView <= 60.01f),
            Is.True,
            cameraFovs);
        CinemachineCamera overview = cameras.Single(camera => camera.name == "CM03R_RoomOverview");
        Assert.That(overview.Lens.FieldOfView, Is.EqualTo(56f).Within(0.01f));
        CinemachineCamera corridorCamera = cameras.Single(camera => camera.name == "CM03R_CorridorLong");
        Assert.That(corridorCamera.Lens.FieldOfView, Is.LessThanOrEqualTo(54f));

        CinemachineCamera cover = cameras.Single(camera => camera.name == "CM03R_UnderTableTwoShot");
        Assert.That(cover.transform.position.y, Is.InRange(0.62f, 0.95f));
        Assert.That(cover.transform.position.z, Is.LessThan(-4f));
        Transform denizAnchor = Find("Deniz_CoverAnchor");
        Transform canAnchor = Find("Can_CoverAnchor");
        Assert.That(Vector3.Distance(denizAnchor.position, canAnchor.position), Is.GreaterThanOrEqualTo(1.1f));
        Vector3 anchorMidpoint = Vector3.Lerp(denizAnchor.position, canAnchor.position, 0.5f) + Vector3.up * 0.42f;
        Assert.That(Vector3.Angle(
            cover.transform.forward,
            (anchorMidpoint - cover.transform.position).normalized), Is.LessThan(1.5f));
        Transform doorwayHeader = Find("SharedHomeDoorwayUpperWall");
        Assert.That(doorwayHeader, Is.Not.Null,
            "Masa-altı kamerası koridor kapısının üstünden skybox görmemeli.");
        Bounds doorwayHeaderBounds = GetBounds(doorwayHeader);
        Assert.That(doorwayHeaderBounds.min.y, Is.LessThanOrEqualTo(2.81f));
        Assert.That(doorwayHeaderBounds.max.y, Is.GreaterThanOrEqualTo(7.99f));
        Assert.That(doorwayHeader.GetComponent<Collider>(), Is.Not.Null.And.Property("enabled").True);
        Assert.That(doorwayHeader.GetComponent<NavMeshModifier>(),
            Is.Not.Null.And.Property("ignoreFromBuild").True,
            "Kapı üst duvarı görsel/fiziksel kabuğu kapatmalı ama tavanda yürünebilir ada üretmemeli.");

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
    public void Preview_ReusesStory01LivedInHomeAndAddsVisibleRoomScaleDestruction()
    {
        Transform art = Find("PreparationInteriorArtPass");
        Assert.That(art, Is.Not.Null,
            "Story 03 aynı prefabın çıplak kabuğunu değil Story 01'in bitmiş ev art katmanını kullanmalı.");
        foreach (string required in new[]
                 {
                     "Story01_CeilingMain",
                     "Story01_FamilyPlanFeatureWall",
                     "Story01_KitchenZone",
                     "Story01_LoungeZone",
                     "Story01_EntryZone",
                     "Story01_WallGallery",
                     "Story01_PracticalLighting",
                     "CanonicalHomeContinuityFurniture"
                 })
            Assert.That(Find(required), Is.Not.Null, required);
        Assert.That(art.GetComponentsInChildren<Renderer>(true), Has.Length.GreaterThanOrEqualTo(40));
        Assert.That(art.GetComponentsInChildren<StoryInteractable>(true), Is.Empty,
            "Ortak ev dekoru Story 03 oynanış hedeflerini çalmamalı.");

        foreach (string dynamicDamage in new[]
                 {
                     "Quake_FallingFamilyPhoto",
                     "Quake_LooseShelfBook_A",
                     "Quake_LooseShelfBook_B",
                     "Quake_FallingTableMug",
                     "Quake_TopplingLivingPlant",
                     "Quake_WindowCrackReveal"
                 })
            Assert.That(Find(dynamicDamage), Is.Not.Null, dynamicDamage);
        Assert.That(Find("Story03_PostQuakeRoomDamage"), Is.Not.Null,
            "Timeline durduğunda yıkım izi kaybolmamalı; post-quake hasarı kalıcı olmalı.");

        PlayableDirector director = Object.FindObjectsByType<PlayableDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(candidate => candidate.name == "EarthquakeTimeline_Rebuild_48s");
        TimelineAsset timeline = (TimelineAsset)director.playableAsset;
        string[] trackNames = timeline.GetOutputTracks().Select(track => track.name).ToArray();
        Assert.That(trackNames, Does.Contain("00 • Tüm oda ve koridor yapı titreşimi"));
        Assert.That(trackNames, Does.Contain("02 • Aile fotoğrafı düşüşü"));
        Assert.That(trackNames, Does.Contain("03 • Kupa düşüşü ve kırılması"));
        Assert.That(trackNames, Does.Contain("03 • Pencere çatlağı görünümü"));
    }

    [Test]
    public void Children_ShowFearAndEmergencyBagIsPhysicallyMountedToDenizBack()
    {
        Transform deniz = Find("Deniz_12");
        Transform wornBag = Find("Deniz_WornEmergencyBag");
        Animator animator = deniz.GetComponentInChildren<Animator>(true);
        Transform torso = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                          animator.GetBoneTransform(HumanBodyBones.Chest) ??
                          animator.GetBoneTransform(HumanBodyBones.Spine);
        Assert.That(new[] { deniz, wornBag, torso }, Has.None.Null);
        Assert.That(wornBag.IsChildOf(torso), Is.True,
            "Story 03 çantası karakter kökünde yüzmemeli; gövde kemiğini izlemeli.");
        Vector3 torsoToBag = GetBounds(wornBag).center - torso.position;
        Assert.That(Vector3.Dot(torsoToBag, deniz.forward), Is.LessThan(-0.02f));
        Assert.That(Mathf.Abs(Vector3.Dot(torsoToBag, deniz.up)), Is.LessThan(0.34f));

        Transform denizFear = Find("Deniz_12_QuakeFearFace");
        Transform canFear = Find("Can_8_QuakeFearFace");
        Assert.That(new[] { denizFear, canFear }, Has.None.Null);
        Assert.That(denizFear.IsChildOf(animator.GetBoneTransform(HumanBodyBones.Head)), Is.True);
        Assert.That(canFear.IsChildOf(
            Find("Can_8").GetComponentInChildren<Animator>(true).GetBoneTransform(HumanBodyBones.Head)), Is.True);
        Assert.That(denizFear.gameObject.activeSelf, Is.False);
        Assert.That(canFear.gameObject.activeSelf, Is.False);

        PlayableDirector director = Object.FindObjectsByType<PlayableDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(candidate => candidate.name == "EarthquakeTimeline_Rebuild_48s");
        string[] activationTracks = ((TimelineAsset)director.playableAsset).GetOutputTracks()
            .OfType<ActivationTrack>()
            .Select(track => track.name)
            .ToArray();
        Assert.That(activationTracks, Does.Contain("00 • Deniz korku ifadesi"));
        Assert.That(activationTracks, Does.Contain("00 • Can korku ifadesi"));
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
    public void OpeningAndPostQuakeProps_AreStagedOnFurnitureInsteadOfScatteredAcrossFloor()
    {
        Bounds tableBounds = GetBounds(Find("SafeTable_Visual"));
        AssertSupportedBy(Find("Can_ToyCar"), tableBounds, "Oyuncak araba masa üzerinde başlamalı.");
        AssertSupportedBy(Find("Can_ToyWheel_Drag"), tableBounds, "Oyuncak teker masa üzerinde başlamalı.");
        AssertSupportedBy(Find("Can_FamilyPlanDrawing"), tableBounds, "Aile planı çizimi masa üzerinde başlamalı.");
        foreach (string prop in new[] { "Can_ToyCar", "Can_ToyWheel_Drag", "Can_FamilyPlanDrawing" })
            Assert.That(GetBounds(Find(prop)).min.y - tableBounds.max.y, Is.InRange(-0.003f, 0.02f),
                prop + " görünür tabladan en fazla 2 cm yukarıda olmalı; gizli dekorun bounds'u yüzey değildir.");
        BoxCollider tabletop = Find("SafeTable").Find("Top").GetComponent<BoxCollider>();
        Assert.That(tabletop.bounds.max.y, Is.EqualTo(tableBounds.max.y).Within(0.005f),
            "Masanın fiziksel üst yüzeyi çizim ve oyuncaktan önce görünmez bir engel oluşturmamalı.");

        Assert.That(Find("FamilyBoardGame").gameObject.activeSelf, Is.False,
            "Dev oyun kumandası Story03 açılış masasını kalabalıklaştırmamalı.");
        Bounds carBounds = GetBounds(Find("Can_ToyCar"));
        Bounds wheelBounds = GetBounds(Find("Can_ToyWheel_Drag"));
        Bounds planBounds = GetBounds(Find("Can_FamilyPlanDrawing"));
        Assert.That(carBounds.size.x, Is.LessThanOrEqualTo(0.35f), "Oyuncak araba çocuk ölçeğinde olmalı.");
        Assert.That(carBounds.size.z, Is.LessThanOrEqualTo(0.28f), "Oyuncak araba masa üzerini kaplamamalı.");
        Assert.That(wheelBounds.size.magnitude, Is.LessThanOrEqualTo(0.19f), "Teker arabayla aynı ölçekte olmalı.");
        Assert.That(planBounds.size.x, Is.LessThanOrEqualTo(0.51f), "Plan kâğıdı masa ölçeğinde olmalı.");
        Assert.That(planBounds.size.z, Is.LessThanOrEqualTo(0.39f), "Plan kâğıdı masa ölçeğinde olmalı.");
        Assert.That(Find("FamilyPlan_Home"), Is.Not.Null);
        Assert.That(Find("FamilyPlan_Route"), Is.Not.Null);
        Assert.That(Find("FamilyPlan_MeetingPoint"), Is.Not.Null);

        Bounds shoeBenchBounds = GetBounds(Find("EntryShoeBench_Continuity"));
        AssertSupportedBy(Find("DenizShoePair_Drag"), shoeBenchBounds, "Deniz'in ayakkabıları giriş rafında olmalı.");
        AssertSupportedBy(Find("CanShoes_World"), shoeBenchBounds, "Can'ın ayakkabıları giriş rafında olmalı.");
        Assert.That(shoeBenchBounds.size.x, Is.LessThanOrEqualTo(0.65f),
            "Ayakkabılık sağ duvarda sığ kalmalı; çıkış aksına doğru uzamamalı.");
        Assert.That(shoeBenchBounds.size.z, Is.InRange(1.35f, 1.7f),
            "Ayakkabılık iki çocuk ayakkabısını taşıyan gerçek bir giriş bankı ölçüsünde olmalı.");
        Assert.That(shoeBenchBounds.center.x, Is.GreaterThan(4.35f),
            "Ayakkabılık odanın ortasında değil sağ duvar şeridinde olmalı.");
        NavMeshObstacle shoeBenchObstacle = Find("EntryShoeBench_Continuity")
            .GetComponentInChildren<NavMeshObstacle>(true);
        Assert.That(shoeBenchObstacle, Is.Not.Null.And.Property("carving").True,
            "Ayakkabılık üstü yürünebilir NavMesh adasına dönüşmemeli.");
        Assert.That(Find("DenizLeftShoe_World").gameObject.activeSelf, Is.False,
            "Eski tek ayakkabı halının yanında kalmamalı.");
        Assert.That(Find("DenizRightShoe_World").gameObject.activeSelf, Is.False,
            "Eski tek ayakkabı halının yanında kalmamalı.");
        Bounds denizShoeBounds = GetBounds(Find("DenizShoePair_Drag"));
        Assert.That(denizShoeBounds.size.x, Is.LessThanOrEqualTo(0.44f),
            "Ayakkabı çifti çocuk ölçeğinde olmalı.");
        Assert.That(denizShoeBounds.size.z, Is.LessThanOrEqualTo(0.36f),
            "Ayakkabı çifti çocuk ölçeğinde olmalı.");
        Assert.That(Find("DenizShoePair_Drag").Find("DenizShoePair_Left/ShoeLace_1"), Is.Not.Null,
            "Sneaker bağcıkları üstten okunmalı.");
        Assert.That(Find("CanShoes_World").Find("LeftShoe").gameObject.activeSelf, Is.True,
            "Can'ın ayakkabı görselleri yanlışlıkla kapatılmamalı.");
        Assert.That(Find("CanShoes_World").Find("LeftShoe/ShoeLace_1"), Is.Not.Null,
            "Can'ın sneaker bağcıkları üstten okunmalı.");
        Assert.That(Find("CanShoes_World_Legacy").gameObject.activeSelf, Is.False,
            "Eski orantısız Can ayakkabıları görünmemeli.");

        Bounds bagShelfBounds = GetBounds(Find("EntryBagShelf_Continuity"));
        AssertSupportedBy(Find("EmergencyBag"), bagShelfBounds, "Afet çantası giriş rafında olmalı.");
        Assert.That(bagShelfBounds.center.x, Is.GreaterThan(4.35f),
            "Çanta rafı ayakkabılıkla aynı duvar düzenine ait olmalı.");
        Assert.That(Find("EntryBagShelf_Continuity").GetComponentInChildren<NavMeshObstacle>(true),
            Is.Not.Null.And.Property("carving").True);

        StoryInteractable[] entryInteractions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string id in new[] { "quake.post.shoes", "quake.post.canlaces", "quake.post.bag" })
        {
            StoryInteractable interaction = entryInteractions.Single(candidate => candidate.InteractionId == id);
            Assert.That(interaction.InteractionPoint.name, Is.EqualTo("EntryStorageFloorStand"), id);
            Assert.That(interaction.InteractionPoint.position.x, Is.LessThan(shoeBenchBounds.min.x - 0.15f), id);
        }
        Assert.That(Find("Can_ToyCar_FinishMarker").gameObject.activeSelf, Is.False,
            "Araba hedef halkası yalnız ilgili sürükleme adımında görünmeli.");
    }

    [Test]
    public void PendantLamp_IsCeilingAnchoredAndLegacyTripodVisualIsDisabled()
    {
        Transform lamp = Find("HangingLamp_QuakeMotion");
        Transform visual = Find("PendantLamp_AnchoredVisual");
        Assert.That(lamp, Is.Not.Null);
        Assert.That(visual, Is.Not.Null);
        Assert.That(lamp.position.y, Is.EqualTo(3.18f).Within(0.01f));
        foreach (string part in new[]
                 {
                     "CeilingPlate", "SuspensionCable", "PendantShade",
                     "PendantLowerRim", "VisibleWarmBulb", "PendantBulbLight"
                 })
            Assert.That(visual.Find(part), Is.Not.Null, part);
        Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Tavan lambası dokunma ışınını kesmemeli.");
        Assert.That(visual.Find("PendantBulbLight").GetComponent<Light>(), Is.Not.Null);

        Transform imported = lamp.Find("HangingLamp_Visual");
        Assert.That(imported, Is.Not.Null);
        Assert.That(imported.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled), Is.True);
        Assert.That(imported.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
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

    private static void AssertSupportedBy(Transform prop, Bounds support, string message)
    {
        Assert.That(prop, Is.Not.Null, message);
        Bounds propBounds = GetBounds(prop);
        Assert.That(propBounds.min.y, Is.GreaterThanOrEqualTo(support.max.y - 0.025f), message);
        Assert.That(propBounds.center.x, Is.InRange(support.min.x - 0.05f, support.max.x + 0.05f), message);
        Assert.That(propBounds.center.z, Is.InRange(support.min.z - 0.05f, support.max.z + 0.05f), message);
    }

    private static Bounds GetBounds(Transform root)
    {
        Assert.That(root, Is.Not.Null);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
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
