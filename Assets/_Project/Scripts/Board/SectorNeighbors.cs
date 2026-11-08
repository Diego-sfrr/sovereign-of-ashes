using System.Collections.Generic;

namespace SovereignOfAshes.Board
{
    /// <summary>
    /// Fotografia imutável da vizinhança de um setor no turno atual.
    /// Fica desatualizada após qualquer rotação; consulte novamente o <see cref="TectonicBoard"/>.
    /// </summary>
    public sealed class SectorNeighbors
    {
        private readonly List<TectonicSector> _all;

        /// <summary>Setor de origem da consulta.</summary>
        public TectonicSector Origin { get; }

        /// <summary>Vizinho lateral no sentido horário (mesmo anel).</summary>
        public TectonicSector Clockwise { get; }

        /// <summary>Vizinho lateral no sentido anti-horário (mesmo anel).</summary>
        public TectonicSector CounterClockwise { get; }

        /// <summary>Vizinho radial alinhado no anel interno, ou <c>null</c> se a origem estiver no anel mais interno.</summary>
        public TectonicSector Inner { get; }

        /// <summary>Vizinho radial alinhado no anel externo, ou <c>null</c> se a origem estiver no anel mais externo.</summary>
        public TectonicSector Outer { get; }

        /// <summary>Indica se a origem toca o Núcleo Divino (apenas setores do <see cref="RingType.InnerSanctuary"/>).</summary>
        public bool IsAdjacentToCore { get; }

        /// <summary>Todos os vizinhos existentes, sem nulos e sem duplicatas.</summary>
        public IReadOnlyList<TectonicSector> All => _all;

        /// <summary>
        /// Cria a fotografia de vizinhança.
        /// </summary>
        /// <param name="origin">Setor consultado.</param>
        /// <param name="clockwise">Vizinho horário.</param>
        /// <param name="counterClockwise">Vizinho anti-horário.</param>
        /// <param name="inner">Vizinho interno (pode ser nulo).</param>
        /// <param name="outer">Vizinho externo (pode ser nulo).</param>
        /// <param name="isAdjacentToCore">Se a origem toca o núcleo.</param>
        internal SectorNeighbors(
            TectonicSector origin,
            TectonicSector clockwise,
            TectonicSector counterClockwise,
            TectonicSector inner,
            TectonicSector outer,
            bool isAdjacentToCore)
        {
            Origin = origin;
            Clockwise = clockwise;
            CounterClockwise = counterClockwise;
            Inner = inner;
            Outer = outer;
            IsAdjacentToCore = isAdjacentToCore;

            _all = new List<TectonicSector>(4);
            AddUnique(clockwise);
            AddUnique(counterClockwise);
            AddUnique(inner);
            AddUnique(outer);
        }

        private void AddUnique(TectonicSector sector)
        {
            if (sector != null && !ReferenceEquals(sector, Origin) && !_all.Contains(sector))
            {
                _all.Add(sector);
            }
        }
    }
}
