using System;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;

public record TitleReviews
{
    public Guid Id { get; init; } = default;
    public required string CrunchyrollSeriesId { get; init; }
    /// <summary>
    /// <see cref="ReviewItem"/> as array json serialized
    /// </summary>
    public required string Reviews { get; init; }
    public required string Language { get; init; }
}