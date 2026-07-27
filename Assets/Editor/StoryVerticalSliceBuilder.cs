using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class StoryVerticalSliceBuilder
{
    private const string ScenePath = "Assets/Scenes/Story_03_Quake.unity";
    private const string StoryRoot = "Assets/Story";
    private const string GeneratedRoot = StoryRoot + "/Generated";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string AudioRoot = GeneratedRoot + "/Audio";
    private const string TimelineRoot = GeneratedRoot + "/Timelines";
    private const string PrefabRoot = StoryRoot + "/Prefabs";
    private const string CuteFurnitureRoot = "Assets/ithappy/Cute_Furniture_Free/Prefabs";
    private const string KenneyBedrollPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx";
    private const string KenneySurvivalMaterialPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Materials/KenneySurvival_Atlas.mat";

    private static readonly Color Navy = new Color32(15, 30, 46, 255);
    private static readonly Color Teal = new Color32(25, 151, 151, 255);
    private static readonly Color Amber = new Color32(244, 173, 65, 255);
    private static readonly Color Coral = new Color32(224, 91, 82, 255);
    private static readonly Color Cream = new Color32(236, 226, 204, 255);
    private static readonly Color Wall = new Color32(183, 197, 194, 255);
    private static readonly Color Floor = new Color32(102, 78, 63, 255);
    private static readonly Color DarkFloor = new Color32(55, 61, 66, 255);

    private sealed class Materials
    {
        public Material wall;
        public Material floor;
        public Material corridor;
        public Material cream;
        public Material navy;
        public Material teal;
        public Material amber;
        public Material coral;
        public Material wood;
        public Material glass;
        public Material glassShard;
        public Material glow;
        public Material safeGlow;
        public Material helpGlow;
        public Material hazardGlow;
        public Material dust;
    }

    private sealed class WorldReferences
    {
        public GameObject environment;
        public GameObject wardrobeSecured;
        public GameObject wardrobeUnsecured;
        public GameObject wardrobeFallen;
        public GameObject safeTable;
        public GameObject shelfStable;
        public GameObject shelfUnsecured;
        public GameObject shelfFallen;
        public GameObject hangingLamp;
        public GameObject looseProps;
        public GameObject clearExit;
        public GameObject clutteredExit;
        public GameObject closedDoor;
        public GameObject openDoor;
        public GameObject leftShoeWorld;
        public GameObject rightShoeWorld;
        public GameObject canShoesWorld;
        public GameObject emergencyBagWorld;
        public GameObject brokenGlassVisual;
        public GameObject denizWornShoes;
        public GameObject denizWornBag;
        public GameObject canWornShoes;
        public Transform tableFocus;
        public Transform windowFocus;
        public Transform wardrobeFocus;
        public Transform exitInspectFocus;
        public Transform glassFocus;
        public Transform leftShoeFocus;
        public Transform rightShoeFocus;
        public Transform shoesFocus;
        public Transform canShoesFocus;
        public Transform bagFocus;
        public Transform bagStrapFocus;
        public Transform flashlightFocus;
        public Transform emergencyLightFocus;
        public Transform corridorFocus;
        public Transform[] corridorStepFoci;
    }

    private sealed class CharacterStoryProps
    {
        public GameObject wornShoes;
        public GameObject wornBag;
    }

    private sealed class BeatDefinition
    {
        public StoryInteractable interactable;
        public string title;
        public string detail;
        public string subtitle;
        public float delayAfter;
    }

    private sealed class InteractionReferences
    {
        public StoryInteractable[] intro;
        public StoryInteractable[] introOptional;
        public StoryInteractable calmSibling;
        public StoryInteractable crouchStep;
        public StoryInteractable coverHeadStep;
        public StoryInteractable safeCover;
        public StoryInteractable unsafeDoor;
        public StoryInteractable unsafeWindow;
        public BeatDefinition[] postQuakeBeats;
        public StoryInteractable lightWithFlashlight;
        public StoryInteractable lightWithoutFlashlight;
        public StoryInteractable exit;
        public StoryInteractable brokenGlassHazard;
        public BeatDefinition[] corridorBeats;
    }

    [MenuItem("Tools/Deprem Story/Build Story_03_Quake Vertical Slice")]
    public static void BuildFromMenu()
    {
        BuildVerticalSlice(true);
    }

    [MenuItem("Tools/Deprem Story/Build Story_03_Quake Vertical Slice (Silent)")]
    public static void BuildSilentFromMenu()
    {
        BuildVerticalSlice(false);
    }

    public static void BuildFromCommandLine()
    {
        BuildVerticalSlice(false);
    }

    private static void BuildVerticalSlice(bool showDialog)
    {
        try
        {
            EnsureFolders();
            StoryAnimationLibraryBuilder.BuildLibrary(false);
            Materials materials = CreateMaterials();
            CreateAudioAssets();
            VolumeProfile volumeProfile = CreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("STORY_03_QUAKE");

            WorldReferences world = BuildWorld(root.transform, materials);
            GameObject sessionRoot = new GameObject("_StorySession");
            StoryGameManager gameManager = sessionRoot.AddComponent<StoryGameManager>();
            GameObject core = new GameObject("_StoryCore");
            core.transform.SetParent(root.transform);
            StorySequenceDirector sequence = core.AddComponent<StorySequenceDirector>();

            GameObject deniz = BuildCharacter("Deniz_12", new Vector3(-0.85f, 0f, -0.8f), 1.56f,
                root.transform, materials, out CharacterStoryProps denizProps);
            Sample.KidsScript legacyMovement = deniz.GetComponent<Sample.KidsScript>();
            if (legacyMovement != null)
                Object.DestroyImmediate(legacyMovement);
            CharacterController[] legacyControllers = deniz.GetComponentsInChildren<CharacterController>(true);
            foreach (CharacterController legacyController in legacyControllers)
                Object.DestroyImmediate(legacyController);
            NavMeshAgent agent = deniz.AddComponent<NavMeshAgent>();
            agent.speed = 1.75f;
            agent.acceleration = 9f;
            agent.angularSpeed = 540f;
            agent.radius = 0.24f;
            agent.height = 1.48f;
            agent.baseOffset = 0f;
            agent.stoppingDistance = 0.15f;
            CapsuleCollider playerCollider = deniz.AddComponent<CapsuleCollider>();
            playerCollider.radius = 0.24f;
            playerCollider.height = 1.46f;
            playerCollider.center = new Vector3(0f, 0.73f, 0f);
            Rigidbody playerBody = deniz.AddComponent<Rigidbody>();
            playerBody.isKinematic = true;
            playerBody.useGravity = false;
            StoryPlayerMovement movement = deniz.AddComponent<StoryPlayerMovement>();
            SetReference(movement, "animator", deniz.GetComponentInChildren<Animator>());

            GameObject can = BuildCharacter("Can_8", new Vector3(0.25f, 0f, -0.45f), 1.23f,
                root.transform, materials, out CharacterStoryProps canProps);
            Sample.KidsScript canLegacyMovement = can.GetComponent<Sample.KidsScript>();
            if (canLegacyMovement != null)
                Object.DestroyImmediate(canLegacyMovement);
            foreach (CharacterController legacyController in can.GetComponentsInChildren<CharacterController>(true))
                Object.DestroyImmediate(legacyController);
            NavMeshAgent canAgent = can.AddComponent<NavMeshAgent>();
            canAgent.speed = 1.65f;
            canAgent.acceleration = 8f;
            canAgent.angularSpeed = 500f;
            canAgent.radius = 0.22f;
            canAgent.height = 1.24f;
            canAgent.baseOffset = 0f;
            canAgent.stoppingDistance = 1.05f;
            EnsureCapsule(can, 0.22f, 1.24f);
            Rigidbody canBody = can.AddComponent<Rigidbody>();
            canBody.isKinematic = true;
            canBody.useGravity = false;
            StorySiblingFollower siblingFollower = can.AddComponent<StorySiblingFollower>();
            SetReference(siblingFollower, "target", deniz.transform);
            SetReference(siblingFollower, "animator", can.GetComponentInChildren<Animator>());

            deniz.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            can.transform.rotation = Quaternion.Euler(0f, -32f, 0f);
            world.denizWornShoes = denizProps.wornShoes;
            world.denizWornBag = denizProps.wornBag;
            world.canWornShoes = canProps.wornShoes;

            StoryCameraController cameraController = BuildCameras(root.transform, deniz.transform, out Camera mainCamera, out CinemachineBrain brain);
            StoryUIController ui = BuildUI(root.transform, cameraController);
            SetReference(ui, "movementOwner", movement);

            StoryTouchManager touch = core.AddComponent<StoryTouchManager>();
            SetReference(touch, "worldCamera", mainCamera);
            SetReference(touch, "player", movement);
            SetReference(touch, "ui", ui);
            SetReference(touch, "cameraController", cameraController);
            SerializedObject touchSettings = new SerializedObject(touch);
            touchSettings.FindProperty("directWorldGestures").boolValue = true;
            touchSettings.ApplyModifiedPropertiesWithoutUndo();

            InteractionReferences interactions = BuildInteractions(root.transform, world, can.transform, materials, sequence);
            PlayableDirector quakeTimeline = BuildEarthquakeTimeline(root.transform, materials, world, out CinemachineImpulseSource impulse, out AudioSource impactSource);
            BuildLighting(root.transform, volumeProfile, out Light roomLight);
            BuildAmbientAudio(root.transform);
            GameObject playerFlashlight = BuildPlayerFlashlight(deniz.transform);
            GameObject emergencyLights = BuildEmergencyRouteLights(root.transform, materials);

            ConfigureSequence(sequence, gameManager, movement, siblingFollower, touch, cameraController, ui,
                deniz.GetComponentInChildren<Animator>(), can.GetComponentInChildren<Animator>(), quakeTimeline, impulse,
                impactSource, roomLight, interactions, world, playerFlashlight, emergencyLights);

            SetReference(ui, "cameraController", cameraController);
            SetReference(cameraController, "brain", brain);

            EditorSceneManager.SaveScene(scene, ScenePath);
            BuildNavigation(world.environment);
            SaveReusablePrefabs(world, deniz, can);
            foreach (CharacterController legacyController in deniz.GetComponentsInChildren<CharacterController>(true))
                Object.DestroyImmediate(legacyController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsWithoutChangingStartupScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            StoryVerticalSliceValidator.Validate(false);

            Selection.activeGameObject = GameObject.Find("STORY_03_QUAKE");
            Debug.Log("Story_03_Quake built successfully: " + ScenePath);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Story_03_Quake dikey dilimi oluşturuldu ve doğrulamaya hazır.", "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Deprem Story", "Sahne kurulamadı:\n" + exception.Message, "Kapat");
            throw;
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Story");
        EnsureFolder(StoryRoot, "Generated");
        EnsureFolder(GeneratedRoot, "Materials");
        EnsureFolder(GeneratedRoot, "Audio");
        EnsureFolder(GeneratedRoot, "Timelines");
        EnsureFolder(StoryRoot, "Prefabs");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Materials CreateMaterials()
    {
        return new Materials
        {
            wall = GetOrCreateMaterial("Wall", Wall, 0.15f),
            floor = GetOrCreateMaterial("WarmFloor", Floor, 0.32f),
            corridor = GetOrCreateMaterial("CorridorFloor", DarkFloor, 0.2f),
            cream = GetOrCreateMaterial("Cream", Cream, 0.24f),
            navy = GetOrCreateMaterial("Navy", Navy, 0.22f),
            teal = GetOrCreateMaterial("Teal", Teal, 0.28f),
            amber = GetOrCreateMaterial("Amber", Amber, 0.25f),
            coral = GetOrCreateMaterial("Coral", Coral, 0.25f),
            wood = GetOrCreateMaterial("Wood", new Color32(123, 83, 61, 255), 0.3f),
            glass = GetOrCreateMaterial("WindowGlass", new Color32(63, 103, 125, 255), 0.75f, true, new Color(0.02f, 0.09f, 0.15f)),
            glassShard = GetOrCreateMaterial("BrokenGlass", new Color32(104, 199, 218, 255), 0.82f, true, new Color(0.18f, 0.72f, 0.95f)),
            glow = GetOrCreateMaterial("InteractionGlow", Amber, 0.15f, true, Amber * 2.2f),
            safeGlow = GetOrCreateMaterial("InteractionSafeGlow", Teal, 0.12f, true, Teal * 2.3f),
            helpGlow = GetOrCreateMaterial("InteractionHelpGlow", Amber, 0.12f, true, Amber * 2.3f),
            hazardGlow = GetOrCreateMaterial("InteractionHazardGlow", Coral, 0.12f, true, Coral * 2.3f),
            dust = GetOrCreateParticleMaterial()
        };
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool emissive = false, Color emission = default)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateParticleMaterial()
    {
        string path = MaterialRoot + "/DustParticles.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (material == null)
        {
            material = new Material(shader) { name = "DustParticles" };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }
        material.SetColor("_BaseColor", new Color(0.78f, 0.71f, 0.6f, 0.42f));
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", GetOrCreateSoftDustTexture());
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D GetOrCreateSoftDustTexture()
    {
        const int size = 64;
        string path = MaterialRoot + "/DustCloudTexture.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "DustCloudTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / size * 2f - 1f,
                    (y + 0.5f) / size * 2f - 1f);
                float radial = Mathf.Clamp01(1f - point.magnitude);
                radial = radial * radial * (3f - 2f * radial);
                float noise = Mathf.PerlinNoise(x * 0.115f + 3.7f, y * 0.115f + 8.1f);
                float alpha = Mathf.Pow(radial, 1.45f) * Mathf.Lerp(0.58f, 1f, noise);
                pixels[y * size + x] = new Color(1f, 0.96f, 0.88f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static VolumeProfile CreateVolumeProfile()
    {
        string path = GeneratedRoot + "/Story_Quake_Volume.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.1f);

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(-0.08f);
        color.contrast.Override(8f);
        color.saturation.Override(-5f);
        color.colorFilter.Override(new Color(1f, 0.97f, 0.9f));

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.42f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static WorldReferences BuildWorld(Transform parent, Materials m)
    {
        WorldReferences world = new WorldReferences();
        GameObject environment = new GameObject("Environment_StoryHome");
        environment.transform.SetParent(parent);
        world.environment = environment;

        Transform room = NewChild(environment.transform, "Room");
        CreatePrimitive("LivingRoom_Floor", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0.25f), new Vector3(10f, 0.24f, 11.5f), m.floor, room);
        // Keep a genuinely navigable 2.2 m doorway. The previous 1.4 m opening was closed by
        // the default Humanoid NavMesh agent radius during bake, so hiding the door still left
        // the living room and corridor as disconnected islands.
        CreatePrimitive("BackWall_Left", PrimitiveType.Cube, new Vector3(-1.8f, 1.7f, 6f), new Vector3(6.4f, 3.4f, 0.22f), m.wall, room);
        CreatePrimitive("BackWall_Right", PrimitiveType.Cube, new Vector3(4.3f, 1.7f, 6f), new Vector3(1.4f, 3.4f, 0.22f), m.wall, room);
        CreatePrimitive("Door_Lintel", PrimitiveType.Cube, new Vector3(2.5f, 3.02f, 6f), new Vector3(2.2f, 0.76f, 0.22f), m.wall, room);
        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-5f, 1.7f, 0.25f), new Vector3(0.22f, 3.4f, 11.5f), m.wall, room);
        CreatePrimitive("LowRightWall", PrimitiveType.Cube, new Vector3(5f, 0.5f, 1.7f), new Vector3(0.22f, 1f, 8.5f), m.wall, room);
        CreatePrimitive("RugBorder", PrimitiveType.Cube, new Vector3(0.15f, 0.025f, 0f), new Vector3(4.9f, 0.05f, 3.7f), m.navy, room, false);
        CreatePrimitive("Rug", PrimitiveType.Cube, new Vector3(0.15f, 0.045f, 0f), new Vector3(4.55f, 0.05f, 3.35f), m.teal, room, false);
        CreatePrimitive("RugInset", PrimitiveType.Cube, new Vector3(0.15f, 0.073f, 0f), new Vector3(3.7f, 0.012f, 2.55f), m.cream, room, false);
        CreatePrimitive("Skirting_Back", PrimitiveType.Cube, new Vector3(-1.825f, 0.18f, 5.82f), new Vector3(6.35f, 0.18f, 0.12f), m.cream, room, false);
        CreatePrimitive("Skirting_Left", PrimitiveType.Cube, new Vector3(-4.82f, 0.18f, 0.1f), new Vector3(0.12f, 0.18f, 11f), m.cream, room, false);
        CreatePrimitive("Skirting_Right", PrimitiveType.Cube, new Vector3(4.82f, 0.18f, 1.7f), new Vector3(0.12f, 0.18f, 8.45f), m.cream, room, false);
        CreatePrimitive("WindowAccentWall", PrimitiveType.Cube, new Vector3(-2.18f, 1.7f, 5.855f), new Vector3(3.35f, 3.12f, 0.045f), m.cream, room, false);

        Transform corridor = NewChild(environment.transform, "Corridor");
        CreatePrimitive("CorridorFloor", PrimitiveType.Cube, new Vector3(2.5f, -0.11f, 9.4f), new Vector3(3f, 0.22f, 6.8f), m.corridor, corridor);
        // Overlap both floor colliders through the wall thickness. Two coplanar cubes that only
        // touched at z=6 baked as separate NavMesh islands on Unity 6 even with a wide opening.
        GameObject doorwayFloorBridge = CreatePrimitive(
            "DoorwayFloorBridge",
            PrimitiveType.Cube,
            new Vector3(2.5f, -0.105f, 6f),
            new Vector3(2.2f, 0.21f, 1.4f),
            m.corridor,
            corridor);
        // This cube only joins the room and corridor navigation surfaces. Its collider must
        // remain authored, but rendering it causes a visible slab across the doorway.
        doorwayFloorBridge.GetComponent<MeshRenderer>().enabled = false;
        CreatePrimitive("CorridorLeftWall", PrimitiveType.Cube, new Vector3(0.98f, 1.7f, 9.4f), new Vector3(0.18f, 3.4f, 6.8f), m.wall, corridor);
        CreatePrimitive("CorridorRightWall", PrimitiveType.Cube, new Vector3(4.02f, 1.7f, 9.4f), new Vector3(0.18f, 3.4f, 6.8f), m.wall, corridor);
        CreatePrimitive("CorridorEnd", PrimitiveType.Cube, new Vector3(2.5f, 1.7f, 12.8f), new Vector3(3f, 3.4f, 0.18f), m.navy, corridor);
        for (int i = 0; i < 4; i++)
            CreatePrimitive("CeilingBeam_" + i, PrimitiveType.Cube, new Vector3(2.5f, 3.12f, 6.8f + i * 1.65f), new Vector3(3f, 0.12f, 0.16f), m.navy, corridor, false);

        BuildWindow(room, m, out world.windowFocus);
        world.safeTable = BuildSafeTable(room, m, out world.tableFocus);
        BuildBedAndProps(room, m, out world.hangingLamp, out world.looseProps);

        world.wardrobeSecured = BuildWardrobe(room, "Wardrobe_Secured", new Vector3(-4.2f, 0f, 5.38f), false, true, m);
        world.wardrobeUnsecured = BuildWardrobe(room, "Wardrobe_Unsecured", new Vector3(-4.2f, 0f, 5.38f), false, false, m);
        world.wardrobeFallen = BuildWardrobe(room, "Wardrobe_Fallen", new Vector3(-3.86f, 0.38f, 4.86f), true, false, m);
        world.wardrobeUnsecured.SetActive(false);
        world.wardrobeFallen.SetActive(false);
        world.wardrobeFocus = CreateFocus("WardrobeFocus", new Vector3(-4.18f, 0.1f, 4.38f), room);

        world.shelfStable = BuildShelf(room, "Shelf_Secured", new Vector3(4.58f, 0f, 1.55f), false, true, m);
        world.shelfUnsecured = BuildShelf(room, "Shelf_Unsecured", new Vector3(4.58f, 0f, 1.55f), false, false, m);
        world.shelfFallen = BuildShelf(room, "Shelf_Fallen", new Vector3(4.18f, 0.2f, 1.48f), true, false, m);
        world.shelfUnsecured.SetActive(false);
        world.shelfFallen.SetActive(false);

        BuildPostQuakeProps(room, m, world);
        BuildDoor(room, m, out world.closedDoor, out world.openDoor);
        world.exitInspectFocus = CreateFocus("ExitInspectFocus", new Vector3(2.5f, 0.1f, 4.9f), room);
        world.corridorFocus = CreateFocus("CorridorFocus", new Vector3(2.5f, 0.1f, 6.35f), corridor);
        world.corridorStepFoci = new[]
        {
            CreateFocus("CorridorThresholdFocus", new Vector3(2.5f, 0.1f, 6.75f), corridor),
            CreateFocus("CorridorLightFocus", new Vector3(1.45f, 0.1f, 8.15f), corridor),
            CreateFocus("CorridorVoiceFocus", new Vector3(3.45f, 0.1f, 10.05f), corridor),
            CreateFocus("CorridorAftershockFocus", new Vector3(2.5f, 0.1f, 11.85f), corridor)
        };
        world.emergencyLightFocus = CreateFocus("EmergencyLightPreviewFocus", new Vector3(3.15f, 0.1f, 5.05f), room);

        world.clearExit = new GameObject("ExitRoute_Cleared");
        world.clearExit.transform.SetParent(environment.transform);
        for (int i = 0; i < 7; i++)
            CreatePrimitive("RouteMarker_" + i, PrimitiveType.Cube, new Vector3(2.5f, 0.015f, 6.35f + i * 0.82f), new Vector3(0.42f, 0.03f, 0.18f), m.teal, world.clearExit.transform, false);

        world.clutteredExit = new GameObject("ExitRoute_ClutteredButPassable");
        world.clutteredExit.transform.SetParent(environment.transform);
        StoryAuthoredPropFactory.CreateParcel("FallenBox", world.clutteredExit.transform,
            new Vector3(3.2f, 0f, 6.55f), new Vector3(0.8f, 0.55f, 0.75f),
            new Vector3(0f, 24f, 8f), m.amber, m.cream, m.coral);
        NavMeshObstacle clutterObstacle = world.clutteredExit.AddComponent<NavMeshObstacle>();
        clutterObstacle.shape = NavMeshObstacleShape.Box;
        clutterObstacle.center = new Vector3(3.2f, 0.3f, 6.55f);
        clutterObstacle.size = new Vector3(0.85f, 0.65f, 0.8f);
        clutterObstacle.carving = true;
        world.clutteredExit.SetActive(false);

        return world;
    }

    private static void BuildWindow(Transform parent, Materials m, out Transform focus)
    {
        GameObject window = new GameObject("Window_DangerZone");
        window.transform.SetParent(parent);
        CreatePrimitive("NightSky", PrimitiveType.Cube, new Vector3(-2.2f, 1.85f, 5.9f), new Vector3(2.42f, 1.52f, 0.05f), m.navy, window.transform, false);
        CreatePrimitive("Glass", PrimitiveType.Cube, new Vector3(-2.2f, 1.85f, 5.83f), new Vector3(2.35f, 1.45f, 0.06f), m.glass, window.transform);
        CreatePrimitive("FrameTop", PrimitiveType.Cube, new Vector3(-2.2f, 2.64f, 5.74f), new Vector3(2.7f, 0.14f, 0.18f), m.navy, window.transform);
        CreatePrimitive("FrameBottom", PrimitiveType.Cube, new Vector3(-2.2f, 1.06f, 5.74f), new Vector3(2.7f, 0.14f, 0.18f), m.navy, window.transform);
        CreatePrimitive("WindowSill", PrimitiveType.Cube, new Vector3(-2.2f, 0.98f, 5.59f), new Vector3(2.9f, 0.12f, 0.42f), m.wood, window.transform);
        CreatePrimitive("FrameMiddle", PrimitiveType.Cube, new Vector3(-2.2f, 1.85f, 5.72f), new Vector3(0.1f, 1.58f, 0.18f), m.navy, window.transform);
        CreatePrimitive("CurtainRod", PrimitiveType.Cylinder, new Vector3(-2.2f, 2.83f, 5.58f), new Vector3(0.065f, 1.65f, 0.065f), m.wood, window.transform, false).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject curtainLeft = CreatePrimitive("CurtainLeft", PrimitiveType.Cube, new Vector3(-3.58f, 1.78f, 5.52f), new Vector3(0.38f, 2.35f, 0.16f), m.coral, window.transform, false);
        GameObject curtainRight = CreatePrimitive("CurtainRight", PrimitiveType.Cube, new Vector3(-0.82f, 1.78f, 5.52f), new Vector3(0.38f, 2.35f, 0.16f), m.coral, window.transform, false);
        curtainLeft.transform.rotation = Quaternion.Euler(0f, 0f, -3f);
        curtainRight.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
        focus = CreateFocus("WindowFocus", new Vector3(-2.2f, 0.1f, 4.75f), window.transform);
    }

    private static GameObject BuildSafeTable(Transform parent, Materials m, out Transform focus)
    {
        GameObject table = new GameObject("SafeTable");
        table.transform.SetParent(parent);
        table.transform.position = new Vector3(0.45f, 0f, 0.65f);
        InstantiateFurniturePrefab("Furniture/Kitchen_Table_09.prefab", "SafeTable_Visual", table.transform,
            Vector3.zero, Vector3.zero, new Vector3(2.7f, 1.05f, 1.55f));
        CreateInvisibleColliderPrimitive("Top", new Vector3(0f, 1.0f, 0f), new Vector3(2.65f, 0.16f, 1.5f), table.transform, true);
        Vector3[] legs =
        {
            new Vector3(-1.08f, 0.5f, -0.56f), new Vector3(1.08f, 0.5f, -0.56f),
            new Vector3(-1.08f, 0.5f, 0.56f), new Vector3(1.08f, 0.5f, 0.56f)
        };
        foreach (Vector3 position in legs)
            CreateInvisibleColliderPrimitive("Leg", position, new Vector3(0.2f, 1f, 0.2f), table.transform, true);
        StoryChapterBuilderCommon.InstantiateAsset(
            CuteFurnitureRoot + "/Decorations/GameConsole_01.prefab", "FamilyBoardGame", table.transform,
            new Vector3(-0.08f, 1.08f, 0f), new Vector3(0.88f, 0.22f, 0.64f),
            new Vector3(0f, -12f, 0f), false);
        focus = CreateFocus("TableFocus", table.transform.position + new Vector3(-0.25f, 0.1f, -1.22f), table.transform);
        return table;
    }

    private static void BuildBedAndProps(Transform parent, Materials m, out GameObject hangingLamp, out GameObject looseProps)
    {
        GameObject sofa = new GameObject("FamilySofa");
        sofa.transform.SetParent(parent);
        InstantiateFurniturePrefab("Furniture/Couch_11.prefab", "FamilySofa_Visual", sofa.transform,
            new Vector3(-2.78f, 0f, -3.72f), Vector3.zero, new Vector3(3.5f, 1.25f, 1.42f));
        CreateInvisibleColliderPrimitive("FamilySofa_Collider", new Vector3(-2.78f, 0.58f, -3.72f),
            new Vector3(3.5f, 1.16f, 1.42f), sofa.transform);
        StoryChapterBuilderCommon.InstantiateAsset(
            KenneyBedrollPath,
            "KoltukBattaniyesi",
            sofa.transform,
            new Vector3(-3.35f, 0.57f, -3.6f),
            new Vector3(0.76f, 0.24f, 0.3f),
            new Vector3(0f, -8f, -5f),
            false,
            false,
            AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath));

        GameObject lowCabinet = new GameObject("LowCabinet");
        lowCabinet.transform.SetParent(parent);
        InstantiateFurniturePrefab("Furniture/Nightstand_02.prefab", "LowCabinet_Visual", lowCabinet.transform,
            new Vector3(3.72f, 0f, -3.92f), Vector3.zero, new Vector3(1.65f, 0.92f, 0.72f));
        CreateInvisibleColliderPrimitive("LowCabinet_Collider", new Vector3(3.72f, 0.45f, -3.92f),
            new Vector3(1.65f, 0.9f, 0.72f), lowCabinet.transform);
        looseProps = new GameObject("LooseProps_QuakeMotion");
        looseProps.transform.SetParent(parent);
        StoryChapterBuilderCommon.InstantiateAsset("Assets/Bolum1Prefab/Radio.prefab", "RadioBody",
            looseProps.transform, new Vector3(3.68f, 0.92f, -3.9f), new Vector3(0.7f, 0.52f, 0.38f),
            new Vector3(0f, 180f, 0f), false, true);
        StoryChapterBuilderCommon.InstantiateAsset(
            CuteFurnitureRoot + "/Decorations/Picture_17.prefab", "FamilyPhoto", looseProps.transform,
            new Vector3(3.04f, 0.92f, -3.91f), new Vector3(0.52f, 0.62f, 0.14f),
            new Vector3(-8f, 180f, 0f), false);
        StoryAuthoredPropFactory.CreateCeramicMug("LooseCup", looseProps.transform,
            new Vector3(4.18f, 0.92f, -3.86f), new Vector3(0.38f, 0.42f, 0.38f),
            new Vector3(0f, -18f, 0f), m.amber, m.navy, false);

        GameObject plant = new GameObject("RoomPlant");
        plant.transform.SetParent(parent);
        InstantiateFurniturePrefab("Plants/Plants_19.prefab", "RoomPlant_Visual", plant.transform,
            new Vector3(-4.15f, 0f, -1.25f), new Vector3(0f, 18f, 0f), new Vector3(0.92f, 1.55f, 0.92f));
        CreateInvisibleColliderPrimitive("RoomPlant_Collider", new Vector3(-4.15f, 0.52f, -1.25f),
            new Vector3(0.62f, 1.04f, 0.62f), plant.transform);

        InstantiateFurniturePrefab("Decorations/Toy_02.prefab", "Can_ToyCar", parent,
            new Vector3(-1.15f, 0.08f, -2.7f), new Vector3(0f, -22f, 0f), new Vector3(0.55f, 0.32f, 0.38f));
        BuildRoomWallDecor(parent, m);

        hangingLamp = new GameObject("HangingLamp_QuakeMotion");
        hangingLamp.transform.SetParent(parent);
        hangingLamp.transform.position = new Vector3(0.35f, 3.0f, 0.45f);
        StoryChapterBuilderCommon.InstantiateAsset(
            CuteFurnitureRoot + "/Decorations/Light_05.prefab", "HangingLamp_Visual", hangingLamp.transform,
            new Vector3(0.35f, 2.25f, 0.45f), new Vector3(0.92f, 0.78f, 0.92f),
            new Vector3(0f, 180f, 0f), false);
        Light lampLight = hangingLamp.AddComponent<Light>();
        lampLight.type = LightType.Point;
        lampLight.color = new Color(1f, 0.82f, 0.58f);
        lampLight.intensity = 1.5f;
        lampLight.range = 4.5f;
    }

    private static GameObject BuildWardrobe(Transform parent, string name, Vector3 position, bool fallen, bool secured, Materials m)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        InstantiateFurniturePrefab("Furniture/Closet_02.prefab", "Wardrobe_Visual", root.transform,
            Vector3.zero, new Vector3(0f, 180f, 0f), new Vector3(1.28f, 2.7f, 0.7f));
        CreateInvisibleColliderPrimitive("BodyCollider", new Vector3(0f, 1.35f, 0f),
            new Vector3(1.28f, 2.7f, 0.7f), root.transform, true);
        if (secured)
        {
            StoryAuthoredPropFactory.CreateMetalBracket("WallBracket", root.transform,
                root.transform.position + new Vector3(0f, 2.42f, 0.34f), new Vector3(0.84f, 0.3f, 0.28f),
                new Vector3(0f, 180f, 0f), m.navy, m.amber, false);
            CreateSafetyStrap(root.transform, -0.42f, m);
            CreateSafetyStrap(root.transform, 0.42f, m);
        }
        root.transform.rotation = fallen ? Quaternion.Euler(0f, 0f, 78f) : Quaternion.identity;
        if (fallen)
        {
            NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 1.25f, 0f);
            obstacle.size = new Vector3(1.35f, 2.7f, 0.8f);
            obstacle.carving = true;
        }
        return root;
    }

    private static GameObject BuildShelf(Transform parent, string name, Vector3 position, bool fallen, bool secured, Materials m)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Bookcase.fbx", "Bookcase_Visual",
            root.transform, position, new Vector3(1.38f, 2.28f, 0.66f), Vector3.zero, false, true);
        for (int i = 0; i < 3; i++)
        {
            float y = 0.35f + i * 0.72f;
            for (int bookIndex = 0; bookIndex < 3; bookIndex++)
            {
                StoryChapterBuilderCommon.InstantiateAsset(
                    CuteFurnitureRoot + ((i + bookIndex) % 2 == 0
                        ? "/Decorations/Book_03.prefab"
                        : "/Decorations/Book_08.prefab"),
                    $"Book_{i}_{bookIndex}", root.transform,
                    position + new Vector3(-0.34f + bookIndex * 0.23f, y + 0.04f, -0.25f),
                    new Vector3(0.18f, 0.38f + 0.04f * ((i + bookIndex) % 2), 0.24f),
                    new Vector3(0f, 0f, (bookIndex - 1) * 3f), false);
            }
        }
        if (secured)
        {
            StoryAuthoredPropFactory.CreateMetalBracket("ShelfAnchorTop", root.transform,
                position + new Vector3(0f, 1.92f, 0.24f), new Vector3(0.68f, 0.3f, 0.28f),
                new Vector3(0f, 180f, 0f), m.navy, m.amber, false);
        }
        root.transform.rotation = fallen ? Quaternion.Euler(0f, 90f, 68f) : Quaternion.Euler(0f, 90f, 0f);
        if (fallen)
        {
            NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 1f, 0f);
            obstacle.size = new Vector3(1.35f, 2.2f, 0.75f);
            obstacle.carving = true;
        }
        return root;
    }

    private static void BuildPostQuakeProps(Transform parent, Materials m, WorldReferences world)
    {
        GameObject shoesRoot = new GameObject("Shoes_PostQuake");
        shoesRoot.transform.SetParent(parent);
        world.leftShoeWorld = StoryAuthoredPropFactory.CreateSingleShoe("DenizLeftShoe_World",
            shoesRoot.transform, new Vector3(-2.92f, 0.02f, -2.22f), new Vector3(0.34f, 0.22f, 0.55f),
            new Vector3(0f, -12f, 0f), m.coral, m.cream);
        world.rightShoeWorld = StoryAuthoredPropFactory.CreateSingleShoe("DenizRightShoe_World",
            shoesRoot.transform, new Vector3(-2.36f, 0.02f, -2.02f), new Vector3(0.34f, 0.22f, 0.55f),
            new Vector3(0f, 8f, 0f), m.coral, m.cream);
        world.canShoesWorld = StoryAuthoredPropFactory.CreateShoePair("CanShoes_World", shoesRoot.transform,
            new Vector3(-1.52f, 0.02f, -1.31f), new Vector3(0.62f, 0.2f, 0.52f),
            new Vector3(0f, 4f, 0f), m.amber, m.navy);
        world.leftShoeFocus = CreateFocus("LeftShoeFocus", new Vector3(-3.12f, 0.1f, -1.82f), shoesRoot.transform);
        world.rightShoeFocus = CreateFocus("RightShoeFocus", new Vector3(-2.13f, 0.1f, -1.66f), shoesRoot.transform);
        world.shoesFocus = CreateFocus("ShoesFocus", new Vector3(-2.54f, 0.1f, -1.48f), shoesRoot.transform);
        world.canShoesFocus = CreateFocus("CanShoesFocus", new Vector3(-1.48f, 0.1f, -0.92f), shoesRoot.transform);

        Vector3 bagPosition = new Vector3(3.72f, 0f, -2.48f);
        GameObject bagRoot = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Story/Prefabs/Preparation/EmergencyBag_Open.prefab", "EmergencyBag", parent,
            bagPosition, new Vector3(0.9f, 0.95f, 0.65f), new Vector3(0f, 168f, 0f), true, true);
        world.emergencyBagWorld = bagRoot;
        world.bagFocus = CreateFocus("BagFocus", new Vector3(3.15f, 0.1f, -2.18f), bagRoot.transform);
        world.bagStrapFocus = CreateFocus("BagStrapFocus", new Vector3(3.62f, 0.1f, -1.72f), bagRoot.transform);
        world.flashlightFocus = CreateFocus("FlashlightFocus", new Vector3(4.12f, 0.1f, -1.98f), bagRoot.transform);

        GameObject shards = StoryAuthoredPropFactory.CreateGlassShards("BrokenGlass_Hazard", parent,
            new Vector3(-2.91f, 0.02f, 4.83f), new Vector3(1.12f, 0.1f, 0.9f), m.glassShard, 9);
        world.brokenGlassVisual = shards;
        world.glassFocus = CreateFocus("GlassHazardFocus", new Vector3(-2.45f, 0.1f, 4.05f), shards.transform);
        shards.SetActive(false);
    }

    private static void BuildDoor(Transform parent, Materials m, out GameObject closedDoor, out GameObject openDoor)
    {
        closedDoor = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Door2.fbx", "Door_Closed", parent,
            new Vector3(2.5f, 0f, 5.9f), new Vector3(1.9f, 2.5f, 0.22f),
            Vector3.zero, false, true);
        StoryChapterBuilderCommon.ConfigureDynamicNavigationBlocker(closedDoor);
        openDoor = StoryChapterBuilderCommon.InstantiateAsset(
            "Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Door2.fbx", "Door_Open", parent,
            new Vector3(3.48f, 0f, 6.78f), new Vector3(0.22f, 2.5f, 1.9f),
            new Vector3(0f, 90f, 0f), false, false);
        openDoor.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        openDoor.SetActive(false);
    }

    private static GameObject BuildCharacter(string name, Vector3 feetPosition, float height, Transform parent, Materials m,
        out CharacterStoryProps storyProps)
    {
        const string prefabPath = "Assets/KidsCharacterFree/Prefabs/Boy0_Humanoid.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance;
        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
            instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        instance.name = name;
        instance.transform.SetParent(parent);
        FitCharacterToHeight(instance, feetPosition, height);
        storyProps = BuildCharacterStoryProps(instance, name, height, m);

        Animator animator = instance.GetComponentInChildren<Animator>();
        RuntimeAnimatorController storyController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StoryAnimationLibraryBuilder.ControllerPath);
        if (animator == null || storyController == null)
            throw new InvalidOperationException("Story karakter Animator kurulumu tamamlanamadi: " + name);
        animator.runtimeAnimatorController = storyController;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return instance;
    }

    private static CharacterStoryProps BuildCharacterStoryProps(GameObject character, string name, float height, Materials m)
    {
        bool isCan = name.StartsWith("Can", StringComparison.Ordinal);
        GameObject wornShoes = StoryAuthoredPropFactory.CreateShoePair(name + "_WornShoes",
            character.transform, character.transform.position + new Vector3(0f, 0.01f, 0.09f),
            new Vector3(height * 0.34f, height * 0.14f, height * 0.3f), Vector3.zero,
            isCan ? m.amber : m.coral, m.navy, false);
        wornShoes.SetActive(false);

        GameObject wornBag = null;
        if (!isCan)
        {
            wornBag = StoryChapterBuilderCommon.InstantiateAsset(
                "Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Backpack.fbx",
                "Deniz_WornEmergencyBag", character.transform,
                character.transform.position + new Vector3(0f, height * 0.27f, -height * 0.17f),
                new Vector3(height * 0.38f, height * 0.48f, height * 0.22f),
                new Vector3(0f, 180f, 0f), false);
            wornBag.SetActive(false);
        }

        return new CharacterStoryProps
        {
            wornShoes = wornShoes,
            wornBag = wornBag
        };
    }

    private static void FitCharacterToHeight(GameObject instance, Vector3 feetPosition, float targetHeight)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            instance.transform.position = feetPosition;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        instance.transform.localScale = Vector3.one * scale;

        renderers = instance.GetComponentsInChildren<Renderer>();
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        Vector3 correction = feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        instance.transform.position += correction;
    }

    private static void EnsureCapsule(GameObject target, float radius, float height)
    {
        if (target.GetComponentInChildren<Collider>() != null)
            return;
        CapsuleCollider collider = target.AddComponent<CapsuleCollider>();
        collider.radius = radius;
        collider.height = height;
        collider.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private static StoryCameraController BuildCameras(Transform parent, Transform player, out Camera mainCamera, out CinemachineBrain brain)
    {
        GameObject cameraRoot = new GameObject("Cameras");
        cameraRoot.transform.SetParent(parent);

        GameObject main = new GameObject("Main Camera");
        main.tag = "MainCamera";
        main.transform.SetParent(cameraRoot.transform);
        main.transform.position = new Vector3(10.2f, 11.2f, -13.2f);
        main.transform.rotation = LookAt(main.transform.position, new Vector3(-0.1f, 0.9f, 0.7f));
        mainCamera = main.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(18, 24, 31, 255);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 120f;
        mainCamera.allowHDR = true;
        main.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = main.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        brain = main.AddComponent<CinemachineBrain>();
        // Dikey ekranda sert zoom hissi yerine oyuncunun mekansal yönünü koruyan,
        // okunabilir bir sinematik geçiş kullanıyoruz.
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.78f);

        StoryCameraBinding[] bindings =
        {
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.RoomOverview, "CM_RoomOverview", new Vector3(10.2f, 11.2f, -13.2f), new Vector3(-0.1f, 0.9f, 0.7f), 48f, player, 18.5f, new Vector2(-0.22f, 0.18f)),
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.QuakeClose, "CM_QuakeClose",
                new Vector3(4.0f, 4.2f, -5.2f), new Vector3(-0.35f, 0.78f, -0.35f), 44f,
                player, 6.6f, new Vector2(-0.08f, 0.13f)),
            // Güvenli alan planı: masa ayaklarının tamamını ve iki kardeşin sığınacağı
            // boşluğu aynı kadrajda tutan alçak, geniş bir üç çeyrek açı.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.UnderTable, "CM_UnderTable",
                new Vector3(1.8f, 1.2f, -3.6f), new Vector3(-0.1f, 0.5f, 0.2f), 46f),
            MakeFollowCamera(cameraRoot.transform, StoryCameraZoneId.PostQuake, "CM_PostQuake",
                new Vector3(5.7f, 5.8f, -7.0f), new Vector3(0.2f, 0.85f, 1.25f), 45f,
                player, 9.5f, new Vector2(-0.12f, 0.15f)),
            // Koridoru oda eşiğinden tek bir uzun planla kurar. Dört dünya etkileşimi
            // kadraj içinde kalır; oyuncu ilerledikçe kamera duvara veya tavana çarpmaz.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.Corridor, "CM_Corridor",
                new Vector3(2.5f, 2.7f, 2.5f), new Vector3(2.5f, 0.7f, 9.3f), 50f),
            // Masa incelemesi: yakın plan yerine masa silueti, dört ayak ve güvenli
            // alt boşluğu gösteren kurucu plan. Oyuncu alt üçlüde kalır.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectTable, "CM_InspectTableLegs",
                new Vector3(5.2f, 2.6f, -5.5f), new Vector3(0.45f, 0.5f, 0.55f), 48f),
            // Pencere incelemesi: camı üst üçlüye, güvenli bekleme mesafesini alt
            // üçlüye alan çapraz oda planı; masa yalnızca derinlik için kenarda kalır.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWindow, "CM_InspectWindow",
                new Vector3(3.2f, 2.6f, -0.5f), new Vector3(-2.2f, 1.78f, 5.65f), 44f),
            // Dolabın tamamı ve duvar bağlantısı tek bakışta okunur.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectWardrobe, "CM_InspectWardrobe",
                new Vector3(-1.6f, 2.15f, 2.3f), new Vector3(-4.18f, 1.42f, 5.38f), 42f),
            // Kapıyı bir kaçış ikonu gibi yakınlaştırmak yerine oda-koridor ilişkisini göster.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectExit, "CM_InspectExit",
                new Vector3(1.0f, 2.2f, 2.3f), new Vector3(2.5f, 1.25f, 5.95f), 40f),
            // Kırık cam planı: zemine yakın, pencereye doğru uzanan çapraz çizgi.
            // Parçalar ve yaklaşılmaması gereken zemin şeridi birlikte okunur; kamera tehlikenin
            // üstüne yapışmaz ve oyuncunun rota bağlamını kaybettirmez. Daha yüksek üç çeyrek
            // açı hem kırıkları hem pencere/dolap referansını aynı portre kadrajında tutar.
            MakeCamera(cameraRoot.transform, StoryCameraZoneId.InspectBrokenGlass, "CM_InspectBrokenGlass",
                new Vector3(-4.35f, 2.35f, -0.65f), new Vector3(-2.88f, 0.82f, 4.72f), 43f)
        };

        GameObject controllerObject = new GameObject("MissionCameraController_Story");
        controllerObject.transform.SetParent(cameraRoot.transform);
        StoryCameraController controller = controllerObject.AddComponent<StoryCameraController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        SerializedProperty array = serialized.FindProperty("cameras");
        array.arraySize = bindings.Length;
        for (int i = 0; i < bindings.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("zone").enumValueIndex = (int)bindings[i].zone;
            element.FindPropertyRelative("camera").objectReferenceValue = bindings[i].camera;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static StoryCameraBinding MakeCamera(Transform parent, StoryCameraZoneId zone, string name, Vector3 position, Vector3 target, float fov)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = LookAt(position, target);
        CinemachineCamera camera = go.AddComponent<CinemachineCamera>();
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 120f;
        camera.Lens = lens;
        camera.Priority = 0;
        CinemachineImpulseListener listener = go.AddComponent<CinemachineImpulseListener>();
        listener.ChannelMask = 1;
        listener.Gain = 1f;
        return new StoryCameraBinding { zone = zone, camera = camera };
    }

    private static StoryCameraBinding MakeFollowCamera(Transform parent, StoryCameraZoneId zone, string name,
        Vector3 position, Vector3 target, float fov, Transform player, float distance, Vector2 screenPosition)
    {
        StoryCameraBinding binding = MakeCamera(parent, zone, name, position, target, fov);
        CinemachineCamera camera = binding.camera;
        camera.Follow = player;

        CinemachinePositionComposer composer = camera.gameObject.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = distance;
        composer.TargetOffset = new Vector3(0f, 0.82f, 0f);
        composer.Damping = new Vector3(0.42f, 0.28f, 0.58f);
        composer.DeadZoneDepth = 0.45f;
        composer.CenterOnActivate = false;
        composer.Lookahead = new LookaheadSettings
        {
            Enabled = true,
            Time = 0.24f,
            Smoothing = 9f,
            IgnoreY = true
        };
        ScreenComposerSettings composition = ScreenComposerSettings.Default;
        composition.ScreenPosition = screenPosition;
        composition.DeadZone.Enabled = true;
        composition.DeadZone.Size = new Vector2(0.12f, 0.08f);
        composition.HardLimits.Enabled = true;
        composition.HardLimits.Size = new Vector2(0.72f, 0.62f);
        composer.Composition = composition;

        // Takip kamerası oyuncuyla odanın kenarına kaydığında duvarın arkasına geçebiliyordu.
        // Cinemachine'in sahne bileşeni görüş çizgisini kontrol edip kamerayı duvarın önüne
        // çeker; böylece özel bir runtime kamera/transform koduna ihtiyaç kalmaz.
        CinemachineDeoccluder deoccluder = camera.gameObject.AddComponent<CinemachineDeoccluder>();
        deoccluder.CollideAgainst = 1 << 0;
        deoccluder.IgnoreTag = "Player";
        deoccluder.TransparentLayers = 0;
        deoccluder.MinimumDistanceFromTarget = 0.65f;
        deoccluder.AvoidObstacles = new CinemachineDeoccluder.ObstacleAvoidance
        {
            Enabled = true,
            DistanceLimit = 0f,
            MinimumOcclusionTime = 0f,
            CameraRadius = 0.24f,
            UseFollowTarget = new CinemachineDeoccluder.ObstacleAvoidance.FollowTargetSettings
            {
                Enabled = true,
                YOffset = 0.82f
            },
            Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward,
            MaximumEffort = 4,
            SmoothingTime = 0.08f,
            Damping = 0.18f,
            DampingWhenOccluded = 0f
        };

        return binding;
    }

    private static StoryUIController BuildUI(Transform parent, StoryCameraController cameraController)
    {
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Regular SDF.asset");
        TMP_FontAsset semibold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-SemiBold SDF.asset");
        TMP_FontAsset bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bolum1/Inter-Bold SDF.asset");

        GameObject canvasObject = new GameObject("StoryUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject safeArea = CreateUIRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<StorySafeAreaPanel>();

        GameObject objective = CreatePanel("ObjectiveStrip", safeArea.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(760f, 196f), new Color(0.045f, 0.09f, 0.13f, 0.94f), false);
        TMP_Text objectiveTitle = CreateText("ObjectiveTitle", objective.transform, bold, 28f, Amber, TextAlignmentOptions.Left, new Vector2(0f, 44f), new Vector2(660f, 42f));
        TMP_Text objectiveDetail = CreateText("ObjectiveDetail", objective.transform, regular, 25f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, -38f), new Vector2(660f, 108f));

        Button pause = CreateButton("PauseButton", safeArea.transform, "II", bold, new Vector2(1f, 1f), new Vector2(-36f, -64f), new Vector2(96f, 96f), Navy, Cream);

        GameObject contextPanel = CreatePanel("ContextPrompt", safeArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 529f), new Vector2(860f, 88f), new Color(0.04f, 0.08f, 0.11f, 0.9f), false);
        TMP_Text context = CreateText("ContextText", contextPanel.transform, semibold, 24f, Amber, TextAlignmentOptions.Center, Vector2.zero, new Vector2(790f, 66f));
        contextPanel.SetActive(false);

        GameObject subtitlePanel = CreatePanel("SubtitlePanel", safeArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 205f), new Vector2(920f, 160f), new Color(0.02f, 0.04f, 0.06f, 0.9f), false);
        TMP_Text subtitle = CreateText("SubtitleText", subtitlePanel.transform, regular, 29f, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(850f, 128f));
        subtitlePanel.SetActive(false);

        GameObject pauseOverlay = CreatePanel("PauseOverlay", safeArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.005f, 0.012f, 0.018f, 0.72f), true);
        GameObject pauseCard = CreatePanel("PausePanel", pauseOverlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(820f, 900f), new Color(0.035f, 0.07f, 0.1f, 0.99f), true);
        CreateText("PauseTitle", pauseCard.transform, bold, 46f, Cream, TextAlignmentOptions.Center, new Vector2(0f, 340f), new Vector2(680f, 70f)).text = "OYUN DURAKLATILDI";
        CreateText("PauseSaveHint", pauseCard.transform, regular, 23f, new Color(0.76f, 0.82f, 0.84f), TextAlignmentOptions.Center, new Vector2(0f, 275f), new Vector2(680f, 56f)).text = "İlerleme son kontrol noktasında otomatik kaydedilir.";
        Toggle shakeToggle = CreateLabeledToggle("ReduceShakeToggle", pauseCard.transform, "Sarsıntıyı azalt", semibold, new Vector2(0f, 120f), Amber, out TMP_Text shakeState);
        CreateText("ShakeHelp", pauseCard.transform, regular, 22f, new Color(0.76f, 0.82f, 0.84f), TextAlignmentOptions.Center, new Vector2(0f, 42f), new Vector2(650f, 62f)).text = "Kamera hareketi azalır; ses ve çevresel efektler korunur.";
        Button resume = CreateButton("ResumeButton", pauseCard.transform, "DEVAM ET", bold, Vector2.one * 0.5f, new Vector2(0f, -130f), new Vector2(560f, 104f), Teal, Color.white);
        Button retry = CreateButton("RetryCheckpointButton", pauseCard.transform, "SON KONTROL NOKTASINA DÖN", semibold, Vector2.one * 0.5f, new Vector2(0f, -260f), new Vector2(560f, 104f), new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        CreateText("RetryHint", pauseCard.transform, regular, 20f, new Color(0.7f, 0.76f, 0.79f), TextAlignmentOptions.Center, new Vector2(0f, -360f), new Vector2(650f, 48f)).text = "Kaydedilen hikâye kararların korunur.";
        pauseOverlay.SetActive(false);

        GameObject completionOverlay = CreatePanel("CompletionOverlay", safeArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.005f, 0.012f, 0.018f, 0.78f), true);
        GameObject completionReport = CreatePanel("CompletionReportCard", completionOverlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(900f, 1160f), new Color(0.035f, 0.07f, 0.1f, 0.99f), true);
        CreateText("CompletionEyebrow", completionReport.transform, semibold, 25f, Amber, TextAlignmentOptions.Center, new Vector2(0f, 480f), new Vector2(720f, 45f)).text = "DİKEY DİLİM TAMAMLANDI";
        CreateText("CompletionTitle", completionReport.transform, bold, 48f, Cream, TextAlignmentOptions.Center, new Vector2(0f, 390f), new Vector2(740f, 110f)).text = "Can'ı güvende tuttun";
        TMP_Text preparationValue = CreateReportRow(completionReport.transform, "HAZIRLIK", "Hazırlık kararları değerlendiriliyor", 210f, Teal, semibold, regular);
        TMP_Text protectionValue = CreateReportRow(completionReport.transform, "KORUMA", "Korunma davranışları değerlendiriliyor", 70f, Amber, semibold, regular);
        TMP_Text cooperationValue = CreateReportRow(completionReport.transform, "YARDIMLAŞMA", "Kardeş desteği değerlendiriliyor", -70f, Coral, semibold, regular);
        CreateText("CompletionFooter", completionReport.transform, regular, 23f, new Color(0.75f, 0.8f, 0.82f), TextAlignmentOptions.Center, new Vector2(0f, -215f), new Vector2(740f, 92f)).text = "Koridorda artçı sarsıntı duyuldu.\nTahliye perdesi burada başlayacak.";
        Button replay = CreateButton("ReplayStoryButton", completionReport.transform, "YENİDEN OYNA", bold, Vector2.one * 0.5f, new Vector2(0f, -365f), new Vector2(580f, 104f), Teal, Color.white);
        Button chapters = CreateButton("ChapterSelectionButton", completionReport.transform, "BÖLÜM SEÇİMİ", semibold, Vector2.one * 0.5f, new Vector2(0f, -495f), new Vector2(580f, 104f), new Color(0.12f, 0.2f, 0.25f, 1f), Cream);

        GameObject chapterCard = CreatePanel("ChapterSelectionCard", completionOverlay.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(900f, 980f), new Color(0.035f, 0.07f, 0.1f, 0.99f), true);
        CreateText("ChapterTitle", chapterCard.transform, bold, 46f, Cream, TextAlignmentOptions.Center, new Vector2(0f, 380f), new Vector2(720f, 70f)).text = "BÖLÜM SEÇİMİ";
        CreateText("ChapterHint", chapterCard.transform, regular, 23f, new Color(0.75f, 0.8f, 0.82f), TextAlignmentOptions.Center, new Vector2(0f, 310f), new Vector2(720f, 60f)).text = "Şu anda oynanabilir kalite kapısı";
        Button replayFromChapters = CreateButton("QuakeChapterButton", chapterCard.transform, "3. PERDE — DEPREMİ YENİDEN OYNA", bold, Vector2.one * 0.5f, new Vector2(0f, 165f), new Vector2(680f, 112f), Teal, Color.white);
        CreateText("LockedPreparation", chapterCard.transform, semibold, 24f, new Color(0.62f, 0.67f, 0.7f), TextAlignmentOptions.Center, new Vector2(0f, 35f), new Vector2(680f, 54f)).text = "1. PERDE — HAZIRLIK  •  KALİTE KAPISI SONRASI";
        CreateText("LockedSafety", chapterCard.transform, semibold, 24f, new Color(0.62f, 0.67f, 0.7f), TextAlignmentOptions.Center, new Vector2(0f, -45f), new Vector2(680f, 54f)).text = "2. PERDE — EVİ GÜVENLİ YAP  •  KİLİTLİ";
        CreateText("LockedEvacuation", chapterCard.transform, semibold, 24f, new Color(0.62f, 0.67f, 0.7f), TextAlignmentOptions.Center, new Vector2(0f, -125f), new Vector2(680f, 54f)).text = "4. PERDE — TAHLİYE  •  KİLİTLİ";
        Button reportBack = CreateButton("CompletionReportBackButton", chapterCard.transform, "RAPORA DÖN", semibold, Vector2.one * 0.5f, new Vector2(0f, -345f), new Vector2(560f, 104f), new Color(0.12f, 0.2f, 0.25f, 1f), Cream);
        chapterCard.SetActive(false);
        completionOverlay.SetActive(false);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(parent);

        StoryUIController controller = canvasObject.AddComponent<StoryUIController>();
        SetReference(controller, "objectiveTitle", objectiveTitle);
        SetReference(controller, "objectiveDetail", objectiveDetail);
        SetReference(controller, "subtitle", subtitle);
        SetReference(controller, "contextPrompt", context);
        SetReference(controller, "pausePanel", pauseOverlay);
        SetReference(controller, "completionPanel", completionOverlay);
        SetReference(controller, "completionReportCard", completionReport);
        SetReference(controller, "chapterSelectionCard", chapterCard);
        SetReference(controller, "reduceShakeToggle", shakeToggle);
        SetReference(controller, "reduceShakeState", shakeState);
        SetReference(controller, "completionPreparationValue", preparationValue);
        SetReference(controller, "completionProtectionValue", protectionValue);
        SetReference(controller, "completionCooperationValue", cooperationValue);
        SetReference(controller, "cameraController", cameraController);

        UnityEventTools.AddPersistentListener(pause.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(resume.onClick, controller.TogglePause);
        UnityEventTools.AddPersistentListener(retry.onClick, controller.RetryCheckpoint);
        UnityEventTools.AddPersistentListener(shakeToggle.onValueChanged, controller.SetReducedShake);
        UnityEventTools.AddPersistentListener(replay.onClick, controller.ReplayStory);
        UnityEventTools.AddPersistentListener(chapters.onClick, controller.ShowChapterSelection);
        UnityEventTools.AddPersistentListener(replayFromChapters.onClick, controller.ReplayStory);
        UnityEventTools.AddPersistentListener(reportBack.onClick, controller.ShowCompletionReport);
        return controller;
    }

    private static TMP_Text CreateReportRow(Transform parent, string label, string value, float y, Color accent, TMP_FontAsset semibold, TMP_FontAsset regular)
    {
        GameObject row = CreatePanel("Report_" + label, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, y), new Vector2(760f, 118f), new Color(1f, 1f, 1f, 0.055f), false);
        TMP_Text heading = CreateText("Label", row.transform, semibold, 22f, accent, TextAlignmentOptions.Left, new Vector2(-205f, 24f), new Vector2(290f, 36f));
        heading.text = label;
        TMP_Text detail = CreateText("Value", row.transform, regular, 23f, Color.white, TextAlignmentOptions.Left, new Vector2(25f, -20f), new Vector2(650f, 62f));
        detail.text = value;
        return detail;
    }

    private static InteractionReferences BuildInteractions(Transform parent, WorldReferences world, Transform can, Materials m, StorySequenceDirector sequence)
    {
        Transform interactionRoot = NewChild(parent, "StoryInteractions");
        InteractionReferences refs = new InteractionReferences();
        refs.intro = new[]
        {
            CreateHotspot("Inspect_SafeTable", world.tableFocus.position + Vector3.up * 0.65f, new Vector3(2.8f, 1.4f, 1.8f), "MASANIN ALTINI İNCELE", StoryInteractionKind.Inspect, false, world.tableFocus, interactionRoot, m, 1.1f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.InspectTable, true, StoryCameraZoneId.RoomOverview, 3.45f),
            CreateHotspot("Inspect_Window", new Vector3(-2.2f, 1.8f, 5.72f), new Vector3(3f, 2.6f, 1.2f), "GÜVENLİ MESAFEYİ GÖR", StoryInteractionKind.Inspect, false, world.windowFocus, interactionRoot, m, 1.1f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.InspectWindow, true, StoryCameraZoneId.RoomOverview, 3.45f),
            CreateHotspot("Inspect_Wardrobe", new Vector3(-4.2f, 1.4f, 5.32f), new Vector3(1.8f, 2.8f, 1.5f), "SABİTLEMEYİ İNCELE", StoryInteractionKind.Inspect, false, world.wardrobeFocus, interactionRoot, m, 1.1f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.InspectWardrobe, true, StoryCameraZoneId.RoomOverview, 3.45f),
            CreateHotspot("Inspect_Exit", new Vector3(2.5f, 1.4f, 5.72f), new Vector3(2.2f, 2.8f, 1.5f), "ÇIKIŞI KONTROL ET", StoryInteractionKind.Inspect, false, world.exitInspectFocus, interactionRoot, m, 1.1f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.InspectExit, true, StoryCameraZoneId.RoomOverview, 3.45f)
        };
        foreach (StoryInteractable interactable in refs.intro)
            UnityEventTools.AddPersistentListener(interactable.OnInteracted, sequence.OnIntroInspection);

        Transform canFocus = CreateFocus("CanInteractionPoint", can.position + new Vector3(-0.35f, 0f, -0.35f), can);
        refs.calmSibling = CreateHotspot("Calm_Can", can.position + Vector3.up * 0.62f, new Vector3(1.15f, 1.45f, 1.15f), "CAN'LA GÖZ TEMASI KUR", StoryInteractionKind.HelpSibling, false, canFocus, can, m, 1.2f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.QuakeClose);
        refs.crouchStep = CreateHotspot("TakeCover_Crouch", world.tableFocus.position + Vector3.up * 0.45f, new Vector3(2.7f, 1.25f, 1.9f), "Masanın güvenli tarafına geç", StoryInteractionKind.TakeCover, false, world.tableFocus, interactionRoot, m, 1.4f, null, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.UnderTable);
        refs.coverHeadStep = CreateHotspot("TakeCover_ProtectHead", new Vector3(0.35f, 0.72f, 0.2f), new Vector3(1.45f, 1.05f, 1.35f), "Başını korumak için burada basılı tut", StoryInteractionKind.TakeCover, true, world.tableFocus, interactionRoot, m, 1.8f, null, StoryInteractionGesture.WorldHold, 1, StoryCameraZoneId.UnderTable);
        refs.safeCover = CreateHotspot("TakeCover_GripTable", new Vector3(-0.67f, 0.55f, 0.08f), new Vector3(0.85f, 1.1f, 0.85f), "Masa ayağında dengeni koru", StoryInteractionKind.TakeCover, true, world.tableFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.WorldHold, 1, StoryCameraZoneId.UnderTable);
        refs.unsafeDoor = CreateHotspot("Unsafe_Door", new Vector3(2.5f, 1.25f, 5.55f), new Vector3(1.7f, 2.7f, 1f), "Kapıya koşmayı dene", StoryInteractionKind.UnsafeChoice, true, world.corridorFocus, interactionRoot, m, 1.2f, Coral, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectExit, true, StoryCameraZoneId.QuakeClose, 2.8f);
        refs.unsafeWindow = CreateHotspot("Unsafe_Window", new Vector3(-2.2f, 1.8f, 5.72f), new Vector3(3f, 2.6f, 1.2f), "Pencereye gitmeyi dene", StoryInteractionKind.UnsafeChoice, true, world.windowFocus, interactionRoot, m, 1.2f, Coral, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectWindow, true, StoryCameraZoneId.QuakeClose, 2.8f);

        StoryInteractable answerParent = CreateHotspot("Post_AnswerParent", world.exitInspectFocus.position + Vector3.up * 0.65f, new Vector3(1.5f, 1.6f, 1.2f), "Kapı yönüne dönüp anneye ses ver", StoryInteractionKind.HelpSibling, false, world.exitInspectFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectExit, true, StoryCameraZoneId.PostQuake, 1.8f);
        StoryInteractable callCan = CreateHotspot("Post_CallCan", can.position + Vector3.up * 0.62f, new Vector3(1.2f, 1.55f, 1.2f), "CAN'A SESLEN", StoryInteractionKind.HelpSibling, false, canFocus, can, m, 1.1f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.QuakeClose, true, StoryCameraZoneId.PostQuake, 1.8f);
        StoryInteractable checkBreathing = CreateHotspot("Post_CheckBreathing", can.position + Vector3.up * 0.65f, new Vector3(1.2f, 1.6f, 1.2f), "NEFESİNİ KONTROL ET", StoryInteractionKind.HelpSibling, true, canFocus, can, m, 2.2f, null, StoryInteractionGesture.WorldHold, 1, StoryCameraZoneId.QuakeClose, true, StoryCameraZoneId.PostQuake, 1.2f);
        StoryInteractable checkArms = CreateHotspot("Post_CheckArms", can.position + Vector3.up * 0.65f, new Vector3(1.2f, 1.6f, 1.2f), "KOLLARINI VE BACAKLARINI GÖZLE", StoryInteractionKind.HelpSibling, false, canFocus, can, m, 1.8f, null, StoryInteractionGesture.SwipeHorizontal, 1, StoryCameraZoneId.QuakeClose, true, StoryCameraZoneId.PostQuake, 1.8f);
        StoryInteractable inspectGlass = CreateHotspot("Post_InspectGlass", world.glassFocus.position + Vector3.up * 0.35f, new Vector3(1.8f, 0.9f, 1.5f), "Cam sınırına yaklaşmadan güvenli kenarı bul", StoryInteractionKind.Inspect, false, world.glassFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectBrokenGlass, true, StoryCameraZoneId.PostQuake, 3.6f);
        StoryInteractable inspectWardrobe = CreateHotspot("Post_InspectWardrobe", new Vector3(-4.2f, 1.4f, 5.32f), new Vector3(1.8f, 2.8f, 1.5f), "DOLABIN DEVRİLME YÖNÜNÜ GÖR", StoryInteractionKind.Inspect, false, world.wardrobeFocus, interactionRoot, m, 1.2f, null, StoryInteractionGesture.Tap, 1, StoryCameraZoneId.InspectWardrobe, true, StoryCameraZoneId.PostQuake, 2.2f);
        StoryInteractable inspectExit = CreateHotspot("Post_InspectExit", new Vector3(2.5f, 1.4f, 5.72f), new Vector3(2.2f, 2.8f, 1.5f), "Kapının açık kalan tarafına yaklaş", StoryInteractionKind.Inspect, false, world.exitInspectFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectExit, true, StoryCameraZoneId.PostQuake, 1.8f);
        StoryInteractable leftShoe = CreateHotspot("Post_LeftShoe", world.leftShoeFocus.position + Vector3.up * 0.3f, new Vector3(0.9f, 0.8f, 0.9f), "Sol ayakkabının yanına git", StoryInteractionKind.Collect, false, world.leftShoeFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach);
        StoryInteractable rightShoe = CreateHotspot("Post_RightShoe", world.rightShoeFocus.position + Vector3.up * 0.3f, new Vector3(0.9f, 0.8f, 0.9f), "Sağ ayakkabının yanına git", StoryInteractionKind.Collect, false, world.rightShoeFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach);
        StoryInteractable putShoes = CreateHotspot("Post_PutShoesOn", world.shoesFocus.position + Vector3.up * 0.35f, new Vector3(1.2f, 0.9f, 1.2f), "AYAKKABILARI GİY", StoryInteractionKind.Collect, false, world.shoesFocus, interactionRoot, m, 1.8f, null, StoryInteractionGesture.SwipeDown, 1);
        StoryInteractable helpCanShoes = CreateHotspot("Post_HelpCanShoes", can.position + Vector3.up * 0.5f, new Vector3(1.25f, 1.35f, 1.25f), "CAN'IN BAĞCIKLARINA YARDIM ET", StoryInteractionKind.HelpSibling, false, canFocus, can, m, 2.2f, null, StoryInteractionGesture.RepeatedTap, 2, StoryCameraZoneId.QuakeClose, true, StoryCameraZoneId.PostQuake, 1.8f);
        StoryInteractable collectBag = CreateHotspot("Post_CollectBag", world.bagFocus.position + Vector3.up * 0.45f, new Vector3(1.2f, 1f, 1.2f), "AFET ÇANTASINI AL", StoryInteractionKind.Collect, false, world.bagFocus, interactionRoot, m, 1.4f, null, StoryInteractionGesture.Tap);
        Transform wornBagFocus = CreateFocus("WornBagInteractionPoint",
            world.denizWornBag.transform.position + new Vector3(0f, 0.1f, -0.42f), world.denizWornBag.transform);
        Vector3 wornBagHotspot = world.denizWornBag.transform.position + Vector3.up * 0.78f;
        StoryInteractable checkWater = CreateHotspot("Post_CheckWater", wornBagHotspot, new Vector3(0.95f, 1.25f, 0.95f), "SU GÖZÜNÜ KONTROL ET", StoryInteractionKind.Inspect, true, wornBagFocus, world.denizWornBag.transform, m, 1.1f, null, StoryInteractionGesture.Tap);
        StoryInteractable checkFirstAid = CreateHotspot("Post_CheckFirstAid", wornBagHotspot, new Vector3(0.95f, 1.25f, 0.95f), "İLK YARDIM GÖZÜNÜ KONTROL ET", StoryInteractionKind.Inspect, true, wornBagFocus, world.denizWornBag.transform, m, 1.1f, null, StoryInteractionGesture.Tap);
        StoryInteractable secureBag = CreateHotspot("Post_SecureBag", wornBagHotspot, new Vector3(0.95f, 1.25f, 0.95f), "İKİ ASKINI SIKILA", StoryInteractionKind.Collect, true, wornBagFocus, world.denizWornBag.transform, m, 1.6f, null, StoryInteractionGesture.SwipeHorizontal);

        refs.postQuakeBeats = new[]
        {
            Beat(answerParent, "EBEVEYNE SES VER", "Engelin arkasındaki anneye ikinizin de iyi olduğunu söyle.", "Deniz: İyiyiz anne! Önce birbirimizi kontrol ediyoruz.", 3f),
            Beat(callCan, "CAN'I KONTROL ET — 1/3", "Can'a adıyla seslen; cevap verip vermediğini dinle.", "Can hemen cevap veriyor; bilinci açık.", 3f),
            Beat(checkBreathing, "CAN'I KONTROL ET — 2/3", "Nefesini ve konuşmasını birkaç saniye gözle.", "Can düzenli nefes alıyor ve nerede olduğunu biliyor.", 3f),
            Beat(checkArms, "CAN'I KONTROL ET — 3/3", "Yaralanma göstermeden kol ve bacak hareketlerini gözle kontrol et.", "Can kollarını ve bacaklarını hareket ettirebiliyor. Birlikte kalkabilirsiniz.", 3.5f),
            Beat(inspectGlass, "ZEMİNİ OKU — 1/3", "Kırık cama dokunma; güvenli yürüme sınırını gör.", "Cam parçaları pencere tarafında. Halının açık kenarı güvenli kalmış.", 3f),
            Beat(inspectWardrobe, "ZEMİNİ OKU — 2/3", "Dolaba yaklaşmadan devrilme yönünü ve geçişi kontrol et.", "{WARDROBE}", 3f),
            Beat(inspectExit, "ZEMİNİ OKU — 3/3", "Kapıya koşmadan çıkışın açık kalan tarafını belirle.", "{EXIT}", 3.5f),
            Beat(leftShoe, "AYAKLARINI KORU — 1/3", "Kırık parçalara basmamak için sol ayakkabıyı al.", "Sol ayakkabı bulundu.", 2.5f),
            Beat(rightShoe, "AYAKLARINI KORU — 2/3", "Sağ ayakkabıyı da al.", "İki ayakkabı da yanında.", 2.5f),
            Beat(putShoes, "AYAKLARINI KORU — 3/3", "Ayakkabıları acele etmeden giy ve bağlarını sık.", "Deniz'in ayakları kırık parçalara karşı korundu.", 3f),
            Beat(helpCanShoes, "CAN'A YARDIM ET", "Can'ın ayakkabılarını giyip bağlamasına yardım et.", "Can da hazır. İkiniz de zeminde daha güvenli yürüyebilirsiniz.", 3f),
            Beat(collectBag, "AFET ÇANTASINI AL — 1/4", "Çantayı düşen eşyalardan uzak taraftan al.", "Afet çantası Deniz'in yanında.", 3f),
            Beat(checkWater, "AFET ÇANTASINI AL — 2/4", "Su gözünü dışarıdan yokla; kapağı açıp zaman kaybetme.", "{WATER}", 3f),
            Beat(checkFirstAid, "AFET ÇANTASINI AL — 3/4", "İlk yardım gözünün kapalı ve erişilebilir olduğunu kontrol et.", "{FIRSTAID}", 3f),
            Beat(secureBag, "AFET ÇANTASINI AL — 4/4", "Ellerin serbest kalsın diye çantanın iki askısını sık.", "Çanta sırtta; Deniz'in iki eli de Can'a yardım etmek için serbest.", 3.5f)
        };

        refs.lightWithFlashlight = CreateHotspot("Post_TestFlashlight", world.flashlightFocus.position + Vector3.up * 0.55f, new Vector3(1.2f, 1.1f, 1.2f), "Feneri alıp zemine yönelt", StoryInteractionKind.Collect, false, world.flashlightFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach);
        refs.lightWithoutFlashlight = CreateHotspot("Post_FindEmergencyLight", world.emergencyLightFocus.position + Vector3.up * 0.55f, new Vector3(1.2f, 1.1f, 1.2f), "Acil ışığın aydınlattığı noktaya git", StoryInteractionKind.Inspect, false, world.emergencyLightFocus, interactionRoot, m, 3f, null, StoryInteractionGesture.Approach);
        Transform exitStandPoint = CreateFocus("ExitStandPoint", new Vector3(2.5f, 0.1f, 5.05f), interactionRoot);
        refs.exit = CreateHotspot("Exit_Corridor", new Vector3(2.5f, 1.25f, 5.25f), new Vector3(2.2f, 2.8f, 1.8f), "Can'la kapı eşiğine ilerle", StoryInteractionKind.Exit, false, exitStandPoint, interactionRoot, m, 3f, Teal, StoryInteractionGesture.Approach, 1, StoryCameraZoneId.InspectExit, true, StoryCameraZoneId.Corridor, 1.8f);
        world.closedDoor.transform.SetParent(refs.exit.transform, true);

        Transform glassHazardCenter = CreateFocus("BrokenGlassHazardCenter", new Vector3(-2.92f, 0.08f, 4.82f), interactionRoot);
        refs.brokenGlassHazard = CreateHotspot("Hazard_BrokenGlass", new Vector3(-2.92f, 0.28f, 4.82f),
            new Vector3(1.45f, 0.58f, 1.25f), "Kırık cam tehlike alanı", StoryInteractionKind.UnsafeChoice,
            true, glassHazardCenter, interactionRoot, m, 0.5f, Coral, StoryInteractionGesture.Approach);
        SerializedObject hazardSerialized = new SerializedObject(refs.brokenGlassHazard);
        hazardSerialized.FindProperty("autoTriggerOnPlayerEnter").boolValue = true;
        hazardSerialized.FindProperty("oneShot").boolValue = false;
        hazardSerialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(refs.brokenGlassHazard.OnInteracted, sequence.OnPostQuakeHazard);

        refs.corridorBeats = new BeatDefinition[world.corridorStepFoci.Length];
        string[] corridorTitles = { "EŞİKTE DİNLE", "AYDINLATMAYI İZLE", "CAN'I YANINDA TUT", "ARTÇIYI FARK ET" };
        string[] corridorDetails =
        {
            "Kapı eşiğinde dur; tavandan ve zeminden yeni bir ses geliyor mu dinle.",
            "Işığı veya acil lambayı zeminde güvenli adım noktalarına yönelt.",
            "Can'ın elini bırakmadan ebeveyn sesinin geldiği yönü doğrula.",
            "İnce titreşimi fark et; merdivene koşmadan açık noktada bekle."
        };
        string[] corridorSubtitles =
        {
            "Eşikte yeni bir düşme sesi yok. Koridora adım atmak güvenli görünüyor.",
            "Zemindeki işaretler okunuyor; karanlık bölüm yavaşça geçilebilir.",
            "Anne ve baba engelin diğer tarafında. Çocuklar sesle iletişimi sürdürüyor.",
            "Tavandan hafif bir çıtırtı geliyor. Deniz durup Can'ı yanında tutuyor."
        };
        StoryInteractionGesture[] corridorGestures =
        {
            StoryInteractionGesture.RepeatedTap,
            StoryInteractionGesture.SwipeHorizontal,
            StoryInteractionGesture.RepeatedTap,
            StoryInteractionGesture.SwipeDown
        };
        for (int i = 0; i < world.corridorStepFoci.Length; i++)
        {
            StoryInteractable marker = CreateHotspot("Corridor_Beat_" + (i + 1), world.corridorStepFoci[i].position + Vector3.up * 0.5f,
                new Vector3(1.25f, 1.1f, 1.25f), corridorTitles[i], StoryInteractionKind.Inspect, false,
                world.corridorStepFoci[i], interactionRoot, m, i == 3 ? 6f : 4f, null,
                StoryInteractionGesture.Approach, 1,
                StoryCameraZoneId.Corridor);
            refs.corridorBeats[i] = Beat(marker, corridorTitles[i], corridorDetails[i], corridorSubtitles[i], 3f);
            UnityEventTools.AddPersistentListener(marker.OnInteracted, sequence.OnCorridorStep);
        }

        UnityEventTools.AddPersistentListener(refs.calmSibling.OnInteracted, sequence.OnSiblingCalmed);
        UnityEventTools.AddPersistentListener(refs.crouchStep.OnInteracted, sequence.OnCrouchStep);
        UnityEventTools.AddPersistentListener(refs.coverHeadStep.OnInteracted, sequence.OnCoverHeadStep);
        UnityEventTools.AddPersistentListener(refs.safeCover.OnInteracted, sequence.OnCoverReached);
        UnityEventTools.AddPersistentListener(refs.unsafeDoor.OnInteracted, sequence.OnUnsafeChoice);
        UnityEventTools.AddPersistentListener(refs.unsafeWindow.OnInteracted, sequence.OnUnsafeChoice);
        foreach (BeatDefinition beat in refs.postQuakeBeats)
            UnityEventTools.AddPersistentListener(beat.interactable.OnInteracted, sequence.OnPostQuakeStep);
        UnityEventTools.AddPersistentListener(refs.lightWithFlashlight.OnInteracted, sequence.OnLightPrepared);
        UnityEventTools.AddPersistentListener(refs.lightWithoutFlashlight.OnInteracted, sequence.OnLightPrepared);
        UnityEventTools.AddPersistentListener(refs.exit.OnInteracted, sequence.OnCorridorReached);
        return refs;
    }

    private static StoryInteractable CreateHotspot(string name, Vector3 position, Vector3 size, string prompt,
        StoryInteractionKind kind, bool fromAnywhere, Transform interactionPoint, Transform parent, Materials m,
        float estimatedSeconds = 1.25f, Color? ringColor = null,
        StoryInteractionGesture gesture = StoryInteractionGesture.Tap, int gestureCount = 1,
        StoryCameraZoneId focusZone = StoryCameraZoneId.None, bool returnCamera = false,
        StoryCameraZoneId returnZone = StoryCameraZoneId.RoomOverview, float focusLingerSeconds = 1.4f)
    {
        GameObject hotspot = new GameObject(name);
        hotspot.transform.SetParent(parent);
        hotspot.transform.position = position;
        BoxCollider collider = hotspot.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;

        GameObject ring = new GameObject("InteractionMarker");
        ring.transform.SetParent(hotspot.transform);
        ring.transform.position = interactionPoint.position + Vector3.up * 0.025f;
        Material markerMaterial = interactionKindMaterial(kind, ringColor, m);
        CreatePrimitive("HighlightRing", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.68f, 0.012f, 0.68f), markerMaterial, ring.transform, false, true);
        CreatePrimitive("HighlightCenter", PrimitiveType.Cylinder, new Vector3(0f, 0.014f, 0f),
            new Vector3(0.16f, 0.014f, 0.16f), markerMaterial, ring.transform, false, true);
        GameObject directionTick = CreatePrimitive("HighlightDirection", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.48f),
            new Vector3(0.13f, 0.018f, 0.28f), markerMaterial, ring.transform, false, true);
        directionTick.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        SphereCollider ringHitArea = ring.AddComponent<SphereCollider>();
        ringHitArea.radius = 0.8f;
        ringHitArea.isTrigger = true;

        StoryInteractable interactable = hotspot.AddComponent<StoryInteractable>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactionId").stringValue = name;
        serialized.FindProperty("interactionKind").enumValueIndex = (int)kind;
        serialized.FindProperty("prompt").stringValue = prompt;
        serialized.FindProperty("interactionPoint").objectReferenceValue = interactionPoint;
        serialized.FindProperty("interactionRange").floatValue = 1.05f;
        serialized.FindProperty("interactionGesture").enumValueIndex = (int)gesture;
        serialized.FindProperty("requiredGestureCount").intValue = gestureCount;
        serialized.FindProperty("estimatedInteractionSeconds").floatValue = estimatedSeconds;
        serialized.FindProperty("focusCameraZone").enumValueIndex = (int)focusZone;
        serialized.FindProperty("returnCameraAfterCompletion").boolValue = returnCamera;
        serialized.FindProperty("returnCameraZone").enumValueIndex = (int)returnZone;
        serialized.FindProperty("focusLingerSeconds").floatValue = focusLingerSeconds;
        serialized.FindProperty("interactFromAnywhere").boolValue = fromAnywhere;
        serialized.FindProperty("oneShot").boolValue = true;
        serialized.FindProperty("availableOnStart").boolValue = false;
        serialized.FindProperty("highlightRoot").objectReferenceValue = ring;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ring.SetActive(false);
        return interactable;
    }

    private static BeatDefinition Beat(StoryInteractable interactable, string title, string detail, string subtitle, float delayAfter)
    {
        return new BeatDefinition
        {
            interactable = interactable,
            title = title,
            detail = detail,
            subtitle = subtitle,
            delayAfter = delayAfter
        };
    }

    private static PlayableDirector BuildEarthquakeTimeline(Transform parent, Materials m, WorldReferences world, out CinemachineImpulseSource impulse, out AudioSource impactSource)
    {
        GameObject timelineRoot = new GameObject("EarthquakeTimeline");
        timelineRoot.transform.SetParent(parent);
        PlayableDirector director = timelineRoot.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;

        GameObject fxRoot = new GameObject("Earthquake_FX_SceneAuthored");
        fxRoot.transform.SetParent(timelineRoot.transform);
        CreateDustParticles(fxRoot.transform, m.dust, "CeilingDust", new Vector3(0f, 2.85f, 0.7f), new Vector3(8f, 0.5f, 8f), 13f, true);
        CreateDustParticles(fxRoot.transform, m.dust, "DebrisBurst", new Vector3(2.8f, 2.4f, 2.4f), new Vector3(2f, 1f, 2f), 22f, false);

        AudioClip rumble = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/quake_rumble.wav");
        AudioSource rumbleSource = fxRoot.AddComponent<AudioSource>();
        rumbleSource.clip = rumble;
        rumbleSource.loop = true;
        rumbleSource.playOnAwake = true;
        rumbleSource.volume = 0.42f;
        rumbleSource.spatialBlend = 0f;

        GameObject flickerObject = new GameObject("QuakeFlickerLight");
        flickerObject.transform.SetParent(fxRoot.transform);
        flickerObject.transform.position = new Vector3(0f, 2.7f, 0f);
        Light flickerLight = flickerObject.AddComponent<Light>();
        flickerLight.type = LightType.Point;
        flickerLight.range = 9f;
        flickerLight.intensity = 1.2f;
        flickerLight.color = new Color(1f, 0.63f, 0.38f);
        Animator animator = flickerObject.AddComponent<Animator>();

        TimelineAsset timeline = CreateTimelineAsset(TimelineRoot + "/Story_03_Earthquake.playable");
        ActivationTrack activationTrack = timeline.CreateTrack<ActivationTrack>(null, "Deprem VFX ve Ses");
        TimelineClip activationClip = activationTrack.CreateDefaultClip();
        activationClip.duration = 120d;
        director.SetGenericBinding(activationTrack, fxRoot);

        AnimationClip lightClip = CreateFlickerClip();
        AnimationTrack animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Işık Titremesi");
        TimelineClip timelineLightClip = animationTrack.CreateClip(lightClip);
        timelineLightClip.duration = 120d;
        director.SetGenericBinding(animationTrack, animator);

        BindLoopingMotion(timeline, director, world.safeTable, "Masa titreşimi", CreateRattleClip("SafeTable_Rattle", world.safeTable.transform, 0.045f, 0.035f, 1.35f));
        BindLoopingMotion(timeline, director, world.hangingLamp, "Asılı lamba salınımı", CreateSwingClip("HangingLamp_Swing"));
        BindLoopingMotion(timeline, director, world.looseProps, "Radyo ve hafif eşya kayması", CreateRattleClip("LooseProps_Slide", world.looseProps.transform, 0.12f, 0.08f, 2.1f));
        BindLoopingMotion(timeline, director, world.closedDoor, "Kapı vuruntusu", CreateDoorRattleClip());
        BindLoopingMotion(timeline, director, world.wardrobeSecured, "Sabit dolap titreşimi", CreateRattleClip("SecuredWardrobe_Rattle", world.wardrobeSecured.transform, 0.018f, 0.012f, 0.9f));
        BindLoopingMotion(timeline, director, world.shelfStable, "Sabit raf titreşimi", CreateRattleClip("SecuredShelf_Rattle", world.shelfStable.transform, 0.016f, 0.012f, 0.8f));
        BindOneShotMotion(timeline, director, world.shelfUnsecured, "Sabitlenmemiş raf devrilmesi", CreateFallClip("UnsecuredShelf_Fall", -68f, 6.5f), 7d);
        BindOneShotMotion(timeline, director, world.wardrobeUnsecured, "Sabitlenmemiş dolap devrilmesi", CreateFallClip("UnsecuredWardrobe_Fall", 78f, 8f), 11d);
        director.playableAsset = timeline;
        fxRoot.SetActive(false);

        GameObject impulseObject = new GameObject("EarthquakeImpulseSource");
        impulseObject.transform.SetParent(timelineRoot.transform);
        impulse = impulseObject.AddComponent<CinemachineImpulseSource>();
        impulse.ImpulseDefinition.ImpulseChannel = 1;
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        impulse.ImpulseDefinition.ImpulseDuration = 0.5f;
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulse.DefaultVelocity = new Vector3(0.65f, 0.25f, 0.5f);

        impactSource = timelineRoot.AddComponent<AudioSource>();
        impactSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/quake_impact.wav");
        impactSource.playOnAwake = false;
        impactSource.volume = 0.8f;
        impactSource.spatialBlend = 0f;
        return director;
    }

    private static void BindLoopingMotion(TimelineAsset timeline, PlayableDirector director, GameObject target, string trackName, AnimationClip clip)
    {
        if (target == null || clip == null)
            return;
        Animator targetAnimator = target.GetComponent<Animator>() ?? target.AddComponent<Animator>();
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.duration = 120d;
        if (motion.asset is AnimationPlayableAsset playableAsset)
            playableAsset.loop = AnimationPlayableAsset.LoopMode.On;
        director.SetGenericBinding(track, targetAnimator);
    }

    private static void BindOneShotMotion(TimelineAsset timeline, PlayableDirector director, GameObject target, string trackName, AnimationClip clip, double start)
    {
        if (target == null || clip == null)
            return;
        Animator targetAnimator = target.GetComponent<Animator>() ?? target.AddComponent<Animator>();
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        TimelineClip motion = track.CreateClip(clip);
        motion.start = start;
        motion.duration = clip.length;
        director.SetGenericBinding(track, targetAnimator);
    }

    private static AnimationClip CreateRattleClip(string name, Transform target, float xAmount, float zAmount, float rotationAmount)
    {
        AnimationClip clip = CreateAnimationAsset(name);
        clip.wrapMode = WrapMode.Loop;
        Vector3 basePosition = target.localPosition;
        float baseYaw = target.localEulerAngles.y;
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.x", new AnimationCurve(
            new Keyframe(0f, basePosition.x), new Keyframe(0.12f, basePosition.x + xAmount), new Keyframe(0.25f, basePosition.x - xAmount * 0.8f),
            new Keyframe(0.42f, basePosition.x + xAmount * 0.55f), new Keyframe(0.62f, basePosition.x - xAmount), new Keyframe(0.82f, basePosition.x + xAmount * 0.35f), new Keyframe(1f, basePosition.x)));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.z", new AnimationCurve(
            new Keyframe(0f, basePosition.z), new Keyframe(0.18f, basePosition.z - zAmount), new Keyframe(0.38f, basePosition.z + zAmount),
            new Keyframe(0.63f, basePosition.z - zAmount * 0.65f), new Keyframe(0.83f, basePosition.z + zAmount * 0.45f), new Keyframe(1f, basePosition.z)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(
            new Keyframe(0f, baseYaw), new Keyframe(0.25f, baseYaw + rotationAmount), new Keyframe(0.52f, baseYaw - rotationAmount), new Keyframe(0.78f, baseYaw + rotationAmount * 0.55f), new Keyframe(1f, baseYaw)));
        return clip;
    }

    private static AnimationClip CreateSwingClip(string name)
    {
        AnimationClip clip = CreateAnimationAsset(name);
        clip.wrapMode = WrapMode.Loop;
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(
            new Keyframe(0f, -2f), new Keyframe(0.5f, 7f), new Keyframe(1.05f, -9f), new Keyframe(1.55f, 6f), new Keyframe(2.1f, -2f)));
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.52f, -10f), new Keyframe(1.08f, 12f), new Keyframe(1.62f, -7f), new Keyframe(2.1f, 0f)));
        return clip;
    }

    private static AnimationClip CreateDoorRattleClip()
    {
        AnimationClip clip = CreateAnimationAsset("Door_Rattle");
        clip.wrapMode = WrapMode.Loop;
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.11f, 4f), new Keyframe(0.22f, -3f), new Keyframe(0.34f, 5f),
            new Keyframe(0.48f, -4f), new Keyframe(0.7f, 2f), new Keyframe(1f, 0f)));
        return clip;
    }

    private static AnimationClip CreateFallClip(string name, float endAngle, float duration)
    {
        AnimationClip clip = CreateAnimationAsset(name);
        clip.wrapMode = WrapMode.ClampForever;
        clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(duration * 0.28f, -Mathf.Sign(endAngle) * 2.5f),
            new Keyframe(duration * 0.48f, endAngle * 0.18f), new Keyframe(duration * 0.76f, endAngle * 0.82f),
            new Keyframe(duration, endAngle)));
        return clip;
    }

    private static AnimationClip CreateAnimationAsset(string name)
    {
        string path = TimelineRoot + "/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static TimelineAsset CreateTimelineAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<TimelineAsset>(path) != null)
            AssetDatabase.DeleteAsset(path);
        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    private static AnimationClip CreateFlickerClip()
    {
        string path = TimelineRoot + "/QuakeLightFlicker.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AnimationClip clip = new AnimationClip { name = "QuakeLightFlicker", frameRate = 30f, wrapMode = WrapMode.Loop };
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 1.25f), new Keyframe(0.18f, 0.25f), new Keyframe(0.32f, 1.5f),
            new Keyframe(0.58f, 0.55f), new Keyframe(0.76f, 1.1f), new Keyframe(1.05f, 0.18f),
            new Keyframe(1.28f, 1.35f), new Keyframe(1.7f, 0.65f), new Keyframe(2f, 1.25f));
        clip.SetCurve(string.Empty, typeof(Light), "m_Intensity", curve);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void CreateDustParticles(Transform parent, Material material, string name, Vector3 position, Vector3 boxScale, float rate, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = loop;
        main.duration = loop ? 6f : 1.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.65f, 0.53f, 0.48f));
        main.gravityModifier = 0.08f;
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        if (!loop)
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxScale;
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void BuildLighting(Transform parent, VolumeProfile profile, out Light roomLight)
    {
        GameObject lights = new GameObject("Lighting");
        lights.transform.SetParent(parent);
        GameObject sunObject = new GameObject("SoftWindowLight");
        sunObject.transform.SetParent(lights.transform);
        sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.78f;
        sun.color = new Color(1f, 0.91f, 0.78f);
        sun.shadows = LightShadows.Soft;

        GameObject roomObject = new GameObject("WarmRoomLight");
        roomObject.transform.SetParent(lights.transform);
        roomObject.transform.position = new Vector3(0f, 2.8f, 0f);
        roomLight = roomObject.AddComponent<Light>();
        roomLight.type = LightType.Point;
        roomLight.range = 10f;
        roomLight.intensity = 1.35f;
        roomLight.color = new Color(1f, 0.72f, 0.48f);
        // Tek bir point light altı gölge yüzü üretip mobil URP gölge atlasını
        // küçültüyordu. Yumuşak yönlü ışık ana gölgeyi taşır; bu yalnızca dolgu ışığıdır.
        roomLight.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("StoryGlobalVolume");
        volumeObject.transform.SetParent(lights.transform);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static void BuildAmbientAudio(Transform parent)
    {
        GameObject audio = new GameObject("AmbientAudio");
        audio.transform.SetParent(parent);
        AudioSource source = audio.AddComponent<AudioSource>();
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/calm_home.wav");
        source.loop = true;
        source.playOnAwake = true;
        source.volume = 0.16f;
        source.spatialBlend = 0f;
    }

    private static GameObject BuildPlayerFlashlight(Transform player)
    {
        GameObject root = new GameObject("BagFlashlight_Result");
        root.transform.SetParent(player);
        root.transform.localPosition = new Vector3(0f, 0.75f, 0.2f);
        root.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        Light light = root.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 9f;
        light.spotAngle = 48f;
        light.innerSpotAngle = 25f;
        light.intensity = 3.2f;
        light.color = new Color(1f, 0.93f, 0.75f);
        light.shadows = LightShadows.Soft;
        root.SetActive(false);
        return root;
    }

    private static GameObject BuildEmergencyRouteLights(Transform parent, Materials m)
    {
        GameObject root = new GameObject("EmergencyRouteLights_Alternative");
        root.transform.SetParent(parent);
        for (int i = 0; i < 5; i++)
        {
            Vector3 lampCenter = new Vector3(1.2f + (i % 2) * 2.6f, 0.28f, 6.75f + i * 1.35f);
            GameObject lamp = StoryChapterBuilderCommon.InstantiateAsset(
                CuteFurnitureRoot + "/Decorations/Light_05.prefab", "EmergencyLamp_" + i, root.transform,
                lampCenter - Vector3.up * 0.16f, new Vector3(0.34f, 0.34f, 0.34f), Vector3.zero,
                false, false, m.coral);
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.2f;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.24f, 0.16f);
        }
        root.SetActive(false);
        return root;
    }

    private static void ConfigureSequence(StorySequenceDirector sequence, StoryGameManager manager, StoryPlayerMovement movement,
        StorySiblingFollower siblingFollower, StoryTouchManager touch, StoryCameraController camera, StoryUIController ui,
        Animator denizAnimator, Animator canAnimator, PlayableDirector quakeTimeline,
        CinemachineImpulseSource impulse, AudioSource impact, Light roomLight, InteractionReferences interactions,
        WorldReferences world, GameObject flashlight, GameObject emergencyLights, GameObject canComfortItem = null)
    {
        SerializedObject serialized = new SerializedObject(sequence);
        Set(serialized, "gameManager", manager);
        Set(serialized, "player", movement);
        Set(serialized, "siblingFollower", siblingFollower);
        Set(serialized, "touchManager", touch);
        Set(serialized, "cameraController", camera);
        Set(serialized, "ui", ui);
        Set(serialized, "denizAnimator", denizAnimator);
        Set(serialized, "canAnimator", canAnimator);
        Set(serialized, "earthquakeTimeline", quakeTimeline);
        Set(serialized, "impulseSource", impulse);
        Set(serialized, "impactSource", impact);
        Set(serialized, "roomLight", roomLight);
        SetArray(serialized, "introInspections", interactions.intro.Cast<Object>().ToArray());
        SetArray(
            serialized,
            "introOptionalMoments",
            interactions.introOptional?.Cast<Object>().ToArray() ?? Array.Empty<Object>());
        Set(serialized, "calmSibling", interactions.calmSibling);
        Set(serialized, "crouchStep", interactions.crouchStep);
        Set(serialized, "coverHeadStep", interactions.coverHeadStep);
        Set(serialized, "safeCover", interactions.safeCover);
        Set(serialized, "unsafeDoor", interactions.unsafeDoor);
        Set(serialized, "unsafeWindow", interactions.unsafeWindow);
        Set(serialized, "quakeMovementCenter", world.tableFocus);
        SetBeats(serialized, "postQuakeBeats", interactions.postQuakeBeats);
        Set(serialized, "lightWithFlashlight", interactions.lightWithFlashlight);
        Set(serialized, "lightWithoutFlashlight", interactions.lightWithoutFlashlight);
        Set(serialized, "corridorExit", interactions.exit);
        Set(serialized, "brokenGlassHazard", interactions.brokenGlassHazard);
        Set(serialized, "postQuakeSafeReturn", world.tableFocus);
        SetBeats(serialized, "corridorBeats", interactions.corridorBeats);
        Set(serialized, "wardrobeSecured", world.wardrobeSecured);
        Set(serialized, "wardrobeUnsecured", world.wardrobeUnsecured);
        Set(serialized, "wardrobeFallen", world.wardrobeFallen);
        Set(serialized, "shelfStable", world.shelfStable);
        Set(serialized, "shelfUnsecured", world.shelfUnsecured);
        Set(serialized, "shelfFallen", world.shelfFallen);
        Set(serialized, "clearExitRoute", world.clearExit);
        Set(serialized, "clutteredExitRoute", world.clutteredExit);
        Set(serialized, "playerFlashlight", flashlight);
        Set(serialized, "emergencyRouteLights", emergencyLights);
        Set(serialized, "closedDoor", world.closedDoor);
        Set(serialized, "openDoor", world.openDoor);
        Set(serialized, "leftShoeWorld", world.leftShoeWorld);
        Set(serialized, "rightShoeWorld", world.rightShoeWorld);
        Set(serialized, "canShoesWorld", world.canShoesWorld);
        Set(serialized, "emergencyBagWorld", world.emergencyBagWorld);
        Set(serialized, "brokenGlassVisual", world.brokenGlassVisual);
        Set(serialized, "denizWornShoes", world.denizWornShoes);
        Set(serialized, "denizWornBag", world.denizWornBag);
        Set(serialized, "canWornShoes", world.canWornShoes);
        Set(serialized, "canComfortItem", canComfortItem);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBeats(SerializedObject serialized, string propertyName, BeatDefinition[] beats)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " serialized alanı bulunamadı.");

        property.arraySize = beats?.Length ?? 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            BeatDefinition beat = beats[i];
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("interactable").objectReferenceValue = beat.interactable;
            element.FindPropertyRelative("objectiveTitle").stringValue = beat.title;
            element.FindPropertyRelative("objectiveDetail").stringValue = beat.detail;
            element.FindPropertyRelative("completionSubtitle").stringValue = beat.subtitle;
            element.FindPropertyRelative("delayAfter").floatValue = beat.delayAfter;
        }
    }

    private static void BuildNavigation(GameObject environment)
    {
        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = environment.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    private static void SaveReusablePrefabs(WorldReferences world, GameObject deniz, GameObject can)
    {
        PrefabUtility.SaveAsPrefabAsset(world.wardrobeSecured, PrefabRoot + "/Wardrobe_Secured.prefab");
        PrefabUtility.SaveAsPrefabAsset(world.wardrobeFallen, PrefabRoot + "/Wardrobe_Damaged.prefab");
        PrefabUtility.SaveAsPrefabAsset(world.shelfStable, PrefabRoot + "/Shelf_Stable.prefab");
        PrefabUtility.SaveAsPrefabAsset(world.shelfFallen, PrefabRoot + "/Shelf_Damaged.prefab");
        PrefabUtility.SaveAsPrefabAsset(deniz, PrefabRoot + "/Deniz_12.prefab");
        PrefabUtility.SaveAsPrefabAsset(can, PrefabRoot + "/Can_8.prefab");
    }

    private static void AddSceneToBuildSettingsWithoutChangingStartupScene()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static void CreateAudioAssets()
    {
        WriteWave(AudioRoot + "/calm_home.wav", 18f, 22050, (time, random) =>
        {
            float hum = Mathf.Sin(time * Mathf.PI * 2f * 52f) * 0.008f;
            float outside = Mathf.Sin(time * Mathf.PI * 2f * 0.18f) * 0.018f;
            return hum + outside + ((float)random.NextDouble() * 2f - 1f) * 0.004f;
        });
        WriteWave(AudioRoot + "/quake_rumble.wav", 12f, 22050, (time, random) =>
        {
            float low = Mathf.Sin(time * Mathf.PI * 2f * 34f) * 0.21f + Mathf.Sin(time * Mathf.PI * 2f * 51f) * 0.11f;
            float pulse = 0.62f + Mathf.Sin(time * Mathf.PI * 2f * 1.7f) * 0.22f;
            float noise = ((float)random.NextDouble() * 2f - 1f) * 0.07f;
            return (low + noise) * pulse;
        });
        WriteWave(AudioRoot + "/quake_impact.wav", 1.25f, 22050, (time, random) =>
        {
            float decay = Mathf.Exp(-time * 4.2f);
            float thud = Mathf.Sin(time * Mathf.PI * 2f * 47f) * 0.7f;
            float crack = ((float)random.NextDouble() * 2f - 1f) * 0.38f;
            return (thud + crack) * decay;
        });
        AssetDatabase.Refresh();
    }

    private static void WriteWave(string assetPath, float duration, int sampleRate, Func<float, System.Random, float> sampleGenerator)
    {
        string fullPath = Path.GetFullPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        System.Random random = new System.Random(assetPath.GetHashCode());
        using FileStream stream = File.Create(fullPath);
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + sampleCount * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(sampleCount * 2);
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = Mathf.Clamp(sampleGenerator(i / (float)sampleRate, random), -1f, 1f);
            writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
        }
    }

    private static Material interactionKindMaterial(StoryInteractionKind kind, Color? ringColor, Materials m)
    {
        if (kind == StoryInteractionKind.UnsafeChoice || (ringColor.HasValue && ringColor.Value == Coral))
            return m.hazardGlow;
        if (kind == StoryInteractionKind.HelpSibling || kind == StoryInteractionKind.Collect)
            return m.helpGlow;
        return m.safeGlow;
    }

    private static GameObject InstantiateFurniturePrefab(string relativePath, string name, Transform parent,
        Vector3 localFeetPosition, Vector3 localEulerAngles, Vector3 targetSize)
    {
        string path = CuteFurnitureRoot + "/" + relativePath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Oda mobilyası bulunamadı: " + path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        instance.transform.localScale = Vector3.one;

        if (!TryGetCombinedRendererBounds(instance, out Bounds bounds))
            throw new InvalidOperationException(name + " görsel sınırı hesaplanamadı.");

        float scale = float.PositiveInfinity;
        if (targetSize.x > 0.001f && bounds.size.x > 0.001f)
            scale = Mathf.Min(scale, targetSize.x / bounds.size.x);
        if (targetSize.y > 0.001f && bounds.size.y > 0.001f)
            scale = Mathf.Min(scale, targetSize.y / bounds.size.y);
        if (targetSize.z > 0.001f && bounds.size.z > 0.001f)
            scale = Mathf.Min(scale, targetSize.z / bounds.size.z);
        if (float.IsInfinity(scale) || scale <= 0f)
            scale = 1f;
        instance.transform.localScale *= scale;

        TryGetCombinedRendererBounds(instance, out bounds);
        Vector3 desiredFeet = parent.TransformPoint(localFeetPosition);
        instance.transform.position += desiredFeet - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return instance;
    }

    private static bool TryGetCombinedRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return true;
    }

    private static GameObject CreateInvisibleColliderPrimitive(string name, Vector3 position, Vector3 scale,
        Transform parent, bool local = false)
    {
        GameObject colliderObject = CreatePrimitive(name, PrimitiveType.Cube, position, scale, null, parent, true, local);
        Renderer renderer = colliderObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
        return colliderObject;
    }

    private static void CreateSafetyStrap(Transform wardrobe, float localX, Materials m)
    {
        StoryAuthoredPropFactory.CreateMetalBracket("SafetyStrap", wardrobe,
            wardrobe.TransformPoint(new Vector3(localX, 2.25f, 0.34f)),
            new Vector3(0.2f, 0.42f, 0.34f), wardrobe.eulerAngles, m.navy, m.teal, false);
    }

    private static void BuildRoomWallDecor(Transform parent, Materials m)
    {
        GameObject decor = new GameObject("RoomWallDecor");
        decor.transform.SetParent(parent);

        GameObject artFrameLarge = StoryChapterBuilderCommon.InstantiateAsset(
            CuteFurnitureRoot + "/Decorations/Picture_21.prefab", "ArtFrameLarge", decor.transform,
            new Vector3(4.18f, 1.65f, 5.62f), new Vector3(0.92f, 0.86f, 0.16f), Vector3.zero,
            false, false);
        artFrameLarge.transform.SetPositionAndRotation(
            new Vector3(4.179999f, 2.08f, 5.898f),
            Quaternion.Euler(0f, 180f, 0f));
        artFrameLarge.transform.localScale = Vector3.one * 1.83469164f;

        GameObject smallFrame = StoryChapterBuilderCommon.InstantiateAsset(
            CuteFurnitureRoot + "/Decorations/Picture_08.prefab", "SmallFrame", decor.transform,
            new Vector3(-4.78f, 1.5f, -0.05f), new Vector3(0.16f, 0.84f, 0.72f),
            new Vector3(0f, 90f, 0f), false, false);
        smallFrame.transform.SetPositionAndRotation(
            new Vector3(-4.85f, 1.91999984f, -2.484f),
            Quaternion.Euler(0f, 90f, 0f));
        smallFrame.transform.localScale = Vector3.one * 1.038648f;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent, bool collider = true, bool local = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        if (local)
            go.transform.localPosition = position;
        else
            go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
        Collider component = go.GetComponent<Collider>();
        if (!collider && component != null)
            Object.DestroyImmediate(component);
        return go;
    }

    private static Transform CreateFocus(string name, Vector3 position, Transform parent)
    {
        GameObject focus = new GameObject(name);
        focus.transform.SetParent(parent);
        focus.transform.position = position;
        focus.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        return focus.transform;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static Quaternion LookAt(Vector3 position, Vector3 target)
    {
        return Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }

    private static GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = (anchorMin + anchorMax) * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color, bool blocksRaycasts)
    {
        GameObject panel = CreateUIRect(name, parent, anchorMin, anchorMax, position, size);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksRaycasts;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject go = CreateUIRect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, dimensions);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchor,
        Vector2 position, Vector2 size, Color background, Color foreground)
    {
        GameObject go = CreatePanel(name, parent, anchor, anchor, position, size, background, true);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ConfigureSelectableColors(button);
        TMP_Text text = CreateText("Label", go.transform, font, 27f, foreground, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(24f, 18f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 27f;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.text = label;
        return button;
    }

    private static Toggle CreateLabeledToggle(string name, Transform parent, string label, TMP_FontAsset font,
        Vector2 position, Color accent, out TMP_Text stateText)
    {
        GameObject root = CreatePanel(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position,
            new Vector2(650f, 108f), new Color(1f, 1f, 1f, 0.065f), true);
        Image background = root.GetComponent<Image>();
        TMP_Text title = CreateText("Label", root.transform, font, 28f, Color.white, TextAlignmentOptions.Left,
            new Vector2(-120f, 0f), new Vector2(350f, 60f));
        title.text = label;

        GameObject track = CreatePanel("Track", root.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(225f, 0f), new Vector2(150f, 64f), new Color(1f, 1f, 1f, 0.16f), false);
        GameObject checkObject = CreatePanel("Checkmark", track.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(136f, 50f), accent, false);
        Image check = checkObject.GetComponent<Image>();
        check.color = accent;
        check.raycastTarget = false;

        stateText = CreateText("State", root.transform, font, 21f, Cream, TextAlignmentOptions.Center,
            new Vector2(225f, 0f), new Vector2(126f, 44f));
        stateText.text = "KAPALI";

        Toggle toggle = root.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = check;
        ConfigureSelectableColors(toggle);
        return toggle;
    }

    private static void ConfigureSelectableColors(Selectable selectable)
    {
        ColorBlock colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.86f, 1f);
        colors.selectedColor = new Color(0.95f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.45f, 0.48f, 0.5f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        selectable.colors = colors;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} bulunamadı.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(propertyName + " serialized alanı bulunamadı.");
        property.objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
