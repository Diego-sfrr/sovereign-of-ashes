using System.Collections.Generic;
using System.Collections.ObjectModel;
using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Carta configurável no Inspector: metadados, regras de alvo e uma lista ordenada de efeitos.
    /// Os efeitos são assets (<see cref="CardEffectDefinitionSO"/>) que o designer combina como peças de LEGO;
    /// uma carta nova é só um asset novo, sem código novo.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Card.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "Sovereign of Ashes/Cards/Card")]
    public sealed class CardDefinitionSO : ScriptableObject, ICardDefinition
    {
        [Header("Identidade")]
        [Tooltip("Id único. Deixe vazio para usar o nome do asset.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [TextArea(2, 5)]
        [SerializeField] private string _description;

        [SerializeField] private Sprite _artwork;

        [SerializeField] private CardRarity _rarity = CardRarity.Common;

        [Header("Regras")]
        [SerializeField, Min(0)] private int _cost = 1;

        [SerializeField] private ConceptType _primaryConcept;

        [SerializeField] private TargetRings _allowedRings = TargetRings.All;

        [Tooltip("Se marcado, a carta não pode ser jogada sobre um setor ocupado.")]
        [SerializeField] private bool _requiresEmptySector;

        [Tooltip("Se marcado, a carta só pode ser jogada sobre um setor com invasor.")]
        [SerializeField] private bool _requiresInvader;

        [Header("Efeitos (executados em ordem)")]
        [SerializeField] private List<CardEffectDefinitionSO> _effects = new List<CardEffectDefinitionSO>();

        private ReadOnlyCollection<ITileEffect> _cachedEffects;

        /// <inheritdoc />
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <inheritdoc />
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <summary>Texto da carta.</summary>
        public string Description => _description;

        /// <summary>Arte da carta.</summary>
        public Sprite Artwork => _artwork;

        /// <summary>Raridade.</summary>
        public CardRarity Rarity => _rarity;

        /// <inheritdoc />
        public int Cost => _cost;

        /// <inheritdoc />
        public ConceptType PrimaryConcept => _primaryConcept;

        /// <inheritdoc />
        public TargetRings AllowedRings => _allowedRings;

        /// <inheritdoc />
        public bool RequiresEmptySector => _requiresEmptySector;

        /// <inheritdoc />
        public bool RequiresInvader => _requiresInvader;

        /// <inheritdoc />
        /// <remarks>
        /// As instâncias são criadas uma vez e reaproveitadas (os efeitos não guardam estado).
        /// Entradas nulas na lista são ignoradas.
        /// </remarks>
        public IReadOnlyList<ITileEffect> Effects
        {
            get
            {
                if (_cachedEffects == null)
                {
                    _cachedEffects = BuildEffects();
                }

                return _cachedEffects;
            }
        }

        private ReadOnlyCollection<ITileEffect> BuildEffects()
        {
            var built = new List<ITileEffect>(_effects.Count);

            foreach (CardEffectDefinitionSO definition in _effects)
            {
                if (definition == null)
                {
                    continue;
                }

                ITileEffect effect = definition.CreateEffect();

                if (effect != null)
                {
                    built.Add(effect);
                }
            }

            return built.AsReadOnly();
        }

        private void OnEnable() => _cachedEffects = null;

        private void OnValidate()
        {
            _cachedEffects = null;

            if (_effects.Count == 0)
            {
                Debug.LogWarning($"[{nameof(CardDefinitionSO)}] '{name}' não tem efeitos.", this);
            }
        }
    }
}
