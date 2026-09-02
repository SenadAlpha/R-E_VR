#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Editor
{
    internal sealed class TestImageImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/test_images/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 4096;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;

            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 4096,
                textureCompression = TextureImporterCompression.Uncompressed,
                format = TextureImporterFormat.RGBA32
            };
            importer.SetPlatformTextureSettings(android);
        }
    }
}
#endif
