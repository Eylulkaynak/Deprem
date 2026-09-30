using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// This test assembly intentionally reaches Assembly-CSharp behaviours by reflection,
/// matching the existing project test contract without introducing a runtime asmdef.
/// </summary>
public sealed class MinigamePackagePlayModeTests
{
    private const int PerformanceWarmupFrames = 45;
    private const int PerformanceSampleFrames = 180;

    [UnityTest]
    public IEnumerator AftershockCover_ErrorCorrectionThenEndToEndCompletion()
    {
        yield return CompleteScene("Minigame_AftershockCover", "aftershock-cover", true);
    }

    [UnityTest]
    public IEnumerator RoomSafety_EndToEndCompletion()
    {
        yield return CompleteScene("Minigame_RoomSafety", "room-safety", false);
    }

    [UnityTest]
    public IEnumerator EmergencyBagRush_EndToEndCompletion()
    {
        yield return CompleteScene("Minigame_EmergencyBagRush", "emergency-bag-rush", false);
    }

    [UnityTest]
    public IEnumerator EmergencyCorridor_EndToEndCompletion()
    {
        yield return CompleteScene("Minigame_EmergencyCorridor", "emergency-corridor", false);
    }

    [UnityTest]
    public IEnumerator RubbleSignal_EndToEndCompletion()
    {
        yield return CompleteScene("Minigame_RubbleSignal", "rubble-signal", false);
    }

    [UnityTest]
    public IEnumerator FirstStages_AcceptRealMouseGestureInput()
    {
        string[] scenes =
        {
            "Minigame_AftershockCover",
            "Minigame_RoomSafety",
            "Minigame_EmergencyBagRush",
            "Minigame_EmergencyCorridor",
            "Minigame_RubbleSignal"
        };
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            foreach (string sceneName in scenes)
            {
                MonoBehaviour session = null;
                yield return LoadStartedSession(sceneName, value => session = value);
                SetPrivate(session, "stageTransitionSeconds", 0f);
                Array stages = GetPrivate<Array>(session, "stages");
                object stage = stages.GetValue(0);
                Array actions = PublicField<Array>(stage, "actions");
                object action = actions.Cast<object>().First(item => PublicField<bool>(item, "isCorrect"));
                Camera camera = GetPrivate<Camera>(session, "worldCamera");
                yield return DriveActionThroughRealMouse(
                    session, mouse, stage, action, camera, sceneName + " ilk aşama", true);
                Assert.That(
                    PublicField<bool>(action, "consumed") || GetProperty<int>(session, "ActiveStageIndex") > 0,
                    Is.True,
                    sceneName + " gerçek mouse gesture girdisini kabul etmedi.");
            }
        }
        finally
        {
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
        }
    }

    [UnityTest]
    public IEnumerator Hub_EightCardsNavigateAndBestResultsPersist()
    {
        string[] ids =
        {
            "firetruck-runner",
            "firefighter-extinguish",
            "evacuation-25d",
            "aftershock-cover",
            "room-safety",
            "emergency-bag-rush",
            "emergency-corridor",
            "rubble-signal"
        };
        string[] scenes =
        {
            "Story_04_FiretruckRunner",
            "Minigame_FirefighterExtinguish",
            "Minigame_Evacuation_25D",
            "Minigame_AftershockCover",
            "Minigame_RoomSafety",
            "Minigame_EmergencyBagRush",
            "Minigame_EmergencyCorridor",
            "Minigame_RubbleSignal"
        };
        string profileFile = "minigame-hub-playmode-test-" + Guid.NewGuid().ToString("N") + ".json";
        string profilePath = null;

        try
        {
            MonoBehaviour hub = null;
            yield return LoadHub(value => hub = value);
            MonoBehaviour progress = GetPrivate<MonoBehaviour>(hub, "progressManager");
            ResetProgressForTest(progress, profileFile);
            profilePath = GetProperty<string>(progress, "ProfilePath");

            for (int index = 0; index < ids.Length; index++)
                Invoke(progress, "RecordResult", ids[index], 3, 950 - index, 50, 100f + index);
            // A weaker replay increments completions without farming coins or replacing
            // any best result.
            Invoke(progress, "RecordResult", ids[0], 1, 300, 30, 180f);
            Invoke(hub, "RefreshCards");

            Assert.That(GetProperty<int>(progress, "TotalBestCoins"), Is.EqualTo(400));
            object totalCoinText = GetPrivate<object>(hub, "totalCoinText");
            Assert.That(GetProperty<string>(totalCoinText, "text"), Is.EqualTo("400 / 400 İMO COIN"));
            Array initialCards = GetPrivate<Array>(hub, "cards");
            Assert.That(initialCards.Length, Is.EqualTo(8));
            for (int index = 0; index < initialCards.Length; index++)
            {
                object card = initialCards.GetValue(index);
                Assert.That(PublicField<string>(card, "minigameId"), Is.EqualTo(ids[index]));
                Assert.That(PublicField<string>(card, "sceneName"), Is.EqualTo(scenes[index]));
                Assert.That(GetProperty<string>(PublicField<object>(card, "starsText"), "text"),
                    Is.EqualTo("3 / 3 YILDIZ"), ids[index]);
                Assert.That(GetProperty<string>(PublicField<object>(card, "coinText"), "text"),
                    Is.EqualTo("50 / 50 İMO"), ids[index]);
            }

            object firstRecord = Invoke(progress, "GetRecord", ids[0]);
            Assert.That(PublicField<int>(firstRecord, "completionCount"), Is.EqualTo(2));
            Assert.That(PublicField<int>(firstRecord, "bestCoins"), Is.EqualTo(50));
            Assert.That(PublicField<int>(firstRecord, "bestStars"), Is.EqualTo(3));

            GameObject reloadOwner = new GameObject("HubProgressReloadProbe");
            MonoBehaviour reloaded = (MonoBehaviour)reloadOwner.AddComponent(progress.GetType());
            ResetProgressForTest(reloaded, profileFile);
            Assert.That(GetProperty<int>(reloaded, "TotalBestCoins"), Is.EqualTo(400));
            object reloadedFirstRecord = Invoke(reloaded, "GetRecord", ids[0]);
            Assert.That(PublicField<int>(reloadedFirstRecord, "completionCount"), Is.EqualTo(2));
            Assert.That(PublicField<int>(reloadedFirstRecord, "bestCoins"), Is.EqualTo(50));
            Object.Destroy(reloadOwner);
            yield return null;

            for (int index = 0; index < scenes.Length; index++)
            {
                if (index > 0)
                    yield return LoadHub(value => hub = value);
                Array cards = GetPrivate<Array>(hub, "cards");
                object card = cards.GetValue(index);
                object playButton = PublicField<object>(card, "playButton");
                Assert.That(playButton, Is.Not.Null, ids[index]);
                object clickEvent = GetProperty<object>(playButton, "onClick");
                Invoke(clickEvent, "Invoke");
                float deadline = Time.realtimeSinceStartup + 5f;
                while (SceneManager.GetActiveScene().name != scenes[index] &&
                       Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scenes[index]), ids[index]);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(profilePath) && File.Exists(profilePath))
                File.Delete(profilePath);
        }
    }

    [UnityTest]
    public IEnumerator AuthoredMinigames_SteadyStateHasZeroGcAndSixtyFpsEditorBudget()
    {
        string[] scenes =
        {
            "Minigame_AftershockCover",
            "Minigame_RoomSafety",
            "Minigame_EmergencyBagRush",
            "Minigame_EmergencyCorridor",
            "Minigame_RubbleSignal"
        };

        bool previousRunInBackground = Application.runInBackground;
        int previousVSyncCount = QualitySettings.vSyncCount;
        int previousTargetFrameRate = Application.targetFrameRate;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        try
        {
            foreach (string scene in scenes)
            {
                PerformanceSample sceneBaseline = default;
                PerformanceSample sample = default;
                yield return ProfileSteadyStateComparison(
                    scene,
                    (baseline, active) =>
                    {
                        sceneBaseline = baseline;
                        sample = active;
                    });
                Debug.Log(sceneBaseline.ToString());
                Debug.Log(sample.ToString());
                Assert.That(sceneBaseline.recorderValid, Is.True, scene + " static scene baseline");
                Assert.That(sample.recorderValid, Is.True, scene);
                Assert.That(sample.directManagerUpdateGcBytes, Is.EqualTo(0),
                    scene + " manager Update steady-state GC allocation");
                Assert.That(sample.p95FrameMilliseconds, Is.LessThanOrEqualTo(17.5f), scene + " p95 frame time");
            }
        }
        finally
        {
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
            Application.runInBackground = previousRunInBackground;
        }
    }

    private static IEnumerator CompleteScene(string sceneName, string minigameId, bool verifyCorrection)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!load.isDone)
            yield return null;
        yield return null;

        MonoBehaviour session = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SingleOrDefault(item => item != null && item.GetType().Name == "MinigameSessionManager");
        Assert.That(session, Is.Not.Null, sceneName);
        Assert.That(GetProperty<string>(session, "MinigameId"), Is.EqualTo(minigameId), sceneName);
        SetPrivate(session, "stageTransitionSeconds", 0f);
        Mouse mouse = InputSystem.AddDevice<Mouse>();

        MonoBehaviour progress = GetPrivate<MonoBehaviour>(session, "progressManager");
        string profileFile = "minigame-playmode-test-" + Guid.NewGuid().ToString("N") + ".json";
        SetPrivate(progress, "profileFileName", profileFile);
        FieldInfo profileField = PrivateField(progress, "profile");
        profileField.SetValue(progress, Activator.CreateInstance(profileField.FieldType));
        SetPrivate(progress, "loaded", false);
        Invoke(progress, "LoadNow");

        float startupDeadline = Time.realtimeSinceStartup + 5f;
        while (GetProperty<int>(session, "ActiveStageIndex") < 0 && Time.realtimeSinceStartup < startupDeadline)
            yield return null;
        Assert.That(GetProperty<int>(session, "ActiveStageIndex"), Is.EqualTo(0),
            sceneName + " ilk aşama başlamadı.");

        try
        {
            if (verifyCorrection)
            {
                int stageBefore = GetProperty<int>(session, "ActiveStageIndex");
                int scoreBefore = GetProperty<int>(session, "Score");
                Array correctionStages = GetPrivate<Array>(session, "stages");
                object correctionStage = correctionStages.GetValue(stageBefore);
                object wrongAction = PublicField<Array>(correctionStage, "actions")
                    .Cast<object>()
                    .FirstOrDefault(item => !PublicField<bool>(item, "isCorrect"));
                Assert.That(wrongAction, Is.Not.Null, "Gerçek hata düzeltme aksiyonu bulunamadı.");
                Camera correctionCamera = GetPrivate<Camera>(session, "worldCamera");
                yield return DriveActionThroughRealMouse(
                    session, mouse, correctionStage, wrongAction, correctionCamera,
                    sceneName + " gerçek yanlış seçim", false);
                Assert.That(GetProperty<int>(session, "ActiveStageIndex"), Is.EqualTo(stageBefore),
                    "Yanlış karar yalnız aktif aşamayı düzeltmeli.");
                Assert.That(GetProperty<int>(session, "Score"), Is.EqualTo(scoreBefore - 100),
                    "Yanlış güvenlik kararı 100 puan düşürmeli.");
                Assert.That(GetPrivate<CanvasGroup>(session, "feedbackGroup").alpha, Is.GreaterThan(0f),
                    "Yanlış seçim görünür geri bildirim üretmeli.");
            }

            int stageCount = GetProperty<int>(session, "StageCount");
            int guard = 0;
            while (!GetProperty<bool>(session, "IsCompleted") && guard++ < stageCount + 2)
            {
                int stageBefore = GetProperty<int>(session, "ActiveStageIndex");
                yield return DriveStageThroughAuthoredInteractions(session, mouse, sceneName, stageBefore);
                float stageDeadline = Time.realtimeSinceStartup + 1f;
                while (!GetProperty<bool>(session, "IsCompleted") &&
                       GetProperty<int>(session, "ActiveStageIndex") == stageBefore &&
                       Time.realtimeSinceStartup < stageDeadline)
                    yield return null;
                Assert.That(GetProperty<bool>(session, "IsCompleted") ||
                            GetProperty<int>(session, "ActiveStageIndex") > stageBefore,
                    Is.True, sceneName + " / " + stageBefore + " ilerlemedi.");
            }

            Assert.That(GetProperty<bool>(session, "IsCompleted"), Is.True, sceneName);
            object record = Invoke(progress, "GetRecord", minigameId);
            Assert.That(record, Is.Not.Null, sceneName);
            Assert.That(PublicField<int>(record, "completionCount"), Is.EqualTo(1), sceneName);
            Assert.That(PublicField<int>(record, "bestCoins"), Is.EqualTo(50), sceneName);
            Assert.That(PublicField<int>(record, "bestStars"), Is.EqualTo(3), sceneName);
        }
        finally
        {
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
            string profilePath = GetProperty<string>(progress, "ProfilePath");
            if (File.Exists(profilePath))
                File.Delete(profilePath);
        }
    }

    private static IEnumerator LoadStartedSession(string sceneName, Action<MonoBehaviour> completed)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!load.isDone)
            yield return null;
        yield return null;

        MonoBehaviour session = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SingleOrDefault(item => item != null && item.GetType().Name == "MinigameSessionManager");
        Assert.That(session, Is.Not.Null, sceneName);
        float startupDeadline = Time.realtimeSinceStartup + 5f;
        while (GetProperty<int>(session, "ActiveStageIndex") < 0 && Time.realtimeSinceStartup < startupDeadline)
            yield return null;
        Assert.That(GetProperty<int>(session, "ActiveStageIndex"), Is.EqualTo(0), sceneName);
        completed(session);
    }

    private static IEnumerator LoadHub(Action<MonoBehaviour> completed)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Minigame_Hub", LoadSceneMode.Single);
        while (!load.isDone)
            yield return null;
        yield return null;
        MonoBehaviour hub = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SingleOrDefault(item => item != null && item.GetType().Name == "MinigameHubManager");
        Assert.That(hub, Is.Not.Null, "Minigame hub manager yüklenmedi.");
        completed(hub);
    }

    private static void ResetProgressForTest(MonoBehaviour progress, string profileFile)
    {
        SetPrivate(progress, "profileFileName", profileFile);
        FieldInfo profileField = PrivateField(progress, "profile");
        profileField.SetValue(progress, Activator.CreateInstance(profileField.FieldType));
        SetPrivate(progress, "loaded", false);
        Invoke(progress, "LoadNow");
    }

    private static IEnumerator DriveStageThroughAuthoredInteractions(
        MonoBehaviour session,
        Mouse mouse,
        string sceneName,
        int expectedStageIndex)
    {
        Array stages = GetPrivate<Array>(session, "stages");
        object stage = stages.GetValue(expectedStageIndex);
        Array actions = PublicField<Array>(stage, "actions");
        Camera camera = GetPrivate<Camera>(session, "worldCamera");
        int driven = 0;
        foreach (object action in actions)
        {
            if (!PublicField<bool>(action, "isCorrect") || PublicField<bool>(action, "consumed"))
                continue;

            yield return DriveActionThroughRealMouse(
                session, mouse, stage, action, camera,
                sceneName + " / aşama " + (expectedStageIndex + 1) + " / " + PublicField<string>(action, "actionId"),
                true);
            driven++;

            float actionDeadline = Time.realtimeSinceStartup + 1f;
            while (GetPrivate<bool>(session, "inputLocked") &&
                   GetProperty<int>(session, "ActiveStageIndex") == expectedStageIndex &&
                   Time.realtimeSinceStartup < actionDeadline)
                yield return null;
            Assert.That(PublicField<bool>(action, "consumed"), Is.True,
                sceneName + " / " + expectedStageIndex + " / " + PublicField<string>(action, "actionId") +
                " authored gesture ile tüketilmedi.");
            if (GetProperty<int>(session, "ActiveStageIndex") != expectedStageIndex)
                break;
        }
        Assert.That(driven, Is.GreaterThan(0), sceneName + " etkileşim sürücüsü doğru hedef bulamadı.");
    }

    private static IEnumerator DriveActionThroughRealMouse(
        MonoBehaviour session,
        Mouse mouse,
        object stage,
        object action,
        Camera camera,
        string context,
        bool expectConsumed)
    {
        float unlockDeadline = Time.realtimeSinceStartup + 2f;
        while (GetPrivate<bool>(session, "inputLocked") && Time.realtimeSinceStartup < unlockDeadline)
            yield return null;
        Assert.That(GetPrivate<bool>(session, "inputLocked"), Is.False, context + " input kilidi açılmadı.");

        SetPublicField(action, "authoredMoveSeconds", 0.02f);
        SetPublicField(stage, "holdDurationSeconds", 0.25f);
        Vector2 start = FindUsableScreenPoint(session, stage, action, camera, context);
        Vector2 release = GestureReleasePoint(stage, action, camera, start);
        string gesture = PublicField<object>(stage, "gesture").ToString();

        QueueMouse(mouse, start, false);
        InvokePrivate(session, "Update");
        if (gesture == "Tap" || gesture == "RepeatedTap")
        {
            int activations = gesture == "RepeatedTap"
                ? Mathf.Max(1, PublicField<int>(action, "requiredActivations"))
                : 1;
            for (int activation = 0; activation < activations; activation++)
            {
                QueueMouse(mouse, start, true);
                InvokePrivate(session, "Update");
                QueueMouse(mouse, start, false);
                InvokePrivate(session, "Update");
                yield return null;
            }
        }
        else
        {
            QueueMouse(mouse, start, true);
            InvokePrivate(session, "Update");
            if (gesture == "Hold")
            {
                float holdDeadline = Time.realtimeSinceStartup + 0.4f;
                while (!PublicField<bool>(action, "consumed") && Time.realtimeSinceStartup < holdDeadline)
                {
                    QueueMouse(mouse, start, true);
                    InvokePrivate(session, "Update");
                    yield return null;
                }
                QueueMouse(mouse, start, false);
                InvokePrivate(session, "Update");
            }
            else
            {
                Vector2 middle = Vector2.Lerp(start, release, 0.55f);
                QueueMouse(mouse, middle, true);
                InvokePrivate(session, "Update");
                QueueMouse(mouse, release, true);
                InvokePrivate(session, "Update");
                QueueMouse(mouse, release, false);
                InvokePrivate(session, "Update");
            }
        }

        float actionDeadline = Time.realtimeSinceStartup + 2f;
        while (expectConsumed && !PublicField<bool>(action, "consumed") &&
               Time.realtimeSinceStartup < actionDeadline)
            yield return null;
        Assert.That(PublicField<bool>(action, "consumed"), Is.EqualTo(expectConsumed),
            context + (expectConsumed ? " gerçek input ile tüketilmedi." : " yanlış seçim tüketilmemeliydi."));
    }

    private static Vector2 FindUsableScreenPoint(
        MonoBehaviour session,
        object stage,
        object action,
        Camera camera,
        string context)
    {
        Collider collider = PublicField<Collider>(action, "targetCollider");
        Assert.That(collider, Is.Not.Null, context);
        Bounds bounds = collider.bounds;
        Vector3[] candidates =
        {
            bounds.center,
            bounds.center + Vector3.up * bounds.extents.y * 0.45f,
            bounds.center - Vector3.up * bounds.extents.y * 0.35f,
            bounds.center + Vector3.right * bounds.extents.x * 0.35f,
            bounds.center - Vector3.right * bounds.extents.x * 0.35f
        };
        List<string> diagnostics = new List<string>();
        foreach (Vector3 candidate in candidates)
        {
            Vector3 projected = camera.WorldToScreenPoint(candidate);
            if (projected.z <= 0f || projected.x < 8f || projected.x > Screen.width - 8f ||
                projected.y < 8f || projected.y > Screen.height - 8f)
            {
                diagnostics.Add($"screen=({projected.x:F0},{projected.y:F0},{projected.z:F1}) kadraj-dışı");
                continue;
            }
            Vector2 point = projected;
            string uiHits = UiHitNames(point);
            if (!string.IsNullOrEmpty(uiHits))
            {
                diagnostics.Add($"screen=({projected.x:F0},{projected.y:F0}) UI={uiHits}");
                continue;
            }
            object resolved = InvokePrivate(session, "FindActionAtScreenPoint", stage, point);
            if (ReferenceEquals(resolved, action))
                return point;
            diagnostics.Add($"screen=({projected.x:F0},{projected.y:F0}) resolved=" +
                            (resolved == null ? "null" : PublicField<string>(resolved, "actionId")));
        }
        Assert.Fail(context + " için ekranda erişilebilir, UI altında kalmayan etkileşim noktası yok: " +
                    PublicField<string>(action, "actionId") + " | screen=" + Screen.width + "x" + Screen.height +
                    " | cameraPos=" + camera.transform.position + " cameraEuler=" + camera.transform.eulerAngles +
                    " cameraForward=" + camera.transform.forward + " | bounds=" + bounds + " | " +
                    string.Join(" ; ", diagnostics));
        return default;
    }

    private static Vector2 GestureReleasePoint(object stage, object action, Camera camera, Vector2 start)
    {
        string gesture = PublicField<object>(stage, "gesture").ToString();
        switch (gesture)
        {
            case "SwipeDown":
                return start + Vector2.down * 130f;
            case "SwipeHorizontal":
                Vector2 expected = PublicField<Vector2>(action, "expectedSwipeDirection");
                if (expected.sqrMagnitude < 0.01f)
                    expected = Vector2.right;
                return start + expected.normalized * 180f;
            case "DragToTarget":
                Transform target = PublicField<Transform>(action, "dragTarget");
                Assert.That(target, Is.Not.Null, "Sürükleme hedefi eksik.");
                Vector3 projected = camera.WorldToScreenPoint(target.position);
                Assert.That(projected.z, Is.GreaterThan(0f), "Sürükleme hedefi kameranın arkasında.");
                Vector2 drop = projected;
                Assert.That(drop.x, Is.InRange(0f, (float)Screen.width), "Sürükleme hedefi yatay kadraj dışında.");
                Assert.That(drop.y, Is.InRange(0f, (float)Screen.height), "Sürükleme hedefi dikey kadraj dışında.");
                Assert.That(IsBlockedByUi(drop), Is.False, "Sürükleme hedefi HUD altında kalıyor.");
                return drop;
            default:
                return start;
        }
    }

    private static bool IsBlockedByUi(Vector2 point)
    {
        return !string.IsNullOrEmpty(UiHitNames(point));
    }

    private static string UiHitNames(Vector2 point)
    {
        if (EventSystem.current == null)
            return string.Empty;
        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = point };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        return hits.Count == 0
            ? string.Empty
            : string.Join(",", hits.Select(hit => hit.gameObject.name).Distinct());
    }

    private static void QueueMouse(Mouse mouse, Vector2 position, bool pressed)
    {
        MouseState state = new MouseState { position = position };
        if (pressed)
            state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
        InputSystem.Update();
    }

    private static IEnumerator ProfileSteadyStateComparison(
        string sceneName,
        Action<PerformanceSample, PerformanceSample> completed)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!load.isDone)
            yield return null;
        yield return null;

        MonoBehaviour session = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "MinigameSessionManager");
        int startupGuard = 0;
        while (GetProperty<int>(session, "ActiveStageIndex") < 0 && startupGuard++ < 180)
            yield return null;
        Assert.That(GetProperty<int>(session, "ActiveStageIndex"), Is.EqualTo(0), sceneName);

        AudioSource voice = GetPrivate<AudioSource>(session, "voiceSource");
        if (voice != null)
            voice.Stop();
        for (int frame = 0; frame < PerformanceWarmupFrames; frame++)
            yield return null;

        session.enabled = false;
        PerformanceSample baseline = default;
        yield return CapturePerformance(sceneName + "/StaticSceneBaseline", result => baseline = result);
        session.enabled = true;
        for (int frame = 0; frame < PerformanceWarmupFrames; frame++)
            yield return null;
        long directManagerUpdateGcBytes = MeasureDirectManagerUpdateAllocation(session);
        PerformanceSample active = default;
        yield return CapturePerformance(sceneName + "/ActiveManager", result => active = result);
        active.directManagerUpdateGcBytes = directManagerUpdateGcBytes;
        completed(baseline, active);
    }

    private static long MeasureDirectManagerUpdateAllocation(MonoBehaviour session)
    {
        MethodInfo updateMethod = session.GetType().GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(updateMethod, Is.Not.Null, "MinigameSessionManager.Update");
        Action update = (Action)Delegate.CreateDelegate(typeof(Action), session, updateMethod);

        // Delegate creation, first-use Input calls and TMP caches are intentionally
        // warmed before the measured steady-state window. No yields occur inside the
        // measured block, so Editor/TestRunner per-frame allocations cannot pollute it.
        for (int iteration = 0; iteration < 64; iteration++)
            update();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 512; iteration++)
            update();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static IEnumerator CapturePerformance(string sceneName, Action<PerformanceSample> completed)
    {

        long[] gcBytes = new long[PerformanceSampleFrames];
        float[] frameMilliseconds = new float[PerformanceSampleFrames];
        bool recorderValid;
        using (ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(
                   ProfilerCategory.Memory,
                   "GC Allocated In Frame",
                   1))
        {
            recorderValid = gcRecorder.Valid;
            // Recorder/TestRunner setup can allocate in its first editor frames. Those
            // frames are instrumentation warm-up, not gameplay steady state.
            for (int frame = 0; frame < 30; frame++)
                yield return null;
            for (int frame = 0; frame < PerformanceSampleFrames; frame++)
            {
                yield return null;
                gcBytes[frame] = gcRecorder.LastValue;
                frameMilliseconds[frame] = Time.unscaledDeltaTime * 1000f;
            }
        }

        Array.Sort(frameMilliseconds);
        long[] sortedGcBytes = (long[])gcBytes.Clone();
        Array.Sort(sortedGcBytes);
        int percentileIndex = Mathf.Clamp(
            Mathf.CeilToInt(PerformanceSampleFrames * 0.95f) - 1,
            0,
            PerformanceSampleFrames - 1);
        int allocatingFrames = 0;
        int consecutiveAllocatingFrames = 0;
        int maximumConsecutiveAllocatingFrames = 0;
        foreach (long bytes in gcBytes)
        {
            if (bytes > 0)
            {
                allocatingFrames++;
                consecutiveAllocatingFrames++;
                maximumConsecutiveAllocatingFrames = Mathf.Max(
                    maximumConsecutiveAllocatingFrames,
                    consecutiveAllocatingFrames);
            }
            else
            {
                consecutiveAllocatingFrames = 0;
            }
        }
        completed(new PerformanceSample
        {
            sceneName = sceneName,
            recorderValid = recorderValid,
            allocatingFrames = allocatingFrames,
            maximumConsecutiveAllocatingFrames = maximumConsecutiveAllocatingFrames,
            maximumGcBytes = gcBytes.Max(),
            medianGcBytes = sortedGcBytes[sortedGcBytes.Length / 2],
            p95GcBytes = sortedGcBytes[percentileIndex],
            p95FrameMilliseconds = frameMilliseconds[percentileIndex],
            memoryBytes = Profiler.GetTotalAllocatedMemoryLong()
        });
    }

    private static FieldInfo PrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field;
    }

    private static T GetPrivate<T>(object target, string fieldName) => (T)PrivateField(target, fieldName).GetValue(target);

    private static void SetPrivate(object target, string fieldName, object value) =>
        PrivateField(target, fieldName).SetValue(target, value);

    private static T GetProperty<T>(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, propertyName);
        return (T)property.GetValue(target);
    }

    private static object Invoke(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, arguments);
    }

    private static object InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, arguments);
    }

    private static T PublicField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }

    private static void SetPublicField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private struct PerformanceSample
    {
        internal string sceneName;
        internal bool recorderValid;
        internal int allocatingFrames;
        internal int maximumConsecutiveAllocatingFrames;
        internal long maximumGcBytes;
        internal long medianGcBytes;
        internal long p95GcBytes;
        internal long directManagerUpdateGcBytes;
        internal float p95FrameMilliseconds;
        internal long memoryBytes;

        public override string ToString() =>
            $"MINIGAME_PERF scene={sceneName} samples={PerformanceSampleFrames} " +
            $"gcFrames={allocatingFrames} maxGcRun={maximumConsecutiveAllocatingFrames} " +
            $"medianGcBytes={medianGcBytes} p95GcBytes={p95GcBytes} maxGcBytes={maximumGcBytes} " +
            $"directManagerGcBytes={directManagerUpdateGcBytes} " +
            $"p95FrameMs={p95FrameMilliseconds:F2} " +
            $"memoryMB={memoryBytes / (1024f * 1024f):F1}";
    }
}
