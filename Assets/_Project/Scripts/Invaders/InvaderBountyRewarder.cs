using System;
using SovereignOfAshes.Economy;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Concede a recompensa em Fé de cada invasor destruído (Observer do <see cref="InvaderRoster.InvaderSlain"/>).
    /// A recompensa passa pelos Mandamentos através do <see cref="FaithIncomeService"/>.
    /// Chame <see cref="Dispose"/> para parar de ouvir.
    /// </summary>
    public sealed class InvaderBountyRewarder : IDisposable
    {
        private readonly InvaderRoster _roster;
        private readonly FaithIncomeService _faith;
        private bool _disposed;

        /// <summary>Disparado após cada recompensa. Parâmetros: invasor, relatório da Fé concedida.</summary>
        public event Action<InvaderEntity, FaithIncomeReport> BountyGranted;

        /// <summary>
        /// Cria o recompensador e começa a ouvir as mortes.
        /// </summary>
        /// <param name="roster">Registro de invasores.</param>
        /// <param name="faith">Serviço de Fé.</param>
        /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
        public InvaderBountyRewarder(InvaderRoster roster, FaithIncomeService faith)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _faith = faith ?? throw new ArgumentNullException(nameof(faith));
            _roster.InvaderSlain += HandleSlain;
        }

        /// <summary>Para de ouvir as mortes de invasores.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _roster.InvaderSlain -= HandleSlain;
            _disposed = true;
        }

        private void HandleSlain(InvaderEntity invader)
        {
            int bounty = invader.Definition.FaithBounty;

            if (bounty <= 0)
            {
                return;
            }

            FaithIncomeReport report = _faith.GrantFaith(bounty, invader);
            BountyGranted?.Invoke(invader, report);
        }
    }
}
