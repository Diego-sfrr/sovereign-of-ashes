using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Rules
{
    /// <summary>
    /// Canal de cálculo que os Mandamentos podem interceptar.
    /// </summary>
    public enum RuleChannel
    {
        /// <summary>Dano a ser causado.</summary>
        Damage = 0,

        /// <summary>Cura a ser aplicada.</summary>
        Healing = 1,

        /// <summary>Geração de recursos (ex.: Fé, Essência).</summary>
        ResourceGain = 2,

        /// <summary>Bônus permanente de ataque.</summary>
        AttackBonus = 3
    }

    /// <summary>
    /// Alvo de um cálculo de regra. Exposto como leitura para que Mandamentos tomem decisões
    /// (ex.: quanto de cura excede a vida máxima) sem poder alterar o alvo diretamente.
    /// </summary>
    public interface IRuleTarget
    {
        /// <summary>Vida atual.</summary>
        int CurrentHealth { get; }

        /// <summary>Vida máxima.</summary>
        int MaxHealth { get; }
    }

    /// <summary>
    /// Efeito final, já processado pelos Mandamentos, pronto para ser aplicado por quem pediu o cálculo.
    /// </summary>
    public readonly struct RuleEffect
    {
        /// <summary>Canal do efeito.</summary>
        public RuleChannel Channel { get; }

        /// <summary>Quantidade (sempre positiva).</summary>
        public int Amount { get; }

        /// <summary>Alvo do efeito (pode ser nulo para recursos globais).</summary>
        public IRuleTarget Target { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="channel">Canal.</param>
        /// <param name="amount">Quantidade.</param>
        /// <param name="target">Alvo.</param>
        public RuleEffect(RuleChannel channel, int amount, IRuleTarget target)
        {
            Channel = channel;
            Amount = amount;
            Target = target;
        }

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Texto "Canal: quantidade".</returns>
        public override string ToString() => $"{Channel}: {Amount}";
    }

    /// <summary>
    /// Contexto mutável que percorre o pipeline de Mandamentos (Chain of Responsibility).
    /// Cada Mandamento pode alterar a quantidade, redirecionar parte dela para outro canal ou interromper a cadeia.
    /// </summary>
    public sealed class RuleContext
    {
        private readonly List<RuleEffect> _redirections = new List<RuleEffect>();

        /// <summary>Canal sendo calculado.</summary>
        public RuleChannel Channel { get; }

        /// <summary>Quantidade original, antes de qualquer Mandamento.</summary>
        public int BaseAmount { get; }

        /// <summary>Quantidade atual, após os Mandamentos já executados (nunca negativa).</summary>
        public int Amount { get; private set; }

        /// <summary>Alvo do cálculo (pode ser nulo).</summary>
        public IRuleTarget Target { get; }

        /// <summary>Origem do efeito (carta, civilização, relíquia...). Pode ser nula.</summary>
        public object Source { get; }

        /// <summary>Profundidade na cadeia de redirecionamentos (0 = efeito original).</summary>
        public int Depth { get; }

        /// <summary>Se algum Mandamento interrompeu a cadeia.</summary>
        public bool IsPropagationStopped { get; private set; }

        /// <summary>Efeitos secundários gerados pelos Mandamentos neste contexto.</summary>
        public IReadOnlyList<RuleEffect> Redirections => _redirections;

        /// <summary>
        /// Cria o contexto.
        /// </summary>
        /// <param name="channel">Canal.</param>
        /// <param name="amount">Quantidade original (não negativa).</param>
        /// <param name="target">Alvo.</param>
        /// <param name="source">Origem.</param>
        /// <param name="depth">Profundidade na cadeia.</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> ou <paramref name="depth"/> forem negativos.</exception>
        public RuleContext(RuleChannel channel, int amount, IRuleTarget target, object source, int depth)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "A quantidade não pode ser negativa.");
            }

            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "A profundidade não pode ser negativa.");
            }

            Channel = channel;
            BaseAmount = amount;
            Amount = amount;
            Target = target;
            Source = source;
            Depth = depth;
        }

        /// <summary>
        /// Define a nova quantidade (valores negativos viram zero).
        /// </summary>
        /// <param name="amount">Nova quantidade.</param>
        public void SetAmount(int amount)
        {
            Amount = Math.Max(0, amount);
        }

        /// <summary>
        /// Gera um efeito secundário em outro canal (ex.: cura vira dano).
        /// O efeito secundário também passa pelo pipeline, respeitando o limite de profundidade.
        /// </summary>
        /// <param name="channel">Canal do novo efeito.</param>
        /// <param name="amount">Quantidade (valores menores ou iguais a zero são ignorados).</param>
        /// <param name="target">Alvo do novo efeito.</param>
        public void Redirect(RuleChannel channel, int amount, IRuleTarget target)
        {
            if (amount > 0)
            {
                _redirections.Add(new RuleEffect(channel, amount, target));
            }
        }

        /// <summary>
        /// Impede que Mandamentos de prioridade menor processem este contexto.
        /// </summary>
        public void StopPropagation()
        {
            IsPropagationStopped = true;
        }
    }
}
