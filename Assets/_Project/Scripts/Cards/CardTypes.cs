using System;
using System.Collections.Generic;
using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Raridade de uma carta (afeta drop e recompensa).
    /// </summary>
    public enum CardRarity
    {
        /// <summary>Comum.</summary>
        Common = 0,

        /// <summary>Incomum.</summary>
        Uncommon = 1,

        /// <summary>Rara.</summary>
        Rare = 2,

        /// <summary>Relíquia divina.</summary>
        Divine = 3
    }

    /// <summary>
    /// Máscara de anéis que uma carta pode ter como alvo.
    /// O bit de cada anel é <c>1 &lt;&lt; (int)RingType</c>.
    /// </summary>
    [Flags]
    public enum TargetRings
    {
        /// <summary>Nenhum anel (carta sem alvo no tabuleiro).</summary>
        None = 0,

        /// <summary>Santuário Interno.</summary>
        InnerSanctuary = 1 << (int)RingType.InnerSanctuary,

        /// <summary>Civilização Intermediária.</summary>
        MiddleCivilization = 1 << (int)RingType.MiddleCivilization,

        /// <summary>Ermos Externos.</summary>
        OuterWilderness = 1 << (int)RingType.OuterWilderness,

        /// <summary>Qualquer anel.</summary>
        All = InnerSanctuary | MiddleCivilization | OuterWilderness
    }

    /// <summary>
    /// Extensões de <see cref="TargetRings"/>.
    /// </summary>
    public static class TargetRingsExtensions
    {
        /// <summary>
        /// Verifica se a máscara inclui o anel.
        /// </summary>
        /// <param name="mask">Máscara de anéis.</param>
        /// <param name="ring">Anel a verificar.</param>
        /// <returns><c>true</c> se o anel é um alvo permitido.</returns>
        public static bool Includes(this TargetRings mask, RingType ring)
        {
            int bit = 1 << (int)ring;
            return ((int)mask & bit) != 0;
        }
    }

    /// <summary>
    /// Contrato de uma carta, independente de Unity, consumido pelo <see cref="CardResolver"/>.
    /// Implementado por <see cref="CardDefinitionSO"/>; testes podem usar implementações falsas.
    /// </summary>
    public interface ICardDefinition
    {
        /// <summary>Identificador único e estável.</summary>
        string Id { get; }

        /// <summary>Nome exibido.</summary>
        string DisplayName { get; }

        /// <summary>Custo em Fé para jogar.</summary>
        int Cost { get; }

        /// <summary>Conceito principal da carta (usado para filtros, sinergias e visual).</summary>
        ConceptType PrimaryConcept { get; }

        /// <summary>Anéis em que a carta pode ser jogada.</summary>
        TargetRings AllowedRings { get; }

        /// <summary>Se a carta só pode ser jogada em setores vazios.</summary>
        bool RequiresEmptySector { get; }

        /// <summary>Efeitos executados em ordem quando a carta é jogada.</summary>
        IReadOnlyList<ITileEffect> Effects { get; }
    }
}
