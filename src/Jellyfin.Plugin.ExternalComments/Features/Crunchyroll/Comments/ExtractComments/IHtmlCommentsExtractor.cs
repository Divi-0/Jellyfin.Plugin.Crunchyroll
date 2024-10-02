using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;

public interface IHtmlCommentsExtractor
{
    public Task<Result<IReadOnlyList<CommentItem>>> GetCommentsAsync(string url, CancellationToken cancellationToken);
}