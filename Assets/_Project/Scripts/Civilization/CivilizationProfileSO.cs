using UnityEngine;

namespace SovereignOfAshes.Civilization
{
    /// <summary>
    /// Perfil de dados de uma civilização, editável no Inspector.
    /// Criado em: Create &gt; Sovereign of Ashes &gt; Civilization &gt; Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "Civilization_", menuName = "Sovereign of Ashes/Civilization/Profile")]
    public sealed class CivilizationProfileSO : ScriptableObject
    {
        [SerializeField] private string _civilizationName = "Filhos da Cinza";

        [Header("Valores iniciais")]
        [SerializeField, Min(0)] private int _initialMilitaryPower = 5;
        [SerializeField, Min(0)] private int _initialFaith = 10;
        [SerializeField, Min(0)] private int _initialRationality = 0;

        [Header("Limites")]
        [SerializeField, Min(1)] private int _maxFaith = 20;
        [SerializeField, Min(1)] private int _maxRationality = 10;

        [Tooltip("Uma facção herege volta a ser devota quando a Racionalidade cai até este valor.")]
        [SerializeField, Min(0)] private int _redemptionThreshold = 3;

        /// <summary>Nome da civilização.</summary>
        public string CivilizationName => _civilizationName;

        /// <summary>
        /// Converte o perfil em configuração imutável para a camada de lógica.
        /// </summary>
        /// <returns>Configuração validada.</returns>
        public CivilizationStatsConfig CreateConfig()
        {
            return new CivilizationStatsConfig(
                _initialMilitaryPower,
                _initialFaith,
                _initialRationality,
                _maxFaith,
                _maxRationality,
                _redemptionThreshold);
        }

        /// <summary>
        /// Cria uma entidade de civilização a partir deste perfil.
        /// </summary>
        /// <returns>Nova entidade no estado inicial.</returns>
        public CivilizationEntity CreateEntity()
        {
            return new CivilizationEntity(_civilizationName, CreateConfig());
        }

        private void OnValidate()
        {
            _initialFaith = Mathf.Clamp(_initialFaith, 0, _maxFaith);
            _initialRationality = Mathf.Clamp(_initialRationality, 0, _maxRationality);
            _redemptionThreshold = Mathf.Clamp(_redemptionThreshold, 0, _maxRationality - 1);
        }
    }
}
