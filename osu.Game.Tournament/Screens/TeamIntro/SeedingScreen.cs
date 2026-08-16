// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Ladder.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.TeamIntro
{
    public partial class SeedingScreen : TournamentMatchScreen
    {
        private Container mainContainer = null!;

        private readonly Bindable<TournamentTeam?> currentTeam = new Bindable<TournamentTeam?>();

        private TourneyButton showFirstTeamButton = null!;
        private TourneyButton showSecondTeamButton = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new TourneyVideo("seeding")
                {
                    RelativeSizeAxes = Axes.Both,
                    Loop = true,
                },
                mainContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        showFirstTeamButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Show first team",
                            Action = () => currentTeam.Value = CurrentMatch.Value?.Team1.Value,
                        },
                        showSecondTeamButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Show second team",
                            Action = () => currentTeam.Value = CurrentMatch.Value?.Team2.Value,
                        },
                        new SettingsTeamDropdown(LadderInfo.Teams)
                        {
                            LabelText = "Show specific team",
                            Current = currentTeam,
                        }
                    }
                }
            };

            currentTeam.BindValueChanged(teamChanged, true);
        }

        private void teamChanged(ValueChangedEvent<TournamentTeam?> team) => updateTeamDisplay();

        public override void Show()
        {
            base.Show();

            // Changes could have been made on editor screen.
            // Rather than trying to track all the possibilities (teams / players / scores) just force a full refresh.
            updateTeamDisplay();
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);

            if (match.NewValue == null)
            {
                showFirstTeamButton.Enabled.Value = false;
                showSecondTeamButton.Enabled.Value = false;
                return;
            }

            showFirstTeamButton.Enabled.Value = true;
            showSecondTeamButton.Enabled.Value = true;

            currentTeam.Value = match.NewValue.Team1.Value;
        }

        private void updateTeamDisplay() => Scheduler.AddOnce(() =>
        {
            if (currentTeam.Value == null)
            {
                mainContainer.Clear();
                return;
            }

            mainContainer.Children = new Drawable[]
            {
                new LeftInfo(currentTeam.Value) { Position = new Vector2(100, 250), },
                new RightInfo(currentTeam.Value) { Position = new Vector2(650, 250), },
            };
        });

        private partial class RightInfo : CompositeDrawable
        {
            public RightInfo(TournamentTeam team)
            {
                FillFlowContainer fill;

                Width = 600f;

                InternalChildren = new Drawable[]
                {
                    fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                    },
                };

                foreach (var seeding in team.SeedingResults)
                {
                    foreach (var beatmap in seeding.Beatmaps)
                    {
                        if (beatmap.Beatmap == null)
                            continue;

                        fill.Add(new BeatmapScoreRow(beatmap));
                    }
                }
            }

            private partial class BeatmapScoreRow : CompositeDrawable
            {
                private partial class BeatmapComponent : CompositeDrawable
                {
                    public BeatmapComponent(SeedingBeatmap beatmap)
                    {
                        AutoSizeAxes = Axes.Y;
                        Width = 400f;
                        Masking = true;
                        InternalChildren = new Drawable[]
                        {
                            new NoUnloadBeatmapSetCover
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(OsuColour.Gray(0.5f), OsuColour.Gray(0)),
                                OnlineInfo = beatmap.Beatmap,
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5),
                                Padding = new MarginPadding(10),
                                Children = new Drawable[]
                                {
                                    new TournamentSpriteText { Text = beatmap.Beatmap?.Metadata.Title ?? "", Font = OsuFont.Torus.With(weight: FontWeight.Bold), },
                                    new TournamentSpriteText { Text = "by", Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Regular) },
                                    new TournamentSpriteText { Text = beatmap?.Beatmap?.Metadata.Artist ?? "", Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Bold) },
                                }
                            },
                        };
                    }
                }

                public BeatmapScoreRow(SeedingBeatmap beatmap)
                {
                    Debug.Assert(beatmap.Beatmap != null);

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;
                    // Colour = OsuColour.Gray(0);

                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black,
                        },
                        new BeatmapComponent(beatmap),
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Width = 150f,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(35),
                            Padding = new MarginPadding { Top = 5f },
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText { Text = beatmap.Score.ToString("#,0"), Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 20), Width = 80 },
                                new TournamentSpriteText
                                    { Text = "#" + beatmap.Seed.Value.ToString("#,0"), Colour = getPlaceColour(beatmap.Seed.Value), Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 20) },
                            }
                        },
                    };
                }

                private static Colour4 getPlaceColour(int place)
                {
                    switch (place)
                    {
                        case 1:
                            return Colour4.Gold;

                        case 2:
                            return Colour4.Silver;

                        case 3:
                            return Colour4.SandyBrown;

                        default:
                            return TournamentGame.TEXT_COLOUR;
                    }
                }
            }

            private partial class ModRow : CompositeDrawable
            {
                private readonly string mods;
                private readonly int seeding;

                public ModRow(string mods, int seeding)
                {
                    this.mods = mods;
                    this.seeding = seeding;

                    Padding = new MarginPadding { Vertical = 10 };

                    AutoSizeAxes = Axes.Y;
                }

                [BackgroundDependencyLoader]
                private void load(TextureStore textures)
                {
                    FillFlowContainer row;

                    InternalChildren = new Drawable[]
                    {
                        row = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                        },
                    };

                    if (!string.IsNullOrEmpty(mods))
                    {
                        row.Add(new Sprite
                        {
                            Texture = textures.Get($"Mods/{mods.ToLowerInvariant()}"),
                            Scale = new Vector2(0.5f)
                        });
                    }

                    row.Add(new Container
                    {
                        Size = new Vector2(50, 16),
                        CornerRadius = 10,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                            },
                            new TournamentSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = seeding.ToString("#,0"),
                                Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR
                            },
                        }
                    });
                }
            }
        }

        private partial class LeftInfo : CompositeDrawable
        {
            public LeftInfo(TournamentTeam? team)
            {
                FillFlowContainer fill;

                Width = 300;

                if (team == null) return;

                InternalChildren = new Drawable[]
                {
                    fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new TeamDisplay(team) { Margin = new MarginPadding { Bottom = 30 } },
                            new RowDisplay("Average Rank:", $"#{team.AverageRank:#,0}"),
                            new RowDisplay("Seed:", team.Seed.Value),
                            new RowDisplay("Last year's placing:", team.LastYearPlacing.Value),
                            new Container { Margin = new MarginPadding { Bottom = 30 } },
                        }
                    },
                };

                fill.Add(new PlayerRowDisplay("Player", "Avg. Score", "Rank", true));

                foreach (var p in team.Players)
                {
                    long totalScore = 0;
                    int totalPlayed = 0;

                    foreach (var result in team.SeedingResults)
                    {
                        foreach (var beatmap in result.Beatmaps)
                        {
                            var score = beatmap.Scores.FirstOrDefault(s => s?.PlayerID == p.OnlineID, null);

                            if (score != null)
                            {
                                totalScore += score.Score;
                                totalPlayed++;
                            }
                        }
                    }

                    fill.Add(new PlayerRowDisplay(p.Username, totalPlayed == 0 ? "N/A" : Math.Round((double)totalScore / totalPlayed).ToString("#,0"), p.Rank?.ToString("\\##,0") ?? "-", false));
                }
            }

            internal partial class RowDisplay : CompositeDrawable
            {
                public RowDisplay(string left, string right)
                {
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;

                    InternalChildren = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = left,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.SemiBold),
                        },
                        new TournamentSpriteText
                        {
                            Text = right,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopLeft,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.Regular),
                        },
                    };
                }
            }

            internal partial class PlayerRowDisplay : CompositeDrawable
            {
                public PlayerRowDisplay(string username, string avgScore, string rank, bool isHeading)
                {
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;
                    var weight = isHeading ? FontWeight.Bold : FontWeight.Regular;

                    InternalChildren = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = username,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: weight),
                            Width = 180f,
                        },
                        new TournamentSpriteText
                        {
                            Text = avgScore,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: weight),
                            Position = new Vector2(180f, 0),
                        },
                        new TournamentSpriteText
                        {
                            Text = rank,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: weight),
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopLeft,
                        },
                    };
                }
            }

            private partial class TeamDisplay : DrawableTournamentTeam
            {
                public TeamDisplay(TournamentTeam? team)
                    : base(team)
                {
                    AutoSizeAxes = Axes.Both;

                    Flag.RelativeSizeAxes = Axes.None;
                    Flag.Scale = new Vector2(1.2f);

                    InternalChild = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            Flag,
                            new OsuSpriteText
                            {
                                Text = team?.FullName.Value ?? "???",
                                Font = OsuFont.Torus.With(size: 32, weight: FontWeight.SemiBold),
                                Colour = TournamentGame.TEXT_COLOUR,
                            },
                        }
                    };
                }
            }
        }

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);
        }
    }
}
