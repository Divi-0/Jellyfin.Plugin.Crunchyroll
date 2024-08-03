using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodeItem
{
    public required string Id { get; init; }
    [JsonPropertyName("slug_title")]
    public required string SlugTitle { get; init; }
    public required string Title { get; init; }
    public required CrunchyrollEpisodeImages Images { get; init; }
    public required string Description { get; init; }
    public required string Episode { get; init; }
}