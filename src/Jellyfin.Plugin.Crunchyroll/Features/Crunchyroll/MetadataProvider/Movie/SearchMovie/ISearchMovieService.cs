using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;

public interface ISearchMovieService
{
    public Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(MovieInfo searchInfo,
        CancellationToken cancellationToken);
}