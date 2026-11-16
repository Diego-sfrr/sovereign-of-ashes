using System;
using System.Collections.Generic;
using System.Linq;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Rules.Commandments;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Um Pacto: regra especial escolhida antes da partida. Toda vantagem tem um preço, e o pacto só mexe na
    /// <see cref="GameSessionConfig"/> (baralho, números iniciais, Mandamentos, giro dos anéis), nunca nas regras do jogo.
    /// </summary>
    public sealed class Covenant
    {
        private readonly Action<GameSessionConfig> _apply;

        /// <summary>Id estável (não é salvo, mas é usado para repetir a partida).</summary>
        public string Id { get; }

        /// <summary>Nome exibido.</summary>
        public string Name { get; }

        /// <summary>A vantagem.</summary>
        public string Boon { get; }

        /// <summary>O preço.</summary>
        public string Curse { get; }

        internal Covenant(string id, string name, string boon, string curse, Action<GameSessionConfig> apply)
        {
            Id = id;
            Name = name;
            Boon = boon;
            Curse = curse;
            _apply = apply;
        }

        /// <summary>
        /// Aplica o pacto a uma configuração.
        /// </summary>
        /// <param name="config">Configuração a alterar.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="config"/> for nula.</exception>
        public void Apply(GameSessionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _apply(config);
        }
    }

    /// <summary>
    /// Os Pactos do jogo.
    /// </summary>
    public static class CovenantCatalog
    {
        /// <summary>Todos os pactos.</summary>
        public static IReadOnlyList<Covenant> All { get; } = new List<Covenant>
        {
            new Covenant("ash_decree", "Decreto das Cinzas",
                "Todo ganho de Fé rende 50% a mais.", "A Racionalidade das civilizações sobe 1 a mais por turno.",
                config =>
                {
                    config.Commandments.Add(new FaithMultiplierCommandment("covenant_ash", "Decreto das Cinzas", 0, 150));
                    config.RationalityGrowthPerTurn += 1;
                }),
            new Covenant("sacred_silence", "Silêncio Sagrado",
                "2 Sermões a mais no baralho.", "A reserva de Fé guarda 2 cristais a menos.",
                config =>
                {
                    AddCards(config, "sermon", 2);
                    FaithEconomyConfig f = config.Faith;
                    config.Faith = new FaithEconomyConfig(Math.Min(f.StartingFaith, Math.Max(2, f.Capacity - 2)), Math.Max(4, f.Capacity - 2),
                        f.BaseIncomePerTurn, f.CivilizationYieldPercent);
                }),
            new Covenant("steel_oath", "Juramento de Aço",
                "As civilizações começam com +3 de Poder Militar.", "O Núcleo Divino tem 6 de vida a menos.",
                config =>
                {
                    config.CoreMaxHealth = Math.Max(6, config.CoreMaxHealth - 6);
                    ReplaceCivilizationStats(config, s => new CivilizationStatsConfig(
                        s.InitialMilitaryPower + 3, s.InitialFaith, s.InitialRationality, s.MaxFaith, s.MaxRationality, s.RedemptionThreshold));
                }),
            new Covenant("wide_hand", "Mão Larga",
                "Você compra 6 cartas por turno em vez de 5.", "A renda base de Fé cai em 1.",
                config =>
                {
                    config.HandSize += 1;
                    FaithEconomyConfig f = config.Faith;
                    config.Faith = new FaithEconomyConfig(f.StartingFaith, f.Capacity, Math.Max(0, f.BaseIncomePerTurn - 1), f.CivilizationYieldPercent);
                }),
            new Covenant("alchemist_pact", "Pacto Alquímico",
                "Mais 3 cartas Infundir (Ferro, Fogo e Carne) no baralho.", "Você começa com 2 de Fé a menos.",
                config =>
                {
                    AddCards(config, "infuse_iron", 1);
                    AddCards(config, "infuse_fire", 1);
                    AddCards(config, "infuse_flesh", 1);
                    FaithEconomyConfig f = config.Faith;
                    config.Faith = new FaithEconomyConfig(Math.Max(1, f.StartingFaith - 2), f.Capacity, f.BaseIncomePerTurn, f.CivilizationYieldPercent);
                }),
            new Covenant("turning_world", "Mundo em Rotação",
                "3 cartas de giro de anel no baralho (2 Giro Divino e 1 Contragiro).", "Os Ermos Externos giram 2 casas por turno em vez de 1.",
                config =>
                {
                    AddCards(config, "divine_turn", 2);
                    AddCards(config, "counter_turn", 1);
                    config.TectonicPlanner = new PatternTectonicShiftPlanner(new[]
                    {
                        new RingShiftRule(new RingRotation(RingType.OuterWilderness, 2, RotationDirection.Clockwise), 1),
                        new RingShiftRule(new RingRotation(RingType.InnerSanctuary, 1, RotationDirection.CounterClockwise), 3)
                    });
                }),
            new Covenant("blood_hunger", "Fome de Sangue",
                "2 Dízimos de Sangue e 1 Milagre no baralho.", "O Núcleo Divino tem 4 de vida a menos.",
                config =>
                {
                    config.CoreMaxHealth = Math.Max(6, config.CoreMaxHealth - 4);
                    AddCards(config, "blood_tithe", 2);
                    AddCards(config, "miracle", 1);
                })
        };

        /// <summary>
        /// Procura um pacto pelo id.
        /// </summary>
        /// <param name="id">Id (pode ser nulo).</param>
        /// <returns>O pacto, ou <c>null</c> se não existir.</returns>
        public static Covenant Find(string id) =>
            string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(c => c.Id == id);

        /// <summary>
        /// Sorteia três pactos diferentes para o jogador escolher.
        /// </summary>
        /// <param name="random">Sorteio (use semente fixa para repetir as opções).</param>
        /// <returns>Três pactos distintos.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="random"/> for nulo.</exception>
        public static IReadOnlyList<Covenant> PickThree(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var pool = new List<Covenant>(All);
            var picks = new List<Covenant>();

            while (picks.Count < 3 && pool.Count > 0)
            {
                int i = random.Next(pool.Count);
                picks.Add(pool[i]);
                pool.RemoveAt(i);
            }

            return picks;
        }

        private static void AddCards(GameSessionConfig config, string cardId, int copies)
        {
            ICardDefinition card = DefaultContent.CreateStarterDeck().Concat(DefaultContent.CreateRewardPool())
                .FirstOrDefault(c => c.Id == cardId);

            if (card == null)
            {
                throw new InvalidOperationException($"Carta '{cardId}' não existe no conteúdo padrão.");
            }

            for (int i = 0; i < copies; i++)
            {
                config.StartingDeck.Add(card);
            }
        }

        private static void ReplaceCivilizationStats(GameSessionConfig config, Func<CivilizationStatsConfig, CivilizationStatsConfig> change)
        {
            for (int i = 0; i < config.Civilizations.Count; i++)
            {
                CivilizationSetup setup = config.Civilizations[i];

                if (setup != null)
                {
                    config.Civilizations[i] = new CivilizationSetup(setup.Name, change(setup.Stats));
                }
            }
        }
    }
}
