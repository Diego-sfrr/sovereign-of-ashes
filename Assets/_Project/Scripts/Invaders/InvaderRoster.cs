using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SovereignOfAshes.Board;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Visão mínima dos invasores para quem só precisa atacá-los (ex.: a fase das civilizações).
    /// </summary>
    public interface IInvaderTargeting
    {
        /// <summary>
        /// Escolhe o invasor mais perigoso: o mais próximo do Núcleo; empate → maior ataque; empate → menor vida.
        /// </summary>
        /// <returns>O invasor prioritário, ou <c>null</c> se não houver nenhum vivo.</returns>
        InvaderEntity SelectPriorityTarget();

        /// <summary>
        /// Remove os invasores mortos do tabuleiro e notifica a morte de cada um.
        /// </summary>
        /// <returns>Quantos foram removidos.</returns>
        int RemoveDead();
    }

    /// <summary>
    /// Registro dos invasores e de quais placas eles ocupam (no máximo um invasor por placa).
    /// É uma camada de ocupação separada do conteúdo alquímico do setor: um invasor pode estar
    /// sobre um setor vazio ou sobre uma estrutura.
    /// </summary>
    public sealed class InvaderRoster : IInvaderTargeting
    {
        private readonly TectonicBoard _board;
        private readonly List<InvaderEntity> _invaders = new List<InvaderEntity>();
        private readonly ReadOnlyCollection<InvaderEntity> _readOnlyInvaders;
        private readonly Dictionary<TectonicSector, InvaderEntity> _occupancy = new Dictionary<TectonicSector, InvaderEntity>();
        private int _nextId = 1;

        /// <summary>Disparado quando um invasor surge.</summary>
        public event Action<InvaderEntity> InvaderSpawned;

        /// <summary>Disparado quando um invasor muda de placa. Parâmetros: invasor, origem, destino.</summary>
        public event Action<InvaderEntity, TectonicSector, TectonicSector> InvaderMoved;

        /// <summary>Disparado quando um invasor morto é removido.</summary>
        public event Action<InvaderEntity> InvaderSlain;

        /// <summary>Invasores vivos, em ordem de surgimento.</summary>
        public IReadOnlyList<InvaderEntity> Invaders => _readOnlyInvaders;

        /// <summary>Quantidade de invasores no tabuleiro.</summary>
        public int Count => _invaders.Count;

        /// <summary>
        /// Cria o registro.
        /// </summary>
        /// <param name="board">Tabuleiro da partida.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="board"/> for nulo.</exception>
        public InvaderRoster(TectonicBoard board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _readOnlyInvaders = _invaders.AsReadOnly();
        }

        /// <summary>
        /// Indica se há um invasor na placa.
        /// </summary>
        /// <param name="sector">Placa.</param>
        /// <returns><c>true</c> se ocupada.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        public bool IsOccupied(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            return _occupancy.ContainsKey(sector);
        }

        /// <summary>
        /// Retorna o invasor da placa.
        /// </summary>
        /// <param name="sector">Placa.</param>
        /// <returns>O invasor, ou <c>null</c> se a placa estiver livre.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="sector"/> for nulo.</exception>
        public InvaderEntity GetInvaderAt(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            return _occupancy.TryGetValue(sector, out InvaderEntity invader) ? invader : null;
        }

        /// <summary>
        /// Cria um invasor em uma placa livre.
        /// </summary>
        /// <param name="definition">Tipo do invasor.</param>
        /// <param name="sector">Placa do tabuleiro.</param>
        /// <param name="invader">Invasor criado, se houve sucesso.</param>
        /// <returns><c>false</c> se a placa já estiver ocupada.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentException">Se a placa não pertencer ao tabuleiro.</exception>
        public bool TrySpawn(IInvaderDefinition definition, TectonicSector sector, out InvaderEntity invader)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            GuardBelongsToBoard(sector);
            invader = null;

            if (_occupancy.ContainsKey(sector))
            {
                return false;
            }

            invader = new InvaderEntity(_nextId++, definition, sector);
            _invaders.Add(invader);
            _occupancy.Add(sector, invader);
            InvaderSpawned?.Invoke(invader);
            return true;
        }

        /// <summary>
        /// Move um invasor para outra placa livre.
        /// </summary>
        /// <param name="invader">Invasor deste registro.</param>
        /// <param name="destination">Placa de destino.</param>
        /// <returns><c>false</c> se o destino estiver ocupado.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        /// <exception cref="ArgumentException">Se o invasor não estiver neste registro ou o destino não for do tabuleiro.</exception>
        public bool TryMove(InvaderEntity invader, TectonicSector destination)
        {
            GuardRegistered(invader);
            GuardBelongsToBoard(destination);

            if (_occupancy.ContainsKey(destination))
            {
                return false;
            }

            TectonicSector origin = invader.Sector;
            _occupancy.Remove(origin);
            _occupancy.Add(destination, invader);
            invader.Sector = destination;
            InvaderMoved?.Invoke(invader, origin, destination);
            return true;
        }

        /// <inheritdoc />
        public InvaderEntity SelectPriorityTarget()
        {
            InvaderEntity best = null;

            foreach (InvaderEntity candidate in _invaders)
            {
                if (candidate.IsDead)
                {
                    continue;
                }

                if (best == null || IsMoreDangerous(candidate, best))
                {
                    best = candidate;
                }
            }

            return best;
        }

        /// <inheritdoc />
        public int RemoveDead()
        {
            int removed = 0;

            for (int i = _invaders.Count - 1; i >= 0; i--)
            {
                InvaderEntity invader = _invaders[i];

                if (!invader.IsDead)
                {
                    continue;
                }

                _invaders.RemoveAt(i);
                _occupancy.Remove(invader.Sector);
                invader.Sector = null;
                removed++;
                InvaderSlain?.Invoke(invader);
            }

            return removed;
        }

        private static bool IsMoreDangerous(InvaderEntity a, InvaderEntity b)
        {
            int ringA = (int)a.Sector.Ring;
            int ringB = (int)b.Sector.Ring;

            if (ringA != ringB)
            {
                return ringA < ringB;
            }

            if (a.Attack != b.Attack)
            {
                return a.Attack > b.Attack;
            }

            return a.CurrentHealth < b.CurrentHealth;
        }

        private void GuardBelongsToBoard(TectonicSector sector)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            if (!_board.GetRing(sector.Ring).Contains(sector))
            {
                throw new ArgumentException($"O setor {sector} não pertence a este tabuleiro.", nameof(sector));
            }
        }

        private void GuardRegistered(InvaderEntity invader)
        {
            if (invader == null)
            {
                throw new ArgumentNullException(nameof(invader));
            }

            if (invader.Sector == null || !_occupancy.TryGetValue(invader.Sector, out InvaderEntity registered)
                || !ReferenceEquals(registered, invader))
            {
                throw new ArgumentException($"O invasor {invader} não está neste registro.", nameof(invader));
            }
        }
    }
}
