using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Persistence.Entities;

public record TitleReviews
{
    public Guid Id { get; init; }
    public required string TitleId { get; init; }
    public required IReadOnlyList<ReviewItem> Reviews { get; init; }
}