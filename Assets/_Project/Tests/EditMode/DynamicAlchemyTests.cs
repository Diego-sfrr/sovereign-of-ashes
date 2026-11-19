using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Achievements;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>A segunda leva de receitas: combinações novas, evoluções e os efeitos dinâmicos dos elementos.</summary>
    public sealed class DynamicAlchemyTests
    {
        private static GameSession NewSession(int coreHealth = 1000)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.CoreMaxHealth = coreHealth;
            config.RewardEveryNTurns = 0;
            config.Seed = 5;
            var session = new GameSession(config);
            session.Start();
            return session;
        }

        private static void Craft(GameSession session, TectonicSector sector, params ConceptType[] concepts)
        {
            foreach (ConceptType concept in concepts)
            {
                session.Alchemy.ApplyConcept(sector, concept);
            }
        }

        private static string ElementAt(TectonicSector sector) => (sector.Content as SectorElement)?.Definition.Id;

        [Test]
        public void TheGrimoire_HasNineteenRecipes_AndEveryResultHasEffectsAndASprite()
        {
            IAlchemyRecipeCatalog catalog = (IAlchemyRecipeCatalog)DefaultContent.CreateRecipes();
            var effects = DefaultContent.CreateElementEffects();

            Assert.AreEqual(19, catalog.Recipes.Count);
            Assert.AreEqual(19, catalog.Recipes.Select(r => r.Result.Id).Distinct().Count(), "dois resultados iguais");

            foreach (IAlchemyRecipe recipe in catalog.Recipes)
            {
                Assert.IsNotEmpty(effects.GetEffects(recipe.Result), $"{recipe.Result.Id} não faz nada");
                Assert.AreNotEqual(PixelSprites.Fallback, ArtCatalog.ForElement(recipe.Result.Id, recipe.Result.Kind), $"{recipe.Result.Id} sem sprite");
            }
        }

        [Test]
        public void MasterAlchemist_AsksForEveryRecipe()
        {
            int recipes = ((IAlchemyRecipeCatalog)DefaultContent.CreateRecipes()).Recipes.Count;
            Assert.AreEqual(recipes, AchievementCatalog.All.First(a => a.Id == "grand_alchemist").Goal);
        }

        [TestCase("fear_pyre", ConceptType.Fire, ConceptType.Fear)]
        [TestCase("crimson_executioner", ConceptType.Blood, ConceptType.Iron)]
        [TestCase("astrolabe", ConceptType.Time, ConceptType.Iron)]
        [TestCase("oracle", ConceptType.Time, ConceptType.Faith)]
        [TestCase("abomination", ConceptType.Fear, ConceptType.Flesh)]
        [TestCase("blood_reliquary", ConceptType.Blood, ConceptType.Time)]
        [TestCase("fear_mist", ConceptType.Fear, ConceptType.Time)]
        public void NewCombinations_CraftTheirElement(string expected, ConceptType first, ConceptType second)
        {
            using (GameSession session = NewSession())
            {
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);
                Craft(session, sector, first, second);
                Assert.AreEqual(expected, ElementAt(sector));
            }
        }

        [Test]
        public void Evolutions_UpgradeAnExistingElement()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector forge = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);
                Craft(session, forge, ConceptType.Iron, ConceptType.Fire, ConceptType.Time);
                Assert.AreEqual("eternal_forge", ElementAt(forge));

                TectonicSector golem = session.Board.GetSectorAt(RingType.MiddleCivilization, 2);
                Craft(session, golem, ConceptType.Flesh, ConceptType.Iron, ConceptType.Fire);
                Assert.AreEqual("magma_golem", ElementAt(golem));

                TectonicSector saint = session.Board.GetSectorAt(RingType.MiddleCivilization, 4);
                Craft(session, saint, ConceptType.Flesh, ConceptType.Faith, ConceptType.Blood);
                Assert.AreEqual("martyr", ElementAt(saint));

                TectonicSector ashes = session.Board.GetSectorAt(RingType.MiddleCivilization, 6);
                Craft(session, ashes, ConceptType.Fire, ConceptType.Time, ConceptType.Faith);
                Assert.AreEqual("phoenix", ElementAt(ashes));

                TectonicSector wraith = session.Board.GetSectorAt(RingType.InnerSanctuary, 1);
                Craft(session, wraith, ConceptType.Blood, ConceptType.Fear, ConceptType.Fear);
                Assert.AreEqual("grim_reaper", ElementAt(wraith));

                TectonicSector altar = session.Board.GetSectorAt(RingType.InnerSanctuary, 3);
                Craft(session, altar, ConceptType.Faith, ConceptType.Blood, ConceptType.Fear);
                Assert.AreEqual("profane_altar", ElementAt(altar));
            }
        }

        [Test]
        public void FearPyre_BurnsEveryInvaderOfItsRing_AndNoOtherRing()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector pyre = session.Board.GetSectorAt(RingType.OuterWilderness, 0);
                Craft(session, pyre, ConceptType.Fire, ConceptType.Fear);

                session.Invaders.TrySpawn(new SimpleInvaderDefinition("t", "T", 5, 1, 0), session.Board.GetSectorAt(RingType.OuterWilderness, 3), out InvaderEntity a);
                session.Invaders.TrySpawn(new SimpleInvaderDefinition("t", "T", 5, 1, 0), session.Board.GetSectorAt(RingType.OuterWilderness, 6), out InvaderEntity b);
                session.Invaders.TrySpawn(new SimpleInvaderDefinition("t", "T", 5, 1, 0), session.Board.GetSectorAt(RingType.MiddleCivilization, 3), out InvaderEntity other);

                session.ElementEffects.ProcessTurnEnd();

                Assert.AreEqual(4, a.CurrentHealth);
                Assert.AreEqual(4, b.CurrentHealth);
                Assert.AreEqual(5, other.CurrentHealth);
            }
        }

        [Test]
        public void BloodReliquary_HealsTheWoundedCore_ButNotAboveTheMaximum()
        {
            using (GameSession session = NewSession(coreHealth: 20))
            {
                Craft(session, session.Board.GetSectorAt(RingType.InnerSanctuary, 0), ConceptType.Blood, ConceptType.Time);
                session.Combat.DealDamage(session.DivineCore, 5);

                session.ElementEffects.ProcessTurnEnd();
                Assert.AreEqual(17, session.DivineCore.CurrentHealth);

                for (int i = 0; i < 5; i++)
                {
                    session.ElementEffects.ProcessTurnEnd();
                }

                Assert.AreEqual(20, session.DivineCore.CurrentHealth);
            }
        }

        [Test]
        public void FearMist_PushesNearbyInvadersOutward()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector mist = session.Board.GetSectorAt(RingType.MiddleCivilization, 2);
                Craft(session, mist, ConceptType.Fear, ConceptType.Time);
                session.Invaders.TrySpawn(new SimpleInvaderDefinition("t", "T", 5, 1, 0), mist, out InvaderEntity invader);

                // O invasor está em cima do elemento: a Névoa o empurra para o anel de fora (mesma posição).
                session.ElementEffects.ProcessTurnEnd();

                Assert.AreEqual(RingType.OuterWilderness, invader.Sector.Ring);
            }
        }

        [Test]
        public void Astrolabe_TurnsItsRingByItself_EveryTurn()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector astrolabe = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);
                Craft(session, astrolabe, ConceptType.Time, ConceptType.Iron);
                int before = session.Board.GetPositionOf(astrolabe);

                session.ElementEffects.ProcessTurnEnd();
                int after = session.Board.GetPositionOf(astrolabe);

                Assert.AreNotEqual(before, after);
                session.ElementEffects.ProcessTurnEnd();
                Assert.AreNotEqual(after, session.Board.GetPositionOf(astrolabe));
            }
        }

        [Test]
        public void ProfaneAltar_PaysManyFaith_ButRaisesRationality()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector altar = session.Board.GetSectorAt(RingType.InnerSanctuary, 3);
                Craft(session, altar, ConceptType.Faith, ConceptType.Blood, ConceptType.Fear);
                int rationality = session.Civilizations.Sum(c => c.Rationality);

                session.ElementEffects.ProcessTurnEnd();

                Assert.Greater(session.Civilizations.Sum(c => c.Rationality), rationality);
            }
        }
    }
}
