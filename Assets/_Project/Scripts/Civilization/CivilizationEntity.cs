using System;

namespace SovereignOfAshes.Civilization
{
    /// <summary>
    /// O Paradoxo do Livre-Arbítrio: uma civilização autônoma com Poder Militar, Fé e Racionalidade.
    /// Os status só mudam por métodos (encapsulamento forte), sempre limitados aos tetos da configuração.
    /// Após cada mudança o <see cref="IFactionState"/> atual decide se a facção troca de lado.
    /// </summary>
    public sealed class CivilizationEntity
    {
        private readonly CivilizationStatsConfig _config;

        /// <summary>Disparado após qualquer mudança efetiva de status.</summary>
        public event Action<CivilizationEntity> StatsChanged;

        /// <summary>Disparado quando o estado muda. Parâmetros: civilização, estado anterior, estado novo.</summary>
        public event Action<CivilizationEntity, IFactionState, IFactionState> StateChanged;

        /// <summary>Disparado especificamente quando a civilização se torna herege.</summary>
        public event Action<CivilizationEntity> HeresyDeclared;

        /// <summary>Nome da civilização.</summary>
        public string Name { get; }

        /// <summary>Poder militar atual (não negativo).</summary>
        public int MilitaryPower { get; private set; }

        /// <summary>Fé atual, em [0, <see cref="MaxFaith"/>].</summary>
        public int Faith { get; private set; }

        /// <summary>Racionalidade atual, em [0, <see cref="MaxRationality"/>].</summary>
        public int Rationality { get; private set; }

        /// <summary>Teto de Fé.</summary>
        public int MaxFaith => _config.MaxFaith;

        /// <summary>Teto de Racionalidade (gatilho da Heresia).</summary>
        public int MaxRationality => _config.MaxRationality;

        /// <summary>Limiar de Racionalidade para um herege se redimir.</summary>
        public int RedemptionThreshold => _config.RedemptionThreshold;

        /// <summary>Estado comportamental atual.</summary>
        public IFactionState State { get; private set; }

        /// <summary>Indica se a facção é herege.</summary>
        public bool IsHeretic => State.Allegiance == FactionAllegiance.Heretic;

        /// <summary>
        /// Cria a civilização.
        /// </summary>
        /// <param name="name">Nome (não vazio).</param>
        /// <param name="config">Configuração de status.</param>
        /// <exception cref="ArgumentException">Se <paramref name="name"/> for vazio.</exception>
        /// <exception cref="ArgumentNullException">Se <paramref name="config"/> for nula.</exception>
        public CivilizationEntity(string name, CivilizationStatsConfig config)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome da civilização não pode ser vazio.", nameof(name));
            }

            _config = config ?? throw new ArgumentNullException(nameof(config));
            Name = name;
            MilitaryPower = config.InitialMilitaryPower;
            Faith = config.InitialFaith;
            Rationality = config.InitialRationality;
            State = DevoutState.Instance;
            State = State.EvaluateTransition(this);
        }

        /// <summary>
        /// Decide o alvo de ataque neste turno (delegado ao estado atual).
        /// </summary>
        /// <returns>O alvo do ataque.</returns>
        public AttackTarget GetAttackTarget() => State.ResolveAttackTarget(this);

        /// <summary>
        /// Altera o Poder Militar (mínimo zero).
        /// </summary>
        /// <param name="delta">Variação (positiva ou negativa).</param>
        public void ChangeMilitaryPower(int delta)
        {
            int newValue = Clamp((long)MilitaryPower + delta, 0, int.MaxValue);
            ApplyStats(newValue, Faith, Rationality);
        }

        /// <summary>
        /// Altera a Fé, limitada a [0, <see cref="MaxFaith"/>].
        /// </summary>
        /// <param name="delta">Variação (positiva ou negativa).</param>
        public void ChangeFaith(int delta)
        {
            int newValue = Clamp((long)Faith + delta, 0, MaxFaith);
            ApplyStats(MilitaryPower, newValue, Rationality);
        }

        /// <summary>
        /// Altera a Racionalidade, limitada a [0, <see cref="MaxRationality"/>].
        /// Atingir o teto transforma a facção em herege.
        /// </summary>
        /// <param name="delta">Variação (positiva ou negativa).</param>
        public void ChangeRationality(int delta)
        {
            int newValue = Clamp((long)Rationality + delta, 0, MaxRationality);
            ApplyStats(MilitaryPower, Faith, newValue);
        }

        private void ApplyStats(int militaryPower, int faith, int rationality)
        {
            if (militaryPower == MilitaryPower && faith == Faith && rationality == Rationality)
            {
                return;
            }

            MilitaryPower = militaryPower;
            Faith = faith;
            Rationality = rationality;
            StatsChanged?.Invoke(this);
            EvaluateState();
        }

        private void EvaluateState()
        {
            IFactionState next = State.EvaluateTransition(this);

            if (next == null || ReferenceEquals(next, State))
            {
                return;
            }

            IFactionState previous = State;
            State = next;
            StateChanged?.Invoke(this, previous, next);

            if (next.Allegiance == FactionAllegiance.Heretic)
            {
                HeresyDeclared?.Invoke(this);
            }
        }

        private static int Clamp(long value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : (int)value;
        }
    }
}
