using System.Collections.Generic;
using System.Linq;
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
        public static IInvasionPlanner CreateInvasionPlanner(int cultistGrowthPerRepeat = 2) =>
            CreateInvasionPlanner(new InvasionPressure(cultistGrowthPerRepeat, 3, 3, 4, InvaderEscalation.None));

        /// <summary>
        /// Ondas padrão com a pressão de uma dificuldade: ritmo das ondas e reforço dos invasores com o passar dos turnos.
        /// </summary>
        /// <param name="pressure">Ritmo e reforço.</param>
        /// <returns>Planejador de invasão.</returns>
        public static IInvasionPlanner CreateInvasionPlanner(InvasionPressure pressure)
        {
            var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", maxHealth: 5, attack: 2, faithBounty: 1);
            var colossus = new SimpleInvaderDefinition("void_colossus", "Colosso do Vazio", maxHealth: 12, attack: 4, faithBounty: 3);
            var profaner = new SimpleInvaderDefinition("profaner", "Profanador", maxHealth: 4, attack: 1, faithBounty: 2, corruptsElements: true);
            var herald = new SimpleInvaderDefinition("heretic_herald", "Arauto Herege", maxHealth: 6, attack: 0, faithBounty: 2, rationalityPerTurn: 1);

            var waves = new List<InvasionWave>
            {
                new InvasionWave(cultist, firstTurn: 1, baseCount: 1, everyNTurns: pressure.CultistEvery, countGrowthPerRepeat: pressure.CultistGrowth),
                new InvasionWave(colossus, firstTurn: 5, baseCount: 1, everyNTurns: pressure.ColossusEvery, countGrowthPerRepeat: 0),
                new InvasionWave(profaner, firstTurn: 6, baseCount: 1, everyNTurns: pressure.ProfanerEvery, countGrowthPerRepeat: 0),
                new InvasionWave(herald, firstTurn: 9, baseCount: 1, everyNTurns: 6, countGrowthPerRepeat: 0),

                // O chefe do meio da partida: um dos três, sorteado por partida.
                new InvasionWave(CreateMidBoss(pressure.MidBoss), firstTurn: 10, baseCount: 1, everyNTurns: 10, countGrowthPerRepeat: 0)
            };

            if (pressure.ExtraInvaders)
            {
                // Invasores com habilidade: cada um ataca uma parte diferente da estratégia do jogador.
                waves.Add(new InvasionWave(CreateGuardian(), firstTurn: 7, baseCount: 1, everyNTurns: 6, countGrowthPerRepeat: 0));
                waves.Add(new InvasionWave(CreateRunner(), firstTurn: 8, baseCount: 1, everyNTurns: 5, countGrowthPerRepeat: 0));
                waves.Add(new InvasionWave(CreateNecroApostle(), firstTurn: 11, baseCount: 1, everyNTurns: 7, countGrowthPerRepeat: 0));
                waves.Add(new InvasionWave(CreateScourge(), firstTurn: 12, baseCount: 1, everyNTurns: 5, countGrowthPerRepeat: 0));
            }

            if (pressure.FinalBossHealth > 0)
            {
                // O chefe final chega no turno 15 (fecho do Ato III) e alcança o Núcleo antes do turno 20.
                waves.Add(new InvasionWave(CreateFinalBoss(pressure.FinalBossHealth), firstTurn: 15, baseCount: 1, everyNTurns: 0, countGrowthPerRepeat: 0));
            }

            var planner = new WaveInvasionPlanner(waves);
            return pressure.Escalation.TurnsPerTier > 0 ? new EscalatingInvasionPlanner(planner, pressure.Escalation) : (IInvasionPlanner)planner;
        }

        /// <summary>Quantos chefes diferentes podem aparecer no turno 10.</summary>
        public const int MidBossCount = 3;

        /// <summary>
        /// Chefe do turno 10: 0 = Inquisidor Cinzento (rende Razão), 1 = Profeta do Tempo (gira o anel do meio),
        /// 2 = Colosso Carmesim (destrói elementos).
        /// </summary>
        /// <param name="index">Número do chefe (qualquer valor é reduzido ao intervalo válido).</param>
        /// <returns>Definição do chefe.</returns>
        public static SimpleInvaderDefinition CreateMidBoss(int index)
        {
            switch (((index % MidBossCount) + MidBossCount) % MidBossCount)
            {
                case 1:
                    return new SimpleInvaderDefinition("time_prophet", "Profeta do Tempo", maxHealth: 26, attack: 3, faithBounty: 5,
                        rotatesMiddleRing: true, isBoss: true);
                case 2:
                    return new SimpleInvaderDefinition("crimson_colossus", "Colosso Carmesim", maxHealth: 32, attack: 4, faithBounty: 5,
                        corruptsElements: true, isBoss: true);
                default:
                    return new SimpleInvaderDefinition("inquisitor", "Inquisidor Cinzento", maxHealth: 20, attack: 4, faithBounty: 5,
                        rationalityPerTurn: 1, isBoss: true);
            }
        }

        /// <summary>Guardião Profano: cada golpe contra ele perde 2 de dano (mínimo 1). Pede ataques fortes.</summary>
        /// <returns>Definição.</returns>
        public static SimpleInvaderDefinition CreateGuardian() =>
            new SimpleInvaderDefinition("profane_guardian", "Guardião Profano", maxHealth: 10, attack: 1, faithBounty: 2, damageReduction: 2);

        /// <summary>Corredor Herege: ignora barreiras (o Golem não o segura). Pede dano antes que ele chegue.</summary>
        /// <returns>Definição.</returns>
        public static SimpleInvaderDefinition CreateRunner() =>
            new SimpleInvaderDefinition("heretic_runner", "Corredor Herege", maxHealth: 4, attack: 2, faithBounty: 2, ignoresBarriers: true);

        /// <summary>Necroapóstolo: cura 2 de vida de todos os outros invasores do anel a cada turno. Mate-o primeiro.</summary>
        /// <returns>Definição.</returns>
        public static SimpleInvaderDefinition CreateNecroApostle() =>
            new SimpleInvaderDefinition("necro_apostle", "Necroapóstolo", maxHealth: 8, attack: 1, faithBounty: 3, healsRing: 2);

        /// <summary>Flagelo Explosivo: ao morrer fere os vizinhos e destrói o elemento do setor. Mate-o onde não importa.</summary>
        /// <returns>Definição.</returns>
        public static SimpleInvaderDefinition CreateScourge() =>
            new SimpleInvaderDefinition("blast_scourge", "Flagelo Explosivo", maxHealth: 6, attack: 3, faithBounty: 2, explodeDamage: 2);

        /// <summary>
        /// Rei Herege, chefe final: muita vida, golpe pesado e +2 de Razão por turno nas civilizações.
        /// </summary>
        /// <param name="health">Vida (a dificuldade escolhe).</param>
        /// <returns>Definição.</returns>
        public static SimpleInvaderDefinition CreateFinalBoss(int health) =>
            new SimpleInvaderDefinition("heretic_king", "Rei Herege", maxHealth: health, attack: health >= 40 ? 5 : 4, faithBounty: 8,
                rationalityPerTurn: 2, isBoss: true);

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

            var pyre = new SimpleElementDefinition("fear_pyre", "Pira do Terror", ElementKind.Structure);
            var executioner = new SimpleElementDefinition("crimson_executioner", "Carrasco Carmesim", ElementKind.Entity);
            var astrolabe = new SimpleElementDefinition("astrolabe", "Astrolábio", ElementKind.Structure);
            var oracle = new SimpleElementDefinition("oracle", "Oráculo", ElementKind.Entity);
            var abomination = new SimpleElementDefinition("abomination", "Aberração", ElementKind.Entity);
            var reliquary = new SimpleElementDefinition("blood_reliquary", "Relicário de Sangue", ElementKind.Structure);
            var mist = new SimpleElementDefinition("fear_mist", "Névoa do Medo", ElementKind.Structure);
            var eternalForge = new SimpleElementDefinition("eternal_forge", "Forja Eterna", ElementKind.Structure);
            var magmaGolem = new SimpleElementDefinition("magma_golem", "Golem de Magma", ElementKind.Entity);
            var martyr = new SimpleElementDefinition("martyr", "Mártir", ElementKind.Entity);
            var phoenix = new SimpleElementDefinition("phoenix", "Fênix", ElementKind.Entity);
            var reaper = new SimpleElementDefinition("grim_reaper", "Ceifador", ElementKind.Entity);
            var profaneAltar = new SimpleElementDefinition("profane_altar", "Altar Profano", ElementKind.Structure);

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
                .AddRecipe(faith, ConceptType.Blood, altar)

                // Segunda leva: combinações novas de conceitos...
                .AddRecipe(fire, ConceptType.Fear, pyre)
                .AddRecipe(blood, ConceptType.Iron, executioner)
                .AddRecipe(time, ConceptType.Iron, astrolabe)
                .AddRecipe(time, ConceptType.Faith, oracle)
                .AddRecipe(fear, ConceptType.Flesh, abomination)
                .AddRecipe(blood, ConceptType.Time, reliquary)
                .AddRecipe(fear, ConceptType.Time, mist)

                // ...e evoluções: um elemento já criado + um conceito vira algo maior.
                .AddRecipe(forge, ConceptType.Time, eternalForge)
                .AddRecipe(golem, ConceptType.Fire, magmaGolem)
                .AddRecipe(saint, ConceptType.Blood, martyr)
                .AddRecipe(ashes, ConceptType.Faith, phoenix)
                .AddRecipe(wraith, ConceptType.Fear, reaper)
                .AddRecipe(altar, ConceptType.Fear, profaneAltar);
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
                .Add("blood_altar", new FaithSpringEffect(2), new CivilizationPulseEffect(rationalityDelta: 1, militaryPowerDelta: 0, faithDelta: 0))

                // Segunda leva: cada elemento faz algo diferente (varrer o anel, curar, repelir, girar sozinho, render Fé).
                .Add("fear_pyre", new RingBurnEffect(1))
                .Add("crimson_executioner", new LifeDrainEffect(damage: 4, healPerHit: 2))
                .Add("astrolabe", new RotationDriftEffect(RotationDirection.Clockwise))
                .Add("oracle", new FaithSpringEffect(1), new CivilizationPulseEffect(rationalityDelta: 0, militaryPowerDelta: 1, faithDelta: 0))
                .Add("abomination", new BarrierEffect(maxDurability: 2, counterDamage: 4))
                .Add("blood_reliquary", new CoreHealEffect(2))
                .Add("fear_mist", new RepelEffect())
                .Add("eternal_forge", new BurnEffect(damageOnSector: 5, damageOnNeighbors: 2))
                .Add("magma_golem", new BarrierEffect(maxDurability: 4, counterDamage: 3), new BurnEffect(damageOnSector: 0, damageOnNeighbors: 2))
                .Add("martyr", new CoreHealEffect(2), new CivilizationPulseEffect(rationalityDelta: -2, militaryPowerDelta: 0, faithDelta: 0))
                .Add("phoenix", new FaithSpringEffect(3), new CoreHealEffect(1))
                .Add("grim_reaper", new LifeDrainEffect(damage: 5, healPerHit: 2))
                .Add("profane_altar", new FaithSpringEffect(4), new CivilizationPulseEffect(rationalityDelta: 2, militaryPowerDelta: 0, faithDelta: 0));

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
            }.Concat(CreateNicheCards()).ToList();
        }

        /// <summary>
        /// Cartas de nicho: cada uma recompensa um jeito específico de jogar (anti-horda, alquimia em massa, sacrifício
        /// de sangue, execução, rotação, anti-heresia e economia). Entram nas Oferendas junto com as demais.
        /// </summary>
        /// <returns>As cartas de nicho.</returns>
        public static IReadOnlyList<ICardDefinition> CreateNicheCards() => new ICardDefinition[]
        {
            // Fogo: varrer hordas e preparar fusões em massa.
            new SimpleCardDefinition("ash_tide", "Maré de Cinzas", "Causa 3 de dano a todos os invasores do anel do setor alvo.",
                3, ConceptType.Fire, TargetRings.All, false, true, new ITileEffect[] { new RingStrikeEffect(3) }),
            new SimpleCardDefinition("wall_of_flame", "Muro de Chamas", "Infunde Fogo no setor alvo e em todos os vizinhos.",
                2, ConceptType.Fire, TargetRings.All, false, false, new ITileEffect[] { new NeighborInfusionEffect(ConceptType.Fire, true) }),

            // Ferro e Carne: estruturas, Golens e Poder Militar.
            new SimpleCardDefinition("foundry", "Fundição", "Infunde Ferro no setor alvo e em todos os vizinhos.",
                2, ConceptType.Iron, TargetRings.All, false, false, new ITileEffect[] { new NeighborInfusionEffect(ConceptType.Iron, true) }),
            new SimpleCardDefinition("flesh_bloom", "Carne Florescente", "Infunde Carne no setor alvo e em todos os vizinhos.",
                2, ConceptType.Flesh, TargetRings.All, false, false, new ITileEffect[] { new NeighborInfusionEffect(ConceptType.Flesh, true) }),
            new SimpleCardDefinition("crusade", "Cruzada", "Todas as civilizações: +5 de Poder Militar e +2 de Racionalidade.",
                3, ConceptType.Iron, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(2, 0, 5) }),

            // Sangue: pagar com a vida do Núcleo por poder.
            new SimpleCardDefinition("bloodletting", "Sangria", "O Núcleo perde 3 de vida (nunca morre por isto) e você causa 7 de dano a um invasor.",
                1, ConceptType.Blood, TargetRings.All, false, true, new ITileEffect[] { new FaithTitheEffect(3, 0), new InvaderStrikeEffect(7, false) }),
            new SimpleCardDefinition("leech_strike", "Golpe Sanguessuga", "Causa 3 de dano a um invasor e cura 3 de vida do Núcleo.",
                2, ConceptType.Blood, TargetRings.All, false, true, new ITileEffect[] { new InvaderStrikeEffect(3, false), new HealCoreEffect(3) }),

            // Medo: execução e controle.
            new SimpleCardDefinition("verdict", "Veredito", "Mata na hora um invasor com 6 ou menos de vida; se tiver mais, causa 3 de dano.",
                2, ConceptType.Fear, TargetRings.All, false, true, new ITileEffect[] { new ExecuteEffect(6, 3) }),
            new SimpleCardDefinition("rout", "Debandada", "Causa 2 de dano a um invasor e o empurra um anel para fora.",
                2, ConceptType.Fear, TargetRings.All, false, true, new ITileEffect[] { new InvaderStrikeEffect(2, false), new PushInvaderOutwardEffect() }),

            // Tempo: rotações que reposicionam o tabuleiro.
            new SimpleCardDefinition("double_turn", "Giro Duplo", "Gira o anel do setor 2 casas no sentido horário.",
                2, ConceptType.Time, TargetRings.All, false, false, new ITileEffect[] { new RotateRingEffect(2, RotationDirection.Clockwise) }),
            new SimpleCardDefinition("pendulum", "Pêndulo", "Causa 2 de dano a um invasor e gira o anel dele 1 casa no sentido horário.",
                1, ConceptType.Time, TargetRings.All, false, true,
                new ITileEffect[] { new InvaderStrikeEffect(2, false), new RotateRingEffect(1, RotationDirection.Clockwise) }),

            // Fé: acalmar a heresia e render mais Fé.
            new SimpleCardDefinition("confession", "Confissão", "Todas as civilizações: -2 de Racionalidade. Cura 2 de vida do Núcleo.",
                1, ConceptType.Faith, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(-2, 0, 0), new HealCoreEffect(2) }),
            new SimpleCardDefinition("harvest", "Colheita Sagrada", "Todas as civilizações: +6 de Fé, que rende mais Fé a cada turno.",
                2, ConceptType.Faith, TargetRings.None, false, false, new ITileEffect[] { new CivilizationInfluenceEffect(0, 6, 0) })
        };

        private static SimpleCardDefinition Infusion(string id, string name, ConceptType concept, string description) =>
            new SimpleCardDefinition(id, name, description, 1, concept, TargetRings.All, false, false,
                new ITileEffect[] { new ConceptInfusionEffect(concept) });
    }

    /// <summary>Ritmo das ondas, reforço e chefes que uma dificuldade aplica.</summary>
    public readonly struct InvasionPressure
    {
        /// <summary>Cultistas a mais a cada repetição da onda.</summary>
        public int CultistGrowth { get; }

        /// <summary>De quantos em quantos turnos chega a onda de Cultistas.</summary>
        public int CultistEvery { get; }

        /// <summary>De quantos em quantos turnos chegam Colossos (a partir do 5).</summary>
        public int ColossusEvery { get; }

        /// <summary>De quantos em quantos turnos chegam Profanadores (a partir do 6).</summary>
        public int ProfanerEvery { get; }

        /// <summary>Reforço dos invasores novos com o passar dos turnos.</summary>
        public InvaderEscalation Escalation { get; }

        /// <summary>Se os invasores com habilidade (Guardião, Corredor, Necroapóstolo, Flagelo) entram nas ondas.</summary>
        public bool ExtraInvaders { get; }

        /// <summary>Vida do Rei Herege, o chefe final do turno 15 (0 = sem chefe final).</summary>
        public int FinalBossHealth { get; }

        /// <summary>Qual chefe aparece no turno 10 (0 Inquisidor, 1 Profeta do Tempo, 2 Colosso Carmesim).</summary>
        public int MidBoss { get; }

        /// <summary>Cria a pressão.</summary>
        public InvasionPressure(int cultistGrowth, int cultistEvery, int colossusEvery, int profanerEvery, InvaderEscalation escalation,
            bool extraInvaders = false, int finalBossHealth = 0, int midBoss = 0)
        {
            CultistGrowth = cultistGrowth;
            CultistEvery = cultistEvery;
            ColossusEvery = colossusEvery;
            ProfanerEvery = profanerEvery;
            Escalation = escalation;
            ExtraInvaders = extraInvaders;
            FinalBossHealth = finalBossHealth;
            MidBoss = midBoss;
        }

        /// <summary>Mesma pressão com outro chefe no turno 10.</summary>
        /// <param name="midBoss">Número do chefe.</param>
        /// <returns>Cópia com o chefe trocado.</returns>
        public InvasionPressure WithMidBoss(int midBoss) =>
            new InvasionPressure(CultistGrowth, CultistEvery, ColossusEvery, ProfanerEvery, Escalation, ExtraInvaders, FinalBossHealth, midBoss);
    }
}
