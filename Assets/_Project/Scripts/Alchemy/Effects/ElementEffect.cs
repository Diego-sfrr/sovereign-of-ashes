using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;

namespace SovereignOfAshes.Alchemy.Effects
{
    /// <summary>
    /// Relato imutável de um efeito alquímico que aconteceu (para crônica e efeitos visuais).
    /// </summary>
    public sealed class ElementEffectReport
    {
        /// <summary>Setor que contém o elemento.</summary>
        public TectonicSector Sector { get; }

        /// <summary>Elemento responsável.</summary>
        public IElementDefinition Element { get; }

        /// <summary>Frase pronta para a crônica.</summary>
        public string Text { get; }

        /// <summary>Se o efeito favorece o jogador.</summary>
        public bool IsBeneficial { get; }

        /// <summary>
        /// Cria o relato.
        /// </summary>
        /// <param name="sector">Setor.</param>
        /// <param name="element">Elemento.</param>
        /// <param name="text">Frase.</param>
        /// <param name="isBeneficial">Se favorece o jogador.</param>
        public ElementEffectReport(TectonicSector sector, IElementDefinition element, string text, bool isBeneficial)
        {
            Sector = sector;
            Element = element;
            Text = text ?? string.Empty;
            IsBeneficial = isBeneficial;
        }
    }

    /// <summary>
    /// Serviços da partida entregues aos efeitos alquímicos.
    /// </summary>
    public sealed class ElementEffectServices
    {
        /// <summary>Tabuleiro.</summary>
        public TectonicBoard Board { get; }

        /// <summary>Invasores.</summary>
        public InvaderRoster Invaders { get; }

        /// <summary>Combate (todo dano e cura passa pelos Mandamentos).</summary>
        public CombatResolver Combat { get; }

        /// <summary>Núcleo Divino.</summary>
        public ICombatant DivineCore { get; }

        /// <summary>Civilizações (a coleção é lida a cada uso).</summary>
        public IReadOnlyCollection<CivilizationEntity> Civilizations { get; }

        /// <summary>Concessão de Fé (passa pelos Mandamentos).</summary>
        public FaithIncomeService Faith { get; }

        /// <summary>
        /// Cria o conjunto de serviços.
        /// </summary>
        /// <param name="board">Tabuleiro.</param>
        /// <param name="invaders">Invasores.</param>
        /// <param name="combat">Combate.</param>
        /// <param name="divineCore">Núcleo Divino.</param>
        /// <param name="civilizations">Civilizações.</param>
        /// <param name="faith">Serviço de Fé.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public ElementEffectServices(
            TectonicBoard board,
            InvaderRoster invaders,
            CombatResolver combat,
            ICombatant divineCore,
            IReadOnlyCollection<CivilizationEntity> civilizations,
            FaithIncomeService faith)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Invaders = invaders ?? throw new ArgumentNullException(nameof(invaders));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            DivineCore = divineCore ?? throw new ArgumentNullException(nameof(divineCore));
            Civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
            Faith = faith ?? throw new ArgumentNullException(nameof(faith));
        }
    }

    /// <summary>
    /// Contexto de um efeito: qual elemento, em qual setor, e os serviços da partida.
    /// </summary>
    public sealed class ElementEffectContext
    {
        private readonly Action<ElementEffectReport> _sink;

        /// <summary>Setor que contém o elemento.</summary>
        public TectonicSector Sector { get; }

        /// <summary>Instância do elemento (guarda o estado, como a durabilidade).</summary>
        public SectorElement Element { get; }

        /// <summary>Serviços da partida.</summary>
        public ElementEffectServices Services { get; }

        /// <summary>
        /// Cria o contexto.
        /// </summary>
        /// <param name="sector">Setor.</param>
        /// <param name="element">Elemento.</param>
        /// <param name="services">Serviços.</param>
        /// <param name="sink">Destino dos relatos (pode ser nulo).</param>
        /// <exception cref="ArgumentNullException">Se algum argumento obrigatório for nulo.</exception>
        public ElementEffectContext(TectonicSector sector, SectorElement element, ElementEffectServices services, Action<ElementEffectReport> sink)
        {
            Sector = sector ?? throw new ArgumentNullException(nameof(sector));
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _sink = sink;
        }

        /// <summary>
        /// Informa que algo aconteceu.
        /// </summary>
        /// <param name="text">Frase para a crônica.</param>
        /// <param name="isBeneficial">Se favorece o jogador.</param>
        public void Report(string text, bool isBeneficial)
        {
            _sink?.Invoke(new ElementEffectReport(Sector, Element.Definition, text, isBeneficial));
        }

        /// <summary>
        /// Invasores vivos no setor e, opcionalmente, nos setores vizinhos (vizinhança do turno atual).
        /// </summary>
        /// <param name="includeNeighbors">Se inclui os vizinhos.</param>
        /// <returns>Lista nova (pode ser alterada pelo chamador).</returns>
        public List<InvaderEntity> InvadersNearby(bool includeNeighbors)
        {
            var found = new List<InvaderEntity>();
            AddIfAlive(Services.Invaders.GetInvaderAt(Sector), found);

            if (includeNeighbors)
            {
                foreach (TectonicSector neighbor in Services.Board.GetNeighbors(Sector).All)
                {
                    AddIfAlive(Services.Invaders.GetInvaderAt(neighbor), found);
                }
            }

            return found;
        }

        private static void AddIfAlive(InvaderEntity invader, List<InvaderEntity> found)
        {
            if (invader != null && !invader.IsDead)
            {
                found.Add(invader);
            }
        }
    }

    /// <summary>
    /// Efeito de um elemento da alquimia (Strategy). É sem estado e pode ser compartilhado entre instâncias;
    /// o estado de cada instância (ex.: durabilidade) fica em <see cref="SectorElement.GetCounter"/>.
    /// </summary>
    public abstract class ElementEffect
    {
        /// <summary>Texto curto que explica o efeito ao jogador (tooltip e Grimório).</summary>
        public abstract string Description { get; }

        /// <summary>
        /// Chamado uma vez por turno para cada setor que contém o elemento, depois da marcha dos invasores.
        /// </summary>
        /// <param name="context">Contexto.</param>
        public virtual void OnTurnEnd(ElementEffectContext context)
        {
        }

        /// <summary>
        /// Indica se o elemento impede a entrada de um invasor no setor.
        /// </summary>
        /// <param name="context">Contexto.</param>
        /// <param name="invader">Invasor que tenta entrar.</param>
        /// <returns><c>true</c> para barrar.</returns>
        public virtual bool BlocksInvader(ElementEffectContext context, InvaderEntity invader) => false;

        /// <summary>
        /// Chamado quando este elemento barrou um invasor (aplique desgaste e dano de volta aqui).
        /// </summary>
        /// <param name="context">Contexto.</param>
        /// <param name="invader">Invasor barrado.</param>
        public virtual void OnInvaderBlocked(ElementEffectContext context, InvaderEntity invader)
        {
        }
    }

    /// <summary>
    /// Fonte dos efeitos de cada elemento. Permite trocar a origem (código, ScriptableObject, JSON) sem alterar o sistema.
    /// </summary>
    public interface IElementEffectProvider
    {
        /// <summary>
        /// Efeitos de um elemento.
        /// </summary>
        /// <param name="element">Elemento.</param>
        /// <returns>Efeitos (lista vazia se o elemento não tiver nenhum; nunca nulo).</returns>
        IReadOnlyList<ElementEffect> GetEffects(IElementDefinition element);
    }
}
