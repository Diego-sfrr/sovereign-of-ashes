using System.IO;
using SovereignOfAshes.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SovereignOfAshes.EditorTools
{
    /// <summary>
    /// Ferramentas de editor: cria a cena jogável e gera o executável do Windows.
    /// Disponível no menu "Sovereign of Ashes" e por linha de comando (<c>-executeMethod</c>).
    /// </summary>
    public static class SovereignSetup
    {
        /// <summary>Caminho da cena principal.</summary>
        public const string ScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Caminho do executável gerado (relativo à pasta do projeto).</summary>
        public const string WindowsBuildPath = "Builds/Windows/SovereignOfAshes.exe";

        /// <summary>
        /// Cria (ou recria) a cena principal com a câmera e o jogo, e a registra no Build Settings.
        /// </summary>
        [MenuItem("Sovereign of Ashes/Criar cena de jogo")]
        public static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();

            var game = new GameObject("Sovereign of Ashes");
            game.AddComponent<SovereignGameView>(); // o SovereignBootstrapper vem junto (RequireComponent)

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sovereign] Cena criada em {ScenePath}. Aperte Play para jogar.");
        }

        /// <summary>
        /// Gera o executável do Windows (64 bits) em <see cref="WindowsBuildPath"/>.
        /// </summary>
        [MenuItem("Sovereign of Ashes/Gerar executável do Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
            }

            PlayerSettings.companyName = "Diego";
            PlayerSettings.productName = "Sovereign of Ashes";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Sovereign] Executável gerado em {WindowsBuildPath} ({report.summary.totalSize / (1024 * 1024)} MB).");
                return;
            }

            Debug.LogError($"[Sovereign] Falha ao gerar o executável: {report.summary.result}.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Linha de comando: cria a cena e gera o executável.
        /// </summary>
        public static void CreateSceneAndBuild()
        {
            CreateMainScene();
            BuildWindows();
        }
    }
}
