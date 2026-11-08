using System;
using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Efeito de carta (Strategy Pattern).
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
    /// Serviços da partida disponíveis para os efeitos de carta.
    /// Agrupar as dependências evita construtores gigantes e permite adicionar serviços sem quebrar os efeitos existentes.
    /// </summary>
    public sealed class CardPlayServices
    {
        /// <summary>Tabuleiro da partida.</summary>
        public TectonicBoard Board { get; }

        /// <summary>Sistema de alquimia.</summary>
        public AlchemySystem Alchemy { get; }

        /// <summary>Invasores no tabuleiro.</summary>
        public InvaderRoster Invaders { get; }

        /// <summary>Resolvedor de combate (todo dano e cura de carta passa pelos Mandamentos).</summary>
        public CombatResolver Combat { get; }

        /// <summary>Núcleo Divino do jogador.</summary>
        public ICombatant DivineCore { get; }

        /// <summary>Civilizações da partida.</summary>
        public IReadOnlyCollection<CivilizationEntity> Civilizations { get; }

        /// <summary>
        /// Cria o conjunto de serviços.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="alchemy">Alquimia.</param>
        /// <param name="invaders">Invasores.</param>
        /// <param name="combat">Combate.</param>
        /// <param name="divineCore">Núcleo Divino.</param>
        /// <param name="civilizations">Civilizações (a coleção é lida a cada uso, então pode crescer).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public CardPlayServices(
            TectonicBoard board,
            AlchemySystem alchemy,
            InvaderRoster invaders,
            CombatResolver combat,
            ICombatant divineCore,
            IReadOnlyCollection<CivilizationEntity> civilizations)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Alchemy = alchemy ?? throw new ArgumentNullException(nameof(alchemy));
            Invaders = invaders ?? throw new ArgumentNullException(nameof(invaders));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            DivineCore = divineCore ?? throw new ArgumentNullException(nameof(divineCore));
            Civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
        }
    }

    /// <summary>
    /// Dados e serviços entregues a um <see cref="ITileEffect"/> durante a execução.
    /// </summary>
    public sealed class TileEffectContext
    {
        /// <summary>Serviços da partida.</summary>
        public CardPlayServices Services { get; }

        /// <summary>Setor escolhido pelo jogador, ou <c>null</c> em cartas globais.</summary>
        public TectonicSector Target { get; }

        /// <summary>Indica se há um setor alvo.</summary>
        public bool HasTarget => Target != null;

        /// <summary>Origem do efeito (normalmente a carta), repassada aos Mandamentos. Pode ser nula.</summary>
        public object Source { get; }

        /// <summary>Atalho para o tabuleiro.</summary>
        public TectonicBoard Board => Services.Board;

        /// <summary>Atalho para a alquimia.</summary>
        public AlchemySystem Alchemy => Services.Alchemy;

        /// <summary>Atalho para os invasores.</summary>
        public InvaderRoster Invaders => Services.Invaders;

        /// <summary>Atalho para o combate.</summary>
        public CombatResolver Combat => Services.Combat;

        /// <summary>
        /// Cria o contexto.
        /// </summary>
        /// <param name="services">Serviços da partida.</param>
        /// <param name="target">Setor alvo (nulo em cartas globais).</param>
        /// <param name="source">Origem do efeito (opcional).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="services"/> for nulo.</exception>
        public TileEffectContext(CardPlayServices services, TectonicSector target, object source = null)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Target = target;
            Source = source;
        }
    }
}
