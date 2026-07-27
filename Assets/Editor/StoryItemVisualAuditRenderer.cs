using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StoryItemVisualAuditRenderer
{
    private const string OutputRoot = "Temp/StoryItemAudit";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Story_01_RebuildPreview.unity",
        "Assets/Scenes/Story_02_RebuildPreview.unity",
        "Assets/Scenes/Story_03_RebuildPreview.unity",
        "Assets/Scenes/Story_04_RebuildPreview.unity"
    };

    [MenuItem("Tools/Deprem Story/QA/Render Rebuild Item Audit (Silent)")]
    public static void RenderAll()
    {
        string activeScenePath = SceneManager.GetActiveScene().path;
        Directory.CreateDirectory(OutputRoot);
        try
        {
            foreach (string scenePath in ScenePaths)
                RenderScene(scenePath);
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }

        Debug.Log("ITEM_VISUAL_AUDIT_READY " + Path.GetFullPath(OutputRoot));
    }

    private static void RenderScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        DraggableItem[] items = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<DraggableItem>(true))
            .Where(item => item != null && VisibleRenderers(item.gameObject).Length > 0)
            .OrderBy(item => item.gameObject.name, StringComparer.Ordinal)
            .ToArray();
        if (items.Length == 0)
            throw new InvalidOperationException(scenePath + " içinde görsel DraggableItem bulunamadı.");

        int columns = Mathf.CeilToInt(Mathf.Sqrt(items.Length));
        int rows = Mathf.CeilToInt(items.Length / (float)columns);
        const float cellWidth = 1.7f;
        const float cellDepth = 1.65f;
        List<string> indexLines = new List<string>
        {
            Path.GetFileNameWithoutExtension(scenePath) + " item visual audit",
            "Grid: row,col -> object | world rotation | original bounds"
        };

        PreviewRenderUtility preview = new PreviewRenderUtility();
        try
        {
            Material pedestalMaterial = CreatePreviewMaterial(new Color32(205, 214, 216, 255), 0.16f);
            Material groundMaterial = CreatePreviewMaterial(new Color32(48, 60, 70, 255), 0.08f);
            for (int index = 0; index < items.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                Vector3 cellCenter = new Vector3(
                    (column - (columns - 1) * 0.5f) * cellWidth,
                    0f,
                    (rows - 1 - row - (rows - 1) * 0.5f) * cellDepth);

                GameObject visualSource = ResolveVisualSource(items[index].gameObject);
                GameObject clone = Object.Instantiate(visualSource);
                clone.name = items[index].gameObject.name;
                DisableAuditNoise(clone);
                Bounds originalBounds = CalculateBounds(clone);
                Quaternion originalRotation = clone.transform.rotation;

                float largest = Mathf.Max(
                    originalBounds.size.x,
                    Mathf.Max(originalBounds.size.y, originalBounds.size.z));
                float scale = 0.88f / Mathf.Max(0.01f, largest);
                clone.transform.localScale *= scale;
                Bounds scaledBounds = CalculateBounds(clone);
                clone.transform.position +=
                    cellCenter + Vector3.up * 0.14f -
                    new Vector3(scaledBounds.center.x, scaledBounds.min.y, scaledBounds.center.z);
                preview.AddSingleGO(clone);

                GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedestal.name = "Pedestal_" + index;
                pedestal.transform.position = cellCenter + Vector3.up * 0.06f;
                pedestal.transform.localScale = new Vector3(1.2f, 0.12f, 1.1f);
                pedestal.GetComponent<Renderer>().sharedMaterial = pedestalMaterial;
                Object.DestroyImmediate(pedestal.GetComponent<Collider>());
                preview.AddSingleGO(pedestal);

                Vector3 euler = NormalizeEuler(originalRotation.eulerAngles);
                string sourceInfo = SourcePrefabInfo(items[index].gameObject);
                indexLines.Add(
                    $"{row + 1},{column + 1} -> {items[index].gameObject.name} | " +
                    $"rot=({euler.x:F0},{euler.y:F0},{euler.z:F0}) | " +
                    $"bounds=({originalBounds.size.x:F2},{originalBounds.size.y:F2},{originalBounds.size.z:F2}) | " +
                    sourceInfo);
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "AuditGround";
            ground.transform.position = new Vector3(0f, -0.035f, 0f);
            ground.transform.localScale = new Vector3(
                columns * cellWidth + 0.45f,
                0.07f,
                rows * cellDepth + 0.45f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            preview.AddSingleGO(ground);

            float width = columns * cellWidth;
            float depth = rows * cellDepth;
            Vector3 focus = new Vector3(0f, 0.25f, 0f);
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = Mathf.Max(depth * 0.68f, width * 0.48f);
            preview.camera.transform.position =
                focus + new Vector3(0f, Mathf.Max(width, depth) * 1.05f, -Mathf.Max(width, depth) * 0.78f);
            preview.camera.transform.rotation =
                Quaternion.LookRotation(focus - preview.camera.transform.position, Vector3.up);
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 100f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color32(25, 34, 43, 255);
            preview.ambientColor = new Color32(120, 132, 140, 255);
            preview.lights[0].intensity = 1.25f;
            preview.lights[0].transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            preview.lights[1].intensity = 0.7f;
            preview.lights[1].transform.rotation = Quaternion.Euler(28f, 145f, 0f);

            preview.BeginStaticPreview(new Rect(0f, 0f, 2048f, 2048f));
            preview.Render(true);
            Texture2D image = preview.EndStaticPreview();
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            File.WriteAllBytes(Path.Combine(OutputRoot, sceneName + "_Items.png"), image.EncodeToPNG());
            File.WriteAllLines(Path.Combine(OutputRoot, sceneName + "_Items.txt"), indexLines);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(pedestalMaterial);
            Object.DestroyImmediate(groundMaterial);
        }
        finally
        {
            preview.Cleanup();
        }

        CaptureSceneOverview(scene, Path.GetFileNameWithoutExtension(scenePath));
    }

    private static Renderer[] VisibleRenderers(GameObject root)
    {
        return root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer =>
                renderer != null &&
                renderer.enabled &&
                !renderer.name.Contains("Marker", StringComparison.OrdinalIgnoreCase) &&
                !renderer.name.Contains("Indicator", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static GameObject ResolveVisualSource(GameObject interactionRoot)
    {
        foreach (Renderer renderer in VisibleRenderers(interactionRoot))
        {
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
            if (prefabRoot != null && prefabRoot.transform.IsChildOf(interactionRoot.transform))
                return prefabRoot;
        }

        return interactionRoot;
    }

    private static void DisableAuditNoise(GameObject root)
    {
        root.SetActive(true);
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name.Contains("Marker", StringComparison.OrdinalIgnoreCase) ||
                renderer.name.Contains("Indicator", StringComparison.OrdinalIgnoreCase))
                renderer.enabled = false;
        }
    }

    private static string SourcePrefabInfo(GameObject root)
    {
        Renderer renderer = VisibleRenderers(root).FirstOrDefault();
        if (renderer == null)
            return "source=<generated>";

        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject);
        if (string.IsNullOrEmpty(path))
            return "source=<generated>";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return "source=" + path;

        Vector3 euler = NormalizeEuler(prefab.transform.localEulerAngles);
        Vector3 scale = prefab.transform.localScale;
        return $"source={path} authoredRot=({euler.x:F0},{euler.y:F0},{euler.z:F0}) " +
               $"authoredScale=({scale.x:F2},{scale.y:F2},{scale.z:F2})";
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = VisibleRenderers(root);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 0.1f);

        bool initialized = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
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
        return bounds;
    }

    private static Material CreatePreviewMaterial(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static void CaptureSceneOverview(Scene scene, string sceneName)
    {
        Camera camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"));
        if (camera == null)
            return;

        RenderTexture target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            camera.targetTexture = target;
            camera.aspect = 1080f / 1920f;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(OutputRoot, sceneName + "_Overview.png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180f ? angle - 360f : angle;
    }
}
