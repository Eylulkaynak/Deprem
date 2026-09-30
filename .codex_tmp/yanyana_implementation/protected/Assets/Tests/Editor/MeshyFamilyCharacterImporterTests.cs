using NUnit.Framework;

public sealed class MeshyFamilyCharacterImporterTests
{
    [Test]
    public void PreparedPrefabsHaveValidHumanoidMeshesAndClosedSkinWeightSeams()
    {
        Assert.DoesNotThrow(() => MeshyFamilyCharacterImporter.Validate(false));
    }
}
