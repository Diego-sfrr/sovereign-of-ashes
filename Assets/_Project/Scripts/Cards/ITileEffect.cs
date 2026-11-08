using System;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Efeito de carta sobre o tabuleiro (Strategy Pattern).
    /// Cada nova carta é uma nova implementação; nenhuma classe existente precisa mudar (OCP).
    /// </summary>
    public interface ITileEffect
    {
        /// <summary>
        /// Executa o efeito.
        /// </summary>
        /// <param name="context">Contexto com o alvo e os serviços disponíveis.</param>
        void Apply(TileEffectContext context);
    }

    /// <summary>
    /// Dados e serviços entregues a um <see cref="ITileEffect"/> durante a execução.
    /// </summary>
    public sealed class TileEffectContext
    {
        /// <summary>Tabuleiro da partida.</summary>
        public TectonicBoard Board { get; }

        /// <summary>Setor escolhido pelo jogador.</summary>
        public TectonicSector Target { get; }

        /// <summary>Sistema de alquimia da partida.</summary>
        public AlchemySystem Alchemy { get; }

        /// <summary>Invasores no tabuleiro.</summary>
        public InvaderRoster Invaders { get; }

        /// <summary>Resolvedor de combate (todo dano de carta passa pelos Mandamentos).</summary>
        public CombatResolver Combat { get; }

        /// <summary>Origem do efeito (normalmente a carta), repassada aos Mandamentos. Pode ser nula.</summary>
        public object Source { get; }

        /// <summary>
        /// Cria o contexto.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="target">Setor alvo.</param>
        /// <param name="alchemy">Sistema de alquimia.</param>
        /// <param name="invaders">Registro de invasores.</param>
        /// <param name="combat">Resolvedor de combate.</param>
        /// <param name="source">Origem do efeito (opcional).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento obrigatório for nulo.</exception>
        public TileEffectContext(
            TectonicBoard board,
            TectonicSector target,
            AlchemySystem alchemy,
            InvaderRoster invaders,
            CombatResolver combat,
            object source = null)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Alchemy = alchemy ?? throw new ArgumentNullException(nameof(alchemy));
            Invaders = invaders ?? throw new ArgumentNullException(nameof(invaders));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Source = source;
        }
    }
}
