using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Habilidades dos invasores novos: Profanador (destrói elementos), Arauto e Inquisidor (empurram a Razão).
    /// </summary>
    public sealed class InvaderTraitsTests
    {
        private static GameSession NewSession()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 4;
            config.CoreMaxHealth = 1000;
            return new GameSession(config);
        }

        [Test]
        public void Profaner_DestroysTheElementOfItsSector_AndTheChronicleSaysSo()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 3);
                session.Alchemy.ApplyConcept(sector, ConceptType.Iron);
                Assert.IsFalse(sector.IsEmpty);
                var profaner = new SimpleInvaderDefinition("profaner", "Profanador", 4, 1, 2, corruptsElements: true);
                session.Invaders.TrySpawn(profaner, sector, out _);
                int reported = 0;
                session.TraitsPhase.ElementCorrupted += (i, s, e) => reported++;

                session.TraitsPhase.Execute(1);

                Assert.IsTrue(sector.IsEmpty);
                Assert.AreEqual(1, reported);
                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("profanou")));
            }
        }

        [Test]
        public void OrdinaryInvaders_LeaveElementsAlone()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 3);
                session.Alchemy.ApplyConcept(sector, ConceptType.Iron);
                session.Invaders.TrySpawn(new SimpleInvaderDefinition("c", "C", 5, 2, 1), sector, out _);

                session.TraitsPhase.Execute(1);

                Assert.IsFalse(sector.IsEmpty);
            }
        }

        [Test]
        public void Herald_PushesRationalityEveryTurn_UntilItDies()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                var civilization = session.Civilizations[0];
                int before = civilization.Rationality;
                var herald = new SimpleInvaderDefinition("heretic_herald", "Arauto", 6, 0, 2, rationalityPerTurn: 1);
                session.Invaders.TrySpawn(herald, session.Board.GetSectorAt(RingType.OuterWilderness, 1), out InvaderEntity entity);

                session.TraitsPhase.Execute(1);
                session.TraitsPhase.Execute(2);
                Assert.AreEqual(before + 2, civilization.Rationality);

                entity.TakeDamage(99);
                session.TraitsPhase.Execute(3);
                Assert.AreEqual(before + 2, civilization.Rationality);
            }
        }

        [Test]
        public void Traits_DescribeThemselves_AndDefaultToNone()
        {
            var profaner = new SimpleInvaderDefinition("p", "P", 4, 1, 2, corruptsElements: true);
            var inquisitor = new SimpleInvaderDefinition("i", "I", 20, 4, 5, rationalityPerTurn: 2);
            var plain = new SimpleInvaderDefinition("c", "C", 5, 2, 1);

            Assert.AreEqual("destrói elementos", profaner.DescribeTraits());
            Assert.AreEqual("+2 Razão por turno", inquisitor.DescribeTraits());
            Assert.AreEqual(string.Empty, plain.DescribeTraits());
            Assert.IsFalse(plain.CorruptsElements());
            Assert.AreEqual(0, plain.RationalityPerTurn());
        }

        [Test]
        public void DefaultWaves_BringTheNewInvadersAndABossAtTurnTen()
        {
            var planner = DefaultContent.CreateInvasionPlanner();

            Assert.IsTrue(planner.PlanSpawns(6).Any(o => o.Definition.Id == "profaner"));
            Assert.IsTrue(planner.PlanSpawns(9).Any(o => o.Definition.Id == "heretic_herald"));
            Assert.IsTrue(planner.PlanSpawns(10).Any(o => o.Definition.Id == "inquisitor"));
            Assert.IsFalse(planner.PlanSpawns(3).Any(o => o.Definition.Id == "profaner" || o.Definition.Id == "inquisitor"));
        }

        [Test]
        public void EndlessPlanner_BossesGrowStrongerEveryTier()
        {
            var planner = new EndlessInvasionPlanner();
            IInvaderDefinition first = planner.PlanSpawns(10).First(o => o.Definition.Id == "inquisitor").Definition;
            IInvaderDefinition later = planner.PlanSpawns(40).First(o => o.Definition.Id == "inquisitor").Definition;

            Assert.Greater(later.MaxHealth, first.MaxHealth);
            Assert.GreaterOrEqual(later.RationalityPerTurn(), first.RationalityPerTurn());
            Assert.IsTrue(planner.PlanSpawns(12).Any(o => o.Definition.Id == "profaner"));
        }

        [Test]
        public void AFullGame_WithTheNewInvaders_StillRunsToTheEnd()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                GameSessionConfig config = DefaultContent.CreateConfig();
                config.Seed = seed;

                using (var session = new GameSession(config))
                {
                    session.Start();
                    int guard = 0;

                    while (session.Turns.State == Turns.TurnState.AwaitingPlayer && guard++ < 100)
                    {
                        session.Player.EndTurn();
                    }

                    Assert.AreEqual(Turns.TurnState.GameOver, session.Turns.State, "seed " + seed);
                }
            }
        }
    }
}
