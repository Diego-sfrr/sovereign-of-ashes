using SovereignOfAshes.Board;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "Intervenção Divina" da view: o painelzinho ao lado do tabuleiro em que o jogador gasta Fé para girar
    /// um anel uma casa ou congelá-lo. A regra é do <see cref="RingControlService"/>; aqui só se desenha e se clica.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const float RingPanelWidth = 184f;

        private static readonly RingType[] RingOrder =
        {
            RingType.OuterWilderness, RingType.MiddleCivilization, RingType.InnerSanctuary
        };

        private static string ShortRing(RingType ring)
        {
            switch (ring)
            {
                case RingType.OuterWilderness: return "Ermos";
                case RingType.MiddleCivilization: return "Meio";
                default: return "Santuário";
            }
        }

        private void DrawRingControl(float width)
        {
            RingControlService control = _session.RingControl;
            bool available = control.CanUseNow && !IsAnimating;

            // Ao lado esquerdo do tabuleiro, medido em coordenadas da interface (o tabuleiro vive no mundo da câmera).
            Vector2 center = WorldToGui(Vector2.zero);
            float radius = Mathf.Abs(WorldToGui(new Vector2(0f, _layout.OuterEdge)).y - center.y);
            var panel = new Rect(center.x - radius - 22f - RingPanelWidth, center.y - 131f, RingPanelWidth, 262f);

            _theme.Frame(panel, available ? GuiTheme.Bronze : new Color(0.25f, 0.2f, 0.16f), new Color(0.07f, 0.06f, 0.08f, 0.94f), 3f);
            _theme.ShadowText(new Rect(panel.x, panel.y + 8f, panel.width, 22f), "INTERVENÇÃO DIVINA", _theme.Tag, GuiTheme.Gold);

            string status = control.UsedThisTurn ? "Já usada neste turno" : "1 por turno";
            _theme.ShadowText(new Rect(panel.x, panel.y + 30f, panel.width, 20f), status, _theme.Small,
                control.UsedThisTurn ? GuiTheme.Bad : GuiTheme.TextDim);

            for (int i = 0; i < RingOrder.Length; i++)
            {
                RingType ring = RingOrder[i];
                float y = panel.y + 62f + i * 54f;
                bool frozen = control.IsFrozen(ring);

                GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 20f),
                    frozen ? $"{ShortRing(ring)} <color={GuiTheme.Hex(GuiTheme.Gold)}>(congelado)</color>" : ShortRing(ring), _theme.Tag);

                bool canRotate = available && control.CanRotate();
                bool canFreeze = available && control.CanFreeze(ring);

                if (RingButton(new Rect(panel.x + 10f, y + 21f, 48f, 28f), "<", canRotate))
                {
                    TryRingAction(control.TryRotate(ring, RotationDirection.CounterClockwise), $"Anel {ShortRing(ring)} girado no sentido anti-horário.");
                }

                if (RingButton(new Rect(panel.x + 64f, y + 21f, 48f, 28f), ">", canRotate))
                {
                    TryRingAction(control.TryRotate(ring, RotationDirection.Clockwise), $"Anel {ShortRing(ring)} girado no sentido horário.");
                }

                if (RingButton(new Rect(panel.x + 118f, y + 21f, 56f, 28f), "||", canFreeze))
                {
                    TryRingAction(control.TryFreeze(ring), $"Anel {ShortRing(ring)} congelado: ignora a próxima rotação.");
                }
            }

            GUI.Label(new Rect(panel.x + 10f, panel.yMax - 40f, panel.width - 20f, 36f),
                $"< anti-horário · > horário · || congelar\nGirar {control.RotateCost} Fé · Congelar {control.FreezeCost} Fé", _theme.Small);
        }

        private bool RingButton(Rect rect, string label, bool enabled)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);
            bool hover = enabled && rect.Contains(mouse);
            Color border = enabled ? (hover ? GuiTheme.Gold : GuiTheme.Bronze) : new Color(0.22f, 0.19f, 0.16f);
            Color fill = enabled ? (hover ? new Color(0.34f, 0.23f, 0.10f) : new Color(0.17f, 0.12f, 0.08f)) : new Color(0.09f, 0.08f, 0.08f);
            _theme.Frame(rect, border, fill, 2f);
            _theme.ShadowText(rect, label, _theme.SmallNumber, enabled ? Color.white : GuiTheme.TextDim);

            GUI.enabled = enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private void TryRingAction(bool done, string message)
        {
            if (done)
            {
                _selectedCard = -1;
                ShowHint(message);
            }
        }
    }
}
