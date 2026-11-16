using System;
using System.Collections.Generic;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Audio;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using UnityEngine;

namespace SovereignOfAshes.Presentation
{
    /// <summary>
    /// Parte "áudio" da view: transforma eventos da partida em efeitos sonoros e escolhe a música (menu ou batalha).
    /// A lógica do jogo não conhece o áudio: aqui só se escuta eventos que a sessão já expõe.
    /// </summary>
    public sealed partial class SovereignGameView
    {
        private GameAudio _audio;
        private readonly List<Action> _audioUnsubscribers = new List<Action>();
        private int _audioLastCoreHealth;
        private int _audioLastHoveredCard = -1;

        private void InitializeAudio()
        {
            _audio = new GameAudio(!_captureMode); // a captura automática de telas roda em silêncio
            ApplyAudioSettings();
            _audio.StartLoading();
            _audio.PlayMusic(MusicId.Menu);
        }

        private void ApplyAudioSettings()
        {
            if (_audio == null)
            {
                return;
            }

            _audio.MusicGain = _settings.MusicGain;
            _audio.SfxGain = _settings.SfxGain;
        }

        private void PlaySound(SoundId id, float volume = 1f) => _audio?.Play(id, volume);

        private void LateUpdate()
        {
            if (_audio == null)
            {
                return;
            }

            _audio.Pump(Time.unscaledDeltaTime);

            bool inGame = _session != null && !IsMenuScreen;

            if (!inGame)
            {
                _audio.PlayMusic(MusicId.Menu);
            }
            else if (_session.Turns.State == TurnState.GameOver)
            {
                _audio.StopMusic();
            }
            else
            {
                _audio.PlayMusic(MusicId.Battle);
            }

            _audio.SetDucked(_uiScreen == UiScreen.Paused);

            if (inGame && _hoveredCard >= 0 && _hoveredCard != _audioLastHoveredCard)
            {
                PlaySound(SoundId.CardHover);
            }

            _audioLastHoveredCard = inGame ? _hoveredCard : -1;
        }

        private void BindAudio()
        {
            UnbindAudio();

            if (_session == null)
            {
                return;
            }

            GameSession s = _session;
            _audioLastCoreHealth = s.DivineCore.CurrentHealth;

            Action<ICardDefinition, TectonicSector> played = (card, sector) => PlaySound(SoundId.CardPlay);
            s.Cards.CardPlayed += played;
            _audioUnsubscribers.Add(() => s.Cards.CardPlayed -= played);

            Action<AlchemyResult> reaction = result =>
            {
                if (result != null && result.ChangedSector)
                {
                    PlaySound(result.Recipe != null ? SoundId.Fusion : SoundId.Infuse);
                }
            };
            s.Alchemy.ReactionResolved += reaction;
            _audioUnsubscribers.Add(() => s.Alchemy.ReactionResolved -= reaction);

            Action<InvaderEntity> slain = invader => PlaySound(SoundId.Slay);
            s.Invaders.InvaderSlain += slain;
            _audioUnsubscribers.Add(() => s.Invaders.InvaderSlain -= slain);

            Action<InvaderEntity> spawned = invader => PlaySound(SoundId.Spawn);
            s.Invaders.InvaderSpawned += spawned;
            _audioUnsubscribers.Add(() => s.Invaders.InvaderSpawned -= spawned);

            Action<Combatant> core = c =>
            {
                if (c.CurrentHealth < _audioLastCoreHealth)
                {
                    PlaySound(SoundId.CoreHit);
                }
                else if (c.CurrentHealth > _audioLastCoreHealth)
                {
                    PlaySound(SoundId.Heal);
                }

                _audioLastCoreHealth = c.CurrentHealth;
            };
            s.DivineCore.Changed += core;
            _audioUnsubscribers.Add(() => s.DivineCore.Changed -= core);

            Action<FaithPool, int, int> faith = (pool, before, after) =>
                PlaySound(after > before ? SoundId.FaithGain : SoundId.FaithSpend);
            s.Faith.Changed += faith;
            _audioUnsubscribers.Add(() => s.Faith.Changed -= faith);

            Action<RingType, int> rotated = (ring, delta) => PlaySound(SoundId.RingRotate);
            s.Board.RingRotated += rotated;
            _audioUnsubscribers.Add(() => s.Board.RingRotated -= rotated);

            Action<int> turn = n => PlaySound(SoundId.TurnBell);
            s.Turns.AwaitingPlayer += turn;
            _audioUnsubscribers.Add(() => s.Turns.AwaitingPlayer -= turn);

            Action<GameOverInfo> over = info =>
                PlaySound(info.Outcome == GameOutcome.Victory ? SoundId.Victory : SoundId.Defeat);
            s.Turns.GameOver += over;
            _audioUnsubscribers.Add(() => s.Turns.GameOver -= over);

            foreach (CivilizationEntity civilization in s.Civilizations)
            {
                CivilizationEntity entity = civilization;
                Action<CivilizationEntity> heresy = c => PlaySound(SoundId.Heresy);
                entity.HeresyDeclared += heresy;
                _audioUnsubscribers.Add(() => entity.HeresyDeclared -= heresy);
            }

            Action<InvaderEntity, TectonicSector, ISectorContent> corrupt = (invader, sector, content) => PlaySound(SoundId.Corrupt);
            s.TraitsPhase.ElementCorrupted += corrupt;
            _audioUnsubscribers.Add(() => s.TraitsPhase.ElementCorrupted -= corrupt);

            Action<RewardOffer> offer = o => PlaySound(SoundId.Offer);
            s.Rewards.OfferCreated += offer;
            _audioUnsubscribers.Add(() => s.Rewards.OfferCreated -= offer);

            Action<ICardDefinition> picked = chosen => PlaySound(chosen != null ? SoundId.CardSelect : SoundId.Click);
            s.Rewards.OfferResolved += picked;
            _audioUnsubscribers.Add(() => s.Rewards.OfferResolved -= picked);

            Action<RingActionKind, RingType, RotationDirection> intervention = (kind, ring, direction) =>
            {
                if (kind == RingActionKind.Freeze)
                {
                    PlaySound(SoundId.Freeze);
                }
            };
            s.RingControl.Used += intervention;
            _audioUnsubscribers.Add(() => s.RingControl.Used -= intervention);
        }

        private void UnbindAudio()
        {
            foreach (Action unsubscribe in _audioUnsubscribers)
            {
                unsubscribe();
            }

            _audioUnsubscribers.Clear();
        }

        // Linha de volume nas Configurações: "-", barra de 10 segmentos e "+". Devolve o novo valor.
        private int DrawVolumeRow(float labelX, float controlX, float y, string label, int value, out bool changed)
        {
            GUI.Label(new Rect(labelX, y + 10f, 260f, 30f), label, _theme.Title);
            changed = false;
            int next = value;

            if (OptionButton(new Rect(controlX, y, 52f, 48f), "-", false))
            {
                next = Mathf.Max(0, value - 1);
            }

            const float segment = 34f;

            for (int i = 0; i < 10; i++)
            {
                var rect = new Rect(controlX + 66f + i * (segment + 4f), y + 8f, segment, 32f);
                _theme.Frame(rect, GuiTheme.Bronze, i < value ? GuiTheme.Gold : new Color(0.10f, 0.08f, 0.09f), 2f);
            }

            if (OptionButton(new Rect(controlX + 66f + 10 * (segment + 4f) + 8f, y, 52f, 48f), "+", false))
            {
                next = Mathf.Min(10, value + 1);
            }

            GUI.Label(new Rect(controlX + 66f + 10 * (segment + 4f) + 72f, y + 10f, 90f, 30f), value == 0 ? "mudo" : $"{value}/10", HintStyle);

            if (next != value)
            {
                changed = true;
            }

            return next;
        }
    }
}
