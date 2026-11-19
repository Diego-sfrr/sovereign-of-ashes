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
        private readonly Dictionary<int, IInvaderDefinition> _guardians = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _runners = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _scourges = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _necros = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _prophets = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _crimsons = new Dictionary<int, IInvaderDefinition>();
        private readonly Dictionary<int, IInvaderDefinition> _kings = new Dictionary<int, IInvaderDefinition>();

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
                    new SimpleInvaderDefinition("inquisitor", "Inquisidor Cinzento", 20 + t * 8, 4 + t / 2, 5, rationalityPerTurn: 1 + t / 3, isBoss: true)), 1));
            }

            // Invasores com habilidade e chefes extras: a variedade cresce junto com a pressão.
            if (turnNumber >= 7 && (turnNumber - 7) % 4 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_guardians, tier, t =>
                    new SimpleInvaderDefinition("profane_guardian", "Guardião Profano", 10 + t * 2, 1 + t / 3, 2, damageReduction: 2 + t / 4)), 1 + (turnNumber - 7) / 16));
            }

            if (turnNumber >= 8 && (turnNumber - 8) % 3 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_runners, tier, t =>
                    new SimpleInvaderDefinition("heretic_runner", "Corredor Herege", 4 + t, 2 + t / 3, 2, ignoresBarriers: true)), 1 + (turnNumber - 8) / 12));
            }

            if (turnNumber >= 11 && (turnNumber - 11) % 4 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_scourges, tier, t =>
                    new SimpleInvaderDefinition("blast_scourge", "Flagelo Explosivo", 6 + t, 3 + t / 3, 2, explodeDamage: 2 + t / 3)), 1 + (turnNumber - 11) / 12));
            }

            if (turnNumber >= 12 && (turnNumber - 12) % 6 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_necros, tier, t =>
                    new SimpleInvaderDefinition("necro_apostle", "Necroapóstolo", 8 + t * 2, 1 + t / 3, 3, healsRing: 2 + t / 3)), 1));
            }

            if (turnNumber >= 20 && turnNumber % 20 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_prophets, tier, t =>
                    new SimpleInvaderDefinition("time_prophet", "Profeta do Tempo", 26 + t * 8, 3 + t / 2, 5, rotatesMiddleRing: true, isBoss: true)), 1));
            }

            if (turnNumber >= 25 && (turnNumber - 25) % 20 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_crimsons, tier, t =>
                    new SimpleInvaderDefinition("crimson_colossus", "Colosso Carmesim", 32 + t * 8, 4 + t / 2, 5, corruptsElements: true, isBoss: true)), 1));
            }

            if (turnNumber >= 30 && turnNumber % 30 == 0)
            {
                orders.Add(new InvaderSpawnOrder(Get(_kings, tier, t =>
                    new SimpleInvaderDefinition("heretic_king", "Rei Herege", 40 + t * 10, 5 + t / 2, 8, rationalityPerTurn: 2 + t / 4, isBoss: true)), 1));
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
