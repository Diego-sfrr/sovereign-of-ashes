using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Alchemy.Effects
{
    /// <summary>Queima todos os invasores do anel em que o elemento está, todo turno.</summary>
    public sealed class RingBurnEffect : ElementEffect
    {
        /// <summary>Dano por invasor.</summary>
        public int Damage { get; }

        /// <inheritdoc />
        public override string Description => $"Queima todos os invasores do próprio anel: {Damage} de dano por turno.";

        /// <summary>Cria o efeito.</summary>
        /// <param name="damage">Dano por invasor (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se o dano não for positivo.</exception>
        public RingBurnEffect(int damage)
        {
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Deve ser maior que zero.");
            }

            Damage = damage;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            // Alvos definidos antes do primeiro golpe, para o resultado não depender da ordem.
            var targets = new List<InvaderEntity>();

            foreach (TectonicSector sector in context.Services.Board.GetRing(context.Sector.Ring).Sectors)
            {
                InvaderEntity invader = context.Services.Invaders.GetInvaderAt(sector);

                if (invader != null && !invader.IsDead)
                {
                    targets.Add(invader);
                }
            }

            foreach (InvaderEntity invader in targets)
            {
                context.Services.Combat.DealDamage(invader, Damage, context.Element);
            }

            if (targets.Count > 0)
            {
                context.Report($"{context.Element.Definition.DisplayName} queimou {targets.Count} invasor(es) do anel ({Damage} de dano cada).", true);
            }
        }
    }

    /// <summary>Cura o Núcleo Divino todo turno.</summary>
    public sealed class CoreHealEffect : ElementEffect
    {
        /// <summary>Vida curada por turno.</summary>
        public int Amount { get; }

        /// <inheritdoc />
        public override string Description => $"Cura {Amount} de vida do Núcleo por turno.";

        /// <summary>Cria o efeito.</summary>
        /// <param name="amount">Cura por turno (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se a cura não for positiva.</exception>
        public CoreHealEffect(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Deve ser maior que zero.");
            }

            Amount = amount;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            if (context.Services.DivineCore.CurrentHealth >= context.Services.DivineCore.MaxHealth)
            {
                return;
            }

            context.Services.Combat.Heal(context.Services.DivineCore, Amount, context.Element);
            context.Report($"{context.Element.Definition.DisplayName} curou o Núcleo (+{Amount}).", true);
        }
    }

    /// <summary>Empurra para fora, um anel, os invasores do setor e dos vizinhos (quando o setor de destino está livre).</summary>
    public sealed class RepelEffect : ElementEffect
    {
        /// <inheritdoc />
        public override string Description => "Empurra um anel para fora os invasores do setor e dos vizinhos, todo turno.";

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            foreach (InvaderEntity invader in context.InvadersNearby(includeNeighbors: true))
            {
                if (invader.Sector.Ring == RingType.OuterWilderness)
                {
                    continue;
                }

                RingType outer = (RingType)((int)invader.Sector.Ring + 1);
                int position = context.Services.Board.GetPositionOf(invader.Sector);
                TectonicSector destination = context.Services.Board.GetSectorAt(outer, position);

                if (!context.Services.Invaders.IsOccupied(destination) && context.Services.Invaders.TryMove(invader, destination))
                {
                    context.Report($"{context.Element.Definition.DisplayName} repeliu {invader.Name} para o anel de fora.", true);
                }
            }
        }
    }

    /// <summary>Gira sozinho o anel em que está, todo turno: o tabuleiro nunca fica parado.</summary>
    public sealed class RotationDriftEffect : ElementEffect
    {
        /// <summary>Sentido do giro.</summary>
        public RotationDirection Direction { get; }

        /// <inheritdoc />
        public override string Description =>
            $"Gira o próprio anel 1 casa por turno, no sentido {(Direction == RotationDirection.Clockwise ? "horário" : "anti-horário")}.";

        /// <summary>Cria o efeito.</summary>
        /// <param name="direction">Sentido do giro.</param>
        public RotationDriftEffect(RotationDirection direction)
        {
            Direction = direction;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            context.Services.Board.RotateRing(context.Sector.Ring, 1, Direction);
            context.Report($"{context.Element.Definition.DisplayName} girou o anel por conta própria.", true);
        }
    }
}
