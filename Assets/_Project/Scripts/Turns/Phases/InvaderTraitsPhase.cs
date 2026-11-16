using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Turns.Phases
{
    /// <summary>
    /// Fase das habilidades dos invasores, logo depois da marcha: Profanadores destroem o elemento do setor em
    /// que acabaram de chegar e Arautos/Inquisidores empurram a Racionalidade das civilizações rumo à heresia.
    /// Só atua sobre invasores vivos; quem foi destruído antes (por cartas, Forjas, civilizações) não age.
    /// </summary>
    public sealed class InvaderTraitsPhase : ITurnPhase
    {
        private readonly InvaderRoster _roster;
        private readonly IReadOnlyCollection<CivilizationEntity> _civilizations;

        /// <summary>Disparado quando um Profanador destrói um elemento. Parâmetros: invasor, setor, elemento destruído.</summary>
        public event Action<InvaderEntity, TectonicSector, ISectorContent> ElementCorrupted;

        /// <summary>Disparado quando um invasor soma Racionalidade às civilizações. Parâmetros: invasor, quantidade.</summary>
        public event Action<InvaderEntity, int> RationalityPushed;

        /// <inheritdoc />
        public string Name => "Habilidades dos Invasores";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="roster">Invasores.</param>
        /// <param name="civilizations">Civilizações (a coleção é lida a cada execução).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public InvaderTraitsPhase(InvaderRoster roster, IReadOnlyCollection<CivilizationEntity> civilizations)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            foreach (InvaderEntity invader in new List<InvaderEntity>(_roster.Invaders))
            {
                if (invader.IsDead || invader.Sector == null)
                {
                    continue;
                }

                if (invader.Definition.CorruptsElements() && !invader.Sector.IsEmpty)
                {
                    TectonicSector sector = invader.Sector;
                    ISectorContent destroyed = sector.Clear();
                    ElementCorrupted?.Invoke(invader, sector, destroyed);
                }

                int pressure = invader.Definition.RationalityPerTurn();

                if (pressure > 0)
                {
                    // Cópia defensiva: a heresia pode alterar a coleção durante a iteração.
                    foreach (CivilizationEntity civilization in new List<CivilizationEntity>(_civilizations))
                    {
                        civilization?.ChangeRationality(pressure);
                    }

                    RationalityPushed?.Invoke(invader, pressure);
                }
            }

            return PhaseResult.Continue;
        }
    }
}
