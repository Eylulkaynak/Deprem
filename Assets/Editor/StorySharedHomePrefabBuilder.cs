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
    private const string KenneyBedrollPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Models/bedroll-packed.fbx";
    private const string KenneySurvivalMaterialPath =
        "Assets/Story/Environment/ThirdParty/KenneySurvival/Materials/KenneySurvival_Atlas.mat";

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

        Material material = AssetDatabase.LoadAssetAtPath<Material>(KenneySurvivalMaterialPath);
        if (material == null)
            throw new FileNotFoundException("Kenney battaniye materyali bulunamadÄ±.", KenneySurvivalMaterialPath);

        StoryChapterBuilderCommon.InstantiateAsset(
            KenneyBedrollPath,
            "KoltukBattaniyesi",
            sofa,
            new Vector3(-3.35f, 0.57f, -3.6f),
            new Vector3(0.76f, 0.24f, 0.3f),
            new Vector3(0f, -8f, -5f),
            false,
            false,
            material);
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
