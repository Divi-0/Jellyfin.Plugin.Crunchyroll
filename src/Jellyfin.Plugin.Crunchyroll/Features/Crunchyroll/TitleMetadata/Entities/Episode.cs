namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record Episode
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required string Description { get; init; }
    public required string EpisodeNumber { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required double SequenceNumber { get; init; }
}