using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Receita alquímica criada pelo designer: <c>Reagente + Catalisador = Resultado</c>.
    /// Adicionar uma nova fusão ao jogo = criar um novo asset, sem tocar em código (OCP).
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Alchemy &gt; Recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Sovereign of Ashes/Alchemy/Recipe")]
    public sealed class AlchemyRecipeSO : ScriptableObject, IAlchemyRecipe
    {
        [Tooltip("Elemento que já está no setor.")]
        [SerializeField] private ElementDefinitionSO _reagent;

        [Tooltip("Conceito da carta jogada sobre o setor.")]
        [SerializeField] private ConceptType _catalyst;

        [Tooltip("Elemento gerado pela fusão.")]
        [SerializeField] private ElementDefinitionSO _result;

        /// <inheritdoc />
        public IElementDefinition Reagent => _reagent;

        /// <inheritdoc />
        public ConceptType Catalyst => _catalyst;

        /// <inheritdoc />
        public IElementDefinition Result => _result;

        /// <summary>Indica se a receita está completa (reagente e resultado preenchidos).</summary>
        public bool IsValid => _reagent != null && _result != null;

        private void OnValidate()
        {
            if (_reagent != null && _reagent == _result)
            {
                Debug.LogWarning($"[{nameof(AlchemyRecipeSO)}] '{name}': reagente e resultado são o mesmo elemento.", this);
            }
        }
    }
}
