using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Presentation.Art
{
    /// <summary>
    /// Cor RGBA de 8 bits por canal, sem depender do Unity (testável).
    /// </summary>
    public readonly struct PixelColor
    {
        /// <summary>Vermelho.</summary>
        public byte R { get; }

        /// <summary>Verde.</summary>
        public byte G { get; }

        /// <summary>Azul.</summary>
        public byte B { get; }

        /// <summary>Opacidade.</summary>
        public byte A { get; }

        /// <summary>
        /// Cria a cor.
        /// </summary>
        /// <param name="r">Vermelho.</param>
        /// <param name="g">Verde.</param>
        /// <param name="b">Azul.</param>
        /// <param name="a">Opacidade.</param>
        public PixelColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Cria a cor a partir de um inteiro 0xRRGGBB (opaca).
        /// </summary>
        /// <param name="rgb">Cor em hexadecimal.</param>
        /// <returns>A cor.</returns>
        public static PixelColor FromHex(int rgb) =>
            new PixelColor((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

        /// <summary>Totalmente transparente.</summary>
        public static PixelColor Clear => new PixelColor(0, 0, 0, 0);
    }

    /// <summary>
    /// Paleta: associa cada caractere de um sprite a uma cor. '.' é sempre transparente.
    /// </summary>
    public sealed class PixelPalette
    {
        /// <summary>Caractere reservado para pixel transparente.</summary>
        public const char Transparent = '.';

        private readonly Dictionary<char, PixelColor> _colors = new Dictionary<char, PixelColor>();

        /// <summary>
        /// Paleta padrão do jogo (fantasia sombria: carvão, ouro velho, brasa).
        /// </summary>
        public static PixelPalette Default { get; } = new PixelPalette()
            .Add('k', 0x1A1420).Add('w', 0xF4EAD5)
            .Add('g', 0xE8B84A).Add('G', 0xA8752A)
            .Add('y', 0xFFD85A).Add('o', 0xF08A2C)
            .Add('r', 0xC8323C).Add('R', 0x7A1E28)
            .Add('s', 0xA9B4C2).Add('S', 0x5D6878)
            .Add('b', 0x5A8AE0).Add('B', 0x2C3F7A)
            .Add('p', 0x8A4AC8).Add('P', 0x45245E)
            .Add('n', 0x8A5A34).Add('N', 0x4E3220)
            .Add('f', 0xE0907A).Add('F', 0xA85A50)
            .Add('e', 0x5AB45A).Add('E', 0x2E6A3A)
            .Add('c', 0x6AD8E0).Add('C', 0x2E7C8A)
            .Add('a', 0x7A7480).Add('A', 0x3E3A44)
            .Add('h', 0x55505E);

        /// <summary>
        /// Registra uma cor.
        /// </summary>
        /// <param name="key">Caractere (não pode ser '.').</param>
        /// <param name="rgb">Cor 0xRRGGBB.</param>
        /// <returns>Esta paleta, para encadear.</returns>
        /// <exception cref="ArgumentException">Se <paramref name="key"/> for o caractere transparente.</exception>
        public PixelPalette Add(char key, int rgb)
        {
            if (key == Transparent)
            {
                throw new ArgumentException("'.' é reservado para transparência.", nameof(key));
            }

            _colors[key] = PixelColor.FromHex(rgb);
            return this;
        }

        /// <summary>
        /// Busca a cor de um caractere.
        /// </summary>
        /// <param name="key">Caractere.</param>
        /// <param name="color">Cor encontrada.</param>
        /// <returns><c>false</c> se o caractere não existir na paleta.</returns>
        public bool TryGet(char key, out PixelColor color)
        {
            if (key == Transparent)
            {
                color = PixelColor.Clear;
                return true;
            }

            return _colors.TryGetValue(key, out color);
        }
    }

    /// <summary>
    /// Sprite em pixel art descrito como texto: cada linha é uma fileira de pixels, de cima para baixo.
    /// </summary>
    public sealed class PixelSpriteDefinition
    {
        /// <summary>Identificador.</summary>
        public string Id { get; }

        /// <summary>Largura em pixels.</summary>
        public int Width { get; }

        /// <summary>Altura em pixels.</summary>
        public int Height { get; }

        /// <summary>Fileiras, de cima para baixo.</summary>
        public IReadOnlyList<string> Rows { get; }

        /// <summary>
        /// Cria o sprite.
        /// </summary>
        /// <param name="id">Identificador (não vazio).</param>
        /// <param name="rows">Fileiras de mesmo comprimento.</param>
        /// <exception cref="ArgumentException">Se o id for vazio ou as fileiras não formarem um retângulo.</exception>
        public PixelSpriteDefinition(string id, params string[] rows)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("O id não pode ser vazio.", nameof(id));
            }

            if (rows == null || rows.Length == 0 || string.IsNullOrEmpty(rows[0]))
            {
                throw new ArgumentException($"O sprite '{id}' não tem pixels.", nameof(rows));
            }

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].Length != rows[0].Length)
                {
                    throw new ArgumentException(
                        $"O sprite '{id}' tem a fileira {i} com {rows[i]?.Length ?? 0} pixels; esperado {rows[0].Length}.", nameof(rows));
                }
            }

            Id = id;
            Width = rows[0].Length;
            Height = rows.Length;
            Rows = rows;
        }
    }

    /// <summary>
    /// Converte sprites de texto em pixels.
    /// </summary>
    public static class PixelGrid
    {
        /// <summary>
        /// Rasteriza o sprite na ordem que o Unity espera: da fileira de baixo para a de cima.
        /// </summary>
        /// <param name="sprite">Sprite.</param>
        /// <param name="palette">Paleta.</param>
        /// <returns>Pixels (Largura x Altura).</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentException">Se houver caractere fora da paleta.</exception>
        public static PixelColor[] Rasterize(PixelSpriteDefinition sprite, PixelPalette palette)
        {
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            var pixels = new PixelColor[sprite.Width * sprite.Height];

            for (int row = 0; row < sprite.Height; row++)
            {
                string line = sprite.Rows[row];
                int y = sprite.Height - 1 - row;

                for (int x = 0; x < sprite.Width; x++)
                {
                    if (!palette.TryGet(line[x], out PixelColor color))
                    {
                        throw new ArgumentException($"Sprite '{sprite.Id}': cor '{line[x]}' (fileira {row}, coluna {x}) não existe na paleta.");
                    }

                    pixels[y * sprite.Width + x] = color;
                }
            }

            return pixels;
        }
    }
}
