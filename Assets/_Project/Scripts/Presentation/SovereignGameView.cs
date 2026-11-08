using System.Collections.Generic;
using System.Text;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Interface visual jogável, sem nenhuma configuração: desenha o tabuleiro (malhas geradas) e o HUD (IMGUI),
    /// anima rotações e marchas, e traduz cliques em ações do <see cref="PlayerActionService"/>.
    /// <para>Uso: adicione este componente a um GameObject vazio e aperte Play.</para>
    /// <para>
    /// Toda regra vive na <see cref="GameSession"/>; esta classe só lê estado e chama ações.
    /// O IMGUI funciona com qualquer sistema de Input (antigo ou novo) e não exige pacotes.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(SovereignBootstrapper))]
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SovereignGameView : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float SidePanelWidth = 360f;
        private const float TopBarHeight = 52f;
        private const float HandAreaHeight = 270f;
        private const float CardWidth = 180f;
        private const float CardHeight = 196f;
        private const float LineHeight = 22f;
        private const float LineStep = 24f;
        private const float CardSpacing = 12f;
        private const int LogLines = 22;

        private static readonly Color InnerRingColor = new Color(0.30f, 0.23f, 0.12f);
        private static readonly Color MiddleRingColor = new Color(0.11f, 0.21f, 0.23f);
        private static readonly Color OuterRingColor = new Color(0.25f, 0.10f, 0.09f);
        private static readonly Color ConceptTint = new Color(0.62f, 0.52f, 0.26f);
        private static readonly Color EntityTint = new Color(0.22f, 0.52f, 0.28f);
        private static readonly Color StructureTint = new Color(0.36f, 0.42f, 0.58f);
        private static readonly Color TargetTint = new Color(0.95f, 0.78f, 0.35f);

        [Header("Câmera")]
        [SerializeField] private float _cameraSize = 6.2f;
        [SerializeField] private float _cameraOffsetY = -1.15f;

        [Header("Animação")]
        [Tooltip("Velocidade da rotação visual dos anéis, em casas por segundo.")]
        [SerializeField, Min(0.1f)] private float _rotationSpeed = 2.5f;

        [Tooltip("Velocidade da marcha visual dos invasores, em unidades por segundo.")]
        [SerializeField, Min(0.1f)] private float _invaderSpeed = 3.5f;

        private readonly Dictionary<int, InvaderVisual> _invaderVisuals = new Dictionary<int, InvaderVisual>();
        private readonly HashSet<TectonicSector> _validTargets = new HashSet<TectonicSector>();
        private readonly List<int> _staleInvaderIds = new List<int>();
        private readonly StringBuilder _text = new StringBuilder();

        private SovereignBootstrapper _bootstrapper;
        private GameSession _session;
        private BoardLayout _layout;
        private BoardRenderer _board;
        private GuiTheme _theme;
        private Camera _camera;
        private float[] _visualOffsets;
        private float[] _targetOffsets;
        private int _selectedCard = -1;
        private TectonicSector _hovered;
        private string _hint = string.Empty;
        private float _hintUntil;
        private float _guiScale = 1f;

        private struct InvaderVisual
        {
            public Vector2 Position;
            public RingType Ring;
        }

        private bool IsAnimating
        {
            get
            {
                if (_visualOffsets == null)
                {
                    return false;
                }

                for (int i = 0; i < _visualOffsets.Length; i++)
                {
                    if (Mathf.Abs(_visualOffsets[i] - _targetOffsets[i]) > 0.001f)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Start()
        {
            _bootstrapper = GetComponent<SovereignBootstrapper>();
            _bootstrapper.SessionCreated += Bind;
            SetupCamera();
            Bind(_bootstrapper.Session);
        }

        private void OnDestroy()
        {
            if (_bootstrapper != null)
            {
                _bootstrapper.SessionCreated -= Bind;
            }

            Unbind();
            _theme?.Dispose();
        }

        // ------------------------------------------------------------------ Ligação com a partida

        private void Bind(GameSession session)
        {
            Unbind();
            _session = session;

            if (_session == null)
            {
                return;
            }

            _layout = new BoardLayout(_session.Board.SectorsPerRing, _session.Board.Rings.Count);
            _board = new BoardRenderer(_layout, transform);

            int ringCount = _session.Board.Rings.Count;
            _visualOffsets = new float[ringCount];
            _targetOffsets = new float[ringCount];

            for (int i = 0; i < ringCount; i++)
            {
                _visualOffsets[i] = _session.Board.Rings[i].RotationOffset;
                _targetOffsets[i] = _visualOffsets[i];
            }

            _invaderVisuals.Clear();
            _selectedCard = -1;
            _session.Board.RingRotated += OnRingRotated;
        }

        private void Unbind()
        {
            if (_session != null)
            {
                _session.Board.RingRotated -= OnRingRotated;
            }

            _board?.Dispose();
            _board = null;
            _session = null;
        }

        private void OnRingRotated(RingType ring, int delta)
        {
            _targetOffsets[(int)ring] += delta;
        }

        private void SetupCamera()
        {
            _camera = Camera.main;

            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera (gerada)");
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
            }

            _camera.orthographic = true;
            _camera.orthographicSize = _cameraSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = GuiTheme.Background;
            _camera.transform.position = new Vector3(0f, _cameraOffsetY, -10f);
            _camera.transform.rotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ Atualização do mundo

        private void Update()
        {
            if (_session == null || _board == null)
            {
                return;
            }

            AnimateRings();
            AnimateInvaders();
            RefreshValidTargets();
            PaintBoard();
        }

        private void AnimateRings()
        {
            float step = _rotationSpeed * Time.deltaTime;

            for (int i = 0; i < _visualOffsets.Length; i++)
            {
                _visualOffsets[i] = Mathf.MoveTowards(_visualOffsets[i], _targetOffsets[i], step);
                _board.SetRingRotation(i, _visualOffsets[i]);
            }
        }

        private void AnimateInvaders()
        {
            float step = _invaderSpeed * Time.deltaTime;
            _staleInvaderIds.Clear();
            _staleInvaderIds.AddRange(_invaderVisuals.Keys);

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (invader.Sector == null)
                {
                    continue;
                }

                Vector2 target = GetPlateCenter(invader.Sector);
                _staleInvaderIds.Remove(invader.Id);

                if (!_invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    // Recém-chegado: entra vindo de fora do tabuleiro.
                    visual.Position = target * 1.25f;
                    visual.Ring = invader.Sector.Ring;
                }

                if (visual.Ring == invader.Sector.Ring && Vector2.Distance(visual.Position, target) < 0.75f)
                {
                    // Mesmo anel: acompanha a rotação da placa exatamente.
                    visual.Position = target;
                }
                else
                {
                    visual.Position = Vector2.MoveTowards(visual.Position, target, step);

                    if (Vector2.Distance(visual.Position, target) < 0.01f)
                    {
                        visual.Ring = invader.Sector.Ring;
                    }
                }

                _invaderVisuals[invader.Id] = visual;
            }

            foreach (int id in _staleInvaderIds)
            {
                _invaderVisuals.Remove(id);
            }
        }

        private void RefreshValidTargets()
        {
            _validTargets.Clear();

            if (_selectedCard < 0)
            {
                return;
            }

            if (_selectedCard >= _session.Deck.Hand.Count || !_session.Player.CanAct)
            {
                _selectedCard = -1;
                return;
            }

            foreach (TectonicSector sector in _session.Board.GetAllSectors())
            {
                if (_session.Player.Preview(_selectedCard, sector) == CardPlayOutcome.Played)
                {
                    _validTargets.Add(sector);
                }
            }
        }

        private void PaintBoard()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            bool selecting = _selectedCard >= 0;

            foreach (TectonicSector sector in _session.Board.GetAllSectors())
            {
                Color color = GetRingColor(sector.Ring);

                if (!sector.IsEmpty)
                {
                    color = Color.Lerp(color, GetContentTint(sector.Content.Definition.Kind), 0.45f);
                }

                if (selecting)
                {
                    color = _validTargets.Contains(sector)
                        ? Color.Lerp(color, TargetTint, 0.35f + 0.25f * pulse)
                        : color * 0.55f;
                }

                if (ReferenceEquals(sector, _hovered))
                {
                    color = Color.Lerp(color, Color.white, 0.18f);
                }

                color.a = 1f;
                _board.SetSectorColor((int)sector.Ring, sector.PlateIndex, color);
            }

            float health = (float)_session.DivineCore.CurrentHealth / _session.DivineCore.MaxHealth;
            Color core = Color.Lerp(new Color(0.55f, 0.08f, 0.06f), GuiTheme.Gold, health);
            core = Color.Lerp(core, Color.white, 0.08f * pulse);
            core.a = 1f;
            _board.SetCoreColor(core);
        }

        // ------------------------------------------------------------------ HUD

        private void OnGUI()
        {
            if (_session == null)
            {
                return;
            }

            if (_theme == null)
            {
                _theme = new GuiTheme();
            }

            _guiScale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_guiScale, _guiScale, 1f));
            float width = Screen.width / _guiScale;
            float height = ReferenceHeight;

            UpdateHover(width, height);

            DrawBoardOverlay();
            DrawTopBar(width);
            DrawLeftPanel(new Rect(16f, TopBarHeight + 12f, SidePanelWidth, height - TopBarHeight - HandAreaHeight - 24f));
            DrawRightPanel(new Rect(width - SidePanelWidth - 16f, TopBarHeight + 12f, SidePanelWidth, height - TopBarHeight - HandAreaHeight - 24f));

            if (_session.Turns.State == TurnState.GameOver)
            {
                DrawGameOver(width, height);
                return;
            }

            DrawHand(width, height);
            DrawHint(width, height);
            DrawSectorTooltip();
            HandleBoardClick(width, height);
        }

        private void DrawTopBar(float width)
        {
            GUI.Box(new Rect(0f, 0f, width, TopBarHeight), GUIContent.none, _theme.Panel);

            TurnSystem turns = _session.Turns;
            string state;

            switch (turns.State)
            {
                case TurnState.AwaitingPlayer: state = $"<color={GuiTheme.Hex(GuiTheme.Good)}>Sua vez</color>"; break;
                case TurnState.GameOver: state = "Fim de jogo"; break;
                default: state = "Resolvendo..."; break;
            }

            string goal = _session.TurnsToSurvive > 0 ? $" / {_session.TurnsToSurvive}" : string.Empty;
            string faith = $"<color={GuiTheme.Hex(GuiTheme.FaithColor)}>Fé {_session.Faith.Current}/{_session.Faith.Capacity}</color>";
            string core = $"Núcleo {_session.DivineCore.CurrentHealth}/{_session.DivineCore.MaxHealth} HP";

            GUI.Label(new Rect(20f, 12f, 260f, 30f), $"<b>SOVEREIGN OF ASHES</b>", _theme.Title);
            GUI.Label(new Rect(300f, 14f, width - 320f, 30f),
                $"<b>Turno {turns.TurnNumber}{goal}</b>     {faith}     {core}     {state}", _theme.Label);
        }

        private void DrawLeftPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none, _theme.Panel);
            float x = area.x + 14f;
            float w = area.width - 28f;
            float y = area.y + 12f;

            GUI.Label(new Rect(x, y, w, 24f), "Civilizações", _theme.Title);
            y += 28f;

            foreach (CivilizationEntity civ in _session.Civilizations)
            {
                string status = civ.IsHeretic
                    ? $"<color={GuiTheme.Hex(GuiTheme.Bad)}><b>HEREGE</b></color>"
                    : $"<color={GuiTheme.Hex(GuiTheme.Good)}>Devota</color>";
                GUI.Label(new Rect(x, y, w, LineHeight), $"<b>{civ.Name}</b>  ·  {status}  ·  Poder {civ.MilitaryPower}", _theme.Label);
                y += LineStep;

                GUI.Label(new Rect(x, y, 96f, LineHeight), $"Fé {civ.Faith}/{civ.MaxFaith}", _theme.Small);
                _theme.DrawBar(new Rect(x + 100f, y + 6f, w - 100f, 9f), (float)civ.Faith / civ.MaxFaith, GuiTheme.FaithColor);
                y += LineStep;

                float reason = (float)civ.Rationality / civ.MaxRationality;
                Color reasonColor = Color.Lerp(GuiTheme.RationalityColor, GuiTheme.Bad, reason);
                GUI.Label(new Rect(x, y, 96f, LineHeight), $"Razão {civ.Rationality}/{civ.MaxRationality}", _theme.Small);
                _theme.DrawBar(new Rect(x + 100f, y + 6f, w - 100f, 9f), reason, reasonColor);
                y += LineStep + 8f;
            }

            y += 6f;
            GUI.Label(new Rect(x, y, w, 24f), "Ao fim deste turno", _theme.Title);
            y += 26f;
            y = DrawForecast(x, y, w);

            y += 10f;
            GUI.Label(new Rect(x, y, w, 24f), "Mandamentos", _theme.Title);
            y += 26f;

            if (_session.Rules.ActiveCommandments.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), "Nenhum ativo.", _theme.Small);
                y += LineStep;
            }
            else
            {
                foreach (ICommandment commandment in _session.Rules.ActiveCommandments)
                {
                    GUI.Label(new Rect(x, y, w, LineHeight), $"• {commandment.DisplayName}", _theme.Label);
                    y += LineStep;
                }
            }

            y += 10f;
            GUI.Label(new Rect(x, y, w, 24f), "Baralho", _theme.Title);
            y += 26f;
            GUI.Label(new Rect(x, y, w, LineHeight),
                $"Compra {_session.Deck.DrawPileCount}  ·  Mão {_session.Deck.Hand.Count}  ·  Descarte {_session.Deck.DiscardPileCount}",
                _theme.Label);
            y += LineStep;
            GUI.Label(new Rect(x, y, w, LineHeight), $"Invasores no tabuleiro: {_session.Invaders.Count}", _theme.Label);
        }

        private float DrawForecast(float x, float y, float w)
        {
            int turn = _session.Turns.TurnNumber;
            IReadOnlyList<RingRotation> shifts = _session.TectonicPlanner.PlanShifts(turn);
            IReadOnlyList<InvaderSpawnOrder> spawns = _session.InvasionPlanner.PlanSpawns(turn);

            if (shifts.Count == 0 && spawns.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), "Nada previsto.", _theme.Small);
                return y + LineStep;
            }

            foreach (RingRotation shift in shifts)
            {
                int delta = shift.Steps * (int)shift.Direction;
                y = DrawWrappedLine(x, y, w, $"Giro: {DisplayNames.Ring(shift.Ring)}, {DisplayNames.Rotation(delta)}");
            }

            foreach (InvaderSpawnOrder spawn in spawns)
            {
                y = DrawWrappedLine(x, y, w,
                    $"Invasão: <color={GuiTheme.Hex(GuiTheme.Bad)}>{spawn.Count}x {spawn.Definition.DisplayName}</color>");
            }

            return y;
        }

        private float DrawWrappedLine(float x, float y, float w, string text)
        {
            var content = new GUIContent(text);
            float height = Mathf.Max(LineHeight, _theme.Label.CalcHeight(content, w));
            GUI.Label(new Rect(x, y, w, height), content, _theme.Label);
            return y + height + (LineStep - LineHeight);
        }

        private void DrawRightPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none, _theme.Panel);
            float x = area.x + 14f;
            float w = area.width - 28f;
            float y = area.y + 12f;

            GUI.Label(new Rect(x, y, w, 24f), "Crônica", _theme.Title);
            y += 28f;

            IReadOnlyList<LogEntry> entries = _session.Log.Entries;
            float bottom = area.yMax - 10f;

            for (int i = entries.Count - 1; i >= 0 && entries.Count - i <= LogLines; i--)
            {
                LogEntry entry = entries[i];
                GUIContent content = new GUIContent($"<color={GuiTheme.Hex(GetToneColor(entry.Tone))}>{entry.Text}</color>");
                float lineHeight = _theme.Label.CalcHeight(content, w);

                if (y + lineHeight > bottom)
                {
                    break;
                }

                GUI.Label(new Rect(x, y, w, lineHeight), content, _theme.Label);
                y += lineHeight + 2f;
            }
        }

        private void DrawHand(float width, float height)
        {
            IReadOnlyList<ICardDefinition> hand = _session.Deck.Hand;
            bool canAct = _session.Player.CanAct && !IsAnimating;
            float handWidth = hand.Count * CardWidth + Mathf.Max(0, hand.Count - 1) * CardSpacing;
            float startX = (width - handWidth) * 0.5f;
            float y = height - CardHeight - 24f;

            for (int i = 0; i < hand.Count; i++)
            {
                ICardDefinition card = hand[i];
                var rect = new Rect(startX + i * (CardWidth + CardSpacing), y, CardWidth, CardHeight);

                if (i == _selectedCard)
                {
                    rect.y -= 18f;
                }

                bool affordable = _session.Faith.CanAfford(card.Cost);
                GUI.enabled = canAct && affordable;
                GUIStyle style = i == _selectedCard ? _theme.CardSelected : _theme.Card;

                if (GUI.Button(rect, BuildCardText(card, affordable), style))
                {
                    OnCardClicked(i, card);
                }

                GUI.enabled = true;
            }

            var endTurn = new Rect(width - 260f, height - 104f, 236f, 72f);
            GUI.enabled = canAct;

            if (GUI.Button(endTurn, "ENCERRAR TURNO", _theme.Button))
            {
                _selectedCard = -1;
                _session.Player.EndTurn();
            }

            GUI.enabled = true;
        }

        private string BuildCardText(ICardDefinition card, bool affordable)
        {
            _text.Clear();
            string costColor = GuiTheme.Hex(affordable ? GuiTheme.FaithColor : GuiTheme.Bad);
            _text.Append("<b><size=15>").Append(card.DisplayName).Append("</size></b>\n");
            _text.Append("<color=").Append(costColor).Append(">").Append(card.Cost).Append(" de Fé</color>  ·  ");
            _text.Append("<color=").Append(GuiTheme.Hex(GuiTheme.TextDim)).Append(">").Append(DisplayNames.Concept(card.PrimaryConcept)).Append("</color>\n\n");
            _text.Append(card.Description).Append("\n\n");

            string target = card.IsGlobal() ? "Uso imediato" : card.RequiresInvader ? "Alvo: invasor" : "Alvo: setor";
            _text.Append("<i><color=").Append(GuiTheme.Hex(GuiTheme.Gold)).Append(">").Append(target).Append("</color></i>");
            return _text.ToString();
        }

        private void OnCardClicked(int handIndex, ICardDefinition card)
        {
            if (card.IsGlobal())
            {
                CardPlayOutcome outcome = _session.Player.PlayFromHand(handIndex, null);
                ShowHint(PlayerActionService.Describe(outcome));
                _selectedCard = -1;
                return;
            }

            _selectedCard = _selectedCard == handIndex ? -1 : handIndex;

            if (_selectedCard >= 0)
            {
                ShowHint(card.RequiresInvader
                    ? "Clique em um setor com invasor. Botão direito cancela."
                    : "Clique em um setor destacado. Botão direito cancela.");
            }
        }

        private void DrawHint(float width, float height)
        {
            string text = Time.time < _hintUntil ? _hint : DefaultHint();

            if (!string.IsNullOrEmpty(text))
            {
                GUI.Label(new Rect(0f, height - CardHeight - 72f, width, 26f), text, _theme.Centered);
            }
        }

        private string DefaultHint()
        {
            if (!_session.Player.CanAct)
            {
                return string.Empty;
            }

            return _selectedCard >= 0
                ? "Escolha o alvo no tabuleiro."
                : "Escolha uma carta. Cartas de uso imediato são jogadas ao clicar.";
        }

        private void ShowHint(string text)
        {
            _hint = text;
            _hintUntil = Time.time + 3f;
        }

        private void DrawGameOver(float width, float height)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), _theme.White);
            GUI.color = Color.white;

            GameOverInfo result = _session.Turns.Result;
            bool victory = result != null && result.Outcome == GameOutcome.Victory;
            string title = victory ? "VITÓRIA" : "O TRONO ESTÁ VAZIO";

            GUI.Label(new Rect(0f, height * 0.32f, width, 70f), title, _theme.BigTitle);
            GUI.Label(new Rect(0f, height * 0.32f + 78f, width, 30f),
                result != null ? $"{result.Reason} (turno {result.TurnNumber})" : string.Empty, _theme.Centered);

            if (GUI.Button(new Rect(width * 0.5f - 140f, height * 0.32f + 140f, 280f, 64f), "JOGAR NOVAMENTE", _theme.Button))
            {
                _bootstrapper.RestartSession();
            }
        }

        // ------------------------------------------------------------------ Tabuleiro: rótulos, dicas e cliques

        private void DrawBoardOverlay()
        {
            foreach (TectonicSector sector in _session.Board.GetAllSectors())
            {
                if (sector.IsEmpty)
                {
                    continue;
                }

                Vector2 gui = WorldToGui(GetPlateCenter(sector));
                string name = sector.Content.Definition.DisplayName;
                float w = Mathf.Max(70f, name.Length * 7.2f);
                GUI.Label(new Rect(gui.x - w * 0.5f, gui.y + 6f, w, 20f), name, _theme.BoardTag);
            }

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (!_invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    continue;
                }

                Vector2 gui = WorldToGui(visual.Position);
                string label = $"{invader.Name}\nHP {invader.CurrentHealth}/{invader.MaxHealth} · ATQ {invader.Attack}";
                GUI.Label(new Rect(gui.x - 62f, gui.y - 30f, 124f, 36f), label, _theme.InvaderTag);
            }

            Vector2 core = WorldToGui(Vector2.zero);
            GUI.Label(new Rect(core.x - 50f, core.y - 12f, 100f, 24f),
                $"<b>{_session.DivineCore.CurrentHealth}</b>", _theme.Centered);
        }

        private void DrawSectorTooltip()
        {
            if (_hovered == null || Event.current == null)
            {
                return;
            }

            _text.Clear();
            _text.Append("<b>").Append(DisplayNames.Ring(_hovered.Ring)).Append("</b>  ·  posição ")
                 .Append(_session.Board.GetPositionOf(_hovered));
            _text.Append('\n').Append(_hovered.IsEmpty ? "Setor vazio" : _hovered.Content.Definition.DisplayName);

            InvaderEntity invader = _session.Invaders.GetInvaderAt(_hovered);

            if (invader != null)
            {
                _text.Append("\n<color=").Append(GuiTheme.Hex(GuiTheme.Bad)).Append(">")
                     .Append(invader.Name).Append(" — ").Append(invader.CurrentHealth).Append('/').Append(invader.MaxHealth)
                     .Append(" HP, ataque ").Append(invader.Attack).Append("</color>");
            }

            Vector2 mouse = Event.current.mousePosition;
            var content = new GUIContent(_text.ToString());
            float height = _theme.Label.CalcHeight(content, 240f) + 20f;
            var rect = new Rect(mouse.x + 18f, mouse.y + 18f, 268f, height);
            GUI.Box(rect, GUIContent.none, _theme.Panel);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, 244f, height - 20f), content, _theme.Label);
        }

        private void UpdateHover(float width, float height)
        {
            Event current = Event.current;

            if (current == null || current.type != EventType.Repaint)
            {
                return;
            }

            _hovered = IsOverBoardArea(current.mousePosition, width, height) && !IsAnimating
                ? PickSector(current.mousePosition)
                : null;
        }

        private void HandleBoardClick(float width, float height)
        {
            Event current = Event.current;

            if (current == null || current.type != EventType.MouseDown)
            {
                return;
            }

            if (current.button == 1)
            {
                _selectedCard = -1;
                current.Use();
                return;
            }

            if (current.button != 0 || _selectedCard < 0 || IsAnimating || !IsOverBoardArea(current.mousePosition, width, height))
            {
                return;
            }

            TectonicSector sector = PickSector(current.mousePosition);

            if (sector == null)
            {
                return;
            }

            CardPlayOutcome outcome = _session.Player.PlayFromHand(_selectedCard, sector);
            ShowHint(PlayerActionService.Describe(outcome));

            if (outcome == CardPlayOutcome.Played)
            {
                _selectedCard = -1;
            }

            current.Use();
        }

        private bool IsOverBoardArea(Vector2 guiPoint, float width, float height)
        {
            return guiPoint.x > SidePanelWidth + 16f
                && guiPoint.x < width - SidePanelWidth - 16f
                && guiPoint.y > TopBarHeight
                && guiPoint.y < height - HandAreaHeight + 30f;
        }

        private TectonicSector PickSector(Vector2 guiPoint)
        {
            Vector3 world = GuiToWorld(guiPoint);
            return _layout.TryPick(world.x, world.y, out int ring, out int position)
                ? _session.Board.GetSectorAt((RingType)ring, position)
                : null;
        }

        // ------------------------------------------------------------------ Conversões e cores

        private Vector2 GetPlateCenter(TectonicSector sector)
        {
            int ring = (int)sector.Ring;
            float angle = _layout.GetAngleDegrees(sector.PlateIndex + _visualOffsets[ring]);
            BoardLayout.PolarToCartesian(_layout.GetCenterRadius(ring), angle, out float x, out float y);
            return new Vector2(x, y);
        }

        private Vector2 WorldToGui(Vector2 world)
        {
            Vector3 screen = _camera.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(screen.x / _guiScale, (Screen.height - screen.y) / _guiScale);
        }

        private Vector3 GuiToWorld(Vector2 gui)
        {
            var screen = new Vector3(gui.x * _guiScale, Screen.height - gui.y * _guiScale, -_camera.transform.position.z);
            return _camera.ScreenToWorldPoint(screen);
        }

        private static Color GetRingColor(RingType ring)
        {
            switch (ring)
            {
                case RingType.InnerSanctuary: return InnerRingColor;
                case RingType.MiddleCivilization: return MiddleRingColor;
                default: return OuterRingColor;
            }
        }

        private static Color GetContentTint(ElementKind kind)
        {
            switch (kind)
            {
                case ElementKind.Entity: return EntityTint;
                case ElementKind.Structure: return StructureTint;
                default: return ConceptTint;
            }
        }

        private static Color GetToneColor(LogTone tone)
        {
            switch (tone)
            {
                case LogTone.Good: return GuiTheme.Good;
                case LogTone.Bad: return GuiTheme.Bad;
                case LogTone.Highlight: return GuiTheme.Gold;
                default: return GuiTheme.Text;
            }
        }
    }
}
