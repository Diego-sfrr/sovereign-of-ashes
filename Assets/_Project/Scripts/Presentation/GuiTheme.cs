using System;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Paleta e estilos do HUD (IMGUI) em tom de fantasia sombria: carvão, ouro velho e brasa.
    /// Deve ser criado dentro de <c>OnGUI</c>, pois depende de <see cref="GUI.skin"/>.
    /// </summary>
    public sealed class GuiTheme : IDisposable
    {
        /// <summary>Fundo da câmera.</summary>
        public static readonly Color Background = new Color(0.05f, 0.045f, 0.05f);

        /// <summary>Ouro velho (títulos, destaques).</summary>
        public static readonly Color Gold = new Color(0.86f, 0.70f, 0.38f);

        /// <summary>Texto comum.</summary>
        public static readonly Color Text = new Color(0.90f, 0.86f, 0.80f);

        /// <summary>Texto secundário.</summary>
        public static readonly Color TextDim = new Color(0.62f, 0.58f, 0.54f);

        /// <summary>Coisas boas para o jogador.</summary>
        public static readonly Color Good = new Color(0.55f, 0.82f, 0.52f);

        /// <summary>Coisas ruins para o jogador.</summary>
        public static readonly Color Bad = new Color(0.93f, 0.42f, 0.36f);

        /// <summary>Cor da Fé.</summary>
        public static readonly Color FaithColor = new Color(0.45f, 0.65f, 0.98f);

        /// <summary>Cor da Racionalidade.</summary>
        public static readonly Color RationalityColor = new Color(0.95f, 0.62f, 0.25f);

        /// <summary>Fundo de barra.</summary>
        public static readonly Color BarBack = new Color(0.16f, 0.15f, 0.16f);

        private readonly Texture2D _panelTexture;
        private readonly Texture2D _cardTexture;
        private readonly Texture2D _cardHoverTexture;
        private readonly Texture2D _cardSelectedTexture;
        private readonly Texture2D _buttonTexture;
        private readonly Texture2D _buttonHoverTexture;
        private bool _disposed;

        /// <summary>Textura branca 1x1 para desenhar barras com <see cref="GUI.color"/>.</summary>
        public Texture2D White { get; }

        /// <summary>Painel escuro semitransparente.</summary>
        public GUIStyle Panel { get; }

        /// <summary>Título de seção.</summary>
        public GUIStyle Title { get; }

        /// <summary>Título grande (fim de jogo).</summary>
        public GUIStyle BigTitle { get; }

        /// <summary>Texto comum.</summary>
        public GUIStyle Label { get; }

        /// <summary>Texto pequeno e discreto.</summary>
        public GUIStyle Small { get; }

        /// <summary>Texto centralizado (dicas).</summary>
        public GUIStyle Centered { get; }

        /// <summary>Rótulos sobre o tabuleiro.</summary>
        public GUIStyle BoardTag { get; }

        /// <summary>Etiqueta de invasor sobre o tabuleiro.</summary>
        public GUIStyle InvaderTag { get; }

        /// <summary>Botão comum.</summary>
        public GUIStyle Button { get; }

        /// <summary>Carta na mão.</summary>
        public GUIStyle Card { get; }

        /// <summary>Carta selecionada.</summary>
        public GUIStyle CardSelected { get; }

        /// <summary>
        /// Cria texturas e estilos. Chame apenas dentro de <c>OnGUI</c>.
        /// </summary>
        public GuiTheme()
        {
            White = MakeTexture(Color.white);
            _panelTexture = MakeTexture(new Color(0.08f, 0.07f, 0.08f, 0.88f));
            _cardTexture = MakeTexture(new Color(0.17f, 0.13f, 0.11f, 0.97f));
            _cardHoverTexture = MakeTexture(new Color(0.24f, 0.18f, 0.13f, 0.98f));
            _cardSelectedTexture = MakeTexture(new Color(0.42f, 0.30f, 0.12f, 1f));
            _buttonTexture = MakeTexture(new Color(0.36f, 0.12f, 0.09f, 1f));
            _buttonHoverTexture = MakeTexture(new Color(0.52f, 0.17f, 0.11f, 1f));

            Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
            Panel.normal.background = _panelTexture;

            Title = MakeText(17, Gold, FontStyle.Bold);
            BigTitle = MakeText(46, Gold, FontStyle.Bold);
            BigTitle.alignment = TextAnchor.MiddleCenter;
            Label = MakeText(14, Text, FontStyle.Normal);
            Small = MakeText(12, TextDim, FontStyle.Normal);
            Centered = MakeText(16, Text, FontStyle.Normal);
            Centered.alignment = TextAnchor.MiddleCenter;

            BoardTag = MakeText(12, Text, FontStyle.Bold);
            BoardTag.alignment = TextAnchor.MiddleCenter;
            BoardTag.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
            BoardTag.padding = new RectOffset(4, 4, 2, 2);

            InvaderTag = MakeText(12, Color.white, FontStyle.Bold);
            InvaderTag.alignment = TextAnchor.MiddleCenter;
            InvaderTag.normal.background = MakeTexture(new Color(0.55f, 0.08f, 0.08f, 0.92f));
            InvaderTag.padding = new RectOffset(4, 4, 2, 2);

            Button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, richText = true };
            Button.normal.background = _buttonTexture;
            Button.hover.background = _buttonHoverTexture;
            Button.active.background = _cardSelectedTexture;
            Button.normal.textColor = Text;
            Button.hover.textColor = Color.white;
            Button.active.textColor = Color.white;

            Card = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(10, 10, 10, 10)
            };
            Card.normal.background = _cardTexture;
            Card.hover.background = _cardHoverTexture;
            Card.active.background = _cardSelectedTexture;
            Card.normal.textColor = Text;
            Card.hover.textColor = Color.white;
            Card.active.textColor = Color.white;

            CardSelected = new GUIStyle(Card);
            CardSelected.normal.background = _cardSelectedTexture;
            CardSelected.hover.background = _cardSelectedTexture;
            CardSelected.normal.textColor = Color.white;
        }

        /// <summary>
        /// Desenha uma barra de progresso.
        /// </summary>
        /// <param name="rect">Área.</param>
        /// <param name="fraction">Preenchimento em [0, 1].</param>
        /// <param name="color">Cor do preenchimento.</param>
        public void DrawBar(Rect rect, float fraction, Color color)
        {
            Color previous = GUI.color;
            GUI.color = BarBack;
            GUI.DrawTexture(rect, White);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), White);
            GUI.color = previous;
        }

        /// <summary>Destrói as texturas criadas.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DestroyTexture(White);
            DestroyTexture(_panelTexture);
            DestroyTexture(_cardTexture);
            DestroyTexture(_cardHoverTexture);
            DestroyTexture(_cardSelectedTexture);
            DestroyTexture(_buttonTexture);
            DestroyTexture(_buttonHoverTexture);
            DestroyTexture(BoardTag.normal.background);
            DestroyTexture(InvaderTag.normal.background);
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

        private static GUIStyle MakeText(int size, Color color, FontStyle fontStyle)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                wordWrap = true,
                richText = true
            };
            style.normal.textColor = color;
            return style;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
