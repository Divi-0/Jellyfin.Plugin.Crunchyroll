using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

public record CrunchyrollReviewsResponse
{
    public IReadOnlyList<CrunchyrollReviewItem> Items { get; init; } = new List<CrunchyrollReviewItem>();
}