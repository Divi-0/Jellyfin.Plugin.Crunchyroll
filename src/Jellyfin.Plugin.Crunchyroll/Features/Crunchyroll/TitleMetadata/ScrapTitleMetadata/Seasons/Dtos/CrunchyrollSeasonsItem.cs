using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

public record CrunchyrollSeasonsItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    [JsonPropertyName("slug_title")]
    public required string SlugTitle { get; init; }
    [JsonPropertyName("season_number")]
    public required int SeasonNumber { get; init; }
}