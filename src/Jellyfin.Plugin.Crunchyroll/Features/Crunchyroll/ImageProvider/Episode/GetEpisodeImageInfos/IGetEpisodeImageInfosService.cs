using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public interface IGetEpisodeImageInfosService
{
    public Task<RemoteImageInfo[]> GetImageInfosAsync(MediaBrowser.Controller.Entities.TV.Episode episode, 
        CancellationToken cancellationToken);
}