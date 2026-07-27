using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Deprem.Story;
using NUnit.Framework;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class StoryProjectAuditTests
{
    private static readonly string[] StoryScenes =
    {
        "Assets/Scenes/Story_01_BagPreparation.unity",
        "Assets/Scenes/Story_02_HomeSafety.unity",
        "Assets/Scenes/Story_03_Quake.unity",
        "Assets/Scenes/Story_04_Evacuation.unity"
    };

    [Test]
    public void StoryRuntime_RespectsSingleInputOwnerAndAuthoredContentBoundary()
    {
        string[] runtimeFiles = Directory.GetFiles("Assets/Scripts/Story", "*.cs", SearchOption.AllDirectories);
        Assert.That(runtimeFiles, Is.Not.Empty);

        string[] inputOwners = runtimeFiles
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("Input.", System.StringComparison.Ordinal) ||
                       source.Contains("Touchscreen.current", System.StringComparison.Ordinal) ||
                       source.Contains("Mouse.current", System.StringComparison.Ordinal);
            })
            .Select(path => Path.GetFileName(path))
            .Distinct()
            .ToArray();
        Assert.That(inputOwners, Is.EqualTo(new[] { "StoryTouchManager.cs" }),
            "Yeni hikâye sahnelerinde ham dokunma/fare girişini yalnız StoryTouchManager okumalı.");

        foreach (string path in runtimeFiles)
        {
            string source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("new GameObject"),
                path + " runtime'da sahne nesnesi üretmemeli; içerik builder/prefab/sahnede hazırlanmalı.");
            Assert.That(source, Does.Not.Contain("AddComponent<"),
                path + " runtime'da component üretmemeli; component sahnede serialize edilmeli.");
            Assert.That(source, Does.Not.Contain("new Material"),
                path + " runtime'da materyal üretmemeli.");
            Assert.That(source, Does.Not.Contain("CreatePrimitive("),
                path + " runtime'da görsel primitive üretmemeli.");
            Assert.That(source, Does.Not.Contain("Camera.main.transform"),
                path + " ana kamerayı doğrudan sürmemeli; Cinemachine kamera öncelikleri kullanılmalı.");
        }
    }

    [TestCaseSource(nameof(StoryScenes))]
    public void StoryScene_HasNoInvisibleInputCameraOrNavigationDeadEnds(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        StoryPlayerMovement player = Object.FindFirstObjectByType<StoryPlayerMovement>();
        StoryTouchManager touch = Object.FindFirstObjectByType<StoryTouchManager>();
        StoryCameraController cameraController = Object.FindFirstObjectByType<StoryCameraController>();
        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        Assert.That(player, Is.Not.Null, scenePath);
        Assert.That(touch, Is.Not.Null, scenePath);
        Assert.That(cameraController, Is.Not.Null, scenePath);
        Assert.That(Object.FindObjectsByType<StoryTouchManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1), scenePath);
        Assert.That(Object.FindObjectsByType<StoryActionButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty, scenePath);

        // Kapalı kapılar runtime'da NavMesh'i carve eder. Rota bütünlüğünü kapıların
        // açılmış hikâye durumunda ölç; kapalı durumun carving sözleşmesi ayrı testtedir.
        foreach (NavMeshObstacle obstacle in Object.FindObjectsByType<NavMeshObstacle>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            NavMeshModifier modifier = obstacle.GetComponent<NavMeshModifier>();
            if (modifier != null && modifier.ignoreFromBuild)
                obstacle.enabled = false;
        }
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        Assert.That(surface, Is.Not.Null, scenePath);
        surface.BuildNavMesh();

        NavMeshAgent playerAgent = player.GetComponent<NavMeshAgent>();
        Assert.That(playerAgent.speed, Is.InRange(1.5f, 2f), scenePath + " natural walk speed");
        Assert.That(NavMesh.SamplePosition(player.transform.position, out NavMeshHit start, 2f, NavMesh.AllAreas),
            Is.True, scenePath + " player spawn");

        Assert.That(interactions.Select(item => item.InteractionId), Is.All.Not.Empty, scenePath);
        Assert.That(interactions.Select(item => item.InteractionId).Distinct().Count(), Is.EqualTo(interactions.Length),
            scenePath + " interaction ids must be unique");
        Assert.That(interactions.Select(item => item.Prompt), Is.All.Not.Empty, scenePath);

        foreach (StoryInteractable interactable in interactions)
        {
            Assert.That(interactable.GetComponentsInChildren<Collider>(true), Is.Not.Empty,
                scenePath + " " + interactable.InteractionId + " needs an authored world collider");
            if (!interactable.InteractFromAnywhere)
            {
                Assert.That(NavMesh.SamplePosition(interactable.InteractionPoint.position, out NavMeshHit target,
                    2f, NavMesh.AllAreas), Is.True, scenePath + " " + interactable.InteractionId);
                NavMeshPath path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path), Is.True,
                    scenePath + " " + interactable.InteractionId);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
                    scenePath + " " + interactable.InteractionId +
                    $" must not softlock behind an unreachable point; start={start.position}, target={target.position}, " +
                    $"corners={string.Join(" -> ", path.corners.Select(corner => corner.ToString()))}");
            }
        }

        StoryCameraBinding[] bindings = GetPrivate<StoryCameraBinding[]>(cameraController, "cameras");
        Assert.That(bindings, Is.Not.Null.And.Not.Empty, scenePath);
        Assert.That(bindings.Select(binding => binding.zone).Distinct().Count(), Is.EqualTo(bindings.Length),
            scenePath + " camera zones must be unique");
        Assert.That(bindings.All(binding => binding.camera != null), Is.True, scenePath);
        Dictionary<StoryCameraZoneId, CinemachineCamera> cameras =
            bindings.ToDictionary(binding => binding.zone, binding => binding.camera);
        foreach (StoryCameraZoneId requiredZone in RequiredCameraZones(scenePath))
            Assert.That(cameras.ContainsKey(requiredZone), Is.True,
                scenePath + " story director can request a camera zone with no authored binding: " + requiredZone);

        foreach (StoryInteractable interactable in interactions)
        {
            if (interactable.FocusCameraZone != StoryCameraZoneId.None)
            {
                Assert.That(cameras.ContainsKey(interactable.FocusCameraZone), Is.True,
                    scenePath + " missing focus camera for " + interactable.InteractionId);
                Vector3 subject = InteractionCenter(interactable);
                CinemachineCamera focusCamera = cameras[interactable.FocusCameraZone];
                CinemachinePositionComposer composer = focusCamera.GetComponent<CinemachinePositionComposer>();
                if (composer == null)
                {
                    AssertPortraitVisible(focusCamera, subject, scenePath + " " + interactable.InteractionId);
                    if (!interactable.InteractFromAnywhere)
                        AssertNotOccluded(focusCamera, interactable, subject,
                        scenePath + " " + interactable.InteractionId);
                }
                else
                {
                    Assert.That(focusCamera.Follow, Is.Not.Null,
                        scenePath + " " + focusCamera.name + " dynamic camera needs a follow subject");
                    Assert.That(composer.CameraDistance, Is.GreaterThan(1.5f),
                        scenePath + " " + focusCamera.name + " dynamic camera distance");
                }
            }
            if (interactable.ReturnCameraAfterCompletion)
                Assert.That(cameras.ContainsKey(interactable.ReturnCameraZone), Is.True,
                    scenePath + " missing return camera for " + interactable.InteractionId);
        }

        foreach (CinemachineCamera camera in cameras.Values)
            Assert.That(camera.Lens.FieldOfView, Is.InRange(38f, 50f), scenePath + " " + camera.name);

        StoryCameraZoneId validZone = bindings[0].zone;
        cameraController.ActivateZone(validZone, true);
        Dictionary<CinemachineCamera, int> prioritiesBeforeMissingZone =
            cameras.Values.ToDictionary(camera => camera, camera => camera.Priority.Value);
        // None means "keep the current authored shot"; it must be a silent no-op rather
        // than a false missing-camera error in interaction and visual QA runs.
        cameraController.ActivateZone(StoryCameraZoneId.None, true);
        Assert.That(cameraController.ActiveZone, Is.EqualTo(validZone),
            scenePath + " missing camera request must not blank every Cinemachine priority.");
        foreach (KeyValuePair<CinemachineCamera, int> entry in prioritiesBeforeMissingZone)
            Assert.That(entry.Key.Priority.Value, Is.EqualTo(entry.Value), scenePath + " " + entry.Key.name);

        string allUiText = string.Join("\n", Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Select(text => text.text ?? string.Empty));
        Assert.That(allUiText.ToUpperInvariant(), Does.Not.Contain("TIKLA"),
            scenePath + " must not turn interaction into a centered click instruction");
    }

    [TestCase("Assets/Scenes/Story_02_HomeSafety.unity", "HomeExitDoor_Closed")]
    [TestCase("Assets/Scenes/Story_03_Quake.unity", "Door_Closed")]
    [TestCase("Assets/Scenes/Story_04_Evacuation.unity", "StairDoor_Closed")]
    [TestCase("Assets/Scenes/Story_04_Evacuation.unity", "BuildingExitDoor_Closed")]
    public void DynamicClosedDoor_BlocksAtRuntimeWithoutLeavingPermanentBakedHole(string scenePath, string doorName)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject door = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(transform => transform.name == doorName).gameObject;
        NavMeshModifier modifier = door.GetComponentInChildren<NavMeshModifier>(true);
        NavMeshObstacle obstacle = door.GetComponentInChildren<NavMeshObstacle>(true);
        Assert.That(modifier, Is.Not.Null, doorName);
        Assert.That(modifier.ignoreFromBuild, Is.True, doorName);
        Assert.That(obstacle, Is.Not.Null, doorName);
        Assert.That(obstacle.carving, Is.True, doorName);
        Assert.That(obstacle.carveOnlyStationary, Is.False, doorName);
        Assert.That(obstacle.size.x * obstacle.size.y * obstacle.size.z, Is.GreaterThan(0.001f), doorName);
    }

    [TestCaseSource(nameof(StoryScenes))]
    public void StoryCharacters_UseCorrectControllersAndCollisionScale(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RuntimeAnimatorController child =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        RuntimeAnimatorController adult =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.AdultControllerPath);

        foreach (string childName in new[] { "Deniz_12", "Can_8" })
        {
            GameObject character = FindOptional(childName);
            if (character == null)
                continue;
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.That(animator.runtimeAnimatorController, Is.EqualTo(child), scenePath + " " + childName);
            Assert.That(animator.applyRootMotion, Is.False, scenePath + " " + childName);
        }

        foreach (string adultName in new[]
                 {
                     "Anne_Ayse", "Nermin_Neighbor_Landing", "Nermin_Neighbor_Street",
                     "Nermin_Neighbor_Assembly"
                 })
        {
            GameObject character = FindOptional(adultName);
            if (character == null)
                continue;
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.That(animator.runtimeAnimatorController, Is.EqualTo(adult), scenePath + " " + adultName);
        }

        StoryPlayerMovement movement = Object.FindFirstObjectByType<StoryPlayerMovement>();
        CapsuleCollider capsule = movement.GetComponent<CapsuleCollider>();
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Assert.That(capsule, Is.Not.Null, scenePath);
        Assert.That(agent, Is.Not.Null, scenePath);
        Assert.That(Mathf.Abs(capsule.radius - agent.radius), Is.LessThan(0.06f), scenePath);
        Assert.That(Mathf.Abs(capsule.height - agent.height), Is.LessThan(0.12f), scenePath);
    }

    private static Vector3 InteractionCenter(StoryInteractable interactable)
    {
        Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
            return interactable.transform.position;
        Bounds bounds = colliders[0].bounds;
        foreach (Collider collider in colliders.Skip(1))
            bounds.Encapsulate(collider.bounds);
        if (interactable.InteractFromAnywhere && interactable.InteractionPoint != null &&
            Vector3.Distance(bounds.center, interactable.InteractionPoint.position) > 4f)
            return interactable.InteractionPoint.position + Vector3.up * 0.65f;
        return bounds.center;
    }

    private static void AssertPortraitVisible(CinemachineCamera source, Vector3 subject, string label)
    {
        GameObject probeObject = new GameObject("StoryAuditCameraProbe");
        try
        {
            Camera probe = probeObject.AddComponent<Camera>();
            probe.enabled = false;
            probe.aspect = 1080f / 1920f;
            probe.fieldOfView = source.Lens.FieldOfView;
            probe.nearClipPlane = source.Lens.NearClipPlane;
            probe.farClipPlane = source.Lens.FarClipPlane;
            probe.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Vector3 viewport = probe.WorldToViewportPoint(subject);
            Assert.That(viewport.z, Is.GreaterThan(0f), label + " behind camera");
            Assert.That(viewport.x, Is.InRange(0.08f, 0.92f), label + " horizontal framing");
            Assert.That(viewport.y, Is.InRange(0.08f, 0.92f), label + " vertical framing");
        }
        finally
        {
            Object.DestroyImmediate(probeObject);
        }
    }

    private static void AssertNotOccluded(CinemachineCamera source, StoryInteractable interactable,
        Vector3 subject, string label)
    {
        Collider[] enclosing = Physics.OverlapSphere(source.transform.position, 0.06f, ~0,
            QueryTriggerInteraction.Ignore);
        Assert.That(enclosing, Is.Empty,
            label + " camera begins inside collider: " + string.Join(", ", enclosing.Select(item => item.name)));

        Vector3 delta = subject - source.transform.position;
        float distance = delta.magnitude;
        RaycastHit[] hits = Physics.RaycastAll(source.transform.position, delta.normalized, distance + 0.05f, ~0,
                QueryTriggerInteraction.Ignore)
            .OrderBy(hit => hit.distance)
            .ToArray();
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.transform.IsChildOf(interactable.transform) ||
                 interactable.transform.IsChildOf(hit.collider.transform)))
                return;
            if (hit.distance >= distance - 0.08f)
                return;
            Renderer visibleSurface = hit.collider != null ? hit.collider.GetComponent<Renderer>() : null;
            if (visibleSurface == null || !visibleSurface.enabled || !visibleSurface.gameObject.activeInHierarchy)
                continue;
            if (hit.collider.bounds.size.x < 1.5f && hit.collider.bounds.size.y < 1.5f &&
                hit.collider.bounds.size.z < 1.5f)
                continue;
            Assert.Fail(label + " occluded by " + hit.collider.name + " at " + hit.distance.ToString("F2") + "m");
        }
    }

    private static GameObject FindOptional(string name)
    {
        Transform transform = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == name);
        return transform != null ? transform.gameObject : null;
    }

    private static StoryCameraZoneId[] RequiredCameraZones(string scenePath)
    {
        string directorPath = scenePath switch
        {
            var path when path.EndsWith("Story_01_BagPreparation.unity", System.StringComparison.Ordinal) =>
                "Assets/Scripts/Story/StoryPreparationDirector.cs",
            var path when path.EndsWith("Story_02_HomeSafety.unity", System.StringComparison.Ordinal) =>
                "Assets/Scripts/Story/StoryHomeSafetyDirector.cs",
            var path when path.EndsWith("Story_03_Quake.unity", System.StringComparison.Ordinal) =>
                "Assets/Scripts/Story/StorySequenceDirector.cs",
            _ => "Assets/Scripts/Story/StoryEvacuationDirector.cs"
        };
        return Regex.Matches(File.ReadAllText(directorPath), @"StoryCameraZoneId\.([A-Za-z0-9_]+)")
            .Cast<Match>()
            .Select(match => (StoryCameraZoneId)System.Enum.Parse(typeof(StoryCameraZoneId), match.Groups[1].Value))
            .Distinct()
            .ToArray();
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName);
        return (T)field.GetValue(target);
    }
}
