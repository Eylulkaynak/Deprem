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
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 54f), Is.True,
            "Kapı yanındaki plan ve aile, dar portre ekranda aynı konuşma kadrajına sığmalı.");

        StoryInteractable safePlay = Object.FindObjectsByType<StoryInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(interactable => interactable.InteractionId == "home.risk.safeplay");
        Assert.That(safePlay.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.HomeFinalTest),
            "Okuma köşesi sürüklemesi iki ucu aynı sabit portre kadrajında göstermeli.");
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
    public void ExitClutter_UsesReadableChildScalePropsWithoutLegacyBlobs()
    {
        Bounds shoes = GetBounds(Find("ExitShoes_Start"));
        Bounds storedShoes = GetBounds(Find("ExitShoes_Stored"));
        Bounds toy = GetBounds(Find("ExitToy_Start"));
        Bounds parcel = GetBounds(Find("ExitParcel_Start"));

        Assert.That(shoes.size.x, Is.LessThanOrEqualTo(0.46f));
        Assert.That(shoes.size.z, Is.LessThanOrEqualTo(0.38f));
        Assert.That(storedShoes.size.x, Is.LessThanOrEqualTo(0.46f));
        Assert.That(storedShoes.size.z, Is.LessThanOrEqualTo(0.38f));
        Assert.That(toy.size.magnitude, Is.LessThanOrEqualTo(0.58f));
        Assert.That(parcel.size.magnitude, Is.LessThanOrEqualTo(0.75f));
        Assert.That(Find("ExitShoes_Start").Find("LeftShoe/ShoeLace_1"), Is.Not.Null,
            "Çıkış ayakkabıları üstten sneaker olarak okunmalı.");
    }

    [Test]
    public void ShelfProps_AreOrderedOnRealShelvesInsteadOfFloatingOrScattering()
    {
        Bounds shelf = GetBounds(Find("Shelf_Unsecured"));
        Transform booksHigh = Find("ShelfBooks_High");
        Transform booksLow = Find("ShelfBooks_Low");
        Assert.That(booksHigh, Is.Not.Null);
        Assert.That(booksLow, Is.Not.Null);
        Assert.That(booksHigh.Cast<Transform>().Count(child => child.name == "BookSet"), Is.EqualTo(1));
        Assert.That(booksLow.Cast<Transform>().Count(child => child.name == "BookSet"), Is.EqualTo(1));
        Assert.That(booksHigh.gameObject.activeSelf, Is.True);
        Assert.That(booksLow.gameObject.activeSelf, Is.False);

        Bounds[] highBooks = booksHigh.Cast<Transform>()
            .Where(child => child.name == "BookSet")
            .Select(GetBounds)
            .OrderBy(bounds => bounds.center.z)
            .ToArray();
        Bounds[] lowBooks = booksLow.Cast<Transform>()
            .Where(child => child.name == "BookSet")
            .Select(GetBounds)
            .OrderBy(bounds => bounds.center.z)
            .ToArray();

        Assert.That(highBooks[0].min.y, Is.GreaterThan(1.43f),
            "Üst raftaki kitaplar dolabın altına veya zemine düşmemeli.");
        Assert.That(lowBooks[0].min.y, Is.GreaterThan(1.08f),
            "İndirilmiş kitaplar gerçek alt raf yüzeyinde durmalı; yerde yüzmemeli.");
        Assert.That(highBooks[0].min.y - lowBooks[0].min.y, Is.InRange(0.28f, 0.42f),
            "Raf değiştirme animasyonu yalnız gerçek iki raf seviyesi arasında çalışmalı.");

        foreach (string itemName in new[]
                 {
                     "ShelfBooks_High", "ShelfBooks_Low",
                     "ShelfVase_High", "ShelfVase_Low",
                     "ShelfFrame_High", "ShelfFrame_Low"
                 })
        {
            Bounds item = GetBounds(Find(itemName));
            Assert.That(item.center.x, Is.InRange(shelf.min.x - 0.08f, shelf.max.x + 0.08f), itemName);
            Assert.That(item.min.z, Is.GreaterThanOrEqualTo(shelf.min.z - 0.04f), itemName);
            Assert.That(item.max.z, Is.LessThanOrEqualTo(shelf.max.z + 0.04f), itemName);
            Assert.That(item.min.y, Is.GreaterThan(1.08f), itemName + " havada/zeminde kalmamalı");
        }
    }

    [Test]
    public void SharedShelfVariants_UseOneConsistentModernSkin()
    {
        Transform[] skins = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name == "StoryShelfVisual")
            .ToArray();
        Assert.That(skins, Has.Length.EqualTo(3));
        foreach (Transform skin in skins)
        {
            Assert.That(skin.Find("BackPanel"), Is.Not.Null);
            Assert.That(skin.Find("LeftFrame"), Is.Not.Null);
            Assert.That(skin.Find("RightFrame"), Is.Not.Null);
            Assert.That(skin.GetComponentsInChildren<Renderer>(true).Count(renderer => renderer.enabled),
                Is.GreaterThanOrEqualTo(7));
        }

        Transform[] legacyVisuals = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.name == "Bookcase_Visual")
            .ToArray();
        Assert.That(legacyVisuals, Has.Length.EqualTo(3));
        Assert.That(legacyVisuals.SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .All(renderer => !renderer.enabled), Is.True,
            "Eski süslü kitaplık görünümü modern varyantla üst üste binmemeli.");
    }

    [Test]
    public void TaskProps_UseConsistentScaleAndSitOnTheirAuthoredSupports()
    {
        foreach (string floorProp in new[]
                 {
                     "ExitShoes_Start", "ExitToy_Start", "ExitParcel_Start",
                     "Can_ToyCar_InitialRoute", "Can_ToyCar_Blocked",
                     "Can_ToyCar_FinalStart", "Can_ToyCar_FinalFinish"
                 })
        {
            Bounds bounds = GetBounds(Find(floorProp));
            Assert.That(bounds.min.y, Is.InRange(-0.01f, 0.04f), floorProp + " zemine oturmalı");
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.LessThanOrEqualTo(0.46f),
                floorProp + " çocuk ölçeğinde kalmalı");
        }

        Bounds shoeTray = GetBounds(Find("ExitShoeTray"));
        Bounds storedShoes = GetBounds(Find("ExitShoes_Stored"));
        Assert.That(Mathf.Abs(storedShoes.min.y - shoeTray.max.y), Is.LessThanOrEqualTo(0.02f),
            "Toplanmış ayakkabılar tepsinin tabanına oturmalı.");
        Assert.That(storedShoes.center.x, Is.InRange(shoeTray.min.x, shoeTray.max.x));
        Assert.That(storedShoes.center.z, Is.InRange(shoeTray.min.z, shoeTray.max.z));

        Bounds toyBasket = GetBounds(Find("ExitToyStorageBasket"));
        Bounds storedToy = GetBounds(Find("ExitToy_Stored"));
        Assert.That(storedToy.center.x, Is.InRange(toyBasket.min.x, toyBasket.max.x));
        Assert.That(storedToy.center.z, Is.InRange(toyBasket.min.z, toyBasket.max.z));
        Assert.That(storedToy.min.y, Is.InRange(toyBasket.min.y, toyBasket.max.y),
            "Toplanmış oyuncak açık sepetin içinde kalmalı.");

        Bounds parcelPallet = GetBounds(Find("ExitParcelPalletSlat_1"));
        Bounds storedParcel = GetBounds(Find("ExitParcel_Stored"));
        Assert.That(Mathf.Abs(storedParcel.min.y - parcelPallet.max.y), Is.LessThanOrEqualTo(0.02f),
            "Toplanmış koli ahşap paletin üzerinde durmalı.");
        Assert.That(Mathf.Max(storedParcel.size.x, storedParcel.size.z), Is.LessThanOrEqualTo(0.43f));

        Bounds workMat = GetBounds(Find("SecuringHardwareWorkMat"));
        foreach (string tableProp in new[]
                 {
                     "UnsafePoweredDrill_Rebuild", "ShelfBracketInHand", "WardrobeHandStrap"
                 })
        {
            Bounds bounds = GetBounds(Find(tableProp));
            Assert.That(Mathf.Abs(bounds.min.y - workMat.max.y), Is.LessThanOrEqualTo(0.02f),
                tableProp + " çalışma matına oturmalı");
            float longestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Assert.That(longestSide, Is.InRange(0.18f, 0.5f),
                tableProp + " gerçekçi el aleti ölçeğinde kalmalı");
        }

        Transform anchorMarks = Find("WardrobeAnchorMarks");
        Assert.That(anchorMarks.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled), Is.True,
            "Dolap işaretleme hedefi sahnede havada duran bir prop gibi görünmemeli.");

        foreach (string cushionName in new[] { "CanReadingNest_Risk", "CanReadingNest_Safe" })
        {
            Transform cushion = Find(cushionName);
            Bounds bounds = GetBounds(cushion);
            Assert.That(bounds.min.y, Is.InRange(0f, 0.04f), cushionName + " zemine oturmalı");
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.InRange(0.65f, 0.9f),
                cushionName + " çocuk minderinden küçük kitap veya büyük yatak gibi görünmemeli");
            Assert.That(cushion.Find("CushionBody"), Is.Not.Null, cushionName + " okunur minder gövdesi");
            Assert.That(cushion.Find("Tuft_1"), Is.Not.Null, cushionName + " debug diski değil dikişli minder olmalı");
        }
    }

    [Test]
    public void SecuringAnimations_OnlySettleLocallyAndDoNotThrowPropsAcrossTheRoom()
    {
        StoryHomeSafetyDirector director = Object.FindFirstObjectByType<StoryHomeSafetyDirector>();
        foreach ((string propName, Animation animation) in new[]
                 {
                     ("ShelfWallBracket", GetPrivate<Animation>(director, "shelfSecureAnimation")),
                     ("WardrobeAnchorStrap", GetPrivate<Animation>(director, "wardrobeSecureAnimation"))
                 })
        {
            Assert.That(animation, Is.Not.Null, propName);
            Assert.That(animation.clip, Is.Not.Null, propName);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(animation.clip)
                         .Where(binding => binding.propertyName.StartsWith("m_LocalPosition")))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(animation.clip, binding);
                Assert.That(Mathf.Abs(curve.keys[^1].value - curve.keys[0].value), Is.LessThanOrEqualTo(0.13f),
                    propName + " yalnız kısa bir yerine oturma hareketi yapmalı");
            }
        }

        foreach ((Transform target, Animation animation) in new[]
                 {
                     (Find("Shelf_Unsecured"), GetPrivate<Animation>(director, "shelfStabilityAnimation")),
                     (Find("Wardrobe_Unsecured"), GetPrivate<Animation>(director, "wardrobeRockAnimation"))
                 })
        {
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip, Is.Not.Null);
            foreach ((string property, float settled) in new[]
                     {
                         ("localEulerAnglesRaw.x", target.localEulerAngles.x),
                         ("localEulerAnglesRaw.y", target.localEulerAngles.y),
                         ("localEulerAnglesRaw.z", target.localEulerAngles.z)
                     })
            {
                EditorCurveBinding binding = AnimationUtility.GetCurveBindings(animation.clip)
                    .Single(candidate => candidate.propertyName == property);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(animation.clip, binding);
                Assert.That(Mathf.DeltaAngle(curve.keys[0].value, settled), Is.EqualTo(0f).Within(0.01f),
                    target.name + "/" + property + " temel dönüşten başlamalı");
                Assert.That(Mathf.DeltaAngle(curve.keys[^1].value, settled), Is.EqualTo(0f).Within(0.01f),
                    target.name + "/" + property + " temel dönüşe dönmeli");
                if (property != "localEulerAnglesRaw.z")
                    Assert.That(curve.keys.All(key => Mathf.Abs(Mathf.DeltaAngle(key.value, settled)) < 0.01f),
                        target.name + "/" + property + " sallanırken yana devrilmemeli");
            }
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
    }

    private static Transform Find(string name)
    {
        return Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(transform => transform.name == name);
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

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }
}
