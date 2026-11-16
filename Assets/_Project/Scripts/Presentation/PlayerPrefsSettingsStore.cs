using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Guarda as preferências do jogador no <see cref="PlayerPrefs"/> do Unity (registro do Windows / arquivo de preferências),
    /// numa única chave de texto.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private readonly string _key;

        /// <summary>
        /// Cria o guardador.
        /// </summary>
        /// <param name="key">Chave do PlayerPrefs (cada tipo de dado usa a sua).</param>
        public PlayerPrefsSettingsStore(string key = "sovereign_of_ashes.settings")
        {
            _key = key;
        }

        /// <inheritdoc />
        public string Load() => PlayerPrefs.GetString(_key, string.Empty);

        /// <inheritdoc />
        public void Save(string text)
        {
            PlayerPrefs.SetString(_key, text ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
