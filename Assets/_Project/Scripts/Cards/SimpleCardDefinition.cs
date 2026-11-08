using System;
using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Carta definida em código puro (conteúdo padrão e testes). Alternativa ao <see cref="CardDefinitionSO"/>.
    /// </summary>
    public sealed class SimpleCardDefinition : ICardDefinition
    {
        private readonly List<ITileEffect> _effects;

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public int Cost { get; }

        /// <inheritdoc />
        public ConceptType PrimaryConcept { get; }

        /// <inheritdoc />
        public TargetRings AllowedRings { get; }

        /// <inheritdoc />
        public bool RequiresEmptySector { get; }

        /// <inheritdoc />
        public bool RequiresInvader { get; }

        /// <inheritdoc />
        public IReadOnlyList<ITileEffect> Effects => _effects;

        /// <summary>
        /// Cria a carta.
        /// </summary>
        /// <param name="id">Id único (não vazio).</param>
        /// <param name="displayName">Nome exibido.</param>
        /// <param name="description">Texto da carta.</param>
        /// <param name="cost">Custo em Fé (não negativo).</param>
        /// <param name="primaryConcept">Conceito principal.</param>
        /// <param name="allowedRings">Anéis permitidos (<see cref="TargetRings.None"/> = carta global).</param>
        /// <param name="requiresEmptySector">Exige setor vazio.</param>
        /// <param name="requiresInvader">Exige invasor no setor.</param>
        /// <param name="effects">Efeitos em ordem (nulos são ignorados).</param>
        /// <exception cref="ArgumentException">Se <paramref name="id"/> for vazio.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="cost"/> for negativo.</exception>
        /// <exception cref="ArgumentNullException">Se <paramref name="effects"/> for nulo.</exception>
        public SimpleCardDefinition(
            string id,
            string displayName,
            string description,
            int cost,
            ConceptType primaryConcept,
            TargetRings allowedRings,
            bool requiresEmptySector,
            bool requiresInvader,
            IEnumerable<ITileEffect> effects)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("O id não pode ser vazio.", nameof(id));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Não pode ser negativo.");
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Description = description ?? string.Empty;
            Cost = cost;
            PrimaryConcept = primaryConcept;
            AllowedRings = allowedRings;
            RequiresEmptySector = requiresEmptySector;
            RequiresInvader = requiresInvader;

            _effects = new List<ITileEffect>();

            foreach (ITileEffect effect in effects)
            {
                if (effect != null)
                {
                    _effects.Add(effect);
                }
            }
        }
    }
}
