using System;

namespace SovereignOfAshes.Audio
{
    /// <summary>
    /// Forma de onda de uma nota sintetizada.
    /// </summary>
    public enum Wave
    {
        /// <summary>Senoide pura (suave).</summary>
        Sine = 0,

        /// <summary>Triangular (suave, um pouco de corpo).</summary>
        Triangle = 1,

        /// <summary>Dente de serra limitada em harmônicos (áspera, corpo grave).</summary>
        Saw = 2,

        /// <summary>Quadrada limitada em harmônicos (oca).</summary>
        Square = 3,

        /// <summary>Sino: parciais inarmônicos (use com decaimento).</summary>
        Bell = 4
    }

    /// <summary>
    /// Sintetizador de áudio em C# puro: tons, ruído filtrado e envelopes somados em buffers de amostras (mono).
    /// Nenhuma dependência do Unity, então tudo é determinístico e testável. Os sons e músicas do jogo são
    /// receitas em cima destas peças (<see cref="SoundCatalog"/>, <see cref="MusicCatalog"/>).
    /// </summary>
    public static class AudioSynth
    {
        /// <summary>Taxa de amostragem (Hz). 22,05 kHz basta para estes timbres e deixa os buffers pequenos.</summary>
        public const int SampleRate = 22050;

        private const double TwoPi = Math.PI * 2.0;

        /// <summary>Cria um buffer silencioso.</summary>
        /// <param name="seconds">Duração.</param>
        /// <returns>Buffer de amostras.</returns>
        public static float[] Buffer(double seconds) => new float[Math.Max(1, (int)Math.Round(seconds * SampleRate))];

        /// <summary>
        /// Arredonda uma frequência para um número inteiro de ciclos em <paramref name="loopSeconds"/>, para a nota
        /// começar e terminar na mesma fase (loops sem estalo).
        /// </summary>
        /// <param name="frequency">Frequência em Hz.</param>
        /// <param name="loopSeconds">Duração do loop.</param>
        /// <returns>Frequência ajustada.</returns>
        public static double LoopFrequency(double frequency, double loopSeconds) =>
            Math.Max(1.0, Math.Round(frequency * loopSeconds)) / loopSeconds;

        /// <summary>
        /// Soma uma nota ao buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="start">Início (s).</param>
        /// <param name="duration">Duração (s).</param>
        /// <param name="freqStart">Frequência inicial (Hz).</param>
        /// <param name="freqEnd">Frequência final (glissando exponencial); 0 = constante.</param>
        /// <param name="amplitude">Volume da nota.</param>
        /// <param name="wave">Forma de onda.</param>
        /// <param name="attack">Subida (s).</param>
        /// <param name="decay">Constante de decaimento exponencial (s); 0 = sem decaimento.</param>
        /// <param name="wrap">Se a nota passar do fim, continua no começo (loops).</param>
        /// <param name="vibratoHz">Frequência do vibrato (0 = sem).</param>
        /// <param name="vibratoDepth">Profundidade do vibrato (fração da frequência).</param>
        /// <param name="sustained">Sem fade no fim (bordões de loop, que precisam emendar sem queda de volume).</param>
        public static void Tone(float[] buffer, double start, double duration, double freqStart, double freqEnd, double amplitude,
            Wave wave, double attack = 0.004, double decay = 0.0, bool wrap = false, double vibratoHz = 0.0, double vibratoDepth = 0.0,
            bool sustained = false)
        {
            int length = buffer.Length;
            int first = (int)Math.Round(start * SampleRate);
            int count = (int)Math.Round(duration * SampleRate);
            double release = Math.Min(0.012, duration * 0.25);
            double phase = 0.0;

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / SampleRate;
                double env = attack > 0.0 ? Math.Min(1.0, t / attack) : 1.0;

                if (decay > 0.0)
                {
                    env *= Math.Exp(-t / decay);
                }

                double remaining = duration - t;

                if (!sustained && remaining < release)
                {
                    env *= Math.Max(0.0, remaining / release);
                }

                double frequency = freqEnd > 0.0 && freqStart > 0.0
                    ? freqStart * Math.Pow(freqEnd / freqStart, t / duration)
                    : freqStart;

                if (vibratoHz > 0.0)
                {
                    frequency *= 1.0 + vibratoDepth * Math.Sin(TwoPi * vibratoHz * t);
                }

                phase += TwoPi * frequency / SampleRate;
                int index = first + i;

                if (index >= length)
                {
                    if (!wrap)
                    {
                        break;
                    }

                    index %= length;
                }

                if (index < 0)
                {
                    continue;
                }

                buffer[index] += (float)(WaveValue(wave, phase) * amplitude * env);
            }
        }

        /// <summary>
        /// Soma ruído passa-baixa (com a frequência de corte variando) ao buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="start">Início (s).</param>
        /// <param name="duration">Duração (s).</param>
        /// <param name="amplitude">Volume.</param>
        /// <param name="cutoffStart">Corte inicial (Hz): quanto maior, mais "chiado".</param>
        /// <param name="cutoffEnd">Corte final (Hz).</param>
        /// <param name="attack">Subida (s).</param>
        /// <param name="decay">Decaimento exponencial (s); 0 = nenhum.</param>
        /// <param name="seed">Semente do ruído (mesma semente, mesmo ruído).</param>
        public static void Noise(float[] buffer, double start, double duration, double amplitude, double cutoffStart, double cutoffEnd,
            double attack, double decay, int seed)
        {
            var random = new Random(seed);
            int first = (int)Math.Round(start * SampleRate);
            int count = (int)Math.Round(duration * SampleRate);
            double release = Math.Min(0.012, duration * 0.25);
            double lowpass = 0.0;

            for (int i = 0; i < count; i++)
            {
                int index = first + i;

                if (index >= buffer.Length)
                {
                    break;
                }

                double t = (double)i / SampleRate;
                double cutoff = cutoffStart + (cutoffEnd - cutoffStart) * (t / duration);
                double a = 1.0 - Math.Exp(-TwoPi * Math.Max(20.0, cutoff) / SampleRate);
                double sample = random.NextDouble() * 2.0 - 1.0;
                lowpass += a * (sample - lowpass);

                // Normaliza para desvio padrão ~0,5, qualquer que seja o corte (variância do filtro de um polo).
                double gain = 0.5 / Math.Sqrt(a / (2.0 - a) / 3.0);
                double env = attack > 0.0 ? Math.Min(1.0, t / attack) : 1.0;

                if (decay > 0.0)
                {
                    env *= Math.Exp(-t / decay);
                }

                double remaining = duration - t;

                if (remaining < release)
                {
                    env *= Math.Max(0.0, remaining / release);
                }

                if (index >= 0)
                {
                    buffer[index] += (float)(lowpass * gain * amplitude * env);
                }
            }
        }

        /// <summary>
        /// Escala o buffer para o pico pedido (nunca amplia ruído quase mudo além de 40x).
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="peak">Pico desejado em (0, 1].</param>
        public static void Normalize(float[] buffer, float peak)
        {
            float max = 0f;

            foreach (float sample in buffer)
            {
                max = Math.Max(max, Math.Abs(sample));
            }

            if (max < 1e-6f)
            {
                return;
            }

            float scale = Math.Min(peak / max, 40f);

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= scale;
            }
        }

        /// <summary>
        /// Aplica um fade curto no começo e no fim (evita estalos ao tocar o clipe).
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="seconds">Duração do fade.</param>
        public static void FadeEdges(float[] buffer, double seconds)
        {
            int n = Math.Min(buffer.Length / 2, (int)(seconds * SampleRate));

            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                buffer[i] *= k;
                buffer[buffer.Length - 1 - i] *= k;
            }
        }

        /// <summary>Valor da forma de onda para uma fase (radianos).</summary>
        /// <param name="wave">Forma.</param>
        /// <param name="phase">Fase.</param>
        /// <returns>Amostra aproximadamente em [-1, 1].</returns>
        public static double WaveValue(Wave wave, double phase)
        {
            switch (wave)
            {
                case Wave.Triangle:
                    return 2.0 / Math.PI * Math.Asin(Math.Sin(phase));

                case Wave.Saw:
                {
                    double sum = 0.0;

                    for (int k = 1; k <= 6; k++)
                    {
                        sum += Math.Sin(k * phase) / k;
                    }

                    return sum * 2.0 / Math.PI * 1.1;
                }

                case Wave.Square:
                {
                    double sum = 0.0;

                    for (int k = 1; k <= 9; k += 2)
                    {
                        sum += Math.Sin(k * phase) / k;
                    }

                    return sum * 4.0 / Math.PI * 0.8;
                }

                case Wave.Bell:
                    return (Math.Sin(phase) + 0.5 * Math.Sin(2.76 * phase) + 0.25 * Math.Sin(5.4 * phase) + 0.12 * Math.Sin(8.93 * phase)) / 1.6;

                default:
                    return Math.Sin(phase);
            }
        }
    }
}
