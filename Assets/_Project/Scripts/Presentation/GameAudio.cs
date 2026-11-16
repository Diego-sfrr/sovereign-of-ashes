using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SovereignOfAshes.Audio;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Toca a música e os efeitos do jogo. Tudo é sintetizado por código (<see cref="SoundCatalog"/>, <see cref="MusicCatalog"/>)
    /// numa thread de segundo plano ao abrir, para o menu não travar; os clipes viram <see cref="AudioClip"/> no thread
    /// principal (<see cref="Pump"/>, uma vez por quadro). Efeitos tocados antes de ficarem prontos são simplesmente ignorados;
    /// a música pedida começa assim que o clipe existir, com fade-in.
    /// </summary>
    public sealed class GameAudio : IDisposable
    {
        private const int VoiceCount = 10;
        private const float MusicFadeSpeed = 0.8f;
        private const float DuckedFactor = 0.4f;

        private readonly bool _enabled;
        private readonly GameObject _root;
        private readonly AudioSource _music;
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private readonly Dictionary<SoundId, AudioClip> _sounds = new Dictionary<SoundId, AudioClip>();
        private readonly Dictionary<MusicId, AudioClip> _tracks = new Dictionary<MusicId, AudioClip>();
        private readonly ConcurrentQueue<KeyValuePair<SoundId, float[]>> _readySounds = new ConcurrentQueue<KeyValuePair<SoundId, float[]>>();
        private readonly ConcurrentQueue<KeyValuePair<MusicId, float[]>> _readyTracks = new ConcurrentQueue<KeyValuePair<MusicId, float[]>>();
        private readonly Dictionary<SoundId, float> _lastPlayed = new Dictionary<SoundId, float>();
        private readonly System.Random _random = new System.Random();
        private MusicId? _wantedMusic;
        private MusicId? _playingMusic;
        private int _nextVoice;
        private bool _ducked;
        private bool _disposed;

        /// <summary>Ganho da música em [0, 1] (o controle do jogador).</summary>
        public float MusicGain { get; set; } = 0.6f;

        /// <summary>Ganho dos efeitos em [0, 1] (o controle do jogador).</summary>
        public float SfxGain { get; set; } = 0.8f;

        /// <summary>Quantos clipes já ficaram prontos (efeitos + músicas).</summary>
        public int LoadedCount => _sounds.Count + _tracks.Count;

        /// <summary>
        /// Cria o reprodutor.
        /// </summary>
        /// <param name="enabled">Se <c>false</c> nada é criado nem tocado (usado no modo de captura automática de telas).</param>
        public GameAudio(bool enabled)
        {
            _enabled = enabled;

            if (!enabled)
            {
                return;
            }

            _root = new GameObject("Áudio do jogo");
            _music = _root.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0f;
            _music.spatialBlend = 0f;

            for (int i = 0; i < VoiceCount; i++)
            {
                var voice = _root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                _voices.Add(voice);
            }
        }

        /// <summary>Começa a sintetizar tudo em segundo plano (menu primeiro, depois os efeitos, por fim a música de batalha).</summary>
        public void StartLoading()
        {
            if (!_enabled)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    _readyTracks.Enqueue(new KeyValuePair<MusicId, float[]>(MusicId.Menu, MusicCatalog.Render(MusicId.Menu)));

                    foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
                    {
                        _readySounds.Enqueue(new KeyValuePair<SoundId, float[]>(id, SoundCatalog.Render(id)));
                    }

                    _readyTracks.Enqueue(new KeyValuePair<MusicId, float[]>(MusicId.Battle, MusicCatalog.Render(MusicId.Battle)));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Falha ao sintetizar o áudio: " + exception.Message);
                }
            });
        }

        /// <summary>
        /// Chame uma vez por quadro: cria os clipes que ficaram prontos e ajusta o volume da música.
        /// </summary>
        /// <param name="deltaTime">Tempo do quadro (s), sem escala.</param>
        public void Pump(float deltaTime)
        {
            if (!_enabled || _disposed)
            {
                return;
            }

            int budget = 4;

            while (budget-- > 0 && _readySounds.TryDequeue(out KeyValuePair<SoundId, float[]> sound))
            {
                _sounds[sound.Key] = MakeClip(sound.Key.ToString(), sound.Value);
            }

            while (_readyTracks.TryDequeue(out KeyValuePair<MusicId, float[]> track))
            {
                _tracks[track.Key] = MakeClip("Música " + track.Key, track.Value);
            }

            if (_wantedMusic.HasValue && _playingMusic != _wantedMusic && _tracks.TryGetValue(_wantedMusic.Value, out AudioClip clip))
            {
                _music.clip = clip;
                _music.volume = 0f;
                _music.Play();
                _playingMusic = _wantedMusic;
            }

            float target = _wantedMusic.HasValue
                ? MusicGain * MusicCatalog.DefaultVolume * (_ducked ? DuckedFactor : 1f)
                : 0f;
            _music.volume = Mathf.MoveTowards(_music.volume, target, deltaTime * MusicFadeSpeed);

            if (!_wantedMusic.HasValue && _playingMusic.HasValue && _music.volume <= 0.001f)
            {
                _music.Stop();
                _playingMusic = null;
            }
        }

        /// <summary>
        /// Toca um efeito (ignorado se ainda não estiver pronto, se o volume for 0 ou se o mesmo som tocou há pouco).
        /// </summary>
        /// <param name="id">Som.</param>
        /// <param name="volume">Volume extra (1 = normal).</param>
        /// <param name="pitchJitter">Variação aleatória do tom (0,05 = ±5%), para não soar sempre igual.</param>
        public void Play(SoundId id, float volume = 1f, float pitchJitter = 0.03f)
        {
            if (!_enabled || _disposed || SfxGain <= 0.001f || !_sounds.TryGetValue(id, out AudioClip clip))
            {
                return;
            }

            float now = Time.unscaledTime;

            if (_lastPlayed.TryGetValue(id, out float last) && now - last < SoundCatalog.MinInterval(id))
            {
                return;
            }

            _lastPlayed[id] = now;
            AudioSource voice = NextVoice();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(SoundCatalog.DefaultVolume(id) * volume * SfxGain);
            voice.pitch = 1f + ((float)_random.NextDouble() * 2f - 1f) * pitchJitter;
            voice.Play();
        }

        /// <summary>Pede uma música (troca com fade-in se for outra; não faz nada se já é a atual).</summary>
        /// <param name="id">Música.</param>
        public void PlayMusic(MusicId id)
        {
            if (!_enabled || _wantedMusic == id)
            {
                return;
            }

            _wantedMusic = id;
        }

        /// <summary>Faz a música sumir com fade-out.</summary>
        public void StopMusic()
        {
            _wantedMusic = null;
        }

        /// <summary>Abaixa a música (ex.: jogo pausado).</summary>
        /// <param name="ducked">Verdadeiro para abaixar.</param>
        public void SetDucked(bool ducked) => _ducked = ducked;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!_enabled)
            {
                return;
            }

            foreach (AudioClip clip in _sounds.Values)
            {
                UnityEngine.Object.Destroy(clip);
            }

            foreach (AudioClip clip in _tracks.Values)
            {
                UnityEngine.Object.Destroy(clip);
            }

            UnityEngine.Object.Destroy(_root);
        }

        private AudioSource NextVoice()
        {
            // Procura uma voz livre; se todas estiverem ocupadas, reaproveita a mais antiga (rodízio).
            for (int i = 0; i < _voices.Count; i++)
            {
                AudioSource candidate = _voices[(_nextVoice + i) % _voices.Count];

                if (!candidate.isPlaying)
                {
                    _nextVoice = (_nextVoice + i + 1) % _voices.Count;
                    return candidate;
                }
            }

            AudioSource stolen = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            return stolen;
        }

        private static AudioClip MakeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, AudioSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
