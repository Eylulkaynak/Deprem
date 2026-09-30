using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

/// <summary>
/// Rebuild sahnelerinde "ekran şunu yap diyor ama kamera yüzünden yapamıyorsun" sınıfı
/// oynanabilirlik hatalarını otomatik bulur. Her kamera zone'u için ekran görüntüsü alır,
/// her StoryInteractable'ın odak kamerasındaki kadraj/occlusion/tap-boyutu durumunu ölçer,
/// sürükleme hedeflerinin aynı kadrajda görünüp görünmediğini ve NavMesh erişilebilirliğini
/// raporlar. Sahnede kalıcı değişiklik yapmaz ve asla kaydetmez.
/// </summary>
[InitializeOnLoad]
internal static class StoryPlayabilityAudit
{
    private const string MenuRoot = "Tools/Deprem Story/QA/Playability Audit/";
    private const string OutputRoot = "Temp/StoryPlayabilityAudit";
    private const string RequestFlagPath = "Temp/story_playability_audit_request.txt";
    private readonly struct CaptureProfile
    {
        internal readonly string name;
        internal readonly int width;
        internal readonly int height;

        internal CaptureProfile(string name, int width, int height)
        {
            this.name = name;
            this.width = width;
            this.height = height;
        }
    }

    private static readonly CaptureProfile[] CaptureProfiles =
    {
        new CaptureProfile("portrait_9x16", 540, 960),
        new CaptureProfile("portrait_20x9", 540, 1200)
    };

    private static CaptureProfile activeCaptureProfile = CaptureProfiles[0];
    private static int CaptureWidth => activeCaptureProfile.width;
    private static int CaptureHeight => activeCaptureProfile.height;
    private const float SafeViewportMinX = 0.10f;
    private const float SafeViewportMaxX = 0.90f;
    private const float SafeViewportMinY = 0.18f;
    private const float SafeViewportMaxY = 0.82f;
    private const float MinVisibleFraction = 0.75f;
    private const float MinTapPixels = 48f;
    private const float CameraClearanceRadius = 0.20f;

    private static readonly string[] AllRebuildScenes =
    {
        "Assets/Scenes/Story_01_RebuildPreview.unity",
        "Assets/Scenes/Story_02_RebuildPreview.unity",
        "Assets/Scenes/Story_03_RebuildPreview.unity",
        "Assets/Scenes/Story_04_RebuildPreview.unity"
    };

    // Bu etkileşimlerin nesnesi/hedefi karakterlere bağlıdır ve director oyun
    // sırasında karakteri odak kadrajına taşır. İnşa-anı ölçümünde kadraj dışı
    // görünmeleri beklenir; rapor okunurluğu için bilgi seviyesine iner.
    private static readonly HashSet<string> RuntimeStagedInteractions = new HashSet<string>(StringComparer.Ordinal)
    {
        "Final_PlaceBagAtExit",
        "Review_ClipWhistleToCan",
        "Give_CanComfortToy",
        "Blackout_FindCan",
        "Blackout_UseWhistle",
        // Story_02: odak bölgesi oyuncuyu izleyen HomeOverview kamerası; oyuncu
        // önce interactionPoint'e yürüdüğü için gerçek kadraj oyun anında kurulur.
        "home.risk.safeplay",
        // Story_03: bu etkileşimlerin nesneleri/hedefleri Deniz'e ya da Can'a
        // bağlıdır (örtünme, koridor el ele, ayakkabı/çanta giydirme) ya da odak
        // bölgeleri oyuncuyu izleyen follow kameralardır; kadraj oyun anında
        // director/follow tarafından kurulur.
        "quake.cover.head",
        "quake.corridor.hand",
        "quake.corridor.aftershock",
        "quake.post.shoes",
        "quake.post.bag",
        "quake.post.flashlight",
        "quake.post.checkcan",
        "quake.intro.radio",
        // Story_04: Can oyuncuyu takip eder; toplanma alanındaki kontrol dokunuşu
        // ve battaniye bırakma hedefi oyun anında kadrajın içindedir.
        "evac.r04.assembly.can",
        "evac.r04.assembly.blanket"
    };

    private static readonly Color32[] MarkerPalette =
    {
        new Color32(255, 59, 48, 255), new Color32(52, 199, 89, 255),
        new Color32(0, 122, 255, 255), new Color32(255, 149, 0, 255),
        new Color32(175, 82, 222, 255), new Color32(255, 204, 0, 255),
        new Color32(90, 200, 250, 255), new Color32(255, 45, 85, 255),
        new Color32(162, 132, 94, 255), new Color32(142, 142, 147, 255),
        new Color32(0, 255, 200, 255), new Color32(199, 244, 100, 255)
    };

    static StoryPlayabilityAudit()
    {
        EditorApplication.delayCall += TryRunFromRequestFlag;
    }

    [MenuItem(MenuRoot + "Run For Story01")]
    private static void RunStory01() => RunForScenes(new[] { AllRebuildScenes[0] });

    [MenuItem(MenuRoot + "Run For Story02")]
    private static void RunStory02() => RunForScenes(new[] { AllRebuildScenes[1] });

    [MenuItem(MenuRoot + "Run For Story03")]
    private static void RunStory03() => RunForScenes(new[] { AllRebuildScenes[2] });

    [MenuItem(MenuRoot + "Run For Story04")]
    private static void RunStory04() => RunForScenes(new[] { AllRebuildScenes[3] });

    [MenuItem(MenuRoot + "Run For All Rebuild Scenes")]
    private static void RunAll() => RunForScenes(AllRebuildScenes);

    private static void TryRunFromRequestFlag()
    {
        string flagFullPath = Path.GetFullPath(RequestFlagPath);
        if (!File.Exists(flagFullPath))
            return;

        // Play Mode ÇIKIŞ geçişinde isPlayingOrWillChangePlaymode false dönerken
        // Application.isPlaying bir kare daha true kalabilir. Denetim sahne değiştirip
        // ekran görüntüsü aldığı için iki bayrağı birden bekle.
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRunFromRequestFlag;
            return;
        }

        string[] requested;
        try
        {
            requested = File.ReadAllLines(flagFullPath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();
        }
        finally
        {
            File.Delete(flagFullPath);
        }

        string[] rejectedRebuilds = requested
            .Where(line => line.StartsWith("rebuild:", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.Substring("rebuild:".Length).Trim())
            .ToArray();
        if (rejectedRebuilds.Length > 0)
        {
            Debug.LogWarning(
                "STORY_PLAYABILITY_AUDIT: rebuild: komutları yok sayıldı. " +
                "QA denetimi authored sahneleri yeniden kurmaz; elle yapılan düzenlemeler korunur. " +
                "Bilinçli rebuild için yalnız açık Builder menülerini kullanın. " +
                $"Yok sayılan: {string.Join(", ", rejectedRebuilds)}");
        }

        string[] scenes = requested
            .Where(line => !line.StartsWith("rebuild:", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (scenes.Length == 1 && scenes[0] == "all")
            scenes = AllRebuildScenes;
        if (scenes.Length > 0)
            RunForScenes(scenes);
    }

    private static void RunForScenes(string[] scenePaths)
    {
        Directory.CreateDirectory(Path.GetFullPath(OutputRoot));
        string statusPath = Path.GetFullPath(Path.Combine(OutputRoot, "status.txt"));
        void Status(string message)
        {
            File.AppendAllText(statusPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            Debug.Log("STORY_PLAYABILITY_AUDIT " + message);
        }

        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Status("ABORT: Play Mode aktifken denetim çalıştırılamaz.");
            return;
        }

        for (int index = 0; index < EditorSceneManager.sceneCount; index++)
        {
            if (EditorSceneManager.GetSceneAt(index).isDirty)
            {
                Status("ABORT: Kaydedilmemiş sahne değişikliği var; denetim veri kaybını önlemek için durduruldu.");
                return;
            }
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        Status($"BEGIN scenes={scenePaths.Length} rebuilds=disabled original={originalScenePath}");
        try
        {
            foreach (string scenePath in scenePaths)
            {
                if (!File.Exists(Path.GetFullPath(scenePath)))
                {
                    Status("SKIP missing scene " + scenePath);
                    continue;
                }
                foreach (CaptureProfile profile in CaptureProfiles)
                {
                    activeCaptureProfile = profile;
                    AuditScene(scenePath, Status);
                }
            }
        }
        catch (Exception exception)
        {
            Status("ERROR " + exception);
            Debug.LogException(exception);
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath) &&
                EditorSceneManager.GetActiveScene().path != originalScenePath)
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            string donePath = Path.GetFullPath(Path.Combine(OutputRoot, "DONE.txt"));
            File.WriteAllText(donePath, DateTime.Now.ToString("O"));
            Status("DONE");
        }
    }

    private static void AuditScene(string scenePath, Action<string> status)
    {
        if (EditorSceneManager.GetActiveScene().path != scenePath)
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string sceneOutputRoot = Path.GetFullPath(Path.Combine(OutputRoot, sceneName, activeCaptureProfile.name));
        Directory.CreateDirectory(sceneOutputRoot);
        status($"SCENE {sceneName} profile={activeCaptureProfile.name} size={CaptureWidth}x{CaptureHeight}");

        StoryCameraController cameraController =
            UnityEngine.Object.FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
        List<(StoryCameraZoneId zone, CinemachineCamera camera)> bindings = ReadCameraBindings(cameraController);
        StoryInteractable[] interactables = UnityEngine.Object
            .FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(HierarchyPath, StringComparer.Ordinal)
            .ToArray();
        StoryPlayerMovement player =
            UnityEngine.Object.FindFirstObjectByType<StoryPlayerMovement>(FindObjectsInactive.Include);
        Vector3 playerStart = player != null ? player.transform.position : Vector3.zero;

        Camera mainCamera = Camera.main ??
            UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (mainCamera == null || cameraController == null || bindings.Count == 0)
        {
            status("SKIP scene without camera controller/bindings: " + sceneName);
            return;
        }

        GameObject probeObject = new GameObject("~StoryPlayabilityAuditProbe")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Camera probe = probeObject.AddComponent<Camera>();
        probe.CopyFrom(mainCamera);
        probe.enabled = false;

        StringBuilder json = new StringBuilder();
        StringBuilder issues = new StringBuilder();
        int issueCount = 0;
        void Issue(string severity, string id, string text)
        {
            if (RuntimeStagedInteractions.Contains(id))
                severity = "BİLGİ/inşa-anı (runtime'da director kadraja taşır)";
            else
                issueCount++;
            issues.Append("- [").Append(severity).Append("] ").Append(id).Append(": ")
                .Append(text).Append('\n');
        }

        json.Append("{\n\"scene\": \"").Append(sceneName).Append("\",\n");
        json.Append("\"profile\": \"").Append(activeCaptureProfile.name).Append("\",\n");
        json.Append("\"resolution\": [").Append(CaptureWidth).Append(", ").Append(CaptureHeight).Append("],\n");
        json.Append("\"playerStart\": ").Append(FormatVector(playerStart)).Append(",\n");

        json.Append("\"cameras\": [\n");
        for (int index = 0; index < bindings.Count; index++)
        {
            (StoryCameraZoneId zone, CinemachineCamera camera) = bindings[index];
            json.Append("  {\"zone\": \"").Append(zone).Append("\"");
            if (camera == null)
            {
                json.Append(", \"missing\": true}");
                Issue("CRITICAL", "CameraBinding/" + zone, "Zone binding'inde kamera referansı yok.");
            }
            else
            {
                Vector3 position = camera.transform.position;
                json.Append(", \"name\": \"").Append(camera.name)
                    .Append("\", \"position\": ").Append(FormatVector(position))
                    .Append(", \"euler\": ").Append(FormatVector(camera.transform.rotation.eulerAngles))
                    .Append(", \"fov\": ").Append(F(camera.Lens.FieldOfView))
                    .Append(", \"near\": ").Append(F(camera.Lens.NearClipPlane))
                    .Append(", \"far\": ").Append(F(camera.Lens.FarClipPlane));
                if (Physics.Raycast(position, camera.transform.forward, out RaycastHit forwardHit, 200f,
                        ~0, QueryTriggerInteraction.Ignore))
                {
                    json.Append(", \"forwardHit\": \"").Append(Escape(HierarchyPath(forwardHit.transform)))
                        .Append("\", \"forwardHitDistance\": ").Append(F(forwardHit.distance));
                    if (forwardHit.distance < 0.42f)
                        Issue("HIGH", "Camera/" + zone,
                            $"Kamera bir yüzeyin içinde/çok yakınında (ileri ışın {forwardHit.distance:F2}m'de '{forwardHit.transform.name}').");
                }
                Collider cameraOverlap = Physics.OverlapSphere(
                        position, CameraClearanceRadius, ~0, QueryTriggerInteraction.Ignore)
                    .FirstOrDefault(candidate => candidate.GetComponentInParent<Animator>() == null);
                if (cameraOverlap != null)
                    Issue("CRITICAL", "Camera/" + zone,
                        $"Kamera {CameraClearanceRadius:F2}m güvenlik hacminde '{cameraOverlap.transform.name}' collider'ıyla çakışıyor.");
                json.Append('}');
            }
            json.Append(index < bindings.Count - 1 ? ",\n" : "\n");
        }
        json.Append("],\n");

        var duplicateZones = bindings.GroupBy(binding => binding.zone)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (StoryCameraZoneId duplicate in duplicateZones)
            Issue("HIGH", "CameraBinding/" + duplicate, "Aynı zone için birden fazla kamera binding'i var.");

        json.Append("\"interactables\": [\n");
        for (int index = 0; index < interactables.Length; index++)
        {
            StoryInteractable interactable = interactables[index];
            SerializedObject serialized = new SerializedObject(interactable);
            bool autoTrigger = serialized.FindProperty("autoTriggerOnPlayerEnter").boolValue;
            bool availableOnStart = serialized.FindProperty("availableOnStart").boolValue;
            bool interactFromAnywhere = serialized.FindProperty("interactFromAnywhere").boolValue;
            Color32 color = MarkerPalette[index % MarkerPalette.Length];

            List<GameObject> reactivated = TemporarilyActivate(interactable.transform);
            Physics.SyncTransforms();
            Bounds bounds = ResolveBounds(interactable);
            Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);
            var draggable = interactable.GetComponent<DraggableItem>();
            Component dropZone = draggable != null ? draggable.DropZoneOverride : null;

            json.Append("  {\"id\": \"").Append(Escape(interactable.InteractionId))
                .Append("\", \"path\": \"").Append(Escape(HierarchyPath(interactable.transform)))
                .Append("\", \"prompt\": \"").Append(Escape(interactable.Prompt))
                .Append("\", \"gesture\": \"").Append(interactable.InteractionGesture)
                .Append("\", \"gestureCount\": ").Append(interactable.RequiredGestureCount)
                .Append(", \"focusZone\": \"").Append(interactable.FocusCameraZone)
                .Append("\", \"returnZone\": \"")
                .Append(interactable.ReturnCameraAfterCompletion ? interactable.ReturnCameraZone.ToString() : "-")
                .Append("\", \"activeInHierarchy\": ").Append(B(reactivated.Count == 0))
                .Append(", \"availableOnStart\": ").Append(B(availableOnStart))
                .Append(", \"autoTrigger\": ").Append(B(autoTrigger))
                .Append(", \"interactFromAnywhere\": ").Append(B(interactFromAnywhere))
                .Append(", \"center\": ").Append(FormatVector(bounds.center))
                .Append(", \"size\": ").Append(FormatVector(bounds.size))
                .Append(", \"colliderCount\": ").Append(colliders.Length)
                .Append(", \"markerColor\": \"").Append(ColorHex(color)).Append('"');

            string label = string.IsNullOrEmpty(interactable.InteractionId)
                ? interactable.name
                : interactable.InteractionId;

            if (colliders.Length == 0 && !autoTrigger)
                Issue("CRITICAL", label, "Hiç collider yok; dünya dokunuşuyla asla seçilemez.");

            bool needsDrag = interactable.InteractionGesture == StoryInteractionGesture.DragToBag ||
                             interactable.InteractionGesture == StoryInteractionGesture.DragToTarget;
            if (needsDrag)
            {
                json.Append(", \"hasDraggable\": ").Append(B(draggable != null));
                if (draggable == null)
                    Issue("CRITICAL", label,
                        "Gesture " + interactable.InteractionGesture +
                        " ama aynı GameObject üzerinde DraggableItem yok (TouchManager GetComponent ile arıyor); sürükleme 'Bu nesne şu anda sürüklenemiyor' çıkmazına düşer.");
                else if (interactable.InteractionGesture == StoryInteractionGesture.DragToTarget && dropZone == null)
                    Issue("HIGH", label,
                        "DragToTarget ama DraggableItem.dropZoneOverride boş; ortak çanta drop zone'una düşer.");
            }

            if ((interactable.InteractionGesture == StoryInteractionGesture.SwipeHorizontal ||
                 interactable.InteractionGesture == StoryInteractionGesture.DragToTarget) &&
                interactable.GestureTarget == null && interactable.InteractionGesture == StoryInteractionGesture.SwipeHorizontal)
                json.Append(", \"gestureTargetMissing\": true");

            // NavMesh erişilebilirliği: yaklaşma gerektiren etkileşimlerde varış noktası
            // navmesh'ten uzaksa MoveTo callback'i menzil şartını asla sağlayamaz.
            if (!interactFromAnywhere && !autoTrigger)
            {
                Vector3 point = interactable.InteractionPoint.position;
                bool sampled = NavMesh.SamplePosition(point, out NavMeshHit navHit, 2.5f, NavMesh.AllAreas);
                float sampleOffset = sampled ? Vector3.Distance(navHit.position, point) : float.PositiveInfinity;
                bool pathComplete = false;
                Vector3 routeStart = ResolveRouteStart(sceneName, interactable, playerStart);
                if (sampled && NavMesh.SamplePosition(routeStart, out NavMeshHit startHit, 2.5f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    pathComplete = NavMesh.CalculatePath(startHit.position, navHit.position, NavMesh.AllAreas, path) &&
                                   path.status == NavMeshPathStatus.PathComplete;
                }
                json.Append(", \"navSampleOffset\": ").Append(F(Mathf.Min(sampleOffset, 999f)))
                    .Append(", \"routeStart\": ").Append(FormatVector(routeStart))
                    .Append(", \"navPathComplete\": ").Append(B(pathComplete));
                float allowed = interactable.InteractionRange + 0.3f;
                if (!sampled)
                    Issue("CRITICAL", label,
                        "InteractionPoint navmesh'e 2.5m içinde örneklenemedi; yaklaşma etkileşimi hiç hazır olamaz.");
                else if (sampleOffset > allowed)
                    Issue("HIGH", label,
                        $"InteractionPoint navmesh'ten {sampleOffset:F2}m uzakta; izinli menzil {allowed:F2}m — varışta 'ready' tetiklenmeyebilir.");
                else if (!pathComplete && sampleOffset <= 0.6f)
                    // Nokta mesh üzerinde ama başlangıçtan yol kapalı: uzun rotalı
                    // sahnelerde kapılar aşamalı açıldığı için genellikle tasarım
                    // gereğidir. Yine de raporda görünür kalır (masa üstü adası gibi
                    // gerçek kopukluklar da aynı imzayı verebilir).
                    Issue("BİLGİ/rota-gating olabilir", label,
                        "Nokta mesh üzerinde; ilgili aşama başlangıcından tam yol yok (aşamalı açılan kapı/rota olabilir).");
                else if (!pathComplete)
                    Issue("HIGH", label, "İlgili aşama başlangıcından interaction point'e tam NavMesh yolu yok.");
            }

            json.Append(", \"zones\": [");
            bool firstZone = true;
            foreach ((StoryCameraZoneId zone, CinemachineCamera zoneCamera) in bindings)
            {
                if (zoneCamera == null)
                    continue;
                ZoneVisibility visibility = MeasureVisibility(probe, zoneCamera, bounds, interactable);
                if (!firstZone)
                    json.Append(", ");
                firstZone = false;
                json.Append("{\"zone\": \"").Append(zone)
                    .Append("\", \"centerViewport\": [").Append(F(visibility.center.x)).Append(", ")
                    .Append(F(visibility.center.y)).Append(']')
                    .Append(", \"inFront\": ").Append(B(visibility.inFront))
                    .Append(", \"centerInView\": ").Append(B(visibility.centerInView))
                    .Append(", \"visibleFraction\": ").Append(F(visibility.visibleCornerFraction))
                    .Append(", \"pixelSize\": [").Append(F(visibility.pixelSize.x)).Append(", ")
                    .Append(F(visibility.pixelSize.y)).Append(']')
                    .Append(", \"occludedBy\": \"").Append(Escape(visibility.occluder ?? string.Empty))
                    .Append("\", \"distance\": ").Append(F(visibility.distance))
                    .Append('}');

                if (zone == interactable.FocusCameraZone && !autoTrigger)
                {
                    // Oto-tetiklenen hacimler (ör. cam tehlikesi zemin trigger'ı)
                    // hiç dokunulmadığı için kadraj/occlusion şartı aranmaz.
                    if (!visibility.inFront || !visibility.centerInView)
                        Issue("CRITICAL", label,
                            $"Odak kamerası {zone} nesneyi kadraja almıyor (viewport {visibility.center.x:F2},{visibility.center.y:F2}).");
                    else if (visibility.occluder != null)
                        Issue("CRITICAL", label,
                            $"Odak kamerası {zone} içinde nesnenin önü '{visibility.occluder}' ile kapalı; dokunma ışını oraya çarpar.");
                    else if (visibility.visibleCornerFraction < MinVisibleFraction)
                        Issue("MEDIUM", label,
                            $"Odak kamerası {zone} nesnenin yalnızca %{visibility.visibleCornerFraction * 100f:F0} kadarını kadrajda tutuyor.");
                    if (!autoTrigger && visibility.inFront && visibility.centerInView &&
                        Mathf.Min(visibility.pixelSize.x, visibility.pixelSize.y) < MinTapPixels)
                        Issue("MEDIUM", label,
                            $"Odak kamerasında dokunma hedefi {visibility.pixelSize.x:F0}x{visibility.pixelSize.y:F0}px — mobil için çok küçük.");
                }
            }
            json.Append(']');

            if (interactable.GestureTarget != null && interactable.FocusCameraZone != StoryCameraZoneId.None)
            {
                CinemachineCamera focusCamera = bindings
                    .FirstOrDefault(binding => binding.zone == interactable.FocusCameraZone).camera;
                if (focusCamera != null)
                {
                    ApplyLens(probe, focusCamera);
                    Vector3 viewport = probe.WorldToViewportPoint(interactable.GestureTarget.position);
                    bool targetVisible = IsInsideMobileSafeViewport(viewport);
                    json.Append(", \"gestureTargetViewport\": [").Append(F(viewport.x)).Append(", ")
                        .Append(F(viewport.y)).Append(", ").Append(F(viewport.z)).Append(']');
                    if (!targetVisible)
                        Issue("CRITICAL", label,
                            $"Sürükleme/jest hedefi ({HierarchyPath(interactable.GestureTarget)}) odak kamerası {interactable.FocusCameraZone} kadrajının dışında.");
                }
            }

            if (needsDrag && draggable != null && dropZone != null &&
                interactable.FocusCameraZone != StoryCameraZoneId.None)
            {
                CinemachineCamera focusCamera = bindings
                    .FirstOrDefault(binding => binding.zone == interactable.FocusCameraZone).camera;
                if (focusCamera != null)
                {
                    ApplyLens(probe, focusCamera);
                    Vector3 viewport = probe.WorldToViewportPoint(dropZone.transform.position);
                    bool zoneVisible = IsInsideMobileSafeViewport(viewport);
                    json.Append(", \"dropZoneViewport\": [").Append(F(viewport.x)).Append(", ")
                        .Append(F(viewport.y)).Append(", ").Append(F(viewport.z)).Append(']');
                    if (!zoneVisible)
                        Issue("CRITICAL", label,
                            $"Bırakma hedefi ({HierarchyPath(dropZone.transform)}) odak kamerası {interactable.FocusCameraZone} kadrajının dışında.");
                }
            }

            RestoreActivation(reactivated);
            json.Append('}').Append(index < interactables.Length - 1 ? ",\n" : "\n");
        }
        json.Append("]\n}\n");

        File.WriteAllText(Path.Combine(sceneOutputRoot, "report.json"), json.ToString());

        // Zone başına temiz + işaretli görüntü. İşaretler odak kamerası bu zone olan
        // etkileşimlerin merkez/çerçevesini ve jest hedeflerini gösterir.
        for (int index = 0; index < bindings.Count; index++)
        {
            (StoryCameraZoneId zone, CinemachineCamera zoneCamera) = bindings[index];
            if (zoneCamera == null)
                continue;
            ApplyLens(probe, zoneCamera);
            Texture2D capture = RenderProbe(probe);
            SavePng(capture, Path.Combine(sceneOutputRoot, $"zone_{index + 1:D2}_{zone}.png"));

            for (int itemIndex = 0; itemIndex < interactables.Length; itemIndex++)
            {
                StoryInteractable interactable = interactables[itemIndex];
                if (interactable.FocusCameraZone != zone)
                    continue;
                Color32 color = MarkerPalette[itemIndex % MarkerPalette.Length];
                List<GameObject> reactivated = TemporarilyActivate(interactable.transform);
                Bounds bounds = ResolveBounds(interactable);
                DrawBoundsMarker(capture, probe, bounds, color);
                if (interactable.GestureTarget != null)
                    DrawCross(capture, probe, interactable.GestureTarget.position, color);
                var draggable = interactable.GetComponent<DraggableItem>();
                if (draggable != null && draggable.DropZoneOverride != null)
                    DrawCross(capture, probe, draggable.DropZoneOverride.transform.position, color);
                RestoreActivation(reactivated);
            }
            SavePng(capture, Path.Combine(sceneOutputRoot, $"zone_{index + 1:D2}_{zone}_annotated.png"));
            UnityEngine.Object.DestroyImmediate(capture);
        }

        UnityEngine.Object.DestroyImmediate(probeObject);

        StringBuilder summary = new StringBuilder();
        summary.Append("# ").Append(sceneName).Append(" oynanabilirlik denetimi\n\n");
        summary.Append("Profil: ").Append(activeCaptureProfile.name).Append(" — ")
            .Append(CaptureWidth).Append('x').Append(CaptureHeight).Append("\n\n");
        summary.Append("Kamera sayısı: ").Append(bindings.Count)
            .Append(" — Etkileşim sayısı: ").Append(interactables.Length).Append("\n\n");
        summary.Append("## Tespit edilen sorunlar (").Append(issueCount).Append(")\n\n");
        summary.Append(issueCount == 0 ? "Sorun bulunamadı.\n" : issues.ToString());
        File.WriteAllText(Path.Combine(sceneOutputRoot, "issues.md"), summary.ToString());
        status($"SCENE_DONE {sceneName} profile={activeCaptureProfile.name} issues={issueCount}");
    }

    private static Vector3 ResolveRouteStart(
        string sceneName,
        StoryInteractable interactable,
        Vector3 playerStart)
    {
        if (sceneName != "Story_04_RebuildPreview" || interactable == null)
            return playerStart;

        string id = interactable.InteractionId ?? string.Empty;
        string anchorName = null;
        if (id == "evac.r04.route.stairs" || id == "evac.r04.route.elevator_unsafe")
            anchorName = "R04_CorridorPoint";
        else if (id == "evac.r04.stairs.upper" || id == "evac.r04.aftershock.handrail")
            anchorName = "R04_StairDoorPoint";
        else if (id == "evac.r04.stairs.lower")
            anchorName = "R04_UpperLandingPoint";
        else if (id.StartsWith("evac.r04.neighbor.", StringComparison.Ordinal))
            anchorName = "R04_LowerLandingPoint";
        else if (id == "evac.r04.exit.door")
            anchorName = "R04_NeighborPoint";
        else if (id == "evac.r04.exit.facade_clear")
            anchorName = "OutsideStandPoint";
        else if (id.StartsWith("evac.r04.street.", StringComparison.Ordinal))
            anchorName = "R04_FacadePoint";
        else if (id.StartsWith("evac.r04.assembly.", StringComparison.Ordinal))
            anchorName = "AssemblyApproachPoint";

        Transform anchor = FindSceneTransform(anchorName);
        return anchor != null ? anchor.position : playerStart;
    }

    private static Transform FindSceneTransform(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == name);
    }

    private struct ZoneVisibility
    {
        public Vector2 center;
        public bool inFront;
        public bool centerInView;
        public float visibleCornerFraction;
        public Vector2 pixelSize;
        public string occluder;
        public float distance;
    }

    private static ZoneVisibility MeasureVisibility(
        Camera probe, CinemachineCamera zoneCamera, Bounds bounds, StoryInteractable interactable)
    {
        ApplyLens(probe, zoneCamera);
        ZoneVisibility result = default;
        Vector3 viewportCenter = probe.WorldToViewportPoint(bounds.center);
        result.center = new Vector2(viewportCenter.x, viewportCenter.y);
        result.inFront = viewportCenter.z > 0f;
        result.centerInView = IsInsideMobileSafeViewport(viewportCenter);
        result.distance = Vector3.Distance(probe.transform.position, bounds.center);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        int visibleCorners = 0;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 world = bounds.center + Vector3.Scale(bounds.extents, CornerSign(corner));
            Vector3 viewport = probe.WorldToViewportPoint(world);
            if (viewport.z <= 0f)
                continue;
            min = Vector2.Min(min, new Vector2(viewport.x, viewport.y));
            max = Vector2.Max(max, new Vector2(viewport.x, viewport.y));
            if (viewport.x is >= 0f and <= 1f && viewport.y is >= 0f and <= 1f)
                visibleCorners++;
        }
        result.visibleCornerFraction = visibleCorners / 8f;
        result.pixelSize = visibleCorners > 0
            ? new Vector2((max.x - min.x) * CaptureWidth, (max.y - min.y) * CaptureHeight)
            : Vector2.zero;

        if (result.inFront)
        {
            Vector3 origin = probe.transform.position;
            Vector3 direction = bounds.center - origin;
            RaycastHit[] hits = Physics.RaycastAll(
                origin, direction.normalized, direction.magnitude + 0.5f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                StoryInteractable owner = hit.collider.GetComponentInParent<StoryInteractable>();
                if (owner == interactable)
                    break;
                if (owner != null)
                    continue;
                if (hit.collider.isTrigger)
                    continue;
                if (hit.distance >= direction.magnitude - 0.05f)
                    break;
                result.occluder = HierarchyPath(hit.transform);
                break;
            }
        }
        return result;
    }

    private static bool IsInsideMobileSafeViewport(Vector3 viewport)
    {
        return viewport.z > 0f &&
               viewport.x >= SafeViewportMinX && viewport.x <= SafeViewportMaxX &&
               viewport.y >= SafeViewportMinY && viewport.y <= SafeViewportMaxY;
    }

    private static Vector3 CornerSign(int index)
    {
        return new Vector3((index & 1) == 0 ? -1f : 1f, (index & 2) == 0 ? -1f : 1f, (index & 4) == 0 ? -1f : 1f);
    }

    private static void ApplyLens(Camera probe, CinemachineCamera zoneCamera)
    {
        probe.transform.SetPositionAndRotation(
            zoneCamera.transform.position, zoneCamera.transform.rotation);
        probe.fieldOfView = zoneCamera.Lens.FieldOfView;
        probe.nearClipPlane = Mathf.Max(0.01f, zoneCamera.Lens.NearClipPlane);
        probe.farClipPlane = zoneCamera.Lens.FarClipPlane;
        probe.aspect = (float)CaptureWidth / CaptureHeight;
    }

    private static Texture2D RenderProbe(Camera probe)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
        try
        {
            target.Create();
            probe.targetTexture = target;
            bool rendered = false;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                try
                {
                    RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest
                    {
                        destination = target
                    };
                    if (RenderPipeline.SupportsRenderRequest(probe, request))
                    {
                        RenderPipeline.SubmitRenderRequest(probe, request);
                        rendered = true;
                    }
                }
                catch (Exception)
                {
                    rendered = false;
                }
            }
            if (!rendered)
                probe.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0, false);
            texture.Apply(false, false);
            return texture;
        }
        finally
        {
            probe.targetTexture = null;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void SavePng(Texture2D texture, string path)
    {
        File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
    }

    private static void DrawBoundsMarker(Texture2D texture, Camera probe, Bounds bounds, Color32 color)
    {
        Vector3 viewportCenter = probe.WorldToViewportPoint(bounds.center);
        if (viewportCenter.z > 0f)
        {
            int x = Mathf.RoundToInt(viewportCenter.x * CaptureWidth);
            int y = Mathf.RoundToInt(viewportCenter.y * CaptureHeight);
            DrawRing(texture, x, y, 9, 2, color);
            DrawFilledRect(texture, x - 1, y - 1, 3, 3, color);
        }

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        bool any = false;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 world = bounds.center + Vector3.Scale(bounds.extents, CornerSign(corner));
            Vector3 viewport = probe.WorldToViewportPoint(world);
            if (viewport.z <= 0f)
                continue;
            any = true;
            min = Vector2.Min(min, new Vector2(viewport.x, viewport.y));
            max = Vector2.Max(max, new Vector2(viewport.x, viewport.y));
        }
        if (!any)
            return;
        DrawRectOutline(
            texture,
            Mathf.RoundToInt(min.x * CaptureWidth), Mathf.RoundToInt(min.y * CaptureHeight),
            Mathf.RoundToInt((max.x - min.x) * CaptureWidth), Mathf.RoundToInt((max.y - min.y) * CaptureHeight),
            color);
    }

    private static void DrawCross(Texture2D texture, Camera probe, Vector3 world, Color32 color)
    {
        Vector3 viewport = probe.WorldToViewportPoint(world);
        if (viewport.z <= 0f)
            return;
        int x = Mathf.RoundToInt(viewport.x * CaptureWidth);
        int y = Mathf.RoundToInt(viewport.y * CaptureHeight);
        for (int offset = -8; offset <= 8; offset++)
        {
            PutPixel(texture, x + offset, y + offset, color);
            PutPixel(texture, x + offset, y - offset, color);
            PutPixel(texture, x + offset + 1, y + offset, color);
            PutPixel(texture, x + offset + 1, y - offset, color);
        }
    }

    private static void DrawRing(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color32 color)
    {
        for (int y = -radius - thickness; y <= radius + thickness; y++)
        {
            for (int x = -radius - thickness; x <= radius + thickness; x++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance >= radius - thickness * 0.5f && distance <= radius + thickness * 0.5f)
                    PutPixel(texture, centerX + x, centerY + y, color);
            }
        }
    }

    private static void DrawFilledRect(Texture2D texture, int startX, int startY, int width, int height, Color32 color)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                PutPixel(texture, startX + x, startY + y, color);
    }

    private static void DrawRectOutline(Texture2D texture, int startX, int startY, int width, int height, Color32 color)
    {
        for (int x = 0; x <= width; x++)
        {
            PutPixel(texture, startX + x, startY, color);
            PutPixel(texture, startX + x, startY + height, color);
        }
        for (int y = 0; y <= height; y++)
        {
            PutPixel(texture, startX, startY + y, color);
            PutPixel(texture, startX + width, startY + y, color);
        }
    }

    private static void PutPixel(Texture2D texture, int x, int y, Color32 color)
    {
        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            texture.SetPixel(x, y, color);
    }

    private static List<GameObject> TemporarilyActivate(Transform target)
    {
        List<GameObject> reactivated = new List<GameObject>();
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
                reactivated.Add(current.gameObject);
            }
            current = current.parent;
        }
        return reactivated;
    }

    private static void RestoreActivation(List<GameObject> reactivated)
    {
        foreach (GameObject go in reactivated)
            go.SetActive(false);
        if (reactivated.Count > 0)
            Physics.SyncTransforms();
    }

    private static Bounds ResolveBounds(StoryInteractable interactable)
    {
        Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(interactable.transform.position, Vector3.zero);
        foreach (Collider collider in colliders)
        {
            if (!collider.gameObject.activeInHierarchy)
                continue;
            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }
        if (hasBounds)
            return bounds;

        Renderer[] renderers = interactable.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.gameObject.activeInHierarchy)
                continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return hasBounds ? bounds : new Bounds(interactable.transform.position, Vector3.one * 0.2f);
    }

    private static List<(StoryCameraZoneId, CinemachineCamera)> ReadCameraBindings(
        StoryCameraController controller)
    {
        List<(StoryCameraZoneId, CinemachineCamera)> bindings = new List<(StoryCameraZoneId, CinemachineCamera)>();
        if (controller == null)
            return bindings;
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty cameras = serialized.FindProperty("cameras");
        if (cameras == null)
            return bindings;
        for (int index = 0; index < cameras.arraySize; index++)
        {
            SerializedProperty element = cameras.GetArrayElementAtIndex(index);
            bindings.Add((
                (StoryCameraZoneId)element.FindPropertyRelative("zone").intValue,
                element.FindPropertyRelative("camera").objectReferenceValue as CinemachineCamera));
        }
        return bindings;
    }

    private static string HierarchyPath(Transform target)
    {
        if (target == null)
            return "<missing>";
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }

    private static string HierarchyPath(StoryInteractable interactable)
    {
        return HierarchyPath(interactable.transform);
    }

    private static string FormatVector(Vector3 value)
    {
        return "[" + F(value.x) + ", " + F(value.y) + ", " + F(value.z) + "]";
    }

    private static string F(float value)
    {
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static string B(bool value)
    {
        return value ? "true" : "false";
    }

    private static string ColorHex(Color32 color)
    {
        return $"#{color.r:X2}{color.g:X2}{color.b:X2}";
    }

    private static string Escape(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
