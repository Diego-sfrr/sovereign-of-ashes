using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Alchemy.Effects;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Turns;
using SovereignOfAshes.Turns.Phases;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Uma partida completa, montada a partir de um <see cref="GameSessionConfig"/> (Composition Root em C# puro).
    /// Dono de todos os sistemas; a interface só lê estado, ouve eventos e chama <see cref="Player"/>.
    /// </summary>
    public sealed class GameSession : IDisposable
    {
        private readonly List<CivilizationEntity> _civilizations = new List<CivilizationEntity>();
        private readonly List<Action<GameSession>> _startHooks = new List<Action<GameSession>>();
        private readonly GameNarrator _narrator;
        private bool _disposed;

        /// <summary>Tabuleiro.</summary>
        public TectonicBoard Board { get; }

        /// <summary>Pipeline de Mandamentos.</summary>
        public RuleModifierSystem Rules { get; }

        /// <summary>Resolvedor de combate.</summary>
        public CombatResolver Combat { get; }

        /// <summary>Núcleo Divino.</summary>
        public Combatant DivineCore { get; }

        /// <summary>Alquimia.</summary>
        public AlchemySystem Alchemy { get; }

        /// <summary>Receitas da alquimia (se implementar <see cref="IAlchemyRecipeCatalog"/>, a interface consegue listá-las).</summary>
        public IAlchemyRecipeProvider Recipes { get; }

        /// <summary>Efeitos de jogo dos elementos da alquimia.</summary>
        public ElementEffectSystem ElementEffects { get; }

        /// <summary>Civilizações.</summary>
        public IReadOnlyList<CivilizationEntity> Civilizations { get; }

        /// <summary>Reserva de Fé.</summary>
        public FaithPool Faith { get; }

        /// <summary>Renda de Fé.</summary>
        public FaithIncomeService FaithIncome { get; }

        /// <summary>Invasores.</summary>
        public InvaderRoster Invaders { get; }

        /// <summary>Recompensa por invasores abatidos.</summary>
        public InvaderBountyRewarder Bounty { get; }

        /// <summary>Baralho.</summary>
        public CardDeck Deck { get; }

        /// <summary>Resolvedor de cartas.</summary>
        public CardResolver Cards { get; }

        /// <summary>Rotações planejadas (para a interface prever o próximo giro).</summary>
        public ITectonicShiftPlanner TectonicPlanner { get; }

        /// <summary>Ondas planejadas (para a interface prever a próxima onda).</summary>
        public IInvasionPlanner InvasionPlanner { get; }

        /// <summary>Turnos a sobreviver para vencer.</summary>
        public int TurnsToSurvive { get; }

        /// <summary>Fase de renda (eventos para a crônica).</summary>
        public FaithIncomePhase IncomePhase { get; }

        /// <summary>Fase das civilizações (eventos para a crônica).</summary>
        public CivilizationPhase CivilizationPhase { get; }

        /// <summary>Fase da marcha (eventos para a crônica).</summary>
        public InvaderAdvancePhase AdvancePhase { get; }

        /// <summary>Fase do surgimento (eventos para a crônica).</summary>
        public InvaderSpawnPhase SpawnPhase { get; }

        /// <summary>Fase das habilidades dos invasores (Profanador, Arauto, Inquisidor).</summary>
        public InvaderTraitsPhase TraitsPhase { get; }

        /// <summary>Habilidades de morte dos invasores (a explosão do Flagelo).</summary>
        public InvaderDeathTraits DeathTraits { get; }

        /// <summary>Ciclo de turnos.</summary>
        public TurnSystem Turns { get; }

        /// <summary>Ações do jogador.</summary>
        public PlayerActionService Player { get; }

        /// <summary>Oferendas Divinas (recompensas de cartas entre as ondas).</summary>
        public RewardSystem Rewards { get; }

        /// <summary>Intervenção Divina: girar ou congelar anéis gastando Fé.</summary>
        public RingControlService RingControl { get; }

        /// <summary>Crônica da partida.</summary>
        public GameLog Log { get; }

        /// <summary>
        /// Monta a partida (ainda não iniciada; chame <see cref="Start"/>).
        /// </summary>
        /// <param name="config">Configuração completa.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="config"/> for nulo.</exception>
        /// <exception cref="InvalidOperationException">Se a configuração estiver incompleta.</exception>
        public GameSession(GameSessionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Validate();

            int seed = config.Seed != 0 ? config.Seed : Environment.TickCount;
            var deckRandom = new Random(seed);
            var spawnRandom = new Random(unchecked(seed * 31 + 7));

            Board = new TectonicBoard(config.SectorsPerRing);
            Rules = new RuleModifierSystem();

            foreach (ICommandment commandment in config.Commandments)
            {
                if (commandment != null)
                {
                    Rules.AddCommandment(commandment);
                }
            }

            Combat = new CombatResolver(Rules);
            DivineCore = new Combatant("Núcleo Divino", config.CoreMaxHealth, 0);
            Recipes = config.Recipes;
            Alchemy = new AlchemySystem(config.Recipes, new DefaultElementFactory());

            foreach (CivilizationSetup setup in config.Civilizations)
            {
                if (setup != null)
                {
                    _civilizations.Add(new CivilizationEntity(setup.Name, setup.Stats));
                }
            }

            Civilizations = new ReadOnlyCollection<CivilizationEntity>(_civilizations);

            Faith = config.Faith.CreatePool();
            FaithIncome = new FaithIncomeService(Faith, config.Faith, Rules);
            Invaders = new InvaderRoster(Board);
            Bounty = new InvaderBountyRewarder(Invaders, FaithIncome);

            ElementEffects = new ElementEffectSystem(
                config.ElementEffects,
                new ElementEffectServices(Board, Invaders, Combat, DivineCore, _civilizations, FaithIncome));

            DeathTraits = new InvaderDeathTraits(Invaders, Board, Combat);

            var services = new CardPlayServices(Board, Alchemy, Invaders, Combat, DivineCore, _civilizations, Faith);
            Cards = new CardResolver(services, Faith);
            Deck = new CardDeck(config.StartingDeck, config.HandSize, deckRandom);

            RingControl = new RingControlService(Board, Faith, () => Player != null && Player.CanAct);
            TectonicPlanner = config.TectonicPlanner;
            InvasionPlanner = config.InvasionPlanner;
            TurnsToSurvive = config.TurnsToSurvive;

            IncomePhase = new FaithIncomePhase(FaithIncome, _civilizations);
            CivilizationPhase = new CivilizationPhase(_civilizations, Combat, DivineCore, Invaders, config.RationalityGrowthPerTurn);
            AdvancePhase = new InvaderAdvancePhase(Invaders, Board, Combat, DivineCore, ElementEffects);
            TraitsPhase = new InvaderTraitsPhase(Invaders, _civilizations, Board);
            SpawnPhase = new InvaderSpawnPhase(Invaders, Board, InvasionPlanner, spawnRandom);

            Turns = new TurnSystem(new ITurnPhase[]
            {
                IncomePhase,
                new DrawPhase(Deck),
                new PlayerActionPhase(),
                new TectonicShiftPhase(Board, TectonicPlanner, RingControl.FilterPlanned, RingControl.ClearFrozen),
                CivilizationPhase,
                AdvancePhase,
                TraitsPhase,
                new ElementEffectsPhase(ElementEffects, Invaders),
                SpawnPhase,
                new GameOverCheckPhase(DivineCore, TurnsToSurvive)
            });

            Rewards = new RewardSystem(Deck, Faith, Turns, config.RewardPool, new Random(unchecked(seed * 17 + 3)),
                config.RewardEveryNTurns, config.RewardSkipFaith, config.RewardNiche);
            Player = new PlayerActionService(Turns, Deck, Cards) { ActionBlocker = () => Rewards.HasPending };
            Turns.AwaitingPlayer += _ => RingControl.BeginTurn();
            _startHooks.AddRange(config.StartHooks);
            Log = new GameLog();
            _narrator = new GameNarrator(this, Log);
        }

        /// <summary>
        /// Rotações que vão acontecer na fase tectônica do turno (já sem os anéis congelados). É o que a previsão mostra.
        /// </summary>
        /// <param name="turn">Turno.</param>
        /// <returns>Rotações previstas.</returns>
        public IReadOnlyList<RingRotation> PlannedShifts(int turn) => RingControl.FilterPlanned(TectonicPlanner.PlanShifts(turn));

        /// <summary>
        /// Começa a partida (turno 1).
        /// </summary>
        /// <exception cref="InvalidOperationException">Se já tiver começado.</exception>
        public void Start()
        {
            foreach (string line in Story.Opening)
            {
                Log.Add(line, LogTone.Highlight, 0);
            }

            if (_civilizations.Count > 0)
            {
                string quote = Story.CivilizationQuote(Story.CivilizationIdOf(_civilizations[0].Name));

                if (quote.Length > 0)
                {
                    Log.Add(_civilizations[0].Name + ": \"" + quote + "\"", LogTone.Info, 0);
                }
            }

            if (TurnsToSurvive == 0)
            {
                foreach (string line in Story.EndlessEntry)
                {
                    Log.Add(line, LogTone.Highlight, 0);
                }
            }

            foreach (Action<GameSession> hook in _startHooks)
            {
                hook(this);
            }

            Turns.StartGame();
        }

        /// <summary>
        /// Libera as assinaturas de eventos internas.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _narrator.Dispose();
            Rewards.Dispose();
            Bounty.Dispose();
            DeathTraits.Dispose();
            _disposed = true;
        }
    }
}
