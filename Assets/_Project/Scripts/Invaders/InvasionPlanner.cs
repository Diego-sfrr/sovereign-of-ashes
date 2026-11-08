using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Ordem de surgimento: quantos invasores de um tipo devem aparecer.
    /// </summary>
    public readonly struct InvaderSpawnOrder
    {
        /// <summary>Tipo do invasor.</summary>
        public IInvaderDefinition Definition { get; }

        /// <summary>Quantidade.</summary>
        public int Count { get; }

        /// <summary>
        /// Cria a ordem.
        /// </summary>
        /// <param name="definition">Tipo do invasor.</param>
        /// <param name="count">Quantidade.</param>
        public InvaderSpawnOrder(IInvaderDefinition definition, int count)
        {
            Definition = definition;
            Count = count;
        }
    }

    /// <summary>
    /// Decide quais invasores surgem em cada turno.
    /// Determinístico, para que a UI possa anunciar a próxima onda.
    /// </summary>
    public interface IInvasionPlanner
    {
        /// <summary>
        /// Invasores previstos para o turno.
        /// </summary>
        /// <param name="turnNumber">Turno (começa em 1).</param>
        /// <returns>Ordens de surgimento (pode ser vazia).</returns>
        IReadOnlyList<InvaderSpawnOrder> PlanSpawns(int turnNumber);
    }

    /// <summary>
    /// Uma onda: surge pela primeira vez em <see cref="FirstTurn"/> e, opcionalmente, se repete
    /// a cada <see cref="EveryNTurns"/> turnos, crescendo <see cref="CountGrowthPerRepeat"/> a cada repetição.
    /// </summary>
    public sealed class InvasionWave
    {
        /// <summary>Tipo do invasor.</summary>
        public IInvaderDefinition Definition { get; }

        /// <summary>Turno da primeira aparição (maior que zero).</summary>
        public int FirstTurn { get; }

        /// <summary>Quantidade na primeira aparição (maior que zero).</summary>
        public int BaseCount { get; }

        /// <summary>Período de repetição (0 = acontece uma vez só).</summary>
        public int EveryNTurns { get; }

        /// <summary>Invasores a mais em cada repetição (escalada de dificuldade).</summary>
        public int CountGrowthPerRepeat { get; }

        /// <summary>
        /// Cria a onda.
        /// </summary>
        /// <param name="definition">Tipo do invasor.</param>
        /// <param name="firstTurn">Primeiro turno (maior que zero).</param>
        /// <param name="baseCount">Quantidade inicial (maior que zero).</param>
        /// <param name="everyNTurns">Período (0 = uma vez).</param>
        /// <param name="countGrowthPerRepeat">Crescimento por repetição (não negativo).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="definition"/> for nula.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for inválido.</exception>
        public InvasionWave(IInvaderDefinition definition, int firstTurn, int baseCount, int everyNTurns, int countGrowthPerRepeat)
        {
            if (firstTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstTurn), firstTurn, "Deve ser maior que zero.");
            }

            if (baseCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCount), baseCount, "Deve ser maior que zero.");
            }

            if (everyNTurns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(everyNTurns), everyNTurns, "Não pode ser negativo.");
            }

            if (countGrowthPerRepeat < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countGrowthPerRepeat), countGrowthPerRepeat, "Não pode ser negativo.");
            }

            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            FirstTurn = firstTurn;
            BaseCount = baseCount;
            EveryNTurns = everyNTurns;
            CountGrowthPerRepeat = countGrowthPerRepeat;
        }

        /// <summary>
        /// Quantos invasores esta onda gera no turno.
        /// </summary>
        /// <param name="turnNumber">Turno.</param>
        /// <returns>Quantidade (0 se a onda não acontece no turno).</returns>
        public int CountForTurn(int turnNumber)
        {
            if (turnNumber < FirstTurn)
            {
                return 0;
            }

            if (turnNumber == FirstTurn)
            {
                return BaseCount;
            }

            if (EveryNTurns == 0 || (turnNumber - FirstTurn) % EveryNTurns != 0)
            {
                return 0;
            }

            long repeats = (turnNumber - FirstTurn) / EveryNTurns;
            return (int)Math.Min(int.MaxValue, BaseCount + repeats * CountGrowthPerRepeat);
        }
    }

    /// <summary>
    /// Planejador baseado em uma lista de ondas.
    /// </summary>
    public sealed class WaveInvasionPlanner : IInvasionPlanner
    {
        private readonly List<InvasionWave> _waves;

        /// <summary>
        /// Cria o planejador.
        /// </summary>
        /// <param name="waves">Ondas (nulas são ignoradas).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="waves"/> for nulo.</exception>
        public WaveInvasionPlanner(IEnumerable<InvasionWave> waves)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            _waves = new List<InvasionWave>();

            foreach (InvasionWave wave in waves)
            {
                if (wave != null)
                {
                    _waves.Add(wave);
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<InvaderSpawnOrder> PlanSpawns(int turnNumber)
        {
            var orders = new List<InvaderSpawnOrder>();

            foreach (InvasionWave wave in _waves)
            {
                int count = wave.CountForTurn(turnNumber);

                if (count > 0)
                {
                    orders.Add(new InvaderSpawnOrder(wave.Definition, count));
                }
            }

            return orders;
        }
    }
}
