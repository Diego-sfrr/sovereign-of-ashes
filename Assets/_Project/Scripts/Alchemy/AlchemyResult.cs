using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Desfecho de aplicar um conceito em um setor.
    /// </summary>
    public enum AlchemyOutcome
    {
        /// <summary>Setor vazio recebeu o elemento base do conceito.</summary>
        Manifested = 0,

        /// <summary>O conteúdo do setor foi transmutado por uma receita.</summary>
        Transmuted = 1,

        /// <summary>Não existe receita para a combinação; o setor ficou inalterado.</summary>
        NoReaction = 2,

        /// <summary>O conceito não possui elemento base cadastrado; o setor ficou inalterado.</summary>
        MissingBaseElement = 3
    }

    /// <summary>
    /// Resultado imutável de uma operação alquímica, entregue a quem chamou e aos ouvintes do evento.
    /// </summary>
    public sealed class AlchemyResult
    {
        /// <summary>Desfecho da operação.</summary>
        public AlchemyOutcome Outcome { get; }

        /// <summary>Setor afetado.</summary>
        public TectonicSector Sector { get; }

        /// <summary>Conceito aplicado.</summary>
        public ConceptType Concept { get; }

        /// <summary>Conteúdo antes da operação (pode ser nulo).</summary>
        public ISectorContent Previous { get; }

        /// <summary>Conteúdo depois da operação (pode ser nulo).</summary>
        public ISectorContent Current { get; }

        /// <summary>Receita usada, se houve transmutação.</summary>
        public IAlchemyRecipe Recipe { get; }

        /// <summary>Indica se o setor mudou.</summary>
        public bool ChangedSector => Outcome == AlchemyOutcome.Manifested || Outcome == AlchemyOutcome.Transmuted;

        /// <summary>
        /// Cria o resultado.
        /// </summary>
        /// <param name="outcome">Desfecho.</param>
        /// <param name="sector">Setor afetado.</param>
        /// <param name="concept">Conceito aplicado.</param>
        /// <param name="previous">Conteúdo anterior.</param>
        /// <param name="current">Conteúdo atual.</param>
        /// <param name="recipe">Receita usada (opcional).</param>
        internal AlchemyResult(
            AlchemyOutcome outcome,
            TectonicSector sector,
            ConceptType concept,
            ISectorContent previous,
            ISectorContent current,
            IAlchemyRecipe recipe)
        {
            Outcome = outcome;
            Sector = sector;
            Concept = concept;
            Previous = previous;
            Current = current;
            Recipe = recipe;
        }
    }
}
