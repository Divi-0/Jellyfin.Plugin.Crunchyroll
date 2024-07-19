using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;

public interface IHtmlReviewsExtractor
{
    public Task<Result<IReadOnlyList<ReviewItem>>> GetReviewsAsync(string url, CancellationToken cancellationToken = default);
}