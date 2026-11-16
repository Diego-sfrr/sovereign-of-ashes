using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SovereignOfAshes.Achievements
{
    /// <summary>
    /// O que o jogador já conquistou e os contadores acumulados entre partidas. Dados puros, com texto
    /// <c>u=a,b;r=x,y;c.kills=5</c> que tolera lixo (nunca lança exceção ao ler).
    /// </summary>
    public sealed class AchievementProgress
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly HashSet<string> _craftedElements = new HashSet<string>();
        private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

        /// <summary>Quantas conquistas já foram ganhas.</summary>
        public int UnlockedCount => _unlocked.Count;

        /// <summary>Quantos elementos diferentes já foram criados.</summary>
        public int CraftedElementCount => _craftedElements.Count;

        /// <summary>Se a conquista já foi ganha.</summary>
        /// <param name="id">Id.</param>
        /// <returns>Verdadeiro se ganha.</returns>
        public bool IsUnlocked(string id) => id != null && _unlocked.Contains(id);

        /// <summary>Marca como ganha.</summary>
        /// <param name="id">Id.</param>
        /// <returns><c>true</c> se era nova.</returns>
        public bool Unlock(string id) => !string.IsNullOrWhiteSpace(id) && _unlocked.Add(id);

        /// <summary>Registra um elemento criado pela alquimia.</summary>
        /// <param name="elementId">Id do elemento.</param>
        /// <returns><c>true</c> se era a primeira vez.</returns>
        public bool AddCraftedElement(string elementId) => !string.IsNullOrWhiteSpace(elementId) && _craftedElements.Add(elementId);

        /// <summary>Lê um contador.</summary>
        /// <param name="key">Nome.</param>
        /// <returns>Valor (0 se nunca usado).</returns>
        public int Get(string key) => _counters.TryGetValue(key, out int value) ? value : 0;

        /// <summary>Soma ao contador (sem estourar).</summary>
        /// <param name="key">Nome.</param>
        /// <param name="amount">Quantidade (não negativa).</param>
        /// <returns>Novo valor.</returns>
        public int Add(string key, int amount)
        {
            long next = (long)Get(key) + Math.Max(0, amount);
            int value = (int)Math.Min(1000000000, next);
            _counters[key] = value;
            return value;
        }

        /// <summary>
        /// Progresso mostrado na tela (0 até a meta) de uma conquista.
        /// </summary>
        /// <param name="definition">Conquista.</param>
        /// <returns>Valor entre 0 e <see cref="AchievementDefinition.Goal"/>.</returns>
        public int ProgressOf(AchievementDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            if (IsUnlocked(definition.Id))
            {
                return definition.Goal;
            }

            int value;

            switch (definition.Id)
            {
                case "exterminator":
                case "horde_reaper": value = Get(AchievementCatalog.CounterKills); break;
                case "devout": value = Get(AchievementCatalog.CounterFaithSpent); break;
                case "veteran": value = Get(AchievementCatalog.CounterGames); break;
                case "grand_alchemist": value = CraftedElementCount; break;
                default: value = 0; break;
            }

            return Math.Max(0, Math.Min(definition.Goal, value));
        }

        /// <summary>
        /// Converte em texto para guardar.
        /// </summary>
        /// <returns>Texto.</returns>
        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append("u=").Append(string.Join(",", _unlocked));
            text.Append(";r=").Append(string.Join(",", _craftedElements));

            foreach (KeyValuePair<string, int> counter in _counters)
            {
                text.Append(";c.").Append(counter.Key).Append('=').Append(counter.Value.ToString(CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        /// <summary>
        /// Lê do texto de <see cref="Serialize"/>; texto vazio ou corrompido dá progresso vazio.
        /// </summary>
        /// <param name="text">Texto (pode ser nulo).</param>
        /// <returns>O progresso lido.</returns>
        public static AchievementProgress Parse(string text)
        {
            var progress = new AchievementProgress();

            if (string.IsNullOrWhiteSpace(text))
            {
                return progress;
            }

            foreach (string pair in text.Split(';'))
            {
                int separator = pair.IndexOf('=');

                if (separator <= 0)
                {
                    continue;
                }

                string key = pair.Substring(0, separator).Trim();
                string value = pair.Substring(separator + 1).Trim();

                if (key == "u" || key == "r")
                {
                    foreach (string id in value.Split(','))
                    {
                        string trimmed = id.Trim();

                        if (trimmed.Length == 0)
                        {
                            continue;
                        }

                        if (key == "u")
                        {
                            // Só conquistas que ainda existem: ids de versões antigas são ignorados.
                            if (AchievementCatalog.Find(trimmed) != null)
                            {
                                progress._unlocked.Add(trimmed);
                            }
                        }
                        else
                        {
                            progress._craftedElements.Add(trimmed);
                        }
                    }
                }
                else if (key.StartsWith("c.", StringComparison.Ordinal) && key.Length > 2
                    && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                {
                    progress._counters[key.Substring(2)] = Math.Max(0, number);
                }
            }

            return progress;
        }
    }
}
