using System;
using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy.Effects
{
    /// <summary>
    /// Catálogo em memória: associa o id de cada elemento aos seus efeitos.
    /// </summary>
    public sealed class ElementEffectCatalog : IElementEffectProvider
    {
        private static readonly IReadOnlyList<ElementEffect> None = new ElementEffect[0];

        private readonly Dictionary<string, List<ElementEffect>> _effects = new Dictionary<string, List<ElementEffect>>();

        /// <summary>
        /// Adiciona efeitos a um elemento (acumula se já existirem).
        /// </summary>
        /// <param name="elementId">Id do elemento (não vazio).</param>
        /// <param name="effects">Efeitos (nulos são ignorados).</param>
        /// <returns>Este catálogo, para encadear chamadas.</returns>
        /// <exception cref="ArgumentException">Se <paramref name="elementId"/> for vazio.</exception>
        /// <exception cref="ArgumentNullException">Se <paramref name="effects"/> for nulo.</exception>
        public ElementEffectCatalog Add(string elementId, params ElementEffect[] effects)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                throw new ArgumentException("O id do elemento não pode ser vazio.", nameof(elementId));
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (!_effects.TryGetValue(elementId, out List<ElementEffect> list))
            {
                list = new List<ElementEffect>();
                _effects[elementId] = list;
            }

            foreach (ElementEffect effect in effects)
            {
                if (effect != null)
                {
                    list.Add(effect);
                }
            }

            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<ElementEffect> GetEffects(IElementDefinition element)
        {
            return element != null && element.Id != null && _effects.TryGetValue(element.Id, out List<ElementEffect> list)
                ? list
                : None;
        }
    }
}
