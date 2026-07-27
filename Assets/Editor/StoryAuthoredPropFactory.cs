using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Creates the few stylized props that are missing from the imported art packs.
/// Every mesh is authored and saved in the project by the editor; nothing is generated at runtime.
/// </summary>
internal static class StoryAuthoredPropFactory
{
    private const string Root = "Assets/Story/Generated/Props";
    private const string MeshRoot = Root + "/Meshes";

    private static readonly Vector2[] RoundedRectangle =
    {
        new(-0.42f, -0.5f), new(0.42f, -0.5f), new(0.5f, -0.42f), new(0.5f, 0.42f),
        new(0.42f, 0.5f), new(-0.42f, 0.5f), new(-0.5f, 0.42f), new(-0.5f, -0.42f)
    };

    private static readonly Vector2[] ShoeOutline =
    {
        new(-0.42f, -0.48f), new(0.42f, -0.48f), new(0.48f, -0.22f), new(0.46f, 0.25f),
        new(0.28f, 0.5f), new(-0.28f, 0.5f), new(-0.46f, 0.25f), new(-0.48f, -0.22f)
    };

    internal static GameObject CreateShoePair(string name, Transform parent, Vector3 feetPosition, Vector3 targetSize,
        Vector3 euler, Material upper, Material sole, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        CreateShoe("LeftShoe", root.transform, new Vector3(-0.31f, 0f, -0.02f), new Vector3(0f, -8f, 0f), upper, sole);
        CreateShoe("RightShoe", root.transform, new Vector3(0.31f, 0f, 0.05f), new Vector3(0f, 11f, 0f), upper, sole);
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateSingleShoe(string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler, Material upper, Material sole, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        CreateShoe("ShoeVisual", root.transform, Vector3.zero, Vector3.zero, upper, sole);
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateParcel(string name, Transform parent, Vector3 feetPosition, Vector3 targetSize,
        Vector3 euler, Material cardboard, Material tape, Material label, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh box = EnsureMesh("RoundedParcelBox", () => CreatePrismMesh("RoundedParcelBox", RoundedRectangle, 1f));
        CreateMeshChild("CardboardBox", root.transform, box, cardboard, new Vector3(0f, 0f, 0f), Vector3.zero,
            Vector3.one);
        CreateMeshChild("PackingTapeLong", root.transform, box, tape, new Vector3(0f, 0.012f, 0f), Vector3.zero,
            new Vector3(0.18f, 1.02f, 1.025f));
        CreateMeshChild("PackingTapeCross", root.transform, box, tape, new Vector3(0f, 0.018f, 0f), Vector3.zero,
            new Vector3(1.025f, 1.03f, 0.16f));
        CreateMeshChild("ShippingLabel", root.transform, box, label, new Vector3(0.19f, 1.035f, 0.12f),
            new Vector3(0f, -12f, 0f), new Vector3(0.34f, 0.025f, 0.25f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateWalkingCane(string name, Transform parent, Vector3 feetPosition,
        Vector3 targetSize, Vector3 euler, Material shaft, Material grip, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh cane = EnsureMesh("WalkingCaneBody", () => CreateTubeMesh("WalkingCaneBody", new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0.65f, 0f),
            new Vector3(0.015f, 1.3f, 0f),
            new Vector3(0.05f, 1.62f, 0f),
            new Vector3(0.16f, 1.78f, 0f),
            new Vector3(0.38f, 1.82f, 0f),
            new Vector3(0.52f, 1.73f, 0f)
        }, 0.045f, 10));
        Mesh cap = EnsureMesh("CaneRubberCap", () => CreatePrismMesh("CaneRubberCap", RoundedRectangle, 1f));
        CreateMeshChild("CurvedShaft", root.transform, cane, shaft, Vector3.zero, Vector3.zero, Vector3.one);
        CreateMeshChild("RubberFoot", root.transform, cap, grip, new Vector3(0f, -0.02f, 0f), Vector3.zero,
            new Vector3(0.13f, 0.12f, 0.13f));
        CreateMeshChild("SoftHandle", root.transform, cap, grip, new Vector3(0.42f, 1.73f, 0f),
            new Vector3(0f, 0f, 90f), new Vector3(0.12f, 0.42f, 0.12f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateGlassShards(string name, Transform parent, Vector3 center, Vector3 spread,
        Material glass, int count = 7, bool collider = false)
    {
        GameObject root = CreateRoot(name, parent, center, Vector3.zero);
        Mesh[] shards =
        {
            EnsureMesh("GlassShardA", () => CreatePrismMesh("GlassShardA",
                new[] { new Vector2(-0.48f, -0.34f), new Vector2(0.5f, -0.18f), new Vector2(-0.24f, 0.5f) }, 0.07f)),
            EnsureMesh("GlassShardB", () => CreatePrismMesh("GlassShardB",
                new[] { new Vector2(-0.5f, -0.2f), new Vector2(0.42f, -0.46f), new Vector2(0.2f, 0.5f) }, 0.06f)),
            EnsureMesh("GlassShardC", () => CreatePrismMesh("GlassShardC",
                new[] { new Vector2(-0.42f, -0.5f), new Vector2(0.5f, 0.12f), new Vector2(-0.16f, 0.48f) }, 0.05f))
        };

        for (int i = 0; i < count; i++)
        {
            float u = Mathf.Repeat(i * 0.6180339f, 1f) - 0.5f;
            float v = Mathf.Repeat(i * 0.381966f + 0.17f, 1f) - 0.5f;
            Vector3 local = new Vector3(u * spread.x, Mathf.Abs(v) * spread.y * 0.08f, v * spread.z);
            Vector3 scale = new Vector3(0.3f + 0.12f * (i % 3), 1f, 0.34f + 0.1f * ((i + 1) % 3));
            CreateMeshChild("GlassShard_" + i, root.transform, shards[i % shards.Length], glass, local,
                new Vector3(i % 2 == 0 ? 5f : -4f, i * 47f, i % 3 == 0 ? 7f : -3f), scale);
        }

        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateDebrisCluster(string name, Transform parent, Vector3 center, Vector3 targetSize,
        Vector3 euler, Material primary, Material secondary, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, center, euler);
        Mesh rubbleA = EnsureMesh("RubbleChunkA", () => CreatePrismMesh("RubbleChunkA",
            new[]
            {
                new Vector2(-0.5f, -0.22f), new Vector2(-0.16f, -0.5f), new Vector2(0.48f, -0.3f),
                new Vector2(0.37f, 0.42f), new Vector2(-0.3f, 0.5f)
            }, 0.72f));
        Mesh rubbleB = EnsureMesh("RubbleChunkB", () => CreatePrismMesh("RubbleChunkB",
            new[]
            {
                new Vector2(-0.44f, -0.46f), new Vector2(0.42f, -0.34f), new Vector2(0.5f, 0.26f),
                new Vector2(0.06f, 0.5f), new Vector2(-0.5f, 0.18f)
            }, 0.58f));

        CreateMeshChild("ChunkLarge", root.transform, rubbleA, primary, new Vector3(-0.24f, 0f, 0.08f),
            new Vector3(0f, 18f, -7f), new Vector3(0.92f, 0.7f, 0.78f));
        CreateMeshChild("ChunkMedium", root.transform, rubbleB, secondary, new Vector3(0.36f, 0f, -0.12f),
            new Vector3(0f, -27f, 5f), new Vector3(0.68f, 0.58f, 0.63f));
        CreateMeshChild("ChunkSmall", root.transform, rubbleA, primary, new Vector3(0.16f, 0f, 0.35f),
            new Vector3(0f, 64f, 9f), new Vector3(0.42f, 0.38f, 0.4f));
        FitToSize(root, center, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateCeramicMug(string name, Transform parent, Vector3 feetPosition, Vector3 targetSize,
        Vector3 euler, Material ceramic, Material inside, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh body = EnsureMesh("CeramicMugBody", () => CreateLatheMesh("CeramicMugBody", new[]
        {
            new Vector2(0.28f, 0f),
            new Vector2(0.34f, 0.05f),
            new Vector2(0.36f, 0.48f),
            new Vector2(0.33f, 0.64f),
            new Vector2(0.31f, 0.69f)
        }, 18, true, false));
        Mesh handle = EnsureMesh("CeramicMugHandle", () => CreateTubeMesh("CeramicMugHandle", new[]
        {
            new Vector3(0.29f, 0.54f, 0f),
            new Vector3(0.48f, 0.55f, 0f),
            new Vector3(0.58f, 0.42f, 0f),
            new Vector3(0.58f, 0.22f, 0f),
            new Vector3(0.46f, 0.12f, 0f),
            new Vector3(0.3f, 0.16f, 0f)
        }, 0.045f, 9));
        Mesh drink = EnsureMesh("MugDarkInset", () => CreateLatheMesh("MugDarkInset",
            new[] { new Vector2(0f, 0f), new Vector2(0.27f, 0f), new Vector2(0.27f, 0.018f), new Vector2(0f, 0.018f) },
            18, false, false));
        CreateMeshChild("CupBody", root.transform, body, ceramic, Vector3.zero, Vector3.zero, Vector3.one);
        CreateMeshChild("CupHandle", root.transform, handle, ceramic, Vector3.zero, Vector3.zero, Vector3.one);
        CreateMeshChild("CupInside", root.transform, drink, inside, new Vector3(0f, 0.655f, 0f), Vector3.zero,
            Vector3.one);
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateMetalBracket(string name, Transform parent, Vector3 feetPosition, Vector3 targetSize,
        Vector3 euler, Material metal, Material accent, bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh box = EnsureMesh("HardwareBevelBox", () => CreatePrismMesh("HardwareBevelBox", RoundedRectangle, 1f));
        CreateMeshChild("WallPlate", root.transform, box, metal, new Vector3(0f, 0.36f, 0f), Vector3.zero,
            new Vector3(0.9f, 0.72f, 0.14f));
        CreateMeshChild("ShelfPlate", root.transform, box, metal, new Vector3(0f, 0.08f, 0.32f), Vector3.zero,
            new Vector3(0.9f, 0.14f, 0.72f));
        CreateMeshChild("BoltLeft", root.transform, box, accent, new Vector3(-0.28f, 0.38f, -0.08f), Vector3.zero,
            new Vector3(0.11f, 0.11f, 0.06f));
        CreateMeshChild("BoltRight", root.transform, box, accent, new Vector3(0.28f, 0.38f, -0.08f), Vector3.zero,
            new Vector3(0.11f, 0.11f, 0.06f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateEmergencyBackpack(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material body,
        Material pocket,
        Material trim,
        Material inside,
        bool open,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh roundedBox = EnsureMesh(
            "EmergencyBackpackRoundedBox",
            () => CreatePrismMesh("EmergencyBackpackRoundedBox", RoundedRectangle, 1f));
        Mesh handle = EnsureMesh("EmergencyBackpackHandle", () => CreateTubeMesh(
            "EmergencyBackpackHandle",
            new[]
            {
                new Vector3(-0.24f, 0.92f, 0.2f),
                new Vector3(-0.22f, 1.09f, 0.2f),
                new Vector3(0.22f, 1.09f, 0.2f),
                new Vector3(0.24f, 0.92f, 0.2f)
            },
            0.035f,
            9));

        float bodyHeight = open ? 0.72f : 1f;
        CreateMeshChild("BackpackBody", root.transform, roundedBox, body, Vector3.zero, Vector3.zero,
            new Vector3(1.08f, bodyHeight, 0.68f));
        CreateMeshChild("FrontPocket", root.transform, roundedBox, pocket, new Vector3(0f, 0.12f, -0.39f),
            Vector3.zero, new Vector3(0.76f, 0.48f, 0.19f));
        CreateMeshChild("PocketTrim", root.transform, roundedBox, trim, new Vector3(0f, 0.52f, -0.49f),
            Vector3.zero, new Vector3(0.65f, 0.045f, 0.055f));
        CreateMeshChild("EmergencyCrossVertical", root.transform, roundedBox, trim,
            new Vector3(0f, 0.25f, -0.5f), Vector3.zero, new Vector3(0.12f, 0.3f, 0.045f));
        CreateMeshChild("EmergencyCrossHorizontal", root.transform, roundedBox, trim,
            new Vector3(0f, 0.34f, -0.505f), Vector3.zero, new Vector3(0.32f, 0.11f, 0.045f));
        CreateMeshChild("LeftShoulderStrap", root.transform, roundedBox, pocket,
            new Vector3(-0.31f, 0.13f, 0.36f), Vector3.zero, new Vector3(0.13f, 0.72f, 0.08f));
        CreateMeshChild("RightShoulderStrap", root.transform, roundedBox, pocket,
            new Vector3(0.31f, 0.13f, 0.36f), Vector3.zero, new Vector3(0.13f, 0.72f, 0.08f));
        CreateMeshChild("TopHandle", root.transform, handle, trim, Vector3.zero, Vector3.zero, Vector3.one);

        if (open)
        {
            CreateMeshChild("BagOpening", root.transform, roundedBox, inside, new Vector3(0f, 0.69f, 0f),
                Vector3.zero, new Vector3(0.88f, 0.055f, 0.54f));
            CreateMeshChild("OpenLid", root.transform, roundedBox, body, new Vector3(0f, 0.69f, 0.49f),
                new Vector3(42f, 0f, 0f), new Vector3(0.98f, 0.12f, 0.62f));
            CreateMeshChild("OpenLidInner", root.transform, roundedBox, inside, new Vector3(0f, 0.72f, 0.45f),
                new Vector3(42f, 0f, 0f), new Vector3(0.82f, 0.035f, 0.5f));
        }
        else
        {
            CreateMeshChild("TopFlap", root.transform, roundedBox, body, new Vector3(0f, 0.78f, -0.08f),
                Vector3.zero, new Vector3(0.94f, 0.24f, 0.62f));
            CreateMeshChild("MainZipper", root.transform, roundedBox, trim, new Vector3(0f, 0.82f, -0.405f),
                Vector3.zero, new Vector3(0.72f, 0.045f, 0.045f));
        }

        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateFirstAidKit(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material body,
        Material trim,
        Material cross,
        Material accent,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh roundedBox = EnsureMesh(
            "FirstAidRoundedCase",
            () => CreatePrismMesh("FirstAidRoundedCase", RoundedRectangle, 1f));
        Mesh handle = EnsureMesh("FirstAidCaseHandle", () => CreateTubeMesh(
            "FirstAidCaseHandle",
            new[]
            {
                new Vector3(-0.24f, 0.67f, 0.12f),
                new Vector3(-0.22f, 0.84f, 0.12f),
                new Vector3(0.22f, 0.84f, 0.12f),
                new Vector3(0.24f, 0.67f, 0.12f)
            },
            0.035f,
            9));

        CreateMeshChild("CaseBody", root.transform, roundedBox, body, Vector3.zero, Vector3.zero,
            new Vector3(1.08f, 0.62f, 0.76f));
        CreateMeshChild("CaseLid", root.transform, roundedBox, body, new Vector3(0f, 0.54f, 0f), Vector3.zero,
            new Vector3(1.02f, 0.16f, 0.72f));
        CreateMeshChild("ZipperFront", root.transform, roundedBox, trim, new Vector3(0f, 0.53f, -0.405f),
            Vector3.zero, new Vector3(0.82f, 0.055f, 0.045f));
        CreateMeshChild("ZipperBack", root.transform, roundedBox, trim, new Vector3(0f, 0.53f, 0.405f),
            Vector3.zero, new Vector3(0.82f, 0.055f, 0.045f));
        CreateMeshChild("FrontCrossVertical", root.transform, roundedBox, cross,
            new Vector3(0f, 0.16f, -0.414f), Vector3.zero, new Vector3(0.13f, 0.32f, 0.035f));
        CreateMeshChild("FrontCrossHorizontal", root.transform, roundedBox, cross,
            new Vector3(0f, 0.255f, -0.416f), Vector3.zero, new Vector3(0.34f, 0.12f, 0.035f));
        CreateMeshChild("TopCrossVertical", root.transform, roundedBox, cross,
            new Vector3(0f, 0.705f, 0f), Vector3.zero, new Vector3(0.13f, 0.025f, 0.34f));
        CreateMeshChild("TopCrossHorizontal", root.transform, roundedBox, cross,
            new Vector3(0f, 0.708f, 0f), Vector3.zero, new Vector3(0.34f, 0.025f, 0.13f));
        CreateMeshChild("LeftLatch", root.transform, roundedBox, accent,
            new Vector3(-0.3f, 0.47f, -0.43f), Vector3.zero, new Vector3(0.1f, 0.13f, 0.055f));
        CreateMeshChild("RightLatch", root.transform, roundedBox, accent,
            new Vector3(0.3f, 0.47f, -0.43f), Vector3.zero, new Vector3(0.1f, 0.13f, 0.055f));
        CreateMeshChild("CarryHandle", root.transform, handle, trim, Vector3.zero, Vector3.zero, Vector3.one);

        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateEnvelope(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material paper,
        Material flap,
        Material seal,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh roundedBox = EnsureMesh(
            "EnvelopeRoundedBody",
            () => CreatePrismMesh("EnvelopeRoundedBody", RoundedRectangle, 0.12f));
        Mesh triangle = EnsureMesh(
            "EnvelopeFlapTriangle",
            () => CreatePrismMesh("EnvelopeFlapTriangle",
                new[] { new Vector2(-0.5f, -0.32f), new Vector2(0.5f, -0.32f), new Vector2(0f, 0.5f) }, 0.025f));
        CreateMeshChild("EnvelopeBody", root.transform, roundedBox, paper, Vector3.zero, Vector3.zero,
            new Vector3(1f, 1f, 0.68f));
        CreateMeshChild("EnvelopeFlap", root.transform, triangle, flap, new Vector3(0f, 0.125f, 0.04f),
            Vector3.zero, new Vector3(0.92f, 1f, 0.58f));
        CreateMeshChild("EnvelopeSeal", root.transform, roundedBox, seal, new Vector3(0f, 0.155f, 0.05f),
            Vector3.zero, new Vector3(0.16f, 0.45f, 0.16f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreatePictureFrame(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material frame,
        Material picture,
        Material accent,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh roundedBox = EnsureMesh(
            "StoryPictureFrameRoundedBox",
            () => CreatePrismMesh("StoryPictureFrameRoundedBox", RoundedRectangle, 1f));
        CreateMeshChild("FrameBack", root.transform, roundedBox, frame, Vector3.zero, Vector3.zero,
            new Vector3(0.86f, 1f, 0.12f));
        CreateMeshChild("Picture", root.transform, roundedBox, picture, new Vector3(0f, 0.12f, -0.071f),
            Vector3.zero, new Vector3(0.66f, 0.72f, 0.035f));
        CreateMeshChild("PictureSun", root.transform, roundedBox, accent, new Vector3(0.18f, 0.62f, -0.095f),
            Vector3.zero, new Vector3(0.14f, 0.14f, 0.025f));
        CreateMeshChild("PictureGround", root.transform, roundedBox, accent, new Vector3(-0.08f, 0.18f, -0.098f),
            new Vector3(0f, 0f, -12f), new Vector3(0.48f, 0.12f, 0.025f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateToyWheel(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material tire,
        Material hub,
        Material axle,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh tireMesh = EnsureMesh("ToyWheelTire", () => CreateLatheMesh("ToyWheelTire", new[]
        {
            new Vector2(0.34f, -0.13f),
            new Vector2(0.43f, -0.08f),
            new Vector2(0.47f, 0f),
            new Vector2(0.43f, 0.08f),
            new Vector2(0.34f, 0.13f)
        }, 18, true, true));
        Mesh hubMesh = EnsureMesh("ToyWheelHub", () => CreateLatheMesh("ToyWheelHub", new[]
        {
            new Vector2(0.22f, -0.145f),
            new Vector2(0.25f, -0.12f),
            new Vector2(0.25f, 0.12f),
            new Vector2(0.22f, 0.145f)
        }, 18, true, true));
        Mesh axleMesh = EnsureMesh("ToyWheelAxle", () => CreateLatheMesh("ToyWheelAxle", new[]
        {
            new Vector2(0.08f, -0.18f),
            new Vector2(0.08f, 0.18f)
        }, 14, true, true));
        CreateMeshChild("RubberTire", root.transform, tireMesh, tire, Vector3.zero, Vector3.zero, Vector3.one);
        CreateMeshChild("BrightHub", root.transform, hubMesh, hub, Vector3.zero, Vector3.zero, Vector3.one);
        CreateMeshChild("AxlePin", root.transform, axleMesh, axle, Vector3.zero, Vector3.zero, Vector3.one);
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    internal static GameObject CreateFoldedCloth(
        string name,
        Transform parent,
        Vector3 feetPosition,
        Vector3 targetSize,
        Vector3 euler,
        Material cloth,
        Material edge,
        bool collider = true)
    {
        GameObject root = CreateRoot(name, parent, feetPosition, euler);
        Mesh roundedBox = EnsureMesh(
            "FoldedClothRoundedLayerV2",
            () => CreatePrismMesh("FoldedClothRoundedLayerV2", RoundedRectangle, 0.06f));
        CreateMeshChild("ClothBottom", root.transform, roundedBox, edge, Vector3.zero, Vector3.zero,
            new Vector3(1f, 1f, 0.76f));
        CreateMeshChild("ClothMiddle", root.transform, roundedBox, cloth, new Vector3(0.04f, 0.045f, -0.02f),
            new Vector3(0f, 2f, 0f), new Vector3(0.94f, 1f, 0.72f));
        CreateMeshChild("ClothTop", root.transform, roundedBox, cloth, new Vector3(-0.02f, 0.09f, 0.03f),
            new Vector3(0f, -3f, 0f), new Vector3(0.88f, 1f, 0.68f));
        CreateMeshChild("FoldLine", root.transform, roundedBox, edge, new Vector3(0.18f, 0.145f, -0.14f),
            Vector3.zero, new Vector3(0.035f, 0.6f, 0.38f));
        FitToSize(root, feetPosition, targetSize);
        if (collider)
            EnsureBoxCollider(root);
        return root;
    }

    private static void CreateShoe(string name, Transform parent, Vector3 position, Vector3 euler, Material upper,
        Material sole)
    {
        GameObject root = CreateRoot(name, parent, position, euler, true);
        Mesh soleMesh = EnsureMesh("SneakerSole", () => CreatePrismMesh("SneakerSole", ShoeOutline, 0.13f));
        Mesh upperMesh = EnsureMesh("SneakerUpper", () => CreatePrismMesh("SneakerUpper", ShoeOutline, 0.36f));
        Mesh insetMesh = EnsureMesh("SneakerInset", () => CreatePrismMesh("SneakerInset", RoundedRectangle, 0.08f));
        CreateMeshChild("Sole", root.transform, soleMesh, sole, Vector3.zero, Vector3.zero,
            new Vector3(0.78f, 1f, 1.3f));
        CreateMeshChild("Upper", root.transform, upperMesh, upper, new Vector3(0f, 0.12f, 0.05f), Vector3.zero,
            new Vector3(0.7f, 1f, 1.05f));
        CreateMeshChild("Collar", root.transform, insetMesh, sole, new Vector3(0f, 0.43f, -0.2f), Vector3.zero,
            new Vector3(0.34f, 1f, 0.34f));
        CreateMeshChild("ToeAccent", root.transform, insetMesh, sole, new Vector3(0f, 0.31f, 0.45f), Vector3.zero,
            new Vector3(0.46f, 0.34f, 0.12f));
    }

    private static GameObject CreateRoot(string name, Transform parent, Vector3 position, Vector3 euler,
        bool local = false)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, !local);
        if (local)
        {
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(euler);
        }
        else
        {
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(euler);
        }
        return root;
    }

    private static GameObject CreateMeshChild(string name, Transform parent, Mesh mesh, Material material,
        Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        GameObject child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.Euler(localEuler);
        child.transform.localScale = localScale;
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        child.GetComponent<MeshRenderer>().sharedMaterial = material;
        return child;
    }

    private static Mesh EnsureMesh(string name, Func<Mesh> create)
    {
        EnsureFolder(MeshRoot);
        string path = MeshRoot + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        Mesh mesh = create();
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh CreatePrismMesh(string name, IReadOnlyList<Vector2> outline, float height)
    {
        int count = outline.Count;
        Vector3[] vertices = new Vector3[count * 2];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(outline[i].x, 0f, outline[i].y);
            vertices[i + count] = new Vector3(outline[i].x, height, outline[i].y);
        }

        List<int> triangles = new();
        for (int i = 1; i < count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
            triangles.Add(count);
            triangles.Add(count + i);
            triangles.Add(count + i + 1);
        }
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            triangles.Add(i);
            triangles.Add(next);
            triangles.Add(count + next);
            triangles.Add(i);
            triangles.Add(count + next);
            triangles.Add(count + i);
        }

        Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateLatheMesh(string name, IReadOnlyList<Vector2> profile, int segments, bool capBottom,
        bool capTop)
    {
        int rings = profile.Count;
        List<Vector3> vertices = new(rings * segments + 2);
        List<int> triangles = new((rings - 1) * segments * 6);
        for (int ring = 0; ring < rings; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y,
                    Mathf.Sin(angle) * profile[ring].x));
            }
        }
        for (int ring = 0; ring < rings - 1; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int a = ring * segments + segment;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + segment;
                int d = (ring + 1) * segments + next;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }
        if (capBottom)
            AddLatheCap(vertices, triangles, profile[0].y, segments, 0, false);
        if (capTop)
            AddLatheCap(vertices, triangles, profile[rings - 1].y, segments, (rings - 1) * segments, true);

        Mesh mesh = new Mesh { name = name, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddLatheCap(List<Vector3> vertices, List<int> triangles, float y, int segments, int ringStart,
        bool upward)
    {
        int center = vertices.Count;
        vertices.Add(new Vector3(0f, y, 0f));
        for (int segment = 0; segment < segments; segment++)
        {
            int next = (segment + 1) % segments;
            if (upward)
            {
                triangles.Add(center);
                triangles.Add(ringStart + segment);
                triangles.Add(ringStart + next);
            }
            else
            {
                triangles.Add(center);
                triangles.Add(ringStart + next);
                triangles.Add(ringStart + segment);
            }
        }
    }

    private static Mesh CreateTubeMesh(string name, IReadOnlyList<Vector3> points, float radius, int ringSegments)
    {
        List<Vector3> vertices = new(points.Count * ringSegments);
        List<int> triangles = new((points.Count - 1) * ringSegments * 6);
        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            Vector3 tangent = pointIndex == 0
                ? points[1] - points[0]
                : pointIndex == points.Count - 1
                    ? points[pointIndex] - points[pointIndex - 1]
                    : points[pointIndex + 1] - points[pointIndex - 1];
            tangent.Normalize();
            Vector3 normal = Vector3.Cross(tangent, Vector3.forward);
            if (normal.sqrMagnitude < 0.01f)
                normal = Vector3.Cross(tangent, Vector3.right);
            normal.Normalize();
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;
            for (int segment = 0; segment < ringSegments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / ringSegments;
                vertices.Add(points[pointIndex] +
                             (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius);
            }
        }
        for (int pointIndex = 0; pointIndex < points.Count - 1; pointIndex++)
        {
            for (int segment = 0; segment < ringSegments; segment++)
            {
                int next = (segment + 1) % ringSegments;
                int a = pointIndex * ringSegments + segment;
                int b = pointIndex * ringSegments + next;
                int c = (pointIndex + 1) * ringSegments + segment;
                int d = (pointIndex + 1) * ringSegments + next;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        Mesh mesh = new Mesh { name = name, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void FitToSize(GameObject root, Vector3 feetPosition, Vector3 targetSize)
    {
        if (!TryGetBounds(root, out Bounds bounds))
            return;
        float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
            Mathf.Min(targetSize.y / Mathf.Max(0.01f, bounds.size.y),
                targetSize.z / Mathf.Max(0.01f, bounds.size.z)));
        root.transform.localScale *= scale;
        TryGetBounds(root, out bounds);
        root.transform.position += feetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }
        bool initialized = false;
        bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;
            Bounds local = renderer.localBounds;
            Matrix4x4 matrix = renderer.localToWorldMatrix;
            Vector3 min = local.min;
            Vector3 max = local.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = matrix.MultiplyPoint3x4(new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z));
                if (!initialized)
                {
                    bounds = new Bounds(corner, Vector3.zero);
                    initialized = true;
                }
                else
                    bounds.Encapsulate(corner);
            }
        }
        return initialized;
    }

    private static void EnsureBoxCollider(GameObject root)
    {
        if (!TryGetBounds(root, out Bounds bounds))
            return;
        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        Vector3 worldSize = bounds.size;
        Vector3 scale = root.transform.lossyScale;
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = localCenter;
        collider.size = new Vector3(
            worldSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            worldSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            worldSize.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
