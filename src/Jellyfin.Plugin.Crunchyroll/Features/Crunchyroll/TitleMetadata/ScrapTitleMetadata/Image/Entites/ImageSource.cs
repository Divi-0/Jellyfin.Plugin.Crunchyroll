namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;

public class ImageSource
{
    public required string Uri { get; set; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}