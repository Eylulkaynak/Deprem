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
        Vector3 firstMagneticPosition = card.transform.position;
        Assert.That(
            Vector3.Distance(firstMagneticPosition, snappedSlot),
            Is.GreaterThan(0.025f),
            "Kart manyetik alana girer girmez socket merkezine sertçe ışınlanmamalı.");
        for (int frame = 0; frame < 24; frame++)
        {
            yield return new WaitForSecondsRealtime(1f / 60f);
            Invoke(draggable, "UpdateManagedDrag", nearSlot);
        }
        Assert.That(
            Vector3.Distance(card.transform.position, snappedSlot),
            Is.LessThan(Vector3.Distance(firstMagneticPosition, snappedSlot) * 0.72f),
            "Kart doğru yuvaya yaklaşınca hedefe kareler boyunca yumuşakça çekilmeli.");
        Vector2 centerSlot = camera.WorldToScreenPoint(socket.position);
        Invoke(draggable, "UpdateManagedDrag", centerSlot);
        Assert.That(Field(draggable, "currentMagneticTarget").GetValue(draggable), Is.EqualTo(socket),
            "Slot merkezinde manyetik hedef kaybolmamalı.");
        Assert.That((float)Field(draggable, "currentMagnetWeight").GetValue(draggable),
            Is.GreaterThan(0.98f), "Slot merkezindeki çekim tam yerleşim ağırlığına ulaşmalı.");
        for (int frame = 0; frame < 32; frame++)
        {
            yield return new WaitForSecondsRealtime(1f / 60f);
            Invoke(draggable, "UpdateManagedDrag", centerSlot);
        }
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
    public IEnumerator OpeningDialogue_ShowsRevisedSubtitlesWithoutStaleVoiceAudio()
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
        yield return new WaitForSecondsRealtime(1f);
        Assert.That(voiceSource.clip, Is.Null);
        Assert.That(voiceSource.isPlaying, Is.False);
        Assert.That(PropertyValue<bool>(ui, "SubtitleActive"), Is.True);
        Assert.That(PropertyValue<int>(ui, "DialogueActorCount"), Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator OpeningDialogue_UsesOnlyTheActiveCommonMouthAndKeepsGazeBounded()
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
        Assert.That(ui, Is.Not.Null);
        Invoke(ui, "ShowSubtitle",
            "Can: Oyuncak arabam da çantaya girebilir mi?\n" +
            "Anne: Önce aile planını ve gerçekten gerekli malzemeleri hazırlayalım. Yer kalırsa bir küçük eşya seçeriz.\n" +
            "Deniz: İlk kart Mahalle Parkı; üzerindeki bilgiyi okuyup doğru başlığa taşıyalım.",
            60f);
        yield return null;
        Assert.That(PropertyValue<int>(ui, "DialogueActorCount"), Is.EqualTo(3));
        Transform canMouth = Find("Can_8_FaceRig").transform.Find("Mouth_Center");
        Transform denizMouth = Find("Deniz_12_FaceRig").transform.Find("Mouth_Center");
        Transform parentMouth = Find("Anne_Ayse_FaceRig").transform.Find("Mouth_Center");
        Assert.That(canMouth, Is.Not.Null);
        Assert.That(denizMouth, Is.Not.Null);
        Assert.That(parentMouth, Is.Not.Null);

        int activationFrames = 0;
        while ((PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias") != "Can" ||
                PropertyValue<Transform>(ui, "ActiveDialogueMouth") == null) &&
               activationFrames++ < 120)
            yield return null;

        Assert.That(PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias"), Is.EqualTo("Can"));
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueMouth"), Is.SameAs(canMouth));
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueListenerRoot"), Is.Not.Null);
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueListenerRoot"),
            Is.Not.SameAs(Find("Can_8").transform));

        Vector3 denizRest = denizMouth.localScale;
        Vector3 parentRest = parentMouth.localScale;
        float canMinimum = canMouth.localScale.y;
        float canMaximum = canMinimum;
        float envelopeMinimum = float.PositiveInfinity;
        float envelopeMaximum = float.NegativeInfinity;
        int mouthSampleCount = 0;
        // Twelve frames may cover less than one subtitle pulse on a fast editor.
        // Sample a real interval while retaining all inactive-mouth and gaze assertions.
        float mouthSampleDeadline = Time.realtimeSinceStartup + 0.6f;
        while (Time.realtimeSinceStartup < mouthSampleDeadline &&
               PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias") == "Can")
        {
            canMinimum = Mathf.Min(canMinimum, canMouth.localScale.y);
            canMaximum = Mathf.Max(canMaximum, canMouth.localScale.y);
            float envelope = PropertyValue<float>(ui, "ActiveDialogueEnvelope");
            envelopeMinimum = Mathf.Min(envelopeMinimum, envelope);
            envelopeMaximum = Mathf.Max(envelopeMaximum, envelope);
            mouthSampleCount++;
            Assert.That(Vector3.Distance(denizMouth.localScale, denizRest), Is.LessThan(0.0001f),
                "Aktif olmayan Deniz'in ağzı hareket etmemeli.");
            Assert.That(Vector3.Distance(parentMouth.localScale, parentRest), Is.LessThan(0.0001f),
                "Aktif olmayan annenin ağzı hareket etmemeli.");
            Assert.That(Mathf.Abs(PropertyValue<float>(ui, "ActiveDialogueHeadYaw")),
                Is.LessThanOrEqualTo(25.05f));
            Assert.That(Mathf.Abs(PropertyValue<float>(ui, "ActiveDialogueHeadPitch")),
                Is.LessThanOrEqualTo(12.05f));
            yield return null;
        }

        Assert.That(envelopeMaximum, Is.GreaterThan(envelopeMinimum + 0.08f),
            $"Ses RMS'i veya altyazı ritmi konuşma zarfını değiştirmeli. samples={mouthSampleCount}");
        Assert.That(canMaximum, Is.GreaterThan(canMinimum + 0.001f),
            $"Aktif konuşmacının ağzı ortak UI sürücüsüyle görünür biçimde açılıp kapanmalı. " +
            $"envelope={envelopeMinimum:F3}..{envelopeMaximum:F3}");
        InvokeNonPublic(ui, "ResetSubtitleState", false);
    }

    [UnityTest]
    public IEnumerator FaceRig_BlinksAndActiveSpeakerMouthActuallyMoves()
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
        Assert.That(ui, Is.Not.Null);
        yield return AdvanceSubtitlesUntilIdle(ui);

        Transform canMouth = Find("Can_8_FaceRig").transform.Find("Mouth_Center");
        Transform denizMouth = Find("Deniz_12_FaceRig").transform.Find("Mouth_Center");
        Assert.That(canMouth, Is.Not.Null);
        Assert.That(denizMouth, Is.Not.Null);
        Transform leftEyelid = canMouth.parent.Find("Eyelid_L");
        Transform rightEyelid = canMouth.parent.Find("Eyelid_R");
        Assert.That(leftEyelid, Is.Not.Null);
        Assert.That(rightEyelid, Is.Not.Null);

        Animation faceAnimation = canMouth.parent.GetComponent<Animation>();
        Assert.That(faceAnimation, Is.Not.Null);
        Assert.That(faceAnimation.clip, Is.Not.Null);
        AnimationState blinkState = faceAnimation[faceAnimation.clip.name];
        Assert.That(blinkState, Is.Not.Null);
        faceAnimation.Stop();
        blinkState.enabled = true;
        blinkState.weight = 1f;

        blinkState.time = 1.70f;
        faceAnimation.Sample();
        float openHeight = leftEyelid.localScale.y;
        blinkState.time = 1.75f;
        faceAnimation.Sample();
        Vector3 closedStart = leftEyelid.localScale;
        Vector3 rightClosedStart = rightEyelid.localScale;
        blinkState.time = 1.87f;
        faceAnimation.Sample();
        Vector3 closedEnd = leftEyelid.localScale;
        blinkState.time = 1.93f;
        faceAnimation.Sample();
        float reopenedHeight = leftEyelid.localScale.y;

        Assert.That(closedStart.y, Is.GreaterThan(openHeight + 0.015f));
        Assert.That(closedEnd.y, Is.EqualTo(closedStart.y).Within(0.001f),
            "Blink tam kapalı görünümü en az 120 ms korumalı.");
        Assert.That(reopenedHeight, Is.LessThan(closedStart.y - 0.015f));
        Assert.That(closedStart.x, Is.LessThanOrEqualTo(0.05f));
        Assert.That(closedStart.y, Is.LessThanOrEqualTo(0.04f));
        Assert.That(closedStart.z, Is.LessThanOrEqualTo(0.01f));
        Assert.That(rightClosedStart.x, Is.LessThanOrEqualTo(0.05f));
        Assert.That(rightClosedStart.y, Is.LessThanOrEqualTo(0.04f));
        Assert.That(rightClosedStart.z, Is.LessThanOrEqualTo(0.01f));
        blinkState.enabled = false;
        faceAnimation.Play();

        Vector3 canRest = canMouth.localScale;
        Vector3 denizRest = denizMouth.localScale;
        Invoke(ui, "ShowSubtitle",
            "Can: Can şimdi ilk cümleyi söylüyor.\nDeniz: Deniz şimdi ikinci cümleyi yanıtlıyor.",
            3.2f);
        yield return null;
        Assert.That(PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias"), Is.EqualTo("Can"));
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueMouth"), Is.SameAs(canMouth));

        float canMinimum = canMouth.localScale.y;
        float canMaximum = canMinimum;
        float canSampleEnd = Time.realtimeSinceStartup + 0.55f;
        while (Time.realtimeSinceStartup < canSampleEnd &&
               PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias") == "Can")
        {
            canMinimum = Mathf.Min(canMinimum, canMouth.localScale.y);
            canMaximum = Mathf.Max(canMaximum, canMouth.localScale.y);
            Assert.That(Vector3.Distance(denizMouth.localScale, denizRest), Is.LessThan(0.0001f));
            yield return null;
        }
        Assert.That(canMaximum, Is.GreaterThan(canMinimum + 0.001f));

        float switchTimeout = Time.realtimeSinceStartup + 2f;
        while (PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias") != "Deniz" &&
               Time.realtimeSinceStartup < switchTimeout)
            yield return null;
        Assert.That(PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias"), Is.EqualTo("Deniz"),
            "Çok satırlı altyazı karakter ağırlığına göre ikinci konuşmacıya geçmeli.");
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueMouth"), Is.SameAs(denizMouth));
        Assert.That(Vector3.Distance(canMouth.localScale, canRest), Is.LessThan(0.0001f),
            "Konuşmacı değişince Can'ın ağzı rest scale'e dönmeli.");

        float denizMinimum = denizMouth.localScale.y;
        float denizMaximum = denizMinimum;
        float denizSampleEnd = Time.realtimeSinceStartup + 0.45f;
        while (Time.realtimeSinceStartup < denizSampleEnd &&
               PropertyValue<string>(ui, "ActiveDialogueSpeakerAlias") == "Deniz")
        {
            denizMinimum = Mathf.Min(denizMinimum, denizMouth.localScale.y);
            denizMaximum = Mathf.Max(denizMaximum, denizMouth.localScale.y);
            Assert.That(Vector3.Distance(canMouth.localScale, canRest), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(PropertyValue<float>(ui, "ActiveDialogueHeadYaw")),
                Is.LessThanOrEqualTo(25.05f));
            Assert.That(Mathf.Abs(PropertyValue<float>(ui, "ActiveDialogueHeadPitch")),
                Is.LessThanOrEqualTo(12.05f));
            yield return null;
        }
        Assert.That(denizMaximum, Is.GreaterThan(denizMinimum + 0.001f));

        InvokeNonPublic(ui, "ResetSubtitleState", false);
        Assert.That(Vector3.Distance(canMouth.localScale, canRest), Is.LessThan(0.0001f));
        Assert.That(Vector3.Distance(denizMouth.localScale, denizRest), Is.LessThan(0.0001f));
        Assert.That(PropertyValue<Transform>(ui, "ActiveDialogueMouth"), Is.Null);
    }

    [UnityTest]
    public IEnumerator PreparationScene_FinalBagWeightDialogueKeepsBothChildrenGrounded()
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

        MonoBehaviour director = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryPreparationDirector");
        MonoBehaviour uiController = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryUIController");
        yield return AdvanceSubtitlesUntilIdle(uiController);

        Animator denizAnimator = Find("Deniz_12").GetComponentInChildren<Animator>(true);
        Animator canAnimator = Find("Can_8").GetComponentInChildren<Animator>(true);
        denizAnimator.Play("Locomotion", 0, 0.14f);
        canAnimator.Play("Locomotion", 0, 0.58f);
        denizAnimator.Update(0f);
        canAnimator.Update(0f);

        Invoke(director, "OnBagWeightTested");
        yield return null;

        Assert.That(denizAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pick Up"), Is.False,
            "Final çanta diyaloğu Deniz'de ayakları havaya kaldıran PickUp klibini başlatmamalı.");
        Assert.That(canAnimator.GetCurrentAnimatorStateInfo(0).IsName("Call Sibling"), Is.False,
            "Final çanta diyaloğu Can'da bileği ters büken Waving klibini başlatmamalı.");
        Assert.That(ActiveRendererBounds(Find("Deniz_12")).min.y, Is.InRange(-0.05f, 0.05f),
            "Deniz final çanta diyaloğunda zeminde kalmalı.");
        Assert.That(ActiveRendererBounds(Find("Can_8")).min.y, Is.InRange(-0.05f, 0.05f),
            "Can final çanta diyaloğunda zeminde kalmalı.");
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
        object radioItem = items.Single(item =>
            Property(item, "ItemId").GetValue(item).ToString() == "Radio");
        object radioItemInteractable = Property(radioItem, "Interactable").GetValue(radioItem);
        Assert.That((bool)Property(radioItemInteractable, "IsAvailable").GetValue(radioItemInteractable), Is.True,
            "Fener çantaya girdikten sonra radyo testi açılmalı; yedek pil testten önce paketlenmemeli.");

        yield return SelectRecommendedAndAdvance(director, items, "Signal", uiController);
        GameObject whistleHandoff = Find("Review_WhistleHandoff");
        GameObject whistleTarget = Find("Review_WhistleCanDropZone");
        GameObject can = Find("Can_8");
        Transform whistleCanPose = (Transform)Field(director, "signalWhistleCanPose").GetValue(director);
        Assert.That(can.activeSelf, Is.True, "Düdük konuşması açılmadan Can sahnede etkin olmalı.");
        // Can gerçek yürüme hızıyla (1.35 m/sn) sahnenin öbür tarafından gelir;
        // eski 1.4 sn sınırı testte hareketi yarıda kesip sonraki görevleri yanlış pozda denetliyordu.
        float canArrivalTimeout = Time.realtimeSinceStartup + 4.6f;
        while ((Vector3.Distance(can.transform.position, whistleCanPose.position) >= 0.05f ||
                Property(cameraController, "ActiveZone").GetValue(cameraController).ToString() !=
                "PreparationSiblingHandoff") &&
               Time.realtimeSinceStartup < canArrivalTimeout)
            yield return null;
        Assert.That(Vector3.Distance(can.transform.position, whistleCanPose.position), Is.LessThan(0.05f),
            "Düdük adımında Can kamera dışındaki eski konumunda değil, masa yanındaki hedef pozda olmalı.");
        Assert.That(whistleHandoff.activeSelf, Is.False,
            "Can konuşurken düdük sürüklemesi erken açılmamalı; ekrana dokunarak diyalog bitirilmeli.");
        Assert.That(whistleTarget.activeSelf, Is.False,
            "Can konuşurken bırakma hedefi henüz etkileşim almamalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationSiblingHandoff"),
            "Can konuşmaya başlamadan önce Can ve düdüğü birlikte gösteren kadraj açılmalı.");
        Bounds denizBoundsAtHandoff = ActiveRendererBounds(Find("Deniz_12"));
        Assert.That(denizBoundsAtHandoff.min.y, Is.InRange(-0.05f, 0.05f),
            "Düdük aşamasında Deniz'in ayakkabıları zeminden kopmamalı.");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        float whistleReadyTimeout = Time.realtimeSinceStartup + 2f;
        while ((!whistleHandoff.activeSelf || !whistleTarget.activeSelf) &&
               Time.realtimeSinceStartup < whistleReadyTimeout)
            yield return null;
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
        Assert.That(foodDiscovery.gameObject.name, Is.EqualTo("SM_Prop_Kitchen_Counter_01_Door_01"));
        Assert.That(((GameObject)Property(foodDiscovery, "HighlightRoot").GetValue(foodDiscovery)).activeSelf,
            Is.True, "Açılacak sol kapak üzerinde yatay sürükleme göstergesi görünmeli.");
        GameObject waterRoot = Find("WorldItem_Water");
        MonoBehaviour waterInspection = Find("WaterExpiryLabel_Unchecked").GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "StoryInteractable");
        Invoke(foodDiscovery, "CompletePreparedInteraction");
        yield return null;
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationFood"),
            "Kapak açılır açılmaz su kamerasına atlanmamalı; açılma sonucu önce dolap kadrajında görülmeli.");
        Assert.That((bool)Property(waterInspection, "IsAvailable").GetValue(waterInspection), Is.False,
            "Su etiketi, dolabın açılma açıklaması bitmeden etkileşim almamalı.");
        object openingObjective = Field(uiController, "objectiveTitle").GetValue(uiController);
        Assert.That(Property(openingObjective, "text").GetValue(openingObjective),
            Is.EqualTo("DOLAP AÇILDI — İÇERİ BAK"));
        Assert.That(Find("KitchenLowCabinet").activeSelf, Is.False);
        GameObject openedKitchenCabinet = Find("KitchenCabinetDoorLeft");
        Assert.That(openedKitchenCabinet.activeSelf, Is.True);
        yield return new WaitForSecondsRealtime(0.85f);
        Transform openedLeftDoor = openedKitchenCabinet.transform.Find("SM_Prop_Kitchen_Counter_01_Door_01");
        Transform openedRightDoor = openedKitchenCabinet.transform.Find("SM_Prop_Kitchen_Counter_01_Door_02");
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(openedLeftDoor.localEulerAngles.y, -105f)), Is.LessThan(3f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(openedRightDoor.localEulerAngles.y, 105f)), Is.LessThan(3f),
            "İki kapak, kapalı dolap modelinin bir karede kaybolması yerine göz önünde yana açılmalı.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationFood"));
        yield return AdvanceSubtitlesUntilIdle(uiController);
        yield return new WaitForSecondsRealtime(0.4f);
        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
               Time.realtimeSinceStartup < cameraTimeout)
            yield return null;
        Assert.That(waterRoot.activeInHierarchy, Is.True,
            "The water bottle must remain visible after the cabinet transition.");
        Assert.That((bool)Property(waterInspection, "IsAvailable").GetValue(waterInspection), Is.True,
            "The visible bottle label must be interactive after the dialogue.");
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationWaterInspection"),
            "Dolabın açılması gösterilip açıklandıktan sonra kamera şişe etiketine geçmeli.");
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
        yield return null;
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationBandageInspection"),
            "Sargı kontrolü başlayınca kamera genel dolap kadrajında kalmamalı; mühür paketini göstermeli.");
        Renderer bandageRenderer = Find("BandageSeal_Unchecked").GetComponentInChildren<Renderer>(true);
        Vector3 bandageViewport = storyCamera.WorldToViewportPoint(bandageRenderer.bounds.center);
        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bandageViewport.z <= 0f ||
                bandageViewport.x < 0.06f || bandageViewport.x > 0.94f ||
                bandageViewport.y < 0.06f || bandageViewport.y > 0.94f) &&
               Time.realtimeSinceStartup < cameraTimeout)
        {
            yield return null;
            bandageViewport = storyCamera.WorldToViewportPoint(bandageRenderer.bounds.center);
        }
        Assert.That(bandageViewport.z, Is.GreaterThan(0f));
        Assert.That(bandageViewport.x, Is.InRange(0.06f, 0.94f));
        Assert.That(bandageViewport.y, Is.InRange(0.06f, 0.94f),
            "Sargı paketi otomatik yakın çekimde telefon kadrajında görünür olmalı.");
        Assert.That(Find("BandageSealInspection").GetComponents<MonoBehaviour>()
                .Any(component => component != null && component.GetType().Name == "StoryInteractable"),
            Is.False,
            "Sargı kontrolü tekrar basma veya basılı tutma istememeli.");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationBag"));
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

        GameObject visibleBag = Find("EmergencyBag_Open_Packing");
        MonoBehaviour bagWeightInteraction = FindInteraction("Final_TestBagWeight");
        Assert.That(visibleBag.activeInHierarchy, Is.True,
            "Ağırlık görevi açıldığında etkileşim verilen gerçek çanta gizlenmemeli.");
        Assert.That((bool)Property(bagWeightInteraction, "IsAvailable").GetValue(bagWeightInteraction), Is.True,
            "Görünür çanta doğrudan sürüklenebilir olmalı.");
        Assert.That(Property(bagWeightInteraction, "InteractionGesture").GetValue(bagWeightInteraction).ToString(),
            Is.EqualTo("DragToTarget"));
        Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
            Is.EqualTo("PreparationBag"),
            "Ağırlık görevi karakter kadrajında değil, çantayı gösteren kamerada açılmalı.");
        Renderer bagRenderer = visibleBag.GetComponentsInChildren<Renderer>(true)
            .First(renderer => renderer.gameObject.name == "EmergencyBag_Open_Visual" ||
                               renderer.transform.IsChildOf(Find("EmergencyBag_Open_Visual").transform));
        Vector3 bagViewport = storyCamera.WorldToViewportPoint(bagRenderer.bounds.center);
        cameraTimeout = Time.realtimeSinceStartup + 3f;
        while ((bagViewport.z <= 0f ||
                bagViewport.x < 0.08f || bagViewport.x > 0.92f ||
                bagViewport.y < 0.08f || bagViewport.y > 0.92f) &&
               Time.realtimeSinceStartup < cameraTimeout)
        {
            yield return null;
            bagViewport = storyCamera.WorldToViewportPoint(bagRenderer.bounds.center);
        }
        Assert.That(bagViewport.z, Is.GreaterThan(0f));
        Assert.That(bagViewport.x, Is.InRange(0.08f, 0.92f));
        Assert.That(bagViewport.y, Is.InRange(0.08f, 0.92f),
            "Oyuncu kaldırması istenen çantayı telefon kadrajında görebilmeli.");

        MonoBehaviour bagDraggable = visibleBag.GetComponents<MonoBehaviour>()
            .Single(component => component != null && component.GetType().Name == "DraggableItem");
        Transform bagLiftTarget = (Transform)Property(bagWeightInteraction, "GestureTarget")
            .GetValue(bagWeightInteraction);
        Vector2 bagDragStart = storyCamera.WorldToScreenPoint(visibleBag.transform.position);
        Vector2 bagDragEnd = storyCamera.WorldToScreenPoint(bagLiftTarget.position);
        Assert.That((bool)InvokeWithResult(bagDraggable, "BeginManagedDrag", bagDragStart), Is.True);
        Invoke(bagDraggable, "UpdateManagedDrag", Vector2.Lerp(bagDragStart, bagDragEnd, 0.65f));
        Assert.That((bool)InvokeWithResult(bagDraggable, "EndManagedDrag", bagDragEnd), Is.True,
            "Çantayı ekranda yukarıdaki hedefe sürükleyip bırakmak gerçekten kabul edilmeli.");
        Invoke(bagWeightInteraction, "CompletePreparedInteraction");
        yield return null;

        Animator denizWeightAnimator = Find("Deniz_12").GetComponentInChildren<Animator>(true);
        Animator canWeightAnimator = Find("Can_8").GetComponentInChildren<Animator>(true);
        Assert.That(denizWeightAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pick Up"), Is.False,
            "Final çanta kontrolü Deniz'de Meshy rigini havaya kaldıran KayKit PickUp klibini tetiklememeli.");
        Assert.That(canWeightAnimator.GetCurrentAnimatorStateInfo(0).IsName("Call Sibling"), Is.False,
            "Final çanta kontrolü Can'ın ayağını ters büken KayKit Waving klibini tetiklememeli.");
        Bounds denizWeightBounds = ActiveRendererBounds(Find("Deniz_12"));
        Bounds canWeightBounds = ActiveRendererBounds(Find("Can_8"));
        Assert.That(denizWeightBounds.min.y, Is.InRange(-0.05f, 0.05f),
            "Final çanta diyaloğunda Deniz zeminden kopmamalı veya zemine gömülmemeli.");
        Assert.That(canWeightBounds.min.y, Is.InRange(-0.05f, 0.05f),
            "Final çanta diyaloğunda Can zeminden kopmamalı veya zemine gömülmemeli.");
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
    public IEnumerator ScriptedFacing_TurnsSmoothlyAndFinishesWhileDialogueLocksMovement()
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
        MonoBehaviour movement = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryPlayerMovement");
        foreach (MonoBehaviour behaviour in behaviours.Where(item =>
                     item != null &&
                     (item.GetType().Name == "StoryPreparationDirector" ||
                      item.GetType().Name == "StoryTouchManager" ||
                      item.GetType().Name == "StoryCameraController" ||
                      item.GetType().Name == "StoryUIController")))
            behaviour.enabled = false;

        Invoke(movement, "SetStoryInputLocked", false);
        Invoke(movement, "Stop");
        movement.transform.rotation = Quaternion.identity;
        Vector3 target = movement.transform.position + Vector3.right * 3f;
        Quaternion expected = Quaternion.LookRotation(Vector3.right, Vector3.up);

        Invoke(movement, "FaceTowards", target);
        Assert.That(Quaternion.Angle(movement.transform.rotation, expected), Is.GreaterThan(80f),
            "FaceTowards must schedule a turn instead of teleporting the character rotation in one frame.");

        yield return null;
        float firstFrameAngle = Quaternion.Angle(movement.transform.rotation, expected);
        Assert.That(firstFrameAngle, Is.InRange(35f, 89.9f),
            "The first turn frame must visibly progress without snapping straight to the target.");

        Invoke(movement, "SetStoryInputLocked", true);
        yield return new WaitForSecondsRealtime(0.55f);
        Assert.That(Quaternion.Angle(movement.transform.rotation, expected), Is.LessThan(1.2f),
            "A dialogue lock must stop walking but allow the already-authored smooth facing turn to finish.");
        Assert.That((bool)Property(movement, "IsMoving").GetValue(movement), Is.False);
    }

    [UnityTest]
    public IEnumerator PlayerWalk_FacesTheNavMeshVelocityInsteadOfWalkingBackward()
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
        MonoBehaviour movement = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour uiController = behaviours.Single(item =>
            item != null && item.GetType().Name == "StoryUIController");
        if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
            yield return AdvanceSubtitlesUntilIdle(uiController);

        // Keep this regression focused on locomotion. The preparation director can
        // start the next authored camera/dialogue beat one frame after the intro,
        // which correctly stops free navigation but makes the facing check flaky.
        foreach (MonoBehaviour behaviour in behaviours.Where(item =>
                     item != null &&
                     (item.GetType().Name == "StoryPreparationDirector" ||
                      item.GetType().Name == "StoryTouchManager" ||
                      item.GetType().Name == "StoryCameraController" ||
                      item.GetType().Name == "StoryUIController")))
            behaviour.enabled = false;
        Invoke(movement, "SetStoryInputLocked", false);

        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Vector3 destination = ReachablePointNearOpenBag();
        Assert.That((bool)InvokeWithResult(movement, "TrySetDestination", destination), Is.True);

        float timeout = Time.realtimeSinceStartup + 2.5f;
        while (agent.velocity.sqrMagnitude < 0.12f && Time.realtimeSinceStartup < timeout)
            yield return null;
        Assert.That(agent.velocity.sqrMagnitude, Is.GreaterThan(0.12f));

        yield return new WaitForSecondsRealtime(0.22f);
        Vector3 planarVelocity = Vector3.ProjectOnPlane(agent.velocity, Vector3.up).normalized;
        Animator animator = movement.GetComponentInChildren<Animator>();
        Assert.That(animator, Is.Not.Null);
        Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Assert.That(leftUpperArm, Is.Not.Null);
        Assert.That(rightUpperArm, Is.Not.Null);
        // Measure the rendered Humanoid body from its shoulder line. Root/Visual transform
        // arrows can both look correct while an authored RootQ turns the animated body around.
        Vector3 shoulderRight =
            Vector3.ProjectOnPlane(rightUpperArm.position - leftUpperArm.position, Vector3.up).normalized;
        Vector3 visibleBodyForward = Vector3.Cross(shoulderRight, Vector3.up).normalized;
        Assert.That(Vector3.Dot(visibleBodyForward, planarVelocity), Is.GreaterThan(0.82f),
            "Deniz'in ekranda görünen yüzü yürüdüğü yöne bakmalı.");
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
        Transform drawerSlide = movingDrawer.parent;
        Vector3 closedSlidePosition = drawerSlide.position;
        Vector3 closedDrawerCenter = movingDrawer.GetComponent<Renderer>().bounds.center;

        yield return AdvanceSubtitlesUntilIdle(uiController);
        object signalZone = System.Enum.Parse(
            Property(cameraController, "ActiveZone").PropertyType,
            "PreparationSignal");
        Invoke(cameraController, "ActivateZone", signalZone, true);
        yield return new WaitForSeconds(0.6f);
        Camera storyCamera = Camera.main;
        Assert.That(storyCamera, Is.Not.Null);
        Vector3 cameraHorizontalForward = Vector3.ProjectOnPlane(storyCamera.transform.forward, Vector3.up).normalized;
        float signalCameraDownAngle = Vector3.Angle(storyCamera.transform.forward, cameraHorizontalForward);
        Assert.That(signalCameraDownAngle, Is.InRange(40f, 47f),
            "The drawer shot must keep the authored 45-degree three-quarter view.");
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
        Assert.That(Property(discovery, "InteractionGesture").GetValue(discovery).ToString(),
            Is.EqualTo("SwipeDiagonalDownRight"));

        Vector3 authoredOpenCenter = closedDrawerCenter +
                                     openNightstand.transform.TransformVector(Vector3.forward * 0.145f);
        Vector2 authoredScreenDelta =
            (Vector2)storyCamera.WorldToScreenPoint(authoredOpenCenter) -
            (Vector2)storyCamera.WorldToScreenPoint(closedDrawerCenter);
        Assert.That(authoredScreenDelta.x, Is.GreaterThan(0f));
        Assert.That(authoredScreenDelta.y, Is.LessThan(0f));
        Assert.That(Vector2.Angle(authoredScreenDelta, new Vector2(1f, -1f)), Is.LessThan(18f),
            "Çekmece kamera kadrajında düz aşağı/yana değil yaklaşık 45 derece sağ alta açılmalı.");

        InvokeNonPublic(touchManager, "CompletePendingInteraction");
        yield return new WaitForSeconds(0.62f);

        Assert.That(closedNightstand.activeSelf, Is.False);
        Assert.That(openNightstand.activeSelf, Is.True);
        Bounds cabinetBounds = openNightstand.GetComponent<Renderer>().bounds;
        Bounds openDrawerBounds = movingDrawer.GetComponent<Renderer>().bounds;
        float drawerTravel = Vector3.Distance(drawerSlide.position, closedSlidePosition);
        Assert.That(drawerTravel, Is.InRange(0.17f, 0.2f),
            "The drawer must open far enough to reveal its contents without leaving its rails.");
        Vector3 drawerMotion = drawerSlide.position - closedSlidePosition;
        Vector3 drawerToCamera = storyCamera.transform.position - closedDrawerCenter;
        drawerMotion.y = 0f;
        drawerToCamera.y = 0f;
        Assert.That(Vector3.Angle(drawerMotion, drawerToCamera), Is.InRange(32f, 48f),
            "The camera must show the rail pull from an approximately 45-degree diagonal.");
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
            string itemId = Property(item, "ItemId").GetValue(item).ToString();
            if (string.Equals(itemId, "Radio", System.StringComparison.Ordinal))
            {
                MonoBehaviour insertBattery =
                    (MonoBehaviour)Field(director, "reviewSignalRadioBatteryInsert").GetValue(director);
                MonoBehaviour powerRadio =
                    (MonoBehaviour)Field(director, "reviewSignalRadio").GetValue(director);
                MonoBehaviour removeBattery =
                    (MonoBehaviour)Field(director, "reviewSignalRadioBatteryRemove").GetValue(director);
                MonoBehaviour cameraController = Object.FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(item => item != null && item.GetType().Name == "StoryCameraController");

                Assert.That(Property(cameraController, "ActiveZone").GetValue(cameraController).ToString(),
                    Is.EqualTo("PreparationRadio"),
                    "Radyo seçildiğinde masa üzerindeki pil ve radyo yakın planı açılmalı.");
                Assert.That((bool)Property(insertBattery, "IsAvailable").GetValue(insertBattery), Is.True);
                float radioBlendTimeout = Time.realtimeSinceStartup + 3f;
                while ((bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) &&
                       Time.realtimeSinceStartup < radioBlendTimeout)
                    yield return null;
                MonoBehaviour touchManager = Object.FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(item => item != null && item.GetType().Name == "StoryTouchManager");
                Collider batteryTouchCollider = insertBattery.GetComponent<Collider>();
                Camera touchCamera = (Camera)Field(touchManager, "worldCamera").GetValue(touchManager);
                Vector2 batteryScreenPosition =
                    touchCamera.WorldToScreenPoint(batteryTouchCollider.bounds.center);
                Ray batteryRay = touchCamera.ScreenPointToRay(batteryScreenPosition);
                string batteryRayHits = string.Join(
                    " -> ",
                    Physics.RaycastAll(batteryRay, 150f, ~0, QueryTriggerInteraction.Collide)
                        .OrderBy(hit => hit.distance)
                        .Select(hit => $"{hit.collider.name}[{hit.collider.GetComponentInParent<MonoBehaviour>()?.GetType().Name}]"));
                InvokeNonPublic(touchManager, "HandleWorldTap", batteryScreenPosition);
                MonoBehaviour batteryDrag = insertBattery.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "DraggableItem");
                Assert.That(Field(touchManager, "managedDrag").GetValue(touchManager),
                    Is.SameAs(batteryDrag),
                    "The visible battery must begin dragging from a direct tap on its enlarged touch volume. Hits: " +
                    batteryRayHits +
                    $"; active={insertBattery.gameObject.activeInHierarchy}, colliderEnabled={batteryTouchCollider.enabled}, " +
                    $"bounds={batteryTouchCollider.bounds}, objectPosition={insertBattery.transform.position}, " +
                    $"screen={batteryScreenPosition}");
                Transform batteryInsertTarget =
                    (Transform)Property(insertBattery, "GestureTarget").GetValue(insertBattery);
                Vector2 batteryInsertTargetScreen = touchCamera.WorldToScreenPoint(batteryInsertTarget.position);
                Invoke(batteryDrag, "UpdateManagedDrag", batteryInsertTargetScreen);
                Assert.That(
                    (bool)InvokeWithResult(batteryDrag, "EndManagedDrag", batteryInsertTargetScreen),
                    Is.True,
                    "Masadaki gerçek yedek pil radyonun görünür pil yuvasına bırakılabilmeli.");
                InvokeNonPublic(touchManager, "ClearPendingInteraction");
                Invoke(insertBattery, "CompletePreparedInteraction");
                Assert.That(Find("Review_RadioBatteryLoose").activeSelf, Is.True,
                    "Pil takılınca aynı fiziksel hiyerarşi açık kalmalı; görsel takası yapılmamalı.");
                Assert.That(Find("Review_RadioBatteryInserted").activeSelf, Is.True,
                    "Aynı pil görselini taşıyan çıkarma etkileşimi radyoda kalmalı.");
                Bounds insertedBatteryVisualBounds =
                    ActiveRendererBounds(Find("Review_RadioBatteryInserted"));
                Bounds inspectionRadioBounds =
                    ActiveRendererBounds(Find("BagReview_EmergencyRadio"));
                Assert.That(
                    insertedBatteryVisualBounds.Intersects(inspectionRadioBounds),
                    Is.True,
                    "Takılmış pil masada ayrı durmamalı; radyo gövdesinin içine oturmalı.");
                Assert.That(insertedBatteryVisualBounds.size.y,
                    Is.GreaterThan(insertedBatteryVisualBounds.size.x * 1.35f),
                    "Takılan pil radyodan yatay çubuk gibi çıkmamalı; haznede dik durmalı.");
                Assert.That(insertedBatteryVisualBounds.max.x - inspectionRadioBounds.max.x,
                    Is.LessThan(0.08f),
                    "Pilin tamamı radyo dışına taşmamalı; yalnız tutulabilir ince yüzü görünmeli.");
                Assert.That((bool)Property(powerRadio, "IsAvailable").GetValue(powerRadio), Is.True,
                    "Pil takılınca radyonun fiziksel güç düğmesi etkinleşmeli.");
                Invoke(powerRadio, "CompletePreparedInteraction");
                Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.True,
                    "Radyo açılınca acil yayın amacı açıklanmalı.");
                yield return AdvanceSubtitlesUntilIdle(uiController);
                Assert.That((bool)Property(removeBattery, "IsAvailable").GetValue(removeBattery), Is.True,
                    "Yayın kontrolünden sonra pil radyodan çıkarılabilmeli.");
                MonoBehaviour removeBatteryDrag = removeBattery.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "DraggableItem");
                Collider removeBatteryCollider = removeBattery.GetComponent<Collider>();
                Vector2 removeBatteryScreen = touchCamera.WorldToScreenPoint(removeBatteryCollider.bounds.center);
                Transform batteryReturnTarget =
                    (Transform)Property(removeBattery, "GestureTarget").GetValue(removeBattery);
                Vector2 batteryReturnTargetScreen = touchCamera.WorldToScreenPoint(batteryReturnTarget.position);
                Assert.That(
                    (bool)InvokeWithResult(removeBatteryDrag, "BeginManagedDrag", removeBatteryScreen),
                    Is.True,
                    "Radyoya takılan aynı pil çıkarma adımında yeniden tutulabilmeli.");
                Invoke(removeBatteryDrag, "UpdateManagedDrag", batteryReturnTargetScreen);
                Assert.That(
                    (bool)InvokeWithResult(removeBatteryDrag, "EndManagedDrag", batteryReturnTargetScreen),
                    Is.True,
                    "Aynı pil radyodan çıkarılıp masadaki fiziksel yerine geri bırakılabilmeli.");
                Invoke(removeBattery, "CompletePreparedInteraction");
                yield return AdvanceSubtitlesUntilIdle(uiController);
                Assert.That((bool)Property(interactable, "IsAvailable").GetValue(interactable), Is.True,
                    "Pil çıkarıldıktan sonra radyo masadan açık çantaya sürüklenebilmeli.");
            }
            else
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

    private static T PropertyValue<T>(object target, string name)
    {
        return (T)Property(target, name).GetValue(target);
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
