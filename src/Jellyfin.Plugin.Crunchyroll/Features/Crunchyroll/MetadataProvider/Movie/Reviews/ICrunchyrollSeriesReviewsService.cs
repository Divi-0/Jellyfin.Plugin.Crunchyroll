using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;

public interface ICrunchyrollMovieReviewsService
{
    public Task<ItemUpdateType> ScrapReviewsAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        CancellationToken cancellationToken);
}