using UnityEngine;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Asset do Mandamento "Dízimo Sagrado".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Commandments &gt; Faith Multiplier.
    /// </summary>
    [CreateAssetMenu(fileName = "Commandment_FaithMultiplier", menuName = "Sovereign of Ashes/Commandments/Faith Multiplier")]
    public sealed class FaithMultiplierCommandmentSO : CommandmentDefinitionSO
    {
        [Tooltip("Percentual aplicado a toda Fé recebida (100 = sem alteração, 150 = +50%).")]
        [SerializeField, Min(0)] private int _multiplierPercent = 150;

        /// <inheritdoc />
        public override ICommandment CreateCommandment()
        {
            return new FaithMultiplierCommandment(Id, DisplayName, Priority, _multiplierPercent);
        }
    }
}
