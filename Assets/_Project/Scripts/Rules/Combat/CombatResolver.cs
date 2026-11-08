using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Rules.Combat
{
    /// <summary>
    /// Ponto único de entrada para dano e cura: passa tudo pelo <see cref="RuleModifierSystem"/>
    /// e só então aplica os efeitos finais nos combatentes.
    /// </summary>
    public sealed class CombatResolver
    {
        private readonly RuleModifierSystem _rules;

        /// <summary>
        /// Cria o resolvedor.
        /// </summary>
        /// <param name="rules">Pipeline de Mandamentos.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="rules"/> for nulo.</exception>
        public CombatResolver(RuleModifierSystem rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Causa dano ao alvo, respeitando os Mandamentos ativos.
        /// </summary>
        /// <param name="target">Alvo.</param>
        /// <param name="amount">Dano bruto.</param>
        /// <param name="source">Origem (opcional).</param>
        /// <returns>Efeitos efetivamente aplicados.</returns>
        public IReadOnlyList<RuleEffect> DealDamage(ICombatant target, int amount, object source = null)
        {
            return ResolveAndApply(RuleChannel.Damage, target, amount, source);
        }

        /// <summary>
        /// Cura o alvo, respeitando os Mandamentos ativos.
        /// </summary>
        /// <param name="target">Alvo.</param>
        /// <param name="amount">Cura bruta.</param>
        /// <param name="source">Origem (opcional).</param>
        /// <returns>Efeitos efetivamente aplicados.</returns>
        public IReadOnlyList<RuleEffect> Heal(ICombatant target, int amount, object source = null)
        {
            return ResolveAndApply(RuleChannel.Healing, target, amount, source);
        }

        private IReadOnlyList<RuleEffect> ResolveAndApply(RuleChannel channel, ICombatant target, int amount, object source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IReadOnlyList<RuleEffect> effects = _rules.Resolve(channel, amount, target, source);

            foreach (RuleEffect effect in effects)
            {
                if (effect.Target is ICombatant combatant)
                {
                    Apply(effect, combatant);
                }
            }

            return effects;
        }

        private static void Apply(RuleEffect effect, ICombatant combatant)
        {
            switch (effect.Channel)
            {
                case RuleChannel.Damage:
                    combatant.TakeDamage(effect.Amount);
                    break;
                case RuleChannel.Healing:
                    combatant.Heal(effect.Amount);
                    break;
                case RuleChannel.AttackBonus:
                    combatant.AddAttack(effect.Amount);
                    break;
                case RuleChannel.ResourceGain:
                    // Recursos não pertencem ao combatente; tratados pelo sistema de economia.
                    break;
            }
        }
    }
}
