using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SearchSeries;

public interface ISearchSeriesService
{
    public Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(SeriesInfo searchInfo,
        CancellationToken cancellationToken);
}