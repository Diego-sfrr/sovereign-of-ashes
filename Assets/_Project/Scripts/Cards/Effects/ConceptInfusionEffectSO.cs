using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Cards.Effects
{
    /// <summary>
    /// Asset do efeito "Infundir Conceito" no setor alvo.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Effects &gt; Concept Infusion.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Infuse_", menuName = "Sovereign of Ashes/Cards/Effects/Concept Infusion")]
    public sealed class ConceptInfusionEffectSO : CardEffectDefinitionSO
    {
        [SerializeField] private ConceptType _concept;

        /// <inheritdoc />
        public override ITileEffect CreateEffect() => new ConceptInfusionEffect(_concept);
    }
}
