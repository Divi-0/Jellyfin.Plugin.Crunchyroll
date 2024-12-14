namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;

public record CrunchyrollSeriesImage
{
    public required int Height { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required int Width { get; init; }
}