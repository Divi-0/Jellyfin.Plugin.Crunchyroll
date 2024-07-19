using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews;

public interface IGetReviewsSession
{
    public ValueTask<Result<IReadOnlyList<ReviewItem>>> GetReviewsForTitleIdAsync(string titleId);
}