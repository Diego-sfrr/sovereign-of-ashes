using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "HUD detalhado" da view: previsão da marcha, números das posições, painel de invasores com retratos,
    /// civilização, pilhas de cartas, carta ampliada, trilha de turnos e telas de fim de jogo pintadas.
    /// Só lê o estado da partida e desenha: nenhuma regra fica aqui.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const float InvaderRowHeight = 56f;

        private readonly Dictionary<int, MarchPrediction> _predictions = new Dictionary<int, MarchPrediction>();
        private readonly List<InvaderEntity> _sortedInvaders = new List<InvaderEntity>();
        private MarchForecaster _forecaster;
        private bool? _gameOverPreview;
        private int _forcedHoverCard = -1;

        /// <summary>Mostra a tela de fim de jogo mesmo sem a partida ter acabado (captura de telas).</summary>
        /// <param name="victory"><c>true</c> vitória, <c>false</c> derrota, <c>null</c> desliga a prévia.</param>
        internal void PreviewGameOver(bool? victory) => _gameOverPreview = victory;

        /// <summary>Força a carta ampliada de uma carta da mão (captura de telas). -1 desliga.</summary>
        /// <param name="handIndex">Índice na mão.</param>
        internal void ForceHoverCard(int handIndex) => _forcedHoverCard = handIndex;

        private void CreateForecaster()
        {
            _forecaster = new MarchForecaster(_session.Board, _session.Invaders, _session.ElementEffects.WouldBlock);
        }

        // ------------------------------------------------------------------ Mundo: fundo, enfeites e previsão

        private void UpdateWorldDecor()
        {
            // O fundo pintado cobre a área visível (com folga para o tremor de tela), sem distorcer.
            float viewHeight = _camera.orthographicSize * 2f * 1.1f;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            float textureAspect = _skin.Backdrop.width / (float)Mathf.Max(1, _skin.Backdrop.height);
            Vector2 size = textureAspect > aspect
                ? new Vector2(viewHeight * textureAspect, viewHeight)
                : new Vector2(viewHeight * aspect, viewHeight * aspect / textureAspect);
            _board.SetBackdropRect(new Vector2(_cameraBase.x, _cameraBase.y), size);
            _board.AnimateDecor(Time.time, (float)_session.DivineCore.CurrentHealth / _session.DivineCore.MaxHealth);

            _predictions.Clear();
            IReadOnlyList<RingRotation> plan = _session.TectonicPlanner.PlanShifts(_session.Turns.TurnNumber);

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (invader.Sector != null && !invader.IsDead)
                {
                    _predictions[invader.Id] = _forecaster.Predict(invader, plan);
                }
            }
        }

        /// <summary>
        /// Números das posições angulares junto à borda externa. As posições são fixas no mundo (os anéis giram por baixo),
        /// e é por elas que a marcha alinha os anéis: o invasor da posição 3 do anel externo entra na posição 3 do anel seguinte.
        /// </summary>
        private void DrawPositionNumerals()
        {
            float radius = _layout.GetOuterRadius(_layout.RingCount - 1) - 0.17f;

            for (int position = 0; position < _layout.SectorsPerRing; position++)
            {
                BoardLayout.PolarToCartesian(radius, _layout.GetAngleDegrees(position), out float x, out float y);
                Vector2 gui = WorldToGui(new Vector2(x, y));
                var rect = new Rect(gui.x - 10f, gui.y - 9f, 20f, 18f);
                _theme.Fill(rect, new Color(0f, 0f, 0f, 0.4f));
                _theme.ShadowText(rect, position.ToString(), _theme.SmallNumber, new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, 0.85f));
            }
        }

        /// <summary>
        /// Mostra a "intenção" de cada invasor na vez do jogador: pontos até o setor para onde ele vai marchar
        /// (já contando as rotações previstas), seta de avanço, escudo se estiver bloqueado ou espada se atacará o Núcleo.
        /// </summary>
        private void DrawIntentOverlay()
        {
            if (!_session.Player.CanAct || IsAnimating)
            {
                return;
            }

            float time = Time.time;

            foreach (InvaderEntity invader in _session.Invaders.Invaders)
            {
                if (!_predictions.TryGetValue(invader.Id, out MarchPrediction prediction)
                    || !_invaderVisuals.TryGetValue(invader.Id, out InvaderVisual visual))
                {
                    continue;
                }

                Vector2 fromGui = WorldToGui(visual.Position);

                if (prediction.Intent == MarchIntent.AttackCore)
                {
                    float bob = Mathf.Sin(time * 5f + invader.Id) * 3f;
                    _theme.Image(new Rect(fromGui.x + 8f, fromGui.y - 60f + bob, 26f, 26f), _art.Sprite("sword"), new Color(1f, 0.5f, 0.45f));
                    continue;
                }

                BoardLayout.PolarToCartesian(_layout.GetCenterRadius((int)prediction.TargetRing),
                    _layout.GetAngleDegrees(prediction.Position), out float targetX, out float targetY);
                Vector2 toGui = WorldToGui(new Vector2(targetX, targetY));
                bool blocked = prediction.Intent == MarchIntent.Blocked;
                Color color = blocked ? new Color(0.95f, 0.6f, 0.25f) : new Color(1f, 0.85f, 0.35f);

                // Pontos que "andam" até o destino.
                const int dots = 5;
                float march = (time * 0.9f) % 1f;

                for (int i = 0; i < dots; i++)
                {
                    float t = 0.2f + 0.7f * ((i + march) / dots);
                    Vector2 point = fromGui + (toGui - fromGui) * t;
                    _theme.Fill(new Rect(point.x - 2f, point.y - 2f, 4f, 4f), new Color(color.r, color.g, color.b, 0.25f + 0.5f * t));
                }

                float size = 32f * (0.85f + 0.15f * Mathf.Sin(time * 5f + invader.Id));
                var marker = new Rect(toGui.x - size * 0.5f, toGui.y - size * 0.5f, size, size);

                if (blocked)
                {
                    _theme.Image(marker, _art.Sprite("shield"), new Color(1f, 0.75f, 0.6f));
                }
                else
                {
                    Vector2 direction = toGui - fromGui;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // o sprite aponta para baixo
                    Matrix4x4 previous = GUI.matrix;
                    GuiTheme.RotateAroundGuiPivot(angle, toGui);
                    _theme.Image(marker, _art.Sprite("chevron"), new Color(1f, 1f, 1f, 0.95f));
                    GUI.matrix = previous;
                }
            }
        }

        // ------------------------------------------------------------------ Painéis laterais

        private float DrawSectionTitle(float x, float y, float w, string title)
        {
            GUI.Label(new Rect(x, y, w, 26f), title, _theme.Title);
            y += 28f;
            _theme.Divider(x, y, w);
            return y + 14f;
        }

        private void DrawPortrait(Rect rect, Texture2D painting, Texture2D fallbackSprite, Color border, Color? tint = null)
        {
            _theme.Frame(rect, border, new Color(0.05f, 0.04f, 0.06f), 2f);
            var inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);

            if (painting != null)
            {
                CardPainter.DrawCover(inner, painting);
            }
            else if (fallbackSprite != null)
            {
                _theme.Image(new Rect(inner.x + inner.width * 0.1f, inner.y + inner.height * 0.1f, inner.width * 0.8f, inner.height * 0.8f),
                    fallbackSprite, tint ?? Color.white);
            }
        }

        private void DrawLeftPanel(Rect area)
        {
            _theme.Panel(area);
            float x = area.x + 18f;
            float w = area.width - 36f;
            float y = area.y + 14f;

            y = DrawSectionTitle(x, y, w, "Civilizações");

            foreach (CivilizationEntity civ in _session.Civilizations)
            {
                y = DrawCivilization(civ, x, y, w);
            }

            y = DrawSectionTitle(x, y, w, "Ao fim deste turno");
            y = DrawForecast(x, y, w) + 6f;

            int commandments = _session.Rules.ActiveCommandments.Count;
            float commandmentsReserve = 28f + 14f + (commandments == 0 ? LineStep : commandments * LineStep) + 10f;

            y = DrawSectionTitle(x, y, w, "Invasores");
            y = DrawInvaderList(x, y, w, area.yMax - commandmentsReserve - 10f);

            y = DrawSectionTitle(x, y + 4f, w, "Mandamentos");

            if (commandments == 0)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), "Nenhum ativo.", _theme.Small);
                return;
            }

            foreach (ICommandment commandment in _session.Rules.ActiveCommandments)
            {
                y = DrawWrappedLine(x, y, w, $"• {commandment.DisplayName}");
            }
        }

        private float DrawCivilization(CivilizationEntity civ, float x, float y, float w)
        {
            DrawPortrait(new Rect(x, y, 64f, 64f), _art.Painting(ArtCatalog.CivilizationPortraitPath(civ.IsHeretic)),
                _art.Sprite(civ.IsHeretic ? "skull" : "saint"), civ.IsHeretic ? GuiTheme.Bad : GuiTheme.Bronze);

            float tx = x + 74f;
            float tw = w - 74f;
            string status = civ.IsHeretic
                ? $"<color={GuiTheme.Hex(GuiTheme.Bad)}><b>HEREGE</b></color>"
                : $"<color={GuiTheme.Hex(GuiTheme.Good)}>Devota</color>";
            GUI.Label(new Rect(tx, y - 2f, tw, LineHeight), $"<b>{civ.Name}</b>", _theme.Label);
            GUI.Label(new Rect(tx, y + 20f, tw, LineHeight), status, _theme.Label);
            _theme.Image(new Rect(tx, y + 42f, 20f, 20f), _art.Sprite("sword"), Color.white);
            GUI.Label(new Rect(tx + 24f, y + 42f, tw - 24f, LineHeight), $"Poder <b>{civ.MilitaryPower}</b>", _theme.Label);
            y += 74f;

            // Fé.
            _theme.Image(new Rect(x, y - 1f, 20f, 20f), _art.Sprite("crystal"), Color.white);
            GUI.Label(new Rect(x + 24f, y, 90f, LineHeight), $"Fé {civ.Faith}/{civ.MaxFaith}", _theme.Small);
            _theme.DrawBar(new Rect(x + 118f, y + 5f, w - 118f, 11f), (float)civ.Faith / civ.MaxFaith, GuiTheme.FaithColor);
            y += LineStep;

            // Razão, com a marca da redenção e a heresia no fim da barra.
            float reason = (float)civ.Rationality / civ.MaxRationality;
            _theme.Image(new Rect(x, y - 1f, 20f, 20f), _art.Sprite("fear"), Color.white);
            GUI.Label(new Rect(x + 24f, y, 90f, LineHeight), $"Razão {civ.Rationality}/{civ.MaxRationality}", _theme.Small);
            var bar = new Rect(x + 118f, y + 5f, w - 118f, 11f);
            _theme.DrawBar(bar, reason, Color.Lerp(GuiTheme.RationalityColor, GuiTheme.Bad, reason));
            float redemptionX = bar.x + bar.width * civ.RedemptionThreshold / civ.MaxRationality;
            _theme.Fill(new Rect(redemptionX - 1f, bar.y - 3f, 2f, bar.height + 6f), new Color(1f, 1f, 1f, 0.55f));
            y += LineStep;

            GUI.Label(new Rect(x, y - 2f, w, 18f),
                civ.IsHeretic ? $"Volta à fé abaixo de {civ.RedemptionThreshold + 1} de Razão." : $"Vira herege ao chegar a {civ.MaxRationality} de Razão.",
                _theme.Small);
            return y + 22f;
        }

        private float DrawInvaderList(float x, float y, float w, float yLimit)
        {
            _sortedInvaders.Clear();
            _sortedInvaders.AddRange(_session.Invaders.Invaders);
            _sortedInvaders.Sort((a, b) =>
            {
                int byRing = ((int)a.Sector.Ring).CompareTo((int)b.Sector.Ring);
                return byRing != 0 ? byRing : a.Id.CompareTo(b.Id);
            });

            if (_sortedInvaders.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), "Nenhum por enquanto.", _theme.Small);
                return y + LineStep;
            }

            int fit = Mathf.Max(1, (int)((yLimit - y) / InvaderRowHeight));
            int rows = _sortedInvaders.Count <= fit ? _sortedInvaders.Count : Mathf.Max(1, fit - 1);

            for (int i = 0; i < rows; i++)
            {
                y = DrawInvaderRow(_sortedInvaders[i], x, y, w);
            }

            if (rows < _sortedInvaders.Count)
            {
                GUI.Label(new Rect(x, y, w, LineHeight), $"+{_sortedInvaders.Count - rows} invasores mais distantes", _theme.Small);
                y += LineStep;
            }

            return y;
        }

        private float DrawInvaderRow(InvaderEntity invader, float x, float y, float w)
        {
            DrawPortrait(new Rect(x, y, 48f, 48f), _art.Painting(ArtCatalog.InvaderPortraitPath(invader.Definition.Id)),
                _art.Invader(invader.Definition), GuiTheme.Bad, ArtCatalog.InvaderTint(invader.Definition.Id));

            float tx = x + 58f;
            float tw = w - 58f;
            GUI.Label(new Rect(tx, y - 3f, tw, LineHeight),
                $"<b>{invader.Name}</b> <color={GuiTheme.Hex(GuiTheme.TextDim)}>· {ShortRingName(invader.Sector.Ring)}</color>", _theme.Label);

            float barWidth = tw * 0.6f;
            _theme.DrawBar(new Rect(tx, y + 21f, barWidth, 8f), (float)invader.CurrentHealth / invader.MaxHealth, GuiTheme.Bad);
            GUI.Label(new Rect(tx + barWidth + 6f, y + 15f, tw - barWidth - 6f, 18f), $"{invader.CurrentHealth}/{invader.MaxHealth}", _theme.Small);

            string intent = DescribeIntent(invader);
            string traits = invader.Definition.DescribeTraits();

            if (traits.Length > 0)
            {
                intent += (intent.Length > 0 ? " " : string.Empty) + $"<color={GuiTheme.Hex(GuiTheme.Gold)}>· {traits}</color>";
            }

            if (intent.Length > 0)
            {
                GUI.Label(new Rect(tx, y + 31f, tw, 18f), intent, _theme.Small);
            }

            return y + InvaderRowHeight;
        }

        private string DescribeIntent(InvaderEntity invader)
        {
            if (!_predictions.TryGetValue(invader.Id, out MarchPrediction p))
            {
                return string.Empty;
            }

            switch (p.Intent)
            {
                case MarchIntent.AttackCore:
                    return $"<color={GuiTheme.Hex(GuiTheme.Bad)}>Ataca o Núcleo ({invader.Attack})</color>";
                case MarchIntent.Blocked:
                    return "<color=#F29B40>Bloqueado: espera</color>";
                default:
                    return $"<color={GuiTheme.Hex(GuiTheme.Gold)}>Avança para {ShortRingName(p.TargetRing)}</color>";
            }
        }

        private static string ShortRingName(RingType ring)
        {
            switch (ring)
            {
                case RingType.InnerSanctuary: return "Santuário";
                case RingType.MiddleCivilization: return "Civilização";
                default: return "Ermos";
            }
        }

        private void DrawRightPanel(Rect area)
        {
            _theme.Panel(area);
            float x = area.x + 26f;
            float w = area.width - 44f;
            float y = area.y + 14f;

            y = DrawSectionTitle(area.x + 18f, y, area.width - 36f, "Crônica");

            IReadOnlyList<LogEntry> entries = _session.Log.Entries;
            float bottom = area.yMax - 12f;

            for (int i = entries.Count - 1; i >= 0 && entries.Count - i <= LogLines; i--)
            {
                LogEntry entry = entries[i];
                bool isTurnMark = entry.Tone == LogTone.Highlight && entry.Text.StartsWith("—");

                if (isTurnMark)
                {
                    if (y + 28f > bottom)
                    {
                        break;
                    }

                    _theme.Divider(area.x + 18f, y + 2f, area.width - 36f);
                    _theme.ShadowText(new Rect(area.x, y + 8f, area.width, 20f), entry.Text.Trim('—', ' '), _theme.SmallNumber, GuiTheme.Gold);
                    y += 30f;
                    continue;
                }

                Color tone = GetToneColor(entry.Tone);
                var content = new GUIContent($"<color={GuiTheme.Hex(tone)}>{entry.Text}</color>");
                float lineHeight = _theme.Label.CalcHeight(content, w);

                if (y + lineHeight > bottom)
                {
                    break;
                }

                _theme.Fill(new Rect(area.x + 16f, y + 2f, 3f, Mathf.Max(8f, lineHeight - 4f)), new Color(tone.r, tone.g, tone.b, 0.8f));
                GUI.Label(new Rect(x, y, w, lineHeight), content, _theme.Label);
                y += lineHeight + 4f;
            }
        }

        // ------------------------------------------------------------------ Barra do topo, pilhas e carta ampliada

        // ------------------------------------------------------------------ Barra do topo: Fé em cristais, vida em corações

        private const float ResourceAnimSeconds = 0.7f;
        private const float ClusterWidth = 290f;

        private int _faithFrom;
        private int _faithTo;
        private float _faithChangeTime = -10f;
        private int _hpFrom;
        private int _hpTo;
        private float _hpChangeTime = -10f;
        private float _hpTrail;

        private void ResetResourceAnimation()
        {
            _faithFrom = _faithTo = _session.Faith.Current;
            _faithChangeTime = -10f;
            _hpFrom = _hpTo = _session.DivineCore.CurrentHealth;
            _hpChangeTime = -10f;
            _hpTrail = _hpTo;
        }

        private void NoteFaithChange(int from, int to)
        {
            _faithFrom = from;
            _faithTo = to;
            _faithChangeTime = Time.time;
        }

        private void NoteCoreChange(int from, int to)
        {
            _hpFrom = from;
            _hpTo = to;
            _hpChangeTime = Time.time;
        }

        /// <summary>
        /// O "rastro" da vida demora um pouco para acompanhar uma perda (como nos jogos de luta):
        /// o jogador vê quanto acabou de perder. Ganhos aparecem na hora.
        /// </summary>
        private void UpdateResourceAnimation(float deltaTime)
        {
            float hp = _session.DivineCore.CurrentHealth;

            if (hp >= _hpTrail)
            {
                _hpTrail = hp;
            }
            else if (Time.time - _hpChangeTime > 0.45f)
            {
                _hpTrail = Mathf.MoveTowards(_hpTrail, hp, deltaTime * Mathf.Max(4f, _session.DivineCore.MaxHealth * 0.6f));
            }
        }

        private static Rect Scaled(Rect rect, float scale)
        {
            float w = rect.width * scale;
            float h = rect.height * scale;
            return new Rect(rect.center.x - w * 0.5f, rect.center.y - h * 0.5f, w, h);
        }

        private void DrawTopBar(float width)
        {
            _theme.Frame(new Rect(0f, 0f, width, TopBarHeight), GuiTheme.Bronze, new Color(0.06f, 0.05f, 0.07f, 0.97f), 3f);
            _theme.Image(new Rect(18f, 12f, 40f, 40f), _art.Sprite("core"), Color.white);
            _theme.ShadowText(new Rect(66f, 12f, 280f, 40f), "SOVEREIGN OF ASHES", _theme.Number, GuiTheme.Gold);

            TurnSystem turns = _session.Turns;
            string goal = _session.TurnsToSurvive > 0 ? $" / {_session.TurnsToSurvive}" : " / ∞";
            _theme.ShadowText(new Rect(380f, 6f, 200f, 30f), $"TURNO {turns.TurnNumber}{goal}", _theme.Number, Color.white);

            string state;
            Color stateColor;

            switch (turns.State)
            {
                case TurnState.AwaitingPlayer: state = "SUA VEZ"; stateColor = GuiTheme.Good; break;
                case TurnState.GameOver: state = "FIM DE JOGO"; stateColor = GuiTheme.Gold; break;
                default: state = "RESOLVENDO..."; stateColor = GuiTheme.TextDim; break;
            }

            _theme.ShadowText(new Rect(380f, 33f, 200f, 18f), state, _theme.Tag, stateColor);

            const float faithX = 610f;
            const float heartsX = 940f;
            DrawFaithCrystals(faithX, 8f);
            DrawCoreHearts(heartsX, 8f);
            _faithGuiPosition = new Vector2(faithX + ClusterWidth * 0.5f, 30f);

            // Botão do Grimório (também abre com a tecla G).
            var grimoire = new Rect(width - 224f, 10f, 208f, 44f);
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);
            bool hover = grimoire.Contains(mouse);
            _theme.Frame(grimoire, hover || _showGrimoire ? GuiTheme.Gold : GuiTheme.Bronze,
                _showGrimoire ? new Color(0.34f, 0.22f, 0.08f) : new Color(0.16f, 0.11f, 0.08f), 3f);
            _theme.Image(new Rect(grimoire.x + 8f, grimoire.y + 6f, 32f, 32f), _art.Sprite("book"), Color.white);
            _theme.ShadowText(new Rect(grimoire.x + 36f, grimoire.y, grimoire.width - 40f, grimoire.height), "GRIMÓRIO  [G]",
                _theme.SmallNumber, Color.white);

            if (GUI.Button(grimoire, GUIContent.none, GUIStyle.none))
            {
                _showGrimoire = !_showGrimoire;
            }

            int turnGoal = _session.TurnsToSurvive;

            if (turnGoal > 0)
            {
                _theme.SegmentedTrack(new Rect(10f, 56f, width - 20f, 4f), turnGoal, Mathf.Clamp(turns.TurnNumber, 0, turnGoal),
                    GuiTheme.Gold, GuiTheme.Good);
            }
        }

        /// <summary>
        /// A Fé do jogador como cristais, um por ponto de capacidade: cheios brilham, vazios ficam escuros.
        /// Ao ganhar, os cristais novos "estouram"; ao gastar, os gastados se dissolvem.
        /// </summary>
        private void DrawFaithCrystals(float x, float y)
        {
            int capacity = Mathf.Max(1, _session.Faith.Capacity);
            int current = _session.Faith.Current;
            float step = Mathf.Min(29f, ClusterWidth / capacity);
            float size = step - 2f;
            Texture2D crystal = _art.Sprite("crystal");
            float age = Time.time - _faithChangeTime;
            bool animating = age >= 0f && age < ResourceAnimSeconds;
            float t = animating ? age / ResourceAnimSeconds : 1f;

            for (int i = 0; i < capacity; i++)
            {
                var rect = new Rect(x + i * step, y + 3f, size, size);
                bool gained = animating && _faithTo > _faithFrom && i >= _faithFrom && i < _faithTo;
                bool spent = animating && _faithTo < _faithFrom && i >= _faithTo && i < _faithFrom;

                if (i < current)
                {
                    if (_skin.Glow != null)
                    {
                        float glow = 0.26f + 0.12f * Mathf.Sin(Time.time * 3f + i * 0.7f) + (gained ? 0.35f * (1f - t) : 0f);
                        _theme.Image(Scaled(rect, 2.2f), _skin.Glow, new Color(0.35f, 0.6f, 1f, glow));
                    }

                    Rect drawn = gained ? Scaled(rect, 1f + 0.55f * (1f - t)) : rect;
                    _theme.Image(drawn, crystal, gained ? Color.Lerp(new Color(0.75f, 0.92f, 1f), Color.white, t) : Color.white);
                }
                else
                {
                    _theme.Image(rect, crystal, new Color(0.18f, 0.19f, 0.26f, 0.95f));

                    if (spent)
                    {
                        _theme.Image(Scaled(rect, 1f + 0.5f * t), crystal, new Color(0.75f, 0.9f, 1f, 1f - t));
                    }
                }
            }

            _theme.ShadowText(new Rect(x, y + 31f, ClusterWidth, 18f), $"Fé  {current}/{_session.Faith.Capacity}", _theme.Tag, GuiTheme.FaithColor);
        }

        /// <summary>
        /// A vida do Núcleo como corações (no máximo 10; cada um vale vários pontos quando a vida é alta),
        /// com preenchimento parcial. Ao levar dano, os corações atingidos tremem e deixam um rastro claro;
        /// com vida baixa, pulsam em vermelho.
        /// </summary>
        private void DrawCoreHearts(float x, float y)
        {
            int max = Mathf.Max(1, _session.DivineCore.MaxHealth);
            int hp = _session.DivineCore.CurrentHealth;
            int perHeart = Mathf.Max(1, (max + 9) / 10);
            int hearts = (max + perHeart - 1) / perHeart;
            float step = Mathf.Min(29f, ClusterWidth / hearts);
            float size = step - 2f;
            Texture2D heart = _art.Sprite("heart");
            float age = Time.time - _hpChangeTime;
            bool animating = age >= 0f && age < 0.5f;
            bool damage = animating && _hpTo < _hpFrom;
            bool heal = animating && _hpTo > _hpFrom;
            bool low = hp * 4 <= max;
            float beat = low ? 0.5f + 0.5f * Mathf.Sin(Time.time * 7f) : 0f;

            for (int i = 0; i < hearts; i++)
            {
                float low01 = i * perHeart;
                float fill = Mathf.Clamp01((hp - low01) / perHeart);
                float trail = Mathf.Clamp01((_hpTrail - low01) / perHeart);
                bool touched = low01 < Mathf.Max(_hpFrom, _hpTo) && low01 + perHeart > Mathf.Min(_hpFrom, _hpTo);
                float shake = damage && touched ? Mathf.Sin(age * 70f) * 3f * (1f - age / 0.5f) : 0f;
                var rect = new Rect(x + i * step + shake, y + 3f, size, size);

                if (low && fill > 0f && _skin.Glow != null)
                {
                    _theme.Image(Scaled(rect, 2f), _skin.Glow, new Color(1f, 0.15f, 0.1f, 0.15f + 0.3f * beat));
                }

                _theme.Image(rect, heart, new Color(0.2f, 0.12f, 0.16f, 0.95f));
                DrawPartialSprite(rect, heart, trail, new Color(1f, 0.88f, 0.88f, 0.95f));

                Rect drawn = heal && touched ? Scaled(rect, 1.25f - 0.25f * (age / 0.5f)) : rect;
                DrawPartialSprite(drawn, heart, fill, heal && touched ? new Color(0.8f, 1f, 0.8f) : Color.white);
            }

            Color label = hp * 3 <= max ? GuiTheme.Bad : GuiTheme.Text;
            _theme.ShadowText(new Rect(x, y + 31f, ClusterWidth, 18f), $"Núcleo  {hp}/{max}", _theme.Tag, label);
        }

        /// <summary>Desenha só a parte esquerda do sprite (de 0 a 1), mantendo os pixels nítidos.</summary>
        private void DrawPartialSprite(Rect rect, Texture2D sprite, float fill, Color tint)
        {
            fill = Mathf.Clamp01(fill);

            if (fill <= 0f)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, rect.width * fill, rect.height), sprite, new Rect(0f, 0f, fill, 1f));
            GUI.color = previous;
        }

        private void DrawPiles(float height)
        {
            Texture2D back = _art.Painting(ArtCatalog.CardBackPath);
            DrawPile(new Rect(38f, height - 196f, 100f, 134f), back, _session.Deck.DrawPileCount, "COMPRA");
            DrawPile(new Rect(160f, height - 196f, 100f, 134f), back, _session.Deck.DiscardPileCount, "DESCARTE");
        }

        private void DrawPile(Rect rect, Texture2D back, int count, string label)
        {
            _theme.ShadowText(new Rect(rect.x - 10f, rect.y - 24f, rect.width + 20f, 20f), label, _theme.SmallNumber, GuiTheme.TextDim);

            if (count <= 0)
            {
                _theme.Fill(rect, new Color(0f, 0f, 0f, 0.35f));
                _theme.Frame(rect, new Color(GuiTheme.Bronze.r, GuiTheme.Bronze.g, GuiTheme.Bronze.b, 0.5f), new Color(0f, 0f, 0f, 0f), 2f);
                _theme.ShadowText(rect, "vazio", _theme.SmallNumber, GuiTheme.TextDim);
                return;
            }

            int layers = Mathf.Min(4, 1 + count / 4);

            for (int i = layers - 1; i >= 0; i--)
            {
                var layer = new Rect(rect.x + i * 3f, rect.y - i * 3f, rect.width, rect.height);
                _theme.Fill(new Rect(layer.x - 2f, layer.y - 2f, layer.width + 4f, layer.height + 4f), new Color(0f, 0f, 0f, 0.85f));

                if (back != null)
                {
                    GUI.DrawTexture(layer, back);
                }
                else
                {
                    _theme.Frame(layer, GuiTheme.Bronze, new Color(0.22f, 0.1f, 0.16f), 3f);
                    _theme.Image(new Rect(layer.center.x - 20f, layer.center.y - 20f, 40f, 40f), _art.Sprite("core"), Color.white);
                }
            }

            var badge = new Rect(rect.center.x - 20f, rect.yMax - 22f, 40f, 28f);
            _theme.Frame(badge, GuiTheme.Gold, new Color(0.08f, 0.06f, 0.08f), 2f);
            _theme.ShadowText(badge, count.ToString(), _theme.SmallNumber, Color.white);
        }

        private void DrawCardZoom(float startX, float baseY, float width)
        {
            if (_hoveredCard < 0 || _hoveredCard >= _session.Deck.Hand.Count)
            {
                return;
            }

            ICardDefinition card = _session.Deck.Hand[_hoveredCard];
            const float scale = 1.5f;
            float zoomWidth = CardWidth * scale;
            float zoomHeight = CardHeight * scale;
            float centerX = startX + _hoveredCard * (CardWidth + CardSpacing) + CardWidth * 0.5f;
            float x = Mathf.Clamp(centerX - zoomWidth * 0.5f, 16f, width - zoomWidth - 16f);
            float y = baseY - zoomHeight - 46f;

            Texture2D illustration = _art.CardIllustration(card);
            CardPainter.DrawZoomed(new Rect(x, y, zoomWidth, zoomHeight), card, illustration != null ? illustration : _art.Card(card),
                _art.Sprite("crystal"), _theme, _session.Faith.CanAfford(card.Cost), illustration != null);
        }

        // ------------------------------------------------------------------ Fim de jogo

        private void RegisterEndlessRecord(GameOverInfo result, bool endless)
        {
            if (!endless || result == null || _recordRegistered)
            {
                return;
            }

            _recordRegistered = true;
            _newRecord = result.TurnNumber > _settings.BestEndlessTurn;

            if (_newRecord)
            {
                _settings.BestEndlessTurn = result.TurnNumber;
                SaveSettings();
            }
        }

        private void DrawGameOver(float width, float height)
        {
            GameOverInfo result = _session.Turns.Result;
            bool endless = _session.TurnsToSurvive == 0;
            RegisterEndlessRecord(result, endless);
            bool victory = _gameOverPreview ?? (result != null && result.Outcome == GameOutcome.Victory);
            Texture2D painting = _art.Painting(victory ? ArtCatalog.VictoryPath : ArtCatalog.DefeatPath);

            if (painting != null)
            {
                CardPainter.DrawCover(new Rect(0f, 0f, width, height), painting);
                _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, victory ? 0.28f : 0.42f));
            }
            else
            {
                _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.78f));
            }

            // Faixa de leitura atrás do texto.
            _theme.Fill(new Rect(0f, height * 0.58f - 20f, width, 250f), new Color(0f, 0f, 0f, 0.6f));
            _theme.Fill(new Rect(0f, height * 0.58f - 20f, width, 3f), new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, 0.8f));
            _theme.Fill(new Rect(0f, height * 0.58f + 227f, width, 3f), new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, 0.8f));

            _theme.ShadowText(new Rect(0f, height * 0.58f, width, 90f), victory ? "VITÓRIA" : "O TRONO ESTÁ VAZIO",
                _theme.Banner, victory ? GuiTheme.Gold : GuiTheme.Bad);
            _theme.ShadowText(new Rect(0f, height * 0.58f + 88f, width, 30f),
                endless && result != null
                    ? $"Você resistiu a {result.TurnNumber} turnos  ·  recorde: {_settings.BestEndlessTurn}{(_newRecord ? "  (NOVO!)" : string.Empty)}"
                    : result != null ? $"{result.Reason} (turno {result.TurnNumber})" : string.Empty, _theme.Centered, GuiTheme.Text);

            if (MenuButton(new Rect(width * 0.5f - 320f, height * 0.58f + 138f, 310f, 72f), "JOGAR NOVAMENTE", primary: true))
            {
                _bootstrapper.RestartSession();
            }

            if (MenuButton(new Rect(width * 0.5f + 10f, height * 0.58f + 138f, 310f, 72f), "MENU PRINCIPAL"))
            {
                _bootstrapper.CloseSession();
            }
        }
    }
}
