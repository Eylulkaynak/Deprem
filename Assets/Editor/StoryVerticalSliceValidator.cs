using System;
using System.Linq;
using Deprem.Story;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class StoryVerticalSliceValidator
{
    private const string ScenePath = "Assets/Scenes/Story_03_Quake.unity";

    [MenuItem("Tools/Deprem Story/Validate Story_03_Quake")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    public static void Validate(bool showDialog)
    {
        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(UnityEngine.Object.FindObjectsByType<StoryTouchManager>(FindObjectsSortMode.None).Length == 1, "Tek StoryTouchManager");
            Require(UnityEngine.Object.FindObjectsByType<StoryPlayerMovement>(FindObjectsSortMode.None).Length == 1, "Tek StoryPlayerMovement");
            Require(UnityEngine.Object.FindObjectsByType<StorySiblingFollower>(FindObjectsSortMode.None).Length == 1, "Can için tek ortak takip navigasyonu");
            Require(UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None).Length == 2, "Deniz ve Can için iki NavMeshAgent");
            StoryGameManager sessionManager = UnityEngine.Object.FindObjectsByType<StoryGameManager>(FindObjectsSortMode.None).Single();
            Require(sessionManager.transform.parent == null, "Kalıcı hikâye oturumu sahne kökünde");
            StoryInteractable[] interactions = UnityEngine.Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(interactions.Length >= 32, $"32+ sıralı sahne etkileşimi (bulunan: {interactions.Length}; {string.Join(", ", interactions.Select(item => item.InteractionId))})");
            Require(interactions.Select(item => item.InteractionId).Distinct(StringComparer.Ordinal).Count() == interactions.Length, "Benzersiz etkileşim kimlikleri");
            Require(interactions.All(item => item.EstimatedInteractionSeconds > 0f), "Etkileşim tempo süreleri");
            Require(interactions.All(item => item.RequiredGestureCount >= 1), "Geçerli jest tekrar sayıları");
            Require(interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.RepeatedTap) &&
                    interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.SwipeDown) &&
                    interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.SwipeHorizontal) &&
                    interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.Approach) &&
                    interactions.Any(item => item.InteractionGesture == StoryInteractionGesture.WorldHold),
                "Konumsal yaklaşma, sabit tutma ve dokunma jestleri");
            StoryInteractable[] misalignedHighlights = interactions.Where(item => !HighlightMatchesInteractionPoint(item)).ToArray();
            Require(misalignedHighlights.Length == 0, "Etkileşim halkaları hedef noktalarıyla hizalı: " +
                string.Join(" | ", misalignedHighlights.Select(item =>
                    $"{item.InteractionId} halka={item.HighlightRoot?.transform.position} hedef={item.InteractionPoint?.position}")));
            Require(interactions.Where(item => !item.InteractFromAnywhere).All(IsOnNavMesh), "Yaklaşılabilir etkileşim noktaları NavMesh üzerinde");
            Require(UnityEngine.Object.FindObjectsByType<StoryActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "Etkileşimler doğrudan sahnede; ortada bağlamsal görev düğmesi yok");
            RectTransform pauseOverlay = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(rect => rect.name == "PauseOverlay");
            RectTransform completionOverlay = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(rect => rect.name == "CompletionOverlay");
            Require(pauseOverlay.anchorMin == Vector2.zero && pauseOverlay.anchorMax == Vector2.one && pauseOverlay.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                "Pause modal blocks background input across the safe area");
            Require(completionOverlay.anchorMin == Vector2.zero && completionOverlay.anchorMax == Vector2.one && completionOverlay.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                "Completion modal blocks background input across the safe area");
            string[] requiredButtons = { "PauseButton", "ResumeButton", "RetryCheckpointButton", "ReplayStoryButton", "ChapterSelectionButton" };
            Require(requiredButtons.All(name => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(button => button.name == name)), "Pause, retry, replay and chapter-selection actions");
            StorySequenceDirector sequence = UnityEngine.Object.FindObjectsByType<StorySequenceDirector>(FindObjectsSortMode.None).Single();
            Require(sequence.MinimumCompletionDuration >= 480f && sequence.MinimumCompletionDuration <= 720f, "8–12 dakika tamamlama kapısı");
            Require(sequence.EstimatedFirstPlayDuration >= 480f && sequence.EstimatedFirstPlayDuration <= 720f, "8–12 dakika yazılmış tempo");
            Require(sequence.AuthoredBeatCount >= 28, "28+ oynanış ritmi");
            CinemachineCamera[] cameras = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            Require(cameras.Length >= 10, "10+ composed Cinemachine cameras with subject-specific interaction shots");
            CinemachineCamera[] followCameras = cameras
                .Where(camera => camera.GetComponent<CinemachinePositionComposer>() != null && camera.Follow != null)
                .ToArray();
            Require(followCameras.Length >= 3
                && followCameras.All(camera =>
                {
                    CinemachineDeoccluder deoccluder = camera.GetComponent<CinemachineDeoccluder>();
                    return deoccluder != null
                        && deoccluder.AvoidObstacles.Enabled
                        && deoccluder.AvoidObstacles.Strategy
                            == CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
                }), "Room follow cameras pull in front of walls instead of rendering a wall-only frame");
            Require(cameras.Any(camera => camera.name == "CM_InspectTableLegs") && cameras.Any(camera => camera.name == "CM_InspectBrokenGlass"),
                "Separate cinematic shots for the table legs and broken glass");
            CinemachineCamera tableShot = cameras.Single(camera => camera.name == "CM_InspectTableLegs");
            CinemachineCamera windowShot = cameras.Single(camera => camera.name == "CM_InspectWindow");
            CinemachineCamera glassShot = cameras.Single(camera => camera.name == "CM_InspectBrokenGlass");
            Require(ShotContainsBounds(tableShot, CombinedBounds(GameObject.Find("SafeTable")), 9f / 16f, 0.025f),
                "9:16 masa planında masa gövdesi ve dört ayak kadraj içinde");
            Require(ShotContainsBounds(windowShot, CombinedBounds(GameObject.Find("Window_DangerZone")), 9f / 16f, 0.015f),
                "9:16 pencere planında cam, çerçeve ve perde kadraj içinde");
            Require(ShotContainsBounds(glassShot, CombinedBounds(FindByNameIncludingInactive("BrokenGlass_Hazard")), 9f / 16f, 0.035f),
                "9:16 kırık cam planında bütün parçalar kadraj içinde");
            Require(tableShot.transform.position.z <= -4.5f && Vector3.Distance(tableShot.transform.position, CombinedBounds(GameObject.Find("SafeTable")).center) >= 5f,
                "Masa incelemesi nesneye yapışmayan temiz kurucu plan");
            Require(glassShot.transform.position.y >= 1.5f && glassShot.transform.position.y <= 3f,
                "Kırık cam ve pencere bağlamını birlikte gösteren yüksek üç çeyrek plan");
            StoryInteractable glassInspection = interactions.Single(item => item.InteractionId == "Post_InspectGlass");
            Require(glassInspection.FocusCameraZone == StoryCameraZoneId.InspectBrokenGlass,
                "Broken glass inspection uses its contextual hazard shot rather than the intact-window shot");
            Require(cameras.Length >= 9, "9+ bestelenmiş Cinemachine kamerası ve etkileşim yakın planları");
            Require(cameras.Count(camera => camera.GetComponent<CinemachinePositionComposer>() != null && camera.Follow != null) >= 3,
                "Oda, deprem ve deprem sonrasında oyuncuyu alt üçlüde izleyen hareketli oynanış kameraları");
            Require(cameras.Single(camera => camera.name == "CM_Corridor").Follow == null,
                "Koridorda duvar ve tavan kirişlerinden kaçan sabit eşik planı");
            Require(UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).Single().navMeshData != null, "Baked NavMesh");
            Require(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 2, "Sahneye yerleştirilmiş VFX");
            Require(UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None).Any(director => director.playableAsset != null), "Deprem Timeline'ı");
            AnimatorController storyController = AssetDatabase.LoadAssetAtPath<AnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
            Require(storyController != null, "Story Animator Controller");
            string[] animatorParameters = storyController.parameters.Select(parameter => parameter.name).ToArray();
            Require(animatorParameters.Contains("Speed") && StoryAnimationLibraryBuilder.RequiredTriggers.All(animatorParameters.Contains),
                "Story Animator parameters");
            Animator[] characterAnimators =
            {
                GameObject.Find("Deniz_12")?.GetComponentInChildren<Animator>(),
                GameObject.Find("Can_8")?.GetComponentInChildren<Animator>()
            };
            Require(characterAnimators.Length == 2, "Deniz and Can Animator references");
            Require(characterAnimators.All(animator => animator != null && animator.runtimeAnimatorController == storyController && !animator.applyRootMotion &&
                                                       animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman),
                "Humanoid retarget and root-motion gate");
            Bounds tableBounds = CombinedBounds(GameObject.Find("SafeTable"));
            Bounds shelfBounds = CombinedBounds(GameObject.Find("Shelf_Secured"));
            Require(!tableBounds.Intersects(shelfBounds), $"Masa ve raf dünya uzayında çakışmıyor (masa: {tableBounds}, raf: {shelfBounds})");
            Vector2 tableXZ = new Vector2(tableBounds.center.x, tableBounds.center.z);
            Vector2 shelfXZ = new Vector2(shelfBounds.center.x, shelfBounds.center.z);
            Require(Vector2.Distance(tableXZ, shelfXZ) >= 3.2f, "Masa ve raf arasında okunaklı mesafe");
            Require(EditorBuildSettings.scenes.FirstOrDefault()?.path == "Assets/Scenes/Story_Rebuild_MainMenu.unity", "Yeni hikâye menüsü başlangıç sahnesi");
            Debug.Log("Story_03_Quake validation passed: input, NavMesh, checkpoints, cameras, character animation, authored VFX/Timeline and published story route are present.");
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_03_Quake yapısal doğrulamayı geçti.", "Tamam");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("Story_03_Quake doğrulama hatası: " + label);
    }

    private static Bounds CombinedBounds(GameObject root)
    {
        if (root == null)
            throw new InvalidOperationException("Doğrulama nesnesi bulunamadı.");
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(root.name + " görsel sınırı bulunamadı.");
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static bool HighlightMatchesInteractionPoint(StoryInteractable interactable)
    {
        if (interactable == null || interactable.HighlightRoot == null || interactable.InteractionPoint == null)
            return false;
        Renderer renderer = interactable.HighlightRoot.GetComponentInChildren<Renderer>(true);
        Vector3 highlightPosition = interactable.HighlightRoot.transform.position;
        return renderer != null && Vector2.Distance(
            new Vector2(highlightPosition.x, highlightPosition.z),
            new Vector2(interactable.InteractionPoint.position.x, interactable.InteractionPoint.position.z)) <= 0.08f;
    }

    private static bool IsOnNavMesh(StoryInteractable interactable)
    {
        return UnityEngine.AI.NavMesh.SamplePosition(interactable.InteractionPoint.position, out _, 1.75f, UnityEngine.AI.NavMesh.AllAreas);
    }

    private static GameObject FindByNameIncludingInactive(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == name)?.gameObject;
    }

    private static bool ShotContainsBounds(CinemachineCamera camera, Bounds bounds, float aspect, float margin)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 point = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
            Vector3 viewport = WorldToViewport(camera, point, aspect);
            if (viewport.z <= 0f || viewport.x < margin || viewport.x > 1f - margin || viewport.y < margin || viewport.y > 1f - margin)
            {
                Debug.LogWarning($"{camera.name} kadraj dışı nokta: world={point}, viewport={viewport}, " +
                                 $"bounds={bounds}, camera={camera.transform.position}, fov={camera.Lens.FieldOfView}");
                return false;
            }
        }
        return true;
    }

    private static Vector3 WorldToViewport(CinemachineCamera camera, Vector3 worldPoint, float aspect)
    {
        Vector3 local = camera.transform.InverseTransformPoint(worldPoint);
        float halfHeight = Mathf.Tan(camera.Lens.FieldOfView * Mathf.Deg2Rad * 0.5f) * local.z;
        float halfWidth = halfHeight * aspect;
        if (local.z <= 0f || halfHeight <= 0f || halfWidth <= 0f)
            return new Vector3(-1f, -1f, local.z);
        return new Vector3(0.5f + local.x / (2f * halfWidth), 0.5f + local.y / (2f * halfHeight), local.z);
    }
}
