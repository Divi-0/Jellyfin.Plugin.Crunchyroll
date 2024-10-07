using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews;

public interface IGetReviewsSession
{
    public ValueTask<Result<IReadOnlyList<ReviewItem>?>> GetReviewsForTitleIdAsync(string titleId);
}