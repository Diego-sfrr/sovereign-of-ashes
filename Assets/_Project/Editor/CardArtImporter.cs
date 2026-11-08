using SovereignOfAshes.Presentation.Art;
using UnityEditor;
using UnityEngine;

namespace SovereignOfAshes.EditorTools
{
    /// <summary>
    /// Configura automaticamente a importação das ilustrações das cartas
    /// (<c>Resources/CardArt/*.png</c>): sem compressão (preserva a paleta do pixel art),
    /// com mipmaps e filtro bilinear, para reduzir a arte de 512px até o quadro da carta sem cintilar.
    /// </summary>
    public sealed class CardArtImporter : AssetPostprocessor
    {
        private const int MaxSize = 512;

        private void OnPreprocessTexture()
        {
            string folder = $"/Resources/{ArtCatalog.CardIllustrationFolder}/";

            if (!assetPath.Replace('\\', '/').Contains(folder))
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
            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
