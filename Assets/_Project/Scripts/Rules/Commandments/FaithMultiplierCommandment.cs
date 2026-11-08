using System;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Mandamento "Dízimo Sagrado": multiplica toda Fé recebida por um percentual
    /// (ex.: 150 = +50%; 50 = metade). Atua no canal <see cref="RuleChannel.ResourceGain"/>.
    /// </summary>
    public sealed class FaithMultiplierCommandment : CommandmentBase
    {
        /// <summary>Percentual aplicado à renda (100 = sem alteração).</summary>
        public int MultiplierPercent { get; }

        /// <summary>
        /// Cria o Mandamento.
        /// </summary>
        /// <param name="id">Id único.</param>
        /// <param name="displayName">Nome exibido.</param>
        /// <param name="priority">Prioridade.</param>
        /// <param name="multiplierPercent">Percentual (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="multiplierPercent"/> for negativo.</exception>
        public FaithMultiplierCommandment(string id, string displayName, int priority, int multiplierPercent)
            : base(id, displayName, priority)
        {
            if (multiplierPercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplierPercent), multiplierPercent, "Não pode ser negativo.");
            }

            MultiplierPercent = multiplierPercent;
        }

        /// <inheritdoc />
        public override bool AppliesTo(RuleContext context)
        {
            return context != null && context.Channel == RuleChannel.ResourceGain && context.Amount > 0;
        }

        /// <inheritdoc />
        public override void Apply(RuleContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            long result = (long)context.Amount * MultiplierPercent / 100;
            context.SetAmount((int)Math.Min(int.MaxValue, result));
        }
    }
}
