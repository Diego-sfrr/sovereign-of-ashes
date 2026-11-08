using System;
using System.Collections.Generic;
using UnityEngine;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Roteiro de invasão editável no Inspector: as ondas e a semente das posições de surgimento.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Invaders &gt; Invasion Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "InvasionProfile", menuName = "Sovereign of Ashes/Invaders/Invasion Profile")]
    public sealed class InvasionProfileSO : ScriptableObject
    {
        /// <summary>
        /// Entrada serializável de uma onda.
        /// </summary>
        [Serializable]
        public sealed class WaveEntry
        {
            [SerializeField] private InvaderDefinitionSO _invader;
            [SerializeField, Min(1)] private int _firstTurn = 1;
            [SerializeField, Min(1)] private int _count = 1;

            [Tooltip("0 = acontece uma vez. 2 = repete a cada 2 turnos.")]
            [SerializeField, Min(0)] private int _everyNTurns = 2;

            [Tooltip("Invasores a mais em cada repetição.")]
            [SerializeField, Min(0)] private int _countGrowthPerRepeat;

            /// <summary>
            /// Converte a entrada em onda lógica.
            /// </summary>
            /// <returns>A onda, ou <c>null</c> se nenhum invasor foi atribuído.</returns>
            public InvasionWave ToWave()
            {
                if (_invader == null)
                {
                    return null;
                }

                return new InvasionWave(_invader, Math.Max(1, _firstTurn), Math.Max(1, _count),
                    Math.Max(0, _everyNTurns), Math.Max(0, _countGrowthPerRepeat));
            }
        }

        [SerializeField] private List<WaveEntry> _waves = new List<WaveEntry>();

        [Tooltip("Semente das posições de surgimento. 0 = aleatória a cada partida.")]
        [SerializeField] private int _spawnSeed;

        /// <summary>Semente configurada (0 = aleatória).</summary>
        public int SpawnSeed => _spawnSeed;

        /// <summary>
        /// Cria o planejador de invasão.
        /// </summary>
        /// <returns>Planejador pronto para a fase de surgimento.</returns>
        public IInvasionPlanner CreatePlanner()
        {
            var waves = new List<InvasionWave>(_waves.Count);

            foreach (WaveEntry entry in _waves)
            {
                InvasionWave wave = entry?.ToWave();

                if (wave != null)
                {
                    waves.Add(wave);
                }
            }

            return new WaveInvasionPlanner(waves);
        }
    }
}
