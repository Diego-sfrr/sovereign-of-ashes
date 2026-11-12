using SovereignOfAshes.Cards;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Estado visual de uma carta na mão.
    /// </summary>
    public struct CardVisualState
    {
        /// <summary>Selecionada (aguardando alvo).</summary>
        public bool Selected { get; set; }

        /// <summary>Mouse sobre a carta.</summary>
        public bool Hovered { get; set; }

        /// <summary>Pode ser usada agora.</summary>
        public bool Usable { get; set; }

        /// <summary>Há Fé suficiente.</summary>
        public bool Affordable { get; set; }

        /// <summary>Tempo (segundos) para animar o brilho da seleção.</summary>
        public float Time { get; set; }
    }

    /// <summary>
    /// Desenha uma carta completa em IMGUI: moldura, faixa do nome, ilustração em pixel art,
    /// linha de tipo, texto e gema de custo.
    /// </summary>
    public static class CardPainter
    {
        private static readonly Color Parchment = new Color(0.17f, 0.13f, 0.11f, 1f);
        private static readonly Color NameBanner = new Color(0.36f, 0.10f, 0.08f, 1f);
        private static readonly Color ArtBack = new Color(0.06f, 0.05f, 0.08f, 1f);

        /// <summary>
        /// Desenha a carta.
        /// </summary>
        /// <param name="rect">Área da carta.</param>
        /// <param name="card">Carta.</param>
        /// <param name="art">Ilustração.</param>
        /// <param name="gem">Sprite da gema de custo.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="state">Estado visual.</param>
        /// <param name="fillArtBox">
        /// <c>true</c> para ilustrações pintadas: preenchem o quadro inteiro (recorte centralizado, sem distorcer).
        /// <c>false</c> para sprites pequenos: ficam centralizados no tamanho original.
        /// </param>
        public static void Draw(Rect rect, ICardDefinition card, Texture2D art, Texture2D gem, GuiTheme theme, CardVisualState state,
            bool fillArtBox = false)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(state.Time * 6f);
            Color border = state.Selected
                ? Color.Lerp(GuiTheme.Gold, Color.white, 0.35f * pulse)
                : state.Hovered && state.Usable ? GuiTheme.Gold : GuiTheme.Bronze;

            if (state.Selected)
            {
                // Halo da seleção.
                theme.Fill(new Rect(rect.x - 8f, rect.y - 8f, rect.width + 16f, rect.height + 16f),
                    new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, 0.18f + 0.15f * pulse));
            }

            theme.Frame(rect, border, Parchment, 4f);

            var banner = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 32f);
            theme.Frame(banner, new Color(0.18f, 0.05f, 0.04f), NameBanner, 2f);
            theme.ShadowText(banner, card.DisplayName, theme.CardName, Color.white);

            var artBox = new Rect(rect.x + 16f, rect.y + 50f, rect.width - 32f, 88f);
            theme.Frame(artBox, GuiTheme.Bronze, ArtBack, 2f);

            if (art != null && fillArtBox)
            {
                // Dentro da borda de 2px do quadro.
                DrawCover(new Rect(artBox.x + 2f, artBox.y + 2f, artBox.width - 4f, artBox.height - 4f), art);
            }
            else if (art != null)
            {
                float artSize = 80f;
                theme.Image(new Rect(artBox.center.x - artSize * 0.5f, artBox.center.y - artSize * 0.5f, artSize, artSize), art, Color.white);
            }

            string target = card.IsGlobal() ? "Uso imediato" : card.RequiresInvader ? "Alvo: invasor" : "Alvo: setor";
            GUI.Label(new Rect(rect.x + 8f, rect.y + 142f, rect.width - 16f, 18f),
                $"{DisplayNames.Concept(card.PrimaryConcept)}  ·  {target}", theme.CardType);

            GUI.Label(new Rect(rect.x + 12f, rect.y + 162f, rect.width - 24f, rect.height - 170f), card.Description, theme.CardBody);

            // O corpo da gema ocupa ~40% superior do sprite; o número fica centrado nele.
            var gemRect = new Rect(rect.x - 16f, rect.y - 18f, 56f, 56f);
            theme.Image(gemRect, gem, Color.white);
            theme.ShadowText(new Rect(gemRect.x, gemRect.y + gemRect.height * 0.40f - 14f, gemRect.width, 28f), card.Cost.ToString(),
                theme.CostNumber, state.Affordable ? Color.white : GuiTheme.Bad);

            if (!state.Usable)
            {
                theme.Fill(rect, new Color(0f, 0f, 0f, 0.45f));
            }
        }

        /// <summary>
        /// Desenha a carta ampliada (pré-visualização ao passar o mouse): a mesma carta, redesenhada em
        /// <paramref name="scale"/> vezes o tamanho padrão, com fontes maiores para o texto ficar legível.
        /// </summary>
        /// <param name="rect">Área da carta ampliada (proporção 178x240).</param>
        /// <param name="card">Carta.</param>
        /// <param name="art">Ilustração.</param>
        /// <param name="gem">Sprite da gema de custo.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="affordable">Se há Fé suficiente.</param>
        /// <param name="fillArtBox"><c>true</c> para ilustrações pintadas.</param>
        public static void DrawZoomed(Rect rect, ICardDefinition card, Texture2D art, Texture2D gem, GuiTheme theme, bool affordable,
            bool fillArtBox)
        {
            float s = rect.width / 178f;

            theme.Fill(new Rect(rect.x - 10f, rect.y - 10f, rect.width + 20f, rect.height + 20f), new Color(0f, 0f, 0f, 0.55f));
            theme.Frame(rect, GuiTheme.Gold, Parchment, 5f);

            var banner = new Rect(rect.x + 12f * s, rect.y + 12f * s, rect.width - 24f * s, 34f * s);
            theme.Frame(banner, new Color(0.18f, 0.05f, 0.04f), NameBanner, 2f);
            theme.ShadowText(banner, card.DisplayName, theme.CardNameLarge, Color.white);

            var artBox = new Rect(rect.x + 18f * s, rect.y + 54f * s, rect.width - 36f * s, 96f * s);
            theme.Frame(artBox, GuiTheme.Bronze, ArtBack, 2f);

            if (art != null && fillArtBox)
            {
                DrawCover(new Rect(artBox.x + 2f, artBox.y + 2f, artBox.width - 4f, artBox.height - 4f), art);
            }
            else if (art != null)
            {
                float artSize = 90f * s;
                theme.Image(new Rect(artBox.center.x - artSize * 0.5f, artBox.center.y - artSize * 0.5f, artSize, artSize), art, Color.white);
            }

            string target = card.IsGlobal() ? "Uso imediato" : card.RequiresInvader ? "Alvo: invasor" : "Alvo: setor";
            GUI.Label(new Rect(rect.x + 8f * s, rect.y + 156f * s, rect.width - 16f * s, 20f * s),
                $"{DisplayNames.Concept(card.PrimaryConcept)}  ·  {target}", theme.CardTypeLarge);
            GUI.Label(new Rect(rect.x + 14f * s, rect.y + 180f * s, rect.width - 28f * s, rect.height - 188f * s), card.Description, theme.CardBodyLarge);

            var gemRect = new Rect(rect.x - 18f * s, rect.y - 20f * s, 60f * s, 60f * s);
            theme.Image(gemRect, gem, Color.white);
            theme.ShadowText(new Rect(gemRect.x, gemRect.y + gemRect.height * 0.40f - 15f * s, gemRect.width, 30f * s), card.Cost.ToString(),
                theme.CostNumberLarge, affordable ? Color.white : GuiTheme.Bad);
        }

        /// <summary>
        /// Desenha a textura cobrindo toda a área, recortando o excesso no centro (como CSS <c>object-fit: cover</c>).
        /// </summary>
        /// <param name="area">Área de destino.</param>
        /// <param name="texture">Textura.</param>
        internal static void DrawCover(Rect area, Texture texture)
        {
            if (area.width <= 0f || area.height <= 0f || texture.width <= 0 || texture.height <= 0)
            {
                return;
            }

            float areaAspect = area.width / area.height;
            float textureAspect = (float)texture.width / texture.height;
            var uv = new Rect(0f, 0f, 1f, 1f);

            if (textureAspect > areaAspect)
            {
                uv.width = areaAspect / textureAspect;
                uv.x = (1f - uv.width) * 0.5f;
            }
            else
            {
                uv.height = textureAspect / areaAspect;
                uv.y = (1f - uv.height) * 0.5f;
            }

            GUI.DrawTextureWithTexCoords(area, texture, uv);
        }
    }
}
