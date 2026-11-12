using System.Collections.Generic;
using NUnit.Framework;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Turns.Phases;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// A previsão da marcha tem que bater exatamente com o que a fase real faz.
    /// </summary>
    public sealed class MarchForecastTests
    {
        private static readonly SimpleInvaderDefinition Dummy = new SimpleInvaderDefinition("dummy", "Teste", 50, 0, 0);

        private sealed class TestBarrier : ISectorBarrier
        {
            public readonly HashSet<TectonicSector> Blocked = new HashSet<TectonicSector>();

            public bool TryBlock(InvaderEntity invader, TectonicSector destination) => Blocked.Contains(destination);
        }

        [Test]
        public void Predict_FollowsPlannedRotationAndAlignment()
        {
            var board = new TectonicBoard(8);
            var roster = new InvaderRoster(board);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.OuterWilderness, 2), out InvaderEntity invader);
            var forecaster = new MarchForecaster(board, roster);
            var plan = new[] { new RingRotation(RingType.OuterWilderness, 1, RotationDirection.Clockwise) };

            MarchPrediction prediction = forecaster.Predict(invader, plan);

            Assert.AreEqual(MarchIntent.Advance, prediction.Intent);
            Assert.AreEqual(3, prediction.Position); // o anel girou 1 casa horária: posição 2 -> 3
            Assert.AreEqual(RingType.OuterWilderness, prediction.Ring);
            Assert.AreEqual(RingType.MiddleCivilization, prediction.TargetRing);
        }

        [Test]
        public void Predict_InnerInvaderAttacksCore_AndOccupiedDestinationBlocks()
        {
            var board = new TectonicBoard(8);
            var roster = new InvaderRoster(board);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.InnerSanctuary, 1), out InvaderEntity inner);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.MiddleCivilization, 1), out InvaderEntity middle);
            var forecaster = new MarchForecaster(board, roster);
            var none = new List<RingRotation>();

            Assert.AreEqual(MarchIntent.AttackCore, forecaster.Predict(inner, none).Intent);
            Assert.IsFalse(forecaster.Predict(inner, none).HasTarget);
            Assert.AreEqual(MarchIntent.Blocked, forecaster.Predict(middle, none).Intent); // o de dentro não sai do lugar
        }

        [Test]
        public void Predict_QueueMovesWhenFrontInvaderAlsoAdvances()
        {
            var board = new TectonicBoard(8);
            var roster = new InvaderRoster(board);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.MiddleCivilization, 4), out InvaderEntity front);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.OuterWilderness, 4), out InvaderEntity back);
            var forecaster = new MarchForecaster(board, roster);
            var none = new List<RingRotation>();

            Assert.AreEqual(MarchIntent.Advance, forecaster.Predict(front, none).Intent);
            Assert.AreEqual(MarchIntent.Advance, forecaster.Predict(back, none).Intent); // a frente libera o lugar
        }

        [Test]
        public void Predict_BarrierBlocks()
        {
            var board = new TectonicBoard(8);
            var roster = new InvaderRoster(board);
            roster.TrySpawn(Dummy, board.GetSectorAt(RingType.OuterWilderness, 5), out InvaderEntity invader);
            TectonicSector wall = board.GetSectorAt(RingType.MiddleCivilization, 5);
            var forecaster = new MarchForecaster(board, roster, (inv, sector) => ReferenceEquals(sector, wall));

            Assert.AreEqual(MarchIntent.Blocked, forecaster.Predict(invader, new List<RingRotation>()).Intent);
        }

        [Test]
        public void Predict_MatchesTheRealMarchPhase_InManyRandomScenarios()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var random = new System.Random(seed);
                int n = random.Next(4, 11);
                var board = new TectonicBoard(n);
                var roster = new InvaderRoster(board);
                var barrier = new TestBarrier();
                var core = new Combatant("Core", 100000, 0);

                var sectors = new List<TectonicSector>(board.GetAllSectors());
                int count = random.Next(1, 8);
                var invaders = new List<InvaderEntity>();

                for (int i = 0; i < count; i++)
                {
                    TectonicSector sector = sectors[random.Next(sectors.Count)];

                    if (roster.TrySpawn(Dummy, sector, out InvaderEntity invader))
                    {
                        invaders.Add(invader);
                    }
                }

                foreach (TectonicSector sector in sectors)
                {
                    if (random.NextDouble() < 0.1)
                    {
                        barrier.Blocked.Add(sector);
                    }
                }

                // Rotações iniciais arbitrárias, depois um plano de turno arbitrário.
                for (int r = 0; r < board.Rings.Count; r++)
                {
                    board.RotateRing((RingType)r, random.Next(0, n), random.Next(2) == 0 ? RotationDirection.Clockwise : RotationDirection.CounterClockwise);
                }

                var plan = new List<RingRotation>();

                for (int r = 0; r < board.Rings.Count; r++)
                {
                    if (random.NextDouble() < 0.7)
                    {
                        plan.Add(new RingRotation((RingType)r, random.Next(0, n + 2),
                            random.Next(2) == 0 ? RotationDirection.Clockwise : RotationDirection.CounterClockwise));
                    }
                }

                var forecaster = new MarchForecaster(board, roster, (inv, sector) => barrier.Blocked.Contains(sector));
                var predictions = new Dictionary<InvaderEntity, MarchPrediction>();

                foreach (InvaderEntity invader in invaders)
                {
                    predictions[invader] = forecaster.Predict(invader, plan);
                }

                board.ApplyRotations(plan);
                new InvaderAdvancePhase(roster, board, new CombatResolver(new RuleModifierSystem()), core, barrier).Execute(1);

                foreach (KeyValuePair<InvaderEntity, MarchPrediction> pair in predictions)
                {
                    InvaderEntity invader = pair.Key;
                    MarchPrediction p = pair.Value;
                    string context = $"semente {seed}, invasor {invader.Id}, intenção {p.Intent}";

                    Assert.AreEqual(p.Position % n, board.GetPositionOf(invader.Sector), context + " (posição)");

                    if (p.Intent == MarchIntent.Advance)
                    {
                        Assert.AreEqual(p.TargetRing, invader.Sector.Ring, context + " (anel de destino)");
                    }
                    else
                    {
                        Assert.AreEqual(p.Ring, invader.Sector.Ring, context + " (não deveria sair do anel)");
                    }
                }
            }
        }
    }
}
