using System;
using System.Collections.Generic;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "Pactos" da view: antes de começar a partida o jogador escolhe 1 de 3 pactos sorteados
    /// (ou segue sem pacto). O efeito do pacto é aplicado pelo <see cref="CovenantCatalog"/> na configuração.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private GameMode _pendingMode = GameMode.Standard;
        private IReadOnlyList<Covenant> _covenantChoices = new List<Covenant>();

        private void OpenCovenantChoice(GameMode mode)
        {
            _pendingMode = mode;

            // Com semente fixa as opções são sempre as mesmas, para a mesma partida poder ser repetida.
            var random = _settings.Seed != 0 ? new System.Random(_settings.Seed) : new System.Random();
            _covenantChoices = CovenantCatalog.PickThree(random);
            _uiScreen = UiScreen.Covenant;
        }

        private void DrawCovenantChoice(float width, float height)
        {
            string modeName = _pendingMode == GameMode.Endless ? "Modo Infinito" : "Nova partida";
            _theme.ShadowText(new Rect(0f, 48f, width, 56f), "ESCOLHA UM PACTO", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(width * 0.5f - 280f, 108f, 560f);
            _theme.ShadowText(new Rect(0f, 120f, width, 30f),
                $"{modeName}: um pacto muda as regras desta partida. Toda vantagem tem um preço.", _theme.Centered, GuiTheme.Text);

            const float cardWidth = 360f;
            const float cardHeight = 430f;
            const float gap = 36f;
            int count = _covenantChoices.Count;
            float startX = (width - (count * cardWidth + (count - 1) * gap)) * 0.5f;
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);

            for (int i = 0; i < count; i++)
            {
                Covenant covenant = _covenantChoices[i];
                var rect = new Rect(startX + i * (cardWidth + gap), 176f, cardWidth, cardHeight);
                bool hover = rect.Contains(mouse);
                _theme.Frame(rect, hover ? GuiTheme.Gold : GuiTheme.Bronze,
                    hover ? new Color(0.20f, 0.14f, 0.07f) : new Color(0.10f, 0.08f, 0.08f), hover ? 5f : 4f);

                _theme.ShadowText(new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, 48f), covenant.Name, _theme.Heading, GuiTheme.Gold);
                _theme.Divider(rect.x + 30f, rect.y + 82f, rect.width - 60f);

                GUI.Label(new Rect(rect.x + 28f, rect.y + 104f, rect.width - 56f, 24f), "VANTAGEM", _theme.Tag);
                var boon = new GUIContent(covenant.Boon);
                float boonHeight = BodyStyle.CalcHeight(boon, rect.width - 56f);
                Color previous = GUI.color;
                GUI.color = GuiTheme.Good;
                GUI.Label(new Rect(rect.x + 28f, rect.y + 130f, rect.width - 56f, boonHeight), boon, BodyStyle);

                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 28f, rect.y + 246f, rect.width - 56f, 24f), "PREÇO", _theme.Tag);
                var curse = new GUIContent(covenant.Curse);
                float curseHeight = BodyStyle.CalcHeight(curse, rect.width - 56f);
                GUI.color = GuiTheme.Bad;
                GUI.Label(new Rect(rect.x + 28f, rect.y + 272f, rect.width - 56f, curseHeight), curse, BodyStyle);
                GUI.color = previous;

                _theme.ShadowText(new Rect(rect.x, rect.yMax - 52f, rect.width, 36f), hover ? "CLIQUE PARA ESCOLHER" : string.Empty,
                    _theme.Tag, GuiTheme.Gold);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    StartGame(_pendingMode, covenant.Id);
                    return;
                }
            }

            float below = 176f + cardHeight + 28f;

            if (MenuButton(new Rect(width * 0.5f - 330f, below, 320f, 56f), "JOGAR SEM PACTO"))
            {
                StartGame(_pendingMode);
            }

            if (MenuButton(new Rect(width * 0.5f + 10f, below, 320f, 56f), "VOLTAR"))
            {
                _uiScreen = UiScreen.Menu;
            }
        }
    }
}
