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

        /// <inheritdoc />
        public int DamageReduction { get; }

        /// <inheritdoc />
        public int HealsRing { get; }

        /// <inheritdoc />
        public bool IgnoresBarriers { get; }

        /// <inheritdoc />
        public int ExplodeDamage { get; }

        /// <inheritdoc />
        public bool RotatesMiddleRing { get; }

        /// <inheritdoc />
        public bool IsBoss { get; }

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
        /// <param name="damageReduction">Redução de cada golpe sofrido (mínimo 1 de dano).</param>
        /// <param name="healsRing">Vida devolvida por turno aos outros invasores do anel.</param>
        /// <param name="ignoresBarriers">Se ignora barreiras (Golem de Ferro).</param>
        /// <param name="explodeDamage">Dano aos vizinhos ao morrer (também destrói o elemento do setor).</param>
        /// <param name="rotatesMiddleRing">Se gira o anel do meio 1 casa por turno.</param>
        /// <param name="isBoss">Se é um chefe.</param>
        /// <exception cref="ArgumentException">Se <paramref name="id"/> for vazio.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor numérico for inválido.</exception>
        public SimpleInvaderDefinition(string id, string displayName, int maxHealth, int attack, int faithBounty,
            bool corruptsElements = false, int rationalityPerTurn = 0, int damageReduction = 0, int healsRing = 0,
            bool ignoresBarriers = false, int explodeDamage = 0, bool rotatesMiddleRing = false, bool isBoss = false)
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
            DamageReduction = Math.Max(0, damageReduction);
            HealsRing = Math.Max(0, healsRing);
            IgnoresBarriers = ignoresBarriers;
            ExplodeDamage = Math.Max(0, explodeDamage);
            RotatesMiddleRing = rotatesMiddleRing;
            IsBoss = isBoss;
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            MaxHealth = maxHealth;
            Attack = attack;
            FaithBounty = faithBounty;
        }
    }
}
