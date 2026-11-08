using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;

namespace SovereignOfAshes.Turns
{
    /// <summary>
    /// Decide quais anéis giram no fim de cada turno.
    /// Deve ser determinístico para que a UI possa mostrar a rotação do próximo turno antes de acontecer.
    /// </summary>
    public interface ITectonicShiftPlanner
    {
        /// <summary>
        /// Rotações previstas para o turno.
        /// </summary>
        /// <param name="turnNumber">Turno (começa em 1).</param>
        /// <returns>Rotações em ordem (pode ser vazia).</returns>
        IReadOnlyList<RingRotation> PlanShifts(int turnNumber);
    }

    /// <summary>
    /// Regra de rotação periódica de um anel: gira <see cref="Rotation"/> a cada <see cref="EveryNTurns"/> turnos.
    /// </summary>
    public sealed class RingShiftRule
    {
        /// <summary>Rotação aplicada.</summary>
        public RingRotation Rotation { get; }

        /// <summary>Período em turnos (1 = todo turno).</summary>
        public int EveryNTurns { get; }

        /// <summary>
        /// Cria a regra.
        /// </summary>
        /// <param name="rotation">Rotação aplicada.</param>
        /// <param name="everyNTurns">Período (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se os valores forem inválidos.</exception>
        public RingShiftRule(RingRotation rotation, int everyNTurns)
        {
            if (everyNTurns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(everyNTurns), everyNTurns, "Deve ser maior que zero.");
            }

            if (rotation.Steps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), rotation.Steps, "Os passos não podem ser negativos.");
            }

            Rotation = rotation;
            EveryNTurns = everyNTurns;
        }

        /// <summary>
        /// Indica se a regra dispara no turno.
        /// </summary>
        /// <param name="turnNumber">Turno.</param>
        /// <returns><c>true</c> se o anel gira neste turno.</returns>
        public bool TriggersOn(int turnNumber) => turnNumber > 0 && turnNumber % EveryNTurns == 0;
    }

    /// <summary>
    /// Planejador baseado em regras periódicas fixas (ex.: anel externo gira 1 casa horária todo turno,
    /// anel interno gira 2 anti-horárias a cada 3 turnos).
    /// </summary>
    public sealed class PatternTectonicShiftPlanner : ITectonicShiftPlanner
    {
        private readonly List<RingShiftRule> _rules;

        /// <summary>
        /// Cria o planejador.
        /// </summary>
        /// <param name="rules">Regras (nulas são ignoradas).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="rules"/> for nulo.</exception>
        public PatternTectonicShiftPlanner(IEnumerable<RingShiftRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _rules = new List<RingShiftRule>();

            foreach (RingShiftRule rule in rules)
            {
                if (rule != null)
                {
                    _rules.Add(rule);
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<RingRotation> PlanShifts(int turnNumber)
        {
            var plan = new List<RingRotation>();

            foreach (RingShiftRule rule in _rules)
            {
                if (rule.TriggersOn(turnNumber))
                {
                    plan.Add(rule.Rotation);
                }
            }

            return plan;
        }
    }
}
