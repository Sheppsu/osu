// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Menu;
using osu.Game.Tournament.Tosu;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class SongBar : CompositeDrawable
    {
        public const float HEIGHT = 145 / 2f;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        public IBeatmapInfo? Beatmap
        {
            get;
            private set;
        }

        public void UpdateBeatmap(IBeatmapInfo? newBeatmap, string newPoolMods = "")
        {
            if (Beatmap == newBeatmap && poolMods == newPoolMods)
                return;

            Beatmap = newBeatmap;
            poolMods = newPoolMods;

            mods.Clear();
            Ruleset rulesetInst = ruleset.Value.CreateInstance();

            for (int i = 0; i < newPoolMods.Length;)
            {
                bool isOptional = newPoolMods.Substring(i, 1) == "(";

                if (isOptional)
                {
                    i += 4;
                    continue;
                }

                string acronym = newPoolMods.Substring(i, 2);
                Mod? mod = rulesetInst.CreateModFromAcronym(acronym);

                if (mod != null)
                {
                    mods.Add(mod);
                }

                i += 2;
            }

            refreshContent();
        }

        private readonly List<Mod> mods = new List<Mod>(4);
        private string poolMods = "";

        private ScheduledDelegate? scheduledRefresh;

        private FillFlowContainer flow = null!;

        private bool expanded;

        public bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                flow.Direction = expanded ? FillDirection.Full : FillDirection.Vertical;
            }
        }

        // Todo: This is a hack for https://github.com/ppy/osu-framework/issues/3617 since this container is at the very edge of the screen and potentially initially masked away.
        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colours.Gray3,
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.4f,
                },
                flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };

            Expanded = true;

            refreshContent();
        }

        public void RefreshContent()
        {
            refreshContent();
        }

        private void refreshContent()
        {
            scheduledRefresh?.Cancel();
            scheduledRefresh = null;

            Beatmap ??= new BeatmapInfo
            {
                Metadata = new BeatmapMetadata
                {
                    Artist = "unknown",
                    Title = "no beatmap selected",
                    Author = new RealmUser { Username = "unknown" },
                },
                DifficultyName = "unknown",
                BeatmapSet = new BeatmapSetInfo(),
                StarRating = 0,
                Difficulty = new BeatmapDifficulty
                {
                    CircleSize = 0,
                    DrainRate = 0,
                    OverallDifficulty = 0,
                    ApproachRate = 0,
                },
            };

            var rulesetInstance = ruleset.Value.CreateInstance();

            double starRating;
            double ar;
            double cs;
            double od;
            double hp;

            var tosuData = TosuData.Fetch();
            var beatmapPath = tosuData?.Menu?.Beatmap?.Path;
            string? songsPath = tosuData?.Settings?.Folders?.Songs;
            bool dataIsValid = beatmapPath != null && beatmapPath.Folder != string.Empty && beatmapPath.File != string.Empty && !string.IsNullOrEmpty(songsPath);

            if (dataIsValid && tosuData!.Menu!.Beatmap!.ID == Beatmap.OnlineID)
            {
                string osuFilePath = Path.Join(songsPath, beatmapPath!.Folder, beatmapPath.File);
                var workingBeatmap = new FlatWorkingBeatmap(osuFilePath);
                var calc = rulesetInstance.CreateDifficultyCalculator(workingBeatmap);
                var modsWithoutHt = mods.Where(m => m.Acronym != "HT").ToArray();
                var difficulty = calc.Calculate(modsWithoutHt);
                var adjustedDifficulty = rulesetInstance.GetAdjustedDisplayDifficulty(workingBeatmap.BeatmapInfo, modsWithoutHt);
                ar = adjustedDifficulty.ApproachRate;
                cs = adjustedDifficulty.CircleSize;
                od = adjustedDifficulty.OverallDifficulty;
                hp = adjustedDifficulty.DrainRate;
                starRating = difficulty.StarRating;
            }
            else
            {
                // waiting for the right beatmap on tosu
                if (dataIsValid && Beatmap.OnlineID != -1)
                {
                    scheduledRefresh = Scheduler.AddDelayed(() =>
                    {
                        scheduledRefresh = null;
                        refreshContent();
                    }, 500);
                }

                var adjustedDifficulty = rulesetInstance.GetAdjustedDisplayDifficulty(Beatmap, mods);
                ar = adjustedDifficulty.ApproachRate;
                cs = adjustedDifficulty.CircleSize;
                od = adjustedDifficulty.OverallDifficulty;
                hp = adjustedDifficulty.DrainRate;
                starRating = Beatmap.StarRating;
            }

            double rate = ModUtils.CalculateRateWithMods(mods);
            double bpm = FormatUtils.RoundBPM(Beatmap.BPM, rate);
            double length = Beatmap.Length / rate;

            (string heading, string content)[] stats;

            switch (ruleset.Value.OnlineID)
            {
                default:
                    stats = new (string heading, string content)[]
                    {
                        ("CS", $"{cs:0.#}"),
                        ("AR", $"{ar:0.#}"),
                        ("OD", $"{od:0.#}"),
                    };
                    break;

                case 1:
                case 3:
                    stats = new (string heading, string content)[]
                    {
                        ("OD", $"{od:0.#}"),
                        ("HP", $"{hp:0.#}")
                    };
                    break;

                case 2:
                    stats = new (string heading, string content)[]
                    {
                        ("CS", $"{cs:0.#}"),
                        ("AR", $"{ar:0.#}"),
                    };
                    break;
            }

            flow.Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = HEIGHT,
                    Width = 0.5f,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,

                    Children = new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,

                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(stats),
                                            new DiffPiece(("Star Rating", $"{starRating.FormatStarRating()}"))
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(("Length", length.ToFormattedDuration().ToString())),
                                            new DiffPiece(("BPM", $"{bpm:0.#}")),
                                        }
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                Colour = Color4.Black,
                                                RelativeSizeAxes = Axes.Both,
                                                Alpha = 0.1f,
                                            },
                                            new OsuLogo
                                            {
                                                Triangles = false,
                                                Scale = new Vector2(0.08f),
                                                Margin = new MarginPadding(50),
                                                X = -10,
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                            },
                                        }
                                    },
                                },
                            }
                        }
                    }
                },
                new TournamentBeatmapPanel(Beatmap, poolMods)
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 0.5f,
                    Height = HEIGHT,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };
        }

        public partial class DiffPiece : TextFlowContainer
        {
            public DiffPiece(params (string heading, string content)[] tuples)
            {
                Margin = new MarginPadding { Horizontal = 15, Vertical = 1 };
                AutoSizeAxes = Axes.Both;

                static void cp(SpriteText s, bool bold)
                {
                    s.Font = OsuFont.Torus.With(weight: bold ? FontWeight.Bold : FontWeight.Regular, size: 15);
                }

                for (int i = 0; i < tuples.Length; i++)
                {
                    (string heading, string content) = tuples[i];

                    if (i > 0)
                    {
                        AddText(" / ", s =>
                        {
                            cp(s, false);
                            s.Spacing = new Vector2(-2, 0);
                        });
                    }

                    AddText(new TournamentSpriteText { Text = heading }, s => cp(s, false));
                    AddText(" ", s => cp(s, false));
                    AddText(new TournamentSpriteText { Text = content }, s => cp(s, true));
                }
            }
        }
    }
}
