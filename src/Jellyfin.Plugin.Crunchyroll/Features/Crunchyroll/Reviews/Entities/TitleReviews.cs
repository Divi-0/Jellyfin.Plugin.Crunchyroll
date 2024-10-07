using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;

public record TitleReviews
{
    public Guid Id { get; init; }
    public required string TitleId { get; init; }
    public required IReadOnlyList<ReviewItem> Reviews { get; init; }
}