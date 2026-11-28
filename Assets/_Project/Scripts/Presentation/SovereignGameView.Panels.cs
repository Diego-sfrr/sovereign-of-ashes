using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Painéis do HUD que mudam de lugar: Civilizações (esquerda), Crônica (direita) e Intervenção. Cada um tem uma
    /// barrinha de arrastar no alto (mouse ou dedo); toque duplo na barrinha devolve o painel ao lugar padrão.
    /// A posição fica salva nas preferências.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const int PanelIdLeft = 0;
        private const int PanelIdRight = 1;
        private const int PanelIdRing = 2;

        private readonly Rect[] _panelRects = new Rect[SovereignOfAshes.Session.GameSettings.MovablePanels];
        private int _draggingPanel = -1;
        private Vector2 _dragGrab;
        private bool _panelsLocked;

        private float HandleHeight => _mobileUi ? 24f : 16f;

        // Usado só pelo modo de captura automática de telas.
        internal void MovePanelForCapture(int id, int x, int y) => _settings.SetPanelOffset(id, x, y);

        /// <summary>Posição final do painel: a padrão mais o deslocamento salvo, sem sair da tela.</summary>
        private Rect PlacePanel(int id, Rect basis, float width, float height)
        {
            (int x, int y) = _settings.GetPanelOffset(id);
            var rect = new Rect(basis.x + x, basis.y + y, basis.width, basis.height);

            // Mantém o painel (e a barrinha) sempre ao alcance do dedo.
            float minY = TopBarHeight + HandleHeight + 2f;
            rect.x = Mathf.Clamp(rect.x, 4f, Mathf.Max(4f, width - rect.width - 4f));
            rect.y = Mathf.Clamp(rect.y, minY, Mathf.Max(minY, height - 90f));
            _panelRects[id] = rect;
            return rect;
        }

        /// <summary>Barrinha de arrastar acima do painel.</summary>
        private void DrawPanelHandle(int id, Rect basis, Rect panel)
        {
            var handle = new Rect(panel.x, panel.y - HandleHeight, panel.width, HandleHeight - 2f);
            Event current = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            bool hover = handle.Contains(current != null ? current.mousePosition : new Vector2(-1f, -1f));
            bool dragging = _draggingPanel == id;

            _theme.Fill(handle, dragging ? new Color(0.45f, 0.32f, 0.12f, 0.95f)
                : hover ? new Color(0.30f, 0.21f, 0.10f, 0.92f) : new Color(0.16f, 0.11f, 0.08f, 0.82f));
            _theme.Outline(handle, dragging || hover ? GuiTheme.Gold : GuiTheme.Bronze, 1f);

            // Três pontinhos no meio: indica que dá para arrastar.
            for (int i = -1; i <= 1; i++)
            {
                _theme.Fill(new Rect(handle.center.x + i * 10f - 2f, handle.center.y - 2f, 4f, 4f), dragging || hover ? GuiTheme.Gold : GuiTheme.TextDim);
            }

            if (current == null || _panelsLocked)
            {
                return;
            }

            switch (current.GetTypeForControl(control))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && handle.Contains(current.mousePosition))
                    {
                        if (current.clickCount >= 2)
                        {
                            // Toque duplo: volta ao lugar padrão.
                            _settings.SetPanelOffset(id, 0, 0);
                            SaveSettings();
                        }
                        else
                        {
                            _draggingPanel = id;
                            _dragGrab = current.mousePosition - new Vector2(panel.x, panel.y);
                            GUIUtility.hotControl = control;
                        }

                        current.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (_draggingPanel == id && GUIUtility.hotControl == control)
                    {
                        Vector2 target = current.mousePosition - _dragGrab;
                        _settings.SetPanelOffset(id, Mathf.RoundToInt(target.x - basis.x), Mathf.RoundToInt(target.y - basis.y));
                        current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (_draggingPanel == id && GUIUtility.hotControl == control)
                    {
                        GUIUtility.hotControl = 0;
                        _draggingPanel = -1;
                        SaveSettings();
                        current.Use();
                    }

                    break;
            }
        }

        /// <summary>Se o ponto está sobre algum painel (o clique não deve cair no tabuleiro por baixo).</summary>
        private bool IsOverPanel(Vector2 point)
        {
            for (int i = 0; i < _panelRects.Length; i++)
            {
                Rect rect = _panelRects[i];

                if (rect.width > 0f && new Rect(rect.x, rect.y - HandleHeight, rect.width, rect.height + HandleHeight).Contains(point))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
