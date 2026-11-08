using System;

namespace SovereignOfAshes.Economy
{
    /// <summary>
    /// Parâmetros imutáveis da economia de Fé.
    /// Construída a partir de <see cref="FaithEconomyProfileSO"/> ou diretamente nos testes.
    /// </summary>
    public sealed class FaithEconomyConfig
    {
        /// <summary>Fé no início da partida.</summary>
        public int StartingFaith { get; }

        /// <summary>Capacidade máxima da reserva.</summary>
        public int Capacity { get; }

        /// <summary>Fé gerada todo turno pelo próprio Núcleo, independente das civilizações.</summary>
        public int BaseIncomePerTurn { get; }

        /// <summary>
        /// Percentual do status de Fé de cada civilização devota convertido em renda por turno
        /// (ex.: 50 → uma civilização com Fé 10 gera 5). Hereges não geram nada.
        /// </summary>
        public int CivilizationYieldPercent { get; }

        /// <summary>
        /// Cria a configuração.
        /// </summary>
        /// <param name="startingFaith">Fé inicial, em [0, <paramref name="capacity"/>].</param>
        /// <param name="capacity">Capacidade (maior que zero).</param>
        /// <param name="baseIncomePerTurn">Renda base (não negativa).</param>
        /// <param name="civilizationYieldPercent">Rendimento das civilizações em % (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for inválido.</exception>
        public FaithEconomyConfig(int startingFaith, int capacity, int baseIncomePerTurn, int civilizationYieldPercent)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Deve ser maior que zero.");
            }

            if (startingFaith < 0 || startingFaith > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(startingFaith), startingFaith, $"Deve estar entre 0 e {capacity}.");
            }

            if (baseIncomePerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseIncomePerTurn), baseIncomePerTurn, "Não pode ser negativo.");
            }

            if (civilizationYieldPercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(civilizationYieldPercent), civilizationYieldPercent, "Não pode ser negativo.");
            }

            StartingFaith = startingFaith;
            Capacity = capacity;
            BaseIncomePerTurn = baseIncomePerTurn;
            CivilizationYieldPercent = civilizationYieldPercent;
        }

        /// <summary>
        /// Cria uma reserva de Fé no estado inicial desta configuração.
        /// </summary>
        /// <returns>Nova reserva.</returns>
        public FaithPool CreatePool() => new FaithPool(StartingFaith, Capacity);
    }
}
