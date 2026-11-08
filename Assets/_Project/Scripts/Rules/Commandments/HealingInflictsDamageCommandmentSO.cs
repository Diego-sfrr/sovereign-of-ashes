using UnityEngine;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Asset do Mandamento "A Cura é Ferida".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Commandments &gt; Healing Inflicts Damage.
    /// </summary>
    [CreateAssetMenu(fileName = "Commandment_HealingInflictsDamage", menuName = "Sovereign of Ashes/Commandments/Healing Inflicts Damage")]
    public sealed class HealingInflictsDamageCommandmentSO : CommandmentDefinitionSO
    {
        [Tooltip("Percentual da cura convertido em dano.")]
        [SerializeField, Min(0)] private int _conversionPercent = 100;

        /// <inheritdoc />
        public override ICommandment CreateCommandment()
        {
            return new HealingInflictsDamageCommandment(Id, DisplayName, Priority, _conversionPercent);
        }
    }
}
