using System.Collections.Generic;
using SovereignOfAshes.Achievements;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "conquistas" da view: a tela com a lista e o aviso que desce do topo quando uma é ganha.
    /// O progresso é gravado pelo <see cref="AchievementTracker"/>; aqui só se lê e se desenha.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private const float ToastSeconds = 4.6f;
        private const float ToastSlide = 0.45f;

        private readonly Queue<AchievementDefinition> _toastQueue = new Queue<AchievementDefinition>();
        private AchievementProgress _achievements = new AchievementProgress();
        private Session.ISettingsStore _achievementStore;
        private AchievementTracker _tracker;
        private AchievementDefinition _toast;
        private float _toastStart;
        private GUIStyle _progressStyle;

        private GUIStyle ProgressStyle => _progressStyle ?? (_progressStyle = new GUIStyle(HintStyle) { alignment = TextAnchor.MiddleRight });

        private void InitializeAchievements()
        {
            _achievementStore = _captureMode
                ? (Session.ISettingsStore)new Session.InMemorySettingsStore()
                : new PlayerPrefsSettingsStore("sovereign_of_ashes.achievements");
            _achievements = AchievementProgress.Parse(_achievementStore.Load());
        }

        private void StartTracking()
        {
            _tracker?.Dispose();
            _tracker = new AchievementTracker(_session, _bootstrapper.LastSettings, _achievements,
                () => _achievementStore.Save(_achievements.Serialize()));
            _tracker.Unlocked += definition =>
            {
                _toastQueue.Enqueue(definition);
                PlaySound(Audio.SoundId.Achievement);
            };
        }

        internal void ShowToastForCapture(string achievementId)
        {
            AchievementDefinition definition = AchievementCatalog.Find(achievementId);

            if (definition != null)
            {
                _toastQueue.Enqueue(definition);
            }
        }

        private void DrawAchievements(float width, float height)
        {
            var panel = new Rect((width - 1180f) * 0.5f, 30f, 1180f, height - 60f);
            _theme.Panel(panel);
            _theme.ShadowText(new Rect(panel.x, panel.y + 16f, panel.width, 44f),
                $"Conquistas  {_achievements.UnlockedCount}/{AchievementCatalog.All.Count}", _theme.Heading, GuiTheme.Gold);
            _theme.Divider(panel.x + 60f, panel.y + 68f, panel.width - 120f);

            const float entryWidth = 530f;
            int rows = (AchievementCatalog.All.Count + 1) / 2;
            float top = panel.y + 90f;

            // A lista ocupa o espaço entre o título e o botão VOLTAR, nunca passa dele.
            float available = panel.yMax - 100f - top;
            float stepY = Mathf.Min(79f, available / rows);
            float entryHeight = stepY - 7f;

            for (int i = 0; i < AchievementCatalog.All.Count; i++)
            {
                var entry = new Rect(panel.x + 40f + (i % 2) * (entryWidth + 40f), top + (i / 2) * stepY, entryWidth, entryHeight);
                DrawAchievementEntry(entry, AchievementCatalog.All[i]);
            }

            if (MenuButton(new Rect(panel.center.x - 130f, panel.yMax - 74f, 260f, 56f), "VOLTAR"))
            {
                _uiScreen = UiScreen.Menu;
            }
        }

        private void DrawAchievementEntry(Rect rect, AchievementDefinition definition)
        {
            bool unlocked = _achievements.IsUnlocked(definition.Id);
            Color previous = GUI.color;
            _theme.Frame(rect, unlocked ? GuiTheme.Gold : GuiTheme.Bronze,
                unlocked ? new Color(0.22f, 0.15f, 0.06f) : new Color(0.09f, 0.07f, 0.08f), unlocked ? 4f : 3f);

            var icon = new Rect(rect.x + 10f, rect.y + 11f, 50f, 50f);
            _theme.Image(icon, _art.Sprite("core"), unlocked ? Color.white : new Color(0.22f, 0.22f, 0.25f, 1f));

            GUI.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(rect.x + 72f, rect.y + 5f, rect.width - 84f, 26f), definition.Name, _theme.Title);
            var description = new GUIContent(definition.Description);
            GUI.Label(new Rect(rect.x + 72f, rect.y + 29f, rect.width - 84f, 40f), description, HintStyle);
            GUI.color = previous;

            if (definition.Goal > 1 && !unlocked)
            {
                int progress = _achievements.ProgressOf(definition);
                var bar = new Rect(rect.x + 72f, rect.yMax - 9f, rect.width - 84f, 4f);
                _theme.Fill(bar, new Color(0.04f, 0.03f, 0.04f));
                _theme.Fill(new Rect(bar.x, bar.y, bar.width * progress / definition.Goal, bar.height), GuiTheme.Gold);
                GUI.Label(new Rect(rect.xMax - 96f, rect.y + 6f, 86f, 22f), $"{progress}/{definition.Goal}", ProgressStyle);
            }
        }

        // Aviso "CONQUISTA DESBLOQUEADA" que desce do topo, fica um pouco e sobe; um de cada vez.
        private void DrawAchievementToast(float width)
        {
            if (_toast == null)
            {
                if (_toastQueue.Count == 0)
                {
                    return;
                }

                _toast = _toastQueue.Dequeue();
                _toastStart = Time.unscaledTime;
            }

            float age = Time.unscaledTime - _toastStart;

            if (age >= ToastSeconds)
            {
                _toast = null;
                return;
            }

            float slideIn = Mathf.Clamp01(age / ToastSlide);
            float slideOut = Mathf.Clamp01((ToastSeconds - age) / ToastSlide);
            float shown = Mathf.Min(slideIn, slideOut);
            shown = shown * shown * (3f - 2f * shown);

            var rect = new Rect((width - 460f) * 0.5f, Mathf.Lerp(-84f, 70f, shown), 460f, 74f);
            _theme.Frame(rect, GuiTheme.Gold, new Color(0.17f, 0.11f, 0.05f, 0.97f), 4f);
            _theme.Image(new Rect(rect.x + 10f, rect.y + 10f, 54f, 54f), _art.Sprite("core"), Color.white);
            _theme.ShadowText(new Rect(rect.x + 74f, rect.y + 6f, rect.width - 84f, 22f), "CONQUISTA DESBLOQUEADA", _theme.Tag, GuiTheme.Gold);
            _theme.ShadowText(new Rect(rect.x + 74f, rect.y + 28f, rect.width - 84f, 38f), _toast.Name, _theme.Number, Color.white);
        }
    }
}
