using System;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Achievements
{
    /// <summary>
    /// Observa uma partida e concede conquistas conforme o jogador joga. Só escuta os eventos que a
    /// <see cref="GameSession"/> já expõe (não interfere em nenhuma regra) e guarda o progresso via <c>save</c>.
    /// </summary>
    public sealed class AchievementTracker : IDisposable
    {
        private readonly GameSession _session;
        private readonly GameSettings _run;
        private readonly AchievementProgress _progress;
        private readonly Action _save;

        private int _lastCoreHealth;
        private bool _coreDamaged;
        private bool _disposed;

        /// <summary>Disparado quando uma conquista nova é ganha.</summary>
        public event Action<AchievementDefinition> Unlocked;

        /// <summary>
        /// Começa a observar a partida.
        /// </summary>
        /// <param name="session">Partida.</param>
        /// <param name="run">Preferências da partida (dificuldade e modo).</param>
        /// <param name="progress">Progresso a atualizar.</param>
        /// <param name="save">Chamado para gravar o progresso (pode ser nulo).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento obrigatório for nulo.</exception>
        public AchievementTracker(GameSession session, GameSettings run, AchievementProgress progress, Action save)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _save = save;
            _lastCoreHealth = session.DivineCore.CurrentHealth;

            session.Invaders.InvaderSlain += OnInvaderSlain;
            session.Alchemy.ReactionResolved += OnReaction;
            session.Faith.Changed += OnFaithChanged;
            session.DivineCore.Changed += OnCoreChanged;
            session.Turns.TurnStarted += OnTurnStarted;
            session.Turns.TurnEnded += OnTurnEnded;
            session.Turns.GameOver += OnGameOver;
            session.Rewards.OfferResolved += OnOfferResolved;

            foreach (CivilizationEntity civilization in session.Civilizations)
            {
                civilization.HeresyDeclared += OnHeresy;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _session.Invaders.InvaderSlain -= OnInvaderSlain;
            _session.Alchemy.ReactionResolved -= OnReaction;
            _session.Faith.Changed -= OnFaithChanged;
            _session.DivineCore.Changed -= OnCoreChanged;
            _session.Turns.TurnStarted -= OnTurnStarted;
            _session.Turns.TurnEnded -= OnTurnEnded;
            _session.Turns.GameOver -= OnGameOver;
            _session.Rewards.OfferResolved -= OnOfferResolved;

            foreach (CivilizationEntity civilization in _session.Civilizations)
            {
                civilization.HeresyDeclared -= OnHeresy;
            }
        }

        private void OnInvaderSlain(InvaderEntity invader)
        {
            Grant("first_blood");
            int kills = _progress.Add(AchievementCatalog.CounterKills, 1);
            GrantIfReached("exterminator", kills);
            GrantIfReached("horde_reaper", kills);
        }

        private void OnReaction(AlchemyResult result)
        {
            if (result == null || !result.ChangedSector || result.Recipe == null)
            {
                return;
            }

            Grant("alchemist");

            if (result.Recipe.Result != null)
            {
                _progress.AddCraftedElement(result.Recipe.Result.Id);
                GrantIfReached("grand_alchemist", _progress.CraftedElementCount);
            }
        }

        private void OnFaithChanged(FaithPool pool, int before, int after)
        {
            if (after < before)
            {
                GrantIfReached("devout", _progress.Add(AchievementCatalog.CounterFaithSpent, before - after));
            }
        }

        private void OnCoreChanged(Combatant core)
        {
            if (core.CurrentHealth < _lastCoreHealth)
            {
                _coreDamaged = true;
            }

            _lastCoreHealth = core.CurrentHealth;
        }

        private void OnOfferResolved(Cards.ICardDefinition chosen)
        {
            if (chosen == null)
            {
                return;
            }

            Grant("offering");

            if (_session.Deck.TotalCount >= 21)
            {
                Grant("grand_deck");
            }
        }

        private void OnHeresy(CivilizationEntity civilization) => Grant("heresy");

        private void OnTurnStarted(int turn)
        {
            if (_run.Mode != GameMode.Endless)
            {
                return;
            }

            GrantIfReached("endless_10", turn);
            GrantIfReached("endless_25", turn);
            GrantIfReached("endless_50", turn);
        }

        private void OnTurnEnded(int turn) => _save?.Invoke();

        private void OnGameOver(GameOverInfo result)
        {
            GrantIfReached("veteran", _progress.Add(AchievementCatalog.CounterGames, 1));

            if (result.Outcome == GameOutcome.Victory)
            {
                Grant("victory");

                if (!string.IsNullOrEmpty(_run.CovenantId))
                {
                    Grant("covenant_win");
                }

                if (_session.DivineCore.CurrentHealth <= 3)
                {
                    Grant("by_a_thread");
                }

                if (!_coreDamaged)
                {
                    Grant("untouched");
                }

                if (_run.Difficulty == Difficulty.Hard && _run.Mode == GameMode.Standard)
                {
                    Grant("ashes_glory");
                }
            }
            else
            {
                Grant("fallen_throne");
            }

            _save?.Invoke();
        }

        private void GrantIfReached(string id, int value)
        {
            AchievementDefinition definition = AchievementCatalog.Find(id);

            if (definition != null && value >= ThresholdOf(definition))
            {
                Grant(id);
            }
        }

        // Metas por turno do modo infinito; nas demais a meta é a própria Goal.
        private static int ThresholdOf(AchievementDefinition definition)
        {
            switch (definition.Id)
            {
                case "endless_10": return 10;
                case "endless_25": return 25;
                case "endless_50": return 50;
                default: return definition.Goal;
            }
        }

        private void Grant(string id)
        {
            if (!_progress.Unlock(id))
            {
                return;
            }

            _save?.Invoke();
            AchievementDefinition definition = AchievementCatalog.Find(id);

            if (definition != null)
            {
                Unlocked?.Invoke(definition);
            }
        }
    }
}
