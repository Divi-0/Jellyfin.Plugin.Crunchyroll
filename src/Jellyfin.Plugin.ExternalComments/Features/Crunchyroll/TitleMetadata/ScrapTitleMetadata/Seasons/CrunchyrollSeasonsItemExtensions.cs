using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;

public static class CrunchyrollSeasonsItemExtensions
{
    public static Season ToSeasonEntity(this CrunchyrollSeasonsItem item, IReadOnlyList<Episode> episodes)
    {
        return new Season()
        {
            Id = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            SeasonNumber = item.SeasonNumber,
            Episodes = episodes.ToList(),
        };
    }
}