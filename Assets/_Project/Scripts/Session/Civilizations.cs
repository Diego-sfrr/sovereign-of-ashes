using System;
using System.Collections.Generic;
using System.Linq;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Uma civilização jogável, escolhida antes da partida. Define os números dela (Poder, Razão), cartas extras no
    /// baralho e uma habilidade passiva (feita por <see cref="GameSessionConfig.StartHooks"/>, ouvindo eventos da sessão).
    /// Só mexe na configuração: nenhuma regra do jogo é alterada.
    /// </summary>
    public sealed class CivilizationDefinition
    {
        private readonly Action<GameSessionConfig> _apply;

        /// <summary>Id estável.</summary>
        public string Id { get; }

        /// <summary>Nome exibido (também é o nome da civilização no jogo).</summary>
        public string Name { get; }

        /// <summary>Frase curta sobre o estilo de jogo.</summary>
        public string Style { get; }

        /// <summary>A vantagem (inclui a passiva).</summary>
        public string Boon { get; }

        /// <summary>O preço.</summary>
        public string Curse { get; }

        /// <summary>Poder Militar inicial (só para mostrar na escolha).</summary>
        public int MilitaryPower { get; }

        /// <summary>Conceitos favorecidos: cada Oferenda Divina traz ao menos uma carta deles.</summary>
        public IReadOnlyList<ConceptType> Niche { get; }

        /// <summary>Os conceitos favorecidos por extenso, como "Fogo e Fé".</summary>
        public string NicheLabel => string.Join(" e ", Niche.Select(DisplayNames.Concept));

        internal CivilizationDefinition(string id, string name, string style, string boon, string curse, int militaryPower,
            ConceptType[] niche, Action<GameSessionConfig> apply)
        {
            Niche = niche ?? new ConceptType[0];
            Id = id;
            Name = name;
            Style = style;
            Boon = boon;
            Curse = curse;
            MilitaryPower = militaryPower;
            _apply = apply;
        }

        /// <summary>
        /// Aplica a civilização a uma configuração (substitui a civilização padrão).
        /// </summary>
        /// <param name="config">Configuração a alterar.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="config"/> for nula.</exception>
        public void Apply(GameSessionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.RewardNiche.Clear();
            config.RewardNiche.AddRange(Niche);
            _apply(config);
        }
    }

    /// <summary>
    /// As civilizações do jogo.
    /// </summary>
    public static class CivilizationCatalog
    {
        /// <summary>Id da civilização padrão.</summary>
        public const string DefaultId = "ash_children";

        /// <summary>Todas as civilizações, na ordem de exibição.</summary>
        public static IReadOnlyList<CivilizationDefinition> All { get; } = new List<CivilizationDefinition>
        {
            new CivilizationDefinition(DefaultId, "Filhos da Cinza",
                "Equilibrados: sem surpresas, bons para aprender.",
                "Poder 5 e Razão que sobe devagar (1 por turno).", "Sem habilidade especial.", 5,
                new[] { ConceptType.Fire, ConceptType.Faith },
                config => Replace(config, "Filhos da Cinza", 5, 10, 0, 20, 10)),

            new CivilizationDefinition("abyss_smiths", "Forjadores do Abismo",
                "Muralha de ferro: estruturas e Golens no centro do jogo.",
                "Poder 8, começam com um Golem de Ferro no anel do meio e 3 cartas de Ferro e Carne a mais no baralho.",
                "Começam com 2 de Razão e a reserva de Fé guarda 2 cristais a menos.", 8,
                new[] { ConceptType.Iron, ConceptType.Flesh },
                config =>
                {
                    Replace(config, "Forjadores do Abismo", 8, 10, 2, 20, 10);
                    AddCards(config, "infuse_iron", 2);
                    AddCards(config, "infuse_flesh", 1);
                    ShrinkFaithReserve(config, 2);
                    config.StartHooks.Add(session =>
                    {
                        TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);
                        session.Alchemy.ApplyConcept(sector, ConceptType.Flesh);
                        session.Alchemy.ApplyConcept(sector, ConceptType.Iron);
                    });
                }),

            new CivilizationDefinition("eclipse_flagellants", "Flagelantes do Eclipse",
                "Sangue e medo: dor que vira força, com a fé sempre à beira do abismo.",
                "Cada carta de Sangue ou Medo que você joga cura 2 de vida do Núcleo. Mais 3 cartas de Sangue e Medo no baralho.",
                "Poder 3 e a Razão sobe 2 por turno: a heresia chega rápido.", 3,
                new[] { ConceptType.Blood, ConceptType.Fear },
                config =>
                {
                    Replace(config, "Flagelantes do Eclipse", 3, 10, 2, 20, 10);
                    config.RationalityGrowthPerTurn = 2;
                    AddCards(config, "infuse_blood", 2);
                    AddCards(config, "infuse_fear", 1);
                    config.StartHooks.Add(session => session.Cards.CardPlayed += (card, sector) =>
                    {
                        if (card.PrimaryConcept == ConceptType.Blood || card.PrimaryConcept == ConceptType.Fear)
                        {
                            session.Combat.Heal(session.DivineCore, 2, card);
                        }
                    });
                }),

            new CivilizationDefinition("time_hermits", "Eremitas do Tempo",
                "Mestres dos anéis: tempo e rotação a seu favor.",
                "Girar um anel (Intervenção Divina) custa 0 de Fé. A Razão só sobe nos turnos pares. 4 cartas de giro e Tempo no baralho.",
                "Poder 4 e começam com 1 de Fé a menos.", 4,
                new[] { ConceptType.Time },
                config =>
                {
                    Replace(config, "Eremitas do Tempo", 4, 10, 0, 20, 10);
                    config.RationalityGrowthPerTurn = 0;
                    AddCards(config, "divine_turn", 2);
                    AddCards(config, "counter_turn", 1);
                    AddCards(config, "infuse_time", 1);
                    FaithEconomyConfig f = config.Faith;
                    config.Faith = new FaithEconomyConfig(Math.Max(1, f.StartingFaith - 1), f.Capacity, f.BaseIncomePerTurn, f.CivilizationYieldPercent);
                    config.StartHooks.Add(session =>
                    {
                        session.RingControl.RotateCost = 0;
                        session.Turns.TurnEnded += turn =>
                        {
                            if (turn % 2 == 0)
                            {
                                foreach (CivilizationEntity civilization in session.Civilizations.ToList())
                                {
                                    civilization.ChangeRationality(1);
                                }
                            }
                        };
                    });
                })
        };

        /// <summary>
        /// Procura uma civilização pelo id.
        /// </summary>
        /// <param name="id">Id (pode ser nulo).</param>
        /// <returns>A civilização, ou <c>null</c>.</returns>
        public static CivilizationDefinition Find(string id) =>
            string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(c => c.Id == id);

        private static void Replace(GameSessionConfig config, string name, int power, int faith, int rationality, int maxFaith, int maxRationality)
        {
            config.Civilizations.Clear();
            config.Civilizations.Add(new CivilizationSetup(name,
                new CivilizationStatsConfig(power, faith, rationality, maxFaith, maxRationality, 3)));
        }

        private static void ShrinkFaithReserve(GameSessionConfig config, int by)
        {
            FaithEconomyConfig f = config.Faith;
            int capacity = Math.Max(4, f.Capacity - by);
            config.Faith = new FaithEconomyConfig(Math.Min(f.StartingFaith, capacity), capacity, f.BaseIncomePerTurn, f.CivilizationYieldPercent);
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
    }
}
