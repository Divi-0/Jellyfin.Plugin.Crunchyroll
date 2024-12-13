using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;

public interface ICrunchyrollSeriesReviewsService
{
    public Task<ItemUpdateType> ScrapReviewsAsync(MediaBrowser.Controller.Entities.TV.Series series, 
        CancellationToken cancellationToken);
}