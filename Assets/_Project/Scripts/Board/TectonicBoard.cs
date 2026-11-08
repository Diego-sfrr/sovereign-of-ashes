using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SovereignOfAshes.Board
{
    /// <summary>
    /// A Crosta Viva: três anéis concêntricos de N setores ao redor do Núcleo Divino.
    /// Lógica pura (POCO), sem dependência de Unity, totalmente testável.
    /// </summary>
    public sealed class TectonicBoard
    {
        /// <summary>
        /// Mínimo de setores por anel para que os vizinhos horário e anti-horário sejam setores distintos.
        /// </summary>
        public const int MinSectorsPerRing = 3;

        private readonly TectonicRing[] _rings;
        private readonly ReadOnlyCollection<TectonicRing> _readOnlyRings;

        /// <summary>
        /// Disparado após um anel girar. Parâmetros: anel, deslocamento efetivo com sinal.
        /// A camada de visão usa este evento para animar a rotação.
        /// </summary>
        public event Action<RingType, int> RingRotated;

        /// <summary>Quantidade de setores em cada anel (N).</summary>
        public int SectorsPerRing { get; }

        /// <summary>Anéis ordenados do mais interno para o mais externo.</summary>
        public IReadOnlyList<TectonicRing> Rings => _readOnlyRings;

        /// <summary>
        /// Cria o tabuleiro.
        /// </summary>
        /// <param name="sectorsPerRing">Quantidade de setores por anel (mínimo <see cref="MinSectorsPerRing"/>).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="sectorsPerRing"/> for menor que o mínimo.</exception>
        public TectonicBoard(int sectorsPerRing)
        {
            if (sectorsPerRing < MinSectorsPerRing)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sectorsPerRing),
                    sectorsPerRing,
                    $"Cada anel precisa de pelo menos {MinSectorsPerRing} setores.");
            }

            SectorsPerRing = sectorsPerRing;

            RingType[] ringTypes = (RingType[])Enum.GetValues(typeof(RingType));
            Array.Sort(ringTypes);
            _rings = new TectonicRing[ringTypes.Length];

            for (int i = 0; i < ringTypes.Length; i++)
            {
                _rings[i] = new TectonicRing(ringTypes[i], sectorsPerRing);
            }

            _readOnlyRings = Array.AsReadOnly(_rings);
        }

        /// <summary>
        /// Retorna o anel do tipo informado.
        /// </summary>
        /// <param name="ring">Tipo do anel.</param>
        /// <returns>O anel correspondente.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se o tipo não existir no tabuleiro.</exception>
        public TectonicRing GetRing(RingType ring)
        {
            int index = (int)ring;

            if (index < 0 || index >= _rings.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(ring), ring, "Anel inexistente no tabuleiro.");
            }

            return _rings[index];
        }

        /// <summary>
        /// Retorna o setor que ocupa uma posição angular de um anel neste turno.
        /// </summary>
        /// <param name="ring">Anel.</param>
        /// <param name="position">Posição angular (normalizada por módulo).</param>
        /// <returns>O setor alinhado.</returns>
        public TectonicSector GetSectorAt(RingType ring, int position)
        {
            return GetRing(ring).GetSectorAtPosition(position);
        }

        /// <summary>
        /// Retorna a posição angular atual de um setor.
        /// </summary>
        /// <param name="sector">Setor pertencente a este tabuleiro.</param>
        /// <returns>Posição em [0, N).</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        /// <exception cref="ArgumentException">Se o setor não pertencer a este tabuleiro.</exception>
        public int GetPositionOf(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            return GetRing(sector.Ring).GetPositionOf(sector);
        }

        /// <summary>
        /// Gira um anel. Rotações nulas (0 passos ou múltiplos de N) não disparam evento.
        /// </summary>
        /// <param name="ring">Anel a girar.</param>
        /// <param name="steps">Quantidade de posições (não negativa).</param>
        /// <param name="direction">Sentido da rotação.</param>
        public void RotateRing(RingType ring, int steps, RotationDirection direction)
        {
            int delta = GetRing(ring).Rotate(steps, direction);

            if (delta != 0)
            {
                RingRotated?.Invoke(ring, delta);
            }
        }

        /// <summary>
        /// Aplica um lote de rotações (fase tectônica do fim do turno).
        /// </summary>
        /// <param name="rotations">Rotações a aplicar, na ordem.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="rotations"/> for nulo.</exception>
        public void ApplyRotations(IEnumerable<RingRotation> rotations)
        {
            if (rotations == null)
            {
                throw new ArgumentNullException(nameof(rotations));
            }

            foreach (RingRotation rotation in rotations)
            {
                RotateRing(rotation.Ring, rotation.Steps, rotation.Direction);
            }
        }

        /// <summary>
        /// Calcula a vizinhança dinâmica de um setor neste turno:
        /// vizinhos laterais do mesmo anel + vizinhos radiais alinhados nos anéis interno e externo.
        /// </summary>
        /// <param name="sector">Setor pertencente a este tabuleiro.</param>
        /// <returns>Fotografia da vizinhança atual.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        /// <exception cref="ArgumentException">Se o setor não pertencer a este tabuleiro.</exception>
        public SectorNeighbors GetNeighbors(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            TectonicRing ring = GetRing(sector.Ring);
            int position = ring.GetPositionOf(sector);
            int ringIndex = (int)sector.Ring;

            TectonicSector clockwise = ring.GetSectorAtPosition(position + 1);
            TectonicSector counterClockwise = ring.GetSectorAtPosition(position - 1);
            TectonicSector inner = TryGetRingAt(ringIndex - 1)?.GetSectorAtPosition(position);
            TectonicSector outer = TryGetRingAt(ringIndex + 1)?.GetSectorAtPosition(position);
            bool touchesCore = ringIndex == 0;

            return new SectorNeighbors(sector, clockwise, counterClockwise, inner, outer, touchesCore);
        }

        /// <summary>
        /// Enumera todos os setores do tabuleiro, do anel interno para o externo.
        /// </summary>
        /// <returns>Sequência de todos os setores.</returns>
        public IEnumerable<TectonicSector> GetAllSectors()
        {
            foreach (TectonicRing ring in _rings)
            {
                foreach (TectonicSector sector in ring.Sectors)
                {
                    yield return sector;
                }
            }
        }

        private TectonicRing TryGetRingAt(int index)
        {
            return index >= 0 && index < _rings.Length ? _rings[index] : null;
        }
    }
}
