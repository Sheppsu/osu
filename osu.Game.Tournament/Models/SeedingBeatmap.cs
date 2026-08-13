// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    public class SeedingBeatmap
    {
        public int ID;

        [JsonProperty("BeatmapInfo")]
        public TournamentBeatmap? Beatmap;

        public List<SeedingScore> Scores = new List<SeedingScore>();

        public Bindable<int> Seed = new BindableInt
        {
            MinValue = 1,
            MaxValue = 256
        };

        private long score = long.MaxValue;

        public long Score
        {
            get
            {
                if (score == long.MaxValue)
                {
                    score = Scores.Sum(s => s.Score);
                }

                return score;
            }
            set
            {
                score = value;
            }
        }
    }
}
