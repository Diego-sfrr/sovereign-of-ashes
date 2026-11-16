using System;

namespace SovereignOfAshes.Audio
{
    /// <summary>
    /// Efeitos sonoros do jogo.
    /// </summary>
    public enum SoundId
    {
        /// <summary>Clique de botão.</summary>
        Click = 0,

        /// <summary>Passar o mouse numa carta.</summary>
        CardHover,

        /// <summary>Escolher uma carta.</summary>
        CardSelect,

        /// <summary>Jogar uma carta.</summary>
        CardPlay,

        /// <summary>Um conceito se manifesta no setor.</summary>
        Infuse,

        /// <summary>Fusão alquímica (receita).</summary>
        Fusion,

        /// <summary>Invasor destruído.</summary>
        Slay,

        /// <summary>Núcleo Divino sofre dano.</summary>
        CoreHit,

        /// <summary>Núcleo Divino se cura.</summary>
        Heal,

        /// <summary>Ganho de Fé.</summary>
        FaithGain,

        /// <summary>Gasto de Fé.</summary>
        FaithSpend,

        /// <summary>Anel girando.</summary>
        RingRotate,

        /// <summary>Anel congelado.</summary>
        Freeze,

        /// <summary>Invasor surge.</summary>
        Spawn,

        /// <summary>Civilização vira herege.</summary>
        Heresy,

        /// <summary>Profanador destrói um elemento.</summary>
        Corrupt,

        /// <summary>Oferenda Divina surge.</summary>
        Offer,

        /// <summary>Conquista desbloqueada.</summary>
        Achievement,

        /// <summary>Começa a vez do jogador.</summary>
        TurnBell,

        /// <summary>Vitória.</summary>
        Victory,

        /// <summary>Derrota.</summary>
        Defeat
    }

    /// <summary>
    /// Receitas dos efeitos sonoros: cada <see cref="SoundId"/> vira um buffer mono (<see cref="AudioSynth.SampleRate"/>),
    /// sempre igual (sementes fixas). Pico limitado a ~0,9 para nunca estourar.
    /// </summary>
    public static class SoundCatalog
    {
        /// <summary>
        /// Gera o som.
        /// </summary>
        /// <param name="id">Qual som.</param>
        /// <returns>Amostras em [-1, 1].</returns>
        public static float[] Render(SoundId id)
        {
            float[] buffer;

            switch (id)
            {
                case SoundId.Click:
                    buffer = AudioSynth.Buffer(0.09);
                    AudioSynth.Noise(buffer, 0, 0.03, 0.5, 3500, 1200, 0, 0.008, 11);
                    AudioSynth.Tone(buffer, 0, 0.08, 900, 560, 0.4, Wave.Triangle, 0.001, 0.025);
                    return Done(buffer, 0.55f);

                case SoundId.CardHover:
                    buffer = AudioSynth.Buffer(0.05);
                    AudioSynth.Tone(buffer, 0, 0.04, 1500, 1250, 0.5, Wave.Sine, 0.002, 0.01);
                    return Done(buffer, 0.25f);

                case SoundId.CardSelect:
                    buffer = AudioSynth.Buffer(0.28);
                    AudioSynth.Tone(buffer, 0, 0.2, 523, 784, 0.45, Wave.Triangle, 0.004, 0.07);
                    AudioSynth.Tone(buffer, 0.02, 0.26, 1046, 0, 0.2, Wave.Bell, 0.003, 0.09);
                    return Done(buffer, 0.5f);

                case SoundId.CardPlay:
                    buffer = AudioSynth.Buffer(0.5);
                    AudioSynth.Noise(buffer, 0, 0.3, 0.55, 300, 3200, 0.07, 0.12, 21);
                    AudioSynth.Tone(buffer, 0.12, 0.38, 150, 58, 0.7, Wave.Sine, 0.003, 0.13);
                    AudioSynth.Tone(buffer, 0.12, 0.2, 600, 0, 0.12, Wave.Triangle, 0.003, 0.06);
                    return Done(buffer, 0.8f);

                case SoundId.Infuse:
                    buffer = AudioSynth.Buffer(0.8);
                    AudioSynth.Tone(buffer, 0, 0.75, 659, 0, 0.4, Wave.Bell, 0.004, 0.28);
                    AudioSynth.Noise(buffer, 0, 0.3, 0.12, 1800, 400, 0.05, 0.1, 31);
                    AudioSynth.Tone(buffer, 0, 0.7, 165, 0, 0.18, Wave.Sine, 0.05, 0.3);
                    return Done(buffer, 0.55f);

                case SoundId.Fusion:
                    buffer = AudioSynth.Buffer(1.7);
                    double[] notes = { 440, 659, 880, 1318 };
                    double[] times = { 0.0, 0.09, 0.18, 0.31 };

                    for (int i = 0; i < notes.Length; i++)
                    {
                        AudioSynth.Tone(buffer, times[i], 1.3, notes[i], 0, 0.32, Wave.Bell, 0.004, 0.45);
                    }

                    AudioSynth.Tone(buffer, 0, 1.5, 110, 0, 0.3, Wave.Sine, 0.25, 0.7);
                    AudioSynth.Noise(buffer, 0.28, 0.5, 0.08, 6000, 2000, 0.05, 0.2, 41);
                    return Done(buffer, 0.7f);

                case SoundId.Slay:
                    buffer = AudioSynth.Buffer(0.5);
                    AudioSynth.Tone(buffer, 0, 0.4, 190, 45, 0.75, Wave.Sine, 0.002, 0.11);
                    AudioSynth.Noise(buffer, 0, 0.35, 0.55, 900, 180, 0.002, 0.09, 51);
                    return Done(buffer, 0.8f);

                case SoundId.CoreHit:
                    buffer = AudioSynth.Buffer(1.0);
                    AudioSynth.Tone(buffer, 0, 0.9, 78, 30, 0.95, Wave.Sine, 0.002, 0.28);
                    AudioSynth.Noise(buffer, 0, 0.7, 0.6, 500, 90, 0.002, 0.17, 61);
                    AudioSynth.Tone(buffer, 0, 0.6, 55, 0, 0.3, Wave.Saw, 0.004, 0.2);
                    return Done(buffer, 0.9f);

                case SoundId.Heal:
                    buffer = AudioSynth.Buffer(1.3);
                    AudioSynth.Tone(buffer, 0.00, 1.0, 523, 0, 0.28, Wave.Bell, 0.01, 0.45);
                    AudioSynth.Tone(buffer, 0.12, 1.0, 659, 0, 0.26, Wave.Bell, 0.01, 0.45);
                    AudioSynth.Tone(buffer, 0.24, 1.0, 784, 0, 0.24, Wave.Bell, 0.01, 0.45);
                    AudioSynth.Tone(buffer, 0, 1.2, 261, 0, 0.18, Wave.Sine, 0.2, 0.6);
                    return Done(buffer, 0.55f);

                case SoundId.FaithGain:
                    buffer = AudioSynth.Buffer(0.6);
                    AudioSynth.Tone(buffer, 0, 0.5, 1760, 0, 0.3, Wave.Bell, 0.002, 0.2);
                    AudioSynth.Tone(buffer, 0.05, 0.5, 2349, 0, 0.2, Wave.Bell, 0.002, 0.18);
                    return Done(buffer, 0.4f);

                case SoundId.FaithSpend:
                    buffer = AudioSynth.Buffer(0.3);
                    AudioSynth.Tone(buffer, 0, 0.22, 1250, 520, 0.4, Wave.Sine, 0.002, 0.07);
                    AudioSynth.Noise(buffer, 0, 0.12, 0.2, 4000, 800, 0.002, 0.04, 71);
                    return Done(buffer, 0.4f);

                case SoundId.RingRotate:
                    buffer = AudioSynth.Buffer(0.9);
                    AudioSynth.Noise(buffer, 0.04, 0.8, 0.6, 220, 520, 0.18, 0.45, 81);
                    AudioSynth.Tone(buffer, 0.04, 0.8, 48, 54, 0.25, Wave.Saw, 0.15, 0.5, vibratoHz: 6, vibratoDepth: 0.02);
                    AudioSynth.Tone(buffer, 0, 0.18, 95, 48, 0.7, Wave.Sine, 0.002, 0.07);
                    return Done(buffer, 0.7f);

                case SoundId.Freeze:
                    buffer = AudioSynth.Buffer(0.9);
                    AudioSynth.Tone(buffer, 0, 0.6, 2200, 620, 0.3, Wave.Sine, 0.003, 0.2);
                    AudioSynth.Tone(buffer, 0.02, 0.8, 3136, 0, 0.16, Wave.Bell, 0.003, 0.35);
                    AudioSynth.Noise(buffer, 0.00, 0.08, 0.35, 7000, 3000, 0.001, 0.03, 91);
                    AudioSynth.Noise(buffer, 0.07, 0.08, 0.3, 7000, 3000, 0.001, 0.03, 92);
                    AudioSynth.Noise(buffer, 0.15, 0.1, 0.25, 6000, 2500, 0.001, 0.04, 93);
                    return Done(buffer, 0.55f);

                case SoundId.Spawn:
                    buffer = AudioSynth.Buffer(0.9);
                    AudioSynth.Tone(buffer, 0, 0.8, 60, 44, 0.5, Wave.Saw, 0.18, 0.5, vibratoHz: 7, vibratoDepth: 0.035);
                    AudioSynth.Noise(buffer, 0, 0.6, 0.35, 300, 150, 0.15, 0.3, 101);
                    return Done(buffer, 0.55f);

                case SoundId.Heresy:
                    buffer = AudioSynth.Buffer(1.6);
                    AudioSynth.Tone(buffer, 0, 1.4, 440, 0, 0.35, Wave.Bell, 0.01, 0.6);
                    AudioSynth.Tone(buffer, 0, 1.4, 466, 0, 0.35, Wave.Bell, 0.01, 0.6);
                    AudioSynth.Tone(buffer, 0, 1.5, 110, 0, 0.4, Wave.Saw, 0.2, 0.7, vibratoHz: 5, vibratoDepth: 0.01);
                    AudioSynth.Tone(buffer, 0.05, 1.3, 311, 0, 0.18, Wave.Triangle, 0.1, 0.6);
                    return Done(buffer, 0.75f);

                case SoundId.Corrupt:
                    buffer = AudioSynth.Buffer(0.6);
                    AudioSynth.Noise(buffer, 0, 0.45, 0.55, 2200, 280, 0.003, 0.13, 111);
                    AudioSynth.Tone(buffer, 0, 0.45, 311, 233, 0.22, Wave.Saw, 0.004, 0.18);
                    AudioSynth.Tone(buffer, 0, 0.4, 82, 50, 0.45, Wave.Sine, 0.003, 0.14);
                    return Done(buffer, 0.7f);

                case SoundId.Offer:
                    buffer = AudioSynth.Buffer(1.5);
                    double[] sparkle = { 784, 988, 1175, 1568, 1976 };

                    for (int i = 0; i < sparkle.Length; i++)
                    {
                        AudioSynth.Tone(buffer, i * 0.08, 1.0, sparkle[i], 0, 0.22, Wave.Bell, 0.004, 0.4);
                    }

                    AudioSynth.Tone(buffer, 0, 1.3, 392, 0, 0.16, Wave.Sine, 0.15, 0.6);
                    return Done(buffer, 0.55f);

                case SoundId.Achievement:
                    buffer = AudioSynth.Buffer(2.0);
                    double[] fanfare = { 523, 659, 784, 1047 };
                    double[] fanfareTimes = { 0.0, 0.10, 0.20, 0.34 };

                    for (int i = 0; i < fanfare.Length; i++)
                    {
                        AudioSynth.Tone(buffer, fanfareTimes[i], 1.5, fanfare[i], 0, 0.3, Wave.Bell, 0.004, 0.6);
                    }

                    AudioSynth.Tone(buffer, 0, 1.8, 261, 0, 0.2, Wave.Triangle, 0.1, 0.9);
                    AudioSynth.Tone(buffer, 0.34, 1.5, 2093, 0, 0.1, Wave.Bell, 0.004, 0.5);
                    return Done(buffer, 0.65f);

                case SoundId.TurnBell:
                    buffer = AudioSynth.Buffer(1.3);
                    AudioSynth.Tone(buffer, 0, 1.2, 220, 0, 0.45, Wave.Bell, 0.006, 0.5);
                    AudioSynth.Tone(buffer, 0, 1.0, 440, 0, 0.12, Wave.Sine, 0.006, 0.4);
                    return Done(buffer, 0.4f);

                case SoundId.Victory:
                    buffer = AudioSynth.Buffer(3.6);
                    double[] triumph = { 440, 554, 659, 880, 1109 };

                    for (int i = 0; i < triumph.Length; i++)
                    {
                        AudioSynth.Tone(buffer, i * 0.16, 2.2, triumph[i], 0, 0.28, Wave.Bell, 0.004, 0.8);
                    }

                    foreach (double chord in new[] { 220.0, 277.0, 330.0 })
                    {
                        AudioSynth.Tone(buffer, 0.1, 3.2, chord, 0, 0.14, Wave.Saw, 0.35, 1.4);
                    }

                    return Done(buffer, 0.7f);

                default:
                    buffer = AudioSynth.Buffer(3.4);
                    double[] fall = { 392, 349, 330, 294 };

                    for (int i = 0; i < fall.Length; i++)
                    {
                        AudioSynth.Tone(buffer, i * 0.38, 1.4, fall[i], 0, 0.3, Wave.Triangle, 0.01, 0.5);
                    }

                    AudioSynth.Tone(buffer, 0, 3.2, 55, 0, 0.35, Wave.Saw, 0.4, 1.4);
                    AudioSynth.Tone(buffer, 1.4, 1.8, 73, 0, 0.2, Wave.Sine, 0.2, 0.8);
                    AudioSynth.Noise(buffer, 0, 3.0, 0.15, 220, 120, 0.4, 1.2, 121);
                    return Done(buffer, 0.65f);
            }
        }

        /// <summary>Volume padrão do efeito (0 a 1), para equilibrar os sons entre si.</summary>
        /// <param name="id">Som.</param>
        /// <returns>Ganho.</returns>
        public static float DefaultVolume(SoundId id)
        {
            switch (id)
            {
                case SoundId.CardHover: return 0.5f;
                case SoundId.Click: return 0.7f;
                case SoundId.FaithGain: return 0.7f;
                case SoundId.FaithSpend: return 0.7f;
                case SoundId.TurnBell: return 0.7f;
                case SoundId.RingRotate: return 0.8f;
                case SoundId.Spawn: return 0.8f;
                default: return 1f;
            }
        }

        /// <summary>Intervalo mínimo (s) entre duas execuções do mesmo som, para não virar barulheira.</summary>
        /// <param name="id">Som.</param>
        /// <returns>Segundos.</returns>
        public static float MinInterval(SoundId id)
        {
            switch (id)
            {
                case SoundId.CardHover: return 0.06f;
                case SoundId.RingRotate: return 0.35f;
                case SoundId.Spawn: return 0.35f;
                case SoundId.Slay: return 0.12f;
                case SoundId.FaithGain: return 0.1f;
                case SoundId.FaithSpend: return 0.08f;
                case SoundId.Infuse: return 0.08f;
                default: return 0.03f;
            }
        }

        private static float[] Done(float[] buffer, float peak)
        {
            AudioSynth.FadeEdges(buffer, 0.004);
            AudioSynth.Normalize(buffer, peak);
            return buffer;
        }
    }
}
