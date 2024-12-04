using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;

public interface IGetCommentsRepository
{
    public Task<Result<IReadOnlyList<CommentItem>?>> GetCommentsAsync(string crunchyrollEpisodeId, int pageSize, int pageNumber, 
        CultureInfo language, CancellationToken cancellationToken);
}