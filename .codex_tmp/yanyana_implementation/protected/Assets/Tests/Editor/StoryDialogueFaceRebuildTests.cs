using System.Collections.Generic;
using System.Linq;
using Deprem.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StoryDialogueFaceRebuildTests
{
    [Test]
    public void NeutralIdleControllers_UseCalmSixSecondCadence()
    {
        AnimatorController child = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            StoryAnimationLibraryBuilder.ControllerPath);
        AnimatorController adult = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            StoryAnimationLibraryBuilder.AdultControllerPath);
        Assert.That(child, Is.Not.Null);
        Assert.That(adult, Is.Not.Null);

        AnimatorState childLocomotion = child.layers[0].stateMachine.states
            .Select(item => item.state)
            .Single(state => state.name == "Locomotion");
        BlendTree childTree = childLocomotion.motion as BlendTree;
        Assert.That(childTree, Is.Not.Null);
        ChildMotion childIdle = childTree.children.Single(item => item.motion.name == "ChildNeutralIdle");
        Assert.That(childIdle.timeScale, Is.EqualTo(0.42f).Within(0.001f));
        Assert.That(childIdle.motion.averageDuration / childIdle.timeScale, Is.GreaterThanOrEqualTo(5.5f),
            "Çocuk idle salınımı telefonda huzursuz edecek kadar hızlı olmamalı.");

        AnimatorState adultIdle = adult.layers[0].stateMachine.states
            .Select(item => item.state)
            .Single(state => state.name == "Adult Idle");
        Assert.That(adultIdle.speed, Is.EqualTo(0.42f).Within(0.001f));
        Assert.That(adultIdle.motion.averageDuration / adultIdle.speed, Is.GreaterThanOrEqualTo(5.5f),
            "Yetişkin idle salınımı telefonda huzursuz edecek kadar hızlı olmamalı.");
    }

    [Test]
    public void CommonFaceClip_HoldsBlinkClosedFor140MillisecondsWithoutAnimatingTheMouth()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            StoryChapterBuilderCommon.CharacterFaceAliveClipPath);
        Assert.That(clip, Is.Not.Null);
        EditorCurveBinding lidBinding = AnimationUtility.GetCurveBindings(clip)
            .Single(binding => binding.path == "Eyelid_L" &&
                               binding.propertyName.EndsWith("Scale.y"));
        AnimationCurve blink = AnimationUtility.GetEditorCurve(clip, lidBinding);
        Keyframe[] closedKeys = blink.keys.Where(key => key.value > 0.02f).Take(2).ToArray();
        Assert.That(closedKeys, Has.Length.EqualTo(2));
        Assert.That(closedKeys[1].time - closedKeys[0].time, Is.InRange(0.12f, 0.16f));
        Assert.That(blink.keys.Max(key => key.value), Is.LessThanOrEqualTo(0.04f),
            "Göz kapağı yüzde dev bir bara dönüşmemeli.");
        Assert.That(AnimationUtility.GetCurveBindings(clip)
            .Any(binding => binding.path.Contains("Mouth_Center")), Is.False,
            "Ortak blink klibi ağzı sürmemeli; konuşmayı yalnız StoryUIController yönetir.");
    }

    [TestCase("Assets/Scenes/Story_01_RebuildPreview.unity", 3)]
    [TestCase("Assets/Scenes/Story_02_RebuildPreview.unity", 4)]
    [TestCase("Assets/Scenes/Story_03_RebuildPreview.unity", 2)]
    [TestCase("Assets/Scenes/Story_04_RebuildPreview.unity", 8)]
    public void RebuildScene_UsesSerializedActorsAndTheSingleCommonFaceRig(string scenePath, int actorCount)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        StoryUIController ui = Object.FindFirstObjectByType<StoryUIController>();
        Assert.That(ui, Is.Not.Null, scenePath);

        SerializedProperty actors = new SerializedObject(ui).FindProperty("dialogueActors");
        Assert.That(actors, Is.Not.Null);
        Assert.That(actors.arraySize, Is.EqualTo(actorCount), scenePath);

        var actorRoots = new HashSet<Transform>();
        for (int index = 0; index < actors.arraySize; index++)
        {
            SerializedProperty actor = actors.GetArrayElementAtIndex(index);
            Transform actorRoot = actor.FindPropertyRelative("actorRoot").objectReferenceValue as Transform;
            Transform head = actor.FindPropertyRelative("head").objectReferenceValue as Transform;
            Transform mouth = actor.FindPropertyRelative("mouth").objectReferenceValue as Transform;
            SerializedProperty aliases = actor.FindPropertyRelative("aliases");

            Assert.That(actorRoot, Is.Not.Null, $"{scenePath} actor {index} root");
            Assert.That(actorRoots.Add(actorRoot), Is.True,
                $"{scenePath} actor {index} aynı karakter kökünü ikinci kez bağlamamalı.");
            Assert.That(head, Is.Not.Null, $"{actorRoot.name} head");
            Assert.That(head.IsChildOf(actorRoot), Is.True, $"{actorRoot.name} head hierarchy");
            Assert.That(mouth, Is.Not.Null, $"{actorRoot.name} mouth");
            Assert.That(mouth.name, Is.EqualTo("Mouth_Center"));
            Assert.That(aliases.arraySize, Is.GreaterThan(0), $"{actorRoot.name} aliases");
            for (int aliasIndex = 0; aliasIndex < aliases.arraySize; aliasIndex++)
                Assert.That(aliases.GetArrayElementAtIndex(aliasIndex).stringValue,
                    Is.Not.Null.And.Not.Empty, $"{actorRoot.name} alias {aliasIndex}");

            Transform faceRig = mouth.parent;
            Assert.That(faceRig, Is.Not.Null);
            Assert.That(faceRig.name, Is.EqualTo(actorRoot.name + "_FaceRig"));
            Assert.That(faceRig.parent, Is.SameAs(head), $"{actorRoot.name} face rig head binding");
            Assert.That(faceRig.Find("Brow_L"), Is.Not.Null);
            Assert.That(faceRig.Find("Brow_R"), Is.Not.Null);
            Assert.That(faceRig.Find("Eyelid_L"), Is.Not.Null);
            Assert.That(faceRig.Find("Eyelid_R"), Is.Not.Null);
            Assert.That(faceRig.GetComponentsInChildren<Collider>(true), Is.Empty,
                $"{actorRoot.name} yüz rig'i dokunma ışınını engellememeli.");

            Animation faceAnimation = faceRig.GetComponent<Animation>();
            Assert.That(faceAnimation, Is.Not.Null, $"{actorRoot.name} common face animation");
            Assert.That(faceAnimation.clip, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(faceAnimation.clip),
                Is.EqualTo(StoryChapterBuilderCommon.CharacterFaceAliveClipPath),
                $"{actorRoot.name} yalnız ortak face clip'i kullanmalı.");
        }
    }
}
