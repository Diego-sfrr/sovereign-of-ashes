using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Receita de transmutação: <c>Reagente (no setor) + Catalisador (carta) = Resultado</c>.
    /// </summary>
    public interface IAlchemyRecipe
    {
        /// <summary>Elemento que já ocupa o setor.</summary>
        IElementDefinition Reagent { get; }

        /// <summary>Conceito da carta jogada sobre o setor.</summary>
        ConceptType Catalyst { get; }

        /// <summary>Elemento produzido pela fusão.</summary>
        IElementDefinition Result { get; }
    }

    /// <summary>
    /// Fonte de receitas consultada pelo <see cref="AlchemySystem"/>.
    /// Permite trocar a origem dos dados (ScriptableObject, JSON, mock de teste) sem alterar o sistema (DIP/OCP).
    /// </summary>
    public interface IAlchemyRecipeProvider
    {
        /// <summary>
        /// Obtém o elemento base manifestado quando um conceito é jogado em um setor vazio.
        /// </summary>
        /// <param name="concept">Conceito da carta.</param>
        /// <param name="element">Elemento base, se houver.</param>
        /// <returns><c>true</c> se o conceito possui um elemento base registrado.</returns>
        bool TryGetBaseElement(ConceptType concept, out IElementDefinition element);

        /// <summary>
        /// Obtém a receita para um reagente e um catalisador.
        /// </summary>
        /// <param name="reagent">Elemento que ocupa o setor.</param>
        /// <param name="catalyst">Conceito jogado.</param>
        /// <param name="recipe">Receita encontrada, se houver.</param>
        /// <returns><c>true</c> se existe uma receita para a combinação.</returns>
        bool TryGetRecipe(IElementDefinition reagent, ConceptType catalyst, out IAlchemyRecipe recipe);
    }

    /// <summary>
    /// Capacidade opcional de um <see cref="IAlchemyRecipeProvider"/>: permitir listar tudo o que conhece
    /// (usada pelo Grimório da interface). Fontes que não sabem enumerar simplesmente não a implementam.
    /// </summary>
    public interface IAlchemyRecipeCatalog
    {
        /// <summary>Todas as receitas, na ordem de cadastro.</summary>
        IReadOnlyList<IAlchemyRecipe> Recipes { get; }

        /// <summary>Elemento base manifestado por cada conceito em setor vazio.</summary>
        IReadOnlyDictionary<ConceptType, IElementDefinition> BaseElements { get; }
    }
}
