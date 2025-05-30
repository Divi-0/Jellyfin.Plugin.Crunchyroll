using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;

public record CrunchyrollSeriesContentItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    [JsonPropertyName("slug_title")]
    public required string SlugTitle { get; init; }
    public required string Description { get; init; }
    [JsonPropertyName("content_provider")]
    public required string ContentProvider { get; init; }
    public required CrunchyrollSeriesImageItem Images { get; init; }
}