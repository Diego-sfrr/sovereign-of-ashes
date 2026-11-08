using System;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;

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

        /// <summary>
        /// Cria o contexto.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="target">Setor alvo.</param>
        /// <param name="alchemy">Sistema de alquimia.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public TileEffectContext(TectonicBoard board, TectonicSector target, AlchemySystem alchemy)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Alchemy = alchemy ?? throw new ArgumentNullException(nameof(alchemy));
        }
    }
}
