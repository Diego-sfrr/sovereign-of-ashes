using System;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Mandamento "Transbordar é Fúria": a cura que excede a vida máxima vira bônus de ataque.
    /// </summary>
    public sealed class OverhealToAttackCommandment : CommandmentBase
    {
        /// <summary>Quantos pontos de cura excedente geram 1 ponto de ataque.</summary>
        public int HealthPerAttackPoint { get; }

        /// <summary>
        /// Cria o Mandamento.
        /// </summary>
        /// <param name="id">Id único.</param>
        /// <param name="displayName">Nome exibido.</param>
        /// <param name="priority">Prioridade.</param>
        /// <param name="healthPerAttackPoint">Taxa de conversão (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="healthPerAttackPoint"/> for menor ou igual a zero.</exception>
        public OverhealToAttackCommandment(string id, string displayName, int priority, int healthPerAttackPoint = 1)
            : base(id, displayName, priority)
        {
            if (healthPerAttackPoint <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healthPerAttackPoint), healthPerAttackPoint, "Deve ser maior que zero.");
            }

            HealthPerAttackPoint = healthPerAttackPoint;
        }

        /// <inheritdoc />
        public override bool AppliesTo(RuleContext context)
        {
            return context != null
                && context.Channel == RuleChannel.Healing
                && context.Target != null
                && context.Amount > 0;
        }

        /// <inheritdoc />
        public override void Apply(RuleContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int missingHealth = Math.Max(0, context.Target.MaxHealth - context.Target.CurrentHealth);
            int excess = context.Amount - missingHealth;

            if (excess <= 0)
            {
                return;
            }

            context.SetAmount(missingHealth);
            context.Redirect(RuleChannel.AttackBonus, excess / HealthPerAttackPoint, context.Target);
        }
    }
}
