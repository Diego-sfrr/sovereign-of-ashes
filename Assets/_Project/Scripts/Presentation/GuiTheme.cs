using System;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Paleta, estilos e primitivas de desenho do HUD (IMGUI) em fantasia sombria: carvão, bronze, ouro velho e brasa.
    /// Deve ser criado dentro de <c>OnGUI</c>, pois depende de <see cref="GUI.skin"/>.
    /// </summary>
    public sealed class GuiTheme : IDisposable
    {
        /// <summary>Fundo da câmera.</summary>
        public static readonly Color Background = new Color(0.035f, 0.03f, 0.045f);

        /// <summary>Ouro velho (títulos, destaques).</summary>
        public static readonly Color Gold = new Color(0.91f, 0.72f, 0.29f);

        /// <summary>Bronze das molduras.</summary>
        public static readonly Color Bronze = new Color(0.47f, 0.33f, 0.17f);

        /// <summary>Preenchimento de painel.</summary>
        public static readonly Color PanelFill = new Color(0.07f, 0.06f, 0.08f, 0.93f);

        /// <summary>Texto comum.</summary>
        public static readonly Color Text = new Color(0.93f, 0.89f, 0.82f);

        /// <summary>Texto secundário.</summary>
        public static readonly Color TextDim = new Color(0.62f, 0.58f, 0.54f);

        /// <summary>Coisas boas para o jogador.</summary>
        public static readonly Color Good = new Color(0.55f, 0.85f, 0.52f);

        /// <summary>Coisas ruins para o jogador.</summary>
        public static readonly Color Bad = new Color(0.95f, 0.38f, 0.32f);

        /// <summary>Cor da Fé.</summary>
        public static readonly Color FaithColor = new Color(0.45f, 0.66f, 1f);

        /// <summary>Cor da Racionalidade.</summary>
        public static readonly Color RationalityColor = new Color(0.97f, 0.62f, 0.22f);

        /// <summary>Fundo de barra.</summary>
        public static readonly Color BarBack = new Color(0.14f, 0.12f, 0.14f);

        private bool _disposed;

        /// <summary>Textura branca 1x1 para desenhar retângulos com <see cref="GUI.color"/>.</summary>
        public Texture2D White { get; }

        /// <summary>Título de seção (ouro).</summary>
        public GUIStyle Title { get; }

        /// <summary>Faixa grande (turno, fim de jogo). Texto branco: a cor vem de <see cref="GUI.color"/>.</summary>
        public GUIStyle Banner { get; }

        /// <summary>Texto comum.</summary>
        public GUIStyle Label { get; }

        /// <summary>Texto pequeno e discreto.</summary>
        public GUIStyle Small { get; }

        /// <summary>Texto centralizado (dicas).</summary>
        public GUIStyle Centered { get; }

        /// <summary>Número grande em branco (a cor vem de <see cref="GUI.color"/>).</summary>
        public GUIStyle Number { get; }

        /// <summary>Número pequeno em branco, centralizado.</summary>
        public GUIStyle SmallNumber { get; }

        /// <summary>Número do custo na gema da carta (branco, centralizado).</summary>
        public GUIStyle CostNumber { get; }

        /// <summary>Números flutuantes (branco; a cor vem de <see cref="GUI.color"/>).</summary>
        public GUIStyle FloatText { get; }

        /// <summary>Nome da carta.</summary>
        public GUIStyle CardName { get; }

        /// <summary>Texto da carta.</summary>
        public GUIStyle CardBody { get; }

        /// <summary>Linha de tipo da carta.</summary>
        public GUIStyle CardType { get; }

        /// <summary>Texto de botão.</summary>
        public GUIStyle ButtonText { get; }

        /// <summary>
        /// Cria texturas e estilos. Chame apenas dentro de <c>OnGUI</c>.
        /// </summary>
        public GuiTheme()
        {
            White = new Texture2D(1, 1);
            White.SetPixel(0, 0, Color.white);
            White.Apply();

            Title = MakeText(18, Gold, FontStyle.Bold, TextAnchor.UpperLeft);
            Banner = MakeText(64, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label = MakeText(14, Text, FontStyle.Normal, TextAnchor.UpperLeft);
            Small = MakeText(12, TextDim, FontStyle.Normal, TextAnchor.UpperLeft);
            Centered = MakeText(16, Text, FontStyle.Normal, TextAnchor.MiddleCenter);
            Number = MakeText(22, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SmallNumber = MakeText(13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            CostNumber = MakeText(20, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            FloatText = MakeText(24, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            CardName = MakeText(14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            CardBody = MakeText(12, Text, FontStyle.Normal, TextAnchor.UpperCenter);
            CardType = MakeText(11, Gold, FontStyle.Italic, TextAnchor.MiddleCenter);
            ButtonText = MakeText(20, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// Desenha um retângulo sólido.
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="color">Cor.</param>
        public void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, White);
            GUI.color = previous;
        }

        /// <summary>
        /// Desenha uma moldura em pixel art: borda escura, borda colorida e preenchimento.
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="border">Cor da borda.</param>
        /// <param name="fill">Cor de dentro.</param>
        /// <param name="thickness">Espessura da borda colorida.</param>
        public void Frame(Rect rect, Color border, Color fill, float thickness = 3f)
        {
            Fill(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), new Color(0f, 0f, 0f, 0.85f));
            Fill(rect, border);
            Fill(new Rect(rect.x + thickness, rect.y + thickness, rect.width - thickness * 2f, rect.height - thickness * 2f), fill);

            // Brilho no canto superior (sensação de relevo).
            Fill(new Rect(rect.x + thickness, rect.y + thickness, rect.width - thickness * 2f, 2f), new Color(1f, 1f, 1f, 0.06f));
        }

        /// <summary>Painel padrão do HUD.</summary>
        /// <param name="rect">Área.</param>
        public void Panel(Rect rect) => Frame(rect, Bronze, PanelFill, 3f);

        /// <summary>
        /// Desenha uma barra de progresso com moldura.
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="fraction">Preenchimento em [0, 1].</param>
        /// <param name="color">Cor do preenchimento.</param>
        public void DrawBar(Rect rect, float fraction, Color color)
        {
            Fill(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0f, 0f, 0f, 0.9f));
            Fill(rect, BarBack);
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), Mathf.Max(1f, rect.height * 0.3f)), new Color(1f, 1f, 1f, 0.25f));
        }

        /// <summary>
        /// Desenha uma textura com cor/opacidade.
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="texture">Textura.</param>
        /// <param name="tint">Cor multiplicada.</param>
        public void Image(Rect rect, Texture texture, Color tint)
        {
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        /// <summary>
        /// Texto com sombra; a cor final vem de <paramref name="color"/> (o estilo deve ter texto branco).
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="text">Texto sem tags de cor.</param>
        /// <param name="style">Estilo (texto branco).</param>
        /// <param name="color">Cor do texto.</param>
        public void ShadowText(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f * color.a);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }

        /// <summary>Destrói as texturas criadas.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (White != null)
            {
                UnityEngine.Object.Destroy(White);
            }

            _disposed = true;
        }

        /// <summary>
        /// Converte uma cor em hexadecimal para usar em rich text.
        /// </summary>
        /// <param name="color">Cor.</param>
        /// <returns>Texto como "#RRGGBB".</returns>
        public static string Hex(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static GUIStyle MakeText(int size, Color color, FontStyle fontStyle, TextAnchor alignment)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                richText = true
            };
            style.normal.textColor = color;
            return style;
        }
    }
}
