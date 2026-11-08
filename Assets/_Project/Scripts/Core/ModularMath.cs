using System;

namespace SovereignOfAshes.Core
{
    /// <summary>
    /// Utilitários de aritmética modular usados pela rotação dos anéis.
    /// </summary>
    public static class ModularMath
    {
        /// <summary>
        /// Retorna o resto matemático (sempre não negativo) de <paramref name="value"/> por <paramref name="modulus"/>.
        /// Diferente do operador <c>%</c> do C#, que retorna negativo para dividendos negativos.
        /// </summary>
        /// <param name="value">Valor a ser normalizado (pode ser negativo).</param>
        /// <param name="modulus">Módulo (deve ser maior que zero).</param>
        /// <returns>Um valor no intervalo [0, <paramref name="modulus"/>).</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="modulus"/> for menor ou igual a zero.</exception>
        public static int Wrap(int value, int modulus)
        {
            if (modulus <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modulus), modulus, "O módulo deve ser maior que zero.");
            }

            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
