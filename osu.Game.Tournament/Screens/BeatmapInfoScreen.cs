// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens
{
    public abstract partial class BeatmapInfoScreen : TournamentMatchScreen
    {
        protected readonly SongBar SongBar;

        protected BeatmapInfoScreen()
        {
            AddInternal(SongBar = new SongBar
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Depth = float.MinValue,
            });
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            ipc.Beatmap.BindValueChanged(beatmapChanged, true);
            //ipc.Mods.BindValueChanged(modsChanged, true);
        }

        // private void modsChanged(ValueChangedEvent<LegacyMods> mods)
        // {
        //     SongBar.Mods = mods.NewValue;
        // }

        private void beatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
            SongBar.FadeInFromZero(300, Easing.OutQuint);
            SongBar.Beatmap = beatmap.NewValue;

            BindableList<TournamentRoundGroup>? roundGroups = LadderInfo.CurrentMatch.Value?.Round.Value?.RoundGroups;
            if (roundGroups == null || LadderInfo.Ruleset.Value == null || beatmap.NewValue == null)
                return;

            string bmMods = string.Empty;

            foreach (TournamentRoundGroup rg in roundGroups)
            {
                foreach (RoundBeatmap bm in rg.Beatmaps)
                {
                    if (bm.Beatmap != null && bm.Beatmap.OnlineID == beatmap.NewValue.OnlineID)
                    {
                        bmMods = bm.Mods;
                        goto search_done;
                    }
                }
            }

            search_done:

            if (bmMods == string.Empty)
                return;

            List<Mod> mods = new List<Mod>(4);
            Ruleset ruleset = LadderInfo.Ruleset.Value.CreateInstance();

            for (int i = 0; i < bmMods.Length;)
            {
                bool isOptional = bmMods.Substring(i, 1) == "(";

                if (isOptional)
                {
                    i += 4;
                    continue;
                }

                string acronym = bmMods.Substring(i, 2);
                Mod? mod = ruleset.CreateModFromAcronym(acronym);

                if (mod != null)
                {
                    mods.Add(mod);
                }

                i += 2;
            }

            SongBar.Mods = ruleset.ConvertToLegacyMods(mods.ToArray());
        }
    }
}
