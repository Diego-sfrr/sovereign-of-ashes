namespace SovereignOfAshes.Core
{
    /// <summary>
    /// Definição estática (dados) de um elemento que pode existir em um setor.
    /// Implementada por <c>ElementDefinitionSO</c>, mas abstraída para manter a lógica testável sem Unity.
    /// </summary>
    public interface IElementDefinition
    {
        /// <summary>Identificador único e estável do elemento (usado como chave nas receitas).</summary>
        string Id { get; }

        /// <summary>Nome exibido ao jogador.</summary>
        string DisplayName { get; }

        /// <summary>Categoria do elemento.</summary>
        ElementKind Kind { get; }
    }

    /// <summary>
    /// Conteúdo em tempo de execução que ocupa um setor tectônico.
    /// O tabuleiro só conhece esta abstração, nunca a implementação concreta da alquimia (DIP).
    /// </summary>
    public interface ISectorContent
    {
        /// <summary>Definição estática que originou este conteúdo.</summary>
        IElementDefinition Definition { get; }
    }
}
