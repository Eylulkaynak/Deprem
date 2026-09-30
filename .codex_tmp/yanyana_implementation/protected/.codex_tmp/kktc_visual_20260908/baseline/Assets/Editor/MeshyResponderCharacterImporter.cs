using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshyResponderCharacterImporter
{
    internal const string Root = "Assets/Story/Characters/MeshyResponders";
    internal const string PrefabRoot = Root + "/Prefabs";

    private static readonly string[] Roles = { "Firefighter", "RescueWorker", "Police" };

    [MenuItem("Tools/Deprem Story/Characters/Prepare Meshy Responders")]
    public static void Prepare()
    {
        PrepareSilent();
        EditorUtility.DisplayDialog(
            "Deprem Story",
            "İtfaiyeci, AFAD çalışanı ve polis Humanoid prefabları hazırlandı.",
            "Tamam");
    }

    [MenuItem("Tools/Deprem Story/Characters/Prepare Meshy Responders (Silent)")]
    public static void PrepareSilent()
    {
        EnsureAssetFolder(Root);
        EnsureAssetFolder(PrefabRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string role in Roles)
        {
            string roleRoot = Root + "/" + role;
            string modelPath = roleRoot + "/" + role + ".fbx";
            string baseColorPath = roleRoot + "/" + role + "_BaseColor.png";
            string normalPath = roleRoot + "/" + role + "_Normal.png";

            RequireFile(modelPath, "Meshy responder model is missing.");
            RequireFile(baseColorPath, "Meshy responder base-color texture is missing.");

            ConfigureModel(modelPath);
            ConfigureTexture(baseColorPath, false);
            if (File.Exists(normalPath))
                ConfigureTexture(normalPath, true);

            Material material = CreateOrUpdateMaterial(role, roleRoot, normalPath);
            CreateOrUpdatePrefab(role, modelPath, material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePreparedAssets();
        Debug.Log("MESHY_RESPONDERS_READY roles=Firefighter,RescueWorker,Police humanoid=true prefabs=3");
    }

    [MenuItem("Tools/Deprem Story/Characters/Validate Meshy Responders")]
    public static void ValidatePreparedAssets()
    {
        var failures = new List<string>();
        foreach (string role in Roles)
        {
            string roleRoot = Root + "/" + role;
            string modelPath = roleRoot + "/" + role + ".fbx";
            string materialPath = roleRoot + "/" + role + "_URP.mat";
            string prefabPath = PrefabRoot + "/" + role + ".prefab";

            Avatar avatar = LoadAvatar(modelPath);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                failures.Add(role + ": valid Humanoid Avatar was not generated");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.shader == null ||
                material.shader.name != "Universal Render Pipeline/Lit")
                failures.Add(role + ": URP/Lit material is missing");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Animator animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
            Renderer renderer = prefab != null ? prefab.GetComponentInChildren<Renderer>(true) : null;
            if (prefab == null)
                failures.Add(role + ": prefab is missing");
            else if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                failures.Add(role + ": prefab Humanoid Animator is missing");
            if (prefab != null && (renderer == null || renderer.sharedMaterial != material))
                failures.Add(role + ": prefab material assignment is missing");
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("Meshy responder validation failed:\n- " + string.Join("\n- ", failures));

        Debug.Log("MESHY_RESPONDERS_VALID roles=3 avatars=3 prefabs=3 materials=3");
    }

    private static void ConfigureModel(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("ModelImporter was not found: " + path);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.isReadable = false;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path, bool normalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("TextureImporter was not found: " + path);

        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(string role, string roleRoot, string normalPath)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit shader was not found.");

        string materialPath = roleRoot + "/" + role + "_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = role + "_URP" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
            material.shader = shader;

        material.SetTexture("_BaseMap",
            AssetDatabase.LoadAssetAtPath<Texture2D>(roleRoot + "/" + role + "_BaseColor.png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.2f);

        Texture2D normal = File.Exists(normalPath)
            ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)
            : null;
        material.SetTexture("_BumpMap", normal);
        if (normal != null)
        {
            material.SetFloat("_BumpScale", 0.45f);
            material.EnableKeyword("_NORMALMAP");
        }
        else
            material.DisableKeyword("_NORMALMAP");

        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateOrUpdatePrefab(string role, string modelPath, Material material)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        Avatar avatar = LoadAvatar(modelPath);
        if (source == null || avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("A valid Humanoid responder model could not be loaded: " + modelPath);

        GameObject root = new GameObject(role);
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = visual.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                int count = Math.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = false;
            }

            string prefabPath = PrefabRoot + "/" + role + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Avatar LoadAvatar(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault();
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid Unity asset folder: " + path);

        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void RequireFile(string path, string message)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(message, path);
    }
}
