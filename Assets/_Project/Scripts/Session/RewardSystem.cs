using System;
using System.Collections.Generic;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Uma Oferenda Divina: três cartas das quais o jogador pode levar uma para o baralho.
    /// </summary>
    public sealed class RewardOffer
    {
        /// <summary>Turno em que a oferenda surgiu.</summary>
        public int Turn { get; }

        /// <summary>As cartas oferecidas.</summary>
        public IReadOnlyList<ICardDefinition> Choices { get; }

        internal RewardOffer(int turn, IReadOnlyList<ICardDefinition> choices)
        {
            Turn = turn;
            Choices = choices;
        }
    }

    /// <summary>
    /// Constrói o baralho durante a partida: a cada N turnos, no começo da vez do jogador, surge uma
    /// <see cref="RewardOffer"/>. Escolher uma carta a coloca no baralho; recusar dá um pouco de Fé.
    /// Determinístico para uma semente. Sem cartas no "pool" ou com intervalo 0, o sistema fica desligado.
    /// </summary>
    public sealed class RewardSystem : IDisposable
    {
        private readonly CardDeck _deck;
        private readonly FaithPool _faith;
        private readonly TurnSystem _turns;
        private readonly List<ICardDefinition> _pool = new List<ICardDefinition>();
        private readonly Random _random;
        private readonly int _everyNTurns;
        private readonly int _skipFaith;
        private bool _disposed;

        /// <summary>Oferenda esperando decisão (<c>null</c> se não há).</summary>
        public RewardOffer Pending { get; private set; }

        /// <summary>Se há uma oferenda esperando decisão.</summary>
        public bool HasPending => Pending != null;

        /// <summary>Quantas oferendas já foram resolvidas (escolhidas ou recusadas).</summary>
        public int ResolvedCount { get; private set; }

        /// <summary>Quantas cartas já foram levadas para o baralho.</summary>
        public int PickedCount { get; private set; }

        /// <summary>Disparado quando uma oferenda surge.</summary>
        public event Action<RewardOffer> OfferCreated;

        /// <summary>Disparado quando a oferenda é resolvida. Parâmetro: a carta escolhida (<c>null</c> se recusada).</summary>
        public event Action<ICardDefinition> OfferResolved;

        /// <summary>
        /// Cria o sistema.
        /// </summary>
        /// <param name="deck">Baralho que recebe as cartas.</param>
        /// <param name="faith">Reserva de Fé (recebe a recompensa de recusar).</param>
        /// <param name="turns">Sistema de turnos (a oferenda surge ao começar a vez do jogador).</param>
        /// <param name="pool">Cartas possíveis (cartas repetidas no pool aumentam a chance).</param>
        /// <param name="random">Sorteio (use semente fixa para partidas reproduzíveis).</param>
        /// <param name="everyNTurns">Intervalo em turnos (0 desliga).</param>
        /// <param name="skipFaith">Fé ganha ao recusar.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento obrigatório for nulo.</exception>
        public RewardSystem(CardDeck deck, FaithPool faith, TurnSystem turns, IEnumerable<ICardDefinition> pool,
            Random random, int everyNTurns, int skipFaith = 1)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _faith = faith ?? throw new ArgumentNullException(nameof(faith));
            _turns = turns ?? throw new ArgumentNullException(nameof(turns));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _everyNTurns = Math.Max(0, everyNTurns);
            _skipFaith = Math.Max(0, skipFaith);

            if (pool != null)
            {
                foreach (ICardDefinition card in pool)
                {
                    if (card != null)
                    {
                        _pool.Add(card);
                    }
                }
            }

            if (IsEnabled)
            {
                _turns.AwaitingPlayer += OnAwaitingPlayer;
                _turns.TurnEnded += OnTurnEnded;
            }
        }

        /// <summary>Se o sistema está ligado (há intervalo e cartas no pool).</summary>
        public bool IsEnabled => _everyNTurns > 0 && _pool.Count > 0;

        /// <summary>Fé ganha ao recusar uma oferenda.</summary>
        public int SkipFaith => _skipFaith;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _turns.AwaitingPlayer -= OnAwaitingPlayer;
            _turns.TurnEnded -= OnTurnEnded;
        }

        /// <summary>
        /// Leva a carta escolhida para o baralho.
        /// </summary>
        /// <param name="index">Posição da carta em <see cref="RewardOffer.Choices"/>.</param>
        /// <exception cref="InvalidOperationException">Se não houver oferenda pendente.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se o índice for inválido.</exception>
        public void Choose(int index)
        {
            RewardOffer offer = RequirePending();

            if (index < 0 || index >= offer.Choices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Escolha inexistente.");
            }

            ICardDefinition card = offer.Choices[index];
            _deck.AddCard(card);
            PickedCount++;
            Resolve(card);
        }

        /// <summary>
        /// Recusa a oferenda e recebe um pouco de Fé.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se não houver oferenda pendente.</exception>
        public void Skip()
        {
            RequirePending();

            if (_skipFaith > 0)
            {
                _faith.Add(_skipFaith);
            }

            Resolve(null);
        }

        private RewardOffer RequirePending() =>
            Pending ?? throw new InvalidOperationException("Não há oferenda pendente.");

        private void Resolve(ICardDefinition chosen)
        {
            Pending = null;
            ResolvedCount++;
            OfferResolved?.Invoke(chosen);
        }

        // Encerrar o turno sem decidir conta como recusar (vale também para jogadores automáticos).
        private void OnTurnEnded(int turn)
        {
            if (Pending != null)
            {
                Skip();
            }
        }

        private void OnAwaitingPlayer(int turn)
        {
            if (Pending != null || turn < _everyNTurns || turn % _everyNTurns != 0)
            {
                return;
            }

            // Escolhe até 3 cartas de tipos diferentes (por id), sorteando sem repetição.
            var candidates = new List<ICardDefinition>(_pool);
            var picks = new List<ICardDefinition>();
            var usedIds = new HashSet<string>();

            while (picks.Count < 3 && candidates.Count > 0)
            {
                int i = _random.Next(candidates.Count);
                ICardDefinition card = candidates[i];
                candidates.RemoveAt(i);

                if (usedIds.Add(card.Id))
                {
                    picks.Add(card);
                }
            }

            Pending = new RewardOffer(turn, picks);
            OfferCreated?.Invoke(Pending);
        }
    }
}
