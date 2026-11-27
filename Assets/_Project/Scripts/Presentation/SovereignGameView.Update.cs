using System.Collections;
using SovereignOfAshes.Session;
using UnityEngine;
using UnityEngine.Networking;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Avisa no menu quando existe uma versão mais nova do jogo. Lê um <c>version.json</c> público (uma única
    /// requisição curta ao abrir o jogo, sem enviar nada do jogador); se estiver offline ou o endereço não estiver
    /// configurado, não acontece nada.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private UpdateManifest _update;

        private IEnumerator CheckForUpdate()
        {
            if (!SupportInfo.HasUpdateChannel || _captureMode)
            {
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(SupportInfo.UpdateManifestUrl))
            {
                request.timeout = 6;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    yield break;
                }

                if (UpdateManifest.TryParse(request.downloadHandler.text, out UpdateManifest manifest) &&
                    GameVersion.IsNewer(Application.version, manifest.Version))
                {
                    _update = manifest;
                }
            }
        }

        /// <summary>Faixa no alto do menu: "nova versão disponível" com botão de baixar.</summary>
        private void DrawUpdateBanner(float width)
        {
            if (_update == null)
            {
                return;
            }

            var bar = new Rect(width * 0.5f - 430f, 14f, 860f, 64f);
            _theme.Panel(bar);
            string notes = string.IsNullOrEmpty(_update.Notes) ? string.Empty : "  ·  " + _update.Notes;
            string text = $"Nova versão {_update.Version} disponível!{notes}";
            _theme.ShadowText(new Rect(bar.x + 20f, bar.y, bar.width - 270f, bar.height), text, _theme.Small, GuiTheme.Gold);

            if (MenuButton(new Rect(bar.xMax - 240f, bar.y + 9f, 220f, 46f), "BAIXAR", primary: true))
            {
                Application.OpenURL(_update.Url);
            }
        }
    }
}
