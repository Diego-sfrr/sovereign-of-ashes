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
