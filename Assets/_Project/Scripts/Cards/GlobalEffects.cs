using System;
using System.Collections.Generic;
using SovereignOfAshes.Civilization;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Influência divina sobre todas as civilizações (ex.: "Sermão" reduz a Racionalidade;
    /// "Recrutar" aumenta o Poder Militar mas desperta a Razão). Não precisa de alvo.
    /// </summary>
    public sealed class CivilizationInfluenceEffect : ITileEffect
    {
        /// <summary>Variação de Racionalidade aplicada a cada civilização.</summary>
        public int RationalityDelta { get; }

        /// <summary>Variação de Fé aplicada a cada civilização.</summary>
        public int FaithDelta { get; }

        /// <summary>Variação de Poder Militar aplicada a cada civilização.</summary>
        public int MilitaryPowerDelta { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="rationalityDelta">Variação de Racionalidade.</param>
        /// <param name="faithDelta">Variação de Fé.</param>
        /// <param name="militaryPowerDelta">Variação de Poder Militar.</param>
        public CivilizationInfluenceEffect(int rationalityDelta, int faithDelta, int militaryPowerDelta)
        {
            RationalityDelta = rationalityDelta;
            FaithDelta = faithDelta;
            MilitaryPowerDelta = militaryPowerDelta;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Cópia defensiva: eventos de heresia podem alterar a coleção.
            foreach (CivilizationEntity civilization in new List<CivilizationEntity>(context.Services.Civilizations))
            {
                if (civilization == null)
                {
                    continue;
                }

                if (FaithDelta != 0)
                {
                    civilization.ChangeFaith(FaithDelta);
                }

                if (MilitaryPowerDelta != 0)
                {
                    civilization.ChangeMilitaryPower(MilitaryPowerDelta);
                }

                if (RationalityDelta != 0)
                {
                    civilization.ChangeRationality(RationalityDelta);
                }
            }
        }
    }

    /// <summary>
    /// Milagre: cura o Núcleo Divino (a cura passa pelos Mandamentos, então "A Cura é Ferida" a transforma em dano).
    /// </summary>
    public sealed class HealCoreEffect : ITileEffect
    {
        /// <summary>Cura bruta.</summary>
        public int Amount { get; }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="amount">Cura bruta (não negativa).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for negativo.</exception>
        public HealCoreEffect(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Não pode ser negativo.");
            }

            Amount = amount;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="context"/> for nulo.</exception>
        public void Apply(TileEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Amount > 0)
            {
                context.Combat.Heal(context.Services.DivineCore, Amount, context.Source);
            }
        }
    }
}
