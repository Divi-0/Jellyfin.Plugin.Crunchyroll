﻿using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces
{
    internal interface IPostEpisodeIdSetTask
    {
        public Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken);
    }
}