using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SovereignOfAshes.Alchemy;
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

        /// <summary>Ciclo de turnos.</summary>
        public TurnSystem Turns { get; }

        /// <summary>Ações do jogador.</summary>
        public PlayerActionService Player { get; }

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

            var services = new CardPlayServices(Board, Alchemy, Invaders, Combat, DivineCore, _civilizations);
            Cards = new CardResolver(services, Faith);
            Deck = new CardDeck(config.StartingDeck, config.HandSize, deckRandom);

            TectonicPlanner = config.TectonicPlanner;
            InvasionPlanner = config.InvasionPlanner;
            TurnsToSurvive = config.TurnsToSurvive;

            IncomePhase = new FaithIncomePhase(FaithIncome, _civilizations);
            CivilizationPhase = new CivilizationPhase(_civilizations, Combat, DivineCore, Invaders, config.RationalityGrowthPerTurn);
            AdvancePhase = new InvaderAdvancePhase(Invaders, Board, Combat, DivineCore);
            SpawnPhase = new InvaderSpawnPhase(Invaders, Board, InvasionPlanner, spawnRandom);

            Turns = new TurnSystem(new ITurnPhase[]
            {
                IncomePhase,
                new DrawPhase(Deck),
                new PlayerActionPhase(),
                new TectonicShiftPhase(Board, TectonicPlanner),
                CivilizationPhase,
                AdvancePhase,
                SpawnPhase,
                new GameOverCheckPhase(DivineCore, TurnsToSurvive)
            });

            Player = new PlayerActionService(Turns, Deck, Cards);
            Log = new GameLog();
            _narrator = new GameNarrator(this, Log);
        }

        /// <summary>
        /// Começa a partida (turno 1).
        /// </summary>
        /// <exception cref="InvalidOperationException">Se já tiver começado.</exception>
        public void Start()
        {
            Log.Add("O Trono desperta sobre a Crosta Viva. Proteja o Núcleo Divino.", LogTone.Highlight, 0);
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
            Bounty.Dispose();
            _disposed = true;
        }
    }
}
