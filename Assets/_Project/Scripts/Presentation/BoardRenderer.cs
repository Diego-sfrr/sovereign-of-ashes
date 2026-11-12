using System;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Texturas usadas pelo <see cref="BoardRenderer"/>.
    /// </summary>
    public sealed class BoardSkin
    {
        /// <summary>Textura de terreno de cada anel (do mais interno ao mais externo).</summary>
        public Texture2D[] RingTextures { get; }

        /// <summary>Fundo atrás do tabuleiro.</summary>
        public Texture2D Backdrop { get; }

        /// <summary>Brilho radial para a sombra do tabuleiro e o halo do Núcleo (opcional).</summary>
        public Texture2D Glow { get; }

        /// <summary>
        /// Cria o conjunto de texturas.
        /// </summary>
        /// <param name="ringTextures">Uma textura por anel.</param>
        /// <param name="backdrop">Fundo.</param>
        /// <param name="glow">Brilho radial (opcional).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="ringTextures"/> ou <paramref name="backdrop"/> forem nulos.</exception>
        public BoardSkin(Texture2D[] ringTextures, Texture2D backdrop, Texture2D glow = null)
        {
            RingTextures = ringTextures ?? throw new ArgumentNullException(nameof(ringTextures));
            Backdrop = backdrop ?? throw new ArgumentNullException(nameof(backdrop));
            Glow = glow;
        }
    }

    /// <summary>
    /// Desenha o tabuleiro no mundo com malhas geradas por código (sem prefabs): fundo com vinheta,
    /// disco do Núcleo e uma fatia de anel texturizada para cada placa. Cada anel tem um pai que gira.
    /// </summary>
    public sealed class BoardRenderer : IDisposable
    {
        private const int SegmentsPerSector = 10;
        private const float SectorGapDegrees = 1.6f;
        private const float TerrainUvScale = 0.33f;

        private readonly GameObject _root;
        private readonly Transform[] _ringRoots;
        private readonly Material[,] _sectorMaterials;
        private readonly Material _coreMaterial;
        private readonly Material _backdropMaterial;
        private readonly Transform _backdrop;
        private readonly BoardDecor _decor;
        private bool _disposed;

        /// <summary>Geometria usada.</summary>
        public BoardLayout Layout { get; }

        /// <summary>
        /// Cria todos os objetos do tabuleiro.
        /// </summary>
        /// <param name="layout">Geometria.</param>
        /// <param name="parent">Pai na hierarquia (pode ser nulo).</param>
        /// <param name="skin">Texturas.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="layout"/> ou <paramref name="skin"/> forem nulos.</exception>
        public BoardRenderer(BoardLayout layout, Transform parent, BoardSkin skin)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));

            if (skin == null)
            {
                throw new ArgumentNullException(nameof(skin));
            }

            _root = new GameObject("Board (gerado)");

            _root.transform.SetParent(parent, false);

            _backdropMaterial = CreateMaterial(skin.Backdrop);
            _backdrop = CreatePiece("Fundo", _root.transform, BuildQuad(1f, 1f, 2f), _backdropMaterial);
            _backdrop.localScale = new Vector3(44f, 26f, 1f);

            _coreMaterial = CreateMaterial(null);
            CreatePiece("Núcleo Divino", _root.transform, BuildDisc(layout.CoreRadius, 48), _coreMaterial);

            _ringRoots = new Transform[layout.RingCount];
            _sectorMaterials = new Material[layout.RingCount, layout.SectorsPerRing];

            for (int ring = 0; ring < layout.RingCount; ring++)
            {
                var ringObject = new GameObject($"Anel {ring}");
                ringObject.transform.SetParent(_root.transform, false);
                _ringRoots[ring] = ringObject.transform;
                Texture2D terrain = ring < skin.RingTextures.Length ? skin.RingTextures[ring] : null;

                for (int plate = 0; plate < layout.SectorsPerRing; plate++)
                {
                    Mesh mesh = BuildAnnularSector(
                        layout.GetInnerRadius(ring),
                        layout.GetOuterRadius(ring),
                        layout.GetAngleDegrees(plate),
                        layout.StepDegrees - SectorGapDegrees,
                        SegmentsPerSector);

                    Material material = CreateMaterial(terrain);
                    _sectorMaterials[ring, plate] = material;
                    CreatePiece($"Placa {ring}-{plate}", ringObject.transform, mesh, material);
                }
            }

            _decor = new BoardDecor(layout, _root.transform, skin.Glow);
        }

        /// <summary>
        /// Posiciona e dimensiona o fundo pintado para cobrir a área visível (chame quando a câmera ou a janela mudar).
        /// </summary>
        /// <param name="center">Centro no mundo.</param>
        /// <param name="size">Largura e altura no mundo.</param>
        public void SetBackdropRect(Vector2 center, Vector2 size)
        {
            _backdrop.localPosition = new Vector3(center.x, center.y, 0f);
            _backdrop.localScale = new Vector3(size.x, size.y, 1f);
        }

        /// <summary>
        /// Anima os enfeites (faixa de runas e halo do Núcleo).
        /// </summary>
        /// <param name="time">Tempo em segundos.</param>
        /// <param name="coreHealth01">Vida do Núcleo em [0, 1].</param>
        public void AnimateDecor(float time, float coreHealth01)
        {
            _decor.Animate(time, coreHealth01);
        }

        /// <summary>
        /// Gira visualmente um anel.
        /// </summary>
        /// <param name="ringIndex">Anel.</param>
        /// <param name="visualOffset">Deslocamento em posições (fracionário durante a animação).</param>
        public void SetRingRotation(int ringIndex, float visualOffset)
        {
            // Ângulo positivo em Z gira no sentido anti-horário; posições crescem no sentido horário.
            _ringRoots[ringIndex].localRotation = Quaternion.Euler(0f, 0f, -visualOffset * Layout.StepDegrees);
        }

        /// <summary>
        /// Define a cor (tom multiplicado sobre a textura) de uma placa.
        /// </summary>
        /// <param name="ringIndex">Anel.</param>
        /// <param name="plateIndex">Índice fixo da placa.</param>
        /// <param name="color">Cor.</param>
        public void SetSectorColor(int ringIndex, int plateIndex, Color color)
        {
            _sectorMaterials[ringIndex, plateIndex].color = color;
        }

        /// <summary>
        /// Define a cor do disco do Núcleo.
        /// </summary>
        /// <param name="color">Cor.</param>
        public void SetCoreColor(Color color)
        {
            _coreMaterial.color = color;
        }

        /// <summary>Destrói os objetos e materiais criados (as texturas pertencem a quem as criou).</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Material material in _sectorMaterials)
            {
                UnityEngine.Object.Destroy(material);
            }

            _decor.Dispose();
            UnityEngine.Object.Destroy(_coreMaterial);
            UnityEngine.Object.Destroy(_backdropMaterial);
            UnityEngine.Object.Destroy(_root);
            _disposed = true;
        }

        private static Transform CreatePiece(string name, Transform parent, Mesh mesh, Material material)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterial = material;
            return piece.transform;
        }

        private static Material CreateMaterial(Texture2D texture)
        {
            // "Sprites/Default" é unlit, multiplica textura x cor e vem incluído nos executáveis por padrão.
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
                // As peças são planas e vistas de frente: desliga o descarte de faces.
                material.SetFloat("_Cull", 0f);
            }

            return material;
        }

        private static Mesh BuildAnnularSector(float innerRadius, float outerRadius, float centerDegrees, float spanDegrees, int segments)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments * 6];
            float start = centerDegrees - spanDegrees * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (start + spanDegrees * i / segments) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[i * 2] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);

                // UV em coordenadas do mundo: a textura de terreno continua de uma placa para a outra.
                uvs[i * 2] = new Vector2(cos * innerRadius * TerrainUvScale, sin * innerRadius * TerrainUvScale);
                uvs[i * 2 + 1] = new Vector2(cos * outerRadius * TerrainUvScale, sin * outerRadius * TerrainUvScale);

                // Escurece a borda interna para dar profundidade.
                colors[i * 2] = new Color(0.72f, 0.72f, 0.72f, 1f);
                colors[i * 2 + 1] = Color.white;
            }

            for (int i = 0; i < segments; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 1;
                triangles[t + 5] = v + 3;
            }

            return CreateMesh("Setor", vertices, uvs, colors, triangles);
        }

        private static Mesh BuildDisc(float radius, int segments)
        {
            var vertices = new Vector3[segments + 1];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            colors[0] = Color.white;

            for (int i = 0; i < segments; i++)
            {
                float angle = 360f * i / segments * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
                colors[i + 1] = new Color(0.55f, 0.55f, 0.55f, 1f);

                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + 1 == segments ? 1 : i + 2;
            }

            return CreateMesh("Núcleo", vertices, uvs, colors, triangles);
        }

        private static Mesh BuildQuad(float width, float height, float depth)
        {
            float w = width * 0.5f;
            float h = height * 0.5f;
            var vertices = new[]
            {
                new Vector3(-w, -h, depth), new Vector3(-w, h, depth), new Vector3(w, h, depth), new Vector3(w, -h, depth)
            };
            var uvs = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            var colors = new[] { Color.white, Color.white, Color.white, Color.white };
            return CreateMesh("Fundo", vertices, uvs, colors, new[] { 0, 1, 2, 0, 2, 3 });
        }

        private static Mesh CreateMesh(string name, Vector3[] vertices, Vector2[] uvs, Color[] colors, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
