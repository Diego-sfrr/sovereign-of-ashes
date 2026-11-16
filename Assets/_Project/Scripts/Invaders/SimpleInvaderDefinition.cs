using System;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Definição de invasor em código puro: usada como padrão quando nenhum asset é configurado e nos testes.
    /// </summary>
    public sealed class SimpleInvaderDefinition : IInvaderDefinition, IInvaderTraits
    {
        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public int MaxHealth { get; }

        /// <inheritdoc />
        public int Attack { get; }

        /// <inheritdoc />
        public int FaithBounty { get; }

        /// <inheritdoc />
        public bool CorruptsElements { get; }

        /// <inheritdoc />
        public int RationalityPerTurn { get; }

        /// <summary>
        /// Cria a definição.
        /// </summary>
        /// <param name="id">Id único (não vazio).</param>
        /// <param name="displayName">Nome exibido (usa o id se vazio).</param>
        /// <param name="maxHealth">Vida máxima (maior que zero).</param>
        /// <param name="attack">Ataque (não negativo).</param>
        /// <param name="faithBounty">Recompensa em Fé (não negativa).</param>
        /// <param name="corruptsElements">Se destrói o elemento do setor em que está.</param>
        /// <param name="rationalityPerTurn">Racionalidade somada às civilizações a cada turno (não negativa).</param>
        /// <exception cref="ArgumentException">Se <paramref name="id"/> for vazio.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor numérico for inválido.</exception>
        public SimpleInvaderDefinition(string id, string displayName, int maxHealth, int attack, int faithBounty,
            bool corruptsElements = false, int rationalityPerTurn = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("O id não pode ser vazio.", nameof(id));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Deve ser maior que zero.");
            }

            if (attack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attack), attack, "Não pode ser negativo.");
            }

            if (faithBounty < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(faithBounty), faithBounty, "Não pode ser negativo.");
            }

            if (rationalityPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rationalityPerTurn), rationalityPerTurn, "Não pode ser negativa.");
            }

            CorruptsElements = corruptsElements;
            RationalityPerTurn = rationalityPerTurn;
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            MaxHealth = maxHealth;
            Attack = attack;
            FaithBounty = faithBounty;
        }
    }
}
