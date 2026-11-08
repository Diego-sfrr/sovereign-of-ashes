using UnityEngine;

namespace SovereignOfAshes.Cards
{
    /// <summary>
    /// Base dos efeitos de carta configuráveis no Inspector.
    /// O asset guarda só os parâmetros; a lógica fica no <see cref="ITileEffect"/> criado por
    /// <see cref="CreateEffect"/> (Factory Method), que continua testável sem Unity.
    /// </summary>
    public abstract class CardEffectDefinitionSO : ScriptableObject
    {
        /// <summary>
        /// Cria a instância lógica do efeito.
        /// </summary>
        /// <returns>Efeito pronto para ser executado.</returns>
        public abstract ITileEffect CreateEffect();
    }
}
