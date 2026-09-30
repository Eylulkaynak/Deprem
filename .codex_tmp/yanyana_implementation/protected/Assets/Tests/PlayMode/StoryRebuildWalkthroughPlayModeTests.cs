using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Rebuild sahnelerini MOTORUN İÇİNDE gerçekten oynayan uçtan uca doğrulama.
/// Sürücü, statik denetimin göremediği katmanı test eder: etkileşimler gerçek
/// yaklaşma/NavMesh yürüyüşleriyle tamamlanır, diyaloglar gerçek dokunuş
/// API'siyle ilerletilir ve her adımda çalışma zamanı bütünlüğü doğrulanır
/// (ayaklar kalça üstüne çıkmaz/tabanlar göğe dönmez, kamera sonlu kalır ve
/// statik geometrinin içine girmez). Bölüm, kendi final checkpoint'ine
/// ulaşmazsa test, son görev metniyle birlikte düşer.
/// </summary>
public sealed class StoryRebuildWalkthroughPlayModeTests
{
#if UNITY_EDITOR
    private const int CaptureFrameRate = 30;
    private const int CaptureWidth = 1080;
    private const int CaptureHeight = 1920;

    [UnityTest, Timeout(1500000)]
    public IEnumerator Story01_Preparation_PlaysToCompletion()
    {
        yield return RunWalkthrough(
            "Assets/Scenes/Story_01_RebuildPreview.unity", "PreparationComplete", 520f);
    }

    [UnityTest, Timeout(1500000)]
    public IEnumerator Story02_HomeSafety_PlaysToCompletion()
    {
        yield return RunWalkthrough(
            "Assets/Scenes/Story_02_RebuildPreview.unity", "HomeSafetyComplete", 460f);
    }

    [UnityTest, Timeout(1500000)]
    public IEnumerator Story03_Quake_PlaysToCompletion()
    {
        yield return RunWalkthrough(
            "Assets/Scenes/Story_03_RebuildPreview.unity", "CorridorReached", 460f);
        yield return new WaitForSecondsRealtime(0.25f);
        AssertActorFraming("ClientExports/KKTC/CorridorReview", "Deniz_12", "Can_8");
    }

    [UnityTest, Timeout(1500000)]
    public IEnumerator Story04_Evacuation_PlaysToCompletion()
    {
        yield return RunWalkthrough(
            "Assets/Scenes/Story_04_RebuildPreview.unity", "AssemblyHeadcountComplete", 600f);
        yield return new WaitForSecondsRealtime(0.25f);
        AssertActorFraming("ClientExports/KKTC/ReunionReview",
            "Deniz_12", "Can_8", "Anne_Assembly_Reunion", "Baba_Assembly_Reunion");
    }

    [UnityTest, Timeout(7200000)]
    public IEnumerator RecordAllFourScenesAtNormalSpeedWithRecorder()
    {
        return RecordChapters(CaptureHeight);
    }

    [UnityTest, Timeout(7200000)]
    public IEnumerator RecordAllFourScenesAtNormalSpeedTallPortrait()
    {
        return RecordChapters(2340);
    }

    [UnityTest, Timeout(1800000)]
    public IEnumerator RecordStory04AtNormalSpeedTallPortrait()
    {
        return RecordChapters(2340, 4);
    }

    [UnityTest, Timeout(3600000)]
    public IEnumerator RecordStory03And04AtNormalSpeedTallPortrait()
    {
        return RecordChapters(2340, 3);
    }

    private static IEnumerator RecordChapters(int recordHeight, int firstChapter = 1)
    {
        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        bool hadSave = File.Exists(savePath);
        byte[] saveBackup = hadSave ? File.ReadAllBytes(savePath) : null;

        string outputFolder = Path.GetFullPath(Path.Combine("Recordings", "RebuildFull"));
        Directory.CreateDirectory(outputFolder);
        string captureName = "Story_Rebuild_" + (firstChapter == 1 ? "01-04_" : firstChapter == 3 ? "03-04_" : "04_") +
                             (recordHeight == 2340 ? "Tall_" : "Full_") +
                             DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFile = Path.Combine(outputFolder, captureName);
        string videoPath = outputFile + ".mp4";
        string statusPath = outputFile + "_STATUS.txt";
        RecorderController recorder = null;
        bool completed = false;

        File.WriteAllText(
            statusPath,
            $"[{DateTime.Now:O}] STARTED{Environment.NewLine}" +
            $"video={videoPath}{Environment.NewLine}" +
            $"resolution={CaptureWidth}x{recordHeight}{Environment.NewLine}" +
            $"fps={CaptureFrameRate}{Environment.NewLine}" +
            "speed=1x\naudio=on\n");

        try
        {
            Time.timeScale = 1f;
            recorder = StartFullWalkthroughRecorder(outputFile, recordHeight);
            Assert.That(recorder.IsRecording(), Is.True, "Unity Recorder kaydı başlayamadı.");
            yield return new WaitForSecondsRealtime(1f);

            if (firstChapter <= 2)
            {
                yield return RunWalkthrough(
                    "Assets/Scenes/Story_01_RebuildPreview.unity",
                    "PreparationComplete",
                    520f,
                    1f,
                    true,
                    false);
                AppendCaptureStatus(statusPath, "STORY_01_DONE");

                yield return RunWalkthrough(
                    "Assets/Scenes/Story_02_RebuildPreview.unity",
                    "HomeSafetyComplete",
                    460f,
                    1f,
                    true,
                    false);
                AppendCaptureStatus(statusPath, "STORY_02_DONE");
            }

            if (firstChapter <= 3)
            {
                yield return RunWalkthrough(
                    "Assets/Scenes/Story_03_RebuildPreview.unity",
                    "CorridorReached",
                    460f,
                    1f,
                    true,
                    false);
                AppendCaptureStatus(statusPath, "STORY_03_DONE");
            }

            yield return RunWalkthrough(
                "Assets/Scenes/Story_04_RebuildPreview.unity",
                "AssemblyHeadcountComplete",
                600f,
                1f,
                true,
                false);
            AppendCaptureStatus(statusPath, "STORY_04_DONE");

            yield return new WaitForSecondsRealtime(1.25f);
            recorder.StopRecording();
            yield return new WaitForSecondsRealtime(1f);

            FileInfo video = new FileInfo(videoPath);
            video.Refresh();
            Assert.That(video.Exists, Is.True, "Unity Recorder MP4 dosyasını oluşturmadı: " + videoPath);
            Assert.That(video.Length, Is.GreaterThan(1024L), "Unity Recorder MP4 dosyası boş: " + videoPath);
            completed = true;
        }
        finally
        {
            if (recorder != null && recorder.IsRecording())
                recorder.StopRecording();

            Time.captureFramerate = 0;
            Time.timeScale = 1f;

            if (hadSave)
                File.WriteAllBytes(savePath, saveBackup);
            else if (File.Exists(savePath))
                File.Delete(savePath);

            AppendCaptureStatus(statusPath, completed ? "COMPLETED" : "FAILED_OR_CANCELLED");
        }
    }

    private static IEnumerator RunWalkthrough(
        string scenePath,
        string finalCheckpoint,
        float budgetScaledSeconds,
        float playbackSpeed = 3f,
        bool preserveAuthoredDialoguePacing = false,
        bool runRuntimeIntegrityChecks = true)
    {
        yield return LoadFreshSceneByPath(scenePath);
        FootBaselines.Clear();
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour touchManager = FindBehaviour("StoryTouchManager");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        MonoBehaviour preparationDirector = TryFindBehaviour("StoryPreparationDirector");
        MonoBehaviour sequenceDirector = TryFindBehaviour("StorySequenceDirector");
        // CorridorReached is the entrance checkpoint, before five playable beats.
        // Only the director's Completed phase proves that Story03 has finished.
        bool IsChapterComplete() => CheckpointName(manager) == finalCheckpoint &&
            (sequenceDirector == null || Property(sequenceDirector, "CurrentPhase").GetValue(sequenceDirector).ToString() == "Completed");

        Time.timeScale = Mathf.Max(0.01f, playbackSpeed);
        try
        {
            float startedAt = Time.time;
            float lastProgressAt = Time.time;
            float nextIntegrityAt = 0f;
            string lastState = string.Empty;
            var cooldowns = new Dictionary<int, float>();

            while (!IsChapterComplete())
            {
                Assert.That(Time.time - startedAt, Is.LessThan(budgetScaledSeconds),
                    scenePath + " — bölüm zaman bütçesinde tamamlanmadı. Checkpoint=" +
                    CheckpointName(manager) + " Görev=" + ObjectiveText(ui));

                if (runRuntimeIntegrityChecks && Time.unscaledTime >= nextIntegrityAt)
                {
                    AssertRuntimeIntegrity(scenePath);
                    nextIntegrityAt = Time.unscaledTime + 0.5f;
                }

                string state = CheckpointName(manager) + "|" + ObjectiveText(ui);
                if (state != lastState)
                {
                    lastState = state;
                    lastProgressAt = Time.time;
                }
                Assert.That(Time.time - lastProgressAt, Is.LessThan(70f),
                    scenePath + " — akış kilitlendi (70sn ilerleme yok). Checkpoint=" +
                    CheckpointName(manager) + " Görev=" + ObjectiveText(ui));

                if ((bool)Property(ui, "SubtitleActive").GetValue(ui))
                {
                    if (!preserveAuthoredDialoguePacing)
                        InvokeWithResult(ui, "TryHandlePrimaryTap");
                    lastProgressAt = Time.time;
                    yield return null;
                    continue;
                }

                MonoBehaviour interactable = PickAvailableInteractable(cooldowns);
                if (interactable == null)
                {
                    // Tetikleme hacmine YÜRÜNEREK açılan adımlar (autoTrigger):
                    // gerçek oyuncu gibi oraya yürünür.
                    MonoBehaviour walkTarget = PickAutoTriggerTarget(cooldowns);
                    if (walkTarget != null)
                    {
                        cooldowns[walkTarget.GetInstanceID()] = Time.time + 6f;
                        Vector3 destination = (Property(walkTarget, "InteractionPoint")
                            .GetValue(walkTarget) as Transform).position;
                        InvokeWithResult(player, "SetStoryInputLocked", false);
                        InvokeWithResult(player, "SetNavigationEnabled", true);
                        if ((bool)InvokeWithResult(player, "TrySetDestination", destination))
                            lastProgressAt = Time.time;
                        yield return new WaitForSeconds(1.2f);
                        continue;
                    }
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                cooldowns[interactable.GetInstanceID()] = Time.time + 7f;

                // Story_01: paketlenecek eşyalar önce "ne işe yarar" açıklamasını
                // ister; gerçek oyuncu akışındaki gibi önce açıklama başlatılır.
                if (preparationDirector != null &&
                    GestureName(interactable) == "DragToBag" &&
                    (bool)preparationDirector.GetType()
                        .GetMethod("TryBeginItemExplanation", BindingFlags.Instance | BindingFlags.Public)
                        .Invoke(preparationDirector, new object[] { interactable }))
                {
                    lastProgressAt = Time.time;
                    yield return null;
                    continue;
                }

                // Tap/hold/drag kabulü doğrudan StoryInteractable çağrısıyla
                // taklit edilmez: gerçek kamera ışını, StoryTouchManager seçim
                // hazırlığı, hold sayacı ve managed drag bırakma yolu çalışır.
                if (UsesTouchManagerPath(GestureName(interactable)))
                {
                    yield return DriveThroughTouchManager(
                        touchManager,
                        cameraController,
                        interactable,
                        preserveAuthoredDialoguePacing);
                    lastProgressAt = Time.time;
                    yield return new WaitForSeconds(0.35f);
                    continue;
                }

                MethodInfo request = interactable.GetType()
                    .GetMethod("RequestInteraction", BindingFlags.Instance | BindingFlags.Public);
                bool accepted = (bool)request.Invoke(interactable, new object[] { player });
                if (accepted)
                    lastProgressAt = Time.time;
                yield return new WaitForSeconds(0.35f);
            }

            Assert.That(CheckpointName(manager), Is.EqualTo(finalCheckpoint), scenePath);
            Assert.That(IsChapterComplete(), Is.True, scenePath + " — final authored phase incomplete.");
            if (preserveAuthoredDialoguePacing)
            {
                // A chapter commits its checkpoint before its closing family dialogue.
                // Keep that authored ending and the completion card in the review video.
                float closingDeadline = Time.realtimeSinceStartup + 30f;
                while ((bool)Property(ui, "SubtitleActive").GetValue(ui))
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(closingDeadline),
                        scenePath + " — bölüm kapanış konuşması tamamlanmadı.");
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        finally
        {
            Time.timeScale = 1f;
        }
    }

    private static void AssertActorFraming(string folder, params string[] names)
    {
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        Directory.CreateDirectory(folder);
        var actors = names.Select(name => GameObject.Find(name)).ToArray();
        Assert.That(actors.All(actor => actor != null && actor.activeInHierarchy), Is.True,
            "Kadrajı doğrulanan aile üyeleri etkin olmalı.");
        float oldAspect = camera.aspect;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var evidence = new List<string> { "Aspect\tActor\tWorld\tViewportFeet\tViewportHead" };
        try
        {
            foreach (int height in new[] { 960, 1170 })
            {
                camera.aspect = 540f / height;
                foreach (GameObject actor in actors)
                {
                    float actorHeight = actor.name == "Can_8" ? 1.2f : actor.name == "Deniz_12" ? 1.45f : 1.78f;
                    Vector3 feet = camera.WorldToViewportPoint(actor.transform.position);
                    Vector3 head = camera.WorldToViewportPoint(actor.transform.position + Vector3.up * actorHeight);
                    evidence.Add($"{camera.aspect:F4}\t{actor.name}\t{actor.transform.position:F3}\t{feet:F3}\t{head:F3}");
                    File.WriteAllLines(folder + "/Framing.tsv", evidence);
                    Assert.That(feet.z, Is.GreaterThan(0f), actor.name);
                    Assert.That(feet.x, Is.InRange(.05f, .95f), actor.name + " feet x");
                    Assert.That(head.x, Is.InRange(.05f, .95f), actor.name + " head x");
                    Assert.That(feet.y, Is.InRange(.18f, .82f), actor.name + " feet clear of subtitles");
                    Assert.That(head.y, Is.InRange(.18f, .84f), actor.name + " head clear of objective");
                }
                RenderTexture target = RenderTexture.GetTemporary(540, height, 24);
                Texture2D frame = new Texture2D(540, height, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    frame.ReadPixels(new Rect(0, 0, 540, height), 0, 0);
                    frame.Apply();
                    File.WriteAllBytes(folder + "/Framing_" + height + ".png", frame.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = oldTarget;
                    RenderTexture.active = oldActive;
                    RenderTexture.ReleaseTemporary(target);
                    Object.DestroyImmediate(frame);
                }
            }
        }
        finally
        {
            camera.aspect = oldAspect;
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
        }
    }

    private static RecorderController StartFullWalkthroughRecorder(string outputFile, int height = CaptureHeight)
    {
        RecorderControllerSettings controllerSettings =
            ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.name = "Deprem Full Rebuild Walkthrough Recorder";
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = CaptureFrameRate;
        controllerSettings.CapFrameRate = true;
        controllerSettings.ExitPlayMode = false;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Story Rebuild 01-04 — Portrait MP4";
        movie.Enabled = true;
        movie.CaptureAudio = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = CaptureWidth,
            OutputHeight = height
        };
        movie.OutputFile = outputFile;
        controllerSettings.AddRecorderSettings(movie);

        RecorderOptions.VerboseMode = false;
        var recorder = new RecorderController(controllerSettings);
        recorder.PrepareRecording();
        recorder.StartRecording();
        return recorder;
    }

    private static void AppendCaptureStatus(string statusPath, string status)
    {
        File.AppendAllText(statusPath, $"[{DateTime.Now:O}] {status}{Environment.NewLine}");
    }

    private static bool UsesTouchManagerPath(string gesture)
    {
        return gesture == "Tap" ||
               gesture == "Approach" ||
               gesture == "RepeatedTap" ||
               gesture == "WorldHold" ||
               gesture == "DragToBag" ||
               gesture == "DragToTarget";
    }

    private static IEnumerator DriveThroughTouchManager(
        MonoBehaviour touchManager,
        MonoBehaviour cameraController,
        MonoBehaviour interactable,
        bool readableGesture = false)
    {
        string interactionId = Property(interactable, "InteractionId").GetValue(interactable) as string;
        object focusZone = Property(interactable, "FocusCameraZone").GetValue(interactable);
        if (focusZone != null && focusZone.ToString() != "None")
            InvokeWithResult(cameraController, "ActivateZone", focusZone);

        Camera camera = Field(touchManager, "worldCamera").GetValue(touchManager) as Camera;
        Assert.That(camera, Is.Not.Null, interactionId + " — StoryTouchManager worldCamera");

        // Director bir görevi görünür yaptığı karede manager/UI kilitlerini bir
        // sonraki karede açabilir. Gerçek kullanıcı da kamera ve diyalog geçişi
        // bitmeden yeni pointer-down üretemez; sürücü aynı kapıyı bekler.
        MonoBehaviour ui = Field(touchManager, "ui").GetValue(touchManager) as MonoBehaviour;
        float inputReadyDeadline = Time.realtimeSinceStartup + 8f;
        while (!(bool)Field(touchManager, "interactionsEnabled").GetValue(touchManager) ||
               (ui != null && (bool)Property(ui, "WorldInputBlocked").GetValue(ui)) ||
               (bool)Property(cameraController, "WorldNavigationBlocked").GetValue(cameraController) ||
               CinemachineIsBlending(camera))
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(inputReadyDeadline),
                interactionId + " — kamera/UI dünya dokunmasını açmadı.");
            yield return null;
        }

        // Cinemachine output ve fizik collider dünyası aynı kareye gelsin.
        yield return new WaitForSecondsRealtime(0.12f);
        Physics.SyncTransforms();

        Assert.That((bool)Field(touchManager, "directWorldGestures").GetValue(touchManager), Is.True,
            interactionId + " — rebuild dokunma yolu directWorldGestures kullanmalı.");

        Vector2 sourceScreen = TouchPoint(interactable.transform, camera, interactionId + " kaynak");
        InvokeNonPublic(touchManager, "HandleWorldTap", sourceScreen);
        string movementImmediatelyAfterTap = MovementDiagnostics(touchManager, interactable);
        yield return null;

        bool available = (bool)Property(interactable, "IsAvailable").GetValue(interactable);
        object pending = Field(touchManager, "pendingInteraction").GetValue(touchManager);
        Assert.That(!available || ReferenceEquals(pending, interactable), Is.True,
            interactionId + " — gerçek dünya ışını doğru StoryInteractable'ı seçmedi. " +
            TouchRayDiagnostics(camera, sourceScreen));

        float prepareDeadline = Time.realtimeSinceStartup + 15f;
        while ((bool)Property(interactable, "IsAvailable").GetValue(interactable) &&
               !(bool)Field(touchManager, "pendingPrepared").GetValue(touchManager))
        {
            if (Time.realtimeSinceStartup >= prepareDeadline)
            {
                Assert.Fail(interactionId +
                            " — dokunma sonrası yaklaşma/hazırlık tamamlanmadı. " +
                            "afterTap={" + movementImmediatelyAfterTap + "}, timeout={" +
                            MovementDiagnostics(touchManager, interactable) + "}");
            }
            Assert.That(Field(touchManager, "pendingInteraction").GetValue(touchManager),
                Is.SameAs(interactable), interactionId + " — hazırlık sırasında hedef kayboldu.");
            yield return null;
        }

        if (!(bool)Property(interactable, "IsAvailable").GetValue(interactable))
            yield break; // Tap ve Approach, hazır olur olmaz manager içinde tamamlanır.

        string gesture = GestureName(interactable);
        sourceScreen = TouchPoint(interactable.transform, camera, interactionId + " jest kaynağı");
        if (gesture == "RepeatedTap")
        {
            int required = (int)Property(interactable, "RequiredGestureCount").GetValue(interactable);
            int guard = 0;
            while ((bool)Property(interactable, "IsAvailable").GetValue(interactable) && guard++ <= required + 1)
            {
                InvokeNonPublic(touchManager, "BeginDirectWorldGesture", sourceScreen);
                yield return null;
            }
        }
        else if (gesture == "WorldHold")
        {
            if (!(bool)Field(touchManager, "worldHoldActive").GetValue(touchManager))
                InvokeNonPublic(touchManager, "BeginDirectWorldGesture", sourceScreen);
            Assert.That((bool)Field(touchManager, "worldHoldActive").GetValue(touchManager), Is.True,
                interactionId + " — manager hold hareketini başlatmadı.");
            float startedAt = (float)Field(touchManager, "worldHoldStartedAt").GetValue(touchManager);
            float duration = (float)Field(touchManager, "worldHoldDuration").GetValue(touchManager);
            InvokeNonPublic(touchManager, "UpdateWorldHoldAt", sourceScreen, startedAt + duration + 0.05f);
            yield return null;
        }
        else if (gesture == "DragToBag" || gesture == "DragToTarget")
        {
            if (Field(touchManager, "managedDrag").GetValue(touchManager) == null)
                InvokeNonPublic(touchManager, "BeginDirectWorldGesture", sourceScreen);
            MonoBehaviour drag = Field(touchManager, "managedDrag").GetValue(touchManager) as MonoBehaviour;
            Assert.That(drag, Is.Not.Null, interactionId + " — manager DraggableItem sürüklemesini başlatmadı.");

            Transform target = Property(interactable, "GestureTarget").GetValue(interactable) as Transform;
            if (target == null && gesture == "DragToBag")
            {
                MonoBehaviour dropZone = Property(drag, "DropZoneOverride").GetValue(drag) as MonoBehaviour;
                dropZone ??= Object.FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .FirstOrDefault(component => component != null && component.GetType().Name == "BagDropZone");
                target = dropZone != null ? dropZone.transform : null;
            }
            Assert.That(target, Is.Not.Null, interactionId + " — sürükleme hedefi eksik.");
            Vector2 targetScreen = TouchPoint(target, camera, interactionId + " bırakma hedefi");
            if (readableGesture)
            {
                float gestureStart = Time.unscaledTime;
                while (Time.unscaledTime - gestureStart < .7f)
                {
                    float progress = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - gestureStart) / .7f);
                    InvokeWithResult(drag, "UpdateManagedDrag", Vector2.Lerp(sourceScreen, targetScreen, progress));
                    yield return null;
                }
            }
            InvokeWithResult(drag, "UpdateManagedDrag", targetScreen);
            bool accepted = (bool)InvokeNonPublic(touchManager, "ReleaseManagedDrag", targetScreen);
            Assert.That(accepted, Is.True,
                interactionId + " — gerçek managed drag hedefte kabul edilmedi.");
            yield return null;
        }

        Assert.That((bool)Property(interactable, "IsAvailable").GetValue(interactable), Is.False,
            interactionId + " — StoryTouchManager jesti etkileşimi tamamlamadı.");
    }

    private static Vector2 TouchPoint(Transform root, Camera camera, string label)
    {
        Transform hotspot = root.Find("MobileTouchHotspot");
        Collider[] colliders = (hotspot != null ? hotspot : root)
            .GetComponentsInChildren<Collider>(true)
            .Where(candidate => candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(colliders, Is.Not.Empty, label + " — etkin dokunma collider'ı yok.");
        Bounds bounds = colliders[0].bounds;
        foreach (Collider collider in colliders.Skip(1))
            bounds.Encapsulate(collider.bounds);

        Vector3 screen = camera.WorldToScreenPoint(bounds.center);
        string framing = $" target={bounds.center}, camera={camera.transform.position}, " +
                         $"forward={camera.transform.forward}, screen={screen}, " +
                         $"pixels={camera.pixelWidth}x{camera.pixelHeight}";
        Assert.That(screen.z, Is.GreaterThan(0f), label + " — kamera arkasında." + framing);
        Assert.That(screen.x, Is.InRange(0f, (float)camera.pixelWidth), label + " — yatay ekran dışında." + framing);
        Assert.That(screen.y, Is.InRange(0f, (float)camera.pixelHeight), label + " — dikey ekran dışında." + framing);
        return new Vector2(screen.x, screen.y);
    }

    private static string TouchRayDiagnostics(Camera camera, Vector2 screenPosition)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        string hits = string.Join(" -> ", Physics.RaycastAll(
                ray, 150f, ~0, QueryTriggerInteraction.Collide)
            .OrderBy(hit => hit.distance)
            .Take(10)
            .Select(hit =>
            {
                MonoBehaviour interaction = hit.collider.GetComponentsInParent<MonoBehaviour>(true)
                    .FirstOrDefault(component => component != null && component.GetType().Name == "StoryInteractable");
                string id = interaction != null
                    ? Property(interaction, "InteractionId").GetValue(interaction) as string
                    : "-";
                return $"{hit.collider.name}[{id},trigger={hit.collider.isTrigger}]";
            }));
        return $"screen={screenPosition}, camera={camera.transform.position}, hits={hits}";
    }

    private static string MovementDiagnostics(MonoBehaviour touchManager, MonoBehaviour interactable)
    {
        MonoBehaviour player = Field(touchManager, "player").GetValue(touchManager) as MonoBehaviour;
        if (player == null)
            return "player=<null>";

        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        Transform point = Property(interactable, "InteractionPoint").GetValue(interactable) as Transform;
        object destinationPending = Field(player, "destinationPending").GetValue(player);
        object activeDestination = Field(player, "activeDestination").GetValue(player);
        object navigationEnabled = Field(player, "navigationEnabled").GetValue(player);
        object inputLocked = Field(player, "storyInputLocked").GetValue(player);
        UnityEngine.AI.NavMeshHit exactHit = default;
        bool exactSample = point != null && UnityEngine.AI.NavMesh.SamplePosition(
            point.position,
            out exactHit,
            0.25f,
            agent != null ? agent.areaMask : UnityEngine.AI.NavMesh.AllAreas);
        var directPath = new UnityEngine.AI.NavMeshPath();
        bool directPathResult = exactSample && agent != null && agent.isOnNavMesh &&
                                agent.CalculatePath(exactHit.position, directPath);
        string obstacles = string.Join(",", Object.FindObjectsByType<UnityEngine.AI.NavMeshObstacle>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Select(obstacle => $"{obstacle.transform.parent?.name}/{obstacle.name}@{obstacle.transform.position}"));
        return $"player={player.transform.position}, point={(point != null ? point.position.ToString() : "<null>")}, " +
               $"activeDestination={activeDestination}, destinationPending={destinationPending}, " +
               $"navigationEnabled={navigationEnabled}, inputLocked={inputLocked}, " +
               $"agentOnMesh={agent != null && agent.isOnNavMesh}, stopped={agent != null && agent.isStopped}, " +
               $"hasPath={agent != null && agent.hasPath}, pathPending={agent != null && agent.pathPending}, " +
               $"pathStatus={(agent != null ? agent.pathStatus.ToString() : "<null>")}, " +
               $"remaining={(agent != null ? agent.remainingDistance : -1f):F3}, " +
               $"velocity={(agent != null ? agent.velocity.ToString() : "<null>")}, " +
               $"exactSample={exactSample}@{(exactSample ? exactHit.position.ToString() : "-")}, " +
               $"directPath={directPathResult}/{directPath.status}, obstacles=[{obstacles}]";
    }

    // Oyuncuya ekranda öğretilen sabit sıralar (ör. Çök-Kapan-Tutun mini oyunu):
    // sürücü de aynı sırayı izler; diğer her şeyde sahne sırası kullanılır.
    private static readonly string[] TaughtOrder =
    {
        "quake.cover.crouch",
        "quake.cover.head",
        "quake.cover.grip"
    };

    private static MonoBehaviour PickAvailableInteractable(Dictionary<int, float> cooldowns)
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(item => item != null && item.GetType().Name == "StoryInteractable")
            .Where(item => (bool)Property(item, "IsAvailable").GetValue(item))
            .Where(item => (bool)Property(item, "WorldSelectable").GetValue(item))
            .Where(item => Property(item, "InteractionKind").GetValue(item).ToString() != "UnsafeChoice")
            .Where(item => !cooldowns.TryGetValue(item.GetInstanceID(), out float until) || Time.time >= until)
            .OrderBy(item =>
            {
                int taught = Array.IndexOf(
                    TaughtOrder,
                    Property(item, "InteractionId").GetValue(item) as string);
                return taught < 0 ? int.MaxValue : taught;
            })
            .ThenBy(item => item.transform.GetSiblingIndex())
            .FirstOrDefault();
    }

    private static MonoBehaviour PickAutoTriggerTarget(Dictionary<int, float> cooldowns)
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(item => item != null && item.GetType().Name == "StoryInteractable")
            .Where(item => (bool)Property(item, "IsAvailable").GetValue(item))
            .Where(item => !(bool)Property(item, "WorldSelectable").GetValue(item))
            .Where(item => Property(item, "InteractionKind").GetValue(item).ToString() != "UnsafeChoice")
            .Where(item => !cooldowns.TryGetValue(item.GetInstanceID(), out float until) || Time.time >= until)
            .FirstOrDefault();
    }

    private static void AssertRuntimeIntegrity(string scenePath)
    {
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null, scenePath + " — ana kamera kayboldu.");
        Vector3 cameraPosition = camera.transform.position;
        Assert.That(
            !float.IsNaN(cameraPosition.x) && !float.IsNaN(cameraPosition.y) && !float.IsNaN(cameraPosition.z) &&
            cameraPosition.magnitude < 500f,
            scenePath + " — kamera konumu bozuldu: " + cameraPosition);

        if (!CinemachineIsBlending(camera))
        {
            string shot = ActiveVirtualCameraName(camera);
            if (shot == "CM_PreparationOverview_Rebuild" || shot == "CM_PreparationDeniz_Rebuild" ||
                shot == "CM_HomeOverview_Rebuild")
            {
                Assert.That(cameraPosition.y, Is.InRange(0.9f, 3.9f),
                    shot + " — takip kamerası apartmanın tamamlanmış iç hacminden çıktı.");
                MonoBehaviour actor = TryFindBehaviour("StoryPlayerMovement");
                Assert.That(actor, Is.Not.Null);
                Assert.That(Vector3.Distance(cameraPosition, actor.transform.position + Vector3.up),
                    Is.GreaterThan(1.15f), shot + " — takip kamerası karakterin gövdesine girdi.");
            }
            // Yalnız GÖRÜŞÜ KAPATAN gömülme hatadır: kameranın arkasında kalan
            // duvar görüntüyü etkilemez, önündeki 13 cm'lik gövde ise ekranı kapatır.
            Collider blocking = Physics
                .OverlapSphere(cameraPosition, 0.13f, ~0, QueryTriggerInteraction.Ignore)
                .FirstOrDefault(candidate =>
                    candidate.GetComponentInParent<Animator>() == null &&
                    Vector3.Dot(
                        candidate.ClosestPoint(cameraPosition) - cameraPosition,
                        camera.transform.forward) > 0f);
            Assert.That(blocking, Is.Null,
                scenePath + " — kamera statik geometrinin içinde: " +
                (blocking != null ? blocking.transform.name : string.Empty) +
                " Kamera=" + ActiveVirtualCameraName(camera) +
                " Konum=" + cameraPosition);
        }

        MonoBehaviour stateManager = TryFindBehaviour("StoryGameManager");
        string runtimeCheckpoint = stateManager != null ? CheckpointName(stateManager) : "None";
        foreach (Animator animator in Object.FindObjectsByType<Animator>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!animator.isHuman || !animator.gameObject.activeInHierarchy)
                continue;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            // Çömelme/korunma pozları ayakları bilerek toplar; sapma ölçütleri
            // yalnız ayakta duruş/yürüyüş için anlamlıdır.
            bool authoredCrouch = animator.GetCurrentAnimatorClipInfo(0)
                .Any(info => info.clip != null &&
                             (info.clip.name.Contains("Cover") || info.clip.name.Contains("Crouch")));
            string activeClips = string.Join(",", animator.GetCurrentAnimatorClipInfo(0)
                .Where(info => info.clip != null)
                .Select(info => info.clip.name));
            foreach (HumanBodyBones bone in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                Transform foot = animator.GetBoneTransform(bone);
                if (foot == null || hips == null)
                    continue;

                // Kemik eksenleri ve kalça yüksekliği rig'e göre değişir; mutlak
                // eşik yerine sahnenin ilk (bilinen-iyi idle) karesi taban alınır.
                // Bu tabandan büyük sapma gerçek bir bozuk poz sınıfıdır.
                int key = foot.GetInstanceID();
                float footToHips = foot.position.y - hips.position.y;
                Vector3 relativeUp = Quaternion.Inverse(animator.transform.rotation) * foot.up;
                if (!FootBaselines.TryGetValue(key, out (float height, Vector3 up) baseline))
                {
                    FootBaselines[key] = (footToHips, relativeUp);
                    continue;
                }
                if (authoredCrouch)
                    continue;

                Assert.That(Mathf.Abs(footToHips - baseline.height), Is.LessThan(0.45f),
                    scenePath + " — " + CharacterName(animator) +
                    " ayağı kalçaya göre ilk duruşundan çok kaydı (bozuk poz). " +
                    $"checkpoint={runtimeCheckpoint}, clips={activeClips}, bone={bone}, " +
                    $"baselineHeight={baseline.height:F3}, currentHeight={footToHips:F3}");
                Assert.That(Vector3.Angle(baseline.up, relativeUp), Is.LessThan(120f),
                    scenePath + " — " + CharacterName(animator) +
                    " ayağı ilk duruşuna göre ters döndü (bozuk poz). " +
                    $"checkpoint={runtimeCheckpoint}, clips={activeClips}, bone={bone}, " +
                    $"baselineUp={baseline.up}, currentUp={relativeUp}");
            }
        }
    }

    private static readonly Dictionary<int, (float height, Vector3 up)> FootBaselines =
        new Dictionary<int, (float, Vector3)>();

    private static string CharacterName(Animator animator)
    {
        Transform current = animator.transform;
        while (current.parent != null && current.parent.parent != null)
            current = current.parent;
        return current.name;
    }

    private static bool CinemachineIsBlending(Camera camera)
    {
        Component brain = camera.GetComponent("CinemachineBrain");
        if (brain == null)
            return false;
        PropertyInfo property = brain.GetType().GetProperty("IsBlending", BindingFlags.Instance | BindingFlags.Public);
        return property != null && (bool)property.GetValue(brain);
    }

    private static string ActiveVirtualCameraName(Camera camera)
    {
        Component brain = camera != null ? camera.GetComponent("CinemachineBrain") : null;
        if (brain == null)
            return "None";

        PropertyInfo property = brain.GetType().GetProperty(
            "ActiveVirtualCamera", BindingFlags.Instance | BindingFlags.Public);
        object activeCamera = property?.GetValue(brain);
        if (activeCamera == null)
            return "None";

        PropertyInfo nameProperty = activeCamera.GetType().GetProperty(
            "Name", BindingFlags.Instance | BindingFlags.Public);
        return nameProperty?.GetValue(activeCamera)?.ToString() ?? activeCamera.ToString();
    }

    private static string ObjectiveText(MonoBehaviour ui)
    {
        PropertyInfo property = ui.GetType()
            .GetProperty("CurrentObjectiveTitle", BindingFlags.Instance | BindingFlags.Public);
        if (property != null)
            return property.GetValue(ui) as string ?? string.Empty;
        return string.Empty;
    }

    private static string GestureName(MonoBehaviour interactable)
    {
        return Property(interactable, "InteractionGesture").GetValue(interactable).ToString();
    }

    private static string CheckpointName(MonoBehaviour manager)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        if (state == null)
            return "None";
        FieldInfo field = state.GetType().GetField("checkpoint", BindingFlags.Instance | BindingFlags.Public);
        return field?.GetValue(state)?.ToString() ?? "None";
    }

    private static IEnumerator LoadFreshSceneByPath(string scenePath)
    {
        MonoBehaviour existing = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
            yield return null;
        }

        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        if (File.Exists(savePath))
            File.Delete(savePath);
        Scene loaded = EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loaded.IsValid(), Is.True, scenePath);
        yield return null;
        yield return new WaitForSecondsRealtime(0.35f);
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
    }

    private static MonoBehaviour FindBehaviour(string typeName)
    {
        MonoBehaviour behaviour = TryFindBehaviour(typeName);
        Assert.That(behaviour, Is.Not.Null, typeName);
        return behaviour;
    }

    private static MonoBehaviour TryFindBehaviour(string typeName)
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == typeName);
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, target.GetType().Name + "." + name);
        return property;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
        return field;
    }

    private static object InvokeNonPublic(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
            {
                if (candidate.Name != method)
                    return false;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length)
                    return false;
                for (int index = 0; index < parameters.Length; index++)
                {
                    object argument = args[index];
                    if (argument != null && !parameters[index].ParameterType.IsInstanceOfType(argument))
                        return false;
                }
                return true;
            });
        Assert.That(info, Is.Not.Null, target.GetType().Name + "." + method);
        return info.Invoke(target, args);
    }

    private static object InvokeWithResult(MonoBehaviour target, string method, params object[] args)
    {
        MethodInfo info = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(candidate =>
            {
                if (candidate.Name != method)
                    return false;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length)
                    return false;
                for (int index = 0; index < parameters.Length; index++)
                {
                    object argument = args[index];
                    if (argument == null)
                    {
                        if (parameters[index].ParameterType.IsValueType &&
                            Nullable.GetUnderlyingType(parameters[index].ParameterType) == null)
                            return false;
                        continue;
                    }
                    if (!parameters[index].ParameterType.IsInstanceOfType(argument))
                        return false;
                }
                return true;
            });
        Assert.That(info, Is.Not.Null, target.GetType().Name + "." + method);
        return info.Invoke(target, args);
    }
#endif
}
