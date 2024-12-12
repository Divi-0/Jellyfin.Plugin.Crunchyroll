using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public interface IGetSeriesImageInfosService
{
    public Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.TV.Series series, 
        CancellationToken cancellationToken);
}