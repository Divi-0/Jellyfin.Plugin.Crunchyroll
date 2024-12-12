using System;
using System.Globalization;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;

public static class CrunchyrollSeasonsItemExtensions
{
    public static Domain.Entities.Season ToSeasonEntity(this CrunchyrollSeasonsItem item, Guid seriesId, CultureInfo language)
    {
        return new Domain.Entities.Season
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