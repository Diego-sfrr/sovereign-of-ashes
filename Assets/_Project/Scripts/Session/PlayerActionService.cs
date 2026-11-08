using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Tudo o que o jogador pode fazer, num só lugar (Facade para a interface):
    /// pré-visualizar e jogar cartas da mão e encerrar o turno. Respeita a vez do jogador.
    /// </summary>
    public sealed class PlayerActionService
    {
        private readonly TurnSystem _turns;
        private readonly CardDeck _deck;
        private readonly CardResolver _cards;

        /// <summary>Disparado após cada tentativa de jogar carta. Parâmetros: carta, alvo, desfecho.</summary>
        public event Action<ICardDefinition, TectonicSector, CardPlayOutcome> CardAttempted;

        /// <summary>Indica se o jogador pode agir agora.</summary>
        public bool CanAct => _turns.IsPlayerTurn;

        /// <summary>
        /// Cria o serviço.
        /// </summary>
        /// <param name="turns">Ciclo de turnos.</param>
        /// <param name="deck">Baralho.</param>
        /// <param name="cards">Resolvedor de cartas.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public PlayerActionService(TurnSystem turns, CardDeck deck, CardResolver cards)
        {
            _turns = turns ?? throw new ArgumentNullException(nameof(turns));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        /// <summary>
        /// Verifica se a carta da mão pode ser jogada no alvo, sem executar nada.
        /// </summary>
        /// <param name="handIndex">Índice na mão.</param>
        /// <param name="target">Setor alvo (nulo para cartas globais).</param>
        /// <returns>O desfecho previsto.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se o índice estiver fora da mão.</exception>
        public CardPlayOutcome Preview(int handIndex, TectonicSector target)
        {
            ICardDefinition card = GetHandCard(handIndex);
            return CanAct ? _cards.CanPlay(card, target) : CardPlayOutcome.NotPlayerTurn;
        }

        /// <summary>
        /// Joga a carta da mão; se der certo, ela vai para o descarte.
        /// </summary>
        /// <param name="handIndex">Índice na mão.</param>
        /// <param name="target">Setor alvo (nulo para cartas globais).</param>
        /// <returns>O desfecho.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se o índice estiver fora da mão.</exception>
        public CardPlayOutcome PlayFromHand(int handIndex, TectonicSector target)
        {
            ICardDefinition card = GetHandCard(handIndex);

            CardPlayOutcome outcome = CanAct ? _cards.Play(card, target) : CardPlayOutcome.NotPlayerTurn;

            if (outcome == CardPlayOutcome.Played)
            {
                _deck.DiscardFromHand(handIndex);
            }

            CardAttempted?.Invoke(card, target, outcome);
            return outcome;
        }

        /// <summary>
        /// Encerra a vez do jogador.
        /// </summary>
        /// <returns><c>false</c> se não era a vez do jogador.</returns>
        public bool EndTurn()
        {
            if (!CanAct)
            {
                return false;
            }

            _turns.EndPlayerTurn();
            return true;
        }

        /// <summary>
        /// Texto curto explicando um desfecho, para a interface.
        /// </summary>
        /// <param name="outcome">Desfecho.</param>
        /// <returns>Mensagem em português.</returns>
        public static string Describe(CardPlayOutcome outcome)
        {
            switch (outcome)
            {
                case CardPlayOutcome.Played: return "Carta jogada.";
                case CardPlayOutcome.InvalidRing: return "Essa carta não pode ser jogada nesse anel.";
                case CardPlayOutcome.SectorOccupied: return "Essa carta exige um setor vazio.";
                case CardPlayOutcome.InsufficientFaith: return "Fé insuficiente.";
                case CardPlayOutcome.NoInvaderOnTarget: return "Escolha um setor com invasor.";
                case CardPlayOutcome.TargetRequired: return "Escolha um setor alvo.";
                case CardPlayOutcome.NotPlayerTurn: return "Não é a sua vez.";
                default: return outcome.ToString();
            }
        }

        private ICardDefinition GetHandCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _deck.Hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handIndex), handIndex, $"A mão tem {_deck.Hand.Count} cartas.");
            }

            return _deck.Hand[handIndex];
        }
    }
}
