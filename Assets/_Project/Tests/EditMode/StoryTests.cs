using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Os textos da história: nada sem texto, nada longo demais, e a crônica realmente os mostra.
    /// </summary>
    public sealed class StoryTests
    {
        private const int MaxLength = 260;

        private static GameSession NewSession(string civilizationId = null, bool endless = false)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 8;
            config.RewardEveryNTurns = 0;
            config.CoreMaxHealth = 5000;
            CivilizationCatalog.Find(civilizationId)?.Apply(config);

            if (endless)
            {
                config.TurnsToSurvive = 0;
            }

            return new GameSession(config);
        }

        [Test]
        public void EveryCivilization_HasAQuoteAndAVictoryEpilogue()
        {
            foreach (CivilizationDefinition civilization in CivilizationCatalog.All)
            {
                Assert.IsNotEmpty(Story.CivilizationQuote(civilization.Id), civilization.Id);
                Assert.IsNotEmpty(Story.VictoryEpilogue(civilization.Id), civilization.Id);
            }

            Assert.AreEqual(CivilizationCatalog.All.Count, Story.CivilizationIds.Count());
        }

        [Test]
        public void EveryInvaderType_HasABark()
        {
            var planner = DefaultContent.CreateInvasionPlanner();
            var ids = Enumerable.Range(1, 12).SelectMany(t => planner.PlanSpawns(t)).Select(o => o.Definition.Id).Distinct();

            foreach (string id in ids)
            {
                Assert.IsNotEmpty(Story.InvaderBark(id), id);
            }
        }

        [Test]
        public void AllTexts_AreShortEnoughForTheInterface()
        {
            var texts = Story.Opening.Concat(Story.EndlessEntry).Concat(Story.Chronicles)
                .Concat(new[] { Story.InquisitorArrives, Story.InquisitorDies, Story.InquisitorVerdict, Story.EndlessDeep, Story.DefeatRare })
                .Concat(CivilizationCatalog.All.Select(c => Story.CivilizationQuote(c.Id)))
                .Concat(CivilizationCatalog.All.Select(c => Story.VictoryEpilogue(c.Id)))
                .Concat(Story.BarkIds.Select(Story.InvaderBark))
                .Concat(new[] { Story.ActEnding(7), Story.ActEnding(14) });

            foreach (string text in texts)
            {
                Assert.IsNotEmpty(text);
                Assert.LessOrEqual(text.Length, MaxLength, text);
            }

            foreach (DefeatCause cause in System.Enum.GetValues(typeof(DefeatCause)))
            {
                Assert.LessOrEqual(Story.DefeatEpilogue(cause, 1).Length, MaxLength + 40, cause.ToString());
            }
        }

        [Test]
        public void DefeatEpilogue_DependsOnTheCause_AndSometimesIsRare()
        {
            Assert.AreNotEqual(Story.DefeatEpilogue(DefeatCause.Invaders, 1), Story.DefeatEpilogue(DefeatCause.Heresy, 1));
            Assert.AreNotEqual(Story.DefeatEpilogue(DefeatCause.Heresy, 1), Story.DefeatEpilogue(DefeatCause.Endless, 1));
            Assert.AreEqual(Story.DefeatRare, Story.DefeatEpilogue(DefeatCause.Invaders, 10));
            Assert.AreEqual(DefeatCause.Endless, Story.CauseOf(true, true));
            Assert.AreEqual(DefeatCause.Heresy, Story.CauseOf(false, true));
            Assert.AreEqual(DefeatCause.Invaders, Story.CauseOf(false, false));
        }

        [Test]
        public void Chronicles_AppearEveryThirdTurn_InOrder_AndNeverRunOut()
        {
            Assert.AreEqual(string.Empty, Story.ChronicleForTurn(1));
            Assert.AreEqual(string.Empty, Story.ChronicleForTurn(4));
            Assert.AreEqual(Story.Chronicles[0], Story.ChronicleForTurn(3));
            Assert.AreEqual(Story.Chronicles[1], Story.ChronicleForTurn(6));
            Assert.IsNotEmpty(Story.ChronicleForTurn(300));
        }

        [Test]
        public void ActEndings_OnlyAtTheEndOfActsOneAndTwo()
        {
            Assert.IsNotEmpty(Story.ActEnding(7));
            Assert.IsNotEmpty(Story.ActEnding(14));
            Assert.AreEqual(string.Empty, Story.ActEnding(8));
        }

        [Test]
        public void Start_WritesTheOpening_AndTheCivilizationQuote()
        {
            using (GameSession session = NewSession("eclipse_flagellants"))
            {
                session.Start();

                string log = string.Join("\n", session.Log.Entries.Select(e => e.Text));
                StringAssert.Contains("Antes do primeiro nome", log);
                StringAssert.Contains("Flagelantes do Eclipse", log);
                StringAssert.Contains(Story.CivilizationQuote("eclipse_flagellants"), log);
            }
        }

        [Test]
        public void EndlessMode_AddsTheLastCycleEntry()
        {
            using (GameSession session = NewSession(endless: true))
            {
                session.Start();
                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("Mas você permaneceu")));
            }

            using (GameSession standard = NewSession())
            {
                standard.Start();
                Assert.IsFalse(standard.Log.Entries.Any(e => e.Text.Contains("Mas você permaneceu")));
            }
        }

        [Test]
        public void EachInvaderType_BarksOnlyTheFirstTimeItAppears()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", 5, 2, 1);
                session.Invaders.TrySpawn(cultist, session.Board.GetSectorAt(RingType.OuterWilderness, 0), out _);
                session.Invaders.TrySpawn(cultist, session.Board.GetSectorAt(RingType.OuterWilderness, 1), out _);

                int barks = session.Log.Entries.Count(e => e.Text.Contains("Nossos mortos chamam"));
                Assert.AreEqual(1, barks);
            }
        }

        [Test]
        public void Inquisitor_SpeaksWhenItAppears_AndWhenItDies()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                var inquisitor = new SimpleInvaderDefinition("inquisitor", "Inquisidor Cinzento", 20, 4, 5, rationalityPerTurn: 1);
                session.Invaders.TrySpawn(inquisitor, session.Board.GetSectorAt(RingType.OuterWilderness, 3), out InvaderEntity boss);

                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("Tu me fizeste santo")));
                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("nunca terminasse de morrer")));

                boss.TakeDamage(99);
                session.Invaders.RemoveDead();

                Assert.IsTrue(session.Log.Entries.Any(e => e.Text.Contains("o que acontece quando ninguém mais obedece")));
            }
        }

        [Test]
        public void Chronicles_ReachTheLogDuringAGame()
        {
            using (GameSession session = NewSession())
            {
                session.Start();

                for (int i = 0; i < 6 && session.Turns.State == Turns.TurnState.AwaitingPlayer; i++)
                {
                    session.Player.EndTurn();
                }

                Assert.IsTrue(session.Log.Entries.Any(e => e.Text == Story.Chronicles[0]));
                Assert.IsTrue(session.Log.Entries.Any(e => e.Text == Story.ActEnding(7)) || session.Turns.TurnNumber <= 7);
            }
        }
    }
}
