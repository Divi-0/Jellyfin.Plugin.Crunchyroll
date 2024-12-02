namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

public record CrunchyrollSeriesRatingResponse
{
    public string Average { get; init; } = string.Empty;
}