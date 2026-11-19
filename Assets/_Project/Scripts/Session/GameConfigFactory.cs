using System;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Monta a configuração de uma partida a partir das preferências do jogador (dificuldade, modo, civilização, pacto).
    /// Fica fora do MonoBehaviour para o jogo e os testes de equilíbrio usarem exatamente a mesma receita.
    /// </summary>
    public static class GameConfigFactory
    {
        /// <summary>Configuração padrão do jogo com as preferências aplicadas.</summary>
        /// <param name="settings">Preferências (nulo = padrão).</param>
        /// <returns>Configuração pronta para criar uma <see cref="GameSession"/>.</returns>
        public static GameSessionConfig Build(GameSettings settings)
        {
            settings = settings ?? new GameSettings();
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = settings.Seed;
            ApplySettings(config, settings, replaceFaith: true, replaceInvasion: true);
            return config;
        }

        /// <summary>
        /// Aplica dificuldade, civilização, pacto e modo infinito, nesta ordem (o pacto vem depois da dificuldade
        /// porque pode mexer em números que ela já escalou).
        /// </summary>
        /// <param name="config">Configuração a alterar.</param>
        /// <param name="settings">Preferências.</param>
        /// <param name="replaceFaith">Se a economia de Fé pode ser trocada pela da dificuldade.</param>
        /// <param name="replaceInvasion">Se as ondas podem ser trocadas pelas da dificuldade (ignorado no modo infinito).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public static void ApplySettings(GameSessionConfig config, GameSettings settings, bool replaceFaith, bool replaceInvasion)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            bool endless = settings.Mode == GameMode.Endless;

            // No modo infinito a dificuldade é sempre a mais dura (hardcore).
            DifficultyPresets.Apply(config, endless ? Difficulty.Hard : settings.Difficulty,
                replaceFaith, replaceInvasion: replaceInvasion && !endless);

            // A civilização escolhida troca a padrão (vem antes do pacto).
            CivilizationCatalog.Find(settings.CivilizationId)?.Apply(config);
            CovenantCatalog.Find(settings.CovenantId)?.Apply(config);

            if (endless)
            {
                config.TurnsToSurvive = 0;

                if (replaceInvasion)
                {
                    config.InvasionPlanner = new EndlessInvasionPlanner();
                }
            }
        }
    }
}
