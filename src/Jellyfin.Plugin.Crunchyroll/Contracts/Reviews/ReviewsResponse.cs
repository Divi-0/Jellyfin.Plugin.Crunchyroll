using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

public record ReviewsResponse
{
    public IReadOnlyList<ReviewItem> Reviews { get; init; } = new List<ReviewItem>();
}