using UnityEngine;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Asset do Mandamento "Transbordar é Fúria".
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Commandments &gt; Overheal To Attack.
    /// </summary>
    [CreateAssetMenu(fileName = "Commandment_OverhealToAttack", menuName = "Sovereign of Ashes/Commandments/Overheal To Attack")]
    public sealed class OverhealToAttackCommandmentSO : CommandmentDefinitionSO
    {
        [Tooltip("Quantos pontos de cura excedente geram 1 ponto de ataque.")]
        [SerializeField, Min(1)] private int _healthPerAttackPoint = 2;

        /// <inheritdoc />
        public override ICommandment CreateCommandment()
        {
            return new OverhealToAttackCommandment(Id, DisplayName, Priority, _healthPerAttackPoint);
        }
    }
}
