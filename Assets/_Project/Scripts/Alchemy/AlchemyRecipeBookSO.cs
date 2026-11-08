using System;
using System.Collections.Generic;
using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Grimório: agrega os elementos base de cada conceito e todas as receitas do jogo.
    /// Indexa as receitas em um dicionário (busca O(1)), eliminando cadeias de if/else ou switch.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Alchemy &gt; Recipe Book.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeBook", menuName = "Sovereign of Ashes/Alchemy/Recipe Book")]
    public sealed class AlchemyRecipeBookSO : ScriptableObject, IAlchemyRecipeProvider
    {
        /// <summary>
        /// Associação entre um conceito e o elemento que ele manifesta em um setor vazio.
        /// </summary>
        [Serializable]
        public sealed class BaseConceptEntry
        {
            [SerializeField] private ConceptType _concept;
            [SerializeField] private ElementDefinitionSO _element;

            /// <summary>Conceito da carta.</summary>
            public ConceptType Concept => _concept;

            /// <summary>Elemento manifestado.</summary>
            public ElementDefinitionSO Element => _element;
        }

        [SerializeField] private List<BaseConceptEntry> _baseConcepts = new List<BaseConceptEntry>();
        [SerializeField] private List<AlchemyRecipeSO> _recipes = new List<AlchemyRecipeSO>();

        private Dictionary<ConceptType, IElementDefinition> _baseCache;
        private Dictionary<(string reagentId, ConceptType catalyst), IAlchemyRecipe> _recipeCache;

        /// <inheritdoc />
        public bool TryGetBaseElement(ConceptType concept, out IElementDefinition element)
        {
            EnsureCache();
            return _baseCache.TryGetValue(concept, out element);
        }

        /// <inheritdoc />
        public bool TryGetRecipe(IElementDefinition reagent, ConceptType catalyst, out IAlchemyRecipe recipe)
        {
            recipe = null;

            if (reagent == null || string.IsNullOrEmpty(reagent.Id))
            {
                return false;
            }

            EnsureCache();
            return _recipeCache.TryGetValue((reagent.Id, catalyst), out recipe);
        }

        private void OnEnable() => InvalidateCache();

        private void OnValidate() => InvalidateCache();

        private void InvalidateCache()
        {
            _baseCache = null;
            _recipeCache = null;
        }

        private void EnsureCache()
        {
            if (_baseCache != null && _recipeCache != null)
            {
                return;
            }

            _baseCache = new Dictionary<ConceptType, IElementDefinition>();
            _recipeCache = new Dictionary<(string, ConceptType), IAlchemyRecipe>();

            foreach (BaseConceptEntry entry in _baseConcepts)
            {
                if (entry == null || entry.Element == null)
                {
                    continue;
                }

                if (!_baseCache.ContainsKey(entry.Concept))
                {
                    _baseCache.Add(entry.Concept, entry.Element);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(AlchemyRecipeBookSO)}] Conceito base duplicado: {entry.Concept}. Mantendo o primeiro.", this);
                }
            }

            foreach (AlchemyRecipeSO recipe in _recipes)
            {
                if (recipe == null || !recipe.IsValid)
                {
                    continue;
                }

                var key = (recipe.Reagent.Id, recipe.Catalyst);

                if (!_recipeCache.ContainsKey(key))
                {
                    _recipeCache.Add(key, recipe);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(AlchemyRecipeBookSO)}] Receita duplicada para {key.Id} + {key.Catalyst} ('{recipe.name}'). Mantendo a primeira.", this);
                }
            }
        }
    }
}
