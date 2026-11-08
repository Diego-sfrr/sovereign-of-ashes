using System;
using System.Collections;
using System.IO;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Modo de captura automática para conferir a interface sem interação humana.
    /// Ativado pela linha de comando: <c>SovereignOfAshes.exe -sovereign-capture "C:\pasta"</c>.
    /// Joga alguns turnos sozinho (pela mesma API do jogador), salva prints em PNG e fecha o jogo.
    /// Não usa mouse nem teclado do sistema.
    /// </summary>
    public sealed class SovereignCaptureHarness : MonoBehaviour
    {
        /// <summary>Argumento de linha de comando que ativa o modo.</summary>
        public const string Argument = "-sovereign-capture";

        private const int MaxTurns = 8;

        private string _directory;
        private SovereignGameView _view;

        /// <summary>
        /// Adiciona o modo de captura ao objeto se o argumento estiver presente.
        /// </summary>
        /// <param name="host">Objeto que tem o <see cref="SovereignGameView"/>.</param>
        public static void AttachIfRequested(GameObject host)
        {
            string directory = ReadDirectory(Environment.GetCommandLineArgs());

            if (host == null || directory == null)
            {
                return;
            }

            var harness = host.AddComponent<SovereignCaptureHarness>();
            harness._directory = directory;
            harness._view = host.GetComponent<SovereignGameView>();
        }

        /// <summary>
        /// Lê a pasta de saída dos argumentos (o valor logo após <see cref="Argument"/>).
        /// </summary>
        /// <param name="args">Argumentos da linha de comando.</param>
        /// <returns>A pasta, ou <c>null</c> se o modo não foi pedido.</returns>
        public static string ReadDirectory(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], Argument, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private void Start()
        {
            Directory.CreateDirectory(_directory);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(2.5f);
            yield return Capture("01-inicio");

            GameSession session = _view != null ? _view.Session : null;

            if (session == null)
            {
                Application.Quit();
                yield break;
            }

            int targeted = FindTargetedCard(session);

            if (targeted >= 0)
            {
                _view.SelectCardForCapture(targeted);
                yield return new WaitForSeconds(0.6f);
                yield return Capture("02-escolhendo-alvo");
                _view.ClearSelection();
            }

            int shot = 3;

            for (int turn = 0; turn < MaxTurns && session.Turns.State == TurnState.AwaitingPlayer; turn++)
            {
                while (PlayOne(session))
                {
                    yield return new WaitForSeconds(0.3f);
                }

                if (turn == 2)
                {
                    yield return Capture($"{shot++:00}-cartas-jogadas");
                }

                session.Player.EndTurn();
                yield return new WaitForSeconds(0.9f);
                yield return Capture($"{shot++:00}-turno{session.Turns.TurnNumber}");
                yield return new WaitForSeconds(1.4f);
            }

            yield return Capture($"{shot:00}-final");
            Application.Quit();
        }

        private IEnumerator Capture(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(_directory, name + ".png"));
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        private static int FindTargetedCard(GameSession session)
        {
            for (int i = 0; i < session.Deck.Hand.Count; i++)
            {
                if (!session.Deck.Hand[i].IsGlobal() && session.Faith.CanAfford(session.Deck.Hand[i].Cost))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool PlayOne(GameSession session)
        {
            for (int i = 0; i < session.Deck.Hand.Count; i++)
            {
                if (session.Deck.Hand[i].IsGlobal())
                {
                    if (session.Player.PlayFromHand(i, null) == CardPlayOutcome.Played)
                    {
                        return true;
                    }

                    continue;
                }

                foreach (TectonicSector sector in session.Board.GetAllSectors())
                {
                    if (session.Player.Preview(i, sector) == CardPlayOutcome.Played)
                    {
                        return session.Player.PlayFromHand(i, sector) == CardPlayOutcome.Played;
                    }
                }
            }

            return false;
        }
    }
}
