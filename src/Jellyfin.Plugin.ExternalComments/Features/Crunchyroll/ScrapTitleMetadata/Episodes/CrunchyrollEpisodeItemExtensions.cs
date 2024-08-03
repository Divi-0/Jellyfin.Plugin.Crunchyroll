using Jellyfin.Plugin.ExternalComments.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes;

public static class CrunchyrollEpisodeItemExtensions
{
    public static Entities.Episode ToEpisodeEntity(this CrunchyrollEpisodeItem item)
    {
        return new Episode()
        {
            Id = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            Description = item.Description,
            EpisodeNumber = item.Episode,
        };
    }
}