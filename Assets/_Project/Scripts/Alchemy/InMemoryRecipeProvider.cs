using System;
using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Definição de elemento em código puro (conteúdo padrão e testes).
    /// </summary>
    public sealed class SimpleElementDefinition : IElementDefinition
    {
        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public ElementKind Kind { get; }

        /// <summary>
        /// Cria a definição.
        /// </summary>
        /// <param name="id">Id único (não vazio).</param>
        /// <param name="displayName">Nome exibido (usa o id se vazio).</param>
        /// <param name="kind">Categoria.</param>
        /// <exception cref="ArgumentException">Se <paramref name="id"/> for vazio.</exception>
        public SimpleElementDefinition(string id, string displayName, ElementKind kind)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("O id não pode ser vazio.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Kind = kind;
        }
    }

    /// <summary>
    /// Grimório em memória: alternativa em código ao <see cref="AlchemyRecipeBookSO"/>,
    /// usado pelo conteúdo padrão e pelos testes.
    /// </summary>
    public sealed class InMemoryRecipeProvider : IAlchemyRecipeProvider, IAlchemyRecipeCatalog
    {
        private sealed class Recipe : IAlchemyRecipe
        {
            public Recipe(IElementDefinition reagent, ConceptType catalyst, IElementDefinition result)
            {
                Reagent = reagent;
                Catalyst = catalyst;
                Result = result;
            }

            public IElementDefinition Reagent { get; }

            public ConceptType Catalyst { get; }

            public IElementDefinition Result { get; }
        }

        private readonly Dictionary<ConceptType, IElementDefinition> _bases = new Dictionary<ConceptType, IElementDefinition>();
        private readonly Dictionary<(string reagentId, ConceptType catalyst), IAlchemyRecipe> _recipes =
            new Dictionary<(string reagentId, ConceptType catalyst), IAlchemyRecipe>();

        // Lista paralela para manter a ordem de cadastro (o dicionário serve só para a busca).
        private readonly List<IAlchemyRecipe> _recipeList = new List<IAlchemyRecipe>();

        /// <inheritdoc />
        public IReadOnlyList<IAlchemyRecipe> Recipes => _recipeList;

        /// <inheritdoc />
        public IReadOnlyDictionary<ConceptType, IElementDefinition> BaseElements => _bases;

        /// <summary>
        /// Registra o elemento manifestado por um conceito em setor vazio (substitui se já existir).
        /// </summary>
        /// <param name="concept">Conceito.</param>
        /// <param name="element">Elemento base.</param>
        /// <returns>Esta instância, para encadear chamadas.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="element"/> for nulo.</exception>
        public InMemoryRecipeProvider AddBase(ConceptType concept, IElementDefinition element)
        {
            _bases[concept] = element ?? throw new ArgumentNullException(nameof(element));
            return this;
        }

        /// <summary>
        /// Registra uma receita (substitui se a combinação já existir).
        /// </summary>
        /// <param name="reagent">Elemento no setor.</param>
        /// <param name="catalyst">Conceito jogado.</param>
        /// <param name="result">Resultado.</param>
        /// <returns>Esta instância, para encadear chamadas.</returns>
        /// <exception cref="ArgumentNullException">Se algum elemento for nulo.</exception>
        public InMemoryRecipeProvider AddRecipe(IElementDefinition reagent, ConceptType catalyst, IElementDefinition result)
        {
            if (reagent == null)
            {
                throw new ArgumentNullException(nameof(reagent));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var recipe = new Recipe(reagent, catalyst, result);
            var key = (reagent.Id, catalyst);

            if (_recipes.TryGetValue(key, out IAlchemyRecipe existing))
            {
                _recipeList[_recipeList.IndexOf(existing)] = recipe; // substitui mantendo a posição
            }
            else
            {
                _recipeList.Add(recipe);
            }

            _recipes[key] = recipe;
            return this;
        }

        /// <inheritdoc />
        public bool TryGetBaseElement(ConceptType concept, out IElementDefinition element) =>
            _bases.TryGetValue(concept, out element);

        /// <inheritdoc />
        public bool TryGetRecipe(IElementDefinition reagent, ConceptType catalyst, out IAlchemyRecipe recipe)
        {
            recipe = null;
            return reagent != null && !string.IsNullOrEmpty(reagent.Id) && _recipes.TryGetValue((reagent.Id, catalyst), out recipe);
        }
    }
}
