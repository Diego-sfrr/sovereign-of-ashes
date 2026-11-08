using System;

namespace SovereignOfAshes.Civilization
{
    /// <summary>
    /// Lealdade de uma facção ao jogador.
    /// </summary>
    public enum FactionAllegiance
    {
        /// <summary>Devota: luta pelo deus contra os invasores.</summary>
        Devout = 0,

        /// <summary>Herege: a razão rompeu a fé; ataca o Núcleo Divino.</summary>
        Heretic = 1
    }

    /// <summary>
    /// Alvo de ataque de uma civilização no turno.
    /// </summary>
    public enum AttackTarget
    {
        /// <summary>Invasores do anel externo.</summary>
        ExternalInvaders = 0,

        /// <summary>O Núcleo Divino do jogador.</summary>
        DivineCore = 1
    }

    /// <summary>
    /// Configuração imutável dos status de uma civilização (valores iniciais e limites).
    /// Construída a partir de <see cref="CivilizationProfileSO"/> ou diretamente nos testes.
    /// </summary>
    public sealed class CivilizationStatsConfig
    {
        /// <summary>Poder militar inicial.</summary>
        public int InitialMilitaryPower { get; }

        /// <summary>Fé inicial.</summary>
        public int InitialFaith { get; }

        /// <summary>Racionalidade inicial.</summary>
        public int InitialRationality { get; }

        /// <summary>Teto de Fé.</summary>
        public int MaxFaith { get; }

        /// <summary>Teto de Racionalidade. Atingir este valor dispara a Heresia.</summary>
        public int MaxRationality { get; }

        /// <summary>
        /// Uma facção herege volta a ser devota quando a Racionalidade cai até este valor (histerese:
        /// evita que a facção alterne de estado a cada pequeno ganho ou perda).
        /// </summary>
        public int RedemptionThreshold { get; }

        /// <summary>
        /// Cria a configuração.
        /// </summary>
        /// <param name="initialMilitaryPower">Poder militar inicial (não negativo).</param>
        /// <param name="initialFaith">Fé inicial, em [0, <paramref name="maxFaith"/>].</param>
        /// <param name="initialRationality">Racionalidade inicial, em [0, <paramref name="maxRationality"/>].</param>
        /// <param name="maxFaith">Teto de Fé (maior que zero).</param>
        /// <param name="maxRationality">Teto de Racionalidade (maior que zero).</param>
        /// <param name="redemptionThreshold">Limiar de redenção, em [0, <paramref name="maxRationality"/>).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor estiver fora do intervalo válido.</exception>
        public CivilizationStatsConfig(
            int initialMilitaryPower,
            int initialFaith,
            int initialRationality,
            int maxFaith,
            int maxRationality,
            int redemptionThreshold)
        {
            if (maxFaith <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFaith), maxFaith, "Deve ser maior que zero.");
            }

            if (maxRationality <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRationality), maxRationality, "Deve ser maior que zero.");
            }

            if (initialMilitaryPower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialMilitaryPower), initialMilitaryPower, "Não pode ser negativo.");
            }

            if (initialFaith < 0 || initialFaith > maxFaith)
            {
                throw new ArgumentOutOfRangeException(nameof(initialFaith), initialFaith, $"Deve estar entre 0 e {maxFaith}.");
            }

            if (initialRationality < 0 || initialRationality > maxRationality)
            {
                throw new ArgumentOutOfRangeException(nameof(initialRationality), initialRationality, $"Deve estar entre 0 e {maxRationality}.");
            }

            if (redemptionThreshold < 0 || redemptionThreshold >= maxRationality)
            {
                throw new ArgumentOutOfRangeException(nameof(redemptionThreshold), redemptionThreshold, $"Deve estar entre 0 e {maxRationality - 1}.");
            }

            InitialMilitaryPower = initialMilitaryPower;
            InitialFaith = initialFaith;
            InitialRationality = initialRationality;
            MaxFaith = maxFaith;
            MaxRationality = maxRationality;
            RedemptionThreshold = redemptionThreshold;
        }
    }
}
