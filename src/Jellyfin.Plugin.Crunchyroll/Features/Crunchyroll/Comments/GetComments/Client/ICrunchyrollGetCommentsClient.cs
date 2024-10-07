using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

public interface ICrunchyrollGetCommentsClient
{
    public Task<Result<CommentsResponse>> GetCommentsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}