using System.Text;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Onde o jogador pode apoiar o criador e mandar sugestões/bugs. São só links públicos: o jogo nunca guarda
    /// chaves de API nem dados pessoais, e nada é enviado sozinho (o relatório só é copiado se a pessoa clicar).
    /// </summary>
    public static class SupportInfo
    {
        /// <summary>Link público da página de apoio do criador.</summary>
        public const string DonationUrl = "https://livepix.gg/pollus999";

        /// <summary>Formulário do Google para sugestões e bugs (vazio = o botão de abrir some).</summary>
        public const string FeedbackUrl = "https://docs.google.com/forms/d/e/1FAIpQLScYO6LQOAl58tqMVKHLAGdlPFv6XfQpdI3ZjQdzVLXOYtOnfQ/viewform";

        /// <summary>
        /// Endereço público do <c>version.json</c> que diz qual é a versão mais nova (vazio = sem aviso de atualização).
        /// </summary>
        public const string UpdateManifestUrl = "";

        /// <summary>Se existe um endereço de atualização configurado.</summary>
        public static bool HasUpdateChannel => !string.IsNullOrWhiteSpace(UpdateManifestUrl);

        /// <summary>Endereço curto para mostrar na tela (sem o "https://").</summary>
        public static string DisplayUrl => DonationUrl.Replace("https://", string.Empty);

        /// <summary>Se já existe um canal configurado para receber feedback.</summary>
        public static bool HasFeedbackChannel => !string.IsNullOrWhiteSpace(FeedbackUrl);

        /// <summary>
        /// Monta um relatório curto e sem dados pessoais para o jogador colar junto do comentário: versão, modo,
        /// civilização, pacto, semente e em que ponto da partida estava.
        /// </summary>
        /// <param name="gameVersion">Versão do jogo.</param>
        /// <param name="settings">Preferências da partida (nulo = padrão).</param>
        /// <param name="turn">Turno atual (0 = fora de partida).</param>
        /// <param name="state">Estado textual (ex.: "em jogo", "derrota").</param>
        /// <returns>Texto pronto para copiar.</returns>
        public static string BuildReport(string gameVersion, GameSettings settings, int turn, string state)
        {
            settings = settings ?? new GameSettings();
            var text = new StringBuilder();
            text.AppendLine("[Sovereign of Ashes - relatório]");
            text.AppendLine("Versão: " + (string.IsNullOrEmpty(gameVersion) ? "?" : gameVersion));
            text.AppendLine("Modo: " + settings.Mode);
            text.AppendLine("Dificuldade: " + DifficultyPresets.Name(settings.Difficulty));
            text.AppendLine("Civilização: " + (settings.CivilizationId ?? "-"));
            text.AppendLine("Pacto: " + (settings.CovenantId ?? "-"));
            text.AppendLine("Semente: " + settings.Seed);
            text.AppendLine("Turno: " + turn + (string.IsNullOrEmpty(state) ? string.Empty : " (" + state + ")"));
            text.AppendLine();
            text.AppendLine("Comentário / bug (o que aconteceu e o que você esperava):");
            return text.ToString();
        }
    }
}
