using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;

public static class CrunchyrollSeasonsItemExtensions
{
    public static Season ToSeasonEntity(this CrunchyrollSeasonsItem item, Guid seriesId, CultureInfo language)
    {
        return new Season
        {
            CrunchyrollId = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            Identifier = item.Identifier,
            SeasonNumber = item.SeasonNumber,
            SeasonSequenceNumber = item.SeasonSequenceNumber,
            SeasonDisplayNumber = item.SeasonDisplayNumber,
            Episodes = [],
            SeriesId = seriesId,
            Language = language.Name
        };
    }
}