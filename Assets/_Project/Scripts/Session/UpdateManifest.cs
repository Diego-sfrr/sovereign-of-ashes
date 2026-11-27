using System;
using System.Text.RegularExpressions;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Descrição da versão mais nova do jogo, lida de um arquivo <c>version.json</c> público:
    /// <c>{"version": "0.4.0", "url": "https://...", "notes": "O que mudou"}</c>.
    /// Só serve para avisar o jogador; nada é baixado nem instalado sozinho.
    /// </summary>
    public sealed class UpdateManifest
    {
        /// <summary>Número da versão mais nova (ex.: <c>0.4.0</c>).</summary>
        public string Version { get; private set; }

        /// <summary>Link da página de download.</summary>
        public string Url { get; private set; }

        /// <summary>Resumo curto do que mudou (pode ser vazio).</summary>
        public string Notes { get; private set; }

        /// <summary>
        /// Lê o texto do manifesto. Nunca lança exceção: texto inválido, sem versão ou com link que não seja https
        /// devolve <c>false</c>.
        /// </summary>
        /// <param name="json">Conteúdo do arquivo (pode ser nulo).</param>
        /// <param name="manifest">Manifesto lido.</param>
        /// <returns>Verdadeiro se o texto era um manifesto válido.</returns>
        public static bool TryParse(string json, out UpdateManifest manifest)
        {
            manifest = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            string version = Field(json, "version");
            string url = Field(json, "url");

            if (string.IsNullOrEmpty(version) || !GameVersion.IsValid(version) ||
                string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            manifest = new UpdateManifest { Version = version, Url = url, Notes = Field(json, "notes") ?? string.Empty };
            return true;
        }

        private static string Field(string json, string key)
        {
            Match match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return match.Success ? match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", " ").Replace("\\/", "/") : null;
        }
    }

    /// <summary>Comparação de números de versão no formato <c>1.2.3</c>.</summary>
    public static class GameVersion
    {
        /// <summary>Se o texto é uma versão com 1 a 4 números separados por ponto.</summary>
        /// <param name="version">Texto a testar.</param>
        /// <returns>Verdadeiro se for válido.</returns>
        public static bool IsValid(string version) =>
            !string.IsNullOrWhiteSpace(version) && Regex.IsMatch(version.Trim(), @"^\d{1,6}(\.\d{1,6}){0,3}$");

        /// <summary>Se <paramref name="latest"/> é estritamente mais nova que <paramref name="current"/>.</summary>
        /// <param name="current">Versão instalada.</param>
        /// <param name="latest">Versão publicada.</param>
        /// <returns>Falso se alguma das duas for inválida ou se não for mais nova.</returns>
        public static bool IsNewer(string current, string latest)
        {
            if (!IsValid(current) || !IsValid(latest))
            {
                return false;
            }

            string[] a = current.Trim().Split('.');
            string[] b = latest.Trim().Split('.');
            int length = Math.Max(a.Length, b.Length);

            for (int i = 0; i < length; i++)
            {
                int x = i < a.Length ? int.Parse(a[i]) : 0;
                int y = i < b.Length ? int.Parse(b[i]) : 0;

                if (y != x)
                {
                    return y > x;
                }
            }

            return false;
        }
    }
}
