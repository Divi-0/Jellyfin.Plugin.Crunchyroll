namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

public record CrunchyrollEpisodeThumbnailSizes
{
    public required int Height { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required int Width { get; init; }
}