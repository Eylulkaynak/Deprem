using System;
using System.IO;
using System.Linq;
using Deprem.Learning;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deprem.Learning.Editor
{
    public static class LearningAppBuilder
    {
        public const string ScenePath = "Assets/LearningApp/Scenes/Deprem_App.unity";
        private const string PanelPath = "Assets/LearningApp/Resources/LearningApp/PanelSettings.asset";
        private const string FontPath = "Assets/Fonts/StoryPlayful/Nunito-Regular.ttf";

        [MenuItem("Tools/Deprem App/Create or Update Native App Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before creating the scene.");
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/LearningApp/Scenes");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(540, 960);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 0;
            const string themePath = "Assets/LearningApp/Resources/LearningApp/AppTheme.tss";
            if (!File.Exists(themePath)) File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath); panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            EditorUtility.SetDirty(panel);
            // Additive authoring preserves any unrelated open and unsaved scene.
            Scene prior = SceneManager.GetActiveScene();
            bool replaceEmpty = string.IsNullOrEmpty(prior.path) && !prior.isDirty;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var cameraObject = new GameObject("App Camera"); var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.93f, 0.96f, 0.99f); camera.cullingMask = 0; cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
                var app = new GameObject("Deprem Native Learning App"); var document = app.AddComponent<UIDocument>();
                document.panelSettings = panel; document.visualTreeAsset = Resources.Load<VisualTreeAsset>("LearningApp/App");
                var controller = app.AddComponent<LearningAppController>();
                var serialized = new SerializedObject(controller); serialized.FindProperty("bodyFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath); serialized.ApplyModifiedPropertiesWithoutUndo();
                var events = new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (!replaceEmpty) EditorSceneManager.CloseScene(scene, true);
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
            }
            ConfigureBuildScenes(); AssetDatabase.SaveAssets();
            Debug.Log("[Deprem App] Native app scene created. 49 courses, 8 mini-games, 3D scene navigation.");
        }

        [MenuItem("Tools/Deprem App/Open Native App")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) BuildScene();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/Deprem App/Configure Mobile Build")]
        public static void ConfigureMobile()
        {
            ConfigureBuildScenes();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AssetDatabase.SaveAssets(); Debug.Log("[Deprem App] App is build scene 0; mobile orientation is portrait.");
        }

        public static void ConfigureBuildScenes()
        {
            string[] required = { ScenePath, "Assets/YanYana/Scenes/YanYana_Adventure.unity", "Assets/Scenes/Story_Rebuild_MainMenu.unity", "Assets/Scenes/Minigame_Hub.unity" };
            foreach (string scene in required) if (!File.Exists(scene)) throw new FileNotFoundException("Missing integrated scene", scene);
            var existing = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            foreach (string scene in required.Skip(1))
            {
                var entry = existing.Find(s => s.path == scene);
                if (entry == null) existing.Add(new EditorBuildSettingsScene(scene, true)); else entry.enabled = true;
            }
            existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = existing.ToArray();
        }

        [MenuItem("Tools/Deprem App/Validate Native App")]
        public static void Validate()
        {
            var data = LearningCatalog.Load();
            if (data.courses.Length != 49 || data.Levels(false).Length != 8 || data.Levels(true).Length != 8) throw new InvalidDataException("Learning catalog is incomplete.");
            foreach (var course in data.courses)
                foreach (var exercise in course.exercises)
                    if (exercise.kind != "info" && (exercise.choices == null || exercise.choices.Count(c => c.correct) != 1)) throw new InvalidDataException("Invalid question: " + course.id);
            foreach (var art in data.art) if (Resources.Load<Texture2D>(art.resource) == null) throw new FileNotFoundException("Missing art: " + art.key);
            if (EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path != ScenePath) throw new InvalidDataException("Deprem_App must be the first enabled scene.");
            Debug.Log("[Deprem App] VALIDATED: 49 courses, 8 mini-games in both modes, all art, native startup scene.");
        }

        [MenuItem("Tools/Deprem App/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support (SDK, NDK and OpenJDK) for Unity 6000.0.58f2 in Unity Hub first.");
            ConfigureMobile(); Validate(); Directory.CreateDirectory("Builds/DepremApp");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/DepremApp/DepremApp.apk", target = BuildTarget.Android, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
        }
    }

    public sealed class LearningArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/LearningApp/Resources/LearningApp/Art/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.isReadable = false; importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/LearningApp/", StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter; var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming; settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f; importer.defaultSampleSettings = settings;
        }
    }
}
