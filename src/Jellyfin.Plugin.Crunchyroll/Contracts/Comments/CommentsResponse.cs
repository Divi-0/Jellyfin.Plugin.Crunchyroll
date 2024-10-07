using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

public class CommentsResponse
{
    public IReadOnlyList<CommentItem> Comments { get; init; } = new List<CommentItem>();
    public int Total { get; init; }
}