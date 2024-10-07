namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

public record CrunchyrollCommentsItemUserAttributes
{
    public string Username { get; init; } = string.Empty;
    public required CrunchyrollCommentsItemUserAttributesAvatar Avatar { get; init; }
}