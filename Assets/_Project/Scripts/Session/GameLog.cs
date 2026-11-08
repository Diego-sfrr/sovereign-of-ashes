using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Tom de uma entrada da crônica (a interface usa para colorir).
    /// </summary>
    public enum LogTone
    {
        /// <summary>Informação neutra.</summary>
        Info = 0,

        /// <summary>Algo bom para o jogador.</summary>
        Good = 1,

        /// <summary>Algo ruim para o jogador.</summary>
        Bad = 2,

        /// <summary>Marco importante (turnos, fim de jogo).</summary>
        Highlight = 3
    }

    /// <summary>
    /// Uma entrada da crônica.
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>Texto.</summary>
        public string Text { get; }

        /// <summary>Tom.</summary>
        public LogTone Tone { get; }

        /// <summary>Turno em que ocorreu.</summary>
        public int Turn { get; }

        /// <summary>
        /// Cria a entrada.
        /// </summary>
        /// <param name="text">Texto.</param>
        /// <param name="tone">Tom.</param>
        /// <param name="turn">Turno.</param>
        public LogEntry(string text, LogTone tone, int turn)
        {
            Text = text ?? string.Empty;
            Tone = tone;
            Turn = turn;
        }
    }

    /// <summary>
    /// Crônica da partida: lista limitada das últimas mensagens narradas.
    /// </summary>
    public sealed class GameLog
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private readonly ReadOnlyCollection<LogEntry> _readOnlyEntries;

        /// <summary>Disparado a cada nova entrada.</summary>
        public event Action<LogEntry> EntryAdded;

        /// <summary>Quantidade máxima de entradas guardadas.</summary>
        public int Capacity { get; }

        /// <summary>Entradas, da mais antiga para a mais recente.</summary>
        public IReadOnlyList<LogEntry> Entries => _readOnlyEntries;

        /// <summary>
        /// Cria a crônica.
        /// </summary>
        /// <param name="capacity">Máximo de entradas (maior que zero).</param>
        /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="capacity"/> for menor ou igual a zero.</exception>
        public GameLog(int capacity = 120)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Deve ser maior que zero.");
            }

            Capacity = capacity;
            _readOnlyEntries = _entries.AsReadOnly();
        }

        /// <summary>
        /// Adiciona uma entrada (descarta a mais antiga se lotado).
        /// </summary>
        /// <param name="text">Texto.</param>
        /// <param name="tone">Tom.</param>
        /// <param name="turn">Turno.</param>
        public void Add(string text, LogTone tone, int turn)
        {
            var entry = new LogEntry(text, tone, turn);

            if (_entries.Count >= Capacity)
            {
                _entries.RemoveAt(0);
            }

            _entries.Add(entry);
            EntryAdded?.Invoke(entry);
        }
    }
}
