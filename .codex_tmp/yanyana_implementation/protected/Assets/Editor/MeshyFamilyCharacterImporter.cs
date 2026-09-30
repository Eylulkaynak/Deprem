using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MeshyFamilyCharacterImporter
{
    public const string Root = "Assets/Story/Characters/MeshyFamily";
    public const string PrefabRoot = Root + "/Prefabs";
    public const string CorrectedMeshRoot = Root + "/CorrectedMeshes";
    public const string DenizWalkingPath = Root + "/Deniz/Deniz_Walking.fbx";

    private const float SeamPositionScale = 100000f;
    private const float SeamWeightTolerance = 0.0001f;

    private readonly struct PositionKey : IEquatable<PositionKey>
    {
        private readonly int x;
        private readonly int y;
        private readonly int z;

        internal PositionKey(Vector3 position)
        {
            x = Mathf.RoundToInt(position.x * SeamPositionScale);
            y = Mathf.RoundToInt(position.y * SeamPositionScale);
            z = Mathf.RoundToInt(position.z * SeamPositionScale);
        }

        public bool Equals(PositionKey other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is PositionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x;
                hash = (hash * 397) ^ y;
                return (hash * 397) ^ z;
            }
        }
    }

    private sealed class RoleSpec
    {
        internal readonly string role;
        internal readonly string modelPath;
        internal readonly string prefabPath;

        internal RoleSpec(string role)
        {
            this.role = role;
            modelPath = Root + "/" + role + "/" + role + "_Rigged.fbx";
            prefabPath = PrefabRoot + "/" + role + ".prefab";
        }
    }

    private static readonly RoleSpec[] Roles =
    {
        new RoleSpec("Deniz"),
        new RoleSpec("Can"),
        new RoleSpec("Anne"),
        new RoleSpec("Baba"),
        new RoleSpec("Komsu")
    };

    [MenuItem("Tools/Deprem Story/Characters/Prepare Meshy Family")]
    public static void PrepareFromMenu()
    {
        Prepare(true);
    }

    [MenuItem("Tools/Deprem Story/Characters/Prepare Meshy Family (Silent)")]
    public static void PrepareSilent()
    {
        Prepare(false);
    }

    [MenuItem("Tools/Deprem Story/Characters/Validate Meshy Family")]
    public static void ValidateFromMenu()
    {
        Validate(true);
    }

    [MenuItem("Tools/Deprem Story/Characters/Validate Meshy Family In Rebuild Scenes (Silent)")]
    public static void ValidateRebuildScenesSilent()
    {
        string activeScenePath = SceneManager.GetActiveScene().path;
        try
        {
            ValidateSceneCharacters(
                "Assets/Scenes/Story_01_RebuildPreview.unity",
                ("Deniz_12", "Deniz"),
                ("Can_8", "Can"),
                ("Anne_Ayse", "Anne"));
            ValidateSceneCharacters(
                "Assets/Scenes/Story_02_RebuildPreview.unity",
                ("Deniz_12", "Deniz"),
                ("Can_8", "Can"),
                ("Anne_Ayse", "Anne"),
                ("Nermin_Neighbor", "Komsu"));
            ValidateSceneCharacters(
                "Assets/Scenes/Story_03_RebuildPreview.unity",
                ("Deniz_12", "Deniz"),
                ("Can_8", "Can"));
            ValidateSceneCharacters(
                "Assets/Scenes/Story_04_RebuildPreview.unity",
                ("Deniz_12", "Deniz"),
                ("Can_8", "Can"),
                ("Anne_Assembly_Reunion", "Anne"),
                ("Baba_Assembly_Reunion", "Baba"));
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }

        Debug.Log("Story_01–04 Meshy aile sahne doğrulaması başarılı.");
    }

    internal static void EnsurePrepared()
    {
        if (Roles.All(role =>
                AssetDatabase.LoadAssetAtPath<GameObject>(role.prefabPath) != null &&
                AssetDatabase.LoadAssetAtPath<Material>(
                    Root + "/" + role.role + "/" + role.role + "_URP.mat") != null &&
                IsHumanoidModelReady(role.modelPath) &&
                ArePrefabSeamsReady(role.prefabPath)))
        {
            AnimationClip walk = LoadPrimaryAnimationClip(DenizWalkingPath);
            if (walk != null && walk.isHumanMotion)
                return;
        }

        Prepare(false);
    }

    public static void Prepare(bool showDialog)
    {
        EnsureFolder(Root);
        EnsureFolder(PrefabRoot);
        EnsureFolder(CorrectedMeshRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (RoleSpec role in Roles)
        {
            RequireFile(role.modelPath);
            ConfigureTexture(role.role, "BaseColor", false);
            ConfigureTexture(role.role, "Normal", true);
            ConfigureTexture(role.role, "Metallic", false);
            ConfigureTexture(role.role, "Roughness", false);
            ConfigureHumanoidModel(role.modelPath, false, null);
        }

        Avatar denizAvatar = LoadAvatar(Roles[0].modelPath);
        if (denizAvatar == null || !denizAvatar.isValid || !denizAvatar.isHuman)
            throw new InvalidOperationException("Deniz Humanoid Avatar oluşturulamadı.");

        RequireFile(DenizWalkingPath);
        ConfigureHumanoidModel(DenizWalkingPath, true, denizAvatar);

        foreach (RoleSpec role in Roles)
        {
            Material material = CreateOrUpdateMaterial(role.role);
            CreateOrUpdatePrefab(role, material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Validate(false);

        Debug.Log("Meshy aile karakterleri Unity için hazırlandı: Deniz, Can, Anne, Baba, Komşu.");
        if (showDialog)
            EditorUtility.DisplayDialog(
                "Deprem Story",
                "Meshy aile karakterleri Humanoid rig, URP materyal ve prefablarıyla hazır.",
                "Tamam");
    }

    public static void Validate(bool showDialog)
    {
        List<string> failures = new List<string>();
        foreach (RoleSpec role in Roles)
        {
            Avatar avatar = LoadAvatar(role.modelPath);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                failures.Add(role.role + ": geçerli Humanoid Avatar yok");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(role.prefabPath);
            if (prefab == null)
            {
                failures.Add(role.role + ": prefab yok");
                continue;
            }

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                failures.Add(role.role + ": prefab Animator Humanoid değil");

            Transform visual = prefab.transform.Find("Visual");
            if (visual == null ||
                Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.y, 180f)) > 0.1f)
                failures.Add(role.role + ": visual forward correction is missing");

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                failures.Add(role.role + ": görünür renderer yok");
            else if (renderers.Any(renderer =>
                         renderer.sharedMaterials.Length == 0 ||
                         renderer.sharedMaterials.Any(material =>
                             material == null ||
                             material.shader == null ||
                             material.shader.name != "Universal Render Pipeline/Lit")))
                failures.Add(role.role + ": URP/Lit materyal ataması eksik");

            int mismatchedSeams = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.sharedMesh != null)
                .Sum(renderer => CountMismatchedSeamGroups(renderer.sharedMesh));
            if (mismatchedSeams > 0)
                failures.Add(role.role + ": " + mismatchedSeams +
                             " skin-weight dikişi animasyonda ayrılabilir");
        }

        AnimationClip walk = LoadPrimaryAnimationClip(DenizWalkingPath);
        if (walk == null || !walk.isHumanMotion)
            failures.Add("Deniz: Humanoid yürüyüş klibi yok");

        if (failures.Count > 0)
            throw new InvalidOperationException("Meshy aile doğrulama hatası:\n- " + string.Join("\n- ", failures));

        Debug.Log("Meshy aile doğrulandı: 5 Humanoid prefab ve Humanoid Deniz yürüyüş klibi.");
        if (showDialog)
            EditorUtility.DisplayDialog("Deprem Story", "Meshy aile doğrulaması başarılı.", "Tamam");
    }

    internal static AnimationClip LoadPrimaryAnimationClip(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(clip => clip.length)
            .FirstOrDefault();
    }

    private static void ConfigureHumanoidModel(string assetPath, bool importAnimation, Avatar sourceAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("ModelImporter bulunamadı: " + assetPath);

        ModelImporterAvatarSetup avatarSetup = sourceAvatar == null
            ? ModelImporterAvatarSetup.CreateFromThisModel
            : ModelImporterAvatarSetup.CopyFromOther;
        bool changed =
            importer.animationType != ModelImporterAnimationType.Human ||
            importer.avatarSetup != avatarSetup ||
            importer.importAnimation != importAnimation ||
            importer.importCameras ||
            importer.importLights ||
            importer.materialImportMode != ModelImporterMaterialImportMode.None ||
            (sourceAvatar != null && importer.sourceAvatar != sourceAvatar);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = avatarSetup;
        importer.sourceAvatar = sourceAvatar;
        importer.importAnimation = importAnimation;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;

        if (importAnimation)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.keepOriginalOrientation = true;
                // DİKKAT: heightFromFeet bu rig'lerde KULLANILAMAZ. Meshy avatar'ının
                // ayak taban verisi bozuk (feetBottomHeight abartılı); ayak-tabanlı kök
                // hesabı karakteri yanlış yüksekliğe oturtup Foot IK ile bilekleri
                // 180 derece büküyor. Orijinal kök korunur; küçük zemin farkı
                // GroundCharacterFromHumanoidFeet + agent.baseOffset ile telafi edilir.
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.heightFromFeet = false;
            }
            importer.clipAnimations = clips;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string role, string suffix, bool normalMap)
    {
        string path = Root + "/" + role + "/" + role + "_" + suffix + ".png";
        RequireFile(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("TextureImporter bulunamadı: " + path);

        TextureImporterType targetType = normalMap
            ? TextureImporterType.NormalMap
            : TextureImporterType.Default;
        bool linear = suffix == "Metallic" || suffix == "Roughness" || normalMap;
        bool changed =
            importer.textureType != targetType ||
            importer.sRGBTexture == linear ||
            !importer.mipmapEnabled ||
            !importer.streamingMipmaps ||
            importer.maxTextureSize != 2048 ||
            importer.textureCompression != TextureImporterCompression.CompressedHQ;

        importer.textureType = targetType;
        importer.sRGBTexture = !linear;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        importer.alphaIsTransparency = false;
        if (changed)
            importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(string role)
    {
        string roleRoot = Root + "/" + role;
        string materialPath = roleRoot + "/" + role + "_URP.mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit shader bulunamadı.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = role + "_URP" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
            material.shader = shader;

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(roleRoot + "/" + role + "_BaseColor.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(roleRoot + "/" + role + "_Normal.png");
        material.SetTexture("_BaseMap", baseColor);
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 0.55f);
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", role == "Deniz" || role == "Can" ? 0.24f : 0.2f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateOrUpdatePrefab(RoleSpec role, Material material)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(role.modelPath);
        if (source == null)
            throw new InvalidOperationException("Karakter modeli yüklenemedi: " + role.modelPath);

        GameObject root = new GameObject(role.role);
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            // Meshy family meshes are authored looking down local -Z while Unity
            // navigation and LookRotation use +Z as forward. Correct that once on
            // the reusable visual root so dialogue facing and locomotion agree.
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
                animator.avatar = LoadAvatar(role.modelPath);
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            SkinnedMeshRenderer[] skinnedRenderers =
                visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < skinnedRenderers.Length; index++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[index];
                renderer.sharedMesh = CreateOrUpdateSeamFixedMesh(role, renderer.sharedMesh, index);
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                int materialCount = Math.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, materialCount).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            PrefabUtility.SaveAsPrefabAsset(root, role.prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static Mesh CreateOrUpdateSeamFixedMesh(RoleSpec role, Mesh sourceMesh, int meshIndex)
    {
        if (sourceMesh == null)
            throw new InvalidOperationException(role.role + ": SkinnedMeshRenderer mesh is missing.");

        Mesh repairedMesh = BuildSeamFixedMesh(sourceMesh, out int repairedGroupCount);
        if (repairedMesh == null)
            return sourceMesh;

        repairedMesh.name = role.role + "_SeamFixed" + (meshIndex == 0 ? string.Empty : "_" + meshIndex);
        string assetPath = CorrectedMeshRoot + "/" + repairedMesh.name + ".asset";
        Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (savedMesh == null)
        {
            AssetDatabase.CreateAsset(repairedMesh, assetPath);
            savedMesh = repairedMesh;
        }
        else
        {
            EditorUtility.CopySerialized(repairedMesh, savedMesh);
            savedMesh.name = repairedMesh.name;
            Object.DestroyImmediate(repairedMesh);
            EditorUtility.SetDirty(savedMesh);
        }

        Debug.Log(role.role + ": repaired " + repairedGroupCount +
                  " mismatched skin-weight seam groups in " + savedMesh.name + ".");
        return savedMesh;
    }

    internal static Mesh BuildSeamFixedMesh(Mesh sourceMesh, out int repairedGroupCount)
    {
        Vector3[] positions = sourceMesh.vertices;
        List<BoneWeight1>[] vertexWeights = ReadVertexWeights(sourceMesh);
        Dictionary<PositionKey, List<int>> groups = BuildPositionGroups(positions);

        repairedGroupCount = 0;
        foreach (List<int> indices in groups.Values)
        {
            if (indices.Count < 2 || !HasMismatchedWeights(indices, vertexWeights))
                continue;

            List<BoneWeight1> sharedWeights = AverageWeights(indices, vertexWeights);
            foreach (int index in indices)
                vertexWeights[index] = sharedWeights;
            repairedGroupCount++;
        }

        if (repairedGroupCount == 0)
            return null;

        Mesh repairedMesh = Object.Instantiate(sourceMesh);
        ApplyVertexWeights(repairedMesh, vertexWeights);
        return repairedMesh;
    }

    private static int CountMismatchedSeamGroups(Mesh mesh)
    {
        List<BoneWeight1>[] vertexWeights = ReadVertexWeights(mesh);
        return BuildPositionGroups(mesh.vertices).Values.Count(indices =>
            indices.Count > 1 && HasMismatchedWeights(indices, vertexWeights));
    }

    private static bool ArePrefabSeamsReady(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return false;

        return prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.sharedMesh != null)
            .All(renderer => CountMismatchedSeamGroups(renderer.sharedMesh) == 0);
    }

    private static Dictionary<PositionKey, List<int>> BuildPositionGroups(
        IReadOnlyList<Vector3> positions)
    {
        Dictionary<PositionKey, List<int>> groups = new Dictionary<PositionKey, List<int>>();
        for (int index = 0; index < positions.Count; index++)
        {
            PositionKey key = new PositionKey(positions[index]);
            if (!groups.TryGetValue(key, out List<int> indices))
            {
                indices = new List<int>();
                groups.Add(key, indices);
            }
            indices.Add(index);
        }
        return groups;
    }

    private static List<BoneWeight1>[] ReadVertexWeights(Mesh mesh)
    {
        List<BoneWeight1>[] result = new List<BoneWeight1>[mesh.vertexCount];
        using (NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex())
        using (NativeArray<BoneWeight1> allWeights = mesh.GetAllBoneWeights())
        {
            int weightIndex = 0;
            for (int vertexIndex = 0; vertexIndex < result.Length; vertexIndex++)
            {
                int count = bonesPerVertex[vertexIndex];
                List<BoneWeight1> weights = new List<BoneWeight1>(count);
                for (int influenceIndex = 0; influenceIndex < count; influenceIndex++)
                    weights.Add(allWeights[weightIndex++]);
                result[vertexIndex] = weights;
            }
        }

        return result;
    }

    private static bool HasMismatchedWeights(
        IReadOnlyList<int> indices,
        IReadOnlyList<List<BoneWeight1>> vertexWeights)
    {
        List<BoneWeight1> baseline = vertexWeights[indices[0]];
        for (int index = 1; index < indices.Count; index++)
        {
            if (WeightDifference(baseline, vertexWeights[indices[index]]) > SeamWeightTolerance)
                return true;
        }
        return false;
    }

    private static float WeightDifference(
        IReadOnlyList<BoneWeight1> first,
        IReadOnlyList<BoneWeight1> second)
    {
        Dictionary<int, float> difference = new Dictionary<int, float>();
        foreach (BoneWeight1 weight in first)
            difference[weight.boneIndex] = weight.weight;
        foreach (BoneWeight1 weight in second)
        {
            difference.TryGetValue(weight.boneIndex, out float current);
            difference[weight.boneIndex] = current - weight.weight;
        }
        return difference.Values.Sum(Mathf.Abs);
    }

    private static List<BoneWeight1> AverageWeights(
        IReadOnlyList<int> indices,
        IReadOnlyList<List<BoneWeight1>> vertexWeights)
    {
        Dictionary<int, float> totals = new Dictionary<int, float>();
        int maximumSourceInfluences = 0;
        foreach (int index in indices)
        {
            List<BoneWeight1> weights = vertexWeights[index];
            maximumSourceInfluences = Math.Max(maximumSourceInfluences, weights.Count);
            foreach (BoneWeight1 weight in weights)
            {
                totals.TryGetValue(weight.boneIndex, out float current);
                totals[weight.boneIndex] = current + weight.weight;
            }
        }

        int targetInfluenceCount = Math.Min(
            totals.Count,
            Math.Max(maximumSourceInfluences, Math.Min(4, totals.Count)));
        KeyValuePair<int, float>[] selected = totals
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(targetInfluenceCount)
            .ToArray();
        float totalWeight = selected.Sum(pair => pair.Value);
        if (totalWeight <= 0f)
            throw new InvalidOperationException("Skin-weight seam group has no valid weight.");

        return selected
            .Select(pair => new BoneWeight1
            {
                boneIndex = pair.Key,
                weight = pair.Value / totalWeight
            })
            .ToList();
    }

    private static void ApplyVertexWeights(
        Mesh mesh,
        IReadOnlyList<List<BoneWeight1>> vertexWeights)
    {
        byte[] bonesPerVertex = new byte[vertexWeights.Count];
        List<BoneWeight1> allWeights = new List<BoneWeight1>();
        for (int index = 0; index < vertexWeights.Count; index++)
        {
            List<BoneWeight1> weights = vertexWeights[index];
            if (weights.Count > byte.MaxValue)
                throw new InvalidOperationException("A vertex has more than 255 bone influences.");
            bonesPerVertex[index] = (byte)weights.Count;
            allWeights.AddRange(weights);
        }

        using (NativeArray<byte> nativeCounts =
               new NativeArray<byte>(bonesPerVertex, Allocator.Temp))
        using (NativeArray<BoneWeight1> nativeWeights =
               new NativeArray<BoneWeight1>(allWeights.ToArray(), Allocator.Temp))
        {
            mesh.SetBoneWeights(nativeCounts, nativeWeights);
        }
    }

    private static Avatar LoadAvatar(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
    }

    private static bool IsHumanoidModelReady(string modelPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        Avatar avatar = LoadAvatar(modelPath);
        return importer != null &&
               importer.animationType == ModelImporterAnimationType.Human &&
               avatar != null &&
               avatar.isValid &&
               avatar.isHuman;
    }

    private static void RequireFile(string assetPath)
    {
        string absolute = Path.GetFullPath(assetPath);
        if (!File.Exists(absolute))
            throw new FileNotFoundException("Meshy aile kaynağı bulunamadı.", assetPath);
    }

    private static void ValidateSceneCharacters(
        string scenePath,
        params (string objectName, string sourceRole)[] expectedCharacters)
    {
        if (!File.Exists(scenePath))
            throw new FileNotFoundException("Rebuild sahnesi bulunamadı.", scenePath);

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Transform[] transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
        foreach ((string objectName, string sourceRole) expected in expectedCharacters)
        {
            Transform character = transforms.FirstOrDefault(candidate => candidate.name == expected.objectName);
            if (character == null)
                throw new InvalidOperationException(scenePath + ": karakter bulunamadı: " + expected.objectName);

            string sourceMarkerName = "CharacterSource_" + expected.sourceRole;
            Transform sourceMarker = character.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == sourceMarkerName);
            if (sourceMarker == null)
                throw new InvalidOperationException(
                    scenePath + ": " + expected.objectName + " kaynağı " + sourceMarkerName + " değil.");

            Animator animator = character.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException(
                    scenePath + ": " + expected.objectName + " Humanoid Animator doğrulanamadı.");

            Transform visual = character.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "Visual");
            if (visual == null ||
                Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.y, 180f)) > 0.1f)
                throw new InvalidOperationException(
                    scenePath + ": " + expected.objectName + " visual forward correction is missing.");
            Renderer[] renderers = visual != null
                ? visual.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            Material[] materials = renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .ToArray();
            if (renderers.Length == 0 ||
                materials.Length == 0 ||
                materials.All(material =>
                    !material.name.StartsWith(expected.sourceRole + "_", StringComparison.Ordinal)) ||
                materials.Any(material => material.shader == null ||
                                          material.shader.name != "Universal Render Pipeline/Lit"))
                throw new InvalidOperationException(
                    scenePath + ": " + expected.objectName + " URP karakter materyali doğrulanamadı.");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folder))
            throw new InvalidOperationException("Klasör yolu geçersiz: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
