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
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

public sealed class StoryPreparationRebuildPreviewTests
{
    private const string ScenePath = "Assets/Scenes/Story_01_RebuildPreview.unity";
    private const string SharedHomePath = "Assets/Story/Prefabs/Home/StoryHome_Shared.prefab";

    [SetUp]
    public void OpenPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [Test]
    public void Preview_UsesConnectedSharedHomeAndIndependentSceneNavigation()
    {
        GameObject root = GameObject.Find("STORY_01_REBUILD_PREVIEW");
        Assert.That(root, Is.Not.Null);

        Transform sharedHome = Find("StoryHome_Shared");
        Assert.That(sharedHome, Is.Not.Null);
        Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sharedHome.gameObject),
            Is.EqualTo(SharedHomePath));
        Assert.That(sharedHome.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(sharedHome.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(sharedHome.localScale, Is.EqualTo(Vector3.one));

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        Assert.That(surface, Is.Not.Null);
        Assert.That(surface.navMeshData, Is.Not.Null);
        Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath), Is.True,
            "Onaylanan Story 01 sahnesi yayın rotasında olmalı.");
    }

    [Test]
    public void Preview_HasSingleInputOwnerNoCenterActionButtonAndElevenComposedCameras()
    {
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryPlayerMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryPreparationDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(interactions, Has.Length.GreaterThanOrEqualTo(34));
        Assert.That(interactions.All(interaction =>
                interaction.GetComponentsInChildren<Renderer>(true).Length > 0), Is.True,
            "Story 01 must not contain collider-only fake interaction hotspots.");

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(12));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 50f), Is.True);
    }

    [Test]
    public void Preview_UsesOneHumanScaleAndKeepsCharactersAboveTheFloor()
    {
        Bounds deniz = CombinedBounds(Find("Deniz_12"));
        Bounds can = CombinedBounds(Find("Can_8"));
        Bounds openBag = CombinedBounds(Find("EmergencyBag_Open_Packing"));
        Transform safeTableTop = Find("SafeTable")?.Find("Top");

        Assert.That(deniz.min.y, Is.InRange(-0.012f, 0.01f),
            "Deniz'in görünür ayakkabıları zemine basmalı; yürüyüşte havada görünmemeli.");
        Assert.That(can.min.y, Is.InRange(-0.012f, 0.01f),
            "Can'ın görünür ayakkabıları zemine basmalı; yürüyüşte havada görünmemeli.");
        Assert.That(deniz.size.y, Is.InRange(1.4f, 1.56f));
        Assert.That(can.size.y, Is.InRange(1.16f, 1.34f));
        Assert.That(deniz.size.y, Is.GreaterThan(can.size.y + 0.12f));

        Assert.That(safeTableTop, Is.Not.Null);
        float tableSurfaceY = safeTableTop.position.y + safeTableTop.lossyScale.y * 0.5f;
        Assert.That(tableSurfaceY, Is.InRange(0.74f, 0.82f),
            "Çocukların kullandığı masa 1 metreyi aşmamalı; ev ölçeğinde yaklaşık 76 cm olmalı.");

        Assert.That(openBag.size.y, Is.InRange(0.36f, 0.56f));
        Assert.That(openBag.size.x, Is.LessThan(0.78f));
        Assert.That(openBag.size.z, Is.LessThan(0.72f));
        Assert.That(openBag.size.y, Is.LessThan(can.size.y * 0.48f),
            "Afet çantası sekiz yaşındaki çocuğun gövdesi kadar büyük görünmemeli.");

        CinemachineCamera signalCamera = Find("CM_PreparationSignal_Rebuild")
            ?.GetComponent<CinemachineCamera>();
        Bounds drawer = CombinedBoundsIncludingInactive(Find("SignalNightstandOpen"));
        Assert.That(signalCamera, Is.Not.Null);
        Assert.That(Vector3.Distance(signalCamera.transform.position, drawer.center), Is.LessThan(3f),
            "Çekmece açılırken kamera içeriği okuyacak kadar yaklaşmalı.");
    }

    [Test]
    public void PackedItemResults_StayOrganizedBelowTheBagRim()
    {
        Transform opening = Find("BagOpeningPoint");
        Transform packedRoot = Find("PackedItemResults");
        Assert.That(opening, Is.Not.Null);
        Assert.That(packedRoot, Is.Not.Null);

        Transform[] packedItems = packedRoot.Cast<Transform>().ToArray();
        Assert.That(packedItems, Has.Length.GreaterThanOrEqualTo(10));
        foreach (Transform packed in packedItems)
        {
            Vector3 offset = packed.position - opening.position;
            Assert.That(offset.x, Is.InRange(-0.22f, 0.22f), packed.name);
            Assert.That(offset.z, Is.InRange(-0.18f, 0.18f), packed.name);
            Assert.That(offset.y, Is.InRange(-0.23f, -0.1f),
                packed.name + " çantanın üstünde havada değil, ağız seviyesinin altında kalmalı.");
        }
    }

    [Test]
    public void DialogueVoiceBank_HasStory01ClipsAndDedicatedSource()
    {
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(ui, Is.Not.Null);

        AudioSource source = GetPrivate<AudioSource>(ui, "dialogueVoiceSource");
        Assert.That(source, Is.Not.Null);
        Assert.That(source.playOnAwake, Is.False);
        Assert.That(source.loop, Is.False);
        Assert.That(source.spatialBlend, Is.Zero);

        Array bindings = GetPrivate<Array>(ui, "dialogueVoices");
        Assert.That(bindings, Is.Not.Null);
        Assert.That(bindings.Length, Is.EqualTo(26));
        foreach (object binding in bindings)
        {
            Type bindingType = binding.GetType();
            string subtitle = bindingType
                .GetField("subtitle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(binding) as string;
            AudioClip clip = bindingType
                .GetField("clip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(binding) as AudioClip;
            Assert.That(subtitle, Is.Not.Null.And.Not.Empty);
            Assert.That(clip, Is.Not.Null, subtitle);
        }
    }

    [Test]
    public void SignalDrawerChoices_UseCompactNonOverlappingMarkers()
    {
        string[] drawerItemNames =
        {
            "DrawerItem_Flashlight",
            "DrawerItem_Batteries",
            "DrawerItem_Whistle",
            "DrawerItem_Radio"
        };

        foreach (string itemName in drawerItemNames)
        {
            StoryInteractable interaction = Find(itemName)?.GetComponent<StoryInteractable>();
            Assert.That(interaction, Is.Not.Null, itemName);
            Assert.That(interaction.HighlightRoot, Is.Not.Null, itemName);

            Bounds markerBounds = CombinedBoundsIncludingInactive(interaction.HighlightRoot.transform);
            Bounds itemBounds = CombinedBoundsIncludingInactive(interaction.transform);
            Assert.That(markerBounds.size.x, Is.LessThan(0.14f),
                itemName + " rozeti yakın çekimde eşyanın üzerini kapatmamalı.");
            Assert.That(markerBounds.size.y, Is.LessThan(0.18f),
                itemName + " rozeti çekmece kadrajını kapatmamalı.");
            Assert.That(markerBounds.center.y, Is.GreaterThan(itemBounds.max.y));
            Assert.That(Mathf.Abs(markerBounds.center.x - itemBounds.center.x), Is.LessThan(0.04f));
        }
    }

    [Test]
    public void EveryRequiredItem_HasPhysicalDragPackedResultAndUniquePersistentFlag()
    {
        Assert.That(Find("OriginalBolum1BagSource"), Is.Not.Null,
            "Açık çanta procedural levhalardan değil orijinal Bölüm 1 modelinden gelmeli.");
        StoryPreparationItem[] items = Object.FindObjectsByType<StoryPreparationItem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(items, Has.Length.EqualTo(13));

        StoryPreparationItem[] required = items.Where(item => item.Recommended).ToArray();
        StoryPreparationItem[] nearMisses = items.Where(item => !item.Recommended).ToArray();
        Assert.That(required, Has.Length.EqualTo(10));
        Assert.That(nearMisses, Has.Length.EqualTo(3));
        BagDropZone physicalBag = GameObject.Find("PhysicalBagOpening").GetComponent<BagDropZone>();
        Assert.That(physicalBag, Is.Not.Null);
        foreach (StoryPreparationItem item in items)
        {
            Assert.That(item.LegacyBagMotion.DropZoneOverride, Is.SameAs(physicalBag), item.ItemId);
            Assert.That(GetPrivate<bool>(item, "hideSourceWhenUnavailable"), Is.True, item.ItemId);
        }
        Assert.That(required.Select(item => item.Flag).Distinct().Count(), Is.EqualTo(10));
        Assert.That(required.All(item => item.Flag != StoryFlag.None), Is.True);
        Assert.That(nearMisses.All(item => item.Flag == StoryFlag.None), Is.True);

        foreach (StoryPreparationCategory category in Enum.GetValues(typeof(StoryPreparationCategory)))
        {
            Assert.That(required.Any(item => item.Category == category), Is.True,
                category + " kategorisi en az bir gerekli fiziksel eşya taşımalı.");
        }

        foreach (StoryPreparationItem item in required)
        {
            Assert.That(item.Interactable, Is.Not.Null, item.ItemId);
            Assert.That(item.Interactable.InteractionGesture,
                Is.EqualTo(StoryInteractionGesture.DragToBag), item.ItemId);
            Assert.That(item.Interactable.InteractFromAnywhere, Is.True, item.ItemId);
            Assert.That(item.LegacyBagMotion, Is.Not.Null, item.ItemId);
            Assert.That(GetPrivate<bool>(item.LegacyBagMotion, "inputEnabled"), Is.False, item.ItemId);
            Assert.That(GetPrivate<bool>(item.LegacyBagMotion, "notifyGameManager"), Is.False, item.ItemId);
            Assert.That(GetPrivate<bool>(item.LegacyBagMotion, "tapToBagEnabled"), Is.False, item.ItemId);
            Assert.That(GetPrivate<GameObject>(item, "packedVisual"), Is.Not.Null, item.ItemId);
            Assert.That(item.Interactable.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1), item.ItemId);
        }

        Assert.That(nearMisses.All(item => item.ConsequenceRoot != null), Is.True);
    }

    [Test]
    public void CategoryFlow_CannotSoftlockOnOptionalItems()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        Assert.That(director.RevisedFlow, Is.True);
        StoryPreparationItem[] items = GetPrivate<StoryPreparationItem[]>(director, "items");
        Assert.That(items, Has.Length.EqualTo(13));

        foreach (StoryPreparationCategory category in Enum.GetValues(typeof(StoryPreparationCategory)))
        {
            StoryPreparationItem[] categoryItems = items.Where(item => item.Category == category).ToArray();
            StoryPreparationItem[] required = categoryItems.Where(item => item.Recommended).ToArray();
            Assert.That(required.Length, Is.GreaterThan(0), category.ToString());
            Assert.That(required.All(item => item.Interactable != null && item.LegacyBagMotion != null), Is.True,
                category + " gerekli fiziksel yolu eksik.");

            int optionalCount = categoryItems.Count(item => !item.Recommended);
            Assert.That(required.Length, Is.LessThanOrEqualTo(categoryItems.Length - optionalCount),
                category + " ilerleme hesabı isteğe bağlı yanlış seçimlere bağlı olmamalı.");
        }
    }

    [Test]
    public void RevisedFlow_UsesPhysicalPlanCardAndFourFurnitureDiscoveries()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable plan = GetPrivate<StoryInteractable>(director, "startFamilyPlan");
        StoryInteractable contact = GetPrivate<StoryInteractable>(director, "placeContactCard");
        StoryInteractable role = GetPrivate<StoryInteractable>(director, "assignCanWhistleRole");
        Assert.That(plan.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(plan.GestureTarget, Is.Not.Null);
        Assert.That(plan.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(plan.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));
        StoryInteractable[] planCards = { plan, contact, role };
        Assert.That(planCards.All(interaction =>
            interaction.InteractionGesture == StoryInteractionGesture.DragToTarget), Is.True);
        Assert.That(planCards.All(interaction => interaction.GestureTarget != null), Is.True);
        Assert.That(planCards.All(interaction => interaction.GetComponent<DraggableItem>() != null), Is.True);
        Assert.That(planCards.All(interaction =>
            GetPrivate<bool>(interaction.GetComponent<DraggableItem>(), "dragOnCameraPlane")), Is.True,
            "Duvardaki plana giden kartlar parmağı dikey ekran düzleminde izlemeli.");
        Assert.That(planCards.All(interaction =>
            GetPrivate<bool>(interaction.GetComponent<DraggableItem>(), "faceCameraWhileDragging")), Is.True,
            "Plan kartları tutulunca yazısı okunacak şekilde kameraya dönmeli.");
        Transform[] dragPlaneAnchors = planCards
            .Select(interaction => GetPrivate<Transform>(
                interaction.GetComponent<DraggableItem>(),
                "dragPlaneAnchor"))
            .ToArray();
        Assert.That(dragPlaneAnchors, Has.All.Not.Null);
        Assert.That(dragPlaneAnchors.Distinct().Count(), Is.EqualTo(1),
            "Masanın farklı yerlerinden alınan bütün kartlar aynı kamera derinliğinde taşınmalı.");
        Assert.That(dragPlaneAnchors[0].name, Is.EqualTo("PlanCardSharedDragPlane"));
        Assert.That(planCards.All(interaction =>
        {
            DraggableItem draggable = interaction.GetComponent<DraggableItem>();
            Transform[] targets = GetPrivate<Transform[]>(draggable, "magneticSnapTargets");
            return targets != null &&
                   targets.Length == 3 &&
                   targets.Select(target => target.name).SequenceEqual(new[]
                   {
                       "PlanCardGhostPulse_1",
                       "PlanCardGhostPulse_2",
                       "PlanCardGhostPulse_3"
                   }) &&
                   GetPrivate<float>(draggable, "magneticSnapViewportRadius") >= 0.18f &&
                   GetPrivate<float>(draggable, "magneticSnapMinTravelPixels") >= 40f &&
                   GetPrivate<float>(draggable, "magneticSnapStrength") >= 0.9f &&
                   GetPrivate<float>(draggable, "magneticSnapSurfaceOffset") <= 0.005f &&
                   GetPrivate<bool>(draggable, "clampDragMinimumY") &&
                   GetPrivate<float>(draggable, "dragMinimumWorldY") >= 1.05f;
        }), Is.True,
            "Üç plan kartı da yuvalara yaklaşınca 3B manyetik çekim uygulamalı ve masa içine inmemeli.");

        string[] artworkNames =
        {
            "MeetingPointCardArtwork",
            "MelekContactCard_Drag_Artwork",
            "CanWhistleRoleCard_Drag_Artwork"
        };
        Assert.That(artworkNames.All(name => Find(name).localPosition.sqrMagnitude < 0.0001f), Is.True,
            "Kart yazısı dünya orijininden miras kalan rastgele offset taşımamalı.");
        string[] labelNames =
        {
            "MeetingPointCardLabel",
            "MelekContactCard_Drag_Label",
            "CanWhistleRoleCard_Drag_Label"
        };
        Assert.That(labelNames.All(name =>
            Quaternion.Angle(Find(name).localRotation, Quaternion.Euler(90f, 0f, 0f)) < 0.1f), Is.True,
            "Üç kartın eldeki yazı yönü aynı olmalı.");

        for (int index = 1; index <= 3; index++)
        {
            float expectedX = new[] { -0.86f, 0f, 0.86f }[index - 1];
            Transform socketMotion = Find("PlanCardGhostPulse_" + index);
            Transform socket = Find("PlanCardGhostSocket_" + index);
            Transform socketInset = Find("PlanCardGhostInset_" + index);
            Assert.That(socketMotion.localPosition.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(socketMotion.localPosition.y, Is.EqualTo(-0.2f).Within(0.001f));
            Assert.That(socketMotion.localPosition.z, Is.EqualTo(0.245f).Within(0.001f),
                "Yeşil socket merkezi tamamlanmış kartın gerçek yerleşim merkeziyle aynı olmalı.");
            Assert.That(socketMotion.localRotation, Is.EqualTo(Quaternion.identity),
                "Yeşil socket çerçevesi slot merkezinden dönüp kaymamalı.");
            Assert.That(socket, Is.Not.Null);
            Assert.That(socket.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(socket.localScale, Is.EqualTo(new Vector3(0.68f, 0.42f, 0.018f)));
            Assert.That(socketInset.localPosition.x, Is.Zero.Within(0.001f));
            Assert.That(socketInset.localPosition.y, Is.Zero.Within(0.001f));
            Assert.That(AssetDatabase.GetAssetPath(socket.GetComponent<Renderer>().sharedMaterial),
                Is.EqualTo("Assets/Story/Generated/Materials/Story01_PlanSocketGhost.mat"));
        }

        string[] completedNames =
        {
            "FamilyPlanCompleteMark",
            "FamilyContactCompleteMark",
            "FamilyCanRoleCompleteMark"
        };
        foreach (string completedName in completedNames)
        {
            Transform completedCard = Find(completedName);
            Assert.That(completedCard.localPosition.z, Is.GreaterThan(0.23f),
                completedName + " pano yüzeyinin içine gömülmemeli.");
            Transform snapMotion = Find(completedName + "_SnapMotion");
            Animation snapAnimation = snapMotion.GetComponent<Animation>();
            Assert.That(snapAnimation, Is.Not.Null);
            Assert.That(snapAnimation.clip, Is.Not.Null);
            Assert.That(snapAnimation.clip.name, Is.EqualTo("Story01_PlanCardSnap"));
        }
        Assert.That(planCards.All(interaction =>
            interaction.OnInteracted.GetPersistentEventCount() >= 3), Is.True);
        Assert.That(planCards.All(interaction =>
            interaction.HighlightRoot.GetComponent<BillboardToCamera>() != null &&
            interaction.HighlightRoot.GetComponentsInChildren<SpriteRenderer>(true).Length == 3 &&
            interaction.HighlightRoot.GetComponentsInChildren<MeshRenderer>(true).Length == 0), Is.True,
            "Görev göstergesi küp/elmas yerine Kenney dokunma sprite'ı kullanmalı.");
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            StoryChapterBuilderCommon.KenneyInputPromptRoot + "/LICENSE.txt"), Is.Not.Null);
        Assert.That(planCards.Select(interaction => interaction.GetComponent<Renderer>().sharedMaterial).Distinct().Count(),
            Is.EqualTo(1), "Kartlar renk eşleştirmesiyle çözülmemeli.");
        Assert.That(planCards.All(interaction =>
            interaction.Prompt == "Kartı oku ve aile planındaki anlamca doğru bölüme sürükle"), Is.True);
        Assert.That(Find("FirstMissionTargetGuide"), Is.Null,
            "Doğru pano yuvası BURAYA BIRAK çerçevesiyle ele verilmemeli.");
        Assert.That(Find("PlanSlotHint_1").GetComponent<TextMeshPro>().text, Does.Contain("AÇIK ALAN"));
        Assert.That(Find("PlanSlotHint_2").GetComponent<TextMeshPro>().text, Does.Contain("BAŞKA ŞEHİRDE"));
        Assert.That(Find("PlanSlotHint_3").GetComponent<TextMeshPro>().text, Does.Contain("YARDIM ÇAĞIR"));
        Assert.That(Find("FamilyContactCompleteMark"), Is.Not.Null);
        Assert.That(Find("FamilyCanRoleCompleteMark"), Is.Not.Null);

        StoryInteractable[] discoveries =
        {
            GetPrivate<StoryInteractable>(director, "discoverSignal"),
            GetPrivate<StoryInteractable>(director, "discoverFood"),
            GetPrivate<StoryInteractable>(director, "discoverHealth"),
            GetPrivate<StoryInteractable>(director, "discoverWarmth")
        };
        Assert.That(discoveries, Has.All.Not.Null);
        Assert.That(discoveries[0].InteractionGesture, Is.EqualTo(StoryInteractionGesture.SwipeDown));
        Assert.That(discoveries.Skip(1).All(interaction =>
            interaction.InteractionGesture == StoryInteractionGesture.SwipeHorizontal), Is.True);
        Assert.That(discoveries.All(interaction => interaction.InteractFromAnywhere), Is.True,
            "Furniture discoveries must react to the first drag on the visible object without a hidden approach tap.");
        Assert.That(discoveries.All(interaction => interaction.GestureTarget == null), Is.True,
            "The bidirectional swipe icon must accept either horizontal direction.");
        Assert.That(discoveries.All(interaction =>
            interaction.OnInteracted.GetPersistentEventCount() >= 2), Is.True,
            "Every discovery must create a visible furniture result and advance the category flow.");

        Transform signalClosed = Find("SignalNightstand");
        Transform signalOpen = Find("SignalNightstandOpen");
        Transform signalDrawer = signalOpen?.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == "Nightstand_02_Door");
        Assert.That(signalClosed, Is.Not.Null);
        Assert.That(signalOpen, Is.Not.Null);
        Assert.That(signalOpen.gameObject.activeSelf, Is.False);
        Assert.That(signalDrawer, Is.Not.Null);
        Animation drawerAnimation = signalDrawer.GetComponent<Animation>();
        Assert.That(drawerAnimation, Is.Not.Null);
        Assert.That(drawerAnimation.playAutomatically, Is.True);
        Assert.That(AssetDatabase.GetAssetPath(drawerAnimation.clip),
            Is.EqualTo("Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim"));
        StoryPreparationItem[] signalItems = Object.FindObjectsByType<StoryPreparationItem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(item => item.Category == StoryPreparationCategory.Signal)
            .Where(item => item.Recommended)
            .ToArray();
        Assert.That(signalItems, Has.Length.EqualTo(4));
        Assert.That(signalItems.All(item =>
                !item.Interactable.transform.IsChildOf(signalDrawer) &&
                item.Interactable.InteractionGesture == StoryInteractionGesture.DragToBag &&
                item.Interactable.FocusCameraZone == StoryCameraZoneId.PreparationBag),
            Is.True);
        Assert.That(signalItems.All(item =>
        {
            Animation stage = item.Interactable.GetComponent<Animation>();
            return stage != null &&
                   AssetDatabase.GetAssetPath(stage.clip) ==
                   "Assets/Story/Generated/Animations/Chapters/Story01_SignalStage_" + item.ItemId + ".anim" &&
                   GetPrivate<float>(item, "stageDelay") >= 0.68f;
        }), Is.True, "Çekmece seçimi masadaki ayrı sürüklenebilir eşyayı sahne animasyonuyla üretmeli.");

        StoryInteractable[] drawerItems = GetPrivate<StoryInteractable[]>(director, "signalDrawerItems");
        Assert.That(drawerItems, Has.Length.EqualTo(4));
        Assert.That(drawerItems.All(item =>
                item != null &&
                item.transform.IsChildOf(signalDrawer) &&
                item.InteractionId.StartsWith("Take_", StringComparison.Ordinal) &&
                item.InteractionGesture == StoryInteractionGesture.Tap &&
                item.FocusCameraZone == StoryCameraZoneId.PreparationSignal),
            Is.True);
        Assert.That(drawerItems.All(item => Mathf.Abs(item.transform.position.y - 0.47f) < 0.02f),
            Is.True, "Eşyaların tabanı çekmece döşemesine oturmalı; havada görünmemeli.");
        Assert.That(Find("KitchenCabinetDoorLeft"), Is.Not.Null);
        Assert.That(Find("AidCabinetOpenDoor"), Is.Not.Null);
        Assert.That(Find("WarmthChestOpenDoor"), Is.Not.Null);
    }

    [Test]
    public void SignalDrawerCamera_DoesNotFrameThroughDeniz()
    {
        Transform camera = Find("CM_PreparationSignal_Rebuild");
        Transform deniz = Find("Deniz_12");
        Transform drawer = Find("SignalNightstandOpen");

        Assert.That(camera, Is.Not.Null);
        Assert.That(deniz, Is.Not.Null);
        Assert.That(drawer, Is.Not.Null);

        Vector3 cameraPosition = camera.position;
        Vector3 drawerFocus = drawer.position + new Vector3(0f, 0.5f, 0.15f);
        Vector3 viewDirection = (drawerFocus - cameraPosition).normalized;
        float projection = Vector3.Dot(deniz.position - cameraPosition, viewDirection);
        Vector3 closestPoint = cameraPosition + viewDirection *
            Mathf.Clamp(projection, 0f, Vector3.Distance(cameraPosition, drawerFocus));
        Vector3 denizPlanar = new Vector3(deniz.position.x, closestPoint.y, deniz.position.z);

        Assert.That(Vector3.Distance(denizPlanar, closestPoint), Is.GreaterThan(0.8f),
            "Deniz çekmece kamerası ile çekmece arasına girip kadrajı kapatmamalı.");
        Assert.That(cameraPosition.y, Is.GreaterThan(1.55f),
            "Çekmece kamerası kulp ve içeriği birlikte görecek kadar yukarıda olmalı.");
    }

    [Test]
    public void FoodAndHealthDiscoveries_GatePackingBehindDirectPhysicalInspection()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable water = GetPrivate<StoryInteractable>(director, "inspectWaterDate");
        StoryInteractable bandage = GetPrivate<StoryInteractable>(director, "inspectBandageSeal");

        Assert.That(water, Is.Not.Null);
        Assert.That(water.InteractionGesture, Is.EqualTo(StoryInteractionGesture.SwipeHorizontal));
        Assert.That(water.GestureTarget, Is.Not.Null);
        Assert.That(water.InteractFromAnywhere, Is.True);
        Assert.That(water.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));

        Assert.That(bandage, Is.Not.Null);
        Assert.That(bandage.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(bandage.InteractFromAnywhere, Is.True);
        Assert.That(bandage.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));

        Assert.That(Find("WaterExpiryLabel_Unchecked"), Is.Not.Null);
        Assert.That(Find("WaterExpiryCheckedState")?.gameObject.activeSelf, Is.False);
        Assert.That(Find("BandageSealInspection"), Is.Not.Null);
        Assert.That(Find("BandageSealCheckedState")?.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void SignalToolsUseVisibleObjectsAndFakeBagClosureStepsAreRemoved()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable signal = GetPrivate<StoryInteractable>(director, "reviewSignal");
        StoryInteractable signalOff =
            GetPrivate<StoryInteractable>(director, "reviewSignalFlashlightOff");
        StoryInteractable radio = GetPrivate<StoryInteractable>(director, "reviewSignalRadio");
        StoryInteractable whistle = GetPrivate<StoryInteractable>(director, "reviewSignalWhistle");

        Assert.That(signal.InteractionGesture, Is.EqualTo(StoryInteractionGesture.Tap));
        Assert.That(signal.RequiredGestureCount, Is.EqualTo(1));
        Assert.That(signal.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationFlashlight));
        Assert.That(signalOff.InteractionGesture, Is.EqualTo(StoryInteractionGesture.Tap));
        Assert.That(signalOff.RequiredGestureCount, Is.EqualTo(1));
        Assert.That(signalOff.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationFlashlight));
        Assert.That(GetPrivate<Transform>(director, "signalFlashlightApproachPoint"), Is.Not.Null);
        Assert.That(radio.InteractionGesture, Is.EqualTo(StoryInteractionGesture.SwipeHorizontal));
        Assert.That(radio.GestureTarget, Is.Not.Null);
        Assert.That(whistle.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(whistle.GestureTarget, Is.Not.Null);
        Assert.That(whistle.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(whistle.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationSiblingHandoff));
        Assert.That(Find("Review_WhistleCanDropZone"), Is.Not.Null);
        Assert.That(Find("Can_WhistleTargetSocket")?.GetComponent<SpriteRenderer>(), Is.Not.Null);
        Assert.That(Find("Can_WhistleTargetSocket")?.GetComponent<BillboardToCamera>(), Is.Not.Null);
        Assert.That(signal.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(signalOff.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(radio.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1));
        Assert.That(whistle.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));
        Assert.That(new[] { signal, signalOff, radio, whistle }.All(interaction =>
            interaction.GetComponentsInChildren<Renderer>(true).Length > 0), Is.True);
        Assert.That(Find("FlashlightInspectionBeam"), Is.Not.Null);
        Assert.That(radio.gameObject.name, Is.EqualTo("BagReview_EmergencyRadio"));
        Assert.That(Find("Can_WhistleClipped"), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "reviewFood"), Is.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "reviewHealth"), Is.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "reviewWarmth"), Is.Null);
        Assert.That(Find("BagReview_FoodPocket_Closed"), Is.Null);
        Assert.That(Find("BagReview_DocumentSeal_Closed"), Is.Null);
        Assert.That(Find("BagReview_MainZipper_Closed"), Is.Null);
    }

    [Test]
    public void ConsoleConflictUsesVisibleBagAndPhysicalConsoleDrag()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable heavyLift = GetPrivate<StoryInteractable>(director, "testBagWeight");
        StoryInteractable removeConsole = GetPrivate<StoryInteractable>(director, "removeConsole");

        Assert.That(heavyLift.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(heavyLift.gameObject.name, Is.EqualTo("EmergencyBag_Open_Packing"));
        Assert.That(heavyLift.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        Assert.That(removeConsole.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(removeConsole.GestureTarget, Is.Not.Null);
        Assert.That(removeConsole.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(removeConsole.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));
        Assert.That(GetPrivate<StoryInteractable>(director, "testBalancedBag"), Is.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "adjustBagStraps"), Is.Null);

        Assert.That(GetPrivate<GameObject>(director, "consoleConflictRoot"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "consoleInBagRoot"), Is.Not.Null);
        Assert.That(GetPrivate<GameObject>(director, "consoleReturnedRoot"), Is.Not.Null);
        Assert.That(Find("ConsoleConflict_InBag_Visual"), Is.Not.Null);
        Assert.That(Find("ConsoleConflict_ReturnedToTable"), Is.Not.Null);
    }

    [Test]
    public void ComfortBeat_IsARequiredPhysicalDragAndPersistsItsChoice()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable comfort = GetPrivate<StoryInteractable>(director, "chooseComfortItem");

        Assert.That(comfort.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(comfort.GestureTarget, Is.Not.Null);
        Assert.That(comfort.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(comfort.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));
        Assert.That(Find("CanComfortToyDropZone")?.GetComponent<BagDropZone>(), Is.Not.Null);
        Assert.That(Enum.IsDefined(typeof(StoryFlag), StoryFlag.BagComfortItem), Is.True);
    }

    [Test]
    public void FinalFlow_UsesObjectGesturesAndEndsThroughBlackoutTimelineSignal()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable weight = GetPrivate<StoryInteractable>(director, "testBagWeight");
        StoryInteractable place = GetPrivate<StoryInteractable>(director, "placeBagAtExit");

        Assert.That(GetPrivate<StoryInteractable>(director, "inspectEmptyBag"), Is.Null,
            "Ağzı zaten açık olan çantaya sahte fermuar açma etkileşimi bağlanmamalı.");
        Assert.That(Find("Inspect_EmptyBag"), Is.Null);
        Assert.That(Find("BagZipperSwipeTarget"), Is.Null);
        Assert.That(weight.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(weight.gameObject.name, Is.EqualTo("EmergencyBag_Open_Packing"));
        Assert.That(GetPrivate<StoryInteractable>(director, "adjustBagStraps"), Is.Null);
        Assert.That(place.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(place.GestureTarget, Is.Not.Null);
        Assert.That(place.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(place.gameObject.name, Is.EqualTo("EmergencyBag_Worn"));
        Assert.That(place.ReturnCameraAfterCompletion, Is.True);
        Assert.That(place.ReturnCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationOverview));
        Assert.That(place.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(5),
            "Rafa bırakma; çantayı taşımış görünümden rafa geçirmeli, girişi kilitlemeli ve Timeline'ı oynatmalı.");

        PlayableDirector blackout = Object.FindObjectsByType<PlayableDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(candidate => candidate.name == "PreparationBlackoutDrill");
        Assert.That(blackout.playableAsset, Is.TypeOf<TimelineAsset>());
        TimelineAsset timeline = (TimelineAsset)blackout.playableAsset;
        Assert.That(timeline.duration, Is.GreaterThanOrEqualTo(17.9d));
        Assert.That(timeline.GetOutputTracks().OfType<ActivationTrack>().Any(), Is.True,
            "Fener sonucu sahne ActivationTrack'i ile görünmeli.");
        Assert.That(timeline.GetOutputTracks().OfType<AnimationTrack>().Count(), Is.GreaterThanOrEqualTo(2),
            "Oda ve dolgu ışığı sahne AnimationTrack'leri ile kararmalı.");
        SignalEmitter[] emitters = timeline.GetOutputTracks().OfType<SignalTrack>()
            .SelectMany(track => track.GetMarkers())
            .OfType<SignalEmitter>()
            .ToArray();
        Assert.That(emitters, Has.Length.GreaterThanOrEqualTo(7));
        Assert.That(emitters.Any(emitter =>
            emitter.asset != null && emitter.asset.name == "Blackout_RetrieveFlashlight_Pause" &&
            emitter.time >= 1.8d && emitter.time <= 2.0d), Is.True);
        Assert.That(emitters.Any(emitter =>
            emitter.asset != null && emitter.asset.name == "Blackout_Whistle_Pause" &&
            emitter.time >= 11.9d && emitter.time <= 12.1d), Is.True);
        Assert.That(emitters.Any(emitter => emitter.time >= 17.9d && emitter.asset != null), Is.True,
            "Tatbikat sonu sinyali final checkpoint zincirini tetiklemeli.");

        StoryInteractable[] allInteractions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        StoryInteractable findCan = allInteractions.Single(interaction =>
            interaction.InteractionId == "Blackout_FindCan");
        StoryInteractable flashlight = allInteractions.Single(interaction =>
            interaction.InteractionId == "Blackout_RetrieveFlashlight");
        StoryInteractable focusPlan = allInteractions.Single(interaction =>
            interaction.InteractionId == "Blackout_FocusFamilyPlan");
        StoryInteractable focusTable = allInteractions.Single(interaction =>
            interaction.InteractionId == "Blackout_FocusSafeTable");
        StoryInteractable whistle = Find("Blackout_Whistle")?.GetComponent<StoryInteractable>();
        Assert.That(findCan?.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(flashlight.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(flashlight.GestureTarget, Is.Not.Null);
        Assert.That(flashlight.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(flashlight.gameObject.name, Is.EqualTo("Blackout_FlashlightInOuterPocket"));
        Assert.That(focusPlan?.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(focusTable?.InteractionGesture, Is.EqualTo(StoryInteractionGesture.WorldHold));
        Assert.That(Find("BlackoutBeam_FamilyPlan")?.GetComponent<Light>()?.type, Is.EqualTo(LightType.Spot));
        Assert.That(Find("BlackoutBeam_SafeTable")?.GetComponent<Light>()?.type, Is.EqualTo(LightType.Spot));
        Assert.That(Find("BlackoutBeam_Can")?.GetComponent<Light>()?.type, Is.EqualTo(LightType.Spot));
        Assert.That(Find("BlackoutBeam_FamilyPlan")?.gameObject.activeSelf, Is.False);
        Assert.That(Find("BlackoutBeam_SafeTable")?.gameObject.activeSelf, Is.False);
        Assert.That(Find("BlackoutBeam_Can")?.gameObject.activeSelf, Is.False);
        Assert.That(whistle?.InteractionGesture, Is.EqualTo(StoryInteractionGesture.RepeatedTap));
        Assert.That(whistle?.RequiredGestureCount, Is.EqualTo(3));
    }

    [Test]
    public void Preview_RemovesCategoryStationsAndKeepsItemsInEnvironmentalContexts()
    {
        Assert.That(Find("SignalStation"), Is.Null);
        Assert.That(Find("FoodStation"), Is.Null);
        Assert.That(Find("HealthStation"), Is.Null);
        Assert.That(Find("WarmthLabel"), Is.Null);
        Assert.That(Find("SignalNightstand"), Is.Not.Null);
        Assert.That(Find("KitchenLowCabinet"), Is.Not.Null);
        Assert.That(Find("AidServiceCase"), Is.Not.Null);
        Assert.That(Find("AidCabinet"), Is.Null);
        Assert.That(Find("WarmthChest"), Is.Not.Null);
        Assert.That(Find("ExitBagShelf"), Is.Not.Null);
        Assert.That(Find("FamilyPlanBoard"), Is.Not.Null);
        Assert.That(Find("CanComfortToy_Result"), Is.Not.Null);
    }

    [Test]
    public void Preview_UsesDistinctMiniBoysAndCleanSupportedSetDressing()
    {
        Transform deniz = Find("Deniz_12");
        Transform can = Find("Can_8");
        Assert.That(deniz, Is.Not.Null);
        Assert.That(can, Is.Not.Null);
        Transform denizSource = deniz.Cast<Transform>()
            .SingleOrDefault(child => child.name.StartsWith("CharacterSource_", StringComparison.Ordinal));
        Transform canSource = can.Cast<Transform>()
            .SingleOrDefault(child => child.name.StartsWith("CharacterSource_", StringComparison.Ordinal));
        Assert.That(denizSource, Is.Not.Null);
        Assert.That(canSource, Is.Not.Null);
        Assert.That(denizSource.name, Is.Not.EqualTo(canSource.name),
            "Deniz ve Can aynı karakter görünümünü kullanmamalı.");

        bool syntyMiniImported = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Any(path =>
                path.IndexOf("mini", StringComparison.OrdinalIgnoreCase) >= 0 &&
                path.IndexOf("character", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (path.IndexOf("SchoolBoy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 path.IndexOf("School_Boy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 path.IndexOf("Son_01", StringComparison.OrdinalIgnoreCase) >= 0));
        if (syntyMiniImported)
        {
            Assert.That(denizSource.name, Does.Not.Contain("character-male-"));
            Assert.That(canSource.name, Does.Not.Contain("character-male-"));
        }
        else
        {
            Assert.That(denizSource.name, Is.EqualTo("CharacterSource_character-male-a"));
            Assert.That(canSource.name, Is.EqualTo("CharacterSource_character-male-d"));
        }

        Transform bagFitCamera = Find("CM_PreparationDeniz_Rebuild");
        Assert.That(bagFitCamera, Is.Not.Null);
        Vector3 cameraDirection = Vector3.ProjectOnPlane(
            bagFitCamera.position - deniz.position,
            Vector3.up).normalized;
        Assert.That(Vector3.Dot(deniz.forward, cameraDirection), Is.GreaterThan(0.35f),
            "Çanta ağırlığı kadrajı Deniz'in ensesinden değil ön üç çeyreğinden görünmeli.");

        Assert.That(Find("FamilyPlanCardTable"), Is.Not.Null);
        Assert.That(Find("FamilyBoardGame")?.gameObject.activeSelf, Is.False,
            "Dağınık dekor oyunu gerçek etkileşim eşyalarıyla karışmamalı.");

        Assert.That(Find("SofaThrow"), Is.Null,
            "Unreadable legacy Quilt prop must not remain on the sofa.");
        Transform sofaBlanket = Find("KoltukBattaniyesi");
        Assert.That(sofaBlanket, Is.Not.Null);
        MeshFilter sofaBlanketMesh = sofaBlanket.GetComponentInChildren<MeshFilter>(true);
        Assert.That(sofaBlanketMesh, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(sofaBlanketMesh.sharedMesh),
            Is.EqualTo("Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx"));
        Bounds sofaBlanketBounds = CombinedBounds(sofaBlanket);
        Assert.That(sofaBlanketBounds.min.y, Is.EqualTo(0.57f).Within(0.015f),
            "Koltuk battaniyesi minderden yukarıda yüzmemeli.");

        Bounds signalNightstand = CombinedBounds(Find("SignalNightstand"));
        Bounds roomPlant = CombinedBounds(Find("RoomPlant"));
        Bounds familySofa = CombinedBounds(Find("FamilySofa"));
        Bounds warmthChest = CombinedBounds(Find("WarmthChest"));
        Assert.That(Mathf.Abs(sofaBlanketBounds.center.z - familySofa.center.z), Is.LessThan(0.25f),
            "Koltuk battaniyesi ön çıtaya değil oturma minderinin merkezine yerleşmeli.");
        Assert.That(signalNightstand.Intersects(roomPlant), Is.False,
            "Görev komodini bitkinin içine rastgele bırakılmamalı.");
        float sofaSideGap = signalNightstand.min.x - familySofa.max.x;
        Assert.That(sofaSideGap, Is.InRange(0f, 0.45f),
            "Görev komodini odanın ortasında değil kanepenin yanında durmalı.");
        Assert.That(Mathf.Abs(signalNightstand.center.z - familySofa.center.z), Is.LessThan(0.35f),
            "Görev komodini kanepe hattına hizalanmalı.");
        Assert.That(signalNightstand.min.y, Is.EqualTo(0f).Within(0.015f));
        Assert.That(roomPlant.min.y, Is.EqualTo(0f).Within(0.015f));
        Assert.That(warmthChest.min.y, Is.EqualTo(0f).Within(0.015f));
        Assert.That(warmthChest.Intersects(roomPlant), Is.False,
            "Sıcaklık sandığı ve bitki ayrı duvar ceplerinde durmalı.");
        Assert.That(warmthChest.Intersects(familySofa), Is.False,
            "Sıcaklık sandığı kanepe hacmine taşmamalı.");

        GameObject warmthChestSource = PrefabUtility.GetCorrespondingObjectFromSource(Find("WarmthChest").gameObject);
        Assert.That(AssetDatabase.GetAssetPath(warmthChestSource),
            Is.EqualTo("Assets/Story/Environment/ThirdParty/KenneySurvival/Models/chest.fbx"),
            "Sıcaklık deposu ikinci bir komodin değil, okunabilir bir sandık modeli olmalı.");
        GameObject openWarmthChestSource =
            PrefabUtility.GetCorrespondingObjectFromSource(Find("WarmthChestOpenDoor").gameObject);
        Assert.That(AssetDatabase.GetAssetPath(openWarmthChestSource),
            Is.EqualTo("Assets/Story/Environment/ThirdParty/KenneySurvival/Models/box-open.fbx"));

        Assert.That(Find("LowCabinet")?.gameObject.activeSelf, Is.False,
            "Ortak evin ikinci komodini Story 01 servis duvarında gereksiz tekrar oluşturmamalı.");
        Bounds foodCabinetBounds = CombinedBounds(Find("KitchenLowCabinet"));
        Bounds aidServiceCase = CombinedBounds(Find("AidServiceCase"));
        Assert.That(Mathf.Abs(aidServiceCase.min.y - foodCabinetBounds.max.y), Is.LessThan(0.12f),
            "İlk yardım çantası havada kalmamalı; depolama dolabının üst yüzeyine oturmalı.");
        Assert.That(aidServiceCase.size.x, Is.LessThan(0.65f),
            "İlk yardım çantası depolama dolabının dışına taşmamalı.");
        Assert.That(aidServiceCase.min.x, Is.GreaterThanOrEqualTo(foodCabinetBounds.min.x - 0.02f));
        Assert.That(aidServiceCase.max.x, Is.LessThanOrEqualTo(foodCabinetBounds.max.x + 0.02f));
        Assert.That(aidServiceCase.min.z, Is.GreaterThanOrEqualTo(foodCabinetBounds.min.z - 0.02f));
        Assert.That(aidServiceCase.max.z, Is.LessThanOrEqualTo(foodCabinetBounds.max.z + 0.02f),
            "First-aid kit must stay inside the cabinet worktop footprint.");
        Assert.That(Find("AidCabinetBadge"), Is.Null);
        Assert.That(Find("AidCabinetCrossVertical"), Is.Null);
        Assert.That(Find("AidCabinetCrossHorizontal"), Is.Null);

        Transform foodCabinet = Find("KitchenLowCabinet");
        Assert.That(foodCabinetBounds.center.z, Is.EqualTo(-1.82f).Within(0.02f),
            "Mutfak dolabı sağ duvardaki depolama hattına oturmalı.");
        GameObject foodCabinetSource = PrefabUtility.GetCorrespondingObjectFromSource(foodCabinet.gameObject);
        Assert.That(AssetDatabase.GetAssetPath(foodCabinetSource),
            Is.EqualTo("Assets/PolygonTown/Models/Props/SM_Prop_Kitchen_Counter_01.fbx"),
            "Servis dolabı uydurma Cute Furniture kutusu değil POLYGON Town tezgâhı olmalı.");
        Assert.That(foodCabinet.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(3));
        Assert.That(foodCabinet.Cast<Transform>()
            .Any(child => child.name.StartsWith("Kitchen_D_01", StringComparison.Ordinal)), Is.False);

        Transform aidCase = Find("AidServiceCase");
        GameObject aidSource = PrefabUtility.GetCorrespondingObjectFromSource(aidCase.gameObject);
        Assert.That(AssetDatabase.GetAssetPath(aidSource),
            Is.EqualTo("Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/FirstAidKit.fbx"),
            "İlk yardım çantası prosedürel kutu değil hazır Quaternius modeli olmalı.");
        Transform bandages = Find("BandageSeal_Unchecked");
        GameObject bandageSource = PrefabUtility.GetCorrespondingObjectFromSource(bandages.gameObject);
        Assert.That(AssetDatabase.GetAssetPath(bandageSource),
            Is.EqualTo("Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Bandages.fbx"),
            "Sargı paketi küplerden değil hazır Quaternius modelinden gelmeli.");
        Bounds bandageBounds = CombinedBounds(bandages);
        Assert.That(bandageBounds.min.x, Is.GreaterThanOrEqualTo(foodCabinetBounds.min.x - 0.02f));
        Assert.That(bandageBounds.max.x, Is.LessThanOrEqualTo(foodCabinetBounds.max.x + 0.02f));
        Assert.That(bandageBounds.min.z, Is.GreaterThanOrEqualTo(foodCabinetBounds.min.z - 0.02f));
        Assert.That(bandageBounds.max.z, Is.LessThanOrEqualTo(foodCabinetBounds.max.z + 0.02f),
            "Bandages must stay inside the cabinet worktop footprint.");
        Assert.That(Find("BandagePackageBody"), Is.Null);
        Transform[] packingSources = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(transform => transform.name.StartsWith("WorldItem_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(packingSources, Has.Length.EqualTo(13));
        Assert.That(packingSources.All(source => !source.gameObject.activeSelf), Is.True,
            "Mutually exclusive packing categories must not be piled on the table in the authored scene.");
        StoryPreparationItem[] packingItems = Find("_Story01PreviewCore")
            .GetComponent<StoryPreparationDirector>()
            .Items;
        StoryPreparationItem blanketItem = packingItems.Single(item => item.ItemId == "Blanket");
        Renderer blanketRenderer = blanketItem.Interactable.GetComponentInChildren<Renderer>(true);
        Assert.That(blanketRenderer, Is.Not.Null);
        GameObject blanketSource = PrefabUtility.GetCorrespondingObjectFromSource(blanketRenderer.gameObject);
        Assert.That(AssetDatabase.GetAssetPath(blanketSource),
            Is.EqualTo("Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx"),
            "The packing blanket must use the readable Kenney bedroll instead of Quilt_514.");
        foreach (IGrouping<StoryPreparationCategory, StoryPreparationItem> category in
                 packingItems.GroupBy(item => item.Category))
        {
            StoryPreparationItem[] categoryItems = category.ToArray();
            for (int first = 0; first < categoryItems.Length; first++)
            {
                Bounds firstBounds = CombinedBoundsIncludingInactive(categoryItems[first].Interactable.transform);
                for (int second = first + 1; second < categoryItems.Length; second++)
                {
                    Bounds secondBounds =
                        CombinedBoundsIncludingInactive(categoryItems[second].Interactable.transform);
                    bool overlapsX = firstBounds.max.x > secondBounds.min.x + 0.025f &&
                                     secondBounds.max.x > firstBounds.min.x + 0.025f;
                    bool overlapsZ = firstBounds.max.z > secondBounds.min.z + 0.025f &&
                                     secondBounds.max.z > firstBounds.min.z + 0.025f;
                    Assert.That(overlapsX && overlapsZ, Is.False,
                        $"{category.Key}: {categoryItems[first].ItemId} and {categoryItems[second].ItemId} overlap.");
                }
            }
        }
        Assert.That(Find("ExitBagShelf").GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
            "Çıkış rafı boş bir görev kökü olmamalı.");

        Transform wardrobe = Find("Wardrobe_Unsecured");
        Assert.That(wardrobe, Is.Not.Null);
        Assert.That(wardrobe.localPosition.x, Is.EqualTo(1.15f).Within(0.01f));

        TextMeshPro header = Find("PlanHeaderText")?.GetComponent<TextMeshPro>();
        Assert.That(header, Is.Not.Null);
        Assert.That(header.text, Is.EqualTo("AİLE AFET PLANI"));
        Assert.That(header.fontSize, Is.LessThanOrEqualTo(1.1f));
        Assert.That(header.overflowMode, Is.EqualTo(TextOverflowModes.Truncate));

        string[] removedFakeReviewMeshes =
        {
            "BagReview_RadioKnob_Untuned",
            "BagReview_RadioKnob_Tuned",
            "BagReview_FoodPocket_Open",
            "BagReview_DocumentSeal_Open",
            "BagReview_MainZipper_Open"
        };
        Assert.That(removedFakeReviewMeshes.All(objectName => Find(objectName) == null), Is.True,
            "Modelde bulunmayan donanım için gizli primitive veya boş etkileşim üretilmemeli.");
        Assert.That(Find("BagReview_EmergencyRadio")?.GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
            "Radyo kontrolü görünür gerçek radyo modelinin üzerinde kalmalı.");
    }

    [Test]
    public void Preview_DisablesDecorativeRealtimeShadows()
    {
        Light[] lights = Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        Assert.That(lights, Is.Not.Empty);
        Assert.That(lights.All(light => light.shadows == LightShadows.None), Is.True);
        Assert.That(renderers.All(renderer =>
            renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off &&
            !renderer.receiveShadows), Is.True);
    }

    private static Transform Find(string name)
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(transform => transform.name == name);
    }

    private static Bounds CombinedBounds(Transform root)
    {
        Assert.That(root, Is.Not.Null);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Bounds CombinedBoundsIncludingInactive(Transform root)
    {
        Assert.That(root, Is.Not.Null);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }
}
