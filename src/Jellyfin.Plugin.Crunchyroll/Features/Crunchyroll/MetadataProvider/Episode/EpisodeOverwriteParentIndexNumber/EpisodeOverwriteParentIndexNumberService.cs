using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;

public class EpisodeOverwriteParentIndexNumberService : IEpisodeOverwriteParentIndexNumberService
{
    private readonly PluginConfiguration _config;

    public EpisodeOverwriteParentIndexNumberService(PluginConfiguration config)
    {
        _config = config;
    }
    
    public Task<ItemUpdateType> SetParentIndexAsync(MediaBrowser.Controller.Entities.TV.Episode episode)
    {
        if (!_config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        if (episode.AirsBeforeEpisodeNumber is null || episode.AirsBeforeSeasonNumber is null || 
            episode.ParentIndexNumber is null || episode.ParentIndexNumber.Value == 0)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        episode.ParentIndexNumber = 0; //Manipulate ParentIndex to Season 0 so that Jellyfin thinks it is a special episode
        
        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}