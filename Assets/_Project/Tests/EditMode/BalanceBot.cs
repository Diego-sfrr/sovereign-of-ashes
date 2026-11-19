using System;
using System.Collections.Generic;
using System.Linq;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Jogador automático simples usado só nos testes de equilíbrio: joga uma partida inteira sem a interface e diz
    /// se venceu. Não é esperto (não planeja combos), então serve de "jogador mediano" para comparar dificuldades.
    /// </summary>
    public static class BalanceBot
    {
        /// <summary>Resultado de uma partida simulada.</summary>
        public sealed class Result
        {
            /// <summary>Se o Núcleo sobreviveu até o fim.</summary>
            public bool Won { get; set; }

            /// <summary>Turno em que a partida acabou.</summary>
            public int Turn { get; set; }

            /// <summary>Vida do Núcleo no fim.</summary>
            public int CoreHealth { get; set; }
        }

        /// <summary>Joga uma partida até o fim.</summary>
        /// <param name="settings">Dificuldade, civilização, pacto e semente.</param>
        /// <param name="maxTurns">Limite de turnos (segurança contra laço infinito).</param>
        /// <param name="tweak">Ajuste opcional da configuração antes de começar (usado para explorar valores).</param>
        /// <returns>Resultado.</returns>
        public static Result Play(GameSettings settings, int maxTurns = 60, Action<GameSessionConfig> tweak = null)
        {
            GameSessionConfig config = GameConfigFactory.Build(settings);
            tweak?.Invoke(config);

            using (var session = new GameSession(config))
            {
                session.Start();

                for (int guard = 0; guard < maxTurns * 3 && session.Turns.State != TurnState.GameOver; guard++)
                {
                    TakeTurn(session);
                }

                return new Result
                {
                    Won = session.Turns.Result != null && session.Turns.Result.Outcome == GameOutcome.Victory,
                    Turn = session.Turns.TurnNumber,
                    CoreHealth = session.DivineCore.CurrentHealth
                };
            }
        }

        private static void TakeTurn(GameSession session)
        {
            if (session.Rewards.HasPending)
            {
                // Escolhe a carta mais barata que causa dano; senão a primeira.
                int pick = 0;
                IReadOnlyList<ICardDefinition> choices = session.Rewards.Pending.Choices;

                for (int i = 0; i < choices.Count; i++)
                {
                    if (choices[i].RequiresInvader)
                    {
                        pick = i;
                        break;
                    }
                }

                session.Rewards.Choose(pick);
            }

            if (!session.Turns.IsPlayerTurn)
            {
                return;
            }

            for (int safety = 0; safety < 12; safety++)
            {
                if (!PlayBestCard(session))
                {
                    break;
                }
            }

            session.Player.EndTurn();
        }

        private static bool PlayBestCard(GameSession session)
        {
            IReadOnlyList<ICardDefinition> hand = session.Deck.Hand;
            bool coreHurt = session.DivineCore.CurrentHealth < session.DivineCore.MaxHealth - 5;
            TectonicSector threat = MostDangerous(session);

            // 1) Dano em ataque: o invasor mais perto do Núcleo.
            if (threat != null)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    if (hand[i].RequiresInvader && session.Player.Preview(i, threat) == CardPlayOutcome.Played)
                    {
                        return session.Player.PlayFromHand(i, threat) == CardPlayOutcome.Played;
                    }
                }
            }

            // 2) Cura quando o Núcleo está ferido; Sermão quando a Razão está alta; resto das globais.
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].RequiresInvader || hand[i].AllowedRings != TargetRings.None)
                {
                    continue;
                }

                bool heals = hand[i].Id == "miracle" || hand[i].Id == "blessed_blood";
                bool calmsReason = hand[i].Id == "sermon" || hand[i].Id == "penance";

                if ((heals && !coreHurt) || (calmsReason && !AnyCivilizationTense(session)))
                {
                    continue;
                }

                if (session.Player.Preview(i, null) == CardPlayOutcome.Played)
                {
                    return session.Player.PlayFromHand(i, null) == CardPlayOutcome.Played;
                }
            }

            // 3) Infusões e demais cartas de setor: num setor vazio qualquer do anel do meio.
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].RequiresInvader || hand[i].AllowedRings == TargetRings.None)
                {
                    continue;
                }

                foreach (TectonicSector sector in session.Board.GetAllSectors())
                {
                    if (session.Player.Preview(i, sector) == CardPlayOutcome.Played)
                    {
                        return session.Player.PlayFromHand(i, sector) == CardPlayOutcome.Played;
                    }
                }
            }

            return false;
        }

        private static bool AnyCivilizationTense(GameSession session) =>
            session.Civilizations.Any(c => c.Rationality >= c.MaxRationality - 3);

        private static TectonicSector MostDangerous(GameSession session)
        {
            // Mais perto do Núcleo = anel de menor valor; entre iguais, o de menos vida.
            return session.Invaders.Invaders
                .Where(i => i.CurrentHealth > 0)
                .OrderBy(i => (int)i.Sector.Ring)
                .ThenBy(i => i.CurrentHealth)
                .Select(i => i.Sector)
                .FirstOrDefault();
        }
    }
}
