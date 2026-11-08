using System;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Resultado da tentativa de jogar uma carta.
    /// </summary>
    public enum CardPlayOutcome
    {
        /// <summary>A carta foi jogada e todos os efeitos executaram.</summary>
        Played = 0,

        /// <summary>O anel do setor não é um alvo permitido.</summary>
        InvalidRing = 1,

        /// <summary>A carta exige setor vazio e o setor está ocupado.</summary>
        SectorOccupied = 2,

        /// <summary>Não há Fé suficiente para pagar o custo.</summary>
        InsufficientFaith = 3,

        /// <summary>A carta exige um invasor no setor e não há nenhum.</summary>
        NoInvaderOnTarget = 4
    }

    /// <summary>
    /// Valida o alvo, cobra o custo em Fé e executa os efeitos de uma carta (POCO, sem Unity).
    /// O custo só é cobrado depois que todas as validações passam.
    /// </summary>
    public sealed class CardResolver
    {
        private readonly TectonicBoard _board;
        private readonly AlchemySystem _alchemy;
        private readonly IFaithWallet _wallet;
        private readonly InvaderRoster _invaders;
        private readonly CombatResolver _combat;

        /// <summary>
        /// Disparado após uma carta ser jogada com sucesso. Parâmetros: carta, setor alvo.
        /// </summary>
        public event Action<ICardDefinition, TectonicSector> CardPlayed;

        /// <summary>
        /// Cria o resolvedor.
        /// </summary>
        /// <param name="board">Tabuleiro da partida.</param>
        /// <param name="alchemy">Sistema de alquimia.</param>
        /// <param name="wallet">Carteira de Fé que paga o custo das cartas.</param>
        /// <param name="invaders">Registro de invasores (alvos das cartas de ataque).</param>
        /// <param name="combat">Resolvedor de combate.</param>
        /// <exception cref="ArgumentNullException">Se alguma dependência for nula.</exception>
        public CardResolver(
            TectonicBoard board,
            AlchemySystem alchemy,
            IFaithWallet wallet,
            InvaderRoster invaders,
            CombatResolver combat)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _alchemy = alchemy ?? throw new ArgumentNullException(nameof(alchemy));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _invaders = invaders ?? throw new ArgumentNullException(nameof(invaders));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        /// <summary>
        /// Verifica se a carta pode ser jogada no setor, sem executar nada (útil para destacar alvos na UI).
        /// </summary>
        /// <param name="card">Carta.</param>
        /// <param name="target">Setor alvo.</param>
        /// <returns><see cref="CardPlayOutcome.Played"/> se for válido, ou o motivo da recusa.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public CardPlayOutcome CanPlay(ICardDefinition card, TectonicSector target)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!card.AllowedRings.Includes(target.Ring))
            {
                return CardPlayOutcome.InvalidRing;
            }

            if (card.RequiresEmptySector && !target.IsEmpty)
            {
                return CardPlayOutcome.SectorOccupied;
            }

            if (card.RequiresInvader && !_invaders.IsOccupied(target))
            {
                return CardPlayOutcome.NoInvaderOnTarget;
            }

            if (!_wallet.CanAfford(Math.Max(0, card.Cost)))
            {
                return CardPlayOutcome.InsufficientFaith;
            }

            return CardPlayOutcome.Played;
        }

        /// <summary>
        /// Joga a carta: valida o alvo, cobra a Fé e executa os efeitos em ordem.
        /// </summary>
        /// <param name="card">Carta.</param>
        /// <param name="target">Setor alvo (deve pertencer ao tabuleiro deste resolvedor).</param>
        /// <returns>O desfecho; se não for <see cref="CardPlayOutcome.Played"/>, nada foi executado nem cobrado.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentException">Se o setor não pertencer ao tabuleiro.</exception>
        public CardPlayOutcome Play(ICardDefinition card, TectonicSector target)
        {
            CardPlayOutcome validation = CanPlay(card, target);

            if (validation != CardPlayOutcome.Played)
            {
                return validation;
            }

            if (!_board.GetRing(target.Ring).Contains(target))
            {
                throw new ArgumentException($"O setor {target} não pertence a este tabuleiro.", nameof(target));
            }

            if (!_wallet.TrySpend(Math.Max(0, card.Cost)))
            {
                return CardPlayOutcome.InsufficientFaith;
            }

            var context = new TileEffectContext(_board, target, _alchemy, _invaders, _combat, card);

            foreach (ITileEffect effect in card.Effects)
            {
                effect?.Apply(context);
            }

            CardPlayed?.Invoke(card, target);
            return CardPlayOutcome.Played;
        }
    }
}
