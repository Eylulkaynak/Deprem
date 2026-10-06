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
            string output = Environment.GetEnvironmentVariable("DEPREM_ANDROID_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = "Builds/DepremApp/DepremApp.apk";
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool appBundle = EditorUserBuildSettings.buildAppBundle;
            bool exportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            MobileTextureSubtarget textureFormat = EditorUserBuildSettings.androidBuildSubtarget;
            bool expansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
            bool customKeystore = PlayerSettings.Android.useCustomKeystore;
            AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;
            AndroidSdkVersions minSdk = PlayerSettings.Android.minSdkVersion, targetSdk = PlayerSettings.Android.targetSdkVersion;
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            bool defaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            bool portrait = PlayerSettings.allowedAutorotateToPortrait, upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft, landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
#if UNITY_ANDROID
            string sdk = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath;
            string ndk = UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath;
            string jdk = UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath;
#endif
            try
            {
#if UNITY_ANDROID
                string sdkOverride = Environment.GetEnvironmentVariable("DEPREM_ANDROID_SDK_PATH");
                string ndkOverride = Environment.GetEnvironmentVariable("DEPREM_ANDROID_NDK_PATH");
                string jdkOverride = Environment.GetEnvironmentVariable("DEPREM_ANDROID_JDK_PATH");
                if (!string.IsNullOrWhiteSpace(sdkOverride)) UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = Path.GetFullPath(sdkOverride);
                if (!string.IsNullOrWhiteSpace(ndkOverride)) UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = Path.GetFullPath(ndkOverride);
                if (!string.IsNullOrWhiteSpace(jdkOverride)) UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = Path.GetFullPath(jdkOverride);
#endif
                ConfigureMobile(); Validate();
                // One signed APK contains both ARM tablet architectures and all game data.
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
                PlayerSettings.Android.useAPKExpansionFiles = false;
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)23;
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"Android build {report.summary.result}: {report.summary.totalErrors} errors.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "android-build-info.txt"),
                    $"Result: {report.summary.result}\nTarget: Android APK\nArchitectures: ARMv7, ARM64\nMinimum API: 23\nTarget API: 35\nScenes: {scenes.Length}\nBytes: {report.summary.totalSize}\n\n" + string.Join("\n", scenes) + "\n");
                Debug.Log($"[Deprem App] ANDROID BUILD SUCCEEDED: {output}; {scenes.Length} scenes.");
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = appBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
                EditorUserBuildSettings.androidBuildSubtarget = textureFormat;
                PlayerSettings.Android.useAPKExpansionFiles = expansionFiles;
                PlayerSettings.Android.useCustomKeystore = customKeystore;
                PlayerSettings.Android.targetArchitectures = architectures;
                PlayerSettings.Android.minSdkVersion = minSdk; PlayerSettings.Android.targetSdkVersion = targetSdk;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, defaultGraphics);
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait; PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft; PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
#if UNITY_ANDROID
                if (Directory.Exists(sdk)) UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
                if (Directory.Exists(ndk)) UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndk;
                if (Directory.Exists(jdk)) UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdk;
#endif
            }
        }

        [MenuItem("Tools/Deprem App/Build Windows EXE")]
        public static void BuildWindows()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new BuildFailedException("Windows Build Support is required.");
            ConfigureBuildScenes(); Validate();
            string output = Environment.GetEnvironmentVariable("DEPREM_WINDOWS_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = "Builds/DepremApp/Windows/DepremApp.exe";
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            foreach (string scene in scenes)
                if (!File.Exists(scene)) throw new FileNotFoundException("Missing build scene", scene);
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            FullScreenMode fullscreen = PlayerSettings.fullScreenMode;
            bool resizable = PlayerSettings.resizableWindow;
            try
            {
                PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"Windows build {report.summary.result}: {report.summary.totalErrors} errors.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-info.txt"),
                    $"Result: {report.summary.result}\nTarget: Windows x64\nScenes: {scenes.Length}\nBytes: {report.summary.totalSize}\n\n" + string.Join("\n", scenes) + "\n");
                Debug.Log($"[Deprem App] WINDOWS BUILD SUCCEEDED: {output}; {scenes.Length} scenes.");
            }
            finally
            {
                PlayerSettings.defaultScreenWidth = width; PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.fullScreenMode = fullscreen; PlayerSettings.resizableWindow = resizable;
            }
        }
    }

#if UNITY_ANDROID && UNITY_EDITOR_WIN
    public sealed class LearningAndroidWindowsTrust : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (Environment.GetEnvironmentVariable("DEPREM_ANDROID_WINDOWS_TRUST") != "1") return;
            string properties = Path.Combine(Directory.GetParent(path).FullName, "gradle.properties");
            File.AppendAllText(properties,
                "\nsystemProp.javax.net.ssl.trustStoreType=Windows-ROOT\nsystemProp.javax.net.ssl.trustStore=NONE\n");
        }
    }
#endif

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
