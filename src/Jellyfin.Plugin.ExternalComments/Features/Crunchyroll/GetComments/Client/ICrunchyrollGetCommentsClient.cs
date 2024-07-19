using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;

public interface ICrunchyrollGetCommentsClient
{
    public Task<Result<CommentsResponse>> GetCommentsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}