using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>Invasores com habilidade (Guardião, Corredor, Necroapóstolo, Flagelo) e os chefes do turno 10 e 15.</summary>
    public sealed class InvaderAbilityTests
    {
        private static GameSession NewSession()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.CoreMaxHealth = 1000;
            config.RewardEveryNTurns = 0;
            config.Seed = 9;
            var session = new GameSession(config);
            session.Start();
            return session;
        }

        private static InvaderEntity Spawn(GameSession session, IInvaderDefinition definition, RingType ring, int position)
        {
            Assert.IsTrue(session.Invaders.TrySpawn(definition, session.Board.GetSectorAt(ring, position), out InvaderEntity invader));
            return invader;
        }

        [Test]
        public void Guardian_ReducesEveryHit_ButNeverBelowOne()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity guardian = Spawn(session, DefaultContent.CreateGuardian(), RingType.OuterWilderness, 0);

                session.Combat.DealDamage(guardian, 4);
                Assert.AreEqual(8, guardian.CurrentHealth);

                session.Combat.DealDamage(guardian, 1);
                Assert.AreEqual(7, guardian.CurrentHealth, "todo golpe causa pelo menos 1");

                session.Combat.DealDamage(guardian, 0);
                Assert.AreEqual(7, guardian.CurrentHealth);
            }
        }

        [Test]
        public void Runner_WalksThroughBarriers_WhileACultistIsStopped()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity runner = Spawn(session, DefaultContent.CreateRunner(), RingType.MiddleCivilization, 0);
                var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista", 5, 2, 1);
                InvaderEntity plain = Spawn(session, cultist, RingType.MiddleCivilization, 4);

                foreach (InvaderEntity invader in new[] { runner, plain })
                {
                    TectonicSector inner = session.Board.GetNeighbors(invader.Sector).Inner;
                    session.Alchemy.ApplyConcept(inner, ConceptType.Flesh);
                    session.Alchemy.ApplyConcept(inner, ConceptType.Iron);
                    Assert.AreEqual("iron_golem", (inner.Content as SovereignOfAshes.Alchemy.SectorElement)?.Definition.Id);
                }

                session.AdvancePhase.Execute(1);

                Assert.AreEqual(RingType.InnerSanctuary, runner.Sector.Ring, "o Corredor ignora o Golem");
                Assert.AreEqual(RingType.MiddleCivilization, plain.Sector.Ring, "o Cultista é barrado");
            }
        }

        [Test]
        public void MarchForecast_KnowsTheRunnerIgnoresBarriers()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity runner = Spawn(session, DefaultContent.CreateRunner(), RingType.MiddleCivilization, 2);
                TectonicSector inner = session.Board.GetNeighbors(runner.Sector).Inner;
                session.Alchemy.ApplyConcept(inner, ConceptType.Flesh);
                session.Alchemy.ApplyConcept(inner, ConceptType.Iron);

                var forecaster = new MarchForecaster(session.Board, session.Invaders, session.ElementEffects.WouldBlock);
                Assert.AreEqual(MarchIntent.Advance, forecaster.Predict(runner, new RingRotation[0]).Intent);
            }
        }

        [Test]
        public void NecroApostle_HealsOtherInvadersOnItsRing_NotOtherRings()
        {
            using (GameSession session = NewSession())
            {
                Spawn(session, DefaultContent.CreateNecroApostle(), RingType.OuterWilderness, 0);
                InvaderEntity sameRing = Spawn(session, DefaultContent.CreateMidBoss(0), RingType.OuterWilderness, 3);
                InvaderEntity otherRing = Spawn(session, DefaultContent.CreateMidBoss(0), RingType.MiddleCivilization, 3);
                session.Combat.DealDamage(sameRing, 10);
                session.Combat.DealDamage(otherRing, 10);

                session.TraitsPhase.Execute(1);

                Assert.AreEqual(12, sameRing.CurrentHealth);
                Assert.AreEqual(10, otherRing.CurrentHealth);
            }
        }

        [Test]
        public void Scourge_ExplodesOnDeath_HurtingNeighborsAndDestroyingTheElement()
        {
            using (GameSession session = NewSession())
            {
                TectonicSector sector = session.Board.GetSectorAt(RingType.MiddleCivilization, 2);
                InvaderEntity scourge = Spawn(session, DefaultContent.CreateScourge(), RingType.MiddleCivilization, 2);
                InvaderEntity neighbor = Spawn(session, DefaultContent.CreateMidBoss(0), RingType.MiddleCivilization, 3);
                InvaderEntity far = Spawn(session, DefaultContent.CreateMidBoss(0), RingType.OuterWilderness, 6);
                session.Alchemy.ApplyConcept(sector, ConceptType.Fire);
                Assert.IsFalse(sector.IsEmpty);

                session.Combat.DealDamage(scourge, 99);
                session.Invaders.RemoveDead();

                Assert.AreEqual(18, neighbor.CurrentHealth);
                Assert.AreEqual(20, far.CurrentHealth);
                Assert.IsTrue(sector.IsEmpty);
            }
        }

        [Test]
        public void Explosions_ChainWithoutCorruptingTheRoster()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity first = Spawn(session, DefaultContent.CreateScourge(), RingType.MiddleCivilization, 2);
                InvaderEntity second = Spawn(session, DefaultContent.CreateScourge(), RingType.MiddleCivilization, 3);
                InvaderEntity bystander = Spawn(session, DefaultContent.CreateMidBoss(0), RingType.MiddleCivilization, 4);
                session.Combat.DealDamage(second, 4); // 2 de vida: a explosão do primeiro basta

                session.Combat.DealDamage(first, 99);
                session.Invaders.RemoveDead();

                Assert.IsTrue(first.IsDead);
                Assert.IsTrue(second.IsDead, "a primeira explosão matou a segunda");
                Assert.AreEqual(1, session.Invaders.Count, "só sobrou quem não morreu");
                Assert.AreEqual(18, bystander.CurrentHealth, "só é vizinho da segunda explosão (2 de dano)");
            }
        }

        [Test]
        public void TimeProphet_TurnsTheMiddleRingEveryTurn()
        {
            using (GameSession session = NewSession())
            {
                Spawn(session, DefaultContent.CreateMidBoss(1), RingType.OuterWilderness, 0);
                int before = session.Board.GetRing(RingType.MiddleCivilization).RotationOffset;

                session.TraitsPhase.Execute(1);

                Assert.AreNotEqual(before, session.Board.GetRing(RingType.MiddleCivilization).RotationOffset);
            }
        }

        [Test]
        public void CrimsonColossus_DestroysTheElementItStandsOn()
        {
            using (GameSession session = NewSession())
            {
                InvaderEntity colossus = Spawn(session, DefaultContent.CreateMidBoss(2), RingType.MiddleCivilization, 1);
                session.Alchemy.ApplyConcept(colossus.Sector, ConceptType.Fire);

                session.TraitsPhase.Execute(1);

                Assert.IsTrue(colossus.Sector.IsEmpty);
            }
        }

        [Test]
        public void FinalBoss_ArrivesAtTurnFifteen_InEveryDifficulty_AndGetsStrongerWithDifficulty()
        {
            int Health(Difficulty difficulty)
            {
                GameSessionConfig config = DefaultContent.CreateConfig();
                DifficultyPresets.Apply(config, difficulty);
                IInvaderDefinition king = config.InvasionPlanner.PlanSpawns(15).First(o => o.Definition.Id == "heretic_king").Definition;
                Assert.IsTrue(king.IsBoss());
                Assert.AreEqual(2, king.RationalityPerTurn());
                Assert.IsFalse(config.InvasionPlanner.PlanSpawns(14).Any(o => o.Definition.Id == "heretic_king"));
                return king.MaxHealth;
            }

            int easy = Health(Difficulty.Easy);
            int normal = Health(Difficulty.Normal);
            int hard = Health(Difficulty.Hard);

            Assert.Less(easy, normal);
            Assert.Less(normal, hard);
        }

        [Test]
        public void ExtraInvaders_OnlyAppearFromNormalUp()
        {
            bool Has(Difficulty difficulty, string id)
            {
                GameSessionConfig config = DefaultContent.CreateConfig();
                DifficultyPresets.Apply(config, difficulty);
                return Enumerable.Range(1, 20).Any(t => config.InvasionPlanner.PlanSpawns(t).Any(o => o.Definition.Id == id));
            }

            foreach (string id in new[] { "profane_guardian", "heretic_runner", "necro_apostle", "blast_scourge" })
            {
                Assert.IsFalse(Has(Difficulty.Easy, id), "Fácil: " + id);
                Assert.IsTrue(Has(Difficulty.Normal, id), "Normal: " + id);
                Assert.IsTrue(Has(Difficulty.Hard, id), "Difícil: " + id);
            }
        }

        [Test]
        public void TheTurnTenBoss_IsDrawnPerSeed_AllThreeAppear_AndAFixedSeedRepeats()
        {
            string BossFor(int seed)
            {
                GameSessionConfig config = GameConfigFactory.Build(new GameSettings { Seed = seed });
                return config.InvasionPlanner.PlanSpawns(10).First(o => o.Definition.IsBoss()).Definition.Id;
            }

            var seen = Enumerable.Range(1, 40).Select(BossFor).Distinct().ToList();

            CollectionAssert.AreEquivalent(new[] { "inquisitor", "time_prophet", "crimson_colossus" }, seen);
            Assert.AreEqual(BossFor(7), BossFor(7));
        }

        [Test]
        public void Escalation_KeepsTheNewAbilities()
        {
            IInvasionPlanner planner = DefaultContent.CreateInvasionPlanner(
                new InvasionPressure(2, 3, 3, 4, new InvaderEscalation(3, 1, 0), extraInvaders: true, finalBossHealth: 40));

            IInvaderDefinition guardian = planner.PlanSpawns(13).First(o => o.Definition.Id == "profane_guardian").Definition;
            IInvaderDefinition runner = planner.PlanSpawns(13).First(o => o.Definition.Id == "heretic_runner").Definition;
            IInvaderDefinition scourge = planner.PlanSpawns(12).First(o => o.Definition.Id == "blast_scourge").Definition;
            IInvaderDefinition king = planner.PlanSpawns(15).First(o => o.Definition.Id == "heretic_king").Definition;

            Assert.AreEqual(2, guardian.DamageReduction());
            Assert.Greater(guardian.MaxHealth, 10);
            Assert.IsTrue(runner.IgnoresBarriers());
            Assert.AreEqual(2, scourge.ExplodeDamage());
            Assert.IsTrue(king.IsBoss());
        }

        [Test]
        public void DescribeTraits_MentionsTheNewAbilities()
        {
            Assert.That(DefaultContent.CreateGuardian().DescribeTraits(), Does.Contain("golpes -2"));
            Assert.That(DefaultContent.CreateRunner().DescribeTraits(), Does.Contain("ignora barreiras"));
            Assert.That(DefaultContent.CreateNecroApostle().DescribeTraits(), Does.Contain("cura o anel"));
            Assert.That(DefaultContent.CreateScourge().DescribeTraits(), Does.Contain("explode"));
            Assert.That(DefaultContent.CreateMidBoss(1).DescribeTraits(), Does.Contain("gira o anel do meio"));
        }

        [Test]
        public void EveryNewInvader_HasABark_AndTheBossesHaveLastWords()
        {
            foreach (string id in new[] { "profane_guardian", "heretic_runner", "necro_apostle", "blast_scourge", "time_prophet", "crimson_colossus", "heretic_king" })
            {
                Assert.IsNotEmpty(Story.InvaderBark(id), id);
            }

            foreach (string id in new[] { "time_prophet", "crimson_colossus", "heretic_king" })
            {
                Assert.IsNotEmpty(Story.BossLastWords(id), id);
            }
        }

        [Test]
        public void EndlessPlanner_BringsTheNewInvadersAndExtraBosses()
        {
            var planner = new EndlessInvasionPlanner();
            string[] ids = Enumerable.Range(1, 60).SelectMany(t => planner.PlanSpawns(t)).Select(o => o.Definition.Id).Distinct().ToArray();

            foreach (string id in new[] { "profane_guardian", "heretic_runner", "necro_apostle", "blast_scourge", "time_prophet", "crimson_colossus", "heretic_king" })
            {
                CollectionAssert.Contains(ids, id);
            }
        }

        [Test]
        public void AFullGame_WithAllTheNewContent_RunsWithoutErrors()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                BalanceBot.Result result = BalanceBot.Play(new GameSettings { Difficulty = Difficulty.Hard, Seed = seed });
                Assert.GreaterOrEqual(result.Turn, 1);
            }
        }
    }
}
