using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;

public record CrunchyrollSeriesImageItem
{
    [JsonPropertyName("poster_tall")]
    public required CrunchyrollSeriesImage[][] PosterTall { get; init; } = [[]];
    [JsonPropertyName("poster_wide")]
    public required CrunchyrollSeriesImage[][] PosterWide { get; init; } = [[]];
}