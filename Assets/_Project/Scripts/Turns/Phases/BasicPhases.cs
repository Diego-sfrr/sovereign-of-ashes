using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Turns.Phases
{
    /// <summary>
    /// Fase de Renda: coleta a Fé do turno (Núcleo + civilizações devotas).
    /// </summary>
    public sealed class FaithIncomePhase : ITurnPhase
    {
        private readonly FaithIncomeService _income;
        private readonly IReadOnlyCollection<CivilizationEntity> _civilizations;

        /// <summary>Disparado com o relatório de cada coleta.</summary>
        public event Action<FaithIncomeReport> IncomeCollected;

        /// <inheritdoc />
        public string Name => "Renda de Fé";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="income">Serviço de renda.</param>
        /// <param name="civilizations">Civilizações da partida (a coleção é lida a cada turno, então pode crescer).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public FaithIncomePhase(FaithIncomeService income, IReadOnlyCollection<CivilizationEntity> civilizations)
        {
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            FaithIncomeReport report = _income.CollectTurnIncome(_civilizations);
            IncomeCollected?.Invoke(report);
            return PhaseResult.Continue;
        }
    }

    /// <summary>
    /// Fase do Jogador: o jogo para e espera o jogador jogar cartas e encerrar o turno.
    /// </summary>
    public sealed class PlayerActionPhase : ITurnPhase
    {
        /// <inheritdoc />
        public string Name => "Ação do Jogador";

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber) => PhaseResult.AwaitPlayer;
    }

    /// <summary>
    /// Fase Tectônica: aplica as rotações planejadas para o turno.
    /// </summary>
    public sealed class TectonicShiftPhase : ITurnPhase
    {
        private readonly TectonicBoard _board;
        private readonly ITectonicShiftPlanner _planner;

        /// <inheritdoc />
        public string Name => "Rotação Tectônica";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="planner">Planejador das rotações.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public TectonicShiftPhase(TectonicBoard board, ITectonicShiftPlanner planner)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            _board.ApplyRotations(_planner.PlanShifts(turnNumber));
            return PhaseResult.Continue;
        }
    }

    /// <summary>
    /// Fase das Civilizações (Livre-Arbítrio): a Racionalidade de cada civilização cresce e cada uma ataca
    /// seu alvo com todo o seu Poder Militar. Devotas atacam o invasor mais perigoso; hereges atacam o Núcleo Divino.
    /// </summary>
    public sealed class CivilizationPhase : ITurnPhase
    {
        private readonly IReadOnlyCollection<CivilizationEntity> _civilizations;
        private readonly CombatResolver _combat;
        private readonly ICombatant _divineCore;
        private readonly IInvaderTargeting _invaders;

        /// <summary>
        /// Disparado quando uma civilização ataca. Parâmetros: civilização, alvo, poder do ataque.
        /// Se o alvo for <see cref="AttackTarget.ExternalInvaders"/> e não houver invasores, o poder é 0.
        /// </summary>
        public event Action<CivilizationEntity, AttackTarget, int> CivilizationAttacked;

        /// <summary>Disparado quando uma civilização acerta um invasor. Parâmetros: civilização, invasor.</summary>
        public event Action<CivilizationEntity, InvaderEntity> InvaderStruck;

        /// <summary>Quanto de Racionalidade cada civilização ganha por turno.</summary>
        public int RationalityGrowthPerTurn { get; }

        /// <inheritdoc />
        public string Name => "Livre-Arbítrio";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="civilizations">Civilizações da partida.</param>
        /// <param name="combat">Resolvedor de combate (aplica os Mandamentos ao dano).</param>
        /// <param name="divineCore">Núcleo Divino do jogador.</param>
        /// <param name="invaders">Alvos das civilizações devotas.</param>
        /// <param name="rationalityGrowthPerTurn">Crescimento de Racionalidade por turno (não negativo).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="rationalityGrowthPerTurn"/> for negativo.</exception>
        public CivilizationPhase(
            IReadOnlyCollection<CivilizationEntity> civilizations,
            CombatResolver combat,
            ICombatant divineCore,
            IInvaderTargeting invaders,
            int rationalityGrowthPerTurn)
        {
            _invaders = invaders ?? throw new ArgumentNullException(nameof(invaders));
            if (rationalityGrowthPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rationalityGrowthPerTurn), rationalityGrowthPerTurn, "Não pode ser negativo.");
            }

            _civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _divineCore = divineCore ?? throw new ArgumentNullException(nameof(divineCore));
            RationalityGrowthPerTurn = rationalityGrowthPerTurn;
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            // Cópia defensiva: um evento pode adicionar ou remover civilizações durante a fase.
            var snapshot = new List<CivilizationEntity>(_civilizations);

            foreach (CivilizationEntity civilization in snapshot)
            {
                if (civilization == null)
                {
                    continue;
                }

                civilization.ChangeRationality(RationalityGrowthPerTurn);

                AttackTarget target = civilization.GetAttackTarget();
                int power = civilization.MilitaryPower;

                if (power > 0 && target == AttackTarget.DivineCore)
                {
                    _combat.DealDamage(_divineCore, power, civilization);
                }
                else if (power > 0 && target == AttackTarget.ExternalInvaders)
                {
                    InvaderEntity invader = _invaders.SelectPriorityTarget();

                    if (invader == null)
                    {
                        power = 0;
                    }
                    else
                    {
                        _combat.DealDamage(invader, power, civilization);
                        InvaderStruck?.Invoke(civilization, invader);
                        _invaders.RemoveDead();
                    }
                }

                CivilizationAttacked?.Invoke(civilization, target, power);
            }

            return PhaseResult.Continue;
        }
    }

    /// <summary>
    /// Fase de Julgamento: derrota se o Núcleo Divino caiu; vitória ao sobreviver ao número de turnos exigido.
    /// </summary>
    public sealed class GameOverCheckPhase : ITurnPhase
    {
        private readonly ICombatant _divineCore;

        /// <summary>Turnos a sobreviver para vencer (0 = sem vitória por sobrevivência).</summary>
        public int TurnsToSurvive { get; }

        /// <inheritdoc />
        public string Name => "Julgamento";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="divineCore">Núcleo Divino.</param>
        /// <param name="turnsToSurvive">Turnos para vencer (0 desativa).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="divineCore"/> for nulo.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="turnsToSurvive"/> for negativo.</exception>
        public GameOverCheckPhase(ICombatant divineCore, int turnsToSurvive)
        {
            if (turnsToSurvive < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnsToSurvive), turnsToSurvive, "Não pode ser negativo.");
            }

            _divineCore = divineCore ?? throw new ArgumentNullException(nameof(divineCore));
            TurnsToSurvive = turnsToSurvive;
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            if (_divineCore.IsDead)
            {
                return PhaseResult.EndGame(GameOutcome.Defeat, "O Núcleo Divino foi destruído. O trono está vazio.");
            }

            if (TurnsToSurvive > 0 && turnNumber >= TurnsToSurvive)
            {
                return PhaseResult.EndGame(GameOutcome.Victory, $"O deus resistiu por {TurnsToSurvive} eras.");
            }

            return PhaseResult.Continue;
        }
    }
}
