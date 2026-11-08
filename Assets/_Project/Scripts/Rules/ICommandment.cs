using System;

namespace SovereignOfAshes.Rules
{
    /// <summary>
    /// Mandamento Divino: modificador de regra global que intercepta cálculos (Strategy + Chain of Responsibility).
    /// </summary>
    public interface ICommandment
    {
        /// <summary>Identificador único (um mesmo Mandamento não pode estar ativo duas vezes).</summary>
        string Id { get; }

        /// <summary>Nome exibido ao jogador.</summary>
        string DisplayName { get; }

        /// <summary>Ordem de execução: maior prioridade executa primeiro.</summary>
        int Priority { get; }

        /// <summary>
        /// Indica se este Mandamento deve processar o contexto.
        /// </summary>
        /// <param name="context">Contexto do cálculo.</param>
        /// <returns><c>true</c> para chamar <see cref="Apply"/>.</returns>
        bool AppliesTo(RuleContext context);

        /// <summary>
        /// Modifica o contexto.
        /// </summary>
        /// <param name="context">Contexto do cálculo.</param>
        void Apply(RuleContext context);
    }

    /// <summary>
    /// Base com os metadados comuns de um Mandamento. Subclasses implementam apenas a regra.
    /// </summary>
    public abstract class CommandmentBase : ICommandment
    {
        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <summary>
        /// Inicializa os metadados.
        /// </summary>
        /// <param name="id">Identificador único (não vazio).</param>
        /// <param name="displayName">Nome exibido (usa o id se vazio).</param>
        /// <param name="priority">Prioridade de execução.</param>
        /// <exception cref="ArgumentException">Se <paramref name="id"/> for vazio.</exception>
        protected CommandmentBase(string id, string displayName, int priority)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("O id do Mandamento não pode ser vazio.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Priority = priority;
        }

        /// <inheritdoc />
        public abstract bool AppliesTo(RuleContext context);

        /// <inheritdoc />
        public abstract void Apply(RuleContext context);
    }
}
