using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

public record CrunchyrollSearchDataItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    [JsonPropertyName("slug_title")]
    public string SlugTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CrunchyrollSearchDataImages? Images { get; init; }
    [JsonPropertyName("episode_metadata")]
    public CrunchyrollSearchDataEpisodeMetadata? EpisodeMetadata { get; init; }
    [JsonPropertyName("series_metadata")]
    public CrunchyrollSearchDataSeriesMetadata? SeriesMetadata { get; init; }
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
    [JsonPropertyName("episode_air_date")]
    public DateTime? EpisodeAirDate { get; init; }
}

public record CrunchyrollSearchDataSeriesMetadata
{
    [JsonPropertyName("series_launch_year")]
    public int? SeriesLaunchYear { get; init; }
}

public record CrunchyrollSearchDataImages
{
    [JsonPropertyName("poster_tall")]
    public CrunchyrollSearchDataImage[][]? PosterTall { get; init; }
    [JsonPropertyName("poster_wide")]
    public CrunchyrollSearchDataImage[][]? PosterWide { get; init; }
    public CrunchyrollSearchDataImage[][]? Thumbnail { get; init; }
}

public record CrunchyrollSearchDataImage
{
    public string Source { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public string Type { get; init; } = string.Empty;
}