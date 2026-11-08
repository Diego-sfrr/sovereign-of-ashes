using UnityEngine;

namespace SovereignOfAshes.Cards.Effects
{
    /// <summary>
    /// Asset do efeito "Curar o Núcleo".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Effects &gt; Heal Core.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_HealCore", menuName = "Sovereign of Ashes/Cards/Effects/Heal Core")]
    public sealed class HealCoreEffectSO : CardEffectDefinitionSO
    {
        [SerializeField, Min(0)] private int _amount = 6;

        /// <inheritdoc />
        public override ITileEffect CreateEffect() => new HealCoreEffect(_amount);
    }
}
