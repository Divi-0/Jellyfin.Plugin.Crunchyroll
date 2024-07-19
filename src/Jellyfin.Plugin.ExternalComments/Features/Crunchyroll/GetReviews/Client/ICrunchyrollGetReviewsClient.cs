using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

public interface ICrunchyrollGetReviewsClient
{
    public Task<Result<ReviewsResponse>> GetReviewsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}