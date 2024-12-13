using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;

public interface ICrunchyrollMovieGetMetadataService
{
    public Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> GetMetadataAsync(MovieInfo info,
        CancellationToken cancellationToken);
}