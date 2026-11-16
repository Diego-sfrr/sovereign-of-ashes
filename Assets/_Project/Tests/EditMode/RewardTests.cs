using System;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Oferendas Divinas (recompensas de cartas) e as cartas novas do pool.
    /// </summary>
    public sealed class RewardTests
    {
        private static GameSession NewSession(int everyN = 3, int seed = 11)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = seed;
            config.RewardEveryNTurns = everyN;
            config.CoreMaxHealth = 1000;
            return new GameSession(config);
        }

        private static void AdvanceTo(GameSession session, int turn)
        {
            while (session.Turns.TurnNumber < turn && session.Turns.State == TurnState.AwaitingPlayer)
            {
                session.Player.EndTurn();
            }
        }

        private static ICardDefinition Card(string id) =>
            DefaultContent.CreateRewardPool().First(c => c.Id == id);

        [Test]
        public void Offer_AppearsEveryThirdTurn_WithThreeDistinctCards()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                Assert.IsFalse(session.Rewards.HasPending);

                AdvanceTo(session, 3);
                Assert.IsTrue(session.Rewards.HasPending);
                Assert.AreEqual(3, session.Rewards.Pending.Choices.Count);
                Assert.AreEqual(3, session.Rewards.Pending.Choices.Select(c => c.Id).Distinct().Count());
                Assert.AreEqual(3, session.Rewards.Pending.Turn);
            }
        }

        [Test]
        public void Offer_BlocksPlayingCards_UntilResolved()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                AdvanceTo(session, 3);

                Assert.IsFalse(session.Player.CanAct);
                Assert.AreEqual(CardPlayOutcome.NotPlayerTurn, session.Player.Preview(0, session.Board.GetAllSectors().First()));

                session.Rewards.Skip();

                Assert.IsTrue(session.Player.CanAct);
            }
        }

        [Test]
        public void Choose_AddsTheCardToTheDeck()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                AdvanceTo(session, 3);
                int before = session.Deck.TotalCount;
                ICardDefinition picked = session.Rewards.Pending.Choices[1];
                ICardDefinition announced = null;
                session.Rewards.OfferResolved += c => announced = c;

                session.Rewards.Choose(1);

                Assert.AreEqual(before + 1, session.Deck.TotalCount);
                Assert.AreEqual(1, session.Deck.CountOf(picked.Id));
                Assert.AreSame(picked, announced);
                Assert.IsFalse(session.Rewards.HasPending);
                Assert.AreEqual(1, session.Rewards.PickedCount);
            }
        }

        [Test]
        public void Skip_GivesFaith_WithoutChangingTheDeck()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                AdvanceTo(session, 3);
                int deck = session.Deck.TotalCount;
                session.Faith.TrySpend(3); // a Fé costuma estar no teto aqui; abre espaço para ver o ganho
                int faith = session.Faith.Current;

                session.Rewards.Skip();

                Assert.AreEqual(deck, session.Deck.TotalCount);
                Assert.AreEqual(faith + 1, session.Faith.Current);
                Assert.AreEqual(1, session.Rewards.ResolvedCount);
            }
        }

        [Test]
        public void EndingTheTurnWithoutDeciding_CountsAsSkipping()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                AdvanceTo(session, 3);

                Assert.IsTrue(session.Player.EndTurn());

                Assert.IsFalse(session.Rewards.HasPending);
                Assert.AreEqual(1, session.Rewards.ResolvedCount);
                Assert.AreEqual(0, session.Rewards.PickedCount);
                Assert.IsTrue(session.Player.CanAct);
            }
        }

        [Test]
        public void Rewards_CanBeDisabled()
        {
            using (GameSession off = NewSession(everyN: 0))
            {
                off.Start();
                AdvanceTo(off, 9);
                Assert.IsFalse(off.Rewards.IsEnabled);
                Assert.AreEqual(0, off.Rewards.ResolvedCount);
            }

            GameSessionConfig config = DefaultContent.CreateConfig();
            config.RewardPool.Clear();

            using (var empty = new GameSession(config))
            {
                Assert.IsFalse(empty.Rewards.IsEnabled);
            }
        }

        [Test]
        public void Rewards_AreReproducibleForTheSameSeed()
        {
            string Offered(int seed)
            {
                using (GameSession session = NewSession(seed: seed))
                {
                    session.Start();
                    AdvanceTo(session, 3);
                    return string.Join(",", session.Rewards.Pending.Choices.Select(c => c.Id));
                }
            }

            Assert.AreEqual(Offered(5), Offered(5));
        }

        [Test]
        public void Choose_Throws_WhenNothingIsPending_OrIndexInvalid()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                Assert.Throws<InvalidOperationException>(() => session.Rewards.Choose(0));
                Assert.Throws<InvalidOperationException>(() => session.Rewards.Skip());

                AdvanceTo(session, 3);
                Assert.Throws<ArgumentOutOfRangeException>(() => session.Rewards.Choose(7));
            }
        }

        [Test]
        public void RewardPool_HasUniqueIds_AndNoneOverlapsTheStarterDeck()
        {
            var pool = DefaultContent.CreateRewardPool();
            var starter = DefaultContent.CreateStarterDeck().Select(c => c.Id).ToList();

            Assert.AreEqual(pool.Count, pool.Select(c => c.Id).Distinct().Count());
            Assert.IsFalse(pool.Any(c => starter.Contains(c.Id)));
        }

        [Test]
        public void DivineTurn_RotatesTheTargetsRing()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                session.Faith.Add(10);
                TectonicSector sector = session.Board.GetSectorAt(RingType.OuterWilderness, 0);

                Assert.AreEqual(CardPlayOutcome.Played, session.Cards.Play(Card("divine_turn"), sector));
                Assert.AreNotEqual(0, session.Board.GetPositionOf(sector));
                int afterClockwise = session.Board.GetPositionOf(sector);

                session.Cards.Play(Card("counter_turn"), sector);
                Assert.AreEqual(0, session.Board.GetPositionOf(sector));
                Assert.AreNotEqual(0, afterClockwise);
            }
        }

        [Test]
        public void Exile_PushesAnInvaderOutward_OnlyIfDestinationIsFree()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                session.Faith.Add(10);
                var definition = new SimpleInvaderDefinition("test", "Teste", 5, 1, 0);

                TectonicSector middle = session.Board.GetSectorAt(RingType.MiddleCivilization, 2);
                Assert.IsTrue(session.Invaders.TrySpawn(definition, middle, out InvaderEntity pushed));
                session.Cards.Play(Card("exile"), middle);
                Assert.AreEqual(RingType.OuterWilderness, pushed.Sector.Ring);
                Assert.AreEqual(2, session.Board.GetPositionOf(pushed.Sector));

                // Já está no anel externo: a carta é gasta, mas nada muda.
                TectonicSector where = pushed.Sector;
                session.Cards.Play(Card("exile"), where);
                Assert.AreSame(where, pushed.Sector);

                // Destino ocupado: o invasor fica onde está.
                TectonicSector other = session.Board.GetSectorAt(RingType.MiddleCivilization, 2);
                Assert.IsTrue(session.Invaders.TrySpawn(definition, other, out InvaderEntity blocked));
                session.Cards.Play(Card("exile"), other);
                Assert.AreEqual(RingType.MiddleCivilization, blocked.Sector.Ring);
            }
        }

        [Test]
        public void BloodTithe_PaysLifeForFaith_ButNeverKillsTheCore()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                int core = session.DivineCore.CurrentHealth;
                int faith = session.Faith.Current;

                session.Cards.Play(Card("blood_tithe"), null);

                Assert.AreEqual(core - 2, session.DivineCore.CurrentHealth);
                Assert.AreEqual(faith + 3, session.Faith.Current);
            }

            GameSessionConfig config = DefaultContent.CreateConfig();
            config.CoreMaxHealth = 1;

            using (var fragile = new GameSession(config))
            {
                fragile.Start();
                fragile.Cards.Play(Card("blood_tithe"), null);
                Assert.AreEqual(1, fragile.DivineCore.CurrentHealth);
                Assert.IsFalse(fragile.DivineCore.IsDead);
            }
        }

        [Test]
        public void ElementStrikeCards_LeaveTheirConceptOnTheSector()
        {
            using (GameSession session = NewSession())
            {
                session.Start();
                session.Faith.Add(10);
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 4);
                session.Invaders.TrySpawn(new SimpleInvaderDefinition("t", "T", 9, 1, 0), sector, out InvaderEntity invader);

                Assert.AreEqual(CardPlayOutcome.Played, session.Cards.Play(Card("iron_spear"), sector));

                Assert.AreEqual(6, invader.CurrentHealth);
                Assert.IsFalse(sector.IsEmpty);
                Assert.AreEqual("iron", sector.Content.Definition.Id);
            }
        }
    }
}
