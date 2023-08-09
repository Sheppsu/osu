// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Online.API;

namespace osu.Game.Tournament.IPC
{
    public partial class FileAndGosuBasedIPC : FileBasedIPC
    {
        private DateTime gosuRequestWaitUntil = DateTime.Now.AddSeconds(5); // allow 15 seconds for lazer to start and get ready
        private ScheduledDelegate scheduled;
        private GosuJsonRequest gosuJsonQueryRequest;

        public class GosuJsonRequest : APIRequest<GosuJson>
        {
            protected override string Target => @"json";
            protected override string Uri => $@"http://localhost:24050/{Target}";

            protected override WebRequest CreateWebRequest()
            {
                // Thread.Sleep(500); // allow gosu to update json
                return new OsuJsonWebRequest<GosuJson>(Uri)
                {
                    AllowInsecureRequests = true,
                    Timeout = 200,
                };
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheduled?.Cancel();
            scheduled = Scheduler.AddDelayed(delegate
            {
                if (!API.IsLoggedIn || gosuRequestWaitUntil > DateTime.Now) // request inhibited
                {

                    return;
                }

                gosuJsonQueryRequest?.Cancel();
                gosuJsonQueryRequest = new GosuJsonRequest();
                gosuJsonQueryRequest.Success += data =>
                {
                    if (data == null)
                    {
                        Logger.Log("[Warning] failed to parse gosumemory json", LoggingTarget.Runtime, LogLevel.Important);
                        return;
                    }

                    if (data.GosuError != null)
                    {
                        Logger.Log($"[Warning] gosumemory reported an error: {data.GosuError}", LoggingTarget.Runtime, LogLevel.Important);
                        return;
                    }

                    GosuData.Value = data;
                };
                gosuJsonQueryRequest.Failure += exception =>
                {
                    Logger.Log($"Failed requesting gosu data: {exception}", LoggingTarget.Runtime, LogLevel.Important);
                    gosuRequestWaitUntil = DateTime.Now.AddSeconds(2); // inhibit calling gosu api again for 2 seconds if failure occured
                };
                API.Queue(gosuJsonQueryRequest);
            }, 250, true);
        }
    }
}