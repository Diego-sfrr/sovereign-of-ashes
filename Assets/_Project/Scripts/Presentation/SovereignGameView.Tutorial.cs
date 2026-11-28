using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Tutorial guiado: uma partida fácil e sempre igual (semente fixa) com um painel de instruções por cima do jogo
    /// normal. Cada passo destaca uma parte da tela; os de ação avançam sozinhos quando o jogador faz o que foi pedido
    /// (jogar uma carta, encerrar o turno), e todos podem ser pulados.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const int TutorialSeed = 7;

        private enum TutorialArea
        {
            None,
            Board,
            TopBar,
            Hand,
            LeftPanel,
            RightPanel,
            EndTurn
        }

        private enum TutorialAdvance
        {
            Button,
            PlayCard,
            EndTurn
        }

        private struct TutorialStep
        {
            public string Title;
            public string Body;
            public TutorialArea Area;
            public TutorialAdvance Advance;
        }

        private static readonly TutorialStep[] TutorialSteps =
        {
            new TutorialStep
            {
                Title = "Bem-vindo, Soberano",
                Body = "Você defende o Núcleo Divino, no centro do tabuleiro. Os invasores nascem no anel externo e avançam um anel por turno até o Núcleo; se ele cair, você perde. Sobreviva 20 turnos.",
                Area = TutorialArea.Board
            },
            new TutorialStep
            {
                Title = "Seus recursos",
                Body = "No topo ficam a vida do Núcleo (os corações), a Fé (os cristais) que paga as cartas e o turno atual. A Fé volta a encher no começo de cada turno.",
                Area = TutorialArea.TopBar
            },
            new TutorialStep
            {
                Title = "Sua mão",
                Body = "Estas são as suas cartas. O número no canto é o custo em Fé. Passe o mouse numa carta para ampliá-la e ler o efeito.",
                Area = TutorialArea.Hand
            },
            new TutorialStep
            {
                Title = "Jogue uma carta",
                Body = "Clique numa carta e depois num setor destacado do tabuleiro. Cartas de uso imediato são jogadas só com o clique. O botão direito cancela. Jogue uma carta para continuar.",
                Area = TutorialArea.Hand,
                Advance = TutorialAdvance.PlayCard
            },
            new TutorialStep
            {
                Title = "Invasores",
                Body = "À direita ficam os invasores e a previsão do próximo turno. As setas no tabuleiro mostram para onde cada um vai marchar, então dá para se antecipar.",
                Area = TutorialArea.RightPanel
            },
            new TutorialStep
            {
                Title = "Sua civilização",
                Body = "À esquerda fica a sua civilização: ela combate sozinha a cada turno, mas ganha Razão. Se a Razão chegar ao máximo, ela vira herege e ataca o Núcleo. Cartas como o Sermão reduzem a Razão.",
                Area = TutorialArea.LeftPanel
            },
            new TutorialStep
            {
                Title = "Alquimia",
                Body = "Cartas Infundir criam conceitos num setor. Jogar um conceito sobre outro funde os dois em estruturas e seres (Forja, Golem, Santo...). A ordem importa: aperte G para ver todas as receitas no Grimório.",
                Area = TutorialArea.Board
            },
            new TutorialStep
            {
                Title = "Encerrar o turno",
                Body = "Quando terminar, clique em ENCERRAR TURNO. Os anéis giram, a sua civilização combate e os invasores marcham.",
                Area = TutorialArea.EndTurn,
                Advance = TutorialAdvance.EndTurn
            },
            new TutorialStep
            {
                Title = "Você está pronto!",
                Body = "A partir daqui é com você. A cada 3 turnos surge uma Oferenda Divina (escolha 1 de 3 cartas), no turno 10 vem um chefe e no 15 o Rei Herege. Esc pausa a partida. Boa sorte!",
                Area = TutorialArea.None
            }
        };

        private int _tutorialStep = -1;
        private bool _startingTutorial;
        private int _tutorialStartTurn;
        private int _tutorialStartHand;

        private bool TutorialActive => _tutorialStep >= 0 && _tutorialStep < TutorialSteps.Length && _session != null;

        private void StartTutorial()
        {
            GameSettings next = _settings.Clone();
            next.Mode = GameMode.Standard;
            next.Difficulty = Difficulty.Easy;
            next.Seed = TutorialSeed;
            next.CovenantId = null;
            next.CivilizationId = null;
            _startingTutorial = true;
            _bootstrapper.StartNewGame(next);
        }

        // Usados só pelo modo de captura automática de telas.
        internal void StartTutorialForCapture() => StartTutorial();

        internal void SetTutorialStepForCapture(int step)
        {
            _tutorialStep = step;
            EnterTutorialStep();
        }

        // Chamado quando uma partida é criada: só a iniciada pelo botão TUTORIAL fica guiada.
        private void ResetTutorialForNewSession()
        {
            _tutorialStep = _startingTutorial ? 0 : -1;
            _startingTutorial = false;
            EnterTutorialStep();
        }

        private void EndTutorial(bool completed)
        {
            _tutorialStep = -1;

            if (completed && !_settings.TutorialDone)
            {
                _settings.TutorialDone = true;
                SaveSettings();
            }
        }

        private void EnterTutorialStep()
        {
            if (_session == null)
            {
                return;
            }

            _tutorialStartTurn = _session.Turns.TurnNumber;
            _tutorialStartHand = _session.Deck.Hand.Count;
        }

        private void NextTutorialStep()
        {
            _tutorialStep++;

            if (_tutorialStep >= TutorialSteps.Length)
            {
                EndTutorial(completed: true);
                return;
            }

            EnterTutorialStep();
        }

        private Rect TutorialAreaRect(TutorialArea area, float width, float height)
        {
            switch (area)
            {
                case TutorialArea.TopBar:
                    return new Rect(4f, 2f, width - 8f, TopBarHeight);
                case TutorialArea.Hand:
                    return new Rect((width - 1040f) * 0.5f, height - HandCardHeight - 40f, 1040f, HandCardHeight + 36f);
                case TutorialArea.LeftPanel:
                    return Grow(_panelRects[PanelIdLeft], 6f);
                case TutorialArea.RightPanel:
                    return Grow(_panelRects[PanelIdRight], 6f);
                case TutorialArea.EndTurn:
                    return new Rect(width - 270f, height - 118f, 256f, 96f);
                case TutorialArea.Board:
                    return new Rect(width * 0.5f - 400f, TopBarHeight + 18f, 800f, height - TopBarHeight - HandAreaHeight - 30f);
                default:
                    return Rect.zero;
            }
        }

        private static Rect Grow(Rect rect, float by) => new Rect(rect.x - by, rect.y - by, rect.width + by * 2f, rect.height + by * 2f);

        private void DrawTutorial(float width, float height)
        {
            if (!TutorialActive || _uiScreen != UiScreen.Playing || _session.Turns.State == TurnState.GameOver)
            {
                return;
            }

            TutorialStep step = TutorialSteps[_tutorialStep];

            // Os passos de ação avançam quando o jogador faz o pedido.
            if (step.Advance == TutorialAdvance.PlayCard && _session.Deck.Hand.Count < _tutorialStartHand)
            {
                NextTutorialStep();
                return;
            }

            if (step.Advance == TutorialAdvance.EndTurn && _session.Turns.TurnNumber > _tutorialStartTurn)
            {
                NextTutorialStep();
                return;
            }

            Rect highlight = TutorialAreaRect(step.Area, width, height);

            if (highlight.width > 0f)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                Color gold = new Color(GuiTheme.Gold.r, GuiTheme.Gold.g, GuiTheme.Gold.b, pulse);
                _theme.Outline(highlight, gold, 4f);
            }

            // Painel de instruções no alto da tela, sem cobrir a mão nem os painéis laterais.
            const float panelWidth = 780f;
            var content = new GUIContent(step.Body);
            float bodyHeight = BodyStyle.CalcHeight(content, panelWidth - 60f);
            float panelHeight = 58f + bodyHeight + 78f;
            var panel = new Rect((width - panelWidth) * 0.5f, TopBarHeight + 14f, panelWidth, panelHeight);
            _theme.Panel(panel);

            _theme.ShadowText(new Rect(panel.x + 30f, panel.y + 12f, panel.width - 200f, 34f), step.Title, _theme.Title, GuiTheme.Gold);
            _theme.ShadowText(new Rect(panel.xMax - 170f, panel.y + 12f, 140f, 34f),
                $"Tutorial {_tutorialStep + 1}/{TutorialSteps.Length}", _theme.Small, GuiTheme.TextDim);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 52f, panel.width - 60f, bodyHeight), content, BodyStyle);

            float buttonsY = panel.yMax - 62f;
            bool lastStep = _tutorialStep == TutorialSteps.Length - 1;

            if (step.Advance == TutorialAdvance.Button)
            {
                if (MenuButton(new Rect(panel.xMax - 250f, buttonsY, 220f, 46f), lastStep ? "CONCLUIR" : "PRÓXIMO", primary: true))
                {
                    NextTutorialStep();
                }
            }
            else if (MenuButton(new Rect(panel.xMax - 250f, buttonsY, 220f, 46f), "PULAR PASSO"))
            {
                NextTutorialStep();
            }

            if (!lastStep && MenuButton(new Rect(panel.x + 30f, buttonsY, 260f, 46f), "PULAR TUTORIAL"))
            {
                EndTutorial(completed: true);
            }
        }
    }
}
