using UnityEngine;

namespace SovereignOfAshes.Cards.Effects
{
    /// <summary>
    /// Asset do efeito "Golpe nos Invasores".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Effects &gt; Invader Strike.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Strike_", menuName = "Sovereign of Ashes/Cards/Effects/Invader Strike")]
    public sealed class InvaderStrikeEffectSO : CardEffectDefinitionSO
    {
        [Tooltip("Dano bruto em cada invasor atingido (antes dos Mandamentos).")]
        [SerializeField, Min(0)] private int _damage = 4;

        [Tooltip("Se marcado, atinge também os invasores dos setores vizinhos (laterais e radiais).")]
        [SerializeField] private bool _includeNeighbors;

        /// <inheritdoc />
        public override ITileEffect CreateEffect() => new InvaderStrikeEffect(_damage, _includeNeighbors);
    }
}
