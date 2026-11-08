using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Alquimia Divina: aplica conceitos em setores.
    /// <list type="bullet">
    /// <item>Setor vazio: manifesta o elemento base do conceito.</item>
    /// <item>Setor ocupado: consulta o <see cref="IAlchemyRecipeProvider"/> e transmuta via <see cref="IElementFactory"/>.</item>
    /// </list>
    /// POCO sem dependência de Unity: as dependências chegam por construtor (DIP).
    /// </summary>
    public sealed class AlchemySystem
    {
        private readonly IAlchemyRecipeProvider _recipes;
        private readonly IElementFactory _factory;

        /// <summary>
        /// Disparado após qualquer aplicação de conceito (inclusive sem reação),
        /// para que a UI, áudio e efeitos em cadeia reajam sem acoplamento.
        /// </summary>
        public event Action<AlchemyResult> ReactionResolved;

        /// <summary>
        /// Cria o sistema.
        /// </summary>
        /// <param name="recipes">Fonte das receitas.</param>
        /// <param name="factory">Fábrica dos elementos resultantes.</param>
        /// <exception cref="ArgumentNullException">Se alguma dependência for nula.</exception>
        public AlchemySystem(IAlchemyRecipeProvider recipes, IElementFactory factory)
        {
            _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Aplica um conceito em um setor (manifestação ou transmutação).
        /// </summary>
        /// <param name="sector">Setor alvo.</param>
        /// <param name="concept">Conceito da carta jogada.</param>
        /// <returns>O resultado da operação.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        public AlchemyResult ApplyConcept(TectonicSector sector, ConceptType concept)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            AlchemyResult result = sector.IsEmpty
                ? Manifest(sector, concept)
                : Transmute(sector, concept);

            ReactionResolved?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Prevê o elemento que surgiria, sem alterar o setor (para pré-visualização na UI).
        /// </summary>
        /// <param name="sector">Setor alvo.</param>
        /// <param name="concept">Conceito a testar.</param>
        /// <param name="preview">Definição do elemento resultante, se houver reação.</param>
        /// <returns><c>true</c> se o conceito alteraria o setor.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        public bool TryPreview(TectonicSector sector, ConceptType concept, out IElementDefinition preview)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            if (sector.IsEmpty)
            {
                return _recipes.TryGetBaseElement(concept, out preview);
            }

            if (_recipes.TryGetRecipe(sector.Content.Definition, concept, out IAlchemyRecipe recipe) && recipe.Result != null)
            {
                preview = recipe.Result;
                return true;
            }

            preview = null;
            return false;
        }

        private AlchemyResult Manifest(TectonicSector sector, ConceptType concept)
        {
            if (!_recipes.TryGetBaseElement(concept, out IElementDefinition baseElement) || baseElement == null)
            {
                return new AlchemyResult(AlchemyOutcome.MissingBaseElement, sector, concept, null, null, null);
            }

            ISectorContent created = _factory.Create(baseElement, null);
            sector.SetContent(created);
            return new AlchemyResult(AlchemyOutcome.Manifested, sector, concept, null, created, null);
        }

        private AlchemyResult Transmute(TectonicSector sector, ConceptType concept)
        {
            ISectorContent previous = sector.Content;

            if (!_recipes.TryGetRecipe(previous.Definition, concept, out IAlchemyRecipe recipe)
                || recipe == null
                || recipe.Result == null)
            {
                return new AlchemyResult(AlchemyOutcome.NoReaction, sector, concept, previous, previous, null);
            }

            ISectorContent created = _factory.Create(recipe.Result, previous);
            sector.SetContent(created);
            return new AlchemyResult(AlchemyOutcome.Transmuted, sector, concept, previous, created, recipe);
        }
    }
}
