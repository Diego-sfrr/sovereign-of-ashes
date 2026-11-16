using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Achievements;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Core;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Civilizações jogáveis e Intervenção Divina (girar e congelar anéis).
    /// </summary>
    public sealed class CivilizationAndRingTests
    {
        private static GameSessionConfig With(string civilizationId, int seed = 21)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = seed;
            config.RewardEveryNTurns = 0; // sem oferenda atrapalhando os testes de turno
            config.CoreMaxHealth = 1000;
            CivilizationCatalog.Find(civilizationId)?.Apply(config);
            return config;
        }

        private static GameSession Session(string civilizationId = null)
        {
            var session = new GameSession(With(civilizationId));
            session.Start();
            session.Faith.Add(10);
            return session;
        }

        // ------------------------------------------------------------------ Civilizações

        [Test]
        public void Catalog_HasFourUniqueCivilizations_AndEachBuildsAPlayableGame()
        {
            Assert.AreEqual(4, CivilizationCatalog.All.Count);
            Assert.AreEqual(4, CivilizationCatalog.All.Select(c => c.Id).Distinct().Count());
            Assert.IsNotNull(CivilizationCatalog.Find(CivilizationCatalog.DefaultId));
            Assert.IsNull(CivilizationCatalog.Find(null));
            Assert.IsNull(CivilizationCatalog.Find("nao_existe"));

            foreach (CivilizationDefinition civilization in CivilizationCatalog.All)
            {
                using (var session = new GameSession(With(civilization.Id)))
                {
                    session.Start();
                    int guard = 0;

                    while (session.Turns.State == TurnState.AwaitingPlayer && guard++ < 100)
                    {
                        session.Player.EndTurn();
                    }

                    Assert.AreEqual(TurnState.GameOver, session.Turns.State, civilization.Id);
                    Assert.AreEqual(civilization.Name, session.Civilizations[0].Name);
                    Assert.AreEqual(1, session.Civilizations.Count);
                }
            }
        }

        [Test]
        public void AbyssSmiths_StartWithAGolemAndHighPower()
        {
            using (GameSession session = Session("abyss_smiths"))
            {
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);

                Assert.IsFalse(sector.IsEmpty);
                Assert.AreEqual("iron_golem", sector.Content.Definition.Id);
                Assert.AreEqual(8, session.Civilizations[0].MilitaryPower);
                Assert.AreEqual(2, session.Civilizations[0].Rationality);
                Assert.AreEqual(18, With("abyss_smiths").StartingDeck.Count);
            }
        }

        [Test]
        public void EclipseFlagellants_HealTheCoreWhenPlayingBloodOrFear_ButGrowFaster()
        {
            using (GameSession session = Session("eclipse_flagellants"))
            {
                session.DivineCore.TakeDamage(10);
                int hurt = session.DivineCore.CurrentHealth;
                ICardDefinition blood = DefaultContent.CreateStarterDeck().First(c => c.Id == "infuse_blood");
                ICardDefinition fire = DefaultContent.CreateStarterDeck().First(c => c.Id == "infuse_fire");
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 1);

                session.Cards.Play(fire, sector);
                Assert.AreEqual(hurt, session.DivineCore.CurrentHealth);

                session.Cards.Play(blood, session.Board.GetSectorAt(RingType.MiddleCivilization, 2));
                Assert.AreEqual(hurt + 2, session.DivineCore.CurrentHealth);
                Assert.AreEqual(3, session.Civilizations[0].MilitaryPower);
            }

            Assert.AreEqual(2, With("eclipse_flagellants").RationalityGrowthPerTurn);
        }

        [Test]
        public void TimeHermits_RotateForFree_AndTheirReasonRisesOnlyOnEvenTurns()
        {
            using (GameSession session = Session("time_hermits"))
            {
                int faith = session.Faith.Current;
                int start = session.Civilizations[0].Rationality;

                Assert.IsTrue(session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise));
                Assert.AreEqual(faith, session.Faith.Current); // girar custa 0 de Fé para eles

                session.Player.EndTurn(); // turno 1 (ímpar): sem aumento
                Assert.AreEqual(start, session.Civilizations[0].Rationality);

                session.Player.EndTurn(); // turno 2 (par): +1
                Assert.AreEqual(start + 1, session.Civilizations[0].Rationality);
            }
        }

        [Test]
        public void DefaultCivilization_IsUnchangedByTheCatalog()
        {
            GameSessionConfig plain = DefaultContent.CreateConfig();
            GameSessionConfig chosen = With(CivilizationCatalog.DefaultId);

            Assert.AreEqual(plain.Civilizations[0].Stats.InitialMilitaryPower, chosen.Civilizations[0].Stats.InitialMilitaryPower);
            Assert.AreEqual(plain.StartingDeck.Count, chosen.StartingDeck.Count);
            Assert.AreEqual(plain.RationalityGrowthPerTurn, chosen.RationalityGrowthPerTurn);
        }

        [Test]
        public void Settings_CloneKeepsTheCivilization()
        {
            Assert.AreEqual("time_hermits", new GameSettings { CivilizationId = "time_hermits" }.Clone().CivilizationId);
        }

        // ------------------------------------------------------------------ Intervenção Divina

        [Test]
        public void Rotate_CostsFaith_MovesTheRing_AndAllowsOnlyOnePerTurn()
        {
            using (GameSession session = Session())
            {
                TectonicSector sector = session.Board.GetSectorAt(RingType.OuterWilderness, 0);
                int faith = session.Faith.Current;
                int fired = 0;
                session.RingControl.Used += (kind, ring, direction) => fired++;

                Assert.IsTrue(session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise));

                Assert.AreEqual(faith - session.RingControl.RotateCost, session.Faith.Current);
                Assert.AreNotEqual(0, session.Board.GetPositionOf(sector));
                Assert.IsTrue(session.RingControl.UsedThisTurn);
                Assert.AreEqual(1, fired);
                Assert.IsFalse(session.RingControl.TryRotate(RingType.MiddleCivilization, RotationDirection.Clockwise));
                Assert.IsFalse(session.RingControl.TryFreeze(RingType.MiddleCivilization));
                Assert.AreEqual(faith - session.RingControl.RotateCost, session.Faith.Current);
            }
        }

        [Test]
        public void Intervention_IsAvailableAgainOnTheNextTurn()
        {
            using (GameSession session = Session())
            {
                session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.CounterClockwise);
                session.Player.EndTurn();

                Assert.IsFalse(session.RingControl.UsedThisTurn);
                Assert.IsTrue(session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise));
                Assert.AreEqual(2, session.RingControl.TotalUsed);
            }
        }

        [Test]
        public void Intervention_NeedsFaith_AndAPlayerTurn_AndNoPendingOffer()
        {
            GameSessionConfig config = With(null);
            config.RewardEveryNTurns = 3;

            using (var session = new GameSession(config))
            {
                session.Start();
                session.Faith.TrySpend(session.Faith.Current);
                Assert.IsFalse(session.RingControl.CanRotate());
                Assert.IsFalse(session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise));
                Assert.IsFalse(session.RingControl.TryFreeze(RingType.OuterWilderness));

                session.Faith.Add(10);

                while (session.Turns.TurnNumber < 3)
                {
                    session.Player.EndTurn();
                }

                Assert.IsTrue(session.Rewards.HasPending);
                session.Faith.Add(10);
                Assert.IsFalse(session.RingControl.CanUseNow); // oferenda pendente bloqueia

                session.Rewards.Skip();
                Assert.IsTrue(session.RingControl.CanUseNow);
            }
        }

        [Test]
        public void Freeze_CancelsTheNextScheduledRotation_ThenThawsAutomatically()
        {
            using (GameSession session = Session())
            {
                // Turno 1: os Ermos giram 1 casa horária por turno.
                TectonicSector sector = session.Board.GetSectorAt(RingType.OuterWilderness, 3);
                int before = session.Board.GetPositionOf(sector);
                int faith = session.Faith.Current;

                Assert.IsTrue(session.PlannedShifts(1).Any(r => r.Ring == RingType.OuterWilderness));
                Assert.IsTrue(session.RingControl.TryFreeze(RingType.OuterWilderness));
                Assert.AreEqual(faith - session.RingControl.FreezeCost, session.Faith.Current);
                Assert.IsTrue(session.RingControl.IsFrozen(RingType.OuterWilderness));
                Assert.IsFalse(session.PlannedShifts(1).Any(r => r.Ring == RingType.OuterWilderness)); // a previsão já reflete

                session.Player.EndTurn();

                Assert.AreEqual(before, session.Board.GetPositionOf(sector)); // não girou
                Assert.IsFalse(session.RingControl.IsFrozen(RingType.OuterWilderness));

                session.Player.EndTurn();
                Assert.AreNotEqual(before, session.Board.GetPositionOf(sector)); // voltou a girar
            }
        }

        [Test]
        public void FrozenRing_DoesNotStopOtherRings()
        {
            using (GameSession session = Session())
            {
                TectonicSector middle = session.Board.GetSectorAt(RingType.MiddleCivilization, 0);
                TectonicSector inner = session.Board.GetSectorAt(RingType.InnerSanctuary, 0);
                session.RingControl.TryFreeze(RingType.OuterWilderness);

                // O Santuário gira no turno 3 (o planejador padrão), mesmo com os Ermos congelados.
                var plan = session.PlannedShifts(3);
                Assert.IsTrue(plan.Any(r => r.Ring == RingType.InnerSanctuary));
                Assert.IsFalse(plan.Any(r => r.Ring == RingType.OuterWilderness));
                Assert.AreEqual(0, session.Board.GetPositionOf(middle));
                Assert.AreEqual(0, session.Board.GetPositionOf(inner));
            }
        }

        [Test]
        public void Rotating_RepositionsInvaders_AndTheNarratorMentionsIt()
        {
            using (GameSession session = Session())
            {
                session.Invaders.TrySpawn(new Invaders.SimpleInvaderDefinition("t", "T", 5, 1, 0),
                    session.Board.GetSectorAt(RingType.OuterWilderness, 0), out Invaders.InvaderEntity invader);

                session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise);

                Assert.AreNotEqual(0, session.Board.GetPositionOf(invader.Sector));
                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("girou o anel")));
            }
        }

        [Test]
        public void Achievements_DivineHandAndSectsUnited_AreAwarded()
        {
            var progress = new AchievementProgress();

            using (GameSession session = Session())
            using (new AchievementTracker(session, new GameSettings(), progress, null))
            {
                for (int i = 0; i < 10; i++)
                {
                    session.Faith.Add(10);
                    Assert.IsTrue(session.RingControl.TryRotate(RingType.OuterWilderness, RotationDirection.Clockwise));
                    session.Player.EndTurn();
                }
            }

            Assert.IsTrue(progress.IsUnlocked("divine_hand"));

            progress.Add(AchievementCatalog.CounterCivWinPrefix + "ash_children", 1);
            progress.Add(AchievementCatalog.CounterCivWinPrefix + "abyss_smiths", 1);
            Assert.AreEqual(2, progress.CivilizationsWon());
            Assert.AreEqual(2, progress.ProgressOf(AchievementCatalog.Find("civ_master")));
        }
    }
}
