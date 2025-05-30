using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public interface IHtmlReviewsExtractor
{
    public Task<Result<IReadOnlyList<ReviewItem>>> GetReviewsAsync(string url, CultureInfo language, CancellationToken cancellationToken = default);
}