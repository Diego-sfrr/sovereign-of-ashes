using System;
using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Alchemy.Effects;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Uma civilização a criar no início da partida.
    /// </summary>
    public sealed class CivilizationSetup
    {
        /// <summary>Nome.</summary>
        public string Name { get; }

        /// <summary>Status iniciais e limites.</summary>
        public CivilizationStatsConfig Stats { get; }

        /// <summary>
        /// Cria a configuração.
        /// </summary>
        /// <param name="name">Nome (não vazio).</param>
        /// <param name="stats">Status.</param>
        /// <exception cref="ArgumentException">Se <paramref name="name"/> for vazio.</exception>
        /// <exception cref="ArgumentNullException">Se <paramref name="stats"/> for nulo.</exception>
        public CivilizationSetup(string name, CivilizationStatsConfig stats)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome não pode ser vazio.", nameof(name));
            }

            Name = name;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }
    }

    /// <summary>
    /// Tudo o que é preciso para montar uma partida (DTO de configuração).
    /// Comece de <see cref="DefaultContent.CreateConfig"/> e substitua só o que quiser.
    /// </summary>
    public sealed class GameSessionConfig
    {
        /// <summary>Setores por anel.</summary>
        public int SectorsPerRing { get; set; } = 8;

        /// <summary>Vida máxima do Núcleo Divino.</summary>
        public int CoreMaxHealth { get; set; } = 30;

        /// <summary>Receitas e elementos base da alquimia.</summary>
        public IAlchemyRecipeProvider Recipes { get; set; }

        /// <summary>
        /// Efeitos de jogo de cada elemento da alquimia. O padrão é vazio: os elementos só aparecem no tabuleiro.
        /// <see cref="DefaultContent.CreateConfig"/> já preenche os efeitos do conteúdo padrão.
        /// </summary>
        public IElementEffectProvider ElementEffects { get; set; } = new ElementEffectCatalog();

        /// <summary>Civilizações iniciais.</summary>
        public List<CivilizationSetup> Civilizations { get; } = new List<CivilizationSetup>();

        /// <summary>Mandamentos ativos desde o início.</summary>
        public List<ICommandment> Commandments { get; } = new List<ICommandment>();

        /// <summary>Economia de Fé.</summary>
        public FaithEconomyConfig Faith { get; set; }

        /// <summary>Rotações tectônicas.</summary>
        public ITectonicShiftPlanner TectonicPlanner { get; set; }

        /// <summary>Racionalidade ganha por turno por cada civilização.</summary>
        public int RationalityGrowthPerTurn { get; set; } = 1;

        /// <summary>Turnos a sobreviver para vencer (0 = sem vitória por sobrevivência).</summary>
        public int TurnsToSurvive { get; set; } = 20;

        /// <summary>Ondas de invasores.</summary>
        public IInvasionPlanner InvasionPlanner { get; set; }

        /// <summary>Baralho inicial.</summary>
        public List<ICardDefinition> StartingDeck { get; } = new List<ICardDefinition>();

        /// <summary>Cartas compradas por turno.</summary>
        public int HandSize { get; set; } = 5;

        /// <summary>Semente do embaralhamento e das posições de surgimento (0 = aleatória).</summary>
        public int Seed { get; set; }

        /// <summary>
        /// Garante que a configuração está completa.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se faltar algum dado obrigatório.</exception>
        public void Validate()
        {
            if (Recipes == null)
            {
                throw new InvalidOperationException($"{nameof(Recipes)} não foi definido.");
            }

            if (Faith == null)
            {
                throw new InvalidOperationException($"{nameof(Faith)} não foi definido.");
            }

            if (ElementEffects == null)
            {
                throw new InvalidOperationException($"{nameof(ElementEffects)} não pode ser nulo (use um catálogo vazio).");
            }

            if (TectonicPlanner == null)
            {
                throw new InvalidOperationException($"{nameof(TectonicPlanner)} não foi definido.");
            }

            if (InvasionPlanner == null)
            {
                throw new InvalidOperationException($"{nameof(InvasionPlanner)} não foi definido.");
            }

            if (StartingDeck.Count == 0)
            {
                throw new InvalidOperationException("O baralho inicial está vazio.");
            }

            if (HandSize <= 0 || CoreMaxHealth <= 0 || RationalityGrowthPerTurn < 0 || TurnsToSurvive < 0)
            {
                throw new InvalidOperationException("Valores numéricos da configuração estão fora do intervalo válido.");
            }
        }
    }
}
