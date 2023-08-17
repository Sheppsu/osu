// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    // TODO: Update to derive from osu-side class?
    public partial class TournamentMatchScoreDisplay : CompositeDrawable
    {
        private const float bar_height = 18;

        [Resolved]
        protected LadderInfo LadderInfo { get; private set; }

        private readonly Bindable<GosuJson> gosuData = new Bindable<GosuJson>();
        private readonly Bindable<TournamentBeatmap> currentBeatmap = new Bindable<TournamentBeatmap>();
        private Bindable<string> currentBeatmapMod = new Bindable<string>();
        private Dictionary<int, PlayerInfo> playerTierValues = new Dictionary<int, PlayerInfo>();

        private readonly MatchScoreCounter score1Text;
        private readonly MatchScoreCounter score2Text;

        private readonly MatchScoreDiffCounter scoreDiffText;

        private readonly Drawable score1Bar;
        private readonly Drawable score2Bar;

        public TournamentMatchScoreDisplay()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new[]
            {
                new Box
                {
                    Name = "top bar red (static)",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height / 4,
                    Width = 0.5f,
                    Colour = TournamentGame.COLOUR_RED,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopRight
                },
                new Box
                {
                    Name = "top bar blue (static)",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height / 4,
                    Width = 0.5f,
                    Colour = TournamentGame.COLOUR_BLUE,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopLeft
                },
                score1Bar = new Box
                {
                    Name = "top bar red",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height,
                    Width = 0,
                    Colour = TournamentGame.COLOUR_RED,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopRight
                },
                score1Text = new MatchScoreCounter
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre
                },
                score2Bar = new Box
                {
                    Name = "top bar blue",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height,
                    Width = 0,
                    Colour = TournamentGame.COLOUR_BLUE,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopLeft
                },
                score2Text = new MatchScoreCounter
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre
                },
                scoreDiffText = new MatchScoreDiffCounter
                {
                    Anchor = Anchor.TopCentre,
                    Margin = new MarginPadding
                    {
                        Top = bar_height / 4,
                        Horizontal = 8
                    },
                    Alpha = 0
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            gosuData.BindValueChanged(_ => updateScores());
            gosuData.BindTo(ipc.GosuData);
            currentBeatmap.BindValueChanged(_ => updateMod());
            currentBeatmap.BindTo(ipc.Beatmap);
        }

        private void updateScores()
        {
            if (
                gosuData.Value is null || 
                gosuData.Value.GosuTourney is null || 
                gosuData.Value.GosuTourney.IpcClients is null
                ) return;
            if (LadderInfo.CurrentMatch.Value is null) {
                Logger.Log("Must select a match.", LoggingTarget.Runtime, LogLevel.Important);
                return;
            };
            var score1 = 0;
            var score2 = 0;
            var tierValue1 = 0;
            var tierValue2 = 0;

            foreach (var client in gosuData.Value.GosuTourney.IpcClients)
            {
                var playerInfo = getPlayerTierValue(Int32.Parse(client.Spectating.UserID));
                if (playerInfo is null) 
                {
                    Logger.Log("Failed to get player info", LoggingTarget.Runtime, LogLevel.Important);
                    return;
                }
                if (playerInfo.Team == 1)
                {
                    tierValue2 += playerInfo.TierValue;
                }
                else
                {
                    tierValue1 += playerInfo.TierValue;
                }

                var scoreMultiplier = getScoreMultiplier(client.Gameplay.Mods.Num);
                if (scoreMultiplier is null) 
                {
                    Logger.Log("Failed to get score multiplier", LoggingTarget.Runtime, LogLevel.Important);
                    return;
                }
                if (playerInfo.Team == 1)
                {
                    score2 += (int)((float)client.Gameplay.Score*scoreMultiplier);
                }
                else
                {
                    score1 += (int)((float)client.Gameplay.Score*scoreMultiplier);
                }
            }

            if (tierValue1 == 0 || tierValue2 == 0) {
                return;
            }

            if (tierValue1 != tierValue2 && currentBeatmapMod.Value != "TB")
            {
                var pickingTeam = getPickingTeam();
                if (pickingTeam is null) return;
                var biggerTeam = tierValue1 > tierValue2 ? 0 : 1;
                var m = biggerTeam == pickingTeam ? 0.1f : 0.2f;
                if (biggerTeam == 1)
                {
                    score2 = (int)((float)score2 * (1.0f - m*Math.Abs(tierValue1-tierValue2)));
                }
                else
                {
                    score1 = (int)((float)score2 * (1.0f - m*Math.Abs(tierValue1-tierValue2)));
                }
            }

            score1Text.Current.Value = score1;
            score2Text.Current.Value = score2;

            var winningText = score1 > score2 ? score1Text : score2Text;
            var losingText = score1 <= score2 ? score1Text : score2Text;

            winningText.Winning = true;
            losingText.Winning = false;

            var winningBar = score1 > score2 ? score1Bar : score2Bar;
            var losingBar = score1 <= score2 ? score1Bar : score2Bar;

            int diff = Math.Max(score1, score2) - Math.Min(score1, score2);

            losingBar.ResizeWidthTo(0, 400, Easing.OutQuint);
            winningBar.ResizeWidthTo(Math.Min(0.4f, MathF.Pow(diff / 1500000f, 0.5f) / 2), 400, Easing.OutQuint);

            scoreDiffText.Alpha = diff != 0 ? 1 : 0;
            scoreDiffText.Current.Value = -diff;
            scoreDiffText.Origin = score1 > score2 ? Anchor.TopLeft : Anchor.TopRight;
        }

        private void updateMod() 
        {
            RoundBeatmap activeBeatmap = null;
            if (LadderInfo.CurrentMatch.Value?.Round.Value?.Beatmaps is null) {
                Logger.Log("Beatmaps is null ??", LoggingTarget.Runtime, LogLevel.Important);
                return;
            };
            foreach (var beatmap in LadderInfo.CurrentMatch.Value.Round.Value.Beatmaps) {
                if (beatmap.Beatmap?.OnlineID == currentBeatmap.Value?.OnlineID)
                {
                    activeBeatmap = beatmap;
                    break;
                }
            }
            if (activeBeatmap is null) {
                Logger.Log("Failed to find active beatmap", LoggingTarget.Runtime, LogLevel.Important);
                return;
            };
            currentBeatmapMod.Value = activeBeatmap.Mods;
        }

        private TournamentUser getPlayer(TournamentTeam team, int userId) {
            if (team.Players is null) return null;
            foreach (var p in team.Players) {
                if (p.OnlineID == userId) return p;
            }
            return null;
        }

        private PlayerInfo getPlayerTierValue(int userId)
        {
            PlayerInfo playerInfo = null;
            if (playerTierValues.ContainsKey(userId)) 
            {
                playerInfo = playerTierValues[userId];
            } 
            else
            {
                var player = getPlayer(LadderInfo.CurrentMatch.Value?.Team1.Value, userId);
                var team = 0;
                if (player is null) {
                    player = getPlayer(LadderInfo.CurrentMatch.Value?.Team2.Value, userId);
                    team = 1;
                }

                if (player is null) return null;

                playerInfo = new PlayerInfo
                {
                    TierValue = 68-((int)player.Tier.ToUpperInvariant()[0]),
                    Team = team,
                };
                playerTierValues.Add(userId, playerInfo);
            }
            return playerInfo;
        }

        private float? getScoreMultiplier(int mods)
        {
            if (currentBeatmapMod.Value is null) return null;
            if ((mods & 2) == 2) return 1.75f; // EZ
            if (currentBeatmapMod.Value != "FM" && currentBeatmapMod.Value != "HR" && (mods & 16) == 16) return 1.2f; // HR
            return 1.0f;
        }

        private int? getPickingTeam() {
            var picksBans = LadderInfo.CurrentMatch.Value?.PicksBans;
            if (picksBans is null || picksBans.Count == 0) return null;
            return picksBans[picksBans.Count-1].Team == TeamColour.Red ? 0 : 1;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            score1Text.X = -Math.Max(5 + score1Text.DrawWidth / 2, score1Bar.DrawWidth);
            score2Text.X = Math.Max(5 + score2Text.DrawWidth / 2, score2Bar.DrawWidth);
        }

        private class PlayerInfo
        {
            public int TierValue { get; set; }
            public int Team { get; set; }
        }

        private partial class MatchScoreCounter : CommaSeparatedScoreCounter
        {
            private OsuSpriteText displayedSpriteText;

            public MatchScoreCounter()
            {
                Margin = new MarginPadding { Top = bar_height, Horizontal = 10 };
            }

            public bool Winning
            {
                set => updateFont(value);
            }

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
            {
                displayedSpriteText = s;
                displayedSpriteText.Spacing = new Vector2(-6);
                updateFont(false);
            });

            private void updateFont(bool winning)
                => displayedSpriteText.Font = winning
                    ? OsuFont.Torus.With(weight: FontWeight.Bold, size: 50, fixedWidth: true)
                    : OsuFont.Torus.With(weight: FontWeight.Regular, size: 40, fixedWidth: true);
        }

        private partial class MatchScoreDiffCounter : CommaSeparatedScoreCounter
        {
            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
            {
                s.Spacing = new Vector2(-2);
                s.Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: bar_height, fixedWidth: true);
            });
        }
    }
}
