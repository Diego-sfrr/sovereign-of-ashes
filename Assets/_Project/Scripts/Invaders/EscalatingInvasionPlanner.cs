using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Quanto os invasores ficam mais fortes com o passar dos turnos (a "pressão" de uma dificuldade).
    /// </summary>
    public readonly struct InvaderEscalation
    {
        /// <summary>Sem reforço: os invasores têm sempre a força da definição.</summary>
        public static readonly InvaderEscalation None = new InvaderEscalation(0, 0, 0);

        /// <summary>A cada quantos turnos sobe um patamar (0 = nunca).</summary>
        public int TurnsPerTier { get; }

        /// <summary>Vida somada por patamar.</summary>
        public int HealthPerTier { get; }

        /// <summary>A cada quantos patamares o ataque sobe 1 (0 = nunca).</summary>
        public int TiersPerAttack { get; }

        /// <summary>Cria o reforço.</summary>
        /// <param name="turnsPerTier">Turnos por patamar (0 = nunca reforça).</param>
        /// <param name="healthPerTier">Vida somada por patamar (não negativa).</param>
        /// <param name="tiersPerAttack">Patamares para +1 de ataque (0 = nunca).</param>
        public InvaderEscalation(int turnsPerTier, int healthPerTier, int tiersPerAttack)
        {
            TurnsPerTier = Math.Max(0, turnsPerTier);
            HealthPerTier = Math.Max(0, healthPerTier);
            TiersPerAttack = Math.Max(0, tiersPerAttack);
        }

        /// <summary>Patamar do turno.</summary>
        /// <param name="turnNumber">Turno (começa em 1).</param>
        /// <returns>0 no começo, subindo a cada <see cref="TurnsPerTier"/> turnos.</returns>
        public int TierFor(int turnNumber) => TurnsPerTier <= 0 || turnNumber < 1 ? 0 : turnNumber / TurnsPerTier;
    }

    /// <summary>
    /// Envolve outro planejador e reforça (vida e ataque) os invasores que ele manda surgir, quanto mais tarde o turno.
    /// Quem já nasceu mantém a força de quando nasceu; só os novos vêm mais duros. É assim que as dificuldades
    /// Normal e Difícil apertam o jogador sem entupir o tabuleiro de invasores.
    /// </summary>
    public sealed class EscalatingInvasionPlanner : IInvasionPlanner
    {
        private readonly IInvasionPlanner _inner;
        private readonly InvaderEscalation _escalation;
        private readonly Dictionary<string, IInvaderDefinition> _scaled = new Dictionary<string, IInvaderDefinition>();

        /// <summary>Cria o planejador.</summary>
        /// <param name="inner">Planejador original.</param>
        /// <param name="escalation">Quanto reforçar.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="inner"/> for nulo.</exception>
        public EscalatingInvasionPlanner(IInvasionPlanner inner, InvaderEscalation escalation)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _escalation = escalation;
        }

        /// <inheritdoc />
        public IReadOnlyList<InvaderSpawnOrder> PlanSpawns(int turnNumber)
        {
            IReadOnlyList<InvaderSpawnOrder> orders = _inner.PlanSpawns(turnNumber);
            int tier = _escalation.TierFor(turnNumber);

            if (tier == 0)
            {
                return orders;
            }

            var result = new List<InvaderSpawnOrder>(orders.Count);

            foreach (InvaderSpawnOrder order in orders)
            {
                result.Add(new InvaderSpawnOrder(Scale(order.Definition, tier), order.Count));
            }

            return result;
        }

        private IInvaderDefinition Scale(IInvaderDefinition definition, int tier)
        {
            string key = definition.Id + "#" + tier;

            if (_scaled.TryGetValue(key, out IInvaderDefinition cached))
            {
                return cached;
            }

            int attackBonus = _escalation.TiersPerAttack > 0 ? tier / _escalation.TiersPerAttack : 0;
            var scaled = new SimpleInvaderDefinition(definition.Id, definition.DisplayName,
                definition.MaxHealth + tier * _escalation.HealthPerTier,
                definition.Attack + attackBonus,
                definition.FaithBounty,
                definition.CorruptsElements(),
                definition.RationalityPerTurn(),
                definition.DamageReduction(),
                definition.HealsRing(),
                definition.IgnoresBarriers(),
                definition.ExplodeDamage(),
                definition.RotatesMiddleRing(),
                definition.IsBoss());

            _scaled[key] = scaled;
            return scaled;
        }
    }
}
