using System;
using SovereignOfAshes.Board;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Habilidades que acontecem quando um invasor morre. Hoje: a explosão do Flagelo, que fere os invasores vizinhos
    /// e destrói o elemento da alquimia do setor onde caiu (então vale pensar onde matá-lo).
    /// </summary>
    public sealed class InvaderDeathTraits : IDisposable
    {
        private readonly InvaderRoster _roster;
        private readonly TectonicBoard _board;
        private readonly CombatResolver _combat;
        private bool _disposed;

        /// <summary>Disparado quando um invasor explode. Parâmetros: o invasor, quantos vizinhos feriu e se destruiu um elemento.</summary>
        public event Action<InvaderEntity, int, bool> Exploded;

        /// <summary>Cria o observador.</summary>
        /// <param name="roster">Invasores.</param>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="combat">Resolvedor de combate.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public InvaderDeathTraits(InvaderRoster roster, TectonicBoard board, CombatResolver combat)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _roster.InvaderSlain += OnSlain;
        }

        /// <summary>Para de ouvir os eventos.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _roster.InvaderSlain -= OnSlain;
        }

        private void OnSlain(InvaderEntity invader)
        {
            int damage = invader.Definition.ExplodeDamage();

            if (damage <= 0 || invader.LastSector == null)
            {
                return;
            }

            TectonicSector sector = invader.LastSector;
            var victims = 0;

            foreach (TectonicSector neighbor in _board.GetNeighbors(sector).All)
            {
                InvaderEntity other = _roster.GetInvaderAt(neighbor);

                if (other != null && !other.IsDead)
                {
                    _combat.DealDamage(other, damage, invader);
                    victims++;
                }
            }

            bool destroyed = !sector.IsEmpty;

            if (destroyed)
            {
                sector.Clear();
            }

            Exploded?.Invoke(invader, victims, destroyed);

            // Quem morreu na explosão sai agora (e pode explodir também: a reação em cadeia é parte do risco).
            _roster.RemoveDead();
        }
    }
}
