using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;

public interface IGetCommentsSession
{
    public ValueTask<IReadOnlyList<CommentItem>> GetCommentsAsync(string episodeId, int pageSize, int pageNumber);
}