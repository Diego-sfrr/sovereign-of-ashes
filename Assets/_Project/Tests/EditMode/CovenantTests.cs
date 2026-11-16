using System;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Pactos: regras especiais escolhidas antes da partida.
    /// </summary>
    public sealed class CovenantTests
    {
        private static GameSessionConfig With(string id)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 9;
            CovenantCatalog.Find(id).Apply(config);
            return config;
        }

        [Test]
        public void Catalog_HasUniqueIds_AndEveryPactBuildsAPlayableGame()
        {
            Assert.AreEqual(CovenantCatalog.All.Count, CovenantCatalog.All.Select(c => c.Id).Distinct().Count());

            foreach (Covenant covenant in CovenantCatalog.All)
            {
                Assert.IsNotEmpty(covenant.Name);
                Assert.IsNotEmpty(covenant.Boon);
                Assert.IsNotEmpty(covenant.Curse);

                using (var session = new GameSession(With(covenant.Id)))
                {
                    session.Start();
                    int guard = 0;

                    while (session.Turns.State == TurnState.AwaitingPlayer && guard++ < 100)
                    {
                        session.Player.EndTurn();
                    }

                    Assert.AreEqual(TurnState.GameOver, session.Turns.State, covenant.Id);
                }
            }
        }

        [Test]
        public void PickThree_ReturnsThreeDistinctPacts_AndIsReproducible()
        {
            var first = CovenantCatalog.PickThree(new Random(5));
            var second = CovenantCatalog.PickThree(new Random(5));

            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(3, first.Select(c => c.Id).Distinct().Count());
            CollectionAssert.AreEqual(first.Select(c => c.Id), second.Select(c => c.Id));
        }

        [Test]
        public void Find_ReturnsNull_ForNothingOrUnknown()
        {
            Assert.IsNull(CovenantCatalog.Find(null));
            Assert.IsNull(CovenantCatalog.Find(string.Empty));
            Assert.IsNull(CovenantCatalog.Find("nao_existe"));
        }

        [Test]
        public void SacredSilence_AddsTwoSermons_AndShrinksTheFaithReserve()
        {
            GameSessionConfig baseline = DefaultContent.CreateConfig();
            GameSessionConfig pact = With("sacred_silence");

            Assert.AreEqual(baseline.StartingDeck.Count + 2, pact.StartingDeck.Count);
            Assert.AreEqual(baseline.Faith.Capacity - 2, pact.Faith.Capacity);
            Assert.LessOrEqual(pact.Faith.StartingFaith, pact.Faith.Capacity);
        }

        [Test]
        public void SteelOath_TradesCoreHealthForMilitaryPower()
        {
            GameSessionConfig baseline = DefaultContent.CreateConfig();
            GameSessionConfig pact = With("steel_oath");

            Assert.AreEqual(baseline.CoreMaxHealth - 6, pact.CoreMaxHealth);
            Assert.AreEqual(baseline.Civilizations[0].Stats.InitialMilitaryPower + 3, pact.Civilizations[0].Stats.InitialMilitaryPower);
        }

        [Test]
        public void WideHand_DrawsSixCards_ButEarnsLessFaith()
        {
            GameSessionConfig pact = With("wide_hand");

            Assert.AreEqual(6, pact.HandSize);
            Assert.AreEqual(1, pact.Faith.BaseIncomePerTurn);

            using (var session = new GameSession(pact))
            {
                session.Start();
                Assert.AreEqual(6, session.Deck.Hand.Count);
            }
        }

        [Test]
        public void AshDecree_BoostsFaithGain_ButRaisesRationalityGrowth()
        {
            GameSessionConfig baseline = DefaultContent.CreateConfig();
            baseline.Seed = 9;
            GameSessionConfig pact = With("ash_decree");

            using (var plain = new GameSession(baseline))
            using (var boosted = new GameSession(pact))
            {
                plain.Start();
                boosted.Start();

                Assert.Greater(boosted.Faith.Current, plain.Faith.Current);
                Assert.AreEqual(baseline.RationalityGrowthPerTurn + 1, pact.RationalityGrowthPerTurn);
            }
        }

        [Test]
        public void TurningWorld_RotatesTheOuterRingTwoStepsPerTurn_AndAddsTurnCards()
        {
            GameSessionConfig pact = With("turning_world");

            Assert.AreEqual(3, pact.StartingDeck.Count(c => c.Id == "divine_turn" || c.Id == "counter_turn"));

            var rotations = pact.TectonicPlanner.PlanShifts(1);
            Assert.AreEqual(2, rotations.First(r => r.Ring == RingType.OuterWilderness).Steps);
        }

        [Test]
        public void AlchemistPact_AddsThreeInfusionCards_AndCostsStartingFaith()
        {
            GameSessionConfig baseline = DefaultContent.CreateConfig();
            GameSessionConfig pact = With("alchemist_pact");

            Assert.AreEqual(baseline.StartingDeck.Count + 3, pact.StartingDeck.Count);
            Assert.AreEqual(baseline.Faith.StartingFaith - 2, pact.Faith.StartingFaith);
        }

        [Test]
        public void SettingsClone_KeepsTheChosenPact()
        {
            var settings = new GameSettings { CovenantId = "steel_oath" };
            Assert.AreEqual("steel_oath", settings.Clone().CovenantId);
        }
    }
}
