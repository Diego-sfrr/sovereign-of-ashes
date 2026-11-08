using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Dados estáticos de um elemento (conceito puro, entidade ou estrutura) que pode ocupar um setor.
    /// Criado pelo designer em: Create &gt; Sovereign of Ashes &gt; Alchemy &gt; Element.
    /// </summary>
    [CreateAssetMenu(fileName = "Element_", menuName = "Sovereign of Ashes/Alchemy/Element")]
    public sealed class ElementDefinitionSO : ScriptableObject, IElementDefinition
    {
        [Tooltip("Identificador único e estável. Não mude depois que receitas o referenciarem.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [SerializeField] private ElementKind _kind = ElementKind.Concept;

        [SerializeField] private Sprite _icon;

        [TextArea]
        [SerializeField] private string _description;

        /// <inheritdoc />
        public string Id => _id;

        /// <inheritdoc />
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <inheritdoc />
        public ElementKind Kind => _kind;

        /// <summary>Ícone exibido pela camada de visão.</summary>
        public Sprite Icon => _icon;

        /// <summary>Texto descritivo (lore / tooltip).</summary>
        public string Description => _description;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = name;
            }
        }
    }
}
