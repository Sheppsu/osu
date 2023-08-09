// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Tournament.IPC
{
    public class GosuJson
    {
        [JsonProperty(@"error")]
        public string GosuError { get; set; }

        [JsonProperty(@"gameplay")]
        public GosuHasNameKey GosuGameplay { get; set; }

        [JsonProperty(@"menu")]
        public GosuJsonMenu GosuMenu { get; set; }

        [JsonProperty(@"resultsScreen")]
        public GosuHasNameKey GosuResultScreen { get; set; }

        [JsonProperty(@"tourney")]
        public GosuJsonTourney GosuTourney { get; set; }

        public class GosuHasNameKey
        {
            [JsonProperty(@"name")]
            public string Name { get; set; } = "";
        }

        public class GosuIpcClientGameplay
        {
            [JsonProperty(@"score")]
            public int Score { get; set; }

            [JsonProperty(@"mods")]
            public GosuIpcClientMods Mods { get; set; }

            [JsonProperty(@"accuracy")]
            public float Accuracy { get; set; }

            [JsonProperty(@"hits")]
            public GosuIpcClientHits Hits { get; set; }
        }

        public class GosuIpcClientHits
        {
            [JsonProperty(@"0")]
            public int MissCount { get; set; }

            [JsonProperty(@"sliderBreaks")]
            public int SliderBreaks { get; set; }
        }

        public class GosuIpcClientSpectating
        {
            [JsonProperty(@"name")]
            public string Name { get; set; }

            [JsonProperty(@"country")]
            public string Country { get; set; }

            [JsonProperty(@"userID")]
            public string UserID { get; set; }
        }

        public class GosuIpcClientMods
        {
            [JsonProperty(@"num")]
            public int Num { get; set; }

            [JsonProperty(@"str")]
            public string Str { get; set; }
        }

        public class GosuIpcClient
        {
            [JsonProperty(@"team")]
            public string Team { get; set; } = "";

            [JsonProperty(@"gameplay")]
            public GosuIpcClientGameplay Gameplay { get; set; }

            [JsonProperty(@"spectating")]
            public GosuIpcClientSpectating Spectating { get; set; }
        }

        public class GosuJsonTourney
        {
            [JsonProperty(@"ipcClients")]
            public List<GosuIpcClient> IpcClients { get; set; }
        }

        public class GosuMenuBeatmap
        {
            [JsonProperty(@"id")]
            public int Id { get; set; }

            [JsonProperty(@"md5")]
            public string MD5 { get; set; }

            [JsonProperty(@"set")]
            public int Set { get; set; }
        }

        public class GosuJsonMenu
        {
            [JsonProperty(@"bm")]
            public GosuMenuBeatmap Bm { get; set; }
        }
    }
}
