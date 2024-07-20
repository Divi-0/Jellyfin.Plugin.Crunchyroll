namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;

public record CrunchyrollReviewItemRatingsItem
{
    public required string Displayed { get; init; }
    public string Unit { get; init; } = string.Empty;
}