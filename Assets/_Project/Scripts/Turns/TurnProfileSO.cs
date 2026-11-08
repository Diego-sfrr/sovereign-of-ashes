using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using UnityEngine;

namespace SovereignOfAshes.Turns
{
    /// <summary>
    /// Regras de ritmo da partida editáveis no Inspector: rotações tectônicas, pressão da Racionalidade e condição de vitória.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Turns &gt; Turn Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "TurnProfile", menuName = "Sovereign of Ashes/Turns/Turn Profile")]
    public sealed class TurnProfileSO : ScriptableObject
    {
        /// <summary>
        /// Entrada serializável de uma regra de rotação periódica.
        /// </summary>
        [Serializable]
        public sealed class RingShiftEntry
        {
            [SerializeField] private RingType _ring = RingType.OuterWilderness;
            [SerializeField, Min(0)] private int _steps = 1;
            [SerializeField] private RotationDirection _direction = RotationDirection.Clockwise;

            [Tooltip("1 = todo turno, 3 = a cada 3 turnos.")]
            [SerializeField, Min(1)] private int _everyNTurns = 1;

            /// <summary>
            /// Converte a entrada em regra lógica.
            /// </summary>
            /// <returns>Regra validada.</returns>
            public RingShiftRule ToRule() =>
                new RingShiftRule(new RingRotation(_ring, _steps, _direction), Math.Max(1, _everyNTurns));
        }

        [Header("Rotação Tectônica")]
        [SerializeField] private List<RingShiftEntry> _ringShifts = new List<RingShiftEntry>();

        [Header("Livre-Arbítrio")]
        [Tooltip("Racionalidade que cada civilização ganha por turno.")]
        [SerializeField, Min(0)] private int _rationalityGrowthPerTurn = 1;

        [Header("Vitória")]
        [Tooltip("Turnos a sobreviver para vencer. 0 = sem vitória por sobrevivência.")]
        [SerializeField, Min(0)] private int _turnsToSurvive = 20;

        /// <summary>Racionalidade ganha por turno.</summary>
        public int RationalityGrowthPerTurn => _rationalityGrowthPerTurn;

        /// <summary>Turnos a sobreviver para vencer.</summary>
        public int TurnsToSurvive => _turnsToSurvive;

        /// <summary>
        /// Cria o planejador tectônico a partir das entradas configuradas.
        /// </summary>
        /// <returns>Planejador pronto para a <c>TectonicShiftPhase</c>.</returns>
        public ITectonicShiftPlanner CreatePlanner()
        {
            var rules = new List<RingShiftRule>(_ringShifts.Count);

            foreach (RingShiftEntry entry in _ringShifts)
            {
                if (entry != null)
                {
                    rules.Add(entry.ToRule());
                }
            }

            return new PatternTectonicShiftPlanner(rules);
        }
    }
}
