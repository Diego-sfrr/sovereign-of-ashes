using System;
using SovereignOfAshes.Board;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Gira o anel do setor alvo. Os invasores e elementos que estão nele giram junto, então a carta
    /// reposiciona a marcha e alinha elementos com os alvos (e a previsão de marcha já reflete o novo estado).
    /// </summary>
    public sealed class RotateRingEffect : ITileEffect
    {
        /// <summary>Casas a girar (maior que zero).</summary>
        public int Steps { get; }

        /// <summary>Sentido do giro.</summary>
        public RotationDirection Direction { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="steps">Casas (maior que zero).</param>
        /// <param name="direction">Sentido.</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="steps"/> não for positivo.</exception>
        public RotateRingEffect(int steps, RotationDirection direction)
        {
            if (steps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "Deve ser maior que zero.");
            }

            Steps = steps;
            Direction = direction;
        }

        /// <inheritdoc />
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.HasTarget)
            {
                context.Board.RotateRing(context.Target.Ring, Steps, Direction);
            }
        }
    }

    /// <summary>
    /// Exílio: empurra o invasor do setor alvo um anel para fora (para o setor de mesma posição). Falha em silêncio
    /// se ele já está no anel externo ou se o destino está ocupado.
    /// </summary>
    public sealed class PushInvaderOutwardEffect : ITileEffect
    {
        /// <inheritdoc />
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.HasTarget)
            {
                return;
            }

            Invaders.InvaderEntity invader = context.Invaders.GetInvaderAt(context.Target);

            if (invader == null || context.Target.Ring == RingType.OuterWilderness)
            {
                return;
            }

            RingType outer = (RingType)((int)context.Target.Ring + 1);
            int position = context.Board.GetPositionOf(context.Target);
            TectonicSector destination = context.Board.GetSectorAt(outer, position);

            if (!context.Invaders.IsOccupied(destination))
            {
                context.Invaders.TryMove(invader, destination);
            }
        }
    }

    /// <summary>
    /// Dízimo de Sangue: o Núcleo paga vida (nunca o suficiente para morrer) e o jogador recebe Fé.
    /// </summary>
    public sealed class FaithTitheEffect : ITileEffect
    {
        /// <summary>Vida que o Núcleo perde (se tiver de sobra).</summary>
        public int CoreDamage { get; }

        /// <summary>Fé recebida.</summary>
        public int FaithGain { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="coreDamage">Vida paga (não negativa).</param>
        /// <param name="faithGain">Fé ganha (não negativa).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo.</exception>
        public FaithTitheEffect(int coreDamage, int faithGain)
        {
            if (coreDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coreDamage), coreDamage, "Não pode ser negativo.");
            }

            if (faithGain < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(faithGain), faithGain, "Não pode ser negativo.");
            }

            CoreDamage = coreDamage;
            FaithGain = faithGain;
        }

        /// <inheritdoc />
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Rules.Combat.ICombatant core = context.Services.DivineCore;
            int damage = Math.Min(CoreDamage, Math.Max(0, core.CurrentHealth - 1));

            if (damage > 0)
            {
                context.Combat.DealDamage(core, damage, context.Source);
            }

            if (FaithGain > 0 && context.Faith != null)
            {
                context.Faith.Add(FaithGain);
            }
        }
    }
}
