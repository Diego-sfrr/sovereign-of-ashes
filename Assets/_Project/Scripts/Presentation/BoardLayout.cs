using System;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Geometria do tabuleiro na tela (matemática pura, sem Unity, testável):
    /// raios de cada anel, ângulo de cada posição e conversão de um ponto em (anel, posição).
    /// <para>
    /// Convenção: a posição 0 fica no topo (90°) e as posições crescem no sentido horário,
    /// igual à convenção lógica do <c>TectonicBoard</c>.
    /// </para>
    /// </summary>
    public sealed class BoardLayout
    {
        /// <summary>Setores por anel.</summary>
        public int SectorsPerRing { get; }

        /// <summary>Quantidade de anéis.</summary>
        public int RingCount { get; }

        /// <summary>Raio do Núcleo Divino.</summary>
        public float CoreRadius { get; }

        /// <summary>Largura de cada anel.</summary>
        public float RingWidth { get; }

        /// <summary>Espaço entre anéis.</summary>
        public float RingGap { get; }

        /// <summary>Ângulo ocupado por um setor, em graus.</summary>
        public float StepDegrees => 360f / SectorsPerRing;

        /// <summary>Raio da borda externa do tabuleiro.</summary>
        public float OuterEdge => GetOuterRadius(RingCount - 1);

        /// <summary>
        /// Cria o layout.
        /// </summary>
        /// <param name="sectorsPerRing">Setores por anel (maior que zero).</param>
        /// <param name="ringCount">Quantidade de anéis (maior que zero).</param>
        /// <param name="coreRadius">Raio do núcleo (maior que zero).</param>
        /// <param name="ringWidth">Largura de cada anel (maior que zero).</param>
        /// <param name="ringGap">Espaço entre anéis (não negativo).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se algum valor for inválido.</exception>
        public BoardLayout(int sectorsPerRing, int ringCount, float coreRadius = 0.9f, float ringWidth = 1.05f, float ringGap = 0.1f)
        {
            if (sectorsPerRing <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sectorsPerRing), sectorsPerRing, "Deve ser maior que zero.");
            }

            if (ringCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ringCount), ringCount, "Deve ser maior que zero.");
            }

            if (coreRadius <= 0f || ringWidth <= 0f || ringGap < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(coreRadius), "Raios e larguras devem ser positivos.");
            }

            SectorsPerRing = sectorsPerRing;
            RingCount = ringCount;
            CoreRadius = coreRadius;
            RingWidth = ringWidth;
            RingGap = ringGap;
        }

        /// <summary>Raio interno do anel.</summary>
        /// <param name="ringIndex">Índice do anel (0 = mais interno).</param>
        /// <returns>Raio interno.</returns>
        public float GetInnerRadius(int ringIndex) => CoreRadius + RingGap + ringIndex * (RingWidth + RingGap);

        /// <summary>Raio externo do anel.</summary>
        /// <param name="ringIndex">Índice do anel.</param>
        /// <returns>Raio externo.</returns>
        public float GetOuterRadius(int ringIndex) => GetInnerRadius(ringIndex) + RingWidth;

        /// <summary>Raio do meio do anel (onde ficam ícones e textos).</summary>
        /// <param name="ringIndex">Índice do anel.</param>
        /// <returns>Raio central.</returns>
        public float GetCenterRadius(int ringIndex) => GetInnerRadius(ringIndex) + RingWidth * 0.5f;

        /// <summary>
        /// Ângulo (graus, anti-horário a partir do eixo X) do centro de uma posição.
        /// Aceita posições fracionárias para animar rotações.
        /// </summary>
        /// <param name="position">Posição angular.</param>
        /// <returns>Ângulo em graus.</returns>
        public float GetAngleDegrees(float position) => 90f - position * StepDegrees;

        /// <summary>
        /// Converte coordenadas polares em cartesianas.
        /// </summary>
        /// <param name="radius">Raio.</param>
        /// <param name="angleDegrees">Ângulo em graus.</param>
        /// <param name="x">X resultante.</param>
        /// <param name="y">Y resultante.</param>
        public static void PolarToCartesian(float radius, float angleDegrees, out float x, out float y)
        {
            double radians = angleDegrees * Math.PI / 180.0;
            x = (float)(radius * Math.Cos(radians));
            y = (float)(radius * Math.Sin(radians));
        }

        /// <summary>
        /// Indica se o ponto está dentro do Núcleo.
        /// </summary>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <returns><c>true</c> se estiver no núcleo.</returns>
        public bool IsInsideCore(float x, float y) => Math.Sqrt(x * x + y * y) <= CoreRadius;

        /// <summary>
        /// Descobre qual (anel, posição) está sob um ponto do tabuleiro.
        /// </summary>
        /// <param name="x">X relativo ao centro do tabuleiro.</param>
        /// <param name="y">Y relativo ao centro do tabuleiro.</param>
        /// <param name="ringIndex">Anel encontrado.</param>
        /// <param name="position">Posição angular encontrada, em [0, N).</param>
        /// <returns><c>false</c> se o ponto estiver fora dos anéis (núcleo, vãos ou fora do tabuleiro).</returns>
        public bool TryPick(float x, float y, out int ringIndex, out int position)
        {
            ringIndex = -1;
            position = -1;
            double radius = Math.Sqrt(x * x + y * y);

            for (int ring = 0; ring < RingCount; ring++)
            {
                if (radius < GetInnerRadius(ring) || radius > GetOuterRadius(ring))
                {
                    continue;
                }

                double degrees = Math.Atan2(y, x) * 180.0 / Math.PI;
                double clockwiseFromTop = 90.0 - degrees;
                clockwiseFromTop %= 360.0;

                if (clockwiseFromTop < 0)
                {
                    clockwiseFromTop += 360.0;
                }

                double step = StepDegrees;
                int slot = (int)Math.Floor((clockwiseFromTop + step * 0.5) / step);

                ringIndex = ring;
                position = slot % SectorsPerRing;
                return true;
            }

            return false;
        }
    }
}
