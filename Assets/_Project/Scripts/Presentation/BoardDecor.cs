using System;
using System.Collections.Generic;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Enfeites do tabuleiro, todos gerados por código: raios que ligam posições alinhadas entre os anéis,
    /// aros de bronze entre os anéis, uma faixa de runas que gira lentamente em volta, sombra sob o tabuleiro
    /// e um halo pulsante sobre o Núcleo. São decoração: nada aqui afeta as regras.
    /// </summary>
    public sealed class BoardDecor : IDisposable
    {
        private const float RuneBandGap = 0.10f;
        private const float DecorDepth = -0.02f;
        private const float HaloDepth = -0.04f;
        private const float ShadowDepth = 0.2f;

        private static readonly Color Gold = new Color(0.91f, 0.72f, 0.29f);
        private static readonly Color Bronze = new Color(0.55f, 0.38f, 0.18f);

        private readonly GameObject _root;
        private readonly Transform _runeBand;
        private readonly Transform _halo;
        private readonly Material _haloMaterial;
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly float _haloBaseScale;
        private bool _disposed;

        /// <summary>
        /// Cria os enfeites.
        /// </summary>
        /// <param name="layout">Geometria do tabuleiro.</param>
        /// <param name="parent">Pai na hierarquia.</param>
        /// <param name="glow">Textura de brilho radial (alfa em gradiente); sem ela, sombra e halo não são desenhados.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="layout"/> for nulo.</exception>
        public BoardDecor(BoardLayout layout, Transform parent, Texture2D glow)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _root = new GameObject("Enfeites (gerado)");
            _root.transform.SetParent(parent, false);

            float outer = layout.OuterEdge;

            if (glow != null)
            {
                Transform shadow = CreateGlow("Sombra", glow, new Color(0f, 0f, 0f, 0.75f), (outer + 1.5f) * 2f, ShadowDepth, out _);
                shadow.SetParent(_root.transform, false);

                _haloBaseScale = (layout.CoreRadius + layout.RingWidth * 1.5f) * 2f;
                _halo = CreateGlow("Halo do Núcleo", glow, new Color(1f, 0.82f, 0.4f, 0.0f), _haloBaseScale, HaloDepth, out _haloMaterial);
                _halo.SetParent(_root.transform, false);
            }

            // Raios fixos no mundo: o alinhamento radial depende só da posição angular, não da placa.
            var spokes = new MeshBuilder();

            for (int position = 0; position < layout.SectorsPerRing; position++)
            {
                spokes.AddRadialBar(layout.CoreRadius, outer, layout.GetAngleDegrees(position), 0.03f, new Color(Gold.r, Gold.g, Gold.b, 0.16f));
            }

            AddPiece("Raios", spokes, DecorDepth);

            // Aros de bronze: na borda do Núcleo, entre os anéis e na borda externa.
            var rims = new MeshBuilder();
            rims.AddArc(layout.CoreRadius + layout.RingGap * 0.5f - 0.025f, layout.CoreRadius + layout.RingGap * 0.5f + 0.025f, 0f, 360f, 96, Bronze);

            for (int ring = 0; ring < layout.RingCount - 1; ring++)
            {
                float middle = (layout.GetOuterRadius(ring) + layout.GetInnerRadius(ring + 1)) * 0.5f;
                rims.AddArc(middle - 0.03f, middle + 0.03f, 0f, 360f, 96, Bronze);
            }

            rims.AddArc(outer + 0.02f, outer + 0.07f, 0f, 360f, 96, new Color(Gold.r, Gold.g, Gold.b, 0.9f));
            AddPiece("Aros", rims, DecorDepth * 1.5f);

            // Faixa de runas (traços longos e curtos) que gira devagar.
            var runes = new MeshBuilder();
            int dashes = layout.SectorsPerRing * 8;
            float step = 360f / dashes;

            for (int i = 0; i < dashes; i++)
            {
                bool major = i % 4 == 0;
                float inner = outer + RuneBandGap;
                runes.AddArc(inner, inner + (major ? 0.16f : 0.08f), i * step, major ? step * 0.55f : step * 0.3f, 3,
                    new Color(Gold.r, Gold.g, Gold.b, major ? 0.6f : 0.32f));
            }

            _runeBand = AddPiece("Faixa de runas", runes, DecorDepth);
        }

        /// <summary>
        /// Anima os enfeites.
        /// </summary>
        /// <param name="time">Tempo em segundos.</param>
        /// <param name="coreHealth01">Vida do Núcleo em [0, 1] (o halo fica mais fraco e vermelho quando ele está ferido).</param>
        public void Animate(float time, float coreHealth01)
        {
            if (_runeBand != null)
            {
                _runeBand.localRotation = Quaternion.Euler(0f, 0f, time * 1.8f);
            }

            if (_halo != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2.4f);
                float health = Mathf.Clamp01(coreHealth01);
                Color tint = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.82f, 0.4f), health);
                tint.a = (0.20f + 0.16f * pulse) * (0.4f + 0.6f * health);
                _haloMaterial.color = tint;
                float scale = _haloBaseScale * (1f + 0.06f * pulse);
                _halo.localScale = new Vector3(scale, scale, 1f);
            }
        }

        /// <summary>Destrói objetos, malhas e materiais criados.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Mesh mesh in _meshes)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            foreach (Material material in _materials)
            {
                UnityEngine.Object.Destroy(material);
            }

            UnityEngine.Object.Destroy(_root);
            _disposed = true;
        }

        private Transform AddPiece(string name, MeshBuilder builder, float depth)
        {
            Mesh mesh = builder.ToMesh(name);
            _meshes.Add(mesh);
            var piece = new GameObject(name);
            piece.transform.SetParent(_root.transform, false);
            piece.transform.localPosition = new Vector3(0f, 0f, depth);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(null);
            return piece.transform;
        }

        private Transform CreateGlow(string name, Texture2D glow, Color color, float diameter, float depth, out Material material)
        {
            var piece = new GameObject(name);
            piece.transform.localPosition = new Vector3(0f, 0f, depth);
            piece.transform.localScale = new Vector3(diameter, diameter, 1f);
            Mesh mesh = MeshBuilder.UnitQuad();
            _meshes.Add(mesh);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            material = CreateMaterial(glow);
            material.color = color;
            piece.AddComponent<MeshRenderer>().sharedMaterial = material;
            return piece.transform;
        }

        private Material CreateMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/Internal-Colored");
            }

            var material = new Material(shader);

            if (texture != null)
            {
                material.mainTexture = texture;
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            _materials.Add(material);
            return material;
        }

        /// <summary>
        /// Montador de malhas 2D coloridas por vértice.
        /// </summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Color> _colors = new List<Color>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public static Mesh UnitQuad()
            {
                var mesh = new Mesh { name = "Quadrado" };
                mesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f)
                };
                mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
                mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateBounds();
                return mesh;
            }

            public void AddArc(float innerRadius, float outerRadius, float startDegrees, float spanDegrees, int segments, Color color)
            {
                int first = _vertices.Count;

                for (int i = 0; i <= segments; i++)
                {
                    float angle = (startDegrees + spanDegrees * i / segments) * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    AddVertex(new Vector3(cos * innerRadius, sin * innerRadius, 0f), color);
                    AddVertex(new Vector3(cos * outerRadius, sin * outerRadius, 0f), color);
                }

                for (int i = 0; i < segments; i++)
                {
                    int v = first + i * 2;
                    AddTriangle(v, v + 1, v + 2);
                    AddTriangle(v + 2, v + 1, v + 3);
                }
            }

            public void AddRadialBar(float fromRadius, float toRadius, float angleDegrees, float halfWidth, Color color)
            {
                float angle = angleDegrees * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var perp = new Vector3(-dir.y, dir.x, 0f) * halfWidth;
                int first = _vertices.Count;
                AddVertex(dir * fromRadius - perp, color);
                AddVertex(dir * fromRadius + perp, color);
                AddVertex(dir * toRadius + perp, color);
                AddVertex(dir * toRadius - perp, color);
                AddTriangle(first, first + 1, first + 2);
                AddTriangle(first, first + 2, first + 3);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.vertices = _vertices.ToArray();
                mesh.colors = _colors.ToArray();
                mesh.uv = _uvs.ToArray();
                mesh.triangles = _triangles.ToArray();
                mesh.RecalculateBounds();
                return mesh;
            }

            private void AddVertex(Vector3 position, Color color)
            {
                _vertices.Add(position);
                _colors.Add(color);
                _uvs.Add(Vector2.zero);
            }

            private void AddTriangle(int a, int b, int c)
            {
                _triangles.Add(a);
                _triangles.Add(b);
                _triangles.Add(c);
            }
        }
    }
}
