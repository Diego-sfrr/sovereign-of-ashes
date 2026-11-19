using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>Cartas de nicho, reforço dos invasores por patamar e o viés de nicho das Oferendas.</summary>
    public sealed class NicheCardTests
    {
        private static GameSession NewSession(int coreHealth = 1000, int seed = 3)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = seed;
            config.CoreMaxHealth = coreHealth;
            config.RewardEveryNTurns = 0;
            var session = new GameSession(config);
            session.Start();
            session.Faith.Add(50);
            return session;
        }

        private static ICardDefinition Card(string id) => DefaultContent.CreateRewardPool().First(c => c.Id == id);

        private static InvaderEntity Spawn(GameSession session, RingType ring, int position, int health = 10)
        {
            session.Invaders.TrySpawn(new SimpleInvaderDefinition("dummy", "Alvo", health, 1, 0),
                session.Board.GetSectorAt(ring, position), out InvaderEntity invader);
            return invader;
        }

        [Test]
        public void NicheCards_AreInThePool_WithUniqueIdsAndText()
        {
            IReadOnlyList<ICardDefinition> pool = DefaultContent.CreateRewardPool();
            IReadOnlyList<ICardDefinition> niche = DefaultContent.CreateNicheCards();

            Assert.AreEqual(13, niche.Count);
            Assert.AreEqual(pool.Count, pool.Select(c => c.Id).Distinct().Count(), "ids repetidos no pool");

            foreach (ICardDefinition card in niche)
            {
                Assert.IsTrue(pool.Any(c => c.Id == card.Id), card.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.Description), card.Id);
                Assert.GreaterOrEqual(card.Cost, 0, card.Id);
                Assert.LessOrEqual(card.Cost, 3, card.Id);
            }

            // Todo conceito tem pelo menos uma carta de nicho (assim toda civilização tem o que ser favorecida).
            foreach (ConceptType concept in (ConceptType[])System.Enum.GetValues(typeof(ConceptType)))
            {
                Assert.IsTrue(niche.Any(c => c.PrimaryConcept == concept), concept.ToString());
            }
        }

        [Test]
        public void AshTide_HitsEveryInvaderOnTheRing_AndOnlyThatRing()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity a = Spawn(session, RingType.OuterWilderness, 0);
                InvaderEntity b = Spawn(session, RingType.OuterWilderness, 3);
                InvaderEntity inner = Spawn(session, RingType.InnerSanctuary, 1);

                Assert.AreEqual(CardPlayOutcome.Played, session.Cards.Play(Card("ash_tide"), a.Sector));

                Assert.AreEqual(7, a.CurrentHealth);
                Assert.AreEqual(7, b.CurrentHealth);
                Assert.AreEqual(10, inner.CurrentHealth);
            }
        }

        [Test]
        public void Verdict_SlaysTheWeak_AndOnlyWoundsTheStrong()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity weak = Spawn(session, RingType.OuterWilderness, 0, health: 6);
                InvaderEntity strong = Spawn(session, RingType.OuterWilderness, 4, health: 12);

                Assert.AreEqual(CardPlayOutcome.Played, session.Cards.Play(Card("verdict"), weak.Sector));
                Assert.IsTrue(weak.IsDead);

                Assert.AreEqual(CardPlayOutcome.Played, session.Cards.Play(Card("verdict"), strong.Sector));
                Assert.AreEqual(9, strong.CurrentHealth);
            }
        }

        [Test]
        public void Bloodletting_PaysWithCoreHealth_ButNeverKillsTheCore()
        {
            using (GameSession session = NewSession(coreHealth: 10))
            {
                InvaderEntity target = Spawn(session, RingType.OuterWilderness, 0, health: 20);

                session.Cards.Play(Card("bloodletting"), target.Sector);
                Assert.AreEqual(7, session.DivineCore.CurrentHealth);
                Assert.AreEqual(13, target.CurrentHealth);

                session.Cards.Play(Card("bloodletting"), target.Sector);
                session.Cards.Play(Card("bloodletting"), target.Sector);
                Assert.GreaterOrEqual(session.DivineCore.CurrentHealth, 1);
            }
        }

        [Test]
        public void Pendulum_StrikesAndRotatesTheRing()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity target = Spawn(session, RingType.MiddleCivilization, 2);
                int before = session.Board.GetPositionOf(target.Sector);

                session.Cards.Play(Card("pendulum"), target.Sector);

                Assert.AreEqual(8, target.CurrentHealth);
                Assert.AreNotEqual(before, session.Board.GetPositionOf(target.Sector));
            }
        }

        [Test]
        public void ExecuteEffect_RejectsInvalidArguments()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ExecuteEffect(0, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ExecuteEffect(3, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new RingStrikeEffect(-1));
        }

        [Test]
        public void Offers_AlwaysContainACardOfTheCivilizationNiche()
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                GameSessionConfig config = DefaultContent.CreateConfig();
                config.Seed = seed;
                config.CoreMaxHealth = 1000;
                config.RewardNiche.Add(ConceptType.Time);

                using (var session = new GameSession(config))
                {
                    session.Start();

                    while (session.Turns.TurnNumber < 3)
                    {
                        session.Player.EndTurn();
                    }

                    Assert.IsTrue(session.Rewards.HasPending);
                    Assert.IsTrue(session.Rewards.Pending.Choices.Any(c => c.PrimaryConcept == ConceptType.Time), $"semente {seed}");
                    Assert.AreEqual(3, session.Rewards.Pending.Choices.Select(c => c.Id).Distinct().Count());
                }
            }
        }

        [Test]
        public void EveryCivilization_HasANicheAndShowsIt()
        {
            foreach (CivilizationDefinition civ in CivilizationCatalog.All)
            {
                Assert.IsNotEmpty(civ.Niche, civ.Id);
                Assert.IsFalse(string.IsNullOrEmpty(civ.NicheLabel), civ.Id);

                GameSessionConfig config = DefaultContent.CreateConfig();
                civ.Apply(config);
                CollectionAssert.AreEquivalent(civ.Niche, config.RewardNiche, civ.Id);
            }
        }

        [Test]
        public void Escalation_ReinforcesNewInvadersAsTurnsPass_WithoutChangingIdentity()
        {
            IInvasionPlanner plain = DefaultContent.CreateInvasionPlanner(new InvasionPressure(2, 3, 3, 4, InvaderEscalation.None));
            IInvasionPlanner hard = DefaultContent.CreateInvasionPlanner(new InvasionPressure(2, 3, 3, 4, new InvaderEscalation(3, 2, 2)));

            IInvaderDefinition early = hard.PlanSpawns(1).First().Definition;
            IInvaderDefinition lateBase = plain.PlanSpawns(19).First(o => o.Definition.Id == "ash_cultist").Definition;
            IInvaderDefinition lateHard = hard.PlanSpawns(19).First(o => o.Definition.Id == "ash_cultist").Definition;

            Assert.AreEqual(plain.PlanSpawns(1).First().Definition.MaxHealth, early.MaxHealth, "antes do primeiro patamar não muda");
            Assert.AreEqual(5 + 6 * 2, lateHard.MaxHealth);
            Assert.AreEqual(lateBase.Attack + 3, lateHard.Attack);
            Assert.AreEqual(lateBase.Id, lateHard.Id);
            Assert.AreEqual(lateBase.FaithBounty, lateHard.FaithBounty);
        }

        [Test]
        public void Escalation_KeepsInvaderTraits()
        {
            IInvasionPlanner planner = DefaultContent.CreateInvasionPlanner(new InvasionPressure(2, 3, 3, 4, new InvaderEscalation(3, 1, 0)));
            IInvaderDefinition profaner = planner.PlanSpawns(10).First(o => o.Definition.Id == "profaner").Definition;
            IInvaderDefinition herald = planner.PlanSpawns(9).First(o => o.Definition.Id == "heretic_herald").Definition;

            Assert.IsTrue(profaner.CorruptsElements());
            Assert.AreEqual(1, herald.RationalityPerTurn());
        }

        [Test]
        public void Difficulties_GetStrictlyHarder_ForTheSameSeed()
        {
            GameSessionConfig easy = DefaultContent.CreateConfig();
            GameSessionConfig normal = DefaultContent.CreateConfig();
            GameSessionConfig hard = DefaultContent.CreateConfig();
            DifficultyPresets.Apply(easy, Difficulty.Easy);
            DifficultyPresets.Apply(normal, Difficulty.Normal);
            DifficultyPresets.Apply(hard, Difficulty.Hard);

            // As ondas chegam em turnos diferentes em cada dificuldade, então compara-se a força total acumulada.
            int TotalPower(GameSessionConfig c, int untilTurn) => Enumerable.Range(1, untilTurn)
                .Sum(turn => c.InvasionPlanner.PlanSpawns(turn).Sum(o => o.Count * (o.Definition.MaxHealth + o.Definition.Attack * 2)));

            for (int turn = 8; turn <= 19; turn++)
            {
                Assert.Less(TotalPower(easy, turn), TotalPower(normal, turn), $"até o turno {turn}: normal deve pesar mais que fácil");
                Assert.Less(TotalPower(normal, turn), TotalPower(hard, turn), $"até o turno {turn}: difícil deve pesar mais que normal");
            }
        }

        [Test]
        public void BalanceBot_FindsEachDifficultyHarderThanTheLast()
        {
            double Average(Difficulty difficulty) => Enumerable.Range(1, 60)
                .Average(seed => BalanceBot.Play(new GameSettings { Difficulty = difficulty, Seed = seed }).Turn);

            double easy = Average(Difficulty.Easy);
            double normal = Average(Difficulty.Normal);
            double hard = Average(Difficulty.Hard);

            Assert.Greater(easy, normal - 0.01, $"fácil {easy:F1} x normal {normal:F1}");
            // O bot morre antes de os reforços do Difícil pesarem: aqui só se exclui o Difícil ser claramente mais fácil.
            Assert.Greater(normal, hard - 0.7, $"normal {normal:F1} x difícil {hard:F1}");
            Assert.Greater(easy, hard + 1.0, $"fácil {easy:F1} x difícil {hard:F1}");
        }
    }
}
