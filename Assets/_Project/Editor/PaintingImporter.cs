using UnityEditor;
using UnityEngine;

namespace SovereignOfAshes.EditorTools
{
    /// <summary>
    /// Configura a importação das pinturas da interface (<c>Resources/UI</c> e <c>Resources/Portraits</c>):
    /// sem compressão (preserva o dithering do pixel art), com mipmaps e filtro bilinear (reduzem a arte sem cintilar)
    /// e sem redimensionar para potência de dois.
    /// </summary>
    public sealed class PaintingImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool isUi = path.Contains("/Resources/UI/");
            bool isPortrait = path.Contains("/Resources/Portraits/");

            if (!isUi && !isPortrait)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = isUi ? 2048 : 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
