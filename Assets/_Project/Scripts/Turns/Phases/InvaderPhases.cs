using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Turns.Phases
{
    /// <summary>
    /// Fase da Marcha: cada invasor avança um anel para dentro, para o setor radialmente alinhado neste turno.
    /// Quem já está no Santuário Interno ataca o Núcleo Divino. Setor de destino ocupado = o invasor espera.
    /// Os invasores são processados do mais interno para o mais externo, para que a fila ande no mesmo turno.
    /// </summary>
    public sealed class InvaderAdvancePhase : ITurnPhase
    {
        private readonly InvaderRoster _roster;
        private readonly TectonicBoard _board;
        private readonly CombatResolver _combat;
        private readonly ICombatant _divineCore;

        /// <summary>Disparado quando um invasor ataca o Núcleo. Parâmetros: invasor, dano bruto.</summary>
        public event Action<InvaderEntity, int> InvaderAttackedCore;

        /// <summary>Disparado quando um invasor não avança porque o destino está ocupado.</summary>
        public event Action<InvaderEntity> InvaderBlocked;

        /// <inheritdoc />
        public string Name => "Marcha dos Invasores";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="roster">Registro de invasores.</param>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="combat">Resolvedor de combate.</param>
        /// <param name="divineCore">Núcleo Divino.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public InvaderAdvancePhase(InvaderRoster roster, TectonicBoard board, CombatResolver combat, ICombatant divineCore)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _divineCore = divineCore ?? throw new ArgumentNullException(nameof(divineCore));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            var marching = new List<InvaderEntity>(_roster.Invaders);

            // Ordenação estável: mais interno primeiro; empate mantém a ordem de surgimento.
            marching.Sort((a, b) =>
            {
                int byRing = ((int)a.Sector.Ring).CompareTo((int)b.Sector.Ring);
                return byRing != 0 ? byRing : a.Id.CompareTo(b.Id);
            });

            foreach (InvaderEntity invader in marching)
            {
                if (invader.IsDead || invader.Sector == null)
                {
                    continue;
                }

                SectorNeighbors neighbors = _board.GetNeighbors(invader.Sector);

                if (neighbors.IsAdjacentToCore)
                {
                    if (invader.Attack > 0)
                    {
                        _combat.DealDamage(_divineCore, invader.Attack, invader);
                        InvaderAttackedCore?.Invoke(invader, invader.Attack);
                    }

                    continue;
                }

                if (neighbors.Inner == null || !_roster.TryMove(invader, neighbors.Inner))
                {
                    InvaderBlocked?.Invoke(invader);
                }
            }

            // Mandamentos como "dano refletido" podem matar invasores durante a marcha.
            _roster.RemoveDead();
            return PhaseResult.Continue;
        }
    }

    /// <summary>
    /// Fase do Surgimento: cria os invasores planejados em placas livres do anel externo.
    /// Se o anel externo estiver lotado, o excedente da onda é perdido (e anunciado).
    /// </summary>
    public sealed class InvaderSpawnPhase : ITurnPhase
    {
        private readonly InvaderRoster _roster;
        private readonly TectonicBoard _board;
        private readonly IInvasionPlanner _planner;
        private readonly Random _random;

        /// <summary>Disparado quando invasores não cabem no anel externo. Parâmetros: tipo, quantos ficaram de fora.</summary>
        public event Action<IInvaderDefinition, int> SpawnOverflow;

        /// <inheritdoc />
        public string Name => "Surgimento dos Invasores";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="roster">Registro de invasores.</param>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="planner">Planejador das ondas.</param>
        /// <param name="random">Gerador das posições (injete com semente fixa para partidas reproduzíveis).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public InvaderSpawnPhase(InvaderRoster roster, TectonicBoard board, IInvasionPlanner planner, Random random)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            foreach (InvaderSpawnOrder order in _planner.PlanSpawns(turnNumber))
            {
                if (order.Definition == null || order.Count <= 0)
                {
                    continue;
                }

                int spawned = 0;

                for (int i = 0; i < order.Count; i++)
                {
                    TectonicSector sector = PickFreeOuterSector();

                    if (sector == null || !_roster.TrySpawn(order.Definition, sector, out _))
                    {
                        break;
                    }

                    spawned++;
                }

                if (spawned < order.Count)
                {
                    SpawnOverflow?.Invoke(order.Definition, order.Count - spawned);
                }
            }

            return PhaseResult.Continue;
        }

        private TectonicSector PickFreeOuterSector()
        {
            var free = new List<TectonicSector>();

            foreach (TectonicSector sector in _board.GetRing(RingType.OuterWilderness).Sectors)
            {
                if (!_roster.IsOccupied(sector))
                {
                    free.Add(sector);
                }
            }

            return free.Count == 0 ? null : free[_random.Next(free.Count)];
        }
    }
}
