using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.SearchEpisode;

public interface ISearchEpisodeService
{
    public Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(EpisodeInfo searchInfo,
        CancellationToken cancellationToken);
}