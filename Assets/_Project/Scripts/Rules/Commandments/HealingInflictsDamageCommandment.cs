using System;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Mandamento "A Cura é Ferida": toda cura é anulada e convertida em dano no mesmo alvo.
    /// </summary>
    public sealed class HealingInflictsDamageCommandment : CommandmentBase
    {
        /// <summary>Percentual da cura convertido em dano (100 = 1 para 1).</summary>
        public int ConversionPercent { get; }

        /// <summary>
        /// Cria o Mandamento.
        /// </summary>
        /// <param name="id">Id único.</param>
        /// <param name="displayName">Nome exibido.</param>
        /// <param name="priority">Prioridade.</param>
        /// <param name="conversionPercent">Percentual convertido (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="conversionPercent"/> for negativo.</exception>
        public HealingInflictsDamageCommandment(string id, string displayName, int priority, int conversionPercent = 100)
            : base(id, displayName, priority)
        {
            if (conversionPercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conversionPercent), conversionPercent, "Não pode ser negativo.");
            }

            ConversionPercent = conversionPercent;
        }

        /// <inheritdoc />
        public override bool AppliesTo(RuleContext context)
        {
            return context != null && context.Channel == RuleChannel.Healing && context.Amount > 0;
        }

        /// <inheritdoc />
        public override void Apply(RuleContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int damage = (int)Math.Min(int.MaxValue, (long)context.Amount * ConversionPercent / 100);
            context.SetAmount(0);
            context.Redirect(RuleChannel.Damage, damage, context.Target);
        }
    }
}
