using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    /// <summary>Adult locomotion shares the adventure's states, with its own ankle-safe walk.</summary>
    public static class YanYanaAdultLocomotion
    {
        public const string ControllerPath = "Assets/YanYana/Animation/AdventureAdults.overrideController";
        public const string BaseControllerPath = "Assets/YanYana/Animation/AdventureCharacters.controller";

        public static RuntimeAnimatorController ForCharacter(string name, RuntimeAnimatorController shared)
        {
            return name == "Ada" || name == "Efe" ? shared : EnsureController(shared);
        }

        public static AnimatorOverrideController EnsureController(RuntimeAnimatorController shared = null)
        {
            shared = shared ? shared : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAdultWalkBuilder.ClipPath);
            if (!walk) { StoryAdultWalkBuilder.Build(); walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAdultWalkBuilder.ClipPath); }
            var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(StoryAnimationLibraryBuilder.ChildNaturalWalkPath);
            if (!shared || !original || !walk) throw new InvalidOperationException("Adult locomotion assets are missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ControllerPath);
            if (!controller)
            {
                controller = new AnimatorOverrideController(shared) { name = "AdventureAdults" };
                AssetDatabase.CreateAsset(controller, ControllerPath);
            }
            if (controller.runtimeAnimatorController != shared) controller.runtimeAnimatorController = shared;
            if (controller[original] != walk) { controller[original] = walk; EditorUtility.SetDirty(controller); }
            return controller;
        }

        [MenuItem("Tools/Yan Yana/Art/Fix Adult Walking Feet")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play Mode before authoring adult locomotion.");
            StoryAdultWalkBuilder.Build();
            var controller = EnsureController();
            var adultAvatars = new HashSet<Avatar>();
            string[] prefabs = new[] { "Derya", "Emre", "Yusuf", "Idil", "Bora" }
                .Select(name => YanYanaCharacterStyle.Root + "/" + name + ".prefab")
                .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { YanYanaCharacterVariants.Root })
                    .Select(AssetDatabase.GUIDToAssetPath)).ToArray();
            foreach (string path in prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var animator = root.GetComponentInChildren<Animator>(true);
                    if (!animator) throw new InvalidOperationException("Missing adult Animator: " + path);
                    adultAvatars.Add(animator.avatar);
                    if (animator.runtimeAnimatorController == controller) continue;
                    animator.runtimeAnimatorController = controller;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var scene = SceneManager.GetSceneByPath(YanYanaAdventureBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(YanYanaAdventureBuilder.ScenePath, OpenSceneMode.Additive);
            int count = 0;
            bool changed = false;
            try
            {
                var shared = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
                foreach (var animator in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Animator>(true)))
                {
                    if (!adultAvatars.Contains(animator.avatar) ||
                        (animator.runtimeAnimatorController != shared && animator.runtimeAnimatorController != controller)) continue;
                    count++;
                    if (animator.runtimeAnimatorController == controller) continue;
                    animator.runtimeAnimatorController = controller;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    changed = true;
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Adult walking feet corrected: {prefabs.Length} prefabs, {count} adventure actors.");
        }
    }
}
