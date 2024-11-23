using System.Text.Json.Serialization;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

public record CrunchyrollSearchDataItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    [JsonPropertyName("slug_title")]
    public string SlugTitle { get; init; } = string.Empty;
    [JsonPropertyName("episode_metadata")]
    public CrunchyrollSearchDataEpisodeMetadata? EpisodeMetadata { get; init; }
}

public record CrunchyrollSearchDataEpisodeMetadata
{
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