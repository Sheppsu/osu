// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Tosu
{
    public class TosuBeatmapStats
    {
        [JsonProperty("AR")]
        public float ApproachRate;

        [JsonProperty("CS")]
        public float CircleSize;

        [JsonProperty("OD")]
        public float OverallDifficulty;

        [JsonProperty("HP")]
        public float DrainRate;

        [JsonProperty("SR")]
        public float StarRating;
    }
}
