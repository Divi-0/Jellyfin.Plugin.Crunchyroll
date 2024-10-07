using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

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