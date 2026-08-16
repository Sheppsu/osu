// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Tosu
{
    public class TosuBeatmap
    {
        [JsonProperty("stats")]
        public TosuBeatmapStats? Stats;

        [JsonProperty("path")]
        public TosuBeatmapPath? Path;

        [JsonProperty("id")]
        public int ID;
    }
}
