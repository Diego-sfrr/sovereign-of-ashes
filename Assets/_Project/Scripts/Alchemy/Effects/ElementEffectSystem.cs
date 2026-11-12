using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;
using SovereignOfAshes.Invaders;

namespace SovereignOfAshes.Alchemy.Effects
{
    /// <summary>
    /// Executa os efeitos dos elementos que estão no tabuleiro: o efeito periódico de fim de turno
    /// e a barreira contra a marcha dos invasores (implementa <see cref="ISectorBarrier"/>).
    /// POCO sem Unity.
    /// </summary>
    public sealed class ElementEffectSystem : ISectorBarrier
    {
        private readonly IElementEffectProvider _provider;
        private readonly ElementEffectServices _services;

        /// <summary>Disparado sempre que um efeito faz algo (crônica, partículas).</summary>
        public event Action<ElementEffectReport> EffectTriggered;

        /// <summary>
        /// Cria o sistema.
        /// </summary>
        /// <param name="provider">Fonte dos efeitos.</param>
        /// <param name="services">Serviços da partida.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public ElementEffectSystem(IElementEffectProvider provider, ElementEffectServices services)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Efeitos do conteúdo de um setor (vazio se o conteúdo não for um elemento ou não tiver efeitos).
        /// </summary>
        /// <param name="content">Conteúdo do setor (pode ser nulo).</param>
        /// <returns>Efeitos, nunca nulo.</returns>
        public IReadOnlyList<ElementEffect> GetEffects(ISectorContent content) =>
            content == null ? _provider.GetEffects(null) : _provider.GetEffects(content.Definition);

        /// <summary>
        /// Efeitos de um elemento.
        /// </summary>
        /// <param name="element">Definição do elemento.</param>
        /// <returns>Efeitos, nunca nulo.</returns>
        public IReadOnlyList<ElementEffect> GetEffects(IElementDefinition element) => _provider.GetEffects(element);

        /// <summary>
        /// Executa o efeito de fim de turno de todos os elementos do tabuleiro, do anel interno para o externo.
        /// </summary>
        public void ProcessTurnEnd()
        {
            foreach (TectonicSector sector in _services.Board.GetAllSectors())
            {
                if (!(sector.Content is SectorElement element))
                {
                    continue;
                }

                IReadOnlyList<ElementEffect> effects = _provider.GetEffects(element.Definition);

                for (int i = 0; i < effects.Count; i++)
                {
                    // Um efeito anterior pode ter destruído o elemento; nesse caso, os demais não agem.
                    if (!ReferenceEquals(sector.Content, element))
                    {
                        break;
                    }

                    effects[i].OnTurnEnd(CreateContext(sector, element));
                }
            }
        }

        /// <summary>
        /// Consulta pura: diz se o setor barraria o invasor, sem aplicar dano nem desgaste (usada na previsão da marcha).
        /// </summary>
        /// <param name="invader">Invasor que tentaria entrar.</param>
        /// <param name="destination">Setor de destino.</param>
        /// <returns><c>true</c> se algum efeito do elemento no setor barraria o invasor.</returns>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public bool WouldBlock(InvaderEntity invader, TectonicSector destination)
        {
            if (invader == null)
            {
                throw new ArgumentNullException(nameof(invader));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!(destination.Content is SectorElement element))
            {
                return false;
            }

            ElementEffectContext context = CreateContext(destination, element);
            IReadOnlyList<ElementEffect> effects = _provider.GetEffects(element.Definition);

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].BlocksInvader(context, invader))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryBlock(InvaderEntity invader, TectonicSector destination)
        {
            if (invader == null)
            {
                throw new ArgumentNullException(nameof(invader));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!(destination.Content is SectorElement element))
            {
                return false;
            }

            ElementEffectContext context = CreateContext(destination, element);
            IReadOnlyList<ElementEffect> effects = _provider.GetEffects(element.Definition);

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].BlocksInvader(context, invader))
                {
                    effects[i].OnInvaderBlocked(context, invader);
                    return true;
                }
            }

            return false;
        }

        private ElementEffectContext CreateContext(TectonicSector sector, SectorElement element) =>
            new ElementEffectContext(sector, element, _services, Publish);

        private void Publish(ElementEffectReport report) => EffectTriggered?.Invoke(report);
    }
}
