using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StorySharedHomePrefabBuilder
{
    public const string SourceScenePath = "Assets/Scenes/Story_03_Quake.unity";
    public const string PrefabFolder = "Assets/Story/Prefabs/Home";
    public const string PrefabPath = PrefabFolder + "/StoryHome_Shared.prefab";

    private const string SourceRootName = "STORY_03_QUAKE";
    private const string SourceEnvironmentName = "Environment_StoryHome";
    [MenuItem("Tools/Deprem Story/Build Shared Story Home Prefab")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Tools/Deprem Story/Build Shared Story Home Prefab (Silent)")]
    public static void BuildSilentFromMenu()
    {
        Build(false);
    }

    public static GameObject Build(bool showDialog)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Ortak ev prefabı Play Mode dışında üretilmelidir.");
        if (!File.Exists(SourceScenePath))
            throw new FileNotFoundException("Kanonik Story 03 sahnesi bulunamadı.", SourceScenePath);

        EnsureFolder(PrefabFolder);
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        GameObject savedPrefab = null;

        try
        {
            Scene sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

            GameObject sourceRoot = sourceScene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == SourceRootName);
            if (sourceRoot == null)
                throw new InvalidOperationException($"'{SourceRootName}' kökü {SourceScenePath} içinde bulunamadı.");

            Transform sourceEnvironment = FindDescendant(sourceRoot.transform, SourceEnvironmentName);
            if (sourceEnvironment == null)
                throw new InvalidOperationException($"'{SourceEnvironmentName}' kanonik Story 03 sahnesinde bulunamadı.");

            GameObject clone = Object.Instantiate(sourceEnvironment.gameObject);
            clone.name = "StoryHome_Shared";
            clone.transform.SetParent(null);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            SceneManager.MoveGameObjectToScene(clone, sourceScene);
            TurnTallShelfVariantsAround(clone.transform);
            ReplaceInconsistentTallShelfVisuals(
                clone.transform,
                StoryChapterBuilderCommon.CreateMaterials());
            ReplaceUnreadableSofaThrow(clone.transform);
            HideDoorwayFloorBridgeMesh(clone.transform);

            // NavMesh verisi sahneye aittir. Her Story sahnesi kendi yüzeyini ve bake'ini üretir;
            // ortak prefab Story 03'ün navigation assetine gizli bir bağ taşımaz.
            foreach (NavMeshSurface surface in clone.GetComponentsInChildren<NavMeshSurface>(true))
                Object.DestroyImmediate(surface);

            savedPrefab = PrefabUtility.SaveAsPrefabAsset(clone, PrefabPath, out bool success);
            Object.DestroyImmediate(clone);
            if (!success || savedPrefab == null)
                throw new InvalidOperationException("StoryHome_Shared prefabı AssetDatabase'e kaydedilemedi.");

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
            StorySharedHomePrefabValidator.Validate(false);

            Debug.Log($"StoryHome_Shared üretildi: {PrefabPath}");
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Deprem Story",
                    "Story 03 kanonik evi bağımsız ortak prefab olarak üretildi.\nMevcut sahneler değiştirilmedi.",
                    "Tamam");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void TurnTallShelfVariantsAround(Transform home)
    {
        string[] shelfNames = { "Shelf_Secured", "Shelf_Unsecured", "Shelf_Fallen" };
        foreach (string shelfName in shelfNames)
        {
            Transform shelf = FindDescendant(home, shelfName);
            if (shelf == null)
                throw new InvalidOperationException($"Ortak evde çevrilecek uzun dolap bulunamadı: {shelfName}");

            shelf.rotation = Quaternion.AngleAxis(180f, Vector3.up) * shelf.rotation;
            for (int childIndex = shelf.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = shelf.GetChild(childIndex);
                if (child.name == "Bookcase_Visual")
                    continue;

                if (child.name.StartsWith("Book_0_", StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                if (child.name.StartsWith("Book_", StringComparison.Ordinal))
                {
                    string[] parts = child.name.Split('_');
                    int row = int.Parse(parts[1]);
                    int slot = int.Parse(parts[2]);
                    float shelfSurfaceY = row == 1 ? 1.16f : 1.50f;
                    float shelfSlotX = -0.3f + slot * 0.3f;
                    child.localPosition = new Vector3(shelfSlotX, shelfSurfaceY, 0f);
                }
                else
                {
                    Vector3 localPosition = child.localPosition;
                    child.localPosition = new Vector3(localPosition.x, localPosition.y, -localPosition.z);
                }

                child.localRotation = Quaternion.AngleAxis(180f, Vector3.up) * child.localRotation;
            }
        }
    }

    private static void ReplaceInconsistentTallShelfVisuals(
        Transform home,
        StoryChapterBuilderCommon.Materials materials)
    {
        foreach (string shelfName in new[] { "Shelf_Secured", "Shelf_Unsecured", "Shelf_Fallen" })
        {
            Transform shelf = FindDescendant(home, shelfName);
            Transform importedVisual = shelf != null ? FindDescendant(shelf, "Bookcase_Visual") : null;
            if (shelf == null || importedVisual == null)
                throw new InvalidOperationException($"Ortak evde yenilenecek raf görseli bulunamadı: {shelfName}");

            Renderer[] importedRenderers = importedVisual.GetComponentsInChildren<Renderer>(true);
            if (importedRenderers.Length == 0)
                throw new InvalidOperationException($"{shelfName} görünür raf bounds'u üretmedi.");

            Bounds localBounds = CalculateLocalBounds(shelf, importedRenderers);
            foreach (Renderer renderer in importedRenderers)
                renderer.enabled = false;

            Transform skin = new GameObject("StoryShelfVisual").transform;
            skin.SetParent(shelf, false);

            float width = localBounds.size.x;
            float height = localBounds.size.y;
            float depth = localBounds.size.z;
            float side = Mathf.Max(0.055f, width * 0.065f);
            float board = Mathf.Max(0.045f, height * 0.032f);
            float back = Mathf.Max(0.028f, depth * 0.075f);
            float frontZ = localBounds.max.z;
            float backZ = localBounds.min.z + back * 0.5f;
            float innerWidth = Mathf.Max(0.1f, width - side * 2f);
            float lowerDoorHeight = height * 0.29f;

            CreateShelfPanel("LeftFrame", skin,
                new Vector3(localBounds.min.x + side * 0.5f, localBounds.center.y, localBounds.center.z),
                new Vector3(side, height, depth), materials.teal);
            CreateShelfPanel("RightFrame", skin,
                new Vector3(localBounds.max.x - side * 0.5f, localBounds.center.y, localBounds.center.z),
                new Vector3(side, height, depth), materials.teal);
            CreateShelfPanel("TopFrame", skin,
                new Vector3(localBounds.center.x, localBounds.max.y - board * 0.5f, localBounds.center.z),
                new Vector3(innerWidth, board, depth), materials.teal);
            CreateShelfPanel("BottomFrame", skin,
                new Vector3(localBounds.center.x, localBounds.min.y + board * 0.5f, localBounds.center.z),
                new Vector3(innerWidth, board, depth), materials.teal);
            CreateShelfPanel("BackPanel", skin,
                new Vector3(localBounds.center.x, localBounds.center.y, backZ),
                new Vector3(innerWidth, height - board * 2f, back), materials.cream);

            float lowerTopY = localBounds.min.y + lowerDoorHeight;
            foreach (float normalizedY in new[] { 0.31f, 0.51f, 0.7f, 0.87f })
            {
                CreateShelfPanel(
                    "ShelfBoard_" + Mathf.RoundToInt(normalizedY * 100f),
                    skin,
                    new Vector3(
                        localBounds.center.x,
                        Mathf.Lerp(localBounds.min.y, localBounds.max.y, normalizedY),
                        localBounds.center.z),
                    new Vector3(innerWidth, board, depth - back),
                    materials.wood);
            }

            float doorGap = Mathf.Max(0.018f, width * 0.018f);
            float doorWidth = (innerWidth - doorGap * 3f) * 0.5f;
            float doorDepth = Mathf.Max(0.035f, depth * 0.065f);
            float doorY = localBounds.min.y + lowerDoorHeight * 0.5f;
            float leftDoorX = localBounds.center.x - doorWidth * 0.5f - doorGap * 0.5f;
            float rightDoorX = localBounds.center.x + doorWidth * 0.5f + doorGap * 0.5f;
            foreach ((string name, float x) in new[]
                     {
                         ("LowerDoorLeft", leftDoorX),
                         ("LowerDoorRight", rightDoorX)
                     })
            {
                CreateShelfPanel(name, skin,
                    new Vector3(x, doorY, frontZ - doorDepth * 0.5f),
                    new Vector3(doorWidth, lowerDoorHeight - board * 1.4f, doorDepth),
                    materials.coral);
                CreateShelfPanel(name + "Knob", skin,
                    new Vector3(
                        x + (name.EndsWith("Left", StringComparison.Ordinal) ? doorWidth * 0.32f : -doorWidth * 0.32f),
                        doorY,
                        frontZ + doorDepth * 0.12f),
                    Vector3.one * Mathf.Max(0.04f, width * 0.035f),
                    materials.amber,
                    PrimitiveType.Sphere);
            }

            // Keep shelf contents above the cabinet doors; this also gives the quake
            // variants a stable, readable surface instead of an ornamental silhouette.
            if (lowerTopY <= localBounds.min.y + board)
                throw new InvalidOperationException($"{shelfName} alt dolap ölçüsü geçersiz üretildi.");
        }
    }

    private static Bounds CalculateLocalBounds(Transform root, Renderer[] renderers)
    {
        bool initialized = false;
        Bounds result = default;
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? world.min.x : world.max.x,
                    (corner & 2) == 0 ? world.min.y : world.max.y,
                    (corner & 4) == 0 ? world.min.z : world.max.z);
                Vector3 localPoint = root.InverseTransformPoint(point);
                if (!initialized)
                {
                    result = new Bounds(localPoint, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(localPoint);
                }
            }
        }
        return result;
    }

    private static GameObject CreateShelfPanel(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        PrimitiveType primitiveType = PrimitiveType.Cube)
    {
        GameObject panel = GameObject.CreatePrimitive(primitiveType);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = localPosition;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = localScale;
        panel.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        return panel;
    }

    private static void ReplaceUnreadableSofaThrow(Transform home)
    {
        Transform sofa = FindDescendant(home, "FamilySofa");
        if (sofa == null)
            throw new InvalidOperationException("Ortak evde aile kanepesi bulunamadÄ±.");

        string[] obsoleteNames = { "SofaThrow", "KoltukBattaniyesi" };
        foreach (string obsoleteName in obsoleteNames)
        {
            Transform obsolete = FindDescendant(home, obsoleteName);
            if (obsolete != null)
                Object.DestroyImmediate(obsolete.gameObject);
        }

        // The packed bedroll replacement read as an orange pipe/log on the sofa.
        // This is only decoration, so keep the cushion clean instead of adding
        // an ambiguous prop that competes with real interaction objects.
    }

    private static void HideDoorwayFloorBridgeMesh(Transform home)
    {
        Transform bridge = FindDescendant(home, "DoorwayFloorBridge");
        if (bridge == null)
            throw new InvalidOperationException("Ortak evde DoorwayFloorBridge bulunamadı.");

        Renderer[] renderers = bridge.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("DoorwayFloorBridge üzerinde gizlenecek renderer bulunamadı.");

        foreach (Renderer renderer in renderers)
            renderer.enabled = false;

        Collider collider = bridge.GetComponent<Collider>();
        if (collider == null)
            throw new InvalidOperationException("DoorwayFloorBridge navigation collider'ı bulunamadı.");

        collider.enabled = true;
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    internal static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendant(root.GetChild(i), name);
            if (match != null)
                return match;
        }

        return null;
    }
}

public static class StorySharedHomePrefabValidator
{
    private static readonly string[] RequiredObjects =
    {
        "Room",
        "LivingRoom_Floor",
        "Corridor",
        "CorridorFloor",
        "DoorwayFloorBridge",
        "SafeTable",
        "Window_DangerZone",
        "Wardrobe_Secured",
        "Wardrobe_Unsecured",
        "Wardrobe_Fallen",
        "Shelf_Secured",
        "Shelf_Unsecured",
        "Shelf_Fallen",
        // Current authored Story 03 scene keeps the closed FBX instance as "Door".
        // The open state is the explicit sibling "Door_Open".
        "Door",
        "Door_Open",
        "ExitRoute_Cleared",
        "ExitRoute_ClutteredButPassable",
        "CorridorThresholdFocus",
        "CorridorAftershockFocus"
    };

    [MenuItem("Tools/Deprem Story/Validate Shared Story Home Prefab")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    public static void Validate(bool showDialog)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StorySharedHomePrefabBuilder.PrefabPath);
        Require(prefab != null, "StoryHome_Shared prefab asseti");

        foreach (string objectName in RequiredObjects)
            Require(
                StorySharedHomePrefabBuilder.FindDescendant(prefab.transform, objectName) != null,
                $"Gerekli ortak ev nesnesi: {objectName}");

        Transform floor = StorySharedHomePrefabBuilder.FindDescendant(prefab.transform, "LivingRoom_Floor");
        Vector3 floorScale = floor.localScale;
        Require(Mathf.Abs(Mathf.Abs(floorScale.x) - 10f) < 0.05f, "Kanonik salon genişliği 10 m");
        Require(Mathf.Abs(Mathf.Abs(floorScale.z) - 11.5f) < 0.05f, "Kanonik salon uzunluğu 11,5 m");

        Require(prefab.GetComponentsInChildren<Collider>(true).Length >= 30, "Ortak ev fizik colliderları");
        Require(prefab.GetComponentsInChildren<Renderer>(true).Length >= 30, "Ortak ev görünür geometri");
        Require(prefab.GetComponentsInChildren<NavMeshSurface>(true).Length == 0,
            "Prefab sahneye özel NavMeshSurface/NavMeshData taşımaz");

        Require(prefab.GetComponentsInChildren<StoryTouchManager>(true).Length == 0, "Prefab input manager taşımaz");
        Require(prefab.GetComponentsInChildren<StoryGameManager>(true).Length == 0, "Prefab session manager taşımaz");
        Require(prefab.GetComponentsInChildren<StorySequenceDirector>(true).Length == 0, "Prefab bölüm director'ı taşımaz");
        Require(prefab.GetComponentsInChildren<StoryInteractable>(true).Length == 0, "Prefab sahneye özel etkileşim taşımaz");

        Transform doorwayFloorBridge =
            StorySharedHomePrefabBuilder.FindDescendant(prefab.transform, "DoorwayFloorBridge");
        Require(
            doorwayFloorBridge.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled),
            "DoorwayFloorBridge mesh renderer'ları görünmez");
        Require(
            doorwayFloorBridge.GetComponent<Collider>() != null &&
            doorwayFloorBridge.GetComponent<Collider>().enabled,
            "DoorwayFloorBridge navigation collider'ı aktif");

        string guid = AssetDatabase.AssetPathToGUID(StorySharedHomePrefabBuilder.PrefabPath);
        Require(!string.IsNullOrWhiteSpace(guid), "Ortak ev prefab GUID");

        Debug.Log($"StoryHome_Shared doğrulandı: objects={RequiredObjects.Length}, colliders={prefab.GetComponentsInChildren<Collider>(true).Length}, renderers={prefab.GetComponentsInChildren<Renderer>(true).Length}, guid={guid}");
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Story", "StoryHome_Shared yapısal doğrulamayı geçti.", "Tamam");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("StoryHome_Shared doğrulama hatası: " + label);
    }
}
