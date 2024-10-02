using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments;

public interface IGetCommentsSession
{
    public ValueTask<IReadOnlyList<CommentItem>> GetCommentsAsync(string episodeId, int pageSize, int pageNumber);
}