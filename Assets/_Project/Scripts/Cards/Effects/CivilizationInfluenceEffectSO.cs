using UnityEngine;

namespace SovereignOfAshes.Cards.Effects
{
    /// <summary>
    /// Asset do efeito "Influência sobre as Civilizações".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Cards &gt; Effects &gt; Civilization Influence.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Influence_", menuName = "Sovereign of Ashes/Cards/Effects/Civilization Influence")]
    public sealed class CivilizationInfluenceEffectSO : CardEffectDefinitionSO
    {
        [Tooltip("Negativo acalma a Razão (afasta a heresia).")]
        [SerializeField] private int _rationalityDelta = -3;

        [SerializeField] private int _faithDelta;

        [SerializeField] private int _militaryPowerDelta;

        /// <inheritdoc />
        public override ITileEffect CreateEffect() =>
            new CivilizationInfluenceEffect(_rationalityDelta, _faithDelta, _militaryPowerDelta);
    }
}
