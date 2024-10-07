using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId.Client;

public record CrunchyrollSearchDataItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    [JsonPropertyName("slug_title")]
    public string SlugTitle { get; init; } = string.Empty;
}