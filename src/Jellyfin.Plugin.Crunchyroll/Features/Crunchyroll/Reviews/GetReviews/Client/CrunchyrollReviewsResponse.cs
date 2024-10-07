using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

public record CrunchyrollReviewsResponse
{
    public IReadOnlyList<CrunchyrollReviewItem> Items { get; init; } = new List<CrunchyrollReviewItem>();
}