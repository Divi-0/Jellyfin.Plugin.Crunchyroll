using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

public record CrunchyrollEpisodeResponse
{
    public IReadOnlyList<CrunchyrollEpisodeDataItem> Data { get; init; } = new List<CrunchyrollEpisodeDataItem>();
}

public record CrunchyrollEpisodeDataItem
{
    public required string Id { get; init; } = string.Empty;
    public required string Title { get; init; } = string.Empty;
    public required string Description { get; init; } = string.Empty;
    
    [JsonPropertyName("slug_title")]
    public string SlugTitle { get; init; } = string.Empty;
    
    [JsonPropertyName("episode_metadata")]
    public required CrunchyrollEpisodeDataItemEpisodeMetadata EpisodeMetadata { get; init; }
    
    public required CrunchyrollEpisodeImages Images { get; init; }
}

public record CrunchyrollEpisodeDataItemEpisodeMetadata
{
    public required string Episode { get; init; }
    
    [JsonPropertyName("episode_number")]
    public required int? EpisodeNumber { get; init; }
    
    [JsonPropertyName("sequence_number")]
    public required double SequenceNumber { get; init; }
    
    [JsonPropertyName("season_id")]
    public required string SeasonId { get; init; }
    
    [JsonPropertyName("season_title")]
    public required string SeasonTitle { get; init; }
    
    [JsonPropertyName("season_slug_title")]
    public required string SeasonSlugTitle { get; init; }
    
    [JsonPropertyName("season_number")]
    public required int SeasonNumber { get; init; }
    
    [JsonPropertyName("season_sequence_number")]
    public required int SeasonSequenceNumber { get; init; }
    
    [JsonPropertyName("season_display_number")]
    public required string SeasonDisplayNumber { get; init; }
    
    [JsonPropertyName("series_id")]
    public required string SeriesId { get; init; }
    
    [JsonPropertyName("series_title")]
    public required string SeriesTitle { get; init; }
    
    [JsonPropertyName("series_slug_title")]
    public required string SeriesSlugTitle { get; init; }
}