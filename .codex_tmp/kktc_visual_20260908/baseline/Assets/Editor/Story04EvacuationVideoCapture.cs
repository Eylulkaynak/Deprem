using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class Story04EvacuationVideoCapture
{
    private const string ScenePath = "Assets/Scenes/Story_04_RebuildPreview.unity";
    private const string PendingKey = "Deprem.Story04EvacuationCapture.Pending";
    private const string OutputKey = "Deprem.Story04EvacuationCapture.Output";

    static Story04EvacuationVideoCapture()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Deprem Story/Video/Record Story 04 Human Evacuation")]
    private static void Record()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Story 04 kaydı başlamadan önce Play Mode'dan çık.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string outputFolder = Path.GetFullPath(Path.Combine("Recordings", "Story04"));
        Directory.CreateDirectory(outputFolder);
        string output = Path.Combine(
            outputFolder,
            "Story04_MerdivenVeCadde_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        SessionState.SetBool(PendingKey, true);
        SessionState.SetString(OutputKey, output);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        GameObject driverObject = new GameObject("__Story04EvacuationCaptureDriver");
        driverObject.SetActive(false);
        Story04EvacuationCaptureDriver driver =
            driverObject.AddComponent<Story04EvacuationCaptureDriver>();
        driver.OutputFile = SessionState.GetString(OutputKey, string.Empty);
        driverObject.SetActive(true);
    }
}

internal sealed class Story04EvacuationCaptureDriver : MonoBehaviour
{
    internal string OutputFile { get; set; }

    private RecorderController recorder;
    private CanvasGroup fade;
    private StoryPlayerMovement telemetryPlayer;
    private StorySiblingFollower telemetrySibling;
    private readonly List<string> telemetry = new List<string>();
    private bool telemetryActive;
    private string telemetryPhase = "setup";

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        StoryEvacuationDirector director = FindAnyObjectByType<StoryEvacuationDirector>();
        StoryPlayerMovement player = FindAnyObjectByType<StoryPlayerMovement>();
        StorySiblingFollower sibling = FindAnyObjectByType<StorySiblingFollower>();
        StoryTouchManager touch = FindAnyObjectByType<StoryTouchManager>();
        StoryUIController ui = FindAnyObjectByType<StoryUIController>();
        StoryCameraController cameras = FindAnyObjectByType<StoryCameraController>();
        if (director == null || player == null || sibling == null || touch == null || ui == null || cameras == null)
            throw new InvalidOperationException("Story 04 kayıt bileşenlerinden biri sahnede bulunamadı.");

        telemetryPlayer = player;
        telemetrySibling = sibling;
        telemetry.Add("time,phase,actor,x,y,z,speed,onOffMeshLink,clip,leftFootY,rightFootY,upAngle");

        director.StopAllCoroutines();
        ui.StopAllCoroutines();
        ClearSubtitle(ui);
        touch.SetWorldNavigationEnabled(true);
        touch.SetInteractionsEnabled(false);
        player.SetNavigationEnabled(true);
        sibling.SetFollowing(true);

        WarpCharacter(player.gameObject, new Vector3(0f, 4.02f, -8.55f));
        WarpCharacter(sibling.gameObject, new Vector3(-0.7f, 4.02f, -9.05f));
        cameras.ActivateZone(StoryCameraZoneId.EvacuationCorridor, true);
        ui.ShowObjective(
            "KORİDORDAKİ İKİ ROTAYI KONTROL ET",
            "Deniz ve Can birlikte ilerleyip asansör ile merdiveni karşılaştırıyor.");
        fade = BuildFadeOverlay();
        fade.alpha = 0f;

        StartRecorder();
        telemetryActive = true;
        telemetryPhase = "corridor";
        yield return new WaitForSecondsRealtime(1.2f);

        // Gerçek NavMesh yürüyüşü: kare kare Warp kullanılmaz. Böylece ayaklar yol alır,
        // adım animasyonu yer değiştirme hızıyla aynı kalır ve çocuklar koridorda ilerler.
        bool corridorMoveStarted = player.TrySetDestination(new Vector3(0f, 4.02f, -1.45f));
        if (!corridorMoveStarted)
            throw new InvalidOperationException("Koridor kayıt rotası NavMesh üzerinde başlatılamadı.");
        yield return WaitForMovement(player, 8f);

        InvokePrivate(director, "BeginRouteChoice");
        yield return new WaitForSecondsRealtime(2.5f);
        cameras.ActivateZone(StoryCameraZoneId.EvacuationElevator, true);
        yield return new WaitForSecondsRealtime(2.6f);
        cameras.ActivateZone(StoryCameraZoneId.EvacuationStairDoor, true);
        yield return new WaitForSecondsRealtime(3.0f);

        director.ChooseStairs();
        yield return new WaitForSecondsRealtime(4.8f);

        // The next section starts under a presentation fade, but the building
        // threshold itself is traversed by the real director/NavMesh flow.  The
        // old capture warped straight outdoors and therefore could not reveal a
        // broken exit walk or a prop blocking the landing.
        yield return FadeTo(1f, 0.65f);
        director.StopAllCoroutines();
        ui.StopAllCoroutines();
        ClearSubtitle(ui);
        WarpCharacter(player.gameObject, new Vector3(0f, 0.02f, 19.55f));
        WarpCharacter(sibling.gameObject, new Vector3(-0.55f, 0.02f, 19.1f));
        player.SetNavigationEnabled(true);
        sibling.SetFollowing(true);
        touch.SetWorldNavigationEnabled(true);
        touch.SetInteractionsEnabled(true);

        SetActive("NeighborCardboard_Blocking_Drag", false);
        SetActive("NeighborFoam_Blocking_Drag", false);
        SetActive("NeighborCane_Blocked", false);
        SetActive("NeighborCardboard_Cleared", true);
        SetActive("NeighborFoam_Cleared", true);
        SetActive("NeighborCane_Reachable", true);
        SetActive("Nermin_Neighbor_Landing", true);
        SetActive("Nermin_Neighbor_Street", false);
        telemetryPhase = "neighbor_layout";
        cameras.ActivateZone(StoryCameraZoneId.EvacuationNeighbor, true);
        ui.ShowObjective(
            "SAĞ DUVAR ŞERİDİ AÇIK",
            "Kutu, köpük ve baston rotadan alındı; Deniz kapıya düz ilerliyor.");
        yield return FadeTo(0f, 0.65f);
        yield return new WaitForSecondsRealtime(2.8f);

        SetActive("Nermin_Neighbor_Landing", false);
        InvokePrivate(director, "BeginBuildingExit");
        cameras.ActivateZone(StoryCameraZoneId.EvacuationBuildingDoor, true);
        telemetryPhase = "building_threshold";
        yield return new WaitForSecondsRealtime(1.0f);
        director.OpenBuildingExit();
        yield return WaitForMovement(player, 12f);
        // Reaching the NavMesh destination is followed by an authored turn back
        // toward the open door.  FinishBuildingExit is invoked only after that
        // turn completes.  Starting the next destination as soon as IsMoving
        // becomes false cancels the turn and drops its completion callback,
        // leaving the director permanently in BuildingExit.  Wait for the real
        // production stage transition before issuing any capture-only movement.
        yield return WaitForDirectorStage(director, "FacadeClear", 5f);
        // FinishBuildingExit opens a 4.2 s subtitle and deliberately locks story
        // input while it is visible.
        yield return WaitForPlayerReady(player, 8f);

        telemetryPhase = "facade_clear";
        Transform facadeSafePoint = FindTransform("FacadeClearPavingPoint");
        if (facadeSafePoint == null)
            throw new InvalidOperationException("Cepheden uzaklaşma kayıt hedefi bulunamadı.");
        bool facadeMoveStarted = player.TrySetDestination(facadeSafePoint.position);
        if (!facadeMoveStarted)
            throw new InvalidOperationException("Cepheden uzaklaşma kayıt rotası başlatılamadı.");
        yield return WaitForMovement(player, 5f);
        director.MoveAwayFromFacade();
        yield return WaitForDirectorStage(director, "StreetRoute", 8f);
        yield return WaitForPlayerReady(player, 8f);

        telemetryPhase = "street";
        director.InspectStreetHazard();
        yield return WaitForPlayerReady(player, 9f);
        director.TakeSafeSidewalk();
        yield return WaitForMovement(player, 18f);
        yield return new WaitForSecondsRealtime(3.0f);

        yield return FadeTo(1f, 0.5f);
        telemetryActive = false;
        recorder.StopRecording();
        File.WriteAllLines(OutputFile + "_motion.csv", telemetry);
        File.WriteAllText(
            OutputFile + "_DONE.txt",
            "Story 04 human evacuation capture completed at " + DateTime.Now.ToString("O"));
        Debug.Log("Story 04 merdiven ve cadde videosu kaydedildi: " + OutputFile + ".mp4");
        yield return new WaitForSecondsRealtime(0.5f);
        EditorApplication.ExitPlaymode();
    }

    private void Update()
    {
        if (!telemetryActive)
            return;

        AppendTelemetry("Deniz", telemetryPlayer != null ? telemetryPlayer.gameObject : null);
        AppendTelemetry("Can", telemetrySibling != null ? telemetrySibling.gameObject : null);
    }

    private void AppendTelemetry(string actor, GameObject character)
    {
        if (character == null)
            return;

        NavMeshAgent agent = character.GetComponent<NavMeshAgent>();
        Animator animator = character.GetComponentInChildren<Animator>();
        string clip = animator != null
            ? string.Join("+", animator.GetCurrentAnimatorClipInfo(0)
                .Where(info => info.clip != null)
                .Select(info => info.clip.name.Replace(',', '_')))
            : string.Empty;
        Transform leftFoot = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.LeftFoot)
            : null;
        Transform rightFoot = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.RightFoot)
            : null;
        float speed = agent != null && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
        bool onLink = agent != null && agent.isOnNavMesh && agent.isOnOffMeshLink;
        float upAngle = Vector3.Angle(character.transform.up, Vector3.up);
        Vector3 position = character.transform.position;
        telemetry.Add(string.Join(",", new[]
        {
            Time.unscaledTime.ToString("F3"), telemetryPhase, actor,
            position.x.ToString("F4"), position.y.ToString("F4"), position.z.ToString("F4"),
            speed.ToString("F4"), onLink ? "1" : "0", clip,
            (leftFoot != null ? leftFoot.position.y : float.NaN).ToString("F4"),
            (rightFoot != null ? rightFoot.position.y : float.NaN).ToString("F4"),
            upAngle.ToString("F3")
        }));
    }

    private void StartRecorder()
    {
        RecorderControllerSettings controllerSettings =
            ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30f;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Story 04 Human Evacuation — Portrait MP4";
        movie.Enabled = true;
        movie.CaptureAudio = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1080,
            OutputHeight = 1920
        };
        movie.OutputFile = OutputFile;
        controllerSettings.AddRecorderSettings(movie);

        recorder = new RecorderController(controllerSettings);
        recorder.PrepareRecording();
        recorder.StartRecording();
    }

    private static IEnumerator WaitForMovement(StoryPlayerMovement player, float timeout)
    {
        float deadline = Time.unscaledTime + timeout;
        bool sawMovement = false;
        while (Time.unscaledTime < deadline)
        {
            sawMovement |= player.IsMoving;
            if (sawMovement && !player.IsMoving)
                yield break;
            yield return null;
        }

        if (!sawMovement)
            Debug.LogWarning("Story 04 kayıt rotasında karakter yürüyüşe başlamadı.");
    }

    private static IEnumerator WaitForPlayerReady(StoryPlayerMovement player, float timeout)
    {
        float deadline = Time.unscaledTime + timeout;
        while (Time.unscaledTime < deadline && player != null && player.StoryInputLocked)
            yield return null;

        if (player == null || player.StoryInputLocked)
            throw new InvalidOperationException("Story 04 kayıt akışında altyazı giriş kilidi zamanında açılmadı.");
    }

    private static IEnumerator WaitForDirectorStage(
        StoryEvacuationDirector director,
        string expectedStage,
        float timeout)
    {
        FieldInfo stageField = typeof(StoryEvacuationDirector).GetField(
            "stage",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (stageField == null)
            throw new MissingFieldException(typeof(StoryEvacuationDirector).Name, "stage");

        float deadline = Time.unscaledTime + timeout;
        while (Time.unscaledTime < deadline)
        {
            object stageValue = stageField.GetValue(director);
            if (stageValue != null && stageValue.ToString() == expectedStage)
                yield break;
            yield return null;
        }

        object actualStage = stageField.GetValue(director);
        throw new InvalidOperationException(
            $"Story 04 kayıt akışı {expectedStage} aşamasına geçemedi; mevcut aşama: {actualStage}.");
    }

    private static void WarpCharacter(GameObject character, Vector3 position)
    {
        NavMeshAgent agent = character.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh && agent.Warp(position))
            return;
        character.transform.position = position;
    }

    private static Transform FindTransform(string objectName)
    {
        return FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.name == objectName);
    }

    private static void SetActive(string objectName, bool active)
    {
        Transform item = FindTransform(objectName);
        if (item == null)
            throw new InvalidOperationException(objectName + " kayıt sahnesinde bulunamadı.");
        item.gameObject.SetActive(active);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        method.Invoke(target, null);
    }

    private static void ClearSubtitle(StoryUIController ui)
    {
        MethodInfo reset = typeof(StoryUIController).GetMethod(
            "ResetSubtitleState",
            BindingFlags.Instance | BindingFlags.NonPublic);
        reset?.Invoke(ui, new object[] { false });
    }

    private CanvasGroup BuildFadeOverlay()
    {
        GameObject canvasObject = new GameObject("__Story04CaptureFade", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = Color.black;
        return imageObject.GetComponent<CanvasGroup>();
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = fade.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        fade.alpha = target;
    }
}
