namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Onde o jogador pode apoiar o criador. É só um link público de doação (LivePix): o jogo nunca guarda
    /// chaves de API nem dados de pagamento, e quem doa paga direto na página do LivePix, no navegador.
    /// </summary>
    public static class SupportInfo
    {
        /// <summary>Link público da página de apoio do criador.</summary>
        public const string DonationUrl = "https://livepix.gg/pollus999";

        /// <summary>Endereço curto para mostrar na tela (sem o "https://").</summary>
        public static string DisplayUrl => DonationUrl.Replace("https://", string.Empty);
    }
}
