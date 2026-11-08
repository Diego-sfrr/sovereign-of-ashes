using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Efeito básico de carta: infunde um conceito no setor alvo (manifesta ou transmuta).
    /// </summary>
    public sealed class ConceptInfusionEffect : ITileEffect
    {
        /// <summary>Conceito infundido.</summary>
        public ConceptType Concept { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="concept">Conceito a infundir.</param>
        public ConceptInfusionEffect(ConceptType concept)
        {
            Concept = concept;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.HasTarget)
            {
                return;
            }

            context.Alchemy.ApplyConcept(context.Target, Concept);
        }
    }

    /// <summary>
    /// Efeito em área: infunde um conceito em todos os vizinhos atuais do setor alvo
    /// (laterais e radiais), aproveitando o alinhamento tectônico do turno.
    /// </summary>
    public sealed class NeighborInfusionEffect : ITileEffect
    {
        /// <summary>Conceito infundido nos vizinhos.</summary>
        public ConceptType Concept { get; }

        /// <summary>Se o próprio setor alvo também recebe o conceito.</summary>
        public bool IncludeTarget { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="concept">Conceito a infundir.</param>
        /// <param name="includeTarget">Se o setor alvo também é afetado.</param>
        public NeighborInfusionEffect(ConceptType concept, bool includeTarget)
        {
            Concept = concept;
            IncludeTarget = includeTarget;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.HasTarget)
            {
                return;
            }

            // A vizinhança é capturada antes de alterar qualquer setor, para o resultado não depender da ordem.
            SectorNeighbors neighbors = context.Board.GetNeighbors(context.Target);

            if (IncludeTarget)
            {
                context.Alchemy.ApplyConcept(context.Target, Concept);
            }

            foreach (TectonicSector neighbor in neighbors.All)
            {
                context.Alchemy.ApplyConcept(neighbor, Concept);
            }
        }
    }
}
