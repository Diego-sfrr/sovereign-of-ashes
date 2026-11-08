using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Turns
{
    /// <summary>
    /// Ciclo de turnos: executa as fases em ordem, para quando uma fase pede o jogador
    /// (<see cref="PhaseResultKind.AwaitPlayer"/>) e retoma em <see cref="EndPlayerTurn"/>.
    /// Ao terminar a última fase, começa o próximo turno na primeira.
    /// <para>
    /// O sistema não conhece nenhuma regra de jogo: tudo vive nas fases (OCP).
    /// Ordem sugerida: Renda de Fé → Jogador → Rotação Tectônica → Civilizações → Fim de Jogo.
    /// </para>
    /// </summary>
    public sealed class TurnSystem
    {
        private readonly ITurnPhase[] _phases;
        private int _phaseIndex;
        private bool _isRunning;

        /// <summary>Disparado no início de cada turno. Parâmetro: número do turno.</summary>
        public event Action<int> TurnStarted;

        /// <summary>Disparado antes de cada fase. Parâmetros: fase, turno.</summary>
        public event Action<ITurnPhase, int> PhaseStarted;

        /// <summary>Disparado quando o jogo passa a esperar o jogador. Parâmetro: turno.</summary>
        public event Action<int> AwaitingPlayer;

        /// <summary>Disparado ao fim de cada turno completo. Parâmetro: número do turno encerrado.</summary>
        public event Action<int> TurnEnded;

        /// <summary>Disparado quando a partida termina.</summary>
        public event Action<GameOverInfo> GameOver;

        /// <summary>Estado atual.</summary>
        public TurnState State { get; private set; } = TurnState.NotStarted;

        /// <summary>Turno atual (0 antes de a partida começar).</summary>
        public int TurnNumber { get; private set; }

        /// <summary>Fase em execução ou aguardando, ou <c>null</c> antes do início.</summary>
        public ITurnPhase CurrentPhase => State == TurnState.NotStarted ? null : _phases[_phaseIndex];

        /// <summary>Informações do fim da partida, ou <c>null</c> se ela não terminou.</summary>
        public GameOverInfo Result { get; private set; }

        /// <summary>Fases na ordem de execução.</summary>
        public IReadOnlyList<ITurnPhase> Phases => Array.AsReadOnly(_phases);

        /// <summary>Atalho: indica se o jogador pode agir agora (jogar cartas).</summary>
        public bool IsPlayerTurn => State == TurnState.AwaitingPlayer;

        /// <summary>
        /// Cria o sistema.
        /// </summary>
        /// <param name="phases">Fases em ordem (ao menos uma; nenhuma nula).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="phases"/> for nulo.</exception>
        /// <exception cref="ArgumentException">Se a lista for vazia ou tiver fases nulas.</exception>
        public TurnSystem(IEnumerable<ITurnPhase> phases)
        {
            if (phases == null)
            {
                throw new ArgumentNullException(nameof(phases));
            }

            _phases = new List<ITurnPhase>(phases).ToArray();

            if (_phases.Length == 0)
            {
                throw new ArgumentException("O turno precisa de pelo menos uma fase.", nameof(phases));
            }

            if (Array.IndexOf(_phases, null) >= 0)
            {
                throw new ArgumentException("A lista de fases não pode conter nulos.", nameof(phases));
            }
        }

        /// <summary>
        /// Começa a partida no turno 1 e executa as fases até precisar do jogador ou a partida terminar.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se a partida já começou.</exception>
        public void StartGame()
        {
            if (State != TurnState.NotStarted)
            {
                throw new InvalidOperationException("A partida já foi iniciada.");
            }

            TurnNumber = 1;
            _phaseIndex = 0;
            State = TurnState.Running;
            TurnStarted?.Invoke(TurnNumber);
            RunUntilBlocked();
        }

        /// <summary>
        /// O jogador encerra sua fase; o sistema retoma as fases automáticas.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se o jogo não estiver esperando o jogador, ou se chamado de dentro de uma fase.</exception>
        public void EndPlayerTurn()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("EndPlayerTurn não pode ser chamado de dentro de uma fase.");
            }

            if (State != TurnState.AwaitingPlayer)
            {
                throw new InvalidOperationException($"Não é a vez do jogador (estado atual: {State}).");
            }

            State = TurnState.Running;
            AdvanceIndex();
            RunUntilBlocked();
        }

        private void RunUntilBlocked()
        {
            _isRunning = true;

            try
            {
                // Proteção contra loop infinito: um turno inteiro sem nenhuma fase de jogador.
                int executedWithoutWaiting = 0;

                while (State == TurnState.Running)
                {
                    if (executedWithoutWaiting > _phases.Length)
                    {
                        throw new InvalidOperationException(
                            "Um turno completo passou sem fase de jogador. Adicione uma fase que retorne AwaitPlayer.");
                    }

                    ITurnPhase phase = _phases[_phaseIndex];
                    PhaseStarted?.Invoke(phase, TurnNumber);
                    PhaseResult result = phase.Execute(TurnNumber);
                    executedWithoutWaiting++;

                    switch (result.Kind)
                    {
                        case PhaseResultKind.AwaitPlayer:
                            State = TurnState.AwaitingPlayer;
                            AwaitingPlayer?.Invoke(TurnNumber);
                            return;

                        case PhaseResultKind.GameOver:
                            State = TurnState.GameOver;
                            Result = new GameOverInfo(result.Outcome, result.Reason, TurnNumber, phase);
                            GameOver?.Invoke(Result);
                            return;

                        default:
                            AdvanceIndex();
                            break;
                    }
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        private void AdvanceIndex()
        {
            _phaseIndex++;

            if (_phaseIndex < _phases.Length)
            {
                return;
            }

            TurnEnded?.Invoke(TurnNumber);
            TurnNumber++;
            _phaseIndex = 0;
            TurnStarted?.Invoke(TurnNumber);
        }
    }
}
