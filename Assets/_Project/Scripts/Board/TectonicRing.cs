using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Board
{
    /// <summary>
    /// Um anel do tabuleiro. Guarda as placas por índice fixo e um deslocamento de rotação.
    /// <para>
    /// Modelo matemático: <c>posição = (índiceDaPlaca + deslocamento) mod N</c>.
    /// Girar o anel altera apenas o deslocamento (O(1)), nunca move objetos na memória.
    /// </para>
    /// </summary>
    public sealed class TectonicRing
    {
        private readonly TectonicSector[] _sectors;
        private readonly ReadOnlyCollection<TectonicSector> _readOnlySectors;

        /// <summary>Tipo (e distância ao núcleo) deste anel.</summary>
        public RingType Type { get; }

        /// <summary>Quantidade de setores (N).</summary>
        public int SectorCount => _sectors.Length;

        /// <summary>Deslocamento acumulado de rotação, sempre em [0, N).</summary>
        public int RotationOffset { get; private set; }

        /// <summary>Setores ordenados pelo índice fixo da placa (não pela posição atual).</summary>
        public IReadOnlyList<TectonicSector> Sectors => _readOnlySectors;

        /// <summary>
        /// Cria um anel com <paramref name="sectorCount"/> placas.
        /// </summary>
        /// <param name="type">Tipo do anel.</param>
        /// <param name="sectorCount">Quantidade de setores (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="sectorCount"/> for menor ou igual a zero.</exception>
        internal TectonicRing(RingType type, int sectorCount)
        {
            if (sectorCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sectorCount), sectorCount, "Um anel precisa de ao menos um setor.");
            }

            Type = type;
            _sectors = new TectonicSector[sectorCount];

            for (int i = 0; i < sectorCount; i++)
            {
                _sectors[i] = new TectonicSector(type, i);
            }

            _readOnlySectors = Array.AsReadOnly(_sectors);
        }

        /// <summary>
        /// Retorna o setor que ocupa a posição angular informada neste turno.
        /// Aceita qualquer inteiro: a posição é normalizada com aritmética modular.
        /// </summary>
        /// <param name="position">Posição angular (pode ser negativa ou maior que N).</param>
        /// <returns>O setor alinhado naquela posição.</returns>
        public TectonicSector GetSectorAtPosition(int position)
        {
            int plateIndex = ModularMath.Wrap(position - RotationOffset, SectorCount);
            return _sectors[plateIndex];
        }

        /// <summary>
        /// Retorna a posição angular atual de um setor deste anel.
        /// </summary>
        /// <param name="sector">Setor pertencente a este anel.</param>
        /// <returns>Posição em [0, N).</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        /// <exception cref="ArgumentException">Se o setor não pertencer a este anel.</exception>
        public int GetPositionOf(TectonicSector sector)
        {
            if (!Contains(sector))
            {
                throw new ArgumentException($"O setor {sector} não pertence ao anel {Type}.", nameof(sector));
            }

            return ModularMath.Wrap(sector.PlateIndex + RotationOffset, SectorCount);
        }

        /// <summary>
        /// Verifica se a instância do setor pertence a este anel.
        /// </summary>
        /// <param name="sector">Setor a verificar.</param>
        /// <returns><c>true</c> se for exatamente uma das placas deste anel.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        public bool Contains(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            return sector.Ring == Type
                && sector.PlateIndex < SectorCount
                && ReferenceEquals(_sectors[sector.PlateIndex], sector);
        }

        /// <summary>
        /// Gira o anel.
        /// </summary>
        /// <param name="steps">Quantidade de posições (não negativa).</param>
        /// <param name="direction">Sentido da rotação.</param>
        /// <returns>O deslocamento efetivo aplicado (com sinal), já reduzido módulo N.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="steps"/> for negativo.</exception>
        internal int Rotate(int steps, RotationDirection direction)
        {
            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "Use a direção para inverter o sentido; os passos não podem ser negativos.");
            }

            int effectiveSteps = steps % SectorCount;
            int delta = effectiveSteps * (int)direction;
            RotationOffset = ModularMath.Wrap(RotationOffset + delta, SectorCount);
            return delta;
        }
    }
}
