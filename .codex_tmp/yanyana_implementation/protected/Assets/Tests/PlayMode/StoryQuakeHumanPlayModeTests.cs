using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public sealed class StoryQuakeHumanPlayModeTests
{
#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator QuakeRebuild_EntryShelfScriptedWalkArrivesWithoutTeleporting()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_03_RebuildPreview.unity");
        MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        director.enabled = false;
        FindBehaviour("StoryTouchManager").enabled = false;
        InvokePublic(player, "SetStoryInputLocked", false);
        InvokePublic(player, "SetNavigationEnabled", true);
        MonoBehaviour shoes = FindInteraction("quake.post.shoes");
        Transform destination = (Transform)Property(shoes, "InteractionPoint").GetValue(shoes);
        Vector3 start = player.transform.position;
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        bool sampledStart = NavMesh.SamplePosition(start, out NavMeshHit startHit, 20f, NavMesh.AllAreas);
        bool sampledTarget = NavMesh.SamplePosition(destination.position, out NavMeshHit targetHit, 20f, NavMesh.AllAreas);
        Debug.Log(
            $"ENTRY_NAV_PROBE onMesh={agent.isOnNavMesh} start={start} sampleStart={sampledStart}:{startHit.position} " +
            $"target={destination.position} sampleTarget={sampledTarget}:{targetHit.position}");
        foreach (Vector3 candidate in new[]
                 {
                     new Vector3(3.2f, 0f, 4.2f),
                     new Vector3(3.2f, 0f, 3.4f),
                     new Vector3(4f, 0f, 3.2f),
                     new Vector3(4f, 0f, 2.2f),
                     new Vector3(3.4f, 0f, 1.8f),
                     new Vector3(-3.4f, 0f, 3.2f)
                 })
        {
            object[] resolveArguments = { candidate, Vector3.zero };
            bool reachable = (bool)InvokePublicResult(player, "TryResolveReachableDestination", resolveArguments);
            Debug.Log($"ENTRY_NAV_CANDIDATE requested={candidate} reachable={reachable} resolved={resolveArguments[1]}");
        }
        bool arrived = false;
        Action callback = () => arrived = true;
        Assert.That((bool)InvokePublicResult(
                player,
                "MoveTo",
                destination,
                shoes.transform.position,
                callback),
            Is.True,
            "Giriş rafı için NavMesh yürüyüşü kabul edilmeli.");

        float deadline = Time.realtimeSinceStartup + 14f;
        while (!arrived && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(arrived, Is.True,
            $"Deniz giriş rafına varamadı. start={start}, current={player.transform.position}, " +
            $"target={destination.position}, remaining={agent.remainingDistance}, " +
            $"path={agent.pathStatus}, hasPath={agent.hasPath}, onNavMesh={agent.isOnNavMesh}.");
    }

    [UnityTest]
    [Timeout(240000)]
    public IEnumerator QuakeRebuild_HumanWorldTouchesAndAuthoredTimingCompleteFromStartToFinish()
    {
        float runStartedAt = Time.realtimeSinceStartup;
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_03_RebuildPreview.unity");

        MonoBehaviour touch = FindBehaviour("StoryTouchManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        MonoBehaviour manager = FindBehaviour("StoryGameManager");

        Assert.That((bool)Field(touch, "directWorldGestures").GetValue(touch), Is.True);
        Assert.That((float)Field(director, "revisedQuakeDelayAfterFamilyMoment").GetValue(director),
            Is.GreaterThanOrEqualTo(50f), "İnsan oynayışı testinde deprem öncesi süre kısaltılmamalı.");
        Assert.That((float)Field(director, "quakeMinimumDuration").GetValue(director),
            Is.GreaterThanOrEqualTo(20f), "Sarsıntı, dört gerçek dünya etkileşimini taşıyacak kadar uzun kalmalı.");

        yield return ReadSubtitlesHumanly(ui);

        // Sakin açılış: bütün hareketler ekrandaki gerçek nesneden başlar.
        yield return PerformWorldGesture(touch, ui, "quake.intro.wheel");
        yield return PerformWorldGesture(touch, ui, "quake.intro.car");

        yield return WaitForPhase(director, "Quake", 80f);
        AssertCheckpoint(manager, "QuakeStart");
        yield return ReadSubtitlesHumanly(ui);

        // Deprem: çıkışa koşmadan Can, masa, baş koruma ve tutunma sırası.
        yield return PerformWorldGesture(touch, ui, "quake.calm.can");
        yield return PerformWorldGesture(touch, ui, "quake.cover.crouch");
        AssertChildrenStaySeparatedUnderCover();
        yield return PerformWorldGesture(touch, ui, "quake.cover.head");
        AssertChildrenStaySeparatedUnderCover();
        yield return PerformWorldGesture(touch, ui, "quake.cover.grip");
        AssertChildrenStaySeparatedUnderCover();

        yield return WaitForPhase(director, "PostQuake", 65f);
        AssertCheckpoint(manager, "PostQuake");
        yield return ReadSubtitlesHumanly(ui);

        // Sarsıntı sonrası: her görev kendi sahne nesnesi ve gerçek hedefiyle yapılır.
        foreach (string id in new[]
                 {
                     "quake.post.checkcan",
                     "quake.post.glass",
                     "quake.post.shoes",
                     "quake.post.canlaces",
                     "quake.post.bag",
                     "quake.post.consequence",
                     "quake.post.familyplan",
                     "quake.post.flashlight",
                     "quake.post.exit"
                 })
            yield return PerformWorldGesture(touch, ui, id);

        yield return WaitForPhase(director, "Corridor", 18f);
        AssertCheckpoint(manager, "CorridorReached");
        yield return ReadSubtitlesHumanly(ui);

        foreach (string id in new[]
                 {
                     "quake.corridor.hand",
                     "quake.corridor.scan",
                     "quake.corridor.rubble",
                     "quake.corridor.parent",
                     "quake.corridor.aftershock"
                 })
            yield return PerformWorldGesture(touch, ui, id);

        yield return WaitForPhase(director, "Completed", 18f);
        yield return ReadSubtitlesHumanly(ui);

        Assert.That(Find("ChapterCompletionCard").activeSelf, Is.True);
        Assert.That(Find("Deniz_WornEmergencyBag").activeSelf, Is.True);
        Assert.That(Time.realtimeSinceStartup - runStartedAt, Is.GreaterThan(95f),
            "Test, yazılmış deprem öncesi ve aktif sarsıntı sürelerini gerçekten yaşamadan bitmemeli.");
    }

    [UnityTest]
    [Timeout(65000)]
    public IEnumerator QuakeRebuild_QuakeActionsUseRealWorldGesturesAndFeetStayPlantedUnderCover()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_03_RebuildPreview.unity");
        MonoBehaviour touch = FindBehaviour("StoryTouchManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");

        // Focused regression: enter the authored quake phase, then use exactly the same world
        // raycasts, approach and timed holds as a player. No director progression method is used
        // after the phase begins.
        string[] securedFurniture = { "Wardrobe_Secured", "Shelf_Secured" };
        Vector3[] securedPositions = securedFurniture.Select(name => Find(name).transform.position).ToArray();
        InvokeNonPublic(director, "BeginQuake", true);
        yield return ReadSubtitlesHumanly(ui);
        for (int index = 0; index < securedFurniture.Length; index++)
            Assert.That(Vector3.Distance(Find(securedFurniture[index]).transform.position, securedPositions[index]),
                Is.LessThan(0.25f), securedFurniture[index] + " sarsılmalı, odanın merkezine taşınmamalı.");
        yield return PerformWorldGesture(touch, ui, "quake.calm.can");
        yield return PerformWorldGesture(touch, ui, "quake.cover.crouch");
        yield return new WaitForSecondsRealtime(0.3f);

        yield return PerformWorldGesture(touch, ui, "quake.cover.head");
        yield return new WaitForSecondsRealtime(0.3f);

        AssertChildrenStaySeparatedUnderCover();
        AssertChildrenFaceEachOtherUnderCover();
        string capturePath = Path.Combine(Path.GetTempPath(), "Deprem_StoryCoverPoseUAL.png");
        if (File.Exists(capturePath))
            File.Delete(capturePath);
        Camera captureCamera = Camera.main;
        const int captureWidth = 540;
        const int captureHeight = 960;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        RenderTexture target = new RenderTexture(captureWidth, captureHeight, 24);
        captureCamera.targetTexture = target;
        RenderTexture.active = target;
        captureCamera.Render();
        Texture2D capture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        capture.ReadPixels(new Rect(0f, 0f, captureWidth, captureHeight), 0, 0);
        capture.Apply(false, false);
        File.WriteAllBytes(capturePath, capture.EncodeToPNG());
        captureCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(capture);
        Assert.That(File.Exists(capturePath), Is.True, "Protective pose QA screenshot must be captured.");

        foreach (string childName in new[] { "Deniz_12", "Can_8" })
        {
            Animator animator = Find(childName).GetComponentInChildren<Animator>(true);
            Assert.That(animator.GetCurrentAnimatorClipInfo(0).Single().clip.name,
                Is.EqualTo("ChildCoverPose"), childName + " must not loop the Crouching walk cycle.");
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Vector3 leftBefore = leftFoot.position;
            Vector3 rightBefore = rightFoot.position;
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(Vector3.Distance(leftBefore, leftFoot.position), Is.LessThan(0.012f),
                childName + " left foot must stay planted while taking cover.");
            Assert.That(Vector3.Distance(rightBefore, rightFoot.position), Is.LessThan(0.012f),
                childName + " right foot must stay planted while taking cover.");
        }

        yield return PerformWorldGesture(touch, ui, "quake.cover.grip");
        yield return WaitForPhase(director, "PostQuake", 12f);
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator QuakeRebuild_PostQuakeShoesWalkDiagnostic()
    {
        yield return LoadFreshSceneByPath("Assets/Scenes/Story_03_RebuildPreview.unity");
        MonoBehaviour touch = FindBehaviour("StoryTouchManager");
        MonoBehaviour ui = FindBehaviour("StoryUIController");
        MonoBehaviour director = FindBehaviour("StorySequenceDirector");
        InvokeNonPublic(director, "SetupPostQuake");
        yield return ReadSubtitlesHumanly(ui);
        yield return PerformWorldGesture(touch, ui, "quake.post.checkcan");
        yield return PerformWorldGesture(touch, ui, "quake.post.glass");
        yield return PerformWorldGesture(touch, ui, "quake.post.shoes");
        Assert.That(Find("Deniz_12_WornShoes").activeSelf, Is.True);
        yield return PerformWorldGesture(touch, ui, "quake.post.canlaces");
        yield return PerformWorldGesture(touch, ui, "quake.post.bag");
        yield return PerformWorldGesture(touch, ui, "quake.post.consequence");
        yield return PerformWorldGesture(touch, ui, "quake.post.familyplan");
        yield return PerformWorldGesture(touch, ui, "quake.post.flashlight");
        yield return PerformWorldGesture(touch, ui, "quake.post.exit");
    }

    private static IEnumerator PerformWorldGesture(MonoBehaviour touch, MonoBehaviour ui, string interactionId)
    {
        MonoBehaviour interaction = FindInteraction(interactionId);
        yield return WaitForInteractionAvailable(interaction, ui, 18f);
        yield return WaitForCameraReady(8f);

        Vector2 source = VisibleScreenPoint(interaction, interactionId + " kaynak");
        InvokeNonPublic(touch, "HandleWorldTap", source);

        string gesture = Property(interaction, "InteractionGesture").GetValue(interaction).ToString();
        if (gesture == "Tap" || gesture == "Approach")
        {
            yield return WaitForConsumed(interaction, interactionId, 18f);
            yield return ReadSubtitlesHumanly(ui);
            yield break;
        }

        yield return WaitForPendingPrepared(touch, interaction, interactionId, 18f);
        yield return WaitForCameraReady(8f);
        yield return null;
        yield return null;
        source = VisibleScreenPoint(interaction, interactionId + " odak kamerasındaki kaynak");

        if (gesture == "DragToTarget" || gesture == "DragToBag")
        {
            if (Field(touch, "managedDrag").GetValue(touch) == null)
                InvokeNonPublic(touch, "HandleWorldTap", source);
            object draggable = Field(touch, "managedDrag").GetValue(touch);
            Assert.That(draggable, Is.Not.Null, interactionId + " dokununca gerçek sürükleme başlamalı.");

            Transform target = (Transform)Property(interaction, "GestureTarget").GetValue(interaction);
            Assert.That(target, Is.Not.Null, interactionId + " gerçek sahne hedefi tanımlı olmalı.");
            Vector2 destination = VisibleScreenPoint(target, interactionId + " hedef");
            for (int step = 1; step <= 12; step++)
            {
                Vector2 pointer = Vector2.Lerp(source, destination, step / 12f);
                InvokePublic(draggable, "UpdateManagedDrag", pointer);
                yield return new WaitForSecondsRealtime(0.045f);
            }

            Assert.That((bool)InvokePublicResult(draggable, "EndManagedDrag", destination), Is.True,
                interactionId + " görünür hedefe bırakılınca kabul edilmeli.");
            InvokeNonPublic(touch, "CompletePendingInteraction");
        }
        else if (gesture == "WorldHold")
        {
            if (!(bool)Field(touch, "worldHoldActive").GetValue(touch))
                InvokeNonPublic(touch, "HandleWorldTap", source);
            Assert.That((bool)Field(touch, "worldHoldActive").GetValue(touch), Is.True,
                interactionId + " hedefin üstünde basılı tutmayı başlatmalı.");

            float holdDuration = (float)Property(interaction, "InteractionSeconds").GetValue(interaction);
            touch.enabled = false; // Gerçek parmak eklenemeyen Test Runner'da aynı basılı tutuşu Update iptal etmesin.
            yield return new WaitForSecondsRealtime(Mathf.Max(0.65f, holdDuration) + 0.08f);
            InvokeNonPublic(touch, "CompletePendingInteraction");
            touch.enabled = true;
        }
        else if (gesture == "RepeatedTap")
        {
            int required = (int)Property(interaction, "RequiredGestureCount").GetValue(interaction);
            for (int tap = 0; tap < required + 1 && (bool)Property(interaction, "IsAvailable").GetValue(interaction); tap++)
            {
                yield return WaitForCameraReady(8f);
                InvokeNonPublic(touch, "HandleWorldTap", VisibleScreenPoint(interaction, interactionId));
                yield return new WaitForSecondsRealtime(0.22f);
            }
        }
        else
        {
            if (!(bool)Field(touch, "directGestureActive").GetValue(touch))
                InvokeNonPublic(touch, "HandleWorldTap", source);
            Assert.That((bool)Field(touch, "directGestureActive").GetValue(touch), Is.True,
                interactionId + " nesnenin üstünde sürükleme hareketini başlatmalı.");

            Transform target = (Transform)Property(interaction, "GestureTarget").GetValue(interaction);
            Vector2 destination;
            if (target != null)
            {
                destination = VisibleScreenPoint(target, interactionId + " yön hedefi");
                float sign = Mathf.Abs(destination.x - source.x) >= 24f
                    ? Mathf.Sign(destination.x - source.x)
                    : 1f;
                if (Mathf.Abs(destination.x - source.x) < 105f)
                    destination.x = Mathf.Clamp(source.x + sign * 115f, 8f, Screen.width - 8f);
            }
            else
            {
                destination = new Vector2(Mathf.Clamp(source.x + 115f, 8f, Screen.width - 8f), source.y);
            }

            Vector2 delta = destination - source;
            Assert.That(Mathf.Abs(delta.x), Is.GreaterThanOrEqualTo(95f), interactionId);
            if (gesture == "SwipeHorizontal")
                Assert.That((bool)InvokeNonPublicResult(touch, "IsHorizontalGestureHeadingToSceneTarget", delta),
                    Is.True, interactionId + " sahnedeki hedef yönüne sürüklenmeli.");
            yield return new WaitForSecondsRealtime(0.28f);
            InvokeNonPublic(touch, "CompletePendingInteraction");
        }

        yield return WaitForConsumed(interaction, interactionId, 8f);
        yield return ReadSubtitlesHumanly(ui);
    }

    private static IEnumerator ReadSubtitlesHumanly(MonoBehaviour ui)
    {
        int pages = 0;
        int clearFrames = 0;
        while (true)
        {
            bool active = (bool)Property(ui, "SubtitleActive").GetValue(ui);
            if (!active)
            {
                if (!(bool)Property(ui, "WorldInputBlocked").GetValue(ui))
                    yield break;
                yield return null;
                clearFrames++;
                Assert.That(clearFrames, Is.LessThan(8), "Altyazı kapanınca dünya girişi açılmalı.");
                continue;
            }

            clearFrames = 0;
            float revealDeadline = Time.realtimeSinceStartup + 12f;
            while ((bool)Property(ui, "SubtitleActive").GetValue(ui) &&
                   !(bool)Property(ui, "SubtitleRevealComplete").GetValue(ui) &&
                   Time.realtimeSinceStartup < revealDeadline)
                yield return null;
            Assert.That((bool)Property(ui, "SubtitleRevealComplete").GetValue(ui), Is.True,
                "Altyazı doğal yazımını tamamlamalı.");
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That((bool)InvokePublicResult(ui, "TryHandlePrimaryTap"), Is.True,
                "Oyuncunun altyazıyı tek dokunuşla ilerletebilmesi gerekir.");
            yield return null;
            pages++;
            Assert.That(pages, Is.LessThan(24), "Diyalog zinciri sonsuza girmemeli.");
        }
    }

    private static IEnumerator WaitForInteractionAvailable(MonoBehaviour interaction, MonoBehaviour ui, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!(bool)Property(interaction, "IsAvailable").GetValue(interaction) &&
               Time.realtimeSinceStartup < deadline)
        {
            if ((bool)Property(ui, "SubtitleActive").GetValue(ui))
                yield return ReadSubtitlesHumanly(ui);
            else
                yield return null;
        }
        bool available = (bool)Property(interaction, "IsAvailable").GetValue(interaction);
        if (!available)
        {
            MonoBehaviour player = FindBehaviour("StoryPlayerMovement");
            MonoBehaviour director = FindBehaviour("StorySequenceDirector");
            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
            Transform point = (Transform)Property(interaction, "InteractionPoint").GetValue(interaction);
            Assert.Fail(
                $"{Property(interaction, "InteractionId").GetValue(interaction)} oyuncuya sırayla açılmalı. " +
                $"phase={Property(director, "CurrentPhase").GetValue(director)}, " +
                $"postIndex={Field(director, "postQuakeIndex").GetValue(director)}, " +
                $"locked={Property(player, "StoryInputLocked").GetValue(player)}, " +
                $"player={player.transform.position}, target={(point != null ? point.position : Vector3.zero)}, " +
                $"onNavMesh={agent.isOnNavMesh}, hasPath={agent.hasPath}, path={agent.pathStatus}, " +
                $"remaining={(agent.isOnNavMesh ? agent.remainingDistance : -1f)}.");
        }
    }

    private static IEnumerator WaitForPendingPrepared(
        MonoBehaviour touch,
        MonoBehaviour interaction,
        string interactionId,
        float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!(bool)Property(interaction, "IsAvailable").GetValue(interaction))
                yield break;
            if (ReferenceEquals(Field(touch, "pendingInteraction").GetValue(touch), interaction) &&
                (bool)Field(touch, "pendingPrepared").GetValue(touch))
                yield break;
            yield return null;
        }
        Assert.Fail(interactionId + " dokunulduğunda karakter yaklaşmalı ve etkileşim hazırlanmalı.");
    }

    private static IEnumerator WaitForConsumed(MonoBehaviour interaction, string id, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while ((bool)Property(interaction, "IsAvailable").GetValue(interaction) &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That((bool)Property(interaction, "IsAvailable").GetValue(interaction), Is.False,
            id + " gerçek hareket tamamlanınca tüketilmeli.");
    }

    private static IEnumerator WaitForCameraReady(float seconds)
    {
        MonoBehaviour cameras = FindBehaviour("StoryCameraController");
        float deadline = Time.realtimeSinceStartup + seconds;
        while ((bool)Property(cameras, "WorldNavigationBlocked").GetValue(cameras) &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That((bool)Property(cameras, "WorldNavigationBlocked").GetValue(cameras), Is.False,
            "Kamera geçişi oyuncu girdisini süresiz kilitlememeli.");
    }

    private static IEnumerator WaitForPhase(MonoBehaviour director, string expected, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (Property(director, "CurrentPhase").GetValue(director).ToString() != expected &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(Property(director, "CurrentPhase").GetValue(director).ToString(), Is.EqualTo(expected));
    }

    private static Vector2 VisibleScreenPoint(MonoBehaviour interaction, string label)
    {
        bool fromAnywhere = (bool)Property(interaction, "InteractFromAnywhere").GetValue(interaction);
        Transform interactionPoint = (Transform)Property(interaction, "InteractionPoint").GetValue(interaction);
        if (fromAnywhere && interactionPoint != null)
            return VisibleScreenPoint(interactionPoint.position, label);

        Collider collider = interaction.GetComponent<Collider>() ?? interaction.GetComponentInChildren<Collider>(true);
        Renderer renderer = interaction.GetComponentInChildren<Renderer>(true);
        Vector3 point = collider != null
            ? collider.bounds.center
            : renderer != null ? renderer.bounds.center : interaction.transform.position;
        return VisibleScreenPoint(point, label);
    }

    private static Vector2 VisibleScreenPoint(Transform target, string label)
    {
        Collider collider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>(true);
        Renderer renderer = target.GetComponentInChildren<Renderer>(true);
        Vector3 point = collider != null
            ? collider.bounds.center
            : renderer != null ? renderer.bounds.center : target.position;
        return VisibleScreenPoint(point, label);
    }

    private static Vector2 VisibleScreenPoint(Vector3 worldPoint, string label)
    {
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null, label);
        Vector3 screen = camera.WorldToScreenPoint(worldPoint);
        Assert.That(screen.z, Is.GreaterThan(0f), label + " kameranın arkasında kalmamalı.");
        Assert.That(screen.x, Is.InRange(5f, Screen.width - 5f),
            $"{label} yatay kadrajda görünmeli. world={worldPoint}, camera={camera.transform.position}, forward={camera.transform.forward}");
        Assert.That(screen.y, Is.InRange(5f, Screen.height - 5f), label + " dikey kadrajda görünmeli.");
        return screen;
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
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
    }

    private static MonoBehaviour FindInteraction(string id)
    {
        MonoBehaviour result = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == "StoryInteractable" &&
                            Property(item, "InteractionId").GetValue(item).ToString() == id);
        return result;
    }

    private static MonoBehaviour FindBehaviour(string typeName)
    {
        return Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item != null && item.GetType().Name == typeName);
    }

    private static GameObject Find(string name)
    {
        Transform result = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == name);
        Assert.That(result, Is.Not.Null, name);
        return result.gameObject;
    }

    private static void AssertCheckpoint(MonoBehaviour manager, string expected)
    {
        object state = Property(manager, "CurrentState").GetValue(manager);
        string checkpoint = state.GetType().GetField("checkpoint").GetValue(state).ToString();
        Assert.That(checkpoint, Is.EqualTo(expected));
    }

    private static void AssertChildrenStaySeparatedUnderCover()
    {
        Vector3 separation = Find("Deniz_12").transform.position - Find("Can_8").transform.position;
        separation.y = 0f;
        Assert.That(separation.magnitude, Is.GreaterThanOrEqualTo(0.9f),
            "Deniz ve Can'Ä±n koruma animasyonlarÄ± aynÄ± noktada iÃ§ iÃ§e girmemeli.");
    }

    private static void AssertChildrenFaceEachOtherUnderCover()
    {
        Transform deniz = Find("Deniz_12").transform;
        Transform can = Find("Can_8").transform;
        Vector3 denizToCan = can.position - deniz.position;
        denizToCan.y = 0f;
        Vector3 canToDeniz = -denizToCan;
        Assert.That(Vector3.Dot(deniz.forward, denizToCan.normalized), Is.GreaterThan(0.92f),
            "Deniz masa altında Can'a dönük olmalı.");
        Assert.That(Vector3.Dot(can.forward, canToDeniz.normalized), Is.GreaterThan(0.92f),
            "Can masa altında Deniz'e dönük olmalı.");
    }

    private static PropertyInfo Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, target.GetType().Name + "." + name);
        return property;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
        return field;
    }

    private static void InvokeNonPublic(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static object InvokeNonPublicResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }

    private static void InvokePublic(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        method.Invoke(target, arguments);
    }

    private static object InvokePublicResult(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target, arguments);
    }
#endif
}
