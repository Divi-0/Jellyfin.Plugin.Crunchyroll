namespace Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

public record ReviewItemAuthor
{
    public required string Username { get; init; }
    public required string AvatarUri { get; init; }
}