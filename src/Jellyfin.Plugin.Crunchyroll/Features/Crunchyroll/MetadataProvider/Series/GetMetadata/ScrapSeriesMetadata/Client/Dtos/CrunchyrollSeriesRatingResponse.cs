namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;

public record CrunchyrollSeriesRatingResponse
{
    public string Average { get; init; } = string.Empty;
}