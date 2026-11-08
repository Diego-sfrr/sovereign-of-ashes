using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Um invasor vivo no tabuleiro. Ocupa uma placa tectônica (e gira junto com ela).
    /// Implementa <see cref="ICombatant"/>, então todo dano recebido passa pelos Mandamentos via <see cref="CombatResolver"/>.
    /// Criado somente pelo <see cref="InvaderRoster"/>.
    /// </summary>
    public sealed class InvaderEntity : ICombatant
    {
        private readonly Combatant _stats;

        /// <summary>Identificador único nesta partida.</summary>
        public int Id { get; }

        /// <summary>Tipo do invasor.</summary>
        public IInvaderDefinition Definition { get; }

        /// <summary>Placa ocupada, ou <c>null</c> se o invasor foi removido.</summary>
        public TectonicSector Sector { get; internal set; }

        /// <summary>Nome para logs e UI.</summary>
        public string Name => Definition.DisplayName;

        /// <inheritdoc />
        public int CurrentHealth => _stats.CurrentHealth;

        /// <inheritdoc />
        public int MaxHealth => _stats.MaxHealth;

        /// <inheritdoc />
        public int Attack => _stats.Attack;

        /// <inheritdoc />
        public bool IsDead => _stats.IsDead;

        /// <summary>
        /// Cria o invasor com vida cheia.
        /// </summary>
        /// <param name="id">Id único.</param>
        /// <param name="definition">Tipo do invasor.</param>
        /// <param name="sector">Placa inicial.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        internal InvaderEntity(int id, IInvaderDefinition definition, TectonicSector sector)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Sector = sector ?? throw new ArgumentNullException(nameof(sector));
            Id = id;
            _stats = new Combatant(definition.DisplayName, Math.Max(1, definition.MaxHealth), Math.Max(0, definition.Attack));
        }

        /// <inheritdoc />
        public void TakeDamage(int amount) => _stats.TakeDamage(amount);

        /// <inheritdoc />
        public void Heal(int amount) => _stats.Heal(amount);

        /// <inheritdoc />
        public void AddAttack(int amount) => _stats.AddAttack(amount);

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Nome, id, vida e posição.</returns>
        public override string ToString() => $"{Name}#{Id} HP {CurrentHealth}/{MaxHealth} em {Sector?.Ring.ToString() ?? "-"}";
    }
}
