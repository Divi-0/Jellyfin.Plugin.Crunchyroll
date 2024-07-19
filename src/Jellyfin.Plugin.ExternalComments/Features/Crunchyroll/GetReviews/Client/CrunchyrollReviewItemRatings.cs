namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

public record CrunchyrollReviewItemRatings
{
    public required CrunchyrollReviewItemRatingsItem Yes { get; init; }
    public required CrunchyrollReviewItemRatingsItem No { get; init; }
    public required int Total { get; init; }
}