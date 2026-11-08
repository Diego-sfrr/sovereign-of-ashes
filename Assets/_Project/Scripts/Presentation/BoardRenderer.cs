using System;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Desenha o tabuleiro no mundo com malhas geradas por código (sem prefabs nem assets):
    /// um disco para o Núcleo e uma fatia de anel para cada placa.
    /// Cada anel tem um objeto pai que gira para animar a rotação tectônica.
    /// </summary>
    public sealed class BoardRenderer : IDisposable
    {
        private const int SegmentsPerSector = 10;
        private const float SectorGapDegrees = 1.4f;

        private readonly GameObject _root;
        private readonly Transform[] _ringRoots;
        private readonly Material[,] _sectorMaterials;
        private readonly Material _coreMaterial;
        private bool _disposed;

        /// <summary>Geometria usada.</summary>
        public BoardLayout Layout { get; }

        /// <summary>
        /// Cria todos os objetos do tabuleiro.
        /// </summary>
        /// <param name="layout">Geometria.</param>
        /// <param name="parent">Pai na hierarquia (pode ser nulo).</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="layout"/> for nulo.</exception>
        public BoardRenderer(BoardLayout layout, Transform parent)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));

            _root = new GameObject("Board (gerado)");
            _root.transform.SetParent(parent, false);

            _coreMaterial = CreateMaterial();
            CreatePiece("Núcleo Divino", _root.transform, BuildDisc(layout.CoreRadius, 48), _coreMaterial);

            _ringRoots = new Transform[layout.RingCount];
            _sectorMaterials = new Material[layout.RingCount, layout.SectorsPerRing];

            for (int ring = 0; ring < layout.RingCount; ring++)
            {
                var ringObject = new GameObject($"Anel {ring}");
                ringObject.transform.SetParent(_root.transform, false);
                _ringRoots[ring] = ringObject.transform;

                for (int plate = 0; plate < layout.SectorsPerRing; plate++)
                {
                    Mesh mesh = BuildAnnularSector(
                        layout.GetInnerRadius(ring),
                        layout.GetOuterRadius(ring),
                        layout.GetAngleDegrees(plate),
                        layout.StepDegrees - SectorGapDegrees,
                        SegmentsPerSector);

                    Material material = CreateMaterial();
                    _sectorMaterials[ring, plate] = material;
                    CreatePiece($"Placa {ring}-{plate}", ringObject.transform, mesh, material);
                }
            }
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
        /// Define a cor de uma placa.
        /// </summary>
        /// <param name="ringIndex">Anel.</param>
        /// <param name="plateIndex">Índice fixo da placa.</param>
        /// <param name="color">Cor.</param>
        public void SetSectorColor(int ringIndex, int plateIndex, Color color)
        {
            _sectorMaterials[ringIndex, plateIndex].color = color;
        }

        /// <summary>
        /// Define a cor do Núcleo.
        /// </summary>
        /// <param name="color">Cor.</param>
        public void SetCoreColor(Color color)
        {
            _coreMaterial.color = color;
        }

        /// <summary>Destrói os objetos e materiais criados.</summary>
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

            UnityEngine.Object.Destroy(_coreMaterial);
            UnityEngine.Object.Destroy(_root);
            _disposed = true;
        }

        private static void CreatePiece(string name, Transform parent, Mesh mesh, Material material)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material CreateMaterial()
        {
            // "Sprites/Default" é unlit, respeita a cor e vem incluído nos executáveis por padrão.
            // As alternativas cobrem projetos em que ele foi removido dos "Always Included Shaders".
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/Internal-Colored");
            }

            var material = new Material(shader);

            if (material.HasProperty("_Cull"))
            {
                // As fatias são planas e vistas de frente: desliga o descarte de faces para não depender da ordem dos vértices.
                material.SetFloat("_Cull", 0f);
            }

            return material;
        }

        private static Mesh BuildAnnularSector(float innerRadius, float outerRadius, float centerDegrees, float spanDegrees, int segments)
        {
            var vertices = new Vector3[(segments + 1) * 2];
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
                colors[i * 2] = Color.white;
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

            return CreateMesh("Setor", vertices, colors, triangles);
        }

        private static Mesh BuildDisc(float radius, int segments)
        {
            var vertices = new Vector3[segments + 1];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = Color.white;

            for (int i = 0; i < segments; i++)
            {
                float angle = 360f * i / segments * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                colors[i + 1] = Color.white;

                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + 1 == segments ? 1 : i + 2;
            }

            return CreateMesh("Núcleo", vertices, colors, triangles);
        }

        private static Mesh CreateMesh(string name, Vector3[] vertices, Color[] colors, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
