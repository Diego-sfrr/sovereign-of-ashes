using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Equilíbrio medido por simulação: um jogador automático mediano joga muitas partidas com sementes diferentes.
    /// As faixas abaixo são o que o jogo promete: Fácil acolhe, Normal exige atenção e Difícil castiga.
    /// </summary>
    public sealed class DifficultyBalanceTests
    {
        private const int Games = 40;

        private static (double winRate, double avgTurn, double avgCore) Measure(Difficulty difficulty, string civilizationId = null, string covenantId = null)
        {
            var results = Enumerable.Range(1, Games)
                .Select(seed => BalanceBot.Play(new GameSettings
                {
                    Difficulty = difficulty,
                    CivilizationId = civilizationId,
                    CovenantId = covenantId,
                    Seed = seed
                }))
                .ToList();

            return (results.Count(r => r.Won) / (double)Games, results.Average(r => r.Turn), results.Average(r => r.CoreHealth));
        }

        [Test]
        [Explicit("Relatório demorado: taxa de vitória do bot por dificuldade e civilização (saída no Console).")]
        public void Report_WinRates_ByDifficultyAndCivilization()
        {
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
            {
                var all = Measure(difficulty);
                Debug.Log($"[Balanco] {difficulty,-6} padrao: vitorias {all.winRate:P0}  turno medio {all.avgTurn:F1}  vida final {all.avgCore:F1}");

                foreach (CivilizationDefinition civ in CivilizationCatalog.All)
                {
                    var one = Measure(difficulty, civ.Id);
                    Debug.Log($"[Balanco] {difficulty,-6} {civ.Id,-18}: vitorias {one.winRate:P0}  turno medio {one.avgTurn:F1}  vida final {one.avgCore:F1}");
                }
            }

            Assert.Pass();
        }
    }
}

