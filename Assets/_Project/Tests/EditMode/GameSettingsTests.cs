using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Preferências do jogador (leitura/gravação tolerante) e o que cada dificuldade muda.
    /// </summary>
    public sealed class GameSettingsTests
    {
        [Test]
        public void Settings_RoundTripThroughText()
        {
            var original = new GameSettings { Difficulty = Difficulty.Hard, Fullscreen = true, Animation = AnimationSpeed.Fast, Seed = 12345 };

            GameSettings copy = GameSettings.Parse(original.Serialize());

            Assert.AreEqual(Difficulty.Hard, copy.Difficulty);
            Assert.IsTrue(copy.Fullscreen);
            Assert.AreEqual(AnimationSpeed.Fast, copy.Animation);
            Assert.AreEqual(12345, copy.Seed);
            Assert.AreEqual(2f, copy.AnimationMultiplier);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("lixo sem formato")]
        [TestCase(";;;===;;")]
        [TestCase("difficulty=abc;seed=;fullscreen=talvez")]
        [TestCase("difficulty=99;animation=-3;seed=-50")]
        public void Parse_NeverThrows_AndFallsBackToDefaults(string text)
        {
            GameSettings settings = GameSettings.Parse(text);

            Assert.AreEqual(Difficulty.Normal, settings.Difficulty);
            Assert.AreEqual(AnimationSpeed.Normal, settings.Animation);
            Assert.IsFalse(settings.Fullscreen);
            Assert.AreEqual(0, settings.Seed);
        }

        [Test]
        public void Parse_KeepsValidFieldsEvenIfOthersAreBroken()
        {
            GameSettings settings = GameSettings.Parse("difficulty=0;animation=7;seed=42;fullscreen=1");

            Assert.AreEqual(Difficulty.Easy, settings.Difficulty);
            Assert.AreEqual(AnimationSpeed.Normal, settings.Animation); // 7 não existe
            Assert.AreEqual(42, settings.Seed);
            Assert.IsTrue(settings.Fullscreen);
        }

        [Test]
        public void Seed_IsClampedToValidRange()
        {
            var settings = new GameSettings { Seed = int.MaxValue };
            Assert.AreEqual(GameSettings.MaxSeed, settings.Seed);

            settings.Seed = -5;
            Assert.AreEqual(0, settings.Seed);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var original = new GameSettings { Seed = 7 };
            GameSettings copy = original.Clone();

            copy.Seed = 8;
            copy.Difficulty = Difficulty.Easy;

            Assert.AreEqual(7, original.Seed);
            Assert.AreEqual(Difficulty.Normal, original.Difficulty);
        }

        [Test]
        public void InMemoryStore_SavesAndLoads()
        {
            ISettingsStore store = new InMemorySettingsStore();
            Assert.AreEqual(string.Empty, store.Load());

            store.Save(new GameSettings { Seed = 99 }.Serialize());

            Assert.AreEqual(99, GameSettings.Parse(store.Load()).Seed);
        }

        [Test]
        public void Difficulty_ChangesCoreHealthFaithAndInvasionGrowth()
        {
            GameSessionConfig easy = Configure(Difficulty.Easy);
            GameSessionConfig normal = Configure(Difficulty.Normal);
            GameSessionConfig hard = Configure(Difficulty.Hard);

            Assert.AreEqual(36, easy.CoreMaxHealth);
            Assert.AreEqual(30, normal.CoreMaxHealth);
            Assert.AreEqual(21, hard.CoreMaxHealth);

            Assert.AreEqual(4, easy.Faith.StartingFaith);
            Assert.AreEqual(11, easy.Faith.Capacity);
            Assert.AreEqual(3, normal.Faith.StartingFaith);
            Assert.AreEqual(2, hard.Faith.StartingFaith);
            Assert.AreEqual(8, hard.Faith.Capacity);

            // Turno 4 é a primeira repetição da onda de Cultistas: 1 + crescimento.
            Assert.AreEqual(2, CultistsAtTurn(easy, 4));
            Assert.AreEqual(4, CultistsAtTurn(normal, 4));
            Assert.AreEqual(5, CultistsAtTurn(hard, 4));
        }

        [Test]
        public void Difficulty_RespectsPlayerCustomizedFaithAndInvasion()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            var customFaith = new SovereignOfAshes.Economy.FaithEconomyConfig(9, 9, 1, 0);
            IInvasionPlanner customPlanner = config.InvasionPlanner;
            config.Faith = customFaith;

            DifficultyPresets.Apply(config, Difficulty.Hard, replaceFaith: false, replaceInvasion: false);

            Assert.AreSame(customFaith, config.Faith);
            Assert.AreSame(customPlanner, config.InvasionPlanner);
            Assert.AreEqual(21, config.CoreMaxHealth); // a vida ainda escala
        }

        [Test]
        public void EveryDifficultyBuildsAPlayableSession()
        {
            foreach (Difficulty difficulty in (Difficulty[])System.Enum.GetValues(typeof(Difficulty)))
            {
                GameSessionConfig config = Configure(difficulty);
                config.Seed = 5;

                using (var session = new GameSession(config))
                {
                    session.Start();
                    Assert.IsTrue(session.Player.CanAct, difficulty.ToString());
                    Assert.AreEqual(config.CoreMaxHealth, session.DivineCore.MaxHealth);
                }
            }
        }

        [Test]
        public void BestEndlessTurn_IsSaved_ButModeIsNot()
        {
            var original = new GameSettings { Mode = GameMode.Endless, BestEndlessTurn = 42 };

            GameSettings copy = GameSettings.Parse(original.Serialize());

            Assert.AreEqual(42, copy.BestEndlessTurn);
            Assert.AreEqual(GameMode.Standard, copy.Mode);
            Assert.AreEqual(GameMode.Endless, original.Clone().Mode);
        }

        [Test]
        public void EndlessPlanner_GrowsForever_AndIsDeterministic()
        {
            var planner = new EndlessInvasionPlanner();

            int Total(int turn) => planner.PlanSpawns(turn).Sum(o => o.Count);

            Assert.AreEqual(Total(37), Total(37));
            Assert.Greater(Total(60), Total(30));
            Assert.Greater(Total(30), Total(10));
            Assert.AreEqual(0, planner.PlanSpawns(0).Count);

            // Turno de orda dobra os Cultistas.
            Assert.AreEqual(EndlessInvasionPlanner.CultistsForTurn(10) * 2,
                planner.PlanSpawns(10).Where(o => o.Definition.Id == "ash_cultist").Sum(o => o.Count));
        }

        [Test]
        public void EndlessPlanner_InvadersGetStrongerEveryTier()
        {
            var planner = new EndlessInvasionPlanner();
            IInvaderDefinition early = planner.PlanSpawns(4).First(o => o.Definition.Id == "void_colossus").Definition;
            IInvaderDefinition late = planner.PlanSpawns(44).First(o => o.Definition.Id == "void_colossus").Definition;

            Assert.Greater(late.MaxHealth, early.MaxHealth);
            Assert.GreaterOrEqual(late.Attack, early.Attack);
        }

        [Test]
        public void EndlessSession_HasNoVictoryCondition()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.TurnsToSurvive = 0;
            config.InvasionPlanner = new EndlessInvasionPlanner();
            config.CoreMaxHealth = 100000; // aguenta para chegar bem além dos 20 turnos

            using (var session = new GameSession(config))
            {
                session.Start();

                for (int i = 0; i < 40 && session.Turns.State == SovereignOfAshes.Turns.TurnState.AwaitingPlayer; i++)
                {
                    session.Player.EndTurn();
                }

                Assert.AreEqual(SovereignOfAshes.Turns.TurnState.AwaitingPlayer, session.Turns.State);
                Assert.Greater(session.Turns.TurnNumber, 30);
            }
        }

        private static GameSessionConfig Configure(Difficulty difficulty)
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            DifficultyPresets.Apply(config, difficulty);
            return config;
        }

        private static int CultistsAtTurn(GameSessionConfig config, int turn) =>
            config.InvasionPlanner.PlanSpawns(turn).Where(o => o.Definition.Id == "ash_cultist").Sum(o => o.Count);
    }
}
