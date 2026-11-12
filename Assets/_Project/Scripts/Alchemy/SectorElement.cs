using System;
using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Alchemy
{
    /// <summary>
    /// Instância em tempo de execução de um elemento colocado em um setor.
    /// Separa o dado estático (<see cref="IElementDefinition"/>) do estado vivo da partida.
    /// </summary>
    public sealed class SectorElement : ISectorContent
    {
        /// <inheritdoc />
        public IElementDefinition Definition { get; }

        /// <summary>
        /// Quantas fusões geraram este elemento (0 = conceito puro).
        /// Útil para escalonar poder ou raridade de cadeias alquímicas longas.
        /// </summary>
        public int FusionDepth { get; }

        /// <summary>
        /// Cria um elemento. Use <see cref="IElementFactory"/> em vez de chamar diretamente.
        /// </summary>
        /// <param name="definition">Definição estática (não nula).</param>
        /// <param name="fusionDepth">Profundidade de fusão (não negativa).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="definition"/> for nula.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="fusionDepth"/> for negativo.</exception>
        public SectorElement(IElementDefinition definition, int fusionDepth)
        {
            if (fusionDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fusionDepth), fusionDepth, "A profundidade de fusão não pode ser negativa.");
            }

            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            FusionDepth = fusionDepth;
        }

        private Dictionary<string, int> _counters;

        /// <summary>
        /// Lê um contador de estado desta instância (ex.: durabilidade restante de um Golem).
        /// Efeitos são estratégias sem estado e compartilhadas; o estado vivo fica aqui, por instância.
        /// </summary>
        /// <param name="key">Nome do contador.</param>
        /// <param name="defaultValue">Valor devolvido se o contador ainda não existir.</param>
        /// <returns>O valor atual ou <paramref name="defaultValue"/>.</returns>
        public int GetCounter(string key, int defaultValue)
        {
            return _counters != null && key != null && _counters.TryGetValue(key, out int value) ? value : defaultValue;
        }

        /// <summary>
        /// Define um contador de estado desta instância.
        /// </summary>
        /// <param name="key">Nome do contador (não vazio).</param>
        /// <param name="value">Novo valor.</param>
        /// <exception cref="ArgumentException">Se <paramref name="key"/> for vazio.</exception>
        public void SetCounter(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("O nome do contador não pode ser vazio.", nameof(key));
            }

            if (_counters == null)
            {
                _counters = new Dictionary<string, int>();
            }

            _counters[key] = value;
        }

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Nome do elemento e profundidade de fusão.</returns>
        public override string ToString()
        {
            return $"{Definition.DisplayName} (fusão {FusionDepth})";
        }
    }
}
