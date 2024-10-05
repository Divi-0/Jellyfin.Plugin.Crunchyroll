namespace Jellyfin.Plugin.ExternalComments.Contracts.Comments;

public class CommentItem
{
    public string? AvatarIconUri { get; set; }
    public required string Author { get; init; }
    public required string Message { get; init; }
    public int Likes { get; init; }
    public int RepliesCount { get; init; }
}