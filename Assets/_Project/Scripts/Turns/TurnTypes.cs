using System;

namespace SovereignOfAshes.Turns
{
    /// <summary>
    /// Estado do ciclo de turnos.
    /// </summary>
    public enum TurnState
    {
        /// <summary>A partida ainda não começou.</summary>
        NotStarted = 0,

        /// <summary>Fases automáticas estão executando.</summary>
        Running = 1,

        /// <summary>O jogo está parado esperando o jogador agir e encerrar o turno.</summary>
        AwaitingPlayer = 2,

        /// <summary>A partida terminou.</summary>
        GameOver = 3
    }

    /// <summary>
    /// Desfecho de uma partida encerrada.
    /// </summary>
    public enum GameOutcome
    {
        /// <summary>O jogador perdeu (ex.: Núcleo Divino destruído).</summary>
        Defeat = 0,

        /// <summary>O jogador venceu (ex.: sobreviveu às eras exigidas).</summary>
        Victory = 1
    }

    /// <summary>
    /// O que o <see cref="TurnSystem"/> deve fazer após uma fase.
    /// </summary>
    public enum PhaseResultKind
    {
        /// <summary>Seguir para a próxima fase.</summary>
        Continue = 0,

        /// <summary>Parar e esperar <see cref="TurnSystem.EndPlayerTurn"/>.</summary>
        AwaitPlayer = 1,

        /// <summary>Encerrar a partida.</summary>
        GameOver = 2
    }

    /// <summary>
    /// Resultado imutável de uma fase. Use as fábricas estáticas.
    /// </summary>
    public readonly struct PhaseResult
    {
        /// <summary>Tipo do resultado.</summary>
        public PhaseResultKind Kind { get; }

        /// <summary>Desfecho da partida (só vale quando <see cref="Kind"/> é <see cref="PhaseResultKind.GameOver"/>).</summary>
        public GameOutcome Outcome { get; }

        /// <summary>Motivo legível do fim de jogo.</summary>
        public string Reason { get; }

        private PhaseResult(PhaseResultKind kind, GameOutcome outcome, string reason)
        {
            Kind = kind;
            Outcome = outcome;
            Reason = reason;
        }

        /// <summary>Seguir para a próxima fase.</summary>
        public static PhaseResult Continue => new PhaseResult(PhaseResultKind.Continue, default, null);

        /// <summary>Esperar o jogador.</summary>
        public static PhaseResult AwaitPlayer => new PhaseResult(PhaseResultKind.AwaitPlayer, default, null);

        /// <summary>
        /// Encerrar a partida.
        /// </summary>
        /// <param name="outcome">Vitória ou derrota.</param>
        /// <param name="reason">Motivo exibido ao jogador.</param>
        /// <returns>Resultado de fim de jogo.</returns>
        public static PhaseResult EndGame(GameOutcome outcome, string reason) =>
            new PhaseResult(PhaseResultKind.GameOver, outcome, reason ?? string.Empty);
    }

    /// <summary>
    /// Uma fase do turno (Strategy). O <see cref="TurnSystem"/> executa as fases em ordem, a cada turno.
    /// </summary>
    public interface ITurnPhase
    {
        /// <summary>Nome da fase, para UI e logs.</summary>
        string Name { get; }

        /// <summary>
        /// Executa a fase.
        /// </summary>
        /// <param name="turnNumber">Número do turno atual (começa em 1).</param>
        /// <returns>O que fazer em seguida.</returns>
        PhaseResult Execute(int turnNumber);
    }

    /// <summary>
    /// Informações do fim de partida.
    /// </summary>
    public sealed class GameOverInfo
    {
        /// <summary>Vitória ou derrota.</summary>
        public GameOutcome Outcome { get; }

        /// <summary>Motivo.</summary>
        public string Reason { get; }

        /// <summary>Turno em que a partida terminou.</summary>
        public int TurnNumber { get; }

        /// <summary>Fase que encerrou a partida.</summary>
        public ITurnPhase Phase { get; }

        /// <summary>
        /// Cria as informações.
        /// </summary>
        /// <param name="outcome">Desfecho.</param>
        /// <param name="reason">Motivo.</param>
        /// <param name="turnNumber">Turno.</param>
        /// <param name="phase">Fase responsável.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="phase"/> for nula.</exception>
        internal GameOverInfo(GameOutcome outcome, string reason, int turnNumber, ITurnPhase phase)
        {
            Outcome = outcome;
            Reason = reason;
            TurnNumber = turnNumber;
            Phase = phase ?? throw new ArgumentNullException(nameof(phase));
        }
    }
}
