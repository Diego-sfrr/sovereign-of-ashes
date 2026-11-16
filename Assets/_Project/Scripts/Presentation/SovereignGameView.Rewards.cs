using SovereignOfAshes.Cards;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "Oferendas Divinas" da view: o modal em que o jogador escolhe 1 de 3 cartas (ou recusa) a cada
    /// poucos turnos. A decisão em si é do <see cref="RewardSystem"/>; aqui só se desenha e se clica.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const float OfferCardWidth = 270f;
        private const float OfferCardHeight = OfferCardWidth * 240f / 178f;
        private const float OfferSpacing = 50f;

        private bool ShouldShowOffer => _session != null && _session.Rewards.HasPending && !IsAnimating;

        private void DrawRewardOffer(float width, float height)
        {
            RewardOffer offer = _session.Rewards.Pending;

            if (offer == null)
            {
                return;
            }

            _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.78f));
            _theme.ShadowText(new Rect(0f, 56f, width, 56f), "OFERENDA DIVINA", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(width * 0.5f - 300f, 116f, 600f);
            _theme.ShadowText(new Rect(0f, 128f, width, 30f),
                $"Turno {offer.Turn}: escolha uma carta para levar ao seu baralho, ou recuse e receba +{_session.Rewards.SkipFaith} de Fé.",
                _theme.Centered, GuiTheme.Text);

            int count = offer.Choices.Count;
            float total = count * OfferCardWidth + (count - 1) * OfferSpacing;
            float startX = (width - total) * 0.5f;
            float y = 184f;
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);

            for (int i = 0; i < count; i++)
            {
                ICardDefinition card = offer.Choices[i];
                var slot = new Rect(startX + i * (OfferCardWidth + OfferSpacing), y, OfferCardWidth, OfferCardHeight);
                bool hover = slot.Contains(mouse);
                var drawRect = hover ? new Rect(slot.x, slot.y - 10f, slot.width, slot.height) : slot;

                Texture2D illustration = _art.CardIllustration(card);
                CardPainter.DrawZoomed(drawRect, card, illustration != null ? illustration : _art.Card(card),
                    _art.Sprite("crystal"), _theme, true, illustration != null);

                if (hover)
                {
                    _theme.Frame(new Rect(drawRect.x - 4f, drawRect.y - 4f, drawRect.width + 8f, drawRect.height + 8f), GuiTheme.Gold, Color.clear, 3f);
                }

                int owned = _session.Deck.CountOf(card.Id);
                _theme.ShadowText(new Rect(slot.x, slot.yMax + 12f, slot.width, 24f),
                    owned == 0 ? "Nova no baralho" : $"Você já tem {owned}", _theme.Centered, owned == 0 ? GuiTheme.Good : GuiTheme.TextDim);

                if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
                {
                    _session.Rewards.Choose(i);
                    ShowHint($"{card.DisplayName} foi para o seu baralho.");
                    return;
                }
            }

            float below = y + OfferCardHeight + 52f;

            if (MenuButton(new Rect(width * 0.5f - 190f, below, 380f, 58f), $"RECUSAR  (+{_session.Rewards.SkipFaith} Fé)"))
            {
                _session.Rewards.Skip();
            }

            _theme.ShadowText(new Rect(0f, below + 68f, width, 26f),
                $"Seu baralho tem {_session.Deck.TotalCount} cartas.", _theme.Centered, GuiTheme.TextDim);
        }
    }
}
