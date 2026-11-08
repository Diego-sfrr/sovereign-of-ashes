using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Economy;

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
        NoInvaderOnTarget = 4,

        /// <summary>A carta precisa de um setor alvo e nenhum foi informado.</summary>
        TargetRequired = 5,

        /// <summary>Não é a vez do jogador (usado pela camada de ações do jogador).</summary>
        NotPlayerTurn = 6
    }

    /// <summary>
    /// Valida o alvo, cobra o custo em Fé e executa os efeitos de uma carta (POCO, sem Unity).
    /// O custo só é cobrado depois que todas as validações passam.
    /// Cartas globais (<see cref="TargetRings.None"/>) não precisam de alvo.
    /// </summary>
    public sealed class CardResolver
    {
        private readonly CardPlayServices _services;
        private readonly IFaithWallet _wallet;

        /// <summary>
        /// Disparado após uma carta ser jogada com sucesso. Parâmetros: carta, setor alvo (nulo em cartas globais).
        /// </summary>
        public event Action<ICardDefinition, TectonicSector> CardPlayed;

        /// <summary>
        /// Cria o resolvedor.
        /// </summary>
        /// <param name="services">Serviços da partida usados pelos efeitos.</param>
        /// <param name="wallet">Carteira de Fé que paga o custo das cartas.</param>
        /// <exception cref="ArgumentNullException">Se alguma dependência for nula.</exception>
        public CardResolver(CardPlayServices services, IFaithWallet wallet)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <summary>
        /// Verifica se a carta pode ser jogada, sem executar nada (útil para destacar alvos na UI).
        /// </summary>
        /// <param name="card">Carta.</param>
        /// <param name="target">Setor alvo (ignorado em cartas globais; pode ser nulo nelas).</param>
        /// <returns><see cref="CardPlayOutcome.Played"/> se for válido, ou o motivo da recusa.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="card"/> for nula.</exception>
        public CardPlayOutcome CanPlay(ICardDefinition card, TectonicSector target)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!card.IsGlobal())
            {
                if (target == null)
                {
                    return CardPlayOutcome.TargetRequired;
                }

                if (!card.AllowedRings.Includes(target.Ring))
                {
                    return CardPlayOutcome.InvalidRing;
                }

                if (card.RequiresEmptySector && !target.IsEmpty)
                {
                    return CardPlayOutcome.SectorOccupied;
                }

                if (card.RequiresInvader && !_services.Invaders.IsOccupied(target))
                {
                    return CardPlayOutcome.NoInvaderOnTarget;
                }
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
        /// <param name="target">Setor alvo (deve pertencer ao tabuleiro); ignorado em cartas globais.</param>
        /// <returns>O desfecho; se não for <see cref="CardPlayOutcome.Played"/>, nada foi executado nem cobrado.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="card"/> for nula.</exception>
        /// <exception cref="ArgumentException">Se o setor não pertencer ao tabuleiro.</exception>
        public CardPlayOutcome Play(ICardDefinition card, TectonicSector target)
        {
            CardPlayOutcome validation = CanPlay(card, target);

            if (validation != CardPlayOutcome.Played)
            {
                return validation;
            }

            TectonicSector effectiveTarget = card.IsGlobal() ? null : target;

            if (effectiveTarget != null && !_services.Board.GetRing(effectiveTarget.Ring).Contains(effectiveTarget))
            {
                throw new ArgumentException($"O setor {effectiveTarget} não pertence a este tabuleiro.", nameof(target));
            }

            if (!_wallet.TrySpend(Math.Max(0, card.Cost)))
            {
                return CardPlayOutcome.InsufficientFaith;
            }

            var context = new TileEffectContext(_services, effectiveTarget, card);

            foreach (ITileEffect effect in card.Effects)
            {
                effect?.Apply(context);
            }

            CardPlayed?.Invoke(card, effectiveTarget);
            return CardPlayOutcome.Played;
        }
    }
}
