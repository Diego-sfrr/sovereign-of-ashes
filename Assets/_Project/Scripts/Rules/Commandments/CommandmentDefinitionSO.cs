using UnityEngine;

namespace SovereignOfAshes.Rules.Commandments
{
    /// <summary>
    /// Base para Mandamentos configuráveis no Inspector (relíquias).
    /// O asset guarda apenas dados; a lógica vive em classes C# puras criadas por <see cref="CreateCommandment"/> (Factory Method).
    /// </summary>
    public abstract class CommandmentDefinitionSO : ScriptableObject
    {
        [Tooltip("Id único. Deixe vazio para usar o nome do asset.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [TextArea]
        [SerializeField] private string _description;

        [Tooltip("Maior prioridade executa primeiro.")]
        [SerializeField] private int _priority;

        [SerializeField] private Sprite _icon;

        /// <summary>Id efetivo do Mandamento.</summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>Nome exibido.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <summary>Descrição para tooltip.</summary>
        public string Description => _description;

        /// <summary>Prioridade de execução.</summary>
        public int Priority => _priority;

        /// <summary>Ícone da relíquia.</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Cria a instância lógica do Mandamento.
        /// </summary>
        /// <returns>Mandamento pronto para o <see cref="RuleModifierSystem"/>.</returns>
        public abstract ICommandment CreateCommandment();
    }
}
