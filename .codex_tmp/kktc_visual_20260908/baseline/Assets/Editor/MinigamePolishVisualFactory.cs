using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Editor-only final art-pass helpers for the minigame package. These helpers deliberately
/// author composed scene objects and reuse existing project assets; they add no runtime code.
/// </summary>
public static partial class MinigamePolishedSceneBuilder
{
    private static Material ScopedMaterial(
        SceneContext context,
        string suffix,
        Color color,
        float smoothness,
        bool emission = false)
    {
        return GetOrCreateMaterial(
            "Minigame_" + Sanitize(context.id) + "_" + suffix,
            color,
            smoothness,
            emission);
    }

    private static GameObject CreateLocalPrimitive(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3 localEuler = default,
        bool collider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider existing = go.GetComponent<Collider>();
        if (!collider && existing != null)
            Object.DestroyImmediate(existing);
        return go;
    }

    private static GameObject CreateAssemblyRoot(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(euler);
        return root;
    }

    private static void CreateLocalWorldText(
        Transform parent,
        string name,
        string text,
        Vector3 localPosition,
        Vector3 localEuler,
        Vector2 size,
        float fontSize,
        Color color)
    {
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(
            out TMP_FontAsset regular,
            out TMP_FontAsset semibold,
            out TMP_FontAsset bold);
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.Euler(localEuler);
        TextMeshPro label = textObject.AddComponent<TextMeshPro>();
        label.font = bold != null ? bold : semibold != null ? semibold : regular;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableAutoSizing = false;
        label.rectTransform.sizeDelta = size;
    }

    private static void FaceCharacterToCamera(
        GameObject character,
        Camera camera,
        float yawOffset = 0f)
    {
        if (character == null || camera == null)
            return;
        Vector3 direction = camera.transform.position - character.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;
        character.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) *
                                       Quaternion.Euler(0f, yawOffset, 0f);
    }

    private static GameObject CreateWorldLabelPlate(
        SceneContext context,
        Transform parent,
        string name,
        string label,
        Vector3 position,
        Material accent,
        float width = 1.55f)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        root.transform.rotation = Quaternion.LookRotation(
            root.transform.position - context.camera.transform.position,
            Vector3.up);
        CreateLocalPrimitive(root.transform, "LabelBack", PrimitiveType.Cube,
            Vector3.zero, new Vector3(width, 0.38f, 0.07f), context.materials.navy);
        CreateLocalPrimitive(root.transform, "LabelAccent", PrimitiveType.Cube,
            new Vector3(0f, -0.15f, -0.052f), new Vector3(width - 0.14f, 0.055f, 0.025f), accent);
        CreateLocalWorldText(root.transform, "LabelText", label,
            new Vector3(0f, 0.025f, -0.052f), Vector3.zero,
            new Vector2(width - 0.12f, 0.31f), 1.45f, context.materials.cream.color);
        return root;
    }

    private static GameObject CreatePreparationMat(
        SceneContext context,
        Transform parent,
        string name,
        string label,
        Vector3 position,
        Vector3 size,
        Material accent,
        float yaw = 0f)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, new Vector3(0f, yaw, 0f));
        Material mat = ScopedMaterial(context, "WorkMat", new Color32(28, 46, 55, 255), 0.24f);
        CreateLocalPrimitive(root.transform, "MatSurface", PrimitiveType.Cube,
            Vector3.zero, new Vector3(size.x, 0.045f, size.z), mat);
        CreateLocalPrimitive(root.transform, "BorderNear", PrimitiveType.Cube,
            new Vector3(0f, 0.032f, -size.z * 0.5f), new Vector3(size.x, 0.025f, 0.045f), accent);
        CreateLocalPrimitive(root.transform, "BorderFar", PrimitiveType.Cube,
            new Vector3(0f, 0.032f, size.z * 0.5f), new Vector3(size.x, 0.025f, 0.045f), accent);
        CreateLocalPrimitive(root.transform, "BorderLeft", PrimitiveType.Cube,
            new Vector3(-size.x * 0.5f, 0.032f, 0f), new Vector3(0.045f, 0.025f, size.z), accent);
        CreateLocalPrimitive(root.transform, "BorderRight", PrimitiveType.Cube,
            new Vector3(size.x * 0.5f, 0.032f, 0f), new Vector3(0.045f, 0.025f, size.z), accent);
        if (!string.IsNullOrWhiteSpace(label))
        {
            CreateWorldLabelPlate(
                context,
                root.transform,
                "MatLabel",
                label,
                root.transform.TransformPoint(new Vector3(0f, 0.25f, size.z * 0.48f)),
                accent,
                Mathf.Clamp(size.x * 0.72f, 1.25f, 2.8f));
        }
        return root;
    }

    private static GameObject CreateExitSafetyLane(
        SceneContext context,
        Transform parent,
        Vector3 position,
        Vector3 size)
    {
        GameObject root = CreatePreparationMat(
            context,
            parent,
            "ExitSafetyLane",
            string.Empty,
            position,
            size,
            context.materials.dark);
        return root;
    }

    private static GameObject CreateBedroomExitDoor(
        SceneContext context,
        Transform parent,
        Vector3 position)
    {
        GameObject root = CreateAssemblyRoot(parent, "BedroomExitDoor", position, Vector3.zero);
        Material door = ScopedMaterial(context, "BedroomDoor", new Color32(221, 213, 190, 255), 0.3f);
        Material inset = ScopedMaterial(context, "BedroomDoorInset", new Color32(178, 203, 201, 255), 0.24f);
        Material frame = ScopedMaterial(context, "BedroomDoorFrame", new Color32(64, 76, 78, 255), 0.38f);
        Material metal = ScopedMaterial(context, "BedroomDoorHardware", new Color32(156, 151, 133, 255), 0.68f);

        CreateLocalPrimitive(root.transform, "DoorSlab", PrimitiveType.Cube,
            new Vector3(0f, 1.16f, 0f), new Vector3(1.42f, 2.32f, 0.12f), door);
        CreateLocalPrimitive(root.transform, "UpperInset", PrimitiveType.Cube,
            new Vector3(0f, 1.64f, -0.075f), new Vector3(1.05f, 0.72f, 0.035f), inset);
        CreateLocalPrimitive(root.transform, "LowerInset", PrimitiveType.Cube,
            new Vector3(0f, 0.67f, -0.075f), new Vector3(1.05f, 0.78f, 0.035f), inset);
        CreateLocalPrimitive(root.transform, "FrameLeft", PrimitiveType.Cube,
            new Vector3(-0.79f, 1.22f, -0.02f), new Vector3(0.16f, 2.55f, 0.19f), frame);
        CreateLocalPrimitive(root.transform, "FrameRight", PrimitiveType.Cube,
            new Vector3(0.79f, 1.22f, -0.02f), new Vector3(0.16f, 2.55f, 0.19f), frame);
        CreateLocalPrimitive(root.transform, "FrameTop", PrimitiveType.Cube,
            new Vector3(0f, 2.46f, -0.02f), new Vector3(1.74f, 0.16f, 0.19f), frame);
        CreateLocalPrimitive(root.transform, "Handle", PrimitiveType.Sphere,
            new Vector3(0.49f, 1.13f, -0.13f), Vector3.one * 0.12f, metal);
        CreateLocalPrimitive(root.transform, "Threshold", PrimitiveType.Cube,
            new Vector3(0f, 0.04f, -0.18f), new Vector3(1.7f, 0.08f, 0.42f), frame);
        CreateWorldLabelPlate(
            context,
            root.transform,
            "ExitDoorPlaque",
            "ÇIKIŞ",
            root.transform.TransformPoint(new Vector3(0f, 2.76f, -0.02f)),
            context.materials.amber,
            1.08f);
        return root;
    }

    private static GameObject CreateBedroomRunner(
        SceneContext context,
        Transform parent,
        Vector3 position,
        Vector3 size)
    {
        GameObject root = CreateAssemblyRoot(parent, "ExitRunner", position, Vector3.zero);
        Material textile = ScopedMaterial(context, "ExitRunnerTextile", new Color32(100, 151, 151, 255), 0.12f);
        Material border = ScopedMaterial(context, "ExitRunnerBorder", new Color32(225, 211, 177, 255), 0.16f);
        Material weave = ScopedMaterial(context, "ExitRunnerWeave", new Color32(75, 125, 130, 255), 0.12f);
        CreateLocalPrimitive(root.transform, "RunnerTextile", PrimitiveType.Cube,
            Vector3.zero, new Vector3(size.x, 0.035f, size.z), textile);
        CreateLocalPrimitive(root.transform, "RunnerEdgeLeft", PrimitiveType.Cube,
            new Vector3(-size.x * 0.47f, 0.023f, 0f), new Vector3(0.055f, 0.02f, size.z * 0.96f), border);
        CreateLocalPrimitive(root.transform, "RunnerEdgeRight", PrimitiveType.Cube,
            new Vector3(size.x * 0.47f, 0.023f, 0f), new Vector3(0.055f, 0.02f, size.z * 0.96f), border);
        for (int band = -3; band <= 3; band++)
        {
            CreateLocalPrimitive(root.transform, "WovenBand_" + band, PrimitiveType.Cube,
                new Vector3(0f, 0.024f, band * size.z / 8f),
                new Vector3(size.x * 0.82f, 0.012f, 0.045f), weave);
        }
        return root;
    }

    private static GameObject CreateLowSafetyCabinet(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        float width)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material wood = ScopedMaterial(context, "LowCabinetWood", new Color32(118, 82, 58, 255), 0.34f);
        Material face = ScopedMaterial(context, "LowCabinetFace", new Color32(218, 207, 177, 255), 0.26f);
        Material recess = ScopedMaterial(context, "LowCabinetRecess", new Color32(72, 91, 94, 255), 0.22f);
        Material hardware = ScopedMaterial(context, "LowCabinetHardware", new Color32(185, 143, 54, 255), 0.62f);
        float sectionWidth = (width - 0.22f) / 3f;

        CreateLocalPrimitive(root.transform, "CabinetBody", PrimitiveType.Cube,
            new Vector3(0f, 0.46f, 0f), new Vector3(width, 0.84f, 0.72f), wood);
        CreateLocalPrimitive(root.transform, "CabinetTop", PrimitiveType.Cube,
            new Vector3(0f, 0.91f, -0.02f), new Vector3(width + 0.12f, 0.12f, 0.8f), face);
        CreateLocalPrimitive(root.transform, "CabinetPlinth", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, 0.04f), new Vector3(width * 0.9f, 0.16f, 0.62f), recess);
        for (int section = 0; section < 3; section++)
        {
            float x = -width * 0.5f + 0.11f + sectionWidth * (section + 0.5f);
            CreateLocalPrimitive(root.transform, "DrawerFace_" + section, PrimitiveType.Cube,
                new Vector3(x, 0.5f, -0.39f), new Vector3(sectionWidth - 0.08f, 0.58f, 0.045f), face);
            CreateLocalPrimitive(root.transform, "DrawerRecess_" + section, PrimitiveType.Cube,
                new Vector3(x, 0.5f, -0.42f), new Vector3(sectionWidth - 0.24f, 0.38f, 0.025f), recess);
            CreateLocalPrimitive(root.transform, "DrawerPull_" + section, PrimitiveType.Cube,
                new Vector3(x, 0.57f, -0.45f), new Vector3(0.24f, 0.055f, 0.035f), hardware);
        }
        return root;
    }

    private static GameObject CreateCompactWorkshopCart(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 size,
        Material accent)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material frame = ScopedMaterial(context, "WorkshopCartFrame", new Color32(56, 68, 71, 255), 0.58f);
        Material top = ScopedMaterial(context, "WorkshopCartTop", new Color32(130, 91, 63, 255), 0.34f);
        Material shelf = ScopedMaterial(context, "WorkshopCartShelf", new Color32(92, 111, 113, 255), 0.36f);
        float halfX = size.x * 0.5f - 0.12f;
        float halfZ = size.z * 0.5f - 0.12f;
        float topY = size.y;

        CreateLocalPrimitive(root.transform, "WoodTop", PrimitiveType.Cube,
            new Vector3(0f, topY, 0f), new Vector3(size.x, 0.12f, size.z), top);
        CreateLocalPrimitive(root.transform, "LowerShelf", PrimitiveType.Cube,
            new Vector3(0f, 0.29f, 0f), new Vector3(size.x * 0.86f, 0.08f, size.z * 0.78f), shelf);
        Vector3[] legs =
        {
            new Vector3(-halfX, topY * 0.52f, -halfZ),
            new Vector3(halfX, topY * 0.52f, -halfZ),
            new Vector3(-halfX, topY * 0.52f, halfZ),
            new Vector3(halfX, topY * 0.52f, halfZ)
        };
        for (int index = 0; index < legs.Length; index++)
        {
            CreateLocalPrimitive(root.transform, "CartLeg_" + index, PrimitiveType.Cube,
                legs[index], new Vector3(0.1f, topY * 0.88f, 0.1f), frame);
            CreateLocalPrimitive(root.transform, "Caster_" + index, PrimitiveType.Sphere,
                new Vector3(legs[index].x, 0.09f, legs[index].z), Vector3.one * 0.15f, frame);
        }
        CreateLocalPrimitive(root.transform, "FrontAccent", PrimitiveType.Cube,
            new Vector3(0f, topY - 0.08f, -size.z * 0.52f),
            new Vector3(size.x * 0.72f, 0.055f, 0.035f), accent);
        return root;
    }

    private static GameObject CreateDrillPilotMarker(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Material accent)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material plate = ScopedMaterial(context, "DrillPilotPlate", new Color32(86, 101, 105, 255), 0.62f);
        Material recess = ScopedMaterial(context, "DrillPilotRecess", new Color32(38, 52, 57, 255), 0.48f);

        CreateLocalPrimitive(root.transform, "PilotPlate", PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(0.24f, 0.035f, 0.24f), plate, new Vector3(90f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "PilotRecess", PrimitiveType.Cylinder,
            new Vector3(0f, 0f, -0.052f), new Vector3(0.11f, 0.025f, 0.11f), recess,
            new Vector3(90f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "PilotCrossHorizontal", PrimitiveType.Cube,
            new Vector3(0f, 0f, -0.085f), new Vector3(0.2f, 0.04f, 0.028f), accent);
        CreateLocalPrimitive(root.transform, "PilotCrossVertical", PrimitiveType.Cube,
            new Vector3(0f, 0f, -0.085f), new Vector3(0.04f, 0.2f, 0.028f), accent);

        BoxCollider collider = AddBoundsCollider(root) as BoxCollider;
        if (collider != null)
            collider.size = new Vector3(0.58f, 0.58f, 0.32f);
        return root;
    }

    private static GameObject CreateDrilledAnchorHardware(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        float width,
        Material accent)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material rail = ScopedMaterial(context, "DrilledAnchorRail", new Color32(104, 118, 122, 255), 0.72f);
        Material plate = ScopedMaterial(context, "DrilledAnchorPlate", new Color32(137, 146, 149, 255), 0.68f);
        Material bolt = ScopedMaterial(context, "DrilledAnchorBolt", new Color32(43, 55, 59, 255), 0.62f);

        CreateLocalPrimitive(root.transform, "LoadRail", PrimitiveType.Cube,
            Vector3.zero, new Vector3(width, 0.14f, 0.12f), rail);
        float plateOffset = width * 0.32f;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = plateOffset * side;
            string suffix = side < 0 ? "Left" : "Right";
            CreateLocalPrimitive(root.transform, "WallPlate_" + suffix, PrimitiveType.Cube,
                new Vector3(x, 0f, -0.065f), new Vector3(0.34f, 0.44f, 0.075f), plate);
            CreateLocalPrimitive(root.transform, "FurnitureClamp_" + suffix, PrimitiveType.Cube,
                new Vector3(x, -0.22f, -0.11f), new Vector3(0.17f, 0.34f, 0.09f), rail);
            CreateLocalPrimitive(root.transform, "SafetyMark_" + suffix, PrimitiveType.Cube,
                new Vector3(x, 0.13f, -0.115f), new Vector3(0.19f, 0.055f, 0.025f), accent);

            Vector3[] boltPoints =
            {
                new Vector3(x - 0.1f, 0.12f, -0.12f),
                new Vector3(x + 0.1f, 0.12f, -0.12f),
                new Vector3(x - 0.1f, -0.12f, -0.12f),
                new Vector3(x + 0.1f, -0.12f, -0.12f)
            };
            for (int index = 0; index < boltPoints.Length; index++)
                CreateLocalPrimitive(root.transform, suffix + "Bolt_" + index, PrimitiveType.Sphere,
                    boltPoints[index], Vector3.one * 0.047f, bolt);
        }
        return root;
    }

    private static GameObject CreateAnchoringRailAssembly(
        SceneContext context,
        Transform parent,
        string name,
        string label,
        Vector3 position,
        float width,
        Material accent,
        Vector3 labelPosition)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material rail = ScopedMaterial(context, "AnchorRail", new Color32(102, 117, 122, 255), 0.72f);
        Material webbing = ScopedMaterial(context, "AnchorWebbing", new Color32(43, 66, 72, 255), 0.28f);
        CreateLocalPrimitive(root.transform, "StructuralRail", PrimitiveType.Cube,
            Vector3.zero, new Vector3(width, 0.13f, 0.11f), rail);
        for (int index = 0; index < 5; index++)
        {
            float x = Mathf.Lerp(-width * 0.42f, width * 0.42f, index / 4f);
            CreateLocalPrimitive(root.transform, "RailBolt_" + index, PrimitiveType.Sphere,
                new Vector3(x, 0f, -0.082f), Vector3.one * 0.052f, context.materials.dark);
        }
        CreateLocalPrimitive(root.transform, "WebbingLeft", PrimitiveType.Cube,
            new Vector3(-width * 0.32f, -0.25f, -0.1f), new Vector3(0.11f, 0.42f, 0.07f), webbing,
            new Vector3(0f, 0f, -8f));
        CreateLocalPrimitive(root.transform, "WebbingRight", PrimitiveType.Cube,
            new Vector3(width * 0.32f, -0.25f, -0.1f), new Vector3(0.11f, 0.42f, 0.07f), webbing,
            new Vector3(0f, 0f, 8f));
        if (!string.IsNullOrWhiteSpace(label))
        {
            CreateWorldLabelPlate(context, root.transform, "RailLabel", label,
                labelPosition, accent, Mathf.Clamp(width * 0.75f, 1.45f, 2.4f));
        }
        return root;
    }

    private static void AddPanelFasteners(Transform parent, Material metal, float x, float y, float z)
    {
        Vector3[] points =
        {
            new Vector3(-x, y, z), new Vector3(x, y, z),
            new Vector3(-x, -y, z), new Vector3(x, -y, z)
        };
        for (int index = 0; index < points.Length; index++)
        {
            CreateLocalPrimitive(parent, "Fastener_" + index, PrimitiveType.Sphere,
                points[index], Vector3.one * 0.045f, metal);
        }
    }

    private static GameObject CreateDimmingOverlay(
        SceneContext context,
        string name,
        Color color)
    {
        Canvas hudCanvas = context.uiRoot.GetComponentInChildren<Canvas>(true);
        if (hudCanvas == null)
            throw new InvalidOperationException("Minigame HUD Canvas bulunamadı: " + context.id);

        GameObject overlayCanvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler));
        overlayCanvasObject.transform.SetParent(context.uiRoot, false);
        Canvas overlayCanvas = overlayCanvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = hudCanvas.sortingOrder - 10;
        UnityEngine.UI.CanvasScaler scaler = overlayCanvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject overlay = new GameObject(
            "FullScreenTint",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image));
        overlay.transform.SetParent(overlayCanvasObject.transform, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image image = overlay.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        overlayCanvasObject.SetActive(false);
        return overlayCanvasObject;
    }

    private static void FitCarrierVisualToSize(
        GameObject visual,
        Vector3 feetPosition,
        Vector3 targetSize)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer != null && renderer.enabled)
            .ToArray();
        if (renderers.Length == 0)
        {
            visual.transform.position = feetPosition;
            return;
        }
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        const float epsilon = 0.000001f;
        float scale = Mathf.Min(
            targetSize.x / Mathf.Max(epsilon, bounds.size.x),
            Mathf.Min(
                targetSize.y / Mathf.Max(epsilon, bounds.size.y),
                targetSize.z / Mathf.Max(epsilon, bounds.size.z)));
        if (!float.IsNaN(scale) && !float.IsInfinity(scale) && scale > 0f)
            visual.transform.localScale *= scale;
        bounds = CombinedBounds(visual);
        visual.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static GameObject CreateEmergencyFlashlight(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material grip = ScopedMaterial(context, "FlashlightGrip", new Color32(39, 53, 59, 255), 0.38f);
        Material metal = ScopedMaterial(context, "FlashlightMetal", new Color32(146, 151, 150, 255), 0.72f);
        Material lens = ScopedMaterial(context, "FlashlightLens", new Color32(221, 245, 244, 255), 0.66f);
        CreateLocalPrimitive(root.transform, "Grip", PrimitiveType.Cylinder,
            new Vector3(0f, 0.17f, 0f), new Vector3(0.13f, 0.31f, 0.13f), grip,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "Head", PrimitiveType.Cylinder,
            new Vector3(0.38f, 0.17f, 0f), new Vector3(0.23f, 0.15f, 0.23f), metal,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "Lens", PrimitiveType.Cylinder,
            new Vector3(0.53f, 0.17f, 0f), new Vector3(0.19f, 0.035f, 0.19f), lens,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "Switch", PrimitiveType.Cube,
            new Vector3(-0.03f, 0.31f, 0f), new Vector3(0.16f, 0.07f, 0.1f), context.materials.amber);
        for (int ridge = 0; ridge < 4; ridge++)
        {
            CreateLocalPrimitive(root.transform, "GripRidge_" + ridge, PrimitiveType.Cylinder,
                new Vector3(-0.19f + ridge * 0.12f, 0.17f, 0f), new Vector3(0.145f, 0.018f, 0.145f), metal,
                new Vector3(0f, 0f, 90f));
        }
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateEmergencyRadio(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material body = ScopedMaterial(context, "RadioBody", new Color32(31, 51, 60, 255), 0.42f);
        Material screen = ScopedMaterial(context, "RadioScreen", new Color32(64, 166, 178, 255), 0.48f);
        Material speaker = ScopedMaterial(context, "RadioSpeaker", new Color32(18, 27, 31, 255), 0.26f);
        Material metal = ScopedMaterial(context, "RadioMetal", new Color32(135, 145, 147, 255), 0.65f);
        CreateLocalPrimitive(root.transform, "RadioCase", PrimitiveType.Cube,
            new Vector3(0f, 0.3f, 0f), new Vector3(0.82f, 0.55f, 0.34f), body);
        CreateLocalPrimitive(root.transform, "Display", PrimitiveType.Cube,
            new Vector3(-0.18f, 0.39f, -0.19f), new Vector3(0.34f, 0.16f, 0.045f), screen);
        CreateLocalPrimitive(root.transform, "Speaker", PrimitiveType.Cylinder,
            new Vector3(0.21f, 0.27f, -0.205f), new Vector3(0.2f, 0.035f, 0.2f), speaker,
            new Vector3(90f, 0f, 0f));
        for (int slot = -2; slot <= 2; slot++)
        {
            CreateLocalPrimitive(root.transform, "SpeakerSlot_" + slot, PrimitiveType.Cube,
                new Vector3(0.21f + slot * 0.055f, 0.27f, -0.245f),
                new Vector3(0.018f, 0.25f, 0.018f), metal);
        }
        CreateLocalPrimitive(root.transform, "TuningKnob", PrimitiveType.Cylinder,
            new Vector3(-0.27f, 0.2f, -0.21f), new Vector3(0.09f, 0.035f, 0.09f), context.materials.amber,
            new Vector3(90f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "Antenna", PrimitiveType.Cylinder,
            new Vector3(0.31f, 0.83f, 0f), new Vector3(0.025f, 0.35f, 0.025f), metal,
            new Vector3(0f, 0f, -10f));
        CreateLocalPrimitive(root.transform, "CarryHandle", PrimitiveType.Cube,
            new Vector3(-0.08f, 0.67f, 0f), new Vector3(0.48f, 0.07f, 0.08f), metal);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateBatteryPack(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material casing = ScopedMaterial(context, "BatteryCasing", new Color32(38, 51, 58, 255), 0.34f);
        Material terminal = ScopedMaterial(context, "BatteryTerminal", new Color32(154, 159, 156, 255), 0.72f);
        for (int index = 0; index < 2; index++)
        {
            float x = (index - 0.5f) * 0.3f;
            CreateLocalPrimitive(root.transform, "Battery_" + index, PrimitiveType.Cylinder,
                new Vector3(x, 0.28f, 0f), new Vector3(0.13f, 0.28f, 0.13f), casing);
            CreateLocalPrimitive(root.transform, "AmberBand_" + index, PrimitiveType.Cylinder,
                new Vector3(x, 0.42f, 0f), new Vector3(0.137f, 0.075f, 0.137f), context.materials.amber);
            CreateLocalPrimitive(root.transform, "Terminal_" + index, PrimitiveType.Cylinder,
                new Vector3(x, 0.585f, 0f), new Vector3(0.055f, 0.025f, 0.055f), terminal);
        }
        CreateLocalPrimitive(root.transform, "BatteryBand", PrimitiveType.Cube,
            new Vector3(0f, 0.22f, 0f), new Vector3(0.46f, 0.13f, 0.31f), context.materials.cyan);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateEmergencyWhistle(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "WhistleMetal", new Color32(164, 170, 169, 255), 0.78f);
        CreateLocalPrimitive(root.transform, "WhistleBody", PrimitiveType.Capsule,
            new Vector3(0f, 0.2f, 0f), new Vector3(0.16f, 0.3f, 0.16f), context.materials.amber,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "MouthPiece", PrimitiveType.Cube,
            new Vector3(0.35f, 0.2f, 0f), new Vector3(0.34f, 0.15f, 0.22f), metal);
        CreateLocalPrimitive(root.transform, "AirSlot", PrimitiveType.Cube,
            new Vector3(0.08f, 0.35f, -0.04f), new Vector3(0.18f, 0.06f, 0.12f), context.materials.dark);
        CreateLocalPrimitive(root.transform, "LanyardEye", PrimitiveType.Sphere,
            new Vector3(-0.34f, 0.2f, 0f), Vector3.one * 0.18f, metal);
        CreateLocalPrimitive(root.transform, "LanyardEyeCut", PrimitiveType.Sphere,
            new Vector3(-0.34f, 0.2f, -0.03f), Vector3.one * 0.09f, context.materials.dark);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateKitchenPan(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material panMetal = ScopedMaterial(context, "PanMetal", new Color32(89, 98, 101, 255), 0.64f);
        Material panInner = ScopedMaterial(context, "PanInner", new Color32(31, 38, 41, 255), 0.3f);
        CreateLocalPrimitive(root.transform, "PanBody", PrimitiveType.Cylinder,
            new Vector3(0f, 0.09f, 0f), new Vector3(0.38f, 0.085f, 0.38f), panMetal);
        CreateLocalPrimitive(root.transform, "PanInner", PrimitiveType.Cylinder,
            new Vector3(0f, 0.18f, 0f), new Vector3(0.32f, 0.025f, 0.32f), panInner);
        CreateLocalPrimitive(root.transform, "PanHandle", PrimitiveType.Cube,
            new Vector3(0.62f, 0.11f, 0f), new Vector3(0.72f, 0.13f, 0.18f), panMetal,
            new Vector3(0f, -5f, 0f));
        CreateLocalPrimitive(root.transform, "HandleGrip", PrimitiveType.Cube,
            new Vector3(0.83f, 0.11f, 0f), new Vector3(0.34f, 0.17f, 0.22f), panInner,
            new Vector3(0f, -5f, 0f));
        CreateLocalPrimitive(root.transform, "WrongChoiceBand", PrimitiveType.Cube,
            new Vector3(0f, 0.22f, -0.25f), new Vector3(0.34f, 0.08f, 0.05f), context.materials.danger);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateSealedBottle(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        bool breakable,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material bottle = breakable
            ? ScopedMaterial(context, "GlassBottle", new Color32(73, 122, 107, 255), 0.74f)
            : ScopedMaterial(context, "WaterBottle", new Color32(137, 210, 222, 255), 0.58f);
        Material label = breakable ? context.materials.danger : context.materials.cream;
        CreateLocalPrimitive(root.transform, "BottleBody", PrimitiveType.Cylinder,
            new Vector3(0f, 0.37f, 0f), new Vector3(0.24f, 0.37f, 0.24f), bottle);
        CreateLocalPrimitive(root.transform, "BottleShoulder", PrimitiveType.Sphere,
            new Vector3(0f, 0.72f, 0f), new Vector3(0.47f, 0.25f, 0.47f), bottle);
        CreateLocalPrimitive(root.transform, "BottleNeck", PrimitiveType.Cylinder,
            new Vector3(0f, 0.87f, 0f), new Vector3(0.11f, 0.17f, 0.11f), bottle);
        CreateLocalPrimitive(root.transform, "SealedCap", PrimitiveType.Cylinder,
            new Vector3(0f, 1.05f, 0f), new Vector3(0.14f, 0.055f, 0.14f),
            breakable ? context.materials.dark : context.materials.cyan);
        CreateLocalPrimitive(root.transform, "BottleLabel", PrimitiveType.Cylinder,
            new Vector3(0f, 0.42f, 0f), new Vector3(0.247f, 0.13f, 0.247f), label);
        CreateLocalPrimitive(root.transform, "LabelMark", PrimitiveType.Cube,
            new Vector3(0f, 0.42f, -0.255f), new Vector3(0.18f, 0.12f, 0.025f),
            breakable ? context.materials.white : context.materials.cyan);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateDurableFoodCan(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "FoodCanMetal", new Color32(151, 156, 153, 255), 0.72f);
        Material label = ScopedMaterial(context, "FoodCanLabel", new Color32(209, 91, 62, 255), 0.36f);
        CreateLocalPrimitive(root.transform, "CanBody", PrimitiveType.Cylinder,
            new Vector3(0f, 0.31f, 0f), new Vector3(0.3f, 0.31f, 0.3f), metal);
        CreateLocalPrimitive(root.transform, "CanLabel", PrimitiveType.Cylinder,
            new Vector3(0f, 0.31f, 0f), new Vector3(0.307f, 0.19f, 0.307f), label);
        CreateLocalPrimitive(root.transform, "TopRim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.635f, 0f), new Vector3(0.32f, 0.025f, 0.32f), metal);
        CreateLocalPrimitive(root.transform, "BottomRim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.015f, 0f), new Vector3(0.32f, 0.025f, 0.32f), metal);
        CreateLocalPrimitive(root.transform, "FoodBadge", PrimitiveType.Sphere,
            new Vector3(0f, 0.33f, -0.31f), new Vector3(0.2f, 0.2f, 0.045f), context.materials.amber);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateCeramicVase(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler = default)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material ceramic = ScopedMaterial(context, "SafetyVaseCeramic", new Color32(65, 139, 151, 255), 0.68f);
        Material inset = ScopedMaterial(context, "SafetyVaseInset", new Color32(30, 59, 66, 255), 0.38f);
        CreateLocalPrimitive(root.transform, "VaseBody", PrimitiveType.Sphere,
            new Vector3(0f, 0.28f, 0f), new Vector3(0.46f, 0.58f, 0.46f), ceramic);
        CreateLocalPrimitive(root.transform, "VaseShoulder", PrimitiveType.Sphere,
            new Vector3(0f, 0.52f, 0f), new Vector3(0.34f, 0.34f, 0.34f), ceramic);
        CreateLocalPrimitive(root.transform, "VaseNeck", PrimitiveType.Cylinder,
            new Vector3(0f, 0.7f, 0f), new Vector3(0.14f, 0.18f, 0.14f), ceramic);
        CreateLocalPrimitive(root.transform, "VaseRim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.89f, 0f), new Vector3(0.2f, 0.055f, 0.2f), ceramic);
        CreateLocalPrimitive(root.transform, "VaseOpening", PrimitiveType.Cylinder,
            new Vector3(0f, 0.95f, 0f), new Vector3(0.12f, 0.018f, 0.12f), inset);
        CreateLocalPrimitive(root.transform, "SafetyBand", PrimitiveType.Cylinder,
            new Vector3(0f, 0.35f, 0f), new Vector3(0.235f, 0.07f, 0.235f), context.materials.amber);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateLayeredWallConsole(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        Material accent,
        string label)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material casing = ScopedMaterial(context, "ConsoleCasing", new Color32(35, 48, 58, 255), 0.48f);
        Material metal = ScopedMaterial(context, "BrushedMetal", new Color32(118, 131, 137, 255), 0.62f);
        Material screen = ScopedMaterial(context, "ConsoleScreen", new Color32(8, 28, 43, 255), 0.52f);
        Material softAccent = ScopedMaterial(
            context,
            "ConsoleAccent_" + Sanitize(label),
            Color.Lerp(accent.color, Color.black, 0.32f),
            0.42f,
            false);

        CreateLocalPrimitive(root.transform, "Casing", PrimitiveType.Cube,
            Vector3.zero, new Vector3(1.22f, 0.82f, 0.2f), casing);
        CreateLocalPrimitive(root.transform, "Bezel", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, -0.14f), new Vector3(1.02f, 0.52f, 0.095f), metal);
        CreateLocalPrimitive(root.transform, "Screen", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, -0.205f), new Vector3(0.88f, 0.39f, 0.035f), screen);
        CreateLocalPrimitive(root.transform, "StatusRibbon", PrimitiveType.Cube,
            new Vector3(-0.14f, 0.22f, -0.23f), new Vector3(0.5f, 0.055f, 0.025f), softAccent);
        CreateLocalPrimitive(root.transform, "ProtectedButtonCollar", PrimitiveType.Cylinder,
            new Vector3(0.36f, -0.23f, -0.18f), new Vector3(0.155f, 0.055f, 0.155f), metal,
            new Vector3(90f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "ProtectedButton", PrimitiveType.Cylinder,
            new Vector3(0.36f, -0.23f, -0.245f), new Vector3(0.105f, 0.045f, 0.105f), accent,
            new Vector3(90f, 0f, 0f));
        for (int index = 0; index < 3; index++)
        {
            CreateLocalPrimitive(root.transform, "StatusLed_" + index, PrimitiveType.Sphere,
                new Vector3(-0.34f + index * 0.14f, -0.23f, -0.23f), Vector3.one * 0.062f,
                index == 1 ? accent : context.materials.dark);
        }
        AddPanelFasteners(root.transform, metal, 0.54f, 0.34f, -0.12f);
        CreateLocalWorldText(root.transform, "DeviceLabel", label,
            new Vector3(0f, 0.08f, -0.235f), Vector3.zero,
            new Vector2(1.4f, 0.3f), 1.25f, context.materials.cream.color);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateGuardedControl(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        Material accent)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material casing = ScopedMaterial(context, "ControlCasing", new Color32(42, 51, 57, 255), 0.55f);
        Material metal = ScopedMaterial(context, "ControlMetal", new Color32(139, 148, 151, 255), 0.68f);
        CreateLocalPrimitive(root.transform, "RubberBase", PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(0.42f, 0.1f, 0.42f), casing);
        CreateLocalPrimitive(root.transform, "MetalGuard", PrimitiveType.Cylinder,
            new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.075f, 0.3f), metal);
        CreateLocalPrimitive(root.transform, "IlluminatedKey", PrimitiveType.Cylinder,
            new Vector3(0f, 0.15f, 0f), new Vector3(0.2f, 0.07f, 0.2f), accent);
        for (int index = 0; index < 4; index++)
        {
            float angle = index * Mathf.PI * 0.5f;
            CreateLocalPrimitive(root.transform, "GuardPost_" + index, PrimitiveType.Cylinder,
                new Vector3(Mathf.Cos(angle) * 0.3f, 0.24f, Mathf.Sin(angle) * 0.3f),
                new Vector3(0.025f, 0.17f, 0.025f), metal);
        }
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateSafetyCertificate(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "CertificateFrame", new Color32(56, 70, 74, 255), 0.55f);
        Material paper = ScopedMaterial(context, "CertificatePaper", new Color32(230, 226, 204, 255), 0.3f);
        CreateLocalPrimitive(root.transform, "Frame", PrimitiveType.Cube, Vector3.zero,
            new Vector3(1.8f, 0.92f, 0.12f), metal);
        CreateLocalPrimitive(root.transform, "Certificate", PrimitiveType.Cube,
            new Vector3(0f, 0f, -0.085f), new Vector3(1.55f, 0.68f, 0.05f), paper);
        CreateLocalPrimitive(root.transform, "CheckShort", PrimitiveType.Cube,
            new Vector3(-0.19f, -0.04f, -0.13f), new Vector3(0.11f, 0.34f, 0.035f),
            context.materials.safe, new Vector3(0f, 0f, -42f));
        CreateLocalPrimitive(root.transform, "CheckLong", PrimitiveType.Cube,
            new Vector3(0.12f, 0.08f, -0.13f), new Vector3(0.11f, 0.58f, 0.035f),
            context.materials.safe, new Vector3(0f, 0f, 43f));
        AddPanelFasteners(root.transform, context.materials.amber, 0.78f, 0.37f, -0.08f);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateWallAnchorPlate(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Material accent)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material metal = ScopedMaterial(context, "AnchorPlateMetal", new Color32(128, 139, 143, 255), 0.68f);
        Material darkMetal = ScopedMaterial(context, "AnchorPlateDark", new Color32(50, 62, 67, 255), 0.5f);
        CreateLocalPrimitive(root.transform, "WallPlate", PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.38f, 0.44f, 0.1f), metal);
        CreateLocalPrimitive(root.transform, "LoadBracket", PrimitiveType.Cube,
            new Vector3(0f, -0.08f, -0.13f), new Vector3(0.24f, 0.12f, 0.28f), darkMetal);
        CreateLocalPrimitive(root.transform, "SafetyIndicator", PrimitiveType.Cube,
            new Vector3(0f, 0.13f, -0.065f), new Vector3(0.24f, 0.075f, 0.035f), accent);
        Vector3[] fasteners =
        {
            new Vector3(-0.13f, 0.16f, -0.07f), new Vector3(0.13f, 0.16f, -0.07f),
            new Vector3(-0.13f, -0.16f, -0.07f), new Vector3(0.13f, -0.16f, -0.07f)
        };
        for (int index = 0; index < fasteners.Length; index++)
            CreateLocalPrimitive(root.transform, "AnchorBolt_" + index, PrimitiveType.Sphere,
                fasteners[index], Vector3.one * 0.045f, context.materials.dark);
        BoxCollider collider = AddBoundsCollider(root) as BoxCollider;
        if (collider != null)
            collider.size = new Vector3(0.58f, 0.62f, 0.42f);
        return root;
    }

    private static GameObject CreateStorageBasket(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 size)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, Vector3.zero);
        Material basket = ScopedMaterial(context, "StorageBasket", new Color32(42, 67, 78, 255), 0.3f);
        CreateLocalPrimitive(root.transform, "BasketFloor", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, 0f), new Vector3(size.x, 0.14f, size.z), basket);
        for (int slat = -3; slat <= 3; slat++)
        {
            float x = slat * size.x / 7.5f;
            CreateLocalPrimitive(root.transform, "FrontSlat_" + slat, PrimitiveType.Cube,
                new Vector3(x, size.y * 0.45f, -size.z * 0.5f),
                new Vector3(0.08f, size.y, 0.07f), basket);
            CreateLocalPrimitive(root.transform, "BackSlat_" + slat, PrimitiveType.Cube,
                new Vector3(x, size.y * 0.45f, size.z * 0.5f),
                new Vector3(0.08f, size.y, 0.07f), basket);
        }
        for (int slat = -2; slat <= 2; slat++)
        {
            float z = slat * size.z / 5.5f;
            CreateLocalPrimitive(root.transform, "LeftSlat_" + slat, PrimitiveType.Cube,
                new Vector3(-size.x * 0.5f, size.y * 0.45f, z),
                new Vector3(0.07f, size.y, 0.08f), basket);
            CreateLocalPrimitive(root.transform, "RightSlat_" + slat, PrimitiveType.Cube,
                new Vector3(size.x * 0.5f, size.y * 0.45f, z),
                new Vector3(0.07f, size.y, 0.08f), basket);
        }
        CreateLocalPrimitive(root.transform, "TopRailFront", PrimitiveType.Cube,
            new Vector3(0f, size.y, -size.z * 0.5f), new Vector3(size.x + 0.08f, 0.08f, 0.1f),
            context.materials.amber);
        CreateLocalPrimitive(root.transform, "TopRailBack", PrimitiveType.Cube,
            new Vector3(0f, size.y, size.z * 0.5f), new Vector3(size.x + 0.08f, 0.08f, 0.1f),
            context.materials.amber);
        return root;
    }

    private static GameObject CreateRouteToken(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        bool turnRight,
        string routeLabel = null)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material tokenAccent = turnRight
            ? ScopedMaterial(context, "RouteTokenAmber", new Color32(190, 125, 41, 255), 0.5f)
            : ScopedMaterial(context, "RouteTokenCyan", new Color32(33, 111, 127, 255), 0.5f);
        Material metal = ScopedMaterial(context, "RouteTokenMetal", new Color32(115, 133, 140, 255), 0.65f);
        CreateLocalPrimitive(root.transform, "TokenRim", PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(0.54f, 0.075f, 0.54f), metal);
        CreateLocalPrimitive(root.transform, "TokenFace", PrimitiveType.Cylinder,
            new Vector3(0f, 0.08f, 0f), new Vector3(0.46f, 0.065f, 0.46f), tokenAccent);
        CreateLocalPrimitive(root.transform, "ArrowStem", PrimitiveType.Cube,
            new Vector3(0f, 0.16f, -0.06f), new Vector3(0.12f, 0.035f, 0.48f),
            context.materials.white);
        float sign = turnRight ? 1f : -1f;
        CreateLocalPrimitive(root.transform, "ArrowHeadA", PrimitiveType.Cube,
            new Vector3(sign * 0.12f, 0.16f, 0.17f), new Vector3(0.11f, 0.035f, 0.33f),
            context.materials.white, new Vector3(0f, sign * 42f, 0f));
        CreateLocalPrimitive(root.transform, "ArrowHeadB", PrimitiveType.Cube,
            new Vector3(sign * 0.22f, 0.16f, 0.05f), new Vector3(0.11f, 0.035f, 0.26f),
            context.materials.white, new Vector3(0f, -sign * 42f, 0f));
        CreateLocalPrimitive(root.transform, "Grip", PrimitiveType.Cube,
            new Vector3(0f, 0.04f, -0.52f), new Vector3(0.42f, 0.15f, 0.11f),
            context.materials.dark);
        if (!string.IsNullOrWhiteSpace(routeLabel))
        {
            CreateLocalPrimitive(root.transform, "InsetLabelPlate", PrimitiveType.Cube,
                new Vector3(0f, 0.155f, -0.34f), new Vector3(routeLabel.Length > 2 ? 0.72f : 0.42f, 0.025f, 0.2f),
                context.materials.navy);
            CreateLocalWorldText(root.transform, "InsetRouteLabel", routeLabel,
                new Vector3(0f, 0.18f, -0.34f), new Vector3(90f, 0f, 0f),
                new Vector2(routeLabel.Length > 2 ? 0.68f : 0.38f, 0.18f), 1.25f,
                context.materials.cream.color);
        }
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateDispatchSlider(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        Material dangerSide,
        Material safeSide,
        string label)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material casing = ScopedMaterial(context, "DispatchSliderCasing", new Color32(41, 50, 57, 255), 0.58f);
        Material metal = ScopedMaterial(context, "DispatchSliderMetal", new Color32(126, 139, 145, 255), 0.68f);
        CreateLocalPrimitive(root.transform, "Base", PrimitiveType.Cube, Vector3.zero,
            new Vector3(1.5f, 0.12f, 0.68f), casing);
        CreateLocalPrimitive(root.transform, "Track", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.05f),
            new Vector3(1.08f, 0.07f, 0.15f), metal);
        CreateLocalPrimitive(root.transform, "DangerStop", PrimitiveType.Cube, new Vector3(-0.58f, 0.12f, 0.05f),
            new Vector3(0.16f, 0.12f, 0.3f), dangerSide);
        CreateLocalPrimitive(root.transform, "SafeStop", PrimitiveType.Cube, new Vector3(0.58f, 0.12f, 0.05f),
            new Vector3(0.16f, 0.12f, 0.3f), safeSide);
        GameObject handle = CreateLocalPrimitive(root.transform, "SliderHandle", PrimitiveType.Cube,
            new Vector3(-0.38f, 0.21f, 0.05f), new Vector3(0.28f, 0.2f, 0.32f), context.materials.cream,
            default, true);
        CreateLocalPrimitive(handle.transform, "HandleAccent", PrimitiveType.Cube,
            new Vector3(0f, 0.56f, 0f), new Vector3(0.8f, 0.12f, 0.8f), safeSide);
        CreateLocalWorldText(root.transform, "IntegratedLabel", label,
            new Vector3(0f, 0.085f, -0.22f), new Vector3(90f, 0f, 0f),
            new Vector2(1.32f, 0.2f), 1.05f, context.materials.cream.color);
        return handle;
    }

    private static GameObject CreateDispatchPodium(
        SceneContext context,
        Vector3 position,
        Vector3 euler)
    {
        GameObject root = CreateAssemblyRoot(context.environment, "MobileDispatchPodium", position, euler);
        Material metal = ScopedMaterial(context, "DispatchMetal", new Color32(73, 86, 93, 255), 0.6f);
        Material rubber = ScopedMaterial(context, "DispatchRubber", new Color32(27, 33, 38, 255), 0.25f);
        CreateLocalPrimitive(root.transform, "Pedestal", PrimitiveType.Cube,
            new Vector3(0f, 0.58f, 0f), new Vector3(1.65f, 1.15f, 0.9f), metal);
        CreateLocalPrimitive(root.transform, "WheelLeft", PrimitiveType.Cylinder,
            new Vector3(-0.58f, 0.12f, -0.38f), new Vector3(0.18f, 0.08f, 0.18f), rubber,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "WheelRight", PrimitiveType.Cylinder,
            new Vector3(0.58f, 0.12f, -0.38f), new Vector3(0.18f, 0.08f, 0.18f), rubber,
            new Vector3(0f, 0f, 90f));
        CreateLocalPrimitive(root.transform, "AngledDesk", PrimitiveType.Cube,
            new Vector3(0f, 1.24f, -0.05f), new Vector3(1.95f, 0.14f, 1.05f),
            context.materials.navy, new Vector3(-12f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "MapScreen", PrimitiveType.Cube,
            new Vector3(0f, 1.34f, -0.18f), new Vector3(1.48f, 0.045f, 0.62f),
            ScopedMaterial(context, "DispatchMap", new Color32(11, 43, 57, 255), 0.5f),
            new Vector3(-12f, 0f, 0f));
        for (int line = -2; line <= 2; line++)
        {
            CreateLocalPrimitive(root.transform, "MapGrid_" + line, PrimitiveType.Cube,
                new Vector3(line * 0.26f, 1.39f, -0.19f), new Vector3(0.025f, 0.02f, 0.56f),
                context.materials.cyan, new Vector3(-12f, 0f, 0f));
        }
        return root;
    }

    private static void CreateParkingPocketMarkings(
        SceneContext context,
        string name,
        Vector3 center,
        float yaw)
    {
        GameObject root = CreateAssemblyRoot(context.environment, name, center, new Vector3(0f, yaw, 0f));
        Material marking = ScopedMaterial(context, "PocketMarking", new Color32(92, 185, 195, 255), 0.15f);
        for (int index = -2; index <= 2; index++)
        {
            CreateLocalPrimitive(root.transform, "SideLeft_" + index, PrimitiveType.Cube,
                new Vector3(-0.92f, 0f, index * 0.58f), new Vector3(0.08f, 0.025f, 0.34f), marking);
            CreateLocalPrimitive(root.transform, "SideRight_" + index, PrimitiveType.Cube,
                new Vector3(0.92f, 0f, index * 0.58f), new Vector3(0.08f, 0.025f, 0.34f), marking);
        }
        for (int index = -1; index <= 1; index++)
        {
            CreateLocalPrimitive(root.transform, "Cap_" + index, PrimitiveType.Cube,
                new Vector3(index * 0.62f, 0f, 1.45f), new Vector3(0.36f, 0.025f, 0.08f), marking);
        }
    }

    private static GameObject CreateRoadBarricade(
        SceneContext context,
        string name,
        Vector3 position,
        Vector3 euler,
        bool active)
    {
        GameObject root = CreateAssemblyRoot(context.environment, name, position, euler);
        Material metal = ScopedMaterial(context, "BarricadeMetal", new Color32(82, 91, 95, 255), 0.58f);
        CreateLocalPrimitive(root.transform, "LeftFoot", PrimitiveType.Cube,
            new Vector3(-0.76f, 0.1f, 0f), new Vector3(0.5f, 0.12f, 0.48f), metal);
        CreateLocalPrimitive(root.transform, "RightFoot", PrimitiveType.Cube,
            new Vector3(0.76f, 0.1f, 0f), new Vector3(0.5f, 0.12f, 0.48f), metal);
        CreateLocalPrimitive(root.transform, "LeftPost", PrimitiveType.Cube,
            new Vector3(-0.76f, 0.66f, 0f), new Vector3(0.11f, 1.1f, 0.11f), metal);
        CreateLocalPrimitive(root.transform, "RightPost", PrimitiveType.Cube,
            new Vector3(0.76f, 0.66f, 0f), new Vector3(0.11f, 1.1f, 0.11f), metal);
        CreateLocalPrimitive(root.transform, "Board", PrimitiveType.Cube,
            new Vector3(0f, 0.86f, 0f), new Vector3(2.25f, 0.36f, 0.12f), context.materials.white);
        for (int stripe = -3; stripe <= 3; stripe++)
        {
            CreateLocalPrimitive(root.transform, "AmberStripe_" + stripe, PrimitiveType.Cube,
                new Vector3(stripe * 0.31f, 0.86f, -0.07f), new Vector3(0.17f, 0.33f, 0.035f),
                context.materials.amber, new Vector3(0f, 0f, -24f));
        }
        CreateLocalPrimitive(root.transform, "WarningLamp", PrimitiveType.Sphere,
            new Vector3(0f, 1.18f, 0f), Vector3.one * 0.17f, context.materials.amber);
        root.SetActive(active);
        return root;
    }

    private static GameObject CreatePedestrianGroup(
        SceneContext context,
        string name,
        Vector3 center,
        bool active)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(context.characters);
        group.transform.position = new Vector3(0f, center.y, 0f);
        RuntimeAnimatorController adultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            StoryAnimationLibraryBuilder.AdultControllerPath);
        string[] prefabs =
        {
            "Assets/Story/Characters/MeshyFamily/Prefabs/Komsu.prefab",
            "Assets/Story/Characters/MeshyFamily/Prefabs/Anne.prefab",
            "Assets/Story/Characters/MeshyFamily/Prefabs/Baba.prefab"
        };
        Vector3[] offsets =
        {
            new Vector3(-0.48f, 0f, 0.1f), new Vector3(0.34f, 0f, -0.18f), new Vector3(0.05f, 0f, 0.5f)
        };
        for (int index = 0; index < prefabs.Length; index++)
        {
            GameObject person = StoryChapterBuilderCommon.InstantiateCharacter(
                prefabs[index], "Pedestrian_" + (index + 1), group.transform,
                center + offsets[index], index == 0 ? 1.68f : 1.72f, adultController);
            FaceCharacterToCamera(person, context.camera, -14f + index * 14f);
        }
        group.SetActive(active);
        return group;
    }

    private static void CreateRoadChevronTrail(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 start,
        Vector3 direction,
        int count,
        Material material)
    {
        Vector3 normalized = direction.normalized;
        float yaw = Mathf.Atan2(normalized.x, normalized.z) * Mathf.Rad2Deg;
        for (int index = 0; index < count; index++)
        {
            Vector3 center = start + normalized * index * 0.72f + Vector3.up * 0.01f;
            GameObject root = CreateAssemblyRoot(parent, name + "_" + index, center, new Vector3(0f, yaw, 0f));
            CreateLocalPrimitive(root.transform, "LeftWing", PrimitiveType.Cube,
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.08f, 0.025f, 0.46f), material,
                new Vector3(0f, -34f, 0f));
            CreateLocalPrimitive(root.transform, "RightWing", PrimitiveType.Cube,
                new Vector3(0.12f, 0f, 0f), new Vector3(0.08f, 0.025f, 0.46f), material,
                new Vector3(0f, 34f, 0f));
        }
    }

    private static void AddVehicleRoofAccent(
        SceneContext context,
        GameObject vehicle,
        string name,
        Material accent)
    {
        Bounds bounds = CombinedBounds(vehicle);
        GameObject root = new GameObject(name);
        root.transform.SetParent(vehicle.transform, false);
        root.transform.localPosition = vehicle.transform.InverseTransformPoint(bounds.center);
        root.transform.localRotation = Quaternion.identity;
        Vector3 lossy = vehicle.transform.lossyScale;
        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(lossy.y));
        float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(lossy.z));
        CreateLocalPrimitive(root.transform, "LightBarBase", PrimitiveType.Cube,
            new Vector3(0f, (bounds.extents.y + 0.025f) / scaleY, 0f),
            new Vector3(0.52f / scaleX, 0.06f / scaleY, 0.22f / scaleZ),
            context.materials.dark);
        CreateLocalPrimitive(root.transform, "SignalLens", PrimitiveType.Cube,
            new Vector3(0f, (bounds.extents.y + 0.075f) / scaleY, 0f),
            new Vector3(0.42f / scaleX, 0.045f / scaleY, 0.16f / scaleZ),
            accent);
    }

    private static void StyleVehicle(
        SceneContext context,
        GameObject vehicle,
        Material body,
        string glassMaterialSuffix)
    {
        Material glass = ScopedMaterial(
            context,
            glassMaterialSuffix,
            new Color32(43, 77, 94, 255),
            0.82f);
        foreach (Renderer renderer in vehicle.GetComponentsInChildren<Renderer>(true))
        {
            string lower = renderer.gameObject.name.ToLowerInvariant();
            if (lower.Contains("wheel") || lower.Contains("steering"))
                renderer.sharedMaterial = context.materials.dark;
            else if (lower.Contains("glass"))
                renderer.sharedMaterial = glass;
            else if (!lower.Contains("lightbar") && !lower.Contains("signal"))
                renderer.sharedMaterial = body;
        }
    }

    private static GameObject CreateBackpackAdjustmentStrap(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        float sideSign)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, new Vector3(0f, 0f, sideSign * 4f));
        Material webbing = ScopedMaterial(context, "BackpackWebbing", new Color32(28, 48, 55, 255), 0.24f);
        Material hardware = ScopedMaterial(context, "BackpackBuckle", new Color32(137, 146, 148, 255), 0.68f);
        CreateLocalPrimitive(root.transform, "ShoulderWebbing", PrimitiveType.Cube,
            new Vector3(0f, 0.36f, 0f), new Vector3(0.18f, 0.82f, 0.075f), webbing);
        CreateLocalPrimitive(root.transform, "AdjusterFrame", PrimitiveType.Cube,
            new Vector3(0f, 0.1f, -0.045f), new Vector3(0.3f, 0.24f, 0.07f), hardware);
        CreateLocalPrimitive(root.transform, "AdjusterInset", PrimitiveType.Cube,
            new Vector3(0f, 0.1f, -0.09f), new Vector3(0.16f, 0.11f, 0.04f), context.materials.amber);
        CreateLocalPrimitive(root.transform, "PullTab", PrimitiveType.Cube,
            new Vector3(sideSign * 0.09f, -0.18f, 0f), new Vector3(0.16f, 0.34f, 0.065f), webbing,
            new Vector3(0f, 0f, sideSign * 18f));
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateGroundSensorPod(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        Material accent,
        string sectorLabel = null)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "SensorMetal", new Color32(101, 116, 124, 255), 0.68f);
        Material rubber = ScopedMaterial(context, "SensorRubber", new Color32(24, 31, 36, 255), 0.22f);
        CreateLocalPrimitive(root.transform, "ShockBase", PrimitiveType.Cylinder,
            new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.12f, 0.42f), rubber);
        CreateLocalPrimitive(root.transform, "AccentRing", PrimitiveType.Cylinder,
            new Vector3(0f, 0.25f, 0f), new Vector3(0.34f, 0.055f, 0.34f), accent);
        CreateLocalPrimitive(root.transform, "ReceiverBody", PrimitiveType.Cylinder,
            new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.25f, 0.2f), metal);
        CreateLocalPrimitive(root.transform, "DirectionalDish", PrimitiveType.Cylinder,
            new Vector3(0f, 0.79f, 0.04f), new Vector3(0.34f, 0.075f, 0.34f), metal,
            new Vector3(68f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "Microphone", PrimitiveType.Capsule,
            new Vector3(0f, 0.88f, -0.13f), new Vector3(0.12f, 0.26f, 0.12f), rubber,
            new Vector3(68f, 0f, 0f));
        for (int leg = 0; leg < 3; leg++)
        {
            float angle = leg * Mathf.PI * 2f / 3f;
            CreateLocalPrimitive(root.transform, "TripodLeg_" + leg, PrimitiveType.Cylinder,
                new Vector3(Mathf.Cos(angle) * 0.28f, 0.25f, Mathf.Sin(angle) * 0.28f),
                new Vector3(0.035f, 0.34f, 0.035f), metal,
                new Vector3(Mathf.Sin(angle) * 18f, 0f, Mathf.Cos(angle) * -18f));
        }
        if (!string.IsNullOrEmpty(sectorLabel))
        {
            CreateLocalPrimitive(root.transform, "SectorBadgeBack", PrimitiveType.Cube,
                new Vector3(0f, 0.43f, -0.25f), new Vector3(0.42f, 0.34f, 0.075f), context.materials.navy);
            CreateLocalPrimitive(root.transform, "SectorBadgeAccent", PrimitiveType.Cube,
                new Vector3(0f, 0.31f, -0.295f), new Vector3(0.34f, 0.055f, 0.035f), accent);
            CreateLocalWorldText(root.transform, "SectorBadgeText", sectorLabel,
                new Vector3(0f, 0.47f, -0.295f), Vector3.zero,
                new Vector2(0.38f, 0.28f), 2.1f, context.materials.cream.color);
        }
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateWaveformChoiceCard(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler,
        Material accent,
        float[] pattern,
        string label = null)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material bezel = ScopedMaterial(context, "WaveBezel", new Color32(68, 82, 90, 255), 0.68f);
        Material screen = ScopedMaterial(context, "WaveScreen", new Color32(4, 18, 28, 255), 0.52f);
        CreateLocalPrimitive(root.transform, "Bezel", PrimitiveType.Cube,
            Vector3.zero, new Vector3(1.02f, 0.82f, 0.13f), bezel);
        CreateLocalPrimitive(root.transform, "Screen", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, -0.085f), new Vector3(0.88f, 0.52f, 0.055f), screen);
        for (int grid = -2; grid <= 2; grid++)
        {
            CreateLocalPrimitive(root.transform, "GridH_" + grid, PrimitiveType.Cube,
                new Vector3(0f, 0.08f + grid * 0.095f, -0.12f), new Vector3(0.82f, 0.012f, 0.018f),
                ScopedMaterial(context, "WaveGrid", new Color32(27, 60, 70, 255), 0.2f));
        }
        float spacing = 0.72f / Mathf.Max(1, pattern.Length - 1);
        for (int index = 0; index < pattern.Length; index++)
        {
            float x = -0.36f + index * spacing;
            float height = Mathf.Clamp(pattern[index], 0.08f, 0.9f) * 0.42f;
            CreateLocalPrimitive(root.transform, "Signal_" + index, PrimitiveType.Cube,
                new Vector3(x, 0.08f, -0.145f), new Vector3(0.035f, height, 0.022f), accent);
        }
        if (!string.IsNullOrWhiteSpace(label))
        {
            CreateLocalPrimitive(root.transform, "LabelRibbon", PrimitiveType.Cube,
                new Vector3(0f, -0.31f, -0.145f), new Vector3(0.88f, 0.15f, 0.025f), context.materials.navy);
            CreateLocalWorldText(root.transform, "IntegratedSignalLabel", label,
                new Vector3(0f, -0.31f, -0.165f), Vector3.zero,
                new Vector2(0.82f, 0.14f), 0.95f, context.materials.cream.color);
        }
        AddPanelFasteners(root.transform, context.materials.dark, 0.45f, 0.3f, -0.08f);
        AddBoundsCollider(root);
        return root;
    }

    private static GameObject CreateDirectionalMicrophone(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "DirectionalMicMetal", new Color32(113, 126, 132, 255), 0.72f);
        Material rubber = ScopedMaterial(context, "DirectionalMicRubber", new Color32(22, 31, 37, 255), 0.22f);
        CreateLocalPrimitive(root.transform, "SupportBase", PrimitiveType.Cylinder,
            new Vector3(0f, 0.06f, 0f), new Vector3(0.32f, 0.06f, 0.32f), rubber);
        CreateLocalPrimitive(root.transform, "Pivot", PrimitiveType.Sphere,
            new Vector3(0f, 0.24f, 0f), Vector3.one * 0.18f, metal);
        CreateLocalPrimitive(root.transform, "Grip", PrimitiveType.Capsule,
            new Vector3(0f, 0.48f, 0.02f), new Vector3(0.15f, 0.3f, 0.15f), rubber,
            new Vector3(68f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "ReceiverBarrel", PrimitiveType.Cylinder,
            new Vector3(0f, 0.72f, 0.15f), new Vector3(0.16f, 0.36f, 0.16f), metal,
            new Vector3(68f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "DirectionalDish", PrimitiveType.Cylinder,
            new Vector3(0f, 0.9f, 0.54f), new Vector3(0.38f, 0.08f, 0.38f), context.materials.cyan,
            new Vector3(68f, 0f, 0f));
        CreateLocalPrimitive(root.transform, "MicCapsule", PrimitiveType.Capsule,
            new Vector3(0f, 0.85f, 0.43f), new Vector3(0.11f, 0.2f, 0.11f), rubber,
            new Vector3(68f, 0f, 0f));
        AddBoundsCollider(root);
        return root;
    }

    private static void CreateDashedBeam(
        Transform parent,
        string name,
        Vector3 start,
        Vector3 end,
        Material material,
        int segments,
        float width)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        for (int index = 0; index < segments; index++)
        {
            float t = (index + 0.5f) / segments;
            GameObject segment = StoryChapterBuilderCommon.CreatePrimitive(
                name + "_" + index,
                PrimitiveType.Cube,
                Vector3.Lerp(start, end, t),
                new Vector3(width, width, length / segments * 0.62f),
                material,
                parent,
                false,
                rotation);
            segment.transform.position += Vector3.up * 0.025f;
        }
    }

    private static GameObject CreateRescueMarkerFlag(
        SceneContext context,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 euler)
    {
        GameObject root = CreateAssemblyRoot(parent, name, position, euler);
        Material metal = ScopedMaterial(context, "MarkerMetal", new Color32(126, 134, 136, 255), 0.62f);
        CreateLocalPrimitive(root.transform, "Stake", PrimitiveType.Cylinder,
            new Vector3(0f, 0.55f, 0f), new Vector3(0.045f, 0.55f, 0.045f), metal);
        CreateLocalPrimitive(root.transform, "SafetyFlag", PrimitiveType.Cube,
            new Vector3(0.28f, 0.9f, 0f), new Vector3(0.52f, 0.3f, 0.045f), context.materials.safe);
        CreateLocalPrimitive(root.transform, "ReflectiveStripe", PrimitiveType.Cube,
            new Vector3(0.28f, 0.9f, -0.035f), new Vector3(0.38f, 0.06f, 0.02f), context.materials.white);
        CreateLocalPrimitive(root.transform, "GroundFoot", PrimitiveType.Cylinder,
            new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.08f, 0.28f), context.materials.dark);
        AddBoundsCollider(root);
        return root;
    }

    private static void DressLivingRoom(SceneContext context)
    {
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Furniture/Nightstand_02.prefab", "LivingRoomConsole", context.environment,
            new Vector3(2.25f, 0f, 3.75f), new Vector3(1.45f, 1.05f, 0.72f),
            new Vector3(0f, 180f, 0f), false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Clock_03.prefab", "LivingRoomClock", context.environment,
            new Vector3(3.15f, 2.35f, 4.28f), new Vector3(0.72f, 0.72f, 0.11f),
            new Vector3(0f, 180f, 0f), false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Book_03.prefab", "SideTableBook", context.environment,
            new Vector3(-2.25f, 0.64f, 1.55f), new Vector3(0.48f, 0.12f, 0.62f),
            new Vector3(0f, 16f, 0f), false);
        StoryAuthoredPropFactory.CreateCeramicMug(
            "FamilyMug", context.environment, new Vector3(-2.62f, 0.64f, 1.58f),
            new Vector3(0.28f, 0.36f, 0.28f), Vector3.zero,
            context.materials.cream, context.materials.dark, false);
        StoryAuthoredPropFactory.CreateFloorCushion(
            "SafeFloorCushion", context.environment, new Vector3(-2.9f, 0f, 0.25f),
            new Vector3(0.82f, 0.3f, 0.82f), new Vector3(0f, 18f, 0f),
            ScopedMaterial(context, "CushionFabric", new Color32(57, 120, 132, 255), 0.28f),
            context.materials.cream, context.materials.amber, false);
        StoryChapterBuilderCommon.InstantiateAsset(
            TownRoot + "/SM_Prop_CeilingExit_01.fbx", "ExitSign", context.environment,
            new Vector3(3.62f, 3.3f, 4.05f), new Vector3(0.95f, 0.45f, 0.18f),
            new Vector3(0f, 180f, 0f), false);
    }

    private static void DressBedroom(SceneContext context)
    {
        CreateFurnitureCarrier(
            context.environment, "BedroomNightstand", "Furniture/Nightstand_02.prefab",
            new Vector3(-4.08f, 0f, 2.72f), new Vector3(0.68f, 0.68f, 0.58f),
            new Vector3(0f, 8f, 0f));
        CreateFurnitureCarrier(
            context.environment, "BedroomLamp", "Decorations/Light_05.prefab",
            new Vector3(-4.08f, 0.68f, 2.72f), new Vector3(0.34f, 0.58f, 0.34f));
        CreateFurnitureCarrier(
            context.environment, "BedroomGuitar", "Decorations/Guitar_01.prefab",
            new Vector3(4.38f, 0f, 2.45f), new Vector3(0.42f, 1.22f, 0.28f),
            new Vector3(0f, -12f, -4f));
        CreateFurnitureCarrier(
            context.environment, "BedroomPlant", "Plants/Plants_19.prefab",
            new Vector3(4.32f, 0f, 3.72f), new Vector3(0.56f, 0.98f, 0.56f));
    }

    private static void DressKitchen(SceneContext context)
    {
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Kitchen_D_01.prefab", "KitchenLowerCabinet", context.environment,
            new Vector3(1.25f, 0f, 3.82f), new Vector3(1.8f, 1.18f, 0.9f),
            new Vector3(0f, 180f, 0f), false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Microwave_01.prefab", "KitchenMicrowave", context.environment,
            new Vector3(2.85f, 1.26f, 3.72f), new Vector3(0.95f, 0.66f, 0.72f),
            new Vector3(0f, 180f, 0f), false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Utensils_01.prefab", "KitchenUtensils", context.environment,
            new Vector3(4.05f, 1.25f, 3.64f), new Vector3(0.55f, 0.8f, 0.42f),
            Vector3.zero, false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Kitchen/Cutting_board_02.prefab", "KitchenCuttingBoard", context.environment,
            new Vector3(3.45f, 1.26f, 3.52f), new Vector3(0.7f, 0.1f, 0.55f),
            new Vector3(0f, 22f, 0f), false);
        StoryAuthoredPropFactory.CreateCeramicMug(
            "KitchenMug", context.environment, new Vector3(2.18f, 1.25f, 3.52f),
            new Vector3(0.3f, 0.38f, 0.3f), Vector3.zero,
            context.materials.cream, context.materials.dark, false);
        StoryChapterBuilderCommon.InstantiateFurniture(
            "Decorations/Picture_21.prefab", "KitchenWallPrint", context.environment,
            new Vector3(-1.55f, 1.9f, 4.28f), new Vector3(1.28f, 1.02f, 0.12f),
            new Vector3(0f, 180f, 0f), false);
    }

    private static void DressRescueSite(SceneContext context)
    {
        StoryChapterBuilderCommon.InstantiateAsset(
            SurvivalRoot + "/Tent.fbx", "ProfessionalCommandTent", context.environment,
            new Vector3(-3.55f, 0f, 3.0f), new Vector3(2.85f, 2.1f, 2.5f),
            new Vector3(0f, 28f, 0f), false, false,
            ScopedMaterial(context, "RescueTent", new Color32(190, 135, 48, 255), 0.24f));
        StoryChapterBuilderCommon.InstantiateAsset(
            SurvivalRoot + "/Radio.fbx", "RescueFieldRadio", context.environment,
            new Vector3(-3.7f, 1.18f, 0.25f), new Vector3(0.45f, 0.5f, 0.35f),
            new Vector3(0f, 35f, 0f), false);
        StoryChapterBuilderCommon.InstantiateAsset(
            SurvivalRoot + "/Shovel.fbx", "RescueToolShovel", context.environment,
            new Vector3(4.0f, 0f, 2.0f), new Vector3(0.35f, 1.65f, 0.25f),
            new Vector3(0f, -18f, -8f), false);
        StoryAuthoredPropFactory.CreateFirstAidKit(
            "RescueFirstAid", context.environment, new Vector3(-4.05f, 0.12f, 0.55f),
            new Vector3(0.68f, 0.52f, 0.46f), new Vector3(0f, 12f, 0f),
            context.materials.navy, context.materials.dark, context.materials.white,
            context.materials.amber, false);
        ParticleSystem dust = StoryChapterBuilderCommon.CreateDust(
            "RescueAtmosphericDust", context.lightingVfx, new Vector3(0.8f, 1.6f, 3.6f),
            StoryChapterBuilderCommon.CreateMaterials(), 14);
        dust.Play();
    }
}
