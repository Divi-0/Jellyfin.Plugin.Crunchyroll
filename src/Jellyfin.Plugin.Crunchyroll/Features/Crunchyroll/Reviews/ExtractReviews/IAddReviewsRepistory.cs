using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public interface IAddReviewsRepistory : ISaveChanges
{
    public Task<Result> AddReviewsForTitleIdAsync(TitleReviews titleReviews, CancellationToken cancellationToken);
    public Task<Result<string?>> GetSeriesSlugTitle(CrunchyrollId seriesId, CancellationToken cancellationToken);
}