using System;
using System.Globalization;
using System.Text;

namespace SovereignOfAshes.Session
{
    /// <summary>
    /// Dificuldade da partida.
    /// </summary>
    public enum Difficulty
    {
        /// <summary>Mais vida e Fé iniciais, invasões mais brandas.</summary>
        Easy = 0,

        /// <summary>Equilíbrio padrão do jogo.</summary>
        Normal = 1,

        /// <summary>Menos vida e Fé, invasões que crescem mais rápido.</summary>
        Hard = 2
    }

    /// <summary>
    /// Velocidade das animações (rotação dos anéis, marcha dos invasores). Não muda nenhuma regra.
    /// </summary>
    public enum AnimationSpeed
    {
        /// <summary>Velocidade padrão.</summary>
        Normal = 0,

        /// <summary>O dobro da velocidade.</summary>
        Fast = 1
    }

    /// <summary>
    /// Modo de jogo.
    /// </summary>
    public enum GameMode
    {
        /// <summary>Sobreviver a um número fixo de turnos.</summary>
        Standard = 0,

        /// <summary>Hardcore infinito: ondas que nunca acabam; vale o recorde de turnos.</summary>
        Endless = 1
    }

    /// <summary>
    /// Preferências do jogador, em dados puros. Sabe se transformar em texto e voltar, tolerando
    /// texto vazio, desconhecido ou corrompido (cai nos valores padrão em vez de lançar exceção).
    /// </summary>
    public sealed class GameSettings
    {
        /// <summary>Maior semente aceita (9 dígitos cabem em qualquer campo de texto e em <see cref="int"/>).</summary>
        public const int MaxSeed = 999999999;

        private int _seed;
        private int _bestEndlessTurn;
        private int _musicVolume = 6;
        private int _sfxVolume = 8;

        /// <summary>Máscara de bits dos painéis recolhidos na partida (a interface decide o significado de cada bit). Salva.</summary>
        public int PanelsCollapsed { get; set; }

        /// <summary>Volume da música, de 0 (mudo) a 10.</summary>
        public int MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = Math.Max(0, Math.Min(10, value));
        }

        /// <summary>Volume dos efeitos sonoros, de 0 (mudo) a 10.</summary>
        public int SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Math.Max(0, Math.Min(10, value));
        }

        /// <summary>Ganho da música em [0, 1].</summary>
        public float MusicGain => _musicVolume / 10f;

        /// <summary>Ganho dos efeitos em [0, 1].</summary>
        public float SfxGain => _sfxVolume / 10f;

        /// <summary>Modo da próxima partida. Não é salvo: o menu escolhe o modo pelo botão que o jogador aperta.</summary>
        public GameMode Mode { get; set; } = GameMode.Standard;

        /// <summary>Civilização escolhida para a próxima partida (<c>null</c> = Filhos da Cinza). Não é salva.</summary>
        public string CivilizationId { get; set; }

        /// <summary>Pacto escolhido para a próxima partida (<c>null</c> = sem pacto). Não é salvo.</summary>
        public string CovenantId { get; set; }

        /// <summary>Recorde do modo infinito (turnos sobrevividos). Salvo.</summary>
        public int BestEndlessTurn
        {
            get => _bestEndlessTurn;
            set => _bestEndlessTurn = Math.Max(0, Math.Min(999999, value));
        }

        /// <summary>Dificuldade das próximas partidas.</summary>
        public Difficulty Difficulty { get; set; } = Difficulty.Normal;

        /// <summary>Tela cheia.</summary>
        public bool Fullscreen { get; set; }

        /// <summary>Velocidade das animações.</summary>
        public AnimationSpeed Animation { get; set; } = AnimationSpeed.Normal;

        /// <summary>
        /// Semente do embaralhamento e dos invasores. 0 = partida aleatória; o mesmo número repete a mesma partida.
        /// O valor é sempre mantido em [0, <see cref="MaxSeed"/>].
        /// </summary>
        public int Seed
        {
            get => _seed;
            set => _seed = Math.Max(0, Math.Min(MaxSeed, value));
        }

        /// <summary>Multiplicador de velocidade das animações (1 ou 2).</summary>
        public float AnimationMultiplier => Animation == AnimationSpeed.Fast ? 2f : 1f;

        /// <summary>
        /// Cria uma cópia independente.
        /// </summary>
        /// <returns>Nova instância com os mesmos valores.</returns>
        public GameSettings Clone() => new GameSettings
        {
            Difficulty = Difficulty,
            Fullscreen = Fullscreen,
            Animation = Animation,
            Seed = Seed,
            Mode = Mode,
            CovenantId = CovenantId,
            CivilizationId = CivilizationId,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            PanelsCollapsed = PanelsCollapsed,
            BestEndlessTurn = BestEndlessTurn
        };

        /// <summary>
        /// Converte em texto no formato <c>chave=valor;chave=valor</c>.
        /// </summary>
        /// <returns>Texto pronto para guardar.</returns>
        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append("difficulty=").Append((int)Difficulty);
            text.Append(";fullscreen=").Append(Fullscreen ? 1 : 0);
            text.Append(";animation=").Append((int)Animation);
            text.Append(";seed=").Append(Seed.ToString(CultureInfo.InvariantCulture));
            text.Append(";best=").Append(BestEndlessTurn.ToString(CultureInfo.InvariantCulture));
            text.Append(";music=").Append(MusicVolume.ToString(CultureInfo.InvariantCulture));
            text.Append(";sfx=").Append(SfxVolume.ToString(CultureInfo.InvariantCulture));
            text.Append(";panels=").Append(PanelsCollapsed.ToString(CultureInfo.InvariantCulture));
            return text.ToString();
        }

        /// <summary>
        /// Lê as preferências de um texto produzido por <see cref="Serialize"/>.
        /// Campos ausentes, inválidos ou fora do intervalo ficam com o valor padrão; nunca lança exceção.
        /// </summary>
        /// <param name="text">Texto (pode ser nulo ou vazio).</param>
        /// <returns>As preferências lidas.</returns>
        public static GameSettings Parse(string text)
        {
            var settings = new GameSettings();

            if (string.IsNullOrWhiteSpace(text))
            {
                return settings;
            }

            foreach (string pair in text.Split(';'))
            {
                int separator = pair.IndexOf('=');

                if (separator <= 0)
                {
                    continue;
                }

                string key = pair.Substring(0, separator).Trim().ToLowerInvariant();
                string value = pair.Substring(separator + 1).Trim();

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                {
                    continue;
                }

                switch (key)
                {
                    case "difficulty":
                        if (Enum.IsDefined(typeof(Difficulty), number))
                        {
                            settings.Difficulty = (Difficulty)number;
                        }

                        break;
                    case "fullscreen":
                        settings.Fullscreen = number != 0;
                        break;
                    case "animation":
                        if (Enum.IsDefined(typeof(AnimationSpeed), number))
                        {
                            settings.Animation = (AnimationSpeed)number;
                        }

                        break;
                    case "seed":
                        settings.Seed = number;
                        break;
                    case "best":
                        settings.BestEndlessTurn = number;
                        break;
                    case "music":
                        settings.MusicVolume = number;
                        break;
                    case "sfx":
                        settings.SfxVolume = number;
                        break;
                    case "panels":
                        settings.PanelsCollapsed = Math.Max(0, number);
                        break;
                }
            }

            return settings;
        }
    }

    /// <summary>
    /// Onde as preferências ficam guardadas entre uma execução e outra (abstração: o jogo usa PlayerPrefs; os testes, memória).
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>Lê o texto guardado, ou vazio se não houver.</summary>
        /// <returns>Texto guardado.</returns>
        string Load();

        /// <summary>Guarda o texto.</summary>
        /// <param name="text">Texto das preferências.</param>
        void Save(string text);
    }

    /// <summary>
    /// Armazenamento em memória (testes e ambientes sem persistência).
    /// </summary>
    public sealed class InMemorySettingsStore : ISettingsStore
    {
        private string _text = string.Empty;

        /// <inheritdoc />
        public string Load() => _text;

        /// <inheritdoc />
        public void Save(string text) => _text = text ?? string.Empty;
    }
}
