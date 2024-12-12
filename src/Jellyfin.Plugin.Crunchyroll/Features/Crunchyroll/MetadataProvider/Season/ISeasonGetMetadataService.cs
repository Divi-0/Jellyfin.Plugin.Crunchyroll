using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;

public interface ISeasonGetMetadataService
{
    public Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Season>> GetMetadataAsync(SeasonInfo info,
        CancellationToken cancellationToken);
}