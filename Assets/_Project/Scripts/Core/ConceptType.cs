namespace SovereignOfAshes.Core
{
    /// <summary>
    /// Conceitos primordiais carregados pelas cartas base. São os "reagentes" da Alquimia Divina.
    /// </summary>
    public enum ConceptType
    {
        /// <summary>Carne: vida bruta, corpos, multiplicação.</summary>
        Flesh = 0,

        /// <summary>Fogo: destruição, purificação, energia.</summary>
        Fire = 1,

        /// <summary>Ferro: estrutura, guerra, ferramenta.</summary>
        Iron = 2,

        /// <summary>Tempo: envelhecimento, erosão, maturação.</summary>
        Time = 3,

        /// <summary>Fé: devoção, ordem divina, milagre.</summary>
        Faith = 4,

        /// <summary>Medo: controle, loucura, submissão.</summary>
        Fear = 5,

        /// <summary>Sangue: sacrifício, linhagem, pacto.</summary>
        Blood = 6
    }

    /// <summary>
    /// Categoria de um elemento que ocupa um setor do tabuleiro.
    /// </summary>
    public enum ElementKind
    {
        /// <summary>Conceito puro recém-manifestado (resultado de jogar uma carta em setor vazio).</summary>
        Concept = 0,

        /// <summary>Entidade viva ou consciente gerada por fusão (ex.: Golem de Ferro).</summary>
        Entity = 1,

        /// <summary>Estrutura inanimada gerada por fusão (ex.: Forja Sagrada).</summary>
        Structure = 2
    }
}
