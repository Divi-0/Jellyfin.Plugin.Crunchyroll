using System;
using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Mappings;

public static class CrunchyRollCommentsResponseMappings
{
    public static CommentsResponse ToCommentResponse(this CrunchyrollCommentsResponse crunchyrollCommentsResponse)
    {
        var comments = crunchyrollCommentsResponse.Items.Select(crunchyrollCommentsItem => new CommentItem()
            {
                Author = crunchyrollCommentsItem.User.Attributes.Username,
                AvatarIconUri = crunchyrollCommentsItem.User.Attributes.Avatar.Unlocked.FirstOrDefault(x => x.Type.Equals("icon_60", StringComparison.OrdinalIgnoreCase))?.Source,
                Message = crunchyrollCommentsItem.Message,
                Likes = crunchyrollCommentsItem.Votes.Like,
                RepliesCount = crunchyrollCommentsItem.RepliesCount
            })
            .ToList();
        
        var commentResponse = new CommentsResponse()
        {
            Comments = comments,
            Total = comments.Count
        };
        
        return commentResponse;
    }
}