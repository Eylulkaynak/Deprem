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
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor-only offline gameplay capture for the project video. This never ships in
/// the player: it drives the existing authored scenes at 1x speed and writes Game
/// View frames (including HUD) under Temp/StoryVideoCapture.
/// </summary>
public sealed class StoryVideoCapturePlayModeTests
{
#if UNITY_EDITOR
    private const int CaptureFps = 24;
    private const int JpegQuality = 90;
    private static readonly string OutputRoot = Path.GetFullPath("Temp/StoryVideoCapture");
    private static readonly Dictionary<int, float> Cooldowns = new Dictionary<int, float>();

    [UnityTest, Timeout(1800000)]
    public IEnumerator RecordNormalSpeedMontageClips()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        bool hadSave = File.Exists(savePath);
        byte[] saveBackup = hadSave ? File.ReadAllBytes(savePath) : null;

        if (Directory.Exists(OutputRoot))
            Directory.Delete(OutputRoot, true);
        Directory.CreateDirectory(OutputRoot);

        try
        {
            yield return RecordOpeningConversation();
            yield return RecordBagPacking();
            yield return RecordSingleShelfFix();
            yield return RecordCorrectCoverSequence();
            yield return RecordStairChoice();
            yield return RecordSafeStreetRoute();
            yield return RecordAssemblyArrival();
            yield return RecordFiretruckScene();
            File.WriteAllText(
                Path.Combine(OutputRoot, "capture_DONE.txt"),
                DateTime.Now.ToString("O") + Environment.NewLine +
                "01_anne_sohbeti=9.0s" + Environment.NewLine +
                "02_canta=11.0s" + Environment.NewLine +
                "03_sabitleme=7.0s" + Environment.NewLine +
                "04_deprem_ckt=10.0s" + Environment.NewLine +
                "05_merdiven_secimi=5.0s" + Environment.NewLine +
                "06_cikis_ve_yuruyus=11.0s" + Environment.NewLine +
                "07_toplanma_alani=3.0s" + Environment.NewLine +
                "08_itfaiye=4.0s" + Environment.NewLine);
        }
        finally
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            if (hadSave)
                File.WriteAllBytes(savePath, saveBackup);
            else if (File.Exists(savePath))
                File.Delete(savePath);
        }
    }

    private static IEnumerator RecordOpeningConversation()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_01_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");

        Assert.That(SubtitleActive(ui), Is.True, "Opening family conversation did not start.");
        yield return CaptureFrames("01_anne_sohbeti", 9f, (float elapsed, ref float nextActionAt) =>
        {
            // Keep the authored Anne/Deniz/Can conversation at its normal reading pace.
        });
    }

    private static IEnumerator RecordBagPacking()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_01_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour director = FindBehaviour("StoryPreparationDirector");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);

        object foodCategory = Enum.Parse(
            Property(director, "CurrentCategory").PropertyType,
            "Food");
        InvokeNonPublic(director, "StartCategory", foodCategory);
        InvokeNonPublic(director, "RevealCategory", foodCategory);
        ClearSubtitleForVideo(ui);
        yield return null;
        Cooldowns.Clear();

        int bagStep = 0;
        yield return CaptureFrames("02_canta", 11f, (float elapsed, ref float nextActionAt) =>
        {
            if (bagStep == 0 && elapsed >= 1.5f)
            {
                DriveOneStep(ui, player, director, false);
                bagStep++;
            }
            else if (bagStep == 1 && elapsed >= 5.35f)
            {
                InvokeWithResult(ui, "TryHandlePrimaryTap");
                bagStep++;
            }
            else if (bagStep == 2 && elapsed >= 7.15f)
            {
                InvokeWithResult(ui, "TryHandlePrimaryTap");
                bagStep++;
            }
            else if (bagStep == 3 && elapsed >= 8.4f)
            {
                DriveOneStep(ui, player, director, false);
                bagStep++;
            }
        }, 1.5f);
    }

    private static IEnumerator RecordSingleShelfFix()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_02_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryHomeSafetyDirector");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);
        InvokeNonPublic(director, "BeginShelfSecuring");
        Assert.That(Property(director, "Stage").GetValue(director).ToString(), Is.EqualTo("SecuringShelf"));
        ActivateCameraZone(cameraController, "HomeShelf");
        HideForVideo("Anne_Ayse", "ShelfBracketInHand");
        GameObject securedBracket = FindGameObject("ShelfWallBracket");
        yield return null;

        bool fixedShelf = false;
        yield return CaptureFrames("03_sabitleme", 7f, (float elapsed, ref float nextActionAt) =>
        {
            if (!fixedShelf && elapsed >= 3.2f)
            {
                securedBracket.SetActive(true);
                securedBracket.GetComponent<Animation>()?.Play();
                InvokeWithResult(ui, "ShowObjective", "RAF DUVARA SABİTLENDİ", "Bağlantı parçası yerinde; geçiş yolu artık güvenli.");
                fixedShelf = true;
            }
        }, 3.2f);
    }

    private static IEnumerator RecordCorrectCoverSequence()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_03_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);
        InvokeNonPublic(director, "BeginQuake", false);
        InvokeNonPublic(director, "SetSiblingCalmedState", false);
        HideForVideo("QuakeRebuildOpeningProps", "Can_ToyCar", "Can_ToyCar_FinishMarker", "FamilyBoardGame");
        DisableQuakeDustForVideo();
        ActivateCameraZone(cameraController, "QuakeClose");
        yield return null;

        bool crouched = false;
        bool covered = false;
        bool held = false;
        yield return CaptureFrames("04_deprem_ckt", 10f, (float elapsed, ref float nextActionAt) =>
        {
            if (!crouched && elapsed >= 2.25f)
            {
                InvokeWithResult(director, "OnCrouchStep");
                ActivateCameraZone(cameraController, "QuakeClose");
                crouched = true;
            }
            if (!covered && elapsed >= 4.85f)
            {
                InvokeWithResult(director, "OnCoverHeadStep");
                ActivateCameraZone(cameraController, "QuakeClose");
                covered = true;
            }
            if (!held && elapsed >= 7.35f)
            {
                InvokeWithResult(director, "OnCoverReached");
                ActivateCameraZone(cameraController, "QuakeClose");
                held = true;
            }
        }, 2.25f);
    }

    private static IEnumerator RecordStairChoice()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_04_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour sibling = FindBehaviour("StorySiblingFollower");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);
        InvokeNonPublic(director, "BeginRouteChoice");
        SetRenderersEnabled(player.gameObject, false);
        SetRenderersEnabled(sibling.gameObject, false);
        FreezeCaptureCamera(
            cameraController,
            new Vector3(1.9f, 6.3f, -5.2f),
            new Vector3(0f, 4.03f, -0.65f),
            44f);
        yield return null;

        bool choseStairs = false;
        yield return CaptureFrames("05_merdiven_secimi", 5f, (float elapsed, ref float nextActionAt) =>
        {
            if (!choseStairs && elapsed >= 2.5f)
            {
                InvokeWithResult(director, "ChooseStairs");
                ClearSubtitleForVideo(ui);
                choseStairs = true;
            }
            PositionCaptureCamera(
                new Vector3(1.9f, 6.3f, -5.2f),
                new Vector3(0f, 4.03f, -0.65f),
                44f);
        }, 2.5f);
    }

    private static IEnumerator RecordDoorExit()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_04_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour sibling = FindBehaviour("StorySiblingFollower");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);

        InvokeWithResult(player, "Warp", new Vector3(0f, 0.02f, 19.25f));
        sibling.transform.position = new Vector3(0.55f, 0.02f, 18.85f);
        InvokeNonPublic(director, "BeginBuildingExit");
        Assert.That(Property(director, "Stage").GetValue(director).ToString(), Is.EqualTo("BuildingExit"));
        GameObject closedDoor = FindGameObject("BuildingExitDoor_Closed");
        GameObject openDoor = FindGameObject("BuildingExitDoor_Open");
        yield return null;

        bool opened = false;
        bool clearedFacade = false;
        yield return CaptureFrames("05_kapi_cikisi", 6f, (float elapsed, ref float nextActionAt) =>
        {
            if (!opened && elapsed >= 1.25f)
            {
                InvokeWithResult(director, "OpenBuildingExit");
                closedDoor.SetActive(false);
                openDoor.SetActive(true);
                opened = true;
            }

            string stage = Property(director, "Stage").GetValue(director).ToString();
            if (!clearedFacade && elapsed >= 4.45f && stage == "FacadeClear")
            {
                InvokeWithResult(director, "MoveAwayFromFacade");
                clearedFacade = true;
            }
        }, 1.25f);

        Assert.That(closedDoor.activeSelf, Is.False, "Dış kapının kapalı modeli kapanmalı.");
        Assert.That(openDoor.activeSelf, Is.True, "Dış kapının açık modeli görünmeli.");
    }

    private static IEnumerator RecordDrivenScene(
        string clipName,
        string scenePath,
        float seconds,
        Func<bool> readyToRecord)
    {
        yield return LoadFreshScene(scenePath);
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour preparationDirector = TryFindBehaviour("StoryPreparationDirector");
        Cooldowns.Clear();

        Time.timeScale = 1f;
        float deadline = Time.time + 650f;
        while (!readyToRecord())
        {
            Assert.That(Time.time, Is.LessThan(deadline),
                scenePath + " video kesiti için hedef duruma ulaşamadı. Görev=" + ObjectiveText(ui));
            DriveOneStep(ui, player, preparationDirector, false);
            yield return new WaitForSeconds(0.12f);
        }

        Time.timeScale = 1f;
        yield return null;
        yield return CaptureFrames(clipName, seconds, (float elapsed, ref float nextActionAt) =>
        {
            if (elapsed < nextActionAt)
                return;

            bool subtitleWasActive = SubtitleActive(ui);
            DriveOneStep(ui, player, preparationDirector, false);
            nextActionAt = elapsed + (subtitleWasActive ? 1.05f : 1.55f);
        });
    }

    private static IEnumerator RecordSafeStreetRoute()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_04_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour sibling = FindBehaviour("StorySiblingFollower");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);
        Vector3 insideStart = new Vector3(0.35f, 0.02f, 19.75f);
        Vector3 routeStart = new Vector3(0.4f, 0.02f, 24.0f);
        Vector3 routeEnd = new Vector3(0.35f, 0.02f, 40.3f);
        InvokeWithResult(player, "Warp", insideStart);
        sibling.transform.position = insideStart + new Vector3(-0.75f, 0f, -0.45f);
        SetActiveIfFound("BuildingExitDoor_Closed", false);
        SetActiveIfFound("BuildingExitDoor_Open", false);
        SetActiveIfFound("EmergencyFiretruck", false);
        Animator playerAnimator = player.GetComponentInChildren<Animator>(true);
        Animator siblingAnimator = sibling.GetComponentInChildren<Animator>(true);
        playerAnimator?.SetFloat("Speed", 1f);
        siblingAnimator?.SetFloat("Speed", 1f);
        FreezeCaptureCamera(
            cameraController,
            new Vector3(2.8f, 3.25f, 15.65f),
            new Vector3(0f, 1.1f, 21.15f),
            42f);
        InvokeWithResult(ui, "ShowObjective", "TOPLANMA ALANINA İLERLE", "Açık kaldırımdan, bina cephesinden uzak yürüyün.");
        yield return null;

        yield return CaptureFrames("06_cikis_ve_yuruyus", 11f, (float elapsed, ref float nextActionAt) =>
        {
            Vector3 playerPosition;
            if (elapsed < 3.0f)
            {
                float exitProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 3.0f));
                playerPosition = Vector3.Lerp(insideStart, routeStart, exitProgress);
                PositionCaptureCamera(
                    new Vector3(2.8f, 3.25f, 15.65f),
                    new Vector3(0f, 1.1f, 21.15f),
                    42f);
            }
            else
            {
                float routeProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01((elapsed - 3.0f) / 7.4f));
                playerPosition = Vector3.Lerp(routeStart, routeEnd, routeProgress);
                PositionCaptureCamera(
                    playerPosition + new Vector3(5.0f, 3.15f, -5.8f),
                    playerPosition + new Vector3(0f, 0.95f, 1.1f),
                    42f);
            }
            InvokeWithResult(player, "Warp", playerPosition);
            sibling.transform.position = playerPosition + new Vector3(-0.75f, 0f, -0.45f);
        });
        playerAnimator?.SetFloat("Speed", 0f);
        siblingAnimator?.SetFloat("Speed", 0f);
    }

    private static IEnumerator RecordAssemblyArrival()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_04_RebuildPreview.unity");
        Time.timeScale = 1f;
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StoryEvacuationDirector");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour sibling = FindBehaviour("StorySiblingFollower");
        MonoBehaviour cameraController = FindBehaviour("StoryCameraController");
        yield return DismissSubtitles(ui);
        director.StopAllCoroutines();
        ClearSubtitleForVideo(ui);
        InvokeWithResult(player, "Warp", new Vector3(0.35f, 0.02f, 40.3f));
        sibling.transform.position = new Vector3(1.0f, 0.02f, 40.05f);
        SetActiveIfFound("Nermin_Neighbor_Street", false);
        SetActiveIfFound("Nermin_Neighbor_Assembly", true);
        ActivateCameraZone(cameraController, "EvacuationAssembly");
        InvokeWithResult(ui, "ShowObjective", "AFET TOPLANMA ALANI", "Deniz ve Can güvenli alana birlikte ulaştı.");
        yield return null;

        yield return CaptureFrames("07_toplanma_alani", 3f, (float elapsed, ref float nextActionAt) =>
        {
            // Keep the arrival shot calm so the assembly sign and family are readable.
        });
    }

    private static IEnumerator RecordFiretruckScene()
    {
        yield return LoadFreshScene("Assets/Scenes/Story_04_FiretruckRunner.unity");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(3.45f);

        MonoBehaviour manager = FindBehaviour("FiretruckRunnerManager");
        bool wentLeft = false;
        bool returnedCenter = false;
        yield return CaptureFrames("08_itfaiye", 4f, (float elapsed, ref float nextActionAt) =>
        {
            if (!wentLeft && elapsed >= 0.35f)
            {
                InvokeNonPublic(manager, "ChangeLane", -1);
                wentLeft = true;
            }
            if (!returnedCenter && elapsed >= 2.45f)
            {
                InvokeNonPublic(manager, "ChangeLane", 1);
                returnedCenter = true;
            }
        });
    }

    private static void HideForVideo(params string[] names)
    {
        HashSet<string> hiddenNames = new HashSet<string>(names ?? Array.Empty<string>());
        foreach (Transform item in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (item != null && hiddenNames.Contains(item.name))
                item.gameObject.SetActive(false);
        }
    }

    private static void SetActiveIfFound(string name, bool active)
    {
        Transform item = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.name == name);
        if (item != null)
            item.gameObject.SetActive(active);
    }

    private static void SetRenderersEnabled(GameObject root, bool enabled)
    {
        if (root == null)
            return;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = enabled;
    }

    private static void FreezeCaptureCamera(
        MonoBehaviour cameraController,
        Vector3 position,
        Vector3 lookAt,
        float fieldOfView)
    {
        if (cameraController != null)
            cameraController.enabled = false;

        MonoBehaviour brain = TryFindBehaviour("CinemachineBrain");
        if (brain != null)
            brain.enabled = false;

        PositionCaptureCamera(position, lookAt, fieldOfView);
    }

    private static void PositionCaptureCamera(Vector3 position, Vector3 lookAt, float fieldOfView)
    {
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null, "Capture camera not found.");
        camera.transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
        camera.fieldOfView = fieldOfView;
    }

    private static void DisableQuakeDustForVideo()
    {
        foreach (ParticleSystem particles in Object.FindObjectsByType<ParticleSystem>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (particles == null)
                continue;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
        }
    }

    private static void ClearSubtitleForVideo(MonoBehaviour ui)
    {
        ui.StopAllCoroutines();
        InvokeNonPublic(ui, "ResetSubtitleState", false);
        FieldInfo subtitleField = ui.GetType().GetField(
            "subtitle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Component subtitleComponent = subtitleField?.GetValue(ui) as Component;
        if (subtitleComponent != null)
        {
            GameObject displayRoot = subtitleComponent.transform.parent != null
                ? subtitleComponent.transform.parent.gameObject
                : subtitleComponent.gameObject;
            displayRoot.SetActive(false);
        }

        FieldInfo deferred = ui.GetType().GetField(
            "subtitleCompletedAfterPointerRelease",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(deferred, Is.Not.Null, "StoryUIController.subtitleCompletedAfterPointerRelease");
        deferred.SetValue(ui, null);

        FieldInfo pointerGuard = ui.GetType().GetField(
            "consumeWorldPointerUntilRelease",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(pointerGuard, Is.Not.Null, "StoryUIController.consumeWorldPointerUntilRelease");
        pointerGuard.SetValue(ui, false);
    }

    private static void ActivateCameraZone(MonoBehaviour cameraController, string zoneName)
    {
        MethodInfo activate = cameraController.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == "ActivateZone" && method.GetParameters().Length == 2);
        object zone = Enum.Parse(activate.GetParameters()[0].ParameterType, zoneName);
        activate.Invoke(cameraController, new[] { zone, (object)true });
    }

    private delegate void CaptureDriver(float elapsed, ref float nextActionAt);

    private static IEnumerator CaptureFrames(
        string clipName,
        float seconds,
        CaptureDriver driver,
        float firstActionAt = 0.65f)
    {
        string clipDirectory = Path.Combine(OutputRoot, clipName);
        if (Directory.Exists(clipDirectory))
            Directory.Delete(clipDirectory, true);
        Directory.CreateDirectory(clipDirectory);

        Assert.That(Camera.main, Is.Not.Null, clipName + " ana kamerası bulunamadı.");
        int frameCount = Mathf.RoundToInt(seconds * CaptureFps);
        float nextActionAt = firstActionAt;
        Time.captureFramerate = CaptureFps;
        try
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                float elapsed = frame / (float)CaptureFps;
                driver?.Invoke(elapsed, ref nextActionAt);
                yield return new WaitForEndOfFrame();

                Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                Assert.That(screenshot, Is.Not.Null, clipName + " frame " + frame);
                byte[] jpeg = screenshot.EncodeToJPG(JpegQuality);
                File.WriteAllBytes(
                    Path.Combine(clipDirectory, $"frame_{frame:D5}.jpg"),
                    jpeg);
                Object.DestroyImmediate(screenshot);
            }
        }
        finally
        {
            Time.captureFramerate = 0;
        }

        File.WriteAllText(
            Path.Combine(clipDirectory, "clip.txt"),
            $"fps={CaptureFps}{Environment.NewLine}frames={frameCount}{Environment.NewLine}seconds={seconds:F2}");
    }

    private static void DriveOneStep(
        MonoBehaviour ui,
        MonoBehaviour player,
        MonoBehaviour preparationDirector,
        bool fastForward)
    {
        if (SubtitleActive(ui))
        {
            InvokeWithResult(ui, "TryHandlePrimaryTap");
            return;
        }

        MonoBehaviour interactable = PickAvailableInteractable();
        if (interactable == null)
        {
            MonoBehaviour walkTarget = PickAutoTriggerTarget();
            if (walkTarget == null)
                return;

            Cooldowns[walkTarget.GetInstanceID()] = Time.time + (fastForward ? 2f : 6f);
            Transform interactionPoint = Property(walkTarget, "InteractionPoint").GetValue(walkTarget) as Transform;
            if (interactionPoint == null)
                return;
            InvokeWithResult(player, "SetStoryInputLocked", false);
            InvokeWithResult(player, "SetNavigationEnabled", true);
            InvokeWithResult(player, "TrySetDestination", interactionPoint.position);
            return;
        }

        Cooldowns[interactable.GetInstanceID()] = Time.time + (fastForward ? 2f : 6f);
        if (preparationDirector != null &&
            GestureName(interactable) == "DragToBag" &&
            (bool)InvokeWithResult(preparationDirector, "TryBeginItemExplanation", interactable))
            return;

        MethodInfo request = interactable.GetType().GetMethod(
            "RequestInteraction",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(request, Is.Not.Null, interactable.name + ".RequestInteraction");
        request.Invoke(interactable, new object[] { player });
    }

    private static MonoBehaviour PickAvailableInteractable()
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(item => item != null && item.GetType().Name == "StoryInteractable")
            .Where(item => (bool)Property(item, "IsAvailable").GetValue(item))
            .Where(item => (bool)Property(item, "WorldSelectable").GetValue(item))
            .Where(item => Property(item, "InteractionKind").GetValue(item).ToString() != "UnsafeChoice")
            .Where(item => !Cooldowns.TryGetValue(item.GetInstanceID(), out float until) || Time.time >= until)
            .OrderBy(item => TaughtOrderIndex(item))
            .ThenBy(item => item.transform.GetSiblingIndex())
            .FirstOrDefault();
    }

    private static MonoBehaviour PickAutoTriggerTarget()
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(item => item != null && item.GetType().Name == "StoryInteractable")
            .Where(item => (bool)Property(item, "IsAvailable").GetValue(item))
            .Where(item => !(bool)Property(item, "WorldSelectable").GetValue(item))
            .Where(item => Property(item, "InteractionKind").GetValue(item).ToString() != "UnsafeChoice")
            .Where(item => !Cooldowns.TryGetValue(item.GetInstanceID(), out float until) || Time.time >= until)
            .FirstOrDefault();
    }

    private static int TaughtOrderIndex(MonoBehaviour interactable)
    {
        string id = Property(interactable, "InteractionId").GetValue(interactable) as string;
        string[] order = { "quake.cover.crouch", "quake.cover.head", "quake.cover.grip" };
        int index = Array.IndexOf(order, id);
        return index < 0 ? int.MaxValue : index;
    }

    private static IEnumerator LoadFreshScene(string scenePath)
    {
        MonoBehaviour existing = TryFindBehaviour("StoryGameManager");
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

    private static bool SubtitleActive(MonoBehaviour ui)
    {
        return (bool)Property(ui, "SubtitleActive").GetValue(ui);
    }

    private static IEnumerator DismissSubtitles(MonoBehaviour ui)
    {
        for (int attempt = 0; attempt < 32 && SubtitleActive(ui); attempt++)
        {
            InvokeWithResult(ui, "TryHandlePrimaryTap");
            yield return null;
        }
        Assert.That(SubtitleActive(ui), Is.False, "Başlangıç altyazısı temizlenemedi.");
    }

    private static GameObject FindGameObject(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
        Assert.That(transform, Is.Not.Null, name);
        return transform.gameObject;
    }

    private static string ObjectiveText(MonoBehaviour ui)
    {
        PropertyInfo property = ui.GetType().GetProperty(
            "CurrentObjectiveTitle",
            BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(ui) as string ?? string.Empty;
    }

    private static string CheckpointName(MonoBehaviour manager)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        FieldInfo checkpoint = state?.GetType().GetField("checkpoint", BindingFlags.Instance | BindingFlags.Public);
        return checkpoint?.GetValue(state)?.ToString() ?? "None";
    }

    private static string GestureName(MonoBehaviour interactable)
    {
        return Property(interactable, "InteractionGesture").GetValue(interactable).ToString();
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

    private static object InvokeWithResult(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == args.Length);
        return info.Invoke(target, args);
    }

    private static void InvokeNonPublic(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == args.Length);
        info.Invoke(target, args);
    }
#endif
}
