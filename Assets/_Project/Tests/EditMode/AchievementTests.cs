using System.Collections.Generic;
using NUnit.Framework;
using SovereignOfAshes.Achievements;
using SovereignOfAshes.Board;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Conquistas: gravação tolerante e concessão conforme se joga.
    /// </summary>
    public sealed class AchievementTests
    {
        [Test]
        public void Progress_RoundTripsThroughText()
        {
            var original = new AchievementProgress();
            original.Unlock("victory");
            original.AddCraftedElement("forge");
            original.Add("kills", 42);

            AchievementProgress copy = AchievementProgress.Parse(original.Serialize());

            Assert.IsTrue(copy.IsUnlocked("victory"));
            Assert.IsFalse(copy.IsUnlocked("heresy"));
            Assert.AreEqual(1, copy.CraftedElementCount);
            Assert.AreEqual(42, copy.Get("kills"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("lixo ;;; = ,,, c.=x;u=;r=;c.kills=abc")]
        public void Parse_NeverThrows(string text)
        {
            AchievementProgress progress = AchievementProgress.Parse(text);

            Assert.AreEqual(0, progress.UnlockedCount);
            Assert.AreEqual(0, progress.Get("kills"));
        }

        [Test]
        public void Parse_IgnoresUnknownIds_AndClampsNegatives()
        {
            AchievementProgress progress = AchievementProgress.Parse("u=victory,nao_existe;c.kills=-5");

            Assert.AreEqual(1, progress.UnlockedCount);
            Assert.AreEqual(0, progress.Get("kills"));
        }

        [Test]
        public void Catalog_HasUniqueIdsWithoutSeparators()
        {
            var seen = new HashSet<string>();

            foreach (AchievementDefinition definition in AchievementCatalog.All)
            {
                Assert.IsTrue(seen.Add(definition.Id), "id repetido: " + definition.Id);
                Assert.IsFalse(definition.Id.Contains(";") || definition.Id.Contains(",") || definition.Id.Contains("="), definition.Id);
                Assert.IsNotEmpty(definition.Name);
                Assert.IsNotEmpty(definition.Description);
            }
        }

        [Test]
        public void Progress_ReportsCounterTowardsGoal()
        {
            var progress = new AchievementProgress();
            progress.Add("kills", 30);

            AchievementDefinition exterminator = AchievementCatalog.Find("exterminator");

            Assert.AreEqual(30, progress.ProgressOf(exterminator));
            progress.Add("kills", 500);
            Assert.AreEqual(exterminator.Goal, progress.ProgressOf(exterminator));
        }

        [Test]
        public void Tracker_AwardsAchievementsWhileAGameIsPlayed()
        {
            var progress = new AchievementProgress();
            var unlockedNow = new List<string>();
            int saves = 0;
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 3;

            using (var session = new GameSession(config))
            using (var tracker = new AchievementTracker(session, new GameSettings(), progress, () => saves++))
            {
                tracker.Unlocked += d => unlockedNow.Add(d.Id);
                session.Start();
                int guard = 0;

                while (session.Turns.State == TurnState.AwaitingPlayer && guard++ < 100)
                {
                    PlayEverything(session);
                    session.Player.EndTurn();
                }
            }

            Assert.AreEqual(1, progress.Get("games"));
            Assert.Greater(progress.Get("kills"), 0);
            Assert.IsTrue(progress.IsUnlocked("first_blood"));
            Assert.IsTrue(progress.IsUnlocked("victory") || progress.IsUnlocked("fallen_throne"));
            Assert.AreEqual(unlockedNow.Count, progress.UnlockedCount);
            Assert.Greater(saves, 0);
        }

        [Test]
        public void Tracker_EndlessMilestone_IsAwardedByTurnReached()
        {
            var progress = new AchievementProgress();
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.TurnsToSurvive = 0;
            config.CoreMaxHealth = 100000;

            using (var session = new GameSession(config))
            using (new AchievementTracker(session, new GameSettings { Mode = GameMode.Endless }, progress, null))
            {
                session.Start();

                for (int i = 0; i < 12 && session.Turns.State == TurnState.AwaitingPlayer; i++)
                {
                    session.Player.EndTurn();
                }
            }

            Assert.IsTrue(progress.IsUnlocked("endless_10"));
            Assert.IsFalse(progress.IsUnlocked("endless_25"));
        }

        [Test]
        public void Tracker_DoesNotAwardEndlessMilestones_InStandardMode()
        {
            var progress = new AchievementProgress();
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.CoreMaxHealth = 100000;

            using (var session = new GameSession(config))
            using (new AchievementTracker(session, new GameSettings(), progress, null))
            {
                session.Start();

                for (int i = 0; i < 12 && session.Turns.State == TurnState.AwaitingPlayer; i++)
                {
                    session.Player.EndTurn();
                }
            }

            Assert.IsFalse(progress.IsUnlocked("endless_10"));
        }

        [Test]
        public void Tracker_StopsListeningAfterDispose()
        {
            var progress = new AchievementProgress();
            GameSessionConfig config = DefaultContent.CreateConfig();

            using (var session = new GameSession(config))
            {
                var tracker = new AchievementTracker(session, new GameSettings(), progress, null);
                tracker.Dispose();
                session.Start();

                for (int i = 0; i < 8 && session.Turns.State == TurnState.AwaitingPlayer; i++)
                {
                    PlayEverything(session);
                    session.Player.EndTurn();
                }
            }

            Assert.AreEqual(0, progress.UnlockedCount);
            Assert.AreEqual(0, progress.Get("kills"));
        }

        private static void PlayEverything(GameSession s)
        {
            bool played = true;

            while (played)
            {
                played = false;

                for (int i = 0; i < s.Deck.Hand.Count && !played; i++)
                {
                    foreach (TectonicSector sector in s.Board.GetAllSectors())
                    {
                        if (s.Player.Preview(i, sector) == SovereignOfAshes.Cards.CardPlayOutcome.Played)
                        {
                            played = s.Player.PlayFromHand(i, sector) == SovereignOfAshes.Cards.CardPlayOutcome.Played;
                            break;
                        }
                    }
                }
            }
        }
    }
}
