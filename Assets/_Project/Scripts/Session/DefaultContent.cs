using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Alchemy.Effects;
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
                ElementEffects = CreateElementEffects(),
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
            config.RewardPool.AddRange(CreateRewardPool());
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
        /// Ondas padrão: Cultistas desde o turno 1 (crescendo), Colossos a partir do 5, Profanadores a partir do 6,
        /// Arautos Heréticos a partir do 9 e um Inquisidor (chefe) no turno 10.
        /// </summary>
        /// <param name="cultistGrowthPerRepeat">Cultistas a mais a cada repetição da onda (a dificuldade usa 1, 2 ou 3).</param>
        /// <returns>Planejador de invasão.</returns>
        public static IInvasionPlanner CreateInvasionPlanner(int cultistGrowthPerRepeat = 2)
        {
            var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", maxHealth: 5, attack: 2, faithBounty: 1);
            var colossus = new SimpleInvaderDefinition("void_colossus", "Colosso do Vazio", maxHealth: 12, attack: 4, faithBounty: 3);
            var profaner = new SimpleInvaderDefinition("profaner", "Profanador", maxHealth: 4, attack: 1, faithBounty: 2, corruptsElements: true);
            var herald = new SimpleInvaderDefinition("heretic_herald", "Arauto Herege", maxHealth: 6, attack: 0, faithBounty: 2, rationalityPerTurn: 1);
            var inquisitor = new SimpleInvaderDefinition("inquisitor", "Inquisidor Cinzento", maxHealth: 20, attack: 4, faithBounty: 5, rationalityPerTurn: 1);

            return new WaveInvasionPlanner(new[]
            {
                new InvasionWave(cultist, firstTurn: 1, baseCount: 1, everyNTurns: 3, countGrowthPerRepeat: cultistGrowthPerRepeat),
                new InvasionWave(colossus, firstTurn: 5, baseCount: 1, everyNTurns: 3, countGrowthPerRepeat: 0),
                new InvasionWave(profaner, firstTurn: 6, baseCount: 1, everyNTurns: 4, countGrowthPerRepeat: 0),
                new InvasionWave(herald, firstTurn: 9, baseCount: 1, everyNTurns: 6, countGrowthPerRepeat: 0),
                new InvasionWave(inquisitor, firstTurn: 10, baseCount: 1, everyNTurns: 10, countGrowthPerRepeat: 0)
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
        /// Efeitos de jogo dos elementos criados pelas fusões padrão.
        /// Os conceitos puros (Carne, Fogo, Ferro...) não têm efeito: são ingredientes.
        /// </summary>
        /// <returns>Catálogo de efeitos.</returns>
        public static IElementEffectProvider CreateElementEffects() =>
            new ElementEffectCatalog()
                .Add("sacred_forge", new BurnEffect(damageOnSector: 3, damageOnNeighbors: 1))
                .Add("iron_golem", new BarrierEffect(maxDurability: 3, counterDamage: 2))
                .Add("living_saint", new CivilizationPulseEffect(rationalityDelta: -1, militaryPowerDelta: 0, faithDelta: 0))
                .Add("wraith", new LifeDrainEffect(damage: 2, healPerHit: 1))
                .Add("ashes", new FaithSpringEffect(1))
                .Add("blood_altar", new FaithSpringEffect(2), new CivilizationPulseEffect(rationalityDelta: 1, militaryPowerDelta: 0, faithDelta: 0));

        /// <summary>
        /// Baralho inicial com 15 cartas.
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

            // Receitas são ordenadas: o que já está no setor + o conceito jogado. Os textos dizem "sobre X vira Y".
            var infuseIron = Infusion("infuse_iron", "Infundir Ferro", ConceptType.Iron,
                "Manifesta Ferro. Sobre Carne vira Golem de Ferro. Fogo depois: Forja.");
            var infuseFire = Infusion("infuse_fire", "Infundir Fogo", ConceptType.Fire,
                "Manifesta Fogo. Sobre Ferro vira Forja Sagrada. Tempo depois: Cinzas.");
            var infuseFlesh = Infusion("infuse_flesh", "Infundir Carne", ConceptType.Flesh,
                "Manifesta Carne. Ferro depois: Golem. Fé depois: Santo Vivo.");
            var infuseFaith = Infusion("infuse_faith", "Infundir Fé", ConceptType.Faith,
                "Manifesta Fé. Sobre Carne vira Santo Vivo. Sangue depois: Altar.");
            var infuseBlood = Infusion("infuse_blood", "Infundir Sangue", ConceptType.Blood,
                "Manifesta Sangue. Sobre Fé vira Altar. Medo depois: Espectro.");
            var infuseFear = Infusion("infuse_fear", "Infundir Medo", ConceptType.Fear,
                "Manifesta Medo. Sobre Sangue vira Espectro.");
            var infuseTime = Infusion("infuse_time", "Infundir Tempo", ConceptType.Time,
                "Manifesta Tempo. Sobre Fogo vira Cinzas, que rendem Fé.");

            return new ICardDefinition[]
            {
                spear, spear, spear,
                storm,
                sermon, sermon,
                miracle,
                recruit,
                infuseIron, infuseFire, infuseFlesh, infuseFaith, infuseBlood, infuseFear, infuseTime
            };
        }

        /// <summary>
        /// Cartas que podem surgir nas Oferendas Divinas. Misturam dano que já deixa um conceito no setor (mais
        /// alquimia sem "mão morta"), controle dos anéis e trocas de recursos.
        /// </summary>
        /// <returns>As cartas do pool de recompensas.</returns>
        public static IReadOnlyList<ICardDefinition> CreateRewardPool()
        {
            ITileEffect[] Strike(int damage, ConceptType concept) =>
                new ITileEffect[] { new InvaderStrikeEffect(damage, false), new ConceptInfusionEffect(concept) };

            return new ICardDefinition[]
            {
                new SimpleCardDefinition("iron_spear", "Lança de Ferro", "Causa 3 de dano a um invasor e infunde Ferro no setor.",
                    1, ConceptType.Iron, TargetRings.All, false, true, Strike(3, ConceptType.Iron)),
                new SimpleCardDefinition("holy_ember", "Brasa Sagrada", "Causa 2 de dano a um invasor e infunde Fogo no setor.",
                    1, ConceptType.Fire, TargetRings.All, false, true, Strike(2, ConceptType.Fire)),
                new SimpleCardDefinition("dread_howl", "Pavor", "Causa 2 de dano a um invasor e infunde Medo no setor.",
                    1, ConceptType.Fear, TargetRings.All, false, true, Strike(2, ConceptType.Fear)),
                new SimpleCardDefinition("blessed_blood", "Sangue Bento", "Cura 3 de vida do Núcleo e infunde Sangue no setor.",
                    1, ConceptType.Blood, TargetRings.All, false, false,
                    new ITileEffect[] { new HealCoreEffect(3), new ConceptInfusionEffect(ConceptType.Blood) }),
                new SimpleCardDefinition("divine_turn", "Giro Divino", "Gira o anel do setor 1 casa no sentido horário.",
                    1, ConceptType.Time, TargetRings.All, false, false,
                    new ITileEffect[] { new RotateRingEffect(1, RotationDirection.Clockwise) }),
                new SimpleCardDefinition("counter_turn", "Contragiro", "Gira o anel do setor 1 casa no sentido anti-horário.",
                    1, ConceptType.Time, TargetRings.All, false, false,
                    new ITileEffect[] { new RotateRingEffect(1, RotationDirection.CounterClockwise) }),
                new SimpleCardDefinition("exile", "Exílio", "Empurra o invasor do setor um anel para fora.",
                    2, ConceptType.Fear, TargetRings.All, false, true, new ITileEffect[] { new PushInvaderOutwardEffect() }),
                new SimpleCardDefinition("blood_tithe", "Dízimo de Sangue", "O Núcleo perde 2 de vida (nunca morre por isto) e você ganha 3 de Fé.",
                    0, ConceptType.Blood, TargetRings.None, false, false, new ITileEffect[] { new FaithTitheEffect(2, 3) }),
                new SimpleCardDefinition("penance", "Penitência", "Todas as civilizações: -5 de Racionalidade.",
                    2, ConceptType.Faith, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(-5, 0, 0) }),
                new SimpleCardDefinition("iron_oath", "Juramento de Ferro", "Todas as civilizações: +3 de Poder Militar.",
                    2, ConceptType.Iron, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(0, 0, 3) })
            };
        }

        private static SimpleCardDefinition Infusion(string id, string name, ConceptType concept, string description) =>
            new SimpleCardDefinition(id, name, description, 1, concept, TargetRings.All, false, false,
                new ITileEffect[] { new ConceptInfusionEffect(concept) });
    }
}
