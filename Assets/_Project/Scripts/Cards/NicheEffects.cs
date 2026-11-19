using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Dano a todos os invasores do anel do setor alvo: a resposta às hordas, que se espalham por um anel inteiro.
    /// </summary>
    public sealed class RingStrikeEffect : ITileEffect
    {
        /// <summary>Dano a cada invasor do anel.</summary>
        public int Damage { get; }

        /// <summary>Cria o efeito.</summary>
        /// <param name="damage">Dano a cada invasor (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se o dano for negativo.</exception>
        public RingStrikeEffect(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Não pode ser negativo.");
            }

            Damage = damage;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Damage == 0 || !context.HasTarget)
            {
                return;
            }

            // Alvos definidos antes do primeiro golpe, para o resultado não depender da ordem.
            var targets = new List<InvaderEntity>();

            foreach (TectonicSector sector in context.Board.GetRing(context.Target.Ring).Sectors)
            {
                InvaderEntity invader = context.Invaders.GetInvaderAt(sector);

                if (invader != null && !invader.IsDead)
                {
                    targets.Add(invader);
                }
            }

            foreach (InvaderEntity invader in targets)
            {
                context.Combat.DealDamage(invader, Damage, context.Source);
            }

            context.Invaders.RemoveDead();
        }
    }

    /// <summary>
    /// Execução: mata na hora o invasor do setor se ele tiver pouca vida; se for forte demais, só o fere.
    /// </summary>
    public sealed class ExecuteEffect : ITileEffect
    {
        /// <summary>Vida atual máxima para a execução matar.</summary>
        public int SlayBelowOrEqual { get; }

        /// <summary>Dano causado quando o invasor é forte demais.</summary>
        public int FallbackDamage { get; }

        /// <summary>Cria o efeito.</summary>
        /// <param name="slayBelowOrEqual">Vida atual até a qual o invasor morre (maior que zero).</param>
        /// <param name="fallbackDamage">Dano contra os mais fortes (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for inválido.</exception>
        public ExecuteEffect(int slayBelowOrEqual, int fallbackDamage)
        {
            if (slayBelowOrEqual <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slayBelowOrEqual), slayBelowOrEqual, "Deve ser maior que zero.");
            }

            if (fallbackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fallbackDamage), fallbackDamage, "Não pode ser negativo.");
            }

            SlayBelowOrEqual = slayBelowOrEqual;
            FallbackDamage = fallbackDamage;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InvaderEntity invader = context.HasTarget ? context.Invaders.GetInvaderAt(context.Target) : null;

            if (invader == null || invader.IsDead)
            {
                return;
            }

            int damage = invader.CurrentHealth <= SlayBelowOrEqual ? invader.CurrentHealth : FallbackDamage;

            if (damage > 0)
            {
                context.Combat.DealDamage(invader, damage, context.Source);
            }

            context.Invaders.RemoveDead();
        }
    }
}
