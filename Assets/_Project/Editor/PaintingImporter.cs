using UnityEditor;
using UnityEngine;

namespace SovereignOfAshes.EditorTools
{
    /// <summary>
    /// Configura a importação das pinturas da interface (<c>Resources/UI</c> e <c>Resources/Portraits</c>):
    /// compressão de alta qualidade (BC7: um quarto do tamanho na memória e no disco, quase sem perda), com mipmaps e filtro bilinear (reduzem a arte sem cintilar)
    /// e sem redimensionar para potência de dois.
    /// </summary>
    public sealed class PaintingImporter : AssetPostprocessor
    {
        /// <summary>Aumente quando mudar as regras de importação: força o Unity a reimportar as texturas existentes.</summary>
        /// <returns>Versão das regras.</returns>
        public override uint GetVersion() => 2;

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
            bool hasAlpha = path.EndsWith("/Resources/UI/logo.png");
            importer.alphaSource = hasAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = hasAlpha;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = isUi ? 2048 : 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
