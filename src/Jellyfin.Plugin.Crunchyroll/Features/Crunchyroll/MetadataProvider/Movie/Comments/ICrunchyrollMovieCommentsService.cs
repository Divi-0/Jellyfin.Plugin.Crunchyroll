using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;

public interface ICrunchyrollMovieCommentsService
{
    public Task<ItemUpdateType> ScrapCommentsAsync(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        CancellationToken cancellationToken);
}