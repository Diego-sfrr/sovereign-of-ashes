using System;
using SovereignOfAshes.Economy;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// O que cada dificuldade muda na configuração da partida. Só mexe em números (vida do Núcleo, Fé inicial e
    /// capacidade, crescimento das ondas); as regras são as mesmas.
    /// </summary>
    public static class DifficultyPresets
    {
        /// <summary>Nome exibido.</summary>
        /// <param name="difficulty">Dificuldade.</param>
        /// <returns>Nome em português.</returns>
        public static string Name(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return "Fácil";
                case Difficulty.Hard: return "Difícil";
                default: return "Normal";
            }
        }

        /// <summary>Descrição curta do que a dificuldade muda.</summary>
        /// <param name="difficulty">Dificuldade.</param>
        /// <returns>Texto para a tela de configurações.</returns>
        public static string Describe(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return "Núcleo com 36 de vida, 4 de Fé inicial (até 11) e invasões bem mais fracas, para aprender sem sufoco.";
                case Difficulty.Hard:
                    return "Núcleo com 21 de vida, 2 de Fé inicial (até 8) e invasões que crescem mais rápido: pede planejamento.";
                default:
                    return "O equilíbrio padrão: Núcleo com 30 de vida, 3 de Fé inicial (até 10).";
            }
        }

        /// <summary>Multiplicador da vida do Núcleo.</summary>
        /// <param name="difficulty">Dificuldade.</param>
        /// <returns>1,2 (fácil), 1 (normal) ou 0,7 (difícil).</returns>
        public static float CoreHealthMultiplier(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 1.2f;
                case Difficulty.Hard: return 0.7f;
                default: return 1f;
            }
        }

        /// <summary>Invasores a mais que cada repetição da onda de Cultistas traz.</summary>
        /// <param name="difficulty">Dificuldade.</param>
        /// <returns>1 (fácil), 2 (normal) ou 3 (difícil).</returns>
        public static int CultistGrowthPerRepeat(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 1;
                case Difficulty.Hard: return 3;
                default: return 2;
            }
        }

        /// <summary>
        /// Aplica a dificuldade a uma configuração.
        /// </summary>
        /// <param name="config">Configuração a alterar.</param>
        /// <param name="difficulty">Dificuldade.</param>
        /// <param name="replaceFaith">Se a economia de Fé pode ser trocada (falso quando o jogador configurou a dele).</param>
        /// <param name="replaceInvasion">Se as ondas podem ser trocadas (falso quando o jogador configurou as dele).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="config"/> for nula.</exception>
        public static void Apply(GameSessionConfig config, Difficulty difficulty, bool replaceFaith = true, bool replaceInvasion = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.CoreMaxHealth = Math.Max(1, (int)Math.Round(config.CoreMaxHealth * CoreHealthMultiplier(difficulty)));

            if (replaceFaith && difficulty != Difficulty.Normal)
            {
                config.Faith = difficulty == Difficulty.Easy
                    ? new FaithEconomyConfig(startingFaith: 4, capacity: 11, baseIncomePerTurn: 2, civilizationYieldPercent: 20)
                    : new FaithEconomyConfig(startingFaith: 2, capacity: 8, baseIncomePerTurn: 2, civilizationYieldPercent: 20);
            }

            if (replaceInvasion)
            {
                config.InvasionPlanner = DefaultContent.CreateInvasionPlanner(CultistGrowthPerRepeat(difficulty));
            }
        }
    }
}
