using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;

public interface IGetReviewsRepository
{
    public Task<Result<IReadOnlyList<ReviewItem>>> GetReviewsForTitleIdAsync(string titleId, CultureInfo language,
        CancellationToken cancellationToken);
}