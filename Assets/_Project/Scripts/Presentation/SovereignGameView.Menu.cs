using System;
using System.Linq;
using SovereignOfAshes.Achievements;
using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "menus" da view: menu principal, Como jogar, Configurações e pausa. Só decide o que mostrar e chama
    /// o <see cref="SovereignBootstrapper"/> para começar, reiniciar ou encerrar a partida.
    /// As preferências (dificuldade, tela cheia, velocidade, semente) são salvas a cada mudança.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private enum UiScreen
        {
            Menu,
            HowToPlay,
            Settings,
            Achievements,
            Covenant,
            Playing,
            Paused
        }

        private const float MenuButtonHeight = 60f;

        private UiScreen _uiScreen = UiScreen.Menu;
        private UiScreen _settingsBack = UiScreen.Menu;
        private GameSettings _settings = new GameSettings();
        private ISettingsStore _store;
        private string _seedText = "0";
        private bool _captureMode;
        private bool _recordRegistered;
        private bool _newRecord;
        private string _confirmLabel;
        private float _confirmUntil;

        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;

        private GUIStyle BodyStyle => _bodyStyle ?? (_bodyStyle = new GUIStyle(_theme.Label) { fontSize = 19, wordWrap = true });

        private GUIStyle HintStyle => _hintStyle ?? (_hintStyle = new GUIStyle(_theme.Small) { fontSize = 16, wordWrap = true });

        private bool IsMenuScreen => _uiScreen == UiScreen.Menu || _uiScreen == UiScreen.HowToPlay || _uiScreen == UiScreen.Settings || _uiScreen == UiScreen.Achievements || _uiScreen == UiScreen.Covenant;

        // ------------------------------------------------------------------ Ciclo de vida

        private void InitializeMenu()
        {
            _captureMode = SovereignCaptureHarness.ReadDirectory(Environment.GetCommandLineArgs()) != null;

            // No modo de captura as preferências do jogador não são lidas nem gravadas: os prints saem sempre iguais.
            _store = _captureMode ? (ISettingsStore)new InMemorySettingsStore() : new PlayerPrefsSettingsStore();
            _settings = GameSettings.Parse(_store.Load());
            _seedText = _settings.Seed.ToString();
            InitializeAchievements();
            ApplyDisplaySettings();
            _uiScreen = _bootstrapper.Session != null ? UiScreen.Playing : UiScreen.Menu;
        }

        private void OnSessionBound()
        {
            if (_session != null)
            {
                _uiScreen = UiScreen.Playing;
                _showGrimoire = false;
                _recordRegistered = false;
                _newRecord = false;
                StartTracking();
            }
        }

        private void OnSessionClosed()
        {
            Unbind();
            _uiScreen = UiScreen.Menu;
            _showGrimoire = false;
        }

        private void StartGame(GameMode mode = GameMode.Standard, string covenantId = null)
        {
            GameSettings next = _settings.Clone();
            next.Mode = mode;
            next.CovenantId = covenantId;
            _bootstrapper.StartNewGame(next);
        }

        private void ApplyDisplaySettings()
        {
            if (!_captureMode)
            {
                Screen.fullScreen = _settings.Fullscreen;
            }
        }

        private void SaveSettings()
        {
            _store.Save(_settings.Serialize());
        }

        private void GoToSettings()
        {
            _settingsBack = _uiScreen;
            _uiScreen = UiScreen.Settings;
        }

        private void LeaveSettings()
        {
            _uiScreen = _settingsBack == UiScreen.Paused ? UiScreen.Paused : UiScreen.Menu;
        }

        private void TogglePause()
        {
            if (_session == null || _session.Turns.State == TurnState.GameOver)
            {
                return;
            }

            _uiScreen = _uiScreen == UiScreen.Paused ? UiScreen.Playing : UiScreen.Paused;
        }

        // Usados só pelo modo de captura automática de telas.
        internal void ShowScreenForCapture(string name)
        {
            switch (name)
            {
                case "settings": _settingsBack = UiScreen.Menu; _uiScreen = UiScreen.Settings; break;
                case "howto": _uiScreen = UiScreen.HowToPlay; break;
                case "achievements": _uiScreen = UiScreen.Achievements; break;
                case "covenant": OpenCovenantChoice(GameMode.Standard); break;
                default: _uiScreen = UiScreen.Menu; break;
            }
        }

        internal void StartGameForCapture(bool endless = false) => StartGame(endless ? GameMode.Endless : GameMode.Standard);

        internal void SetPausedForCapture(bool paused)
        {
            if (_session != null)
            {
                _uiScreen = paused ? UiScreen.Paused : UiScreen.Playing;
            }
        }

        // ------------------------------------------------------------------ Primitivas

        private bool MenuButton(Rect rect, string label, bool primary = false)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);
            bool hover = rect.Contains(mouse);
            Color border = hover || primary ? GuiTheme.Gold : GuiTheme.Bronze;
            Color fill = primary
                ? (hover ? new Color(0.62f, 0.18f, 0.10f) : new Color(0.46f, 0.12f, 0.08f))
                : (hover ? new Color(0.32f, 0.21f, 0.10f) : new Color(0.16f, 0.11f, 0.08f));
            _theme.Frame(rect, border, fill, primary ? 4f : 3f);
            _theme.ShadowText(rect, label, _theme.ButtonText, Color.white);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Botão de opção (grupo de escolha única): o selecionado fica dourado.</summary>
        private bool OptionButton(Rect rect, string label, bool selected)
        {
            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(-1f, -1f);
            bool hover = rect.Contains(mouse);
            Color border = selected ? GuiTheme.Gold : hover ? GuiTheme.Text : GuiTheme.Bronze;
            Color fill = selected ? new Color(0.40f, 0.27f, 0.08f) : hover ? new Color(0.24f, 0.17f, 0.09f) : new Color(0.12f, 0.09f, 0.08f);
            _theme.Frame(rect, border, fill, selected ? 4f : 3f);
            _theme.ShadowText(rect, label, _theme.SmallNumber, selected ? Color.white : GuiTheme.Text);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none) && !selected;
        }

        /// <summary>Botão que pede uma segunda confirmação em até 3 segundos (para ações que perdem a partida).</summary>
        private bool ConfirmButton(Rect rect, string label)
        {
            bool armed = _confirmLabel == label && Time.time < _confirmUntil;

            if (MenuButton(rect, armed ? "CLIQUE DE NOVO PARA CONFIRMAR" : label))
            {
                if (armed)
                {
                    _confirmLabel = null;
                    return true;
                }

                _confirmLabel = label;
                _confirmUntil = Time.time + 3f;
            }

            return false;
        }

        // ------------------------------------------------------------------ Telas

        private void DrawMenuFlow(float width, float height)
        {
            HandleMenuKeys();
            DrawMenuBackdrop(width, height);

            switch (_uiScreen)
            {
                case UiScreen.HowToPlay: DrawHowToPlay(width, height); break;
                case UiScreen.Settings: DrawSettings(width, height); break;
                case UiScreen.Achievements: DrawAchievements(width, height); break;
                case UiScreen.Covenant: DrawCovenantChoice(width, height); break;
                default: DrawMainMenu(width, height); break;
            }
        }

        private void HandleMenuKeys()
        {
            Event current = Event.current;

            if (current == null || current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.Escape)
            {
                if (_uiScreen == UiScreen.Settings)
                {
                    LeaveSettings();
                }
                else if (_uiScreen == UiScreen.HowToPlay || _uiScreen == UiScreen.Achievements || _uiScreen == UiScreen.Covenant)
                {
                    _uiScreen = UiScreen.Menu;
                }
            }
            else if (current.keyCode == KeyCode.Return && _uiScreen == UiScreen.Menu && _session == null)
            {
                OpenCovenantChoice(GameMode.Standard);
            }
        }

        private void DrawMenuBackdrop(float width, float height)
        {
            Texture2D painting = _art.Painting(ArtCatalog.BackdropPath);

            if (painting != null)
            {
                CardPainter.DrawCover(new Rect(0f, 0f, width, height), painting);
            }
            else
            {
                _theme.Fill(new Rect(0f, 0f, width, height), GuiTheme.Background);
            }

            // Escurece o conjunto e as bordas para o texto ler bem.
            _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.42f));
            _theme.Fill(new Rect(0f, 0f, width, 150f), new Color(0f, 0f, 0f, 0.35f));
            _theme.Fill(new Rect(0f, height - 150f, width, 150f), new Color(0f, 0f, 0f, 0.45f));

            if (_session == null && Event.current != null && Event.current.type == EventType.Repaint)
            {
                _embers.Update(Time.deltaTime);
            }

            _embers.Draw(width, height, _theme.White);
        }

        private void DrawMainMenu(float width, float height)
        {
            float cx = width * 0.5f;

            Texture2D logo = _art.Painting(ArtCatalog.LogoPath);

            if (logo != null)
            {
                // O logo tem bordas translúcidas: respira de leve e se mistura ao fundo.
                float breathe = 1f + 0.012f * Mathf.Sin(Time.time * 1.6f);
                float logoWidth = 1180f * breathe;
                float logoHeight = logoWidth * logo.height / logo.width;
                GUI.DrawTexture(new Rect(cx - logoWidth * 0.5f, -14f - (logoHeight - 660f) * 0.5f, logoWidth, logoHeight), logo, ScaleMode.StretchToFill, true);
            }
            else
            {
                _theme.ShadowText(new Rect(0f, 250f, width, 80f), "SOVEREIGN OF ASHES", _theme.Banner, GuiTheme.Gold);
                _theme.ShadowText(new Rect(0f, 330f, width, 34f), "Ecos do Trono Vazio", _theme.Centered, GuiTheme.Text);
            }

            if (MenuButton(new Rect(cx - 360f, 628f, 350f, 70f), "JOGAR AGORA", primary: true))
            {
                OpenCovenantChoice(GameMode.Standard);
            }

            string record = _settings.BestEndlessTurn > 0 ? $"INFINITO  ·  RECORDE {_settings.BestEndlessTurn}" : "MODO INFINITO (HARDCORE)";

            if (MenuButton(new Rect(cx + 10f, 628f, 350f, 70f), record))
            {
                OpenCovenantChoice(GameMode.Endless);
            }

            string trophies = $"CONQUISTAS {_achievements.UnlockedCount}/{AchievementCatalog.All.Count}";
            string[] labels = { "COMO JOGAR", trophies, "CONFIGURAÇÕES", "SAIR" };
            float[] widths = { 190f, 250f, 220f, 130f };
            float x = cx - (190f + 250f + 220f + 130f + 3f * 12f) * 0.5f;

            for (int i = 0; i < labels.Length; i++)
            {
                if (MenuButton(new Rect(x, 714f, widths[i], 52f), labels[i]))
                {
                    switch (i)
                    {
                        case 0: _uiScreen = UiScreen.HowToPlay; break;
                        case 1: _uiScreen = UiScreen.Achievements; break;
                        case 2: GoToSettings(); break;
                        default: Application.Quit(); break;
                    }
                }

                x += widths[i] + 12f;
            }

            string seed = _settings.Seed == 0 ? "aleatória" : _settings.Seed.ToString();
            _theme.ShadowText(new Rect(0f, height - 92f, width, 26f),
                $"Dificuldade: {DifficultyPresets.Name(_settings.Difficulty)}     ·     Semente: {seed}", _theme.Centered, GuiTheme.TextDim);
            _theme.ShadowText(new Rect(0f, height - 62f, width, 26f),
                "Dentro da partida:  Esc pausa   ·   G abre o Grimório   ·   passe o mouse numa carta para ampliá-la", _theme.Centered, GuiTheme.TextDim);
        }

        private void DrawHowToPlay(float width, float height)
        {
            var panel = new Rect((width - 1100f) * 0.5f, 70f, 1100f, height - 140f);
            _theme.Panel(panel);
            _theme.ShadowText(new Rect(panel.x, panel.y + 18f, panel.width, 44f), "Como jogar", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(panel.x + 60f, panel.y + 70f, panel.width - 120f);

            string[,] sections =
            {
                { "Objetivo", "Mantenha o Núcleo Divino de pé por 20 turnos." },
                { "Seu turno", "Você compra 5 cartas e gasta Fé (os cristais lá em cima) para jogá-las. Em cartas com alvo, clique na carta e depois num setor destacado do tabuleiro; as de uso imediato são jogadas ao clicar. O botão direito cancela. Quando terminar, clique em ENCERRAR TURNO." },
                { "Depois do seu turno", "Os anéis giram, sua civilização combate e a Razão dela sobe, os invasores marcham um anel para dentro e novas ondas surgem. As setas no tabuleiro mostram para onde cada invasor vai." },
                { "Invasores", "Eles entram pelo setor de mesmo número no anel seguinte (os números ficam na borda do tabuleiro). Ao chegar ao Santuário Interno, atacam o Núcleo todo turno. Cartas de ataque e a sua civilização os derrubam; cada um destruído rende Fé." },
                { "Razão e heresia", "Se a Razão da civilização chegar ao máximo, ela vira herege e passa a atacar o Núcleo. Cartas como o Sermão a reduzem." },
                { "Oferendas e Pactos", "Antes de cada partida você escolhe um Pacto: uma regra especial com vantagem e preço. Durante a partida, a cada 3 turnos surge uma Oferenda Divina: escolha 1 de 3 cartas para o baralho (ou recuse e ganhe Fé). Cartas como Giro Divino giram um anel inteiro e reposicionam os invasores." },
                { "Novos invasores", "O Profanador destrói o elemento do setor em que chega. O Arauto Herege e o Inquisidor (chefe do turno 10) somam Razão às civilizações a cada turno: mate-os antes que a heresia chegue." },
                { "Modo infinito", "No botão MODO INFINITO as ondas nunca acabam: chegam hordas cada vez maiores e mais fortes, com a dificuldade Difícil fixa. Não existe vitória; o objetivo é bater o seu recorde de turnos." },
                { "Alquimia", "As cartas Infundir criam conceitos; jogar um conceito sobre outro os funde em estruturas e seres que agem sozinhos (Forja, Golem, Santo, Espectro...). A ordem importa: aperte G para ver as receitas no Grimório." }
            };

            // Duas colunas: as seções cabem sem passar do painel.
            float columnWidth = (panel.width - 150f) * 0.5f;
            int perColumn = (sections.GetLength(0) + 1) / 2;
            float top = panel.y + 92f;
            float y = top;

            for (int i = 0; i < sections.GetLength(0); i++)
            {
                if (i == perColumn)
                {
                    y = top;
                }

                float x = panel.x + 55f + (i < perColumn ? 0f : columnWidth + 40f);
                GUI.Label(new Rect(x, y, columnWidth, 28f), sections[i, 0], _theme.Title);
                y += 30f;
                var body = new GUIContent(sections[i, 1]);
                float bodyHeight = HintStyle.CalcHeight(body, columnWidth);
                GUI.Label(new Rect(x, y, columnWidth, bodyHeight), body, HintStyle);
                y += bodyHeight + 18f;
            }

            if (MenuButton(new Rect(panel.center.x - 130f, panel.yMax - 84f, 260f, MenuButtonHeight), "VOLTAR"))
            {
                _uiScreen = UiScreen.Menu;
            }
        }

        private void DrawSettings(float width, float height)
        {
            var panel = new Rect((width - 1080f) * 0.5f, 120f, 1080f, 700f);
            _theme.Panel(panel);
            _theme.ShadowText(new Rect(panel.x, panel.y + 18f, panel.width, 44f), "Configurações", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(panel.x + 60f, panel.y + 70f, panel.width - 120f);

            float labelX = panel.x + 60f;
            float controlX = panel.x + 340f;
            float y = panel.y + 110f;

            // Dificuldade.
            GUI.Label(new Rect(labelX, y + 10f, 260f, 30f), "Dificuldade", _theme.Title);

            foreach (Difficulty difficulty in (Difficulty[])Enum.GetValues(typeof(Difficulty)))
            {
                var option = new Rect(controlX + (int)difficulty * 170f, y, 160f, 48f);

                if (OptionButton(option, DifficultyPresets.Name(difficulty), _settings.Difficulty == difficulty))
                {
                    _settings.Difficulty = difficulty;
                    SaveSettings();
                }
            }

            var description = new GUIContent(DifficultyPresets.Describe(_settings.Difficulty));
            float descriptionHeight = HintStyle.CalcHeight(description, 600f);
            GUI.Label(new Rect(controlX, y + 56f, 600f, descriptionHeight), description, HintStyle);
            y += 56f + Mathf.Max(descriptionHeight, 34f) + 34f;

            // Tela cheia.
            GUI.Label(new Rect(labelX, y + 10f, 260f, 30f), "Tela cheia", _theme.Title);

            if (OptionButton(new Rect(controlX, y, 160f, 48f), "Desligada", !_settings.Fullscreen))
            {
                _settings.Fullscreen = false;
                ApplyDisplaySettings();
                SaveSettings();
            }

            if (OptionButton(new Rect(controlX + 170f, y, 160f, 48f), "Ligada", _settings.Fullscreen))
            {
                _settings.Fullscreen = true;
                ApplyDisplaySettings();
                SaveSettings();
            }

            y += 84f;

            // Velocidade das animações.
            GUI.Label(new Rect(labelX, y + 10f, 260f, 30f), "Animações", _theme.Title);

            if (OptionButton(new Rect(controlX, y, 160f, 48f), "Normal", _settings.Animation == AnimationSpeed.Normal))
            {
                _settings.Animation = AnimationSpeed.Normal;
                SaveSettings();
            }

            if (OptionButton(new Rect(controlX + 170f, y, 160f, 48f), "Rápidas", _settings.Animation == AnimationSpeed.Fast))
            {
                _settings.Animation = AnimationSpeed.Fast;
                SaveSettings();
            }

            GUI.Label(new Rect(controlX + 345f, y - 2f, 340f, 60f), "Só a velocidade da rotação dos anéis e da marcha dos invasores.", HintStyle);
            y += 84f;

            // Semente.
            GUI.Label(new Rect(labelX, y + 10f, 260f, 30f), "Semente", _theme.Title);
            var seedBox = new Rect(controlX, y, 220f, 48f);
            _theme.Frame(seedBox, GuiTheme.Bronze, new Color(0.10f, 0.08f, 0.11f), 3f);
            string edited = GUI.TextField(new Rect(seedBox.x + 3f, seedBox.y + 3f, seedBox.width - 6f, seedBox.height - 6f), _seedText, 9, _theme.TextInput);

            if (edited != _seedText)
            {
                _seedText = new string(edited.Where(char.IsDigit).ToArray());
                int.TryParse(_seedText, out int seed);
                _settings.Seed = seed;
                SaveSettings();
            }

            GUI.Label(new Rect(controlX + 235f, y - 4f, 450f, 60f),
                "0 = partida aleatória. Use o mesmo número para repetir exatamente a mesma partida (cartas e invasores).", HintStyle);
            y += 90f;

            string note = _settingsBack == UiScreen.Paused
                ? "Dificuldade e semente valem a partir da próxima partida."
                : "As configurações são salvas automaticamente.";
            _theme.ShadowText(new Rect(panel.x, y, panel.width, 26f), note, _theme.Centered, GuiTheme.TextDim);

            if (MenuButton(new Rect(panel.x + 60f, panel.yMax - 90f, 340f, MenuButtonHeight), "RESTAURAR PADRÕES"))
            {
                _settings = new GameSettings();
                _seedText = "0";
                ApplyDisplaySettings();
                SaveSettings();
            }

            if (MenuButton(new Rect(panel.xMax - 60f - 260f, panel.yMax - 90f, 260f, MenuButtonHeight), "VOLTAR"))
            {
                LeaveSettings();
            }
        }

        private void DrawPauseMenu(float width, float height)
        {
            _theme.Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.7f));

            var panel = new Rect((width - 520f) * 0.5f, 210f, 520f, 540f);
            _theme.Panel(panel);
            _theme.ShadowText(new Rect(panel.x, panel.y + 20f, panel.width, 44f), "Pausado", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(panel.x + 50f, panel.y + 72f, panel.width - 100f);

            float x = panel.x + 60f;
            float w = panel.width - 120f;
            float y = panel.y + 108f;

            if (MenuButton(new Rect(x, y, w, 72f), "CONTINUAR", primary: true))
            {
                _uiScreen = UiScreen.Playing;
            }

            y += 96f;

            if (MenuButton(new Rect(x, y, w, MenuButtonHeight), "GRIMÓRIO"))
            {
                _uiScreen = UiScreen.Playing;
                _showGrimoire = true;
            }

            y += 76f;

            if (MenuButton(new Rect(x, y, w, MenuButtonHeight), "CONFIGURAÇÕES"))
            {
                GoToSettings();
            }

            y += 76f;

            if (ConfirmButton(new Rect(x, y, w, MenuButtonHeight), "REINICIAR PARTIDA"))
            {
                _bootstrapper.RestartSession();
            }

            y += 76f;

            if (ConfirmButton(new Rect(x, y, w, MenuButtonHeight), "MENU PRINCIPAL"))
            {
                _bootstrapper.CloseSession();
            }
        }
    }
}
