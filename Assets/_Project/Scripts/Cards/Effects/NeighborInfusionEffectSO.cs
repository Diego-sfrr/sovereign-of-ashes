using SovereignOfAshes.Core;
using UnityEngine;

namespace SovereignOfAshes.Cards.Effects
{
    /// <summary>
    /// Asset do efeito em área "Infundir nos Vizinhos".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Effects &gt; Neighbor Infusion.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_InfuseNeighbors_", menuName = "Sovereign of Ashes/Cards/Effects/Neighbor Infusion")]
    public sealed class NeighborInfusionEffectSO : CardEffectDefinitionSO
    {
        [SerializeField] private ConceptType _concept;

        [Tooltip("Se marcado, o setor alvo também recebe o conceito.")]
        [SerializeField] private bool _includeTarget = true;

        /// <inheritdoc />
        public override ITileEffect CreateEffect() => new NeighborInfusionEffect(_concept, _includeTarget);
    }
}
