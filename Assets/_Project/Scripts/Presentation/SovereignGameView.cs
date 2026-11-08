using System.Collections.Generic;
using System.Text;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Presentation.Fx;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Interface jogável sem configuração: tabuleiro texturizado (malhas geradas), arte em pixel art,
    /// cartas ilustradas, efeitos (números flutuantes, partículas, tremor, brasas) e HUD em IMGUI.
    /// <para>Uso: adicione este componente a um GameObject vazio e aperte Play.</para>
    /// <para>Toda regra vive na <see cref="GameSession"/>; esta classe só lê estado e chama ações.</para>
    /// </summary>
    [RequireComponent(typeof(SovereignBootstrapper))]
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SovereignGameView : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float SidePanelWidth = 340f;
        private const float TopBarHeight = 64f;
        private const float HandAreaHeight = 290f;
        private const float CardWidth = 178f;
        private const float CardHeight = 240f;
        private const float CardSpacing = 20f;
        private const float LineHeight = 22f;
        private const float LineStep = 24f;
        private const int LogLines = 24;
        private const float BannerDuration = 1.7f;

        private static readonly Color InnerRingTint = new Color(1.00f, 0.86f, 0.62f);
        private static readonly Color MiddleRingTint = new Color(0.62f, 0.86f, 0.86f);
        private static readonly Color OuterRingTint = new Color(0.95f, 0.62f, 0.56f);
        private static readonly Color TargetTint = new Color(1f, 0.85f, 0.4f);

        [Header("Câmera")]
        [SerializeField] private float _cameraSize = 6.4f;
        [SerializeField] private float _cameraOffsetY = -1.25f;

        [Header("Animação")]
        [Tooltip("Velocidade da rotação visual dos anéis, em casas por segundo.")]
        [SerializeField, Min(0.1f)] private float _rotationSpeed = 2.2f;

        [Tooltip("Velocidade da marcha visual dos invasores, em unidades por segundo.")]
        [SerializeField, Min(0.1f)] private float _invaderSpeed = 3.2f;

        private readonly Dictionary<int, InvaderVisual> _invaderVisuals = new Dictionary<int, InvaderVisual>();
        private readonly Dictionary<int, int> _lastInvaderHealth = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _invaderHitTime = new Dictionary<int, float>();
        private readonly HashSet<TectonicSector> _validTargets = new HashSet<TectonicSector>();
        private readonly List<int> _staleIds = new List<int>();
        private readonly StringBuilder _text = new StringBuilder();
        private readonly FloatingTextFx _floatingText = new FloatingTextFx();
        private readonly PuffFx _puffs = new PuffFx();
        private readonly EmberFx _embers = new EmberFx(60);
        private readonly ScreenShake _shake = new ScreenShake();

        private SovereignBootstrapper _bootstrapper;
        private GameSession _session;
        private BoardLayout _layout;
        private BoardRenderer _board;
        private ArtLibrary _art;
        private BoardSkin _skin;
        private GuiTheme _theme;
        private Camera _camera;
        private Vector3 _cameraBase;
        private float[] _visualOffsets;
        private float[] _targetOffsets;
        private int _selectedCard = -1;
        private int _hoveredCard = -1;
        private TectonicSector _hovered;
        private string _hint = string.Empty;
        private float _hintUntil;
        private float _guiScale = 1f;
        private int _lastCoreHealth;
        private int _lastFaith;
        private int _lastTurn;
        private float _bannerStart = -10f;
        private Vector2 _faithGuiPosition = new Vector2(700f, 40f);

        private struct InvaderVisual
        {
            public Vector2 Position;
            public RingType Ring;
        }

        /// <summary>Partida exibida.</summary>
        internal GameSession Session => _session;

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

        /// <summary>Seleciona uma carta da mão (usado pelo modo de captura automática).</summary>
        /// <param name="handIndex">Índice na mão.</param>
        internal void SelectCardForCapture(int handIndex) => _selectedCard = handIndex;

        /// <summary>Limpa a seleção (usado pelo modo de captura automática).</summary>
        internal void ClearSelection() => _selectedCard = -1;

        private void Start()
        {
            _bootstrapper = GetComponent<SovereignBootstrapper>();
            _bootstrapper.SessionCreated += Bind;
            _art = new ArtLibrary();
            _skin = CreateSkin(_art);
            SetupCamera();
            Bind(_bootstrapper.Session);
            SovereignCaptureHarness.AttachIfRequested(gameObject);
        }

        private void OnDestroy()
        {
            if (_bootstrapper != null)
            {
                _bootstrapper.SessionCreated -= Bind;
            }

            Unbind();
            _theme?.Dispose();
            _art?.Dispose();
        }

        // ------------------------------------------------------------------ Ligação com a partida

        private static BoardSkin CreateSkin(ArtLibrary art)
        {
            var rings = new[]
            {
                art.CreateTerrain(32, new Color(0.30f, 0.20f, 0.08f), new Color(0.48f, 0.34f, 0.14f), new Color(0.66f, 0.50f, 0.22f), 11),
                art.CreateTerrain(32, new Color(0.08f, 0.17f, 0.18f), new Color(0.14f, 0.28f, 0.28f), new Color(0.22f, 0.40f, 0.36f), 23),
                art.CreateTerrain(32, new Color(0.20f, 0.07f, 0.07f), new Color(0.33f, 0.12f, 0.10f), new Color(0.46f, 0.20f, 0.14f), 37)
            };

            Texture2D backdrop = art.CreateVignette(128, new Color(0.16f, 0.09f, 0.16f), new Color(0.015f, 0.01f, 0.02f));
            return new BoardSkin(rings, backdrop);
        }

        private void Bind(GameSession session)
        {
            Unbind();
            _session = session;

            if (_session == null)
            {
                return;
            }

            _layout = new BoardLayout(_session.Board.SectorsPerRing, _session.Board.Rings.Count);
            _board = new BoardRenderer(_layout, transform, _skin);

            int ringCount = _session.Board.Rings.Count;
            _visualOffsets = new float[ringCount];
            _targetOffsets = new float[ringCount];

            for (int i = 0; i < ringCount; i++)
            {
                _visualOffsets[i] = _session.Board.Rings[i].RotationOffset;
                _targetOffsets[i] = _visualOffsets[i];
            }

            _invaderVisuals.Clear();
            _lastInvaderHealth.Clear();
            _invaderHitTime.Clear();
            _selectedCard = -1;
            _lastCoreHealth = _session.DivineCore.CurrentHealth;
            _lastFaith = _session.Faith.Current;
            _lastTurn = _session.Turns.TurnNumber;
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
            _cameraBase = new Vector3(0f, _cameraOffsetY, -10f);
            _camera.transform.position = _cameraBase;
            _camera.transform.rotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ Atualização do mundo

        private void Update()
        {
            if (_session == null || _board == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            DetectChanges();
            AnimateRings(deltaTime);
            AnimateInvaders(deltaTime);

            _floatingText.Update(deltaTime);
            _puffs.Update(deltaTime);
            _embers.Update(deltaTime);
            Vector2 shake = _shake.Update(deltaTime, 0.35f);
            _camera.transform.position = new Vector3(_cameraBase.x + shake.x, _cameraBase.y + shake.y, _cameraBase.z);

            RefreshValidTargets();
            PaintBoard();
        }

        private void DetectChanges()
        {
            // Invasores feridos ou destruídos.
            _staleIds.Clear();
            _staleIds.AddRange(_lastInvaderHealth.Keys);

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                _staleIds.Remove(invader.Id);

                if (_lastInvaderHealth.TryGetValue(invader.Id, out int previous) && invader.CurrentHealth < previous
                    && _invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    _floatingText.Spawn(visual.Position, true, $"-{previous - invader.CurrentHealth}", GuiTheme.Bad);
                    _invaderHitTime[invader.Id] = Time.time;
                }

                _lastInvaderHealth[invader.Id] = invader.CurrentHealth;
            }

            foreach (int id in _staleIds)
            {
                if (_invaderVisuals.TryGetValue(id, out InvaderVisual visual))
                {
                    _puffs.Burst(visual.Position, new Color(0.55f, 0.5f, 0.6f), 22);
                    _puffs.Burst(visual.Position, GuiTheme.Gold, 8);
                }

                _lastInvaderHealth.Remove(id);
                _invaderHitTime.Remove(id);
            }

            // Núcleo.
            int core = _session.DivineCore.CurrentHealth;

            if (core < _lastCoreHealth)
            {
                _floatingText.Spawn(Vector2.zero, true, $"-{_lastCoreHealth - core}", GuiTheme.Bad, 1.6f);
                _puffs.Burst(Vector2.zero, GuiTheme.Bad, 16);
                _shake.Add(0.55f);
            }
            else if (core > _lastCoreHealth)
            {
                _floatingText.Spawn(Vector2.zero, true, $"+{core - _lastCoreHealth}", GuiTheme.Good, 1.6f);
                _puffs.Burst(Vector2.zero, GuiTheme.Good, 14);
            }

            _lastCoreHealth = core;

            // Fé.
            int faith = _session.Faith.Current;

            if (faith != _lastFaith)
            {
                int delta = faith - _lastFaith;
                _floatingText.Spawn(_faithGuiPosition + new Vector2(0f, 96f), false,
                    delta > 0 ? $"+{delta} Fé" : $"{delta} Fé", delta > 0 ? GuiTheme.FaithColor : GuiTheme.TextDim);
                _lastFaith = faith;
            }

            // Novo turno.
            if (_session.Turns.TurnNumber != _lastTurn)
            {
                _lastTurn = _session.Turns.TurnNumber;
                _bannerStart = Time.time;
            }
        }

        private void AnimateRings(float deltaTime)
        {
            float step = _rotationSpeed * deltaTime;

            for (int i = 0; i < _visualOffsets.Length; i++)
            {
                _visualOffsets[i] = Mathf.MoveTowards(_visualOffsets[i], _targetOffsets[i], step);
                _board.SetRingRotation(i, _visualOffsets[i]);
            }
        }

        private void AnimateInvaders(float deltaTime)
        {
            float step = _invaderSpeed * deltaTime;
            _staleIds.Clear();
            _staleIds.AddRange(_invaderVisuals.Keys);

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (invader.Sector == null)
                {
                    continue;
                }

                Vector2 target = GetPlateCenter(invader.Sector);
                _staleIds.Remove(invader.Id);

                if (!_invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    // Recém-chegado: entra vindo de fora do tabuleiro.
                    visual.Position = target * 1.3f;
                    visual.Ring = invader.Sector.Ring;
                }

                if (visual.Ring == invader.Sector.Ring && Vector2.Distance(visual.Position, target) < 0.75f)
                {
                    visual.Position = target; // mesmo anel: acompanha a rotação da placa
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

            foreach (int id in _staleIds)
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
                Color color = GetRingTint(sector.Ring);

                if (selecting)
                {
                    color = _validTargets.Contains(sector)
                        ? Color.Lerp(color, TargetTint, 0.45f + 0.35f * pulse) * 1.15f
                        : color * 0.45f;
                }

                if (ReferenceEquals(sector, _hovered))
                {
                    color = Color.Lerp(color, Color.white, 0.25f);
                }

                color.a = 1f;
                _board.SetSectorColor((int)sector.Ring, sector.PlateIndex, color);
            }

            float health = (float)_session.DivineCore.CurrentHealth / _session.DivineCore.MaxHealth;
            Color core = Color.Lerp(new Color(0.5f, 0.06f, 0.05f), new Color(0.95f, 0.75f, 0.3f), health);
            core = Color.Lerp(core, Color.white, 0.12f * pulse);
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

            _embers.Draw(width, height, _theme.White);
            DrawBoardOverlay();
            _puffs.Draw(WorldToGui, _theme.White);

            DrawTopBar(width);
            float panelHeight = height - TopBarHeight - HandAreaHeight - 20f;
            DrawLeftPanel(new Rect(14f, TopBarHeight + 12f, SidePanelWidth, panelHeight));
            DrawRightPanel(new Rect(width - SidePanelWidth - 14f, TopBarHeight + 12f, SidePanelWidth, panelHeight));
            DrawTurnBanner(width);
            _floatingText.Draw(WorldToGui, _theme.FloatText);

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
            _theme.Frame(new Rect(0f, 0f, width, TopBarHeight), GuiTheme.Bronze, new Color(0.06f, 0.05f, 0.07f, 0.97f), 3f);
            _theme.Image(new Rect(18f, 12f, 40f, 40f), _art.Sprite("core"), Color.white);
            _theme.ShadowText(new Rect(66f, 12f, 280f, 40f), "SOVEREIGN OF ASHES", _theme.Number, GuiTheme.Gold);

            TurnSystem turns = _session.Turns;
            float x = width * 0.5f - 330f;

            string goal = _session.TurnsToSurvive > 0 ? $" / {_session.TurnsToSurvive}" : string.Empty;
            _theme.ShadowText(new Rect(x, 12f, 200f, 40f), $"TURNO {turns.TurnNumber}{goal}", _theme.Number, Color.white);
            x += 200f;

            _theme.Image(new Rect(x, 14f, 36f, 36f), _art.Sprite("gem"), Color.white);
            _theme.ShadowText(new Rect(x + 42f, 12f, 120f, 40f), $"{_session.Faith.Current}/{_session.Faith.Capacity}", _theme.Number, GuiTheme.FaithColor);
            _faithGuiPosition = new Vector2(x + 70f, 30f);
            x += 160f;

            _theme.Image(new Rect(x, 14f, 36f, 36f), _art.Sprite("heart"), Color.white);
            _theme.ShadowText(new Rect(x + 42f, 12f, 120f, 40f), $"{_session.DivineCore.CurrentHealth}/{_session.DivineCore.MaxHealth}", _theme.Number, GuiTheme.Bad);
            x += 160f;

            string state;
            Color stateColor;

            switch (turns.State)
            {
                case TurnState.AwaitingPlayer: state = "SUA VEZ"; stateColor = GuiTheme.Good; break;
                case TurnState.GameOver: state = "FIM DE JOGO"; stateColor = GuiTheme.Gold; break;
                default: state = "RESOLVENDO..."; stateColor = GuiTheme.TextDim; break;
            }

            _theme.ShadowText(new Rect(x, 12f, 220f, 40f), state, _theme.Number, stateColor);
        }

        private void DrawLeftPanel(Rect area)
        {
            _theme.Panel(area);
            float x = area.x + 16f;
            float w = area.width - 32f;
            float y = area.y + 14f;

            GUI.Label(new Rect(x, y, w, 26f), "Civilizações", _theme.Title);
            y += 32f;

            foreach (CivilizationEntity civ in _session.Civilizations)
            {
                var portrait = new Rect(x, y, 52f, 52f);
                _theme.Frame(portrait, civ.IsHeretic ? GuiTheme.Bad : GuiTheme.Bronze, new Color(0.05f, 0.04f, 0.06f), 2f);
                _theme.Image(new Rect(portrait.x + 6f, portrait.y + 6f, 40f, 40f), _art.Sprite(civ.IsHeretic ? "skull" : "saint"), Color.white);

                float tx = x + 62f;
                float tw = w - 62f;
                string status = civ.IsHeretic
                    ? $"<color={GuiTheme.Hex(GuiTheme.Bad)}><b>HEREGE</b></color>"
                    : $"<color={GuiTheme.Hex(GuiTheme.Good)}>Devota</color>";
                GUI.Label(new Rect(tx, y, tw, LineHeight), $"<b>{civ.Name}</b>", _theme.Label);
                GUI.Label(new Rect(tx, y + 22f, tw, LineHeight), $"{status}  ·  Poder <b>{civ.MilitaryPower}</b>", _theme.Label);
                y += 60f;

                GUI.Label(new Rect(x, y, 96f, LineHeight), $"Fé {civ.Faith}/{civ.MaxFaith}", _theme.Small);
                _theme.DrawBar(new Rect(x + 100f, y + 5f, w - 100f, 11f), (float)civ.Faith / civ.MaxFaith, GuiTheme.FaithColor);
                y += LineStep;

                float reason = (float)civ.Rationality / civ.MaxRationality;
                Color reasonColor = Color.Lerp(GuiTheme.RationalityColor, GuiTheme.Bad, reason);
                GUI.Label(new Rect(x, y, 96f, LineHeight), $"Razão {civ.Rationality}/{civ.MaxRationality}", _theme.Small);
                _theme.DrawBar(new Rect(x + 100f, y + 5f, w - 100f, 11f), reason, reasonColor);
                y += LineStep + 12f;
            }

            GUI.Label(new Rect(x, y, w, 26f), "Ao fim deste turno", _theme.Title);
            y += 30f;
            y = DrawForecast(x, y, w);

            y += 10f;
            GUI.Label(new Rect(x, y, w, 26f), "Mandamentos", _theme.Title);
            y += 30f;

            if (_session.Rules.ActiveCommandments.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), "Nenhum ativo.", _theme.Small);
                y += LineStep;
            }
            else
            {
                foreach (ICommandment commandment in _session.Rules.ActiveCommandments)
                {
                    y = DrawWrappedLine(x, y, w, $"• {commandment.DisplayName}");
                }
            }

            y += 10f;
            GUI.Label(new Rect(x, y, w, 26f), "Baralho", _theme.Title);
            y += 30f;
            _theme.Image(new Rect(x, y - 2f, 26f, 26f), _art.Sprite("book"), Color.white);
            GUI.Label(new Rect(x + 32f, y, w - 32f, LineHeight),
                $"Compra {_session.Deck.DrawPileCount}  ·  Mão {_session.Deck.Hand.Count}  ·  Descarte {_session.Deck.DiscardPileCount}",
                _theme.Label);
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
                _theme.Image(new Rect(x, y - 3f, 28f, 28f), _art.Invader(spawn.Definition), Color.white);
                y = DrawWrappedLine(x + 34f, y + 2f, w - 34f,
                    $"<color={GuiTheme.Hex(GuiTheme.Bad)}>{spawn.Count}x {spawn.Definition.DisplayName}</color> invade");
                y += 4f;
            }

            return y;
        }

        private float DrawWrappedLine(float x, float y, float w, string text)
        {
            var content = new GUIContent(text);
            float lineHeight = Mathf.Max(LineHeight, _theme.Label.CalcHeight(content, w));
            GUI.Label(new Rect(x, y, w, lineHeight), content, _theme.Label);
            return y + lineHeight + (LineStep - LineHeight);
        }

        private void DrawRightPanel(Rect area)
        {
            _theme.Panel(area);
            float x = area.x + 16f;
            float w = area.width - 32f;
            float y = area.y + 14f;

            GUI.Label(new Rect(x, y, w, 26f), "Crônica", _theme.Title);
            y += 32f;

            IReadOnlyList<LogEntry> entries = _session.Log.Entries;
            float bottom = area.yMax - 12f;

            for (int i = entries.Count - 1; i >= 0 && entries.Count - i <= LogLines; i--)
            {
                LogEntry entry = entries[i];
                var content = new GUIContent($"<color={GuiTheme.Hex(GetToneColor(entry.Tone))}>{entry.Text}</color>");
                float lineHeight = _theme.Label.CalcHeight(content, w);

                if (y + lineHeight > bottom)
                {
                    break;
                }

                GUI.Label(new Rect(x, y, w, lineHeight), content, _theme.Label);
                y += lineHeight + 3f;
            }
        }

        private void DrawTurnBanner(float width)
        {
            float t = (Time.time - _bannerStart) / BannerDuration;

            if (t < 0f || t > 1f || _session.Turns.State == TurnState.GameOver)
            {
                return;
            }

            float alpha = t < 0.15f ? t / 0.15f : t > 0.75f ? 1f - (t - 0.75f) / 0.25f : 1f;
            float y = 330f;
            float x = SidePanelWidth + 28f;
            float w = width - (SidePanelWidth + 28f) * 2f;
            var gold = new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, alpha);
            _theme.Fill(new Rect(x, y, w, 110f), new Color(0f, 0f, 0f, 0.6f * alpha));
            _theme.Fill(new Rect(x, y, w, 3f), gold);
            _theme.Fill(new Rect(x, y + 107f, w, 3f), gold);
            _theme.ShadowText(new Rect(x, y, w, 110f), $"TURNO {_session.Turns.TurnNumber}", _theme.Banner, gold);
        }

        private void DrawHand(float width, float height)
        {
            IReadOnlyList<ICardDefinition> hand = _session.Deck.Hand;
            bool canAct = _session.Player.CanAct && !IsAnimating;
            float handWidth = hand.Count * CardWidth + Mathf.Max(0, hand.Count - 1) * CardSpacing;
            float startX = (width - handWidth) * 0.5f;
            float baseY = height - CardHeight - 16f;
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);

            _hoveredCard = -1;

            for (int i = 0; i < hand.Count; i++)
            {
                var slot = new Rect(startX + i * (CardWidth + CardSpacing), baseY - 24f, CardWidth, CardHeight + 24f);

                if (slot.Contains(mouse))
                {
                    _hoveredCard = i;
                }
            }

            for (int i = 0; i < hand.Count; i++)
            {
                ICardDefinition card = hand[i];
                bool affordable = _session.Faith.CanAfford(card.Cost);
                bool usable = canAct && affordable;
                bool selected = i == _selectedCard;
                bool hovered = i == _hoveredCard;
                float lift = selected ? 26f : hovered && usable ? 16f : 0f;
                var rect = new Rect(startX + i * (CardWidth + CardSpacing), baseY - lift, CardWidth, CardHeight);

                CardPainter.Draw(rect, card, _art.Card(card), _art.Sprite("gem"), _theme, new CardVisualState
                {
                    Selected = selected,
                    Hovered = hovered,
                    Usable = usable,
                    Affordable = affordable,
                    Time = Time.time
                });

                GUI.enabled = usable;

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    OnCardClicked(i, card);
                }

                GUI.enabled = true;
            }

            var endTurn = new Rect(width - 262f, height - 110f, 240f, 80f);
            bool endHover = endTurn.Contains(mouse) && canAct;
            Color endBorder = canAct ? (endHover ? GuiTheme.Gold : GuiTheme.Bronze) : new Color(0.25f, 0.22f, 0.2f);
            Color endFill = canAct ? (endHover ? new Color(0.55f, 0.16f, 0.10f) : new Color(0.40f, 0.11f, 0.08f)) : new Color(0.15f, 0.13f, 0.13f);
            _theme.Frame(endTurn, endBorder, endFill, 4f);
            _theme.ShadowText(endTurn, "ENCERRAR\nTURNO", _theme.ButtonText, canAct ? Color.white : GuiTheme.TextDim);

            GUI.enabled = canAct;

            if (GUI.Button(endTurn, GUIContent.none, GUIStyle.none))
            {
                _selectedCard = -1;
                _session.Player.EndTurn();
            }

            GUI.enabled = true;
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
                _theme.ShadowText(new Rect(0f, height - CardHeight - 74f, width, 26f), text, _theme.Centered, GuiTheme.Text);
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
            _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.78f));

            GameOverInfo result = _session.Turns.Result;
            bool victory = result != null && result.Outcome == GameOutcome.Victory;
            float size = 160f + 8f * Mathf.Sin(Time.time * 2f);
            _theme.Image(new Rect(width * 0.5f - size * 0.5f, height * 0.22f - size * 0.5f + 40f, size, size),
                _art.Sprite(victory ? "core" : "skull"), Color.white);

            _theme.ShadowText(new Rect(0f, height * 0.40f, width, 90f), victory ? "VITÓRIA" : "O TRONO ESTÁ VAZIO",
                _theme.Banner, victory ? GuiTheme.Gold : GuiTheme.Bad);
            _theme.ShadowText(new Rect(0f, height * 0.40f + 92f, width, 30f),
                result != null ? $"{result.Reason} (turno {result.TurnNumber})" : string.Empty, _theme.Centered, GuiTheme.Text);

            var button = new Rect(width * 0.5f - 150f, height * 0.40f + 150f, 300f, 72f);
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            bool hover = button.Contains(mouse);
            _theme.Frame(button, hover ? GuiTheme.Gold : GuiTheme.Bronze, hover ? new Color(0.55f, 0.16f, 0.10f) : new Color(0.40f, 0.11f, 0.08f), 4f);
            _theme.ShadowText(button, "JOGAR NOVAMENTE", _theme.ButtonText, Color.white);

            if (GUI.Button(button, GUIContent.none, GUIStyle.none))
            {
                _bootstrapper.RestartSession();
            }
        }

        // ------------------------------------------------------------------ Tabuleiro: arte, dicas e cliques

        private void DrawBoardOverlay()
        {
            // Núcleo Divino.
            Vector2 coreGui = WorldToGui(Vector2.zero);
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 2.4f);
            float coreSize = 112f * pulse;
            _theme.Image(new Rect(coreGui.x - coreSize * 0.5f, coreGui.y - coreSize * 0.5f - 6f, coreSize, coreSize), _art.Sprite("core"), Color.white);
            _theme.ShadowText(new Rect(coreGui.x - 60f, coreGui.y + 46f, 120f, 30f), _session.DivineCore.CurrentHealth.ToString(),
                _theme.FloatText, Color.white);

            // Elementos da alquimia.
            foreach (TectonicSector sector in _session.Board.GetAllSectors())
            {
                if (sector.IsEmpty)
                {
                    continue;
                }

                Vector2 gui = WorldToGui(GetPlateCenter(sector));
                _theme.Image(new Rect(gui.x - 22f, gui.y - 22f, 44f, 44f), _art.Element(sector.Content.Definition), Color.white);
            }

            // Invasores.
            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (!_invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    continue;
                }

                Vector2 gui = WorldToGui(visual.Position);
                float bob = Mathf.Sin(Time.time * 3f + invader.Id) * 2f;
                bool hit = _invaderHitTime.TryGetValue(invader.Id, out float hitAt) && Time.time - hitAt < 0.25f;
                var spriteRect = new Rect(gui.x - 28f, gui.y - 38f + bob, 56f, 56f);

                _theme.Fill(new Rect(gui.x - 20f, gui.y + 14f, 40f, 5f), new Color(0f, 0f, 0f, 0.45f)); // sombra
                _theme.Image(spriteRect, _art.Invader(invader.Definition), hit ? new Color(1f, 0.45f, 0.45f) : Color.white);

                var bar = new Rect(gui.x - 24f, gui.y + 22f, 48f, 7f);
                _theme.DrawBar(bar, (float)invader.CurrentHealth / invader.MaxHealth, GuiTheme.Bad);
            }
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
                     .Append(invader.Name).Append(": ").Append(invader.CurrentHealth).Append('/').Append(invader.MaxHealth)
                     .Append(" HP, ataque ").Append(invader.Attack).Append("</color>");
            }

            Vector2 mouse = Event.current.mousePosition;
            var content = new GUIContent(_text.ToString());
            float contentHeight = _theme.Label.CalcHeight(content, 244f) + 22f;
            var rect = new Rect(mouse.x + 20f, mouse.y + 20f, 272f, contentHeight);
            _theme.Panel(rect);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 11f, 244f, contentHeight - 22f), content, _theme.Label);
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
            return guiPoint.x > SidePanelWidth + 14f
                && guiPoint.x < width - SidePanelWidth - 14f
                && guiPoint.y > TopBarHeight
                && guiPoint.y < height - HandAreaHeight + 20f;
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

        private static Color GetRingTint(RingType ring)
        {
            switch (ring)
            {
                case RingType.InnerSanctuary: return InnerRingTint;
                case RingType.MiddleCivilization: return MiddleRingTint;
                default: return OuterRingTint;
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
