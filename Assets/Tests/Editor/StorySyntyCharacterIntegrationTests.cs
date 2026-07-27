using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class StorySyntyCharacterIntegrationTests
{
    [TestCase("Assets/Scenes/Story_01_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_02_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_03_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_04_RebuildPreview.unity")]
    public void RebuildPreview_UsesChibiChildrenWithValidHumanoidAvatars(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        AssertCharacter("Deniz_12", "character-male-a");
        AssertCharacter("Can_8", "character-male-d");
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/Story/Characters/ThirdParty/KenneyMini/LICENSE.txt"), Is.Not.Null);
    }

    [Test]
    public void AdultRoles_UseChibiFamilyAndDistinctEmergencyCast()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Story_04_RebuildPreview.unity",
            OpenSceneMode.Single);

        AssertRoleSource("Anne_Assembly_Reunion", "merchantpr");
        AssertRoleSource("Baba_Assembly_Reunion", "merchantpr");
        AssertRoleSource("AssemblyWorker", "Character_Paramedic_01");
        Assert.That(Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Select(item => item.name), Does.Contain("CharacterSource_Character_Grandma_01"));
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

    private static void AssertCharacter(string roleName, string sourceName)
    {
        GameObject role = GameObject.Find(roleName);
        Assert.That(role, Is.Not.Null, roleName);
        Assert.That(
            role.transform.Find("CharacterSource_" + sourceName),
            Is.Not.Null,
            roleName);

        SkinnedMeshRenderer[] visibleMeshes = role
            .GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.gameObject.activeSelf)
            .ToArray();
        Assert.That(visibleMeshes.Select(renderer => renderer.name),
            Does.Contain("head-mesh"), roleName);

        Animator animator = role.GetComponentInChildren<Animator>(true);
        Assert.That(animator, Is.Not.Null, roleName);
        Assert.That(animator.avatar, Is.Not.Null, roleName);
        Assert.That(animator.avatar.isValid, Is.True, roleName);
        Assert.That(animator.avatar.isHuman, Is.True, roleName);
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null, roleName);
    }

    private static void AssertRoleSource(string roleName, string sourceName)
    {
        GameObject role = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == roleName)
            ?.gameObject;
        Assert.That(role, Is.Not.Null, roleName);
        Assert.That(role.transform.Find("CharacterSource_" + sourceName), Is.Not.Null, roleName);
    }
}
