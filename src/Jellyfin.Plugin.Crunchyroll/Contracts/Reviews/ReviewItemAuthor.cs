namespace Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

public record ReviewItemAuthor
{
    public required string Username { get; init; }
    public required string AvatarUri { get; set; }
}