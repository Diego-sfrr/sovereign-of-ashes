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

        /// <summary>Quanto cada golpe sofrido é reduzido (Guardião Profano). Todo golpe causa pelo menos 1.</summary>
        int DamageReduction { get; }

        /// <summary>Vida que devolve, todo turno, aos outros invasores do mesmo anel (Necroapóstolo).</summary>
        int HealsRing { get; }

        /// <summary>Se ignora barreiras como o Golem de Ferro (Corredor Herege).</summary>
        bool IgnoresBarriers { get; }

        /// <summary>Dano que causa aos invasores vizinhos ao morrer, destruindo também o elemento do próprio setor (Flagelo Explosivo).</summary>
        int ExplodeDamage { get; }

        /// <summary>Se gira o anel do meio 1 casa por turno, embaralhando o tabuleiro (Profeta do Tempo).</summary>
        bool RotatesMiddleRing { get; }

        /// <summary>Se é um chefe (aparece anunciado e dá mais história).</summary>
        bool IsBoss { get; }
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

        /// <summary>Redução de dano por golpe do invasor.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Valor (0 se não tem a habilidade).</returns>
        public static int DamageReduction(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits ? traits.DamageReduction : 0;

        /// <summary>Vida devolvida por turno aos outros invasores do anel.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Valor (0 se não tem a habilidade).</returns>
        public static int HealsRing(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits ? traits.HealsRing : 0;

        /// <summary>Se o invasor ignora barreiras.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Verdadeiro se tem a habilidade.</returns>
        public static bool IgnoresBarriers(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits && traits.IgnoresBarriers;

        /// <summary>Dano da explosão ao morrer.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Valor (0 se não explode).</returns>
        public static int ExplodeDamage(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits ? traits.ExplodeDamage : 0;

        /// <summary>Se o invasor gira o anel do meio.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Verdadeiro se tem a habilidade.</returns>
        public static bool RotatesMiddleRing(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits && traits.RotatesMiddleRing;

        /// <summary>Se o invasor é um chefe.</summary>
        /// <param name="definition">Definição (pode ser nula).</param>
        /// <returns>Verdadeiro para chefes.</returns>
        public static bool IsBoss(this IInvaderDefinition definition) =>
            definition is IInvaderTraits traits && traits.IsBoss;

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

            void Add(string part) => text += (text.Length > 0 ? ", " : string.Empty) + part;

            if (definition.DamageReduction() > 0)
            {
                Add($"golpes -{definition.DamageReduction()}");
            }

            if (definition.HealsRing() > 0)
            {
                Add($"cura o anel +{definition.HealsRing()}");
            }

            if (definition.IgnoresBarriers())
            {
                Add("ignora barreiras");
            }

            if (definition.ExplodeDamage() > 0)
            {
                Add($"explode ({definition.ExplodeDamage()})");
            }

            if (definition.RotatesMiddleRing())
            {
                Add("gira o anel do meio");
            }

            return text;
        }
    }
}
