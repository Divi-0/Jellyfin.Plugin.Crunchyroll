using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public interface IGetMovieImageInfosService
{
    public Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        CancellationToken cancellationToken);
}