using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Conteúdo padrão em código: permite jogar uma partida completa sem criar nenhum asset.
    /// Cada parte pode ser substituída por ScriptableObjects no <c>SovereignBootstrapper</c>.
    /// </summary>
    public static class DefaultContent
    {
        /// <summary>
        /// Cria uma configuração completa e jogável.
        /// </summary>
        /// <returns>Nova configuração (pode ser alterada livremente).</returns>
        public static GameSessionConfig CreateConfig()
        {
            var config = new GameSessionConfig
            {
                SectorsPerRing = 8,
                CoreMaxHealth = 30,
                Recipes = CreateRecipes(),
                Faith = new FaithEconomyConfig(startingFaith: 3, capacity: 10, baseIncomePerTurn: 2, civilizationYieldPercent: 20),
                TectonicPlanner = CreateTectonicPlanner(),
                RationalityGrowthPerTurn = 1,
                TurnsToSurvive = 20,
                InvasionPlanner = CreateInvasionPlanner(),
                HandSize = 5,
                Seed = 0
            };

            config.Civilizations.Add(CreateCivilization());
            config.StartingDeck.AddRange(CreateStarterDeck());
            return config;
        }

        /// <summary>
        /// Civilização inicial: os Filhos da Cinza.
        /// </summary>
        /// <returns>Configuração da civilização.</returns>
        public static CivilizationSetup CreateCivilization() =>
            new CivilizationSetup("Filhos da Cinza", new CivilizationStatsConfig(
                initialMilitaryPower: 5, initialFaith: 10, initialRationality: 0,
                maxFaith: 20, maxRationality: 10, redemptionThreshold: 3));

        /// <summary>
        /// Rotações padrão: Ermos giram 1 casa horária por turno; Santuário gira 1 anti-horária a cada 3 turnos.
        /// </summary>
        /// <returns>Planejador tectônico.</returns>
        public static ITectonicShiftPlanner CreateTectonicPlanner() =>
            new PatternTectonicShiftPlanner(new[]
            {
                new RingShiftRule(new RingRotation(RingType.OuterWilderness, 1, RotationDirection.Clockwise), 1),
                new RingShiftRule(new RingRotation(RingType.InnerSanctuary, 1, RotationDirection.CounterClockwise), 3)
            });

        /// <summary>
        /// Ondas padrão: Cultistas desde o turno 1 (crescendo) e Colossos a partir do turno 6.
        /// </summary>
        /// <returns>Planejador de invasão.</returns>
        public static IInvasionPlanner CreateInvasionPlanner()
        {
            var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", maxHealth: 5, attack: 2, faithBounty: 1);
            var colossus = new SimpleInvaderDefinition("void_colossus", "Colosso do Vazio", maxHealth: 12, attack: 4, faithBounty: 3);

            return new WaveInvasionPlanner(new[]
            {
                new InvasionWave(cultist, firstTurn: 1, baseCount: 1, everyNTurns: 3, countGrowthPerRepeat: 2),
                new InvasionWave(colossus, firstTurn: 5, baseCount: 1, everyNTurns: 3, countGrowthPerRepeat: 0)
            });
        }

        /// <summary>
        /// Elementos e receitas padrão da Alquimia Divina.
        /// </summary>
        /// <returns>Grimório em memória.</returns>
        public static IAlchemyRecipeProvider CreateRecipes()
        {
            var flesh = new SimpleElementDefinition("flesh", "Carne Viva", ElementKind.Concept);
            var fire = new SimpleElementDefinition("fire", "Chama", ElementKind.Concept);
            var iron = new SimpleElementDefinition("iron", "Ferro Bruto", ElementKind.Concept);
            var time = new SimpleElementDefinition("time", "Areia do Tempo", ElementKind.Concept);
            var faith = new SimpleElementDefinition("faith", "Relicário", ElementKind.Concept);
            var fear = new SimpleElementDefinition("fear", "Sombra", ElementKind.Concept);
            var blood = new SimpleElementDefinition("blood", "Sangue", ElementKind.Concept);

            var forge = new SimpleElementDefinition("sacred_forge", "Forja Sagrada", ElementKind.Structure);
            var golem = new SimpleElementDefinition("iron_golem", "Golem de Ferro", ElementKind.Entity);
            var saint = new SimpleElementDefinition("living_saint", "Santo Vivo", ElementKind.Entity);
            var wraith = new SimpleElementDefinition("wraith", "Espectro", ElementKind.Entity);
            var ashes = new SimpleElementDefinition("ashes", "Cinzas", ElementKind.Concept);
            var altar = new SimpleElementDefinition("blood_altar", "Altar de Sacrifício", ElementKind.Structure);

            return new InMemoryRecipeProvider()
                .AddBase(ConceptType.Flesh, flesh)
                .AddBase(ConceptType.Fire, fire)
                .AddBase(ConceptType.Iron, iron)
                .AddBase(ConceptType.Time, time)
                .AddBase(ConceptType.Faith, faith)
                .AddBase(ConceptType.Fear, fear)
                .AddBase(ConceptType.Blood, blood)
                .AddRecipe(iron, ConceptType.Fire, forge)
                .AddRecipe(flesh, ConceptType.Iron, golem)
                .AddRecipe(flesh, ConceptType.Faith, saint)
                .AddRecipe(blood, ConceptType.Fear, wraith)
                .AddRecipe(fire, ConceptType.Time, ashes)
                .AddRecipe(faith, ConceptType.Blood, altar);
        }

        /// <summary>
        /// Baralho inicial com 10 cartas.
        /// </summary>
        /// <returns>Cartas do baralho.</returns>
        public static IReadOnlyList<ICardDefinition> CreateStarterDeck()
        {
            var spear = new SimpleCardDefinition("fire_spear", "Lança de Fogo", "Causa 4 de dano a um invasor.",
                1, ConceptType.Fire, TargetRings.All, false, true, new ITileEffect[] { new InvaderStrikeEffect(4, false) });

            var storm = new SimpleCardDefinition("ash_storm", "Tempestade de Cinzas",
                "Causa 3 de dano ao invasor alvo e aos invasores dos setores vizinhos.",
                3, ConceptType.Fire, TargetRings.All, false, true, new ITileEffect[] { new InvaderStrikeEffect(3, true) });

            var sermon = new SimpleCardDefinition("sermon", "Sermão", "Todas as civilizações: -3 de Racionalidade.",
                1, ConceptType.Faith, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(-3, 0, 0) });

            var miracle = new SimpleCardDefinition("miracle", "Milagre", "Cura 6 de vida do Núcleo Divino.",
                2, ConceptType.Faith, TargetRings.None, false, false, new ITileEffect[] { new HealCoreEffect(6) });

            var recruit = new SimpleCardDefinition("recruit", "Recrutar Fiéis",
                "Todas as civilizações: +2 de Poder Militar e +1 de Racionalidade.",
                2, ConceptType.Iron, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(1, 0, 2) });

            var infuseIron = new SimpleCardDefinition("infuse_iron", "Infundir Ferro",
                "Manifesta Ferro em um setor. Ferro + Fogo = Forja Sagrada.",
                1, ConceptType.Iron, TargetRings.All, false, false, new ITileEffect[] { new ConceptInfusionEffect(ConceptType.Iron) });

            var infuseFire = new SimpleCardDefinition("infuse_fire", "Infundir Fogo",
                "Manifesta Fogo em um setor, ou o funde com o que estiver lá.",
                1, ConceptType.Fire, TargetRings.All, false, false, new ITileEffect[] { new ConceptInfusionEffect(ConceptType.Fire) });

            return new ICardDefinition[]
            {
                spear, spear, spear,
                storm,
                sermon, sermon,
                miracle,
                recruit,
                infuseIron,
                infuseFire
            };
        }
    }
}
