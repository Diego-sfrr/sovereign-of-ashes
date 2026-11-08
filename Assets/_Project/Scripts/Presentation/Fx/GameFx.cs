using System;
using System.Collections.Generic;
using UnityEngine;

namespace SovereignOfAshes.Presentation.Fx
{
    /// <summary>
    /// Números e textos que sobem e somem (dano, cura, Fé). Posições no mundo ou na tela (GUI).
    /// </summary>
    public sealed class FloatingTextFx
    {
        private struct Entry
        {
            public Vector2 Position;
            public bool IsWorld;
            public string Text;
            public Color Color;
            public float Age;
            public float Life;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// Cria um texto flutuante.
        /// </summary>
        /// <param name="position">Posição (mundo ou GUI).</param>
        /// <param name="isWorld">Se a posição está no mundo.</param>
        /// <param name="text">Texto.</param>
        /// <param name="color">Cor.</param>
        /// <param name="life">Duração em segundos.</param>
        public void Spawn(Vector2 position, bool isWorld, string text, Color color, float life = 1.3f)
        {
            _entries.Add(new Entry { Position = position, IsWorld = isWorld, Text = text, Color = color, Life = life });
        }

        /// <summary>Avança o tempo.</summary>
        /// <param name="deltaTime">Segundos.</param>
        public void Update(float deltaTime)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                entry.Age += deltaTime;

                if (entry.Age >= entry.Life)
                {
                    _entries.RemoveAt(i);
                }
                else
                {
                    _entries[i] = entry;
                }
            }
        }

        /// <summary>
        /// Desenha (chamar dentro de OnGUI).
        /// </summary>
        /// <param name="worldToGui">Conversão de mundo para GUI.</param>
        /// <param name="style">Estilo do texto.</param>
        public void Draw(Func<Vector2, Vector2> worldToGui, GUIStyle style)
        {
            Color previous = GUI.color;

            foreach (Entry entry in _entries)
            {
                float t = entry.Age / entry.Life;
                Vector2 gui = entry.IsWorld ? worldToGui(entry.Position) : entry.Position;
                gui.y -= 26f + t * 46f;
                float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                var rect = new Rect(gui.x - 80f, gui.y - 16f, 160f, 32f);

                GUI.color = new Color(0f, 0f, 0f, alpha * 0.85f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), entry.Text, style);
                GUI.color = new Color(entry.Color.r, entry.Color.g, entry.Color.b, alpha);
                GUI.Label(rect, entry.Text, style);
            }

            GUI.color = previous;
        }
    }

    /// <summary>
    /// Partículas quadradas (fumaça e faíscas) no espaço do mundo.
    /// </summary>
    public sealed class PuffFx
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Color Color;
            public float Size;
            public float Age;
            public float Life;
        }

        private readonly List<Particle> _particles = new List<Particle>();
        private readonly System.Random _random = new System.Random();

        /// <summary>
        /// Emite uma explosão de partículas.
        /// </summary>
        /// <param name="position">Origem no mundo.</param>
        /// <param name="color">Cor base.</param>
        /// <param name="count">Quantidade.</param>
        public void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                float speed = 0.6f + (float)_random.NextDouble() * 1.6f;
                Color tint = Color.Lerp(color, Color.white, (float)_random.NextDouble() * 0.35f);

                _particles.Add(new Particle
                {
                    Position = position,
                    Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed + 0.4f),
                    Color = tint,
                    Size = 4f + (float)_random.NextDouble() * 6f,
                    Life = 0.5f + (float)_random.NextDouble() * 0.5f
                });
            }
        }

        /// <summary>Avança o tempo.</summary>
        /// <param name="deltaTime">Segundos.</param>
        public void Update(float deltaTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle particle = _particles[i];
                particle.Age += deltaTime;

                if (particle.Age >= particle.Life)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                particle.Position = particle.Position + particle.Velocity * deltaTime;
                particle.Velocity = particle.Velocity * (1f - 2.2f * deltaTime);
                _particles[i] = particle;
            }
        }

        /// <summary>
        /// Desenha (chamar dentro de OnGUI).
        /// </summary>
        /// <param name="worldToGui">Conversão de mundo para GUI.</param>
        /// <param name="white">Textura branca 1x1.</param>
        public void Draw(Func<Vector2, Vector2> worldToGui, Texture2D white)
        {
            Color previous = GUI.color;

            foreach (Particle particle in _particles)
            {
                float alpha = 1f - particle.Age / particle.Life;
                Vector2 gui = worldToGui(particle.Position);
                GUI.color = new Color(particle.Color.r, particle.Color.g, particle.Color.b, alpha);
                GUI.DrawTexture(new Rect(gui.x - particle.Size * 0.5f, gui.y - particle.Size * 0.5f, particle.Size, particle.Size), white);
            }

            GUI.color = previous;
        }
    }

    /// <summary>
    /// Brasas que sobem lentamente pela tela (atmosfera).
    /// </summary>
    public sealed class EmberFx
    {
        private struct Ember
        {
            public float X;
            public float Y;
            public float Speed;
            public float Size;
            public float Phase;
        }

        private readonly Ember[] _embers;
        private readonly System.Random _random = new System.Random(7);
        private float _time;

        /// <summary>
        /// Cria o campo de brasas.
        /// </summary>
        /// <param name="count">Quantidade.</param>
        public EmberFx(int count)
        {
            _embers = new Ember[Math.Max(0, count)];

            for (int i = 0; i < _embers.Length; i++)
            {
                _embers[i] = NewEmber((float)_random.NextDouble());
            }
        }

        /// <summary>Avança o tempo.</summary>
        /// <param name="deltaTime">Segundos.</param>
        public void Update(float deltaTime)
        {
            _time += deltaTime;

            for (int i = 0; i < _embers.Length; i++)
            {
                _embers[i].Y -= _embers[i].Speed * deltaTime;

                if (_embers[i].Y < -0.02f)
                {
                    _embers[i] = NewEmber(1.02f);
                }
            }
        }

        /// <summary>
        /// Desenha (chamar dentro de OnGUI).
        /// </summary>
        /// <param name="width">Largura virtual da tela.</param>
        /// <param name="height">Altura virtual da tela.</param>
        /// <param name="white">Textura branca 1x1.</param>
        public void Draw(float width, float height, Texture2D white)
        {
            Color previous = GUI.color;

            foreach (Ember ember in _embers)
            {
                float flicker = 0.55f + 0.45f * Mathf.Sin(_time * 3f + ember.Phase);
                float x = ember.X * width + Mathf.Sin(_time * 0.8f + ember.Phase) * 12f;
                GUI.color = new Color(1f, 0.55f + 0.25f * flicker, 0.2f, 0.18f + 0.22f * flicker);
                GUI.DrawTexture(new Rect(x, ember.Y * height, ember.Size, ember.Size), white);
            }

            GUI.color = previous;
        }

        private Ember NewEmber(float y) => new Ember
        {
            X = (float)_random.NextDouble(),
            Y = y,
            Speed = 0.015f + (float)_random.NextDouble() * 0.035f,
            Size = 2f + (float)_random.NextDouble() * 3f,
            Phase = (float)_random.NextDouble() * 6.28f
        };
    }

    /// <summary>
    /// Tremor de tela com decaimento ("trauma").
    /// </summary>
    public sealed class ScreenShake
    {
        private readonly System.Random _random = new System.Random(3);
        private float _trauma;

        /// <summary>Adiciona trauma (0 a 1).</summary>
        /// <param name="amount">Intensidade.</param>
        public void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        /// <summary>
        /// Avança o tempo e devolve o deslocamento atual da câmera.
        /// </summary>
        /// <param name="deltaTime">Segundos.</param>
        /// <param name="maxOffset">Deslocamento máximo em unidades do mundo.</param>
        /// <returns>Deslocamento.</returns>
        public Vector2 Update(float deltaTime, float maxOffset)
        {
            _trauma = Mathf.Max(0f, _trauma - deltaTime * 1.6f);
            float power = _trauma * _trauma * maxOffset;
            return new Vector2(((float)_random.NextDouble() * 2f - 1f) * power, ((float)_random.NextDouble() * 2f - 1f) * power);
        }
    }
}
