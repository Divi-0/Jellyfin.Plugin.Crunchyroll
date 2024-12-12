using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;

public interface IEpisodeOverwriteParentIndexNumberService
{
    public Task<ItemUpdateType> SetParentIndexAsync(MediaBrowser.Controller.Entities.TV.Episode episode);
}