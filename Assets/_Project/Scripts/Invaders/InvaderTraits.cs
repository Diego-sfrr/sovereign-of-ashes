namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Habilidades opcionais de um invasor, além de vida e ataque. Definições que não implementam esta interface
    /// simplesmente não têm habilidades (comportamento antigo).
    /// </summary>
    public interface IInvaderTraits
    {
        /// <summary>Se destrói o elemento da alquimia do setor em que está (Profanador).</summary>
        bool CorruptsElements { get; }

        /// <summary>Racionalidade que soma a todas as civilizações a cada turno em que está vivo (Arauto, Inquisidor).</summary>
        int RationalityPerTurn { get; }
    }

    /// <summary>
    /// Leitura segura das habilidades de uma definição qualquer.
    /// </summary>
    public static class InvaderTraitsExtensions
    {
        /// <summary>
        /// Se o invasor destrói elementos.
        /// </summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Verdadeiro se tem a habilidade.</returns>
        public static bool CorruptsElements(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits && traits.CorruptsElements;

        /// <summary>
        /// Racionalidade por turno que o invasor soma às civilizações.
        /// </summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Valor (0 se não tem a habilidade).</returns>
        public static int RationalityPerTurn(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits ? traits.RationalityPerTurn : 0;

        /// <summary>
        /// Descrição curta das habilidades, para a interface (vazia se não houver).
        /// </summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Texto como "destrói elementos" ou "+1 Razão por turno".</returns>
        public static string DescribeTraits(this IInvaderDefinition definition)
        {
            string text = string.Empty;

            if (definition.CorruptsElements())
            {
                text = "destrói elementos";
            }

            int rationality = definition.RationalityPerTurn();

            if (rationality > 0)
            {
                text += (text.Length > 0 ? ", " : string.Empty) + $"+{rationality} Razão por turno";
            }

            return text;
        }
    }
}
