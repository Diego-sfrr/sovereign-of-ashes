using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Efeito de ataque: causa dano ao invasor do setor alvo e, opcionalmente, aos invasores dos setores vizinhos
    /// (laterais e radiais alinhados neste turno). O dano passa pelos Mandamentos; mortos são removidos
    /// na hora, o que dispara a recompensa em Fé.
    /// </summary>
    public sealed class InvaderStrikeEffect : ITileEffect
    {
        /// <summary>Dano bruto por invasor atingido.</summary>
        public int Damage { get; }

        /// <summary>Se os invasores dos setores vizinhos também são atingidos.</summary>
        public bool IncludeNeighbors { get; }

        /// <summary>Disparado para cada invasor atingido. Parâmetros: invasor, dano bruto.</summary>
        public event Action<InvaderEntity, int> InvaderHit;

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="damage">Dano bruto (não negativo).</param>
        /// <param name="includeNeighbors">Se atinge também os vizinhos.</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="damage"/> for negativo.</exception>
        public InvaderStrikeEffect(int damage, bool includeNeighbors)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Não pode ser negativo.");
            }

            Damage = damage;
            IncludeNeighbors = includeNeighbors;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Damage == 0)
            {
                return;
            }

            // Os alvos são definidos antes do primeiro golpe, para o resultado não depender da ordem.
            var targets = new List<InvaderEntity>();
            AddInvaderAt(context, context.Target, targets);

            if (IncludeNeighbors)
            {
                foreach (TectonicSector neighbor in context.Board.GetNeighbors(context.Target).All)
                {
                    AddInvaderAt(context, neighbor, targets);
                }
            }

            foreach (InvaderEntity invader in targets)
            {
                context.Combat.DealDamage(invader, Damage, context.Source);
                InvaderHit?.Invoke(invader, Damage);
            }

            context.Invaders.RemoveDead();
        }

        private static void AddInvaderAt(TileEffectContext context, TectonicSector sector, List<InvaderEntity> targets)
        {
            InvaderEntity invader = context.Invaders.GetInvaderAt(sector);

            if (invader != null && !invader.IsDead)
            {
                targets.Add(invader);
            }
        }
    }
}
