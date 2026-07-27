using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public sealed class StoryPreparationPlayModeTests
{
    [UnityTest]
    public IEnumerator OpenBagFlowsFromFamilyPlanDirectlyToSignalDiscovery()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour director = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryPreparationDirector");
        MonoBehaviour manager = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryGameManager");
        MonoBehaviour uiController = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryUIController");

        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnFamilyPlanStarted");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnContactCardPlaced");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnCanWhistleRolePlaced");
        yield return AdvanceSubtitlesUntilIdle(uiController);

        Assert.That(GameObject.Find("Inspect_EmptyBag"), Is.Null);
        Assert.That(GameObject.Find("BagZipperSwipeTarget"), Is.Null);
        Assert.That(Find("EmergencyBag_Open_Packing").activeSelf, Is.True);
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(),
            Is.EqualTo("Signal"));
        MonoBehaviour signalDiscovery = FindInteraction("Discover_SignalDrawer");
        Assert.That((bool)Property(signalDiscovery, "IsAvailable").GetValue(signalDiscovery), Is.True,
            "Family-plan dialogue must hand control directly to the first real object search.");
        AssertCheckpoint(manager, "BagInspected");
    }

    [UnityTest]
    public IEnumerator FamilyPlanCard_UsesVisibleSocketSnapAnimation()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        Animation socketPulse = Find("PlanCardGhostPulse_1").GetComponent<Animation>();
        Assert.That(socketPulse, Is.Not.Null);
        Assert.That(socketPulse.isPlaying, Is.True,
            "Boş plan yuvası kart bırakılmadan önce hafif nabız animasyonu oynamalı.");

        GameObject completedCard = Find("FamilyPlanCompleteMark");
        Transform snapMotion = Find("FamilyPlanCompleteMark_SnapMotion").transform;
        completedCard.SetActive(false);
        snapMotion.localPosition = Vector3.zero;
        snapMotion.localRotation = Quaternion.identity;
        snapMotion.localScale = Vector3.one;
        completedCard.SetActive(true);
        yield return null;

        Animation animation = snapMotion.GetComponent<Animation>();
        Assert.That(animation, Is.Not.Null);
        Assert.That(animation.isPlaying, Is.True,
            "Tamamlanan kart görünür olduğunda socket'e oturma animasyonu kendiliğinden başlamalı.");
        Assert.That(snapMotion.localPosition.z, Is.GreaterThan(0.08f),
            "Kart önce pano yüzeyinin önünde belirip sonra yuvaya oturmalı.");

        yield return new WaitForSeconds(0.78f);
        Assert.That(Mathf.Abs(snapMotion.localPosition.z), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(snapMotion.localRotation, Quaternion.identity), Is.LessThan(0.5f));
        Assert.That(Vector3.Distance(snapMotion.localScale, Vector3.one), Is.LessThan(0.02f));
    }

    [UnityTest]
    public IEnumerator FamilyPlanCard_RemainsCameraFacingWhileShotChanges()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        Camera camera = Camera.main;
        GameObject card = Find("FamilyMeetingPointCard_Drag");
        MonoBehaviour draggable = card.GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "DraggableItem");
        Assert.That(camera, Is.Not.Null);
        Assert.That(draggable, Is.Not.Null);

        Vector2 pointer = camera.WorldToScreenPoint(card.transform.position);
        Assert.That((bool)InvokeWithResult(draggable, "BeginManagedDrag", pointer), Is.True);
        Invoke(draggable, "UpdateManagedDrag", pointer);
        Assert.That(Vector3.Dot(card.transform.up, -camera.transform.forward), Is.GreaterThan(0.995f));

        camera.transform.rotation = Quaternion.Euler(24f, 38f, 3f);
        Invoke(draggable, "UpdateManagedDrag", pointer);
        Assert.That(Vector3.Dot(card.transform.up, -camera.transform.forward), Is.GreaterThan(0.995f),
            "Kamera blend ederken kart eski açıda kalıp panoya/duvara saplanmamalı.");

        Invoke(draggable, "CancelManagedDrag");
        yield return null;
    }

    [UnityTest]
    public IEnumerator FamilyPlanCard_LeftAndRightDragStayInFrontOfTheWall()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        Camera camera = Camera.main;
        GameObject card = Find("FamilyMeetingPointCard_Drag");
        Transform board = Find("FamilyPlanBoard").transform;
        Transform planeAnchor = Find("PlanCardSharedDragPlane").transform;
        MonoBehaviour draggable = card.GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "DraggableItem");
        float anchoredDepth = board.InverseTransformPoint(planeAnchor.position).z;

        Vector2 start = camera.WorldToScreenPoint(card.transform.position);
        Assert.That((bool)InvokeWithResult(draggable, "BeginManagedDrag", start), Is.True);

        foreach (float viewportX in new[] { 0.12f, 0.5f, 0.88f })
        {
            Vector2 pointer = new Vector2(Screen.width * viewportX, Screen.height * 0.78f);
            Invoke(draggable, "UpdateManagedDrag", pointer);
            float cardDepth = board.InverseTransformPoint(card.transform.position).z;
            Assert.That(cardDepth, Is.InRange(0.39f, anchoredDepth + 0.02f),
                "Kart merkezi pano ve duvar yüzeyinin güvenli biçimde önünde kalmalı.");
        }

        Invoke(draggable, "UpdateManagedDrag",
            new Vector2(Screen.width * 0.5f, Screen.height * 0.03f));
        Assert.That(card.transform.position.y, Is.GreaterThanOrEqualTo(1.075f),
            "Kart aşağı sürüklenince masa hacminin içine geçmemeli.");

        Invoke(draggable, "CancelManagedDrag");
        yield return null;
    }

    [UnityTest]
    public IEnumerator FamilyPlanCard_SoftSnapsNearSlotAndAcceptsNearRelease()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        Camera camera = Camera.main;
        GameObject card = Find("FamilyMeetingPointCard_Drag");
        GameObject dropZone = Find("FamilyPlanCardDropZone");
        Transform socket = Find("PlanCardGhostPulse_1").transform;
        Transform planeAnchor = Find("PlanCardSharedDragPlane").transform;
        MonoBehaviour draggable = card.GetComponents<MonoBehaviour>()
            .Single(component => component.GetType().Name == "DraggableItem");
        float radiusRatio = (float)Field(draggable, "magneticSnapViewportRadius").GetValue(draggable);
        float dragLift = (float)Field(draggable, "dragLift").GetValue(draggable);
        float surfaceOffset = (float)Field(draggable, "magneticSnapSurfaceOffset").GetValue(draggable);
        float minimumY = (float)Field(draggable, "dragMinimumWorldY").GetValue(draggable);

        Vector2 start = camera.WorldToScreenPoint(card.transform.position);
        Assert.That((bool)InvokeWithResult(draggable, "BeginManagedDrag", start), Is.True);

        Vector2 slotScreen = camera.WorldToScreenPoint(socket.position);
        float radiusPixels = Mathf.Min(Screen.width, Screen.height) * radiusRatio;
        Vector2 nearSlot = slotScreen + Vector2.right * radiusPixels * 0.55f;
        Plane boardPlane = new Plane(planeAnchor.forward, planeAnchor.position);
        Ray rawRay = camera.ScreenPointToRay(nearSlot);
        Assert.That(boardPlane.Raycast(rawRay, out float enter), Is.True);
        Vector3 rawPosition = rawRay.GetPoint(enter) + Vector3.up * dragLift;
        Vector3 snappedSlot = socket.position + planeAnchor.forward * surfaceOffset +
                              Vector3.up * dragLift;
        snappedSlot.y = Mathf.Max(snappedSlot.y, minimumY);

        Invoke(draggable, "UpdateManagedDrag", nearSlot);
        Assert.That(
            Vector3.Distance(card.transform.position, snappedSlot),
            Is.LessThan(Vector3.Distance(rawPosition, snappedSlot) * 0.82f),
            "Kart doğru yuvaya yaklaşınca parmak konumundan yumuşakça socket merkezine çekilmeli.");
        Vector2 centerSlot = camera.WorldToScreenPoint(socket.position);
        Invoke(draggable, "UpdateManagedDrag", centerSlot);
        Assert.That(Vector3.Distance(card.transform.position, snappedSlot), Is.LessThan(0.015f),
            "Kart slot merkezine gelince hover'da kalmamalı; gerçek yerleşim konumuna ilerlemeli.");
        Assert.That(Vector3.Dot(card.transform.up, planeAnchor.forward), Is.GreaterThan(0.995f),
            "Kart slota girerken pano yüzeyine hizalanmalı.");
        Vector3 snappedPosition = card.transform.position;
        Quaternion firstHoverRotation = card.transform.rotation;
        yield return new WaitForSecondsRealtime(0.17f);
        Invoke(draggable, "UpdateManagedDrag", centerSlot);
        Assert.That(Vector3.Distance(card.transform.position, snappedPosition), Is.LessThan(0.002f),
            "Slotta bekleyen kartın konumu sallanmamalı; yalnız görsel rotasyonu yaşamalı.");
        Assert.That(Quaternion.Angle(firstHoverRotation, card.transform.rotation), Is.GreaterThan(0.35f),
            "Tam snap noktasında kartın hafif canlı salınımı kaybolmamalı.");
        Assert.That((bool)InvokeWithResult(draggable, "EndManagedDrag", nearSlot), Is.True,
            "Kart manyetik alan içindeyken parmak tam merkeze gelmese de doğru yuva bırakmayı kabul etmeli.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator OpeningDialogue_SelectsTheMatchingSyntheticVoiceClip()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour ui = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryUIController");
        AudioSource voiceSource = (AudioSource)Field(ui, "dialogueVoiceSource").GetValue(ui);
        float timeout = Time.realtimeSinceStartup + 2f;
        while (voiceSource.clip == null && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(voiceSource.clip, Is.Not.Null);
        Assert.That(voiceSource.clip.name, Is.EqualTo("01_opening"));
    }

    [UnityTest]
    public IEnumerator PreparationScene_CompletesAllDecisionRoundsWithoutSoftlock()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Story_01_RebuildPreview"));
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour director = behaviours.Single(item => item != null && item.GetType().Name == "StoryPreparationDirector");
        MonoBehaviour manager = behaviours.Single(item => item != null && item.GetType().Name == "StoryGameManager");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        Assert.That(director, Is.Not.Null);
        Assert.That(manager, Is.Not.Null);
        object[] items = ((System.Array)Property(director, "Items").GetValue(director)).Cast<object>().ToArray();
        Assert.That(items, Has.Length.EqualTo(13));
        Assert.That((bool)Property(director, "RevisedFlow").GetValue(director), Is.True);
        AssertCheckpoint(manager, "PreparationStart");

        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnFamilyPlanStarted");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnContactCardPlaced");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnCanWhistleRolePlaced");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(GameObject.Find("Inspect_EmptyBag"), Is.Null,
            "Açık çanta için ikinci bir açma/fermuar görevi üretilmemeli.");
        Assert.That(Find("EmergencyBag_Open_Packing").activeSelf, Is.True);
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Signal"));
        MonoBehaviour signalDiscovery = FindInteraction("Discover_SignalDrawer");
        Invoke(signalDiscovery, "CompletePreparedInteraction");
        yield return new WaitForSeconds(0.62f);

        object flashlight = items.Single(item => Property(item, "ItemId").GetValue(item).ToString() == "Flashlight");
        GameObject flashlightSource = (GameObject)Field(flashlight, "sourceRoot").GetValue(flashlight);
        GameObject flashlightPacked = (GameObject)Field(flashlight, "packedVisual").GetValue(flashlight);
        MonoBehaviour flashlightMotion = (MonoBehaviour)Property(flashlight, "LegacyBagMotion").GetValue(flashlight);
        MonoBehaviour drawerFlashlight = Find("DrawerItem_Flashlight").GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "StoryInteractable");
        MonoBehaviour cameraController = behaviours.Single(item => item != null && item.GetType().Name == "StoryCameraController");
        float cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < cameraTimeout)
            yield return null;
        Assert.That((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController), Is.False,
            "Malzeme ve açık çanta kadrajı oturduktan sonra dünya etkileşimi geri açılmalı.");
        Camera storyCamera = Camera.main;
        Assert.That(storyCamera, Is.Not.Null);
        Collider drawerFlashlightCollider = Find("DrawerItem_Flashlight").GetComponent<Collider>();
        Assert.That(drawerFlashlightCollider, Is.Not.Null);
        Vector2 itemScreenPosition = storyCamera.WorldToScreenPoint(drawerFlashlightCollider.bounds.center);
        Assert.That(itemScreenPosition.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(itemScreenPosition.y, Is.InRange(0f, (float)Screen.height));
        Assert.That(Property(drawerFlashlight, "InteractionGesture").GetValue(drawerFlashlight).ToString(),
            Is.EqualTo("Tap"));
        Assert.That((bool)Property(drawerFlashlight, "IsAvailable").GetValue(drawerFlashlight), Is.True);
        Invoke(drawerFlashlight, "CompletePreparedInteraction");
        Assert.That(Find("DrawerItem_Flashlight").activeSelf, Is.False,
            "Çekmecedeki fener dokununca çekmecede kalmamalı.");
        Assert.That(flashlightSource.activeSelf, Is.True,
            "Çekmeceden seçilen fener önce masadaki sürükleme noktasında görünmeli.");

        yield return new WaitForSeconds(0.82f);
        Assert.That(Vector3.Distance(flashlightSource.transform.position, new Vector3(-0.25f, 0.81f, 0.65f)),
            Is.LessThan(0.08f), "Fener çekmeceden standart masa yuvasına taşınmalı.");
        object flashlightInteractable = Property(flashlight, "Interactable").GetValue(flashlight);
        Assert.That(Property(flashlightInteractable, "InteractionGesture").GetValue(flashlightInteractable).ToString(),
            Is.EqualTo("DragToBag"));
        Assert.That((bool)Property(flashlightInteractable, "IsAvailable").GetValue(flashlightInteractable), Is.False,
            "İlk seçilen eşya masaya gelir gelmez çanta fazını tek başına başlatmamalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationSignal"),
            "Dört gerekli eşya seçilene kadar kamera çekmecede kalmalı.");

        foreach (string drawerItemName in new[]
                 {
                     "DrawerItem_Batteries",
                     "DrawerItem_Whistle",
                     "DrawerItem_Radio"
                 })
        {
            MonoBehaviour drawerItem = Find(drawerItemName).GetComponents<MonoBehaviour>()
                .Single(item => item.GetType().Name == "StoryInteractable");
            Assert.That((bool)Property(drawerItem, "IsAvailable").GetValue(drawerItem), Is.True,
                drawerItemName + " ilk seçimden sonra da seçilebilir kalmalı.");
            Invoke(drawerItem, "CompletePreparedInteraction");
            yield return null;
        }

        yield return new WaitForSeconds(0.9f);
        Assert.That(new[]
        {
            "DrawerItem_Flashlight",
            "DrawerItem_Batteries",
            "DrawerItem_Whistle",
            "DrawerItem_Radio"
        }.All(name => !Find(name).activeSelf), Is.True,
            "Dört gerekli eşya da önce çekmeceden seçilip masaya gönderilmeli.");
        Assert.That(items.Where(item =>
                Property(item, "Category").GetValue(item).ToString() == "Signal" &&
                (bool)Property(item, "Recommended").GetValue(item))
            .All(item =>
            {
                GameObject source = (GameObject)Field(item, "sourceRoot").GetValue(item);
                object interactable = Property(item, "Interactable").GetValue(item);
                return source.activeSelf &&
                       (bool)Property(interactable, "IsAvailable").GetValue(interactable) ==
                       (Property(item, "ItemId").GetValue(item).ToString() == "Flashlight");
            }), Is.True,
            "Dört eşya masada görünmeli; yalnız sıradaki fener açıklama için etkileşim almalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationBag"),
            "Toplu çekmece seçimi tamamlanınca kamera masaya ve çantaya geçmeli.");

        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < cameraTimeout)
            yield return null;
        Collider flashlightCollider = flashlightSource.GetComponent<Collider>();
        Collider bagOpeningCollider = Find("PhysicalBagOpening").GetComponent<Collider>();
        Assert.That(flashlightCollider, Is.Not.Null);
        Assert.That(bagOpeningCollider, Is.Not.Null);
        itemScreenPosition = storyCamera.WorldToScreenPoint(flashlightCollider.bounds.center);
        Vector2 bagScreenPosition = storyCamera.WorldToScreenPoint(bagOpeningCollider.bounds.center);
        InvokeNonPublic(touchManager, "HandleWorldTap", itemScreenPosition);
        Assert.That(Field(touchManager, "managedDrag").GetValue(touchManager), Is.Null);
        Assert.That((bool)Property(flashlightInteractable, "IsAvailable").GetValue(flashlightInteractable), Is.False,
            "Fener yaklaşma ve fiziksel kontrol sırasında çantaya sürüklenememeli.");

        MonoBehaviour flashlightSwitchOn =
            (MonoBehaviour)Field(director, "reviewSignal").GetValue(director);
        MonoBehaviour flashlightSwitchOff =
            (MonoBehaviour)Field(director, "reviewSignalFlashlightOff").GetValue(director);
        Transform flashlightApproach =
            (Transform)Field(director, "signalFlashlightApproachPoint").GetValue(director);
        float flashlightApproachTimeout = Time.realtimeSinceStartup + 6f;
        while (!(bool)Property(flashlightSwitchOn, "IsAvailable").GetValue(flashlightSwitchOn) &&
               Time.realtimeSinceStartup < flashlightApproachTimeout)
            yield return null;
        Assert.That((bool)Property(flashlightSwitchOn, "IsAvailable").GetValue(flashlightSwitchOn), Is.True,
            "Deniz fenere yaklaşınca yakın plandaki fiziksel açma düğmesi etkinleşmeli.");
        Assert.That(Vector3.Distance(movement.transform.position, flashlightApproach.position), Is.LessThan(0.45f),
            "Deniz fener kontrolünden önce masadaki yaklaşma noktasına yürümeli.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationFlashlight"),
            "Fener düğmesi için ayrı yakın plan kamera açılmalı.");
        GameObject flashlightBeam = Find("FlashlightInspectionBeam");
        Assert.That(flashlightBeam.activeSelf, Is.False);
        Invoke(flashlightSwitchOn, "CompletePreparedInteraction");
        Assert.That(flashlightBeam.activeSelf, Is.True, "Üst düğmeye basınca fener gerçekten yanmalı.");
        Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.True,
            "Fener açıldıktan sonra ne işe yaradığı anlatılmalı.");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That((bool)Property(flashlightSwitchOff, "IsAvailable").GetValue(flashlightSwitchOff), Is.True,
            "Açıklamadan sonra aynı düğmeyle kapatma adımı açılmalı.");
        Invoke(flashlightSwitchOff, "CompletePreparedInteraction");
        Assert.That(flashlightBeam.activeSelf, Is.False, "İkinci düğme dokunuşu feneri kapatmalı.");
        Assert.That((bool)Property(flashlightInteractable, "IsAvailable").GetValue(flashlightInteractable), Is.True,
            "Fener kapatılıp yakın plandan çıkınca çantaya sürükleme açılmalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationBag"),
            "Fener kapatılınca kamera çanta yerleştirme kadrajına uzaklaşmalı.");

        itemScreenPosition = storyCamera.WorldToScreenPoint(flashlightCollider.bounds.center);
        bagScreenPosition = storyCamera.WorldToScreenPoint(bagOpeningCollider.bounds.center);
        Assert.That((bool)InvokeWithResult(flashlightMotion, "BeginManagedDrag", itemScreenPosition), Is.True);
        Invoke(flashlightMotion, "UpdateManagedDrag", bagScreenPosition);
        Assert.That((bool)InvokeWithResult(flashlightMotion, "EndManagedDrag", bagScreenPosition), Is.True,
            "Masadaki fener açık çantanın ağzına bırakılabilmeli.");
        Invoke(flashlightInteractable, "CompletePreparedInteraction");
        yield return new WaitForSeconds((float)Property(flashlightMotion, "BagEntryDuration").GetValue(flashlightMotion) + 0.15f);
        Assert.That(flashlightSource.activeSelf, Is.False, "Fener masadan çantaya indikten sonra masada kalmamalı.");
        Assert.That(flashlightPacked.activeSelf, Is.True, "Fenerin çanta içindeki kalıcı görseli açılmalı.");
        Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.False,
            "Açıklama yerleştirmeden önce oynadığı için çantaya girişten sonra ikinci kez açılmamalı.");
        object batteries = items.Single(item =>
            Property(item, "ItemId").GetValue(item).ToString() == "Batteries");
        object batteriesInteractable = Property(batteries, "Interactable").GetValue(batteries);
        Assert.That((bool)Property(batteriesInteractable, "IsAvailable").GetValue(batteriesInteractable), Is.True,
            "Fener çantaya tamamen girdikten sonra sıradaki yedek pil açıklaması açılmalı.");

        yield return SelectRecommendedAndAdvance(director, items, "Signal", uiController);
        Invoke(director, "OnSignalRadioTuned");
        GameObject whistleHandoff = Find("Review_WhistleHandoff");
        GameObject whistleTarget = Find("Review_WhistleCanDropZone");
        Assert.That(whistleHandoff.activeSelf, Is.False,
            "Can konuşurken düdük sürüklemesi erken açılmamalı; ekrana dokunarak diyalog bitirilmeli.");
        Assert.That(whistleTarget.activeSelf, Is.False,
            "Can konuşurken bırakma hedefi henüz etkileşim almamalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationSiblingHandoff"),
            "Can konuşmaya başlamadan önce Can ve düdüğü birlikte gösteren kadraj açılmalı.");
        Bounds denizBoundsAtHandoff = ActiveRendererBounds(Find("Deniz_12"));
        Assert.That(denizBoundsAtHandoff.min.y, Is.InRange(-0.015f, 0.045f),
            "Düdük aşamasında Deniz'in ayakkabıları zeminden kopmamalı.");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(whistleHandoff.activeSelf, Is.True);
        Assert.That(whistleTarget.activeSelf, Is.True);
        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < cameraTimeout)
            yield return null;
        Collider whistleCollider = whistleHandoff.GetComponent<Collider>();
        Collider whistleTargetCollider = whistleTarget.GetComponent<Collider>();
        Vector3 whistleViewport = storyCamera.WorldToViewportPoint(whistleCollider.bounds.center);
        Vector3 targetViewport = storyCamera.WorldToViewportPoint(whistleTargetCollider.bounds.center);
        Assert.That(whistleViewport.z, Is.GreaterThan(0f));
        Assert.That(targetViewport.z, Is.GreaterThan(0f));
        Assert.That(whistleViewport.x, Is.InRange(0.08f, 0.92f));
        Assert.That(targetViewport.x, Is.InRange(0.08f, 0.92f));
        Assert.That(whistleViewport.y, Is.InRange(0.08f, 0.92f));
        Assert.That(targetViewport.y, Is.InRange(0.08f, 0.92f));
        MonoBehaviour whistleMotion = whistleHandoff.GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "DraggableItem");
        Vector2 whistleScreen = storyCamera.WorldToScreenPoint(whistleCollider.bounds.center);
        Vector2 targetScreen = storyCamera.WorldToScreenPoint(whistleTargetCollider.bounds.center);
        Assert.That((bool)InvokeWithResult(whistleMotion, "BeginManagedDrag", whistleScreen), Is.True);
        Invoke(whistleMotion, "UpdateManagedDrag", targetScreen);
        Assert.That((bool)InvokeWithResult(whistleMotion, "EndManagedDrag", targetScreen), Is.True,
            "Düdük, aynı kadrajda görünen Can hedefinin üstüne bırakılabilmeli.");
        MonoBehaviour whistleInteraction = whistleHandoff.GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "StoryInteractable");
        Invoke(whistleInteraction, "CompletePreparedInteraction");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        AssertCheckpoint(manager, "CommunicationPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Food"));

        MonoBehaviour foodDiscovery = FindInteraction("Discover_FoodCabinet");
        Assert.That((bool)Property(foodDiscovery, "IsAvailable").GetValue(foodDiscovery), Is.True,
            "The kitchen cabinet must become interactive when the food step starts.");
        Invoke(foodDiscovery, "CompletePreparedInteraction");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        yield return new WaitForSecondsRealtime(0.4f);
        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < cameraTimeout)
            yield return null;
        GameObject waterRoot = Find("WorldItem_Water");
        MonoBehaviour waterInspection = Find("WaterExpiryLabel_Unchecked").GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "StoryInteractable");
        Assert.That(waterRoot.activeInHierarchy, Is.True,
            "The water bottle must remain visible after the cabinet transition.");
        Assert.That((bool)Property(waterInspection, "IsAvailable").GetValue(waterInspection), Is.True,
            "The visible bottle label must be interactive after the dialogue.");
        Vector3 waterViewport = storyCamera.WorldToViewportPoint(
            Find("WaterExpiryLabel_Unchecked").GetComponent<BoxCollider>().bounds.center);
        Assert.That(waterViewport.z, Is.GreaterThan(0f));
        Assert.That(waterViewport.x, Is.InRange(0.06f, 0.94f));
        Assert.That(waterViewport.y, Is.InRange(0.06f, 0.94f),
            "The objective must not unlock while its water-label target is outside the shot.");
        Invoke(waterInspection, "CompletePreparedInteraction");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        object state = Property(manager, "CurrentState").GetValue(manager);
        int mistakesBefore = (int)state.GetType().GetField("mistakeCount").GetValue(state);
        object pan = items.Single(item => Property(item, "ItemId").GetValue(item).ToString() == "Pan");
        Invoke(director, "ResolveChoice", pan);
        Assert.That((int)state.GetType().GetField("mistakeCount").GetValue(state), Is.EqualTo(mistakesBefore + 1));
        Assert.That(((GameObject)Property(pan, "ConsequenceRoot").GetValue(pan)).activeSelf, Is.True);
        yield return AdvanceSubtitlesUntilIdle(uiController);

        yield return SelectRecommendedAndAdvance(director, items, "Food", uiController);
        Invoke(director, "ReviewFoodCategory");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        AssertCheckpoint(manager, "FoodPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Health"));

        Invoke(director, "DiscoverHealthCategory");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnBandageSealChecked");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        yield return SelectRecommendedAndAdvance(director, items, "Health", uiController);
        Invoke(director, "ReviewHealthCategory");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        AssertCheckpoint(manager, "HealthPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Warmth"));

        Invoke(director, "DiscoverWarmthCategory");
        yield return SelectRecommendedAndAdvance(director, items, "Warmth", uiController);
        Invoke(director, "ReviewWarmthCategory");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        AssertCheckpoint(manager, "WarmthPacked");

        Invoke(director, "OnBagWeightTested");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnConsoleRemoved");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnComfortItemChosen");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        AssertCheckpoint(manager, "BagFitted");
        Invoke(director, "OnBagPlacedAtExit");
        yield return AdvanceSubtitlesUntilIdle(uiController);

        AssertCheckpoint(manager, "PreparationComplete");
        System.Type flagType = manager.GetType().Assembly.GetType("Deprem.Story.StoryFlag");
        object bagReady = System.Enum.Parse(flagType, "BagReady");
        Assert.That((bool)manager.GetType().GetMethod("HasFlag").Invoke(manager, new[] { bagReady }), Is.True);
        System.Collections.IEnumerable completedActs = (System.Collections.IEnumerable)state.GetType().GetField("completedActs").GetValue(state);
        Assert.That(completedActs.Cast<object>().Any(value => value.ToString() == "Preparation"), Is.True);
        Assert.That(Find("EmergencyBag_Open_Packing").activeSelf, Is.False);
        Assert.That(Find("EmergencyBag_Worn").activeSelf, Is.False);
        Assert.That(Find("EmergencyBag_ExitShelf").activeSelf, Is.True);
        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator DialogueImmediatelyStopsExistingRouteAndRejectsWorldTap()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");

        if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
            yield return AdvanceSubtitlesUntilIdle(uiController);

        Vector3 destination = ReachablePointNearOpenBag();
        NavMeshAgent routeAgent = movement.GetComponent<NavMeshAgent>();
        string routeDiagnostics =
            $"onNavMesh={routeAgent != null && routeAgent.isOnNavMesh}, " +
            $"storyLocked={Property(movement, "StoryInputLocked").GetValue(movement)}, " +
            $"navigationEnabled={Field(movement, "navigationEnabled").GetValue(movement)}, " +
            $"worldBlocked={Property(uiController, "WorldInputBlocked").GetValue(uiController)}, " +
            $"position={movement.transform.position}, destination={destination}";
        Assert.That((bool)InvokeWithResult(movement, "TrySetDestination", destination), Is.True,
            "Test route must start on the baked NavMesh. " + routeDiagnostics);
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.True);

        touchManager.enabled = false;
        Vector3 stoppedPosition = movement.transform.position;
        Invoke(uiController, "ShowSubtitle", "Anne: Konuşurken Deniz bulunduğu yerde kalır.", 10f);

        Assert.That((bool)Property(uiController, "WorldInputBlocked").GetValue(uiController), Is.True);
        Assert.That((bool)Property(movement, "StoryInputLocked").GetValue(movement), Is.True,
            "Visible dialogue must lock the movement owner itself, not only the touch dispatcher.");
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.False,
            "Opening a subtitle must synchronously clear a route that was already active.");
        Assert.That((bool)Property(movement, "IsMoving").GetValue(movement), Is.False);
        NavMeshAgent movementAgent = movement.GetComponent<NavMeshAgent>();
        Assert.That(movementAgent, Is.Not.Null);
        Assert.That(movementAgent.isStopped, Is.True,
            "Dialogue UI must stop the NavMeshAgent directly, even if the touch dispatcher is disabled.");

        Assert.That((bool)InvokeWithResult(movement, "TrySetDestination", destination), Is.False,
            "No scene event or delayed callback may assign a route while dialogue is visible.");
        InvokeNonPublic(touchManager, "HandleWorldTap", new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.False,
            "A world tap received while dialogue is visible must not create a new NavMesh route.");

        yield return new WaitForSecondsRealtime(0.35f);
        Assert.That(Vector3.Distance(movement.transform.position, stoppedPosition), Is.LessThan(0.03f),
            "Deniz must remain stationary for the whole visible dialogue.");
        touchManager.enabled = true;
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(movementAgent.isStopped, Is.False,
            "The NavMeshAgent must be released only after the dialogue has fully closed.");
    }

    [UnityTest]
    public IEnumerator CameraBlendStopsFreeRouteAndRejectsTapUntilFramingSettles()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");
        MonoBehaviour cameraController = behaviours.Single(item => item != null && item.GetType().Name == "StoryCameraController");

        if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
            yield return AdvanceSubtitlesUntilIdle(uiController);

        Vector3 destination = ReachablePointNearOpenBag();
        Assert.That((bool)InvokeWithResult(movement, "TrySetDestination", destination), Is.True);
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.True);

        PropertyInfo activeZoneProperty = Property(cameraController, "ActiveZone");
        object preparationParent = System.Enum.Parse(activeZoneProperty.PropertyType, "PreparationParent");
        Invoke(cameraController, "ActivateZone", preparationParent);
        Assert.That((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController), Is.True,
            "A composed camera change must immediately own world input for the whole blend.");

        InvokeNonPublic(touchManager, "HandleWorldTap", new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.False,
            "A tap made during a camera push-in must stop the old free route and must not create a new one.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController), Is.False,
            "World navigation must return after the Cinemachine blend and pointer guard settle.");
        Assert.That((bool)Field(movement, "destinationPending").GetValue(movement), Is.False);
    }

    [UnityTest]
    public IEnumerator BlockedWorldPoint_ResolvesToNearestReachableFloor()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour movement = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MethodInfo resolve = movement.GetType().GetMethod("TryResolveReachableDestination", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(resolve, Is.Not.Null);

        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Assert.That(agent, Is.Not.Null);
        foreach (Vector3 blockedPoint in new[]
                 {
                     new Vector3(30f, 2.8f, 0f),
                     new Vector3(-30f, 2.8f, 0f),
                     new Vector3(0f, 2.8f, 30f),
                     new Vector3(0f, 2.8f, -30f)
                 })
        {
            object[] arguments = { blockedPoint, Vector3.zero };
            Assert.That((bool)resolve.Invoke(movement, arguments), Is.True, blockedPoint.ToString());
            Vector3 resolvedPoint = (Vector3)arguments[1];
            Assert.That(Vector3.Distance(resolvedPoint, blockedPoint), Is.GreaterThan(1f), blockedPoint.ToString());
            Assert.That(NavMesh.SamplePosition(resolvedPoint, out NavMeshHit hit, 0.15f, NavMesh.AllAreas), Is.True,
                blockedPoint.ToString());
            Assert.That(Vector3.Distance(hit.position, resolvedPoint), Is.LessThan(0.15f), blockedPoint.ToString());
            NavMeshPath path = new NavMeshPath();
            Assert.That(agent.CalculatePath(resolvedPoint, path), Is.True, blockedPoint.ToString());
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), blockedPoint.ToString());
        }
    }

    [UnityTest]
    public IEnumerator FurnitureDiscovery_FirstPullOpensRealDrawerAndRevealsContents()
    {
        MonoBehaviour existingManager = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existingManager != null)
        {
            Object.Destroy(existingManager.gameObject);
            yield return null;
        }
        DeleteStorySave();

        yield return LoadPreparationRebuildScene();

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        MonoBehaviour touchManager = behaviours.Single(item => item.GetType().Name == "StoryTouchManager");
        MonoBehaviour cameraController = behaviours.Single(item => item.GetType().Name == "StoryCameraController");
        MonoBehaviour uiController = behaviours.Single(item => item.GetType().Name == "StoryUIController");
        MonoBehaviour discovery = FindInteraction("Discover_SignalDrawer");
        GameObject closedNightstand = Find("SignalNightstand");
        GameObject openNightstand = Find("SignalNightstandOpen");
        Transform movingDrawer = openNightstand.GetComponentsInChildren<Transform>(true)
            .Single(item => item.name == "Nightstand_02_Door");
        float closedDrawerZ = movingDrawer.localPosition.z;
        Vector3 closedDrawerCenter = movingDrawer.GetComponent<Renderer>().bounds.center;

        yield return AdvanceSubtitlesUntilIdle(uiController);
        object signalZone = System.Enum.Parse(
            Property(cameraController, "ActiveZone").PropertyType,
            "PreparationSignal");
        Invoke(cameraController, "ActivateZone", signalZone, true);
        yield return null;
        Camera storyCamera = Camera.main;
        Assert.That(storyCamera, Is.Not.Null);
        Vector3 cameraHorizontalForward = Vector3.ProjectOnPlane(storyCamera.transform.forward, Vector3.up).normalized;
        float signalCameraDownAngle = Vector3.Angle(storyCamera.transform.forward, cameraHorizontalForward);
        Assert.That(signalCameraDownAngle, Is.InRange(24f, 38f),
            "The drawer shot must read as a forward pull, not a top-down vertical drop.");
        Invoke(discovery, "SetAvailable", true);

        Vector2 screenPosition = storyCamera.WorldToScreenPoint(
            discovery.GetComponentInChildren<Collider>(true).bounds.center);
        Assert.That(screenPosition.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(screenPosition.y, Is.InRange(0f, (float)Screen.height));
        Assert.That((float)Field(touchManager, "worldSwipeThreshold").GetValue(touchManager), Is.LessThanOrEqualTo(52f));

        InvokeNonPublic(touchManager, "HandleWorldTap", screenPosition);

        Assert.That(Field(touchManager, "pendingInteraction").GetValue(touchManager), Is.SameAs(discovery),
            "The visible drawer must win the world raycast instead of the furniture collider in front of it.");
        Assert.That((bool)Field(touchManager, "directGestureActive").GetValue(touchManager), Is.True,
            "The first pointer-down on the drawer must begin the swipe; it must not be consumed as a hidden approach tap.");
        Assert.That(Property(discovery, "InteractionGesture").GetValue(discovery).ToString(), Is.EqualTo("SwipeDown"));

        InvokeNonPublic(touchManager, "CompletePendingInteraction");
        yield return new WaitForSeconds(0.62f);

        Assert.That(closedNightstand.activeSelf, Is.False);
        Assert.That(openNightstand.activeSelf, Is.True);
        float drawerTravel = movingDrawer.localPosition.z - closedDrawerZ;
        Assert.That(drawerTravel, Is.InRange(0.16f, 0.2f),
            "The drawer must open far enough to reveal its contents without leaving its rails.");
        Bounds cabinetBounds = openNightstand.GetComponent<Renderer>().bounds;
        Bounds openDrawerBounds = movingDrawer.GetComponent<Renderer>().bounds;
        Vector3 drawerMotion = openDrawerBounds.center - closedDrawerCenter;
        Vector3 drawerToCamera = storyCamera.transform.position - closedDrawerCenter;
        drawerMotion.y = 0f;
        drawerToCamera.y = 0f;
        Assert.That(Vector3.Dot(drawerMotion.normalized, drawerToCamera.normalized), Is.GreaterThan(0.95f),
            "The drawer must travel toward the camera on its rail axis rather than downward.");
        float railOverlap = Mathf.Min(cabinetBounds.max.z, openDrawerBounds.max.z) -
                            Mathf.Max(cabinetBounds.min.z, openDrawerBounds.min.z);
        Assert.That(railOverlap, Is.GreaterThan(openDrawerBounds.size.z * 0.3f),
            "At least one third of the open drawer must remain inside the nightstand.");
        Assert.That(openDrawerBounds.max.z, Is.GreaterThan(cabinetBounds.max.z + 0.15f),
            "The open drawer must still protrude far enough to expose the selectable items.");
        string[] drawerItemNames =
        {
            "DrawerItem_Flashlight",
            "DrawerItem_Batteries",
            "DrawerItem_Whistle",
            "DrawerItem_Radio"
        };
        Assert.That(drawerItemNames.All(name => Find(name).activeInHierarchy), Is.True,
            "Dört haberleşme aracı açılan çekmecenin içinde masaya alınabilir olmalı.");
        float drawerBottom = movingDrawer.GetComponent<Renderer>().bounds.min.y;
        Assert.That(drawerItemNames.All(name =>
                Find(name).transform.position.y >= drawerBottom - 0.01f &&
                Find(name).transform.position.y <= drawerBottom + 0.08f),
            Is.True, "Çekmecedeki eşyalar havada kalmamalı; tabanları çekmeceye oturmalı.");
        Assert.That(drawerItemNames.All(name =>
        {
            GameObject item = Find(name);
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            float visualBottom = renderers.Min(renderer => renderer.bounds.min.y);
            return Mathf.Abs(visualBottom - item.transform.position.y) < 0.02f;
        }), Is.True, "Eşya pivotu değil, gerçek render edilen model çekmece tabanına oturmalı.");
        Assert.That(drawerItemNames.All(name =>
                Find(name).transform.IsChildOf(movingDrawer)),
            Is.True, "Eşyalar açılan gerçek çekmece tablasıyla birlikte hareket etmeli.");
    }

    private static MonoBehaviour FindInteraction(string interactionId)
    {
        return Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item =>
                item != null &&
                item.GetType().Name == "StoryInteractable" &&
                string.Equals(
                    Property(item, "InteractionId").GetValue(item)?.ToString(),
                    interactionId,
                    System.StringComparison.Ordinal));
    }

    private static Vector3 ReachablePointNearOpenBag()
    {
        Vector3 origin = Find("EmergencyBag_Open_Packing").transform.position +
                         new Vector3(-0.82f, 0f, -0.34f);
        Assert.That(NavMesh.SamplePosition(origin, out NavMeshHit hit, 2f, NavMesh.AllAreas), Is.True,
            "Open bag route probe must resolve to the baked preparation-room NavMesh.");
        return hit.position;
    }

    private static IEnumerator SelectRecommendedAndAdvance(MonoBehaviour director, object[] items, string category,
        MonoBehaviour uiController)
    {
        foreach (object item in items.Where(item =>
                     Property(item, "Category").GetValue(item).ToString() == category &&
                     (bool)Property(item, "Recommended").GetValue(item) &&
                     !((GameObject)Field(item, "packedVisual").GetValue(item)).activeSelf).ToArray())
        {
            GameObject sourceRoot = (GameObject)Field(item, "sourceRoot").GetValue(item);
            if (category == "Signal" && !sourceRoot.activeSelf)
            {
                Invoke(item, "StageForPacking");
                yield return new WaitForSeconds(0.8f);
            }

            object interactable = Property(item, "Interactable").GetValue(item);
            float availableTimeout = Time.realtimeSinceStartup + 2f;
            while (!(bool)Property(interactable, "IsAvailable").GetValue(interactable) &&
                   Time.realtimeSinceStartup < availableTimeout)
                yield return null;
            Assert.That((bool)Property(interactable, "IsAvailable").GetValue(interactable), Is.True,
                Property(item, "ItemId").GetValue(item) + " sıralı paketleme sırasında açılmadı.");
            Assert.That(
                (bool)InvokeWithResult(director, "TryBeginItemExplanation", interactable),
                Is.True,
                Property(item, "ItemId").GetValue(item) + " çantaya girmeden önce açıklanmalı.");
            yield return AdvanceSubtitlesUntilIdle(uiController);

            Invoke(director, "ResolveChoice", item);
            MonoBehaviour motion = (MonoBehaviour)Property(item, "LegacyBagMotion").GetValue(item);
            float placementDelay = motion != null
                ? (float)Property(motion, "BagEntryDuration").GetValue(motion) + 0.15f
                : 0.5f;
            yield return new WaitForSeconds(placementDelay);
        }
    }

    private static void DeleteStorySave()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        if (File.Exists(savePath))
            File.Delete(savePath);
    }

    private static IEnumerator AdvanceSubtitlesUntilIdle(MonoBehaviour uiController)
    {
        int advanced = 0;
        int releaseFrames = 0;
        while (true)
        {
            bool subtitleActive = (bool)Property(uiController, "SubtitleActive").GetValue(uiController);
            if (!subtitleActive)
            {
                if (!(bool)Property(uiController, "WorldInputBlocked").GetValue(uiController))
                    break;

                yield return null;
                if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
                {
                    releaseFrames = 0;
                    continue;
                }

                releaseFrames++;
                Assert.That(releaseFrames, Is.LessThanOrEqualTo(4),
                    "Dialogue release guard did not clear after the primary pointer was released.");
                continue;
            }

            Assert.That((bool)Property(uiController, "WorldInputBlocked").GetValue(uiController), Is.True,
                "A visible dialogue must consume taps before they can become world navigation.");

            Assert.That((bool)InvokeWithResult(uiController, "TryHandlePrimaryTap"), Is.True);
            Assert.That((bool)Property(uiController, "SubtitleRevealComplete").GetValue(uiController), Is.True,
                "The first tap must reveal the complete subtitle without advancing it.");
            Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.True);

            Assert.That((bool)InvokeWithResult(uiController, "TryHandlePrimaryTap"), Is.True);
            yield return null;
            advanced++;
            releaseFrames = 0;
            Assert.That(advanced, Is.LessThanOrEqualTo(8), "Dialogue callbacks formed an endless subtitle chain.");
        }
    }

    private static IEnumerator LoadPreparationRebuildScene()
    {
#if UNITY_EDITOR
        const string scenePath = "Assets/Scenes/Story_01_RebuildPreview.unity";
        Scene loadedScene = EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loadedScene.IsValid(), Is.True, scenePath);
        yield return null;
#else
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_01_RebuildPreview", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
#endif
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, name);
        return property;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field;
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static void InvokeNonPublic(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static object InvokeNonPublicWithResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }

    private static object InvokeWithResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }

    private static void AssertCheckpoint(MonoBehaviour manager, string expected)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        object checkpoint = state.GetType().GetField("checkpoint").GetValue(state);
        Assert.That(checkpoint.ToString(), Is.EqualTo(expected));
    }

    private static Bounds ActiveRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false)
            .Where(renderer => renderer != null && renderer.enabled)
            .ToArray();
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static GameObject Find(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
        Assert.That(transform, Is.Not.Null, name);
        return transform.gameObject;
    }
}
