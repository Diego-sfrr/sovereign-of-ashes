using System;

namespace SovereignOfAshes.Economy
{
    /// <summary>
    /// Carteira de Fé vista por quem só precisa gastar (ex.: <c>CardResolver</c>).
    /// Segregação de interface: consumidores não conseguem gerar Fé nem mudar a capacidade.
    /// </summary>
    public interface IFaithWallet
    {
        /// <summary>Fé disponível.</summary>
        int Current { get; }

        /// <summary>
        /// Verifica se há Fé suficiente.
        /// </summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        /// <returns><c>true</c> se <see cref="Current"/> for maior ou igual a <paramref name="amount"/>.</returns>
        bool CanAfford(int amount);

        /// <summary>
        /// Gasta Fé se houver saldo; caso contrário não altera nada.
        /// </summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        /// <returns><c>true</c> se o gasto foi feito.</returns>
        bool TrySpend(int amount);
    }

    /// <summary>
    /// Reserva de Fé do deus (o "mana" do jogador), limitada a uma capacidade máxima.
    /// Fé recebida além da capacidade é perdida.
    /// </summary>
    public sealed class FaithPool : IFaithWallet
    {
        /// <summary>Disparado quando o saldo muda. Parâmetros: reserva, saldo anterior, saldo novo.</summary>
        public event Action<FaithPool, int, int> Changed;

        /// <inheritdoc />
        public int Current { get; private set; }

        /// <summary>Capacidade máxima da reserva.</summary>
        public int Capacity { get; private set; }

        /// <summary>Espaço livre até a capacidade.</summary>
        public int Headroom => Capacity - Current;

        /// <summary>
        /// Cria a reserva.
        /// </summary>
        /// <param name="initial">Saldo inicial, em [0, <paramref name="capacity"/>].</param>
        /// <param name="capacity">Capacidade (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se os valores forem inválidos.</exception>
        public FaithPool(int initial, int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Deve ser maior que zero.");
            }

            if (initial < 0 || initial > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initial), initial, $"Deve estar entre 0 e {capacity}.");
            }

            Capacity = capacity;
            Current = initial;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for negativo.</exception>
        public bool CanAfford(int amount)
        {
            GuardNonNegative(amount);
            return Current >= amount;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for negativo.</exception>
        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            SetCurrent(Current - amount);
            return true;
        }

        /// <summary>
        /// Deposita Fé "crua", sem passar pelos Mandamentos.
        /// Para renda de turno use <see cref="FaithIncomeService"/>, que aplica os Mandamentos.
        /// </summary>
        /// <param name="amount">Quantidade (não negativa).</param>
        /// <returns>Quanto foi efetivamente depositado (o excedente à capacidade é perdido).</returns>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="amount"/> for negativo.</exception>
        public int Add(int amount)
        {
            GuardNonNegative(amount);
            int deposited = Math.Min(amount, Headroom);
            SetCurrent(Current + deposited);
            return deposited;
        }

        /// <summary>
        /// Altera a capacidade (ex.: relíquia que amplia a reserva). Se diminuir, o saldo é cortado.
        /// </summary>
        /// <param name="capacity">Nova capacidade (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="capacity"/> for menor ou igual a zero.</exception>
        public void SetCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Deve ser maior que zero.");
            }

            int previous = Current;
            Capacity = capacity;
            Current = Math.Min(Current, capacity);

            // Dispara mesmo sem mudança de saldo, para a UI atualizar a barra de capacidade.
            Changed?.Invoke(this, previous, Current);
        }

        /// <summary>Representação legível para logs.</summary>
        /// <returns>Texto "Fé atual/capacidade".</returns>
        public override string ToString() => $"Fé {Current}/{Capacity}";

        private void SetCurrent(int value)
        {
            if (value == Current)
            {
                return;
            }

            int previous = Current;
            Current = value;
            Changed?.Invoke(this, previous, value);
        }

        private static void GuardNonNegative(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Não pode ser negativo.");
            }
        }
    }
}
