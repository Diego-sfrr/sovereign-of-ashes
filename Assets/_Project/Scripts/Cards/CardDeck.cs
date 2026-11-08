using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Baralho do jogador: pilha de compra, mão e descarte.
    /// Quando a pilha de compra acaba, o descarte é embaralhado e vira a nova pilha.
    /// </summary>
    public sealed class CardDeck
    {
        private readonly List<ICardDefinition> _drawPile = new List<ICardDefinition>();
        private readonly List<ICardDefinition> _hand = new List<ICardDefinition>();
        private readonly List<ICardDefinition> _discardPile = new List<ICardDefinition>();
        private readonly ReadOnlyCollection<ICardDefinition> _readOnlyHand;
        private readonly Random _random;

        /// <summary>Disparado sempre que alguma pilha muda.</summary>
        public event Action<CardDeck> Changed;

        /// <summary>Tamanho da mão ao comprar no início do turno.</summary>
        public int HandSize { get; }

        /// <summary>Cartas na mão, na ordem em que foram compradas.</summary>
        public IReadOnlyList<ICardDefinition> Hand => _readOnlyHand;

        /// <summary>Cartas restantes na pilha de compra.</summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>Cartas no descarte.</summary>
        public int DiscardPileCount => _discardPile.Count;

        /// <summary>Total de cartas do baralho (todas as pilhas).</summary>
        public int TotalCount => _drawPile.Count + _hand.Count + _discardPile.Count;

        /// <summary>
        /// Cria o baralho e embaralha a pilha de compra.
        /// </summary>
        /// <param name="cards">Cartas iniciais (nulas são ignoradas).</param>
        /// <param name="handSize">Tamanho da mão (maior que zero).</param>
        /// <param name="random">Gerador do embaralhamento (injete com semente fixa para partidas reproduzíveis).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="cards"/> ou <paramref name="random"/> forem nulos.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="handSize"/> for menor ou igual a zero.</exception>
        public CardDeck(IEnumerable<ICardDefinition> cards, int handSize, Random random)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (handSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(handSize), handSize, "Deve ser maior que zero.");
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            HandSize = handSize;
            _readOnlyHand = _hand.AsReadOnly();

            foreach (ICardDefinition card in cards)
            {
                if (card != null)
                {
                    _drawPile.Add(card);
                }
            }

            Shuffle(_drawPile);
        }

        /// <summary>
        /// Compra cartas até a mão ter <see cref="HandSize"/> cartas (ou o baralho acabar).
        /// </summary>
        /// <returns>Quantas cartas foram compradas.</returns>
        public int DrawUpToHandSize()
        {
            int drawn = 0;

            while (_hand.Count < HandSize)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        break;
                    }

                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }

                int top = _drawPile.Count - 1;
                _hand.Add(_drawPile[top]);
                _drawPile.RemoveAt(top);
                drawn++;
            }

            if (drawn > 0)
            {
                Changed?.Invoke(this);
            }

            return drawn;
        }

        /// <summary>
        /// Move a carta da mão para o descarte (após ser jogada).
        /// </summary>
        /// <param name="handIndex">Índice na mão.</param>
        /// <returns>A carta removida.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se o índice estiver fora da mão.</exception>
        public ICardDefinition DiscardFromHand(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handIndex), handIndex, $"A mão tem {_hand.Count} cartas.");
            }

            ICardDefinition card = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            _discardPile.Add(card);
            Changed?.Invoke(this);
            return card;
        }

        /// <summary>
        /// Descarta a mão inteira (fim do turno).
        /// </summary>
        public void DiscardHand()
        {
            if (_hand.Count == 0)
            {
                return;
            }

            _discardPile.AddRange(_hand);
            _hand.Clear();
            Changed?.Invoke(this);
        }

        private void Shuffle(List<ICardDefinition> cards)
        {
            // Fisher-Yates.
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                ICardDefinition swap = cards[i];
                cards[i] = cards[j];
                cards[j] = swap;
            }
        }
    }
}
