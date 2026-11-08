using System;
using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Commandments;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Ponte entre o Unity e a lógica: converte os ScriptableObjects (todos opcionais) em um
    /// <see cref="GameSessionConfig"/>, cria a <see cref="GameSession"/> e controla seu ciclo de vida.
    /// Campos vazios usam o <see cref="DefaultContent"/>, então a partida roda sem nenhum asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SovereignBootstrapper : MonoBehaviour
    {
        [Header("Tabuleiro")]
        [SerializeField, Min(3)] private int _sectorsPerRing = 8;
        [SerializeField, Min(1)] private int _coreMaxHealth = 30;

        [Header("Conteúdo (opcional — vazio usa o conteúdo padrão)")]
        [SerializeField] private AlchemyRecipeBookSO _recipeBook;
        [SerializeField] private List<CardDefinitionSO> _startingDeck = new List<CardDefinitionSO>();
        [SerializeField, Min(1)] private int _handSize = 5;
        [SerializeField] private List<CivilizationProfileSO> _civilizationProfiles = new List<CivilizationProfileSO>();
        [SerializeField] private List<CommandmentDefinitionSO> _startingCommandments = new List<CommandmentDefinitionSO>();
        [SerializeField] private FaithEconomyProfileSO _faithEconomy;
        [SerializeField] private TurnProfileSO _turnProfile;
        [SerializeField] private InvasionProfileSO _invasionProfile;

        [Header("Partida")]
        [Tooltip("Semente do embaralhamento e dos invasores. 0 = aleatória.")]
        [SerializeField] private int _seed;

        [Tooltip("Copia a crônica da partida para o Console do Unity.")]
        [SerializeField] private bool _mirrorLogToConsole = true;

        /// <summary>Disparado sempre que uma nova partida é criada (inclusive ao reiniciar).</summary>
        public event Action<GameSession> SessionCreated;

        /// <summary>Partida atual.</summary>
        public GameSession Session { get; private set; }

        private void Awake()
        {
            CreateSession();
        }

        private void Start()
        {
            Session.Start();
        }

        private void OnDestroy()
        {
            DisposeSession();
        }

        /// <summary>
        /// Descarta a partida atual e começa outra do zero.
        /// </summary>
        public void RestartSession()
        {
            DisposeSession();
            CreateSession();
            Session.Start();
        }

        /// <summary>
        /// Encerra a vez do jogador (também disponível pelo menu de contexto do componente).
        /// </summary>
        [ContextMenu("Encerrar Turno")]
        public void EndTurn()
        {
            if (Session == null || !Session.Player.EndTurn())
            {
                Debug.LogWarning("Não é possível encerrar o turno agora.", this);
            }
        }

        private void CreateSession()
        {
            Session = new GameSession(BuildConfig());

            if (_mirrorLogToConsole)
            {
                Session.Log.EntryAdded += MirrorToConsole;
            }

            SessionCreated?.Invoke(Session);
        }

        private void DisposeSession()
        {
            if (Session == null)
            {
                return;
            }

            Session.Log.EntryAdded -= MirrorToConsole;
            Session.Dispose();
            Session = null;
        }

        private GameSessionConfig BuildConfig()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.SectorsPerRing = _sectorsPerRing;
            config.CoreMaxHealth = _coreMaxHealth;
            config.HandSize = _handSize;
            config.Seed = _seed;

            if (_recipeBook != null)
            {
                config.Recipes = _recipeBook;
            }

            if (HasAny(_startingDeck))
            {
                config.StartingDeck.Clear();

                foreach (CardDefinitionSO card in _startingDeck)
                {
                    if (card != null)
                    {
                        config.StartingDeck.Add(card);
                    }
                }
            }

            if (HasAny(_civilizationProfiles))
            {
                config.Civilizations.Clear();

                foreach (CivilizationProfileSO profile in _civilizationProfiles)
                {
                    if (profile != null)
                    {
                        config.Civilizations.Add(new CivilizationSetup(profile.CivilizationName, profile.CreateConfig()));
                    }
                }
            }

            foreach (CommandmentDefinitionSO commandment in _startingCommandments)
            {
                if (commandment != null)
                {
                    config.Commandments.Add(commandment.CreateCommandment());
                }
            }

            if (_faithEconomy != null)
            {
                config.Faith = _faithEconomy.CreateConfig();
            }

            if (_turnProfile != null)
            {
                config.TectonicPlanner = _turnProfile.CreatePlanner();
                config.RationalityGrowthPerTurn = _turnProfile.RationalityGrowthPerTurn;
                config.TurnsToSurvive = _turnProfile.TurnsToSurvive;
            }

            if (_invasionProfile != null)
            {
                config.InvasionPlanner = _invasionProfile.CreatePlanner();

                if (config.Seed == 0)
                {
                    config.Seed = _invasionProfile.SpawnSeed;
                }
            }

            return config;
        }

        private void MirrorToConsole(LogEntry entry)
        {
            if (entry.Tone == LogTone.Bad)
            {
                Debug.LogWarning($"[T{entry.Turn}] {entry.Text}", this);
            }
            else
            {
                Debug.Log($"[T{entry.Turn}] {entry.Text}", this);
            }
        }

        private static bool HasAny<T>(List<T> list) where T : class
        {
            if (list == null)
            {
                return false;
            }

            foreach (T item in list)
            {
                if (item != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
