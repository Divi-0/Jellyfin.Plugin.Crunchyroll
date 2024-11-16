using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;

public static class CrunchyrollSeasonsItemExtensions
{
    public static Season ToSeasonEntity(this CrunchyrollSeasonsItem item, IReadOnlyList<Episode> episodes)
    {
        return new Season()
        {
            Id = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            Identifier = item.Identifier,
            SeasonNumber = item.SeasonNumber,
            SeasonSequenceNumber = item.SeasonSequenceNumber,
            SeasonDisplayNumber = item.SeasonDisplayNumber,
            Episodes = episodes.ToList(),
        };
    }
}