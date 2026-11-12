using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Alchemy.Effects;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Rules.Commandments;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns.Phases;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Testes dos efeitos de jogo da alquimia (Forja, Golem, Santo, Espectro, Cinzas, Altar).
    /// </summary>
    public sealed class ElementEffectTests
    {
        private static readonly SimpleInvaderDefinition Cultist = new SimpleInvaderDefinition("cultist", "Cultista", 5, 2, 2);
        private static readonly SimpleInvaderDefinition Colossus = new SimpleInvaderDefinition("colossus", "Colosso", 12, 4, 3);

        /// <summary>Mundo mínimo para testar efeitos sem a partida inteira.</summary>
        private sealed class World
        {
            public readonly TectonicBoard Board = new TectonicBoard(8);
            public readonly RuleModifierSystem Rules = new RuleModifierSystem();
            public readonly Combatant Core = new Combatant("Core", 30, 0);
            public readonly List<CivilizationEntity> Civilizations = new List<CivilizationEntity>();
            public readonly CombatResolver Combat;
            public readonly FaithPool Pool;
            public readonly FaithIncomeService Faith;
            public readonly InvaderRoster Invaders;
            public readonly AlchemySystem Alchemy;
            public readonly ElementEffectSystem Effects;
            public readonly List<ElementEffectReport> Reports = new List<ElementEffectReport>();

            public World()
            {
                var config = new FaithEconomyConfig(0, 50, 0, 0);
                Combat = new CombatResolver(Rules);
                Pool = config.CreatePool();
                Faith = new FaithIncomeService(Pool, config, Rules);
                Invaders = new InvaderRoster(Board);
                Alchemy = new AlchemySystem(DefaultContent.CreateRecipes(), new DefaultElementFactory());
                Effects = new ElementEffectSystem(
                    DefaultContent.CreateElementEffects(),
                    new ElementEffectServices(Board, Invaders, Combat, Core, Civilizations, Faith));
                Effects.EffectTriggered += Reports.Add;
            }

            public TectonicSector Sector(RingType ring, int position) => Board.GetSectorAt(ring, position);

            public SectorElement Fuse(TectonicSector sector, params ConceptType[] concepts)
            {
                foreach (ConceptType concept in concepts)
                {
                    Alchemy.ApplyConcept(sector, concept);
                }

                return (SectorElement)sector.Content;
            }

            public InvaderEntity Spawn(IInvaderDefinition definition, TectonicSector sector)
            {
                Assert.IsTrue(Invaders.TrySpawn(definition, sector, out InvaderEntity invader));
                return invader;
            }

            public void RunEffectsPhase() => new ElementEffectsPhase(Effects, Invaders).Execute(1);
        }

        [Test]
        public void Forge_BurnsSectorAndNeighbors_AndKillsGrantBounty()
        {
            var w = new World();
            var bounty = new InvaderBountyRewarder(w.Invaders, w.Faith);
            SectorElement forge = w.Fuse(w.Sector(RingType.InnerSanctuary, 2), ConceptType.Iron, ConceptType.Fire);
            Assert.AreEqual("sacred_forge", forge.Definition.Id);

            InvaderEntity onForge = w.Spawn(Cultist, w.Sector(RingType.InnerSanctuary, 2));
            InvaderEntity beside = w.Spawn(Cultist, w.Sector(RingType.InnerSanctuary, 3));
            InvaderEntity far = w.Spawn(Cultist, w.Sector(RingType.InnerSanctuary, 6));

            w.RunEffectsPhase();
            Assert.AreEqual(2, onForge.CurrentHealth); // 3 no setor
            Assert.AreEqual(4, beside.CurrentHealth);  // 1 no vizinho
            Assert.AreEqual(5, far.CurrentHealth);

            w.RunEffectsPhase(); // o do setor morre; a recompensa (2 de Fé) entra
            Assert.AreEqual(2, w.Invaders.Count);
            Assert.AreEqual(2, w.Pool.Current);
            Assert.IsTrue(w.Reports.All(r => r.IsBeneficial && r.Element.Id == "sacred_forge"));

            bounty.Dispose();
        }

        [Test]
        public void Golem_BlocksMarch_WearsDownAndCrumbles()
        {
            var w = new World();
            TectonicSector golemSector = w.Sector(RingType.MiddleCivilization, 0);
            SectorElement golem = w.Fuse(golemSector, ConceptType.Flesh, ConceptType.Iron);
            Assert.AreEqual("iron_golem", golem.Definition.Id);

            InvaderEntity brute = w.Spawn(Colossus, w.Sector(RingType.OuterWilderness, 0));
            var march = new InvaderAdvancePhase(w.Invaders, w.Board, w.Combat, w.Core, w.Effects);
            int blocked = 0;
            march.InvaderBlocked += _ => blocked++;

            march.Execute(1);
            march.Execute(2);
            Assert.AreEqual(RingType.OuterWilderness, brute.Sector.Ring); // ainda barrado
            Assert.IsFalse(golemSector.IsEmpty);

            march.Execute(3); // terceira investida: o Golem desmorona
            Assert.IsTrue(golemSector.IsEmpty);
            Assert.AreEqual(3, blocked);
            Assert.AreEqual(12 - 3 * 2, brute.CurrentHealth);
            Assert.AreEqual(2, w.Reports.Count(r => r.IsBeneficial));
            Assert.AreEqual(1, w.Reports.Count(r => !r.IsBeneficial));

            march.Execute(4); // sem barreira, passa
            Assert.AreEqual(RingType.MiddleCivilization, brute.Sector.Ring);
        }

        [Test]
        public void Golem_CounterDamageCanKillTheInvader()
        {
            var w = new World();
            w.Fuse(w.Sector(RingType.MiddleCivilization, 4), ConceptType.Flesh, ConceptType.Iron);
            var weak = new SimpleInvaderDefinition("imp", "Diabrete", 2, 1, 0);

            InvaderEntity imp = w.Spawn(weak, w.Sector(RingType.OuterWilderness, 4));
            var march = new InvaderAdvancePhase(w.Invaders, w.Board, w.Combat, w.Core, w.Effects);

            march.Execute(1); // 2 de dano de volta mata o diabrete (a fase remove os mortos)
            Assert.IsTrue(imp.IsDead);
            Assert.AreEqual(0, w.Invaders.Count);
        }

        [Test]
        public void Saint_CalmsRationality_Altar_TradesRationalityForFaith()
        {
            var calm = new World();
            var civ = new CivilizationEntity("Civ", new CivilizationStatsConfig(1, 10, 5, 20, 10, 3));
            calm.Civilizations.Add(civ);
            calm.Fuse(calm.Sector(RingType.MiddleCivilization, 1), ConceptType.Flesh, ConceptType.Faith);
            calm.RunEffectsPhase();
            Assert.AreEqual(4, civ.Rationality);

            var sacrifice = new World();
            var civ2 = new CivilizationEntity("Civ", new CivilizationStatsConfig(1, 10, 5, 20, 10, 3));
            sacrifice.Civilizations.Add(civ2);
            SectorElement altar = sacrifice.Fuse(sacrifice.Sector(RingType.MiddleCivilization, 1), ConceptType.Faith, ConceptType.Blood);
            Assert.AreEqual("blood_altar", altar.Definition.Id);
            sacrifice.RunEffectsPhase();
            Assert.AreEqual(2, sacrifice.Pool.Current);
            Assert.AreEqual(6, civ2.Rationality);
            Assert.IsTrue(sacrifice.Reports.Any(r => !r.IsBeneficial)); // o custo do Altar aparece como ruim
        }

        [Test]
        public void Wraith_DrainsNearbyInvadersAndHealsCore()
        {
            var w = new World();
            w.Core.TakeDamage(10);
            SectorElement wraith = w.Fuse(w.Sector(RingType.MiddleCivilization, 2), ConceptType.Blood, ConceptType.Fear);
            Assert.AreEqual("wraith", wraith.Definition.Id);

            InvaderEntity a = w.Spawn(Cultist, w.Sector(RingType.MiddleCivilization, 2));
            InvaderEntity b = w.Spawn(Cultist, w.Sector(RingType.MiddleCivilization, 3));
            InvaderEntity far = w.Spawn(Cultist, w.Sector(RingType.MiddleCivilization, 6));

            w.RunEffectsPhase();

            Assert.AreEqual(3, a.CurrentHealth);
            Assert.AreEqual(3, b.CurrentHealth);
            Assert.AreEqual(5, far.CurrentHealth);
            Assert.AreEqual(22, w.Core.CurrentHealth); // 20 + 1 por invasor atingido
        }

        [Test]
        public void Ashes_FaithGoesThroughCommandments()
        {
            var w = new World();
            w.Rules.AddCommandment(new FaithMultiplierCommandment("tithe", "Dízimo", 0, 300));
            SectorElement ashes = w.Fuse(w.Sector(RingType.OuterWilderness, 0), ConceptType.Fire, ConceptType.Time);
            Assert.AreEqual("ashes", ashes.Definition.Id);

            w.RunEffectsPhase();

            Assert.AreEqual(3, w.Pool.Current); // +1 de Fé x300%
        }

        [Test]
        public void ConceptsWithoutEffect_AreInert()
        {
            var w = new World();
            w.Fuse(w.Sector(RingType.InnerSanctuary, 0), ConceptType.Iron);
            InvaderEntity invader = w.Spawn(Cultist, w.Sector(RingType.InnerSanctuary, 0));

            w.RunEffectsPhase();

            Assert.AreEqual(5, invader.CurrentHealth);
            Assert.IsEmpty(w.Reports);
        }

        [Test]
        public void EveryDefaultFusionResultHasAnEffect_AndEveryConceptHasACard()
        {
            IAlchemyRecipeProvider provider = DefaultContent.CreateRecipes();
            IElementEffectProvider effects = DefaultContent.CreateElementEffects();
            var catalog = (IAlchemyRecipeCatalog)provider;

            Assert.AreEqual(6, catalog.Recipes.Count);
            Assert.AreEqual(7, catalog.BaseElements.Count);

            foreach (IAlchemyRecipe recipe in catalog.Recipes)
            {
                IReadOnlyList<ElementEffect> list = effects.GetEffects(recipe.Result);
                Assert.IsNotEmpty(list, $"{recipe.Result.Id} sem efeito");
                Assert.IsTrue(list.All(e => !string.IsNullOrWhiteSpace(e.Description)), $"{recipe.Result.Id} sem descrição");
            }

            // Sem uma carta para cada conceito, parte das receitas seria inalcançável.
            IReadOnlyList<ICardDefinition> deck = DefaultContent.CreateStarterDeck();

            foreach (ConceptType concept in (ConceptType[])System.Enum.GetValues(typeof(ConceptType)))
            {
                Assert.IsTrue(deck.Any(c => c.PrimaryConcept == concept && c.Effects.Any(e => e is ConceptInfusionEffect)),
                    $"nenhuma carta infunde {concept}");
            }
        }

        [Test]
        public void RecipeCatalog_ListsInRegistrationOrder_AndReplacementKeepsPosition()
        {
            var a = new SimpleElementDefinition("a", "A", ElementKind.Concept);
            var b = new SimpleElementDefinition("b", "B", ElementKind.Concept);
            var x = new SimpleElementDefinition("x", "X", ElementKind.Structure);
            var y = new SimpleElementDefinition("y", "Y", ElementKind.Structure);
            var provider = new InMemoryRecipeProvider()
                .AddRecipe(a, ConceptType.Fire, x)
                .AddRecipe(b, ConceptType.Iron, x)
                .AddRecipe(a, ConceptType.Fire, y); // substitui a primeira

            Assert.AreEqual(2, provider.Recipes.Count);
            Assert.AreEqual("y", provider.Recipes[0].Result.Id);
            Assert.AreEqual("x", provider.Recipes[1].Result.Id);
        }

        [Test]
        public void FullSession_RunsElementEffectsPhaseInTheTurn()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 9;

            using (var session = new GameSession(config))
            {
                Assert.IsTrue(session.Turns.Phases.Any(p => p is ElementEffectsPhase));
                session.Start();

                // Forja numa placa do Santuário, com um Colosso em cima: o turno completo a faz queimar.
                TectonicSector sector = session.Board.GetSectorAt(RingType.InnerSanctuary, 5);
                session.Alchemy.ApplyConcept(sector, ConceptType.Iron);
                session.Alchemy.ApplyConcept(sector, ConceptType.Fire);
                var colossus = new SimpleInvaderDefinition("c", "Colosso", 40, 0, 0);
                session.Invaders.TrySpawn(colossus, sector, out InvaderEntity invader);

                int triggered = 0;
                session.ElementEffects.EffectTriggered += _ => triggered++;
                session.Player.EndTurn();

                Assert.Greater(triggered, 0);
                Assert.Less(invader.CurrentHealth, 40);
            }
        }
    }
}
