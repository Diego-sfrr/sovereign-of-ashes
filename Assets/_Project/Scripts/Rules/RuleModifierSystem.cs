using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Rules
{
    /// <summary>
    /// Pipeline de Mandamentos Divinos. Todo cálculo de dano, cura ou recurso passa por aqui
    /// antes de ser aplicado, permitindo alterar as regras do jogo em tempo de execução.
    /// <para>
    /// Segurança contra loops: cada Mandamento executa no máximo uma vez por cadeia de redirecionamento
    /// e a cadeia tem profundidade máxima (<see cref="MaxChainDepth"/>).
    /// </para>
    /// </summary>
    public sealed class RuleModifierSystem
    {
        /// <summary>Profundidade padrão de redirecionamentos encadeados.</summary>
        public const int DefaultMaxChainDepth = 4;

        private readonly List<ICommandment> _commandments = new List<ICommandment>();
        private readonly IReadOnlyList<ICommandment> _readOnlyCommandments;

        /// <summary>Disparado quando um Mandamento é ativado.</summary>
        public event Action<ICommandment> CommandmentAdded;

        /// <summary>Disparado quando um Mandamento é removido.</summary>
        public event Action<ICommandment> CommandmentRemoved;

        /// <summary>Disparado sempre que um Mandamento altera um cálculo (útil para feedback visual).</summary>
        public event Action<ICommandment, RuleContext> CommandmentTriggered;

        /// <summary>Mandamentos ativos, em ordem de execução.</summary>
        public IReadOnlyList<ICommandment> ActiveCommandments => _readOnlyCommandments;

        /// <summary>Profundidade máxima de redirecionamentos.</summary>
        public int MaxChainDepth { get; }

        /// <summary>
        /// Cria o sistema.
        /// </summary>
        /// <param name="maxChainDepth">Profundidade máxima de redirecionamentos (não negativa).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="maxChainDepth"/> for negativo.</exception>
        public RuleModifierSystem(int maxChainDepth = DefaultMaxChainDepth)
        {
            if (maxChainDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxChainDepth), maxChainDepth, "Não pode ser negativo.");
            }

            MaxChainDepth = maxChainDepth;
            _readOnlyCommandments = _commandments.AsReadOnly();
        }

        /// <summary>
        /// Ativa um Mandamento, mantendo a lista ordenada por prioridade (decrescente, estável).
        /// </summary>
        /// <param name="commandment">Mandamento a ativar.</param>
        /// <returns><c>false</c> se já existir um Mandamento com o mesmo id.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="commandment"/> for nulo.</exception>
        public bool AddCommandment(ICommandment commandment)
        {
            if (commandment == null)
            {
                throw new ArgumentNullException(nameof(commandment));
            }

            if (IndexOf(commandment.Id) >= 0)
            {
                return false;
            }

            int insertAt = _commandments.Count;

            for (int i = 0; i < _commandments.Count; i++)
            {
                if (commandment.Priority > _commandments[i].Priority)
                {
                    insertAt = i;
                    break;
                }
            }

            _commandments.Insert(insertAt, commandment);
            CommandmentAdded?.Invoke(commandment);
            return true;
        }

        /// <summary>
        /// Remove um Mandamento pelo id.
        /// </summary>
        /// <param name="commandmentId">Id do Mandamento.</param>
        /// <returns><c>true</c> se foi removido.</returns>
        public bool RemoveCommandment(string commandmentId)
        {
            int index = IndexOf(commandmentId);

            if (index < 0)
            {
                return false;
            }

            ICommandment removed = _commandments[index];
            _commandments.RemoveAt(index);
            CommandmentRemoved?.Invoke(removed);
            return true;
        }

        /// <summary>
        /// Processa um cálculo pelo pipeline e devolve os efeitos finais a aplicar.
        /// Um único pedido pode gerar vários efeitos (ex.: cura parcial + bônus de ataque).
        /// </summary>
        /// <param name="channel">Canal do cálculo.</param>
        /// <param name="amount">Quantidade original (valores negativos viram zero).</param>
        /// <param name="target">Alvo (pode ser nulo).</param>
        /// <param name="source">Origem (pode ser nula).</param>
        /// <returns>Efeitos finais, todos com quantidade positiva.</returns>
        public IReadOnlyList<RuleEffect> Resolve(RuleChannel channel, int amount, IRuleTarget target, object source = null)
        {
            var output = new List<RuleEffect>();

            if (amount <= 0)
            {
                return output;
            }

            ResolveRecursive(new RuleEffect(channel, amount, target), source, 0, new HashSet<string>(), output);
            return output;
        }

        private void ResolveRecursive(
            RuleEffect request,
            object source,
            int depth,
            HashSet<string> firedInChain,
            List<RuleEffect> output)
        {
            var context = new RuleContext(request.Channel, request.Amount, request.Target, source, depth);

            // Cópia defensiva: um Mandamento pode adicionar/remover outros durante a execução.
            ICommandment[] snapshot = _commandments.ToArray();

            foreach (ICommandment commandment in snapshot)
            {
                if (context.IsPropagationStopped)
                {
                    break;
                }

                if (firedInChain.Contains(commandment.Id) || !commandment.AppliesTo(context))
                {
                    continue;
                }

                commandment.Apply(context);
                firedInChain.Add(commandment.Id);
                CommandmentTriggered?.Invoke(commandment, context);
            }

            if (context.Amount > 0)
            {
                output.Add(new RuleEffect(context.Channel, context.Amount, context.Target));
            }

            foreach (RuleEffect redirection in context.Redirections)
            {
                if (depth + 1 > MaxChainDepth)
                {
                    // Limite atingido: o efeito é aplicado "cru", sem novos Mandamentos.
                    output.Add(redirection);
                    continue;
                }

                // Cada ramo recebe sua própria cópia, para que ramos irmãos não interfiram entre si.
                ResolveRecursive(redirection, source, depth + 1, new HashSet<string>(firedInChain), output);
            }
        }

        private int IndexOf(string commandmentId)
        {
            if (string.IsNullOrEmpty(commandmentId))
            {
                return -1;
            }

            for (int i = 0; i < _commandments.Count; i++)
            {
                if (string.Equals(_commandments[i].Id, commandmentId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
