using System;
using System.Collections.Generic;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Rules;

namespace SovereignOfAshes.Economy
{
    /// <summary>
    /// Relatório imutável da coleta de Fé de um turno.
    /// </summary>
    public sealed class FaithIncomeReport
    {
        /// <summary>Renda bruta, antes dos Mandamentos.</summary>
        public int GrossIncome { get; }

        /// <summary>Renda após os Mandamentos (o que tentou entrar na reserva).</summary>
        public int ModifiedIncome { get; }

        /// <summary>Quanto efetivamente entrou na reserva.</summary>
        public int Deposited { get; }

        /// <summary>Renda perdida por exceder a capacidade.</summary>
        public int Overflow => ModifiedIncome - Deposited;

        /// <summary>
        /// Efeitos de outros canais gerados por Mandamentos durante a coleta
        /// (ex.: "toda Fé recebida também cura o Núcleo"). Quem chamou deve aplicá-los.
        /// </summary>
        public IReadOnlyList<RuleEffect> SideEffects { get; }

        /// <summary>
        /// Cria o relatório.
        /// </summary>
        /// <param name="grossIncome">Renda bruta.</param>
        /// <param name="modifiedIncome">Renda modificada.</param>
        /// <param name="deposited">Depositado.</param>
        /// <param name="sideEffects">Efeitos secundários.</param>
        internal FaithIncomeReport(int grossIncome, int modifiedIncome, int deposited, IReadOnlyList<RuleEffect> sideEffects)
        {
            GrossIncome = grossIncome;
            ModifiedIncome = modifiedIncome;
            Deposited = deposited;
            SideEffects = sideEffects;
        }
    }

    /// <summary>
    /// Calcula e deposita a renda de Fé do turno:
    /// renda base do Núcleo + rendimento de cada civilização devota.
    /// Toda renda passa pelo canal <see cref="RuleChannel.ResourceGain"/> do <see cref="RuleModifierSystem"/>,
    /// então Mandamentos podem multiplicar, cortar ou desviar a Fé.
    /// </summary>
    public sealed class FaithIncomeService
    {
        private readonly FaithPool _pool;
        private readonly FaithEconomyConfig _config;
        private readonly RuleModifierSystem _rules;

        /// <summary>Disparado após cada coleta de turno.</summary>
        public event Action<FaithIncomeReport> IncomeCollected;

        /// <summary>
        /// Cria o serviço.
        /// </summary>
        /// <param name="pool">Reserva que recebe a renda.</param>
        /// <param name="config">Parâmetros da economia.</param>
        /// <param name="rules">Pipeline de Mandamentos.</param>
        /// <exception cref="ArgumentNullException">Se alguma dependência for nula.</exception>
        public FaithIncomeService(FaithPool pool, FaithEconomyConfig config, RuleModifierSystem rules)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Rendimento bruto de uma civilização neste turno (zero se herege).
        /// </summary>
        /// <param name="civilization">Civilização.</param>
        /// <returns>Fé gerada antes dos Mandamentos.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="civilization"/> for nula.</exception>
        public int CalculateCivilizationYield(CivilizationEntity civilization)
        {
            if (civilization == null)
            {
                throw new ArgumentNullException(nameof(civilization));
            }

            if (civilization.IsHeretic)
            {
                return 0;
            }

            long yield = (long)civilization.Faith * _config.CivilizationYieldPercent / 100;
            return (int)Math.Min(int.MaxValue, yield);
        }

        /// <summary>
        /// Coleta a renda do turno e deposita na reserva.
        /// Cada fonte passa pelos Mandamentos separadamente, com a fonte como <c>Source</c>
        /// (um Mandamento pode, por exemplo, afetar apenas a renda das civilizações).
        /// </summary>
        /// <param name="civilizations">Civilizações da partida (nulo = nenhuma; entradas nulas são ignoradas).</param>
        /// <returns>Relatório da coleta.</returns>
        public FaithIncomeReport CollectTurnIncome(IEnumerable<CivilizationEntity> civilizations)
        {
            int gross = 0;
            int modified = 0;
            var sideEffects = new List<RuleEffect>();

            Accumulate(_config.BaseIncomePerTurn, this, ref gross, ref modified, sideEffects);

            if (civilizations != null)
            {
                foreach (CivilizationEntity civilization in civilizations)
                {
                    if (civilization != null)
                    {
                        Accumulate(CalculateCivilizationYield(civilization), civilization, ref gross, ref modified, sideEffects);
                    }
                }
            }

            int deposited = _pool.Add(modified);
            var report = new FaithIncomeReport(gross, modified, deposited, sideEffects.AsReadOnly());
            IncomeCollected?.Invoke(report);
            return report;
        }

        /// <summary>
        /// Concede Fé avulsa (recompensas, eventos), passando pelos Mandamentos.
        /// Não dispara <see cref="IncomeCollected"/>, que é exclusivo da renda de turno.
        /// </summary>
        /// <param name="amount">Fé bruta (não negativa).</param>
        /// <param name="source">Origem (ex.: o invasor destruído).</param>
        /// <returns>Relatório da concessão.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for negativo.</exception>
        public FaithIncomeReport GrantFaith(int amount, object source)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Não pode ser negativo.");
            }

            int gross = 0;
            int modified = 0;
            var sideEffects = new List<RuleEffect>();

            Accumulate(amount, source, ref gross, ref modified, sideEffects);

            int deposited = _pool.Add(modified);
            return new FaithIncomeReport(gross, modified, deposited, sideEffects.AsReadOnly());
        }

        private void Accumulate(int amount, object source, ref int gross, ref int modified, List<RuleEffect> sideEffects)
        {
            if (amount <= 0)
            {
                return;
            }

            gross = SaturatingAdd(gross, amount);

            foreach (RuleEffect effect in _rules.Resolve(RuleChannel.ResourceGain, amount, null, source))
            {
                if (effect.Channel == RuleChannel.ResourceGain)
                {
                    modified = SaturatingAdd(modified, effect.Amount);
                }
                else
                {
                    sideEffects.Add(effect);
                }
            }
        }

        private static int SaturatingAdd(int a, int b) => (int)Math.Min(int.MaxValue, (long)a + b);
    }
}
