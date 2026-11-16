using System;

namespace SovereignOfAshes.Audio
{
    /// <summary>
    /// Músicas do jogo (loops).
    /// </summary>
    public enum MusicId
    {
        /// <summary>Tema do menu: pads lentos em lá menor e sinos distantes.</summary>
        Menu = 0,

        /// <summary>Tema da batalha: bordão grave, batida de coração e arpejos esparsos.</summary>
        Battle
    }

    /// <summary>
    /// Receitas das músicas. Cada loop dura <see cref="LoopSeconds"/> e é sintetizado com frequências de ciclos inteiros e
    /// notas que "dão a volta" no fim do buffer, então emenda sem estalo. Tudo determinístico.
    /// </summary>
    public static class MusicCatalog
    {
        /// <summary>Duração de cada loop (s).</summary>
        public const double LoopSeconds = 32.0;

        /// <summary>Volume padrão (0 a 1) da música relativo ao controle do jogador.</summary>
        public const float DefaultVolume = 0.6f;

        /// <summary>
        /// Gera o loop.
        /// </summary>
        /// <param name="id">Qual música.</param>
        /// <returns>Amostras em [-1, 1] (pico ~0,5).</returns>
        public static float[] Render(MusicId id) => id == MusicId.Menu ? RenderMenu() : RenderBattle();

        private static float[] RenderMenu()
        {
            float[] b = AudioSynth.Buffer(LoopSeconds);

            // Quatro acordes de 8 s: Am, F, Dm, E (a "dominante" dá a tensão de volta ao Am).
            double[][] chords =
            {
                new[] { 110.00, 130.81, 164.81, 220.00 },
                new[] { 87.31, 110.00, 130.81, 174.61 },
                new[] { 73.42, 146.83, 174.61, 220.00 },
                new[] { 82.41, 103.83, 123.47, 164.81 }
            };

            for (int c = 0; c < chords.Length; c++)
            {
                double chordStart = c * 8.0;

                foreach (double note in chords[c])
                {
                    // Cada nota é um pad: sobe e desce suave, começando 1 s antes do acorde e passando 1 s depois.
                    Pad(b, chordStart - 1.0, 10.0, note, 0.11);
                    Pad(b, chordStart - 1.0, 10.0, note * 1.004, 0.07);
                }
            }

            // Bordão contínuo em lá (ciclos inteiros: emenda perfeita).
            double drone = AudioSynth.LoopFrequency(55.0, LoopSeconds);
            AudioSynth.Tone(b, 0, LoopSeconds, drone, 0, 0.20, Wave.Sine, 0, 0, wrap: true, sustained: true);
            AudioSynth.Tone(b, 0, LoopSeconds, drone * 2, 0, 0.07, Wave.Sine, 0, 0, wrap: true, sustained: true);

            // Sinos distantes, dois por acorde.
            double[] bells = { 659.25, 523.25, 523.25, 440.00, 440.00, 587.33, 493.88, 659.25 };
            double[] times = { 3.0, 6.5, 11.0, 14.5, 19.0, 22.5, 27.0, 30.5 };

            for (int i = 0; i < bells.Length; i++)
            {
                AudioSynth.Tone(b, times[i], 4.5, bells[i], 0, 0.10, Wave.Bell, 0.01, 1.5, wrap: true);
            }

            Breathing(b, 0.05, 420, 7);
            AudioSynth.Normalize(b, 0.5f);
            return b;
        }

        private static float[] RenderBattle()
        {
            float[] b = AudioSynth.Buffer(LoopSeconds);

            double drone = AudioSynth.LoopFrequency(55.0, LoopSeconds);
            double fifth = AudioSynth.LoopFrequency(82.41, LoopSeconds);
            AudioSynth.Tone(b, 0, LoopSeconds, drone, 0, 0.24, Wave.Sine, 0, 0, wrap: true, sustained: true);
            AudioSynth.Tone(b, 0, LoopSeconds, drone * 2, 0, 0.10, Wave.Triangle, 0, 0, wrap: true, sustained: true);
            AudioSynth.Tone(b, 0, LoopSeconds, fifth, 0, 0.12, Wave.Sine, 0, 0, wrap: true, sustained: true);

            // Batida de coração a cada 2 s ("tum-tum").
            for (double t = 0.0; t < LoopSeconds; t += 2.0)
            {
                AudioSynth.Tone(b, t, 0.45, 66, 36, 0.42, Wave.Sine, 0.003, 0.12, wrap: true);
                AudioSynth.Tone(b, t + 0.34, 0.4, 58, 34, 0.30, Wave.Sine, 0.003, 0.11, wrap: true);
            }

            // Tambor grave marcando o fim de cada fase de 8 s.
            for (double t = 7.0; t < LoopSeconds; t += 8.0)
            {
                AudioSynth.Tone(b, t, 1.0, 92, 40, 0.5, Wave.Sine, 0.003, 0.3, wrap: true);
                AudioSynth.Noise(b, t, 0.4, 0.2, 500, 120, 0.002, 0.12, 301 + (int)t);
            }

            // Pad escuro que alterna Am e Dm, bem baixo.
            double[][] pads =
            {
                new[] { 110.00, 130.81, 164.81 },
                new[] { 98.00, 116.54, 146.83 }
            };

            for (int c = 0; c < 4; c++)
            {
                foreach (double note in pads[c % 2])
                {
                    Pad(b, c * 8.0 - 1.5, 11.0, note, 0.07);
                }
            }

            // Arpejos esparsos: um motivo de 4 notas a cada 4 s, rodando pela escala menor.
            double[] scale = { 220.00, 261.63, 329.63, 392.00, 293.66, 349.23 };

            for (int bar = 0; bar < 8; bar++)
            {
                for (int n = 0; n < 4; n++)
                {
                    double note = scale[(bar + n * 2) % scale.Length];
                    AudioSynth.Tone(b, bar * 4.0 + 1.0 + n * 0.5, 0.9, note, 0, 0.075, Wave.Triangle, 0.004, 0.3, wrap: true);
                }
            }

            Breathing(b, 0.06, 320, 9);
            AudioSynth.Normalize(b, 0.5f);
            return b;
        }

        // Nota longa com envelope suave (seno ao quadrado). Dá a volta no fim do buffer.
        private static void Pad(float[] b, double start, double duration, double frequency, double amplitude)
        {
            int length = b.Length;
            int first = (int)Math.Round(start * AudioSynth.SampleRate);
            int count = (int)Math.Round(duration * AudioSynth.SampleRate);
            double phase = 0.0;
            double f = AudioSynth.LoopFrequency(frequency, LoopSeconds);

            for (int i = 0; i < count; i++)
            {
                double x = (double)i / count;
                double s = Math.Sin(Math.PI * x);
                double env = s * s;
                phase += 2.0 * Math.PI * f / AudioSynth.SampleRate;
                int index = ((first + i) % length + length) % length;
                b[index] += (float)(Math.Sin(phase) * amplitude * env + Math.Sin(phase * 2.0) * amplitude * 0.25 * env);
            }
        }

        // Vento: ruído suave cujo volume sobe e desce uma vez por loop (zero nas pontas, então emenda).
        private static void Breathing(float[] b, double amplitude, double cutoff, int seed)
        {
            float[] wind = new float[b.Length];
            AudioSynth.Noise(wind, 0, LoopSeconds, 1.0, cutoff, cutoff, 0, 0, seed);

            for (int i = 0; i < b.Length; i++)
            {
                double s = Math.Sin(Math.PI * i / b.Length);
                b[i] += (float)(wind[i] * amplitude * s * s);
            }
        }
    }
}
