using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Ondas do modo infinito: nunca acabam e só pioram. Todo turno chegam Cultistas em número crescente,
    /// Colossos entram de dois em dois turnos a partir do 4, e a cada 5 turnos uma "orda" dobra os Cultistas.
    /// Profanadores, Arautos e um Inquisidor (chefe, de 10 em 10 turnos) completam a pressão.
    /// A cada 10 turnos todos ficam mais fortes (vida e ataque). O limite real é o anel externo, que lota.
    /// Determinístico: o mesmo turno sempre gera o mesmo plano.
    /// </summary>
    public sealed class EndlessInvasionPlanner : IInvasionPlanner
    {
        private const int TurnsPerTier = 10;
        private readonly Dictionary<int, IInvaderDefinition> _cultists = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _colossi = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _profaners = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _heralds = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _inquisitors = new Dictionary<int, IInvaderDefinition>();

        /// <summary>Quantos Cultistas surgem no turno (sem a orda).</summary>
        /// <param name="turnNumber">Turno (começa em 1).</param>
        /// <returns>Quantidade.</returns>
        public static int CultistsForTurn(int turnNumber) => turnNumber < 1 ? 0 : 1 + turnNumber / 2;

        /// <summary>Se o turno é de orda (Cultistas dobrados).</summary>
        /// <param name="turnNumber">Turno.</param>
        /// <returns>Verdadeiro a cada 5 turnos.</returns>
        public static bool IsHordeTurn(int turnNumber) => turnNumber >= 5 && turnNumber % 5 == 0;

        /// <summary>Quantos Colossos surgem no turno.</summary>
        /// <param name="turnNumber">Turno.</param>
        /// <returns>Zero antes do turno 4 e nos turnos ímpares.</returns>
        public static int ColossiForTurn(int turnNumber) => turnNumber >= 4 && turnNumber % 2 == 0 ? 1 + (turnNumber - 4) / 6 : 0;

        /// <inheritdoc />
        public IReadOnlyList<InvaderSpawnOrder> PlanSpawns(int turnNumber)
        {
            var orders = new List<InvaderSpawnOrder>();

            if (turnNumber < 1)
            {
                return orders;
            }

            int tier = turnNumber / TurnsPerTier;
            int cultists = CultistsForTurn(turnNumber) * (IsHordeTurn(turnNumber) ? 2 : 1);
            orders.Add(new InvaderSpawnOrder(Get(_cultists, tier, t =>
                new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", 5 + t * 2, 2 + t / 3, 1)), cultists));

            int colossi = ColossiForTurn(turnNumber);

            if (colossi > 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_colossi, tier, t =>
                    new SimpleInvaderDefinition("void_colossus", "Colosso do Vazio", 12 + t * 3, 4 + t / 2, 3)), colossi));
            }

            // Profanadores de 3 em 3 turnos desde o 6; Arautos de 5 em 5 desde o 9; um Inquisidor a cada 10 turnos.
            if (turnNumber >= 6 && (turnNumber - 6) % 3 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_profaners, tier, t =>
                    new SimpleInvaderDefinition("profaner", "Profanador", 4 + t, 1 + t / 3, 2, corruptsElements: true)), 1 + (turnNumber - 6) / 12));
            }

            if (turnNumber >= 9 && (turnNumber - 9) % 5 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_heralds, tier, t =>
                    new SimpleInvaderDefinition("heretic_herald", "Arauto Herege", 6 + t * 2, 0, 2, rationalityPerTurn: 1)), 1));
            }

            if (turnNumber >= 10 && turnNumber % 10 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_inquisitors, tier, t =>
                    new SimpleInvaderDefinition("inquisitor", "Inquisidor Cinzento", 20 + t * 8, 4 + t / 2, 5, rationalityPerTurn: 1 + t / 3)), 1));
            }

            return orders;
        }

        private static IInvaderDefinition Get(Dictionary<int, IInvaderDefinition> cache, int tier, Func<int, IInvaderDefinition> make)
        {
            if (!cache.TryGetValue(tier, out IInvaderDefinition definition))
            {
                definition = make(tier);
                cache[tier] = definition;
            }

            return definition;
        }
    }
}
