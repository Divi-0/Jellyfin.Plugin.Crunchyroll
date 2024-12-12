using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;

public record CrunchyrollSeasonsItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    [JsonPropertyName("slug_title")]
    public required string SlugTitle { get; init; }
    public required string Identifier { get; init; }
    [JsonPropertyName("season_display_number")]
    public required string SeasonDisplayNumber { get; init; }
    [JsonPropertyName("season_number")]
    public required int SeasonNumber { get; init; }
    [JsonPropertyName("season_sequence_number")]
    public required int SeasonSequenceNumber { get; init; }
}