using System;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Audio;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// O áudio é sintetizado por código: aqui se garante que nenhum som sai mudo, estourado, com NaN ou com estalo.
    /// </summary>
    public sealed class AudioSynthTests
    {
        private static float Rms(float[] samples) => (float)Math.Sqrt(samples.Sum(s => (double)s * s) / samples.Length);

        private static float Peak(float[] samples) => samples.Max(Math.Abs);

        [Test]
        public void EverySound_IsAudible_InRange_AndFinite()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                float[] samples = SoundCatalog.Render(id);
                double seconds = (double)samples.Length / AudioSynth.SampleRate;

                Assert.IsTrue(samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s)), id + " tem NaN/infinito");
                Assert.LessOrEqual(Peak(samples), 0.95f, id + " estoura");
                Assert.GreaterOrEqual(Peak(samples), 0.2f, id + " está baixo demais");
                Assert.GreaterOrEqual(Rms(samples), 0.01f, id + " é quase silêncio");
                Assert.Greater(seconds, 0.03, id.ToString());
                Assert.LessOrEqual(seconds, 4.0, id + " é longo demais");
            }
        }

        [Test]
        public void EverySound_StartsAndEndsAtSilence_SoItNeverClicks()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                float[] samples = SoundCatalog.Render(id);

                Assert.Less(Math.Abs(samples[0]), 0.02f, id + " começa com estalo");
                Assert.Less(Math.Abs(samples[samples.Length - 1]), 0.02f, id + " termina com estalo");
            }
        }

        [Test]
        public void Rendering_IsDeterministic()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                CollectionAssert.AreEqual(SoundCatalog.Render(id), SoundCatalog.Render(id), id.ToString());
            }

            CollectionAssert.AreEqual(MusicCatalog.Render(MusicId.Menu), MusicCatalog.Render(MusicId.Menu));
        }

        [Test]
        public void Sounds_AreDistinctFromEachOther()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                float[] samples = SoundCatalog.Render(id);
                string fingerprint = $"{samples.Length}:{Math.Round(Rms(samples), 3)}:{Math.Round(samples.Skip(samples.Length / 3).First(), 3)}";
                Assert.IsTrue(seen.Add(fingerprint), id + " parece igual a outro som");
            }
        }

        [Test]
        public void MusicLoops_AreAudible_Quiet_AndSeamless()
        {
            foreach (MusicId id in Enum.GetValues(typeof(MusicId)))
            {
                float[] loop = MusicCatalog.Render(id);

                Assert.AreEqual((int)(MusicCatalog.LoopSeconds * AudioSynth.SampleRate), loop.Length, id.ToString());
                Assert.IsTrue(loop.All(s => !float.IsNaN(s) && !float.IsInfinity(s)), id + " tem NaN");
                Assert.LessOrEqual(Peak(loop), 0.55f, id + " alto demais para música de fundo");
                Assert.GreaterOrEqual(Rms(loop), 0.05f, id + " quase mudo");

                // Emenda: a distância entre a última e a primeira amostra tem de ser parecida com a de duas amostras vizinhas.
                float seam = Math.Abs(loop[0] - loop[loop.Length - 1]);
                float typicalStep = Enumerable.Range(1, 2000).Select(i => Math.Abs(loop[i] - loop[i - 1])).Max();
                Assert.LessOrEqual(seam, Math.Max(0.02f, typicalStep * 2f), id + " tem estalo na emenda do loop");
            }
        }

        [Test]
        public void Music_ThemesDiffer()
        {
            float[] menu = MusicCatalog.Render(MusicId.Menu);
            float[] battle = MusicCatalog.Render(MusicId.Battle);

            Assert.AreEqual(menu.Length, battle.Length);
            Assert.IsFalse(menu.SequenceEqual(battle));
        }

        [Test]
        public void LoopFrequency_GivesAWholeNumberOfCycles()
        {
            double f = AudioSynth.LoopFrequency(55.0, MusicCatalog.LoopSeconds);
            Assert.AreEqual(0.0, f * MusicCatalog.LoopSeconds % 1.0, 1e-9);
            Assert.AreEqual(55.0, f, 0.05);
        }

        [Test]
        public void Normalize_ScalesToPeak_AndLeavesSilenceAlone()
        {
            float[] samples = { 0.1f, -0.2f, 0.05f };
            AudioSynth.Normalize(samples, 0.8f);
            Assert.AreEqual(0.8f, Peak(samples), 1e-5f);

            float[] silent = new float[10];
            AudioSynth.Normalize(silent, 0.8f);
            Assert.AreEqual(0f, Peak(silent));
        }

        [Test]
        public void Settings_Volumes_RoundTrip_AndClamp()
        {
            var settings = new GameSettings { MusicVolume = 3, SfxVolume = 9 };
            GameSettings copy = GameSettings.Parse(settings.Serialize());

            Assert.AreEqual(3, copy.MusicVolume);
            Assert.AreEqual(9, copy.SfxVolume);
            Assert.AreEqual(0.3f, copy.MusicGain, 1e-5f);

            settings.MusicVolume = 99;
            settings.SfxVolume = -4;
            Assert.AreEqual(10, settings.MusicVolume);
            Assert.AreEqual(0, settings.SfxVolume);
            Assert.AreEqual(6, new GameSettings().MusicVolume);
            Assert.AreEqual(8, new GameSettings().SfxVolume);
        }
    }
}
