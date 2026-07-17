using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class StoryPreparationPlayModeTests
{
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

        AsyncOperation load = SceneManager.LoadSceneAsync("Story_01_BagPreparation", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, "Story_01_BagPreparation must be available through Build Settings.");
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Story_01_BagPreparation"));
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour director = behaviours.Single(item => item != null && item.GetType().Name == "StoryPreparationDirector");
        MonoBehaviour manager = behaviours.Single(item => item != null && item.GetType().Name == "StoryGameManager");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");
        Assert.That(director, Is.Not.Null);
        Assert.That(manager, Is.Not.Null);
        object[] items = ((System.Array)Property(director, "Items").GetValue(director)).Cast<object>().ToArray();
        Assert.That(items, Has.Length.EqualTo(26));
        AssertCheckpoint(manager, "PreparationStart");

        Invoke(director, "OnFamilyPlanStarted");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        MonoBehaviour inspectBag = Find("Inspect_EmptyBag").GetComponents<MonoBehaviour>()
            .Single(item => item.GetType().Name == "StoryInteractable");
        float guidedMoveTimeout = Time.realtimeSinceStartup + 5f;
        while (!(bool)Property(inspectBag, "IsAvailable").GetValue(inspectBag) && Time.realtimeSinceStartup < guidedMoveTimeout)
            yield return null;
        Assert.That((bool)Property(inspectBag, "IsAvailable").GetValue(inspectBag), Is.True,
            "Anne konuşması bitince Deniz çantaya otomatik yaklaşmalı ve ancak sonra çanta etkileşimi açılmalı.");
        Vector3 bagStand = ((Transform)Property(inspectBag, "InteractionPoint").GetValue(inspectBag)).position;
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        Assert.That(Vector3.Distance(movement.transform.position, bagStand), Is.LessThan(1.35f));

        Invoke(director, "OnBagInspected");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Signal"));

        object flashlight = items.Single(item => Property(item, "ItemId").GetValue(item).ToString() == "Flashlight");
        GameObject flashlightSource = (GameObject)Field(flashlight, "sourceRoot").GetValue(flashlight);
        GameObject flashlightPacked = (GameObject)Field(flashlight, "packedVisual").GetValue(flashlight);
        MonoBehaviour flashlightMotion = (MonoBehaviour)Property(flashlight, "LegacyBagMotion").GetValue(flashlight);
        Vector3 sourcePosition = flashlightSource.transform.position;
        yield return new WaitForSecondsRealtime(0.9f);
        Camera storyCamera = Camera.main;
        Assert.That(storyCamera, Is.Not.Null);
        Vector2 itemScreenPosition = storyCamera.WorldToScreenPoint(flashlightMotion.transform.position);
        Vector2 bagScreenPosition = storyCamera.WorldToScreenPoint(Find("OriginalBagOpening").transform.position);
        Assert.That(itemScreenPosition.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(itemScreenPosition.y, Is.InRange(0f, (float)Screen.height));
        Assert.That(bagScreenPosition.x, Is.InRange(0f, (float)Screen.width));
        Assert.That(bagScreenPosition.y, Is.InRange(0f, (float)Screen.height));
        MonoBehaviour bagDropZone = behaviours.Single(item => item != null && item.GetType().Name == "BagDropZone");
        Assert.That((bool)InvokeWithResult(bagDropZone, "ContainsScreenPoint", bagScreenPosition, storyCamera), Is.True,
            "Ekranda görünen çanta ağzı, kamera açısı ne olursa olsun geçerli bırakma alanı olmalı.");
        object flashlightInteractable = Property(flashlight, "Interactable").GetValue(flashlight);
        InvokeNonPublic(touchManager, "HandleWorldTap", itemScreenPosition);
        Assert.That(Field(touchManager, "pendingInteraction").GetValue(touchManager), Is.SameAs(flashlightInteractable),
            "Masadaki nesneye dokunmak StoryTouchManager üzerinden doğrudan o nesneyi seçmeli.");
        InvokeNonPublic(touchManager, "BeginDirectWorldGesture", itemScreenPosition);
        Assert.That(Property(flashlightMotion, "IsDragging").GetValue(flashlightMotion), Is.True,
            "İkinci bir ekran butonu olmadan, dünya nesnesi parmak altında sürüklenmeye başlamalı.");
        Invoke(flashlightMotion, "UpdateManagedDrag", bagScreenPosition);
        Assert.That((bool)InvokeWithResult(flashlightMotion, "EndManagedDrag", bagScreenPosition), Is.True,
            "Masadan tutulan fener açık çanta ağzında bırakılınca kabul edilmeli.");
        InvokeNonPublic(touchManager, "CompletePendingInteraction");
        yield return new WaitForSeconds((float)Property(flashlightMotion, "BagEntryDuration").GetValue(flashlightMotion) + 0.15f);
        Assert.That(flashlightSource.activeSelf, Is.False, "Fener fiziksel olarak çantaya indikten sonra masa üstünde kalmamalı.");
        Assert.That(flashlightPacked.activeSelf, Is.True, "Fenerin çanta içindeki kalıcı görseli açılmalı.");
        Assert.That(Vector3.Distance(flashlightMotion.transform.position, sourcePosition), Is.GreaterThan(0.25f));
        Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.False,
            "Doğru her nesneden sonra uzun diyalog açılmamalı; akış fiziksel çanta yerleştirmeye devam etmeli.");

        yield return SelectRecommendedAndAdvance(director, items, "Signal", uiController);
        AssertCheckpoint(manager, "CommunicationPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Food"));

        object state = Property(manager, "CurrentState").GetValue(manager);
        int mistakesBefore = (int)state.GetType().GetField("mistakeCount").GetValue(state);
        object pan = items.Single(item => Property(item, "ItemId").GetValue(item).ToString() == "Pan");
        Invoke(director, "ResolveChoice", pan);
        Assert.That((int)state.GetType().GetField("mistakeCount").GetValue(state), Is.EqualTo(mistakesBefore + 1));
        Assert.That(((GameObject)Property(pan, "ConsequenceRoot").GetValue(pan)).activeSelf, Is.True);
        yield return AdvanceSubtitlesUntilIdle(uiController);

        yield return SelectRecommendedAndAdvance(director, items, "Food", uiController);
        AssertCheckpoint(manager, "FoodPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Health"));

        yield return SelectRecommendedAndAdvance(director, items, "Health", uiController);
        AssertCheckpoint(manager, "HealthPacked");
        Assert.That(Property(director, "CurrentCategory").GetValue(director).ToString(), Is.EqualTo("Warmth"));

        yield return SelectRecommendedAndAdvance(director, items, "Warmth", uiController);
        AssertCheckpoint(manager, "WarmthPacked");

        Invoke(director, "OnBagWeightTested");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnBagStrapsAdjusted");
        AssertCheckpoint(manager, "BagFitted");
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Invoke(director, "OnBagPlacedAtExit");
        yield return AdvanceSubtitlesUntilIdle(uiController);

        AssertCheckpoint(manager, "PreparationComplete");
        System.Type flagType = manager.GetType().Assembly.GetType("Deprem.Story.StoryFlag");
        object bagReady = System.Enum.Parse(flagType, "BagReady");
        Assert.That((bool)manager.GetType().GetMethod("HasFlag").Invoke(manager, new[] { bagReady }), Is.True);
        System.Collections.IEnumerable completedActs = (System.Collections.IEnumerable)state.GetType().GetField("completedActs").GetValue(state);
        Assert.That(completedActs.Cast<object>().Any(value => value.ToString() == "Preparation"), Is.True);
        Assert.That(Find("OpenEmergencyBag_OriginalBolum1").activeSelf, Is.False);
        Assert.That(Find("Deniz_WornPreparedBag").activeSelf, Is.False);
        Assert.That(Find("PreparedBag_ExitShelf").activeSelf, Is.True);
        Assert.That(Find("PreparationCompletionCard").activeSelf, Is.True);
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

        AsyncOperation load = SceneManager.LoadSceneAsync("Story_01_BagPreparation", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.2f);

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");

        if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
            yield return AdvanceSubtitlesUntilIdle(uiController);

        Vector3 destination = Find("FoodStationPoint").transform.position;
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
        yield return AdvanceSubtitlesUntilIdle(uiController);
        Assert.That(movementAgent.isStopped, Is.False,
            "The NavMeshAgent must be released only after the dialogue has fully closed.");
        touchManager.enabled = true;
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

        AsyncOperation load = SceneManager.LoadSceneAsync("Story_01_BagPreparation", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.2f);

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MonoBehaviour movement = behaviours.Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MonoBehaviour touchManager = behaviours.Single(item => item != null && item.GetType().Name == "StoryTouchManager");
        MonoBehaviour uiController = behaviours.Single(item => item != null && item.GetType().Name == "StoryUIController");
        MonoBehaviour cameraController = behaviours.Single(item => item != null && item.GetType().Name == "StoryCameraController");

        if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
            yield return AdvanceSubtitlesUntilIdle(uiController);

        Vector3 destination = Find("FoodStationPoint").transform.position;
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
        AsyncOperation load = SceneManager.LoadSceneAsync("Story_01_BagPreparation", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;
        yield return null;

        MonoBehaviour movement = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryPlayerMovement");
        MethodInfo resolve = movement.GetType().GetMethod("TryResolveReachableDestination", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(resolve, Is.Not.Null);

        foreach (Vector3 blockedPoint in new[]
                 {
                     new Vector3(6.2f, 0f, 0.5f),       // sağ duvar
                     new Vector3(7.5f, 2.8f, 0.5f),     // yüksek ve duvarın arkası
                     new Vector3(0f, 2.2f, 7.4f),       // arka duvarın arkası
                     new Vector3(-2.8f, 1.1f, 2.9f)     // sehpa üstü
                 })
        {
            object[] arguments = { blockedPoint, Vector3.zero };
            Assert.That((bool)resolve.Invoke(movement, arguments), Is.True, blockedPoint.ToString());
            Vector3 resolvedPoint = (Vector3)arguments[1];
            Assert.That(Mathf.Abs(resolvedPoint.x), Is.LessThan(5.5f), blockedPoint.ToString());
            Assert.That(resolvedPoint.z, Is.InRange(-5.5f, 6.35f), blockedPoint.ToString());
        }
    }

    private static IEnumerator SelectRecommendedAndAdvance(MonoBehaviour director, object[] items, string category,
        MonoBehaviour uiController)
    {
        foreach (object item in items.Where(item =>
                     Property(item, "Category").GetValue(item).ToString() == category &&
                     (bool)Property(item, "Recommended").GetValue(item) &&
                     ((GameObject)Field(item, "sourceRoot").GetValue(item)).activeSelf).ToArray())
        {
            Invoke(director, "ResolveChoice", item);
            yield return null;
            if ((bool)Property(uiController, "SubtitleActive").GetValue(uiController))
                yield return AdvanceSubtitlesUntilIdle(uiController);
        }
    }

    private static IEnumerator AdvanceSubtitlesUntilIdle(MonoBehaviour uiController)
    {
        int advanced = 0;
        do
        {
            Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.True);
            Assert.That((bool)Property(uiController, "WorldInputBlocked").GetValue(uiController), Is.True,
                "A visible dialogue must consume taps before they can become world navigation.");

            Assert.That((bool)InvokeWithResult(uiController, "TryHandlePrimaryTap"), Is.True);
            Assert.That((bool)Property(uiController, "SubtitleRevealComplete").GetValue(uiController), Is.True,
                "The first tap must reveal the complete subtitle without advancing it.");
            Assert.That((bool)Property(uiController, "SubtitleActive").GetValue(uiController), Is.True);

            Assert.That((bool)InvokeWithResult(uiController, "TryHandlePrimaryTap"), Is.True);
            yield return null;
            advanced++;
            Assert.That(advanced, Is.LessThanOrEqualTo(3), "Dialogue callbacks formed an endless subtitle chain.");
        }
        while ((bool)Property(uiController, "SubtitleActive").GetValue(uiController));

        // Altyazıyı kapatan fiziksel dokunuş dünyaya sızmasın diye UI, parmak/fare bırakılana
        // kadar bir kare daha bloklu kalabilir. Sonraki bağımsız rota denemesinden önce bu
        // bırakma karesini bekle; runtime korumasını yanlışlıkla test hatası sayma.
        int releaseFrames = 0;
        while ((bool)Property(uiController, "WorldInputBlocked").GetValue(uiController))
        {
            yield return null;
            releaseFrames++;
            Assert.That(releaseFrames, Is.LessThanOrEqualTo(4),
                "Dialogue release guard did not clear after the primary pointer was released.");
        }
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

    private static GameObject Find(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
        Assert.That(transform, Is.Not.Null, name);
        return transform.gameObject;
    }
}
