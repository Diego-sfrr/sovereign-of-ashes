using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// O que um invasor vai fazer na próxima marcha.
    /// </summary>
    public enum MarchIntent
    {
        /// <summary>Avança um anel para dentro.</summary>
        Advance = 0,

        /// <summary>Já está no Santuário Interno: ataca o Núcleo Divino.</summary>
        AttackCore = 1,

        /// <summary>Quer avançar, mas o destino está ocupado ou há uma barreira (ex.: Golem de Ferro).</summary>
        Blocked = 2
    }

    /// <summary>
    /// Previsão imutável da marcha de um invasor, já considerando as rotações tectônicas planejadas.
    /// </summary>
    public readonly struct MarchPrediction
    {
        /// <summary>O que o invasor fará.</summary>
        public MarchIntent Intent { get; }

        /// <summary>Anel onde o invasor estará depois das rotações (o anel atual dele).</summary>
        public RingType Ring { get; }

        /// <summary>Posição angular do invasor depois das rotações. O alvo da marcha tem a mesma posição.</summary>
        public int Position { get; }

        /// <summary>Anel de destino (só vale para <see cref="MarchIntent.Advance"/> e <see cref="MarchIntent.Blocked"/>).</summary>
        public RingType TargetRing { get; }

        /// <summary>Indica se há um anel de destino (falso para <see cref="MarchIntent.AttackCore"/>).</summary>
        public bool HasTarget => Intent != MarchIntent.AttackCore;

        /// <summary>
        /// Cria a previsão.
        /// </summary>
        /// <param name="intent">Intenção.</param>
        /// <param name="ring">Anel após as rotações.</param>
        /// <param name="position">Posição após as rotações.</param>
        /// <param name="targetRing">Anel de destino.</param>
        public MarchPrediction(MarchIntent intent, RingType ring, int position, RingType targetRing)
        {
            Intent = intent;
            Ring = ring;
            Position = position;
            TargetRing = targetRing;
        }
    }

    /// <summary>
    /// Prevê a marcha dos invasores sem alterar nada (consulta pura): usa as rotações planejadas para o turno,
    /// a ocupação das placas e, opcionalmente, as barreiras. Permite à interface mostrar a "intenção" de cada invasor.
    /// <para>
    /// Espelha a regra da <c>InvaderAdvancePhase</c>: invasores do anel interno agem primeiro, então um destino
    /// ocupado só barra se o ocupante também não for andar.
    /// </para>
    /// </summary>
    public sealed class MarchForecaster
    {
        private readonly TectonicBoard _board;
        private readonly InvaderRoster _roster;
        private readonly Func<InvaderEntity, TectonicSector, bool> _wouldBlock;

        /// <summary>
        /// Cria o previsor.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="roster">Invasores.</param>
        /// <param name="wouldBlock">Diz se uma barreira impediria o invasor de entrar no setor (opcional).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="board"/> ou <paramref name="roster"/> forem nulos.</exception>
        public MarchForecaster(TectonicBoard board, InvaderRoster roster, Func<InvaderEntity, TectonicSector, bool> wouldBlock = null)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _wouldBlock = wouldBlock;
        }

        /// <summary>
        /// Prevê a marcha de um invasor.
        /// </summary>
        /// <param name="invader">Invasor vivo e ainda no tabuleiro.</param>
        /// <param name="plannedRotations">Rotações que acontecerão antes da marcha (podem ser vazias).</param>
        /// <returns>A previsão.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentException">Se o invasor não estiver no tabuleiro.</exception>
        public MarchPrediction Predict(InvaderEntity invader, IReadOnlyList<RingRotation> plannedRotations)
        {
            if (plannedRotations == null)
            {
                throw new ArgumentNullException(nameof(plannedRotations));
            }

            return PredictInternal(invader, ComputeOffsets(plannedRotations));
        }

        private int[] ComputeOffsets(IReadOnlyList<RingRotation> plannedRotations)
        {
            var offsets = new int[_board.Rings.Count];

            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = _board.Rings[i].RotationOffset;
            }

            int n = _board.SectorsPerRing;

            foreach (RingRotation rotation in plannedRotations)
            {
                int ring = (int)rotation.Ring;
                int delta = (rotation.Steps % n) * (int)rotation.Direction;
                offsets[ring] = ModularMath.Wrap(offsets[ring] + delta, n);
            }

            return offsets;
        }

        private MarchPrediction PredictInternal(InvaderEntity invader, int[] offsets)
        {
            if (invader == null)
            {
                throw new ArgumentNullException(nameof(invader));
            }

            if (invader.Sector == null)
            {
                throw new ArgumentException($"O invasor {invader} não está no tabuleiro.", nameof(invader));
            }

            int n = _board.SectorsPerRing;
            int ringIndex = (int)invader.Sector.Ring;
            int position = ModularMath.Wrap(invader.Sector.PlateIndex + offsets[ringIndex], n);

            if (ringIndex == 0)
            {
                return new MarchPrediction(MarchIntent.AttackCore, invader.Sector.Ring, position, invader.Sector.Ring);
            }

            var targetRing = (RingType)(ringIndex - 1);
            int plate = ModularMath.Wrap(position - offsets[ringIndex - 1], n);
            TectonicSector target = _board.GetRing(targetRing).Sectors[plate];
            bool blocked = _wouldBlock != null && _wouldBlock(invader, target);

            if (!blocked)
            {
                InvaderEntity occupant = _roster.GetInvaderAt(target);

                if (occupant != null && !occupant.IsDead && !ReferenceEquals(occupant, invader))
                {
                    // O ocupante (anel mais interno) age antes: se ele também andar, o destino fica livre.
                    blocked = PredictInternal(occupant, offsets).Intent != MarchIntent.Advance;
                }
            }

            return new MarchPrediction(blocked ? MarchIntent.Blocked : MarchIntent.Advance, invader.Sector.Ring, position, targetRing);
        }
    }
}
