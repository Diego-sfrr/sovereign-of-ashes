using System;
using System.Collections.Generic;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Por que a partida terminou em derrota (escolhe o epílogo).
    /// </summary>
    public enum DefeatCause
    {
        /// <summary>Os invasores destruíram o Núcleo.</summary>
        Invaders = 0,

        /// <summary>Uma civilização herege destruiu o Núcleo.</summary>
        Heresy = 1,

        /// <summary>Queda no Modo Infinito.</summary>
        Endless = 2
    }

    /// <summary>
    /// Os textos da história (ver HISTORIA.md): abertura, falas, epílogos e crônicas. Tudo curto, para caber na interface.
    /// Conteúdo puro (sem Unity), então os testes garantem que nada fica sem texto.
    /// </summary>
    public static class Story
    {
        /// <summary>Linhas da abertura, mostradas na Crônica quando a partida começa.</summary>
        public static IReadOnlyList<string> Opening { get; } = new[]
        {
            "Antes do primeiro nome, houve o silêncio.",
            "O Primeiro Soberano partiu quando os mortais aprenderam a duvidar, e deixou um coração em brasa onde devia haver um deus.",
            "Você é o eco que sobrou nos degraus do Trono Vazio. Mantenha o Núcleo aceso."
        };

        /// <summary>Entrada do Modo Infinito.</summary>
        public static IReadOnlyList<string> EndlessEntry { get; } = new[]
        {
            "O fim foi alcançado. Mas você permaneceu.",
            "Enquanto houver um deus diante do Núcleo, não haverá último dia."
        };

        /// <summary>Texto das ondas avançadas do Modo Infinito.</summary>
        public const string EndlessDeep = "Você já não protege o mundo. Protege a repetição.";

        /// <summary>Doze crônicas de uma linha, que aparecem de tempos em tempos.</summary>
        public static IReadOnlyList<string> Chronicles { get; } = new[]
        {
            "O Primeiro não criou o mundo para ser adorado. Criou-o para poder continuar sem ele.",
            "Toda oração deixa marca. Algumas sobrevivem a quem rezou.",
            "As cinzas não esquecem. Só aprenderam a não responder.",
            "Nos Ermos não há demônios. Há devotos que sobreviveram às próprias preces.",
            "O Golem é o fiel perfeito: tem coração para bater e nenhum cérebro para duvidar.",
            "Um Sermão bem dito vale dez muralhas: cega o olho antes que a mão procure a espada.",
            "A alquimia nada cria. Troca o nome da dor para que pareça milagre.",
            "Um altar vazio ainda pode exigir sacrifícios.",
            "Cuidado com quem conta as batidas do próprio sangue: logo contará as falhas do seu deus.",
            "O Inquisidor nunca mentiu sobre a missão. Só esqueceu por que ela existia.",
            "A Fé mantém os deuses vivos. Ninguém perguntou o que mantém vivos os mortais.",
            "Cinza é o único elemento honesto da criação: é o que sobra quando a mentira acaba de queimar."
        };

        /// <summary>Falas do Inquisidor Cinzento (chefe do turno 10).</summary>
        public const string InquisitorArrives = "Tu me fizeste santo com fogo e mentira, pequeno Eco. Ardi até virar cinza. E na cinza, enxerguei.";

        /// <summary>Fala do Inquisidor ao morrer.</summary>
        public const string InquisitorDies = "Então a ordem terminou. Diga-me, pequeno deus: o que acontece quando ninguém mais obedece?";

        /// <summary>A grande frase do turno 10 (aparece depois da chegada do Inquisidor).</summary>
        public const string InquisitorVerdict = "Você não foi escolhido para salvar o mundo. Foi feito para que ele nunca terminasse de morrer.";

        private static readonly Dictionary<string, string> Quotes = new Dictionary<string, string>
        {
            { "ash_children", "A neve morna cai do Trono. De joelhos, faremos do pó o nosso pão." },
            { "abyss_smiths", "A carne duvida e apodrece. Só o ferro, batido na fé, aguenta o peso do mundo." },
            { "eclipse_flagellants", "Sangrem no escuro. Um deus faminto só escuta o que dói." },
            { "time_hermits", "A pedra gira, o pêndulo range. Contamos os segundos antes que o centro se apague." }
        };

        private static readonly Dictionary<string, string> Victories = new Dictionary<string, string>
        {
            { "ash_children", "As últimas orações ainda ecoam. Os Filhos da Cinza ajoelham diante do Núcleo. O mundo vive. E, entre as cinzas, uma pergunta começa a crescer." },
            { "abyss_smiths", "O Ferro resistiu onde os deuses falharam. O Golem fica imóvel diante do coração. Pela primeira vez, os Forjadores se perguntam se ainda precisam de um criador." },
            { "eclipse_flagellants", "O sangue secou. O sacrifício acabou. Ninguém sabe se a dor foi necessária. O Eclipse continua sobre quem ainda quer sofrer." },
            { "time_hermits", "Vinte ciclos passaram. Os Eremitas reconheceram cada morte, cada retorno. Desta vez, o futuro não lhes mostrou o que vem depois." }
        };

        private static readonly Dictionary<string, string> Barks = new Dictionary<string, string>
        {
            { "ash_cultist", "\"Nossos mortos chamam. Entregue-nos o coração.\"" },
            { "void_colossus", "\"Reconheço o chamado. Reconheço a ruína.\"" },
            { "profaner", "\"Carne é só carne. Fogo é só combustão. Vejam o milagre evaporar no sal.\"" },
            { "heretic_herald", "\"Olhem para cima. O Trono está vazio, e o sol que adoram é uma boca.\"" },
            { "inquisitor", "\"" + InquisitorArrives + "\"" }
        };

        private static readonly string[] Defeats =
        {
            "O último cristal se partiu. Nenhuma oração respondeu. No Trono Vazio, sua consciência se esgarça como fumaça. Pela primeira vez em milênios, o mundo fica no escuro, e os homens respiram sem senhor.",
            "Não foram os monstros que cravaram a lança no Núcleo, e sim as mãos calejadas dos seus próprios fiéis. Sem ódio, com a calma de quem acorda de um pesadelo, seus filhos desligam Deus.",
            "Nenhum império arde para sempre. Após incontáveis voltas da Crosta, até o ferro cansou de obedecer e o último cristal virou pó. O Trono enfim faz jus ao nome: vazio, frio, esquecido."
        };

        /// <summary>Epílogo raro de derrota (qualquer causa).</summary>
        public const string DefeatRare = "A Crosta desabou. O Trono permaneceu vazio. Mas algo, nas profundezas, começou a procurar um novo coração.";

        /// <summary>
        /// Frase de início da civilização (tela de escolha e turno 1).
        /// </summary>
        /// <param name="civilizationId">Id da civilização.</param>
        /// <returns>A frase, ou vazio se não houver.</returns>
        public static string CivilizationQuote(string civilizationId) =>
            civilizationId != null && Quotes.TryGetValue(civilizationId, out string text) ? text : string.Empty;

        /// <summary>
        /// Epílogo de vitória da civilização.
        /// </summary>
        /// <param name="civilizationId">Id da civilização (nulo = Filhos da Cinza).</param>
        /// <returns>O epílogo.</returns>
        public static string VictoryEpilogue(string civilizationId) =>
            civilizationId != null && Victories.TryGetValue(civilizationId, out string text) ? text : Victories["ash_children"];

        /// <summary>
        /// Epílogo de derrota. Uma partida em cada cinco (pela semente) mostra o epílogo raro.
        /// </summary>
        /// <param name="cause">Causa.</param>
        /// <param name="seed">Semente da partida (decide o epílogo raro).</param>
        /// <returns>O epílogo.</returns>
        public static string DefeatEpilogue(DefeatCause cause, int seed)
        {
            if (seed % 5 == 0)
            {
                return DefeatRare;
            }

            return Defeats[(int)cause];
        }

        /// <summary>
        /// Fala de um invasor ao surgir pela primeira vez na partida.
        /// </summary>
        /// <param name="invaderId">Id do invasor.</param>
        /// <returns>A fala, ou vazio se o tipo não tem fala.</returns>
        public static string InvaderBark(string invaderId) =>
            invaderId != null && Barks.TryGetValue(invaderId, out string text) ? text : string.Empty;

        /// <summary>
        /// Fecho de ato mostrado ao fim de certos turnos (7 e 14); vazio nos outros.
        /// </summary>
        /// <param name="turn">Turno que acabou de terminar.</param>
        /// <returns>O texto, ou vazio.</returns>
        public static string ActEnding(int turn)
        {
            switch (turn)
            {
                case 7: return "As primeiras orações foram ouvidas. Mas, entre elas, havia vozes que não pediam salvação.";
                case 14: return "O guardião caiu. E, pela primeira vez em eras, ninguém sabia quem daria a próxima ordem.";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Crônica a mostrar ao começar o turno, ou vazio. Aparecem nos turnos 3, 6, 9, ... na ordem; no turno 3 a primeira.
        /// </summary>
        /// <param name="turn">Turno que começa.</param>
        /// <returns>A crônica, ou vazio.</returns>
        public static string ChronicleForTurn(int turn)
        {
            if (turn < 3 || turn % 3 != 0)
            {
                return string.Empty;
            }

            return Chronicles[(turn / 3 - 1) % Chronicles.Count];
        }

        /// <summary>Todas as civilizações com texto (para testes).</summary>
        public static IEnumerable<string> CivilizationIds => Quotes.Keys;

        /// <summary>Todos os invasores com fala (para testes).</summary>
        public static IEnumerable<string> BarkIds => Barks.Keys;

        /// <summary>Quantos epílogos de derrota existem por causa (para testes).</summary>
        public static int DefeatCauseCount => Defeats.Length;

        /// <summary>
        /// Descobre a causa de uma derrota.
        /// </summary>
        /// <param name="endless">Se era o Modo Infinito.</param>
        /// <param name="anyHeretic">Se alguma civilização estava herege.</param>
        /// <returns>A causa.</returns>
        public static DefeatCause CauseOf(bool endless, bool anyHeretic) =>
            endless ? DefeatCause.Endless : anyHeretic ? DefeatCause.Heresy : DefeatCause.Invaders;

        /// <summary>
        /// Id da civilização pelo nome (a sessão só conhece o nome).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <returns>O id, ou o da civilização padrão se não houver.</returns>
        public static string CivilizationIdOf(string name)
        {
            foreach (CivilizationDefinition civilization in CivilizationCatalog.All)
            {
                if (civilization.Name == name)
                {
                    return civilization.Id;
                }
            }

            return CivilizationCatalog.DefaultId;
        }
    }
}
