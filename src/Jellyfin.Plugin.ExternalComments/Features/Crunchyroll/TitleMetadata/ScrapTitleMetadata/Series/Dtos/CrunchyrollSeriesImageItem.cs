using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

public record CrunchyrollSeriesImageItem
{
    [JsonPropertyName("poster_tall")]
    public required CrunchyrollSeriesImage[][] PosterTall { get; init; } = [[]];
    [JsonPropertyName("poster_wide")]
    public required CrunchyrollSeriesImage[][] PosterWide { get; init; } = [[]];
}