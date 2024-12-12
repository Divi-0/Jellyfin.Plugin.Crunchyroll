using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

public record CrunchyrollEpisodeItem
{
    public required string Id { get; init; }
    [JsonPropertyName("slug_title")]
    public required string SlugTitle { get; init; }
    public required string Title { get; init; }
    public required CrunchyrollEpisodeImages Images { get; init; }
    public required string Description { get; init; }
    public required string Episode { get; init; }
    [JsonPropertyName("episode_number")]
    public required int? EpisodeNumber { get; init; }
    [JsonPropertyName("sequence_number")]
    public required double SequenceNumber { get; init; }
    [JsonPropertyName("season_id")]
    public required string SeasonId { get; init; }
    [JsonPropertyName("series_id")]
    public required string SeriesId { get; init; }
    [JsonPropertyName("series_slug_title")]
    public required string SeriesSlugTitle { get; init; }
}