using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;

public interface ICrunchyrollEpisodeCommentsService
{
    public Task<ItemUpdateType> ScrapCommentsAsync(MediaBrowser.Controller.Entities.TV.Episode episode, 
        CancellationToken cancellationToken);
}