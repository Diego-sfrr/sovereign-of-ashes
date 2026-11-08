namespace SovereignOfAshes.Board
{
    /// <summary>
    /// Anéis concêntricos da Crosta Viva, ordenados do Núcleo Divino para fora.
    /// O valor numérico é a distância ao núcleo e é usado para encontrar vizinhos radiais.
    /// </summary>
    public enum RingType
    {
        /// <summary>Anel mais interno, encostado no Núcleo Divino.</summary>
        InnerSanctuary = 0,

        /// <summary>Anel intermediário onde vivem as civilizações.</summary>
        MiddleCivilization = 1,

        /// <summary>Anel externo, fronteira com os invasores.</summary>
        OuterWilderness = 2
    }

    /// <summary>
    /// Sentido de rotação de um anel. O valor é o sinal aplicado ao deslocamento.
    /// Convenção: posições crescem no sentido horário.
    /// </summary>
    public enum RotationDirection
    {
        /// <summary>Sentido horário (posição +1 por passo).</summary>
        Clockwise = 1,

        /// <summary>Sentido anti-horário (posição -1 por passo).</summary>
        CounterClockwise = -1
    }

    /// <summary>
    /// Comando imutável de rotação a ser aplicado em um anel no fim do turno.
    /// </summary>
    public readonly struct RingRotation
    {
        /// <summary>Anel a ser girado.</summary>
        public RingType Ring { get; }

        /// <summary>Quantidade de posições (não negativa).</summary>
        public int Steps { get; }

        /// <summary>Sentido da rotação.</summary>
        public RotationDirection Direction { get; }

        /// <summary>
        /// Cria um comando de rotação.
        /// </summary>
        /// <param name="ring">Anel a ser girado.</param>
        /// <param name="steps">Quantidade de posições (não negativa).</param>
        /// <param name="direction">Sentido da rotação.</param>
        public RingRotation(RingType ring, int steps, RotationDirection direction)
        {
            Ring = ring;
            Steps = steps;
            Direction = direction;
        }
    }
}
