using System.Linq;
using UnityEditor;

/// <summary>
/// Yayın sahnelerinin tek kaynağı. Hiçbir builder kendi başına listeyi daraltmaz.
/// </summary>
public static class MinigameSceneCatalog
{
    public const string MainMenuPath = "Assets/Scenes/Story_Rebuild_MainMenu.unity";
    public const string HubPath = "Assets/Scenes/Minigame_Hub.unity";
    public const string FiretruckPath = "Assets/Scenes/Story_04_FiretruckRunner.unity";
    public const string FirefighterExtinguishPath = "Assets/Scenes/Minigame_FirefighterExtinguish.unity";
    public const string Evacuation25DPath = "Assets/Scenes/Minigame_Evacuation_25D.unity";
    public const string AftershockCoverPath = "Assets/Scenes/Minigame_AftershockCover.unity";
    public const string RoomSafetyPath = "Assets/Scenes/Minigame_RoomSafety.unity";
    public const string EmergencyBagRushPath = "Assets/Scenes/Minigame_EmergencyBagRush.unity";
    public const string EmergencyCorridorPath = "Assets/Scenes/Minigame_EmergencyCorridor.unity";
    public const string RubbleSignalPath = "Assets/Scenes/Minigame_RubbleSignal.unity";

    public static readonly string[] OrderedScenePaths =
    {
        MainMenuPath,
        "Assets/Scenes/Story_01_RebuildPreview.unity",
        "Assets/Scenes/Story_02_RebuildPreview.unity",
        "Assets/Scenes/Story_03_RebuildPreview.unity",
        "Assets/Scenes/Story_04_RebuildPreview.unity",
        HubPath,
        FiretruckPath,
        FirefighterExtinguishPath,
        Evacuation25DPath,
        AftershockCoverPath,
        RoomSafetyPath,
        EmergencyBagRushPath,
        EmergencyCorridorPath,
        RubbleSignalPath
    };

    public static void PublishBuildSettings()
    {
        // Scene meta files may have been created earlier in the same editor command.
        // Force the import first, then let Unity resolve each path to its native GUID.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorBuildSettingsScene[] desired = OrderedScenePaths
            .Select(path =>
            {
                string guidText = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrWhiteSpace(guidText) || guidText.All(character => character == '0'))
                    throw new System.InvalidOperationException("Build sahnesi GUID alamadı: " + path);
                EditorBuildSettingsScene scene = new EditorBuildSettingsScene(path, true);
                // Unity 6 can preserve a stale empty m_guid when only the path setter is
                // exercised during a builder command. Assign both serialized fields.
                scene.guid = new GUID(guidText);
                return scene;
            })
            .ToArray();
        // Unity merges entries by path and can retain an old empty native m_guid.
        // Clear the native list first, then republish the authoritative catalog.
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
        EditorBuildSettings.scenes = desired;
        string zeroGuid = new string('0', 32);
        for (int index = 0; index < EditorBuildSettings.scenes.Length; index++)
        {
            EditorBuildSettingsScene published = EditorBuildSettings.scenes[index];
            string expectedGuid = AssetDatabase.AssetPathToGUID(published.path);
            if (published.guid.ToString() == zeroGuid || published.guid.ToString() != expectedGuid)
                throw new System.InvalidOperationException("Build Settings sıfır GUID üretti: " + published.path);
        }
    }
}
