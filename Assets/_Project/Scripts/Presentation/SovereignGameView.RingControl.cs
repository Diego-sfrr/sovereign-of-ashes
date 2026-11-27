using SovereignOfAshes.Board;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "Intervenção Divina" da view: o painelzinho ao lado do tabuleiro em que o jogador gasta Fé para girar
    /// um anel uma casa ou congelá-lo. A regra é do <see cref="RingControlService"/>; aqui só se desenha e se clica.
    /// O painel abre e fecha (clique no título) e lembra o estado.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const float RingPanelWidth = 196f;

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
            bool collapsed = IsCollapsed(PanelIntervention);
            float panelHeight = collapsed ? 44f : _mobileUi ? 312f : 282f;

            // Ao lado esquerdo do tabuleiro, medido em coordenadas da interface (o tabuleiro vive no mundo da câmera).
            Vector2 center = WorldToGui(Vector2.zero);
            float radius = Mathf.Abs(WorldToGui(new Vector2(0f, _layout.OuterEdge)).y - center.y);
            var panel = new Rect(center.x - radius - 22f - RingPanelWidth, center.y - 141f, RingPanelWidth, panelHeight);

            _theme.Frame(panel, available ? GuiTheme.Bronze : new Color(0.25f, 0.2f, 0.16f), new Color(0.07f, 0.06f, 0.08f, 0.94f), 3f);

            float y = panel.y + 10f;

            if (!SectionOpen(ref y, panel.x + 12f, panel.width - 24f, "Intervenção", PanelIntervention, compact: true))
            {
                return;
            }

            string status = control.UsedThisTurn ? "Já usada neste turno" : "1 por turno";
            GUI.Label(new Rect(panel.x + 12f, y - 6f, panel.width - 24f, 20f), status, control.UsedThisTurn ? StatusBadStyle : _theme.Small);
            y += 20f;

            for (int i = 0; i < RingOrder.Length; i++)
            {
                RingType ring = RingOrder[i];
                float rowY = y + i * 54f;
                bool frozen = control.IsFrozen(ring);

                GUI.Label(new Rect(panel.x + 12f, rowY, panel.width - 24f, 20f),
                    frozen ? $"{ShortRing(ring)} <color={GuiTheme.Hex(GuiTheme.Gold)}>(congelado)</color>" : ShortRing(ring), _theme.Tag);

                bool canRotate = available && control.CanRotate();
                bool canFreeze = available && control.CanFreeze(ring);

                if (RingButton(new Rect(panel.x + 12f, rowY + 22f, 52f, 28f), "<", canRotate))
                {
                    TryRingAction(control.TryRotate(ring, RotationDirection.CounterClockwise), $"Anel {ShortRing(ring)} girado no sentido anti-horário.");
                }

                if (RingButton(new Rect(panel.x + 70f, rowY + 22f, 52f, 28f), ">", canRotate))
                {
                    TryRingAction(control.TryRotate(ring, RotationDirection.Clockwise), $"Anel {ShortRing(ring)} girado no sentido horário.");
                }

                if (RingButton(new Rect(panel.x + 128f, rowY + 22f, 56f, 28f), "||", canFreeze))
                {
                    TryRingAction(control.TryFreeze(ring), $"Anel {ShortRing(ring)} congelado: ignora a próxima rotação.");
                }
            }

            float legendY = y + 3 * 54f + 2f;
            GUI.Label(new Rect(panel.x + 12f, legendY, panel.width - 24f, 20f), $"<  >  girar ({control.RotateCost} Fé)", _theme.Small);
            GUI.Label(new Rect(panel.x + 12f, legendY + 18f, panel.width - 24f, 20f), $"||  congelar ({control.FreezeCost} Fé)", _theme.Small);
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

            if (clicked)
            {
                PlaySound(Audio.SoundId.Click);
            }

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
