namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodeThumbnailSizes
{
    public required int Height { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required int Width { get; init; }
}