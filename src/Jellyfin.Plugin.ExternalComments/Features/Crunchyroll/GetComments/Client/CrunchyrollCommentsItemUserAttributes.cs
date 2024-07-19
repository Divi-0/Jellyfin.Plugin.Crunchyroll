namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;

public record CrunchyrollCommentsItemUserAttributes
{
    public string Username { get; init; } = string.Empty;
    public required CrunchyrollCommentsItemUserAttributesAvatar Avatar { get; init; }
}