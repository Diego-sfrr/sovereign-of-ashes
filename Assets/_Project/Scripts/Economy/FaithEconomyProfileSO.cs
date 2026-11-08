using UnityEngine;

namespace SovereignOfAshes.Economy
{
    /// <summary>
    /// Perfil da economia de Fé, editável no Inspector (permite variar por dificuldade ou personagem).
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Economy &gt; Faith Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "FaithEconomy", menuName = "Sovereign of Ashes/Economy/Faith Profile")]
    public sealed class FaithEconomyProfileSO : ScriptableObject
    {
        [SerializeField, Min(0)] private int _startingFaith = 3;

        [SerializeField, Min(1)] private int _capacity = 10;

        [Tooltip("Fé gerada todo turno pelo próprio Núcleo.")]
        [SerializeField, Min(0)] private int _baseIncomePerTurn = 2;

        [Tooltip("Percentual da Fé de cada civilização devota convertido em renda por turno.")]
        [SerializeField, Min(0)] private int _civilizationYieldPercent = 20;

        /// <summary>
        /// Converte o perfil em configuração imutável.
        /// </summary>
        /// <returns>Configuração validada.</returns>
        public FaithEconomyConfig CreateConfig()
        {
            return new FaithEconomyConfig(_startingFaith, _capacity, _baseIncomePerTurn, _civilizationYieldPercent);
        }

        private void OnValidate()
        {
            _startingFaith = Mathf.Clamp(_startingFaith, 0, _capacity);
        }
    }
}
