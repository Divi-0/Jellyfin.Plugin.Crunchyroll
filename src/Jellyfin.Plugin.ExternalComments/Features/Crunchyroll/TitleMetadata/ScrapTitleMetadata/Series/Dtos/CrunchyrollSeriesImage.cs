namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

public record CrunchyrollSeriesImage
{
    public required string Height { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required string Width { get; init; }
}