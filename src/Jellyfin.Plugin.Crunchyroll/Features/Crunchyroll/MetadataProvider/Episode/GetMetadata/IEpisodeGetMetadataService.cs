using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public interface IEpisodeGetMetadataService
{
    public Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Episode>> GetMetadataAsync(EpisodeInfo info,
        CancellationToken cancellationToken);
}