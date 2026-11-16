using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Economy;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// O tipo de intervenção divina sobre um anel.
    /// </summary>
    public enum RingActionKind
    {
        /// <summary>Gira o anel uma casa agora.</summary>
        Rotate = 0,

        /// <summary>O anel ignora a próxima rotação programada.</summary>
        Freeze = 1
    }

    /// <summary>
    /// Intervenção Divina: o jogador gasta Fé para girar um anel uma casa ou congelá-lo (ele ignora a próxima
    /// rotação programada). Só uma intervenção por turno, só na vez do jogador e sem Oferenda pendente.
    /// A previsão de marcha passa por <see cref="FilterPlanned"/>, então as setas já mostram o efeito do congelamento.
    /// </summary>
    public sealed class RingControlService
    {
        private readonly TectonicBoard _board;
        private readonly FaithPool _faith;
        private readonly Func<bool> _canAct;
        private readonly HashSet<RingType> _frozen = new HashSet<RingType>();

        /// <summary>Fé que custa girar um anel (algumas civilizações reduzem).</summary>
        public int RotateCost { get; set; } = 1;

        /// <summary>Fé que custa congelar um anel.</summary>
        public int FreezeCost { get; set; } = 2;

        /// <summary>Se a intervenção do turno já foi usada.</summary>
        public bool UsedThisTurn { get; private set; }

        /// <summary>Quantas intervenções já foram feitas na partida.</summary>
        public int TotalUsed { get; private set; }

        /// <summary>Disparado a cada intervenção. Parâmetros: ação, anel, sentido (só vale para <see cref="RingActionKind.Rotate"/>).</summary>
        public event Action<RingActionKind, RingType, RotationDirection> Used;

        /// <summary>
        /// Cria o serviço.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="faith">Reserva de Fé do jogador.</param>
        /// <param name="canAct">Devolve se o jogador pode agir agora (vez dele e sem oferenda pendente).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public RingControlService(TectonicBoard board, FaithPool faith, Func<bool> canAct)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _faith = faith ?? throw new ArgumentNullException(nameof(faith));
            _canAct = canAct ?? throw new ArgumentNullException(nameof(canAct));
        }

        /// <summary>Se o anel está congelado (vai ignorar a próxima rotação programada).</summary>
        /// <param name="ring">Anel.</param>
        /// <returns>Verdadeiro se congelado.</returns>
        public bool IsFrozen(RingType ring) => _frozen.Contains(ring);

        /// <summary>Se alguma intervenção é possível agora (vez do jogador, ainda não usada).</summary>
        public bool CanUseNow => !UsedThisTurn && _canAct();

        /// <summary>Se dá para girar o anel agora (inclui ter Fé).</summary>
        /// <returns>Verdadeiro se possível.</returns>
        public bool CanRotate() => CanUseNow && _faith.CanAfford(RotateCost);

        /// <summary>Se dá para congelar o anel agora (inclui ter Fé e o anel ainda não estar congelado).</summary>
        /// <param name="ring">Anel.</param>
        /// <returns>Verdadeiro se possível.</returns>
        public bool CanFreeze(RingType ring) => CanUseNow && !IsFrozen(ring) && _faith.CanAfford(FreezeCost);

        /// <summary>
        /// Gira o anel uma casa.
        /// </summary>
        /// <param name="ring">Anel.</param>
        /// <param name="direction">Sentido.</param>
        /// <returns><c>false</c> se não foi possível (nada é gasto).</returns>
        public bool TryRotate(RingType ring, RotationDirection direction)
        {
            if (!CanRotate() || !_faith.TrySpend(RotateCost))
            {
                return false;
            }

            UsedThisTurn = true;
            TotalUsed++;
            _board.RotateRing(ring, 1, direction);
            Used?.Invoke(RingActionKind.Rotate, ring, direction);
            return true;
        }

        /// <summary>
        /// Congela o anel: ele ignora a próxima rotação programada.
        /// </summary>
        /// <param name="ring">Anel.</param>
        /// <returns><c>false</c> se não foi possível (nada é gasto).</returns>
        public bool TryFreeze(RingType ring)
        {
            if (!CanFreeze(ring) || !_faith.TrySpend(FreezeCost))
            {
                return false;
            }

            UsedThisTurn = true;
            TotalUsed++;
            _frozen.Add(ring);
            Used?.Invoke(RingActionKind.Freeze, ring, RotationDirection.Clockwise);
            return true;
        }

        /// <summary>
        /// Remove das rotações programadas as dos anéis congelados.
        /// </summary>
        /// <param name="planned">Rotações programadas.</param>
        /// <returns>As rotações que realmente vão acontecer.</returns>
        public IReadOnlyList<RingRotation> FilterPlanned(IReadOnlyList<RingRotation> planned)
        {
            if (planned == null)
            {
                throw new ArgumentNullException(nameof(planned));
            }

            if (_frozen.Count == 0)
            {
                return planned;
            }

            var kept = new List<RingRotation>();

            foreach (RingRotation rotation in planned)
            {
                if (!_frozen.Contains(rotation.Ring))
                {
                    kept.Add(rotation);
                }
            }

            return kept;
        }

        /// <summary>Descongela todos os anéis (a fase tectônica chama isto depois de aplicar as rotações).</summary>
        public void ClearFrozen() => _frozen.Clear();

        /// <summary>Libera a intervenção para o novo turno do jogador.</summary>
        public void BeginTurn() => UsedThisTurn = false;
    }
}
