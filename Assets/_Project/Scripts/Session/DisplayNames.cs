using SovereignOfAshes.Board;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Nomes em português dos tipos do jogo, para a crônica e a interface.
    /// </summary>
    public static class DisplayNames
    {
        /// <summary>
        /// Nome de um anel.
        /// </summary>
        /// <param name="ring">Anel.</param>
        /// <returns>Nome exibido.</returns>
        public static string Ring(RingType ring)
        {
            switch (ring)
            {
                case RingType.InnerSanctuary: return "Santuário Interno";
                case RingType.MiddleCivilization: return "Anel da Civilização";
                case RingType.OuterWilderness: return "Ermos Externos";
                default: return ring.ToString();
            }
        }

        /// <summary>
        /// Nome de um conceito.
        /// </summary>
        /// <param name="concept">Conceito.</param>
        /// <returns>Nome exibido.</returns>
        public static string Concept(ConceptType concept)
        {
            switch (concept)
            {
                case ConceptType.Flesh: return "Carne";
                case ConceptType.Fire: return "Fogo";
                case ConceptType.Iron: return "Ferro";
                case ConceptType.Time: return "Tempo";
                case ConceptType.Faith: return "Fé";
                case ConceptType.Fear: return "Medo";
                case ConceptType.Blood: return "Sangue";
                default: return concept.ToString();
            }
        }

        /// <summary>
        /// Descreve um deslocamento de rotação com sinal.
        /// </summary>
        /// <param name="delta">Deslocamento (positivo = horário).</param>
        /// <returns>Texto como "2 casas no sentido horário".</returns>
        public static string Rotation(int delta)
        {
            int steps = delta < 0 ? -delta : delta;
            string unit = steps == 1 ? "casa" : "casas";
            string direction = delta >= 0 ? "horário" : "anti-horário";
            return $"{steps} {unit} no sentido {direction}";
        }
    }
}
