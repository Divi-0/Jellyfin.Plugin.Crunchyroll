namespace Jellyfin.Plugin.Crunchyroll.Domain.Entities;

public class ImageSource
{
    public required string Uri { get; set; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}