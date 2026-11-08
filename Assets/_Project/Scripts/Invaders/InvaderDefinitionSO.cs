using UnityEngine;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Tipo de invasor editável no Inspector (ex.: Cultista Cinzento, Colosso do Vazio).
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Invaders &gt; Invader.
    /// </summary>
    [CreateAssetMenu(fileName = "Invader_", menuName = "Sovereign of Ashes/Invaders/Invader")]
    public sealed class InvaderDefinitionSO : ScriptableObject, IInvaderDefinition
    {
        [Tooltip("Id único. Deixe vazio para usar o nome do asset.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [TextArea]
        [SerializeField] private string _description;

        [SerializeField] private Sprite _icon;

        [Header("Atributos")]
        [SerializeField, Min(1)] private int _maxHealth = 6;

        [Tooltip("Dano por turno ao Núcleo depois de alcançar o Santuário Interno.")]
        [SerializeField, Min(0)] private int _attack = 2;

        [Tooltip("Fé ganha ao destruir este invasor.")]
        [SerializeField, Min(0)] private int _faithBounty = 1;

        /// <inheritdoc />
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <inheritdoc />
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <summary>Descrição para tooltip.</summary>
        public string Description => _description;

        /// <summary>Ícone exibido pela camada de visão.</summary>
        public Sprite Icon => _icon;

        /// <inheritdoc />
        public int MaxHealth => Mathf.Max(1, _maxHealth);

        /// <inheritdoc />
        public int Attack => Mathf.Max(0, _attack);

        /// <inheritdoc />
        public int FaithBounty => Mathf.Max(0, _faithBounty);
    }
}
