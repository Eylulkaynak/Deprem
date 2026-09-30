using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StoryCharacterPackageImporter
{
    private const string TownPackageName = "POLYGON_Town_Unity_2021_3_v1_8_5.unitypackage";
    private const string CityCharactersPackageName = "POLYGON_City_Characters_Unity_2021_3_v1_2_1.unitypackage";
    private static readonly string[] ImportedMaterialRoots =
    {
        "Assets/PolygonTown/Materials",
        "Assets/POLYGONCityCharacters/Materials"
    };
    private const string CuratedTownEnvironmentRoot = "Assets/Story/Environment/SyntyTown";
    private static readonly string[] CuratedTownEnvironmentModels =
    {
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_Preset_04.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_Preset_06.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_Door_01.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_Interior_Stairs_01.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_Interior_StairsRail_02.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_InteriorWall_01.fbx",
        "Assets/PolygonTown/Models/Buildings/SM_Bld_House_InteriorWall_Door_01.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Bush_01.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Bush_02.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Fence_White_Post_01.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Fence_White_Straight_01.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Tree_01.fbx",
        "Assets/PolygonTown/Models/Environment/SM_Env_Tree_02.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_ParkBench_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Bookshelf_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_CardboardBox_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_CeilingExit_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_CeilingLight_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Clock_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_FloorMat_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_LetterBox_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Lightswitch_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_PotPlant_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Rug_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_RubbishBin_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Sign_BusStop_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_Streetlamp_01.fbx",
        "Assets/PolygonTown/Models/Props/SM_Prop_StreetSign_Arrow_01.fbx",
        "Assets/PolygonTown/Models/Vehicles/SM_Veh_Firetruck_01.fbx",
        "Assets/PolygonTown/Models/Vehicles/SM_Veh_Pickup_01.fbx"
    };
    private static readonly string[] UnusedTownAssetRoots =
    {
        "Assets/PolygonTown/Scenes",
        "Assets/PolygonTown/Models/Buildings",
        "Assets/PolygonTown/Models/CollisionConvex",
        "Assets/PolygonTown/Models/CollisionCustom",
        "Assets/PolygonTown/Models/Environment",
        "Assets/PolygonTown/Models/Generic",
        "Assets/PolygonTown/Models/Items",
        "Assets/PolygonTown/Models/Props",
        "Assets/PolygonTown/Models/Vehicles",
        "Assets/PolygonTown/Prefabs/Buildings",
        "Assets/PolygonTown/Prefabs/Environment",
        "Assets/PolygonTown/Prefabs/Generic",
        "Assets/PolygonTown/Prefabs/Items",
        "Assets/PolygonTown/Prefabs/Props",
        "Assets/PolygonTown/Prefabs/Vehicles"
    };

    [MenuItem("Tools/Deprem/Story/Import Downloaded Character Packages")]
    public static void ImportDownloadedCharacterPackages()
    {
        string downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        ImportPackage(Path.Combine(downloads, TownPackageName));
        ImportPackage(Path.Combine(downloads, CityCharactersPackageName));
        PruneTownToCharacterAssets();
        ConvertImportedMaterialsToUrp();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log(
            "Story character packages imported. " +
            "Primary family: PolygonTown. Supporting adults: POLYGONCityCharacters.");
    }

    [MenuItem("Tools/Deprem/Story/Prepare Imported Character Packages")]
    public static void PrepareImportedCharacterPackages()
    {
        PruneTownToCharacterAssets();
        ConvertImportedMaterialsToUrp();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Imported Synty character materials prepared for URP.");
    }

    [MenuItem("Tools/Deprem/Story/Prune Imported Packages To Character Assets")]
    public static void PruneImportedPackagesToCharacterAssets()
    {
        PruneTownToCharacterAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Unused POLYGON Town environment content removed; character dependencies retained.");
    }

    private static void ImportPackage(string packagePath)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(
                $"Downloaded Unity package was not found: {packagePath}",
                packagePath);
        }

        AssetDatabase.ImportPackage(packagePath, false);
    }

    private static void ConvertImportedMaterialsToUrp()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            throw new InvalidOperationException("URP/Lit shader was not found.");

        foreach (string guid in AssetDatabase.FindAssets("t:Material", ImportedMaterialRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == urpLit)
                continue;

            Texture baseMap = material.HasProperty("_MainTex")
                ? material.GetTexture("_MainTex")
                : null;
            Color baseColor = material.HasProperty("_Color")
                ? material.GetColor("_Color")
                : Color.white;
            Texture normalMap = material.HasProperty("_BumpMap")
                ? material.GetTexture("_BumpMap")
                : null;
            float metallic = material.HasProperty("_Metallic")
                ? material.GetFloat("_Metallic")
                : 0f;
            float smoothness = material.HasProperty("_Glossiness")
                ? material.GetFloat("_Glossiness")
                : 0.2f;

            material.shader = urpLit;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseMap);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_BumpMap"))
                material.SetTexture("_BumpMap", normalMap);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
    }

    private static void PruneTownToCharacterAssets()
    {
        PreserveCuratedTownEnvironmentModels();

        foreach (string path in UnusedTownAssetRoots)
        {
            if (AssetDatabase.IsValidFolder(path) && !AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException("Unused Synty asset folder could not be removed: " + path);
        }
    }

    private static void PreserveCuratedTownEnvironmentModels()
    {
        EnsureAssetFolder(CuratedTownEnvironmentRoot);

        foreach (string sourcePath in CuratedTownEnvironmentModels)
        {
            UnityEngine.Object source = AssetDatabase.LoadMainAssetAtPath(sourcePath);
            if (source == null)
                continue;

            string destinationPath = CuratedTownEnvironmentRoot + "/" + Path.GetFileName(sourcePath);
            if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
            {
                AssetDatabase.DeleteAsset(sourcePath);
                continue;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(
                    "Curated Synty Town model could not be retained: " + sourcePath + ". " + error);
        }
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }
}
