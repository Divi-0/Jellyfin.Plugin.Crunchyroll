namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

public record CrunchyrollCommentsItemUserAttributesAvatarIcon
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}