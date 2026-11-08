using System;
using System.Collections.Generic;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using UnityEngine;

namespace SovereignOfAshes.Presentation.Art
{
    /// <summary>
    /// Fábrica e cache das texturas do jogo: sprites em pixel art (filtro "Point", nítidos em qualquer escala)
    /// e texturas procedurais de terreno e fundo. Dono de todas as texturas que cria.
    /// </summary>
    public sealed class ArtLibrary : IDisposable
    {
        private readonly Dictionary<string, Texture2D> _sprites = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> _illustrations = new Dictionary<string, Texture2D>();
        private readonly List<Texture2D> _owned = new List<Texture2D>();
        private bool _disposed;

        /// <summary>
        /// Textura de um sprite pelo id (usa o sprite padrão se não existir).
        /// </summary>
        /// <param name="spriteId">Id do sprite.</param>
        /// <returns>Textura pixelada.</returns>
        public Texture2D Sprite(string spriteId)
        {
            string key = spriteId ?? PixelSprites.Fallback;

            if (_sprites.TryGetValue(key, out Texture2D cached))
            {
                return cached;
            }

            if (!PixelSprites.TryGet(key, out PixelSpriteDefinition definition))
            {
                PixelSprites.TryGet(PixelSprites.Fallback, out definition);
            }

            Texture2D texture = FromPixels(definition.Width, definition.Height, PixelGrid.Rasterize(definition, PixelPalette.Default));
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            _sprites[key] = texture;
            return texture;
        }

        /// <summary>Arte de um elemento da alquimia.</summary>
        /// <param name="definition">Elemento.</param>
        /// <returns>Textura.</returns>
        public Texture2D Element(IElementDefinition definition) =>
            Sprite(ArtCatalog.ForElement(definition?.Id, definition?.Kind ?? ElementKind.Concept));

        /// <summary>Arte de um invasor.</summary>
        /// <param name="definition">Invasor.</param>
        /// <returns>Textura.</returns>
        public Texture2D Invader(IInvaderDefinition definition) => Sprite(ArtCatalog.ForInvader(definition?.Id));

        /// <summary>Ilustração de uma carta em pixel art gerada por código (reserva quando não há ilustração pintada).</summary>
        /// <param name="card">Carta.</param>
        /// <returns>Textura.</returns>
        public Texture2D Card(ICardDefinition card) => Sprite(ArtCatalog.ForCard(card?.Id, card?.PrimaryConcept ?? ConceptType.Faith));

        /// <summary>
        /// Ilustração pintada da carta, carregada de <c>Resources/CardArt/&lt;id&gt;</c> (resultado em cache, inclusive ausências).
        /// </summary>
        /// <param name="card">Carta.</param>
        /// <returns>A textura, ou <c>null</c> se a carta não tiver ilustração.</returns>
        public Texture2D CardIllustration(ICardDefinition card)
        {
            string path = ArtCatalog.CardIllustrationPath(card?.Id);

            if (path == null)
            {
                return null;
            }

            if (!_illustrations.TryGetValue(path, out Texture2D texture))
            {
                // Texturas de Resources pertencem ao Unity: não entram em _owned nem são destruídas no Dispose.
                texture = Resources.Load<Texture2D>(path);
                _illustrations[path] = texture;
            }

            return texture;
        }

        /// <summary>
        /// Cria uma textura de terreno pixelada e repetível (blocos 4x4 com granulação e rachaduras).
        /// </summary>
        /// <param name="size">Tamanho em pixels (múltiplo de 4).</param>
        /// <param name="dark">Tom escuro.</param>
        /// <param name="mid">Tom médio (predominante).</param>
        /// <param name="light">Tom claro.</param>
        /// <param name="seed">Semente.</param>
        /// <returns>Textura com repetição.</returns>
        public Texture2D CreateTerrain(int size, Color dark, Color mid, Color light, int seed)
        {
            var random = new System.Random(seed);
            int cells = Math.Max(1, size / 4);
            var coarse = new float[cells * cells];

            for (int i = 0; i < coarse.Length; i++)
            {
                coarse[i] = (float)random.NextDouble();
            }

            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = coarse[(y / 4 % cells) * cells + (x / 4 % cells)] * 0.7f + (float)random.NextDouble() * 0.3f;
                    Color color = value < 0.32f ? dark : value < 0.78f ? mid : light;

                    if (random.NextDouble() < 0.025)
                    {
                        color = dark * 0.75f;
                    }

                    color.a = 1f;
                    pixels[y * size + x] = color;
                }
            }

            Texture2D texture = Own(new Texture2D(size, size, TextureFormat.RGBA32, false));
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        /// <summary>
        /// Cria um fundo com vinheta radial (centro iluminado, bordas escuras).
        /// </summary>
        /// <param name="size">Tamanho em pixels.</param>
        /// <param name="center">Cor do centro.</param>
        /// <param name="edge">Cor das bordas.</param>
        /// <returns>Textura suave.</returns>
        public Texture2D CreateVignette(int size, Color center, Color edge)
        {
            var pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;
            float max = Mathf.Sqrt(half * half * 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half;
                    float dy = y - half;
                    float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / max);
                    Color color = Color.Lerp(center, edge, Mathf.Pow(t, 1.4f));
                    color.a = 1f;
                    pixels[y * size + x] = color;
                }
            }

            Texture2D texture = Own(new Texture2D(size, size, TextureFormat.RGBA32, false));
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        /// <summary>Destrói todas as texturas criadas.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Texture2D texture in _owned)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _owned.Clear();
            _sprites.Clear();
            _illustrations.Clear();
            _disposed = true;
        }

        private Texture2D FromPixels(int width, int height, PixelColor[] source)
        {
            var pixels = new Color32[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                pixels[i] = new Color32(source[i].R, source[i].G, source[i].B, source[i].A);
            }

            Texture2D texture = Own(new Texture2D(width, height, TextureFormat.RGBA32, false));
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D Own(Texture2D texture)
        {
            _owned.Add(texture);
            return texture;
        }
    }
}
