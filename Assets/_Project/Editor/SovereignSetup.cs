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
            PlayerSettings.runInBackground = true; // o jogo continua animando mesmo sem foco (e o modo de captura funciona)
            ApplyFastStartupSettings();

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

        /// <summary>Pasta da versão de navegador (WebGL), relativa à pasta do projeto.</summary>
        public const string WebBuildPath = "Builds/WebGL";

        /// <summary>
        /// Gera a versão de navegador (WebGL), que roda em computador e celular (Android e iPhone) sem instalar nada.
        /// Usa compressão Brotli com plano B no próprio carregador, então funciona em qualquer hospedagem estática.
        /// </summary>
        [MenuItem("Sovereign of Ashes/Gerar versão para navegador (WebGL)")]
        public static void BuildWebGL()
        {
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
            }

            PlayerSettings.companyName = "Diego";
            PlayerSettings.productName = "Sovereign of Ashes";
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:Sovereign";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Sovereign] Versão web gerada em {WebBuildPath} ({report.summary.totalSize / (1024 * 1024)} MB).");
                return;
            }

            Debug.LogError($"[Sovereign] Falha ao gerar a versão web: {report.summary.result}.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Arquivo APK gerado para Android (relativo à pasta do projeto).</summary>
        public const string AndroidBuildPath = "Builds/Android/SovereignOfAshes.apk";

        /// <summary>
        /// Gera o APK para Android (instalação direta, sem loja): somente horizontal, 64 bits (ARM64) e IL2CPP.
        /// Precisa do módulo "Android Build Support" (com SDK/NDK e JDK) instalado no Unity Hub.
        /// </summary>
        [MenuItem("Sovereign of Ashes/Gerar APK para Android")]
        public static void BuildAndroid()
        {
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
            }

            PlayerSettings.companyName = "Diego";
            PlayerSettings.productName = "Sovereign of Ashes";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.diego.sovereignofashes");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            Directory.CreateDirectory(Path.GetDirectoryName(AndroidBuildPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = AndroidBuildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Sovereign] APK gerado em {AndroidBuildPath} ({report.summary.totalSize / (1024 * 1024)} MB).");
                return;
            }

            Debug.LogError($"[Sovereign] Falha ao gerar o APK: {report.summary.result}.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Ajustes que encurtam a abertura do jogo: sem tela de splash do Unity e com Direct3D 11 (a inicialização do
        /// Direct3D 12 é bem mais lenta e este jogo 2D não usa nada que dependa dela).
        /// </summary>
        private static void ApplyFastStartupSettings()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
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
