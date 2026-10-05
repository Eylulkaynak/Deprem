using System;
using UnityEditor;

namespace Deprem.Learning.Editor
{
    // Preserve the 400x80 institutional lockup's aspect ratio and fine lettering.
    public sealed class LearningBrandImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 1;
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/LearningApp/Resources/LearningApp/Brand/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
        }

        [MenuItem("Tools/Deprem App/Review/Reimport Institution Logos")]
        public static void Reimport()
        {
            foreach (string name in new[] { "imo-logo-full.jpg", "imo-logo-square.png" })
                AssetDatabase.ImportAsset("Assets/LearningApp/Resources/LearningApp/Brand/" + name, ImportAssetOptions.ForceUpdate);
        }
    }
}
