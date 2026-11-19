using System.Collections.Generic;

namespace SovereignOfAshes.Achievements
{
    /// <summary>
    /// Uma conquista: id estável (é o que fica salvo), nome, descrição e meta.
    /// </summary>
    public sealed class AchievementDefinition
    {
        /// <summary>Id estável, sem ponto e vírgula, igual ou vírgula.</summary>
        public string Id { get; }

        /// <summary>Nome exibido.</summary>
        public string Name { get; }

        /// <summary>O que fazer para ganhar.</summary>
        public string Description { get; }

        /// <summary>Meta numérica (1 = conquista simples; maior que 1 mostra barra de progresso).</summary>
        public int Goal { get; }

        /// <summary>
        /// Cria a definição.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="name">Nome.</param>
        /// <param name="description">Descrição.</param>
        /// <param name="goal">Meta (pelo menos 1).</param>
        public AchievementDefinition(string id, string name, string description, int goal = 1)
        {
            Id = id;
            Name = name;
            Description = description;
            Goal = goal < 1 ? 1 : goal;
        }
    }

    /// <summary>
    /// A lista de todas as conquistas do jogo.
    /// </summary>
    public static class AchievementCatalog
    {
        // Nomes dos contadores acumulados entre partidas.
        public const string CounterKills = "kills";
        public const string CounterFaithSpent = "faith";
        public const string CounterGames = "games";
        public const string CounterInterventions = "interventions";
        public const string CounterCivWinPrefix = "winciv_";

        /// <summary>Todas as conquistas, na ordem de exibição.</summary>
        public static IReadOnlyList<AchievementDefinition> All { get; } = new List<AchievementDefinition>
        {
            new AchievementDefinition("first_blood", "Primeiro Sangue", "Destrua o seu primeiro invasor."),
            new AchievementDefinition("exterminator", "Exterminador", "Destrua 100 invasores (somando todas as partidas).", 100),
            new AchievementDefinition("horde_reaper", "Ceifador de Hordas", "Destrua 500 invasores (somando todas as partidas).", 500),
            new AchievementDefinition("alchemist", "Primeira Transmutação", "Faça a sua primeira fusão alquímica."),
            new AchievementDefinition("grand_alchemist", "Mestre Alquimista", "Crie todos os 19 elementos das receitas.", 19),
            new AchievementDefinition("devout", "Devoto", "Gaste 100 de Fé (somando todas as partidas).", 100),
            new AchievementDefinition("offering", "Oferenda Aceita", "Leve uma carta nova para o baralho numa Oferenda Divina."),
            new AchievementDefinition("grand_deck", "Baralho Consagrado", "Chegue a 21 cartas no baralho numa mesma partida."),
            new AchievementDefinition("heresy", "Fé Corrompida", "Deixe uma civilização virar herege."),
            new AchievementDefinition("fallen_throne", "O Trono Caiu", "Perca uma partida. Toda lenda começa assim."),
            new AchievementDefinition("veteran", "Veterano", "Termine 10 partidas.", 10),
            new AchievementDefinition("victory", "O Trono Resiste", "Vença uma partida."),
            new AchievementDefinition("covenant_win", "Pacto Cumprido", "Vença uma partida usando um Pacto."),
            new AchievementDefinition("divine_hand", "Mão Divina", "Use a Intervenção Divina 10 vezes (em todas as partidas).", 10),
            new AchievementDefinition("civ_master", "Seitas Unidas", "Vença com cada uma das 4 civilizações.", 4),
            new AchievementDefinition("by_a_thread", "Por um Fio", "Vença com o Núcleo com 3 ou menos de vida."),
            new AchievementDefinition("untouched", "Intocável", "Vença sem o Núcleo sofrer dano algum."),
            new AchievementDefinition("ashes_glory", "Cinzas e Glória", "Vença na dificuldade Difícil."),
            new AchievementDefinition("endless_10", "Resistência", "Chegue ao turno 10 no Modo Infinito."),
            new AchievementDefinition("endless_25", "Inabalável", "Chegue ao turno 25 no Modo Infinito."),
            new AchievementDefinition("endless_50", "Lenda das Cinzas", "Chegue ao turno 50 no Modo Infinito.")
        };

        /// <summary>
        /// Procura uma conquista pelo id.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <returns>A definição, ou <c>null</c>.</returns>
        public static AchievementDefinition Find(string id)
        {
            foreach (AchievementDefinition definition in All)
            {
                if (definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
