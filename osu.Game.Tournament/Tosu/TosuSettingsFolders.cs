// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Tosu
{
    public class TosuSettingsFolders
    {
        [JsonProperty("game")]
        public string Game = string.Empty;

        [JsonProperty("skin")]
        public string Skin = string.Empty;

        [JsonProperty("songs")]
        public string Songs = string.Empty;
    }
}
