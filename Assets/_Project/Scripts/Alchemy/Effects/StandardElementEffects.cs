using System;
using System.Collections.Generic;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Alchemy.Effects
{
    /// <summary>
    /// Chama contínua: todo turno queima o invasor que estiver no setor e, mais fraco, os dos setores vizinhos.
    /// (Forja Sagrada.)
    /// </summary>
    public sealed class BurnEffect : ElementEffect
    {
        /// <summary>Dano ao invasor no próprio setor.</summary>
        public int DamageOnSector { get; }

        /// <summary>Dano aos invasores nos setores vizinhos.</summary>
        public int DamageOnNeighbors { get; }

        /// <inheritdoc />
        public override string Description
        {
            get
            {
                string neighbors = DamageOnNeighbors > 0 ? $" e {DamageOnNeighbors} nos setores vizinhos" : string.Empty;
                return $"Queima invasores todo turno: {DamageOnSector} de dano no setor{neighbors}.";
            }
        }

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="damageOnSector">Dano no setor (não negativo).</param>
        /// <param name="damageOnNeighbors">Dano nos vizinhos (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum dano for negativo.</exception>
        public BurnEffect(int damageOnSector, int damageOnNeighbors)
        {
            if (damageOnSector < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damageOnSector), damageOnSector, "Não pode ser negativo.");
            }

            if (damageOnNeighbors < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damageOnNeighbors), damageOnNeighbors, "Não pode ser negativo.");
            }

            DamageOnSector = damageOnSector;
            DamageOnNeighbors = damageOnNeighbors;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            Strike(context, context.Services.Invaders.GetInvaderAt(context.Sector), DamageOnSector);

            if (DamageOnNeighbors <= 0)
            {
                return;
            }

            foreach (Board.TectonicSector neighbor in context.Services.Board.GetNeighbors(context.Sector).All)
            {
                Strike(context, context.Services.Invaders.GetInvaderAt(neighbor), DamageOnNeighbors);
            }
        }

        private static void Strike(ElementEffectContext context, InvaderEntity invader, int damage)
        {
            if (invader == null || invader.IsDead || damage <= 0)
            {
                return;
            }

            context.Services.Combat.DealDamage(invader, damage, context.Element);
            context.Report($"{context.Element.Definition.DisplayName} queimou {invader.Name} ({damage} de dano).", true);
        }
    }

    /// <summary>
    /// Barreira: nenhum invasor entra no setor. Quem tenta passar leva dano de volta, e cada tentativa
    /// desgasta o elemento até ele desmoronar. (Golem de Ferro.)
    /// </summary>
    public sealed class BarrierEffect : ElementEffect
    {
        /// <summary>Nome do contador de durabilidade em <see cref="SectorElement"/>.</summary>
        public const string DurabilityCounter = "durability";

        /// <summary>Quantas tentativas de passagem o elemento aguenta.</summary>
        public int MaxDurability { get; }

        /// <summary>Dano devolvido a cada invasor barrado.</summary>
        public int CounterDamage { get; }

        /// <inheritdoc />
        public override string Description =>
            $"Bloqueia a marcha dos invasores e fere quem tenta passar ({CounterDamage} de dano). Aguenta {MaxDurability} investidas.";

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="maxDurability">Durabilidade (maior que zero).</param>
        /// <param name="counterDamage">Dano de volta (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for inválido.</exception>
        public BarrierEffect(int maxDurability, int counterDamage)
        {
            if (maxDurability <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDurability), maxDurability, "Deve ser maior que zero.");
            }

            if (counterDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(counterDamage), counterDamage, "Não pode ser negativo.");
            }

            MaxDurability = maxDurability;
            CounterDamage = counterDamage;
        }

        /// <inheritdoc />
        public override bool BlocksInvader(ElementEffectContext context, InvaderEntity invader) => true;

        /// <inheritdoc />
        public override void OnInvaderBlocked(ElementEffectContext context, InvaderEntity invader)
        {
            string name = context.Element.Definition.DisplayName;

            if (CounterDamage > 0)
            {
                context.Services.Combat.DealDamage(invader, CounterDamage, context.Element);
            }

            int remaining = context.Element.GetCounter(DurabilityCounter, MaxDurability) - 1;
            context.Element.SetCounter(DurabilityCounter, remaining);

            if (remaining <= 0)
            {
                context.Sector.Clear();
                context.Report($"{name} barrou {invader.Name} e desmoronou.", false);
            }
            else
            {
                context.Report($"{name} barrou {invader.Name} ({remaining} de {MaxDurability} investidas restantes).", true);
            }
        }
    }

    /// <summary>
    /// Fonte de Fé: concede Fé todo turno (passando pelos Mandamentos). (Cinzas e Altar de Sacrifício.)
    /// </summary>
    public sealed class FaithSpringEffect : ElementEffect
    {
        /// <summary>Fé concedida por turno.</summary>
        public int Amount { get; }

        /// <inheritdoc />
        public override string Description => $"Rende +{Amount} de Fé por turno.";

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="amount">Fé por turno (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for menor ou igual a zero.</exception>
        public FaithSpringEffect(int amount)
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
            FaithIncomeReport report = context.Services.Faith.GrantFaith(Amount, context.Element);

            if (report.Deposited > 0)
            {
                context.Report($"{context.Element.Definition.DisplayName} rendeu +{report.Deposited} de Fé.", true);
            }
        }
    }

    /// <summary>
    /// Pulso sobre as civilizações: altera Razão, Poder Militar e/ou Fé de todas a cada turno. (Santo Vivo, Altar.)
    /// </summary>
    public sealed class CivilizationPulseEffect : ElementEffect
    {
        /// <summary>Variação de Racionalidade por turno.</summary>
        public int RationalityDelta { get; }

        /// <summary>Variação de Poder Militar por turno.</summary>
        public int MilitaryPowerDelta { get; }

        /// <summary>Variação de Fé por turno.</summary>
        public int FaithDelta { get; }

        /// <inheritdoc />
        public override string Description => $"Civilizações, por turno: {Summary()}.";

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="rationalityDelta">Variação de Racionalidade.</param>
        /// <param name="militaryPowerDelta">Variação de Poder Militar.</param>
        /// <param name="faithDelta">Variação de Fé.</param>
        public CivilizationPulseEffect(int rationalityDelta, int militaryPowerDelta, int faithDelta)
        {
            RationalityDelta = rationalityDelta;
            MilitaryPowerDelta = militaryPowerDelta;
            FaithDelta = faithDelta;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            // Cópia defensiva: a heresia dispara eventos que podem alterar a coleção.
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

            context.Report($"{context.Element.Definition.DisplayName} agiu sobre as civilizações ({Summary()}).", RationalityDelta <= 0);
        }

        private string Summary()
        {
            var parts = new List<string>();

            if (RationalityDelta != 0)
            {
                parts.Add($"{Signed(RationalityDelta)} de Razão");
            }

            if (MilitaryPowerDelta != 0)
            {
                parts.Add($"{Signed(MilitaryPowerDelta)} de Poder Militar");
            }

            if (FaithDelta != 0)
            {
                parts.Add($"{Signed(FaithDelta)} de Fé");
            }

            return string.Join(", ", parts);
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
    }

    /// <summary>
    /// Dreno de vida: fere os invasores no setor e nos vizinhos e cura o Núcleo Divino por cada um atingido. (Espectro.)
    /// </summary>
    public sealed class LifeDrainEffect : ElementEffect
    {
        /// <summary>Dano a cada invasor atingido.</summary>
        public int Damage { get; }

        /// <summary>Cura do Núcleo por invasor atingido.</summary>
        public int HealPerHit { get; }

        /// <inheritdoc />
        public override string Description =>
            $"Drena invasores no setor e nos vizinhos ({Damage} de dano) e cura o Núcleo em {HealPerHit} por invasor atingido.";

        /// <summary>
        /// Cria o efeito.
        /// </summary>
        /// <param name="damage">Dano (não negativo).</param>
        /// <param name="healPerHit">Cura por invasor atingido (não negativa).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo.</exception>
        public LifeDrainEffect(int damage, int healPerHit)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Não pode ser negativo.");
            }

            if (healPerHit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healPerHit), healPerHit, "Não pode ser negativa.");
            }

            Damage = damage;
            HealPerHit = healPerHit;
        }

        /// <inheritdoc />
        public override void OnTurnEnd(ElementEffectContext context)
        {
            // Os alvos são definidos antes do primeiro golpe, para o resultado não depender da ordem.
            List<InvaderEntity> targets = context.InvadersNearby(includeNeighbors: true);

            foreach (InvaderEntity invader in targets)
            {
                if (Damage > 0)
                {
                    context.Services.Combat.DealDamage(invader, Damage, context.Element);
                }

                if (HealPerHit > 0)
                {
                    context.Services.Combat.Heal(context.Services.DivineCore, HealPerHit, context.Element);
                }

                context.Report($"{context.Element.Definition.DisplayName} drenou {invader.Name} ({Damage} de dano, Núcleo +{HealPerHit}).", true);
            }
        }
    }
}
