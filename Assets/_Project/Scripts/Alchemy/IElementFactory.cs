using System;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Fábrica de elementos em tempo de execução (Factory Pattern).
    /// Centraliza a construção para que futuras regras (bônus de fusão, mutações, pooling)
    /// sejam adicionadas trocando a fábrica, sem alterar o <see cref="AlchemySystem"/>.
    /// </summary>
    public interface IElementFactory
    {
        /// <summary>
        /// Cria um elemento.
        /// </summary>
        /// <param name="definition">Definição do elemento a criar.</param>
        /// <param name="source">Conteúdo que foi transmutado, ou <c>null</c> se for uma manifestação em setor vazio.</param>
        /// <returns>O novo conteúdo do setor.</returns>
        ISectorContent Create(IElementDefinition definition, ISectorContent source);
    }

    /// <summary>
    /// Fábrica padrão: cria <see cref="SectorElement"/> e incrementa a profundidade de fusão.
    /// </summary>
    public sealed class DefaultElementFactory : IElementFactory
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="definition"/> for nula.</exception>
        public ISectorContent Create(IElementDefinition definition, ISectorContent source)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            int depth = source is SectorElement previous ? previous.FusionDepth + 1 : 0;
            return new SectorElement(definition, depth);
        }
    }
}
