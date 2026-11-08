using System;

namespace SovereignOfAshes.Rules.Combat
{
    /// <summary>
    /// Qualquer coisa que recebe dano, cura e bônus de ataque (Núcleo Divino, entidades, invasores).
    /// </summary>
    public interface ICombatant : IRuleTarget
    {
        /// <summary>Ataque atual.</summary>
        int Attack { get; }

        /// <summary>Indica se a vida chegou a zero.</summary>
        bool IsDead { get; }

        /// <summary>Aplica dano já calculado pelos Mandamentos.</summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        void TakeDamage(int amount);

        /// <summary>Aplica cura já calculada pelos Mandamentos.</summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        void Heal(int amount);

        /// <summary>Aumenta o ataque.</summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        void AddAttack(int amount);
    }

    /// <summary>
    /// Implementação simples de <see cref="ICombatant"/> com vida limitada a [0, MaxHealth].
    /// </summary>
    public sealed class Combatant : ICombatant
    {
        /// <summary>Disparado após qualquer mudança de status.</summary>
        public event Action<Combatant> Changed;

        /// <summary>Nome para logs e UI.</summary>
        public string Name { get; }

        /// <inheritdoc />
        public int CurrentHealth { get; private set; }

        /// <inheritdoc />
        public int MaxHealth { get; }

        /// <inheritdoc />
        public int Attack { get; private set; }

        /// <inheritdoc />
        public bool IsDead => CurrentHealth <= 0;

        /// <summary>
        /// Cria o combatente com vida cheia.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="maxHealth">Vida máxima (maior que zero).</param>
        /// <param name="attack">Ataque inicial (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se os valores forem inválidos.</exception>
        public Combatant(string name, int maxHealth, int attack)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Deve ser maior que zero.");
            }

            if (attack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attack), attack, "Não pode ser negativo.");
            }

            Name = string.IsNullOrWhiteSpace(name) ? "Combatente" : name;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Attack = attack;
        }

        /// <inheritdoc />
        public void TakeDamage(int amount)
        {
            GuardNonNegative(amount);
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            Changed?.Invoke(this);
        }

        /// <inheritdoc />
        public void Heal(int amount)
        {
            GuardNonNegative(amount);
            CurrentHealth = (int)Math.Min(MaxHealth, (long)CurrentHealth + amount);
            Changed?.Invoke(this);
        }

        /// <inheritdoc />
        public void AddAttack(int amount)
        {
            GuardNonNegative(amount);
            Attack = (int)Math.Min(int.MaxValue, (long)Attack + amount);
            Changed?.Invoke(this);
        }

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Nome, vida e ataque.</returns>
        public override string ToString() => $"{Name} HP {CurrentHealth}/{MaxHealth} ATK {Attack}";

        private static void GuardNonNegative(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Não pode ser negativo.");
            }
        }
    }
}
