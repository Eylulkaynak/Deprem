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
    private const float ExpectedCeilingBottomY = 4.03f;

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
    public void Preview_HasSingleInputOwnerNoCenterActionButtonAndFifteenComposedCameras()
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
        string[] modelSurfaceHotspotIds =
        {
            "Inspect_FlashlightSwitchOn",
            "Inspect_FlashlightSwitchOff",
            "Review_PowerEmergencyRadio"
        };
        Assert.That(interactions.All(interaction =>
                interaction.GetComponentsInChildren<Renderer>(true).Length > 0 ||
                modelSurfaceHotspotIds.Contains(interaction.InteractionId)), Is.True,
            "Only the narrow hotspots placed over existing model controls may omit their own renderer.");

        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        Assert.That(GetPrivate<bool>(touch, "directWorldGestures"), Is.True);

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(cameras, Has.Length.EqualTo(15));
        Assert.That(cameras.All(camera =>
            camera.Lens.FieldOfView >= 38f && camera.Lens.FieldOfView <= 60f), Is.True);
    }

    [Test]
    public void InteractionCameras_FrameTheirActualTargetsAtPortraitAspect()
    {
        AssertPortraitCameraFrames(
            "CM_PreparationSignal_Rebuild",
            CombinedBoundsIncludingInactive(Find("SignalNightstandOpen")),
            0.12f);
        AssertPortraitCameraFrames(
            "CM_PreparationFlashlight_Rebuild",
            CombinedBoundsIncludingInactive(Find("WorldItem_Flashlight")?.Find("El feneri")),
            0.12f);
        AssertPortraitCameraFrames(
            "CM_PreparationRadio_Rebuild",
            CombinedBoundsIncludingInactive(Find("BagReview_EmergencyRadio")),
            0.12f);
        AssertPortraitCameraFrames(
            "CM_PreparationWaterInspection_Rebuild",
            CombinedBoundsIncludingInactive(Find("Su")),
            0.24f);
        AssertPortraitCameraFrames(
            "CM_PreparationWaterInspection_Rebuild",
            CombinedBoundsIncludingInactive(Find("WaterExpiryLabel_Surface")),
            0.06f);
        AssertPortraitCameraFrames(
            "CM_PreparationBandageInspection_Rebuild",
            CombinedBoundsIncludingInactive(Find("BandageSeal_Unchecked")),
            0.16f);
        AssertPortraitCameraFrames(
            "CM_PreparationSiblingHandoff_Rebuild",
            CombinedBoundsIncludingInactive(Find("Review_WhistleHandoff_Visual")),
            0.06f);

        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        Assert.That(GetPrivate<StoryInteractable>(director, "inspectWaterDate").FocusCameraZone,
            Is.EqualTo(StoryCameraZoneId.PreparationWaterInspection));
        Assert.That(GetPrivate<StoryInteractable>(director, "inspectBandageSeal"), Is.Null,
            "Sargı yakın çekimi ikinci bir sahte etkileşim hotspot'u oluşturmamalı.");
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
    public void FamilyCharacters_HaveReadableFacesAndDoNotUseBrokenFootStabilization()
    {
        foreach (string characterName in new[] { "Deniz_12", "Can_8", "Anne_Ayse" })
        {
            Transform character = Find(characterName);
            Assert.That(character, Is.Not.Null, characterName);

            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, characterName + " Animator");
            Assert.That(animator.stabilizeFeet, Is.False,
                characterName + " Meshy ayak hedefleri masa çevresinde dizleri katlamamalı.");

            Transform faceRig = Find(characterName + "_FaceRig");
            Assert.That(faceRig, Is.Not.Null, characterName + " yüz rig'i");
            Assert.That(faceRig.IsChildOf(character), Is.True, characterName + " yüz rig hiyerarşisi");
            Assert.That(faceRig.Find("Brow_L"), Is.Not.Null, characterName + " sol kaş");
            Assert.That(faceRig.Find("Brow_R"), Is.Not.Null, characterName + " sağ kaş");
            Assert.That(faceRig.Find("Eyelid_L"), Is.Not.Null, characterName + " sol göz kapağı");
            Assert.That(faceRig.Find("Eyelid_R"), Is.Not.Null, characterName + " sağ göz kapağı");
            Transform mouth = faceRig.Find("Mouth_Center");
            Assert.That(mouth, Is.Not.Null, characterName + " ağız");
            Assert.That(mouth.GetComponent<Renderer>(), Is.Not.Null, characterName + " görünür ağız");
            Assert.That(faceRig.GetComponent<Animation>(), Is.Not.Null,
                characterName + " canlı yüz animasyonu");
        }


        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(ui, Is.Not.Null);
        SerializedProperty actors = new SerializedObject(ui).FindProperty("dialogueActors");
        Assert.That(actors, Is.Not.Null);
        Assert.That(actors.arraySize, Is.EqualTo(3));
        string[] expectedRoots = { "Deniz_12", "Can_8", "Anne_Ayse" };
        string[][] expectedAliases =
        {
            new[] { "Deniz" },
            new[] { "Can" },
            new[] { "Anne", "Ayşe", "Ayse" }
        };
        for (int index = 0; index < actors.arraySize; index++)
        {
            SerializedProperty actor = actors.GetArrayElementAtIndex(index);
            Transform actorRoot = actor.FindPropertyRelative("actorRoot").objectReferenceValue as Transform;
            Transform head = actor.FindPropertyRelative("head").objectReferenceValue as Transform;
            Transform mouth = actor.FindPropertyRelative("mouth").objectReferenceValue as Transform;
            SerializedProperty aliases = actor.FindPropertyRelative("aliases");
            Assert.That(actorRoot, Is.Not.Null);
            Assert.That(actorRoot.name, Is.EqualTo(expectedRoots[index]));
            Assert.That(head, Is.Not.Null.And.Property("name").EqualTo("Head"));
            Assert.That(head.IsChildOf(actorRoot), Is.True);
            Assert.That(mouth, Is.Not.Null.And.Property("name").EqualTo("Mouth_Center"));
            Assert.That(mouth.IsChildOf(actorRoot), Is.True);
            Assert.That(aliases.arraySize, Is.EqualTo(expectedAliases[index].Length));
            for (int aliasIndex = 0; aliasIndex < aliases.arraySize; aliasIndex++)
                Assert.That(aliases.GetArrayElementAtIndex(aliasIndex).stringValue,
                    Is.EqualTo(expectedAliases[index][aliasIndex]));
        }

        AnimationClip faceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            StoryChapterBuilderCommon.CharacterFaceAliveClipPath);
        Assert.That(faceClip, Is.Not.Null);
        string[] animatedPaths = AnimationUtility.GetCurveBindings(faceClip)
            .Select(binding => binding.path)
            .Distinct()
            .ToArray();
        Assert.That(animatedPaths, Does.Contain("Eyelid_L"));
        Assert.That(animatedPaths, Does.Contain("Eyelid_R"));
        Assert.That(animatedPaths, Does.Not.Contain("Mouth_Center"),
            "Ağız sabit idle döngüsüyle değil aktif konuşmacının sesiyle hareket etmeli.");
    }

    [Test]
    public void PreparationBagCamera_KeepsTheDropTargetAboveTheSubtitleSafeBand()
    {
        CinemachineCamera bagCamera = Find("CM_PreparationBag_Rebuild")
            ?.GetComponent<CinemachineCamera>();
        Bounds bag = CombinedBounds(Find("EmergencyBag_Open_Packing"));

        Assert.That(bagCamera, Is.Not.Null);
        Vector3 cameraLocalCenter = bagCamera.transform.InverseTransformPoint(bag.center);
        Assert.That(cameraLocalCenter.z, Is.GreaterThan(0f));

        float halfHeight = cameraLocalCenter.z *
                           Mathf.Tan(bagCamera.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
        float viewportY = 0.5f + cameraLocalCenter.y / (2f * halfHeight);
        Assert.That(viewportY, Is.GreaterThan(0.36f),
            "Çanta, alt diyalog panelinin arkasında kalmamalı; sürükleme hedefi orta oyun alanında görünmeli.");
    }

    [Test]
    public void OpeningConversation_StagesFamilyOutsideSofaAndUsesChildHeightThreeQuarterCamera()
    {
        Transform deniz = Find("Deniz_12");
        Transform can = Find("Can_8");
        Transform parent = Find("Anne_Ayse");
        Bounds sofa = CombinedBounds(Find("FamilySofa"));

        Assert.That(sofa.Intersects(CombinedBounds(deniz)), Is.False,
            "Deniz başlangıç konuşmasında koltuğun içine girmemeli.");
        Assert.That(sofa.Intersects(CombinedBounds(can)), Is.False,
            "Can başlangıç konuşmasında koltuğun içine girmemeli.");
        Assert.That(Vector3.Distance(deniz.position, can.position), Is.InRange(1.1f, 1.8f));
        Assert.That(Vector3.Distance(parent.position, deniz.position), Is.LessThan(1.7f));
        Assert.That(Vector3.Distance(parent.position, can.position), Is.LessThan(1.7f));

        Vector3 denizToParent = Vector3.ProjectOnPlane(parent.position - deniz.position, Vector3.up).normalized;
        Vector3 canToParent = Vector3.ProjectOnPlane(parent.position - can.position, Vector3.up).normalized;
        Vector3 parentToChildren = Vector3.ProjectOnPlane(
            (deniz.position + can.position) * 0.5f - parent.position,
            Vector3.up).normalized;
        Assert.That(Vector3.Dot(deniz.forward, denizToParent), Is.GreaterThan(0.9f));
        Assert.That(Vector3.Dot(can.forward, canToParent), Is.GreaterThan(0.9f));
        Assert.That(Vector3.Dot(parent.forward, parentToChildren), Is.GreaterThan(0.9f));

        Transform overview = Find("CM_PreparationOverview_Rebuild");
        CinemachinePositionComposer composer = overview.GetComponent<CinemachinePositionComposer>();
        Assert.That(composer, Is.Not.Null);
        Assert.That(composer.CameraDistance, Is.InRange(5.2f, 6.2f),
            "Başlangıç kamerası odayı tepeden gören uzak güvenlik kamerası gibi olmamalı.");
        Assert.That(overview.GetComponent<CinemachineCamera>().Lens.FieldOfView, Is.InRange(40f, 45f));
        CinemachineDeoccluder overviewDeoccluder = overview.GetComponent<CinemachineDeoccluder>();
        Assert.That(overviewDeoccluder, Is.Not.Null);
        Assert.That(overviewDeoccluder.AvoidObstacles.Enabled, Is.True);
        Assert.That(overviewDeoccluder.AvoidObstacles.UseFollowTarget.Enabled, Is.True);
        Assert.That(overviewDeoccluder.AvoidObstacles.CameraRadius, Is.GreaterThanOrEqualTo(0.28f));
        Assert.That(overviewDeoccluder.AvoidObstacles.DampingWhenOccluded, Is.Zero,
            "Takip kamerası fizik hacminden kademeli değil ilk karede çıkmalı.");
        Assert.That(overviewDeoccluder.AvoidObstacles.Strategy,
            Is.EqualTo(CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward));
        Assert.That(overview.position.y, Is.LessThan(5f),
            "Başlangıç kadrajı çocukların çok üstünde kalmamalı.");

        Vector3 parentToCamera = Vector3.ProjectOnPlane(
            overview.position - parent.position,
            Vector3.up).normalized;
        Assert.That(Vector3.Dot(parent.forward, parentToCamera), Is.GreaterThan(0.35f),
            "Konuşma kadrajında en azından annenin yüzü ön üç çeyrekten görünmeli.");
    }

    [Test]
    public void OpeningConversation_UsesCommonUIBindingsWithoutLegacySpeakerRuntime()
    {
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(ui, Is.Not.Null);
        Assert.That(ui.DialogueActorCount, Is.EqualTo(3));

        foreach (string indicatorName in new[]
        {
            "SpeakerIndicator_Deniz",
            "SpeakerIndicator_Can",
            "SpeakerIndicator_Anne"
        })
            Assert.That(Find(indicatorName), Is.Null, indicatorName + " ortak yüz sistemiyle kaldırılmalı.");

        Type directorType = typeof(StoryPreparationDirector);
        foreach (string removedField in new[]
        {
            "denizMouth", "canMouth", "parentMouth",
            "denizSpeakerIndicator", "canSpeakerIndicator", "parentSpeakerIndicator"
        })
            Assert.That(directorType.GetField(removedField, BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Null, removedField + " StoryPreparationDirector içinde çift sürüş yaratmamalı.");
    }

    [Test]
    public void PackedItemResults_StayOrganizedBelowTheBagRim()
    {
        Transform opening = Find("BagOpeningPoint");
        Transform packedRoot = Find("PackedItemResults");
        Bounds bagBounds = CombinedBoundsIncludingInactive(Find("EmergencyBag_Open_Packing").Find("KKTC_AuthoredVisual"));
        Assert.That(opening, Is.Not.Null);
        Assert.That(packedRoot, Is.Not.Null);
        Assert.That(Vector2.Distance(
                new Vector2(opening.position.x, opening.position.z),
                new Vector2(Find("EmergencyBag_Open_Packing").GetComponentsInChildren<Transform>(true).First(t=>t.name=="MouthAnchor").position.x,
                    Find("EmergencyBag_Open_Packing").GetComponentsInChildren<Transform>(true).First(t=>t.name=="MouthAnchor").position.z)),
            Is.LessThan(0.06f),
            "Bag opening and packed item positions must align with the visible bag.");

        Transform[] packedItems = packedRoot.Cast<Transform>().ToArray();
        Assert.That(packedItems, Has.Length.GreaterThanOrEqualTo(10));
        foreach (Transform packed in packedItems)
        {
            Vector3 offset = CombinedBoundsIncludingInactive(packed).center - opening.position;
            Assert.That(offset.x, Is.InRange(-0.15f, 0.15f), packed.name);
            Assert.That(offset.z, Is.InRange(-0.12f, 0.12f), packed.name);
            Assert.That(offset.y, Is.InRange(-0.33f, -0.025f),
                packed.name + " çantanın üstünde havada değil, ağız seviyesinin altında kalmalı.");
            Transform packedVisual=packed.Find("KKTC_AuthoredVisual");
            Transform loose=Find(packed.name.Replace("Packed_","WorldItem_"));
            Transform looseVisual=loose?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="KKTC_AuthoredVisual");
            if(packedVisual!=null && looseVisual!=null)
                Assert.That(packedVisual.lossyScale.x,Is.EqualTo(looseVisual.lossyScale.x).Within(.025f),
                    packed.name+" çantaya konunca minyatüre dönüşmemeli.");
        }
        Assert.That(packedItems.All(packed =>
                CombinedBoundsIncludingInactive(packed).max.y < opening.position.y - 0.02f),
            Is.True,
            "Packed item renderers must remain below the visible bag rim.");
    }

    [Test]
    public void DialogueVoiceBank_LeavesRevisedSubtitlesReadyForNewVoiceRecording()
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
        Assert.That(bindings.Length, Is.Zero, "Revised KKTC subtitles must not play mismatched prototype speech.");
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
    public void FoodCan_IsPalmSizedUprightAndKeepsACompactTouchMarker()
    {
        Transform food = Find("WorldItem_Food");
        Assert.That(food, Is.Not.Null);

        Bounds visualBounds = CombinedBoundsIncludingInactive(food.Find("Konserve"));
        Assert.That(visualBounds.size.x, Is.InRange(0.08f, 0.14f));
        Assert.That(visualBounds.size.y, Is.InRange(0.1f, 0.16f));
        Assert.That(visualBounds.size.z, Is.InRange(0.08f, 0.14f));
        Assert.That(visualBounds.size.y, Is.GreaterThan(visualBounds.size.x * 0.9f),
            "Konserve masada yatık bir varil gibi değil, dik ve avuç içi boyutunda durmalı.");

        StoryInteractable interaction = food.GetComponent<StoryInteractable>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(interaction.HighlightRoot, Is.Not.Null);
        Assert.That(interaction.HighlightRoot.transform.localScale.x, Is.LessThanOrEqualTo(0.31f));

        BoxCollider touchCollider = food.GetComponent<BoxCollider>();
        Assert.That(touchCollider, Is.Not.Null);
        bool wasActive = food.gameObject.activeSelf;
        food.gameObject.SetActive(true);
        try
        {
            Assert.That(touchCollider.bounds.size.x, Is.GreaterThanOrEqualTo(0.18f),
                "Küçülen konserve mobil ekranda hâlâ rahat seçilebilmeli.");
        }
        finally
        {
            food.gameObject.SetActive(wasActive);
        }
    }

    [Test]
    public void NonSignalPackingProps_RestOnTheVisiblePreparationTableSurface()
    {
        float tableSurfaceY = CombinedBounds(Find("SafeTable_Visual")).max.y;
        StoryPreparationItem[] items = Object.FindObjectsByType<StoryPreparationItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (StoryPreparationItem item in items.Where(candidate =>
                     candidate.Category != StoryPreparationCategory.Signal))
        {
            Transform visual = item.Interactable.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "KKTC_AuthoredVisual");
            Assert.That(visual, Is.Not.Null, item.ItemId);
            Bounds bounds = CombinedBoundsIncludingInactive(visual);
            Assert.That(bounds.min.y, Is.EqualTo(tableSurfaceY + 0.004f).Within(0.006f),
                item.ItemId + " masanın üstünde yüzmemeli veya tablanın içine girmemeli.");
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
        string[] answerGhostNames =
        {
            "PlanSlotAnswerGhost_1",
            "PlanSlotAnswerGhost_2",
            "PlanSlotAnswerGhost_3"
        };
        for (int cardIndex = 0; cardIndex < planCards.Length; cardIndex++)
        {
            StoryInteractable interaction = planCards[cardIndex];
            bool hidesOwnAnswerGhost = Enumerable.Range(0, interaction.OnInteracted.GetPersistentEventCount())
                .Any(eventIndex =>
                {
                    Object target = interaction.OnInteracted.GetPersistentTarget(eventIndex);
                    return target != null &&
                           target.name == answerGhostNames[cardIndex] &&
                           interaction.OnInteracted.GetPersistentMethodName(eventIndex) == "SetActive";
                });
            Assert.That(hidesOwnAnswerGhost, Is.True,
                answerGhostNames[cardIndex] + " kart yerleşince kapanmalı; iki metin üst üste kalmamalı.");
        }
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
                   GetPrivate<float>(draggable, "magneticSnapResponse") <= 9f &&
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
            Assert.That(snapAnimation.clip.length, Is.GreaterThanOrEqualTo(0.9f),
                "Kartın son socket oturuşu sert tek karelik snap değil, yumuşak settle olmalı.");
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
        for (int index = 1; index <= 3; index++)
        {
            Transform answerGhost = Find("PlanSlotAnswerGhost_" + index);
            Assert.That(answerGhost.GetComponentsInChildren<TextMeshPro>(true), Is.Empty,
                "Manyetik yuvada ikinci bir cevap metni bulunmamalı; kart yaklaşınca yazılar üst üste biner.");
            Assert.That(Find("PlanSlotTitle_" + index).GetComponent<TextMeshPro>().text,
                Is.Not.Empty, "Doğru yer kategori başlığından anlaşılmalı.");
        }
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
        Assert.That(discoveries[0].InteractionGesture,
            Is.EqualTo(StoryInteractionGesture.SwipeDiagonalDownRight));
        Transform signalGestureIcon = discoveries[0].HighlightRoot.transform.Find("ObjectiveGestureIcon");
        Assert.That(signalGestureIcon, Is.Not.Null);
        Assert.That(Mathf.DeltaAngle(signalGestureIcon.localEulerAngles.z, 45f), Is.Zero.Within(0.1f),
            "Çekmece göstergesi ekrandaki sağ-alt çekme yönünü göstermeli.");
        Assert.That(discoveries.Skip(1).All(interaction =>
            interaction.InteractionGesture == StoryInteractionGesture.SwipeHorizontal), Is.True);
        Assert.That(discoveries[1].gameObject.name,
            Is.EqualTo("SM_Prop_Kitchen_Counter_01_Door_01"),
            "Mutfak keşfi tüm dolabın belirsiz collider'ına değil, görünen sol alt kapağa bağlı olmalı.");
        Assert.That(discoveries[1].Prompt,
            Is.EqualTo("Sol alt dolap kapağını tutup sağa doğru kaydır"));
        Bounds foodDoorBounds = CombinedBoundsIncludingInactive(discoveries[1].transform);
        Assert.That(discoveries[1].HighlightRoot.transform.position.y,
            Is.InRange(foodDoorBounds.center.y + 0.14f, foodDoorBounds.center.y + 0.22f));
        Assert.That(discoveries[1].HighlightRoot.transform.position.x,
            Is.LessThan(foodDoorBounds.min.x),
            "Yatay sürükleme göstergesi ilk yardım kutusunun arkasında değil, kapak yüzünün önünde görünmeli.");
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
        Transform signalDrawerSlide = signalDrawer.parent;
        Assert.That(signalDrawerSlide, Is.Not.Null);
        Assert.That(signalDrawerSlide.name, Is.EqualTo("SignalDrawerSlide"));
        Assert.That(signalDrawerSlide.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(signalDrawer.localPosition.y, Is.EqualTo(0.3462006f).Within(0.0001f),
            "The authored drawer height must stay untouched.");
        Animation drawerAnimation = signalDrawerSlide.GetComponent<Animation>();
        Assert.That(drawerAnimation, Is.Not.Null);
        Assert.That(drawerAnimation.playAutomatically, Is.True);
        Assert.That(AssetDatabase.GetAssetPath(drawerAnimation.clip),
            Is.EqualTo("Assets/Story/Generated/Animations/Chapters/Story01_SignalDrawerOpen.anim"));
        EditorCurveBinding[] drawerBindings = AnimationUtility.GetCurveBindings(drawerAnimation.clip);
        Assert.That(drawerBindings.Select(binding => binding.propertyName),
            Is.EquivalentTo(new[]
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y",
                "m_LocalPosition.z"
            }));
        foreach (EditorCurveBinding fixedAxis in drawerBindings.Where(binding =>
                     !binding.propertyName.EndsWith("Position.z", StringComparison.Ordinal)))
        {
            AnimationCurve fixedCurve = AnimationUtility.GetEditorCurve(drawerAnimation.clip, fixedAxis);
            Assert.That(fixedCurve.keys.All(key => Mathf.Abs(key.value) < 0.0001f), Is.True,
                "The drawer pivot's X/Y must remain fixed at zero.");
        }
        EditorCurveBinding drawerZBinding = drawerBindings.Single(binding =>
            binding.propertyName.EndsWith("Position.z", StringComparison.Ordinal));
        AnimationCurve drawerCurve = AnimationUtility.GetEditorCurve(drawerAnimation.clip, drawerZBinding);
        Assert.That(drawerCurve.Evaluate(0f), Is.Zero.Within(0.0001f));
        Assert.That(drawerCurve.Evaluate(drawerCurve.keys[^1].time), Is.EqualTo(0.145f).Within(0.0001f),
            "Çekmece rayından kopacak kadar dışarı taşmamalı.");
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
        Animation kitchenOpenAnimation = Find("KitchenCabinetDoorLeft").GetComponent<Animation>();
        Assert.That(kitchenOpenAnimation, Is.Not.Null);
        Assert.That(kitchenOpenAnimation.playAutomatically, Is.True);
        Assert.That(AssetDatabase.GetAssetPath(kitchenOpenAnimation.clip),
            Is.EqualTo("Assets/Story/Generated/Animations/Chapters/Story01_KitchenCabinetOpen.anim"));
        Assert.That(AnimationUtility.GetCurveBindings(kitchenOpenAnimation.clip)
                .Select(binding => binding.path)
                .Distinct(),
            Is.EquivalentTo(new[]
            {
                "SM_Prop_Kitchen_Counter_01_Door_01",
                "SM_Prop_Kitchen_Counter_01_Door_02"
            }), "İki gerçek kapak tek karede kaybolmak yerine yana açılmalı.");
        Assert.That(Find("AidCabinetOpenDoor"), Is.Not.Null);
        Assert.That(Find("WarmthChestOpenDoor"), Is.Not.Null);
    }

    [Test]
    public void PlanCardTable_ContainsAllCardsAndNotesOnItsPhysicalSurface()
    {
        Bounds table = CombinedBounds(Find("FamilyPlanCardTable"));
        Assert.That(table.size.x, Is.EqualTo(1.12f).Within(0.03f));
        Assert.That(table.size.y, Is.EqualTo(0.72f).Within(0.02f));
        Assert.That(table.size.z, Is.EqualTo(1.92f).Within(0.03f));

        string[] cardNames =
        {
            "FamilyMeetingPointCard_Drag",
            "MelekContactCard_Drag",
            "CanWhistleRoleCard_Drag"
        };
        foreach (string cardName in cardNames)
        {
            Collider cardCollider = Find(cardName)?.GetComponent<Collider>();
            Assert.That(cardCollider, Is.Not.Null, cardName);
            Bounds card = cardCollider.bounds;
            Assert.That(card.min.x, Is.GreaterThanOrEqualTo(table.min.x + 0.08f), cardName);
            Assert.That(card.max.x, Is.LessThanOrEqualTo(table.max.x - 0.08f), cardName);
            Assert.That(card.min.z, Is.GreaterThanOrEqualTo(table.min.z + 0.10f), cardName);
            Assert.That(card.max.z, Is.LessThanOrEqualTo(table.max.z - 0.10f), cardName);
        }

        Bounds notes = CombinedBounds(Find("Story01_PlanTableNotes"));
        Assert.That(notes.min.x, Is.GreaterThanOrEqualTo(table.min.x + 0.015f));
        Assert.That(notes.max.x, Is.LessThanOrEqualTo(table.max.x - 0.015f));
        Assert.That(notes.min.z, Is.GreaterThanOrEqualTo(table.min.z + 0.015f));
        Assert.That(notes.max.z, Is.LessThanOrEqualTo(table.max.z - 0.015f));
        Assert.That(cardNames.All(cardName =>
        {
            Bounds card = Find(cardName).GetComponent<Collider>().bounds;
            bool overlapsX = notes.max.x > card.min.x + 0.01f &&
                             card.max.x > notes.min.x + 0.01f;
            bool overlapsZ = notes.max.z > card.min.z + 0.01f &&
                             card.max.z > notes.min.z + 0.01f;
            return !overlapsX || !overlapsZ;
        }), Is.True, "Dekoratif not kâğıdı etkileşim kartlarının altına girmemeli.");
    }

    [Test]
    public void FamilyPlanBoard_UsesAuthoredPanelsInsteadOfVisiblePrimitiveBoxes()
    {
        SpriteRenderer frame = Find("FamilyPlanBoardFrame_Kenney").GetComponent<SpriteRenderer>();
        Assert.That(frame, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(frame.sprite),
            Is.EqualTo("Assets/Story/UI/ThirdParty/KenneyUIAdventure/panel_brown.png"));
        Assert.That(frame.drawMode, Is.EqualTo(SpriteDrawMode.Sliced));

        SpriteRenderer header = Find("PlanHeaderRibbon").GetComponent<SpriteRenderer>();
        Assert.That(header, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(header.sprite),
            Is.EqualTo("Assets/Story/UI/ThirdParty/KenneyUIAdventure/button_brown.png"));
        Assert.That(Find("PlanHeader").GetComponent<Renderer>().enabled, Is.False,
            "Pano başlığında görünür primitive kutu kalmamalı.");

        for (int index = 1; index <= 3; index++)
        {
            Assert.That(Find("PlanSlot_" + index).GetComponent<Renderer>().enabled, Is.False);
            Assert.That(Find("PlanSlotHeader_" + index).GetComponent<Renderer>().enabled, Is.False);
            Assert.That(Find("PlanCardGhostSocket_" + index).GetComponent<Renderer>().enabled, Is.False);
            Assert.That(Find("PlanCardGhostInset_" + index).GetComponent<Renderer>().enabled, Is.False);

            SpriteRenderer cardPanel = Find("PlanSlotPaper_" + index).GetComponent<SpriteRenderer>();
            SpriteRenderer titlePanel = Find("PlanSlotHeaderPaper_" + index).GetComponent<SpriteRenderer>();
            SpriteRenderer ghostPanel = Find("PlanCardGhostPaper_" + index).GetComponent<SpriteRenderer>();
            Assert.That(AssetDatabase.GetAssetPath(cardPanel.sprite),
                Is.EqualTo("Assets/Story/UI/ThirdParty/CartoonUIPack/basic white/pop up window.png"));
            Assert.That(AssetDatabase.GetAssetPath(titlePanel.sprite),
                Is.EqualTo("Assets/Story/UI/ThirdParty/KenneyUIAdventure/button_grey.png"));
            Assert.That(ghostPanel.color.a, Is.InRange(0.65f, 0.8f),
                "Kart yuvası sert yeşil kutu değil, yumuşak yarı saydam ghost panel olmalı.");
        }

        string[] tablePaperNames =
        {
            "MeetingPointCardPaper",
            "MelekContactCard_Drag_Paper",
            "CanWhistleRoleCard_Drag_Paper"
        };
        foreach (string paperName in tablePaperNames)
        {
            SpriteRenderer paper = Find(paperName).GetComponent<SpriteRenderer>();
            Assert.That(paper, Is.Not.Null,
                "Masadaki kartlar bozuk dekor mesh'i yerine standart panel sprite kullanmalı.");
            Assert.That(AssetDatabase.GetAssetPath(paper.sprite),
                Is.EqualTo("Assets/Story/UI/ThirdParty/CartoonUIPack/basic white/pop up window.png"));
            Assert.That(Quaternion.Angle(
                    paper.transform.localRotation,
                    Quaternion.Euler(90f, 0f, 0f)),
                Is.LessThan(0.1f),
                "Üç masa kartı da masaya aynı yönde ve düz oturmalı.");
        }
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
    public void Story01ExtendedRightWall_HasContinuousSkirting()
    {
        Transform wall = Find("RightWall_Story01");
        Transform skirting = Find("Skirting_Right");

        Assert.That(wall, Is.Not.Null);
        Assert.That(skirting, Is.Not.Null);
        Assert.That(skirting.localPosition.z, Is.EqualTo(wall.localPosition.z).Within(0.01f));
        Assert.That(skirting.localScale.z, Is.GreaterThanOrEqualTo(wall.localScale.z - 0.3f),
            "Story 01'de uzatılan sağ duvarın süpürgeliği yarım kalmamalı.");
        Assert.That(skirting.localPosition.y, Is.EqualTo(0.18f).Within(0.01f));
    }

    [Test]
    public void InteriorCameras_DoNotExposeSkyAboveTheClosedRoomWalls()
    {
        Transform leftWall = Find("LeftWall");
        Transform rightWall = Find("RightWall_Story01");
        Transform backLeft = Find("KKTC_WindowWall_West");
        Transform backRight = Find("BackWall_Right");
        Transform planCamera = Find("CM_PreparationFamilyPlan_Rebuild");

        Assert.That(new[] { leftWall, rightWall, backLeft, backRight, planCamera },
            Has.None.Null);
        foreach (Transform wall in new[] { leftWall, rightWall, backLeft, backRight })
        {
            Bounds wallBounds = CombinedBounds(wall);
            Assert.That(wallBounds.max.y, Is.GreaterThanOrEqualTo(ExpectedCeilingBottomY-0.01f),
                wall.name + " iç mekân portre kadrajında skybox göstermeyecek kadar yüksek olmalı.");
            Assert.That(wallBounds.min.y, Is.LessThanOrEqualTo(0.01f));
        }

        Assert.That(CombinedBounds(leftWall).max.y, Is.GreaterThan(planCamera.position.y + 2.4f),
            "Aile planı kamerasının üst kadrajı sol duvarla dolmalı; dışarı görünmemeli.");
    }

    [Test]
    public void InteriorArtPass_ClosesTheRoomAndBuildsDistinctLivedInZones()
    {
        Transform art = Find("PreparationInteriorArtPass");
        Transform ceiling = Find("Story01_CeilingMain");

        Assert.That(art, Is.Not.Null);
        Assert.That(ceiling, Is.Not.Null);
        Assert.That(ceiling.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Görsel tavan kamera ve NavMesh collider'ı olmamalı.");
        Bounds ceilingBounds = CombinedBounds(ceiling);
        Assert.That(ceilingBounds.min.y, Is.InRange(4.01f, 4.05f));
        Assert.That(ceilingBounds.size.x, Is.GreaterThan(9.5f));
        Assert.That(ceilingBounds.size.z, Is.GreaterThan(11f));

        Bounds planFrameTopBounds = CombinedBounds(Find("FamilyPlanBoard"));
        Bounds crownBounds = CombinedBounds(Find("Story01_Crown_Left"));
        Assert.That(ceilingBounds.min.y - planFrameTopBounds.max.y, Is.GreaterThanOrEqualTo(0.85f),
            "Aile planı çerçevesi yükseltilmiş tavana yapışmamalı.");
        Assert.That(crownBounds.min.y - planFrameTopBounds.max.y, Is.GreaterThanOrEqualTo(0.6f),
            "Pano ile taç çıtası arasında okunur bir üst-duvar bandı kalmalı.");

        string[] authoredZones =
        {
            "Story01_FamilyPlanFeatureWall",
            "Story01_KitchenZone",
            "Story01_LoungeZone",
            "Story01_EntryZone",
            "Story01_WallGallery",
            "Story01_PracticalLighting"
        };
        Assert.That(authoredZones.All(name => Find(name) != null), Is.True);
        Assert.That(new[]
        {
            "Story01_FamilyPlanBackdrop",
            "Story01_CeilingBeam_Spine",
            "Story01_KitchenFridge",
            "Story01_KitchenUpperStorage",
            "Story01_KitchenSinkBasin",
            "Story01_KitchenFaucetSpout",
            "Story01_KitchenOvenDoor",
            "Story01_KitchenCooktop",
            "Story01_KitchenRangeHood",
            "Story01_UnderCabinetWarmStrip",
            "Story01_EntryPlant",
            "Story01_SofaTableLamp",
            "Story01_TableLampShadeBody",
            "Story01_PendantFixture",
            "Story01_PendantShadeBody",
            "Story01_PendantWarmBulb",
            "Story01_Gallery_FamilyMemory"
        }.All(name => Find(name) != null), Is.True,
            "Mutfak, salon ve giriş yalnız trim değil okunur ev eşyaları taşımalı.");
        Assert.That(Find("Story01_Gallery_SafetyPlan"), Is.Null,
            "Aile afet panosunun arkasında yarım görünen eski portre kalmamalı.");
        Assert.That(Find("UtensilCrock"), Is.Null,
            "Mutfak dekoru prototip silindirlerden değil gerçek prop prefabından gelmeli.");
        Assert.That(Find("KitchenCanister"), Is.Null,
            "Etkileşimli konserveyle karışan dekoratif silindir tezgahta kalmamalı.");
        Assert.That(Find("LampShade"), Is.Null,
            "Eski tek parça koyu silindir abajur sahnede kalmamalı.");
        Assert.That(Find("Story01_KitchenUtensils"), Is.Null,
            "Oklava gibi okunan üç ayaklı mutfak dekoru tezgahta kalmamalı.");
        Assert.That(Find("Story01_KitchenToaster"), Is.Null);
        Assert.That(Find("Story01_KitchenMug"), Is.Null,
            "Etkileşimli hikâye nesnelerinin yüzeyi rastgele kenar prop'larıyla daraltılmamalı.");

        Renderer[] artRenderers = art.GetComponentsInChildren<Renderer>(true);
        Assert.That(artRenderers.Length, Is.GreaterThanOrEqualTo(45),
            "Art-pass portre kadrajlarında boş duvar bırakmayacak kadar katmanlı olmalı.");
        Assert.That(art.GetComponentsInChildren<StoryInteractable>(true), Is.Empty);
        Assert.That(art.GetComponentsInChildren<DraggableItem>(true), Is.Empty);
        Assert.That(art.GetComponentsInChildren<BagDropZone>(true), Is.Empty,
            "Dekor etkileşim sahipliğini çalmamalı.");

        foreach (string blockerName in new[]
                 {
                     "Story01_KitchenFridge",
                     "Story01_KitchenBaseRunBlocker",
                     "Story01_EntryShoeConsoleBlocker"
                 })
        {
            BoxCollider blocker = Find(blockerName)?.GetComponent<BoxCollider>();
            Assert.That(blocker, Is.Not.Null, blockerName + " görünür statik blocker");
            Assert.That(blocker.isTrigger, Is.False);
            Assert.That(blocker.bounds.size.y, Is.GreaterThan(0.6f));
        }

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.That(cameras.All(camera => camera.transform.position.y < ceilingBounds.min.y - 0.1f), Is.True,
            "Hiçbir Story 01 sanal kamerası kapalı tavanın üstünde kalmamalı.");
    }

    [Test]
    public void KitchenZone_IsAContinuousFunctionalWorkWallInsteadOfOneLonelyCabinet()
    {
        Transform kitchen = Find("Story01_KitchenZone");
        Transform counter = Find("Story01_KitchenContinuousCounter");
        Transform baseRun = Find("Story01_KitchenBaseRunBlocker");
        Transform sink = Find("Story01_KitchenSinkBasin");
        Transform faucet = Find("Story01_KitchenFaucetStem");
        Transform oven = Find("Story01_KitchenOvenBody");
        Transform pullOut = Find("Story01_KitchenPullOutBase");
        Transform cooktop = Find("Story01_KitchenCooktop");
        Transform hood = Find("Story01_KitchenRangeHood");
        Transform fridge = Find("Story01_KitchenFridge");
        Transform endReturn = Find("Story01_KitchenEndReturnWall");
        Transform discoveryCabinet = Find("KitchenLowCabinet");
        Assert.That(new[]
        {
            kitchen, counter, baseRun, sink, faucet, oven, pullOut, cooktop, hood, fridge, endReturn,
            discoveryCabinet
        }, Has.None.Null);

        Bounds counterBounds = CombinedBounds(counter);
        Bounds baseBounds = CombinedBounds(baseRun);
        Bounds sinkBounds = CombinedBounds(sink);
        Bounds ovenBounds = CombinedBounds(oven);
        Bounds pullOutBounds = CombinedBounds(pullOut);
        Bounds fridgeBounds = CombinedBounds(fridge);
        Bounds discoveryBounds = CombinedBounds(discoveryCabinet);
        Assert.That(counterBounds.size.z, Is.GreaterThanOrEqualTo(3.8f),
            "Mutfak tezgâhı tek komodin genişliğinde kalmamalı.");
        Assert.That(baseBounds.min.y, Is.EqualTo(0f).Within(0.01f));
        Assert.That(baseBounds.max.y, Is.LessThan(discoveryBounds.max.y - 0.002f),
            "Arka dolgu bloğu eski tezgâh yüzeyiyle aynı düzlemde bitmemeli.");
        Assert.That(counterBounds.min.y, Is.GreaterThan(discoveryBounds.max.y + 0.002f),
            "Kesintisiz tezgâh eski dolap yüzeyiyle coplanar kalıp z-fighting üretmemeli.");
        Assert.That(counterBounds.size.y, Is.InRange(0.035f, 0.045f));
        Assert.That(sinkBounds.min.y, Is.GreaterThanOrEqualTo(counterBounds.max.y - 0.04f));
        Assert.That(CombinedBounds(cooktop).min.y, Is.GreaterThanOrEqualTo(counterBounds.max.y - 0.04f));
        Assert.That(ovenBounds.min.y, Is.EqualTo(0f).Within(0.01f));

        Assert.That(Mathf.Abs(discoveryBounds.center.z - (-1.82f)), Is.LessThan(0.08f));
        Assert.That(Mathf.Abs(ovenBounds.center.z - (-3.08f)), Is.LessThan(0.05f));
        Assert.That(fridgeBounds.center.z, Is.LessThan(-3.8f));
        Assert.That(CombinedBounds(endReturn).max.y,
            Is.GreaterThanOrEqualTo(ExpectedCeilingBottomY - 0.03f),
            "Buzdolabı yanındaki dönüş duvarı tavana kadar çıkıp skybox yarığını kapatmalı.");
        Assert.That(pullOutBounds.min.z - ovenBounds.max.z, Is.LessThanOrEqualTo(0.03f),
            "Fırın ile ince çekmece arasında boş şerit kalmamalı.");
        Assert.That(discoveryBounds.min.z - pullOutBounds.max.z, Is.LessThanOrEqualTo(0.03f),
            "Görev dolabı ile ince çekmece arasında boş şerit kalmamalı.");

        Assert.That(new[]
        {
            "Story01_KitchenUpperSink",
            "Story01_KitchenUpperPantry",
            "Story01_KitchenHoodCanopy",
            "Story01_KitchenOvenWindow",
            "Story01_KitchenOvenHandle",
            "Story01_KitchenPullOutFront",
            "Story01_KitchenPullOutHandle",
            "Story01_KitchenCuttingBoard"
        }.All(name => Find(name) != null), Is.True);
        Assert.That(Enumerable.Range(0, 4).All(index => Find("Story01_KitchenBurner_" + index) != null),
            Is.True,
            "Ocakta dört ayrı göz görünmeli.");
        Assert.That(kitchen.GetComponentsInChildren<StoryInteractable>(true), Is.Empty,
            "Yeni mutfak görselleri görev dolabının input sahipliğini çalmamalı.");

        foreach (string propName in new[]
                 {
                     "AidServiceCase",
                     "AidServiceCase_Removed",
                     "BandageSeal_Unchecked",
                     "Story01_KitchenCuttingBoard"
                 })
        {
            Transform prop = Find(propName);
            Assert.That(prop, Is.Not.Null);
            Assert.That(CombinedBoundsIncludingInactive(prop).min.y,
                Is.GreaterThanOrEqualTo(counterBounds.max.y - 0.002f),
                propName + " yükseltilmiş tezgâhın içine gömülmemeli.");
        }

        Bounds verticalJoint = CombinedBounds(Find("Story01_KitchenTileJoint_0"));
        Bounds horizontalJoint = CombinedBounds(Find("Story01_KitchenTileRow_33"));
        Assert.That(Mathf.Abs(verticalJoint.min.x - horizontalJoint.min.x),
            Is.GreaterThanOrEqualTo(0.003f),
            "Kesişen backsplash derzlerinin ön yüzleri coplanar olmamalı.");
    }

    [Test]
    public void EntryWall_IsOneGroundedMudroomCompositionWithoutFloatingNoveltyProps()
    {
        Transform entry = Find("Story01_EntryZone");
        Transform panel = Find("Story01_EntryMudroomPanel");
        Transform console = Find("Story01_EntryShoeConsole");
        Transform consoleTop = Find("Story01_EntryShoeConsoleTop");
        Transform mail = Find("Story01_EntryMailTray");
        Transform rack = Find("Story01_EntryCoatRack");
        Transform plant = Find("Story01_EntryPlant");

        Assert.That(new[] { entry, panel, console, consoleTop, mail, rack, plant }, Has.None.Null);
        Assert.That(Find("Story01_EntryKeyShelf"), Is.Null,
            "Desteksiz eski anahtar rafı giriş duvarında kalmamalı.");
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(transform => transform.name.StartsWith("Story01_CoatHook_", StringComparison.Ordinal)), Is.False,
            "Renkli kürelerden oluşan eski askılar kaldırılmalı.");

        Bounds consoleBounds = CombinedBounds(console);
        Assert.That(panel.position.z, Is.EqualTo(3.1f).Within(0.01f),
            "Askılık ve ayakkabılık sağa kaydırılmış duvar kompozisyonunun merkezinde kalmalı.");
        Assert.That(consoleBounds.min.y, Is.EqualTo(0f).Within(0.01f),
            "Ayakkabı dolabı zemine oturmalı.");
        Assert.That(consoleBounds.size.y, Is.InRange(0.72f, 0.86f));
        Assert.That(consoleBounds.size.z, Is.InRange(1.3f, 1.5f));
        BoxCollider consoleBlocker = Find("Story01_EntryShoeConsoleBlocker")?.GetComponent<BoxCollider>();
        Assert.That(consoleBlocker, Is.Not.Null);
        Assert.That(consoleBlocker.enabled && !consoleBlocker.isTrigger, Is.True,
            "Karakter ayakkabı dolabının içinden veya üstünden yürümemeli.");

        Bounds topBounds = CombinedBounds(consoleTop);
        Bounds mailBounds = CombinedBounds(mail);
        Assert.That(mailBounds.min.y, Is.EqualTo(topBounds.max.y).Within(0.012f),
            "Giriş postası dolap tablasına temas etmeli.");
        Assert.That(mailBounds.min.x, Is.GreaterThanOrEqualTo(topBounds.min.x - 0.01f));
        Assert.That(mailBounds.max.x, Is.LessThanOrEqualTo(topBounds.max.x + 0.01f));
        Assert.That(mailBounds.min.z, Is.GreaterThanOrEqualTo(topBounds.min.z - 0.01f));
        Assert.That(mailBounds.max.z, Is.LessThanOrEqualTo(topBounds.max.z + 0.01f));

        Assert.That(new[]
        {
            "Story01_EntryCoatRackBackplate",
            "Story01_EntryCoatRackShelf",
            "Story01_EntryCoatRackBracketVertical_302",
            "Story01_EntryCoatRackBracketArm_382",
            "Story01_EntryHookBase_0",
            "Story01_EntryHookArm_1",
            "Story01_EntryHookUpturn_2"
        }.All(name => Find(name) != null), Is.True,
            "Duvar askısı arkalık, tabla, konsol ve gerçek L kancalardan oluşmalı.");
        Assert.That(rack.GetComponentsInChildren<MeshFilter>(true)
            .All(filter => filter.sharedMesh != null && filter.sharedMesh.name == "Cube"), Is.True,
            "Giriş askıları renkli küre değil ince metal profil olarak okunmalı.");

        Bounds plantBounds = CombinedBounds(plant);
        Assert.That(plantBounds.min.y, Is.EqualTo(topBounds.max.y).Within(0.012f),
            "Bitki zeminde tek başına durmak yerine giriş konsolunun tablasına oturmalı.");
        Assert.That(plantBounds.min.x, Is.GreaterThanOrEqualTo(topBounds.min.x - 0.01f));
        Assert.That(plantBounds.max.x, Is.LessThanOrEqualTo(topBounds.max.x + 0.01f));
        Assert.That(plantBounds.min.z, Is.GreaterThanOrEqualTo(topBounds.min.z - 0.01f));
        Assert.That(plantBounds.max.z, Is.LessThanOrEqualTo(topBounds.max.z + 0.01f));
        Assert.That(plantBounds.size.y, Is.InRange(0.32f, 0.4f));
        Assert.That(plant.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Masa üstü bitkisi dokunma ışınını veya NavMesh'i çalmamalı.");
    }

    [Test]
    public void Wardrobe_UsesAnAnchoredBuiltInSkinInsteadOfTheToyClosetVisual()
    {
        Transform wardrobe = Find("Wardrobe_Unsecured");
        Transform skin = Find("Story01_WardrobeBuiltInSkin");
        Assert.That(wardrobe, Is.Not.Null);
        Assert.That(skin, Is.Not.Null);
        Assert.That(skin.IsChildOf(wardrobe), Is.True);

        Renderer[] importedRenderers = wardrobe.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !renderer.transform.IsChildOf(skin))
            .ToArray();
        Assert.That(importedRenderers, Is.Not.Empty);
        Assert.That(importedRenderers.All(renderer => !renderer.enabled), Is.True,
            "Yuvarlak turkuaz-pembe oyuncak dolap görseli tamamen gizlenmeli.");

        Bounds skinBounds = CombinedBounds(skin);
        Assert.That(skinBounds.min.y, Is.EqualTo(0f).Within(0.015f));
        Assert.That(skinBounds.size.x, Is.InRange(1.24f, 1.55f));
        Assert.That(skinBounds.size.y, Is.InRange(1.95f, 2.2f));
        Assert.That(skinBounds.size.z, Is.InRange(0.55f, 0.78f));
        Assert.That(skinBounds.max.x, Is.LessThanOrEqualTo(1.25f),
            "Dolap arka duvardaki geçit ağzına taşmamalı.");
        Assert.That(new[]
        {
            "Story01_WardrobePlinth",
            "Story01_WardrobeCornice",
            "Story01_WardrobeDoor_Left_UpperInset",
            "Story01_WardrobeDoor_Right_LowerInset",
            "Story01_WardrobeHandle_Left",
            "Story01_WardrobeHandle_Right"
        }.All(name => Find(name) != null), Is.True);
        Assert.That(skin.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Görsel paneller ayrı ayrı collider üretmemeli.");

        Collider[] enabledBlockers = wardrobe.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.enabled)
            .ToArray();
        Assert.That(enabledBlockers, Has.Length.EqualTo(1));
        Assert.That(enabledBlockers[0].transform, Is.EqualTo(wardrobe));
        Assert.That(enabledBlockers[0].isTrigger, Is.False);
        Bounds blockerBounds = enabledBlockers[0].bounds;
        Assert.That(Vector3.Distance(blockerBounds.min, skinBounds.min), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(blockerBounds.max, skinBounds.max), Is.LessThan(0.01f));
    }

    [Test]
    public void ShelfContents_AreReadablePropsRestingOnTheirActualBoards()
    {
        Transform shelf = Find("Shelf_Unsecured");
        Transform contents = Find("Story01_ShelfCuratedContents");
        Assert.That(shelf, Is.Not.Null);
        Assert.That(contents, Is.Not.Null);
        Assert.That(contents.IsChildOf(shelf), Is.True);
        Assert.That(shelf.position.z, Is.EqualTo(1.23f).Within(0.01f),
            "Uzun raf sağ duvar kompozisyonuyla birlikte ekran sağına kaymalı.");

        Transform[] legacyBooks = shelf.GetComponentsInChildren<Transform>(true)
            .Where(transform => transform.name.StartsWith("Book_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(legacyBooks, Is.Not.Empty);
        Assert.That(legacyBooks.All(book => !book.gameObject.activeSelf), Is.True,
            "Tabla merkezine gömülen eski kitap kökleri görünmemeli.");

        (string prop, string board)[] placements =
        {
            ("Story01_ShelfPlant", "ShelfBoard_31"),
            ("Story01_ShelfBooks_Lower", "ShelfBoard_51"),
            ("Story01_ShelfStack_Lower", "ShelfBoard_51"),
            ("Story01_ShelfToyCar", "ShelfBoard_70"),
            ("Story01_ShelfBooks_Upper", "ShelfBoard_70"),
            ("Story01_ShelfStack_Top", "ShelfBoard_87")
        };
        foreach ((string propName, string boardName) in placements)
        {
            Transform prop = Find(propName);
            Transform board = shelf.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == boardName);
            Assert.That(prop, Is.Not.Null, propName);
            Assert.That(board, Is.Not.Null, boardName);
            Bounds propBounds = CombinedBounds(prop);
            Bounds boardBounds = CombinedBounds(board);
            Assert.That(propBounds.min.y, Is.EqualTo(boardBounds.max.y + 0.006f).Within(0.012f),
                propName + " tabla yüzeyine oturmalı; tahtanın içinde veya havada olmamalı.");
            Assert.That(propBounds.min.x, Is.GreaterThanOrEqualTo(boardBounds.min.x - 0.015f), propName);
            Assert.That(propBounds.max.x, Is.LessThanOrEqualTo(boardBounds.max.x + 0.015f), propName);
            Assert.That(propBounds.min.z, Is.GreaterThanOrEqualTo(boardBounds.min.z - 0.015f), propName);
            Assert.That(propBounds.max.z, Is.LessThanOrEqualTo(boardBounds.max.z + 0.015f), propName);
            Assert.That(propBounds.size.y, Is.GreaterThan(0.1f),
                propName + " okunamayacak kadar küçük olmamalı.");
        }
        Assert.That(contents.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Raf dekoru dokunma ışınını veya NavMesh'i çalmamalı.");
    }

    [Test]
    public void ExitBagConsole_IsShallowAlongTheWallAndSupportsTheFinalBag()
    {
        Transform exitShelf = Find("ExitBagShelf");
        Transform skin = Find("Story01_ExitConsoleSkin");
        Transform top = Find("Story01_ExitConsoleTop");
        Transform finalBag = Find("EmergencyBag_ExitShelf");
        Transform dropZone = Find("ExitShelfBagDropZone");
        Assert.That(new[] { exitShelf, skin, top, finalBag, dropZone }, Has.None.Null);
        Assert.That(skin.IsChildOf(exitShelf), Is.True);

        Renderer[] importedRenderers = exitShelf.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !renderer.transform.IsChildOf(skin))
            .ToArray();
        Assert.That(importedRenderers, Is.Not.Empty);
        Assert.That(importedRenderers.All(renderer => !renderer.enabled), Is.True,
            "Duvara dik uzayan eski yuvarlak mutfak dolabı görseli gizlenmeli.");

        Bounds skinBounds = CombinedBounds(skin);
        Assert.That(skinBounds.center.z, Is.EqualTo(5.03f).Within(0.015f),
            "Çıkış konsolu duvar kompozisyonuyla birlikte ekran sağına kaymalı.");
        Assert.That(skinBounds.min.y, Is.EqualTo(0f).Within(0.01f));
        Assert.That(skinBounds.size.x, Is.InRange(0.5f, 0.58f),
            "Çıkış konsolu dolaşım alanına doğru derinleşmemeli.");
        Assert.That(skinBounds.size.y, Is.InRange(0.85f, 0.88f));
        Assert.That(skinBounds.size.z, Is.InRange(1.18f, 1.25f));
        Assert.That(skinBounds.size.z, Is.GreaterThan(skinBounds.size.x * 2f),
            "Konsolun uzun ekseni duvar boyunca olmalı.");
        Assert.That(new[]
        {
            "Story01_ExitConsoleBack",
            "Story01_ExitConsoleSide_Left",
            "Story01_ExitConsoleSide_Right",
            "Story01_ExitConsoleMiddleShelf",
            "Story01_ExitConsoleDivider",
            "Story01_ExitConsoleToeKick",
            "Story01_ExitConsoleBackdrop",
            "Story01_ExitConsoleFrameTop"
        }.All(name => Find(name) != null), Is.True);
        Assert.That(skin.GetComponentsInChildren<Collider>(true), Is.Empty);

        Collider[] enabledBlockers = exitShelf.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.enabled)
            .ToArray();
        Assert.That(enabledBlockers, Has.Length.EqualTo(1));
        Assert.That(enabledBlockers[0].transform, Is.EqualTo(exitShelf));
        Assert.That(enabledBlockers[0].isTrigger, Is.False);
        Assert.That(Vector3.Distance(enabledBlockers[0].bounds.min, skinBounds.min), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(enabledBlockers[0].bounds.max, skinBounds.max), Is.LessThan(0.01f));

        Bounds topBounds = CombinedBounds(top);
        Bounds bagBounds = CombinedBoundsIncludingInactive(finalBag);
        Assert.That(bagBounds.min.y, Is.EqualTo(topBounds.max.y + 0.012f).Within(0.014f),
            "Final çanta konsol tablasına oturmalı.");
        Assert.That(bagBounds.center.z, Is.EqualTo(topBounds.center.z).Within(0.08f));
        Assert.That(dropZone.position.x, Is.LessThan(skinBounds.min.x));
        Assert.That(dropZone.position.y, Is.GreaterThan(topBounds.max.y));
        Assert.That(dropZone.position.z, Is.EqualTo(skinBounds.center.z).Within(0.01f));
        Assert.That(Find("Story01_ExitClock").position.z,
            Is.EqualTo(skinBounds.center.z).Within(0.01f),
            "Çıkış saati konsoldan kopuk biçimde gri duvarda yüzmemeli.");

        Bounds backWallBounds = CombinedBounds(Find("BackWall_Right"));
        Bounds frameTopBounds = CombinedBounds(Find("Story01_ExitConsoleFrameTop"));
        Assert.That(backWallBounds.min.z - frameTopBounds.max.z, Is.GreaterThanOrEqualTo(0.1f),
            "Çıkış konsolu çerçevesi sol köşedeki arka duvar dönüşünün içine girmemeli.");
    }

    [Test]
    public void Corridor_IsVisuallyClosedAndWarmAtTheRoomThreshold()
    {
        Transform corridorArt = Find("Story01_CorridorArt");
        Transform corridorCeiling = Find("Story01_CorridorCeiling");
        Transform endPanel = Find("Story01_CorridorEndPanel");
        Transform endPhoto = Find("Story01_CorridorEndPhoto");
        Transform upperLeft = Find("Story01_CorridorUpperWall_Left");
        Transform upperRight = Find("Story01_CorridorUpperWall_Right");
        Assert.That(new[] { corridorArt, corridorCeiling, endPanel, endPhoto, upperLeft, upperRight },
            Has.None.Null);
        Assert.That(corridorArt.GetComponentsInChildren<Collider>(true), Is.Empty,
            "Koridor kaplaması mevcut navigation collider'larını çoğaltmamalı.");

        Bounds ceilingBounds = CombinedBounds(corridorCeiling);
        Assert.That(ceilingBounds.size.z, Is.GreaterThan(6.5f));
        Assert.That(ceilingBounds.min.y, Is.InRange(4.01f, 4.05f));
        foreach (Transform upperWall in new[] { upperLeft, upperRight })
        {
            Bounds upperBounds = CombinedBounds(upperWall);
            Assert.That(upperBounds.min.y, Is.EqualTo(3.4f).Within(0.015f));
            Assert.That(upperBounds.max.y, Is.EqualTo(ceilingBounds.min.y).Within(0.015f),
                upperWall.name + " koridor duvarıyla yükseltilmiş tavan arasında skybox aralığı bırakmamalı.");
        }
        Assert.That(CombinedBounds(endPanel).max.y, Is.EqualTo(ceilingBounds.min.y).Within(0.015f),
            "Koridor son duvarı yükseltilmiş tavana kadar kapanmalı.");
        Assert.That(new[]
        {
            "Story01_CorridorRunnerBorder",
            "Story01_CorridorRunnerInset",
            "Story01_CorridorWainscot_Left",
            "Story01_CorridorWainscot_Right",
            "Story01_CorridorFlushMount_755",
            "Story01_CorridorFlushMount_1015"
        }.All(name => Find(name) != null), Is.True);

        Light entryLight = Find("Story01Practical_EntryLight")?.GetComponent<Light>();
        Assert.That(entryLight, Is.Not.Null);
        Assert.That(entryLight.transform.position.z, Is.GreaterThan(6f));
        Assert.That(entryLight.range, Is.GreaterThanOrEqualTo(4.1f),
            "Koridor eşiği girişten siyah boşluk gibi görünmemeli.");
    }

    [Test]
    public void PracticalLighting_CreatesWarmZonesAndParticipatesInBlackout()
    {
        Transform lighting = Find("Story01_PracticalLighting");
        Assert.That(lighting, Is.Not.Null);
        Light[] practicals = lighting.GetComponentsInChildren<Light>(true)
            .Where(light => light.name.StartsWith("Story01Practical_", StringComparison.Ordinal))
            .ToArray();
        Assert.That(practicals, Has.Length.EqualTo(3));
        Assert.That(practicals.All(light =>
            light.type == LightType.Point &&
            light.shadows == LightShadows.None &&
            light.intensity >= 0.8f &&
            light.range >= 3f &&
            light.GetComponent<Animator>() != null), Is.True,
            "Bölgesel ışıklar sıcak, mobil-güvenli ve karanlık provasına bağlı olmalı.");

        Light fill = Find("StoryFillLight")?.GetComponent<Light>();
        Assert.That(fill, Is.Not.Null);
        Assert.That(fill.intensity, Is.InRange(1.1f, 1.25f),
            "Tek düz fill ışığı sahneyi yıkamamalı; pratik ışıklar hacim vermeli.");
    }

    [Test]
    public void FoodUsesPhysicalInspectionAndHealthUsesAutomaticVisualInspection()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable water = GetPrivate<StoryInteractable>(director, "inspectWaterDate");
        StoryInteractable bandage = GetPrivate<StoryInteractable>(director, "inspectBandageSeal");

        Assert.That(water, Is.Not.Null);
        Assert.That(water.InteractionGesture, Is.EqualTo(StoryInteractionGesture.SwipeHorizontal));
        Assert.That(water.GestureTarget, Is.Not.Null);
        Assert.That(water.InteractFromAnywhere, Is.True);
        Assert.That(water.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));

        Assert.That(bandage, Is.Null);

        Assert.That(Find("WaterExpiryLabel_Unchecked"), Is.Not.Null);
        Assert.That(Find("WaterExpiryCheckedState")?.gameObject.activeSelf, Is.False);
        Assert.That(Find("BandageSealInspection"), Is.Not.Null);
        Assert.That(Find("BandageSealInspection").GetComponent<StoryInteractable>(), Is.Null);
        Assert.That(Find("BandageSealInspection").GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(Find("BandageSealCheckedState"), Is.Null);
        Assert.That(Find("BandageSealCheckmarkShort"), Is.Null);
        Assert.That(Find("BandageSealCheckmarkLong"), Is.Null);
    }

    [Test]
    public void SignalToolsUseVisibleObjectsAndFakeBagClosureStepsAreRemoved()
    {
        StoryPreparationDirector director = Object.FindFirstObjectByType<StoryPreparationDirector>();
        StoryInteractable signal = GetPrivate<StoryInteractable>(director, "reviewSignal");
        StoryInteractable signalOff =
            GetPrivate<StoryInteractable>(director, "reviewSignalFlashlightOff");
        StoryInteractable radioBatteryInsert =
            GetPrivate<StoryInteractable>(director, "reviewSignalRadioBatteryInsert");
        StoryInteractable radio = GetPrivate<StoryInteractable>(director, "reviewSignalRadio");
        StoryInteractable radioBatteryRemove =
            GetPrivate<StoryInteractable>(director, "reviewSignalRadioBatteryRemove");
        StoryInteractable whistle = GetPrivate<StoryInteractable>(director, "reviewSignalWhistle");

        Assert.That(signal.InteractionGesture, Is.EqualTo(StoryInteractionGesture.Tap));
        Assert.That(signal.RequiredGestureCount, Is.EqualTo(1));
        Assert.That(signal.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationFlashlight));
        Assert.That(signalOff.InteractionGesture, Is.EqualTo(StoryInteractionGesture.Tap));
        Assert.That(signalOff.RequiredGestureCount, Is.EqualTo(1));
        Assert.That(signalOff.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationFlashlight));
        Assert.That(GetPrivate<Transform>(director, "signalFlashlightApproachPoint"), Is.Not.Null);
        Assert.That(radioBatteryInsert.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(radioBatteryInsert.GestureTarget, Is.Not.Null);
        Assert.That(radioBatteryInsert.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(radioBatteryInsert.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationRadio));
        Assert.That(radio.InteractionGesture, Is.EqualTo(StoryInteractionGesture.Tap));
        Assert.That(radio.GestureTarget, Is.Null);
        Assert.That(radio.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationRadio));
        Assert.That(radioBatteryRemove.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(radioBatteryRemove.GestureTarget, Is.Not.Null);
        Assert.That(radioBatteryRemove.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(radioBatteryRemove.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationRadio));
        Assert.That(whistle.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(whistle.GestureTarget, Is.Not.Null);
        Assert.That(whistle.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(whistle.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationSiblingHandoff));
        Assert.That(Find("Review_WhistleCanDropZone"), Is.Not.Null);
        Assert.That(Find("Can_WhistleTargetSocket")?.GetComponent<SpriteRenderer>(), Is.Not.Null);
        Assert.That(Find("Can_WhistleTargetSocket")?.GetComponent<BillboardToCamera>(), Is.Not.Null);
        Assert.That(signal.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(signalOff.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(radioBatteryInsert.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(2));
        Assert.That(radio.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(radioBatteryRemove.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(4));
        Assert.That(whistle.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(3));
        Assert.That(new[] { radioBatteryInsert, radioBatteryRemove, whistle }.All(interaction =>
            interaction.GetComponentsInChildren<Renderer>(true).Length > 0), Is.True);
        Assert.That(signal.GetComponentsInChildren<Renderer>(true), Is.Empty,
            "Fenerin model yüzeyindeki hotspot sahte bir düğme çizmemeli.");
        Assert.That(signalOff.GetComponentsInChildren<Renderer>(true), Is.Empty,
            "Fenerin kapatma hotspot'u sahte bir ikinci düğme çizmemeli.");
        Assert.That(radio.GetComponentsInChildren<Renderer>(true), Is.Empty,
            "Radyonun kendi beyaz düğmesi collider ile kullanılmalı; üstüne primitive düğme bindirilmemeli.");
        Assert.That(Find("FlashlightInspectionBeam"), Is.Not.Null);
        Assert.That(radio.gameObject.name, Is.EqualTo("Review_RadioPowerButton"));
        Assert.That(Find("SignalRadioInspectionStage"), Is.Not.Null);
        Assert.That(Find("BagReview_EmergencyRadio"), Is.Not.Null);
        Assert.That(Find("Review_RadioBatterySlot"), Is.Not.Null);
        Assert.That(Find("Review_RadioBatteryStorageDropZone"), Is.Not.Null);
        Assert.That(Find("RadioBatteryBay"), Is.Not.Null);
        Assert.That(Find("RadioBatteryBay").GetComponentsInChildren<Renderer>(true).Length,
            Is.EqualTo(1),
            "Pil hedefi radyoya yapıştırılmış dev çerçeve değil, tek ve küçük bir gövde girintisi olmalı.");
        Assert.That(Find("RadioBatteryBay_TopRail"), Is.Null);
        Assert.That(Find("RadioBatteryBay_BottomRail"), Is.Null);
        Assert.That(Find("RadioBatteryBay_FrontRail"), Is.Null);
        Assert.That(Find("RadioBatteryBay_BackRail"), Is.Null);
        Assert.That(Find("RadioBatteryBay_Contact"), Is.Null);
        Assert.That(Find("Review_RadioBatteryTableRest"), Is.Null,
            "Masadaki gerçek pilin altına sahte beyaz hedef tablası eklenmemeli.");
        Assert.That(Find("RadioBatteryInsertSocket"), Is.Null,
            "Pil yuvasında modelden kopuk yeşil sprite bulunmamalı.");
        Assert.That(Find("RadioBatteryStorageSocket"), Is.Null,
            "Pil bırakma noktasında modelden kopuk yeşil sprite bulunmamalı.");
        Assert.That(radio.GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(radio.GetComponent<MeshRenderer>(), Is.Null);
        Transform radioModel = Find("BagReview_EmergencyRadio").Find("KKTC_AuthoredVisual");
        Vector3 authoredPowerButton = radioModel.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SwitchAnchor").position;
        Assert.That(Vector3.Distance(radio.transform.position, authoredPowerButton), Is.LessThan(0.005f),
            "Güç etkileşimi tahmini bir gövde noktasına değil, modeldeki beyaz düğmenin üstüne oturmalı.");
        Vector3 radioButtonTouchSize = Vector3.Scale(
            radio.GetComponent<BoxCollider>().size,
            radio.transform.lossyScale);
        Assert.That(radioButtonTouchSize.x, Is.InRange(0.07f, 0.1f));
        Assert.That(radioButtonTouchSize.y, Is.InRange(0.055f, 0.085f));
        Assert.That(radioButtonTouchSize.z, Is.InRange(0.07f, 0.1f));
        Assert.That(radio.HighlightRoot.transform.position.y - authoredPowerButton.y, Is.InRange(0.055f, 0.085f),
            "Küçük amber ok doğrudan beyaz düğmenin üstünde durmalı.");
        Assert.That(radio.HighlightRoot.transform.Find("ObjectiveBadge").gameObject.activeSelf, Is.False);
        Assert.That(radio.HighlightRoot.transform.Find("ObjectiveGestureIcon").gameObject.activeSelf, Is.False);
        Assert.That(Find("RadioPowerState_Off").GetComponentsInChildren<Renderer>(true), Is.Empty);
        Assert.That(Find("RadioPowerState_On").GetComponentsInChildren<Renderer>(true), Is.Empty);
        Assert.That(Find("Can_WhistleClipped"), Is.Not.Null);
        Assert.That(GetPrivate<Transform>(director, "signalWhistleCanPose"), Is.Not.Null);

        Bounds radioBounds = CombinedBoundsIncludingInactive(Find("BagReview_EmergencyRadio"));
        Transform worldBattery = Find("WorldItem_Batteries");
        Transform looseBattery = Find("Review_RadioBatteryLoose");
        Transform insertedBattery = Find("Review_RadioBatteryInserted");
        Bounds looseBatteryBounds = CombinedBoundsIncludingInactive(
            worldBattery.Find("Review_RadioBatteryLoose/Review_RadioBatteryInserted/Yedek pil"));
        Transform authoredSocket=radioModel.GetComponentsInChildren<Transform>(true).First(t=>t.name=="BatterySocket");
        Assert.That(Vector3.Distance(authoredSocket.position,radioBatteryInsert.GestureTarget.position),Is.LessThan(.001f));
        Assert.That(Find("RadioBatteryBay").GetComponent<Renderer>().enabled,Is.False,"The old fake cavity is replaced by the modelled well.");
        Collider looseBatteryCollider = Find("Review_RadioBatteryLoose").GetComponent<Collider>();
        Vector3 looseBatteryTouchSize = Vector3.Scale(
            ((BoxCollider)looseBatteryCollider).size,
            looseBatteryCollider.transform.lossyScale);
        Assert.That(radioBounds.min.y, Is.InRange(0.779f, 0.81f),
            "Radyo test sırasında hazırlık masasının üstünde durmalı.");
        Assert.That(looseBattery.IsChildOf(worldBattery), Is.True,
            "Radyo testi masadaki gerçek WorldItem_Batteries hiyerarşisini kullanmalı.");
        Assert.That(insertedBattery.IsChildOf(looseBattery), Is.True,
            "Takma ve çıkarma etkileşimleri aynı fiziksel pil görselini taşımalı.");
        Assert.That(Find("Review_RadioBatteryLoose_Visual"), Is.Null,
            "Test için ikinci bir gevşek pil spawn edilmemeli.");
        Assert.That(Find("Review_RadioBatteryInserted_Visual"), Is.Null,
            "Takılmış pil için ikinci bir görsel spawn edilmemeli.");
        Assert.That(Find("Review_RadioBatteryStored"), Is.Null,
            "Masaya dönüşte üçüncü bir pil spawn edilmemeli.");
        Assert.That(looseBatteryBounds.size.magnitude, Is.InRange(0.045f,0.1f),
            "Radyoya takılacak gerçek masa pili görünür ölçekte olmalı.");
        Assert.That(radioBatteryInsert.GestureTarget.position.x, Is.GreaterThan(radioBounds.center.x),
            "Pil hedefi kameradan saklanan arka yüzde değil, radyonun kameraya bakan yanında olmalı.");
        Assert.That(radioBatteryInsert.GestureTarget.position.x, Is.LessThan(radioBounds.max.x + 0.1f),
            "Pil hedefi radyo gövdesinden kopuk biçimde havada durmamalı.");
        Assert.That(Vector3.Angle(radioBatteryInsert.GestureTarget.up, Vector3.up), Is.LessThan(1f),
            "Pil hazneye girerken masa yönünde kalmamalı; uzun ekseni dikey pil yatağına dönmeli.");
        Vector3 restPosition=looseBattery.position; Quaternion restRotation=looseBattery.rotation;
        looseBattery.SetPositionAndRotation(authoredSocket.position,authoredSocket.rotation);
        Bounds seated=CombinedBoundsIncludingInactive(worldBattery.Find("Review_RadioBatteryLoose/Review_RadioBatteryInserted/Yedek pil"));
        Assert.That(seated.min.y,Is.GreaterThanOrEqualTo(radioBounds.min.y));
        Assert.That(seated.max.y,Is.LessThan(radioBounds.max.y));
        Assert.That(seated.center.x,Is.InRange(radioBounds.min.x,radioBounds.max.x));
        looseBattery.SetPositionAndRotation(restPosition,restRotation);
        Assert.That(looseBatteryBounds.min.y, Is.InRange(0.79f, 0.85f),
            "Yedek pil masanın üstünde durmalı.");

        Assert.That(radioBounds.size.magnitude, Is.GreaterThan(looseBatteryBounds.size.magnitude * 3f),
            "The radio must read as the main prop instead of looking the same size as its battery.");
        Assert.That(looseBatteryTouchSize.x, Is.GreaterThanOrEqualTo(0.19f));
        Assert.That(looseBatteryTouchSize.y, Is.GreaterThanOrEqualTo(0.11f));
        Assert.That(looseBatteryTouchSize.z, Is.GreaterThanOrEqualTo(0.21f),
            "The small battery model needs a mobile-friendly touch volume.");
        Assert.That(looseBatteryTouchSize.x, Is.LessThan(0.42f));
        Assert.That(looseBatteryTouchSize.y, Is.LessThan(0.3f));
        Assert.That(looseBatteryTouchSize.z, Is.LessThan(0.42f),
            "The battery touch collider must remain local to the prop instead of covering the room.");
        Assert.That(radioBatteryInsert.HighlightRoot.transform.localScale.x, Is.LessThanOrEqualTo(0.3f),
            "The close-up gesture badge must not cover the battery.");

        Transform flashlightVisual = signal.transform.parent.Find("El feneri");
        Bounds flashlightBounds = CombinedBoundsIncludingInactive(flashlightVisual);
        Bounds switchBounds = signal.GetComponent<BoxCollider>().bounds;
        Assert.That(switchBounds.center.y - flashlightBounds.max.y, Is.InRange(-0.05f, 0.04f),
            "Fener düğmesi gövdeden kopuk biçimde havada durmamalı.");
        Assert.That(switchBounds.center.x, Is.InRange(
            flashlightBounds.min.x - 0.02f,
            flashlightBounds.max.x + 0.02f));
        Assert.That(switchBounds.center.z, Is.InRange(
            flashlightBounds.min.z - 0.02f,
            flashlightBounds.max.z + 0.02f));

        Bounds whistleBounds = CombinedBoundsIncludingInactive(Find("Review_WhistleHandoff_Visual"));
        Assert.That(whistleBounds.min.y, Is.InRange(0.79f, 0.85f),
            "Can'a verilecek düdük görünür biçimde hazırlık masasının üstünde durmalı.");
        Assert.That(whistleBounds.center.x, Is.InRange(-0.3f, 0.3f));
        Assert.That(whistleBounds.center.z, Is.InRange(0.4f, 0.9f));

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

        Assert.That(heavyLift.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(heavyLift.gameObject.name, Is.EqualTo("EmergencyBag_Open_Packing"));
        Assert.That(heavyLift.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        Assert.That(heavyLift.GestureTarget, Is.Not.Null);
        Assert.That(heavyLift.GestureTarget.name, Is.EqualTo("BagWeightLiftDropZone"));
        Assert.That(heavyLift.GestureTarget.position.y,
            Is.GreaterThan(heavyLift.transform.position.y + 0.8f));
        Assert.That(heavyLift.GestureTarget.GetComponent<BagDropZone>(), Is.Not.Null);
        Assert.That(heavyLift.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(heavyLift.Prompt, Does.Contain("yukarıdaki hedefe sürükle"));
        Assert.That(heavyLift.OnInteracted.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(2));
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
        Assert.That(Find("ConsoleConflict_InBag_Visual").IsChildOf(heavyLift.transform), Is.True,
            "Konsol çanta kaldırılırken çantayla birlikte hareket etmeli.");
        Assert.That(Find("ConsoleConflict_Heavy_Visual").IsChildOf(Find("ConsoleWeightConsequence")), Is.True,
            "Ağırlık sonucu açıldığında konsol çökmüş çantada görünmeli.");
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
        Assert.That(weight.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(weight.gameObject.name, Is.EqualTo("EmergencyBag_Open_Packing"));
        Assert.That(weight.GestureTarget, Is.Not.Null);
        Assert.That(weight.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(GetPrivate<StoryInteractable>(director, "adjustBagStraps"), Is.Null);
        Assert.That(place.InteractionGesture, Is.EqualTo(StoryInteractionGesture.DragToTarget));
        Assert.That(place.GestureTarget, Is.Not.Null);
        Assert.That(place.GetComponent<DraggableItem>(), Is.Not.Null);
        Assert.That(place.gameObject.name, Is.EqualTo("EmergencyBag_Worn"));
        Assert.That(place.ReturnCameraAfterCompletion, Is.False,
            "Final raf tablosu kapanış diyaloğu boyunca ekranda kalmalı.");
        Assert.That(place.FocusCameraZone, Is.EqualTo(StoryCameraZoneId.PreparationExitShelf));
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
    public void WornBackpacks_AreMountedToDenizTorsoInsteadOfFloatingFromTheCharacterRoot()
    {
        Transform deniz = Find("Deniz_12");
        Animator animator = deniz.GetComponentInChildren<Animator>(true);
        Transform torso = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                          animator.GetBoneTransform(HumanBodyBones.Chest) ??
                          animator.GetBoneTransform(HumanBodyBones.Spine);
        Assert.That(torso, Is.Not.Null);

        foreach (string bagName in new[] { "Deniz_WornEmergencyBag", "EmergencyBag_Worn" })
        {
            Transform bag = Find(bagName);
            Assert.That(bag, Is.Not.Null, bagName);
            Assert.That(bag.IsChildOf(torso), Is.True,
                bagName + " karakter kökünde yüzmemeli; gövde animasyonunu fiziksel olarak izlemeli.");
            Bounds bounds = CombinedBoundsIncludingInactive(bag);
            Vector3 torsoToBag = bounds.center - torso.position;
            Assert.That(Vector3.Dot(torsoToBag, deniz.forward), Is.LessThan(-0.02f),
                bagName + " Deniz'in arkasında durmalı.");
            Assert.That(Mathf.Abs(Vector3.Dot(torsoToBag, deniz.up)), Is.LessThan(0.34f),
                bagName + " belde veya baş hizasında yüzmemeli.");
        }
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
        Assert.That(Find("KoltukBattaniyesi"), Is.Null,
            "Turuncu boru gibi okunan paketli bedroll dekoru kanepede kalmamalı.");

        Bounds signalNightstand = CombinedBounds(Find("SignalNightstand"));
        Bounds roomPlant = CombinedBounds(Find("RoomPlant"));
        Collider roomPlantCollider = Find("RoomPlant_Collider")?.GetComponent<Collider>();
        Bounds familySofa = CombinedBounds(Find("FamilySofa"));
        Bounds warmthChest = CombinedBounds(Find("WarmthChest"));
        Assert.That(roomPlantCollider, Is.Not.Null);
        Assert.That(Vector2.Distance(
                new Vector2(roomPlantCollider.bounds.center.x, roomPlantCollider.bounds.center.z),
                new Vector2(roomPlant.center.x, roomPlant.center.z)),
            Is.LessThan(0.08f),
            "Bitkinin görünmez blocker'ı eski konumda kalıp kamerayı veya rotayı engellememeli.");
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
            Is.EqualTo("Assets/Story/Environment/ThirdParty/KenneySurvival/Models/chest.fbx"),
            "Kapalı ve açık durum aynı fiziksel sandığa ait görünmeli.");

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
        Assert.That(wardrobe.localPosition.x, Is.EqualTo(0.48f).Within(0.01f));

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
    public void OpeningFamily_MeshFacesPointIntoTheConversation()
    {
        Transform deniz = Find("Deniz_12");
        Transform can = Find("Can_8");
        Transform parent = Find("Anne_Ayse");
        Assert.That(deniz, Is.Not.Null);
        Assert.That(can, Is.Not.Null);
        Assert.That(parent, Is.Not.Null);

        foreach ((Transform character, Vector3 target) in new[]
                 {
                     (deniz, parent.position),
                     (can, parent.position),
                     (parent, (deniz.position + can.position) * 0.5f)
                 })
        {
            Transform visual = character.Find("Visual");
            Assert.That(visual, Is.Not.Null, character.name);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.y, 180f)),
                Is.LessThan(0.1f), character.name + " visual forward correction");

            Vector3 visibleModelForward =
                -Vector3.ProjectOnPlane(visual.forward, Vector3.up).normalized;
            Vector3 conversationDirection =
                Vector3.ProjectOnPlane(target - character.position, Vector3.up).normalized;
            Assert.That(Vector3.Dot(visibleModelForward, conversationDirection),
                Is.GreaterThan(0.96f),
                character.name + " must visibly face the family conversation.");
        }
    }

    [Test]
    public void Preview_UsesOneSoftShadowSunForGroundedProps()
    {
        Light[] lights = Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        Assert.That(lights, Is.Not.Empty);
        Assert.That(lights.Count(light=>light.shadows!=LightShadows.None),Is.EqualTo(1));
        Assert.That(lights.Single(light=>light.shadows!=LightShadows.None).shadows,Is.EqualTo(LightShadows.Soft));
        Assert.That(Find("EmergencyBag_Open_Packing").Find("KKTC_AuthoredVisual").GetComponentsInChildren<Renderer>(true)
            .All(r=>r.receiveShadows),Is.True);
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

    private static void AssertPortraitCameraFrames(
        string cameraName,
        Bounds subject,
        float minimumViewportHeight)
    {
        CinemachineCamera source = Find(cameraName)?.GetComponent<CinemachineCamera>();
        Assert.That(source, Is.Not.Null, cameraName);

        GameObject cameraObject = new GameObject(cameraName + "_PortraitFramingProbe")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        try
        {
            Camera probe = cameraObject.AddComponent<Camera>();
            probe.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            probe.fieldOfView = source.Lens.FieldOfView;
            probe.aspect = 9f / 16f;
            probe.nearClipPlane = 0.01f;
            probe.farClipPlane = 100f;

            Vector3 min = subject.min;
            Vector3 max = subject.max;
            Vector3 viewportMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 viewportMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 viewport = probe.WorldToViewportPoint(corner);
                Assert.That(viewport.z, Is.GreaterThan(0.01f),
                    cameraName + " hedefin arkasına bakıyor.");
                viewportMin = Vector3.Min(viewportMin, viewport);
                viewportMax = Vector3.Max(viewportMax, viewport);
            }

            Vector3 center = probe.WorldToViewportPoint(subject.center);
            float viewportHeight = viewportMax.y - viewportMin.y;
            Assert.That(center.x, Is.InRange(0.14f, 0.86f),
                cameraName + " hedefi yatay kadrajın dışında bırakıyor.");
            Assert.That(center.y, Is.InRange(0.16f, 0.82f),
                cameraName + " hedefi görev şeridinin altında okunur alanda tutmuyor.");
            Assert.That(viewportMin.x, Is.LessThan(0.95f));
            Assert.That(viewportMax.x, Is.GreaterThan(0.05f));
            Assert.That(viewportMin.y, Is.LessThan(0.9f));
            Assert.That(viewportMax.y, Is.GreaterThan(0.08f));
            Assert.That(viewportHeight, Is.GreaterThanOrEqualTo(minimumViewportHeight),
                $"{cameraName} hedefi telefonda okunamayacak kadar küçük gösteriyor: {viewportHeight:F3}");
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }
}
