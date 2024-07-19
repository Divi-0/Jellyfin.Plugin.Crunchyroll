using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

public record CrunchyrollReviewItem
{
    public required CrunchyrollReviewItemReview Review { get; init; }
    [JsonPropertyName("author_rating")]
    public required string AuthorRating { get; init; }
    public required CrunchyrollReviewItemAuthor Author { get; init; }
    public required CrunchyrollReviewItemRatings Ratings { get; init; }
}