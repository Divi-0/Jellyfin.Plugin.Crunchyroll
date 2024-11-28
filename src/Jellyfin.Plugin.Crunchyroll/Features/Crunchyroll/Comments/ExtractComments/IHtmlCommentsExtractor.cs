using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public interface IHtmlCommentsExtractor
{
    public Task<Result<IReadOnlyList<CommentItem>>> GetCommentsAsync(string url, CultureInfo language, 
        CancellationToken cancellationToken);
}