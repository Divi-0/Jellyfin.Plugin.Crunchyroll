using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public interface IHtmlReviewsExtractor
{
    public Task<Result<IReadOnlyList<ReviewItem>>> GetReviewsAsync(string url, CancellationToken cancellationToken = default);
}