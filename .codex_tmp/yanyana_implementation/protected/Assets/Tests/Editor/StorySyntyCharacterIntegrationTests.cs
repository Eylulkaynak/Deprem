using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StorySyntyCharacterIntegrationTests
{
    [TestCase("Assets/Scenes/Story_01_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_02_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_03_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_04_RebuildPreview.unity")]
    public void RebuildPreview_UsesChibiChildrenWithValidHumanoidAvatars(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        AssertCharacter(scene, "Deniz_12", "Deniz");
        AssertCharacter(scene, "Can_8", "Can");
    }

    [Test]
    public void AdultRoles_UseChibiFamilyAndDistinctEmergencyCast()
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Story_04_RebuildPreview.unity",
            OpenSceneMode.Single);

        AssertRoleSource(scene, "Anne_Assembly_Reunion", "Anne");
        AssertRoleSource(scene, "Baba_Assembly_Reunion", "Baba");
        AssertRoleSource(scene, "AssemblyWorker", "RescueWorker");
        AssertRoleSource(scene, "AssemblyPolice", "Police");
        AssertRoleSource(scene, "EmergencyFirefighter", "Firefighter");
        Assert.That(SceneTransforms(scene).Select(item => item.name), Does.Contain("CharacterSource_Komsu"));
    }

    [Test]
    public void ImportedSyntyMaterials_AllUseUrpLitAndGpuInstancing()
    {
        string[] materialRoots =
        {
            "Assets/PolygonTown/Materials",
            "Assets/POLYGONCityCharacters/Materials"
        };
        string[] guids = AssetDatabase.FindAssets("t:Material", materialRoots);
        Assert.That(guids, Has.Length.EqualTo(37));

        foreach (string guid in guids)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null, material.name);
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), material.name);
            Assert.That(material.enableInstancing, Is.True, material.name);
        }
    }

    private static void AssertCharacter(Scene scene, string roleName, string sourceName)
    {
        GameObject role = FindInScene(scene, roleName);
        Assert.That(role, Is.Not.Null, roleName);
        Assert.That(
            role.transform.Find("CharacterSource_" + sourceName),
            Is.Not.Null,
            roleName);

        SkinnedMeshRenderer[] visibleMeshes = role
            .GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.gameObject.activeSelf)
            .ToArray();
        Assert.That(visibleMeshes, Is.Not.Empty, roleName);

        Animator animator = role.GetComponentInChildren<Animator>(true);
        Assert.That(animator, Is.Not.Null, roleName);
        Assert.That(animator.avatar, Is.Not.Null, roleName);
        Assert.That(animator.avatar.isValid, Is.True, roleName);
        Assert.That(animator.avatar.isHuman, Is.True, roleName);
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null, roleName);
    }

    private static void AssertRoleSource(Scene scene, string roleName, string sourceName)
    {
        GameObject role = SceneTransforms(scene)
            .FirstOrDefault(item => item.name == roleName)
            ?.gameObject;
        Assert.That(role, Is.Not.Null, roleName);
        Assert.That(role.transform.Find("CharacterSource_" + sourceName), Is.Not.Null, roleName);
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        return SceneTransforms(scene)
            .FirstOrDefault(item => item.name == objectName)
            ?.gameObject;
    }

    private static Transform[] SceneTransforms(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
    }
}
