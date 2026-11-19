using System;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Alchemy.Effects;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Core;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Transforma os eventos dos sistemas em frases da crônica (Observer).
    /// Mantém a narração fora das regras e fora da interface.
    /// </summary>
    public sealed class GameNarrator : IDisposable
    {
        private readonly GameSession _session;
        private readonly GameLog _log;
        private readonly System.Collections.Generic.HashSet<string> _barked = new System.Collections.Generic.HashSet<string>();
        private bool _disposed;

        /// <summary>
        /// Cria o narrador e assina os eventos da partida.
        /// </summary>
        /// <param name="session">Partida.</param>
        /// <param name="log">Crônica que recebe as frases.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public GameNarrator(GameSession session, GameLog log)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            Subscribe();
        }

        /// <summary>Cancela as assinaturas.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Unsubscribe();
            _disposed = true;
        }

        private int Turn => _session.Turns.TurnNumber;

        private void Write(string text, LogTone tone) => _log.Add(text, tone, Turn);

        private void Subscribe()
        {
            _session.Turns.TurnStarted += OnTurnStarted;
            _session.Turns.GameOver += OnGameOver;
            _session.IncomePhase.IncomeCollected += OnIncome;
            _session.Board.RingRotated += OnRingRotated;
            _session.Alchemy.ReactionResolved += OnReaction;
            _session.Cards.CardPlayed += OnCardPlayed;
            _session.CivilizationPhase.CivilizationAttacked += OnCivilizationAttacked;
            _session.CivilizationPhase.InvaderStruck += OnInvaderStruck;
            _session.AdvancePhase.InvaderAttackedCore += OnInvaderAttackedCore;
            _session.SpawnPhase.SpawnOverflow += OnSpawnOverflow;
            _session.TraitsPhase.ElementCorrupted += OnElementCorrupted;
            _session.RingControl.Used += OnRingControlUsed;
            _session.TraitsPhase.RationalityPushed += OnRationalityPushed;
            _session.TraitsPhase.RingHealed += OnRingHealed;
            _session.TraitsPhase.MiddleRingTurned += OnMiddleRingTurned;
            _session.DeathTraits.Exploded += OnExploded;
            _session.Invaders.InvaderSpawned += OnInvaderSpawned;
            _session.Invaders.InvaderSlain += OnInvaderSlain;
            _session.Bounty.BountyGranted += OnBounty;
            _session.Rules.CommandmentTriggered += OnCommandment;
            _session.ElementEffects.EffectTriggered += OnElementEffect;

            foreach (CivilizationEntity civilization in _session.Civilizations)
            {
                civilization.StateChanged += OnCivilizationStateChanged;
            }
        }

        private void Unsubscribe()
        {
            _session.Turns.TurnStarted -= OnTurnStarted;
            _session.Turns.GameOver -= OnGameOver;
            _session.IncomePhase.IncomeCollected -= OnIncome;
            _session.Board.RingRotated -= OnRingRotated;
            _session.Alchemy.ReactionResolved -= OnReaction;
            _session.Cards.CardPlayed -= OnCardPlayed;
            _session.CivilizationPhase.CivilizationAttacked -= OnCivilizationAttacked;
            _session.CivilizationPhase.InvaderStruck -= OnInvaderStruck;
            _session.AdvancePhase.InvaderAttackedCore -= OnInvaderAttackedCore;
            _session.SpawnPhase.SpawnOverflow -= OnSpawnOverflow;
            _session.TraitsPhase.ElementCorrupted -= OnElementCorrupted;
            _session.RingControl.Used -= OnRingControlUsed;
            _session.TraitsPhase.RationalityPushed -= OnRationalityPushed;
            _session.TraitsPhase.RingHealed -= OnRingHealed;
            _session.TraitsPhase.MiddleRingTurned -= OnMiddleRingTurned;
            _session.DeathTraits.Exploded -= OnExploded;
            _session.Invaders.InvaderSpawned -= OnInvaderSpawned;
            _session.Invaders.InvaderSlain -= OnInvaderSlain;
            _session.Bounty.BountyGranted -= OnBounty;
            _session.Rules.CommandmentTriggered -= OnCommandment;
            _session.ElementEffects.EffectTriggered -= OnElementEffect;

            foreach (CivilizationEntity civilization in _session.Civilizations)
            {
                civilization.StateChanged -= OnCivilizationStateChanged;
            }
        }

        private void OnTurnStarted(int turn)
        {
            _log.Add($"— Turno {turn} —", LogTone.Highlight, turn);

            string ending = Story.ActEnding(turn - 1);

            if (ending.Length > 0)
            {
                _log.Add(ending, LogTone.Highlight, turn);
            }

            string chronicle = Story.ChronicleForTurn(turn);

            if (chronicle.Length > 0)
            {
                _log.Add(chronicle, LogTone.Info, turn);
            }

            if (_session.TurnsToSurvive == 0 && turn >= 30 && turn % 10 == 0)
            {
                _log.Add(Story.EndlessDeep, LogTone.Highlight, turn);
            }
        }

        private void OnGameOver(GameOverInfo info)
        {
            string title = info.Outcome == GameOutcome.Victory ? "VITÓRIA" : "DERROTA";
            Write($"{title}: {info.Reason}", LogTone.Highlight);
        }

        private void OnIncome(FaithIncomeReport report)
        {
            if (report.Deposited > 0)
            {
                Write($"+{report.Deposited} de Fé (renda do turno).", LogTone.Good);
            }

            if (report.Overflow > 0)
            {
                Write($"{report.Overflow} de Fé se perdeu: a reserva está cheia.", LogTone.Bad);
            }
        }

        private void OnRingRotated(RingType ring, int delta) =>
            Write($"{DisplayNames.Ring(ring)} girou {DisplayNames.Rotation(delta)}.", LogTone.Info);

        private void OnReaction(AlchemyResult result)
        {
            string concept = DisplayNames.Concept(result.Concept);

            switch (result.Outcome)
            {
                case AlchemyOutcome.Manifested:
                    Write($"{result.Current.Definition.DisplayName} se manifestou ({DisplayNames.Ring(result.Sector.Ring)}).", LogTone.Info);
                    break;
                case AlchemyOutcome.Transmuted:
                    Write($"Fusão! {result.Previous.Definition.DisplayName} + {concept} = {result.Current.Definition.DisplayName}.", LogTone.Good);
                    break;
                case AlchemyOutcome.NoReaction:
                    Write($"{concept} não reagiu com {result.Previous.Definition.DisplayName}.", LogTone.Info);
                    break;
                case AlchemyOutcome.MissingBaseElement:
                    Write($"O conceito {concept} não tem forma neste mundo.", LogTone.Info);
                    break;
            }
        }

        private void OnCardPlayed(ICardDefinition card, TectonicSector target) =>
            Write($"Você invocou {card.DisplayName}.", LogTone.Info);

        private void OnCivilizationAttacked(CivilizationEntity civilization, AttackTarget target, int power)
        {
            if (target == AttackTarget.DivineCore && power > 0)
            {
                Write($"{civilization.Name}, herege, atacou o Núcleo com {power} de poder!", LogTone.Bad);
            }
        }

        private void OnInvaderStruck(CivilizationEntity civilization, InvaderEntity invader) =>
            Write($"{civilization.Name} combateu {invader.Name} ({invader.CurrentHealth}/{invader.MaxHealth}).", LogTone.Good);

        private void OnInvaderAttackedCore(InvaderEntity invader, int damage) =>
            Write($"{invader.Name} atacou o Núcleo ({damage} de dano).", LogTone.Bad);

        private void OnRingControlUsed(RingActionKind kind, RingType ring, RotationDirection direction)
        {
            if (kind == RingActionKind.Freeze)
            {
                Write($"Você congelou o anel {RingName(ring)}: ele ignora a próxima rotação.", LogTone.Highlight);
            }
            else
            {
                Write($"Você girou o anel {RingName(ring)} por conta própria.", LogTone.Highlight);
            }
        }

        private static string RingName(RingType ring)
        {
            switch (ring)
            {
                case RingType.OuterWilderness: return "dos Ermos Externos";
                case RingType.MiddleCivilization: return "da Civilização";
                default: return "do Santuário Interno";
            }
        }

        private void OnElementCorrupted(InvaderEntity invader, TectonicSector sector, ISectorContent destroyed) =>
            Write($"{invader.Name} profanou e destruiu {destroyed?.Definition?.DisplayName ?? "um elemento"}.", LogTone.Bad);

        private void OnRationalityPushed(InvaderEntity invader, int amount) =>
            Write($"{invader.Name} semeia dúvida: +{amount} de Racionalidade nas civilizações.", LogTone.Bad);

        private void OnRingHealed(InvaderEntity healer, int count, int amount) =>
            Write($"{healer.Name} curou {count} invasor(es) do anel (+{amount} de vida cada).", LogTone.Bad);

        private void OnMiddleRingTurned(InvaderEntity invader) =>
            Write($"{invader.Name} girou o anel da Civilização: o tabuleiro se embaralhou.", LogTone.Bad);

        private void OnExploded(InvaderEntity invader, int victims, bool destroyedElement)
        {
            string hit = victims > 0 ? $" e feriu {victims} invasor(es) vizinho(s)" : string.Empty;
            string element = destroyedElement ? " Um elemento foi destruído no setor." : string.Empty;
            Write($"{invader.Name} explodiu{hit}.{element}", LogTone.Highlight);
        }

        private void OnSpawnOverflow(IInvaderDefinition definition, int missing) =>
            Write($"Os Ermos estão lotados: {missing}x {definition.DisplayName} não surgiram.", LogTone.Info);

        private void OnInvaderSpawned(InvaderEntity invader)
        {
            Write($"{invader.Name} surgiu nos Ermos Externos.", LogTone.Bad);

            // A primeira vez que cada tipo surge, ele fala.
            string bark = Story.InvaderBark(invader.Definition.Id);

            if (bark.Length > 0 && _barked.Add(invader.Definition.Id))
            {
                Write($"{invader.Name}: {bark}", LogTone.Bad);

                if (invader.Definition.Id == "inquisitor")
                {
                    Write("\"" + Story.InquisitorVerdict + "\"", LogTone.Highlight);
                }
            }
        }

        private void OnInvaderSlain(InvaderEntity invader)
        {
            Write($"{invader.Name} foi destruído.", LogTone.Good);

            if (invader.Definition.Id == "inquisitor")
            {
                Write($"{invader.Name}: \"" + Story.InquisitorDies + "\"", LogTone.Highlight);
            }
            else
            {
                string last = Story.BossLastWords(invader.Definition.Id);

                if (last.Length > 0)
                {
                    Write($"{invader.Name}: {last}", LogTone.Highlight);
                }
            }
        }

        private void OnBounty(InvaderEntity invader, FaithIncomeReport report)
        {
            if (report.Deposited > 0)
            {
                Write($"+{report.Deposited} de Fé (recompensa).", LogTone.Good);
            }
        }

        private void OnElementEffect(ElementEffectReport report) =>
            Write(report.Text, report.IsBeneficial ? LogTone.Good : LogTone.Bad);

        private void OnCommandment(ICommandment commandment, RuleContext context) =>
            Write($"Mandamento \"{commandment.DisplayName}\" alterou {context.Channel}: {context.BaseAmount} -> {context.Amount}.", LogTone.Info);

        private void OnCivilizationStateChanged(CivilizationEntity civilization, IFactionState previous, IFactionState current)
        {
            if (current.Allegiance == FactionAllegiance.Heretic)
            {
                Write($"HERESIA! {civilization.Name} rejeitou o Trono e marchará contra o Núcleo.", LogTone.Bad);
            }
            else
            {
                Write($"{civilization.Name} voltou à fé.", LogTone.Good);
            }
        }
    }
}
