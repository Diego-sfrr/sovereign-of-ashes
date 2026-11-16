using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "civilização" da view: a tela de escolha antes do pacto (4 seitas, cada uma com vantagem e preço).
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private string _pendingCivilization = CivilizationCatalog.DefaultId;

        private void OpenCivilizationChoice(GameMode mode)
        {
            _pendingMode = mode;
            _uiScreen = UiScreen.Civilization;
        }

        private void DrawCivilizationChoice(float width, float height)
        {
            string modeName = _pendingMode == GameMode.Endless ? "Modo Infinito" : "Nova partida";
            const float cardHeight = 450f;
            float top = Mathf.Max(170f, (height - cardHeight) * 0.5f + 30f);
            float head = top - 150f;
            _theme.ShadowText(new Rect(0f, head, width, 56f), "ESCOLHA SUA CIVILIZAÇÃO", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(width * 0.5f - 300f, head + 60f, 600f);
            _theme.ShadowText(new Rect(0f, head + 72f, width, 30f),
                $"{modeName}: cada seita muda como você joga. Depois você ainda escolhe um Pacto.", _theme.Centered, GuiTheme.Text);

            int count = CivilizationCatalog.All.Count;
            const float cardWidth = 340f;
            const float gap = 22f;
            float startX = (width - (count * cardWidth + (count - 1) * gap)) * 0.5f;
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);

            for (int i = 0; i < count; i++)
            {
                CivilizationDefinition civilization = CivilizationCatalog.All[i];
                var rect = new Rect(startX + i * (cardWidth + gap), top, cardWidth, cardHeight);
                bool hover = rect.Contains(mouse);
                _theme.Frame(rect, hover ? GuiTheme.Gold : GuiTheme.Bronze,
                    hover ? new Color(0.20f, 0.14f, 0.07f) : new Color(0.10f, 0.08f, 0.08f), hover ? 5f : 4f);

                Texture2D portrait = _art.Painting(ArtCatalog.CivilizationPortraitPath(false));

                if (portrait != null)
                {
                    var portraitRect = new Rect(rect.x + 14f, rect.y + 14f, 84f, 84f);
                    _theme.Frame(portraitRect, GuiTheme.Bronze, Color.black, 2f);
                    CardPainter.DrawCover(new Rect(portraitRect.x + 2f, portraitRect.y + 2f, portraitRect.width - 4f, portraitRect.height - 4f), portrait);
                }

                GUI.Label(new Rect(rect.x + 108f, rect.y + 16f, rect.width - 120f, 56f), civilization.Name, _theme.Title);
                GUI.Label(new Rect(rect.x + 108f, rect.y + 70f, rect.width - 120f, 28f), $"Poder {civilization.MilitaryPower}", HintStyle);

                var style = new GUIContent(civilization.Style);
                float styleHeight = HintStyle.CalcHeight(style, rect.width - 40f);
                GUI.Label(new Rect(rect.x + 20f, rect.y + 112f, rect.width - 40f, styleHeight), style, HintStyle);

                Color previous = GUI.color;
                float y = rect.y + 112f + styleHeight + 10f;

                var quote = new GUIContent("\u201C" + Story.CivilizationQuote(civilization.Id) + "\u201D");
                float quoteHeight = HintStyle.CalcHeight(quote, rect.width - 40f);
                GUI.color = GuiTheme.Gold;
                GUI.Label(new Rect(rect.x + 20f, y, rect.width - 40f, quoteHeight), quote, HintStyle);
                y += quoteHeight + 12f;

                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 20f, y, rect.width - 40f, 22f), "VANTAGEM", _theme.Tag);
                var boon = new GUIContent(civilization.Boon);
                float boonHeight = HintStyle.CalcHeight(boon, rect.width - 40f);
                GUI.color = GuiTheme.Good;
                GUI.Label(new Rect(rect.x + 20f, y + 22f, rect.width - 40f, boonHeight), boon, HintStyle);
                y += 22f + boonHeight + 14f;

                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 20f, y, rect.width - 40f, 22f), "PREÇO", _theme.Tag);
                var curse = new GUIContent(civilization.Curse);
                float curseHeight = HintStyle.CalcHeight(curse, rect.width - 40f);
                GUI.color = GuiTheme.Bad;
                GUI.Label(new Rect(rect.x + 20f, y + 22f, rect.width - 40f, curseHeight), curse, HintStyle);
                GUI.color = previous;

                _theme.ShadowText(new Rect(rect.x, rect.yMax - 46f, rect.width, 30f), hover ? "CLIQUE PARA ESCOLHER" : string.Empty,
                    _theme.Tag, GuiTheme.Gold);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    _pendingCivilization = civilization.Id;
                    OpenCovenantChoice(_pendingMode);
                    return;
                }
            }

            if (MenuButton(new Rect(width * 0.5f - 130f, top + cardHeight + 26f, 260f, 56f), "VOLTAR"))
            {
                _uiScreen = UiScreen.Menu;
            }
        }
    }
}
