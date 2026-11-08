namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Dados estáticos de um tipo de invasor.
    /// Implementado por <see cref="InvaderDefinitionSO"/>; testes podem usar implementações falsas.
    /// </summary>
    public interface IInvaderDefinition
    {
        /// <summary>Identificador único e estável.</summary>
        string Id { get; }

        /// <summary>Nome exibido.</summary>
        string DisplayName { get; }

        /// <summary>Vida máxima (maior que zero).</summary>
        int MaxHealth { get; }

        /// <summary>Dano causado ao Núcleo por turno ao alcançar o Santuário Interno.</summary>
        int Attack { get; }

        /// <summary>Fé concedida ao jogador quando o invasor é destruído.</summary>
        int FaithBounty { get; }
    }
}
