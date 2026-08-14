// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Framework.Logging;

namespace osu.Game.Tournament.Tosu
{
    public class TosuData
    {
        [JsonProperty("menu")]
        public TosuMenu? Menu;

        [JsonProperty("settings")]
        public TosuSettings? Settings;

        public static TosuData? Fetch()
        {
            JsonWebRequest<TosuData> webRequest = new JsonWebRequest<TosuData>
            {
                Url = "http://127.0.0.1:24050/json",
                Timeout = 200,
                AllowInsecureRequests = true
            };

            try
            {
                webRequest.Perform();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to fetch tosu data");
                return null;
            }
            return webRequest.ResponseObject;
        }
    }
}
