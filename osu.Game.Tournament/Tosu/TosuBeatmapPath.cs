// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Tosu
{
    public class TosuBeatmapPath
    {
        [JsonProperty("full")]
        public string Full = string.Empty;

        [JsonProperty("folder")]
        public string Folder = string.Empty;

        [JsonProperty("file")]
        public string File = string.Empty;

        [JsonProperty("bg")]
        public string Background = string.Empty;

        [JsonProperty("audio")]
        public string Audio = string.Empty;
    }
}
