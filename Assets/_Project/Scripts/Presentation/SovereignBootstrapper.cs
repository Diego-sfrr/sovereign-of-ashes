using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Rules.Commandments;
using SovereignOfAshes.Turns;
using SovereignOfAshes.Turns.Phases;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Composition Root: único MonoBehaviour que conhece todos os sistemas.
    /// Cria os POCOs, injeta as dependências, monta o ciclo de turnos e conecta os eventos à camada de visão.
    /// Nenhuma regra de jogo vive aqui.
    /// </summary>
    public sealed class SovereignBootstrapper : MonoBehaviour
    {
        [Header("Tabuleiro")]
        [SerializeField, Min(TectonicBoard.MinSectorsPerRing)] private int _sectorsPerRing = 8;

        [Header("Dados")]
        [SerializeField] private AlchemyRecipeBookSO _recipeBook;
        [SerializeField] private List<CivilizationProfileSO> _civilizationProfiles = new List<CivilizationProfileSO>();
        [SerializeField] private List<CommandmentDefinitionSO> _startingCommandments = new List<CommandmentDefinitionSO>();

        [Header("Núcleo Divino")]
        [SerializeField, Min(1)] private int _coreMaxHealth = 30;

        [Header("Economia de Fé")]
        [Tooltip("Opcional. Sem perfil, usa Fé inicial 3, capacidade 10, renda base 2 e 20% das civilizações.")]
        [SerializeField] private FaithEconomyProfileSO _faithEconomy;

        [Header("Turnos")]
        [Tooltip("Opcional. Sem perfil, o anel externo gira 1 casa horária por turno, Racionalidade +1 por turno e vitória no turno 20.")]
        [SerializeField] private TurnProfileSO _turnProfile;

        [Header("Invasores")]
        [Tooltip("Opcional. Sem perfil: 1 Cultista Cinzento (HP 5, ATK 2, 1 de Fé) no turno 1, repetindo a cada 2 turnos com +1 por repetição.")]
        [SerializeField] private InvasionProfileSO _invasionProfile;

        [Header("Demo")]
        [SerializeField] private CardDefinitionSO _demoCard;

        private readonly List<CivilizationEntity> _civilizations = new List<CivilizationEntity>();
        private FaithIncomePhase _incomePhase;
        private CivilizationPhase _civilizationPhase;
        private InvaderAdvancePhase _advancePhase;
        private InvaderSpawnPhase _spawnPhase;
        private InvaderBountyRewarder _bounty;

        /// <summary>Invasores no tabuleiro.</summary>
        public InvaderRoster Invaders { get; private set; }

        /// <summary>Tabuleiro da partida.</summary>
        public TectonicBoard Board { get; private set; }

        /// <summary>Sistema de alquimia (nulo se nenhum grimório for atribuído).</summary>
        public AlchemySystem Alchemy { get; private set; }

        /// <summary>Pipeline de Mandamentos.</summary>
        public RuleModifierSystem Rules { get; private set; }

        /// <summary>Resolvedor de dano e cura.</summary>
        public CombatResolver Combat { get; private set; }

        /// <summary>Núcleo Divino do jogador.</summary>
        public Combatant DivineCore { get; private set; }

        /// <summary>Reserva de Fé do jogador.</summary>
        public FaithPool Faith { get; private set; }

        /// <summary>Serviço de renda de Fé por turno.</summary>
        public FaithIncomeService FaithIncome { get; private set; }

        /// <summary>Resolvedor de cartas (nulo se a alquimia estiver desativada).</summary>
        public CardResolver Cards { get; private set; }

        /// <summary>Ciclo de turnos.</summary>
        public TurnSystem Turns { get; private set; }

        /// <summary>Civilizações da partida.</summary>
        public IReadOnlyList<CivilizationEntity> Civilizations => _civilizations;

        private void Awake()
        {
            Board = new TectonicBoard(_sectorsPerRing);
            Rules = new RuleModifierSystem();
            Combat = new CombatResolver(Rules);
            DivineCore = new Combatant("Núcleo Divino", _coreMaxHealth, 0);

            foreach (CommandmentDefinitionSO definition in _startingCommandments)
            {
                if (definition != null)
                {
                    Rules.AddCommandment(definition.CreateCommandment());
                }
            }

            if (_recipeBook != null)
            {
                Alchemy = new AlchemySystem(_recipeBook, new DefaultElementFactory());
            }
            else
            {
                Debug.LogWarning($"[{nameof(SovereignBootstrapper)}] Nenhum Recipe Book atribuído; alquimia e cartas desativadas.", this);
            }

            foreach (CivilizationProfileSO profile in _civilizationProfiles)
            {
                if (profile != null)
                {
                    _civilizations.Add(profile.CreateEntity());
                }
            }

            FaithEconomyConfig faithConfig = _faithEconomy != null
                ? _faithEconomy.CreateConfig()
                : new FaithEconomyConfig(startingFaith: 3, capacity: 10, baseIncomePerTurn: 2, civilizationYieldPercent: 20);

            Faith = faithConfig.CreatePool();
            FaithIncome = new FaithIncomeService(Faith, faithConfig, Rules);

            if (Alchemy != null)
            {
                Cards = new CardResolver(Board, Alchemy, Faith);
            }

            Invaders = new InvaderRoster(Board);
            _bounty = new InvaderBountyRewarder(Invaders, FaithIncome);

            Turns = BuildTurnSystem();
            Subscribe();
        }

        private void Start()
        {
            Turns.StartGame();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _bounty?.Dispose();
        }

        private TurnSystem BuildTurnSystem()
        {
            ITectonicShiftPlanner planner;
            int rationalityGrowth;
            int turnsToSurvive;

            if (_turnProfile != null)
            {
                planner = _turnProfile.CreatePlanner();
                rationalityGrowth = _turnProfile.RationalityGrowthPerTurn;
                turnsToSurvive = _turnProfile.TurnsToSurvive;
            }
            else
            {
                planner = new PatternTectonicShiftPlanner(new[]
                {
                    new RingShiftRule(new RingRotation(RingType.OuterWilderness, 1, RotationDirection.Clockwise), 1)
                });
                rationalityGrowth = 1;
                turnsToSurvive = 20;
            }

            IInvasionPlanner invasion;
            int seed = 0;

            if (_invasionProfile != null)
            {
                invasion = _invasionProfile.CreatePlanner();
                seed = _invasionProfile.SpawnSeed;
            }
            else
            {
                var cultist = new SimpleInvaderDefinition("ash_cultist", "Cultista Cinzento", maxHealth: 5, attack: 2, faithBounty: 1);
                invasion = new WaveInvasionPlanner(new[]
                {
                    new InvasionWave(cultist, firstTurn: 1, baseCount: 1, everyNTurns: 2, countGrowthPerRepeat: 1)
                });
            }

            var random = seed == 0 ? new System.Random() : new System.Random(seed);

            _incomePhase = new FaithIncomePhase(FaithIncome, _civilizations);
            _civilizationPhase = new CivilizationPhase(_civilizations, Combat, DivineCore, Invaders, rationalityGrowth);
            _advancePhase = new InvaderAdvancePhase(Invaders, Board, Combat, DivineCore);
            _spawnPhase = new InvaderSpawnPhase(Invaders, Board, invasion, random);

            return new TurnSystem(new ITurnPhase[]
            {
                _incomePhase,
                new PlayerActionPhase(),
                new TectonicShiftPhase(Board, planner),
                _civilizationPhase,
                _advancePhase,
                _spawnPhase,
                new GameOverCheckPhase(DivineCore, turnsToSurvive)
            });
        }

        /// <summary>
        /// Joga a carta de demo no primeiro setor do anel do meio (botão direito &gt; Jogar Carta Demo).
        /// Só funciona na fase do jogador.
        /// </summary>
        [ContextMenu("Jogar Carta Demo")]
        private void PlayDemoCard()
        {
            if (Turns == null || Cards == null || _demoCard == null)
            {
                Debug.LogWarning("Precisa estar em Play Mode, com Recipe Book e Demo Card atribuídos.", this);
                return;
            }

            if (!Turns.IsPlayerTurn)
            {
                Debug.LogWarning($"Não é a vez do jogador (estado: {Turns.State}).", this);
                return;
            }

            TectonicSector target = Board.GetSectorAt(RingType.MiddleCivilization, 0);
            CardPlayOutcome outcome = Cards.Play(_demoCard, target);
            Debug.Log($"Carta '{_demoCard.DisplayName}' (custo {_demoCard.Cost}) em {target}: {outcome}. {Faith}");
        }

        /// <summary>
        /// Encerra a fase do jogador; o jogo executa rotação, civilizações, julgamento e a renda do próximo turno
        /// (botão direito &gt; Encerrar Turno).
        /// </summary>
        [ContextMenu("Encerrar Turno")]
        private void EndTurn()
        {
            if (Turns == null)
            {
                Debug.LogWarning("Entre em Play Mode primeiro.", this);
                return;
            }

            if (!Turns.IsPlayerTurn)
            {
                Debug.LogWarning($"Não é possível encerrar o turno agora (estado: {Turns.State}).", this);
                return;
            }

            Turns.EndPlayerTurn();
        }

        private void Subscribe()
        {
            Board.RingRotated += HandleRingRotated;
            Rules.CommandmentTriggered += HandleCommandmentTriggered;
            Faith.Changed += HandleFaithChanged;

            if (Alchemy != null)
            {
                Alchemy.ReactionResolved += HandleReaction;
            }

            foreach (CivilizationEntity civilization in _civilizations)
            {
                civilization.HeresyDeclared += HandleHeresy;
            }

            _incomePhase.IncomeCollected += HandleIncome;
            _civilizationPhase.CivilizationAttacked += HandleCivilizationAttack;
            _civilizationPhase.InvaderStruck += HandleInvaderStruck;
            _advancePhase.InvaderAttackedCore += HandleInvaderAttackedCore;
            _spawnPhase.SpawnOverflow += HandleSpawnOverflow;
            Invaders.InvaderSpawned += HandleInvaderSpawned;
            Invaders.InvaderMoved += HandleInvaderMoved;
            Invaders.InvaderSlain += HandleInvaderSlain;
            _bounty.BountyGranted += HandleBounty;
            Turns.TurnStarted += HandleTurnStarted;
            Turns.AwaitingPlayer += HandleAwaitingPlayer;
            Turns.GameOver += HandleGameOver;
        }

        private void Unsubscribe()
        {
            if (Turns == null)
            {
                return;
            }

            Board.RingRotated -= HandleRingRotated;
            Rules.CommandmentTriggered -= HandleCommandmentTriggered;
            Faith.Changed -= HandleFaithChanged;

            if (Alchemy != null)
            {
                Alchemy.ReactionResolved -= HandleReaction;
            }

            foreach (CivilizationEntity civilization in _civilizations)
            {
                civilization.HeresyDeclared -= HandleHeresy;
            }

            _incomePhase.IncomeCollected -= HandleIncome;
            _civilizationPhase.CivilizationAttacked -= HandleCivilizationAttack;
            _civilizationPhase.InvaderStruck -= HandleInvaderStruck;
            _advancePhase.InvaderAttackedCore -= HandleInvaderAttackedCore;
            _spawnPhase.SpawnOverflow -= HandleSpawnOverflow;
            Invaders.InvaderSpawned -= HandleInvaderSpawned;
            Invaders.InvaderMoved -= HandleInvaderMoved;
            Invaders.InvaderSlain -= HandleInvaderSlain;
            _bounty.BountyGranted -= HandleBounty;
            Turns.TurnStarted -= HandleTurnStarted;
            Turns.AwaitingPlayer -= HandleAwaitingPlayer;
            Turns.GameOver -= HandleGameOver;
        }

        private void HandleTurnStarted(int turn) => Debug.Log($"===== TURNO {turn} =====");

        private void HandleAwaitingPlayer(int turn) =>
            Debug.Log($"Sua vez (turno {turn}). {Faith} | {DivineCore}. Jogue cartas e use 'Encerrar Turno'.");

        private void HandleGameOver(GameOverInfo info) =>
            Debug.LogWarning($"FIM DE JOGO ({info.Outcome}) no turno {info.TurnNumber}: {info.Reason}");

        private void HandleIncome(FaithIncomeReport report) =>
            Debug.Log($"Renda de Fé: bruta {report.GrossIncome}, após Mandamentos {report.ModifiedIncome}, " +
                      $"depositada {report.Deposited}, perdida {report.Overflow}.");

        private void HandleCivilizationAttack(CivilizationEntity civilization, AttackTarget target, int power) =>
            Debug.Log($"{civilization.Name} (Racionalidade {civilization.Rationality}/{civilization.MaxRationality}) " +
                      $"ataca {target} com poder {power}.");

        private void HandleInvaderSpawned(InvaderEntity invader) =>
            Debug.Log($"Invasor surgiu: {invader} (posição {Board.GetPositionOf(invader.Sector)}).");

        private void HandleInvaderMoved(InvaderEntity invader, TectonicSector from, TectonicSector to) =>
            Debug.Log($"{invader.Name}#{invader.Id} marchou de {from.Ring} para {to.Ring}.");

        private void HandleInvaderStruck(CivilizationEntity civilization, InvaderEntity invader) =>
            Debug.Log($"{civilization.Name} golpeou {invader}.");

        private void HandleInvaderAttackedCore(InvaderEntity invader, int damage) =>
            Debug.LogWarning($"{invader.Name}#{invader.Id} atacou o Núcleo ({damage}). {DivineCore}");

        private void HandleInvaderSlain(InvaderEntity invader) =>
            Debug.Log($"{invader.Name}#{invader.Id} foi destruído.");

        private void HandleBounty(InvaderEntity invader, FaithIncomeReport report) =>
            Debug.Log($"Recompensa por {invader.Name}: +{report.Deposited} de Fé.");

        private void HandleSpawnOverflow(IInvaderDefinition definition, int missing) =>
            Debug.LogWarning($"Os Ermos estão lotados: {missing}x {definition.DisplayName} não conseguiram surgir.");

        private void HandleRingRotated(RingType ring, int delta) => Debug.Log($"Anel {ring} girou {delta:+#;-#;0}.");

        private void HandleReaction(AlchemyResult result) =>
            Debug.Log($"Alquimia {result.Outcome}: {result.Sector} ({result.Previous?.Definition?.DisplayName ?? "vazio"} + {result.Concept})");

        private void HandleCommandmentTriggered(ICommandment commandment, RuleContext context) =>
            Debug.Log($"Mandamento '{commandment.DisplayName}' alterou {context.Channel}: {context.BaseAmount} -> {context.Amount}");

        private void HandleFaithChanged(FaithPool pool, int previous, int current) =>
            Debug.Log($"Fé: {previous} -> {current} (capacidade {pool.Capacity})");

        private void HandleHeresy(CivilizationEntity civilization) =>
            Debug.LogWarning($"HERESIA! {civilization.Name} voltou-se contra o Núcleo Divino.");
    }
}
