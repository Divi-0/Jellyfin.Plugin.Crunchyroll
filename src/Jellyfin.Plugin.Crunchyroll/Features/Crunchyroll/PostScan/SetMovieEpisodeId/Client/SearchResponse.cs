namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;

public record SearchResponse
{
    public required string SeriesId { get; init; }
    public required string SeriesSlugTitle { get; init; }
    public required string EpisodeId { get; init; }
    public required string EpisodeSlugTitle { get; init; }
}