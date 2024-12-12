using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;

public interface ISeriesGetMetadataService
{
    public Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Series>> GetMetadataAsync(SeriesInfo info,
        CancellationToken cancellationToken);
}