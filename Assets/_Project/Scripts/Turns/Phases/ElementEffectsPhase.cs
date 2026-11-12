using System;
using SovereignOfAshes.Alchemy.Effects;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Turns.Phases
{
    /// <summary>
    /// Fase dos Efeitos Alquímicos: os elementos do tabuleiro agem (queimam, drenam, rendem Fé, acalmam civilizações).
    /// Roda depois da marcha, então um invasor que acabou de chegar a uma Forja já é queimado no mesmo turno.
    /// Invasores mortos pelos efeitos são removidos aqui, o que dispara a recompensa em Fé.
    /// </summary>
    public sealed class ElementEffectsPhase : ITurnPhase
    {
        private readonly ElementEffectSystem _effects;
        private readonly InvaderRoster _roster;

        /// <inheritdoc />
        public string Name => "Efeitos Alquímicos";

        /// <summary>
        /// Cria a fase.
        /// </summary>
        /// <param name="effects">Sistema de efeitos.</param>
        /// <param name="roster">Registro de invasores.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public ElementEffectsPhase(ElementEffectSystem effects, InvaderRoster roster)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        /// <inheritdoc />
        public PhaseResult Execute(int turnNumber)
        {
            _effects.ProcessTurnEnd();
            _roster.RemoveDead();
            return PhaseResult.Continue;
        }
    }
}
