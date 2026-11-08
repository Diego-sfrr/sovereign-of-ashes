using System;
using SovereignOfAshes.Cards;

namespace SovereignOfAshes.Turns.Phases
{
    /// <summary>
    /// Fase de Compra: descarta a mão do turno anterior e compra uma mão nova.
    /// </summary>
    public sealed class DrawPhase : ITurnPhase
    {
        private readonly CardDeck _deck;

        /// <inheritdoc />
        public string Name => "Compra";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="deck">Baralho do jogador.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="deck"/> for nulo.</exception>
        public DrawPhase(CardDeck deck)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            _deck.DiscardHand();
            _deck.DrawUpToHandSize();
            return PhaseResult.Continue;
        }
    }
}
