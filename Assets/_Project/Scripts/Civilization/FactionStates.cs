using System;

namespace SovereignOfAshes.Civilization
{
    /// <summary>
    /// Estado comportamental de uma facção (State Pattern).
    /// Os estados são sem estado interno (stateless) e podem ser compartilhados entre civilizações.
    /// </summary>
    public interface IFactionState
    {
        /// <summary>Lealdade representada por este estado.</summary>
        FactionAllegiance Allegiance { get; }

        /// <summary>
        /// Decide o alvo de ataque da civilização neste turno.
        /// </summary>
        /// <param name="civilization">Civilização que vai atacar.</param>
        /// <returns>O alvo do ataque.</returns>
        AttackTarget ResolveAttackTarget(CivilizationEntity civilization);

        /// <summary>
        /// Avalia se a civilização deve trocar de estado após uma mudança de status.
        /// </summary>
        /// <param name="civilization">Civilização avaliada.</param>
        /// <returns>O próximo estado (pode ser o próprio estado atual).</returns>
        IFactionState EvaluateTransition(CivilizationEntity civilization);
    }

    /// <summary>
    /// Estado devoto: ataca os invasores e vira herege quando a Racionalidade atinge o teto.
    /// </summary>
    public sealed class DevoutState : IFactionState
    {
        /// <summary>Instância compartilhada.</summary>
        public static readonly DevoutState Instance = new DevoutState();

        private DevoutState()
        {
        }

        /// <inheritdoc />
        public FactionAllegiance Allegiance => FactionAllegiance.Devout;

        /// <inheritdoc />
        public AttackTarget ResolveAttackTarget(CivilizationEntity civilization) => AttackTarget.ExternalInvaders;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="civilization"/> for nula.</exception>
        public IFactionState EvaluateTransition(CivilizationEntity civilization)
        {
            if (civilization == null)
            {
                throw new ArgumentNullException(nameof(civilization));
            }

            return civilization.Rationality >= civilization.MaxRationality
                ? (IFactionState)HereticState.Instance
                : this;
        }
    }

    /// <summary>
    /// Estado herege: ataca o Núcleo Divino e só se redime quando a Racionalidade cai até o limiar de redenção.
    /// </summary>
    public sealed class HereticState : IFactionState
    {
        /// <summary>Instância compartilhada.</summary>
        public static readonly HereticState Instance = new HereticState();

        private HereticState()
        {
        }

        /// <inheritdoc />
        public FactionAllegiance Allegiance => FactionAllegiance.Heretic;

        /// <inheritdoc />
        public AttackTarget ResolveAttackTarget(CivilizationEntity civilization) => AttackTarget.DivineCore;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Se <paramref name="civilization"/> for nula.</exception>
        public IFactionState EvaluateTransition(CivilizationEntity civilization)
        {
            if (civilization == null)
            {
                throw new ArgumentNullException(nameof(civilization));
            }

            return civilization.Rationality <= civilization.RedemptionThreshold
                ? (IFactionState)DevoutState.Instance
                : this;
        }
    }
}
