using System;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Modo celular: ativado em Android/iPhone (inclusive no navegador) ou por <c>-sovereign-mobile</c> /
    /// <c>?mobile=1</c> na página para testar no computador. Amplia a interface, usa medidas compactas e fontes maiores.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        // Fonte de texto maior no celular: o mesmo tamanho em pixels fica bem menor numa tela de 6 polegadas.
        private const float MobileFontScale = 1.15f;

        private bool _mobileUi;
        private float _uiZoom = 1f;

        /// <summary>Altura lógica atual da interface (a tela de referência dividida pelo zoom).</summary>
        private float CurrentHeight => ReferenceHeight / _uiZoom;

        private void DetectMobileUi()
        {
            bool forced = Array.IndexOf(Environment.GetCommandLineArgs(), "-sovereign-mobile") >= 0;
            string url = Application.absoluteURL ?? string.Empty;
            forced |= url.IndexOf("mobile=1", StringComparison.OrdinalIgnoreCase) >= 0;
            _mobileUi = Application.isMobilePlatform || forced;
            GuiTheme.FontScale = _mobileUi ? MobileFontScale : 1f;

            if (_mobileUi)
            {
                // 30 quadros por segundo bastam para um jogo de cartas e poupam bateria e temperatura.
                Application.targetFrameRate = 30;
                QualitySettings.vSyncCount = 0;
            }
        }

        // Medidor de desempenho: no celular escreve no log, a cada 10 s, os quadros por segundo, o pior quadro e a memória.
        private float _perfTime;
        private int _perfFrames;
        private float _perfWorst;
        private int _perfGcStart;

        private void TrackPerformance()
        {
            if (!_mobileUi)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            _perfTime += delta;
            _perfFrames++;
            _perfWorst = Mathf.Max(_perfWorst, delta);

            if (_perfTime < 10f)
            {
                return;
            }

            int gc = GC.CollectionCount(0);
            Debug.Log($"[Perf] fps={_perfFrames / _perfTime:F1} piorQuadro={_perfWorst * 1000f:F0}ms gc0={gc - _perfGcStart} " +
                      $"memoriaGerenciada={GC.GetTotalMemory(false) / (1024 * 1024)}MB tela={_uiScreen}");
            _perfTime = 0f;
            _perfFrames = 0;
            _perfWorst = 0f;
            _perfGcStart = gc;
        }

        /// <summary>Zoom da interface no celular, conforme a tela (telas de texto longo precisam de mais altura).</summary>
        private float MobileZoom()
        {
            if (_session != null && !IsMenuScreen)
            {
                return 1.3f;
            }

            switch (_uiScreen)
            {
                case UiScreen.HowToPlay:
                case UiScreen.Settings:
                    return 1.05f;
                case UiScreen.Civilization:
                case UiScreen.Covenant:
                    return 1.1f;
                case UiScreen.Achievements:
                    return 1.15f;
                default:
                    return 1.2f;
            }
        }
    }
}
