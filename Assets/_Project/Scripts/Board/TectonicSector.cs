using System;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Board
{
    /// <summary>
    /// Uma Placa Tectônica: um setor físico de um anel.
    /// O setor tem identidade fixa (<see cref="PlateIndex"/>) e se move junto com o anel ao girar,
    /// carregando seu conteúdo. A posição angular atual é calculada pelo <see cref="TectonicRing"/>.
    /// </summary>
    public sealed class TectonicSector
    {
        /// <summary>
        /// Disparado quando o conteúdo do setor muda. Parâmetros: setor, conteúdo anterior, conteúdo novo.
        /// </summary>
        public event Action<TectonicSector, ISectorContent, ISectorContent> ContentChanged;

        /// <summary>Anel ao qual o setor pertence.</summary>
        public RingType Ring { get; }

        /// <summary>Índice fixo da placa dentro do anel (não muda com a rotação).</summary>
        public int PlateIndex { get; }

        /// <summary>Conteúdo atual do setor, ou <c>null</c> se estiver vazio.</summary>
        public ISectorContent Content { get; private set; }

        /// <summary>Indica se o setor não possui conteúdo.</summary>
        public bool IsEmpty => Content == null;

        /// <summary>
        /// Cria um setor. Normalmente chamado apenas pelo <see cref="TectonicRing"/>.
        /// </summary>
        /// <param name="ring">Anel ao qual o setor pertence.</param>
        /// <param name="plateIndex">Índice fixo da placa (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="plateIndex"/> for negativo.</exception>
        internal TectonicSector(RingType ring, int plateIndex)
        {
            if (plateIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plateIndex), plateIndex, "O índice da placa não pode ser negativo.");
            }

            Ring = ring;
            PlateIndex = plateIndex;
        }

        /// <summary>
        /// Substitui o conteúdo do setor.
        /// </summary>
        /// <param name="content">Novo conteúdo (não nulo). Para esvaziar use <see cref="Clear"/>.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="content"/> for nulo.</exception>
        public void SetContent(ISectorContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content), "Use Clear() para esvaziar um setor.");
            }

            ReplaceContent(content);
        }

        /// <summary>
        /// Esvazia o setor.
        /// </summary>
        /// <returns>O conteúdo removido, ou <c>null</c> se o setor já estava vazio.</returns>
        public ISectorContent Clear()
        {
            ISectorContent removed = Content;
            ReplaceContent(null);
            return removed;
        }

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Texto no formato "Anel#Placa [Conteúdo]".</returns>
        public override string ToString()
        {
            string content = IsEmpty ? "vazio" : Content.Definition?.DisplayName ?? "?";
            return $"{Ring}#{PlateIndex} [{content}]";
        }

        private void ReplaceContent(ISectorContent newContent)
        {
            if (ReferenceEquals(Content, newContent))
            {
                return;
            }

            ISectorContent previous = Content;
            Content = newContent;
            ContentChanged?.Invoke(this, previous, newContent);
        }
    }
}
