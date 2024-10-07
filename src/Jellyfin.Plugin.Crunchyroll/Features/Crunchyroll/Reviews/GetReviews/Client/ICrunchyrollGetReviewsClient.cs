using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

public interface ICrunchyrollGetReviewsClient
{
    public Task<Result<ReviewsResponse>> GetReviewsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}