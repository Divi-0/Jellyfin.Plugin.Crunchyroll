namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;

public record SearchResponse
{
    public required string SeriesId { get; init; }
    public required string SeriesSlugTitle { get; init; }
    public required string SeasonId { get; init; }
    public required string EpisodeId { get; init; }
    public required string EpisodeSlugTitle { get; init; }
}